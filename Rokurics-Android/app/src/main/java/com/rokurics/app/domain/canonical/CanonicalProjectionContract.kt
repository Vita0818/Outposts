package com.rokurics.app.domain.canonical

import java.util.Locale

enum class CanonicalArtifactProducer(val value: String) {
    AUDIO_CAPTURE("audioCapture"),
    TRANSCRIPTION("transcription"),
    NOTE_GENERATION("noteGeneration"),
    UNKNOWN("unknown");
}

object CanonicalProjectionContract {

    private fun normalizedRequired(value: String, fallback: String): String =
        value.trim().nilIfEmpty ?: fallback

    private fun timestampString(timestamp: CanonicalTimestamp): String =
        String.format(Locale.US, "%.6f", timestamp.date.time / 1000.0)

    val generatedArtifactKinds: Set<CanonicalArtifact.Kind> = setOf(
        CanonicalArtifact.Kind.TRANSCRIPT_JSON,
        CanonicalArtifact.Kind.TRANSCRIPT_MARKDOWN,
        CanonicalArtifact.Kind.NOTE_MARKDOWN,
        CanonicalArtifact.Kind.NOTE_JSON,
        CanonicalArtifact.Kind.SUMMARY_JSON
    )

    fun artifactID(objectID: String, kind: CanonicalArtifact.Kind): String =
        kind.artifactID(normalizedRequired(objectID, "unknown-recording"))

    fun artifactKey(objectID: String, kind: CanonicalArtifact.Kind): String =
        "${normalizedRequired(objectID, "unknown-recording")}|${kind.name.lowercase()}"

    fun makeCanonicalFolderID(folderID: String): CanonicalLibraryObjectID =
        CanonicalLibraryObjectID(folderID, "folder:unknown")

    fun makeCanonicalStudyItemID(itemID: String): CanonicalLibraryObjectID =
        CanonicalLibraryObjectID(itemID, "studyItem:unknown")

    fun normalizeFolderName(name: String): String =
        name.trim().nilIfEmpty ?: "未命名文件夹"

    fun normalizeStudyItemTitle(title: String, itemKind: CanonicalStudyItemKind = CanonicalStudyItemKind.UNKNOWN): String =
        title.trim().nilIfEmpty ?: if (itemKind == CanonicalStudyItemKind.STANDALONE_NOTE) "未命名笔记" else "未命名条目"

    fun normalizeTags(tags: List<String>): List<String> =
        tags.mapNotNull { it.trim().nilIfEmpty?.lowercase() }
            .toSet()
            .sorted()

    fun normalizeFilingPath(path: CanonicalHierarchyPath): CanonicalHierarchyPath =
        CanonicalHierarchyPath(path.components)

    fun normalizeParentReferences(references: List<CanonicalParentReference>): List<CanonicalParentReference> {
        val seen = mutableSetOf<String>()
        return references
            .sortedBy { it.parentID.rawValue }
            .filter { reference ->
                val key = "${reference.relation}|${reference.parentID.rawValue}"
                seen.add(key)
            }
    }

    fun normalizeTombstone(isDeleted: Boolean, deletedAt: CanonicalTimestamp?): CanonicalTimestamp? =
        if (isDeleted) deletedAt else null

    fun metadataHashPayload(folder: CanonicalFolderMetadata): Map<String, String> = mapOf(
        "schema" to "canonical-folder-business-metadata-v1",
        "folderID" to folder.folderID.rawValue,
        "name" to folder.name,
        "parentID" to (folder.parentID?.rawValue ?: ""),
        "hierarchyPath" to folder.hierarchyPath.stableKey,
        "hierarchyLevel" to (folder.hierarchyLevel ?: ""),
        "colorToken" to (folder.colorToken ?: ""),
        "orderingKey" to (folder.orderingKey ?: ""),
        "isDeleted" to if (folder.isDeleted) "true" else "false",
        "deletedAt" to (folder.deletedAt?.let { timestampString(it) } ?: "")
    )

