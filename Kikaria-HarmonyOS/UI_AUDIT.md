# Kikaria HarmonyOS — UI 审计报告

**日期**: 2026-05-31 | **方法**: 只读源代码审查 | **范围**: 全部 16 页面 + 组件 + 主题 + 布局系统

---

## 1. 严重问题（Critical）

### 1.1 ReviewPage — `advanceOrFinish` 用 setTimeout 延时刷新引发竞态

**位置**: `ReviewPage.ets` 第 336-344 行

```typescript
advanceOrFinish(): void {
    if (this.hasNext) {
        appState.nextPoint()
        setTimeout(() => {
            this.refreshFromState()
        }, 200)
    } else {
        navPathStack.pop()
    }
}
```

`appState.nextPoint()` 更新内部状态后，`refreshFromState()` 被延迟 200ms 执行。在这 200ms 间隔内，`appState.currentPoint` 已经指向下一个知识点，但 UI 仍然显示旧数据。

**后果**: 用户快速连续点击"下一个"时（点击速度快于 200ms），每次点击都会调用 `appState.nextPoint()`（跳过知识点），然后旧的 `setTimeout` 回调读取已经被跳过的 `appState.currentPoint`。

**修复**: 去掉 `setTimeout`，改为直接调用 `this.refreshFromState()`。如果 `nextPoint()` 有异步副作用（如存储 IO），用 `await` 而非 `setTimeout`。

---

### 1.2 SettingsPage — 单栏和双栏模式下所有设置部分重复两次

**位置**: `SettingsPage.ets` 第 263-445 行

`build()` 方法中有两个巨大的 if-else 分支（`useTwoColumn` vs 单栏），每个分支包含完整的"每日目标 + 预警阈值 + 倒计时 + 通知 + 外观 + 关于"部分。两段代码几乎完全相同（~120 行重复），仅在 profile 区域有差异。

**后果**: 修改任何一个设置项（如添加新的 Stepper 或调整文案）必须改两处。当前两段代码的细微不一致（如倒计时卡片在双栏模式少了 `SettingsInfoTextRow`）会导致不同布局下的体验差异。

**修复**: 提取 `settingsContent()` Builder 方法复用。

---

### 1.3 InitialProfileSetupPage — 硬编码初始用户名检查

**位置**: `InitialProfileSetupPage.ets` 第 33-34 行

```typescript
this.displayName = p.displayName === 'Vita' ? '' : p.displayName
this.userHandle = p.userHandle === 'vita_0818' ? '' : p.userHandle
```

与 Kikaria Android 相同的问题——`"Vita"` 和 `"vita_0818"` 硬编码到生产代码。已确认这是跨平台存在的测试数据泄漏。

**修复**: 去掉这两行特殊检查，仅检查 `isEmpty()`。

---

## 2. 高严重度（High）

### 2.1 ReviewPage — 单栏模式下阅读列完全重复 twoColumn 的逻辑

**位置**: `ReviewPage.ets` 第 277-314 行

`readingColumn()` Builder（第 136 行）定义了标签、标题、提示/答案按钮。但单栏布局（第 277-314 行）**直接内联重复了 `readingColumn` 的全部逻辑**——包括标签渲染、标题、提示按钮、答案按钮。没有复用 `readingColumn()`。

**后果**: 如果修改阅读列的任何 UI 元素（如添加标签颜色、修改标题字体大小），只改了 `readingColumn()` 而忘记同步修改内联代码，两个布局会出现不一致。

---

### 2.2 ScopeSelectionPage — 标签选择后不保存状态即返回

**位置**: `ScopeSelectionPage.ets` 第 98-103 行、第 130-134 行

用户点击标签时 `selectedTags` 集合在组件本地更新，但直到用户点击底部的"应用并开始复习"按钮时才调用 `appState.selectedTags = new Set(this.selectedTags)` 并 `saveAppState()`。

**后果**: 用户选择标签后直接按系统返回键退出页面——所有选择丢失。底部按钮的文案"应用并开始复习"暗示用户完成选择后必须点击按钮，但这与 iOS/Android 的即时生效模式不一致。

**修复**: 在 `aboutToDisappear` 中自动保存选择结果，或改为每次点击标签立即保存。

---

### 2.3 Homepage (Index) — 气泡动画使用 ArkUI `.animation()` 但 scale 目标值不明确

**位置**: `Index.ets` 第 180-195 行

```typescript
.scale({ x: scale, y: scale })
.animation({
    duration: 5400, curve: Curve.EaseInOut,
    iterations: -1, playMode: PlayMode.Alternate
})
```

