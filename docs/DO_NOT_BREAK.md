# Do Not Break

以下规则是 Outposts 四模式的禁区。任何未来 Codex、OpenCode 线程、DeepCode CLI、Spark 或 OpenCode 独立任务执行时都不得破坏。

## 模式禁区

- 不得混用 Agent、ExAgent、Spark、OpenCode 的权限边界。
- 不得把 OpenCode 独立模式纳入 supervisor batch state、checkpoint 或恢复流程。
- 不得让 OpenCode 独立模式读取 Agent、ExAgent、Spark 或 supervisor 调度文档。
- 不得把 ExAgent 当成 OpenCode 独立模式；ExAgent 的执行器仍为 DeepCode CLI。
- 不得把 OpenCode 独立模式当成 ExAgent；OpenCode 独立模式不启动 DeepCode CLI 执行器。
- 不得因为任务紧急而跳过模式声明确认。

## Agent / ExAgent supervisor 禁区

- 不得让 Supervisor 自己写业务代码。
- 不得让 Supervisor 自己读业务源码。
- 不得让 Supervisor 自己跑构建测试。
- 不得让 Supervisor 查看具体业务 diff。
- 不得让 Supervisor 代替 DeepCode CLI 判断源码迁移细节。
- 不得把 DeepCode CLI 自评当成用户验收。

## DeepCode CLI 禁区

- 不得读取或发送敏感信息。
- 不得修改 Apple 源项目。
- 不得修改参考图目录。
- 不得执行破坏性 Git 操作。
- 不得清理工作区、缓存、构建产物或用户级工具链。
- 不得把无效截图报告为有效视觉验收。
- 不得把 Qwen 当成代码修改者。

## Spark 禁区

- 不得在未确认 `GPT-5.3-Codex-Spark` 时执行 Spark。
- 不得在模型无法确认时继续执行 Spark。
- 不得在 Spark 模式下修改 `/Users/vita/Vitemis/Vela`。
- 不得在 Spark 模式下修改参考图目录。
- 不得在 Spark 模式下读取或发送敏感信息。
- 不得在 Spark 模式下执行破坏性 Git 操作。
- 不得伪装为 Agent 或 ExAgent 执行。

## Qwen 视觉禁区

- 不得把 Qwen 当主执行器。
- 不得让 Qwen 修改文件。
- 不得在视觉任务中让 Spark 或 DeepCode 主观判读截图并宣称视觉验收完成。
- 不得在无 Qwen 图片识别/对比报告下宣称像素级视觉任务完成。
- 不得把 Qwen 当代码修改者。
- 不得将源码、密钥、token、`.env`、证书或私密配置传给 Qwen。
- 不得把 qwen API Key 写入仓库文件、配置文件或报告。
- 不得因为网络受限或 helper 不可用就跳过 Qwen 报告要求并直接收口。

## OpenCode 独立模式禁区

OpenCode 独立模式不得读取：

- `AGENTS.md`
- `EXAGENT_MODE.md`
- `docs/OUTPOSTS_MODE_EXECUTION.md`
- `docs/OUTPOSTS_SUPERVISOR.md`
- `docs/BATCH_SCHEDULING.md`
- `docs/DEEPCODE_CLI_TERMINAL_PROTOCOL.md`
- `docs/DEEPCODE_CLI_VISUAL_QWEN_PROTOCOL.md`
- `docs/RECOVERY_PLAYBOOK.md`
- `docs/REPORTING_FORMATS.md`
- `docs/SECURITY_AND_BOUNDARIES.md`
- `docs/DO_NOT_BREAK.md`
- `.outposts-supervisor/**`

OpenCode 独立模式只读取 `OPENCODE_MODE.md` 与目标项目自己的项目文档。

OpenCode 独立模式不得修改 Apple 源项目、参考图目录或无关目录。

## 终端禁区

- 不得隐藏运行用户无法观察的正式任务。
- 不得使用用户不可观察的后台通道作为正式任务主通道。
- 不得忽略 `cd -> pwd -> deepcode`。
- 不得在 `pwd` 未确认前发送正式任务 prompt。
- 不得跳过 DeepCode 内短握手。
- 不得把“进程还活着”当作进展。

## 调度禁区

