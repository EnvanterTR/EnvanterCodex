import json,subprocess
c=json.loads(subprocess.check_output(['docker','inspect','envtr-stage-web-8cdb77c']))[0]
e=dict(x.split('=',1) for x in c['Config']['Env'])
assert not c['HostConfig']['PortBindings']
assert c['HostConfig']['ReadonlyRootfs']
assert c['HostConfig']['Memory']==768*1024*1024
assert c['HostConfig']['NanoCpus']==1000000000
assert json.loads(subprocess.check_output(['docker','network','inspect','envtr-stage-ae875b6']))[0]['Internal']
assert 'Host=envtr-stage-db-6198ef6;' in e['ConnectionStrings__EnvanterTR']
assert 'Database=envantertr_stage_8cdb77c' in e['ConnectionStrings__EnvanterTR']
assert all(not v for k,v in e.items() if k.startswith('Iyzipay__') or k.startswith('Setup__'))
assert all(not e.get(k) for k in ['Email__Smtp__Host','Email__Smtp__Username','Email__Smtp__Password'])
assert e['CloudTrials__Enabled']=='false' and e['Platform__CrmFollowUpEmailRemindersEnabled']=='false'
for m in c['Mounts']:
 if m['Destination'].startswith('/var/lib/envantertr/'):
  assert m['Source'].startswith('/var/lib/envantertr-staging/8cdb77c-20261030/runtime-')
print('EXACT_STAGE_RUNTIME_ISOLATION_AND_SIDE_EFFECT_GUARDS_PASS')
