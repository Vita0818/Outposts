/**
 * SamplePresets - Built-in preset data for first launch.
 * Translated from KnowledgePoint.defaultMarkdownText and KnowledgePreset.all
 * in source/Kikaria/KnowledgePoint.swift
 */

import { KnowledgePreset } from '../model/KnowledgePoint'

/**
 * Default sample Markdown with math/calculus knowledge points.
 * Content preserved from the source's defaultMarkdownText.
 */
export const DEFAULT_MARKDOWN_TEXT = `# Limit Preservation of Sign

tags: Calculus, Limit, Basic

hint:
If the limit is positive, the function value is positive nearby.

content:
If lim f(x) = A and A > 0, then f(x) > 0 in some sufficiently small neighborhood.

---

# Rolle's Theorem

tags: Calculus, Mean Value Theorem

hint:
Check continuity, differentiability, and equal endpoint values.

content:
If f is continuous on [a,b], differentiable on (a,b), and f(a) = f(b), then there is a point c in (a,b) with f'(c) = 0.

---

# Derivative Product Rule

tags: Calculus, Derivative, Basic

hint:
Differentiate one factor at a time, then add the two terms.

content:
For differentiable functions f and g, (fg)' = f'g + fg'.

---

# Matrix Multiplication Size

tags: Linear Algebra, Matrix, Basic

hint:
The inner dimensions must match.

content:
An m by n matrix can multiply an n by p matrix, and the result is an m by p matrix.

---

# Linear Independence

tags: Linear Algebra, Vector Space

hint:
Only the trivial coefficient combination gives the zero vector.

content:
Vectors v1 through vn are linearly independent if c1v1 + ... + cnvn = 0 implies c1 = ... = cn = 0.

---

# Bayes' Theorem

tags: Probability, Conditional Probability

hint:
Reverse a conditional probability using the prior and evidence.

content:
P(A | B) = P(B | A)P(A) / P(B), assuming P(B) is not zero.`

/**
 * Built-in presets - mirroring the source's KnowledgePreset.all
 */
export const DEFAULT_PRESET_ID = 'advanced-math'

export const builtInPresets: KnowledgePreset[] = [
  new KnowledgePreset(
    DEFAULT_PRESET_ID,
    'Advanced Mathematics',
    'Calculus, Linear Algebra, Probability',
    'Core concepts in advanced mathematics including limits, derivatives, matrices, and probability theory.',
    'Mathematics',
    DEFAULT_MARKDOWN_TEXT,
    true
  )
]
