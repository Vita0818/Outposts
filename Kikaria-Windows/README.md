# Kikaria Windows

原生 **WinUI 3** 的 Windows 版 Kikaria——对着只读参照 `Kikaria-Apple`(SwiftUI 背诵 App)
重建的 Windows 原生实现。Kikaria 用结构化 Markdown 知识点预设驱动「先回忆,再查看」的背诵流程,
并提供重点集锦 / 已掌握 / 今日概览 / 复习历史等学习状态管理。

```text
Kikaria-Windows/
  Kikaria-Windows.sln        解决方案(Kikaria.Core + Kikaria.App,Debug|x64 / Release|x64)
  global.json                .NET SDK 10.0.300(rollForward latestFeature)
  README.md
  src/
    Kikaria.Core/            纯逻辑(net8.0,无 UI 依赖,可在任意平台单测)
      Models.cs              数据模型 + JSON 序列化(KnowledgePoint / PresetStudyState / KikariaAppState)
      MarkdownParser.cs      知识点 Markdown 解析 / 导出
      LatexParser.cs         $..$ / $$..$$ 公式词法(照抄 Apple 规则)
      MathFallback.cs        readableMathFallback 完整符号映射移植
      AppStore.cs            JSON 持久化(%LOCALAPPDATA%\Kikaria\appState.json)+ 迁移合并
      StudyLogic.cs          复习队列 shuffle/reconcile、危险线判定、今日计数、搜索
      Presets.cs             内置预设加载(应用目录 Presets\*.md)
      Localization.cs        中英映射表(照抄 KikariaTypography)+ 混排辅助
    Kikaria.App/             WinUI3 应用(net8.0-windows10.0.19041.0,unpackaged)
      App.xaml(.cs)          主题资源(亮/暗全色表 + 渐变 + 玻璃卡画刷 + 字体/排版)
      MainWindow.xaml(.cs)   无 NavigationView,Frame 全屏路由 + 自定义标题栏融入背景
      Theme.cs               颜色/渐变/玻璃卡参数常量 + 主题感知
      Converters.cs          值转换器
      AppSession.cs          运行态单例(状态操作 + 持久化入口)
      Controls/              GlassCard(玻璃卡)/ MathText(公式混排)/ Toast / WheelPicker
      Pages/                 16 个页面(见下)
      Presets/               内置预设 Markdown(与 Kikaria-Apple/Presets 原样一致)
```

## 构建(需要 Windows)

沿用 Intatis-Windows 已验证的 unpackaged WinUI3 配方(Windows App SDK 1.5、
`WindowsPackageType=None`、PRI 生成关闭、x64/x86/ARM64)。要求 Windows 10 19041+ 与 .NET SDK:

```powershell
cd Kikaria-Windows
dotnet restore Kikaria-Windows.sln
dotnet build -c Release
dotnet run --project src/Kikaria.App
```

也可以在 Visual Studio 中打开 `Kikaria-Windows.sln` 直接 F5。

## 已移植功能

- **知识库**:结构化 Markdown 解析/导出(`# 标题`、`tags:`、`hint:`、`content:`、`---` 分块),
  5 个内置预设(大学物理 / 大学英语Band4 / 微积分 / 离散数学 / 离散数学_BACKUP)原样打包。
- **预设管理**:切换(带确认与 Toast)、新建(名称/分类/导入 .md|.txt/Markdown 文本)、
  编辑元数据、导出 Markdown(FileSavePicker)、删除(至少保留一个)、知识点增删改。
- **背诵**:范围选择(标签多选+搜索)、shuffle 队列(首位避免与上一点相同)、查看提示/查看答案、
  答案后按 normal / reinforcement / mastered 三模式动作网格、上一个/下一个、
  今日复习次数 pill、重点集锦/已掌握完成页。
- **学习状态**:重点集锦(次数累加「再次加入 ×n」)、已掌握(掌握即清空重点)、活动记录、
  今日概览(新增掌握/查看答案/查看提示/总已掌握/倒数)、复习历史月历热力 + 当日记录。
- **设置**:每日目标 1-100、倒数日起止(含起止校验)、进度安全线 1-100、
  通知开关与时间(仅保存设置,未接系统通知)、危险线状态展示、编辑资料、新手引导重放、
  Markdown 格式说明(含 AI Prompt 复制)、隐私政策、版权 © 2026 Vita、版本 1.0.0、浙ICP备2026034004号。
- **持久化**:`%LOCALAPPDATA%\Kikaria\appState.json`(schemaVersion=4),含内置预设更新重置、
  存量自定义预设合并、无效 currentPresetID 回退等迁移逻辑。
- **公式**:LaTeX 词法(代码围栏/`$` 转义/`$$` 块级)与 readableMathFallback 符号映射完整移植,
  `MathText` 控件用 RichTextBlock 混排(公式为衬线斜体文本,块级居中)。

## 与 Apple 版差异

- **公式渲染为文本 fallback 而非 SwiftMath**:Windows 版没有原生 LaTeX 排版引擎,
  公式显示为可读文本(如 `\frac{a}{b}` → `(a) / (b)`,希腊字母/运算符映射为 Unicode)。
- **无 Widget**:未移植桌面小组件与 WidgetSnapshot。
- **无本地推送通知**:通知开关与时间仅保存在本机,学习进度提醒在设置页展示状态,不接系统通知。
- **头像为文字头像**:用昵称首字母圆代替照片头像(Apple 版支持照片)。
- **混排简化**:中文与拉丁/数字不逐字符切换字体,正文统一 Microsoft YaHei UI;
  数学 fallback 文本用 Microsoft Serif UI 斜体。
- **单列手机版布局**:窗口 480x900 起步、最小 420x760,未做 iPad 双列自适应。

## 验证状态

本仓库于 macOS 编写,**尚未在 Windows 上编译**。已做静态自查:全部 XAML 良构、
x:Class 与命名空间一致、XAML 事件处理器与 code-behind 方法一一对应、
C# 括号/引号平衡复查通过。首次在 Windows 机器上执行 `dotnet build` 后,
如遇少量 API 签名级差异,按报错顺序修复即可。
