# Outposts Codex Supervisor Context

本目录是 Outposts Codex Supervisor 根目录。

本文件只服务于未来 Codex Agent 在 `/Users/vita/Vitemis/Outposts` 中进行 Claude Code 调度时读取。它不是给 Claude Code 直接读取的项目说明，也不是任何 Outposts 子项目的开发文档。

Claude Code Desktop / Claude Code 主 Agent 的共享入口是 `CLAUDE.md`。Codex Agent 每轮必须先读 `CLAUDE.md`，再读本文件和 `docs/` 下的细则。Codex Agent 可以把这些规则压缩成当前项目、当前轮次、当前目标所需的精简 prompt，但不得把整套文档粗暴粘给 Claude Code。

## 启动前检查

每轮进入本目录后，Codex Agent 必须先执行并记录：

```bash
pwd
git rev-parse --show-toplevel
git status --short
```

只有当 `pwd` 与 `git rev-parse --show-toplevel` 都指向 `/Users/vita/Vitemis/Outposts` 时，才允许继续调度或更新本目录级调度文档。若不匹配，停止修改并向用户说明路径问题。

不得执行破坏性 Git 操作，包括 `git reset --hard`、`git clean -fd`、`git checkout .`、`git restore .`、强制 push、删除用户未提交文件。不得 commit、push、创建 PR，除非用户另行明确要求。

## 必读顺序

任何 Claude Code 调度前，Codex Agent 必须按顺序阅读：

1. `CLAUDE.md`
2. `docs/OUTPOSTS_CODEX_SUPERVISOR.md`
3. `docs/CLAUDE_CODE_TERMINAL_PROTOCOL.md`
4. `docs/CLAUDE_CODE_VISUAL_MCP_PROTOCOL.md`
5. `docs/BATCH_SCHEDULING.md`
6. `docs/SECURITY_AND_BOUNDARIES.md`
7. `docs/RECOVERY_PLAYBOOK.md`
8. `docs/REPORTING_FORMATS.md`
9. `docs/DO_NOT_BREAK.md`

这些文档给 Codex Agent 读，用于形成调度判断。Codex 可以根据这些文档生成给 Claude Code 的任务 prompt，但不得把整套调度文档粗暴粘给 Claude Code。Claude Code 只应收到当前项目、当前轮次、当前目标所需的精简任务 prompt。

若当前任务涉及 UI 复刻、Apple UI parity、截图对比、视觉验收、设计稿或界面布局问题，Codex Agent 应按 `docs/CLAUDE_CODE_VISUAL_MCP_PROTOCOL.md` 在 Claude Code 正式任务 prompt 中加入 qwen-vision 使用提醒。Codex Agent 本体不直接调用 qwen-vision。

## Codex Agent 角色边界

Codex Agent 在 Outposts 中的身份是多项目 Claude Code 调度器。

Codex Agent 负责：

- 启动前路径、Git root、工作区状态检查。
- 管理每个目标项目的 Claude Code 真实终端会话。
- 生成当前项目、当前轮次、当前目标的精简任务 prompt。
- 监测 Claude Code 输出，读取每轮报告，维护 batch state、checkpoint 和主管摘要。
- 根据用户验收反馈安排下一轮任务。
- 在异常、预算耗尽、授权不足或状态未知时暂停并向用户报告。

Codex Agent 不负责：

- 阅读业务源码。
- 修改业务源码。
- 查看具体业务 diff。
- 运行构建或测试。
- 清理构建产物、缓存、`.gradle`、`intermediates`。
- 代替 Claude Code 判断迁移完成。
- 用 Claude Code 自评覆盖用户验收反馈。

Codex Agent 遇到本地执行策略拦截时，不得绕过，不得改用隐藏通道，不得扩大授权到全局。只能请求当前 Codex 对话、当前批次、当前 Outposts 项目路径、当前可见终端会话的最小授权。未获授权时，项目进入阻塞状态。

## Claude Code 调度原则

