# Outposts Supervisor Context

本目录是 Outposts 根目录。

本文件服务于从 Codex 对话启动的 Outposts 工作流。OpenCode 独立模式不要读取本文件；OpenCode 独立模式只读取 `OPENCODE_MODE.md` 与目标项目自己的项目文档。由 OpenCode 线程发起的 ExAgent 模式读取 `EXAGENT_MODE.md`。

## 四种互斥模式

Outposts 支持四种互斥模式：

1. **Agent 模式**：从 Codex 对话发起；Codex 只做 supervisor；由 supervisor 独立调度 DeepCode one-shot 与 QwenCode one-shot。
2. **ExAgent 模式**：从 OpenCode 线程发起；除发起者为 OpenCode 线程外，其余要求、配置、worker、预算、报告、安全边界均与 Agent 模式一致。
3. **Spark 模式**：从 Codex 对话发起；由 `GPT-5.3-Codex-Spark` 本体直接读取、修改、构建和验证目标项目。
4. **OpenCode 模式**：OpenCode 独立执行；不进入 supervisor 流程，不使用 DeepCode / QwenCode 调度链，不读取 Agent / Spark / ExAgent 调度文档。

用户未明确声明模式时，不得猜测执行；必须要求用户明确 `Agent 模式`、`ExAgent 模式`、`Spark 模式` 或 `OpenCode 模式`。

## Agent 模式

### 触发条件

满足任一条件即视为 Agent 模式：

- 用户命令明确包含“Agent 模式”。
- 用户明确写“主管模式”。
- 用户明确写“Codex 调度”。
- 用户明确写“调度 DeepCode / QwenCode”。
- 用户明确写“DeepCode / QwenCode 批处理”。

### 核心结构

Agent 模式不是 DeepCode 会话管理模式，而是 supervisor 对两个并列 worker 的文件化调度模式：

```text
INITIATOR=Codex
SUPERVISOR=Codex
IMPLEMENTATION_WORKER=DeepCode_ONE_SHOT
VISION_WORKER=QwenCode_ONE_SHOT
WORKER_INTERACTION=FORBIDDEN
STATE_TRANSFER=SUPERVISOR_MANAGED_FILES
```

Codex 在 Agent 模式下只做 supervisor：

- 允许：执行 Outposts 根目录启动前检查。
- 允许：按批次预算启动真实可见或可观察的一次性 DeepCode / QwenCode 窗口。
- 允许：给 DeepCode / QwenCode 提交当前项目、当前轮次、当前目标所需的完整 one-shot prompt。
- 允许：读取 `DeepCode-output/` 与 `QwenCode-output/` 中的结构化报告。
- 允许：从 DeepCode 报告中定位 actual screenshot 路径，再交给 QwenCode。
- 允许：把 QwenCode 视觉报告文件路径提供给下一轮 DeepCode prompt。
- 允许：维护 supervisor batch state、checkpoint、summary、report。
- 禁止：Codex 本体读取业务源码。
- 禁止：Codex 本体修改业务源码。
- 禁止：Codex 本体运行目标项目构建、测试或 lint。
- 禁止：Codex 本体查看具体业务 diff。
- 禁止：Codex 本体直接判读截图或替代 QwenCode 做视觉结论。
- 禁止：Codex 本体把 DeepCode 或 QwenCode 自评当成用户验收。

### Worker 间隔离

DeepCode 与 QwenCode 均为一次性干活 worker。它们之间不得直接通信：

```text
FORBIDDEN:
DeepCode -> QwenCode
QwenCode -> DeepCode
DeepCode -> Qwen helper / vision helper
QwenCode -> DeepCode-output
QwenCode -> source code
```

所有请求都必须来自 supervisor。所有上下文、输出路径、截图路径、上一轮报告路径都由 supervisor 显式拼入下一次 one-shot prompt。

DeepCode 只负责：

- 按 supervisor prompt 读取目标项目源码。
- 按授权只读参考 Apple 源项目。
- 修改 Outposts 目标项目。
- 运行目标项目构建、测试、lint、截图与诊断命令。
- 读取 supervisor 明确传入的 `QwenCode-output/*.md` 视觉报告文件。
- 输出结构化报告到 `DeepCode-output/`。

QwenCode 只负责：

- 使用 Qwen3.7-Plus 读取 supervisor 传入的截图路径。
- 分析 reference screenshot。
- 分析 actual screenshot。
- 在 reference 与 actual 均有效时执行视觉对比。
- 输出结构化视觉报告到 `QwenCode-output/`。

