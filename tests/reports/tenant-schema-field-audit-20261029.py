import csv,datetime,io,json,os,pathlib,subprocess
stage=pathlib.Path('/var/lib/envantertr-staging/68f4d88-20261029')
assert (stage/'acceptance-probes.exit').read_text().strip()=='0'
container='envtr-stage-db-6198ef6'
info=json.loads(subprocess.check_output(['docker','inspect',container]))[0]
env=dict(x.split('=',1) for x in info['Config']['Env'])
sql="""SELECT table_name,bool_or(column_name='CompanyId'),bool_or(column_name='CreatedAt'),bool_or(column_name='UpdatedAt'),bool_or(column_name='DeletedAt') FROM information_schema.columns WHERE table_schema='public' AND table_name<>'__EFMigrationsHistory' GROUP BY table_name ORDER BY table_name"""
output=subprocess.check_output(['docker','exec',container,'psql','-X','-v','ON_ERROR_STOP=1','-At','-U',env['POSTGRES_USER'],'-d','envantertr_stage_68f4d88','-c',sql],text=True)
rows=[line.split('|') for line in output.splitlines()]
assert len(rows)==95,len(rows)
report=io.StringIO(); writer=csv.writer(report);writer.writerow(['Table','CompanyId','CreatedAt','UpdatedAt','DeletedAt']);writer.writerows(rows)
path=stage/'schema-fields-audit.csv';assert not path.is_symlink();path.write_text(report.getvalue());os.chmod(path,0o600)
print('SCHEMA_FIELD_AUDIT',datetime.datetime.now(datetime.timezone.utc).isoformat(),'TABLES',len(rows))
for index,field in enumerate(['CompanyId','CreatedAt','UpdatedAt','DeletedAt'],1):
 missing=[row[0] for row in rows if row[index]!='t']
 print(field,'PRESENT',len(rows)-len(missing),'MISSING',len(missing))
 print('MISSING_TABLES',','.join(missing))
print('ALL_FOUR_PRESENT',sum(all(value=='t' for value in row[1:]) for row in rows))
print('READ_ONLY_SCHEMA_SOURCE; FULL_CSV',path)
