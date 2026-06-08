package com.rokurics.app.domain.canonical

import java.security.MessageDigest
import java.util.Date
import java.util.Locale

enum class CanonicalSyncRuntimeMode(
    val canUseCanonicalAsPrimary: Boolean = false,
    val evaluatesCanonicalCandidate: Boolean = true
) {
    DISABLED,
    DIAGNOSTICS_ONLY,
    CANONICAL_PLAN_NO_COMMIT,
    CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK(canUseCanonicalAsPrimary = true),
    BLOCKED(evaluatesCanonicalCandidate = false)
}

enum class CanonicalSyncRuntimeDiagnosticKind {
    CANONICAL_SYNC_RUNTIME_MODE_EVALUATED,
    CANONICAL_SYNC_RUNTIME_AUTHORITY_GATE_ALLOWED,
    CANONICAL_SYNC_RUNTIME_AUTHORITY_GATE_BLOCKED,
    CANONICAL_SYNC_RUNTIME_PLAN_EVALUATED,
    CANONICAL_SYNC_RUNTIME_PLAN_ALLOWED,
    CANONICAL_SYNC_RUNTIME_PLAN_USED,
    CANONICAL_SYNC_RUNTIME_PLAN_NO_COMMIT,
    CANONICAL_SYNC_RUNTIME_PLAN_FALLBACK,
    CANONICAL_SYNC_RUNTIME_PLAN_BLOCKED,
    CANONICAL_SYNC_RUNTIME_LEGACY_HASH_MISMATCH_IGNORED,
    CANONICAL_SYNC_RUNTIME_UNSUPPORTED_OBJECT_BLOCKED,
    CANONICAL_SYNC_RUNTIME_CONFLICT_BLOCKED,
    CANONICAL_SYNC_RUNTIME_PEER_SNAPSHOT_UNAVAILABLE,
    CANONICAL_SYNC_RUNTIME_DUPLICATE_LEGACY_SUPPRESSED,
    CANONICAL_SYNC_RUNTIME_DUPLICATE_EXECUTION_PREVENTED,
    CANONICAL_SYNC_RUNTIME_METADATA_HASH_EQUAL,
    CANONICAL_SYNC_RUNTIME_MODIFIED_AT_LWW_APPLIED,
    CANONICAL_SYNC_RUNTIME_MODIFIED_AT_UNAVAILABLE,
    CANONICAL_SYNC_RUNTIME_SCHEMA_MISMATCH,
    CANONICAL_EXISTENCE_TRUTH_EVALUATED,
    CANONICAL_EXISTENCE_APPLY_BRIDGE_EVALUATED,
    CANONICAL_EXISTENCE_APPLY_BRIDGE_BLOCKED,
    CANONICAL_EXISTENCE_METADATA_ONLY_RECORD_WRITTEN,
    CANONICAL_EXISTENCE_METADATA_ONLY_RECORD_NO_OP,
    CANONICAL_EXISTENCE_APPLY_BRIDGE_ROLLBACK_STARTED,
    CANONICAL_EXISTENCE_APPLY_BRIDGE_ROLLBACK_COMPLETED,
    CANONICAL_EXISTENCE_APPLY_BRIDGE_ROLLBACK_FAILED,
    CANONICAL_EXISTENCE_PEER_METADATA_ONLY_UPLOAD_CANDIDATE,
    CANONICAL_EXISTENCE_PEER_ABSENT_METADATA_BRIDGE_REQUIRED,
    CANONICAL_EXISTENCE_PEER_UNKNOWN_DEFERRED,
    CANONICAL_EXISTENCE_AUDIO_SAME_NO_OP,
    CANONICAL_EXISTENCE_AUDIO_CONFLICT,
    CANONICAL_EXISTENCE_MANIFEST_RECORDINGS_CONSUMED,
    CANONICAL_EXISTENCE_MANIFEST_RECORDINGS_IGNORED_BLOCKED,
    CANONICAL_EXISTENCE_DID_NOT_WRITE_AUDIO,
    CANONICAL_EXISTENCE_DID_NOT_MARK_AUDIO_AVAILABLE,
    CANONICAL_APPLY_RUNTIME_MODE_EVALUATED,
    CANONICAL_APPLY_RUNTIME_GATE_ALLOWED,
    CANONICAL_APPLY_RUNTIME_GATE_BLOCKED,
    CANONICAL_APPLY_RUNTIME_ACTION_STARTED,
    CANONICAL_APPLY_RUNTIME_ACTION_COMPLETED,
    CANONICAL_APPLY_RUNTIME_ACTION_FAILED,
    CANONICAL_APPLY_RUNTIME_ROLLBACK_STARTED,
    CANONICAL_APPLY_RUNTIME_ROLLBACK_COMPLETED,
    CANONICAL_APPLY_RUNTIME_ROLLBACK_FAILED,
    CANONICAL_APPLY_RUNTIME_LEGACY_FALLBACK_USED,
    CANONICAL_APPLY_RUNTIME_DUPLICATE_LEGACY_SUPPRESSED,
    CANONICAL_APPLY_RUNTIME_AUDIO_ACTION_BLOCKED,
    CANONICAL_APPLY_RUNTIME_REPORT_BUILT
}

data class CanonicalSyncRuntimeDiagnostic(
    val kind: CanonicalSyncRuntimeDiagnosticKind,
    val syncRunID: String?,
    val mode: CanonicalSyncRuntimeMode,
    val objectID: String? = null,
    val hashPrefix: String? = null,
    val count: Int? = null,
    val detail: String? = null
) {
    val id: String =
        listOfNotNull(kind.name, syncRunID, objectID, detail).joinToString("|")
}

// ── Type 1: CanonicalTimestamp ──

data class CanonicalTimestamp(
    val date: Date
) : Comparable<CanonicalTimestamp> {
    override fun compareTo(other: CanonicalTimestamp): Int {
        return date.compareTo(other.date)
    }
}

// ── Type 2: CanonicalHash ──

