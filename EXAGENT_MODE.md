# Outposts ExAgent Mode

本文件是 ExAgent 模式入口。

ExAgent 模式由一个 OpenCode 线程发起。它与 Agent 模式的唯一差异是发起者：Agent 由 Codex 对话发起，ExAgent 由 OpenCode 线程发起。除此以外，执行器、路径检查、模型检查、终端协议、预算、恢复、报告、安全边界均与 Agent 模式一致。

## 触发条件

满足任一条件即进入 ExAgent 模式：

- 用户明确写“ExAgent 模式”。
- 用户明确写“OpenCode 线程发起 Agent”。
- 用户明确写“由 OpenCode 发起 DeepCode 调度”。
- 用户明确写“OpenCode 主管 + DeepCode CLI”。

如果用户只是说“OpenCode 模式”，不等于 ExAgent；应使用 `OPENCODE_MODE.md`。

## 基本结构

```text
INITIATOR=OpenCode_THREAD
MODE=EXAGENT
SUPERVISOR=OpenCode_THREAD
EXECUTOR=DeepCode CLI
```

ExAgent 中，OpenCode 线程承担 supervisor 职责：

- 执行 Outposts 根目录启动前检查。
- 为每个目标项目建立独立真实可见或可观察的 DeepCode CLI 会话。
- 发送短握手并记录模型、路径、READY 状态。
- 向 DeepCode CLI 发送当前项目、当前轮次、当前目标的精简 prompt。
- 读取 DeepCode CLI 结构化报告。
- 维护批次状态、checkpoint、summary、report。
- 生成主管摘要。

OpenCode 线程在 ExAgent 中不得承担实现者职责：

- 不得读取业务源码。
- 不得修改业务源码。
- 不得运行目标项目构建、测试或 lint。
- 不得查看具体业务 diff。
- 不得把 DeepCode 自评当成用户验收。

## 必读顺序

ExAgent 线程必须按顺序阅读：

1. `EXAGENT_MODE.md`
2. `docs/OUTPOSTS_SUPERVISOR.md`
3. `docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md`
4. `docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md`
5. `docs/BATCH_SCHEDULING.md`
6. `docs/SECURITY_AND_BOUNDARIES.md`
7. `docs/RECOVERY_PLAYBOOK.md`
8. `docs/REPORTING_FORMATS.md`
9. `docs/DO_NOT_BREAK.md`

ExAgent 线程不要读取 `OPENCODE_MODE.md` 的独立执行规则，也不要把 Spark 规则混入 ExAgent 的 DeepCode prompt。

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

除非用户另行指定，每个项目独立 DeepCode CLI 会话、独立状态、独立轮次、独立阻塞原因。

## DeepCode 启动

每个项目固定启动序列：

```bash
cd <PROJECT_PATH>
pwd
deepcode
```

若本机命令名不是 `deepcode`，必须在批次参数中记录：

```text
DEEPCODE_CLI_COMMAND=<实际命令>
```

不得在 Outposts 根目录直接启动子项目正式任务。

## 短握手

每轮正式任务前必须先发送：

```text
[H]
只回一行，不读文件，不改文件，不构建，不测试：
MODEL=<当前模型或 DeepCode 当前后端>; PWD=<当前工作目录>; READY=<YES/NO>
```

短握手失败、模型不匹配、路径不匹配、READY=NO 均不计入有效轮次。

## 批次参数

每个 ExAgent 批次必须明确：

```text
MODE=EXAGENT
INITIATOR=OpenCode_THREAD
SUPERVISOR=OpenCode_THREAD
EXECUTOR=DeepCode CLI
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
EXPECTED_DEEPCODE_MODEL
```

若用户没有指定 `EXPECTED_DEEPCODE_MODEL`，使用 Outposts 默认预期：DeepSeek V4 Pro 系列；若 DeepCode CLI 无法准确报告当前后端，必须在主管摘要中标为不确定项。

## 报告格式

ExAgent 最终主管摘要应包含：

```text
MODE: EXAGENT
INITIATOR: OpenCode_THREAD
MODEL_CHECK_RESULT:
PATH_CHECK_RESULT:
DEEPCODE_TERMINALS:
PROJECT_REPORTS:
ROUNDS_COMPLETED:
BLOCKERS:
SUPERVISOR_SUMMARY:
SCOPE_CONFIRMATION:
NEXT_ACTION:
UNCERTAINTIES:
```

用户只需要看主管摘要，不需要 DeepCode CLI 长报告。
