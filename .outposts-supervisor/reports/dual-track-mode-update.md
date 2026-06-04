# Dual Track Mode Rules Update

## 本轮动作

- 审核并更新 Outposts 调度规则文档，仅覆盖允许清单内文件。
- 新增 `docs/DUAL_TRACK_EXECUTION.md`，明确 Spark / Agent 双轨定义、触发词、边界、报告格式与错误处理。
- 为 `AGENTS.md`、`docs/OUTPOSTS_CODEX_SUPERVISOR.md`、`docs/BATCH_SCHEDULING.md`、`docs/SECURITY_AND_BOUNDARIES.md`、`docs/REPORTING_FORMATS.md`、`docs/DO_NOT_BREAK.md` 增补双轨模式约束。

## 哪些文件更新

- `/Users/vita/Vitemis/Outposts/AGENTS.md`
- `/Users/vita/Vitemis/Outposts/docs/DUAL_TRACK_EXECUTION.md`
- `/Users/vita/Vitemis/Outposts/docs/OUTPOSTS_CODEX_SUPERVISOR.md`
- `/Users/vita/Vitemis/Outposts/docs/BATCH_SCHEDULING.md`
- `/Users/vita/Vitemis/Outposts/docs/SECURITY_AND_BOUNDARIES.md`
- `/Users/vita/Vitemis/Outposts/docs/REPORTING_FORMATS.md`
- `/Users/vita/Vitemis/Outposts/docs/DO_NOT_BREAK.md`
- `/Users/vita/Vitemis/Outposts/.outposts-supervisor/reports/dual-track-mode-update.md`

## Spark 模式规则（已固化）

- 需要明确模式声明：`Spark 模式` / `使用 Spark` / `Codex 本体直接改` 等。
- 执行前强制 `MODEL_CHECK_RESULT`，当前模型必须为 `GPT-5.3-Codex-Spark`。
- 仅在确认后，Codex 可直接读写 Outposts 目标项目代码、运行命令并提交改动。
- 禁止改 `/Users/vita/Vitemis/Vela`、参考图目录及执行危险 Git 操作。

## Agent 模式规则（已固化）

- 需要明确模式声明：`Agent 模式` / `主管模式` / `调度 Claude Code` / `CC 窗口` / `DeepSeek + Qwen`。
- Codex 仅负责调度与汇总，不直接读写源码，不直接构建测试。
- Claude Code 负责实际源码阅读、修改、构建、测试及视觉/截图流程。

## 后续用户发命令方式

请每次新任务明确模式，示例：

- `Spark 模式：...`
- `Agent 模式：...`

## 未修改项确认

- 本轮未修改任何业务源码、构建脚本、测试源码。
- 本轮未启动迁移任务。
- 本轮未运行构建或测试命令。
