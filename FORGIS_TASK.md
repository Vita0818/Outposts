# FORGIS_TASK.md

# Kikaria Android Migration Task

你正在执行一次平台迁移任务：将源仓库 `Vita0818/Kikaria` 中的 iOS / iPadOS / macOS 项目，迁移并复刻为 Android 版本。

本次任务不是重新设计 Kikaria，而是对照源仓库中的 iOS 项目结构、交互逻辑、视觉风格和 UI 细节，尽可能像素级复刻出一个 Android 版本。

---

## 0. 本提示词文件位置

本提示词文件名固定为：

`FORGIS_TASK.md`

本文件应放在目标仓库 `Vita0818/Outposts` 的根目录下：

`Vita0818/Outposts/FORGIS_TASK.md`

注意：

`FORGIS_TASK.md` 是本次任务唯一允许放在 `Outposts` 根目录下的任务文件。

除 `FORGIS_TASK.md` 外，本次迁移生成或修改的所有 Android 项目文件，都必须放在：

`Vita0818/Outposts/Kikaria-Android/`

---

## 1. 仓库与目录范围

### 1.1 源仓库

源仓库：

`Vita0818/Kikaria`

源仓库是只读输入。

你必须先完整阅读、分析源仓库中与 iOS 主 App 相关的代码、资源、UI 组件、数据模型、样式系统和交互逻辑，再开始生成 Android 版本。

重点关注但不限于：

- SwiftUI 页面结构
- Kikaria 的主界面 / 首页
- 复习页面
- 预设切换
- 每日目标
- 倒数日
- 重点集锦
- 已掌握清单
- 设置页
- 用户头像 / 资料相关 UI
- Markdown 知识点导入、解析、展示逻辑
- 颜色系统
- 字体系统
- 卡片、气泡、玻璃感组件、按钮、输入框等通用 UI
- iPad / Mac 适配中可迁移到 Android 平板的设计逻辑

---

### 1.2 源仓库严格只读规则

严禁修改源仓库 `Vita0818/Kikaria` 的任何内容。

即使你拥有写权限，也必须把源仓库视为完全只读。

禁止对源仓库执行任何写入、提交、推送或状态改变操作。

明确禁止：

- 修改源仓库中的任何文件；
- 创建源仓库中的任何新文件；
- 删除源仓库中的任何文件；
- 移动或重命名源仓库中的任何文件；
- 格式化源仓库代码；
- 在源仓库中运行会产生文件变更的命令；
- 在源仓库中创建分支；
- 在源仓库中提交 commit；
- 向源仓库 push；
- 在源仓库中创建 Pull Request；
- 修改源仓库 GitHub Actions；
- 修改源仓库 issue、release、wiki 或其它仓库内容；
- 把 Android 迁移结果写回源仓库；
- 为了复用资源而直接改动源仓库资源。

允许的源仓库操作仅限：

- 读取文件；
- 列出目录；
- 搜索代码；
- 查看 git 历史；
- 查看资源文件；
- 分析项目结构；
- 复制必要的设计信息到目标 Android 项目中。

如果某个操作可能导致源仓库文件变化，必须停止，不得执行。

---

### 1.3 目标仓库

目标仓库：

`Vita0818/Outposts`

重要：`Outposts` 不是 Kikaria Android 的专属仓库，它会存储多个迁移项目。

本次任务生成的所有 Android 项目文件，必须全部放在：

`Kikaria-Android/`

也就是说，目标仓库内最终应形成：

`Vita0818/Outposts/Kikaria-Android/`

禁止将 Android 工程文件散落到目标仓库根目录。

---

### 1.4 目标仓库根目录规则

目标仓库根目录只允许存在本任务提示词文件：

`FORGIS_TASK.md`

除 `FORGIS_TASK.md` 外，不得因为本次迁移任务在 `Outposts` 根目录创建、修改、删除其它项目文件。

尤其禁止在 `Outposts` 根目录直接创建：

- `app/`
- `gradle/`
- `.gradle/`
- `.idea/`
- `build.gradle`
- `build.gradle.kts`
- `settings.gradle`
- `settings.gradle.kts`
- `gradlew`
- `gradlew.bat`
- `gradle.properties`
- `local.properties`
- `src/`
- `AndroidManifest.xml`

这些文件如果需要存在，必须放在：