- 不得等所有项目完成后才处理先完成项目。
- 不得无预算无限运行。
- 不得在时间预算和轮次预算尚未耗尽、且项目无硬阻塞时，因为软状态提前收束批次。
- 不得把 `READY_FOR_USER_REVIEW` 当成默认终止态；必须检查 remaining gaps、next recommendation 和可执行下一步。
- 不得把 `REFERENCE_ONLY` 当成失败或终止态。
- 不得因为缺 actual screenshot 就停止 reference-first 修正；只要 reference screenshot 可用，就应继续利用 Qwen reference 理解推进。
- 不得把 `WINDOWS_HOST_VALIDATION_PENDING` 当成默认终止态；若仍可做静态 WinUI/XAML 修复，应继续。
- 不得在时间预算到达后启动新轮。
- 不得强杀正在正常运行的任务来满足软时间预算。
- 不得从第一轮重跑。
- 不得重复发送上一轮 prompt。
- 不得在状态未知时继续正式迁移。
- 不得无限循环视觉微调；默认最多 2 轮视觉验收。
- 不得把共享 Android Emulator 理解成用户需要手动切换项目、手动点击 Build/Run 或回复 `READY`。
- 不得在前台包名不匹配时默认等待用户手动切换；必须先自动重启目标 App 并重试校验，最多 2 次。
- 不得强行并行操作同一个 DevEco Preview；HarmonyOS 视觉验收建议串行。

## 用户反馈禁区

- 不得把用户验收反馈降级。
- 不得用工具报告覆盖用户手工观察。
- 不得忽略用户要求暂停、停止、只汇报或等待确认。
- 不得把用户指出的问题归为“已完成”而不安排下一轮处理。
- 不得因为 Qwen 判断“相似”就跳过用户人工验收。
- 用户人工视觉反馈优先级高于 Qwen 的匹配判断。

## 迁移边界禁区

- 不得允许执行器根据文字描述重写而不读 Apple 源码或 reference screenshot，除非用户明确改变任务目标。
- 不得修改 Apple 源项目 `/Users/vita/Vitemis/Vela`。
- 不得在 Apple 源项目内写文件、清理文件、运行会生成文件的命令。
- 不得让 Supervisor 读取 Apple 源项目源码内容。

## 工作区禁区

不得执行：

```text
git reset --hard
git clean -fd
git checkout .
git restore .
```

不得 commit、push、创建 PR，除非用户另行明确要求。

不得清理 build、cache、`.gradle`、`intermediates`。

不得删除、清理或修改：

- `~/.hvigor`
- 用户级 DevEco、HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存
- 全局工具链、全局包管理器状态

不得全局安装 `pnpm`、npm 包、ohpm 包或任何全局工具链依赖。

不得删除 `.outposts-supervisor/visual-evidence`、当前批次截图、Qwen 输出、state、checkpoint、report 或 batch state。重新截图必须创建新的 `RUN_ID` 证据目录，而不是覆盖或删除旧证据。

## 安全禁区

- 不得访问无关目录。
- 不得读取或发送敏感信息。
- 不得读取 `.env`、token、私钥、证书、p12、provisioning profile、ssh key、API key、Keychain 内容。
- 不得把源码、密钥、token、`.env`、证书或私密配置传给 Qwen。
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
- 全桌面截图不算有效 actual screenshot，除非明确裁剪出 App、Preview 或窗口区域。
- Qwen 看过无效桌面截图，只能报告 `QWEN_CALLED=YES` 与 `QWEN_VALID_VISUAL_EVIDENCE=NO`，不得报告为有效验收。
- 不得用 Qwen 替代构建和测试。
- 不得把视觉证据截图散落到子项目源码目录或 Apple 源项目；必须写入 `.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT_NAME>/`。
- 不得把错误 Android device serial 的截图当成当前项目截图。
- 不得把另一个 Android 项目的 App 截图当成当前项目截图；截图前必须校验前台包名等于目标 `applicationId`。
- 不得猜 Android `applicationId` 或主 Activity；必须从当前项目配置或设备 package manager 输出确认。
- 不得覆盖旧 actual screenshot；Android 截图文件名必须唯一。
- 不得把项目内临时截图作为最终视觉证据；必须复制到 visual-evidence 后才算截图链成功。
- 不得把 `installDebug` 误解为普通 UI 修改许可；它只可作为 screenshot preflight 的最小安装步骤，且必须报告 `INSTALL_NEEDED_FOR_SCREENSHOT`、`INSTALL_COMMAND`、`INSTALL_RESULT`。
