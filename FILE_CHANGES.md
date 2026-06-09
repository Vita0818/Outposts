# Outposts Mode Upgrade File Notes

本包将 Outposts 工作流升级为三模式：Agent、Spark、OpenCode。

## 建议新增文件

- `OPENCODE_MODE.md`：OpenCode 模式唯一入口，避免读取 Codex/Spark/Agent 调度文档。
- `docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md`：Agent 模式 DeepCode CLI 终端协议。
- `docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md`：Agent/Spark 视觉辅助协议。

## 建议覆盖文件

- `AGENTS.md`
- `docs/DUAL_TRACK_EXECUTION.md`
- `docs/BATCH_SCHEDULING.md`
- `docs/OUTPOSTS_CODEX_SUPERVISOR.md`
- `docs/RECOVERY_PLAYBOOK.md`
- `docs/REPORTING_FORMATS.md`
- `docs/SECURITY_AND_BOUNDARIES.md`
- `docs/DO_NOT_BREAK.md`

## 建议处理旧文件

以下两个旧文件已改为 deprecated stub，避免未来误用 Claude Code：

- `docs/CLAUDE_CODE_TERMINAL_PROTOCOL.md`
- `docs/CLAUDE_CODE_VISUAL_MCP_PROTOCOL.md`

如果仓库中允许删除旧协议文件，可以删掉这两个旧文件；如果担心旧引用残留，保留 stub 更安全。
