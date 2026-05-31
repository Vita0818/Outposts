# Claude Code Terminal Protocol

## 正式通道要求

Claude Code 正式任务必须通过真实可见或可观察的交互式终端运行。可接受的形式包括：

- 用户可见的 Terminal 窗口。
- 每项目独立 `screen` 会话。
- 每项目独立 `tmux` 会话。
- 能被用户观察、能被 Codex 读取的 live log 会话。

不得使用隐藏 headless 通道作为正式任务主通道。不得使用 `claude -p`、stdin feed、task-file launcher、`--resume` 作为正式任务机制。

只读探测、环境排错或恢复分析如需其他方式，必须先确认不违反当前用户授权，并且不得替代正式任务终端协议。

UI 视觉任务中的 `qwen-vision` 调用必须由真实终端中的 Claude Code 主 Agent 发起，并作为该项目会话的可观察工作内容记录在报告中。Codex Agent 不直接调用 `qwen-vision`，也不得用不可观察的视觉工具调用替代正式终端机制。

## 每项目独立会话

每个目标项目必须有独立 Claude Code 终端。会话命名应包含项目名和批次名，例如：

```text
outposts-<BATCH_NAME>__Kikaria-Android
outposts-<BATCH_NAME>__Kikaria-HarmonyOS
outposts-<BATCH_NAME>__Rokurics-Android
outposts-<BATCH_NAME>__Rokurics-HarmonyOS
outposts-<BATCH_NAME>__Rokurics-Windows
```

一个项目的 prompt、输出、错误、报告不得混入另一个项目。

## 启动顺序

每个项目必须严格按以下顺序启动：

```bash
cd <PROJECT_PATH>
pwd
claude
```

`pwd` 必须在启动 Claude 前执行。`pwd` 输出必须与目标项目路径一致，否则不得进入 Claude 正式任务。

不得在 Outposts 根目录直接启动用于子项目任务的 Claude。不得依赖 Claude 自己猜测工作目录。

## Claude 内短握手

每轮正式任务前，Codex Agent 必须先在 Claude 内发送短握手：

```text
[H]
只回一行，不读文件，不改文件，不构建，不测试：
MODEL=<当前模型>; PWD=<当前工作目录>; READY=<YES/NO>
```

短握手只允许 Claude 返回一行，不允许 Claude 读取文件、修改文件、运行构建或测试。

短握手不算正式任务轮次。短握手失败、模型不匹配、路径不匹配、READY=NO 都不计入已完成轮次，但必须记录状态和原因。

## 模型检查规则

Codex Agent 必须记录 Claude 返回的 `MODEL` 字段。

如果用户或批次要求特定模型，而握手显示其他模型，例如 Flash、Opus、Sonnet 或不明模型，必须暂停该项目并进入 `MODEL_MISMATCH` 或 `MANUAL_DECISION_REQUIRED`。

如果外部计费后台显示的模型与 Claude 握手不一致，例如 DeepSeek 后台显示 Flash 增长，但本批次预期不是 Flash，必须停止启动新轮并让用户确认。

不得在模型不确定时继续正式迁移。

## 路径检查规则

Codex Agent 必须记录 Claude 返回的 `PWD` 字段。

`PWD` 必须等于当前项目路径。若不一致：

1. 不发送正式任务 prompt。
2. 记录 `WORKDIR_MISMATCH`。
3. 关闭或搁置该错误会话。
4. 重新按 `cd -> pwd -> claude` 建立正确会话，或等待用户确认。

Claude Code 不得在 Apple 源项目目录中执行写操作。Apple 源项目路径只读。

## 30 秒窗口监测频率

正式任务运行中，Codex Agent 应约每 30 秒读取每个运行中项目的可观察输出。

监测目标：

- 是否有新输出。
- 是否出现结构化报告。
- 是否出现权限、模型、路径、工具链、网络、计费错误。
- UI 视觉任务中是否成功生成截图、调用 `qwen-vision`、得到视觉差异结论。
- 是否出现 Claude 等待用户输入。
- 是否长时间无输出且没有明确进展。

监测不应打断正常运行中的 Claude Code。时间预算到达后，不强杀正在正常运行的 Claude Code，只是不再启动新轮。

## 输出读取方式

优先读取用户可观察的终端缓冲区、`screen`/`tmux` capture、或对应 live log。读取方式必须能与用户人工观察对齐。

不得把不可见后台 stdout 当作唯一事实来源。不得仅凭进程存在判断任务仍在有效推进。

每次读取后，Codex Agent 应更新项目状态：

```text
PROJECT_NAME
SESSION_NAME
LAST_OBSERVED_AT
CURRENT_ROUND_STATUS
LAST_VISIBLE_OUTPUT_SUMMARY
WAITING_FOR_INPUT
ERROR_DETECTED
REPORT_DETECTED
NEXT_ACTION
```

## 用户人工接管

用户可以随时接管某个 Claude Code 终端。发生人工接管时，Codex Agent 必须记录：

- 接管时间。
- 项目名。
- 接管前状态。
- Codex 是否继续只读观察。
- 用户是否要求暂停自动发送 prompt。

用户接管后，Codex Agent 不得继续向该会话发送正式任务 prompt，除非用户明确恢复自动调度。

## Claude Code Desktop 启动纪律

Claude Code Desktop 自行管理 Outposts 时，也必须遵循同样的真实终端纪律。每个项目一个独立会话；不得把多个项目塞进同一 Claude Code 上下文；不得在 `/Users/vita/Vitemis/Outposts` 根目录直接执行子项目正式任务。

固定启动序列：

```bash
cd <目标项目路径>
pwd
claude
```

`pwd` 输出必须严格等于目标项目路径。若不匹配，立即停止，不得发送正式任务 prompt。

正式任务前必须发送：

```text
[H]
只回一行，不读文件，不改文件，不构建，不测试：
MODEL=<当前模型>; PWD=<当前工作目录>; READY=<YES/NO>
```

`MODEL` 必须是 DeepSeek V4 Pro / `deepseek-v4-pro` / `deepseek-v4-pro[1m]`。不接受 Flash、Opus、Sonnet、Haiku、GPT、unknown 或其他非预期模型。`READY=YES` 且 `PWD` 正确后才允许发送正式任务。

## 禁用正式任务通道

以下机制不得作为正式迁移主通道：

- 隐藏 headless。
- `claude -p`。
- stdin feed。
- task-file launcher。
- `--resume` 旧会话。
- 用户不可见的后台 stdout。

可观察 live log、`screen` 或 `tmux` 只能作为用户可见窗口的辅助读取层，不能替代真实可观察会话。

## 监测与报告触发

活跃会话约每 30 秒读取一次。读取目的只包括确认新输出、等待输入、工具调用、构建/测试状态、qwen 调用、结构化报告和错误。不得高频轮询，不得把“进程还活着”当作进展。

哪个项目先输出结构化报告，就先处理哪个项目。不得等待所有项目统一完成。
