# Do Not Break

以下规则是 Outposts Codex Supervisor 的禁区。任何未来 Codex Agent 在本目录调度 Claude Code 时都不得破坏。

## 角色禁区

- 不得让 Codex Agent 自己写业务代码。
- 不得让 Codex Agent 自己读业务源码。
- 不得让 Codex Agent 自己跑构建测试。
- 不得让 Codex Agent 代替 Claude Code 判断源码迁移细节。
- 不得把 Claude Code 自评当成用户验收。
- 不得把 `qwen-vision` 当主 Agent。
- 不得让 `qwen-vision` 修改文件。

## 双轨模式禁区

- 不得在未确认 Spark 模式时让 Codex 本体直接改业务代码。
- 不得在 Agent 模式下让 Codex 本体读写业务源码。
- 不得在 Spark 模式下跳过 `GPT-5.3-Codex-Spark` 模型确认。
- 不得在模型无法确认时继续执行 Spark。
- 不得混用 Spark 与 Agent 的权限边界。
- 不得因为任务紧急而跳过“模式声明”确认。
- 不得把 `.claude/settings.local.json` 当作 Codex GUI 权限文件。
- 不得把 `.codex/config.toml` 当作 Claude Code 权限文件。

## 终端禁区

- 不得隐藏运行用户无法观察的正式任务。
- 不得回退到 `claude -p` 正式任务机制。
- 不得使用 stdin feed、task-file launcher、`--resume` 作为正式任务主通道。
- 不得忽略 `cd -> pwd -> claude`。
- 不得在 `pwd` 未确认前发送正式任务 prompt。
- 不得跳过 Claude 内短握手。

## 调度禁区

- 不得用“进程还活着”作为进展。
- 不得等所有项目完成后才处理先完成项目。
- 不得无预算无限运行。
- 不得在时间预算和轮次预算尚未耗尽、且项目无硬阻塞时，因为软状态提前收束批次。
- 不得把 `READY_FOR_USER_REVIEW` 当成默认终止态；必须检查 remaining gaps、next recommendation 和可执行下一步。
- 不得把 `REFERENCE_ONLY` 当成失败或终止态。
- 不得因为缺 actual screenshot 就停止 reference-first 修正；只要 reference screenshot 可用，就应继续利用 qwen reference 理解推进。
- 不得把 `WINDOWS_HOST_VALIDATION_PENDING` 当成默认终止态；若仍可做静态 WinUI/XAML 修复，应继续。
- 不得在时间预算到达后启动新轮。
- 不得强杀正在正常运行的 Claude Code 来满足软时间预算。
- 不得从第一轮重跑。
- 不得重复发送上一轮 prompt。
- 不得在状态未知时继续正式迁移。
- 不得无限循环视觉微调；默认最多 2 轮视觉验收。
- 不得把 Android Studio 当成每项目必须单独启动的 IDE；一个 Android Studio 可管理多个 Emulator。
- 不得把共享 Android Emulator 理解成用户需要手动切换项目、手动点击 Build/Run 或回复 `READY`。
- 不得把 `ANDROID_WAITING_FOR_USER_APP_SWITCH` 当作常规流程；共享 Emulator 必须由 Claude Code 通过 adb、Gradle applicationId、必要安装、启动和前台包名校验自动切换目标 App。
- 不得在前台包名不匹配时默认等待用户手动切换；必须先自动重启目标 App 并重试校验，最多 2 次。
- 不得强行并行操作同一个 DevEco Preview；HarmonyOS 视觉验收建议串行。
- 不得在边界事件尚未复盘前立刻启动下一批大规模功能调度。

## 用户反馈禁区

- 不得把用户验收反馈降级。
- 不得用 Claude Code 报告覆盖用户手工观察。
- 不得忽略用户要求暂停、停止、只汇报或等待确认。
- 不得把用户指出的问题归为“已完成”而不安排下一轮处理。
- 不得因为 `qwen-vision` 判断“相似”就跳过用户人工验收。
- 用户人工视觉反馈优先级高于 `qwen-vision` 的匹配判断。

## 迁移边界禁区

- 不得允许 Claude 根据文字描述重写而不读 Apple 源码，除非用户明确改变任务目标。
- 不得修改 Apple 源项目 `/Users/vita/Vitemis/Vela`。
- 不得让 Claude Code 在 Apple 源项目内写文件、清理文件、运行会生成文件的命令。
- 不得让 Codex Agent 读取 Apple 源项目源码内容。

## 工作区禁区

