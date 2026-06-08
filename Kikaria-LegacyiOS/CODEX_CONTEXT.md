# Kikaria Codex Context

本文件用于新的 Codex 对话快速理解 Kikaria 当前项目状态、架构、功能范围和开发红线。开始任何开发任务前，先阅读本文件，再结合当前代码做只读确认。

最后更新：2026-05-07

## 一、项目基本信息

- 项目名：Kikaria
- 当前路径：`/Users/vita/Project/Kikaria`
- GitHub remote：`origin https://github.com/Vita0818/Kikaria.git`
- 技术栈：Swift / SwiftUI / WidgetKit / UserDefaults + Codable JSON
- 产品定位：local-first iOS memorization assistant，本地优先的 iOS 背诵辅助 App
- 核心原则：本地存储、无账号、无云同步、无网络依赖、无第三方库

## 二、重要目录结构

- `Kikaria/`
  - 主 iOS App target 源码和资源目录。
  - 包含 SwiftUI 页面、数据模型、存档、通知、资源和 entitlements。

- `KikariaWidget/`
  - Widget Extension target。
  - 负责读取 App 写入的 `WidgetSnapshot`，渲染 small / medium / large 三种小组件。

- `scripts/`
  - 项目脚本目录。
  - 当前主要脚本是 `scripts/build.sh`，用于本地 Xcode build 检查。

- `Kikaria.xcodeproj/`
  - Xcode 工程配置。
  - 包含主 App target、Widget target、shared schemes、target dependency、signing、bundle id 等配置。
  - 不要随便修改 signing、scheme、target 或 App Icon 设置。

- `README.md`
  - 项目简介和 v0.1 目标。
  - 说明 Kikaria 是本地优先 iOS 背诵助手，支持 Markdown 导入、标签复习、提示/答案展示、重点集锦。
  - 不要随便修改，除非用户明确要求。

- `SPEC.md`
  - v0.1 产品和技术规格。
  - 记录 Markdown 格式、基础数据模型、主页面、开发规则。
  - 不要随便修改，除非用户明确要求。

- `Kikaria/ContentView.swift`
  - 当前主 App 的核心文件。
  - 包含主题、Liquid Glass 修饰器、App 路由、主页面、设置、多预设、本地存档、通知管理、Review Screen、范围选择、今日概览、复习历史、重点集锦、已掌握、Markdown 编辑/说明、个人资料和头像上传等。
  - 文件很大，修改前必须先定位相关 struct/function，避免牵动无关逻辑。

- `Kikaria/KikariaApp.swift`
  - App 入口。
  - 设置 `UNUserNotificationCenter.current().delegate = KikariaNotificationDelegate.shared`，并加载 `ContentView()`。

- `Kikaria/KnowledgePoint.swift`
  - 知识点和预设模型。
  - 包含 `KnowledgePoint`、Markdown 解析/导出、`KnowledgePreset` 和内置预设。
  - `reinforcementCount` 的迁移和编码逻辑在这里，不能轻易改。

- `Kikaria/KikariaTypography.swift`
  - 字体样式工具。
  - 提供 app title、中文标题、正文、按钮、标签、数字等统一字体入口。

- `Kikaria/StudyTracking.swift`
  - 学习行为记录和 Widget 数据模型。
  - 包含 `StudyActivityType`、`StudyActivityRecord`、`WidgetKnowledgePointPreview`、App 侧 `WidgetSnapshot`、App 侧 `WidgetDataStore`。

- `KikariaWidget/KikariaWidget.swift`
  - Widget Extension 的完整实现。
  - 包含 Widget 侧 `WidgetSnapshot`、`WidgetDataStore.loadSnapshot()`、TimelineProvider、small / medium / large 布局、Dark Mode 适配。

- `scripts/build.sh`
  - Xcode build 脚本。
  - 自动选择可用 iPhone Simulator，运行 `xcodebuild` 构建 `Kikaria` scheme，并把 DerivedData 放到 `.build/DerivedData`。

## 三、当前已实现功能

- 首页
  - 显示 `Kikaria` 标题、右上角头像入口、中央泡泡开始按钮、日期/今日进度卡、范围/重点集锦/已掌握/当前预设入口。

