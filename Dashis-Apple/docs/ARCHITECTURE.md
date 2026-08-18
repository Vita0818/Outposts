# ARCHITECTURE

## 外部依赖优先与禁止功能兜底（Vitemis 强制规则）

本项目继承 `/Users/vita/Vitemis/docs/DEPENDENCY_POLICY.md`。本节是强制约束，不是建议。

- 当用户指定、仓库已经采用，或经许可证、provenance、安全与平台审查可采用的外部依赖提供同等能力时，必须直接集成该依赖的官方 API 或官方扩展点。
- 不得自行重写同等能力，不得新增替代 adapter、shim、compatibility layer、wrapper、proxy、facade、协议翻译层、parallel backend、preview backend、shadow implementation 或“先兜底、以后再换”的实现。
- 本地代码只允许保留官方 API 必需的最薄生命周期、类型、权限、配置和 bundle 接线；不得重新实现、解释、扩展或替代依赖的核心能力。
- exact 依赖因版本、构建、签名、许可证、平台、安全或官方 API 限制无法接入时，必须停止该能力、明确失败、报告 blocker 并请求用户决定；不得静默降级、切换 legacy/另一 provider/backend、使用 cache/mock/简化路径或继续交付不完整替代实现。
- 现有 fallback、adapter 或重复实现不构成先例，后续不得扩展。安全 fail-closed 与明确要求的旧数据解码/迁移不是功能兜底，但必须保持最窄范围，不能演化成备用产品实现。
- 只有用户针对 exact 依赖、exact 范围和退出条件作出的新明文决定才能例外。

## 当前产品边界

Dashis 当前是 macOS 原生 SwiftUI、provider-first 的 AI 用量 dashboard，不使用 `WKWebView`、Web dashboard、Node gateway 或 localhost 业务服务。

- Xcode 工程包含三个 target：macOS App `Dashis`、命令行 helper `ClaudeStatusLineHelper`、测试 target `DashisTests`。
- shared scheme `Dashis` 构建 App 与 helper，并在 Test action 运行 `DashisTests`。
- Dashboard 固定展示 Codex、Claude、Google AI、OpenRouter 四个内置 provider；Settings 只展示 runtime 与这四个 provider，不提供动态 Add provider。
- `script/build_and_run.sh` 是本地 build/run 入口；`.codex/environments/environment.toml` 的 Run action 调用同一脚本。
- 当前没有 iOS target、后端、数据库、长期凭据存储或部署配置；这些边界仍为 `UNKNOWN`。

## 目标分层

```text
DashisApp / DashboardView
  -> DashisProviderStore
       UI state / session-only inputs / explicit user actions
       generation guards / Clear / snapshot-to-card projection
  -> DashisProviderService
       composition root only
       -> CodexUsageClient
       -> ClaudeUsageClient
       -> GoogleConsumerUsageClient
       -> GeminiAPIProjectUsageClient
       -> OpenRouterUsageClient
       -> Google ProviderConnectionCoordinator
       -> OpenRouter ProviderConnectionCoordinator

Shared provider foundation
  -> ProviderSnapshot / QuotaWindow / ProviderBalance / ProviderMetric
  -> ProviderCardProjection / FreshnessPolicy
  -> ProviderJSON
  -> ProviderHTTPClient -> ProviderEndpointPolicy
  -> LoopbackOAuthCoordinator / ProviderOAuthSupport
```

`DashisProviderService` 不解析 provider 响应、不持久化凭据，也不定义 endpoint；它只组装 adapter 和连接协调器。各 adapter 先生成结构化 `ProviderSnapshot`，Store 再统一投影为卡片与详情内容，避免把 UI 文案当成数据模型。

## 统一 snapshot 语义

`ProviderSnapshot` 包含 provider、scope、source、采集时间、quota windows、balance、metrics、warnings 和 partial failures。来源级别必须保留到 UI：

| source | 含义 | 当前示例 |
|---|---|---|
| `officialDirect` | 官方接口直接返回值 | OpenRouter key/account 数据、Codex Enterprise Analytics |
| `officialDerived` | 官方 limit 与 usage 经过严格匹配后推导 | Gemini API project quota |
| `officialLocalBridge` | 官方本地程序把字段交给用户命令 | Claude Code `statusLine.rate_limits` |
| `experimentalPrivate` | 非公开、可能失效的只读契约 | Codex personal `wham` |
| `manualOnly` | 没有受支持的第三方机器接口 | Google consumer subscription |

Freshness 由 snapshot 是否有数据、`observedAt` 与 source TTL 共同决定。没有可信数据时必须显示 `No data` 或 manual 状态。原始 negative remaining 和超过 100% 的 used 值保留；只有进度条投影被限制在 `0...100`。

## Provider 数据链路

### Codex