`Kikaria-Android/`

例如：

- `Kikaria-Android/settings.gradle.kts`
- `Kikaria-Android/build.gradle.kts`
- `Kikaria-Android/app/build.gradle.kts`
- `Kikaria-Android/app/src/main/...`

---

## 2. 允许与禁止的访问范围

### 2.1 允许读取

你只能读取：

1. 源仓库 `Vita0818/Kikaria` 的内容；
2. 目标仓库 `Vita0818/Outposts` 的根目录，用于确认 `FORGIS_TASK.md` 和 `Kikaria-Android/` 状态；
3. 目标仓库 `Vita0818/Outposts/Kikaria-Android/` 下的内容，如果该目录已经存在。

---

### 2.2 允许写入

你只能写入：

1. `Vita0818/Outposts/FORGIS_TASK.md`
2. `Vita0818/Outposts/Kikaria-Android/`

除此之外，不得写入其它路径。

---

### 2.3 明确禁止

禁止：

- 修改源仓库；
- 修改 `Outposts` 中其它项目；
- 删除或重构 `Outposts` 中已有的其它项目；
- 将 Android 项目文件放到 `Outposts` 根目录；
- 访问与本次任务无关的仓库、目录、用户文件或系统目录；
- 进行全盘搜索；
- 读取密钥、token、证书、Keychain、环境变量中的敏感信息；
- 使用破坏性 git 命令，例如 `git reset --hard`、`git clean -fdx`、强制推送、删除分支等；
- 因迁移方便而重写 Kikaria 的产品逻辑；
- 把 Kikaria 改造成 Material Design 风格的新产品；
- 使用云端后端替代本地数据逻辑；
- 将用户数据上传到外部服务；
- 引入广告、登录系统、远程数据库、统计 SDK 或非必要网络请求。

---

## 3. 迁移目标

目标是创建一个 Android Studio 可打开、可构建、结构清晰的 Kikaria Android 项目。

推荐技术栈：

- Kotlin
- Jetpack Compose
- Material 3 作为底层能力，但视觉上不要套用默认 Material 风格
- AndroidX Navigation
- Kotlinx Serialization 或等价轻量方案
- 本地持久化优先，可使用 DataStore / Room / JSON 文件，具体选择以实现简洁和贴近源项目为准

Android 项目必须位于：

`Kikaria-Android/`

项目应尽量做到：

1. 可以直接用 Android Studio 打开；
2. 有清晰的 Gradle 配置；
3. 包名合理，例如 `com.vitemis.kikaria`；
4. 代码结构清楚；
5. UI 能够在手机竖屏下复刻 iOS 主体验；
6. 尽可能兼容 Android 平板横屏；
7. 不依赖后端服务；
8. 不依赖 iOS 专有资源；
9. 不使用外部图床；
10. 不生成空壳项目，必须有可运行的主要页面与核心交互。

---

## 4. 执行顺序与迁移策略

本任务必须严格按照以下顺序执行。

---

### 4.1 第一步：检查目标工作区

在开始迁移前，必须先检查目标仓库 `Vita0818/Outposts` 的当前状态。

检查内容包括：

1. 当前仓库根目录下已有文件和目录；
2. 是否已经存在 `Kikaria-Android/`；
3. 如果 `Kikaria-Android/` 已存在，检查其中已有内容；
4. 确认本次任务不会覆盖、删除、移动或污染 `Outposts` 中其它项目；
5. 确认除 `FORGIS_TASK.md` 外，不会在 `Outposts` 根目录生成 Android 工程文件。

检查完成后，再继续下一步。

不得在完成目标工作区检查前直接生成 Android 项目。

---

### 4.2 第二步：只读阅读并理解源仓库

目标工作区检查完成后，必须对源仓库 `Vita0818/Kikaria` 进行只读阅读和理解。

源仓库只能读取，严禁修改。

必须阅读并理解源项目的真实代码结构，而不是根据任务描述臆测 Kikaria 的实现。

重点阅读内容包括但不限于：

- App 入口；
- SwiftUI 页面组织；
- 首页结构；
- 复习流程；
- 数据模型；
- 状态管理；
- 预设 / 知识点 / 学习记录相关逻辑；
- 重点集锦、已掌握、每日目标、倒数日等功能是否真实存在以及如何实现；
- Markdown 或内容解析逻辑；
- 颜色系统；
- 字体系统；
- 通用 UI 组件；
- iPhone / iPad / macOS 适配逻辑；
- Assets / 图标 / 资源文件；
- 项目当前实际完成度。

