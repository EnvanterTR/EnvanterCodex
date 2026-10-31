import json,pathlib,subprocess
stage=pathlib.Path('/var/lib/envantertr-staging/eae37c7-20261030')
assert (stage/'build.exit').read_text().strip()=='0'
name='envtr-stage-web-eae37c7'
assert not subprocess.check_output(['docker','ps','-aq','--filter','name=^/'+name+'$'],text=True).strip()
assert json.loads(subprocess.check_output(['docker','network','inspect','envtr-stage-ae875b6']))[0]['Internal']
subprocess.run(['docker','run','-d','--name',name,'--network','envtr-stage-ae875b6','--read-only','--memory','768m','--cpus','1',
 '--tmpfs','/tmp:rw,nosuid,noexec,size=64m','--env-file',str(stage/'runtime.private.env'),
 '--mount','type=bind,src='+str(stage/'runtime-media')+',dst=/var/lib/envantertr/company-media',
 '--mount','type=bind,src='+str(stage/'runtime-keys')+',dst=/var/lib/envantertr/data-protection-keys',
 'envantertr-staging:eae37c7'],check=True,stdout=subprocess.DEVNULL)
web=json.loads(subprocess.check_output(['docker','inspect',name]))[0]
ip=web['NetworkSettings']['Networks']['envtr-stage-ae875b6']['IPAddress']
subprocess.run(['docker','run','-d','--name','envtr-stage-public-eae37c7','--network','envtr-stage-ae875b6',
 '--memory','128m','--cpus','0.25','--add-host','web:'+ip,'envantertr-staging-public:eae37c7'],check=True,stdout=subprocess.DEVNULL)
print('EXACT_SUPPORT_RECIPIENT_BOTH_STAGE_BUILDS_EXIT0_AND_CONTAINERS_STARTED')