- 不得清理工作区。
- 不得删除用户未提交文件。
- 不得执行 `git reset --hard`。
- 不得执行 `git clean -fd`。
- 不得执行 `git checkout .`。
- 不得执行 `git restore .`。
- 不得 commit、push、创建 PR，除非用户另行明确要求。
- 不得清理 build、cache、`.gradle`、`intermediates`。
- 不得删除、清理或修改 `~/.hvigor`。
- 不得删除、清理或修改用户级 DevEco、HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 不得全局安装 `pnpm`、npm 包、ohpm 包或任何全局工具链依赖。
- 不得执行用户级或系统级工具链修复。工具链异常时只允许报告 `HOST_ENV_BLOCKED` 或 `TOOLCHAIN_REPAIR_NEEDS_USER`，等待用户处理。
- HarmonyOS 项目的写入只允许发生在对应 Outposts 目标项目目录内；不得把“修编译”扩大成用户级环境改造。
- 不得删除 `.outposts-supervisor/visual-evidence`。
- 不得删除当前批次截图、qwen 输出、state、checkpoint、report 或 batch state。
- 不得把“清理临时截图”作为任务收尾动作。
- 如果需要重新截图，必须创建新的 `RUN_ID` 证据目录，而不是覆盖或删除旧证据。

## 安全禁区

- 不得访问无关目录。
- 不得读取或发送敏感信息。
- 不得读取 `.env`、token、私钥、证书、p12、provisioning profile、ssh key、API key、Keychain 内容。
- 不得把源码、密钥、token、`.env`、证书或私密配置传给 `qwen-vision`。
- 不得把一次本地执行策略授权扩展为全局授权。
- 不得在模型、计费或授权状态异常时继续正式迁移。

## 报告禁区

- 不得把 Claude Code 长报告全文贴给用户。
- 不得省略阻塞原因。
- 不得省略构建或测试未运行的事实。
- 不得把子项目源码细节写入主管摘要。
- 不得假装已经读取或验证子项目源码。
- 不得在没有截图的情况下假装完成视觉验收。
- 不得把无效截图报告为有效视觉验收。
- 全桌面截图不算有效 actual screenshot，除非明确裁剪出 App、Preview 或窗口区域。
- `qwen-vision` 看过无效桌面截图，只能报告 `QWEN_CALLED=YES` 与 `QWEN_VALID_VISUAL_EVIDENCE=NO`，不得报告为有效验收。
- 不得用 `qwen-vision` 替代构建和测试。
- 不得在用户已确认 Android emulator 显示目标页面时继续误报“没有 Android emulator”，除非 `adb` 或截图命令实际失败。
- 不得在用户已确认 DevEco Preview 显示目标页面时继续误报“没有 HarmonyOS Preview”，除非 Preview、设备或截图命令实际失败。
- 不得把视觉证据截图散落到子项目源码目录或 Apple 源项目；必须写入 `.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT_NAME>/`。
- 不得把错误 Android device serial 的截图当成当前项目截图。
- 不得把另一个 Android 项目的 App 截图当成当前项目截图；截图前必须校验前台包名等于目标 `applicationId`。
- 不得猜 Android `applicationId` 或主 Activity；必须从当前项目配置或设备 package manager 输出确认。
- 不得覆盖旧 actual screenshot；Android 截图文件名必须唯一。
- 不得把项目内临时截图作为最终视觉证据；必须复制到 visual-evidence 后才算截图链成功。
- 不得把 `installDebug` 误解为普通 UI 修改许可；它只可作为 screenshot preflight 的最小安装步骤，且必须报告 `INSTALL_NEEDED_FOR_SCREENSHOT`、`INSTALL_COMMAND`、`INSTALL_RESULT`。
- 不得清理子项目 build/cache 来处理视觉证据；也不得删除当前 `RUN_ID` 的 visual-evidence 目录。

## Claude Code Desktop / Codex 共同禁区补充

- 不得让 Codex Agent 读写业务源码、查看具体业务 diff、运行构建或测试。
- 不得隐藏正式任务窗口，不得回退到 headless、`claude -p`、stdin feed、task-file launcher 或 `--resume` 旧会话。
- 不得忽略 `cd -> pwd -> claude`，不得在 `pwd` 不匹配时继续。
- 不得用“进程还活着”当进展。
- 不得等全部项目完成才处理先完成项目。
- 不得无预算无限运行。
- 不得忽略用户验收反馈。
- 不得让 Claude 根据文字描述重写而不读 Apple 源码或 reference screenshot。
- 不得修改 Apple 源项目。
- 不得修改、删除、重命名参考图目录。
- 不得清理工作区。
- 不得清理 `~/.hvigor`、用户级 DevEco/HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 不得全局安装 `pnpm`、npm 包或 ohpm 包。
- 不得删除 visual evidence、截图、qwen 输出、state、checkpoint、report。
- 不得用无效桌面截图冒充 App/Preview/窗口视觉验收。
- 不得把 `REFERENCE_ONLY` 当失败。
- 不得把 `READY_FOR_USER_REVIEW` 当默认终止态。
- 不得把 `WINDOWS_HOST_VALIDATION_PENDING` 当默认终止态。
- 不得把 WinUI 3 `WMC0011 Unknown member` 默认归因于 SDK 缺失；先排查 WPF/Avalonia/幻觉 XAML 属性。
