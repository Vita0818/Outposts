# Batch Scheduling

## 必填批处理参数

每个批次开始前必须确定：

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

（必要时）
MODE: `SPARK` / `SPARK_QWEN` / `AGENT`
```

## 双轨批次必填字段

在每个批次状态里需额外记录：

- `MODE: SPARK|SPARK_QWEN|AGENT`
- `MODEL_CHECK_RESULT: <模型校验结果>`
- `EXECUTOR: <Codex | Claude Code>`
- `DIRECT_CODE_MODIFICATION_ALLOWED: YES|NO`

规则：

- Spark 执行轮次由 Codex 本体直接完成，不应记入“正式 Claude Code 轮次”。
- Spark_QWEN 执行轮次同 Spark，由 Codex 本体直接完成，不计入“正式 Claude Code 轮次”。
- Agent 执行轮次按 Claude Code 的一次完整报告计入 `ROUNDS_COMPLETED`。
- 禁止把 Spark 的直接修改记录为 Claude 报告轮次，反之也不应把 Agent 运行直接记作 Codex 一人完成。

推荐默认语义：

- `BATCH_NAME`：短名，用于 session、checkpoint、summary 命名。
- `CONCURRENCY`：同时运行的项目数；五项目并行时通常为 `5`。
- `BATCH_TIME_BUDGET_MINUTES`：本批次软时间预算。
- `MAX_REPORT_ROUNDS_PER_PROJECT`：每个项目最多完成多少个有效报告轮。
- `STOP_MODE`：到达预算后的停止策略，通常为 `SOFT_STOP`.
- `AUTO_CONTINUE_WITHIN_BUDGET`：预算内是否允许自动进入下一轮。
- `NO_NEW_ROUNDS_AFTER_TIME_BUDGET`：时间预算到达后不得启动新轮，通常为 `YES`。
- `WAIT_RUNNING_ROUNDS_TO_FINISH`：时间到达后是否等待运行中轮次自然结束，通常为 `YES`。
- `VISION_VALIDATION_MAX_ROUNDS`：涉及 UI 复刻或视觉验收时，每项目默认最多进行 `2` 轮 qwen-vision 视觉对比微调。

如果批次目标包含 UI 复刻、Apple UI parity、视觉验收、截图对比、界面布局、颜色、字体、间距、圆角、阴影、组件位置、设计稿、真机截图或模拟器截图：
- 当任务由 Spark 执行且 Spark 能获取 visual evidence 与 qwen 报告时，设定 `MODE=SPARK_QWEN`，并把 qwen 视觉提醒写入该轮 Spark 执行计划。
- 否则以 `AGENT` 执行，并在 prompt 中加入 `CLAUDE_CODE_VISUAL_MCP_PROTOCOL.md` 规定的 qwen-vision 使用提醒。

视觉验收批次还必须设置固定证据目录：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT_NAME>/
```

目录下必须按项目创建 `reference/`、`actual/`、`qwen/`。Android 项目优先用已启动的 Emulator 和 `adb -s <DEVICE_SERIAL> exec-out screencap -p` 生成纯设备截图。HarmonyOS 项目优先用 DevEco Preview、Emulator 或真机截图；必要时可用 `screencapture` 保存 DevEco 可见窗口截图，并明确标注是否为完整 IDE 截图。Windows 项目必须有 Windows/.NET UI 环境，否则报告 `HOST_ENV_BLOCKED`。

共享 Android Emulator 的调度含义是“截图阶段串行加锁”，不是“用户手动切换项目”。Codex Agent 不得要求用户在 Android Studio 中手动选择 Kikaria-Android 或 Rokurics-Android，不得要求用户点击 Build/Run 后回复 `READY`。Android 项目需要截图时，Claude Code 必须自动获取 `ANDROID_EMULATOR_LOCK`，通过固定 adb 路径、Gradle 配置读取 `applicationId`、必要的 `installDebug`、adb 启动、前台包名校验和 `screencap` 完成截图链。

