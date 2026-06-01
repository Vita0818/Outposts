# Kikaria & Rokurics Android — 首页像素级布局审计

**日期**: 2026-05-31 | **方法**: iOS 源代码逐值对比 Android 实现 | **范围**: 首页的间距、尺寸、字体、定位

---

## 一、Rokurics Android HomePage

### 基准: iOS `RokuricsHomeView` (第 22-52 行)

```swift
VStack(spacing: 0) {
    homeHeader                    .padding(.top, metrics.homeTopPadding)
    Spacer(minLength: isPadWidth ? 34 : 22)      // ① header → orb
    RecordingOrb
    Spacer(minLength: isPadWidth ? 32 : 20)      // ② orb → nav card
    HomeNavigationCard            .padding(.bottom, metrics.homeBottomPadding)
}
.padding(.horizontal, metrics.horizontalPadding)
.frame(maxWidth: metrics.homeMaxWidth)
.frame(maxWidth: .infinity, minHeight: metrics.height, alignment: .top)
```

### Android `HomeContent` 实现 (第 568-678 行)

对比：

| 元素 | iOS 值 | Android 值 | 偏差 |
|------|--------|-----------|------|
| header topPadding | compact=18, pad=24 | isWide?24:18（来自 metrics） | ✓ 正确 |
| ① header→Orb 间距 | compact=22, pad=34 | isWide?34:22 | ✓ 正确 |
| ② Orb→导航卡 间距 | compact=20, pad=32 | **无** | ✗ 完全缺失 |
| ③ 导航卡→底部 | compact=26(760+)/18(<760), pad=34 | isWide?34:26/18 | ✓ 正确 |
| ④ 水平 padding | compact=24(<360:20), pad=32 | 使用 metrics.horizontalPadding | ✓ 正确 |
| ⑤ 最大宽度 | compact=infinity, pad=680/760 | 使用 metrics.contentMaxWidth | ✓ 正确 |
| ⑥ VStack minHeight | 使用 metrics.height | 无（靠 verticalScroll） | ⚠ 无 minHeight |

**缺失 ② 的视觉效果：** iOS 在 Orb(底部边缘 190dp) 和导航卡(顶部边缘)之间有 20-32dp 的呼吸空间。Android 版本 Orb 和导航卡紧贴在一起。在暗色模式下，Orb 的环形阴影直接压在导航卡的描边上，产生视觉冲突。

---

### 导航卡描边叠加顺序问题

```kotlin
// Android: HomeNavigationCard (第 1306-1328 行)
Row(
    modifier = Modifier
        .fillMaxWidth()
        .height((104 * scale).dp)
        .shadow(elevation = (20 * scale).dp, ...)
        .clip(RoundedCornerShape((30 * scale).dp))
        .background(navCardFill)                               // 层1: 填充
        .background(Color.White.copy(alpha = 0.12f), ...)     // 层2: 白色覆盖
        .background(                                            // 层3: 渐变描边→实际是填充
            Brush.linearGradient(...),
            RoundedCornerShape((30 * scale).dp)
        )
)
```

`background(Brush, shape)` 在 Compose 中是**形状内的渐变填充**，不是描边。iOS 的做法是 `overlay { RoundedRectangle().stroke(LinearGradient(...), lineWidth: 1) }`——这是一个 1px 描边。Android 版的三层 `background` 叠加会在导航卡内部产生一个渐变矩形填充，视觉上卡片内部出现一个多余的渐变色块。

**正确实现**应当是对比度：使用 `Modifier.drawBehind { drawRoundRect(..., style = Stroke(width = 1.dp.toPx())) }` 只画边框，而非 `background(Brush, shape)`。

---

### 底部胶囊 blur 污染文字

```kotlin
// 第 164-175 行
Box(
    modifier = Modifier
        .fillMaxWidth()
        .height(54.dp)
        .blur(radius = 0.5.dp)   // ← blur 作用于整个 Box
        ...
        .clip(RoundedCornerShape(50))
        .background(capsuleFill)
        ...
)
```