data class CanonicalHash(
    val algorithm: String,
    val value: String
) {
    constructor(value: String) : this(
        algorithm = "sha256",
        value = value.trim().lowercase()
    )

    companion object {
        private const val SHA256 = "SHA-256"

        fun sha256(map: Map<String, String>): CanonicalHash {
            val jsonBytes = sortedJsonBytes(map)
            return sha256(jsonBytes)
        }

        fun sha256String(value: String): CanonicalHash {
            return sha256(value.toByteArray(Charsets.UTF_8))
        }

        private fun sha256(data: ByteArray): CanonicalHash {
            val digest = MessageDigest.getInstance(SHA256)
            val hashBytes = digest.digest(data)
            val hex = hashBytes.joinToString("") { "%02x".format(Locale.US, it) }
            return CanonicalHash(hex)
        }

        private fun sortedJsonBytes(map: Map<String, String>): ByteArray {
            val sorted = map.toSortedMap()
            val sb = StringBuilder("{")
            sorted.entries.forEachIndexed { index, (key, value) ->
                if (index > 0) sb.append(",")
                sb.append("\"")
                sb.append(key.escapeJson())
                sb.append("\":\"")
                sb.append(value.escapeJson())
                sb.append("\"")
            }
            sb.append("}")
            return sb.toString().toByteArray(Charsets.UTF_8)
        }

        private fun String.escapeJson(): String {
            return this.replace("\\", "\\\\").replace("\"", "\\\"")
                .replace("\n", "\\n").replace("\r", "\\r").replace("\t", "\\t")
        }
    }
}

// ── Type 3: CanonicalCapability ──

enum class CanonicalCapability {
    RECORDING_METADATA,
    AUDIO_ARTIFACT,
    RECEIVE_RECORD,
    TRANSCRIPT_ARTIFACT,
    NOTE_ARTIFACT,
    SUMMARY_ARTIFACT,
    OBJECT_PROJECTION,
    CANONICAL_LIBRARY_OBJECTS_V1,
    CANONICAL_FOLDER_OBJECTS_V1,
    CANONICAL_STUDY_ITEM_OBJECTS_V1,
    CANONICAL_TRANSFER_STATE_V1,
    CANONICAL_OBJECT_PROJECTION_V1,
    CANONICAL_INVENTORY_BUILDER_V1,
    CANONICAL_RETIREMENT_READINESS_V1
}

// ── Type 4: CanonicalNode ──

data class CanonicalNode(
    val nodeID: String,
    val platform: String,
    val displayName: String?,
    val capabilities: List<CanonicalCapability>
) {
    val id: String get() = nodeID
}

// ── Type 5: CanonicalRecordingMetadata ──

data class CanonicalRecordingMetadata(
    val objectID: String,
    val title: String,
    val createdAt: CanonicalTimestamp,
    val modifiedAt: CanonicalTimestamp,
    val duration: Double?,
    val filing: CanonicalRecordingMetadata.Filing?,
    val tags: List<String>,
    val isDeleted: Boolean,
    val deletedAt: CanonicalTimestamp?
) {
    data class Filing(
        val type: String?,
        val subject: String?,
        val chapter: String?,
        val topic: String?
    ) {

        val isEmpty: Boolean
            get() = type == null && subject == null && chapter == null && topic == null

        companion object {
            private fun normalized(value: String?): String? {
                return value?.trim()?.nilIfEmpty
            }
        }
    }

    companion object {
        const val BUSINESS_METADATA_HASH_SCHEMA_VERSION = "canonical-recording-business-metadata-v1"

        operator fun invoke(
            objectID: String,
            title: String,
            createdAt: CanonicalTimestamp,
            modifiedAt: CanonicalTimestamp,
            duration: Double? = null,
            filing: Filing? = null,
            tags: List<String> = emptyList(),
            isDeleted: Boolean = false,
            deletedAt: CanonicalTimestamp? = null
        ): CanonicalRecordingMetadata {
            return CanonicalRecordingMetadata(
                objectID = normalizedRequired(objectID, "unknown-recording"),
                title = normalizedRequiredPreservingInput(title, "未命名录音"),
                createdAt = createdAt,
                modifiedAt = modifiedAt,
                duration = duration,
                filing = if (filing?.isEmpty == true) null else filing,
                tags = normalizedTags(tags),
                isDeleted = isDeleted,
                deletedAt = deletedAt
            )
        }

        private fun normalizedRequired(value: String, fallback: String): String {
            return value.trim().nilIfEmpty ?: fallback
        }

        private fun normalizedRequiredPreservingInput(value: String, fallback: String): String {
            return if (value.trim().isEmpty()) fallback else value
        }

        private fun normalizedTags(tags: List<String>): List<String> {
            return tags.mapNotNull { it.trim().nilIfEmpty?.lowercase() }
                .toSet().sorted()
        }

        private fun timestampString(timestamp: CanonicalTimestamp): String {
            return numberString(timestamp.date.time.toDouble() / 1000.0)
        }

        private fun numberString(value: Double): String {
            return "%.6f".format(Locale.US, value)
        }
    }

    val metadataHash: CanonicalHash
        get() = CanonicalHash.sha256(
            mapOf(
                "schema" to BUSINESS_METADATA_HASH_SCHEMA_VERSION,
                "objectID" to objectID,
                "title" to title,
                "filing.type" to (filing?.type ?: ""),
                "filing.subject" to (filing?.subject ?: ""),
                "filing.chapter" to (filing?.chapter ?: ""),
                "filing.topic" to (filing?.topic ?: ""),
                "tags" to tags.joinToString("\u001F"),
                "isDeleted" to if (isDeleted) "true" else "false",
                "deletedAt" to (deletedAt?.let { composeTimestampString(it) } ?: "")
            )
        )

    private fun composeTimestampString(timestamp: CanonicalTimestamp): String {
        return "%.6f".format(Locale.US, timestamp.date.time.toDouble() / 1000.0)
    }
}

// ── Type 6: CanonicalArtifact ──

data class CanonicalArtifact(
    val artifactID: String,
    val objectID: String,
    val kind: CanonicalArtifact.Kind,
    val availability: CanonicalArtifact.Availability,
    val contentHash: CanonicalHash?,
    val byteSize: Long?,
    val logicalName: String?,
    val logicalPathToken: String?,
    val modifiedAt: CanonicalTimestamp?,
    val observedAt: CanonicalTimestamp?,
    val producedBy: CanonicalArtifactProducer?,
    val producedByNodeID: String?,
    val tombstone: Boolean?
) {
    enum class Kind {
        AUDIO,
        METADATA,
        RECEIVE_RECORD,
        TRANSCRIPT_JSON,
        TRANSCRIPT_MARKDOWN,
        NOTE_MARKDOWN,
        NOTE_JSON,
        SUMMARY_JSON;

        fun artifactID(objectID: String): String {
            return "${name.lowercase()}:$objectID"
        }
    }

    enum class Availability {
        UNKNOWN,
        MISSING,
        AVAILABLE_WITHOUT_HASH,
        AVAILABLE
    }

    val id: String get() = artifactID

    val provesCanonicalAudioAvailability: Boolean
        get() = kind == Kind.AUDIO &&
                availability == Availability.AVAILABLE &&
                contentHash != null &&
                byteSize != null &&
                tombstone != true

    val provesCanonicalGeneratedArtifactAvailability: Boolean
        get() = CanonicalProjectionContract.provesGeneratedArtifactAvailability(this)

    val isCanonicalGeneratedArtifact: Boolean
        get() = CanonicalProjectionContract.generatedArtifactKinds.contains(kind)
}