```text
用户点击 Check desktop usage
  -> 安全读取 ~/.codex/auth.json
     regular file / O_NOFOLLOW / current UID / private permissions / <= 1 MiB
  -> GET chatgpt.com/backend-api/wham/usage
  -> GET chatgpt.com/backend-api/wham/rate-limit-reset-credits
  -> experimentalPrivate snapshot；两个请求允许部分成功

用户点击 Check workspace analytics
  -> session-only analytics key + workspace ID
  -> GET api.chatgpt.com/v1/analytics/codex/workspaces/{workspace}/usage
  -> 每页最多 500 条，最多 100 页
  -> officialDirect workspace metrics
```

Personal `wham` 不是公开稳定 API，失败时 fail closed，不能触发登录刷新、额度重置、兑换或其它副作用。Enterprise Analytics 是组织 workspace 聚合使用量，不等同于个人订阅 remaining。

### Claude

```text
用户点击 Preview connect
  -> 验证 App bundle 中的 helper 与计划安装路径
  -> 安全读取 ~/.claude/settings.json
  -> 生成字段级 statusLine patch 摘要（无持久写入）

用户点击 Apply change
  -> 安装/更新私有目录中的 helper
  -> 再校验 settings fingerprint
  -> 原子写入 statusLine patch

Claude Code 后续调用 statusLine
  -> dashis-claude-statusline 接收原始 stdin
  -> 仅提取 5-hour / 7-day used_percentage 和 resets_at
  -> 原子写入 <= 8 KiB、0600 的净化 snapshot
  -> 若原先已有 statusLine command，将同一份 stdin 传给原命令并转发 stdout/stderr/exit status

用户点击 Reload snapshot
  -> ClaudeUsageClient 安全读取净化 snapshot
  -> officialLocalBridge snapshot
```

helper product `dashis-claude-statusline` 嵌入 `Dashis.app/Contents/MacOS/`。Preview 只验证它并准备指向预定私有路径的 patch；用户确认 Apply 后才安装或更新到 `~/Library/Application Support/com.vitemis.dashis/ClaudeBridge/bin/`，随后原子修改 settings。snapshot 位于同一 `ClaudeBridge` 根下的 `snapshot.json`。

缺少 `rate_limits` 时 helper 不覆盖旧 snapshot；单窗口更新会保留另一窗口；完全相同的窗口不会刷新 `observedAt`。Dashis 不主动发送 Claude 请求，真实更新依赖 Claude Code 后续产生响应。Preview disconnect + Apply 会恢复原 statusLine 并删除安全校验通过的 snapshot；`Clear loaded data` 只清 snapshot，不改变 bridge 配置。

### Google AI

Google provider 有两个互斥 mode，切换 mode 会清除该 provider 当前 mode 的临时状态和展示数据。

Consumer subscription：

- 没有受支持的第三方余额 API；`Open Gemini official page` 只打开官方页面。
- 用户可选填 used/limit/remaining/unit 并记录带采集时间的 manual snapshot。
- 不读取浏览器 Cookie、profile、Keychain、Gemini/Antigravity 私有 token 或 TUI 输出。
- Antigravity 的 quota/credits 由用户在其 CLI 中输入 `/credits` 人工查看，不由 Dashis 抓取。

Gemini API project：

```text
用户输入 Google Desktop OAuth client ID、project ID/number 与可选 exact quota IDs
  -> 默认浏览器打开 accounts.google.com
  -> 随机 127.0.0.1 port + 随机 callback path
  -> PKCE S256 + state + cloud-platform scope
  -> POST oauth2.googleapis.com/token
  -> 仅内存保存短期 access token；丢弃 refresh_token / id_token
  -> GET cloudquotas.googleapis.com/v1/projects/{project}/locations/global/
         services/generativelanguage.googleapis.com/quotaInfos
  -> GET monitoring.googleapis.com/v3/projects/{project}/timeSeries
  -> FULL point 分页按完整 metric/resource labels 合并
  -> 按 quota ID、limit_name、dimension/model/location 和 metric type 严格匹配
  -> minute/hour DELTA 选最新完整可见历史窗并标 exact as-of
  -> concurrent GAUGE 取最新；未知 cadence 不计算 remaining
  -> officialDerived snapshot
```

Project ID/number 由用户手工输入；当前实现不枚举项目。可选 quota ID 取 Cloud Quotas 的 exact `quotaId`，逗号/空白分隔；留空时按受支持 cadence 优先并最多自动选 24 个 definition，防止无界 Monitoring fan-out。授权账户还必须具备 `cloudquotas.quotas.get` 和 `monitoring.timeSeries.list` 所需 IAM 权限。Cloud Monitoring 可能约延迟 150 秒；minute/hour 不能把请求时刻切开的 DELTA 当完整窗口，故选择最新完整公共历史窗并把 as-of 写进 window label 与 warning。RPD 重置按 `America/Los_Angeles` 日历午夜；limit 与 usage 不能可靠对齐、Cloud Quotas 与 Monitoring limit 冲突或 cadence 未知时必须显示 unavailable/警告，不能猜测。

