# TESTING

## 外部依赖与禁止兜底验证（Vitemis 强制规则）

本项目继承 `/Users/vita/Vitemis/docs/DEPENDENCY_POLICY.md`。涉及外部能力的变更必须验证：

- exact 外部依赖可用时只调用其官方 API/扩展点，不调用第一方重复实现。
- 依赖缺失、版本不兼容或构建/签名/许可证/平台/安全条件不成立时，产生明确、可诊断失败并停止该能力。
- 失败路径不会切换到 legacy、另一 provider/backend、adapter/shim、cache、mock、简化实现或不完整路径。
- 测试 double 只存在于测试 target，不进入 production selection 或 runtime fallback。
- Review 检查新增 wrapper/adapter/facade 是否仅为官方 API 必需的最薄接线；发现核心能力复制、第二实现或静默降级即判定失败。

## 当前测试面

Dashis 当前有三个 Xcode target：

- `Dashis`：macOS SwiftUI App。
- `ClaudeStatusLineHelper`：产物名 `dashis-claude-statusline`，嵌入 App 的 `Contents/MacOS`。
- `DashisTests`：由 shared scheme `Dashis` 的 Test action 运行，test host 为构建后的 Dashis App。

测试源码位于：

```text
tests/DashisTests/ProviderFoundationTests.swift
tests/DashisTests/ProviderDecoderTests.swift
tests/DashisTests/ProviderCorrectnessTests.swift
tests/DashisTests/SecurityBoundaryTests.swift
```

自动测试必须使用合成 fixture、离线运行且不读取真实账户。当前没有 Web/Node 测试入口、package manager、lint/format 工具或 iOS test target。

## 推荐的完整验证顺序

从仓库根目录执行。为了避免生成物进入仓库，示例把 DerivedData 放到系统临时目录：

```sh
pwd
git rev-parse --show-toplevel
git status --short -- .
plutil -lint Dashis.xcodeproj/project.pbxproj
xcodebuild -list -project Dashis.xcodeproj
xcodebuild \
  -project Dashis.xcodeproj \
  -scheme Dashis \
  -configuration Debug \
  -destination 'platform=macOS' \
  -derivedDataPath "${TMPDIR%/}/dashis-tests-derived-data" \
  test
xcodebuild \
  -project Dashis.xcodeproj \
  -scheme Dashis \
  -configuration Debug \
  -destination 'platform=macOS' \
  -derivedDataPath "${TMPDIR%/}/dashis-build-derived-data" \
  ENABLE_DEBUG_DYLIB=NO \
  build
./script/build_and_run.sh --verify
git diff --check
git status --short -- .
```

验收标准：

- `pwd` 与 Git root 都是 `/Users/vita/Vitemis/Dashis`，且没有清理或覆盖用户已有改动。
- `plutil` 通过；`xcodebuild -list` 能发现 `Dashis`、`ClaudeStatusLineHelper`、`DashisTests` 和 shared scheme `Dashis`。
- Test action 0 failure。测试数量会随安全回归用例增长，不把固定数量当成契约。
- Debug build 通过；`Dashis.app/Contents/MacOS/dashis-claude-statusline` 存在并可由 `Bundle.url(forAuxiliaryExecutable:)` 找到。
- `--verify` 能通过 LaunchServices 启动 App，并确认 `Dashis` 进程保持运行。
- `git diff --check` 无空白错误；最终状态只包含任务范围内预期变更和明确保留的用户已有改动。

## 本地 build/run

常规构建并打开：

```sh
./script/build_and_run.sh
```

构建、打开并验证进程：

```sh
./script/build_and_run.sh --verify
```

脚本会停止旧的 `Dashis` 进程，使用 `ENABLE_DEBUG_DYLIB=NO` 构建 Debug App，在临时 DerivedData 中准备 bundle xattr，然后通过 `/usr/bin/open -n` 打开。Codex App 的 Run action 调用同一个脚本。

其它脚本 mode：

```sh
./script/build_and_run.sh --debug
./script/build_and_run.sh --logs
./script/build_and_run.sh --telemetry
```

`--debug` 进入 LLDB；`--logs` 读取 Dashis 进程日志；`--telemetry` 同时过滤 Dashis 进程和 `com.vitemis.dashis` subsystem。日志验证不得包含真实 token、API key、OAuth code/verifier、账号标识或完整 provider body。

## 自动测试覆盖要求

### Provider foundation

