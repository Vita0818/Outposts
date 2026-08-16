# CURRENT_STATE

## 当前状态

- 项目名：Dashis；独立 Git root 为 `/Users/vita/Vitemis/Dashis`，远程 `origin` 为 `https://github.com/Vita0818/Dashis.git`。
- 当前产品是 macOS 原生 SwiftUI provider-first dashboard；没有 WebView、网页入口、Node gateway 或旧 mock telemetry/runs。
- `Dashis.xcodeproj` 当前包含三个 target：macOS App `Dashis`、命令行 helper `ClaudeStatusLineHelper`、单元测试 `DashisTests`。shared scheme `Dashis` 会构建 App/helper 并运行测试 target。
- `script/build_and_run.sh` 是本地 build/run 入口；`.codex/environments/environment.toml` 的 Run action 调用该脚本。
- 当前只实现 macOS；iOS target 仍为 `UNKNOWN`。

## 已实现能力

### 统一 provider 模型

- Dashboard 固定展示 Codex、Claude、Google AI、OpenRouter 四张内置卡片，侧边栏提供对应详情页，Settings 固定在底部。
- 所有 adapter 返回 `ProviderSnapshot` / `QuotaWindow`，UI 同时展示 source、scope、observed time、Fresh/Stale/Expired 和 partial failure。
- 数据来源分为 `officialDirect`、`officialDerived`、`officialLocalBridge`、`experimentalPrivate`、`manualOnly`；UI 不把推导值、私有 endpoint 或手动值伪装成官方实时余额。
- 原始 remaining 允许为负数；仅进度条投影到 `0...100`。Dashboard 卡只展示摘要，完整窗口/警告留在 provider detail。

### Codex

- Personal：只有用户点击检查时才以普通文件、非 symlink、当前 UID、大小与权限约束读取 `~/.codex/auth.json`；调用两个非公开 `wham` endpoint，来源标为 `Experimental`。
- Personal usage 与 reset credits 独立请求；其中一个失败时保留另一个结果并展示 partial failure。
- Enterprise：使用用户临时提供的 analytics API key 与 workspace ID 调用官方 Codex Analytics usage endpoint，最多 100 页、每页 500 条，来源标为 `Official`。
- API key/token 不写入磁盘、UserDefaults、Keychain、日志、fixture 或文档。

### Claude

- 用户显式点击 `Preview connect` 后，Dashis 只验证 bundled helper、读取 `~/.claude/settings.json` 并展示字段级变更摘要，不做持久写入；第二次 `Apply change` 才安装/更新私有 App Support helper 并写入经过复核的 Claude settings patch。
- 独立 helper `dashis-claude-statusline` 接收 Claude Code 官方 statusLine JSON，只保存净化后的 schema version、observed time、5-hour/7-day used percentage 与 reset time。
- helper 保留并执行用户原有 statusLine command，向其传递完全相同的 stdin，并转发 stdout、stderr 与退出状态。
- snapshot 最大 8 KiB，要求普通文件、当前 UID、私有权限；缺少 `rate_limits` 不覆盖旧值，单窗口可独立更新，毫秒 epoch 和越界百分比被拒绝。
- Disconnect 恢复原 statusLine；Clear/Disconnect 可删除经过验证的净化 snapshot。Dashis 不会为了刷新配额自动发送 Claude 请求。

### Google AI

- Consumer subscription：没有受支持的第三方余额 API，卡片保持 `Manual check required`；用户可显式打开 Gemini 官方页面，或录入带采集时间的手动值，来源标为 `Manual check`。
- Consumer 模式不读取浏览器 Cookie、profile、Gemini/Antigravity 私有 OAuth 文件、Keychain 或 TUI 输出。
- Gemini API project：用系统默认浏览器发起 Google Desktop OAuth，使用随机 `127.0.0.1` loopback callback、PKCE S256 与 state；project ID/number 由用户手工输入，只保留 session access token，丢弃 refresh token 与 ID token。
- Project 模式分页读取 Cloud Quotas `QuotaInfo`，再用 exact metric type 查询 Cloud Monitoring；按 quota dimensions/model/location 匹配，usage 可跨 method 聚合。可选 quota ID 输入支持逗号或空白分隔；留空时自动选择被限制为最多 24 个 quota definition，Monitoring 的 FULL point 分页会先按完整 labels 合并。
- DELTA 求和、GAUGE 取最新值；分钟/小时 quota 选择最新完整且已可见的公共历史窗口，并在主值、`Historical` badge、caption、window label/warning 显示历史语义与 exact as-of，绝不冒充当前实时窗口；未知 refresh interval 不计算 remaining。RPD 使用 `America/Los_Angeles` 日历午夜，结果标为 `Official · Estimated` 并显示约 150 秒 Monitoring 延迟。
- 授权主体需要可执行 `cloudquotas.quotas.get` 与 `monitoring.timeSeries.list`；Consumer 的 Antigravity quota/credits 只提供 CLI `/credits` 人工查看指引。

