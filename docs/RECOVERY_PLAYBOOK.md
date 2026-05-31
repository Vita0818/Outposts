# Recovery Playbook

## 总原则

恢复时只从最新可确认状态继续。

不得从第一轮重跑。不得重复发送上一轮 prompt。不得用“进程还活着”当作进度。不得清理工作区。不得在状态未知时继续正式迁移。

恢复目标是重建事实：

- 当前 Codex 窗口是否可靠。
- 每个 Claude Code 会话是否存在、可观察、路径正确、模型正确。
- 上一轮 prompt 是否送达。
- Claude Code 是否返回结构化报告。
- 是否已消耗有效轮次。
- 是否需要用户重新授权或决策。

## Codex 窗口卡死

1. 新窗口进入 `/Users/vita/Vitemis/Outposts`。
2. 执行启动前检查。
3. 读取 `AGENTS.md` 和 `docs/` 调度文档。
4. 读取 `.outposts-supervisor/` 中最新 checkpoint、batch state、summary、report。
5. 不读取子项目源码。
6. 不重跑迁移。
7. 判断旧 Claude Code 会话是否仍可观察。
8. 如果状态未知，暂停等待用户。

## remote compact / stream disconnected

出现 remote compact、stream disconnected、网络压缩失败时：

1. 记录发生时间和受影响项目。
2. 不假设 prompt 已送达。
3. 不假设任务失败或成功。
4. 检查真实终端或 live log 的最后可见输出。
5. 若 Claude Code 仍在正常输出，继续只读观察。
6. 若无法确认状态，进入 `MANUAL_DECISION_REQUIRED`。

## 新窗口恢复

新窗口恢复不得直接启动新轮。必须先读取 checkpoint，并对每个项目归类：

- 已有结构化报告：可纳入主管摘要。
- 正在运行且可观察：继续观察，不重复发送 prompt。
- 会话存在但无输出：检查是否等待输入或卡死。
- prompt 未送达：记录未送达，不计轮次。
- 状态未知：暂停等待用户。

## checkpoint 恢复

checkpoint 至少应包含：

```text
BATCH_NAME
PROJECT_NAME
TARGET_PATH
SESSION_NAME
LAST_CONFIRMED_ROUND
LAST_PROMPT_SENT_AT
LAST_REPORT_RECEIVED_AT
LAST_KNOWN_STATUS
ROUNDS_COMPLETED
TIME_BUDGET_REACHED
NO_NEW_ROUNDS
BLOCKER
NEXT_ACTION
```

如果 checkpoint 与 live log 冲突，以用户可观察的最新终端事实为准，并在主管摘要中标注冲突。

## Claude Code 进程还活着但无报告

进程存在不等于任务有效推进。

处理顺序：

1. 读取可观察输出。
2. 判断是否等待用户输入。
3. 判断是否仍有新增输出。
4. 判断是否超出正常静默窗口。
5. 若长时间无输出且无报告，标记 `BLOCKED_NEEDS_USER` 或 `MANUAL_DECISION_REQUIRED`。
6. 不重复发送上一轮 prompt。

## prompt 没送达

如果无法确认 prompt 已送达：

1. 不计入有效轮次。
2. 不假设 Claude Code 已执行。
3. 记录 `PROMPT_DELIVERY_UNKNOWN`。
4. 若会话仍可用，先用短握手重新确认模型与路径。
5. 只有确认旧 prompt 没有执行且用户允许，才生成新的当前轮 prompt。

## API 402 Insufficient Balance

出现 API 402：

1. 立即停止启动新轮。
2. 标记受影响项目 `BLOCKED_NEEDS_USER`。
3. 记录错误原文的短摘要。
4. 不反复重试。
5. 不切换模型规避，除非用户明确授权。
6. 向用户说明需要处理余额或计费配置。

## DeepSeek 后台显示 Flash 增长

如果 DeepSeek 后台或计费面板显示 Flash 增长，而本批次预期不是 Flash：

1. 停止启动新轮。
2. 对所有未开始正式轮的项目暂停。
3. 对运行中项目等待自然报告或用户指示。
4. 记录后台观察与 Claude 握手模型。
5. 请求用户确认是否继续、切换模型或终止批次。

## Claude 握手显示 Opus/Sonnet

如果短握手显示 Opus、Sonnet 或任何非预期模型：

1. 不发送正式任务 prompt。
2. 标记 `MODEL_MISMATCH`。
3. 记录 `MODEL`、`PWD`、项目名。
4. 等待用户确认模型策略。

如果用户没有指定预期模型，但模型会影响成本或能力，也应在主管摘要中列为不确定项。

## 本地执行策略拦截

遇到本地执行策略拦截：

1. 不绕过。
2. 不全局授权。
3. 不改用隐藏 headless 正式通道。
4. 记录被拦截操作、路径、项目、目的。
5. 请求当前对话、当前批次、当前路径的最小授权。
6. 用户拒绝或未确认时，进入 `BLOCKED_NEEDS_USER`。

## 路径不匹配

如果 `pwd` 或 Claude 握手 `PWD` 与目标项目路径不一致：

