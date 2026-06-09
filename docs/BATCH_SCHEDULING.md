# Batch Scheduling

本文只适用于从 Codex 对话启动的 **Agent 模式** 与 **Spark 模式**。

OpenCode 模式完全不由 Codex 调度；OpenCode 不读取本文。OpenCode 的唯一入口是 `OPENCODE_MODE.md`。

## 必填批处理参数

每个 Codex 批次开始前必须确定：

```text
MODE: AGENT | SPARK
BATCH_NAME
CONCURRENCY
BATCH_TIME_BUDGET_MINUTES
MAX_REPORT_ROUNDS_PER_PROJECT
STOP_MODE
AUTO_CONTINUE_WITHIN_BUDGET
NO_NEW_ROUNDS_AFTER_TIME_BUDGET
WAIT_RUNNING_ROUNDS_TO_FINISH
VISION_VALIDATION_MAX_ROUNDS
QWEN_VISUAL_ASSIST: YES | NO
```

Agent 模式还必须记录：

```text
EXECUTOR: DeepCode CLI
DEEPCODE_CLI_COMMAND: deepcode | <实际命令>
DEEPCODE_MODEL_EXPECTED: <用户指定则填写，否则 UNKNOWN_ALLOWED>
DIRECT_CODE_MODIFICATION_ALLOWED_BY_CODEX: NO
```

Spark 模式还必须记录：

```text
EXECUTOR: GPT-5.3-Codex-Spark
SPARK_MODEL_EXPECTED: GPT-5.3-Codex-Spark
DIRECT_CODE_MODIFICATION_ALLOWED_BY_CODEX: YES
```

OpenCode 不进入上述批次状态，不写 Codex batch state，不消费 Codex 轮次预算。

## 轮次计数

Agent 模式：

- 一轮按 DeepCode CLI 的一次完整结构化报告计入 `ROUNDS_COMPLETED`。
- 仅启动终端、仅 `cd`/`pwd`、仅短握手、prompt 未送达、模型错误、路径错误、权限拦截、工具链缺失，不计有效轮次。

Spark 模式：

- Spark 由 Codex/Spark 本体直接执行。
- 一次完整修改 + 验证 + 结构化报告计为一轮 Spark 执行。
- 不得把 Spark 的直接修改记录为 DeepCode 报告轮次。

OpenCode 模式：

- 不由 Codex 记录轮次。
- 不读取 `.outposts-supervisor` batch state。
- 需要记录时，由 OpenCode 在项目内用 OpenCode 自己的报告方式处理。

## 视觉批次规则

若批次目标包含 UI、Apple UI parity、视觉验收、截图对比、界面布局、颜色、字体、间距、圆角、阴影、组件位置、设计稿、真机截图或模拟器截图，必须设置：

```text
QWEN_VISUAL_ASSIST=YES
VISION_VALIDATION_MAX_ROUNDS=2
```

