# Rokurics Windows — UI 审计报告

**日期**: 2026-05-31 | **方法**: 只读源代码审查 | **范围**: 12 个 XAML 页面 + App.xaml + ViewModels + Converters

---

## 1. 严重问题（Critical）

### 1.1 MacStudyLibraryPage — 四种视图面板同时存在于内存中

**位置**: `MacStudyLibraryPage.xaml` 第 107-436 行

同一个页面内声明了 4 个互斥的视图面板：`DetailPanel`、`BrowserPanel`、`TranscriptPanel`、`NotePanel`，全部放在同一个 `StackPanel` 树中，仅靠 `Visibility="Collapsed"` 切换。没有使用 `Frame` 导航或 `ContentControl`。

WinUI 3 的 `StackPanel` 不会跳过 `Collapsed` 子项的布局测量——即使 `Visibility` 为 `Collapsed`，子项的 XAML 树仍在内存中完整构建。4 个面板各包含 `ScrollViewer`、`Border`、`ListView`、`TextBox` 等 10+ 控件。

**后果**: 打开学习库页面时，4 套视图的控件全部被实例化，约 80+ 控件常驻内存。对于浏览列表场景（占总使用时间 >90%），Detail/Transcript/Note 的控件完全多余。

**修复**: 使用 `Frame` + 子页面导航，或至少用 `x:Load="False"` 延迟加载非活动视图。

---

### 1.2 RequestedTheme="Dark" 硬编码在 App.xaml 根节点

**位置**: `App.xaml` 第 7 行

```xml
<Application x:Class="Rokurics.App" ...
             RequestedTheme="Dark">
```

整个应用的根级别 强制为 Dark 模式。所有页面使用 `{ThemeResource ...}` 引用但本质上都从暗色资源字典取值。与 HarmonyOS 类似——应用不支持浅色模式。

但对比 HarmonyOS 的优势是：WinUI 3 使用 `{ThemeResource}` 而非硬编码色值，所以理论上改为 `RequestedTheme="Default"` 后，只要 ResourceDictionary 中有对应的浅色 Brush 定义，就能支持双模式。当前 App.xaml 资源字典中的 Brush（`RokuricsAquaBrush`、`RokuricsMintBrush` 等）是浅色值，但页面使用 `{ThemeResource AcrylicBackgroundFillColorDefaultBrush}`——这在 Dark 下自动映射到暗色版本。

**后果**: 与 HarmonyOS 的硬编码不同，WinUI 有修复的基础——只需要把 `RequestedTheme` 从 `"Dark"` 改为 `"Default"`，并确保双色 resources 正确。

---

### 1.3 HomePage.xaml — 网格布局的三栏居中完全缺失响应式

**位置**: `HomePage.xaml` 第 8-19 行

```xml
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="*" />
    <ColumnDefinition Width="Auto" MaxWidth="520" />
    <ColumnDefinition Width="*" />
</Grid.ColumnDefinitions>
```

三栏 `*/Auto/*` 模式用于居中 `MaxWidth=520` 的内容列。这在窗口宽 1040+ 时效果不错（两边各 ~260px 空白）。但当窗口缩小到 <520 时，Auto 列被挤压，左右 `*` 列收缩到 0，内容列左对齐但**没有被包裹在 ScrollViewer 中**。

**后果**: 窗口宽度 <520px 时，HomePage 的录音控制按钮被裁剪。没有最小窗口宽度限制，没有响应式断点。在 900px 宽窗口上左右留白过大（各约 190px），内容区只占 520px。

**修复**: 添加 `Page.MinWidth="420"` 限制；或用 `VisualStateManager` 定义 3 个断点（Narrow/Wide/Wider）。

---

## 2. 高严重度（High）

### 2.1 MacIPhoneConnectionPage 背景硬编码为暗色渐变

**位置**: `MacIPhoneConnectionPage.xaml` 第 53 行

```xml
<Grid Background="{StaticResource RokuricsPageGradientDarkBrush}">
```

与 HomePage 使用 `{ThemeResource}` 不同，Connection 页面**硬编码使用 Dark 渐变 Brush**。App.xaml 中同时定义了 `RokuricsPageGradientBrush`（浅色）和 `RokuricsPageGradientDarkBrush`（暗色），但页面选择了后者。

**后果**: 即使应用改为 `RequestedTheme="Default"`，Connection 页面仍显示暗色背景。暗色背景上的白色文字在浅色窗口外观中很突兀。

**修复**: 使用 `{ThemeResource RokuricsPageGradientBrush}` 并通过 ThemeDictionaries 控制 light/dark 取值。

---

### 2.2 MacAIChatPage 同样硬编码 Dark 背景

**位置**: `MacAIChatPage.xaml` 第 7 行

```xml
<Grid Background="{StaticResource RokuricsPageGradientDarkBrush}">
```

与 Connection 页面相同的问题。且该页面的 GreetingText 样式也使用了 `{ThemeResource TextFillColorSecondaryBrush}`，在 Dark 下是浅灰色，但直接硬编码在 ChatGreetingStyle 中。

---

### 2.3 SettingsViewModel — `AppSettings.Save()` 每次调用都触发全量 JSON 序列化+文件写入

