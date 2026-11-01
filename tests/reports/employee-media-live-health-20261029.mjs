import assert from 'node:assert/strict';
import {writeFile} from 'node:fs/promises';
const lines=[new Date().toISOString()];
for(const host of ['envantertr.com','santa.envantertr.com','farma.envantertr.com']){
 const r=await fetch('https://'+host+'/api/status');
 assert.equal(r.status,200,host); const d=await r.json();
 assert.equal(d.status,'ready');assert.equal(d.migrationCount,122);assert.equal(d.pendingMigrationCount,0);
 lines.push(host+' 200 ready 122/0');
}
await writeFile('tests/reports/employee-media-live-health-20261029.log',lines.join('\n')+'\n');console.log(lines.join('\n'));