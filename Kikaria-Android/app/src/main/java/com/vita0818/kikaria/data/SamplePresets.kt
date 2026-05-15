package com.vita0818.kikaria.data

/**
 * Built-in sample presets for first launch, translated from the iOS Presets/ directory.
 * Now includes 5 presets: advanced-math, college-english, college-physics, calculus, discrete-math.
 */
object SamplePresets {

    val advancedMath = KnowledgePreset(
        id = "advanced-math",
        name = "高等数学知识点",
        subtitle = "微积分与线性代数核心概念",
        description = "涵盖极限、导数、中值定理、矩阵等核心知识点",
        category = "数学",
        isBuiltIn = true,
        markdownText = """
# 极限的保号性
tags: 微积分, 极限, 基础
hint: 若极限为正，则函数值在附近也为正。
content: 若 lim f(x) = A 且 A > 0，则在充分小的邻域内 f(x) > 0。
---
# 罗尔定理
tags: 微积分, 中值定理
hint: 验证连续性、可导性以及端点函数值相等。
content: 若 f 在 [a,b] 上连续，在 (a,b) 内可导，且 f(a)=f(b)，则存在 c 使 f'(c)=0。
---
# 导数乘法法则
tags: 微积分, 导数, 基础
hint: 每次对一个因子求导，然后将两项相加。
content: 对于可导函数 f 和 g，(fg)' = f'g + fg'。
---
# 矩阵乘法维度
tags: 线性代数, 矩阵, 基础
hint: 内部维度必须匹配。
content: m×n 矩阵可与 n×p 矩阵相乘，结果为 m×p 矩阵。
---
# 线性无关
tags: 线性代数, 向量空间
hint: 只有平凡的系数组合才能得到零向量。
content: 向量 v1 到 vn 线性无关，若 c1v1+...+cnvn=0 蕴含 c1=...=cn=0。
---
# 贝叶斯定理
tags: 概率论, 条件概率
hint: 通过先验和证据来反转条件概率。
content: P(A|B) = P(B|A)P(A) / P(B)，假设 P(B) 不为零。
        """.trimIndent()
    )

    val collegeEnglish = KnowledgePreset(
        id = "college-english",
        name = "大学英语 Band 4",
        subtitle = "核心词汇与短语",
        description = "大学英语四级核心词汇，Unit 1-2，含英文释义、中文翻译",
        category = "英语",
        isBuiltIn = true,
        markdownText = """
# commencement
tags: Unit 1, new words
hint: The high school commencement speaker was giving a rather inspiring address.
content: 1. A ceremony at which students receive diplomas. 毕业典礼。2. The beginning of something. 开始；开端。
---
# faculty
tags: Unit 1, new words
hint: A drop in enrollment will affect students, faculty, and administrators.
content: 1. All the teachers in a university. 全体教员。2. A department within a university. 系；部；院。
---
# confront
tags: Unit 1, new words
hint: You must confront your fears and doubts and take risks again and again.
content: Deal with a problem or difficult situation. 处理；解决。
---
# mediocre
tags: Unit 1, new words
hint: Tourists crowd the gift shops to buy mediocre products at high prices.
content: Not very good. 不太好的。
---
# deadline
tags: Unit 1, new words
hint: Without extra help, it's going to be very difficult for us to meet the Friday deadline.
content: A date or time by which you must complete something. 截止时间；最后期限。
---
# spoil
tags: Unit 1, new words
hint: There was no need to rush into a conversation that might spoil everything.
content: Have a bad effect on something so it is no longer enjoyable. 毁掉；破坏。
---
# adequate
tags: Unit 1, new words
hint: The lunchtime menu is more than adequate to satisfy the biggest appetite.
content: Enough in quantity or good enough in quality. 足够的；充分的。
---
# tolerate
tags: Unit 1, new words
hint: The government is not prepared to tolerate the situation any longer.
content: 1. Allow without criticizing. 容忍。2. Accept something unpleasant. 忍受。
---
# barrier
tags: Unit 2, new words
hint: Lack of confidence is a psychological barrier to success.
content: A problem or rule that prevents progress. 障碍；阻碍。
---
# norm
tags: Unit 2, new words
hint: Short-term contracts are now the norm with some big companies.
content: The usual or normal situation. 常态；规范。
---
# drop out
tags: Unit 1, phrases
hint: Bill dropped out of college after his first year.
content: Leave school before your course has finished. 退学；辍学。
---
# get away with
tags: Unit 1, phrases
hint: He's been getting away with his bad behavior for too long.
content: Not be caught or punished for doing something wrong. 做错事不被发现。
---
# let alone
tags: Unit 2, phrases
hint: He cannot walk anymore, let alone play golf.
content: Used after a negative to say the next thing is even more unlikely. 更不用说。
        """.trimIndent()
    )

