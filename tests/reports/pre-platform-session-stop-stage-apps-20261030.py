import datetime,json,pathlib,re,subprocess
root=pathlib.Path('/var/lib/envantertr-staging')
containers=json.loads(subprocess.check_output(['docker','inspect',*subprocess.check_output(['docker','ps','-q'],text=True).split()]))
production={c['Name']:c['Id'] for c in containers if '-izrckbfan6xop8b6xgigduvt-' in c['Name']}
assert len(production)==3
rows=[]
for c in containers:
 name=c['Name'].lstrip('/')
 if name not in ['envtr-stage-web-f12dd9b','envtr-stage-public-f12dd9b']: continue
 assert c['Config']['Image'].startswith(('envantertr-staging:','envantertr-staging-public:','envantertr-stage-public:')),(name,c['Config']['Image'])
 assert not c['HostConfig']['PortBindings'],name
 assert set(c['NetworkSettings']['Networks'])=={'envtr-stage-ae875b6'},name
 rows.append({'name':name,'id':c['Id'],'image':c['Config']['Image']})
report={'time':datetime.datetime.now(datetime.timezone.utc).isoformat(),'stopped':rows,'production':production,'before':subprocess.check_output(['free','-m'],text=True)}
path=root/'pre-platform-session-stage-stop-20261030.json';assert not path.exists();path.write_text(json.dumps(report,indent=2));path.chmod(0o600)
if rows: subprocess.run(['docker','stop','--time','15',*[r['name'] for r in rows]],check=True,timeout=50,stdout=subprocess.DEVNULL)
for r in rows:
 c=json.loads(subprocess.check_output(['docker','inspect',r['name']]))[0]
 assert not c['State']['Running'] and c['Id']==r['id']
for name,identity in production.items():
 c=json.loads(subprocess.check_output(['docker','inspect',identity]))[0]
 assert c['State']['Running'] and c['Name']==name
report['after']=subprocess.check_output(['free','-m'],text=True);report['passed']=True
path.write_text(json.dumps(report,indent=2));path.chmod(0o600)
print('OBSOLETE_STAGE_STOP_PASS',len(rows),'test application containers stopped; production identities unchanged; no container/volume/file deletion')
print(report['after'])
print('Evidence',path,'; recovery: docker start with the saved stopped names')
