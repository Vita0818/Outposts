/**
 * CanonicalShadowDiagnostics — shadow comparison diagnostics and reporting.
 * Ported from Rokurics-Apple (CanonicalShadowDiagnostics.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalNode, CanonicalManifest,
  CanonicalRecordingObject, CanonicalArtifact, CanonicalArtifactKind,
  CanonicalProjectionContract, nilIfEmpty, hashPrefix
} from './CanonicalCore'

// =============================================================================
// SHADOW NODE ROLE
// =============================================================================

export enum CanonicalShadowNodeRole {
  IPHONE = 'iPhone',
  MAC = 'Mac'
}

// =============================================================================
// SHADOW MISMATCH CATEGORY
// =============================================================================

export enum CanonicalShadowMismatchCategory {
  LEGACY_RECORDING_MISSING_IN_CANONICAL = 'legacyRecordingMissingInCanonical',
  CANONICAL_OBJECT_MISSING_IN_LEGACY = 'canonicalObjectMissingInLegacy',
  STUDY_ITEM_ONLY_WITHOUT_RECEIVE_RECORD = 'studyItemOnlyWithoutReceiveRecord',
  RECEIVE_RECORD_ONLY_WITHOUT_STUDY_ITEM = 'receiveRecordOnlyWithoutStudyItem',
  LEGACY_METADATA_HASH_MISMATCH_BUT_CANONICAL_HASH_MATCH = 'legacyMetadataHashMismatchButCanonicalHashMatch',
  CANONICAL_METADATA_HASH_MISMATCH = 'canonicalMetadataHashMismatch',
  CANONICAL_METADATA_HASH_CONVERGED = 'canonicalMetadataHashConverged',
  CANONICAL_AUDIO_SAME_HASH_SAME_SIZE = 'canonicalAudioSameHashSameSize',
  CANONICAL_AUDIO_MISSING = 'canonicalAudioMissing',
  CANONICAL_AUDIO_UNKNOWN = 'canonicalAudioUnknown',
  CANONICAL_AUDIO_CONFLICT = 'canonicalAudioConflict',
  PEER_UNKNOWN = 'peerUnknown',
  LEGACY_WOULD_UPLOAD_METADATA_BUT_CANONICAL_NO_OP = 'legacyWouldUploadMetadataButCanonicalNoOp',
  CANONICAL_PLAN_USED = 'canonicalPlanUsed',
  CANONICAL_PLAN_FALLBACK = 'canonicalPlanFallback',
  CANONICAL_GENERATED_ARTIFACT_PEER_SAME_NO_OP = 'canonicalGeneratedArtifactPeerSameNoOp',
  CANONICAL_GENERATED_ARTIFACT_PEER_UNKNOWN_DEFERRED = 'canonicalGeneratedArtifactPeerUnknownDeferred',
  CANONICAL_GENERATED_ARTIFACT_CONFLICT = 'canonicalGeneratedArtifactConflict'
}

// =============================================================================
// SHADOW LEGACY OBJECT FACT
// =============================================================================

export class CanonicalShadowLegacyObjectFact {
  objectID: string
  legacyMetadataHashPrefix: string | null
  audioHashPrefix: string | null
  audioByteSize: number | null
  audioAvailability: string
  hasRecordingMetadata: boolean
  hasReceiveRecord: boolean
  hasStudyItem: boolean

  constructor(
    objectID: string,
    legacyMetadataHash: string | null = null,
    audioHash: string | null = null,
    audioByteSize: number | null = null,
    audioAvailability: string = 'unknown',
    hasRecordingMetadata: boolean = false,
    hasReceiveRecord: boolean = false,
    hasStudyItem: boolean = false
  ) {
    this.objectID = objectID.trim()
    this.legacyMetadataHashPrefix = CanonicalShadowLegacyObjectFact.hashPrefix(legacyMetadataHash)
    this.audioHashPrefix = CanonicalShadowLegacyObjectFact.hashPrefix(audioHash)
    this.audioByteSize = audioByteSize
    this.audioAvailability = (nilIfEmpty(audioAvailability.trim()) ?? 'unknown')
    this.hasRecordingMetadata = hasRecordingMetadata
    this.hasReceiveRecord = hasReceiveRecord
    this.hasStudyItem = hasStudyItem
  }

  static merged(facts: CanonicalShadowLegacyObjectFact[]): CanonicalShadowLegacyObjectFact[] {
    const grouped = new Map<string, CanonicalShadowLegacyObjectFact[]>()
    for (const f of facts) {
      const arr = grouped.get(f.objectID) ?? []
      arr.push(f)
      grouped.set(f.objectID, arr)
    }
    return Array.from(grouped.entries()).map(([objectID, values]) => {
      return new CanonicalShadowLegacyObjectFact(
        objectID,
        values.map(v => v.legacyMetadataHashPrefix).find(x => x !== null) ?? null,
        values.map(v => v.audioHashPrefix).find(x => x !== null) ?? null,
        values.map(v => v.audioByteSize).find(x => x !== null) ?? null,
        values.find(v => v.audioAvailability !== 'unknown')?.audioAvailability ?? 'unknown',
        values.some(v => v.hasRecordingMetadata),
        values.some(v => v.hasReceiveRecord),
        values.some(v => v.hasStudyItem)
      )
    }).sort((a, b) => a.objectID.localeCompare(b.objectID))
  }

  private static hashPrefix(value: string | null): string | null {
    if (!value) {
      return null
    }
    const normalized = value.trim().toLowerCase()
    if (normalized.length === 0) {
      return null
    }
    return normalized.substring(0, 12)
  }
}

// =============================================================================
// SHADOW LEGACY SNAPSHOT
// =============================================================================

export class CanonicalShadowLegacySnapshot {
  recordingCount: number
  studyItemCount: number
  artifactCount: number
  objects: CanonicalShadowLegacyObjectFact[]

  constructor(
    recordingCount: number,
    studyItemCount: number,
    artifactCount: number,
    objects: CanonicalShadowLegacyObjectFact[]
  ) {
    this.recordingCount = recordingCount
    this.studyItemCount = studyItemCount
    this.artifactCount = artifactCount
    this.objects = CanonicalShadowLegacyObjectFact.merged(objects)
  }
}

// =============================================================================
// SHADOW MISMATCH
// =============================================================================

export class CanonicalShadowMismatch {
  category: CanonicalShadowMismatchCategory
  objectID: string | null
  artifactID: string | null
  detail: string | null
  localHashPrefix: string | null
  peerHashPrefix: string | null
  localByteSize: number | null
  peerByteSize: number | null

  constructor(
    category: CanonicalShadowMismatchCategory,
    objectID: string | null = null,
    artifactID: string | null = null,
    detail: string | null = null,
    localHashPrefix: string | null = null,
    peerHashPrefix: string | null = null,
    localByteSize: number | null = null,
    peerByteSize: number | null = null
  ) {
    this.category = category
    this.objectID = objectID
    this.artifactID = artifactID
    this.detail = detail
    this.localHashPrefix = localHashPrefix
    this.peerHashPrefix = peerHashPrefix
    this.localByteSize = localByteSize
    this.peerByteSize = peerByteSize
  }

  get id(): string {
    return [this.category, this.objectID ?? '', this.artifactID ?? '', this.detail ?? ''].join('|')
  }
}

// =============================================================================
// SHADOW SUMMARIES
// =============================================================================

export class CanonicalShadowArtifactSummary {
  artifactID: string
  objectID: string
  kind: string
  availability: string
  hashPrefix: string | null
  hasHash: boolean
  hasByteSize: boolean
  byteSize: number | null
  logicalName: string | null

  constructor(
    artifactID: string,
    objectID: string,
    kind: string,
    availability: string,
    hashPrefix: string | null,
    hasHash: boolean,
    hasByteSize: boolean,
    byteSize: number | null,
    logicalName: string | null
  ) {
    this.artifactID = artifactID
    this.objectID = objectID
    this.kind = kind
    this.availability = availability
    this.hashPrefix = hashPrefix
    this.hasHash = hasHash
    this.hasByteSize = hasByteSize
    this.byteSize = byteSize
    this.logicalName = logicalName
  }
}

export class CanonicalShadowObjectSummary {
  objectID: string
  canonicalMetadataHashPrefix: string | null
  legacyMetadataHashPrefix: string | null
  createdAt: Date | null
  modifiedAt: Date | null
  audioAvailability: string
  audioHashPrefix: string | null
  audioHashPresent: boolean
  audioByteSizePresent: boolean
  audioByteSize: number | null
  hasRecordingMetadata: boolean
  hasReceiveRecord: boolean
  hasStudyItem: boolean

  constructor(
    objectID: string,
    canonicalMetadataHashPrefix: string | null = null,
    legacyMetadataHashPrefix: string | null = null,
    createdAt: Date | null = null,
    modifiedAt: Date | null = null,
    audioAvailability: string = 'unknown',
    audioHashPrefix: string | null = null,
    audioHashPresent: boolean = false,
    audioByteSizePresent: boolean = false,
    audioByteSize: number | null = null,
    hasRecordingMetadata: boolean = false,
    hasReceiveRecord: boolean = false,
    hasStudyItem: boolean = false
  ) {
    this.objectID = objectID
    this.canonicalMetadataHashPrefix = canonicalMetadataHashPrefix
    this.legacyMetadataHashPrefix = legacyMetadataHashPrefix
    this.createdAt = createdAt
    this.modifiedAt = modifiedAt
    this.audioAvailability = audioAvailability
    this.audioHashPrefix = audioHashPrefix
    this.audioHashPresent = audioHashPresent
    this.audioByteSizePresent = audioByteSizePresent
    this.audioByteSize = audioByteSize
    this.hasRecordingMetadata = hasRecordingMetadata
    this.hasReceiveRecord = hasReceiveRecord
    this.hasStudyItem = hasStudyItem
  }

  get id(): string {
    return this.objectID
  }
}

// =============================================================================
// SHADOW LEGACY COMPARISON
// =============================================================================

export class CanonicalShadowLegacyComparison {
  legacyRecordingCount: number
  legacyStudyItemCount: number
  legacyArtifactCount: number
  canonicalObjectCount: number
  canonicalArtifactCount: number
  metadataHashConvergedObjectIDs: string[]
  objectSummaries: CanonicalShadowObjectSummary[]
  artifactSummaries: CanonicalShadowArtifactSummary[]
  mismatches: CanonicalShadowMismatch[]

  constructor(
    legacyRecordingCount: number,
    legacyStudyItemCount: number,
    legacyArtifactCount: number,
    canonicalObjectCount: number,
    canonicalArtifactCount: number,
    metadataHashConvergedObjectIDs: string[],
    objectSummaries: CanonicalShadowObjectSummary[],
    artifactSummaries: CanonicalShadowArtifactSummary[],
    mismatches: CanonicalShadowMismatch[]
  ) {
    this.legacyRecordingCount = legacyRecordingCount
    this.legacyStudyItemCount = legacyStudyItemCount
    this.legacyArtifactCount = legacyArtifactCount
    this.canonicalObjectCount = canonicalObjectCount
    this.canonicalArtifactCount = canonicalArtifactCount
    this.metadataHashConvergedObjectIDs = metadataHashConvergedObjectIDs
    this.objectSummaries = objectSummaries
    this.artifactSummaries = artifactSummaries
    this.mismatches = mismatches
  }
}

// =============================================================================
// SHADOW REPORT
// =============================================================================

export class CanonicalShadowReport {
  static readonly CURRENT_SCHEMA_VERSION = 1

  schemaVersion: number
  runID: string
  syncRunID: string | null
  trigger: string | null
  nodeID: string
  nodeRole: CanonicalShadowNodeRole
  generatedAt: Date
  durationMs: number
  manifestHashPrefix: string | null
  comparison: CanonicalShadowLegacyComparison

  constructor(
    schemaVersion: number,
    runID: string,
    syncRunID: string | null,
    trigger: string | null,
    nodeID: string,
    nodeRole: CanonicalShadowNodeRole,
    generatedAt: Date,
    durationMs: number,
    manifestHashPrefix: string | null,
    comparison: CanonicalShadowLegacyComparison
  ) {
    this.schemaVersion = schemaVersion
    this.runID = runID
    this.syncRunID = syncRunID
    this.trigger = trigger
    this.nodeID = nodeID
    this.nodeRole = nodeRole
    this.generatedAt = generatedAt
    this.durationMs = durationMs
    this.manifestHashPrefix = manifestHashPrefix
    this.comparison = comparison
  }
}

// =============================================================================
// SHADOW REPORT BUILDER
// =============================================================================

export class CanonicalShadowReportBuilder {
  build(
    runID: string | null = null,
    syncRunID: string | null = null,
    trigger: string | null = null,
    nodeID: string,
    nodeRole: CanonicalShadowNodeRole,
    generatedAt: Date = new Date(),
    durationMs: number,
    manifest: CanonicalManifest,
    legacy: CanonicalShadowLegacySnapshot,
    peerManifest: CanonicalManifest | null = null,
    peerLegacy: CanonicalShadowLegacySnapshot | null = null
  ): CanonicalShadowReport {
    const localObjectsByID = new Map(manifest.objects.map(o => [o.objectID, o] as [string, CanonicalRecordingObject]))
    const peerObjectsByID = new Map((peerManifest?.objects ?? []).map(o => [o.objectID, o] as [string, CanonicalRecordingObject]))
    const legacyByID = new Map(legacy.objects.map(o => [o.objectID, o] as [string, CanonicalShadowLegacyObjectFact]))
    const peerLegacyByID = new Map((peerLegacy?.objects ?? []).map(o => [o.objectID, o] as [string, CanonicalShadowLegacyObjectFact]))

    const allIDs = new Set([
      ...localObjectsByID.keys(), ...legacyByID.keys(),
      ...peerObjectsByID.keys(), ...peerLegacyByID.keys()
    ])
    const objectIDs = Array.from(allIDs).sort()

    const mismatches: CanonicalShadowMismatch[] = []
    const convergedObjectIDs: string[] = []

    const objectSummaries = objectIDs.map(objectID => {
      const object = localObjectsByID.get(objectID)
      const legacyFact = legacyByID.get(objectID)
      this.appendStructuralMismatches(objectID, object, legacyFact, mismatches)
      this.appendMetadataMismatches(objectID, object, peerObjectsByID.get(objectID),
        legacyFact, peerLegacyByID.get(objectID), nodeRole, convergedObjectIDs, mismatches)
      this.appendAudioMismatches(objectID, object, peerLegacyByID.get(objectID), peerLegacy !== null, mismatches)
      return this.makeObjectSummary(objectID, object, legacyFact)
    })

    const artifactSummaries = manifest.objects
      .flatMap(o => o.artifacts)
      .sort((a, b) => a.artifactID.localeCompare(b.artifactID))
      .map(a => this.makeArtifactSummary(a))

    const comparison = new CanonicalShadowLegacyComparison(
      legacy.recordingCount, legacy.studyItemCount, legacy.artifactCount,
      manifest.objects.length,
      manifest.objects.reduce((sum, o) => sum + o.artifacts.length, 0),
      convergedObjectIDs.sort(), objectSummaries, artifactSummaries,
      CanonicalShadowReportBuilder.uniqueMismatches(mismatches)
    )

    return new CanonicalShadowReport(
      1, runID ?? syncRunID ?? CanonicalShadowReportBuilder.generateUUID(),
      syncRunID, trigger ? nilIfEmpty(trigger.trim()) : null, nodeID, nodeRole,
      generatedAt, Math.max(0, durationMs),
      CanonicalShadowReportBuilder.hashPrefixValue(manifest.manifestHash.value),
      comparison
    )
  }

  private appendStructuralMismatches(
    objectID: string,
    object: CanonicalRecordingObject | undefined,
    legacyFact: CanonicalShadowLegacyObjectFact | undefined,
    mismatches: CanonicalShadowMismatch[]
  ) {
    if (!object && (legacyFact?.hasRecordingMetadata === true || legacyFact?.hasReceiveRecord === true)) {
      mismatches.push(new CanonicalShadowMismatch(
        CanonicalShadowMismatchCategory.LEGACY_RECORDING_MISSING_IN_CANONICAL, objectID
      ))
    }
    if (object && !legacyFact) {
      mismatches.push(new CanonicalShadowMismatch(
        CanonicalShadowMismatchCategory.CANONICAL_OBJECT_MISSING_IN_LEGACY, objectID
      ))
    }
  }

  private appendMetadataMismatches(
    objectID: string,
    localObject: CanonicalRecordingObject | undefined,
    peerObject: CanonicalRecordingObject | undefined,
    localLegacy: CanonicalShadowLegacyObjectFact | undefined,
    peerLegacy: CanonicalShadowLegacyObjectFact | undefined,
    nodeRole: CanonicalShadowNodeRole,
    convergedObjectIDs: string[],
    mismatches: CanonicalShadowMismatch[]
  ) {
    if (!localObject || !peerObject) {
      return
    }
    const localHash = localObject.metadataHash.value
    const peerHash = peerObject.metadataHash.value
    if (localHash === peerHash) {
      convergedObjectIDs.push(objectID)
      mismatches.push(new CanonicalShadowMismatch(
        CanonicalShadowMismatchCategory.CANONICAL_METADATA_HASH_CONVERGED, objectID, null,
        'metadataHashEqual',
        CanonicalShadowReportBuilder.hashPrefixValue(localHash),
        CanonicalShadowReportBuilder.hashPrefixValue(peerHash)
      ))
    } else {
      mismatches.push(new CanonicalShadowMismatch(
        CanonicalShadowMismatchCategory.CANONICAL_METADATA_HASH_MISMATCH, objectID, null,
        null,
        CanonicalShadowReportBuilder.hashPrefixValue(localHash),
        CanonicalShadowReportBuilder.hashPrefixValue(peerHash)
      ))
    }
  }

  private appendAudioMismatches(
    objectID: string,
    localObject: CanonicalRecordingObject | undefined,
    peerLegacy: CanonicalShadowLegacyObjectFact | undefined,
    peerLegacyWasProvided: boolean,
    mismatches: CanonicalShadowMismatch[]
  ) {
    if (!localObject) {
      return
    }
    const localAudio = localObject.audioArtifact
    if (!localAudio) {
      mismatches.push(new CanonicalShadowMismatch(
        CanonicalShadowMismatchCategory.CANONICAL_AUDIO_MISSING, objectID
      ))
      return
    }
    if (!localAudio.provesCanonicalAudioAvailability || !localAudio.contentHash || !localAudio.byteSize) {
      mismatches.push(new CanonicalShadowMismatch(
        CanonicalShadowMismatchCategory.CANONICAL_AUDIO_UNKNOWN, objectID,
        localAudio.artifactID, null,
        CanonicalShadowReportBuilder.hashPrefixValue(localAudio.contentHash?.value ?? null),
        null, localAudio.byteSize, null
      ))
      return
    }
    if (!peerLegacyWasProvided) {
      return
    }
    if (!peerLegacy) {
      mismatches.push(new CanonicalShadowMismatch(
        CanonicalShadowMismatchCategory.PEER_UNKNOWN, objectID,
        localAudio.artifactID, null,
        CanonicalShadowReportBuilder.hashPrefixValue(localAudio.contentHash.value),
        null, localAudio.byteSize, null
      ))
      return
    }
    const localHash = CanonicalShadowReportBuilder.hashPrefixValue(localAudio.contentHash.value)
    if (localHash === peerLegacy.audioHashPrefix && localAudio.byteSize === peerLegacy.audioByteSize) {
      mismatches.push(new CanonicalShadowMismatch(
        CanonicalShadowMismatchCategory.CANONICAL_AUDIO_SAME_HASH_SAME_SIZE, objectID,
        localAudio.artifactID, null, localHash, peerLegacy.audioHashPrefix,
        localAudio.byteSize, peerLegacy.audioByteSize
      ))
    } else {
      mismatches.push(new CanonicalShadowMismatch(
        CanonicalShadowMismatchCategory.CANONICAL_AUDIO_CONFLICT, objectID,
        localAudio.artifactID, null, localHash, peerLegacy.audioHashPrefix,
        localAudio.byteSize, peerLegacy.audioByteSize
      ))
    }
  }

  private makeObjectSummary(
    objectID: string,
    object: CanonicalRecordingObject | undefined,
    legacyFact: CanonicalShadowLegacyObjectFact | undefined
  ): CanonicalShadowObjectSummary {
    const audio = object?.audioArtifact
    return new CanonicalShadowObjectSummary(
      objectID,
      object ? CanonicalShadowReportBuilder.hashPrefixValue(object.metadataHash.value) : null,
      legacyFact?.legacyMetadataHashPrefix ?? null,
      object?.metadata.createdAt.date ?? null,
      object?.metadata.modifiedAt.date ?? null,
      audio?.availability ?? legacyFact?.audioAvailability ?? 'unknown',
      audio ? CanonicalShadowReportBuilder.hashPrefixValue(audio.contentHash?.value ?? null) : legacyFact?.audioHashPrefix ?? null,
      audio?.contentHash !== null || legacyFact?.audioHashPrefix !== null,
      audio?.byteSize !== null || legacyFact?.audioByteSize !== null,
      audio?.byteSize ?? legacyFact?.audioByteSize ?? null,
      legacyFact?.hasRecordingMetadata ?? false,
      legacyFact?.hasReceiveRecord ?? false,
      legacyFact?.hasStudyItem ?? false
    )
  }

  private makeArtifactSummary(artifact: CanonicalArtifact): CanonicalShadowArtifactSummary {
    return new CanonicalShadowArtifactSummary(
      artifact.artifactID, artifact.objectID, artifact.kind,
      artifact.availability,
      CanonicalShadowReportBuilder.hashPrefixValue(artifact.contentHash?.value ?? null),
      artifact.contentHash !== null, artifact.byteSize !== null,
      artifact.byteSize,
      CanonicalShadowReportBuilder.logicalName(artifact.logicalName)
    )
  }

  private static uniqueMismatches(mismatches: CanonicalShadowMismatch[]): CanonicalShadowMismatch[] {
    const seen = new Set<string>()
    return mismatches.filter(m => {
      const key = [m.category, m.objectID ?? '', m.artifactID ?? '', m.detail ?? ''].join('|')
      if (seen.has(key)) {
        return false
      }
      seen.add(key)
      return true
    }).sort((a, b) => {
      if (a.category !== b.category) {
        return a.category.localeCompare(b.category)
      }
      return (a.objectID ?? '').localeCompare(b.objectID ?? '')
    })
  }

  static hashPrefixValue(value: string | null): string | null {
    if (!value) {
      return null
    }
    const normalized = value.trim().toLowerCase()
    if (normalized.length === 0) {
      return null
    }
    return normalized.substring(0, 12)
  }

  static logicalName(value: string | null): string | null {
    if (!value) {
      return null
    }
    const trimmed = nilIfEmpty(value.trim())
    if (!trimmed) {
      return null
    }
    const parts = trimmed.split('/')
    return nilIfEmpty(parts[parts.length - 1]) ?? trimmed
  }

  static generateUUID(): string {
    return 'shadow-' + Math.random().toString(36).substring(2, 10) + Date.now().toString(36)
  }
}