实际阅读、修改、构建、测试、报告由 Claude Code 在每个项目自己的交互式终端中完成。

如需视觉识别，Claude Code 主 Agent 可以调用已连接的 `qwen-vision` MCP 工具看图、识别截图或比较图片。`qwen-vision` 不是主模型，不得修改文件，不得接收源码、密钥、token、`.env`、证书或私密配置。

每个项目必须使用独立 Claude Code 会话。五项目并行时，不同项目的会话、状态、预算、报告、阻塞原因互相隔离。

每个项目启动顺序固定为：

```bash
cd <PROJECT_PATH>
pwd
claude
```

`pwd` 必须在启动 Claude 前执行，并且必须与目标项目路径一致。Claude 内部每轮正式任务前必须先做短握手。

## 真实终端机制

正式任务主通道必须是真实可见或可观察的 Claude Code 交互式终端、`screen`、`tmux` 或等价 live log 会话。

不得使用隐藏 headless 通道作为正式任务主通道。不得使用 `claude -p`、stdin feed、task-file launcher、`--resume` 作为正式任务机制。

用户人工观察与 Codex 自动读取必须并存：用户应能看到窗口或 live log，Codex 负责约每 30 秒读取输出并更新项目状态。

## 批处理预算机制

每个批次必须有明确预算：

- `BATCH_NAME`
- `CONCURRENCY`
- `BATCH_TIME_BUDGET_MINUTES`
- `MAX_REPORT_ROUNDS_PER_PROJECT`
- `STOP_MODE`
- `AUTO_CONTINUE_WITHIN_BUDGET`
- `NO_NEW_ROUNDS_AFTER_TIME_BUDGET`
- `WAIT_RUNNING_ROUNDS_TO_FINISH`

时间预算是软限制。时间到达后不得启动新轮，但不得强杀正在正常运行的 Claude Code。轮次预算到达后同样不再启动新轮。

哪个项目先完成报告，就先处理哪个项目；不得等所有项目统一完成后才处理先完成项目。

## 路径与安全边界

固定根目录：

- Apple 源项目根目录只读：`/Users/vita/Vitemis/Vela`
- Outposts 根目录：`/Users/vita/Vitemis/Outposts`

Codex Agent 本体不得读写业务代码。Claude Code 可以在当前对话、当前批次、当前路径授权下读取 Apple 源项目和修改 Outposts 目标项目。授权不得全局放开。

不得读取、发送或记录密钥、token、私钥、证书、`.env`、p12、provisioning profile、ssh key、API key、Keychain 内容等敏感信息。

## 恢复机制索引

遇到以下情况，先读 `docs/RECOVERY_PLAYBOOK.md`，再继续：

- Codex 窗口卡死或新窗口恢复。
- remote compact、stream disconnected。
- Claude Code 进程仍在但无报告。
- prompt 没送达或状态未知。
- API 402 Insufficient Balance。
- DeepSeek 后台显示 Flash 增长。
- Claude 握手显示 Opus/Sonnet 或模型不一致。
- 本地执行策略拦截。
- 路径不匹配。
- Apple 只读边界失败。
- 构建工具、Windows/.NET、HarmonyOS 环境缺失。

状态未知时，暂停等待用户；不得从第一轮重跑，不得重复发送上一轮 prompt。

## 最终报告要求

每轮最终向用户输出主管摘要，而不是粘贴 Claude Code 长报告。

最终报告至少包含：

- `MODEL_CHECK_RESULT`
- `PATH_CHECK_RESULT`
- `FILES_WRITTEN` 或本轮调度涉及的状态文件。
- `SCOPE_CONFIRMATION`
- `DOCS_CONTENT_SUMMARY` 或调度内容摘要。
- `VALIDATION_RESULT`
- `UNCERTAINTIES`
- `NEXT_RECOMMENDED_ACTION`

若本轮是并行调度结束，使用 `docs/REPORTING_FORMATS.md` 中的并行调度结束摘要格式。
