/**
 * CanonicalInventoryBuilderContract — inventory snapshot to manifest builder.
 * Ported from Rokurics-Apple (CanonicalInventoryBuilderContract.swift).
 * Builds a CanonicalManifest from an inventory snapshot with coverage reporting.
 */

import {
  CanonicalTimestamp, CanonicalCapability, CanonicalNode, CanonicalManifest,
  CanonicalRecordingObject, CanonicalLibraryObject, CanonicalObjectKind,
  CanonicalLibraryObjectID, CanonicalLibraryTombstone,
  CanonicalFolderObject, CanonicalStudyItemObject, CanonicalStandaloneNoteObject
} from './CanonicalCore'
import { provesGeneratedArtifactAvailability, generatedArtifactKinds } from './CanonicalCore'

// ── Inventory Unsupported Object ──

export class CanonicalInventoryUnsupportedObject {
  objectID: CanonicalLibraryObjectID
  objectKind: CanonicalObjectKind
  reason: string

  constructor(objectID: CanonicalLibraryObjectID, objectKind: CanonicalObjectKind, reason: string) {
    this.objectID = objectID
    this.objectKind = objectKind
    this.reason = reason
  }

  get id(): string {
    return this.objectID.rawValue
  }
}

// ── Inventory Input Snapshot ──

export class CanonicalInventoryInputSnapshot {
  node: CanonicalNode
  generatedAt: CanonicalTimestamp
  recordingObjects: CanonicalRecordingObject[]
  libraryObjects: CanonicalLibraryObject[]
  libraryTombstones: CanonicalLibraryTombstone[]
  unsupportedObjects: CanonicalInventoryUnsupportedObject[]

  constructor(
    node: CanonicalNode,
    generatedAt: Date = new Date(),
    recordingObjects: CanonicalRecordingObject[] = [],
    libraryObjects: CanonicalLibraryObject[] = [],
    libraryTombstones: CanonicalLibraryTombstone[] = [],
    unsupportedObjects: CanonicalInventoryUnsupportedObject[] = []
  ) {
    this.node = node
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.recordingObjects = recordingObjects
    this.libraryObjects = libraryObjects
    this.libraryTombstones = libraryTombstones
    this.unsupportedObjects = unsupportedObjects
  }
}

// ── Inventory Coverage Report ──

export class CanonicalInventoryCoverageReport {
  recordingCoverage: number
  audioCoverage: number
  generatedArtifactCoverage: number
  folderCoverage: number
  studyItemCoverage: number
  tombstoneCoverage: number
  unsupportedLegacyObjectCount: number
  fallbackRequiredCount: number

  constructor(
    recordingCoverage: number,
    audioCoverage: number,
    generatedArtifactCoverage: number,
    folderCoverage: number,
    studyItemCoverage: number,
    tombstoneCoverage: number,
    unsupportedLegacyObjectCount: number,
    fallbackRequiredCount: number
  ) {
    this.recordingCoverage = recordingCoverage
    this.audioCoverage = audioCoverage
    this.generatedArtifactCoverage = generatedArtifactCoverage
    this.folderCoverage = folderCoverage
    this.studyItemCoverage = studyItemCoverage
    this.tombstoneCoverage = tombstoneCoverage
    this.unsupportedLegacyObjectCount = unsupportedLegacyObjectCount
    this.fallbackRequiredCount = fallbackRequiredCount
  }
}

// ── Inventory Build Diagnostics ──

export class CanonicalInventoryBuildDiagnostics {
  phases: string[]
  unsupportedReasons: string[]

  constructor(phases: string[], unsupportedReasons: string[]) {
    this.phases = phases
    this.unsupportedReasons = unsupportedReasons
  }
}

// ── Inventory Build Result ──

export class CanonicalInventoryBuildResult {
  manifest: CanonicalManifest
  coverage: CanonicalInventoryCoverageReport
  diagnostics: CanonicalInventoryBuildDiagnostics

  constructor(
    manifest: CanonicalManifest,
    coverage: CanonicalInventoryCoverageReport,
    diagnostics: CanonicalInventoryBuildDiagnostics
  ) {
    this.manifest = manifest
    this.coverage = coverage
    this.diagnostics = diagnostics
  }
}

// ── Inventory Builder Contract ──

export class CanonicalInventoryBuilderContract {
  build(snapshot: CanonicalInventoryInputSnapshot): CanonicalInventoryBuildResult {
    const folders = snapshot.libraryObjects
      .map(o => o.folder)
      .filter(f => f !== null) as CanonicalFolderObject[]
    const studyItems = snapshot.libraryObjects
      .map(o => o.studyItem ?? o.standaloneNote?.studyItem ?? null)
      .filter(s => s !== null) as CanonicalStudyItemObject[]
    const standaloneNotes = snapshot.libraryObjects
      .map(o => o.standaloneNote)
      .filter(n => n !== null) as CanonicalStandaloneNoteObject[]

    let generatedArtifactCount = 0
    for (const obj of snapshot.recordingObjects) {
      for (const a of obj.artifacts) {
        if (generatedArtifactKinds().indexOf(a.kind) >= 0) {
          generatedArtifactCount++
        }
      }
    }

    let audioCoverage = 0
    for (const obj of snapshot.recordingObjects) {
      if (obj.audioAvailable) audioCoverage++
    }

    const capabilities: CanonicalCapability[] = [
      CanonicalCapability.CANONICAL_LIBRARY_OBJECTS_V1,
      CanonicalCapability.CANONICAL_FOLDER_OBJECTS_V1,
      CanonicalCapability.CANONICAL_STUDY_ITEM_OBJECTS_V1,
      CanonicalCapability.CANONICAL_INVENTORY_BUILDER_V1
    ]

    const manifest = CanonicalManifest.make(
      snapshot.node,
      snapshot.generatedAt.date,
      snapshot.recordingObjects,
      snapshot.libraryObjects,
      folders,
      studyItems,
      standaloneNotes,
      snapshot.libraryTombstones,
      capabilities
    )

    const coverage = new CanonicalInventoryCoverageReport(
      snapshot.recordingObjects.length,
      audioCoverage,
      generatedArtifactCount,
      folders.length,
      studyItems.length,
      snapshot.libraryTombstones.length,
      snapshot.unsupportedObjects.length,
      snapshot.unsupportedObjects.length
    )

    const diagnostics = new CanonicalInventoryBuildDiagnostics(
      [
        'canonicalInventoryCoverageReportWritten',
        'canonicalLibraryObjectsProjected'
      ],
      snapshot.unsupportedObjects.map(o => o.reason).sort()
    )

    return new CanonicalInventoryBuildResult(manifest, coverage, diagnostics)
  }
}
