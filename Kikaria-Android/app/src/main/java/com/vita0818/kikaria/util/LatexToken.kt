package com.vita0818.kikaria.util

/**
 * LaTeX token types, translated from LatexToken.swift.
 *
 * Represents parsed elements from a knowledge point content string
 * that contains LaTeX math markup ($...$ and $$...$$).
 */
sealed class LatexToken {
    data class Text(val value: String) : LatexToken()
    data class InlineMath(val source: String, val body: String) : LatexToken()
    data class BlockMath(val source: String, val body: String) : LatexToken()
    data class Fallback(val value: String) : LatexToken()
}