// ── Type 7: CanonicalArtifactFact ──

data class CanonicalArtifactFact(
    val kind: CanonicalArtifact.Kind,
    val availability: CanonicalArtifact.Availability,
    val contentHash: CanonicalHash?,
    val byteSize: Long?,
    val logicalName: String?,
    val logicalPathToken: String?,
    val modifiedAt: CanonicalTimestamp?,
    val observedAt: CanonicalTimestamp?,
    val producedBy: CanonicalArtifactProducer?,
    val producedByNodeID: String?,
    val tombstone: Boolean?
) {

    companion object {
        fun audio(
            availability: CanonicalArtifact.Availability,
            contentHash: CanonicalHash? = null,
            byteSize: Long? = null,
            logicalName: String? = null,
            logicalPathToken: String? = null,
            modifiedAt: CanonicalTimestamp? = null,
            observedAt: CanonicalTimestamp? = null,
            producedByNodeID: String? = null
        ): CanonicalArtifactFact {
            return CanonicalArtifactFact(
                kind = CanonicalArtifact.Kind.AUDIO,
                availability = availability,
                contentHash = contentHash,
                byteSize = byteSize,
                logicalName = logicalName,
                logicalPathToken = logicalPathToken,
                modifiedAt = modifiedAt,
                observedAt = observedAt,
                producedBy = CanonicalArtifactProducer.AUDIO_CAPTURE,
                producedByNodeID = producedByNodeID,
                tombstone = null
            )
        }
    }

    fun makeArtifact(
        objectID: String,
        fallbackProducedByNodeID: String? = null
    ): CanonicalArtifact {
        return CanonicalArtifact(
            artifactID = CanonicalProjectionContract.artifactID(objectID, kind),
            objectID = objectID,
            kind = kind,
            availability = availability,
            contentHash = contentHash,
            byteSize = byteSize,
            logicalName = logicalName,
            logicalPathToken = logicalPathToken,
            modifiedAt = modifiedAt,
            observedAt = observedAt,
            producedBy = producedBy,
            producedByNodeID = producedByNodeID ?: fallbackProducedByNodeID,
            tombstone = tombstone
        )
    }
}

// ── Type 8: CanonicalSyncState ──

enum class CanonicalSyncState {
    UNKNOWN,
    LOCAL_ONLY,
    SYNCED,
    DIVERGED,
    DELETED,
    CONFLICT
}

// ── Type 9: CanonicalTransferState ──

enum class CanonicalTransferState {
    NONE,
    QUEUED,
    IN_FLIGHT,
    RETRY_PENDING,
    COMPLETED,
    FAILED,
    CONFLICT
}

// ── Type 10: CanonicalProcessingState ──

data class CanonicalProcessingState(
    val transcription: CanonicalProcessingState.Stage,
    val note: CanonicalProcessingState.Stage
) {
    enum class Stage {
        NOT_STARTED,
        QUEUED,
        PROCESSING,
        COMPLETED,
        FAILED,
        UNKNOWN
    }

    companion object {
        val UNKNOWN = CanonicalProcessingState(Stage.UNKNOWN, Stage.UNKNOWN)
    }
}

// ── Type 11: CanonicalRecordingObject ──

data class CanonicalRecordingObject(
    val objectID: String,
    val nodeID: String?,
    var metadata: CanonicalRecordingMetadata,
    var metadataHash: CanonicalHash,
    val artifacts: List<CanonicalArtifact>,
    var syncState: CanonicalSyncState,
    var transferState: CanonicalTransferState,
    var processingState: CanonicalProcessingState,
    val receivedAt: CanonicalTimestamp?,
    val observedAt: CanonicalTimestamp?
) {
    val id: String get() = objectID

    val audioArtifact: CanonicalArtifact?
        get() = artifacts.firstOrNull { it.kind == CanonicalArtifact.Kind.AUDIO }

    val audioAvailable: Boolean
        get() = audioArtifact?.provesCanonicalAudioAvailability == true

    fun replacingArtifacts(newArtifacts: List<CanonicalArtifact>): CanonicalRecordingObject {
        return copy(
            artifacts = newArtifacts.sortedBy { it.artifactID }
        )
    }

    companion object {
        operator fun invoke(
            objectID: String,
            nodeID: String? = null,
            metadata: CanonicalRecordingMetadata,
            artifacts: List<CanonicalArtifact> = emptyList(),
            syncState: CanonicalSyncState = CanonicalSyncState.UNKNOWN,
            transferState: CanonicalTransferState = CanonicalTransferState.NONE,
            processingState: CanonicalProcessingState = CanonicalProcessingState.UNKNOWN,
            receivedAt: CanonicalTimestamp? = null,
            observedAt: CanonicalTimestamp? = null
        ): CanonicalRecordingObject {
            return CanonicalRecordingObject(
                objectID = objectID.trim().nilIfEmpty ?: metadata.objectID,
                nodeID = nodeID?.trim()?.nilIfEmpty,
                metadata = metadata,
                metadataHash = metadata.metadataHash,
                artifacts = artifacts.sortedBy { it.artifactID },
                syncState = syncState,
                transferState = transferState,
                processingState = processingState,
                receivedAt = receivedAt,
                observedAt = observedAt
            )
        }
    }
}

// ── Type 12: CanonicalManifest ──

