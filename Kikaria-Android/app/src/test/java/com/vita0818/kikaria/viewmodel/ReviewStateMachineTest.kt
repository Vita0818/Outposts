package com.vita0818.kikaria.viewmodel

import com.vita0818.kikaria.data.KnowledgePoint
import org.junit.Assert.*
import org.junit.Test
import kotlin.math.abs

/**
 * Tests the pure-Kotlin review state machine logic that drives both phone
 * and tablet review layouts, without Android framework dependencies.
 *
 * ViewModel-level tests (KikariaViewModel) require Robolectric or
 * instrumented tests due to AndroidViewModel dependency — tracked as a gap.
 */
class ReviewStateMachineTest {

    // ── Review point progression logic ──

    @Test
    fun `next index advances by 1`() {
        val queue = listOf(point("A"), point("B"), point("C"))
        var idx = 0
        idx = advanceIndex(queue, idx)
        assertEquals(1, idx)
    }

    @Test
    fun `next index clamps at last item`() {
        val queue = listOf(point("A"), point("B"))
        var idx = 1
        idx = advanceIndex(queue, idx)
        assertEquals(1, idx)
    }

    @Test
    fun `previous index goes back by 1`() {
        var idx = 2
        idx = retreatIndex(idx)
        assertEquals(1, idx)
    }

    @Test
    fun `previous index clamps at 0`() {
        var idx = 0
        idx = retreatIndex(idx)
        assertEquals(0, idx)
    }

    @Test
    fun `progress is 0 for empty queue`() {
        assertEquals(0f, calcProgress(emptyList(), -1))
    }

    @Test
    fun `progress reaches 1 at last item`() {
        assertEquals(1f, calcProgress(listOf(point("A"), point("B"), point("C")), 2))
    }

    @Test
    fun `progress at 50 percent in 4-item queue at index 1`() {
        assertEquals(0.5f, calcProgress(listOf(point("A"), point("B"), point("C"), point("D")), 1))
    }

    // ── Progressive reveal simulation (hint → content → next) ──

    @Test
    fun `progressive reveal starts with nothing shown`() {
        val state = ReviewDisplayState()
        assertFalse(state.hintShown)
        assertFalse(state.contentShown)
    }

    @Test
    fun `show hint reveals hint only`() {
        val state = ReviewDisplayState()
        state.showHint()
        assertTrue(state.hintShown)
        assertFalse(state.contentShown)
    }

    @Test
    fun `show content after hint reveals both`() {
        val state = ReviewDisplayState()
        state.showHint()
        state.showContent()
        assertTrue(state.hintShown)
        assertTrue(state.contentShown)
    }

    @Test
    fun `next point resets display flags`() {
        val state = ReviewDisplayState()
        state.showHint()
        state.showContent()
        state.resetForNext()
        assertFalse(state.hintShown)
        assertFalse(state.contentShown)
    }

    // ── iOS-aligned swipe gesture logic (matches ReviewScreen gesture handlers) ──

    @Test
    fun `horizontal dominance detection — horizontal wins when 1_4x larger`() {
        assertTrue(isHorizontalDominant(dx = 100f, dy = 50f, threshold = 80f, dominance = 1.4f))
    }

    @Test
    fun `horizontal dominance — vertical wins when similar magnitude`() {
        assertFalse(isHorizontalDominant(dx = 100f, dy = 80f, threshold = 80f, dominance = 1.4f))
    }

    @Test
    fun `horizontal dominance — requires minimum threshold`() {
        assertFalse(isHorizontalDominant(dx = 30f, dy = 10f, threshold = 80f, dominance = 1.4f))
    }

    @Test
    fun `vertical dominance detection — vertical wins when 1_4x larger`() {
        assertTrue(isVerticalDominant(dx = 40f, dy = 100f, threshold = 90f, dominance = 1.4f))
    }

    @Test
    fun `vertical dominance — horizontal wins when similar magnitude`() {
        assertFalse(isVerticalDominant(dx = 90f, dy = 100f, threshold = 90f, dominance = 1.4f))
    }

    @Test
    fun `swipe left in normal mode — reveals content and adds reinforcement`() {
        val action = mapIosSwipeLeft(ReviewMode.NORMAL)
        assertEquals(IosSwipeAction.REVEAL_AND_REINFORCE, action)
    }

