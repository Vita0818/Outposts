# Rokurics Android — UI 审计报告

**日期**: 2026-05-31 | **方法**: 只读源代码审查 | **范围**: 全部 6 个 Composable 屏幕 + 主题系统 + GlassStyles

---

## 1. 严重问题（Critical）

### 1.1 HomeContent — Dashboard 卡片和导航卡片被定义但从未渲染

**位置**: `HomeScreen.kt` 第 566-683 行

`HomeContent` 只渲染了三样东西：标题行、Orb、底部空白。`HomeNavigationCard`（第 1293 行定义）、`DashboardStatsCard`（第 710 行）、`DashboardConnectionCard`（第 811 行）、`TransferQueueCard`（第 948 行）全部定义在同一个文件中，但在 `HomeContent` 的 `Column` 里没有被调用。

**iOS 对比**: iOS 的 `RokuricsHomeView` 在 Orb 下方渲染了 `RokuricsHomeNavigationCard`（三段式导航：学习库 / AI 对话 / Mac 连接）。

**后果**: 用户在首页只能看到一个 Orb 和标题。无法从首页导航到学习库、AI 对话或 Mac 连接——只能通过底部胶囊式 Dock 进入（在 compact 模式下）。如果设备判定为 non-compact（平板），BottomNavDock 不渲染，用户完全无法离开首页。

**修复**: 在 `HomeContent` 的 `Spacer(navigationBarsPadding)` 之前插入 `HomeNavigationCard`。

---

### 1.2 GlassStyles — `rokuricsGlassCard` 用错 `background` 叠加导致描边变填充

**位置**: `GlassStyles.kt` 第 27-65 行

```kotlin
fun Modifier.rokuricsGlassCard(...): Modifier = this
    .shadow(...)
    .clip(RoundedCornerShape(cornerRadius))
    .background(fillColor.copy(alpha = fillOpacity))     // 层1: 填充
    .background(                                           // 层2: 渐变"描边" —— 错了
        Brush.linearGradient(...),
    )
    .clip(RoundedCornerShape(cornerRadius))
    .background(                                           // 层3: 内高光
        Brush.linearGradient(...),
        RoundedCornerShape(cornerRadius)
    )
```

在 Compose 中，`Modifier.background()` 是逐层向后绘制的。层2的渐变 `background` 不带 shape 参数，会**填满整个裁剪区域**，而不是只画边框。iOS 的做法是 `overlay { shape.stroke(...) }`——描边在内容上方、仅沿轮廓绘制。

**结果**: Android 版的玻璃卡片看起来是一个渐变填充的矩形，与 iOS 的透明毛玻璃效果完全不同。三次 `background()` 叠加等同于三次纯色填充，没有透明层次感。

**修复**: 使用 `Modifier.drawBehind { drawRoundRect(..., style = Stroke(...)) }` 或 `Modifier.border()` 实现描边，而非 `background(brush)`。

---

### 1.3 RecordingOrb — `isBreathing` 永远为 `true`，空闲态也在呼吸

**位置**: `HomeScreen.kt` 第 1029 行

```kotlin
val isBreathing = isActiveSession || true
```

`true || anything` 在 Kotlin 中永远是 `true`。这意味着 Orb 的缩放动画（`animateFloat` 1f → 1.022f）在所有状态下都在运行。iOS 的设计是空闲时 Orb 静止。

**后果**: 用户不录音时首页 Orb 也在持续微动，与 iOS 的静态 orb 不一致。同时也浪费 GPU 资源。

**修复**: 去掉 `|| true`，改为 `val isBreathing = isActiveSession`。

---

## 2. 高严重度（High）

### 2.1 HomeContent 暗色环形阴影在浅色模式下可见

**位置**: `HomeScreen.kt` 第 1139-1151 行

```kotlin
if (isIdle) {
    Box(
        modifier = Modifier
            .size((238 * effectiveScale).dp)
            .background(Color(0xFF152222).copy(alpha = 0.82f))
            ...
    )
}
```

`Color(0xFF152222)` 是硬编码的暗绿色，alpha 0.82 意味着几乎不透明。在浅色模式下，这是一个突兀的暗色圆环，覆盖在明亮的渐变 Orb 周围。

**后果**: 浅色模式首页 Orb 外面套着一个深色圆环，视觉上非常不协调。

**修复**: 改为 `adaptiveColor`，浅色模式用浅色值或直接去掉这层。

---

### 2.2 底部导航胶囊在浅色模式下丢失背景模糊

**位置**: `HomeScreen.kt` 第 161-195 行

底部导航胶囊的背景层叠顺序正确（fill → gradient stroke → inner gloss），但**缺少背景模糊**。iOS 的 dock capsule 有 `.background(.ultraThinMaterial)` 提供透过模糊看到背景的效果。Android 版本是纯色半透明填充，在复杂背景上看起来像一块色块。

