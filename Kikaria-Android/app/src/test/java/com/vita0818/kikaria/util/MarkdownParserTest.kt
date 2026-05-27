package com.vita0818.kikaria.util

import org.junit.Assert.*
import org.junit.Test
import java.util.Date

class MarkdownParserTest {

    @Test
    fun parsesSingleKnowledgePoint() {
        val markdown = """
# Limit Preservation

tags: Calculus, Limit

hint:
If the limit is positive, the function value is positive nearby.

content:
If lim f(x) = A and A > 0, then f(x) > 0 in some sufficiently small neighborhood.
        """.trimIndent()

        val points = MarkdownParser.parseKnowledgePoints(markdown)
        assertEquals(1, points.size)
        val point = points[0]
        assertEquals("Limit Preservation", point.title)
        assertEquals(listOf("Calculus", "Limit"), point.tags)
        assertTrue(point.hint.contains("limit is positive"))
        assertTrue(point.content.contains("lim f(x) = A"))
    }

    @Test
    fun parsesMultipleKnowledgePoints() {
        val markdown = """
# Point One

tags: Tag1, Tag2

hint:
Hint for point one.

content:
Content for point one.

---

# Point Two

tags: Tag3

hint:
Hint for point two.

content:
Content for point two.
        """.trimIndent()

        val points = MarkdownParser.parseKnowledgePoints(markdown)
        assertEquals(2, points.size)
        assertEquals("Point One", points[0].title)
        assertEquals(listOf("Tag1", "Tag2"), points[0].tags)
        assertEquals("Point Two", points[1].title)
        assertEquals(listOf("Tag3"), points[1].tags)
    }

    @Test
    fun parsesChineseTags() {
        val markdown = """
# 极限的保号性

tags: 微积分, 极限，基础

hint:
若极限为正，则函数值在附近也为正。

content:
若 lim f(x) = A 且 A > 0，则在充分小的邻域内 f(x) > 0。
        """.trimIndent()

        val points = MarkdownParser.parseKnowledgePoints(markdown)
        assertEquals(1, points.size)
        assertEquals("极限的保号性", points[0].title)
        assertEquals(listOf("微积分", "极限", "基础"), points[0].tags)
    }

    @Test
    fun returnsEmptyListForEmptyInput() {
        val points = MarkdownParser.parseKnowledgePoints("")
        assertTrue(points.isEmpty())
    }

    @Test
    fun returnsEmptyListForMissingHintOrContent() {
        val markdown = """
# Incomplete Point

tags: Test

hint:

content:

        """.trimIndent()

        val points = MarkdownParser.parseKnowledgePoints(markdown)
        assertTrue(points.isEmpty())
    }

    @Test
    fun exportsPointsToMarkdown() {
        val markdown = """
# Test Title

tags: Tag1, Tag2

hint:
Test hint.

content:
Test content.
        """.trimIndent()

        val points = MarkdownParser.parseKnowledgePoints(markdown)
        val exported = MarkdownParser.markdownFromPoints(points)
        assertTrue(exported.contains("# Test Title"))
        assertTrue(exported.contains("tags: Tag1, Tag2"))
        assertTrue(exported.contains("hint:"))
        assertTrue(exported.contains("content:"))
    }

    @Test
    fun roundTripPreservesContent() {
        val original = """
# Point A

tags: Math, Basic

hint:
Hint for point A.

content:
Content for point A with multiple lines.

---

# Point B

tags: Physics

hint:
Hint for point B.

content:
Content for point B.
        """.trimIndent()

        val points = MarkdownParser.parseKnowledgePoints(original)
        assertEquals(2, points.size)
        val exported = MarkdownParser.markdownFromPoints(points)
        val reparsed = MarkdownParser.parseKnowledgePoints(exported)
        assertEquals(2, reparsed.size)
        assertEquals("Point A", reparsed[0].title)
        assertEquals("Point B", reparsed[1].title)
    }

    @Test
    fun parsesKnowledgePointWithDate() {
        val date = Date(1000000L)
        val markdown = """
# Dated Point

tags: Test

hint:
A hint.

content:
Some content.
        """.trimIndent()

        val points = MarkdownParser.parseKnowledgePoints(markdown, date)
        assertEquals(1, points.size)
        assertEquals(date, points[0].createdAt)
        assertEquals(date, points[0].updatedAt)
    }
}
