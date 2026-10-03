# Flotis-Windows（WinUI 3 原生 Windows 版）

本目录是 `Flotis-Apple`（macOS 悬浮语音输入胶囊）的 **WinUI 3 原生 Windows 移植**，对照 macOS V0.13 的产品行为实现。Flotis-Apple 全程只读；本目录内代码全部为 Windows 侧实现。

> 本机没有 Windows 开发环境：全部代码按"可直接构建"标准写好，但**未运行构建/测试**。首次构建预计仍需少量编译器反馈修正（见文末 UNCERTAINTIES）。

## 功能对齐（相对 Flotis-Apple V0.13）

- LSUIElement 等价物：无主窗口、托盘图标 + 全局热键 + 非激活悬浮胶囊。
- 全局热键（`RegisterHotKey` 官方 API）：voice / Quick Ask / panel 显隐 / 对比上一个·下一个，固定 ID 100/200/300/400/500，冲突时每 2 秒重试并经橙点与无障碍文本暴露。
- 语音状态机：idle → requestingPermission → connecting → recording/streaming → stopping → transcribing → reviewing → 复制回剪贴板 → idle 胶囊保持可见；session generation 防旧会话回写；录音上限倒计时自动 stop。
- 六条转写路径（adapter ID 与 config.json schema 与 macOS 逐字一致）：
  - `apple-on-device`：Windows 平台语音识别（`Windows.Media.SpeechRecognizer`），语言包缺失时**显式失败**，无第二引擎；
  - `openai-audio-transcriptions-http-v1`：multipart / OpenRouter JSON+Base64，仅 HTTPS+Bearer，不跟随重定向，严格顶层 `text`；
  - `openai-realtime-transcription-ga`：WSS + Bearer，GA session.update，PCM16 24 kHz mono，手动 commit，item 级装配；
  - `dashscope-paraformer-ws-v1`：run-task/task-started/result-generated/finish-task/task-finished，PCM16k mono；
  - `volcengine-bigasr-ws-v3`：火山二进制帧协议，X-Api-* 头，resource ID 与 model_name 分离，last-packet 终态；
  - `glm-asr-http-sse-v4`：WAV 上传 + SSE 流式响应，要求 `[DONE]` 终态。
- 多模型对比：2–4 个 recorded-file route 共享同一段录音并发转写，单项失败隔离，自动打开首个成功项，对比快捷键仅在 ≥2 个成功候选时临时注册。
- Quick Ask：独立聊天 catalog（canonical `quick_ask` 区），非流式 Chat Completions，420×560 置顶临时面板，关闭即销毁，Enter 发送 / Shift+Enter 换行 / Esc 关闭取消。
- canonical 配置：唯一真源 `%APPDATA%\Flotis\config.json`，schema v2 键名与 macOS 完全一致（`$schema`、`schema_version`、`model`、`provider_order`、`enabled_providers`、`comparison`、`provider`、`shortcuts`、`quick_ask`）；进程锁 + 命名互斥体覆盖 read-modify-write，临时文件 + 原子替换写入，目录 ACL 收敛到当前用户；损坏/异常大/符号链接文件拒绝覆盖。
- 凭据边界：API key 只存在于对应 provider 的 `options.apiKey`；secret boundary（adapter/scheme/host/port/auth）变化时旧 key 不被静默复用或迁移。
- Connection Tester：与真实会话同一 registry/runtime 工厂，使用程序生成的 0.8 s 无隐私合成音；成功只存安全摘要与配置 fingerprint。
- 界面语言：跟随系统 UI 语言，简中标识 → 简中，其余 → 英文；与参考一致的单一双语入口（`UIStrings`）。

## 构建与运行

```bash
cd Flotis-Windows
dotnet restore
dotnet build -c Debug        # 需要 Windows + .NET 8 SDK + Windows App SDK workload
dotnet run --project src/Flotis.App
```

- 目标框架 `net8.0-windows10.0.19041.0`，unpackaged（`WindowsPackageType=None`），x86/x64/ARM64。
- 单实例由命名 Mutex 保证（等价 macOS `LSMultipleInstancesProhibited`）。
- 麦克风/语音识别权限走 Windows 隐私设置；被拒时各链路显式报错。

## 外部依赖（Dependency Policy 决策记录）

依据 `/Users/vita/Vitemis/docs/DEPENDENCY_POLICY.md` 的强制顺序逐项记录：

