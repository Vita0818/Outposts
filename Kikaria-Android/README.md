# Kikaria-Android

Kikaria 的 Android 原生重建版,以只读参照项目 `Kikaria-Apple`(SwiftUI)为源,使用 Kotlin + Jetpack Compose(Material 3)完整移植。

## 产品

Kikaria 是本地优先的背诵/记忆助手:导入结构化 Markdown 知识点(标题/tags/hint/content),按标签随机背诵,先回忆再看提示/答案,把不熟的内容加入重点集锦、把掌握的内容标记为已掌握,并跟踪每日目标、倒数日与学习历史。无账号、无云同步、无网络依赖。

## 环境要求

- Android Studio(或仅命令行 Gradle)
- JDK 17+
- Android SDK Platform 34
- Kotlin 1.9.22 / AGP 8.2.2 / Gradle 8.5 / Compose BOM 2024.02.00

## 构建

```bash
./gradlew assembleDebug
# 产物: app/build/outputs/apk/debug/app-debug.apk
```

仓库不含 `local.properties`(SDK 路径),Android Studio 打开会自动生成;命令行构建请自行为 `sdk.dir` 指向本机 SDK。

## 结构

```
app/src/main/
  AndroidManifest.xml
  assets/presets/            # 内置预设(从 Kikaria-Apple/Presets 原样复制)
  java/com/vita0818/kikaria/
    MainActivity.kt          # 入口 + 路由(AppRoute 对齐)
    AppModel.kt              # 运行态 + 业务动作(ContentView.swift 的 @State 集合)
    data/                    # 模型/Markdown 解析/存档迁移/学习进度判定
    math/                    # LaTeX 词法、jlatexmath 位图渲染、可读文本 fallback
    notif/                   # 学习进度本地通知(AlarmManager)
    ui/theme/                # 色板/渐变/主题(KikariaTheme.swift 对齐)
    ui/math/MathText.kt      # 数学混排(空行分段/行内图片占位/块级居中)
    ui/Components.kt         # 玻璃卡/搜索栏/渐变按钮/Toast/空态
    ui/pages/                # 17 个页面:首页/引导/资料设置/背诵/范围/概览/历史/
                             # 重点集锦/已掌握/设置/预设管理/知识点编辑/格式说明
```

## 已移植

- 数据模型与存档结构(schemaVersion 4、per-preset 状态、内置预设合并迁移、废弃 ID 清理、内置内容变更重置状态)
- Markdown 知识点解析/导出(整行 `---` 分块、`#` 标题、`tags:`/`hint:`/`content:` 整行标记、中英文逗号分标签)
- 复习流程:shuffle 队列(避免与上一点重复)、查看提示/答案、三模式(normal/reinforcement/mastered)动作网格、左滑加入重点(绝不误标掌握)、上滑看答案/下一个、下滑上一个、右滑打开范围选择、每日复习计数
- 重点集锦/已掌握语义:重点可重复累加(×n)、标记掌握同时清空重点、移出互不影响另一状态
- 今日概览、月历热力复习历史(每日 reviewedAnswer 计数四档染色)
- 每日目标(1-100)/倒数日(起止校验)/进度安全线判定与每日一次本地提醒(需通知权限;Android 13+ 运行时申请)
- 预设管理:切换(确认框)/新建(粘贴或导入 .md/.txt)/编辑元数据/导出 Markdown/删除(至少保留一个)
- 知识点增删改(保留 id/掌握/重点计数)、Markdown 格式说明页 + AI Prompt 复制
- 首次资料设置(头像照片选择+512px JPEG 压缩/昵称/用户名)与 3 页新手引导(可重放)
- LaTeX:$...$/$$...$$ 词法(代码块不解析、\$ 转义、行内不跨行)、jlatexmath 位图渲染(行内 ×1.02/块级 ×1.34 clamp)、渲染失败时完整 Unicode 可读 fallback(对齐 readableMathFallback)
- 主题:全部 19 组 Light/Dark 色值与 6 组渐变、玻璃卡、深色模式;中英文混排(中文无衬线/拉丁衬线)

## 与 Apple 版差异

- 公式渲染用 jlatexmath-android(Apple 为 SwiftMath);失败 fallback 文本转换逐条对齐
- 无桌面小组件(Apple 有 iOS Widget);无 macOS 双栏布局(手机单列布局)
- 通知为每日一次 AlarmManager 提醒(Apple 为 UNCalendarNotificationTrigger);无开机自动重排
- 复习页键盘快捷键/滚轮选择器等桌面交互不适用,滚轮改为列表点选对话框
- 持久化为 SharedPreferences JSON(字段结构与 Apple Codable 存档对齐,但跨端不互通档案)
- 版本基于仓库内已验证工具链(compileSdk 34);应用可在 Android 17(API 37)设备/模拟器上运行

## 验证状态

- 本仓库于 macOS 使用 `./gradlew assembleDebug` 构建通过。
- 已在 Android 17(API 37,arm64 模拟器)上完成真机流程验证,截图证据见 `runtime-evidence/`:
  首次资料设置 → 3 页引导 → 首页(24 标签/0/20 进度)→ 背诵(提示/答案/公式图片渲染/三模式动作/
  掌握标记/下一个)→ 今日概览(计数口径与 Apple 一致)→ 复习历史(月历热力+当日 4 类统计)→
  已掌握列表 → 设置页 → 预设切换(确认框+徽章+每预设状态隔离,大学物理 286/英语 748/微积分 223/
  离散 994/994 知识点)。
- 未覆盖运行时验证:新建/编辑预设、知识点编辑、通知实际触发、范围标签多选交互(代码路径已实现)。
