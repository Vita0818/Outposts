# Outposts Codex Supervisor

本文只适用于从 Codex 对话启动的 Agent 模式与 Spark 模式。OpenCode 模式不读取本文。

## 总体定位

Outposts Codex Supervisor 是多项目调度器。它管理 Agent 模式下 DeepCode CLI 的启动、握手、任务分发、输出监测、报告读取、预算控制、状态恢复和主管摘要。

Codex Supervisor 不是 Agent 模式下的开发者、测试者或构建者。实际阅读源码、修改目标项目、运行构建、运行测试、产出每轮项目报告的主体是 **DeepCode CLI**。

Spark 模式例外：当用户明确选择 Spark 模式且模型确认为 `GPT-5.3-Codex-Spark` 时，Codex/Spark 本体可直接读写目标项目并执行验证。

## 五项目并行管理对象

默认调度对象是：

- `/Users/vita/Vitemis/Outposts/Kikaria-Android`
- `/Users/vita/Vitemis/Outposts/Kikaria-HarmonyOS`
- `/Users/vita/Vitemis/Outposts/Rokurics-Android`
- `/Users/vita/Vitemis/Outposts/Rokurics-HarmonyOS`
- `/Users/vita/Vitemis/Outposts/Rokurics-Windows`

只允许在路径级别提及这些项目。Agent 模式下 Codex 不得总结其源码内容、UI 实现、业务逻辑或测试实现。

每个项目必须有独立 DeepCode CLI 会话、独立状态、独立轮次计数、独立时间预算、独立阻塞原因。一个项目阻塞时，不自动阻塞其他项目。

## 三模式角色定义

### Agent 模式

- Codex 是 supervisor。
- DeepCode CLI 是 executor。
- Codex 不直接改目标代码。
- Codex 不读业务源码、不看具体 diff、不运行构建测试。
- DeepCode CLI 在真实可见终端中执行业务任务。

### Spark 模式

- GPT-5.3-Codex-Spark 是 implementer。
- Spark 执行前必须确认模型。
- Spark 可直接读写目标项目并运行验证。
- Spark 不运行多窗口多项目 DeepCode 调度机制，除非用户明确另行设计。

### OpenCode 模式

- 完全不属于 Codex Supervisor。
- OpenCode 不读取本文。
- OpenCode 不使用 Codex checkpoint、batch state 或 DeepCode 会话。
- OpenCode 只读取 `OPENCODE_MODE.md` 与目标项目自己的项目文档。

## Codex 职责

Agent 模式下，Codex 负责：

- 执行 Outposts 根目录启动前检查。
- 为每个项目启动或接管真实可观察 DeepCode CLI 终端。
- 在正式任务前发送短握手并校验模型与路径。
- 生成当前项目、当前轮次、当前目标的精简 prompt。
- 约每 30 秒读取终端输出或 live log。
- 维护 `.outposts-supervisor/` 下的 batch state、checkpoint、summary、report。
- 把 DeepCode CLI 报告压缩为主管摘要。
- 把用户验收反馈转换成下一轮 DeepCode CLI 任务。

Agent 模式下，Codex 不负责：

- 阅读业务源码。
- 修改业务源码。
- 查看具体业务 diff。
- 运行构建或测试。
- 清理构建产物、缓存、`.gradle`、`intermediates`。
- 代替 DeepCode 判断迁移完成。
- 用 DeepCode 自评覆盖用户验收反馈。

## DeepCode CLI 职责

DeepCode CLI 负责：

- 在授权范围内读取 Apple 源项目作为只读参考。
- 在授权范围内读取和修改 Outposts 目标项目。
- 运行目标项目需要的构建、测试、诊断命令。
- 在 UI、截图对比或视觉验收任务中，按 `DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md` 使用 Qwen helper。
- 输出每轮结构化报告。
- 明确报告改动、验证、失败、阻塞、剩余问题。

DeepCode CLI 不得：

- 修改 Apple 源项目。
- 修改参考图目录。
- 读取或发送敏感文件。
- 执行破坏性 Git 操作。
- 清理用户级工具链或缓存。
- 调用 Claude Code 作为子执行器。

## 用户反馈优先级

用户验收反馈优先于 DeepCode CLI 自评。

如果 DeepCode 声称完成，但用户反馈存在视觉、行为、平台、构建、测试、迁移完整性问题，Codex 必须把用户反馈提升为下一轮任务输入。不得用 DeepCode 的自评覆盖用户验收反馈。

当用户人工视觉反馈与 Qwen 的相似度判断冲突时，用户人工视觉反馈优先。

用户明确说暂停、停止、只汇报、等待确认时，Codex 必须停止启动新轮。

## 项目状态机

