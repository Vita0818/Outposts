# Outposts Supervisor Context

本目录是 Outposts 根目录。

本文件服务于从 Codex 对话启动的 Outposts 工作流。OpenCode 独立模式不要读取本文件；OpenCode 独立模式只读取 `OPENCODE_MODE.md` 与目标项目自己的项目文档。由 OpenCode 线程发起的 ExAgent 模式读取 `EXAGENT_MODE.md`。

## 四种互斥模式

Outposts 支持四种互斥模式：

1. **Agent 模式**：从 Codex 对话发起；Codex 只做主管；实际执行器为 DeepCode CLI。
2. **ExAgent 模式**：从 OpenCode 线程发起；除发起者为 OpenCode 线程外，其余要求、配置、执行器、预算、报告、安全边界均与 Agent 模式一致。
3. **Spark 模式**：从 Codex 对话发起；由 `GPT-5.3-Codex-Spark` 本体直接读取、修改、构建和验证目标项目。
4. **OpenCode 模式**：OpenCode 独立执行；不进入 Codex/ExAgent supervisor 流程，不使用 DeepCode CLI 执行器，不读取 Agent/Spark/ExAgent 调度文档。

用户未明确声明模式时，不得猜测执行；必须要求用户明确 `Agent 模式`、`ExAgent 模式`、`Spark 模式` 或 `OpenCode 模式`。

## Agent 模式

### 触发条件

满足任一条件即视为 Agent 模式：

- 用户命令明确包含“Agent 模式”。
- 用户明确写“主管模式”。
- 用户明确写“Codex 调度”。
- 用户明确写“调度 DeepCode”。
- 用户明确写“DeepCode CLI”。

### 执行器

Agent 模式的正式执行器是 DeepCode CLI。每个目标项目使用独立真实可见或可观察的 DeepCode CLI 会话。

Codex 在 Agent 模式下只做主管：

- 允许：执行 Outposts 根目录启动前检查。
- 允许：启动、观察、恢复 DeepCode CLI 会话。
- 允许：给 DeepCode CLI 发送当前项目、当前轮次、当前目标所需的精简 prompt。
- 允许：读取 DeepCode CLI 的结构化报告并生成主管摘要。
- 禁止：Codex 本体读取业务源码。
- 禁止：Codex 本体修改业务源码。
- 禁止：Codex 本体运行目标项目构建、测试或 lint。
- 禁止：Codex 本体查看具体业务 diff。
- 禁止：Codex 本体把 DeepCode 自评当成用户验收。

DeepCode CLI 在每个目标项目自己的终端中执行：

- 读取目标项目源码。
- 按授权只读参考 Apple 源项目。
- 修改 Outposts 目标项目。
- 运行目标项目构建、测试、lint、截图与诊断命令。
- 在 UI / 视觉任务中调用或读取已配置的 Qwen 视觉 helper。
- 输出结构化报告。

## ExAgent 模式

ExAgent 模式由 OpenCode 线程发起。它与 Agent 模式的唯一差异是发起者：

- Agent：发起者是 Codex 对话。
- ExAgent：发起者是 OpenCode 线程。

除此以外，ExAgent 的执行器、路径检查、模型检查、DeepCode CLI 终端协议、视觉辅助、预算、恢复、报告、安全边界与 Agent 模式完全一致。

ExAgent 入口是 `EXAGENT_MODE.md`。OpenCode 线程进入 ExAgent 时不使用 `OPENCODE_MODE.md` 的独立执行规则。

## Spark 模式

### 触发条件

满足任一条件即视为 Spark 模式：

- 用户命令明确包含“Spark 模式”。
- 用户明确写“使用 Spark”。
- 用户明确写“由 GPT-5.3-Codex-Spark 执行”。
- 用户明确写“Codex 本体直接改”。

### 执行前强制检查

1. 必须输出 `MODEL_CHECK_RESULT`。
2. 当前模型必须确认是 `GPT-5.3-Codex-Spark`。
3. 无法确认模型时必须停止本轮。
4. 不得用其他模型替代 Spark。

### Spark 权限

Spark 模式中，Codex/Spark 本体是实现者：

- 允许：读取当前目标项目源码。
- 允许：修改 `/Users/vita/Vitemis/Outposts` 下当前目标项目文件。
- 允许：在当前目标项目内运行构建、测试、lint、截图、校验命令。
- 允许：写调度记录与报告。
- 禁止：修改 `/Users/vita/Vitemis/Vela`。
- 禁止：修改参考图目录。
- 禁止：访问无关目录。
- 禁止：读取、发送或记录密钥、token、`.env`、证书、ssh key、Keychain 内容等敏感信息。
- 禁止：执行破坏性 Git 操作。
- 禁止：清理工作区、构建产物、缓存或用户级工具链。
- 禁止：伪装为 Agent 或 ExAgent 执行。

### Spark 视觉辅助

若 Spark 任务涉及 UI、截图、视觉、像素级差异、reference/actual、设计稿或界面复刻，则 Spark 模式内启用：

```text
QWEN_VISUAL_ASSIST=YES
```

要求：

- Spark 不得主观判读截图并宣布视觉验收完成。
- 代码修改必须基于 Qwen 视觉报告、有效截图证据或用户明确反馈。
- reference 与 actual 均有效时必须做 compare。
- actual 不可用时只能报告 `REFERENCE_ONLY`，不得声称完成视觉闭环。

## OpenCode 模式

OpenCode 模式完全不由 Codex 启动、调度或恢复。

OpenCode 模式必须满足：

