# Security And Boundaries

本文只适用于 Codex 模式：Agent 与 Spark。OpenCode 模式不读取本文；OpenCode 使用 `OPENCODE_MODE.md`。

## 固定路径

Apple 源项目根目录只读：

```text
/Users/vita/Vitemis/Vela
```

Outposts 根目录：

```text
/Users/vita/Vitemis/Outposts
```

默认目标项目必须位于 Outposts 根目录下。任何超出这两个根目录的访问都需要用户明确确认；无关目录不得访问。

参考图目录只读：

```text
/Users/vita/Vitemis/Outposts/Kikaria-Ref
/Users/vita/Vitemis/Outposts/Rokurics-iOS-Ref
/Users/vita/Vitemis/Outposts/Rokurics-macOS-Ref
```

## Agent 模式边界

Agent 模式下：

- Codex 本体不写业务源码。
- Codex 本体不读业务源码。
- Codex 本体不查看具体业务 diff。
- Codex 本体不运行构建、测试、lint、截图或平台命令。
- DeepCode CLI 是唯一正式执行器。
- 不得调用 Claude Code。

Codex 可以读取：

- Outposts 根目录调度文档。
- `.outposts-supervisor/` 调度记录、batch state、checkpoint、summary、report。
- 用户明确允许的调度说明。

DeepCode CLI 可在当前对话、当前批次、当前项目授权下：

- 读取 Apple 源项目 `/Users/vita/Vitemis/Vela`，仅限只读。
- 读取和修改当前 Outposts 目标项目。
- 运行目标项目所需构建、测试、诊断命令。
- 输出结构化报告。

DeepCode CLI 不得：

- 修改 Apple 源项目。
- 修改参考图目录。
- 读取或发送敏感文件。
- 清理用户级工具链。
- 执行破坏性 Git 操作。
- 把源码、密钥或私密配置传给 Qwen。
- 调用 Claude Code 作为子执行器。

## Spark 模式边界

Spark 模式下：

- 允许：在 `/Users/vita/Vitemis/Outposts` 下当前目标项目内，Codex/Spark 本体读写代码与资源文件。
- 允许：在该目标项目范围内运行构建、测试、lint、截图与最小诊断命令。
- 禁止：修改 `/Users/vita/Vitemis/Vela`。
- 禁止：写入参考图目录。
- 禁止：执行危险 Git 操作。
- 禁止：读取或发送敏感文件。
- 禁止：访问无关目录。

视觉任务中：

- 允许在 Spark 权限范围内执行代码修复。
- 视觉闭环必须依赖 Qwen 报告。
- 不允许凭文本推断图片内容并声明验收完成。
- 不允许删除 reference、actual 或 qwen 输出。

## OpenCode 隔离边界

OpenCode 模式完全不属于 Codex 模式。

OpenCode 不得读取：

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
- `docs/DO_NOT_BREAK.md`
- `.outposts-supervisor/` 中的 Codex batch state、checkpoint、summary、report

OpenCode 只读取：

- `OPENCODE_MODE.md`
- 当前目标项目自己的项目文档

若 OpenCode 需要项目规则，应把 OpenCode 专用规则写入目标项目自己的文档，而不是把 Codex/Spark/Agent 调度文档暴露给 OpenCode。

## 敏感信息禁区

不得读取、发送、复制、摘要或写入以下内容：

- 密钥、token、私钥、证书。
- `.env`、`.env.*`。
- p12、provisioning profile。
- ssh key。
- API key。
- Keychain 内容。
- 账户凭据、cookie、session。
- 与当前任务无关的用户私人文件。

如果执行器请求读取敏感文件，必须暂停并向用户报告。

## Git 边界

不得执行：

- `git reset --hard`
- `git clean -fd`
- `git checkout .`
- `git restore .`
- 强制 push
- 删除用户未提交文件

不得 commit、push、创建 PR，除非用户另行明确要求。

工作区很脏时，不得清理；只能记录状态并继续在授权范围内调度。

## Apple 源项目只读

Apple 源项目 `/Users/vita/Vitemis/Vela` 只能作为迁移参考源。

不得在 Apple 源项目内：

- 写文件。
- 格式化文件。
- 运行会生成或修改文件的命令。
- 清理缓存。
- 执行 Git 修改操作。

若执行器报告需要写入 Apple 源项目，必须拒绝该轮继续并进入 `SOURCE_READONLY_FAILED` 或 `MANUAL_DECISION_REQUIRED`。

## 网络与外部发送边界

DeepCode CLI、Spark 或 Qwen helper 读取本地项目内容并发送给外部模型服务前，必须有用户对当前对话、当前批次、当前路径的明确授权。

Qwen 只允许接收截图路径或图片内容，不得接收：

- 源码。
- API Key。
- `.env`。
- token。
- 密钥。
- 证书。
- 完整私密配置。

Qwen API Key 只允许从环境变量读取，不得写入仓库文件、配置文件或报告。

## 本地执行策略拦截

本地执行策略拦截时：

1. 不绕过。
2. 不改用隐藏通道。
3. 不请求全局授权。
4. 记录被拦截操作、项目、路径、目的。
5. 判断是否属于当前对话、当前批次、当前路径的必要操作。
6. 只请求最小范围授权。
7. 用户未授权时，项目进入 `BLOCKED_NEEDS_USER`。

## 计费与模型异常

出现以下情况必须停止启动新轮并等待用户确认：

- API 402 Insufficient Balance。
- 外部后台计费模型与预期不一致。
- DeepCode 握手返回非预期模型。
- DeepCode 无法确认当前模型且用户指定了模型。
- Spark 无法确认 `GPT-5.3-Codex-Spark`。

不得在计费或模型状态不明时继续正式迁移。

## HarmonyOS 用户级工具链禁区

HarmonyOS 项目不得删除、清理或修改：

- `~/.hvigor`
- 用户级 DevEco 缓存。
- 用户级 HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 全局工具链、全局包管理器状态。

不得全局安装 `pnpm`、npm 包或 ohpm 包。若构建失败指向用户级工具链问题，只能报告 `HOST_ENV_BLOCKED` 或 `TOOLCHAIN_REPAIR_NEEDS_USER`，等待用户处理。

## 视觉证据保护

不得删除 `.outposts-supervisor/visual-evidence`、截图、qwen 输出、batch state、checkpoint、summary、report。视觉证据不是临时垃圾。

重新截图必须新建 `RUN_ID` 目录或唯一文件名，不得覆盖或删除旧证据。

## 授权范围

授权必须限定：

- 当前对话。
- 当前批次。
- 当前项目或路径。
- 当前任务目标。

不得把一次授权扩展为未来批次、其他目录或其他项目的默认授权。
