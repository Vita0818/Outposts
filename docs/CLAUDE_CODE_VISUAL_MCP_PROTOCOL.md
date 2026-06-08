# Claude Code Visual MCP Protocol

## 定位

Claude Code 当前可使用 Qwen 多模态 MCP 工具：

```text
qwen-vision · connected · 3 tools
```

`qwen-vision` 不是主模型。主 Agent 仍然是 Claude Code / DeepSeek V4 Pro。主 Agent 负责推理、写代码、修改文件、运行命令、构建、测试和总结。

`qwen-vision` 只负责看图、识别截图、比较图片。它不得直接修改文件，不得接收 API Key、`.env`、token、密钥、证书、完整源码或私密配置。

Codex Agent 本体不直接调用 `qwen-vision`。Codex Agent 只负责在适当的 Claude Code 正式任务 prompt 中要求 Claude Code 使用 `qwen-vision`。

## Spark + Qwen 视觉流程

当本轮为 Spark + Qwen 视觉辅助模式时执行固定闭环：

1. Spark 读取任务与相关代码。
2. 生成或定位 reference 截图。
3. 生成 actual 截图。
4. 调用 Qwen3.7Plus 进行 `inspect_screenshot(reference)`、`inspect_screenshot(actual)`，并在两图都可得时执行 `compare_screenshots(reference, actual)`。
5. Spark 根据 Qwen 输出拆解视觉差异并修改代码。
6. Spark 运行构建/测试并按需要重拍截图。
7. 以新截图复跑 compare，直至达到任务停止条件或达到每批可视化轮次上限。

若实际截图缺失：

- 可先只做 reference inspect，进入 `REFERENCE_ONLY`。
- Spark 不得因此宣告视觉闭环完成。

## Spark + Qwen 视觉接入规则（可选 helper）

若当前环境已有 Qwen helper / MCP / API wrapper，优先复用，不要临时接新链路。  
若没有现有工具，不要硬编码 API Key，不要把 Key 写入仓库。

1. API Key 只允许从环境变量读取（如 `DASHSCOPE_API_KEY`、`QWEN_API_KEY`）。
2. 图像输入仅限截图路径（reference/actual）。
3. 输出报告建议落盘：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT>/qwen/
```
4. Spark 只读取 Qwen 报告，不得将源码、token、`.env`、证书或私密配置传给 Qwen。

## 工具清单

### `inspect_screenshot(image_path, goal="")`

用途：分析单张截图，输出页面结构、可见文字、颜色、布局、组件、视觉风格。

典型场景：

- UI 截图。
- 设计稿。
- App 页面。
- 网页截图识别。

### `compare_screenshots(reference_image_path, actual_image_path, goal="")`

用途：比较参考图和实际渲染图，输出视觉差异、匹配度、修正建议。

典型场景：

- UI 还原后，Claude Code 截取实际页面。
- 将实际渲染图与参考图对比。
- 根据差异继续修正布局、颜色、字体、间距、圆角、阴影和组件位置。

### `extract_text_and_controls(image_path)`

用途：从截图中提取文字、按钮、输入框、表格、图标等 UI 元素。

典型场景：

- OCR。
- 控件识别。
- 界面元素定位。

## 调用原则

1. 纯代码、后端、算法、文档任务不要调用 `qwen-vision`。
2. 只有涉及截图、设计稿、页面渲染图、视觉验收时才调用 `qwen-vision`。
3. `qwen-vision` 只处理图片，不处理源码。
4. 不要把 API Key、`.env`、token、密钥、证书、完整源码或私密配置传给 `qwen-vision`。
5. `qwen-vision` 只负责识图和视觉差异分析。
6. 文件修改、代码调整、构建、测试、总结都由 Claude Code 主 Agent 完成。
7. Codex Agent 本体不直接调用 `qwen-vision`；Codex 只负责在适当任务 prompt 中要求 Claude Code 使用 `qwen-vision`。
8. 如果没有可用截图路径，不得假装已经做了视觉识别；应要求 Claude Code 先生成或截取实际页面截图，或报告缺少视觉材料。
9. Spark + Qwen 模式下，Spark 不得自行判读截图，必须以 Qwen 输出作为唯一视觉证据。
10. 默认视觉验收最多重复 2 轮，避免无限微调。
10. 如果用户明确要求“完美复刻 Apple UI”，Claude Code 应优先尝试建立 reference screenshot 和 actual screenshot 的视觉对比闭环。

## 单张截图识别标准提示词

当 Claude Code 需要分析参考截图时，Codex 给 Claude 的 prompt 中可使用：

```text
请调用 qwen-vision 的 inspect_screenshot 工具分析这张图片：