data class CanonicalManifest(
    val schemaVersion: Int,
    val node: CanonicalNode,
    val generatedAt: CanonicalTimestamp,
    val objects: List<CanonicalRecordingObject>,
    val libraryObjects: List<CanonicalLibraryObject>,
    val folders: List<CanonicalFolderObject>,
    val studyItems: List<CanonicalStudyItemObject>,
    val standaloneNotes: List<CanonicalStandaloneNoteObject>,
    val libraryTombstones: List<CanonicalLibraryTombstone>,
    val manifestCapabilities: List<CanonicalCapability>,
    val manifestHash: CanonicalHash
) {
    companion object {
        const val CURRENT_SCHEMA_VERSION = 1

        fun make(
            node: CanonicalNode,
            generatedAt: Date = Date(),
            objects: List<CanonicalRecordingObject>,
            libraryObjects: List<CanonicalLibraryObject> = emptyList(),
            folders: List<CanonicalFolderObject> = emptyList(),
            studyItems: List<CanonicalStudyItemObject> = emptyList(),
            standaloneNotes: List<CanonicalStandaloneNoteObject> = emptyList(),
            libraryTombstones: List<CanonicalLibraryTombstone> = emptyList(),
            manifestCapabilities: List<CanonicalCapability> = emptyList()
        ): CanonicalManifest {
            val sortedObjects = objects.sortedBy { it.objectID }
            val sortedLibraryObjects = libraryObjects.sortedBy { it.objectID.rawValue }
            val sortedFolders = folders.sortedBy { it.folderID.rawValue }
            val sortedStudyItems = studyItems.sortedBy { it.itemID.rawValue }
            val sortedStandaloneNotes = standaloneNotes.sortedBy { it.noteID.rawValue }
            val sortedTombstones = libraryTombstones.sortedBy { it.tombstoneID }
            val caps = manifestCapabilities.toSet().sortedBy { it.name }
            var manifest = CanonicalManifest(
                schemaVersion = CURRENT_SCHEMA_VERSION,
                node = node,
                generatedAt = CanonicalTimestamp(generatedAt),
                objects = sortedObjects,
                libraryObjects = sortedLibraryObjects,
                folders = sortedFolders,
                studyItems = sortedStudyItems,
                standaloneNotes = sortedStandaloneNotes,
                libraryTombstones = sortedTombstones,
                manifestCapabilities = caps,
                manifestHash = CanonicalHash("")
            )
            manifest = manifest.copy(manifestHash = manifest.computedManifestHash())
            return manifest
        }

        private fun timestampString(timestamp: CanonicalTimestamp): String {
            return "%.6f".format(Locale.US, timestamp.date.time.toDouble() / 1000.0)
        }
    }

    fun objectWithID(objectID: String): CanonicalRecordingObject? {
        return objects.firstOrNull { it.objectID == objectID }
    }

    fun computedManifestHash(): CanonicalHash {
        return CanonicalHash.sha256(
            mapOf(
                "schemaVersion" to schemaVersion.toString(),
                "nodeID" to node.nodeID,
                "nodePlatform" to node.platform,
                "nodeCapabilities" to node.capabilities.joinToString("\u001F") { it.name.lowercase() },
                "manifestCapabilities" to manifestCapabilities.joinToString("\u001F") { it.name.lowercase() },
                "generatedAt" to composeTimestampString(generatedAt),
                "objects" to objects.joinToString("\u001E") { objectHashSummary(it) },
                "libraryObjects" to libraryObjects.joinToString("\u001E") { libraryObjectHashSummary(it) },
                "folders" to folders.joinToString("\u001E") { it.metadataHash.value },
                "studyItems" to studyItems.joinToString("\u001E") { it.metadataHash.value },
                "standaloneNotes" to standaloneNotes.joinToString("\u001E") { it.metadataHash.value },
                "libraryTombstones" to libraryTombstones.joinToString("\u001E") { libraryTombstoneHashSummary(it) }
            )
        )
    }

    val hasValidManifestHash: Boolean
        get() {
            val computed = computedManifestHash()
            return manifestHash.algorithm == computed.algorithm && manifestHash.value == computed.value
        }

    private fun objectHashSummary(obj: CanonicalRecordingObject): String {
        val artifactsPart = obj.artifacts.joinToString("\u001D") { artifact ->
            listOf(
                artifact.artifactID,
                artifact.kind.name.lowercase(),
                artifact.availability.name.lowercase(),
                artifact.contentHash?.value ?: "",
                artifact.byteSize?.toString() ?: ""
            ).joinToString("\u001F")
        }
        return listOf(
            obj.objectID,
            obj.metadataHash.value,
            obj.metadataHash.algorithm,
            obj.syncState.name.lowercase(),
            obj.transferState.name.lowercase(),
            if (obj.metadata.isDeleted) "deleted" else "active",
            artifactsPart
        ).joinToString("\u001F")
    }

    private fun libraryObjectHashSummary(obj: CanonicalLibraryObject): String {
        return listOf(
            obj.objectID.rawValue,
            obj.kind.name.lowercase(),
            obj.metadataHash.value,
            if (obj.isDeleted) "deleted" else "active",
            obj.businessModifiedAt?.let { composeTimestampString(it) } ?: ""
        ).joinToString("\u001F")
    }

    private fun libraryTombstoneHashSummary(tombstone: CanonicalLibraryTombstone): String {
        return listOf(
            tombstone.tombstoneID,
            tombstone.objectID.rawValue,
            tombstone.objectKind.name.lowercase(),
            tombstone.deletedAt?.let { composeTimestampString(it) } ?: "",
            tombstone.reason.name.lowercase()
        ).joinToString("\u001F")
    }

    private fun composeTimestampString(timestamp: CanonicalTimestamp): String {
        return "%.6f".format(Locale.US, timestamp.date.time.toDouble() / 1000.0)
    }
}

// ── Type 13: ConflictReason ──

enum class ConflictReason {
    OBJECT_IDENTITY_COLLISION,
    METADATA_MODIFIED_ON_BOTH_SIDES,
    ARTIFACT_HASH_MISMATCH,
    ARTIFACT_SIZE_MISMATCH,
    ARTIFACT_UNAVAILABLE_MISMATCH
}

// ── Type 14: SyncDecision ──

data class SyncDecision(
    val kind: SyncDecision.Kind,
    val objectID: String,
    val reason: String,
    val conflictReason: ConflictReason? = null
) {
    enum class Kind {
        NO_OP,
        UPLOAD_METADATA,
        DOWNLOAD_METADATA,
        DEFER_UNTIL_PEER_KNOWN,
        CONFLICT
    }

    companion object {
        fun metadata(
            local: CanonicalRecordingObject,
            peer: CanonicalRecordingObject?
        ): SyncDecision {
            if (peer == null) {
                return SyncDecision(
                    kind = Kind.UPLOAD_METADATA,
                    objectID = local.objectID,
                    reason = "peer_missing_metadata"
                )
            }
            if (sameHash(local.metadataHash, peer.metadataHash)) {
                return SyncDecision(
                    kind = Kind.NO_OP,
                    objectID = local.objectID,
                    reason = "metadata_hash_equal"
                )
            }
            if (local.metadata.modifiedAt.date.after(peer.metadata.modifiedAt.date)) {
                return SyncDecision(
                    kind = Kind.UPLOAD_METADATA,
                    objectID = local.objectID,
                    reason = "local_metadata_newer"
                )
            }
            if (peer.metadata.modifiedAt.date.after(local.metadata.modifiedAt.date)) {
                return SyncDecision(
                    kind = Kind.DOWNLOAD_METADATA,
                    objectID = local.objectID,
                    reason = "peer_metadata_newer"
                )
            }
            return SyncDecision(
                kind = Kind.CONFLICT,
                objectID = local.objectID,
                reason = "metadata_hash_mismatch_same_modified_at",
                conflictReason = ConflictReason.METADATA_MODIFIED_ON_BOTH_SIDES
            )
        }

        private fun sameHash(left: CanonicalHash, right: CanonicalHash): Boolean {
            return left.algorithm == right.algorithm && left.value == right.value
        }
    }
}

