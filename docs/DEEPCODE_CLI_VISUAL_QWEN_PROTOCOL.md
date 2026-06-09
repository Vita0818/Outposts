# DeepCode CLI Visual Qwen Protocol

本文定义 Agent、ExAgent 与 Spark 模式中的 Qwen 视觉辅助规则。

Qwen helper 不是主执行器。主执行器分别是：

```text
Agent:   DeepCode CLI
ExAgent: DeepCode CLI
Spark:   GPT-5.3-Codex-Spark
```

Qwen helper 只负责看图、识别截图、比较图片。它不得直接修改文件，不得接收 API Key、`.env`、token、密钥、证书、完整源码或私密配置。

OpenCode 独立模式不读取本文。

## 何时启用

当任务包含以下目标时启用：

- UI 复刻。
- Apple UI parity。
- 视觉验收。
- 截图对比。
- 界面布局。
- 颜色、字体、间距、圆角、阴影、组件位置。
- 设计稿。
- 真机截图或模拟器截图。
- reference / actual 对比。

如果任务只是修编译、修单元测试、改数据模型、写文档、调整构建脚本、修后端逻辑或修算法，不要求启用 Qwen。

## 通用原则

1. Qwen 只处理图片，不处理源码。
2. Qwen 不直接修改文件。
3. 主执行器负责代码修改、构建、测试和总结。
4. 不得把 API Key、`.env`、token、密钥、证书、完整源码或私密配置传给 Qwen。
5. 没有截图就不得声称完成视觉验收。
6. 默认视觉验收最多重复 2 轮，避免无限微调。
7. 用户人工视觉反馈优先级高于 Qwen 的相似判断。

## Agent / ExAgent 视觉流程

Agent 或 ExAgent 中，Supervisor 不直接调用 Qwen。Supervisor 只在 DeepCode CLI 正式任务 prompt 中要求 DeepCode 使用可用的 Qwen helper。

流程：

1. DeepCode 确认当前任务需要视觉辅助。
2. DeepCode 定位 reference screenshot。
3. DeepCode 生成或定位 actual screenshot。
4. DeepCode 调用或读取 Qwen helper 分析 reference。
5. 如 actual 可用，DeepCode 调用或读取 Qwen helper 分析 actual。
6. reference 与 actual 均有效时，DeepCode 调用或读取 Qwen helper 进行 compare。
7. DeepCode 根据 Qwen 输出修改代码。
8. DeepCode 运行构建、测试或平台验证。
9. DeepCode 输出结构化报告。

若 DeepCode CLI 没有可用 Qwen helper：

- 记录 `QWEN_AVAILABLE=NO`。
- 视觉任务进入 `QWEN_UNAVAILABLE_IN_SESSION` 或 `REFERENCE_ONLY`，取决于是否仍有 reference 可供非视觉推断。
- 不得宣称完成像素级视觉闭环。

## Spark 视觉流程

Spark 模式中，Spark 可直接使用既有 Qwen helper 或读取其报告。

流程：

1. Spark 确认模型为 `GPT-5.3-Codex-Spark`。
2. Spark 读取任务与相关代码。
3. Spark 生成或定位 reference screenshot。
4. Spark 生成或定位 actual screenshot。
5. Spark 使用 Qwen helper 分析 reference / actual，并在两者都有效时 compare。
6. Spark 根据 Qwen 输出拆解视觉差异并修改代码。
7. Spark 运行构建/测试并按需重拍截图。
8. Spark 输出结构化报告。

若 actual 缺失：

- 只能走 `REFERENCE_ONLY`。
- 不得声称 compare 完成。
- 不得声称视觉闭环完成。

## Qwen 接入边界

若当前环境已有 Qwen helper / MCP / API wrapper，优先复用，不要临时接新链路。

若需要外部 API：

- API Key 只允许从环境变量读取，例如 `DASHSCOPE_API_KEY` 或 `QWEN_API_KEY`。
- 图像输入仅限截图路径。
- 不得把 Key 写入仓库文档、源码、`.env`、配置文件或报告。
- 网络不可用或 helper 不可用时，报告 `QWEN_HELPER_NETWORK_NOT_AVAILABLE` 或 `QWEN_UNAVAILABLE_IN_SESSION`。

建议视觉输出目录：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT>/qwen/
```

## 视觉证据目录

所有 supervisor 视觉证据统一写入：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT_NAME>/
```

每个项目固定子目录：

```text
reference/
actual/
qwen/
```

- `reference/` 存参考截图。
- `actual/` 存目标端实际渲染截图。
- `qwen/` 存 Qwen 识别或对比摘要。

不得把截图散落到子项目源码目录。不得把截图写进 Apple 源项目。不得清理子项目 build/cache。

不得删除 `.outposts-supervisor/visual-evidence`、当前批次截图、Qwen 输出、state、checkpoint、report 或 batch state。需要重新截图时必须创建新的 `RUN_ID` 目录，不得覆盖、删除或复用旧证据目录。

## 有效 actual screenshot 标准

有效 actual screenshot 只能是：

