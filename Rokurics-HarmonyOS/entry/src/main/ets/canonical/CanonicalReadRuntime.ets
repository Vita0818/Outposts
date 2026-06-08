/**
 * CanonicalReadRuntime — canonical read runtime with guarded projection and comparison.
 * Ported from Rokurics-Apple (CanonicalReadRuntime.swift).
 */

import {
  CanonicalTimestamp, CanonicalRecordingObject, CanonicalRecordingMetadata,
  CanonicalLibraryObject, CanonicalManifest, nilIfEmpty, hashPrefix
} from './CanonicalCore'
import {
  CanonicalProductionRedaction, CanonicalProductionDomain
} from './CanonicalProductionPorts'

// =============================================================================
// READ PROJECTION SOURCE
// =============================================================================

export enum CanonicalReadProjectionSource {
  LEGACY = 'legacy',
  CANONICAL = 'canonical'
}

export enum CanonicalReadDomain {
  RECORDING_METADATA = 'recordingMetadata',
  LIBRARY_METADATA = 'libraryMetadata',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  AUDIO_UPLOAD_STATUS = 'audioUploadStatus',
  SYNC_ENGINE_STATUS = 'syncEngineStatus'
}

export enum CanonicalReadRuntimeMode {
  DISABLED = 'disabled',
  PARALLEL_COMPARE = 'parallelCompare',
  CANONICAL_READ_CANDIDATE = 'canonicalReadCandidate',
  GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK = 'guardedCanonicalReadWithLegacyFallback',
  BLOCKED = 'blocked'

  get buildsCanonicalCandidate(): boolean {
    switch (this) {
      case CanonicalReadRuntimeMode.DISABLED:
      case CanonicalReadRuntimeMode.BLOCKED:
        return false
      case CanonicalReadRuntimeMode.PARALLEL_COMPARE:
      case CanonicalReadRuntimeMode.CANONICAL_READ_CANDIDATE:
      case CanonicalReadRuntimeMode.GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK:
        return true
    }
  }
}

// =============================================================================
// READ RUNTIME POLICY
// =============================================================================

export class CanonicalReadRuntimePolicy {
  debugInternalBuild: boolean
  ownerApproved: boolean
  manualOwnerApproval: boolean
  releaseDefaultBuild: boolean
  legacyFallbackAvailable: boolean
  diagnosticsRedacted: boolean
  applyRuntimeEvidenceValidForNonAudio: boolean
  uploadRuntimeEvidenceValidForAudioStatus: boolean
  inventorySnapshotAvailable: boolean
  planAuthorityEvidenceValid: boolean
  existenceTruthEvidenceValid: boolean
  otherDomainsNotConflicting: boolean
  allowDivergentGuardedReadForTests: boolean
  readMustNotTriggerSyncUpload: boolean
  readMustNotMutateStore: boolean
  maxDiagnosticsEvents: number

  constructor(
    debugInternalBuild: boolean = false,
    ownerApproved: boolean = false,
    manualOwnerApproval: boolean = false,
    releaseDefaultBuild: boolean = true,
    legacyFallbackAvailable: boolean = true,
    diagnosticsRedacted: boolean = true,
    applyRuntimeEvidenceValidForNonAudio: boolean = false,
    uploadRuntimeEvidenceValidForAudioStatus: boolean = false,
    inventorySnapshotAvailable: boolean = false,
    planAuthorityEvidenceValid: boolean = false,
    existenceTruthEvidenceValid: boolean = false,
    otherDomainsNotConflicting: boolean = true,
    allowDivergentGuardedReadForTests: boolean = false,
    readMustNotTriggerSyncUpload: boolean = true,
    readMustNotMutateStore: boolean = true,
    maxDiagnosticsEvents: number = 64
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApproved = ownerApproved
    this.manualOwnerApproval = manualOwnerApproval
    this.releaseDefaultBuild = releaseDefaultBuild
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.diagnosticsRedacted = diagnosticsRedacted
    this.applyRuntimeEvidenceValidForNonAudio = applyRuntimeEvidenceValidForNonAudio
    this.uploadRuntimeEvidenceValidForAudioStatus = uploadRuntimeEvidenceValidForAudioStatus
    this.inventorySnapshotAvailable = inventorySnapshotAvailable
    this.planAuthorityEvidenceValid = planAuthorityEvidenceValid
    this.existenceTruthEvidenceValid = existenceTruthEvidenceValid
    this.otherDomainsNotConflicting = otherDomainsNotConflicting
    this.allowDivergentGuardedReadForTests = allowDivergentGuardedReadForTests
    this.readMustNotTriggerSyncUpload = readMustNotTriggerSyncUpload
    this.readMustNotMutateStore = readMustNotMutateStore
    this.maxDiagnosticsEvents = Math.max(0, maxDiagnosticsEvents)
  }

  static explicitGuardedDebugInternal(
    allowDivergentGuardedReadForTests: boolean = false
  ): CanonicalReadRuntimePolicy {
    return new CanonicalReadRuntimePolicy(
      true, true, true, false, true, true, true, true, true, true, true, true,
      allowDivergentGuardedReadForTests
    )
  }
}

// =============================================================================
// READ RUNTIME CONFIGURATION
// =============================================================================

export class CanonicalReadRuntimeConfiguration {
  mode: CanonicalReadRuntimeMode
  policy: CanonicalReadRuntimePolicy

  constructor(
    mode: CanonicalReadRuntimeMode = CanonicalReadRuntimeMode.DISABLED,
    policy: CanonicalReadRuntimePolicy = new CanonicalReadRuntimePolicy()
  ) {
    this.mode = mode
    this.policy = policy
  }

  static readonly DISABLED = new CanonicalReadRuntimeConfiguration()