注意：本提示词中提到的页面、功能和组件名称，可能只是迁移目标方向，不代表源仓库中一定以相同名称或相同结构存在。你必须以源仓库真实代码为准。

---

### 4.3 第三步：输出源仓库理解摘要

在开始写入 Android 项目文件前，必须先形成一份源仓库理解摘要。

摘要应至少包括：

1. 源项目主要目录结构；
2. App 入口和主页面结构；
3. 主要页面清单；
4. 核心数据模型；
5. 主要状态管理方式；
6. 主要 UI 风格特征；
7. 字体、颜色、卡片、气泡、按钮等视觉系统；
8. 已确认存在的功能；
9. 未在源仓库中确认或尚未完整实现的功能；
10. 哪些内容应直接迁移；
11. 哪些内容只能作为 Android 版本预留结构。

这份理解摘要可以写入最终报告，也可以在开发过程中作为内部迁移依据。

不得在未理解源仓库结构的情况下直接按模板生成 Android App。

---

### 4.4 第四步：制定 Android 映射方案

完成源仓库理解后，必须将 iOS / SwiftUI 结构映射到 Android / Kotlin / Jetpack Compose 结构。

映射时应说明或在代码结构中体现：

- SwiftUI View 对应哪些 Compose Screen；
- SwiftUI 组件对应哪些 Compose Component；
- 源项目数据模型对应哪些 Kotlin data class；
- 源项目状态管理对应 Android 中的 ViewModel / Repository / local state；
- 源项目资源对应 Android drawable / vector / Compose 绘制；
- 源项目字体和颜色如何在 Compose Theme 中复刻；
- 哪些 iOS 专属能力需要 Android 平台替代方案。

映射原则是：

先复刻，再适配。

不得为了 Android 平台习惯而主动改变 Kikaria 的产品结构和视觉气质。

---

### 4.5 第五步：开始生成 Android 项目

只有在完成：

1. 目标工作区检查；
2. 源仓库只读阅读；
3. 源仓库理解摘要；
4. Android 映射方案；

之后，才允许开始创建或修改：

`Vita0818/Outposts/Kikaria-Android/`

中的 Android 项目文件。

所有生成文件必须位于：

`Kikaria-Android/`

除 `FORGIS_TASK.md` 外，不得在 `Outposts` 根目录创建 Android 工程文件。

---

### 4.6 迁移策略

必须采用“阅读理解优先，翻译复刻优先”的迁移策略：

1. 先检查目标工作区；
2. 再只读阅读源仓库；
3. 再理解源项目真实结构；
4. 再建立 iOS 到 Android 的映射；
5. 最后生成 Android 项目；
6. 优先复刻源项目已有逻辑；
7. 对源项目尚未实现但产品方向明确的功能，只能做合理预留，不得伪装成已完整实现；
8. 必要时替换平台专属 API；
9. 只在 Android 平台确实需要时做最小调整；
10. 不主动重构产品；
11. 不重新设计信息架构。

你不是在设计一个“类似 Kikaria 的 Android App”，而是在理解真实 Kikaria 源项目之后，把它迁移到 Android。

---

### 4.7 不确定内容处理规则

如果任务描述与源仓库实际代码不一致，以源仓库真实代码为准。

如果源仓库中没有找到某个功能的完整实现，不得臆造其完成度。

可以采取以下方式处理：

- 已实现功能：迁移为 Android 可用功能；
- 部分实现功能：迁移已有部分，并清楚标注未完成部分；
- 仅有设计痕迹的功能：创建合理的数据结构或 UI 预留，但不得宣称已完整实现；
- 源仓库完全没有的功能：除非本任务明确要求，否则不要强行新增。

最终报告必须区分：

- 已从源项目迁移的内容；
- Android 平台适配内容；
- 根据产品方向补充的内容；
- 仅预留但未完整实现的内容。

---

## 5. UI 复刻要求

Kikaria Android 的 UI 必须尽量复刻源 iOS 项目。

重点保持：