- 泡泡开始按钮
  - `StartReviewButton` 使用多个柔和气泡、中心圆形箭头、呼吸和缓慢 orbit 动画。

- 日期 / 今日进度卡
  - 首页显示英文日期、倒数天数、今日新增已掌握数量和每日目标。

- 范围选择
  - `ScopeSelectionView` 支持标签选择。
  - 未选择标签时默认使用全部知识点。
  - 支持搜索标签或知识点。

- 重点集锦
  - 知识点可重复加入重点集锦。
  - 使用 `reinforcementCount` 记录加入次数。
  - `ReinforcementView` 支持搜索、展示 hint/content、移出重点、开始重点背诵。

- 已掌握
  - 知识点可加入已掌握。
  - `MasteredView` 支持搜索、展示卡片、移出已掌握、开始已掌握复习。

- 普通背诵
  - 按当前 `selectedTags` 或全部知识点生成随机复习队列。
  - 初始只显示标题、标签、今日复习次数。
  - 用户可查看提示、查看答案，再执行后续动作。

- 重点背诵
  - 只复习 `reinforcementCount > 0` 的知识点。
  - 可移出重点、加入已掌握或进入下一个。

- 已掌握复习
  - 只复习 `isMastered == true` 的知识点。
  - 可再次加入重点、移出已掌握或进入下一个。

- 三种模式统一按钮区
  - 答案显示后，底部统一使用左侧上下两个按钮加右侧“下一个”按钮的布局。

- 手势操作
  - 上滑：未显示答案时显示答案；已显示答案时进入下一个。
  - 下滑：在允许区域返回上一个/队列前一个。
  - 左滑：按当前模式执行重点或已掌握相关动作。
  - 右滑：普通模式从左侧打开范围选择面板。

- 长答案滚动
  - Review Screen 内容区外层是 `ScrollView`。
  - hint/answer 使用 `FloatingInfoCard`，正文不设置截断，长答案应完整显示并可滚动。

- 重点集锦重复加入次数
  - 每次加入调用 `KnowledgePoint.addReinforcement()`，次数递增。
  - Toast 会显示加入次数。

- 重点集锦按次数排序
  - `ReinforcementView` 先按 `reinforcementCount` 降序，再按 `lastReinforcedAt` 降序，最后按标题排序。

- 今日概览
  - `TodayOverviewView` 展示今日新增已掌握、查看答案、查看提示、总已掌握、倒数日、目标进度文案。

- 复习历史
  - `ReviewHistoryView` 以月历形式展示每日学习记录数量，并显示当日行为摘要。

- 每日目标
  - 设置页可通过 wheel picker 调整 `dailyGoal`，范围 1 到 100。

- 倒数日
  - 设置页支持开始日期和结束日期。
  - 结束日期不能早于开始日期。

- 学习进度通知
  - 每个 preset 可独立开启。
  - 结合倒数日、已掌握进度和安全线判断是否安排本地通知。

- 多预设
  - 内置多个 `KnowledgePreset`。
  - 每个 preset 有独立 `PresetStudyState`。
  - 切换预设会保存当前状态并恢复目标预设状态。

- 上传 / 编辑 / 删除预设
  - 支持新建自定义预设，导入 `.md` / `.txt` 文件或粘贴 Markdown。
  - 支持编辑预设名称、分类、描述。
  - 自定义预设可删除；至少保留一个预设。

- 添加 / 编辑 / 删除知识点
  - `EditPresetView` 和 `EditKnowledgePointView` 支持增删改知识点。
  - 保存后会重新生成该 preset 的 Markdown 文本。

- Markdown 格式说明
  - `MarkdownFormatGuideView` 展示格式模板、完整示例、规则说明。

- AI Prompt 复制
  - Markdown 格式说明页提供整理学习资料的 AI Prompt。
  - 使用 `UIPasteboard.general.string` 复制。

- 新手引导
  - 首次进入后展示 `OnboardingView`。
  - 设置页可重新打开新手引导。

- 首次设置个人资料
  - 首次启动如果未完成个人资料设置，会强制展示 `InitialProfileSetupView`。

