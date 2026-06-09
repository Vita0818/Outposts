# Deprecated: Claude Code Terminal Protocol

本文件已停用。

Outposts Agent 模式已经从 Claude Code 迁移到 **DeepCode CLI**。当前三模式为：

1. Agent：Codex 主管 + DeepCode CLI 执行。
2. Spark：GPT-5.3-Codex-Spark 本体执行。
3. OpenCode：完全不经过 Codex。

不得再调用 Claude Code，不得启动 `claude`，不得使用 `claude -p` 或 CC 窗口作为正式任务通道。

当前 Agent 模式终端协议请使用：

```text
docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md
```

OpenCode 模式不得读取本文件；OpenCode 只读取：

```text
OPENCODE_MODE.md
```
