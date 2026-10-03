# Rokurics Apple 对照 Android / Windows 差距审计

## 0. 执行记录

- MODE: SPARK
- MODEL_CHECK_RESULT: PASS。本轮按本对话已确认的 Spark 执行口径继续；未把模型口径写入任何规则文档。
- PATH_CHECK_RESULT: PASS。`pwd` 与 `git rev-parse --show-toplevel` 均为 `/Users/vita/Vitemis/Outposts`。
- SCOPE_CONFIRMATION: `Rokurics-Apple` 只读参考；`Rokurics-Android` 与 `Rokurics-Windows` 只读审计；本轮只写入本报告。
- DEEPCODE_OUTPUTS: NONE。
- QWENCODE_OUTPUTS: NONE。本轮未生成 reference/actual 截图，也未做视觉验收；若后续进入界面像素级校验，需要单独启动 QwenCode compare。
- BUILD_RESULT: NOT_RUN。本轮是静态源码/文档审计。
- TEST_RESULT: NOT_RUN。本轮是静态源码/文档审计。

## 1. Apple 参考基线

Apple 端不是单一 UI 复刻目标，而是一套 iPhone + Mac + shared canonical runtime 的完整链路：

1. iPhone app 级同步服务在 `RokuricsApp` 持有并跟随 scene phase 启停：`Rokurics-Apple/Rokurics/RokuricsApp.swift:13`, `:19`, `:25`；实现体在 `StudyLibrarySyncCoordinator.swift` 的 `LocalNetworkSyncAppService`：`:10971`, `activate() :11257`, `suspend() :11706`。
2. Mac receiver 使用统一安全 HTTPS 协议：Apple server 提供 `POST /pair`、录音元数据/音频/分片上传、`/sync/start`、`/sync/start-ack`、`/sync/inventory` 等路由：`Rokurics-Apple/RokuricsMac/SecureLocalHTTPSServer.swift:2319`, `:2327`, `:2355`, `:2443`, `:2451`, `:2455`。
3. 请求安全验证不是普通 HTTP handler，而是 route rule + timestamp + nonce + body hash + signature verifier：`Rokurics-Apple/RokuricsMac/RequestVerifier.swift:28`, `:46`, `:168`, `:180`。
4. Apple shared canonical runtime 覆盖 apply、sync、status truth、effective status UI projection、connection runtime、transfer runtime/retry/proof/readiness、diagnostics 等文件族；Android/Windows 要对齐时不能只迁移模型名。
5. Apple Mac 主界面用 `NavigationSplitView` 聚合 dashboard、iPhone connection、study library、AI chat 等 detail view：`Rokurics-Apple/RokuricsMac/MacRootView.swift:32`, `:77`, `:87`, `:100`, `:108`。

## 2. Android 未做好点

### A-P0-1 App 级后台同步/心跳链路缺失

Android 已有 `LocalNetworkSyncEngine.performTick("manual")`，但入口只在连接页手动触发：`Rokurics-Android/app/src/main/java/com/rokurics/app/ui/connection/MacConnectionScreen.kt:148`。`ContentView`/`HomeScreen` 没有等价于 Apple `LocalNetworkSyncAppService` 的 app 级生命周期服务、heartbeat monitor、sync requested 事件循环或自动 tick。

影响：配对后即使协议端点存在，也更像“手动同步按钮”，不是 Apple 当前的持续状态交换/自动同步运行时。

### A-P0-2 Canonical production kernel 仍未达到 Apple 语义

Android 有 `CanonicalKernelFacade`，但 production 分支仍出现占位数据：

- upload finalize proof 使用 `0L` size 与空 sha256：`Rokurics-Android/app/src/main/java/com/rokurics/app/domain/canonical/CanonicalKernelFacade.kt:813`。
- metadata/generated artifact/conflict 仍调用 `placeholderMetadata(...)`：`:824`, `:835`, `:856`, `:917`。

同时 Android 缺 Apple shared canonical 的关键运行时族：effective status/UI projection、status truth runtime、connection/status exchange runtime、transfer runtime/retry/readiness/proof、diagnostics/protocol abstraction 等。