`scale` 属性通过 `{ x: scale, y: scale }` 绑定到 `@State bubbleScale: number = 1.0`，但 `.animation()` 的 `PlayMode.Alternate` 会交替应用 1.0 和 `bubbleScale`（0.94）。问题在于：动画声明依赖于 `bubbleScale` 作为属性值，而 `PlayMode.Alternate` 期望的是"from → to"两个不同的值——当前 from 和 to 都是 `bubbleScale`，ARK UI 的 Alternate 行为取决于属性本身是否有显式变化。

**后果**: 气泡的呼吸动画效果在 HarmonyOS 的不同 API 版本间可能不一致——某些版本会正确缩放，某些版本可能不做任何动画（因为起始值和目标值相同）。

**修复**: 显式指定 from 0.94 / to 1.06 的 animateTo 或使用两个 `@State` 值切换。

---

### 2.4 SettingsPage Toggle — 暗色模式 Toggle 的 onChange 不更新 UI

**位置**: `SettingsPage.ets` 第 331-332 行

```typescript
SettingsToggleRow({
    label: '暗色模式', isOn: this.isDark,
    onChange: (_value: boolean) => { appState.toggleDarkMode() }
})
```

`onChange` 调用了 `appState.toggleDarkMode()`，这会更新 `AppStorage.setAndRef` 中的全局暗色模式标志。但 `SettingsToggleRow` 依赖传入的 `isOn` 作为 `@Link`——组件内部的 `Toggle` 的 `isOn` 变化不会自动同步到这个 `@Link`。需要 `onChange` 回调中同时更新本地状态。

**后果**: 用户点击暗色模式开关后，`appState` 中的全局标志更新了（页面背景色变了），但 Toggle 开关本身可能不反映新状态（因为 `this.isDark` 只从 `@StorageLink` 读取初始值）。

---

## 3. 中严重度（Medium）

### 3.1 ReinforcementPage 和 MasteredPage 代码 95% 相同

两个页面的结构几乎完全一致：标题行、空状态、起始按钮、List + ForEach + buildPointCard。差异仅是：图标（★ vs ✓）、颜色（NEXT_AMBER vs MASTERED_GREEN）、操作（toggleReinforcement vs toggleMastered）、详情展开标签（"移出重点" vs "取消掌握"）。

**后果**: 任何修改必须同步两处。总计 ~460 行重复代码。

**修复**: 提取泛型 `PointListPage` 组件，参数化 `title`、`icon`、`color`、`emptyMessage`、`toggleMethod`。

---

### 3.2 所有页面使用 `@Entry @Component struct` 双模式导致组件实例冗余

每个页面同时定义为 `@Builder export function XxxPageBuilder()` 和 `@Entry @Component struct XxxPage`。`XxxPageBuilder` 是这个 `Builder` 只创建 `XxxPageContent`。`XxxPageContent` 才是真正的页面内容。`Index.ets` 的 `navDestinationRouter` 通过 name 分发到 Builder。

这种三层模式是为了兼容 `Navigation + NavDestination` 的双路由方式，但导致每个页面多了一层不必要的 `@Entry` 组件。

**后果**: 架构复杂度增加，每个页面有三种结构体身份。

---

### 3.3 TodayOverviewPage — 进度条宽度使用内联计算

**位置**: `TodayOverviewPage.ets` 第 94 行

```typescript
Row()
    .width(`${Math.min(100, this.todayMastered / Math.max(1, this.dailyGoal) * 100)}%`)
```

使用字符串插值 `'...%'` 设置 `width`。ArkUI 的 `.width()` 接受字符串百分比或数值 vp——`'58%'` 是合法的。但 `Math.min(100, ...)` 的结果加上 `%` 后缀，如果计算结果是浮点数（如 58.333），会渲染为 `'58.333%'`，这在某些 ArkUI 版本中可能不精确。

**修复**: `width(${Math.round(percent)}%)` 取整。

---

### 3.4 LiquidGlassCard 使用 `backdropBlur`——这是好的，但透明度数值与 iOS 不对齐

**位置**: `KikariaComponents.ets` 第 47 行

```typescript
.backgroundColor(KikariaColors.GLASS_SURFACE + (isDarkModeEnabled() ? '33' : '7A'))
.backdropBlur(24)
```

`backdropBlur` 是 HarmonyOS 的真正背景模糊 API，比 Android 版的纯色填充强得多。但透明度值 `'33'`（暗色 20%）和 `'7A'`（浅色 48%）与 iOS 的暗色 `fillOpacity * 0.78`（约 37%，上限 36%）有差距——暗色模式下玻璃表面透明度偏低，卡片与背景区分度不够。

**修复**: 暗色提高到 `'4D'`（30%）或 `'5C'`（36%）。

---

### 3.5 Index 首页的 `this.bubbleScale = 0.94` 在 `aboutToAppear` 中设置——首次渲染时气泡为 1.0

**位置**: `Index.ets` 第 60 行

