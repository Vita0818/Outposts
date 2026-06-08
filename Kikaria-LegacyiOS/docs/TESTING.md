# TESTING

最后自查日期：2026-05-26

## 环境要求

- macOS + Xcode。
- 可用 iOS Simulator，用于 `Kikaria` iOS App 和 Widget 构建/运行。
- `xcrun simctl` 可列出可用 iPhone Simulator。
- SwiftPM 能解析 `Package.resolved` 中锁定的 SwiftMath 依赖。

## 依赖安装方式

未发现 `Package.swift`、CocoaPods、Carthage、npm、Homebrew 脚本或手写依赖安装脚本。

依赖由 Xcode/SwiftPM 根据：

```text
Kikaria.xcodeproj/project.xcworkspace/xcshareddata/swiftpm/Package.resolved
```

解析。当前远程包：

- `SwiftMath`：`https://github.com/mgriebling/SwiftMath.git`，版本 `1.7.3`。

首次构建或清理 SwiftPM 缓存后，Xcode 可能需要联网解析/下载该依赖。

## 构建命令

推荐主 App 构建：

```sh
scripts/build.sh
```

脚本行为：

- 使用 `Kikaria.xcodeproj`。
- 默认 scheme：`Kikaria`。
- 默认 configuration：`Debug`。
- 自动选择可用 iPhone Simulator，优先 Booted iPhone。
- 默认 DerivedData：`.build/DerivedData`。

可覆盖环境变量：

```sh
CONFIGURATION=Release scripts/build.sh
SIMULATOR_ID=<device-uuid> scripts/build.sh
DESTINATION='platform=iOS Simulator,name=iPhone 16' scripts/build.sh
```

等价直接命令示例：

```sh
xcodebuild -project Kikaria.xcodeproj -scheme Kikaria -configuration Debug -destination 'platform=iOS Simulator,name=iPhone 16' -derivedDataPath .build/DerivedData build
```

Widget 构建命令：

```sh
xcodebuild -project Kikaria.xcodeproj -scheme KikariaWidget -configuration Debug -destination 'platform=iOS Simulator,name=iPhone 16' -derivedDataPath .build/DerivedData build
```

macOS App 构建命令：

```sh
xcodebuild -project Kikaria.xcodeproj -scheme KikariaMac -configuration Debug -destination 'platform=macOS' -derivedDataPath .build/DerivedData build
```

说明：`xcodebuild -list` 能识别 `KikariaMac` scheme，但未在 `xcshareddata/xcschemes/` 中看到对应 scheme 文件；若命令失败，需要在 Xcode 中确认 scheme 来源。

## 单元测试命令

当前只发现 `KikariaMacTests`，内容仍是模板测试。可尝试：

```sh
xcodebuild test -project Kikaria.xcodeproj -scheme KikariaMac -destination 'platform=macOS' -derivedDataPath .build/DerivedData
```

iOS `Kikaria.xcscheme` 的 `TestAction` 没有显式测试 target；是否可运行有效 iOS 单元测试未确认。

## 集成测试命令

未发现独立集成测试目录或脚本。

如需把 App + Widget 作为集成验证，应至少构建：

```sh
scripts/build.sh
xcodebuild -project Kikaria.xcodeproj -scheme KikariaWidget -configuration Debug -destination 'platform=iOS Simulator,name=iPhone 16' -derivedDataPath .build/DerivedData build
```

## UI 测试命令

当前发现 `KikariaMacUITests`，但测试内容是 Xcode 模板。可尝试：

```sh
xcodebuild test -project Kikaria.xcodeproj -scheme KikariaMac -destination 'platform=macOS' -derivedDataPath .build/DerivedData
```

如果需要 iOS UI 测试，当前未发现对应 target。

## 静态检查 / lint / format 命令

未发现 SwiftLint、SwiftFormat 或自定义 lint 脚本。

通用空白检查：

```sh
git diff --check
```

可选 Xcode 分析：

```sh
xcodebuild -project Kikaria.xcodeproj -scheme Kikaria -configuration Debug -destination 'platform=iOS Simulator,name=iPhone 16' -derivedDataPath .build/DerivedData analyze
```

上面的 analyze 命令未在本轮验证。

## 手动验证矩阵

业务或 UI 修改后，按影响范围选择验证：

- iPhone portrait：启动、首次个人资料、首次引导、首页、普通复习。
- iPhone landscape：Review Screen、长答案滚动、手势。
- iPad portrait：首页、列表页、设置页、Review 底部按钮。
- iPad landscape：两列布局、范围选择面板、Review 双列。
- macOS：窗口最小尺寸、侧边栏、快捷键、复用页面、编辑资料。
- Widget small/medium/large：浅色、深色、空数据、长 preset 名、长知识点标题。
- Markdown：导入 `.md` / `.txt`、中文逗号 tags、缺少 hint/content 的错误提示、LaTeX inline/block。
- 数据持久化：切换 preset、重启恢复、每日目标、倒数日期、重点/已掌握状态。
- 通知：权限请求、开关、时间、倒数安全线、关闭后取消通知。

## 常见失败原因

- 没有可用 iPhone Simulator，`scripts/build.sh` 会报错。
- SwiftPM 无法解析或下载 SwiftMath。
- Xcode signing、App Group、bundle id 或 entitlements 被误改。
- `Presets/` 中 Markdown 格式无有效知识点，导致内置 preset 初始化失败或为空。
- Widget App Group 读取失败时会 fallback 到 standard UserDefaults 或 placeholder。
- 本地通知被系统权限拒绝。
- `ContentView.swift` 局部改动破坏 `onChange` 保存或 Review queue 状态。

## 本轮是否实际运行命令

本轮实际运行了只读检查命令，包括：

- `pwd`
- `git rev-parse --show-toplevel`
- `git status --short`
- `rg --files`
- `find`
- `sed`
- `plutil -p`
- `xcodebuild -list -project Kikaria.xcodeproj`

本轮未运行构建或测试。用户要求本轮是文档生成任务，并明确“不为了本任务运行完整构建或测试”。