**影响**: 底部导航在首页气泡背景上会失去毛玻璃的柔和感。

---

### 2.3 RecordingLibraryScreen — 每次播放都创建新 MediaPlayer

**位置**: `RecordingLibraryScreen.kt` 第 424-428 行

```kotlin
val mp = MediaPlayer().apply {
    setDataSource(file.absolutePath)
    prepare(); start()
    setOnCompletionListener { stopPlayback() }
}
mediaPlayer.value = mp
```

没有复用、没有调用 `reset()` 前先 `release()` 旧实例。`stopPlayback()` 中有 `release()`（第 141 行），但如果用户快速切换播放不同录音，`release()` 和 `new MediaPlayer()` 之间可能存在资源泄漏。

**影响**: 快速连续切换播放多个录音时可能出现 `IllegalStateException`（MediaPlayer 未正确释放）。

---

### 2.4 AI 对话 — `runBlocking` 在主线程做 IO

**位置**: `AIChatScreen.kt` 第 337 行和第 952 行

```kotlin
runBlocking { chatStore.delete(conv.id) }
runBlocking { chatStore.save(persisted); chatStore.pruneOldest() }
```

`runBlocking` 阻塞当前线程直到协程完成。在 Composable 上下文中，当前线程是主线程。对话历史保存和删除涉及文件 IO（JSON 序列化 + 文件写入），应该用 `LaunchedEffect` 或 `CoroutineScope.launch`，而非 `runBlocking`。

**影响**: 保存对话时主线程会短暂冻结（50-200ms，取决于文件大小和 IO 速度）。

---

### 2.5 主题系统未注册到 MaterialTheme

**位置**: `Theme.kt`（仅 62 行）vs `Typography.kt`

```kotlin
// Theme.kt — 只注册了 colorScheme，没有 typography
MaterialTheme(colorScheme = colorScheme, content = content)
```

`RokuricsTypography` 的函数（`appTitle()`, `caption()`, `largeNumber()` 等）定义了排版 token，但从未注册到 `MaterialTheme.typography`。每个页面都得手动调用 `fontSize = 39.sp, fontWeight = FontWeight.SemiBold, fontFamily = FontFamily.Serif`。

**影响**: 排版 token 系统形同虚设，全局排版一致性靠人工记忆维持。

---

## 3. 中严重度（Medium）

### 3.1 首页 Orb 气泡透明度过低

**位置**: `HomeScreen.kt` 第 1059-1093 行

iOS 的气泡透明度为 0.42/0.32/0.30/0.34（`RokuricsOrbBubble`），Android 版本为 0.30/0.24/0.22/0.26。降低了约 25-30%。在暗色模式下气泡几乎不可见。

### 3.2 RecordingSessionScreen — 4 个 LaunchedEffect 监控重叠状态

**位置**: `RecordingSessionScreen.kt` 第 111-141 行

四个 `LaunchedEffect` 分别监听 `(state, userInteractionTick, isAppActive, isFiling)`、`(isLowPowerMode, elapsedSeconds)`、`(state, isAppActive)`、`(isAppActive)`。逻辑分散，且同一个状态变化（如 `isAppActive` 切到 false）可能触发多个 `LaunchedEffect` 同时执行。

**影响**: 维护困难，未来修改低功耗逻辑时容易引入竞争条件。

### 3.3 RecordingSessionScreen — FilingOverlay 候选项未过滤祖先层级

**位置**: `RecordingSessionScreen.kt` 第 615-638 行

iOS 的 `StudyFilingCandidateResolver` 会按当前已选择的归档路径筛选候选项（选了"数学"后只显示数学下的课程）。Android 版只按 `activeLevel` 收集所有候选项，不检查这个候选项是否属于当前已在上级选定的归档。

**影响**: 选了"数学"后切换到"课程"级别，候选项列表仍然显示所有课程（包括语文、英语下的），逻辑不正确。

### 3.4 Color.kt — light-only gradients used as static fields

**位置**: `Color.kt` 第 72-88 行

```kotlin
val actionGradientBrush: Brush = Brush.linearGradient(
    colors = actionGradientLight, ...  // 硬编码 light
)
```

这些 Brush 被创建为 `object` 的静态属性——一旦创建，颜色就固定了，不会随暗色模式切换而改变。虽然调用方有 `adaptivePageGradientBrush()` Composable 函数来获取正确版本，但静态 Brush 的存在本身就是陷阱。

### 3.5 AIChatScreen 和不读 `RokuricsTypography` token

**位置**: `AIChatScreen.kt` 全文

