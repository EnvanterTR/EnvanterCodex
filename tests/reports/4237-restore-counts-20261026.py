import json,subprocess

def counts(container,database=None):
 c=json.loads(subprocess.check_output(['docker','inspect',container]))[0]
 env=dict(x.split('=',1) for x in c['Config']['Env'])
 user=env['POSTGRES_USER']; database=database or env['POSTGRES_DB']
 def query(sql):
  return subprocess.check_output(['docker','exec',container,'psql','-X','-v','ON_ERROR_STOP=1','-U',user,'-d',database,'-At','-c',sql],text=True).strip()
 tables=query("SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename").splitlines()
 assert len(tables)==95,len(tables)
 sql=' UNION ALL '.join("SELECT '"+t.replace("'","''")+"', count(*) FROM public.\""+t.replace('"','""')+"\"" for t in tables)
 return query(sql)
production=counts('database-izrckbfan6xop8b6xgigduvt-20261025T160415')
restored=counts('envtr-stage-db-6198ef6','envantertr_stage_c22ce1c')
assert production==restored,'Table count mismatch; retain private details, investigate before release'
print('FRESH_RESTORE_95_PUBLIC_TABLE_COUNTS_MATCH_PRODUCTION')
