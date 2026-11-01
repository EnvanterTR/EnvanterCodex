# Session isolation staging preparation — 30 October 2026

Remote product candidate8cdb77c69392f97915b8f64f406b240fcc760d5e has the same src/,deploy/coolify/,Dockerfile source as local951747c and subsequent evidence-only commits. Current full hosted browser regression passed locally with explicit read-only new-source mounts. The corrected auth verifier passed locally; full corrected remote CI remains required. Original CI41009579015 Linux failed at that obsolete verifier locator; Windows5m06s passed; PostgreSQLjob123912172427 remains running at18m52s. Do not cancel/restart/push while active.

Single exact stage build /var/lib/envantertr-staging/8cdb77c-20261030 PID3620837 remains running. Preparation PID3623670 completed prepare.exit0. Fresh SQL pre-web-v1-20261030T055542Z.dump hash/list/independent restore passed, schema122/95 original table field fingerprints saved privately. Cloneenvantertr_stage_8cdb77c and separate media/keys prepared; SMTP/payment/setup/trial effects disabled. Stage apps have not started; runtime/HTTP/data-preservation probes are prepared but not executed. No release of this candidate.

Current verified production remainsd19cfe5; successful release/health/source/access evidence is in operational-role-production-release-20261030.md. Legacy date CSV is privately on the user desktop; no date backfill. FullHedef/P1 remains incomplete.

Intermediate8cdb77c stage accepted: build0/prepare0/acceptance-probes0, runtime/HTTP/proxy/CSRF/tenant-platform boundaries and all95original table field projections preserved, schema122/0. ProbePID3636215; evidence session-isolation-stage-pass-20261030.png. No production deployment.

IMPORTANT: subsequent raw body-session guard changes product HTML. This stage is no longer equivalent to the current combined candidate. Do not reuse it as final-stage proof or deploy the incomplete intermediate revision merely to bypass the remaining work. See session-export-body-20261030.md. Final combined source/build/staging/release verification remains required.