影响：Android 可以跑部分 planner/transport，但还不能声明与 Apple canonical kernel 的生产语义一致；尤其是 status truth、artifact apply、conflict record、upload finalize proof 都容易出现假成功。

### A-P0-3 同步/上传状态的 UI truth source 不完整

Apple 参考实现有 effective/display status projection、status truth runtime、upload state truth。Android Home 目前直接用本地 `ConnectionStore` 与页面状态派生 UI：`Rokurics-Android/app/src/main/java/com/rokurics/app/ui/home/HomeScreen.kt:65`, `:478`。这会绕开 canonical status truth，也无法表达 Apple 端的“实际状态”和“展示状态”分层。

影响：当 Mac 状态、上传队列、artifact sync、manual sync 结果不一致时，Android 首页可能显示“已连接/可同步”，但底层 canonical runtime 并没有一致的事实源。

### A-P1-1 录音后 AI 处理链路仍以 mock/placeholder 为主

Android AI chat 页面可以基于设置构造 provider：`Rokurics-Android/app/src/main/java/com/rokurics/app/ui/chat/AIChatScreen.kt:891`, `:897`。但录音处理链路默认仍是 mock：

- `ProcessingCoordinator` 默认 `MockTranscriptionProvider` 与 `MockNoteGenerationProvider`：`Rokurics-Android/app/src/main/java/com/rokurics/app/domain/provider/ProcessingCoordinator.kt:6`, `:19`。
- `RecordingManager` 默认 transcription/note provider 仍是 mock：`Rokurics-Android/app/src/main/java/com/rokurics/app/service/RecordingManager.kt:333`, `:334`, `:335`。
- `WhisperCppEngine` 的 native/CLI 分支还是 placeholder：`Rokurics-Android/app/src/main/java/com/rokurics/app/domain/provider/WhisperCppEngine.kt:149`, `:153`, `:161`, `:165`。

影响：聊天能力和录音转写/笔记生成能力没有统一到 Apple 类似的 coordinator/provider pipeline；录音完成后很可能不能得到真实 transcript/note。

### A-P1-2 连接状态不是共享 reactive source

`HomeScreen` 默认 `remember { ConnectionStore() }`：`Rokurics-Android/app/src/main/java/com/rokurics/app/ui/home/HomeScreen.kt:65`；连接页也单独 `remember { ConnectionStore() }`：`Rokurics-Android/app/src/main/java/com/rokurics/app/ui/connection/MacConnectionScreen.kt:44`。`ConnectionStore.isPaired` 是普通 getter：`Rokurics-Android/app/src/main/java/com/rokurics/app/data/ConnectionStore.kt:98`。

影响：配对成功后首页可能不会即时刷新；不同页面可能各自读写同一个 SharedPreferences，但 Compose state 不会自然传播。

### A-P1-3 录音中的系统级持续 UX 仅有基础前台通知

Android manifest 和 `RecordingService` 已有 foreground service/notification，但 Apple iPhone 端还有 ActivityKit Live Activity 与更完整的录音中状态控制。Android 不需要照搬 Live Activity API，但需要明确 Android 等价策略：ongoing notification action、锁屏/状态栏展示、录音异常恢复、权限撤销后的状态回收。

影响：录音本身已比 Windows 更实，但“录音中体验和状态可靠性”还没有 Apple 端同等级闭环。

### A-P2-1 测试覆盖仍偏底层单测

Android 当前有若干 unit tests，例如 secure storage/upload、recording metadata、WhisperCppEngine、sync diff planner 等；但没有 `app/src/androidTest` 目录。缺口主要是配对/同步端到端、foreground recording service、Compose navigation/state、canonical production proof/status truth 的集成验证。

### A-P2-2 UI 视觉 parity 未完成验收

源码上 Android 首页已经明显向 Apple iPhone 首页靠拢，但本轮未抓取 Android actual screenshot，也未用 QwenCode 与 Apple reference compare。因此只能记录“源码结构接近”，不能记录“视觉完成”。

