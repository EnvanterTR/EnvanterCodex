import json,subprocess,urllib.request,urllib.error
c=json.loads(subprocess.check_output(['docker','inspect','envtr-stage-web-9585fae']))[0]
ip=c['NetworkSettings']['Networks']['envtr-stage-ae875b6']['IPAddress']
def request(host,path,method='GET'):
 req=urllib.request.Request('http://'+ip+':8080'+path,headers={'Host':host,'X-Forwarded-Proto':'https','Content-Type':'application/json'},method=method,data=b'{}' if method!='GET' else None)
 try: return urllib.request.urlopen(req,timeout=20)
 except urllib.error.HTTPError as error: return error
for host in ['envantertr.com','santa.envantertr.com','farma.envantertr.com']:
 r=request(host,'/api/status'); d=json.load(r); assert r.code==200 and d['status']=='ready' and d['migrationCount']==122 and d['pendingMigrationCount']==0; print(host,'READY 122/0')
 for method in ['GET','PUT','PATCH']:
  r=request(host,'/api/assets/00000000-0000-0000-0000-000000000001',method); assert r.code==401,(host,method,r.code); print(host,method,'ANONYMOUS 401')
 r=request(host,'/api/session/csrf'); d=json.load(r); assert r.code==200 and d.get('requestToken'); assert any('secure' in x.lower() for x in r.headers.get_all('Set-Cookie',[])); print(host,'CSRF token present; Secure cookie')

for suffix,method in [('/pdf','GET'),('/email','GET'),('/email','POST')]:
 r=request('santa.envantertr.com','/api/employees/00000000-0000-0000-0000-000000000001/handover-documents/00000000-0000-0000-0000-000000000002'+suffix,method); assert r.code==401,(suffix,method,r.code); print('handover',suffix,method,'ANONYMOUS 401')
for path,method in [('/api/company/onboarding','GET'),('/api/company/onboarding/complete','POST')]:
 r=request('santa.envantertr.com',path,method); assert r.code==401,(path,r.code); print(path,method,'ANONYMOUS 401')
proxy=json.loads(subprocess.check_output(['docker','inspect','envtr-stage-public-9585fae']))[0]
assert not proxy['HostConfig']['PortBindings']
assert proxy['Config']['Image']=='envantertr-staging-public:9585fae'
proxyip=proxy['NetworkSettings']['Networks']['envtr-stage-ae875b6']['IPAddress']
for host in ['envantertr.com','santa.envantertr.com','farma.envantertr.com']:
 req=urllib.request.Request('http://'+proxyip+'/company-onboarding.css',headers={'Host':host,'X-Forwarded-Proto':'https'})
 with urllib.request.urlopen(req,timeout=20) as r:
  assert r.code==200 and 'text/css' in r.headers['Content-Type'] and b'.onboarding-panel' in r.read()
 print(host,'STAGED_PROXY_ONBOARDING_CSS_200_TEXT_CSS')
print('EXACT_9585_STAGE_HTTP_AND_PROXY_PASS')
