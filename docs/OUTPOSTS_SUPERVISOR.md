# Outposts Supervisor

本文定义 Agent / ExAgent 模式的 supervisor 行为。

Agent 与 ExAgent 只有发起者不同：

```text
Agent:   INITIATOR=Codex
ExAgent: INITIATOR=OpenCode_THREAD
```

其余 supervisor 规则相同。

## 总体定位

Supervisor 管理 DeepCode CLI 的启动、任务分发、输出监测、报告读取、预算控制、状态恢复和主管摘要。

Supervisor 不是业务实现者。实际阅读源码、修改目标项目、运行构建、运行测试、产出每轮项目报告的主体是 DeepCode CLI。

## 默认目标项目

除非用户另行指定，默认调度对象为：

```text
/Users/vita/Vitemis/Outposts/Kikaria-Android
/Users/vita/Vitemis/Outposts/Kikaria-HarmonyOS
/Users/vita/Vitemis/Outposts/Rokurics-Android
/Users/vita/Vitemis/Outposts/Rokurics-HarmonyOS
/Users/vita/Vitemis/Outposts/Rokurics-Windows
```

只允许在 supervisor 摘要中进行路径级别与状态级别描述；不得总结具体源码内容、业务实现或具体 diff。

每个项目每轮都使用独立、短生命周期 DeepCode CLI 会话（任务结束即弃置），并保持独立状态、独立轮次计数、独立时间预算、独立阻塞原因。一个项目阻塞时，不自动阻塞其他项目。

## 职责分界

Supervisor 负责：

- 执行 Outposts 根目录启动前检查。
- 为每个项目启动或接管真实可观察 DeepCode CLI 终端（一次一窗口）。
- 在预填充 prompt 中携带模型与路径校验并开始任务，不再依赖后续独立输入。
- 生成当前项目、当前轮次、当前目标的精简 prompt。
- 约每 30 秒读取终端输出或 live log。
- 维护 `.outposts-supervisor/` 下的 batch state、checkpoint、summary、report，前提是当前任务允许写这些调度记录。
- 把 DeepCode CLI 报告压缩为主管摘要。
- 把用户验收反馈转换成下一轮 DeepCode CLI 任务。
- 每条 DeepCode 正式任务 prompt 末尾必须包含：

```text
请把输出写入到DeepCode-output/*****.md中。
```

DeepCode CLI 负责：

- 在授权范围内读取 Apple 源项目作为只读参考。
- 在授权范围内读取和修改 Outposts 目标项目。
- 运行目标项目需要的构建、测试、诊断命令。
- 在 UI 复刻、截图对比或视觉验收任务中，按视觉辅助协议使用 Qwen helper。
- 输出每轮结构化报告。
- 明确报告改动、验证、失败、阻塞、剩余问题。

Qwen helper 只负责图片理解与视觉差异分析，不是主执行器。推理、取舍、文件修改、命令执行、构建、测试和总结仍由 DeepCode CLI 完成。

## 用户反馈优先级

用户验收反馈优先于工具自评。

如果 DeepCode CLI 声称完成，但用户反馈存在视觉、行为、平台、构建、测试、迁移完整性问题，Supervisor 必须把用户反馈提升为下一轮任务输入。

当用户人工视觉反馈与 Qwen 判断冲突时，用户人工视觉反馈优先。

用户明确说暂停、停止、只汇报、等待确认时，Supervisor 必须停止启动新轮。

## 项目状态机

每个项目维护以下状态之一：

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
TOOLCHAIN_REPAIR_NEEDS_USER
MANUAL_DECISION_REQUIRED
```

## 异步事件循环

五项目并行调度采用异步事件循环：

1. 为每个项目建立或恢复独立 DeepCode CLI 会话（按每轮一窗口）。
2. 对每个项目执行 `cd -> pwd`。
3. 对每个项目通过一次预填充 prompt 同时提交任务元信息与正式任务内容（含模型与路径校验）。
4. 对 READY 项目发送当前轮正式 prompt（在同一预填充任务内完成）。
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
- DeepCode CLI 需要访问或发送超出授权范围的本地内容。
- Apple 源项目只读边界无法保证。
- API 402、计费模型异常或后台计费指标不一致。
- 本地执行策略拦截，且需要用户授权才能继续。
- 构建工具、平台 SDK、Windows/.NET、HarmonyOS 环境缺失，且无法在当前授权内处理。
- prompt 可能没送达，或 DeepCode CLI 状态未知。
- 用户验收反馈与 DeepCode CLI 报告冲突。
- 时间或轮次预算已达上限。

## 何时继续下一轮

只有同时满足以下条件，才允许启动下一轮：

- 上一轮已经产出结构化报告。
- Supervisor 已读取报告并形成主管判断。
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
- actual screenshot 暂时不可用，但 reference screenshot 可用。
- `QWEN_COMPARE_SCREENSHOTS_COMPLETED=NO`，但 qwen reference inspect 已完成。
- `WINDOWS_HOST_VALIDATION_PENDING`，但仍有静态 WinUI/XAML 修复、reference UI 理解或 API 兼容处理可做。

这些状态应进入 `ROUND_COMPLETE_CONTINUE_ELIGIBLE`。只有预算耗尽、轮次耗尽、用户要求暂停、或出现硬阻塞时才停止。

## 项目特殊调度规则

- Kikaria-Android：首页和背诵页是主要视觉对齐目标；多轮小修无改善时允许 UI shell、page layout、navigation 整体重构；优先使用 `Kikaria-Ref`；保持 Android build/test 绿色。
- Kikaria-HarmonyOS：编译通过是第一优先级；构建未恢复前不得堆 UI 功能；禁止用户级 Hvigor/SDK 清理；若恢复报告显示 `SAFE_TO_CONTINUE=YES`，可重新纳入迁移调度。
- Rokurics-Android：禁止按文字描述自创 UI；必须对照 `Rokurics-iOS-Ref` 和 Apple 源项目；目标是高级、极简、暗色/玻璃质感；dark mode/theme support 是明确事项。
- Rokurics-HarmonyOS：必须避免黄色/异常色块；需要有效 Preview/设备截图；无效桌面截图不算验收；禁止全局 `pnpm`/npm/ohpm 和用户级 Hvigor/SDK 清理。
- Rokurics-Windows：目标是 WinUI 3 / Windows App SDK / C#；`WMC0011 Unknown member` 优先按混入 WPF/Avalonia/幻觉 XAML 属性处理；最小改动修兼容；不换框架、不重构架构；Debug/ARM64 build 与窗口启动必须在 Win11 ARM + VS2022 验证。
