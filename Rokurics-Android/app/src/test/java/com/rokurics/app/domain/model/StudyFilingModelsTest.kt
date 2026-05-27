package com.rokurics.app.domain.model

import org.junit.Assert.*
import org.junit.Test

class StudyFilingModelsTest {

    @Test
    fun studyFilingPath_empty() {
        val path = StudyFilingPath()
        assertTrue(path.isEmpty)
        assertEquals("未分类", path.displaySummary)
    }

    @Test
    fun studyFilingPath_fullHierarchy() {
        val path = StudyFilingPath(
            type = "自然科学",
            subject = "物理",
            chapter = "力学",
            topic = "牛顿定律"
        )
        assertFalse(path.isEmpty)
        assertEquals("自然科学 / 物理 / 力学 / 牛顿定律", path.displaySummary)
    }

    @Test
    fun studyFilingPath_partialHierarchy() {
        val path = StudyFilingPath(type = "数学", subject = "微积分")
        assertEquals("数学 / 微积分", path.displaySummary)
    }

    @Test
    fun studyBrowsePath_appending() {
        val path = StudyBrowsePath(listOf("自然科学", "物理"))
        val child = path.appending("力学")
        assertEquals(3, child.depth)
        assertEquals(listOf("自然科学", "物理", "力学"), child.components)
    }

    @Test
    fun studyBrowsePath_truncatedTo() {
        val path = StudyBrowsePath(listOf("A", "B", "C", "D"))
        val truncated = path.truncatedTo(2)
        assertEquals(listOf("A", "B"), truncated.components)
    }

    @Test
    fun studyTag_displayTitle() {
        val tag = StudyTag(id = "tag1", namespace = "custom", value = "重要", displayName = "非常重要")
        assertEquals("非常重要", tag.displayTitle)
    }

    @Test
    fun studyTag_defaultDisplayTitle() {
        val tag = StudyTag(id = "tag2", namespace = "custom", value = "重要")
        assertEquals("重要", tag.displayTitle)
    }
}
