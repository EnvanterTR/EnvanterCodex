import datetime, hashlib, json, os, pathlib, re, subprocess

# Run after exact staging acceptance. Full CI must pass before deployment;
# it does not deploy, mutate production records, or restore production.
stage = pathlib.Path('/var/lib/envantertr-staging/f12dd9b-20261030')
resource = 'izrckbfan6xop8b6xgigduvt'
previous = 'd19cfe549dfab76c2088ed1425d9733456db360e'
assert stage.is_dir() and all((stage/(name+'.exit')).read_text().strip() == '0' for name in ['build','prepare','acceptance-probes'])
assert not (stage/'release-recovery-path.txt').exists(), 'Inspect existing release recovery before repeating'
ids = subprocess.check_output(['docker','ps','-q','--filter','name='+resource],text=True).split()
containers = json.loads(subprocess.check_output(['docker','inspect',*ids]))
def service(name):
 matches=[c for c in containers if c['Name'].lstrip('/').startswith(name+'-'+resource+'-')]
 assert len(matches)==1
 assert matches[0]['State']['Running']
 return matches[0]
web, public, database = service('web'), service('public-site'), service('database')
assert all(c['Config']['Image'].endswith(':'+previous) for c in [web,public])
assert database['State']['Health']['Status']=='healthy'
pc=database['Name'].lstrip('/')
env=dict(v.split('=',1) for v in database['Config']['Env'])
user,dbname=env['POSTGRES_USER'],env['POSTGRES_DB']
stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
dump='/var/lib/postgresql/data/envantertr-backups/pre-web-v1-'+stamp+'.dump'
def run(args,**kwargs): return subprocess.run(args,check=True,**kwargs)
run(['docker','exec',pc,'pg_dump','-U',user,'-d',dbname,'--format=custom','--file='+dump])
run(['docker','exec',pc,'pg_restore','--list',dump],stdout=subprocess.DEVNULL)
checksum=subprocess.check_output(['docker','exec',pc,'sha256sum',dump],text=True)
run(['docker','exec','-i',pc,'sh','-c','cat > "$1.sha256"','sh',dump],input=checksum,text=True)
private=stage/('release-'+stamp+'.private.dump')
run(['docker','cp',pc+':'+dump,str(private)]);os.chmod(private,0o600)
assert hashlib.sha256(private.read_bytes()).hexdigest()==checksum.split()[0]
sc='envtr-stage-db-6198ef6'
se=dict(v.split('=',1) for v in json.loads(subprocess.check_output(['docker','inspect',sc]))[0]['Config']['Env'])
clone='envantertr_f12_release_'+stamp.lower()
run(['docker','exec',sc,'createdb','-U',se['POSTGRES_USER'],clone])
with private.open('rb') as stream:
 run(['docker','exec','-i',sc,'pg_restore','-U',se['POSTGRES_USER'],'-d',clone,'--no-owner','--no-acl','--exit-on-error'],stdin=stream,stdout=subprocess.DEVNULL)
def query(sql):
 return subprocess.check_output(['docker','exec',sc,'psql','-X','-v','ON_ERROR_STOP=1','-At','-U',se['POSTGRES_USER'],'-d',clone,'-c',sql],text=True).strip()
assert query('SELECT count(*) FROM "__EFMigrationsHistory"')=='122'
assert query("SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE' AND table_name<>'__EFMigrationsHistory'")=='95'
prepare=stage/('release-rollback-'+stamp+'.private.log')
with prepare.open('w') as log:
 os.chmod(prepare,0o600)
 run(['bash','deploy/coolify/prepare-rollback-bundle.sh',resource,web['Name'].lstrip('/'),public['Name'].lstrip('/'),pc,dump],cwd=stage,stdout=log,stderr=subprocess.STDOUT)
paths=re.findall(r'^/var/backups/envantertr/rollback-\d{8}T\d{6}Z$',prepare.read_text(),re.M)
assert len(paths)==1
bundle=paths[0]
verify=stage/('release-recovery-'+stamp+'.private.log')
with verify.open('w') as log:
 os.chmod(verify,0o600)
 run(['bash','deploy/coolify/verify-rollback-bundle.sh',bundle],cwd=stage,stdout=log,stderr=subprocess.STDOUT)
assert 'ROLLBACK_FILES_AND_IMAGES_RESTORED' in verify.read_text()
(stage/'release-recovery-path.txt').write_text(bundle+'\n'+dump+'\n'+clone+'\n')
print('FRESH_RELEASE_SQL_HASH_LIST_AND_INDEPENDENT_RESTORE_PASS schema122 tables95',dump)
print('CURRENT_D19_SQL_MEDIA_KEYS_IMAGES_RECOVERY_PASS',bundle)
print(subprocess.check_output(['du','-sh',bundle],text=True).strip())
print('NO_PRODUCTION_RESTORE_OR_DEPLOY_PERFORMED')
