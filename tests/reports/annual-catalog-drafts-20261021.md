# Annual catalog drafts — 2026-10-21

Added immutable approved8tier definition and platform-admin preview/create-drafts endpoints. Each new draft has its approved finite seat quota, TRY annual price, MonthlyPrice=0, tax-exclusive description and IsPublic=false. Existing matching codes and unrelated plans are preserved. Repeated creation adds no duplicates; creation/audit are transactional. Existing platform authorization, CSRF and critical-write MFA apply.

Actual HTML Chromium acceptancePASS:8tier preview, cancel no write, explicit reason/CSRF, duplicate submit blocked, unpublished refreshed plan list,390px. SyntheticAPI. Targeted PostgreSQL process1945exit0PASS1/1: ordinary-admin403, missingCSRF400, invalidreason400,7missingdraftscreated/firstexistingpreserved777, repeatedcallcreates0, exactannualquota/prices and7auditrecords. DisposableDB/networkcleaned. Webbuild0warnings/errors; testbuild4test-hostdeprecationwarnings. Inventory2253/sourceguards/diffcheckPASS.

No production draft creation, price publication, payment, tax calculation or trial account provisioning occurred. Requires predecessor quota PR22.15day trial metadata describes the approved policy, not completed trial runtime functionality. Continue actual trial/onboarding and payment conversion implementation.
