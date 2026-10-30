# Stable browser business clock — 30 October2026

TAMAM (local browser regression): the complete asset-detail-actions.mjs Chromium scenario exited0 with ENVANTERTR_UI_TEST_NOW=2026-10-29T23:59:50.000Z while the actual Node wall clock was2026-10-30T00:08:21.990Z. Build and run logs:asset-detail-midnight-browser-build-20261030.log andasset-detail-midnight-browser-20261030.log.

## Observed CI failure

CI40976671612 Linux job123806302528 failed in1m47s atasset-detail-actions.mjs:4291. The unchanged strict90-day license-horizon assertion observed7862400000ms(91days), expected7776000000ms(90days). The run crossed UTC midnight: report fixtures had captured the previous day while the browser used the next day. Evidence:support-recipient-ci-clock-failure-20261030.log/.png and the original job URLhttps://github.com/EnvanterTR/EnvanterCodex/actions/runs/40976671612/job/123806302528#step:61:321.

## Change

A scenario instant is captured once (with an optional test-only override). License/warranty fixtures, maintenance business dates, audit schedule and budget-year expectations use that instant. The browser uses the same instant and explicit UTC. Node's real clock remains unchanged for bounded waits and mutation version tokens. Product source and its date calculations are unchanged. The90-day assertion was retained exactly; no91-day tolerance or removed assertion.

Playwright setFixedTime fixes Date/new Date while keeping timers running, as documented in the[official Clock API](https://playwright.dev/docs/api/class-clock#clock-set-fixed-time). Each harness browser/context closes normally; the clock does not alter other browser scenarios or real accounts.

The mismatched-date run completed its strict assertions, all52 sidebar destinations and error-free completion. One destination still intentionally shows an explicit feature/module status page; this is not full feature parity acceptance.

## Remaining gate

This is a local fix. OriginalCI40976671612 still has a live PostgreSQL job; do not cancel/restart/push until its terminal result. Then send the changed test harness through a new complete CI run. Support email sourceeae37c7 remains independently7/7 locally accepted and is not live. Current production8a07aec remains unchanged. Full Hedef/P1 remains open.

TAMAM dediğim 1 maddenin 1'i test çıktısıyla doğrulanmıştır.
