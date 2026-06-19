# Supervisor Worker Visual Protocol

本文定义 Agent / ExAgent 模式中的视觉 worker 调度规则。

Agent / ExAgent 中，视觉任务由 QwenCode one-shot 执行。QwenCode 已配置 Qwen3.7-Plus 作为专用视觉模型。

重要边界：QwenCode 不是 DeepCode 的 helper。DeepCode 不能调用 QwenCode，QwenCode 也不能调用 DeepCode。所有 QwenCode 请求都必须由 supervisor 发起。

## 何时启用

当任务包含以下目标时启用 QwenCode：

- UI 复刻。
- Apple UI parity。
- 视觉验收。
- 截图对比。
- 界面布局。
- 颜色、字体、间距、圆角、阴影、组件位置。
- 设计稿。
- 真机截图或模拟器截图。
- reference / actual 对比。

如果任务只是修编译、修单元测试、改数据模型、写文档、调整构建脚本、修后端逻辑或修算法，不要求启用 QwenCode。

## 通用原则

1. QwenCode 只处理截图路径和视觉目标，不处理源码。
2. QwenCode 不直接修改文件。
3. DeepCode 负责代码修改、构建、测试和实现报告。
4. Supervisor 负责发起 QwenCode 请求，并把 QwenCode 报告路径传给下一轮 DeepCode。
5. DeepCode 只读取 supervisor 指定的 QwenCode 报告文件；不得自行调用 QwenCode。
6. 不得把 API Key、`.env`、token、密钥、证书、完整源码或私密配置传给 QwenCode。
7. 没有截图就不得声称完成视觉验收。
8. 默认视觉验收最多重复 2 轮，避免无限微调。
9. 用户人工视觉反馈优先级高于 QwenCode 的相似判断。

## Reference-first 流程

UI 视觉批次必须先读取 reference screenshot，再谈 actual screenshot。

### Step 1: Supervisor 调 QwenCode reference inspect

```text
Supervisor -> QwenCode_ONE_SHOT
INPUT:
  VISUAL_TASK_TYPE=REFERENCE_INSPECT
  REFERENCE_SCREENSHOT=<absolute path>
  ACTUAL_SCREENSHOT=NONE
OUTPUT:
  <PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/round-<N>-reference-inspect.md
```

QwenCode 输出 reference 页面结构、文字、颜色、布局、组件、视觉风格和不确定项。

### Step 2: Supervisor 调 DeepCode implementation

```text
Supervisor -> DeepCode_ONE_SHOT
INPUT:
  TASK_OBJECTIVE=<implementation/fix objective>
  SUPERVISOR_PROVIDED_QWENCODE_REPORTS=<reference inspect report path>
OUTPUT:
  <PROJECT_PATH>/DeepCode-output/<BATCH_NAME>/round-<N>-implement.md
```

DeepCode 根据 supervisor 提供的 QwenCode reference 报告、Apple 只读参考和目标项目实现进行修改，运行构建/测试，并尽可能生成 actual screenshot 路径。

### Step 3: Supervisor 调 QwenCode actual inspect / compare

如果 DeepCode 报告给出有效 actual screenshot 路径：

```text
Supervisor -> QwenCode_ONE_SHOT
INPUT:
  VISUAL_TASK_TYPE=COMPARE
  REFERENCE_SCREENSHOT=<absolute path>
  ACTUAL_SCREENSHOT=<absolute path from DeepCode-output report>
OUTPUT:
  <PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/round-<N>-actual-compare.md
```

QwenCode 输出实际图有效性、reference/actual 差异、主要视觉问题和修正建议。

### Step 4: Supervisor 调下一轮 DeepCode fix-from-qwen

```text
Supervisor -> DeepCode_ONE_SHOT
INPUT:
  TASK_OBJECTIVE=fix according to supervisor-provided QwenCode visual report
  PREVIOUS_DEEPCODE_REPORTS=<previous DeepCode report path>
  SUPERVISOR_PROVIDED_QWENCODE_REPORTS=<actual compare report path>
OUTPUT:
  <PROJECT_PATH>/DeepCode-output/<BATCH_NAME>/round-<N+1>-fix-from-qwen.md
```

DeepCode 必须读取指定 QwenCode 报告；若读取失败，必须报告失败，不得声称已根据 Qwen 结果修复。

## Worker 隔离

视觉流程中禁止：

```text
DeepCode calls QwenCode
DeepCode calls Qwen helper
QwenCode reads source code
QwenCode reads DeepCode-output
QwenCode modifies files
QwenCode runs build/test
```

合法链路只有：