// ── Type 15: TransferDecision ──

data class TransferDecision(
    val kind: TransferDecision.Kind,
    val objectID: String,
    val artifactID: String?,
    val reason: String,
    val conflictReason: ConflictReason? = null
) {
    enum class Kind {
        NO_OP,
        UPLOAD,
        DOWNLOAD,
        DEFER_UNTIL_PEER_KNOWN,
        CONFLICT,
        LOCAL_UNAVAILABLE
    }

    companion object {
        fun audio(
            local: CanonicalRecordingObject,
            peer: CanonicalRecordingObject?
        ): TransferDecision {
            val localAudio = local.audioArtifact
            val peerAudio = peer?.audioArtifact
            val artifactID = localAudio?.artifactID ?: peerAudio?.artifactID

            if (localAudio == null || !localAudio.provesCanonicalAudioAvailability) {
                return TransferDecision(
                    kind = Kind.LOCAL_UNAVAILABLE,
                    objectID = local.objectID,
                    artifactID = artifactID,
                    reason = "local_audio_unproven"
                )
            }

            if (peer == null) {
                return TransferDecision(
                    kind = Kind.DEFER_UNTIL_PEER_KNOWN,
                    objectID = local.objectID,
                    artifactID = artifactID,
                    reason = "peer_unknown_is_not_missing"
                )
            }

            if (peerAudio == null || peerAudio.availability == CanonicalArtifact.Availability.MISSING) {
                return TransferDecision(
                    kind = Kind.UPLOAD,
                    objectID = local.objectID,
                    artifactID = artifactID,
                    reason = "peer_audio_missing"
                )
            }

            if (!peerAudio.provesCanonicalAudioAvailability) {
                return TransferDecision(
                    kind = Kind.DEFER_UNTIL_PEER_KNOWN,
                    objectID = local.objectID,
                    artifactID = artifactID,
                    reason = "peer_audio_unproven"
                )
            }

            if (!sameHash(localAudio.contentHash, peerAudio.contentHash)) {
                return TransferDecision(
                    kind = Kind.CONFLICT,
                    objectID = local.objectID,
                    artifactID = artifactID,
                    reason = "audio_hash_mismatch",
                    conflictReason = ConflictReason.ARTIFACT_HASH_MISMATCH
                )
            }

            if (localAudio.byteSize != peerAudio.byteSize) {
                return TransferDecision(
                    kind = Kind.CONFLICT,
                    objectID = local.objectID,
                    artifactID = artifactID,
                    reason = "audio_size_mismatch",
                    conflictReason = ConflictReason.ARTIFACT_SIZE_MISMATCH
                )
            }

            return TransferDecision(
                kind = Kind.NO_OP,
                objectID = local.objectID,
                artifactID = artifactID,
                reason = "peer_audio_same_hash_and_size"
            )
        }

        private fun sameHash(left: CanonicalHash?, right: CanonicalHash?): Boolean {
            return left?.algorithm == right?.algorithm && left?.value == right?.value
        }
    }
}

// ── Type 16: ObjectProjection ──

data class ObjectProjection(
    val objectID: String,
    val displayTitle: String,
    val metadataHash: CanonicalHash,
    val audioAvailable: Boolean,
    val syncState: CanonicalSyncState,
    val transferState: CanonicalTransferState,
    val processingState: CanonicalProcessingState,
    val conflictReasons: List<ConflictReason>
) {
    val id: String get() = objectID

    companion object {
        fun make(
            obj: CanonicalRecordingObject,
            conflictReasons: List<ConflictReason> = emptyList()
        ): ObjectProjection {
            return ObjectProjection(
                objectID = obj.objectID,
                displayTitle = obj.metadata.title,
                metadataHash = obj.metadataHash,
                audioAvailable = obj.audioAvailable,
                syncState = obj.syncState,
                transferState = obj.transferState,
                processingState = obj.processingState,
                conflictReasons = conflictReasons
            )
        }
    }
}

// ── Type 17: CanonicalRecordingExistenceState ──

enum class CanonicalRecordingExistenceState {
    ABSENT,
    METADATA_ONLY,
    RECEIVE_RECORD_ONLY,
    STUDY_ITEM_ONLY,
    METADATA_AND_STUDY_ITEM,
    AUDIO_AVAILABLE,
    AUDIO_HASH_SIZE_MATCHED,
    AUDIO_CONFLICT,
    PEER_UNKNOWN,
    TOMBSTONED,
    UNSUPPORTED;

    val isAudioProof: Boolean
        get() = this == AUDIO_AVAILABLE || this == AUDIO_HASH_SIZE_MATCHED
}

// ── Type 18: CanonicalRecordingExistenceSource ──

enum class CanonicalRecordingExistenceSource {
    CANONICAL_MANIFEST,
    STUDY_LIBRARY_MANIFEST,
    LOCAL_INVENTORY,
    PEER_INVENTORY,
    RECORDING_METADATA,
    RECEIVE_RECORD,
    STUDY_ITEM,
    AUDIO_ARTIFACT,
    COMPLETED_UPLOAD_LEDGER,
    CANONICAL_EXISTENCE_LEDGER
}

// ── Type 19: CanonicalRecordingExistenceDecision ──

enum class CanonicalRecordingExistenceDecision {
    NO_OP,
    APPLY_METADATA_ONLY_BRIDGE,
    UPLOAD_AUDIO_CANDIDATE,
    AUDIO_SAME_NO_OP,
    CONFLICT,
    DEFERRED,
    BLOCKED,
    UNSUPPORTED
}

// ── Type 20: CanonicalRecordingExistenceBlocker ──

enum class CanonicalRecordingExistenceBlocker {
    TOMBSTONED_PARENT,
    PEER_UNKNOWN,
    MISSING_LOCAL_AUDIO,
    LOCAL_AUDIO_UNPROVEN,
    PEER_AUDIO_UNPROVEN,
    AUDIO_HASH_MISMATCH,
    AUDIO_SIZE_MISMATCH,
    COMPLETED_LEDGER_NOT_AUDIO_PROOF,
    METADATA_ONLY_NOT_AUDIO_PROOF,
    RECEIVE_RECORD_NOT_AUDIO_PROOF,
    STUDY_ITEM_NOT_AUDIO_PROOF,
    UNSUPPORTED_OBJECT
}

