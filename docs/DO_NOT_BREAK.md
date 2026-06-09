# Do Not Break

以下规则是 Outposts 三模式的禁区。任何未来 Codex、DeepCode CLI、Spark 或 OpenCode 执行时都不得破坏。

## 模式禁区

- 不得在未明确模式时执行任务。
- 不得混用 Agent、Spark、OpenCode 的权限边界。
- 不得把 OpenCode 纳入 Codex batch state、checkpoint 或 supervisor 恢复流程。
- 不得让 OpenCode 读取 Codex/Spark/Agent 调度文档。
- 不得把 Spark 视觉辅助写成第四种模式；它只是 `QWEN_VISUAL_ASSIST=YES`。

## Agent 模式禁区

- 不得再调用 Claude Code。
- 不得启动 `claude`、`claude -p`、Claude Desktop 或 CC 窗口。
- 不得把旧 Claude Code 协议当作当前 Agent 协议。
- 不得让 Codex Agent 自己写业务代码。
- 不得让 Codex Agent 自己读业务源码。
- 不得让 Codex Agent 自己跑构建测试。
- 不得让 Codex Agent 查看具体业务 diff。
- 不得让 Codex Agent 代替 DeepCode CLI 判断源码迁移细节。
- 不得把 DeepCode CLI 自评当成用户验收。

## DeepCode CLI 禁区

- 不得修改 Apple 源项目 `/Users/vita/Vitemis/Vela`。
- 不得修改参考图目录。
- 不得读取或发送敏感文件。
- 不得调用 Claude Code 作为子执行器。
- 不得执行破坏性 Git 操作。
- 不得清理用户级工具链、SDK、缓存。
- 不得把源码、密钥、token、`.env`、证书或私密配置传给 Qwen。

## Spark 禁区

- 不得在未确认 `GPT-5.3-Codex-Spark` 时执行 Spark。
- 不得在模型无法确认时继续执行 Spark。
- 不得在 Spark 模式下修改 `/Users/vita/Vitemis/Vela`。
- 不得在 Spark 模式下修改参考图目录。
- 不得读取或发送敏感文件。
- 不得无依据大规模重构，除非用户明确要求。
- 不得伪装为 Agent 模式执行。

## Qwen 视觉禁区

- 不得把 Qwen 当主 Agent。
- 不得让 Qwen 修改文件。
- 不得在视觉任务中让 Spark 或 DeepCode 主观判读截图并宣称视觉验收完成。
- 不得在无 Qwen 图片识别/对比报告下宣称像素级视觉验收完成。
- 不得将源码、密钥、token、`.env`、证书或私密配置传给 Qwen。
- 不得把 Qwen API Key 写入仓库文件、配置文件或报告。
- 不得因为网络受限或 helper 不可用就跳过 Qwen 报告要求并直接收口。
- 不得用 Qwen 替代构建、测试或用户人工验收。
- 用户人工视觉反馈优先级高于 Qwen 匹配判断。

## OpenCode 禁区

OpenCode 模式不得读取：

- `AGENTS.md`
- `docs/OUTPOSTS_CODEX_SUPERVISOR.md`
- `docs/DUAL_TRACK_EXECUTION.md`
- `docs/BATCH_SCHEDULING.md`
- `docs/CLAUDE_CODE_TERMINAL_PROTOCOL.md`
- `docs/CLAUDE_CODE_VISUAL_MCP_PROTOCOL.md`
- `docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md`
- `docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md`
- `docs/RECOVERY_PLAYBOOK.md`
- `docs/REPORTING_FORMATS.md`
- `docs/SECURITY_AND_BOUNDARIES.md`
- 本文件
- `.outposts-supervisor/` 下的 Codex 状态、checkpoint、report、summary

OpenCode 只读取 `OPENCODE_MODE.md` 与目标项目自己的项目文档。

## 终端禁区

- 不得隐藏运行用户无法观察的正式任务。
- 不得回退到 Claude Code 或 `claude -p` 正式任务机制。
- 不得使用 stdin feed、task-file launcher、`--resume` 作为正式任务主通道。
- 不得忽略 `cd -> pwd -> deepcode`。
- 不得在 `pwd` 未确认前发送正式任务 prompt。
- 不得跳过 DeepCode 内短握手。
- 不得把不可见后台 stdout 当作唯一事实来源。

## 调度禁区

