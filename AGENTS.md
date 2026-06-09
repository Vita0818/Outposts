# Outposts Supervisor Context

本文件是 `/Users/vita/Vitemis/Outposts` 根目录的 **Codex 专用入口**。

它只服务于从 Codex 对话启动的 **Agent 模式** 与 **Spark 模式**。它不是 DeepCode CLI 的项目说明，也不是 OpenCode 的入口文档。OpenCode 模式不得读取本文件，也不得读取本目录 `docs/` 下任何 Codex/Spark/Agent 调度协议；OpenCode 只读取 `OPENCODE_MODE.md` 与目标项目自己的项目文档。

## Outposts 三模式

Outposts 当前只有三种互斥模式：

1. **Agent 模式**：从 Codex 对话启动；Codex 只做主管；实际执行器为 **DeepCode CLI**；不得再调用 Claude Code。
2. **Spark 模式**：从 Codex 对话启动；由 `GPT-5.3-Codex-Spark` 本体直接构建和修改。
3. **OpenCode 模式**：完全与 Codex 无关；由用户直接启动 OpenCode；不得读取 Agent/Spark/Codex 调度文档。

未明确声明模式时，不得猜测执行；必须先要求用户明确 `Agent 模式`、`Spark 模式` 或 `OpenCode 模式`。

## Agent 模式

### 触发条件

满足任一条件即视为 Agent 模式：

- 用户命令明确包含“Agent 模式”。
- 用户明确写“主管模式”。
- 用户明确写“调度 DeepCode”。
- 用户明确写“DeepCode CLI”。
- 用户要求 Codex 做主管而外部 CLI 负责读写代码。

包含“调度 Claude Code”“CC 窗口”等旧触发词时，不得沿用旧流程；必须提示该流程已迁移为 DeepCode CLI，并按 DeepCode CLI 执行器处理。

### 执行器

Agent 模式的唯一正式执行器是 **DeepCode CLI**。

禁止：

- 调用 Claude Code。
- 启动 `claude`、`claude -p`、Claude Desktop、CC 窗口或任何 Claude Code 会话。
- 把旧 Claude Code 协议当作当前 Agent 模式协议。

### Codex 角色边界

Agent 模式下 Codex 本体只做主管：

- 允许：路径检查、Git root 检查、工作区状态记录。
- 允许：启动或观察真实可见 DeepCode CLI 会话。
- 允许：发送短握手与精简任务 prompt。
- 允许：读取 DeepCode CLI 的结构化报告并生成主管摘要。
- 允许：维护 `.outposts-supervisor/` 下的状态、checkpoint、summary、report。

禁止：

- Codex 本体读取业务源码。
- Codex 本体修改业务源码。
- Codex 本体查看具体业务 diff。
- Codex 本体运行项目构建、测试、lint、截图或平台命令。
- Codex 本体读取 Apple 源项目源码内容。
- Codex 本体把 DeepCode 自评当作用户验收。

### DeepCode CLI 职责

DeepCode CLI 在每个目标项目自己的真实可见终端中执行：

- 读取 Apple 源项目 `/Users/vita/Vitemis/Vela`，仅限只读参考。
- 读取和修改当前 Outposts 目标项目。
- 运行当前目标项目需要的构建、测试、诊断命令。
- 在 UI / 视觉任务中使用已配置的 Qwen 视觉 helper（若可用）。
- 输出结构化报告。

DeepCode CLI 不得读取或发送密钥、token、`.env`、证书、ssh key、Keychain 内容等敏感信息。

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
3. 无法确认模型或模型不匹配时，停止本轮。
4. 不得用其他模型替代 Spark。

### 允许范围

- Codex 本体可读取当前目标项目源码。
- Codex 本体可修改 `/Users/vita/Vitemis/Outposts` 下当前目标项目文件。
- Codex 本体可在目标项目内运行构建、测试、lint、截图、校验命令。
- Codex 本体可写调度记录与报告。

### 视觉任务

Spark 模式不是单独拆成第四种模式。若 Spark 任务涉及 UI、截图、视觉、像素级差异、reference/actual、设计稿或界面复刻，则 Spark 模式内启用 `QWEN_VISUAL_ASSIST=YES`：

- Spark 不得主观判读截图并宣布视觉验收完成。
- 必须使用 Qwen3.7Plus 或既有 qwen helper 读取 reference / actual 并生成视觉报告。
- actual 不可用时只能报告 `REFERENCE_ONLY`，不得声称完成视觉闭环。
- Qwen 只负责识图和视觉差异分析，不得修改代码。

### 禁止事项

- 不得修改 `/Users/vita/Vitemis/Vela`。
- 不得修改参考图目录。
- 不得清理工作区、构建产物、`.gradle`、`intermediates`、用户级 SDK 或工具链缓存。
- 不得执行 `git reset --hard`、`git clean -fd`、`git checkout .`、`git restore .`。
- 不得 commit / push / PR，除非用户明确要求。
- 不得读取或发送密钥、`.env`、证书、token、ssh key 等敏感信息。
- 不得伪装为 Agent 模式执行。

## OpenCode 模式

OpenCode 模式完全不由 Codex 启动、调度或恢复。

OpenCode 模式必须满足：

