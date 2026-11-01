# Operational notification email role authorization — 30 October 2026

Local implementation and focused acceptance passed: 5/5 tests, 1.1806 minutes, exit 0. Isolated temporary PostgreSQL container/network cleanup completed. No production data or real recipient changed. Not deployed.

Operational notification preferences do not grant permanent inventory access. Delivery now resolves the current company role and requires assets.read before collecting reminder data or claiming a digest. This matches the asset.read policy required by notification preferences endpoints. Built-in HR-only role, custom role with its inventory permission removed, and inactive custom role each produce zero SMTP messages and no digest claim. Restoring viewer permission permits one delivery; a second worker pass does not duplicate it. Both existing direct-unit/location employee-scope acceptance cases also passed.

Evidence: OperationalEmailRoleAuthorizationAcceptanceTests.cs; operational-email-role-authorization-20261030.log. No schema change. This proves the checked current-role boundary, not atomic authorization against changes occurring after the final check, nor completion of all P1 queue requirements.

Full remote CI 40980530003 tests the preceding support-email candidate and clock fix, not this unpushed operational-email change. Do not cancel that run or claim it covers this change. New remote CI/staging/release gates remain required.

TAMAM dediğim 1 maddenin 1'i test çıktısıyla doğrulanmıştır.
