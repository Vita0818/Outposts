# Dashis 使用教程

本文面向在本机运行和验收 Dashis 的用户。Dashis 当前是 macOS 原生 SwiftUI dashboard，不是网页、WebView 或 localhost gateway 包装。

## 现在能做什么

Dashis 固定提供四个 provider：

| Provider | 当前模式 | 数据性质 |
|---|---|---|
| Codex | Personal desktop；Enterprise workspace analytics | Personal 为非公开实验接口；Enterprise 为官方 workspace usage |
| Claude | Claude Code statusLine 本地 bridge | 官方本地字段的净化 snapshot |
| Google AI | Consumer subscription；Gemini API project | Consumer 人工查看；Project 由官方 Cloud API 推导 |
| OpenRouter | OAuth key；Advanced management key | 官方 key limit 或账户级 credits/activity/analytics |

Settings 只显示 native runtime 与这四个 provider；当前不能动态 Add provider。iOS、后端、长期历史、通知、自动定时刷新和跨启动凭据仍未实现。

## 先理解 source 与 freshness

每张卡都会诚实标注数据来源：

- `Official`：官方接口直接返回。
- `Official · Estimated`：用官方 limit 与 usage 严格匹配后推导。
- `Official · Local`：Claude Code 官方 statusLine 通过本地 bridge 提供。
- `Experimental`：Codex personal 的非公开只读 endpoint，可能随时变化。
- `Manual check`：没有受支持的第三方机器接口，由用户人工查看或录入。

Freshness 显示 `Updated now`、`Stale`、`Expired` 或 `No data`。没有可信数据时不会生成一个看似实时的百分比。若 remaining 为负数，Dashis 会如实显示超额；只有进度条被限制在可绘制范围。

## 启动 Dashis

在项目根目录运行：

```sh
./script/build_and_run.sh
```

需要同时确认 App 已启动并保持运行时：

```sh
./script/build_and_run.sh --verify
```

脚本会停止旧的 Dashis 进程，构建 macOS Debug App，准备生成 bundle 的 xattr，再通过 LaunchServices 打开。构建使用 `ENABLE_DEBUG_DYLIB=NO`，避免临时 App 依赖 Xcode debug dylib/stub executor。Codex App 中的 Run action 调用同一脚本。

## 界面导览

启动后应看到：

- Sidebar：Dashboard、Codex、Claude、Google AI、OpenRouter，以及固定在底部的 Settings。
- Dashboard：四张 provider 大卡；卡片用于摘要，不显示旧 mock telemetry、runs 或 Web 内容。
- Provider detail：完整 quota windows、metrics、warning、partial failure，以及该 provider 的显式操作。
- Settings：`Native runtime` 和固定四-provider 列表，没有 Add provider。

Dashboard 卡片上的主按钮会执行当前 mode 的主要动作；更完整的配置和清理按钮在 provider detail。

## Codex

### Personal desktop usage

1. 确认当前 macOS 用户已经在 Codex Desktop/CLI 使用同一套本地登录材料。
2. 打开左侧 `Codex`。
3. 点击 `Check desktop usage`。

只有点击后，Dashis 才会安全读取本机 `~/.codex/auth.json` 并访问两个只读 endpoint：

```text
https://chatgpt.com/backend-api/wham/usage
https://chatgpt.com/backend-api/wham/rate-limit-reset-credits
```

这两个 endpoint 不是公开稳定的 Codex quota API，因此 UI 标为 `Experimental`。usage 与 reset credits 是独立请求；其中一个失败时，另一个已验证结果仍应显示，并列出 partial failure。Dashis 不会刷新登录、写 auth、重置额度或触发 Codex 任务。

若本地 auth 不存在、是 symlink、所有者/权限不安全、过大或格式不支持，界面只显示净化错误，不显示文件内容或 token。

### Enterprise workspace analytics

在同一页面输入：

- `workspace id`。
- 具有 `codex.enterprise.analytics.read` scope 的 analytics API key。
- 1–90 天的 Analytics window。

点击 `Check workspace analytics`。Dashis 会分页读取官方 workspace usage，每页最多 500 条、最多 100 页。它展示的是组织 workspace 聚合 activity/turn/token，不是个人订阅 remaining。

Analytics key 只在当前 App 内存中存在。点击 `Clear` 会清空 Codex 输入与已加载 snapshot；退出 App 后也不会保留。不要把真实 key 写入文档、截图、issue、测试或日志。

## Claude

### 连接本地 bridge