QwenCode 不得读取源码、不得修改文件、不得读取 DeepCode 报告、不得接收密钥或私密配置。

## ExAgent 模式

ExAgent 模式由 OpenCode 线程发起。它与 Agent 模式的唯一差异是发起者：

- Agent：发起者是 Codex 对话；supervisor 是 Codex。
- ExAgent：发起者是 OpenCode 线程；supervisor 是 OpenCode 线程。

除此以外，ExAgent 的 one-shot worker、路径检查、模型检查、视觉调度、预算、恢复、报告、安全边界与 Agent 模式完全一致。

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
QWENCODE_VISUAL_ASSIST=YES
```

要求：

- Spark 不得主观判读截图并宣布视觉验收完成。
- 视觉结论必须来自 QwenCode 输出报告、有效截图证据或用户明确反馈。
- reference 与 actual 均有效时必须做 compare。
- actual 不可用时只能报告 `REFERENCE_ONLY`，不得声称完成视觉闭环。

## OpenCode 模式

OpenCode 模式完全不由 Codex 启动、调度或恢复。

OpenCode 模式必须满足：

- 用户直接启动 OpenCode 独立任务。
- OpenCode 不读取本文件。
- OpenCode 不读取 `EXAGENT_MODE.md` 或 `docs/` 下的 supervisor / Agent / Spark / ExAgent 协议。
- OpenCode 只读取 `OPENCODE_MODE.md` 与当前目标项目自己的项目文档。
- OpenCode 不使用 supervisor checkpoint、batch state、DeepCode-output、QwenCode-output 或 Spark 模型检查。

OpenCode 独立模式仍遵守项目边界：Apple 项目只读参考；只构建或修改 Android、HarmonyOS、Windows 目标项目；不得写入 `/Users/vita/Vitemis/Vela`；不得读取敏感信息；不得执行破坏性 Git 操作。

## 启动前检查

Agent、ExAgent 或 Spark 模式进入 Outposts 根目录后，必须先执行并记录：

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
4. `docs/WORKER_ONE_SHOT_INVOCATION_PROTOCOL.md`
5. `docs/SUPERVISOR_WORKER_VISUAL_PROTOCOL.md`
6. `docs/BATCH_SCHEDULING.md`
7. `docs/SECURITY_AND_BOUNDARIES.md`
8. `docs/RECOVERY_PLAYBOOK.md`
9. `docs/REPORTING_FORMATS.md`
10. `docs/DO_NOT_BREAK.md`

这些文档给 Codex 读，用于形成调度判断。Codex 可以根据这些文档生成给 DeepCode / QwenCode 的当前项目、当前轮次、当前目标所需的精简 prompt，但不得把整套调度文档粗暴粘给 worker。

OpenCode 独立模式不得使用上述顺序；OpenCode 独立模式只读取 `OPENCODE_MODE.md`。

ExAgent 模式使用 `EXAGENT_MODE.md` 规定的顺序。

## 输出目录

Agent / ExAgent 的跨轮交接只允许通过文件路径完成：

```text
<PROJECT_PATH>/DeepCode-output/<BATCH_NAME>/
<PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/
```

每条 DeepCode prompt 必须指定唯一输出文件，例如：

```text
<PROJECT_PATH>/DeepCode-output/<BATCH_NAME>/round-001-implement.md
```

每条 QwenCode prompt 必须指定唯一输出文件，例如：

```text
<PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/round-001-reference-inspect.md
<PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/round-001-actual-compare.md
```

下一轮 DeepCode 若需根据 Qwen 视觉报告修正，Supervisor 必须写入：

```text
QWENCODE_REPORT_TO_READ=<absolute path to QwenCode-output/*.md>
```

DeepCode 不得自行调用 QwenCode，也不得凭空声称“已参考 Qwen”。它必须在 `DeepCode-output` 报告中写明实际读取的 QwenCode 报告路径。

## 最终报告要求

Supervisor 输出给用户的是主管摘要，不是 DeepCode / QwenCode 长报告。

最终报告至少包含：

```text
MODE
MODEL_CHECK_RESULT
PATH_CHECK_RESULT
SCOPE_CONFIRMATION
OUTPUT_FILES_WRITTEN
DEEPCODE_OUTPUTS
QWENCODE_OUTPUTS
VALIDATION_RESULT
UNCERTAINTIES
NEXT_RECOMMENDED_ACTION
```
