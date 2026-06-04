# Kikaria Android — 首页像素级布局偏差（仅 Compact 布局）

**日期**: 2026-05-31 | **范围**: 位置、尺寸、间距 | **排除**: 颜色、质感、iPad/横屏布局

---

## 一、气泡区 (StartReviewButton)

### 1.1 容器尺寸错误 — 272×272 vs 272×260（Critical）

| 方向 | iOS | Android | 偏差 |
|------|-----|---------|------|
| 宽度 | `272 * scale` | `272 * scale` | ✓ |
| 高度 | `260 * scale` | `272 * scale` | **+12dp (+4.6%)** |

iOS 的 `StartReviewButton` 定义在 `.frame(width: 272*scale, height: 260*scale)`——不是正方形。Android 的 `Modifier.size(272*scale)` 创建正方形。容器多出 12dp 高度，导致气泡下方比上方多出空间。

### 1.2 四颗卫星气泡位置全部为四正方向而非对角线方向（Critical）

| 气泡 | iOS 偏移 (x, y) | Android 偏移 (x, y) |
|------|----------------|---------------------|
| Bubble 1 (cyan+mint) | **(-96, -68)** — 左上对角线 | (0, -98) — 正上方 |
| Bubble 2 (lavender+mist) | **(102, -56)** — 右上对角线 | (100, 0) — 正右方 |
| Bubble 3 (green+cyan) | **(92, 80)** — 右下对角线 | (0, 98) — 正下方 |
| Bubble 4 (sky+white) | **(-106, 78)** — 左下对角线 | (-100, 0) — 正左方 |

iOS 的四颗气泡沿公转轨道分布在对角线方位，视觉上构成一个倾斜旋转环。Android 的四颗气泡在正上下左右四个方向，构成一个十字形。**轨道视觉完全不同**。

### 1.3 中心圆呼吸幅度不同（High）

| 参数 | iOS | Android | 偏差 |
|------|-----|---------|------|
| 呼吸缩放范围 | `1.012 ↔ 0.996`（摆幅 0.016） | `0.992 ↔ 1.018`（摆幅 0.026） | **+63%** |
| 起始值 | 1.012（先胀大） | 0.992（先缩小） | **相位相反** |
| 垂直呼吸偏移 | `y: 2 ↔ -5` | `y: 2 ↔ -5` | ✓ 一致 |

Android 的中心圆呼吸幅度是 iOS 的 1.6 倍，且初始相位相反——iOS 打开时圆向内收，Android 向外胀。

### 1.4 卫星气泡呼吸幅度远小于 iOS（High）

| 气泡 | iOS 缩放范围 | Android 缩放范围 | 比例 |
|------|------------|----------------|------|
| Bubble 1 | `1.035 ↔ 0.985`（摆幅 0.05） | `0.992 ↔ 1.018`（摆幅 0.026） | **-48%** |
| Bubble 2 | `0.985 ↔ 1.04`（摆幅 0.055） | `1/0.992 ↔ 1/1.018` ≈ 1.008↔0.982（摆幅 0.026） | **-53%** |

iOS 的卫星气泡呼吸幅度约 5-5.5%，有明确的胀缩节奏感。Android 统一使用 `breathe` 值，幅度仅 2.6%，几乎看不出缩放。

### 1.5 中心箭头文字有额外 shadow / 符号差异（Medium）

| | iOS | Android |
|---|-----|---------|
| 中心符号 | `Image(systemName: "arrow.right")` — SF Symbol 箭头 | `"→"` (U+2192) — Unicode 文本箭头 |
| 字号 | `70 * scale` `.font(.system(...))` | `(70 * scale).sp` |
| shadow | `color: deepText.opacity(0.10)`, radius: 8, y: 4 | 无 |

SF Symbol `arrow.right` 有美术设计的粗细渐变和不规则箭头头。"→" 是等宽字符，箭头头小、线条细。视觉效果完全不同。

### 1.6 多余的光晕环 — Android 有，iOS 没有（High）

Android 在公转气泡和中心圆之间插入了一个 `glowSize = (210*scale).dp` 的光晕圆（第 661-677 行）——它位于 Z 序的气泡和中心圆之间，`graphicsLayer { scale = breathe }` 随呼吸同步缩放。

iOS `StartReviewButton` 的 Z 序是：公转气泡 → 中心圆 → overlay 高光 → overlay 描边。**没有光晕环**。

