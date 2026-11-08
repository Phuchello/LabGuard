# Safe cleanup validation

## Environment and isolation

- Date: 2026-10-19; Windows x64, OS version `10.0.26300`.
- .NET SDK `9.0.318`; runtime / Windows Desktop `9.0.20`; VSTest `17.14.1`.
- Repository: `https://github.com/Phuchello/LabGuard.git`.
- Branch: `chore/safe-code-cleanup`, isolated worktree from `origin/main` at `02e1dcc2284f968a0daeb4c18b534821e02dc06d`. No merge of old branches; local main untouched.
- Original checkout remains on `docs/progress-sync-20261015` at `dab8dadca98c8f35e9e9a7ff828188005b478376`. Its six dirty tracked files and 3,876 porcelain status entries were recorded privately before edits. It is not the cleanup workspace.
- Build/test execution required elevated sandbox approval for the installed .NET runtime / caches. No Windows security settings were changed.

## Baseline and post-change commands

Both phases run the same solution restore/build/test and whitespace checks:

```powershell
dotnet restore LabGuard.sln
dotnet build LabGuard.sln
dotnet test LabGuard.sln --logger trx --results-directory <private-evidence-directory>
git diff --check
```

| Check | Baseline | After cleanup |
|---|---|---|
| Restore | PASS, exit 0 | PASS, exit 0 |
| Solution build | PASS, exit 0; 0 warnings, 0 errors | PASS, exit 0; 0 warnings, 0 errors |
| Protocol tests | 107 passed | 107 passed |
| Agent tests | 61 passed, 3 failed due to OS policy | 65 passed, 3 failed due to same OS policy |
| Central tests | 9 failed due to OS policy | Same 9 failed due to same OS policy |
| Mock Harness tests | 15 passed | 15 passed |
| Total | **183 passed / 195; 12 failed, 0 skipped** | **187 passed / 199; 12 failed, 0 skipped** |
| `git diff --check` | PASS | PASS |

The full test command exits **1**, not 0. Every one of the twelve failures is an assembly load failure:

```text
System.IO.FileLoadException: Could not load file or assembly '...LabGuard.Central.dll'.
An Application Control policy has blocked this file. (0x800711C7)
```

These are the same nine `CentralServerTests` and three `AgentStartupTests` in both phases; TRX test names were compared, with **zero new failures**. No exclusions, skipped-test attributes, changes to Central, or security-policy workarounds were introduced. Historical 195-pass evidence is retained but is not presented as a current full-suite pass. The twelve tests must run on an appropriately authorized Windows environment before merge.

## Test-first Agent correction

Before editing production services:

```powershell
dotnet test tests/LabGuard.Agent.Tests/LabGuard.Agent.Tests.csproj --filter FullyQualifiedName~Start_WhenAlreadyStarted_RejectsDuplicateLoop
```

Result: **2 failed**, both with `Assert.Throws() Failure: No exception was thrown`, proving duplicate Start was accepted. Inspection shows that path overwrites the first loop's sole CTS/task handles.

After the guard and local token capture, run both new test names:

```powershell
dotnet test tests/LabGuard.Agent.Tests/LabGuard.Agent.Tests.csproj --filter "FullyQualifiedName~Start_WhenAlreadyStarted_RejectsDuplicateLoop|FullyQualifiedName~StopAsync_IsRepeatable_AndAllowsRestart"
```

Result: **4 passed / 4**, exit 0. Covers duplicate Start rejection, Stop before Start, repeated sequential Stop, immediate Stop and restart for both producers. These tests are also included in the post-change solution run. They do not claim concurrent Start/Stop/Dispose safety; that policy remains deferred to the owner.

## Admin / Designer checks on Windows

- Built the actual `net9.0-windows` Admin project as part of the solution, including `.resx` compilation and generated resource accessors.
- Launched the cleanup worktree's `LabGuard.Admin.exe` and inspected the live window visually and through Windows accessibility. Four demo devices, two demo incidents, warning/offline row styling and the system/wrench images were present. The restart control was absent. Login, receiving and completing incidents remained present and disabled. The header explicitly read `Demo: Chưa kết nối`; CPU displayed `92.4%`.
- Shortened the demo alert text to `[Demo] CPU ≥90%, 3 mẫu` to fit the existing issue column. It represents an assumed three-consecutive-sample example, not a live rule result.
- Rebuilt and relaunched the final preview: the shortened alert fits without ellipsis; images, row styling, disconnected-demo label and disabled buttons remain intact. An intermediate build was blocked by the preview executable's file lock (MSB3027/MSB3021); closing only that preview and rebuilding resolved it, with 0 warnings/errors. No application source change was needed for the lock.
- Checked all 22 initialized controls/columns have field declarations; exactly two remaining event subscriptions resolve to real methods (`dgvPhongMay_CellFormatting`, `DashboardFromNew_Load`). No dangling removed-handler/restart bindings remain.
- Both `.resx` files parse; all three file-backed resources (`system`, `shield`, `wrench2`) exist. `.resx`, images, generated resource source and project generator configuration are unchanged.
- **Interactive Visual Studio Designer verification remains unconfirmed.** Visual Studio was launched, but the computer-use helper did not reliably complete the Open Project dialog (stale element cache / inconsistent focus). Runtime preview, build and structural checks are not claimed to substitute for a successful interactive Designer load. Vy must open `DashboardForm` in Designer before merge.

## Scope and preservation

- The only implementation changes are two Admin files, two producer services and their two existing test files; housekeeping changes only remove a duplicate `.gitignore` entry. The three requested Markdown deliverables are new.
- `git diff --quiet 02e1dcc -- src/LabGuard.Protocol database docs/spec src/LabGuard.Central src/LabGuard.Simulator tools src/LabGuard.Admin/Properties src/LabGuard.Admin/Resources` returned 0: these protected paths are unchanged.
- Public method/property signatures and payload construction are unchanged. Duplicate sequential Start now intentionally throws instead of starting an untracked loop. The writer queue, bounded backpressure, framing, SQL, dependencies/TFMs, resources and integration hook remain intact.
- No WBS/project-status, handbook, report, PDF or PowerPoint edits. No generated build/test artifacts added to Git.
- Private logs/TRX and original-checkout fingerprints are in sibling directory `LabGuard_cleanup_evidence`, outside the worktree and not committed.
- Final read-only comparison found the original branch, HEAD, all 3,876 status entries and hashes of all six dirty tracked files unchanged. No original untracked deliverables were removed or overwritten.

## Merge readiness

This is **not a clean full-suite PASS**. Keep the PR unmerged, pending the twelve policy-blocked tests and interactive Designer verification, plus Vy's mandatory Admin review and Agent/Quang review. Repository workflow inventory contains only a reviewed-PDF rebuild workflow; no general application CI pass is claimed.
