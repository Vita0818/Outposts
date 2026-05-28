# Outposts Boundary Incident Report

- DATE: 2026-05-28
- PURPOSE: 固化 qwen-vision 批次中暴露的边界问题，并作为后续调度前置规则。
- SCOPE: 仅记录 Outposts Codex Supervisor 调度规则，不记录子项目源码细节。

## Incident Summary

本次边界复盘来自 `outposts-qwen-ui-parity-and-winui-fix-round` 批次。

已确认需要加固的执行纪律：

1. HarmonyOS 项目不得把编译修复扩大成用户级工具链修复。
2. 视觉证据不得作为临时文件删除。
3. qwen-vision 调用必须区分“工具被调用”和“有效视觉验收完成”。
4. Codex Agent 本体继续只做调度、记录和主管摘要，不读源码、不写源码、不构建、不测试、不看具体 diff。

## Rules Written

### HarmonyOS 用户级目录与全局工具链禁令

未来调度中，Claude Code 不得：

- 删除、清理或修改 `~/.hvigor`。
- 删除、清理或修改用户级 DevEco、HarmonyOS SDK、Hvigor、ohpm、npm、pnpm 缓存。
- 全局安装 `pnpm`、npm 包、ohpm 包或任何全局工具链依赖。
- 执行全局工具链修复、SDK 修复或用户目录缓存重建。

如果工具链、SDK、网络代理或全局包管理器异常，只能报告：

- `HOST_ENV_BLOCKED`
- `TOOLCHAIN_REPAIR_NEEDS_USER`
- `BLOCKED_NEEDS_USER`

Claude Code 只允许在对应 Outposts 目标项目目录内修改源码和项目配置。

### 视觉证据保留规则

未来调度中，Claude Code 不得：

- 删除 `.outposts-supervisor/visual-evidence`。
- 删除当前批次截图、qwen 输出、state、checkpoint、report 或 batch state。
- 把“清理临时截图”作为任务收尾动作。
- 删除旧 `RUN_ID` 后复用路径。

如果需要重新截图，必须创建新的 `RUN_ID` 证据目录。

### qwen-vision 有效证据规则

报告必须区分：

```text
QWEN_CALLED:
QWEN_VALID_VISUAL_EVIDENCE:
QWEN_COMPARE_SCREENSHOTS_COMPLETED:
```

有效 actual screenshot 只能是：

- App 实际渲染画面。
- Android emulator 或真机纯设备截图。
- HarmonyOS Preview、真机或模拟器画面。
- Windows app 真实窗口截图。

无效截图包括：

- 未裁剪的全桌面截图。
- 无法明确定位 App/Preview 区域的 IDE 或桌面截图。
- 截错设备、截错项目、截错窗口的图片。
- 只有启动器、权限弹窗、桌面或无关应用的截图。

`qwen-vision` 看过无效截图，只能记为 `QWEN_CALLED=YES`，不得记为有效视觉验收。

## Codex Agent Boundary

Codex Agent 仍然不得：

- 读取业务源码。
- 修改业务源码。
- 运行构建或测试。
- 查看具体 diff。
- 清理工作区。
- commit、push、创建 PR。

Codex Agent 只负责：

- 真实可见 Claude Code 终端调度。
- 写入 `.outposts-supervisor` 调度记录。
- 读取 Claude Code 文字报告。
- 生成主管摘要。

## Next Batch Recommendation

建议不要把下一批做成五项目大规模功能批次。建议拆分为小批次：

1. `Rokurics-Android-dark-mode-theme-support`
   - 目标：只修 dark mode / theme support。
   - 验证：保持 build/test 绿色；qwen 检查暗色 home/library 截图。

2. `Rokurics-HarmonyOS-valid-screenshot-color-check`
   - 目标：先用有效 Preview、设备或模拟器截图确认黄色问题是否仍存在。
   - 禁止：全局 `pnpm`、用户级 `~/.hvigor`、用户级 DevEco/HarmonyOS 缓存操作。

3. `Kikaria-Android-visual-evidence-regeneration`
   - 目标：重新生成视觉证据，继续首页和背诵页视觉对齐。
   - 禁止：删除截图、state、checkpoint 或视觉证据。

4. `Kikaria-HarmonyOS-compile-recovery-safe-boundary`
   - 目标：先修编译。
   - 限制：不得清理用户级 Hvigor；需要用户级工具链操作时立即暂停并报告。

5. `Rokurics-Windows-win11-arm-validation`
   - 目标：等待 Windows 11 ARM + Visual Studio 2022 环境验证。
   - 限制：不得在 macOS 上假装 Debug/ARM64 build 或窗口 launch 通过。

## Files Updated In This Boundary Pass

- `docs/DO_NOT_BREAK.md`
- `docs/CLAUDE_CODE_VISUAL_MCP_PROTOCOL.md`
- `docs/BATCH_SCHEDULING.md`
- `docs/RECOVERY_PLAYBOOK.md`
- `.outposts-supervisor/reports/outposts-boundary-incident-20260528.md`
