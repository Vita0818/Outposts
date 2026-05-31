# Kikaria Android — UI 审计报告

**日期**: 2026-05-31 | **方法**: 只读源代码审查 | **范围**: 全部 16 个 Composable 屏幕 + 主题系统 + 共享组件

---

## 1. 严重问题（Critical）

### 1.1 HomeScreen PadPortrait — 气泡区爆炸，Dashboard 卡片不可见

**位置**: `HomeScreen.kt` 第 263-267 行

```kotlin
Column(
    modifier = Modifier.fillMaxWidth().weight(1f, fill = true),
    verticalArrangement = Arrangement.Center
) {
    KikariaStartBubble(onClick = onStartReview, homeScale = bubbleScale)
}
```

**链路**: 内层 Column（无高度约束）位于外层 `Column(fillMaxSize + verticalScroll)` 中。`verticalScroll` 向其子项传入 `maxHeight = Infinity`。内层 Column 收到这个约束后，`weight(1f, fill = true)` 计算 `remaining = Infinity - header高度 - 底部卡片高度 = Infinity`。气泡区被分配了无限高度，底部卡片（Dashboard card）被推到无限远。

**后果**: iPad 竖屏下，用户打开首页只看到一个气泡浮在画面中间。向下滚动永远触不到 Dashboard 卡片。`CompactHomeLayout` 不受影响，因为手机布局没用 `weight()`。

**修复**: 去掉 `weight(1f, fill = true)`，改为用 `Spacer` 分配剩余空间；或者去掉外层 `verticalScroll`，让 Column 自然填满屏幕（iPad 竖屏内容不需要滚动）。

---

### 1.2 ReviewScreen — 底部操作区撑满全屏，答案卡片不可见

**位置**: `ReviewScreen.kt` 第 1009-1018 行

```kotlin
ReviewActionButton(
    text = "下一个", icon = "↬", ...,
    modifier = Modifier.weight(0.54f).fillMaxHeight(),  // ← 问题
    onClick = { viewModel.nextPoint() }
)
```

**链路**: 这个按钮位于 `ReviewBottomActionBar` 中的 Row 内。Row 位于底部 Box（`heightIn(min = 138.dp)`，无 max 限制）。Box 位于外层 Column 的非 weighted 子项中，收到的约束含 `maxHeight ≈ 屏幕高度`。Row 测量时，右侧"下一个"按钮的 `fillMaxHeight()` 索取 Row 的全部可用高度（≈ 屏幕高度），左侧两个按钮按内容包裹。Row 高度 = max(左, 右) ≈ 屏幕高度。

**后果**: 用户点开答案后，内容区 `weight(1f)` 被压缩到零，只能看到巨大的"下一个"按钮。点击"查看答案"后核心体验完全崩溃。

**修复**: 底部 Box 加 `heightIn(max = 220.dp)`；或者去掉 `fillMaxHeight()`，靠 Row 的 `verticalAlignment` 自然决定高度。

---

## 2. 高严重度（High）

### 2.1 ScopeSelectionPanel 在手势层捕获全部触摸

**位置**: `ReviewScreen.kt` 第 566-573 行

```kotlin
Box(
    modifier = Modifier
        .fillMaxSize()
        .background(Color.Black.copy(alpha = scopePanelAlpha * 0.35f))
        .pointerInput(Unit) { detectTapGestures { showScopePanel = false } }
)
```

当范围面板展示时（从左侧滑入），这个 `pointerInput` 覆盖了整个屏幕。`scopePanelAlpha` 动画是从 0 到 1，在动画初期（alpha ≈ 0），用户看不到面板但所有触摸已被劫持。

**后果**: 面板打开/关闭动画期间（300ms），ReviewScreen 的滑动手势全部失效。如果用户在手势阈值内松开，面板不会关闭但内容已被遮挡。

**修复**: `pointerInput` 只放在半透明遮罩上，面板卡片区域不拦截手势；或者用 `Modifier.clickable` 替代 `pointerInput(Unit) { detectTapGestures }` 仅在面板外区域生效。

---

### 2.2 PickerDialog 遮罩层不可见

**位置**: `SettingsScreen.kt` 第 208 行

```kotlin
Box(Modifier.fillMaxSize()
    .background(Color.Black.copy(alpha = 0.001f))
    .clickable { onDismiss() }, ...)
```

`alpha = 0.001f` 在 Human 视觉中完全透明。在暗色模式下，对话框玻璃卡片和页面背景颜色相近，用户无法区分。

**后果**: 用户打开一个 picker（如每日目标、倒数日），看到的是一张玻璃卡片悬浮在与背景几乎相同的页面上，没有遮罩提示这是模态弹窗。点击卡片外的区域会意外关闭 picker，但用户无法预判这个行为。

**修复**: `alpha` 改为 `0.35f` 或 `0.4f`。

---

### 2.3 TodayOverviewScreen — LazyVerticalGrid 在 verticalScroll 中嵌套

**位置**: `TodayOverviewScreen.kt` 第 141-171 行