1. 不发送正式任务 prompt。
2. 标记 `WORKDIR_MISMATCH`。
3. 关闭、搁置或重新建立会话。
4. 重新执行 `cd -> pwd -> claude`。
5. 记录错误会话名，避免误用。

## Apple 只读边界失败

如果发现 Claude Code 尝试或已经写入 Apple 源项目：

1. 立即暂停该项目。
2. 记录路径和操作摘要。
3. 不自动修复、不自动回滚。
4. 不执行 Git 清理。
5. 等待用户决定。

如果只是 Claude Code 声称需要写 Apple 源项目，应拒绝该要求并要求其改为只读参考。

## 构建工具缺失

构建工具缺失时：

1. Claude Code 应报告缺失工具、命令、平台、错误摘要。
2. Codex Agent 不自行安装依赖。
3. 不把工具链缺失计为迁移完成。
4. 若当前批次目标允许环境修复，需用户确认后再继续。
5. 否则标记 `TOOLCHAIN_MISSING`。

## Windows/.NET 环境缺失

在 macOS host 上调度 Windows/.NET 项目时，如果缺少 dotnet、Windows SDK、MSBuild 或目标平台能力：

1. 不臆测构建结果。
2. 记录无法验证的命令和原因。
3. 要求 Claude Code给出无需本机执行的剩余风险说明。
4. 标记 `TOOLCHAIN_MISSING` 或 `READY_FOR_USER_REVIEW`，取决于用户目标。

## HarmonyOS 编译失败

HarmonyOS 编译失败时：

1. 区分源码错误、Hvigor 错误、DevEco SDK 初始化错误、签名或设备配置错误。
2. 只把 Claude Code 实际观察到的错误纳入报告。
3. 不清理项目 `.hvigor`、build、cache。
4. 不删除、清理或修改 `~/.hvigor`。
5. 不删除、清理或修改用户级 DevEco、HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
6. 不全局安装 `pnpm`、npm 包、ohpm 包或任何全局工具链依赖。
7. 不执行全局工具链修复。
8. 如果是环境缺失，标记 `TOOLCHAIN_MISSING`、`HOST_ENV_BLOCKED` 或 `TOOLCHAIN_REPAIR_NEEDS_USER`。
9. 只允许 Claude Code 在对应 Outposts 目标项目目录内修改源码和项目配置。

如果 Claude Code 已经执行用户级清理或全局安装尝试：

1. 立即停止该项目继续运行。
2. 不自动回滚、不执行 Git restore、不清理更多文件。
3. 要求 Claude Code 输出 incident report。
4. 将项目标记为 `BLOCKED_NEEDS_USER` 或 `TOOLCHAIN_REPAIR_NEEDS_USER`。
5. 在主管摘要中列出可能受影响的目录和后续人工处理建议。

## 视觉证据被删除或无效

如果发现 Claude Code 删除当前批次截图、qwen 输出、state、checkpoint、report、batch state 或 `.outposts-supervisor/visual-evidence`：

1. 立即停止该项目继续运行。
2. 不自动重建旧证据，不覆盖旧 `RUN_ID`。
3. 记录被删除的路径摘要。
4. 将项目标记为 `BLOCKED_NEEDS_USER`。
5. 若用户要求继续视觉验收，必须创建新的 `RUN_ID` 证据目录。

如果 `qwen-vision` 被调用但输入图片不是有效 App、Preview、设备或窗口截图：

1. 报告 `QWEN_CALLED=YES`。
2. 报告 `QWEN_VALID_VISUAL_EVIDENCE=NO`。
3. 报告 `QWEN_COMPARE_SCREENSHOTS_COMPLETED=NO`，除非 reference 与 actual 均有效且已完成对比。
4. 不把该轮称为有效视觉验收。
5. 需要继续时，先获取有效 actual screenshot，再调用 qwen。

全桌面截图只有在裁剪出明确 App、Preview 或窗口区域后，才能作为有效视觉证据。

## Android 共享 Emulator 截图链恢复

共享 Android Emulator 出现截图问题时，不得把常规流程切换为用户手动操作。共享 Emulator 只表示同一时间只能有一个 Android 项目操作设备；Claude Code 仍应自动完成目标 App 的安装、启动、前台校验和截图。

若发现 actual screenshot 是另一个项目的 App、launcher、桌面、权限弹窗或无关界面：

1. 标记原图为 `INVALID_WRONG_APP_SCREENSHOT` 或 `QWEN_VALID_VISUAL_EVIDENCE=NO`。
2. 保留旧图和旧 qwen 输出，不删除、不覆盖。
3. 重新获取 `ANDROID_EMULATOR_LOCK`。
4. 确认 `/Users/vita/Library/Android/sdk/platform-tools/adb` 与 `adb devices -l`。
5. 从当前项目 Gradle 配置读取 `applicationId`，不得猜包名。
6. 检查目标 App 是否已安装；如未安装，可在当前项目内执行最小 `installDebug` 或等价安装命令。
7. 用 adb 自动启动目标 App，并校验前台包名。
8. 前台包名正确后，用唯一文件名重新截图到 visual-evidence。
9. 再调用 qwen actual inspect 或 compare。

