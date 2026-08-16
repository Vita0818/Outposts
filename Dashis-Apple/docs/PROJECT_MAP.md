# PROJECT_MAP

## 当前目录

```text
Dashis/
├── AGENTS.md / CLAUDE.md / GEMINI.md
├── .codex/environments/environment.toml
├── App/macOS/
│   ├── DashisApp.swift
│   ├── DashboardView.swift
│   ├── DashisSidebar.swift
│   ├── DashisDashboardDetail.swift
│   ├── DashisDashboardComponents.swift
│   ├── DashisDesign.swift
│   ├── DashisModels.swift
│   ├── DashisProviderStore.swift
│   ├── DashisProviderService.swift
│   ├── ProviderSnapshot.swift
│   ├── ProviderUsageClient.swift
│   ├── ProviderCardProjection.swift
│   ├── ProviderJSON.swift
│   ├── ProviderEndpointPolicy.swift
│   ├── ProviderHTTPClient.swift
│   ├── LoopbackOAuthCoordinator.swift
│   ├── ProviderConnectionCoordinator.swift
│   ├── ProviderOAuthSupport.swift
│   ├── CodexUsageClient.swift
│   ├── ClaudeStatusLineCodec.swift
│   ├── ClaudeUsageClient.swift
│   ├── ClaudeSettingsPatcher.swift
│   ├── GoogleQuotaClient.swift
│   └── OpenRouterUsageClient.swift
├── Tools/ClaudeStatusLineHelper/main.swift
├── tests/DashisTests/
│   ├── ProviderFoundationTests.swift
│   ├── ProviderDecoderTests.swift
│   ├── ProviderCorrectnessTests.swift
│   └── SecurityBoundaryTests.swift
├── Dashis.xcodeproj/
│   ├── project.pbxproj
│   └── xcshareddata/xcschemes/Dashis.xcscheme
├── script/build_and_run.sh
├── codex-report/07_10_26-21_34-provider-quota-integration.md
└── docs/
    ├── ARCHITECTURE.md
    ├── CURRENT_STATE.md
    ├── DO_NOT_BREAK.md
    ├── PROJECT_MAP.md
    ├── TESTING.md
    └── USER_TUTORIAL.md
```

## 入口与 targets

- Agent 入口：根目录 `AGENTS.md`、`CLAUDE.md`、`GEMINI.md`；`docs/` 中同名文件为 shim。
- macOS App：`Dashis.xcodeproj` / target 与 shared scheme `Dashis` / `App/macOS/DashisApp.swift`。
- Claude helper：target `ClaudeStatusLineHelper`，源码为 `Tools/ClaudeStatusLineHelper/main.swift` 与共享 codec，product 名 `dashis-claude-statusline`。
- Swift tests：target `DashisTests`；由 shared scheme `Dashis` 的 Test action 执行。
- build/run：`script/build_and_run.sh`；Codex app Run action 位于 `.codex/environments/environment.toml`。

## UI 与状态层

- `DashboardView.swift`：`NavigationSplitView` 根视图和 root-owned Store。
- `DashisSidebar.swift`：Dashboard、固定四个内置 provider 与底部 Settings；没有动态 Add/custom provider。
- `DashisDashboardDetail.swift`：Dashboard cards、单 provider detail、Settings 路由。
- `DashisDashboardComponents.swift`：卡片、source/freshness badge、Codex/Claude/Google/OpenRouter 原生控件。
- `DashisDesign.swift`：系统白/黑主题、serif typography、状态色与玻璃卡片。
- `DashisModels.swift`：provider-first UI 值类型与固定四个内置 provider 空态；源码中的 custom factory 仅是内部 fallback，当前 UI 不暴露注册入口。
- `DashisProviderStore.swift`：UI state、session-only 输入/credential、显式动作、Clear generation guard 和 snapshot 投影。
- `DashisProviderService.swift`：仅作为 adapter composition root；不再包含 endpoint、decoder 或凭据逻辑。

## 统一 provider 基座

- `ProviderSnapshot.swift`：provider ID/scope/source、quota window、balance、metric、warning/failure、freshness。
- `ProviderUsageClient.swift`：adapter protocol。
- `ProviderCardProjection.swift`：snapshot 到 provider card/detail 的统一投影；保留负 remaining，仅限制进度条。
- `ProviderJSON.swift`：有限数值、日期和安全错误归一化。
- `ProviderEndpointPolicy.swift`：request method/URL/query/body allowlist。
- `ProviderHTTPClient.swift`：ephemeral/no-cache/no-cookie `URLSession`、redirect 拒绝、有限 retry。
- `LoopbackOAuthCoordinator.swift`：由默认浏览器发起授权，只绑定随机 `127.0.0.1` callback、校验授权 URL/path 与适用 provider 的 state、支持取消。
- `ProviderConnectionCoordinator.swift`：OpenRouter 与 Google 各自独立的 session OAuth orchestration；OpenRouter 官方 flow 无 state，使用随机 callback path + PKCE，Google 使用 state + PKCE。
- `ProviderOAuthSupport.swift`：Google Desktop OAuth/PKCE/state/token request 与 session access token。

## Provider adapters

- `CodexUsageClient.swift`：personal experimental `wham` 与 Enterprise Analytics 分页。
- `OpenRouterUsageClient.swift`：OAuth key、management credits/activity/meta-driven analytics/generation。
- `GoogleQuotaClient.swift`：consumer manual snapshot、Cloud Quotas/Monitoring decoder 与 quota derivation。
- `ClaudeStatusLineCodec.swift`：Claude statusLine 净化 DTO、安全 snapshot 文件与 prior command marker。
- `ClaudeUsageClient.swift`：本地 snapshot 到 5-hour/7-day quota windows。
- `ClaudeSettingsPatcher.swift`：helper 安装、settings 顶层 byte-range Connect/Disconnect patch 与并发保护。
- `Tools/ClaudeStatusLineHelper/main.swift`：statusLine stdin capture、净化快照更新、prior command 兼容转发。

## 测试与研究

- `ProviderFoundationTests.swift`：endpoint allowlist、source/freshness、负余额投影、ISO 日期、PKCE。
- `ProviderDecoderTests.swift`：Codex/OpenRouter decoder、Claude codec/settings patch、Google manual/quota/OAuth fixture，以及手工数值推导的有限值边界。
- `ProviderCorrectnessTests.swift`：四 provider registry、严格 decoder、Google cadence/dimension/limit/concurrent/OAuth correctness。
- `SecurityBoundaryTests.swift`：JSON bridge、endpoint path/query、Claude snapshot 文件属性与 embedded helper 端到端边界。
- `codex-report/07_10_26-21_34-provider-quota-integration.md`：本轮四 provider 研究、来源分级、实施路线与实现附录。
- 自动测试必须保持合成、离线且不读取真实 `~/.codex`、`~/.claude`、浏览器、Keychain 或用户 provider 数据。

## 生成物

- 仓库内无 build 生成物。脚本与验证命令把 DerivedData 写入系统临时目录。
- `Dashis.app` 内嵌 helper 位于 `Contents/MacOS/dashis-claude-statusline`；`Preview connect` 不复制文件，只有用户确认 `Apply change` 才会复制到 `~/Library/Application Support/com.vitemis.dashis/ClaudeBridge/bin/`。
- Claude 净化 snapshot 位于同一 app-support 根下的 `snapshot.json`，不属于 Git 生成物，Clear/Disconnect 可删除。
- 当前没有 package manager、Web bundle、Node 依赖或部署配置。
