package com.vita0818.kikaria.data

/**
 * Built-in sample presets for first launch, translated from KnowledgePoint.swift.
 *
 * Contains all 4 built-in presets from the iOS source:
 * - 大学物理 (University Physics)
 * - 大学英语Band4 (College English Band 4)
 * - 微积分 (Calculus, set theory + advanced math)
 * - 离散数学 (Discrete Mathematics - propositional logic)
 *
 * Each preset includes the first several knowledge point entries from the
 * corresponding source Markdown file under source/Presets/.
 *
 * Preset IDs follow the source convention: "builtin-{displayName}"
 */
object SamplePresets {

    // ─── 大学物理 (University Physics) ───

    val universityPhysics = KnowledgePreset(
        id = "builtin-大学物理",
        name = "大学物理",
        subtitle = "质点运动学与牛顿力学",
        description = "涵盖质点运动学、牛顿定律、动量能量等大学物理核心知识点",
        category = "物理",
        isBuiltIn = true,
        markdownText = """
# 力学研究对象

tags: 质点运动学

hint:
力学研究什么？运动学只描述什么？

content:
力学研究的对象是机械运动。

运动学只描述物体的运动，不研究产生运动的原因。

---

# 质点模型

tags: 质点运动学

hint:
质点忽略什么？它是不是几何点？

content:
质点是忽略物体形状和大小后得到的一种理想化模型。

注意：质点不是几何点，而是保留质量、忽略形状和大小的物理模型。

---

# 参考系

tags: 质点运动学

hint:
描述运动时选作参考的系统叫什么？

content:
参考系是描述运动时选作参考的物体系，或者说是观察者所在的系统。

运动的描述依赖参考系。

---

# 曲线运动的自然坐标分解

tags: 质点运动学

hint:
曲线运动的加速度可分解为哪两部分？

content:
研究曲线运动的方法是选取适当的坐标系将运动加以分解。

例如，平面运动在自然坐标系中可分解为切向加速度和法向加速度：

切向加速度：a_t = dv/dt

法向加速度：a_n = v^2/ρ

其中 ρ 为曲率半径。

---

# 位移、速度和加速度的特点

tags: 质点运动学

hint:
研究质点运动时，这三个量要注意哪些性质？

content:
研究质点运动时，必须注意位移、速度和加速度的相对性、瞬时性和矢量性。

它们都与参考系有关，并且通常是随时间变化的矢量。

---

# 相对运动的速度和加速度关系

tags: 质点运动学

hint:
两个相对平动参考系中，速度和加速度如何变换？

content:
相对运动是分析质点在两个相对作平动的参考系中，位移、速度和加速度之间的关系和差异。

若 S2 系相对 S1 系的速度和加速度分别为 v0 和 a0，则：

v2 = v1 - v0

a2 = a1 - a0

上述变换式仅在非相对论情况下成立。

---

# 牛顿第一定律和惯性系

tags: 牛顿定律

hint:
牛顿第一定律的内容是什么？惯性系的定义是什么？

content:
牛顿第一定律（惯性定律）：任何物体都保持静止或匀速直线运动状态，直到外力迫使它改变这种状态为止。

惯性系：牛顿第一定律成立的参考系称为惯性系。地球通常可近似看作惯性系。
        """.trimIndent()
    )

    // ─── 大学英语Band4 (College English Band 4) ───

