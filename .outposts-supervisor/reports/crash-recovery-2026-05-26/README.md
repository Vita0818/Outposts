# Crash Recovery Reports - 2026-05-26

本目录记录当前 Codex Agent 对话中的 Outposts crash recovery 只读恢复报告阶段。

执行边界：

1. Codex Agent 只调度 Claude Code，不读取项目源码、不查看 diff、不运行构建或测试。
2. Claude Code 按用户授权读取限定路径并输出只读恢复报告。
3. 每个项目使用独立 Claude Code 会话。
4. 每个项目正式恢复报告前必须先通过短握手。
5. 完成五个项目恢复报告后暂停，不启动正式迁移。

