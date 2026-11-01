# Live Cloud trial route check — 2026-10-23

- Live `GET https://envantertr.com/api/status`: HTTP 200, `ready`, migration count 118, pending 0.
- Live `GET https://envantertr.com/api/plans`: HTTP 200, empty array.
- Live `GET https://envantertr.com/api/checkout/status`: HTTP 200, `available:false`, provider `null`.
- Live `GET https://envantertr.com/api/trials/status`: HTTP 404.
- The current candidate source maps `/api/trials/status` in `src/EnvanterTR.Web/Data/CloudTrialEndpoints.cs` and calls `MapCloudTrialEndpoints()` from `Program.cs`. The verified deployed revision is `origin/main` at `4d55270827be312775ebffe60c5a6d74011d7802`; that revision has no `CloudTrialEndpoints.cs` and does not map cloud trial endpoints. This explains the live 404: the candidate trial feature has not been released.
- `demo3.envantertr.com` DNS resolves to Cloudflare A records and HTTPS `/api/status` returns HTTP 200 with TLS verify result 0. This only proves wildcard routing/TLS and generic application health. No Demo3 tenant was created.
- No production writes or deployment were performed. The deployment gate still requires a staging acceptance environment, a fresh production backup, and a rollback rehearsal. Checkout also remains blocked by absent Iyzico/SMTP secrets and an empty published plan catalog.
- Next: stage and review the candidate route before release; satisfy the deployment protection gate and external provider/mail setup before production onboarding or publication.
