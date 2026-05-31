# Outposts Dispatch Rules Refresh

Date: 2026-05-29

Scope:
- Created `CLAUDE.md` as the Claude Code Desktop / Codex shared Outposts dispatch entry.
- Updated `AGENTS.md` as the Codex Agent-specific entry.
- Updated scheduler docs under `docs/` with shared dispatch rules, visual MCP rules, budget semantics, recovery rules, reporting formats, and forbidden boundaries.

Rules refreshed:
- DeepSeek V4 Pro is the required main reasoning route for formal dispatch.
- Claude Code Desktop / Claude Code 主 Agent may perform actual migration work in formal project sessions.
- Codex Agent remains scheduler-only and must not read/write business source, inspect business diffs, run builds/tests, clean workspaces, or commit.
- `qwen-vision` is an MCP visual tool only; it is not the main model and must not modify files or receive secrets/source.
- Apple source and reference screenshot directories are read-only.
- Formal dispatch must use visible/observable Claude Code terminals with `cd -> pwd -> claude`, short handshake, 30 second monitoring, and asynchronous project handling.
- `READY_FOR_USER_REVIEW`, `REFERENCE_ONLY`, actual screenshot unavailable, and `WINDOWS_HOST_VALIDATION_PENDING` are soft states, not default terminal states.
- HarmonyOS user-level toolchain cleanup and global package installation are forbidden.
- Visual evidence must be retained under `.outposts-supervisor/visual-evidence`.
- Boundary incidents require read-only recovery reports before migration resumes.

No business source, build scripts, tests, Apple source, build outputs, caches, or project implementation files were intentionally modified by this documentation refresh.
