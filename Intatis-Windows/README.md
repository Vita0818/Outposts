# Intatis Windows

原生 **WinUI3** 的 Windows 版 Intatis——对着只读参考 `Intatis-Apple`（Apple-first 本地 AI
工作区，macOS Chat / Code / Cowork 三产品面 + CLI）重建的 Windows 对应实现。

```text
src/
  Intatis.Core/   无 UI 的会话引擎（协议 / EventLog / Providers / 权限 / 工具 / Cowork）
  Intatis.Cli/    intatis 命令行（chat / code / cowork REPL + selftest）
  Intatis.App/    WinUI3 桌面应用（Chat / Code / Cowork / Settings）
```

`Intatis-Apple` 为只读参考，本项目不依赖任何 macOS 框架；`Intatis.Core` 与
`Intatis.Cli` 是纯 `net8.0`，可在任意平台运行（CLI 的 selftest 即跨平台离线测试）。

## 构建（需要 Windows）

沿用 Rokurics-Windows 已在本机验证过的 unpackaged WinUI3 配方
（Windows App SDK 1.5、`WindowsPackageType=None`、PRI 生成关闭、x64/x86/ARM64）：

```powershell
cd Intatis-Windows
dotnet restore
dotnet build -c Debug
dotnet run --project src/Intatis.Cli  -- selftest
dotnet run --project src/Intatis.Cli  -- config
dotnet run --project src/Intatis.Cli  -- chat
dotnet run --project src/Intatis.Cli  -- code <目录>
dotnet run --project src/Intatis.Cli  -- cowork <目录>
dotnet run --project src/Intatis.App
```

GUI 也可以在 Visual Studio 中打开 `Intatis-Windows.sln` 直接 F5。

## 配置

与 Apple 版同构的 Intatis JSON/JSONC 配置（支持注释与尾逗号），查找顺序：

1. `INTATIS_CONFIG` 环境变量
2. `%AppData%\Intatis\Intatis-Windows\intatis.json[c]`
3. `~/.config/intatis/intatis.json[c]`

最小示例：

```jsonc
{
  "model": "chat/gpt-4o-mini",
  "permission_reviewer_model": "chat/gpt-4o-mini",
  "provider": {
    "chat": {
      "npm": "@ai-sdk/openai-compatible",
      "options": {
        "baseURL": "https://api.openai.com/v1",
        "apiKey": "{env:OPENAI_API_KEY}"
      },
      "models": {
        "gpt-4o-mini": { "name": "GPT-4o mini" }
      }
    }
  }
}
```

- 密钥只以引用形式存在（`{env:VAR}`、`{file:path}`、auth file、provider config），
  在构造 provider 时才懒解析，绝不进入 EventLog、session.json 或诊断输出。
- `permission_reviewer_model` 缺省继承同一文档的顶层 `model`；显式填写但无法解析时
  **fail closed**（自动审查禁用，只走人工批准），不回退主模型。
- `image_model` / `transcription_model` / `embedding_model` / `reranker_model` 字段会
  解析并显示，但对应的多模态 / Knowledge 能力尚未移植（见下）。
- CLI 侧另有 `INTATIS_BASE_URL` / `INTATIS_API_KEY` / `INTATIS_MODEL` 快捷覆盖。

## 架构契约（与 Apple 版保持一致）

- **EventLog 是会话唯一事实源**：`<AppData>\Intatis\Intatis-Windows\sessions\<id>\events.jsonl`，
  append-only、每会话单调 `seq`、写者租约互斥（第二个运行时直接拒绝打开）；
  `session.json` 是可随时重建、无密钥的派生缓存。
- 事件类型 **只增不改**（snake_case wire 名），读取端跳过未知未来类型并保留其 seq。
- UI 只消费折叠投影（`ConversationProjection` / 各 ViewModel），不把模型原始输出当事实。
- **权限三层门**：Layer A 确定性门（敏感路径 / 越界 / 破坏性命令直接拒绝，只读检查类
  shell 命令低风险直行）→ Layer B 模型审查者（`permission_reviewer_model`，纯文本
  `ALLOW`/`DENY` 判词协议，任何失败回退人工）→ Layer C 用户（CLI y/n 提示或 GUI
  权限卡片）。每次裁决都以 `permission_request` / `permission_resolved` 事件对落盘。
