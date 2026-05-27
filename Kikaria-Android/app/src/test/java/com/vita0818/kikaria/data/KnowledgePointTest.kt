package com.vita0818.kikaria.data

import org.junit.Assert.*
import org.junit.Test
import java.util.Date

class KnowledgePointTest {

    @Test
    fun constructorDefaults() {
        val point = KnowledgePoint(
            title = "Test",
            tags = listOf("Tag1"),
            hint = "Hint",
            content = "Content"
        )
        assertNotNull(point.id)
        assertEquals("Test", point.title)
        assertEquals(0, point.reinforcementCount)
        assertFalse(point.isReinforced)
        assertFalse(point.isMastered)
        assertNull(point.lastReinforcedAt)
    }

    @Test
    fun addReinforcementIncrementsCount() {
        val point = KnowledgePoint(
            title = "Test",
            hint = "H",
            content = "C"
        )
        val result1 = point.addReinforcement()
        assertEquals(1, result1.reinforcementCount)
        assertTrue(result1.isReinforced)
        assertNotNull(result1.lastReinforcedAt)

        val result2 = result1.addReinforcement()
        assertEquals(2, result2.reinforcementCount)
        assertTrue(result2.isReinforced)
    }

    @Test
    fun clearReinforcementResetsToZero() {
        val point = KnowledgePoint(
            title = "Test",
            hint = "H",
            content = "C",
            reinforcementCount = 3
        )
        assertTrue(point.isReinforced)

        val cleared = point.clearReinforcement()
        assertEquals(0, cleared.reinforcementCount)
        assertFalse(cleared.isReinforced)
        assertNull(cleared.lastReinforcedAt)
    }

    @Test
    fun markMasteredClearsReinforcement() {
        val point = KnowledgePoint(
            title = "Test",
            hint = "H",
            content = "C",
            reinforcementCount = 2
        )
        val mastered = point.markMastered()
        assertTrue(mastered.isMastered)
        assertEquals(0, mastered.reinforcementCount)
        assertFalse(mastered.isReinforced)
        assertNull(mastered.lastReinforcedAt)
    }

    @Test
    fun unmarkMasteredPreservesReinforcement() {
        val point = KnowledgePoint(
            title = "Test",
            hint = "H",
            content = "C",
            isMastered = true
        )
        val unmastered = point.unmarkMastered()
        assertFalse(unmastered.isMastered)
        // Reinforcement state from before should not be affected
        assertEquals(point.reinforcementCount, unmastered.reinforcementCount)
    }

    @Test
    fun isReinforcedDerivedFromCount() {
        assertEquals(false, KnowledgePoint(title = "T", hint = "H", content = "C", reinforcementCount = 0).isReinforced)
        assertEquals(true, KnowledgePoint(title = "T", hint = "H", content = "C", reinforcementCount = 1).isReinforced)
        assertEquals(true, KnowledgePoint(title = "T", hint = "H", content = "C", reinforcementCount = 5).isReinforced)
    }

    @Test
    fun updatedAtChangesOnMutation() {
        val before = Date(System.currentTimeMillis() - 10000)
        val point = KnowledgePoint(
            title = "Test",
            hint = "H",
            content = "C",
            updatedAt = before
        )
        val reinforced = point.addReinforcement()
        assertTrue(reinforced.updatedAt.after(before))
    }

    @Test
    fun copyPreservesId() {
        val point = KnowledgePoint(
            title = "Original",
            hint = "H",
            content = "C"
        )
        val updated = point.copy(title = "Updated")
        assertEquals(point.id, updated.id)
        assertEquals("Updated", updated.title)
    }
}
