# Spark + Qwen 视觉辅助模式规则更新

- 日期：2026-06-05
- 根目录：/Users/vita/Vitemis/Outposts
- 目标：按用户说明新增 Spark + Qwen 视觉辅助子模式，仅更新监督层规则与文档。

## 更新背景

GPT-5.3-Codex-Spark 本身不具备稳定的多模态图片判断能力，历史视觉任务不能让 Spark 本体直接通过肉眼或文本推断图片。为避免误判，必须把视觉判断交给 Qwen3.7Plus（或已接入 helper）并将其结果回流到代码修改计划。

## 三种模式定义（本轮新增）

1. **Spark**：纯代码、非视觉任务。
2. **Spark + Qwen**：涉及截图、reference/actual、UI_AUDIT、像素级可见差异的任务。
   - 执行者：Codex-Spark。
   - 视觉判断：Qwen3.7Plus。
   - 禁止：Spark 自主判图。
3. **Agent**：需要 Claude Code 读源码、构建、测试与持续调度的模式。

## qwen 接入注意事项

- 优先复用现有 qwen helper / MCP / API wrapper，不重复引入新链路。
- 禁止硬编码或将 API Key 写入仓库。
- 允许从环境变量读取 `DASHSCOPE_API_KEY` 或 `QWEN_API_KEY`。
- Qwen 仅负责图片识别与对比，不得改文件。
- 视觉证据与 Qwen 输出保持在：
  - `/Users/vita/Vitemis/Outposts/.outposts-supervisor/visual-evidence/<BATCH>/<RUN>/<PROJECT>/qwen/`
- Spark 只读该报告，不得将源码、token、`.env`、证书传给 qwen。

## network_access 影响

若 `.codex/config.toml` 的 `network_access = false`，且当前无可复用 qwen helper，Spark + Qwen 任务应直接阻塞并在报告/汇总中标注：

- `QWEN_HELPER_NETWORK_NOT_AVAILABLE`

不应为满足该模式而将 Codex 整体改为无限制网络。

## 后续命令建议

1. 当视觉任务命中时使用关键词触发：
   - `Spark 模式 + 截图/视觉/qwen/像素级/reference/actual`。
2. 在任务文档或 prompt 中补充：
   - 模式=`SPARK_QWEN`
   - Qwen 辅助调用链与输出路径
3. 若无 Qwen 可用，改用 Agent 模式（由 Claude Code + qwen-vision MCP 执行）或明确要求只做 reference-first 静态修复。
