# Kikaria & Rokurics Android — 首页布局专项审计

**日期**: 2026-05-31 | **方法**: 只读源代码审查 | **范围**: 两个首页的全部 Composable 代码

---

## 一、Kikaria Android HomePage（`HomeScreen.kt`, 974 行）

### 1.1 Critical — 无

（之前报告的 PadPortrait `weight(1f, fill = true)` 问题已在此版本中修复。第 263-267 行的气泡区 Column 现在没有 `weight()` 修饰符。）

### 1.2 High — PadPortrait 气泡失去垂直居中

**位置**: `HomeScreen.kt` 第 263-272 行

```kotlin
Column(
    modifier = Modifier.fillMaxWidth(),
    horizontalAlignment = Alignment.CenterHorizontally
) {
    KikariaStartBubble(onClick = onStartReview, homeScale = bubbleScale)
}
```

之前的 Critical 修复方式是把 `weight(1f, fill = true)` 整个删掉。但 iOS 设计意图是气泡在 header 和底部卡片之间的**可用空间内垂直居中**。删掉 `weight` 后修复了"底部卡片不可见"的 bug，但引入了新问题：气泡紧贴 header 下方，与底部卡片之间只有 `Spacer(32.dp)`。

在 iPad 竖屏上（1024×1366），header ~100dp，气泡 ~330dp，卡片 ~350dp，总计约 780dp，剩余约 586dp 全部留在页面最底部。气泡没有在可用空间内居中——它被挤在顶部。

**修复**: 用 `Spacer(Modifier.weight(1f))` 放在气泡上方、`Spacer(Modifier.weight(1f))` 放在气泡下方（不使用 `fill = true`），让气泡在 header 和卡片之间自然居中。与使用 `fill = true` 的区别是 weight 分配的不是无限高度而是实际剩余高度。

---

### 1.3 High — 气泡轨道动画同时使用 `rememberInfiniteTransition` 和 `LaunchedEffect`

**位置**: `HomeScreen.kt` 第 576-583 行

```kotlin
val transition = rememberInfiniteTransition(label = "bubble")
var orbitAngle by remember { mutableFloatStateOf(0f) }
LaunchedEffect(Unit) {
    while (true) {
        withFrameMillis { frameMillis ->
            orbitAngle = ((frameMillis % 150_000L).toFloat() / 150_000f) * 360f
        }
    }
}
```

`breathe` 和 `breatheY` 通过 `rememberInfiniteTransition.animateFloat()` 正确实现（渲染线程动画、无重组开销）。但 `orbitAngle` 却使用了 `LaunchedEffect` + `withFrameMillis` 手动更新 `mutableFloatStateOf`，每次更新都会触发重组。

气泡外层容器的 `graphicsLayer { rotationZ = orbitAngle }` 按理说 `graphicsLayer` 内的属性变化不应该触发整树重组——但 `orbitAngle` 本身是 `State` 的读取，`graphicsLayer` 的 lambda 每次重组都会重新执行。正确的做法是用 `rememberInfiniteTransition.animateFloat(0f, 360f, tween(150000))`，与 `breathe` 保持一致的动画管道。

**后果**: 四颗卫星气泡每分钟触发约 60 次重组（以帧率频率），每次重组中 4 个 `GlassyBubble` 的 modifier chain 被重新创建。

**修复**: 将 `orbitAngle` 也改为 `transition.animateFloat(0f, 360f, ...)`。

---

### 1.4 Medium — GlassyBubble 径向高光中心硬编码不随气泡位置变化

**位置**: `HomeScreen.kt` 第 526-535 行

```kotlin
drawCircle(
    brush = Brush.radialGradient(
        listOf(...white...),
        center = Offset(size.width * 0.22f, size.height * 0.22f),
        radius = r
    )
)
```

径向高光的中心固定在 `(size * 0.22, size * 0.22)`——它在气泡自身的左上角。每颗卫星气泡在公转环上位于不同的位置（上/下/左/右），但高光都打在同一个相对方位。在公转过程中，气泡朝向不同方向时高光应该调整位置以保持与环境光一致。

**后果**: 气泡在转到不同方位时，高光始终在左上角，视觉上像"自发光"而非"环境光反射"。

---

### 1.5 Medium — LandscapeLayout 的 avatar 覆盖层使用了 `Box.align(TopEnd)` 但父 Box 是 `fillMaxSize + verticalScroll`

**位置**: `HomeScreen.kt` 第 314-318 行

```kotlin
Box(
    modifier = Modifier
        .fillMaxSize()
        .verticalScroll(rememberScrollState()),
    contentAlignment = Alignment.Center
) {
    Row(...) { ... }
    // Avatar overlay
    Box(
        modifier = Modifier
            .align(Alignment.TopEnd)
            ...
    )
}
```

