# Outposts Dual Track Execution

本文定义 Outposts 调度的双轨规则：Spark（Codex 直接执行）与 Agent（Codex 调度、Claude Code 主执行）。

## 双轨模式总览

两种模式职责完全不同且互斥：

- Spark：适合小范围、高频、边界清晰的任务，Codex 本体直接修改 Outposts 目标项目。
- Agent：适合复杂、长程、大范围任务，由 Claude Code 在真实可见窗口执行，Codex 做调度与汇总。

除非用户明确声明，否则禁止默认采用任何模式；必须先问清。

## Spark 模式定义

用户在命令中明确包含“Spark 模式”时启动。

- 前置要求：先确认当前模型为 `GPT-5.3-Codex-Spark`。
- 若无法确认模型、模型不匹配或确认失败，立即停止本轮。
- 目标是明确可验证的小范围改动，避免长流程和多轮复杂依赖推导。

## Agent 模式定义

用户在命令中明确包含“Agent 模式”时启动。

- Codex 本体只做调度：管理真实可见的 Claude Code 终端、握手、任务 prompt、报告聚合。
- 实际源码读取、修改、构建、测试由 Claude Code 执行。
- 启动前必须进行标准握手和模型确认。

## 触发词

Spark 触发词：

- Spark 模式
- 使用 Spark
- 由 GPT-5.3-Codex-Spark 执行
- Codex 本体直接改

Agent 触发词：

- Agent 模式
- 主管模式
- 调度 Claude Code
- CC 窗口
- DeepSeek + Qwen

## 两种模式权限差异

Spark：

- 模型检查：必须为 `GPT-5.3-Codex-Spark`。
- 直接读写权限在目标项目内由 Codex 本体承担。
- 可直接运行项目命令（构建/测试等）。
- 不运行多窗口多项目协作机制。

Agent：

- 模型是 DeepSeek（主任务由 Claude Code 执行）。
- Codex 本体不读写业务源码，不运行构建测试。
- 业务改动仅由 Claude Code 在批准上下文中执行。

## 两种模式执行边界

Spark 允许：

- 业务源码最小改动
- 精准修复单文件/单模块问题
- 小范围构建报错修复
- 明确的差异截图修补

Spark 禁止：

- 修改 `/Users/vita/Vitemis/Vela`
- 修改参考图目录
- 改动无关目录
- 启动复杂迁移批次
- 无模型确认直接执行

Agent 允许：

- 大范围迁移、架构级重构协作
- 多项目并行调度
- 依赖 Claude Code 自主长流程执行和报告

Agent 限制：

- Codex 不直接改业务源码
- 不替代 Claude Code 进行源码判断
- 不进行深层本地 diff 逐段审查决策

## 典型任务

- Spark：UI_AUDIT 项目清单修复、单文件修正、明确构建/测试错误、单元测试补丁。
- Agent：多项目并行、深度迁移、跨文件/跨平台任务、长期执行任务、需持续状态监控批次。

## 模式选择表

| 任务类型 | 推荐模式 |
| --- | --- |
| 单文件小修 | Spark |
| UI_AUDIT 明确项 | Spark |
| 构建错误小修 | Spark |
| 单元测试修复 | Spark |
| Android 首页像素修正 | Spark |
| 多项目并行调度 | Agent |
| 跨平台迁移 | Agent |
| 深度阅读 iOS/macOS 架构 | Agent |
| qwen 多图视觉批次 | Agent 或 Spark，取决于用户指定 |
| 长时间自主执行 | Agent |

## 模式不明确时行为

如果用户未写明模式：

1. 停止当前任务执行。
2. 先向用户回复“请明确本轮使用 Spark 模式还是 Agent 模式”。
3. 仅在用户确认后继续。

## 报告格式

Spark 报告模板：`MODE: SPARK`

- MODEL_CHECK_RESULT
- PATH_CHECK_RESULT
- PROJECT
- FILES_CHANGED
- BUILD_RESULT
- TEST_RESULT
- VALIDATION_RESULT
- RISKS
- NEXT_ACTION
- SCOPE_CONFIRMATION

Agent 报告模板：`MODE: AGENT`

- MODEL_CHECK_RESULT
- PATH_CHECK_RESULT
- CLAUDE_TERMINALS
- PROJECT_REPORTS
- ROUNDS_COMPLETED
- BLOCKERS
- SUPERVISOR_SUMMARY
- SCOPE_CONFIRMATION
- NEXT_ACTION

## 错误模式处理

- 模型确认失败 → 立即停机并要求确认。
- 模型错误、路径不符、不可逆授权缺失 → 切换或回退到 `BLOCKED`。
- Spark 中发现超范围改动风险 → 升级为 Agent 前先通知用户。
- Agent 中发现 Claude 任务越界 → 记录 incident 后阻止后续。
