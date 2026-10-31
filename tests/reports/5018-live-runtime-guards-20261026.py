import json,subprocess
c=json.loads(subprocess.check_output(['docker','inspect','web-izrckbfan6xop8b6xgigduvt-20261026T093426']))[0]
e=dict(x.split('=',1) for x in c['Config']['Env'])
assert c['Config']['Image'].endswith(':5018bc595684fe4410a9412755b39b2ada9cde0b')
assert c['State']['Running'] and c['RestartCount']==0
assert e['Email__Smtp__Host']=='smtp-relay.gmail.com' and e['Email__Smtp__Port']=='587' and e['Email__Smtp__EnableSsl'].lower()=='true'
assert not e.get('Email__Smtp__Username') and not e.get('Email__Smtp__Password')
assert e['CloudTrials__Enabled'].lower()=='false'
assert c['HostConfig']['ReadonlyRootfs']
print('LIVE_5018_RUNTIME_PASS; exact image; zero restarts; relay TLS587; no SMTP AUTH; trials closed; read-only filesystem')