在 scrollable Box 中，`align(TopEnd)` 定位的子项位于滚动内容的右上角，而非视口固定位置。用户向下滚动时 Avatar 会随内容一起滚出视野——这不是 iOS 的行为。

**后果**: 横屏模式下滚动页面后，设置入口（头像）消失在视口外。

**修复**: 将 Avatar 放在滚动容器外部，作为 overlay。

---

### 1.6 Medium — Bubble 四颗卫星的透明度普遍偏高

| 气泡 | Android 透明度 | iOS 透明度 | 差值 |
|------|---------------|-----------|------|
| Bubble 1 (cyan+mint) | 0.62 | 0.42 | +48% |
| Bubble 2 (lavender+mist) | 0.56 | 0.32 | +75% |
| Bubble 3 (green+cyan) | 0.52 | 0.30 | +73% |
| Bubble 4 (sky+white) | 0.50 | 0.34 | +47% |

iOS 的气泡使用 `opacity(0.42/0.32/0.30/0.34)` 以营造柔和的毛玻璃感。Android 版本的透明度值被整体抬高了约 50-75%，气泡看起来比 iOS 版更"实心"、更不透明。

### 1.7 Low — `HomeLandscapeLayout` 中 `cardScale` 硬编码 1.05

**位置**: `HomeScreen.kt` 第 312 行

```kotlin
val cardScale = 1.05f
```

而 `KikariaPhoneMetrics` 中定义了 `homeLandscapeCardScale()` 函数返回 `min(max(homeLandscapeRightWidth() / 420, 1.0), 1.05)`。这里没有调用它，而是硬编码了一个与函数默认值相同的常量。如果宽度变化导致 rightWidth 与 420 的比率不同，硬编码值不会响应。

---

## 二、Rokurics Android HomePage（`HomeScreen.kt`, 1429 行）

### 2.1 与上次审计相比已修复的问题

- **`isBreathing = isActiveSession || true`** → 已改为 `val isBreathing = isActiveSession`（第 1024 行）
- **HomeContent 缺失导航卡片** → `HomeNavigationCard(...)` 已加入（第 664-670 行）
- **OrbitingBubble 缺失独立呼吸缩放** → 添加了 `breathingScale` 参数和 `.scale(breathingScale)`（第 1234-1291 行）
- **暗色环形阴影硬编码** → 改为 `adaptiveColor(...)`（第 1144 行）
- **底部导航胶囊描边使用 drawBehind** → 已改用 `drawBehind { drawRoundRect(..., style = Stroke(...)) }`（第 177-191 行）

### 2.2 Critical — 无

### 2.3 High — 底部导航胶囊与 HomeContent 中的 HomeNavigationCard 功能冗余

**位置**: 第 146-252 行（底部胶囊）和第 664-670 行（导航卡片）

在 compact 模式的首页上，同时出现两套导航。

**底部胶囊**（`isOnHome && isCompact` 时显示）:
- 学习库 / AI 对话 / Mac 连接

**HomeNavigationCard**（`HomeContent` 中）:
- 学习库 / AI 对话 / Mac 连接

两套组件提供完全相同的三个导航目标。在 compact 模式下用户看到：
1. 顶部标题"Rokurics" + 头像
2. 中央 RecordingOrb
3. 三段式玻璃导航卡片（学习库 / AI 对话 / Mac 连接）
4. 底部半透明胶囊（同样是学习库 / AI 对话 / Mac 连接）

底部胶囊没有提供额外功能——它复制的三个入口在导航卡片中已经存在。在 non-compact（平板）模式下，底部胶囊被 NavigationRail 替代，但此时 HomeNavigationCard 依然存在。

**分析**: iOS 的设计中，导航卡片是首页核心入口，底部没有 dock。底部胶囊的存在看起来是为了在没有导航栏（全屏沉浸式设计）的情况下提供"从子页面快速返回三个主模块"的能力——但它的展示条件 `isOnHome` 意味着它只在首页显示，用户从子页面无法用它导航。

**建议**: 重新思考底部胶囊的定位。如果它是"首页快捷入口"，那么它冗余。如果它是"全局 TabBar"，应该在所有子页面都显示（当前不显示）。

---

### 2.4 High — 底部导航胶囊在浅色模式下虚化不明显

**位置**: 第 168 行

```kotlin
.blur(radius = 0.5.dp)
```

`blur(0.5.dp)` 效果极弱——0.5dp 在 3x 密度设备上仅约 1.5px。iOS 使用 `.background(.ultraThinMaterial)` 产生约 20-30px 半径的高斯模糊。0.5dp 的模糊在人眼几乎无法察觉，胶囊的毛玻璃效果完全依赖 `fillColor.copy(alpha = 0.55f)` 的半透明填充。

**更严重的问题**: `.blur()` 修饰符作用于整个 Box（包括其内部的文字和图标），而非仅作用于背景。这意味着胶囊中的所有导航文本和图标也会被轻微模糊。

