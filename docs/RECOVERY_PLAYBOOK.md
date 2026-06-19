# Recovery Playbook

## 总原则

恢复时只从最新可确认状态继续。

不得从第一轮重跑。不得重复发送上一轮 prompt。不得用“进程还活着”当作进度。不得清理工作区。不得在状态未知时继续正式迁移。

Agent / ExAgent 中，事实来源是：

1. Supervisor checkpoint / batch state。
2. 指定输出文件：`DeepCode-output/<BATCH_NAME>/*.md` 与 `QwenCode-output/<BATCH_NAME>/*.md`。
3. 可观察终端或 live log。
4. 用户明确反馈。

worker 终端上下文、历史记忆或“窗口还在”不作为跨轮状态来源。

## 新窗口恢复

新窗口恢复不得直接启动新轮。必须先读取 checkpoint，并对每个项目归类：

- 已有 DeepCode 输出文件且 `REPORT_COMPLETE=YES`：可纳入主管摘要。
- 已有 QwenCode 输出文件且 `REPORT_COMPLETE=YES`：可作为下一轮 DeepCode 输入。
- one-shot 正在运行且可观察：继续观察，不重复发送 prompt。
- one-shot prompt 未送达：记录未送达，不计轮次。
- 输出文件缺失：不计轮次，进入恢复判断。
- 状态未知：暂停等待用户。

## checkpoint 必填字段

checkpoint 至少应包含：

```text
BATCH_NAME
PROJECT_NAME
TARGET_PATH
LAST_CONFIRMED_DEEPCODE_ROUND
LAST_CONFIRMED_QWENCODE_ROUND
LATEST_DEEPCODE_REPORT_PATH
LATEST_QWENCODE_REFERENCE_REPORT_PATH
LATEST_QWENCODE_ACTUAL_REPORT_PATH
LATEST_QWENCODE_COMPARE_REPORT_PATH
LATEST_REFERENCE_SCREENSHOT_PATH
LATEST_ACTUAL_SCREENSHOT_PATH
LAST_PROMPT_SENT_AT
LAST_REPORT_RECEIVED_AT
LAST_KNOWN_STATUS
ROUNDS_COMPLETED
VISION_ROUNDS_COMPLETED
TIME_BUDGET_REACHED
NO_NEW_ROUNDS
BLOCKER
NEXT_ACTION
```

如果 checkpoint 与输出文件冲突，以输出文件和用户可观察的最新终端事实为准，并在主管摘要中标注冲突。

## one-shot 进程还活着但无输出文件

进程存在不等于任务有效推进。

处理顺序：

1. 读取可观察输出。
2. 判断是否仍有新增输出。
3. 检查指定 `OUTPUT_FILE` 是否存在。
4. 检查输出文件是否包含 `REPORT_COMPLETE=YES`。
5. 若长时间无输出且无报告，标记 `BLOCKED_NEEDS_USER` 或 `MANUAL_DECISION_REQUIRED`。
6. 不重复发送上一轮 prompt。

## prompt 没送达

如果无法确认 one-shot prompt 已送达：

1. 不计入有效轮次。
2. 不假设 worker 已执行。
3. 记录 `PROMPT_DELIVERY_UNKNOWN`。
4. 检查指定输出文件是否存在。
5. 只有确认旧 prompt 没有执行且用户允许，才生成新的当前轮 prompt，且必须使用新的输出文件名。

## DeepCode 输出缺失

如果 DeepCode 输出缺失：

```text
DEEPCODE_OUTPUT_MISSING
```

处理方式：

1. 不计入业务轮次。
2. 保留 live log 与 prompt 摘要。
3. 不让 QwenCode 或下一轮 DeepCode 猜测该轮结果。
4. 若用户允许重试，创建新的 DeepCode 输出路径，不覆盖旧路径。

## QwenCode 输出缺失

如果 QwenCode 输出缺失：

```text
QWENCODE_OUTPUT_MISSING
```

处理方式：

1. 不计入视觉尝试成功。
2. 不把该轮称为视觉验收完成。
3. 不把缺失报告路径传给 DeepCode。
4. 若用户允许重试，创建新的 QwenCode 输出路径，不覆盖旧路径。

## QwenCode 报告路径丢失

如果 DeepCode 下一轮需要 QwenCode 报告，但 checkpoint 中缺少路径：

1. 搜索当前项目 `QwenCode-output/<BATCH_NAME>/`。
2. 只接受包含 `REPORT_COMPLETE=YES` 的报告。
3. 按文件名、时间戳和字段确认是否为当前项目当前轮次的报告。
4. 无法确认时进入 `MANUAL_DECISION_REQUIRED`。
5. 不让 DeepCode 凭空根据“上一轮 Qwen 结论”继续。

## API 402 / 计费异常

出现 API 402、计费模型异常或后台计费指标不一致：

1. 立即停止启动新轮。
2. 标记受影响项目 `BLOCKED_NEEDS_USER`。
3. 记录错误原文的短摘要。
4. 不反复重试。
5. 不切换模型规避，除非用户明确授权。
6. 向用户说明需要处理余额或计费配置。

## 模型不匹配

如果 DeepCode 输出报告不是预期 DeepSeek 后端，或无法确认：

```text
MODEL_MISMATCH
```

如果 QwenCode 报告不是 Qwen3.7-Plus 或无法确认：

```text
QWENCODE_MODEL_MISMATCH
```

处理方式：

1. 不计入有效轮次。
2. 不继续使用该报告做下一轮输入。
3. 停止启动新轮。
4. 等待用户确认模型策略。

## 本地执行策略拦截

遇到本地执行策略拦截：