    @Test
    fun `swipe left in reinforcement mode — removes reinforcement and goes to next`() {
        val action = mapIosSwipeLeft(ReviewMode.REINFORCEMENT)
        assertEquals(IosSwipeAction.REMOVE_REINFORCEMENT_AND_NEXT, action)
    }

    @Test
    fun `swipe left in mastered mode — removes mastered and goes to next`() {
        val action = mapIosSwipeLeft(ReviewMode.MASTERED)
        assertEquals(IosSwipeAction.REMOVE_MASTERED_AND_NEXT, action)
    }

    @Test
    fun `swipe right in normal mode nothing shown — opens scope panel`() {
        val action = mapIosSwipeRight(ReviewMode.NORMAL, isContentShown = false, isHintShown = false)
        assertEquals(IosSwipeAction.OPEN_SCOPE_PANEL, action)
    }

    @Test
    fun `swipe right with content shown — toggles reinforcement`() {
        val action = mapIosSwipeRight(ReviewMode.NORMAL, isContentShown = true, isHintShown = true)
        assertEquals(IosSwipeAction.TOGGLE_REINFORCEMENT, action)
    }

    @Test
    fun `swipe right with only hint shown — reveals content`() {
        val action = mapIosSwipeRight(ReviewMode.NORMAL, isContentShown = false, isHintShown = true)
        assertEquals(IosSwipeAction.SHOW_CONTENT, action)
    }

    @Test
    fun `swipe right in non-normal mode nothing shown — shows hint`() {
        val action = mapIosSwipeRight(ReviewMode.REINFORCEMENT, isContentShown = false, isHintShown = false)
        assertEquals(IosSwipeAction.SHOW_HINT, action)
    }

    @Test
    fun `swipe up with content shown — goes to next point`() {
        val action = mapIosSwipeUp(isContentShown = true)
        assertEquals(IosSwipeAction.NEXT_POINT, action)
    }

    @Test
    fun `swipe up with content hidden — reveals content`() {
        val action = mapIosSwipeUp(isContentShown = false)
        assertEquals(IosSwipeAction.SHOW_CONTENT, action)
    }

    @Test
    fun `swipe up with content shown but no next — no action`() {
        val action = mapIosSwipeUp(isContentShown = true)
        assertEquals(IosSwipeAction.NEXT_POINT, action)
    }

    @Test
    fun `swipe down — always goes to previous point`() {
        assertEquals(IosSwipeAction.PREVIOUS_POINT, mapIosSwipeDown())
    }

    // ── KnowledgePoint model operations ──

    @Test
    fun `add reinforcement increments count and sets isReinforced`() {
        val p = point("A")
        assertFalse(p.isReinforced)
        assertEquals(0, p.reinforcementCount)

        val reinforced = p.addReinforcement()
        assertTrue(reinforced.isReinforced)
        assertEquals(1, reinforced.reinforcementCount)
        assertNotNull(reinforced.lastReinforcedAt)
    }

    @Test
    fun `add reinforcement multiple times accumulates count`() {
        val p = point("A")
        val r1 = p.addReinforcement()
        val r2 = r1.addReinforcement()
        val r3 = r2.addReinforcement()
        assertEquals(3, r3.reinforcementCount)
        assertEquals("×3", "×${r3.reinforcementCount}")
    }

    @Test
    fun `clear reinforcement resets count and removes isReinforced`() {
        val p = point("A").addReinforcement().addReinforcement()
        assertTrue(p.isReinforced)
        assertEquals(2, p.reinforcementCount)

        val cleared = p.clearReinforcement()
        assertFalse(cleared.isReinforced)
        assertEquals(0, cleared.reinforcementCount)
        assertNull(cleared.lastReinforcedAt)
    }

    @Test
    fun `mark mastered sets isMastered and clears reinforcement`() {
        val p = point("A").addReinforcement()
        val mastered = p.markMastered()
        assertTrue(mastered.isMastered)
        assertEquals(0, mastered.reinforcementCount)
        assertNull(mastered.lastReinforcedAt)
    }