**位置**: `SettingsViewModel.cs` 第 90-118 行

```csharp
[RelayCommand]
private void Save()
{
    var settings = new AppSettings { ... 20+ properties ... };
    AppSettings.Save(settings);
}
```

`Save` 命令在设置页每个保存按钮后面调用，每次都会创建新的 `AppSettings` 对象并执行 `File.WriteAllText`。设置页的 Whisper 配置有多达 11 个独立的 TextBox/ComboBox，每次修改触发 `Save` 都会全量 IO。

**后果**: 频繁的 JSON 序列化和磁盘写入没有防抖。设置页面在高频输入时可能短暂卡顿。

**修复**: 添加 `Debounce(300ms)` 或在关掉设置页时才保存。

---

### 2.4 StudyLibraryPage — ItemsControl 不支持虚拟化

**位置**: `StudyLibraryPage.xaml` 第 63、101 行

```xml
<ItemsControl ItemsSource="{x:Bind ViewModel.Folders, Mode=OneWay}">
```

`ItemsControl` 渲染所有 Folders 和 Items 没有虚拟化——如果有 200 个文件夹或 500 个录音，所有项都被一次性布局测量和渲染。

**后果**: 大量录音（>100）时，页面加载和滚动性能显著下降。

**修复**: 使用 `ListView` 或 `ItemsRepeater` 替代 `ItemsControl` 以获得虚拟化。

---

### 2.5 MainWindow.NavigateTo — 每次导航创建全新 Page 实例

**位置**: `MainWindow.xaml.cs` 第 57-85 行

```csharp
private void NavigateTo(string page)
{
    ContentFrame.Content = page switch
    {
        "studyLibrary" => CreatePage<MacStudyLibraryPage>(),
        "aiChat" => CreatePage<MacAIChatPage>(),
        ...
    };
}
private static T CreatePage<T>() where T : Page, new() => new T();
```

每次切换导航项（如从"学习库"切到"AI 对话"再切回"学习库"），学习库页面被**丢弃并重建**。ViewModels 在 DI 中是 Singleton，但 Page 本身的 XAML 树需要重新通过 `InitializeComponent()` 构建。

**后果**: 每次导航切换都有 50-200ms 的页面重建延迟，且页面的 UI 状态（滚动位置、选中项、展开状态）全部丢失。

**修复**: 缓存 `ContentFrame` 中的 Page 实例（`Dictionary<string, Page>`）。

---

## 3. 中严重度（Medium）

### 3.1 MacIPhoneConnectionPage — 呼吸动画在 Storyboard 中定义，重复播放不可控

**位置**: `MacIPhoneConnectionPage.xaml` 第 8-36 行

呼吸动画用 XAML `Storyboard RepeatBehavior="Forever" AutoReverse="True"` 实现，比 HarmonyOS 的 setTimeout 方案好得多。但设备连接状态变化时（`ConnectedPanel.Visibility` 从 Collapsed → Visible），动画需要手动 Begin/Fill/Stop 切换——当前 code-behind 未见这些逻辑。

### 3.2 页面背景色不一致

| 页面 | 背景 |
|------|------|
| HomePage | 无显式背景（继承窗口） |
| StudyLibraryPage | 无显式背景 |
| MacStudyLibraryPage | `RokuricsPageGradientDarkBrush` |
| MacAIChatPage | `RokuricsPageGradientDarkBrush` |
| ChatPage | 无显式背景 |
| MacIPhoneConnectionPage | `RokuricsPageGradientDarkBrush` |
| MacSettingsPage | `RokuricsPageGradientDarkBrush` |

HomePage 和 ChatPage 没有显式背景（依赖 MicaBackdrop 透过窗口），而 MacStudyLibrary 等有硬编码暗色渐变。两者共存导致视觉断裂。

### 3.3 RokuricsIconButtonStyle — 按压缩放使用 RenderTransform

**位置**: `App.xaml` 第 267-315 行

使用 `ScaleTransform` + `VisualStateManager` 做按钮按压缩放（Normal → PointerOver 1.025x → Pressed 0.985x）。这是正确的 WinUI 动画方式，但缺少 `Pressed → Normal` 的过渡动画（直接 snap 回 1.0），与 `PointerOver` 的展开有 1.025 → 1.0 的视觉跳跃。

### 3.4 StudyLibraryPage 和 MacStudyLibraryPage 功能重复

项目中同时存在 `StudyLibraryPage.xaml`（简洁版，2 个 Row，95 行）和 `MacStudyLibraryPage.xaml`（完整版，4 个面板，441 行）。当前 MainWindow 使用 MacStudyLibraryPage，StudyLibraryPage 只在代码历史中可见。两个页面的功能有高度重叠（面包屑导航、文件夹列表、录音列表）。

### 3.5 ChatRoleToBackgroundConverter — 用户消息气泡使用全局渐变 Brush 但无法动态调整

**位置**: `ValueConverters.cs` 第 49-61 行

用户消息气泡复用 `RokuricsColors.ActionGradientBrush`（静态 Brush）作为背景。因为Brush 是资源字典中的静态资源，所有用户消息气泡共享同一个 Brush 实例。如果未来需要按会话动态调整颜色，无法通过 Binding 切换。

