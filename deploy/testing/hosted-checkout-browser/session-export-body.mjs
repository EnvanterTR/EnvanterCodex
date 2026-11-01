import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {chromium} from 'playwright';

const pending=new Map();
const server=createServer(async(req,res)=>{
  const url=new URL(req.url,'http://localhost'),path=url.pathname;
  res.setHeader('Cache-Control','no-store');
  if(path==='/api/test-export'){
    const method=url.searchParams.get('method');
    res.setHeader('Content-Type',method==='formData'?'application/x-www-form-urlencoded':'application/json');
    res.write(method==='formData'?'company=':'{"company":');
    pending.set(method,res);return;
  }
  if(path.startsWith('/api/')){
    res.setHeader('Content-Type','application/json');
    if(path==='/api/status')return res.end('{"status":"ready"}');
    if(path==='/api/setup/status')return res.end('{"setupRequired":false}');
    if(path==='/api/session/csrf')return res.end('{"requestToken":"fixture"}');
    res.statusCode=401;return res.end('{}');
  }
  try{
    res.setHeader('Content-Type',path.endsWith('.js')?'text/javascript':path.endsWith('.json')?'application/json':path.endsWith('.css')?'text/css':'text/html');
    res.end(await readFile(path==='/'?'/checkout-site/company-app.html':'/checkout-site'+path));
  }catch{res.statusCode=404;res.end();}
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const browser=await chromium.launch({headless:true});
try{
  for(const method of ['blob','text','arrayBuffer','formData','json'])for(const changeSession of [true,false]){
    const page=await browser.newPage();
    await page.goto('http://127.0.0.1:'+server.address().port+'/');
    await page.waitForFunction(()=>document.body.classList.contains('auth-required'));
    const read=page.evaluate(async method=>{
      const response=await api('/api/test-export?method='+method,{responseType:'raw'});
      window.exportBodyStarted=true;
      try{
        let value=await response[method]();
        if(value instanceof Blob)value=await value.text();
        if(value instanceof ArrayBuffer)value=new TextDecoder().decode(value);
        if(value instanceof FormData)value=value.get('company');
        return {value:typeof value==='object'?value.company:value};
      }catch(error){return {code:error.code};}
    },method);
    await page.waitForFunction(()=>window.exportBodyStarted===true);
    if(changeSession)await page.evaluate(()=>authSessionGeneration++);
    assert.ok(pending.has(method));
    pending.get(method).end(method==='formData'?'original':'"original"}');pending.delete(method);
    const result=await read;
    if(changeSession)assert.equal(result.code,'STALE_SESSION_RESPONSE');
    else{assert.equal(result.code,undefined);assert.match(result.value,/original/);}
    console.log('PASS native Response.'+method+' '+(changeSession?'rejects body after session change':'preserves current-session body'));
    await page.close();
  }
  console.log('SESSION_EXPORT_BODY_BROWSER 10/10 passed');
}finally{
  for(const response of pending.values())response.end();
  await browser.close();await new Promise(resolve=>server.close(resolve));
}