- 头像上传
  - 使用 PhotosUI `PhotosPicker` 选择图片。
  - 图片会压缩到最长边 512，保存为 JPEG/PNG Data。

- 本地存档
  - 主状态通过 `UserDefaults.standard` 保存 Codable JSON。
  - 不使用登录、云同步或服务器。

- Widget
  - WidgetKit 小组件支持 systemSmall / systemMedium / systemLarge。
  - App 写入 snapshot，Widget 读取 snapshot 并展示学习概览。

- Dark Mode / Light Mode
  - App 和 Widget 都使用 adaptive colors 或 `colorScheme` 适配深浅色。

- Liquid Glass 风格
  - App 使用自定义 `liquidGlassCard`、`liquidGlassCapsule`、`liquidGlassCircle` 等修饰器。
  - Widget 也有独立 glass card 样式。

## 四、数据模型和持久化

- `KnowledgePreset`
  - 字段：`id`、`name`、`subtitle`、`description`、`category`、`markdownText`、`isBuiltIn`。
  - 内置 preset 包括高等数学、大学英语、解剖学、示例模板。
  - `knowledgePointCount` 通过解析 `markdownText` 计算。

- `KnowledgePoint`
  - 字段：`id`、`title`、`tags`、`hint`、`content`、`isReinforced`、`reinforcementCount`、`lastReinforcedAt`、`isMastered`、`createdAt`、`updatedAt`。
  - Markdown 解析要求：标题以 `#` 开头，包含 `hint:` 和 `content:`，hint/content 非空。
  - tags 用英文逗号或中文逗号分隔。

- `PresetStudyState`
  - 每个 preset 的独立学习状态。
  - 字段：`presetId`、`knowledgePoints`、`markdownText`、`selectedTags`、`dailyReviewRecords`、`activityRecords`、`dailyGoal`、`countdownStartDate`、`countdownEndDate`、`notificationsEnabled`、`notificationTime`、`dangerPercent`。

- `reinforcementCount`
  - 当前重点集锦的真实状态依据。
  - `reinforcementCount > 0` 表示在重点集锦中。
  - `isReinforced` 只是兼容字段，编码时等于 `reinforcementCount > 0`。
  - 解码旧数据时，如果旧 `isReinforced == true` 且没有 count，会迁移为 1。
  - 不要把重点集锦状态改回 Bool。

- `isMastered`
  - 表示是否已掌握。
  - 标记已掌握时会清除重点状态：`clearReinforcement()`。
  - 移出已掌握时不应破坏重点状态。

- `selectedTags`
  - 保存于对应 preset 的 `PresetStudyState.selectedTags`。
  - 恢复时会通过当前知识点可用 tags 过滤无效 tag。

- `dailyGoal`
  - 保存于对应 preset 的 `PresetStudyState.dailyGoal`。
  - 范围被 clamp 到 1 到 100。
  - legacy 默认 preset 可读取旧 key `dailyLearningGoal`。

- `countdownStartDate` / `countdownEndDate`
  - 保存于对应 preset 的 `PresetStudyState`。
  - 旧字段 `countdownDate` 会迁移到 `countdownEndDate`。

- `notificationsEnabled` / `notificationTime` / `dangerPercent`
  - 保存于对应 preset 的 `PresetStudyState`。
  - `notificationTime` 会标准化为今天的 hour/minute。
  - `dangerPercent` 范围为 1 到 100，默认 80。

- `UserProfile`
  - 字段：`displayName`、`userHandle`、`avatarSystemName`、`avatarImageData`。
  - 属于全局 AppState，不按 preset 分开。

- `avatarImageData`
  - 存在 `UserProfile.avatarImageData`。
  - 来源于 PhotosPicker，经过压缩后保存为 Data。

- `StudyActivityRecord`
  - 字段：`id`、`presetId`、`date`、`type`、`pointId`、`pointTitle`。
  - `type` 包括查看提示、查看答案、加入/移出重点、加入/移出已掌握等。
  - 用于今日概览、复习历史、Widget 今日统计。

- `hasCompletedOnboarding`
  - 全局保存于 `KikariaAppState`。
  - legacy 也会读取旧 UserDefaults key `hasCompletedOnboarding`。

