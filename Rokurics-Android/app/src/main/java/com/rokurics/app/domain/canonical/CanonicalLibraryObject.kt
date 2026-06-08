package com.rokurics.app.domain.canonical

data class CanonicalLibraryObjectID(
    val rawValue: String
) {
    constructor(value: String, fallback: String = "unknownUnsupported:unknown") : this(
        value.trim().nilIfEmpty ?: fallback
    )
}

enum class CanonicalObjectKind(val value: String) {
    RECORDING("recording"),
    FOLDER("folder"),
    STANDALONE_STUDY_ITEM("standaloneStudyItem"),
    STANDALONE_NOTE("standaloneNote"),
    RECORDING_ASSOCIATED_STUDY_ITEM("recordingAssociatedStudyItem"),
    GENERATED_ARTIFACT_ENVELOPE("generatedArtifactEnvelope"),
    UNKNOWN_UNSUPPORTED("unknownUnsupported")
}

enum class CanonicalStudyItemKind(val value: String) {
    RECORDING_BUNDLE("recordingBundle"),
    STANDALONE_NOTE("standaloneNote"),
    EXTERNAL_RESOURCE("externalResource"),
    UNKNOWN("unknown")
}

data class CanonicalParentReference(
    val parentID: CanonicalLibraryObjectID,
    val relation: String = "parent"
)

data class CanonicalHierarchyPath(
    val components: List<String> = emptyList()
) {

    val stableKey: String
        get() = components.joinToString(separator = "\u001F")

    companion object {
        fun normalized(value: String): String? = value.trim().nilIfEmpty
    }
}

data class CanonicalFolderMetadata(
    val folderID: CanonicalLibraryObjectID,
    val name: String,
    val parentID: CanonicalLibraryObjectID? = null,
    val hierarchyPath: CanonicalHierarchyPath = CanonicalHierarchyPath(),
    val hierarchyLevel: String? = null,
    val colorToken: String? = null,
    val orderingKey: String? = null,
    val isDeleted: Boolean = false,
    val deletedAt: CanonicalTimestamp? = null,
    val businessModifiedAt: CanonicalTimestamp = CanonicalTimestamp(java.util.Date())
) {
    val metadataHash: CanonicalHash
        get() = CanonicalHash.sha256(CanonicalProjectionContract.metadataHashPayload(this))
}

data class CanonicalStudyItemMetadata(
    val itemID: CanonicalLibraryObjectID,
    val itemKind: CanonicalStudyItemKind,
    val title: String,
    val filingPath: CanonicalHierarchyPath = CanonicalHierarchyPath(),
    val folderIDs: List<CanonicalLibraryObjectID> = emptyList(),
    val parentReferences: List<CanonicalParentReference> = emptyList(),
    val tags: List<String> = emptyList(),
    val logicalResourceTokens: List<String> = emptyList(),
    val associatedRecordingID: String? = null,
    val isDeleted: Boolean = false,
    val deletedAt: CanonicalTimestamp? = null,
    val businessModifiedAt: CanonicalTimestamp = CanonicalTimestamp(java.util.Date())
) {

    val metadataHash: CanonicalHash
        get() = CanonicalHash.sha256(CanonicalProjectionContract.metadataHashPayload(this))
}

data class CanonicalStandaloneNoteMetadata(
    val noteID: CanonicalLibraryObjectID,
    val title: String,
    val filingPath: CanonicalHierarchyPath = CanonicalHierarchyPath(),
    val folderIDs: List<CanonicalLibraryObjectID> = emptyList(),
    val isDeleted: Boolean = false,
    val deletedAt: CanonicalTimestamp? = null,
    val tags: List<String> = emptyList(),
    val businessModifiedAt: CanonicalTimestamp = CanonicalTimestamp(java.util.Date())
) {

    val metadataHash: CanonicalHash
        get() = CanonicalHash.sha256(mapOf(
            "schema" to "canonical-standalone-note-business-metadata-v1",
            "noteID" to noteID.rawValue,
            "title" to title,
            "filingPath" to filingPath.stableKey,
            "folderIDs" to folderIDs.joinToString(separator = "\u001F") { it.rawValue },
            "tags" to tags.joinToString(separator = "\u001F"),
            "isDeleted" to if (isDeleted) "true" else "false",
            "deletedAt" to (deletedAt?.let { timestampString(it) } ?: "")
        ))

    companion object {
        private fun timestampString(timestamp: CanonicalTimestamp): String =
            String.format(java.util.Locale.US, "%.6f", timestamp.date.time / 1000.0)
    }
}

data class CanonicalFolderObject(
    val folderID: CanonicalLibraryObjectID,
    val metadata: CanonicalFolderMetadata,
    val metadataHash: CanonicalHash,
    val isDeleted: Boolean,
    val deletedAt: CanonicalTimestamp?,
    val businessModifiedAt: CanonicalTimestamp
) {
    constructor(metadata: CanonicalFolderMetadata) : this(
        folderID = metadata.folderID,
        metadata = metadata,
        metadataHash = metadata.metadataHash,
        isDeleted = metadata.isDeleted,
        deletedAt = metadata.deletedAt,
        businessModifiedAt = metadata.businessModifiedAt
    )
}

data class CanonicalStudyItemObject(
    val itemID: CanonicalLibraryObjectID,
    val metadata: CanonicalStudyItemMetadata,
    val metadataHash: CanonicalHash,
    val isDeleted: Boolean,
    val deletedAt: CanonicalTimestamp?,
    val businessModifiedAt: CanonicalTimestamp
) {
    constructor(metadata: CanonicalStudyItemMetadata) : this(
        itemID = metadata.itemID,
        metadata = metadata,
        metadataHash = metadata.metadataHash,
        isDeleted = metadata.isDeleted,
        deletedAt = metadata.deletedAt,
        businessModifiedAt = metadata.businessModifiedAt
    )
}

