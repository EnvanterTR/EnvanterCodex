# Windows restart and app recovery check — 13 October 2026

## Finding

Windows System event 1074 records `MoNotificationUx.exe` initiating a planned operating-system update restart on behalf of the interactive user session at 14:28:24 local time. The reason is `Operating System: Service pack (Planned)` and code `0x80020010`. Windows Update recorded an update download at 14:16:09. Kernel-Power/EventLog entries then show the system shutting down and starting again. Docker Desktop's backend exited at the same 14:28:24 timestamp. This restart accounts for the simultaneous closure of Codex, Docker Desktop, GitHub Desktop, and other open applications.

The event proves that the Windows update notification component initiated a planned restart. It does not establish whether the restart was automatically scheduled or selected by a person in the update notification. An earlier 08:44 restart was initiated by `StartMenuExperienceHost.exe` with reason `Other (Unplanned)`; available event details do not explain it.

Application log review found no matching Windows `Application Error` or `Application Hang` crash entries for ChatGPT/Codex, Docker Desktop, or GitHub Desktop in the reviewed period. A separate `LiveKernelEvent 141` GPU watchdog report was recorded at 14:30 after the restart. It is a graphics-kernel diagnostic; available evidence does not connect it to an app closure. Docker had also experienced an earlier Secrets Engine socket rename failure, which is a separate Docker startup issue and not the 14:28 all-app shutdown.

## Recovery and monitoring

Current-user startup entries now launch the installed ChatGPT/Codex desktop app via its registered Windows app ID, GitHub Desktop, Docker Desktop, and a hidden process-exit monitor after the next user sign-in. The previous Codex startup value launched a command-line `codex.exe`, not the desktop app; this was corrected. The monitor tracks top-level ChatGPT, GitHub Desktop, Docker UI, and Docker backend processes and appends timestamp/PID/path observations to `%LOCALAPPDATA%\\EnvanterTRDiagnostics\\app-exits.jsonl`.

The three applications were running at the time of this check. Local Compose services were running, PostgreSQL was healthy, `/api/status` was `ready` with 84/84 migrations, and the main app and demo returned HTTP 200. The code under test was then at `1e3b24f1cfe97c0b78e181761202504dcec50593`; the diagnosis note was subsequently committed as `a1afdde` on `main`. Current SaaS routing work is tracked on `fix/coolify-tenant-wildcard`.

## Limits

Startup entries can relaunch apps after Windows signs the user in; they cannot keep apps open during a Windows restart or guarantee a restart will never happen. The process monitor reports disappearance and context but does not prove the cause or capture exit codes. Its new root-process tracking can only gather evidence from now onward. Verify automatic relaunch after the next sign-in before treating that behavior as accepted. Preventing update-driven restarts while a user is signed in requires an appropriate Windows update policy and administrator rights; no such policy was changed.

## Project acceptance checks — 13 October 2026

- All 37 repository `tools/Verify-*.mjs` checks passed. This covers route/navigation, responsive layouts, table structure, role gates, action/endpoint wiring, import/export previews, and the named menu families. These are source-level/UI-contract checks; they are not a complete interactive browser acceptance of every write action.
- `deploy/testing/run-security-acceptance.ps1` passed **104/104** tests against its unique isolated PostgreSQL 17 Compose project (test duration 10m 28s). The script removed that isolated project, test database volume, and network after success. The product development Compose project was not targeted.
- Rechecked development Compose: `web`, `public-site`, and `database` all running; database healthy. `/api/status` ready with 84 migrations and 0 pending; `localhost:8080/` and `localhost:8081/demo/` return HTTP 200. GitHub `origin/main` is reachable and points to the current code HEAD `1e3b24f1cfe97c0b78e181761202504dcec50593`.
- The broad menu/subaction inventory and real-session browser acceptance remain in progress. No menu functionality percentage is inferred from these source-level checks.
