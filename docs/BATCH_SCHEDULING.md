# Batch Scheduling

## 必填批处理参数

每个批次开始前必须确定：

```text
BATCH_NAME
CONCURRENCY
BATCH_TIME_BUDGET_MINUTES
MAX_REPORT_ROUNDS_PER_PROJECT
STOP_MODE
AUTO_CONTINUE_WITHIN_BUDGET
NO_NEW_ROUNDS_AFTER_TIME_BUDGET
WAIT_RUNNING_ROUNDS_TO_FINISH
```

推荐默认语义：

- `BATCH_NAME`：短名，用于 session、checkpoint、summary 命名。
- `CONCURRENCY`：同时运行的项目数；五项目并行时通常为 `5`。
- `BATCH_TIME_BUDGET_MINUTES`：本批次软时间预算。
- `MAX_REPORT_ROUNDS_PER_PROJECT`：每个项目最多完成多少个有效报告轮。
- `STOP_MODE`：到达预算后的停止策略，通常为 `SOFT_STOP`.
- `AUTO_CONTINUE_WITHIN_BUDGET`：预算内是否允许自动进入下一轮。
- `NO_NEW_ROUNDS_AFTER_TIME_BUDGET`：时间预算到达后不得启动新轮，通常为 `YES`。
- `WAIT_RUNNING_ROUNDS_TO_FINISH`：时间到达后是否等待运行中轮次自然结束，通常为 `YES`。

## 什么算一轮

一轮 Claude Code 正式任务只有在以下步骤全部完成后，才计入 `ROUNDS_COMPLETED`：

1. 短握手通过。
2. Codex Agent 发送正式任务 prompt。
3. Claude Code 执行任务。
4. Claude Code 返回一次结构化报告。
5. Codex Agent 读取报告并形成主管判断。

## 什么不算一轮

以下情况不计入有效完成轮：

- 仅启动终端。
- 仅执行 `cd` 或 `pwd`。
- 仅完成短握手。
- prompt 没送达。
- Claude Code 未返回报告。
- 模型错误。
- 路径错误。
- 只读边界失败。
- 本地执行策略拦截。
- API 402 或计费异常。
- 工具链缺失导致正式任务未开始。

这些情况必须记录状态，但不得消耗有效轮次预算。

## 时间预算行为

时间预算是软限制。

达到 `BATCH_TIME_BUDGET_MINUTES` 后：

1. 设置 `TIME_BUDGET_REACHED=YES`。
2. 设置 `NO_NEW_ROUNDS=YES`。
3. 不再启动任何新正式轮。
4. 若没有运行中的正式任务，项目进入 `STOPPED_BY_TIME_BUDGET`。
5. 若有运行中的正式任务，允许其自然结束；结束后进入 `STOPPED_BY_TIME_BUDGET`。

不得因为时间预算到达而强杀正在正常运行的 Claude Code。

## 轮次预算行为

达到 `MAX_REPORT_ROUNDS_PER_PROJECT` 后：

1. 设置 `NO_NEW_ROUNDS=YES`。
2. 若没有运行中的正式任务，项目进入 `STOPPED_BY_ROUND_BUDGET`。
3. 若有运行中的正式任务，允许其自然结束；结束后进入 `STOPPED_BY_ROUND_BUDGET`。

轮次预算按项目分别计算。一个项目达到轮次上限，不影响其他项目在预算内继续。

## 终止状态枚举

批次结束时，每个项目必须进入以下任一终止状态：

- `STOPPED_BY_ROUND_BUDGET`
- `STOPPED_BY_TIME_BUDGET`
- `READY_FOR_USER_REVIEW`
- `BLOCKED_NEEDS_USER`
- `FAILED_PREFLIGHT`
- `MODEL_MISMATCH`
- `WORKDIR_MISMATCH`
- `SOURCE_READONLY_FAILED`
- `TOOLCHAIN_MISSING`
- `MANUAL_DECISION_REQUIRED`

只有所有项目均进入终止状态，才能输出批次最终主管摘要。

## 项目异步处理规则

五项目并行时采用异步处理：

- 哪个项目先完成报告，就先读取和处理哪个项目。
- 不等所有项目统一完成。
- 已完成项目若预算允许且无阻塞，可以先进入下一轮。
- 阻塞项目记录原因并暂停，不阻塞其他项目。
- 每个项目的下一轮 prompt 必须基于该项目自己的最新报告、checkpoint 和用户反馈。

## 五项目并行调度循环

循环步骤：

1. 初始化批次参数和项目状态。
2. 为每个项目建立独立真实终端。
3. 执行 `cd -> pwd -> claude`。
4. 发送短握手。
5. 对 READY 项目发送正式任务。
6. 约每 30 秒读取所有运行中项目输出。
7. 对最先返回报告的项目立即处理。
8. 更新 `ROUNDS_COMPLETED`、预算、状态和 checkpoint。
9. 若允许继续，启动该项目下一轮；否则进入终止状态。
10. 所有项目终止后输出最终主管摘要。

## 示例批处理参数

```text
BATCH_NAME=apple-ui-perfect-parity
CONCURRENCY=5
BATCH_TIME_BUDGET_MINUTES=120
MAX_REPORT_ROUNDS_PER_PROJECT=2
STOP_MODE=SOFT_STOP
AUTO_CONTINUE_WITHIN_BUDGET=YES
NO_NEW_ROUNDS_AFTER_TIME_BUDGET=YES
WAIT_RUNNING_ROUNDS_TO_FINISH=YES
```

需要后续确认：每个批次的实际时间预算、轮次上限、目标项目集合和预期 Claude 模型必须由用户或上游调度说明明确给出。
