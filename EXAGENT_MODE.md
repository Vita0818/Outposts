# Outposts ExAgent Mode

本文件是 ExAgent 模式入口。

ExAgent 模式由一个 OpenCode 线程发起。它与 Agent 模式的唯一差异是发起者：Agent 由 Codex 对话发起，ExAgent 由 OpenCode 线程发起。除此以外，worker、路径检查、模型检查、one-shot 调用协议、预算、恢复、报告、安全边界均与 Agent 模式一致。

## 触发条件

满足任一条件即进入 ExAgent 模式：

- 用户明确写“ExAgent 模式”。
- 用户明确写“OpenCode 线程发起 Agent”。
- 用户明确写“由 OpenCode 发起 DeepCode / QwenCode 调度”。
- 用户明确写“OpenCode 主管 + DeepCode / QwenCode”。

如果用户只是说“OpenCode 模式”，不等于 ExAgent；应使用 `OPENCODE_MODE.md`。

## 基本结构

```text
INITIATOR=OpenCode_THREAD
MODE=EXAGENT
SUPERVISOR=OpenCode_THREAD
IMPLEMENTATION_WORKER=DeepCode_ONE_SHOT
VISION_WORKER=QwenCode_ONE_SHOT
WORKER_INTERACTION=FORBIDDEN
STATE_TRANSFER=SUPERVISOR_MANAGED_FILES
```

ExAgent 中，OpenCode 线程承担 supervisor 职责：

- 执行 Outposts 根目录启动前检查。
- 按轮次启动真实可见或可观察的一次性 DeepCode 窗口。
- 按轮次启动真实可见或可观察的一次性 QwenCode 窗口。
- 向每个 worker 提交完整 one-shot prompt。
- 指定每个 worker 的唯一输出文件路径。
- 读取 `DeepCode-output/` 与 `QwenCode-output/` 中的结构化报告。
- 从 DeepCode 报告中定位 screenshot 路径，再交给 QwenCode。
- 把上一轮或本轮 QwenCode 视觉报告路径提供给下一轮 DeepCode 输入。
- 维护批次状态、checkpoint、summary、report。
- 生成主管摘要。

OpenCode 线程在 ExAgent 中不得承担实现者或视觉执行者职责：

- 不得读取业务源码。
- 不得修改业务源码。
- 不得运行目标项目构建、测试或 lint。
- 不得查看具体业务 diff。
- 不得直接判读截图。
- 不得把 DeepCode / QwenCode 自评当成用户验收。

## Worker 隔离规则

DeepCode 和 QwenCode 都是一次性干活 Agent，不是互相可调用的工具。它们之间不得直接通信：

```text
DeepCode -> QwenCode: FORBIDDEN
QwenCode -> DeepCode: FORBIDDEN
DeepCode -> vision helper: FORBIDDEN
QwenCode -> source code: FORBIDDEN
QwenCode -> DeepCode-output: FORBIDDEN
```

所有请求必须由 supervisor 发起。所有跨轮上下文必须由 supervisor 以文件路径形式显式传入。

## 必读顺序

ExAgent 线程必须按顺序阅读：

1. `EXAGENT_MODE.md`
2. `docs/OUTPOSTS_MODE_EXECUTION.md`
3. `docs/OUTPOSTS_SUPERVISOR.md`
4. `docs/WORKER_ONE_SHOT_INVOCATION_PROTOCOL.md`
5. `docs/SUPERVISOR_WORKER_VISUAL_PROTOCOL.md`
6. `docs/BATCH_SCHEDULING.md`
7. `docs/SECURITY_AND_BOUNDARIES.md`
8. `docs/RECOVERY_PLAYBOOK.md`
9. `docs/REPORTING_FORMATS.md`
10. `docs/DO_NOT_BREAK.md`

ExAgent 线程不要读取 `OPENCODE_MODE.md` 的独立执行规则，也不要把 Spark 直接实现规则混入 ExAgent 的 DeepCode / QwenCode prompt。

## 启动前检查

进入 `/Users/vita/Vitemis/Outposts` 后必须执行并记录：

```bash
pwd
git rev-parse --show-toplevel
git status --short
```

只有当 `pwd` 与 `git rev-parse --show-toplevel` 都指向 `/Users/vita/Vitemis/Outposts` 时，才允许继续调度或更新 supervisor 状态文件。

## 目标项目

默认目标项目：

