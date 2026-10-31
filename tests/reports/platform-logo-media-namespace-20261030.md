# Platform company-logo namespace protection — 30 October2026

TAMAM (local implementation/acceptance): the administrator company-logo endpoint now applies CompanyMediaPathGuard before reading the selected company's image. Tenant authorization remains unchanged. The independent platform reader previously bypassed the protected company-logo reader.

Two PostgreSQL acceptance cases passed (directory and final-file redirection), total1.8974minutes; exit0, temporary database/network removed. Each case proves normal authorized PNG bytes/content type, ordinary company-admin denial, redirected path404, unchanged foreign bytes/company metadata and restored normal read. Evidence:platform-logo-media-namespace-20261029.log.

Not deployed. This does not complete Hedef6.1/P1 or the real customer acceptance gates. No schema, real credentials, user photo or live data changed.

TAMAM dediğim 1 maddenin 1'i test çıktısıyla doğrulanmıştır.
