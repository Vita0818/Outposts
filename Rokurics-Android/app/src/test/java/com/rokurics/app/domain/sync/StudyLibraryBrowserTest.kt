package com.rokurics.app.domain.sync

import com.rokurics.app.domain.model.*
import org.junit.Assert.*
import org.junit.Test

class StudyLibraryBrowserTest {

    private val fixedTime = 1700000000000L

    private fun makeItem(
        id: String,
        title: String,
        type: String? = null,
        subject: String? = null,
        chapter: String? = null,
        topic: String? = null,
        recordingID: String? = "rec-$id"
    ) = StudyItemMetadata(
        itemID = id,
        title = title,
        filing = StudyFilingPath(type = type, subject = subject, chapter = chapter, topic = topic),
        recordingID = recordingID,
        createdAt = fixedTime,
        updatedAt = fixedTime
    )

    @Test
    fun testEmptyLibraryReturnsEmptyContent() {
        val content = StudyLibraryBrowser.content(
            items = emptyList(),
            folders = emptyList(),
            path = StudyBrowsePath()
        )
        assertTrue(content.isEmpty)
    }

    @Test
    fun testRootShowsTypeLevelFolders() {
        val items = listOf(
            makeItem("i1", "Lecture 1", type = "Math"),
            makeItem("i2", "Lecture 2", type = "Math"),
            makeItem("i3", "Note 1", type = "Physics")
        )
        val content = StudyLibraryBrowser.content(items, emptyList(), StudyBrowsePath())

        assertEquals(2, content.folders.size)
        assertEquals("Math", content.folders[0].title)
        assertEquals(2, content.folders[0].itemCount)
        assertEquals("Physics", content.folders[1].title)
        assertEquals(1, content.folders[1].itemCount)
    }

    @Test
    fun testDrillDownToSubjectLevel() {
        val items = listOf(
            makeItem("i1", "Calculus 1", type = "Math", subject = "Calculus"),
            makeItem("i2", "Calculus 2", type = "Math", subject = "Calculus"),
            makeItem("i3", "Algebra 1", type = "Math", subject = "Algebra")
        )
        // First navigate to Math
        val typeContent = StudyLibraryBrowser.content(items, emptyList(), StudyBrowsePath().appending("Math"))

        assertFalse(typeContent.folders.isEmpty())
        val subjects = typeContent.folders.map { it.title }.toSet()
        assertTrue(subjects.contains("Calculus"))
        assertTrue(subjects.contains("Algebra"))
    }

    @Test
    fun testMaxDepthShowsItemsDirectly() {
        val items = listOf(
            makeItem("i1", "Final Lecture", type = "Math", subject = "Calculus", chapter = "Derivatives", topic = "Chain Rule")
        )
        val path = StudyBrowsePath(
            components = listOf("Math", "Calculus", "Derivatives", "Chain Rule")
        )
        val content = StudyLibraryBrowser.content(items, emptyList(), path)

        assertTrue(content.folders.isEmpty())
        assertEquals(1, content.items.size)
        assertEquals("Final Lecture", content.items[0].title)
    }

    @Test
    fun testUncategorizedItemsGrouped() {
        val items = listOf(
            makeItem("i1", "Untyped", type = null),
            makeItem("i2", "Also Untyped", type = null)
        )
        val content = StudyLibraryBrowser.content(items, emptyList(), StudyBrowsePath())

        assertEquals(1, content.folders.size)
        assertEquals("未分类", content.folders[0].title)
    }

    @Test
    fun testBreadcrumbsAtRoot() {
        val crumbs = StudyLibraryBrowser.breadcrumbs(StudyBrowsePath())
        assertEquals(1, crumbs.size)
        assertEquals("学习库", crumbs[0].first)
    }

    @Test
    fun testBreadcrumbsAtDepth2() {
        val path = StudyBrowsePath(components = listOf("Math", "Calculus"))
        val crumbs = StudyLibraryBrowser.breadcrumbs(path)
        assertEquals(3, crumbs.size)
        assertEquals("学习库", crumbs[0].first)
        assertEquals("Math", crumbs[1].first)
        assertEquals("Calculus", crumbs[2].first)
    }

    @Test
    fun testBrowsePathNavigation() {
        val root = StudyBrowsePath()
        assertTrue(root.isRoot)
        assertEquals(0, root.depth)

        val math = root.appending("Math")
        assertEquals(1, math.depth)
        assertFalse(math.isRoot)
        assertEquals("Math", math.components[0])

        val parent = math.parent
        assertTrue(parent.isRoot)
    }

    @Test
    fun testTruncatedPath() {
        val path = StudyBrowsePath(components = listOf("A", "B", "C", "D"))
        val truncated = path.truncatedTo(2)
        assertEquals(2, truncated.depth)
        assertEquals(listOf("A", "B"), truncated.components)
    }

    @Test
    fun testBrowseContentShowsRecordings() {
        val withFolders = StudyBrowseContent(folders = listOf(
            StudyBrowseFolder(id = "f1", levelKey = "type", title = "Math", itemCount = 3)
        ))
        assertFalse(withFolders.showsRecordings)

        val withoutFolders = StudyBrowseContent(folders = emptyList(), items = listOf(
            makeItem("i1", "Test")
        ))
        assertTrue(withoutFolders.showsRecordings)
    }

    @Test
    fun testFolderTileColors() {
        val folder = StudyBrowseFolder(
            id = "f1",
            levelKey = "type",
            title = "Math",
            colorToken = StudyFolderColorToken.RED
        )
        assertEquals(StudyFolderColorToken.RED, folder.colorToken)
        assertEquals(0xFFE07A5F, folder.colorToken!!.hexColor)
    }

    @Test
    fun testFolderIsFallback() {
        val normal = StudyBrowseFolder(id = "f1", levelKey = "type", title = "Math")
        assertFalse(normal.isFallback)

        val fallback = StudyBrowseFolder(id = "f2", levelKey = "type", title = "未分类")
        assertTrue(fallback.isFallback)
    }
}