整个 AI 聊天页面从未使用 `RokuricsTypography.largeNumber()` 或 `appTitle()`。所有字体大小都是直接 `fontSize = 32.sp` 硬编码。`appTitle` 定义了 serif 39sp semibold，但 AI 聊天页标题用的是 sans-serif 32sp bold。

### 3.6 MacConnectionScreen — 配对的 device bubble 没有呼吸动画

**位置**: `MacConnectionScreen.kt` 第 225-249 行

iOS 的配对状态 bubble 有与 Orb 类似的呼吸动画。Android 版是一个完全静态的圆形图标。与首页 Orb 和 RecordingSession 页的丰富动画形成断层。

---

## 4. 低严重度（Low）

### 4.1 `formatPosition` 和 `formatPositionMini` 重复定义

**位置**: `HomeScreen.kt` 第 484-496 行

两个函数逻辑完全相同，其中一个未被使用。

### 4.2 AmbientBubble — 缺少 blur

**位置**: `HomeScreen.kt` 第 1397-1425 行

iOS 的 `RokuricsAmbientBubble` 有 `blur(radius: 0.15)` 来柔化气泡边缘。Android 版没有。气泡在边缘处看起来过于锐利。

### 4.3 OrbitingBubble — 反旋转抵消了轨道旋转

**位置**: `HomeScreen.kt` 第 1243-1290 行

每个 `OrbitingBubble` 使用 `graphicsLayer { rotationZ = counterRotation }`——而 `counterRotation = -orbitAngle`。这抵消了外层旋转环带来的旋转，使得每个气泡在屏幕上保持不动。iOS 的 `RokuricsOrbBubble` 也有这个反旋转，用于保持气泡内部渐变方向不变，但 iOS 在气泡层还额外叠加了 `scaleEffect(isBreathing ? 1.035 : 0.985)` 来产生呼吸感。Android 的 `OrbitingBubble` 没有这个独立的呼吸缩放。

### 4.4 `HomeContent` 的自适应计算复制了 `RokuricsAdaptiveMetrics` 的逻辑

**位置**: `HomeScreen.kt` 第 584-605 行

`HomeContent` 内部重新计算了 `isWide`、`isShort`、`orbScale`、`headerScale`、`dashboardScale`——这些与 `RokuricsAdaptiveMetrics` 中的定义完全重复。定义好的自适应类没有被使用。

---

## 5. 正面发现

以下方面在迁移中做得很好：

1. **RecordingOrb 的 Canvas 描边实现正确** — 与 GlassStyles 中的 `rokuricsGlassCard` 不同，Orb 的描边使用了 `Canvas(Modifier.fillMaxSize()) { drawCircle(..., style = Stroke(...)) }`，这是正确的做法。

2. **RecordingSessionScreen 的低功耗模式** — 完整复刻了 iOS 的 5 秒无操作进入全屏分钟数显示 + 点击退出模式。Android 还额外用 `LifecycleEventObserver` 正确处理了生命周期感知。

3. **FilingOverlay 的四级归档 UI** — 层级选择按钮、候选项 FlowRow、新建输入框、保存/直接保存双按钮，完整迁移了 iOS 的归档体验。

4. **MacConnectionScreen 的配对表单** — 输入验证（端口数字过滤、指纹 SHA256 格式、配对码 6 位数字）做得到位。连接状态卡片和退避状态显示也完整。

5. **自适应底部导航** — Compact 模式用底部 Capsule、Medium+ 用 NavigationRail，比 iOS 只做手机形态更进一步。

6. **`adaptiveColor()` Composable** — 一行 `if (isSystemInDarkTheme()) dark else light` 包住了所有颜色切换需求，语义清晰。

7. **ChatBubble 的不对称圆角** — 用户消息右下角小圆角、助手消息左下角小圆角（`bottomStart = if (isUser) 16.dp else 4.dp`），这个细节对聊天 UI 很重要。

---

## 6. 总结

| 严重度 | 数量 | 关键项 |
|--------|------|--------|
| Critical | 3 | Home 页无导航卡片、玻璃描边变成填充、Orb 永远在呼吸 |
| High | 5 | 暗色环形阴影、底部导航无模糊、MediaPlayer 泄漏、主线程 runBlocking、主题未注册 |
| Medium | 6 | 气泡透明度低、LaunchedEffect 重叠、Filing 候选项过滤缺失、静态 Brush 陷阱等 |
| Low | 4 | 代码重复、模糊缺失、自适应类未使用等 |

最关键的问题是 **Home 页缺失导航卡片**——如果设备判定为平板（width ≥ 600dp），用户将无法从首页导航到任何子页面。这是一个阻塞级 bug。

玻璃效果的实现方式（`background` 叠加代替 `stroke`）从根本上偏离了 iOS 的设计语言，导致所有玻璃卡片看起来是"渐变填充块"而非"透明毛玻璃卡片"。