| 能力 | 选定依赖 | 版本 | 许可证 | 决策理由 |
| --- | --- | --- | --- | --- |
| UI 框架 | Microsoft.WindowsAppSDK (WinUI 3) | 1.5.240627000 | MIT（微软官方 NuGet） | 用户指定 "WinUI3 原生"；仓库 Kikaria/Intatis/Rokurics 已采用同一版本线 |
| MVVM 可观测基元 | CommunityToolkit.Mvvm | 8.2.2 | MIT | Rokurics-Windows 已采用；仅用其官方 ObservableObject API |
| JSON | System.Text.Json | 8.0.4 | MIT | Rokurics 先例；BCL 内建 |
| 音频捕获与重采样 | NAudio | 2.2.1 | MIT（naudio/NAudio 官方 NuGet，provenance 良好） | WASAPI 捕获与 Media Foundation 重采样的官方封装；自研 DSP/COM interop 属于"重新实现同等能力"，违反 Dependency Policy。平台审查：纯 managed、无原生分发负担、Windows 桌面事实标准 |
| 界面字体 | JetBrains Mono v2.304 variable TTF | v2.304 | SIL OFL 1.1 | 与 macOS 端 exact 同一依赖；仓内 TTF SHA-256 `662a196d58f1183bf2d77428b6d5283fe3f45161ab021bea4036bc98e5cac016`，随包附带 OFL.txt/AUTHORS.txt |
| Win32 interop | 自写 P/Invoke（user32/shell32/gdi32） | — | — | RegisterHotKey/Shell_NotifyIcon/AddFontResourceEx 等 OS API 的最薄生命周期接线，不属于第三方替代 |

禁止功能兜底条款的执行：

- 平台语音识别不可用/语言包缺失 → 该 adapter 报错终止，不降级到任何在线引擎或云服务；
- HTTP/WSS 全部仅 HTTPS/WSS、凭据请求不跟随重定向；错误文本先做精确 key 脱敏再做通用模式脱敏后截断；
- config.json 结构非法时不覆盖原文件，进程内使用恢复配置并提示。

## 与 macOS 参考的刻意平台差异

这些是平台语义差异的**明文决定**，不是静默降级：

1. **热键修饰键映射**：descriptor JSON 保持 `command/option/shift/control` 四布尔；Windows 侧 `command` 映射为 Win 键。默认值：voice `Ctrl+Alt+A`、Quick Ask `Ctrl+Alt+Q`、panel `Win+Alt+Shift+0`、prev/next `Alt+←/→`。
2. **胶囊快捷键字形**：胶囊沿用紧凑字形 `⌃⌥A/⌃⌥Q`（视觉对齐参考）；Settings 录制行显示 Windows 记法（`Ctrl+Alt+A`）。Windows 记法更长，空闲胶囊宽度自适应到最大 220 DIP（参考为固定 96pt + 缩放下限 70%）。
3. **Quick Ask 面板焦点**：Windows 无 `.nonactivatingPanel` 等价物；面板置顶但点击会取焦（标准窗口行为）。Esc/E 再次热键/关闭按钮行为与参考一致（销毁会话）。
4. **粘贴注入**：V0.13 主链路只写系统剪贴板，不做 AX 注入；Windows 版同样只复制不模拟按键，`ClipboardPasteInjector` 未移植（无可移植的平台机制）。
5. **InputMethodKit 输入法接口**：macOS 特有 IMK bundle；Windows 等价物需要 TSF 输入法 DLL 工程，超出本轮范围，**未实现且未伪造**（见 UNCERTAINTIES）。
6. **旧数据迁移**：Windows 侧不存在历史 UserDefaults/secrets.json 数据，v1/v2 迁移层有意省略；canonical 文档结构不变。
7. **测试记录时间戳**：`testedAt` 使用 ISO8601 字符串（Swift 默认 Date 编码不同）；测试记录是机器本地缓存，跨平台 fingerprint 失配只会使旧测试失效并要求重测，属安全方向。

## 目录结构

```
Flotis-Windows/
├── Flotis-Windows.sln
├── global.json
└── src/Flotis.App/
    ├── App.xaml(.cs)            # AppDelegate 等价装配
    ├── Program.cs               # 入口 + 单实例
    ├── Interop/                 # Win32: 热键 sink、托盘、窗口样式
    ├── Models/                  # VoiceInputState、KeyboardShortcutDescriptor
    ├── Services/
    │   ├── Providers/           # schema 表、TranscriptionConnection、canonical 文档
    │   ├── Stores/              # config.json 存储、provider/comparison/hotkey store
    │   ├── Adapters/            # 六条协议路径
    │   ├── Audio/               # NAudio 捕获、WAV/M4A 录制、合成测试音
    │   ├── QuickAsk/            # client/session/catalog store
    │   └── …                    # controller、registry、tester、comparison、UIStrings
    ├── Views/                   # 胶囊、Quick Ask 面板、Settings 三页
    └── Assets/Fonts/            # JetBrains Mono TTF + OFL 许可证
```

## 手动验证清单（首次上机）

1. 启动后托盘出现图标，胶囊出现在屏幕下方中央；拖动位置重启后保留。
2. voice 热键开始/停止录音；reviewing 卡片编辑后"复制并返回"写入剪贴板并缩回小胶囊。
3. Settings → 转写：新增 OpenAI Compatible provider，录入 key、多模型、Test Provider 成功后保存并设为当前。
4. 开启对比（勾选 ≥2 个就绪 route）后录音，验证双列候选卡与 prev/next 快捷键。
5. Quick Ask 热键打开面板；配置 chat provider 后发送/停止/复制回复；关闭面板再打开确认消息已销毁。
6. 断网/错误 key 下 Test Provider 与真实会话均给出脱敏后的失败摘要。
