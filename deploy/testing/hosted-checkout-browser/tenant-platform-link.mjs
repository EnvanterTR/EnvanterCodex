import assert from 'node:assert/strict';
import {readFile,mkdir} from 'node:fs/promises';
import {chromium} from 'playwright';

const source=process.env.TENANT_PLATFORM_SOURCE || '/checkout-site';
const evidence=process.env.TENANT_PLATFORM_EVIDENCE || '/evidence';
await mkdir(evidence,{recursive:true});
const browser=await chromium.launch({headless:true});
try {
 for(const [host,target,expected] of [
  ['north.example.invalid','https://example.invalid/platform-admin.html','https://example.invalid/platform-admin.html'],
  ['localhost','/platform-admin.html','https://localhost/platform-admin.html'],
  ['north.example.invalid','https://user:secret@example.invalid/platform-admin.html',null]
 ]) {
  const page=await browser.newPage({viewport:{width:390,height:844}}),errors=[];
  page.on('pageerror',error=>errors.push(error.message));
  await page.route('**/*',async route=>{
   const url=new URL(route.request().url());
   if(url.hostname!==host)return route.abort();
   if(url.pathname==='/api/session/csrf')return route.fulfill({json:{requestToken:'synthetic-csrf'}});
   if(url.pathname==='/api/session')return route.fulfill({status:401,json:{message:'Sign in required'}});
   if(url.pathname==='/api/status')return route.fulfill({json:{status:'ready'}});
   if(url.pathname==='/api/setup/status')return route.fulfill({json:{setupRequired:false,platformLoginUrl:target}});
   if(url.pathname.startsWith('/api/'))return route.fulfill({json:[]});
   const path=url.pathname==='/'?'/index.html':url.pathname;
   const bytes=await readFile(source+path).catch(()=>null);
   return bytes?route.fulfill({body:bytes,contentType:path.endsWith('.js')?'text/javascript':path.endsWith('.css')?'text/css':'text/html'}):route.fulfill({status:404,body:''});
  });
  await page.goto('https://'+host+'/');
  await page.locator('#retryApiButton').waitFor({state:'visible'});
  await page.waitForFunction(()=>!document.querySelector('#retryApiButton').disabled);
  const link=page.locator('#platformLoginLink');
  if(expected){assert.equal(await link.isVisible(),true);assert.equal(await link.getAttribute('href'),expected);}
  else assert.equal(await link.isVisible(),false);
  assert.deepEqual(errors,[]);
  if(host==='north.example.invalid'&&expected)await page.screenshot({path:evidence+'/tenant-platform-root-link.png'});
  await page.close();
 }
 console.log('PASS tenant platform login uses root host, local login stays local, and credential-bearing URLs remain hidden (synthetic transport).');
} finally { await browser.close(); }
