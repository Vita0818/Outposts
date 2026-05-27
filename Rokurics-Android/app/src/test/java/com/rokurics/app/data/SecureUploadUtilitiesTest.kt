package com.rokurics.app.data

import org.junit.Assert.*
import org.junit.Test
import java.io.File

class SecureUploadUtilitiesTest {

    @Test
    fun sha256Hex_consistentOutput() {
        val input = "hello world".toByteArray()
        val hash1 = SecureUploadUtilities.sha256Hex(input)
        val hash2 = SecureUploadUtilities.sha256Hex(input)
        assertEquals(64, hash1.length)
        assertEquals(hash1, hash2)
    }

    @Test
    fun sha256Hex_differentInputsProduceDifferentHashes() {
        val hash1 = SecureUploadUtilities.sha256Hex("hello".toByteArray())
        val hash2 = SecureUploadUtilities.sha256Hex("world".toByteArray())
        assertNotEquals(hash1, hash2)
    }

    @Test
    fun hmacSHA256Base64URL_validSecretProducesSignature() {
        val secret = SecureUploadUtilities.randomBase64URLToken()
        val signature = SecureUploadUtilities.hmacSHA256Base64URL("test message", secret)
        assertNotNull(signature)
        assertTrue(signature!!.isNotEmpty())
    }

    @Test
    fun hmacSHA256Base64URL_invalidSecretProducesNull() {
        val signature = SecureUploadUtilities.hmacSHA256Base64URL("test", "not-valid-base64!!!")
        assertNull(signature)
    }

    @Test
    fun hmacSHA256Base64URL_sameInputProducesSameResult() {
        val secret = SecureUploadUtilities.randomBase64URLToken()
        val sig1 = SecureUploadUtilities.hmacSHA256Base64URL("payload", secret)
        val sig2 = SecureUploadUtilities.hmacSHA256Base64URL("payload", secret)
        assertEquals(sig1, sig2)
    }

    @Test
    fun hmacSHA256Base64URL_differentPayloadsProduceDifferentResults() {
        val secret = SecureUploadUtilities.randomBase64URLToken()
        val sig1 = SecureUploadUtilities.hmacSHA256Base64URL("payload1", secret)
        val sig2 = SecureUploadUtilities.hmacSHA256Base64URL("payload2", secret)
        assertNotEquals(sig1, sig2)
    }

    @Test
    fun randomBase64URLToken_generatesUniqueTokens() {
        val token1 = SecureUploadUtilities.randomBase64URLToken()
        val token2 = SecureUploadUtilities.randomBase64URLToken()
        assertNotEquals(token1, token2)
        assertTrue(token1.isNotEmpty())
        assertTrue(token2.isNotEmpty())
    }

    @Test
    fun normalizedCertificateFingerprint_lowercaseToUppercase() {
        val result = SecureUploadUtilities.normalizedCertificateFingerprint("abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890")
        assertEquals("ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890", result)
    }

    @Test
    fun normalizedCertificateFingerprint_removesNonHex() {
        val result = SecureUploadUtilities.normalizedCertificateFingerprint("AB:CD:EF:12 34-56_78")
        assertEquals("ABCDEF12345678", result)
    }

    @Test
    fun sha256Hex_fileHashing() {
        // Create temp file in build directory which is always writable
        val file = File("build/tmp/test_sha256.txt")
        file.parentFile?.mkdirs()
        file.writeText("test content for file hashing")
        try {
            val hash = SecureUploadUtilities.sha256Hex(file)
            assertEquals(64, hash.length)
        } finally {
            file.delete()
        }
    }
}
