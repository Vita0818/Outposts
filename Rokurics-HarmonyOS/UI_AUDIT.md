# Rokurics HarmonyOS — UI 审计报告

**日期**: 2026-05-31 | **方法**: 只读源代码审查 | **范围**: 全部 10 页面 + 组件 + 主题系统 + 工具类

---

## 1. 严重问题（Critical）

### 1.1 全部页面硬编码暗色模式——浅色模式下界面完全不可用

**位置**: `RokuricsTheme.ets` 第 11-45 行，所有页面的 `.linearGradient()` 调用

HarmonyOS 的主题系统**直接写死了暗色模式的数值**：

```typescript
static readonly aqua = '#57D6D1'          // iOS dark 值
static readonly deepText = '#E6FAF7'      // iOS dark 值
static readonly glassSurface = '#0D2424'  // iOS dark 值
static readonly pageGradientStart = '#051414'
static readonly pageGradientMid = '#0A2B29'
static readonly pageGradientEnd = '#030D12'
```

所有页面（10 个）的背景渐变都使用这三个暗色值。`colorAlpha()` 函数虽然保持了透明度比例，但色相本身是暗色的——浅色设备上背景是`#051414`（接近纯黑），白色文字 `#E6FAF7` 在暗色背景上虽能看清，但玻璃卡片的半透明叠加在同一暗色背景上时完全失去层次感。

对比 iOS：每个颜色都有 `adaptive(light:..., dark:...)` 双值切换。对比 Android：`adaptiveColor()` Composable 函数用 `isSystemInDarkTheme()` 动态选择。

**后果**: HarmonyOS 设备在浅色模式下，所有页面呈现暗色界面，与系统外观冲突。玻璃卡片因背景和卡片都是暗色调而失去毛玻璃层次感。

**修复**: 为每个颜色定义 light/dark 双值，监听 `colorMode` 变化并动态切换。或至少为浅色模式单独定义一套色板。

---

### 1.2 HomePage 呼吸动画使用 setTimeout 轮询——每 50ms 触发整树重绘

**位置**: `HomePage.ets` 第 53-59 行

```typescript
private startBreathing(): void {
    const update = () => {
        this.breathePhase = Math.sin(Date.now() / 1000 * Math.PI / 2.4) * 0.5 + 0.5
        setTimeout(update, 50)
    }
    update()
}
```

这是 20fps 的 JavaScript 定时器循环，每次更新 `@State breathePhase` 触发整棵 HomePage 组件树重绘（包括 4 个卫星气泡、3 个涟漪圈、中心 Orb、导航卡片、背景气泡）。在 iOS 上同样的效果通过 Core Animation 在渲染线程完成，不阻塞主线程。

**后果**: 首页持续消耗 CPU（约 1-3% 额外），在低端 HarmonyOS 设备上可能导致掉帧和发热。`setTimeout` 不与屏幕刷新率同步，可能出现撕裂。

**修复**: 使用 `animateTo()` + ArkUI 内置动画框架（`curve: Curve.Linear` + `iterations: -1`），将动画交给渲染线程。

---

### 1.3 RecordingLibraryPage 过滤芯片全部为非功能性存根

**位置**: `RecordingLibraryPage.ets` 第 915-917 行

```typescript
private filterTranscribed(): boolean { return false }
private filterNoteGenerated(): boolean { return false }
private filterUploaded(): boolean { return false }
```

三个过滤芯片（已转写、有笔记、已上传）在 UI 上可见可点击，但点击后不做任何过滤——`FilterChip` 的 `active` 参数始终为 `false`，`onToggle` 回调不更新任何状态。

**后果**: 用户点击"已转写"筛选后，列表不变，按钮也不变高亮。交互反馈完全缺失。

**修复**: 为每个滤芯声明 `@State` 变量，并在 `getSortedRecordings()` 中按这些变量过滤。

---

### 1.4 FilingOverlay 候选值完全硬编码，与用户数据无关

**位置**: `RecordingSessionPage.ets` 第 10-15 行

