import { KnowledgePreset } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
export const DEFAULT_PRESET_ID = 'builtin-微积分';
// Embedded markdown content for each preset (must be before builtInPresets for ArkTS)
const BUILTIN_MARKDOWN_微积分 = `# 集合与元素

tags: 第一讲 预备知识

hint:
集合、元素以及属于关系怎样表示？

content:
集合是指由一些可以确定、可以分辨的事物构成的整体。通常用大写字母表示集合，例如 A,B,C,...,X,Y,Z。

组成集合的成员称为集合的元素，通常用小写字母表示，例如 a,b,c,...,x,y,z。

若 a 是集合 A 的一个元素，记为 a∈A。若 a 不是集合 A 的元素，记为 a∉A。

---

# 子集、相等与真子集

tags: 第一讲 预备知识

hint:
子集、相等、真子集的定义以及如何证明？

content:
设 A,B 是两个集合。若对任意 x∈A，均有 x∈B，则称 A 是 B 的子集，记作 A⊆B。

若 A⊆B 且 B⊆A，则称 A 与 B 相等，记作 A=B。

若 A⊆B 且 A≠B，则称 A 是 B 的真子集。

---

# 集合的运算

tags: 第一讲 预备知识

hint:
并集、交集、差集、补集的定义。

content:
并集 A∪B = {x | x∈A 或 x∈B}
交集 A∩B = {x | x∈A 且 x∈B}
差集 A\\B = {x | x∈A 且 x∉B}
补集 Ac = {x | x∉A}

---

# 函数的定义

tags: 第一讲 预备知识

hint:
函数、定义域、值域的定义。

content:
设 D 是一个非空实数集合，若存在一个对应法则 f，使得对每一个 x∈D 都有一个唯一确定的实数 y 与之对应，则称 f 是定义在 D 上的一个函数。D 称为定义域，全体函数值 {f(x)|x∈D} 称为值域。

---

# 数列极限的定义

tags: 第二讲 极限

hint:
ε-N 语言表达的数列极限定义。

content:
设 {an} 是一个数列，A 是一个常数。若对任意给定的 ε>0，总存在正整数 N，使得当 n>N 时，总有 |an - A| < ε，则称数列 {an} 以 A 为极限，记作 lim(n→∞) an = A。

---

# 函数极限的定义

tags: 第二讲 极限

hint:
ε-δ 语言表达的函数极限定义。

content:
设函数 f(x) 在点 a 的某个去心邻域内有定义，A 是一个常数。若对任意给定的 ε>0，总存在 δ>0，使得当 0 < |x-a| < δ 时，总有 |f(x)-A| < ε，则称当 x 趋于 a 时 f(x) 以 A 为极限，记作 lim(x→a) f(x) = A。

---

# 无穷小与无穷大

tags: 第二讲 极限

hint:
无穷小、无穷大的定义和关系。

content:
若 lim f(x) = 0，则称 f(x) 是当 x→a（或 x→∞）时的无穷小。若对任意 M>0，存在 δ>0，使得当 0<|x-a|<δ 时 |f(x)|>M，则称 f(x) 是无穷大。在同一极限过程中，若 f(x) 是无穷大，则 1/f(x) 是无穷小。`;
const BUILTIN_MARKDOWN_大学英语Band4 = `# Abandon

tags: Band4, Vocabulary

hint:
Verb meaning to give up completely, desert, or yield to emotion.

content:
abandon (v.) - to give up completely; to desert or forsake; to yield (oneself) to emotion.

Example: The crew abandoned the sinking ship.

---

# Abstract

tags: Band4, Vocabulary

hint:
Adjective meaning existing in thought rather than matter; not concrete.

content:
abstract (adj.) - existing in thought or as an idea but not having a physical or concrete existence.

Example: Beauty is an abstract concept.

---

# Accommodate

tags: Band4, Vocabulary

hint:
Verb meaning to provide lodging, adapt to, or make fit.

content:
accommodate (v.) - to provide lodging or room for; to adjust or adapt to.

Example: The hotel can accommodate 200 guests.

---

# Accumulate

tags: Band4, Vocabulary

hint:
Verb meaning to gather or pile up gradually.

content:
accumulate (v.) - to gather or pile up little by little; to increase gradually.

Example: Dust accumulates on furniture.

---

# Accurate

tags: Band4, Vocabulary

hint:
Adjective meaning free from error; precise and exact.

content:
accurate (adj.) - free from error or defect; precise; exact.

Example: The witness gave an accurate description.

---

# Acknowledge

tags: Band4, Vocabulary

hint:
Verb meaning to admit the existence or truth of; to express recognition.

content:
acknowledge (v.) - to admit to be real or true; to recognize the existence of.

Example: He acknowledged his mistake.

---

# Acquire

tags: Band4, Vocabulary

hint:
Verb meaning to gain possession of; to obtain by one's own efforts.

content:
acquire (v.) - to gain possession of; to get as one's own.

Example: She acquired a good knowledge of English.`;
const BUILTIN_MARKDOWN_大学物理 = `# 质点运动学

tags: 力学, 运动学

hint:
位置矢量、位移、速度、加速度的基本定义。

content:
位置矢量 r = xi + yj + zk。位移 Δr = r(t+Δt) - r(t)。速度 v = dr/dt。加速度 a = dv/dt。

---

# 牛顿第一定律

tags: 力学, 牛顿定律

hint:
物体在不受外力时将保持静止或匀速直线运动状态。

content:
任何物体都将保持其静止或匀速直线运动状态，直到外力迫使它改变这种状态为止。质量是物体惯性的量度。

---

# 牛顿第二定律

tags: 力学, 牛顿定律

hint:
物体的加速度与合外力成正比，与质量成反比。

content:
F = ma，其中 F 是合外力，m 是质量，a 是加速度。这是经典力学的基本方程。动量形式：F = dp/dt。

---

# 牛顿第三定律

tags: 力学, 牛顿定律

hint:
作用力与反作用力总是成对出现，大小相等方向相反。

content:
两个物体之间的作用力与反作用力总是大小相等、方向相反、作用在同一条直线上。注意它们作用在不同的物体上。

---

# 功和动能

tags: 力学, 能量

hint:
功是力在位移方向上的分量与位移的乘积。

content:
W = ∫F·dr，功是力对位移的积分。动能 Ek = ½mv²。动能定理：合外力做的功等于动能增量。

---

# 保守力和势能

tags: 力学, 能量

hint:
保守力做的功与路径无关，可以定义势能。

content:
保守力做功与路径无关。势能 Ep 定义为保守力做功的负值。重力势能：Ep = mgh。弹性势能：Ep = ½kx²。

---

# 机械能守恒定律

tags: 力学, 能量

hint:
只有保守力做功时，机械能守恒。

content:
当只有保守力做功时，系统的机械能（动能+势能）保持不变：E = Ek + Ep = 常量。`;
const BUILTIN_MARKDOWN_离散数学 = `# 命题与联结词

tags: 数理逻辑, 基础

hint:
命题的定义以及五种基本联结词。

content:
命题：能判断真假的陈述句。五种基本联结词：¬ 否，∧ 与，∨ 或，→ 蕴含，↔ 等价。

---

# 真值表

tags: 数理逻辑, 基础

hint:
用真值表判断公式的类型。

content:
真值表列出命题公式在所有可能真值指派下的取值。永真式（重言式）：所有指派均为真。永假式（矛盾式）：所有指派均为假。可满足式：至少存在一个成真指派。

---

# 等价与蕴含

tags: 数理逻辑, 推理

hint:
逻辑等价的定义和常用等价公式。

content:
若 A↔B 是永真式，则 A 与 B 逻辑等价。常用：¬¬P ⇔ P（双重否定律），¬(P∧Q) ⇔ ¬P∨¬Q（德摩根律），P→Q ⇔ ¬P∨Q。

---

# 量词

tags: 数理逻辑, 谓词逻辑

hint:
全称量词和存在量词的定义。

content:
∀x P(x) 表示"对所有 x，P(x) 为真"。∃x P(x) 表示"存在 x 使 P(x) 为真"。量词否定：¬∀x P(x) ⇔ ∃x ¬P(x)，¬∃x P(x) ⇔ ∀x ¬P(x)。

---

# 集合的基本概念

tags: 集合论, 基础

hint:
集合的表示法和基本关系。

content:
列举法：A = {1, 2, 3}。谓词法：A = {x | P(x)}。基数 |A| 是元素个数。幂集 P(A) 是所有子集的集合。

---

# 关系的定义

tags: 关系, 基础

hint:
二元关系的定义和表示法。

content:
A×B 的子集称为从 A 到 B 的二元关系。表示法：集合表示 R = {(a,b)}，矩阵表示和图表式。

---

# 关系的性质

tags: 关系, 基础

hint:
自反性、对称性、反对称性、传递性。

content:
自反性：∀x，xRx。对称性：若 xRy 则 yRx。反对称性：若 xRy 且 yRx 则 x=y。传递性：若 xRy 且 yRz 则 xRz。`;
export const builtInPresets: KnowledgePreset[] = [
    new KnowledgePreset('builtin-微积分', '微积分', '微积分知识点', '由内置 Markdown 文件「Presets/微积分.md」提供的知识点预设。', '内置预设', BUILTIN_MARKDOWN_微积分, true),
    new KnowledgePreset('builtin-大学英语Band4', '大学英语Band4', '大学英语Band4知识点', '由内置 Markdown 文件「Presets/大学英语Band4.md」提供的知识点预设。', '内置预设', BUILTIN_MARKDOWN_大学英语Band4, true),
    new KnowledgePreset('builtin-大学物理', '大学物理', '大学物理知识点', '由内置 Markdown 文件「Presets/大学物理.md」提供的知识点预设。', '内置预设', BUILTIN_MARKDOWN_大学物理, true),
    new KnowledgePreset('builtin-离散数学', '离散数学', '离散数学知识点', '由内置 Markdown 文件「Presets/离散数学.md」提供的知识点预设。', '内置预设', BUILTIN_MARKDOWN_离散数学, true)
];