- `hasCompletedProfileSetup`
  - 全局保存于 `KikariaAppState`。
  - 如果旧数据没有此字段，会根据 profile 是否不同于默认值推断。

- AppState JSON key
  - `kikaria.appStateJSON`
  - 对应 `KikariaAppState.currentSchemaVersion = 2`。

- WidgetSnapshot key
  - `kikaria.widgetSnapshot`

- App Group
  - `group.com.vita0818.kikaria`
  - App 和 Widget entitlements 均配置该 App Group。

- 加载流程
  - `ContentView.onAppear` 调用 `loadInitialPresetStateIfNeeded()`。
  - 读取 `kikaria.appStateJSON`，成功则 `applyLoadedAppState()`。
  - decode 失败时尝试 legacy `presetLibraryJSON`。
  - 若都不可用，回退到内置预设和默认状态。

- 保存流程
  - 多数状态变化通过 `onChange` 调用 `persistCurrentStudyStateIfReady()` 或 `saveAppStateIfReady()`。
  - App 进入 inactive/background 时保存。
  - 当前 preset 状态会写回 `presetStates[currentPresetID]`。

## 五、Review Screen 红线

1. `ReviewView` 在 `Kikaria/ContentView.swift`。
2. 不要随便重构 Review Screen 布局骨架。当前结构是内容区 `ScrollView` 加底部固定 action region。
3. 长答案必须完整显示，不能给 hint/answer 加 `lineLimit` 截断。
4. 普通模式按钮区：
   - 左上：加入 / 再次加入重点集锦
   - 左下：加入已掌握
   - 右侧：下一个
5. 重点模式按钮区：
   - 左上：移出重点集锦
   - 左下：加入已掌握
   - 右侧：下一个
6. 已掌握模式按钮区：
   - 左上：加入重点集锦 / 再次加入重点集锦
   - 左下：移出已掌握
   - 右侧：下一个
7. 不要把 `reinforcementCount` 改回 Bool。
8. 不要破坏上滑 / 下滑 / 左右滑逻辑：
   - 上滑未显示答案时 reveal answer。
   - 上滑已显示答案时 next。
   - 下滑用于 previous/back queue，但阅读区滚动要优先。
   - 左滑按模式执行重点/已掌握动作。
   - 右滑只在普通模式打开范围面板。
9. 不要破坏 Toast 和学习记录。
   - `revealHint()` 记录 `.viewedHint`。
   - `revealContent()` 记录 `.reviewedAnswer` 并更新 `dailyReviewRecords`。
   - 加入/移出重点和已掌握都要记录对应 `StudyActivityRecord`。
10. 普通模式左滑只加入重点，不能顺手标记掌握。
11. 重点模式左滑只移出重点，不能影响已掌握。
12. 已掌握模式左滑只移出已掌握，不能影响重点状态。

## 六、Widget 红线

1. Widget 支持 `systemSmall` / `systemMedium` / `systemLarge`。
2. Small Widget：
   - 显示 Kikaria。
   - 显示当前预设。
   - 显示日期。
   - 显示今日完成进度：`todayMasteredCount / dailyGoal`。
3. Medium Widget：
   - 左侧显示品牌、预设、日期、进度。
   - 右侧显示 2 个随机知识点卡片。
4. Large Widget：
   - 顶部显示品牌、预设、今日进度、倒数天数。
   - 下方显示最多 4 个随机知识点胶囊。
5. Small / Medium / Large 是独立布局。改一个尺寸时必须确认不会改坏其它尺寸。
6. `WidgetSnapshot` 字段：
   - `presetName`
   - `todayMasteredCount`
   - `masteredCount`
   - `dailyGoal`
   - `countdownDays`
   - `todayReviewCount`
   - `todayHintCount`
   - `randomKnowledgePoints`
   - `lastUpdated`
7. `randomKnowledgePoints` 生成逻辑：
   - App 侧优先使用未掌握知识点：`knowledgePoints.filter { !$0.isMastered }`。
   - 如果全部已掌握，则使用全部知识点。
   - `shuffled().prefix(5)` 后映射为 `WidgetKnowledgePointPreview(title:tag:)`。
