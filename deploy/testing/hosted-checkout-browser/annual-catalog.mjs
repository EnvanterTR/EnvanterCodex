import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {chromium} from 'playwright';

const html=await readFile('/platform-site/platform-admin.html');
const users=[1,4,10,50,100,250,1000,2000], monthly=[200,350,500,750,1000,1200,2000,3000];
const tiers=users.map((users,index)=>({code:`tier-${users}`,name:`Paket ${users}`,users,monthlyEquivalent:monthly[index],annualPrice:monthly[index]*12}));
const writes=[]; let plans=[],release,ready;
const server=createServer(async(req,res)=>{
  const path=new URL(req.url,'http://localhost').pathname;
  const json=(value,status=200)=>{res.writeHead(status,{'content-type':'application/json'});res.end(JSON.stringify(value));};
  if(path==='/'){res.writeHead(200,{'content-type':'text/html'});res.end(html);return;}
  if(path.endsWith('.js')){res.writeHead(200,{'content-type':'application/javascript'});res.end('');return;}
  if(path==='/api/session')return json({platformAdmin:true,name:'Catalog acceptance'});
  if(path==='/api/session/2fa')return json({enabled:true});
  if(path==='/api/session/csrf')return json({requestToken:'catalog-csrf'});
  if(path==='/api/platform/plans/annual-catalog')return json({plans:tiers});
  if(path==='/api/platform/plans/annual-catalog/drafts'){
    let raw='';for await(const chunk of req)raw+=chunk;
    writes.push({body:JSON.parse(raw),csrf:req.headers['x-csrf-token']});
    await new Promise(resolve=>{release=resolve;ready();});
    const created=plans.length?[]:tiers.map(tier=>({id:tier.code,code:tier.code}));
    plans=tiers.map(tier=>({id:tier.code,code:tier.code,name:tier.name,deploymentKind:'cloud',maxLoginUsers:tier.users,annualPrice:tier.annualPrice,monthlyPrice:0,oneTimePrice:0,currency:'TRY',isPublic:false,isActive:true}));
    return json({created,preservedCodes:created.length?[]:tiers.map(tier=>tier.code),published:false});
  }
  if(path==='/api/platform/plans')return json(plans);
  if(path==='/api/platform/overview')return json({companies:{total:0,active:0,suspended:0},subscriptions:{paidActive:0,customerTrials:0,demos:0,expired:0,renewalsDue:0},checkout:{paymentPending:0,paymentReview:0,paymentFailed:0,provisioningFailed:0},recurringRevenue:{currencies:[]},recentPayments:[]});
  return json({items:[],total:0,page:1,pageSize:25});
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const browser=await chromium.launch({headless:true,args:['--no-sandbox']});
try{
  const page=await browser.newPage();const errors=[];page.on('pageerror',error=>errors.push(error.message));
  await page.goto(`http://127.0.0.1:${server.address().port}/`);
  await page.locator('#nav [data-view="plans"]').click();
  await page.locator('#openAnnualCatalog').click();
  await page.waitForFunction(()=>document.querySelectorAll('#annualCatalogPreview tbody tr').length===8);
  assert.match(await page.locator('#annualCatalogPreview').innerText(),/2\.400/);
  await page.locator('#cancelAnnualCatalog').click();assert.equal(writes.length,0);
  await page.locator('#openAnnualCatalog').click();
  await page.locator('#applyAnnualCatalog').waitFor({state:'visible'});
  await page.locator('#annualCatalogForm [name="changeReason"]').fill('Onaylanan yıllık fiyatların hazırlanması');
  const received=new Promise(resolve=>{ready=resolve;});
  await page.locator('#applyAnnualCatalog').click();await received;
  await page.locator('#annualCatalogForm').dispatchEvent('submit');
  assert.equal(writes.length,1);assert.equal(writes[0].csrf,'catalog-csrf');
  assert.equal(writes[0].body.changeReason,'Onaylanan yıllık fiyatların hazırlanması');
  assert.equal(await page.locator('#applyAnnualCatalog').isDisabled(),true);
  release();
  await page.getByText('8 paket taslağı oluşturuldu; 0 mevcut paket korundu. Web sitesinde yayımlanmadı.',{exact:true}).waitFor();
  await page.setViewportSize({width:390,height:844});
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
  await page.locator('#closeAnnualCatalog').click();
  await page.waitForFunction(()=>document.querySelectorAll('#plansTable tbody tr').length===8);
  assert.equal(await page.locator('#plansTable .pill').filter({hasText:'Gizli'}).count(),8);
  assert.deepEqual(errors,[]);
  console.log('PASS: annual catalog preview shows8tiers, cancel writes nothing, explicit reason/CSRF creates once, duplicate submit is blocked, plans refresh as unpublished and390px fits. SyntheticAPI.');
}finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