- 年轻感
- 通透感
- 高级感
- 淡蓝色 / 玻璃感主视觉
- 精装书感但不过度厚重的字体气质
- 极简布局
- 非必要不增加元素
- 首页气泡的轻盈感
- 卡片式内容结构
- 柔和阴影
- 大圆角
- 克制的动效
- 清晰的主操作入口

不要把它做成普通 Android Material 模板。

---

## 6. 字体与排版要求

源项目中 Kikaria 的视觉气质很依赖字体。

Android 版本应尽量复刻：

- 中文：优先使用系统中文字体中接近宋体 / 书籍感的表现；
- 英文和数字：尽量使用 serif 气质；
- 数字在首页、统计卡片、倒数日、每日目标等位置应有高级感，不要全部使用普通 sans-serif；
- 技术性 ID 或调试文本才使用 monospace；
- 不要在界面中混用过多字体；
- 不要为了 Android 默认一致性牺牲 Kikaria 原本的气质。

如果 Android 无法完全复刻 Apple 字体，应通过 Compose 的 FontFamily、字重、字号、行高和 letter spacing 尽量接近。

---

## 7. 核心页面迁移要求

以下页面和功能是 Kikaria Android 的目标方向。

迁移时必须先在源仓库中确认其真实实现状态：

- 如果源项目已经实现，应尽量直接迁移；
- 如果源项目部分实现，应迁移已有部分并保留扩展结构；
- 如果源项目尚未实现，但属于 Kikaria 已确定的产品方向，可以做轻量预留；
- 不得把未确认或未完成的功能伪装成已经完整迁移。

至少应优先关注以下模块：

1. 首页；
2. 复习 / 学习主流程；
3. 知识点数据模型；
4. 预设或内容集合；
5. 重点集锦 / 收藏类功能；
6. 已掌握 / 学习状态类功能；
7. 每日目标；
8. 倒数日；
9. 设置 / 用户入口；
10. Markdown 或结构化内容导入与展示。

具体页面名称、组件名称和数据结构，必须以源仓库真实代码为准。

### 7.1 首页

首页是 Kikaria 的核心气质页面，必须重点复刻。

应包含：

- 左上角 `Kikaria` 标题；
- 右上角头像 / 用户入口；
- 淡蓝色背景；
- 若干漂浮气泡 / 卡片；
- 每日目标进度气泡；
- 倒数日气泡；
- 当前预设气泡；
- 主开始按钮或箭头入口；
- 轻微、克制、缓慢的动态效果；
- 整体留白和层次应接近源 iOS 项目。

不要在首页堆叠过多解释性文字。

---

### 7.2 复习页面

复习页面应复刻源项目的学习流程。

应包含：

- 当前知识点名称；
- 提示；
- 答案 / 内容；
- 查看提示；
- 查看答案；
- 加入重点集锦；
- 标记已掌握；
- 上一条 / 下一条或等价切换；
- 随机 / 固定洗牌复习逻辑；
- 与源 iOS 项目一致的按钮状态变化。

如果源项目支持手势：

- 上滑查看答案；
- 右滑加入重点集锦；
- 左滑打开范围选择；
- 下滑返回上一条或随机第一条；

Android 版本应尽量用 Compose 手势实现。若部分手势受平台限制，应保留主要操作按钮，并在代码中保持结构便于后续补全。

---

### 7.3 重点集锦

应实现重点集锦列表。

要求：

- 可查看已加入重点集锦的知识点；
- 可从重点集锦中移除；
- 状态与复习页面同步；
- 尽量复刻源项目卡片样式；
- 如源项目有左右滑移除，应尽量实现 Android 等价交互。

---

### 7.4 已掌握清单

应实现已掌握知识点列表。

要求：

- 可查看已掌握内容；
- 可取消已掌握；
- 与复习页面状态同步；
- “开始已掌握复习”在界面中应使用较短表达，例如“开始复习”。

---

### 7.5 预设切换与内容管理

应支持多个预设。

至少保留源项目中的预设结构和示例数据逻辑。

应支持或预留：

- 切换预设；
- 每个预设独立保存学习状态；
- 每个预设独立保存重点集锦；
- 每个预设独立保存已掌握；
- 每个预设独立保存每日目标；
- 手动添加 / 编辑 / 删除知识点；
- Markdown 导入的结构。

不要把所有预设的状态混在一起。

---

### 7.6 每日目标

应实现每日目标设置。

要求：

