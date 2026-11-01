import json,subprocess,urllib.request,urllib.error
c=json.loads(subprocess.check_output(['docker','inspect','envtr-stage-web-f12dd9b']))[0]
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
proxy=json.loads(subprocess.check_output(['docker','inspect','envtr-stage-public-f12dd9b']))[0]
assert not proxy['HostConfig']['PortBindings']
assert proxy['Config']['Image']=='envantertr-staging-public:f12dd9b'
proxyip=proxy['NetworkSettings']['Networks']['envtr-stage-ae875b6']['IPAddress']
for host in ['envantertr.com','santa.envantertr.com','farma.envantertr.com']:
 req=urllib.request.Request('http://'+proxyip+'/company-onboarding.css',headers={'Host':host,'X-Forwarded-Proto':'https'})
 with urllib.request.urlopen(req,timeout=20) as r:
  assert r.code==200 and 'text/css' in r.headers['Content-Type'] and b'.onboarding-panel' in r.read()
 print(host,'STAGED_PROXY_ONBOARDING_CSS_200_TEXT_CSS')
from html.parser import HTMLParser
class ResetFeedbackParser(HTMLParser):
 def __init__(self): super().__init__(); self.form_depth=0; self.outside=False
 def handle_starttag(self,tag,attrs):
  attributes=dict(attrs)
  if tag=='form': self.form_depth+=1
  if attributes.get('id')=='message': self.outside=self.form_depth==0
 def handle_endtag(self,tag):
  if tag=='form': self.form_depth-=1
for host in ['envantertr.com','santa.envantertr.com','farma.envantertr.com']:
 req=urllib.request.Request('http://'+proxyip+'/reset-password.html',headers={'Host':host,'X-Forwarded-Proto':'https'})
 with urllib.request.urlopen(req,timeout=20) as r:
  assert r.code==200
  parsed=ResetFeedbackParser(); parsed.feed(r.read().decode()); assert parsed.outside
 print(host,'RESET_FEEDBACK_OUTSIDE_HIDDEN_FORM_PASS')
print('EXACT_SESSION_FINAL_STAGE_HTTP_AND_PROXY_PASS')
def proxy_request(host,path):
 req=urllib.request.Request('http://'+proxyip+path,headers={'Host':host,'X-Forwarded-Proto':'https'})
 try: return urllib.request.urlopen(req,timeout=20)
 except urllib.error.HTTPError as error: return error
for host in ['santa.envantertr.com','farma.envantertr.com','independent.envantertr.com']:
 for path in ['/platform-admin.html','/platform-setup.html','/admin','/admin/companies','/platform-admin-members.js','/platform-admin-subscriptions.js','/api/platform/overview','/api/platform/bootstrap/status']:
  r=proxy_request(host,path); assert r.code==404,(host,path,r.code); assert b'<html' not in r.read().lower()
  print(host,path,'REAL_PROXY_PLATFORM_404')
 r=proxy_request(host,'/'); assert r.code==200 and b'loginForm' in r.read()
 r=proxy_request(host,'/api/setup/status'); assert r.code==200 and json.load(r)['platformLoginUrl']=='https://envantertr.com/platform-admin.html'
 print(host,'COMPANY_LOGIN_AND_ROOT_PLATFORM_LINK_PASS')
for path in ['/platform-admin.html','/platform-setup.html']:
 r=proxy_request('envantertr.com',path); assert r.code==200 and b'<html' in r.read().lower()
 print('envantertr.com',path,'ROOT_PLATFORM_HTML_200')
print('EXACT_SESSION_FINAL_TENANT_PLATFORM_PROXY_BOUNDARY_PASS')
for host in ['santa.envantertr.com','farma.envantertr.com','independent.envantertr.com']:
 req=urllib.request.Request('http://'+proxyip+'/api/session/platform-reauth',data=b'{}',method='POST',headers={'Host':host,'X-Forwarded-Proto':'https','Content-Type':'application/json'})
 try: r=urllib.request.urlopen(req,timeout=20)
 except urllib.error.HTTPError as error: r=error
 assert r.code==404,(host,r.code)
 print(host,'REAL_PROXY_PLATFORM_REAUTH_POST_404')

import http.client
for host in ['envantertr.com','www.envantertr.com','santa.envantertr.com','farma.envantertr.com']:
 for path in ['/admin','/admin/','/admin?view=ignored']:
  connection=http.client.HTTPConnection(proxyip,80,timeout=20)
  connection.request('GET',path,headers={'Host':host,'X-Forwarded-Proto':'https'})
  response=connection.getresponse(); response.read()
  expected=404 if host.startswith(('santa.','farma.')) else 302
  assert response.status==expected,(host,path,response.status)
  assert response.getheader('Location')==(None if expected==404 else '/platform-admin.html')
  assert not response.getheader('Set-Cookie')
  print(host,path,'ADMIN_ENTRY',expected,'RELATIVE_LOCATION_OR_TENANT_DENIAL_PASS')
  connection.close()
