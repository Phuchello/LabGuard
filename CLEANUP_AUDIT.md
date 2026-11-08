# Safe cleanup audit

Base: `02e1dcc2284f968a0daeb4c18b534821e02dc06d` (`origin/main`).
Line references below identify the **pre-change base**, not shifted candidate lines.
Audit recorded before implementation; outcomes are completed with validation.

| Classification | File and base line | Problem / evidence | Risk | Disposition |
|---|---|---|---|---|
| Confirmed dead code | `src/LabGuard.Admin/DashboardForm.cs:20-48,78-111,140-153`; `DashboardForm.Designer.cs:80-323` | Sixteen private handlers have empty bodies. Repository-wide reference search finds only their definitions and twelve Designer subscriptions; four have no subscriptions. Formatting and Load handlers contain real logic. | Low | Remove empty handlers and their subscriptions together; preserve formatting/Load. |
| Confirmed misleading UI | `src/LabGuard.Admin/DashboardForm.Designer.cs:185-198,400` | `button3` says "Khởi động lại máy"; its only handler is empty and no external dependency exists. Remote restart is outside scope. | Low | Remove control, field, initialization, panel membership and handler. |
| Incomplete implementation | `src/LabGuard.Admin/DashboardForm.Designer.cs:107-122,200-228`; `DashboardForm.cs:134` | Login / incident actions have no implementation. `ClearDemoData()` explicitly documents future C-002/C-003 integration. | Low | Keep legitimate controls disabled; preserve public integration hook, resources and preview. |
| Quality risk | `src/LabGuard.Admin/DashboardForm.cs:116-130`; `DashboardForm.Designer.cs:145` | Demo CPU 88.6% and CPU alert at 85% contradict ADR HIGH_CPU >=90% for three consecutive samples; "Connecting" implies nonexistent connectivity. | Low | Make demo CPU/alert consistent and label preview as disconnected demo. Do not implement rule engine. |
| Redundant code | `src/LabGuard.Admin/DashboardForm.cs:1-9`; `.gitignore:34,50` | Six imported namespaces unused by remaining Form logic; `*.log` appears twice. | Low | Removed unused imports and second identical ignore entry only. |
| Quality risk | `src/LabGuard.Agent/MetricsService.cs:161-167`; `HeartbeatService.cs:125-131` | A second Start overwrites the only CTS/task handles, potentially orphaning the first periodic loop. | Moderate | Two regression tests failed before the correction, then passed. Guard duplicate Start and capture token locally. Two more tests cover repeated sequential Stop and restart. No shared lifecycle abstraction. |
| Quality risk / deferred | `src/LabGuard.Agent/MetricsService.cs:172-200`; `HeartbeatService.cs:136-164` | Stop/Dispose overlap and faulted task cleanup need a separately specified lifecycle policy. Synchronous Dispose does not await task completion. | Moderate | Defer to Phúc; do not claim concurrent lifecycle safety or async disposal has been implemented. |
| Quality risk / deferred | `src/LabGuard.Agent/AgentSocketClient.cs:90-124,175-214,291-345` | Concurrent connect/disconnect/dispose interleavings merit dedicated tests. Current synchronous and asynchronous disposal semantics differ intentionally. | Moderate | Leave file, single-writer queue (256/Wait), backpressure and public interfaces unchanged; review note for Phúc. |
| Quality risk / deferred | `src/LabGuard.Central/CentralServer.cs:122-125,228-229,253-259` | Cancellation between accept and task scheduling and exceptions from external event subscribers may affect socket cleanup; ownership/error policy changes need Quang review. | Moderate | No Central edits. Findings are inspection notes, not claimed reproduced failures. |
| Incomplete implementation | `src/LabGuard.Simulator/Program.cs:1-2`; `docs/internal/TEAM_ASSIGNMENTS.md:19` | Placeholder is explicitly retained in the conditional P1 roadmap. | None | Retain Simulator, fixtures, TCP Echo, Mock Harness and historical evidence. |
| Incomplete implementation | `src/LabGuard.Admin/Properties/Resources.resx:124-125` | `shield` has a generated resource accessor, although not currently displayed. Absence of a current control use does not establish future obsolescence. | Low | Retain all resources and resource generator settings. |

No tracked `bin/`, `obj/`, `.vs/`, `TestResults/`, `.tmp` or `.bak` artifacts were found by the scoped tracked-file scan. Historical documents/evidence are not cleanup targets.

## Ownership and merge gates

- Admin: **Lý Tuyết Vy** must review before merge.
- Agent: **Võ Trọng Phúc**, with **Nguyễn Bá Quang** as reviewer per ownership matrix.
- Central inspection notes are deferred to Quang; Ngọc's tools and advisory responsibilities are unchanged.
- Do not merge this cleanup automatically. No WBS/task-state, SQL, DTO, frozen-contract, protocol, handbook, report or presentation changes.