## 3. Windows 未做好点

### W-P0-1 Secure receiver / pairing / upload / sync server 仍是 stub，且协议路由不兼容 Apple

Windows `KestrelReceiverService` 仍是基础 stub：`Rokurics-Windows/Rokurics/Services/InfrastructureStubs.cs:90`；启动/停止/配对相关方法未实现或返回占位：`:109`, `:113`。`KestrelRouteHandler.MapRoutes` 仍未实现：`Rokurics-Windows/Rokurics/Services/KestrelRoutes.cs:372`。

更关键的是路由协议不一致：

- Windows 设计为 `/pairing/begin`, `/pairing/verify`, `/pairing/complete`, `/pairing/status`, `POST /upload`：`Rokurics-Windows/Rokurics/Services/KestrelRoutes.cs:11`, `:12`, `:13`, `:14`, `:15`, `:352`, `:356`。
- Apple/Android 当前协议使用 `POST /pair` 与 Apple 录音/同步路由；Android client 也请求 `/pair`、`/sync/start`、`/sync/start-ack`、`/sync/inventory`、分片上传 session 路由：`Rokurics-Android/app/src/main/java/com/rokurics/app/data/SecureUploadClient.kt:55`, `:283`, `:302`, `:327`, `:763`, `:807`, `:840`。

影响：Windows 端目前不能作为 Apple 语义下的 Mac receiver 替代端，也不能稳定接 Android/iPhone 的真实上传与同步请求。

### W-P0-2 请求安全验证缺失

Apple receiver 的安全边界集中在 `RequestVerifier`，按 path rule 校验 timestamp、nonce、body hash、signature。Windows 当前没有等价 verifier；`PairingService.VerifyFingerprintAsync` 甚至直接返回 true：`Rokurics-Windows/Rokurics/Services/InfrastructureStubs.cs:357`。

影响：即使 Kestrel routes 后续能跑通，也会缺少 replay 防护、body 完整性、shared secret signature 与 unknown device 拒绝路径。

### W-P0-3 Windows 录音/音频捕获仍不可用

Windows `RecordingManager.StartRecording()` 中 WASAPI 是 TODO，但仍把状态切到 Recording：`Rokurics-Windows/Rokurics/Services/RecordingManager.cs:105`, `:125`。`StopRecording()` 也没有真实停止 capture：`:164`；最终 metadata 写入 `fileSize = 0`：`:207`。底层 `WindowsAudioCapture` 也是未实现 stub：`Rokurics-Windows/Rokurics/Services/InfrastructureStubs.cs:253`, `:274`。

影响：Windows UI 可能显示正在录音或已生成记录，但不会产出真实音频文件；这会污染后续 upload/transcription/library flow。

### W-P0-4 Canonical kernel 基本缺席

Windows 只有局部模型/merge/state machine；缺 Apple shared canonical 的 `CanonicalKernelFacade`、apply runtime、sync runtime、upload runtime、transport runtime、status truth、connection runtime、transfer retry/proof/readiness 等核心文件族。

影响：Windows 目前不是 Apple canonical runtime 的端口，而是较早期的模型/合并逻辑加 stub service。跨端冲突、artifact apply、inventory diff、status truth 和 retry proof 都无法对齐。

### W-P0-5 Upload client 当前是 mock success

App DI 注册 `MockRecordingUploadClient`：`Rokurics-Windows/Rokurics/App.xaml.cs:53`；mock client 在 `Rokurics-Windows/Rokurics/Services/MockProviders.cs:100` 开始实现，不做真实网络传输。

影响：任何“上传成功”的 Windows 本地结果都不能作为协议或文件传输成功证据。

### W-P1-1 AI/transcription provider pipeline 仍未生产化

App 默认注册 `MockTranscriptionProvider`：`Rokurics-Windows/Rokurics/App.xaml.cs:50`。`WhisperCppProvider.TranscribeAsync` 是 TODO/NotImplemented：`Rokurics-Windows/Rokurics/Services/InfrastructureStubs.cs:223`。Note/chat 侧有 real provider 接口雏形，但录音转写入口仍未接近 Apple Mac 的 coordinator/provider 运行形态。

