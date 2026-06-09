# Outposts Three Mode Execution

本文替代旧“双轨”规则。Outposts 当前只有三种互斥工作模式：**Agent**、**Spark**、**OpenCode**。

旧的 Spark + Qwen 不再是独立模式，而是 Spark 或 Agent 模式中的视觉辅助能力：`QWEN_VISUAL_ASSIST=YES`。

## 三模式总览

| 模式 | 启动入口 | 执行主体 | Codex 是否参与 | 典型任务 |
| --- | --- | --- | --- | --- |
| Agent | Codex 对话 | DeepCode CLI | 是，Codex 只做主管 | 多项目并行、复杂迁移、长程任务 |
| Spark | Codex 对话 | GPT-5.3-Codex-Spark | 是，Codex/Spark 本体直接执行 | 小范围修复、明确构建错误、单文件补丁、局部 UI 修正 |
| OpenCode | 用户直接启动 OpenCode | OpenCode | 否 | 完全独立的项目实现、调试、构建 |

三种模式不可混用。未声明模式时停止执行并要求用户明确。

## Agent 模式定义

用户在命令中明确包含“Agent 模式”时启动。

Agent 模式从 Codex 对话启动，但 Codex 不直接开发。Codex 的职责是：

- 检查 Outposts 根目录、Git root、工作区状态。
- 为每个目标项目启动或接管真实可见 DeepCode CLI 会话。
- 发送短握手，确认模型/后端、路径和 READY 状态。
- 向 DeepCode CLI 发送当前项目、当前轮次、当前目标所需的精简任务 prompt。
- 监测可观察输出，读取结构化报告。
- 维护 `.outposts-supervisor/` 下的 checkpoint、batch state、report、summary。
- 输出主管摘要。

DeepCode CLI 的职责是：

- 读取 Apple 源项目作为只读参考。
- 读取和修改当前 Outposts 目标项目。
- 运行构建、测试、截图、诊断命令。
- 在视觉任务中使用已配置的 Qwen helper（若可用）。
- 输出结构化项目报告。

Agent 模式禁止：

- 调用 Claude Code。
- 使用 `claude`、`claude -p`、Claude Desktop 或 CC 窗口。
- Codex 本体读写业务源码或运行构建测试。
- DeepCode CLI 修改 Apple 源项目或参考图目录。

## Spark 模式定义

用户在命令中明确包含“Spark 模式”时启动。

Spark 模式由 `GPT-5.3-Codex-Spark` 本体直接执行。执行前必须确认当前模型：

```text
MODEL_CHECK_RESULT:
- Expected: GPT-5.3-Codex-Spark
- Actual: <当前模型>
- Result: PASS/FAIL
```

无法确认或不匹配时停止。

Spark 允许：

- 在目标项目内读取和修改源码。
- 在目标项目内运行构建、测试、lint、截图、最小诊断命令。
- 写调度记录、patch summary、验证报告。

Spark 禁止：

- 修改 `/Users/vita/Vitemis/Vela`。
- 修改参考图目录。
- 读取或发送敏感文件。
- 执行破坏性 Git 操作。
- 清理 build/cache/用户级工具链。
- 在模型不匹配时继续。

## Spark 视觉辅助

Spark 任务若涉及 UI、截图、视觉、reference、actual、像素级差异、设计稿或视觉验收，设置：

```text
MODE=SPARK
QWEN_VISUAL_ASSIST=YES
```

规则：

- Qwen 只负责看图、识别截图、比较截图。
- Spark 负责代码修改、构建、测试和总结。
- 没有 Qwen 报告时，不得声称完成视觉验收。
- 只有 reference 无 actual 时，报告 `REFERENCE_ONLY`。
- actual 和 reference 都有效时，才可报告 `QWEN_COMPARE_SCREENSHOTS_COMPLETED=YES`。

## Agent 视觉辅助

Agent 模式的视觉辅助由 DeepCode CLI 发起。Codex 只在任务 prompt 中要求 DeepCode CLI 使用已配置的 Qwen helper。

```text
MODE=AGENT
EXECUTOR=DeepCode CLI
QWEN_VISUAL_ASSIST=YES/NO
```

DeepCode CLI 不得把源码、密钥、`.env`、证书或完整私密配置传给 Qwen。

如果 DeepCode CLI 无可用 Qwen helper：

- 纯代码任务可继续。
- 视觉验收任务不得宣称视觉闭环完成。
- 报告 `QWEN_UNAVAILABLE_IN_SESSION` 或 `QWEN_HELPER_NOT_CONFIGURED`。

## OpenCode 模式定义

OpenCode 模式完全与 Codex 无关。用户直接在目标项目启动 OpenCode。

OpenCode 模式只读取：

- `OPENCODE_MODE.md`
- 当前目标项目自己的项目文档

OpenCode 模式不得读取：

- `AGENTS.md`
- `docs/OUTPOSTS_CODEX_SUPERVISOR.md`
- `docs/DUAL_TRACK_EXECUTION.md`
- `docs/BATCH_SCHEDULING.md`
- `docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md`
- `docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md`
- `docs/RECOVERY_PLAYBOOK.md`
- `docs/REPORTING_FORMATS.md`
- `docs/DO_NOT_BREAK.md`
- 任何 Codex/Spark/Agent 调度状态、checkpoint、report、summary

OpenCode 模式仍遵守：Apple 项目只读作参考；构建 Android、HarmonyOS、Windows 目标项目；不得修改 `/Users/vita/Vitemis/Vela`；不得读取敏感信息；不得执行破坏性 Git 操作。

## 触发词

Agent 触发词：

- Agent 模式
- 主管模式
- 调度 DeepCode
- DeepCode CLI
- Codex 主管

Spark 触发词：

- Spark 模式
- 使用 Spark
- GPT-5.3-Codex-Spark
- Codex 本体直接改

OpenCode 触发词：

- OpenCode 模式
- 用 OpenCode
- 直接让 OpenCode 做
- 不走 Codex

旧触发词处理：

- “调度 Claude Code”“CC 窗口”“Claude Code 主 Agent”属于旧协议用语；不得实际调用 Claude Code。
- 若用户只说旧触发词且没有明确 DeepCode，回复说明当前 Agent 模式已迁移到 DeepCode CLI，并要求确认或按 DeepCode CLI 执行。

## 模式选择表

| 任务类型 | 推荐模式 |
| --- | --- |
| 单文件小修 | Spark |
| 明确构建错误小修 | Spark |
| 单元测试补丁 | Spark |
| 小范围 UI 修正 | Spark + Qwen 视觉辅助 |
| 多项目并行 | Agent |
| 跨平台迁移 | Agent |
| 长时间自主执行 | Agent |
| 需要外部 CLI 深读项目 | Agent |
| 完全脱离 Codex 的项目实现 | OpenCode |
| 不希望 OpenCode 看到 Codex/Spark 规则 | OpenCode |

## 报告模板索引

- Spark：见 `REPORTING_FORMATS.md` 的 `MODE: SPARK`。
- Spark 视觉：仍为 `MODE: SPARK`，增加 `QWEN_VISUAL_ASSIST=YES` 字段。
- Agent：见 `REPORTING_FORMATS.md` 的 `MODE: AGENT`，执行器字段为 DeepCode CLI。
- OpenCode：见 `OPENCODE_MODE.md`，不走 Codex 主管摘要。
