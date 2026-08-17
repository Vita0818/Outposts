# Intatis Android

原生 **Android 17（API 37）** 的 Intatis——对着只读参考 `Intatis-Apple`（Apple-first
本地 AI 工作区）重建的 Android 对应实现。按 Apple 项目自己的移动端契约
（“iOS 是严格的 Chat 子集：不链接 Tools、Permission、AgentKernel、Cowork、MCP 或本地
workspace/shell”），Android 版同为 **严格 Chat 子集**：流式聊天、provider/model 配置、
会话历史，不包含 Code/Cowork/工具链。

```text
shared/   纯 Kotlin JVM 模块：会话引擎（协议 / EventLog / 投影 / SSE / OpenAI 兼容
          流式 / 配置导入 / ChatLoop），桌面与 Android 共用
cli/      intatis JVM 命令行（chat REPL + 离线 selftest，24 项断言）
app/      Compose Android 应用（Chat / Sessions / Settings）
```

## 版本矩阵（本机验证过）

| 组件 | 版本 |
| --- | --- |
| compileSdk / targetSdk | **37**（Android 17；平台由 AGP 构建时自动安装 `android-37.0`） |
| minSdk | 26 |
| Gradle | 9.4.1 |
| AGP | 9.0.1（内置 Kotlin 支持；compileSdk 37 需 AGP 9.x，8.x 不识别 `android-37.0`） |
| Kotlin / Compose 插件 | 2.3.0 |
| Compose BOM | 2026.01.00 |
| OkHttp / serialization / coroutines | 4.12.0 / 1.9.0 / 1.10.2 |

## 构建与验证（本机已全部通过）

```bash
cd Intatis-Android
./gradlew :cli:run --args="selftest"   # 离线自测：24 passed, 0 failed
./gradlew :app:assembleDebug           # 产出 app/build/outputs/apk/debug/app-debug.apk
```

模拟器验证（API 37 AVD "kikaria17"）：

```bash
adb install -r app/build/outputs/apk/debug/app-debug.apk
adb shell monkey -p com.intatis.android -c android.intent.category.LAUNCHER 1
adb shell dumpsys activity activities | grep ResumedActivity   # 应为 com.intatis.android/.MainActivity
adb exec-out screencap -p > chat.png
```

本机验证记录：selftest 24/24 通过；`assembleDebug` 成功；安装启动后前台包名
`com.intatis.android/.MainActivity`；Sessions 页真实列出设备上创建的会话
（EventLog 在 `filesDir/intatis/sessions/` 落盘并回读成功）。截图见
`runtime-evidence/`。

## 配置

与 Apple 版同构的 Intatis JSON/JSONC 配置，查找顺序：

1. `INTATIS_CONFIG` 环境变量
2. 应用私有目录 `files/intatis/intatis.json[c]`（可通过 Settings 页提示的路径推入：
   `adb push intatis.json /data/data/com.intatis.android/files/intatis/`）
3. CLI 侧 `~/.intatis/intatis.json[c]`

```jsonc
{
  "model": "chat/gpt-4o-mini",
  "provider": {
    "chat": {
      "npm": "@ai-sdk/openai-compatible",
      "options": {
        "baseURL": "https://api.openai.com/v1",
        "apiKey": "{env:OPENAI_API_KEY}"   // 移动端建议 {file:...} 推入私有目录
      },
      "models": { "gpt-4o-mini": { "name": "GPT-4o mini" } }
    }
  }
}
```

- 密钥只以引用形式存在（`{env:VAR}` / `{file:path}` / auth file），构造 provider 时才解析，
  绝不进入 EventLog、session.json。
- `permission_reviewer_model` 等角色路由字段会解析并在 Settings 展示；对应能力
  （自动审查、图像、转写、Knowledge）未移植到移动端，字段仅保持配置兼容。
- CLI 侧支持 `INTATIS_BASE_URL` / `INTATIS_API_KEY` / `INTATIS_MODEL` 快捷覆盖。

## 架构契约（与 Apple 版一致）

- **EventLog 是会话唯一事实源**：`<sessions>/<id>/events.jsonl`，append-only、每会话
  单调 `seq`、写者租约互斥（`FileChannel.tryLock`，含同 JVM 重叠锁处理）；`session.json`
  是可重建、无密钥的派生缓存。
- 事件类型 **只增不改**（snake_case wire 名），读取端跳过未知未来类型并保留其 seq。
- UI 只消费折叠投影（`ConversationProjection` → `StateFlow`），不把模型原始输出当事实。
- 用户消息为 user 胶囊气泡（圆角 16），助手消息无气泡左对齐 + 角色行 + 流式省略号；
  composer 圆角 20；标题用衬线字体——对应 Apple 端 IntatisDesign 令牌的 Compose 映射。

## CLI

```bash
./gradlew :cli:run --args="config"     # 查看解析后的配置（密钥脱敏）
./gradlew :cli:run --args="chat"       # 流式聊天 REPL（/model /attach /exit）
./gradlew :cli:run --args="selftest"   # 离线测试套件
```

## 尚未移植（移动端契约之外或后续批次）

- Code / Cowork / 工具与权限链 / MCP / 多模态（Apple 移动端契约明确不含）
- hosted web search 与 citations 抓取、自动会话标题、Markdown 富渲染
- Widget / 平板双栏 / Material You 动态取色（当前使用语义化明暗主题）