`blur(0.5.dp)` 的效果是：顶部 0.5dp 被轻微模糊，**包括 Box 内部的 Row 中的文字和图标**。0.5dp 的模糊量太小不足以产生毛玻璃背景效果，但足以对文字的可读性产生负面影响。文字看起来像"失焦"了一点点。

---

## 二、Kikaria Android HomePage

### Compact 布局（手机）

| 元素 | iOS 值 | Android 值 | 偏差 |
|------|--------|-----------|------|
| 标题上边距 | 14（`.padding(.top, 14)`） | 18（`metrics.titleTopPadding`） | **+4dp (+29%)** |
| 标题→气泡间距 | `Spacer(minLength: 32)` | `Spacer(32.dp)` | ✓ 正确 |
| 气泡→卡片间距 | `Spacer(minLength: 30)` | `Spacer(30.dp)` | ✓ 正确 |
| 卡片底部间距 | `.padding(.bottom, 12)` | `Spacer(12.dp)` | ✓ 正确 |
| 卡片内 VStack 间距 | `spacing: 12` | `spacedBy(12.dp)` | ✓ 正确 |
| 标题字号 | `39 * headerScale` | `(39 * headerScale).toInt()` | ✓ 正确 |
| 头像大小 | `44 * headerScale` | `(44 * headerScale).dp` | ✓ 正确 |
| VStack alignment | `alignment: .center`（整帧居中） | `contentAlignment = TopCenter` | **不一致** |

**标题上边距偏差**：iOS 用 `.padding(.top, 14)` 硬编码（非 metrics 驱动），Android 使用 `metrics.titleTopPadding = 18.dp`。标题向下偏移了 4dp，在 390dp 宽的屏幕上约占垂直空间的 1%。

**VStack alignment**：iOS 在 `padPortrait` 以外使用 `alignment: .center`，让内容在 minHeight 帧内垂直居中。Android 使用 `TopCenter`，内容贴在顶部。如果内容总高度小于屏幕高度（常见于大屏手机），iOS 标题 + 气泡会显示在视口中央偏上区域，Android 则全部置顶。

### PadPortrait 布局（iPad 竖屏）

| 元素 | iOS 值 | Android 值 | 偏差 |
|------|--------|-----------|------|
| 页面顶部 padding | `isLargePortrait ? 58 : 48` | 同（来自 metrics） | ✓ 正确 |
| 标题字号 | `isLargePortrait ? 58 : 54` | 同 | ✓ 正确 |
| 头像大小 | `isLargePortrait ? 66 : 62` | 同 | ✓ 正确 |
| 气泡上方间距 | `Spacer(minLength: bubbleSafeSpacing)` | **无 Spacer** | ✗ 缺失 |
| 气泡下方间距 | `Spacer(minLength: bubbleSafeSpacing)` | `Spacer(32.dp)` | **硬编码 32 vs 36/30** |
| 气泡容器 | `.frame(maxWidth:.infinity, maxHeight:.infinity)` | 无高度约束 Column | ✗ 结构不同 |
| 卡片区域间距 | `VStack(spacing: 18)` | `spacedBy(18.dp)` | ✓ 正确 |

iOS PadPortrait 的气泡区域结构：
```swift
VStack(spacing: 0) {
    Spacer(minLength: bubbleSafeSpacing)    // 36 or 30
    StartReviewButton(...)
    Spacer(minLength: bubbleSafeSpacing)    // 36 or 30
}
.frame(maxWidth: .infinity, maxHeight: .infinity)
```

这是一个 `maxHeight: .infinity` 的 VStack，气泡在其内部通过上下两个等长 Spacer 完美居中。下方的卡片区域在气泡 VStack 之后直接排列。

Android 的实现：
```kotlin
Column(modifier = Modifier.fillMaxWidth(),
       horizontalAlignment = Alignment.CenterHorizontally) {
    KikariaStartBubble(onClick = onStartReview, homeScale = bubbleScale)
}
Spacer(modifier = Modifier.height(32.dp))
```

