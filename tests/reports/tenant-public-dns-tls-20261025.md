# Section 20.9 public subdomain infrastructure — 2026-10-25

Read-only external checks at 03:56 UTC; no DNS, certificate or tenant record changed.

| Host | Public DNS A lookup | HTTPS health | App state |
| --- | --- | --- | --- |
| demo3.envantertr.com | DNS status 0, answers present | 200 | ready |
| santa.envantertr.com | DNS status 0, answers present | 200 | ready |
| acceptance-20261025.envantertr.com | DNS status 0, answers present | 200 | ready |

Raw sanitized results: `tenant-public-dns-tls-20261025.json`. DNS answers were obtained through Google's public DNS-over-HTTPS resolver. HTTPS requests used the platform's normal certificate and hostname validation; no skip-validation option, insecure flag or browser warning bypass was used.

The newly selected acceptance host was not registered as a company during this check. Its successful public DNS and HTTPS health response supports the shared subdomain routing infrastructure: additional manual DNS/proxy changes are not necessary merely to reach this host. It does not establish that every possible hostname is valid or that a company has been provisioned.

Initial local Windows lookup of demo3 returned a malformed-DNS-packet error. The independent public resolver and actual HTTPS connection succeeded; that local lookup error is not reported as a tenant outage.

## Scope boundary

This closes an evidence gap between earlier staging Host-header checks and actual public DNS/TLS reachability. It does **not** complete Hedef 20.9. Normal Demo3 account creation, owner login, data isolation, and deletion without affecting other tenants are still required. Santa/Farma authenticated acceptance, actual SMTP delivery, and payment acceptance remain open. No OTP or customer credential was used.

## Actual production company presence

A subsequent read-only SQL query on the verified current production database independently checked exact slugs:

| Slug | Company rows | Active, non-deleted rows |
| --- | ---: | ---: |
| demo3 | 0 | 0 |
| farma | 1 | 1 |
| santa | 0 | 0 |

Query: `live-section20-company-presence-20261025.sql`; observed output: `live-section20-company-presence-20261025.png`. Only the three requested slug aggregate counts were selected; no user email, identifier, credential or customer inventory was exposed. No record was written.

Therefore Santa's Section 20.2 owner login and Demo3's Section 20.9 creation/login cannot currently pass merely because their DNS and health routes work. The next authenticated acceptance action is normal platform provisioning, not another DNS/proxy edit. Farma's existence alone proves none of its ten customer workflow steps.
