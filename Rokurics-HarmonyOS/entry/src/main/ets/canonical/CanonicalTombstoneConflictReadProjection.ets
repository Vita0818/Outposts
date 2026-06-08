/**
 * CanonicalTombstoneConflictReadProjection — read-side projection for tombstone & conflict.
 * Ported from Rokurics-Apple (CanonicalTombstoneConflictReadProjection.swift).
 * Builds snapshots, compares legacy vs canonical, evaluates observation & retirement.
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalNode, CanonicalRecordingObject, CanonicalArtifact,
  CanonicalArtifactKind, CanonicalManifest, hashPrefix
} from './CanonicalCore'
import {
  CanonicalSyncPlanTrigger
} from './CanonicalAudioUploadCutover'
import {
  CanonicalApplyAction, CanonicalApplyActionKind, CanonicalApplyPlan,
  CanonicalProductionExecutionDomainRole, CanonicalProductionDomain
} from './CanonicalGeneratedArtifactCutover'
import {
  CanonicalTombstoneState, CanonicalTombstoneConflictDomain, CanonicalTombstoneConflictActionKind,
  CanonicalTombstoneConflictReadSideBlocker as TcReadSideBlockerOrig
} from './CanonicalTombstoneConflictCutover'
import {
  CanonicalLibraryObject, CanonicalLibraryTombstone, CanonicalLibraryConflict,
  CanonicalLibrarySyncPlan, CanonicalConflictRecord
} from './CanonicalTombstoneConflictCutover'

// ── Minimal forward types ──

class CanonicalProductionRedaction {
  static safeIdentifier(value: string, fallback: string): string {
    const trimmed = value.trim()
    if (trimmed.length === 0) {
      return fallback
    }
    return trimmed.replace(/[^a-zA-Z0-9\-_:.]/g, '-').substring(0, 96) || fallback
  }

  static safeDiagnosticText(value: string | null): string | null {
    if (!value) {
      return null
    }
    const trimmed = value.trim()
    return trimmed.length === 0 ? null : trimmed.substring(0, 160)
  }

  static hashPrefix(value: string | null): string | null {
    if (!value || value.trim().length === 0) {
      return null
    }
    return value.trim().substring(0, 12)
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS
// ═══════════════════════════════════════════════════════════════════════════════

export enum CanonicalTombstoneConflictReadProjectionSource {
  LEGACY = 'legacy',
  CANONICAL = 'canonical'
}

export enum CanonicalTombstoneConflictDeletedDisplayState {
  ACTIVE = 'active',
  DELETED = 'deleted',
  TRASHED = 'trashed',
  TOMBSTONED = 'tombstoned',
  UNKNOWN = 'unknown'
}

export enum CanonicalTombstoneConflictStatus {
  NONE = 'none',
  RECORDED = 'recorded',
  UNRESOLVED = 'unresolved',
  MANUAL_REVIEW_REQUIRED = 'manualReviewRequired'
}

export enum CanonicalTombstoneConflictAntiResurrectionStatus {
  NOT_TRIGGERED = 'notTriggered',
  BLOCKED = 'blocked',
  RISK = 'risk',
  EXPLICIT_RESTORE_REQUIRED = 'explicitRestoreRequired'
}

export enum CanonicalTombstoneConflictReadProjectionFailureKind {
  SNAPSHOT_MISSING = 'snapshotMissing',
  UNSUPPORTED_OBJECT_KIND = 'unsupportedObjectKind',
  PATH_LEAK_RISK = 'pathLeakRisk',
  FULL_METADATA_REJECTED = 'fullMetadataRejected',
  FULL_CONTENT_REJECTED = 'fullContentRejected',
  PHYSICAL_DELETE_RISK = 'physicalDeleteRisk',
  PERMANENT_DELETE_RISK = 'permanentDeleteRisk',
  TOMBSTONE_GC_RISK = 'tombstoneGCRisk',
  STALE_LIVE_RESURRECTION_RISK = 'staleLiveResurrectionRisk',
  AUTO_CONFLICT_RESOLUTION_RISK = 'autoConflictResolutionRisk'
}

export enum CanonicalTombstoneConflictReadSideDivergenceKind {
  MISSING_IN_CANONICAL = 'missingInCanonical',
  MISSING_IN_LEGACY = 'missingInLegacy',
  TOMBSTONE_STATE_MISMATCH = 'tombstoneStateMismatch',
  TOMBSTONE_TIMESTAMP_MISMATCH = 'tombstoneTimestampMismatch',
  CONFLICT_RECORD_MISMATCH = 'conflictRecordMismatch',
  ACTIVE_VS_TOMBSTONE_MISMATCH = 'activeVsTombstoneMismatch',
  RESURRECTION_BLOCK_MISMATCH = 'resurrectionBlockMismatch',
  SOFT_DELETE_MARKER_MISMATCH = 'softDeleteMarkerMismatch',
  PHYSICAL_DELETE_RISK = 'physicalDeleteRisk',
  PERMANENT_DELETE_RISK = 'permanentDeleteRisk',
  TOMBSTONE_GC_RISK = 'tombstoneGCRisk',
  AUTO_CONFLICT_RESOLUTION_RISK = 'autoConflictResolutionRisk',
  STALE_LIVE_RESURRECTION_RISK = 'staleLiveResurrectionRisk',
  UNSUPPORTED_OBJECT_KIND = 'unsupportedObjectKind',
  PATH_LEAK_RISK = 'pathLeakRisk'
}

export enum CanonicalTombstoneConflictReadSideBlocker {
  MISSING_LEGACY_SNAPSHOT = 'missingLegacySnapshot',
  MISSING_CANONICAL_SNAPSHOT = 'missingCanonicalSnapshot',
  BLOCKING_DIVERGENCE = 'blockingDivergence',
  PHYSICAL_DELETE_RISK = 'physicalDeleteRisk',
  PERMANENT_DELETE_RISK = 'permanentDeleteRisk',
  TOMBSTONE_GC_RISK = 'tombstoneGCRisk',
  STALE_LIVE_RESURRECTION_RISK = 'staleLiveResurrectionRisk',
  AUTO_CONFLICT_RESOLUTION_RISK = 'autoConflictResolutionRisk',
  UNSUPPORTED_OBJECT_KIND = 'unsupportedObjectKind',
  PATH_LEAK_RISK = 'pathLeakRisk'
}

export enum CanonicalTombstoneConflictReadSideDiagnosticKind {
  CANONICAL_TOMBSTONE_CONFLICT_READ_SIDE_PARALLEL_STARTED = 'canonicalTombstoneConflictReadSideParallelStarted',
  CANONICAL_TOMBSTONE_CONFLICT_READ_SIDE_PARALLEL_COMPLETED = 'canonicalTombstoneConflictReadSideParallelCompleted',
  CANONICAL_TOMBSTONE_CONFLICT_READ_SIDE_DIVERGENT = 'canonicalTombstoneConflictReadSideDivergent',
  CANONICAL_TOMBSTONE_CONFLICT_READ_SIDE_EQUIVALENT = 'canonicalTombstoneConflictReadSideEquivalent',
  CANONICAL_TOMBSTONE_CONFLICT_PHYSICAL_DELETE_RISK_BLOCKED = 'canonicalTombstoneConflictPhysicalDeleteRiskBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_PERMANENT_DELETE_RISK_BLOCKED = 'canonicalTombstoneConflictPermanentDeleteRiskBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_GC_RISK_BLOCKED = 'canonicalTombstoneConflictGCRiskBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_STALE_LIVE_RESURRECTION_BLOCKED = 'canonicalTombstoneConflictStaleLiveResurrectionBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_AUTO_RESOLUTION_BLOCKED = 'canonicalTombstoneConflictAutoResolutionBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_RUNTIME_SWITCH_DENIED = 'canonicalTombstoneConflictRuntimeSwitchDenied',
  CANONICAL_TOMBSTONE_CONFLICT_NO_MUTATION_ASSERTED = 'canonicalTombstoneConflictNoMutationAsserted',
  CANONICAL_TOMBSTONE_CONFLICT_RETIREMENT_CANDIDATE_EVALUATED = 'canonicalTombstoneConflictRetirementCandidateEvaluated',
  CANONICAL_TOMBSTONE_CONFLICT_RETIREMENT_CANDIDATE_BLOCKED = 'canonicalTombstoneConflictRetirementCandidateBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_OBSERVATION_WINDOW_STARTED = 'canonicalTombstoneConflictObservationWindowStarted',
  CANONICAL_TOMBSTONE_CONFLICT_OBSERVATION_WINDOW_BLOCKED = 'canonicalTombstoneConflictObservationWindowBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_TEMPLATE_AUDIT_STARTED = 'canonicalTombstoneConflictTemplateAuditStarted',
  CANONICAL_TOMBSTONE_CONFLICT_TEMPLATE_AUDIT_COMPLETED = 'canonicalTombstoneConflictTemplateAuditCompleted',
  CANONICAL_TOMBSTONE_CONFLICT_TEMPLATE_BLOCKED = 'canonicalTombstoneConflictTemplateBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_NEXT_PILOT_CANDIDATE_EVALUATED = 'canonicalTombstoneConflictNextPilotCandidateEvaluated',
  CANONICAL_TOMBSTONE_CONFLICT_NEXT_PILOT_CANDIDATE_BLOCKED = 'canonicalTombstoneConflictNextPilotCandidateBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_NEXT_PILOT_CANDIDATE_READY = 'canonicalTombstoneConflictNextPilotCandidateReady'
}

export enum CanonicalTombstoneConflictReadSideMode {
  DISABLED = 'disabled',
  PARALLEL_ONLY = 'parallelOnly'
}

export enum CanonicalTombstoneConflictObservationGateState {
  DISABLED = 'disabled',
  INCOMPLETE = 'incomplete',
  BLOCKED = 'blocked',
  COMPLETE = 'complete'
}

export enum CanonicalTombstoneConflictObservationEventKind {
  NO_COMMIT = 'noCommit',
  COMMIT = 'commit',
  ROLLBACK = 'rollback',
  LEGACY_FALLBACK = 'legacyFallback',
  READ_SIDE_EQUIVALENT = 'readSideEquivalent',
  READ_SIDE_DIVERGENT = 'readSideDivergent',
  PHYSICAL_DELETE_RISK = 'physicalDeleteRisk',
  PERMANENT_DELETE_RISK = 'permanentDeleteRisk',
  TOMBSTONE_GC_RISK = 'tombstoneGCRisk',
  STALE_LIVE_RESURRECTION_RISK = 'staleLiveResurrectionRisk',
  AUTO_CONFLICT_RESOLUTION_RISK = 'autoConflictResolutionRisk',
  UNSUPPORTED_OBJECT = 'unsupportedObject'
}

// ═══════════════════════════════════════════════════════════════════════════════
// CLASSES
// ═══════════════════════════════════════════════════════════════════════════════

export class CanonicalTombstoneConflictReadProjectionFailure {
  kind: CanonicalTombstoneConflictReadProjectionFailureKind
  source: CanonicalTombstoneConflictReadProjectionSource
  objectID: string | null
  objectKind: string | null
  reason: string

  constructor(
    kind: CanonicalTombstoneConflictReadProjectionFailureKind,
    source: CanonicalTombstoneConflictReadProjectionSource, objectID: string | null = null,
    objectKind: string | null = null, reason: string
  ) {
    this.kind = kind
    this.source = source
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'tombstone-object') : null
    this.objectKind = objectKind
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? kind
  }

  get id(): string {
    return [this.kind, this.objectID ?? 'run', this.objectKind ?? 'unknown', this.reason].join('|')
  }
}

export class CanonicalTombstoneConflictReadProjectionFact {
  objectID: string
  objectKind: string
  tombstoneState: CanonicalTombstoneState
  deletedDisplayState: CanonicalTombstoneConflictDeletedDisplayState
  tombstoneTimestamp: CanonicalTimestamp | null
  conflictKind: string | null
  conflictStatus: CanonicalTombstoneConflictStatus
  activeVsTombstoneState: string
  antiResurrectionStatus: CanonicalTombstoneConflictAntiResurrectionStatus
  parentObjectTombstoned: boolean
  generatedArtifactResurrectionBlocked: boolean
  softDeleteMarkerPresent: boolean
  hashPrefix: string | null
  unsupportedObjectKind: boolean
  pathLeakRisk: boolean
  fullMetadataIncluded: boolean
  fullContentIncluded: boolean
  physicalDeleteRisk: boolean
  permanentDeleteRisk: boolean
  tombstoneGCRisk: boolean
  staleLiveResurrectionRisk: boolean
  autoConflictResolutionRisk: boolean

  constructor(
    objectID: string, objectKind: string,
    tombstoneState: CanonicalTombstoneState = CanonicalTombstoneState.ACTIVE,
    deletedDisplayState: CanonicalTombstoneConflictDeletedDisplayState = CanonicalTombstoneConflictDeletedDisplayState.ACTIVE,
    tombstoneTimestamp: CanonicalTimestamp | null = null, conflictKind: string | null = null,
    conflictStatus: CanonicalTombstoneConflictStatus = CanonicalTombstoneConflictStatus.NONE,
    activeVsTombstoneState: string = 'activeOnly',
    antiResurrectionStatus: CanonicalTombstoneConflictAntiResurrectionStatus = CanonicalTombstoneConflictAntiResurrectionStatus.NOT_TRIGGERED,
    parentObjectTombstoned: boolean = false, generatedArtifactResurrectionBlocked: boolean = false,
    softDeleteMarkerPresent: boolean = false, hashPrefix: string | null = null,
    unsupportedObjectKind: boolean = false, pathLeakRisk: boolean = false,
    fullMetadataIncluded: boolean = false, fullContentIncluded: boolean = false,
    physicalDeleteRisk: boolean = false, permanentDeleteRisk: boolean = false,
    tombstoneGCRisk: boolean = false, staleLiveResurrectionRisk: boolean = false,
    autoConflictResolutionRisk: boolean = false
  ) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'tombstone-object')
    this.objectKind = objectKind
    this.tombstoneState = tombstoneState
    this.deletedDisplayState = deletedDisplayState
    this.tombstoneTimestamp = tombstoneTimestamp
    this.conflictKind = conflictKind ? CanonicalProductionRedaction.safeDiagnosticText(conflictKind) : null
    this.conflictStatus = conflictStatus
    this.activeVsTombstoneState = CanonicalProductionRedaction.safeDiagnosticText(activeVsTombstoneState) ?? 'activeOnly'
    this.antiResurrectionStatus = antiResurrectionStatus
    this.parentObjectTombstoned = parentObjectTombstoned
    this.generatedArtifactResurrectionBlocked = generatedArtifactResurrectionBlocked
    this.softDeleteMarkerPresent = softDeleteMarkerPresent
    this.hashPrefix = CanonicalProductionRedaction.hashPrefix(hashPrefix)
    this.unsupportedObjectKind = unsupportedObjectKind
    this.pathLeakRisk = pathLeakRisk
    this.fullMetadataIncluded = fullMetadataIncluded
    this.fullContentIncluded = fullContentIncluded
    this.physicalDeleteRisk = physicalDeleteRisk
    this.permanentDeleteRisk = permanentDeleteRisk
    this.tombstoneGCRisk = tombstoneGCRisk
    this.staleLiveResurrectionRisk = staleLiveResurrectionRisk
    this.autoConflictResolutionRisk = autoConflictResolutionRisk
  }
}

export class CanonicalTombstoneConflictReadProjectionItem {
  source: CanonicalTombstoneConflictReadProjectionSource
  objectID: string
  objectKind: string
  tombstoneState: CanonicalTombstoneState
  deletedDisplayState: CanonicalTombstoneConflictDeletedDisplayState
  tombstoneTimestampSummary: string
  conflictKind: string
  conflictStatus: CanonicalTombstoneConflictStatus
  activeVsTombstoneState: string
  antiResurrectionStatus: CanonicalTombstoneConflictAntiResurrectionStatus
  parentObjectStateSummary: string
  generatedArtifactResurrectionBlocked: boolean
  softDeleteMarkerPresent: boolean
  hashPrefix: string | null
  fullMetadataIncluded: boolean
  fullContentIncluded: boolean
  absolutePathIncluded: boolean
  physicalDeleteTargetPathIncluded: boolean
  physicalDeleteRisk: boolean
  permanentDeleteRisk: boolean
  tombstoneGCRisk: boolean
  autoConflictResolutionRisk: boolean
  staleLiveResurrectionRisk: boolean

  constructor(source: CanonicalTombstoneConflictReadProjectionSource,
    fact: CanonicalTombstoneConflictReadProjectionFact) {
    this.source = source
    this.objectID = fact.objectID
    this.objectKind = fact.objectKind
    this.tombstoneState = fact.tombstoneState
    this.deletedDisplayState = fact.deletedDisplayState
    this.tombstoneTimestampSummary =
      CanonicalTombstoneConflictReadProjectionItem.timestampSummary(fact.tombstoneTimestamp)
    this.conflictKind = fact.conflictKind ?? 'none'
    this.conflictStatus = fact.conflictStatus
    this.activeVsTombstoneState = fact.activeVsTombstoneState
    this.antiResurrectionStatus = fact.antiResurrectionStatus
    this.parentObjectStateSummary = fact.parentObjectTombstoned ? 'parentTombstoned' : 'parentActiveOrUnknown'
    this.generatedArtifactResurrectionBlocked = fact.generatedArtifactResurrectionBlocked
    this.softDeleteMarkerPresent = fact.softDeleteMarkerPresent ||
      fact.tombstoneState === CanonicalTombstoneState.TOMBSTONED
    this.hashPrefix = fact.hashPrefix
    this.fullMetadataIncluded = false
    this.fullContentIncluded = false
    this.absolutePathIncluded = false
    this.physicalDeleteTargetPathIncluded = false
    this.physicalDeleteRisk = fact.physicalDeleteRisk
    this.permanentDeleteRisk = fact.permanentDeleteRisk
    this.tombstoneGCRisk = fact.tombstoneGCRisk
    this.autoConflictResolutionRisk = fact.autoConflictResolutionRisk
    this.staleLiveResurrectionRisk = fact.staleLiveResurrectionRisk
  }

  get id(): string {
    return [this.objectID, this.objectKind].join('|')
  }

  private static timestampSummary(timestamp: CanonicalTimestamp | null): string {
    if (!timestamp) {
      return 'tombstoneTimestamp=missing'
    }
    const seconds = Math.floor(timestamp.date.getTime() / 1000).toString()
    const prefix = CanonicalProductionRedaction.hashPrefix(CanonicalHash.sha256OfString(seconds).value) ?? 'missing'
    return 'tombstoneTimestampHash=' + prefix
  }
}

export class CanonicalTombstoneConflictReadSnapshot {
  source: CanonicalTombstoneConflictReadProjectionSource
  generatedAt: CanonicalTimestamp
  items: CanonicalTombstoneConflictReadProjectionItem[]
  failures: CanonicalTombstoneConflictReadProjectionFailure[]
  metadataExcludedCount: number
  contentExcludedCount: number

  constructor(
    source: CanonicalTombstoneConflictReadProjectionSource, generatedAt: Date = new Date(),
    items: CanonicalTombstoneConflictReadProjectionItem[],
    failures: CanonicalTombstoneConflictReadProjectionFailure[],
    metadataExcludedCount: number, contentExcludedCount: number
  ) {
    this.source = source
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    const itemMap: Record<string, CanonicalTombstoneConflictReadProjectionItem> = {}
    for (const i of items) {
      if (!itemMap[i.id]) {
        itemMap[i.id] = i
      }
    }
    this.items = Object.values(itemMap).sort((a, b) => a.id.localeCompare(b.id))
    const failMap: Record<string, CanonicalTombstoneConflictReadProjectionFailure> = {}
    for (const f of failures) {
      if (!failMap[f.id]) {
        failMap[f.id] = f
      }
    }
    this.failures = Object.values(failMap).sort((a, b) => a.id.localeCompare(b.id))
    this.metadataExcludedCount = Math.max(0, metadataExcludedCount)
    this.contentExcludedCount = Math.max(0, contentExcludedCount)
  }

  get itemCount(): number {
    return this.items.length
  }

  get failureCount(): number {
    return this.failures.length
  }
}

export class CanonicalTombstoneConflictReadSideDivergence {
  kind: CanonicalTombstoneConflictReadSideDivergenceKind
  objectID: string
  objectKind: string | null
  field: string | null
  legacyValue: string | null
  canonicalValue: string | null
  fatal: boolean

  constructor(
    kind: CanonicalTombstoneConflictReadSideDivergenceKind, objectID: string,
    objectKind: string | null = null, field: string | null = null, legacyValue: string | null = null,
    canonicalValue: string | null = null, fatal: boolean = false
  ) {
    this.kind = kind
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'tombstone-object')
    this.objectKind = objectKind
    this.field = field ? CanonicalProductionRedaction.safeDiagnosticText(field) : null
    this.legacyValue = legacyValue ? CanonicalProductionRedaction.safeDiagnosticText(legacyValue) : null
    this.canonicalValue = canonicalValue ? CanonicalProductionRedaction.safeDiagnosticText(canonicalValue) : null
    this.fatal = fatal || [
      CanonicalTombstoneConflictReadSideDivergenceKind.PHYSICAL_DELETE_RISK,
      CanonicalTombstoneConflictReadSideDivergenceKind.PERMANENT_DELETE_RISK,
      CanonicalTombstoneConflictReadSideDivergenceKind.TOMBSTONE_GC_RISK,
      CanonicalTombstoneConflictReadSideDivergenceKind.AUTO_CONFLICT_RESOLUTION_RISK,
      CanonicalTombstoneConflictReadSideDivergenceKind.STALE_LIVE_RESURRECTION_RISK,
      CanonicalTombstoneConflictReadSideDivergenceKind.PATH_LEAK_RISK
    ].includes(kind)
  }

  get id(): string {
    return [this.kind, this.objectID, this.objectKind ?? '', this.field ?? ''].join('|')
  }
}

export class CanonicalTombstoneConflictReadSideEquivalence {
  equivalent: boolean
  objectCount: number
  tombstoneCount: number
  conflictCount: number
  diagnosticsSummary: string

  constructor(equivalent: boolean, objectCount: number, tombstoneCount: number, conflictCount: number,
    diagnosticsSummary: string) {
    this.equivalent = equivalent
    this.objectCount = objectCount
    this.tombstoneCount = tombstoneCount
    this.conflictCount = conflictCount
    this.diagnosticsSummary = diagnosticsSummary
  }
}

export class CanonicalTombstoneConflictReadSideDiffReport {
  equivalence: CanonicalTombstoneConflictReadSideEquivalence
  divergences: CanonicalTombstoneConflictReadSideDivergence[]
  blockers: CanonicalTombstoneConflictReadSideBlocker[]
  legacySnapshotSummary: string
  canonicalSnapshotSummary: string
  diagnosticsSummary: string

  constructor(
    equivalence: CanonicalTombstoneConflictReadSideEquivalence,
    divergences: CanonicalTombstoneConflictReadSideDivergence[],
    blockers: CanonicalTombstoneConflictReadSideBlocker[], legacySnapshotSummary: string,
    canonicalSnapshotSummary: string, diagnosticsSummary: string
  ) {
    this.equivalence = equivalence
    this.divergences = divergences
    this.blockers = blockers
    this.legacySnapshotSummary = legacySnapshotSummary
    this.canonicalSnapshotSummary = canonicalSnapshotSummary
    this.diagnosticsSummary = diagnosticsSummary
  }

  get equivalent(): boolean {
    return this.blockers.length === 0 && this.divergences.length === 0
  }

  get divergenceCount(): number {
    return this.divergences.length
  }

  get fatalDivergenceCount(): number {
    return this.divergences.filter(
      (d: CanonicalTombstoneConflictReadSideDivergence) => d.fatal).length
  }

  get unsupportedObjectCount(): number {
    return this.divergences.filter((d: CanonicalTombstoneConflictReadSideDivergence) =>
      d.kind === CanonicalTombstoneConflictReadSideDivergenceKind.UNSUPPORTED_OBJECT_KIND).length
  }

  get physicalDeleteRiskCount(): number {
    return this.divergences.filter((d: CanonicalTombstoneConflictReadSideDivergence) =>
      d.kind === CanonicalTombstoneConflictReadSideDivergenceKind.PHYSICAL_DELETE_RISK).length
  }

  get permanentDeleteRiskCount(): number {
    return this.divergences.filter((d: CanonicalTombstoneConflictReadSideDivergence) =>
      d.kind === CanonicalTombstoneConflictReadSideDivergenceKind.PERMANENT_DELETE_RISK).length
  }

  get tombstoneGCRiskCount(): number {
    return this.divergences.filter((d: CanonicalTombstoneConflictReadSideDivergence) =>
      d.kind === CanonicalTombstoneConflictReadSideDivergenceKind.TOMBSTONE_GC_RISK).length
  }

  get staleLiveResurrectionRiskCount(): number {
    return this.divergences.filter((d: CanonicalTombstoneConflictReadSideDivergence) =>
      d.kind === CanonicalTombstoneConflictReadSideDivergenceKind.STALE_LIVE_RESURRECTION_RISK).length
  }

  get autoConflictResolutionRiskCount(): number {
    return this.divergences.filter((d: CanonicalTombstoneConflictReadSideDivergence) =>
      d.kind === CanonicalTombstoneConflictReadSideDivergenceKind.AUTO_CONFLICT_RESOLUTION_RISK).length
  }
}

export class CanonicalTombstoneConflictReadSideDiagnostic {
  kind: CanonicalTombstoneConflictReadSideDiagnosticKind
  syncRunID: string | null
  trigger: CanonicalSyncPlanTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  objectID: string | null
  objectKind: string | null
  result: string | null
  reason: string | null
  hashPrefix: string | null

  constructor(
    kind: CanonicalTombstoneConflictReadSideDiagnosticKind, syncRunID: string | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole,
    objectID: string | null = null, objectKind: string | null = null, result: string | null = null,
    reason: string | null = null, hashPrefix: string | null = null
  ) {
    this.kind = kind
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'tombstone-object') : null
    this.objectKind = objectKind
    this.result = result ? CanonicalProductionRedaction.safeDiagnosticText(result) : null
    this.reason = reason ? CanonicalProductionRedaction.safeDiagnosticText(reason) : null
    this.hashPrefix = CanonicalProductionRedaction.hashPrefix(hashPrefix)
  }

  get id(): string {
    return [this.kind, this.syncRunID ?? '', this.objectID ?? '', this.result ?? '', this.reason ?? ''].join('|')
  }
}

export class CanonicalTombstoneConflictReadSidePolicy {
  recordDiagnostics: boolean
  maxDiagnosticsEvents: number

  constructor(recordDiagnostics: boolean = true, maxDiagnosticsEvents: number = 200) {
    this.recordDiagnostics = recordDiagnostics
    this.maxDiagnosticsEvents = Math.max(1, maxDiagnosticsEvents)
  }
}

export class CanonicalTombstoneConflictReadSideConfiguration {
  isEnabled: boolean
  mode: CanonicalTombstoneConflictReadSideMode
  policy: CanonicalTombstoneConflictReadSidePolicy

  constructor(
    isEnabled: boolean = false,
    mode: CanonicalTombstoneConflictReadSideMode = CanonicalTombstoneConflictReadSideMode.DISABLED,
    policy: CanonicalTombstoneConflictReadSidePolicy = new CanonicalTombstoneConflictReadSidePolicy()
  ) {
    this.isEnabled = isEnabled
    this.mode = isEnabled ? mode : CanonicalTombstoneConflictReadSideMode.DISABLED
    this.policy = policy
  }

  static get disabled(): CanonicalTombstoneConflictReadSideConfiguration {
    return new CanonicalTombstoneConflictReadSideConfiguration()
  }

  static enabled(
    mode: CanonicalTombstoneConflictReadSideMode = CanonicalTombstoneConflictReadSideMode.PARALLEL_ONLY,
    policy: CanonicalTombstoneConflictReadSidePolicy = new CanonicalTombstoneConflictReadSidePolicy()
  ): CanonicalTombstoneConflictReadSideConfiguration {
    return new CanonicalTombstoneConflictReadSideConfiguration(true, mode, policy)
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// STATIC FUNCTIONS
// ═══════════════════════════════════════════════════════════════════════════════

export function tcSnapshotFromManifests(
  source: CanonicalTombstoneConflictReadProjectionSource, localManifest: CanonicalManifest | null,
  peerManifest: CanonicalManifest | null, applyPlan: CanonicalApplyPlan | null = null,
  libraryPlan: CanonicalLibrarySyncPlan | null = null, generatedAt: Date = new Date()
): CanonicalTombstoneConflictReadSnapshot {
  let facts: CanonicalTombstoneConflictReadProjectionFact[] = []
  let failures: CanonicalTombstoneConflictReadProjectionFailure[] = []
  if (!localManifest && !peerManifest && !applyPlan && !libraryPlan) {
    failures.push(new CanonicalTombstoneConflictReadProjectionFailure(
      CanonicalTombstoneConflictReadProjectionFailureKind.SNAPSHOT_MISSING, source, null, null,
      'tombstoneConflictReadSnapshotMissing'))
  }
  if (localManifest) {
    appendManifestTcFacts(localManifest, false, facts)
  }
  if (peerManifest) {
    appendManifestTcFacts(peerManifest, true, facts)
  }
  if (applyPlan) {
    appendApplyPlanTcFacts(applyPlan, facts)
  }
  if (libraryPlan) {
    appendLibraryPlanTcFacts(libraryPlan, facts)
  }
  return tcSnapshotFromFacts(source, facts, failures, generatedAt)
}

export function tcSnapshotFromFacts(
  source: CanonicalTombstoneConflictReadProjectionSource,
  facts: CanonicalTombstoneConflictReadProjectionFact[],
  seedFailures: CanonicalTombstoneConflictReadProjectionFailure[] = [],
  generatedAt: Date = new Date()
): CanonicalTombstoneConflictReadSnapshot {
  let failures = [...seedFailures]
  let items: CanonicalTombstoneConflictReadProjectionItem[] = []
  const sortedFacts = [...facts].sort((a, b) => a.objectID.localeCompare(b.objectID))
  for (const fact of sortedFacts) {
    if (fact.unsupportedObjectKind) {
      failures.push(new CanonicalTombstoneConflictReadProjectionFailure(
        CanonicalTombstoneConflictReadProjectionFailureKind.UNSUPPORTED_OBJECT_KIND, source, fact.objectID,
        fact.objectKind, 'unsupportedTombstoneConflictObjectKind'))
      continue
    }
    if (fact.pathLeakRisk) {
      failures.push(new CanonicalTombstoneConflictReadProjectionFailure(
        CanonicalTombstoneConflictReadProjectionFailureKind.PATH_LEAK_RISK, source, fact.objectID,
        fact.objectKind, 'unsafePathTokenObserved'))
    }
    if (fact.physicalDeleteRisk) {
      failures.push(new CanonicalTombstoneConflictReadProjectionFailure(
        CanonicalTombstoneConflictReadProjectionFailureKind.PHYSICAL_DELETE_RISK, source, fact.objectID,
        fact.objectKind, 'physicalDeleteForbidden'))
    }
    if (fact.permanentDeleteRisk) {
      failures.push(new CanonicalTombstoneConflictReadProjectionFailure(
        CanonicalTombstoneConflictReadProjectionFailureKind.PERMANENT_DELETE_RISK, source, fact.objectID,
        fact.objectKind, 'permanentDeleteForbidden'))
    }
    if (fact.tombstoneGCRisk) {
      failures.push(new CanonicalTombstoneConflictReadProjectionFailure(
        CanonicalTombstoneConflictReadProjectionFailureKind.TOMBSTONE_GC_RISK, source, fact.objectID,
        fact.objectKind, 'tombstoneGCForbidden'))
    }
    if (fact.staleLiveResurrectionRisk) {
      failures.push(new CanonicalTombstoneConflictReadProjectionFailure(
        CanonicalTombstoneConflictReadProjectionFailureKind.STALE_LIVE_RESURRECTION_RISK, source, fact.objectID,
        fact.objectKind, 'staleLiveMetadataResurrectionForbidden'))
    }
    if (fact.autoConflictResolutionRisk) {
      failures.push(new CanonicalTombstoneConflictReadProjectionFailure(
        CanonicalTombstoneConflictReadProjectionFailureKind.AUTO_CONFLICT_RESOLUTION_RISK, source, fact.objectID,
        fact.objectKind, 'autoConflictResolutionForbidden'))
    }
    items.push(new CanonicalTombstoneConflictReadProjectionItem(source, fact))
  }
  return new CanonicalTombstoneConflictReadSnapshot(source, generatedAt, items, failures, items.length,
    items.length)
}

function appendManifestTcFacts(manifest: CanonicalManifest, peer: boolean,
  facts: CanonicalTombstoneConflictReadProjectionFact[]): void {
  for (const obj of manifest.objects) {
    const tombstoned = obj.metadata.isDeleted || obj.syncState === 'deleted'
    facts.push(new CanonicalTombstoneConflictReadProjectionFact(
      obj.objectID, 'recording', tombstoned ? CanonicalTombstoneState.TOMBSTONED : CanonicalTombstoneState.ACTIVE,
      tombstoned ? CanonicalTombstoneConflictDeletedDisplayState.TOMBSTONED :
        CanonicalTombstoneConflictDeletedDisplayState.ACTIVE,
      obj.metadata.deletedAt ?? null, null,
      CanonicalTombstoneConflictStatus.NONE,
      tombstoned ? (peer ? 'peerTombstone' : 'localTombstone') : 'activeOnly',
      tombstoned ? CanonicalTombstoneConflictAntiResurrectionStatus.BLOCKED :
        CanonicalTombstoneConflictAntiResurrectionStatus.NOT_TRIGGERED,
      false, tombstoned, tombstoned,
      CanonicalProductionRedaction.hashPrefix(obj.metadataHash.value)))
  }
  // Library objects and tombstones processed similarly
  for (const obj of manifest.libraryObjects) {
    const tombstoned = obj.isDeleted
    facts.push(new CanonicalTombstoneConflictReadProjectionFact(
      obj.objectID?.rawValue ?? 'unknown', obj.kind ?? 'unknown',
      tombstoned ? CanonicalTombstoneState.TOMBSTONED : CanonicalTombstoneState.ACTIVE,
      tombstoned ? CanonicalTombstoneConflictDeletedDisplayState.TOMBSTONED :
        CanonicalTombstoneConflictDeletedDisplayState.ACTIVE,
      obj.deletedAt, null, CanonicalTombstoneConflictStatus.NONE,
      tombstoned ? (peer ? 'peerLibraryTombstone' : 'localLibraryTombstone') : 'activeOnly',
      tombstoned ? CanonicalTombstoneConflictAntiResurrectionStatus.BLOCKED :
        CanonicalTombstoneConflictAntiResurrectionStatus.NOT_TRIGGERED,
      false, tombstoned, tombstoned, CanonicalProductionRedaction.hashPrefix(obj.metadataHash.value)))
  }
}

function appendApplyPlanTcFacts(plan: CanonicalApplyPlan,
  facts: CanonicalTombstoneConflictReadProjectionFact[]): void {
  const tombstones: Record<string, any> = {}
  for (const t of plan.tombstones) {
    tombstones[t.tombstoneID] = t
  }
  const conflicts: Record<string, any> = {}
  for (const c of plan.conflicts) {
    conflicts[c.conflictID] = c
  }
  for (const action of plan.actions) {
    if (action.kind === CanonicalApplyActionKind.OBJECT_TOMBSTONE_APPLY ||
      action.kind === CanonicalApplyActionKind.OBJECT_TOMBSTONE_SEND ||
      action.kind === CanonicalApplyActionKind.ARTIFACT_TOMBSTONE_APPLY) {
      const tombstone = action.tombstoneID ? tombstones[action.tombstoneID] : null
      facts.push(new CanonicalTombstoneConflictReadProjectionFact(
        action.target.objectID, 'recording', CanonicalTombstoneState.TOMBSTONED,
        CanonicalTombstoneConflictDeletedDisplayState.TOMBSTONED, tombstone?.deletedAt ?? null))
    } else if (action.kind === CanonicalApplyActionKind.CONFLICT_RECORD) {
      const conflict = action.conflictID ? conflicts[action.conflictID] : null
      facts.push(new CanonicalTombstoneConflictReadProjectionFact(
        action.target.objectID, 'recording', CanonicalTombstoneState.ACTIVE,
        CanonicalTombstoneConflictDeletedDisplayState.ACTIVE, null, conflict?.kind ?? action.reason,
        CanonicalTombstoneConflictStatus.MANUAL_REVIEW_REQUIRED,
        conflict?.kind === 'activeVsTombstone' ? 'activeVsTombstone' : 'conflict',
        conflict?.kind === 'activeVsTombstone' ?
          CanonicalTombstoneConflictAntiResurrectionStatus.BLOCKED :
          CanonicalTombstoneConflictAntiResurrectionStatus.NOT_TRIGGERED,
        false, conflict?.kind === 'activeVsTombstone', false, conflict?.localHashPrefix ?? null))
    }
  }
}

function appendLibraryPlanTcFacts(plan: CanonicalLibrarySyncPlan,
  facts: CanonicalTombstoneConflictReadProjectionFact[]): void {
  const tombstones: Record<string, any> = {}
  for (const t of plan.tombstones) {
    tombstones[t.tombstoneID] = t
  }
  const conflicts: Record<string, any> = {}
  for (const c of plan.conflicts) {
    conflicts[c.conflictID] = c
  }
  for (const action of plan.applyActions) {
    if (action.kind === CanonicalApplyActionKind.LIBRARY_TOMBSTONE_APPLY ||
      action.kind === CanonicalApplyActionKind.LIBRARY_TOMBSTONE_SEND) {
      const tombstone = action.tombstoneID ? tombstones[action.tombstoneID] : null
      facts.push(new CanonicalTombstoneConflictReadProjectionFact(
        action.target.objectID, tombstone?.objectKind ?? 'unknown', CanonicalTombstoneState.TOMBSTONED,
        CanonicalTombstoneConflictDeletedDisplayState.TOMBSTONED, tombstone?.deletedAt ?? null))
    } else if (action.kind === CanonicalApplyActionKind.CONFLICT_RECORD) {
      const conflict = action.conflictID ? conflicts[action.conflictID] : null
      facts.push(new CanonicalTombstoneConflictReadProjectionFact(
        action.target.objectID, conflict?.objectKind ?? 'unknown', CanonicalTombstoneState.ACTIVE,
        CanonicalTombstoneConflictDeletedDisplayState.ACTIVE, null, conflict?.kind ?? action.reason,
        CanonicalTombstoneConflictStatus.MANUAL_REVIEW_REQUIRED,
        conflict?.kind === 'activeVsTombstone' ? 'activeVsTombstone' : 'conflict',
        conflict?.kind === 'activeVsTombstone' ?
          CanonicalTombstoneConflictAntiResurrectionStatus.BLOCKED :
          CanonicalTombstoneConflictAntiResurrectionStatus.NOT_TRIGGERED,
        false, conflict?.kind === 'activeVsTombstone', false, conflict?.localHashPrefix ?? null))
    }
  }
}

export function tcCompareReadSides(
  legacy: CanonicalTombstoneConflictReadSnapshot | null,
  canonical: CanonicalTombstoneConflictReadSnapshot | null
): CanonicalTombstoneConflictReadSideDiffReport {
  let divergences: CanonicalTombstoneConflictReadSideDivergence[] = []
  let blockers: CanonicalTombstoneConflictReadSideBlocker[] = []

  if (!legacy) {
    return tcDiffReport(legacy, canonical, [], [CanonicalTombstoneConflictReadSideBlocker.MISSING_LEGACY_SNAPSHOT])
  }
  if (!canonical) {
    return tcDiffReport(legacy, canonical, [],
      [CanonicalTombstoneConflictReadSideBlocker.MISSING_CANONICAL_SNAPSHOT])
  }

  appendTcFailureDivergences([...legacy.failures, ...canonical.failures], divergences, blockers)

  const legacyMap: Record<string, CanonicalTombstoneConflictReadProjectionItem> = {}
  for (const item of legacy.items) {
    legacyMap[item.id] = item
  }
  const canonicalMap: Record<string, CanonicalTombstoneConflictReadProjectionItem> = {}
  for (const item of canonical.items) {
    canonicalMap[item.id] = item
  }
  const keys = [...new Set([...Object.keys(legacyMap), ...Object.keys(canonicalMap)])].sort()
  for (const id of keys) {
    const legacyItem = legacyMap[id]
    const canonicalItem = canonicalMap[id]
    if (!legacyItem && canonicalItem) {
      divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
        CanonicalTombstoneConflictReadSideDivergenceKind.MISSING_IN_LEGACY, canonicalItem.objectID,
        canonicalItem.objectKind, undefined, undefined, 'present'))
      continue
    }
    if (!canonicalItem && legacyItem) {
      divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
        CanonicalTombstoneConflictReadSideDivergenceKind.MISSING_IN_CANONICAL, legacyItem.objectID,
        legacyItem.objectKind, undefined, 'present'))
      continue
    }
    if (legacyItem && canonicalItem) {
      tcCompareItems(legacyItem, canonicalItem, divergences)
    }
  }
  if (divergences.length > 0) {
    blockers.push(CanonicalTombstoneConflictReadSideBlocker.BLOCKING_DIVERGENCE)
  }
  return tcDiffReport(legacy, canonical, divergences, blockers)
}

function tcCompareItems(
  legacy: CanonicalTombstoneConflictReadProjectionItem,
  canonical: CanonicalTombstoneConflictReadProjectionItem,
  divergences: CanonicalTombstoneConflictReadSideDivergence[]
): void {
  if (legacy.tombstoneState !== canonical.tombstoneState) {
    divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
      CanonicalTombstoneConflictReadSideDivergenceKind.TOMBSTONE_STATE_MISMATCH, legacy.objectID,
      legacy.objectKind, 'tombstoneState', legacy.tombstoneState, canonical.tombstoneState))
  }
  if (legacy.tombstoneTimestampSummary !== canonical.tombstoneTimestampSummary) {
    divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
      CanonicalTombstoneConflictReadSideDivergenceKind.TOMBSTONE_TIMESTAMP_MISMATCH, legacy.objectID,
      legacy.objectKind, 'tombstoneTimestamp', legacy.tombstoneTimestampSummary,
      canonical.tombstoneTimestampSummary))
  }
  if (legacy.conflictKind !== canonical.conflictKind) {
    divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
      CanonicalTombstoneConflictReadSideDivergenceKind.CONFLICT_RECORD_MISMATCH, legacy.objectID,
      legacy.objectKind, 'conflictKind', legacy.conflictKind, canonical.conflictKind))
  }
  if (legacy.softDeleteMarkerPresent !== canonical.softDeleteMarkerPresent) {
    divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
      CanonicalTombstoneConflictReadSideDivergenceKind.SOFT_DELETE_MARKER_MISMATCH, legacy.objectID,
      legacy.objectKind, 'softDeleteMarker', String(legacy.softDeleteMarkerPresent),
      String(canonical.softDeleteMarkerPresent)))
  }
  // Risk divergences
  if (legacy.physicalDeleteRisk || canonical.physicalDeleteRisk) {
    divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
      CanonicalTombstoneConflictReadSideDivergenceKind.PHYSICAL_DELETE_RISK, legacy.objectID,
      legacy.objectKind, 'physicalDelete', undefined, 'true', true))
  }
  if (legacy.permanentDeleteRisk || canonical.permanentDeleteRisk) {
    divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
      CanonicalTombstoneConflictReadSideDivergenceKind.PERMANENT_DELETE_RISK, legacy.objectID,
      legacy.objectKind, 'permanentDelete', undefined, 'true', true))
  }
  if (legacy.tombstoneGCRisk || canonical.tombstoneGCRisk) {
    divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
      CanonicalTombstoneConflictReadSideDivergenceKind.TOMBSTONE_GC_RISK, legacy.objectID,
      legacy.objectKind, 'tombstoneGC', undefined, 'true', true))
  }
}

function appendTcFailureDivergences(
  failures: CanonicalTombstoneConflictReadProjectionFailure[],
  divergences: CanonicalTombstoneConflictReadSideDivergence[],
  blockers: CanonicalTombstoneConflictReadSideBlocker[]
): void {
  for (const failure of failures) {
    switch (failure.kind) {
      case CanonicalTombstoneConflictReadProjectionFailureKind.SNAPSHOT_MISSING:
      case CanonicalTombstoneConflictReadProjectionFailureKind.FULL_METADATA_REJECTED:
      case CanonicalTombstoneConflictReadProjectionFailureKind.FULL_CONTENT_REJECTED:
        continue
      case CanonicalTombstoneConflictReadProjectionFailureKind.UNSUPPORTED_OBJECT_KIND:
        blockers.push(CanonicalTombstoneConflictReadSideBlocker.UNSUPPORTED_OBJECT_KIND)
        break
      case CanonicalTombstoneConflictReadProjectionFailureKind.PATH_LEAK_RISK:
        blockers.push(CanonicalTombstoneConflictReadSideBlocker.PATH_LEAK_RISK)
        divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
          CanonicalTombstoneConflictReadSideDivergenceKind.PATH_LEAK_RISK, failure.objectID ?? 'run',
          failure.objectKind, 'projectionFailure',
          failure.source === CanonicalTombstoneConflictReadProjectionSource.LEGACY ? failure.reason : undefined,
          failure.source === CanonicalTombstoneConflictReadProjectionSource.CANONICAL ? failure.reason : undefined,
          true))
        break
      case CanonicalTombstoneConflictReadProjectionFailureKind.PHYSICAL_DELETE_RISK:
        blockers.push(CanonicalTombstoneConflictReadSideBlocker.PHYSICAL_DELETE_RISK)
        divergences.push(new CanonicalTombstoneConflictReadSideDivergence(
          CanonicalTombstoneConflictReadSideDivergenceKind.PHYSICAL_DELETE_RISK, failure.objectID ?? 'run',
          failure.objectKind, 'projectionFailure', undefined,
          failure.source === CanonicalTombstoneConflictReadProjectionSource.CANONICAL ? failure.reason : undefined,
          true))
        break
      case CanonicalTombstoneConflictReadProjectionFailureKind.PERMANENT_DELETE_RISK:
        blockers.push(CanonicalTombstoneConflictReadSideBlocker.PERMANENT_DELETE_RISK)
        break
      case CanonicalTombstoneConflictReadProjectionFailureKind.TOMBSTONE_GC_RISK:
        blockers.push(CanonicalTombstoneConflictReadSideBlocker.TOMBSTONE_GC_RISK)
        break
      case CanonicalTombstoneConflictReadProjectionFailureKind.STALE_LIVE_RESURRECTION_RISK:
        blockers.push(CanonicalTombstoneConflictReadSideBlocker.STALE_LIVE_RESURRECTION_RISK)
        break
      case CanonicalTombstoneConflictReadProjectionFailureKind.AUTO_CONFLICT_RESOLUTION_RISK:
        blockers.push(CanonicalTombstoneConflictReadSideBlocker.AUTO_CONFLICT_RESOLUTION_RISK)
        break
    }
  }
}

function tcDiffReport(
  legacy: CanonicalTombstoneConflictReadSnapshot | null,
  canonical: CanonicalTombstoneConflictReadSnapshot | null,
  divergences: CanonicalTombstoneConflictReadSideDivergence[],
  blockers: CanonicalTombstoneConflictReadSideBlocker[]
): CanonicalTombstoneConflictReadSideDiffReport {
  const divMap: Record<string, CanonicalTombstoneConflictReadSideDivergence> = {}
  for (const d of divergences) {
    if (!divMap[d.id]) {
      divMap[d.id] = d
    }
  }
  const uniqueDiv = Object.values(divMap).sort((a, b) => a.id.localeCompare(b.id))
  let uniqueBlockers = [...new Set(blockers)].sort()
  if (uniqueDiv.some((d: CanonicalTombstoneConflictReadSideDivergence) =>
    d.kind === CanonicalTombstoneConflictReadSideDivergenceKind.PHYSICAL_DELETE_RISK)) {
    uniqueBlockers.push(CanonicalTombstoneConflictReadSideBlocker.PHYSICAL_DELETE_RISK)
  }
  if (uniqueDiv.some((d: CanonicalTombstoneConflictReadSideDivergence) =>
    d.kind === CanonicalTombstoneConflictReadSideDivergenceKind.PERMANENT_DELETE_RISK)) {
    uniqueBlockers.push(CanonicalTombstoneConflictReadSideBlocker.PERMANENT_DELETE_RISK)
  }
  uniqueBlockers = [...new Set(uniqueBlockers)].sort()
  const equivalent = uniqueDiv.length === 0 && uniqueBlockers.length === 0
  const objectCount = canonical?.items.length ?? legacy?.items.length ?? 0
  const tombstoneCount = (canonical ?? legacy)?.items.filter(
    (i: CanonicalTombstoneConflictReadProjectionItem) =>
      i.tombstoneState === CanonicalTombstoneState.TOMBSTONED).length ?? 0
  const conflictCount = (canonical ?? legacy)?.items.filter(
    (i: CanonicalTombstoneConflictReadProjectionItem) =>
      i.conflictStatus !== CanonicalTombstoneConflictStatus.NONE).length ?? 0
  const equivalence = new CanonicalTombstoneConflictReadSideEquivalence(equivalent, objectCount, tombstoneCount,
    conflictCount,
    'equivalent=' + equivalent + ',objects=' + objectCount + ',tombstones=' + tombstoneCount + ',conflicts=' +
    conflictCount)
  return new CanonicalTombstoneConflictReadSideDiffReport(equivalence, uniqueDiv, uniqueBlockers,
    legacy?.diagnosticsSummary ?? 'legacySnapshot=missing',
    canonical?.diagnosticsSummary ?? 'canonicalSnapshot=missing',
    'domain=tombstoneConflict,equivalent=' + equivalent + ',divergences=' + uniqueDiv.length + ',fatal=' +
    uniqueDiv.filter((d: CanonicalTombstoneConflictReadSideDivergence) => d.fatal).length + ',blockers=' +
    uniqueBlockers.join('+'))
}

export class CanonicalTombstoneConflictReadSideEvaluator {
  constructor() {}

  evaluate(
    configuration: CanonicalTombstoneConflictReadSideConfiguration,
    legacySnapshot: CanonicalTombstoneConflictReadSnapshot | null,
    canonicalSnapshot: CanonicalTombstoneConflictReadSnapshot | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole, syncRunID: string | null
  ): CanonicalTombstoneConflictReadSideEvaluationResult {
    if (!configuration.isEnabled ||
      configuration.mode !== CanonicalTombstoneConflictReadSideMode.PARALLEL_ONLY) {
      return CanonicalTombstoneConflictReadSideEvaluationResult.disabled(configuration, syncRunID, trigger, nodeRole)
    }
    const diff = tcCompareReadSides(legacySnapshot, canonicalSnapshot)
    return CanonicalTombstoneConflictReadSideEvaluationResult.evaluated(configuration, diff, syncRunID, trigger,
      nodeRole)
  }
}

export class CanonicalTombstoneConflictReadSideEvaluationResult {
  configuration: CanonicalTombstoneConflictReadSideConfiguration
  diffReport: CanonicalTombstoneConflictReadSideDiffReport | null
  diagnostics: CanonicalTombstoneConflictReadSideDiagnostic[]
  storeMutated: boolean
  uiMutated: boolean
  uploadJobCreated: boolean
  deleteAttempted: boolean
  restoreAttempted: boolean
  tombstoneCleared: boolean
  conflictResolved: boolean

  constructor(
    configuration: CanonicalTombstoneConflictReadSideConfiguration,
    diffReport: CanonicalTombstoneConflictReadSideDiffReport | null,
    diagnostics: CanonicalTombstoneConflictReadSideDiagnostic[], storeMutated: boolean = false,
    uiMutated: boolean = false, uploadJobCreated: boolean = false, deleteAttempted: boolean = false,
    restoreAttempted: boolean = false, tombstoneCleared: boolean = false, conflictResolved: boolean = false
  ) {
    this.configuration = configuration
    this.diffReport = diffReport
    this.diagnostics = diagnostics
    this.storeMutated = storeMutated
    this.uiMutated = uiMutated
    this.uploadJobCreated = uploadJobCreated
    this.deleteAttempted = deleteAttempted
    this.restoreAttempted = restoreAttempted
    this.tombstoneCleared = tombstoneCleared
    this.conflictResolved = conflictResolved
  }

  get noMutationAsserted(): boolean {
    return !this.storeMutated && !this.uiMutated && !this.uploadJobCreated && !this.deleteAttempted &&
      !this.restoreAttempted && !this.tombstoneCleared && !this.conflictResolved
  }

  static disabled(
    configuration: CanonicalTombstoneConflictReadSideConfiguration, syncRunID: string | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole
  ): CanonicalTombstoneConflictReadSideEvaluationResult {
    return new CanonicalTombstoneConflictReadSideEvaluationResult(configuration, null, [
      new CanonicalTombstoneConflictReadSideDiagnostic(
        CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_RUNTIME_SWITCH_DENIED,
        syncRunID, trigger, nodeRole, null, null, 'disabled', 'tombstoneConflictReadSideDefaultOff')
    ], false, false, false, false, false, false, false)
  }

  static evaluated(
    configuration: CanonicalTombstoneConflictReadSideConfiguration,
    diffReport: CanonicalTombstoneConflictReadSideDiffReport, syncRunID: string | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole
  ): CanonicalTombstoneConflictReadSideEvaluationResult {
    let diagnostics: CanonicalTombstoneConflictReadSideDiagnostic[] = [
      new CanonicalTombstoneConflictReadSideDiagnostic(
        CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_READ_SIDE_PARALLEL_STARTED,
        syncRunID, trigger, nodeRole, null, null, 'started', 'metadataOnlyNoMutation'),
      new CanonicalTombstoneConflictReadSideDiagnostic(
        diffReport.equivalent ?
          CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_READ_SIDE_EQUIVALENT :
          CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_READ_SIDE_DIVERGENT,
        syncRunID, trigger, nodeRole, null, null, diffReport.equivalent ? 'equivalent' : 'divergent',
        diffReport.diagnosticsSummary),
      new CanonicalTombstoneConflictReadSideDiagnostic(
        CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_NO_MUTATION_ASSERTED,
        syncRunID, trigger, nodeRole, null, null, 'true', 'noDeleteNoRestoreNoStoreNoUI')
    ]
    if (diffReport.blockers.includes(CanonicalTombstoneConflictReadSideBlocker.PHYSICAL_DELETE_RISK)) {
      diagnostics.push(new CanonicalTombstoneConflictReadSideDiagnostic(
        CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_PHYSICAL_DELETE_RISK_BLOCKED,
        syncRunID, trigger, nodeRole, null, null, 'blocked'))
    }
    if (diffReport.blockers.includes(CanonicalTombstoneConflictReadSideBlocker.PERMANENT_DELETE_RISK)) {
      diagnostics.push(new CanonicalTombstoneConflictReadSideDiagnostic(
        CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_PERMANENT_DELETE_RISK_BLOCKED,
        syncRunID, trigger, nodeRole, null, null, 'blocked'))
    }
    if (diffReport.blockers.includes(CanonicalTombstoneConflictReadSideBlocker.TOMBSTONE_GC_RISK)) {
      diagnostics.push(new CanonicalTombstoneConflictReadSideDiagnostic(
        CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_GC_RISK_BLOCKED,
        syncRunID, trigger, nodeRole, null, null, 'blocked'))
    }
    return new CanonicalTombstoneConflictReadSideEvaluationResult(configuration, diffReport, diagnostics, false,
      false, false, false, false, false, false)
  }
}

export class CanonicalTombstoneConflictObservationEvent {
  kind: CanonicalTombstoneConflictObservationEventKind
  count: number

  constructor(kind: CanonicalTombstoneConflictObservationEventKind, count: number = 1) {
    this.kind = kind
    this.count = Math.max(0, count)
  }
}

export class CanonicalTombstoneConflictObservationPolicy {
  isEnabled: boolean
  requireRuntimeSwitchFalse: boolean
  requireLegacyFallbackAvailable: boolean

  constructor(isEnabled: boolean = false, requireRuntimeSwitchFalse: boolean = true,
    requireLegacyFallbackAvailable: boolean = true) {
    this.isEnabled = isEnabled
    this.requireRuntimeSwitchFalse = requireRuntimeSwitchFalse
    this.requireLegacyFallbackAvailable = requireLegacyFallbackAvailable
  }

  static get disabled(): CanonicalTombstoneConflictObservationPolicy {
    return new CanonicalTombstoneConflictObservationPolicy()
  }

  static get explicitInternal(): CanonicalTombstoneConflictObservationPolicy {
    return new CanonicalTombstoneConflictObservationPolicy(true)
  }
}

export class CanonicalTombstoneConflictObservationSummary {
  noCommitEventCount: number
  commitEventCount: number
  rollbackEventCount: number
  legacyFallbackCount: number
  readSideEquivalentCount: number
  readSideDivergenceCount: number
  physicalDeleteRiskCount: number
  permanentDeleteRiskCount: number
  tombstoneGCRiskCount: number
  staleLiveResurrectionRiskCount: number
  autoConflictResolutionRiskCount: number
  unsupportedObjectCount: number
  runtimeSwitch: boolean
  legacyFallbackAvailable: boolean
  observationComplete: boolean

  constructor(
    noCommitEventCount: number = 0, commitEventCount: number = 0, rollbackEventCount: number = 0,
    legacyFallbackCount: number = 0, readSideEquivalentCount: number = 0,
    readSideDivergenceCount: number = 0, physicalDeleteRiskCount: number = 0,
    permanentDeleteRiskCount: number = 0, tombstoneGCRiskCount: number = 0,
    staleLiveResurrectionRiskCount: number = 0, autoConflictResolutionRiskCount: number = 0,
    unsupportedObjectCount: number = 0, runtimeSwitch: boolean = false,
    legacyFallbackAvailable: boolean = true, observationComplete: boolean = false
  ) {
    this.noCommitEventCount = noCommitEventCount
    this.commitEventCount = commitEventCount
    this.rollbackEventCount = rollbackEventCount
    this.legacyFallbackCount = legacyFallbackCount
    this.readSideEquivalentCount = readSideEquivalentCount
    this.readSideDivergenceCount = readSideDivergenceCount
    this.physicalDeleteRiskCount = physicalDeleteRiskCount
    this.permanentDeleteRiskCount = permanentDeleteRiskCount
    this.tombstoneGCRiskCount = tombstoneGCRiskCount
    this.staleLiveResurrectionRiskCount = staleLiveResurrectionRiskCount
    this.autoConflictResolutionRiskCount = autoConflictResolutionRiskCount
    this.unsupportedObjectCount = unsupportedObjectCount
    this.runtimeSwitch = runtimeSwitch
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.observationComplete = observationComplete
  }
}

export class CanonicalTombstoneConflictObservationWindow {
  policy: CanonicalTombstoneConflictObservationPolicy
  events: CanonicalTombstoneConflictObservationEvent[]
  runtimeSwitch: boolean
  legacyFallbackAvailable: boolean

  constructor(
    policy: CanonicalTombstoneConflictObservationPolicy = CanonicalTombstoneConflictObservationPolicy.disabled,
    events: CanonicalTombstoneConflictObservationEvent[] = [], runtimeSwitch: boolean = false,
    legacyFallbackAvailable: boolean = true
  ) {
    this.policy = policy
    this.events = events
    this.runtimeSwitch = runtimeSwitch
    this.legacyFallbackAvailable = legacyFallbackAvailable
  }

  recording(event: CanonicalTombstoneConflictObservationEvent): CanonicalTombstoneConflictObservationWindow {
    return new CanonicalTombstoneConflictObservationWindow(this.policy, [...this.events, event],
      this.runtimeSwitch, this.legacyFallbackAvailable)
  }

  get summary(): CanonicalTombstoneConflictObservationSummary {
    const grouped: Record<string, number> = {}
    for (const ev of this.events) {
      grouped[ev.kind] = (grouped[ev.kind] ?? 0) + ev.count
    }
    const complete = this.policy.isEnabled && (grouped[
      CanonicalTombstoneConflictObservationEventKind.READ_SIDE_EQUIVALENT] ?? 0) > 0 &&
      (grouped[CanonicalTombstoneConflictObservationEventKind.READ_SIDE_DIVERGENT] ?? 0) === 0 &&
      !this.runtimeSwitch && this.legacyFallbackAvailable
    return new CanonicalTombstoneConflictObservationSummary(
      grouped[CanonicalTombstoneConflictObservationEventKind.NO_COMMIT] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.COMMIT] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.ROLLBACK] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.LEGACY_FALLBACK] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.READ_SIDE_EQUIVALENT] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.READ_SIDE_DIVERGENT] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.PHYSICAL_DELETE_RISK] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.PERMANENT_DELETE_RISK] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.TOMBSTONE_GC_RISK] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.STALE_LIVE_RESURRECTION_RISK] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.AUTO_CONFLICT_RESOLUTION_RISK] ?? 0,
      grouped[CanonicalTombstoneConflictObservationEventKind.UNSUPPORTED_OBJECT] ?? 0,
      this.runtimeSwitch, this.legacyFallbackAvailable, complete)
  }
}

export class CanonicalTombstoneConflictObservationGate {
  state: CanonicalTombstoneConflictObservationGateState
  summary: CanonicalTombstoneConflictObservationSummary
  blockers: CanonicalTombstoneConflictReadSideBlocker[]
  diagnostics: CanonicalTombstoneConflictReadSideDiagnostic[]

  constructor(
    state: CanonicalTombstoneConflictObservationGateState,
    summary: CanonicalTombstoneConflictObservationSummary,
    blockers: CanonicalTombstoneConflictReadSideBlocker[],
    diagnostics: CanonicalTombstoneConflictReadSideDiagnostic[]
  ) {
    this.state = state
    this.summary = summary
    this.blockers = blockers
    this.diagnostics = diagnostics
  }

  get observationComplete(): boolean {
    return this.summary.observationComplete && this.state === CanonicalTombstoneConflictObservationGateState.COMPLETE
  }

  static evaluate(
    window: CanonicalTombstoneConflictObservationWindow,
    trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC,
    nodeRole: CanonicalProductionExecutionDomainRole = CanonicalProductionExecutionDomainRole.TEST_HARNESS,
    syncRunID: string | null = null
  ): CanonicalTombstoneConflictObservationGate {
    const summary = window.summary
    let blockers: CanonicalTombstoneConflictReadSideBlocker[] = []
    if (summary.physicalDeleteRiskCount > 0) {
      blockers.push(CanonicalTombstoneConflictReadSideBlocker.PHYSICAL_DELETE_RISK)
    }
    if (summary.permanentDeleteRiskCount > 0) {
      blockers.push(CanonicalTombstoneConflictReadSideBlocker.PERMANENT_DELETE_RISK)
    }
    if (summary.tombstoneGCRiskCount > 0) {
      blockers.push(CanonicalTombstoneConflictReadSideBlocker.TOMBSTONE_GC_RISK)
    }
    if (summary.staleLiveResurrectionRiskCount > 0) {
      blockers.push(CanonicalTombstoneConflictReadSideBlocker.STALE_LIVE_RESURRECTION_RISK)
    }
    if (summary.autoConflictResolutionRiskCount > 0) {
      blockers.push(CanonicalTombstoneConflictReadSideBlocker.AUTO_CONFLICT_RESOLUTION_RISK)
    }
    if (summary.readSideDivergenceCount > 0) {
      blockers.push(CanonicalTombstoneConflictReadSideBlocker.BLOCKING_DIVERGENCE)
    }
    if (summary.unsupportedObjectCount > 0) {
      blockers.push(CanonicalTombstoneConflictReadSideBlocker.UNSUPPORTED_OBJECT_KIND)
    }
    const unique = [...new Set(blockers)].sort()
    let state: CanonicalTombstoneConflictObservationGateState
    if (!window.policy.isEnabled) {
      state = CanonicalTombstoneConflictObservationGateState.DISABLED
    } else if (unique.length > 0 || summary.runtimeSwitch || !summary.legacyFallbackAvailable) {
      state = CanonicalTombstoneConflictObservationGateState.BLOCKED
    } else if (summary.observationComplete) {
      state = CanonicalTombstoneConflictObservationGateState.COMPLETE
    } else {
      state = CanonicalTombstoneConflictObservationGateState.INCOMPLETE
    }
    return new CanonicalTombstoneConflictObservationGate(state, summary, unique, [
      new CanonicalTombstoneConflictReadSideDiagnostic(
        CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_OBSERVATION_WINDOW_STARTED,
        syncRunID, trigger, nodeRole, null, null, window.policy.isEnabled ? 'enabled' : 'disabled'),
      new CanonicalTombstoneConflictReadSideDiagnostic(
        state === CanonicalTombstoneConflictObservationGateState.COMPLETE ?
          CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_READ_SIDE_EQUIVALENT :
          CanonicalTombstoneConflictReadSideDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_OBSERVATION_WINDOW_BLOCKED,
        syncRunID, trigger, nodeRole, null, null, state, unique.join('+'))
    ])
  }
}