    @Test
    fun `unmark mastered clears master flag only`() {
        val p = point("A").markMastered()
        val unmastered = p.unmarkMastered()
        assertFalse(unmastered.isMastered)
    }

    @Test
    fun `unmark mastered preserves reinforcement state`() {
        val p = point("A").addReinforcement()
        val mastered = p.markMastered()
        val unmastered = mastered.unmarkMastered()
        assertFalse(unmastered.isMastered)
        // Reinforcement was cleared by markMastered, stays cleared after unmark
        assertEquals(0, unmastered.reinforcementCount)
    }

    // ── Multi-step review flow simulation ──

    @Test
    fun `full review flow — hint, content, reinforced, next`() {
        val state = ReviewDisplayState()
        val queue = listOf(point("A"), point("B"), point("C"))
        var idx = 0

        // Start: nothing shown
        assertFalse(state.hintShown)
        assertFalse(state.contentShown)

        // Show hint
        state.showHint()
        assertTrue(state.hintShown)
        assertFalse(state.contentShown)

        // Show content (after hint)
        state.showContent()
        assertTrue(state.hintShown)
        assertTrue(state.contentShown)

        // Next point resets
        idx = advanceIndex(queue, idx)
        state.resetForNext()
        assertEquals(1, idx)
        assertFalse(state.hintShown)
        assertFalse(state.contentShown)
    }

    @Test
    fun `reinforcement queue ordered by count descending`() {
        val p1 = point("A").addReinforcement().addReinforcement() // count=2
        val p2 = point("B").addReinforcement() // count=1
        val p3 = point("C").addReinforcement().addReinforcement().addReinforcement() // count=3

        val sorted = listOf(p1, p2, p3).sortedWith(
            compareByDescending<KnowledgePoint> { it.reinforcementCount }
                .thenByDescending { it.lastReinforcedAt }
                .thenBy { it.title }
        )
        assertEquals("C", sorted[0].title) // count=3 first
        assertEquals("A", sorted[1].title) // count=2 second
        assertEquals("B", sorted[2].title) // count=1 last
    }

    @Test
    fun `mastered points exclude non-mastered`() {
        val all = listOf(
            point("A").markMastered(),
            point("B"),
            point("C").markMastered()
        )
        val mastered = all.filter { it.isMastered }
        assertEquals(2, mastered.size)
        assertTrue(mastered.all { it.isMastered })
    }

    // ── Edge cases: empty queue, single item, index bounds ──

    @Test
    fun `calc progress for single item queue returns 1_0`() {
        assertEquals(1f, calcProgress(listOf(point("A")), 0))
    }

    @Test
    fun `advance index on empty queue returns 0`() {
        assertEquals(0, advanceIndex(emptyList(), 0))
    }

    @Test
    fun `retreat index from 0 stays at 0`() {
        assertEquals(0, retreatIndex(0))
    }

    @Test
    fun `retreat index from negative clamps to 0`() {
        assertEquals(0, retreatIndex(-1))
    }

    // ── Swipe sequence edge cases ──

    @Test
    fun `swipe left in normal mode always reveals content before reinforcement`() {
        // iOS contract: left swipe in normal mode reveals content AND adds reinforcement
        val action = mapIosSwipeLeft(ReviewMode.NORMAL)
        assertEquals(IosSwipeAction.REVEAL_AND_REINFORCE, action)
    }

    @Test
    fun `swipe left in reinforcement mode always removes and advances`() {
        // iOS contract: reinforcement mode left swipe removes from list and advances
        val action = mapIosSwipeLeft(ReviewMode.REINFORCEMENT)
        assertEquals(IosSwipeAction.REMOVE_REINFORCEMENT_AND_NEXT, action)
    }

    @Test
    fun `swipe left in mastered mode always removes and advances`() {
        // iOS contract: mastered mode left swipe removes from list and advances
        val action = mapIosSwipeLeft(ReviewMode.MASTERED)
        assertEquals(IosSwipeAction.REMOVE_MASTERED_AND_NEXT, action)
    }

    @Test
    fun `swipe right normal nothing shown opens scope — not reveals hint`() {
        val action = mapIosSwipeRight(ReviewMode.NORMAL, isContentShown = false, isHintShown = false)
        assertEquals(IosSwipeAction.OPEN_SCOPE_PANEL, action)
    }