    val collegePhysics = KnowledgePreset(
        id = "college-physics",
        name = "大学物理知识点",
        subtitle = "力学、热学、电磁学与近代物理",
        description = "涵盖质点运动学、牛顿定律、功和能、刚体力学、振动与波、相对论、热力学、电磁学、光学",
        category = "物理",
        isBuiltIn = true,
        markdownText = """
# 力学研究对象
tags: 质点运动学
hint: 力学研究什么？
content: 力学研究的对象是机械运动。运动学只描述运动，不研究原因。
---
# 质点模型
tags: 质点运动学
hint: 质点忽略什么？
content: 质点是忽略形状和大小、保留质量的理想化模型。
---
# 牛顿第一定律和惯性系
tags: 牛顿运动定律
hint: 物体不受外力时如何运动？
content: 物体若不受外力或合外力为零，将保持静止或匀速直线运动状态。满足此定律的参考系称为惯性系。
---
# 动量守恒定律
tags: 牛顿运动定律
hint: 合外力为零时动量如何变化？
content: 当质点系所受合外力为零时，系统总动量守恒。
---
# 动能
tags: 功和能
hint: 动能公式是什么？
content: 动能是运动状态的函数。Ek = (1/2)mv²。
---
# 机械能守恒定律
tags: 功和能
hint: 保守力系统中机械能如何？
content: 仅有保守力作用的封闭系统中，机械能守恒：E = Ek + U = const。
---
# 角动量守恒定律
tags: 角动量
hint: 合外力矩为零时角动量如何？
content: 若系统所受合外力矩为零，则角动量守恒。
---
# 转动惯量
tags: 刚体力学
hint: 转动惯量由什么决定？
content: I = Σ Δmi·ri²。反映刚体转动惯性，与质量和分布有关。
---
# 简谐振动
tags: 机械振动
hint: 简谐振动表达式？
content: x = A cos(ωt + φ)。A 振幅，ω 角频率，φ 初相位。
---
# 洛伦兹变换
tags: 狭义相对论
hint: 光速不变导致什么？
content: x' = γ(x−ut)，t' = γ(t−ux/c²)，γ = 1/√(1−u²/c²)。
---
# 理想气体状态方程
tags: 气体动理论
hint: pV 与温度的关系？
content: pV = νRT = nkT。
---
# 热力学第一定律
tags: 热力学基础
hint: 系统吸热等于什么？
content: dQ = dE + dW。吸收热量 = 内能增量 + 对外做功。
---
# 卡诺循环
tags: 热力学基础
hint: 卡诺效率只与什么有关？
content: η = 1 − T₂/T₁。只与高低温热源温度有关。
---
# 高斯定理
tags: 静电场
hint: 闭合曲面电通量由什么决定？
content: 通过闭合曲面的电通量等于曲面内包围电荷量除以 ε₀。
---
# 毕奥-萨伐尔定律
tags: 稳恒磁场
hint: 电流元产生的磁场？
content: dB = (μ₀/4π)(Idl × er)/r²。
---
# 法拉第电磁感应定律
tags: 电磁感应
hint: 感应电动势公式？
content: ε = −dΦm/dt。
---
# 麦克斯韦方程组
tags: 麦克斯韦方程
hint: 四个方程统一描述什么？
content: 统一描述电磁场规律，包括高斯定理、磁高斯定理、法拉第定律、安培环路定理。
---
# 杨氏双缝实验
tags: 光的干涉
hint: 条纹形状？
content: 明暗相间的等距直条纹。
        """.trimIndent()
    )

