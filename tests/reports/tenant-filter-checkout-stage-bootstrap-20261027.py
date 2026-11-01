import pathlib,subprocess

sha='e80370c76d18e7983b1d3692fa662efa49cbeeb1'
stage=pathlib.Path('/var/lib/envantertr-staging/e80370c-20261027')
assert not stage.exists(), 'Do not overwrite/restart an existing stage job'
stage.mkdir(mode=0o700)
subprocess.run(['curl','--fail','--location','--retry','2','--silent','--show-error','--output',str(stage/'source.tar.gz'),
 'https://github.com/EnvanterTR/EnvanterCodex/archive/'+sha+'.tar.gz'],check=True)
subprocess.run(['tar','-xzf',str(stage/'source.tar.gz'),'--strip-components=1','-C',str(stage)],check=True)
assert (stage/'src/EnvanterTR.Web/Data/CompanyOnboardingEndpoints.cs').is_file()
job=subprocess.Popen(['bash','-c','docker build -t envantertr-staging:e80370c -f Dockerfile . > build.log 2>&1 && docker build -t envantertr-staging-public:e80370c -f deploy/coolify/Dockerfile.public-site . >> build.log 2>&1; result=$?; printf "%s\\n" "$result" > build.exit'],cwd=stage,
 stdin=subprocess.DEVNULL,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,start_new_session=True)
(stage/'build.pid').write_text(str(job.pid)+'\n')
print('EXACT_TENANT_FILTER_CHECKOUT_FIX_STAGE_BUILD_STARTED pid='+str(job.pid)+' path='+str(stage),flush=True)