每个 Agent 项目维护以下状态之一：

```text
NOT_STARTED
PREFLIGHT_PENDING
SESSION_STARTING
HANDSHAKE_PENDING
READY_FOR_PROMPT
ROUND_RUNNING
REPORT_RECEIVED
ROUND_COMPLETE_CONTINUE_ELIGIBLE
READY_FOR_USER_REVIEW
BLOCKED_NEEDS_USER
STOPPED_BY_TIME_BUDGET
STOPPED_BY_ROUND_BUDGET
FAILED_PREFLIGHT
MODEL_MISMATCH
WORKDIR_MISMATCH
SOURCE_READONLY_FAILED
TOOLCHAIN_MISSING
HOST_ENV_BLOCKED
MANUAL_DECISION_REQUIRED
```

## 异步事件循环

五项目并行调度采用异步事件循环：

1. 为每个项目建立或恢复独立 DeepCode CLI 终端会话。
2. 对每个项目执行 `cd -> pwd -> deepcode`。
3. 对每个项目发送短握手并记录模型与路径。
4. 对 READY 项目发送当前轮正式 prompt。
5. 每约 30 秒轮询所有运行中项目的可观察输出。
6. 哪个项目先产出报告，就先处理哪个项目。
7. 对已报告项目立即更新状态、预算、checkpoint 和主管摘要。
8. 在预算允许且无阻塞时，立即为该项目决定是否进入下一轮。
9. 不等待所有项目统一完成才处理先完成项目。

“进程还活着”不是进展。只有可观察输出、结构化报告、明确错误或用户可见状态变化才能作为状态依据。

## 何时暂停等待用户

出现以下情况时暂停当前项目，必要时暂停整个批次：

- Outposts 根目录或项目工作目录不匹配。
- DeepCode 握手模型与用户指定或批次预期不一致。
- DeepCode 需要访问或发送超出授权范围的本地内容。
- Apple 源项目只读边界无法保证。
- API 402、计费模型异常或后台计费指标不一致。
- 本地执行策略拦截且需要用户授权。
- 构建工具、平台 SDK、Windows/.NET、HarmonyOS 环境缺失，且无法在当前授权内处理。
- prompt 可能没送达，或 DeepCode 状态未知。
- 用户验收反馈与 DeepCode 报告冲突。
- 时间或轮次预算已达上限。

## 何时继续下一轮

只有同时满足以下条件，才允许启动下一轮：

- 上一轮已经产出结构化报告。
- Codex 已读取报告并形成主管判断。
- 当前项目未阻塞。
- 时间预算允许启动新轮。
- 轮次预算允许启动新轮。
- 用户没有要求暂停。
- 下一轮目标可由报告、用户反馈或既定 batch plan 明确得出。

不得从第一轮重新开始。不得重复发送上一轮 prompt。下一轮 prompt 必须基于最新报告、checkpoint 和用户反馈。

## 软状态处理

以下状态不得自动当成终止态：

- `READY_FOR_USER_REVIEW` 但仍有 remaining gaps、next recommendation 或可执行下一步。
- `REFERENCE_ONLY`。
- actual screenshot 暂不可用但 reference screenshot 可用。
- `QWEN_COMPARE_SCREENSHOTS_COMPLETED=NO` 但 reference inspect 已完成。
- `WINDOWS_HOST_VALIDATION_PENDING` 但仍有静态 WinUI/XAML 修复、reference UI 理解或 API 兼容处理可做。

这些状态应进入 `ROUND_COMPLETE_CONTINUE_ELIGIBLE`。只有预算耗尽、轮次耗尽、用户要求暂停或出现硬阻塞时才停止。

## 项目特殊调度规则

- Kikaria-Android：首页和背诵页是主要视觉对齐目标；多轮小修无改善时允许 UI shell、page layout、navigation 整体重构；优先使用 `Kikaria-Ref`；保持 `assembleDebug` / `testDebug` 绿色。
- Kikaria-HarmonyOS：编译通过是第一优先级；构建未恢复前不得堆 UI 功能；禁止用户级 Hvigor/SDK 清理。
- Rokurics-Android：禁止按文字描述自创 UI；必须对照 `Rokurics-iOS-Ref` 和 Apple 源项目；dark mode/theme support 是明确事项。
- Rokurics-HarmonyOS：必须避免黄色/异常色块；需要有效 Preview/设备截图；无效桌面截图不算验收；禁止全局 `pnpm`/npm/ohpm 和用户级 Hvigor/SDK 清理。
- Rokurics-Windows：目标是 WinUI 3 / Windows App SDK / C#；最小改动修兼容；不换框架、不重构架构；真实 build/launch 必须在 Win11 ARM + VS2022 验证。
