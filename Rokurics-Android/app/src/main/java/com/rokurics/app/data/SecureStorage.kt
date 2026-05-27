package com.rokurics.app.data

import android.content.Context
import android.content.SharedPreferences
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

// ── Interface ───────────────────────────────────────────────────

interface SecureStorage {
    fun put(key: String, value: String)
    fun get(key: String): String?
    fun remove(key: String)
    fun clear()
    fun contains(key: String): Boolean
}

// ── Android Keystore AES-GCM implementation ─────────────────────

class KeystoreSecureStorage(
    private val context: android.content.Context,
    private val alias: String = "rokurics_secure_storage"
) : SecureStorage {

    private val prefs: SharedPreferences =
        context.getSharedPreferences("rokurics_encrypted", Context.MODE_PRIVATE)

    private val keyStore: KeyStore = KeyStore.getInstance("AndroidKeyStore").apply {
        load(null)
    }

    init {
        ensureKey()
    }

    override fun put(key: String, value: String) {
        val cipher = Cipher.getInstance(TRANSFORMATION)
        cipher.init(Cipher.ENCRYPT_MODE, getOrCreateKey())
        val iv = cipher.iv
        val encrypted = cipher.doFinal(value.toByteArray(Charsets.UTF_8))
        val encoded = Base64.encodeToString(iv, Base64.NO_WRAP) + ":" +
                Base64.encodeToString(encrypted, Base64.NO_WRAP)
        prefs.edit().putString(key, encoded).apply()
    }

    override fun get(key: String): String? {
        val encoded = prefs.getString(key, null) ?: return null
        val parts = encoded.split(":", limit = 2)
        if (parts.size != 2) return null
        return try {
            val iv = Base64.decode(parts[0], Base64.NO_WRAP)
            val encrypted = Base64.decode(parts[1], Base64.NO_WRAP)
            val cipher = Cipher.getInstance(TRANSFORMATION)
            cipher.init(Cipher.DECRYPT_MODE, getOrCreateKey(), GCMParameterSpec(128, iv))
            String(cipher.doFinal(encrypted), Charsets.UTF_8)
        } catch (_: Exception) {
            null
        }
    }

    override fun remove(key: String) {
        prefs.edit().remove(key).apply()
    }

    override fun clear() {
        prefs.edit().clear().apply()
        // Also delete the keystore key to force regeneration
        try { keyStore.deleteEntry(alias) } catch (_: Exception) {}
    }

    override fun contains(key: String): Boolean {
        return prefs.contains(key)
    }

    private fun ensureKey() {
        if (!keyStore.containsAlias(alias)) {
            createKey()
        }
    }

    private fun getOrCreateKey(): SecretKey {
        if (!keyStore.containsAlias(alias)) createKey()
        return (keyStore.getEntry(alias, null) as KeyStore.SecretKeyEntry).secretKey
    }

    private fun createKey() {
        val generator = KeyGenerator.getInstance(
            KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore"
        )
        generator.init(
            KeyGenParameterSpec.Builder(
                alias,
                KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT
            )
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setKeySize(256)
                .build()
        )
        generator.generateKey()
    }

    companion object {
        private const val TRANSFORMATION = "AES/GCM/NoPadding"
    }
}

// ── Fake implementation for tests ────────────────────────────────

class FakeSecureStorage : SecureStorage {
    private val store = mutableMapOf<String, String>()

    override fun put(key: String, value: String) {
        store[key] = value
    }

    override fun get(key: String): String? = store[key]

    override fun remove(key: String) {
        store.remove(key)
    }

    override fun clear() {
        store.clear()
    }

    override fun contains(key: String): Boolean = store.containsKey(key)
}