    val calculus = KnowledgePreset(
        id = "calculus",
        name = "微积分知识点",
        subtitle = "极限、导数、积分与级数",
        description = "涵盖集合论预备知识、数列与函数极限、连续性与导数、微分中值定理、不定积分与定积分、广义积分",
        category = "数学",
        isBuiltIn = true,
        markdownText = """
# 集合与元素
tags: 预备知识
hint: 集合与元素的属于关系？
content: 若 a 是集合 A 的元素，记为 a ∈ A。常用数集：N,Z,Q,R。
---
# 区间与邻域
tags: 预备知识
hint: 开区间、闭区间、邻域如何表示？
content: (a,b) 开区间，[a,b] 闭区间。U(a,ε)=(a−ε,a+ε) 为 a 的 ε 邻域。
---
# 函数的有界性与单调性
tags: 预备知识
hint: 有界和单调如何定义？
content: 有界：∃M>0 使 |f(x)|≤M。单调增：x₁<x₂⇒f(x₁)≤f(x₂)。
---
# 函数的奇偶性
tags: 预备知识
hint: 偶函数和奇函数满足什么？
content: 偶：f(−x)=f(x)，关于 y 轴对称。奇：f(−x)=−f(x)，关于原点对称。
---
# 数列极限的定义
tags: 极限
hint: ε-N 定义？
content: ∀ε>0, ∃N, n>N ⇒ |aₙ−a|<ε，则 lim aₙ=a。
---
# 数列极限性质
tags: 极限
hint: 唯一性、有界性、保号性？
content: 极限唯一。收敛必有界。极限为正则充分靠后项为正。
---
# 夹逼定理
tags: 极限
hint: 夹在中间数列的极限？
content: 若 aₙ≤bₙ≤cₙ 且 lim aₙ=lim cₙ=a，则 lim bₙ=a。
---
# 单调有界定理
tags: 极限
hint: 单调数列何时收敛？
content: 单调递增有上界必收敛；单调递减有下界必收敛。
---
# 函数极限的 ε-δ 定义
tags: 极限
hint: lim(x→x₀)f(x)=A 的定义？
content: ∀ε>0, ∃δ>0, 0<|x−x₀|<δ ⇒ |f(x)−A|<ε。
---
# 两个重要极限
tags: 极限
hint: 最重要的基本极限？
content: lim(x→0) sinx/x = 1。lim(x→∞) (1+1/x)ˣ = e。
---
# 连续与间断
tags: 连续性与导数
hint: 连续的条件？间断点分类？
content: lim f(x)=f(x₀)⇒连续。第一类：左右极限存在。可去：极限存在但不等于函数值。跳跃：左右极限不等。
---
# 闭区间连续函数性质
tags: 连续性与导数
hint: 有哪些定理？
content: 有界性、最值定理、介值定理、零点定理（f(a)f(b)<0⇒∃ξ使f(ξ)=0）。
---
# 导数的定义
tags: 连续性与导数
hint: 导数如何定义？
content: f'(x₀)=lim [f(x₀+Δx)−f(x₀)]/Δx。可导⇒连续。
---
# 求导法则
tags: 连续性与导数
hint: 和、积、商的导数？
content: (f±g)'=f'±g'。(fg)'=f'g+fg'。(f/g)'=(f'g−fg')/g²。链式法则：dy/dx=f'(u)g'(x)。
---
# 基本导数公式
tags: 连续性与导数
hint: sinx, cosx, eˣ, lnx 的导数？
content: (sinx)'=cosx。(cosx)'=−sinx。(eˣ)'=eˣ。(lnx)'=1/x。(xᵅ)'=αxᵅ⁻¹。
---
# 中值定理
tags: 连续性与导数
hint: 罗尔、拉格朗日定理？
content: 罗尔：f(a)=f(b)⇒∃ξ使f'(ξ)=0。拉格朗日：∃ξ使f'(ξ)=[f(b)−f(a)]/(b−a)。
---
# 泰勒公式
tags: 连续性与导数
hint: 多项式逼近函数？
content: f(x)=f(x₀)+f'(x₀)(x−x₀)+...+f⁽ⁿ⁾(x₀)(x−x₀)ⁿ/n!+Rₙ。
---
# 极值判别
tags: 连续性与导数
hint: 驻点是否为极值？
content: f'(x₀)=0且f''(x₀)>0⇒极小；f''(x₀)<0⇒极大。
---
# 不定积分
tags: 不定积分
hint: 原函数与不定积分？
content: ∫f(x)dx=F(x)+C。基本公式：∫xᵅdx=xᵅ⁺¹/(α+1)+C，∫dx/x=ln|x|+C。
---
# 换元与分部积分
tags: 不定积分
hint: 两种积分方法？
content: 换元：∫g(φ(x))φ'(x)dx=∫g(u)du。分部：∫udv=uv−∫vdu。
---
# 定积分
tags: 定积分
hint: 定积分定义与计算？
content: ∫ₐᵇf(x)dx=F(b)−F(a)（牛-莱公式）。连续、有限间断有界、单调函数可积。
---
# 广义积分
tags: 定积分
hint: 无穷区间积分？
content: ∫ₐ^∞f(x)dx=lim(b→∞)∫ₐᵇf(x)dx。极限存在则收敛。
---
# 定积分应用
tags: 定积分
hint: 面积和弧长？
content: 面积：S=∫ₐᵇ[y₂(x)−y₁(x)]dx。弧长：s=∫√[1+(y')²]dx。
        """.trimIndent()
    )