- **Cowork 不递归**：agent 间通信一律经 Mediator + MessageBus（密钥扫描、4KB 上限），
  目标 agent 的 `AgentLoop` 只在 FIFO 调度器自己的任务里运行；每个 agent 的推理绑定
  冻结在会话内，改菜单不重路由。
- Code / Cowork 的每次工具调用都经过 `ToolRegistry` + 权限链；文件工具受
  `PathConfinement`（规范化、`.env*` / 密钥 / `.git/config` 等敏感路径拒绝、工作区围栏）。

## 已移植

- 会话核心：Envelope JSONL、EventLog（写者锁、单调 seq、fail-soft 重放、流式订阅）、
  投影、`session.json` v2、会话历史/删除。
- Providers：OpenAI 兼容 SSE 流式（增量 tool_calls 累积、`reasoning_effort`、
  `stream_options.include_usage`、图片 content parts）、配置导入（provider map、
  enabled/disabled、内置默认 baseURL、角色路由、JSONC）、密钥懒解析、`env` 快捷覆盖。
- ChatLoop（无工具流式聊天）与 AgentLoop（工具调用循环，含权限事件对、取消语义、
  轮次统计与终局）。
- 工具：`read_file` / `list_files` / `search_text` / `write_file` / `apply_patch`
  （Add/Update/Delete 上下文补丁）/ `git_status` / `git_diff` / `git_recent_commits` /
  `run_shell`（围栏 + 危险命令拒绝 + 超时）。
- Cowork：roster、FIFO AgentScheduler（并发 4、每 agent 单任务占用、邮箱）、
  Mediator + MessageBus、WorkTask 图（依赖/环检测/完成需结果）、Orchestrator
  （`@main` 协调者、`@agent` 路由、ask/delegate + 结果等待、审查者绑定）。
- GUI：NavigationView 外壳（侧栏 236）、Chat（消息 560 / 内容 900 / 胶囊气泡 / 流式 /
  模型选择 / 用量条）、Code（工作区选择、转写流、工具卡、权限卡）、Cowork
  （348 状态栏：roster / 控制面 / 任务；线程列 + composer）、Settings（provider
  目录编辑 + 保存/测试 + 角色路由展示）。
- CLI：`chat` / `code` / `cowork` REPL、`/model` `/mode` `/attach`、`@agent` 路由、
  `/agents` `/agent add|rm` `/tasks`、离线 `selftest`（EventLog / 投影 / 调度器 /
  Mediator / 补丁 / 门 / 配置导入 / ChatLoop 伪 provider 全链路）。

## 尚未移植（后续批次）

- MCP（client/server、OAuth、conformance）、Knowledge（embedding/reranker）、
  Skills、浏览器工具、文档/媒体工具（PDF/DOCX/图像生成/转写）、managed terminal
  (PTY)、hosted web search 与 citations 抓取、iOS 面。
- EventLog 的 WAL 崩溃恢复与跨进程读锁（当前为写者租约 + 单机假设）、
  SubmittedIntent outbox、模型历史压缩、自动会话标题、Goal/ContinuationRun 运行时
  （WorkTask 图与 Goal 事件已定义，控制面未接）。
- GUI 的会话历史侧栏、重命名/删除、附件按钮（CLI 已支持 `/attach`）、
  Markdown 富渲染（当前纯文本 + 等宽工具卡）。

## 验证状态

本仓库在 macOS 上编写，**尚未在 Windows 上编译运行**（WinUI3 无法在 macOS 构建）。
已做静态校验：全部 XAML/manifest 良构、XAML 事件处理器与 code-behind 一一对应、
C# 括号平衡复查通过。首次在 Windows 机器上执行
`dotnet build` + `intatis selftest` 后，如遇编译错误按报错顺序修复即可（预期只会有
少量 API 签名级别差异）。
