# Signed handover PDF email — local acceptance, 26 October 2026

Status: implemented and locally verified; NOT deployed. Production remains 4237b5735859cb9c2672ddc5a2f3a5ef22ba566c, schema 121. Candidate introduces additive schema 122. Full remote CI, exact staging, fresh backup and recovery checks remain required.

Explicit confirmation sends only to the current active employee registered email. Company/employee/location scope, asset.write, CSRF, signed snapshot and signature integrity, and production TLS are enforced. A durable attempt record and document lock prevent duplicate SMTP sends for concurrent or replayed keys. An uncertain result is not automatically resent; a new explicit resend must reference the last attempt. SMTP acceptance is distinguished from recipient delivery.

## Evidence

- handover-email-fixed-acceptance-20261026.log: all five API/PDF cases pass. Its migration case exposed an EF-generated UTF-8 BOM in the retained SQL artifact.
- The SQL artifact was saved as UTF-8 without BOM and SHA-256 regenerated; SQL statements were unchanged. handover-email-migration-final-20261026.log: migration test 1/1 passes, exact generated SQL matches, applying twice preserves original-field projections and results in 122 applied migrations.
- handover-email-browser-final-20261026.log: Chromium regression passes, including cancel without POST, lost-response recovery with identical key and exactly one send, explicit resend confirmation, PDF download and mobile layout.
- The earlier failed logs are retained. Viewer authorization fixture now uses a real viewer membership; browser assertions use the actual registered employee email and the existing dialog close control.
- handover-email-20261026/email-confirm.png visually checked: recipient and document identified, cancel and send controls visible.
- sample-message.eml is synthetic localhost SMTP evidence, not customer email. Python email parser found exactly one application/pdf attachment; pypdf opens its one page. Attachment SHA-256: 8541e913cdb5eb15010894476f045dbf599164e560c5e6b9f6d8408f5eb284a8. API test separately asserts attachment hash matches the persisted attempt.
- Migration artifact SHA-256: 01c04a11378e0e92d011a35bfd0d4762c2bcb9ababc0fa610b2f7116d753a2c0.

No real signature was supplied, no customer recovery code was used, and no production email or migration was triggered by these tests. Santa real-document signing and recipient inbox confirmation remain separate acceptance steps.
