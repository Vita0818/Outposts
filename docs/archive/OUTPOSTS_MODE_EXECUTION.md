# Outposts Mode Execution

本文定义 Outposts 仅保留的两种执行模式：Agent 与 Spark。

## 模式总览

| 模式 | 发起者 | Supervisor | Worker / Executor | 适用场景 |
| --- | --- | --- | --- | --- |
| Agent | Codex 对话 | Codex | DeepCode one-shot + QwenCode one-shot | 多项目、长流程、需调度与恢复的任务 |
| Spark | Codex 对话 | 无单独 supervisor；Spark 本体执行 | GPT-5.3-Codex-Spark | 小范围、边界清晰、可直接验证的改动 |

两种模式互斥。不得在同一轮把同一主体同时当 supervisor 和实现者。

仅维护 Outposts 根级模式、调度或安全文档，不构成进入任一执行模式；开始调度 worker 或直接修改目标项目时，才必须声明 Agent 或 Spark。

## Agent worker 图

Agent 的关键结构：

```text
Supervisor
  ├─ one-shot request -> DeepCode / DeepSeek
  │      role: planning, code reading, code writing, build/test, screenshot generation, implementation report
  │      output: <PROJECT_PATH>/DeepCode-output/<BATCH_NAME>/*.md
  │
  └─ one-shot request -> QwenCode / Qwen3.7-Plus
         role: reference inspect, actual inspect, screenshot compare, visual report
         output: <PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/*.md
```

DeepCode 和 QwenCode 不互相调用、不共享对话上下文、不直接通信。所有上下文和文件路径均由 supervisor 管理。

## Agent 模式

Agent 模式从 Codex 对话发起。Codex 只做 supervisor。

Codex 负责：

- 根目录检查。
- 以一次性窗口方式调用 DeepCode / QwenCode。
- 生成当前项目、当前轮次、当前目标的完整 one-shot prompt。
- 指定 worker 输出文件路径。
- 读取 `DeepCode-output` / `QwenCode-output` 结构化报告。
- 从报告中提取下一轮所需路径。
- 把上一轮 QwenCode 视觉报告路径传给下一轮 DeepCode。
- 维护 supervisor 状态和主管摘要。

Codex 不负责：

- 读取业务源码。
- 修改业务源码。
- 运行构建或测试。
- 查看具体业务 diff。
- 直接判读截图。
- 代替 worker 判断迁移完成。

## Agent 固定约定

- 每条正式任务以 one-shot prompt 提交给 DeepCode 或 QwenCode。
- 每次调用一个新的 worker 窗口，任务结束后即弃置，不复用该窗口上下文。
- 窗口启动后不得再追加第二条业务 prompt。
- DeepCode 输出必须写入 `<PROJECT_PATH>/DeepCode-output/<BATCH_NAME>/*.md`。
- QwenCode 输出必须写入 `<PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/*.md`。
- DeepCode 不能调用 QwenCode。
- QwenCode 不能调用 DeepCode。
- DeepCode 不能调用任何视觉 helper。
- QwenCode 不能读取源码或 DeepCode-output。
- worker 之间只通过 supervisor 指定的文件路径间接交接。

## Spark 模式

Spark 模式从 Codex 对话发起。执行前必须确认当前模型是：

```text
GPT-5.3-Codex-Spark
```

Spark 本体可以在当前目标项目内读取、修改、构建和验证。Spark 不使用 Agent 的 DeepCode / QwenCode one-shot 调度链。

Spark 适合：

- 单文件或小范围修复。
- 明确构建错误修复。
- 明确测试错误修复。
- 小范围 UI_AUDIT 项。
- 需要快速验证的局部改动。

Spark 不适合：

- 多项目并行。
- 长程迁移。
- 跨平台大规模重构。
- 状态未知的恢复任务。

视觉任务中，Spark 可启用 `QWENCODE_VISUAL_ASSIST=YES`，但不得主观判读图片并宣称视觉验收完成。

## 触发词

Agent：

- Agent 模式
- 主管模式
- Codex 调度
- 调度 DeepCode / QwenCode
- DeepCode / QwenCode 批处理

Spark：

- Spark 模式
- 使用 Spark
- GPT-5.3-Codex-Spark
- Codex 本体直接改

## 模式不明确时

如果用户未明确写出模式：

1. 停止当前任务执行。
2. 要求用户明确本轮使用 `Agent` 还是 `Spark`。
3. 仅在用户确认后继续。

该规则只约束目标项目执行，不约束根级模式文档维护。

## 模式选择表

| 任务类型 | 推荐模式 |
| --- | --- |
| 单文件小修 | Spark |
| 局部构建错误修复 | Spark |
| 从 Codex 发起多项目并行 | Agent |
| 深度迁移 / 跨平台 / 多轮恢复 | Agent |
| 多图视觉对比且需长流程调度 | Agent |
| 多图视觉对比且范围很小 | Spark + QWENCODE_VISUAL_ASSIST |

## 错误模式处理

- 模式未声明 → 停止并要求明确模式。
- Spark 模型确认失败 → 停止本轮。
- Agent 中 DeepCode 或 QwenCode 模型、路径或输出异常 → 不计有效轮次，并进入恢复规则。