// ── Type 21: CanonicalRecordingExistenceTruth ──

data class CanonicalRecordingExistenceTruth(
    val objectID: String,
    val localState: CanonicalRecordingExistenceState,
    val peerState: CanonicalRecordingExistenceState,
    val decision: CanonicalRecordingExistenceDecision,
    val sources: List<CanonicalRecordingExistenceSource>,
    val blockers: List<CanonicalRecordingExistenceBlocker>,
    val localMetadataHashPrefix: String?,
    val peerMetadataHashPrefix: String?,
    val localAudioHashPrefix: String?,
    val peerAudioHashPrefix: String?,
    val localByteSize: Long?,
    val peerByteSize: Long?
) {
    val peerAudioAvailable: Boolean
        get() = peerState.isAudioProof

    val shouldCreateUploadCandidate: Boolean
        get() = decision == CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE

    val requiresMetadataApplyBridge: Boolean
        get() = decision == CanonicalRecordingExistenceDecision.APPLY_METADATA_ONLY_BRIDGE

    companion object {
        fun evaluate(
            objectID: String,
            local: CanonicalRecordingObject?,
            peer: CanonicalRecordingObject?,
            peerKnown: Boolean = true,
            peerStudyItemExists: Boolean = false,
            peerReceiveRecordExists: Boolean = false,
            peerCompletedLedgerOnly: Boolean = false,
            tombstonedParent: Boolean = false
        ): CanonicalRecordingExistenceTruth {
            val normalizedObjectID = objectID.trim()
            val sources = mutableSetOf(CanonicalRecordingExistenceSource.CANONICAL_MANIFEST)
            if (local != null) {
                sources.add(CanonicalRecordingExistenceSource.LOCAL_INVENTORY)
                sources.add(CanonicalRecordingExistenceSource.RECORDING_METADATA)
            }
            if (peer != null) {
                sources.add(CanonicalRecordingExistenceSource.PEER_INVENTORY)
                sources.add(CanonicalRecordingExistenceSource.RECORDING_METADATA)
            }
            if (peerStudyItemExists) {
                sources.add(CanonicalRecordingExistenceSource.STUDY_ITEM)
            }
            if (peerReceiveRecordExists) {
                sources.add(CanonicalRecordingExistenceSource.RECEIVE_RECORD)
            }
            if (peerCompletedLedgerOnly) {
                sources.add(CanonicalRecordingExistenceSource.COMPLETED_UPLOAD_LEDGER)
            }

            val localState = existenceState(
                obj = local,
                known = true,
                studyItemExists = false,
                receiveRecordExists = false,
                completedLedgerOnly = false,
                tombstonedParent = tombstonedParent
            )
            var peerState = existenceState(
                obj = peer,
                known = peerKnown,
                studyItemExists = peerStudyItemExists,
                receiveRecordExists = peerReceiveRecordExists,
                completedLedgerOnly = peerCompletedLedgerOnly,
                tombstonedParent = tombstonedParent
            )
            val blockers = mutableSetOf<CanonicalRecordingExistenceBlocker>()

            if (tombstonedParent) {
                blockers.add(CanonicalRecordingExistenceBlocker.TOMBSTONED_PARENT)
                return truth(
                    objectID = normalizedObjectID,
                    local = local, peer = peer,
                    localState = localState,
                    peerState = CanonicalRecordingExistenceState.TOMBSTONED,
                    decision = CanonicalRecordingExistenceDecision.BLOCKED,
                    sources = sources, blockers = blockers
                )
            }

            if (!peerKnown) {
                blockers.add(CanonicalRecordingExistenceBlocker.PEER_UNKNOWN)
                return truth(
                    objectID = normalizedObjectID,
                    local = local, peer = peer,
                    localState = localState,
                    peerState = CanonicalRecordingExistenceState.PEER_UNKNOWN,
                    decision = CanonicalRecordingExistenceDecision.DEFERRED,
                    sources = sources, blockers = blockers
                )
            }

            val localAudio = local?.audioArtifact
            if (localAudio == null) {
                blockers.add(CanonicalRecordingExistenceBlocker.MISSING_LOCAL_AUDIO)
                return truth(
                    objectID = normalizedObjectID,
                    local = local, peer = peer,
                    localState = localState,
                    peerState = peerState,
                    decision = if (peerState == CanonicalRecordingExistenceState.ABSENT)
                        CanonicalRecordingExistenceDecision.APPLY_METADATA_ONLY_BRIDGE
                    else
                        CanonicalRecordingExistenceDecision.NO_OP,
                    sources = sources, blockers = blockers
                )
            }

            if (!localAudio.provesCanonicalAudioAvailability) {
                blockers.add(CanonicalRecordingExistenceBlocker.LOCAL_AUDIO_UNPROVEN)
                return truth(
                    objectID = normalizedObjectID,
                    local = local, peer = peer,
                    localState = localState,
                    peerState = peerState,
                    decision = CanonicalRecordingExistenceDecision.BLOCKED,
                    sources = sources, blockers = blockers
                )
            }

            return when (peerState) {
                CanonicalRecordingExistenceState.ABSENT -> {
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = peerState,
                        decision = CanonicalRecordingExistenceDecision.APPLY_METADATA_ONLY_BRIDGE,
                        sources = sources, blockers = blockers
                    )
                }
                CanonicalRecordingExistenceState.METADATA_ONLY -> {
                    blockers.add(CanonicalRecordingExistenceBlocker.METADATA_ONLY_NOT_AUDIO_PROOF)
                    if (peerCompletedLedgerOnly) {
                        blockers.add(CanonicalRecordingExistenceBlocker.COMPLETED_LEDGER_NOT_AUDIO_PROOF)
                    }
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = peerState,
                        decision = CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE,
                        sources = sources, blockers = blockers
                    )
                }
                CanonicalRecordingExistenceState.RECEIVE_RECORD_ONLY -> {
                    blockers.add(CanonicalRecordingExistenceBlocker.RECEIVE_RECORD_NOT_AUDIO_PROOF)
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = peerState,
                        decision = CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE,
                        sources = sources, blockers = blockers
                    )
                }
                CanonicalRecordingExistenceState.STUDY_ITEM_ONLY -> {
                    blockers.add(CanonicalRecordingExistenceBlocker.STUDY_ITEM_NOT_AUDIO_PROOF)
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = peerState,
                        decision = CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE,
                        sources = sources, blockers = blockers
                    )
                }
                CanonicalRecordingExistenceState.METADATA_AND_STUDY_ITEM -> {
                    blockers.add(CanonicalRecordingExistenceBlocker.METADATA_ONLY_NOT_AUDIO_PROOF)
                    blockers.add(CanonicalRecordingExistenceBlocker.STUDY_ITEM_NOT_AUDIO_PROOF)
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = peerState,
                        decision = CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE,
                        sources = sources, blockers = blockers
                    )
                }
                CanonicalRecordingExistenceState.AUDIO_AVAILABLE -> {
                    val peerAudio = peer?.audioArtifact
                    if (peerAudio == null || !peerAudio.provesCanonicalAudioAvailability) {
                        blockers.add(CanonicalRecordingExistenceBlocker.PEER_AUDIO_UNPROVEN)
                        return truth(
                            objectID = normalizedObjectID,
                            local = local, peer = peer,
                            localState = localState,
                            peerState = CanonicalRecordingExistenceState.UNSUPPORTED,
                            decision = CanonicalRecordingExistenceDecision.DEFERRED,
                            sources = sources, blockers = blockers
                        )
                    }
                    if (localAudio.contentHash != peerAudio.contentHash) {
                        blockers.add(CanonicalRecordingExistenceBlocker.AUDIO_HASH_MISMATCH)
                        peerState = CanonicalRecordingExistenceState.AUDIO_CONFLICT
                        return truth(
                            objectID = normalizedObjectID,
                            local = local, peer = peer,
                            localState = localState,
                            peerState = peerState,
                            decision = CanonicalRecordingExistenceDecision.CONFLICT,
                            sources = sources, blockers = blockers
                        )
                    }
                    if (localAudio.byteSize != peerAudio.byteSize) {
                        blockers.add(CanonicalRecordingExistenceBlocker.AUDIO_SIZE_MISMATCH)
                        peerState = CanonicalRecordingExistenceState.AUDIO_CONFLICT
                        return truth(
                            objectID = normalizedObjectID,
                            local = local, peer = peer,
                            localState = localState,
                            peerState = peerState,
                            decision = CanonicalRecordingExistenceDecision.CONFLICT,
                            sources = sources, blockers = blockers
                        )
                    }
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = CanonicalRecordingExistenceState.AUDIO_HASH_SIZE_MATCHED,
                        decision = CanonicalRecordingExistenceDecision.AUDIO_SAME_NO_OP,
                        sources = sources, blockers = blockers
                    )
                }
                CanonicalRecordingExistenceState.AUDIO_HASH_SIZE_MATCHED -> {
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = CanonicalRecordingExistenceState.AUDIO_HASH_SIZE_MATCHED,
                        decision = CanonicalRecordingExistenceDecision.AUDIO_SAME_NO_OP,
                        sources = sources, blockers = blockers
                    )
                }
                CanonicalRecordingExistenceState.AUDIO_CONFLICT -> {
                    blockers.add(CanonicalRecordingExistenceBlocker.AUDIO_HASH_MISMATCH)
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = peerState,
                        decision = CanonicalRecordingExistenceDecision.CONFLICT,
                        sources = sources, blockers = blockers
                    )
                }
                CanonicalRecordingExistenceState.PEER_UNKNOWN -> {
                    blockers.add(CanonicalRecordingExistenceBlocker.PEER_UNKNOWN)
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = peerState,
                        decision = CanonicalRecordingExistenceDecision.DEFERRED,
                        sources = sources, blockers = blockers
                    )
                }
                CanonicalRecordingExistenceState.TOMBSTONED -> {
                    blockers.add(CanonicalRecordingExistenceBlocker.TOMBSTONED_PARENT)
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = peerState,
                        decision = CanonicalRecordingExistenceDecision.BLOCKED,
                        sources = sources, blockers = blockers
                    )
                }
                CanonicalRecordingExistenceState.UNSUPPORTED -> {
                    blockers.add(CanonicalRecordingExistenceBlocker.UNSUPPORTED_OBJECT)
                    truth(
                        objectID = normalizedObjectID,
                        local = local, peer = peer,
                        localState = localState,
                        peerState = peerState,
                        decision = CanonicalRecordingExistenceDecision.UNSUPPORTED,
                        sources = sources, blockers = blockers
                    )
                }
            }
        }

        private fun existenceState(
            obj: CanonicalRecordingObject?,
            known: Boolean,
            studyItemExists: Boolean,
            receiveRecordExists: Boolean,
            completedLedgerOnly: Boolean,
            tombstonedParent: Boolean
        ): CanonicalRecordingExistenceState {
            if (!known) return CanonicalRecordingExistenceState.PEER_UNKNOWN
            if (tombstonedParent || obj?.syncState == CanonicalSyncState.DELETED) {
                return CanonicalRecordingExistenceState.TOMBSTONED
            }
            if (obj == null) {
                if (receiveRecordExists) return CanonicalRecordingExistenceState.RECEIVE_RECORD_ONLY
                if (studyItemExists) return CanonicalRecordingExistenceState.STUDY_ITEM_ONLY
                return if (completedLedgerOnly) CanonicalRecordingExistenceState.METADATA_ONLY
                else CanonicalRecordingExistenceState.ABSENT
            }
            if (obj.audioAvailable) return CanonicalRecordingExistenceState.AUDIO_AVAILABLE
            if (receiveRecordExists && studyItemExists) {
                return CanonicalRecordingExistenceState.METADATA_AND_STUDY_ITEM
            }
            if (receiveRecordExists) return CanonicalRecordingExistenceState.RECEIVE_RECORD_ONLY
            if (studyItemExists) return CanonicalRecordingExistenceState.METADATA_AND_STUDY_ITEM
            return CanonicalRecordingExistenceState.METADATA_ONLY
        }

        private fun truth(
            objectID: String,
            local: CanonicalRecordingObject?,
            peer: CanonicalRecordingObject?,
            localState: CanonicalRecordingExistenceState,
            peerState: CanonicalRecordingExistenceState,
            decision: CanonicalRecordingExistenceDecision,
            sources: Set<CanonicalRecordingExistenceSource>,
            blockers: Set<CanonicalRecordingExistenceBlocker>
        ): CanonicalRecordingExistenceTruth {
            return CanonicalRecordingExistenceTruth(
                objectID = objectID,
                localState = localState,
                peerState = peerState,
                decision = decision,
                sources = sources.sortedBy { it.name },
                blockers = blockers.sortedBy { it.name },
                localMetadataHashPrefix = local?.metadataHash?.value?.shortCanonicalPrefix,
                peerMetadataHashPrefix = peer?.metadataHash?.value?.shortCanonicalPrefix,
                localAudioHashPrefix = local?.audioArtifact?.contentHash?.value?.shortCanonicalPrefix,
                peerAudioHashPrefix = peer?.audioArtifact?.contentHash?.value?.shortCanonicalPrefix,
                localByteSize = local?.audioArtifact?.byteSize,
                peerByteSize = peer?.audioArtifact?.byteSize
            )
        }
    }

    fun diagnostics(
        syncRunID: String?,
        mode: CanonicalSyncRuntimeMode
    ): List<CanonicalSyncRuntimeDiagnostic> {
        val output = mutableListOf(
            CanonicalSyncRuntimeDiagnostic(
                kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_TRUTH_EVALUATED,
                syncRunID = syncRunID,
                mode = mode,
                objectID = objectID,
                hashPrefix = localAudioHashPrefix ?: peerAudioHashPrefix
                    ?: localMetadataHashPrefix ?: peerMetadataHashPrefix,
                count = localByteSize?.toInt(),
                detail = "${localState.name}->${peerState.name}:${decision.name}"
            )
        )

        when (decision) {
            CanonicalRecordingExistenceDecision.APPLY_METADATA_ONLY_BRIDGE -> {
                output.add(
                    CanonicalSyncRuntimeDiagnostic(
                        kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_PEER_ABSENT_METADATA_BRIDGE_REQUIRED,
                        syncRunID = syncRunID, mode = mode,
                        objectID = objectID,
                        hashPrefix = localMetadataHashPrefix,
                        detail = peerState.name
                    )
                )
            }
            CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE -> {
                output.add(
                    CanonicalSyncRuntimeDiagnostic(
                        kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_PEER_METADATA_ONLY_UPLOAD_CANDIDATE,
                        syncRunID = syncRunID, mode = mode,
                        objectID = objectID,
                        hashPrefix = localAudioHashPrefix,
                        count = localByteSize?.toInt(),
                        detail = peerState.name
                    )
                )
            }
            CanonicalRecordingExistenceDecision.AUDIO_SAME_NO_OP -> {
                output.add(
                    CanonicalSyncRuntimeDiagnostic(
                        kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_AUDIO_SAME_NO_OP,
                        syncRunID = syncRunID, mode = mode,
                        objectID = objectID,
                        hashPrefix = localAudioHashPrefix,
                        count = localByteSize?.toInt(),
                        detail = "sameHashAndSize"
                    )
                )
            }
            CanonicalRecordingExistenceDecision.CONFLICT -> {
                output.add(
                    CanonicalSyncRuntimeDiagnostic(
                        kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_AUDIO_CONFLICT,
                        syncRunID = syncRunID, mode = mode,
                        objectID = objectID,
                        hashPrefix = localAudioHashPrefix,
                        count = localByteSize?.toInt(),
                        detail = blockers.joinToString("+") { it.name.lowercase() }
                    )
                )
            }
            CanonicalRecordingExistenceDecision.DEFERRED -> {
                if (peerState == CanonicalRecordingExistenceState.PEER_UNKNOWN) {
                    output.add(
                        CanonicalSyncRuntimeDiagnostic(
                            kind = CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_PEER_UNKNOWN_DEFERRED,
                            syncRunID = syncRunID, mode = mode,
                            objectID = objectID,
                            detail = "peerUnknown"
                        )
                    )
                }
            }
            else -> {}
        }

        return output
    }
}