    val collegeEnglishBand4 = KnowledgePreset(
        id = "builtin-大学英语Band4",
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
Would passengers please turn off their mobile phones before the commencement of the flight?

content:
1. A ceremony at which university, college, or high school students receive their diplomas.
2. The beginning of something.

1. 毕业典礼。
2. 开始；开端。

---

# faculty

tags: Unit 1, new words

hint:
A drop in enrollment will affect students, faculty, and administrators.
These plans were part of a reorganization that divided the university into six faculties.

content:
1. All the teachers in a university.
2. A department or group of related departments within a university.

1. 全体教员。
2. 系；部；院。

---

# confront

tags: Unit 1, new words

hint:
You must confront your fears and doubts and take risks again and again.
Here are some problems that have confronted scientists all over the world in the past decade.

content:
1. Deal with a problem or difficult situation.
2. If a problem or difficult situation confronts someone, it appears and needs to be dealt with by that person.

1. 处理；解决。
2. 临到……头上；使面对。

---

# quitter

tags: Unit 1, new words

hint:
I have asked you to do this tough task because I know you are not a quitter.

content:
Someone who does not have the determination or courage to finish something that is difficult.

遇到困难就放弃的人；半途而废的人。

---

# mediocre

tags: Unit 1, new words

hint:
Tourists crowd the gift shops to buy mediocre products at high prices.
His school record was mediocre.

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
The existing law is not adequate to deal with these problems.

content:
Enough in quantity or good enough in quality for a particular purpose.

足够的；充分的；合乎需要的。
        """.trimIndent()
    )

    // ─── 微积分 (Calculus / Advanced Math) ───

    val calculus = KnowledgePreset(
        id = "builtin-微积分",
        name = "微积分",
        subtitle = "集合论基础与极限理论",
        description = "涵盖集合论、映射、极限、导数、中值定理等微积分核心概念",
        category = "数学",
        isBuiltIn = true,
        markdownText = """
# 集合与元素

tags: 第一讲 预备知识

hint:
集合、元素以及属于关系怎样表示？

content:
集合是指由一些可以确定、可以分辨的事物构成的整体。通常用大写字母表示集合，例如 A, B, C, ..., X, Y, Z。

组成集合的成员称为集合的元素，通常用小写字母表示，例如 a, b, c, ..., x, y, z。

若 a 是集合 A 的一个元素，记为：a ∈ A

若 a 不是集合 A 的元素，记为：a ∉ A

---

# 子集、相等与真子集

tags: 第一讲 预备知识

hint:
子集、集合相等和真子集分别如何定义？

content:
若对任意 b ∈ B，都有 b ∈ A，则称 B 是 A 的子集，记为：B ⊂ A

若 A ⊂ B 且 B ⊂ A，则称 A 与 B 相等，记为：A = B

若 B ⊂ A 且 B ≠ A，则称 B 是 A 的真子集。

---

# 集合的并、交、差

tags: 第一讲 预备知识

hint:
并集、交集和差集分别由哪些元素构成？

content:
集合 A 与 B 的并集为：A ∪ B = {x | x ∈ A 或 x ∈ B}

集合 A 与 B 的交集为：A ∩ B = {x | x ∈ A 且 x ∈ B}

集合 A 与 B 的差集为：A - B = {x | x ∈ A 且 x ∉ B}

---

# 补集与德摩根律

tags: 第一讲 预备知识

hint:
补集如何定义？德摩根律怎样描述补集与并交的关系？

content:
设全集为 U，集合 A 的补集为：A^c = {x | x ∈ U 且 x ∉ A}

德摩根律：
(A ∪ B)^c = A^c ∩ B^c
(A ∩ B)^c = A^c ∪ B^c

---

# 映射的定义与三要素

tags: 第一讲 预备知识

hint:
映射由哪三要素确定？单射、满射和双射分别如何定义？

content:
映射 f: A → B 由定义域 A、值域 B 和对应法则 f 三要素确定。

若 f(x₁) = f(x₂) 蕴含 x₁ = x₂，则 f 为单射。

若对任意 y ∈ B，存在 x ∈ A 使 f(x) = y，则 f 为满射。

既是单射又是满射的映射称为双射（一一对应）。

---

# 极限的保号性

tags: 极限, 基础

hint:
若极限为正，则函数值在附近也为正。

content:
若 lim f(x) = A 且 A > 0，则在充分小的邻域内 f(x) > 0。

---

# 罗尔定理

tags: 中值定理

hint:
验证连续性、可导性以及端点函数值相等。

content:
若 f 在 [a,b] 上连续，在 (a,b) 内可导，且 f(a) = f(b)，则存在 c ∈ (a,b) 使得 f'(c) = 0。
        """.trimIndent()
    )

    // ─── 离散数学 (Discrete Mathematics) ───

    val discreteMath = KnowledgePreset(
        id = "builtin-离散数学",
        name = "离散数学",
        subtitle = "命题逻辑基础",
        description = "涵盖命题逻辑、真值表、逻辑联结词、范式、推理规则等离散数学核心概念",
        category = "数学",
        isBuiltIn = true,
        markdownText = """
# 命题与真值

tags: 1.1 Propositional Logic

hint:
什么样的句子才是命题？

content:
命题是能够明确判断真假的陈述句。一个命题必须满足两个条件：它是陈述性的，并且真值唯一。

命题的真值只有两种：T 表示真；F 表示假。

不能判断真假的句子、命令句、疑问句、含有未赋值变量的开放句，通常都不是命题。

---

# 命题变量与复合命题

tags: 1.1 Propositional Logic

hint:
简单命题怎样组成更复杂的逻辑表达式？

content:
命题变量用来代表命题，常用 p, q, r, s, ... 表示。

不能再由更简单命题分解得到的命题称为原子命题。

由已有命题通过逻辑联结词构造出的新命题称为复合命题。常见逻辑联结词包括否定、合取、析取、异或、蕴含和双条件。

---

# 否定

tags: 1.1 Propositional Logic

hint:
否定命题的真值如何变化？

content:
命题 p 的否定记为 ¬p，读作"非 p"。

否定会把命题的真值反转：
p = T ⇒ ¬p = F
p = F ⇒ ¬p = T

---

# 合取

tags: 1.1 Propositional Logic

hint:
合取命题何时为真？

content:
命题 p 与 q 的合取记为 p ∧ q，读作"p 且 q"。

合取命题仅在 p 和 q 同时为真时才为真，否则为假。

真值表：T ∧ T = T，其余组合均为 F。

---

# 析取

tags: 1.1 Propositional Logic

hint:
析取命题何时为假？

content:
命题 p 与 q 的析取记为 p ∨ q，读作"p 或 q"。

析取命题仅在 p 和 q 同时为假时才为假，否则为真。

真值表：F ∨ F = F，其余组合均为 T。

---

# 异或

tags: 1.1 Propositional Logic

hint:
异或与普通析取有何不同？

content:
命题 p 与 q 的异或记为 p ⊕ q，读作"p 异或 q"。

异或命题在 p 和 q 真值不同时为真，真值相同时为假。

真值表：T ⊕ F = T，F ⊕ T = T，T ⊕ T = F，F ⊕ F = F。

---

# 蕴含

tags: 1.1 Propositional Logic

hint:
蕴含命题何时为假？

content:
命题 p 蕴含 q 记为 p → q，读作"若 p 则 q"。

蕴含命题仅在 p 为真且 q 为假时才为假，其他情况均为真。

真值表：T → F = F，其余组合均为 T。
        """.trimIndent()
    )

    // ─── Collection ───

    /** All built-in presets in display order. */
    val all: List<KnowledgePreset> = listOf(
        calculus,            // 微积分
        discreteMath,        // 离散数学
        universityPhysics,   // 大学物理
        collegeEnglishBand4  // 大学英语Band4
    )

    /** Default preset (first in the list). */
    val defaultPreset: KnowledgePreset
        get() = all.first()
}