Claude quota 来自 Claude Code 官方 `statusLine.rate_limits`。Dashis 不读取 Claude auth、Cookie、transcript，也不会为了刷新额度自动发送 Claude 请求。

1. 打开左侧 `Claude`。
2. 点击 `Preview connect`。
3. 阅读 `Pending settings change` 摘要。
4. 确认后点击 `Apply change`；不接受则点 `Cancel`。

需要特别理解 Preview 与 Apply 的差别：

- `Preview connect` 只验证 App bundle 中的 `dashis-claude-statusline`、安全读取 `~/.claude/settings.json` 并生成字段级预览；它不安装文件，也不修改 settings。
- `Cancel` 不产生持久改动。
- 只有 `Apply change` 会安装/更新私有 helper、再次校验 settings 是否被并发修改，并原子写入 statusLine 配置。
- 如果用户原本已有受支持的 statusLine command，Dashis 会把它链在 helper 后面，保留同一 stdin、stdout、stderr 与退出状态。

helper 和 snapshot 默认位于：

```text
~/Library/Application Support/com.vitemis.dashis/ClaudeBridge/bin/dashis-claude-statusline
~/Library/Application Support/com.vitemis.dashis/ClaudeBridge/snapshot.json
```

snapshot 只保存 schema version、采集时间，以及 5-hour/7-day 的 used percentage/reset time；不保存 cwd、session、transcript、repo、model、cost、email 或原始 JSON。

### 获取和刷新 Claude 用量

Apply 成功后：

1. 在 Claude Code 中正常产生至少一次模型响应。
2. 回到 Dashis 点击 `Reload snapshot`。

若 Claude Code/订阅提供 `rate_limits`，Dashis 会显示 5-hour 和/或 7-day window，并计算 remaining = 100 - used。一个窗口可能单独缺失；没有 `rate_limits` 的 statusLine 事件不会清除旧值，也不会把旧数据重新标成刚更新。

本地 snapshot 超过 15 分钟显示 stale，超过 24 小时显示 expired。产生新的 Claude Code 响应后再 Reload；Dashis 不会替你发送请求。

### 清除与断开

- `Clear loaded data`：只删除经过安全校验的净化 snapshot，不修改 Claude statusLine；bridge 仍保持连接。
- `Preview disconnect`：只生成恢复预览。
- `Preview disconnect` 后 `Apply change`：恢复连接前的 statusLine（没有旧 command 时移除 Dashis statusLine），并删除安全 snapshot。

如果 settings 在 Preview 与 Apply 之间变化，Apply 会拒绝覆盖；重新 Preview 后再决定。

## Google AI

Google 页面顶部有两个互斥 mode。切换 mode 会取消当前 Google OAuth 操作并清除当前展示的 Google snapshot；必要时需要重新连接/检查。

### Consumer subscription

Google 没有提供让第三方 App 读取 Gemini consumer subscription 剩余量的受支持 API，因此此模式始终是人工流程：

1. 点击 `Open Gemini official page` 在默认浏览器打开 Gemini 官方页面。
2. 若使用 Antigravity CLI，在其终端输入 `/credits` 查看官方 quota/credits。
3. 可选：把 `manual used`、`manual limit`、`manual remaining` 与 `unit` 填入 Dashis。
4. 点击 `Record manual reading`。

所有数值都可留空；有值时必须是有限数字。若 used + remaining 与 limit 不一致，Dashis 会原样显示并给出警告，不会偷偷修正。manual reading 带当前采集时间，source 始终为 `Manual check`，不会自动更新。

Dashis 不抓 Gemini/Antigravity 网页 DOM、Cookie、browser profile、Keychain、私有 OAuth 文件、TUI 输出或未公开 endpoint。

### Gemini API project

准备条件：

- 在 Google Cloud 项目中启用 Cloud Quotas 与 Cloud Monitoring 所需 API。
- 创建 OAuth client 类型为 Desktop app，并取得 client ID；不要在 Dashis 中输入 client secret。
- 当前授权主体对目标项目具备可执行 `cloudquotas.quotas.get` 与 `monitoring.timeSeries.list` 的 IAM 权限。
- 已知目标 Google Cloud project ID 或 project number。Dashis 当前不会列出项目，必须手工输入。

操作步骤：

