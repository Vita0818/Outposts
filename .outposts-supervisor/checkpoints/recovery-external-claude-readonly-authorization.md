# Outposts Recovery External Claude Authorization

用户已在当前 Codex Agent 对话、当前 Outposts crash recovery 阶段中明确授权调用 Claude Code，并允许 Claude Code 读取限定路径内的本地项目内容，将必要上下文发送给外部模型服务分析，以生成只读恢复报告。

授权范围：

1. `/Users/vita/Vitemis/Outposts`
2. `/Users/vita/Vitemis/Vela`

授权目的仅限恢复上一轮 Codex/Claude Code 卡死后的项目状态。

限制：

1. 只允许只读恢复报告。
2. 不允许修改目标项目源码。
3. 不允许修改 Apple 源项目。
4. 不允许删除、清理、重置、恢复、checkout、reset、clean 文件。
5. 不允许运行构建或测试。
6. 不允许 commit、push、PR。
7. 不允许读取或发送密钥、token、私钥、证书、.env、p12、provisioning profile、ssh key、API key、Keychain 内容等敏感信息。
8. 不允许访问无关目录。
9. 不允许从第 1 轮重新开始迁移。
10. 不允许发送正式迁移 prompt。

