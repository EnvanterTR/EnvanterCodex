# Platform login lockout — 27 October 2026

Result: local Identity/PostgreSQL API2/2passed,34.3449seconds,exit0. Existing options test proves configured five attempts/five minutes; new functional test uses the actual login endpoint and synthetic platform account without acceptance authentication headers.

Each of five wrong-password requests returns401. Fresh identity scopes prove failure counts1–4 and no early lock; the fifth request creates a lock. The persisted LockoutEnd lies between the first-attempt timestamp+5minutes and final timestamp+5minutes. A correct password during the lock still returns401; protected platform overview remains401, and a fresh scope proves the lock deadline is unchanged. No real user was locked and no five-minute sleep or time-policy relaxation was used.

This covers the active lock boundary; it does not claim successful login after expiry, MFA challenge throttling, every IP policy or real production lockout. Product code/configuration did not change. Build0, disposable platform-lockout-20261027 DB/network cleanup0; image retained. Existing migration/analyzer warnings are in logs; no new migration.

Evidence: platform-lockout-build-20261027.log, platform-lockout-acceptance-20261027.log, platform-lockout-cleanup-20261027.log. New test remains local until an appropriate next CI push; existing85c4f8cCI was not restarted. Full Kapı6/Hedef remain open.

TAMAM dediğim1yerel kilit kabul paketinin1'i gerçekAPI/Identity/PostgreSQLçıktısıyla doğrulanmıştır.

## Second-factor lockout extension

The functional case is now a theory named Platform_login_locks_after_five_failed_factors_and_rejects_correct_password_during_five_minute_lock. It covers wrong-password and correct-password/invalid-second-factor input separately. The latter uses synthetic MFA enrollment plus a deterministic invalid recovery/token string (no actual user code), observes400 on attempts1–4,423 on5, persisted five-minute lock, and401 for a later correct-password request. Fresh scopes show AccessFailedCount accumulating despite password verification. This proves the rejection path including failed recovery fallback; it does not claim every six-digit TOTP boundary or recovery-code success behavior.

Combined final run3/3passed39.3283seconds,exit0; build0 and disposable platform-factor-lockout-20261027 DB/network cleanup0. Logs platform-factor-lockout-build-20261027.log, platform-factor-lockout-acceptance-20261027.log, platform-factor-lockout-cleanup-20261027.log. Earlier2/2 run remains historical supporting evidence, not an additional three cases. No product/production/authentication policy change. RemoteCI for this local extension remains pending.
