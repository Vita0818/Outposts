# Do Not Break

以下规则是 Outposts Agent 与 Spark 两种模式的禁区。任何未来 Codex、DeepCode、QwenCode 或 Spark 执行时都不得破坏。

## 模式禁区

- 不得混用 Agent 与 Spark 的权限边界。
- 不得让 Spark 冒充 Agent supervisor，也不得让 Agent supervisor 直接承担 Spark 实现职责。
- 不得因为任务紧急而跳过模式声明确认。

## Agent supervisor 禁区

- 不得让 Supervisor 自己写业务代码。
- 不得让 Supervisor 自己读业务源码。
- 不得让 Supervisor 自己跑构建测试。
- 不得让 Supervisor 查看具体业务 diff。
- 不得让 Supervisor 直接判读截图。
- 不得让 Supervisor 代替 DeepCode 判断源码迁移细节。
- 不得让 Supervisor 代替 QwenCode 判断视觉差异。
- 不得把 DeepCode / QwenCode 自评当成用户验收。

## Worker 调度禁区

- 不得把 DeepCode 或 QwenCode 当作可持续多次交互会话。
- 不得进入 worker 窗口后再追加第二条业务 prompt。
- 不得复用上一轮 DeepCode / QwenCode 窗口上下文。
- 不得让 worker 依赖上一轮窗口记忆。
- 不得省略 `OUTPUT_FILE`。
- 不得覆盖旧 `DeepCode-output` 或 `QwenCode-output` 文件。
- 不得省略上一轮 DeepCode / QwenCode 报告路径。
- 不得让 DeepCode “记住上一轮视觉结论”；必须显式传入 QwenCode 报告路径。

## DeepCode 禁区

- 不得读取或发送敏感信息。
- 不得修改 Apple 源项目。
- 不得修改参考图目录。
- 不得执行破坏性 Git 操作。
- 不得清理工作区、缓存、构建产物或用户级工具链。
- 不得把无效截图报告为有效视觉验收。
- 不得把 QwenCode 当成代码修改者。
- 不得直接调用 QwenCode。
- 不得调用任何视觉 helper。
- 不得要求 QwenCode 回答问题。
- 不得在未读取 supervisor 指定的 QwenCode 报告时声称已根据 Qwen 视觉结果修复。

## QwenCode 视觉禁区

- 不得把 QwenCode 当代码执行器。
- 不得让 QwenCode 修改文件。
- 不得让 QwenCode 读取源码。
- 不得让 QwenCode 读取 DeepCode-output。
- 不得让 QwenCode 运行构建或测试。
- 不得让 QwenCode 接收 API Key、`.env`、token、密钥、证书、完整源码或私密配置。
- 不得让 QwenCode 与 DeepCode 直接通信。
- 不得在无 QwenCode 图片识别/对比报告下宣称像素级视觉任务完成。
- 不得把 qwen API Key 写入仓库文件、配置文件或报告。
- 不得因为网络受限或执行器不可用就跳过 QwenCode 报告要求并直接收口。

## Spark 禁区

- 不得在未确认 `GPT-5.3-Codex-Spark` 时执行 Spark。
- 不得在模型无法确认时继续执行 Spark。
- 不得在 Spark 模式下修改 `/Users/vita/Vitemis/Vela`。
- 不得在 Spark 模式下修改参考图目录。
- 不得在 Spark 模式下读取或发送敏感信息。
- 不得在 Spark 模式下执行破坏性 Git 操作。
- 不得伪装为 Agent 执行。

## 终端禁区

- 不得隐藏运行用户无法观察的正式任务。
- 不得使用用户不可观察的后台通道作为正式任务主通道。
- 不得忽略 `cd -> pwd -> <one-shot invocation>`。
- 不得在 `pwd` 未确认前启动正式任务。
- 不得跳过 one-shot prompt 首段模型/路径校验。
- 不得把“进程还活着”当作进展。

## 调度禁区

