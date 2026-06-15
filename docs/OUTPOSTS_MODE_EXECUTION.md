# Outposts Mode Execution

本文定义 Outposts 四模式：Agent、ExAgent、Spark、OpenCode。

## 模式总览

| 模式 | 发起者 | 主管 | 执行器 | 适用场景 |
| --- | --- | --- | --- | --- |
| Agent | Codex 对话 | Codex | DeepCode CLI | 多项目、长流程、需调度与恢复的任务 |
| ExAgent | OpenCode 线程 | OpenCode 线程 | DeepCode CLI | 与 Agent 同配置，但由 OpenCode 线程发起 |
| Spark | Codex 对话 | 无单独主管；Spark 本体执行 | GPT-5.3-Codex-Spark | 小范围、边界清晰、可直接验证的改动 |
| OpenCode | OpenCode 独立任务 | 无 supervisor | OpenCode 本身 | 单项目直接修改和构建，不进入 supervisor 流程 |

四种模式互斥。不得在同一轮把同一主体同时当 supervisor 和实现者。

## Agent 模式

Agent 模式从 Codex 对话发起。Codex 只做 supervisor，DeepCode CLI 是实际执行器。

Codex 负责：

- 根目录检查。
- 按预填充方式启动/监测/恢复 DeepCode CLI 会话。
- 生成当前项目、当前轮次、当前目标的精简 prompt（每条 prompt 作为一次完整提交）。
- 读取 DeepCode CLI 结构化报告。
- 维护 supervisor 状态和主管摘要。

固定约定：

- 每条正式任务以预填充形式提交给 DeepCode（单条 prompt 完成）。
- 每次只使用一个 DeepCode 窗口，任务结束后即弃置，不复用该窗口。
- 每条 prompt 末尾必须包含：`请把输出写入到DeepCode-output/*****.md中。`
- 读取 DeepCode CLI 结构化报告。

Codex 不负责：

- 读取业务源码。
- 修改业务源码。
- 运行构建或测试。
- 查看具体业务 diff。
- 代替执行器判断迁移完成。

## ExAgent 模式

ExAgent 模式从 OpenCode 线程发起。除发起者不同外，ExAgent 与 Agent 完全一致：

```text
Agent:   INITIATOR=Codex;          SUPERVISOR=Codex;          EXECUTOR=DeepCode CLI
ExAgent: INITIATOR=OpenCode_THREAD; SUPERVISOR=OpenCode_THREAD; EXECUTOR=DeepCode CLI
```

ExAgent 使用同一套：

- DeepCode CLI 终端协议。
- Qwen 视觉辅助协议。
- 批次预算规则。
- 恢复规则。
- 报告格式。
- 安全边界。

ExAgent 不等同于 OpenCode 独立模式。OpenCode 独立模式由 OpenCode 本身直接读写目标项目；ExAgent 则只由 OpenCode 线程做 supervisor，实际执行仍由 DeepCode CLI 完成。

## Spark 模式

Spark 模式从 Codex 对话发起。执行前必须确认当前模型是：

```text
GPT-5.3-Codex-Spark
```

Spark 本体可以在当前目标项目内读取、修改、构建和验证。Spark 不使用 DeepCode CLI，不进入 Agent/ExAgent 的多终端调度机制。

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

视觉任务中，Spark 可启用 `QWEN_VISUAL_ASSIST=YES`，但不得主观判读图片并宣称视觉验收完成。

## OpenCode 模式

OpenCode 模式是独立执行模式。OpenCode 自己直接在目标项目里读写、构建、测试和报告。

OpenCode 模式不读取：

- `AGENTS.md`
- `EXAGENT_MODE.md`
- `docs/` 下 supervisor / Agent / ExAgent / Spark 调度协议
- supervisor checkpoint、batch state、summary、report

OpenCode 模式只读取：

- `OPENCODE_MODE.md`
- 当前目标项目自己的项目文档

OpenCode 模式不启动 DeepCode CLI 作为执行器，不消费 supervisor 轮次预算。

## 触发词

Agent：

- Agent 模式
- 主管模式
- Codex 调度
- 调度 DeepCode
- DeepCode CLI

ExAgent：

- ExAgent 模式
- OpenCode 线程发起 Agent
- OpenCode 发起 DeepCode 调度
- OpenCode 主管 + DeepCode CLI

Spark：

- Spark 模式
- 使用 Spark
- GPT-5.3-Codex-Spark
- Codex 本体直接改

OpenCode：

- OpenCode 模式
- OpenCode 独立执行
- 直接用 OpenCode 改

## 模式不明确时

如果用户未明确写出模式：

1. 停止当前任务执行。
2. 要求用户明确本轮使用 `Agent`、`ExAgent`、`Spark` 还是 `OpenCode`。
3. 仅在用户确认后继续。

## 模式选择表

| 任务类型 | 推荐模式 |
| --- | --- |
| 单文件小修 | Spark |
| 局部构建错误修复 | Spark |
| 单项目 OpenCode 直接开发 | OpenCode |
| 从 Codex 发起多项目并行 | Agent |
| 从 OpenCode 发起多项目并行 | ExAgent |
| 深度迁移 / 跨平台 / 多轮恢复 | Agent 或 ExAgent |
| 多图视觉对比且需长流程调度 | Agent 或 ExAgent |
| 多图视觉对比且范围很小 | Spark + QWEN_VISUAL_ASSIST |

## 错误模式处理

- 模式未声明 → 停止并要求明确模式。
- Spark 模型确认失败 → 停止本轮。
- Agent / ExAgent 中执行器模型、路径或 READY 异常 → 不发送正式任务 prompt。
- OpenCode 独立任务误读 supervisor 文档 → 停止并重启为正确模式。
- ExAgent 被误当成 OpenCode 独立任务 → 停止，重新按 ExAgent 入口执行。