  static explicitGuardedCanonicalRead(
    allowDivergentGuardedReadForTests: boolean = false
  ): CanonicalReadRuntimeConfiguration {
    return new CanonicalReadRuntimeConfiguration(
      CanonicalReadRuntimeMode.GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK,
      CanonicalReadRuntimePolicy.explicitGuardedDebugInternal(allowDivergentGuardedReadForTests)
    )
  }
}

// =============================================================================
// PROJECTION FAILURE
// =============================================================================

export enum CanonicalReadProjectionFailureKind {
  SNAPSHOT_MISSING = 'snapshotMissing',
  UNSUPPORTED_OBJECT = 'unsupportedObject',
  PATH_CONTENT_LEAK_RISK = 'pathContentLeakRisk'
}

export class CanonicalReadProjectionFailure {
  kind: CanonicalReadProjectionFailureKind
  domain: CanonicalReadDomain
  objectID: string | null
  reason: string

  constructor(
    kind: CanonicalReadProjectionFailureKind,
    domain: CanonicalReadDomain,
    objectID: string | null = null,
    reason: string
  ) {
    this.kind = kind
    this.domain = domain
    this.objectID = objectID ? CanonicalReadRuntimeRedaction.safeIdentifier(objectID, 'object') : null
    this.reason = CanonicalReadRuntimeRedaction.safeText(reason) ?? kind
  }

  get id(): string {
    return [this.kind, this.domain, this.objectID ?? 'run', this.reason].join('|')
  }
}

// =============================================================================
// RECORDING READ PROJECTION RECORD
// =============================================================================

export class CanonicalRecordingReadProjectionRecord {
  objectID: string
  title: string
  tags: string[]
  filingSummary: string
  createdAtSummary: string
  modifiedAtSummary: string
  durationSeconds: number | null
  metadataHashPrefix: string | null
  isDeleted: boolean
  syncState: string
  processingSummary: string

  constructor(
    objectID: string,
    title: string,
    tags: string[] = [],
    filingSummary: string = 'none',
    createdAt: CanonicalTimestamp,
    modifiedAt: CanonicalTimestamp,
    duration: number | null = null,
    metadataHashPrefix: string | null = null,
    isDeleted: boolean = false,
    syncState: string = 'unknown',
    processingSummary: string = 'unknown'
  ) {
    this.objectID = CanonicalReadRuntimeRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.title = CanonicalReadRuntimeRedaction.safeDisplayText(title, 'Untitled')
    this.tags = tags
      .map(t => CanonicalReadRuntimeRedaction.safeDisplayText(t, ''))
      .filter(t => t.length > 0)
      .sort()
    this.filingSummary = CanonicalReadRuntimeRedaction.safeText(filingSummary) ?? 'none'
    this.createdAtSummary = CanonicalRecordingReadProjectionRecord.timestampSummary(createdAt)
    this.modifiedAtSummary = CanonicalRecordingReadProjectionRecord.timestampSummary(modifiedAt)
    this.durationSeconds = duration ? Math.max(0, Math.round(duration)) : null
    this.metadataHashPrefix = CanonicalReadRuntimeRedaction.hashPrefix(metadataHashPrefix)
    this.isDeleted = isDeleted
    this.syncState = syncState
    this.processingSummary = CanonicalReadRuntimeRedaction.safeText(processingSummary) ?? 'unknown'
  }

  get id(): string {
    return this.objectID
  }

  get tagsKey(): string {
    return this.tags.join('|')
  }

  private static timestampSummary(timestamp: CanonicalTimestamp): string {
    return 'unixSeconds=' + Math.floor(timestamp.date.getTime() / 1000)
  }

  private static filingSummary(filing: CanonicalRecordingMetadata.Filing | null): string {
    if (!filing) {
      return 'none'
    }
    const parts: string[] = []
    if (filing.type) parts.push('type=' + filing.type)
    if (filing.subject) parts.push('subject=' + filing.subject)
    if (filing.chapter) parts.push('chapter=' + filing.chapter)
    if (filing.topic) parts.push('topic=' + filing.topic)
    return parts.length > 0 ? parts.join(',') : 'none'
  }

  static fromObject(object: CanonicalRecordingObject): CanonicalRecordingReadProjectionRecord {
    return new CanonicalRecordingReadProjectionRecord(
      object.objectID, object.metadata.title, object.metadata.tags,
      CanonicalRecordingReadProjectionRecord.filingSummary(object.metadata.filing),
      object.metadata.createdAt, object.metadata.modifiedAt,
      object.metadata.duration,
      CanonicalReadRuntimeRedaction.hashPrefix(object.metadataHash.value),
      object.metadata.isDeleted, object.syncState,
      'transcription=' + object.processingState.transcription + ',note=' + object.processingState.note
    )
  }
}

// =============================================================================
// RECORDING READ PROJECTION
// =============================================================================

export class CanonicalRecordingReadProjection {
  source: CanonicalReadProjectionSource
  records: CanonicalRecordingReadProjectionRecord[]
  failures: CanonicalReadProjectionFailure[]

  constructor(
    source: CanonicalReadProjectionSource,
    records: CanonicalRecordingReadProjectionRecord[] = [],
    failures: CanonicalReadProjectionFailure[] = []
  ) {
    this.source = source
    this.records = records.sort((a, b) => a.objectID.localeCompare(b.objectID))
    this.failures = failures.sort((a, b) => a.id.localeCompare(b.id))
  }

