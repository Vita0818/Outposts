# Outposts Parallel Scheduling Budget Policy

从本检查点开始，Outposts 多项目并行调度必须使用“轮次预算 + 时间预算”机制。

## Per-Project Inputs

每个并行项目必须由用户提供：

1. `MAX_REPORT_ROUNDS`
2. `TIME_BUDGET_MINUTES`

## Effective Round Definition

一轮 Claude Code 正式任务只有在以下步骤全部完成后，才计入 `ROUNDS_COMPLETED`：

1. handshake 通过。
2. 发送正式任务 prompt。
3. Claude Code 执行。
4. Claude Code 返回一次结构化报告。
5. Codex Agent 读取报告并形成主管判断。

仅 handshake 不算一轮。启动失败、模型错误、路径错误、只读边界失败不算有效完成轮，但必须记录失败状态。

## Time Budget Semantics

`TIME_BUDGET_MINUTES` 是软限制，只控制是否允许启动新一轮，不强杀正在运行的 Claude Code。

达到时间预算后：

1. 设置 `TIME_BUDGET_REACHED = YES`。
2. 设置 `NO_NEW_ROUNDS = YES`。
3. 若当前没有运行中的正式任务，进入 `STOPPED_BY_TIME_BUDGET`。
4. 若当前有运行中的正式任务，允许自然结束；结束后进入 `STOPPED_BY_TIME_BUDGET`。

## Round Budget Semantics

达到 `MAX_REPORT_ROUNDS` 后：

1. 设置 `NO_NEW_ROUNDS = YES`。
2. 若当前没有运行中的正式任务，进入 `STOPPED_BY_ROUND_BUDGET`。
3. 若当前有运行中的正式任务，允许自然结束；结束后进入 `STOPPED_BY_ROUND_BUDGET`。

## Terminal States

本次批处理只有当所有并行项目均进入以下任一状态后，才能停止并输出最终主管摘要：

1. `STOPPED_BY_ROUND_BUDGET`
2. `STOPPED_BY_TIME_BUDGET`
3. `READY_FOR_USER_REVIEW`
4. `BLOCKED_NEEDS_USER`
5. `FAILED_PREFLIGHT`
6. `MODEL_MISMATCH`
7. `WORKDIR_MISMATCH`
8. `SOURCE_READONLY_FAILED`
9. `TOOLCHAIN_MISSING`
10. `MANUAL_DECISION_REQUIRED`

## Forbidden Behavior

1. 不得因为时间预算到达而强杀正常运行中的 Claude Code。
2. 不得在时间预算到达后继续启动新轮。
3. 不得无限续轮。
4. 不得用“进程还活着”作为有效进展。
5. 不得把 Claude Code 长报告完整贴给用户。
6. 不得由 Codex Agent 自己读代码、写代码、跑构建、跑测试。

## Required State Fields

每个项目必须维护：

```text
PROJECT_NAME
TARGET_PATH
SOURCE_READONLY_PATH
MAX_REPORT_ROUNDS
TIME_BUDGET_MINUTES
BATCH_START_TIME
ROUNDS_COMPLETED
CURRENT_ROUND_STATUS
LAST_REPORT_STATUS
TIME_BUDGET_REACHED
NO_NEW_ROUNDS
FINAL_STATE
NEXT_ACTION
```

## Final Summary Format

最终汇报必须使用用户指定格式：

```text
Outposts 并行调度结束摘要：

- 项目：...
  轮次：已完成 X / 上限 Y
  运行时长：约 X 分钟 / 预算 Y 分钟
  最终状态：
  构建结果：
  测试结果：
  本轮完成：
  剩余问题：
  下一步建议：

全局判断：
- 可人工 review：
- 需要继续下一批调度：
- 需要用户决策：
- 明确失败/阻塞：
- 建议下一轮优先级：
```