Android screenshot preflight 的顺序固定为：

1. 获取 `ANDROID_EMULATOR_LOCK`。
2. 确认 `/Users/vita/Library/Android/sdk/platform-tools/adb` 可用。
3. 执行 `adb devices -l`。
4. 从当前 Android 项目 Gradle 配置读取 `applicationId`。
5. 检查目标 App 是否已安装。
6. 如未安装，在当前目标项目内执行最小 `installDebug` 或等价安装命令。
7. adb 启动目标 App。
8. 校验前台包名等于目标 `applicationId`。
9. 截图到 visual-evidence 的 `actual/` 目录，文件名必须唯一。
10. 释放 `ANDROID_EMULATOR_LOCK`。

`installDebug` 在此处只属于 `SCREENSHOT_PREFLIGHT`，用于让目标 App 出现在 Emulator 中；不得伴随 UI 修改。报告必须包含 `INSTALL_NEEDED_FOR_SCREENSHOT`、`INSTALL_COMMAND`、`INSTALL_RESULT`。如果安装失败，项目进入 `INSTALL_FOR_SCREENSHOT_FAILED`，不得继续 UI 修改。

如果前台包名不等于目标 `applicationId`，Claude Code 必须自动重新启动目标 App 并重试校验，最多 2 次；仍失败才报告 `ANDROID_FOREGROUND_PACKAGE_MISMATCH`。不得把另一个项目的 App 截图当作当前项目截图。

视觉证据是批次审计材料，不是临时垃圾文件。Claude Code 不得删除 `.outposts-supervisor/visual-evidence`、当前批次截图、qwen 输出、state、checkpoint、report 或 batch state。需要重新截图时必须创建新的 `RUN_ID` 目录，不得通过删除旧证据来“收尾”。

视觉报告必须区分 `QWEN_CALLED`、`QWEN_VALID_VISUAL_EVIDENCE`、`QWEN_COMPARE_SCREENSHOTS_COMPLETED`。`qwen-vision` 调用过无效桌面截图时，只能计为 `QWEN_CALLED=YES`，不得计为有效视觉验收。

当用户确认 Android Studio、Android Emulator、DevEco 或 Preview 已打开并显示目标页面时，Claude Code 不得继续把对应项目标记为“无模拟器”或“无 Preview”，除非实际截图命令失败并给出具体错误。

如果批次目标只是修编译、修单元测试、改数据模型、写文档、调整构建脚本、修后端逻辑或修算法，不要求调用 `qwen-vision`。

## 什么算一轮

一轮 Claude Code 正式任务只有在以下步骤全部完成后，才计入 `ROUNDS_COMPLETED`：

1. 短握手通过。
2. Codex Agent 发送正式任务 prompt。
3. Claude Code 执行任务。
4. Claude Code 返回一次结构化报告。
5. Codex Agent 读取报告并形成主管判断。

## 什么不算一轮

以下情况不计入有效完成轮：

- 仅启动终端。
- 仅执行 `cd` 或 `pwd`。
- 仅完成短握手。
- prompt 没送达。
- Claude Code 未返回报告。
- 模型错误。
- 路径错误。
- 只读边界失败。
- 本地执行策略拦截。
- API 402 或计费异常。
- 工具链缺失导致正式任务未开始。
- Claude Code 因边界违规被 Codex 停止并只输出 incident report。

这些情况必须记录状态，但不得消耗有效轮次预算。

## HarmonyOS 工具链边界

HarmonyOS 项目调度必须把源码修复和用户级工具链修复分开。

Claude Code 不得执行：

- 删除、清理或修改 `~/.hvigor`。
- 删除、清理或修改用户级 DevEco、HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 全局安装 `pnpm`、npm 包、ohpm 包或任何全局工具链依赖。
- 全局工具链修复、SDK 修复、用户目录缓存重建。