**修复**: 将 `blur()` 仅作用于 background 层——使用 `drawBehind` 中的 `drawRoundRect` 配合 `BlurMaskFilter`（Compose 1.6+ 可用），或将模糊效果放在一个独立的背景层 Box 中（不包含内容）。

---

### 2.5 Medium — `HomeContent` 中 metrics 被重新计算但未使用 `RokuricsAdaptiveMetrics`

**位置**: 第 585-591 行

```kotlin
val metrics = RokuricsAdaptiveMetrics.from(maxWidth, maxHeight)
val isWide = metrics.isWide
val isShort = metrics.isShort
val headerScale = metrics.headerScale
val orbScale = metrics.orbScale
val dashboardScale = metrics.dashboardScale
val contentMaxWidth = metrics.contentMaxWidth
```

现在代码正确创建了 `RokuricsAdaptiveMetrics` 实例并使用它的计算属性——这是对之前审计中"重复计算`RokuricsAdaptiveMetrics`逻辑"问题的修复。但仍有一个小问题：`homeBottomPadding` 被写成了内联计算（第 673 行）`if (isWide) 34.dp else if (isShort) 18.dp else 26.dp`，而 `RokuricsAdaptiveMetrics` 中有 `.homeBottomPadding` 属性。应统一使用 Metrics 的属性而非手动重复逻辑。

---

### 2.6 Medium — `transferQueueLabel` 的 `Spacer(navigationBarsPadding())` 被放在了内容流中

**位置**: 第 675 行

```kotlin
Spacer(modifier = Modifier.navigationBarsPadding())
Spacer(modifier = Modifier.height(16.dp))
```

在 `verticalScroll` Column 末尾放置 `navigationBarsPadding()` 作为 Spacer 高度——这在逻辑上是正确的（为底部导航栏留出空间）。但 Compose 的 `navigationBarsPadding()` 通常应该作为 `Modifier.padding()` 的一部分应用于容器，而不是作为内容流中的一个 Spacer。当前用法在纯语义上是等效的，但违反了语义约定（`navigationBarsPadding` 是窗口 insets 修饰符，不是内容间距）。

---

### 2.7 Medium — `HomeContent` 中 Orb 和导航卡片之间的间距没有显式 Spacer

**位置**: 第 656-665 行

```kotlin
RecordingOrb(...)         // 结尾无 Spacer

HomeNavigationCard(...)   // 紧接 Orb 下方
```

iOS 参考中 Orb 和导航卡片之间有 `Spacer(minLength: 32)`（`RokuricsHomeView` 第 39 行）。Android 版这两个元素之间既没有 Spacer 也没有 padding——它们直接通过 Column 的自然排列紧贴在一起。在 `scale = 0.78`（小屏）时看起来合理，但在 `scale = 1.16`（大屏）时 Orb 的 272dp 与导航卡 104dp 紧贴显得很挤。

**修复**: 添加 `Spacer(minHeight)`，值从 iOS 的 `isPadWidth ? 32 : 20` 参考。

---

### 2.8 Low — AmbientBubble 的 `shadowElevation` 使用 `graphicsLayer`

**位置**: 第 1417 行

```kotlin
.graphicsLayer { this.shadowElevation = (sizeDp * 0.08f).coerceIn(4f, 18f) }
```

`graphicsLayer.shadowElevation` 是已废弃的 API（Compose 1.x 时代）。当前 Compose 应使用 `Modifier.shadow(elevation, shape)` 代替。在 Compose 1.5+ 中，`graphicsLayer.shadowElevation` 的行为不再保证一致。

---

### 2.9 Low — PersistentMiniPlayer 拖拽手势未阻止滚动穿透

**位置**: 第 442-470 行

`MinipPlayer` 中有一个 `Slider` 组件，位于可滚动的首页 Column 的子 Column 中。`Slider` 的拖拽手势可能被外层 `verticalScroll` 拦截——在短设备上，用户拖动进度条时首页可能意外滚动。

---

## 三、两页对比总评

| 维度 | Kikaria Android | Rokurics Android |
|------|----------------|------------------|
| Critical 数量 | 0 | 0 |
| High 数量 | 2 (PadPortrait 居中、动画混合) | 2 (冗余导航、模糊无效) |
| Medium 数量 | 3 | 4 |
| Low 数量 | 1 | 2 |
| 上一轮 Critical 修复率 | 1/1 (100%) | 4/4 (100%) |
| 代码总体感 | 中等偏上 | 良好、进步显著 |

两个首页的上一轮 Critical bug 已全部修复。Kikaria 的 PadPortrait `weight(1f)` 修复引入了新的布局偏差（气泡不再居中），这是修复不完整导致的问题。Rokurics 的改进最显著——Orb 动画、导航卡片、暗色环形阴影、气泡呼吸缩放同时修复了 4 个问题。当前仅剩的问题是底部胶囊与导航卡片的冗余和一个语义错误（blur 作用于内容）。
