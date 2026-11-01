import hashlib,json,pathlib,subprocess
stage=pathlib.Path('/var/lib/envantertr-staging/5018bc5-20261026')
sql=stage/'tests/reports/handover-email-121-to-122-20261026.sql'
assert hashlib.sha256(sql.read_bytes()).hexdigest()=='01c04a11378e0e92d011a35bfd0d4762c2bcb9ababc0fa610b2f7116d753a2c0'
bundle=pathlib.Path('/var/backups/envantertr/rollback-20261026T092057Z')
assert bundle.is_dir() and not bundle.is_symlink()
subprocess.run(['sha256sum','-c',str(bundle/'SHA256SUMS')],check=True,stdout=subprocess.DEVNULL)
c='database-izrckbfan6xop8b6xgigduvt-20261026T014656'
e=dict(x.split('=',1) for x in json.loads(subprocess.check_output(['docker','inspect',c]))[0]['Config']['Env'])
def q(s): return subprocess.check_output(['docker','exec',c,'psql','-X','-At','-v','ON_ERROR_STOP=1','-U',e['POSTGRES_USER'],'-d',e['POSTGRES_DB'],'-c',s],text=True).strip()
assert q('SELECT count(*) FROM "__EFMigrationsHistory"')=='121'
subprocess.run(['docker','exec','-i','-e','PGOPTIONS=-c lock_timeout=5s -c statement_timeout=60s',c,'psql','-X','-v','ON_ERROR_STOP=1','-U',e['POSTGRES_USER'],'-d',e['POSTGRES_DB']],input=sql.read_bytes(),check=True,stdout=subprocess.DEVNULL)
assert q('SELECT count(*) FROM "__EFMigrationsHistory"')=='122'
assert q('SELECT count(*) FROM "EmployeeHandoverEmailAttempts"')=='0'
print('PRODUCTION_ADDITIVE_MIGRATION_122_PASS; new delivery ledger empty; no email sent')