  static build(
    source: CanonicalReadProjectionSource,
    manifest: CanonicalManifest | null
  ): CanonicalRecordingReadProjection {
    if (!manifest) {
      return new CanonicalRecordingReadProjection(source, [], [
        new CanonicalReadProjectionFailure(
          CanonicalReadProjectionFailureKind.SNAPSHOT_MISSING,
          CanonicalReadDomain.RECORDING_METADATA, null, 'recordingManifestMissing'
        )
      ])
    }
    return new CanonicalRecordingReadProjection(
      source,
      manifest.objects.map(o => CanonicalRecordingReadProjectionRecord.fromObject(o))
    )
  }

  get diagnosticsSummary(): string {
    return 'source=' + this.source + ',records=' + this.records.length + ',failures=' + this.failures.length
  }
}

// =============================================================================
// READ SNAPSHOT
// =============================================================================

export class CanonicalReadSnapshotRedaction {
  excludesAbsolutePaths: boolean
  excludesFullHashes: boolean
  excludesSecrets: boolean
  excludesFullGeneratedContent: boolean
  excludesRequestResponseBodies: boolean

  constructor(
    excludesAbsolutePaths: boolean,
    excludesFullHashes: boolean,
    excludesSecrets: boolean,
    excludesFullGeneratedContent: boolean,
    excludesRequestResponseBodies: boolean
  ) {
    this.excludesAbsolutePaths = excludesAbsolutePaths
    this.excludesFullHashes = excludesFullHashes
    this.excludesSecrets = excludesSecrets
    this.excludesFullGeneratedContent = excludesFullGeneratedContent
    this.excludesRequestResponseBodies = excludesRequestResponseBodies
  }

  static readonly REDACTED = new CanonicalReadSnapshotRedaction(true, true, true, true, true)

  get isRedacted(): boolean {
    return this.excludesAbsolutePaths && this.excludesFullHashes &&
      this.excludesSecrets && this.excludesFullGeneratedContent &&
      this.excludesRequestResponseBodies
  }
}

export class CanonicalReadSnapshot {
  source: CanonicalReadProjectionSource
  generatedAt: CanonicalTimestamp
  recordingMetadata: CanonicalRecordingReadProjection
  redaction: CanonicalReadSnapshotRedaction

  constructor(
    source: CanonicalReadProjectionSource,
    generatedAt: Date = new Date(),
    recordingMetadata: CanonicalRecordingReadProjection,
    redaction: CanonicalReadSnapshotRedaction = CanonicalReadSnapshotRedaction.REDACTED
  ) {
    this.source = source
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.recordingMetadata = recordingMetadata
    this.redaction = redaction
  }

  get diagnosticsSummary(): string {
    return 'source=' + this.source + ',recordings=' + this.recordingMetadata.records.length +
      ',redacted=' + this.redaction.isRedacted
  }
}

// =============================================================================
// READ RUNTIME DIVERGENCE
// =============================================================================

export enum CanonicalReadRuntimeDivergenceKind {
  MISSING_OBJECT = 'missingObject',
  METADATA_MISMATCH = 'metadataMismatch',
  TITLE_TAGS_FOLDER_MISMATCH = 'titleTagsFolderMismatch',
  ARTIFACT_AVAILABILITY_MISMATCH = 'artifactAvailabilityMismatch',
  TOMBSTONE_CONFLICT_MISMATCH = 'tombstoneConflictMismatch',
  AUDIO_AVAILABILITY_MISMATCH = 'audioAvailabilityMismatch',
  UPLOAD_STATUS_MISMATCH = 'uploadStatusMismatch',
  UNSUPPORTED_OBJECT = 'unsupportedObject',
  PATH_CONTENT_LEAK_RISK = 'pathContentLeakRisk'
}

export class CanonicalReadRuntimeDivergence {
  kind: CanonicalReadRuntimeDivergenceKind
  domain: CanonicalReadDomain
  objectID: string | null
  field: string | null
  legacyValue: string | null
  canonicalValue: string | null
  fatal: boolean

  constructor(
    kind: CanonicalReadRuntimeDivergenceKind,
    domain: CanonicalReadDomain,
    objectID: string | null = null,
    field: string | null = null,
    legacyValue: string | null = null,
    canonicalValue: string | null = null,
    fatal: boolean = false
  ) {
    this.kind = kind
    this.domain = domain
    this.objectID = objectID ? CanonicalReadRuntimeRedaction.safeIdentifier(objectID, 'object') : null
    this.field = CanonicalReadRuntimeRedaction.safeText(field)
    this.legacyValue = CanonicalReadRuntimeRedaction.safeText(legacyValue)
    this.canonicalValue = CanonicalReadRuntimeRedaction.safeText(canonicalValue)
    this.fatal = fatal || kind === CanonicalReadRuntimeDivergenceKind.PATH_CONTENT_LEAK_RISK
  }

  get id(): string {
    return [this.kind, this.domain, this.objectID ?? 'run', this.field ?? ''].join('|')
  }
}

// =============================================================================
// READ RUNTIME DIFF
// =============================================================================

export class CanonicalReadRuntimeEquivalenceReport {
  equivalent: boolean
  divergenceCount: number
  fatalDivergenceCount: number
  domainsCompared: CanonicalReadDomain[]
  diagnosticsSummary: string

  constructor(
    equivalent: boolean,
    divergenceCount: number,
    fatalDivergenceCount: number,
    domainsCompared: CanonicalReadDomain[],
    diagnosticsSummary: string
  ) {
    this.equivalent = equivalent
    this.divergenceCount = divergenceCount
    this.fatalDivergenceCount = fatalDivergenceCount
    this.domainsCompared = domainsCompared
    this.diagnosticsSummary = diagnosticsSummary
  }
}

export class CanonicalReadRuntimeDiff {
  divergences: CanonicalReadRuntimeDivergence[]
  equivalenceReport: CanonicalReadRuntimeEquivalenceReport
  legacySnapshotSummary: string
  canonicalSnapshotSummary: string
  diagnosticsSummary: string