视觉证据固定目录：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT_NAME>/
```

每个项目固定子目录：

```text
reference/
actual/
qwen/
```

规则：

- reference 图只读。
- actual 图必须来自有效 App、Preview、真机、模拟器或窗口截图。
- Qwen 输出写入 `qwen/`。
- 不得删除、覆盖旧 visual evidence。
- 重新截图必须创建新的 `RUN_ID` 或唯一文件名。
- 无 actual 时可走 `REFERENCE_ONLY`，不得声称 compare 完成。

## Android screenshot preflight

Android 项目需要 actual screenshot 时，DeepCode CLI 或 Spark 必须自动完成截图链，不得要求用户在 Android Studio 手动切项目、手动 Build/Run 或回复 `READY`。

固定流程：

1. 获取 `ANDROID_EMULATOR_LOCK`。
2. 确认 `/Users/vita/Library/Android/sdk/platform-tools/adb` 可用。
3. 执行 `adb devices -l`。
4. 从当前 Android 项目 Gradle 配置读取 `applicationId`，不得猜包名。
5. 检查目标 App 是否已安装。
6. 如未安装，只允许在当前项目内执行最小 `installDebug` 或等价安装命令。
7. adb 启动目标 App。
8. 校验前台包名等于目标 `applicationId`。
9. 前台正确后执行 `screencap`。
10. 截图保存到 visual-evidence 的 `actual/` 目录，文件名唯一。
11. 释放 `ANDROID_EMULATOR_LOCK`。

`installDebug` 在此处只属于 `SCREENSHOT_PREFLIGHT`，不得伴随 UI 修改。报告必须包含：

```text
INSTALL_NEEDED_FOR_SCREENSHOT
INSTALL_COMMAND
INSTALL_RESULT
```

若安装失败，项目进入 `INSTALL_FOR_SCREENSHOT_FAILED`，不得继续 UI 修改。若前台包名两次重试仍不匹配，进入 `ANDROID_FOREGROUND_PACKAGE_MISMATCH`。

## HarmonyOS 工具链边界

HarmonyOS 项目必须区分源码修复与用户级工具链修复。

禁止：

- 删除、清理或修改 `~/.hvigor`。
- 删除、清理或修改用户级 DevEco、HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 全局安装 `pnpm`、npm 包、ohpm 包或任何全局工具链依赖。
- 执行全局工具链修复、SDK 修复、用户目录缓存重建。

如果失败原因指向用户级工具链、SDK 缓存、网络代理、全局包管理器或 DevEco 安装状态，项目应进入：

```text
HOST_ENV_BLOCKED
TOOLCHAIN_REPAIR_NEEDS_USER
BLOCKED_NEEDS_USER
```

## 时间预算行为

时间预算是软限制。

达到 `BATCH_TIME_BUDGET_MINUTES` 后：

1. 设置 `TIME_BUDGET_REACHED=YES`。
2. 设置 `NO_NEW_ROUNDS=YES`。
3. 不再启动任何新正式轮。
4. 若有运行中的正式任务，允许自然结束。
5. 不得强杀正在正常运行的 DeepCode CLI 或 Spark 验证命令。

`AUTO_CONTINUE_WITHIN_BUDGET=YES` 时，只要当前时间未达到预算、项目轮次未达上限、无硬阻塞且仍有可执行下一步，就应继续下一轮。

不得因为以下软状态提前收束：

- `READY_FOR_USER_REVIEW` 但仍有 remaining gaps 或 next recommendation。
- `REFERENCE_ONLY`。
- actual screenshot 暂时不可用但 reference screenshot 可用。
- `QWEN_COMPARE_SCREENSHOTS_COMPLETED=NO`。
- `WINDOWS_HOST_VALIDATION_PENDING` 但仍可做静态 WinUI/XAML 修复。
- UI 仍有剩余差异。

## 轮次预算行为

达到 `MAX_REPORT_ROUNDS_PER_PROJECT` 后：

1. 设置 `NO_NEW_ROUNDS=YES`。
2. 若没有运行中的正式任务，项目进入 `STOPPED_BY_ROUND_BUDGET`。
3. 若有运行中的正式任务，允许自然结束。

轮次预算按项目分别计算。一个项目达到轮次上限，不影响其他项目在预算内继续。

## 终止状态枚举

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
QWEN_PERMISSION_GATED
QWEN_UNAVAILABLE_IN_SESSION
QWEN_HELPER_NOT_CONFIGURED
QWEN_INVALID_VISUAL_EVIDENCE
INSTALL_FOR_SCREENSHOT_FAILED
ANDROID_FOREGROUND_PACKAGE_MISMATCH
USER_DEVICE_INTERVENTION_REQUIRED
WINDOWS_HOST_VALIDATION_PENDING
MANUAL_DECISION_REQUIRED
```

## 硬阻塞

允许立即暂停或终止项目的硬阻塞：

- `MODEL_MISMATCH`
- `WORKDIR_MISMATCH`
- `SOURCE_READONLY_FAILED`
- `API_402_INSUFFICIENT_BALANCE`
- `BILLING_MODEL_MISMATCH`
- `LOCAL_EXECUTION_POLICY_BLOCKED`
- `INSTALL_FOR_SCREENSHOT_FAILED`
- `USER_DEVICE_INTERVENTION_REQUIRED`
- 破坏性 Git 操作。
- 清理用户级工具链或全局安装依赖。
- 删除 visual evidence、checkpoint、state、report。
- 真实需要用户手动决策且无法安全继续。

## 五项目并行调度循环（Agent 模式）

1. 初始化批次参数和项目状态。
2. 为每个项目建立独立真实可见 DeepCode CLI 终端。
3. 执行 `cd -> pwd -> deepcode`。
4. 发送短握手。
5. 对 READY 项目发送正式任务。
6. 约每 30 秒读取所有运行中项目输出。
7. 对最先返回报告的项目立即处理。
8. 更新 `ROUNDS_COMPLETED`、预算、状态和 checkpoint。
9. 若允许继续，启动该项目下一轮。
10. 所有项目终止后输出最终主管摘要。

## 项目批次偏好

- Kikaria-Android：首页和背诵页优先；可整体重构 UI shell；保持 Android build/test 绿色。
- Kikaria-HarmonyOS：先编译；构建未恢复前不堆功能；用户级工具链问题进入 `TOOLCHAIN_REPAIR_NEEDS_USER`。
- Rokurics-Android：必须 reference-first；dark mode/theme support 是明确任务；actual screenshot 可通过 adb 闭环。
- Rokurics-HarmonyOS：有效 Preview/设备截图优先；禁止用户级工具链清理或全局包安装。
- Rokurics-Windows：WinUI 3/XAML 静态修复可在 Windows 主机验证前继续；真实 build/launch 只能在 Win11 ARM + VS2022 验证。
