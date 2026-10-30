import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {execFileSync} from 'node:child_process';
import vm from 'node:vm';

const baseline = process.argv.includes('--baseline');
const html = baseline ? execFileSync('git', ['show', 'd19cfe549dfab76c2088ed1425d9733456db360e:src/EnvanterTR.Web/wwwroot/index.html'], {encoding:'utf8',maxBuffer:16*1024*1024})
  : await readFile('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const start = html.indexOf('    async function api(path, options = {}) {');
const end = html.indexOf('    async function refreshCsrf()', start);
assert.ok(start >= 0 && end > start, 'Use the actual application API helper');
const source = html.slice(start, end);
const deferred = () => {let resolve; const promise = new Promise(done => resolve = done); return {promise,resolve};};
const response = (status=200, json=async()=>({company:'original'})) => ({status,ok:status<400,
  headers:new Headers({'content-type':'application/json'}),json});
function harness(fetch) {
  const context = vm.createContext({fetch,Headers,AbortController,setTimeout,clearTimeout});
  vm.runInContext(`let authSessionGeneration=0, requestToken='csrf', loginCalls=0;
    const translateUiText=x=>x; const showLogin=()=>loginCalls++; ${source}
    this.call=api; this.changeSession=()=>authSessionGeneration++; this.logins=()=>loginCalls;`, context);
  return context;
}
let passed = 0;
async function check(name, action) {await action(); console.log('PASS '+name); passed++;}

if (baseline) {
  const gate=deferred(), h=harness(()=>gate.promise);
  const pending=h.call('/api/assets'); h.changeSession(); gate.resolve(response());
  assert.equal((await pending).company,'original');
  console.log('BASELINE_REPRODUCED: old successful response reaches the consumer after session change');
  const body=deferred(),raw=response(); raw.blob=()=>body.promise;
  const download=harness(async()=>raw),received=await download.call('/export',{responseType:'raw'});
  const reading=received.blob();download.changeSession();body.resolve('old private export');
  assert.equal(await reading,'old private export');
  console.log('BASELINE_REPRODUCED: old raw export body reaches consumer after session change');
} else {
  await check('reject successful headers from the old session before delivering company data', async()=>{
    const gate=deferred(), h=harness(()=>gate.promise);
    const pending=h.call('/api/assets'); h.changeSession(); gate.resolve(response());
    await assert.rejects(pending,{code:'STALE_SESSION_RESPONSE'}); assert.equal(h.logins(),0);
  });
  await check('reject JSON body finishing after session change',async()=>{
    const body=deferred(), started=deferred(), h=harness(async()=>response(200,()=>{started.resolve();return body.promise;}));
    const pending=h.call('/api/assets'); await started.promise; h.changeSession(); body.resolve({company:'original'});
    await assert.rejects(pending,{code:'STALE_SESSION_RESPONSE'});
  });
  for (const [name,status,options] of [['empty',204,{}],['raw',200,{responseType:'raw'}]])
    await check('reject stale '+name+' response',async()=>{
      const gate=deferred(),h=harness(()=>gate.promise),pending=h.call('/api/assets',options);
      h.changeSession();gate.resolve(response(status));await assert.rejects(pending,{code:'STALE_SESSION_RESPONSE'});
    });
  await check('old unauthorized response never opens login over the new account',async()=>{
    const gate=deferred(),h=harness(()=>gate.promise),pending=h.call('/api/assets');h.changeSession();gate.resolve(response(401));
    await assert.rejects(pending,{code:'STALE_AUTH_REQUIRED'});assert.equal(h.logins(),0);
  });
  await check('current unauthorized response retains login behavior',async()=>{
    const h=harness(async()=>response(401));await assert.rejects(h.call('/api/assets'),{code:'AUTH_REQUIRED'});assert.equal(h.logins(),1);
  });
  await check('reject old error details parsed after session change',async()=>{
    const body=deferred(),started=deferred(),h=harness(async()=>response(403,()=>{started.resolve();return body.promise;}));
    const pending=h.call('/api/assets');await started.promise;h.changeSession();body.resolve({message:'original company detail'});
    await assert.rejects(pending,error=>error.code==='STALE_SESSION_RESPONSE' && error.payload===undefined);
  });
  await check('current JSON, empty and raw responses retain original results',async()=>{
    const data={company:'current'},json=response(200,async()=>data),empty=response(204),raw=response();
    const h=harness(async path=>path==='/json'?json:path==='/empty'?empty:raw);
    assert.equal(await h.call('/json'),data);assert.equal(await h.call('/empty'),null);
    assert.equal(await h.call('/raw',{responseType:'raw'}),raw);
  });
  await check('current error retains status and payload',async()=>{
    const payload={message:'current detail'},h=harness(async()=>response(403,async()=>payload));
    await assert.rejects(h.call('/api/assets'),error=>error.status===403 && error.payload===payload);
  });
  await check('late old request cannot replace a new-session consumer result',async()=>{
    const gate=deferred(),h=harness(path=>path==='/old'?gate.promise:Promise.resolve(response(200,async()=>({company:'new'}))));
    let selected=null;
    const old=h.call('/old').then(data=>selected=data.company);
    h.changeSession();selected=(await h.call('/new')).company;gate.resolve(response());
    await assert.rejects(old,{code:'STALE_SESSION_RESPONSE'});assert.equal(selected,'new');
  });
  for(const method of ['blob','text','arrayBuffer','formData','json']) {
    await check('reject stale raw '+method+' body after headers were accepted',async()=>{
      const body=deferred(),raw=response();raw[method]=()=>body.promise;
      const h=harness(async()=>raw),received=await h.call('/export',{responseType:'raw'});
      const reading=received[method]();h.changeSession();body.resolve('private original export');
      await assert.rejects(reading,{code:'STALE_SESSION_RESPONSE'});
    });
    await check('current raw '+method+' body remains usable',async()=>{
      const value={current:true},raw=response();raw[method]=async()=>value;
      const h=harness(async()=>raw),received=await h.call('/export',{responseType:'raw'});
      assert.equal(await received[method](),value);
    });
  }
  console.log(`SESSION_RESPONSE_ISOLATION ${passed}/${passed} passed`);
}
