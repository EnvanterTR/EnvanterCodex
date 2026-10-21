import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {chromium} from 'playwright';
const html=await readFile('/public-site/trial.html'),script=await readFile('/public-site/trial.js');
let available=false,fail=false,unsafe=false,release,ready;
const writes=[],urls=[];
const planId='11111111-1111-4111-8111-111111111111';
let base;
const server=createServer(async(req,res)=>{
  urls.push(req.url);const path=new URL(req.url,'http://localhost').pathname;
  const json=(value,status=200)=>{res.writeHead(status,{'content-type':'application/json'});res.end(JSON.stringify(value));};
  if(path==='/trial.html'){res.writeHead(200,{'content-type':'text/html'});res.end(html);return;}
  if(path==='/trial.js'){res.writeHead(200,{'content-type':'application/javascript'});res.end(script);return;}
  if(path==='/api/trials/status')return json({available,noticeVersion:'cloud-trial-data-v1'});
  if(path==='/api/plans')return json([{id:planId,name:'Bireysel',deploymentKind:'cloud',maxLoginUsers:1,monthlyPrice:0,annualPrice:2400,currency:'TRY'}]);
  if(path==='/api/session/csrf')return json({requestToken:'trial-csrf'});
  if(req.method==='POST'){
    let raw='';for await(const chunk of req)raw+=chunk;
    writes.push({path,body:JSON.parse(raw),csrf:req.headers['x-csrf-token']});
    if(path==='/api/trials/request'){
      await new Promise(resolve=>{release=resolve;ready();});
      return json({},fail?503:202);
    }
    if(path==='/api/trials/confirm')return json({portalUrl:unsafe?'https://attacker.example/':'https://farma.localhost/',setupUrl:`${base}/reset-password.html#email=synthetic&token=setup-test`,endsAt:'2026-11-05T12:00:00Z'});
  }
  res.writeHead(404);res.end();
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));base=`http://localhost:${server.address().port}`;
const browser=await chromium.launch({headless:true,args:['--no-sandbox']});
try{
  const page=await browser.newPage();const errors=[];page.on('pageerror',error=>errors.push(error.message));
  await page.goto(`${base}/trial.html`);await page.getByText('Deneme kaydı şu anda kullanılamıyor. Daha sonra yeniden deneyin.',{exact:true}).waitFor();
  assert.equal(await page.locator('[name="email"]').isDisabled(),true);assert.equal(writes.length,0);
  available=true;await page.reload();await page.waitForFunction(()=>!document.querySelector('#fields').disabled);
  for(const [name,slug] of [['Farma A.Ş.','farma'],['Firma Farma A.Ş.','farma'],['Santa','santa'],['Celal','celal']]){await page.locator('[name="companyName"]').fill(name);assert.equal(await page.locator('[name="slug"]').inputValue(),slug);}
  await page.locator('[name="slug"]').fill('custom-portal');await page.locator('[name="companyName"]').fill('Yeni şirket');assert.equal(await page.locator('[name="slug"]').inputValue(),'custom-portal');
  await page.locator('[name="ownerName"]').fill('Synthetic Owner');await page.locator('[name="email"]').fill('owner@example.invalid');await page.locator('[name="consent"]').check();
  const received=new Promise(resolve=>{ready=resolve;});await page.locator('#requestTrial').click();await received;
  await page.locator('#registration').dispatchEvent('submit');assert.equal(writes.length,1);assert.equal(writes[0].csrf,'trial-csrf');assert.equal(writes[0].body.noticeVersion,'cloud-trial-data-v1');assert.equal(writes[0].body.slug,'custom-portal');assert.equal(await page.locator('#requestTrial').isDisabled(),true);release();
  await page.getByText('Doğrulama bağlantısı gönderildi. E-postanızı kontrol edin; bağlantı 30 dakika geçerlidir.',{exact:true}).waitFor();
  for(const language of ['tr','en','ru','uk','mk']){await page.locator('#language').selectOption(language);assert.equal(await page.locator('html').getAttribute('lang'),language);assert.ok((await page.locator('h1').innerText()).length>5);assert.ok(!(await page.locator('body').innerText()).includes('undefined'));}
  await page.setViewportSize({width:390,height:844});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
  await page.goto(`${base}/trial.html#token=synthetic-proof`);await page.waitForFunction(()=>!document.querySelector('#confirmTrial').disabled);
  assert.equal(new URL(page.url()).hash,'');assert.equal(writes.length,1,'Confirmation must require an explicit click.');await page.locator('#confirmTrial').click();
  await page.locator('#success').waitFor({state:'visible'});assert.equal(writes[1].body.token,'synthetic-proof');assert.equal(writes[1].csrf,'trial-csrf');assert.equal(await page.locator('#portalLink').getAttribute('href'),'https://farma.localhost/');assert.match(await page.locator('#setupLink').getAttribute('href'),/^http:\/\/localhost:\d+\/reset-password\.html#/);
  assert.ok(urls.every(url=>!url.includes('synthetic-proof')),'Verification proof must never appear in a request URL.');
  await page.locator('#language').selectOption('tr');unsafe=true;await page.goto(`${base}/trial.html#token=second-proof`);await page.waitForFunction(()=>!document.querySelector('#confirmTrial').disabled);await page.locator('#confirmTrial').click();await page.getByText('İşlem tamamlanamadı. Bilgileri ve bağlantınızı kontrol edip yeniden deneyin.',{exact:true}).waitFor();assert.equal(await page.locator('#success').isHidden(),true);assert.deepEqual(errors,[]);
  console.log('PASS: trial registration fails closed, preserves custom slugs, sends one CSRF/consent POST, supports5languages/390px, requires explicit email confirmation, removes fragment proof from address, and rejects external portal links. SyntheticAPI.');
}finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
