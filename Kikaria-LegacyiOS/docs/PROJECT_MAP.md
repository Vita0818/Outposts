# PROJECT_MAP

最后自查日期：2026-05-26

本文件根据当前源码、Xcode 工程配置、脚本和测试文件整理。扫描时排除了 `.git/`、`.build/`、`build/`、`DerivedData/`、`xcuserdata/` 等依赖缓存、构建产物或用户本地状态。

## 顶层目录树

```text
.
├── AGENTS.md
├── CODEX_CONTEXT.md
├── README.md
├── SPEC.md
├── Kikaria/
├── Kikaria.xcodeproj/
├── KikariaMac/
├── KikariaMacTests/
├── KikariaMacUITests/
├── KikariaWidget/
├── Presets/
├── docs/
├── scripts/
├── KikariaNewIcon.png
└── .gitignore
```

忽略或不应手工维护的目录：

- `.git/`：Git 内部数据。
- `.build/`：`scripts/build.sh` 默认 DerivedData 输出。
- `build/`、`DerivedData/`：Xcode 构建产物。
- `Kikaria.xcodeproj/**/xcuserdata/`：用户本地 Xcode 状态。

## 关键目录职责

- `Kikaria/`：主 App 共享源码目录。包含 iOS App 入口、跨 iOS/macOS 复用的 SwiftUI 主界面、数据模型、Markdown 解析、学习记录、数学公式渲染、适配布局、字体工具、资源和 entitlements。
- `KikariaMac/`：macOS App target 的薄包装。`KikariaMacRootView` 复用 `ContentView()`，并配置桌面窗口 chrome、最小尺寸和默认窗口大小。
- `KikariaWidget/`：Widget Extension target。读取 App 写入的 `WidgetSnapshot`，渲染 small、medium、large 三种小组件。
- `KikariaMacTests/`：Swift Testing 单元测试 target，目前只有模板测试。
- `KikariaMacUITests/`：XCTest UI 测试 target，目前包含启动和启动性能模板。
- `Presets/`：内置 Markdown 知识点资源。工程以 folder reference 方式把整个目录打入主 App 和 Mac App 资源。
- `scripts/`：项目脚本，目前只有 `scripts/build.sh`，用于构建 `Kikaria` scheme。
- `docs/`：本轮新增的 Codex 常驻项目上下文文档。
- `Kikaria.xcodeproj/`：Xcode 工程、targets、schemes、SwiftPM resolved package、signing、资源和 target 关系。

## 关键文件清单

- `Kikaria/KikariaApp.swift`：iOS App `@main` 入口；设置 `UNUserNotificationCenter` delegate 并加载 `ContentView()`。
- `Kikaria/ContentView.swift`：核心 UI、路由、状态、持久化、通知调度、Preset 管理、Review Screen、个人资料、设置页、历史/概览等逻辑。
- `Kikaria/KnowledgePoint.swift`：`KnowledgePoint`、Markdown parse/export、`KnowledgePreset` 和内置 preset 资源加载。
- `Kikaria/StudyTracking.swift`：学习活动记录、Widget snapshot 模型、App 侧 Widget 数据写入。
- `Kikaria/KikariaAdaptiveLayout.swift`：跨 iPhone/iPad/Mac 的尺寸分类、页面宽度、两列布局和缩放指标。
- `Kikaria/KikariaTypography.swift`：中文、英文、数字混排字体入口。
- `Kikaria/KikariaLatexParser.swift`、`Kikaria/LatexToken.swift`、`Kikaria/KikariaMathText.swift`、`Kikaria/KikariaMathFormulaView.swift`：LaTeX token 化、本地 SwiftMath 渲染和 fallback 显示。
- `KikariaWidget/KikariaWidget.swift`：Widget 数据读取、TimelineProvider、三尺寸布局和 Widget bundle 入口。
- `KikariaMac/KikariaMacApp.swift`：macOS App `@main` 入口。
- `KikariaMac/KikariaMacRootView.swift`：macOS 复用主 ContentView 的窗口包装。
- `scripts/build.sh`：Xcode build 检查脚本，默认输出到 `.build/DerivedData`。
- `Kikaria.xcodeproj/project.xcworkspace/xcshareddata/swiftpm/Package.resolved`：锁定 SwiftMath 1.7.3。
- `Kikaria.xcodeproj/xcshareddata/xcschemes/Kikaria.xcscheme`：主 App shared scheme。
- `Kikaria.xcodeproj/xcshareddata/xcschemes/KikariaWidget.xcscheme`：Widget shared scheme。

