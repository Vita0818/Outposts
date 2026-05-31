package com.rokurics.app.domain.sync

import android.content.Context
import android.util.Base64
import com.rokurics.app.RokuricsApp
import com.rokurics.app.data.*
import com.rokurics.app.domain.model.*
import kotlinx.coroutines.*
import java.io.File

class LocalNetworkSyncEngine(
    private val context: Context = RokuricsApp.instance
) {
    private val inventoryBuilder = LocalNetworkSyncInventoryBuilder(context)
    private val diffPlanner = LocalNetworkSyncDiffPlanner()
    private val syncClient = SecureUploadClient()
    private val connectionStore = ConnectionStore(context)
    private val studyLibraryStore = StudyLibraryStore(context)
    private val syncStateStore = LocalNetworkSyncStateStore(context)
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    private var isSyncing = false

    data class SyncTickResult(
        val diffPlan: LocalNetworkSyncDiffPlan? = null,
        val statusText: String = "",
        val success: Boolean = false,
        val error: String? = null
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
        try {
            // 1. Build local inventory
            val localInventory = inventoryBuilder.buildInventory(settings.deviceID, android.os.Build.MODEL)

            // 2. Fetch peer inventory
            val fetchResult = syncClient.fetchLocalNetworkSyncInventory(settings, localInventory.inventoryHash)
            if (fetchResult.isFailure) {
                val error = fetchResult.exceptionOrNull()?.message ?: "unknown"
                syncStateStore.recordFailure("network_error", error)
                return@withContext SyncTickResult(statusText = "获取对端库存失败: $error", success = false, error = error)
            }

            val response = fetchResult.getOrThrow()
            if (!response.ok || response.inventory == null) {
                val error = response.error ?: "peer returned ok=false"
                syncStateStore.recordFailure("peer_error", error)
                return@withContext SyncTickResult(statusText = "对端拒绝: $error", success = false, error = error)
            }

            val peerInventory = response.inventory
            val lastSuccessfulSyncAt = syncState.lastSuccessfulSyncAt

            // 3. Record attempt
            syncStateStore.recordAttempt(
                peerDeviceID = peerInventory.device.deviceID,
                localInventoryHash = localInventory.inventoryHash,
                peerInventoryHash = peerInventory.inventoryHash,
                pendingUploadCount = 0,
                pendingDownloadCount = 0
            )

            // 4. Diff
            val plan = diffPlanner.plan(localInventory, peerInventory, lastSuccessfulSyncAt)

            // 5. Apply peer recording statuses (mark local recordings as uploaded if peer says so)
            applyPeerRecordingStatuses(peerInventory)

            // 6. Apply peer metadata if needed
            if (plan.downloadMetadataActions.isNotEmpty() && peerInventory.studyManifest != null) {
                try {
                    val applyResult = studyLibraryStore.applySyncManifest(peerInventory.studyManifest, settings.deviceID)
                    // Response includes apply results
                } catch (e: Exception) {
                    // Non-fatal: metadata apply failed but we continue
                }
            }

            // 7. Upload local metadata if needed
            if (plan.uploadMetadataActions.isNotEmpty()) {
                val localManifest = studyLibraryStore.makeSyncManifest(settings.deviceID)
                val applyResult = syncClient.applyLocalNetworkSyncMetadata(settings, localManifest)
                if (applyResult.isFailure) {
                    // Non-fatal: upload failed but local state is preserved
                }
            }

            // 8. Download artifacts if needed
            for (action in plan.downloadArtifactActions) {
                downloadArtifact(settings, action.entityID)
            }

            // 9. Refresh inventory hash and record success
            val refreshedInventory = inventoryBuilder.buildInventory(settings.deviceID, android.os.Build.MODEL)
            syncStateStore.recordSuccess(
                peerDeviceID = peerInventory.device.deviceID,
                localInventoryHash = refreshedInventory.inventoryHash,
                peerInventoryHash = peerInventory.inventoryHash,
                appliedPeerRevision = null,
                pendingUploadCount = plan.uploadMetadataActions.size + plan.uploadRecordingAudioActions.size,
                pendingDownloadCount = plan.downloadMetadataActions.size + plan.downloadArtifactActions.size
            )

            SyncTickResult(
                diffPlan = plan,
                statusText = plan.summary,
                success = true
            )
        } catch (e: Exception) {
            syncStateStore.recordFailure("sync_error", e.message ?: "unknown")
            SyncTickResult(
                statusText = "同步失败: ${e.message}",
                success = false,
                error = e.message
            )
        } finally {
            isSyncing = false
        }
    }

    private fun applyPeerRecordingStatuses(peerInventory: LocalNetworkSyncInventory) {
        val recordings = AudioFileStore(context).loadAllMetadata()
        for (peerRec in peerInventory.recordings) {
            if (peerRec.receiveStatus == "completed") {
                val local = recordings.find { it.id == peerRec.recordingID } ?: continue
                if (local.uploadStatus != RecordingUploadStatus.UPLOADED.rawValue) {
                    AudioFileStore(context).updateMetadata(
                        local.copy(
                            uploadStatus = RecordingUploadStatus.UPLOADED.rawValue,
                            uploadProgressDescription = "已在 Mac 上确认接收"
                        )
                    )
                }
            }
        }
    }

    private suspend fun downloadArtifact(settings: SecureMacConnectionSnapshot, artifactID: String) {
        val result = syncClient.requestLocalNetworkSyncArtifact(settings, artifactID)
        result.fold(
            onSuccess = { response ->
                if (!response.ok || response.dataBase64 == null) return
                val data = try {
                    Base64.decode(response.dataBase64, Base64.DEFAULT)
                } catch (_: Exception) { return }

                // Verify checksum if provided
                if (response.checksum != null) {
                    val actualChecksum = SecureUploadUtilities.sha256Hex(data)
                    if (actualChecksum != response.checksum) return // checksum mismatch, discard
                }

                // Write to file
                val logicalPath = response.logicalPathToken ?: return
                // Path traversal protection
                if (logicalPath.contains("..") || logicalPath.startsWith("/")) return

                val targetFile = File(context.filesDir, "Rokurics/study/$logicalPath")
                targetFile.parentFile?.mkdirs()
                targetFile.writeBytes(data)

                // Update study item to reference the new file
                val studyDir = File(context.filesDir, "Rokurics/study")
                val relativePath = targetFile.relativeTo(studyDir).path
                when (response.kind) {
                    "transcriptMarkdown" -> {
                        val artID = response.artifactID ?: return
                        val items = studyLibraryStore.allStudyItems
                        val item = items.find { it.itemID == artID.removePrefix("artifact_").take(36) }
                        if (item != null) {
                            studyLibraryStore.save(item.copy(transcriptMarkdownRelativePath = relativePath))
                        }
                    }
                    "noteMarkdown" -> {
                        val artID = response.artifactID ?: return
                        val items = studyLibraryStore.allStudyItems
                        val item = items.find { it.itemID == artID.removePrefix("artifact_").take(36) }
                        if (item != null) {
                            studyLibraryStore.save(item.copy(noteRelativePath = relativePath))
                        }
                    }
                }
            },
            onFailure = { /* artifact download is non-fatal */ }
        )
    }

    fun isCurrentlySyncing(): Boolean = isSyncing
}
