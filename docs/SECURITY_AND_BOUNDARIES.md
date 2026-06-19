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

## Agent / ExAgent Supervisor 边界

Agent / ExAgent 中，Supervisor 是 Codex 或 OpenCode 线程。

Supervisor 允许：

- 读取 Outposts 根目录调度文档。
- 读取 `.outposts-supervisor/` 调度记录、batch state、checkpoint、summary、report。
- 读取 `DeepCode-output/` 与 `QwenCode-output/` 中的结构化报告。
- 启动真实可见或可观察的 DeepCode / QwenCode one-shot 窗口。
- 把 worker 输出文件路径传给下一轮 prompt。
- 生成主管摘要。

Supervisor 禁止：

- 读取业务源码。
- 修改业务源码。
- 运行构建、测试或 lint。
- 查看具体业务 diff。
- 读取 Apple 源项目源码内容。
- 读取敏感文件。
- 直接判读截图。
- 代替 DeepCode 执行迁移。
- 代替 QwenCode 生成视觉结论。

## DeepCode 边界

DeepCode 是实现 worker。

DeepCode 允许：

- 在当前任务授权下读取 Apple 源项目 `/Users/vita/Vitemis/Vela`，但只读。
- 读取和修改当前 Outposts 目标项目。
- 运行目标项目所需构建、测试、诊断命令。
- 在当前目标项目内生成 actual screenshot。
- 读取 supervisor 指定的 QwenCode 输出报告。
- 输出结构化报告到 `DeepCode-output/`。

DeepCode 禁止：

- 修改 Apple 源项目。
- 修改参考图目录。
- 读取或发送敏感文件。
- 直接调用 QwenCode。
- 调用视觉 helper。
- 在未读取 supervisor 指定的 QwenCode 报告时声称已根据 Qwen 视觉结果修复。
- 访问无关目录。

## QwenCode 边界

QwenCode 是视觉 worker，默认模型为：

```text
Qwen3.7-Plus
```

QwenCode 允许读取：

- supervisor 指定的 reference screenshot。
- supervisor 指定的 actual screenshot。
- supervisor 指定的视觉目标文字。

QwenCode 禁止读取或接收：

- 源码。
- DeepCode-output。
- Apple 源项目源码。
- 密钥、token、私钥、证书。
- `.env`、`.env.*`。
- p12、provisioning profile。
- ssh key。
- API key。
- Keychain 内容。
- 账户凭据、cookie、session。
- 与当前任务无关的用户私人文件。

QwenCode 禁止修改任何项目文件。QwenCode 只能输出视觉报告到：

```text
<PROJECT_PATH>/QwenCode-output/<BATCH_NAME>/
```

## Spark 模式边界

Spark 模式中，Codex 本体是实现者。

允许：

- 在 `/Users/vita/Vitemis/Outposts` 下当前目标项目内，Codex 本体可读写代码与资源文件。
- 在该目标项目范围内运行构建、测试、lint、截图与最小诊断命令。

禁止：

- 修改 `/Users/vita/Vitemis/Vela`。
- 写入参考图目录。
- 执行危险 Git 操作（`git reset --hard`、`git clean -fd`、`git checkout .`、`git restore .`）。
- 读取或发送敏感文件（密钥、token、`.env`、证书、ssh key 等）。
- 访问无关目录。

视觉任务中，Spark 不得主观判读图片并宣称视觉验收完成；视觉结论必须来自 QwenCode 报告、有效截图证据或用户明确反馈。

## OpenCode 独立模式边界

OpenCode 独立模式只读取 `OPENCODE_MODE.md` 与当前目标项目自己的项目文档。

OpenCode 独立模式不得读取：

- Agent / ExAgent / Spark supervisor 调度文档。
- `.outposts-supervisor/**`。
- `DeepCode-output/**`。
- `QwenCode-output/**`。

OpenCode 独立模式不得写 Apple 源项目或参考图目录。

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

如果任何 worker 请求读取敏感文件，Supervisor 必须暂停并向用户报告。

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

## 本地执行策略拦截

当本地执行策略拦截 worker 或 Supervisor 所需操作时：

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
- DeepCode 后端与预期 DeepSeek 模型不一致。
- QwenCode 未能确认 Qwen3.7-Plus。
- worker 无法确认当前模型。

不得在计费或模型状态不明时继续正式迁移。

## 网络与外部发送边界

DeepCode 读取本地项目内容并发送给外部模型服务前，必须有用户对当前对话、当前批次、当前路径的明确授权。

QwenCode 只能发送截图与视觉目标，不得发送源码或敏感配置。

如果授权只允许只读恢复报告，则不得发送正式迁移 prompt，不得修改文件，不得运行构建测试。

## HarmonyOS 用户级工具链禁区

HarmonyOS 项目不得删除、清理或修改：

- `~/.hvigor`
- 用户级 DevEco 缓存。
- 用户级 HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 全局工具链、全局包管理器状态。

不得全局安装 `pnpm`、npm 包或 ohpm 包。若构建失败指向用户级工具链问题，只能报告 `HOST_ENV_BLOCKED` 或 `TOOLCHAIN_REPAIR_NEEDS_USER`，等待用户处理。

## 视觉证据保护

不得删除 `.outposts-supervisor/visual-evidence`、截图、QwenCode 输出、DeepCode 输出、batch state、checkpoint、summary、report。视觉证据不是临时垃圾。重新截图必须新建 `RUN_ID` 目录，不得覆盖或删除旧证据。

## 参考图目录只读

参考图目录只读：

```text
/Users/vita/Vitemis/Outposts/Kikaria-Ref
/Users/vita/Vitemis/Outposts/Rokurics-iOS-Ref
/Users/vita/Vitemis/Outposts/Rokurics-macOS-Ref
```

不得修改、删除、重命名或重新压缩参考图。
