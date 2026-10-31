import assert from 'node:assert/strict';
import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
const lines=[new Date().toISOString()];
const root='https://envantertr.com';
const get=async p=>{const r=await fetch(root+p);assert.equal(r.status,200,p);return r.json();};
assert.equal((await get('/api/plans')).length,8);
assert.equal((await get('/api/checkout/status')).available,false);
assert.equal((await get('/api/trials/status')).available,false);
lines.push('PASS eight plans; real checkout and public trials unavailable');
const base='https://santa.envantertr.com/api/employees/00000000-0000-0000-0000-000000000001/handover-documents/00000000-0000-0000-0000-000000000002';
for(const [suffix,method] of [['pdf','GET'],['email','GET'],['email','POST']]){
 const r=await fetch(base+'/'+suffix,{method});assert.equal(r.status,401);lines.push(`PASS anonymous ${method} /${suffix} 401`);
}
for(const p of ['/product-gallery.css','/product-gallery.js',...['farma-company-portal-preview','account-security-product-preview','asset-label-roll-transparent','asset-label-devices','asset-label-desktop','asset-label-hardware'].map(n=>'/assets/images/'+n+'.png')]){
 const r=await fetch(root+p);assert.equal(r.status,200,p);
 const hash=b=>createHash('sha256').update(b).digest('hex');
 const local=await readFile('src/EnvanterTR.PublicSite/wwwroot'+p).catch(async()=>readFile('src/EnvanterTR.PublicSite'+p));
 assert.equal(hash(Buffer.from(await r.arrayBuffer())),hash(local),p);lines.push('PASS '+p+' SHA256='+hash(local));
}
const app=await fetch('https://santa.envantertr.com/index.html');assert.equal(app.status,200);
assert.equal(await app.text(),await readFile('src/EnvanterTR.Web/wwwroot/index.html','utf8'));lines.push('PASS live company HTML equals candidate');
const style=await readFile('src/EnvanterTR.Web/wwwroot/company-onboarding.css');
for(const host of ['envantertr.com','santa.envantertr.com','farma.envantertr.com']){
 const r=await fetch('https://'+host+'/company-onboarding.css');assert.equal(r.status,200,host);
 assert.match(r.headers.get('content-type'),/text\/css/);
 assert.deepEqual(Buffer.from(await r.arrayBuffer()),style,host+' exact onboarding CSS');
 lines.push('PASS '+host+' onboarding CSS 200 text/css and exact source bytes');
 for(const [suffix,method] of [['','GET'],['/complete','POST']]){
  const denied=await fetch('https://'+host+'/api/company/onboarding'+suffix,{method});
  assert.equal(denied.status,401,host+' '+method+suffix);lines.push('PASS '+host+' anonymous onboarding '+method+suffix+' 401');
 }
}
const resetHtml=await readFile('src/EnvanterTR.Web/wwwroot/reset-password.html','utf8');
for(const host of ['envantertr.com','santa.envantertr.com','farma.envantertr.com']){
 const response=await fetch('https://'+host+'/reset-password.html');
 assert.equal(response.status,200,host+' reset page');
 assert.equal(await response.text(),resetHtml,host+' exact reset HTML');
 lines.push('PASS '+host+' reset page 200 and exact candidate source');
}
for(const host of ['santa.envantertr.com','farma.envantertr.com']){
 for(const path of ['/platform-admin.html','/platform-setup.html','/admin','/platform-admin-members.js','/api/platform/overview']){
  const r=await fetch('https://'+host+path);assert.equal(r.status,404,host+path);
  assert.doesNotMatch(await r.text(),/<html/i);lines.push('PASS '+host+path+' platform boundary404');
 }
 const r=await fetch('https://'+host+'/api/setup/status');assert.equal(r.status,200);
 assert.equal((await r.json()).platformLoginUrl,'https://envantertr.com/platform-admin.html');
 lines.push('PASS '+host+' platform login URL uses root domain');
}
for(const host of ['santa.envantertr.com','farma.envantertr.com']){
 const r=await fetch('https://'+host+'/api/session/platform-reauth',{method:'POST',headers:{'content-type':'application/json'},body:'{}'});
 assert.equal(r.status,404,host+' platform reauth POST');lines.push('PASS '+host+' platform reauth POST404');
}
const rootPlatform=await fetch(root+'/platform-admin.html');assert.equal(rootPlatform.status,200);
assert.match(await rootPlatform.text(),/platformLoginSubmit/);lines.push('PASS apex platform login remains available');
for (const host of ['envantertr.com','www.envantertr.com','santa.envantertr.com','farma.envantertr.com']) {
 for (const path of ['/admin','/admin/','/admin?view=ignored']) {
  const response=await fetch('https://'+host+path,{redirect:'manual'});
  const isTenant=host.startsWith('santa.')||host.startsWith('farma.');
  assert.equal(response.status,isTenant?404:302,host+path);
  assert.equal(response.headers.get('location'),isTenant?null:'/platform-admin.html',host+path+' Location');
  assert.equal(response.headers.get('set-cookie'),null,host+path+' no session creation');
  lines.push('PASS '+host+path+' admin '+response.status+' relative location or tenant denial; no session cookie');
 }
}
await writeFile('tests/reports/attachment-media-live-boundaries-20261029.log',lines.join('\n')+'\n');console.log(lines.join('\n'));
