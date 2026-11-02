# Live-derived release candidate — security acceptance

- Measured: 2026-11-02T11:52:15.198Z.
- Candidate branch: `release/v1-candidate-20261102`.
- Candidate commit: `1a676b2ae2dce9416fdd918ba8c26472f49ec316`.
- Lineage: clean isolated worktree derived from live deployment SHA `4789b509c0b719c1bf71a0b8032f7224bae9fd43`; migration ID set matches the deployed revision (122 IDs).
- Release build: PASS, 0 warnings, 0 errors.
- Source verifiers: 85/85 PASS.
- UI inventory check: PASS, 2,304 controls / 71 routes.
- PostgreSQL security acceptance: 413 passed, 0 failed, 1.9849 hours; isolated Compose database; containers, network, and volume removed after completion.
- Production database writes and deployment: none.
- Scope: this is a candidate regression result. It does not complete the production backup/restore rehearsal, integrate the dirty feature branch, or establish live customer acceptance. Iyzico remains unavailable until an approved merchant account exists.