图片路径：
/绝对路径/到/图片.png

目标：
识别页面结构、可见文字、主要颜色、布局、组件、视觉风格和可能的实现注意事项。

要求：
1. qwen-vision 只负责识图。
2. 不要修改任何文件。
3. 不要读取或传递 API Key、.env、token 或私密配置。
4. 工具返回后，由你作为主 Agent 对识别结果进行归纳和判断。
5. 输出应包含：
   - 页面整体描述
   - 区域布局
   - 可见文字
   - 主要颜色和字体风格
   - 组件列表
   - UI 复刻时的注意事项
   - 不确定之处
```

## UI 还原与视觉验收标准流程

当任务目标是 UI parity、视觉复刻或截图验收时，Claude Code 应采用以下流程：

1. 先调用 `qwen-vision.inspect_screenshot` 分析参考截图。
2. 主 Agent 根据识别结果修改目标项目代码。
3. 主 Agent 本地运行目标项目，并截取实际渲染图。
4. 调用 `qwen-vision.compare_screenshots` 对比：
   - `reference_image_path` = 参考截图路径
   - `actual_image_path` = 实际渲染截图路径
5. 主 Agent 根据 `qwen-vision` 给出的差异继续修正代码。
6. 默认最多重复 2 轮视觉验收。
7. 每轮视觉验收后必须在报告中说明：
   - 是否成功生成实际截图；
   - 是否成功调用 `qwen-vision`；
   - 主要视觉差异；
   - 已修复的差异；
   - 剩余差异；
   - 无法验证的原因。

视觉验收不能替代构建、测试或用户人工验收。

## 视觉验收环境规则

### IDE / 模拟器职责

Android：

- Android Studio 只作为 Emulator 管理工具。
- 不需要为 Kikaria-Android 和 Rokurics-Android 分别启动两个 Android Studio；一个 Android Studio 足够。
- 机器性能允许时可以启动两个 Android Emulator；否则只启动一个 Emulator，并让两个 Android 项目串行做视觉截图。
- 共享 Emulator 只意味着 Android 项目的截图阶段必须串行获取 `ANDROID_EMULATOR_LOCK`；不意味着用户需要在 Android Studio 中手动切项目、手动选择 Build/Run，或回复 `READY`。
- Kikaria-Android 和 Rokurics-Android 共用同一个 Emulator 时，Claude Code 必须通过 adb、Gradle、安装、启动和前台包名校验自动切换目标 App。
- 推荐默认设备名：`Outposts_Android_UI`。
- 如果检测不到 emulator，不得声称完成 Android 视觉验收，必须报告 `BLOCKED_BY_NO_EMULATOR`。

HarmonyOS：

- HarmonyOS 视觉验收需要 DevEco Preview、Emulator 或真机截图。
- DevEco 启动一次即可。
- Kikaria-HarmonyOS 和 Rokurics-HarmonyOS 的视觉验收建议串行，不要强行并行操作同一个 DevEco。
- 如果 DevEco、Preview 或设备不可用，不得声称完成 HarmonyOS 视觉验收，必须报告 `BLOCKED_BY_DEVECO_OR_DEVICE`。

Windows：

- Rokurics-Windows 需要 Windows/.NET 环境才能做真实 UI 验证。
- 如果当前 macOS host 无 .NET SDK 或 Windows UI 环境，应报告 `HOST_ENV_BLOCKED`。
- 不得假装完成 Windows 视觉验收。

### 截图证据目录

所有视觉证据统一写入：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT_NAME>/
```

每个项目使用固定子目录：

```text
reference/
actual/
qwen/
```

- `reference/` 存 Apple 端参考截图。
- `actual/` 存目标端实际渲染截图。
- `qwen/` 存 `qwen-vision` 识别或对比摘要。

不得把截图散落到子项目源码目录。不得把截图写进 Apple 源项目。不得清理子项目 build/cache。

不得删除 `.outposts-supervisor/visual-evidence`、当前批次截图、qwen 输出、state、checkpoint、report 或 batch state。不得把“清理临时截图”作为任务收尾动作。如果需要重新截图，必须创建新的 `RUN_ID` 目录；不得覆盖、删除或复用旧 `RUN_ID` 证据目录。

### Android 截图规则

如果 emulator 已启动，Claude Code 应优先使用 adb 获取纯设备截图。默认 adb 绝对路径为：

