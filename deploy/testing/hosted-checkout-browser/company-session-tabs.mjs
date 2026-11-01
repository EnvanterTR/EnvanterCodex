import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {chromium} from 'playwright';

const baseline=process.argv.includes('--baseline'),pending=new Map();
let loginRequests=0;
const server=createServer(async(req,res)=>{
  const url=new URL(req.url,'http://localhost'),path=url.pathname;
  res.setHeader('Cache-Control','no-store');
  if(path==='/api/test-private'){
    res.setHeader('Content-Type','application/json');res.write('{"company":');
    pending.set('body',res);return;
  }
  if(path.startsWith('/api/')){
    res.setHeader('Content-Type','application/json');
    if(path==='/api/status')return res.end('{"status":"ready"}');
    if(path==='/api/setup/status')return res.end('{"setupRequired":false}');
    if(path==='/api/session/csrf')return res.end('{"requestToken":"synthetic"}');
    if(path==='/api/session/login'){loginRequests++;return res.end('{"companyId":"11111111-1111-4111-8111-111111111111"}');}
    if(path==='/api/session/logout')return res.end('{}');
    res.statusCode=401;return res.end('{}');
  }
  try{
    res.setHeader('Content-Type',path.endsWith('.js')?'text/javascript':path.endsWith('.css')?'text/css':path.endsWith('.json')?'application/json':'text/html');
    res.end(await readFile(path==='/'?'/checkout-site/company-app.html':'/checkout-site'+path));
  }catch{res.statusCode=404;res.end();}
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const otherOriginServer=createServer(server.listeners('request')[0]);
await new Promise(resolve=>otherOriginServer.listen(0,'127.0.0.1',resolve));
const browser=await chromium.launch({headless:true});
try{
  for(const fallback of [false,true])for(const action of baseline?['logout']:['logout','login']){
    const context=await browser.newContext();
    if(fallback)await context.addInitScript(()=>{window.BroadcastChannel=undefined;});
    const first=await context.newPage(),second=await context.newPage();
    const origin='http://127.0.0.1:'+server.address().port+'/';
    await Promise.all([first.goto(origin),second.goto(origin)]);
    for(const page of [first,second])await page.waitForFunction(()=>document.body.classList.contains('auth-required'));
    // Fixture-only presentation: keep startup unauthenticated to avoid unrelated
    // module data loaders. Exercise actual api(), body guard and bound UI handlers.
    await second.evaluate(()=>hideLogin());
    if(action==='logout')await first.evaluate(()=>hideLogin());
    const reading=second.evaluate(async()=>{
      const response=await api('/api/test-private',{responseType:'raw'});window.bodyStarted=true;
      try{return {value:(await response.json()).company};}catch(error){return {code:error.code};}
    });
    await second.waitForFunction(()=>window.bodyStarted===true);
    if(action==='logout'){
      await first.locator('#logoutButton').click();
      await first.waitForFunction(()=>document.body.classList.contains('auth-required'));
    }else{
      const before=loginRequests;
      await first.locator('#loginForm [name="email"]').fill('synthetic@example.invalid');
      await first.locator('#loginForm [name="password"]').fill('SyntheticFixtureOnly-123!');
      await first.locator('#loginForm button[type="submit"]').click();
      await first.waitForFunction(()=>document.querySelector('#loginForm [name="password"]').value==='');
      assert.equal(loginRequests,before+1);
    }
    if(!baseline)await second.waitForFunction(()=>document.body.classList.contains('auth-required'));
    pending.get('body').end('"previous-company"}');pending.delete('body');
    const result=await reading;
    if(baseline){
      assert.equal(result.value,'previous-company');
      assert.equal(await second.evaluate(()=>document.body.classList.contains('auth-required')),false);
      console.log('BASELINE_COMPANY_OTHER_TAB_ACCEPTS_PRIVATE_BODY_AFTER_LOGOUT '+(fallback?'storage':'broadcast'));
    }else{
      assert.equal(result.code,'STALE_SESSION_RESPONSE');
      console.log('PASS company other tab rejects previous body after actual '+action+' '+(fallback?'storage fallback':'BroadcastChannel'));
    }
    await context.close();
  }
  if(!baseline)for(const fallback of [false,true]){
    const context=await browser.newContext();
    if(fallback)await context.addInitScript(()=>{window.BroadcastChannel=undefined;});
    const first=await context.newPage(),other=await context.newPage();
    await first.goto('http://127.0.0.1:'+server.address().port+'/');
    await other.goto('http://127.0.0.1:'+otherOriginServer.address().port+'/');
    for(const page of [first,other]){
      await page.waitForFunction(()=>document.body.classList.contains('auth-required'));
      await page.evaluate(()=>hideLogin());
    }
    const reading=other.evaluate(async()=>{
      const response=await api('/api/test-private',{responseType:'raw'});window.bodyStarted=true;
      return (await response.json()).company;
    });
    await other.waitForFunction(()=>window.bodyStarted===true);
    await first.locator('#logoutButton').click();
    await first.waitForFunction(()=>document.body.classList.contains('auth-required'));
    pending.get('body').end('"other-origin-company"}');pending.delete('body');
    assert.equal(await reading,'other-origin-company');
    assert.equal(await other.evaluate(()=>document.body.classList.contains('auth-required')),false);
    console.log('PASS different-origin company document remains independent '+(fallback?'storage fallback':'BroadcastChannel'));
    await context.close();
  }
  console.log(baseline?'COMPANY_CROSS_TAB_BASELINE 2/2 reproduced':'COMPANY_CROSS_TAB_BROWSER 6/6 passed');
}finally{
  for(const response of pending.values())response.end();
  await browser.close();await new Promise(resolve=>server.close(resolve));await new Promise(resolve=>otherOriginServer.close(resolve));
}
