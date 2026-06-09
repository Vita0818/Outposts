# DeepCode CLI Visual Qwen Protocol

本文定义 Agent 模式与 Spark 模式中的 Qwen 视觉辅助规则。

Qwen 视觉辅助不是第四种模式。当前模式仍只能是：

```text
MODE=AGENT
```

或：

```text
MODE=SPARK
```

视觉辅助用字段表示：

```text
QWEN_VISUAL_ASSIST=YES
```

OpenCode 模式不读取本文。

## 定位

Qwen3.7Plus 或既有 qwen helper 只负责：

- 看图。
- 识别截图。
- 提取 UI 文本与控件。
- 比较 reference 与 actual 截图。
- 输出视觉差异与修正建议。

Qwen 不负责：

- 修改文件。
- 运行命令。
- 读源码。
- 接收 API Key、token、`.env`、证书、私钥、完整源码或私密配置。
- 替代构建、测试或用户人工验收。

Agent 模式下，DeepCode CLI 是主执行器。Spark 模式下，GPT-5.3-Codex-Spark 是主执行器。

## 何时启用

任务包含以下任一内容时启用：

- UI 复刻。
- Apple UI parity。
- 截图对比。
- 视觉验收。
- 页面布局、颜色、字体、间距、圆角、阴影、组件位置。
- reference / actual。
- 真机截图、模拟器截图、Preview 截图、设计稿。

纯代码、后端、算法、数据模型、构建脚本、单元测试任务不要调用 Qwen。

## Agent 模式视觉流程

Agent 模式中，Codex 不直接调用 Qwen。Codex 只在 DeepCode CLI 正式任务 prompt 中要求 DeepCode 使用可用的 Qwen helper。

固定流程：

1. DeepCode 确认当前任务需要视觉辅助。
2. DeepCode 定位 reference screenshot。
3. DeepCode 生成或定位 actual screenshot。
4. DeepCode 调用 Qwen helper 分析 reference。
5. 如 actual 可用，DeepCode 调用 Qwen helper 分析 actual。
6. reference 与 actual 均有效时，DeepCode 调用 Qwen helper 进行 compare。
7. DeepCode 根据 Qwen 输出修改代码。
8. DeepCode 运行构建、测试或平台验证。
9. DeepCode 输出结构化报告。

若 DeepCode CLI 没有可用 Qwen helper：

- 报告 `QWEN_UNAVAILABLE_IN_SESSION` 或 `QWEN_HELPER_NOT_CONFIGURED`。
- 不得声称完成视觉验收。
- 若任务可在 reference-only 基础上推进，报告 `REFERENCE_ONLY` 并继续可安全的代码修正。

## Spark 模式视觉流程

Spark 模式中，Spark 可直接使用既有 Qwen helper 或读取其报告。

固定流程：

1. Spark 确认模型为 `GPT-5.3-Codex-Spark`。
2. Spark 读取任务与相关代码。
3. Spark 生成或定位 reference screenshot。
4. Spark 生成或定位 actual screenshot。
5. 调用 Qwen3.7Plus 或既有 qwen helper 分析图片。
6. Spark 根据 Qwen 输出拆解视觉差异并修改代码。
7. Spark 运行构建/测试并按需重拍截图。
8. 以新截图复跑 compare，直到达到停止条件或视觉轮次上限。

若 actual 缺失：

- 可先做 reference inspect。
- 报告 `REFERENCE_ONLY`。
- 不得宣称视觉闭环完成。

## Qwen helper 接入边界

如需通过外部 API 或 helper 调用 Qwen：

- 只允许复用当前环境已存在的 helper / MCP / API wrapper。
- 不临时硬编码 API Key。
- API Key 只允许从环境变量读取，例如 `DASHSCOPE_API_KEY`、`QWEN_API_KEY`。
- 不得把 Key 写入仓库文档、源码、`.env`、`.codex/config.toml`、OpenCode 配置或报告。
- 图像输入仅限截图路径。
- 输出报告写入 visual-evidence 的 `qwen/` 目录。

若网络不可用且无可复用 helper：

```text
QWEN_AVAILABLE=NO
QWEN_HELPER_NETWORK_NOT_AVAILABLE=YES
```

视觉验收任务不得继续声称闭环完成。

## 视觉证据目录