```text
/Users/vita/Library/Android/sdk/platform-tools/adb
```

Android screenshot preflight 必须按以下顺序执行：

1. 获取 `ANDROID_EMULATOR_LOCK`。同一时间只有一个 Android 项目可以操作共享 Emulator。
2. 确认 adb 绝对路径可用；不得只运行 `adb` 后声称 `adb not found`。
3. 执行 `adb devices -l`，确认目标 emulator 存在且不是 `offline` 或 `unauthorized`。
4. 从当前 Android 目标项目的 Gradle 配置、manifest 或等价项目配置读取 `applicationId`；不得猜包名。
5. 检查目标 App 是否已安装。
6. 如果 App 未安装，允许在当前目标项目内执行最小安装流程，例如 `./gradlew installDebug` 或项目等价安装命令。该步骤只属于 `SCREENSHOT_PREFLIGHT`，不得伴随 UI 修改。
7. 使用 adb 自动启动目标 App。
8. 校验当前前台包名等于目标 `applicationId`。
9. 前台包名正确后，执行 `screencap`。
10. 截图保存到 visual-evidence 的 `actual/` 目录，使用唯一文件名。
11. 释放 `ANDROID_EMULATOR_LOCK`。

优先启动命令：

```text
/Users/vita/Library/Android/sdk/platform-tools/adb -s <DEVICE_SERIAL> shell monkey -p <APPLICATION_ID> -c android.intent.category.LAUNCHER 1
```

如果 `monkey` 无法启动，Claude Code 可以从项目 manifest、Gradle 配置或设备 package manager 输出确认主 Activity 后使用：

```text
adb -s <DEVICE_SERIAL> shell am start -n <APPLICATION_ID>/<MAIN_ACTIVITY>
```

不得猜 Activity。必须从项目配置或设备输出确认。

截图命令形态：

```text
/Users/vita/Library/Android/sdk/platform-tools/adb -s <DEVICE_SERIAL> exec-out screencap -p > <ACTUAL_SCREENSHOT_PATH>
```

如果有多个 emulator，必须显式指定 `-s <DEVICE_SERIAL>`。不得把错误设备、错误 App 或另一个项目的 App 截图当作当前项目截图。

前台包名校验可使用以下命令或等价方式：

```text
adb -s <DEVICE_SERIAL> shell dumpsys activity activities
adb -s <DEVICE_SERIAL> shell dumpsys window
adb -s <DEVICE_SERIAL> shell pidof <APPLICATION_ID>
```

如果前台包名不是目标 `applicationId`：

1. 不得截图。
2. 自动重新启动目标 App。
3. 再次校验前台包名。
4. 最多重试 2 次。
5. 仍失败才报告 `ANDROID_FOREGROUND_PACKAGE_MISMATCH`。

如果 `installDebug` 或等价安装失败，报告 `INSTALL_FOR_SCREENSHOT_FAILED`，停止该项目截图链，不得继续 UI 修改。

Android 报告必须包含：

```text
ANDROID_EMULATOR_LOCK:
ANDROID_ADB_PATH:
ANDROID_ADB_STATUS:
ANDROID_DEVICE_SERIALS:
APPLICATION_ID_SOURCE:
APPLICATION_ID:
INSTALL_NEEDED_FOR_SCREENSHOT:
INSTALL_COMMAND:
INSTALL_RESULT:
APP_LAUNCH_COMMAND:
FOREGROUND_PACKAGE_CHECK:
FOREGROUND_PACKAGE:
ACTUAL_SCREENSHOT_PATH:
SCREENSHOT_CHAIN_STATUS:
```

`ANDROID_WAITING_FOR_USER_APP_SWITCH` 不作为常规流程使用。只有 adb 完全看不到 emulator、设备 `offline/unauthorized`、系统权限弹窗无法处理、安装因签名或 Gradle 环境失败且无法项目内修复、或 App 启动后出现必须人工处理的系统弹窗时，才可报告 `USER_DEVICE_INTERVENTION_REQUIRED`。

actual screenshot 文件名必须唯一，例如：

```text
round1-home-kikaria-20260530-130501.png
round1-dashboard-rokurics-20260530-130544.png
```

不得覆盖旧图。若先写项目内临时文件，必须复制到 visual-evidence 后才算截图链成功；项目内临时截图不能作为最终视觉证据。

### HarmonyOS 截图规则