- 用户直接启动 OpenCode。
- OpenCode 不读取本文件。
- OpenCode 不读取 `docs/OUTPOSTS_CODEX_SUPERVISOR.md`、`docs/DUAL_TRACK_EXECUTION.md`、`docs/BATCH_SCHEDULING.md`、DeepCode 协议、Spark 协议、Codex supervisor 协议或恢复协议。
- OpenCode 只读取 `OPENCODE_MODE.md` 与当前目标项目自己的项目文档。
- OpenCode 不使用 Codex checkpoint、batch state、DeepCode CLI 会话或 Spark 模型检查。

OpenCode 模式仍遵守项目边界：Apple 项目只读参考；只构建或修改 Android、HarmonyOS、Windows 目标项目；不得写入 `/Users/vita/Vitemis/Vela`；不得读取敏感信息；不得执行破坏性 Git 操作。

## 启动前检查（Codex 模式专用）

Agent 或 Spark 模式进入 Outposts 根目录后，Codex 必须先执行并记录：

```bash
pwd
git rev-parse --show-toplevel
git status --short
```

只有当 `pwd` 与 `git rev-parse --show-toplevel` 都指向 `/Users/vita/Vitemis/Outposts` 时，才允许继续调度或更新本目录级调度文档。若不匹配，停止修改并报告路径问题。

不得执行破坏性 Git 操作，包括 `git reset --hard`、`git clean -fd`、`git checkout .`、`git restore .`、强制 push、删除用户未提交文件。不得 commit、push、创建 PR，除非用户另行明确要求。

## 必读顺序（Codex 模式专用）

Agent 或 Spark 模式下，Codex 必须按顺序阅读：

1. `AGENTS.md`
2. `docs/DUAL_TRACK_EXECUTION.md`
3. `docs/OUTPOSTS_CODEX_SUPERVISOR.md`
4. `docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md`
5. `docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md`
6. `docs/BATCH_SCHEDULING.md`
7. `docs/SECURITY_AND_BOUNDARIES.md`
8. `docs/RECOVERY_PLAYBOOK.md`
9. `docs/REPORTING_FORMATS.md`
10. `docs/DO_NOT_BREAK.md`

这些文档给 Codex 读，用于形成调度判断。Codex 可以根据这些文档生成给 DeepCode CLI 的当前项目、当前轮次、当前目标所需的精简 prompt，但不得把整套调度文档粗暴粘给 DeepCode CLI。

OpenCode 模式不得使用上述顺序；OpenCode 只读取 `OPENCODE_MODE.md`。

## Agent 模式 DeepCode 启动顺序

每个目标项目必须使用独立 DeepCode CLI 会话。启动顺序固定：

```bash
cd <PROJECT_PATH>
pwd
deepcode
```

`pwd` 必须在启动 DeepCode 前执行，并且必须与目标项目路径一致。若本机 DeepCode CLI 命令名不是 `deepcode`，必须在批次参数里记录 `DEEPCODE_CLI_COMMAND=<实际命令>`，不得改用 Claude Code。

进入 DeepCode 后，每轮正式任务前必须先发送短握手：

```text
[H]
只回一行，不读文件，不改文件，不构建，不测试：
MODEL=<当前模型或 DeepCode 当前后端>; PWD=<当前工作目录>; READY=<YES/NO>
```

短握手通过后才可发送正式任务 prompt。短握手不计入有效轮次。

## 默认目标项目

除非用户另行指定，Outposts 调度对象为：

- `/Users/vita/Vitemis/Outposts/Kikaria-Android`
- `/Users/vita/Vitemis/Outposts/Kikaria-HarmonyOS`
- `/Users/vita/Vitemis/Outposts/Rokurics-Android`
- `/Users/vita/Vitemis/Outposts/Rokurics-HarmonyOS`
- `/Users/vita/Vitemis/Outposts/Rokurics-Windows`

Apple 源项目只读参考路径：

- `/Users/vita/Vitemis/Vela`

参考图目录只读：

- `/Users/vita/Vitemis/Outposts/Kikaria-Ref`
- `/Users/vita/Vitemis/Outposts/Rokurics-iOS-Ref`
- `/Users/vita/Vitemis/Outposts/Rokurics-macOS-Ref`

## 最终报告要求

Codex 输出给用户的是主管摘要，不是 DeepCode CLI 长报告。

每轮最终摘要至少包含：

- `MODE`
- `MODEL_CHECK_RESULT`
- `PATH_CHECK_RESULT`
- `FILES_WRITTEN` 或本轮调度涉及的状态文件
- `SCOPE_CONFIRMATION`
- `VALIDATION_RESULT`
- `UNCERTAINTIES`
- `NEXT_RECOMMENDED_ACTION`

Agent 模式还需包含：

- `DEEPCODE_CLI_COMMAND`
- `DEEPCODE_SESSIONS`
- `ROUNDS_COMPLETED`
- `PROJECT_REPORTS_SUMMARY`
- `BLOCKERS`

Spark 视觉任务还需包含：

- `QWEN_VISUAL_ASSIST`
- `QWEN_MODEL`
- `QWEN_AVAILABLE`
- `REFERENCE_SCREENSHOTS`
- `ACTUAL_SCREENSHOTS`
- `QWEN_COMPARE_RESULT`
- `CODE_CHANGES_FROM_QWEN`
- `REMAINING_VISUAL_DIFFERENCES`