如果 HarmonyOS 构建失败原因指向用户级工具链、SDK 缓存、网络代理、全局包管理器或 DevEco 安装状态，项目应进入：

- `HOST_ENV_BLOCKED`
- `TOOLCHAIN_REPAIR_NEEDS_USER`
- `BLOCKED_NEEDS_USER`

除非用户另行明确授权，Claude Code 只允许在对应 Outposts 目标项目目录内修改源码和项目配置。

## 时间预算行为

时间预算是软限制。

`BATCH_TIME_BUDGET_MINUTES` 不是必须跑满的时长，但在
`AUTO_CONTINUE_WITHIN_BUDGET=YES` 时，只要当前时间未达到预算、项目轮次未达到
`MAX_REPORT_ROUNDS_PER_PROJECT`、项目没有硬阻塞、并且报告中仍有可执行下一步，就必须继续该项目下一轮。

不得仅因为 Claude Code 报告中出现以下软状态就停止项目或收束整批：

- `READY_FOR_USER_REVIEW`，但仍有 remaining gaps 或 next recommendation。
- `REFERENCE_ONLY`。
- 缺少 actual screenshot，但 reference screenshot 可用。
- `QWEN_COMPARE_SCREENSHOTS_COMPLETED=NO`，但 qwen reference inspect 已完成。
- `WINDOWS_HOST_VALIDATION_PENDING`，但仍有 WinUI/XAML 静态修复可做。
- UI 仍有剩余差异或 Claude Code 明确建议下一轮继续。

这些情况应标记为 `ROUND_COMPLETE_CONTINUE_ELIGIBLE`。若时间和轮次预算允许，Codex Agent 应继续下一轮。

达到 `BATCH_TIME_BUDGET_MINUTES` 后：

1. 设置 `TIME_BUDGET_REACHED=YES`。
2. 设置 `NO_NEW_ROUNDS=YES`。
3. 不再启动任何新正式轮。
4. 若没有运行中的正式任务，项目进入 `STOPPED_BY_TIME_BUDGET`。
5. 若有运行中的正式任务，允许其自然结束；结束后进入 `STOPPED_BY_TIME_BUDGET`。

不得因为时间预算到达而强杀正在正常运行的 Claude Code。

## 轮次预算行为

达到 `MAX_REPORT_ROUNDS_PER_PROJECT` 后：

1. 设置 `NO_NEW_ROUNDS=YES`。
2. 若没有运行中的正式任务，项目进入 `STOPPED_BY_ROUND_BUDGET`。
3. 若有运行中的正式任务，允许其自然结束；结束后进入 `STOPPED_BY_ROUND_BUDGET`。

轮次预算按项目分别计算。一个项目达到轮次上限，不影响其他项目在预算内继续。

## 终止状态枚举

批次结束时，每个项目必须进入以下任一终止状态：

- `STOPPED_BY_ROUND_BUDGET`
- `STOPPED_BY_TIME_BUDGET`
- `READY_FOR_USER_REVIEW`
- `BLOCKED_NEEDS_USER`
- `FAILED_PREFLIGHT`
- `MODEL_MISMATCH`
- `WORKDIR_MISMATCH`
- `SOURCE_READONLY_FAILED`
- `TOOLCHAIN_MISSING`
- `HOST_ENV_BLOCKED`
- `TOOLCHAIN_REPAIR_NEEDS_USER`
- `QWEN_INVALID_VISUAL_EVIDENCE`
- `INSTALL_FOR_SCREENSHOT_FAILED`
- `ANDROID_FOREGROUND_PACKAGE_MISMATCH`
- `USER_DEVICE_INTERVENTION_REQUIRED`
- `MANUAL_DECISION_REQUIRED`

只有所有项目均进入终止状态，才能输出批次最终主管摘要。

## 硬阻塞与软状态

允许项目立即终止的硬阻塞包括：