1. 切换到 `Gemini API project`。
2. 输入 `Google Desktop OAuth client ID`。
3. 输入 `Google Cloud project ID or number`。
4. 可选：在 `optional quota IDs, comma-separated` 输入一个或多个 Cloud Quotas exact `quotaId`；逗号、空格或换行都可分隔。
5. 点击 `Connect Google`。
6. 在系统默认浏览器完成 Google 授权。
7. Dashis 连接成功后会自动检查一次；之后可点击 `Check quotas` 重查。

quota ID 可从目标项目的 Cloud Quotas 控制台或官方 `quotaInfos.list` 响应中的 `quotaId` 字段取得，不要填 display name。留空时 Dashis 会按受支持 cadence 优先、稳定排序，最多自动选择 24 个 definition；输入 exact ID 可进一步缩小 Monitoring 请求范围。

授权只请求 `https://www.googleapis.com/auth/cloud-platform` scope，使用随机 `127.0.0.1` loopback port/path、PKCE S256 与 state。access token 只在当前 App session 保留；refresh token 和 ID token 被丢弃。浏览器本来已登录只会减少登录步骤，不代表 Dashis 已自动获得授权。

Dashis 从 Cloud Quotas 读取有效 limit，再从 Cloud Monitoring 读取对应 limit/usage series。只有 quota ID、`limit_name`、dimension/model/location、metric type 和窗口可可靠匹配时才计算 remaining；否则显示 unavailable/警告。region/zone 会与 Monitoring 的 exact location label 对齐。minute/hour 会选择最新完整且已可见的历史窗口；巨型主值会附 `historical`，badge 显示 `Historical`，caption/window/warning 显示 exact `as of`。它不是当前实时分钟余额。RPD 按 `America/Los_Angeles` 日历午夜重置。

Cloud Monitoring 通常可能延迟约 150 秒，因此刚产生的请求不一定立即出现。Project 结果标为 `Official · Estimated`，不是 provider 直接返回的余额。

点击 `Clear` 会取消 Google 的本地 OAuth listener和在飞请求，清除 access token、client/project/quota-ID/manual 输入与 snapshot；不会撤销 Google 账户中的其它授权或修改项目 IAM。

## OpenRouter

OpenRouter 页面有 `OAuth key` 与 `Management key` 两个 mode。切换 mode 会取消当前 OpenRouter OAuth 操作、清除该 provider 的临时 key 和 snapshot；必要时重新连接或重新输入 management key。

### 默认 OAuth key

1. 保持 `OAuth key` mode。
2. 点击 `Connect OpenRouter`。
3. 在系统默认浏览器中确认授权。
4. 回到 Dashis 查看 key limit/usage/remaining；之后可点击 `Check key limit` 重查。

Dashis 使用随机 `127.0.0.1` callback port 和一次性随机 path，加上 PKCE S256。OpenRouter 官方 OAuth 授权参数没有 `state`，因此 Dashis 不发送伪造 state；callback 的隔离依赖随机 path、严格本机绑定、一次性 listener、精确 path 校验与 PKCE verifier。

授权 code 会通过 `POST /api/v1/auth/keys` 换成用户控制的 OpenRouter API key，再用 `GET /api/v1/key` 读取 key-level limit、usage、`limit_remaining`、reset/expiry。key 只在当前 App session 内存中存在。

点击 `Clear` 会取消本地 listener/task，清 key、verifier、输入与 snapshot，但它不能保证撤销已经在 OpenRouter 服务端创建的 key。如果浏览器端已经批准，而 Dashis 随后取消、崩溃、超时或状态不确定，请到 OpenRouter 官方账户/API keys 页面手工 revoke 对应 key。

### Advanced management key

只有需要账户级数据时才切换到 `Management key`：

1. 输入临时 `management API key`。
2. 可选输入 `generation id`。
3. 选择 1–90 天 Analytics window。
4. 点击 `Check management data`。

此模式查询账户 credits、过去 activity、beta analytics meta/query 和可选 generation detail。analytics 先读取 meta，只汇总服务端标为非 rate 的可加总 metric。若结果 `truncated`，Dashis 会自动把时间窗缩小一半重试一次并明确标注较窄口径；若重试仍被截断，UI 保持不完整警告，用户可继续缩短 Analytics window 后重查。

理解数值规则：

- negative remaining 会原样显示，不会被钳成 0。
- token total 优先 provider 的 `total_tokens`；缺失时才使用 prompt + completion。
- reasoning 是 output/completion breakdown，不会再次加进 total。
- 不同日期、模型或 endpoint 的 rate 不会被相加成伪造的账户总 rate。
- credits、activity、analytics、generation 任一失败时，其它成功结果仍会保留，并显示 partial failure。

