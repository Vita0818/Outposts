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

## Agent / ExAgent supervisor 边界

Agent / ExAgent 中，Supervisor 不写业务源码，业务代码操作由 DeepCode CLI 在当前任务授权下完成。

Supervisor 不得：

- 读取业务源码。
- 修改业务源码。
- 查看具体业务 diff。
- 运行构建。
- 运行测试。
- 清理构建产物或缓存。
- 读取 Apple 源项目源码内容。
- 读取敏感文件。
- 代替 DeepCode CLI 执行迁移。

Supervisor 可以读取 Outposts 根目录调度文档、`.outposts-supervisor/` 调度记录、batch state、checkpoint、summary、report，以及用户明确允许的调度说明。

DeepCode CLI 可在当前对话或当前 OpenCode 线程、当前批次、当前路径授权下：

- 读取 Apple 源项目 `/Users/vita/Vitemis/Vela`，但只读。
- 读取和修改 Outposts 目标项目。
- 运行目标项目所需构建、测试、诊断命令。
- 输出结构化报告。

授权必须限定：

- 当前发起上下文。
- 当前批次。
- 当前项目或路径。
- 当前任务目标。

不得全局放开本地执行策略。不得把一次授权扩展为未来批次、其他目录或其他项目的默认授权。

## Spark 模式边界

Spark 模式允许：

- 在 `/Users/vita/Vitemis/Outposts` 下当前目标项目内，Spark 本体读写代码与资源文件。
- 在该目标项目范围内运行构建、测试、lint、截图与最小诊断命令。

Spark 模式禁止：

- 修改 `/Users/vita/Vitemis/Vela`。
- 写入参考图目录。
- 执行破坏性 Git 操作。
- 读取或发送敏感文件。
- 访问无关目录。
- 清理 build、cache、`.gradle`、`intermediates` 或用户级工具链。

## OpenCode 独立模式边界

OpenCode 独立模式只读取 `OPENCODE_MODE.md` 与目标项目自己的项目文档。

OpenCode 独立模式可在当前目标项目内读写、构建、测试和报告；不得读取 supervisor 调度文档、状态、checkpoint 或 batch state。

OpenCode 独立模式不得修改 Apple 源项目、参考图目录或无关目录。

## Qwen 调用边界

如需 Qwen helper 或外部 Qwen API，必须满足：

- 用户已明确允许该 helper 的网络访问，或 helper 已在当前环境中可用。
- 仅通过环境变量读取 API Key，例如 `DASHSCOPE_API_KEY`、`QWEN_API_KEY`。
- 图像输入仅限截图路径。
- 不得将 key 写入仓库文档、源码、配置文件或报告。
- 不得将源码、密钥、token、`.env`、证书或私密配置传给 Qwen。

网络不可用且无可复用 helper 时，应报告 `QWEN_HELPER_NETWORK_NOT_AVAILABLE` 或 `QWEN_UNAVAILABLE_IN_SESSION`。

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

如果执行器请求读取敏感文件，Supervisor 必须暂停并向用户报告。

## Git 边界

不得执行：

```text
git reset --hard
git clean -fd
git checkout .
git restore .
force push
```

不得删除用户未提交文件。不得 commit、push、创建 PR，除非用户另行明确要求。

工作区很脏时，不得清理；只能记录状态并继续在授权范围内调度。

## Apple 源项目只读

Apple 源项目 `/Users/vita/Vitemis/Vela` 只能作为迁移参考源。

不得在 Apple 源项目内：

- 写文件。
- 格式化文件。
- 运行会生成或修改文件的命令。
- 清理缓存。
- 执行 Git 修改操作。

若执行器报告需要写入 Apple 源项目，Supervisor 必须拒绝该轮继续并进入 `SOURCE_READONLY_FAILED` 或 `MANUAL_DECISION_REQUIRED`。

## 参考图目录只读

参考图目录只读：

```text
/Users/vita/Vitemis/Outposts/Kikaria-Ref
/Users/vita/Vitemis/Outposts/Rokurics-iOS-Ref
/Users/vita/Vitemis/Outposts/Rokurics-macOS-Ref
```

不得修改、删除、重命名或重新压缩参考图。

## 本地执行策略拦截

当本地执行策略拦截所需操作时：

1. 不绕过。
2. 不改用隐藏通道规避。
3. 记录被拦截的操作、项目、路径、目的。
4. 判断是否属于当前发起上下文、当前批次、当前路径的必要操作。
5. 只请求最小范围授权。
6. 若用户未授权，项目进入 `BLOCKED_NEEDS_USER`。

任何授权提示必须明确范围，不得请求全局授权。

## 计费与模型异常

出现以下情况必须停止启动新轮并等待用户确认：

- API 402 Insufficient Balance。
- 外部后台计费模型与预期不一致。
- DeepCode 短握手显示非预期模型。
- DeepCode 无法确认当前模型且任务依赖模型能力或成本边界。
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

不得删除 `.outposts-supervisor/visual-evidence`、截图、Qwen 输出、batch state、checkpoint、summary、report。视觉证据不是临时垃圾。重新截图必须新建 `RUN_ID` 目录，不得覆盖或删除旧证据。
