import assert from 'node:assert/strict';
import {readFile,mkdir} from 'node:fs/promises';
import {chromium} from 'playwright';

const html=await readFile(process.env.PASSWORD_RESET_HTML || '/checkout-site/reset-password.html','utf8');
const evidence=process.env.PASSWORD_RESET_EVIDENCE || '/evidence';
await mkdir(evidence,{recursive:true});
const browser=await chromium.launch({headless:true});
try {
 const page=await browser.newPage({viewport:{width:390,height:844}});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const posts=[];
 await page.route('**/*',async route=>{
  const request=route.request(),url=new URL(request.url());
  if(url.hostname!=='reset.example.invalid')return route.abort();
  if(url.pathname==='/reset-password.html')return route.fulfill({contentType:'text/html',body:html});
  if(url.pathname==='/api/session/csrf')return route.fulfill({json:{requestToken:'synthetic-csrf'}});
  if(url.pathname==='/api/session/password/reset'){
   assert.equal(request.method(),'POST');assert.equal(request.headers()['x-csrf-token'],'synthetic-csrf');
   const body=request.postDataJSON();posts.push(body);
   assert.equal(body.email,'invited@example.invalid');assert.equal(body.token,'synthetic-invitation-token');
   if(posts.length===1)return route.fulfill({status:503,json:{message:'Parola yenileme sırasında bir veritabanı sorunu oluştu. Lütfen yeniden deneyin.'}});
   return route.fulfill({json:{message:'Parolanız yenilendi. Yeni parolanızla giriş yapabilirsiniz.'}});
  }
  return route.abort();
 });
 await page.goto('https://reset.example.invalid/reset-password.html#email=invited%40example.invalid&token=synthetic-invitation-token');
 assert.equal(new URL(page.url()).hash,'');assert.equal(new URL(page.url()).search,'');
 await page.locator('[name="password"]').fill('Synthetic.Reset-Retry-42!');
 await page.locator('[name="confirmPassword"]').fill('Synthetic.Reset-Retry-42!');
 await page.locator('button[type="submit"]').click();
 await page.locator('#message').filter({hasText:'veritabanı sorunu'}).waitFor({state:'visible'});
 assert.equal(await page.locator('#resetForm').isVisible(),true);
 assert.equal(await page.locator('button[type="submit"]').isEnabled(),true);
 await page.screenshot({path:evidence+'/reset-retry-error.png'});
 await page.locator('button[type="submit"]').click();
 await page.locator('#message').filter({hasText:'Yeni parolanızla giriş yapabilirsiniz'}).waitFor({state:'visible'});
 assert.equal(posts.length,2);assert.deepEqual(posts[0],posts[1]);
 assert.equal(await page.locator('#resetForm').isVisible(),false);
 assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
 await page.screenshot({path:evidence+'/reset-retry-success.png'});
 await page.reload();
 await page.locator('#message').filter({hasText:'eksik veya geçersiz'}).waitFor({state:'visible'});
 assert.equal(await page.locator('#resetForm').isVisible(),false);assert.equal(posts.length,2);
 await page.screenshot({path:evidence+'/reset-missing-link.png'});
 assert.deepEqual(errors,[]);
 console.log('PASS synthetic reset UI: fragment removed; CSRF; 503 retains form and same token for explicit retry; success and missing-link messages remain visible with hidden form;390px/no JS errors. No real account reset.');
} finally {await browser.close();}
