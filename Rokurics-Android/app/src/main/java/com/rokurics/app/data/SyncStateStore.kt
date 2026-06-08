package com.rokurics.app.data

import android.content.Context
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.rokurics.app.RokuricsApp
import com.rokurics.app.domain.model.LocalNetworkSyncControlPlaneState
import com.rokurics.app.domain.model.LocalNetworkTransferProgress
import com.rokurics.app.domain.model.LocalNetworkSyncState
import com.rokurics.app.domain.model.StudyLibrarySyncState
import java.io.File

class LocalNetworkSyncStateStore(
    private val context: Context = RokuricsApp.instance
) {
    private val gson: Gson = GsonBuilder().setPrettyPrinting().create()
    private val stateFile: File
        get() {
            val dir = File(context.filesDir, "Rokurics/Sync").also { it.mkdirs() }
            return File(dir, "local-network-sync-state.json")
        }

    fun load(): LocalNetworkSyncState {
        if (!stateFile.exists()) return LocalNetworkSyncState()
        return try {
            gson.fromJson(stateFile.readText(), LocalNetworkSyncState::class.java)
        } catch (_: Exception) {
            LocalNetworkSyncState()
        }
    }

    fun save(state: LocalNetworkSyncState) {
        val tmpFile = File(stateFile.parentFile, "${stateFile.name}.tmp")
        tmpFile.writeText(gson.toJson(state))
        tmpFile.renameTo(stateFile)
    }

    fun recordAttempt(
        localDeviceID: String? = null,
        peerDeviceID: String,
        localInventoryHash: String,
        peerInventoryHash: String,
        pendingUploadCount: Int,
        pendingDownloadCount: Int,
        planSummary: String? = null,
        conflictCount: Int? = null,
        at: Long = System.currentTimeMillis()
    ) {
        val current = load()
        save(current.copy(
            localDeviceID = localDeviceID ?: current.localDeviceID,
            peerDeviceID = peerDeviceID,
            lastSyncStartedAt = at,
            lastSyncAt = at,
            lastPeerDeviceID = peerDeviceID,
            lastLocalInventoryHash = localInventoryHash,
            lastPeerInventoryHash = peerInventoryHash,
            lastPlanSummary = planSummary,
            lastConflictCount = conflictCount,
            pendingUploadCount = pendingUploadCount,
            pendingDownloadCount = pendingDownloadCount,
            lastErrorCode = null,
            lastErrorMessage = null
        ))
    }

    fun recordSuccess(
        peerDeviceID: String,
        localInventoryHash: String,
        peerInventoryHash: String,
        appliedPeerRevision: String?,
        pendingUploadCount: Int,
        pendingDownloadCount: Int,
        at: Long = System.currentTimeMillis()
    ) {
        val current = load()
        save(current.copy(
            version = LocalNetworkSyncState.CURRENT_VERSION,
            peerDeviceID = peerDeviceID,
            lastSyncCompletedAt = at,
            lastSyncAt = at,
            lastSuccessfulSyncAt = at,
            lastPeerDeviceID = peerDeviceID,
            lastLocalInventoryHash = localInventoryHash,
            lastPeerInventoryHash = peerInventoryHash,
            lastAppliedPeerRevision = appliedPeerRevision ?: current.lastAppliedPeerRevision,
            consecutiveFailureCount = 0,
            nextAllowedSyncAt = null,
            lastErrorCode = null,
            lastErrorMessage = null,
            pendingUploadCount = pendingUploadCount,
            pendingDownloadCount = pendingDownloadCount,
            activeTransfers = if (pendingUploadCount == 0 && pendingDownloadCount == 0) {
                emptyList()
            } else {
                current.activeTransfers
            }
        ))
    }

    fun recordActiveTransfers(transfers: List<LocalNetworkTransferProgress>) {
        val current = load()
        save(current.copy(activeTransfers = transfers))
    }

    fun recordControlPlane(
        syncRunID: String,
        state: LocalNetworkSyncControlPlaneState,
        at: Long = System.currentTimeMillis()
    ) {
        val current = load()
        val nextSuccessAt = when (state) {
            LocalNetworkSyncControlPlaneState.COMPLETED -> at
            else -> current.lastSuccessfulSyncAt
        }
        val nextSyncCompletedAt = when (state) {
            LocalNetworkSyncControlPlaneState.COMPLETED,
            LocalNetworkSyncControlPlaneState.FAILED -> at
            else -> current.lastSyncCompletedAt
        }
        val nextLastSyncAt = if (state == LocalNetworkSyncControlPlaneState.SYNC_START_ACKED ||
            state == LocalNetworkSyncControlPlaneState.INVENTORY_EXCHANGING
        ) {
            current.lastSyncAt ?: at
        } else {
            current.lastSyncAt
        }
        save(current.copy(
            activeSyncRunID = syncRunID,
            controlPlaneState = state,
            lastControlPlaneUpdatedAt = at,
            lastSyncCompletedAt = nextSyncCompletedAt,
            lastSuccessfulSyncAt = nextSuccessAt,
            lastSyncAt = nextLastSyncAt
        ))
    }

    fun recordFailure(
        code: String?,
        message: String?,
        at: Long = System.currentTimeMillis(),
        minimumBackoff: Long = LocalNetworkSyncState.BASE_BACKOFF_SECONDS,
        maximumBackoff: Long = LocalNetworkSyncState.MAX_BACKOFF_SECONDS
    ) {
        val current = load()
        val failures = current.consecutiveFailureCount + 1
        val exponent = kotlin.math.max(0, failures - 1)
        val delaySeconds = kotlin.math.min(minimumBackoff * (1L shl exponent), maximumBackoff)
        save(current.copy(
            version = LocalNetworkSyncState.CURRENT_VERSION,
            lastSyncCompletedAt = at,
            lastSyncAt = at,
            consecutiveFailureCount = failures,
            nextAllowedSyncAt = System.currentTimeMillis() + delaySeconds * 1000,
            lastErrorCode = code,
            lastErrorMessage = message
        ))
    }

    fun replace(nextState: LocalNetworkSyncState) {
        save(nextState)
    }
}