- App 实际渲染画面。
- Android emulator 或真机的纯设备截图。
- HarmonyOS Preview、真机或模拟器画面。
- Windows app 真实窗口截图。

以下不得作为有效视觉验收：

- 未裁剪的全桌面截图。
- 只显示 IDE、桌面、启动器、权限弹窗或无关应用的截图。
- 截错 Android device serial、截错项目、截错窗口的图片。
- 无法明确定位 App、Preview 或窗口区域的模糊截图。

如果只能获得完整桌面或 IDE 截图，必须先裁剪出明确的 App、Preview 或窗口区域，或者报告：

```text
QWEN_VALID_VISUAL_EVIDENCE=NO
```

Qwen 看过无效截图只说明工具被调用，不代表完成视觉验收。

## Android 截图规则

如果 emulator 已启动，优先使用 adb 获取纯设备截图。默认 adb 绝对路径：

```text
/Users/vita/Library/Android/sdk/platform-tools/adb
```

Android screenshot preflight 必须按以下顺序执行：

1. 获取 `ANDROID_EMULATOR_LOCK`。
2. 确认 adb 绝对路径可用。
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

截图命令：

```text
/Users/vita/Library/Android/sdk/platform-tools/adb -s <DEVICE_SERIAL> exec-out screencap -p > <ACTUAL_SCREENSHOT_PATH>
```

如果有多个 emulator，必须显式指定 `-s <DEVICE_SERIAL>`。

如果前台包名不是目标 `applicationId`：

1. 不得截图。
2. 自动重新启动目标 App。
3. 再次校验前台包名。
4. 最多重试 2 次。
5. 仍失败才报告 `ANDROID_FOREGROUND_PACKAGE_MISMATCH`。

如果安装失败，报告 `INSTALL_FOR_SCREENSHOT_FAILED`，停止该项目截图链，不得继续 UI 修改。

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

## HarmonyOS 截图规则

HarmonyOS 视觉验收需要 DevEco Preview、Emulator 或真机截图。

如果 DevEco Preview 没有可直接导出的纯预览截图命令，允许先使用 macOS `screencapture` 抓取 DevEco 可见窗口或全屏截图，保存到项目的 `actual/` 目录。

如能进一步裁剪出纯手机预览区域，应保存为 `actual/home-preview-cropped.png`，并优先交给 Qwen。若只能获得完整 IDE 截图，报告必须明确截图类型为完整 IDE 截图，并要求 Qwen 聚焦 Preview 区域；此类截图在未裁剪前不得计为有效 actual screenshot。

## Windows 截图规则

Rokurics-Windows 需要 Windows/.NET UI 环境才能做真实 UI 验证。

如果当前 host 无 Windows UI 环境，应报告：

```text
WINDOWS_HOST_VALIDATION_PENDING=YES
```

不得假装完成 Windows 视觉验收。

## Reference-first 流程

UI 视觉批次必须先读取 reference screenshot，再谈 actual screenshot。

合法路径：

1. 理想路径：读取 reference screenshot，获取目标项目 actual screenshot，调用 Qwen inspect/compare，再由主执行器修改、构建、测试、总结。
2. 退化路径：如果模拟器、Preview、真机或窗口截图暂不可得，仍必须先调用或读取 Qwen reference inspect，再结合 Apple 源项目只读信息和目标项目当前实现修正 UI。

`REFERENCE_ONLY` 不是失败，也不是终止态。报告必须写明：

```text
QWEN_CALLED=YES
QWEN_VALID_VISUAL_EVIDENCE=REFERENCE_ONLY
QWEN_COMPARE_SCREENSHOTS_COMPLETED=NO
ACTUAL_SCREENSHOT_BLOCKER=<具体原因>
```

## 参考图目录映射

```text
Kikaria-Android, Kikaria-HarmonyOS: /Users/vita/Vitemis/Outposts/Kikaria-Ref
Rokurics-Android, Rokurics-HarmonyOS: /Users/vita/Vitemis/Outposts/Rokurics-iOS-Ref
Rokurics-Windows: /Users/vita/Vitemis/Outposts/Rokurics-macOS-Ref
```

参考图目录只读。不得修改、删除、重命名或重新压缩参考图。

## 必填视觉报告字段

```text
QWEN_REQUIRED:
QWEN_CALLED:
QWEN_VALID_VISUAL_EVIDENCE:
QWEN_COMPARE_SCREENSHOTS_COMPLETED:
REFERENCE_SCREENSHOTS_USED:
ACTUAL_SCREENSHOTS:
VISION_TOOLS_CALLED:
VISION_RESULT_SUMMARY:
ACTUAL_SCREENSHOT_BLOCKER:
VISUAL_VALIDATION_LIMITATIONS:
```

`QWEN_CALLED=YES` 不等于有效验收。无效桌面截图必须报告 `QWEN_VALID_VISUAL_EVIDENCE=NO`。reference-only 必须明确写 `QWEN_VALID_VISUAL_EVIDENCE=REFERENCE_ONLY`。
