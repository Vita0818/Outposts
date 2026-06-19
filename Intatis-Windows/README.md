# Intatis Windows

基于 `Intatis-Apple` 思路实现的 Windows 版本，包含：

- `Intatis.Cli`：CLI（Chat / Code / Cowork）
- `Intatis.Gui`：WPF GUI（Chat / Code / Cowork）
- `Intatis.Shared`：CLI 与 GUI 的共享会话、配置、模型与工作区工具

## 运行前置

- 需要 .NET 8 SDK（Windows）
- 仅读项目：`Intatis-Apple`
- 目标：`Intatis-Windows` 为独立实现，不依赖 macOS 框架

## CLI

```bash
cd Intatis-Windows
dotnet run --project src/Intatis.Cli/Intatis.Cli.csproj
dotnet run --project src/Intatis.Cli/Intatis.Cli.csproj -- help
dotnet run --project src/Intatis.Cli/Intatis.Cli.csproj -- config
dotnet run --project src/Intatis.Cli/Intatis.Cli.csproj -- settings
dotnet run --project src/Intatis.Cli/Intatis.Cli.csproj -- chat
dotnet run --project src/Intatis.Cli/Intatis.Cli.csproj -- code ./.git
dotnet run --project src/Intatis.Cli/Intatis.Cli.csproj -- cowork
dotnet run --project src/Intatis.Cli/Intatis.Cli.csproj -- selftest
```

配置来源优先级：

1. 环境变量
2. `%AppData%\\Intatis\\Intatis-Windows\\config.json`
3. 默认值

环境变量：

- `INTATIS_BASE_URL`（默认 `https://api.openai.com/v1`）
- `INTATIS_API_KEY`
- `INTATIS_MODEL`（默认 `gpt-4o-mini`）
- `INTATIS_REASONING`（可选：`minimal|low|medium|high|off`）
- `INTATIS_MODE`（`chat|code|cowork`）
- `INTATIS_WORKSPACE`（Code/Cowork 默认路径）
- `INTATIS_USAGE`（`0/1`）

### Chat 命令

- `help`：显示会话帮助
- `clear`：清空本地会话
- `/mode`：`/mode chat|code|cowork`
- `/model`：查看/设置会话模型
- `/reasoning`：查看/设置推理强度（`minimal|low|medium|high|off`）
- `/attach <path>`：附加文本或图片附件到下一条消息（图片走 vision，多文本文件会内联）`/attach clear` 清空
- `/config`：打印运行时配置
- `/exit`：退出当前模式

### Code 命令

- 常规：
  - `ls [path]`
  - `read <file>`
  - `write <file> <text>`
  - `search <text> [::path]`（例如：`search fixme::src`）
  - `cwd`
- Slash：
  - `/help`
  - `/mode chat|code|cowork`
  - `/exit`
- 其他：
  - `help`
  - `clear`

### Cowork 命令

- `@agent <message>`
- `/help`
- `/agents`
- `/model [name]`
- `/agent add <name> <path> [model]`
- `/attach <path>`
- `/mode chat|code|cowork`
- `/exit`

## GUI

```bash
cd Intatis-Windows
dotnet run --project src/Intatis.Gui/Intatis.Gui.csproj
```

GUI 与 CLI 共享配置与 `IntatisConfig` 结构，便于同一配置在两端复用。

## 解决方案

```bash
cd Intatis-Windows
dotnet build Intatis-Windows.sln
```

## 目录结构

- `src/Intatis.Shared`：共享核心与网络客户端
- `src/Intatis.Cli`：命令行交互（chat/code/cowork/selftest）
- `src/Intatis.Gui`：WPF 客户端界面

### 同步项

- 附件、模型、reasoning、工作区配置与 CLI 共用 `IntatisConfig`，两端可复用同一套配置。
