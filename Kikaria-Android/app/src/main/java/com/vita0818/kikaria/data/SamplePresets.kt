package com.vita0818.kikaria.data

/**
 * Built-in sample presets for first launch, translated from KnowledgePoint.swift.
 *
 * These provide immediate content so the app can launch and display study
 * material without any network access or file import.
 */
object SamplePresets {

    val advancedMath = KnowledgePreset(
        id = "advanced-math",
        name = "高等数学知识点",
        subtitle = "微积分与线性代数核心概念",
        description = "涵盖极限、导数、中值定理、矩阵等高等数学核心知识点",
        category = "数学",
        isBuiltIn = true,
        markdownText = """
# 极限的保号性

tags: 微积分, 极限, 基础

hint:
若极限为正，则函数值在附近也为正。

content:
若 lim f(x) = A 且 A > 0，则在充分小的邻域内 f(x) > 0。

---

# 罗尔定理

tags: 微积分, 中值定理

hint:
验证连续性、可导性以及端点函数值相等。

content:
若 f 在 [a,b] 上连续，在 (a,b) 内可导，且 f(a) = f(b)，则存在 c ∈ (a,b) 使得 f'(c) = 0。

---

# 导数乘法法则

tags: 微积分, 导数, 基础

hint:
每次对一个因子求导，然后将两项相加。

content:
对于可导函数 f 和 g，(fg)' = f'g + fg'。

---

# 矩阵乘法维度

tags: 线性代数, 矩阵, 基础

hint:
内部维度必须匹配。

content:
m×n 矩阵可与 n×p 矩阵相乘，结果为 m×p 矩阵。

---

# 线性无关

tags: 线性代数, 向量空间

hint:
只有平凡的系数组合才能得到零向量。

content:
向量 v1 到 vn 线性无关，若 c1v1 + ... + cnvn = 0 蕴含 c1 = ... = cn = 0。

---

# 贝叶斯定理

tags: 概率论, 条件概率

hint:
通过先验和证据来反转条件概率。

content:
P(A|B) = P(B|A)P(A) / P(B)，假设 P(B) 不为零。
        """.trimIndent()
    )

    val collegeEnglish = KnowledgePreset(
        id = "college-english",
        name = "大学英语 Band 4",
        subtitle = "核心词汇与释义",
        description = "大学英语四级考试核心词汇，包含英文释义与中文翻译",
        category = "英语",
        isBuiltIn = true,
        markdownText = """
# commencement

tags: Unit 1, new words

hint:
The high school commencement speaker was giving a rather inspiring address.

content:
1. A ceremony at which university, college, or high school students receive their diplomas.
2. The beginning of something.
1. 毕业典礼。2. 开始；开端。

---

# confront

tags: Unit 1, new words

hint:
You must confront your fears and doubts and take risks again and again.

content:
Deal with a problem or difficult situation.
处理；解决。

---

# mediocre

tags: Unit 1, new words

hint:
Tourists crowd the gift shops to buy mediocre products at high prices.

content:
Not very good.
不太好的。

---

# deadline

tags: Unit 1, new words

hint:
Without extra help, it's going to be very difficult for us to meet the Friday deadline.

content:
A date or time by which you have to do or complete something.
截止时间；最后期限。

---

# adequate

tags: Unit 1, new words

hint:
The lunchtime menu is more than adequate to satisfy the biggest appetite.

content:
Enough in quantity or good enough in quality for a particular purpose.
足够的；充分的；合乎需要的。
        """.trimIndent()
    )

    val all: List<KnowledgePreset> = listOf(advancedMath, collegeEnglish)

    val defaultPreset: KnowledgePreset
        get() = all.first { it.id == KnowledgePreset.DEFAULT_PRESET_ID }
}
