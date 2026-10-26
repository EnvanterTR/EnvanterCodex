import json, os, pathlib, re, shutil, subprocess
stage=pathlib.Path('/var/lib/envantertr-staging/c22ce1c-20261026')
assert stage.is_dir() and not stage.is_symlink()
def inspect(kind,name):
    return json.loads(subprocess.check_output(['docker',kind,'inspect',name]))[0]
old=inspect('container','envtr-stage-web-124b2d1')
env=dict(item.split('=',1) for item in old['Config']['Env'])
connection=env['ConnectionStrings__EnvanterTR']
assert re.search(r'(?i)Host=envtr-stage-db-6198ef6(?:;|$)',connection)
connection,n=re.subn(r'(?i)(Database|Initial Catalog)=[^;]*','Database=envantertr_stage_c22ce1c',connection)
assert n==1
env['ConnectionStrings__EnvanterTR']=connection
for key in list(env):
    if key.startswith('Iyzipay__') or key.startswith('Setup__'):
        env[key]=''
env.update({'Email__Smtp__Host':'','Email__Smtp__Username':'','Email__Smtp__Password':'','CloudTrials__Enabled':'false','Platform__CrmFollowUpEmailRemindersEnabled':'false'})
assert inspect('network','envtr-stage-ae875b6')['Internal'] is True
production=inspect('container','web-izrckbfan6xop8b6xgigduvt-20261025T160415')
for destination,volume,local in [('/var/lib/envantertr/company-media','company-media-data','runtime-media'),('/var/lib/envantertr/data-protection-keys','data-protection-keys-data','runtime-keys')]:
    source=pathlib.Path(next(m['Source'] for m in production['Mounts'] if m['Destination']==destination))
    assert str(source)=='/var/lib/docker/volumes/izrckbfan6xop8b6xgigduvt_'+volume+'/_data'
    target=stage/local
    assert not target.exists()
    subprocess.run(['cp','-a',str(source),str(target)],check=True)
output=stage/'runtime.private.env'
assert not output.exists()
assert all(chr(10) not in value and chr(13) not in value for value in env.values())
output.write_text(''.join(key+'='+value+chr(10) for key,value in env.items()))
os.chmod(output,0o600)
print('C22_ISOLATED_ENV_MEDIA_KEYS_READY; SMTP/payment/bootstrap disabled; internal network verified')