不得要求用户在 Android Studio 里手动选择项目、点击 Build/Run 或回复 `READY`。`ANDROID_WAITING_FOR_USER_APP_SWITCH` 不作为常规恢复状态使用。

只有以下情况才报告 `USER_DEVICE_INTERVENTION_REQUIRED`：

- `adb devices` 完全看不到 emulator。
- emulator 为 `offline` 或 `unauthorized`。
- 设备出现 Claude Code 无法处理的系统授权或权限弹窗。
- `installDebug` 因签名、SDK 或 Gradle 环境失败，且无法在项目内修复。
- App 启动后出现必须人工处理的系统弹窗。

若 `installDebug` 失败，报告 `INSTALL_FOR_SCREENSHOT_FAILED`，停止该项目截图链，不得继续 UI 修改。若前台包名重试 2 次后仍不匹配，报告 `ANDROID_FOREGROUND_PACKAGE_MISMATCH`，并保留所有证据。

## 状态未知

状态未知时：

1. 暂停。
2. 输出当前已知事实。
3. 列出缺失证据。
4. 给出最小恢复选项。
5. 等待用户决策。

不得为了“继续推进”而猜测上一轮结果。

## qwen permission gated

如果 Claude Code 报告 `qwen-vision` 权限受限：

1. 不切换到 bypass permissions。
2. 不扩大授权到其他 MCP server。
3. 检查当前项目 `.claude/settings.local.json` 中是否允许三个 qwen 工具。
4. 检查 `disabledMcpjsonServers` 是否包含 `qwen-vision`。
5. 只允许追加或恢复 qwen 三个视觉工具的最小 allow 规则。
6. 若仍不可用，报告 `QWEN_PERMISSION_GATED` 或 `QWEN_UNAVAILABLE_IN_SESSION`。

## actual screenshot unavailable

actual screenshot 不可用不是自动终止条件。处理方式：

1. 先使用 reference screenshot 做 qwen inspect。
2. 报告 `QWEN_VALID_VISUAL_EVIDENCE=REFERENCE_ONLY`。
3. 报告 `ACTUAL_SCREENSHOT_BLOCKER`。
4. 继续基于 reference screenshot、Apple 源项目只读信息和目标项目当前实现修正 UI，除非批次目标明确要求必须 actual compare。
5. 后续获取 actual screenshot 后再补做 inspect/compare。

## DevEco / hdc / screencapture 失败

HarmonyOS 截图失败时：

1. 区分 DevEco Preview 不可用、hdc 无 target、设备截图失败、macOS Screen Recording 权限缺失、截图不是有效 Preview 区域。
2. 不把无效桌面截图当作有效视觉证据。
3. 不清理 `~/.hvigor` 或用户级 SDK 缓存。
4. 不全局安装工具。
5. 如需用户级环境处理，报告 `TOOLCHAIN_REPAIR_NEEDS_USER` 或 `BLOCKED_BY_DEVECO_OR_DEVICE`。

## HarmonyOS 边界违规恢复

若 Claude Code 误触 `git checkout`、`git restore`、`git reset`、`git clean`、清理项目/用户级工具链缓存、全局安装包或删除证据：

1. 立即停止该项目继续迁移。
2. 不自动回滚，不执行更多 Git 修改命令。
3. 不清理更多文件。
4. 只做只读恢复报告。
5. 报告是否执行过相关命令、当前工作区是否可能被回退、项目内工具链镜像是否可能被删除/重建、是否安全继续、需要用户人工检查哪些文件。
6. 只有恢复报告显示 `SAFE_TO_CONTINUE=YES` 或用户明确确认后，才可重新纳入调度。

Kikaria-HarmonyOS 若恢复报告显示 `SAFE_TO_CONTINUE=YES`，可从 0 轮继续正式迁移；若显示 `UNKNOWN` 或 `NO`，必须等待用户人工检查。

## git checkout / restore / clean 误触后的只读报告字段

```text
RECOVERY_REPORT
PROJECT_NAME
TASK_CLASS=RECOVERY_REPORT_NOT_MIGRATION_ROUND
MODEL_CHECK_RESULT
PATH_CHECK_RESULT
READONLY_SCOPE_CONFIRMATION
READONLY_COMMANDS_RUN
FORBIDDEN_COMMANDS_NOT_RUN_THIS_RECOVERY
GIT_CHECKOUT_BUILD_PROFILE_JSON5_DETECTED
CURRENT_WORKTREE_POSSIBLY_REVERTED
BUILD_PROFILE_JSON5_STATUS
PROJECT_LOCAL_TOOLCHAIN_MIRROR_STATUS
USER_LEVEL_TOOLCHAIN_STATUS_THIS_RECOVERY
SAFE_TO_CONTINUE
IF_NOT_SAFE_USER_MANUAL_CHECK_REQUIRED
FILES_MODIFIED_THIS_RECOVERY
BUILD_RUN
TEST_RUN
MIGRATION_ROUND_COUNTED
BLOCKERS
NEXT_RECOMMENDATION
```
