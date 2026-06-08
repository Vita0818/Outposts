package com.rokurics.app.data

import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.rokurics.app.domain.model.LocalNetworkSyncInventory
import com.rokurics.app.domain.model.CanonicalManifest
import com.rokurics.app.domain.model.CanonicalManifestNode
import com.rokurics.app.domain.model.SecureMacConnectionSnapshot
import com.rokurics.app.domain.model.SecurePairingResult
import com.rokurics.app.domain.model.SecureUploadServerResponse
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.File
import javax.net.ssl.HttpsURLConnection
import java.net.URL
import java.util.Date

class SecureUploadClient {

    suspend fun healthCheck(
        host: String,
        port: Int,
        macFingerprint: String,
        certificateFingerprint: String? = macFingerprint
    ): Result<Boolean> = withContext(Dispatchers.IO) {
        try {
            val url = secureUrl(host, port, "/health")
            val connection = URL(url).openConnection() as HttpsURLConnection
            if (certificateFingerprint != null) {
                CertificatePinner(certificateFingerprint).applyTo(connection)
            }
            connection.requestMethod = "GET"
            connection.connectTimeout = 15_000
            connection.readTimeout = 60_000
            val code = connection.responseCode
            connection.disconnect()
            if (code in 200..299) Result.success(true)
            else Result.failure(Exception("Health check failed: $code"))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun pair(
        host: String,
        port: Int,
        pairingCode: String,
        macFingerprint: String,
        deviceName: String,
        deviceType: String = "Android",
        certificateFingerprint: String? = macFingerprint
    ): Result<SecurePairingResult> = withContext(Dispatchers.IO) {
        try {
            val url = secureUrl(host, port, "/pair")
            val payload = JSONObject().apply {
                put("pairingCode", pairingCode.trim())
                put("deviceName", deviceName)
                put("deviceType", deviceType)
            }
            val responseBody = httpPost(url, payload.toString().toByteArray(), "application/json", certificateFingerprint)
            val json = JSONObject(responseBody)

            if (json.optBoolean("ok", false)) {
                Result.success(SecurePairingResult(
                    deviceID = json.optString("deviceID", ""),
                    sharedSecretBase64URL = json.optString("sharedSecret", ""),
                    pairedAt = json.optString("pairedAt", ""),
                    macName = json.optString("macName", json.optString("macDisplayName", "")),
                    macModel = json.optString("macModel", json.optString("macDeviceModel", ""))
                ))
            } else {
                Result.failure(Exception(json.optString("error", "pairing_failed")))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun uploadSignedData(
        settings: SecureMacConnectionSnapshot,
        path: String,
        body: ByteArray,
        contentType: String,
        uploadType: String,
        recordingID: String,
        fileName: String
    ): Result<SecureUploadServerResponse> = withContext(Dispatchers.IO) {
        try {
            val now = Date()
            val bodySHA256 = SecureUploadUtilities.sha256Hex(body)
            val timestamp = String.format("%.0f", now.time / 1000.0)
            val nonce = SecureUploadUtilities.randomBase64URLToken()
            val signaturePayload = "POST\n$path\n$timestamp\n$nonce\n$bodySHA256"
            val signature = SecureUploadUtilities.hmacSHA256Base64URL(
                signaturePayload, settings.sharedSecretBase64URL
            ) ?: return@withContext Result.failure(Exception("invalid_secret"))

            val url = secureUrl(settings.macHost, settings.macPort, path)
            val headers = mapOf(
                "Content-Type" to contentType,
                "X-Rokurics-Device-ID" to settings.deviceID,
                "X-Rokurics-Timestamp" to timestamp,
                "X-Rokurics-Nonce" to nonce,
                "X-Rokurics-Body-SHA256" to bodySHA256,
                "X-Rokurics-Signature" to signature,
                "X-Rokurics-Recording-ID" to recordingID,
                "X-Rokurics-Filename" to fileName,
                "X-Rokurics-Upload-Type" to uploadType
            )

            val responseBody = httpPost(url, body, headers, settings.macFingerprint)
            val json = JSONObject(responseBody)
            val result = SecureUploadServerResponse(
                ok = json.optBoolean("ok", false),
                message = json.optString("message", "").ifEmpty { null },
                disposition = json.optString("disposition", "").ifEmpty { null },
                fileName = json.optString("fileName", "").ifEmpty { null },
                recordingID = json.optString("recordingID", "").ifEmpty { null },
                metadataFileName = json.optString("metadataFileName", "").ifEmpty { null },
                audioFileName = json.optString("audioFileName", "").ifEmpty { null },
                receiveStatus = json.optString("receiveStatus", "").ifEmpty { null },
                processingStatus = json.optString("processingStatus", "").ifEmpty { null },
                error = json.optString("error", "").ifEmpty { null },
                reason = json.optString("reason", "").ifEmpty { null }
            )
            Result.success(result)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun uploadSignedFile(
        settings: SecureMacConnectionSnapshot,
        path: String,
        file: File,
        contentType: String,
        uploadType: String,
        recordingID: String,
        fileName: String
    ): Result<SecureUploadServerResponse> = withContext(Dispatchers.IO) {
        try {
            val bodyBytes = file.readBytes()
            uploadSignedData(settings, path, bodyBytes, contentType, uploadType, recordingID, fileName)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    // ── Local Network Sync Endpoints ──────────────────────────────────

    data class SyncInventoryRequest(
        val deviceID: String,
        val generatedAt: Long = System.currentTimeMillis(),
        val localInventoryHash: String? = null,
        val syncRunID: String? = null
    )

    data class SyncStartRequest(
        val syncRunID: String,
        val deviceID: String,
        val platform: String,
        val requestedAt: Long = System.currentTimeMillis(),
        val reason: String = "manual"
    )

    data class SyncStartResponse(
        val ok: Boolean = false,
        val syncRunID: String? = null,
        val peerDeviceID: String? = null,
        val ackAt: Long? = null,
        val disposition: String? = null,
        val error: String? = null
    )

    data class SyncStartAckRequest(
        val syncRunID: String,
        val deviceID: String,
        val platform: String,
        val acknowledgedAt: Long = System.currentTimeMillis(),
        val disposition: String = "ok"
    )

    data class SyncStartAckResponse(
        val ok: Boolean = false,
        val syncRunID: String? = null,
        val peerDeviceID: String? = null,
        val ackReceivedAt: Long? = null,
        val error: String? = null
    )

    data class SyncInventoryResponse(
        val ok: Boolean = false,
        val inventory: LocalNetworkSyncInventory? = null,
        val error: String? = null
    )

    data class SyncManifestRequest(
        val manifest: com.rokurics.app.domain.model.StudyLibrarySyncManifest
    )

    data class SyncManifestResponse(
        val ok: Boolean = false,
        val manifest: com.rokurics.app.domain.model.StudyLibrarySyncManifest? = null,
        val applyResult: com.rokurics.app.domain.model.StudyLibrarySyncApplyResult? = null,
        val error: String? = null
    )

    data class SyncArtifactRequest(
        val artifactID: String,
        val offset: Long? = null,
        val length: Int? = null,
        val syncRunID: String? = null
    )

    data class SyncArtifactResponse(
        val ok: Boolean = false,
        val artifactID: String? = null,
        val kind: String? = null,
        val checksum: String? = null,
        val size: Long? = null,
        val logicalPathToken: String? = null,
        val dataBase64: String? = null,
        val offset: Long? = null,
        val nextOffset: Long? = null,
        val totalSize: Long? = null,
        val isFinalChunk: Boolean? = null,
        val error: String? = null
    )

    data class SyncArtifactStatusRequest(
        val artifactID: String,
        val kind: String? = null,
        val ownerID: String? = null,
        val logicalPathToken: String? = null,
        val checksum: String? = null,
        val size: Long? = null,
        val syncRunID: String? = null
    )

    data class SyncArtifactStatusResponse(
        val ok: Boolean = false,
        val artifactID: String? = null,
        val checksum: String? = null,
        val size: Long? = null,
        val confirmedBytes: Long? = null,
        val nextOffset: Long? = null,
        val state: String? = null,
        val error: String? = null
    )

    data class SyncArtifactPutRequest(
        val artifactID: String,
        val kind: String,
        val ownerID: String,
        val checksum: String,
        val size: Long,
        val updatedAt: Long,
        val logicalPathToken: String,
        val dataBase64: String,
        val offset: Long? = null,
        val chunkSize: Int? = null,
        val totalSize: Long? = null,
        val isFinalChunk: Boolean? = null,
        val syncRunID: String? = null
    )

    data class SyncArtifactPutResponse(
        val ok: Boolean = false,
        val artifactID: String? = null,
        val disposition: String? = null,
        val checksum: String? = null,
        val size: Long? = null,
        val confirmedBytes: Long? = null,
        val error: String? = null
    )

    suspend fun sendLocalNetworkSyncStart(
        settings: SecureMacConnectionSnapshot,
        request: SyncStartRequest
    ): Result<SyncStartResponse> = withContext(Dispatchers.IO) {
        try {
            val json = postSignedJSON(settings, "/sync/start", request, 5_000, 8_000)
            Result.success(SyncStartResponse(
                ok = json.optBoolean("ok", false),
                syncRunID = json.optString("syncRunID", "").ifEmpty { null },
                peerDeviceID = json.optString("peerDeviceID", "").ifEmpty { null },
                ackAt = if (json.has("ackAt")) json.optLong("ackAt") else null,
                disposition = json.optString("disposition", "").ifEmpty { null },
                error = json.optString("error", "").ifEmpty { null }
            ))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun sendLocalNetworkSyncStartAck(
        settings: SecureMacConnectionSnapshot,
        request: SyncStartAckRequest
    ): Result<SyncStartAckResponse> = withContext(Dispatchers.IO) {
        try {
            val json = postSignedJSON(settings, "/sync/start-ack", request, 5_000, 8_000)
            Result.success(SyncStartAckResponse(
                ok = json.optBoolean("ok", false),
                syncRunID = json.optString("syncRunID", "").ifEmpty { null },
                peerDeviceID = json.optString("peerDeviceID", "").ifEmpty { null },
                ackReceivedAt = if (json.has("ackReceivedAt")) json.optLong("ackReceivedAt") else null,
                error = json.optString("error", "").ifEmpty { null }
            ))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun fetchLocalNetworkSyncInventory(
        settings: SecureMacConnectionSnapshot,
        localInventoryHash: String?,
        syncRunID: String? = null
    ): Result<SyncInventoryResponse> = withContext(Dispatchers.IO) {
        try {
            val body = SyncInventoryRequest(
                deviceID = settings.deviceID,
                generatedAt = System.currentTimeMillis(),
                localInventoryHash = localInventoryHash,
                syncRunID = syncRunID
            )
            val json = postSignedJSON(settings, "/sync/inventory", body, 10_000, 20_000)
            val inventory = if (json.has("inventory") && !json.isNull("inventory")) {
                parseInventory(json.getJSONObject("inventory"))
            } else null
            Result.success(SyncInventoryResponse(
                ok = json.optBoolean("ok", false),
                inventory = inventory,
                error = json.optString("error", "").ifEmpty { null }
            ))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun fetchLocalNetworkSyncArtifactStatus(
        settings: SecureMacConnectionSnapshot,
        request: SyncArtifactStatusRequest
    ): Result<SyncArtifactStatusResponse> = withContext(Dispatchers.IO) {
        try {
            val json = postSignedJSON(settings, "/sync/artifact-status", request, 10_000, 20_000)
            Result.success(SyncArtifactStatusResponse(
                ok = json.optBoolean("ok", false),
                artifactID = json.optString("artifactID", "").ifEmpty { null },
                checksum = json.optString("checksum", "").ifEmpty { null },
                size = if (json.has("size")) json.optLong("size") else null,
                confirmedBytes = if (json.has("confirmedBytes")) json.optLong("confirmedBytes") else null,
                nextOffset = if (json.has("nextOffset")) json.optLong("nextOffset") else null,
                state = json.optString("state", "").ifEmpty { null },
                error = json.optString("error", "").ifEmpty { null }
            ))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun putLocalNetworkSyncArtifact(
        settings: SecureMacConnectionSnapshot,
        request: SyncArtifactPutRequest
    ): Result<SyncArtifactPutResponse> = withContext(Dispatchers.IO) {
        try {
            val json = postSignedJSON(settings, "/sync/artifact-put", request, 15_000, 30_000)
            Result.success(SyncArtifactPutResponse(
                ok = json.optBoolean("ok", false),
                artifactID = json.optString("artifactID", "").ifEmpty { null },
                disposition = json.optString("disposition", "").ifEmpty { null },
                checksum = json.optString("checksum", "").ifEmpty { null },
                size = if (json.has("size")) json.optLong("size") else null,
                confirmedBytes = if (json.has("confirmedBytes")) json.optLong("confirmedBytes") else null,
                error = json.optString("error", "").ifEmpty { null }
            ))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun applyLocalNetworkSyncMetadata(
        settings: SecureMacConnectionSnapshot,
        manifest: com.rokurics.app.domain.model.StudyLibrarySyncManifest
    ): Result<SyncManifestResponse> = withContext(Dispatchers.IO) {
        try {
            val body = SyncManifestRequest(manifest = manifest)
            val json = postSignedJSON(settings, "/sync/apply-metadata", body, 15_000, 30_000)
            val responseManifest = if (json.has("manifest") && !json.isNull("manifest")) {
                parseManifest(json.getJSONObject("manifest"))
            } else null
            val applyResult = if (json.has("applyResult") && !json.isNull("applyResult")) {
                val ar = json.getJSONObject("applyResult")
                com.rokurics.app.domain.model.StudyLibrarySyncApplyResult(
                    appliedItemCount = ar.optInt("appliedItemCount", 0),
                    appliedFolderCount = ar.optInt("appliedFolderCount", 0),
                    tombstoneCount = ar.optInt("tombstoneCount", 0),
                    conflictCount = ar.optInt("conflictCount", 0),
                    skippedOlderCount = ar.optInt("skippedOlderCount", 0),
                    failedChanges = ar.optInt("failedChanges", 0)
                )
            } else null
            Result.success(SyncManifestResponse(
                ok = json.optBoolean("ok", false),
                manifest = responseManifest,
                applyResult = applyResult,
                error = json.optString("error", "").ifEmpty { null }
            ))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun requestLocalNetworkSyncArtifact(
        settings: SecureMacConnectionSnapshot,
        artifactID: String
    ): Result<SyncArtifactResponse> = withContext(Dispatchers.IO) {
        try {
            val body = SyncArtifactRequest(artifactID = artifactID)
            val json = postSignedJSON(settings, "/sync/artifact-request", body, 10_000, 30_000)
            Result.success(SyncArtifactResponse(
                ok = json.optBoolean("ok", false),
                artifactID = json.optString("artifactID", "").ifEmpty { null },
                kind = json.optString("kind", "").ifEmpty { null },
                checksum = json.optString("checksum", "").ifEmpty { null },
                size = if (json.has("size")) json.optLong("size") else null,
                logicalPathToken = json.optString("logicalPathToken", "").ifEmpty { null },
                offset = if (json.has("offset")) json.optLong("offset") else null,
                totalSize = if (json.has("totalSize")) json.optLong("totalSize") else null,
                isFinalChunk = json.optBoolean("isFinalChunk", false).takeIf { json.has("isFinalChunk") },
                nextOffset = if (json.has("nextOffset")) json.optLong("nextOffset") else null,
                dataBase64 = json.optString("dataBase64", "").ifEmpty { null },
                error = json.optString("error", "").ifEmpty { null }
            ))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun requestLocalNetworkSyncArtifact(
        settings: SecureMacConnectionSnapshot,
        request: SyncArtifactRequest
    ): Result<SyncArtifactResponse> = withContext(Dispatchers.IO) {
        try {
            val json = postSignedJSON(settings, "/sync/artifact-request", request, 10_000, 30_000)
            Result.success(SyncArtifactResponse(
                ok = json.optBoolean("ok", false),
                artifactID = json.optString("artifactID", "").ifEmpty { null },
                kind = json.optString("kind", "").ifEmpty { null },
                checksum = json.optString("checksum", "").ifEmpty { null },
                size = if (json.has("size")) json.optLong("size") else null,
                logicalPathToken = json.optString("logicalPathToken", "").ifEmpty { null },
                dataBase64 = json.optString("dataBase64", "").ifEmpty { null },
                offset = if (json.has("offset")) json.optLong("offset") else null,
                nextOffset = if (json.has("nextOffset")) json.optLong("nextOffset") else null,
                totalSize = if (json.has("totalSize")) json.optLong("totalSize") else null,
                isFinalChunk = json.optBoolean("isFinalChunk", false).takeIf { json.has("isFinalChunk") },
                error = json.optString("error", "").ifEmpty { null }
            ))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    // ── Signed JSON POST (HMAC-SHA256) ──────────────────────────────

    private inline fun <reified T> postSignedJSON(
        settings: SecureMacConnectionSnapshot,
        path: String,
        body: T,
        connectTimeoutMs: Int = 15_000,
        readTimeoutMs: Int = 60_000
    ): JSONObject {
        val gson = GsonBuilder().disableHtmlEscaping().create()
        val bodyJson = gson.toJson(body)
        val bodyBytes = bodyJson.toByteArray(Charsets.UTF_8)
        val now = Date()
        val bodySHA256 = SecureUploadUtilities.sha256Hex(bodyBytes)
        val timestamp = String.format("%.0f", now.time / 1000.0)
        val nonce = SecureUploadUtilities.randomBase64URLToken()
        val signaturePayload = "POST\n$path\n$timestamp\n$nonce\n$bodySHA256"
        val signature = SecureUploadUtilities.hmacSHA256Base64URL(
            signaturePayload, settings.sharedSecretBase64URL
        ) ?: throw IllegalStateException("invalid_secret")

        val url = secureUrl(settings.macHost, settings.macPort, path)
        val headers = mapOf(
            "Content-Type" to "application/json",
            "X-Rokurics-Device-ID" to settings.deviceID,
            "X-Rokurics-Timestamp" to timestamp,
            "X-Rokurics-Nonce" to nonce,
            "X-Rokurics-Body-SHA256" to bodySHA256,
            "X-Rokurics-Signature" to signature
        )

        val responseBody = httpPost(url, bodyBytes, headers, settings.macFingerprint)
        return JSONObject(responseBody)
    }

    // ── JSON parsing helpers ─────────────────────────────────────────

    private fun parseInventory(json: JSONObject): LocalNetworkSyncInventory {
        val gson = Gson()
        val deviceJson = json.getJSONObject("device")
            val device = com.rokurics.app.domain.model.LocalNetworkSyncDeviceSection(
                deviceID = deviceJson.optString("deviceID", ""),
                deviceName = deviceJson.optString("deviceName", ""),
                platform = com.rokurics.app.domain.model.LocalNetworkSyncPlatform.from(
                    deviceJson.optString("platform", "Mac")
                ),
                generatedAt = deviceJson.optLong("generatedAt", 0),
                lastKnownPeerRevision = deviceJson.optString("lastKnownPeerRevision", "").ifEmpty { null },
                appSchemaVersion = deviceJson.optInt("appSchemaVersion", 1)
            )

        fun parseRecordings(arr: org.json.JSONArray?): List<com.rokurics.app.domain.model.LocalNetworkSyncRecordingEntry> {
            if (arr == null) return emptyList()
            return (0 until arr.length()).map { i ->
                val r = arr.getJSONObject(i)
                com.rokurics.app.domain.model.LocalNetworkSyncRecordingEntry(
                    recordingID = r.optString("recordingID", ""),
                    metadataHash = r.optString("metadataHash", "").ifEmpty { null },
                    audioAvailable = r.optBoolean("audioAvailable", false),
                    audioChecksum = r.optString("audioChecksum", "").ifEmpty { null },
                    audioSize = if (r.has("audioSize") && !r.isNull("audioSize")) r.optLong("audioSize") else null,
                    uploadLedgerState = r.optString("uploadLedgerState", "").ifEmpty { null },
                    receiveStatus = r.optString("receiveStatus", "").ifEmpty { null },
                    processingStatus = r.optString("processingStatus", "").ifEmpty { null },
                    updatedAt = r.optLong("updatedAt", 0),
                    deleted = r.optBoolean("deleted", false)
                )
            }
        }

        fun parseFolders(arr: org.json.JSONArray?): List<com.rokurics.app.domain.model.LocalNetworkSyncFolderEntry> {
            if (arr == null) return emptyList()
            return (0 until arr.length()).map { i ->
                val f = arr.getJSONObject(i)
                com.rokurics.app.domain.model.LocalNetworkSyncFolderEntry(
                    folderID = f.optString("folderID", ""),
                    parentID = f.optString("parentID", "").ifEmpty { null },
                    path = f.optString("path", "").ifEmpty { null },
                    name = f.optString("name", ""),
                    colorToken = f.optString("colorToken", "").ifEmpty { null },
                    updatedAt = f.optLong("updatedAt", 0),
                    revisionHash = f.optString("revisionHash", "").ifEmpty { null },
                    deleted = f.optBoolean("deleted", false)
                )
            }
        }

        fun parseItems(arr: org.json.JSONArray?): List<com.rokurics.app.domain.model.LocalNetworkSyncStudyItemEntry> {
            if (arr == null) return emptyList()
            return (0 until arr.length()).map { i ->
                val si = arr.getJSONObject(i)
                val folderIDsArr = si.optJSONArray("folderIDs")
                val folderIDs = if (folderIDsArr != null) {
                    (0 until folderIDsArr.length()).map { folderIDsArr.getString(it) }
                } else emptyList()
                com.rokurics.app.domain.model.LocalNetworkSyncStudyItemEntry(
                    itemID = si.optString("itemID", ""),
                    kind = try {
                        com.rokurics.app.domain.model.StudyItemKind.valueOf(si.optString("kind", "RECORDING_BUNDLE").uppercase())
                    } catch (_: Exception) { com.rokurics.app.domain.model.StudyItemKind.RECORDING_BUNDLE },
                    title = si.optString("title", ""),
                    folderIDs = folderIDs,
                    recordingID = si.optString("recordingID", "").ifEmpty { null },
                    updatedAt = si.optLong("updatedAt", 0),
                    revisionHash = si.optString("revisionHash", "").ifEmpty { null },
                    deleted = si.optBoolean("deleted", false)
                )
            }
        }

        fun parseArtifacts(arr: org.json.JSONArray?): List<com.rokurics.app.domain.model.LocalNetworkSyncArtifactEntry> {
            if (arr == null) return emptyList()
            return (0 until arr.length()).map { i ->
                val a = arr.getJSONObject(i)
                com.rokurics.app.domain.model.LocalNetworkSyncArtifactEntry(
                    artifactID = a.optString("artifactID", ""),
                    kind = com.rokurics.app.domain.model.LocalNetworkSyncArtifactKind.from(
                        a.optString("kind", "audio")
                    ),
                    ownerID = a.optString("ownerID", ""),
                    checksum = a.optString("checksum", "").ifEmpty { null },
                    size = if (a.has("size") && !a.isNull("size")) a.optLong("size") else null,
                    updatedAt = a.optLong("updatedAt", 0),
                    availability = try {
                        com.rokurics.app.domain.model.LocalNetworkSyncArtifactAvailability.valueOf(
                            a.optString("availability", "local").uppercase()
                        )
                    } catch (_: Exception) { com.rokurics.app.domain.model.LocalNetworkSyncArtifactAvailability.LOCAL },
                    logicalPathToken = a.optString("logicalPathToken", "").ifEmpty { null }
                )
            }
        }

        fun parseCanonicalManifest(obj: org.json.JSONObject?): com.rokurics.app.domain.model.CanonicalManifest? {
            if (obj == null) return null
            val schemaVersion = obj.optInt("schemaVersion", 1)
            val generatedAt = if (obj.has("generatedAt") && !obj.isNull("generatedAt")) {
                obj.optLong("generatedAt", 0L)
            } else null
            val manifestHash = obj.optString("manifestHash", "").ifEmpty { null }
            val node = if (obj.has("node") && obj.optJSONObject("node") != null) {
                val nodeObj = obj.getJSONObject("node")
                com.rokurics.app.domain.model.CanonicalManifestNode(
                    nodeID = nodeObj.optString("nodeID", "").ifEmpty { null },
                    platform = nodeObj.optString("platform", "").ifEmpty { null },
                    displayName = nodeObj.optString("displayName", "").ifEmpty { null }
                )
            } else null
            return com.rokurics.app.domain.model.CanonicalManifest(
                node = node,
                payload = jsonToMap(obj),
                schemaVersion = schemaVersion,
                generatedAt = generatedAt,
                manifestHash = manifestHash
            )
        }

        return LocalNetworkSyncInventory(
            device = device,
            recordings = parseRecordings(json.optJSONArray("recordings")),
            folders = parseFolders(json.optJSONArray("folders")),
            studyItems = parseItems(json.optJSONArray("studyItems")),
            artifacts = parseArtifacts(json.optJSONArray("artifacts")),
            studyManifest = if (json.has("studyManifest") && !json.isNull("studyManifest")) {
                parseManifest(json.getJSONObject("studyManifest"))
            } else null,
            canonicalManifest = parseCanonicalManifest(
                if (json.has("canonicalManifest") && !json.isNull("canonicalManifest")) {
                    if (json.get("canonicalManifest") is String) {
                        org.json.JSONObject(json.getString("canonicalManifest"))
                    } else {
                        json.getJSONObject("canonicalManifest")
                    }
                } else null
            )
        )
    }

    private fun parseManifest(json: JSONObject): com.rokurics.app.domain.model.StudyLibrarySyncManifest {
        val gson = Gson()
        fun parseItems(arr: org.json.JSONArray?): List<com.rokurics.app.domain.model.StudyItemMetadata> {
            if (arr == null) return emptyList()
            return (0 until arr.length()).map { i ->
                gson.fromJson(arr.getJSONObject(i).toString(), com.rokurics.app.domain.model.StudyItemMetadata::class.java)
            }
        }
        fun parseFolders(arr: org.json.JSONArray?): List<com.rokurics.app.domain.model.StudyFolderMetadata> {
            if (arr == null) return emptyList()
            return (0 until arr.length()).map { i ->
                gson.fromJson(arr.getJSONObject(i).toString(), com.rokurics.app.domain.model.StudyFolderMetadata::class.java)
            }
        }
        fun parseTombstones(arr: org.json.JSONArray?): List<com.rokurics.app.domain.model.StudyLibrarySyncTombstone> {
            if (arr == null) return emptyList()
            return (0 until arr.length()).map { i ->
                val t = arr.getJSONObject(i)
                com.rokurics.app.domain.model.StudyLibrarySyncTombstone(
                    id = t.optString("id", ""),
                    entityKind = try {
                        com.rokurics.app.domain.model.StudyLibrarySyncEntityKind.valueOf(t.optString("entityKind", "ITEM").uppercase())
                    } catch (_: Exception) { com.rokurics.app.domain.model.StudyLibrarySyncEntityKind.ITEM },
                    entityID = t.optString("entityID", ""),
                    operation = try {
                        com.rokurics.app.domain.model.StudyLibrarySyncOperation.valueOf(t.optString("operation", "TRASH").uppercase())
                    } catch (_: Exception) { com.rokurics.app.domain.model.StudyLibrarySyncOperation.TRASH },
                    updatedAt = t.optLong("updatedAt", 0),
                    modifiedByDeviceID = t.optString("modifiedByDeviceID", "").ifEmpty { null }
                )
            }
        }
        fun parsePendingUploads(arr: org.json.JSONArray?): List<com.rokurics.app.domain.model.PendingRecordingUpload> {
            if (arr == null) return emptyList()
            return (0 until arr.length()).map { i ->
                val p = arr.getJSONObject(i)
                com.rokurics.app.domain.model.PendingRecordingUpload(
                    id = p.optString("id", ""),
                    itemID = p.optString("itemID", ""),
                    recordingID = p.optString("recordingID", ""),
                    localAudioRelativePath = p.optString("localAudioRelativePath", ""),
                    targetDeviceID = p.optString("targetDeviceID", ""),
                    status = try {
                        com.rokurics.app.domain.model.PendingRecordingUploadStatus.valueOf(p.optString("status", "PENDING").uppercase())
                    } catch (_: Exception) { com.rokurics.app.domain.model.PendingRecordingUploadStatus.PENDING },
                    createdAt = p.optLong("createdAt", 0),
                    updatedAt = p.optLong("updatedAt", 0),
                    lastAttemptAt = if (p.has("lastAttemptAt") && !p.isNull("lastAttemptAt")) p.optLong("lastAttemptAt") else null,
                    retryCount = p.optInt("retryCount", 0),
                    lastError = p.optString("lastError", "").ifEmpty { null }
                )
            }
        }

        fun parseRecordings(arr: org.json.JSONArray?): List<com.rokurics.app.domain.model.LocalNetworkSyncRecordingEntry> {
            if (arr == null) return emptyList()
            return (0 until arr.length()).map { i ->
                val r = arr.getJSONObject(i)
                com.rokurics.app.domain.model.LocalNetworkSyncRecordingEntry(
                    recordingID = r.optString("recordingID", ""),
                    metadataHash = r.optString("metadataHash", "").ifEmpty { null },
                    audioAvailable = r.optBoolean("audioAvailable", false),
                    audioChecksum = r.optString("audioChecksum", "").ifEmpty { null },
                    audioSize = if (r.has("audioSize") && !r.isNull("audioSize")) r.optLong("audioSize") else null,
                    uploadLedgerState = r.optString("uploadLedgerState", "").ifEmpty { null },
                    receiveStatus = r.optString("receiveStatus", "").ifEmpty { null },
                    processingStatus = r.optString("processingStatus", "").ifEmpty { null },
                    updatedAt = r.optLong("updatedAt", 0),
                    deleted = r.optBoolean("deleted", false)
                )
            }
        }

        return com.rokurics.app.domain.model.StudyLibrarySyncManifest(
            deviceID = json.optString("deviceID", ""),
            generatedAt = json.optLong("generatedAt", 0),
            libraryVersion = json.optInt("libraryVersion", 1),
            items = parseItems(json.optJSONArray("items")),
            recordings = parseRecordings(json.optJSONArray("recordings")),
            folders = parseFolders(json.optJSONArray("folders")),
            tombstones = parseTombstones(json.optJSONArray("tombstones")),
            pendingUploads = parsePendingUploads(json.optJSONArray("pendingUploads")),
            baseCommitID = json.optString("baseCommitID", "").ifEmpty { null },
            commitID = json.optString("commitID", "").ifEmpty { null },
            localManifestHash = json.optString("localManifestHash", "").ifEmpty { null }
        )
    }

    private fun jsonToMap(jsonObject: org.json.JSONObject): Map<String, Any?> {
        val iterator = jsonObject.keys()
        val entries = mutableMapOf<String, Any?>()
        while (iterator.hasNext()) {
            val key = iterator.next()
            entries[key] = toKotlinValue(jsonObject.get(key))
        }
        val sortedEntries = entries.toList().sortedBy { it.first }.toMap(LinkedHashMap())
        return sortedEntries
    }

    private fun toKotlinValue(value: Any?): Any? {
        return when (value) {
            is org.json.JSONObject -> jsonToMap(value)
            is org.json.JSONArray -> {
                val list = (0 until value.length()).map { idx -> toKotlinValue(value.get(idx)) }
                list
            }
            is JSONObject.NULL -> null
            else -> value
        }
    }

    // ── Resumable Upload Session Endpoints ────────────────────────────

    suspend fun startResumableUploadSession(
        settings: SecureMacConnectionSnapshot,
        request: com.rokurics.app.domain.model.ResumableAudioUploadStartRequest
    ): Result<com.rokurics.app.domain.model.ResumableAudioUploadSessionResponse> =
        withContext(Dispatchers.IO) {
            try {
                val json = postSignedJSON(
                    settings, "/upload-recording-audio-session/start",
                    request, 15_000, 30_000
                )
                Result.success(parseSessionResponse(json))
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun getResumableUploadSessionStatus(
        settings: SecureMacConnectionSnapshot,
        request: com.rokurics.app.domain.model.ResumableAudioUploadStatusRequest
    ): Result<com.rokurics.app.domain.model.ResumableAudioUploadSessionResponse> =
        withContext(Dispatchers.IO) {
            try {
                val json = postSignedJSON(
                    settings, "/upload-recording-audio-session/status",
                    request, 10_000, 20_000
                )
                Result.success(parseSessionResponse(json))
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun uploadResumableAudioChunk(
        settings: SecureMacConnectionSnapshot,
        recordingID: String,
        sessionID: String,
        chunk: ByteArray,
        offset: Long,
        totalSHA256: String
    ): Result<com.rokurics.app.domain.model.ResumableAudioUploadSessionResponse> =
        withContext(Dispatchers.IO) {
            try {
                val now = Date()
                val chunkSHA256 = SecureUploadUtilities.sha256Hex(chunk)
                val timestamp = String.format("%.0f", now.time / 1000.0)
                val nonce = SecureUploadUtilities.randomBase64URLToken()
                val signaturePayload = "POST\n/upload-recording-audio-session/chunk\n$timestamp\n$nonce\n$chunkSHA256"
                val signature = SecureUploadUtilities.hmacSHA256Base64URL(
                    signaturePayload, settings.sharedSecretBase64URL
                ) ?: return@withContext Result.failure(Exception("invalid_secret"))

                val url = secureUrl(settings.macHost, settings.macPort, "/upload-recording-audio-session/chunk")
                val headers = mapOf(
                    "Content-Type" to "application/octet-stream",
                    "X-Rokurics-Device-ID" to settings.deviceID,
                    "X-Rokurics-Timestamp" to timestamp,
                    "X-Rokurics-Nonce" to nonce,
                    "X-Rokurics-Body-SHA256" to chunkSHA256,
                    "X-Rokurics-Signature" to signature,
                    "X-Rokurics-Recording-ID" to recordingID,
                    "X-Rokurics-Filename" to "audio.m4a.part",
                    "X-Rokurics-Upload-Type" to "recording-audio-chunk",
                    "X-Rokurics-Session-ID" to sessionID,
                    "X-Rokurics-Chunk-Offset" to offset.toString(),
                    "X-Rokurics-Chunk-Length" to chunk.size.toString(),
                    "X-Rokurics-Chunk-SHA256" to chunkSHA256,
                    "X-Rokurics-Total-SHA256" to totalSHA256
                )

                val responseBody = httpPost(url, chunk, headers, settings.macFingerprint)
                val json = JSONObject(responseBody)
                Result.success(parseSessionResponse(json))
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun finalizeResumableUploadSession(
        settings: SecureMacConnectionSnapshot,
        request: com.rokurics.app.domain.model.ResumableAudioUploadFinalizeRequest
    ): Result<com.rokurics.app.domain.model.ResumableAudioUploadSessionResponse> =
        withContext(Dispatchers.IO) {
            try {
                val json = postSignedJSON(
                    settings, "/upload-recording-audio-session/finalize",
                    request, 15_000, 60_000
                )
                Result.success(parseSessionResponse(json))
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    private fun parseSessionResponse(json: JSONObject): com.rokurics.app.domain.model.ResumableAudioUploadSessionResponse {
        return com.rokurics.app.domain.model.ResumableAudioUploadSessionResponse(
            ok = json.optBoolean("ok", false),
            disposition = json.optString("disposition", "").ifEmpty { null },
            status = json.optString("status", "").ifEmpty { null },
            sessionID = json.optString("sessionID", "").ifEmpty { null },
            confirmedBytes = json.optLong("confirmedBytes", 0),
            nextOffset = json.optLong("nextOffset", 0),
            chunkSize = if (json.has("chunkSize") && !json.isNull("chunkSize")) json.optInt("chunkSize") else null,
            completed = json.optBoolean("completed", false),
            finalAudioExists = if (json.has("finalAudioExists") && !json.isNull("finalAudioExists")) json.optBoolean("finalAudioExists") else null,
            chunkAccepted = if (json.has("chunkAccepted") && !json.isNull("chunkAccepted")) json.optBoolean("chunkAccepted") else null,
            finalAudioRelativePath = json.optString("finalAudioRelativePath", "").ifEmpty { null },
            checksum = json.optString("checksum", "").ifEmpty { null },
            fileSize = if (json.has("fileSize") && !json.isNull("fileSize")) json.optLong("fileSize") else null,
            receiveStatus = json.optString("receiveStatus", "").ifEmpty { null },
            processingStatus = json.optString("processingStatus", "").ifEmpty { null },
            error = json.optString("error", "").ifEmpty { null },
            reason = json.optString("reason", "").ifEmpty { null }
        )
    }

    private fun secureUrl(host: String, port: Int, path: String): String =
        "https://$host:$port$path"

    private fun httpPost(url: String, body: ByteArray, headers: Map<String, String>, certificateFingerprint: String? = null): String {
        val connection = URL(url).openConnection() as HttpsURLConnection
        if (certificateFingerprint != null) {
            CertificatePinner(certificateFingerprint).applyTo(connection)
        }
        return try {
            connection.requestMethod = "POST"
            connection.doOutput = true
            connection.connectTimeout = 15_000
            connection.readTimeout = 60_000
            headers.forEach { (key, value) -> connection.setRequestProperty(key, value) }
            connection.outputStream.use { it.write(body) }
            val code = connection.responseCode
            val stream = if (code in 200..299) connection.inputStream else connection.errorStream
            stream?.bufferedReader()?.readText() ?: "{}"
        } finally {
            connection.disconnect()
        }
    }

    private fun httpPost(url: String, body: ByteArray, contentType: String, certificateFingerprint: String? = null): String {
        return httpPost(url, body, mapOf("Content-Type" to contentType), certificateFingerprint)
    }
}