1. 不绕过。
2. 不全局授权。
3. 不改用隐藏 headless 正式通道。
4. 记录被拦截操作、路径、项目、目的。
5. 请求当前对话、当前批次、当前路径的最小授权。
6. 用户拒绝或未确认时，进入 `BLOCKED_NEEDS_USER`。

## 路径不匹配

如果 `pwd` 或 worker 报告 `PWD` 与目标项目路径不一致：

1. 不计入有效轮次。
2. 标记 `WORKDIR_MISMATCH`。
3. 不使用该输出作为下一轮输入。
4. 如需重试，重新执行 `cd -> pwd -> one-shot invocation`，并使用新的输出文件路径。

## Apple 只读边界失败

如果发现 DeepCode、Spark 或 OpenCode 尝试或已经写入 Apple 源项目：

1. 立即暂停该项目。
2. 记录路径和操作摘要。
3. 不自动修复、不自动回滚。
4. 不执行 Git 清理。
5. 等待用户决定。

如果只是执行器声称需要写 Apple 源项目，应拒绝该要求并要求其改为只读参考。

## 视觉证据被删除或无效

如果发现执行器删除当前批次截图、QwenCode 输出、DeepCode 输出、state、checkpoint、report、batch state 或 `.outposts-supervisor/visual-evidence`：

1. 立即停止该项目继续运行。
2. 不自动重建旧证据，不覆盖旧 `RUN_ID`。
3. 记录被删除的路径摘要。
4. 将项目标记为 `BLOCKED_NEEDS_USER`。
5. 若用户要求继续视觉验收，必须创建新的 `RUN_ID` 证据目录。

如果 QwenCode 被调用但输入图片不是有效 App、Preview、设备或窗口截图：

1. 报告 `QWENCODE_CALLED=YES`。
2. 报告 `QWENCODE_VALID_VISUAL_EVIDENCE=NO`。
3. 报告 `QWENCODE_COMPARE_COMPLETED=NO`，除非 reference 与 actual 均有效且已完成对比。
4. 不把该轮称为有效视觉验收。
5. 需要继续时，先获取有效 actual screenshot，再由 supervisor 调用 QwenCode。

全桌面截图只有在裁剪出明确 App、Preview 或窗口区域后，才能作为有效视觉证据。

## actual screenshot unavailable

actual screenshot 不可用不是自动终止条件。处理方式：

1. 先使用 reference screenshot 调用 QwenCode inspect。
2. 报告 `QWENCODE_VALID_VISUAL_EVIDENCE=REFERENCE_ONLY`。
3. 报告 `ACTUAL_SCREENSHOT_BLOCKER`。
4. 继续基于 QwenCode reference 报告、Apple 源项目只读信息和目标项目当前实现修正 UI，除非批次目标明确要求必须 actual compare。
5. 后续获取 actual screenshot 后再调用 QwenCode inspect/compare。

## Android 共享 Emulator 截图链恢复

共享 Android Emulator 出现截图问题时，不得把常规流程切换为用户手动操作。共享 Emulator 只表示同一时间只能有一个 Android 项目操作设备；DeepCode 仍应自动完成目标 App 的安装、启动、前台校验和截图。

若发现 actual screenshot 是另一个项目的 App、launcher、桌面、权限弹窗或无关界面：

1. 标记原图为 `INVALID_WRONG_APP_SCREENSHOT` 或 `QWENCODE_VALID_VISUAL_EVIDENCE=NO`。
2. 保留旧图和旧 QwenCode 输出，不删除、不覆盖。
3. 重新获取 `ANDROID_EMULATOR_LOCK`。
4. 确认 `/Users/vita/Library/Android/sdk/platform-tools/adb` 与 `adb devices -l`。
5. 从当前项目 Gradle 配置读取 `applicationId`，不得猜包名。
6. 检查目标 App 是否已安装；如未安装，可在当前项目内执行最小 `installDebug` 或等价安装命令。
7. 用 adb 自动启动目标 App，并校验前台包名。
8. 前台包名正确后，用唯一文件名重新截图到 visual-evidence。
9. DeepCode 报告 actual screenshot 路径。
10. Supervisor 再把截图路径交给 QwenCode actual inspect 或 compare。

不得要求用户在 Android Studio 里手动选择项目、点击 Build/Run 或回复 `READY`。

只有以下情况才报告 `USER_DEVICE_INTERVENTION_REQUIRED`：

- `adb devices` 完全看不到 emulator。
- emulator 为 `offline` 或 `unauthorized`。
- 设备出现执行器无法处理的系统授权或权限弹窗。
- `installDebug` 因签名、SDK 或 Gradle 环境失败，且无法在项目内修复。
- App 启动后出现必须人工处理的系统弹窗。

## 状态未知

状态未知时：

1. 暂停。
2. 输出当前已知事实。
3. 列出缺失证据。
4. 给出最小恢复选项。
5. 等待用户决策。

不得为了“继续推进”而猜测上一轮结果。

## 边界事故报告字段

```text
BOUNDARY_INCIDENT_REPORT
PROJECT_NAME
INCIDENT_TYPE
FORBIDDEN_ACTION_DETECTED
EVIDENCE_SOURCE
FILES_OR_DIRS_POSSIBLY_AFFECTED
USER_LEVEL_TOOLCHAIN_TOUCHED
VISUAL_EVIDENCE_TOUCHED
DEEPCODE_OUTPUT_TOUCHED
QWENCODE_OUTPUT_TOUCHED
GIT_DESTRUCTIVE_COMMAND_TOUCHED
AUTO_RECOVERY_ATTEMPTED
SAFE_TO_CONTINUE
USER_MANUAL_CHECK_REQUIRED
NEXT_RECOMMENDATION
```