所有视觉证据统一写入：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT_NAME>/
```

每个项目固定子目录：

```text
reference/
actual/
qwen/
```

不得把截图散落到子项目源码目录。不得写入 Apple 源项目。不得删除旧 visual evidence。需要重新截图时创建新的 `RUN_ID` 或唯一文件名。

## 有效 actual screenshot 标准

有效 actual screenshot 只能是：

- App 实际渲染画面。
- Android emulator 或真机的纯设备截图。
- HarmonyOS Preview、真机或模拟器画面。
- Windows app 真实窗口截图。

以下不得作为有效视觉验收：

- 未裁剪全桌面截图。
- 只显示 IDE、桌面、启动器、权限弹窗或无关应用的截图。
- 截错 Android device serial。
- 截错项目、截错窗口。
- 无法明确定位 App、Preview 或窗口区域的模糊截图。

Qwen 看过无效截图只说明工具被调用，不代表完成视觉验收。

报告必须区分：

```text
QWEN_CALLED
QWEN_VALID_VISUAL_EVIDENCE
QWEN_COMPARE_SCREENSHOTS_COMPLETED
```

字段语义：

- `QWEN_CALLED=YES`：实际调用过 Qwen。
- `QWEN_VALID_VISUAL_EVIDENCE=YES`：输入图片符合有效截图标准。
- `QWEN_VALID_VISUAL_EVIDENCE=REFERENCE_ONLY`：只有 reference 有效，无 actual compare。
- `QWEN_VALID_VISUAL_EVIDENCE=NO`：输入不是有效视觉证据。
- `QWEN_COMPARE_SCREENSHOTS_COMPLETED=YES`：reference 与 actual 均有效且已完成 compare。

## Android 截图规则

Android actual screenshot 优先使用：

```text
/Users/vita/Library/Android/sdk/platform-tools/adb
```

固定流程：

1. 获取 `ANDROID_EMULATOR_LOCK`。
2. 确认 adb 绝对路径可用。
3. 执行 `adb devices -l`。
4. 从当前 Android 项目配置读取 `applicationId`。
5. 检查目标 App 是否安装。
6. 未安装时执行当前项目内最小安装流程。
7. adb 启动目标 App。
8. 校验前台包名等于 `applicationId`。
9. 前台正确后执行 `screencap`。
10. 保存到 visual-evidence 的 `actual/`，文件名唯一。
11. 释放 `ANDROID_EMULATOR_LOCK`。

不得猜包名或 Activity。不得把另一个项目的 App 截图当作当前项目截图。

## HarmonyOS 截图规则

HarmonyOS 视觉验收需要 DevEco Preview、Emulator 或真机截图。

如果只能用 macOS `screencapture` 抓取 DevEco 可见窗口：

- 应尽量裁剪出 Preview 区域。
- 若只能获得完整 IDE 截图，必须报告截图类型为完整 IDE 截图。
- 完整 IDE 截图不自动算有效 actual，除非能明确聚焦 Preview 区域并裁剪。

不得因截图困难而清理 `~/.hvigor` 或用户级 SDK 缓存。

## Windows 截图规则

Rokurics-Windows 真实 UI 验证需要 Windows/.NET UI 环境。macOS host 无法完成真实 WinUI 窗口验收时，报告：

```text
WINDOWS_HOST_VALIDATION_PENDING=YES
```

不得假装 build/launch 或视觉验收通过。

## Reference-first 退化路径

UI 视觉批次必须先读取 reference screenshot。

合法路径：

1. 理想路径：reference inspect + actual inspect + compare + 修正 + 验证。
2. 退化路径：actual 暂不可得时，reference inspect + Apple 源项目只读信息 + 目标项目实现修正。

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

参考图目录只读。不得修改、删除、重命名或重新压缩参考图。

## 报告字段

视觉任务报告必须包含：

```text
QWEN_VISUAL_ASSIST
QWEN_MODEL
QWEN_AVAILABLE
QWEN_CALL_METHOD
QWEN_CALLED
QWEN_VALID_VISUAL_EVIDENCE
QWEN_COMPARE_SCREENSHOTS_COMPLETED
REFERENCE_SCREENSHOTS_USED
ACTUAL_SCREENSHOTS
VISION_TOOLS_CALLED
VISION_RESULT_SUMMARY
CODE_CHANGES_FROM_QWEN
ACTUAL_SCREENSHOT_BLOCKER
REMAINING_VISUAL_DIFFERENCES
VISUAL_VALIDATION_LIMITATIONS
```

## 禁止事项

- 不得把 Qwen 当主 Agent。
- 不得让 Qwen 修改文件。
- 不得把源码、密钥、token、`.env`、证书或私密配置传给 Qwen。
- 不得在没有截图的情况下假装完成视觉验收。
- 不得把无效桌面截图、IDE 截图或错误项目截图报告为有效视觉验收。
- 不得无限循环视觉微调；默认最多 2 轮。
- 不得用 Qwen 替代构建和测试。
- 不得因为 Qwen 判断“相似”就跳过用户人工验收。
- 用户人工视觉反馈优先级高于 Qwen 匹配判断。