`this.bubbleScale = 0.94` 在 `aboutToAppear` 中设置，但 `aboutToAppear` 在 `.animation()` 初始化之后执行。引擎可能需要一帧的重新测量才能将 scale 从初始值 1.0 过渡到 0.94——这意味着首次展示时气泡有一个瞬间的 1.0 → 0.94 跳变。

---

## 4. 低严重度（Low）

### 4.1 ReviewPage 提示/答案的 emoji 前缀（💡、📖）使用 Unicode 字符

**位置**: `ReviewPage.ets` 第 163、169 行

与 Android 版 Rokurics 类似——emoji 在不同 OEM 设备上渲染不一致。

### 4.2 返回按钮使用纯文本 `← 返回`

**位置**: 所有页面

统一的 `Text('← 返回')` Button 风格，没有玻璃圆形样式。在 HarmonyOS 桌面模式下与 Material 风格的返回体验不同。

### 4.3 TodayOverviewPage 活动列表中 `border({ width: { bottom: 1 } })` 作为分隔线

**位置**: `TodayOverviewPage.ets` 第 226 行

使用 border-bottom 而非 `Divider()` 组件作为列表项分隔线。轻微不一致——其他页面使用 `Divider()` 或 `SettingsSectionDivider`。

### 4.4 OnboardingPage 无滑动切换——只能通过按钮翻页

**位置**: `OnboardingPage.ets` 第 58-123 行

iOS 和 Android 版的 Onboarding 支持滑动手势切换页面。HarmonyOS 版仅通过按钮翻页。`currentPage` 的切换没有动画过渡。

---

## 5. 正面发现

以下是 Kikaria HarmonyOS 中做得特别好的方面：

1. **真正的暗色/浅色双模式** — `adaptive(light, dark)` + `AppStorage.ref(DARK_MODE_KEY)` 的设计比 Rokurics HarmonyOS 的硬编码暗色强了一个量级。全局切换后所有页面动态响应，包括 gradient 渐变色的双值切换。这是五个审计项目中**唯一正确实现了动态双模式的 ArkUI 项目**。

2. **`backdropBlur` 真毛玻璃** — `LiquidGlassCard` 使用 HarmonyOS 原生的 `backdropBlur(24)`，在支持的设备上实现了真正的背景模糊毛玻璃效果。优于 Android 版的纯色填充。

3. **`KikariaAdaptiveLayout` 完整的响应式系统** — 346 行纯函数提供 width category、padding、scale factor、two-column layout constants，完全匹配 iOS 版 Kikaria 的 `KikariaAdaptiveLayout.Metrics`。三个 Breakpoint（compact/regularPad/widePad）+ iPad 竖屏/横屏差异化 + 页面级 maxWidth + 独立的 scale per page type。

4. **SettingsPage 的 Picker 交互** — 日期选择器和时间选择器使用内联 Stack overlay 而非弹窗，带半透明遮罩 + 毛玻璃卡片 + 取消/确定按钮。交互清晰、视觉统一。

5. **`KikariaComponents.ets` 的组件库体系** — `LiquidGlassCard`、`SettingsSectionCard`、`SettingsListRow`、`SettingsToggleRow`、`SettingsStepperRow` 等设置组件与前三个平台相比是最完整的。

6. **`Navigation + NavDestination` 路由模式** — 使用 ArkUI 原生的 Navigation 组件管理页面栈，每个子页面是 `NavDestination`。比手动管理 `router.pushUrl` 更规范。

7. **首页三种布局** — `singleColumnLayout`、`padPortraitLayout`、`twoColumnLayout` 覆盖手机/平板竖屏/平板横屏三种场景，与 Kikaria Android 的设计一致。

---

## 6. 总结

| 严重度 | 数量 | 关键项 |
|--------|------|--------|
| Critical | 3 | setTimeout 竞态、Settings 双栏代码重复、硬编码初始用户名 |
| High | 4 | Review 单栏重复 readingColumn、Scope 不保存即返回、气泡动画值歧义、暗色 Toggle 状态不同步 |
| Medium | 5 | Reinforcement/Mastered 重复、路由三层模式冗余、进度条精度、玻璃透明度、气泡初始跳变 |
| Low | 4 | emoji 前缀、返回按钮样式、border 分隔线、Onboarding 无滑动 |

Kikaria HarmonyOS 的整体 UI 质量在五个审计项目中属于中上水平。与同一团队的 Rokurics HarmonyOS（硬编码暗色模式，Critical 级）相比，Kikaria 的 `adaptive()` 双模式颜色系统是显著进步。主要问题集中在代码重复（ReviewPage 阅读列、SettingsPage 双栏、Reinforcement/Mastered 几乎相同）和少量逻辑 bug（setTimeout 竞态、Toggle 状态同步），没有阻塞级布局 bug。
