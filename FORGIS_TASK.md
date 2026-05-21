本轮任务：将新版 Kikaria 迁移为 Android Kotlin Jetpack Compose 版本，目标写入 target_subdir：Kikaria-Android。

严格范围：
1. 只允许读取 source repo 中的 Kikaria 源码、资源、配置和文档。
2. 只允许写入 target repo 的 Kikaria-Android 目录。
3. 不允许修改 target repo 中 Kikaria-Android 之外的任何目录。
4. 不允许访问 source repo / target repo 之外的文件。
5. 不允许访问桌面、下载、文档、其他项目、钥匙串、系统目录或个人文件。
6. 不允许全盘搜索。
7. 不允许写入 secrets、token、API key 或私人路径。
8. 如果需要访问范围外文件，必须停止并说明原因。

迁移目标：
把新版 Kikaria 的 iOS / SwiftUI 应用迁移为 Android 版本，技术栈为 Kotlin + Jetpack Compose。

第一轮目标不是完整迁移所有功能，而是建立可继续迭代的 Android 工程基础：

1. 创建或补全 Kikaria-Android 的基础工程结构。
2. 保持源项目的信息架构、主要页面、核心交互和视觉风格。
3. 优先迁移核心模型、数据结构、主要页面骨架和导航关系。
4. 优先迁移新版 Kikaria 的首页 / 学习入口 / 预设或知识点管理 / 背诵流程相关结构。
5. 暂时无法完整迁移的功能，应记录为 deferred 或 TODO，不要随意重设计。
6. 不要为了适配 Android 而改变产品结构。
7. 不要引入无关功能。
8. 不要引入复杂第三方依赖，除非 Android 基础工程必须。

UI / UX 要求：
1. 尽量保持 Kikaria 原有的简洁、精装书感、低噪声界面。
2. 迁移 SwiftUI 到 Compose 时，优先保持布局层次、视觉密度、组件关系和交互意图。
3. 不要把界面改成普通 Material Demo 风格。
4. 不要随意增加多余按钮、提示、卡片、说明文字。
5. 相似组件必须尽量复用，不要复制粘贴多个重复实现。
6. 首页、复习页、设置页、知识点/预设管理页之间的标题字号、边距、按钮风格要统一。
7. 圆形按钮优先使用系统图标，不要自造复杂图形。
8. 动画保持克制，避免性能风险。
9. 如果源项目中有细粒度字体管理逻辑，Android 版本也要建立集中式 typography / script-aware text 的基础，而不是到处硬编码 font。

重要产品规则：
1. 用户名、昵称、头像等必须来自用户资料/设置，不得硬编码 “Vita”。
2. 如果字符串中混合英文、数字、中文，应考虑未来做 script-aware typography，不要把所有文本粗暴套同一个字体。
3. 重点集锦、已掌握、每日目标、倒数日、预设切换、知识点导入/管理等逻辑应尽量参考源项目。
4. 如果某个功能源代码还没读到，不要猜测实现；先搜索/读取相关文件。
5. 不要编造不存在的源项目行为。

工程要求：
1. Android 代码应使用 Kotlin + Jetpack Compose。
2. 目录结构应清晰，便于后续继续迁移。
3. 尽量建立 shared model / state / repository / UI component 分层。
4. 不要一次性塞进巨大单文件。
5. 不要提交不可解释的大范围重构。
6. 每次修改后使用 git_diff 自查。
7. 如果 build/test command 未配置，不要伪造构建结果；在报告中说明 skipped。
8. 如果生成 migration plan，请围绕 active unit 工作，不要跳到无关文件。
9. 不自动执行下一个 unit。

输出要求：
1. 修改完成后生成清晰 final summary。
2. 报告哪些 SwiftUI 文件已读取。
3. 报告哪些 Android 文件已创建或修改。
4. 报告哪些功能已迁移、哪些 deferred、哪些 blocked。
5. 报告下一轮建议迁移的 unit。
6. 不输出完整源码、完整 diff、secrets 或私人绝对路径。