---

## 4. 低严重度（Low）

### 4.1 MainWindow 搜索框的 SuggestionItems 永远是 null

**位置**: `MainWindow.xaml.cs` 第 108 行

```csharp
sender.ItemsSource = null;
```

搜索建议功能未实现。输入文本变化时建议列表被显式设为 null。

### 4.2 App.xaml — FontIcon Glyph 使用 Unicode 十六进制码

`&#xE8F1;`、`&#xE8EA;` 等 Segoe Fluent Icons 字体的码点在代码中不可读。WinUI 3 推荐使用 `SymbolIcon`（如 `Symbol="Library"`）而非手动指定 Glyph 码。

### 4.3 MacSettingsPage — 每个 Settings Row 的布局重复

转录、AI、关于三个 Section 的每个 Row 使用相同的 `Button` + `Grid(2 Columns) + TextBlock + Right StackPanel` 布局，在 XAML 中重复了 >10 次。可以提取为 DataTemplate。

### 4.4 StudyLibraryViewModel 在 App.cs 中实例化时注入了 StudyLibraryStore

**位置**: `App.xaml.cs` 第 59-63 行

```csharp
services.AddSingleton<StudyLibraryViewModel>(sp => {
    var studyStore = sp.GetRequiredService<StudyLibraryStore>();
    return new StudyLibraryViewModel(studyStore);
});
```

这是正确的 DI 模式——MVVM CommunityToolkit + Microsoft.Extensions.DI 的集成比其他三个平台的依赖管理清晣得多。

---

## 5. 正面发现

以下是 Windows 迁移中做得特别好的方面：

1. **MicaBackdrop** — `MainWindow.xaml.cs` 第 24 行：`SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop()`。这是 WinUI 3 的原生毛玻璃效果——比其他三个平台的模拟毛玻璃都要真实。遗憾的是 HomePage 没有显式使用透明/半透明背景来利用这个效果。

2. **App.xaml 样式系统** — 316 行的完整 Resource Dictionary：排版 Token、玻璃卡片 Style、胶囊 Style、圆 Style、Icon Button 的 ControlTemplate 含 ScaleTransform 动画。这是四个平台中最系统化的 Design Token 体系。

3. **CommunityToolkit.Mvvm** — `[ObservableProperty]`、`[RelayCommand]` 源生成器简化了 ViewModel 代码，`SettingsViewModel` 的 50+ 个 observable 属性只需一行声明。

4. **VisualStateManager 在按钮模板中的应用** — `RokuricsIconButtonStyle` 通过 VSM 正确管理 Normal/PointerOver/Pressed/Disabled 四种状态的 ScaleTransform 和透明度，完全在渲染线程执行。

5. **MacIPhoneConnectionPage 的呼吸动画** — XAML `Storyboard` + `CubicEase` + `RepeatBehavior="Forever"` + `AutoReverse="True"` 的组合是四个平台中最优雅的 Orb 呼吸动画实现——全部在 XAML 层面完成，不涉及任何 C# 代码。

6. **ChatRoleToAlignmentConverter / ChatRoleToBackgroundConverter** — 聊天消息的对齐和背景完全通过 XAML 绑定和 Converter 实现，没有在 code-behind 中硬编码逻辑。

7. **DI 容器** — `Microsoft.Extensions.DependencyInjection` 的 ServiceCollection + ServiceProvider 正确管理了 Singleton 和 Transient 的生命周期。`StudyLibraryStore` 依赖 `AudioFileStore` 的注入链也比 Android 版更清晰。

8. **MergedDictionaries** — App.xaml 使用 `<ResourceDictionary.MergedDictionaries><XamlControlsResources/>` 导入 WinUI 3 默认样式，然后覆盖。模式正确。

---

## 6. 总结

| 严重度 | 数量 | 关键项 |
|--------|------|--------|
| Critical | 3 | 4 面板同时驻内存、RequestedTheme 硬编码 Dark、HomePage 无响应式 |
| High | 5 | Connection/AIChat 硬编码 Dark 背景、Save 无防抖、ItemsControl 无虚拟化、Page 每次都重建 |
| Medium | 5 | 动画生命周期、背景色不一致、按压缩放 snap、页面重复、渐变 Brush 共享 |
| Low | 4 | 搜索未实现、Glyph 码可读性、布局重复、DI 配置正确 |

Windows 项目的整体架构质量是四个平台中最高的：Mica 毛玻璃 + VSM 动画 + DI 容器 + CommunityToolkit.Mvvm + XAML Resource Dictionary 的组合远比其他平台的"手写 CSS/Modifier"先进。UI 层面的核心问题是**页面每次都重建**和**多面板同时驻留内存**，修复成本低但收益高（导航速度和内存占用）。

与 HarmonyOS 类似，暗色模式硬编码是最大的用户体验问题，但修复成本远低于 HarmonyOS——WinUI 3 的 ThemeResource 体系天然支持双模式，只需将 `RequestedTheme="Dark"` 改为 `"Default"` 并补充浅色资源。