影响：Windows 不能承担 Apple Mac 端“接收录音后转写、生成笔记、参与 AI chat”的完整角色。

### W-P1-2 App state / DI 有重复实例风险

`App.xaml.cs` 注册了 singleton `RecordingManager`：`Rokurics-Windows/Rokurics/App.xaml.cs:48`，但 `MainViewModel` 又直接 `new RecordingManager()`：`Rokurics-Windows/Rokurics/ViewModels/MainViewModel.cs:29`。

影响：页面、服务、view model 可能各持一份录音/库/同步状态，后续即使功能实现，也容易出现 UI 显示与后台服务状态不一致。

### W-P1-3 Windows Mac shell UI 与 Apple Mac root 不一致

Apple Mac root 有 dashboard detail view：`Rokurics-Apple/RokuricsMac/MacRootView.swift:77`。Windows `MainWindow` 默认进入 study library：`Rokurics-Windows/Rokurics/MainWindow.xaml.cs:12`, `:28`；导航项也只见 study library / AI chat / iPhone connection / settings，未见 dashboard：`Rokurics-Windows/Rokurics/MainWindow.xaml:49`。另有 `HomePage` 录音 UI：`Rokurics-Windows/Rokurics/Views/HomePage.xaml:3`，但主窗口路由没有进入它。

影响：Windows 的主界面更像“Mac library shell”，不是 Apple Mac dashboard-first 管理台；HomePage 也可能成为孤立页面或历史遗留。

### W-P2-1 测试覆盖极薄

排除 `bin/obj` 后，Windows 测试主要只看到 `Rokurics-Windows/Rokurics.Tests/StudyModelsTests.cs`。缺少 Kestrel route/security verifier、pairing, upload session, audio capture, canonical kernel, UI state/DI 的测试。

### W-P2-2 UI 视觉 parity 未完成验收

Windows 本轮未启动 app、未抓 screenshot、未走 QwenCode compare。当前只能基于 XAML/路由结构指出与 Apple Mac root 的结构差异，不能声明视觉差距已经穷尽。

## 4. 建议修复顺序

1. Windows 先补 P0 secure receiver 协议：统一为 Apple/Android 使用的 `/pair`、录音元数据/音频/session 上传、`/sync/start`、`/sync/start-ack`、`/sync/inventory` 等路由；同时实现 RequestVerifier 等价校验。
2. Windows 补真实音频捕获与真实 upload client，移除 mock success 对主流程的影响。
3. Windows 移植/重建 canonical production kernel，而不是继续扩展局部 merger/stub。
4. Android 补 app 级 `LocalNetworkSyncAppService` 等价物：生命周期启停、heartbeat、sync requested、自动 tick、错误回收。
5. Android 修 canonical production 语义：真实 finalize proof、真实 metadata/artifact/conflict apply、status truth/effective status UI projection。
6. Android 统一录音后 AI provider pipeline：决定本地 Whisper.cpp JNI、云 provider、或安全 Mac provider，并让 `RecordingManager` 与设置页/AI provider 共享配置。
7. 两端补集成测试，再进入截图/QwenCode 视觉验收。

## 5. 不确定性与边界

- 本报告是静态源码/文档审计，不是 build/test/screenshot 结果。
- Apple 只作为只读参考；本轮未修改 `Rokurics-Apple`、`Rokurics-Android`、`Rokurics-Windows`。
- 现有 worktree 已有大量与本轮无关的脏状态；本轮只新增本报告。
- UI 视觉差异需要后续 common phone/window sizes 的 actual screenshot 与 QwenCode compare；本轮不声称视觉闭环。

## 6. 下一步建议

优先拆成两个 Spark 实施任务：

1. Windows P0 protocol/runtime task：Kestrel receiver + verifier + route parity + mock upload removal。
2. Android P0 sync/canonical task：app-level sync service + canonical production/status truth 修正。
