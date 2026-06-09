# Deprecated: Claude Code Visual MCP Protocol

本文件已停用。

Outposts Agent 模式已经从 Claude Code 迁移到 **DeepCode CLI**。视觉辅助不再绑定 Claude Code MCP 流程，而是统一作为 Agent 或 Spark 模式下的 `QWEN_VISUAL_ASSIST=YES` 能力。

当前视觉规则请使用：

```text
docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md
```

不得再要求 Claude Code 调用 qwen-vision。Agent 模式下由 DeepCode CLI 使用已配置的 Qwen helper；Spark 模式下由 GPT-5.3-Codex-Spark 使用已配置的 Qwen helper 或读取其报告。

OpenCode 模式不得读取本文件；OpenCode 只读取：

```text
OPENCODE_MODE.md
```
