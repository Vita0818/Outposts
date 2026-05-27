package com.vita0818.kikaria.util

import org.junit.Assert.*
import org.junit.Test

class KikariaMathFallbackTest {

    @Test
    fun convertsGreekLetters() {
        val result = KikariaMathFallback.convert("\\alpha + \\beta = \\gamma")
        assertTrue(result.contains("α"))
        assertTrue(result.contains("β"))
        assertTrue(result.contains("γ"))
        assertFalse(result.contains("\\alpha"))
        assertFalse(result.contains("\\beta"))
    }

    @Test
    fun convertsGreekUppercase() {
        val result = KikariaMathFallback.convert("\\Delta x \\to 0")
        assertTrue(result.contains("Δ"))
        assertFalse(result.contains("\\Delta"))
    }

    @Test
    fun convertsRelations() {
        val result = KikariaMathFallback.convert("x \\leq y \\geq z \\neq 0")
        assertTrue(result.contains("≤"))
        assertTrue(result.contains("≥"))
        assertTrue(result.contains("≠"))
        assertFalse(result.contains("\\leq"))
    }

    @Test
    fun convertsArrows() {
        val result = KikariaMathFallback.convert("x \\to y \\implies z")
        assertTrue(result.contains("→"))
        assertTrue(result.contains("⇒"))
    }

    @Test
    fun convertsFractions() {
        val result = KikariaMathFallback.convert("\\frac{a}{b}")
        assertTrue(result.contains("/"))
        assertTrue(result.contains("a"))
        assertTrue(result.contains("b"))
        assertFalse(result.contains("\\frac"))
    }

    @Test
    fun convertsSumAndIntegral() {
        val result = KikariaMathFallback.convert("\\sum_{i=1}^{n} \\int_a^b")
        assertTrue(result.contains("Σ"))
        assertTrue(result.contains("∫"))
    }

    @Test
    fun convertsSqrt() {
        val result = KikariaMathFallback.convert("\\sqrt{x^2 + y^2}")
        assertTrue(result.contains("√"))
        assertFalse(result.contains("\\sqrt"))
    }

    @Test
    fun convertsSetNotation() {
        val result = KikariaMathFallback.convert("\\{x \\in \\mathbb{R} \\mid x > 0\\}")
        assertTrue(result.contains("∈"))
    }

    @Test
    fun convertsOperators() {
        val result = KikariaMathFallback.convert("\\pm \\mp \\times \\div \\cdot \\circ")
        assertTrue(result.contains("±"))
        assertTrue(result.contains("∓"))
        assertTrue(result.contains("×"))
        assertTrue(result.contains("÷"))
        assertTrue(result.contains("·"))
        assertTrue(result.contains("∘"))
    }

    @Test
    fun stripsEnvironments() {
        val result = KikariaMathFallback.convert("\\begin{cases} a \\\\ b \\end{cases}")
        assertTrue(result.contains("a"))
        assertTrue(result.contains("b"))
        assertFalse(result.contains("\\begin"))
        assertFalse(result.contains("\\end"))
    }

    @Test
    fun rendersContentWithInlineMath() {
        val D = "${'$'}"
        val content = "函数 ${D}f(x)=x^2${D} 的导数是 ${D}2x${D}。"
        val rendered = KikariaMathFallback.renderContent(content)
        assertTrue(rendered.contains("f(x)=x"))
        assertTrue(rendered.contains("2x"))
        assertFalse(rendered.contains("${'$'}"))
    }

    @Test
    fun rendersContentWithBlockMath() {
        val D = "${'$'}"
        val content = "极限定义：\n\n$D${D}\n\\lim_{x\\to0}\\frac{\\sin x}{x}=1\n$D${D}"
        val rendered = KikariaMathFallback.renderContent(content)
        assertTrue(rendered.contains("lim"))
        assertTrue(rendered.contains("→"))
        assertTrue(rendered.contains("/"))
    }

    @Test
    fun plainTextPassesThrough() {
        val result = KikariaMathFallback.convert("This is plain text with no math.")
        assertEquals("This is plain text with no math.", result.trim())
    }

    @Test
    fun handlesEmptyString() {
        assertEquals("", KikariaMathFallback.convert(""))
    }

    @Test
    fun stripsScalingCommands() {
        val result = KikariaMathFallback.convert("\\left( x \\right)")
        assertTrue(result.contains("("))
        assertTrue(result.contains("x"))
        assertTrue(result.contains(")"))
        assertFalse(result.contains("\\left"))
        assertFalse(result.contains("\\right"))
    }

