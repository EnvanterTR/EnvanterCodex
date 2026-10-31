import assert from 'node:assert/strict';
import {readFile,mkdir} from 'node:fs/promises';
import {chromium} from 'playwright';

// UI/interaction acceptance with synthetic transport. PostgreSQL/API acceptance is a separate suite.
const source=process.env.ONBOARDING_SOURCE_ROOT || '/checkout-site';
const html=await readFile(source+'/index.html','utf8');
await mkdir('/evidence',{recursive:true});
const browser=await chromium.launch({headless:true});
try {
 const page=await browser.newPage({viewport:{width:1440,height:1000}});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 let readyCount=0,completedAt=null,completeCalls=0,failCompletion=false;
 let role='owner';
 const state=()=>({steps:['profile','location','employee','asset','assignment'].map((key,i)=>({key,ready:i<readyCount})),readyCount,ready:readyCount===5,completedAt});
 await page.route('**/*',async route=>{
  const request=route.request(),url=new URL(request.url());
  if(url.hostname!=='setup.example.invalid')return route.abort();
  if(!url.pathname.startsWith('/api/')){
   const path=url.pathname==='/'?'/index.html':url.pathname;
   const body=await readFile(source+path).catch(()=>Buffer.from(''));
   return route.fulfill({contentType:path.endsWith('.css')?'text/css':path.endsWith('.js')?'text/javascript':'text/html',body});
  }
  if(url.pathname==='/api/company/onboarding') return route.fulfill({json:state()});
  if(url.pathname.startsWith('/api/lookups/')) return route.fulfill({json:[]});
  if(url.pathname==='/api/company/onboarding/complete'){
   assert.equal(request.method(),'POST');assert.equal(request.headers()['x-csrf-token'],'synthetic-csrf');completeCalls++;
   if(failCompletion)return route.fulfill({status:409,json:{message:'Kayıtlar değişti. İlerlemeyi yenileyin.'}});
   assert.equal(readyCount,5);completedAt='2026-10-26T12:00:00+00:00';return route.fulfill({json:state()});
  }
  const payload={
   '/api/status':{status:'ready'},'/api/setup/status':{setupRequired:false},'/api/session/csrf':{requestToken:'synthetic-csrf'},
   '/api/session':{companyId:'synthetic-company',name:'Synthetic Owner',companyName:'Setup company',role},
   '/api/company/branding':{name:'Setup company',hasLogo:false},
   '/api/company/profile':{name:'Setup company',slug:'setup',tenantDomain:'example.invalid',contactEmail:'owner@example.invalid',hasLogo:false},
   '/api/company/label-settings':{isConfigured:false,settings:null},'/api/assets/custom-fields':[],
   '/api/appearance/preferences':{language:'tr',theme:'dark'}
  }[url.pathname]??{items:[],entries:[],total:0,page:1,pageSize:50};
  return route.fulfill({json:payload});
 });
 await page.goto('https://setup.example.invalid/');
 const panel=page.locator('#companyOnboardingPanel');
 await page.locator('#onboardingProgress').filter({hasText:'0 / 5'}).waitFor();
 assert.equal(await panel.getAttribute('open'),'');
 assert.equal(await page.locator('#onboardingComplete').isDisabled(),true);
 assert.equal(await panel.locator('[data-onboarding-step]').count(),5);
 await page.screenshot({path:'/evidence/onboarding-desktop-light.png',fullPage:false});
 await page.locator('#appearanceButton').click();
 await page.locator('[data-theme-choice="dark"]').click();
 await page.locator('#appearanceSave').click();
 await page.waitForFunction(()=>document.documentElement.dataset.theme==='dark');
 await page.screenshot({path:'/evidence/onboarding-desktop-dark.png',fullPage:false});
 for(const [key,pageId] of [['profile','companySettingsPage'],['location','directoryPage'],['employee','employeesPage'],['asset','overview'],['assignment','overview']]){
  await panel.locator(`[data-onboarding-step="${key}"] button`).last().click();
  await page.locator('#'+pageId).waitFor({state:'visible'});
  await page.locator('#sidebar [data-page="Genel Bakış"]').click();
  await panel.waitFor({state:'visible'});
 }
 readyCount=3;await page.locator('#onboardingRefresh').click();await page.locator('#onboardingProgress').filter({hasText:'3 / 5'}).waitFor();
 assert.equal(await panel.locator('[data-ready="true"]').count(),3);
 readyCount=5;await page.locator('#onboardingRefresh').click();await page.locator('#onboardingComplete').waitFor();
 await page.waitForFunction(()=>!document.querySelector('#onboardingComplete').disabled);
 failCompletion=true;await page.locator('#onboardingComplete').click();await page.locator('#onboardingFeedback').filter({hasText:'Kayıtlar değişti'}).waitFor();
 assert.equal(completedAt,null);
 failCompletion=false;await page.locator('#onboardingRefresh').click();
 await page.waitForFunction(()=>!document.querySelector('#onboardingComplete').disabled);
 await page.locator('#onboardingComplete').click();await page.locator('#onboardingProgress').filter({hasText:'Kurulum tamamlandı'}).waitFor();
 assert.equal(completeCalls,2);assert.equal(await page.locator('#onboardingComplete').isVisible(),false);
 await page.reload();await page.locator('#onboardingProgress').filter({hasText:'Kurulum tamamlandı'}).waitFor();assert.equal(await panel.getAttribute('open'),null);
 await panel.locator('summary').click();
 await page.setViewportSize({width:390,height:844});
 assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
 await page.screenshot({path:'/evidence/onboarding-mobile-dark.png',fullPage:false});
 role='viewer';await page.reload();await page.locator('body:not(.auth-required)').waitFor();
 assert.equal(await panel.isVisible(),false);
 assert.deepEqual(errors,[]);
 console.log('PASS onboarding UI: five real navigation targets, partial/ready state, rejected completion, CSRF POST, persisted completion display, 390px no overflow, viewer hidden, no JS errors. Synthetic transport; API/PostgreSQL verified separately.');
} finally {await browser.close();}