- 固定四 provider registry、scope/source/freshness 与 no-data 行为。
- snapshot 到卡片的投影；negative remaining 保留，只有 progress clamp。
- 安全 JSON 数值/布尔/日期转换；非有限值、错误类型和过大整数 fail closed。
- endpoint policy 拒绝非 HTTPS、错误端口、lookalike host、embedded credentials、fragment、trailing slash、dot segment、重复/未知 query、错误 method/content type/body。
- ephemeral HTTP client 拒绝 redirect、限制 response size，只对幂等请求执行有限 retry。
- PKCE verifier/challenge 格式与 loopback callback 约束。

### Codex

- personal usage windows/reset credits decoder；缺少关键 envelope 时 fail closed。
- usage 与 reset 一成一败时保留 partial result。
- Enterprise aliases、分页 token、重复/非法 token、最多 100 页和错误 envelope。
- 本地 auth 文件的 symlink、所有者、权限与 1 MiB 上限保护；测试必须使用临时合成文件，不能读真实 `~/.codex/auth.json`。

### Claude

- 缺少/null `rate_limits`、单窗口、0/100 边界、越界百分比、秒/毫秒 epoch 和 future/stale/expired。
- 相同窗口事件不能刷新 `observedAt`；单窗口更新保守保留另一窗口。
- snapshot 普通文件、UID、0600、8 KiB、schema 与 symlink 保护。
- Connect/Disconnect patch 恢复 prior statusLine、duplicate key、并发 fingerprint 和权限保护。
- helper 端到端保持原 stdin/stdout/stderr/exit status；测试输入必须是合成 JSON，且不能写默认用户 snapshot。

### Google AI

- Consumer manual 空态不虚构 quota；手动值与 manual freshness。
- OAuth 仅有 `cloud-platform` scope、state + PKCE、严格 `127.0.0.1` callback、token body 无 client secret、refresh/ID token 不保存。
- Cloud Quotas 与 Monitoring decoder 的分页与 fail-closed 行为。
- quota ID / `limit_name` / model / location / dimension 精确匹配；更具体维度不能与默认 bucket 重复计数。
- DELTA 最新完整历史窗与 exact as-of、region/zone→location、concurrent GAUGE 最新值、limit 冲突、错误 metric type、未知 cadence、negative remaining。
- RPD 使用 `America/Los_Angeles` 日历午夜。

### OpenRouter

- `/key` 直接 `limit_remaining` 与负值；credits 的负 `total_credits - total_usage`。
- OAuth 授权参数不包含未被官方契约定义的 state；随机 callback path + PKCE 的约束。
- activity/generation 的 `total_tokens` 优先；fallback 只用 prompt + completion，reasoning 不重复相加。
- rate/非可加总 metric 不跨 row 伪造总和。
- analytics meta-driven metric/dimension、metadata row count、`truncated` 警告与非法 schema。
- credits/activity/analytics/generation partial failure 相互隔离。
- `Clear` 或 mode switch 后，迟到响应不能恢复已清除 key/snapshot。

## 手动 UI 验收

### 全局

- Sidebar 恰好显示 Dashboard、Codex、Claude、Google AI、OpenRouter 和底部 Settings；没有 Add provider。
- Dashboard 恰好显示四张大卡；没有 v0.1 mock telemetry、runs、WebView、网页或 Node gateway。
- 卡片与 detail 显示正确 source badge、scope、freshness、warning 与 partial failure；no data 不显示为 fresh。
- 完整 metrics/windows 在 detail 可见；Dashboard 超出摘要数量时用 `more` 提示而不是静默丢失。
- light/dark 分别使用 macOS 系统白/黑；英文与数字保持 serif。
- Settings 的 runtime 行显示 SwiftUI、WebView Not linked、Ephemeral URLSession、127.0.0.1 loopback、opt-in Claude bridge；Providers 只有固定四项。

### Codex

- 未点击前不读取 auth；未登录或 auth 文件不安全时只显示净化错误。
- `Check desktop usage` 能显示可用 windows/reset；其中一个 endpoint 失败时另一结果仍保留并显示 partial failure。
- Workspace ID + `codex.enterprise.analytics.read` key 能触发 Enterprise Analytics；key 不跨重启存在，Clear 后输入与 snapshot 清空。
- UI 始终标注 personal 为 `Experimental`，Enterprise 组织 usage 不显示为个人 remaining。

### Claude

