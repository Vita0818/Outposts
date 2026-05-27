# Do Not Break

以下规则是 Outposts Codex Supervisor 的禁区。任何未来 Codex Agent 在本目录调度 Claude Code 时都不得破坏。

## 角色禁区

- 不得让 Codex Agent 自己写业务代码。
- 不得让 Codex Agent 自己读业务源码。
- 不得让 Codex Agent 自己跑构建测试。
- 不得让 Codex Agent 代替 Claude Code 判断源码迁移细节。
- 不得把 Claude Code 自评当成用户验收。

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
- 不得在时间预算到达后启动新轮。
- 不得强杀正在正常运行的 Claude Code 来满足软时间预算。
- 不得从第一轮重跑。
- 不得重复发送上一轮 prompt。
- 不得在状态未知时继续正式迁移。

## 用户反馈禁区

- 不得把用户验收反馈降级。
- 不得用 Claude Code 报告覆盖用户手工观察。
- 不得忽略用户要求暂停、停止、只汇报或等待确认。
- 不得把用户指出的问题归为“已完成”而不安排下一轮处理。

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

## 安全禁区

- 不得访问无关目录。
- 不得读取或发送敏感信息。
- 不得读取 `.env`、token、私钥、证书、p12、provisioning profile、ssh key、API key、Keychain 内容。
- 不得把一次本地执行策略授权扩展为全局授权。
- 不得在模型、计费或授权状态异常时继续正式迁移。

## 报告禁区

- 不得把 Claude Code 长报告全文贴给用户。
- 不得省略阻塞原因。
- 不得省略构建或测试未运行的事实。
- 不得把子项目源码细节写入主管摘要。
- 不得假装已经读取或验证子项目源码。