- `MODEL_MISMATCH`
- `WORKDIR_MISMATCH`
- `SOURCE_READONLY_FAILED`
- `API_402_INSUFFICIENT_BALANCE`
- `BILLING_MODEL_MISMATCH`
- `LOCAL_EXECUTION_POLICY_BLOCKED`
- `INSTALL_FOR_SCREENSHOT_FAILED`
- `USER_DEVICE_INTERVENTION_REQUIRED`
- 明确边界违规，例如 `git checkout/reset/restore/clean`、清理用户级 `~/.hvigor`、全局安装 `pnpm/npm/ohpm`、删除 visual evidence、checkpoint 或 report。
- 真实需要用户手动决策且 Claude Code 无法安全继续。

以下不是硬阻塞，不能自动终止项目：

- `READY_FOR_USER_REVIEW` 但仍有明确剩余任务。
- `REFERENCE_ONLY`。
- actual screenshot 暂时不可用，但 reference screenshot 可用。
- Windows host validation pending，但仍可做静态 WinUI/XAML 修复。
- qwen 已完成 reference inspect，但尚未完成 compare screenshots。
- 共享 Android Emulator 当前前台包名不匹配但 adb 可用、目标 App 可安装或可启动。该情况应先自动启动目标 App 并重试前台校验，不得默认等待用户手动切换。

`QWEN_VALID_VISUAL_EVIDENCE=REFERENCE_ONLY` 是有效退化路径，不是失败状态。只要用户提供的 reference screenshots 可用，且 qwen 已完成 reference 理解，Claude Code 可以继续根据 reference screenshots、Apple 源项目只读信息和目标项目当前实现修正 UI。后续若获得 actual screenshot，再补做 actual inspect 或 compare。

`READY_FOR_USER_REVIEW` 只有在以下条件同时满足时才可作为真实终止态：

1. 构建、测试或相应平台验证已经给出明确结果。
2. qwen reference-first 视觉理解已经完成。
3. 能获取 actual screenshot 时，已完成 actual inspect 或 compare。
4. 剩余问题仅为用户主观验收，不存在明确下一步可执行任务。
5. Claude Code 报告中没有 `NEXT_ROUND_RECOMMENDATION` 或类似继续修改建议。

如果报告仍包含 remaining UI differences、remaining functional gaps、next recommendation、actual screenshot missing but reference available、review page still needs compare、dark mode still incomplete、WinUI validation pending with static fixes possible，则不得终止，应继续下一轮。

## 项目异步处理规则

五项目并行时采用异步处理：

- 哪个项目先完成报告，就先读取和处理哪个项目。
- 不等所有项目统一完成。
- 已完成项目若预算允许且无阻塞，可以先进入下一轮。
- 阻塞项目记录原因并暂停，不阻塞其他项目。
- 每个项目的下一轮 prompt 必须基于该项目自己的最新报告、checkpoint 和用户反馈。

## 五项目并行调度循环

循环步骤：

1. 初始化批次参数和项目状态。
2. 为每个项目建立独立真实终端。
3. 执行 `cd -> pwd -> claude`。
4. 发送短握手。
5. 对 READY 项目发送正式任务。
6. 约每 30 秒读取所有运行中项目输出。
7. 对最先返回报告的项目立即处理。
8. 更新 `ROUNDS_COMPLETED`、预算、状态和 checkpoint。
9. 若允许继续，启动该项目下一轮；否则进入终止状态。
10. 所有项目终止后输出最终主管摘要。

## 示例批处理参数

```text
BATCH_NAME=apple-ui-perfect-parity
CONCURRENCY=5
BATCH_TIME_BUDGET_MINUTES=120
MAX_REPORT_ROUNDS_PER_PROJECT=2
STOP_MODE=SOFT_STOP
AUTO_CONTINUE_WITHIN_BUDGET=YES
NO_NEW_ROUNDS_AFTER_TIME_BUDGET=YES
WAIT_RUNNING_ROUNDS_TO_FINISH=YES
VISION_VALIDATION_MAX_ROUNDS=2
```