  constructor(
    divergences: CanonicalReadRuntimeDivergence[],
    equivalenceReport: CanonicalReadRuntimeEquivalenceReport,
    legacySnapshotSummary: string,
    canonicalSnapshotSummary: string,
    diagnosticsSummary: string
  ) {
    this.divergences = divergences
    this.equivalenceReport = equivalenceReport
    this.legacySnapshotSummary = legacySnapshotSummary
    this.canonicalSnapshotSummary = canonicalSnapshotSummary
    this.diagnosticsSummary = diagnosticsSummary
  }

  get equivalent(): boolean {
    return this.equivalenceReport.equivalent
  }

  get divergenceCount(): number {
    return this.equivalenceReport.divergenceCount
  }

  static compare(legacy: CanonicalReadSnapshot, canonical: CanonicalReadSnapshot): CanonicalReadRuntimeDiff {
    const divergences: CanonicalReadRuntimeDivergence[] = []
    CanonicalReadRuntimeDiff.compareRecordingMetadata(legacy.recordingMetadata, canonical.recordingMetadata, divergences)

    const uniqueDivergences = Array.from(
      new Map(divergences.map(d => [d.id, d] as [string, CanonicalReadRuntimeDivergence])).values()
    ).sort((a, b) => a.id.localeCompare(b.id))

    const equivalent = uniqueDivergences.length === 0
    const domains = Object.values(CanonicalReadDomain)
    const kinds = Array.from(new Set(uniqueDivergences.map(d => d.kind))).sort().join('+')
    const report = new CanonicalReadRuntimeEquivalenceReport(
      equivalent, uniqueDivergences.length,
      uniqueDivergences.filter(d => d.fatal).length, domains,
      'equivalent=' + equivalent + ',divergences=' + uniqueDivergences.length +
      ',fatal=' + uniqueDivergences.filter(d => d.fatal).length + ',kinds=' + kinds
    )
    return new CanonicalReadRuntimeDiff(
      uniqueDivergences, report,
      legacy.diagnosticsSummary, canonical.diagnosticsSummary,
      'domains=' + domains.join('+') + ',' + report.diagnosticsSummary
    )
  }

  private static compareRecordingMetadata(
    legacy: CanonicalRecordingReadProjection,
    canonical: CanonicalRecordingReadProjection,
    divergences: CanonicalReadRuntimeDivergence[]
  ) {
    for (const failure of (legacy.failures.concat(canonical.failures))) {
      let kind: CanonicalReadRuntimeDivergenceKind
      switch (failure.kind) {
        case CanonicalReadProjectionFailureKind.SNAPSHOT_MISSING:
          kind = CanonicalReadRuntimeDivergenceKind.MISSING_OBJECT
          break
        case CanonicalReadProjectionFailureKind.UNSUPPORTED_OBJECT:
          kind = CanonicalReadRuntimeDivergenceKind.UNSUPPORTED_OBJECT
          break
        case CanonicalReadProjectionFailureKind.PATH_CONTENT_LEAK_RISK:
          kind = CanonicalReadRuntimeDivergenceKind.PATH_CONTENT_LEAK_RISK
          break
      }
      divergences.push(new CanonicalReadRuntimeDivergence(
        kind, failure.domain, failure.objectID, 'projection',
        null, failure.reason, kind === CanonicalReadRuntimeDivergenceKind.PATH_CONTENT_LEAK_RISK
      ))
    }
    const legacyByID = new Map(legacy.records.map(r => [r.objectID, r] as [string, CanonicalRecordingReadProjectionRecord]))
    const canonicalByID = new Map(canonical.records.map(r => [r.objectID, r] as [string, CanonicalRecordingReadProjectionRecord]))
    const allIDs = new Set([...legacyByID.keys(), ...canonicalByID.keys()])
    for (const objectID of Array.from(allIDs).sort()) {
      const legacyRecord = legacyByID.get(objectID)
      const canonicalRecord = canonicalByID.get(objectID)
      if (!legacyRecord) {
        divergences.push(new CanonicalReadRuntimeDivergence(
          CanonicalReadRuntimeDivergenceKind.MISSING_OBJECT,
          CanonicalReadDomain.RECORDING_METADATA, objectID, null, null, 'present'
        ))
        continue
      }
      if (!canonicalRecord) {
        divergences.push(new CanonicalReadRuntimeDivergence(
          CanonicalReadRuntimeDivergenceKind.MISSING_OBJECT,
          CanonicalReadDomain.RECORDING_METADATA, objectID, null, 'present', null
        ))
        continue
      }
      CanonicalReadRuntimeDiff.appendMismatch(
        CanonicalReadRuntimeDivergenceKind.TITLE_TAGS_FOLDER_MISMATCH,
        CanonicalReadDomain.RECORDING_METADATA, objectID, 'title',
        legacyRecord.title, canonicalRecord.title, divergences
      )
      CanonicalReadRuntimeDiff.appendMismatch(
        CanonicalReadRuntimeDivergenceKind.TITLE_TAGS_FOLDER_MISMATCH,
        CanonicalReadDomain.RECORDING_METADATA, objectID, 'tags',
        legacyRecord.tagsKey, canonicalRecord.tagsKey, divergences
      )
      CanonicalReadRuntimeDiff.appendMismatch(
        CanonicalReadRuntimeDivergenceKind.METADATA_MISMATCH,
        CanonicalReadDomain.RECORDING_METADATA, objectID, 'deleted',
        String(legacyRecord.isDeleted), String(canonicalRecord.isDeleted), divergences
      )
      CanonicalReadRuntimeDiff.appendMismatch(
        CanonicalReadRuntimeDivergenceKind.METADATA_MISMATCH,
        CanonicalReadDomain.RECORDING_METADATA, objectID, 'metadataHashPrefix',
        legacyRecord.metadataHashPrefix ?? 'nil', canonicalRecord.metadataHashPrefix ?? 'nil', divergences
      )
    }
  }

