import hashlib,json,os,pathlib,re,shutil,subprocess,datetime
stage=pathlib.Path('/var/lib/envantertr-staging/5018bc5-20261026')
assert stage.is_dir() and not stage.is_symlink()
def run(args,**kw): return subprocess.run(args,check=True,**kw)
def inspect(name): return json.loads(subprocess.check_output(['docker','inspect',name]))[0]
prod=inspect('database-izrckbfan6xop8b6xgigduvt-20261026T014656')
penv=dict(x.split('=',1) for x in prod['Config']['Env'])
pc=prod['Name'].lstrip('/'); pu=penv['POSTGRES_USER']; pd=penv['POSTGRES_DB']
sc='envtr-stage-db-6198ef6'; se=dict(x.split('=',1) for x in inspect(sc)['Config']['Env']); su=se['POSTGRES_USER']
clone='envantertr_stage_5018bc5'
def query(container,user,db,sql):
 return subprocess.check_output(['docker','exec',container,'psql','-X','-v','ON_ERROR_STOP=1','-At','-U',user,'-d',db,'-c',sql],text=True).strip()
assert query(pc,pu,pd,'SELECT count(*) FROM "__EFMigrationsHistory"')=='121'
assert query(sc,su,se['POSTGRES_DB'],"SELECT count(*) FROM pg_database WHERE datname='"+clone+"'")=='0'
ts=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
dump='/var/lib/postgresql/data/envantertr-backups/pre-web-v1-'+ts+'.dump'
run(['docker','exec',pc,'pg_dump','-U',pu,'-d',pd,'--format=custom','--file='+dump])
run(['docker','exec',pc,'pg_restore','--list',dump],stdout=subprocess.DEVNULL)
checksum=subprocess.check_output(['docker','exec',pc,'sha256sum',dump],text=True)
run(['docker','exec','-i',pc,'sh','-c','cat > "$1.sha256"','sh',dump],input=checksum,text=True)
private=stage/'restore.private.dump'; run(['docker','cp',pc+':'+dump,str(private)]); os.chmod(private,0o600)
assert hashlib.sha256(private.read_bytes()).hexdigest()==checksum.split()[0]
run(['docker','exec',sc,'createdb','-U',su,clone])
run(['docker','exec','-i',sc,'pg_restore','-U',su,'-d',clone,'--no-owner','--no-acl','--exit-on-error'],stdin=private.open('rb'),stdout=subprocess.DEVNULL)
columns=query(sc,su,clone,"SELECT table_name,column_name FROM information_schema.columns WHERE table_schema='public' AND table_name<>'__EFMigrationsHistory' ORDER BY table_name,ordinal_position")
tables={}
for row in columns.splitlines():
 t,c=row.split('|'); tables.setdefault(t,[]).append(c)
assert len(tables)==94,len(tables)
def quote(v): return '"'+v.replace('"','""')+'"'
def fingerprints():
 result={}
 for t,cs in tables.items():
  sql='COPY (SELECT row_to_json(t)::text FROM (SELECT '+','.join(map(quote,cs))+' FROM public.'+quote(t)+') t ORDER BY row_to_json(t)::text COLLATE "C") TO STDOUT'
  result[t]=hashlib.sha256(query(sc,su,clone,sql).encode()).hexdigest()
 return result
before=fingerprints()
sqlfile=stage/'tests/reports/handover-email-121-to-122-20261026.sql'
assert hashlib.sha256(sqlfile.read_bytes()).hexdigest()=='01c04a11378e0e92d011a35bfd0d4762c2bcb9ababc0fa610b2f7116d753a2c0'
sql=sqlfile.read_text()
assert all(x not in sql.upper() for x in ['DROP TABLE','DROP COLUMN','DELETE FROM'])
for attempt in [1,2]:
 run(['docker','exec','-i',sc,'psql','-X','-v','ON_ERROR_STOP=1','-U',su,'-d',clone],input=sql,text=True,stdout=subprocess.DEVNULL)
 assert fingerprints()==before,'Original field projection mismatch'
 assert query(sc,su,clone,'SELECT count(*) FROM "__EFMigrationsHistory"')=='122'
 print('CLONE_MIGRATION_'+str(attempt)+'_PASS; 94 original table field projections unchanged',flush=True)
old=inspect('envtr-stage-web-4237b57'); env=dict(x.split('=',1) for x in old['Config']['Env'])
assert 'Host=envtr-stage-db-6198ef6;' in env['ConnectionStrings__EnvanterTR']
env['ConnectionStrings__EnvanterTR'],n=re.subn(r'(?i)(Database|Initial Catalog)=[^;]*','Database='+clone,env['ConnectionStrings__EnvanterTR']); assert n==1
for k in env:
 if k.startswith('Iyzipay__') or k.startswith('Setup__'): env[k]=''
env.update({'Email__Smtp__Host':'','Email__Smtp__Username':'','Email__Smtp__Password':'','CloudTrials__Enabled':'false','Platform__CrmFollowUpEmailRemindersEnabled':'false'})
assert inspect('envtr-stage-ae875b6')['Internal'] is True
web=inspect('web-izrckbfan6xop8b6xgigduvt-20261026T014656')
for dest,local in [('/var/lib/envantertr/company-media','runtime-media'),('/var/lib/envantertr/data-protection-keys','runtime-keys')]:
 source=next(m['Source'] for m in web['Mounts'] if m['Destination']==dest)
 assert source.startswith('/var/lib/docker/volumes/izrckbfan6xop8b6xgigduvt_')
 target=stage/local; assert not target.exists(); run(['cp','-a',source,str(target)])
privateenv=stage/'runtime.private.env'; assert not privateenv.exists()
assert all('\n' not in v and '\r' not in v for v in env.values())
privateenv.write_text(''.join(k+'='+v+'\n' for k,v in env.items())); os.chmod(privateenv,0o600)
(stage/'backup-path.txt').write_text(dump+'\n')
print('FRESH_BACKUP_AND_ISOLATED_CLONE_READY '+dump,flush=True)
