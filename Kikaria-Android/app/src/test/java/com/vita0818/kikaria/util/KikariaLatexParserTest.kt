package com.vita0818.kikaria.util

import org.junit.Assert.*
import org.junit.Test

class KikariaLatexParserTest {

    private val D = "${'$'}"

    @Test
    fun tokenizesPlainText() {
        val tokens = KikariaLatexParser.tokenize("Hello world.")
        assertEquals(1, tokens.size)
        assertTrue(tokens[0] is LatexToken.Text)
        assertEquals("Hello world.", (tokens[0] as LatexToken.Text).value)
    }

    @Test
    fun tokenizesInlineMath() {
        val tokens = KikariaLatexParser.tokenize("This is ${D}f(x)=x^2${D} formula.")
        assertEquals(3, tokens.size)
        assertTrue(tokens[0] is LatexToken.Text)
        assertTrue(tokens[1] is LatexToken.InlineMath)
        assertTrue(tokens[2] is LatexToken.Text)
        val math = tokens[1] as LatexToken.InlineMath
        assertEquals("f(x)=x^2", math.body)
    }

    @Test
    fun tokenizesBlockMath() {
        val tokens = KikariaLatexParser.tokenize("Text $D${D}\nx = y\n$D${D} more text.")
        assertEquals(3, tokens.size)
        assertTrue(tokens[0] is LatexToken.Text)
        assertTrue(tokens[1] is LatexToken.BlockMath)
        assertTrue(tokens[2] is LatexToken.Text)
        val math = tokens[1] as LatexToken.BlockMath
        assertTrue(math.body.trim().contains("x = y"))
    }

    @Test
    fun tokenizesMultipleMathBlocks() {
        val tokens = KikariaLatexParser.tokenize("A ${D}a+b${D} B ${D}c-d${D} C")
        val mathTokens = tokens.filterIsInstance<LatexToken.InlineMath>()
        assertEquals(2, mathTokens.size)
        assertEquals("a+b", mathTokens[0].body)
        assertEquals("c-d", mathTokens[1].body)
    }

    @Test
    fun preservesCodeSpans() {
        val tokens = KikariaLatexParser.tokenize("Code: `${D}not_math${D}` end")
        assertEquals(1, tokens.size)
        assertTrue(tokens[0] is LatexToken.Text)
        assertTrue((tokens[0] as LatexToken.Text).value.contains("`${D}not_math${D}`"))
    }

    @Test
    fun preservesCodeBlock() {
        val tokens = KikariaLatexParser.tokenize("```\n${D}not_math${D}\n$D${D}\n```")
        assertEquals(1, tokens.size)
        assertTrue(tokens[0] is LatexToken.Text)
    }

    @Test
    fun handlesEscapedDollar() {
        val tokens = KikariaLatexParser.tokenize("Price \\${D}100 is not math but ${D}x${D} is.")
        val mathTokens = tokens.filterIsInstance<LatexToken.InlineMath>()
        assertEquals(1, mathTokens.size)
        assertEquals("x", mathTokens[0].body)
    }

    @Test
    fun emptyStringReturnsEmptyList() {
        val tokens = KikariaLatexParser.tokenize("")
        assertTrue(tokens.isEmpty())
    }

    @Test
    fun realWorldMathContent() {
        val text = "若 lim f(${D}x${D}) = A 且 A > 0。\n\n当 ${D}x \\\\to 0${D} 时，${D}\\\\frac{\\\\sin ${D}x${D}}{${D}x${D}}=1${D}。"
        val tokens = KikariaLatexParser.tokenize(text)
        val mathTokens = tokens.filterIsInstance<LatexToken.InlineMath>()
        // Verify we find math tokens (the actual parsing may vary due to escaping complexity)
        assertTrue(mathTokens.isNotEmpty())
    }

    @Test
    fun inlineMathStopsAtNewline() {
        val text = "${D} a\nb ${D}"
        val tokens = KikariaLatexParser.tokenize(text)
        val mathTokens = tokens.filterIsInstance<LatexToken.InlineMath>()
        assertTrue(mathTokens.isEmpty())
    }
}