气泡没有上方 Spacer→不居中。下方不是 `bubbleSafeSpacing`（36 或 30）而是固定 32→与 iOS 不一致。气泡的 "maxHeight: infinity + 双 Spacer 居中" 机制被简化为 "一个固定高度 Spacer + 无上方空间"。在大屏 iPad (1366×1024) 上，气泡被挤在 header 下方约 20dp 处，而非在整个可用垂直空间的中心。

### 气泡视觉效果

| 元素 | iOS 值 | Android 值 | 偏差 |
|------|--------|-----------|------|
| Bubble 1 透明度 | 0.42 | 0.62 | **+48%** |
| Bubble 2 透明度 | 0.32 | 0.56 | **+75%** |
| Bubble 3 透明度 | 0.30 | 0.52 | **+73%** |
| Bubble 4 透明度 | 0.34 | 0.50 | **+47%** |
| 气泡尺寸(px) | 92/80/78/74 | 92/80/78/74 | ✓ 正确 |
| 呼吸动画周期 | 5.4s | 5.4s | ✓ 正确 |
| 公转周期 | 150s | 150s | ✓ 正确 |

四颗卫星气泡在 iOS 中呈现为柔和半透明的材质球，Android 中由于 alpha 被抬高 50-75% 显得更"实心"。

### Landscape 布局

| 元素 | iOS 值 | Android 值 | 偏差 |
|------|--------|-----------|------|
| 左栏宽度 | `metrics.homeLandscapeLeftWidth` | 同 | ✓ |
| 右栏宽度 | `metrics.homeLandscapeRightWidth` | 同 | ✓ |
| 栏间距 | `metrics.homeLandscapeColumnSpacing` | 硬编码 `56.dp` | **不一致** |
| 标题→气泡间距 | `Spacer(minLength: 34)` | `Spacer(34.dp)` | ✓ 正确 |
| 气泡下方间距 | `Spacer(minLength: 34)` | `Spacer(34.dp)` | ✓ 正确 |
| 卡片间距 | `(14 * cardScale).dp` | `(14 * cardScale).dp` | ✓ 正确 |
| Avatar 位置 | `ZStack(alignment: .topTrailing)` 在 ScrollView 外部 | `Box.align(TopEnd)` 在 **ScrollView 内部** | **会随滚动消失** |

iOS landscape 的 avatar 使用了 `ZStack(alignment: .topTrailing)`——avatar 是 fixed overlay，不随 ScrollView 滚动。Android 将 avatar 放在 scrollable Box 内部，滚动时消失。

---

## 三、汇总

### Rokurics Android — 3 个位置/大小偏差

| # | 等级 | 描述 |
|---|------|------|
| 1 | **Critical** | Orb 和导航卡之间缺少 `Spacer(minLength: 20-32)`，两元素紧贴 |
| 2 | High | 导航卡三层 `background` 叠加导致描边变成渐变填充 |
| 3 | Low | 底部胶囊 `blur(0.5.dp)` 污染文字可读性 |

### Kikaria Android — 8 个位置/大小偏差

| # | 等级 | 描述 |
|---|------|------|
| 1 | **Critical** | PadPortrait 气泡无上方 Spacer，不居中。下方 Spacer 固定 32 而非 36/30 |
| 2 | High | 紧凑模式标题上边距 18dp（iOS=14dp），偏移 +29% |
| 3 | High | 紧凑模式 contentAlignment 为 TopCenter（iOS=Center），内容置顶不居中 |
| 4 | High | 四个卫星气泡透明度整体偏高 50-75% |
| 5 | Medium | Landscape avatar 在 ScrollView 内部而非 overlay，滚动后消失 |
| 6 | Medium | Landscape columnSpacing 硬编码 56.dp 而非使用 `metrics.homeLandscapeColumnSpacing` |
| 7 | Medium | 公转动画 (`orbitAngle`) 使用 `LaunchedEffect` + `withFrameMillis` 而非 `animateFloat`，每次更新触发重组 |
| 8 | Low | 径向高光固定在气泡左上角，不随公转位置调整 |
