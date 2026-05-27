package com.rokurics.app.domain.model

data class SecureMacConnectionSnapshot(
    val macHost: String = "",
    val macPort: Int = 0,
    val macFingerprint: String = "",
    val macName: String = "",
    val macModel: String = "",
    val deviceID: String = "",
    val sharedSecretBase64URL: String = "",
    val pairedAt: String = ""
) {
    val isPaired: Boolean
        get() = macHost.isNotEmpty() && macPort > 0 && macFingerprint.isNotEmpty()
                && deviceID.isNotEmpty() && sharedSecretBase64URL.isNotEmpty()
}

data class SecurePairingResult(
    val deviceID: String,
    val sharedSecretBase64URL: String,
    val pairedAt: String,
    val macName: String = "",
    val macModel: String = ""
)

data class SecureUploadServerResponse(
    val ok: Boolean = false,
    val message: String? = null,
    val disposition: String? = null,
    val fileName: String? = null,
    val recordingID: String? = null,
    val metadataFileName: String? = null,
    val audioFileName: String? = null,
    val receiveStatus: String? = null,
    val processingStatus: String? = null,
    val error: String? = null,
    val reason: String? = null
)

data class RokuricsPairingInfo(
    val host: String = "",
    val portText: String = "",
    val pairingCode: String = "",
    val fingerprint: String = ""
)
