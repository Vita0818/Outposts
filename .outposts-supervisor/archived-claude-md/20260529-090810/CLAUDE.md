# Outposts Claude Code Dispatch Context

本目录是 Outposts 多项目迁移管理根目录。本文档给 Claude Code Desktop / Claude Code 主 Agent 和 Codex Agent 共同读取，用于统一 Outposts 下多个迁移项目的调度、边界、视觉验收和恢复规则。

## Outposts 身份

管理的目标项目包括：

- `Kikaria-Android`
- `Kikaria-HarmonyOS`
- `Rokurics-Android`
- `Rokurics-HarmonyOS`
- `Rokurics-Windows`

固定路径：

- Apple 源项目只读根目录：`/Users/vita/Vitemis/Vela`
- Outposts 目标项目根目录：`/Users/vita/Vitemis/Outposts`
- Kikaria 参考图目录：`/Users/vita/Vitemis/Outposts/Kikaria-Ref`
- Rokurics iOS 参考图目录：`/Users/vita/Vitemis/Outposts/Rokurics-iOS-Ref`
- Rokurics macOS 参考图目录：`/Users/vita/Vitemis/Outposts/Rokurics-macOS-Ref`

## 必读顺序

每次 Claude Code Desktop 或 Codex Agent 开始调度前，必须先阅读：

1. `docs/OUTPOSTS_CODEX_SUPERVISOR.md`
2. `docs/CLAUDE_CODE_TERMINAL_PROTOCOL.md`
3. `docs/CLAUDE_CODE_VISUAL_MCP_PROTOCOL.md`
4. `docs/BATCH_SCHEDULING.md`
5. `docs/SECURITY_AND_BOUNDARIES.md`
6. `docs/RECOVERY_PLAYBOOK.md`
7. `docs/REPORTING_FORMATS.md`
8. `docs/DO_NOT_BREAK.md`

## 角色边界

Claude Code Desktop / Claude Code 主 Agent 可以在正式任务授权范围内读取目标项目、只读读取 Apple 源项目、只读读取参考图目录、修改目标项目、运行构建、运行测试、截图、调用 `qwen-vision`、输出结构化报告。

Codex Agent 只做调度、输入 prompt、监测可见终端、读取 Claude Code 报告、更新 `.outposts-supervisor` 调度记录和输出主管摘要。Codex Agent 不得读写业务源码，不得查看具体业务 diff，不得跑构建或测试。

`qwen-vision` 只是 Claude Code 可调用的 MCP 视觉工具，不是主模型。主推理模型必须是 DeepSeek V4 Pro / `deepseek-v4-pro` / `deepseek-v4-pro[1m]` 这一类 Pro 路由。

## 硬边界

1. Apple 源项目 `/Users/vita/Vitemis/Vela` 只读，不得修改。
2. 参考图目录只读，不得修改、删除、重命名。
3. 所有写入只能发生在对应 Outposts 目标项目目录内，或 `.outposts-supervisor` 调度记录、checkpoint、report、视觉证据目录内。
4. 不得读取或发送密钥、token、私钥、证书、`.env`、p12、ssh key、API key、Keychain 内容等敏感信息。
5. 不得执行 `git clean`、`git reset`、`git restore`、`git checkout`。
6. 不得 commit、push、创建 PR，除非用户另行明确要求。
7. 不得删除 `.outposts-supervisor/visual-evidence`、截图、qwen 输出、state、checkpoint、report。
8. HarmonyOS 项目不得清理 `~/.hvigor`、用户级 DevEco/HarmonyOS SDK 缓存，不得全局安装 `pnpm`、npm、ohpm 包。
9. 不得用无效桌面截图冒充 App、Preview、设备或窗口视觉证据。
10. 不得用“进程还活着”作为有效进展。

## 真实终端机制

每个项目必须使用独立真实可见或可观察 Claude Code 会话。正式任务主通道不得隐藏。

每个项目启动前必须在普通 shell 层执行：

```bash
cd <目标项目路径>
pwd
claude
```

`pwd` 必须严格等于目标项目路径，才允许启动或使用 Claude Code 执行正式任务。不得在 Outposts 根目录启动后让 Claude 自己猜项目路径。

不得使用隐藏 headless 作为正式任务主通道。不得使用 `claude -p`、stdin feed、task-file launcher、`--resume` 旧会话。每约 30 秒监测一次活跃窗口。哪个项目先完成结构化报告，就先处理哪个项目；不得等待所有项目统一完成。

## 模型与握手

正式任务前必须发送短握手：

```text
[H]
只回一行，不读文件，不改文件，不构建，不测试：
MODEL=<当前模型>; PWD=<当前工作目录>; READY=<YES/NO>
```

规则：

- `MODEL` 必须是 DeepSeek V4 Pro / `deepseek-v4-pro` / `deepseek-v4-pro[1m]`。
- 不接受 `deepseek-v4-flash`、`deepseek-chat`、`deepseek-r1`、`claude`、`sonnet`、`opus`、`haiku`、`gpt`、`unknown`。
- `PWD` 必须严格等于当前目标项目路径。
- 只有 `READY=YES` 后才可发送正式任务。
- 短握手不算迁移轮次。

