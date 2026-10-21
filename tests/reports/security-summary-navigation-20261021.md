# Security summary navigation — 2026-10-21

Live AX inspection showed MFA and active-session summaries carrying asset-list button labels. Root cause: global .stats .stat initialization attached dashboard asset navigation to security cards too.

Restricted dashboard-card navigation initialization to #overview .stats .stat. Security summaries remain articles with their real text; dedicated account security and session controls remain available. All9 overview cards retain button behavior.

Current HTML and test mounted into Chromium command-palette acceptance: exit0 PASS, including explicit zero security summary role=button and9 overview navigation cards, plus existing MFA/session/navigation regressions. No production data or MFA enrollment was changed by synthetic tests. Not yet deployed.