class StudyLibrarySyncStateStore(
    private val context: Context = RokuricsApp.instance
) {
    private val gson: Gson = GsonBuilder().setPrettyPrinting().create()
    private val stateFile: File
        get() {
            val dir = File(context.filesDir, "Rokurics/Sync").also { it.mkdirs() }
            return File(dir, "study-library-sync-state.json")
        }

    fun load(): StudyLibrarySyncState {
        if (!stateFile.exists()) {
            return StudyLibrarySyncState(deviceID = "")
        }
        return try {
            gson.fromJson(stateFile.readText(), StudyLibrarySyncState::class.java)
        } catch (_: Exception) {
            StudyLibrarySyncState(deviceID = "")
        }
    }

    fun save(state: StudyLibrarySyncState) {
        val tmpFile = File(stateFile.parentFile, "${stateFile.name}.tmp")
        tmpFile.writeText(gson.toJson(state))
        tmpFile.renameTo(stateFile)
    }

    fun recordPull(
        deviceID: String,
        remoteManifestHash: String?,
        remoteCommitID: String?,
        at: Long = System.currentTimeMillis()
    ) {
        val current = load()
        save(current.copy(
            deviceID = deviceID,
            lastPulledAt = at,
            lastRemoteManifestHash = remoteManifestHash ?: current.lastRemoteManifestHash,
            lastKnownRemoteCommitID = remoteCommitID ?: current.lastKnownRemoteCommitID,
            lastError = null
        ))
    }

    fun recordPush(
        deviceID: String,
        remoteManifestHash: String?,
        remoteCommitID: String?,
        pendingUploads: Int,
        at: Long = System.currentTimeMillis()
    ) {
        val current = load()
        save(current.copy(
            deviceID = deviceID,
            lastPushedAt = at,
            lastSuccessfulSyncAt = at,
            lastRemoteManifestHash = remoteManifestHash ?: current.lastRemoteManifestHash,
            lastKnownRemoteCommitID = remoteCommitID ?: current.lastKnownRemoteCommitID,
            pendingLocalChanges = 0,
            pendingUploads = 0,
            failedChanges = 0,
            lastError = null
        ))
    }

    fun recordPendingUploads(
        deviceID: String,
        pendingUploads: Int,
        failedChanges: Int,
        error: String?
    ) {
        val current = load()
        save(current.copy(
            deviceID = deviceID,
            pendingLocalChanges = pendingUploads,
            pendingUploads = pendingUploads,
            failedChanges = failedChanges,
            lastError = error
        ))
    }

    fun recordFailure(
        deviceID: String,
        error: String?,
        failedChanges: Int,
        pendingUploads: Int
    ) {
        val current = load()
        save(current.copy(
            deviceID = deviceID,
            lastError = error,
            failedChanges = failedChanges,
            pendingUploads = pendingUploads
        ))
    }

    fun replace(nextState: StudyLibrarySyncState) {
        save(nextState)
    }
}