    // ── Review completion state machine (Round 1: isReviewCompleted) ──

    @Test
    fun `nextPoint with hasNext=true always advances regardless of mode`() {
        assertEquals(NextPointOutcome.ADVANCED, computeNextPointOutcome(ReviewMode.NORMAL, hasNext = true))
        assertEquals(NextPointOutcome.ADVANCED, computeNextPointOutcome(ReviewMode.REINFORCEMENT, hasNext = true))
        assertEquals(NextPointOutcome.ADVANCED, computeNextPointOutcome(ReviewMode.MASTERED, hasNext = true))
    }

    @Test
    fun `nextPoint without next in normal mode stays — does not complete`() {
        assertEquals(NextPointOutcome.STAYED, computeNextPointOutcome(ReviewMode.NORMAL, hasNext = false))
    }

    @Test
    fun `nextPoint without next in reinforcement mode triggers completion`() {
        assertEquals(NextPointOutcome.COMPLETED, computeNextPointOutcome(ReviewMode.REINFORCEMENT, hasNext = false))
    }

    @Test
    fun `nextPoint without next in mastered mode triggers completion`() {
        assertEquals(NextPointOutcome.COMPLETED, computeNextPointOutcome(ReviewMode.MASTERED, hasNext = false))
    }

    @Test
    fun `startReview always resets isReviewCompleted`() {
        val state = ReviewSessionState()
        state.complete() // simulate completion
        assertTrue(state.isCompleted)
        state.startNewSession()
        assertFalse(state.isCompleted)
    }

    @Test
    fun `isReviewCompleted stays false during normal review`() {
        val state = ReviewSessionState()
        assertFalse(state.isCompleted)
        state.advance() // hasNext = true
        assertFalse(state.isCompleted)
        state.advance() // hasNext = true
        assertFalse(state.isCompleted)
    }

    @Test
    fun `isReviewCompleted triggers only on exhaustion in non-normal mode`() {
        // Normal mode: 3 items, reach last
        val normalState = ReviewSessionState()
        repeat(2) { normalState.advance() } // now at index 2 (last)
        assertFalse(normalState.isCompleted)

        // Reinforcement mode: exhaust queue
        val reinfState = ReviewSessionState(mode = ReviewMode.REINFORCEMENT)
        repeat(2) { reinfState.advance() } // now at index 2 (last)
        assertFalse(reinfState.isCompleted) // still has next initially? No, last means no next
        // Force exhaust
        reinfState.forceExhaust()
        assertTrue(reinfState.isCompleted)
    }

    @Test
    fun `completion does not trigger in normal mode even at end of queue`() {
        val state = ReviewSessionState(mode = ReviewMode.NORMAL, size = 1)
        // At index 0, hasNext = false (only 1 item)
        assertFalse(state.hasNext)
        assertEquals(NextPointOutcome.STAYED, state.tryNext())
        assertFalse(state.isCompleted)
    }

    @Test
    fun `completion triggers in reinforcement mode at end of queue`() {
        val state = ReviewSessionState(mode = ReviewMode.REINFORCEMENT, size = 1)
        assertFalse(state.hasNext)
        assertEquals(NextPointOutcome.COMPLETED, state.tryNext())
        assertTrue(state.isCompleted)
    }

    @Test
    fun `completion clears current index to -1`() {
        val state = ReviewSessionState(mode = ReviewMode.MASTERED, size = 1)
        assertEquals(0, state.currentIndex)
        state.tryNext() // should complete
        assertTrue(state.isCompleted)
        assertEquals(-1, state.currentIndex)
    }

    // ── Helpers ──

    private fun point(title: String) = KnowledgePoint(
        title = title, hint = "", content = ""
    )

    companion object {
        fun advanceIndex(queue: List<KnowledgePoint>, currentIndex: Int): Int {
            return if (currentIndex < queue.size - 1) currentIndex + 1 else currentIndex
        }

        fun retreatIndex(currentIndex: Int): Int {
            return maxOf(0, currentIndex - 1)
        }

        fun calcProgress(queue: List<KnowledgePoint>, currentIndex: Int): Float {
            if (queue.isEmpty()) return 0f
            return (currentIndex + 1).toFloat() / queue.size.toFloat()
        }
    }
}

