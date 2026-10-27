# Platform login lockout — 27 October 2026

Result: local Identity/PostgreSQL API2/2passed,34.3449seconds,exit0. Existing options test proves configured five attempts/five minutes; new functional test uses the actual login endpoint and synthetic platform account without acceptance authentication headers.

Each of five wrong-password requests returns401. Fresh identity scopes prove failure counts1–4 and no early lock; the fifth request creates a lock. The persisted LockoutEnd lies between the first-attempt timestamp+5minutes and final timestamp+5minutes. A correct password during the lock still returns401; protected platform overview remains401, and a fresh scope proves the lock deadline is unchanged. No real user was locked and no five-minute sleep or time-policy relaxation was used.

This covers the active lock boundary; it does not claim successful login after expiry, MFA challenge throttling, every IP policy or real production lockout. Product code/configuration did not change. Build0, disposable platform-lockout-20261027 DB/network cleanup0; image retained. Existing migration/analyzer warnings are in logs; no new migration.

Evidence: platform-lockout-build-20261027.log, platform-lockout-acceptance-20261027.log, platform-lockout-cleanup-20261027.log. New test remains local until an appropriate next CI push; existing85c4f8cCI was not restarted. Full Kapı6/Hedef remain open.

TAMAM dediğim1yerel kilit kabul paketinin1'i gerçekAPI/Identity/PostgreSQLçıktısıyla doğrulanmıştır.