## 入口文件

- iOS App：`Kikaria/KikariaApp.swift` 的 `KikariaApp`.
- macOS App：`KikariaMac/KikariaMacApp.swift` 的 `KikariaMacApp`.
- Widget Extension：`KikariaWidget/KikariaWidget.swift` 的 `KikariaWidgetBundle`.
- 主 UI 入口：`Kikaria/ContentView.swift` 的 `ContentView`.

## 配置文件

- `Kikaria.xcodeproj/project.pbxproj`：targets 包括 `Kikaria`、`KikariaWidget`、`KikariaMac`、`KikariaMacTests`、`KikariaMacUITests`。
- `Kikaria/Info.plist`：主 App Info.plist，包含显示名、版本变量、场景配置、方向配置等。
- `KikariaWidget/Info.plist`：Widget Extension Info.plist，`NSExtensionPointIdentifier` 为 `com.apple.widgetkit-extension`。
- `Kikaria/Kikaria.entitlements` 和 `KikariaWidget/KikariaWidget.entitlements`：都配置 App Group `group.com.vita0818.kikaria`。
- `.gitignore`：忽略 `.DS_Store`、`xcuserdata/`、`.build/`、`.swiftpm/`、`DerivedData/`、`build/` 等。
- `Package.resolved`：当前唯一 SwiftPM 远程包为 `SwiftMath`。

## 测试目录

- `KikariaMacTests/KikariaMacTests.swift`：Swift Testing 模板，尚无业务断言。
- `KikariaMacUITests/KikariaMacUITests.swift`：XCTest UI 模板，启动 App 但无业务断言。
- `KikariaMacUITests/KikariaMacUITestsLaunchTests.swift`：启动截图模板。
- 未发现主 iOS App 专属单元测试 target 或 parser/persistence 单元测试。

## 资源目录

- `Kikaria/Assets.xcassets/`：主 App AccentColor 和 AppIcon。
- `KikariaMac/Assets.xcassets/`：Mac App AccentColor 和多尺寸 AppIcon。
- `Presets/`：内置 Markdown 预设，包括 `大学物理.md`、`大学英语Band4.md`、`微积分.md`、`离散数学.md`、`离散数学_BACKUP.md`。
- `KikariaNewIcon.png`：根目录独立图片；当前未在 `project.pbxproj` 中发现直接引用，用途需要后续确认。

## 生成物和缓存目录说明

- `.build/DerivedData`：`scripts/build.sh` 的默认 DerivedData 路径。
- `build/`、`DerivedData/`：Xcode 构建输出，不应纳入文档或源码修改。
- `.DS_Store` 和 `Presets/.DS_Store`：macOS 自动生成物，已被 `.gitignore` 覆盖。

## 不确定项

- `KikariaNewIcon.png` 的当前用途需要后续确认。
- `Presets/离散数学_BACKUP.md` 位于被打包的 `Presets/` 目录中，按当前 `KnowledgePreset.loadBuiltInPresets()` 逻辑可能会作为内置预设被加载；是否有意保留为用户可见预设需要后续确认。
- `KikariaMac` scheme 能被 `xcodebuild -list` 识别，但未在 `xcshareddata/xcschemes/` 目录中找到对应 shared scheme 文件；其来源需要后续确认。
