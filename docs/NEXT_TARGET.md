# NEXT_TARGET

- Status: completed initial port (2026-08 session)
- Objective: Intatis-Windows WinUI3 native version with deferred features partially implemented
- Scope: Auto title service, EventLog torn-tail recovery, SubmittedIntent outbox, CLI goal parsing, GUI sidebar start, Markdown control skeleton
- Validation: not possible on macOS (no dotnet SDK, no WinUI3); requires Windows build/test
- Blockers: .NET SDK 10.0.300 pinned but net8.0 project; must verify on actual Windows machine; full sidebar styling, auto-title smoke, markdown theme sync, and compaction design alignment with Codex App Server remain open.