如果 DevEco Preview 没有可直接导出的纯预览截图命令，允许先使用 macOS `screencapture` 抓取 DevEco 可见窗口或全屏截图，保存到项目的 `actual/` 目录。

如能进一步裁剪出纯手机预览区域，应保存为 `actual/home-preview-cropped.png`，并优先交给 `qwen-vision`。如果只能获得完整 IDE 截图，报告必须明确截图类型为完整 IDE 截图，并要求 `qwen-vision` 聚焦 Preview 区域。

Rokurics-HarmonyOS 的黄色色块验证必须基于实际 Preview、Emulator 或真机截图；当前截图若未复现黄色色块，也必须报告“当前 Preview 未复现黄色色块”并列出仍需检查的页面。

### qwen-vision 截图闭环

1. 有 `reference` 图时，调用 `inspect_screenshot`。
2. 有 `actual` 图时，调用 `inspect_screenshot`。
3. 同时有 `reference` 和 `actual` 时，调用 `compare_screenshots`。
4. `qwen-vision` 只识图，不改文件。
5. 主 Agent 根据视觉差异修改代码、构建、测试、总结。
6. 没有截图就不得声称完成视觉验收。

### qwen-vision 有效截图标准

只有以下图片可以作为有效 `actual screenshot`：

- App 实际渲染画面。
- Android emulator 或真机的纯设备截图。
- HarmonyOS Preview、真机或模拟器画面。
- Windows app 真实窗口截图。

以下图片不得作为有效视觉验收：

- 未裁剪的 macOS 或 Windows 全桌面截图。
- 只显示 IDE、桌面、启动器、权限弹窗或无关应用的截图。
- 截错 Android device serial、截错项目、截错窗口的图片。
- 无法明确定位 App、Preview 或窗口区域的模糊截图。

如果只能获得完整桌面或 IDE 截图，必须先裁剪出明确的 App、Preview 或窗口区域，或者在报告中写明 `QWEN_VALID_VISUAL_EVIDENCE=NO`。`qwen-vision` 看过无效截图只说明工具被调用，不代表完成视觉验收。

每轮报告必须区分：

```text
QWEN_CALLED:
QWEN_VALID_VISUAL_EVIDENCE:
QWEN_COMPARE_SCREENSHOTS_COMPLETED:
```

字段语义：

- `QWEN_CALLED=YES` 只表示 Claude Code 实际调用过 `qwen-vision`。
- `QWEN_VALID_VISUAL_EVIDENCE=YES` 只在输入图片符合有效截图标准时成立。
- `QWEN_COMPARE_SCREENSHOTS_COMPLETED=YES` 只在 reference 与 actual 都有效且已调用 `compare_screenshots` 时成立。

### 当前环境事实不得误报

当用户明确确认 Android emulator 已显示目标页面时，不得再报告“没有 Android emulator”，除非 `adb devices` 或截图命令实际失败并给出具体失败原因。

当用户明确确认 DevEco Preview 已显示目标页面时，不得再报告“没有 HarmonyOS Preview”，除非 `screencapture`、Preview 导出或设备截图实际失败并给出具体失败原因。

## 视觉对比标准提示词

当 Claude Code 已有参考图和实际渲染图时，Codex 给 Claude 的 prompt 中可使用：

```text
请调用 qwen-vision 的 compare_screenshots 工具进行视觉验收。

参考图路径：
/绝对路径/到/reference.png

实际渲染图路径：
/绝对路径/到/actual.png

目标：
比较实际页面与参考图在布局、间距、颜色、字体、圆角、阴影、组件位置、视觉层级上的差异。

要求：
1. qwen-vision 只输出视觉差异和修正建议。
2. 不要让 qwen-vision 直接修改文件。
3. 你作为主 Agent 需要根据差异判断哪些问题优先修。
4. 如果两图差异明显，按严重程度排序。
5. 如果两图几乎一致，应说明匹配度较高。
6. 不要把源码、密钥或配置文件传给 qwen-vision。
```

## 控件识别标准提示词

当 Claude Code 需要识别按钮、文本、输入框、列表、图标时，Codex 给 Claude 的 prompt 中可使用：

```text
请调用 qwen-vision 的 extract_text_and_controls 工具分析这张截图：

图片路径：
/绝对路径/到/图片.png

目标：
提取截图中的文字、按钮、输入框、列表项、图标、导航元素和可交互控件。

要求：
1. qwen-vision 只负责识别图中可见元素。
2. 不要修改文件。
3. 不要读取或传递敏感信息。
4. 工具返回后，由主 Agent 判断这些控件如何映射到当前项目 UI 实现。
```

