import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {execFileSync} from 'node:child_process';
import vm from 'node:vm';

const baseline=process.argv.includes('--baseline');
const html=baseline?execFileSync('git',['show','3b98183bd11e6300da70ca99de385328e47ae11d:src/EnvanterTR.Web/wwwroot/platform-admin.html'],{encoding:'utf8',maxBuffer:8*1024*1024}):
  await readFile('src/EnvanterTR.Web/wwwroot/platform-admin.html','utf8');
const csrfStart=html.indexOf('async function csrf()'),apiStart=html.indexOf('async function api(url,options={})'),apiEnd=html.indexOf('window.platformAdminApi=api',apiStart);
const loginStart=html.indexOf('function showLogin()'),loginEnd=html.indexOf('\n',loginStart);
const guardStart=html.indexOf('function ensurePlatformSession(');
assert.ok(csrfStart>=0&&apiStart>csrfStart&&apiEnd>apiStart&&loginStart>=0);
const source=(guardStart>=0?html.slice(guardStart,csrfStart):'')+html.slice(csrfStart,apiEnd)+html.slice(loginStart,loginEnd);
const deferred=()=>{let resolve;const promise=new Promise(done=>resolve=done);return {promise,resolve};};
const response=(status=200,data={private:'original'})=>({status,ok:status<400,headers:new Headers(),json:async()=>data});
function harness(fetch,reauth=async()=>{}){
  let shows=0,closed=0;
  const elements={'#appShell':{hidden:false},'#loginWrap':{classList:{remove:()=>shows++}}};
  const context=vm.createContext({Headers,fetch,requestPlatformReauthentication:reauth,
    showPlatformMfaSetup:()=>{},checkPlatformLoginReadiness:async()=>{},checkPlatformBootstrapAvailability:async()=>{},
    document:{querySelectorAll:()=>[{close:()=>closed++}]},$:selector=>elements[selector]});
  vm.runInContext(`const state={csrf:'fixture',authGeneration:0};${source}
    this.call=api;this.signOut=showLogin;this.state=state;`,context);
  return {context,call:context.call,signOut:context.signOut,state:context.state,shows:()=>shows,closed:()=>closed};
}
let passed=0;
async function check(name,action){await action();passed++;console.log('PASS '+name);}
if(baseline){
  const gate=deferred(),h=harness(()=>gate.promise),read=h.call('/private');
  h.signOut();gate.resolve(response());assert.equal((await read).private,'original');
  console.log('BASELINE_PLATFORM_PRIVATE_RESPONSE_AFTER_LOGOUT_REPRODUCED');
  const csrfGate=deferred(),writes=[],w=harness(url=>url==='/api/session/csrf'?csrfGate.promise:(writes.push(url),Promise.resolve(response())));
  w.state.csrf=null;const write=w.call('/private-write',{method:'POST',body:'{}'});w.signOut();
  csrfGate.resolve(response(200,{requestToken:'new-token'}));await write;assert.equal(writes.length,1);
  console.log('BASELINE_PLATFORM_WRITE_AFTER_LOGOUT_DURING_CSRF_REPRODUCED');
}else{
  await check('drop successful platform response after actual showLogin',async()=>{
    const gate=deferred(),h=harness(()=>gate.promise),read=h.call('/private');h.signOut();gate.resolve(response());
    await assert.rejects(read,{code:'STALE_PLATFORM_RESPONSE'});assert.equal(h.shows(),1);assert.equal(h.closed(),1);
  });
  await check('drop platform JSON body completing after logout',async()=>{
    const body=deferred(),started=deferred(),r=response();r.json=()=>{started.resolve();return body.promise;};
    const h=harness(async()=>r),read=h.call('/private');await started.promise;h.signOut();body.resolve({private:'original'});
    await assert.rejects(read,{code:'STALE_PLATFORM_RESPONSE'});
  });
  for(const status of [401,403]){
    await check('stale '+status+' cannot reopen login over a later session',async()=>{
      const gate=deferred(),h=harness(()=>gate.promise),read=h.call('/private');h.signOut();gate.resolve(response(status));
      await assert.rejects(read,{code:'STALE_PLATFORM_RESPONSE'});assert.equal(h.shows(),1);
    });
    await check('current '+status+' retains status and login behavior',async()=>{
      const h=harness(async()=>response(status,{message:'denied'}));
      await assert.rejects(h.call('/private'),error=>error.status===status);assert.equal(h.shows(),1);
    });
  }
  await check('logout during CSRF prevents token assignment and protected write',async()=>{
    const gate=deferred(),writes=[],h=harness(url=>url==='/api/session/csrf'?gate.promise:(writes.push(url),Promise.resolve(response())));
    h.state.csrf=null;const write=h.call('/private-write',{method:'POST',body:'{}'});h.signOut();gate.resolve(response(200,{requestToken:'late-token'}));
    await assert.rejects(write,{code:'STALE_PLATFORM_RESPONSE'});assert.deepEqual(writes,[]);assert.equal(h.state.csrf,null);
  });
  await check('logout during step-up never resends an old protected action',async()=>{
    const gate=deferred(),started=deferred(),writes=[];
    const h=harness(async url=>{writes.push(url);return response(428,{code:'platform_reauth_required'});},()=>{started.resolve();return gate.promise;});
    const write=h.call('/private-write',{method:'POST',body:'{}'});await started.promise;h.signOut();gate.resolve();
    await assert.rejects(write,{code:'STALE_PLATFORM_RESPONSE'});assert.equal(writes.length,1);
  });
  await check('current step-up retries exactly once and strips custom options',async()=>{
    let requests=0,prompts=0;const h=harness(async(url,options)=>{
      requests++;assert.equal(Object.hasOwn(options,'_skipReauth'),false);assert.equal(Object.hasOwn(options,'responseType'),false);
      return requests===1?response(428,{code:'platform_reauth_required'}):response(200,{saved:true});
    },async()=>{prompts++;});
    assert.equal((await h.call('/private-write',{method:'POST',body:'{}'})).saved,true);assert.equal(requests,2);assert.equal(prompts,1);
  });
  await check('current and stale empty responses remain distinct',async()=>{
    assert.equal(await harness(async()=>response(204)).call('/empty'),null);
    const gate=deferred(),h=harness(()=>gate.promise),read=h.call('/empty');h.signOut();gate.resolve(response(204));
    await assert.rejects(read,{code:'STALE_PLATFORM_RESPONSE'});
  });
  for(const method of ['blob','text','arrayBuffer','formData','json']){
    await check('platform raw '+method+' rejects after logout',async()=>{
      const body=deferred(),r=response();r[method]=()=>body.promise;
      const h=harness(async()=>r),raw=await h.call('/export',{responseType:'raw'}),reading=raw[method]();h.signOut();body.resolve('original');
      await assert.rejects(reading,{code:'STALE_PLATFORM_RESPONSE'});
    });
    await check('current platform raw '+method+' remains readable',async()=>{
      const r=response();r[method]=async()=>'current';const h=harness(async()=>r);
      assert.equal(await (await h.call('/export',{responseType:'raw'}))[method](),'current');
    });
  }
  console.log(`PLATFORM_SESSION_ISOLATION ${passed}/${passed} passed`);
}