```text
Supervisor reads DeepCode-output -> Supervisor extracts screenshot path -> Supervisor passes screenshot path to QwenCode
Supervisor reads QwenCode-output -> Supervisor passes report path to DeepCode
```

## QwenCode 接入边界

QwenCode 默认模型：

```text
Qwen3.7-Plus
```

如当前 QwenCode 配置异常或模型无法确认，报告：

```text
QWENCODE_MODEL_MISMATCH
QWENCODE_UNAVAILABLE
```

QwenCode 输入仅限：

- reference screenshot 路径。
- actual screenshot 路径。
- 视觉任务目标。
- 输出文件路径。
- 必要的非源码页面目标说明。

QwenCode 禁止输入：

- 源码。
- API Key。
- `.env`。
- token。
- 密钥、证书、私密配置。
- DeepCode-output 报告。
- 用户无关私人文件。

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

同时，QwenCode 结构化报告必须写入目标项目：

```text
<PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/
```

视觉证据和 QwenCode 报告都不是临时垃圾文件。不得删除 `.outposts-supervisor/visual-evidence`、当前批次截图、QwenCode 输出、DeepCode 输出、state、checkpoint、report 或 batch state。需要重新截图时必须创建新的 `RUN_ID` 目录，不得覆盖、删除或复用旧证据目录。

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
QWENCODE_VALID_VISUAL_EVIDENCE=NO
```

QwenCode 看过无效截图只说明 worker 被调用，不代表完成视觉验收。

## Android 截图规则

如果 emulator 已启动，DeepCode 应优先使用 adb 获取纯设备截图。默认 adb 绝对路径：

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
11. DeepCode 在 `DeepCode-output` 报告中写明 actual screenshot 路径。
12. 释放 `ANDROID_EMULATOR_LOCK`。

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

DeepCode 报告必须包含：

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

如果 DevEco Preview 没有可直接导出的纯预览截图命令，允许 DeepCode 使用 macOS `screencapture` 抓取 DevEco 可见窗口或全屏截图，保存到项目的 `actual/` 目录。

如能进一步裁剪出纯手机预览区域，应保存为 `actual/home-preview-cropped.png`，并由 supervisor 把该路径交给 QwenCode。若只能获得完整 IDE 截图，报告必须明确截图类型为完整 IDE 截图，并要求 QwenCode 聚焦 Preview 区域；此类截图在未裁剪前不得计为有效 actual screenshot。

## Windows 截图规则

Rokurics-Windows 需要 Windows/.NET UI 环境才能做真实 UI 验证。

如果当前 host 无 Windows UI 环境，应报告：

```text
WINDOWS_HOST_VALIDATION_PENDING=YES
```

不得假装完成 Windows 视觉验收。

## Reference-only 退化路径

如果模拟器、Preview、真机或窗口截图暂不可得：

1. Supervisor 仍应先调用 QwenCode 完成 reference inspect。
2. DeepCode 可读取 supervisor 提供的 reference inspect 报告继续修正 UI。
3. 报告必须写明 actual screenshot blocker。
4. 不得声明 compare 完成。
5. 不得声明视觉闭环完成。

报告字段：

```text
QWENCODE_CALLED=YES
QWENCODE_VALID_VISUAL_EVIDENCE=REFERENCE_ONLY
QWENCODE_COMPARE_COMPLETED=NO
ACTUAL_SCREENSHOT_BLOCKER=<具体原因>
```

## 参考图目录映射

```text
Kikaria-Android, Kikaria-HarmonyOS: /Users/vita/Vitemis/Outposts/Kikaria-Ref
Rokurics-Android, Rokurics-HarmonyOS: /Users/vita/Vitemis/Outposts/Rokurics-iOS-Ref
Rokurics-Windows: /Users/vita/Vitemis/Outposts/Rokurics-macOS-Ref
```

参考图目录只读。不得修改、删除、重命名或重新压缩参考图。

## QwenCode 必填报告字段

```text
MODEL_CHECK_RESULT:
PATH_CHECK_RESULT:
WORKER: QwenCode_ONE_SHOT
MODEL: Qwen3.7-Plus
OUTPUT_FILE_WRITTEN:
VISUAL_TASK_TYPE:
REFERENCE_SCREENSHOT_READ:
ACTUAL_SCREENSHOT_READ:
QWENCODE_CALLED:
QWENCODE_VALID_VISUAL_EVIDENCE:
QWENCODE_COMPARE_COMPLETED:
VISION_RESULT_SUMMARY:
MAJOR_VISUAL_DIFFERENCES:
REMAINING_VISUAL_BLOCKERS:
VISUAL_VALIDATION_LIMITATIONS:
REPORT_COMPLETE:
```
