import datetime,json,subprocess

sha='d19cfe549dfab76c2088ed1425d9733456db360e'
resource='izrckbfan6xop8b6xgigduvt'
ids=subprocess.check_output(['docker','ps','-q','--filter','name='+resource],text=True).split()
containers=json.loads(subprocess.check_output(['docker','inspect',*ids]))
for service in ['web','public-site','database']:
 matches=[c for c in containers if c['Name'].lstrip('/').startswith(service+'-'+resource+'-')]
 assert len(matches)==1,(service,len(matches))
 c=matches[0]
 assert c['State']['Running'] and c['RestartCount']==0
 if service=='database':
  assert c['State']['Health']['Status']=='healthy'
 else:
  assert c['Config']['Image'].endswith(':'+sha)
 if service=='web':
  assert c['HostConfig']['ReadonlyRootfs']
  e=dict(x.split('=',1) for x in c['Config']['Env'])
  assert e['Email__Smtp__Host']=='smtp-relay.gmail.com'
  assert e['Email__Smtp__Port']=='587' and e['Email__Smtp__EnableSsl'].lower()=='true'
  assert not e.get('Email__Smtp__Username') and not e.get('Email__Smtp__Password')
  assert e['CloudTrials__Enabled'].lower()=='false'
  assert not e.get('Iyzipay__ApiKey') and not e.get('Iyzipay__SecretKey')
  assert e['Iyzipay__CallbackUrl']=='https://envantertr.com/api/checkout/callback'
 print(service,c['Name'].lstrip('/'),'PASS exact image/health/runtime guards')
print('LIVE_OPERATIONAL_ROLE_RUNTIME_PASS',datetime.datetime.now(datetime.timezone.utc).isoformat())