    @Test
    fun convertsPartialNabla() {
        val result = KikariaMathFallback.convert("\\partial f / \\partial x")
        assertTrue(result.contains("∂"))
    }

    @Test
    fun stripsRemainingBackslashCommands() {
        val result = KikariaMathFallback.convert("\\unknownCommand{value}")
        assertFalse(result.contains("\\unknownCommand"))
    }

    // ── Edge cases: nested braces ──

    @Test
    fun handlesNestedFrac() {
        val result = KikariaMathFallback.convert("\\frac{\\frac{a}{b}}{c}")
        assertTrue(result.contains("a"))
        assertTrue(result.contains("b"))
        assertTrue(result.contains("c"))
        assertTrue(result.contains("/"))
        assertFalse(result.contains("\\frac"))
    }

    @Test
    fun handlesNestedSqrt() {
        val result = KikariaMathFallback.convert("\\sqrt{\\sqrt{x}}")
        assertTrue(result.contains("√"))
        assertTrue(result.contains("x"))
        assertFalse(result.contains("\\sqrt"))
    }

    @Test
    fun handlesDeeplyNestedBraces() {
        val result = KikariaMathFallback.convert(
            "\\frac{\\sqrt{\\alpha + \\beta}}{\\gamma}"
        )
        assertTrue(result.contains("α"))
        assertTrue(result.contains("β"))
        assertTrue(result.contains("γ"))
        assertTrue(result.contains("√"))
        assertTrue(result.contains("/"))
    }

    // ── Edge cases: malformed input ──

    @Test
    fun handlesUnmatchedBraces() {
        val result = KikariaMathFallback.convert("\\frac{a{b}")
        // Should not crash; should strip the unrecognized command prefix
        assertNotNull(result)
    }

    @Test
    fun handlesEmptyBracedArg() {
        val result = KikariaMathFallback.convert("\\frac{}{b}")
        assertTrue(result.contains("/"))
        assertTrue(result.contains("b"))
        assertFalse(result.contains("\\frac"))
    }

    @Test
    fun handlesCommandWithoutBraces() {
        val result = KikariaMathFallback.convert("\\alpha + \\sqrt x")  // sqrt missing braces
        assertTrue(result.contains("α"))
        // Should not crash on malformed sqrt
        assertNotNull(result)
    }

    @Test
    fun handlesConsecutiveBracedCommands() {
        val result = KikariaMathFallback.convert("\\bar{x}\\hat{y}\\tilde{z}")
        assertTrue(result.contains("x̄"))
        assertTrue(result.contains("ŷ"))
        assertTrue(result.contains("z̃"))
    }

    // ── Edge cases: mixed bidirectional text ──

    @Test
    fun handlesCjkWithMathFormula() {
        val D = "${'$'}"
        val content = "其中 $D\\alpha$D 是角度，$D\\beta$D 是方位角。"
        val rendered = KikariaMathFallback.renderContent(content)
        assertTrue(rendered.contains("α"))
        assertTrue(rendered.contains("β"))
        assertTrue(rendered.contains("角度"))
        assertTrue(rendered.contains("方位角"))
        assertFalse(rendered.contains("$"))
    }

    @Test
    fun handlesCjkBlockMath() {
        val D = "${'$'}"
        val content = "公式如下：\n\n$D$D\n\\frac{a}{b} + c\n$D$D\n\n其中 a、b、c 为常数。"
        val rendered = KikariaMathFallback.renderContent(content)
        assertTrue(rendered.contains("a"))
        assertTrue(rendered.contains("b"))
        assertTrue(rendered.contains("c"))
        assertTrue(rendered.contains("/"))
        assertTrue(rendered.contains("常数"))
        assertFalse(rendered.contains("$D$D"))
    }

    @Test
    fun handlesRtlTextWithMath() {
        val result = KikariaMathFallback.convert("\\text{مرحبا} \\alpha \\beta")
        // Should preserve Arabic text and convert Greek
        assertTrue(result.contains("α"))
        assertTrue(result.contains("β"))
        assertFalse(result.contains("\\alpha"))
    }

    @Test
    fun emptyBracedCommandReturnsEmpty() {
        val result = KikariaMathFallback.convert("\\operatorname{}")
        // Should not crash; empty operator name should produce empty result
        assertNotNull(result)
    }
}
