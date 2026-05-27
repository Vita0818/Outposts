package com.vita0818.kikaria.ui.guide

import androidx.compose.foundation.background
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaScrollPageShell
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics

/**
 * Markdown format guide screen, translated from the iOS MarkdownFormatGuideView
 * in ContentView.swift.
 *
 * Shows the Kikaria structured Markdown format rules, templates, and examples.
 */
@Composable
fun MarkdownFormatGuideScreen(
    onBack: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText

    val metrics = rememberKikariaPhoneMetrics()

    KikariaScrollPageShell(onBack = onBack, metrics = metrics) {
        Spacer(modifier = Modifier.height(metrics.pageTopPadding))
                Text(
                    text = KikariaTypography.mixedText(
                        "Markdown 格式说明",
                        size = 28,
                        weight = FontWeight.Bold
                    ),
                    color = deepText
                )

                Spacer(modifier = Modifier.height(4.dp))

                Text(
                    text = KikariaTypography.mixedText(
                        "学习 Kikaria 结构化 Markdown 的格式规则与写法。",
                        size = 14,
                        weight = FontWeight.Medium
                    ),
                    color = softText
                )

                Spacer(modifier = Modifier.height(16.dp))

                // Introduction
                GuideCard(title = null) {
                    Text(
                        text = KikariaTypography.mixedText(
                            "Kikaria 使用结构化 Markdown 来导入知识点。" +
                                    "每个知识点由标题、标签、提示和答案组成。" +
                                    "多个知识点之间使用 --- 分隔。",
                            size = 15,
                            weight = FontWeight.Normal
                        ),
                        color = deepText,
                        lineHeight = 24.sp
                    )
                }

                Spacer(modifier = Modifier.height(12.dp))

                // Format template
                GuideCard(title = "格式模板") {
                    CodeBlock(
                        text = """# 知识点名称

tags: 标签1, 标签2, 标签3

hint:
这里写提示，可以是一句话，也可以是几行文字。

content:
这里写完整答案或背诵内容，可以是一段或多段文字。

---"""
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    Text(
                        text = "多个知识点之间用一行 --- 分隔。",
                        fontSize = 14.sp,
                        fontWeight = FontWeight.Medium,
                        color = softText
                    )
                }

                Spacer(modifier = Modifier.height(12.dp))

                // Format rules
                GuideCard(title = "规则说明") {
                    RuleText("标题必须以 # 开头。")
                    RuleText("tags: 后面写标签，多个标签用英文逗号或中文逗号分隔。")
                    RuleText("hint: 后面写提示。")
                    RuleText("content: 后面写完整内容。")
                    RuleText("每个知识点之间用单独一行 --- 分隔。")
                    RuleText("建议每个知识点不要太长，适合一次背诵。")
                    RuleText("标签可以用于后续选择背诵范围。")
                }

                Spacer(modifier = Modifier.height(12.dp))

                // LaTeX notes
                GuideCard(title = "LaTeX 公式") {
                    RuleText("行内公式必须写成：\$f(x)=x^2\$。")
                    RuleText("块级公式必须写成：用 \$\$...\$\$ 单独成块。")
                    RuleText("只有 \$...\$ 和 \$\$...\$\$ 中的内容会渲染为公式。")
                    RuleText("没有包裹的 LaTeX 命令不会自动识别，会按普通文本显示。")
                    RuleText("公式环境中不要混入中文，中文说明应放在公式外。")
                    RuleText("导入、编辑和导出都会保留原始 LaTeX 源码。")

                    Spacer(modifier = Modifier.height(8.dp))

                    CodeBlock(
                        text = """函数 ${'$'}f(x)=x^2${'$'} 的导数是 ${'$'}2x${'$'}。

当 x 接近 0 时，有：

${'$'}${'$'}
\\lim_{x\\to0}\\frac{\\sin x}{x}=1
${'$'}${'$'}"""
                    )
                }

                Spacer(modifier = Modifier.height(12.dp))

                // Complete example
                GuideCard(title = "完整示例") {
                    CodeBlock(
                        text = """# 极限的保号性

tags: 高等数学, 极限, 基础

hint:
当函数极限大于 0 时，函数值在充分靠近该点时也大于 0。

content:
若 lim f(x) = A，且 A > 0，则存在某个去心邻域，使得在该邻域内 f(x) > 0。

---

# 罗尔定理

tags: 高等数学, 中值定理

hint:
闭区间连续，开区间可导，两端函数值相等。

content:
若函数 f(x) 在 [a,b] 上连续，在 (a,b) 内可导，且 f(a)=f(b)，则至少存在一点 ξ∈(a,b)，使得 f'(ξ)=0。"""
                    )
                }

                Spacer(modifier = Modifier.height(12.dp))

                // AI prompt
                GuideCard(title = "给 AI 助手的 Prompt") {
                    Text(
                        text = KikariaTypography.mixedText(
                            "你可以把下面的 Prompt 发给 AI 助手，让它帮你整理成 Kikaria 格式。",
                            size = 14,
                            weight = FontWeight.Medium
                        ),
                        color = softText,
                        lineHeight = 22.sp
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    CodeBlock(
                        text = """请你把我提供的学习资料整理成 Kikaria 背诵 App 支持的结构化 Markdown 知识点。

格式必须严格遵守：

# 知识点名称

tags: 标签1, 标签2, 标签3

hint:
用简洁语言给出背诵提示，不要直接泄露完整答案。

content:
写出完整、准确、适合背诵的知识点内容。

---

要求：
1. 每个知识点之间必须用单独一行 --- 分隔。
2. 每个知识点都必须包含标题、tags、hint、content 四部分。
3. tags 后的标签用逗号分隔。
4. hint 要简短，适合作为回忆提示。
5. content 要完整、准确、适合直接背诵。
6. 不要生成多余解释。
7. 不要使用表格。
8. 不要把多个知识点混在一起。
9. 如果原资料太长，请拆分成多个小知识点。
10. 输出结果只保留 Markdown 内容，不要添加寒暄或说明。
11. 数学公式可以使用 LaTeX。
12. 只有 ${'$'}...${'$'} 和 ${'$'}${'$'}...${'$'}${'$'} 中的内容会渲染为公式。
13. 行内公式用 ${'$'}...${'$'}，块级公式用 ${'$'}${'$'}...${'$'}${'$'}。
14. 公式环境中不要混入中文，中文解释要写在公式外。

下面是需要整理的资料：

【在这里粘贴课本、讲义、笔记或 OCR 文本】"""
                    )
                }

                Spacer(modifier = Modifier.height(32.dp))
    }
}

// ─── Guide Card ───

@Composable
private fun GuideCard(
    title: String?,
    content: @Composable () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText

    KikariaGlassCard(
        modifier = Modifier.fillMaxWidth(),
        cornerRadius = 24.dp,
        fillOpacity = 0.42f
    ) {
        Column(modifier = Modifier.padding(18.dp)) {
            if (title != null) {
                Text(
                    text = KikariaTypography.mixedText(
                        title,
                        size = 18,
                        weight = FontWeight.SemiBold
                    ),
                    color = deepText
                )
                Spacer(modifier = Modifier.height(12.dp))
            }
            content()
        }
    }
}

// ─── Code Block ───

@Composable
private fun CodeBlock(text: String) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val mist = if (isDark) KikariaColors.MistDark else KikariaColors.Mist

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(12.dp))
            .background(mist.copy(alpha = 0.5f))
            .padding(14.dp)
    ) {
        Text(
            text = text,
            fontSize = 12.sp,
            fontWeight = FontWeight.Normal,
            fontFamily = FontFamily.Monospace,
            color = deepText.copy(alpha = 0.88f),
            lineHeight = 19.sp
        )
    }
}

// ─── Rule Text ───

@Composable
private fun RuleText(text: String) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText

    Text(
        text = KikariaTypography.mixedText(
            text,
            size = 14,
            weight = FontWeight.Medium
        ),
        color = deepText.copy(alpha = 0.88f),
        lineHeight = 22.sp,
        modifier = Modifier.padding(vertical = 4.dp)
    )
    }
