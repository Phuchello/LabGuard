# Safe code cleanup

## Changes

- Admin: removed sixteen empty event handlers and their twelve Designer subscriptions together. Preserved row formatting, demo loading and `ClearDemoData()` for later integration.
- Admin: removed only the nonfunctional, out-of-scope "Khởi động lại máy" button. Kept Login, receiving and completing incidents visible but disabled until implemented.
- Admin: replaced the fake connecting status with "Demo: Chưa kết nối". Demo PC-DEMO-02 now shows CPU 92.4% and a clearly labeled three-sample >=90% CPU example, consistent with the approved HIGH_CPU rule. This is mock data, not rule-engine implementation.
- Agent: `MetricsService.Start()` and `HeartbeatService.Start()` now reject a second start before `StopAsync()` with `InvalidOperationException`, preserving the original loop's CTS/task. Each scheduled task captures its own token instead of reading the mutable CTS field later.
- Tests: added four focused tests for duplicate-start rejection, sequential repeated Stop and restart. The two duplicate-start tests were run and failed before the production correction, then passed afterward.
- Housekeeping: removed six unused Admin imports and one duplicate `*.log` ignore rule.

No changes to Central, AgentSocketClient/writer queue, protocol/DTOs, SQL, frozen specifications, target frameworks, project files, resources, Simulator, tools, WBS, handbooks, reports or presentations. No feature-completion or end-to-end monitoring claims.

See [audit](CLEANUP_AUDIT.md) for deferred ownership-specific risks and [validation](VALIDATION.md) for actual results and local policy limitations. Owner review is required before merge; this task does not merge the PR.