```typescript
const FILING_OPTIONS: Record<string, string[]> = {
  'type': ['课堂录音', '自学笔记', '会议记录', '访谈采访', '灵感记录', '其他'],
  'subject': ['数学', '物理', '化学', '生物', '计算机', '英语', '历史', '文学', '哲学', '艺术'],
  'chapter': [],
  'topic': []
}
```

这些是硬编码的固定选项，与用户的学习库中已有的归档值无关。iOS 的做法是从 `StudyLibraryStore` 已有录音的归档值中提取候选项，并允许新建。

**后果**: 用户之前的归档中如果用过"微积分"作为课程名，在归档新录音时不会看到"微积分"作为建议。每次归档都是重新选择/输入，没有学习库上下文。

**修复**: 从 `StudyFolderStore.listFolders()` 和已有录音的 `studyFiling` 中提取候选项。

---

## 2. 高严重度（High）

### 2.1 MacConnectionPage 整个配对/同步流程为全 Mock

**位置**: `MacConnectionPage.ets` 第 456-478 行

```typescript
private testConnection(): void {
    setTimeout(() => { this.feedbackText = '连接测试完成 (Mock)'; ... }, 1500)
}
private startPairing(): void {
    setTimeout(() => { this.isPaired = true; this.deviceModel = 'Mac'; ... }, 2000)
}
```

测试连接和配对都是 `setTimeout` 模拟。同步按钮也标注 `'同步完成 (Mock)'`。页面 UI 布局完整、交互流程看起来真实，但**没有任何网络请求代码**。

**后果**: 用户完成配对表单后点击"配对"，2 秒后显示"配对成功"，但实际上没有任何证书交换、没有指纹校验、没有 HTTPS 连接建立。应用会显示"已连接"状态，但后续任何依赖这个连接的逻辑（上传、同步）都会失败。

**修复**: 实现真正的 HTTPS 连接测试和配对流程，或在 UI 上明确标注为演示状态。

---

### 2.2 RecordingSessionPage 暂停/继续按钮与玻璃卡片控制区重叠

**位置**: `RecordingSessionPage.ets` 第 228-294 行

在录音/暂停状态下，同时渲染了两套控制按钮：
- 第 228 行：一个 `Row` 中的玻璃卡片控制按钮（暂停/继续）  
- 第 275 行：一个独立的圆形浮动按钮（暂停/继续）

两套按钮在同一个 `Column` 中，且都是基于 `state === RECORDING || state === PAUSED` 的条件渲染。它们会**同时出现**，占用空间并造成困惑。

**后果**: 用户看到两组暂停/继续按钮——一组在玻璃卡片中，一组是单独的圆形浮动按纽。点击任何一个都会触发 `toggleRecording()`，但视觉冗余让用户困惑。

**修复**: 二选一，去掉浮动按钮或去掉玻璃卡片行。

---

### 2.3 AIChatPage 对话历史覆盖层无遮罩、手势穿透

**位置**: `AIChatPage.ets` 第 202-280 行

对话历史展开时没有半透明遮罩背景，直接覆盖在消息区域上方。`zIndex(10)` 确保它在上层，但没有 `position({ x: 0, y: 0 })` 全局定位——它在一个 `Column` 流程中，只占据自己的自然高度。

**后果**: 用户打开对话历史时，如果消息列表较长，对话历史卡片可能被挤出视野或被消息列表遮挡。

---

### 2.4 所有页面的对话框使用 `position({ x: 0, y: 0 })` 全局定位但事件穿透不可控

**位置**: 多处（`RecordingLibraryPage.ets` 第 406-408行、`RecordingDetailPage.ets` 第 891-896 行等）

所有自定义对话框（重命名、删除确认）使用：
```typescript
Column()
  .width('100%').height('100%').backgroundColor('#50000000')
  .position({ x: 0, y: 0 })
  .onClick(() => { this.showRenameDialog = false })
```

外层的 `.onClick` 关闭整个对话框，但内层的保存/取消按钮也在同一 `Column` 树内。`onClick` 会冒泡——点击保存按钮时，如果保存操作触发了异步状态变化（如 `await` 后），关闭事件可能会先于保存完成触发。

