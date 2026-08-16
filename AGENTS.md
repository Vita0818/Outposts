# Outposts Working Context

本目录是 Outposts 根目录。

本文件服务于在本目录内启动的编码助手工作流。

## 工作模式

Outposts 使用正常工作模式：助手本体直接读取、修改、构建和验证目标项目，不经过任何 supervisor 或 worker 调度层。

权限边界：

- 允许：读取当前目标项目源码。
- 允许：修改 `/Users/vita/Vitemis/Outposts` 下当前目标项目文件。
- 允许：在当前目标项目内运行构建、测试、lint、截图、校验命令。
- 允许：写工作记录与报告。
- 禁止：修改 `/Users/vita/Vitemis/Vela`。
- 禁止：修改参考图目录。
- 禁止：访问无关目录。
- 禁止：读取、发送或记录密钥、token、`.env`、证书、ssh key、Keychain 内容等敏感信息。
- 禁止：执行破坏性 Git 操作。
- 禁止：清理工作区、构建产物、缓存或用户级工具链。

## 视觉工作

若任务涉及 UI、截图、视觉、像素级差异、reference/actual、设计稿或界面复刻：

- 视觉结论必须基于实际生成并读取过的截图，或用户明确反馈；不得凭想象或推测声称视觉验收完成。
- reference 与 actual 均有效时必须做对比。
- actual 不可用时只能报告 `REFERENCE_ONLY`，不得声称完成视觉闭环。

## 启动前检查

进入 Outposts 根目录后，必须先执行并记录：

```bash
pwd
git rev-parse --show-toplevel
git status --short
```

只有当 `pwd` 与 `git rev-parse --show-toplevel` 都指向 `/Users/vita/Vitemis/Outposts` 时，才允许继续修改项目文件或更新本目录级文档。若不匹配，停止修改并报告路径问题。

不得执行破坏性 Git 操作，包括 `git reset --hard`、`git clean -fd`、`git checkout .`、`git restore .`、强制 push、删除用户未提交文件。未经用户明文要求具体 Git 操作，不 add、不 commit、不 push、不创建 PR；编辑、整理、修复、验证或准备工作都不等于提交请求。若用户要求提交，只提交当前 Git root 中与本任务相关的文件；不得递归进入、暂存、提交或推送子仓库、submodule、nested Git repo 或依赖 checkout。

完成实现、修复、验证或文档维护后，必须将已完成的持久性改动及时回写到相关项目文档；若无需更新文档，最终报告说明原因。

## 相关文档

- `docs/NEXT_TARGET.md`（如果存在）：当前下一个目标。

`docs/archive/` 下的调度文档（OUTPOSTS_MODE_EXECUTION、OUTPOSTS_SUPERVISOR、WORKER_ONE_SHOT_INVOCATION_PROTOCOL、SUPERVISOR_WORKER_VISUAL_PROTOCOL、BATCH_SCHEDULING、RECOVERY_PLAYBOOK、SECURITY_AND_BOUNDARIES、REPORTING_FORMATS、DO_NOT_BREAK）属于已停用的 supervisor/worker 调度机制存档，仅作历史参考，不再作为工作规则执行。

## 最终报告要求

完成一轮工作后，输出给用户的是简明工作摘要，至少包含：

```text
CHANGES
VALIDATION_RESULT
UNCERTAINTIES
NEXT_RECOMMENDED_ACTION
```