data class CanonicalStandaloneNoteObject(
    val noteID: CanonicalLibraryObjectID,
    val metadata: CanonicalStandaloneNoteMetadata,
    val metadataHash: CanonicalHash,
    val isDeleted: Boolean,
    val deletedAt: CanonicalTimestamp?,
    val businessModifiedAt: CanonicalTimestamp
) {
    constructor(metadata: CanonicalStandaloneNoteMetadata) : this(
        noteID = metadata.noteID,
        metadata = metadata,
        metadataHash = metadata.metadataHash,
        isDeleted = metadata.isDeleted,
        deletedAt = metadata.deletedAt,
        businessModifiedAt = metadata.businessModifiedAt
    )
}

enum class CanonicalLibraryTombstoneReason(val value: String) {
    USER_DELETE("userDelete"),
    CONFLICT_RESOLUTION("conflictResolution"),
    PARENT_TOMBSTONE("parentTombstone"),
    MANUAL_CLEANUP("manualCleanup"),
    UNKNOWN("unknown")
}

enum class CanonicalTombstonePolicy(val value: String) {
    SOFT_DELETE_ONLY("softDeleteOnly"),
    ANTI_RESURRECTION("antiResurrection"),
    NO_PHYSICAL_DELETE("noPhysicalDelete"),
    NO_PERMANENT_DELETE("noPermanentDelete"),
    NO_GARBAGE_COLLECTION("noGarbageCollection")
}

data class CanonicalLibraryTombstone(
    val tombstoneID: String,
    val objectID: CanonicalLibraryObjectID,
    val objectKind: CanonicalObjectKind,
    val deletedAt: CanonicalTimestamp?,
    val sourceNodeID: String?,
    val reason: CanonicalLibraryTombstoneReason,
    val policies: List<CanonicalTombstonePolicy>
) {
    constructor(
        objectID: CanonicalLibraryObjectID,
        objectKind: CanonicalObjectKind,
        deletedAt: CanonicalTimestamp?,
        sourceNodeID: String? = null,
        reason: CanonicalLibraryTombstoneReason,
        policies: List<CanonicalTombstonePolicy> = listOf(
            CanonicalTombstonePolicy.SOFT_DELETE_ONLY,
            CanonicalTombstonePolicy.ANTI_RESURRECTION,
            CanonicalTombstonePolicy.NO_PHYSICAL_DELETE,
            CanonicalTombstonePolicy.NO_PERMANENT_DELETE,
            CanonicalTombstonePolicy.NO_GARBAGE_COLLECTION
        )
    ) : this(
        tombstoneID = listOf("libraryTombstone", objectKind.value, objectID.rawValue)
            .joinToString(separator = "|"),
        objectID = objectID,
        objectKind = objectKind,
        deletedAt = deletedAt,
        sourceNodeID = sourceNodeID?.trim()?.nilIfEmpty,
        reason = reason,
        policies = policies.toSet().sortedBy { it.value }
    )
}

data class CanonicalLibraryObject(
    val objectID: CanonicalLibraryObjectID,
    val kind: CanonicalObjectKind,
    val folder: CanonicalFolderObject? = null,
    val studyItem: CanonicalStudyItemObject? = null,
    val standaloneNote: CanonicalStandaloneNoteObject? = null
) {
    constructor(
        objectID: CanonicalLibraryObjectID,
        kind: CanonicalObjectKind,
        folder: CanonicalFolderObject? = null,
        studyItem: CanonicalStudyItemObject? = null,
        standaloneNote: CanonicalStandaloneNoteObject? = null,
        unsupportedReason: String? = null
    ) : this(
        objectID = objectID,
        kind = kind,
        folder = folder,
        studyItem = studyItem,
        standaloneNote = standaloneNote
    )

    val metadataHash: CanonicalHash
        get() = when (kind) {
            CanonicalObjectKind.FOLDER ->
                folder?.metadataHash ?: CanonicalHash.sha256String(objectID.rawValue)
            CanonicalObjectKind.STANDALONE_STUDY_ITEM,
            CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM ->
                studyItem?.metadataHash ?: CanonicalHash.sha256String(objectID.rawValue)
            CanonicalObjectKind.STANDALONE_NOTE ->
                standaloneNote?.metadataHash
                    ?: studyItem?.metadataHash
                    ?: CanonicalHash.sha256String(objectID.rawValue)
            CanonicalObjectKind.RECORDING ->
                CanonicalHash.sha256String(objectID.rawValue)
            CanonicalObjectKind.GENERATED_ARTIFACT_ENVELOPE,
            CanonicalObjectKind.UNKNOWN_UNSUPPORTED ->
                CanonicalHash.sha256(
                    mapOf(
                        "schema" to "canonical-library-object-unsupported-v1",
                        "objectID" to objectID.rawValue,
                        "kind" to kind.value,
                        "reason" to ""
                    )
                )
        }

    val businessModifiedAt: CanonicalTimestamp?
        get() = folder?.businessModifiedAt ?: studyItem?.businessModifiedAt

    val isDeleted: Boolean
        get() = folder?.isDeleted ?: studyItem?.isDeleted ?: false

    val deletedAt: CanonicalTimestamp?
        get() = folder?.deletedAt ?: studyItem?.deletedAt
}