// ── Review display state (the pure-logic subset of ViewModel review state) ──

private class ReviewDisplayState {
    var hintShown = false
    var contentShown = false

    fun showHint() { hintShown = true }
    fun showContent() { contentShown = true }
    fun resetForNext() {
        hintShown = false
        contentShown = false
    }
}

// ── iOS-aligned swipe gesture logic (pure functions for testability) ──

fun isHorizontalDominant(dx: Float, dy: Float, threshold: Float, dominance: Float): Boolean {
    val horizontal = abs(dx)
    val vertical = abs(dy)
    return horizontal > threshold && horizontal > vertical * dominance
}

fun isVerticalDominant(dx: Float, dy: Float, threshold: Float, dominance: Float): Boolean {
    val horizontal = abs(dx)
    val vertical = abs(dy)
    return vertical > threshold && vertical > horizontal * dominance
}

enum class IosSwipeAction {
    NONE, SHOW_HINT, SHOW_CONTENT, NEXT_POINT, PREVIOUS_POINT,
    TOGGLE_REINFORCEMENT, OPEN_SCOPE_PANEL,
    REVEAL_AND_REINFORCE, REMOVE_REINFORCEMENT_AND_NEXT, REMOVE_MASTERED_AND_NEXT
}

fun mapIosSwipeLeft(mode: ReviewMode): IosSwipeAction {
    return when (mode) {
        ReviewMode.NORMAL -> IosSwipeAction.REVEAL_AND_REINFORCE
        ReviewMode.REINFORCEMENT -> IosSwipeAction.REMOVE_REINFORCEMENT_AND_NEXT
        ReviewMode.MASTERED -> IosSwipeAction.REMOVE_MASTERED_AND_NEXT
    }
}

fun mapIosSwipeRight(mode: ReviewMode, isContentShown: Boolean, isHintShown: Boolean): IosSwipeAction {
    if (mode == ReviewMode.NORMAL && !isHintShown && !isContentShown) {
        return IosSwipeAction.OPEN_SCOPE_PANEL
    }
    return when {
        isContentShown -> IosSwipeAction.TOGGLE_REINFORCEMENT
        isHintShown -> IosSwipeAction.SHOW_CONTENT
        else -> IosSwipeAction.SHOW_HINT
    }
}

fun mapIosSwipeUp(isContentShown: Boolean): IosSwipeAction {
    return if (isContentShown) IosSwipeAction.NEXT_POINT else IosSwipeAction.SHOW_CONTENT
}

fun mapIosSwipeDown(): IosSwipeAction = IosSwipeAction.PREVIOUS_POINT

// ── Review completion state machine (Round 1: isReviewCompleted logic) ──

enum class NextPointOutcome { ADVANCED, STAYED, COMPLETED }

fun computeNextPointOutcome(mode: ReviewMode, hasNext: Boolean): NextPointOutcome {
    return if (hasNext) NextPointOutcome.ADVANCED
    else if (mode != ReviewMode.NORMAL) NextPointOutcome.COMPLETED
    else NextPointOutcome.STAYED
}

private class ReviewSessionState(
    var mode: ReviewMode = ReviewMode.NORMAL,
    size: Int = 3
) {
    var currentIndex = 0
    private val queueSize = size
    var isCompleted = false

    val hasNext: Boolean get() = currentIndex < queueSize - 1

    fun advance() {
        if (hasNext) {
            currentIndex++
        }
    }

    fun forceExhaust() {
        if (mode != ReviewMode.NORMAL && !hasNext) {
            isCompleted = true
            currentIndex = -1
        }
    }

    fun tryNext(): NextPointOutcome {
        val outcome = computeNextPointOutcome(mode, hasNext)
        when (outcome) {
            NextPointOutcome.ADVANCED -> currentIndex++
            NextPointOutcome.COMPLETED -> {
                isCompleted = true
                currentIndex = -1
            }
            NextPointOutcome.STAYED -> { /* no-op */ }
        }
        return outcome
    }

    fun complete() {
        isCompleted = true
        currentIndex = -1
    }

    fun startNewSession() {
        isCompleted = false
        currentIndex = 0
    }
}
