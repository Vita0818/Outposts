# qwen-vision local settings update

- Date: 2026-05-28
- Root: /Users/vita/Vitemis/Outposts
- Scope: only the five project `.claude/settings.local.json` files plus this supervisor report.

## qwen-vision allow rules

Added the following permission rules where missing:

- `mcp__qwen-vision__inspect_screenshot`
- `mcp__qwen-vision__compare_screenshots`
- `mcp__qwen-vision__extract_text_and_controls`

Updated files:

- `/Users/vita/Vitemis/Outposts/Kikaria-Android/.claude/settings.local.json`
- `/Users/vita/Vitemis/Outposts/Kikaria-HarmonyOS/.claude/settings.local.json`
- `/Users/vita/Vitemis/Outposts/Rokurics-Android/.claude/settings.local.json`
- `/Users/vita/Vitemis/Outposts/Rokurics-HarmonyOS/.claude/settings.local.json`
- `/Users/vita/Vitemis/Outposts/Rokurics-Windows/.claude/settings.local.json`

Existing permissions, deny rules, ask rules, sandbox settings, and other keys were preserved.

Potential conflict:

- `Kikaria-Android/.claude/settings.local.json` still contains `disabledMcpjsonServers: ["qwen-vision"]`. This was not changed in this task. It may prevent qwen-vision from loading even though the allow rules are now present.

## DevEco / HarmonyOS screenshot command check

PATH result:

- `which hdc`: not found.

DevEco bundled hdc:

- `/Applications/DevEco-Studio.app/Contents/sdk/default/openharmony/toolchains/hdc`: exists and is executable.

Confirmed from `hdc --help`:

- `list targets [-v]`
- `shell [COMMAND...]`
- `file recv [option] remote local`

Target status:

- `hdc list targets`: `Connect server failed`
- `hdc list targets -v`: `Connect server failed`
- `hdc checkserver`: `Connect server failed`
- `hdc shell snapshot_display --help`: `Connect server failed`

Conclusion:

- The local hdc binary is available via the DevEco bundle, but no working hdc server/target was confirmed in this run.
- Because `snapshot_display --help` could not reach a device/server, the device-side `snapshot_display` options were not locally confirmed from a target.
- The intended device/emulator screenshot chain remains:
  - `hdc shell snapshot_display -f /data/local/tmp/outposts-shot.jpeg`
  - `hdc file recv /data/local/tmp/outposts-shot.jpeg <local-output-path>`
- If multiple targets are present, use hdc target selection, such as `-t <connectkey>`, and do not use screenshots from the wrong device.
- 未确认到 DevEco Preview 面板专用 CLI 截图命令；设备/模拟器截图可用 hdc snapshot_display；Preview 面板可考虑 DevEco 内置截图功能或 macOS screencapture 兜底。