    fun metadataHashPayload(item: CanonicalStudyItemMetadata): Map<String, String> = mapOf(
        "schema" to "canonical-study-item-business-metadata-v1",
        "itemID" to item.itemID.rawValue,
        "itemKind" to item.itemKind.value,
        "title" to item.title,
        "filingPath" to item.filingPath.stableKey,
        "folderIDs" to item.folderIDs.joinToString(separator = "\u001F") { it.rawValue },
        "parentReferences" to item.parentReferences.joinToString(separator = "\u001F") { "${it.relation}:${it.parentID.rawValue}" },
        "tags" to item.tags.joinToString(separator = "\u001F"),
        "resourceTokens" to item.logicalResourceTokens.joinToString(separator = "\u001F"),
        "associatedRecordingID" to (item.associatedRecordingID ?: ""),
        "isDeleted" to if (item.isDeleted) "true" else "false",
        "deletedAt" to (item.deletedAt?.let { timestampString(it) } ?: "")
    )

    fun objectKind(
        legacyItemKind: String,
        recordingID: String?
    ): CanonicalObjectKind {
        val kind = legacyItemKind.trim()
        if (recordingID?.trim()?.nilIfEmpty != null) {
            return CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM
        }
        if (kind == "standaloneNote") {
            return CanonicalObjectKind.STANDALONE_NOTE
        }
        return if (kind.isEmpty()) CanonicalObjectKind.UNKNOWN_UNSUPPORTED else CanonicalObjectKind.STANDALONE_STUDY_ITEM
    }

    fun safeLogicalResourceToken(token: String?): String? =
        safeLogicalPathToken(token)

    fun safeLogicalPathToken(token: String?): String? {
        val trimmed = token?.trim()?.takeIf { it.isNotEmpty() } ?: return null
        if (trimmed.startsWith("/") || trimmed.contains("://") || trimmed.contains("\\")) return null
        if (trimmed.contains("//") || trimmed.endsWith("/")) return null
        val components = trimmed.split("/")
        if (components.isEmpty() || components.any { it == "." || it == ".." }) return null
        return trimmed
    }

    fun logicalName(token: String?): String? {
        val safeToken = safeLogicalPathToken(token) ?: return null
        return safeToken.split("/").lastOrNull()
    }

    fun producer(kind: CanonicalArtifact.Kind, platform: String): CanonicalArtifactProducer {
        val normalizedPlatform = platform.trim().lowercase()
        return when (kind) {
            CanonicalArtifact.Kind.AUDIO ->
                if (normalizedPlatform.contains("iphone")) CanonicalArtifactProducer.AUDIO_CAPTURE else CanonicalArtifactProducer.UNKNOWN
            CanonicalArtifact.Kind.TRANSCRIPT_JSON, CanonicalArtifact.Kind.TRANSCRIPT_MARKDOWN ->
                if (normalizedPlatform.contains("mac")) CanonicalArtifactProducer.TRANSCRIPTION else CanonicalArtifactProducer.UNKNOWN
            CanonicalArtifact.Kind.NOTE_MARKDOWN, CanonicalArtifact.Kind.NOTE_JSON, CanonicalArtifact.Kind.SUMMARY_JSON ->
                if (normalizedPlatform.contains("mac")) CanonicalArtifactProducer.NOTE_GENERATION else CanonicalArtifactProducer.UNKNOWN
            CanonicalArtifact.Kind.METADATA, CanonicalArtifact.Kind.RECEIVE_RECORD ->
                CanonicalArtifactProducer.UNKNOWN
        }
    }

    fun requiredCapability(kind: CanonicalArtifact.Kind): CanonicalCapability? {
        return when (kind) {
            CanonicalArtifact.Kind.AUDIO -> CanonicalCapability.AUDIO_ARTIFACT
            CanonicalArtifact.Kind.TRANSCRIPT_JSON, CanonicalArtifact.Kind.TRANSCRIPT_MARKDOWN -> CanonicalCapability.TRANSCRIPT_ARTIFACT
            CanonicalArtifact.Kind.NOTE_MARKDOWN, CanonicalArtifact.Kind.NOTE_JSON -> CanonicalCapability.NOTE_ARTIFACT
            CanonicalArtifact.Kind.SUMMARY_JSON -> CanonicalCapability.SUMMARY_ARTIFACT
            CanonicalArtifact.Kind.METADATA, CanonicalArtifact.Kind.RECEIVE_RECORD -> null
        }
    }