  private static appendMismatch(
    kind: CanonicalReadRuntimeDivergenceKind,
    domain: CanonicalReadDomain,
    objectID: string,
    field: string,
    legacyValue: string,
    canonicalValue: string,
    divergences: CanonicalReadRuntimeDivergence[]
  ) {
    if (legacyValue === canonicalValue) {
      return
    }
    divergences.push(new CanonicalReadRuntimeDivergence(kind, domain, objectID, field, legacyValue, canonicalValue))
  }
}

// =============================================================================
// READ RUNTIME GATE
// =============================================================================

export enum CanonicalReadRuntimeGateBlocker {
  BLOCKED_MODE = 'blockedMode',
  CANONICAL_SNAPSHOT_MISSING = 'canonicalSnapshotMissing',
  APPLY_RUNTIME_EVIDENCE_MISSING = 'applyRuntimeEvidenceMissing',
  UPLOAD_RUNTIME_EVIDENCE_MISSING = 'uploadRuntimeEvidenceMissing',
  INVENTORY_SNAPSHOT_MISSING = 'inventorySnapshotMissing',
  PLAN_AUTHORITY_EVIDENCE_MISSING = 'planAuthorityEvidenceMissing',
  EXISTENCE_TRUTH_EVIDENCE_MISSING = 'existenceTruthEvidenceMissing',
  DIVERGENCE_PRESENT = 'divergencePresent',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  OTHER_DOMAIN_CONFLICT = 'otherDomainConflict',
  RELEASE_DEFAULT_BUILD = 'releaseDefaultBuild',
  DEBUG_INTERNAL_APPROVAL_MISSING = 'debugInternalApprovalMissing',
  MANUAL_OWNER_APPROVAL_MISSING = 'manualOwnerApprovalMissing',
  DIAGNOSTICS_NOT_REDACTED = 'diagnosticsNotRedacted',
  READ_MAY_TRIGGER_SYNC_UPLOAD = 'readMayTriggerSyncUpload',
  READ_MAY_MUTATE_STORE = 'readMayMutateStore',
  PATH_CONTENT_LEAK_RISK = 'pathContentLeakRisk'
}

export class CanonicalReadRuntimeGateResult {
  allowed: boolean
  blockers: CanonicalReadRuntimeGateBlocker[]
  diagnosticsSummary: string

  constructor(blockers: CanonicalReadRuntimeGateBlocker[]) {
    const uniqueBlockers = Array.from(new Set(blockers)).sort((a, b) => a.localeCompare(b))
    this.allowed = uniqueBlockers.length === 0
    this.blockers = uniqueBlockers
    this.diagnosticsSummary = 'allowed=' + (uniqueBlockers.length === 0) +
      ',blockers=' + uniqueBlockers.join('+')
  }
}

export class CanonicalReadRuntimeGate {
  static evaluate(
    configuration: CanonicalReadRuntimeConfiguration,
    canonicalSnapshotAvailable: boolean,
    diff: CanonicalReadRuntimeDiff | null
  ): CanonicalReadRuntimeGateResult {
    const policy = configuration.policy
    const blockers: CanonicalReadRuntimeGateBlocker[] = []
    if (configuration.mode === CanonicalReadRuntimeMode.BLOCKED) {
      blockers.push(CanonicalReadRuntimeGateBlocker.BLOCKED_MODE)
    }
    if (!canonicalSnapshotAvailable) {
      blockers.push(CanonicalReadRuntimeGateBlocker.CANONICAL_SNAPSHOT_MISSING)
    }
    if (!policy.applyRuntimeEvidenceValidForNonAudio) {
      blockers.push(CanonicalReadRuntimeGateBlocker.APPLY_RUNTIME_EVIDENCE_MISSING)
    }
    if (!policy.uploadRuntimeEvidenceValidForAudioStatus) {
      blockers.push(CanonicalReadRuntimeGateBlocker.UPLOAD_RUNTIME_EVIDENCE_MISSING)
    }
    if (!policy.inventorySnapshotAvailable) {
      blockers.push(CanonicalReadRuntimeGateBlocker.INVENTORY_SNAPSHOT_MISSING)
    }
    if (!policy.planAuthorityEvidenceValid) {
      blockers.push(CanonicalReadRuntimeGateBlocker.PLAN_AUTHORITY_EVIDENCE_MISSING)
    }
    if (!policy.existenceTruthEvidenceValid) {
      blockers.push(CanonicalReadRuntimeGateBlocker.EXISTENCE_TRUTH_EVIDENCE_MISSING)
    }
    if (!policy.legacyFallbackAvailable) {
      blockers.push(CanonicalReadRuntimeGateBlocker.LEGACY_FALLBACK_UNAVAILABLE)
    }
    if (!policy.otherDomainsNotConflicting) {
      blockers.push(CanonicalReadRuntimeGateBlocker.OTHER_DOMAIN_CONFLICT)
    }
    if (policy.releaseDefaultBuild) {
      blockers.push(CanonicalReadRuntimeGateBlocker.RELEASE_DEFAULT_BUILD)
    }
    if (!policy.debugInternalBuild || !policy.ownerApproved) {
      blockers.push(CanonicalReadRuntimeGateBlocker.DEBUG_INTERNAL_APPROVAL_MISSING)
    }
    if (!policy.manualOwnerApproval) {
      blockers.push(CanonicalReadRuntimeGateBlocker.MANUAL_OWNER_APPROVAL_MISSING)
    }
    if (!policy.diagnosticsRedacted) {
      blockers.push(CanonicalReadRuntimeGateBlocker.DIAGNOSTICS_NOT_REDACTED)
    }
    if (!policy.readMustNotTriggerSyncUpload) {
      blockers.push(CanonicalReadRuntimeGateBlocker.READ_MAY_TRIGGER_SYNC_UPLOAD)
    }
    if (!policy.readMustNotMutateStore) {
      blockers.push(CanonicalReadRuntimeGateBlocker.READ_MAY_MUTATE_STORE)
    }
    if (diff) {
      if (diff.divergenceCount > 0 && !policy.allowDivergentGuardedReadForTests) {
        blockers.push(CanonicalReadRuntimeGateBlocker.DIVERGENCE_PRESENT)
      }
      if (diff.divergences.some(d => d.kind === CanonicalReadRuntimeDivergenceKind.PATH_CONTENT_LEAK_RISK)) {
        blockers.push(CanonicalReadRuntimeGateBlocker.PATH_CONTENT_LEAK_RISK)
      }
    }
    return new CanonicalReadRuntimeGateResult(blockers)
  }
}