    val discreteMath = KnowledgePreset(
        id = "discrete-math",
        name = "离散数学知识点",
        subtitle = "命题逻辑、谓词、集合、函数与证明",
        description = "涵盖命题逻辑基本联结词与等价、谓词与量词、集合运算、函数与关系、证明方法、序列与求和",
        category = "数学",
        isBuiltIn = true,
        markdownText = """
# 命题与真值
tags: 命题逻辑
hint: 什么样的句子才是命题？
content: 命题是能够明确判断真假的陈述句。真值只有 T（真）和 F（假）。
---
# 逻辑联结词
tags: 命题逻辑
hint: ¬, ∧, ∨, ⊕ 分别是什么意思？
content: ¬p：非 p。p∧q：且。p∨q：或（包含式）。p⊕q：异或（恰好一真）。
---
# 条件语句与双条件
tags: 命题逻辑
hint: p→q 和 p↔q 何时为真？
content: p→q 仅在 p 真 q 假时为假。p↔q 同真同假时为真。p→q≡¬q→¬p（逆否等价）。
---
# 重言式与逻辑等价
tags: 命题逻辑
hint: 重言式、矛盾式、逻辑等价？
content: 重言式：永真（p∨¬p）。矛盾式：永假（p∧¬p）。p≡q 表示在所有赋值下同真值。
---
# 德摩根律
tags: 命题逻辑
hint: 否定合取和析取？
content: ¬(p∧q)≡¬p∨¬q。¬(p∨q)≡¬p∧¬q。
---
# 谓词与量词
tags: 谓词逻辑
hint: ∀ 和 ∃ 表示什么？
content: ∀xP(x)：所有 x 满足 P。∃xP(x)：存在 x 满足 P。¬∀≡∃¬，¬∃≡∀¬。
---
# 嵌套量词
tags: 谓词逻辑
hint: ∀x∃y 和 ∃y∀x 一样吗？
content: 不一样。∀x∃y：每个 x 有自己的 y。∃y∀x：同一 y 对所有 x 成立。顺序一般不可交换。
---
# 推理规则
tags: 证明方法
hint: 肯定前件、否定后件、假言三段论？
content: 肯定前件：p, p→q ⇒ q。否定后件：¬q, p→q ⇒ ¬p。假言三段论：p→q, q→r ⇒ p→r。
---
# 集合基本概念
tags: 集合论
hint: 集合、子集、幂集？
content: 集合是无序的不同对象整体。A⊆B：A 中元素都在 B 中。P(A) 为幂集，|P(A)|=2^|A|。
---
# 集合运算
tags: 集合论
hint: 并、交、差、补？
content: A∪B（并）、A∩B（交）、A−B（差）、Ā（补）。德摩根：Ā∪B̄=Ā∩B̄。
---
# 函数
tags: 函数
hint: 单射、满射、双射？
content: f:A→B。单射：a≠b⇒f(a)≠f(b)。满射：f(A)=B。双射：既是单射又是满射。
---
# 序列与求和
tags: 序列
hint: 等差、等比求和？
content: 等差：aₙ=a+nd。等比：aₙ=arⁿ。Σj=n(n+1)/2。等比和：a(rⁿ⁺¹−1)/(r−1)。
---
# 矩阵
tags: 矩阵
hint: 矩阵乘法条件？
content: m×k 乘 k×n 得 m×n。(AB)ᵢⱼ=Σaᵢₗbₗⱼ。乘法不交换。
---
# 证明方法
tags: 证明方法
hint: 直接证明、反证法、逆否证明？
content: 直接：假设 p 推出 q。反证：假设 ¬p 推出矛盾。逆否：证明 ¬q→¬p。
---
# 集合基数
tags: 集合论
hint: 可数与不可数？
content: N,Z,Q 可数。R 不可数（康托对角线法）。|A|≤|B| 表示存在 A→B 的单射。
        """.trimIndent()
    )

    val all: List<KnowledgePreset> = listOf(advancedMath, collegeEnglish, collegePhysics, calculus, discreteMath)

    val defaultPreset: KnowledgePreset
        get() = all.first { it.id == KnowledgePreset.DEFAULT_PRESET_ID }
}
