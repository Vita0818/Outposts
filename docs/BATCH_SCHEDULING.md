# Batch Scheduling

本文适用于 Agent、ExAgent 与 Spark。OpenCode 独立模式不进入 supervisor batch state。

## 必填批处理参数

每个批次开始前必须确定：

```text
MODE: AGENT / EXAGENT / SPARK
INITIATOR: Codex / OpenCode_THREAD
SUPERVISOR: Codex / OpenCode_THREAD / NONE
IMPLEMENTATION_WORKER: DeepCode_ONE_SHOT / GPT-5.3-Codex-Spark
VISION_WORKER: QwenCode_ONE_SHOT / QWENCODE_VISUAL_ASSIST / NONE
WORKER_INTERACTION: FORBIDDEN
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

Agent / ExAgent 还必须记录：

```text
DEEPCODE_CLI_COMMAND
DEEPCODE_ONE_SHOT_INVOCATION
EXPECTED_DEEPCODE_MODEL
DEEPCODE_MODEL_CHECK_REQUIRED=YES
QWENCODE_CLI_COMMAND
QWENCODE_ONE_SHOT_INVOCATION
EXPECTED_QWENCODE_MODEL=Qwen3.7-Plus
QWENCODE_MODEL_CHECK_REQUIRED=YES
DIRECT_CODE_MODIFICATION_ALLOWED=NO_FOR_SUPERVISOR
DEEPCODE_OUTPUT_ROOT=<PROJECT_PATH>/DeepCode-output/<BATCH_NAME>/
QWENCODE_OUTPUT_ROOT=<PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/
```

Spark 还必须记录：

```text
SPARK_MODEL_EXPECTED=GPT-5.3-Codex-Spark
SPARK_MODEL_CHECK_REQUIRED=YES
DIRECT_CODE_MODIFICATION_ALLOWED=YES_WITHIN_TARGET_PROJECT
```

OpenCode 独立模式不进入上述批次状态，不写 supervisor batch state，不消费 supervisor 轮次预算。

## Agent / ExAgent 一轮如何计数

一轮 DeepCode 实现任务只有在以下步骤全部完成后，才计入 `ROUNDS_COMPLETED`：

1. Supervisor 生成完整 DeepCode one-shot prompt。
2. Prompt 中指定模型检查、路径检查、输入文件、输出文件和业务目标。
3. DeepCode 执行任务。
4. DeepCode 将结构化报告写入指定 `DeepCode-output/*.md`。
5. Supervisor 读取报告并形成主管判断。
6. 报告中 `REPORT_COMPLETE=YES`。

QwenCode 视觉调用不计入 DeepCode 业务轮次，但计入视觉尝试次数，受 `VISION_VALIDATION_MAX_ROUNDS` 控制。

## Spark 一轮如何计数

一轮 Spark 执行只有在以下步骤全部完成后，才计入 Spark 轮次：

1. `GPT-5.3-Codex-Spark` 模型确认通过。
2. Spark 读取任务并在授权范围内执行。
3. Spark 修改、验证或明确说明未修改。
4. Spark 输出结构化报告。

## 什么不算一轮

以下情况不计入有效完成轮：

- 仅启动终端。
- 仅执行 `cd` 或 `pwd`。
- 仅完成模型/路径校验且不含业务任务。
- one-shot prompt 没送达。
- DeepCode 未写入指定输出文件。
- QwenCode 未写入指定输出文件。
- 输出文件缺少 `REPORT_COMPLETE=YES`。
- 模型错误。
- 路径错误。
- 只读边界失败。
- 本地执行策略拦截。
- API 402 或计费异常。
- 工具链缺失导致正式任务未开始。
- 边界违规 incident report。

这些情况必须记录状态，但不得消耗有效轮次预算。

## 视觉任务批次

如果批次目标包含 UI 复刻、Apple UI parity、视觉验收、截图对比、界面布局、颜色、字体、间距、圆角、阴影、组件位置、设计稿、真机截图或模拟器截图：

```text
VISION_WORKER=QwenCode_ONE_SHOT
VISION_VALIDATION_MAX_ROUNDS=2
```

固定证据目录：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT_NAME>/
```

每项目创建：

```text
reference/
actual/
qwen/
```

QwenCode 结构化视觉报告写入：

```text
<PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/
```

视觉证据和 QwenCode 报告是批次审计材料，不是临时垃圾文件。不得删除 `.outposts-supervisor/visual-evidence`、当前批次截图、QwenCode 输出、DeepCode 输出、state、checkpoint、report 或 batch state。需要重新截图时必须创建新的 `RUN_ID` 目录。

## 视觉任务推荐轮次顺序

```text
1. QwenCode reference inspect。
2. DeepCode implementation，输入 reference inspect 报告路径。
3. DeepCode 生成或报告 actual screenshot 路径。
4. QwenCode actual inspect / compare，输入 reference 与 actual screenshot 路径。
5. DeepCode fix-from-qwen，输入 compare 报告路径。
6. 如预算允许，继续 actual screenshot -> QwenCode compare -> DeepCode fix。
```

其中第 2、5 步计入 DeepCode 业务轮次；第 1、4、6 中的 QwenCode 调用计入视觉尝试次数。

## Android screenshot preflight

Android 项目需要 actual screenshot 时，DeepCode 或 Spark 必须自动完成截图链，不得要求用户在 Android Studio 手动切项目、手动 Build/Run 或回复 `READY`。

顺序固定：

1. 获取 `ANDROID_EMULATOR_LOCK`。
2. 确认 `/Users/vita/Library/Android/sdk/platform-tools/adb` 可用。
3. 执行 `adb devices -l`。
4. 从当前 Android 项目 Gradle 配置读取 `applicationId`。
5. 检查目标 App 是否已安装。
6. 如未安装，在当前目标项目内执行最小 `installDebug` 或等价安装命令。
7. adb 启动目标 App。
8. 校验前台包名等于目标 `applicationId`。
9. 截图到 visual-evidence 的 `actual/` 目录，文件名必须唯一。
10. 在 DeepCode-output 报告中写明 screenshot path。
11. 释放 `ANDROID_EMULATOR_LOCK`。

`installDebug` 在此处只属于 `SCREENSHOT_PREFLIGHT`，不得伴随 UI 修改。报告必须包含 `INSTALL_NEEDED_FOR_SCREENSHOT`、`INSTALL_COMMAND`、`INSTALL_RESULT`。

如果安装失败，项目进入 `INSTALL_FOR_SCREENSHOT_FAILED`，不得继续 UI 修改。

如果前台包名不等于目标 `applicationId`，必须自动重新启动目标 App 并重试校验，最多 2 次；仍失败才报告 `ANDROID_FOREGROUND_PACKAGE_MISMATCH`。不得把另一个项目的 App 截图当作当前项目截图。

## HarmonyOS 工具链边界

HarmonyOS 项目调度必须把源码修复和用户级工具链修复分开。

不得执行：

- 删除、清理或修改 `~/.hvigor`。
- 删除、清理或修改用户级 DevEco、HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 全局安装 `pnpm`、npm 包、ohpm 包或任何全局工具链依赖。
- 全局工具链修复、SDK 修复、用户目录缓存重建。

如果 HarmonyOS 构建失败原因指向用户级工具链、SDK 缓存、网络代理、全局包管理器或 DevEco 安装状态，项目应进入：

```text
HOST_ENV_BLOCKED
TOOLCHAIN_REPAIR_NEEDS_USER
BLOCKED_NEEDS_USER
```

除非用户另行明确授权，只允许在对应 Outposts 目标项目目录内修改源码和项目配置。

## 时间预算行为

时间预算是软限制。

`BATCH_TIME_BUDGET_MINUTES` 不是必须跑满的时长，但在 `AUTO_CONTINUE_WITHIN_BUDGET=YES` 时，只要当前时间未达到预算、项目轮次未达到 `MAX_REPORT_ROUNDS_PER_PROJECT`、项目没有硬阻塞、并且报告中仍有可执行下一步，就必须继续该项目下一轮。

不得仅因为以下软状态停止项目或收束整批：

- `READY_FOR_USER_REVIEW`，但仍有 remaining gaps 或 next recommendation。
- `REFERENCE_ONLY`。
- 缺少 actual screenshot，但 reference screenshot 可用。
- `QWENCODE_COMPARE_COMPLETED=NO`，但 QwenCode reference inspect 已完成。
- `WINDOWS_HOST_VALIDATION_PENDING`，但仍有 WinUI/XAML 静态修复可做。
- UI 仍有剩余差异或执行器明确建议下一轮继续。

这些情况应标记为 `ROUND_COMPLETE_CONTINUE_ELIGIBLE`。若时间和轮次预算允许，应继续下一轮。

达到 `BATCH_TIME_BUDGET_MINUTES` 后：

1. 设置 `TIME_BUDGET_REACHED=YES`。
2. 设置 `NO_NEW_ROUNDS=YES`。
3. 不再启动任何新正式轮。
4. 若没有运行中的正式任务，项目进入 `STOPPED_BY_TIME_BUDGET`。
5. 若有运行中的正式任务，允许其自然结束；结束后进入 `STOPPED_BY_TIME_BUDGET`。

不得因为时间预算到达而强杀正在正常运行的任务。

## 轮次预算行为

达到 `MAX_REPORT_ROUNDS_PER_PROJECT` 后：

1. 设置 `NO_NEW_ROUNDS=YES`。
2. 若没有运行中的正式任务，项目进入 `STOPPED_BY_ROUND_BUDGET`。
3. 若有运行中的正式任务，允许其自然结束；结束后进入 `STOPPED_BY_ROUND_BUDGET`。

轮次预算按项目分别计算。一个项目达到轮次上限，不影响其他项目在预算内继续。

## 终止状态枚举

批次结束时，每个项目必须进入以下任一终止状态：

```text
STOPPED_BY_ROUND_BUDGET
STOPPED_BY_TIME_BUDGET
READY_FOR_USER_REVIEW
BLOCKED_NEEDS_USER
FAILED_PREFLIGHT
MODEL_MISMATCH
WORKDIR_MISMATCH
SOURCE_READONLY_FAILED
TOOLCHAIN_MISSING
HOST_ENV_BLOCKED
TOOLCHAIN_REPAIR_NEEDS_USER
LOCAL_EXECUTION_POLICY_BLOCKED
API_402_INSUFFICIENT_BALANCE
BILLING_MODEL_MISMATCH
DEEPCODE_OUTPUT_MISSING
QWENCODE_OUTPUT_MISSING
QWENCODE_MODEL_MISMATCH
QWENCODE_UNAVAILABLE
QWENCODE_INVALID_VISUAL_EVIDENCE
INSTALL_FOR_SCREENSHOT_FAILED
ANDROID_FOREGROUND_PACKAGE_MISMATCH
USER_DEVICE_INTERVENTION_REQUIRED
WINDOWS_HOST_VALIDATION_PENDING
MANUAL_DECISION_REQUIRED
```

只有所有项目均进入终止状态，才能输出批次最终主管摘要。

## 硬阻塞

允许立即暂停或终止项目的硬阻塞：

- `MODEL_MISMATCH`
- `WORKDIR_MISMATCH`
- `SOURCE_READONLY_FAILED`
- `API_402_INSUFFICIENT_BALANCE`
- `BILLING_MODEL_MISMATCH`
- `LOCAL_EXECUTION_POLICY_BLOCKED`
- `DEEPCODE_OUTPUT_MISSING`
- `QWENCODE_OUTPUT_MISSING` 且视觉任务必须继续。
- `QWENCODE_MODEL_MISMATCH`
- `INSTALL_FOR_SCREENSHOT_FAILED`
- `USER_DEVICE_INTERVENTION_REQUIRED`
- 明确边界违规，例如破坏性 Git 操作、清理用户级工具链、删除 visual evidence、checkpoint 或 report。
- 真实需要用户手动决策且无法安全继续。

软状态不得混入硬阻塞字段。

## 项目异步处理规则

五项目并行时采用异步处理：

- 哪个 worker 先完成报告，就先读取和处理哪个项目。
- 不等所有项目统一完成。
- 已完成项目若预算允许且无阻塞，可以先进入下一轮。
- 阻塞项目记录原因并暂停，不阻塞其他项目。
- 每个项目的下一轮 prompt 必须基于该项目自己的最新报告、checkpoint、用户反馈和 QwenCode 报告路径。

## 五项目并行调度循环

1. 初始化批次参数和项目状态。
2. 为每个项目创建输出目录。
3. 需要视觉时，先启动 QwenCode reference inspect one-shot。
4. 启动 DeepCode implementation one-shot，并传入可用的 QwenCode reference report 路径。
5. 对 DeepCode 产出的 actual screenshot 路径启动 QwenCode compare one-shot。
6. 对需要继续的项目启动下一轮 DeepCode fix-from-qwen one-shot，并传入 QwenCode compare report 路径。
7. 约每 30 秒读取所有运行中项目输出。
8. 对最先返回报告的项目立即处理。
9. 更新 `ROUNDS_COMPLETED`、视觉轮次、预算、状态和 checkpoint。
10. 若允许继续，启动该项目下一轮；否则进入终止状态。
11. 所有项目终止后输出最终主管摘要。

## 标准批次模板

```text
MODE:
INITIATOR:
SUPERVISOR:
IMPLEMENTATION_WORKER:
VISION_WORKER:
WORKER_INTERACTION: FORBIDDEN
BATCH_NAME:
CONCURRENCY:
BATCH_TIME_BUDGET_MINUTES:
MAX_REPORT_ROUNDS_PER_PROJECT:
STOP_MODE: SOFT_TIME_BUDGET
AUTO_CONTINUE_WITHIN_BUDGET: YES/NO
NO_NEW_ROUNDS_AFTER_TIME_BUDGET: YES
WAIT_RUNNING_ROUNDS_TO_FINISH: YES
VISION_VALIDATION_MAX_ROUNDS: 2
DEEPCODE_CLI_COMMAND:
DEEPCODE_ONE_SHOT_INVOCATION:
QWENCODE_CLI_COMMAND:
QWENCODE_ONE_SHOT_INVOCATION:
```