这个额外元素的存在让 Android 气泡整体看起来"大了一圈"——公转环和中心圆之间多了 10dp 的发光过渡。

### 1.7 容器在屏幕上的呼吸偏移方向错误（Low）

| | iOS | Android |
|---|-----|---------|
| 起始 Y 偏移 | `+2 * scale`（向下） | `+2f * scale`（向下） |
| 呼吸振幅 | `2 → -5`（向上移动 7dp） | `2 → -5`（向上移动 7dp） |
| 起始相位 | 向下 2dp | 向下 2dp ✓ 一致 |

---

## 二、卡片区

### 2.1 Progress 卡片 HStack 间距缺失（High）

```swift
// iOS
HStack(alignment: .center, spacing: 14 * scale) { ... }
//                                  ↑ 最小 14dp 间距

// Android (HomeInfoCards 第 772 行)
Row(
    modifier = Modifier.padding(...),
    verticalAlignment = Alignment.CenterVertically
    // ← 没有 horizontalArrangement 参数，默认 Start + 0 间距
) {
    Column(Modifier.weight(1f)) { ... }   // 吞掉所有剩余空间
    progressText
    Spacer(8*scale)
    chevron
}
```

iOS 保证日期栏和进度数字之间最少 14dp。Android 用 `weight(1f)` 把剩余空间全吞了——如果日期文本较短（如 "May 31st"），间距可以到 100+dp；如果日期文本很长，会 0dp 紧贴。两种都不对。

**正确实现**: `Row(horizontalArrangement = Arrangement.SpaceBetween)` 或使用 `Arrangement.spacedBy(14.dp)`。

### 2.2 Progress 卡片内日期和 Days Left 间距 4dp vs 5dp（Low）

| | iOS | Android |
|---|-----|---------|
| dateTitle ↔ daysLeftText 间距 | `VStack(spacing: 5)` | `Spacer(4.dp)` |

差 1dp。

### 2.3 Dashboard Metric 列无 minHeight（Medium）

| | iOS | Android |
|---|-----|---------|
| Metric 列最小高度 | `minHeight: 82 * scale` | 无 |

Android 的 `DashboardMetricColumn` 没有最小高度。当标签文本很短、数值很短时（如 "已掌握" / "0"），列的高度会塌缩，在三列之间产生不均匀的高度。

### 2.4 Dashboard 分隔线高度不缩放（Low）

| | iOS | Android |
|---|-----|---------|
| 分隔线高度 | `42 * scale` | 固定 `42.dp` |

在 PadPortrait 模式下 `scale > 1`（实际 1.05-1.24），iOS 的分隔线会按比例缩放而 Android 不。但按用户要求只审计 Compact 布局，此时 scale=1，这个偏差不触发。

### 2.5 Preset 行 chevron 字号 14sp vs 12sp（Low）

| | iOS | Android |
|---|-----|---------|
| Preset 行 chevron | `12 * scale` 通过 `.font(.system(...))` | `14.sp` 硬编码 |

差 2sp。按用户要求只审计位置和大小。

### 2.6 Preset 行无 minHeight（Medium）

| | iOS | Android |
|---|-----|---------|
| Preset 行最小高度 | `minHeight: 56 * scale` | 无 `.heightIn(min = ...)` |

与 Metric 列相同的问题——当预设名为空或很短时，该行高度塌缩。

---

## 三、汇总

按位置/大小影响的优先级排序：

| # | 等级 | 问题 |
|---|------|------|
| P0 | **布局错误** | 四颗卫星气泡在四正方向（十字形）而非 iOS 的对角线位置 |
| P1 | **尺寸错误** | 气泡容器 272×272 应为 272×260 |
| P2 | **多余元素** | 公转环和中心圆之间有光晕环（iOS 没有） |
| P3 | **振幅错误** | 卫星气泡呼吸幅度仅为 iOS 的 48-53% |
| P4 | **振幅+相位错误** | 中心圆呼吸幅度 1.6 倍于 iOS，且相位相反 |
| P5 | **间距缺失** | Progress 卡片日期区与数字之间无 min 间距 |
| P6 | **符号差异** | "→" vs SF Symbol arrow.right — 视觉宽度和粗细不同 |
| P7 | **minHeight 缺失** | Dashboard Metric 列和 Preset 行缺少 minHeight |
| P8 | **字号差异** | Preset 行 chevron 14sp vs 12sp |
| P9 | **间距差异** | Progress 卡片 inner VStack spacing 4dp vs 5dp |
