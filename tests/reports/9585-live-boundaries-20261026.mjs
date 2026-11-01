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
await writeFile('tests/reports/9585-live-boundaries-20261026.log',lines.join('\n')+'\n');console.log(lines.join('\n'));
