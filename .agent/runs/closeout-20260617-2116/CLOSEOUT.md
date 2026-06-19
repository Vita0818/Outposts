# Closeout Audit - Failed Canonical Migration

MODE: CLOSEOUT_AUDIT
DATE: 2026-06-17
SCOPE: Stop-loss audit for the attempted Rokurics Apple canonical migration to Android and Windows.

## Commands Run

- `git status --short`
- `git diff --stat`
- `git diff --name-only`
- `git status --short --untracked-files=all`
- Read only changed files or changed-file metadata for the focused audit targets.

No source edits, rollback commands, builds, tests, or canonical migration work were performed.

## Summary

The attempted migration produced untracked large compat files and small tracked bridges that were not compiled. The generated compat files show clear signs of mechanical compatibility scaffolding rather than a verified platform implementation. At least two concrete compile-risk patterns are present:

- Android has transfer type declarations both in `CanonicalTransferStateMachine.kt` and in the untracked `CanonicalCanonicalCoreCompatTypes.kt`.
- Windows `CanonicalCoreCompatTypes.cs` declares `CanonicalKernelModeMirror` twice in the same file.

Default recommendation: do not keep the generated canonical compat files as-is. Treat them as failed migration artifacts unless a later one-file review proves otherwise.

## Changed Files And Recommendations