// =============================================================================
// READ RUNTIME DIAGNOSTIC
// =============================================================================

export enum CanonicalReadRuntimeDiagnosticKind {
  CANONICAL_READ_RUNTIME_MODE_EVALUATED = 'canonicalReadRuntimeModeEvaluated',
  CANONICAL_READ_RUNTIME_SERVED_CANONICAL = 'canonicalReadRuntimeServedCanonical',
  CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK = 'canonicalReadRuntimeServedLegacyFallback',
  CANONICAL_READ_RUNTIME_DIFF_EQUIVALENT = 'canonicalReadRuntimeDiffEquivalent',
  CANONICAL_READ_RUNTIME_DIFF_DIVERGENT = 'canonicalReadRuntimeDiffDivergent',
  CANONICAL_READ_RUNTIME_BLOCKED = 'canonicalReadRuntimeBlocked',
  CANONICAL_READ_RUNTIME_REPORT_BUILT = 'canonicalReadRuntimeReportBuilt'
}

export class CanonicalReadRuntimeDiagnostic {
  kind: CanonicalReadRuntimeDiagnosticKind
  syncRunID: string | null
  mode: CanonicalReadRuntimeMode
  source: CanonicalReadProjectionSource | null
  count: number | null
  detail: string | null

  constructor(
    kind: CanonicalReadRuntimeDiagnosticKind,
    syncRunID: string | null,
    mode: CanonicalReadRuntimeMode,
    source: CanonicalReadProjectionSource | null = null,
    count: number | null = null,
    detail: string | null = null
  ) {
    this.kind = kind
    this.syncRunID = syncRunID ? CanonicalReadRuntimeRedaction.safeText(syncRunID) : null
    this.mode = mode
    this.source = source
    this.count = count
    this.detail = detail ? CanonicalReadRuntimeRedaction.safeText(detail) : null
  }

  get id(): string {
    return [this.kind, this.syncRunID ?? '', this.mode, this.detail ?? ''].join('|')
  }

  get diagnosticsSummary(): string {
    const parts: string[] = ['kind=' + this.kind, 'mode=' + this.mode]
    if (this.source) { parts.push('source=' + this.source) }
    if (this.count !== null) { parts.push('count=' + this.count) }
    if (this.detail) { parts.push('detail=' + this.detail) }
    return parts.join(',')
  }
}

// =============================================================================
// READ RUNTIME FALLBACK
// =============================================================================

export enum CanonicalReadRuntimeFallback {
  NONE = 'none',
  LEGACY_DEFAULT = 'legacyDefault',
  PARALLEL_COMPARE_RETURNS_LEGACY = 'parallelCompareReturnsLegacy',
  CANONICAL_CANDIDATE_NOT_SERVED = 'canonicalCandidateNotServed',
  GUARDED_GATE_BLOCKED = 'guardedGateBlocked',
  CANONICAL_PROJECTION_MISSING = 'canonicalProjectionMissing',
  CANONICAL_READ_EXCEPTION = 'canonicalReadException',
  BLOCKED_MODE = 'blockedMode'
}

// =============================================================================
// READ RUNTIME RESULT
// =============================================================================

export class CanonicalReadRuntimeResult {
  mode: CanonicalReadRuntimeMode
  returnedSource: CanonicalReadProjectionSource
  readSnapshot: CanonicalReadSnapshot
  legacySnapshot: CanonicalReadSnapshot
  canonicalCandidate: CanonicalReadSnapshot | null
  diff: CanonicalReadRuntimeDiff | null
  gateResult: CanonicalReadRuntimeGateResult | null
  fallback: CanonicalReadRuntimeFallback
  canonicalReadServed: boolean
  legacyFallbackServed: boolean
  canonicalCandidateBuilt: boolean
  storeMutated: boolean
  syncOrUploadTriggered: boolean
  uploadJobCreated: boolean
  resourceMoved: boolean
  productionDataWritten: boolean
  diagnostics: CanonicalReadRuntimeDiagnostic[]
  diagnosticsSummary: string