- `Preview connect` 只验证 helper并生成 settings 变更预览；Apply 前 helper 目标路径和 `~/.claude/settings.json` 都不应变化。
- Apply 后，运行一次会返回 rate limits 的 Claude Code 响应，再点 `Reload snapshot`；出现 5-hour/7-day 数据与 `Official · Local`。
- 没有新响应时旧数据按 15 分钟 stale、24 小时 expired，不得因无关 statusLine 事件重新变 fresh。
- 原先已有 statusLine 时仍有相同输出/错误/退出状态。
- `Clear loaded data` 只删除 snapshot，bridge 仍连接；`Preview disconnect` + Apply 恢复原 statusLine 并删除 snapshot。

### Google AI

- Consumer mode 只打开 Gemini 官方页面或接受 manual reading；不自动读取个人 subscription 余额。Antigravity 余额由用户在 CLI 输入 `/credits` 人工核对。
- Project mode 要求 Desktop OAuth client ID 与手工输入的 project ID/number；可选 exact quota IDs 支持逗号/空白分隔，留空最多自动选择 24 个 definition。默认浏览器授权后只在当前 App session 保持连接。
- 缺少 `cloudquotas.quotas.get` 或 `monitoring.timeSeries.list` 时显示净化权限错误，不尝试扩大 scope。
- 成功时显示 `Official · Estimated`、project scope、quota windows 和约 150 秒延迟警告；minute/hour 历史窗的主值、badge、caption 与 as-of 都必须明确历史语义，不能像 live balance。
- Clear 取消 Google listener/请求、清 token/client/project/quota-ID/manual fields 和 snapshot，不应取消正在进行的 OpenRouter 独立 flow。

### OpenRouter

- 默认 OAuth mode 在系统默认浏览器打开 OpenRouter，随机 localhost callback 授权后显示 `/api/v1/key` limit/usage/remaining。
- 取消/拒绝/超时、key 过期或 HTTP 401/403 时显示净化错误并要求重新连接。
- Clear 取消本地 listener并清 session key/snapshot；若浏览器端可能已创建 key，去 OpenRouter 官方账户页面 revoke。
- Management mode 只有用户显式提供 management key 后才查询 credits/activity/meta/query 与可选 generation；key 不持久化。
- negative remaining 如实显示；reasoning 只显示 breakdown；rate metric 不求和；analytics truncated 时自动缩窗重试一次并显示实际口径。
- 任一子请求失败时，其它成功结果仍显示，并列出 partial failure。

## 安全回归检查

- 测试和日志扫描不出现 `Bearer` 后的真实值、API key、OAuth code/state/verifier、account/workspace 私有标识或完整 JSON body。
- 合成 fixture 不从真实响应复制；使用明确虚构的 ID、数值和日期。
- Clear、mode switch、OAuth 取消和 App 退出后，本地 session credential 不可再次使用。
- Claude snapshot 不包含 cwd、session、transcript、repo、email、model、cost 或原始 stdin。
- Google consumer 流程不接触 Cookie/browser profile/Keychain/TUI；Codex/Claude 自动测试不接触用户 home 下真实文件。

## 常见验证故障

### `--verify` 被系统策略拒绝

查看最近系统日志：

```sh
/usr/bin/log show --style compact --last 2m --predicate 'eventMessage CONTAINS[c] "Dashis" OR eventMessage CONTAINS[c] "com.vitemis.dashis" OR eventMessage CONTAINS[c] "AppleSystemPolicy" OR eventMessage CONTAINS[c] "AMFI"'
```

若出现 AppleSystemPolicy/AMFI 拒绝，确认脚本仍执行生成 bundle 的 provenance/quarantine xattr 准备，并使用 `ENABLE_DEBUG_DYLIB=NO`。

### Xcode logging timeout

scheme 的 Run action 已设置 `IDEPreferLogStreaming=YES`。单独的 logging initialization timeout 不一定表示 App 崩溃，应同时检查进程是否运行和系统 crash/AMFI 日志。

### Provider 真实数据缺失

真实 provider 验收只由用户在 UI 显式授权或输入 session credential 后执行；自动测试不会证明账号权限或服务端当前契约。排障时只记录 HTTP 状态类别和净化错误，不复制真实 key、账号 ID 或 response body。

## 文档任务验证

只改文档时至少运行：

```sh
git diff --check
git status --short -- .
```

若影响启动、构建、Run action、UI 流程、provider 接入、凭据、endpoint allowlist、验证或排障，必须同步更新 `docs/USER_TUTORIAL.md`。未运行构建/测试时，最终报告必须明确写“未运行构建/测试”。
