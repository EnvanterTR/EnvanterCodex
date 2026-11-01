import hashlib,json,os,pathlib,re,shutil,subprocess,datetime
stage=pathlib.Path('/var/lib/envantertr-staging/2997062-20261029')
assert stage.is_dir() and not stage.is_symlink()
def run(args,**kw): return subprocess.run(args,check=True,**kw)
def inspect(name): return json.loads(subprocess.check_output(['docker','inspect',name]))[0]
prod=inspect('database-izrckbfan6xop8b6xgigduvt-20261028T235342')
penv=dict(x.split('=',1) for x in prod['Config']['Env'])
pc=prod['Name'].lstrip('/'); pu=penv['POSTGRES_USER']; pd=penv['POSTGRES_DB']
sc='envtr-stage-db-6198ef6'; se=dict(x.split('=',1) for x in inspect(sc)['Config']['Env']); su=se['POSTGRES_USER']
clone='envantertr_stage_2997062'
def query(container,user,db,sql):
 return subprocess.check_output(['docker','exec',container,'psql','-X','-v','ON_ERROR_STOP=1','-At','-U',user,'-d',db,'-c',sql],text=True).strip()
assert query(pc,pu,pd,'SELECT count(*) FROM "__EFMigrationsHistory"')=='122'
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
assert len(tables)==95,len(tables)
def quote(v): return '"'+v.replace('"','""')+'"'
def fingerprints():
 result={}
 for t,cs in tables.items():
  sql='COPY (SELECT row_to_json(t)::text FROM (SELECT '+','.join(map(quote,cs))+' FROM public.'+quote(t)+') t ORDER BY row_to_json(t)::text COLLATE "C") TO STDOUT'
  result[t]=hashlib.sha256(query(sc,su,clone,sql).encode()).hexdigest()
 return result
before=fingerprints()
baseline=stage/'baseline.private.json'
baseline.write_text(json.dumps({'columns':tables,'hashes':before})); os.chmod(baseline,0o600)
print('FRESH_RESTORED_SCHEMA_122; 95 original data table field fingerprints recorded privately',flush=True)
old=inspect('envtr-stage-web-56e4083'); env=dict(x.split('=',1) for x in old['Config']['Env'])
assert 'Host=envtr-stage-db-6198ef6;' in env['ConnectionStrings__EnvanterTR']
env['ConnectionStrings__EnvanterTR'],n=re.subn(r'(?i)(Database|Initial Catalog)=[^;]*','Database='+clone,env['ConnectionStrings__EnvanterTR']); assert n==1
for k in env:
 if k.startswith('Iyzipay__') or k.startswith('Setup__'): env[k]=''
env.update({'Email__Smtp__Host':'','Email__Smtp__Username':'','Email__Smtp__Password':'','CloudTrials__Enabled':'false','Platform__CrmFollowUpEmailRemindersEnabled':'false'})
assert inspect('envtr-stage-ae875b6')['Internal'] is True
web=inspect('web-izrckbfan6xop8b6xgigduvt-20261028T235342')
for dest,local in [('/var/lib/envantertr/company-media','runtime-media'),('/var/lib/envantertr/data-protection-keys','runtime-keys')]:
 source=next(m['Source'] for m in web['Mounts'] if m['Destination']==dest)
 assert source.startswith('/var/lib/docker/volumes/izrckbfan6xop8b6xgigduvt_')
 target=stage/local; assert not target.exists(); run(['cp','-a',source,str(target)])
privateenv=stage/'runtime.private.env'; assert not privateenv.exists()
assert all('\n' not in v and '\r' not in v for v in env.values())
privateenv.write_text(''.join(k+'='+v+'\n' for k,v in env.items())); os.chmod(privateenv,0o600)
(stage/'backup-path.txt').write_text(dump+'\n')
print('FRESH_BACKUP_AND_ISOLATED_CLONE_READY '+dump,flush=True)