- 不得用“进程还活着”作为进展。
- 不得等所有项目完成后才处理先完成项目。
- 不得无预算无限运行。
- 不得在时间预算和轮次预算尚未耗尽、且项目无硬阻塞时，因为软状态提前收束批次。
- 不得把 `READY_FOR_USER_REVIEW` 当成默认终止态；必须检查 remaining gaps、next recommendation 和可执行下一步。
- 不得把 `REFERENCE_ONLY` 当成失败或终止态。
- 不得因为缺 actual screenshot 就停止 reference-first 修正。
- 不得把 `WINDOWS_HOST_VALIDATION_PENDING` 当成默认终止态。
- 不得在时间预算到达后启动新轮。
- 不得强杀正在正常运行的 DeepCode CLI 来满足软时间预算。
- 不得从第一轮重跑。
- 不得重复发送上一轮 prompt。
- 不得在状态未知时继续正式迁移。
- 不得无限循环视觉微调；默认最多 2 轮视觉验收。

## Android / HarmonyOS / Windows 禁区

- 不得把 Android Studio 当成每项目必须单独启动的 IDE；一个 Android Studio 可管理多个 Emulator。
- 不得把共享 Android Emulator 理解成用户需要手动切换项目、手动点击 Build/Run 或回复 `READY`。
- 不得把 `ANDROID_WAITING_FOR_USER_APP_SWITCH` 当作常规流程。
- 不得在前台包名不匹配时默认等待用户手动切换；必须先自动重启目标 App 并重试校验，最多 2 次。
- 不得强行并行操作同一个 DevEco Preview；HarmonyOS 视觉验收建议串行。
- 不得把 WinUI 3 `WMC0011 Unknown member` 默认归因于 SDK 缺失；先排查 WPF/Avalonia/幻觉 XAML 属性。

## 用户反馈禁区

- 不得把用户验收反馈降级。
- 不得用 DeepCode CLI 报告覆盖用户手工观察。
- 不得忽略用户要求暂停、停止、只汇报或等待确认。
- 不得把用户指出的问题归为“已完成”而不安排下一轮处理。
- 不得因为 Qwen 判断“相似”就跳过用户人工验收。

## 迁移边界禁区

- 不得允许执行器根据文字描述重写而不读 Apple 源码或 reference screenshot，除非用户明确改变任务目标。
- 不得修改 Apple 源项目 `/Users/vita/Vitemis/Vela`。
- 不得在 Apple 源项目内写文件、清理文件、运行会生成文件的命令。
- Agent 模式下 Codex 不得读取 Apple 源项目源码内容。

## 工作区禁区

- 不得清理工作区。
- 不得删除用户未提交文件。
- 不得执行 `git reset --hard`。
- 不得执行 `git clean -fd`。
- 不得执行 `git checkout .`。
- 不得执行 `git restore .`。
- 不得 commit、push、创建 PR，除非用户另行明确要求。
- 不得清理 build、cache、`.gradle`、`intermediates`。
- 不得删除、清理或修改 `~/.hvigor`。
- 不得删除、清理或修改用户级 DevEco、HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 不得全局安装 `pnpm`、npm 包、ohpm 包或任何全局工具链依赖。
- 不得删除 `.outposts-supervisor/visual-evidence`。
- 不得删除当前批次截图、qwen 输出、state、checkpoint、report 或 batch state。
- 不得把“清理临时截图”作为任务收尾动作。

## 安全禁区

- 不得访问无关目录。
- 不得读取或发送敏感信息。
- 不得读取 `.env`、token、私钥、证书、p12、provisioning profile、ssh key、API key、Keychain 内容。
- 不得把一次本地执行策略授权扩展为全局授权。
- 不得在模型、计费或授权状态异常时继续正式迁移。

## 报告禁区

- 不得把 DeepCode CLI 长报告全文贴给用户。
- 不得省略阻塞原因。
- 不得省略构建或测试未运行的事实。
- 不得把子项目源码细节写入主管摘要。
- 不得假装已经读取或验证子项目源码。
- 不得在没有截图的情况下假装完成视觉验收。
- 不得把无效截图报告为有效视觉验收。
- 不得把错误 Android device serial 的截图当成当前项目截图。
- 不得把另一个 Android 项目的 App 截图当成当前项目截图。
- 不得猜 Android `applicationId` 或主 Activity。
- 不得覆盖旧 actual screenshot。
- 不得把项目内临时截图作为最终视觉证据。
- 不得把 `installDebug` 误解为普通 UI 修改许可；它只可作为 screenshot preflight 的最小安装步骤。
