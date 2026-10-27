import hashlib,json,pathlib,subprocess
stage=pathlib.Path('/var/lib/envantertr-staging/5d1e261-20261027')
baseline=json.loads((stage/'baseline.private.json').read_text())
c='envtr-stage-db-6198ef6'; db='envantertr_stage_5d1e261'
e=dict(x.split('=',1) for x in json.loads(subprocess.check_output(['docker','inspect',c]))[0]['Config']['Env'])
def q(sql):return subprocess.check_output(['docker','exec',c,'psql','-X','-v','ON_ERROR_STOP=1','-At','-U',e['POSTGRES_USER'],'-d',db,'-c',sql],text=True).strip()
def quote(v):return '"'+v.replace('"','""')+'"'
after={}
for t,cs in baseline['columns'].items():
 sql='COPY (SELECT row_to_json(t)::text FROM (SELECT '+','.join(map(quote,cs))+' FROM public.'+quote(t)+') t ORDER BY row_to_json(t)::text COLLATE "C") TO STDOUT'
 after[t]=hashlib.sha256(q(sql).encode()).hexdigest()
assert len(after)==95 and after==baseline['hashes'],'Restored data changed during stage startup/probes; investigate private clone before release'
assert q('SELECT count(*) FROM "__EFMigrationsHistory"')=='122'
assert not json.loads(subprocess.check_output(['docker','inspect',c]))[0]['HostConfig']['PortBindings']
print('STAGE_STARTUP_AND_PROBES_PRESERVE_ALL_95_DATA_TABLE_FIELD_PROJECTIONS; schema122; no migration applied')