- 用户直接启动 OpenCode 独立任务。
- OpenCode 不读取本文件。
- OpenCode 不读取 `EXAGENT_MODE.md` 或 `docs/` 下的 supervisor / Agent / Spark / ExAgent 协议。
- OpenCode 只读取 `OPENCODE_MODE.md` 与当前目标项目自己的项目文档。
- OpenCode 不使用 supervisor checkpoint、batch state、DeepCode CLI 会话或 Spark 模型检查。

OpenCode 独立模式仍遵守项目边界：Apple 项目只读参考；只构建或修改 Android、HarmonyOS、Windows 目标项目；不得写入 `/Users/vita/Vitemis/Vela`；不得读取敏感信息；不得执行破坏性 Git 操作。

## 启动前检查

Agent 或 Spark 模式进入 Outposts 根目录后，必须先执行并记录：

```bash
pwd
git rev-parse --show-toplevel
git status --short
```

只有当 `pwd` 与 `git rev-parse --show-toplevel` 都指向 `/Users/vita/Vitemis/Outposts` 时，才允许继续调度或更新本目录级调度文档。若不匹配，停止修改并报告路径问题。

不得执行破坏性 Git 操作，包括 `git reset --hard`、`git clean -fd`、`git checkout .`、`git restore .`、强制 push、删除用户未提交文件。不得 commit、push、创建 PR，除非用户明确要求。

## Codex 模式必读顺序

Agent 或 Spark 模式下，Codex 必须按顺序阅读：

1. `AGENTS.md`
2. `docs/OUTPOSTS_MODE_EXECUTION.md`
3. `docs/OUTPOSTS_SUPERVISOR.md`
4. `docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md`
5. `docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md`
6. `docs/BATCH_SCHEDULING.md`
7. `docs/SECURITY_AND_BOUNDARIES.md`
8. `docs/RECOVERY_PLAYBOOK.md`
9. `docs/REPORTING_FORMATS.md`
10. `docs/DO_NOT_BREAK.md`

这些文档给 Codex 读，用于形成调度判断。Codex 可以根据这些文档生成给 DeepCode CLI 的当前项目、当前轮次、当前目标所需的精简 prompt，但不得把整套调度文档粗暴粘给 DeepCode CLI。

OpenCode 独立模式不得使用上述顺序；OpenCode 独立模式只读取 `OPENCODE_MODE.md`。

ExAgent 模式使用 `EXAGENT_MODE.md` 规定的顺序。

## Agent 模式 DeepCode 启动顺序

每个目标项目必须使用独立 DeepCode CLI 会话。启动顺序固定：

```bash
cd <PROJECT_PATH>
pwd
deepcode
```

`pwd` 必须在启动 DeepCode 前执行，并且必须与目标项目路径一致。若本机 DeepCode CLI 命令名不是 `deepcode`，必须在批次参数里记录 `DEEPCODE_CLI_COMMAND=<实际命令>`。

进入 DeepCode 后，每轮正式任务前必须先发送短握手：

```text
[H]
只回一行，不读文件，不改文件，不构建，不测试：
MODEL=<当前模型或 DeepCode 当前后端>; PWD=<当前工作目录>; READY=<YES/NO>
```

短握手不计入正式轮次。

## 批处理预算

每个批次必须明确：

```text
MODE
BATCH_NAME
CONCURRENCY
BATCH_TIME_BUDGET_MINUTES
MAX_REPORT_ROUNDS_PER_PROJECT
STOP_MODE
AUTO_CONTINUE_WITHIN_BUDGET
NO_NEW_ROUNDS_AFTER_TIME_BUDGET
WAIT_RUNNING_ROUNDS_TO_FINISH
VISION_VALIDATION_MAX_ROUNDS
```

Agent / ExAgent 轮次按 DeepCode CLI 返回一次完整结构化报告计数。Spark 轮次按一次完整直接修改、验证和结构化报告计数。OpenCode 独立模式不计入 supervisor batch state。

时间预算是软限制。时间到达后不得启动新轮，但不得强杀正在正常运行的任务。轮次预算到达后同样不再启动新轮。

## 路径与安全边界

固定根目录：

- Apple 源项目根目录只读：`/Users/vita/Vitemis/Vela`
- Outposts 根目录：`/Users/vita/Vitemis/Outposts`

不得读取、发送或记录密钥、token、私钥、证书、`.env`、p12、provisioning profile、ssh key、API key、Keychain 内容等敏感信息。

## 最终报告要求

Codex 输出给用户的是主管摘要，不是 DeepCode CLI 长报告。

最终报告至少包含：

```text
MODE
MODEL_CHECK_RESULT
PATH_CHECK_RESULT
SCOPE_CONFIRMATION
FILES_WRITTEN_OR_STATUS_FILES
VALIDATION_RESULT
UNCERTAINTIES
NEXT_RECOMMENDED_ACTION
```

Agent 模式还需包含：

```text
DEEPCODE_TERMINALS
PROJECT_REPORTS
ROUNDS_COMPLETED
BLOCKERS
SUPERVISOR_SUMMARY
```

Spark 视觉任务还需包含：

```text
QWEN_VISUAL_ASSIST
QWEN_MODEL
QWEN_AVAILABLE
QWEN_CALL_METHOD
REFERENCE_SCREENSHOTS
ACTUAL_SCREENSHOTS
QWEN_INSPECT_REFERENCE_RESULT
QWEN_INSPECT_ACTUAL_RESULT
QWEN_COMPARE_RESULT
CODE_CHANGES_FROM_QWEN
REMAINING_VISUAL_DIFFERENCES
```