- 不得等所有项目完成后才处理先完成项目。
- 不得无预算无限运行。
- 不得在时间预算和轮次预算尚未耗尽、且项目无硬阻塞时，因为软状态提前收束批次。
- 不得把 `READY_FOR_USER_REVIEW` 当成默认终止态；必须检查 remaining gaps、next recommendation 和可执行下一步。
- 不得把 `REFERENCE_ONLY` 当成失败或终止态。
- 不得因为缺 actual screenshot 就停止 reference-first 修正；只要 reference screenshot 可用，就应继续利用 QwenCode reference 理解推进。
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
- 不得用 worker 报告覆盖用户手工观察。
- 不得忽略用户要求暂停、停止、只汇报或等待确认。
- 不得把用户指出的问题归为“已完成”而不安排下一轮处理。
- 不得因为 QwenCode 判断“相似”就跳过用户人工验收。
- 用户人工视觉反馈优先级高于 QwenCode 的匹配判断。

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

未经用户明文要求具体 Git 操作，不 add、不 commit、不 push、不创建 PR；编辑、整理、修复、验证或准备工作都不等于提交请求。
若用户要求提交，只提交当前 Git root 中与本任务相关的文件；不得递归进入、暂存、提交或推送子仓库、submodule、nested Git repo 或依赖 checkout。

不得清理 build、cache、`.gradle`、`intermediates`。

不得删除、清理或修改：

- `~/.hvigor`
- 用户级 DevEco、HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存
- 全局工具链、全局包管理器状态

不得全局安装 `pnpm`、npm 包、ohpm 包或任何全局工具链依赖。

不得删除 `.outposts-supervisor/visual-evidence`、当前批次截图、QwenCode 输出、DeepCode 输出、state、checkpoint、report 或 batch state。重新截图必须创建新的 `RUN_ID` 证据目录，而不是覆盖或删除旧证据。

不得创建 mock 系统目录（如 `tmp-home/`、`tmp_appdata/`、`AppData/`、`Roaming/`、`Local/` 等），不得通过修改 `HOME`、`APPDATA`、`USERPROFILE` 等环境变量指向临时目录来绕过构建权限错误。若 `dotnet restore/build` 因读取 `%APPDATA%\NuGet\NuGet.Config` 被拒绝而失败，应在 csproj 同级目录放置项目级 `nuget.config` 解决，而不是创建 mock 用户目录。

## 安全禁区

- 不得访问无关目录。
- 不得读取或发送敏感信息。
- 不得读取 `.env`、token、私钥、证书、p12、provisioning profile、ssh key、API key、Keychain 内容。
- 不得把源码、密钥、token、`.env`、证书或私密配置传给 QwenCode。
- 不得把一次本地执行策略授权扩展为全局授权。
- 不得在模型、计费或授权状态异常时继续正式迁移。

## 报告禁区

- 不得把 DeepCode / QwenCode 长报告全文贴给用户。
- 不得省略阻塞原因。
- 不得省略构建或测试未运行的事实。
- 不得把子项目源码细节写入主管摘要。
- 不得假装已经读取或验证子项目源码。
- 不得在没有截图的情况下假装完成视觉验收。
- 不得把无效截图报告为有效视觉验收。
- 全桌面截图不算有效 actual screenshot，除非明确裁剪出 App、Preview 或窗口区域。
- QwenCode 看过无效桌面截图，只能报告 `QWENCODE_CALLED=YES` 与 `QWENCODE_VALID_VISUAL_EVIDENCE=NO`，不得报告为有效验收。
- 不得用 QwenCode 替代构建和测试。
- 不得把视觉证据截图散落到子项目源码目录或 Apple 源项目；必须写入 `.outposts-supervisor/visual-evidence/<BATCH_NAME>/<RUN_ID>/<PROJECT_NAME>/`。
- 不得把错误 Android device serial 的截图当成当前项目截图。
- 不得把另一个 Android 项目的 App 截图当成当前项目截图；截图前必须校验前台包名等于目标 `applicationId`。
- 不得猜 Android `applicationId` 或主 Activity；必须从当前项目配置或设备 package manager 输出确认。
- 不得覆盖旧 actual screenshot；Android 截图文件名必须唯一。
- 不得把项目内临时截图作为最终视觉证据；必须复制到 visual-evidence 后才算截图链成功。
- 不得把 `installDebug` 误解为普通 UI 修改许可；它只可作为 screenshot preflight 的最小安装步骤，且必须报告 `INSTALL_NEEDED_FOR_SCREENSHOT`、`INSTALL_COMMAND`、`INSTALL_RESULT`。
