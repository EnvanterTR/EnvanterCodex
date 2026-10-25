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