### OpenRouter

- 默认模式使用官方 OAuth PKCE，在默认浏览器与随机 `127.0.0.1` callback 上取得用户控制的 API key，再以 `/api/v1/key` 读取 key limit/usage/remaining；官方 OpenRouter flow 没有 state，当前实现以高熵随机 callback path、精确 path 校验和 PKCE 隔离会话；key 仅存当前 app session。
- Advanced management 模式使用临时 management key，独立查询 credits、activity、analytics meta/query 和可选 generation；单项失败不会抹掉其它数据。
- Analytics 请求先读 `/analytics/meta` 再选择实际存在且可加总的 metric/dimension；rate metric 被排除。`metadata.truncated` 时自动把时间窗缩小一半重试一次，并明确显示实际采用的较窄窗口或仍不完整警告。
- remaining 不钳制负值；token total 优先官方 `total_tokens`，fallback 为 prompt + completion，reasoning 只作 output breakdown，不重复相加。
- 不把不同日期、模型或 endpoint 的 rate metric 相加成伪造总 rate。`Clear` 会清除 Dashis 本地 OAuth/key 状态，但若服务端可能已经创建 key，用户仍需在 OpenRouter 官方账户页 revoke。

### 安全网络与验证

- `ProviderHTTPClient` 使用 ephemeral `URLSessionConfiguration`，禁用 cache、cookie 与 credential store；所有 redirect 均拒绝，429/502/503/504 与有限瞬时网络错误最多重试一次。
- `ProviderEndpointPolicy` 同时校验 HTTPS、精确 host/path、method、query、body schema 与端口；OAuth 授权 URL 和 localhost callback 另有严格校验。
- `Clear` 会失效当前 provider generation、关闭活动 OAuth listener，并清除临时 key/token、输入、PKCE/OAuth 会话引用与内存 snapshot，避免迟到响应重新写回。
- `DashisTests` 当前有 66 个纯合成、离线测试，覆盖 decoder、数值溢出与负 remaining、reasoning、analytics metadata、allowlist、PKCE、Claude 净化/settings 恢复、Google quota 推导与 freshness；不访问真实账户。

## 当前验证状态

- `plutil -lint Dashis.xcodeproj/project.pbxproj`：通过。
- `xcodebuild -list -project Dashis.xcodeproj`：可发现 `Dashis`、`ClaudeStatusLineHelper`、`DashisTests`。
- macOS Debug build：通过，helper 被嵌入 `Dashis.app/Contents/MacOS/dashis-claude-statusline`。
- `xcodebuild test`：66/66 通过；所有测试均为离线合成 fixture，不读取真实 provider 数据。
- `script/build_and_run.sh --verify`：本轮沙箱外启动验证的授权请求因 Codex 使用额度被拒，未运行；这是验证环境限制，不是 build/test 失败。
- 真实 provider 账户仍需用户在 UI 中主动授权后人工验收；自动测试不会读取任何真实凭据。

## 未确认

- iOS target、共享代码边界与移动端 OAuth/bridge 方案。
- dashboard 的长期用户角色、业务 KPI、刷新调度、通知、数据保留和后端需求。
- 是否允许未来将 OpenRouter/Google refresh token 持久化到 Keychain；当前明确不持久化。
- Codex personal `wham` 是非公开契约，可能随时变化；失败时必须继续 fail closed。
- Google consumer subscription 若未来发布正式余额 API，需要重新评估，不能沿用网页或 TUI 抓取。

## 工作区注意

- 当前工作树包含用户在本任务前已有的修改与删除；不得清理、回退或覆盖无关改动。
- 未经明确请求，不 add、commit、push 或创建 PR。
- 真实账号数据仅能通过用户显式操作进入当前 app session；不得把凭据、完整响应或私人路径写入仓库。
