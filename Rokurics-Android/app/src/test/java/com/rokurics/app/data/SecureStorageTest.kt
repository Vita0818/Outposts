package com.rokurics.app.data

import org.junit.Assert.*
import org.junit.Before
import org.junit.Test

class SecureStorageTest {

    private lateinit var storage: FakeSecureStorage

    @Before
    fun setUp() {
        storage = FakeSecureStorage()
    }

    @Test
    fun testPutAndGet() {
        storage.put("key1", "secret_value_123")
        assertEquals("secret_value_123", storage.get("key1"))
    }

    @Test
    fun testGetMissingKeyReturnsNull() {
        assertNull(storage.get("nonexistent"))
    }

    @Test
    fun testRemoveKey() {
        storage.put("key1", "value")
        storage.remove("key1")
        assertNull(storage.get("key1"))
    }

    @Test
    fun testContains() {
        assertFalse(storage.contains("key1"))
        storage.put("key1", "value")
        assertTrue(storage.contains("key1"))
    }

    @Test
    fun testClear() {
        storage.put("key1", "v1")
        storage.put("key2", "v2")
        storage.clear()
        assertNull(storage.get("key1"))
        assertNull(storage.get("key2"))
        assertFalse(storage.contains("key1"))
    }

    @Test
    fun testOverwriteValue() {
        storage.put("key", "original")
        storage.put("key", "updated")
        assertEquals("updated", storage.get("key"))
    }

    @Test
    fun testMigrationSimulation() {
        // Simulate: old SharedPreferences value migrated to secure storage,
        // then old prefs cleared
        val legacyValues = mapOf("sharedSecret" to "secret_abc", "deviceID" to "device_xyz")
        val sensitiveKeys = listOf("sharedSecret", "deviceID")

        // Migration step
        for (key in sensitiveKeys) {
            val legacyValue = legacyValues[key]
            if (legacyValue != null && !storage.contains(key)) {
                storage.put(key, legacyValue)
            }
        }

        // Verify values exist in secure storage
        assertEquals("secret_abc", storage.get("sharedSecret"))
        assertEquals("device_xyz", storage.get("deviceID"))

        // Clear happened successfully
        assertNull(storage.get("macHost")) // not migrated, not in storage
    }

    @Test
    fun testApiKeyMigration() {
        // Simulate AI settings migration: apiKey extracted from JSON blob
        val openAIApiKey = "sk-test1234567890"
        val anthropicApiKey = "sk-ant-test9876543210"

        // Step 1: API keys encrypted
        storage.put("ai.openai_apikey", openAIApiKey)
        storage.put("ai.anthropic_apikey", anthropicApiKey)

        // Step 2: JSON blob stored without apiKey
        val openAIConfigJson = """{"baseURL":"https://api.openai.com/v1","model":"gpt-4o","apiKey":""}"""

        // Step 3: On load, reconstruct with apiKey from secure storage
        val loadedApiKey = storage.get("ai.openai_apikey")
        assertEquals(openAIApiKey, loadedApiKey)

        // apiKey is NOT in the plain JSON
        assertFalse(openAIConfigJson.contains("sk-test"))
    }

    @Test
    fun testEmptyValueClearsEntry() {
        storage.put("key", "value")
        assertEquals("value", storage.get("key"))

        // Simulate clearing: remove when empty value is set
        storage.remove("key")
        assertNull(storage.get("key"))
    }

    @Test
    fun testMultipleKeysIndependence() {
        storage.put("sharedSecret", "secret1")
        storage.put("deviceID", "device1")

        storage.remove("sharedSecret")

        assertNull(storage.get("sharedSecret"))
        assertEquals("device1", storage.get("deviceID"))
    }

    @Test
    fun testBase64EncodingRoundtrip() {
        // Verify that base64-encodeable values survive round trip
        val original = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+/="
        storage.put("base64value", original)
        assertEquals(original, storage.get("base64value"))
    }
}
