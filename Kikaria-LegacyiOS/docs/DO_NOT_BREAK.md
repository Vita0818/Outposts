# DO_NOT_BREAK

最后自查日期：2026-05-26

本文件记录修改 Kikaria 时不得破坏的工程约束。未来 Codex 修改前必须先读本文件，再读对应源码。

## 不得破坏的用户数据格式

- Markdown 导入格式：
  - 每个知识点用单独一行 `---` 分隔。
  - 标题必须以 `#` 开头。
  - `tags:` 支持英文逗号和中文逗号。
  - `hint:` 必须在 `content:` 前。
  - hint 和 content 都不能为空。
- `KnowledgePoint` 编解码兼容：
  - `reinforcementCount > 0` 是重点集锦真实状态。
  - `isReinforced` 是兼容字段，编码时应反映 `reinforcementCount > 0`。
  - 旧数据只有 `isReinforced == true` 时应迁移为 `reinforcementCount = 1`。
  - `lastReinforcedAt` 在 count 为 0 时应清空。
- `PresetStudyState` 字段不能随意改名或删除：
  - `knowledgePoints`
  - `markdownText`
  - `selectedTags`
  - `dailyReviewRecords`
  - `activityRecords`
  - `dailyGoal`
  - `countdownStartDate`
  - `countdownEndDate`
  - `notificationsEnabled`
  - `notificationTime`
  - `dangerPercent`
- `KikariaAppState.storageKey` 必须保持 `kikaria.appStateJSON`，除非写迁移。
- Legacy key 不能无迁移删除：
  - `presetLibraryJSON`
  - `dailyLearningGoal`
  - `hasCompletedOnboarding`
  - legacy `countdownDate`
- `dailyGoal` 范围应 clamp 到 1...100。
- `dangerPercent` 范围应 clamp 到 1...100。

## 不得破坏的文件路径约定

- `Presets/` 是内置 Markdown 资源目录，`KnowledgePreset` 会在 App bundle 的 `Presets` 子目录中查找 `.md`。
- `Kikaria/Assets.xcassets/AppIcon.appiconset/` 是主 App icon 来源。
- `KikariaMac/Assets.xcassets/AppIcon.appiconset/` 是 macOS icon 来源。
- `Kikaria/Kikaria.entitlements` 和 `KikariaWidget/KikariaWidget.entitlements` 必须保持 App Group 对齐。
- `scripts/build.sh` 默认写 `.build/DerivedData`，不要改成污染源码目录的输出路径。
- `.gitignore` 中的 `.build/`、`DerivedData/`、`build/`、`xcuserdata/` 等忽略规则不能随意移除。

## 不得破坏的 API / 路由 / 协议 / 存储结构

- `AppRoute` case 会被 `NavigationStack` 使用，删除或改名会影响页面导航。
- `ReviewMode` 三种模式语义必须保持：
  - normal：普通复习。
  - reinforcement：重点集锦。
  - mastered：已掌握。
- `WidgetSnapshot` App 侧和 Widget 侧字段必须同步。
- Widget storage：
  - App Group ID：`group.com.vita0818.kikaria`
  - snapshot key：`kikaria.widgetSnapshot`
  - 先写/读 App Group，再 fallback standard UserDefaults。
- Widget kind：`KikariaProgressWidget`。
- Widget 支持 families：`.systemSmall`、`.systemMedium`、`.systemLarge`。
- 通知 identifier：`kikaria.studyProgressWarning.<presetID>`。
- LaTeX 只识别 `$...$` 和 `$$...$$`，代码块/反引号内容不应被当作公式。

## 不得绕过的安全机制

- 文件导入必须继续使用 security-scoped resource 访问。
- iOS 图片导入应通过 `PhotosPicker` 或等价系统授权机制。
- 本地通知必须经过系统授权，不得绕过权限状态。
- 不要加入登录、云同步、远程推送、运行时网络上传或服务器 API，除非用户明确改变产品方向。
- 不要在文档、源码、日志或测试数据中写入密钥、token、证书私钥、账号密码或真实 shared secret。