```text
/Users/vita/Vitemis/Outposts/Kikaria-Android
/Users/vita/Vitemis/Outposts/Kikaria-HarmonyOS
/Users/vita/Vitemis/Outposts/Rokurics-Android
/Users/vita/Vitemis/Outposts/Rokurics-HarmonyOS
/Users/vita/Vitemis/Outposts/Rokurics-Windows
```

除非用户另行指定，每个项目独立状态、独立轮次、独立阻塞原因。每轮使用新的 DeepCode / QwenCode 一次性窗口，不复用窗口上下文。

## One-shot 调用

每轮调用必须采用“窗口启动即执行完整 prompt”的方式。不得进入 DeepCode 或 QwenCode 后再追加业务指令。

DeepCode 调用模板由批次参数记录：

```text
DEEPCODE_CLI_COMMAND=<实际命令>
DEEPCODE_ONE_SHOT_INVOCATION=<实际窗口/命令/prompt 注入方式>
```

QwenCode 调用模板由批次参数记录：

```text
QWENCODE_CLI_COMMAND=<实际命令>
QWENCODE_ONE_SHOT_INVOCATION=<实际窗口/命令/prompt 注入方式>
QWENCODE_MODEL_EXPECTED=Qwen3.7-Plus
```

如果本机命令名、参数或窗口唤起方式不确定，必须先记录为 `MANUAL_DECISION_REQUIRED`，不得猜命令。

## 输出目录

DeepCode 输出文件必须由 supervisor 指定：

```text
<PROJECT_PATH>/DeepCode-output/<BATCH_NAME>/
```

QwenCode 输出文件必须由 supervisor 指定：

```text
<PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/
```

文件名必须唯一，推荐：

```text
round-001-reference-inspect.md
round-001-implement.md
round-001-actual-compare.md
round-002-fix-from-qwen.md
```

## QwenCode 报告传递

DeepCode 不能自行发起 QwenCode。DeepCode 只能读取 supervisor 在 prompt 中明确给出的 QwenCode 报告路径。

下一轮 DeepCode prompt 必须显式写：

```text
PREVIOUS_QWENCODE_REPORTS:
- <absolute path to QwenCode-output/*.md>
```

DeepCode 输出必须包含：

```text
QWENCODE_REPORTS_REQUESTED_BY_SUPERVISOR:
QWENCODE_REPORTS_READ:
QWENCODE_FINDINGS_USED:
QWENCODE_REPORT_READ_FAILURES:
```

若未能读取指定报告，DeepCode 必须报告失败，不得声称已根据 Qwen 结果修改。

## 批次参数

每个 ExAgent 批次必须明确：

```text
MODE=EXAGENT
INITIATOR=OpenCode_THREAD
SUPERVISOR=OpenCode_THREAD
IMPLEMENTATION_WORKER=DeepCode_ONE_SHOT
VISION_WORKER=QwenCode_ONE_SHOT
WORKER_INTERACTION=FORBIDDEN
BATCH_NAME
CONCURRENCY
BATCH_TIME_BUDGET_MINUTES
MAX_REPORT_ROUNDS_PER_PROJECT
STOP_MODE
AUTO_CONTINUE_WITHIN_BUDGET
NO_NEW_ROUNDS_AFTER_TIME_BUDGET
WAIT_RUNNING_ROUNDS_TO_FINISH
VISION_VALIDATION_MAX_ROUNDS
DEEPCODE_CLI_COMMAND
DEEPCODE_ONE_SHOT_INVOCATION
EXPECTED_DEEPCODE_MODEL
QWENCODE_CLI_COMMAND
QWENCODE_ONE_SHOT_INVOCATION
QWENCODE_MODEL_EXPECTED=Qwen3.7-Plus
```

若用户没有指定 `EXPECTED_DEEPCODE_MODEL`，使用 Outposts 默认预期：DeepSeek V4 Pro 系列；若 DeepCode 无法准确报告当前后端，必须在主管摘要中标为不确定项。

## 报告格式

ExAgent 最终主管摘要应包含：

```text
MODE: EXAGENT
INITIATOR: OpenCode_THREAD
MODEL_CHECK_RESULT:
PATH_CHECK_RESULT:
DEEPCODE_OUTPUTS:
QWENCODE_OUTPUTS:
ROUNDS_COMPLETED:
VISION_ROUNDS_COMPLETED:
BLOCKERS:
SUPERVISOR_SUMMARY:
SCOPE_CONFIRMATION:
NEXT_ACTION:
UNCERTAINTIES:
```

用户只需要看主管摘要，不需要 DeepCode / QwenCode 长报告。