Management key 同样只在当前 App 内存中存在，`Clear` 后清空；不要把真实 key 写入文档、截图、issue、fixture 或日志。

## 凭据与 Clear 的共同规则

- 所有网络检查都来自用户显式点击；Dashis 不做后台定时 provider 请求。
- Codex Enterprise key、OpenRouter key、Google access token、OAuth state/PKCE verifier 都不会写入 UserDefaults、仓库、文档、日志或 Keychain。
- Clear 会使当前 provider 正在执行的旧响应失效，防止它稍后重新填回已清空的 UI。
- Claude 净化 snapshot 是唯一受控的本地用量文件；它不包含凭据，并可由 Clear/Disconnect 删除。
- 遇到错误时只分享净化后的错误类别。不要复制真实 Authorization、账号 ID、完整 request/response 或 provider 页面中的私人数据。

## 手动验收清单

- `./script/build_and_run.sh --verify` 构建并启动成功。
- Sidebar 和 Settings 恰好只有固定四 provider，没有 Add provider。
- Dashboard 没有 WebView、网页、Node gateway、旧 mock telemetry 或 runs。
- no-data 卡显示 `No data`，各 provider source badge 与真实路径一致。
- Codex personal 为 Experimental；Enterprise 是 workspace usage，不冒充个人 remaining。
- Claude Preview 无持久写入，Apply 才安装 helper并改 settings；已有 statusLine 连接/断开后能恢复。
- Google consumer 只人工查看；Project mode 显示 Estimated 和约 150 秒延迟警告。
- OpenRouter OAuth 取消、拒绝、超时、key 过期都有净化错误；Clear 后必要时能按指引去服务端 revoke。
- 所有 SecureField/session token 在 Clear 或 App 退出后不可复用；日志和 UI 不泄漏凭据/完整响应。
- light/dark 分别是 macOS 系统白/黑，英文与数字保持 serif。

## 常见问题

### `./script/build_and_run.sh --verify` 失败

查看最近系统日志：

```sh
/usr/bin/log show --style compact --last 2m --predicate 'eventMessage CONTAINS[c] "Dashis" OR eventMessage CONTAINS[c] "com.vitemis.dashis" OR eventMessage CONTAINS[c] "AppleSystemPolicy" OR eventMessage CONTAINS[c] "AMFI"'
```

如果出现 AppleSystemPolicy/AMFI 拒绝，确认脚本仍使用 `ENABLE_DEBUG_DYLIB=NO`，并执行生成 bundle 的 provenance/quarantine xattr 准备。

### Xcode Console 出现 logging timeout

shared scheme 已设置 `IDEPreferLogStreaming=YES`。`Failed to initialize logging system due to time out` 不一定代表 App 崩溃；同时检查 Dashis 进程和系统 crash/AMFI 日志。

### Claude 一直没有数据

- 确认已 Preview 并 Apply，而不是只完成 Preview。
- 确认 Claude Code 版本/订阅会提供 statusLine `rate_limits`。
- Apply 后在 Claude Code 产生一次真实响应，再回 Dashis Reload。
- `Clear loaded data` 不会断开 bridge；如果曾 Disconnect，需要重新 Connect + Apply。

### Google Project 显示权限错误或旧数据

- 检查 project ID/number 是目标项目，不是 display name。
- 检查当前授权主体具备 `cloudquotas.quotas.get` 与 `monitoring.timeSeries.list`。
- 检查相关 API 已启用。
- 等待约 150 秒再重查 Monitoring；仍无法精确匹配时 Dashis 会保持 unavailable，而不会猜测。

### OpenRouter 授权后仍未连接

- 确认浏览器回调到本机 `127.0.0.1` 没有被代理/防火墙拦截。
- 重新点击 Connect，使用新的随机 callback/PKCE session。
- 如果浏览器已经批准但 App 状态不确定，先到 OpenRouter 官方账户页检查并 revoke 多余 key。

### Codex personal 突然失效

Personal `wham` 是非公开契约，可能改变。不要通过放宽 endpoint、复制 Cookie 或刷新登录来绕过；保留 Experimental/fail-closed 状态，等待重新研究公开方案。

## 教程维护规则

任何变更只要影响启动、构建、Run action、Dashboard/sidebar/detail/Settings、provider 接入、凭据生命周期、endpoint allowlist、验证或排障，就必须同步更新本文。未更新时必须在最终报告说明原因。
