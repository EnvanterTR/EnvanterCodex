import csv, datetime, json, pathlib, subprocess

# Read only the accepted private clone; never select account/token values.
stage = pathlib.Path('/var/lib/envantertr-staging/eae37c7-20261030')
assert (stage / 'acceptance-probes.exit').read_text().strip() == '0'
container = 'envtr-stage-db-6198ef6'
database = 'envantertr_stage_eae37c7'
environment = dict(item.split('=', 1) for item in json.loads(subprocess.check_output(
    ['docker', 'inspect', container]))[0]['Config']['Env'])

def query(sql):
    return subprocess.check_output(['docker', 'exec', container, 'psql', '-X', '-v', 'ON_ERROR_STOP=1',
        '-At', '-U', environment['POSTGRES_USER'], '-d', database, '-c', sql], text=True).strip()

def identifier(value):
    return '"' + value.replace('"', '""') + '"'

metadata = json.loads((stage / 'legacy-date-coverage.private.json').read_text())
destination = stage / 'legacy-created-dates-input.private.csv'
assert not destination.exists(), 'Do not overwrite user-supplied date input'
rows = []
for table in metadata:
    if 'CreatedAt' not in table['missing'] or table['rows'] == 0:
        continue
    name = table['table']
    # Primary keys come from PostgreSQL, including composite Identity keys.
    key_sql = """SELECT a.attname FROM pg_index i
        JOIN pg_class c ON c.oid=i.indrelid JOIN pg_namespace n ON n.oid=c.relnamespace
        CROSS JOIN LATERAL unnest(i.indkey) WITH ORDINALITY AS k(attnum, position)
        JOIN pg_attribute a ON a.attrelid=c.oid AND a.attnum=k.attnum
        WHERE i.indisprimary AND n.nspname='public' AND c.relname='%s' ORDER BY k.position""" % name.replace("'", "''")
    keys = query(key_sql).splitlines()
    assert keys, 'Missing primary key for ' + name
    selected = list(dict.fromkeys(keys + (['CompanyId'] if query(
        "SELECT count(*) FROM information_schema.columns WHERE table_schema='public' AND table_name='%s' AND column_name='CompanyId'" % name.replace("'", "''")) == '1' else [])))
    records = query('SELECT row_to_json(t)::text FROM (SELECT ' + ','.join(map(identifier, selected)) +
        ' FROM public.' + identifier(name) + ') t ORDER BY row_to_json(t)::text COLLATE "C"')
    for line in records.splitlines():
        record = json.loads(line)
        rows.append({'Table': name, 'PrimaryKeyJson': json.dumps({key: record[key] for key in keys}, separators=(',', ':')),
            'CompanyId': record.get('CompanyId') or '', 'CreatedAtUtc': '', 'SourceReference': ''})

assert len(rows) == sum(table['rows'] for table in metadata if 'CreatedAt' in table['missing'])
with destination.open('x', encoding='utf-8-sig', newline='') as output:
    destination.chmod(0o600)
    writer = csv.DictWriter(output, fieldnames=['Table', 'PrimaryKeyJson', 'CompanyId', 'CreatedAtUtc', 'SourceReference'])
    writer.writeheader()
    writer.writerows(rows)
print('LEGACY_DATE_INPUT_READY', len(rows), 'records; empty dates; primary keys only; no credential/token values')
print('Private file:', destination)
print('No database rows, dates, schema or production state were changed.')
