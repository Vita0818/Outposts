package com.vita0818.kikaria.viewmodel

import org.junit.Assert.assertEquals
import org.junit.Test
import java.util.Calendar
import java.util.Date

/**
 * Pure-logic tests for countdownDays calculation and TodayOverview progress messages.
 *
 * These mirror the calculation logic in KikariaViewModel.countdownDays
 * and TodayOverviewScreen progress message branches, isolated from Android.
 */
class CountdownCalculationTest {

    // ── countdownDays pure logic (matches KikariaViewModel.countdownDays) ──

    @Test
    fun `countdownDays returns 0 when endDate is null`() {
        assertEquals(0, countdownDays(null, today()))
    }

    @Test
    fun `countdownDays returns 0 when endDate equals today`() {
        val today = today()
        assertEquals(0, countdownDays(today, today))
    }

    @Test
    fun `countdownDays returns 1 when endDate is tomorrow`() {
        assertEquals(1, countdownDays(tomorrow(), today()))
    }

    @Test
    fun `countdownDays returns 0 when endDate is yesterday past`() {
        assertEquals(0, countdownDays(yesterday(), today()))
    }

    @Test
    fun `countdownDays returns 30 when endDate is 30 days ahead`() {
        val future = daysFromNow(30)
        assertEquals(30, countdownDays(future, today()))
    }

    @Test
    fun `countdownDays returns 0 when endDate is far in past`() {
        val past = daysFromNow(-365)
        assertEquals(0, countdownDays(past, today()))
    }

    @Test
    fun `countdownDays works for large future value`() {
        val future = daysFromNow(999)
        assertEquals(999, countdownDays(future, today()))
    }

    @Test
    fun `countdownDays returns 0 for null endDate regardless of today`() {
        assertEquals(0, countdownDays(null, daysFromNow(100)))
        assertEquals(0, countdownDays(null, yesterday()))
    }

    @Test
    fun `countdownDays edge — one second before midnight`() {
        val cal = Calendar.getInstance()
        cal.set(Calendar.HOUR_OF_DAY, 23)
        cal.set(Calendar.MINUTE, 59)
        cal.set(Calendar.SECOND, 59)
        cal.set(Calendar.MILLISECOND, 0)
        val endOfToday = cal.time

        // Same day, so diff is 0
        assertEquals(0, countdownDays(endOfToday, today()))
    }

    @Test
    fun `countdownDays edge — one second after midnight tomorrow`() {
        val cal = Calendar.getInstance()
        cal.add(Calendar.DAY_OF_YEAR, 1)
        cal.set(Calendar.HOUR_OF_DAY, 0)
        cal.set(Calendar.MINUTE, 0)
        cal.set(Calendar.SECOND, 1)
        cal.set(Calendar.MILLISECOND, 0)
        val startOfTomorrow = cal.time

        // Tomorrow at 00:00:01, so diff should be 1 day
        assertEquals(1, countdownDays(startOfTomorrow, today()))
    }

    // ── TodayOverview progress message logic ──

    @Test
    fun `progress message — goal reached shows congratulations`() {
        assertEquals("congratulations", progressCategory(mastered = 20, goal = 20, reviews = 0))
        assertEquals("congratulations", progressCategory(mastered = 25, goal = 20, reviews = 5))
    }

    @Test
    fun `progress message — active review with progress shows remaining`() {
        assertEquals("progress", progressCategory(mastered = 5, goal = 20, reviews = 3))
        assertEquals("progress", progressCategory(mastered = 0, goal = 20, reviews = 1))
    }

    @Test
    fun `progress message — no review activity shows quiet`() {
        assertEquals("quiet", progressCategory(mastered = 0, goal = 20, reviews = 0))
        assertEquals("quiet", progressCategory(mastered = 5, goal = 20, reviews = 0))
    }

    @Test
    fun `progress message — goal is 1 and reached`() {
        assertEquals("congratulations", progressCategory(mastered = 1, goal = 1, reviews = 1))
    }

    @Test
    fun `progress message — goal is 0 edge case`() {
        // With goal=0, mastered >= 0 is always true, so congratulations
        assertEquals("congratulations", progressCategory(mastered = 0, goal = 0, reviews = 0))
    }

    @Test
    fun `remaining to goal clamps at 0`() {
        assertEquals(0, remainingToGoal(mastered = 25, goal = 20))
        assertEquals(0, remainingToGoal(mastered = 20, goal = 20))
        assertEquals(15, remainingToGoal(mastered = 5, goal = 20))
    }

    // ── Countdown display formatting ──

    @Test
    fun `countdown display shows days when positive`() {
        assertEquals("30天", countdownDisplay(30))
        assertEquals("1天", countdownDisplay(1))
    }

    @Test
    fun `countdown display shows dash when zero or negative`() {
        assertEquals("--", countdownDisplay(0))
        assertEquals("--", countdownDisplay(-1))
    }

    // ── Helpers ──

    companion object {
        private fun today(): Date {
            val cal = Calendar.getInstance()
            cal.set(Calendar.HOUR_OF_DAY, 0)
            cal.set(Calendar.MINUTE, 0)
            cal.set(Calendar.SECOND, 0)
            cal.set(Calendar.MILLISECOND, 0)
            return cal.time
        }

        private fun tomorrow(): Date {
            val cal = Calendar.getInstance()
            cal.add(Calendar.DAY_OF_YEAR, 1)
            cal.set(Calendar.HOUR_OF_DAY, 0)
            cal.set(Calendar.MINUTE, 0)
            cal.set(Calendar.SECOND, 0)
            cal.set(Calendar.MILLISECOND, 0)
            return cal.time
        }

        private fun yesterday(): Date {
            val cal = Calendar.getInstance()
            cal.add(Calendar.DAY_OF_YEAR, -1)
            cal.set(Calendar.HOUR_OF_DAY, 0)
            cal.set(Calendar.MINUTE, 0)
            cal.set(Calendar.SECOND, 0)
            cal.set(Calendar.MILLISECOND, 0)
            return cal.time
        }

        private fun daysFromNow(days: Int): Date {
            val cal = Calendar.getInstance()
            cal.add(Calendar.DAY_OF_YEAR, days)
            cal.set(Calendar.HOUR_OF_DAY, 0)
            cal.set(Calendar.MINUTE, 0)
            cal.set(Calendar.SECOND, 0)
            cal.set(Calendar.MILLISECOND, 0)
            return cal.time
        }

        /**
         * Pure calculation matching KikariaViewModel.countdownDays.
         */
        fun countdownDays(endDate: Date?, today: Date): Int {
            if (endDate == null) return 0
            val target = Calendar.getInstance().apply {
                time = endDate
                set(Calendar.HOUR_OF_DAY, 0)
                set(Calendar.MINUTE, 0)
                set(Calendar.SECOND, 0)
                set(Calendar.MILLISECOND, 0)
            }.time
            val diffMs = target.time - today.time
            val days = (diffMs / (1000 * 60 * 60 * 24)).toInt()
            return maxOf(0, days)
        }

        /**
         * Categorizes the TodayOverview progress message into one of three states.
         * Matches the when-branches in TodayOverviewScreen.
         */
        fun progressCategory(mastered: Int, goal: Int, reviews: Int): String {
            return when {
                mastered >= goal -> "congratulations"
                reviews > 0 -> "progress"
                else -> "quiet"
            }
        }

        fun remainingToGoal(mastered: Int, goal: Int): Int {
            return maxOf(0, goal - mastered)
        }

        fun countdownDisplay(days: Int): String {
            return if (days > 0) "${days}天" else "--"
        }
    }
}