// ── Type 22: CanonicalExistenceApplyRuntimeMode ──

enum class CanonicalExistenceApplyRuntimeMode {
    DISABLED,
    DIAGNOSTICS_ONLY,
    NO_COMMIT,
    TEST_ROOT_APPLY,
    PRODUCTION_ROOT_APPLY,
    BLOCKED;

    val evaluatesCandidates: Boolean
        get() = this != BLOCKED

    val canCommitMetadataOnlyRecord: Boolean
        get() = this == TEST_ROOT_APPLY || this == PRODUCTION_ROOT_APPLY
}

// ── Type 23: CanonicalExistenceApplyRuntimePolicy ──

data class CanonicalExistenceApplyRuntimePolicy(
    val debugInternalBuild: Boolean,
    val ownerApproved: Boolean,
    val releaseDefaultBuild: Boolean,
    val diagnosticsRedacted: Boolean,
    val legacyFallbackAvailable: Boolean,
    val rootBoundRequired: Boolean,
    val rollbackRequired: Boolean,
    val atomicWriteRequired: Boolean,
    val postconditionRequired: Boolean,
    val writeAudioAllowed: Boolean,
    val markAudioAvailableAllowed: Boolean
) {
    constructor() : this(
        debugInternalBuild = false,
        ownerApproved = false,
        releaseDefaultBuild = true,
        diagnosticsRedacted = true,
        legacyFallbackAvailable = true,
        rootBoundRequired = true,
        rollbackRequired = true,
        atomicWriteRequired = true,
        postconditionRequired = true,
        writeAudioAllowed = false,
        markAudioAvailableAllowed = false
    )
}

