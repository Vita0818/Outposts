# Security And Boundaries

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

## Codex Agent 本体边界

### Spark 模式边界

- 允许：在 `/Users/vita/Vitemis/Outposts` 下当前目标项目内，Codex 本体可读写代码与资源文件。
- 允许：在该目标项目范围内运行构建、测试、lint、截图与最小诊断命令。
- 禁止：修改 `/Users/vita/Vitemis/Vela`。
- 禁止：写入参考图目录。
- 禁止：执行危险 Git 操作（`git reset --hard`、`git clean -fd`、`git checkout .`、`git restore .`）。
- 禁止：读取或发送敏感文件（密钥、token、`.env`、证书、ssh key 等）。
- 禁止：访问无关目录。

### Agent 模式边界

- Codex 本体不写业务源码，业务代码操作由 Claude Code 在当前任务授权下完成。
- Claude Code 可按任务要求读取 Apple 源项目（只读）与目标项目。
- Codex 本体只做调度、监测和报告汇总。
- 参考图目录、`/Users/vita/Vitemis/Vela`、Apple 源项目均保持只读边界（除读取外不得写）。

Codex Agent 本体不得：

- 读取业务源码。
- 修改业务源码。
- 运行构建。
- 运行测试。
- 清理构建产物或缓存。
- 读取 Apple 源项目源码内容。
- 读取敏感文件。
- 代替 Claude Code 执行迁移。

Codex Agent 可以读取 Outposts 根目录调度文档、`.outposts-supervisor/` 调度记录、batch state、checkpoint、summary、report，以及用户明确允许的调度说明。

## Claude Code 授权边界

Claude Code 可在当前对话、当前批次、当前路径授权下：

- 读取 Apple 源项目 `/Users/vita/Vitemis/Vela`，但只读。
- 读取和修改 Outposts 目标项目。
- 运行目标项目所需构建、测试、诊断命令。
- 输出结构化报告。

授权必须限定：

- 当前对话。
- 当前批次。
- 当前项目或路径。
- 当前任务目标。

不得全局放开本地执行策略。不得把一次授权扩展为未来批次、其他目录或其他项目的默认授权。

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

如果 Claude Code 请求读取敏感文件，Codex Agent 必须暂停并向用户报告。

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

若 Claude Code 报告需要写入 Apple 源项目，Codex Agent 必须拒绝该轮继续并进入 `SOURCE_READONLY_FAILED` 或 `MANUAL_DECISION_REQUIRED`。

## 本地执行策略拦截

当本地执行策略拦截 Claude Code 或 Codex Agent 所需操作时：

1. 不绕过。
2. 不改用隐藏通道规避。
3. 记录被拦截的操作、项目、路径、目的。
4. 判断是否属于当前对话、当前批次、当前路径的必要操作。
5. 只请求最小范围授权。
6. 若用户未授权，项目进入 `BLOCKED_NEEDS_USER`。

任何授权提示必须明确范围，不得请求全局授权。

## 计费与模型异常

出现以下情况必须停止启动新轮并等待用户确认：

- API 402 Insufficient Balance。
- 外部后台计费模型与预期不一致。
- DeepSeek 后台显示 Flash 增长，但本批次预期不是 Flash。
- Claude 短握手显示 Opus、Sonnet、Flash 或其他非预期模型。
- Claude 无法确认当前模型。

不得在计费或模型状态不明时继续正式迁移。

## 网络与外部发送边界

Claude Code 读取本地项目内容并发送给外部模型服务前，必须有用户对当前对话、当前批次、当前路径的明确授权。

如果授权只允许只读恢复报告，则不得发送正式迁移 prompt，不得修改文件，不得运行构建测试。

网络压缩、remote compact、stream disconnected 等问题出现时，按 `RECOVERY_PLAYBOOK.md` 处理。

## Claude Code Desktop / Codex 共同安全边界

Claude Code Desktop 可以在正式任务授权内读取 Apple 源项目、参考图目录和目标项目，并写目标项目；Codex Agent 不得读写业务源码、不得看具体业务 diff、不得运行构建测试。两者都不得读取或发送敏感文件。

Apple 源项目只读：`/Users/vita/Vitemis/Vela`。参考图目录只读：

- `/Users/vita/Vitemis/Outposts/Kikaria-Ref`
- `/Users/vita/Vitemis/Outposts/Rokurics-iOS-Ref`
- `/Users/vita/Vitemis/Outposts/Rokurics-macOS-Ref`

Outposts 目标项目可读写，但只允许 Claude Code 主 Agent 在正式任务范围内写入对应目标项目目录，或写入 `.outposts-supervisor` 调度记录、视觉证据、checkpoint、report。

## HarmonyOS 用户级工具链禁区

HarmonyOS 项目不得删除、清理或修改：

- `~/.hvigor`
- 用户级 DevEco 缓存。
- 用户级 HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 全局工具链、全局包管理器状态。

不得全局安装 `pnpm`、npm 包或 ohpm 包。若构建失败指向用户级工具链问题，只能报告 `HOST_ENV_BLOCKED` 或 `TOOLCHAIN_REPAIR_NEEDS_USER`，等待用户处理。

## 视觉证据保护

不得删除 `.outposts-supervisor/visual-evidence`、截图、qwen 输出、batch state、checkpoint、summary、report。视觉证据不是临时垃圾。重新截图必须新建 `RUN_ID` 目录，不得覆盖或删除旧证据。

## 本地执行策略与计费异常

本地执行策略拦截时，不得绕过，不得改用隐藏通道，不得请求全局授权。只请求当前对话、当前批次、当前路径、当前可见终端会话的最小授权。

出现 API 402、DeepSeek 后台 Flash 增长、计费模型与握手模型不一致、Claude 握手返回 Opus/Sonnet/unknown 时，必须暂停启动新轮并报告。
