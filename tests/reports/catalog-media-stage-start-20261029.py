import json,pathlib,subprocess
stage=pathlib.Path('/var/lib/envantertr-staging/68f4d88-20261029')
assert (stage/'build.exit').read_text().strip()=='0'
name='envtr-stage-web-68f4d88'
assert not subprocess.check_output(['docker','ps','-aq','--filter','name=^/'+name+'$'],text=True).strip()
assert json.loads(subprocess.check_output(['docker','network','inspect','envtr-stage-ae875b6']))[0]['Internal']
subprocess.run(['docker','run','-d','--name',name,'--network','envtr-stage-ae875b6','--read-only',
 '--tmpfs','/tmp:rw,nosuid,noexec,size=64m','--env-file',str(stage/'runtime.private.env'),
 '--mount','type=bind,src='+str(stage/'runtime-media')+',dst=/var/lib/envantertr/company-media',
 '--mount','type=bind,src='+str(stage/'runtime-keys')+',dst=/var/lib/envantertr/data-protection-keys',
 'envantertr-staging:68f4d88'],check=True,stdout=subprocess.DEVNULL)
web=json.loads(subprocess.check_output(['docker','inspect',name]))[0]
ip=web['NetworkSettings']['Networks']['envtr-stage-ae875b6']['IPAddress']
subprocess.run(['docker','run','-d','--name','envtr-stage-public-68f4d88','--network','envtr-stage-ae875b6',
 '--add-host','web:'+ip,'envantertr-staging-public:68f4d88'],check=True,stdout=subprocess.DEVNULL)
print('EXACT_CATALOG_MEDIA_BOTH_STAGE_BUILDS_EXIT0_AND_CONTAINERS_STARTED')