| Path | Status | Scale | Mechanical / Stub Signal | Duplicate Risk | Business Break Risk | Recommendation | Reason |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| `.DS_Store` | modified | binary | no | no | low | REVERT | Finder metadata is unrelated to canonical migration. |
| `AGENTS.md` | modified | 174-line diff | no code | no | medium | SPLIT_REVIEW | Supervisor protocol changes are broad workflow changes and should not ride with canonical source changes. |
| `EXAGENT_MODE.md` | modified | 147-line diff | no code | no | medium | SPLIT_REVIEW | Mode protocol edits need separate review. |
| `Flotis-Windows/.DS_Store` | modified | binary | no | no | low | REVERT | Finder metadata is unrelated. |
| `OPENCODE_MODE.md` | modified | 23-line diff | no code | no | medium | SPLIT_REVIEW | OpenCode protocol edits are out of scope for Rokurics canonical migration. |
| `Rokurics-Android/app/src/main/java/com/rokurics/app/domain/canonical/CanonicalKernelFacade.kt` | modified | 31-line diff | adapter bridge | yes, via generated compat dependency | high | REVERT | It redirects facade calls into unverified transfer compat code; rollback with transfer state machine. |
| `Rokurics-Android/app/src/main/java/com/rokurics/app/domain/canonical/CanonicalTransferStateMachine.kt` | modified | 114-line diff, 196 total lines | compatibility adapter | yes with untracked compat file | high | REVERT | Adds unverified state mapping and a likely type mismatch by returning `canonicalNow()` where `CanonicalTimestamp` is expected. |
| `Rokurics-Android/app/src/main/java/com/rokurics/app/ui/home/HomeScreen.kt` | modified | 398-line diff | no canonical relation | no | high | REVERT | Large unrelated UI deletion removes navigation rail and persistent mini-player behavior. |
| `Rokurics-Windows/Rokurics/App.xaml` | modified | 3-line diff | no | no | low/medium | SPLIT_REVIEW | Theme change is unrelated to canonical kernel migration. |
| `Rokurics-Windows/Rokurics/Views/MacStudyLibraryPage.xaml` | modified | 24-line diff | no | no | medium | SPLIT_REVIEW | View virtualization/load changes are unrelated and need UI-specific validation. |
| `Rokurics-Windows/Rokurics/Views/MacStudyLibraryPage.xaml.cs` | modified | 54-line diff | no | no | medium | SPLIT_REVIEW | Panel visibility controller may be valid, but belongs in a separate UI task. |
| `docs/BATCH_SCHEDULING.md` | modified | 110-line diff | no code | no | medium | SPLIT_REVIEW | Scheduling protocol changes are not canonical migration output. |
| `docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md` | deleted | 192 lines | no code | no | high | REVERT | Protocol deletion is unrelated and risky. |
| `docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md` | deleted | 279 lines | no code | no | high | REVERT | Protocol deletion is unrelated and risky. |
| `docs/DO_NOT_BREAK.md` | modified | 87-line diff | no code | no | medium | SPLIT_REVIEW | Boundary policy edits need separate review. |
| `docs/OUTPOSTS_MODE_EXECUTION.md` | modified | 89-line diff | no code | no | medium | SPLIT_REVIEW | Mode execution policy edits are out of migration scope. |
| `docs/OUTPOSTS_SUPERVISOR.md` | modified | 205-line diff | no code | no | medium/high | SPLIT_REVIEW | Supervisor behavior changes need independent review. |
| `docs/RECOVERY_PLAYBOOK.md` | modified | 270-line diff | no code | no | medium | SPLIT_REVIEW | Recovery protocol edits are broad process changes. |
| `docs/REPORTING_FORMATS.md` | modified | 263-line diff | no code | no | medium | SPLIT_REVIEW | Reporting format changes should not be bundled with source migration. |
| `docs/SECURITY_AND_BOUNDARIES.md` | modified | 166-line diff | no code | no | high | SPLIT_REVIEW | Security boundary changes need explicit standalone review. |
| `tmp-home/.DS_Store` | deleted | binary | no | no | low | REVERT | Tracked temp metadata deletion should be handled separately. |
| `tmp-home/AppData/.DS_Store` | deleted | binary | no | no | low | REVERT | Tracked temp metadata deletion should be handled separately. |
| `tmp-home/AppData/Local/NuGet/Migrations/1` | deleted | empty | no | no | low | REVERT | Temp NuGet artifact deletion is unrelated. |
| `tmp-home/AppData/Roaming/NuGet/NuGet.Config` | deleted | 10 lines | no | no | medium | REVERT | NuGet config deletion is unrelated to canonical migration. |
| `tmp.nuget.config` | deleted | 10 lines | no | no | medium | REVERT | NuGet config deletion is unrelated to canonical migration. |
| `Rokurics-Android/app/src/main/java/com/rokurics/app/domain/canonical/CanonicalCanonicalCoreCompatTypes.kt` | untracked new | 2216 lines | yes: comments call it minimal compatibility layer; many disabled/unknown/sample paths | high | high | REVERT | Large generated compat layer was not compiled and duplicates transfer/status concepts in the same package. |
| `Rokurics-Windows/Rokurics/Models/CanonicalCoreCompatTypes.cs` | untracked new | 1136 lines | yes: bridge/stub runtime patterns and simplified status model | high | high | REVERT | Contains duplicate `CanonicalKernelModeMirror` and broad unverified public surface. |
| `Rokurics-Windows/Rokurics/Models/CanonicalTransferStateMachine.cs` | untracked new | 212 lines | adapter mapping | medium | medium/high | SPLIT_REVIEW | Smaller than the compat cores, but still uncompiled and should be reviewed as one file before keeping. |
| `Spark-log/*` | untracked new | 66 total lines | audit/log only | no | low | KEEP | Preserve as evidence of the failed run; several entries contain literal shell substitutions and are not completion proof. |
| `Spark-logs/*` | untracked new | 135 total lines | audit/log only | no | low | KEEP | Preserve as evidence; content shows repeated scans and incomplete migration claims. |
| `docs/.DS_Store` | untracked new | binary | no | no | low | REVERT | Finder metadata should not be kept. |
| `docs/SUPERVISOR_WORKER_VISUAL_PROTOCOL.md` | untracked new | unknown in tracked diff | no code | no | medium | SPLIT_REVIEW | New protocol doc should be reviewed separately from canonical source. |
| `docs/WORKER_ONE_SHOT_INVOCATION_PROTOCOL.md` | untracked new | unknown in tracked diff | no code | no | medium | SPLIT_REVIEW | New protocol doc should be reviewed separately from canonical source. |
| `Flotis-Apple/` | untracked new directory | many files | unknown | unknown | medium | SPLIT_REVIEW | Unrelated project directory; exclude from this closeout unless user confirms intent. |
| `Intatis-Android/` | untracked new directory | many files | unknown | unknown | medium/high | SPLIT_REVIEW | Unrelated project scaffold; do not bundle with Rokurics rollback. |
| `Intatis-Apple/` | untracked new directory | many files | unknown | unknown | medium | SPLIT_REVIEW | Unrelated project directory; needs separate provenance review. |
| `Intatis-Windows/` | untracked new directory | many files | unknown | unknown | medium/high | SPLIT_REVIEW | Unrelated project scaffold; do not bundle with Rokurics rollback. |
| `Kikaria-Android/QwenCode-output/exagent-greeting-test/round-001-greeting.md` | untracked new | one report | no code | no | low | SPLIT_REVIEW | Unrelated worker output; keep only if needed for a separate Kikaria audit. |

