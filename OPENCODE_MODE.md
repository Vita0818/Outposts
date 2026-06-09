# Outposts OpenCode Mode

本文件是 OpenCode 模式的唯一入口。

OpenCode 模式完全与 Codex 无关。不要从 Codex 对话启动，不使用 Codex checkpoint、batch state、summary、report，也不读取任何 Agent/Spark/Codex 调度协议。

## OpenCode 只能读取什么

OpenCode 模式只允许读取：

1. 本文件：`/Users/vita/Vitemis/Outposts/OPENCODE_MODE.md`
2. 当前目标项目自己的项目文档。
3. 当前任务明确要求读取的目标项目源码。
4. Apple 源项目 `/Users/vita/Vitemis/Vela`，仅限只读参考。
5. 参考图目录，仅限只读：
   - `/Users/vita/Vitemis/Outposts/Kikaria-Ref`
   - `/Users/vita/Vitemis/Outposts/Rokurics-iOS-Ref`
   - `/Users/vita/Vitemis/Outposts/Rokurics-macOS-Ref`

## OpenCode 不得读取什么

OpenCode 不得读取以下 Codex / Agent / Spark 调度文档：

```text
/Users/vita/Vitemis/Outposts/AGENTS.md
/Users/vita/Vitemis/Outposts/docs/OUTPOSTS_CODEX_SUPERVISOR.md
/Users/vita/Vitemis/Outposts/docs/DUAL_TRACK_EXECUTION.md
/Users/vita/Vitemis/Outposts/docs/BATCH_SCHEDULING.md
/Users/vita/Vitemis/Outposts/docs/CLAUDE_CODE_TERMINAL_PROTOCOL.md
/Users/vita/Vitemis/Outposts/docs/CLAUDE_CODE_VISUAL_MCP_PROTOCOL.md
/Users/vita/Vitemis/Outposts/docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md
/Users/vita/Vitemis/Outposts/docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md
/Users/vita/Vitemis/Outposts/docs/RECOVERY_PLAYBOOK.md
/Users/vita/Vitemis/Outposts/docs/REPORTING_FORMATS.md
/Users/vita/Vitemis/Outposts/docs/SECURITY_AND_BOUNDARIES.md
/Users/vita/Vitemis/Outposts/docs/DO_NOT_BREAK.md
```

OpenCode 也不得读取：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/
```

除非用户明确要求 OpenCode 读取某个 OpenCode 专用报告路径。

## 启动方式

OpenCode 模式由用户直接在目标项目启动，例如：

```bash
cd /Users/vita/Vitemis/Outposts/<TARGET_PROJECT>
pwd
opencode
```

`pwd` 必须等于目标项目路径。不要在 Outposts 根目录直接启动后让 OpenCode 自己猜项目。

## 目标范围

OpenCode 默认用于构建和修改以下目标平台项目：

- Android
- HarmonyOS
- Windows

默认目标项目：

```text
/Users/vita/Vitemis/Outposts/Kikaria-Android
/Users/vita/Vitemis/Outposts/Kikaria-HarmonyOS
/Users/vita/Vitemis/Outposts/Rokurics-Android
/Users/vita/Vitemis/Outposts/Rokurics-HarmonyOS
/Users/vita/Vitemis/Outposts/Rokurics-Windows
```

Apple 源项目只读参考：

```text
/Users/vita/Vitemis/Vela
```

OpenCode 不得修改 Apple 源项目。

## 项目策略

OpenCode 模式仍采用：

```text
Apple 项目只读作参考，构建 Android、HarmonyOS 和 Windows。
```

含义：

- 可以读取 Apple 源项目理解 UI、架构、交互和业务行为。
- 不得写入 Apple 源项目。
- 不得运行会在 Apple 源项目内生成或修改文件的命令。
- 只能修改当前目标项目。
- 构建、测试、截图、诊断命令只能在当前目标项目或必要平台工具范围内执行。

## 安全边界

OpenCode 不得读取、发送、复制、摘要或写入：

- 密钥、token、私钥、证书。
- `.env`、`.env.*`。
- p12、provisioning profile。
- ssh key。
- API key。
- Keychain 内容。
- 账户凭据、cookie、session。
- 与当前任务无关的用户私人文件。

## Git 边界

OpenCode 不得执行：

- `git reset --hard`
- `git clean -fd`
- `git checkout .`
- `git restore .`
- 强制 push
- 删除用户未提交文件

不得 commit、push、创建 PR，除非用户另行明确要求。

工作区很脏时，不得清理；只能记录状态并继续在授权范围内工作。

## HarmonyOS 边界

OpenCode 不得删除、清理或修改：

- `~/.hvigor`
- 用户级 DevEco 缓存。
- 用户级 HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 全局工具链、全局包管理器状态。

不得全局安装 `pnpm`、npm 包或 ohpm 包。若构建失败指向用户级工具链问题，只能报告 `HOST_ENV_BLOCKED` 或 `TOOLCHAIN_REPAIR_NEEDS_USER`，等待用户处理。

## 视觉与截图

若任务涉及 UI 复刻、视觉验收或截图对比：

- reference 图只读。
- actual 截图必须来自有效 App、Preview、真机、模拟器或窗口。
- 不得把未裁剪桌面截图、IDE 截图、错误项目截图当成有效验收。
- 不得删除旧截图或视觉证据。
- 若需要 Qwen 等视觉模型，必须由用户明确配置或授权；不得读取 Codex/Spark 的 Qwen 协议文档。

OpenCode 可在当前目标项目内保存自己的视觉证据，但不得写入 Codex supervisor 的 `.outposts-supervisor/`，除非用户明确要求。

## 报告格式

OpenCode 结束一轮任务时，建议输出简短报告：

```text
MODE: OPENCODE
PROJECT:
PWD:
FILES_CHANGED:
COMMANDS_RUN:
BUILD_RESULT:
TEST_RESULT:
VISUAL_RESULT:
BLOCKERS:
RISKS:
NEXT_ACTION:
SCOPE_CONFIRMATION:
```

不得粘贴敏感信息。不得把 Apple 源项目内容全文贴出。不得把未运行的构建或测试说成已通过。
