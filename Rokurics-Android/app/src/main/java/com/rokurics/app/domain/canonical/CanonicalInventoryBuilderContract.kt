package com.rokurics.app.domain.canonical

import java.util.Date

data class CanonicalInventoryUnsupportedObject(
    val objectID: CanonicalLibraryObjectID,
    val objectKind: CanonicalObjectKind,
    val reason: String
) {
    val id: String get() = objectID.rawValue
}

data class CanonicalInventoryInputSnapshot(
    val node: CanonicalNode,
    val generatedAt: CanonicalTimestamp = CanonicalTimestamp(Date()),
    val recordingObjects: List<CanonicalRecordingObject> = emptyList(),
    val libraryObjects: List<CanonicalLibraryObject> = emptyList(),
    val libraryTombstones: List<CanonicalLibraryTombstone> = emptyList(),
    val unsupportedObjects: List<CanonicalInventoryUnsupportedObject> = emptyList()
)

data class CanonicalInventoryCoverageReport(
    val recordingCoverage: Int,
    val audioCoverage: Int,
    val generatedArtifactCoverage: Int,
    val folderCoverage: Int,
    val studyItemCoverage: Int,
    val tombstoneCoverage: Int,
    val unsupportedLegacyObjectCount: Int,
    val fallbackRequiredCount: Int
)

data class CanonicalInventoryBuildDiagnostics(
    val phases: List<String>,
    val unsupportedReasons: List<String>
)

data class CanonicalInventoryBuildResult(
    val manifest: CanonicalManifest,
    val coverage: CanonicalInventoryCoverageReport,
    val diagnostics: CanonicalInventoryBuildDiagnostics
)

class CanonicalInventoryBuilderContract {
    fun build(from: CanonicalInventoryInputSnapshot): CanonicalInventoryBuildResult {
        val folders = from.libraryObjects.mapNotNull { it.folder }
        val studyItems = from.libraryObjects.mapNotNull { obj ->
            obj.studyItem
        }
        val standaloneNotes = from.libraryObjects.mapNotNull { it.standaloneNote }
        val generatedArtifactCount = from.recordingObjects.sumOf { obj ->
            obj.artifacts.count { CanonicalProjectionContract.generatedArtifactKinds.contains(it.kind) }
        }
        val audioCoverage = from.recordingObjects.count { it.audioAvailable }
        val fallbackCount = from.unsupportedObjects.size
        val capabilities = listOf(
            CanonicalCapability.CANONICAL_LIBRARY_OBJECTS_V1,
            CanonicalCapability.CANONICAL_FOLDER_OBJECTS_V1,
            CanonicalCapability.CANONICAL_STUDY_ITEM_OBJECTS_V1,
            CanonicalCapability.CANONICAL_INVENTORY_BUILDER_V1
        )
        val manifest = CanonicalManifest.make(
            node = from.node,
            generatedAt = from.generatedAt.date,
            objects = from.recordingObjects,
            libraryObjects = from.libraryObjects,
            folders = folders,
            studyItems = studyItems,
            standaloneNotes = standaloneNotes,
            libraryTombstones = from.libraryTombstones,
            manifestCapabilities = capabilities
        )
        val coverage = CanonicalInventoryCoverageReport(
            recordingCoverage = from.recordingObjects.size,
            audioCoverage = audioCoverage,
            generatedArtifactCoverage = generatedArtifactCount,
            folderCoverage = folders.size,
            studyItemCoverage = studyItems.size,
            tombstoneCoverage = from.libraryTombstones.size,
            unsupportedLegacyObjectCount = from.unsupportedObjects.size,
            fallbackRequiredCount = fallbackCount
        )
        val diagnostics = CanonicalInventoryBuildDiagnostics(
            phases = listOf(
                "canonicalInventoryCoverageReportWritten",
                "canonicalLibraryObjectsProjected"
            ),
            unsupportedReasons = from.unsupportedObjects.map { it.reason }.sorted()
        )
        return CanonicalInventoryBuildResult(
            manifest = manifest,
            coverage = coverage,
            diagnostics = diagnostics
        )
    }
}