## 不得随意重构的核心模块

- `Kikaria/ContentView.swift`
  - Review Screen、路由、Preset 管理、状态保存、通知和 Widget 刷新都在此文件中。
  - 拆分前必须先确认状态流和回归范围。
- `Kikaria/KnowledgePoint.swift`
  - Markdown 解析、导出、`KnowledgePoint` 编解码迁移、内置 preset 加载都在这里。
- `Kikaria/StudyTracking.swift`
  - 学习活动和 Widget snapshot App 侧结构在这里。
- `KikariaWidget/KikariaWidget.swift`
  - Widget 数据结构镜像、timeline 和三尺寸布局在这里。
- `Kikaria/KikariaAdaptiveLayout.swift`
  - iPhone/iPad/Mac 自适应布局指标在这里。
- `Kikaria/KikariaMathText.swift` 和 `Kikaria/KikariaMathFormulaView.swift`
  - 长答案、公式渲染、fallback 和横向滚动相关。

## 不得删除或覆盖的资源

- `Presets/*.md` 内置知识点资源。
- `Kikaria/Assets.xcassets/**` 和 `KikariaMac/Assets.xcassets/**`。
- `KikariaNewIcon.png`，当前用途未确认，删除前必须确认。
- `Kikaria.xcodeproj/xcshareddata/xcschemes/*.xcscheme`。
- entitlements、Info.plist、Package.resolved。
- 用户未提交改动和用户本地文件。

## 不得引入的架构倒退

- 不把本地优先状态改成依赖网络、账号或云端。
- 不把 Widget 数据读取改成只依赖 standard UserDefaults。
- 不把重点集锦重复计数降级成 Bool。
- 不让标记已掌握和重点状态互相污染：
  - 标记已掌握会清重点。
  - 移出已掌握不应破坏重点。
  - 重点模式移出重点不应影响已掌握。
- 不给 Review 长答案、hint/content、LaTeX block 加会截断内容的 `lineLimit`。
- 不只验证一个 Widget 尺寸就修改共享 Widget 布局。
- 不硬编码只适配浅色模式的颜色。

## 修改前必须阅读的关键源码位置

- App 启动：`Kikaria/KikariaApp.swift`
- 主 UI/状态：`Kikaria/ContentView.swift`
- Markdown 与 preset：`Kikaria/KnowledgePoint.swift`
- 学习记录/Widget snapshot：`Kikaria/StudyTracking.swift`
- Widget：`KikariaWidget/KikariaWidget.swift`
- macOS 包装：`KikariaMac/KikariaMacRootView.swift`
- 适配布局：`Kikaria/KikariaAdaptiveLayout.swift`
- 数学渲染：`Kikaria/KikariaMathText.swift`、`Kikaria/KikariaMathFormulaView.swift`
- 构建脚本：`scripts/build.sh`
- 工程配置：`Kikaria.xcodeproj/project.pbxproj`

## 回归验证要求

按改动范围选择验证：

- 数据模型或存档：新增/旧数据解码、保存、重启恢复、legacy 迁移。
- Markdown：合法/非法 Markdown、中文逗号 tags、LaTeX 保留、导出再导入。
- Review：三种模式、手势、提示/答案、长答案滚动、重点/已掌握动作、Toast 和学习记录。
- Preset：切换、新建、编辑、删除、至少保留一个 preset。
- Widget：small/medium/large、深浅色、空数据、长文本、App Group fallback。
- 通知：权限、开关、时间、倒数日期、危险线、不达标/达标两种判断。
- macOS：窗口尺寸、侧边栏、快捷键、资料编辑。
- 构建：至少运行 `scripts/build.sh`，除非用户明确要求不构建或环境不允许。
