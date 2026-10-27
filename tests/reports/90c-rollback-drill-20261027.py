import os,pathlib,re,subprocess
stage=pathlib.Path('/var/lib/envantertr-staging/90cc105-20261027')
resource='izrckbfan6xop8b6xgigduvt'
suffix='20261026T202413'
dump=(stage/'backup-path.txt').read_text().strip()
assert re.fullmatch(r'/var/lib/postgresql/data/envantertr-backups/pre-web-v1-\d{8}T\d{6}Z.dump',dump)
prepare=stage/'rollback-prepare.log'
with prepare.open('w') as log:
 os.chmod(prepare,0o600)
 result=subprocess.run(['bash','deploy/coolify/prepare-rollback-bundle.sh',resource,'web-'+resource+'-'+suffix,
  'public-site-'+resource+'-20261026T202413','database-'+resource+'-20261026T202412',dump],cwd=stage,stdout=log,stderr=subprocess.STDOUT)
assert result.returncode==0,'Rollback preparation failed; inspect private server log'
paths=re.findall(r'^/var/backups/envantertr/rollback-\d{8}T\d{6}Z$',prepare.read_text(),re.M)
assert len(paths)==1,paths
bundle=paths[0]
verify=stage/'rollback-verify.log'
with verify.open('w') as log:
 os.chmod(verify,0o600)
 result=subprocess.run(['bash','deploy/coolify/verify-rollback-bundle.sh',bundle],cwd=stage,stdout=log,stderr=subprocess.STDOUT)
assert result.returncode==0,'Rollback drill failed; inspect private server log'
assert 'ROLLBACK_FILES_AND_IMAGES_RESTORED' in verify.read_text()
(stage/'rollback-bundle-path.txt').write_text(bundle+'\n')
print('ROLLBACK_PREPARE_AND_FILE_IMAGE_RECOVERY_DRILL_EXIT0',bundle)
print(subprocess.check_output(['du','-sh',bundle],text=True).strip())
print('NO_PRODUCTION_RESTORE_PERFORMED')
