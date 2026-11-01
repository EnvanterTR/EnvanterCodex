import pathlib,subprocess,datetime
paths=[pathlib.Path('/etc/crontab'),*pathlib.Path('/etc/cron.d').glob('*'),*pathlib.Path('/var/spool/cron/crontabs').glob('*')]
found=[]
for p in paths:
 if not p.is_file(): continue
 text=p.read_text(errors='replace')
 for line in text.splitlines():
  if not line.lstrip().startswith('#') and ('daily-postgres-backup' in line or ('envantertr' in line.lower() and 'backup' in line.lower())): found.append(str(p))
print('HOST_CRON_ENVANTERTR_BACKUP_MATCH_FILES',sorted(set(found)))
r=subprocess.run(['systemctl','list-timers','--all','--no-pager','--plain'],capture_output=True,text=True)
print('SYSTEMD_ENVANTERTR_BACKUP_TIMER_MATCH',any('envantertr' in x.lower() and 'backup' in x.lower() for x in r.stdout.splitlines()))
print('READ_ONLY_SCHEDULE_AUDIT',datetime.datetime.now(datetime.timezone.utc).isoformat())
