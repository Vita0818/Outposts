# Worker One-shot Invocation Protocol

本文定义 Agent / ExAgent 模式下 DeepCode 与 QwenCode 的一次性窗口调用协议。

## 核心规则

DeepCode 与 QwenCode 不作为可持续交互会话使用。每次调用都必须在窗口启动时一次性提交完整 prompt。窗口启动后不得再追加业务指令。

```text
VALID:
Supervisor -> launch worker window with complete prompt -> worker writes output file -> supervisor reads file -> window discarded

INVALID:
Supervisor -> enter worker -> handshake -> send prompt -> send follow-up -> reuse session memory
```

## Worker 分工

DeepCode：DeepSeek；负责规划、读源码、写代码、构建、测试、截图生成、实现报告。

QwenCode：Qwen3.7-Plus；负责看图、reference/actual 识别、截图对比、视觉报告。

Supervisor：Codex 或 OpenCode 线程；负责发起所有请求、管理输入输出路径、串联轮次、控制预算和生成主管摘要。

## Worker 间禁止通信

```text
DeepCode -> QwenCode: FORBIDDEN
QwenCode -> DeepCode: FORBIDDEN
DeepCode -> Qwen helper: FORBIDDEN
QwenCode -> source code: FORBIDDEN
QwenCode -> DeepCode-output: FORBIDDEN
```

DeepCode 只能读取 supervisor 在 prompt 中显式给出的 `QwenCode-output/*.md` 文件路径。QwenCode 只能读取 supervisor 在 prompt 中显式给出的截图路径。

## 命令记录

由于本机 DeepCode / QwenCode 的实际命令名和 one-shot 参数可能随配置不同，每个批次必须记录：

```text
DEEPCODE_CLI_COMMAND=<actual command>
DEEPCODE_ONE_SHOT_INVOCATION=<actual invocation template>
QWENCODE_CLI_COMMAND=<actual command>
QWENCODE_ONE_SHOT_INVOCATION=<actual invocation template>
```

如果命令不确定，不得猜测；进入 `MANUAL_DECISION_REQUIRED`。

## 启动顺序

DeepCode：

```bash
cd <PROJECT_PATH>
pwd
<DEEPCODE_ONE_SHOT_INVOCATION with complete prompt>
```

QwenCode：

```bash
cd <PROJECT_PATH>
pwd
<QWENCODE_ONE_SHOT_INVOCATION with complete prompt>
```

`pwd` 必须等于目标项目路径。不得在 Outposts 根目录直接启动子项目 one-shot 任务。

## 模型与路径检查

因为 DeepCode / QwenCode 不再多次交互，模型与路径检查必须写入同一条 one-shot prompt 的最前部。

DeepCode prompt 开头必须要求：

```text
MODEL_CHECK_REQUIRED=YES
EXPECTED_MODEL=<DeepSeek V4 Pro 系列或用户指定>
PWD_MUST_EQUAL=<PROJECT_PATH>
IF_MODEL_OR_PWD_MISMATCH=write mismatch report to OUTPUT_FILE and stop
```

QwenCode prompt 开头必须要求：

```text
MODEL_CHECK_REQUIRED=YES
EXPECTED_MODEL=Qwen3.7-Plus
PWD_MUST_EQUAL=<PROJECT_PATH>
IF_MODEL_OR_PWD_MISMATCH=write mismatch report to OUTPUT_FILE and stop
```

模型或路径不匹配时，worker 必须只写 mismatch report，不得继续业务任务。

## 输出路径强制

DeepCode 必须写入：

```text
<PROJECT_PATH>/DeepCode-output/<BATCH_NAME>/<unique-file>.md
```

QwenCode 必须写入：

```text
<PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/<unique-file>.md
```

输出文件名由 supervisor 指定。worker 不得自行选择输出目录，不得覆盖旧文件。

## DeepCode prompt 必含块

