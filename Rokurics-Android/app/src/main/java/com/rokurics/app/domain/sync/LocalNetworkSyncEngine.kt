package com.rokurics.app.domain.sync

import android.content.Context
import android.util.Base64
import com.rokurics.app.RokuricsApp
import com.rokurics.app.data.*
import com.rokurics.app.domain.model.*
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.ByteArrayOutputStream
import java.io.File
import java.util.UUID

class LocalNetworkSyncEngine(
    private val context: Context = RokuricsApp.instance
) {
    private val inventoryBuilder = LocalNetworkSyncInventoryBuilder(context)
    private val diffPlanner = LocalNetworkSyncDiffPlanner()
    private val syncClient = SecureUploadClient()
    private val connectionStore = ConnectionStore(context)
    private val studyLibraryStore = StudyLibraryStore(context)
    private val syncStateStore = LocalNetworkSyncStateStore(context)
    private val artifactChunkBytes = 2 * 1024 * 1024

    private var isSyncing = false

    private val platform = "Android"

    data class SyncTickResult(
        val diffPlan: LocalNetworkSyncDiffPlan? = null,
        val statusText: String = "",
        val success: Boolean = false,
        val error: String? = null,
        val syncRunID: String? = null
    )

    suspend fun performTick(trigger: String): SyncTickResult = withContext(Dispatchers.IO) {
        if (isSyncing) {
            return@withContext SyncTickResult(statusText = "同步进行中，跳过", success = false)
        }

        val syncState = syncStateStore.load()
        if (!syncState.isSyncAllowed) {
            val remaining = syncState.backoffRemainingSeconds
            return@withContext SyncTickResult(
                statusText = "退避中 · ${remaining}s 后可重试",
                success = false
            )
        }

        val settings = connectionStore.snapshot
        if (!settings.isPaired) {
            return@withContext SyncTickResult(statusText = "未配对，无法同步", success = false)
        }

        isSyncing = true
        val syncRunID = makeSyncRunId(settings.deviceID)
        var negotiatedRunID = syncRunID
        syncStateStore.recordControlPlane(syncRunID, LocalNetworkSyncControlPlaneState.SYNC_START_SIGNAL_SENT)

        try {
            val localInventory = inventoryBuilder.buildInventory(
                deviceID = settings.deviceID,
                deviceName = android.os.Build.MODEL
            )

            val startRequest = SecureUploadClient.SyncStartRequest(
                syncRunID = syncRunID,
                deviceID = settings.deviceID,
                platform = platform,
                reason = trigger
            )
            val startResponse = syncClient.sendLocalNetworkSyncStart(settings, startRequest)
                .getOrNull()
                ?: throw Exception("start_sync_request_failed")
            if (!startResponse.ok) {
                throw Exception(startResponse.error ?: "peer_rejected_sync_start")
            }

            negotiatedRunID = startResponse.syncRunID ?: syncRunID
            syncStateStore.recordControlPlane(negotiatedRunID, LocalNetworkSyncControlPlaneState.SYNC_START_SIGNAL_RECEIVED)
            syncStateStore.recordControlPlane(negotiatedRunID, LocalNetworkSyncControlPlaneState.SYNC_START_ACKED)

            val startAckRequest = SecureUploadClient.SyncStartAckRequest(
                syncRunID = negotiatedRunID,
                deviceID = settings.deviceID,
                platform = platform
            )
            val startAckResponse = syncClient.sendLocalNetworkSyncStartAck(settings, startAckRequest)
                .getOrNull()
                ?: throw Exception("start_sync_ack_request_failed")
            if (!startAckResponse.ok) {
                throw Exception(startAckResponse.error ?: "peer_rejected_sync_ack")
            }

            syncStateStore.recordControlPlane(negotiatedRunID, LocalNetworkSyncControlPlaneState.INVENTORY_EXCHANGING)

            val inventoryResponse = syncClient.fetchLocalNetworkSyncInventory(
                settings = settings,
                localInventoryHash = localInventory.inventoryHash,
                syncRunID = negotiatedRunID
            ).getOrNull()
                ?: throw Exception("inventory_request_failed")

            if (!inventoryResponse.ok || inventoryResponse.inventory == null) {
                throw Exception(inventoryResponse.error ?: "peer_inventory_invalid")
            }

            val peerInventory = inventoryResponse.inventory
            val localInventoryHash = localInventory.inventoryHash
            val peerInventoryHash = peerInventory.inventoryHash
            val lastSuccessfulSyncAt = syncState.lastSuccessfulSyncAt

            val plan = diffPlanner.plan(
                local = localInventory,
                peer = peerInventory,
                lastSuccessfulSyncAt = lastSuccessfulSyncAt
            )

            val uploadsPlanned = plan.uploadMetadataActions.size +
                plan.uploadArtifactActions.size +
                plan.uploadRecordingAudioActions.size
            val downloadsPlanned = plan.downloadMetadataActions.size + plan.downloadArtifactActions.size

            syncStateStore.recordAttempt(
                localDeviceID = settings.deviceID,
                peerDeviceID = peerInventory.device.deviceID,
                localInventoryHash = localInventoryHash,
                peerInventoryHash = peerInventoryHash,
                pendingUploadCount = uploadsPlanned,
                pendingDownloadCount = downloadsPlanned,
                planSummary = plan.summary,
                conflictCount = plan.conflictActions.size
            )

            syncStateStore.recordControlPlane(negotiatedRunID, LocalNetworkSyncControlPlaneState.PLANNING_TRANSFERS)

            var transferProgress = buildTransferProgress(plan)
            syncStateStore.recordActiveTransfers(transferProgress)
            syncStateStore.recordControlPlane(negotiatedRunID, LocalNetworkSyncControlPlaneState.TRANSFER_JOBS_CREATED)

            applyPeerRecordingStatuses(peerInventory)

            if (peerInventory.studyManifest != null) {
                try {
                    studyLibraryStore.applySyncManifest(peerInventory.studyManifest, settings.deviceID)
                } catch (_: Exception) {
                    // do not fail the whole run on partial metadata merge
                }
            }

            syncStateStore.recordControlPlane(negotiatedRunID, LocalNetworkSyncControlPlaneState.TRANSFERRING)
            val transferFailures = mutableListOf<String>()

            for (action in plan.uploadMetadataActions) {
                transferProgress = markTransferState(
                    transferProgress,
                    action,
                    LocalNetworkSyncTransferState.TRANSFERRING,
                    "上传元数据中"
                )
                syncStateStore.recordActiveTransfers(transferProgress)

                // upload of metadata is represented by manifest exchange
                transferProgress = markTransferState(
                    transferProgress,
                    action,
                    LocalNetworkSyncTransferState.COMPLETE,
                    "元数据计划同步"
                )
            }

            if (plan.uploadMetadataActions.isNotEmpty()) {
                try {
                    val localManifest = studyLibraryStore.makeSyncManifest(settings.deviceID)
                    val applyResponse = syncClient.applyLocalNetworkSyncMetadata(settings, localManifest).getOrNull()
                    if (applyResponse?.ok != true) {
                        transferFailures.add(applyResponse?.error ?: "apply_metadata_failed")
                    }
                } catch (e: Exception) {
                    transferFailures.add(e.message ?: "apply_metadata_failed")
                }
            }

            syncStateStore.recordActiveTransfers(transferProgress)

            for (action in plan.uploadArtifactActions) {
                val artifact = localInventory.artifacts.find { it.artifactID == action.entityID }
                val ok = artifact?.let { uploadArtifact(settings, it, negotiatedRunID) } ?: false
                val state = if (ok) LocalNetworkSyncTransferState.COMPLETE else LocalNetworkSyncTransferState.FAILED
                transferProgress = markTransferState(
                    transferProgress,
                    action,
                    state,
                    if (ok) "上传完成" else "上传失败"
                )
                syncStateStore.recordActiveTransfers(transferProgress)
                if (!ok) transferFailures.add("upload_${action.entityID}")
            }

            for (action in plan.uploadRecordingAudioActions) {
                val itemID = localInventory.studyItems
                    .firstOrNull { it.recordingID == action.entityID }
                    ?.itemID
                    ?: action.entityID
                val artifact = localInventory.artifacts.firstOrNull {
                    it.kind == LocalNetworkSyncArtifactKind.AUDIO && it.ownerID == itemID
                }
                val ok = artifact?.let { uploadArtifact(settings, it, negotiatedRunID) } ?: false
                val state = if (ok) LocalNetworkSyncTransferState.COMPLETE else LocalNetworkSyncTransferState.FAILED
                transferProgress = markTransferState(
                    transferProgress,
                    action,
                    state,
                    if (ok) "音频上传完成" else "音频上传失败"
                )
                syncStateStore.recordActiveTransfers(transferProgress)
                if (!ok) transferFailures.add("upload_audio_${action.entityID}")
            }

            for (action in plan.downloadMetadataActions) {
                transferProgress = markTransferState(
                    transferProgress,
                    action,
                    LocalNetworkSyncTransferState.TRANSFERRING,
                    "等待元数据更新"
                )
                syncStateStore.recordActiveTransfers(transferProgress)
                transferProgress = markTransferState(
                    transferProgress,
                    action,
                    LocalNetworkSyncTransferState.COMPLETE,
                    "元数据已同步"
                )
            }

            for (action in plan.downloadArtifactActions) {
                val artifact = peerInventory.artifacts.find { it.artifactID == action.entityID }
                transferProgress = markTransferState(
                    transferProgress,
                    action,
                    LocalNetworkSyncTransferState.TRANSFERRING,
                    "下载中"
                )
                syncStateStore.recordActiveTransfers(transferProgress)

                val ok = artifact?.let { downloadArtifact(settings, it, negotiatedRunID) } ?: false
                val state = if (ok) LocalNetworkSyncTransferState.COMPLETE else LocalNetworkSyncTransferState.FAILED
                transferProgress = markTransferState(
                    transferProgress,
                    action,
                    state,
                    if (ok) "下载完成" else "下载失败"
                )
                syncStateStore.recordActiveTransfers(transferProgress)
                if (!ok) transferFailures.add("download_${action.entityID}")
            }

            syncStateStore.recordActiveTransfers(transferProgress.filter { it.state == LocalNetworkSyncTransferState.TRANSFERRING })

            val refreshedInventory = inventoryBuilder.buildInventory(settings.deviceID, android.os.Build.MODEL)
            val finalSyncCompleted = transferFailures.isEmpty()

            syncStateStore.recordSuccess(
                peerDeviceID = peerInventory.device.deviceID,
                localInventoryHash = refreshedInventory.inventoryHash,
                peerInventoryHash = peerInventoryHash,
                appliedPeerRevision = peerInventory.inventoryRevision,
                pendingUploadCount = if (finalSyncCompleted) 0 else transferProgress.count { it.state == LocalNetworkSyncTransferState.PENDING },
                pendingDownloadCount = if (finalSyncCompleted) 0 else transferProgress.count { it.state == LocalNetworkSyncTransferState.PENDING }
            )
            syncStateStore.recordControlPlane(
                negotiatedRunID,
                if (finalSyncCompleted) LocalNetworkSyncControlPlaneState.COMPLETED else LocalNetworkSyncControlPlaneState.FAILED
            )
            syncStateStore.recordActiveTransfers(emptyList())

            if (!finalSyncCompleted) {
                throw Exception(transferFailures.joinToString(", "))
            }

            SyncTickResult(
                diffPlan = plan,
                statusText = plan.summary,
                success = true,
                syncRunID = negotiatedRunID
            )
        } catch (e: Exception) {
            syncStateStore.recordFailure("sync_error", e.message ?: "unknown")
            syncStateStore.recordActiveTransfers(emptyList())
            syncStateStore.recordControlPlane(negotiatedRunID, LocalNetworkSyncControlPlaneState.FAILED)
            SyncTickResult(
                statusText = "同步失败: ${e.message}",
                success = false,
                error = e.message,
                syncRunID = negotiatedRunID
            )
        } finally {
            isSyncing = false
        }
    }

    private fun makeSyncRunId(deviceID: String): String {
        val suffix = UUID.randomUUID().toString().replace("-", "").take(10)
        return "run_${deviceID.take(8)}_${System.currentTimeMillis()}_${suffix}"
    }

    private fun buildTransferProgress(plan: LocalNetworkSyncDiffPlan): List<LocalNetworkSyncTransferProgress> {
        val actions = mutableListOf<LocalNetworkSyncDiffAction>().apply {
            addAll(plan.uploadMetadataActions)
            addAll(plan.uploadArtifactActions)
            addAll(plan.downloadMetadataActions)
            addAll(plan.downloadArtifactActions)
            addAll(plan.uploadRecordingAudioActions)
        }

        return actions
            .map { action ->
                val objectKind = actionKindToObjectKind(action)
                LocalNetworkSyncTransferProgress(
                    objectID = action.entityID,
                    objectKind = objectKind,
                    state = LocalNetworkTransferState.PENDING,
                    progressFraction = 0.0,
                    receivedBytes = 0L,
                    totalBytes = 0L,
                    sourceDeviceID = null,
                    statusText = "排队"
                )
            }
            .filter { it.objectID.isNotBlank() }
            .distinctBy {
                val key = it.objectKind.ifBlank { "metadata" }
                "${it.objectID}:$key"
            }
    }

    private fun actionKindToObjectKind(action: LocalNetworkSyncDiffAction): String {
        return when (action.kind) {
            LocalNetworkSyncDiffActionKind.UPLOAD_RECORDING_AUDIO -> "recordingAudio"
            else -> action.entityKind
        }
    }

    private fun markTransferState(
        current: List<LocalNetworkSyncTransferProgress>,
        action: LocalNetworkSyncDiffAction,
        state: LocalNetworkSyncTransferState,
        statusText: String
    ): List<LocalNetworkSyncTransferProgress> {
        return current.map { transfer ->
            if (transfer.objectID == action.entityID && transfer.objectKind == actionKindToObjectKind(action)) {
                transfer.copy(state = state, statusText = statusText)
            } else {
                transfer
            }
        }
    }

    private fun applyPeerRecordingStatuses(peerInventory: LocalNetworkSyncInventory) {
        val recordings = AudioFileStore(context).loadAllMetadata(includeDeleted = true)
        for (peerRec in peerInventory.recordings) {
            if (peerRec.receiveStatus == "completed") {
                val local = recordings.find { it.id == peerRec.recordingID } ?: continue
                if (local.uploadStatus != "uploaded") {
                    AudioFileStore(context).updateMetadata(
                        local.copy(
                            uploadStatus = "uploaded",
                            uploadProgressDescription = "已在对端确认接收"
                        )
                    )
                }
            }
        }
    }

    private suspend fun uploadArtifact(
        settings: SecureMacConnectionSnapshot,
        artifact: LocalNetworkSyncArtifactEntry,
        syncRunID: String
    ): Boolean {
        val pathToken = artifact.logicalPathToken ?: return false
        if (!isSafeRelativePath(pathToken)) return false
        val sourceFile = File(context.filesDir, "Rokurics/study/$pathToken")
        if (!sourceFile.exists()) return false

        val bytes = sourceFile.readBytes()
        if (bytes.isEmpty()) return false

        val checksum = SecureUploadUtilities.sha256Hex(bytes)
        val totalSize = bytes.size.toLong()

        val status = SecureUploadClient.SyncArtifactStatusRequest(
            artifactID = artifact.artifactID,
            kind = artifact.kind.rawValue,
            ownerID = artifact.ownerID,
            logicalPathToken = artifact.logicalPathToken,
            checksum = checksum,
            size = totalSize,
            syncRunID = syncRunID
        )
        val statusResponse = syncClient.fetchLocalNetworkSyncArtifactStatus(settings, status).getOrNull()
        if (statusResponse?.ok == true && statusResponse.state.equals("complete", ignoreCase = true) && statusResponse.size == totalSize) {
            return true
        }

        val resumedOffset = if (statusResponse?.ok == true) {
            val nextOffset = statusResponse.nextOffset ?: 0L
            if (nextOffset > 0L && (statusResponse.size == null || statusResponse.size == totalSize)) {
                nextOffset
            } else {
                0L
            }
        } else {
            0L
        }

        var offset = if (resumedOffset in 0..totalSize) resumedOffset else 0L
        while (offset < totalSize) {
            val remaining = totalSize - offset
            val currentChunkSize = remaining.coerceAtMost(artifactChunkBytes.toLong()).toInt()
            val chunk = bytes.copyOfRange(offset.toInt(), (offset + currentChunkSize).toInt())
            if (chunk.isEmpty()) return false

            val request = SecureUploadClient.SyncArtifactPutRequest(
                artifactID = artifact.artifactID,
                kind = artifact.kind.rawValue,
                ownerID = artifact.ownerID,
                checksum = checksum,
                size = totalSize,
                updatedAt = artifact.updatedAt,
                logicalPathToken = artifact.logicalPathToken,
                dataBase64 = Base64.encodeToString(chunk, Base64.NO_WRAP),
                offset = offset,
                chunkSize = currentChunkSize,
                totalSize = totalSize,
                isFinalChunk = (offset + currentChunkSize >= totalSize),
                syncRunID = syncRunID
            )

            val result = syncClient.putLocalNetworkSyncArtifact(settings, request).getOrNull() ?: return false
            if (!result.ok) return false

            val confirmed = result.confirmedBytes ?: (offset + currentChunkSize)
            if (confirmed <= offset || confirmed > totalSize) return false
            offset = confirmed
        }

        return true
    }

    private suspend fun downloadArtifact(
        settings: SecureMacConnectionSnapshot,
        artifact: LocalNetworkSyncArtifactEntry,
        syncRunID: String
    ): Boolean {
        val targetTokenFromArtifact = artifact.logicalPathToken

        var offset = 0L
        var expectedSize: Long? = null
        var lastResponse: SecureUploadClient.SyncArtifactResponse? = null
        val chunks = ByteArrayOutputStream()
        while (true) {
            val request = SecureUploadClient.SyncArtifactRequest(
                artifactID = artifact.artifactID,
                offset = offset,
                length = artifactChunkBytes,
                syncRunID = syncRunID
            )
            val response = syncClient.requestLocalNetworkSyncArtifact(settings, request).getOrNull()
                ?: return false
            lastResponse = response
            if (!response.ok) return false

            if (response.offset != null && response.offset != offset) {
                return false
            }

            val chunk = response.dataBase64 ?: return false
            val bytes = Base64.decode(chunk, Base64.DEFAULT)
            if (bytes.isEmpty()) return false
            if (offset + bytes.size > Int.MAX_VALUE.toLong() && response.totalSize == null) return false
            chunks.write(bytes)

            val nextOffset = response.nextOffset ?: (offset + bytes.size)
            if (nextOffset <= offset) return false
            offset = nextOffset

            if (response.totalSize != null) {
                expectedSize = response.totalSize
                if (offset > response.totalSize) return false
            }

            val shouldStop = response.isFinalChunk == true || response.nextOffset == null
            if (shouldStop) break
        }

        val fileBytes = chunks.toByteArray()
        if (expectedSize != null && fileBytes.size.toLong() != expectedSize) {
            return false
        }

        val checksum = SecureUploadUtilities.sha256Hex(fileBytes)

        if (!expectedChecksumMatches(artifact.checksum, checksum)) {
            return false
        }

        val targetPathToken = targetTokenFromArtifact ?: lastResponse?.logicalPathToken
        val targetPath = targetPathToken ?: return false
        if (!isSafeRelativePath(targetPath)) return false

        val targetFile = File(context.filesDir, "Rokurics/study/$targetPath")
        targetFile.parentFile?.mkdirs()
        targetFile.writeBytes(fileBytes)

        if (artifact.kind != LocalNetworkSyncArtifactKind.AUDIO) {
            applyDownloadedArtifactToStudyItem(artifact.kind, artifact.ownerID, targetPath)
        }
        return true
    }

    private fun expectedChecksumMatches(expected: String?, actual: String): Boolean {
        val expectedChecksum = expected ?: return true
        return expected == actual
    }

    private fun applyDownloadedArtifactToStudyItem(
        kind: LocalNetworkSyncArtifactKind,
        ownerID: String,
        pathToken: String
    ) {
        val targetItem = studyLibraryStore.itemByRecordingID(ownerID)
            ?: studyLibraryStore.allStudyItems.find { it.itemID == ownerID }
            ?: return

        val nextItem = when (kind) {
            LocalNetworkSyncArtifactKind.TRANSCRIPT_MARKDOWN -> targetItem.copy(
                transcriptMarkdownRelativePath = pathToken
            )
            LocalNetworkSyncArtifactKind.TRANSCRIPT_JSON -> targetItem.copy(
                transcriptRelativePath = pathToken
            )
            LocalNetworkSyncArtifactKind.NOTE_MARKDOWN -> targetItem.copy(
                noteRelativePath = pathToken
            )
            LocalNetworkSyncArtifactKind.NOTE_JSON -> targetItem.copy(
                noteRelativePath = pathToken
            )
            LocalNetworkSyncArtifactKind.SUMMARY_MARKDOWN -> targetItem.copy(
                summaryMarkdownRelativePath = pathToken
            )
            LocalNetworkSyncArtifactKind.SUMMARY_JSON -> targetItem.copy(
                summaryJSONRelativePath = pathToken
            )
            LocalNetworkSyncArtifactKind.RECEIVE_JSON -> targetItem.copy(
                receiveRelativePath = pathToken
            )
            else -> targetItem
        }

        if (nextItem != targetItem) {
            try {
                studyLibraryStore.save(nextItem)
            } catch (_: Exception) {
            }
        }
    }

    private fun isSafeRelativePath(path: String?): Boolean {
        if (path.isNullOrBlank()) return false
        if (path.contains("..")) return false
        if (path.startsWith("/")) return false
        if (path.startsWith("~")) return false
        return true
    }

    fun isCurrentlySyncing(): Boolean = isSyncing
}