// ── Type 24: CanonicalExistenceApplyRuntimeConfiguration ──

data class CanonicalExistenceApplyRuntimeConfiguration(
    val mode: CanonicalExistenceApplyRuntimeMode,
    val policy: CanonicalExistenceApplyRuntimePolicy
) {
    constructor() : this(
        mode = CanonicalExistenceApplyRuntimeMode.DISABLED,
        policy = CanonicalExistenceApplyRuntimePolicy()
    )

    companion object {
        val DISABLED = CanonicalExistenceApplyRuntimeConfiguration()
    }

    val canWriteMetadataOnlyRecord: Boolean
        get() {
            if (!mode.canCommitMetadataOnlyRecord ||
                !policy.diagnosticsRedacted ||
                !policy.legacyFallbackAvailable ||
                !policy.rootBoundRequired ||
                !policy.rollbackRequired ||
                !policy.atomicWriteRequired ||
                !policy.postconditionRequired ||
                policy.writeAudioAllowed ||
                policy.markAudioAvailableAllowed
            ) return false
            if (mode == CanonicalExistenceApplyRuntimeMode.PRODUCTION_ROOT_APPLY) {
                return policy.debugInternalBuild && policy.ownerApproved && !policy.releaseDefaultBuild
            }
            return mode == CanonicalExistenceApplyRuntimeMode.TEST_ROOT_APPLY
        }
}

// ── Type 25: String extension properties ──

val String.nilIfEmpty: String?
    get() = if (isEmpty()) null else this

val String.shortCanonicalPrefix: String
    get() = trim().take(12)