## qwen-vision 规则

`qwen-vision` 是 MCP 视觉工具，不是主模型。Claude Code 主 Agent 负责推理、代码修改、构建、测试和总结；`qwen-vision` 只负责：

- `inspect_screenshot(image_path, goal="")`
- `compare_screenshots(reference_image_path, actual_image_path, goal="")`
- `extract_text_and_controls(image_path)`

不得把源码、密钥、token、`.env`、证书或私密配置传给 `qwen-vision`。UI 视觉批次必须优先读取 reference screenshot。若 actual screenshot 暂时不可得，仍可用 reference-only 进行 UI 理解修正。

`REFERENCE_ONLY` 不是失败，也不是终止态。无效桌面截图不能算有效视觉证据；只有 App 实际渲染画面、Android 设备截图、HarmonyOS Preview/真机/模拟器画面、Windows app 窗口截图才可作为有效 actual screenshot。

参考图映射：

- Kikaria 项目使用 `Kikaria-Ref`
- Rokurics Android/HarmonyOS 使用 `Rokurics-iOS-Ref`
- Rokurics Windows 使用 `Rokurics-macOS-Ref`

## 批处理规则

每批必须声明：

- `BATCH_NAME`
- `CONCURRENCY`
- `BATCH_TIME_BUDGET_MINUTES`
- `MAX_REPORT_ROUNDS_PER_PROJECT`
- `STOP_MODE`
- `AUTO_CONTINUE_WITHIN_BUDGET`
- `NO_NEW_ROUNDS_AFTER_TIME_BUDGET`
- `WAIT_RUNNING_ROUNDS_TO_FINISH`

时间预算是软限制。时间到达后不强杀正在正常运行的 Claude Code，只是不再启动新轮。

一轮必须包含：握手通过、正式任务 prompt 发出、Claude Code 执行、结构化报告返回、主管判断完成。仅 handshake 不算一轮。

`READY_FOR_USER_REVIEW` 不能无条件终止；如果仍有 `next recommendation`、remaining gaps、actual screenshot 缺失但 reference 可用、或预算内可执行下一步，应继续下一轮。

`WINDOWS_HOST_VALIDATION_PENDING` 不是默认终止态；如果仍有静态 WinUI/XAML 修复、reference UI 对齐或 API 兼容处理可做，应继续。只有只剩 Windows 主机验证且当前主机无法执行时，才暂停等待用户。

## 事故恢复

Codex 或 Claude 窗口卡死时，不得从第一轮重跑，不得重复发送上一轮 prompt，不得清理工作区。先读取 checkpoint、batch state、visible terminal 或 live log，再做只读恢复报告。

边界违规后必须先恢复检查，不得立刻继续迁移。API 402、DeepSeek 后台 Flash 计费增长、Claude 握手返回 Opus/Sonnet、路径不匹配、本地执行策略拦截，都必须暂停并报告。

## 项目特殊规则

### Kikaria-Android

- UI 多轮无改善时，允许 UI shell、page layout、navigation 层面的整体重构。
- 首页和背诵页 / ReviewScreen 是主要视觉对齐目标。
- 必须优先参考 `Kikaria-Ref`。
- 尽量保持 `assembleDebug` / `testDebug` 绿色。

### Kikaria-HarmonyOS

- 第一优先级经常是编译通过。
- 构建未恢复前不得堆功能。
- 禁止清理用户级 Hvigor、DevEco SDK、HarmonyOS SDK 缓存。
- 需要用户级工具链操作时，报告 `TOOLCHAIN_REPAIR_NEEDS_USER`。
- 如果恢复报告显示 `SAFE_TO_CONTINUE=YES`，可重新纳入调度，迁移轮次从 0 继续。

### Rokurics-Android

- 禁止基于文字描述再造 UI。
- 必须对照 `Rokurics-iOS-Ref` 和 Apple 源项目。
- 质感目标：高级、极简、暗色/玻璃质感、接近 Apple 端。
- dark mode / theme support 是明确事项。
- qwen Android actual screenshot 已被证明可行，应优先使用 adb 纯设备截图。

### Rokurics-HarmonyOS

- 必须避免黄色或异常色块。
- 需要有效 Preview、设备或模拟器截图。
- 无效桌面截图不算视觉验收。
- 禁止全局 `pnpm`、npm、ohpm。
- 禁止清理用户级 Hvigor、DevEco SDK、HarmonyOS SDK 缓存。

### Rokurics-Windows

- 目标是 WinUI 3 / Windows App SDK / C# 客户端。
- 不要把 XamlCompiler `WMC0011 Unknown member` 默认误判为 SDK 缺失。
- 已知问题是混入 WPF/Avalonia/幻觉 XAML 属性。
- 最小改动修 WinUI 3 XAML 兼容。
- 不更换框架，不重构架构。
- Debug/ARM64 build 和窗口启动必须在 Win11 ARM + Visual Studio 2022 环境验证。
- `WINDOWS_HOST_VALIDATION_PENDING` 不是默认终止；若仍有静态 XAML 修复可做，应继续。