- 范围参考源项目；
- 首页显示当前进度；
- 复习行为应影响今日进度；
- 每个预设可独立保存；
- UI 尽量复刻源项目的滚轮 / 弹出式选择体验。

如果 Android 原生滚轮实现复杂，可以先用 Compose 中接近的 picker，但视觉应保持 Kikaria 风格，不要使用普通系统弹窗风格。

---

### 7.7 倒数日

应实现倒数日设置与展示。

要求：

- 首页显示倒数日；
- 可设置目标日期；
- 样式接近源项目；
- 不要使用过重的默认 Android DatePicker 外观，除非已经做了样式包裹。

---

### 7.8 设置页 / 用户页

应实现设置入口。

要求：

- 头像；
- 用户名或展示名；
- 每日目标设置；
- 倒数日设置；
- 预设管理入口；
- 关于 Kikaria；
- 整体风格继续保持浅蓝、玻璃感、卡片式。

---

## 8. 数据与内容格式

Kikaria 是本地优先的学习辅助 App。

Android 版本应使用本地数据。

应尽量复刻源项目的数据模型，例如：

- Preset
- KnowledgeItem
- Hint
- Content
- Tags
- Highlight / Favorite
- Mastered
- DailyGoal
- Countdown
- UserProfile

如果源项目已经有明确的数据结构，应优先翻译源结构，而不是重新发明。

---

### 8.1 Markdown 知识点导入

应保留 Kikaria 的 Markdown 内容方向。

支持知识点包含：

- 知识点名称；
- 提示；
- 内容；
- 标签。

不要引入 HTML 作为核心内容格式。

HTML 最多只能作为未来导入转换方向，不得作为 Android 版本的核心内部格式。

---

## 9. Kikaria 内容块方向

Kikaria 未来的内容扩展方向是：

- Markdown
- 本地 assets
- Kikaria-specific fenced content blocks
- 解析为结构化 Kikaria Content Blocks
- 使用专门 renderer 渲染

因此 Android 项目中，内容渲染部分应尽量预留扩展性。

未来可能支持：

- 图片；
- LaTeX / 数学公式；
- 表格；
- 图结构；
- 数据结构；
- 有机化学结构式；
- 代码块；
- 其它学习内容块。

本次迁移不要求全部实现，但不要把架构写死成只能显示纯文本。

---

## 10. Android 工程结构建议

请在 `Kikaria-Android/` 下创建完整项目。

建议结构如下，可根据实际需要微调：

`Kikaria-Android/`
- `settings.gradle.kts`
- `build.gradle.kts`
- `gradle.properties`
- `gradlew`
- `gradlew.bat`
- `gradle/`
- `app/`
  - `build.gradle.kts`
  - `src/main/AndroidManifest.xml`
  - `src/main/java/com/vitemis/kikaria/`
    - `MainActivity.kt`
    - `KikariaApp.kt`
    - `data/`
    - `model/`
    - `ui/`
      - `theme/`
      - `components/`
      - `screens/`
      - `navigation/`
    - `logic/`
    - `content/`
  - `src/main/res/`
    - `drawable/`
    - `mipmap-*`
    - `values/`

请保持目录清晰，不要把所有代码塞进一个文件。

---

## 11. 视觉组件迁移要求

请优先抽象并实现以下 Compose 组件：

- `KikariaTheme`
- `KikariaColors`
- `KikariaTypography`
- `KikariaGlassCard`
- `KikariaBubble`
- `KikariaPrimaryButton`
- `KikariaIconButton`
- `KikariaTextButton`
- `KikariaScreenScaffold`
- `KnowledgeCard`
- `PresetBubble`
- `DailyGoalBubble`
- `CountdownBubble`
- `ReviewActionBar`
- `SettingRow`

组件命名可调整，但必须体现源项目中的样式系统和复用逻辑。

不要在每个页面重复写一套颜色、圆角、阴影和字体。

---

## 12. 动效要求

动效应克制。

优先实现：

- 首页气泡缓慢漂浮 / 旋转；
- 按钮轻微缩放反馈；
- 卡片淡入；
- 状态切换淡入淡出；
- 重点集锦 / 已掌握操作后的轻提示；
- 复习卡片切换的轻动画。

不要加入炫技动画。

不要让动画影响学习效率。

---

## 13. 平台适配

至少适配：

- Android 手机竖屏；
- Android 平板横屏的基本布局。