```text
WORKER=DeepCode_ONE_SHOT
MODEL_EXPECTED=<DeepSeek V4 Pro 系列或用户指定>
PROJECT_NAME=<name>
PROJECT_PATH=<absolute path>
PWD_MUST_EQUAL=<absolute path>
BATCH_NAME=<batch>
ROUND_ID=<round>
TASK_OBJECTIVE=<objective>
OUTPUT_FILE=<absolute DeepCode-output path>
PREVIOUS_DEEPCODE_REPORTS=<absolute paths or NONE>
SUPERVISOR_PROVIDED_QWENCODE_REPORTS=<absolute paths or NONE>
USER_FEEDBACK=<summary or NONE>
REFERENCE_PATHS=<absolute paths or NONE>
DO_NOT_CALL_QWENCODE=YES
DO_NOT_USE_VISION_HELPER=YES
DO_NOT_READ_SENSITIVE_FILES=YES
```

DeepCode 最终报告必须写明：

```text
MODEL_CHECK_RESULT:
PATH_CHECK_RESULT:
OUTPUT_FILE_WRITTEN:
PREVIOUS_DEEPCODE_REPORTS_READ:
QWENCODE_REPORTS_REQUESTED_BY_SUPERVISOR:
QWENCODE_REPORTS_READ:
QWENCODE_FINDINGS_USED:
QWENCODE_REPORT_READ_FAILURES:
FILES_CHANGED:
COMMANDS_RUN:
BUILD_RESULT:
TEST_RESULT:
ACTUAL_SCREENSHOTS_GENERATED:
BLOCKERS:
NEXT_RECOMMENDED_ACTION:
REPORT_COMPLETE:
```

## QwenCode prompt 必含块

```text
WORKER=QwenCode_ONE_SHOT
MODEL_EXPECTED=Qwen3.7-Plus
PROJECT_NAME=<name>
PROJECT_PATH=<absolute path>
PWD_MUST_EQUAL=<absolute path>
BATCH_NAME=<batch>
ROUND_ID=<round>
VISUAL_TASK_TYPE=REFERENCE_INSPECT|ACTUAL_INSPECT|COMPARE
REFERENCE_SCREENSHOT=<absolute path or NONE>
ACTUAL_SCREENSHOT=<absolute path or NONE>
OUTPUT_FILE=<absolute QwenCode-output path>
DO_NOT_READ_SOURCE=YES
DO_NOT_READ_DEEPCODE_OUTPUT=YES
DO_NOT_MODIFY_FILES=YES
DO_NOT_READ_SENSITIVE_FILES=YES
```

QwenCode 最终报告必须写明：

```text
MODEL_CHECK_RESULT:
PATH_CHECK_RESULT:
OUTPUT_FILE_WRITTEN:
VISUAL_TASK_TYPE:
REFERENCE_SCREENSHOT_READ:
ACTUAL_SCREENSHOT_READ:
QWENCODE_VALID_VISUAL_EVIDENCE:
QWENCODE_COMPARE_COMPLETED:
VISION_RESULT_SUMMARY:
MAJOR_VISUAL_DIFFERENCES:
REMAINING_VISUAL_BLOCKERS:
SCREENSHOT_VALIDITY_NOTES:
REPORT_COMPLETE:
```

## Supervisor 读取规则

Supervisor 只能根据输出文件判断 worker 是否完成。终端里出现“done”但输出文件不存在或 `REPORT_COMPLETE` 不为 `YES`，不得计为有效完成轮。

完成判定：

1. 输出文件存在。
2. 文件路径等于 prompt 中指定的 `OUTPUT_FILE`。
3. `MODEL_CHECK_RESULT` 和 `PATH_CHECK_RESULT` 可确认。
4. `REPORT_COMPLETE=YES`。
5. 若是视觉任务，QwenCode 输出文件包含有效截图判定字段。

## 禁止事项

- 不得进入 DeepCode / QwenCode 后继续发送第二条业务 prompt。
- 不得复用上一轮窗口上下文。
- 不得把 worker 终端记忆当作状态。
- 不得省略 `OUTPUT_FILE`。
- 不得让 DeepCode 自行调用 QwenCode 或任何视觉 helper。
- 不得让 QwenCode 读取源码、DeepCode-output、密钥或私密配置。
- 不得让 DeepCode 在没有 supervisor 提供的 QwenCode 报告路径时宣称像素级视觉验收完成。
