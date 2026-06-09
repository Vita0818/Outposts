# DeepCode CLI Terminal Protocol

本文定义 Agent / ExAgent 模式下 DeepCode CLI 的真实终端协议。

## 正式通道要求

DeepCode CLI 正式任务必须通过真实可见或可观察的交互式终端运行。可接受形式包括：

- 用户可见的 Terminal 窗口。
- 每项目独立 `screen` 会话。
- 每项目独立 `tmux` 会话。
- 能被用户观察、能被 supervisor 读取的 live log 会话。

不得使用隐藏 headless 通道作为正式任务主通道。不得使用不可观察的后台 stdout 作为唯一事实来源。

只读探测、环境排错或恢复分析如需其他方式，必须先确认不违反当前用户授权，并且不得替代正式任务终端协议。

## 每项目独立会话

每个目标项目必须有独立 DeepCode CLI 终端。会话命名建议包含项目名和批次名，例如：

```text
outposts-<BATCH_NAME>__Kikaria-Android
outposts-<BATCH_NAME>__Kikaria-HarmonyOS
outposts-<BATCH_NAME>__Rokurics-Android
outposts-<BATCH_NAME>__Rokurics-HarmonyOS
outposts-<BATCH_NAME>__Rokurics-Windows
```

一个项目的 prompt、输出、错误、报告不得混入另一个项目。

## 启动顺序

每个项目必须严格按以下顺序启动：

```bash
cd <PROJECT_PATH>
pwd
deepcode
```

`pwd` 必须在启动 DeepCode 前执行。`pwd` 输出必须与目标项目路径一致，否则不得进入正式任务。

若本机 DeepCode CLI 命令名不是 `deepcode`，必须在批次参数中显式记录：

```text
DEEPCODE_CLI_COMMAND=<实际命令>
```

不得在 Outposts 根目录直接启动用于子项目任务的 DeepCode。不得依赖 DeepCode 自己猜测工作目录。

## DeepCode 内短握手

每轮正式任务前，Supervisor 必须先在 DeepCode CLI 内发送短握手：

```text
[H]
只回一行，不读文件，不改文件，不构建，不测试：
MODEL=<当前模型或 DeepCode 当前后端>; PWD=<当前工作目录>; READY=<YES/NO>
```

短握手只允许 DeepCode 返回一行，不允许 DeepCode 读取文件、修改文件、运行构建或测试。

短握手不算正式任务轮次。短握手失败、模型不匹配、路径不匹配、READY=NO 都不计入已完成轮次，但必须记录状态和原因。

## 模型检查规则

Supervisor 必须记录 DeepCode 返回的 `MODEL` 字段。

默认预期模型为 DeepSeek V4 Pro 系列，除非用户或批次另行指定。可接受名称由批次参数 `EXPECTED_DEEPCODE_MODEL` 明确。

规则：

- 若用户或批次要求特定模型，而握手显示其他模型，暂停项目并进入 `MODEL_MISMATCH` 或 `MANUAL_DECISION_REQUIRED`。
- 若 DeepCode CLI 无法报告精确模型，但用户没有指定精确模型，记录 `MODEL=UNKNOWN_REPORTED_BY_DEEPCODE`，并在主管摘要中列为不确定项。
- 若任务成本、能力或合规边界依赖具体模型，而模型无法确认，进入 `MANUAL_DECISION_REQUIRED`。
- 不得把 DeepCode 的未知模型等同于 Spark 模型。
- 不得用 Spark 模型检查替代 DeepCode CLI 模型检查。

## 路径检查规则

Supervisor 必须记录 DeepCode 返回的 `PWD` 字段。

`PWD` 必须等于当前项目路径。若不一致：

1. 不发送正式任务 prompt。
2. 记录 `WORKDIR_MISMATCH`。
3. 关闭或搁置错误会话。
4. 重新按 `cd -> pwd -> deepcode` 建立正确会话，或等待用户确认。

DeepCode CLI 不得在 Apple 源项目目录中执行写操作。Apple 源项目路径只读。

## 30 秒窗口监测频率

正式任务运行中，Supervisor 应约每 30 秒读取每个运行中项目的可观察输出。

监测目标：

- 是否有新输出。
- 是否出现结构化报告。
- 是否出现权限、模型、路径、工具链、网络、计费错误。
- UI 视觉任务中是否成功生成截图、调用或读取 Qwen helper、得到视觉差异结论。
- 是否出现 DeepCode 等待用户输入。
- 是否长时间无输出且没有明确进展。

监测不应打断正常运行中的 DeepCode CLI。时间预算到达后，不强杀正在正常运行的 DeepCode CLI，只是不再启动新轮。

## 输出读取方式

优先读取用户可观察的终端缓冲区、`screen`/`tmux` capture、或对应 live log。读取方式必须能与用户人工观察对齐。

每次读取后，Supervisor 应更新项目状态：

```text
PROJECT_NAME
SESSION_NAME
LAST_OBSERVED_AT
CURRENT_ROUND_STATUS
LAST_VISIBLE_OUTPUT_SUMMARY
WAITING_FOR_INPUT
ERROR_DETECTED
REPORT_DETECTED
NEXT_ACTION
```

## 用户人工接管

用户可以随时接管某个 DeepCode CLI 终端。发生人工接管时，Supervisor 必须记录：

- 接管时间。
- 项目名。
- 接管前状态。
- Supervisor 是否继续只读观察。
- 用户是否要求暂停自动发送 prompt。

用户接管后，Supervisor 不得继续向该会话发送正式任务 prompt，除非用户明确恢复自动调度。

## Prompt 纪律

Supervisor 给 DeepCode CLI 的正式任务 prompt 必须是当前项目、当前轮次、当前目标所需的精简 prompt。

不得把整套调度文档粗暴粘给 DeepCode CLI。不得把 OpenCode 独立模式文档混入 DeepCode prompt。

每轮正式 prompt 首部建议包含：

```text
MODE=<AGENT|EXAGENT>
INITIATOR=<Codex|OpenCode_THREAD>
SUPERVISOR=<Codex|OpenCode_THREAD>
EXECUTOR=DeepCode CLI
PROJECT=<PROJECT_NAME>
TARGET_PATH=<PROJECT_PATH>
ROUND_INDEX=<N>
EXPECTED_DEEPCODE_MODEL=<model or default>
APPLE_SOURCE_READONLY=/Users/vita/Vitemis/Vela
OUTPOSTS_ROOT=/Users/vita/Vitemis/Outposts
DO_NOT_READ_SENSITIVE_FILES=YES
DO_NOT_RUN_DESTRUCTIVE_GIT=YES
```

## DeepCode 每轮报告字段

DeepCode 每轮报告必须至少包含：

```text
MODEL_CHECK_RESULT
PATH_CHECK_RESULT
SOURCE_READONLY_CHECK
PROJECT_NAME
ROUND_INDEX
TASK_RECEIVED
FILES_READ_SUMMARY
FILES_CHANGED
COMMANDS_RUN
BUILD_RESULT
TEST_RESULT
IMPLEMENTED_THIS_ROUND
REMAINING_UI_DIFFERENCES
REMAINING_FUNCTIONAL_GAPS
BLOCKERS
REGRESSION_RISKS
NEXT_RECOMMENDATION
READY_FOR_USER_REVIEW
```

不得省略未运行的构建或测试；必须写 `NOT_RUN`、`NOT_ATTEMPTED` 或具体阻塞原因。
