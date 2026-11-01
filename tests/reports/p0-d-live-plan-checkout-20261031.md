# P0-D live plan and checkout status — 31 October 2026

Read-only GET of `https://envantertr.com/api/plans` returned eight published TRY Cloud plans. The API says the price is VAT excluded and annual payment is a single charge; the monthly amount is for comparison only.

| Plan | Login users | Annual price |
|---|---:|---:|
| Bireysel | 1 | 2,400 TL |
| İşletme | 4 | 4,200 TL |
| İşletme | 10 | 6,000 TL |
| Kurumsal | 50 | 9,000 TL |
| Kurumsal | 100 | 12,000 TL |
| Kurumsal | 250 | 14,400 TL |
| Kurumsal | 1,000 | 24,000 TL |
| Kurumsal | 2,000 | 36,000 TL |

Read-only GET of `/api/checkout/status` returned `available: false`, `provider: null`. Therefore the public plan catalog is live, but online payment/provisioning cannot yet be accepted. The user previously confirmed no iyzico merchant account exists. No payment was initiated and no live configuration was changed. P0-D remains open for merchant onboarding, provider credentials, checkout/webhook reconciliation, and successful sandbox/real acceptance.