8. Widget 数据读取：
   - 先读 App Group `UserDefaults(suiteName: "group.com.vita0818.kikaria")`。
   - 失败后 fallback 到 `UserDefaults.standard`。
   - 都失败时使用 placeholder。
9. Widget 数据写入：
   - App 侧 `WidgetDataStore.save(_:)` 同时写 App Group 和 standard UserDefaults。
   - 写入后调用 `WidgetCenter.shared.reloadAllTimelines()`。
10. Dark Mode 必须适配。
    - Widget 使用 `colorScheme` 和 adaptive colors。
    - 不要引入只适配浅色的硬编码视觉。

## 七、通知系统

- `KikariaNotificationDelegate`
  - 在 `KikariaApp.init()` 中设为 `UNUserNotificationCenter` delegate。
  - 前台收到通知时展示 banner、sound、list。

- `KikariaNotificationManager`
  - 负责请求通知权限、取消通知、重排所有 preset 通知、调试测试通知、进度预警判断。

- 每个 preset 使用独立 identifier：
  - `kikaria.studyProgressWarning.<presetID>`

- 通知文案包含 preset name：
  - `今天的「<presetName>」学习量尚未达标哦，抓紧学习吧。`

- 每个 preset 独立设置通知：
  - `notificationsEnabled`
  - `notificationTime`
  - `dangerPercent`
  - `countdownStartDate`
  - `countdownEndDate`

- 触发条件：
  - 通知开启且系统授权。
  - 有有效知识点。
  - 已设置倒数开始/结束日期。
  - 当前日期不早于开始日期。
  - 当前已掌握进度低于安全线。

- 不要添加远程推送。
- 不要添加服务器。
- 不要添加登录、账号体系或云同步。

## 八、构建与 Git 规则

1. 构建命令：
   - `scripts/build.sh`

2. build 产物：
   - `.build/DerivedData`

3. `.gitignore` 已忽略：
   - `.build/`
   - `DerivedData/`
   - `build/`
   - `.swiftpm/`
   - `*.xcuserstate`
   - `xcuserdata/`
   - `.DS_Store`

4. 不要自动创建 git commit。

5. 每次改动后必须 build。
   - 除非用户明确要求本轮不运行构建。
   - 如果运行 `scripts/build.sh`，要提醒它会写 `.build/DerivedData` 缓存。

6. 不要改 Xcode signing。
   - 不要随便修改 `DEVELOPMENT_TEAM`、`CODE_SIGN_STYLE`、entitlements、bundle id。

7. 不要改 App Icon，除非用户明确要求。
   - App Icon 当前在 `Kikaria/Assets.xcassets/AppIcon.appiconset/`。

8. 不要引入第三方库。

9. 不要引入网络、登录、云同步。

10. 大改前先只读检查并说明计划。

11. 如果遇到旧路径 DerivedData/cache warning：
    - 先汇报。
    - 不要擅自改 Xcode 配置。
    - 如需清理 `.build/DerivedData`，先获得用户明确许可。

12. 当前工作树可能存在用户或 Xcode 产生的本地改动。
    - 修改前先看 `git status`。
    - 不要回退你没有创建的改动。

## 九、后续 Codex 工作方式

未来 Codex 接手 Kikaria 时，请遵守：

1. 先读 `CODEX_CONTEXT.md`。
2. 再用只读命令确认当前代码和 Git 状态，因为本文件可能不是最新状态。
3. 不确定就先只读汇报，不要直接改。
4. 开始修改前明确说明要改哪些文件。
5. 同时明确说明不会改哪些文件，尤其是 README、SPEC、Swift 无关文件、Xcode signing、App Icon。
6. 修改后运行 `scripts/build.sh`，除非用户明确说不要构建。
7. 不自动 commit，不自动 push，不自动开 PR，除非用户明确要求。
8. 重要 UI 不要自由发挥，先按用户截图和描述执行。
9. Review Screen、Widget 三尺寸布局、本地存档、多预设状态、通知逻辑都是高风险区域，改动前要先定位现有实现并说明影响范围。
10. 若任务只要求汇报或计划，严格保持只读。