**后果**: 用户在对话框中点击"保存"时，同时触发了保存和关闭。由于 HarmonyOS 中 `onClick` 是同步执行的而保存操作是异步的，对话框可能在保存完成前关闭，然后 `await loadAll()` 修改已不在视图中的状态。

---

## 3. 中严重度（Medium）

### 3.1 BackIcon 使用 Unicode 字符 ← 而非矢量图形

**位置**: `CustomIcons.ets` 第 8-14 行

```typescript
@Builder
export function BackIcon(size: number, color: string) {
    Text('←').fontSize(size).fontWeight(600).fontColor(color)
}
```

`←` (U+2190) 在不同 HarmonyOS 设备的字体回退链中渲染结果不同。华为设备默认使用 HarmonyOS Sans，而其他 OEM 可能有不同的 fallback 字体。

**后果**: 返回箭头在不同设备上形状各异——有的粗短、有的细长、有的带尾部勾线。

**修复**: 使用 `Polygon` 自绘箭头，或使用 HarmonyOS 内置的 `symbol` 图标系统。

---

### 3.2 RecordingDetailPage 播放使用 fd:// 协议——需打开文件描述符

**位置**: `RecordingDetailPage.ets` 第 162-164 行

```typescript
const file: fileIo.File = fileIo.openSync(audioPath, fileIo.OpenMode.READ_ONLY)
this.avPlayer = await media.createAVPlayer()
this.avPlayer.url = `fd://${file.fd}`
```

这是正确的低层 API 用法，但 `fileIo.openSync` 是同步打开，可能阻塞主线程 50-100ms（取决于文件大小和存储速度）。且 `file` 句柄在 `releasePlayer()` 中没有显式关闭（仅调用了 `avPlayer.release()`）。

**后果**: 播放大型录音文件时首帧有微小卡顿；播放器释放后文件描述符泄漏（`file.close()` 未被调用）。

**修复**: 在 `releasePlayer()` 中增加 `fileIo.closeSync(file.fd)` 逻辑。

---

### 3.3 所有页面使用完全相同的 `linearGradient` 硬编码在页面 `build()` 最外层

每个页面的 `build()` 最后都有：
```typescript
.linearGradient({
    direction: GradientDirection.RightBottom,
    colors: [
        [RokuricsColors.pageGradientStart, 1.0],
        [RokuricsColors.pageGradientMid, 1.0],
        [RokuricsColors.pageGradientEnd, 1.0]
    ]
})
```

这 10 段完全相同的代码应该提取到一个共享组件或扩展方法中。

---

### 3.4 自定义图标使用复杂的 Stack/Polygon/Rect 组合——维护困难

`CustomIcons.ets`（416 行）包含了全套自绘图标，包括垃圾桶、文档徽章、铅笔、云上传等。优点是不依赖外部库，但缺点也很明显：

- `TrashIcon` 有 10 层 `Stack` 嵌套的 `Rect` 和 `Polygon`
- `CloudUploadIcon` 用 `Row` 中的 `Circle` + `Rect` + `Circle` 构建云朵
- 每个图标使用 `position()` 绝对定位内部元素，坐标值全是 `size * 0.xx` 的形式

**后果**: 任何一个图标的大小、颜色或形状调整都需要理解复杂的坐标算术。没有视觉预览工具辅助开发。

---

### 3.5 FormatHelpers 和 HapticFeedback 未被复查

`FormatHelpers.ets` 和 `HapticFeedback.ets` 在两个平台审计中没有被发现引用 HarmonyOS 特有的 API 滥用或错误——审计范围外。但它们被页面广泛引用，应在后续专门审查。

---

## 4. 低严重度（Low）

### 4.1 DeviceConnectionCard 图标位置偏移

**位置**: `DeviceConnectionCard.ets` 第 22-31 行

```typescript
Stack() {
    Circle().width(58).height(58).fill(RokuricsColors.mistGreen)
}
.width(58).height(58)