```kotlin
KikariaScrollPageShell(...) {  // 内部有 verticalScroll
    ...
    LazyVerticalGrid(
        columns = GridCells.Fixed(2),
        ...
    ) { ... }
}
```

`KikariaScrollPageShell` 内部使用 `Column + verticalScroll`，而 `LazyVerticalGrid` 作为其子项被放入这个可滚动容器中。

**后果**: `LazyVerticalGrid` 在 `verticalScroll` Column 中会丢失懒加载能力——所有网格项被一次性测量和布局。尽管 TodayOverview 只有 4 个格子，当前不会出问题，但这个模式是错误的。

**修复**: 将整个页面改为非懒加载的 `Column` 直接排列固定网格；或者让页面本身就是一个 `LazyColumn`，将网格作为 `item {}` 放入。

---

## 3. 中严重度（Medium）

### 3.1 ReviewScreen — 范围面板标签列表不使用 LazyColumn

**位置**: `ReviewScreen.kt` 第 661 行

```kotlin
Column(
    modifier = Modifier.weight(1f).verticalScroll(rememberScrollState()),
    verticalArrangement = Arrangement.spacedBy(10.dp)
) {
    filteredTags.chunked(2).forEach { rowTags ->
        Row(...) { ... }
    }
}
```

`chunked(2).forEach` 在组合阶段遍历所有标签，没有使用 `LazyColumn`。如果用户有 50+ 标签，所有标签行会在首次进入 scope panel 时全部组合。

**后果**: 标签数量多时（>30），scroll 的惰性测量失效，首帧组合时间过长。

**修复**: 替换为 `LazyColumn`，用 `items(filteredTags.chunked(2))` 替代 `forEach`。

---

### 3.2 SettingsScreen 自定义 Toggle 无动画

**位置**: `SettingsScreen.kt` 第 186 行

```kotlin
Box(
    Modifier.size(51.dp, 31.dp)...
    contentAlignment = if (isOn) Alignment.CenterEnd else Alignment.CenterStart
) { Box(Modifier.size(23.dp)...) }
```

`contentAlignment` 作为 Box 参数，在 `isOn` 变化时直接跳变，没有过渡动画。这与其他 UI 元素（按钮缩放动画、呼吸动画）形成反差。

**修复**: 用 `Modifier.offset(x = animateDpAsState(...))` 驱动 thumb 位置。

---

### 3.3 ScopeSelectionScreen — emoji 搜索图标

**位置**: `ScopeSelectionScreen.kt` 第 133 行

```kotlin
Text("🔍", fontSize = (15 * scale).sp)
```

使用 emoji 而非 `KikariaIcons.search`（`Icons.Filled.Search`）。emoji 渲染因 Android OEM 皮肤而异——三星、小米、Pixel 各有自己的 emoji 字体，外观不一致。

**后果**: 搜索图标在不同设备上可能完全不同，与设计系统的其余部分不协调。

**修复**: 使用 `Icon(KikariaIcons.search, ...)`。

---

### 3.4 CompactHomeLayout — 顶部 padding 硬编码

**位置**: `HomeScreen.kt` 第 168 行

```kotlin
Row(modifier = Modifier.fillMaxWidth().padding(top = 14.dp), ...)
```

iPad 版的 `PadPortraitHomeLayout` 使用 `topPadding = if (isLargePortrait) 58.dp else 48.dp`，但手机版硬编码了 `14.dp` 而没有使用 `metrics` 系统。

**后果**: 在可折叠设备（如 Galaxy Fold 展开态）或小屏设备上，`14.dp` 可能不适用。代码不一致也增加维护负担。

**修复**: 统一用 `metrics.titleTopPadding` 或类似 token。

---

### 3.5 EditorTextField 在 EditPresetScreen 和 NewPresetScreen 中重复定义

**位置**: 两个文件的末尾各有一个完全相同的 `private fun EditorTextField`

**后果**: 维护成本翻倍——修改预设编辑器的输入框样式必须改两处。

**修复**: 提取到 `KikariaSharedComponents` 或同一个文件中的共享位置。

---

### 3.6 PickerWheel 使用 ‹ › 箭头而非回弹式滚轮

**位置**: `SettingsScreen.kt` 第 225-236 行

Picker 使用左右箭头逐值切换（`val idx = values.indexOf(selected); if (idx > 0) onSelected(values[idx - 1])`）。在 `DateRangePicker` 中，日期按月粒度调整，无法选择具体日期。

**后果**: 用户设置"每日学习目标"时，从默认值调到想要的值（如 20→5）需要点 15 次箭头。日期选择只能按月调，无法精确到日。

**修复**: 使用 Compose 的原生 `DatePickerDialog` / `TimePicker` 或 `NumberPicker`。

---

## 4. 低严重度（Low）

### 4.1 EditProfileScreen — 头像加载在主线程

**位置**: `EditProfileScreen.kt` 第 94-103 行

