# Outposts OpenCode Mode

本文件是 OpenCode 独立模式的唯一入口。

OpenCode 独立模式不使用 supervisor 流程，不启动 DeepCode CLI 作为执行器，不读取 Agent、ExAgent 或 Spark 调度文档。OpenCode 本身在目标项目中直接执行读取、修改、构建和报告。

如果用户明确要求“ExAgent 模式”，不要使用本文；改用 `EXAGENT_MODE.md`。

## OpenCode 只能读取什么

OpenCode 独立模式只允许读取：

```text
/Users/vita/Vitemis/Outposts/OPENCODE_MODE.md
/Users/vita/Vitemis/Outposts/<目标项目>/AGENTS.md
/Users/vita/Vitemis/Outposts/<目标项目>/README.md
/Users/vita/Vitemis/Outposts/<目标项目>/docs/**
```

前提是这些目标项目文档存在，且不包含敏感信息。

## OpenCode 不得读取什么

OpenCode 独立模式不得读取：

```text
/Users/vita/Vitemis/Outposts/AGENTS.md
/Users/vita/Vitemis/Outposts/EXAGENT_MODE.md
/Users/vita/Vitemis/Outposts/docs/OUTPOSTS_MODE_EXECUTION.md
/Users/vita/Vitemis/Outposts/docs/OUTPOSTS_SUPERVISOR.md
/Users/vita/Vitemis/Outposts/docs/BATCH_SCHEDULING.md
/Users/vita/Vitemis/Outposts/docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md
/Users/vita/Vitemis/Outposts/docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md
/Users/vita/Vitemis/Outposts/docs/RECOVERY_PLAYBOOK.md
/Users/vita/Vitemis/Outposts/docs/REPORTING_FORMATS.md
/Users/vita/Vitemis/Outposts/docs/SECURITY_AND_BOUNDARIES.md
/Users/vita/Vitemis/Outposts/docs/DO_NOT_BREAK.md
```

OpenCode 独立模式也不得读取 supervisor 状态目录，除非用户明确要求读取 OpenCode 专用报告路径：

```text
/Users/vita/Vitemis/Outposts/.outposts-supervisor/**
```

## 启动方式

OpenCode 独立模式由用户直接在目标项目启动，例如：

```bash
cd /Users/vita/Vitemis/Outposts/<目标项目>
pwd
opencode
```

`pwd` 必须等于目标项目路径。不要在 Outposts 根目录直接启动后让 OpenCode 自己猜项目。

## 默认目标平台

OpenCode 独立模式默认用于构建和修改以下目标平台项目：

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

## 基本策略

OpenCode 独立模式仍采用：

```text
Apple 项目只读参考；构建 Android、HarmonyOS 和 Windows 目标项目。
```

允许：

- 在当前目标项目内读取源码。
- 在当前目标项目内修改源码、资源、构建配置和项目文档。
- 在当前目标项目内运行构建、测试、lint、截图、诊断命令。
- 只读读取 Apple 源项目作为迁移参考。
- 输出当前项目的简短结构化报告。

禁止：

- 修改 `/Users/vita/Vitemis/Vela`。
- 修改参考图目录。
- 访问无关目录。
- 读取、发送、复制、摘要或写入敏感信息。
- 清理工作区或用户级工具链。
- 执行破坏性 Git 操作。
- 把 OpenCode 独立任务写入 supervisor checkpoint 或 batch state。

## 敏感信息禁区

OpenCode 不得读取、发送、复制、摘要或写入：

- 密钥、token、私钥、证书。
- `.env`、`.env.*`。
- p12、provisioning profile。
- ssh key。
- API key。
- Keychain 内容。
- 账户凭据、cookie、session。
- 与当前目标项目无关的用户私人文件。

## Git 禁区

OpenCode 不得执行：

```text
git reset --hard
git clean -fd
git checkout .
git restore .
force push
```

不得删除用户未提交文件。不得 commit、push、创建 PR，除非用户明确要求。

## HarmonyOS 工具链禁区

HarmonyOS 项目不得删除、清理或修改：

```text
~/.hvigor
用户级 DevEco 缓存
用户级 HarmonyOS SDK
用户级 Hvigor 缓存
用户级 ohpm/npm/pnpm 缓存
```

不得全局安装 `pnpm`、npm 包、ohpm 包或任何全局工具链依赖。若构建失败指向用户级工具链问题，只能报告需要用户处理。

## 视觉任务

OpenCode 独立模式如果需要视觉模型，必须由用户明确配置或授权。OpenCode 可以在当前目标项目内保存自己的视觉证据，但不得写入 supervisor 证据目录，除非用户明确要求。

## 输出报告

OpenCode 独立模式结束一轮任务时，建议输出：

```text
MODE: OPENCODE
PROJECT:
PWD:
FILES_CHANGED:
COMMANDS_RUN:
BUILD_RESULT:
TEST_RESULT:
VALIDATION_RESULT:
BLOCKERS:
RISKS:
NEXT_ACTION:
```
