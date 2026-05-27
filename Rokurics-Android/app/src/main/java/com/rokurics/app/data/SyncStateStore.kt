package com.rokurics.app.data

import android.content.Context
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.rokurics.app.RokuricsApp
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
        peerDeviceID: String,
        localInventoryHash: String,
        peerInventoryHash: String,
        pendingUploadCount: Int,
        pendingDownloadCount: Int,
        at: Long = System.currentTimeMillis()
    ) {
        val current = load()
        save(current.copy(
            lastSyncAt = at,
            lastPeerDeviceID = peerDeviceID,
            lastLocalInventoryHash = localInventoryHash,
            lastPeerInventoryHash = peerInventoryHash,
            pendingUploadCount = pendingUploadCount,
            pendingDownloadCount = pendingDownloadCount
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
            pendingDownloadCount = pendingDownloadCount
        ))
    }

    fun recordFailure(
        code: String?,
        message: String?,
        at: Long = System.currentTimeMillis()
    ) {
        val current = load()
        val failures = current.consecutiveFailureCount + 1
        val delaySeconds = minOf(
            LocalNetworkSyncState.BASE_BACKOFF_SECONDS * (1L shl (failures - 1)),
            LocalNetworkSyncState.MAX_BACKOFF_SECONDS
        )
        save(current.copy(
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