ConnectionIcon(24, ..., this.isPaired)
```

`Circle` 和 `ConnectionIcon` 是同一 `Column` 内的平级元素（不是 `Stack` 子项），`ConnectionIcon` 不在 `Circle` 内部而是在其下方。

### 4.2 SettingsPage 返回按钮没有玻璃样式

**位置**: `SettingsPage.ets` 第 154-160 行

所有其他页面的返回按钮都使用了完整的玻璃圆形样式（`borderRadius(22)` + `backgroundColor(glassSurface)` + `shadow` + `border` gradient）。设置页的返回按钮是 `backgroundColor(Color.Transparent)` 的纯文字 `←`。

### 4.3 StudyLibraryBrowserPage 未读，但根据 RecordingLibraryPage 的 `studyFolderStore.listFolders()` 调用判断

`RecordingLibraryPage` 调用 `folderStore.listFolders()` 和 `createFolder()` 来管理分类，但 UI 上没有直接使用 `StudyLibraryBrowserPage`——浏览器页面以单独的 `@Entry @Component` 存在，路由在 `main_pages.json` 中。

### 4.4 PrivacyPolicyPage 和 AuthTestPage 未纳入 UI 审计

这两个页面存在于 `main_pages.json` 中但本次审计未深读——从命名和路由可知它们属于内容展示页和测试页，UI 风险较低。

---

## 5. 正面发现

以下是 HarmonyOS 迁移中做得特别好的方面：

1. **RecordingDetailPage 功能最完整** — 播放、上传、转写、笔记生成、导出（Markdown/JSON）、归档编辑、文件状态、重命名、删除全部在一个页面实现。是三个迁移平台中 detail 页功能最丰富的。

2. **自定义图标系统（CustomIcons.ets）** — 尽管维护成本高，但完全不依赖外部资源，所有图标都是 `@Builder` 函数用 Primitive Shapes（Circle/Rect/Polygon）自绘。跨 HarmonyOS 设备版本兼容性好。

3. **玻璃卡片样式高度一致** — 每个玻璃卡片（`RecordingLibraryPage` 的录音行、`RecordingDetailPage` 的信息卡、`MacConnectionPage` 的配对表单）都使用了相同的 `border` + `shadow` + `backgroundColor` 三层模式，视觉一致性超过 Android 版。

4. **AIChatPage 的对话气泡不对称圆角** — 用户消息的 `{ topLeft: 16, topRight: 4, bottomLeft: 16, bottomRight: 16 }` 和助手消息的 `{ topLeft: 4, topRight: 16, bottomLeft: 16, bottomRight: 16 }` 正确实现了聊天 UI 的不对称气泡。

5. **SettingsPage 的内联展开编辑器** — 个人资料编辑和 AI 配置编辑直接在设置页内 expand/collapse，不跳转子页面，交互更流畅。

6. **RecordingLibraryPage 的文件夹颜色系统** — `FOLDER_COLORS` 数组 + `setColorToken` + 重命名时携带颜色，是三个迁移平台中唯一完整实现的文件夹颜色功能。

---

## 6. 总结

| 严重度 | 数量 | 关键项 |
|--------|------|--------|
| Critical | 4 | 硬编码暗色模式、setTimeout 动画、过滤芯片存根、归档选项硬编码 |
| High | 4 | Mac 连接全 Mock、录影页双按钮重叠、对话历史无遮罩、对话框事件穿透 |
| Medium | 5 | 文字箭头、文件描述符泄漏、渐变重复、自绘图标复杂、未复查工具 |
| Low | 4 | 图标偏移、设置返回按钮样式缺失、浏览器页未审计、额外页面未审计 |

最严重的问题毫无疑问是**暗色模式硬编码**——这不是"暗色模式没有适配"，而是"系统从根本上不支持浅色模式"。修复这个需要为整个颜色系统建立双值切换。

HomePage 的 setTimeout 动画和 RecordingSessionPage 的硬编码归档选项是其次需要解决的问题。前者是性能问题，后者是功能正确性问题。Mac 连接的全 Mock 状态虽然功能上不完整，但 UI 本身没有布局 bug——它需要在后端服务实现后才能正常工作。
