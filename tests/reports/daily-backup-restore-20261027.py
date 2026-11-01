import datetime, hashlib, json, os, pathlib, re, subprocess

def run(args, **kwargs):
    return subprocess.run(args, check=True, **kwargs)

def inspect(name):
    return json.loads(subprocess.check_output(['docker', 'inspect', name]))[0]

resource = 'izrckbfan6xop8b6xgigduvt'
ids = subprocess.check_output(['docker', 'ps', '-q', '--filter', 'name=database-' + resource + '-'], text=True).split()
assert len(ids) == 1, 'Require one current production database'
production = inspect(ids[0])
assert production['State']['Running'] and production['State']['Health']['Status'] == 'healthy'
pc = production['Name'].lstrip('/')
target = 'envtr-stage-db-6198ef6'
target_info = inspect(target)
assert target_info['State']['Running'] and not target_info['HostConfig']['PortBindings']
assert any(inspect_network['Internal'] for inspect_network in (
    json.loads(subprocess.check_output(['docker', 'network', 'inspect', name]))[0]
    for name in target_info['NetworkSettings']['Networks']))
assert pc != target
target_env = dict(value.split('=', 1) for value in target_info['Config']['Env'])
target_user = target_env['POSTGRES_USER']
clone = 'envantertr_daily_restore_20261027'
stage = pathlib.Path('/var/lib/envantertr-staging/daily-restore-20261027')
assert not stage.exists(), 'Inspect existing restore before repeating'

files = subprocess.check_output(['docker', 'exec', pc, 'sh', '-c',
    'find /var/lib/postgresql/data/envantertr-backups -maxdepth 1 -type f -name "daily-20261027T*.dump"'], text=True).splitlines()
files = sorted(path for path in files if re.fullmatch(
    r'/var/lib/postgresql/data/envantertr-backups/daily-20261027T\d{6}Z\.dump', path))
assert len(files) == 1, 'Require the single observed daily job output'
dump = files[0]
assert '/daily-20261027T0000' in dump, 'Require the observed midnight scheduled run'
run(['docker', 'exec', pc, 'sha256sum', '-c', dump + '.sha256'], stdout=subprocess.DEVNULL)
run(['docker', 'exec', pc, 'pg_restore', '--list', dump], stdout=subprocess.DEVNULL)
checksum = subprocess.check_output(['docker', 'exec', pc, 'sha256sum', dump], text=True).split()[0]

def query(db, sql):
    return subprocess.check_output(['docker', 'exec', target, 'psql', '-X', '-v', 'ON_ERROR_STOP=1',
        '-At', '-U', target_user, '-d', db, '-c', sql], text=True).strip()

assert query(target_env['POSTGRES_DB'], "SELECT count(*) FROM pg_database WHERE datname='" + clone + "'") == '0'
stage.mkdir(mode=0o700)
private_dump = stage / 'scheduled.private.dump'
run(['docker', 'cp', pc + ':' + dump, str(private_dump)], stdout=subprocess.DEVNULL)
os.chmod(private_dump, 0o600)
assert hashlib.sha256(private_dump.read_bytes()).hexdigest() == checksum
run(['docker', 'exec', target, 'createdb', '-U', target_user, clone])
with private_dump.open('rb') as stream:
    run(['docker', 'exec', '-i', target, 'pg_restore', '-U', target_user, '-d', clone,
        '--no-owner', '--no-acl', '--exit-on-error'], stdin=stream, stdout=subprocess.DEVNULL)
assert query(clone, 'SELECT count(*) FROM "__EFMigrationsHistory"') == '122'
assert query(clone, "SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE' AND table_name<>'__EFMigrationsHistory'") == '95'
assert query(clone, 'SELECT count(*) FROM "Companies"') != '0'
assert query(clone, 'SELECT count(*) FROM "AspNetUsers"') != '0'
assert query(clone, 'SELECT count(*) FROM "CompanyMemberships" m LEFT JOIN "Companies" c ON c."Id"=m."CompanyId" LEFT JOIN "AspNetUsers" u ON u."Id"=m."UserId" WHERE c."Id" IS NULL OR u."Id" IS NULL') == '0'
result = {'source': dump, 'targetContainer': target, 'clone': clone, 'schema': 122, 'dataTables': 95,
    'hashVerified': True, 'restoreExit': 0, 'membershipIntegrity': True,
    'checkedAt': datetime.datetime.now(datetime.timezone.utc).isoformat()}
(stage / 'restore-result.json').write_text(json.dumps(result))
print('SCHEDULED_DAILY_DUMP_HASH_LIST_AND_INDEPENDENT_RESTORE_PASS', dump)
print('RESTORED_SCHEMA122_TABLES95_NONEMPTY_COMPANY_IDENTITY_MEMBERSHIP_INTEGRITY_PASS')
print('ISOLATED_RESTORE_TARGET', target, clone)
print('NO_PRODUCTION_RESTORE_OR_CONFIGURATION_CHANGE', result['checkedAt'])