### OpenRouter

OpenRouter 有默认 OAuth key mode 与 Advanced management key mode。

默认 OAuth：

```text
用户点击 Connect OpenRouter
  -> 默认浏览器打开 https://openrouter.ai/auth
  -> 随机 127.0.0.1 port + 随机一次性 callback path
  -> PKCE S256（OpenRouter 官方 OAuth 契约没有 state 参数）
  -> POST /api/v1/auth/keys 换取用户控制的 API key
  -> session-only key
  -> GET /api/v1/key
  -> officialDirect key limit / usage / limit_remaining
```

OpenRouter 官方 OAuth 授权 URL 没有定义 `state`，因此实现不伪造 provider 未接受的 state；callback 的隔离依赖高熵随机 path、只绑定 `127.0.0.1`、精确 path 校验、一次性 listener 与 PKCE verifier。Google OAuth 仍使用并严格校验 state。

Advanced management：

- session-only management key 并发查询 `/api/v1/credits`、`/api/v1/activity`、`/api/v1/analytics/meta`、`/api/v1/analytics/query` 和可选 `/api/v1/generation?id=...`。
- analytics 先读取 meta，只选择实际可用且 `is_rate == false` 的可加总 metric/dimension；`metadata.truncated` 时自动缩小时间窗一半重试一次，并明确显示较窄口径或仍不完整警告。
- 每个子请求保留独立 partial failure，不因一个失败抹掉其它有效结果。
- rate/token metric 分别保留 provider 返回的意义；不得把不同日期、模型或 endpoint 的 rate 相加成一个伪造速率。
- total token 优先 provider 的 `total_tokens`，缺失时使用 prompt + completion；reasoning 只作 output breakdown，不再次相加。

`Clear` 会取消本地 listener/task 并清除 app 内的 key、verifier、输入与 snapshot，但无法保证撤销已经由 `/auth/keys` 在 OpenRouter 服务端创建的 key。若授权完成后状态不确定，用户必须在 OpenRouter 官方账户页面撤销该 key。

## OAuth 与网络安全边界

- OAuth 使用系统默认浏览器，由 `NSWorkspace` 打开 provider 授权 URL；不是 `ASWebAuthenticationSession`。
- loopback listener 只绑定随机 `127.0.0.1` 端口，callback path 含随机 nonce；不绑定 `localhost`、IPv6 或外部接口。
- Google 和 OpenRouter 分别使用独立 `ProviderConnectionCoordinator`；Clear 一个 provider 不应取消另一个 provider 的连接。
- 所有远端数据请求经 `ProviderHTTPClient`；配置为 ephemeral、无 cache、无 cookie、无 credential store，响应上限 8 MiB。
- 远端 redirect 一律拒绝；POST token/code exchange 不重试，只有 GET/HEAD 可对有限的 429/502/503/504 或瞬时网络错误重试一次。
- `ProviderEndpointPolicy` 校验 HTTPS、标准端口、精确 host/path/method/query/body schema，并拒绝 embedded credentials、fragment、trailing slash 与未允许字段。
- 错误只进入净化摘要；不显示 Authorization、key、code、verifier、完整请求/响应或账号标识。

## 状态生命周期

所有远端检查由用户显式动作触发。Store 使用每-provider generation 和 operation ID：切换 mode、Clear 或开始新动作后，旧异步响应不能重新写回 UI。Google access token、OpenRouter OAuth key/management key、Codex Enterprise analytics key 与 PKCE/OAuth 中间状态只存在于当前 App session。

Claude 是唯一允许事件驱动写入本地净化 snapshot 的 bridge；该文件只含白名单 quota 字段，不是凭据或完整 provider 响应。当前没有 refresh token 或 API key 的跨启动持久化；未来若引入 Keychain，必须作为独立凭据政策变更评审。

## UI 与设计边界

- Sidebar 固定为 Dashboard、Codex、Claude、Google AI、OpenRouter 与底部 Settings。
- Dashboard 只展示四张大 provider card；完整 windows、metrics、warnings 和 partial failures 在 provider detail 展示。
- 主题保持 macOS 系统白/黑，语义色只表达 connected/watch/incident；英文与数字维持 serif typography。
- 不重新引入装饰性品牌块、subtitle 堆叠、Recent monitors、timeline、旧 Models/Runs/Alerts、首页小指标网格或 inspector-first 布局。

## 未确认架构

- iOS target、跨 Apple 平台共享代码与移动端 OAuth/Claude bridge：`UNKNOWN`。
- 后端/BFF、数据库、通知、定时刷新、长期历史与 dashboard 业务 KPI：`UNKNOWN`。
- OpenRouter/Google refresh token 是否可持久化到 Keychain：未批准；当前明确不持久化。
- Google consumer 若未来发布官方第三方余额 API、Codex personal 若未来发布公开 quota API：需要重新研究和安全评审，不能自动沿用当前 manual/private 路径。
