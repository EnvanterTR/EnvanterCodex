# Personal employee EULA file namespace — 2026-10-29

The assigned employee's EULA download alias now applies CompanyMediaPathGuard to the full storage path before opening the file. Its existing membership, active company/employee, location/unit, assignment, revocation and current-version checks remain unchanged.

Existing employee EULA acceptance scenario extended with foreign directory and final-file symbolic links. Both return404 and preserve foreign sentinel bytes; removing each link restores the original own-company PDF. The rest of the scenario verifies linked-employee privacy, EULA versioning, CSRF, confirmed acceptance, idempotency, audit and revocation behavior.

Local PostgreSQL:1/1 passed,2.1850minutes,exit0; temporary database/network removed. Evidence:employee-eula-media-namespace-20261029.log. No real user agreement accepted and no production deployment performed.