## Outposts 批处理使用规则

当用户的批处理目标包含以下关键词时，Codex 应在给 Claude Code 的正式任务 prompt 中加入 `qwen-vision` 使用提醒：

- UI 复刻
- Apple UI parity
- 视觉验收
- 截图对比
- 界面布局
- 颜色问题
- 字体/间距/圆角/阴影
- 页面结构对齐
- 组件位置对齐
- 设计稿
- 真机截图
- 模拟器截图

如果任务只是以下类型，则不要要求调用 `qwen-vision`：

- 修编译
- 修单元测试
- 改数据模型
- 写文档
- 调整构建脚本
- 修后端逻辑
- 修算法

## 正式任务 prompt 简短块

在 UI 视觉任务中，Codex 可把下面这段插入给 Claude Code 的正式 prompt：

```text
本轮涉及 UI / 视觉对齐任务。你已连接 qwen-vision MCP 工具。

使用规则：
1. 涉及截图理解、参考图分析、实际渲染图对比时，请调用 qwen-vision。
2. qwen-vision 只负责识图和视觉差异分析。
3. 代码修改、构建、测试、总结由你作为主 Agent 完成。
4. 禁止把 API Key、.env、token、密钥、证书、完整源码或私密配置传给 qwen-vision。
5. 禁止让 qwen-vision 直接修改文件。
6. 如果可以生成实际页面截图，请优先使用 compare_screenshots 做视觉验收。
7. 默认最多进行 2 轮视觉验收，避免无限微调。
8. 最终报告中必须写明 qwen-vision 是否被调用、输入截图路径、主要视觉差异和剩余差异。
```

## 报告字段扩展

当批次目标涉及 UI 复刻时，每轮 Claude Code 最终报告必须额外包含：

```text
QWEN_VISION_USED:
REFERENCE_SCREENSHOTS:
ACTUAL_SCREENSHOTS:
VISION_COMPARISON_RESULT:
MAJOR_VISUAL_DIFFERENCES:
FIXES_FROM_VISUAL_REVIEW:
REMAINING_VISUAL_DIFFERENCES:
VISUAL_VALIDATION_LIMITATIONS:
```

当批次目标涉及截图闭环或视觉验收环境时，每轮 Claude Code 最终报告还必须包含：

```text
VISUAL_ENVIRONMENT:
- ANDROID_STUDIO_RUNNING:
- ANDROID_EMULATOR_STATUS:
- ANDROID_DEVICE_SERIALS:
- DEVECO_RUNNING:
- HARMONYOS_PREVIEW_OR_DEVICE_STATUS:
- WINDOWS_UI_ENV_STATUS:

SCREENSHOT_EVIDENCE:
- REFERENCE_SCREENSHOT_DIR:
- ACTUAL_SCREENSHOT_DIR:
- QWEN_OUTPUT_DIR:
- SCREENSHOTS_GENERATED:
- SCREENSHOTS_MISSING_REASON:

QWEN_VISION_RESULT:
- QWEN_VISION_AVAILABLE:
- QWEN_VISION_USED:
- QWEN_CALLED:
- QWEN_VALID_VISUAL_EVIDENCE:
- QWEN_COMPARE_SCREENSHOTS_COMPLETED:
- INSPECT_SCREENSHOT_CALLED:
- COMPARE_SCREENSHOTS_CALLED:
- MAJOR_VISUAL_DIFFERENCES:
- REMAINING_VISUAL_BLOCKERS:
```

Codex 主管摘要中也应简要保留：

- 是否使用 `qwen-vision`。
- 是否有参考图。
- 是否有实际渲染图。
- 主要视觉差异。
- 是否已根据视觉差异修正。
- 剩余视觉验收阻塞。

## Spark + Qwen 任务补充字段（供规则一致性复用）

- `MODE=SPARK_QWEN`
- `QWEN_MODEL`
- `QWEN_AVAILABLE`
- `QWEN_CALL_METHOD`
- `REFERENCE_SCREENSHOTS`
- `ACTUAL_SCREENSHOTS`
- `QWEN_INSPECT_REFERENCE_RESULT`
- `QWEN_INSPECT_ACTUAL_RESULT`
- `QWEN_COMPARE_RESULT`
- `CODE_CHANGES_FROM_QWEN`

## 禁止事项

