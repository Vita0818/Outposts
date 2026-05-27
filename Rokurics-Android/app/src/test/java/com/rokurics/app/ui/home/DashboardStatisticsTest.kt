package com.rokurics.app.ui.home

import org.junit.Assert.*
import org.junit.Test

class DashboardStatisticsTest {

    @Test
    fun testTotalDurationZeroForNoRecordings() {
        val recordings = emptyList<FakeRecording>()
        val total = recordings.sumOf { it.duration }
        assertEquals(0.0, total, 0.001)
    }

    @Test
    fun testTotalDurationSum() {
        val recordings = listOf(
            FakeRecording("1", 300.0, "Math"),
            FakeRecording("2", 150.0, "Math"),
            FakeRecording("3", 45.0, "Physics")
        )
        val total = recordings.sumOf { it.duration }
        assertEquals(495.0, total, 0.001)
    }

    @Test
    fun testCategoryCount() {
        val recordings = listOf(
            FakeRecording("1", 100.0, "Math"),
            FakeRecording("2", 200.0, "Math"),
            FakeRecording("3", 50.0, "Physics"),
            FakeRecording("4", 75.0, "Chemistry")
        )
        val categories = recordings
            .mapNotNull { it.type }
            .filter { it.isNotEmpty() }
            .distinct()
        assertEquals(3, categories.size)
    }

    @Test
    fun testCategoriesWithEmptyTypeFiltered() {
        val recordings = listOf(
            FakeRecording("1", 100.0, "Math"),
            FakeRecording("2", 200.0, null),
            FakeRecording("3", 50.0, "")
        )
        val categories = recordings
            .mapNotNull { it.type }
            .filter { it.isNotEmpty() }
            .distinct()
        assertEquals(1, categories.size)
    }

    @Test
    fun testUploadedCount() {
        val recordings = listOf(
            FakeRecording("1", 100.0, "Math", uploadStatus = "uploaded"),
            FakeRecording("2", 200.0, "Math", uploadStatus = "localOnly"),
            FakeRecording("3", 50.0, "Physics", uploadStatus = "uploaded"),
            FakeRecording("4", 75.0, "Chemistry", uploadStatus = "pending")
        )
        val uploaded = recordings.count { it.uploadStatus == "uploaded" }
        assertEquals(2, uploaded)
    }

    @Test
    fun testTranscriptionCount() {
        val recordings = listOf(
            FakeRecording("1", transcriptionStatus = "completed"),
            FakeRecording("2", transcriptionStatus = "inProgress"),
            FakeRecording("3", transcriptionStatus = "notStarted"),
            FakeRecording("4", transcriptionStatus = null)
        )
        val count = recordings.count {
            val s = it.transcriptionStatus
            s != null && s != "notStarted" && s != "not_started"
        }
        assertEquals(2, count)
    }

    @Test
    fun testNoteCount() {
        val recordings = listOf(
            FakeRecording("1", noteStatus = "completed"),
            FakeRecording("2", noteStatus = null),
            FakeRecording("3", noteStatus = "notStarted"),
            FakeRecording("4", noteStatus = "generated")
        )
        val count = recordings.count {
            val s = it.noteStatus
            s != null && s != "notStarted" && s != "not_started"
        }
        assertEquals(2, count)
    }

    @Test
    fun testFormatDurationShort() {
        // Minutes only
        assertEquals("5m", formatDur(300.0))
        assertEquals("0m", formatDur(0.0))
        // Hours + minutes
        assertEquals("1h0m", formatDur(3600.0))
        assertEquals("2h30m", formatDur(9000.0))
    }

    @Test
    fun testEmptyStatsAllZeros() {
        val recordings = emptyList<FakeRecording>()
        val totalCount = recordings.size
        val totalDuration = recordings.sumOf { it.duration }
        val uploaded = recordings.count { it.uploadStatus == "uploaded" }

        assertEquals(0, totalCount)
        assertEquals(0.0, totalDuration, 0.001)
        assertEquals(0, uploaded)
    }

    @Test
    fun testFilingCategoryBreakdown() {
        val recordings = listOf(
            FakeRecording("1", type = "Math", subject = "Calculus"),
            FakeRecording("2", type = "Math", subject = "Algebra"),
            FakeRecording("3", type = "Physics", subject = "Mechanics"),
            FakeRecording("4", type = "Math", subject = "Linear Algebra"),
            FakeRecording("5", type = null, subject = null)
        )
        val byType = recordings
            .groupBy { it.type ?: "未分类" }
            .mapValues { it.value.size }

        assertEquals(3, byType["Math"])
        assertEquals(1, byType["Physics"])
        assertEquals(1, byType["未分类"])
    }

    private data class FakeRecording(
        val id: String,
        val duration: Double = 0.0,
        val type: String? = null,
        val subject: String? = null,
        val uploadStatus: String = "localOnly",
        val transcriptionStatus: String? = null,
        val noteStatus: String? = null
    )

    private fun formatDur(seconds: Double): String {
        val totalSecs = seconds.toLong()
        val hours = totalSecs / 3600
        val mins = (totalSecs % 3600) / 60
        return if (hours > 0) "${hours}h${mins}m" else "${mins}m"
    }
}
