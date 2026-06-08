# 命题与真值

tags: 1.1 Propositional Logic

hint:
什么样的句子才是命题？

content:
命题是能够明确判断真假的陈述句。一个命题必须满足两个条件：它是陈述性的，并且真值唯一。

命题的真值只有两种：

$$
T
$$

表示真；

$$
F
$$

表示假。

不能判断真假的句子、命令句、疑问句、含有未赋值变量的开放句，通常都不是命题。

---

# 命题变量与复合命题

tags: 1.1 Propositional Logic

hint:
简单命题怎样组成更复杂的逻辑表达式？

content:
命题变量用来代表命题，常用：

$$
p,q,r,s,\ldots
$$

表示。

不能再由更简单命题分解得到的命题称为原子命题。

由已有命题通过逻辑联结词构造出的新命题称为复合命题。常见逻辑联结词包括否定、合取、析取、异或、蕴含和双条件。

---

# 否定

tags: 1.1 Propositional Logic

hint:
否定命题的真值如何变化？

content:
命题 $p$ 的否定记为：

$$
\neg p
$$

读作“非 $p$”。

否定会把命题的真值反转：

$$
p=T \Rightarrow \neg p=F
$$

$$
p=F \Rightarrow \neg p=T
$$

真值表为：

| $p$ | $\neg p$ |
|---|---|
| T | F |
| F | T |

---

# 合取

tags: 1.1 Propositional Logic

hint:
“并且”什么时候为真？

content:
命题 $p$ 与 $q$ 的合取记为：

$$
p\land q
$$

读作“$p$ 且 $q$”。

只有当 $p$ 与 $q$ 都为真时，$p\land q$ 才为真；只要其中至少一个为假，合取就为假。

真值表为：

| $p$ | $q$ | $p\land q$ |
|---|---|---|
| T | T | T |
| T | F | F |
| F | T | F |
| F | F | F |

---

# 析取

tags: 1.1 Propositional Logic

hint:
逻辑中的“或”默认是包含式还是排斥式？

content:
命题 $p$ 与 $q$ 的析取记为：

$$
p\lor q
$$

读作“$p$ 或 $q$”。

在命题逻辑中，$\lor$ 通常表示包含式“或”：只要 $p$ 与 $q$ 中至少一个为真，$p\lor q$ 就为真；只有两者都为假时才为假。

真值表为：

| $p$ | $q$ | $p\lor q$ |
|---|---|---|
| T | T | T |
| T | F | T |
| F | T | T |
| F | F | F |

---

# 异或

tags: 1.1 Propositional Logic

hint:
“二者恰好一个为真”怎样表示？

content:
命题 $p$ 与 $q$ 的异或记为：

$$
p\oplus q
$$

也可写作 $p$ XOR $q$。

当且仅当 $p$ 与 $q$ 中恰好一个为真时，$p\oplus q$ 为真；若两者同真或同假，则为假。

真值表为：

| $p$ | $q$ | $p\oplus q$ |
|---|---|---|
| T | T | F |
| T | F | T |
| F | T | T |
| F | F | F |

---

# 条件语句

tags: 1.1 Propositional Logic

hint:
“如果 $p$，那么 $q$”什么时候为假？

content:
条件语句记为：

$$
p\to q
$$

读作“若 $p$，则 $q$”。

其中 $p$ 称为假设、前件或前提，$q$ 称为结论或后件。

条件语句只有在 $p$ 为真而 $q$ 为假时为假，其余情况都为真。

真值表为：

| $p$ | $q$ | $p\to q$ |
|---|---|---|
| T | T | T |
| T | F | F |
| F | T | T |
| F | F | T |

---

# 必要条件与充分条件

tags: 1.1 Propositional Logic

hint:
$p\to q$ 中，$p$ 和 $q$ 谁是充分条件，谁是必要条件？

content:
对于条件语句：

$$
p\to q
$$

可以理解为：

$p$ 是 $q$ 的充分条件，因为 $p$ 成立足以推出 $q$ 成立。

$q$ 是 $p$ 的必要条件，因为 $p$ 成立时 $q$ 必须成立。

常见等价说法包括：

$$
p\to q
$$

表示“$q$ whenever $p$”、“$p$ only if $q$”、“$q$ is necessary for $p$”、“$p$ is sufficient for $q$”。

---

# 逆命题、否命题与逆否命题

tags: 1.1 Propositional Logic

hint:
一个蕴含式最可靠的等价变形是哪一个？

content:
给定条件语句：

$$
p\to q
$$

它的逆命题为：

$$
q\to p
$$

它的否命题为：

$$
\neg p\to \neg q
$$

它的逆否命题为：

$$
\neg q\to \neg p
$$

原命题与逆否命题逻辑等价：

$$
p\to q\equiv \neg q\to \neg p
$$

逆命题和否命题彼此等价，但一般不与原命题等价。

---

# 双条件语句

tags: 1.1 Propositional Logic

hint:
“当且仅当”怎样表示？什么时候为真？

content:
双条件语句记为：

$$
p\leftrightarrow q
$$

读作“$p$ 当且仅当 $q$”。

当 $p$ 与 $q$ 真值相同，即同真或同假时，$p\leftrightarrow q$ 为真；当二者真值不同，则为假。

真值表为：

| $p$ | $q$ | $p\leftrightarrow q$ |
|---|---|---|
| T | T | T |
| T | F | F |
| F | T | F |
| F | F | T |

双条件语句等价于两个方向的蕴含同时成立：

$$
p\leftrightarrow q\equiv (p\to q)\land(q\to p)
$$

---

# 复合命题真值表

tags: 1.1 Propositional Logic

hint:
含有 $n$ 个命题变量的真值表有多少行？

content:
复合命题的真值可以通过真值表逐步计算。

如果复合命题包含 $n$ 个不同命题变量，则真值表需要：

$$
2^n
$$

行。

构造真值表时，通常先列出各命题变量的所有真值组合，再依照逻辑联结词的优先级，逐层计算子表达式和最终表达式的真值。

---

# 逻辑运算符优先级

tags: 1.1 Propositional Logic

hint:
没有括号时，逻辑运算符按什么顺序结合？

content:
常见逻辑运算符的优先级从高到低为：

$$
\neg
$$

$$
\land
$$

$$
\lor
$$

$$
\to
$$

$$
\leftrightarrow
$$

因此，$\neg p\land q$ 应理解为：

$$
(\neg p)\land q
$$

而不是：

$$
\neg(p\land q)
$$

为了避免歧义，复杂表达式中应尽量使用括号。

---

# 位与布尔变量

tags: 1.1 Propositional Logic

hint:
计算机中的 $0$ 和 $1$ 怎样对应逻辑真值？

content:
位是只可能取两个值的符号：

$$
0,\quad 1
$$

逻辑中可用位表示真值：

$$
1\leftrightarrow T
$$

$$
0\leftrightarrow F
$$

只取真或假的变量称为布尔变量。布尔变量可以用位来表示。

---

# 位串与按位运算

tags: 1.1 Propositional Logic

hint:
按位 OR、AND、XOR 怎样作用在两个位串上？

content:
位串是由若干个 $0$ 和 $1$ 组成的序列，其长度为位的个数。

对两个等长位串，可以逐位进行逻辑运算：

按位 OR 对应 $\lor$；

按位 AND 对应 $\land$；

按位 XOR 对应 $\oplus$。

若两个位串为：

$$
x_1x_2\cdots x_n
$$

和：

$$
y_1y_2\cdots y_n
$$

则按位 OR 的第 $i$ 位为：

$$
x_i\lor y_i
$$

按位 AND 的第 $i$ 位为：

$$
x_i\land y_i
$$

按位 XOR 的第 $i$ 位为：

$$
x_i\oplus y_i
$$

---

# 英语句子到命题逻辑的翻译

tags: 1.2 Applications of Propositional Logic

hint:
把自然语言翻译成逻辑表达式时，最关键的步骤是什么？

content:
将自然语言句子翻译为命题逻辑表达式时，通常先把简单陈述抽象为命题变量，再用逻辑联结词表达它们之间的关系。

常见翻译要点：

“not” 对应 $\neg$；

“and” 对应 $\land$；

包含式“or” 对应 $\lor$；

排斥式“or” 对应 $\oplus$；

“if ... then ...” 对应 $\to$；

“only if” 通常表示蕴含右侧条件；

“if and only if” 对应 $\leftrightarrow$。

翻译时必须根据语义判断“or”是包含式还是排斥式。

---

# 系统规格的一致性

tags: 1.2 Applications of Propositional Logic

hint:
一组规格什么时候是相容的？

content:
系统规格可以翻译成命题逻辑表达式。

若存在至少一种命题变量真值赋值，使所有规格表达式同时为真，则称这组规格是一致的或相容的。

若不存在任何真值赋值使所有规格同时成立，则这些规格不一致。

检查一致性的一般方法是：把每条规格写成逻辑公式，然后判断它们的合取是否可满足。

---

# 布尔搜索

tags: 1.2 Applications of Propositional Logic

hint:
搜索中的 AND、OR、NOT 分别起什么作用？

content:
布尔搜索使用逻辑联结词组合关键词。

AND 要求搜索结果同时包含多个关键词。

OR 要求搜索结果至少包含其中一个关键词。

NOT 用来排除包含某个关键词的结果。

例如，想查找同时涉及 $A$ 与 $B$ 但不涉及 $C$ 的内容，可写作：

$$
A\land B\land \neg C
$$

在具体搜索引擎中，NOT 可能被写成减号或其他符号。

---

# 逻辑谜题建模

tags: 1.2 Applications of Propositional Logic

hint:
骑士与骗子、宝箱题等逻辑谜题怎样系统处理？

content:
逻辑谜题可以用命题变量表示关键事实，再把题目陈述翻译为逻辑公式。

基本步骤：

1. 为每个可能事实设置命题变量；
2. 把每个人的陈述、规则和限制翻译成逻辑表达式；
3. 加入题目给定的全局条件；
4. 求满足所有条件的真值赋值；
5. 由满足赋值还原答案。

如果没有满足赋值，说明条件矛盾；如果有多个满足赋值，说明答案不唯一。

---

# 逻辑门

tags: 1.2 Applications of Propositional Logic

hint:
NOT、AND、OR 门分别对应什么逻辑运算？

content:
逻辑电路接收位作为输入，并输出位。基本逻辑门包括：

NOT 门：

$$
p\mapsto \neg p
$$

AND 门：

$$
(p,q)\mapsto p\land q
$$

OR 门：

$$
(p,q)\mapsto p\lor q
$$

更复杂的数字电路可以由这些基本逻辑门组合构造。

---

# 由逻辑表达式构造电路

tags: 1.2 Applications of Propositional Logic

hint:
怎样从命题公式得到组合逻辑电路？

content:
若输出由命题公式给出，可以按照公式的结构自底向上构造电路。

处理方法：

1. 每个输入命题变量对应一个输入信号；
2. 每个否定 $\neg p$ 对应一个 NOT 门；
3. 每个合取 $p\land q$ 对应一个 AND 门；
4. 每个析取 $p\lor q$ 对应一个 OR 门；
5. 子表达式的输出作为更高层逻辑门的输入。

这样可以把任意只含 $\neg,\land,\lor$ 的命题表达式实现为组合电路。

---

# 重言式、矛盾式与可能式

tags: 1.3 Propositional Equivalences

hint:
一个复合命题总真、总假或有真有假时分别叫什么？

content:
复合命题若在所有真值赋值下都为真，称为重言式。

复合命题若在所有真值赋值下都为假，称为矛盾式。

复合命题若既不是重言式，也不是矛盾式，称为可能式。

例如：

$$
p\lor\neg p
$$

是重言式；

$$
p\land\neg p
$$

是矛盾式。

---

# 逻辑等价

tags: 1.3 Propositional Equivalences

hint:
两个命题公式在什么意义下“相同”？

content:
若两个复合命题在所有命题变量真值赋值下都有相同真值，则称它们逻辑等价。

记为：

$$
p\equiv q
$$

等价地，$p$ 与 $q$ 逻辑等价当且仅当：

$$
p\leftrightarrow q
$$

是重言式。

---

# 命题逻辑基本等价律

tags: 1.3 Propositional Equivalences

hint:
命题逻辑中最常用的代换规则有哪些？

content:
常用逻辑等价包括：

恒等律：

$$
p\land T\equiv p,\qquad p\lor F\equiv p
$$

支配律：

$$
p\lor T\equiv T,\qquad p\land F\equiv F
$$

幂等律：

$$
p\lor p\equiv p,\qquad p\land p\equiv p
$$

双重否定律：

$$
\neg(\neg p)\equiv p
$$

交换律：

$$
p\lor q\equiv q\lor p,\qquad p\land q\equiv q\land p
$$

结合律：

$$
(p\lor q)\lor r\equiv p\lor(q\lor r)
$$

$$
(p\land q)\land r\equiv p\land(q\land r)
$$

---

# 分配律、吸收律与否定律

tags: 1.3 Propositional Equivalences

hint:
合取和析取怎样相互分配？吸收律如何简化表达式？

content:
分配律：

$$
p\lor(q\land r)\equiv(p\lor q)\land(p\lor r)
$$

$$
p\land(q\lor r)\equiv(p\land q)\lor(p\land r)
$$

吸收律：

$$
p\lor(p\land q)\equiv p
$$

$$
p\land(p\lor q)\equiv p
$$

否定律：

$$
p\lor\neg p\equiv T
$$

$$
p\land\neg p\equiv F
$$

---

# 德摩根律

tags: 1.3 Propositional Equivalences

hint:
否定“并且”和否定“或者”时，联结词怎样改变？

content:
德摩根律为：

$$
\neg(p\land q)\equiv \neg p\lor \neg q
$$

$$
\neg(p\lor q)\equiv \neg p\land \neg q
$$

对于多个命题，也有：

$$
\neg(p_1\lor p_2\lor\cdots\lor p_n)
\equiv
\neg p_1\land\neg p_2\land\cdots\land\neg p_n
$$

$$
\neg(p_1\land p_2\land\cdots\land p_n)
\equiv
\neg p_1\lor\neg p_2\lor\cdots\lor\neg p_n
$$

---

# 条件语句的逻辑等价

tags: 1.3 Propositional Equivalences

hint:
蕴含式怎样改写成析取式？

content:
条件语句可以改写为析取式：

$$
p\to q\equiv \neg p\lor q
$$

原命题与逆否命题等价：

$$
p\to q\equiv \neg q\to\neg p
$$

否定蕴含式为：

$$
\neg(p\to q)\equiv p\land\neg q
$$

常见合取形式：

$$
(p\to q)\land(p\to r)\equiv p\to(q\land r)
$$

$$
(p\to r)\land(q\to r)\equiv(p\lor q)\to r
$$

常见析取形式：

$$
(p\to q)\lor(p\to r)\equiv p\to(q\lor r)
$$

$$
(p\to r)\lor(q\to r)\equiv(p\land q)\to r
$$

---

# 双条件语句的逻辑等价

tags: 1.3 Propositional Equivalences

hint:
当且仅当可以化成哪些等价形式？

content:
双条件语句可以表示为两个方向的蕴含：

$$
p\leftrightarrow q\equiv(p\to q)\land(q\to p)
$$

也可以表示为同真或同假：

$$
p\leftrightarrow q\equiv(p\land q)\lor(\neg p\land\neg q)
$$

同时有：

$$
p\leftrightarrow q\equiv \neg p\leftrightarrow \neg q
$$

其否定可写为：

$$
\neg(p\leftrightarrow q)\equiv p\leftrightarrow\neg q
$$

---

# 可满足性

tags: 1.3 Propositional Equivalences

hint:
一个命题公式什么时候是可满足的？

content:
若存在至少一种命题变量真值赋值，使复合命题为真，则称该复合命题可满足。

若不存在任何真值赋值使复合命题为真，则称它不可满足。

不可满足命题等价于其否定为重言式。

判断可满足性可以使用真值表，也可以通过逻辑等价和推理简化命题公式。

---

# SAT 建模思想

tags: 1.3 Propositional Equivalences

hint:
现实问题如何转化为命题公式的可满足性问题？

content:
SAT 建模的核心是把问题中的选择、状态或限制转化为命题变量与逻辑约束。

一般步骤：

1. 用命题变量表示基本选择或事实；
2. 用合取把所有必须满足的约束连接起来；
3. 用析取表示“至少一个”；
4. 用互斥约束表示“至多一个”；
5. 求使整个公式为真的赋值。

如果存在满足赋值，则原问题有解；如果不存在，则原问题无解。

---

# 谓词与命题函数

tags: 1.4 Predicates and Quantifiers

hint:
带变量的断言如何变成命题？

content:
谓词用来描述对象所具有的性质或对象之间的关系。

含有变量的语句称为命题函数或谓词表达式。例如：

$$
P(x)
$$

在没有指定 $x$ 的取值或变量范围时，一般还不是命题。

当给变量赋具体值，或用量词约束变量后，命题函数可以变成具有真值的命题。

---

# 全称量词

tags: 1.4 Predicates and Quantifiers

hint:
“对所有 $x$”怎样表示？何时为真？

content:
全称量词表示某性质对论域中所有元素都成立。

记为：

$$
\forall x\,P(x)
$$

读作“对所有 $x$，$P(x)$ 成立”。

$\forall x\,P(x)$ 为真，当且仅当论域中每一个元素 $x$ 都使 $P(x)$ 为真。

如果存在一个元素使 $P(x)$ 为假，则该元素就是反例，整个全称命题为假。

---

# 存在量词

tags: 1.4 Predicates and Quantifiers

hint:
“存在某个 $x$”怎样表示？何时为真？

content:
存在量词表示论域中至少有一个元素使性质成立。

记为：

$$
\exists x\,P(x)
$$

读作“存在 $x$，使得 $P(x)$ 成立”。

$\exists x\,P(x)$ 为真，当且仅当论域中至少有一个元素 $x$ 使 $P(x)$ 为真。

如果论域中每个元素都使 $P(x)$ 为假，则存在命题为假。

---

# 论域

tags: 1.4 Predicates and Quantifiers

hint:
量词为什么必须指定范围？

content:
量词的含义依赖于论域，也就是变量允许取值的集合。

同一个表达式：

$$
\forall x\,P(x)
$$

或：

$$
\exists x\,P(x)
$$

在不同论域下可能有不同真值。

因此，使用量词时必须明确变量的论域。没有论域，量化命题的意义通常不完整。

---

# 唯一存在量词

tags: 1.4 Predicates and Quantifiers

hint:
“存在唯一一个”怎样表示？

content:
唯一存在量词表示恰好存在一个元素满足某性质，记为：

$$
\exists!x\,P(x)
$$

读作“存在唯一的 $x$ 使 $P(x)$ 成立”。

也可用普通量词表达为：

$$
\exists x\left(P(x)\land \forall y(P(y)\to y=x)\right)
$$

这表示：至少存在一个满足 $P$ 的元素，并且所有满足 $P$ 的元素都等于这个元素。

---

# 有限论域上的量词

tags: 1.4 Predicates and Quantifiers

hint:
有限集合上的全称量词和存在量词可以转化成什么？

content:
若论域为有限集合：

$$
\{x_1,x_2,\ldots,x_n\}
$$

则全称量词等价于有限合取：

$$
\forall x\,P(x)\equiv P(x_1)\land P(x_2)\land\cdots\land P(x_n)
$$

存在量词等价于有限析取：

$$
\exists x\,P(x)\equiv P(x_1)\lor P(x_2)\lor\cdots\lor P(x_n)
$$

---

# 限制量词

tags: 1.4 Predicates and Quantifiers

hint:
“所有满足条件的 $x$”和“存在满足条件的 $x$”怎样翻译？

content:
限制全称量词通常写成“对所有满足 $P(x)$ 的 $x$，有 $Q(x)$”，逻辑形式为：

$$
\forall x(P(x)\to Q(x))
$$

限制存在量词通常写成“存在满足 $P(x)$ 的 $x$，并且 $Q(x)$”，逻辑形式为：

$$
\exists x(P(x)\land Q(x))
$$

注意：限制全称量词常用蕴含，限制存在量词常用合取。

---

# 量词优先级、绑定变量与自由变量

tags: 1.4 Predicates and Quantifiers

hint:
量词作用到哪一部分？变量是否已经被约束？

content:
量词 $\forall$ 与 $\exists$ 的优先级高于命题逻辑联结词。

量词作用的表达式范围称为该量词的辖域。

若变量出现在某个以该变量为对象的量词辖域中，则称该变量被绑定。

若变量没有被量词绑定，也没有被赋具体值，则称该变量是自由变量。

一个含自由变量的表达式通常不是命题；要成为命题，需要给自由变量赋值或用量词绑定它。

---

# 含量词表达式的逻辑等价

tags: 1.4 Predicates and Quantifiers

hint:
量词能否分配到合取或析取上？

content:
全称量词可以分配到合取上：

$$
\forall x(P(x)\land Q(x))
\equiv
\forall xP(x)\land \forall xQ(x)
$$

存在量词可以分配到析取上：

$$
\exists x(P(x)\lor Q(x))
\equiv
\exists xP(x)\lor \exists xQ(x)
$$

但一般不能把全称量词分配到析取上：

$$
\forall x(P(x)\lor Q(x))
\not\equiv
\forall xP(x)\lor \forall xQ(x)
$$

也一般不能把存在量词分配到合取上：

$$
\exists x(P(x)\land Q(x))
\not\equiv
\exists xP(x)\land \exists xQ(x)
$$

---

# 量词的德摩根律

tags: 1.4 Predicates and Quantifiers

hint:
否定全称命题和否定存在命题时，量词如何改变？

content:
量词的否定规则为：

$$
\neg\forall x\,P(x)\equiv \exists x\,\neg P(x)
$$

$$
\neg\exists x\,P(x)\equiv \forall x\,\neg P(x)
$$

也就是说：

否定“所有都成立”，得到“至少有一个不成立”。

否定“存在一个成立”，得到“所有都不成立”。

这些规则也称为量词的德摩根律。

---

# 嵌套量词

tags: 1.5 Nested Quantifiers

hint:
多个量词连续出现时，怎样逐层理解？

content:
含有多个量词的表达式称为嵌套量词表达式。

例如：

$$
\forall x\exists y\,P(x,y)
$$

表示：对每个 $x$，都存在某个可能依赖于 $x$ 的 $y$，使 $P(x,y)$ 成立。

理解嵌套量词时，应从左到右逐层解释，每个量词控制其辖域中的变量。

---

# 量词顺序的重要性

tags: 1.5 Nested Quantifiers

hint:
$\forall x\exists y$ 和 $\exists y\forall x$ 通常一样吗？

content:
嵌套量词的顺序通常会影响命题含义。

表达式：

$$
\forall x\exists y\,P(x,y)
$$

表示每个 $x$ 可以有自己的 $y$。

表达式：

$$
\exists y\forall x\,P(x,y)
$$

表示存在同一个 $y$，对所有 $x$ 都适用。

一般情况下：

$$
\forall x\exists y\,P(x,y)
\not\equiv
\exists y\forall x\,P(x,y)
$$

---

# 同类量词可以交换

tags: 1.5 Nested Quantifiers

hint:
连续两个全称量词或连续两个存在量词能否交换？

content:
同类量词的顺序可以交换。

两个全称量词满足：

$$
\forall x\forall y\,P(x,y)
\equiv
\forall y\forall x\,P(x,y)
$$

两个存在量词满足：

$$
\exists x\exists y\,P(x,y)
\equiv
\exists y\exists x\,P(x,y)
$$

但不同类量词一般不能随意交换。

---

# 多变量语句的翻译原则

tags: 1.5 Nested Quantifiers

hint:
翻译含“每个”“某个”“唯一”的句子时，怎样安排量词？

content:
翻译自然语言中的多变量语句时，应先明确论域，再确定变量和谓词。

常见模式：

“每个 $x$ 都有某个 $y$”：

$$
\forall x\exists y\,P(x,y)
$$

“存在某个 $y$ 对所有 $x$ 都成立”：

$$
\exists y\forall x\,P(x,y)
$$

“每个 $x$ 恰好有一个 $y$”：

$$
\forall x\exists!y\,P(x,y)
$$

若不用唯一存在量词，可写成：

$$
\forall x\exists y\left(P(x,y)\land \forall z(P(x,z)\to z=y)\right)
$$

---

# 嵌套量词的否定

tags: 1.5 Nested Quantifiers

hint:
否定嵌套量词时，怎样把否定号推到最里面？

content:
否定嵌套量词表达式时，逐层使用量词德摩根律，将否定号向内推进。

例如：

$$
\neg\forall x\exists y\,P(x,y)
\equiv
\exists x\neg\exists y\,P(x,y)
\equiv
\exists x\forall y\,\neg P(x,y)
$$

再如：

$$
\neg\exists x\forall y\,P(x,y)
\equiv
\forall x\exists y\,\neg P(x,y)
$$

规律是：每经过一层否定，$\forall$ 与 $\exists$ 互换，直到否定作用在谓词本身。

---

# 参数化量词与变量依赖

tags: 1.5 Nested Quantifiers

hint:
在 $\forall x\exists y$ 中，$y$ 是否可以依赖于 $x$？

content:
在表达式：

$$
\forall x\exists y\,P(x,y)
$$

中，存在的 $y$ 可以依赖于前面给定的 $x$。

而在表达式：

$$
\exists y\forall x\,P(x,y)
$$

中，必须先选出一个固定的 $y$，并且这个同一个 $y$ 对所有 $x$ 都有效。

这正是不同量词顺序含义不同的根源。

---

# 论证与有效性

tags: 1.6 Rules of Inference

hint:
什么时候一个论证形式是有效的？

content:
论证由若干前提和一个结论组成。

若在所有使前提都为真的真值赋值下，结论也必然为真，则称该论证有效。

逻辑上可写为：若前提为：

$$
p_1,p_2,\ldots,p_n
$$

结论为 $q$，则论证有效当且仅当：

$$
(p_1\land p_2\land\cdots\land p_n)\to q
$$

是重言式。

---

# 命题逻辑推理规则

tags: 1.6 Rules of Inference

hint:
常见有效推理规则有哪些？

content:
常见推理规则包括：

肯定前件：

$$
\frac{p,\ p\to q}{q}
$$

否定后件：

$$
\frac{\neg q,\ p\to q}{\neg p}
$$

假言三段论：

$$
\frac{p\to q,\ q\to r}{p\to r}
$$

析取三段论：

$$
\frac{p\lor q,\ \neg p}{q}
$$

附加律：

$$
\frac{p}{p\lor q}
$$

化简律：

$$
\frac{p\land q}{p}
$$

合取律：

$$
\frac{p,\ q}{p\land q}
$$

归结律：

$$
\frac{p\lor q,\ \neg p\lor r}{q\lor r}
$$

---

# 常见推理谬误

tags: 1.6 Rules of Inference

hint:
肯定后件和否定前件为什么无效？

content:
肯定后件是无效推理：

$$
\frac{p\to q,\ q}{p}
$$

即使 $q$ 成立，也不能推出 $p$ 成立，因为 $q$ 可能由其他原因导致。

否定前件也是无效推理：

$$
\frac{p\to q,\ \neg p}{\neg q}
$$

即使 $p$ 不成立，也不能推出 $q$ 不成立。

这两种错误常源于把条件语句误认为双条件语句。

---

# 谓词逻辑推理规则

tags: 1.6 Rules of Inference

hint:
含量词的推理中，怎样从全称或存在命题得到实例？

content:
全称实例化：

$$
\frac{\forall x\,P(x)}{P(c)}
$$

其中 $c$ 是论域中的任意元素。

全称概括：

$$
\frac{P(c)\ \text{for arbitrary }c}{\forall x\,P(x)}
$$

其中 $c$ 必须是任意选取的元素，不能带有额外特殊假设。

存在实例化：

$$
\frac{\exists x\,P(x)}{P(c)\ \text{for some }c}
$$

其中 $c$ 是某个满足 $P$ 的元素，但不能任意指定。

存在概括：

$$
\frac{P(c)}{\exists x\,P(x)}
$$

只要找到一个满足 $P$ 的具体元素，就能推出存在命题。

---

# 全称肯定前件与全称否定后件

tags: 1.6 Rules of Inference

hint:
怎样把命题逻辑推理规则与全称量词结合？

content:
全称肯定前件：

$$
\forall x(P(x)\to Q(x))
$$

$$
P(a)
$$

因此：

$$
Q(a)
$$

全称否定后件：

$$
\forall x(P(x)\to Q(x))
$$

$$
\neg Q(a)
$$

因此：

$$
\neg P(a)
$$

这些规则把全称实例化与命题逻辑中的肯定前件、否定后件结合起来。

---

# 证明、定理与相关概念

tags: 1.7 Introduction to Proofs

hint:
定理、引理、推论和猜想分别是什么？

content:
证明是由公理、定义、已知定理和推理规则组成的有效论证，用来说明某个数学命题为真。

已经被证明的重要命题称为定理。

辅助证明其他结论的较小定理称为引理。

由定理直接推出的结论称为推论。

尚未被证明但被认为可能为真的命题称为猜想。

作为基础假定而不证明的命题称为公理或公设。

---

# 直接证明

tags: 1.7 Introduction to Proofs

hint:
要证明 $p\to q$，直接证明从哪里开始？

content:
直接证明用于证明条件命题：

$$
p\to q
$$

基本思路是：假设 $p$ 为真，然后通过定义、已知事实和有效推理，推出 $q$ 为真。

直接证明的结构为：

1. 假设 $p$ 成立；
2. 从 $p$ 出发进行逻辑推导；
3. 得到 $q$ 成立；
4. 因此 $p\to q$ 成立。

---

# 逆否证明

tags: 1.7 Introduction to Proofs

hint:
证明 $p\to q$ 时，什么时候可以改证 $\neg q\to\neg p$？

content:
由于：

$$
p\to q\equiv \neg q\to\neg p
$$

要证明 $p\to q$，可以改为证明其逆否命题：

$$
\neg q\to\neg p
$$

逆否证明的结构为：

1. 假设 $\neg q$ 成立；
2. 由此推出 $\neg p$；
3. 因此 $\neg q\to\neg p$ 成立；
4. 从而原命题 $p\to q$ 成立。

当从结论的否定出发更容易推出假设的否定时，逆否证明尤其有用。

---

# 空证明与平凡证明

tags: 1.7 Introduction to Proofs

hint:
在什么情况下条件命题可以不用真正推出结论？

content:
对于条件命题：

$$
p\to q
$$

若能证明 $p$ 永远为假，则 $p\to q$ 自动为真。这种证明称为空证明。

若能证明 $q$ 永远为真，则无论 $p$ 如何，$p\to q$ 都为真。这种证明称为平凡证明。

空证明依赖于蕴含式在前件为假时为真；平凡证明依赖于蕴含式在后件为真时为真。

---

# 反证法

tags: 1.7 Introduction to Proofs

hint:
反证法通过制造什么来证明命题？

content:
反证法用于证明命题 $p$ 为真时，先假设其否定为真：

$$
\neg p
$$

然后推出矛盾，例如推出：

$$
r\land\neg r
$$

由于矛盾不可能为真，因此假设 $\neg p$ 错误，故 $p$ 为真。

证明条件命题 $p\to q$ 时，也可以假设：

$$
p\land\neg q
$$

再推出矛盾，从而证明 $p\to q$。

---

# 等价命题的证明

tags: 1.7 Introduction to Proofs

hint:
证明“当且仅当”通常要证明几个方向？

content:
要证明双条件命题：

$$
p\leftrightarrow q
$$

通常需要分别证明：

$$
p\to q
$$

和：

$$
q\to p
$$

因为：

$$
p\leftrightarrow q\equiv(p\to q)\land(q\to p)
$$

若要证明多个命题：

$$
p_1,p_2,\ldots,p_n
$$

互相等价，可以证明一个蕴含环：

$$
p_1\to p_2\to\cdots\to p_n\to p_1
$$

从而说明它们真值相同。

---

# 反例

tags: 1.7 Introduction to Proofs

hint:
怎样证明一个全称命题是假的？

content:
要否定全称命题：

$$
\forall x\,P(x)
$$

只需找到一个反例，也就是某个 $a$，使得：

$$
\neg P(a)
$$

成立。

因此：

$$
\exists x\,\neg P(x)
$$

为真，从而：

$$
\forall x\,P(x)
$$

为假。

反例是推翻错误猜想和全称断言的基本工具。

---

# 穷举证明与分情形证明

tags: 1.8 Proof Methods and Strategy

hint:
当对象有限或自然分成几类时，怎样组织证明？

content:
穷举证明用于对象数量有限的情形：逐一检查所有可能情况，若每一种情况结论都成立，则命题成立。

分情形证明用于对象可以分成若干类的情形：把总体划分为覆盖全部可能且便于处理的若干情形，分别证明每个情形中结论成立。

分情形证明需要注意：

1. 情形必须覆盖所有可能；
2. 每个情形都要证明；
3. 情形不一定必须互斥，但互斥会使结构更清楚。

---

# 不失一般性

tags: 1.8 Proof Methods and Strategy

hint:
什么时候可以说“without loss of generality”？

content:
当多个情形在结构上对称，只需证明其中一个代表情形，其他情形可通过同样论证得到时，可以使用“不失一般性”。

使用不失一般性必须满足：被省略的情形与已证明情形在逻辑上完全对称，不能因为方便而忽略本质不同的情形。

---

# 存在性证明

tags: 1.8 Proof Methods and Strategy

hint:
存在命题可以怎样证明？

content:
存在性命题形如：

$$
\exists x\,P(x)
$$

证明存在性有两种常见方式。

构造性存在证明：直接给出一个具体对象 $a$，并证明：

$$
P(a)
$$

非构造性存在证明：不明确给出对象，但通过逻辑论证说明至少有一个对象满足 $P$。

构造性证明通常更强，因为它给出实际例子；非构造性证明有时更容易建立存在性。

---

# 唯一性证明

tags: 1.8 Proof Methods and Strategy

hint:
证明“存在唯一”需要证明哪些部分？

content:
唯一性命题形如：

$$
\exists!x\,P(x)
$$

通常分两步证明：

第一步，证明存在性：

$$
\exists x\,P(x)
$$

第二步，证明唯一性：若 $a$ 与 $b$ 都满足 $P$，则：

$$
a=b
$$

即：

$$
(P(a)\land P(b))\to a=b
$$

这样即可说明满足条件的对象恰好有一个。

---

# 正向推理与反向推理

tags: 1.8 Proof Methods and Strategy

hint:
找证明时，可以从已知条件出发，也可以从目标倒推。

content:
正向推理从已知条件、定义和已证明结论出发，逐步推出目标结论。

反向推理从目标结论出发，思考若要得到目标，需要先证明哪些中间结论，再继续向已知条件回溯。

在正式证明中，通常需要把反向推理发现的路线整理成正向的逻辑推导。

---

# 改编已有证明

tags: 1.8 Proof Methods and Strategy

hint:
遇到新命题时，为什么要寻找类似命题的证明？

content:
当新命题与已知命题结构相似时，可以尝试改编已有证明。

常用做法：

1. 找到已知证明中真正关键的思想；
2. 判断哪些步骤依赖于具体对象，哪些步骤可以保留；
3. 替换变量、条件或对象；
4. 检查每一步在新情形中是否仍然有效。

改编已有证明可以显著降低寻找证明的难度。

---

# 寻找反例的策略

tags: 1.8 Proof Methods and Strategy

hint:
猜想证明不出来时，如何系统寻找反例？

content:
面对一个全称猜想，若直接证明困难，可以尝试寻找反例。

常见策略：

1. 从最小、最简单的对象开始测试；
2. 检查边界情况和极端情况；
3. 寻找使条件刚好成立但结论可能失败的情况；
4. 对比已知类似结论，找出多出来或缺少的条件；
5. 若找不到反例，再尝试证明猜想。

一个反例足以否定全称命题。

---

# 证明策略总览

tags: 1.8 Proof Methods and Strategy

hint:
选择证明方法时，先看命题的逻辑结构。

content:
选择证明方法时，可根据命题形式判断：

若目标是 $p\to q$，优先考虑直接证明或逆否证明。

若目标是否定某命题，常可考虑反证法。

若目标是 $p\leftrightarrow q$，通常证明两个方向。

若目标是 $\forall x\,P(x)$，证明任取对象都满足 $P$。

若目标是 $\exists x\,P(x)$，寻找构造性或非构造性存在证明。

若目标是否定全称命题，寻找反例。

若对象分成若干类别，使用分情形证明。

若问题与已有结论相似，尝试改编已有证明。

---

# 集合

tags: 2.1 Sets

hint:
集合由哪些对象组成？顺序和重复是否重要？

content:
集合是由不同对象组成的无序 collection。集合中的对象称为元素或成员。

若 $a$ 是集合 $A$ 的元素，记为：

$$
a\in A
$$

若 $a$ 不是集合 $A$ 的元素，记为：

$$
a\notin A
$$

集合中元素的顺序不重要，重复列出同一个元素也不会改变集合。

---

# 花名册表示法与集合构造式

tags: 2.1 Sets

hint:
怎样列出集合？怎样用性质描述集合？

content:
花名册表示法直接列出集合中的元素，例如：

$$
A=\{1,2,3,4\}
$$

集合构造式通过元素满足的性质描述集合，例如：

$$
A=\{x\mid P(x)\}
$$

表示所有满足性质 $P(x)$ 的元素 $x$ 组成的集合。

若需要指定论域，可写为：

$$
A=\{x\in S\mid P(x)\}
$$

---

# 常用数集

tags: 2.1 Sets

hint:
离散数学中常见数集如何记号？

content:
常用数集记号包括：

$$
\mathbb{N}
$$

表示自然数集合。

$$
\mathbb{Z}
$$

表示整数集合。

$$
\mathbb{Z}^{+}
$$

表示正整数集合。

$$
\mathbb{Q}
$$

表示有理数集合。

$$
\mathbb{R}
$$

表示实数集合。

$$
\mathbb{R}^{+}
$$

表示正实数集合。

$$
\mathbb{C}
$$

表示复数集合。

---

# 区间表示法

tags: 2.1 Sets

hint:
开区间、闭区间和半开半闭区间怎样用集合表示？

content:
设 $a,b\in\mathbb{R}$ 且 $a\le b$。

闭区间：

$$
[a,b]=\{x\mid a\le x\le b\}
$$

左闭右开区间：

$$
[a,b)=\{x\mid a\le x<b\}
$$

左开右闭区间：

$$
(a,b]=\{x\mid a<x\le b\}
$$

开区间：

$$
(a,b)=\{x\mid a<x<b\}
$$

---

# 集合相等

tags: 2.1 Sets

hint:
两个集合怎样才算相等？

content:
两个集合相等，当且仅当它们有完全相同的元素。

也就是说：

$$
A=B
$$

当且仅当对任意对象 $x$，都有：

$$
x\in A\leftrightarrow x\in B
$$

集合的列出顺序和重复元素不影响集合本身。

---

# 空集

tags: 2.1 Sets

hint:
没有任何元素的集合怎样表示？

content:
不含任何元素的集合称为空集，记为：

$$
\varnothing
$$

也可写为：

$$
\{\}
$$

注意：

$$
\varnothing\ne\{\varnothing\}
$$

因为 $\{\varnothing\}$ 是含有一个元素的集合，这个元素本身是空集。

---

# 子集与超集

tags: 2.1 Sets

hint:
一个集合的每个元素都属于另一个集合时，怎样记号？

content:
若集合 $A$ 的每个元素都是集合 $B$ 的元素，则称 $A$ 是 $B$ 的子集，$B$ 是 $A$ 的超集，记为：

$$
A\subseteq B
$$

其逻辑形式为：

$$
\forall x(x\in A\to x\in B)
$$

每个集合都是它自身的子集：

$$
A\subseteq A
$$

空集是任意集合的子集：

$$
\varnothing\subseteq A
$$

---

# 真子集

tags: 2.1 Sets

hint:
什么时候一个子集是 proper subset？

content:
若：

$$
A\subseteq B
$$

且：

$$
A\ne B
$$

则称 $A$ 是 $B$ 的真子集，记为：

$$
A\subset B
$$

真子集要求 $B$ 中至少有一个元素不属于 $A$。

---

# 证明两个集合相等

tags: 2.1 Sets

hint:
证明集合相等通常分哪两个包含关系？

content:
证明两个集合 $A$ 与 $B$ 相等，常用双向包含法。

只需证明：

$$
A\subseteq B
$$

以及：

$$
B\subseteq A
$$

若两者都成立，则：

$$
A=B
$$

---

# 有限集与基数

tags: 2.1 Sets

hint:
集合中元素个数怎样表示？

content:
若集合 $A$ 有有限个不同元素，则称 $A$ 为有限集。

集合 $A$ 的元素个数称为 $A$ 的基数，记为：

$$
|A|
$$

若集合 $A$ 含有 $n$ 个不同元素，则：

$$
|A|=n
$$

---

# 幂集

tags: 2.1 Sets

hint:
一个集合的所有子集组成什么集合？

content:
集合 $A$ 的幂集是由 $A$ 的所有子集构成的集合，记为：

$$
\mathcal{P}(A)
$$

定义为：

$$
\mathcal{P}(A)=\{S\mid S\subseteq A\}
$$

若：

$$
|A|=n
$$

则：

$$
|\mathcal{P}(A)|=2^n
$$

---

# 笛卡尔积

tags: 2.1 Sets

hint:
两个集合的有序对集合怎样定义？

content:
集合 $A$ 和 $B$ 的笛卡尔积定义为：

$$
A\times B=\{(a,b)\mid a\in A,\ b\in B\}
$$

其中 $(a,b)$ 是有序对，通常：

$$
(a,b)\ne(b,a)
$$

因此一般有：

$$
A\times B\ne B\times A
$$

除非在特殊情况下二者相等。

---

# 笛卡尔积的基数

tags: 2.1 Sets

hint:
有限集合的笛卡尔积有多少个有序对？

content:
若 $A$ 和 $B$ 是有限集，则：

$$
|A\times B|=|A||B|
$$

更一般地，对有限集合 $A_1,A_2,\ldots,A_n$，有：

$$
|A_1\times A_2\times\cdots\times A_n|
=
|A_1||A_2|\cdots |A_n|
$$

---

# 有序 $n$ 元组

tags: 2.1 Sets

hint:
有序对怎样推广到多个坐标？

content:
有序 $n$ 元组写作：

$$
(a_1,a_2,\ldots,a_n)
$$

其中每个位置都重要。

两个有序 $n$ 元组相等，当且仅当对应分量全部相等：

$$
(a_1,a_2,\ldots,a_n)=(b_1,b_2,\ldots,b_n)
$$

当且仅当：

$$
a_i=b_i,\quad i=1,2,\ldots,n
$$

---

# 多个集合的笛卡尔积

tags: 2.1 Sets

hint:
多个集合的笛卡尔积由什么样的元组组成？

content:
多个集合的笛卡尔积定义为：

$$
A_1\times A_2\times\cdots\times A_n
=
\{(a_1,a_2,\ldots,a_n)\mid a_i\in A_i,\ i=1,2,\ldots,n\}
$$

若所有集合都相同，即：

$$
A_1=A_2=\cdots=A_n=A
$$

则常记为：

$$
A^n
$$

---

# 集合并运算

tags: 2.2 Set Operations

hint:
属于 $A$ 或属于 $B$ 的元素组成哪个集合？

content:
集合 $A$ 与 $B$ 的并集定义为：

$$
A\cup B=\{x\mid x\in A\lor x\in B\}
$$

也就是说，元素属于 $A\cup B$，当且仅当它属于 $A$、属于 $B$，或同时属于二者。

---

# 集合交运算

tags: 2.2 Set Operations

hint:
同时属于两个集合的元素组成哪个集合？

content:
集合 $A$ 与 $B$ 的交集定义为：

$$
A\cap B=\{x\mid x\in A\land x\in B\}
$$

元素属于 $A\cap B$，当且仅当它同时属于 $A$ 和 $B$。

---

# 不相交集合

tags: 2.2 Set Operations

hint:
两个集合没有公共元素时叫什么？

content:
若两个集合 $A$ 与 $B$ 没有公共元素，则称它们不相交。

形式化地说，$A$ 与 $B$ 不相交当且仅当：

$$
A\cap B=\varnothing
$$

---

# 集合差

tags: 2.2 Set Operations

hint:
属于 $A$ 但不属于 $B$ 的元素组成什么？

content:
集合 $A$ 与 $B$ 的差定义为：

$$
A-B=\{x\mid x\in A\land x\notin B\}
$$

也常写作：

$$
A\setminus B
$$

它表示从 $A$ 中去掉所有属于 $B$ 的元素。

---

# 补集

tags: 2.2 Set Operations

hint:
相对于全集，不属于某集合的元素组成什么？

content:
设全集为 $U$。集合 $A$ 的补集定义为：

$$
\overline{A}=\{x\in U\mid x\notin A\}
$$

也可写为：

$$
U-A
$$

补集总是相对于给定全集而言的。

---

# 对称差

tags: 2.2 Set Operations

hint:
属于其中一个集合但不同时属于两个集合的元素组成什么？

content:
集合 $A$ 与 $B$ 的对称差定义为：

$$
A\oplus B=\{x\mid x\in A\oplus x\in B\}
$$

也可写作：

$$
A\oplus B=(A-B)\cup(B-A)
$$

等价地：

$$
A\oplus B=(A\cup B)-(A\cap B)
$$

它包含属于 $A$ 或 $B$ 但不同时属于二者的元素。

---

# 集合恒等律与支配律

tags: 2.2 Set Operations

hint:
空集和全集在并、交中扮演什么角色？

content:
集合运算中的恒等律为：

$$
A\cup\varnothing=A
$$

$$
A\cap U=A
$$

支配律为：

$$
A\cup U=U
$$

$$
A\cap\varnothing=\varnothing
$$

---

# 集合幂等律、补元律与双重补集律

tags: 2.2 Set Operations

hint:
集合和自身运算、与补集运算会得到什么？

content:
幂等律：

$$
A\cup A=A
$$

$$
A\cap A=A
$$

补元律：

$$
A\cup\overline{A}=U
$$

$$
A\cap\overline{A}=\varnothing
$$

双重补集律：

$$
\overline{\overline{A}}=A
$$

---

# 集合交换律与结合律

tags: 2.2 Set Operations

hint:
并集和交集的顺序、括号是否重要？

content:
交换律：

$$
A\cup B=B\cup A
$$

$$
A\cap B=B\cap A
$$

结合律：

$$
(A\cup B)\cup C=A\cup(B\cup C)
$$

$$
(A\cap B)\cap C=A\cap(B\cap C)
$$

因此多个集合做并或交时，括号可以省略。

---

# 集合分配律

tags: 2.2 Set Operations

hint:
并和交如何互相分配？

content:
集合运算的分配律为：

$$
A\cup(B\cap C)=(A\cup B)\cap(A\cup C)
$$

$$
A\cap(B\cup C)=(A\cap B)\cup(A\cap C)
$$

---

# 集合吸收律

tags: 2.2 Set Operations

hint:
集合表达式里出现 $A$ 和含 $A$ 的更复杂项时如何简化？

content:
集合吸收律为：

$$
A\cup(A\cap B)=A
$$

$$
A\cap(A\cup B)=A
$$

吸收律常用于化简集合表达式。

---

# 集合德摩根律

tags: 2.2 Set Operations

hint:
补集怎样把并变成交、把交变成并？

content:
集合德摩根律为：

$$
\overline{A\cup B}=\overline{A}\cap\overline{B}
$$

$$
\overline{A\cap B}=\overline{A}\cup\overline{B}
$$

推广到多个集合：

$$
\overline{\bigcup_{i=1}^{n}A_i}
=
\bigcap_{i=1}^{n}\overline{A_i}
$$

$$
\overline{\bigcap_{i=1}^{n}A_i}
=
\bigcup_{i=1}^{n}\overline{A_i}
$$

---

# 广义并集与广义交集

tags: 2.2 Set Operations

hint:
一族集合的并和交怎样用大型运算符表示？

content:
给定集合族：

$$
A_1,A_2,\ldots,A_n
$$

它们的广义并集为：

$$
\bigcup_{i=1}^{n}A_i
=
A_1\cup A_2\cup\cdots\cup A_n
$$

元素属于该并集，当且仅当它属于至少一个 $A_i$。

广义交集为：

$$
\bigcap_{i=1}^{n}A_i
=
A_1\cap A_2\cap\cdots\cap A_n
$$

元素属于该交集，当且仅当它属于每一个 $A_i$。

---

# 用位串表示有限集合

tags: 2.2 Set Operations

hint:
怎样把有限全集中的子集编码成 0-1 位串？

content:
设全集为有限集：

$$
U=\{u_1,u_2,\ldots,u_n\}
$$

若 $A\subseteq U$，则可以用长度为 $n$ 的位串表示 $A$。

第 $i$ 位为 $1$，表示：

$$
u_i\in A
$$

第 $i$ 位为 $0$，表示：

$$
u_i\notin A
$$

这样，集合运算可以转化为位运算。

---

# 位串表示下的集合运算

tags: 2.2 Set Operations

hint:
并、交、补分别对应哪些按位逻辑运算？

content:
在位串表示下，集合并对应按位 OR：

$$
A\cup B\leftrightarrow a_i\lor b_i
$$

集合交对应按位 AND：

$$
A\cap B\leftrightarrow a_i\land b_i
$$

集合补对应按位 NOT：

$$
\overline{A}\leftrightarrow \neg a_i
$$

集合对称差对应按位 XOR：

$$
A\oplus B\leftrightarrow a_i\oplus b_i
$$

---

# 函数

tags: 2.3 Functions

hint:
函数怎样把一个集合中的每个元素唯一地送到另一个集合？

content:
从集合 $A$ 到集合 $B$ 的函数是把 $A$ 中每个元素唯一地对应到 $B$ 中某个元素的规则。

记作：

$$
f:A\to B
$$

若 $a\in A$，则 $f(a)$ 是 $a$ 在函数 $f$ 下的像。

集合 $A$ 称为定义域，集合 $B$ 称为陪域。

---

# 函数的值域

tags: 2.3 Functions

hint:
函数实际取到的所有值组成哪个集合？

content:
函数 $f:A\to B$ 的值域是所有实际被取到的函数值组成的集合：

$$
\operatorname{range}(f)=\{f(a)\mid a\in A\}
$$

值域总是陪域 $B$ 的子集：

$$
\operatorname{range}(f)\subseteq B
$$

---

# 函数相等

tags: 2.3 Functions

hint:
两个函数怎样才算是同一个函数？

content:
两个函数相等，需要满足三个条件：

1. 定义域相同；
2. 陪域相同；
3. 每个定义域元素的函数值相同。

形式化地，$f$ 与 $g$ 相等，当且仅当：

$$
f:A\to B,\quad g:A\to B
$$

且对所有 $x\in A$，都有：

$$
f(x)=g(x)
$$

---

# 函数的像

tags: 2.3 Functions

hint:
一个子集在函数作用下会变成哪个集合？

content:
设 $f:A\to B$，且 $S\subseteq A$。集合 $S$ 在 $f$ 下的像为：

$$
f(S)=\{f(s)\mid s\in S\}
$$

特别地，整个定义域 $A$ 的像就是函数值域：

$$
f(A)=\operatorname{range}(f)
$$

---

# 函数的原像

tags: 2.3 Functions

hint:
陪域中的一个子集，对应到定义域中哪些元素？

content:
设 $f:A\to B$，且 $T\subseteq B$。集合 $T$ 在 $f$ 下的原像为：

$$
f^{-1}(T)=\{a\in A\mid f(a)\in T\}
$$

注意：即使函数 $f$ 没有反函数，集合原像 $f^{-1}(T)$ 仍然有意义。

---

# 一对一函数

tags: 2.3 Functions

hint:
不同输入不能得到相同输出，这是什么性质？

content:
函数 $f:A\to B$ 称为一对一函数或单射，当且仅当对任意 $a,b\in A$：

$$
f(a)=f(b)\to a=b
$$

等价地：

$$
a\ne b\to f(a)\ne f(b)
$$

也就是说，不同的定义域元素有不同的像。

---

# 满射

tags: 2.3 Functions

hint:
陪域中每个元素都被取到，这是什么性质？

content:
函数 $f:A\to B$ 称为从 $A$ 到 $B$ 上的函数或满射，当且仅当：

$$
\forall b\in B\,\exists a\in A\,(f(a)=b)
$$

等价地：

$$
\operatorname{range}(f)=B
$$

即陪域中的每个元素至少有一个原像。

---

# 双射

tags: 2.3 Functions

hint:
既一对一又满的函数有什么特殊意义？

content:
函数 $f:A\to B$ 若既是一对一函数，又是满射，则称为双射。

双射在 $A$ 和 $B$ 之间建立一一对应关系。

若存在双射：

$$
f:A\to B
$$

则集合 $A$ 与 $B$ 的元素可以完全配对。

---

# 反函数

tags: 2.3 Functions

hint:
什么样的函数可以反过来定义？

content:
若 $f:A\to B$ 是双射，则存在反函数：

$$
f^{-1}:B\to A
$$

满足：

$$
f^{-1}(b)=a
$$

当且仅当：

$$
f(a)=b
$$

反函数把每个像唯一地送回它的原像。

---

# 函数组合

tags: 2.3 Functions

hint:
先做一个函数再做另一个函数，如何记号？

content:
设：

$$
f:A\to B
$$

$$
g:B\to C
$$

则 $f$ 与 $g$ 的复合函数记为：

$$
g\circ f:A\to C
$$

定义为：

$$
(g\circ f)(a)=g(f(a))
$$

复合函数的顺序很重要，一般：

$$
g\circ f\ne f\circ g
$$

---

# 函数图像

tags: 2.3 Functions

hint:
函数可以怎样用有序对集合表示？

content:
函数 $f:A\to B$ 的图像是有序对集合：

$$
\{(a,f(a))\mid a\in A\}
$$

它完整记录了每个定义域元素及其函数值。

对于有限集合上的函数，图像可以看作一张由输入和输出组成的表。

---

# 下取整函数

tags: 2.3 Functions

hint:
不超过 $x$ 的最大整数怎样记？

content:
下取整函数记为：

$$
\lfloor x\rfloor
$$

它表示小于或等于 $x$ 的最大整数。

其基本性质为：

$$
\lfloor x\rfloor\le x<\lfloor x\rfloor+1
$$

若 $n\in\mathbb{Z}$，则：

$$
\lfloor x\rfloor=n
$$

当且仅当：

$$
n\le x<n+1
$$

---

# 上取整函数

tags: 2.3 Functions

hint:
不小于 $x$ 的最小整数怎样记？

content:
上取整函数记为：

$$
\lceil x\rceil
$$

它表示大于或等于 $x$ 的最小整数。

其基本性质为：

$$
\lceil x\rceil-1<x\le \lceil x\rceil
$$

若 $n\in\mathbb{Z}$，则：

$$
\lceil x\rceil=n
$$

当且仅当：

$$
n-1<x\le n
$$

---

# 序列

tags: 2.4 Sequences and Summations

hint:
序列可以看成哪类函数？

content:
序列是定义域为整数集合中连续部分的函数。

通常把序列写为：

$$
a_1,a_2,\ldots,a_n,\ldots
$$

也可写为：

$$
\{a_n\}
$$

其中 $a_n$ 称为序列的第 $n$ 项。

有限序列只含有限多项；无限序列含有无限多项。

---

# 等比数列

tags: 2.4 Sequences and Summations

hint:
相邻两项比值固定的序列怎样表示？

content:
等比数列的一般形式为：

$$
a,ar,ar^2,\ldots,ar^n,\ldots
$$

其第 $n$ 项可写为：

$$
a_n=ar^n
$$

其中 $a$ 为初始因子，$r$ 为公比。

---

# 等差数列

tags: 2.4 Sequences and Summations

hint:
相邻两项差值固定的序列怎样表示？

content:
等差数列的一般形式为：

$$
a,a+d,a+2d,\ldots,a+nd,\ldots
$$

其第 $n$ 项可写为：

$$
a_n=a+nd
$$

其中 $a$ 为初始项，$d$ 为公差。

---

# 字符串

tags: 2.4 Sequences and Summations

hint:
字符串可以看成由什么组成的有限序列？

content:
字符串是由某个字符集中的元素组成的有限序列。

若字母表为 $\Sigma$，则一个长度为 $n$ 的字符串可写为：

$$
a_1a_2\cdots a_n
$$

其中：

$$
a_i\in\Sigma,\quad i=1,2,\ldots,n
$$

空字符串是不含任何字符的字符串，常记为：

$$
\lambda
$$

---

# 递推关系

tags: 2.4 Sequences and Summations

hint:
怎样用前面的项定义后面的项？

content:
递推关系是用序列前面若干项来定义后面项的等式。

一般形式可写为：

$$
a_n=F(a_0,a_1,\ldots,a_{n-1})
$$

为了唯一确定序列，还需要给出初始条件，例如：

$$
a_0=c
$$

或：

$$
a_0=c_0,\quad a_1=c_1
$$

递推关系加初始条件可以确定一个序列。

---

# 求和记号

tags: 2.4 Sequences and Summations

hint:
怎样用 $\sum$ 表示一串项的和？

content:
求和记号：

$$
\sum_{j=m}^{n}a_j
$$

表示：

$$
a_m+a_{m+1}+\cdots+a_n
$$

其中 $j$ 称为求和指标，$m$ 是下限，$n$ 是上限。

如果下限大于上限，通常约定该和为空和，其值为：

$$
0
$$

---

# 求和的线性性质

tags: 2.4 Sequences and Summations

hint:
求和能否拆开？常数能否提出？

content:
对任意常数 $c$，有：

$$
\sum_{j=m}^{n}ca_j
=
c\sum_{j=m}^{n}a_j
$$

对两个序列 $a_j,b_j$，有：

$$
\sum_{j=m}^{n}(a_j+b_j)
=
\sum_{j=m}^{n}a_j+\sum_{j=m}^{n}b_j
$$

以及：

$$
\sum_{j=m}^{n}(a_j-b_j)
=
\sum_{j=m}^{n}a_j-\sum_{j=m}^{n}b_j
$$

---

# 常用求和公式

tags: 2.4 Sequences and Summations

hint:
前 $n$ 个整数、平方数、立方数的和公式是什么？

content:
常用求和公式包括：

$$
\sum_{j=1}^{n}j=\frac{n(n+1)}{2}
$$

$$
\sum_{j=1}^{n}j^2=\frac{n(n+1)(2n+1)}{6}
$$

$$
\sum_{j=1}^{n}j^3=\left(\frac{n(n+1)}{2}\right)^2
$$

常数列求和：

$$
\sum_{j=1}^{n}1=n
$$

---

# 有限等比求和公式

tags: 2.4 Sequences and Summations

hint:
有限等比数列的和怎样计算？

content:
若 $r\ne1$，则有限等比和为：

$$
\sum_{j=0}^{n}ar^j
=
a\frac{r^{n+1}-1}{r-1}
$$

也可写为：

$$
\sum_{j=0}^{n}ar^j
=
a\frac{1-r^{n+1}}{1-r}
$$

若 $r=1$，则：

$$
\sum_{j=0}^{n}a=(n+1)a
$$

---

# 双重求和

tags: 2.4 Sequences and Summations

hint:
二维指标的求和如何理解？

content:
双重求和形如：

$$
\sum_{i=1}^{m}\sum_{j=1}^{n}a_{ij}
$$

它表示先对 $j$ 求和，再对 $i$ 求和：

$$
\sum_{i=1}^{m}
\left(
\sum_{j=1}^{n}a_{ij}
\right)
$$

若求和区域是矩形，且各项有定义，则有限双重和可以交换求和顺序：

$$
\sum_{i=1}^{m}\sum_{j=1}^{n}a_{ij}
=
\sum_{j=1}^{n}\sum_{i=1}^{m}a_{ij}
$$

---

# 乘积记号

tags: 2.4 Sequences and Summations

hint:
怎样用 $\prod$ 表示一串项的乘积？

content:
乘积记号：

$$
\prod_{j=m}^{n}a_j
$$

表示：

$$
a_m a_{m+1}\cdots a_n
$$

其中 $j$ 是乘积指标。

如果乘积下限大于上限，通常约定为空积，其值为：

$$
1
$$

---

# 集合的相同基数

tags: 2.5 Cardinality of Sets

hint:
两个集合什么时候“大小相同”？

content:
若存在从集合 $A$ 到集合 $B$ 的双射：

$$
f:A\to B
$$

则称 $A$ 与 $B$ 有相同的基数，记为：

$$
|A|=|B|
$$

这意味着 $A$ 中元素可以和 $B$ 中元素一一配对。

---

# 有限集的基数比较

tags: 2.5 Cardinality of Sets

hint:
有限集合中，双射与元素个数有什么关系？

content:
若 $A$ 和 $B$ 是有限集，则：

$$
|A|=|B|
$$

当且仅当它们含有相同数量的元素。

若存在从 $A$ 到 $B$ 的单射，则：

$$
|A|\le |B|
$$

若存在从 $A$ 到 $B$ 的满射，则：

$$
|A|\ge |B|
$$

---

# 可数集

tags: 2.5 Cardinality of Sets

hint:
什么样的集合可以按自然数顺序列出来？

content:
集合 $S$ 称为可数集，如果它是有限集，或者它与正整数集合有相同基数。

若 $S$ 与正整数集合有相同基数，则称 $S$ 为可数无限集。

也就是说，若存在双射：

$$
f:\mathbb{Z}^{+}\to S
$$

则 $S$ 是可数无限集。

---

# 不可数集

tags: 2.5 Cardinality of Sets

hint:
什么样的无限集合无法用正整数列举？

content:
若一个集合既不是有限集，也不是可数无限集，则称它为不可数集。

不可数集无法被排列成一个序列：

$$
s_1,s_2,s_3,\ldots
$$

使得集合中每个元素都恰好出现一次。

实数集合 $\mathbb{R}$ 是不可数的。

---

# 整数集合可数

tags: 2.5 Cardinality of Sets

hint:
正整数、整数是否有相同的基数？

content:
整数集合 $\mathbb{Z}$ 是可数无限集。

虽然 $\mathbb{Z}$ 包含正整数、负整数和零，但它仍可按如下方式排列：

$$
0,1,-1,2,-2,3,-3,\ldots
$$

因此存在从 $\mathbb{Z}^{+}$ 到 $\mathbb{Z}$ 的双射。

---

# 有理数集合可数

tags: 2.5 Cardinality of Sets

hint:
分数看起来很多，为什么仍然可以数出来？

content:
有理数集合 $\mathbb{Q}$ 是可数的。

原因是每个有理数都可写成整数之比，并且可以系统地枚举所有分子分母组合，再跳过重复表示。

因此，尽管有理数在数轴上稠密，集合 $\mathbb{Q}$ 仍然是可数无限集。

---

# 实数集合不可数

tags: 2.5 Cardinality of Sets

hint:
为什么区间中的实数不能被列成一个序列？

content:
实数集合 $\mathbb{R}$ 是不可数的。

特别地，区间 $(0,1)$ 中的实数不可数。

常用证明方法是康托对角线法：假设 $(0,1)$ 中所有实数都能列成一个序列，然后构造一个新的实数，使它与列表中第 $n$ 个实数的第 $n$ 位小数不同，从而该实数不在列表中，得到矛盾。

---

# 基数不等式

tags: 2.5 Cardinality of Sets

hint:
怎样比较两个集合的基数大小？

content:
若存在从 $A$ 到 $B$ 的单射，则记：

$$
|A|\le |B|
$$

若存在从 $A$ 到 $B$ 的双射，则：

$$
|A|=|B|
$$

若：

$$
|A|\le |B|
$$

且：

$$
|A|\ne |B|
$$

则记：

$$
|A|<|B|
$$

---

# 施罗德-伯恩斯坦定理

tags: 2.5 Cardinality of Sets

hint:
两个集合若能互相单射到对方，能推出什么？

content:
若存在从 $A$ 到 $B$ 的单射，也存在从 $B$ 到 $A$ 的单射，则 $A$ 和 $B$ 有相同基数。

形式化地：

$$
|A|\le |B|\land |B|\le |A|\to |A|=|B|
$$

这称为施罗德-伯恩斯坦定理。

---

# 矩阵

tags: 2.6 Matrices

hint:
矩阵由哪些元素按什么形式排列？

content:
矩阵是按行和列排列的数或对象的矩形阵列。

一个 $m\times n$ 矩阵 $A$ 可写为：

$$
A=
\begin{bmatrix}
a_{11} & a_{12} & \cdots & a_{1n}\\
a_{21} & a_{22} & \cdots & a_{2n}\\
\vdots & \vdots & \ddots & \vdots\\
a_{m1} & a_{m2} & \cdots & a_{mn}
\end{bmatrix}
$$

其中 $a_{ij}$ 表示第 $i$ 行第 $j$ 列的元素。

---

# 矩阵的维数与元素记号

tags: 2.6 Matrices

hint:
$m\times n$ 矩阵中 $m$ 和 $n$ 分别表示什么？

content:
若矩阵 $A$ 有 $m$ 行、$n$ 列，则称 $A$ 是 $m\times n$ 矩阵。

常记作：

$$
A=[a_{ij}]
$$

其中：

$$
1\le i\le m,\quad 1\le j\le n
$$

行指标 $i$ 表示元素所在行，列指标 $j$ 表示元素所在列。

---

# 矩阵相等

tags: 2.6 Matrices

hint:
两个矩阵相等需要满足什么条件？

content:
两个矩阵 $A=[a_{ij}]$ 和 $B=[b_{ij}]$ 相等，当且仅当它们有相同的维数，并且对应元素全部相等。

也就是说：

$$
A=B
$$

当且仅当：

$$
a_{ij}=b_{ij}
$$

对所有允许的 $i,j$ 都成立。

---

# 矩阵加法

tags: 2.6 Matrices

hint:
矩阵加法怎样逐项进行？

content:
只有同维数矩阵才能相加。

设：

$$
A=[a_{ij}],\quad B=[b_{ij}]
$$

都是 $m\times n$ 矩阵，则：

$$
A+B=[a_{ij}+b_{ij}]
$$

即对应位置的元素相加。

---

# 矩阵数乘

tags: 2.6 Matrices

hint:
一个数乘以矩阵时，作用到哪些元素？

content:
若 $A=[a_{ij}]$ 是矩阵，$c$ 是数，则：

$$
cA=[ca_{ij}]
$$

也就是说，矩阵中每个元素都乘以 $c$。

---

# 矩阵乘法

tags: 2.6 Matrices

hint:
矩阵乘法中，左矩阵的列数必须和右矩阵的什么相等？

content:
若 $A=[a_{ij}]$ 是 $m\times k$ 矩阵，$B=[b_{ij}]$ 是 $k\times n$ 矩阵，则乘积 $AB$ 是 $m\times n$ 矩阵。

其第 $i$ 行第 $j$ 列元素为：

$$
(AB)_{ij}=\sum_{\ell=1}^{k}a_{i\ell}b_{\ell j}
$$

矩阵乘法一般不满足交换律，即通常：

$$
AB\ne BA
$$

---

# 零矩阵与单位矩阵

tags: 2.6 Matrices

hint:
矩阵加法和乘法中的特殊矩阵是什么？

content:
所有元素都为 $0$ 的矩阵称为零矩阵，记为：

$$
O
$$

$n\times n$ 单位矩阵记为：

$$
I_n
$$

其主对角线元素为 $1$，其余元素为 $0$：

$$
(I_n)_{ij}=
\begin{cases}
1, & i=j,\\
0, & i\ne j.
\end{cases}
$$

对任意适当维数的矩阵 $A$，有：

$$
AI=IA=A
$$

---

# 矩阵转置

tags: 2.6 Matrices

hint:
转置如何交换矩阵的行和列？

content:
矩阵 $A=[a_{ij}]$ 的转置记为：

$$
A^T
$$

若 $A$ 是 $m\times n$ 矩阵，则 $A^T$ 是 $n\times m$ 矩阵，并且：

$$
(A^T)_{ij}=a_{ji}
$$

转置会把原矩阵的行变成列，列变成行。

---

# 方阵与矩阵幂

tags: 2.6 Matrices

hint:
什么时候可以定义矩阵的幂？

content:
行数和列数相同的矩阵称为方阵。

若 $A$ 是 $n\times n$ 方阵，则可以定义矩阵幂：

$$
A^0=I_n
$$

$$
A^r=\underbrace{AA\cdots A}_{r\text{ factors}}
$$

其中 $r$ 为正整数。

---

# 零一矩阵

tags: 2.6 Matrices

hint:
只含 $0$ 和 $1$ 的矩阵叫什么？

content:
只含 $0$ 和 $1$ 的矩阵称为零一矩阵。

零一矩阵常用于表示离散结构，例如关系、图、网络连接和布尔数据。

若 $A=[a_{ij}]$ 是零一矩阵，则：

$$
a_{ij}\in\{0,1\}
$$

---

# 零一矩阵的 join 与 meet

tags: 2.6 Matrices

hint:
零一矩阵上的并和交如何逐项定义？

content:
设 $A=[a_{ij}]$ 与 $B=[b_{ij}]$ 是同维零一矩阵。

它们的 join 记为：

$$
A\lor B
$$

逐项定义为：

$$
(A\lor B)_{ij}=a_{ij}\lor b_{ij}
$$

它们的 meet 记为：

$$
A\land B
$$

逐项定义为：

$$
(A\land B)_{ij}=a_{ij}\land b_{ij}
$$

---

# 零一矩阵的布尔积

tags: 2.6 Matrices

hint:
布尔矩阵乘法把普通加法和乘法替换成了什么？

content:
设 $A=[a_{ij}]$ 是 $m\times k$ 零一矩阵，$B=[b_{ij}]$ 是 $k\times n$ 零一矩阵。

它们的布尔积 $A\odot B$ 是 $m\times n$ 零一矩阵，其元素为：

$$
(A\odot B)_{ij}
=
\bigvee_{\ell=1}^{k}(a_{i\ell}\land b_{\ell j})
$$

布尔积把普通矩阵乘法中的乘法替换为 $\land$，把加法替换为 $\lor$。

---

# 零一矩阵的布尔幂

tags: 2.6 Matrices

hint:
零一方阵在布尔积下怎样定义幂？

content:
若 $A$ 是 $n\times n$ 零一方阵，则其布尔幂用布尔积递归定义：

$$
A^{[1]}=A
$$

$$
A^{[r]}=A^{[r-1]}\odot A
$$

其中 $r$ 为正整数。

布尔幂常用于研究关系或图中经过若干步的可达性。

---

# 算法

tags: 3.1 Algorithms

hint:
算法是解决问题的一串什么样的步骤？

content:
算法是用于执行计算或解决问题的有限序列精确指令。

一个算法应当把一般问题的输入转化为期望的输出，而不是只解决某一个具体实例。

算法通常关注“方法本身”，而程序则是把算法用某种编程语言实现出来的具体文本。

---

# 数学建模与算法

tags: 3.1 Algorithms

hint:
解决一般问题时，为什么先要建立数学模型？

content:
许多实际问题需要先转化为数学模型，再设计解决模型的算法。

常用离散结构包括集合、序列、函数、排列、关系、图、树、网络和有限状态机等。

基本过程是：

1. 把实际问题翻译为数学对象；
2. 明确输入、输出和约束；
3. 给出有限、精确、可执行的求解步骤；
4. 证明算法正确；
5. 分析算法所需资源。

---

# 算法的输入与输出

tags: 3.1 Algorithms

hint:
算法通常要说明哪些输入和输出？

content:
算法通常需要明确输入和输出。

输入是算法开始执行前给定的数据。

输出是算法执行结束后产生的结果。

一个算法可以有零个或多个输入，但必须有至少一个输出。

描述算法时，应当说明输入数据的类型、规模和满足的前提条件，以及输出结果应满足的性质。

---

# 算法的确定性

tags: 3.1 Algorithms

hint:
为什么算法的每一步不能含糊？

content:
算法的每一步必须精确明确，执行者在每一步都能确定下一步该做什么。

这种性质称为确定性。

若一个过程包含含糊指令、无法判断的选择，或没有明确的执行规则，则它不是严格意义上的算法。

---

# 算法的正确性

tags: 3.1 Algorithms

hint:
一个算法什么时候算正确？

content:
若算法对每一个合法输入都能产生正确输出，则称该算法是正确的。

算法正确性通常需要证明，而不能只依赖少量测试。

证明算法正确时，一般需要说明：

1. 算法确实会终止；
2. 终止时输出满足问题要求；
3. 所有合法输入都被覆盖。

---

# 算法的有限性、有效性与通用性

tags: 3.1 Algorithms

hint:
算法为什么必须会停，并且每一步必须可实际执行？

content:
算法必须具有有限性，即对任意合法输入，算法应在有限步后终止。

算法必须具有有效性，即每一步操作都足够基本，可以准确执行。

算法还应具有通用性，即它解决的是一类问题，而不是单个特定实例。

---

# 伪代码

tags: 3.1 Algorithms

hint:
为什么教材用伪代码而不是某种具体编程语言？

content:
伪代码是一种介于自然语言和程序设计语言之间的算法描述方式。

它强调算法思想，而不是具体语言的语法细节。

伪代码通常使用：

```text
procedure
if ... then
while
for
return
```

等结构描述控制流程。

赋值语句常写作：

```text
x := value
```

注释通常用大括号或自然语言说明。

---

# 最大元素算法

tags: 3.1 Algorithms

hint:
如何在有限序列中寻找最大项？

content:
寻找有限序列中最大元素的基本算法是：先把第一个元素作为当前最大值，然后从左到右扫描序列，遇到更大的元素就更新当前最大值。

伪代码：

```text
procedure max(a1, a2, ..., an)
    max := a1
    for i := 2 to n
        if max < ai then max := ai
    return max
```

该算法对含 $n$ 个元素的序列使用：

$$
n-1
$$

次比较。

---

# 搜索问题

tags: 3.1 Algorithms

hint:
搜索算法要解决的核心问题是什么？

content:
搜索问题的目标是在一个列表中定位某个指定元素。

输入通常包括一个列表：

$$
a_1,a_2,\ldots,a_n
$$

和目标元素 $x$。

输出通常是满足：

$$
a_i=x
$$

的位置 $i$，若不存在这样的 $i$，则返回特殊值，例如 $0$ 或“未找到”。

---

# 线性搜索

tags: 3.1 Algorithms

hint:
最直接的搜索算法怎样逐个检查元素？

content:
线性搜索按列表顺序逐个检查元素，直到找到目标或扫描完整个列表。

伪代码：

```text
procedure linear_search(x, a1, a2, ..., an)
    i := 1
    while i <= n and x != ai
        i := i + 1
    if i <= n then location := i
    else location := 0
    return location
```

线性搜索不要求列表有序。

---

# 二分搜索

tags: 3.1 Algorithms

hint:
有序列表中如何反复把搜索范围减半？

content:
二分搜索适用于已经按递增顺序排列的列表。

它每次比较目标元素 $x$ 与当前中间元素，根据比较结果丢弃左半部分或右半部分。

伪代码：

```text
procedure binary_search(x, a1, a2, ..., an)
    i := 1
    j := n
    while i < j
        m := floor((i + j) / 2)
        if x > am then i := m + 1
        else j := m
    if x = ai then location := i
    else location := 0
    return location
```

二分搜索的关键思想是：每一步都把可能位置集合大约缩小一半。

---

# 排序

tags: 3.1 Algorithms

hint:
排序算法的目标是什么？

content:
排序是把列表中的元素重新排列成指定顺序的过程。

若列表元素可以比较大小，递增排序的目标是把：

$$
a_1,a_2,\ldots,a_n
$$

重排为：

$$
b_1,b_2,\ldots,b_n
$$

使得：

$$
b_1\le b_2\le\cdots\le b_n
$$

排序是计算机科学中最重要的基础问题之一。

---

# 冒泡排序

tags: 3.1 Algorithms

hint:
冒泡排序怎样通过相邻交换把大元素逐步沉到底部？

content:
冒泡排序通过多轮扫描列表来排序。每一轮从列表开头开始，依次比较相邻元素，若顺序错误就交换它们。

一轮扫描后，当前未排序部分中的最大元素会被交换到它应在的位置。

伪代码：

```text
procedure bubble_sort(a1, a2, ..., an)
    for i := 1 to n - 1
        for j := 1 to n - i
            if aj > a(j+1) then interchange aj and a(j+1)
```

冒泡排序简单，但通常效率不高。

---

# 插入排序

tags: 3.1 Algorithms

hint:
插入排序怎样维护一个已经排好序的前缀？

content:
插入排序从第二个元素开始，逐步把第 $j$ 个元素插入前面已经排好序的 $j-1$ 个元素中的正确位置。

第 $j$ 步完成后，前 $j$ 个元素已经有序。

伪代码：

```text
procedure insertion_sort(a1, a2, ..., an)
    for j := 2 to n
        i := 1
        while aj > ai
            i := i + 1
        m := aj
        for k := 0 to j - i - 1
            a(j-k) := a(j-k-1)
        ai := m
```

插入排序在列表已经接近有序时通常表现较好。

---

# 字符串匹配

tags: 3.1 Algorithms

hint:
怎样在长文本中寻找短模式串出现的位置？

content:
字符串匹配问题的目标是：给定文本串 $T$ 和模式串 $P$，找出 $P$ 在 $T$ 中出现的所有位置。

若文本为：

$$
T=t_1t_2\cdots t_n
$$

模式为：

$$
P=p_1p_2\cdots p_m
$$

其中 $m\le n$，则称 $P$ 在位移 $s$ 处出现，当且仅当：

$$
t_{s+1}=p_1,\ t_{s+2}=p_2,\ \ldots,\ t_{s+m}=p_m
$$

---

# 朴素字符串匹配算法

tags: 3.1 Algorithms

hint:
最直接的字符串匹配算法怎样检查所有位移？

content:
朴素字符串匹配算法依次检查所有可能位移：

$$
s=0,1,\ldots,n-m
$$

对每个位移，逐字符比较模式串和文本对应位置。

伪代码：

```text
procedure string_match(n, m, t1, ..., tn, p1, ..., pm)
    for s := 0 to n - m
        j := 1
        while j <= m and t(s+j) = pj
            j := j + 1
        if j > m then print "s is a valid shift"
```

该算法思想简单，但在长文本和长模式串上可能效率较低。

---

# 贪心算法

tags: 3.1 Algorithms

hint:
贪心算法每一步选择什么？

content:
贪心算法是在每一步选择当前看来最好的选择，而不是枚举所有可能的选择序列。

贪心算法常用于优化问题，例如最小化代价或最大化收益。

重要提醒：贪心算法得到可行解，并不自动保证得到最优解。必须证明它总能得到最优解，或者给出反例说明它不总是最优。

---

# 收银员找零算法

tags: 3.1 Algorithms

hint:
找零时每一步取什么硬币？

content:
收银员找零算法是一种贪心算法。对于给定金额，每一步都选择面值不超过剩余金额的最大硬币。

伪代码：

```text
procedure change(c1, c2, ..., cr, n)
    for i := 1 to r
        di := 0
        while n >= ci
            di := di + 1
            n := n - ci
    return d1, d2, ..., dr
```

其中硬币面值通常按从大到小排列：

$$
c_1>c_2>\cdots>c_r
$$

该算法在某些币值系统中最优，但并非对所有币值系统都最优。

---

# 活动安排贪心算法

tags: 3.1 Algorithms

hint:
想安排尽可能多的不重叠报告，应优先选择哪一个？

content:
活动安排问题要求在同一场地中安排尽可能多的互不重叠活动。

一种贪心策略是：每一步选择与已安排活动兼容、且结束时间最早的活动。

算法思想：

```text
sort talks by nondecreasing ending time
schedule := empty
for each talk in sorted order
    if talk starts after or when the last scheduled talk ends
        add talk to schedule
return schedule
```

这种策略的关键是：尽早结束的活动为后续活动留下最大空间。

---

# 函数增长

tags: 3.2 The Growth of Functions

hint:
为什么分析算法时只关心增长趋势？

content:
算法分析常关注输入规模增大时资源需求的增长趋势，而不是精确运行时间。

为了描述增长趋势，通常忽略常数倍和低阶项。

例如函数：

$$
3n^2+10n+5
$$

在大规模输入下主要由：

$$
n^2
$$

这一项决定增长速度。

---

# 大 $O$ 记号

tags: 3.2 The Growth of Functions

hint:
$f(x)$ 是 $O(g(x))$ 表示什么上界关系？

content:
设 $f$ 和 $g$ 是从实数或整数到实数的函数。

若存在常数 $C>0$ 和 $k$，使得当 $x>k$ 时：

$$
|f(x)|\le C|g(x)|
$$

则称 $f(x)$ 是 $O(g(x))$，记作：

$$
f(x)=O(g(x))
$$

这表示 $g(x)$ 给出了 $f(x)$ 渐近意义下的上界。

---

# 大 $O$ 的见证

tags: 3.2 The Growth of Functions

hint:
证明 $f(x)=O(g(x))$ 时，需要找出什么常数？

content:
证明：

$$
f(x)=O(g(x))
$$

需要找出一组常数：

$$
C>0,\quad k
$$

使得对所有：

$$
x>k
$$

都有：

$$
|f(x)|\le C|g(x)|
$$

这样的二元组 $(C,k)$ 称为 $f(x)=O(g(x))$ 的见证。

---

# 多项式的大 $O$ 估计

tags: 3.2 The Growth of Functions

hint:
多项式的增长阶由哪一项决定？

content:
若：

$$
f(x)=a_nx^n+a_{n-1}x^{n-1}+\cdots+a_1x+a_0
$$

且：

$$
a_n\ne0
$$

则：

$$
f(x)=O(x^n)
$$

多项式的最高次项决定其渐近增长阶，低次项不会改变大 $O$ 阶。

---

# 大 $O$ 的加法与乘法性质

tags: 3.2 The Growth of Functions

hint:
两个大 $O$ 估计相加或相乘时，阶怎样变化？

content:
若：

$$
f_1(x)=O(g_1(x))
$$

且：

$$
f_2(x)=O(g_2(x))
$$

则：

$$
(f_1+f_2)(x)=O(\max(|g_1(x)|,|g_2(x)|))
$$

并且：

$$
(f_1f_2)(x)=O(g_1(x)g_2(x))
$$

这使得复杂函数的大 $O$ 估计可以由组成部分组合得到。

---

# 大 $\Omega$ 记号

tags: 3.2 The Growth of Functions

hint:
大 $\Omega$ 表示什么下界关系？

content:
若存在常数 $C>0$ 和 $k$，使得当 $x>k$ 时：

$$
|f(x)|\ge C|g(x)|
$$

则称 $f(x)$ 是 $\Omega(g(x))$，记作：

$$
f(x)=\Omega(g(x))
$$

这表示 $g(x)$ 给出了 $f(x)$ 渐近意义下的下界。

---

# 大 $\Theta$ 记号

tags: 3.2 The Growth of Functions

hint:
什么时候两个函数具有同一增长阶？

content:
若同时有：

$$
f(x)=O(g(x))
$$

和：

$$
f(x)=\Omega(g(x))
$$

则称 $f(x)$ 是 $\Theta(g(x))$，记作：

$$
f(x)=\Theta(g(x))
$$

这表示 $f(x)$ 与 $g(x)$ 在渐近意义下具有相同增长阶，只差常数因子。

---

# 多项式的 $\Theta$ 阶

tags: 3.2 The Growth of Functions

hint:
最高次项非零的多项式精确增长阶是什么？

content:
若：

$$
f(x)=a_nx^n+a_{n-1}x^{n-1}+\cdots+a_1x+a_0
$$

且：

$$
a_n\ne0
$$

则：

$$
f(x)=\Theta(x^n)
$$

最高次非零项决定多项式的精确渐近阶。

---

# 常见增长阶

tags: 3.2 The Growth of Functions

hint:
常见函数从慢到快大致如何排列？

content:
常见增长阶从慢到快通常为：

$$
1
$$

$$
\log n
$$

$$
n
$$

$$
n\log n
$$

$$
n^2
$$

$$
n^3
$$

$$
2^n
$$

$$
n!
$$

在算法分析中，多项式增长通常比指数增长和阶乘增长更可接受。

---

# 对数阶与阶乘的估计

tags: 3.2 The Growth of Functions

hint:
$\log n!$ 的常用渐近上界是什么？

content:
因为：

$$
n!=1\cdot2\cdot\cdots\cdot n
$$

且每一项都不超过 $n$，所以：

$$
n!\le n^n
$$

两边取对数得：

$$
\log n!\le n\log n
$$

因此：

$$
\log n!=O(n\log n)
$$

更精确地，$\log n!$ 与 $n\log n$ 同阶。

---

# 算法复杂度

tags: 3.3 Complexity of Algorithms

hint:
算法复杂度衡量哪些资源？

content:
算法复杂度用于衡量算法解决给定规模问题所需的资源。

时间复杂度衡量算法所需的运行时间或基本操作次数。

空间复杂度衡量算法所需的存储空间。

在理论分析中，通常用输入规模 $n$ 的函数来表示复杂度。

---

# 输入规模

tags: 3.3 Complexity of Algorithms

hint:
复杂度为什么必须相对于输入规模来讨论？

content:
输入规模是描述输入数据大小的参数，通常记为 $n$。

不同问题的输入规模含义不同。例如：

列表问题中，$n$ 可以是元素个数；

矩阵问题中，$n$ 可以是矩阵维数；

命题逻辑问题中，$n$ 可以是命题变量个数；

字符串问题中，$n$ 可以是文本长度。

复杂度通常表示为输入规模 $n$ 的函数。

---

# 最坏情况复杂度

tags: 3.3 Complexity of Algorithms

hint:
最坏情况复杂度衡量什么？

content:
最坏情况时间复杂度是在所有规模为 $n$ 的输入中，算法所需时间的最大值。

它描述算法在最不利输入下的性能保证。

若一个算法最坏情况下至多使用 $f(n)$ 级别的基本操作，则可用大 $O$ 记号给出上界。

---

# 平均情况复杂度

tags: 3.3 Complexity of Algorithms

hint:
平均情况复杂度为什么有时比最坏情况更贴近实际？

content:
平均情况时间复杂度衡量规模为 $n$ 的所有输入在某种概率分布下所需时间的平均值。

它适合描述随机或典型输入下的性能。

但平均情况分析依赖于输入分布假设；如果分布假设不合理，分析结果可能不能反映真实使用情况。

---

# 线性搜索复杂度

tags: 3.3 Complexity of Algorithms

hint:
线性搜索最坏情况下要检查多少个元素？

content:
线性搜索在最坏情况下需要检查整个列表。

若列表有 $n$ 个元素，则最坏情况下比较次数为：

$$
n
$$

因此线性搜索的最坏情况时间复杂度为：

$$
O(n)
$$

更精确地说，它具有线性复杂度：

$$
\Theta(n)
$$

---

# 二分搜索复杂度

tags: 3.3 Complexity of Algorithms

hint:
二分搜索为什么是对数复杂度？

content:
二分搜索每次比较后都会把候选范围大约减半。

若初始列表长度为 $n$，最多经过约：

$$
\lceil \log_2 n\rceil
$$

次减半后即可定位目标或确认不存在。

因此二分搜索的最坏情况时间复杂度为：

$$
O(\log n)
$$

也常写为：

$$
\Theta(\log n)
$$

---

# 冒泡排序复杂度

tags: 3.3 Complexity of Algorithms

hint:
冒泡排序需要进行多少轮相邻比较？

content:
冒泡排序在最坏情况下进行多轮相邻比较。

比较次数为：

$$
(n-1)+(n-2)+\cdots+1
$$

即：

$$
\frac{n(n-1)}{2}
$$

因此其最坏情况时间复杂度为：

$$
\Theta(n^2)
$$

---

# 插入排序复杂度

tags: 3.3 Complexity of Algorithms

hint:
插入排序最坏情况下，每个新元素要和多少前驱元素比较？

content:
插入排序在最坏情况下，第 $j$ 个元素可能需要与前面 $j-1$ 个已排序元素逐一比较。

总比较次数数量级为：

$$
1+2+\cdots+(n-1)
=
\frac{n(n-1)}{2}
$$

因此插入排序的最坏情况时间复杂度为：

$$
\Theta(n^2)
$$

---

# 矩阵乘法复杂度

tags: 3.3 Complexity of Algorithms

hint:
按定义相乘两个 $n\times n$ 矩阵需要多少乘法？

content:
按矩阵乘法定义计算两个 $n\times n$ 矩阵的乘积时，结果矩阵有：

$$
n^2
$$

个元素。

每个元素需要：

$$
n
$$

次乘法和：

$$
n-1
$$

次加法。

因此总乘法次数为：

$$
n^3
$$

总加法次数为：

$$
n^2(n-1)
$$

所以直接矩阵乘法的复杂度为：

$$
O(n^3)
$$

---

# 布尔矩阵乘法复杂度

tags: 3.3 Complexity of Algorithms

hint:
零一矩阵布尔积中，每个元素需要多少布尔操作？

content:
设 $A$ 和 $B$ 是 $n\times n$ 零一矩阵。

布尔积中每个元素可写为：

$$
(A\odot B)_{ij}
=
\bigvee_{q=1}^{n}(a_{iq}\land b_{qj})
$$

对每个元素，需要 $n$ 次 AND 和 $n$ 次 OR 级别的布尔操作。

结果矩阵有 $n^2$ 个元素，因此总操作数为：

$$
2n^3
$$

所以复杂度为：

$$
O(n^3)
$$

---

# 矩阵链乘法问题

tags: 3.3 Complexity of Algorithms

hint:
矩阵乘法有结合律，为什么乘法顺序仍然重要？

content:
矩阵乘法满足结合律，因此：

$$
A_1A_2\cdots A_n
$$

的最终结果不依赖于加括号方式。

但不同加括号方式会导致中间矩阵维数不同，从而使标量乘法次数差异很大。

若一个 $p\times q$ 矩阵乘以一个 $q\times r$ 矩阵，直接乘法需要：

$$
pqr
$$

次标量乘法。

因此矩阵链乘法问题的目标是选择加括号方式，使总乘法次数最少。

---

# 算法范式

tags: 3.3 Complexity of Algorithms

hint:
什么是 algorithmic paradigm？

content:
算法范式是构造算法的一般思路或方法框架。

常见算法范式包括：

1. 暴力法；
2. 贪心算法；
3. 分治法；
4. 动态规划；
5. 回溯法；
6. 概率算法。

算法范式帮助我们针对不同问题设计合适的算法结构。

---

# 暴力算法

tags: 3.3 Complexity of Algorithms

hint:
暴力算法为什么直观但常常低效？

content:
暴力算法按照问题陈述和定义，用最直接的方式解决问题，不利用特殊结构或巧妙性质。

常见暴力思路包括：

1. 检查所有可能解；
2. 逐一比较所有对象；
3. 按定义直接计算。

暴力算法通常容易设计和验证，但可能效率较低。

---

# 最近点对的暴力算法

tags: 3.3 Complexity of Algorithms

hint:
给定平面上 $n$ 个点，暴力法如何找最近点对？

content:
最近点对问题的暴力算法检查所有点对，并记录距离最小的一对。

若点为：

$$
(x_1,y_1),(x_2,y_2),\ldots,(x_n,y_n)
$$

则点对数量为：

$$
\frac{n(n-1)}{2}
$$

可比较平方距离：

$$
(x_j-x_i)^2+(y_j-y_i)^2
$$

而不必计算平方根。

因此暴力算法的复杂度为：

$$
\Theta(n^2)
$$

---

# 常见复杂度类型

tags: 3.3 Complexity of Algorithms

hint:
常见复杂度名称和 $\Theta$ 表达式怎样对应？

content:
常见复杂度类型包括：

| 表达式 | 名称 |
|---|---|
| $\Theta(1)$ | 常数复杂度 |
| $\Theta(\log n)$ | 对数复杂度 |
| $\Theta(n)$ | 线性复杂度 |
| $\Theta(n\log n)$ | 线性对数复杂度 |
| $\Theta(n^b)$ | 多项式复杂度 |
| $\Theta(b^n)$ | 指数复杂度 |
| $\Theta(n!)$ | 阶乘复杂度 |

其中 $b>1$。

---

# 多项式、指数与阶乘复杂度

tags: 3.3 Complexity of Algorithms

hint:
为什么指数和阶乘复杂度通常比多项式复杂度更危险？

content:
若算法复杂度为：

$$
\Theta(n^b)
$$

其中 $b\ge1$ 为整数，则称其具有多项式复杂度。

若复杂度为：

$$
\Theta(b^n),\quad b>1
$$

则称其具有指数复杂度。

若复杂度为：

$$
\Theta(n!)
$$

则称其具有阶乘复杂度。

当 $n$ 增大时，指数和阶乘增长通常远快于多项式增长，因此更容易在实际计算中变得不可承受。

---

# 可处理问题与难处理问题

tags: 3.3 Complexity of Algorithms

hint:
为什么多项式时间通常被看作可处理的分界线？

content:
若一个问题可以用最坏情况多项式时间算法解决，则称该问题可处理。

也就是说，存在算法，其最坏情况复杂度不超过：

$$
O(n^b)
$$

其中 $b$ 为常数。

若没有已知的最坏情况多项式时间算法，或者不能用多项式时间算法解决，则这类问题通常被认为难处理。

需要注意：多项式时间并不保证实际一定很快，因为高次多项式或巨大常数也可能导致运行时间很长。

---

# 不可解问题

tags: 3.3 Complexity of Algorithms

hint:
是否所有问题都存在算法？

content:
并非所有问题都能由算法解决。

若一个问题存在某个算法可以对所有合法输入给出正确答案，则称它是可解问题。

若不存在任何算法能够解决该问题，则称为不可解问题。

停机问题是经典不可解问题：不存在一个通用算法可以判断任意程序在任意输入下是否最终停止。

---

# 类 $P$

tags: 3.3 Complexity of Algorithms

hint:
哪些问题属于 $P$？

content:
类 $P$ 包含所有可以在多项式时间内求解的问题。

若存在某个算法能在最坏情况下使用：

$$
O(n^b)
$$

时间解决问题，其中 $b$ 是常数，则该问题属于 $P$。

$P$ 类问题通常被视为理论上可有效求解的问题。

---

# 类 $NP$

tags: 3.3 Complexity of Algorithms

hint:
$NP$ 问题的解不一定容易找到，但什么容易完成？

content:
类 $NP$ 包含那些给定一个候选解后，可以在多项式时间内验证该解是否正确的问题。

也就是说，对问题实例和候选证书，可以在多项式时间内检查其合法性。

显然：

$$
P\subseteq NP
$$

因为若一个问题能多项式时间求解，那么其解也能多项式时间验证。

---

# $NP$ 完全问题

tags: 3.3 Complexity of Algorithms

hint:
为什么 $NP$ 完全问题被看作 $NP$ 中最难的一类？

content:
$NP$ 完全问题是 $NP$ 中一类特殊问题，具有如下性质：

1. 它本身属于 $NP$；
2. 任意 $NP$ 问题都可以在多项式时间内归约到它。

因此，如果任一 $NP$ 完全问题存在多项式时间算法，则所有 $NP$ 问题都有多项式时间算法。

可满足性问题是最早被证明为 $NP$ 完全的问题之一。

---

# 可满足性问题的复杂度意义

tags: 3.3 Complexity of Algorithms

hint:
为什么 SAT 是算法复杂度理论中的核心问题？

content:
可满足性问题要求判断一个命题公式是否存在某种真值赋值，使公式为真。

若公式有 $n$ 个命题变量，暴力法需要检查：

$$
2^n
$$

种真值赋值。

因此暴力算法具有指数复杂度。

可满足性问题属于 $NP$，因为给定一个真值赋值后，可以在多项式时间内验证它是否使公式为真。

它也是 $NP$ 完全问题。

---

# 精确算法与近似算法

tags: 3.3 Complexity of Algorithms

hint:
遇到难处理问题时，为什么有时会接受近似解？

content:
对于某些难处理问题，精确求解可能需要过长时间。

在实际应用中，可以寻找近似算法。近似算法不一定返回最优解，但可能在较短时间内返回接近最优的解。

若近似算法能保证解与最优解的差距在一定范围内，则它在实际中可能非常有价值。

---

# 复杂度分析的核心目标

tags: 3.3 Complexity of Algorithms

hint:
分析算法复杂度最终是为了比较什么？

content:
复杂度分析的目标是比较不同算法在输入规模增大时的资源需求。

主要关注：

1. 运行时间如何随输入规模增长；
2. 存储空间如何随输入规模增长；
3. 最坏情况和平均情况表现如何；
4. 是否存在更高效的算法；
5. 问题本身是否可处理、难处理或不可解。

复杂度分析使我们能够在理论和实践上评价算法质量。

---

# 整除

tags: 4.1 Divisibility and Modular Arithmetic

hint:
$a$ 整除 $b$ 的意思是什么？

content:
设 $a,b$ 为整数，且：

$$
a\ne0
$$

若存在整数 $c$，使得：

$$
b=ac
$$

则称 $a$ 整除 $b$，记为：

$$
a\mid b
$$

若 $a$ 不整除 $b$，记为：

$$
a\nmid b
$$

当 $a\mid b$ 时，$a$ 称为 $b$ 的因子或除数，$b$ 称为 $a$ 的倍数。

---

# 整除的基本性质

tags: 4.1 Divisibility and Modular Arithmetic

hint:
整除关系对加法、乘法和传递有什么性质？

content:
设 $a,b,c$ 为整数，且 $a\ne0$。

若：

$$
a\mid b
$$

且：

$$
a\mid c
$$

则：

$$
a\mid(b+c)
$$

若：

$$
a\mid b
$$

则对任意整数 $c$，有：

$$
a\mid bc
$$

若：

$$
a\mid b
$$

且：

$$
b\mid c
$$

则：

$$
a\mid c
$$

---

# 整除与线性组合

tags: 4.1 Divisibility and Modular Arithmetic

hint:
若 $a$ 同时整除 $b$ 和 $c$，那么它还能整除什么？

content:
若 $a,b,c$ 为整数，$a\ne0$，并且：

$$
a\mid b,\qquad a\mid c
$$

则对任意整数 $m,n$，有：

$$
a\mid (mb+nc)
$$

也就是说，一个数若同时整除两个整数，就整除它们的任意整数线性组合。

---

# 除法算法

tags: 4.1 Divisibility and Modular Arithmetic

hint:
整数除以正整数时，商和余数怎样唯一确定？

content:
设 $a$ 为整数，$d$ 为正整数。则存在唯一的整数 $q$ 和 $r$，使得：

$$
a=dq+r
$$

并且：

$$
0\le r<d
$$

其中 $d$ 称为除数，$a$ 称为被除数，$q$ 称为商，$r$ 称为余数。

记号为：

$$
q=a\operatorname{div}d
$$

$$
r=a\bmod d
$$

---

# div 与 mod

tags: 4.1 Divisibility and Modular Arithmetic

hint:
$a\operatorname{div}d$ 和 $a\bmod d$ 分别表示什么？

content:
当 $a$ 是整数，$d$ 是正整数时：

$$
a\operatorname{div}d
$$

表示 $a$ 除以 $d$ 的商。

$$
a\bmod d
$$

表示 $a$ 除以 $d$ 的余数。

它们满足：

$$
a=d(a\operatorname{div}d)+(a\bmod d)
$$

并且：

$$
0\le a\bmod d<d
$$

一个整数 $a$ 能被 $d$ 整除，当且仅当：

$$
a\bmod d=0
$$

---

# 同余

tags: 4.1 Divisibility and Modular Arithmetic

hint:
两个整数模 $m$ 同余是什么意思？

content:
设 $a,b$ 为整数，$m$ 为正整数。

若：

$$
m\mid(a-b)
$$

则称 $a$ 与 $b$ 模 $m$ 同余，记为：

$$
a\equiv b\pmod m
$$

若 $a$ 与 $b$ 模 $m$ 不同余，记为：

$$
a\not\equiv b\pmod m
$$

这里 $m$ 称为模数。

---

# 同余与余数

tags: 4.1 Divisibility and Modular Arithmetic

hint:
同余是否等价于余数相同？

content:
设 $a,b$ 为整数，$m$ 为正整数。则：

$$
a\equiv b\pmod m
$$

当且仅当：

$$
a\bmod m=b\bmod m
$$

因此，两个整数模 $m$ 同余，等价于它们被 $m$ 除时有相同余数。

---

# 同余的加法与乘法性质

tags: 4.1 Divisibility and Modular Arithmetic

hint:
同余式能否相加和相乘？

content:
设 $m$ 为正整数。若：

$$
a\equiv b\pmod m
$$

且：

$$
c\equiv d\pmod m
$$

则：

$$
a+c\equiv b+d\pmod m
$$

并且：

$$
ac\equiv bd\pmod m
$$

特别地，若：

$$
a\equiv b\pmod m
$$

则对任意正整数 $k$，有：

$$
a^k\equiv b^k\pmod m
$$

---

# mod 函数的加法与乘法

tags: 4.1 Divisibility and Modular Arithmetic

hint:
先取余再相加、相乘，结果是否与最后取余一致？

content:
设 $m$ 为正整数，$a,b$ 为整数。则：

$$
(a+b)\bmod m=((a\bmod m)+(b\bmod m))\bmod m
$$

并且：

$$
ab\bmod m=((a\bmod m)(b\bmod m))\bmod m
$$

这说明在模运算中，可以先把参与运算的整数替换为其余数。

---

# 模 $m$ 加法与乘法

tags: 4.1 Divisibility and Modular Arithmetic

hint:
集合 $Z_m$ 上怎样定义加法和乘法？

content:
设：

$$
Z_m=\{0,1,\ldots,m-1\}
$$

模 $m$ 加法定义为：

$$
a+_m b=(a+b)\bmod m
$$

模 $m$ 乘法定义为：

$$
a\cdot_m b=(ab)\bmod m
$$

在这种运算下，我们说是在做模 $m$ 算术。

---

# 模 $m$ 算术的基本性质

tags: 4.1 Divisibility and Modular Arithmetic

hint:
$Z_m$ 上的加法和乘法有哪些类似普通整数运算的性质？

content:
在 $Z_m$ 上，模 $m$ 加法和乘法满足封闭性。

模 $m$ 加法满足结合律、交换律，且 $0$ 是加法单位元。

对每个 $a\in Z_m$，其加法逆元为：

$$
m-a
$$

其中 $a=0$ 时逆元仍为 $0$。

模 $m$ 乘法满足结合律、交换律，且 $1$ 是乘法单位元。

乘法对加法满足分配律。

---

# $b$ 进制展开

tags: 4.2 Integer Representations and Algorithms

hint:
任意正整数如何唯一写成 $b$ 的幂的线性组合？

content:
设 $b$ 是大于 $1$ 的整数。任意正整数 $n$ 都可以唯一表示为：

$$
n=a_kb^k+a_{k-1}b^{k-1}+\cdots+a_1b+a_0
$$

其中 $k$ 为非负整数，且：

$$
0\le a_i<b
$$

并且：

$$
a_k\ne0
$$

这种表示称为 $n$ 的 $b$ 进制展开，记为：

$$
(a_ka_{k-1}\cdots a_1a_0)_b
$$

---

# 二进制、八进制与十六进制

tags: 4.2 Integer Representations and Algorithms

hint:
计算机中常用哪些进制表示整数？

content:
以 $2$ 为基的展开称为二进制展开，其中每个数字只能是 $0$ 或 $1$。

以 $8$ 为基的展开称为八进制展开。

以 $16$ 为基的展开称为十六进制展开。

十六进制需要 $16$ 个数字，通常使用：

$$
0,1,2,3,4,5,6,7,8,9,A,B,C,D,E,F
$$

其中：

$$
A=10,\ B=11,\ C=12,\ D=13,\ E=14,\ F=15
$$

---

# 从十进制转换到 $b$ 进制

tags: 4.2 Integer Representations and Algorithms

hint:
反复除以 $b$ 时，余数怎样组成 $b$ 进制数字？

content:
把正整数 $n$ 转换为 $b$ 进制，可反复使用除法算法。

先写：

$$
n=bq_0+a_0
$$

其中 $a_0$ 是最右边的 $b$ 进制数字。

再写：

$$
q_0=bq_1+a_1
$$

其中 $a_1$ 是从右向左第二个数字。

继续把商除以 $b$，直到商为 $0$。所得余数从后向前排列，就是 $n$ 的 $b$ 进制展开。

---

# 构造 $b$ 进制展开的算法

tags: 4.2 Integer Representations and Algorithms

hint:
怎样用伪代码求整数的 $b$ 进制展开？

content:
构造 $b$ 进制展开的算法如下：

```text
procedure base_b_expansion(n, b)
    q := n
    k := 0
    while q != 0
        a_k := q mod b
        q := q div b
        k := k + 1
    return (a_(k-1), ..., a_1, a_0)
```

其中各个 $a_i$ 是从右向左得到的 $b$ 进制数字。

---

# 二进制与八进制、十六进制的快速转换

tags: 4.2 Integer Representations and Algorithms

hint:
为什么二进制和八进制、十六进制之间转换特别方便？

content:
因为：

$$
8=2^3
$$

每个八进制数字对应 $3$ 个二进制位。

因为：

$$
16=2^4
$$

每个十六进制数字对应 $4$ 个二进制位。

二进制转八进制时，从右向左每 $3$ 位分组，不足则在最左侧补 $0$。

二进制转十六进制时，从右向左每 $4$ 位分组，不足则在最左侧补 $0$。

反向转换时，将每个八进制数字替换为 $3$ 位二进制，将每个十六进制数字替换为 $4$ 位二进制。

---

# 二进制加法算法

tags: 4.2 Integer Representations and Algorithms

hint:
二进制加法怎样处理进位？

content:
设两个整数的二进制展开为：

$$
a=(a_{n-1}a_{n-2}\cdots a_1a_0)_2
$$

$$
b=(b_{n-1}b_{n-2}\cdots b_1b_0)_2
$$

从最低位开始逐位相加，并记录进位。

若当前进位为 $c$，则第 $j$ 位满足：

$$
a_j+b_j+c=2d+s_j
$$

其中 $s_j$ 是和的第 $j$ 位，$d$ 是新的进位。

该算法对 $n$ 位整数使用：

$$
O(n)
$$

次位操作。

---

# 二进制乘法算法

tags: 4.2 Integer Representations and Algorithms

hint:
普通乘法在二进制中如何转化为移位和加法？

content:
设：

$$
b=(b_{n-1}b_{n-2}\cdots b_1b_0)_2
$$

则：

$$
ab=a(b_0 2^0+b_1 2^1+\cdots+b_{n-1}2^{n-1})
$$

因此：

$$
ab=\sum_{j=0}^{n-1}ab_j2^j
$$

当 $b_j=1$ 时，部分积为 $a$ 左移 $j$ 位；当 $b_j=0$ 时，部分积为 $0$。

最后把所有部分积相加即可得到乘积。

传统二进制乘法的位操作复杂度为：

$$
O(n^2)
$$

---

# div 与 mod 的计算算法

tags: 4.2 Integer Representations and Algorithms

hint:
怎样通过反复减法计算商和余数？

content:
给定整数 $a$ 与正整数 $d$，可以通过反复减去 $d$ 来计算：

$$
a\operatorname{div}d
$$

和：

$$
a\bmod d
$$

基本伪代码如下：

```text
procedure division_algorithm(a, d)
    q := 0
    r := |a|
    while r >= d
        r := r - d
        q := q + 1
    if a < 0 and r > 0
        r := d - r
        q := -(q + 1)
    return (q, r)
```

返回值满足：

$$
a=dq+r,\qquad 0\le r<d
$$

---

# 模指数运算

tags: 4.2 Integer Representations and Algorithms

hint:
为什么不能先算巨大幂再取模？

content:
在密码学中，经常需要计算：

$$
b^n\bmod m
$$

当 $b,n,m$ 很大时，先计算 $b^n$ 再取模会产生极大的整数，消耗大量内存。

模指数运算的核心思想是：在每一步乘法或平方后立即取模，使中间结果始终保持在较小范围内。

---

# 快速模指数算法

tags: 4.2 Integer Representations and Algorithms

hint:
怎样利用指数的二进制展开快速计算 $b^n\bmod m$？

content:
设：

$$
n=(a_{k-1}a_{k-2}\cdots a_1a_0)_2
$$

则：

$$
n=\sum_{i=0}^{k-1}a_i2^i
$$

快速模指数算法维护两个变量：当前结果 $x$ 与当前幂 $power$。

伪代码：

```text
procedure modular_exponentiation(b, n, m)
    x := 1
    power := b mod m
    for i := 0 to k - 1
        if a_i = 1
            x := (x * power) mod m
        power := (power * power) mod m
    return x
```

该算法使用反复平方，能高效计算：

$$
b^n\bmod m
$$

---

# 素数与合数

tags: 4.3 Primes and Greatest Common Divisors

hint:
一个大于 $1$ 的正整数什么时候是 prime？

content:
大于 $1$ 的整数 $p$ 称为素数，当且仅当它的正因子只有：

$$
1
$$

和：

$$
p
$$

大于 $1$ 且不是素数的正整数称为合数。

整数 $1$ 既不是素数，也不是合数。

---

# 合数的因子判别

tags: 4.3 Primes and Greatest Common Divisors

hint:
判断合数时，只需要找什么样的因子？

content:
正整数 $n>1$ 是合数，当且仅当存在整数 $a$，使得：

$$
a\mid n
$$

并且：

$$
1<a<n
$$

也就是说，只要能找到一个非平凡因子，$n$ 就是合数。

---

# 算术基本定理

tags: 4.3 Primes and Greatest Common Divisors

hint:
每个正整数如何唯一分解成素数乘积？

content:
每个大于 $1$ 的正整数都可以写成素数的乘积。

并且，若把素因子按非降顺序排列，则这种分解是唯一的。

也就是说，每个正整数 $n>1$ 可唯一写成：

$$
n=p_1p_2\cdots p_k
$$

其中每个 $p_i$ 都是素数，且：

$$
p_1\le p_2\le\cdots\le p_k
$$

---

# 素因数分解

tags: 4.3 Primes and Greatest Common Divisors

hint:
如何表示一个整数中各个素因子的幂次？

content:
正整数 $n>1$ 的素因数分解常写为：

$$
n=p_1^{a_1}p_2^{a_2}\cdots p_s^{a_s}
$$

其中 $p_1,p_2,\ldots,p_s$ 是互不相同的素数，$a_i$ 是正整数。

这种形式清楚记录了每个素因子出现的次数。

---

# 试除法判别素数

tags: 4.3 Primes and Greatest Common Divisors

hint:
为什么检查因子只需要到 $\sqrt n$？

content:
若 $n$ 是合数，则存在整数 $a,b$，使得：

$$
n=ab
$$

并且：

$$
1<a\le b<n
$$

因此：

$$
a\le\sqrt n
$$

所以，若 $n$ 没有不超过 $\sqrt n$ 的素因子，则 $n$ 是素数。

这就是试除法判别素数的依据。

---

# 素数有无限多个

tags: 4.3 Primes and Greatest Common Divisors

hint:
欧几里得证明素数无穷多的核心构造是什么？

content:
素数有无限多个。

证明思想：假设只有有限多个素数：

$$
p_1,p_2,\ldots,p_n
$$

构造整数：

$$
Q=p_1p_2\cdots p_n+1
$$

则任意 $p_i$ 都不能整除 $Q$，因为 $Q$ 除以 $p_i$ 余数为 $1$。

所以 $Q$ 要么是新的素数，要么有不在列表中的素因子。两种情况都与“已经列出所有素数”矛盾。

---

# 梅森素数

tags: 4.3 Primes and Greatest Common Divisors

hint:
形如 $2^p-1$ 的素数叫什么？

content:
形如：

$$
2^p-1
$$

的素数称为梅森素数，其中 $p$ 本身必须是素数。

若 $2^n-1$ 是素数，则 $n$ 必须是素数。

但反过来不成立：$p$ 是素数并不保证 $2^p-1$ 是素数。

---

# 素数计数函数

tags: 4.3 Primes and Greatest Common Divisors

hint:
$\pi(x)$ 表示什么？

content:
素数计数函数记为：

$$
\pi(x)
$$

它表示不超过 $x$ 的素数个数。

例如，若要研究素数在整数中的分布，可以考察：

$$
\pi(x)
$$

随着 $x$ 增大如何增长。

---

# 素数定理

tags: 4.3 Primes and Greatest Common Divisors

hint:
不超过 $x$ 的素数个数大约是多少？

content:
素数定理说明：

$$
\pi(x)\sim \frac{x}{\ln x}
$$

也就是说，当 $x$ 很大时，不超过 $x$ 的素数个数大约为：

$$
\frac{x}{\ln x}
$$

因此，靠近 $n$ 的整数是素数的概率大约为：

$$
\frac{1}{\ln n}
$$

---

# 最大公约数

tags: 4.3 Primes and Greatest Common Divisors

hint:
两个整数的 greatest common divisor 是什么？

content:
设 $a,b$ 为整数，且不同时为 $0$。

能同时整除 $a$ 和 $b$ 的最大整数称为 $a$ 与 $b$ 的最大公约数，记为：

$$
\gcd(a,b)
$$

若：

$$
\gcd(a,b)=1
$$

则称 $a$ 与 $b$ 互素或相对素。

---

# 两两互素

tags: 4.3 Primes and Greatest Common Divisors

hint:
一组整数 pairwise relatively prime 是什么意思？

content:
若一组整数中任意两个不同整数都互素，则称这组整数两两互素。

也就是说，对所有 $i\ne j$，都有：

$$
\gcd(a_i,a_j)=1
$$

两两互素比“所有数的最大公约数为 $1$”更强。

---

# 最小公倍数

tags: 4.3 Primes and Greatest Common Divisors

hint:
两个正整数的 least common multiple 是什么？

content:
正整数 $a$ 与 $b$ 的最小公倍数是能同时被 $a$ 与 $b$ 整除的最小正整数，记为：

$$
\operatorname{lcm}(a,b)
$$

若 $a,b$ 是正整数，则有：

$$
ab=\gcd(a,b)\operatorname{lcm}(a,b)
$$

因此：

$$
\operatorname{lcm}(a,b)=\frac{ab}{\gcd(a,b)}
$$

---

# 用素因数分解求最大公约数和最小公倍数

tags: 4.3 Primes and Greatest Common Divisors

hint:
素因数分解中，最大公约数取小指数，最小公倍数取大指数。

content:
设：

$$
a=p_1^{\alpha_1}p_2^{\alpha_2}\cdots p_s^{\alpha_s}
$$

$$
b=p_1^{\beta_1}p_2^{\beta_2}\cdots p_s^{\beta_s}
$$

其中未出现的素因子指数视为 $0$。

则：

$$
\gcd(a,b)=\prod_{i=1}^{s}p_i^{\min(\alpha_i,\beta_i)}
$$

并且：

$$
\operatorname{lcm}(a,b)=\prod_{i=1}^{s}p_i^{\max(\alpha_i,\beta_i)}
$$

---

# 欧几里得算法

tags: 4.3 Primes and Greatest Common Divisors

hint:
怎样用连续取余快速求最大公约数？

content:
欧几里得算法利用以下事实：

$$
\gcd(a,b)=\gcd(b,a\bmod b)
$$

反复使用除法算法：

$$
a=bq+r
$$

把问题转化为更小的一对整数：

$$
(b,r)
$$

直到余数为 $0$。

最后一个非零余数就是原来两个数的最大公约数。

---

# 欧几里得算法伪代码

tags: 4.3 Primes and Greatest Common Divisors

hint:
求 $\gcd(a,b)$ 的循环算法怎样写？

content:
欧几里得算法可写为：

```text
procedure gcd(a, b)
    x := a
    y := b
    while y != 0
        r := x mod y
        x := y
        y := r
    return x
```

该算法返回：

$$
\gcd(a,b)
$$

---

# 贝祖定理

tags: 4.3 Primes and Greatest Common Divisors

hint:
最大公约数能否写成两个整数的线性组合？

content:
若 $a,b$ 为正整数，则存在整数 $s,t$，使得：

$$
\gcd(a,b)=sa+tb
$$

这称为贝祖定理。

整数 $s,t$ 称为贝祖系数。

贝祖定理说明，最大公约数是 $a$ 与 $b$ 的整数线性组合。

---

# 扩展欧几里得算法

tags: 4.3 Primes and Greatest Common Divisors

hint:
怎样在求最大公约数的同时求贝祖系数？

content:
扩展欧几里得算法在欧几里得算法求余的同时，记录每个余数作为原始两个整数的线性组合。

若欧几里得算法产生商：

$$
q_1,q_2,\ldots
$$

则可递推计算贝祖系数：

$$
s_j=s_{j-2}-q_{j-1}s_{j-1}
$$

$$
t_j=t_{j-2}-q_{j-1}t_{j-1}
$$

最终得到：

$$
\gcd(a,b)=sa+tb
$$

---

# 素数整除乘积引理

tags: 4.3 Primes and Greatest Common Divisors

hint:
素数整除一个乘积时，它必须整除哪个因子？

content:
若 $p$ 是素数，并且：

$$
p\mid ab
$$

则：

$$
p\mid a
$$

或：

$$
p\mid b
$$

更一般地，若素数 $p$ 整除有限多个整数的乘积，则 $p$ 至少整除其中一个因子。

---

# 同余式的可约性

tags: 4.3 Primes and Greatest Common Divisors

hint:
同余式两边什么时候可以除以同一个数？

content:
设 $m$ 为正整数，$a,b,c$ 为整数。

若：

$$
ac\equiv bc\pmod m
$$

且：

$$
\gcd(c,m)=1
$$

则：

$$
a\equiv b\pmod m
$$

也就是说，只有当被约去的因子与模数互素时，才可以安全地在同余式两边约去它。

---

# 模逆元

tags: 4.4 Solving Congruences

hint:
$a$ 模 $m$ 的逆元需要满足什么同余式？

content:
设 $a$ 与 $m$ 为整数，且 $m>1$。

若整数 $\overline{a}$ 满足：

$$
a\overline{a}\equiv1\pmod m
$$

则称 $\overline{a}$ 是 $a$ 模 $m$ 的逆元。

模逆元存在当且仅当：

$$
\gcd(a,m)=1
$$

若存在，则逆元在模 $m$ 意义下唯一。

---

# 用贝祖系数求模逆元

tags: 4.4 Solving Congruences

hint:
贝祖等式怎样给出模逆元？

content:
若：

$$
\gcd(a,m)=1
$$

则由贝祖定理，存在整数 $s,t$，使得：

$$
sa+tm=1
$$

两边对 $m$ 取模，得到：

$$
sa\equiv1\pmod m
$$

因此：

$$
s
$$

就是 $a$ 模 $m$ 的一个逆元。

扩展欧几里得算法可以用来求这个 $s$。

---

# 线性同余方程

tags: 4.4 Solving Congruences

hint:
形如 $ax\equiv b\pmod m$ 的方程怎样求解？

content:
线性同余方程形如：

$$
ax\equiv b\pmod m
$$

其中 $x$ 是整数变量。

若：

$$
\gcd(a,m)=1
$$

则 $a$ 有模 $m$ 逆元 $\overline{a}$，两边乘以 $\overline{a}$ 得：

$$
x\equiv \overline{a}b\pmod m
$$

这给出模 $m$ 意义下唯一的解。

---

# 线性同余方程的一般可解条件

tags: 4.4 Solving Congruences

hint:
当 $a$ 与 $m$ 不互素时，$ax\equiv b\pmod m$ 何时有解？

content:
设：

$$
d=\gcd(a,m)
$$

线性同余方程：

$$
ax\equiv b\pmod m
$$

有解当且仅当：

$$
d\mid b
$$

若有解，可把方程除以 $d$，化为：

$$
\frac{a}{d}x\equiv \frac{b}{d}\pmod{\frac{m}{d}}
$$

此时：

$$
\gcd\left(\frac{a}{d},\frac{m}{d}\right)=1
$$

可用模逆元求解。

---

# 中国剩余定理

tags: 4.4 Solving Congruences

hint:
多个模数两两互素时，线性同余组有怎样的唯一解？

content:
设：

$$
m_1,m_2,\ldots,m_n
$$

是两两互素且大于 $1$ 的正整数。对任意整数：

$$
a_1,a_2,\ldots,a_n
$$

同余方程组：

$$
x\equiv a_1\pmod {m_1}
$$

$$
x\equiv a_2\pmod {m_2}
$$

$$
\cdots
$$

$$
x\equiv a_n\pmod {m_n}
$$

在模：

$$
m=m_1m_2\cdots m_n
$$

意义下有唯一解。

---

# 中国剩余定理的构造解

tags: 4.4 Solving Congruences

hint:
怎样显式构造中国剩余定理的解？

content:
令：

$$
m=m_1m_2\cdots m_n
$$

并定义：

$$
M_k=\frac{m}{m_k}
$$

由于 $m_k$ 与 $M_k$ 互素，存在 $y_k$，使得：

$$
M_ky_k\equiv1\pmod {m_k}
$$

则同余方程组的一个解为：

$$
x=\sum_{k=1}^{n}a_kM_ky_k
$$

所有解都与该 $x$ 模 $m$ 同余。

---

# 费马小定理

tags: 4.4 Solving Congruences

hint:
素数模下，$a^{p-1}$ 有什么性质？

content:
若 $p$ 是素数，且整数 $a$ 不能被 $p$ 整除，则：

$$
a^{p-1}\equiv1\pmod p
$$

这是费马小定理。

等价地，对任意整数 $a$，都有：

$$
a^p\equiv a\pmod p
$$

费马小定理是快速模运算、素性测试和 RSA 正确性的基础之一。

---

# 伪素数

tags: 4.4 Solving Congruences

hint:
合数什么时候会伪装成素数通过费马测试？

content:
设 $b$ 为正整数。若合数 $n$ 满足：

$$
b^{n-1}\equiv1\pmod n
$$

则称 $n$ 是以 $b$ 为底的伪素数。

伪素数说明：满足费马小定理形式的同余式，并不一定能保证 $n$ 是素数。

---

# 卡迈克尔数

tags: 4.4 Solving Congruences

hint:
哪类合数会对所有互素底数都通过费马测试？

content:
合数 $n$ 称为卡迈克尔数，若对所有满足：

$$
\gcd(b,n)=1
$$

的正整数 $b$，都有：

$$
b^{n-1}\equiv1\pmod n
$$

卡迈克尔数是特殊的伪素数，会让简单费马素性测试失效。

---

# 原根

tags: 4.4 Solving Congruences

hint:
模素数 $p$ 下，哪个数的幂能生成所有非零剩余类？

content:
设 $p$ 是素数。若整数 $r$ 满足：每个不能被 $p$ 整除的整数都与某个 $r$ 的幂模 $p$ 同余，则称 $r$ 是模 $p$ 的原根。

也就是说，集合：

$$
r^0,r^1,\ldots,r^{p-2}
$$

在模 $p$ 意义下给出所有非零剩余类。

---

# 离散对数

tags: 4.4 Solving Congruences

hint:
普通对数的模运算版本是什么？

content:
设 $p$ 是素数，$r$ 是模 $p$ 的原根。

若：

$$
r^e\equiv a\pmod p
$$

且：

$$
0\le e\le p-2
$$

则称 $e$ 是 $a$ 以 $r$ 为底、模 $p$ 的离散对数。

离散对数问题是：给定 $p,r,a$，求满足上述同余式的 $e$。

一般情况下，离散对数问题被认为很难，这使它成为密码学中的重要基础。

---

# 哈希函数

tags: 4.5 Applications of Congruences

hint:
如何用同余把大编号映射到较小的存储位置？

content:
哈希函数把键 $k$ 映射到存储位置。

常见形式为：

$$
h(k)=k\bmod m
$$

其中 $m$ 是可用存储位置的数量。

这种函数容易计算，并且输出总在：

$$
\{0,1,\ldots,m-1\}
$$

中。

不同键可能映射到同一位置，这称为冲突。

---

# 双重哈希

tags: 4.5 Applications of Congruences

hint:
发生哈希冲突时，怎样用第二个哈希函数决定探测序列？

content:
双重哈希用两个哈希函数处理冲突。

常见设置为：

$$
h(k)=k\bmod p
$$

其中 $p$ 为素数。

第二个哈希函数可取：

$$
g(k)=(k+1)\bmod(p-2)
$$

发生冲突时，使用探测序列：

$$
h(k,i)=(h(k)+i\cdot g(k))\bmod p
$$

通过改变 $i$ 查找下一个可用位置。

---

# 线性同余伪随机数生成器

tags: 4.5 Applications of Congruences

hint:
怎样用递推同余式生成伪随机数？

content:
线性同余生成器用递推式：

$$
x_{n+1}=(ax_n+c)\bmod m
$$

生成伪随机数序列。

其中：

$m$ 称为模数；

$a$ 称为乘数；

$c$ 称为增量；

$x_0$ 称为种子。

若 $c=0$，则称为纯乘法生成器：

$$
x_{n+1}=ax_n\bmod m
$$

---

# 校验位

tags: 4.5 Applications of Congruences

hint:
为什么识别码末尾常加一位校验数字？

content:
校验位用于检测识别号码中的常见错误。

基本思想是：给定前若干位数字，选择最后一位，使整个数字串满足某个同余条件。

录入或印刷出错后，该同余条件通常会被破坏，从而可以检测错误。

校验位常用于商品码、书号、票号、账号等编号系统。

---

# UPC 校验位

tags: 4.5 Applications of Congruences

hint:
UPC 的 12 位数字满足什么同余条件？

content:
UPC 通常包含 $12$ 个十进制数字：

$$
x_1x_2\cdots x_{12}
$$

最后一位 $x_{12}$ 是校验位，要求：

$$
3x_1+x_2+3x_3+x_4+\cdots+3x_{11}+x_{12}\equiv0\pmod {10}
$$

也就是说，奇数位置权重为 $3$，偶数位置权重为 $1$，加权和必须能被 $10$ 整除。

---

# ISBN-10 校验位

tags: 4.5 Applications of Congruences

hint:
ISBN-10 的校验位为什么可能是 $X$？

content:
ISBN-10 是 $10$ 位编码：

$$
x_1x_2\cdots x_{10}
$$

最后一位 $x_{10}$ 是校验位，要求：

$$
\sum_{i=1}^{10}ix_i\equiv0\pmod {11}
$$

等价地：

$$
x_{10}\equiv\sum_{i=1}^{9}ix_i\pmod {11}
$$

因为模 $11$ 的余数可能是 $10$，所以用字母 $X$ 表示校验位 $10$。

---

# ISBN-10 的错误检测

tags: 4.5 Applications of Congruences

hint:
ISBN-10 校验位可以检测哪些常见错误？

content:
ISBN-10 的校验规则：

$$
\sum_{i=1}^{10}ix_i\equiv0\pmod {11}
$$

可以检测任意单个数字错误。

它也可以检测任意两个不同数字的换位错误。

原因是模数 $11$ 为素数，并且位置权重：

$$
1,2,\ldots,10
$$

在模 $11$ 下都非零且两两不同。

---

# 加密与解密

tags: 4.6 Cryptography

hint:
加密和解密分别做什么？

content:
加密是把明文消息变换成不易被未授权者理解的密文。

解密是把密文恢复为原始明文。

若用 $p$ 表示明文字母或明文块，用 $c$ 表示密文字母或密文块，则加密可抽象为：

$$
c=E(p)
$$

解密可抽象为：

$$
p=D(c)
$$

并要求：

$$
D(E(p))=p
$$

---

# 凯撒密码

tags: 4.6 Cryptography

hint:
把每个字母向后平移 $3$ 位怎样用模运算表示？

content:
先把字母转换为：

$$
Z_{26}=\{0,1,\ldots,25\}
$$

中的数字，其中 $A=0$，$B=1$，依此类推。

凯撒密码把明文字母 $p$ 加密为：

$$
f(p)=(p+3)\bmod 26
$$

解密时使用：

$$
f^{-1}(p)=(p-3)\bmod 26
$$

---

# 移位密码

tags: 4.6 Cryptography

hint:
凯撒密码怎样推广为任意平移量？

content:
移位密码使用密钥：

$$
k\in Z_{26}
$$

将明文字母 $p$ 加密为：

$$
c=(p+k)\bmod 26
$$

解密为：

$$
p=(c-k)\bmod 26
$$

移位密码是字符密码，也是私钥密码，因为知道加密密钥即可快速得到解密方法。

---

# 仿射密码

tags: 4.6 Cryptography

hint:
在字母编号上做一次线性变换，什么时候可解密？

content:
仿射密码将明文字母 $p$ 加密为：

$$
c=(ap+b)\bmod 26
$$

其中必须满足：

$$
\gcd(a,26)=1
$$

这样 $a$ 在模 $26$ 下存在逆元，才能解密。

若 $\overline{a}$ 是 $a$ 模 $26$ 的逆元，则解密为：

$$
p=\overline{a}(c-b)\bmod 26
$$

---

# 块密码

tags: 4.6 Cryptography

hint:
字符密码和块密码有什么区别？

content:
字符密码一次加密一个字符。

块密码一次加密固定长度的字符块。

例如，排列密码可以把明文分成固定长度的块，然后按某个排列重新排列块内字符。

块密码通常比单字符替换更灵活，也更适合现代加密系统。

---

# 密码分析

tags: 4.6 Cryptography

hint:
不知道密钥时尝试恢复明文的过程叫什么？

content:
密码分析是在不知道解密密钥，或只知道部分加密方法的情况下，试图从密文恢复明文的过程。

简单的古典密码，如移位密码和仿射密码，通常容易受到频率分析等方法攻击。

现代密码系统的目标是使密码分析在实际计算资源下不可行。

---

# 密码系统

tags: 4.6 Cryptography

hint:
一个 cryptosystem 由哪五个部分组成？

content:
密码系统是五元组：

$$
(\mathcal{P},\mathcal{C},\mathcal{K},\mathcal{E},\mathcal{D})
$$

其中：

$\mathcal{P}$ 是明文消息集合；

$\mathcal{C}$ 是密文消息集合；

$\mathcal{K}$ 是密钥空间；

$\mathcal{E}$ 是加密函数集合；

$\mathcal{D}$ 是解密函数集合。

对密钥 $k$，加密函数记为：

$$
E_k
$$

相应解密函数记为：

$$
D_k
$$

并要求：

$$
D_k(E_k(p))=p
$$

---

# 私钥密码系统

tags: 4.6 Cryptography

hint:
为什么移位密码属于私钥密码？

content:
私钥密码系统中，加密密钥和解密密钥都必须保密。

通信双方需要事先共享密钥。

若攻击者知道私钥，就能加密和解密消息。

移位密码和仿射密码都是私钥密码系统，因为知道加密密钥就能快速得到解密方式。

---

# 公钥密码系统

tags: 4.6 Cryptography

hint:
公钥密码怎样避免每一对通信者预先共享密钥？

content:
公钥密码系统中，加密密钥公开，解密密钥保密。

任何人都可以使用接收者的公钥加密消息，但只有拥有私钥的接收者能高效解密。

公钥密码的安全性依赖于：从公开加密信息恢复私钥或明文在计算上极其困难。

RSA 是经典公钥密码系统。

---

# RSA 密钥生成

tags: 4.6 Cryptography

hint:
RSA 的公钥和私钥怎样由两个大素数得到？

content:
RSA 选择两个大素数：

$$
p,\quad q
$$

令：

$$
n=pq
$$

选择整数 $e$，满足：

$$
\gcd(e,(p-1)(q-1))=1
$$

公钥为：

$$
(n,e)
$$

再求 $e$ 模：

$$
(p-1)(q-1)
$$

的逆元 $d$，即：

$$
ed\equiv1\pmod{(p-1)(q-1)}
$$

私钥为 $d$，同时需要保密素因子 $p,q$。

---

# RSA 加密

tags: 4.6 Cryptography

hint:
RSA 如何把明文块变成密文块？

content:
RSA 先把明文消息转换为小于 $n$ 的整数块：

$$
m_1,m_2,\ldots,m_k
$$

对每个明文块 $m$，使用公钥：

$$
(n,e)
$$

加密为：

$$
c=m^e\bmod n
$$

实际计算时使用快速模指数算法，而不是先计算巨大幂。

---

# RSA 解密

tags: 4.6 Cryptography

hint:
RSA 解密为什么要用 $d$ 次幂？

content:
RSA 解密使用私钥 $d$。对密文块 $c$，计算：

$$
m=c^d\bmod n
$$

其中 $d$ 满足：

$$
ed\equiv1\pmod{(p-1)(q-1)}
$$

若：

$$
c\equiv m^e\pmod n
$$

则有：

$$
c^d\equiv m\pmod n
$$

这保证了正确解密。

---

# RSA 的安全基础

tags: 4.6 Cryptography

hint:
为什么公开 $n$ 和 $e$ 后，攻击者仍然难以解密？

content:
RSA 的安全性依赖于大整数分解的困难性。

公钥中公开：

$$
n=pq
$$

但若 $p$ 和 $q$ 足够大，已知 $n$ 要分解出 $p,q$ 在经典计算下非常困难。

若攻击者能分解 $n$，就能计算：

$$
(p-1)(q-1)
$$

并求出私钥 $d$。

因此，RSA 的安全核心是：乘两个大素数容易，反过来分解它们的乘积困难。

---

# 密钥交换协议

tags: 4.6 Cryptography

hint:
两方如何在不安全信道上生成共享密钥？

content:
密钥交换协议用于让通信双方在不安全信道上生成共享密钥。

典型思想是：双方公开某些计算结果，但各自保留私密指数或私密信息，使得双方能算出同一个共享密钥，而窃听者难以恢复该密钥。

Diffie-Hellman 密钥交换依赖于离散对数问题的困难性。

---

# Diffie-Hellman 密钥交换

tags: 4.6 Cryptography

hint:
双方各自保留指数，如何得到同一个共享密钥？

content:
选定素数 $p$ 和模 $p$ 的原根 $\alpha$。

Alice 选择秘密整数 $a$，发送：

$$
\alpha^a\bmod p
$$

Bob 选择秘密整数 $b$，发送：

$$
\alpha^b\bmod p
$$

Alice 计算：

$$
(\alpha^b)^a\bmod p
$$

Bob 计算：

$$
(\alpha^a)^b\bmod p
$$

二者得到相同共享密钥：

$$
\alpha^{ab}\bmod p
$$

攻击者若只能看到公开值，需要解决离散对数问题才能恢复共享密钥。

---

# 数字签名

tags: 4.6 Cryptography

hint:
RSA 中怎样证明消息确实来自发送者？

content:
数字签名用于让接收者确认消息确实由声称的发送者发出。

在 RSA 中，发送者可用自己的私钥变换消息块，相当于对消息签名。

接收者再用发送者的公钥验证并恢复原消息。

若验证成功，说明只有拥有相应私钥的人才能生成该签名。

---

# 签名后加密

tags: 4.6 Cryptography

hint:
怎样同时保证来源可信和内容保密？

content:
为了同时实现签名和保密，发送者可以先用自己的私钥对消息签名，再用接收者的公钥加密签名后的结果。

接收者收到后，先用自己的私钥解密，再用发送者的公钥验证签名并恢复消息。

这一过程同时提供：

1. 机密性；
2. 发送者认证；
3. 消息完整性。

---

# 同态加密

tags: 4.6 Cryptography

hint:
能否在不解密的情况下对密文进行计算？

content:
同态加密允许在加密数据上进行某些计算，使得计算结果解密后等于明文上相应计算的结果。

若密码系统支持任意计算，则称为全同态加密。

全同态加密的目标是：无需解密数据，也能在远程系统上运行程序并得到加密形式的正确输出。

---

# RSA 的乘法同态性

tags: 4.6 Cryptography

hint:
RSA 加密怎样与明文乘法相容？

content:
设 RSA 公钥为：

$$
(n,e)
$$

加密函数为：

$$
E(M)=M^e\bmod n
$$

则对明文 $M_1,M_2$，有：

$$
E(M_1)E(M_2)\bmod n
=
(M_1^eM_2^e)\bmod n
$$

因此：

$$
E(M_1)E(M_2)\bmod n
=
E(M_1M_2)
$$

这说明 RSA 具有乘法同态性。

RSA 不是全同态密码系统。

---

# 数学归纳法

tags: 5.1 Mathematical Induction

hint:
证明对所有正整数成立的命题时，为什么只需证明起点和递推？

content:
数学归纳法用于证明形如：

$$
\forall n\,P(n)
$$

的命题，其中论域通常是正整数。

证明分为两步：

1. 基础步：证明 $P(1)$ 为真；
2. 归纳步：证明对任意正整数 $k$，若 $P(k)$ 为真，则 $P(k+1)$ 为真。

形式化地，如果：

$$
P(1)
$$

为真，且：

$$
\forall k(P(k)\to P(k+1))
$$

为真，则：

$$
\forall n\,P(n)
$$

为真。

---

# 基础步

tags: 5.1 Mathematical Induction

hint:
归纳证明中必须先证明哪个最初情况？

content:
基础步是数学归纳法中的第一步，用来证明命题在起始整数处成立。

若要证明：

$$
P(n)
$$

对所有正整数 $n$ 成立，则基础步通常是证明：

$$
P(1)
$$

若要证明：

$$
P(n)
$$

对所有整数 $n\ge b$ 成立，则基础步应证明：

$$
P(b)
$$

基础步不能省略，否则归纳链没有起点。

---

# 归纳假设

tags: 5.1 Mathematical Induction

hint:
归纳步中，临时假设的命题是什么？

content:
在归纳步中，先假设命题对某个任意但固定的整数 $k$ 成立。

这个假设称为归纳假设：

$$
P(k)
$$

归纳假设不是假设结论已经全部成立，而是在证明条件命题：

$$
P(k)\to P(k+1)
$$

时临时假设前件成立。

---

# 归纳步

tags: 5.1 Mathematical Induction

hint:
归纳步真正要证明的是哪个蕴含式？

content:
归纳步要证明：如果命题对 $k$ 成立，则命题对 $k+1$ 成立。

即证明：

$$
P(k)\to P(k+1)
$$

其中 $k$ 必须是任意允许范围内的整数。

归纳步的常见结构为：

1. 设 $k$ 为任意满足条件的整数；
2. 假设 $P(k)$ 成立；
3. 利用归纳假设推出 $P(k+1)$；
4. 得到 $P(k)\to P(k+1)$。

---

# 数学归纳法的阶梯模型

tags: 5.1 Mathematical Induction

hint:
为什么“能上第一阶”和“能从第 $k$ 阶到第 $k+1$ 阶”足够？

content:
数学归纳法可用无限阶梯理解。

若能到达第一阶，并且对任意正整数 $k$，只要能到达第 $k$ 阶，就能到达第 $k+1$ 阶，则可以到达每一阶。

对应到命题：

$$
P(1)
$$

表示第一阶可达；

$$
P(k)\to P(k+1)
$$

表示能从第 $k$ 阶走到第 $k+1$ 阶；

于是：

$$
\forall n\,P(n)
$$

成立。

---

# 数学归纳法的多米诺模型

tags: 5.1 Mathematical Induction

hint:
归纳法和推倒无限排多米诺有什么对应关系？

content:
数学归纳法也可用无限排多米诺理解。

若第一个多米诺被推倒，并且每个第 $k$ 个多米诺倒下都会推倒第 $k+1$ 个，则所有多米诺都会倒下。

对应关系为：

第一个多米诺倒下：

$$
P(1)
$$

第 $k$ 个倒下推出第 $k+1$ 个倒下：

$$
P(k)\to P(k+1)
$$

所有多米诺倒下：

$$
\forall n\,P(n)
$$

---

# 数学归纳法的有效性

tags: 5.1 Mathematical Induction

hint:
归纳法为什么可靠？它依赖于正整数的什么性质？

content:
数学归纳法的有效性可由良序性解释。

若 $P(1)$ 为真，且对所有正整数 $k$ 都有：

$$
P(k)\to P(k+1)
$$

但仍存在某个正整数 $n$ 使 $P(n)$ 为假，则所有反例组成的集合非空。

由良序性，这个反例集合有最小元素 $m$。

因为 $P(1)$ 为真，所以：

$$
m\ne1
$$

于是 $m-1$ 是正整数，并且小于 $m$，所以 $P(m-1)$ 为真。

由归纳步：

$$
P(m-1)\to P(m)
$$

可得 $P(m)$ 为真，与 $m$ 是反例矛盾。

因此不存在反例，命题对所有正整数成立。

---

# 从非 1 起点开始的归纳法

tags: 5.1 Mathematical Induction

hint:
若命题从 $n=b$ 才开始成立，应如何修改基础步？

content:
若要证明：

$$
P(n)
$$

对所有整数：

$$
n\ge b
$$

成立，只需把基础步改为证明：

$$
P(b)
$$

归纳步改为证明对所有：

$$
k\ge b
$$

都有：

$$
P(k)\to P(k+1)
$$

其中 $b$ 可以是负整数、零或正整数。

---

# 归纳证明求和公式

tags: 5.1 Mathematical Induction

hint:
用归纳法证明求和公式时，通常怎样从 $k$ 推到 $k+1$？

content:
证明求和公式时，归纳步通常从 $P(k)$ 的等式出发，在两边同时加上第 $k+1$ 项。

例如若要证明：

$$
\sum_{j=1}^{n}a_j=F(n)
$$

归纳假设为：

$$
\sum_{j=1}^{k}a_j=F(k)
$$

则归纳步中考察：

$$
\sum_{j=1}^{k+1}a_j=\sum_{j=1}^{k}a_j+a_{k+1}
$$

再用归纳假设化为：

$$
F(k)+a_{k+1}
$$

最后证明它等于：

$$
F(k+1)
$$

---

# 等比求和公式的归纳结构

tags: 5.1 Mathematical Induction

hint:
有限等比和如何用归纳法证明？

content:
有限等比求和公式为：

$$
\sum_{j=0}^{n}ar^j=\frac{ar^{n+1}-a}{r-1}
$$

其中：

$$
r\ne1
$$

归纳证明时，基础步验证 $n=0$：

$$
\sum_{j=0}^{0}ar^j=a
$$

归纳步中从：

$$
\sum_{j=0}^{k}ar^j=\frac{ar^{k+1}-a}{r-1}
$$

出发，两边加上：

$$
ar^{k+1}
$$

并化简得到：

$$
\sum_{j=0}^{k+1}ar^j=\frac{ar^{k+2}-a}{r-1}
$$

---

# 归纳证明不等式

tags: 5.1 Mathematical Induction

hint:
归纳证明不等式时，怎样保证方向不被破坏？

content:
用数学归纳法证明不等式时，归纳步通常从归纳假设给出的不等式出发，再进行保持不等号方向的变形。

常见做法包括：

1. 在不等式两边加同一个数；
2. 两边乘以同一个正数；
3. 用更强的下界或上界替换某些项；
4. 把目标式分解成已知部分和新增部分。

必须注意：乘以负数会改变不等号方向，不能随意操作。

---

# 归纳证明整除性

tags: 5.1 Mathematical Induction

hint:
怎样用归纳法证明某表达式总能被某数整除？

content:
证明整除性命题时，目标通常形如：

$$
d\mid f(n)
$$

归纳步中假设：

$$
d\mid f(k)
$$

也就是存在整数 $c$ 使：

$$
f(k)=dc
$$

然后把 $f(k+1)$ 写成：

$$
f(k+1)=f(k)+d\cdot h(k)
$$

或其他明显含有 $d$ 因子的形式，从而推出：

$$
d\mid f(k+1)
$$

---

# 归纳证明集合大小

tags: 5.1 Mathematical Induction

hint:
像“$n$ 元集合有 $2^n$ 个子集”这类命题怎么归纳？

content:
涉及集合大小的归纳证明常通过比较 $n$ 元集合与 $n+1$ 元集合实现。

例如，若已知任意 $k$ 元集合有：

$$
2^k
$$

个子集，则对一个 $k+1$ 元集合，固定其中一个元素 $a$。

它的子集分成两类：

1. 不含 $a$ 的子集；
2. 含 $a$ 的子集。

这两类数量相等，各为：

$$
2^k
$$

所以总数为：

$$
2^k+2^k=2^{k+1}
$$

---

# 错误归纳证明的常见问题

tags: 5.1 Mathematical Induction

hint:
归纳证明看似合理但错误，常见漏洞在哪里？

content:
错误归纳证明常见漏洞包括：

1. 基础步没有覆盖归纳步需要的起点；
2. 归纳步只证明了某些特殊 $k$；
3. 从 $P(k)$ 推不出 $P(k+1)$；
4. 偷偷使用了尚未证明的 $P(k+1)$；
5. 在某个边界情况中公式或操作不成立；
6. 强归纳中使用了不存在或未覆盖的前项。

归纳证明必须确保每一环都能从已知条件严格推出下一环。

---

# 强归纳法

tags: 5.2 Strong Induction and Well-Ordering

hint:
强归纳法和普通归纳法相比，归纳假设更强在哪里？

content:
强归纳法用于证明：

$$
\forall n\,P(n)
$$

它的结构为：

1. 基础步：证明 $P(1)$；
2. 强归纳步：证明对任意正整数 $k$，若 $P(1),P(2),\ldots,P(k)$ 都为真，则 $P(k+1)$ 为真。

形式化地，若：

$$
P(1)
$$

为真，且：

$$
\forall k((P(1)\land P(2)\land\cdots\land P(k))\to P(k+1))
$$

为真，则：

$$
\forall n\,P(n)
$$

为真。

---

# 强归纳法的使用场景

tags: 5.2 Strong Induction and Well-Ordering

hint:
什么时候普通归纳不够自然，强归纳更合适？

content:
当证明 $P(k+1)$ 需要用到不止 $P(k)$，而需要用到多个更小情形时，强归纳法更自然。

典型情形包括：

1. 递推关系依赖多个前项；
2. 证明数的分解性质；
3. 证明算法递归正确性；
4. 对象由较小对象组合而成；
5. 每一步可能从 $k+1$ 跳回某个远小于 $k$ 的值。

强归纳法允许使用所有已知较小规模的命题。

---

# 强归纳的基础步数量

tags: 5.2 Strong Induction and Well-Ordering

hint:
强归纳是否总是只需要一个基础步？

content:
强归纳有时需要多个基础步。

若归纳步要用到前面若干项，例如要从 $P(k-1)$ 推出 $P(k+1)$，则必须保证这些前项在最小情况中已经被证明。

例如若递推依赖前两项，常需要证明：

$$
P(1)
$$

和：

$$
P(2)
$$

再证明：

$$
(P(1)\land\cdots\land P(k))\to P(k+1)
$$

基础步的数量应覆盖归纳步的最早使用范围。

---

# 普通归纳与强归纳的等价性

tags: 5.2 Strong Induction and Well-Ordering

hint:
强归纳是否真的比普通归纳更强？

content:
强归纳法和普通数学归纳法在逻辑上等价。

强归纳看起来使用了更强的归纳假设，但它可以由普通归纳证明；普通归纳也可以看作强归纳的特殊情形。

实际使用中，强归纳常让证明更自然，尤其适合需要多个前驱情形的命题。

---

# 良序性

tags: 5.2 Strong Induction and Well-Ordering

hint:
非空的非负整数集合一定有什么元素？

content:
良序性说明：每个非空的非负整数集合都有最小元素。

若：

$$
S\subseteq\mathbb{N}
$$

且：

$$
S\ne\varnothing
$$

则存在：

$$
m\in S
$$

使得对所有 $s\in S$，都有：

$$
m\le s
$$

良序性是许多反证和最小反例证明的基础。

---

# 最小反例法

tags: 5.2 Strong Induction and Well-Ordering

hint:
如何用“最小反例”证明一个全称命题？

content:
最小反例法依赖良序性。

要证明：

$$
\forall n\,P(n)
$$

可以反设存在反例，即存在 $n$ 使 $\neg P(n)$ 成立。

所有反例组成非空集合：

$$
S=\{n\mid \neg P(n)\}
$$

由良序性，$S$ 有最小元素 $m$。

接着利用 $m$ 的最小性说明所有比 $m$ 小的相关情形都满足 $P$，再推出 $P(m)$，从而矛盾。

---

# 良序性与强归纳

tags: 5.2 Strong Induction and Well-Ordering

hint:
良序性怎样保证强归纳法有效？

content:
强归纳法的有效性可由良序性证明。

若基础步成立，且强归纳步成立，但仍存在反例，则由良序性存在最小反例 $m$。

因为 $m$ 是最小反例，所有比 $m$ 小的正整数都满足命题。

强归纳步于是推出：

$$
P(m)
$$

成立，这与 $m$ 是反例矛盾。

因此不存在反例，命题对所有正整数成立。

---

# 递归

tags: 5.3 Recursive Definitions and Structural Induction

hint:
什么叫用对象自身来定义对象？

content:
递归是用对象自身或较小规模的同类对象来定义对象的方法。

递归定义通常包含两部分：

1. 基础步：直接指定最简单对象；
2. 递归步：说明如何由已有对象构造新对象。

递归可用于定义序列、函数、集合、字符串、树和算法。

---

# 递归定义的函数

tags: 5.3 Recursive Definitions and Structural Induction

hint:
递归定义函数时，需要给出哪些内容？

content:
对非负整数定义域上的函数进行递归定义时，通常需要两部分：

基础步：指定函数在 $0$ 处的值：

$$
f(0)
$$

递归步：说明怎样用较小整数处的函数值计算较大整数处的函数值。

例如：

$$
f(n+1)=G(f(n),n)
$$

配合初值 $f(0)$，可以唯一确定整个函数。

---

# 递归定义的阶乘

tags: 5.3 Recursive Definitions and Structural Induction

hint:
$n!$ 的递归定义是什么？

content:
阶乘函数可递归定义为：

$$
0!=1
$$

并且对非负整数 $n$：

$$
(n+1)!=(n+1)n!
$$

基础步给出 $0!$，递归步用 $n!$ 定义 $(n+1)!$。

---

# 递归定义的幂

tags: 5.3 Recursive Definitions and Structural Induction

hint:
$a^n$ 如何用递归方式定义？

content:
设 $a$ 为非零实数。幂函数可递归定义为：

$$
a^0=1
$$

并且对非负整数 $n$：

$$
a^{n+1}=a\cdot a^n
$$

这种定义直接对应递归算法中计算幂的方式。

---

# 递归定义的序列

tags: 5.3 Recursive Definitions and Structural Induction

hint:
递推关系怎样定义序列？

content:
序列可以通过初始条件和递推关系递归定义。

例如，若给出：

$$
a_0=c
$$

以及：

$$
a_{n+1}=F(a_n)
$$

则所有项 $a_0,a_1,a_2,\ldots$ 都被确定。

若递推关系依赖多个前项，则需要给出足够多个初始条件。

---

# 斐波那契数列

tags: 5.3 Recursive Definitions and Structural Induction

hint:
每一项等于前两项之和的经典数列怎样定义？

content:
斐波那契数列递归定义为：

$$
f_0=0
$$

$$
f_1=1
$$

并且对 $n\ge2$：

$$
f_n=f_{n-1}+f_{n-2}
$$

每一项由前两项决定，因此需要两个初始条件。

---

# 递归定义集合

tags: 5.3 Recursive Definitions and Structural Induction

hint:
集合如何通过初始元素和生成规则来定义？

content:
递归定义集合时，通常包含：

基础步：指定某些初始元素属于集合；

递归步：说明如果某些对象已经在集合中，那么怎样由它们构造新的集合元素。

递归定义还隐含排除原则：只有通过基础步和有限次递归步得到的元素才属于该集合。

---

# 递归定义字符串集合

tags: 5.3 Recursive Definitions and Structural Induction

hint:
字母表上的所有字符串如何递归生成？

content:
设字母表为 $\Sigma$。所有由 $\Sigma$ 中符号构成的字符串集合记为：

$$
\Sigma^*
$$

其递归定义为：

基础步：

$$
\lambda\in\Sigma^*
$$

其中 $\lambda$ 是空字符串。

递归步：若：

$$
w\in\Sigma^*
$$

且：

$$
x\in\Sigma
$$

则：

$$
wx\in\Sigma^*
$$

也就是说，可以在已有字符串末尾附加一个字母表符号生成新字符串。

---

# 字符串长度的递归定义

tags: 5.3 Recursive Definitions and Structural Induction

hint:
字符串长度怎样随末尾添加一个符号而变化？

content:
字符串长度函数 $l(w)$ 可递归定义为：

$$
l(\lambda)=0
$$

若：

$$
w\in\Sigma^*
$$

且：

$$
x\in\Sigma
$$

则：

$$
l(wx)=l(w)+1
$$

空字符串长度为 $0$，每在末尾添加一个符号，长度增加 $1$。

---

# 字符串连接的递归定义

tags: 5.3 Recursive Definitions and Structural Induction

hint:
连接字符串时，怎样递归地把第二个字符串的符号附加到第一个字符串后面？

content:
设 $w_1,w_2\in\Sigma^*$。字符串连接可递归定义为：

基础步：

$$
w\lambda=w
$$

递归步：若 $x\in\Sigma$，则：

$$
w_1(w_2x)=(w_1w_2)x
$$

这表示把 $w_2$ 的符号依次附加到 $w_1$ 后面。

---

# 递归定义良构公式

tags: 5.3 Recursive Definitions and Structural Induction

hint:
命题逻辑公式如何由变量和联结词递归生成？

content:
命题逻辑的良构公式可递归定义。

基础步：$T$、$F$ 以及任意命题变量都是良构公式。

递归步：若 $E$ 与 $F$ 是良构公式，则以下都是良构公式：

$$
(\neg E)
$$

$$
(E\land F)
$$

$$
(E\lor F)
$$

$$
(E\to F)
$$

$$
(E\leftrightarrow F)
$$

只有通过这些规则有限次生成的表达式才是良构公式。

---

# 根树的递归定义

tags: 5.3 Recursive Definitions and Structural Induction

hint:
根树如何由较小根树生成？

content:
根树可递归定义。

基础步：单个顶点 $r$ 是一棵根树，其中 $r$ 是根。

递归步：若 $T_1,T_2,\ldots,T_n$ 是两两不相交的根树，根分别为：

$$
r_1,r_2,\ldots,r_n
$$

则取一个新顶点 $r$，并从 $r$ 连边到每个 $r_i$，得到的新图也是根树，$r$ 为新根。

---

# 满二叉树的递归定义

tags: 5.3 Recursive Definitions and Structural Induction

hint:
full binary tree 如何由左右子树构造？

content:
满二叉树可递归定义。

基础步：只有一个顶点 $r$ 的树是满二叉树。

递归步：若 $T_1$ 与 $T_2$ 是不相交的满二叉树，则可以构造一棵新的满二叉树：

$$
T_1\cdot T_2
$$

它由一个新根 $r$、左子树 $T_1$、右子树 $T_2$ 组成，并从 $r$ 分别连到两棵子树的根。

---

# 结构归纳法

tags: 5.3 Recursive Definitions and Structural Induction

hint:
对递归定义的集合证明性质时，为什么不按整数归纳，而按构造规则归纳？

content:
结构归纳法用于证明递归定义集合中所有元素都满足某性质。

证明分为两步：

基础步：证明基础步中直接指定的所有元素都满足该性质；

递归步：假设用于构造新元素的已有元素都满足该性质，证明按递归规则得到的新元素也满足该性质。

这样即可推出递归定义集合中的所有元素都满足该性质。

---

# 结构归纳的有效性

tags: 5.3 Recursive Definitions and Structural Induction

hint:
结构归纳为什么可靠？

content:
结构归纳的有效性来自数学归纳法。

可以令 $P(n)$ 表示：所有用不超过 $n$ 次递归步骤生成的元素都满足目标性质。

基础步证明 $P(0)$。

递归步证明：

$$
P(k)\to P(k+1)
$$

由数学归纳法可得：

$$
\forall n\,P(n)
$$

因此任意经过有限次递归生成的元素都满足目标性质。

---

# 对字符串使用结构归纳

tags: 5.3 Recursive Definitions and Structural Induction

hint:
证明所有字符串都有某性质时，基础步和递归步是什么？

content:
要对：

$$
w\in\Sigma^*
$$

证明性质 $P(w)$，可用结构归纳。

基础步：证明空字符串满足性质：

$$
P(\lambda)
$$

递归步：假设 $P(w)$ 为真，其中：

$$
w\in\Sigma^*
$$

再证明对任意：

$$
x\in\Sigma
$$

都有：

$$
P(wx)
$$

成立。

---

# 对满二叉树使用结构归纳

tags: 5.3 Recursive Definitions and Structural Induction

hint:
证明所有满二叉树都有某性质时，递归步怎样写？

content:
要证明所有满二叉树 $T$ 都满足性质 $P(T)$，可用结构归纳。

基础步：证明只有一个顶点的满二叉树满足 $P$。

递归步：假设满二叉树 $T_1$ 与 $T_2$ 满足性质：

$$
P(T_1)
$$

和：

$$
P(T_2)
$$

证明由它们构造出的满二叉树：

$$
T_1\cdot T_2
$$

也满足：

$$
P(T_1\cdot T_2)
$$

---

# 递归算法

tags: 5.4 Recursive Algorithms

hint:
什么样的算法会调用自身？

content:
递归算法是直接或间接调用自身来解决问题的算法。

递归算法通常包含：

1. 基础情形：可以直接给出答案；
2. 递归情形：把原问题化为一个或多个更小的同类问题；
3. 合并步骤：由较小问题的解得到原问题的解。

递归算法必须确保每次递归调用都使问题规模变小，并最终到达基础情形。

---

# 递归阶乘算法

tags: 5.4 Recursive Algorithms

hint:
如何用递归算法计算 $n!$？

content:
阶乘的递归算法基于：

$$
0!=1
$$

和：

$$
n!=n(n-1)!
$$

伪代码：

```text
procedure factorial(n)
    if n = 0 then return 1
    else return n * factorial(n - 1)
```

该算法对非负整数 $n$ 返回：

$$
n!
$$

---

# 递归幂算法

tags: 5.4 Recursive Algorithms

hint:
如何用递归算法计算 $a^n$？

content:
递归计算幂可以基于：

$$
a^0=1
$$

和：

$$
a^n=a\cdot a^{n-1}
$$

伪代码：

```text
procedure power(a, n)
    if n = 0 then return 1
    else return a * power(a, n - 1)
```

该算法要求 $n$ 为非负整数，并且通常假设 $a\ne0$。

---

# 递归欧几里得算法

tags: 5.4 Recursive Algorithms

hint:
怎样用递归形式计算最大公约数？

content:
递归欧几里得算法基于：

$$
\gcd(0,b)=b
$$

和：

$$
\gcd(a,b)=\gcd(b\bmod a,\ a)
$$

其中：

$$
0<a<b
$$

伪代码：

```text
procedure gcd(a, b)
    if a = 0 then return b
    else return gcd(b mod a, a)
```

每次递归调用都会减小参数规模，最终到达 $a=0$ 的基础情形。

---

# 递归模指数算法

tags: 5.4 Recursive Algorithms

hint:
怎样用反复平方递归计算 $b^n\bmod m$？

content:
递归模指数算法基于指数的奇偶性。

基础情形：

$$
b^0\bmod m=1
$$

若 $n$ 为偶数：

$$
b^n\bmod m=\left(b^{n/2}\bmod m\right)^2\bmod m
$$

若 $n$ 为奇数：

$$
b^n\bmod m=
\left(\left(b^{\lfloor n/2\rfloor}\bmod m\right)^2\bmod m\cdot b\bmod m\right)\bmod m
$$

这种方法每次将指数大约减半，因此比逐次乘法更高效。

---

# 递归二分搜索

tags: 5.4 Recursive Algorithms

hint:
二分搜索如何递归地缩小查找区间？

content:
递归二分搜索用于有序列表：

$$
a_1,a_2,\ldots,a_n
$$

在区间 $[i,j]$ 中查找元素 $x$。

伪代码：

```text
procedure binary_search(i, j, x)
    m := floor((i + j) / 2)
    if x = a_m then return m
    else if x < a_m and i < m then return binary_search(i, m - 1, x)
    else if x > a_m and j > m then return binary_search(m + 1, j, x)
    else return 0
```

递归调用总是在更短的子区间中进行。

---

# 证明递归算法正确性

tags: 5.4 Recursive Algorithms

hint:
递归算法的正确性通常怎样用归纳法证明？

content:
递归算法的正确性常用数学归纳法或强归纳法证明。

基本思路：

1. 证明算法在基础情形下返回正确答案；
2. 假设算法对较小规模输入都正确；
3. 证明在当前规模下，递归调用返回正确子结果；
4. 证明由这些子结果组合出的结果正确。

若算法可能调用多个更小规模问题，通常使用强归纳法更自然。

---

# 递归算法的终止性

tags: 5.4 Recursive Algorithms

hint:
如何保证递归不会无限调用自己？

content:
证明递归算法终止时，需要说明每次递归调用都会使某个非负整数度量严格减小。

该度量可以是：

1. 输入规模；
2. 指数；
3. 列表长度；
4. 树高度；
5. 参数中的某个正整数。

因为非负整数满足良序性，严格下降过程不可能无限继续，所以算法最终到达基础情形并终止。

---

# 递归斐波那契算法

tags: 5.4 Recursive Algorithms

hint:
直接递归计算斐波那契数为什么会重复计算？

content:
斐波那契数可由递归算法计算：

```text
procedure fibonacci(n)
    if n = 0 then return 0
    else if n = 1 then return 1
    else return fibonacci(n - 1) + fibonacci(n - 2)
```

该算法直接对应定义：

$$
f_n=f_{n-1}+f_{n-2}
$$

但它会多次重复计算相同的较小项，因此效率较低。

---

# 迭代斐波那契算法

tags: 5.4 Recursive Algorithms

hint:
怎样避免递归斐波那契中的重复计算？

content:
迭代算法可以从初始值开始逐步计算斐波那契数，避免重复计算。

伪代码：

```text
procedure iterative_fibonacci(n)
    if n = 0 then return 0
    x := 0
    y := 1
    for i := 2 to n
        z := x + y
        x := y
        y := z
    return y
```

迭代算法只保存最近两个值，通常比直接递归算法更高效。

---

# 归并排序

tags: 5.4 Recursive Algorithms

hint:
归并排序如何“分而治之”？

content:
归并排序是一种递归排序算法。

基本思想：

1. 将列表分成两个长度相同或近似相同的子列表；
2. 递归地对两个子列表排序；
3. 将两个已排序子列表合并成一个有序列表。

伪代码：

```text
procedure mergesort(L)
    if length(L) > 1
        split L into L1 and L2
        L := merge(mergesort(L1), mergesort(L2))
    return L
```

归并排序体现了分治思想。

---

# 合并两个有序列表

tags: 5.4 Recursive Algorithms

hint:
怎样把两个已经有序的列表合成一个有序列表？

content:
合并两个有序列表时，每次比较两个列表的首元素，把较小者移入结果列表。

伪代码：

```text
procedure merge(L1, L2)
    L := empty list
    while L1 and L2 are both nonempty
        remove the smaller first element from L1 or L2
        append it to L
    append the remaining elements of the nonempty list
    return L
```

若两个列表长度分别为 $m$ 和 $n$，最坏情况下合并需要：

$$
m+n-1
$$

次比较。

---

# 快速排序思想

tags: 5.4 Recursive Algorithms

hint:
快速排序如何围绕枢轴划分列表？

content:
快速排序是一种递归排序算法。

基本思想：

1. 选择一个枢轴元素；
2. 把其余元素分成两个子列表：小于枢轴的元素和大于枢轴的元素；
3. 递归地对两个子列表排序；
4. 将左子列表、枢轴、右子列表合并。

伪代码：

```text
procedure quicksort(L)
    if length(L) <= 1 then return L
    pivot := first element of L
    L1 := elements of L less than pivot
    L2 := elements of L greater than pivot
    return concatenate(quicksort(L1), pivot, quicksort(L2))
```

快速排序通常很快，但最坏情况下可能退化。

---

# 程序正确性

tags: 5.5 Program Correctness

hint:
为什么测试不能替代正确性证明？

content:
程序正确性研究如何证明程序对所有合法输入都产生正确输出。

测试只能检查有限个样例。即使程序通过所有样例测试，也不能保证它对所有输入都正确。

正确性证明需要使用逻辑推理、循环不变式、归纳法等方法说明程序在所有可能情况下都满足规格。

---

# 部分正确性

tags: 5.5 Program Correctness

hint:
partial correctness 是否保证程序一定终止？

content:
程序段 $S$ 关于初始断言 $p$ 和终止断言 $q$ 部分正确，表示：

如果 $p$ 在输入处为真，并且程序 $S$ 终止，那么 $q$ 在输出处为真。

记作 Hoare 三元组：

$$
p\{S\}q
$$

部分正确性只说明“如果终止则结果正确”，不保证程序一定终止。

---

# 完全正确性

tags: 5.5 Program Correctness

hint:
一个程序要真正正确，需要证明哪两部分？

content:
要证明程序完全正确，通常需要证明两部分：

1. 部分正确性：若程序在满足初始断言的输入上终止，则终止时满足终止断言；
2. 终止性：程序在所有满足初始断言的输入上都会终止。

只有同时具备这两点，程序才对所有合法输入产生正确输出。

---

# 初始断言与终止断言

tags: 5.5 Program Correctness

hint:
程序验证中，输入条件和输出要求分别叫什么？

content:
初始断言描述程序开始执行前输入值必须满足的性质。

终止断言描述程序结束后输出值应满足的性质。

若初始断言为 $p$，终止断言为 $q$，程序段为 $S$，则：

$$
p\{S\}q
$$

表示：只要 $p$ 成立且 $S$ 终止，结束时 $q$ 成立。

---

# Hoare 三元组

tags: 5.5 Program Correctness

hint:
$p\{S\}q$ 怎样读？

content:
Hoare 三元组写作：

$$
p\{S\}q
$$

其中：

$p$ 是初始断言；

$S$ 是程序或程序段；

$q$ 是终止断言。

它表示程序段 $S$ 关于 $p$ 和 $q$ 部分正确。

也就是说，如果执行 $S$ 前 $p$ 成立，并且 $S$ 终止，则执行后 $q$ 成立。

---

# 组合规则

tags: 5.5 Program Correctness

hint:
如果两个程序段连续执行，怎样组合它们的正确性证明？

content:
若程序段 $S_1$ 关于 $p$ 与 $q$ 部分正确：

$$
p\{S_1\}q
$$

且程序段 $S_2$ 关于 $q$ 与 $r$ 部分正确：

$$
q\{S_2\}r
$$

则连续程序段 $S_1;S_2$ 关于 $p$ 与 $r$ 部分正确：

$$
p\{S_1;S_2\}r
$$

这称为组合规则。

---

# if 语句的正确性规则

tags: 5.5 Program Correctness

hint:
没有 else 的 if 语句需要分哪两种情况验证？

content:
对于程序段：

```text
if condition then S
```

要证明：

$$
p\{\text{if condition then }S\}q
$$

需要证明两件事：

当 $p$ 与条件都成立时，执行 $S$ 后 $q$ 成立：

$$
(p\land condition)\{S\}q
$$

当 $p$ 成立而条件不成立时，不执行 $S$，仍有 $q$ 成立：

$$
(p\land \neg condition)\to q
$$

---

# if-else 语句的正确性规则

tags: 5.5 Program Correctness

hint:
有 else 的条件语句如何分支验证？

content:
对于程序段：

```text
if condition then S1 else S2
```

要证明其关于 $p$ 和 $q$ 部分正确，需要分别证明两个分支。

条件为真时：

$$
(p\land condition)\{S_1\}q
$$

条件为假时：

$$
(p\land \neg condition)\{S_2\}q
$$

若两者都成立，则：

$$
p\{\text{if condition then }S_1\text{ else }S_2\}q
$$

成立。

---

# while 循环与循环不变式

tags: 5.5 Program Correctness

hint:
循环证明中，什么性质在每次迭代前后都保持不变？

content:
循环不变式是在循环每次迭代前后都保持为真的断言。

对于：

```text
while condition
    S
```

若 $p$ 是循环不变式，则需要证明：

1. 循环开始前 $p$ 成立；
2. 若 $p$ 和 condition 都成立，执行循环体 $S$ 后 $p$ 仍成立。

即：

$$
(p\land condition)\{S\}p
$$

循环结束时，condition 为假，因此可结合：

$$
p\land \neg condition
$$

推出终止断言。

---

# while 循环的部分正确性规则

tags: 5.5 Program Correctness

hint:
循环不变式如何推出循环结束后的断言？

content:
若 $p$ 是 while 循环的不变式，且有：

$$
(p\land condition)\{S\}p
$$

则：

$$
p\{\text{while condition }S\}(p\land\neg condition)
$$

成立。

也就是说，只要循环初始时 $p$ 成立，并且每次循环体保持 $p$，则循环终止时 $p$ 仍成立，同时循环条件为假。

---

# 循环终止性证明

tags: 5.5 Program Correctness

hint:
怎样证明 while 循环不会无限执行？

content:
证明循环终止通常需要找一个非负整数度量，称为变元或秩函数。

每次循环执行后，该度量都严格减小，并且始终保持非负。

由于非负整数不存在无限严格下降序列，所以循环必定在有限步后终止。

这常与循环不变式一起使用，完成完全正确性证明。

---

# 乘法程序的循环不变式

tags: 5.5 Program Correctness

hint:
用重复加法计算乘积时，循环中应保持什么关系？

content:
用重复加法计算：

$$
mn
$$

时，若先设：

$$
a=|n|
$$

并在循环中维护计数器 $k$ 与累加值 $x$，典型循环不变式为：

$$
x=mk\land k\le a
$$

每执行一次循环：

```text
x := x + m
k := k + 1
```

若原来 $x=mk$，执行后得到：

$$
x=m(k+1)
$$

循环结束时：

$$
k=a
$$

所以：

$$
x=ma=m|n|
$$

再根据 $n$ 的符号得到最终乘积。

---

# 程序验证的基本流程

tags: 5.5 Program Correctness

hint:
完整验证一个程序时，通常怎样拆解？

content:
程序验证的一般流程为：

1. 明确初始断言和终止断言；
2. 将程序拆分为简单语句、条件语句和循环；
3. 对顺序语句使用组合规则；
4. 对条件语句分别验证各分支；
5. 对循环寻找合适的不变式；
6. 证明循环不变式初始化成立、被循环体保持；
7. 证明循环终止；
8. 由循环结束条件和不变式推出终止断言。

---

# 递归与归纳的对应关系

tags: 5.5 Program Correctness

hint:
为什么递归程序常用归纳法证明？

content:
递归程序通过调用自身解决较小问题。

归纳法正好用于证明“如果较小规模正确，则较大规模也正确”。

因此递归程序正确性证明通常对应：

基础情形：证明无递归调用时结果正确；

归纳假设：假设递归调用能正确解决较小输入；

归纳步：证明当前调用利用递归结果能得到正确输出。

这种结构与递归算法的结构完全对应。

---

# 第五章核心思想

tags: 5.5 Program Correctness

hint:
归纳、递归和程序正确性之间有什么统一关系？

content:
第五章的核心思想是：用递推结构处理无限对象或复杂对象。

数学归纳法用于证明关于所有整数的命题。

强归纳法允许使用所有较小情形，适合分解型问题。

递归定义用基础对象和生成规则定义函数、集合、字符串和树。

结构归纳法用于证明递归定义对象的性质。

递归算法用基础情形和递归调用解决问题。

程序正确性证明用断言、规则和不变式保证算法实现满足规格。

---

# 组合学与枚举

tags: 6.1 The Basics of Counting

hint:
Counting 这一章研究的核心对象是什么？

content:
组合学研究对象的排列、选择和安排。

枚举是组合学中的重要内容，指计算满足某些性质的对象个数。

计数方法常用于：

1. 分析算法复杂度；
2. 计算概率；
3. 统计密码、地址、序列等可能性数量；
4. 研究排列、组合、图结构和离散模型。

---

# 乘法原则

tags: 6.1 The Basics of Counting

hint:
一个过程分成连续两步时，总方法数怎样计算？

content:
若一个过程可以分成两个连续任务。

第一个任务有：

$$
n_1
$$

种做法。

对第一个任务的每种做法，第二个任务都有：

$$
n_2
$$

种做法。

则整个过程共有：

$$
n_1n_2
$$

种做法。

这称为乘法原则。

---

# 扩展乘法原则

tags: 6.1 The Basics of Counting

hint:
多个连续任务的方法数怎样相乘？

content:
若一个过程由 $m$ 个连续任务组成，并且第 $i$ 个任务有：

$$
n_i
$$

种做法，其中：

$$
i=1,2,\ldots,m
$$

则整个过程共有：

$$
n_1n_2\cdots n_m
$$

种做法。

乘法原则适合处理“先做这个，再做那个”的分步选择问题。

---

# 笛卡尔积计数

tags: 6.1 The Basics of Counting

hint:
有限集合笛卡尔积的元素个数怎样计算？

content:
若 $A_1,A_2,\ldots,A_m$ 都是有限集合，则：

$$
|A_1\times A_2\times\cdots\times A_m|
=
|A_1||A_2|\cdots |A_m|
$$

这是乘法原则在集合上的形式。

每个有序 $m$ 元组：

$$
(a_1,a_2,\ldots,a_m)
$$

都由每个集合中独立选择一个元素得到。

---

# 函数个数

tags: 6.1 The Basics of Counting

hint:
从 $m$ 元集合到 $n$ 元集合共有多少个函数？

content:
设：

$$
|A|=m,\qquad |B|=n
$$

从 $A$ 到 $B$ 的函数：

$$
f:A\to B
$$

共有：

$$
n^m
$$

个。

原因是：$A$ 中每个元素都可以独立地映射到 $B$ 中任意一个元素，共有 $n$ 种选择。

---

# 单射函数个数

tags: 6.1 The Basics of Counting

hint:
从 $m$ 元集合到 $n$ 元集合的一对一函数有多少个？

content:
设：

$$
|A|=m,\qquad |B|=n
$$

若：

$$
m\le n
$$

则从 $A$ 到 $B$ 的单射共有：

$$
n(n-1)(n-2)\cdots(n-m+1)
$$

个。

也可写为：

$$
\frac{n!}{(n-m)!}
$$

若：

$$
m>n
$$

则不存在从 $A$ 到 $B$ 的单射。

---

# bit string 计数

tags: 6.1 The Basics of Counting

hint:
长度为 $n$ 的 bit string 有多少个？

content:
长度为 $n$ 的 bit string 每一位都有两个选择：

$$
0
$$

或：

$$
1
$$

由乘法原则，长度为 $n$ 的 bit string 共有：

$$
2^n
$$

个。

若每位从 $k$ 个符号中选择，则长度为 $n$ 的字符串共有：

$$
k^n
$$

个。

---

# 树形图

tags: 6.1 The Basics of Counting

hint:
分步选择过程怎样用图形辅助计数？

content:
树形图用分支表示每一步可能的选择。

从根出发，每一层对应一个选择步骤；每条从根到叶的路径对应一个完整结果。

若每一层分支数固定或容易统计，可以用树形图直观体现乘法原则。

树形图适合分析较小规模的分步选择问题。

---

# 求和原则

tags: 6.1 The Basics of Counting

hint:
一个任务可以用互不重叠的几类方式完成时，总方法数怎样相加？

content:
若一个任务可以用第一类方式完成，且有：

$$
n_1
$$

种做法；

也可以用第二类方式完成，且有：

$$
n_2
$$

种做法；

并且两类做法没有重叠，则任务共有：

$$
n_1+n_2
$$

种做法。

这称为求和原则。

---

# 扩展求和原则

tags: 6.1 The Basics of Counting

hint:
任务分成多个互不重叠类别时怎样计数？

content:
若一个任务可以分为 $m$ 类互不重叠的做法。

第 $i$ 类有：

$$
n_i
$$

种做法，其中：

$$
i=1,2,\ldots,m
$$

则总做法数为：

$$
n_1+n_2+\cdots+n_m
$$

求和原则适合处理“属于这一类，或属于那一类”的分类计数问题。

---

# 不交并的计数

tags: 6.1 The Basics of Counting

hint:
两两不交集合的并集大小如何计算？

content:
若有限集合：

$$
A_1,A_2,\ldots,A_m
$$

两两不交，则：

$$
\left|\bigcup_{i=1}^{m}A_i\right|
=
\sum_{i=1}^{m}|A_i|
$$

这是求和原则在集合上的形式。

“两两不交”是直接相加的关键条件。

---

# 减法原则

tags: 6.1 The Basics of Counting

hint:
如果两类计数有重叠，为什么要减去交集？

content:
若一个任务可以用第一类方式完成，共有 $n_1$ 种；也可以用第二类方式完成，共有 $n_2$ 种。

如果两类中有 $n_{12}$ 种做法被重复计数，则总数为：

$$
n_1+n_2-n_{12}
$$

集合形式为：

$$
|A\cup B|=|A|+|B|-|A\cap B|
$$

这也称为两个集合的容斥原理。

---

# 补集计数

tags: 6.1 The Basics of Counting

hint:
有时直接数满足条件的对象很难，应该改数什么？

content:
若全集 $U$ 有限，$A$ 是其中一类对象，则：

$$
|A|=|U|-|\overline{A}|
$$

当直接计数 $A$ 很困难，而计数不属于 $A$ 的对象更容易时，可以使用补集计数。

这常用于“至少一个”“不全为”“不满足某条件”等问题。

---

# 除法原则

tags: 6.1 The Basics of Counting

hint:
如果每个最终对象被同样重复计数 $d$ 次，怎样修正？

content:
若一个过程能以 $n$ 种方式执行，但每个真正不同的结果都被恰好计数 $d$ 次，则不同结果的个数为：

$$
\frac{n}{d}
$$

这称为除法原则。

集合形式：若有限集合 $A$ 被划分为 $n$ 个两两不交子集，且每个子集都有 $d$ 个元素，则：

$$
n=\frac{|A|}{d}
$$

---

# 一一对应计数

tags: 6.1 The Basics of Counting

hint:
为什么把难数的对象转化为容易数的对象有效？

content:
若两个有限集合 $A$ 与 $B$ 之间存在双射：

$$
f:A\to B
$$

则：

$$
|A|=|B|
$$

因此，可以通过构造一一对应，把难以直接计数的对象转化为更容易计数的对象。

这种方法称为双射计数或一一对应计数。

---

# 鸽巢原理

tags: 6.2 The Pigeonhole Principle

hint:
超过 $k$ 个对象放入 $k$ 个盒子，会发生什么？

content:
鸽巢原理说明：若把至少：

$$
k+1
$$

个对象放入：

$$
k
$$

个盒子，则至少有一个盒子包含两个或更多对象。

形式化地，如果 $k$ 是正整数，并且有 $k+1$ 个或更多对象被放入 $k$ 个盒子，则存在某个盒子至少含有 $2$ 个对象。

---

# 鸽巢原理的反证思想

tags: 6.2 The Pigeonhole Principle

hint:
为什么鸽巢原理显然成立？

content:
鸽巢原理常用反证或逆否证明。

若每个盒子最多只有一个对象，则 $k$ 个盒子最多容纳：

$$
k
$$

个对象。

但题设对象数至少为：

$$
k+1
$$

这与“最多 $k$ 个对象”矛盾。

因此，至少有一个盒子含有两个或更多对象。

---

# 函数形式的鸽巢原理

tags: 6.2 The Pigeonhole Principle

hint:
从较大集合到较小集合的函数为什么不能是单射？

content:
若集合 $A$ 至少有 $k+1$ 个元素，而集合 $B$ 有 $k$ 个元素，则任意函数：

$$
f:A\to B
$$

都不可能是一对一函数。

原因是：把 $A$ 中元素看作对象，把它们的函数值看作盒子。对象多于盒子，所以至少两个不同元素有相同函数值。

---

# 广义鸽巢原理

tags: 6.2 The Pigeonhole Principle

hint:
$N$ 个对象放入 $k$ 个盒子，至少有一个盒子多大？

content:
广义鸽巢原理说明：若把 $N$ 个对象放入 $k$ 个盒子，则至少有一个盒子包含至少：

$$
\left\lceil\frac{N}{k}\right\rceil
$$

个对象。

这是因为如果每个盒子都少于这个数，总对象数就无法达到 $N$。

---

# 鸽巢原理的最大下界形式

tags: 6.2 The Pigeonhole Principle

hint:
若每个盒子最多放 $r$ 个对象，总共最多能放多少个？

content:
若 $k$ 个盒子中每个盒子最多放 $r$ 个对象，则总对象数最多为：

$$
kr
$$

因此，若对象数至少为：

$$
kr+1
$$

则至少有一个盒子包含至少：

$$
r+1
$$

个对象。

这是广义鸽巢原理的等价形式。

---

# 鸽巢原理的取模应用

tags: 6.2 The Pigeonhole Principle

hint:
为什么任取 $m+1$ 个整数，必有两个模 $m$ 同余？

content:
任意整数除以正整数 $m$ 时，余数只能是：

$$
0,1,\ldots,m-1
$$

共 $m$ 种。

若取：

$$
m+1
$$

个整数，把整数放入按余数分类的 $m$ 个盒子中。

由鸽巢原理，至少有两个整数余数相同。

因此这两个整数模 $m$ 同余。

---

# 鸽巢原理的生日型应用

tags: 6.2 The Pigeonhole Principle

hint:
人数超过日期类别数时，能推出什么？

content:
鸽巢原理可用于生日或星期问题。

若一年按最多 $366$ 个可能生日分类，则在任意：

$$
367
$$

个人中，至少有两个人生日相同。

若只按星期几分类，则只有：

$$
7
$$

个盒子。

任意：

$$
8
$$

个人中，至少有两个人出生在同一星期几。

---

# 排列

tags: 6.3 Permutations and Combinations

hint:
permutation 是否关心顺序？

content:
排列是集合元素的有序安排。

若使用集合中全部元素，则称为该集合的一个排列。

例如，对集合：

$$
\{a,b,c\}
$$

序列：

$$
(a,b,c)
$$

和：

$$
(b,a,c)
$$

是不同排列，因为顺序不同。

---

# 全排列数

tags: 6.3 Permutations and Combinations

hint:
$n$ 个不同对象共有多少种排列？

content:
$n$ 个不同对象的全排列数为：

$$
n!
$$

其中：

$$
n!=n(n-1)(n-2)\cdots 2\cdot1
$$

并约定：

$$
0!=1
$$

这是因为第一个位置有 $n$ 种选择，第二个位置有 $n-1$ 种选择，依此类推。

---

# $r$ 排列

tags: 6.3 Permutations and Combinations

hint:
从 $n$ 个元素中有序选出 $r$ 个，怎样计数？

content:
从 $n$ 个不同元素中有序选出 $r$ 个元素，称为 $r$ 排列。

$r$ 排列数记为：

$$
P(n,r)
$$

其公式为：

$$
P(n,r)=n(n-1)(n-2)\cdots(n-r+1)
$$

也可写作：

$$
P(n,r)=\frac{n!}{(n-r)!}
$$

其中：

$$
0\le r\le n
$$

---

# 排列中的块法

tags: 6.3 Permutations and Combinations

hint:
若某些元素必须相邻，怎样计数？

content:
当排列中要求若干元素必须连续出现时，可以把这些元素看成一个整体块。

若某个固定块必须保持内部顺序，则先把该块作为一个对象，与其他对象一起排列。

若块内部也可重排，则还需再乘以块内部排列数。

块法适合处理“某几个字符必须相邻”“某些人必须坐在一起”等问题。

---

# 组合

tags: 6.3 Permutations and Combinations

hint:
combination 是否关心顺序？

content:
组合是从集合中无序选取若干元素。

从 $n$ 个元素中选出 $r$ 个元素的组合称为 $r$ 组合。

组合只关心选了哪些元素，不关心选择顺序。

因此：

$$
\{a,b,c\}=\{c,a,b\}
$$

它们是同一个组合。

---

# 组合数公式

tags: 6.3 Permutations and Combinations

hint:
从 $n$ 个元素中无序选 $r$ 个，有多少种？

content:
从 $n$ 个元素中选出 $r$ 个的组合数记为：

$$
C(n,r)
$$

也写作：

$$
\binom{n}{r}
$$

公式为：

$$
C(n,r)=\binom{n}{r}=\frac{n!}{r!(n-r)!}
$$

其中：

$$
0\le r\le n
$$

---

# 排列与组合的关系

tags: 6.3 Permutations and Combinations

hint:
为什么 $P(n,r)=C(n,r)r!$？

content:
一个 $r$ 排列可以分两步得到：

1. 先从 $n$ 个元素中选出 $r$ 个元素；
2. 再把这 $r$ 个元素按顺序排列。

第一步有：

$$
C(n,r)
$$

种方法。

第二步有：

$$
r!
$$

种方法。

因此：

$$
P(n,r)=C(n,r)r!
$$

从而：

$$
C(n,r)=\frac{P(n,r)}{r!}
$$

---

# 互补组合恒等式

tags: 6.3 Permutations and Combinations

hint:
选 $r$ 个元素等价于不选多少个元素？

content:
从 $n$ 个元素中选出 $r$ 个，与选出不被选择的 $n-r$ 个元素是一一对应的。

因此：

$$
\binom{n}{r}=\binom{n}{n-r}
$$

其中：

$$
0\le r\le n
$$

这称为互补组合恒等式。

---

# 二项式系数

tags: 6.4 Binomial Coefficients and Identities

hint:
为什么组合数也叫 binomial coefficient？

content:
组合数：

$$
\binom{n}{r}
$$

称为二项式系数，因为它出现在二项式展开：

$$
(x+y)^n
$$

中。

具体地，$x^{n-r}y^r$ 的系数是：

$$
\binom{n}{r}
$$

因此组合数同时具有计数意义和代数展开意义。

---

# 帕斯卡恒等式

tags: 6.4 Binomial Coefficients and Identities

hint:
$\binom{n+1}{k}$ 如何由上一行两个数得到？

content:
帕斯卡恒等式为：

$$
\binom{n+1}{k}
=
\binom{n}{k-1}
+
\binom{n}{k}
$$

其中：

$$
1\le k\le n
$$

组合解释：从 $n+1$ 个元素中选 $k$ 个，按是否包含某个指定元素分为两类。

包含该元素时，需要从其余 $n$ 个中选 $k-1$ 个；

不包含该元素时，需要从其余 $n$ 个中选 $k$ 个。

---

# 帕斯卡三角形

tags: 6.4 Binomial Coefficients and Identities

hint:
二项式系数如何排列成三角形？

content:
帕斯卡三角形按行排列二项式系数。

第 $n$ 行包含：

$$
\binom{n}{0},\binom{n}{1},\ldots,\binom{n}{n}
$$

每一行两端都是 $1$：

$$
\binom{n}{0}=\binom{n}{n}=1
$$

内部元素由上一行相邻两个元素相加得到：

$$
\binom{n}{k}=\binom{n-1}{k-1}+\binom{n-1}{k}
$$

---

# 二项式定理

tags: 6.4 Binomial Coefficients and Identities

hint:
$(x+y)^n$ 的展开式是什么？

content:
二项式定理说明，对非负整数 $n$，有：

$$
(x+y)^n
=
\sum_{j=0}^{n}\binom{n}{j}x^{n-j}y^j
$$

展开中第 $j$ 项含有：

$$
x^{n-j}y^j
$$

其系数为：

$$
\binom{n}{j}
$$

---

# 二项式系数之和

tags: 6.4 Binomial Coefficients and Identities

hint:
第 $n$ 行二项式系数全部相加等于多少？

content:
令二项式定理中的：

$$
x=1,\qquad y=1
$$

得到：

$$
2^n
=
\sum_{j=0}^{n}\binom{n}{j}
$$

组合解释：$n$ 元集合的所有子集数为 $2^n$，也等于按子集大小分类后的总数。

---

# 交错二项式系数之和

tags: 6.4 Binomial Coefficients and Identities

hint:
第 $n$ 行二项式系数交替相加为什么为零？

content:
令二项式定理中的：

$$
x=1,\qquad y=-1
$$

得到：

$$
0=(1-1)^n
=
\sum_{j=0}^{n}(-1)^j\binom{n}{j}
$$

当：

$$
n>0
$$

时，有：

$$
\sum_{j=0}^{n}(-1)^j\binom{n}{j}=0
$$

---

# 加权二项式系数之和

tags: 6.4 Binomial Coefficients and Identities

hint:
所有 $j\binom{n}{j}$ 相加等于多少？

content:
恒等式为：

$$
\sum_{j=0}^{n}j\binom{n}{j}=n2^{n-1}
$$

组合解释：统计从 $n$ 元集合中先选一个子集，再从该子集中选一个被标记元素的方式数。

也可以先选被标记元素，有 $n$ 种选择；再从剩下 $n-1$ 个元素中任意选取其他元素，有 $2^{n-1}$ 种选择。

---

# 范德蒙德恒等式

tags: 6.4 Binomial Coefficients and Identities

hint:
从两个集合合起来选 $r$ 个元素，怎样按来自第一个集合的个数分类？

content:
范德蒙德恒等式为：

$$
\binom{m+n}{r}
=
\sum_{k=0}^{r}
\binom{m}{k}\binom{n}{r-k}
$$

组合解释：从含 $m$ 个元素的集合和含 $n$ 个元素的集合的并中选 $r$ 个元素。

若从第一个集合中选 $k$ 个，则从第二个集合中必须选 $r-k$ 个。

对所有可能的 $k$ 求和得到总数。

---

# 组合证明

tags: 6.4 Binomial Coefficients and Identities

hint:
不用代数变形，如何证明组合恒等式？

content:
组合证明通过说明等式两边计数的是同一个集合，来证明恒等式。

常见方法：

1. 直接说明左右两边用不同方式计算同一类对象；
2. 把对象分成互不重叠的类别，得到求和式；
3. 构造左右两边计数对象之间的双射。

组合证明通常能揭示恒等式的结构含义。

---

# 允许重复的 $r$ 排列

tags: 6.5 Generalized Permutations and Combinations

hint:
从 $n$ 个对象中有序选 $r$ 次，每次可重复，有多少种？

content:
若从 $n$ 个元素中有序选出 $r$ 个，且允许重复使用元素，则每个位置都有 $n$ 种选择。

由乘法原则，允许重复的 $r$ 排列数为：

$$
n^r
$$

这适用于长度为 $r$ 的字符串、密码、编号等问题。

---

# 允许重复的 $r$ 组合

tags: 6.5 Generalized Permutations and Combinations

hint:
从 $n$ 类对象中无序选 $r$ 个并允许重复，怎样计数？

content:
从 $n$ 个不同类型中无序选出 $r$ 个对象，允许重复，称为允许重复的 $r$ 组合。

其数量为：

$$
\binom{n+r-1}{r}
$$

也可写为：

$$
\binom{n+r-1}{n-1}
$$

这称为 stars and bars 方法的基本公式。

---

# Stars and Bars 方法

tags: 6.5 Generalized Permutations and Combinations

hint:
非负整数解个数为什么等于组合数？

content:
非负整数方程：

$$
x_1+x_2+\cdots+x_n=r
$$

的解的个数为：

$$
\binom{n+r-1}{r}
$$

解释：把 $r$ 个相同对象看作星号，把 $n-1$ 个分隔符看作隔板。

总共有：

$$
r+n-1
$$

个位置，选择其中 $n-1$ 个放隔板，或选择 $r$ 个放星号。

---

# 正整数解的 stars and bars

tags: 6.5 Generalized Permutations and Combinations

hint:
若每个变量至少为 $1$，如何转化为非负整数解？

content:
正整数方程：

$$
x_1+x_2+\cdots+x_n=r
$$

其中：

$$
x_i\ge1
$$

的解个数为：

$$
\binom{r-1}{n-1}
$$

前提是：

$$
r\ge n
$$

证明方法：令：

$$
y_i=x_i-1
$$

则：

$$
y_i\ge0
$$

且：

$$
y_1+y_2+\cdots+y_n=r-n
$$

所以解数为：

$$
\binom{(r-n)+n-1}{n-1}=\binom{r-1}{n-1}
$$

---

# 有上界约束的重复组合

tags: 6.5 Generalized Permutations and Combinations

hint:
若每类对象最多能选一定数量，为什么不能直接套 stars and bars？

content:
若要求：

$$
x_1+x_2+\cdots+x_n=r
$$

并且每个变量有上界：

$$
x_i\le u_i
$$

则不能直接使用基本 stars and bars 公式。

常见处理方法包括：

1. 先用 stars and bars 计数所有非负解；
2. 再减去违反上界的解；
3. 对多个变量同时违反上界的情况，需要使用容斥原理。

这种问题常在第八章容斥原理中系统处理。

---

# 不可区分对象的排列

tags: 6.5 Generalized Permutations and Combinations

hint:
若对象中有重复类型，全排列要除以哪些重复次数？

content:
若共有 $n$ 个对象，其中有 $k$ 种类型。

第 $i$ 种类型有：

$$
n_i
$$

个不可区分对象，并且：

$$
n_1+n_2+\cdots+n_k=n
$$

则不同排列数为：

$$
\frac{n!}{n_1!n_2!\cdots n_k!}
$$

分母用于消除同类对象内部交换造成的重复计数。

---

# 分配可区分对象到可区分盒子

tags: 6.5 Generalized Permutations and Combinations

hint:
$r$ 个不同对象放入 $n$ 个不同盒子，有多少种方式？

content:
若有 $r$ 个可区分对象和 $n$ 个可区分盒子，并且每个对象可以放入任意一个盒子，则每个对象有 $n$ 种选择。

由乘法原则，分配方式共有：

$$
n^r
$$

这等价于从 $r$ 个对象集合到 $n$ 个盒子集合的函数个数。

---

# 分配不可区分对象到可区分盒子

tags: 6.5 Generalized Permutations and Combinations

hint:
$r$ 个相同对象放入 $n$ 个不同盒子，有多少种方式？

content:
把 $r$ 个不可区分对象放入 $n$ 个可区分盒子，允许盒子为空，等价于求非负整数解：

$$
x_1+x_2+\cdots+x_n=r
$$

其中 $x_i$ 表示第 $i$ 个盒子中的对象数。

因此分配方式数为：

$$
\binom{n+r-1}{r}
$$

也可写为：

$$
\binom{n+r-1}{n-1}
$$

---

# 分配可区分对象到不可区分盒子

tags: 6.5 Generalized Permutations and Combinations

hint:
对象不同、盒子相同且不许空盒时，用什么数计数？

content:
把 $r$ 个可区分对象分配到 $j$ 个不可区分盒子中，且没有盒子为空，其方式数为第二类斯特林数：

$$
S(r,j)
$$

它也表示把 $r$ 个元素的集合划分成 $j$ 个非空子集的方式数。

若最多使用 $n$ 个不可区分盒子，则方式数为：

$$
\sum_{j=1}^{n}S(r,j)
$$

---

# 第二类斯特林数

tags: 6.5 Generalized Permutations and Combinations

hint:
$S(n,j)$ 计数什么？

content:
第二类斯特林数记为：

$$
S(n,j)
$$

它表示把 $n$ 个可区分对象划分成 $j$ 个非空、不可区分盒子的方式数。

等价地，它表示把 $n$ 元集合划分成 $j$ 个非空子集的方式数。

常用边界值包括：

$$
S(n,1)=1
$$

$$
S(n,n)=1
$$

---

# 盒子分配计数总览

tags: 6.5 Generalized Permutations and Combinations

hint:
对象和盒子是否可区分，会影响使用哪类公式。

content:
分配对象到盒子的计数取决于对象和盒子是否可区分。

可区分对象放入可区分盒子：

$$
n^r
$$

不可区分对象放入可区分盒子：

$$
\binom{n+r-1}{r}
$$

可区分对象放入 $j$ 个不可区分非空盒子：

$$
S(r,j)
$$

不可区分对象放入不可区分盒子通常对应整数分拆问题，一般没有像前几类那样简单的闭式公式。

---

# 生成排列

tags: 6.6 Generating Permutations and Combinations

hint:
为什么需要系统生成所有排列？

content:
在模拟、搜索、测试和枚举问题中，经常需要系统地产生某个集合的所有排列。

生成排列的目标是：不重复、不遗漏地产生所有：

$$
n!
$$

个排列。

常见方式是按字典序生成下一个排列。

---

# 字典序

tags: 6.6 Generating Permutations and Combinations

hint:
怎样比较两个排列谁在前？

content:
字典序按照从左到右的顺序比较两个序列。

给定两个不同序列：

$$
a_1a_2\cdots a_n
$$

和：

$$
b_1b_2\cdots b_n
$$

找到第一个满足：

$$
a_i\ne b_i
$$

的位置 $i$。

若：

$$
a_i<b_i
$$

则第一个序列在字典序中排在第二个序列之前。

---

# 下一个排列算法思想

tags: 6.6 Generating Permutations and Combinations

hint:
按字典序生成下一个排列时，关键是找到哪个位置？

content:
按字典序生成下一个排列的基本思想：

1. 从右向左找第一个位置 $j$，使得：

$$
a_j<a_{j+1}
$$

2. 在 $a_j$ 右侧找大于 $a_j$ 的最小元素 $a_k$；
3. 交换 $a_j$ 和 $a_k$；
4. 将 $j$ 右侧部分按递增顺序排列。

若不存在这样的 $j$，说明当前排列已经是最后一个排列。

---

# 生成所有排列的伪代码思想

tags: 6.6 Generating Permutations and Combinations

hint:
怎样从最小排列开始反复生成下一个排列？

content:
生成集合：

$$
\{1,2,\ldots,n\}
$$

所有排列的一种方法是从：

$$
1,2,\ldots,n
$$

开始，反复应用“下一个排列”算法，直到得到：

$$
n,n-1,\ldots,1
$$

伪代码思想：

```text
start with the permutation 1,2,...,n
print it
while the current permutation is not n,n-1,...,1
    replace it by the next permutation in lexicographic order
    print it
```

这样可以按字典序输出全部 $n!$ 个排列。

---

# 生成组合

tags: 6.6 Generating Permutations and Combinations

hint:
怎样系统地产生所有 $r$ 组合？

content:
为了生成集合：

$$
\{1,2,\ldots,n\}
$$

的所有 $r$ 组合，可以把每个组合按递增序列表示：

$$
a_1<a_2<\cdots<a_r
$$

从最小组合：

$$
1,2,\ldots,r
$$

开始，按字典序反复生成下一个组合，直到最大组合：

$$
n-r+1,n-r+2,\ldots,n
$$

---

# 下一个组合算法思想

tags: 6.6 Generating Permutations and Combinations

hint:
如何从当前递增组合得到字典序下一个组合？

content:
当前 $r$ 组合表示为：

$$
a_1<a_2<\cdots<a_r
$$

要生成下一个组合：

1. 从右向左找第一个还能增大的位置 $i$，满足：

$$
a_i<n-r+i
$$

2. 将：

$$
a_i
$$

增加 $1$；

3. 对其右侧位置依次设为最小可能值：

$$
a_{i+1}=a_i+1,\quad a_{i+2}=a_i+2,\ldots
$$

若不存在这样的 $i$，说明当前组合已经是最后一个组合。

---

# bit string 生成子集

tags: 6.6 Generating Permutations and Combinations

hint:
怎样用 bit string 生成一个集合的所有子集？

content:
若集合：

$$
S=\{s_1,s_2,\ldots,s_n\}
$$

则每个长度为 $n$ 的 bit string 对应 $S$ 的一个子集。

第 $i$ 位为 $1$，表示选择：

$$
s_i
$$

第 $i$ 位为 $0$，表示不选择：

$$
s_i
$$

因此生成所有长度为 $n$ 的 bit string，就等价于生成 $S$ 的所有：

$$
2^n
$$

个子集。

---

# 格雷码

tags: 6.6 Generating Permutations and Combinations

hint:
有没有办法让相邻 bit string 只差一位？

content:
格雷码是一种排列所有长度为 $n$ 的 bit string 的方式，使得相邻两个 bit string 恰好只在一位上不同。

格雷码常用于需要逐步改变状态、减少切换错误或系统地生成子集的场景。

反射格雷码可递归构造：

1. 先列出长度 $n-1$ 的格雷码；
2. 在前半部分前加 $0$；
3. 将原列表反向，在后半部分前加 $1$。

---

# Cantor 展开

tags: 6.6 Generating Permutations and Combinations

hint:
排列如何与小于 $n!$ 的整数建立对应？

content:
Cantor 展开把小于 $n!$ 的非负整数唯一表示为：

$$
a_1 1!+a_2 2!+\cdots+a_{n-1}(n-1)!
$$

其中：

$$
0\le a_i\le i
$$

对一个排列，也可定义相应的 Cantor digits：对 $k=2,3,\ldots,n$，令 $a_{k-1}$ 为排列中跟在 $k$ 后面且小于 $k$ 的整数个数。

这种对应可用于把排列与整数编号互相转换。

---

# 第六章核心思想

tags: 6.6 Generating Permutations and Combinations

hint:
Counting 章的工具箱包括哪些核心方法？

content:
第六章的核心是建立基础计数工具箱。

核心方法包括：

1. 乘法原则；
2. 求和原则；
3. 减法原则与补集计数；
4. 除法原则；
5. 鸽巢原理与广义鸽巢原理；
6. 排列与组合；
7. 二项式系数和二项式定理；
8. 允许重复和不可区分对象的计数；
9. 对象分配到盒子的模型；
10. 生成排列、组合和子集的算法。

这些方法构成后续概率、递推关系、容斥原理和图论计数的基础。

---

# 离散概率

tags: 7.1 An Introduction to Discrete Probability

hint:
离散概率研究哪些实验结果？

content:
离散概率研究结果集合为有限集或可数集的随机实验。

随机实验是指会产生某个结果的过程，但在实验前不能确定具体结果。

概率用于描述事件发生的可能性，并常与计数方法结合使用。

在有限且等可能的样本空间中，概率可以通过“有利结果数除以所有结果数”来计算。

---

# 随机实验

tags: 7.1 An Introduction to Discrete Probability

hint:
概率论中的 experiment 是什么？

content:
随机实验是一个会产生某个结果的过程。

实验的所有可能结果构成样本空间。

例如：

1. 掷一枚硬币；
2. 掷一颗骰子；
3. 随机生成一个 bit string；
4. 从集合中随机选择一个元素；
5. 抽取一手牌。

每次实验会产生样本空间中的一个结果。

---

# 样本空间

tags: 7.1 An Introduction to Discrete Probability

hint:
所有可能结果组成什么集合？

content:
样本空间是随机实验所有可能结果组成的集合，通常记为：

$$
S
$$

例如，掷一颗普通骰子的样本空间为：

$$
S=\{1,2,3,4,5,6\}
$$

若实验结果有限，并且每个结果等可能，则可以直接使用拉普拉斯概率定义。

---

# 事件

tags: 7.1 An Introduction to Discrete Probability

hint:
事件和样本空间之间是什么关系？

content:
事件是样本空间的子集。

若样本空间为 $S$，事件 $E$ 满足：

$$
E\subseteq S
$$

事件发生，表示实验结果属于集合 $E$。

不可能事件为空集：

$$
\varnothing
$$

必然事件为整个样本空间：

$$
S
$$

---

# 拉普拉斯概率定义

tags: 7.1 An Introduction to Discrete Probability

hint:
有限等可能样本空间中，事件概率怎样计算？

content:
设 $S$ 是有限非空样本空间，且所有结果等可能。若 $E$ 是事件，则事件 $E$ 的概率为：

$$
p(E)=\frac{|E|}{|S|}
$$

其中 $|E|$ 是有利结果数，$|S|$ 是所有可能结果数。

概率总满足：

$$
0\le p(E)\le1
$$

---

# 概率为 0 与概率为 1

tags: 7.1 An Introduction to Discrete Probability

hint:
不可能事件和必然事件的概率分别是多少？

content:
若事件不可能发生，即：

$$
E=\varnothing
$$

则：

$$
p(E)=0
$$

若事件必然发生，即：

$$
E=S
$$

则：

$$
p(E)=1
$$

一般地，事件概率越接近 $1$，事件越可能发生；越接近 $0$，事件越不可能发生。

---

# 用计数方法求概率

tags: 7.1 An Introduction to Discrete Probability

hint:
概率问题为什么常常先转化为计数问题？

content:
在有限等可能样本空间中：

$$
p(E)=\frac{|E|}{|S|}
$$

因此求概率常转化为两个计数任务：

1. 计算样本空间大小 $|S|$；
2. 计算事件中有利结果数 $|E|$。

常用计数工具包括乘法原则、加法原则、排列、组合和补集计数。

---

# 抽样有放回与无放回

tags: 7.1 An Introduction to Discrete Probability

hint:
抽取后是否放回，会怎样改变样本空间大小？

content:
抽样有放回时，每次抽取后对象回到总体中，因此每一步可选对象数不变。

若从 $n$ 个对象中有放回地依次抽取 $r$ 次，则结果数为：

$$
n^r
$$

抽样无放回时，每次抽取后对象不再回到总体中，因此每一步可选对象数减少。

若从 $n$ 个不同对象中无放回地依次抽取 $r$ 次，则结果数为：

$$
n(n-1)\cdots(n-r+1)
$$

---

# 补事件概率

tags: 7.1 An Introduction to Discrete Probability

hint:
直接求“至少一个”困难时，常改求什么？

content:
事件 $E$ 的补事件为：

$$
\overline{E}=S-E
$$

其概率满足：

$$
p(\overline{E})=1-p(E)
$$

等价地：

$$
p(E)=1-p(\overline{E})
$$

补事件法常用于求“至少一个”“不是全部”“不发生某坏情况”的概率。

---

# 两事件并的概率

tags: 7.1 An Introduction to Discrete Probability

hint:
两个事件相加时为什么要减去交集？

content:
若 $E_1$ 与 $E_2$ 是样本空间 $S$ 中的事件，则：

$$
p(E_1\cup E_2)=p(E_1)+p(E_2)-p(E_1\cap E_2)
$$

原因是 $E_1\cap E_2$ 中的结果在 $p(E_1)$ 与 $p(E_2)$ 中被重复计算了一次。

---

# 互斥事件

tags: 7.1 An Introduction to Discrete Probability

hint:
两个事件不能同时发生时，交集是什么？

content:
若两个事件 $E_1$ 和 $E_2$ 不能同时发生，则称它们互斥。

形式化地：

$$
E_1\cap E_2=\varnothing
$$

此时：

$$
p(E_1\cup E_2)=p(E_1)+p(E_2)
$$

更一般地，若事件 $E_1,E_2,\ldots,E_n$ 两两互斥，则：

$$
p\left(\bigcup_{i=1}^{n}E_i\right)=\sum_{i=1}^{n}p(E_i)
$$

---

# 概率分布

tags: 7.2 Probability Theory

hint:
如果样本空间结果不等可能，应怎样指定概率？

content:
设 $S$ 是有限或可数样本空间。概率分布是给每个结果 $s\in S$ 指定一个概率 $p(s)$ 的函数。

它必须满足：

$$
0\le p(s)\le1
$$

且：

$$
\sum_{s\in S}p(s)=1
$$

事件 $E$ 的概率定义为：

$$
p(E)=\sum_{s\in E}p(s)
$$

---

# 均匀分布

tags: 7.2 Probability Theory

hint:
有限集合上每个结果概率相同时叫什么分布？

content:
若有限样本空间 $S$ 有：

$$
|S|=n
$$

个元素，并且每个结果概率相同，则称其服从均匀分布。

每个结果 $s\in S$ 的概率为：

$$
p(s)=\frac{1}{n}
$$

在均匀分布下，事件概率退化为拉普拉斯公式：

$$
p(E)=\frac{|E|}{|S|}
$$

---

# 一般概率下的补事件与并事件公式

tags: 7.2 Probability Theory

hint:
非等可能情形下，补事件和并事件公式是否仍成立？

content:
即使样本空间中的结果不等可能，仍有：

$$
p(\overline{E})=1-p(E)
$$

对两个事件：

$$
p(E_1\cup E_2)=p(E_1)+p(E_2)-p(E_1\cap E_2)
$$

若 $E_1$ 与 $E_2$ 互斥，则：

$$
p(E_1\cup E_2)=p(E_1)+p(E_2)
$$

---

# 可数个互斥事件的概率

tags: 7.2 Probability Theory

hint:
一列两两互斥事件的并，概率怎样计算？

content:
若：

$$
E_1,E_2,\ldots
$$

是一列两两互斥事件，则：

$$
p\left(\bigcup_i E_i\right)=\sum_i p(E_i)
$$

该公式适用于有限列或可数无限列事件。

前提是事件两两不相交：

$$
E_i\cap E_j=\varnothing,\quad i\ne j
$$

---

# 条件概率

tags: 7.2 Probability Theory

hint:
已知 $F$ 发生后，$E$ 的概率怎样定义？

content:
设 $E$ 和 $F$ 是样本空间 $S$ 中的事件，且：

$$
p(F)>0
$$

则在已知 $F$ 发生的条件下，$E$ 发生的条件概率定义为：

$$
p(E\mid F)=\frac{p(E\cap F)}{p(F)}
$$

条件概率可以理解为：把样本空间限制到 $F$ 内，再计算 $E$ 在其中发生的比例。

---

# 条件概率的乘法公式

tags: 7.2 Probability Theory

hint:
$p(E\cap F)$ 怎样用条件概率表示？

content:
由条件概率定义：

$$
p(E\mid F)=\frac{p(E\cap F)}{p(F)}
$$

可得：

$$
p(E\cap F)=p(E\mid F)p(F)
$$

同理，若 $p(E)>0$，则：

$$
p(E\cap F)=p(F\mid E)p(E)
$$

这些公式常用于分步计算联合事件概率。

---

# 独立事件

tags: 7.2 Probability Theory

hint:
一个事件发生不影响另一个事件概率时，怎样表示？

content:
若事件 $E$ 与 $F$ 满足：

$$
p(E\cap F)=p(E)p(F)
$$

则称 $E$ 与 $F$ 独立。

当 $p(F)>0$ 时，独立也等价于：

$$
p(E\mid F)=p(E)
$$

当 $p(E)>0$ 时，也等价于：

$$
p(F\mid E)=p(F)
$$

直观上，一个事件的发生不会改变另一个事件发生的概率。

---

# 不独立事件

tags: 7.2 Probability Theory

hint:
如何判断两个事件不是 independent？

content:
要证明两个事件 $E$ 与 $F$ 不独立，只需证明：

$$
p(E\cap F)\ne p(E)p(F)
$$

或者在条件概率有定义时证明：

$$
p(E\mid F)\ne p(E)
$$

不独立表示一个事件的发生会改变另一个事件的概率。

---

# 两两独立与相互独立

tags: 7.2 Probability Theory

hint:
多个事件两两独立是否一定相互独立？

content:
事件 $E_1,E_2,\ldots,E_n$ 两两独立，表示任意两个不同事件都满足：

$$
p(E_i\cap E_j)=p(E_i)p(E_j)
$$

其中：

$$
i\ne j
$$

相互独立更强，要求任意两个或更多事件的交都满足概率乘积公式：

$$
p(E_{i_1}\cap E_{i_2}\cap\cdots\cap E_{i_m})
=
p(E_{i_1})p(E_{i_2})\cdots p(E_{i_m})
$$

其中：

$$
2\le m\le n
$$

两两独立不一定推出相互独立。

---

# Bernoulli 试验

tags: 7.2 Probability Theory

hint:
只有成功和失败两种结果的实验叫什么？

content:
Bernoulli 试验是只有两个可能结果的随机实验，通常称为成功和失败。

设成功概率为：

$$
p
$$

失败概率为：

$$
q=1-p
$$

一次抛硬币、一次检测是否命中、一次试验是否通过，都可建模为 Bernoulli 试验。

---

# 独立 Bernoulli 试验中恰好 $k$ 次成功

tags: 7.2 Probability Theory

hint:
$n$ 次独立 Bernoulli 试验中恰好 $k$ 次成功的概率是什么？

content:
设进行 $n$ 次独立 Bernoulli 试验，每次成功概率为 $p$，失败概率为：

$$
q=1-p
$$

则恰好有 $k$ 次成功的概率为：

$$
\binom{n}{k}p^kq^{n-k}
$$

其中：

$$
0\le k\le n
$$

组合因子 $\binom{n}{k}$ 表示选择哪 $k$ 次试验成功。

---

# 二项分布

tags: 7.2 Probability Theory

hint:
独立 Bernoulli 试验的成功次数服从什么分布？

content:
若随机变量 $X$ 表示 $n$ 次独立 Bernoulli 试验中的成功次数，每次成功概率为 $p$，则 $X$ 服从二项分布。

其概率为：

$$
p(X=k)=\binom{n}{k}p^k(1-p)^{n-k}
$$

其中：

$$
k=0,1,\ldots,n
$$

---

# 随机变量

tags: 7.2 Probability Theory

hint:
如何把实验结果转化为数值？

content:
随机变量是定义在样本空间上的函数，它把每个实验结果映射到一个实数。

若样本空间为 $S$，随机变量 $X$ 是函数：

$$
X:S\to\mathbb{R}
$$

例如，掷两颗骰子时，可以定义 $X$ 为两点数之和，也可以定义 $X$ 为较大的点数。

---

# 随机变量的分布

tags: 7.2 Probability Theory

hint:
随机变量取每个值的概率怎样记录？

content:
随机变量 $X$ 的分布由所有可能值及其概率组成。

若 $r$ 是 $X$ 可能取到的值，则记录：

$$
(r,\ p(X=r))
$$

其中：

$$
p(X=r)=p(\{s\in S\mid X(s)=r\})
$$

分布描述了随机变量各个数值出现的概率。

---

# 生日问题的补事件思路

tags: 7.2 Probability Theory

hint:
求“至少两人生日相同”时，为什么先求“生日全不同”？

content:
生日问题常用补事件法。

设 $n$ 个人的生日独立且均匀分布在 $366$ 天中。

先计算所有人生日都不同的概率：

$$
p_n=
\frac{365}{366}
\cdot
\frac{364}{366}
\cdots
\frac{367-n}{366}
$$

则至少两人生日相同的概率为：

$$
1-p_n
$$

补事件通常比直接统计“至少一对相同”更容易。

---

# 贝叶斯定理

tags: 7.3 Bayes’ Theorem

hint:
如何由 $p(E\mid F)$ 反推出 $p(F\mid E)$？

content:
设 $E$ 和 $F$ 是样本空间中的事件，并且：

$$
p(E)>0,\qquad p(F)>0
$$

贝叶斯定理为：

$$
p(F\mid E)=
\frac{p(E\mid F)p(F)}
{p(E\mid F)p(F)+p(E\mid \overline{F})p(\overline{F})}
$$

它用于在观察到证据 $E$ 后，更新事件 $F$ 发生的概率。

---

# 贝叶斯定理的推导结构

tags: 7.3 Bayes’ Theorem

hint:
贝叶斯公式为什么成立？

content:
由条件概率可知：

$$
p(F\mid E)=\frac{p(E\cap F)}{p(E)}
$$

又有：

$$
p(E\cap F)=p(E\mid F)p(F)
$$

并且 $E$ 可以分解为互斥两部分：

$$
E=(E\cap F)\cup(E\cap\overline{F})
$$

所以：

$$
p(E)=p(E\mid F)p(F)+p(E\mid\overline{F})p(\overline{F})
$$

代入即可得到贝叶斯定理。

---

# 贝叶斯定理的扩展形式

tags: 7.3 Bayes’ Theorem

hint:
若样本空间被多个互斥事件分割，怎样更新其中一个事件的概率？

content:
设事件：

$$
F_1,F_2,\ldots,F_n
$$

两两互斥，且：

$$
\bigcup_{i=1}^{n}F_i=S
$$

若：

$$
p(E)>0
$$

且每个：

$$
p(F_i)>0
$$

则：

$$
p(F_j\mid E)=
\frac{p(E\mid F_j)p(F_j)}
{\sum_{i=1}^{n}p(E\mid F_i)p(F_i)}
$$

这是贝叶斯定理的扩展形式。

---

# 先验概率与后验概率

tags: 7.3 Bayes’ Theorem

hint:
贝叶斯更新前后的概率分别叫什么？

content:
在贝叶斯定理中，事件 $F$ 原本的概率：

$$
p(F)
$$

称为先验概率。

观察到证据 $E$ 后，更新得到的概率：

$$
p(F\mid E)
$$

称为后验概率。

贝叶斯推理的核心是：用新证据把先验概率更新为后验概率。

---

# 贝叶斯分类思想

tags: 7.3 Bayes’ Theorem

hint:
贝叶斯方法怎样用于根据特征判断类别？

content:
贝叶斯分类根据观察到的特征或证据，计算每个类别的后验概率。

若类别为 $F_i$，证据为 $E$，则计算：

$$
p(F_i\mid E)
$$

并选择后验概率最大的类别。

例如，垃圾邮件过滤可以把“邮件是垃圾邮件”作为事件，把邮件中出现某些词作为证据，用贝叶斯定理估计邮件属于垃圾邮件的概率。

---

# 朴素贝叶斯假设

tags: 7.3 Bayes’ Theorem

hint:
为什么垃圾邮件过滤中常假设不同词的出现相互独立？

content:
朴素贝叶斯方法假设：在给定类别的条件下，各个特征近似独立。

若证据由多个词出现事件组成：

$$
E_1,E_2,\ldots,E_n
$$

并且在类别 $F$ 下条件独立，则：

$$
p(E_1\cap E_2\cap\cdots\cap E_n\mid F)
=
\prod_{i=1}^{n}p(E_i\mid F)
$$

这种假设简化了贝叶斯计算，虽然在现实中不一定完全成立。

---

# 随机变量的期望

tags: 7.4 Expected Value and Variance

hint:
期望为什么是加权平均？

content:
设 $X$ 是样本空间 $S$ 上的随机变量。$X$ 的期望或平均值定义为：

$$
E(X)=\sum_{s\in S}p(s)X(s)
$$

它是随机变量取值的加权平均，每个取值按对应结果的概率加权。

若样本空间为：

$$
S=\{x_1,x_2,\ldots,x_n\}
$$

则：

$$
E(X)=\sum_{i=1}^{n}p(x_i)X(x_i)
$$

---

# 按随机变量取值计算期望

tags: 7.4 Expected Value and Variance

hint:
如果多个结果对应同一个随机变量值，怎样简化期望计算？

content:
若 $X$ 的可能取值组成集合 $X(S)$，则期望也可按随机变量的取值分组计算：

$$
E(X)=\sum_{r\in X(S)}p(X=r)r
$$

这种形式避免逐个枚举样本空间中的每个结果，适合多个结果对应同一个数值的情形。

---

# 偏差

tags: 7.4 Expected Value and Variance

hint:
随机变量某次取值离平均值有多远？

content:
随机变量 $X$ 在结果 $s$ 处的偏差定义为：

$$
X(s)-E(X)
$$

它表示该结果下随机变量取值与平均值之间的差。

偏差可以为正、为负或为零。

方差通过偏差平方的期望来度量取值分散程度。

---

# 期望的线性性质

tags: 7.4 Expected Value and Variance

hint:
期望能否拆开求和？是否需要独立性？

content:
若 $X_1,X_2,\ldots,X_n$ 是同一样本空间上的随机变量，则：

$$
E(X_1+X_2+\cdots+X_n)
=
E(X_1)+E(X_2)+\cdots+E(X_n)
$$

对常数 $a,b$，有：

$$
E(aX+b)=aE(X)+b
$$

期望的线性性不要求随机变量相互独立。

---

# Bernoulli 成功次数的期望

tags: 7.4 Expected Value and Variance

hint:
$n$ 次 Bernoulli 试验中，成功次数的平均值是多少？

content:
设 $X$ 表示 $n$ 次 Bernoulli 试验中的成功次数，每次成功概率为 $p$。

可以把 $X$ 写成指示随机变量之和：

$$
X=X_1+X_2+\cdots+X_n
$$

其中 $X_i=1$ 表示第 $i$ 次成功，$X_i=0$ 表示失败。

因为：

$$
E(X_i)=p
$$

所以：

$$
E(X)=np
$$

---

# 平均情况复杂度作为期望

tags: 7.4 Expected Value and Variance

hint:
算法的平均比较次数如何用随机变量表示？

content:
平均情况复杂度可以看作随机变量的期望。

设样本空间为所有可能输入，$X(s)$ 表示算法在输入 $s$ 上使用的基本操作次数。

若输入 $s$ 的概率为 $p(s)$，则平均操作次数为：

$$
E(X)=\sum_{s\in S}p(s)X(s)
$$

因此，平均情况分析需要明确输入的概率分布。

---

# 指示随机变量

tags: 7.4 Expected Value and Variance

hint:
事件是否发生可以怎样变成随机变量？

content:
给定事件 $A$，其指示随机变量 $I_A$ 定义为：

$$
I_A(s)=
\begin{cases}
1, & s\in A,\\
0, & s\notin A.
\end{cases}
$$

其期望为：

$$
E(I_A)=p(A)
$$

指示随机变量常与期望线性性结合，用来计算“满足某性质的对象个数”的期望。

---

# 几何分布

tags: 7.4 Expected Value and Variance

hint:
第一次成功发生在第 $k$ 次试验的概率是什么？

content:
若随机变量 $X$ 表示独立 Bernoulli 试验中第一次成功出现所需的试验次数，且每次成功概率为 $p$，则 $X$ 服从参数为 $p$ 的几何分布。

其概率为：

$$
p(X=k)=(1-p)^{k-1}p
$$

其中：

$$
k=1,2,3,\ldots
$$

---

# 几何分布的期望

tags: 7.4 Expected Value and Variance

hint:
第一次成功平均需要多少次试验？

content:
若随机变量 $X$ 服从参数为 $p$ 的几何分布，则：

$$
E(X)=\frac{1}{p}
$$

也就是说，若每次试验成功概率为 $p$，则平均需要：

$$
\frac{1}{p}
$$

次试验才出现第一次成功。

---

# 独立随机变量

tags: 7.4 Expected Value and Variance

hint:
两个随机变量独立意味着它们取值的联合概率如何分解？

content:
随机变量 $X$ 和 $Y$ 独立，当且仅当对所有实数 $r_1,r_2$，都有：

$$
p(X=r_1\land Y=r_2)=p(X=r_1)p(Y=r_2)
$$

独立随机变量的任意取值事件相互独立。

---

# 独立随机变量乘积的期望

tags: 7.4 Expected Value and Variance

hint:
独立时，$E(XY)$ 与 $E(X)E(Y)$ 有什么关系？

content:
若随机变量 $X$ 和 $Y$ 独立，则：

$$
E(XY)=E(X)E(Y)
$$

注意：期望的加法线性性不需要独立性，但乘积期望分解通常需要独立性。

---

# 方差

tags: 7.4 Expected Value and Variance

hint:
怎样度量随机变量围绕期望的分散程度？

content:
随机变量 $X$ 的方差定义为偏差平方的期望：

$$
V(X)=\sum_{s\in S}(X(s)-E(X))^2p(s)
$$

方差越大，说明随机变量取值越分散。

方差永远非负：

$$
V(X)\ge0
$$

---

# 方差的计算公式

tags: 7.4 Expected Value and Variance

hint:
方差如何用 $E(X^2)$ 简化计算？

content:
方差也可写为：

$$
V(X)=E(X^2)-E(X)^2
$$

其中：

$$
E(X^2)=\sum_{s\in S}p(s)X(s)^2
$$

该公式常比直接使用偏差平方定义更方便。

---

# 标准差

tags: 7.4 Expected Value and Variance

hint:
方差开平方得到什么？

content:
随机变量 $X$ 的标准差定义为方差的平方根：

$$
\sigma(X)=\sqrt{V(X)}
$$

标准差与随机变量本身具有相同量纲，常用于描述随机变量偏离平均值的典型程度。

---

# Bernoulli 随机变量的方差

tags: 7.4 Expected Value and Variance

hint:
一次成功失败试验的方差是多少？

content:
设 $X$ 是 Bernoulli 随机变量：

$$
X=
\begin{cases}
1, & \text{success},\\
0, & \text{failure}.
\end{cases}
$$

若成功概率为 $p$，失败概率为：

$$
q=1-p
$$

则：

$$
E(X)=p
$$

且：

$$
V(X)=pq=p(1-p)
$$

---

# 独立随机变量和的方差

tags: 7.4 Expected Value and Variance

hint:
独立随机变量相加时，方差是否相加？

content:
若随机变量 $X$ 和 $Y$ 独立，则：

$$
V(X+Y)=V(X)+V(Y)
$$

更一般地，若：

$$
X_1,X_2,\ldots,X_n
$$

两两独立，则：

$$
V(X_1+X_2+\cdots+X_n)
=
V(X_1)+V(X_2)+\cdots+V(X_n)
$$

这个公式称为 Bienaymé 公式。

---

# Bernoulli 成功次数的方差

tags: 7.4 Expected Value and Variance

hint:
$n$ 次独立 Bernoulli 试验中成功次数的方差是多少？

content:
设 $X$ 表示 $n$ 次独立 Bernoulli 试验中的成功次数，每次成功概率为 $p$，失败概率为：

$$
q=1-p
$$

把 $X$ 写成 $n$ 个独立指示变量之和：

$$
X=X_1+X_2+\cdots+X_n
$$

每个 $X_i$ 的方差为：

$$
pq
$$

因此：

$$
V(X)=npq
$$

---

# 切比雪夫不等式

tags: 7.4 Expected Value and Variance

hint:
随机变量偏离均值至少 $r$ 的概率有什么上界？

content:
设随机变量 $X$ 有期望 $E(X)$ 和方差 $V(X)$，且 $r>0$。切比雪夫不等式为：

$$
p(|X-E(X)|\ge r)\le \frac{V(X)}{r^2}
$$

它说明：方差越小，随机变量远离均值的概率越小。

该不等式不要求知道随机变量的完整分布。

---

# 概率算法

tags: 7.4 Expected Value and Variance

hint:
Monte Carlo algorithm 与普通确定性算法有什么不同？

content:
概率算法是在一个或多个步骤中使用随机选择的算法，也称 Monte Carlo 算法。

它可能在很短时间内给出答案，但允许存在小概率错误。

概率算法适合某些确定性精确算法过慢的问题，例如某些素性测试。

分析概率算法时，需要估计错误概率和运行时间。

---

# 概率方法

tags: 7.4 Expected Value and Variance

hint:
怎样用“概率大于零”证明对象存在？

content:
概率方法是一种存在性证明技巧。

基本思路：

1. 在某个有限对象集合上定义概率分布；
2. 考察对象满足某性质的事件 $E$；
3. 证明：

$$
p(E)>0
$$

4. 因此至少存在一个对象满足该性质。

概率方法通常不直接构造对象，但能证明对象必然存在。

---

# 第七章核心思想

tags: 7.4 Expected Value and Variance

hint:
离散概率这一章的工具链是什么？

content:
第七章的核心是把计数、事件和随机变量结合起来。

主要内容包括：

1. 用样本空间和事件描述随机实验；
2. 在等可能情形下用计数计算概率；
3. 在一般情形下用概率分布指定每个结果的概率；
4. 用条件概率和独立性分析事件关系；
5. 用 Bernoulli 试验和二项分布处理重复试验；
6. 用贝叶斯定理根据证据更新概率；
7. 用期望描述随机变量的平均值；
8. 用方差和标准差描述随机变量的分散程度；
9. 用概率方法和概率算法处理存在性与计算问题。

---

# 递推关系

tags: 8.1 Applications of Recurrence Relations

hint:
递推关系用什么来定义序列的后续项？

content:
递推关系是用序列前面若干项来表示后续项的等式。

若序列为：

$$
a_0,a_1,a_2,\ldots
$$

一个递推关系可能形如：

$$
a_n=F(a_{n-1},a_{n-2},\ldots)
$$

递推关系本身通常不足以唯一确定序列，还需要初始条件。

---

# 初始条件

tags: 8.1 Applications of Recurrence Relations

hint:
为什么递推关系还需要起始项？

content:
初始条件是在递推关系生效前给出的若干项的值。

例如递推关系：

$$
a_n=2a_{n-1}
$$

配合初始条件：

$$
a_0=5
$$

唯一确定序列：

$$
a_n=5\cdot2^n
$$

没有初始条件时，同一个递推关系可能对应多个不同序列。

---

# 递推关系建模计数问题

tags: 8.1 Applications of Recurrence Relations

hint:
什么时候适合用递推关系解决计数问题？

content:
当一个规模为 $n$ 的计数问题可以由规模更小的问题得到时，适合建立递推关系。

基本步骤：

1. 定义 $a_n$ 为规模 $n$ 的对象数量；
2. 按照对象的最后一步、第一步或结构特征分类；
3. 把 $a_n$ 表示为较小规模项的函数；
4. 给出足够的初始条件；
5. 求解递推关系或用递推关系计算数值。

---

# 兔子问题与斐波那契数

tags: 8.1 Applications of Recurrence Relations

hint:
每月新兔子对数为什么会导致斐波那契递推？

content:
设 $f_n$ 表示第 $n$ 个月兔子对数。

在经典兔子模型中，新生兔子需要两个月成熟，成熟后每月产生一对新兔子，并且兔子不死亡。

此时第 $n$ 个月兔子对数等于前一个月已有兔子对数加上新生兔子对数。

新生兔子数等于第 $n-2$ 个月已经存在的兔子对数，因此：

$$
f_n=f_{n-1}+f_{n-2}
$$

配合适当初始条件得到斐波那契数列。

---

# 汉诺塔递推关系

tags: 8.1 Applications of Recurrence Relations

hint:
移动 $n$ 个盘子为什么需要先移动 $n-1$ 个盘子？

content:
设 $H_n$ 表示把 $n$ 个盘子从一根柱子移动到另一根柱子所需的最少步数。

移动 $n$ 个盘子需要：

1. 先把上面 $n-1$ 个盘子移到辅助柱；
2. 把最大盘子移动到目标柱；
3. 再把 $n-1$ 个盘子移到目标柱。

因此：

$$
H_n=2H_{n-1}+1
$$

初始条件为：

$$
H_1=1
$$

解得：

$$
H_n=2^n-1
$$

---

# 不含连续零的 bit string

tags: 8.1 Applications of Recurrence Relations

hint:
按最后一位或最后两位分类，怎样得到斐波那契型递推？

content:
设 $a_n$ 表示长度为 $n$ 且不含两个连续 $0$ 的 bit string 数量。

若合法串以 $1$ 结尾，则前 $n-1$ 位可为任意合法串，有 $a_{n-1}$ 种。

若合法串以 $0$ 结尾，则倒数第二位必须是 $1$，前 $n-2$ 位可为任意合法串，有 $a_{n-2}$ 种。

因此：

$$
a_n=a_{n-1}+a_{n-2}
$$

初始条件为：

$$
a_1=2,\qquad a_2=3
$$

---

# 不含连续一的 bit string

tags: 8.1 Applications of Recurrence Relations

hint:
“不含连续 1”与“不含连续 0”的计数为什么相同？

content:
设 $b_n$ 表示长度为 $n$ 且不含两个连续 $1$ 的 bit string 数量。

若合法串以 $0$ 结尾，则前 $n-1$ 位可为任意合法串，有 $b_{n-1}$ 种。

若合法串以 $1$ 结尾，则倒数第二位必须是 $0$，前 $n-2$ 位可为任意合法串，有 $b_{n-2}$ 种。

因此：

$$
b_n=b_{n-1}+b_{n-2}
$$

该递推与不含连续零的情形相同，只是 $0$ 和 $1$ 的角色互换。

---

# 偶数个零的十进制代码字

tags: 8.1 Applications of Recurrence Relations

hint:
添加一位数字时，零的个数奇偶性会怎样变化？

content:
设 $a_n$ 表示长度为 $n$ 且包含偶数个 $0$ 的十进制代码字数量。

构造长度为 $n$ 的合法代码字：

1. 在长度 $n-1$ 的合法代码字后添加非零数字，有 $9a_{n-1}$ 种；
2. 在长度 $n-1$ 的含奇数个 $0$ 的代码字后添加 $0$。

长度为 $n-1$ 的所有十进制代码字共有：

$$
10^{n-1}
$$

个，因此含奇数个 $0$ 的代码字有：

$$
10^{n-1}-a_{n-1}
$$

个。

所以：

$$
a_n=9a_{n-1}+(10^{n-1}-a_{n-1})=8a_{n-1}+10^{n-1}
$$

初始条件为：

$$
a_1=9
$$

---

# Catalan 数递推

tags: 8.1 Applications of Recurrence Relations

hint:
某些对象分解成左右两部分时，会得到怎样的卷积递推？

content:
Catalan 数常记为：

$$
C_n
$$

它满足递推关系：

$$
C_n=\sum_{k=0}^{n-1}C_kC_{n-1-k}
$$

初始条件为：

$$
C_0=1
$$

这种递推常出现在可以把对象分解为左侧规模 $k$ 和右侧规模 $n-1-k$ 的问题中。

Catalan 数计数的对象包括括号化方式、某些路径、满二叉树等。

---

# 动态规划

tags: 8.1 Applications of Recurrence Relations

hint:
dynamic programming 如何利用重叠子问题？

content:
动态规划是一种算法范式，用于把问题分解为相互重叠的子问题，并用递推关系由子问题解组合出原问题解。

动态规划通常包含：

1. 定义状态；
2. 建立状态转移递推式；
3. 给出初始状态；
4. 按合适顺序保存并计算子问题结果；
5. 返回目标状态的结果。

动态规划通过存储子问题答案避免重复计算。

---

# 动态规划与贪心算法的区别

tags: 8.1 Applications of Recurrence Relations

hint:
动态规划为什么不只做当前看起来最好的选择？

content:
贪心算法每一步做当前最优选择，而动态规划通常比较多个可能选择，并保存子问题最优解。

动态规划适合满足最优子结构且具有重叠子问题的优化问题。

若局部最优选择不一定导向全局最优，动态规划往往比贪心算法更可靠。

---

# 加权活动安排递推

tags: 8.1 Applications of Recurrence Relations

hint:
想最大化总参加人数时，当前活动选或不选如何递推？

content:
设活动按结束时间排序。令 $T(j)$ 表示前 $j$ 个活动能够得到的最大总权重。

设第 $j$ 个活动权重为 $w_j$，并令 $p(j)$ 表示与第 $j$ 个活动兼容且编号最大的前置活动。

则递推关系为：

$$
T(j)=\max(T(j-1),\ w_j+T(p(j)))
$$

其中：

$$
T(0)=0
$$

第一项表示不选第 $j$ 个活动，第二项表示选择第 $j$ 个活动。

---

# 线性齐次常系数递推关系

tags: 8.2 Solving Linear Recurrence Relations

hint:
什么样的递推关系叫 linear homogeneous recurrence relation？

content:
$k$ 阶线性齐次常系数递推关系形如：

$$
a_n=c_1a_{n-1}+c_2a_{n-2}+\cdots+c_ka_{n-k}
$$

其中：

$$
c_1,c_2,\ldots,c_k
$$

为常数，并且：

$$
c_k\ne0
$$

称其为 $k$ 阶，是因为 $a_n$ 由前 $k$ 项线性表示。

---

# 初始条件唯一确定解

tags: 8.2 Solving Linear Recurrence Relations

hint:
$k$ 阶线性递推需要多少个初始条件？

content:
$k$ 阶线性递推关系需要 $k$ 个初始条件来唯一确定一个序列。

若递推关系为：

$$
a_n=c_1a_{n-1}+c_2a_{n-2}+\cdots+c_ka_{n-k}
$$

则常给出：

$$
a_0=C_0,\ a_1=C_1,\ \ldots,\ a_{k-1}=C_{k-1}
$$

有了递推关系和这些初始条件，序列所有后续项都被唯一确定。

---

# 特征方程

tags: 8.2 Solving Linear Recurrence Relations

hint:
如何从线性齐次递推关系得到多项式方程？

content:
对线性齐次常系数递推关系：

$$
a_n=c_1a_{n-1}+c_2a_{n-2}+\cdots+c_ka_{n-k}
$$

构造特征方程：

$$
r^k-c_1r^{k-1}-c_2r^{k-2}-\cdots-c_{k-1}r-c_k=0
$$

该方程的根称为特征根。

特征根决定递推关系解的形式。

---

# 一阶线性齐次递推

tags: 8.2 Solving Linear Recurrence Relations

hint:
$a_n=ca_{n-1}$ 的通解是什么？

content:
一阶线性齐次递推关系：

$$
a_n=ca_{n-1}
$$

的解为：

$$
a_n=a_0c^n
$$

若初始条件为：

$$
a_0=A
$$

则：

$$
a_n=Ac^n
$$

这是等比数列。

---

# 二阶线性齐次递推的不同特征根

tags: 8.2 Solving Linear Recurrence Relations

hint:
二阶递推若有两个不同特征根，通解是什么？

content:
设二阶线性齐次递推关系为：

$$
a_n=c_1a_{n-1}+c_2a_{n-2}
$$

其特征方程为：

$$
r^2-c_1r-c_2=0
$$

若有两个不同特征根：

$$
r_1,\quad r_2
$$

则通解为：

$$
a_n=\alpha_1r_1^n+\alpha_2r_2^n
$$

常数 $\alpha_1,\alpha_2$ 由初始条件确定。

---

# 二阶线性齐次递推的重根

tags: 8.2 Solving Linear Recurrence Relations

hint:
二阶递推若特征方程有重根，为什么要多乘一个 $n$？

content:
设二阶线性齐次递推的特征方程有重根：

$$
r
$$

则通解为：

$$
a_n=\alpha_1r^n+\alpha_2nr^n
$$

也可写作：

$$
a_n=(\alpha_1+\alpha_2n)r^n
$$

常数由初始条件确定。

重根使得需要加入 $n r^n$ 这一线性独立解。

---

# 高阶线性齐次递推的不同特征根

tags: 8.2 Solving Linear Recurrence Relations

hint:
$k$ 个不同特征根时，解怎样组合？

content:
若 $k$ 阶线性齐次常系数递推关系的特征方程有 $k$ 个不同特征根：

$$
r_1,r_2,\ldots,r_k
$$

则通解为：

$$
a_n=\alpha_1r_1^n+\alpha_2r_2^n+\cdots+\alpha_kr_k^n
$$

常数：

$$
\alpha_1,\alpha_2,\ldots,\alpha_k
$$

由 $k$ 个初始条件确定。

---

# 高阶线性齐次递推的重根形式

tags: 8.2 Solving Linear Recurrence Relations

hint:
如果某个特征根重数为 $m$，对应解项有哪些？

content:
若特征根 $r$ 的重数为 $m$，则它在通解中贡献：

$$
\alpha_0r^n+\alpha_1nr^n+\alpha_2n^2r^n+\cdots+\alpha_{m-1}n^{m-1}r^n
$$

也可写为：

$$
(\alpha_0+\alpha_1n+\cdots+\alpha_{m-1}n^{m-1})r^n
$$

对每个不同特征根分别写出对应部分，再求和得到总通解。

---

# 线性非齐次常系数递推关系

tags: 8.2 Solving Linear Recurrence Relations

hint:
非齐次递推比齐次递推多了什么项？

content:
线性非齐次常系数递推关系形如：

$$
a_n=c_1a_{n-1}+c_2a_{n-2}+\cdots+c_ka_{n-k}+F(n)
$$

其中：

$$
F(n)
$$

不是恒等于零的函数。

与之对应的齐次递推关系为：

$$
a_n=c_1a_{n-1}+c_2a_{n-2}+\cdots+c_ka_{n-k}
$$

---

# 非齐次递推的通解结构

tags: 8.2 Solving Linear Recurrence Relations

hint:
非齐次递推的解由哪两部分组成？

content:
线性非齐次递推的通解等于齐次通解加上一个特解。

若：

$$
a_n^{(h)}
$$

是对应齐次递推的通解，而：

$$
a_n^{(p)}
$$

是非齐次递推的一个特解，则非齐次递推的通解为：

$$
a_n=a_n^{(h)}+a_n^{(p)}
$$

最后用初始条件确定齐次解中的常数。

---

# 非齐次递推的特解猜测形式

tags: 8.2 Solving Linear Recurrence Relations

hint:
若非齐次项是多项式乘指数，特解怎样猜？

content:
若非齐次项形如：

$$
F(n)=(b_tn^t+b_{t-1}n^{t-1}+\cdots+b_0)s^n
$$

则可尝试特解：

$$
a_n^{(p)}=(p_tn^t+p_{t-1}n^{t-1}+\cdots+p_0)s^n
$$

若 $s$ 是对应齐次特征方程的重根，重数为 $m$，则需要乘以：

$$
n^m
$$

即猜测：

$$
a_n^{(p)}=n^m(p_tn^t+p_{t-1}n^{t-1}+\cdots+p_0)s^n
$$

---

# 斐波那契数的闭式解

tags: 8.2 Solving Linear Recurrence Relations

hint:
斐波那契递推的特征根是什么？

content:
斐波那契递推为：

$$
f_n=f_{n-1}+f_{n-2}
$$

特征方程为：

$$
r^2-r-1=0
$$

其两个根为：

$$
\alpha=\frac{1+\sqrt5}{2}
$$

和：

$$
\beta=\frac{1-\sqrt5}{2}
$$

因此斐波那契数可写为：

$$
f_n=\frac{\alpha^n-\beta^n}{\sqrt5}
$$

在初始条件 $f_0=0,\ f_1=1$ 下成立。

---

# 分治算法

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
divide-and-conquer 怎样分解问题？

content:
分治算法把原问题递归地分解为若干个更小的、互不重叠的同类型子问题。

一般过程：

1. Divide：把规模 $n$ 的问题分成若干规模较小的子问题；
2. Conquer：递归解决这些子问题；
3. Combine：把子问题解合并成原问题解。

分治适合二分搜索、归并排序、快速乘法等问题。

---

# 分治递推关系

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
规模 $n$ 问题分成 $a$ 个规模 $n/b$ 子问题时，复杂度怎样递推？

content:
若一个分治算法把规模 $n$ 的问题分成：

$$
a
$$

个规模为：

$$
\frac{n}{b}
$$

的子问题，并且合并或额外处理需要：

$$
g(n)
$$

次操作，则复杂度函数 $f(n)$ 满足：

$$
f(n)=af\left(\frac{n}{b}\right)+g(n)
$$

这称为分治递推关系。

---

# 展开分治递推

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
如何通过反复代入展开 $f(n)=af(n/b)+g(n)$？

content:
设：

$$
n=b^k
$$

若：

$$
f(n)=af\left(\frac{n}{b}\right)+g(n)
$$

则反复代入得到：

$$
f(n)=a^k f(1)+\sum_{j=0}^{k-1}a^j g\left(\frac{n}{b^j}\right)
$$

其中：

$$
k=\log_b n
$$

该公式可用于估计许多分治算法的复杂度。

---

# 二分搜索的分治递推

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
二分搜索每次保留多少规模的子问题？

content:
二分搜索每次比较中间元素后，只需继续搜索原列表的一半。

若 $f(n)$ 表示比较次数，则递推关系可写为：

$$
f(n)=f\left(\frac{n}{2}\right)+1
$$

基础条件为：

$$
f(1)=1
$$

因此：

$$
f(n)=O(\log n)
$$

---

# 归并排序的分治递推

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
归并排序为什么有两个半规模子问题和线性合并代价？

content:
归并排序把长度为 $n$ 的列表分成两个长度约为 $n/2$ 的子列表，分别排序后再线性合并。

因此比较或操作次数满足递推形式：

$$
f(n)=2f\left(\frac{n}{2}\right)+O(n)
$$

由主定理可得：

$$
f(n)=O(n\log n)
$$

更精确地，在常见模型下归并排序具有：

$$
\Theta(n\log n)
$$

复杂度。

---

# 分治递推的主定理形式

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
$f(n)=af(n/b)+cn^d$ 的增长阶由什么比较决定？

content:
设：

$$
f(n)=af\left(\frac{n}{b}\right)+cn^d
$$

其中：

$$
a\ge1,\quad b>1,\quad c>0,\quad d\ge0
$$

并假设：

$$
n=b^k
$$

比较 $a$ 与 $b^d$：

若：

$$
a<b^d
$$

则：

$$
f(n)=O(n^d)
$$

若：

$$
a=b^d
$$

则：

$$
f(n)=O(n^d\log n)
$$

若：

$$
a>b^d
$$

则：

$$
f(n)=O(n^{\log_b a})
$$

---

# 主定理的直观意义

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
子问题总规模和合并代价谁主导最终复杂度？

content:
在递推：

$$
f(n)=af\left(\frac{n}{b}\right)+cn^d
$$

中，$a$ 表示子问题个数，$b$ 表示规模缩小因子，$n^d$ 表示每层额外工作。

若：

$$
a<b^d
$$

每层额外工作由顶层主导。

若：

$$
a=b^d
$$

每层贡献同阶，层数为：

$$
\log_b n
$$

若：

$$
a>b^d
$$

叶子层或子问题数量增长主导复杂度。

---

# 快速整数乘法的分治思想

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
如何把两个大整数乘法化为较小整数乘法？

content:
将两个 $2n$ 位整数拆成高位和低位：

$$
a=a_1 2^n+a_0
$$

$$
b=b_1 2^n+b_0
$$

普通分治乘法需要计算：

$$
a_1b_1,\quad a_1b_0,\quad a_0b_1,\quad a_0b_0
$$

共 $4$ 个半规模乘法。

更快的方法通过计算：

$$
(a_1+a_0)(b_1+b_0)
$$

结合 $a_1b_1$ 和 $a_0b_0$，只用 $3$ 个半规模乘法得到交叉项。

---

# Karatsuba 乘法复杂度

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
三个半规模乘法带来什么复杂度？

content:
快速整数乘法中，若一个规模 $n$ 的乘法问题被分成 $3$ 个规模 $n/2$ 的乘法子问题，并且合并只需线性额外工作，则复杂度满足：

$$
f(n)=3f\left(\frac{n}{2}\right)+O(n)
$$

由主定理：

$$
f(n)=O(n^{\log_2 3})
$$

其中：

$$
\log_2 3\approx1.585
$$

这比普通乘法的二次复杂度更好。

---

# 矩阵乘法的分治递推

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
把矩阵分块后，普通分治需要多少个子矩阵乘法？

content:
将两个 $n\times n$ 矩阵分成四个：

$$
\frac{n}{2}\times\frac{n}{2}
$$

子矩阵。

普通分块乘法需要计算 $8$ 个半规模矩阵乘法，并做若干矩阵加法。

因此复杂度递推为：

$$
f(n)=8f\left(\frac{n}{2}\right)+O(n^2)
$$

由主定理：

$$
f(n)=O(n^3)
$$

---

# Strassen 矩阵乘法思想

tags: 8.3 Divide-and-Conquer Algorithms and Recurrence Relations

hint:
如果半规模矩阵乘法从 $8$ 个降到 $7$ 个，复杂度如何变化？

content:
Strassen 算法通过巧妙组合子矩阵，只需 $7$ 个半规模矩阵乘法，而不是普通分块乘法的 $8$ 个。

其递推关系为：

$$
f(n)=7f\left(\frac{n}{2}\right)+O(n^2)
$$

由主定理：

$$
f(n)=O(n^{\log_2 7})
$$

其中：

$$
\log_2 7\approx2.807
$$

因此理论上快于普通矩阵乘法的 $O(n^3)$。

---

# 生成函数

tags: 8.4 Generating Functions

hint:
怎样用幂级数编码一个序列？

content:
序列：

$$
a_0,a_1,a_2,\ldots
$$

的普通生成函数定义为：

$$
G(x)=a_0+a_1x+a_2x^2+\cdots
$$

也可写作：

$$
G(x)=\sum_{k=0}^{\infty}a_kx^k
$$

生成函数把序列的第 $k$ 项编码为 $x^k$ 的系数。

---

# 有限序列的生成函数

tags: 8.4 Generating Functions

hint:
有限序列怎样看成生成函数？

content:
有限序列：

$$
a_0,a_1,\ldots,a_n
$$

可通过在后面补零变成无限序列：

$$
a_0,a_1,\ldots,a_n,0,0,\ldots
$$

其生成函数为多项式：

$$
G(x)=a_0+a_1x+\cdots+a_nx^n
$$

---

# 系数提取

tags: 8.4 Generating Functions

hint:
如何从生成函数中读出序列项？

content:
若：

$$
G(x)=\sum_{n=0}^{\infty}a_nx^n
$$

则 $a_n$ 是 $x^n$ 的系数。

常用记号：

$$
[x^n]G(x)=a_n
$$

生成函数方法的核心就是把计数问题转化为求某个幂级数中指定幂的系数。

---

# 基本生成函数

tags: 8.4 Generating Functions

hint:
全 1 序列和等比序列的生成函数是什么？

content:
全 $1$ 序列：

$$
1,1,1,\ldots
$$

的生成函数为：

$$
\frac{1}{1-x}=\sum_{n=0}^{\infty}x^n
$$

等比序列：

$$
1,a,a^2,a^3,\ldots
$$

的生成函数为：

$$
\frac{1}{1-ax}=\sum_{n=0}^{\infty}a^nx^n
$$

---

# 常用生成函数表

tags: 8.4 Generating Functions

hint:
几个基础序列对应哪些生成函数？

content:
常用生成函数包括：

$$
\sum_{n=0}^{\infty}x^n=\frac{1}{1-x}
$$

$$
\sum_{n=0}^{\infty}a^nx^n=\frac{1}{1-ax}
$$

$$
\sum_{n=0}^{\infty}(n+1)x^n=\frac{1}{(1-x)^2}
$$

更一般地：

$$
\sum_{n=0}^{\infty}\binom{n+k}{k}x^n=\frac{1}{(1-x)^{k+1}}
$$

---

# 生成函数的加法与数乘

tags: 8.4 Generating Functions

hint:
两个序列逐项相加时，生成函数怎样变化？

content:
若序列 $\{a_n\}$ 的生成函数为 $A(x)$，序列 $\{b_n\}$ 的生成函数为 $B(x)$，则序列：

$$
\{a_n+b_n\}
$$

的生成函数为：

$$
A(x)+B(x)
$$

对常数 $c$，序列：

$$
\{ca_n\}
$$

的生成函数为：

$$
cA(x)
$$

---

# 生成函数的移位

tags: 8.4 Generating Functions

hint:
序列前面补零，对生成函数有什么影响？

content:
若：

$$
G(x)=\sum_{n=0}^{\infty}a_nx^n
$$

则序列：

$$
0,a_0,a_1,a_2,\ldots
$$

的生成函数为：

$$
xG(x)
$$

更一般地，在序列前面补 $k$ 个零，相当于生成函数乘以：

$$
x^k
$$

---

# 生成函数乘法与卷积

tags: 8.4 Generating Functions

hint:
两个生成函数相乘时，系数怎样卷积？

content:
若：

$$
A(x)=\sum_{n=0}^{\infty}a_nx^n
$$

$$
B(x)=\sum_{n=0}^{\infty}b_nx^n
$$

则：

$$
A(x)B(x)=\sum_{n=0}^{\infty}c_nx^n
$$

其中：

$$
c_n=\sum_{k=0}^{n}a_kb_{n-k}
$$

这称为卷积。

---

# 用生成函数计数选择问题

tags: 8.4 Generating Functions

hint:
每类对象可选数量不同，怎样转化为生成函数乘积？

content:
若有若干类对象，第 $i$ 类可选择的数量集合为：

$$
S_i
$$

则为第 $i$ 类建立生成因子：

$$
\sum_{j\in S_i}x^j
$$

所有类别组合的生成函数为：

$$
\prod_i\left(\sum_{j\in S_i}x^j\right)
$$

其中 $x^r$ 的系数就是总共选择 $r$ 个对象的方式数。

---

# 用生成函数计数找零方式

tags: 8.4 Generating Functions

hint:
硬币找零问题的生成函数怎样写？

content:
若硬币面值为：

$$
d_1,d_2,\ldots,d_k
$$

且每种硬币数量不限，则找零方式的生成函数为：

$$
\prod_{i=1}^{k}\frac{1}{1-x^{d_i}}
$$

其中：

$$
[x^n]\prod_{i=1}^{k}\frac{1}{1-x^{d_i}}
$$

表示凑成金额 $n$ 的方式数。

---

# 扩展二项式系数

tags: 8.4 Generating Functions

hint:
当上标不是非负整数时，二项式系数怎样定义？

content:
对实数 $u$ 和非负整数 $k$，扩展二项式系数定义为：

$$
\binom{u}{k}=
\frac{u(u-1)(u-2)\cdots(u-k+1)}{k!}
$$

其中：

$$
\binom{u}{0}=1
$$

当 $u$ 是非负整数且 $k\le u$ 时，这与普通组合数一致。

---

# 扩展二项式定理

tags: 8.4 Generating Functions

hint:
$(1+x)^u$ 的幂级数展开是什么？

content:
扩展二项式定理说明：

$$
(1+x)^u=
\sum_{k=0}^{\infty}\binom{u}{k}x^k
$$

在形式幂级数意义下可用来展开许多生成函数。

例如：

$$
\frac{1}{(1-x)^m}=(1-x)^{-m}
$$

可用扩展二项式定理得到系数公式。

---

# 用生成函数解递推关系

tags: 8.4 Generating Functions

hint:
递推关系怎样转化为生成函数方程？

content:
用生成函数解递推关系的一般步骤：

1. 设：

$$
G(x)=\sum_{n=0}^{\infty}a_nx^n
$$

2. 将递推关系两边乘以 $x^n$；
3. 对适当的 $n$ 范围求和；
4. 用 $G(x)$ 表示各个求和式；
5. 解出 $G(x)$；
6. 展开 $G(x)$，读取 $a_n$ 的公式。

---

# 生成函数证明恒等式

tags: 8.4 Generating Functions

hint:
怎样通过比较系数证明组合恒等式？

content:
生成函数可用于证明组合恒等式。

基本方法：

1. 构造两个相等的函数表达式；
2. 将它们分别展开为幂级数；
3. 比较两边 $x^n$ 的系数；
4. 得到相应的组合恒等式。

例如，利用：

$$
(1+x)^{m+n}=(1+x)^m(1+x)^n
$$

比较 $x^r$ 的系数，可得范德蒙德恒等式。

---

# 指数生成函数

tags: 8.4 Generating Functions

hint:
指数生成函数和普通生成函数有什么不同？

content:
序列：

$$
a_0,a_1,a_2,\ldots
$$

的指数生成函数定义为：

$$
\sum_{n=0}^{\infty}a_n\frac{x^n}{n!}
$$

与普通生成函数相比，指数生成函数把第 $n$ 项放在：

$$
\frac{x^n}{n!}
$$

的系数中。

指数生成函数常用于排列型计数问题。

---

# 容斥原理：两个集合

tags: 8.5 Inclusion–Exclusion

hint:
两个集合的并集大小为什么要减去交集？

content:
两个有限集合 $A$ 与 $B$ 的并集大小为：

$$
|A\cup B|=|A|+|B|-|A\cap B|
$$

原因是交集中的元素在：

$$
|A|+|B|
$$

中被计算了两次，因此需要减去一次。

---

# 容斥原理：三个集合

tags: 8.5 Inclusion–Exclusion

hint:
三个集合的并集大小怎样加减？

content:
三个有限集合的容斥公式为：

$$
|A\cup B\cup C|
=
|A|+|B|+|C|
-|A\cap B|-|A\cap C|-|B\cap C|
+|A\cap B\cap C|
$$

先加上单个集合大小，再减去两两交集，最后加回三重交集。

---

# 一般容斥原理

tags: 8.5 Inclusion–Exclusion

hint:
$n$ 个集合的并集大小怎样交替加减？

content:
对有限集合：

$$
A_1,A_2,\ldots,A_n
$$

有：

$$
\left|\bigcup_{i=1}^{n}A_i\right|
=
\sum_{1\le i\le n}|A_i|
-
\sum_{1\le i<j\le n}|A_i\cap A_j|
+
\sum_{1\le i<j<k\le n}|A_i\cap A_j\cap A_k|
-\cdots
+(-1)^{n+1}|A_1\cap A_2\cap\cdots\cap A_n|
$$

规律是：奇数个集合的交集加，偶数个集合的交集减。

---

# 容斥公式的项数

tags: 8.5 Inclusion–Exclusion

hint:
$n$ 个集合的容斥公式有多少项？

content:
一般容斥公式中，每个非空子集都对应一个交集项。

$n$ 个集合的非空子集数为：

$$
2^n-1
$$

因此容斥公式共有：

$$
2^n-1
$$

项。

当 $n$ 很大时，直接展开容斥公式会非常长。

---

# 容斥原理的计数思想

tags: 8.5 Inclusion–Exclusion

hint:
为什么容斥公式最终每个元素只被计算一次？

content:
若某个元素属于恰好 $r$ 个集合，则它在容斥公式中被计算次数为：

$$
\binom{r}{1}-\binom{r}{2}+\binom{r}{3}-\cdots+(-1)^{r+1}\binom{r}{r}
$$

由二项式恒等式可知：

$$
\binom{r}{1}-\binom{r}{2}+\cdots+(-1)^{r+1}\binom{r}{r}=1
$$

因此每个属于并集的元素最终被恰好计算一次。

---

# 容斥原理的概率形式

tags: 8.5 Inclusion–Exclusion

hint:
事件并的概率是否也满足容斥公式？

content:
对事件：

$$
E_1,E_2,\ldots,E_n
$$

有概率形式的容斥公式：

$$
p\left(\bigcup_{i=1}^{n}E_i\right)
=
\sum_i p(E_i)
-\sum_{i<j}p(E_i\cap E_j)
+\sum_{i<j<k}p(E_i\cap E_j\cap E_k)
-\cdots
+(-1)^{n+1}p(E_1\cap E_2\cap\cdots\cap E_n)
$$

该公式与集合大小形式完全对应。

---

# 互斥情形下的容斥简化

tags: 8.5 Inclusion–Exclusion

hint:
如果事件两两互斥，容斥公式会变成什么？

content:
若集合或事件两两不相交，则所有两两交集及更高交集都为空。

因此容斥公式退化为直接相加。

集合形式：

$$
\left|\bigcup_{i=1}^{n}A_i\right|=\sum_{i=1}^{n}|A_i|
$$

概率形式：

$$
p\left(\bigcup_{i=1}^{n}E_i\right)=\sum_{i=1}^{n}p(E_i)
$$

---

# 容斥的补集策略

tags: 8.5 Inclusion–Exclusion

hint:
求“不具有任何坏性质”的对象数量时，怎样使用容斥？

content:
很多问题要求计算不具有任何坏性质的对象数量。

设全集大小为 $N$，性质 $P_i$ 对应的坏集合为 $A_i$。

不具有任何坏性质的对象数为：

$$
N-\left|\bigcup_{i=1}^{n}A_i\right|
$$

再对并集使用容斥原理即可。

这种方法常用于限制条件计数、错排、满射、筛法等问题。

---

# 不具备任何性质的容斥形式

tags: 8.6 Applications of Inclusion–Exclusion

hint:
若要数没有任何 $P_i$ 性质的对象，应怎样写公式？

content:
设全集大小为 $N$，$P_i$ 是性质，$N(P_{i_1}P_{i_2}\cdots P_{i_k})$ 表示同时具有这些性质的对象数。

则不具备任何性质 $P_1,P_2,\ldots,P_n$ 的对象数为：

$$
N(P_1'P_2'\cdots P_n')
=
N
-\sum_iN(P_i)
+\sum_{i<j}N(P_iP_j)
-\sum_{i<j<k}N(P_iP_jP_k)
+\cdots
+(-1)^nN(P_1P_2\cdots P_n)
$$

---

# 有上界的非负整数解

tags: 8.6 Applications of Inclusion–Exclusion

hint:
非负整数解再加上每个变量的上界时，如何使用容斥？

content:
要求：

$$
x_1+x_2+\cdots+x_n=r
$$

的非负整数解，并且附加上界：

$$
x_i\le u_i
$$

可先忽略上界，用 stars and bars 得到所有非负解：

$$
\binom{n+r-1}{r}
$$

再令 $P_i$ 表示违反第 $i$ 个上界：

$$
x_i\ge u_i+1
$$

然后用容斥原理减去至少一个上界被违反的解。

---

# 筛法与容斥

tags: 8.6 Applications of Inclusion–Exclusion

hint:
如何用容斥数出不被若干素数整除的整数？

content:
若要统计不超过 $N$ 的正整数中，不被素数：

$$
p_1,p_2,\ldots,p_k
$$

中任何一个整除的数，可以令 $P_i$ 表示“能被 $p_i$ 整除”。

同时被一组素数整除，等价于被这些素数的乘积整除。

因此对应数量为：

$$
\left\lfloor\frac{N}{p_{i_1}p_{i_2}\cdots p_{i_j}}\right\rfloor
$$

再用容斥原理计算不具备任何 $P_i$ 的整数数量。

---

# 用容斥计数素数

tags: 8.6 Applications of Inclusion–Exclusion

hint:
为什么只需筛掉不超过 $\sqrt N$ 的素因子？

content:
要找不超过 $N$ 的素数，合数一定有一个不超过：

$$
\sqrt N
$$

的素因子。

因此可以列出所有不超过 $\sqrt N$ 的素数：

$$
p_1,p_2,\ldots,p_k
$$

再用容斥原理统计大于 $1$ 且不被这些素数整除的数。

最后还要把这些小素数本身加入计数。

这就是埃拉托色尼筛法的容斥解释。

---

# 满射个数

tags: 8.6 Applications of Inclusion–Exclusion

hint:
从 $m$ 元集合到 $n$ 元集合的 onto functions 有多少个？

content:
设：

$$
m\ge n
$$

从 $m$ 元集合到 $n$ 元集合的满射个数为：

$$
\sum_{j=0}^{n}(-1)^j\binom{n}{j}(n-j)^m
$$

展开为：

$$
n^m-\binom{n}{1}(n-1)^m+\binom{n}{2}(n-2)^m-\cdots+(-1)^{n-1}\binom{n}{n-1}1^m
$$

这里用容斥排除“某些陪域元素没有原像”的函数。

---

# 满射与第二类斯特林数

tags: 8.6 Applications of Inclusion–Exclusion

hint:
满射个数和把对象分到非空不可区分盒子有什么关系？

content:
从 $m$ 元集合到 $n$ 元集合的满射个数也等于：

$$
n!S(m,n)
$$

其中：

$$
S(m,n)
$$

是第二类斯特林数，表示把 $m$ 个可区分对象划分为 $n$ 个非空不可区分盒子的方式数。

因而：

$$
S(m,n)=\frac{1}{n!}\sum_{j=0}^{n}(-1)^j\binom{n}{j}(n-j)^m
$$

---

# 工作分配问题

tags: 8.6 Applications of Inclusion–Exclusion

hint:
把 $m$ 个不同工作分给 $n$ 个不同员工且每人至少一个，等价于什么？

content:
把 $m$ 个不同工作分配给 $n$ 个不同员工，并要求每个员工至少得到一个工作，等价于计数从工作集合到员工集合的满射。

因此方式数为：

$$
\sum_{j=0}^{n}(-1)^j\binom{n}{j}(n-j)^m
$$

也可写成：

$$
n!S(m,n)
$$

---

# 错排

tags: 8.6 Applications of Inclusion–Exclusion

hint:
一个排列没有任何元素留在原位置，叫什么？

content:
错排是一个排列，使得没有任何对象留在它原来的位置。

若排列 $\pi$ 作用在：

$$
\{1,2,\ldots,n\}
$$

上，则错排满足：

$$
\pi(i)\ne i,\quad i=1,2,\ldots,n
$$

错排常用于帽子问题、信封错装问题等。

---

# 错排公式

tags: 8.6 Applications of Inclusion–Exclusion

hint:
$n$ 个对象的错排数如何由容斥公式得到？

content:
设 $D_n$ 表示 $n$ 个对象的错排数。令 $P_i$ 表示“第 $i$ 个对象留在原位置”。

由容斥原理：

$$
D_n
=
n!-\binom{n}{1}(n-1)!
+\binom{n}{2}(n-2)!
-\cdots
+(-1)^n\binom{n}{n}0!
$$

化简得：

$$
D_n=n!\sum_{j=0}^{n}\frac{(-1)^j}{j!}
$$

即：

$$
D_n=n!\left(1-\frac{1}{1!}+\frac{1}{2!}-\frac{1}{3!}+\cdots+(-1)^n\frac{1}{n!}\right)
$$

---

# 错排概率

tags: 8.6 Applications of Inclusion–Exclusion

hint:
随机排列中没有元素在原位的概率趋近于多少？

content:
随机排列 $n$ 个对象时，没有任何对象留在原位置的概率为：

$$
\frac{D_n}{n!}
=
\sum_{j=0}^{n}\frac{(-1)^j}{j!}
$$

当：

$$
n\to\infty
$$

时，该概率趋近于：

$$
e^{-1}
$$

约为：

$$
0.368
$$

---

# 错排递推关系

tags: 8.6 Applications of Inclusion–Exclusion

hint:
错排数除了容斥公式，还有哪些递推形式？

content:
错排数满足递推关系：

$$
D_n=(n-1)(D_{n-1}+D_{n-2})
$$

其中：

$$
n\ge2
$$

并有初始值：

$$
D_0=1,\qquad D_1=0
$$

另一个常用递推为：

$$
D_n=nD_{n-1}+(-1)^n
$$

这些递推可由组合讨论或容斥公式推出。

---

# 恰有 $k$ 个固定点的排列

tags: 8.6 Applications of Inclusion–Exclusion

hint:
如果要求恰好 $k$ 个元素留在原位置，怎样计数？

content:
要计数 $n$ 个对象的排列中恰好有 $k$ 个固定点：

先选择固定的 $k$ 个对象：

$$
\binom{n}{k}
$$

剩下 $n-k$ 个对象必须错排，有：

$$
D_{n-k}
$$

种。

因此总数为：

$$
\binom{n}{k}D_{n-k}
$$

特别地，$k=0$ 时得到错排数 $D_n$。

---

# 欧拉函数的容斥公式

tags: 8.6 Applications of Inclusion–Exclusion

hint:
如何用容斥计算不超过 $n$ 且与 $n$ 互素的整数个数？

content:
设正整数 $n$ 的不同素因子为：

$$
p_1,p_2,\ldots,p_k
$$

欧拉函数 $\phi(n)$ 表示不超过 $n$ 且与 $n$ 互素的正整数个数。

由容斥原理可得：

$$
\phi(n)=n\prod_{i=1}^{k}\left(1-\frac{1}{p_i}\right)
$$

因为需要排除所有能被 $p_i$ 中至少一个整除的整数。

---

# 第八章核心思想

tags: 8.6 Applications of Inclusion–Exclusion

hint:
高级计数技术这一章的工具链是什么？

content:
第八章的核心是用更强的结构化方法解决基础计数方法难以直接处理的问题。

主要工具包括：

1. 用递推关系建模计数问题；
2. 用线性递推的特征方程求闭式解；
3. 用动态规划保存重叠子问题结果；
4. 用分治递推分析递归算法复杂度；
5. 用生成函数把序列编码为幂级数；
6. 用系数提取解决选择、分配和递推问题；
7. 用容斥原理计数多个性质的并集；
8. 用容斥解决满射、筛法和错排问题。

---

# 二元关系

tags: 9.1 Relations and Their Properties

hint:
两个集合之间的关系如何用有序对集合表示？

content:
设 $A$ 和 $B$ 是集合。从 $A$ 到 $B$ 的二元关系是笛卡尔积：

$$
A\times B
$$

的一个子集。

若：

$$
R\subseteq A\times B
$$

则 $R$ 是从 $A$ 到 $B$ 的关系。

当：

$$
(a,b)\in R
$$

时，称 $a$ 与 $b$ 在关系 $R$ 下相关，记为：

$$
aRb
$$

当：

$$
(a,b)\notin R
$$

时，记为：

$$
a\not R b
$$

---

# 集合上的关系

tags: 9.1 Relations and Their Properties

hint:
一个集合内部元素之间的关系是什么子集？

content:
若 $A$ 是集合，$A$ 上的二元关系是：

$$
A\times A
$$

的子集。

也就是说：

$$
R\subseteq A\times A
$$

称为 $A$ 上的关系。

集合上的关系用于描述同一集合中元素之间的联系，例如相等、整除、同余、大小关系、可达关系等。

---

# 关系的数量

tags: 9.1 Relations and Their Properties

hint:
$n$ 元集合上共有多少个二元关系？

content:
若：

$$
|A|=n
$$

则：

$$
|A\times A|=n^2
$$

$A$ 上的每个关系都是 $A\times A$ 的一个子集。

因此，$A$ 上的二元关系总数为：

$$
2^{n^2}
$$

若：

$$
|A|=m,\quad |B|=n
$$

则从 $A$ 到 $B$ 的关系总数为：

$$
2^{mn}
$$

---

# 自反关系

tags: 9.1 Relations and Their Properties

hint:
每个元素都与自己相关时，关系具有什么性质？

content:
设 $R$ 是集合 $A$ 上的关系。

若对任意：

$$
a\in A
$$

都有：

$$
(a,a)\in R
$$

则称 $R$ 是自反的。

逻辑形式为：

$$
\forall a\in A,\ aRa
$$

自反关系必须包含所有对角线有序对：

$$
(a,a)
$$

---

# 非自反关系

tags: 9.1 Relations and Their Properties

hint:
没有任何元素与自己相关时，关系叫什么？

content:
设 $R$ 是集合 $A$ 上的关系。

若对任意：

$$
a\in A
$$

都有：

$$
(a,a)\notin R
$$

则称 $R$ 是非自反的，也称反自反的。

逻辑形式为：

$$
\forall a\in A,\ \neg(aRa)
$$

非自反不是“不是自反”的同义词。一个关系可以既不是自反，也不是非自反。

---

# 对称关系

tags: 9.1 Relations and Their Properties

hint:
$a$ 与 $b$ 相关时，$b$ 是否也必须与 $a$ 相关？

content:
设 $R$ 是集合 $A$ 上的关系。

若对任意 $a,b\in A$，只要：

$$
(a,b)\in R
$$

就有：

$$
(b,a)\in R
$$

则称 $R$ 是对称的。

逻辑形式为：

$$
\forall a\forall b(aRb\to bRa)
$$

---

# 反对称关系

tags: 9.1 Relations and Their Properties

hint:
两个不同元素能否双向相关？

content:
设 $R$ 是集合 $A$ 上的关系。

若对任意 $a,b\in A$，当：

$$
(a,b)\in R
$$

且：

$$
(b,a)\in R
$$

时，必有：

$$
a=b
$$

则称 $R$ 是反对称的。

等价地，若 $a\ne b$，则不能同时有：

$$
aRb
$$

和：

$$
bRa
$$

---

# 非对称关系

tags: 9.1 Relations and Their Properties

hint:
$aRb$ 成立时，$bRa$ 必须不成立，这是什么性质？

content:
设 $R$ 是集合 $A$ 上的关系。

若对任意 $a,b\in A$，只要：

$$
aRb
$$

就有：

$$
\neg(bRa)
$$

则称 $R$ 是非对称的。

逻辑形式为：

$$
\forall a\forall b(aRb\to \neg(bRa))
$$

非对称关系一定是非自反的，也一定是反对称的。

---

# 传递关系

tags: 9.1 Relations and Their Properties

hint:
如果 $a$ 关系到 $b$，$b$ 关系到 $c$，是否能推出 $a$ 关系到 $c$？

content:
设 $R$ 是集合 $A$ 上的关系。

若对任意 $a,b,c\in A$，只要：

$$
(a,b)\in R
$$

且：

$$
(b,c)\in R
$$

就有：

$$
(a,c)\in R
$$

则称 $R$ 是传递的。

逻辑形式为：

$$
\forall a\forall b\forall c((aRb\land bRc)\to aRc)
$$

---

# 关系性质的相互独立性

tags: 9.1 Relations and Their Properties

hint:
自反、对称、反对称、传递之间是否互相推出？

content:
关系的常见性质通常相互独立。

一个关系可以是对称的但不是反对称的，也可以是反对称的但不是对称的。

一个关系也可以既对称又反对称。此时若 $a\ne b$，则不允许出现 $aRb$ 或 $bRa$ 的双向非对角配对。

一个关系可以既不是对称，也不是反对称。

因此判断关系性质时，应分别按定义检查。

---

# 关系的交并运算

tags: 9.1 Relations and Their Properties

hint:
两个关系可以怎样用集合运算组合？

content:
若 $R$ 和 $S$ 都是从 $A$ 到 $B$ 的关系，则它们可以作为集合进行运算。

并关系：

$$
R\cup S
$$

包含属于 $R$ 或属于 $S$ 的有序对。

交关系：

$$
R\cap S
$$

包含同时属于 $R$ 和 $S$ 的有序对。

差关系：

$$
R-S
$$

包含属于 $R$ 但不属于 $S$ 的有序对。

---

# 逆关系

tags: 9.1 Relations and Their Properties

hint:
把每个有序对反过来，会得到什么关系？

content:
设 $R$ 是从 $A$ 到 $B$ 的关系。$R$ 的逆关系记为：

$$
R^{-1}
$$

定义为：

$$
R^{-1}=\{(b,a)\mid (a,b)\in R\}
$$

因此 $R^{-1}$ 是从 $B$ 到 $A$ 的关系。

若 $R$ 是 $A$ 上的关系，则 $R^{-1}$ 也是 $A$ 上的关系。

---

# 复合关系

tags: 9.1 Relations and Their Properties

hint:
$a$ 先经 $R$ 到 $b$，再经 $S$ 到 $c$，怎样定义复合关系？

content:
设 $R$ 是从 $A$ 到 $B$ 的关系，$S$ 是从 $B$ 到 $C$ 的关系。

$R$ 与 $S$ 的复合关系记为：

$$
S\circ R
$$

定义为：

$$
S\circ R=\{(a,c)\mid \exists b\in B((a,b)\in R\land (b,c)\in S)\}
$$

也就是说，$a$ 与 $c$ 在 $S\circ R$ 下相关，当且仅当存在中间元素 $b$，使 $aRb$ 且 $bSc$。

---

# 关系的幂

tags: 9.1 Relations and Their Properties

hint:
关系与自身反复复合怎样记？

content:
设 $R$ 是集合 $A$ 上的关系。关系的幂递归定义为：

$$
R^1=R
$$

并且对正整数 $n$：

$$
R^{n+1}=R^n\circ R
$$

因此：

$$
(a,b)\in R^n
$$

表示存在从 $a$ 到 $b$ 的长度为 $n$ 的关系链。

---

# 传递性与关系幂

tags: 9.1 Relations and Their Properties

hint:
传递关系如何用 $R^n$ 描述？

content:
设 $R$ 是集合 $A$ 上的关系。

若 $R$ 是传递的，则对所有正整数 $n$，都有：

$$
R^n\subseteq R
$$

反过来，如果对所有正整数 $n$ 都有：

$$
R^n\subseteq R
$$

则 $R$ 是传递的。

事实上，检查：

$$
R^2\subseteq R
$$

已经足以判断传递性。

---

# $n$ 元关系

tags: 9.2 n-ary Relations and Their Applications

hint:
二元关系如何推广到多个集合之间的关系？

content:
设 $A_1,A_2,\ldots,A_n$ 是集合。一个 $n$ 元关系是笛卡尔积：

$$
A_1\times A_2\times\cdots\times A_n
$$

的子集。

即：

$$
R\subseteq A_1\times A_2\times\cdots\times A_n
$$

关系中的元素是有序 $n$ 元组：

$$
(a_1,a_2,\ldots,a_n)
$$

其中：

$$
a_i\in A_i
$$

---

# 关系的度与域

tags: 9.2 n-ary Relations and Their Applications

hint:
$n$ 元关系的 arity 和 domains 是什么？

content:
$n$ 元关系的度是它所含元组的长度，也就是：

$$
n
$$

若：

$$
R\subseteq A_1\times A_2\times\cdots\times A_n
$$

则集合：

$$
A_1,A_2,\ldots,A_n
$$

称为该关系的域。

每个位置上的元素都来自对应的域。

---

# 关系数据库

tags: 9.2 n-ary Relations and Their Applications

hint:
数据库中的表为什么可以看成 $n$ 元关系？

content:
关系数据库用 $n$ 元关系表示信息。

数据库表中的每一行对应一个有序 $n$ 元组，也称为记录。

表中的每一列对应一个属性，也对应 $n$ 元关系中的一个分量。

因此，一个有 $n$ 个属性的表可以看成一个 $n$ 元关系。

---

# 属性与记录

tags: 9.2 n-ary Relations and Their Applications

hint:
关系数据库中，列和行分别叫什么？

content:
在关系数据库中，列称为属性。

每个属性有一个可能值的集合，称为该属性的域。

行称为记录，也可称为元组。

如果一个表有 $n$ 个属性，那么每条记录就是一个 $n$ 元组：

$$
(a_1,a_2,\ldots,a_n)
$$

---

# 主键

tags: 9.2 n-ary Relations and Their Applications

hint:
数据库中如何唯一识别一条记录？

content:
主键是一个属性或属性组合，能够唯一确定关系中的每一条记录。

若属性集合 $K$ 是主键，则任意两条不同记录在 $K$ 中的取值不能完全相同。

单个属性可以作为主键，多个属性也可以组合成复合主键。

主键用于唯一识别记录并避免歧义。

---

# 复合主键

tags: 9.2 n-ary Relations and Their Applications

hint:
单个属性不能唯一识别记录时，怎么办？

content:
当没有单个属性可以唯一确定记录时，可以使用多个属性组成复合主键。

复合主键是若干属性的组合，使得任意两条不同记录在这些属性上的组合值不同。

若属性组 $K$ 是复合主键，则记录在 $K$ 上的投影可以唯一标识整条记录。

---

# 选择运算

tags: 9.2 n-ary Relations and Their Applications

hint:
数据库中按条件筛选行，对应什么关系操作？

content:
选择运算从关系中选出满足给定条件的元组。

若 $R$ 是一个 $n$ 元关系，条件为 $P$，则选择结果为：

$$
\{(a_1,a_2,\ldots,a_n)\in R\mid P(a_1,a_2,\ldots,a_n)\}
$$

选择运算保留原来的属性，但减少记录数量。

在数据库中，选择类似 SQL 的 WHERE 条件筛选。

---

# 投影运算

tags: 9.2 n-ary Relations and Their Applications

hint:
数据库中只保留某些列，对应什么关系操作？

content:
投影运算从 $n$ 元关系中保留指定位置的分量，删除其他分量。

若 $R$ 是 $n$ 元关系，投影到位置：

$$
i_1,i_2,\ldots,i_m
$$

上，记为：

$$
P_{i_1,i_2,\ldots,i_m}(R)
$$

其结果为：

$$
\{(a_{i_1},a_{i_2},\ldots,a_{i_m})\mid (a_1,a_2,\ldots,a_n)\in R\}
$$

投影运算会减少属性数量，并自动消除重复元组。

---

# 连接运算

tags: 9.2 n-ary Relations and Their Applications

hint:
两个表按照共同字段匹配合并，对应什么关系操作？

content:
连接运算把两个关系中具有共同属性匹配值的元组合并。

若一个关系包含属性集合 $A$，另一个关系包含属性集合 $B$，它们在某些属性上有共同域，则连接结果包含所有在共同属性上取值一致的组合元组。

连接运算是关系数据库中合并多个表信息的核心操作。

---

# SQL 与关系操作

tags: 9.2 n-ary Relations and Their Applications

hint:
SQL 中 SELECT、FROM、WHERE 分别对应哪些关系操作？

content:
SQL 查询可以理解为关系操作的组合。

FROM 指定参与查询的关系或表。

WHERE 指定选择条件，对应选择运算。

SELECT 指定输出属性，对应投影运算。

若 FROM 中有多个表，并通过共同属性或条件匹配，则通常涉及连接运算。

---

# 事务数据库

tags: 9.2 n-ary Relations and Their Applications

hint:
购物篮数据如何用关系表示？

content:
事务数据库记录一系列交易，每个交易包含若干项目。

每条交易可以看成一个集合：

$$
T\subseteq I
$$

其中 $I$ 是所有可能项目的集合。

也可以把交易数据库表示为由交易编号和项目组成的关系。

事务数据库常用于数据挖掘，特别是发现频繁项集和关联规则。

---

# 项集

tags: 9.2 n-ary Relations and Their Applications

hint:
在交易数据库中，一组商品叫什么？

content:
在数据挖掘中，项集是项目集合的一个子集。

若所有项目集合为 $I$，则项集为：

$$
A\subseteq I
$$

如果一个交易 $T$ 包含项集 $A$，则：

$$
A\subseteq T
$$

项集用于描述多个项目在同一交易中共同出现的情况。

---

# 支持度

tags: 9.2 n-ary Relations and Their Applications

hint:
一个项集出现在多少比例的交易中？

content:
项集 $A$ 的支持度是包含 $A$ 的交易所占比例。

若交易总数为 $N$，包含 $A$ 的交易数为 $N_A$，则：

$$
\operatorname{support}(A)=\frac{N_A}{N}
$$

支持度用于衡量项集在数据库中出现得是否频繁。

---

# 频繁项集

tags: 9.2 n-ary Relations and Their Applications

hint:
项集何时被称为 frequent itemset？

content:
给定阈值 $t$，若项集 $A$ 的支持度满足：

$$
\operatorname{support}(A)\ge t
$$

则称 $A$ 是频繁项集。

频繁项集表示一组项目经常在同一交易中出现，是发现关联规则的基础。

---

# 关联规则

tags: 9.2 n-ary Relations and Their Applications

hint:
从购买一组商品推测另一组商品，用什么形式表示？

content:
关联规则形如：

$$
A\to B
$$

其中 $A$ 和 $B$ 是项集，并且通常要求：

$$
A\cap B=\varnothing
$$

它表示：包含 $A$ 的交易往往也包含 $B$。

关联规则常用于购物篮分析和推荐系统。

---

# 关联规则的置信度

tags: 9.2 n-ary Relations and Their Applications

hint:
包含 $A$ 的交易中，有多少也包含 $B$？

content:
关联规则：

$$
A\to B
$$

的置信度定义为：

$$
\operatorname{confidence}(A\to B)
=
\frac{\operatorname{support}(A\cup B)}
{\operatorname{support}(A)}
$$

它表示在已知交易包含 $A$ 的条件下，交易也包含 $B$ 的比例。

---

# 用矩阵表示关系

tags: 9.3 Representing Relations

hint:
有限集合之间的关系如何转成零一矩阵？

content:
设：

$$
A=\{a_1,a_2,\ldots,a_m\}
$$

$$
B=\{b_1,b_2,\ldots,b_n\}
$$

若 $R$ 是从 $A$ 到 $B$ 的关系，则 $R$ 可用 $m\times n$ 零一矩阵：

$$
M_R=[m_{ij}]
$$

表示，其中：

$$
m_{ij}=
\begin{cases}
1, & (a_i,b_j)\in R,\\
0, & (a_i,b_j)\notin R.
\end{cases}
$$

矩阵表示依赖于 $A$ 和 $B$ 中元素的排列顺序。

---

# 矩阵判断自反性

tags: 9.3 Representing Relations

hint:
关系矩阵主对角线全为 $1$ 表示什么？

content:
设 $R$ 是集合：

$$
A=\{a_1,a_2,\ldots,a_n\}
$$

上的关系，矩阵为：

$$
M_R=[m_{ij}]
$$

$R$ 自反当且仅当对所有：

$$
i=1,2,\ldots,n
$$

都有：

$$
m_{ii}=1
$$

也就是说，主对角线元素必须全为 $1$。

---

# 矩阵判断对称性

tags: 9.3 Representing Relations

hint:
关系矩阵等于自己的转置表示什么？

content:
设 $R$ 是有限集合上的关系，矩阵为 $M_R$。

$R$ 对称当且仅当：

$$
M_R=M_R^T
$$

等价地，对所有 $i,j$，都有：

$$
m_{ij}=m_{ji}
$$

这表示若 $a_iRa_j$，则 $a_jRa_i$。

---

# 矩阵判断反对称性

tags: 9.3 Representing Relations

hint:
反对称关系的矩阵中，非对角位置能否双向为 $1$？

content:
设 $R$ 是有限集合上的关系，矩阵为：

$$
M_R=[m_{ij}]
$$

$R$ 反对称当且仅当对所有：

$$
i\ne j
$$

不可能同时有：

$$
m_{ij}=1
$$

和：

$$
m_{ji}=1
$$

也就是说，非主对角线上的对称位置不能同时为 $1$。

---

# 矩阵表示下的关系并交

tags: 9.3 Representing Relations

hint:
关系的并和交在矩阵上对应什么布尔运算？

content:
若 $R$ 和 $S$ 是从 $A$ 到 $B$ 的关系，则：

$$
M_{R\cup S}=M_R\lor M_S
$$

其中 $\lor$ 是逐项布尔 OR。

并且：

$$
M_{R\cap S}=M_R\land M_S
$$

其中 $\land$ 是逐项布尔 AND。

这说明关系的集合运算可以用零一矩阵的布尔运算实现。

---

# 矩阵表示下的复合关系

tags: 9.3 Representing Relations

hint:
关系复合对应什么矩阵乘法？

content:
设 $R$ 是从 $A$ 到 $B$ 的关系，$S$ 是从 $B$ 到 $C$ 的关系。

若矩阵分别为 $M_R$ 和 $M_S$，则复合关系 $S\circ R$ 的矩阵为：

$$
M_{S\circ R}=M_R\odot M_S
$$

其中 $\odot$ 表示布尔矩阵乘法。

布尔乘法中的元素为：

$$
(M_R\odot M_S)_{ij}
=
\bigvee_k\left((M_R)_{ik}\land(M_S)_{kj}\right)
$$

---

# 有向图表示关系

tags: 9.3 Representing Relations

hint:
关系中的有序对如何画成有向边？

content:
设 $R$ 是集合 $A$ 上的关系。可以用有向图表示 $R$。

图的顶点是 $A$ 中的元素。

若：

$$
(a,b)\in R
$$

则从顶点 $a$ 画一条指向顶点 $b$ 的有向边。

若：

$$
(a,a)\in R
$$

则在顶点 $a$ 上画一个环。

---

# 有向图判断自反性

tags: 9.3 Representing Relations

hint:
关系图中每个顶点都有环，表示什么性质？

content:
用有向图表示集合 $A$ 上的关系 $R$ 时，$R$ 自反当且仅当每个顶点都有指向自身的环。

也就是说，对每个：

$$
a\in A
$$

都存在边：

$$
a\to a
$$

---

# 有向图判断对称性

tags: 9.3 Representing Relations

hint:
每条边都有反向边表示什么？

content:
用有向图表示关系 $R$ 时，$R$ 对称当且仅当每条从 $a$ 到 $b$ 的有向边都有反向边从 $b$ 到 $a$。

形式化地：

$$
a\to b
$$

出现时，也必须有：

$$
b\to a
$$

对于环，反向仍是自身，不影响对称性判断。

---

# 有向图判断反对称性

tags: 9.3 Representing Relations

hint:
反对称关系的有向图禁止什么样的双向边？

content:
用有向图表示关系 $R$ 时，$R$ 反对称当且仅当不同顶点之间不能同时存在相反方向的边。

也就是说，若 $a\ne b$，则不能同时有：

$$
a\to b
$$

和：

$$
b\to a
$$

环不违反反对称性。

---

# 有向图判断传递性

tags: 9.3 Representing Relations

hint:
若存在 $a\to b$ 和 $b\to c$，传递性要求什么？

content:
用有向图表示关系 $R$ 时，$R$ 传递当且仅当对于任意顶点 $a,b,c$，只要存在边：

$$
a\to b
$$

和：

$$
b\to c
$$

就必须存在边：

$$
a\to c
$$

在图中，这意味着每条长度为 $2$ 的有向路径都必须有对应的直接边。

---

# 闭包

tags: 9.4 Closures of Relations

hint:
包含原关系并具有某性质的最小关系是什么？

content:
设 $R$ 是集合 $A$ 上的关系，$P$ 是关系的一种性质。

若关系 $S$ 满足：

1. $R\subseteq S$；
2. $S$ 具有性质 $P$；
3. 对任意具有性质 $P$ 且包含 $R$ 的关系 $T$，都有 $S\subseteq T$；

则称 $S$ 是 $R$ 关于性质 $P$ 的闭包。

闭包是“包含原关系并具有目标性质的最小扩展”。

---

# 自反闭包

tags: 9.4 Closures of Relations

hint:
要让关系自反，最少需要添加哪些有序对？

content:
设 $R$ 是集合 $A$ 上的关系。

令对角关系为：

$$
\Delta=\{(a,a)\mid a\in A\}
$$

则 $R$ 的自反闭包为：

$$
R\cup \Delta
$$

也就是说，只需添加所有缺失的自环：

$$
(a,a)
$$

---

# 对称闭包

tags: 9.4 Closures of Relations

hint:
要让关系对称，最少需要添加哪些反向有序对？

content:
设 $R$ 是集合 $A$ 上的关系。$R$ 的逆关系为：

$$
R^{-1}=\{(b,a)\mid (a,b)\in R\}
$$

则 $R$ 的对称闭包为：

$$
R\cup R^{-1}
$$

也就是说，对每个已有有序对：

$$
(a,b)
$$

添加反向有序对：

$$
(b,a)
$$

即可得到最小对称关系。

---

# 关系中的路径

tags: 9.4 Closures of Relations

hint:
关系的幂与有向图中的路径有什么联系？

content:
设 $R$ 是集合 $A$ 上的关系，并用有向图表示。

若：

$$
(a,b)\in R^n
$$

则从 $a$ 到 $b$ 存在一条长度为 $n$ 的有向路径。

反过来，若有向图中从 $a$ 到 $b$ 存在长度为 $n$ 的路径，则：

$$
(a,b)\in R^n
$$

因此，关系幂刻画了路径长度。

---

# 连通关系

tags: 9.4 Closures of Relations

hint:
存在任意正长度路径的顶点对组成什么关系？

content:
设 $R$ 是集合 $A$ 上的关系。由 $R$ 生成的连通关系记为：

$$
R^*
$$

定义为所有存在正长度路径的有序对组成的关系：

$$
R^*=\bigcup_{n=1}^{\infty}R^n
$$

当 $A$ 有限且：

$$
|A|=m
$$

时，只需考虑：

$$
R^1\cup R^2\cup\cdots\cup R^m
$$

---

# 传递闭包

tags: 9.4 Closures of Relations

hint:
让关系传递，最少需要加入哪些可达对？

content:
关系 $R$ 的传递闭包是包含 $R$ 的最小传递关系。

它等于由 $R$ 生成的连通关系：

$$
R^*=\bigcup_{n=1}^{\infty}R^n
$$

也就是说，只要从 $a$ 到 $b$ 存在一条正长度路径，就在传递闭包中包含有序对：

$$
(a,b)
$$

---

# 用布尔矩阵幂求传递闭包

tags: 9.4 Closures of Relations

hint:
如何用关系矩阵的布尔幂求可达性？

content:
设 $R$ 是 $n$ 元集合上的关系，矩阵为 $M_R$。

传递闭包的矩阵可由布尔幂求得：

$$
M_{R^*}=M_R\lor M_R^{[2]}\lor\cdots\lor M_R^{[n]}
$$

其中：

$$
M_R^{[k]}
$$

表示 $M_R$ 的第 $k$ 个布尔幂。

该方法直接使用“长度为 $k$ 的路径”思想。

---

# Warshall 算法

tags: 9.4 Closures of Relations

hint:
Warshall 算法如何逐步允许更多中间顶点？

content:
Warshall 算法用于求有限集合上关系的传递闭包。

设关系矩阵为：

$$
W_0=M_R
$$

算法逐步构造矩阵：

$$
W_1,W_2,\ldots,W_n
$$

其中 $W_k$ 表示只允许编号不超过 $k$ 的顶点作为中间顶点时的可达性。

递推公式为：

$$
w_{ij}^{(k)}=w_{ij}^{(k-1)}\lor\left(w_{ik}^{(k-1)}\land w_{kj}^{(k-1)}\right)
$$

最终：

$$
W_n
$$

就是传递闭包矩阵。

---

# Warshall 算法伪代码

tags: 9.4 Closures of Relations

hint:
传递闭包的三重循环怎样写？

content:
Warshall 算法可写为：

```text
procedure Warshall(M)
    W := M
    for k := 1 to n
        for i := 1 to n
            for j := 1 to n
                W[i,j] := W[i,j] or (W[i,k] and W[k,j])
    return W
```

其中 $M$ 是关系的零一矩阵，返回值是传递闭包矩阵。

该算法的基本操作数量级为：

$$
O(n^3)
$$

---

# 等价闭包

tags: 9.4 Closures of Relations

hint:
包含一个关系的最小等价关系怎样得到？

content:
包含关系 $R$ 的最小等价关系称为 $R$ 的等价闭包。

它必须同时具有自反性、对称性和传递性。

一种构造思路是：

1. 先加入所有自环，得到自反闭包；
2. 再加入所有反向边，得到对称闭包；
3. 最后加入所有可达对，得到传递闭包。

等价闭包把原关系生成的连通分量合并成等价类。

---

# 等价关系

tags: 9.5 Equivalence Relations

hint:
哪三种性质合在一起定义了 equivalence relation？

content:
集合 $A$ 上的关系 $R$ 称为等价关系，当且仅当它同时满足：

1. 自反性；
2. 对称性；
3. 传递性。

即对所有 $a,b,c\in A$：

$$
aRa
$$

$$
aRb\to bRa
$$

$$
(aRb\land bRc)\to aRc
$$

等价关系抽象了“在某种意义下相同”的概念。

---

# 同余模 $m$ 是等价关系

tags: 9.5 Equivalence Relations

hint:
为什么 congruence modulo $m$ 是等价关系？

content:
设 $m$ 为正整数。在整数集合上定义关系：

$$
aRb\iff a\equiv b\pmod m
$$

该关系是等价关系。

自反性：因为：

$$
m\mid(a-a)
$$

对称性：若：

$$
m\mid(a-b)
$$

则：

$$
m\mid(b-a)
$$

传递性：若：

$$
m\mid(a-b)
$$

且：

$$
m\mid(b-c)
$$

则：

$$
m\mid(a-c)
$$

---

# 等价类

tags: 9.5 Equivalence Relations

hint:
与某个元素等价的所有元素组成什么集合？

content:
设 $R$ 是集合 $A$ 上的等价关系，$a\in A$。

$a$ 的等价类记为：

$$
[a]_R
$$

定义为：

$$
[a]_R=\{s\in A\mid aRs\}
$$

若不强调关系 $R$，可简写为：

$$
[a]
$$

等价类是所有与 $a$ 等价的元素组成的集合。

---

# 等价类相等条件

tags: 9.5 Equivalence Relations

hint:
两个等价类什么时候完全相同？

content:
设 $R$ 是集合 $A$ 上的等价关系，$a,b\in A$。

以下命题等价：

$$
aRb
$$

$$
[a]_R=[b]_R
$$

$$
[a]_R\cap[b]_R\ne\varnothing
$$

因此，两个等价类要么完全相同，要么完全不相交。

---

# 等价类构成划分

tags: 9.5 Equivalence Relations

hint:
所有等价类如何把集合分成若干块？

content:
设 $R$ 是集合 $A$ 上的等价关系。

所有等价类的并为整个集合：

$$
\bigcup_{a\in A}[a]_R=A
$$

并且任意两个不同等价类不相交：

$$
[a]_R\ne[b]_R\to [a]_R\cap[b]_R=\varnothing
$$

因此，等价类把集合 $A$ 划分成若干互不重叠的非空块。

---

# 划分

tags: 9.5 Equivalence Relations

hint:
一个集合被分成不重叠且覆盖全体的非空子集，叫什么？

content:
集合 $S$ 的一个划分是由若干非空子集组成的集合族：

$$
\{A_i\mid i\in I\}
$$

满足：

1. 每个块非空：

$$
A_i\ne\varnothing
$$

2. 不同块不相交：

$$
i\ne j\to A_i\cap A_j=\varnothing
$$

3. 所有块的并为全集：

$$
\bigcup_{i\in I}A_i=S
$$

---

# 划分生成等价关系

tags: 9.5 Equivalence Relations

hint:
给定一个划分，怎样定义对应的等价关系？

content:
设：

$$
\{A_i\mid i\in I\}
$$

是集合 $S$ 的一个划分。

定义关系 $R$：若 $x$ 和 $y$ 属于划分中的同一个块，则：

$$
xRy
$$

即：

$$
xRy\iff \exists i\in I(x\in A_i\land y\in A_i)
$$

这样得到的 $R$ 是等价关系。

其等价类正好是划分中的各个块。

---

# 等价关系与划分的对应

tags: 9.5 Equivalence Relations

hint:
等价关系和划分之间是什么关系？

content:
集合上的每个等价关系都会产生一个划分，其块为等价类。

反过来，集合上的每个划分都会产生一个等价关系：同一块中的元素互相等价。

因此，集合上的等价关系与集合的划分一一对应。

---

# 商集

tags: 9.5 Equivalence Relations

hint:
所有等价类组成的集合叫什么？

content:
设 $R$ 是集合 $A$ 上的等价关系。

所有等价类组成的集合称为 $A$ 关于 $R$ 的商集，记为：

$$
A/R
$$

定义为：

$$
A/R=\{[a]_R\mid a\in A\}
$$

商集中的元素不是原集合元素，而是等价类。

---

# 偏序关系

tags: 9.6 Partial Orderings

hint:
partial ordering 由哪三种性质定义？

content:
集合 $S$ 上的关系 $R$ 称为偏序关系，当且仅当它满足：

1. 自反性；
2. 反对称性；
3. 传递性。

通常用符号：

$$
\preceq
$$

表示偏序关系。

也就是说，对所有 $a,b,c\in S$：

$$
a\preceq a
$$

$$
(a\preceq b\land b\preceq a)\to a=b
$$

$$
(a\preceq b\land b\preceq c)\to a\preceq c
$$

---

# 偏序集

tags: 9.6 Partial Orderings

hint:
集合连同偏序关系一起叫什么？

content:
若 $\preceq$ 是集合 $S$ 上的偏序关系，则有序对：

$$
(S,\preceq)
$$

称为偏序集，英文为 partially ordered set，简称 poset。

偏序集中的某些元素可能无法比较。

这与全序集不同，全序要求任意两个元素都可比较。

---

# 可比与不可比

tags: 9.6 Partial Orderings

hint:
在偏序集中，两个元素什么时候 comparable？

content:
设 $(S,\preceq)$ 是偏序集。

若对 $a,b\in S$，有：

$$
a\preceq b
$$

或：

$$
b\preceq a
$$

则称 $a$ 与 $b$ 可比。

若两者都不成立，则称 $a$ 与 $b$ 不可比。

偏序允许不可比元素存在。

---

# 全序

tags: 9.6 Partial Orderings

hint:
若任意两个元素都可比较，偏序会变成什么？

content:
若 $(S,\preceq)$ 是偏序集，并且对任意 $a,b\in S$，都有：

$$
a\preceq b
$$

或：

$$
b\preceq a
$$

则称 $\preceq$ 是全序或线性序。

此时 $(S,\preceq)$ 称为全序集或链。

---

# 良序

tags: 9.6 Partial Orderings

hint:
每个非空子集都有最小元素的全序叫什么？

content:
全序集 $(S,\preceq)$ 称为良序集，当且仅当 $S$ 的每个非空子集都有最小元素。

也就是说，对任意非空：

$$
T\subseteq S
$$

存在：

$$
a\in T
$$

使得对所有 $t\in T$，都有：

$$
a\preceq t
$$

正整数集合在通常小于等于关系下是良序集。

---

# 字典序

tags: 9.6 Partial Orderings

hint:
字符串或元组怎样按第一个不同位置排序？

content:
字典序用于比较有序元组或字符串。

给定两个不同元组：

$$
(a_1,a_2,\ldots,a_n)
$$

和：

$$
(b_1,b_2,\ldots,b_n)
$$

找到第一个满足：

$$
a_i\ne b_i
$$

的位置 $i$。

若：

$$
a_i\prec b_i
$$

则第一个元组在字典序中小于第二个元组。

---

# 偏序的积序

tags: 9.6 Partial Orderings

hint:
两个偏序集的笛卡尔积怎样自然定义偏序？

content:
设 $(A,\preceq_A)$ 和 $(B,\preceq_B)$ 是偏序集。

在 $A\times B$ 上可定义积序：

$$
(a_1,b_1)\preceq(a_2,b_2)
$$

当且仅当：

$$
a_1\preceq_A a_2
$$

且：

$$
b_1\preceq_B b_2
$$

这种偏序要求每个坐标都不超过对应坐标。

---

# Hasse 图

tags: 9.6 Partial Orderings

hint:
如何简化有向图来表示有限偏序？

content:
有限偏序集可以用 Hasse 图表示。

构造方法：

1. 从偏序关系的有向图开始；
2. 去掉所有自环；
3. 去掉由传递性隐含的边；
4. 把较大的元素画在较高位置；
5. 省略边的箭头，默认方向向上。

Hasse 图突出偏序中的覆盖关系。

---

# 覆盖关系

tags: 9.6 Partial Orderings

hint:
在 Hasse 图中，什么时候 $b$ 覆盖 $a$？

content:
在偏序集 $(S,\preceq)$ 中，若：

$$
a\prec b
$$

且不存在元素 $c\in S$ 满足：

$$
a\prec c\prec b
$$

则称 $b$ 覆盖 $a$。

Hasse 图中通常只画覆盖关系对应的边。

---

# 极大元

tags: 9.6 Partial Orderings

hint:
一个元素上方没有更大元素时，叫什么？

content:
设 $(S,\preceq)$ 是偏序集，$a\in S$。

若不存在 $b\in S$ 使得：

$$
a\prec b
$$

则称 $a$ 是极大元。

极大元不要求大于所有元素，只要求没有元素严格大于它。

一个偏序集可以有多个极大元。

---

# 极小元

tags: 9.6 Partial Orderings

hint:
一个元素下方没有更小元素时，叫什么？

content:
设 $(S,\preceq)$ 是偏序集，$a\in S$。

若不存在 $b\in S$ 使得：

$$
b\prec a
$$

则称 $a$ 是极小元。

极小元不要求小于所有元素，只要求没有元素严格小于它。

一个偏序集可以有多个极小元。

---

# 最大元

tags: 9.6 Partial Orderings

hint:
大于等于所有元素的元素叫什么？

content:
设 $(S,\preceq)$ 是偏序集，$a\in S$。

若对所有 $b\in S$，都有：

$$
b\preceq a
$$

则称 $a$ 是最大元，也称 greatest element。

最大元若存在，则唯一。

最大元一定是极大元，但极大元不一定是最大元。

---

# 最小元

tags: 9.6 Partial Orderings

hint:
小于等于所有元素的元素叫什么？

content:
设 $(S,\preceq)$ 是偏序集，$a\in S$。

若对所有 $b\in S$，都有：

$$
a\preceq b
$$

则称 $a$ 是最小元，也称 least element。

最小元若存在，则唯一。

最小元一定是极小元，但极小元不一定是最小元。

---

# 上界与下界

tags: 9.6 Partial Orderings

hint:
一个元素若大于子集中所有元素，叫什么？

content:
设 $(S,\preceq)$ 是偏序集，$A\subseteq S$。

若 $u\in S$ 满足对所有 $a\in A$：

$$
a\preceq u
$$

则称 $u$ 是 $A$ 的上界。

若 $l\in S$ 满足对所有 $a\in A$：

$$
l\preceq a
$$

则称 $l$ 是 $A$ 的下界。

---

# 最小上界与最大下界

tags: 9.6 Partial Orderings

hint:
所有上界中最小的、所有下界中最大的分别叫什么？

content:
设 $(S,\preceq)$ 是偏序集，$A\subseteq S$。

若 $u$ 是 $A$ 的上界，并且对 $A$ 的任意上界 $v$，都有：

$$
u\preceq v
$$

则称 $u$ 是 $A$ 的最小上界，也称 least upper bound 或 join。

若 $l$ 是 $A$ 的下界，并且对 $A$ 的任意下界 $v$，都有：

$$
v\preceq l
$$

则称 $l$ 是 $A$ 的最大下界，也称 greatest lower bound 或 meet。

---

# Join 与 Meet 记号

tags: 9.6 Partial Orderings

hint:
最小上界和最大下界常用哪些符号？

content:
在偏序集中，两个元素 $a$ 和 $b$ 的最小上界常记为：

$$
a\vee b
$$

读作 join。

两个元素 $a$ 和 $b$ 的最大下界常记为：

$$
a\wedge b
$$

读作 meet。

这些记号在格中尤其常用。

---

# 格

tags: 9.6 Partial Orderings

hint:
任意两个元素都有 join 和 meet 的偏序集叫什么？

content:
偏序集 $(S,\preceq)$ 称为格，当且仅当任意两个元素：

$$
a,b\in S
$$

都有最小上界和最大下界。

即：

$$
a\vee b
$$

和：

$$
a\wedge b
$$

都存在。

格是研究偏序结构的重要对象。

---

# 有界格

tags: 9.6 Partial Orderings

hint:
格中若同时有最大元和最小元，叫什么？

content:
若格 $L$ 同时有最大元和最小元，则称 $L$ 是有界格。

最大元通常记为：

$$
1
$$

满足：

$$
x\preceq 1
$$

最小元通常记为：

$$
0
$$

满足：

$$
0\preceq x
$$

其中 $x$ 为 $L$ 中任意元素。

---

# 分配格

tags: 9.6 Partial Orderings

hint:
格中的 join 和 meet 若满足分配律，叫什么格？

content:
格 $L$ 称为分配格，当且仅当对任意 $x,y,z\in L$，都有：

$$
x\vee(y\wedge z)=(x\vee y)\wedge(x\vee z)
$$

以及：

$$
x\wedge(y\vee z)=(x\wedge y)\vee(x\wedge z)
$$

幂集格：

$$
(\mathcal{P}(S),\subseteq)
$$

在并和交下是分配格。

---

# 格中的补元

tags: 9.6 Partial Orderings

hint:
有界格中，某元素的 complement 满足什么条件？

content:
设 $L$ 是有界格，最大元为 $1$，最小元为 $0$。

元素 $a$ 的补元是元素 $b$，满足：

$$
a\vee b=1
$$

且：

$$
a\wedge b=0
$$

若有界格中每个元素都有补元，则称该格为补格。

---

# 链与反链

tags: 9.6 Partial Orderings

hint:
偏序集中，所有元素两两可比或两两不可比的子集分别叫什么？

content:
偏序集中的一个子集若任意两个元素都可比，则称为链。

若一个子集中任意两个不同元素都不可比，则称为反链。

链体现全序型结构；反链体现彼此无法比较的结构。

---

# 兼容全序

tags: 9.6 Partial Orderings

hint:
怎样把偏序扩展成一个不违反原关系的全序？

content:
设 $(S,\preceq)$ 是偏序集。若 $\le_T$ 是 $S$ 上的全序，并且每当：

$$
a\preceq b
$$

时，也有：

$$
a\le_T b
$$

则称 $\le_T$ 是与 $\preceq$ 兼容的全序。

兼容全序保留了原偏序中所有必须遵守的先后关系。

---

# 拓扑排序

tags: 9.6 Partial Orderings

hint:
如何把有先后约束的任务排成一个线性顺序？

content:
拓扑排序是构造与给定偏序兼容的全序的过程。

对于有限偏序集，拓扑排序可以反复执行：

1. 找到一个极小元；
2. 把它放入输出序列；
3. 从偏序集中删除该元素；
4. 对剩余元素重复。

得到的线性序列满足所有偏序约束。

---

# 拓扑排序的算法思想

tags: 9.6 Partial Orderings

hint:
每一步选择哪个任务最安全？

content:
拓扑排序每一步选择当前没有前置约束的元素，即极小元。

伪代码：

```text
procedure topological_sort(S, <=)
    L := empty list
    while S is nonempty
        choose a minimal element a of S
        remove a from S
        append a to L
    return L
```

若偏序表示任务依赖关系，该算法给出一个可执行任务顺序。

---

# 项目调度与偏序

tags: 9.6 Partial Orderings

hint:
任务依赖关系为什么形成偏序？

content:
若任务 $a$ 必须在任务 $b$ 之前完成，可以写为：

$$
a\preceq b
$$

只要依赖关系无循环，任务之间的先后约束形成偏序。

拓扑排序可以把偏序任务集合排成一个线性执行顺序，使所有前置任务都在后续任务之前完成。

---

# 第九章核心思想

tags: 9.6 Partial Orderings

hint:
Relations 这一章的核心结构有哪些？

content:
第九章围绕关系展开，核心内容包括：

1. 用笛卡尔积子集定义二元关系；
2. 研究关系的自反、对称、反对称、传递等性质；
3. 用 $n$ 元关系建模数据库和数据挖掘；
4. 用零一矩阵和有向图表示有限关系；
5. 构造自反闭包、对称闭包和传递闭包；
6. 用 Warshall 算法求传递闭包；
7. 用等价关系刻画“同类”并产生划分；
8. 用偏序关系刻画部分先后关系；
9. 用 Hasse 图、格和拓扑排序分析偏序结构。

---

# 图

tags: 10.1 Graphs and Graph Models

hint:
图由哪两类对象组成？

content:
图是由顶点和连接顶点的边组成的离散结构。

通常记为：

$$
G=(V,E)
$$

其中 $V$ 是非空顶点集合，$E$ 是边集合。

每条边有一个或两个相关联的顶点，这些顶点称为该边的端点。

图可以用来建模网络、关系、路径、依赖、竞争、通信和分配等问题。

---

# 有限图与无限图

tags: 10.1 Graphs and Graph Models

hint:
图的顶点集和边集是否有限，会决定什么？

content:
若图的顶点集和边集都是有限集合，则称该图为有限图。

若图的顶点集或边集是无限集合，则称该图为无限图。

在算法与实际建模中，通常研究有限图。

有限图可以用顶点表、边表、邻接矩阵、关联矩阵或邻接表等有限数据结构表示。

---

# 简单图

tags: 10.1 Graphs and Graph Models

hint:
没有环、没有重边的无向图叫什么？

content:
简单图是无向图的一种，满足：

1. 每条边连接两个不同顶点；
2. 任意两个不同顶点之间至多有一条边；
3. 不允许环；
4. 不允许多重边。

简单图适合表示“是否存在某种关系”的模型，例如朋友关系、相识关系、竞争关系等。

---

# 多重图

tags: 10.1 Graphs and Graph Models

hint:
允许两个顶点之间有多条边的图叫什么？

content:
多重图允许两个相同顶点之间存在多条边。

这些边称为多重边。

多重图不允许环时，仍然每条边连接两个不同顶点。

多重图适合表示两个对象之间可能有多种或多次连接的情形，例如两座城市之间的多条道路或两个机场之间的多班航线。

---

# 伪图

tags: 10.1 Graphs and Graph Models

hint:
允许环的无向图叫什么？

content:
伪图是允许环的无向图。

环是一条连接某个顶点到自身的边。

伪图也可以允许多重边。

若某个模型中对象可以与自身发生关系，就可能需要用环表示这种自连接。

---

# 有向图

tags: 10.1 Graphs and Graph Models

hint:
边有方向时，图怎样定义？

content:
有向图由顶点集合和有向边集合组成。

有向边是有序对：

$$
(u,v)
$$

其中 $u$ 是起点，$v$ 是终点。

有向边 $(u,v)$ 表示从 $u$ 指向 $v$ 的连接。

有向图适合表示方向性关系，例如网页链接、影响关系、先修课程依赖、航班方向和引用关系。

---

# 有向多重图

tags: 10.1 Graphs and Graph Models

hint:
允许相同起点和终点之间多条有向边的图叫什么？

content:
有向多重图允许相同有序顶点对之间存在多条有向边。

也就是说，可能存在多条边都从 $u$ 指向 $v$。

有向多重图适合表示重复发生的方向性连接，例如一天内同一航空公司从一个机场飞往另一个机场的多趟航班。

---

# 混合图

tags: 10.1 Graphs and Graph Models

hint:
同时有有向边和无向边的图叫什么？

content:
混合图同时包含有向边和无向边。

它适合建模既有双向连接又有单向连接的系统。

例如道路网络中，双向道路可用无向边表示，单行道可用有向边表示；如果两点之间可能有多条道路，还可能需要多重边。

---

# 建立图模型的三个问题

tags: 10.1 Graphs and Graph Models

hint:
选择图模型时，先问哪三个结构问题？

content:
建立图模型时，常先回答三个问题：

1. 边是无向的、有向的，还是二者都有；
2. 是否允许两个顶点之间出现多条边；
3. 是否允许环。

这三个问题决定应使用简单图、多重图、伪图、有向图、有向多重图还是混合图。

---

# 社交网络图

tags: 10.1 Graphs and Graph Models

hint:
社交网络中，顶点和边通常分别表示什么？

content:
社交网络可用图建模。

顶点表示个人、组织或账户。

边表示人与人之间的某种关系，例如相识、朋友、关注、影响、合作等。

如果关系是对称的，如互相认识，可用无向边。

如果关系有方向，如某人影响某人或某人关注某人，可用有向边。

---

# 相识图与友谊图

tags: 10.1 Graphs and Graph Models

hint:
为什么相识关系通常用简单无向图？

content:
相识图或友谊图用顶点表示人。

若两个人相识或互为朋友，则在对应顶点之间连一条无向边。

通常不需要多重边，也不需要环，因此相识图和友谊图常用简单无向图建模。

---

# 影响图

tags: 10.1 Graphs and Graph Models

hint:
“某人能影响某人”为什么适合用有向图？

content:
影响关系通常有方向性。

若人 $a$ 能影响人 $b$，则在影响图中画有向边：

$$
a\to b
$$

人 $a$ 能影响 $b$，不一定意味着 $b$ 能影响 $a$。

因此影响图通常是有向图。

---

# 合作图

tags: 10.1 Graphs and Graph Models

hint:
两人是否曾合作，适合用什么图？

content:
合作图用顶点表示人。

若两个人曾经合作完成某项工作，则在对应顶点之间连一条无向边。

合作关系通常是对称的，因此用无向边表示。

若只关心是否合作过，而不关心合作次数，则通常使用简单图。

---

# Web 图

tags: 10.1 Graphs and Graph Models

hint:
网页和超链接怎样形成有向图？

content:
Web 图用顶点表示网页。

若网页 $a$ 上有指向网页 $b$ 的链接，则画有向边：

$$
a\to b
$$

由于链接有方向，Web 图是有向图。

Web 图会随网页和链接的增删不断变化。

---

# 引用图

tags: 10.1 Graphs and Graph Models

hint:
论文或专利之间的引用关系如何建模？

content:
引用图用顶点表示文档，例如论文、专利或法律意见。

若文档 $a$ 引用文档 $b$，则画有向边：

$$
a\to b
$$

引用关系有方向，因此引用图是有向图。

通常不需要环，也不需要多重边。

---

# 模块依赖图

tags: 10.1 Graphs and Graph Models

hint:
软件模块之间的依赖关系为什么是有向的？

content:
模块依赖图用顶点表示程序模块。

若模块 $B$ 依赖模块 $A$，则画有向边：

$$
A\to B
$$

这表示 $B$ 不能独立于 $A$ 正常工作。

模块依赖图可用于软件设计、测试顺序安排和维护分析。

---

# 先行图

tags: 10.1 Graphs and Graph Models

hint:
程序语句的执行先后依赖怎样用图表示？

content:
先行图用顶点表示程序语句或任务。

若语句 $S_j$ 必须在语句 $S_k$ 之前执行，则画有向边：

$$
S_j\to S_k
$$

先行图可用于并行处理和任务调度，帮助判断哪些语句可以并发执行，哪些语句必须等待前置语句完成。

---

# 道路网络图

tags: 10.1 Graphs and Graph Models

hint:
道路中的交叉口和道路分别对应图中的什么？

content:
道路网络可用图建模。

顶点表示交叉口或地点。

边表示道路。

双向道路可用无向边表示，单向道路可用有向边表示。

若两个交叉口之间有多条道路，需要使用多重边。

若存在环形道路，可能需要使用环。

因此复杂道路网络通常需要混合图或多重图建模。

---

# 航线图

tags: 10.1 Graphs and Graph Models

hint:
航班从一个机场到另一个机场，为什么用有向边？

content:
航线图用顶点表示机场。

若存在从机场 $A$ 到机场 $B$ 的航班，则画有向边：

$$
A\to B
$$

因为航班有出发地和目的地，边具有方向。

若同一天同一方向有多班航班，则需要有向多重图表示。

---

# 生态位重叠图

tags: 10.1 Graphs and Graph Models

hint:
物种之间竞争资源如何用图表示？

content:
生态位重叠图用顶点表示物种。

若两个物种竞争相同资源，则在对应顶点之间连一条无向边。

这种关系通常是对称的，因此用无向图。

若只关心是否竞争，而不关心竞争资源数量，则通常使用简单图。

---

# 蛋白质相互作用图

tags: 10.1 Graphs and Graph Models

hint:
两个蛋白质能结合执行功能时，怎样表示？

content:
蛋白质相互作用图用顶点表示蛋白质。

若两个蛋白质发生相互作用，则在对应顶点之间连一条无向边。

这类图通常很大，可用于发现重要蛋白质、分析细胞功能模块和推断新蛋白质功能。

---

# 图的相邻与关联

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
顶点与顶点、边与顶点之间分别有什么术语？

content:
在无向图中，若顶点 $u$ 和 $v$ 是同一条边的端点，则称 $u$ 与 $v$ 相邻。

连接 $u$ 和 $v$ 的边称为与 $u$ 和 $v$ 关联。

若边 $e$ 的端点是 $u$ 和 $v$，也可说 $e$ 连接 $u$ 和 $v$。

---

# 邻居与邻域

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
一个顶点所有相邻顶点组成什么集合？

content:
图 $G=(V,E)$ 中，顶点 $v$ 的所有邻居组成 $v$ 的邻域，记为：

$$
N(v)
$$

若 $A\subseteq V$，则 $A$ 的邻域定义为：

$$
N(A)=\bigcup_{v\in A}N(v)
$$

也就是与 $A$ 中至少一个顶点相邻的所有顶点集合。

---

# 顶点的度

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
无向图中，顶点的度怎样计算？环贡献几次？

content:
无向图中，顶点 $v$ 的度是与 $v$ 关联的边数，记为：

$$
\deg(v)
$$

若存在一个环以 $v$ 为端点，则该环对 $v$ 的度贡献：

$$
2
$$

因为环在该顶点处有两个端点。

---

# 孤立顶点与悬挂顶点

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
度为 $0$ 和度为 $1$ 的顶点分别叫什么？

content:
若顶点 $v$ 的度为：

$$
\deg(v)=0
$$

则称 $v$ 为孤立顶点。

若顶点 $v$ 的度为：

$$
\deg(v)=1
$$

则称 $v$ 为悬挂顶点或叶顶点。

与悬挂顶点关联的边称为悬挂边。

---

# 握手定理

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
无向图中所有顶点度数之和等于什么？

content:
设 $G=(V,E)$ 是无向图，边数为 $|E|$。

则所有顶点度数之和满足：

$$
\sum_{v\in V}\deg(v)=2|E|
$$

这是握手定理。

原因是每条边对总度数贡献 $2$，环也贡献 $2$。

---

# 奇度顶点个数为偶数

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
为什么任何无向图中奇数度顶点个数一定是偶数？

content:
由握手定理：

$$
\sum_{v\in V}\deg(v)=2|E|
$$

总度数是偶数。

偶数度顶点的度数和为偶数，因此奇数度顶点的度数和也必须为偶数。

若有奇数个奇数度顶点，则它们度数和为奇数，矛盾。

所以无向图中奇数度顶点个数一定为偶数。

---

# 有向图的入度与出度

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
有向边进入和离开一个顶点分别怎样计数？

content:
在有向图中，顶点 $v$ 的入度是以 $v$ 为终点的有向边数量，记为：

$$
\deg^-(v)
$$

顶点 $v$ 的出度是以 $v$ 为起点的有向边数量，记为：

$$
\deg^+(v)
$$

环同时以 $v$ 为起点和终点，因此对入度和出度各贡献 $1$。

---

# 有向图入度出度定理

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
所有入度之和和所有出度之和都等于什么？

content:
设 $G=(V,E)$ 是有向图。则：

$$
\sum_{v\in V}\deg^-(v)=|E|
$$

并且：

$$
\sum_{v\in V}\deg^+(v)=|E|
$$

因此：

$$
\sum_{v\in V}\deg^-(v)
=
\sum_{v\in V}\deg^+(v)
$$

每条有向边恰好有一个起点和一个终点。

---

# 基础无向图

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
忽略有向图边的方向，会得到什么图？

content:
把有向图中每条有向边的方向忽略，得到的无向图称为基础无向图。

基础无向图和原有向图具有相同顶点。

若原有向图中从 $u$ 到 $v$ 有边，则基础无向图中 $u$ 与 $v$ 之间有无向边。

基础无向图用于研究不依赖方向的图性质，例如弱连通性。

---

# 完全图

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
每对不同顶点之间都有边的简单图叫什么？

content:
$n$ 个顶点上的完全图记为：

$$
K_n
$$

它是一个简单图，任意两个不同顶点之间都有且只有一条边。

其边数为：

$$
\binom{n}{2}=\frac{n(n-1)}{2}
$$

完全图表示所有顶点两两相邻。

---

# 圈图

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
$n$ 个顶点首尾成环的简单图叫什么？

content:
圈图记为：

$$
C_n
$$

其中：

$$
n\ge3
$$

它由顶点：

$$
v_1,v_2,\ldots,v_n
$$

和边：

$$
\{v_1,v_2\},\{v_2,v_3\},\ldots,\{v_{n-1},v_n\},\{v_n,v_1\}
$$

组成。

每个顶点的度都是：

$$
2
$$

---

# 轮图

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
给圈图加一个中心顶点并连到所有圈上顶点，得到什么图？

content:
轮图记为：

$$
W_n
$$

其中：

$$
n\ge3
$$

它由圈图：

$$
C_n
$$

加上一个新顶点构成，并把新顶点连接到 $C_n$ 的每个顶点。

轮图共有：

$$
n+1
$$

个顶点。

---

# $n$ 维超立方体

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
哪些 bit string 顶点之间相邻？

content:
$n$ 维超立方体图记为：

$$
Q_n
$$

它的顶点是所有长度为 $n$ 的 bit string。

两个顶点相邻，当且仅当它们的 bit string 恰好在一位上不同。

因此：

$$
|V(Q_n)|=2^n
$$

每个顶点的度为：

$$
n
$$

---

# 二部图

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
顶点能分成两组且边只跨组连接时，图叫什么？

content:
简单图 $G$ 称为二部图，若其顶点集可分为两个互不相交的集合：

$$
V_1,\quad V_2
$$

使得每条边都连接 $V_1$ 中一个顶点和 $V_2$ 中一个顶点。

也就是说，不存在同一部分内部两个顶点之间的边。

---

# 完全二部图

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
两部分之间所有可能边都存在的二部图叫什么？

content:
完全二部图记为：

$$
K_{m,n}
$$

它的顶点集分成两个部分，大小分别为：

$$
m,\quad n
$$

第一部分中每个顶点都与第二部分中每个顶点相邻。

同一部分内部没有边。

其边数为：

$$
mn
$$

---

# 二部图的判别

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
图可二分与奇圈之间有什么关系？

content:
一个简单图是二部图，当且仅当它不含奇长度的圈。

若图中存在奇圈，就无法把顶点分成两个部分使每条边都跨组。

若图中没有奇圈，可以按路径长度奇偶性把顶点分成两类，从而得到二部划分。

---

# 完全匹配

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
二部图中把一侧每个顶点都配到另一侧不同顶点的边集叫什么？

content:
在二部图中，匹配是没有公共端点的边集。

若二部图的一个匹配覆盖第一部分中的每个顶点，则称为从第一部分到第二部分的完全匹配。

完全匹配常用于工作分配、婚配问题、任务分配等模型。

---

# Hall 婚配定理

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
二部图中一侧存在完全匹配的充要条件是什么？

content:
设二部图的两个顶点部分为 $A$ 和 $B$。

存在覆盖 $A$ 中所有顶点的匹配，当且仅当对任意：

$$
S\subseteq A
$$

都有：

$$
|N(S)|\ge |S|
$$

其中 $N(S)$ 是 $S$ 在 $B$ 中的邻居集合。

这称为 Hall 婚配定理。

---

# 正则图

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
所有顶点度数相同的图叫什么？

content:
如果简单图中每个顶点都有相同度数 $r$，则称该图为 $r$ 正则图。

即对所有顶点 $v$，都有：

$$
\deg(v)=r
$$

例如，圈图 $C_n$ 是 $2$ 正则图。

完全图 $K_n$ 是：

$$
(n-1)
$$

正则图。

---

# 子图

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
一个图的顶点和边都取自另一个图时，叫什么？

content:
图 $H=(W,F)$ 是图 $G=(V,E)$ 的子图，若：

$$
W\subseteq V
$$

且：

$$
F\subseteq E
$$

并且 $F$ 中每条边的端点都属于 $W$。

子图表示从原图中保留部分顶点和部分边得到的图。

---

# 诱导子图

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
选定顶点后，保留它们之间所有原有边，得到什么图？

content:
设 $G=(V,E)$ 是图，$W\subseteq V$。

由 $W$ 诱导的子图包含顶点集 $W$，并包含 $G$ 中所有两个端点都在 $W$ 中的边。

诱导子图记为：

$$
G[W]
$$

它保留所选顶点之间在原图中存在的全部连接。

---

# 删除边与删除顶点

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
从图中移除边或顶点时，分别会发生什么？

content:
从图 $G$ 中删除边 $e$，记为：

$$
G-e
$$

结果保留所有顶点，只删除该边。

从图 $G$ 中删除顶点 $v$，记为：

$$
G-v
$$

结果删除顶点 $v$，并删除所有与 $v$ 关联的边。

若删除顶点集 $V'$，则记为：

$$
G-V'
$$

---

# 边收缩

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
把一条边的两个端点合并成一个顶点，叫什么操作？

content:
边收缩是把一条边的两个端点合并为一个新顶点，并相应调整与这两个端点关联的边。

若收缩边：

$$
\{u,v\}
$$

则顶点 $u$ 和 $v$ 被替换为一个新顶点。

边收缩常用于图的简化、平面图研究和图同胚相关问题。

---

# 图的并

tags: 10.2 Graph Terminology and Special Types of Graphs

hint:
两个简单图的顶点和边合并后得到什么？

content:
设两个简单图为：

$$
G_1=(V_1,E_1)
$$

和：

$$
G_2=(V_2,E_2)
$$

它们的并图为：

$$
G_1\cup G_2=(V_1\cup V_2,\ E_1\cup E_2)
$$

并图包含两个图中出现过的所有顶点和所有边。

---

# 邻接矩阵

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
简单图怎样用零一矩阵表示？

content:
设简单图 $G=(V,E)$ 的顶点为：

$$
v_1,v_2,\ldots,v_n
$$

其邻接矩阵是 $n\times n$ 零一矩阵：

$$
A=[a_{ij}]
$$

其中：

$$
a_{ij}=
\begin{cases}
1, & \{v_i,v_j\}\in E,\\
0, & \{v_i,v_j\}\notin E.
\end{cases}
$$

简单无向图的邻接矩阵是对称矩阵，且主对角线全为 $0$。

---

# 有向图的邻接矩阵

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
有向边 $v_i\to v_j$ 在邻接矩阵中怎样表示？

content:
设有向图顶点为：

$$
v_1,v_2,\ldots,v_n
$$

邻接矩阵为：

$$
A=[a_{ij}]
$$

其中：

$$
a_{ij}=
\begin{cases}
1, & (v_i,v_j)\in E,\\
0, & (v_i,v_j)\notin E.
\end{cases}
$$

第 $i$ 行记录从 $v_i$ 出发的边，第 $j$ 列记录进入 $v_j$ 的边。

---

# 多重图的邻接矩阵

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
两个顶点之间有多条边时，邻接矩阵元素怎样取值？

content:
对无向多重图，邻接矩阵中元素 $a_{ij}$ 表示顶点 $v_i$ 与 $v_j$ 之间边的条数。

因此矩阵元素可以是非负整数，而不仅仅是 $0$ 或 $1$。

对于伪图，主对角线元素可用来表示环的数量。

不同教材对环在邻接矩阵主对角线上的计数约定可能不同，使用时应明确约定。

---

# 关联矩阵

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
顶点与边的关联关系怎样用矩阵表示？

content:
设无向图 $G=(V,E)$ 有顶点：

$$
v_1,v_2,\ldots,v_n
$$

和边：

$$
e_1,e_2,\ldots,e_m
$$

关联矩阵是 $n\times m$ 矩阵：

$$
M=[m_{ij}]
$$

其中：

$$
m_{ij}=
\begin{cases}
1, & v_i\text{ is incident with }e_j,\\
0, & v_i\text{ is not incident with }e_j.
\end{cases}
$$

每一列描述一条边与哪些顶点关联。

---

# 关联矩阵表示多重边与环

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
多重边和环在关联矩阵中如何体现？

content:
在关联矩阵中，多重边会产生相同的列，因为它们关联同一对顶点。

环只关联一个顶点，因此对应列中只有一个位置为 $1$。

这种表示能区分不同边，因此适合表示多重图和伪图。

---

# 邻接表

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
稀疏图为什么常用邻接表而不是邻接矩阵？

content:
邻接表为每个顶点列出所有相邻顶点。

若图较稀疏，即边数远少于最大可能边数，邻接表通常比邻接矩阵节省空间。

简单无向图的邻接表中，每条边会在两个端点的列表中各出现一次。

有向图的邻接表通常列出每个顶点的出邻居。

---

# 邻接矩阵与邻接表的取舍

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
矩阵和表分别适合什么图？

content:
邻接矩阵适合稠密图，或需要快速判断两个顶点是否相邻的情形。

判断 $v_i$ 与 $v_j$ 是否相邻只需查看：

$$
a_{ij}
$$

邻接表适合稀疏图，或需要遍历某个顶点所有邻居的情形。

若图有 $n$ 个顶点和 $m$ 条边，邻接矩阵空间通常为：

$$
O(n^2)
$$

邻接表空间通常为：

$$
O(n+m)
$$

---

# 图同构

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
两个图结构相同但顶点名字不同，怎样形式化？

content:
两个简单图：

$$
G_1=(V_1,E_1)
$$

和：

$$
G_2=(V_2,E_2)
$$

称为同构，若存在从 $V_1$ 到 $V_2$ 的双射：

$$
f:V_1\to V_2
$$

使得对任意 $a,b\in V_1$，都有：

$$
\{a,b\}\in E_1
\leftrightarrow
\{f(a),f(b)\}\in E_2
$$

这样的函数 $f$ 称为图同构。

---

# 图同构的不变量

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
如果两个图同构，它们必须共享哪些性质？

content:
若两个图同构，则许多结构性质必须相同，包括：

1. 顶点数；
2. 边数；
3. 各顶点度数组成的多重集合；
4. 连通分量数；
5. 是否连通；
6. 是否有圈；
7. 是否二部；
8. 特定长度圈的数量。

这些性质称为图同构不变量。

若某个不变量不同，则两个图一定不同构。

---

# 度序列

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
把所有顶点度数按顺序列出，能帮助判断什么？

content:
图的度序列是把所有顶点的度数按非增或非减顺序排列得到的序列。

若两个简单图同构，则它们的度序列必相同。

但度序列相同不一定说明两个图同构。

度序列是判断不同构的常用快速工具。

---

# 图同构不是只看图形外观

tags: 10.3 Representing Graphs and Graph Isomorphism

hint:
同一个图可以画得很不一样吗？

content:
同一个图可以用不同方式绘制，边的弯曲方式、顶点位置和图形外观都可能不同。

图同构关注的是邻接结构，而不是具体画法。

两个图若存在保持邻接关系的顶点一一对应，即使画法不同，也同构。

反之，外观看起来相似但邻接结构不同，也不同构。

---

# 路径

tags: 10.4 Connectivity

hint:
从一个顶点到另一个顶点的边序列叫什么？

content:
在无向图中，从顶点 $u$ 到顶点 $v$ 的路径是一个顶点序列：

$$
v_0,v_1,\ldots,v_n
$$

其中：

$$
v_0=u,\quad v_n=v
$$

并且对每个 $i=0,1,\ldots,n-1$，都有边：

$$
\{v_i,v_{i+1}\}
$$

路径的长度是路径中边的条数：

$$
n
$$

---

# 回路与圈

tags: 10.4 Connectivity

hint:
路径起点和终点相同时，叫什么？

content:
若路径的起点和终点相同，则称为回路或 circuit。

若路径：

$$
v_0,v_1,\ldots,v_n
$$

满足：

$$
v_0=v_n
$$

且除了起点终点重合外，其他顶点不重复，则称为圈或 cycle。

圈通常要求长度至少为：

$$
3
$$

---

# 简单路径

tags: 10.4 Connectivity

hint:
路径中不重复经过顶点时，叫什么？

content:
若路径中没有重复顶点，则称为简单路径。

也就是说，路径：

$$
v_0,v_1,\ldots,v_n
$$

满足：

$$
v_i\ne v_j
$$

对所有：

$$
0\le i<j\le n
$$

成立。

若起点和终点相同，则可形成简单圈的概念。

---

# 连通图

tags: 10.4 Connectivity

hint:
任意两个顶点之间都有路径时，图叫什么？

content:
无向图 $G$ 称为连通图，当且仅当图中任意两个不同顶点之间都存在路径。

形式化地，对任意：

$$
u,v\in V
$$

都存在从 $u$ 到 $v$ 的路径。

若存在两个顶点之间没有路径，则图不连通。

---

# 连通分量

tags: 10.4 Connectivity

hint:
一个图中最大的连通部分叫什么？

content:
无向图的连通分量是极大的连通子图。

极大表示：不能再加入原图中的其他顶点和边而保持连通。

每个顶点属于且只属于一个连通分量。

图连通当且仅当它只有一个连通分量。

---

# 路径存在的等价关系

tags: 10.4 Connectivity

hint:
“两个顶点之间有路径”在无向图中是什么关系？

content:
在无向图中，定义关系：

$$
uRv
$$

当且仅当 $u$ 与 $v$ 之间存在路径。

这个关系是等价关系：

自反性：每个顶点到自身有长度 $0$ 路径；

对称性：无向路径可以反向走；

传递性：两条路径可以首尾连接。

该等价关系的等价类就是连通分量。

---

# 割点

tags: 10.4 Connectivity

hint:
删除某个顶点后，图的连通分量数增加，这个顶点叫什么？

content:
设 $G$ 是连通无向图。

若删除顶点 $v$ 以及与它关联的所有边后，所得图：

$$
G-v
$$

不连通，则称 $v$ 是割点，也称 articulation point。

割点表示网络中的关键顶点，删除它会破坏连通性。

---

# 割边

tags: 10.4 Connectivity

hint:
删除某条边后，图不再连通，这条边叫什么？

content:
设 $G$ 是连通无向图。

若删除边 $e$ 后，所得图：

$$
G-e
$$

不连通，则称 $e$ 是割边，也称桥。

割边表示网络中的关键连接，删除它会增加连通分量数。

---

# 点连通度

tags: 10.4 Connectivity

hint:
最少删除多少顶点能使图不连通？

content:
图 $G$ 的点连通度记为：

$$
\kappa(G)
$$

它是为了使图不连通或变成平凡图所需删除的最少顶点数。

若：

$$
\kappa(G)\ge k
$$

则称图是 $k$ 连通的。

点连通度用于衡量图在顶点失效下的鲁棒性。

---

# 边连通度

tags: 10.4 Connectivity

hint:
最少删除多少边能使图不连通？

content:
图 $G$ 的边连通度记为：

$$
\lambda(G)
$$

它是为了使图不连通所需删除的最少边数。

若：

$$
\lambda(G)\ge k
$$

则称图是 $k$ 边连通的。

边连通度用于衡量网络在连接失效下的可靠性。

---

# 有向路径

tags: 10.4 Connectivity

hint:
有向图中路径为什么必须尊重边的方向？

content:
有向图中的路径是顶点序列：

$$
v_0,v_1,\ldots,v_n
$$

使得对每个：

$$
i=0,1,\ldots,n-1
$$

都有有向边：

$$
(v_i,v_{i+1})
$$

路径必须沿着边的方向前进，不能反向使用有向边。

---

# 强连通

tags: 10.4 Connectivity

hint:
有向图中任意两个顶点能互相到达时，叫什么？

content:
有向图称为强连通，当且仅当对任意两个顶点 $u$ 和 $v$，都存在从 $u$ 到 $v$ 的有向路径，也存在从 $v$ 到 $u$ 的有向路径。

强连通要求双向可达，而不是只要求无向意义下连通。

---

# 弱连通

tags: 10.4 Connectivity

hint:
忽略有向边方向后连通，原有向图叫什么？

content:
有向图称为弱连通，当且仅当忽略所有边的方向后得到的基础无向图是连通的。

弱连通不要求任意两个顶点之间双向可达。

因此，强连通一定推出弱连通，但弱连通不一定推出强连通。

---

# 强连通分量

tags: 10.4 Connectivity

hint:
有向图中最大的强连通子图叫什么？

content:
有向图的强连通分量是极大的强连通子图。

在同一个强连通分量中，任意两个顶点都可以通过有向路径互相到达。

不同强连通分量之间不可能合并后仍保持强连通。

---

# 可达性

tags: 10.4 Connectivity

hint:
从顶点 $u$ 能沿有向路径到达 $v$，怎样描述？

content:
在有向图中，若存在从顶点 $u$ 到顶点 $v$ 的有向路径，则称 $v$ 从 $u$ 可达。

可达性可用关系的传递闭包或邻接矩阵的布尔幂表示。

若邻接矩阵为 $A$，则布尔矩阵：

$$
A\lor A^{[2]}\lor\cdots\lor A^{[n]}
$$

刻画正长度路径可达性。

---

# 欧拉路径

tags: 10.5 Euler and Hamilton Paths

hint:
经过每条边恰好一次的路径叫什么？

content:
图中的欧拉路径是经过图中每条边恰好一次的路径。

欧拉路径可以重复经过顶点，但不能重复经过边。

如果欧拉路径的起点和终点不同，它不是欧拉回路。

欧拉路径用于解决“一笔画”类问题和需要遍历所有边的路线问题。

---

# 欧拉回路

tags: 10.5 Euler and Hamilton Paths

hint:
经过每条边恰好一次并回到起点的回路叫什么？

content:
欧拉回路是经过图中每条边恰好一次，并且起点等于终点的回路。

欧拉回路要求遍历所有边而不重复边。

若图有欧拉回路，则一定有欧拉路径。

---

# 无向图存在欧拉回路的判别

tags: 10.5 Euler and Hamilton Paths

hint:
连通多重图什么时候有 Euler circuit？

content:
连通多重图有欧拉回路，当且仅当每个顶点的度都是偶数。

即对所有顶点 $v$，都有：

$$
\deg(v)\equiv0\pmod 2
$$

直观原因是：每次进入一个非起终点顶点，都必须从另一条边离开，因此边在该顶点成对出现。

---

# 无向图存在欧拉路径的判别

tags: 10.5 Euler and Hamilton Paths

hint:
连通多重图什么时候有 Euler path 但没有 Euler circuit？

content:
连通多重图有欧拉路径但没有欧拉回路，当且仅当它恰好有两个奇度顶点。

这两个奇度顶点必须作为欧拉路径的两个端点。

若奇度顶点数为 $0$，则存在欧拉回路。

若奇度顶点数不是 $0$ 或 $2$，则不存在欧拉路径。

---

# Konigsberg 七桥问题

tags: 10.5 Euler and Hamilton Paths

hint:
为什么七桥问题可以转化为欧拉路径问题？

content:
Konigsberg 七桥问题要求找到一条路线，使每座桥恰好经过一次。

把陆地区域表示为顶点，把桥表示为边，就得到一个多重图。

原问题等价于问该图是否存在欧拉路径。

由于对应图中有超过两个奇度顶点，因此不存在欧拉路径，也就不可能一次性恰好走过每座桥。

---

# 中国邮递员问题

tags: 10.5 Euler and Hamilton Paths

hint:
如果没有欧拉回路，怎样尽量少重复边遍历所有边？

content:
中国邮递员问题要求在图中找到一条闭合路线，使其经过每条边至少一次，并使总长度最小。

如果图有欧拉回路，则欧拉回路就是最优解。

如果图没有欧拉回路，则需要重复某些边，使所有顶点度数变为偶数，并尽量使重复边总代价最小。

---

# 有向图欧拉回路判别

tags: 10.5 Euler and Hamilton Paths

hint:
有向图中每个顶点入度和出度满足什么条件时有 Euler circuit？

content:
一个有向图存在欧拉回路的必要条件是每个顶点入度等于出度：

$$
\deg^-(v)=\deg^+(v)
$$

对所有顶点 $v$ 成立。

还需要所有非零度顶点在适当意义下处于同一个连通部分中，通常要求忽略方向后连通，并满足有向可达性条件。

直观上，每次进入一个顶点，都必须能从该顶点离开。

---

# 有向图欧拉路径判别

tags: 10.5 Euler and Hamilton Paths

hint:
有向图中起点和终点的出入度应怎样不同？

content:
有向图存在从 $s$ 到 $t$ 的欧拉路径时，通常满足：

起点 $s$ 的出度比入度大 $1$：

$$
\deg^+(s)=\deg^-(s)+1
$$

终点 $t$ 的入度比出度大 $1$：

$$
\deg^-(t)=\deg^+(t)+1
$$

其他顶点入度等于出度：

$$
\deg^+(v)=\deg^-(v)
$$

并且所有相关顶点处在同一个连通结构中。

---

# Hamilton 路径

tags: 10.5 Euler and Hamilton Paths

hint:
恰好经过每个顶点一次的路径叫什么？

content:
Hamilton 路径是经过图中每个顶点恰好一次的简单路径。

它关注顶点是否都被访问，而不是边是否都被使用。

Hamilton 路径可以不使用所有边。

---

# Hamilton 回路

tags: 10.5 Euler and Hamilton Paths

hint:
恰好经过每个顶点一次并回到起点的回路叫什么？

content:
Hamilton 回路是经过每个顶点恰好一次，并回到起点的回路。

除了起点和终点相同外，每个其他顶点恰好出现一次。

若图存在 Hamilton 回路，则必然存在 Hamilton 路径。

---

# Euler 与 Hamilton 的区别

tags: 10.5 Euler and Hamilton Paths

hint:
Euler 看边，Hamilton 看什么？

content:
欧拉路径和欧拉回路关注是否恰好遍历每条边一次。

Hamilton 路径和 Hamilton 回路关注是否恰好访问每个顶点一次。

因此：

欧拉问题是边遍历问题；

Hamilton 问题是顶点遍历问题。

一个图可能有欧拉路径但没有 Hamilton 路径，也可能有 Hamilton 路径但没有欧拉路径。

---

# Hamilton 回路的困难性

tags: 10.5 Euler and Hamilton Paths

hint:
Hamilton 回路是否有像 Euler 回路那样简单的充要条件？

content:
与欧拉回路不同，Hamilton 回路没有已知的简单充要判别条件。

判断一个图是否有 Hamilton 回路通常困难得多。

Hamilton 回路问题与旅行商问题密切相关，是组合优化和计算复杂性中的重要问题。

---

# Dirac 定理

tags: 10.5 Euler and Hamilton Paths

hint:
若每个顶点度数足够大，如何保证 Hamilton 回路存在？

content:
Dirac 定理给出 Hamilton 回路存在的充分条件。

设 $G$ 是有 $n$ 个顶点的简单图，其中：

$$
n\ge3
$$

若每个顶点的度都至少为：

$$
\frac{n}{2}
$$

则 $G$ 有 Hamilton 回路。

即：

$$
\deg(v)\ge\frac{n}{2}
$$

对所有顶点 $v$ 成立时，Hamilton 回路一定存在。

---

# Ore 定理

tags: 10.5 Euler and Hamilton Paths

hint:
非相邻顶点的度数和足够大时，能推出什么？

content:
Ore 定理给出 Hamilton 回路存在的另一个充分条件。

设 $G$ 是有 $n$ 个顶点的简单图，其中：

$$
n\ge3
$$

若对任意两个不相邻顶点 $u$ 和 $v$，都有：

$$
\deg(u)+\deg(v)\ge n
$$

则 $G$ 有 Hamilton 回路。

Ore 定理只是一条充分条件，不是必要条件。

---

# 旅行商问题

tags: 10.5 Euler and Hamilton Paths

hint:
在加权完全图中找最短 Hamilton 回路，对应什么问题？

content:
旅行商问题要求在一组城市之间找到一条最短路线，使得旅行者访问每个城市恰好一次并回到出发城市。

图论模型是加权完全图。

顶点表示城市，边权表示城市之间距离或费用。

目标是找到权重总和最小的 Hamilton 回路。

旅行商问题是著名的困难组合优化问题。

---

# 加权图

tags: 10.6 Shortest-Path Problems

hint:
边上带数值的图叫什么？

content:
加权图是在边上赋予数值权重的图。

权重可表示距离、时间、费用、容量、延迟或风险等。

若边 $e$ 的权重为 $w(e)$，则路径的长度或代价通常定义为路径上各边权重之和。

最短路径问题就是在加权图中寻找总权重最小的路径。

---

# 路径长度与路径权重

tags: 10.6 Shortest-Path Problems

hint:
在加权图中，路径长度如何计算？

content:
在加权图中，路径：

$$
v_0,v_1,\ldots,v_k
$$

的权重为路径上各边权重之和：

$$
w(v_0,v_1)+w(v_1,v_2)+\cdots+w(v_{k-1},v_k)
$$

最短路径是指在所有从起点到终点的路径中，总权重最小的路径。

---

# 最短路径问题

tags: 10.6 Shortest-Path Problems

hint:
给定起点和终点，最短路径问题要求什么？

content:
最短路径问题是在加权图中寻找从起点 $s$ 到终点 $t$ 的最小权重路径。

若需要从一个源点到所有其他顶点的最短路径，则称为单源最短路径问题。

若需要所有顶点对之间的最短路径，则称为全源最短路径问题。

---

# Dijkstra 算法适用条件

tags: 10.6 Shortest-Path Problems

hint:
Dijkstra 算法要求边权满足什么条件？

content:
Dijkstra 算法用于求解加权图中的单源最短路径。

它要求所有边权非负。

若存在负权边，Dijkstra 算法的贪心选择不一定正确。

在非负权图中，Dijkstra 算法每一步确定一个当前距离最小的未确定顶点，并保证该距离已经是最终最短距离。

---

# Dijkstra 算法思想

tags: 10.6 Shortest-Path Problems

hint:
Dijkstra 每一步选择哪个顶点？

content:
Dijkstra 算法维护一个已确定最短距离的顶点集合 $S$。

初始时：

$$
S=\varnothing
$$

起点 $s$ 的距离设为：

$$
0
$$

其他顶点距离设为无穷大。

每一步选择不在 $S$ 中、当前距离最小的顶点 $u$，把 $u$ 加入 $S$，然后用 $u$ 的出边松弛邻居距离。

---

# 松弛操作

tags: 10.6 Shortest-Path Problems

hint:
什么时候通过 $u$ 到 $v$ 的路径能改进当前最短距离估计？

content:
设当前从源点 $s$ 到顶点 $u$ 的距离估计为 $d(u)$，边 $(u,v)$ 的权重为 $w(u,v)$。

若：

$$
d(u)+w(u,v)<d(v)
$$

则可以更新：

$$
d(v):=d(u)+w(u,v)
$$

这称为松弛操作。

松弛表示：通过 $u$ 到达 $v$ 比当前已知路径更短。

---

# Dijkstra 算法伪代码

tags: 10.6 Shortest-Path Problems

hint:
单源非负权最短路径算法怎样写？

content:
Dijkstra 算法伪代码：

```text
procedure Dijkstra(G, w, s)
    for each vertex v
        d[v] := infinity
        previous[v] := undefined
    d[s] := 0
    S := empty set
    while S does not contain all vertices
        choose u not in S with minimum d[u]
        add u to S
        for each neighbor v of u not in S
            if d[u] + w(u,v) < d[v]
                d[v] := d[u] + w(u,v)
                previous[v] := u
    return d, previous
```

其中 $d[v]$ 是从源点 $s$ 到 $v$ 的最短距离估计。

---

# Dijkstra 算法复杂度

tags: 10.6 Shortest-Path Problems

hint:
简单实现的 Dijkstra 算法复杂度是多少？

content:
若图有 $n$ 个顶点，Dijkstra 算法的简单实现每次从未确定顶点中线性寻找距离最小者。

这种实现需要大约：

$$
O(n^2)
$$

时间。

若使用优先队列和适当数据结构，可在稀疏图上获得更好的实际性能。

---

# Floyd-Warshall 算法思想

tags: 10.6 Shortest-Path Problems

hint:
所有点对最短路径如何逐步允许更多中间点？

content:
Floyd-Warshall 算法用于求所有顶点对之间的最短路径。

设：

$$
d_{ij}^{(k)}
$$

表示从 $i$ 到 $j$ 的路径中，只允许编号不超过 $k$ 的顶点作为中间点时的最短距离。

递推为：

$$
d_{ij}^{(k)}
=
\min\left(d_{ij}^{(k-1)},\ d_{ik}^{(k-1)}+d_{kj}^{(k-1)}\right)
$$

最终：

$$
d_{ij}^{(n)}
$$

就是从 $i$ 到 $j$ 的最短路径长度。

---

# Floyd-Warshall 算法伪代码

tags: 10.6 Shortest-Path Problems

hint:
所有点对最短路径的三重循环怎样写？

content:
Floyd-Warshall 算法伪代码：

```text
procedure Floyd_Warshall(W)
    D := W
    for k := 1 to n
        for i := 1 to n
            for j := 1 to n
                D[i,j] := min(D[i,j], D[i,k] + D[k,j])
    return D
```

其中 $W$ 是初始权重矩阵。

若 $i$ 与 $j$ 之间无边，可把对应权重设为无穷大。

该算法时间复杂度为：

$$
O(n^3)
$$

---

# 平面图

tags: 10.7 Planar Graphs

hint:
能否把图画在平面上且边不交叉？

content:
如果一个图可以画在平面上，使得任意两条边只可能在公共端点相交，而不会在其他地方交叉，则称该图为平面图。

这种没有边交叉的画法称为平面嵌入或平面表示。

平面性关注是否存在某种无交叉画法，而不是某一次画法是否交叉。

---

# 平面表示与区域

tags: 10.7 Planar Graphs

hint:
平面图画出来后，平面被分成哪些部分？

content:
一个平面图的无交叉画法把平面分成若干个连通区域，称为面或区域。

其中有一个无界区域，称为外部区域。

区域的边界由图中的边组成。

平面图的区域数通常记为：

$$
r
$$

---

# Euler 公式

tags: 10.7 Planar Graphs

hint:
连通平面简单图的顶点、边、区域数满足什么公式？

content:
若连通平面图有：

$$
v
$$

个顶点、

$$
e
$$

条边、

$$
r
$$

个区域，则满足 Euler 公式：

$$
v-e+r=2
$$

这是平面图理论的基本公式。

---

# 平面图边数上界

tags: 10.7 Planar Graphs

hint:
连通简单平面图最多有多少条边？

content:
若 $G$ 是连通简单平面图，且顶点数：

$$
v\ge3
$$

边数为 $e$，则：

$$
e\le3v-6
$$

原因是每个区域边界长度至少为 $3$，而每条边在区域边界计数中贡献两次。

---

# 无三角平面图边数上界

tags: 10.7 Planar Graphs

hint:
若平面图没有长度为 $3$ 的圈，边数上界怎样加强？

content:
若连通简单平面图没有长度为 $3$ 的圈，且：

$$
v\ge3
$$

则每个区域边界长度至少为 $4$。

此时边数满足：

$$
e\le2v-4
$$

这个上界常用于证明某些二部图非平面。

---

# $K_5$ 非平面

tags: 10.7 Planar Graphs

hint:
为什么完全图 $K_5$ 不可能是平面图？

content:
完全图 $K_5$ 有：

$$
v=5
$$

个顶点和：

$$
e=10
$$

条边。

若它是简单平面图，则应满足：

$$
e\le3v-6
$$

即：

$$
10\le3\cdot5-6=9
$$

矛盾。

因此：

$$
K_5
$$

不是平面图。

---

# $K_{3,3}$ 非平面

tags: 10.7 Planar Graphs

hint:
为什么完全二部图 $K_{3,3}$ 不可能平面？

content:
完全二部图 $K_{3,3}$ 有：

$$
v=6
$$

个顶点和：

$$
e=9
$$

条边。

它没有三角形。

若它是平面图，则应满足无三角平面图边数上界：

$$
e\le2v-4
$$

即：

$$
9\le2\cdot6-4=8
$$

矛盾。

因此：

$$
K_{3,3}
$$

不是平面图。

---

# 细分

tags: 10.7 Planar Graphs

hint:
把一条边替换成经过新顶点的路径，叫什么操作？

content:
细分是把图中的一条边：

$$
\{u,v\}
$$

替换为两条边：

$$
\{u,w\},\quad \{w,v\}
$$

其中 $w$ 是新顶点。

反复细分可以把一条边替换为一条路径。

细分不会改变图的本质连接结构，但会增加度为 $2$ 的顶点。

---

# 同胚图

tags: 10.7 Planar Graphs

hint:
两个图若能通过细分得到同一个图，它们有什么关系？

content:
两个图称为同胚，若它们可以通过对边进行若干次细分而得到同构图。

同胚关系用于刻画图中是否包含 $K_5$ 或 $K_{3,3}$ 的细分结构。

---

# Kuratowski 定理

tags: 10.7 Planar Graphs

hint:
平面图的禁忌结构是什么？

content:
Kuratowski 定理说明：一个图是非平面的，当且仅当它包含一个同胚于：

$$
K_5
$$

或：

$$
K_{3,3}
$$

的子图。

等价地，一个图是平面图，当且仅当它不含 $K_5$ 或 $K_{3,3}$ 的细分作为子图。

---

# 平面图的对偶图

tags: 10.7 Planar Graphs

hint:
怎样用区域作为顶点构造对偶图？

content:
给定一个平面图的平面表示，可以构造其对偶图。

对偶图中，每个区域对应一个顶点。

若原图中一条边分隔两个区域，则在对偶图中连接对应的两个区域顶点。

若某条边两侧是同一区域，对偶图中可能出现环。

对偶图依赖于原图的具体平面表示。

---

# 图着色

tags: 10.8 Graph Coloring

hint:
给顶点染色时，相邻顶点有什么限制？

content:
图的顶点着色是给图中每个顶点分配一种颜色，使得相邻顶点颜色不同。

若使用 $k$ 种颜色完成顶点着色，则称该图是 $k$ 可着色的。

着色问题常用于地图着色、任务冲突、考试安排、寄存器分配和频率分配等问题。

---

# 色数

tags: 10.8 Graph Coloring

hint:
使图正确着色所需的最少颜色数叫什么？

content:
图 $G$ 的色数是对其顶点正确着色所需的最少颜色数，记为：

$$
\chi(G)
$$

若：

$$
\chi(G)=k
$$

表示 $G$ 可以用 $k$ 种颜色着色，但不能用更少颜色正确着色。

---

# 完全图的色数

tags: 10.8 Graph Coloring

hint:
$K_n$ 为什么需要 $n$ 种颜色？

content:
完全图：

$$
K_n
$$

中任意两个不同顶点都相邻。

因此每个顶点都必须使用不同颜色。

所以：

$$
\chi(K_n)=n
$$

---

# 二部图的色数

tags: 10.8 Graph Coloring

hint:
非空二部图需要多少种颜色？

content:
若图 $G$ 是二部图，且至少有一条边，则：

$$
\chi(G)=2
$$

因为可以把两个部分分别染成两种颜色。

若图没有边，则所有顶点可使用同一种颜色，因此：

$$
\chi(G)=1
$$

---

# 圈图的色数

tags: 10.8 Graph Coloring

hint:
偶圈和奇圈的色数有什么区别？

content:
圈图 $C_n$ 的色数取决于 $n$ 的奇偶性。

若 $n$ 为偶数，则：

$$
\chi(C_n)=2
$$

因为偶圈是二部图。

若 $n$ 为奇数，则：

$$
\chi(C_n)=3
$$

因为奇圈不能用两种颜色正确着色，但三种颜色足够。

---

# 平面图四色定理

tags: 10.8 Graph Coloring

hint:
任意平面图最多需要多少种颜色？

content:
四色定理说明：任意平面图的顶点都可以用不超过四种颜色正确着色。

形式化地，若 $G$ 是平面图，则：

$$
\chi(G)\le4
$$

四色定理是图论中的著名定理，其证明依赖大量计算机辅助检查。

---

# 五色定理

tags: 10.8 Graph Coloring

hint:
比四色定理更容易证明的平面图着色上界是什么？

content:
五色定理说明：任意平面图都可以用不超过五种颜色正确着色。

即若 $G$ 是平面图，则：

$$
\chi(G)\le5
$$

五色定理比四色定理弱，但证明更简单，通常可由 Euler 公式和归纳法证明。

---

# 地图着色与对偶图

tags: 10.8 Graph Coloring

hint:
地图区域着色如何转化为图着色？

content:
地图着色问题可转化为图着色问题。

把地图的每个区域看成图的顶点。

若两个区域有共同边界，则在对应顶点之间连边。

对地图区域进行着色，使相邻区域颜色不同，等价于对该图进行顶点着色。

---

# 冲突图

tags: 10.8 Graph Coloring

hint:
哪些对象不能同时使用同一资源时，如何建图？

content:
冲突图用顶点表示对象，用边表示两个对象不能共享同一资源。

图着色中的颜色表示可分配的资源类型、时间段、频率、寄存器或教室等。

若两个顶点相邻，则它们必须使用不同颜色。

最少颜色数表示完成分配所需的最少资源种类。

---

# 考试排程图模型

tags: 10.8 Graph Coloring

hint:
有学生同时选两门课时，为什么这两门课不能同一时间考试？

content:
考试排程可用图着色建模。

顶点表示课程。

若存在学生同时选了两门课程，则在这两门课程对应顶点之间连边。

颜色表示考试时间段。

正确着色保证有冲突的课程不会安排在同一时间段。

所需最少时间段数就是该图的色数。

---

# 贪心着色算法

tags: 10.8 Graph Coloring

hint:
按某个顶点顺序依次染色时，每一步选什么颜色？

content:
贪心着色算法按给定顺序处理顶点。

对当前顶点，选择不会与已染色邻居冲突的最小编号颜色。

伪代码：

```text
procedure greedy_coloring(G, ordering)
    for each vertex v in ordering
        assign to v the smallest color not used by colored neighbors of v
    return coloring
```

贪心着色快速简单，但使用的颜色数依赖顶点顺序，不一定达到色数。

---

# 边着色

tags: 10.8 Graph Coloring

hint:
给边染色时，相邻边有什么限制？

content:
边着色是给图的每条边分配颜色，使得共享端点的两条边颜色不同。

边色数是正确边着色所需的最少颜色数。

边着色可用于调度问题，例如把共享资源的任务安排到不同时间段。

---

# 第十章核心思想

tags: 10.8 Graph Coloring

hint:
Graphs 这一章的主线是什么？

content:
第十章的核心是用图建模对象之间的连接关系，并研究图的结构和算法。

主要内容包括：

1. 图的基本类型：简单图、多重图、伪图、有向图、混合图；
2. 图模型：社交网络、Web、引用、道路、航线、依赖和生物网络；
3. 基本术语：相邻、度、入度、出度、子图、特殊图；
4. 图的表示：邻接矩阵、关联矩阵、邻接表；
5. 图同构与结构不变量；
6. 连通性、路径、连通分量和强连通；
7. 欧拉路径、Hamilton 路径及其应用；
8. 加权图和最短路径算法；
9. 平面图、Euler 公式和非平面判别；
10. 图着色、色数和调度应用。

---

# 树

tags: 11.1 Introduction to Trees

hint:
什么样的无向图叫做 tree？

content:
树是连通且不含简单回路的无向图。

若图 $T=(V,E)$ 是树，则它满足：

1. 任意两个顶点之间存在路径；
2. 图中不存在简单回路；
3. 顶点之间的连接结构没有冗余。

树常用于表示层级结构、决策过程、搜索过程、编码结构和网络连接骨架。

---

# 森林

tags: 11.1 Introduction to Trees

hint:
没有简单回路但不一定连通的图叫什么？

content:
森林是不含简单回路的无向图。

森林可以不连通。

森林的每个连通分量都是一棵树。

因此，森林可以看成若干棵互不相交的树的并。

---

# 树中简单路径唯一性

tags: 11.1 Introduction to Trees

hint:
树中任意两个顶点之间有几条简单路径？

content:
若 $T$ 是树，则 $T$ 中任意两个不同顶点之间存在唯一的简单路径。

反过来，若一个无向图中任意两个不同顶点之间都存在唯一简单路径，则该图是树。

因此，树也可等价地定义为：任意两顶点之间有唯一简单路径的无向图。

---

# 树的边数

tags: 11.1 Introduction to Trees

hint:
$n$ 个顶点的树有多少条边？

content:
若树 $T$ 有 $n$ 个顶点，则它有：

$$
n-1
$$

条边。

也就是说：

$$
|E(T)|=|V(T)|-1
$$

这条公式是树最重要的计数性质之一。

---

# 森林的边数

tags: 11.1 Introduction to Trees

hint:
有 $n$ 个顶点、$c$ 个连通分量的森林有多少条边？

content:
若森林 $F$ 有：

$$
n
$$

个顶点，并且有：

$$
c
$$

个连通分量，则它有：

$$
n-c
$$

条边。

原因是每个连通分量都是树。若第 $i$ 个分量有 $n_i$ 个顶点，则它有 $n_i-1$ 条边。

把所有分量相加：

$$
\sum_{i=1}^{c}(n_i-1)=n-c
$$

---

# 树中删边与加边

tags: 11.1 Introduction to Trees

hint:
树中删一条边或加一条边，会发生什么？

content:
树中任意删除一条边，图都会变得不连通。

树中任意添加一条连接两个已有顶点的新边，都会产生唯一一个简单回路。

这是因为树中任意两个顶点之间原本已有唯一简单路径。新边加上这条路径，就形成一个简单回路。

---

# 树的极小连通性

tags: 11.1 Introduction to Trees

hint:
为什么树是“刚好连通”的图？

content:
树是极小连通图。

这表示树本身连通，但删除任意一条边后都会不连通。

等价地，树中的每条边都是桥。

因此，树没有冗余连接。

---

# 树的极大无回路性

tags: 11.1 Introduction to Trees

hint:
为什么树是“边最多但无回路”的图？

content:
树是极大无简单回路图。

这表示树本身没有简单回路，但在任意两个不相邻顶点之间添加一条边，都会产生简单回路。

因此，树在保持无回路的条件下已经不能再添加边。

---

# 根树

tags: 11.1 Introduction to Trees

hint:
在树中指定一个特殊顶点后，会得到什么结构？

content:
根树是在树中指定一个顶点作为根后得到的树。

指定根后，每条边可按远离根的方向理解。

根树通常画成根在最上方，边向下延伸。

根树适合表示层级关系，例如组织结构、文件系统、表达式结构和搜索树。

---

# 根

tags: 11.1 Introduction to Trees

hint:
根树中最上层的特殊顶点叫什么？

content:
根树中被指定的特殊顶点称为根。

通常记为：

$$
r
$$

根没有父节点。

从根到树中任意顶点都存在唯一简单路径。

这条路径决定了根树中的祖先、后代、父节点和子节点关系。

---

# 父节点与子节点

tags: 11.1 Introduction to Trees

hint:
根树中，一个顶点的直接上一级和直接下一级分别叫什么？

content:
设 $T$ 是根树，$v$ 是非根顶点。

$v$ 的父节点是从根到 $v$ 的唯一路径上紧挨着 $v$ 的前一个顶点。

若 $u$ 是 $v$ 的父节点，则 $v$ 是 $u$ 的子节点。

每个非根顶点恰好有一个父节点。

一个顶点可以有零个、一个或多个子节点。

---

# 兄弟节点

tags: 11.1 Introduction to Trees

hint:
有同一个父节点的顶点叫什么？

content:
在根树中，具有相同父节点的顶点称为兄弟节点。

若顶点 $u$ 和 $v$ 的父节点相同，且：

$$
u\ne v
$$

则 $u$ 和 $v$ 是兄弟节点。

兄弟节点处在根树的同一层，并共享同一个直接祖先。

---

# 祖先与后代

tags: 11.1 Introduction to Trees

hint:
从根到某顶点的路径上，前面的顶点和下面的顶点分别叫什么？

content:
在根树中，若顶点 $u$ 位于从根到顶点 $v$ 的路径上，并且：

$$
u\ne v
$$

则称 $u$ 是 $v$ 的祖先。

相反，若 $u$ 是 $v$ 的祖先，则 $v$ 是 $u$ 的后代。

根是所有非根顶点的祖先。

叶节点没有后代。

---

# 叶节点与内部节点

tags: 11.1 Introduction to Trees

hint:
没有子节点的顶点和有子节点的顶点分别叫什么？

content:
在根树中，没有子节点的顶点称为叶节点。

有一个或多个子节点的顶点称为内部节点。

若整棵树只有一个顶点，则这个根同时也是叶节点。

若根树不止一个顶点，则根是内部节点。

---

# 子树

tags: 11.1 Introduction to Trees

hint:
以某个顶点为根，包含其所有后代的部分叫什么？

content:
设 $T$ 是根树，$v$ 是其中一个顶点。

以 $v$ 为根的子树由 $v$、$v$ 的所有后代，以及这些顶点之间的所有边组成。

子树保留原根树中的父子关系，但把 $v$ 看作该子树的根。

---

# $m$ 叉树

tags: 11.1 Introduction to Trees

hint:
每个内部节点最多有 $m$ 个子节点的根树叫什么？

content:
若一棵根树中每个内部节点最多有 $m$ 个子节点，则称它为 $m$ 叉树。

若：

$$
m=2
$$

则称为二叉树。

$m$ 叉树用于表示每个决策点最多有 $m$ 种选择的层级结构。

---

# 满 $m$ 叉树

tags: 11.1 Introduction to Trees

hint:
每个内部节点恰好有 $m$ 个子节点的树叫什么？

content:
若一棵 $m$ 叉树中每个内部节点恰好有 $m$ 个子节点，则称它为满 $m$ 叉树。

满二叉树是每个内部节点恰好有两个子节点的根树。

满 $m$ 叉树的结构便于建立顶点数、叶节点数和内部节点数之间的公式。

---

# 满 $m$ 叉树的顶点数

tags: 11.1 Introduction to Trees

hint:
满 $m$ 叉树中，内部节点数怎样决定总顶点数？

content:
设 $T$ 是满 $m$ 叉树，有 $i$ 个内部节点。

因为每个内部节点恰好有 $m$ 个子节点，所以边数为：

$$
mi
$$

树中边数等于顶点数减 $1$，因此总顶点数为：

$$
n=mi+1
$$

---

# 满 $m$ 叉树的叶节点数

tags: 11.1 Introduction to Trees

hint:
满 $m$ 叉树中，叶节点数和内部节点数有什么关系？

content:
设满 $m$ 叉树有 $i$ 个内部节点和 $l$ 个叶节点。

总顶点数满足：

$$
n=i+l
$$

又因为满 $m$ 叉树有：

$$
n=mi+1
$$

所以：

$$
l=(m-1)i+1
$$

这给出了叶节点数与内部节点数的关系。

---

# 满 $m$ 叉树的等价公式

tags: 11.1 Introduction to Trees

hint:
若知道总顶点数或叶节点数，如何反求内部节点数？

content:
设满 $m$ 叉树有 $n$ 个顶点、$i$ 个内部节点和 $l$ 个叶节点。

有：

$$
n=mi+1
$$

因此：

$$
i=\frac{n-1}{m}
$$

又有：

$$
l=(m-1)i+1
$$

可推出：

$$
l=\frac{(m-1)n+1}{m}
$$

以及：

$$
n=\frac{ml-1}{m-1}
$$

其中：

$$
m>1
$$

---

# 有序根树

tags: 11.1 Introduction to Trees

hint:
若每个节点的子节点有从左到右的顺序，叫什么树？

content:
有序根树是子节点有指定顺序的根树。

在有序根树中，每个内部节点的子节点按从左到右排列。

有序根树中，子节点的相对次序属于树结构的一部分。

因此，即使两棵根树的父子关系相同，只要子节点顺序不同，它们也可能是不同的有序根树。

---

# 有序二叉树

tags: 11.1 Introduction to Trees

hint:
二叉树中，左孩子和右孩子为什么不同？

content:
有序二叉树是每个内部节点最多有两个有序子节点的根树。

若一个节点有两个子节点，第一个称为左孩子，第二个称为右孩子。

若一个节点只有一个子节点，也需要区分它是左孩子还是右孩子。

因此，有序二叉树中的左右位置是结构的一部分。

---

# 层

tags: 11.1 Introduction to Trees

hint:
根到顶点的路径长度决定了什么？

content:
在根树中，顶点 $v$ 的层数是从根到 $v$ 的唯一路径长度。

根的层数为：

$$
0
$$

若一个顶点在第 $k$ 层，则它的子节点在第：

$$
k+1
$$

层。

层数刻画顶点在树中的深度。

---

# 高度

tags: 11.1 Introduction to Trees

hint:
根树的高度等于哪类顶点的最大层数？

content:
根树的高度是所有顶点层数的最大值。

等价地，它是从根到某个叶节点的最长路径长度。

若树只有一个根节点，则高度为：

$$
0
$$

高度常用于分析搜索树和递归算法的最坏情况复杂度。

---

# 平衡 $m$ 叉树

tags: 11.1 Introduction to Trees

hint:
叶节点都在最底两层时，树叫什么？

content:
一棵高度为 $h$ 的 $m$ 叉树称为平衡的，若所有叶节点都在第 $h$ 层或第 $h-1$ 层。

平衡树避免部分分支过深。

在搜索树中，平衡性通常能保证查找、插入和删除的效率接近对数级。

---

# $m$ 叉树的叶节点上界

tags: 11.1 Introduction to Trees

hint:
高度为 $h$ 的 $m$ 叉树最多有多少个叶节点？

content:
若一棵 $m$ 叉树的高度为 $h$，则它最多有：

$$
m^h
$$

个叶节点。

这是因为第 $0$ 层最多有 $1$ 个顶点，第 $1$ 层最多有 $m$ 个顶点，第 $h$ 层最多有：

$$
m^h
$$

个顶点。

叶节点数不能超过最底层最大顶点数。

---

# 叶节点数与高度下界

tags: 11.1 Introduction to Trees

hint:
有 $l$ 个叶节点的 $m$ 叉树，高度至少是多少？

content:
若 $m$ 叉树有 $l$ 个叶节点，高度为 $h$，则：

$$
l\le m^h
$$

因此：

$$
h\ge \lceil \log_m l\rceil
$$

这给出了由叶节点数推出的高度下界。

---

# 树的应用概览

tags: 11.2 Applications of Trees

hint:
树在计算机科学中常用来建模哪些结构？

content:
树可用于多种计算和建模任务，包括：

1. 二叉搜索树；
2. 决策树；
3. 前缀码；
4. Huffman 编码；
5. 博弈树；
6. 文件系统目录；
7. 解析树和表达式树；
8. 搜索空间与回溯过程。

这些应用利用了树的层级结构和无回路结构。

---

# 二叉搜索树

tags: 11.2 Applications of Trees

hint:
二叉搜索树中，左子树和右子树的键分别满足什么关系？

content:
二叉搜索树是一棵二叉树，每个顶点带有一个键。

对任意顶点 $v$：

1. $v$ 左子树中所有键都小于 $v$ 的键；
2. $v$ 右子树中所有键都大于 $v$ 的键。

因此，查找某个键时，可以从根开始比较，若目标更小则转向左子树，若目标更大则转向右子树。

---

# 二叉搜索树插入

tags: 11.2 Applications of Trees

hint:
向二叉搜索树插入新键时，怎样决定向左还是向右？

content:
向二叉搜索树插入新键 $x$ 时，从根开始比较：

若 $x$ 小于当前节点键，则进入左子树；

若 $x$ 大于当前节点键，则进入右子树；

若需要进入的子树为空，则把 $x$ 插入为新的左孩子或右孩子。

伪代码思想：

```text
current := root
while current is not empty
    if x < key(current)
        move to left child
    else
        move to right child
insert x at the first empty position reached
```

---

# 二叉搜索树查找复杂度

tags: 11.2 Applications of Trees

hint:
查找最多比较多少次取决于树的什么？

content:
在二叉搜索树中查找一个键时，每次比较后沿一条边向下移动。

因此查找所需比较次数至多与树的高度成正比。

若树高度为 $h$，则最坏情况下查找复杂度为：

$$
O(h)
$$

若树平衡且有 $n$ 个顶点，则：

$$
h=O(\log n)
$$

查找效率为对数级。

若树严重不平衡，高度可能达到：

$$
n-1
$$

查找会退化为线性级。

---

# 决策树

tags: 11.2 Applications of Trees

hint:
用树表示一系列比较或决策时，顶点和叶子各代表什么？

content:
决策树是一种根树，用于表示一系列决策过程。

内部节点表示一次测试、比较或决策。

边表示该测试的可能结果。

叶节点表示最终输出、分类结果或决策结论。

从根到叶的一条路径对应一次完整决策过程。

---

# 二叉决策树

tags: 11.2 Applications of Trees

hint:
每次测试只有两个可能结果时，决策树是什么树？

content:
若每次决策只有两个可能结果，则对应的决策树是二叉树。

例如在比较排序中，每次比较两个元素，结果只有：

$$
<
$$

或：

$$
>
$$

两种基本可能，因此比较排序可用二叉决策树建模。

---

# 比较排序的决策树

tags: 11.2 Applications of Trees

hint:
为什么排序算法可以用决策树表示？

content:
基于比较的排序算法可以用决策树表示。

每个内部节点表示一次元素比较。

每条边表示比较结果。

每个叶节点表示一种可能的最终排列顺序。

若要正确排序 $n$ 个不同元素，决策树至少需要有：

$$
n!
$$

个叶节点，因为可能输入排列共有 $n!$ 种。

---

# 比较排序下界

tags: 11.2 Applications of Trees

hint:
为什么任何比较排序至少需要 $\lceil \log_2 n! \rceil$ 次比较？

content:
基于比较的排序算法对应一棵二叉决策树。

若最坏情况下最多比较 $h$ 次，则决策树高度至多为 $h$，叶节点数最多为：

$$
2^h
$$

正确排序 $n$ 个不同元素需要至少：

$$
n!
$$

个叶节点。

因此：

$$
2^h\ge n!
$$

从而：

$$
h\ge \lceil \log_2 n!\rceil
$$

所以任何比较排序最坏情况下至少需要：

$$
\lceil \log_2 n!\rceil
$$

次比较。

---

# 前缀码

tags: 11.2 Applications of Trees

hint:
没有任何码字是另一个码字的前缀，叫什么编码？

content:
前缀码是一组码字，满足没有任何码字是另一个码字的前缀。

形式化地，若 $c_i$ 和 $c_j$ 是两个不同码字，则 $c_i$ 不能是 $c_j$ 的前缀。

前缀码可以被即时解码，因为读到一个完整码字时，不必等待后续符号来判断它是否结束。

---

# 前缀码的二叉树表示

tags: 11.2 Applications of Trees

hint:
如何用二叉树表示由 0 和 1 组成的前缀码？

content:
二进制前缀码可以用二叉树表示。

从根开始，左边可标记为：

$$
0
$$

右边可标记为：

$$
1
$$

每个符号对应一个叶节点。

从根到叶节点路径上的边标签串就是该符号的码字。

前缀码条件等价于：所有码字都位于叶节点，不允许某个码字对应内部节点。

---

# 前缀码解码

tags: 11.2 Applications of Trees

hint:
给定前缀码树，如何从比特串恢复符号？

content:
使用前缀码树解码时，从根开始读取比特。

读到 $0$ 就走向标记为 $0$ 的分支，读到 $1$ 就走向标记为 $1$ 的分支。

每到达一个叶节点，就输出该叶节点对应的符号，并回到根继续读取后续比特。

由于前缀码没有码字是另一个码字的前缀，解码是无歧义的。

---

# Huffman 编码

tags: 11.2 Applications of Trees

hint:
如何根据符号频率构造平均长度最短的前缀码？

content:
Huffman 编码是一种根据符号频率构造最优二进制前缀码的贪心算法。

频率高的符号通常得到较短码字。

频率低的符号通常得到较长码字。

Huffman 编码在给定符号频率时，能构造使平均码长最小的二进制前缀码。

---

# Huffman 编码算法

tags: 11.2 Applications of Trees

hint:
Huffman 算法每一步合并哪两棵树？

content:
Huffman 编码算法步骤：

```text
start with one single-vertex tree for each symbol
assign each tree the symbol frequency as weight
while more than one tree remains
    choose two trees with smallest weights
    combine them under a new root
    assign the new tree weight equal to the sum of the two weights
label the two new edges 0 and 1
return the resulting binary tree
```

最终每个符号对应一个叶节点，其码字是从根到该叶节点路径上的 $0$ 和 $1$ 标签串。

---

# Huffman 平均码长

tags: 11.2 Applications of Trees

hint:
一个前缀码的平均比特数怎样计算？

content:
设符号 $s_i$ 的频率为 $p_i$，码字长度为 $l_i$。

该编码的平均码长为：

$$
\sum_i p_i l_i
$$

Huffman 算法的目标是在所有二进制前缀码中使这个加权平均值最小。

---

# Huffman 编码的贪心性

tags: 11.2 Applications of Trees

hint:
为什么 Huffman 算法属于贪心算法？

content:
Huffman 算法每一步都选择当前权重最小的两棵树合并。

这个选择是局部最优的：最低频率的符号被安排在较深位置。

Huffman 算法的正确性说明，这种反复局部最优选择能得到全局最优前缀码。

---

# 博弈树

tags: 11.2 Applications of Trees

hint:
两人轮流游戏如何用树表示？

content:
博弈树用顶点表示游戏局面，用边表示合法走法。

根表示初始局面。

从某个局面到下一个局面的合法移动对应一条边。

叶节点表示游戏结束局面。

从根到叶的一条路径表示一盘完整游戏过程。

博弈树适用于没有随机因素、双方轮流行动且信息完全的游戏。

---

# 博弈树的终局值

tags: 11.2 Applications of Trees

hint:
如何用胜负值标记博弈树叶节点？

content:
在博弈树中，可给叶节点赋值表示终局结果。

例如对当前玩家而言：

$$
1
$$

表示必胜局面；

$$
0
$$

表示必败局面。

然后可以从叶节点向上反推内部节点的值。

如果当前玩家存在至少一个走法能到达对手必败局面，则当前局面为必胜。

如果所有走法都会到达对手必胜局面，则当前局面为必败。

---

# 极小极大思想

tags: 11.2 Applications of Trees

hint:
双方都采取最优策略时，如何从博弈树倒推结果？

content:
极小极大思想用于分析双方轮流、完全信息、零和博弈。

在自己的回合，玩家选择使自己结果最好的子节点。

在对手回合，假设对手会选择使自己最差的子节点。

因此可以从叶节点向上递归计算每个局面的值。

这种方法可以判断在双方都采取最优策略时，起始局面对先手是胜还是负。

---

# 有序根树的通用地址系统

tags: 11.3 Tree Traversal

hint:
怎样给有序根树的每个顶点分配一个地址？

content:
在有序根树中，可以用通用地址系统标记顶点。

根的地址为：

$$
0
$$

根的从左到右第 $i$ 个孩子地址为：

$$
i
$$

若某顶点地址为：

$$
a
$$

则它的第 $j$ 个孩子地址为：

$$
a.j
$$

这样，每个顶点都由从根到该顶点路径上的孩子编号唯一标记。

---

# 有序根树的字典序

tags: 11.3 Tree Traversal

hint:
通用地址如何按字典序排列？

content:
有序根树的地址可以按字典序比较。

两个地址从左到右比较，找到第一个不同的位置。

较小编号所在的地址在前。

若一个地址是另一个地址的前缀，则较短地址在前。

这种字典序与前序遍历得到的顶点顺序一致。

---

# 树遍历

tags: 11.3 Tree Traversal

hint:
系统访问树中每个顶点的算法叫什么？

content:
树遍历是系统访问有序根树中每个顶点的过程。

常见遍历包括：

1. 前序遍历；
2. 中序遍历；
3. 后序遍历。

这些遍历通常用递归定义，广泛用于表达式处理、语法分析、搜索和树结构编码。

---

# 前序遍历

tags: 11.3 Tree Traversal

hint:
前序遍历先访问根还是子树？

content:
前序遍历的规则是：先访问根，再从左到右依次前序遍历各子树。

若根为 $r$，从左到右子树为：

$$
T_1,T_2,\ldots,T_n
$$

则前序遍历顺序为：

$$
r,\ \operatorname{preorder}(T_1),\ \operatorname{preorder}(T_2),\ldots,\operatorname{preorder}(T_n)
$$

---

# 前序遍历伪代码

tags: 11.3 Tree Traversal

hint:
preorder 的递归算法怎样写？

content:
前序遍历伪代码：

```text
procedure preorder(T)
    r := root of T
    list r
    for each child c of r from left to right
        T_c := subtree with c as root
        preorder(T_c)
```

前序遍历先处理当前节点，再处理所有子树。

---

# 中序遍历

tags: 11.3 Tree Traversal

hint:
中序遍历在什么时候访问根？

content:
对有序根树，中序遍历先中序遍历最左子树，然后访问根，再从左到右中序遍历其余子树。

若根为 $r$，子树为：

$$
T_1,T_2,\ldots,T_n
$$

则中序遍历顺序为：

$$
\operatorname{inorder}(T_1),\ r,\ \operatorname{inorder}(T_2),\ldots,\operatorname{inorder}(T_n)
$$

在二叉树中，这对应“左子树、根、右子树”。

---

# 中序遍历伪代码

tags: 11.3 Tree Traversal

hint:
inorder 的递归算法怎样写？

content:
中序遍历伪代码：

```text
procedure inorder(T)
    r := root of T
    if r is a leaf
        list r
    else
        l := first child of r from left to right
        inorder(subtree rooted at l)
        list r
        for each remaining child c of r from left to right
            inorder(subtree rooted at c)
```

对二叉树而言，它就是先遍历左子树，再访问根，最后遍历右子树。

---

# 后序遍历

tags: 11.3 Tree Traversal

hint:
后序遍历什么时候访问根？

content:
后序遍历的规则是：先从左到右后序遍历所有子树，最后访问根。

若根为 $r$，子树为：

$$
T_1,T_2,\ldots,T_n
$$

则后序遍历顺序为：

$$
\operatorname{postorder}(T_1),\operatorname{postorder}(T_2),\ldots,\operatorname{postorder}(T_n),\ r
$$

---

# 后序遍历伪代码

tags: 11.3 Tree Traversal

hint:
postorder 的递归算法怎样写？

content:
后序遍历伪代码：

```text
procedure postorder(T)
    r := root of T
    for each child c of r from left to right
        T_c := subtree with c as root
        postorder(T_c)
    list r
```

后序遍历先处理子树，再处理根。

---

# 表达式树

tags: 11.3 Tree Traversal

hint:
算术表达式如何用二叉树表示？

content:
表达式树是表示算术表达式的有序二叉树。

内部节点通常表示运算符。

叶节点通常表示变量或常数。

若内部节点表示二元运算符，则其左子树和右子树分别表示该运算符的两个操作数。

表达式树可以消除括号歧义，并支持不同遍历得到不同表达式记法。

---

# 前缀记法

tags: 11.3 Tree Traversal

hint:
表达式树的前序遍历得到什么记法？

content:
对表达式树进行前序遍历，得到表达式的前缀形式。

前缀记法也称 Polish notation。

在前缀记法中，运算符写在操作数之前。

例如二元运算可写作：

$$
+xy
$$

表示：

$$
x+y
$$

前缀记法在运算符元数固定时不需要括号。

---

# 后缀记法

tags: 11.3 Tree Traversal

hint:
表达式树的后序遍历得到什么记法？

content:
对表达式树进行后序遍历，得到表达式的后缀形式。

后缀记法也称 reverse Polish notation。

在后缀记法中，运算符写在操作数之后。

例如：

$$
xy+
$$

表示：

$$
x+y
$$

后缀记法在运算符元数固定时不需要括号，常适合用栈计算。

---

# 中缀记法

tags: 11.3 Tree Traversal

hint:
表达式树的中序遍历得到什么记法？

content:
对表达式树进行中序遍历，得到表达式的中缀形式。

中缀记法把二元运算符写在两个操作数之间。

例如：

$$
x+y
$$

就是中缀形式。

中缀形式通常需要括号或运算符优先级规则来消除歧义。

---

# 前缀表达式求值

tags: 11.3 Tree Traversal

hint:
前缀表达式为什么可以从右向左求值？

content:
前缀表达式中，二元运算符位于两个操作数之前。

求值时可从右向左扫描。

遇到操作数时保存；遇到运算符时，取其右侧最近的两个可用操作数进行计算，并把结果作为新操作数。

由于前缀记法无歧义，因此不需要括号。

---

# 后缀表达式求值

tags: 11.3 Tree Traversal

hint:
后缀表达式为什么适合用栈求值？

content:
后缀表达式中，二元运算符位于两个操作数之后。

求值时从左向右扫描：

1. 遇到操作数就压入栈；
2. 遇到二元运算符就弹出两个操作数；
3. 计算结果再压回栈。

扫描结束后，栈中唯一元素就是表达式值。

---

# 遍历与树结构编码

tags: 11.3 Tree Traversal

hint:
仅知道遍历序列是否一定能恢复有序根树？

content:
前序遍历或后序遍历本身通常不足以唯一确定一般有序根树。

如果同时知道每个顶点的子节点数，则前序遍历或后序遍历可以唯一确定有序根树。

对于满有序 $m$ 叉树，由于每个内部节点的子节点数固定，因此前序或后序遍历结合节点类型信息常能编码结构。

---

# 生成树

tags: 11.4 Spanning Trees

hint:
包含原图全部顶点的树形子图叫什么？

content:
设 $G$ 是简单图。

$G$ 的生成树是 $G$ 的一个子图，满足：

1. 包含 $G$ 的所有顶点；
2. 是一棵树。

若 $G$ 有 $n$ 个顶点，则任意生成树都有：

$$
n-1
$$

条边。

---

# 生成树存在条件

tags: 11.4 Spanning Trees

hint:
什么样的简单图有生成树？

content:
一个简单图有生成树，当且仅当它是连通图。

若图有生成树，则生成树本身连通且包含所有顶点，所以原图连通。

若原图连通，可以通过删除回路中的边，直到没有回路为止，得到一棵包含所有顶点的树，即生成树。

---

# 生成树的用途

tags: 11.4 Spanning Trees

hint:
为什么连通网络常要找一棵生成树？

content:
生成树提供连接所有顶点所需的最小边集。

在网络中，生成树可用于：

1. 删除冗余连接；
2. 保持所有节点连通；
3. 构建广播结构；
4. 避免消息循环；
5. 设计低成本网络骨架；
6. 系统遍历图中所有顶点。

---

# 深度优先搜索

tags: 11.4 Spanning Trees

hint:
DFS 如何沿一条路径尽可能走到底？

content:
深度优先搜索从某个起点开始，尽可能沿未访问边向前走。

当当前顶点没有未访问邻居时，回溯到上一个仍有未访问邻居的顶点。

在连通图中，DFS 可构造一棵生成树。

DFS 也称为 backtracking。

---

# DFS 生成树

tags: 11.4 Spanning Trees

hint:
DFS 中哪些边进入生成树？

content:
在深度优先搜索中，每当第一次访问一个新顶点时，使用的那条边被加入生成树。

若遇到已经访问过的顶点，则不加入该边，以免形成回路。

最终在连通图中，所有顶点都被访问，加入的边构成一棵生成树。

---

# DFS 伪代码思想

tags: 11.4 Spanning Trees

hint:
DFS 的递归结构是什么？

content:
DFS 可递归描述为：

```text
procedure DFS(v)
    mark v as visited
    for each neighbor w of v
        if w is not visited
            add edge {v,w} to tree
            DFS(w)
```

若图不连通，则需要对每个尚未访问的顶点重新启动 DFS，以得到生成森林。

---

# 广度优先搜索

tags: 11.4 Spanning Trees

hint:
BFS 如何一层一层扩展？

content:
广度优先搜索从起点开始，先访问所有距离起点为 $1$ 的顶点，再访问距离为 $2$ 的顶点，依此类推。

它按层次扩展搜索边界。

在连通图中，BFS 可构造一棵生成树。

BFS 通常用队列实现。

---

# BFS 生成树

tags: 11.4 Spanning Trees

hint:
BFS 中树的层数表示什么？

content:
BFS 生成树中，一个顶点所在层数等于它从起点出发的最短路径长度。

若起点为根，则第 $k$ 层顶点是距离根为：

$$
k
$$

的顶点。

因此，在无权图中，BFS 生成树可以用于求从起点到各顶点的最短路径。

---

# BFS 伪代码思想

tags: 11.4 Spanning Trees

hint:
BFS 为什么通常使用队列？

content:
BFS 可用队列实现：

```text
procedure BFS(s)
    mark s as visited
    enqueue s
    while queue is not empty
        v := dequeue
        for each neighbor w of v
            if w is not visited
                mark w as visited
                add edge {v,w} to tree
                enqueue w
```

队列保证先发现的顶点先被扩展，因此搜索按层次进行。

---

# DFS 与 BFS 的区别

tags: 11.4 Spanning Trees

hint:
DFS 和 BFS 的搜索形态有什么不同？

content:
DFS 优先沿一条路径深入，无法继续时再回溯。

BFS 优先访问当前层的所有顶点，再进入下一层。

DFS 常用于回溯、拓扑搜索、连通性分析和生成深层搜索树。

BFS 常用于无权最短路径、层次结构和逐层扩展搜索。

---

# 回溯

tags: 11.4 Spanning Trees

hint:
当当前选择走不通时，算法怎样返回并尝试其他选择？

content:
回溯是一种系统搜索方法。

它逐步构造候选解；当发现当前部分解不可能扩展为完整解时，就撤销最近选择，返回上一步尝试其他选择。

回溯过程常可表示为搜索树。

DFS 是实现回溯搜索的常见方式。

---

# 用回溯解决图着色

tags: 11.4 Spanning Trees

hint:
怎样系统尝试给图用 $k$ 种颜色着色？

content:
图着色的回溯方法按顶点顺序逐个尝试颜色。

对当前顶点，尝试所有不与已着色邻居冲突的颜色。

若某个选择导致后续无法完成，则撤销该颜色选择并尝试下一个颜色。

若所有顶点都成功着色，则得到一个合法 $k$ 着色。

若所有尝试失败，则图不能用 $k$ 种颜色着色。

---

# 生成树与图遍历

tags: 11.4 Spanning Trees

hint:
为什么遍历图时自然会得到生成树？

content:
在连通图中，从一个起点开始遍历所有顶点时，每个非起点顶点第一次被发现时，都有一条边把它连接到已经访问过的顶点。

把这些首次发现边收集起来，得到的子图：

1. 包含所有顶点；
2. 连通；
3. 没有回路。

因此它是一棵生成树。

---

# 带权图

tags: 11.5 Minimum Spanning Trees

hint:
边上有代价时，生成树应如何选择？

content:
带权图是边带有数值权重的图。

权重可表示成本、距离、时间、长度、延迟或容量等。

在带权连通无向图中，每棵生成树都有总权重：

$$
\sum_{e\in T}w(e)
$$

其中 $T$ 是生成树的边集合。

---

# 最小生成树

tags: 11.5 Minimum Spanning Trees

hint:
在所有生成树中，总边权最小的树叫什么？

content:
连通带权无向图的最小生成树是总权重最小的生成树。

也就是说，若 $T$ 是生成树，目标是最小化：

$$
\sum_{e\in T}w(e)
$$

最小生成树连接所有顶点，同时使选中边的总代价最小。

---

# 最小生成树的应用

tags: 11.5 Minimum Spanning Trees

hint:
哪些网络设计问题可以转化为 MST？

content:
最小生成树可用于设计低成本连通网络。

典型应用包括：

1. 通信网络铺设；
2. 电缆或光纤连接；
3. 道路或管道连接；
4. 传感器网络骨架；
5. 聚类分析；
6. 近似求解某些优化问题。

关键要求是：连接所有顶点，并使边总代价最小。

---

# Prim 算法

tags: 11.5 Minimum Spanning Trees

hint:
Prim 算法每一步选哪条边？

content:
Prim 算法从一条最小权重边或一个初始顶点开始，逐步扩展一棵树。

每一步选择一条权重最小的边，要求：

1. 该边与当前树中的某个顶点关联；
2. 加入该边后不形成简单回路；
3. 该边把一个新顶点加入树中。

重复直到树包含所有顶点。

---

# Prim 算法伪代码

tags: 11.5 Minimum Spanning Trees

hint:
Prim 的生成过程怎样写成伪代码？

content:
Prim 算法可写为：

```text
procedure Prim(G)
    T := a minimum-weight edge
    while T has fewer than n - 1 edges
        choose a minimum-weight edge incident to a vertex in T
        such that adding it does not form a simple circuit
        add this edge to T
    return T
```

其中 $G$ 是有 $n$ 个顶点的连通带权无向图。

最终 $T$ 是最小生成树。

---

# Kruskal 算法

tags: 11.5 Minimum Spanning Trees

hint:
Kruskal 算法每一步从全图选什么边？

content:
Kruskal 算法从空图开始，按边权从小到大选择边。

每一步选择当前权重最小且加入后不会形成简单回路的边。

重复直到已经选择：

$$
n-1
$$

条边。

最终得到最小生成树。

---

# Kruskal 算法伪代码

tags: 11.5 Minimum Spanning Trees

hint:
Kruskal 的选边过程怎样写成伪代码？

content:
Kruskal 算法可写为：

```text
procedure Kruskal(G)
    T := empty graph
    while T has fewer than n - 1 edges
        choose a minimum-weight edge in G
        that does not form a simple circuit with edges already in T
        add this edge to T
    return T
```

其中 $G$ 是有 $n$ 个顶点的连通带权无向图。

---

# Prim 与 Kruskal 的区别

tags: 11.5 Minimum Spanning Trees

hint:
两个 MST 算法的选边视角有什么不同？

content:
Prim 算法始终维护一棵正在扩展的树。

它每一步选择连接当前树和树外顶点的最小权重边。

Kruskal 算法始终维护一个无回路的边集合，可能包含多个连通分量。

它每一步从全图剩余边中选择不会产生回路的最小权重边。

两者都是贪心算法，并且都能得到最小生成树。

---

# MST 不唯一性

tags: 11.5 Minimum Spanning Trees

hint:
最小生成树是否一定唯一？

content:
最小生成树不一定唯一。

当图中存在相同权重边时，Prim 算法或 Kruskal 算法在某些步骤可能有多个合法选择。

不同选择可能得到不同生成树，但它们的总权重相同，且都可能是最小生成树。

若所有边权互不相同，则最小生成树唯一。

---

# MST 贪心选择的安全性

tags: 11.5 Minimum Spanning Trees

hint:
为什么 MST 算法可以安全地选某些最轻边？

content:
最小生成树算法依赖安全边思想。

直观地说，若把顶点集合分成两部分，连接这两部分的所有边中权重最小的边，可以安全地加入某棵最小生成树。

Prim 算法每次选择连接当前树与外部顶点的最轻边。

Kruskal 算法每次选择连接两个不同分量的最轻可用边。

这些选择不会破坏形成最小生成树的可能性。

---

# MST 与最短路径的区别

tags: 11.5 Minimum Spanning Trees

hint:
最小生成树是否保证任意两点间路径最短？

content:
最小生成树最小化的是连接所有顶点的总边权。

它不保证树中任意两点之间的路径是原图中的最短路径。

最短路径问题关注指定顶点对之间的路径权重最小。

最小生成树问题关注用最小总成本连接全部顶点。

因此 MST 和最短路径是不同问题。

---

# 第十一章核心思想

tags: 11.5 Minimum Spanning Trees

hint:
Trees 这一章的主线是什么？

content:
第十一章的核心是树结构及其算法应用。

主要内容包括：

1. 树、森林和树的基本性质；
2. 根树、$m$ 叉树、二叉树、叶节点、内部节点和高度；
3. 满 $m$ 叉树的顶点数和叶节点公式；
4. 二叉搜索树、决策树、前缀码、Huffman 编码和博弈树；
5. 前序、中序、后序遍历及其在表达式中的应用；
6. 生成树、DFS、BFS 和回溯；
7. 最小生成树及 Prim、Kruskal 两种贪心算法。

树将图论、递归、搜索、编码、排序下界和网络优化连接在一起。

---

# 布尔代数的对象

tags: 12.1 Boolean Functions

hint:
布尔代数主要研究哪两个值？

content:
布尔代数研究集合：

$$
B=\{0,1\}
$$

上的运算与规则。

其中 $0$ 和 $1$ 可分别表示假与真，也可表示开关的关与开、电子信号的低与高。

布尔代数是逻辑电路设计的数学基础。

---

# 布尔补

tags: 12.1 Boolean Functions

hint:
布尔值取反怎样表示？

content:
布尔补是把布尔值反转的运算。

布尔补通常用横线表示：

$$
\overline{0}=1
$$

$$
\overline{1}=0
$$

若变量为 $x$，其补为：

$$
\overline{x}
$$

布尔补对应命题逻辑中的否定：

$$
\neg p
$$

---

# 布尔和

tags: 12.1 Boolean Functions

hint:
Boolean sum 对应逻辑中的哪种运算？

content:
布尔和记为：

$$
x+y
$$

也称 OR 运算。

其取值规则为：

| $x$ | $y$ | $x+y$ |
|---|---|---|
| 1 | 1 | 1 |
| 1 | 0 | 1 |
| 0 | 1 | 1 |
| 0 | 0 | 0 |

布尔和对应命题逻辑中的析取：

$$
p\lor q
$$

---

# 布尔积

tags: 12.1 Boolean Functions

hint:
Boolean product 对应逻辑中的哪种运算？

content:
布尔积记为：

$$
x\cdot y
$$

也可省略点号写作：

$$
xy
$$

它也称 AND 运算。

其取值规则为：

| $x$ | $y$ | $xy$ |
|---|---|---|
| 1 | 1 | 1 |
| 1 | 0 | 0 |
| 0 | 1 | 0 |
| 0 | 0 | 0 |

布尔积对应命题逻辑中的合取：

$$
p\land q
$$

---

# 布尔运算优先级

tags: 12.1 Boolean Functions

hint:
没有括号时，补、积、和按什么顺序计算？

content:
布尔表达式中，若没有括号，通常按以下优先级计算：

1. 先计算补；
2. 再计算布尔积；
3. 最后计算布尔和。

例如表达式：

$$
\overline{x}y+z
$$

应理解为：

$$
(\overline{x}y)+z
$$

为了避免歧义，复杂表达式中应使用括号。

---

# 布尔函数

tags: 12.1 Boolean Functions

hint:
布尔函数的输入和输出都来自哪个集合？

content:
$n$ 元布尔函数是从：

$$
B^n
$$

到：

$$
B
$$

的函数，其中：

$$
B=\{0,1\}
$$

也就是说，一个 $n$ 元布尔函数可写为：

$$
F:B^n\to B
$$

它把每个 $n$ 位布尔输入映射为一个布尔输出。

---

# 布尔函数的次数

tags: 12.1 Boolean Functions

hint:
Boolean function of degree $n$ 中的 $n$ 表示什么？

content:
若布尔函数有 $n$ 个布尔变量作为输入，则称它是 $n$ 次布尔函数。

例如：

$$
F(x,y,z)
$$

是 $3$ 次布尔函数。

它的定义域为：

$$
B^3
$$

共有：

$$
2^3
$$

种不同输入组合。

---

# 布尔函数的个数

tags: 12.1 Boolean Functions

hint:
$n$ 个变量的布尔函数一共有多少个？

content:
$n$ 个布尔变量共有：

$$
2^n
$$

种输入组合。

对每个输入组合，函数值都可以独立选择为 $0$ 或 $1$。

因此，$n$ 次布尔函数总数为：

$$
2^{2^n}
$$

例如，$3$ 次布尔函数共有：

$$
2^{2^3}=256
$$

个。

---

# 布尔表达式

tags: 12.1 Boolean Functions

hint:
布尔表达式如何递归构造？

content:
布尔表达式可递归定义。

基础表达式包括：

$$
0,\quad 1,\quad x_1,x_2,\ldots,x_n
$$

如果 $E_1$ 和 $E_2$ 是布尔表达式，则以下也是布尔表达式：

$$
\overline{E_1}
$$

$$
(E_1+E_2)
$$

$$
(E_1E_2)
$$

布尔表达式通过布尔补、布尔和、布尔积构造布尔函数。

---

# 布尔表达式表示布尔函数

tags: 12.1 Boolean Functions

hint:
一个表达式怎样决定一个布尔函数？

content:
布尔表达式中的每个变量取值为 $0$ 或 $1$ 后，表达式通过布尔运算得到一个 $0$ 或 $1$。

因此，一个含有 $n$ 个变量的布尔表达式定义了一个 $n$ 次布尔函数。

不同表达式可以表示同一个布尔函数。

例如，若两个表达式对所有输入组合都有相同输出，则它们表示同一个布尔函数。

---

# 布尔表达式相等

tags: 12.1 Boolean Functions

hint:
两个布尔表达式何时表示同一个函数？

content:
若两个布尔表达式 $E_1$ 和 $E_2$ 对所有变量取值都给出相同结果，则称它们相等。

也就是说：

$$
E_1=E_2
$$

当且仅当：

$$
E_1(x_1,\ldots,x_n)=E_2(x_1,\ldots,x_n)
$$

对所有：

$$
(x_1,\ldots,x_n)\in B^n
$$

成立。

---

# 布尔表达式与命题逻辑

tags: 12.1 Boolean Functions

hint:
布尔代数中的 $0,1,+,\cdot,\overline{x}$ 分别对应命题逻辑中的什么？

content:
布尔代数与命题逻辑可以相互翻译。

对应关系为：

| 布尔代数 | 命题逻辑 |
|---|---|
| $0$ | $F$ |
| $1$ | $T$ |
| $x+y$ | $p\lor q$ |
| $xy$ | $p\land q$ |
| $\overline{x}$ | $\neg p$ |

因此，布尔恒等式可以转化为命题逻辑等价式。

---

# 布尔表达式的对偶

tags: 12.1 Boolean Functions

hint:
布尔表达式的 dual 怎样得到？

content:
布尔表达式的对偶通过以下替换得到：

1. 把所有布尔和 $+$ 换成布尔积 $\cdot$；
2. 把所有布尔积 $\cdot$ 换成布尔和 $+$；
3. 把所有 $0$ 换成 $1$；
4. 把所有 $1$ 换成 $0$；
5. 变量和补不变。

对偶运算不改变变量本身，只交换运算和常量。

---

# 对偶原理

tags: 12.1 Boolean Functions

hint:
若一个布尔恒等式成立，它的对偶是否也成立？

content:
对偶原理说明：若一个布尔恒等式成立，则把等式两边都取对偶后得到的新恒等式也成立。

例如，若有恒等式：

$$
x+0=x
$$

取对偶得到：

$$
x\cdot1=x
$$

也成立。

对偶原理可以由一个恒等式自动产生另一个有效恒等式。

---

# 双重补律

tags: 12.1 Boolean Functions

hint:
对一个布尔变量连续取两次补会怎样？

content:
双重补律为：

$$
\overline{\overline{x}}=x
$$

它表示对布尔值取反两次会回到原值。

该恒等式对应命题逻辑中的双重否定律：

$$
\neg\neg p\equiv p
$$

---

# 布尔幂等律

tags: 12.1 Boolean Functions

hint:
变量和自身做 OR 或 AND 会得到什么？

content:
布尔代数的幂等律为：

$$
x+x=x
$$

$$
x\cdot x=x
$$

也可写为：

$$
xx=x
$$

它们说明，同一个条件重复进行 OR 或 AND 不会改变结果。

---

# 布尔恒等律

tags: 12.1 Boolean Functions

hint:
$0$ 和 $1$ 分别在哪些运算中是单位元？

content:
布尔恒等律为：

$$
x+0=x
$$

$$
x\cdot1=x
$$

其中 $0$ 是布尔和的单位元，$1$ 是布尔积的单位元。

---

# 布尔支配律

tags: 12.1 Boolean Functions

hint:
和 $1$ 做 OR、和 $0$ 做 AND 会得到什么？

content:
布尔支配律为：

$$
x+1=1
$$

$$
x\cdot0=0
$$

也就是说，只要 OR 中出现 $1$，结果就是 $1$；只要 AND 中出现 $0$，结果就是 $0$。

---

# 布尔交换律与结合律

tags: 12.1 Boolean Functions

hint:
布尔和与布尔积的顺序和括号是否重要？

content:
交换律：

$$
x+y=y+x
$$

$$
xy=yx
$$

结合律：

$$
x+(y+z)=(x+y)+z
$$

$$
x(yz)=(xy)z
$$

因此，多个布尔和或多个布尔积可以不写括号。

---

# 布尔分配律

tags: 12.1 Boolean Functions

hint:
布尔代数中，和与积如何相互分配？

content:
布尔分配律有两条：

$$
x+yz=(x+y)(x+z)
$$

$$
x(y+z)=xy+xz
$$

第一条是布尔和对布尔积的分配，第二条是布尔积对布尔和的分配。

这与集合代数和命题逻辑中的分配律对应。

---

# 布尔德摩根律

tags: 12.1 Boolean Functions

hint:
补一个和或积时，运算符怎样改变？

content:
布尔代数中的德摩根律为：

$$
\overline{xy}=\overline{x}+\overline{y}
$$

$$
\overline{x+y}=\overline{x}\,\overline{y}
$$

它们对应命题逻辑中：

$$
\neg(p\land q)\equiv \neg p\lor\neg q
$$

和：

$$
\neg(p\lor q)\equiv \neg p\land\neg q
$$

---

# 布尔吸收律

tags: 12.1 Boolean Functions

hint:
$x$ 与含 $x$ 的更复杂项组合时如何化简？

content:
布尔吸收律为：

$$
x+xy=x
$$

$$
x(x+y)=x
$$

吸收律可用于简化布尔表达式，减少电路中的门数量。

---

# 布尔补元律

tags: 12.1 Boolean Functions

hint:
一个变量和它的补做 OR 或 AND 会得到什么？

content:
布尔补元律为：

$$
x+\overline{x}=1
$$

$$
x\overline{x}=0
$$

第一条称为单位性质，第二条称为零性质。

它们对应命题逻辑中的排中律和矛盾律。

---

# 抽象布尔代数

tags: 12.1 Boolean Functions

hint:
除了 $\{0,1\}$，还有哪些结构也满足布尔代数规则？

content:
布尔代数可以抽象定义为一个集合 $B$，配有两个二元运算：

$$
\vee,\quad \wedge
$$

两个特殊元素：

$$
0,\quad 1
$$

以及补运算：

$$
\overline{x}
$$

并满足恒等律、补元律、结合律、交换律和分配律等规则。

集合代数、命题逻辑等都可以看作布尔代数实例。

---

# 集合代数作为布尔代数

tags: 12.1 Boolean Functions

hint:
幂集在并、交、补下为什么是布尔代数？

content:
设全集为 $U$。幂集：

$$
\mathcal{P}(U)
$$

在集合并、集合交和补集运算下构成布尔代数。

对应关系为：

$$
A\vee B=A\cup B
$$

$$
A\wedge B=A\cap B
$$

$$
\overline{A}=U-A
$$

$$
0=\varnothing
$$

$$
1=U
$$

集合恒等式正是布尔代数恒等式的一个模型。

---

# 布尔代数与分配补格

tags: 12.1 Boolean Functions

hint:
从格的观点看，怎样得到布尔代数？

content:
从格论观点看，若一个格满足：

1. 有最小元 $0$；
2. 有最大元 $1$；
3. 每个元素都有补元；
4. 满足分配律；

则它是布尔代数。

这里 join 对应：

$$
\vee
$$

meet 对应：

$$
\wedge
$$

补元满足：

$$
x\vee\overline{x}=1
$$

和：

$$
x\wedge\overline{x}=0
$$

---

# 文字

tags: 12.2 Representing Boolean Functions

hint:
literal 是变量本身还是变量的补？

content:
布尔变量 $x$ 的文字是：

$$
x
$$

或：

$$
\overline{x}
$$

也就是说，文字可以是变量本身，也可以是变量的补。

在布尔函数表示中，文字是构成乘积项与和项的基本单位。

---

# 小项

tags: 12.2 Representing Boolean Functions

hint:
每个变量恰好出现一次的布尔积叫什么？

content:
关于变量：

$$
x_1,x_2,\ldots,x_n
$$

的小项是布尔积：

$$
y_1y_2\cdots y_n
$$

其中每个：

$$
y_i
$$

是：

$$
x_i
$$

或：

$$
\overline{x_i}
$$

小项在恰好一个输入组合上取值为 $1$，在其他输入组合上取值为 $0$。

---

# 输入组合对应的小项

tags: 12.2 Representing Boolean Functions

hint:
给定一行真值表，如何写出对应小项？

content:
对输入组合：

$$
(a_1,a_2,\ldots,a_n)
$$

其中每个：

$$
a_i\in\{0,1\}
$$

构造小项时：

若：

$$
a_i=1
$$

则使用文字：

$$
x_i
$$

若：

$$
a_i=0
$$

则使用文字：

$$
\overline{x_i}
$$

这些文字相乘得到的小项只在该输入组合上取值为 $1$。

---

# 积之和展开

tags: 12.2 Representing Boolean Functions

hint:
怎样从真值表中函数值为 $1$ 的行得到表达式？

content:
积之和展开是用小项的布尔和表示布尔函数。

做法：

1. 找出真值表中函数值为 $1$ 的所有输入组合；
2. 对每个这样的输入组合写出对应小项；
3. 把这些小项用布尔和连接。

若函数 $F$ 在小项 $m_1,m_2,\ldots,m_k$ 对应的输入上为 $1$，则：

$$
F=m_1+m_2+\cdots+m_k
$$

---

# 析取范式

tags: 12.2 Representing Boolean Functions

hint:
sum-of-products expansion 又叫什么？

content:
积之和展开也称为析取范式，英文为 disjunctive normal form。

它把布尔函数表示为若干小项的 OR。

每个小项是若干文字的 AND。

因此形式为：

$$
F=P_1+P_2+\cdots+P_k
$$

其中每个 $P_i$ 是布尔积。

---

# 每个布尔函数都有积之和展开

tags: 12.2 Representing Boolean Functions

hint:
任意布尔函数都能用 AND、OR、NOT 表示吗？

content:
每个布尔函数都可以表示为积之和展开。

理由是：对函数值为 $1$ 的每个输入组合，都有一个对应小项只在该组合上为 $1$。

把所有这些小项做布尔和，就会在且仅在原函数为 $1$ 的输入组合上取值为 $1$。

因此任意布尔函数都可由：

$$
+,\quad \cdot,\quad \overline{\phantom{x}}
$$

表示。

---

# 零函数的积之和展开

tags: 12.2 Representing Boolean Functions

hint:
若布尔函数对所有输入都为 $0$，积之和怎样写？

content:
若布尔函数 $F$ 对所有输入组合都取值为 $0$，则没有任何小项需要加入积之和展开。

此时可把函数表示为常量：

$$
F=0
$$

这对应空的布尔和。

---

# 一函数的积之和展开

tags: 12.2 Representing Boolean Functions

hint:
若布尔函数对所有输入都为 $1$，积之和包含哪些小项？

content:
若 $n$ 元布尔函数 $F$ 对所有输入组合都取值为 $1$，则其积之和展开包含全部：

$$
2^n
$$

个小项。

该展开等价于常量函数：

$$
F=1
$$

在化简时，所有小项的和可以化简为 $1$。

---

# 从积之和到电路

tags: 12.2 Representing Boolean Functions

hint:
积之和展开如何直接变成逻辑电路？

content:
积之和展开可以直接转化为两层逻辑电路。

每个小项用 AND 门实现。

所有小项的输出再输入到 OR 门。

若某个文字是补变量，则先使用 inverter 得到该补变量。

因此，积之和展开给出了由 NOT、AND、OR 门实现布尔函数的系统方法。

---

# 函数完备性

tags: 12.2 Representing Boolean Functions

hint:
一组布尔运算什么时候 functionally complete？

content:
若一组布尔运算可以表示任意布尔函数，则称这组运算是函数完备的。

由于每个布尔函数都有积之和展开，所以运算集合：

$$
\{+,\cdot,\overline{\phantom{x}}\}
$$

是函数完备的。

如果能用某组运算表示补、和、积，则该组运算也是函数完备的。

---

# $\{+,\overline{\phantom{x}}\}$ 的函数完备性

tags: 12.2 Representing Boolean Functions

hint:
只用 OR 和 NOT，如何得到 AND？

content:
运算集合：

$$
\{+,\overline{\phantom{x}}\}
$$

是函数完备的。

因为布尔积可由德摩根律表示为：

$$
xy=\overline{\overline{x}+\overline{y}}
$$

既然可以用 OR 和 NOT 构造 AND，而 OR 和 NOT 已经可用，就能表示任意布尔函数。

---

# $\{\cdot,\overline{\phantom{x}}\}$ 的函数完备性

tags: 12.2 Representing Boolean Functions

hint:
只用 AND 和 NOT，如何得到 OR？

content:
运算集合：

$$
\{\cdot,\overline{\phantom{x}}\}
$$

是函数完备的。

因为布尔和可由德摩根律表示为：

$$
x+y=\overline{\overline{x}\,\overline{y}}
$$

既然可以用 AND 和 NOT 构造 OR，而 AND 和 NOT 已经可用，就能表示任意布尔函数。

---

# NAND 运算

tags: 12.2 Representing Boolean Functions

hint:
NAND 什么时候输出 $0$？

content:
NAND 运算记为：

$$
x\mid y
$$

定义为：

$$
x\mid y=\overline{xy}
$$

它只有在：

$$
x=1,\quad y=1
$$

时输出 $0$，其他情况下输出 $1$。

NAND 是 AND 后接 NOT 的复合运算。

---

# 用 NAND 表示补、积、和

tags: 12.2 Representing Boolean Functions

hint:
为什么单独一个 NAND 运算就是函数完备的？

content:
NAND 可表示补：

$$
\overline{x}=x\mid x
$$

NAND 可表示布尔积：

$$
xy=(x\mid y)\mid(x\mid y)
$$

NAND 可表示布尔和：

$$
x+y=(x\mid x)\mid(y\mid y)
$$

因此，仅使用 NAND 运算就能表示任意布尔函数。

---

# NOR 运算

tags: 12.2 Representing Boolean Functions

hint:
NOR 什么时候输出 $1$？

content:
NOR 运算记为：

$$
x\downarrow y
$$

定义为：

$$
x\downarrow y=\overline{x+y}
$$

它只有在：

$$
x=0,\quad y=0
$$

时输出 $1$，其他情况下输出 $0$。

NOR 是 OR 后接 NOT 的复合运算。

---

# 用 NOR 表示补、和、积

tags: 12.2 Representing Boolean Functions

hint:
为什么单独一个 NOR 运算也是函数完备的？

content:
NOR 可表示补：

$$
\overline{x}=x\downarrow x
$$

NOR 可表示布尔和：

$$
x+y=(x\downarrow y)\downarrow(x\downarrow y)
$$

NOR 可表示布尔积：

$$
xy=(x\downarrow x)\downarrow(y\downarrow y)
$$

因此，仅使用 NOR 运算就能表示任意布尔函数。

---

# 积之和展开不一定最简

tags: 12.2 Representing Boolean Functions

hint:
从真值表直接得到的表达式为什么可能很长？

content:
积之和展开保证能表示任意布尔函数，但不保证最简。

若函数在许多输入组合上取值为 $1$，积之和展开会包含许多小项。

每个小项又包含所有变量的一个文字，因此表达式可能包含大量运算。

后续电路最小化的目标就是把这些表达式化简，减少门数量和输入数量。

---

# 逻辑门

tags: 12.3 Logic Gates

hint:
逻辑门把布尔输入变成什么输出？

content:
逻辑门是实现布尔运算的电路元件。

它接受一个或多个布尔输入：

$$
0,\quad 1
$$

并输出一个布尔值。

常见逻辑门包括 inverter、OR gate、AND gate、NAND gate、NOR gate 和 XOR gate。

逻辑门组合可以实现布尔函数。

---

# Inverter

tags: 12.3 Logic Gates

hint:
inverter 实现哪个布尔运算？

content:
Inverter 是实现布尔补的逻辑门。

输入为 $x$，输出为：

$$
\overline{x}
$$

因此：

$$
0\mapsto1
$$

$$
1\mapsto0
$$

它也称为 NOT gate。

---

# OR gate

tags: 12.3 Logic Gates

hint:
OR gate 的输出是什么？

content:
OR gate 接受两个或多个布尔输入，并输出它们的布尔和。

对两个输入 $x$ 和 $y$，输出为：

$$
x+y
$$

只要至少一个输入为 $1$，输出就是 $1$。

所有输入都为 $0$ 时，输出才为 $0$。

---

# AND gate

tags: 12.3 Logic Gates

hint:
AND gate 的输出是什么？

content:
AND gate 接受两个或多个布尔输入，并输出它们的布尔积。

对两个输入 $x$ 和 $y$，输出为：

$$
xy
$$

只有当所有输入都为 $1$ 时，输出才为 $1$。

只要至少一个输入为 $0$，输出就是 $0$。

---

# 从布尔表达式构造电路

tags: 12.3 Logic Gates

hint:
怎样把布尔表达式系统转成电路？

content:
从布尔表达式构造电路时，可按表达式结构逐层实现：

1. 每个变量对应一个输入信号；
2. 每个补变量使用 inverter；
3. 每个布尔积使用 AND gate；
4. 每个布尔和使用 OR gate；
5. 子表达式输出作为更大表达式的输入。

这种方法可以直接实现任意用补、和、积写成的布尔函数。

---

# 电路输出对应布尔函数

tags: 12.3 Logic Gates

hint:
给定电路后，如何写出它的布尔表达式？

content:
要从电路写出输出表达式，可以从输入端开始沿电路逐步标记每个门的输出。

Inverter 输出补；

OR gate 输出布尔和；

AND gate 输出布尔积。

最终输出端的表达式就是该电路表示的布尔函数。

---

# 两开关控制灯的电路

tags: 12.3 Logic Gates

hint:
灯在至少一个开关闭合时亮，对应哪个布尔函数？

content:
若两个开关输入为 $x$ 和 $y$，灯在至少一个开关闭合时亮，则输出函数为：

$$
F(x,y)=x+y
$$

这可用一个 OR gate 实现。

若灯只有在两个开关都闭合时才亮，则输出函数为：

$$
F(x,y)=xy
$$

这可用一个 AND gate 实现。

---

# 异或电路

tags: 12.3 Logic Gates

hint:
两个输入恰好一个为 $1$ 时输出 $1$，怎样用 AND、OR、NOT 表示？

content:
异或函数满足：当且仅当两个输入恰好一个为 $1$ 时输出 $1$。

可表示为：

$$
x\oplus y=x\overline{y}+\overline{x}y
$$

因此可用 inverter 得到：

$$
\overline{x},\quad \overline{y}
$$

用 AND gate 构造两个乘积项，再用 OR gate 合并。

---

# 半加器

tags: 12.3 Logic Gates

hint:
半加器输入两个 bit，输出哪两个 bit？

content:
半加器用于相加两个 bit。

输入为：

$$
x,\quad y
$$

输出为 sum bit 和 carry bit。

sum bit 为：

$$
s=x\oplus y=x\overline{y}+\overline{x}y
$$

carry bit 为：

$$
c=xy
$$

半加器不接收来自低位的进位输入。

---

# 全加器

tags: 12.3 Logic Gates

hint:
全加器比半加器多了哪个输入？

content:
全加器用于相加两个 bit 和一个进位输入。

输入为：

$$
x,\quad y,\quad z
$$

其中 $z$ 是 carry-in。

输出为 sum bit 和 carry bit。

sum bit 为：

$$
s=x\oplus y\oplus z
$$

carry bit 为：

$$
c=xy+xz+yz
$$

carry bit 在至少两个输入为 $1$ 时为 $1$。

---

# 多位二进制加法器

tags: 12.3 Logic Gates

hint:
怎样用半加器和全加器加两个多位二进制数？

content:
多位二进制加法器可以由一个半加器和若干全加器串联构成。

最低位相加不需要 carry-in，可用半加器。

更高位需要把低一位产生的 carry 作为输入，因此使用全加器。

每一级输出一个 sum bit，并把 carry 传递给下一位。

这种结构称为 ripple-carry adder。

---

# NAND 门的电路完备性

tags: 12.3 Logic Gates

hint:
只用 NAND gate 能否构造任意电路？

content:
NAND gate 对应 NAND 运算：

$$
x\mid y=\overline{xy}
$$

由于 NAND 单独就是函数完备的，所以只用 NAND gate 可以构造任意可由 AND、OR、NOT 构造的电路。

在实际数字电路设计中，NAND gate 因其完备性非常重要。

---

# NOR 门的电路完备性

tags: 12.3 Logic Gates

hint:
只用 NOR gate 能否构造任意电路？

content:
NOR gate 对应 NOR 运算：

$$
x\downarrow y=\overline{x+y}
$$

由于 NOR 单独就是函数完备的，所以只用 NOR gate 可以构造任意布尔电路。

这意味着所有逻辑功能都可以仅由一种类型的门实现。

---

# 电路最小化

tags: 12.4 Minimization of Circuits

hint:
为什么要化简布尔表达式？

content:
电路最小化的目标是用更少、更简单的布尔表达式表示同一个布尔函数。

通常希望得到：

1. 乘积项数量尽可能少；
2. 每个乘积项中的文字数量尽可能少；
3. 对应电路使用更少逻辑门；
4. 电路成本、延迟和复杂度更低。

最小化常从积之和展开出发进行化简。

---

# 最小化的目标形式

tags: 12.4 Minimization of Circuits

hint:
minimization 通常优化哪类表达式？

content:
在本章中，布尔函数最小化通常指：在所有表示该函数的积之和表达式中，找出乘积项尽可能少、且文字总数尽可能少的表达式。

目标形式为：

$$
F=P_1+P_2+\cdots+P_k
$$

其中每个 $P_i$ 是文字的布尔积。

先尽量减少 $k$，再尽量减少各 $P_i$ 中的文字数量。

---

# 相邻小项的合并

tags: 12.4 Minimization of Circuits

hint:
两个小项只差一个变量时，为什么可以合并？

content:
若两个乘积项只在一个变量上互补，其余文字完全相同，则可以合并。

例如：

$$
xy+x\overline{y}=x(y+\overline{y})=x
$$

原因是：

$$
y+\overline{y}=1
$$

合并会消去发生变化的那个变量，从而减少文字数。

---

# Karnaugh 图

tags: 12.4 Minimization of Circuits

hint:
K-map 用什么结构表示小项？

content:
Karnaugh 图简称 K-map，是用于化简布尔函数的图形工具。

$n$ 个变量的 K-map 有：

$$
2^n
$$

个单元格。

每个单元格对应一个小项。

若函数在该小项对应输入上取值为 $1$，就在对应单元格填入 $1$。

K-map 的布局使相邻单元格对应的小项只差一个文字。

---

# K-map 的相邻性

tags: 12.4 Minimization of Circuits

hint:
K-map 中为什么边界单元格也可能相邻？

content:
K-map 中相邻单元格表示对应小项只在一个变量上不同。

为了保持这种性质，K-map 的行列通常按 Gray code 排列。

在二维 K-map 中，最上行与最下行、最左列与最右列也被看作相邻。

这种环绕相邻性可以帮助发现更大的可合并区域。

---

# Gray code 在 K-map 中的作用

tags: 12.4 Minimization of Circuits

hint:
为什么 K-map 的行列不用普通二进制顺序？

content:
K-map 的行列使用 Gray code 排列，使相邻行或相邻列对应的 bit string 恰好只差一位。

这样，相邻单元格对应的小项只差一个变量。

例如，四变量 K-map 常用两位 Gray code 标记行列：

$$
11,\ 10,\ 00,\ 01
$$

相邻性包括边界环绕相邻。

---

# K-map 分组原则

tags: 12.4 Minimization of Circuits

hint:
K-map 中怎样把 $1$ 分组成可化简项？

content:
K-map 化简时，把值为 $1$ 的相邻单元格分成尽可能大的矩形块。

分组大小必须是 $2$ 的幂：

$$
1,\ 2,\ 4,\ 8,\ldots
$$

每个块对应一个乘积项。

块中保持不变的变量保留下来，发生变化的变量被消去。

应尽量使用大块，并覆盖所有为 $1$ 的单元格。

---

# K-map 中单元格覆盖

tags: 12.4 Minimization of Circuits

hint:
一个 $1$ 单元格是否可以被多个块覆盖？

content:
K-map 化简中，一个值为 $1$ 的单元格可以被多个块覆盖。

允许重叠有助于形成更大的块，从而减少表达式中的文字数。

但最终必须保证每个值为 $1$ 的单元格至少被一个块覆盖。

不需要覆盖值为 $0$ 的单元格，除非它是 don't care 条件。

---

# 二变量 K-map

tags: 12.4 Minimization of Circuits

hint:
两个变量的 K-map 有多少个单元格？

content:
二变量布尔函数的 K-map 有：

$$
2^2=4
$$

个单元格。

每个单元格对应一个小项：

$$
xy,\quad x\overline{y},\quad \overline{x}y,\quad \overline{x}\overline{y}
$$

相邻两个 $1$ 可合并成一个只含一个文字的项。

四个 $1$ 可合并成常量：

$$
1
$$

---

# 三变量 K-map

tags: 12.4 Minimization of Circuits

hint:
三个变量的 K-map 有多少个单元格，通常怎样排列？

content:
三变量布尔函数的 K-map 有：

$$
2^3=8
$$

个单元格。

通常排成：

$$
2\times4
$$

矩形。

一维用一个变量标记，另一维用两个变量的 Gray code 标记。

分组时要考虑左右边界相邻。

---

# 四变量 K-map

tags: 12.4 Minimization of Circuits

hint:
四变量的 K-map 通常是什么形状？

content:
四变量布尔函数的 K-map 有：

$$
2^4=16
$$

个单元格。

通常排成：

$$
4\times4
$$

矩形。

行和列都用两位 Gray code 标记。

分组时要考虑上下边界相邻和左右边界相邻。

---

# 多变量 K-map

tags: 12.4 Minimization of Circuits

hint:
$n$ 变量 K-map 的行数和列数可怎样设置？

content:
$n$ 变量 K-map 有：

$$
2^n
$$

个单元格。

可排成：

$$
2^{\lfloor n/2\rfloor}
$$

行和：

$$
2^{\lceil n/2\rceil}
$$

列。

行列用 Gray code 标记，使相邻单元格尽量对应只差一个变量的小项。

实际中，K-map 通常适合变量数较少的函数。

---

# K-map 的适用范围

tags: 12.4 Minimization of Circuits

hint:
为什么 K-map 不适合很多变量？

content:
K-map 对少量变量非常直观，常用于化简二到四变量的布尔函数。

对于五个或六个变量，K-map 仍可使用，但已经变得复杂。

变量更多时，单元格数量为：

$$
2^n
$$

增长很快，K-map 难以手工使用。

此时更适合使用算法化方法，如 Quine-McCluskey 方法或计算机辅助设计工具。

---

# 蕴含项

tags: 12.4 Minimization of Circuits

hint:
什么样的乘积项叫 implicant？

content:
布尔函数 $F$ 的蕴含项是一个文字乘积 $P$，满足：只要 $P$ 取值为 $1$，函数 $F$ 也取值为 $1$。

形式化地：

$$
P=1\to F=1
$$

蕴含项对应 K-map 中只覆盖 $1$ 单元格的一个可合并块。

---

# 主蕴含项

tags: 12.4 Minimization of Circuits

hint:
不能再删除文字仍保持蕴含性的项叫什么？

content:
布尔函数的主蕴含项是不能再通过删除文字来扩大而仍保持为蕴含项的乘积项。

也就是说，$P$ 是主蕴含项，当且仅当：

1. $P$ 是蕴含项；
2. 从 $P$ 中删除任意文字后得到的项都不再是蕴含项。

在 K-map 中，主蕴含项通常对应不能再扩大的最大合法分组。

---

# 本质主蕴含项

tags: 12.4 Minimization of Circuits

hint:
如果某个小项只能被一个主蕴含项覆盖，这个主蕴含项叫什么？

content:
若某个主蕴含项覆盖了至少一个没有被其他主蕴含项覆盖的小项，则称它是本质主蕴含项。

本质主蕴含项必须出现在任何最小化表达式中。

在化简时，应先选取所有本质主蕴含项，再考虑如何覆盖剩余小项。

---

# Don't care 条件

tags: 12.4 Minimization of Circuits

hint:
某些输入组合永远不会出现时，输出值可以怎样处理？

content:
Don't care 条件是指某些输入组合不可能出现，或其输出值无关紧要。

在化简布尔函数时，可以把 don't care 单元格当作 $0$，也可以当作 $1$。

选择方式取决于哪种处理能使表达式更简单。

Don't care 条件常用符号 $d$ 或 $X$ 标记。

---

# Quine-McCluskey 方法

tags: 12.4 Minimization of Circuits

hint:
有没有比 K-map 更适合算法实现的化简方法？

content:
Quine-McCluskey 方法是一种系统化、表格化的布尔函数最小化方法。

它适合计算机实现，也适合处理比 K-map 更复杂的函数。

基本思想是：

1. 把小项表示成 bit string；
2. 合并只差一位的小项；
3. 反复合并得到更短乘积项；
4. 找出主蕴含项；
5. 选择能覆盖全部原始小项的最小主蕴含项集合。

---

# 小项的 bit string 表示

tags: 12.4 Minimization of Circuits

hint:
Quine-McCluskey 方法中，变量和补变量如何编码？

content:
在 Quine-McCluskey 方法中，每个小项用长度为 $n$ 的 bit string 表示。

若小项中出现变量：

$$
x_i
$$

则第 $i$ 位写：

$$
1
$$

若小项中出现补变量：

$$
\overline{x_i}
$$

则第 $i$ 位写：

$$
0
$$

例如：

$$
x\overline{y}z
$$

可表示为：

$$
101
$$

---

# Quine-McCluskey 的分组步骤

tags: 12.4 Minimization of Circuits

hint:
为什么要按 bit string 中 $1$ 的个数分组？

content:
Quine-McCluskey 方法先按 bit string 中 $1$ 的个数对小项分组。

只有 $1$ 的个数相差 $1$ 的两个小项，才可能只差一个变量，从而可以合并。

因此分组可以减少比较次数，使合并过程更系统。

---

# 合并只差一位的小项

tags: 12.4 Minimization of Circuits

hint:
两个 bit string 只差一位时，怎样表示合并结果？

content:
若两个小项的 bit string 恰好只在一个位置不同，则对应小项可以合并。

合并后的字符串在相同位置保留原位，在不同位置写破折号：

$$
-
$$

破折号表示该变量被消去。

例如：

$$
101
$$

和：

$$
111
$$

可合并为：

$$
1-1
$$

对应乘积项中不再包含第二个变量。

---

# 反复合并与主蕴含项

tags: 12.4 Minimization of Circuits

hint:
Quine-McCluskey 方法为什么要多轮合并？

content:
合并后的项还可能继续与其他项合并。

若两个带破折号的字符串在破折号位置相同，并且在其他位置恰好只差一位，也可以继续合并。

反复合并直到不能再合并。

没有被进一步合并的项就是主蕴含项候选。

---

# 主蕴含项表

tags: 12.4 Minimization of Circuits

hint:
如何判断哪些主蕴含项必须选？

content:
Quine-McCluskey 方法用主蕴含项表记录覆盖关系。

表的列对应原始小项。

表的行对应主蕴含项。

若某个主蕴含项覆盖某个小项，就在对应位置标记。

若某列只有一个标记，则该列对应的小项只能由一个主蕴含项覆盖，这个主蕴含项是本质主蕴含项，必须选入化简表达式。

---

# 选择覆盖全部小项的主蕴含项

tags: 12.4 Minimization of Circuits

hint:
选完本质主蕴含项后，剩下的小项怎样处理？

content:
在选择所有本质主蕴含项后，可能仍有一些原始小项未被覆盖。

此时需要从剩余主蕴含项中选择若干项，覆盖所有未覆盖小项。

目标是让选出的乘积项数量尽量少，并且文字总数尽量少。

这一步可能需要比较多个候选覆盖方案。

---

# K-map 与 Quine-McCluskey 的关系

tags: 12.4 Minimization of Circuits

hint:
两种化简方法本质上都在寻找什么？

content:
K-map 和 Quine-McCluskey 方法本质上都在寻找能够覆盖函数取值为 $1$ 的小项的较大蕴含项。

K-map 用图形相邻块直观合并小项。

Quine-McCluskey 用 bit string 和表格系统合并小项。

两者都试图获得更少乘积项和更少文字的积之和表达式。

---

# 第十二章核心思想

tags: 12.4 Minimization of Circuits

hint:
Boolean Algebra 这一章的主线是什么？

content:
第十二章的核心是用布尔代数描述、实现和化简数字电路。

主要内容包括：

1. 布尔值、布尔和、布尔积与布尔补；
2. 布尔函数和布尔表达式；
3. 布尔恒等式与对偶原理；
4. 用小项构造积之和展开；
5. 函数完备性、NAND 和 NOR；
6. 用逻辑门实现布尔表达式；
7. 半加器、全加器等基础电路；
8. 用 K-map 进行图形化最小化；
9. 用 Quine-McCluskey 方法进行系统化最小化。

这一章把命题逻辑、布尔代数和实际电路设计连接起来。

---

# 计算模型

tags: 13.1 Languages and Grammars

hint:
第十三章为什么要同时研究文法、有限状态机和图灵机？

content:
计算模型用于形式化描述“什么可以被计算”和“怎样被计算”。

第十三章主要研究三类结构：

1. 文法：用于生成语言中的字符串；
2. 有限状态机：用有限个状态处理输入，适合建模有限记忆系统；
3. 图灵机：带有可读写磁带，是更一般的计算模型。

这些模型帮助我们研究语言识别、编译器、自动机、可计算性、可解性以及复杂度类别。

---

# 形式语言

tags: 13.1 Languages and Grammars

hint:
formal language 与自然语言有什么区别？

content:
形式语言是由精确定义的语法规则生成或识别的字符串集合。

与自然语言不同，形式语言只关注字符串是否符合规则，通常不关心语义。

若字母表为 $V$，则一个形式语言通常是：

$$
V^*
$$

的某个子集。

形式语言可用于描述编程语言、数据格式、协议、模式匹配和自动机可识别的字符串集合。

---

# 字母表与字符串

tags: 13.1 Languages and Grammars

hint:
一个语言中的基本符号和有限符号序列分别叫什么？

content:
字母表是一个有限非空符号集合，通常记为：

$$
V
$$

由字母表中符号组成的有限序列称为字符串。

空字符串记为：

$$
\lambda
$$

所有由 $V$ 中符号组成的字符串集合记为：

$$
V^*
$$

其中包括空字符串。

---

# 字符串连接

tags: 13.1 Languages and Grammars

hint:
两个字符串首尾相接得到什么操作？

content:
若 $x$ 与 $y$ 是字符串，则它们的连接记为：

$$
xy
$$

连接操作把 $y$ 接在 $x$ 的后面。

空字符串是连接运算的单位元：

$$
\lambda x=x
$$

$$
x\lambda=x
$$

字符串连接通常不满足交换律，即一般：

$$
xy\ne yx
$$

---

# 字符串长度

tags: 13.1 Languages and Grammars

hint:
字符串长度怎样记号？空字符串长度是多少？

content:
字符串 $x$ 的长度是其中符号的个数，常记为：

$$
l(x)
$$

空字符串长度为：

$$
l(\lambda)=0
$$

若 $x$ 与 $y$ 是字符串，则：

$$
l(xy)=l(x)+l(y)
$$

字符串长度常用于定义语言、自动机输入和计算复杂度。

---

# 文法

tags: 13.1 Languages and Grammars

hint:
grammar 怎样用有限规则生成语言？

content:
文法是用于生成字符串的有限规则系统。

一个短语结构文法通常记为：

$$
G=(V,T,S,P)
$$

其中：

$V$ 是词汇表，即所有符号集合；

$T\subseteq V$ 是终结符集合；

$S\in V-T$ 是起始符号；

$P$ 是产生式集合。

文法从起始符号 $S$ 出发，反复应用产生式，生成只含终结符的字符串。

---

# 终结符与非终结符

tags: 13.1 Languages and Grammars

hint:
文法中哪些符号最终出现在语言字符串里，哪些符号只是生成过程的中间符号？

content:
终结符是最终生成的语言字符串中允许出现的符号，集合记为：

$$
T
$$

非终结符是生成过程中的辅助符号，属于：

$$
V-T
$$

起始符号 $S$ 必须是非终结符：

$$
S\in V-T
$$

最终属于语言的字符串必须只由终结符组成。

---

# 产生式

tags: 13.1 Languages and Grammars

hint:
文法规则通常写成什么形式？

content:
产生式是文法中的替换规则，通常写作：

$$
w_0\to w_1
$$

其中 $w_0$ 和 $w_1$ 是由词汇表符号组成的字符串，并且 $w_0$ 至少包含一个非终结符。

产生式表示：在推导过程中，可以把某处出现的 $w_0$ 替换为 $w_1$。

---

# 直接推导

tags: 13.1 Languages and Grammars

hint:
一次应用产生式得到新字符串，怎样记号？

content:
设文法中有产生式：

$$
w_0\to w_1
$$

若字符串 $z$ 可以写为：

$$
z=xw_0y
$$

则可把其中的 $w_0$ 替换为 $w_1$，得到：

$$
xw_1y
$$

这称为一步直接推导，记为：

$$
xw_0y\Rightarrow xw_1y
$$

---

# 多步推导

tags: 13.1 Languages and Grammars

hint:
反复应用产生式得到字符串，怎样表示？

content:
若字符串 $w$ 可通过有限次产生式应用推导出字符串 $z$，则记为：

$$
w\Rightarrow^* z
$$

其中：

$$
\Rightarrow^*
$$

表示零步或多步推导。

零步推导允许：

$$
w\Rightarrow^* w
$$

---

# 文法生成的语言

tags: 13.1 Languages and Grammars

hint:
一个文法最终生成的语言是什么集合？

content:
文法：

$$
G=(V,T,S,P)
$$

生成的语言记为：

$$
L(G)
$$

定义为所有能从起始符号 $S$ 推导出的终结符字符串集合：

$$
L(G)=\{w\in T^*\mid S\Rightarrow^* w\}
$$

也就是说，$L(G)$ 中的字符串必须只含终结符，并且能由文法规则生成。

---

# 语言生成与语言识别

tags: 13.1 Languages and Grammars

hint:
文法和自动机分别从什么角度描述语言？

content:
文法通常从生成角度描述语言：给出规则，说明哪些字符串可以被生成。

自动机通常从识别角度描述语言：给定一个输入字符串，判断它是否属于语言。

对同一个语言，可以研究：

1. 是否存在某类文法生成它；
2. 是否存在某类自动机识别它；
3. 生成能力和识别能力之间的对应关系。

---

# 短语结构文法

tags: 13.1 Languages and Grammars

hint:
最一般的文法类型叫什么？

content:
短语结构文法是最一般的文法形式，通常也称 type 0 grammar。

它的产生式只要求左侧至少包含一个非终结符。

短语结构文法生成的语言也称为 phrase-structure language。

在计算模型中，短语结构文法与图灵机识别能力相对应。

---

# Chomsky 层级

tags: 13.1 Languages and Grammars

hint:
文法按产生式限制可以分成哪些类型？

content:
Chomsky 层级把文法按产生式限制分为四类：

type 0：短语结构文法；

type 1：上下文有关文法；

type 2：上下文无关文法；

type 3：正则文法。

它们的生成能力满足包含关系：

$$
\text{type 3}\subseteq \text{type 2}\subseteq \text{type 1}\subseteq \text{type 0}
$$

限制越强，语言类越小，但越容易分析和识别。

---

# Type 1 文法

tags: 13.1 Languages and Grammars

hint:
context-sensitive grammar 对产生式长度有什么限制？

content:
Type 1 文法也称上下文有关文法。

它的产生式通常要求右侧长度不小于左侧长度。

若产生式为：

$$
w_0\to w_1
$$

则要求：

$$
l(w_1)\ge l(w_0)
$$

并且左侧必须至少包含一个非终结符。

上下文有关文法生成上下文有关语言。

---

# Type 2 文法

tags: 13.1 Languages and Grammars

hint:
context-free grammar 的产生式左侧有什么特殊限制？

content:
Type 2 文法也称上下文无关文法。

它的每条产生式左侧必须是单个非终结符。

形式为：

$$
A\to w
$$

其中 $A$ 是非终结符，$w$ 是由词汇表符号组成的字符串。

上下文无关文法广泛用于描述编程语言语法和表达式结构。

---

# Type 3 文法

tags: 13.1 Languages and Grammars

hint:
regular grammar 的产生式通常有什么形式？

content:
Type 3 文法也称正则文法。

右线性正则文法的产生式通常形如：

$$
A\to aB
$$

或：

$$
A\to a
$$

其中 $A,B$ 是非终结符，$a$ 是终结符。

在适当约定下，也可允许产生空字符串的规则。

正则文法生成的语言称为正则语言。

---

# 正则语言

tags: 13.1 Languages and Grammars

hint:
regular language 可以由哪类文法生成？

content:
正则语言是由正则文法生成的语言。

等价地，正则语言也可以由有限状态自动机识别，并可由正则表达式表示。

正则语言是形式语言中最基本的一类，适合描述只需要有限记忆即可识别的字符串模式。

---

# 上下文无关语言

tags: 13.1 Languages and Grammars

hint:
context-free language 由什么文法生成？

content:
上下文无关语言是由上下文无关文法生成的语言。

上下文无关文法的产生式左侧是单个非终结符：

$$
A\to w
$$

这种语言比正则语言更强，可描述嵌套结构，例如括号匹配和表达式语法。

---

# 派生树

tags: 13.1 Languages and Grammars

hint:
上下文无关文法的推导过程怎样用树表示？

content:
派生树用于表示上下文无关文法的一次推导。

根节点是起始符号。

内部节点是非终结符。

叶节点从左到右连接起来，形成被生成的字符串。

若某个节点标记为 $A$，并使用产生式：

$$
A\to x_1x_2\cdots x_k
$$

则该节点有 $k$ 个子节点，分别标记为：

$$
x_1,x_2,\ldots,x_k
$$

---

# 歧义文法

tags: 13.1 Languages and Grammars

hint:
一个字符串若有两棵不同派生树，文法具有什么问题？

content:
上下文无关文法称为歧义的，如果存在某个字符串，它可以由该文法生成，并且具有两棵不同的派生树。

歧义文法会导致语法结构不唯一。

在编程语言设计中，歧义可能使表达式解析不明确，因此通常需要通过优先级规则、结合性规则或改写文法来消除歧义。

---

# Backus-Naur Form

tags: 13.1 Languages and Grammars

hint:
BNF 用来描述哪类文法的语法规则？

content:
Backus-Naur Form 简称 BNF，是描述上下文无关文法的一种记号。

BNF 常用于规定编程语言语法。

典型写法为：

```text
<nonterminal> ::= expression
```

其中左侧是非终结符，右侧给出可替换的形式。

多个可选右侧常用竖线分隔：

```text
<A> ::= option1 | option2 | option3
```

---

# BNF 与编程语言语法

tags: 13.1 Languages and Grammars

hint:
为什么 BNF 对编译器很重要？

content:
BNF 可以精确描述编程语言中哪些字符串是合法程序或合法表达式。

编译器的词法分析和语法分析依赖这种明确语法。

例如，可以用 BNF 定义标识符、表达式、语句、函数声明和程序结构。

精确语法规则使计算机能够判断输入程序是否符合语言规范。

---

# 有限状态机

tags: 13.2 Finite-State Machines with Output

hint:
finite-state machine 由哪些组成部分构成？

content:
带输出的有限状态机可写为：

$$
M=(S,I,O,f,g,s_0)
$$

其中：

$S$ 是有限状态集合；

$I$ 是输入字母表；

$O$ 是输出字母表；

$f:S\times I\to S$ 是转移函数；

$g:S\times I\to O$ 是输出函数；

$s_0$ 是初始状态。

每读入一个输入符号，机器根据当前状态和输入符号转移到新状态，并产生一个输出符号。

---

# 状态

tags: 13.2 Finite-State Machines with Output

hint:
有限状态机中的状态起什么作用？

content:
状态表示机器当前记住的信息。

有限状态机只能处在有限多个状态之一。

由于状态数有限，有限状态机只有有限记忆能力。

它不能记住任意长的历史信息，只能记住被编码进状态集合的有限信息。

---

# 输入字母表与输出字母表

tags: 13.2 Finite-State Machines with Output

hint:
FSM 每一步读取和产生的符号分别来自哪里？

content:
输入字母表 $I$ 是机器可能读取的输入符号集合。

输出字母表 $O$ 是机器可能产生的输出符号集合。

对每个当前状态：

$$
s\in S
$$

和输入符号：

$$
x\in I
$$

机器通过转移函数决定下一个状态，并通过输出函数决定输出符号。

---

# 转移函数

tags: 13.2 Finite-State Machines with Output

hint:
转移函数根据什么决定下一状态？

content:
有限状态机的转移函数为：

$$
f:S\times I\to S
$$

它把当前状态和输入符号映射到下一状态。

若机器处于状态 $s$，读入符号 $x$，则下一状态为：

$$
f(s,x)
$$

转移函数描述了机器如何随着输入变化而更新状态。

---

# 输出函数

tags: 13.2 Finite-State Machines with Output

hint:
带输出的 FSM 每一步怎样产生输出？

content:
带输出的有限状态机的输出函数为：

$$
g:S\times I\to O
$$

若机器处于状态 $s$，读入符号 $x$，则输出为：

$$
g(s,x)
$$

这种输出依赖于当前状态和当前输入，因此是 Mealy 型有限状态机的形式。

---

# 状态表

tags: 13.2 Finite-State Machines with Output

hint:
如何用表格表示 FSM 的转移函数和输出函数？

content:
状态表用行表示当前状态，用列表示输入符号。

对每个状态和输入组合，表中列出：

1. 下一状态；
2. 对应输出。

状态表完整记录了有限状态机的转移函数 $f$ 和输出函数 $g$。

对于复杂机器，状态表比文字描述更清晰。

---

# 状态图

tags: 13.2 Finite-State Machines with Output

hint:
FSM 怎样用带标签的有向图表示？

content:
状态图用顶点表示状态。

若在状态 $s$ 输入 $x$ 时转移到状态 $t$ 并输出 $y$，则画一条从 $s$ 到 $t$ 的有向边，并标记为：

$$
x,y
$$

即：

$$
s\xrightarrow{x,y}t
$$

初始状态通常用特殊箭头标出。

---

# 输入字符串产生输出字符串

tags: 13.2 Finite-State Machines with Output

hint:
FSM 读入一串符号时，如何产生一串输出？

content:
设输入字符串为：

$$
x=x_1x_2\cdots x_k
$$

机器从初始状态 $s_0$ 开始，依次读入每个符号。

若第 $i$ 步前状态为 $s_{i-1}$，则：

$$
s_i=f(s_{i-1},x_i)
$$

该步输出：

$$
y_i=g(s_{i-1},x_i)
$$

最终输出字符串为：

$$
y_1y_2\cdots y_k
$$

---

# FSM 的有限记忆

tags: 13.2 Finite-State Machines with Output

hint:
有限状态机为什么只能记住有限信息？

content:
有限状态机的所有历史信息都必须压缩到当前状态中。

由于状态集合 $S$ 是有限的，机器只能区分有限多种历史情形。

因此，有限状态机适合处理只需要有限记忆的任务，如自动售货机、简单控制器、延迟器、加法器和正则语言识别。

它不适合处理需要任意大计数或无限记忆的任务。

---

# 自动售货机模型

tags: 13.2 Finite-State Machines with Output

hint:
自动售货机的状态可以表示什么？

content:
自动售货机可用有限状态机建模。

状态可以表示当前已经投入的金额。

输入符号可以是投币、选择饮料或退币请求。

输出符号可以是无输出、找零、释放商品或退回金额。

机器根据当前金额和输入动作转移到新状态，并产生相应输出。

---

# 延迟机器

tags: 13.2 Finite-State Machines with Output

hint:
把输入延迟一个单位输出时，状态需要记住什么？

content:
单位延迟机器读入输入字符串：

$$
x_1x_2\cdots x_k
$$

并输出：

$$
0x_1x_2\cdots x_{k-1}
$$

为了做到这一点，机器需要记住前一个输入符号。

因此可使用状态记录“上一次输入是 $0$”或“上一次输入是 $1$”。

初始输出通常设为 $0$。

---

# 二进制加法有限状态机

tags: 13.2 Finite-State Machines with Output

hint:
逐位加两个二进制数时，FSM 状态需要记住什么？

content:
用有限状态机逐位相加两个二进制数时，状态只需要记住前一位产生的进位。

因此可使用两个状态：

1. 当前进位为 $0$；
2. 当前进位为 $1$。

每一步输入是一对二进制位，输出是当前和位，并根据是否产生新进位转移状态。

这说明有限状态机可以完成某些有限记忆的算术任务。

---

# 有限状态自动机

tags: 13.3 Finite-State Machines with No Output

hint:
没有输出但有 final states 的机器叫什么？

content:
有限状态自动机是不带输出、但带有终止状态集合的有限状态机。

确定型有限状态自动机可写为：

$$
M=(S,I,f,s_0,F)
$$

其中：

$S$ 是有限状态集合；

$I$ 是输入字母表；

$f:S\times I\to S$ 是转移函数；

$s_0$ 是初始状态；

$F\subseteq S$ 是终止状态集合。

---

# 终止状态

tags: 13.3 Finite-State Machines with No Output

hint:
有限状态自动机怎样决定接受或拒绝输入？

content:
有限状态自动机读完整个输入字符串后，只看最终停留的状态。

若最终状态属于终止状态集合：

$$
F
$$

则输入字符串被接受或识别。

若最终状态不属于 $F$，则输入字符串不被接受。

终止状态通常在状态图中用双圈表示。

---

# 扩展转移函数

tags: 13.3 Finite-State Machines with No Output

hint:
转移函数原本只读一个符号，怎样扩展到整个字符串？

content:
确定型有限状态自动机的转移函数原本定义为：

$$
f:S\times I\to S
$$

可递归扩展到字符串：

$$
f:S\times I^*\to S
$$

定义为：

$$
f(s,\lambda)=s
$$

若 $x\in I^*$ 且 $a\in I$，则：

$$
f(s,xa)=f(f(s,x),a)
$$

扩展转移函数表示从状态 $s$ 读完整个字符串后到达的状态。

---

# 语言识别

tags: 13.3 Finite-State Machines with No Output

hint:
一个自动机识别的语言由哪些字符串组成？

content:
设有限状态自动机为：

$$
M=(S,I,f,s_0,F)
$$

若字符串 $x\in I^*$ 满足：

$$
f(s_0,x)\in F
$$

则称 $x$ 被 $M$ 识别或接受。

$M$ 识别的语言记为：

$$
L(M)
$$

定义为：

$$
L(M)=\{x\in I^*\mid f(s_0,x)\in F\}
$$

---

# 等价有限状态自动机

tags: 13.3 Finite-State Machines with No Output

hint:
两个自动机何时等价？

content:
若两个有限状态自动机识别同一个语言，则称它们等价。

也就是说，若：

$$
L(M_1)=L(M_2)
$$

则 $M_1$ 与 $M_2$ 等价。

等价自动机可能状态数不同。

自动机最小化的目标是在等价自动机中找到状态数尽可能少的自动机。

---

# DFA 设计中的状态记忆

tags: 13.3 Finite-State Machines with No Output

hint:
设计自动机时，状态通常用来记录什么？

content:
设计确定型有限状态自动机时，状态应记录判断语言成员资格所需的有限信息。

例如：

1. 是否已经看到某个模式；
2. 当前字符串末尾是否匹配某个前缀；
3. 已读入的 $1$ 的奇偶性；
4. 已读入的 $0$ 的数量是否达到某个阈值；
5. 当前后缀属于哪一类。

状态越精确，自动机越容易正确；状态越冗余，自动机可能越不简洁。

---

# 识别以某模式结尾的字符串

tags: 13.3 Finite-State Machines with No Output

hint:
为什么需要记住当前后缀？

content:
要识别以某个固定模式结尾的字符串，自动机只需记住当前输入后缀与目标模式前缀的最大匹配长度。

例如识别以 $00$ 结尾的 bit string，可用状态记录当前末尾：

1. 没有连续末尾 $0$；
2. 恰好以一个 $0$ 结尾；
3. 已经以至少两个连续 $0$ 结尾。

最终状态是第三类状态。

---

# 识别奇偶性

tags: 13.3 Finite-State Machines with No Output

hint:
识别 1 的个数为奇数，需要几个核心状态？

content:
要识别含有奇数个 $1$ 的 bit string，只需记录当前已读入的 $1$ 的个数奇偶性。

可使用两个状态：

1. 偶数个 $1$；
2. 奇数个 $1$。

读入 $1$ 时在两个状态之间切换。

读入 $0$ 时状态不变。

若目标是奇数个 $1$，则“奇数状态”为终止状态。

---

# 自动机状态乘积构造

tags: 13.3 Finite-State Machines with No Output

hint:
同时记录两个性质时，状态数通常怎样组合？

content:
若自动机需要同时记录两个有限性质，可以把状态设计为有序对。

例如，一个性质有 $m$ 种状态，另一个性质有 $n$ 种状态，则组合状态最多有：

$$
mn
$$

种。

这称为状态乘积思想。

它常用于识别同时满足两个条件的语言，例如“$1$ 的个数为奇数且以两个 $0$ 结尾”。

---

# 机器最小化

tags: 13.3 Finite-State Machines with No Output

hint:
为什么要减少等价自动机的状态数？

content:
某些构造方法得到的有限状态自动机可能包含冗余状态。

机器最小化的目标是构造一个等价自动机，使状态数最少。

在硬件和软件实现中，较少状态通常意味着更低成本、更少存储、更快处理和更简单实现。

最小化前通常先删除从初始状态不可达的状态。

---

# 可区分状态

tags: 13.3 Finite-State Machines with No Output

hint:
两个状态在什么意义下不能合并？

content:
在一个 DFA 中，两个状态 $s$ 和 $t$ 可区分，如果存在某个输入字符串 $x$，使得从 $s$ 和 $t$ 出发读入 $x$ 后，一个到达终止状态，另一个不到达终止状态。

形式化地：

$$
f(s,x)\in F
$$

与：

$$
f(t,x)\in F
$$

的真假不同。

可区分状态不能合并。

---

# 不可区分状态

tags: 13.3 Finite-State Machines with No Output

hint:
两个状态对所有后续输入都表现一样时，能怎样处理？

content:
两个状态 $s$ 和 $t$ 不可区分，若对每个输入字符串 $x$，从 $s$ 和 $t$ 出发读入 $x$ 后，要么都到达终止状态，要么都不到达终止状态。

不可区分状态在语言识别上行为相同，因此可合并为一个状态。

自动机最小化就是把不可区分状态归为同一等价类。

---

# 非确定型有限状态自动机

tags: 13.3 Finite-State Machines with No Output

hint:
NFA 与 DFA 的转移函数最大区别是什么？

content:
非确定型有限状态自动机可写为：

$$
M=(S,I,f,s_0,F)
$$

其中转移函数为：

$$
f:S\times I\to \mathcal{P}(S)
$$

也就是说，对于当前状态和输入符号，下一状态可以是一个状态集合。

这与 DFA 中下一状态唯一不同。

---

# NFA 的接受条件

tags: 13.3 Finite-State Machines with No Output

hint:
NFA 何时接受一个字符串？

content:
NFA 读入字符串时，可能沿多条路径同时演化。

若存在至少一条可能路径，使得读完整个输入后到达某个终止状态，则该字符串被接受。

因此 NFA 的接受条件是“存在一条接受路径”，而不是所有路径都接受。

---

# NFA 与 DFA 的等价性

tags: 13.3 Finite-State Machines with No Output

hint:
非确定性是否让有限状态自动机识别更多语言？

content:
NFA 与 DFA 识别的语言类相同。

也就是说，任意 NFA 都存在一个等价的 DFA，识别同一个语言。

非确定性可以让自动机描述更简洁，但不会扩大有限状态自动机可识别语言的范围。

---

# 子集构造法

tags: 13.3 Finite-State Machines with No Output

hint:
如何把 NFA 转换成 DFA？

content:
子集构造法把 NFA 的状态集合的子集作为 DFA 的状态。

若 NFA 的状态集合为 $S$，则构造出的 DFA 状态属于：

$$
\mathcal{P}(S)
$$

初始状态为：

$$
\{s_0\}
$$

若当前 DFA 状态为 $A\subseteq S$，输入为 $a$，则下一状态为：

$$
\bigcup_{s\in A}f(s,a)
$$

凡是与 $F$ 有交集的子集都作为 DFA 的终止状态。

---

# 正则表达式

tags: 13.4 Language Recognition

hint:
regular expression 如何递归定义？

content:
给定符号集合 $I$，正则表达式递归定义如下：

$$
\varnothing
$$

是正则表达式；

$$
\lambda
$$

是正则表达式；

若：

$$
x\in I
$$

则 $x$ 是正则表达式；

若 $A$ 和 $B$ 是正则表达式，则：

$$
AB
$$

$$
A\cup B
$$

$$
A^*
$$

也是正则表达式。

---

# 正则表达式表示的集合

tags: 13.4 Language Recognition

hint:
正则表达式的每种构造分别表示什么语言操作？

content:
正则表达式表示字符串集合。

基本情形：

$$
\varnothing
$$

表示空集；

$$
\lambda
$$

表示集合：

$$
\{\lambda\}
$$

符号 $x$ 表示集合：

$$
\{x\}
$$

复合情形：

$$
AB
$$

表示连接；

$$
A\cup B
$$

表示并；

$$
A^*
$$

表示 Kleene 闭包。

---

# 语言连接

tags: 13.4 Language Recognition

hint:
两个字符串集合的连接怎样定义？

content:
若 $A$ 和 $B$ 是字符串集合，则它们的连接定义为：

$$
AB=\{xy\mid x\in A,\ y\in B\}
$$

也就是说，从 $A$ 中取一个字符串，从 $B$ 中取一个字符串，把它们连接起来。

语言连接通常不满足交换律，即一般：

$$
AB\ne BA
$$

---

# Kleene 闭包

tags: 13.4 Language Recognition

hint:
一个字符串集合可以重复连接任意多次，得到什么？

content:
设 $A$ 是字符串集合。

定义：

$$
A^0=\{\lambda\}
$$

并且：

$$
A^{n+1}=A^nA
$$

Kleene 闭包定义为：

$$
A^*=\bigcup_{n=0}^{\infty}A^n
$$

它表示由 $A$ 中字符串连接零次或多次得到的所有字符串集合。

---

# 正则集

tags: 13.4 Language Recognition

hint:
regular set 是由哪些基础集合通过哪些操作构成的？

content:
正则集是能由正则表达式表示的字符串集合。

它从以下基本集合开始：

$$
\varnothing
$$

$$
\{\lambda\}
$$

$$
\{x\}
$$

其中 $x$ 是输入字母表中的符号。

然后反复使用三种操作：

1. 并；
2. 连接；
3. Kleene 闭包。

所有能这样构造出的集合都是正则集。

---

# Kleene 定理

tags: 13.4 Language Recognition

hint:
有限状态自动机能识别的语言和正则表达式表示的语言有什么关系？

content:
Kleene 定理说明：一个语言能被有限状态自动机识别，当且仅当它是正则集。

也就是说，以下两个条件等价：

1. 存在有限状态自动机 $M$，使得语言为 $L(M)$；
2. 存在正则表达式表示该语言。

因此，有限状态自动机与正则表达式具有相同表达能力。

---

# 正则文法与正则集

tags: 13.4 Language Recognition

hint:
正则文法生成的语言与正则表达式有什么关系？

content:
一个语言由正则文法生成，当且仅当它是正则集。

因此，以下三种描述方式等价：

1. 正则文法；
2. 正则表达式；
3. 有限状态自动机。

它们都刻画同一类语言：正则语言。

---

# 正则语言的封闭性

tags: 13.4 Language Recognition

hint:
正则语言对哪些操作封闭？

content:
正则语言对以下操作封闭：

1. 并；
2. 连接；
3. Kleene 闭包。

由 Kleene 定理和自动机构造，还可证明正则语言对补、交和差等操作也封闭。

封闭性使我们能从简单正则语言构造复杂正则语言。

---

# 非正则语言

tags: 13.4 Language Recognition

hint:
有限状态机为什么不能识别某些需要无限记忆的语言？

content:
有些语言不能被任何有限状态自动机识别，因此不是正则语言。

典型例子是：

$$
\{0^n1^n\mid n\ge0\}
$$

识别该语言需要记住前面 $0$ 的个数，并与后面 $1$ 的个数相等比较。

有限状态机只有有限个状态，无法记住任意大的计数，因此不能识别该语言。

---

# 有限状态机的记忆限制

tags: 13.4 Language Recognition

hint:
为什么 FSA 适合模式识别，却不适合任意计数匹配？

content:
有限状态机的全部记忆都存放在有限个状态中。

因此它能识别有限模式、固定后缀、奇偶性、模数计数等只需有限信息的问题。

但它不能识别需要无界计数或嵌套匹配的语言，例如：

$$
\{0^n1^n\mid n\ge0\}
$$

这类语言需要比有限状态更强的记忆机制。

---

# Pushdown 自动机

tags: 13.4 Language Recognition

hint:
在有限状态机上加一个栈，会得到什么模型？

content:
Pushdown 自动机是在有限状态机基础上增加一个栈的计算模型。

栈提供后进先出的无限记忆能力。

每一步可以根据当前状态、输入符号和栈顶符号进行转移，并可对栈进行压入或弹出操作。

Pushdown 自动机可以识别上下文无关语言。

---

# Pushdown 自动机与上下文无关语言

tags: 13.4 Language Recognition

hint:
PDA 识别的语言对应哪类文法？

content:
一个语言能被 pushdown 自动机识别，当且仅当它能由上下文无关文法生成。

因此：

$$
\text{PDA languages}=\text{context-free languages}
$$

PDA 的栈使它能识别许多有限状态机不能识别的嵌套或配对结构，例如括号匹配和：

$$
\{0^n1^n\mid n\ge0\}
$$

---

# 线性有界自动机

tags: 13.4 Language Recognition

hint:
linear bounded automaton 比 PDA 更强在哪里？

content:
线性有界自动机是一种受限图灵机，其可用磁带空间被输入长度的线性函数限制。

它比 pushdown 自动机更强。

线性有界自动机可以识别上下文有关语言。

例如，它能识别一些 PDA 无法识别的语言，如：

$$
\{0^n1^n2^n\mid n\ge0\}
$$

---

# 自动机层级

tags: 13.4 Language Recognition

hint:
不同自动机模型与语言层级怎样对应？

content:
不同自动机模型对应不同语言类：

有限状态自动机识别正则语言；

Pushdown 自动机识别上下文无关语言；

线性有界自动机识别上下文有关语言；

图灵机识别短语结构文法生成的语言。

这些模型的识别能力逐步增强。

---

# 图灵机

tags: 13.5 Turing Machines

hint:
图灵机相比有限状态机多了什么？

content:
图灵机是在有限控制器基础上增加一条可读写磁带的计算模型。

磁带由单元格组成，向左右无限延伸。

控制器每一步处于有限状态之一，并根据当前状态和当前磁带符号决定：

1. 写入什么符号；
2. 移动到哪个新状态；
3. 磁头向左或向右移动。

图灵机比有限状态机强，因为它具有可读写的无界记忆。

---

# 图灵机的形式定义

tags: 13.5 Turing Machines

hint:
图灵机由哪些数学对象组成？

content:
图灵机可写为：

$$
T=(S,I,f,s_0)
$$

其中：

$S$ 是有限状态集合；

$I$ 是字母表，并包含空白符号 $B$；

$f$ 是部分转移函数；

$s_0$ 是初始状态。

转移函数为：

$$
f:S\times I\to S\times I\times\{R,L\}
$$

但它是部分函数，不一定对所有状态和符号组合都有定义。

---

# 图灵机的五元组

tags: 13.5 Turing Machines

hint:
一条转移规则怎样写成五元组？

content:
若图灵机在状态 $s$ 读到符号 $x$ 时，应进入状态 $s'$，写入符号 $x'$，并向方向 $d$ 移动，则写成五元组：

$$
(s,x,s',x',d)
$$

其中：

$$
d\in\{R,L\}
$$

$R$ 表示向右移动一格，$L$ 表示向左移动一格。

---

# 图灵机一步操作

tags: 13.5 Turing Machines

hint:
图灵机每一步做哪三件事？

content:
若当前状态为 $s$，当前磁带符号为 $x$，并且：

$$
f(s,x)=(s',x',d)
$$

则图灵机执行三件事：

1. 进入新状态 $s'$；
2. 用 $x'$ 替换当前单元格中的 $x$；
3. 若 $d=R$ 则磁头右移一格，若 $d=L$ 则磁头左移一格。

若 $f(s,x)$ 未定义，则机器停机。

---

# 初始位置

tags: 13.5 Turing Machines

hint:
图灵机运行前，输入字符串怎样放在磁带上？

content:
给定输入字符串时，通常把输入符号连续写在磁带的相邻单元格中。

输入之外的所有单元格都填空白符号：

$$
B
$$

机器从初始状态：

$$
s_0
$$

开始，磁头位于输入字符串的最左符号处。

若输入为空，磁头位于某个空白单元格上。

---

# 停机

tags: 13.5 Turing Machines

hint:
图灵机什么时候停止运行？

content:
图灵机在当前状态和当前磁带符号没有对应转移规则时停机。

也就是说，若：

$$
f(s,x)
$$

未定义，则机器停止。

图灵机可能在有限步后停机，也可能永远不停机。

是否停机是可计算性理论中的核心问题之一。

---

# 图灵机的终止状态

tags: 13.5 Turing Machines

hint:
在教材定义中，final state 怎样由五元组描述确定？

content:
图灵机的终止状态是没有作为任何五元组第一状态出现的状态。

若状态 $s$ 不出现在任何转移规则：

$$
(s,x,s',x',d)
$$

的第一个位置，则 $s$ 是终止状态。

当图灵机停在终止状态时，可用于表示输入被识别或接受。

---

# 图灵机识别字符串

tags: 13.5 Turing Machines

hint:
图灵机怎样判断一个字符串属于某个集合？

content:
设 $V$ 是字母表 $I$ 的子集。

图灵机 $T$ 识别字符串 $x\in V^*$，当且仅当：把 $x$ 写在磁带上并从初始位置开始运行时，$T$ 最终停在一个终止状态。

若某集合 $A\subseteq V^*$ 中的字符串恰好都被 $T$ 识别，则称 $T$ 识别集合 $A$。

---

# 图灵机识别的语言

tags: 13.5 Turing Machines

hint:
哪些语言可以被图灵机识别？

content:
图灵机能够识别的语言正好对应短语结构文法生成的语言。

也就是说，一个集合能被图灵机识别，当且仅当它能由 type 0 文法生成。

图灵机的识别能力强于有限状态自动机、pushdown 自动机和线性有界自动机。

---

# 识别 $\{0^n1^n\}$ 的图灵机思想

tags: 13.5 Turing Machines

hint:
图灵机如何用磁带标记配对 $0$ 和 $1$？

content:
要识别语言：

$$
\{0^n1^n\mid n\ge1\}
$$

图灵机可以反复执行：

1. 找到最左边尚未标记的 $0$，把它改成标记符号；
2. 向右找到最右边尚未标记的 $1$，把它改成标记符号；
3. 回到左侧继续；
4. 若所有 $0$ 与 $1$ 都能一一配对，并且顺序正确，则接受。

这种策略利用磁带作为无界记忆，有限状态机做不到这一点。

---

# 图灵机计算函数

tags: 13.5 Turing Machines

hint:
图灵机如何看作计算字符串函数的机器？

content:
若图灵机 $T$ 在输入字符串 $x$ 上运行并停机，且最终磁带上留下字符串 $y$，则可定义：

$$
T(x)=y
$$

若 $T$ 在输入 $x$ 上不停机，则 $T(x)$ 未定义。

因此，图灵机可以看作计算字符串上的偏函数。

---

# 偏函数

tags: 13.5 Turing Machines

hint:
为什么图灵机计算的函数可能不是对所有输入都有定义？

content:
偏函数是只在定义域的一部分输入上有值的函数。

图灵机可能对某些输入停机并给出输出，对另一些输入永远运行不停止。

因此，图灵机自然计算偏函数。

图灵机在其停机输入集合上有定义，在不停机输入上未定义。

---

# 非负整数的一元表示

tags: 13.5 Turing Machines

hint:
教材中怎样把非负整数写成图灵机磁带上的字符串？

content:
为了让图灵机计算数论函数，可以用一元表示非负整数。

非负整数 $n$ 表示为：

$$
n+1
$$

个连续的 $1$。

因此：

$$
0
$$

表示为：

$$
1
$$

而：

$$
5
$$

表示为：

$$
111111
$$

---

# 多元组的一元编码

tags: 13.5 Turing Machines

hint:
如何把 $(n_1,n_2,\ldots,n_k)$ 编码为磁带字符串？

content:
非负整数 $k$ 元组：

$$
(n_1,n_2,\ldots,n_k)
$$

可编码为：

$$
1^{n_1+1}*1^{n_2+1}*\cdots *1^{n_k+1}
$$

其中星号用作分隔符。

例如：

$$
(2,0,1,3)
$$

可表示为：

$$
111*1*11*1111
$$

这种编码使图灵机能够处理数论函数。

---

# 图灵可计算函数

tags: 13.5 Turing Machines

hint:
什么函数称为 computable？

content:
若存在图灵机可以计算某个函数，则称该函数是可计算的。

若不存在任何图灵机能计算该函数，则称该函数是不可计算的。

可计算性研究哪些函数和问题能够由机械过程完成。

图灵机给出了“算法可计算”的形式模型。

---

# Church-Turing 论题

tags: 13.5 Turing Machines

hint:
为什么图灵机被看作一般计算的模型？

content:
Church-Turing 论题认为：任何有效可计算的过程都可以由图灵机实现。

这不是数学定理，而是关于“有效计算”含义的基本论题。

它说明图灵机捕捉了算法计算的本质。

因此，可由普通计算机算法完成的任务，原则上都可由图灵机模拟。

---

# 图灵机变体的等价性

tags: 13.5 Turing Machines

hint:
多带图灵机、二维磁带图灵机等是否比普通图灵机更强？

content:
图灵机有许多变体，例如：

1. 多带图灵机；
2. 磁头可保持不动的图灵机；
3. 二维磁带图灵机；
4. 多磁头图灵机；
5. 非确定型图灵机；
6. 只向一侧无限的磁带模型。

这些变体可能让某些构造更方便，但它们在可计算性意义上与标准图灵机等价。

也就是说，它们不能计算标准图灵机不能计算的函数。

---

# 多带图灵机

tags: 13.5 Turing Machines

hint:
为什么多带模型常用于简化构造？

content:
多带图灵机同时拥有多条磁带和多个读写头。

每一步可以读取多条磁带上的符号，写入多个符号，并移动多个磁头。

多带图灵机常使算法描述更自然，例如复制、比较和多阶段计算。

但每个多带图灵机都可以由单带图灵机模拟，因此不增加可计算能力。

---

# 非确定型图灵机

tags: 13.5 Turing Machines

hint:
nondeterministic Turing machine 和 deterministic Turing machine 的转移规则有什么区别？

content:
确定型图灵机的转移由部分函数给出，因此同一状态和磁带符号最多对应一个动作。

非确定型图灵机的转移由关系给出，因此同一状态和磁带符号可以对应多个可能动作。

运行时，非确定型图灵机可以在可能动作中作出选择。

一个输入被接受，当且仅当存在某条选择路径最终到达接受状态。

---

# 判定问题

tags: 13.5 Turing Machines

hint:
答案只有 yes 或 no 的问题叫什么？

content:
判定问题是答案只有“是”或“否”的问题。

每个判定问题都可对应一个语言：把所有答案为“是”的输入编码组成集合。

若存在图灵机能对每个输入停机并正确回答是否属于该集合，则该判定问题可解。

---

# 不可解问题

tags: 13.5 Turing Machines

hint:
什么样的判定问题没有任何算法能解决？

content:
若不存在任何图灵机能够对所有输入停机并给出正确答案，则该判定问题称为不可解。

不可解问题说明并非每个明确提出的数学或计算问题都有算法解法。

停机问题是最著名的不可解问题之一。

---

# 停机问题

tags: 13.5 Turing Machines

hint:
能否构造一个通用算法判断任意程序是否最终停止？

content:
停机问题问：是否存在一个算法，能判断任意图灵机 $T$ 在任意输入 $x$ 上是否最终停机。

停机问题是不可解的。

也就是说，不存在图灵机能够对所有二元组：

$$
(T,x)
$$

正确判断 $T$ 在输入 $x$ 上是否停机。

---

# 忙海狸函数

tags: 13.5 Turing Machines

hint:
为什么 busy beaver function 是不可计算函数的例子？

content:
忙海狸函数通常用来描述给定状态数的图灵机在空白磁带上停机前最多能产生多少个 $1$。

该函数增长极快。

它是不可计算函数的经典例子。

若能计算忙海狸函数，就能解决停机问题相关的判定，因此这与停机问题不可解矛盾。

---

# 判定问题与特征函数

tags: 13.5 Turing Machines

hint:
为什么每个判定问题都可转化为计算一个 $0$-$1$ 函数？

content:
每个判定问题都可转化为一个特征函数。

若输入 $x$ 的答案为“是”，定义：

$$
\chi(x)=1
$$

若答案为“否”，定义：

$$
\chi(x)=0
$$

该判定问题可解，当且仅当对应的特征函数可由图灵机计算。

---

# 类 $P$ 的图灵机定义

tags: 13.5 Turing Machines

hint:
用确定型图灵机怎样精确定义 $P$？

content:
判定问题属于类 $P$，若存在确定型图灵机能在输入长度的多项式时间内解决该问题。

也就是说，存在多项式 $p(n)$，使得对任意长度为 $n$ 的输入，机器在不超过：

$$
p(n)
$$

步内停机并给出正确答案。

类 $P$ 中的问题通常称为可处理问题。

---

# 类 $NP$ 的图灵机定义

tags: 13.5 Turing Machines

hint:
用非确定型图灵机怎样定义 $NP$？

content:
判定问题属于类 $NP$，若存在非确定型图灵机能在输入长度的多项式时间内解决该问题。

直观上，若答案为“是”，机器可以通过某条正确选择路径在多项式时间内验证这一点。

等价地，$NP$ 可理解为：给定合适证书后，可以在多项式时间内验证“是”实例的问题类。

---

# $P\subseteq NP$

tags: 13.5 Turing Machines

hint:
为什么确定型多项式时间算法也是非确定型多项式时间算法？

content:
每台确定型图灵机都可以看作特殊的非确定型图灵机：每一步只有一个可选转移。

因此，若某判定问题可由确定型图灵机多项式时间解决，则它也可由非确定型图灵机多项式时间解决。

所以：

$$
P\subseteq NP
$$

---

# $P$ 与 $NP$ 问题

tags: 13.5 Turing Machines

hint:
理论计算机科学中最著名的开放问题是什么？

content:
一个核心开放问题是：

$$
P=NP
$$

是否成立。

它问的是：每个能在多项式时间内验证解的问题，是否也能在多项式时间内找到解。

目前尚不知道：

$$
P=NP
$$

还是：

$$
P\ne NP
$$

---

# $NP$ 完全问题

tags: 13.5 Turing Machines

hint:
$NP$ 中最难的一类问题如何定义？

content:
一个问题称为 $NP$ 完全问题，若它满足：

1. 它属于 $NP$；
2. 如果它属于 $P$，则所有 $NP$ 问题都属于 $P$。

换句话说，若某个 $NP$ 完全问题有多项式时间算法，则：

$$
P=NP
$$

典型 $NP$ 完全问题包括可满足性问题和 Hamilton 回路问题。

---

# 可处理与难处理

tags: 13.5 Turing Machines

hint:
从图灵机角度，tractable 和 intractable 如何区分？

content:
若一个判定问题属于 $P$，即能被确定型图灵机在多项式时间内解决，则通常称它是可处理的。

不属于 $P$ 的问题通常称为难处理的。

需要注意，难处理不等于不可解。

难处理问题可能仍可由某个算法解决，只是不存在已知多项式时间算法，或被证明不可能有多项式时间算法。

---

# 可解与不可解

tags: 13.5 Turing Machines

hint:
solvable 和 tractable 有什么区别？

content:
可解问题是存在图灵机算法能对所有输入停机并给出正确答案的问题。

可处理问题是能在多项式时间内解决的问题。

因此：

可处理问题一定可解；

可解问题不一定可处理；

不可解问题则不存在能解决所有输入的算法。

停机问题是不可解问题，不只是难处理问题。

---

# 第十三章核心思想

tags: 13.5 Turing Machines

hint:
Modeling Computation 这一章的主线是什么？

content:
第十三章的核心是用形式模型刻画语言、自动机和计算能力。

主要内容包括：

1. 用文法生成形式语言；
2. 用 Chomsky 层级区分文法类型；
3. 用有限状态机建模有限记忆系统；
4. 用 DFA 和 NFA 识别语言；
5. 用正则表达式、正则文法和有限状态自动机刻画正则语言；
6. 用 pushdown 自动机和线性有界自动机理解更强语言类；
7. 用图灵机作为一般计算模型；
8. 用停机问题说明不可解性；
9. 用图灵机精确定义 $P$、$NP$ 与 $NP$ 完全问题。