需要后续确认：每个批次的实际时间预算、轮次上限、目标项目集合和预期 Claude 模型必须由用户或上游调度说明明确给出。

## 标准批次模板

```text
BATCH_NAME:
CONCURRENCY:
BATCH_TIME_BUDGET_MINUTES:
MAX_REPORT_ROUNDS_PER_PROJECT:
STOP_MODE: SOFT_TIME_BUDGET
AUTO_CONTINUE_WITHIN_BUDGET: YES/NO
NO_NEW_ROUNDS_AFTER_TIME_BUDGET: YES
WAIT_RUNNING_ROUNDS_TO_FINISH: YES
```

一轮定义为：短握手通过、正式任务发出、Claude Code 执行、结构化报告返回、Codex/主管判断完成。仅 handshake、路径检查、模型错误、执行策略拦截、API 402、prompt 未送达、边界违规 incident report 不计有效迁移轮次。

## 继续条件

在 `AUTO_CONTINUE_WITHIN_BUDGET=YES` 时，只要当前时间未达到预算、项目轮次未达到上限、没有硬阻塞、且仍有可执行下一步，就应继续下一轮。不得因为 Claude Code 写了 `READY_FOR_USER_REVIEW`、`REFERENCE_ONLY`、`actual screenshot unavailable`、`WINDOWS_HOST_VALIDATION_PENDING` 就提前收束。

## 硬阻塞

允许立即暂停或终止项目的硬阻塞：

- `MODEL_MISMATCH`
- `WORKDIR_MISMATCH`
- `SOURCE_READONLY_FAILED`
- `API_402_INSUFFICIENT_BALANCE`
- `BILLING_MODEL_MISMATCH`
- `LOCAL_EXECUTION_POLICY_BLOCKED`
- git checkout/reset/restore/clean。
- 清理 `~/.hvigor` 或用户级 DevEco/HarmonyOS SDK 缓存。
- 全局安装 `pnpm`、npm、ohpm。
- 删除 visual evidence、checkpoint、state、report。
- 真实需要用户决策且 Claude 无法安全继续。

## 软状态

以下状态只能作为下一轮输入，不能单独作为终止理由：

- `READY_FOR_USER_REVIEW` 但仍有 remaining gaps 或 next recommendation。
- `REFERENCE_ONLY`。
- actual screenshot 暂不可得，但 reference screenshot 可用。
- qwen 已完成 reference inspect 但尚未 compare。
- `WINDOWS_HOST_VALIDATION_PENDING`，但静态 WinUI/XAML 修复仍可做。
- UI 仍有剩余差异。

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
QWEN_INVALID_VISUAL_EVIDENCE
INSTALL_FOR_SCREENSHOT_FAILED
ANDROID_FOREGROUND_PACKAGE_MISMATCH
USER_DEVICE_INTERVENTION_REQUIRED
WINDOWS_HOST_VALIDATION_PENDING
MANUAL_DECISION_REQUIRED
```

## 项目批次偏好

- Kikaria-Android：首页和背诵页优先；可整体重构 UI shell；保持 Android build/test 绿色。
- Kikaria-HarmonyOS：先编译；构建未恢复前不堆功能；若需要用户级工具链操作，进入 `TOOLCHAIN_REPAIR_NEEDS_USER`。
- Rokurics-Android：必须 reference-first；dark mode/theme support 是明确任务；actual screenshot 可通过 adb 继续闭环。
- Rokurics-HarmonyOS：有效 Preview/设备截图优先；禁止用户级工具链清理或全局包安装。
- Rokurics-Windows：WinUI 3/XAML 静态修复可在 Windows 主机验证前继续；真正 build/launch 只能在 Win11 ARM + VS2022 验证。