## High-Risk Files

- `Rokurics-Android/app/src/main/java/com/rokurics/app/domain/canonical/CanonicalCanonicalCoreCompatTypes.kt`
- `Rokurics-Windows/Rokurics/Models/CanonicalCoreCompatTypes.cs`
- `Rokurics-Android/app/src/main/java/com/rokurics/app/domain/canonical/CanonicalTransferStateMachine.kt`
- `Rokurics-Android/app/src/main/java/com/rokurics/app/domain/canonical/CanonicalKernelFacade.kt`
- `Rokurics-Android/app/src/main/java/com/rokurics/app/ui/home/HomeScreen.kt`
- `docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md`
- `docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md`

## Recommended Immediate Rollback Set

- `.DS_Store`
- `Flotis-Windows/.DS_Store`
- `Rokurics-Android/app/src/main/java/com/rokurics/app/domain/canonical/CanonicalKernelFacade.kt`
- `Rokurics-Android/app/src/main/java/com/rokurics/app/domain/canonical/CanonicalTransferStateMachine.kt`
- `Rokurics-Android/app/src/main/java/com/rokurics/app/ui/home/HomeScreen.kt`
- `docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md`
- `docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md`
- `tmp-home/.DS_Store`
- `tmp-home/AppData/.DS_Store`
- `tmp-home/AppData/Local/NuGet/Migrations/1`
- `tmp-home/AppData/Roaming/NuGet/NuGet.Config`
- `tmp.nuget.config`
- `Rokurics-Android/app/src/main/java/com/rokurics/app/domain/canonical/CanonicalCanonicalCoreCompatTypes.kt`
- `Rokurics-Windows/Rokurics/Models/CanonicalCoreCompatTypes.cs`
- `docs/.DS_Store`

## Temporarily Retain, But Review Separately

- `Rokurics-Windows/Rokurics/Models/CanonicalTransferStateMachine.cs`
- `Rokurics-Windows/Rokurics/App.xaml`
- `Rokurics-Windows/Rokurics/Views/MacStudyLibraryPage.xaml`
- `Rokurics-Windows/Rokurics/Views/MacStudyLibraryPage.xaml.cs`
- `AGENTS.md`
- `EXAGENT_MODE.md`
- `OPENCODE_MODE.md`
- `docs/BATCH_SCHEDULING.md`
- `docs/DO_NOT_BREAK.md`
- `docs/OUTPOSTS_MODE_EXECUTION.md`
- `docs/OUTPOSTS_SUPERVISOR.md`
- `docs/RECOVERY_PLAYBOOK.md`
- `docs/REPORTING_FORMATS.md`
- `docs/SECURITY_AND_BOUNDARIES.md`
- `docs/SUPERVISOR_WORKER_VISUAL_PROTOCOL.md`
- `docs/WORKER_ONE_SHOT_INVOCATION_PROTOCOL.md`
- `Spark-log/*`
- `Spark-logs/*`
- Unrelated untracked project directories: `Flotis-Apple/`, `Intatis-Android/`, `Intatis-Apple/`, `Intatis-Windows/`
- `Kikaria-Android/QwenCode-output/exagent-greeting-test/round-001-greeting.md`

## Next Small Tasks

1. Rollback task: revert only the immediate rollback set above, with no Apple source reads.
2. Android transfer review: review only `CanonicalTransferStateMachine.kt` and its facade call sites; decide whether the original file should be restored or a small adapter kept.
3. Android compat review: review only the `CanonicalUploadStateTruth` type family if the untracked compat file is ever revisited; do not keep the whole generated file.
4. Windows compat review: review only the `CanonicalKernelModeMirror` and protocol primitive type family in `CanonicalCoreCompatTypes.cs`; first resolve duplicate definitions.
5. Windows transfer review: review only `CanonicalTransferStateMachine.cs` as a single-file proposal and compile it before keeping it.
6. Android UI review: review only `HomeScreen.kt` to restore or separately validate the navigation rail and persistent mini-player changes.
7. Protocol docs review: review Outposts docs changes as one documentation-only task, separate from Rokurics source.

## Final Audit Result

The canonical migration attempt should be considered failed and unvalidated. The generated compat files should not be treated as migrated canonical kernel code. Recommended action is rollback of the high-risk generated and unrelated source changes, then split any future work into one-file or one-type-family reviews.