    fun availability(
        isPresent: Boolean,
        contentHash: CanonicalHash?,
        byteSize: Long?
    ): CanonicalArtifact.Availability {
        if (!isPresent) return CanonicalArtifact.Availability.MISSING
        return if (contentHash != null && byteSize != null)
            CanonicalArtifact.Availability.AVAILABLE
        else
            CanonicalArtifact.Availability.AVAILABLE_WITHOUT_HASH
    }

    fun makeArtifact(
        objectID: String,
        kind: CanonicalArtifact.Kind,
        availability: CanonicalArtifact.Availability,
        contentHash: CanonicalHash? = null,
        byteSize: Long? = null,
        logicalPathToken: String? = null,
        modifiedAt: CanonicalTimestamp? = null,
        observedAt: CanonicalTimestamp? = null,
        producedByNodeID: String? = null,
        platform: String
    ): CanonicalArtifact {
        val safeToken = safeLogicalPathToken(logicalPathToken)
        val producer = producer(kind, platform)
        return CanonicalArtifact(
            artifactID = artifactID(objectID, kind),
            objectID = objectID,
            kind = kind,
            availability = availability,
            contentHash = contentHash,
            byteSize = byteSize,
            logicalName = logicalName(safeToken),
            logicalPathToken = safeToken,
            modifiedAt = modifiedAt,
            observedAt = observedAt,
            producedBy = if (producer == CanonicalArtifactProducer.UNKNOWN) null else producer,
            producedByNodeID = producedByNodeID,
            tombstone = null
        )
    }

    fun provesGeneratedArtifactAvailability(artifact: CanonicalArtifact?): Boolean {
        val a = artifact ?: return false
        return generatedArtifactKinds.contains(a.kind) &&
            a.availability == CanonicalArtifact.Availability.AVAILABLE &&
            a.contentHash != null &&
            a.byteSize != null &&
            a.tombstone != true
    }

    fun sameContent(left: CanonicalArtifact, right: CanonicalArtifact): Boolean {
        return left.contentHash?.algorithm == right.contentHash?.algorithm &&
            left.contentHash?.value == right.contentHash?.value &&
            left.byteSize == right.byteSize &&
            left.contentHash != null &&
            left.byteSize != null
    }

    fun isAuthoritativeProducer(artifact: CanonicalArtifact, node: CanonicalNode): Boolean {
        if (artifact.tombstone == true) return false
        val requiredCap = requiredCapability(artifact.kind) ?: return false
        if (!node.capabilities.contains(requiredCap)) return false
        if (artifact.producedByNodeID != null && artifact.producedByNodeID != node.nodeID) return false
        return when (artifact.kind) {
            CanonicalArtifact.Kind.AUDIO ->
                artifact.producedBy == CanonicalArtifactProducer.AUDIO_CAPTURE &&
                    node.platform.lowercase().contains("iphone")
            CanonicalArtifact.Kind.TRANSCRIPT_JSON, CanonicalArtifact.Kind.TRANSCRIPT_MARKDOWN ->
                artifact.producedBy == CanonicalArtifactProducer.TRANSCRIPTION &&
                    node.platform.lowercase().contains("mac")
            CanonicalArtifact.Kind.NOTE_MARKDOWN, CanonicalArtifact.Kind.NOTE_JSON, CanonicalArtifact.Kind.SUMMARY_JSON ->
                artifact.producedBy == CanonicalArtifactProducer.NOTE_GENERATION &&
                    node.platform.lowercase().contains("mac")
            CanonicalArtifact.Kind.METADATA, CanonicalArtifact.Kind.RECEIVE_RECORD -> false
        }
    }
}
