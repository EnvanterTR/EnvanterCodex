import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {chromium} from 'playwright';

const baseline=process.env.ENVANTERTR_WORKSPACE_BASELINE==='true';
let documentRequests=0;
const server=createServer(async(req,res)=>{
  const path=new URL(req.url,'http://localhost').pathname;
  res.setHeader('Cache-Control','no-store');
  if(path.startsWith('/api/')){
    res.setHeader('Content-Type','application/json');
    if(path==='/api/status') return res.end(JSON.stringify({status:'ready',migrationCount:122,pendingMigrationCount:0}));
    if(path==='/api/setup/status') return res.end(JSON.stringify({setupRequired:false}));
    if(path==='/api/session/csrf') return res.end(JSON.stringify({requestToken:'fixture'}));
    res.statusCode=401; return res.end('{}');
  }
  try{
    const file=path==='/'?'/checkout-site/company-app.html':'/checkout-site'+path;
    if(path==='/')documentRequests++;
    res.setHeader('Content-Type',path.endsWith('.js')?'text/javascript':path.endsWith('.json')?'application/json':path.endsWith('.css')?'text/css':'text/html');
    res.end(await readFile(file));
  }catch{res.statusCode=404;res.end();}
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const browser=await chromium.launch({headless:true});
try{
  for(const change of ['company','account','role','same-context']){
    const page=await browser.newPage();
    await page.goto('http://127.0.0.1:'+server.address().port+'/');
    await page.waitForFunction(()=>document.body.classList.contains('auth-required'));
    const before=documentRequests;
    await page.evaluate(change=>{
      activeCompanyContextId='original-company';requestCurrentUserId='original-user';sessionRole='owner';
      document.querySelector('#assetRows').innerHTML='<tr id="private-old-record"><td>PRIVATE ORIGINAL COMPANY DATA</td></tr>';
      const session={companyId:change==='company'?'new-company':'original-company',
        userId:change==='account'?'new-user':'original-user',role:change==='role'?'viewer':'owner'};
      api=async path=>path==='/api/status'?{status:'ready'}:path==='/api/setup/status'?{setupRequired:false}:
        path==='/api/session'?session:new Promise(()=>{});
      // Keep subsequent loading pending so a retained document cannot accidentally
      // clear the fixture through an unrelated successful data refresh.
      loadSupplierNameSuggestions=()=>new Promise(()=>{});
      void startApp();
    },change);
    if(baseline || change==='same-context'){
      await page.waitForFunction(()=>!document.body.classList.contains('auth-required'));
      assert.equal(await page.locator('#private-old-record').count(),1);
      assert.equal(documentRequests,before);
      console.log((baseline?'BASELINE_OLD_DOM_REMAINS ':'PASS same authorized context retains document ')+change);
    }else{
      await page.waitForFunction(()=>!document.querySelector('#private-old-record')&&document.body.classList.contains('auth-required'));
      assert.equal(documentRequests,before+1,'one fresh document load');
      console.log('PASS fresh document removes previous workspace on '+change+' change');
    }
    await page.close();
  }
  console.log(baseline?'BASELINE_WORKSPACE_EXPOSURE_REPRODUCED':'WORKSPACE_RESET_BROWSER 4/4 passed');
}finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