  constructor(
    mode: CanonicalReadRuntimeMode,
    returnedSource: CanonicalReadProjectionSource,
    readSnapshot: CanonicalReadSnapshot,
    legacySnapshot: CanonicalReadSnapshot,
    canonicalCandidate: CanonicalReadSnapshot | null,
    diff: CanonicalReadRuntimeDiff | null,
    gateResult: CanonicalReadRuntimeGateResult | null,
    fallback: CanonicalReadRuntimeFallback,
    canonicalReadServed: boolean,
    legacyFallbackServed: boolean,
    canonicalCandidateBuilt: boolean,
    storeMutated: boolean,
    syncOrUploadTriggered: boolean,
    uploadJobCreated: boolean,
    resourceMoved: boolean,
    productionDataWritten: boolean,
    diagnostics: CanonicalReadRuntimeDiagnostic[],
    diagnosticsSummary: string
  ) {
    this.mode = mode
    this.returnedSource = returnedSource
    this.readSnapshot = readSnapshot
    this.legacySnapshot = legacySnapshot
    this.canonicalCandidate = canonicalCandidate
    this.diff = diff
    this.gateResult = gateResult
    this.fallback = fallback
    this.canonicalReadServed = canonicalReadServed
    this.legacyFallbackServed = legacyFallbackServed
    this.canonicalCandidateBuilt = canonicalCandidateBuilt
    this.storeMutated = storeMutated
    this.syncOrUploadTriggered = syncOrUploadTriggered
    this.uploadJobCreated = uploadJobCreated
    this.resourceMoved = resourceMoved
    this.productionDataWritten = productionDataWritten
    this.diagnostics = diagnostics
    this.diagnosticsSummary = diagnosticsSummary
  }
}

// =============================================================================
// READ RUNTIME PROVIDER
// =============================================================================

export class CanonicalReadRuntimeProvider {
  configuration: CanonicalReadRuntimeConfiguration

  constructor(configuration: CanonicalReadRuntimeConfiguration = new CanonicalReadRuntimeConfiguration()) {
    this.configuration = configuration
  }

  read(
    legacySnapshot: CanonicalReadSnapshot,
    canonicalSnapshot: CanonicalReadSnapshot | null,
    syncRunID: string | null = null,
    canonicalReadFailureReason: string | null = null
  ): CanonicalReadRuntimeResult {
    const mode = this.configuration.mode
    const evaluated = this.diagnostic(
      CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_MODE_EVALUATED,
      syncRunID, null, null, 'mode=' + mode
    )

    switch (mode) {
      case CanonicalReadRuntimeMode.DISABLED:
        return this.makeResult(
          legacySnapshot, legacySnapshot, null, null, null,
          CanonicalReadRuntimeFallback.LEGACY_DEFAULT,
          [evaluated, this.diagnostic(CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
            syncRunID, CanonicalReadProjectionSource.LEGACY, null, 'disabledDefaultLegacy')]
        )
      case CanonicalReadRuntimeMode.BLOCKED: {
        const diff = canonicalSnapshot
          ? CanonicalReadRuntimeDiff.compare(legacySnapshot, canonicalSnapshot)
          : null
        return this.makeResult(
          legacySnapshot, legacySnapshot, canonicalSnapshot, diff,
          new CanonicalReadRuntimeGateResult([CanonicalReadRuntimeGateBlocker.BLOCKED_MODE]),
          CanonicalReadRuntimeFallback.BLOCKED_MODE,
          [
            evaluated,
            this.diagnostic(CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_BLOCKED,
              syncRunID, CanonicalReadProjectionSource.LEGACY, 1, 'blockedMode'),
            this.diagnostic(CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
              syncRunID, CanonicalReadProjectionSource.LEGACY, null, 'blockedMode')
          ].concat(this.diffDiagnostics(diff, syncRunID))
        )
      }
      case CanonicalReadRuntimeMode.PARALLEL_COMPARE:
      case CanonicalReadRuntimeMode.CANONICAL_READ_CANDIDATE: {
        const diff = canonicalSnapshot
          ? CanonicalReadRuntimeDiff.compare(legacySnapshot, canonicalSnapshot)
          : null
        const fallback = mode === CanonicalReadRuntimeMode.PARALLEL_COMPARE
          ? CanonicalReadRuntimeFallback.PARALLEL_COMPARE_RETURNS_LEGACY
          : CanonicalReadRuntimeFallback.CANONICAL_CANDIDATE_NOT_SERVED
        const reason = mode === CanonicalReadRuntimeMode.PARALLEL_COMPARE
          ? 'parallelCompareReturnsLegacy' : 'canonicalCandidateNotServed'
        return this.makeResult(
          legacySnapshot, legacySnapshot, canonicalSnapshot, diff, null, fallback,
          [evaluated, this.diagnostic(CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
            syncRunID, CanonicalReadProjectionSource.LEGACY, null, reason)]
            .concat(this.diffDiagnostics(diff, syncRunID))
        )
      }
      case CanonicalReadRuntimeMode.GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK: {
        if (!canonicalSnapshot) {
          return this.makeResult(
            legacySnapshot, legacySnapshot, null, null,
            CanonicalReadRuntimeGate.evaluate(this.configuration, false, null),
            CanonicalReadRuntimeFallback.CANONICAL_PROJECTION_MISSING,
            [
              evaluated,
              this.diagnostic(CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_BLOCKED,
                syncRunID, CanonicalReadProjectionSource.LEGACY, 1, 'canonicalProjectionMissing'),
              this.diagnostic(CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
                syncRunID, CanonicalReadProjectionSource.LEGACY, null, 'canonicalProjectionMissing')
            ]
          )
        }
        const diff = CanonicalReadRuntimeDiff.compare(legacySnapshot, canonicalSnapshot)
        const gate = CanonicalReadRuntimeGate.evaluate(this.configuration, true, diff)
        let diagnostics = [evaluated].concat(this.diffDiagnostics(diff, syncRunID))
        if (canonicalReadFailureReason) {
          diagnostics.push(this.diagnostic(
            CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
            syncRunID, CanonicalReadProjectionSource.LEGACY, null, canonicalReadFailureReason
          ))
          return this.makeResult(
            legacySnapshot, legacySnapshot, canonicalSnapshot, diff, gate,
            CanonicalReadRuntimeFallback.CANONICAL_READ_EXCEPTION, diagnostics
          )
        }
        if (!gate.allowed) {
          diagnostics.push(this.diagnostic(
            CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_BLOCKED,
            syncRunID, CanonicalReadProjectionSource.LEGACY, gate.blockers.length,
            gate.blockers.join('+')
          ))
          diagnostics.push(this.diagnostic(
            CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_LEGACY_FALLBACK,
            syncRunID, CanonicalReadProjectionSource.LEGACY, null, 'guardedGateBlocked'
          ))
          return this.makeResult(
            legacySnapshot, legacySnapshot, canonicalSnapshot, diff, gate,
            CanonicalReadRuntimeFallback.GUARDED_GATE_BLOCKED, diagnostics
          )
        }
        diagnostics.push(this.diagnostic(
          CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_SERVED_CANONICAL,
          syncRunID, CanonicalReadProjectionSource.CANONICAL, null,
          'guardedCanonicalReadWithLegacyFallback'
        ))
        return this.makeResult(
          canonicalSnapshot, legacySnapshot, canonicalSnapshot, diff, gate,
          CanonicalReadRuntimeFallback.NONE, diagnostics
        )
      }
    }
  }