平板不需要完全重做，但不要在大屏上显得严重拉伸。

可参考源项目 iPad / Mac 的布局思想：

- 大屏增加留白；
- 内容居中；
- 卡片宽度受限；
- 侧边信息可以更舒展；
- 不要把手机 UI 简单拉满全屏。

---

## 14. 资源迁移

请检查源项目中的可迁移资源。

对于 iOS 专用资源：

- 不能直接使用的，应在 Android 中创建等价资源；
- 图标可用 Compose 绘制或 Android vector drawable；
- 不要引用不存在的 iOS asset 名称；
- 不要把源项目中无关的构建产物复制到 Android 项目。

如需要占位头像或图标，应使用本地 vector / Compose 绘制，不要依赖远程图片。

注意：资源迁移只能读取源仓库资源，然后在目标仓库 `Kikaria-Android/` 中创建 Android 等价资源。不得改动源仓库资源文件。

---

## 15. 构建与验证

完成后请尽量保证：

1. Android Studio 可以打开 `Kikaria-Android/`；
2. Gradle sync 结构合理；
3. `app` 模块存在；
4. `MainActivity` 可启动；
5. 首页可见；
6. 主要导航可用；
7. 至少有示例预设和示例知识点；
8. 复习流程可走通；
9. 重点集锦和已掌握状态能在 App 会话中生效；
10. 本地持久化基础可用，或至少结构已经准备好并清楚标注未完成部分。

如果 CI 环境无法实际运行 Android 构建，请仍然保证文件结构和 Gradle 配置尽可能正确，并在最终总结中明确说明未运行的原因。

构建或检查命令只能在：

`Vita0818/Outposts/Kikaria-Android/`

或其子目录中运行。

不得在源仓库中运行可能产生构建产物或文件变化的命令。

---

## 16. 最低可接受成果

本轮迁移最低必须产出：

- 一个完整的 Android Studio 项目目录；
- 项目位于 `Kikaria-Android/`；
- Compose App 可启动；
- 首页已经明显具有 Kikaria 的视觉风格；
- 有复习页面；
- 有预设 / 示例知识点数据；
- 有重点集锦与已掌握的基础逻辑；
- 有设置页或用户页入口；
- 有统一主题、颜色、字体和组件系统；
- 没有把工程文件散落到 Outposts 根目录；
- 没有修改源仓库任何内容。

如果时间不足，优先完成：

1. 工程结构；
2. 主题系统；
3. 首页；
4. 复习主流程；
5. 数据模型；
6. 重点集锦 / 已掌握；
7. 设置与内容管理。

---

## 17. 禁止的“偷懒实现”

禁止：

- 只创建 README；
- 只创建空 Android 工程；
- 只写伪代码；
- 只给计划不落地；
- 用 WebView 包一个网页冒充 Android App；
- 直接套 Material 默认模板；
- 忽略源仓库 UI；
- 把 Kikaria 改成另一个风格；
- 把所有代码写进 `MainActivity.kt`；
- 把项目文件放在目标仓库根目录；
- 删除 Outposts 中其它项目；
- 访问本任务无关文件；
- 修改源仓库任何文件。

---

## 18. 最终输出要求

完成任务后，请在最终报告中说明：

1. 读取了源仓库哪些关键文件或目录；
2. 确认源仓库未被修改；
3. Android 项目生成在什么路径；
4. 创建或修改了哪些关键文件；
5. 已实现哪些页面；
6. 已实现哪些核心逻辑；
7. 哪些地方是源项目的直接迁移；
8. 哪些地方是 Android 平台必要调整；
9. 是否运行了构建或检查；
10. 如果没有运行，说明原因；
11. 后续建议的下一步。

最终报告不要夸大完成度。

如果某个功能只是预留结构，没有完整实现，必须明确说明。

---

## 19. 任务核心判断标准

判断本次迁移是否成功，不看 Android 默认规范是否漂亮，而看：

- 是否像 Kikaria；
- 是否尊重源项目结构；
- 是否复刻源项目视觉；
- 是否保留学习流程；
- 是否保持本地优先；
- 是否把所有项目文件放在 `Kikaria-Android/`；
- 是否没有污染 `Outposts` 根目录和其它项目；
- 是否严格保持源仓库只读且未修改。

请开始执行迁移。