```kotlin
val avatarBitmap = remember(avatarUri) {
    try {
        val uri = avatarUri?.let { Uri.parse(it) }
        if (uri != null) {
            context.contentResolver.openInputStream(uri)?.use { stream ->
                BitmapFactory.decodeStream(stream)
            }?.asImageBitmap()
        } else null
    } catch (_: Exception) { null }
}
```

`BitmapFactory.decodeStream` 在主线程执行。对于大尺寸照片，可能导致首帧卡顿。

**修复**: 用 `LaunchedEffect` + `Dispatchers.IO` 异步解码，或使用 Coil/Glide。

---

### 4.2 MarkdownFormatGuideScreen — CodeBlock 未使用 KikariaTypography

**位置**: `MarkdownFormatGuideScreen.kt` 第 285 行

```kotlin
Text(text = text, ..., fontFamily = FontFamily.Monospace, ...)
```

代码块直接使用 `FontFamily.Monospace` 而非 `KikariaTypography.technical()`。排版 token 系统被绕过。

**修复**: 使用 `KikariaTypography.mixedText(text, ...)` 或至少用统一的技术字体 token。

---

### 4.3 ProfileSetupScreen — 硬编码初始用户名检查

**位置**: `ProfileSetupScreen.kt` 第 74-79 行

```kotlin
var displayName by remember { mutableStateOf(
    if (initialDisplayName == "Vita" || initialDisplayName.isEmpty()) "" else initialDisplayName
) }
var userHandle by remember { mutableStateOf(
    if (initialHandle == "vita_0818" || initialHandle.isEmpty()) "" else initialHandle
) }
```

`"Vita"` 和 `"vita_0818"` 是硬编码到生产代码中的测试数据。每位新用户叫 Vita 的人都会被要求重新填写名称。

**修复**: 去掉这些硬编码检查，仅检查 `isEmpty()`。

---

### 4.4 暗色模式下的玻璃 card 内高光偏暗

**位置**: `KikariaSharedComponents.kt` 第 100-103 行

```kotlin
fun Modifier.kikariaGlassInnerHighlight(shape, isDark): Modifier = this.drawBehind {
    val hlOpacity = if (isDark) 0.10f else 0.18f
    drawRoundRect(color = Color.White.copy(alpha = hlOpacity), ...)
}
```

暗色模式高光只有 10%，而 iOS 参考是暗色模式下高光也保持在约 25% 以提供清晰的玻璃质感边沿。当前效果在暗色模式下玻璃卡片几乎与背景混为一体。

**修复**: 暗色高光改为 `0.16f` 到 `0.22f`。

---

## 5. 正面发现

以下方面做得很好，值得在其他平台迁移时参照：

1. **`KikariaTypography.mixedText()`** — 中英混排字体切换（中文用系统默认、拉丁字母用 Serif）实现完整。相比之下，Rokurics Android 的 Typography 完全没有这个能力。

2. **`KikariaGlassCard` 的描边绘制** — 使用 `drawBehind` + `Stroke` 而非叠加 `background` 层，比 Rokurics Android 的 GlassStyles 叠加方式更接近 iOS 原版。

3. **`KikariaPhoneMetrics`** — 309 行的自适应系统精确度极高，覆盖 compact/regularPad/widePad 三种形态，包含 iPad 竖屏/横屏、可折叠设备等场景。每类页面（home/review/form/settings）都有独立的 maxWidth、scale 和 padding token。

4. **暗色/浅色双模式** — 每个颜色 token（Sky/SkyDark、GlassSurface/GlassSurfaceDark 等）都做了双值定义，且所有渐变（PageGradient、ActionGradient 等）都区分了浅色和深色版本。比 HarmonyOS 的硬编码暗色方案强得多。

5. **多形态首页** — `CompactHomeLayout`、`PadPortraitHomeLayout`、`HomeLandscapeLayout` 三种布局，以及 `KikariaStartBubble` 的呼吸动画使用 Compose 的 `rememberInfiniteTransition` 而非 `setTimeout`，动画质量高于 HarmonyOS 的实现。

6. **`KikariaScrollPageShell` / `KikariaFormPageShell`** — 统一的页面外壳组件（渐变背景、返回按钮覆盖层、滚动/表单布局）确保了全局一致性。

---

## 6. 总结

| 严重度 | 数量 | 关键项 |
|--------|------|--------|
| Critical | 2 | HomeScreen iPad 气泡爆炸、ReviewScreen 底部操作区撑满 |
| High | 3 | Scope 面板手势劫持、Picker 遮罩透明、LazyGrid 嵌套 |
| Medium | 6 | LazyColumn 缺失、Toggle 无动画、emoji 图标、硬编码 padding 等 |
| Low | 4 | 主线程图片解码、硬编码用户名、暗色高光偏暗等 |

两个 Critical 问题都会在真实设备上导致核心页面不可用——iPad 首页 Dashboard 卡片不可见、背诵页答案区被压缩。建议优先修这两个。

9 个中低严重度问题属于代码质量和一致性范畴，不影响基本功能，但累计影响用户体验的精细度。
