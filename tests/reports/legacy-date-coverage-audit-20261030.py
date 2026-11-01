import datetime,json,os,pathlib,subprocess
stage=pathlib.Path('/var/lib/envantertr-staging/eae37c7-20261030')
assert (stage/'acceptance-probes.exit').read_text().strip()=='0'
c='envtr-stage-db-6198ef6';d='envantertr_stage_eae37c7'
e=dict(x.split('=',1) for x in json.loads(subprocess.check_output(['docker','inspect',c]))[0]['Config']['Env'])
def q(sql):return subprocess.check_output(['docker','exec',c,'psql','-X','-v','ON_ERROR_STOP=1','-At','-U',e['POSTGRES_USER'],'-d',d,'-c',sql],text=True).strip()
def quote(s):return '"'+s.replace('"','""')+'"'
cols={}
for row in q("SELECT table_name,column_name FROM information_schema.columns WHERE table_schema='public' AND table_name<>'__EFMigrationsHistory' ORDER BY table_name,ordinal_position").splitlines():
 t,n=row.split('|');cols.setdefault(t,[]).append(n)
assert len(cols)==95
rows=[]
for t,columns in cols.items():
 missing=[f for f in ['CreatedAt','UpdatedAt','DeletedAt'] if f not in columns]
 if not missing:continue
 count=int(q('SELECT count(*) FROM public.'+quote(t)))
 timestamps=[x for x in columns if x.endswith(('At','On'))]
 rows.append({'table':t,'rows':count,'missing':missing,'availableDateColumnNames':timestamps})
p=stage/'legacy-date-coverage.private.json';p.write_text(json.dumps(rows,indent=2));p.chmod(0o600)
print('LEGACY_DATE_COVERAGE_READ_ONLY',datetime.datetime.now(datetime.timezone.utc).isoformat())
for f in ['CreatedAt','UpdatedAt','DeletedAt']:
 affected=[r for r in rows if f in r['missing']]
 populated=[r for r in affected if r['rows']>0]
 print(f,'missingTables',len(affected),'populatedTables',len(populated),'existingRows',sum(r['rows'] for r in populated))
for r in rows:
 if r['rows']>0 and 'CreatedAt' in r['missing']:
  print('CREATED_DATE_ABSENT',r['table'],'rows='+str(r['rows']),'otherDateColumns='+','.join(r['availableDateColumnNames']))
print('No dates, rows, model or schema were changed; private report',p)
