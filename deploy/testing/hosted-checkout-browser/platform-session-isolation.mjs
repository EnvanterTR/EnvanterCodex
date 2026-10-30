import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {chromium} from 'playwright';

const pending=new Map();
let signedIn=false,authenticatedCsrfReads=0;
const server=createServer(async(req,res)=>{
  const url=new URL(req.url,'http://localhost'),path=url.pathname;
  res.setHeader('Cache-Control','no-store');
  if(path==='/api/test-private'){
    const method=url.searchParams.get('method');
    res.setHeader('Content-Type',method==='formData'?'application/x-www-form-urlencoded':'application/json');
    res.write(method==='formData'?'company=':'{"company":');
    pending.set(method,res);return;
  }
  if(path.startsWith('/api/')){
    if(path==='/api/session/login')signedIn=true;
    if(path==='/api/session/csrf'&&signedIn)authenticatedCsrfReads++;
    res.setHeader('Content-Type','application/json');
    const data=path==='/api/session/csrf'?{requestToken:'synthetic'}:
      (path==='/api/session'||path==='/api/session/login')?{platformAdmin:true,name:'Synthetic Admin'}:
      path==='/api/session/2fa'?{enabled:true}:
      path==='/api/status'?{status:'ready'}:
      path==='/api/platform/overview'?{companies:{},subscriptions:{},checkout:{}}:
      path==='/api/platform/bootstrap/status'?{setupRequired:false}:{items:[]};
    return res.end(JSON.stringify(data));
  }
  try{
    res.setHeader('Content-Type',path.endsWith('.js')?'text/javascript':path.endsWith('.css')?'text/css':'text/html');
    res.end(await readFile(path==='/'?'/platform-site/platform-admin.html':'/platform-site'+path));
  }catch{res.statusCode=404;res.end();}
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const browser=await chromium.launch({headless:true});
const crossTabBaseline=process.argv.includes('--cross-tab-baseline');
try{
  if(!crossTabBaseline){
  for(const method of ['blob','text','arrayBuffer','formData','json'])for(const signOut of [true,false]){
    const page=await browser.newPage();
    await page.goto('http://127.0.0.1:'+server.address().port+'/');
    await page.locator('#appShell').waitFor({state:'visible'});
    const reading=page.evaluate(async method=>{
      const response=await window.platformAdminApi('/api/test-private?method='+method,{responseType:'raw'});
      window.privateBodyStarted=true;
      try{
        let value=await response[method]();
        if(value instanceof Blob)value=await value.text();
        if(value instanceof ArrayBuffer)value=new TextDecoder().decode(value);
        if(value instanceof FormData)value=value.get('company');
        return {value:typeof value==='object'?value.company:value};
      }catch(error){return {code:error.code};}
    },method);
    await page.waitForFunction(()=>window.privateBodyStarted===true);
    if(signOut){
      await page.locator('#logout').click();
      await page.locator('#appShell').waitFor({state:'hidden'});
      await page.locator('#loginForm').waitFor({state:'visible'});
    }
    assert.ok(pending.has(method));
    pending.get(method).end(method==='formData'?'original':'"original"}');pending.delete(method);
    const result=await reading;
    if(signOut)assert.equal(result.code,'STALE_PLATFORM_RESPONSE');
    else{assert.equal(result.code,undefined);assert.match(result.value,/original/);}
    console.log('PASS actual platform logout/native Response.'+method+' '+(signOut?'rejects old body':'preserves current body'));
    await page.close();
  }
  const page=await browser.newPage();
  await page.goto('http://127.0.0.1:'+server.address().port+'/');
  await page.locator('#appShell').waitFor({state:'visible'});
  await page.evaluate(()=>{
    const old=document.createElement('div');old.id='old-admin-private-marker';
    old.textContent='Previous administrator workspace';document.querySelector('#appShell').append(old);
    window.previousAdminCache='old private cache';
  });
  await page.locator('#logout').click();
  await page.locator('#loginForm').waitFor({state:'visible'});
  await page.locator('#loginForm [name="email"]').fill('synthetic@example.invalid');
  await page.locator('#loginForm [name="password"]').fill('SyntheticFixtureOnly-123!');
  await Promise.all([page.waitForEvent('load'),page.locator('#loginForm button[type="submit"]').click()]);
  await page.locator('#appShell').waitFor({state:'visible'});
  assert.equal(await page.locator('#old-admin-private-marker').count(),0);
  assert.equal(await page.evaluate(()=>window.previousAdminCache),undefined);
  assert.ok(authenticatedCsrfReads>0,'Reload must fetch a fresh CSRF token under the authenticated identity');
  console.log('PASS successful platform sign-in replaces previous administrator DOM and JavaScript cache');
  await page.close();
  }
  for(const storageFallback of [false,true])for(const action of crossTabBaseline?['logout']:['logout','login']){
    const context=await browser.newContext();
    if(storageFallback)await context.addInitScript(()=>{window.BroadcastChannel=undefined;});
    const first=await context.newPage(),second=await context.newPage();
    const origin='http://127.0.0.1:'+server.address().port+'/';
    await Promise.all([first.goto(origin),second.goto(origin)]);
    await first.locator('#appShell').waitFor({state:'visible'});
    await second.locator('#appShell').waitFor({state:'visible'});
    if(action==='login'){
      await first.locator('#logout').click();
      await first.locator('#loginForm').waitFor({state:'visible'});
      await second.reload();
      await second.locator('#appShell').waitFor({state:'visible'});
    }
    const reading=second.evaluate(async()=>{
      const response=await window.platformAdminApi('/api/test-private?method=json',{responseType:'raw'});
      window.privateBodyStarted=true;
      try{return {value:(await response.json()).company};}catch(error){return {code:error.code};}
    });
    await second.waitForFunction(()=>window.privateBodyStarted===true);
    if(action==='logout'){
      await first.locator('#logout').click();
      await first.locator('#appShell').waitFor({state:'hidden'});
    }else{
      await first.locator('#loginForm [name="email"]').fill('next-admin@example.invalid');
      await first.locator('#loginForm [name="password"]').fill('SyntheticFixtureOnly-456!');
      await Promise.all([first.waitForEvent('load'),first.locator('#loginForm button[type="submit"]').click()]);
      await first.locator('#appShell').waitFor({state:'visible'});
    }
    if(!crossTabBaseline)await second.locator('#appShell').waitFor({state:'hidden'});
    pending.get('json').end('"previous-admin"}');pending.delete('json');
    const result=await reading;
    if(crossTabBaseline){
      assert.equal(result.value,'previous-admin');
      assert.equal(await second.locator('#appShell').isVisible(),true);
      console.log('BASELINE_OTHER_PLATFORM_TAB_ACCEPTS_PRIVATE_BODY_AFTER_LOGOUT '+(storageFallback?'storage':'broadcast'));
    }else{
      assert.equal(result.code,'STALE_PLATFORM_RESPONSE');
      await second.locator('#loginForm').waitFor({state:'visible'});
      console.log('PASS another platform tab rejects old private body after actual '+action+' '+(storageFallback?'storage fallback':'BroadcastChannel'));
    }
    await context.close();
  }
  console.log(crossTabBaseline?'PLATFORM_CROSS_TAB_BASELINE 2/2 reproduced':'PLATFORM_SESSION_ISOLATION_BROWSER 15/15 passed');
}finally{
  for(const response of pending.values())response.end();
  await browser.close();await new Promise(resolve=>server.close(resolve));
}
