# Reporting Formats

## Claude Code 每轮报告字段

要求 Claude Code 每轮返回结构化报告，字段应包含：

```text
PROJECT_NAME
ROUND_ID
MODEL
PWD
TASK_RECEIVED
FILES_READ_SUMMARY
FILES_CHANGED
COMMANDS_RUN
BUILD_RESULT
TEST_RESULT
USER_FEEDBACK_ADDRESSED
COMPLETED_WORK
REMAINING_WORK
BLOCKERS
RISKS
NEXT_RECOMMENDED_ACTION
READY_FOR_USER_REVIEW
```

`FILES_READ_SUMMARY` 只能摘要范围，不要求贴源码。`COMMANDS_RUN` 应列命令和结果摘要。`BUILD_RESULT`、`TEST_RESULT` 必须区分未运行、无法运行、失败、通过。

## Codex 主管摘要字段

Codex Agent 给用户的摘要应包含：

```text
BATCH_NAME
MODEL_CHECK_RESULT
PATH_CHECK_RESULT
SCOPE_CONFIRMATION
PROJECT_STATUS_SUMMARY
BUDGET_STATUS
BLOCKERS
USER_FEEDBACK_STATUS
NEXT_RECOMMENDED_ACTION
UNCERTAINTIES
```

用户只需要看主管摘要。不得把 Claude Code 长报告整段贴给用户。必要时只摘取关键结论、阻塞、验证结果和下一步。

## 项目状态字段

每个项目摘要建议使用：

```text
PROJECT_NAME
TARGET_PATH
SESSION_NAME
STATUS
ROUNDS_COMPLETED
MAX_REPORT_ROUNDS
ELAPSED_MINUTES
TIME_BUDGET_MINUTES
LAST_CONFIRMED_ACTION
BUILD_RESULT
TEST_RESULT
BLOCKER
NEXT_ACTION
```

## 预算字段

预算摘要必须包含：

```text
BATCH_TIME_BUDGET_MINUTES
TIME_BUDGET_REACHED
MAX_REPORT_ROUNDS_PER_PROJECT
ROUNDS_COMPLETED_BY_PROJECT
NO_NEW_ROUNDS
RUNNING_ROUNDS_WAITED_TO_FINISH
STOP_REASON
```

## 阻塞字段

阻塞项必须明确：

```text
PROJECT_NAME
BLOCKER_TYPE
BLOCKER_DETAIL
USER_DECISION_NEEDED
SAFE_TO_CONTINUE_OTHER_PROJECTS
```

常见 `BLOCKER_TYPE`：

- `MODEL_MISMATCH`
- `WORKDIR_MISMATCH`
- `SOURCE_READONLY_FAILED`
- `POLICY_BLOCKED`
- `API_402`
- `TOOLCHAIN_MISSING`
- `PROMPT_DELIVERY_UNKNOWN`
- `STATE_UNKNOWN`
- `USER_REVIEW_REQUIRED`

## crash recovery 摘要格式

```text
Outposts crash recovery 摘要：

- 恢复窗口：
- 读取的调度记录：
- 未读取的内容：
- Git/root 检查：
- 项目状态：
  - <PROJECT_NAME>：<LAST_KNOWN_STATUS>；证据：<CHECKPOINT_OR_LOG>；下一步：<NEXT_ACTION>
- 不能确认：
- 需要用户决策：
- 下一步建议：
```

## 并行调度结束摘要格式

```text
Outposts 并行调度结束摘要：

- 项目：<PROJECT_NAME>
  轮次：已完成 <X> / 上限 <Y>
  运行时长：约 <M> 分钟 / 预算 <B> 分钟
  最终状态：<STATUS>
  构建结果：<BUILD_RESULT>
  测试结果：<TEST_RESULT>
  本轮完成：<SHORT_SUMMARY>
  剩余问题：<REMAINING_WORK_OR_NONE>
  阻塞：<BLOCKER_OR_NONE>
  下一步建议：<NEXT_ACTION>

全局判断：
- 可人工 review：
- 需要继续下一批调度：
- 需要用户决策：
- 明确失败/阻塞：
- 建议下一轮优先级：
```

## 用户验收反馈转下一轮任务格式

```text
USER_FEEDBACK_INPUT
PROJECT_NAME=<PROJECT_NAME>
SOURCE=<user/manual review/screenshot/build log>
FEEDBACK_SUMMARY=<concise summary>
ACCEPTANCE_GAP=<what is not acceptable yet>
NEXT_ROUND_OBJECTIVE=<what Claude Code must address>
MUST_VERIFY=<build/test/manual check expected>
DO_NOT_REPEAT=<previous failed approach or prompt>
```

用户验收反馈不得被降级为普通建议。它应进入下一轮正式任务的目标和验收条件。

## 禁止报告方式

不得：

- 粘贴 Claude Code 长报告全文。
- 用“Claude 说完成了”代替主管判断。
- 省略未运行的构建或测试。
- 隐藏模型、路径、授权、预算异常。
- 把子项目源码内容写入主管摘要。
- 把敏感信息写入摘要、checkpoint 或 report。
