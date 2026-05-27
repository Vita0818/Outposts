package com.rokurics.app.data

import android.content.Context
import android.content.SharedPreferences
import com.rokurics.app.RokuricsApp
import com.rokurics.app.domain.model.SecureMacConnectionSnapshot
import com.rokurics.app.domain.model.SecurePairingResult

class ConnectionStore(
    context: Context = RokuricsApp.instance,
    private val secureStorage: SecureStorage = KeystoreSecureStorage(context)
) {
    private val plainPrefs: SharedPreferences =
        context.getSharedPreferences("rokurics_connection", Context.MODE_PRIVATE)

    // Legacy SharedPreferences (migrated on first use)
    private val legacySecurePrefs: SharedPreferences =
        context.getSharedPreferences("rokurics_secure_connection", Context.MODE_PRIVATE)

    init {
        migrateIfNeeded()
    }

    private fun migrateIfNeeded() {
        if (!plainPrefs.getBoolean(MIGRATED_KEY, false)) {
            // Migrate sensitive values from old SharedPreferences to encrypted storage
            for (key in listOf(KEY_SHARED_SECRET, KEY_DEVICE_ID, KEY_MAC_FINGERPRINT)) {
                val legacyValue = legacySecurePrefs.getString(key, null)
                if (legacyValue != null && legacyValue.isNotEmpty() && !secureStorage.contains(key)) {
                    secureStorage.put(key, legacyValue)
                }
            }
            plainPrefs.edit().putBoolean(MIGRATED_KEY, true).apply()
            // Clear legacy secure prefs after successful migration
            legacySecurePrefs.edit().clear().apply()
        }
    }

    // ── Non-sensitive (plain SharedPreferences) ──────────────

    var macHost: String
        get() = plainPrefs.getString(KEY_MAC_HOST, "") ?: ""
        set(value) = plainPrefs.edit().putString(KEY_MAC_HOST, value).apply()

    var macPort: Int
        get() = plainPrefs.getInt(KEY_MAC_PORT, 8787)
        set(value) = plainPrefs.edit().putInt(KEY_MAC_PORT, value).apply()

    var macName: String
        get() = plainPrefs.getString(KEY_MAC_NAME, "") ?: ""
        set(value) = plainPrefs.edit().putString(KEY_MAC_NAME, value).apply()

    var macModel: String
        get() = plainPrefs.getString(KEY_MAC_MODEL, "") ?: ""
        set(value) = plainPrefs.edit().putString(KEY_MAC_MODEL, value).apply()

    var pairedAt: String
        get() = plainPrefs.getString(KEY_PAIRED_AT, "") ?: ""
        set(value) = plainPrefs.edit().putString(KEY_PAIRED_AT, value).apply()

    // ── Sensitive (encrypted storage) ────────────────────────

    var macFingerprint: String
        get() = secureStorage.get(KEY_MAC_FINGERPRINT) ?: ""
        set(value) {
            if (value.isEmpty()) secureStorage.remove(KEY_MAC_FINGERPRINT)
            else secureStorage.put(KEY_MAC_FINGERPRINT, value)
        }

    var deviceID: String
        get() = secureStorage.get(KEY_DEVICE_ID) ?: ""
        set(value) {
            if (value.isEmpty()) secureStorage.remove(KEY_DEVICE_ID)
            else secureStorage.put(KEY_DEVICE_ID, value)
        }

    var sharedSecret: String
        get() = secureStorage.get(KEY_SHARED_SECRET) ?: ""
        set(value) {
            if (value.isEmpty()) secureStorage.remove(KEY_SHARED_SECRET)
            else secureStorage.put(KEY_SHARED_SECRET, value)
        }

    // ── Snapshot ─────────────────────────────────────────────

    val snapshot: SecureMacConnectionSnapshot
        get() = SecureMacConnectionSnapshot(
            macHost = macHost.cleanHost(),
            macPort = macPort,
            macFingerprint = SecureUploadUtilities.normalizedCertificateFingerprint(macFingerprint),
            macName = macName,
            macModel = macModel,
            deviceID = deviceID,
            sharedSecretBase64URL = sharedSecret,
            pairedAt = pairedAt
        )

    val isPaired: Boolean get() = snapshot.isPaired

    fun savePairing(result: SecurePairingResult, host: String, port: Int, fingerprint: String) {
        macHost = host.cleanHost()
        macPort = port
        macFingerprint = SecureUploadUtilities.normalizedCertificateFingerprint(fingerprint)
        macName = result.macName
        macModel = result.macModel
        deviceID = result.deviceID
        sharedSecret = result.sharedSecretBase64URL
        pairedAt = result.pairedAt
    }

    fun clearPairing() {
        secureStorage.remove(KEY_SHARED_SECRET)
        secureStorage.remove(KEY_DEVICE_ID)
        secureStorage.remove(KEY_MAC_FINGERPRINT)
        plainPrefs.edit().clear().apply()
    }

    companion object {
        private const val KEY_MAC_HOST = "macHost"
        private const val KEY_MAC_PORT = "macPort"
        private const val KEY_MAC_FINGERPRINT = "macFingerprint"
        private const val KEY_MAC_NAME = "macName"
        private const val KEY_MAC_MODEL = "macModel"
        private const val KEY_DEVICE_ID = "deviceID"
        private const val KEY_SHARED_SECRET = "sharedSecret"
        private const val KEY_PAIRED_AT = "pairedAt"
        private const val MIGRATED_KEY = "_secure_migrated_v1"
    }
}

private fun String.cleanHost(): String = this
    .trim()
    .removePrefix("https://")
    .removePrefix("http://")
    .split("/").first()
    .split(":").first()
