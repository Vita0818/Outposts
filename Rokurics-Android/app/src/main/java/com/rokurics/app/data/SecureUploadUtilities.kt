package com.rokurics.app.data

import java.io.File
import java.io.FileInputStream
import java.security.MessageDigest
import java.security.SecureRandom
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec
import java.util.Base64

object SecureUploadUtilities {
    private val secureRandom = SecureRandom()

    fun randomBase64URLToken(byteCount: Int = 16): String {
        val bytes = ByteArray(byteCount)
        secureRandom.nextBytes(bytes)
        return Base64.getUrlEncoder().withoutPadding().encodeToString(bytes)
    }

    fun sha256Hex(data: ByteArray): String {
        val digest = MessageDigest.getInstance("SHA-256")
        return digest.digest(data).joinToString("") { "%02x".format(it) }
    }

    fun sha256Hex(file: File): String {
        val digest = MessageDigest.getInstance("SHA-256")
        FileInputStream(file).use { input ->
            val buffer = ByteArray(1024 * 1024)
            var bytesRead: Int
            while (input.read(buffer).also { bytesRead = it } != -1) {
                digest.update(buffer, 0, bytesRead)
            }
        }
        return digest.digest().joinToString("") { "%02x".format(it) }
    }

    fun hmacSHA256Base64URL(message: String, secretBase64URL: String): String? {
        return try {
            val secretBytes = Base64.getUrlDecoder().decode(secretBase64URL)
            val keySpec = SecretKeySpec(secretBytes, "HmacSHA256")
            val mac = Mac.getInstance("HmacSHA256")
            mac.init(keySpec)
            val signature = mac.doFinal(message.toByteArray(Charsets.UTF_8))
            Base64.getUrlEncoder().withoutPadding().encodeToString(signature)
        } catch (e: Exception) {
            null
        }
    }

    fun normalizedCertificateFingerprint(value: String): String {
        return value.uppercase().filter { it in '0'..'9' || it in 'A'..'F' }
    }

    fun sha256Hex(data: String): String {
        return sha256Hex(data.toByteArray(Charsets.UTF_8))
    }
}