  private makeResult(
    returnedSnapshot: CanonicalReadSnapshot,
    legacySnapshot: CanonicalReadSnapshot,
    canonicalSnapshot: CanonicalReadSnapshot | null,
    diff: CanonicalReadRuntimeDiff | null,
    gate: CanonicalReadRuntimeGateResult | null,
    fallback: CanonicalReadRuntimeFallback,
    diagnostics: CanonicalReadRuntimeDiagnostic[]
  ): CanonicalReadRuntimeResult {
    let diags = diagnostics
    diags.push(this.diagnostic(
      CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_REPORT_BUILT,
      diags.length > 0 ? diags[0].syncRunID : null, returnedSnapshot.source,
      diff?.divergenceCount ?? null, 'fallback=' + fallback
    ))
    const limitedDiags = diags.slice(0, this.configuration.policy.maxDiagnosticsEvents)
    const served = returnedSnapshot.source === CanonicalReadProjectionSource.CANONICAL &&
      fallback === CanonicalReadRuntimeFallback.NONE
    const summaryParts = [
      'mode=' + this.configuration.mode,
      'returned=' + returnedSnapshot.source,
      'fallback=' + fallback,
      'canonicalServed=' + served,
      'canonicalCandidateBuilt=' + (canonicalSnapshot !== null &&
        this.configuration.mode.buildsCanonicalCandidate),
      'divergences=' + (diff?.divergenceCount ?? 0),
      'storeMutated=false',
      'syncOrUploadTriggered=false',
      'uploadJobCreated=false',
      'resourceMoved=false',
      'productionDataWritten=false'
    ]
    return new CanonicalReadRuntimeResult(
      this.configuration.mode, returnedSnapshot.source, returnedSnapshot,
      legacySnapshot, canonicalSnapshot, diff, gate, fallback, served,
      returnedSnapshot.source === CanonicalReadProjectionSource.LEGACY && fallback !== CanonicalReadRuntimeFallback.NONE,
      canonicalSnapshot !== null && this.configuration.mode.buildsCanonicalCandidate,
      false, false, false, false, false, limitedDiags, summaryParts.join(',')
    )
  }

  private diffDiagnostics(diff: CanonicalReadRuntimeDiff | null, syncRunID: string | null): CanonicalReadRuntimeDiagnostic[] {
    if (!diff) {
      return []
    }
    return [this.diagnostic(
      diff.equivalent ? CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_DIFF_EQUIVALENT
        : CanonicalReadRuntimeDiagnosticKind.CANONICAL_READ_RUNTIME_DIFF_DIVERGENT,
      syncRunID, CanonicalReadProjectionSource.CANONICAL, diff.divergenceCount,
      diff.equivalenceReport.diagnosticsSummary
    )]
  }

  private diagnostic(
    kind: CanonicalReadRuntimeDiagnosticKind,
    syncRunID: string | null,
    source: CanonicalReadProjectionSource | null,
    count: number | null = null,
    detail: string | null = null
  ): CanonicalReadRuntimeDiagnostic {
    return new CanonicalReadRuntimeDiagnostic(
      kind, syncRunID, this.configuration.mode, source, count, detail
    )
  }
}

// =============================================================================
// READ RUNTIME REDACTION
// =============================================================================

export class CanonicalReadRuntimeRedaction {
  static safeIdentifier(value: string, fallback: string): string {
    return CanonicalProductionRedaction.safeIdentifier(value, fallback)
  }

  static safeDisplayText(value: string, fallback: string): string {
    return CanonicalProductionRedaction.safeDiagnosticText(value) ?? fallback
  }

  static safeText(value: string | null): string | null {
    if (!value) {
      return null
    }
    return CanonicalProductionRedaction.safeDiagnosticText(value)
  }

  static hashPrefix(value: string | null): string | null {
    if (!value) {
      return null
    }
    return CanonicalProductionRedaction.hashPrefix(value)
  }

  static containsForbiddenSignal(value: string): boolean {
    return CanonicalReadRuntimeRedaction.containsSensitivePathSignal(value) ||
      value.includes('{') || value.includes('}') ||
      value.includes('://') || value.length > 320
  }

  private static containsSensitivePathSignal(value: string): boolean {
    return CanonicalProductionRedaction.containsSensitivePathSignal(value) ||
      value.includes('/') || value.includes('\\')
  }
}