- 不得把 `qwen-vision` 当主 Agent。
- 不得让 `qwen-vision` 修改文件。
- 不得把源码、密钥、token、`.env`、证书或私密配置传给 `qwen-vision`。
- 不得在没有截图的情况下假装完成视觉验收。
- 不得把无效桌面截图、IDE 截图或错误项目截图报告为有效视觉验收。
- 不得无限循环视觉微调。
- 不得用 `qwen-vision` 替代构建和测试。
- 不得因为 `qwen-vision` 说“相似”就跳过用户人工验收。
- 用户人工视觉反馈优先级高于 `qwen-vision` 的匹配判断。

## Reference-first 视觉流程

UI 视觉批次必须先读取 reference screenshot，再谈 actual screenshot。合法路径有两种：

1. 理想路径：读取 reference screenshot，获取目标项目 actual screenshot，调用 qwen inspect/compare，再由 Claude Code 主 Agent 修改、构建、测试、总结。
2. 退化路径：如果模拟器、Preview、真机或窗口截图暂不可得，仍必须先调用 `qwen-vision.inspect_screenshot` 理解 reference screenshot，再结合 Apple 源项目只读信息和目标项目实现进行修正。

`REFERENCE_ONLY` 不是失败，也不是终止态。报告必须写明：

```text
QWEN_CALLED=YES
QWEN_VALID_VISUAL_EVIDENCE=REFERENCE_ONLY
QWEN_COMPARE_SCREENSHOTS_COMPLETED=NO
ACTUAL_SCREENSHOT_BLOCKER=<具体原因>
```

## 参考图目录映射

- Kikaria-Android、Kikaria-HarmonyOS：`/Users/vita/Vitemis/Outposts/Kikaria-Ref`
- Rokurics-Android、Rokurics-HarmonyOS：`/Users/vita/Vitemis/Outposts/Rokurics-iOS-Ref`
- Rokurics-Windows：`/Users/vita/Vitemis/Outposts/Rokurics-macOS-Ref`

参考图目录只读。不得修改、删除、重命名或重新压缩参考图。Claude Code 的 `.claude/settings.local.json` 应仅授予对应参考图目录的只读 `Read(.../**)` 权限，并保留 `qwen-vision` 三个工具 allow：

```text
mcp__qwen-vision__inspect_screenshot
mcp__qwen-vision__compare_screenshots
mcp__qwen-vision__extract_text_and_controls
```

`disabledMcpjsonServers` 中不得包含 `qwen-vision`。不得使用 `bypassPermissions`，不得扩大到其他 MCP server。

## 视觉证据目录

所有视觉证据统一写入：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT_NAME>/
```

每项目固定子目录：

```text
reference/
actual/
qwen/
```

不得把截图散落到子项目源码目录，不得写入 Apple 源项目，不得删除旧 visual evidence。需要重新截图时创建新的 `RUN_ID`。

## 有效 actual screenshot 标准

有效 actual screenshot 只能是 App 实际渲染画面、Android emulator/真机纯设备截图、HarmonyOS Preview/真机/模拟器画面、Windows app 真实窗口截图。

未裁剪的全桌面截图、只显示 IDE 或桌面的截图、截错设备 serial、截错项目、截错窗口，都不能算有效视觉证据。`qwen-vision` 看过无效截图只代表 `QWEN_CALLED=YES`，不得报告有效验收。

## 必填视觉报告字段

```text
QWEN_CALLED:
QWEN_VALID_VISUAL_EVIDENCE:
QWEN_COMPARE_SCREENSHOTS_COMPLETED:
REFERENCE_SCREENSHOTS_USED:
ACTUAL_SCREENSHOTS:
VISION_TOOLS_CALLED:
VISION_RESULT_SUMMARY:
ACTUAL_SCREENSHOT_BLOCKER:
```

## 项目视觉重点

- Kikaria-Android：首页、背诵页 / ReviewScreen；多轮无改善时允许 UI shell / page layout / navigation 整体重构。
- Kikaria-HarmonyOS：构建未恢复前不堆 UI；恢复后再做有效 Preview/设备截图。
- Rokurics-Android：禁止文字描述自创 UI；使用 `Rokurics-iOS-Ref`；暗色、玻璃、高级质感；qwen + adb actual screenshot 路径已证明可行。
- Rokurics-HarmonyOS：重点检查黄色/异常色块、背景污染、组件层级；无效桌面截图不算。
- Rokurics-Windows：使用 `Rokurics-macOS-Ref`；actual Windows screenshot 需要 Win11 ARM + VS2022 环境，缺环境时报告 `WINDOWS_HOST_VALIDATION_PENDING`。
