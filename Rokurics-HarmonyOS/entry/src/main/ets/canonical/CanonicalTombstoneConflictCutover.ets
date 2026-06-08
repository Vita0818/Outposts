/**
 * CanonicalTombstoneConflictCutover — tombstone & conflict cutover domain.
 * Ported from Rokurics-Apple (CanonicalTombstoneConflictCutover.swift).
 * Evaluates tombstone and conflict candidates, guards, and commit execution.
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalNode, CanonicalRecordingObject, CanonicalArtifact,
  CanonicalArtifactKind, CanonicalManifest
} from './CanonicalCore'
import {
  CanonicalCutoverMode, CanonicalCutoverToken, CanonicalCutoverAppSeamMode,
  CanonicalSyncPlanTrigger
} from './CanonicalAudioUploadCutover'
import {
  CanonicalApplyAction, CanonicalApplyActionKind, CanonicalApplyTarget, CanonicalApplyPlan,
  CanonicalRollbackPlan, CanonicalRollbackResult, CanonicalProductionDomain,
  CanonicalProductionExecutionDomainRole, CanonicalProductionSideEffect
} from './CanonicalGeneratedArtifactCutover'

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

export class CanonicalTombstone {
  tombstoneID: string
  state: string
  deletedAt: CanonicalTimestamp | null
  objectID: string

  constructor(tombstoneID: string, state: string, deletedAt: CanonicalTimestamp | null, objectID: string) {
    this.tombstoneID = tombstoneID
    this.state = state
    this.deletedAt = deletedAt
    this.objectID = objectID
  }
}

export class CanonicalLibraryTombstone {
  tombstoneID: string
  deletedAt: CanonicalTimestamp | null
  objectKind: string
  objectID: any

  constructor(tombstoneID: string, deletedAt: CanonicalTimestamp | null, objectKind: string, objectID: any) {
    this.tombstoneID = tombstoneID
    this.deletedAt = deletedAt
    this.objectKind = objectKind
    this.objectID = objectID
  }
}

export class CanonicalConflictRecord {
  conflictID: string
  kind: string
  resolutionPolicy: string
  localHashPrefix: string | null

  constructor(conflictID: string, kind: string, resolutionPolicy: string, localHashPrefix: string | null) {
    this.conflictID = conflictID
    this.kind = kind
    this.resolutionPolicy = resolutionPolicy
    this.localHashPrefix = localHashPrefix
  }
}

export class CanonicalLibraryConflict {
  conflictID: string
  kind: string

  constructor(conflictID: string, kind: string) {
    this.conflictID = conflictID
    this.kind = kind
  }
}

export class CanonicalLibraryObject {
  objectID: any
  kind: any
  isDeleted: boolean
  deletedAt: CanonicalTimestamp | null
  metadataHash: CanonicalHash

  constructor(objectID: any, kind: any, isDeleted: boolean = false, deletedAt: CanonicalTimestamp | null = null,
    metadataHash: CanonicalHash = new CanonicalHash('0')) {
    this.objectID = objectID
    this.kind = kind
    this.isDeleted = isDeleted
    this.deletedAt = deletedAt
    this.metadataHash = metadataHash
  }
}

export class CanonicalLibrarySyncPlan {
  applyActions: CanonicalApplyAction[]
  conflicts: CanonicalConflictRecord[]
  tombstones: CanonicalLibraryTombstone[]

  constructor(
    applyActions: CanonicalApplyAction[] = [], conflicts: CanonicalConflictRecord[] = [],
    tombstones: CanonicalLibraryTombstone[] = []
  ) {
    this.applyActions = applyActions
    this.conflicts = conflicts
    this.tombstones = tombstones
  }
}

export class CanonicalNoCommitStagingEvidence {
  bytesWritten: number
  logicalPathToken: string

  constructor(bytesWritten: number, logicalPathToken: string) {
    this.bytesWritten = bytesWritten
    this.logicalPathToken = logicalPathToken
  }
}

export class CanonicalNoCommitCleanupEvidence {
  cleaned: boolean

  constructor(cleaned: boolean) {
    this.cleaned = cleaned
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS
// ═══════════════════════════════════════════════════════════════════════════════

export enum CanonicalTombstoneConflictDomain {
  OBJECT_TOMBSTONE = 'objectTombstone',
  LIBRARY_TOMBSTONE = 'libraryTombstone',
  GENERATED_ARTIFACT_TOMBSTONE_MARKER = 'generatedArtifactTombstoneMarker',
  ACTIVE_VS_TOMBSTONE_CONFLICT = 'activeVsTombstoneConflict',
  METADATA_CONFLICT_RECORD = 'metadataConflictRecord',
  ARTIFACT_CONFLICT_RECORD = 'artifactConflictRecord'
}

export function domainProductionDomain(domain: CanonicalTombstoneConflictDomain): CanonicalProductionDomain {
  switch (domain) {
    case CanonicalTombstoneConflictDomain.OBJECT_TOMBSTONE:
    case CanonicalTombstoneConflictDomain.LIBRARY_TOMBSTONE:
    case CanonicalTombstoneConflictDomain.GENERATED_ARTIFACT_TOMBSTONE_MARKER:
      return CanonicalProductionDomain.TOMBSTONES
    default:
      return CanonicalProductionDomain.CONFLICTS
  }
}

export function domainRequiresConflictLedger(domain: CanonicalTombstoneConflictDomain): boolean {
  switch (domain) {
    case CanonicalTombstoneConflictDomain.ACTIVE_VS_TOMBSTONE_CONFLICT:
    case CanonicalTombstoneConflictDomain.METADATA_CONFLICT_RECORD:
    case CanonicalTombstoneConflictDomain.ARTIFACT_CONFLICT_RECORD:
      return true
    default:
      return false
  }
}

export enum CanonicalTombstoneConflictActionKind {
  OBJECT_TOMBSTONE_APPLY = 'objectTombstoneApply',
  OBJECT_TOMBSTONE_SEND = 'objectTombstoneSend',
  LIBRARY_TOMBSTONE_APPLY = 'libraryTombstoneApply',
  LIBRARY_TOMBSTONE_SEND = 'libraryTombstoneSend',
  GENERATED_ARTIFACT_TOMBSTONE_MARK_UNSUPPORTED = 'generatedArtifactTombstoneMarkUnsupported',
  CONFLICT_RECORD = 'conflictRecord',
  RESURRECTION_BLOCKED = 'resurrectionBlocked',
  UNSUPPORTED = 'unsupported'
}

export function actionKindIsTombstoneMarkerWrite(kind: CanonicalTombstoneConflictActionKind): boolean {
  return kind === CanonicalTombstoneConflictActionKind.OBJECT_TOMBSTONE_APPLY ||
    kind === CanonicalTombstoneConflictActionKind.OBJECT_TOMBSTONE_SEND ||
    kind === CanonicalTombstoneConflictActionKind.LIBRARY_TOMBSTONE_APPLY ||
    kind === CanonicalTombstoneConflictActionKind.LIBRARY_TOMBSTONE_SEND
}

export function actionKindIsConflictLedgerWrite(kind: CanonicalTombstoneConflictActionKind): boolean {
  return kind === CanonicalTombstoneConflictActionKind.CONFLICT_RECORD ||
    kind === CanonicalTombstoneConflictActionKind.RESURRECTION_BLOCKED
}

export function actionKindIsExecutable(kind: CanonicalTombstoneConflictActionKind): boolean {
  return actionKindIsTombstoneMarkerWrite(kind) || actionKindIsConflictLedgerWrite(kind)
}

export enum CanonicalTombstoneConflictFailure {
  DISABLED = 'disabled',
  UNSUPPORTED_MODE = 'unsupportedMode',
  UNSUPPORTED_DOMAIN = 'unsupportedDomain',
  UNSUPPORTED_ACTION = 'unsupportedAction',
  MISSING_TOKEN = 'missingToken',
  MISSING_OWNER_APPROVAL = 'missingOwnerApproval',
  MISSING_ROLLBACK = 'missingRollback',
  MISSING_NO_COMMIT_EVIDENCE = 'missingNoCommitEvidence',
  MISSING_DRY_RUN_EQUIVALENCE = 'missingDryRunEquivalence',
  MISSING_EXECUTION_SHADOW_EVIDENCE = 'missingExecutionShadowEvidence',
  MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE = 'missingRealDataShadowCopyEvidence',
  BLOCKING_DIVERGENCE = 'blockingDivergence',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  MISSING_METADATA_ROUTE_EVIDENCE = 'missingMetadataRouteEvidence',
  PRODUCTION_PORT_UNAVAILABLE = 'productionPortUnavailable',
  APPLY_PORT_DRY_RUN_ONLY = 'applyPortDryRunOnly',
  ROOT_BOUND_WRITE_UNAVAILABLE = 'rootBoundWriteUnavailable',
  ATOMIC_REPLACE_UNAVAILABLE = 'atomicReplaceUnavailable',
  ROLLBACK_CHECKPOINT_UNAVAILABLE = 'rollbackCheckpointUnavailable',
  ROLLBACK_VERIFICATION_MISSING = 'rollbackVerificationMissing',
  PRODUCTION_ROOT_ENABLED_BY_DEFAULT = 'productionRootEnabledByDefault',
  TEST_ROOT_MISSING = 'testRootMissing',
  SOFT_DELETE_MARKER_UNSUPPORTED = 'softDeleteMarkerUnsupported',
  SOFT_TOMBSTONE_STORE_UNSUPPORTED = 'softTombstoneStoreUnsupported',
  CONFLICT_LEDGER_UNSUPPORTED = 'conflictLedgerUnsupported',
  OBJECT_ID_MISMATCH = 'objectIDMismatch',
  TOMBSTONE_STATE_MISMATCH = 'tombstoneStateMismatch',
  MISSING_TOMBSTONE_TIMESTAMP = 'missingTombstoneTimestamp',
  MISSING_TOMBSTONE_WINS_POLICY = 'missingTombstoneWinsPolicy',
  MISSING_ROLLBACK_EVIDENCE = 'missingRollbackEvidence',
  TOMBSTONE_TIMESTAMP_INVALID = 'tombstoneTimestampInvalid',
  TOMBSTONE_POLICY_MISSING = 'tombstonePolicyMissing',
  ROLLBACK_EVIDENCE_MISSING = 'rollbackEvidenceMissing',
  PRECONDITION_MISMATCH = 'preconditionMismatch',
  POSTCONDITION_MISMATCH = 'postconditionMismatch',
  APPLY_FAILURE_BEFORE_COMMIT = 'applyFailureBeforeCommit',
  APPLY_FAILURE_AFTER_PARTIAL_COMMIT = 'applyFailureAfterPartialCommit',
  ROLLBACK_FAILURE = 'rollbackFailure',
  RESURRECTION_RISK_DETECTED = 'resurrectionRiskDetected',
  PHYSICAL_DELETE_ATTEMPTED = 'physicalDeleteAttempted',
  PERMANENT_DELETE_ATTEMPTED = 'permanentDeleteAttempted',
  TOMBSTONE_GC_ATTEMPTED = 'tombstoneGCAttempted',
  UNSUPPORTED_RESTORE = 'unsupportedRestore',
  CONFLICT_POLICY_AMBIGUOUS = 'conflictPolicyAmbiguous',
  GENERATED_ARTIFACT_TOMBSTONE_UNSUPPORTED = 'generatedArtifactTombstoneUnsupported',
  MISSING_INTERNAL_CANARY_CONFIGURATION = 'missingInternalCanaryConfiguration',
  CANARY_BUDGET_ABOVE_ONE_DENIED = 'canaryBudgetAboveOneDenied',
  MISSING_CANARY_STAGE_EVIDENCE = 'missingCanaryStageEvidence',
  CANARY_STAGE_BLOCKED = 'canaryStageBlocked',
  CANARY_STAGE_ORDER_VIOLATION = 'canaryStageOrderViolation',
  PREVIOUS_STAGE_FAILURE = 'previousStageFailure',
  PREVIOUS_STAGE_ROLLBACK_FAILURE = 'previousStageRollbackFailure'
}

export enum CanonicalTombstoneConflictApplyPortMode {
  DISABLED = 'disabled',
  DRY_RUN = 'dryRun',
  FAKE_IN_MEMORY = 'fakeInMemory',
  TEST_ROOT_BOUND = 'testRootBound',
  PRODUCTION_ROOT_DISABLED = 'productionRootDisabled',
  PRODUCTION_ROOT_BOUND = 'productionRootBound'
}

export function tcApplyPortIsNonDryRunRootBound(mode: CanonicalTombstoneConflictApplyPortMode): boolean {
  return mode === CanonicalTombstoneConflictApplyPortMode.TEST_ROOT_BOUND ||
    mode === CanonicalTombstoneConflictApplyPortMode.PRODUCTION_ROOT_BOUND
}

export enum CanonicalTombstoneConflictCutoverDiagnosticKind {
  CANONICAL_TOMBSTONE_CUTOVER_GATE_EVALUATED = 'canonicalTombstoneCutoverGateEvaluated',
  CANONICAL_TOMBSTONE_CUTOVER_GATE_BLOCKED = 'canonicalTombstoneCutoverGateBlocked',
  CANONICAL_TOMBSTONE_NO_COMMIT_STARTED = 'canonicalTombstoneNoCommitStarted',
  CANONICAL_TOMBSTONE_NO_COMMIT_COMPLETED = 'canonicalTombstoneNoCommitCompleted',
  CANONICAL_TOMBSTONE_COMMIT_STARTED = 'canonicalTombstoneCommitStarted',
  CANONICAL_TOMBSTONE_COMMIT_COMPLETED = 'canonicalTombstoneCommitCompleted',
  CANONICAL_TOMBSTONE_COMMIT_FAILED = 'canonicalTombstoneCommitFailed',
  CANONICAL_TOMBSTONE_ROLLBACK_STARTED = 'canonicalTombstoneRollbackStarted',
  CANONICAL_TOMBSTONE_ROLLBACK_COMPLETED = 'canonicalTombstoneRollbackCompleted',
  CANONICAL_TOMBSTONE_ROLLBACK_FAILED = 'canonicalTombstoneRollbackFailed',
  CANONICAL_TOMBSTONE_LEGACY_FALLBACK_USED = 'canonicalTombstoneLegacyFallbackUsed',
  CANONICAL_TOMBSTONE_PHYSICAL_DELETE_BLOCKED = 'canonicalTombstonePhysicalDeleteBlocked',
  CANONICAL_TOMBSTONE_PERMANENT_DELETE_BLOCKED = 'canonicalTombstonePermanentDeleteBlocked',
  CANONICAL_TOMBSTONE_GC_BLOCKED = 'canonicalTombstoneGCBlocked',
  CANONICAL_TOMBSTONE_RESURRECTION_BLOCKED = 'canonicalTombstoneResurrectionBlocked',
  CANONICAL_TOMBSTONE_RESURRECTION_RISK_DETECTED = 'canonicalTombstoneResurrectionRiskDetected',
  CANONICAL_CONFLICT_RECORD_COMMITTED = 'canonicalConflictRecordCommitted',
  CANONICAL_CONFLICT_POLICY_AMBIGUOUS_BLOCKED = 'canonicalConflictPolicyAmbiguousBlocked',
  CANONICAL_TOMBSTONE_DUPLICATE_LEGACY_SUPPRESSED = 'canonicalTombstoneDuplicateLegacySuppressed',
  CANONICAL_TOMBSTONE_CANARY_STARTED = 'canonicalTombstoneCanaryStarted',
  CANONICAL_TOMBSTONE_CANARY_COMPLETED = 'canonicalTombstoneCanaryCompleted'
}

export enum CanonicalTombstoneConflictCanaryStage {
  DISABLED = 'disabled',
  N1 = 'n1',
  N3 = 'n3',
  N10 = 'n10',
  ALL_ELIGIBLE = 'allEligible'
}

export function tcCanaryStageIsExecutable(stage: CanonicalTombstoneConflictCanaryStage): boolean {
  return stage !== CanonicalTombstoneConflictCanaryStage.DISABLED
}

export function tcCanaryStagePrev(stage: CanonicalTombstoneConflictCanaryStage):
  CanonicalTombstoneConflictCanaryStage | null {
  switch (stage) {
    case CanonicalTombstoneConflictCanaryStage.DISABLED: return null
    case CanonicalTombstoneConflictCanaryStage.N1: return CanonicalTombstoneConflictCanaryStage.DISABLED
    case CanonicalTombstoneConflictCanaryStage.N3: return CanonicalTombstoneConflictCanaryStage.N1
    case CanonicalTombstoneConflictCanaryStage.N10: return CanonicalTombstoneConflictCanaryStage.N3
    case CanonicalTombstoneConflictCanaryStage.ALL_ELIGIBLE:
      return CanonicalTombstoneConflictCanaryStage.N10
  }
}

export function tcCanaryStageBudget(stage: CanonicalTombstoneConflictCanaryStage): number {
  switch (stage) {
    case CanonicalTombstoneConflictCanaryStage.DISABLED: return 0
    case CanonicalTombstoneConflictCanaryStage.N1: return 1
    case CanonicalTombstoneConflictCanaryStage.N3: return 3
    case CanonicalTombstoneConflictCanaryStage.N10: return 10
    case CanonicalTombstoneConflictCanaryStage.ALL_ELIGIBLE: return Number.MAX_SAFE_INTEGER
  }
}

export enum CanonicalTombstoneState {
  ACTIVE = 'active',
  TOMBSTONED = 'tombstoned'
}

export enum CanonicalTombstoneConflictStageEvidenceStatus {
  MISSING = 'missing',
  INCOMPLETE = 'incomplete',
  PASSED = 'passed',
  FAILED = 'failed',
  BLOCKED = 'blocked'
}

// ═══════════════════════════════════════════════════════════════════════════════
// CLASSES
// ═══════════════════════════════════════════════════════════════════════════════

export class CanonicalTombstoneConflictCanaryPolicy {
  requestedStage: CanonicalTombstoneConflictCanaryStage
  canaryMaxObjectsPerSyncRun: number
  allowCandidateExecution: boolean
  allowsInternalN1Execution: boolean
  explicitInternalTestConfiguration: boolean
  runtimeSwitchEnabled: boolean
  allowAllEligible: boolean

  constructor(
    requestedStage: CanonicalTombstoneConflictCanaryStage = CanonicalTombstoneConflictCanaryStage.DISABLED,
    canaryMaxObjectsPerSyncRun: number = 0, allowCandidateExecution: boolean = false,
    allowsInternalN1Execution: boolean = false, explicitInternalTestConfiguration: boolean = false,
    runtimeSwitchEnabled: boolean = false, allowAllEligible: boolean = false
  ) {
    this.requestedStage = requestedStage
    this.canaryMaxObjectsPerSyncRun = Math.max(0, canaryMaxObjectsPerSyncRun)
    this.allowCandidateExecution = allowCandidateExecution
    this.allowsInternalN1Execution = allowsInternalN1Execution
    this.explicitInternalTestConfiguration = explicitInternalTestConfiguration
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.allowAllEligible = allowAllEligible
  }

  static get disabled(): CanonicalTombstoneConflictCanaryPolicy {
    return new CanonicalTombstoneConflictCanaryPolicy()
  }

  get budget(): number {
    return tcCanaryStageIsExecutable(this.requestedStage) ? tcCanaryStageBudget(this.requestedStage) :
      this.canaryMaxObjectsPerSyncRun
  }
}

export class CanonicalTombstoneConflictCandidate {
  action: CanonicalApplyAction
  recordingTombstone: CanonicalTombstone | null
  libraryTombstone: CanonicalLibraryTombstone | null
  conflict: CanonicalConflictRecord | null
  libraryConflict: CanonicalLibraryConflict | null
  rollbackCheckpointID: string | null
  tombstoneWinsIfNewerPolicy: boolean
  rollbackEvidenceAvailable: boolean
  explicitRestoreSignal: boolean
  staleLiveMetadataRisk: boolean
  conflictPolicyKnown: boolean
  routePath: string

  constructor(
    action: CanonicalApplyAction, recordingTombstone: CanonicalTombstone | null = null,
    libraryTombstone: CanonicalLibraryTombstone | null = null,
    conflict: CanonicalConflictRecord | null = null,
    libraryConflict: CanonicalLibraryConflict | null = null,
    rollbackCheckpointID: string | null = null, tombstoneWinsIfNewerPolicy: boolean = false,
    rollbackEvidenceAvailable: boolean = false, explicitRestoreSignal: boolean = false,
    staleLiveMetadataRisk: boolean = false, conflictPolicyKnown: boolean = true,
    routePath: string = '/sync/apply-metadata'
  ) {
    this.action = action
    this.recordingTombstone = recordingTombstone
    this.libraryTombstone = libraryTombstone
    this.conflict = conflict
    this.libraryConflict = libraryConflict
    this.rollbackCheckpointID = rollbackCheckpointID ?
      CanonicalProductionRedaction.safeIdentifier(rollbackCheckpointID, 'tombstone-conflict-checkpoint') : null
    this.tombstoneWinsIfNewerPolicy = tombstoneWinsIfNewerPolicy
    this.rollbackEvidenceAvailable = rollbackEvidenceAvailable
    this.explicitRestoreSignal = explicitRestoreSignal
    this.staleLiveMetadataRisk = staleLiveMetadataRisk
    this.conflictPolicyKnown = conflictPolicyKnown
    this.routePath = CanonicalProductionRedaction.safeDiagnosticText(routePath) ?? '/sync/apply-metadata'
  }

  get id(): string {
    return this.action.actionID
  }

  get objectID(): string {
    return this.action.target.objectID
  }

  get domain(): CanonicalTombstoneConflictDomain {
    if (this.actionKind === CanonicalTombstoneConflictActionKind.OBJECT_TOMBSTONE_APPLY ||
      this.actionKind === CanonicalTombstoneConflictActionKind.OBJECT_TOMBSTONE_SEND) {
      return CanonicalTombstoneConflictDomain.OBJECT_TOMBSTONE
    }
    if (this.actionKind === CanonicalTombstoneConflictActionKind.LIBRARY_TOMBSTONE_APPLY ||
      this.actionKind === CanonicalTombstoneConflictActionKind.LIBRARY_TOMBSTONE_SEND) {
      return CanonicalTombstoneConflictDomain.LIBRARY_TOMBSTONE
    }
    if (this.actionKind === CanonicalTombstoneConflictActionKind.GENERATED_ARTIFACT_TOMBSTONE_MARK_UNSUPPORTED) {
      return CanonicalTombstoneConflictDomain.GENERATED_ARTIFACT_TOMBSTONE_MARKER
    }
    if (this.actionKind === CanonicalTombstoneConflictActionKind.RESURRECTION_BLOCKED) {
      return CanonicalTombstoneConflictDomain.ACTIVE_VS_TOMBSTONE_CONFLICT
    }
    if (this.actionKind === CanonicalTombstoneConflictActionKind.CONFLICT_RECORD) {
      if (this.conflict?.kind === 'activeVsTombstone' || this.libraryConflict?.kind === 'activeVsTombstone') {
        return CanonicalTombstoneConflictDomain.ACTIVE_VS_TOMBSTONE_CONFLICT
      }
      return CanonicalTombstoneConflictDomain.METADATA_CONFLICT_RECORD
    }
    return CanonicalTombstoneConflictDomain.METADATA_CONFLICT_RECORD
  }

  get actionKind(): CanonicalTombstoneConflictActionKind {
    switch (this.action.kind) {
      case CanonicalApplyActionKind.OBJECT_TOMBSTONE_APPLY:
        return CanonicalTombstoneConflictActionKind.OBJECT_TOMBSTONE_APPLY
      case CanonicalApplyActionKind.OBJECT_TOMBSTONE_SEND:
        return CanonicalTombstoneConflictActionKind.OBJECT_TOMBSTONE_SEND
      case CanonicalApplyActionKind.LIBRARY_TOMBSTONE_APPLY:
        return CanonicalTombstoneConflictActionKind.LIBRARY_TOMBSTONE_APPLY
      case CanonicalApplyActionKind.LIBRARY_TOMBSTONE_SEND:
        return CanonicalTombstoneConflictActionKind.LIBRARY_TOMBSTONE_SEND
      case CanonicalApplyActionKind.ARTIFACT_TOMBSTONE_APPLY:
        return CanonicalTombstoneConflictActionKind.GENERATED_ARTIFACT_TOMBSTONE_MARK_UNSUPPORTED
      case CanonicalApplyActionKind.CONFLICT_RECORD:
        return CanonicalTombstoneConflictActionKind.CONFLICT_RECORD
      case CanonicalApplyActionKind.DEFERRED_UNSUPPORTED:
        return this.action.failureReason === 'tombstoneBlocksResurrection' ?
          CanonicalTombstoneConflictActionKind.RESURRECTION_BLOCKED :
          CanonicalTombstoneConflictActionKind.UNSUPPORTED
      default:
        return CanonicalTombstoneConflictActionKind.UNSUPPORTED
    }
  }

  get tombstoneState(): CanonicalTombstoneState {
    if (this.recordingTombstone?.state === 'tombstoned' || this.libraryTombstone ||
      actionKindIsTombstoneMarkerWrite(this.actionKind)) {
      return CanonicalTombstoneState.TOMBSTONED
    }
    return CanonicalTombstoneState.ACTIVE
  }

  get deletedAt(): CanonicalTimestamp | null {
    return this.recordingTombstone?.deletedAt ?? this.libraryTombstone?.deletedAt ?? null
  }

  get deletedAtSummary(): string {
    if (!this.deletedAt) {
      return 'deletedAt=missing'
    }
    const value = Math.floor(this.deletedAt.date.getTime() / 1000).toString()
    return 'deletedAtHash=' + (CanonicalProductionRedaction.hashPrefix(
      CanonicalHash.sha256OfString(value).value) ?? 'missing')
  }

  get conflictKindSummary(): string {
    return this.conflict?.kind ?? this.libraryConflict?.kind ??
      (this.actionKind === CanonicalTombstoneConflictActionKind.RESURRECTION_BLOCKED ?
        'resurrectionBlocked' : 'none')
  }

  get conflictPolicySummary(): string {
    return this.conflict?.resolutionPolicy ??
      (this.libraryConflict ? 'manualReview' :
        this.actionKind === CanonicalTombstoneConflictActionKind.RESURRECTION_BLOCKED ?
          'tombstoneRequiresManualReview' : 'none')
  }

  get markerHash(): CanonicalHash {
    return CanonicalHash.sha256OfMap({
      'schema': 'canonical-tombstone-conflict-v8-11',
      'objectID': this.objectID,
      'domain': this.domain,
      'actionKind': this.actionKind,
      'tombstoneState': this.tombstoneState,
      'deletedAtSummary': this.deletedAtSummary,
      'conflictKind': this.conflictKindSummary,
      'conflictPolicy': this.conflictPolicySummary,
      'softDeleteOnly': 'true',
      'physicalDeleteSuppressed': 'true',
      'permanentDeleteSuppressed': 'true',
      'tombstoneGCSuppressed': 'true'
    })
  }

  get hasActiveVsTombstoneConflict(): boolean {
    return this.conflict?.kind === 'activeVsTombstone' || this.libraryConflict?.kind === 'activeVsTombstone'
  }

  static candidates(
    applyPlan: CanonicalApplyPlan, libraryPlan: CanonicalLibrarySyncPlan | null,
    localManifest: CanonicalManifest, peerManifest: CanonicalManifest,
    rollbackCheckpointPrefix: string = 'tombstone-conflict-cutover'
  ): CanonicalTombstoneConflictCandidate[] {
    const localMap: Record<string, CanonicalRecordingObject> = {}
    for (const obj of localManifest.objects) {
      localMap[obj.objectID] = obj
    }
    const peerMap: Record<string, CanonicalRecordingObject> = {}
    for (const obj of peerManifest.objects) {
      peerMap[obj.objectID] = obj
    }
    const tombstonesByID: Record<string, CanonicalTombstone> = {}
    for (const t of applyPlan.tombstones) {
      tombstonesByID[t.tombstoneID] = t
    }
    const conflictsByID: Record<string, CanonicalConflictRecord> = {}
    for (const c of applyPlan.conflicts) {
      conflictsByID[c.conflictID] = c
    }
    let candidates: CanonicalTombstoneConflictCandidate[] = []
    for (const action of applyPlan.actions) {
      if (action.kind !== CanonicalApplyActionKind.OBJECT_TOMBSTONE_APPLY &&
        action.kind !== CanonicalApplyActionKind.OBJECT_TOMBSTONE_SEND &&
        action.kind !== CanonicalApplyActionKind.ARTIFACT_TOMBSTONE_APPLY &&
        action.kind !== CanonicalApplyActionKind.CONFLICT_RECORD &&
        !(action.kind === CanonicalApplyActionKind.DEFERRED_UNSUPPORTED &&
          action.failureReason === 'tombstoneBlocksResurrection')) {
        continue
      }
      candidates.push(new CanonicalTombstoneConflictCandidate(action,
        action.tombstoneID ? tombstonesByID[action.tombstoneID] : null,
        null, action.conflictID ? conflictsByID[action.conflictID] : null, null,
        rollbackCheckpointPrefix + '-' + action.target.objectID + '-' + action.kind,
        true, true, false, false, true))
    }
    return candidates
  }
}

export class CanonicalTombstoneConflictCutoverEvidence {
  noCommitEvidenceAvailable: boolean
  realDataShadowCopyVerified: boolean
  executionShadowVerified: boolean
  dryRunEquivalenceVerified: boolean
  noBlockingDivergence: boolean
  noUnresolvedConflict: boolean
  metadataRouteEvidenceAvailable: boolean
  productionPortAvailable: boolean
  realRootBoundApplyPortAvailable: boolean
  applyPortMode: CanonicalTombstoneConflictApplyPortMode
  rootBoundWriteAvailable: boolean
  atomicReplaceAvailable: boolean
  rollbackCheckpointAvailable: boolean
  rollbackVerified: boolean
  productionRootDisabledByDefault: boolean
  testRootUsed: boolean
  softTombstoneStoreSupported: boolean
  conflictLedgerSupported: boolean
  tombstoneWinsIfNewerPolicyAvailable: boolean
  rollbackEvidenceAvailable: boolean
  legacyFallbackAvailable: boolean
  rollbackPlan: CanonicalRollbackPlan | null

  constructor(
    noCommitEvidenceAvailable: boolean = false, realDataShadowCopyVerified: boolean = false,
    executionShadowVerified: boolean = false, dryRunEquivalenceVerified: boolean = false,
    noBlockingDivergence: boolean = false, noUnresolvedConflict: boolean = false,
    metadataRouteEvidenceAvailable: boolean = false, productionPortAvailable: boolean = false,
    realRootBoundApplyPortAvailable: boolean = false,
    applyPortMode: CanonicalTombstoneConflictApplyPortMode = CanonicalTombstoneConflictApplyPortMode.DISABLED,
    rootBoundWriteAvailable: boolean = false, atomicReplaceAvailable: boolean = false,
    rollbackCheckpointAvailable: boolean = false, rollbackVerified: boolean = false,
    productionRootDisabledByDefault: boolean = false, testRootUsed: boolean = false,
    softTombstoneStoreSupported: boolean = false, conflictLedgerSupported: boolean = false,
    tombstoneWinsIfNewerPolicyAvailable: boolean = false, rollbackEvidenceAvailable: boolean = false,
    legacyFallbackAvailable: boolean = false, rollbackPlan: CanonicalRollbackPlan | null = null
  ) {
    this.noCommitEvidenceAvailable = noCommitEvidenceAvailable
    this.realDataShadowCopyVerified = realDataShadowCopyVerified
    this.executionShadowVerified = executionShadowVerified
    this.dryRunEquivalenceVerified = dryRunEquivalenceVerified
    this.noBlockingDivergence = noBlockingDivergence
    this.noUnresolvedConflict = noUnresolvedConflict
    this.metadataRouteEvidenceAvailable = metadataRouteEvidenceAvailable
    this.productionPortAvailable = productionPortAvailable
    this.realRootBoundApplyPortAvailable = realRootBoundApplyPortAvailable
    this.applyPortMode = applyPortMode
    this.rootBoundWriteAvailable = rootBoundWriteAvailable
    this.atomicReplaceAvailable = atomicReplaceAvailable
    this.rollbackCheckpointAvailable = rollbackCheckpointAvailable
    this.rollbackVerified = rollbackVerified
    this.productionRootDisabledByDefault = productionRootDisabledByDefault
    this.testRootUsed = testRootUsed
    this.softTombstoneStoreSupported = softTombstoneStoreSupported
    this.conflictLedgerSupported = conflictLedgerSupported
    this.tombstoneWinsIfNewerPolicyAvailable = tombstoneWinsIfNewerPolicyAvailable
    this.rollbackEvidenceAvailable = rollbackEvidenceAvailable
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.rollbackPlan = rollbackPlan
  }

  static passing(rollbackPlan: CanonicalRollbackPlan): CanonicalTombstoneConflictCutoverEvidence {
    return new CanonicalTombstoneConflictCutoverEvidence(true, true, true, true, true, true, true, true, true,
      CanonicalTombstoneConflictApplyPortMode.TEST_ROOT_BOUND, true, true, true, true, true, true, true, true,
      true, true, true, rollbackPlan)
  }
}

export class CanonicalTombstoneConflictCutoverGate {
  mode: CanonicalCutoverMode
  allowed: boolean
  failures: CanonicalTombstoneConflictFailure[]
  legacyFallbackAvailable: boolean
  reason: string

  constructor(
    mode: CanonicalCutoverMode, failures: CanonicalTombstoneConflictFailure[],
    legacyFallbackAvailable: boolean, reason: string
  ) {
    this.mode = mode
    this.failures = [...new Set(failures)].sort()
    this.allowed = this.failures.length === 0
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ??
      (this.allowed ? 'allowed' : 'blocked')
  }
}

export class CanonicalTombstoneConflictProductionCommitResult {
  actionID: string
  objectID: string
  domain: CanonicalTombstoneConflictDomain
  actionKind: CanonicalTombstoneConflictActionKind
  committed: boolean
  partialCommit: boolean
  preconditionVerified: boolean
  postconditionVerified: boolean
  tombstoneState: CanonicalTombstoneState
  deletedAtSummary: string
  conflictKind: string
  conflictPolicy: string
  sideEffects: CanonicalProductionSideEffect[]
  failureKind: CanonicalTombstoneConflictFailure | null
  reason: string

  constructor(
    actionID: string, objectID: string, domain: CanonicalTombstoneConflictDomain,
    actionKind: CanonicalTombstoneConflictActionKind, committed: boolean, partialCommit: boolean = false,
    preconditionVerified: boolean = true, postconditionVerified: boolean = true,
    tombstoneState: CanonicalTombstoneState, deletedAtSummary: string, conflictKind: string,
    conflictPolicy: string, sideEffects: CanonicalProductionSideEffect[] = [],
    failureKind: CanonicalTombstoneConflictFailure | null = null, reason: string
  ) {
    this.actionID = CanonicalProductionRedaction.safeIdentifier(actionID, actionKind)
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'tombstone-object')
    this.domain = domain
    this.actionKind = actionKind
    this.committed = committed
    this.partialCommit = partialCommit
    this.preconditionVerified = preconditionVerified
    this.postconditionVerified = postconditionVerified
    this.tombstoneState = tombstoneState
    this.deletedAtSummary = CanonicalProductionRedaction.safeDiagnosticText(deletedAtSummary) ?? 'deletedAt=missing'
    this.conflictKind = CanonicalProductionRedaction.safeDiagnosticText(conflictKind) ?? 'none'
    this.conflictPolicy = CanonicalProductionRedaction.safeDiagnosticText(conflictPolicy) ?? 'none'
    this.sideEffects = sideEffects
    this.failureKind = failureKind
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? (committed ? 'committed' : 'failed')
  }
}

export class CanonicalTombstoneConflictRollbackExecutionResult {
  checkpointID: string
  succeeded: boolean
  fatal: boolean
  reason: string

  constructor(checkpointID: string, succeeded: boolean, fatal: boolean = false, reason: string) {
    this.checkpointID = CanonicalProductionRedaction.safeIdentifier(checkpointID, 'tombstone-conflict-checkpoint')
    this.succeeded = succeeded
    this.fatal = fatal
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ??
      (succeeded ? 'rollbackCompleted' : 'rollbackFailed')
  }
}

export interface CanonicalTombstoneConflictCutoverExecutor {
  commitTombstoneConflict(candidate: CanonicalTombstoneConflictCandidate):
    Promise<CanonicalTombstoneConflictProductionCommitResult>
  rollbackTombstoneConflict(candidate: CanonicalTombstoneConflictCandidate,
    reason: CanonicalTombstoneConflictFailure):
    Promise<CanonicalTombstoneConflictRollbackExecutionResult>
}

export class CanonicalTombstoneConflictCutoverDiagnostic {
  kind: CanonicalTombstoneConflictCutoverDiagnosticKind
  syncRunID: string | null
  trigger: CanonicalSyncPlanTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  domain: CanonicalTombstoneConflictDomain | null
  objectID: string | null
  action: string | null
  tombstoneState: CanonicalTombstoneState | null
  conflictKind: string | null
  result: string | null
  reason: string | null
  hashPrefix: string | null

  constructor(
    kind: CanonicalTombstoneConflictCutoverDiagnosticKind, syncRunID: string | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole,
    domain: CanonicalTombstoneConflictDomain | null = null, objectID: string | null = null,
    action: string | null = null, tombstoneState: CanonicalTombstoneState | null = null,
    conflictKind: string | null = null, result: string | null = null, reason: string | null = null,
    hash: CanonicalHash | null = null
  ) {
    this.kind = kind
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.domain = domain
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'tombstone-object') : null
    this.action = CanonicalProductionRedaction.safeDiagnosticText(action)
    this.tombstoneState = tombstoneState
    this.conflictKind = CanonicalProductionRedaction.safeDiagnosticText(conflictKind)
    this.result = CanonicalProductionRedaction.safeDiagnosticText(result)
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason)
    this.hashPrefix = hash ? CanonicalProductionRedaction.hashPrefix(hash.value) : null
  }

  get id(): string {
    return [this.kind, this.objectID ?? 'run', this.result ?? '', this.reason ?? ''].join('|')
  }
}

export class CanonicalTombstoneConflictCutoverResult {
  gate: CanonicalTombstoneConflictCutoverGate
  commits: CanonicalTombstoneConflictProductionCommitResult[]
  rollbackResults: CanonicalTombstoneConflictRollbackExecutionResult[]
  diagnostics: CanonicalTombstoneConflictCutoverDiagnostic[]
  legacyFallbackUsed: boolean
  duplicateLegacySuppressedActionIDs: string[]
  canaryAttemptedCount: number
  canarySucceeded: boolean
  fatalBlocker: boolean

  constructor(
    gate: CanonicalTombstoneConflictCutoverGate,
    commits: CanonicalTombstoneConflictProductionCommitResult[] = [],
    rollbackResults: CanonicalTombstoneConflictRollbackExecutionResult[] = [],
    diagnostics: CanonicalTombstoneConflictCutoverDiagnostic[] = [],
    legacyFallbackUsed: boolean = false, duplicateLegacySuppressedActionIDs: string[] = [],
    canaryAttemptedCount: number = 0, canarySucceeded: boolean = false, fatalBlocker: boolean = false
  ) {
    this.gate = gate
    this.commits = commits
    this.rollbackResults = rollbackResults
    this.diagnostics = diagnostics
    this.legacyFallbackUsed = legacyFallbackUsed
    this.duplicateLegacySuppressedActionIDs = duplicateLegacySuppressedActionIDs
    this.canaryAttemptedCount = canaryAttemptedCount
    this.canarySucceeded = canarySucceeded
    this.fatalBlocker = fatalBlocker
  }

  get succeeded(): boolean {
    return this.gate.allowed && !this.fatalBlocker && this.commits.length > 0 &&
      this.commits.every(
        (c: CanonicalTombstoneConflictProductionCommitResult) => c.committed && c.preconditionVerified &&
          c.postconditionVerified)
  }
}

export class CanonicalTombstoneConflictCutoverRunner {
  constructor() {}

  evaluateGate(
    mode: CanonicalCutoverMode, policy: CanonicalTombstoneConflictCanaryPolicy,
    token: CanonicalCutoverToken | null, evidence: CanonicalTombstoneConflictCutoverEvidence,
    candidates: CanonicalTombstoneConflictCandidate[], trigger: CanonicalSyncPlanTrigger
  ): CanonicalTombstoneConflictCutoverGate {
    let failures: CanonicalTombstoneConflictFailure[] = []
    if (mode === CanonicalCutoverMode.DISABLED) {
      failures.push(CanonicalTombstoneConflictFailure.DISABLED)
    }
    if (mode !== CanonicalCutoverMode.CANARY && mode !== CanonicalCutoverMode.GUARDED_EXECUTE_COMMIT) {
      failures.push(CanonicalTombstoneConflictFailure.UNSUPPORTED_MODE)
    }
    if (!token) {
      failures.push(CanonicalTombstoneConflictFailure.MISSING_TOKEN)
    }
    if (token?.ownerApproved !== true) {
      failures.push(CanonicalTombstoneConflictFailure.MISSING_OWNER_APPROVAL)
    }
    const executableCandidates = candidates.filter((c: CanonicalTombstoneConflictCandidate) =>
      actionKindIsExecutable(c.actionKind))
    const requiredDomains = new Set(executableCandidates.map(
      (c: CanonicalTombstoneConflictCandidate) => domainProductionDomain(c.domain)))
    if (executableCandidates.length === 0) {
      failures.push(CanonicalTombstoneConflictFailure.UNSUPPORTED_ACTION)
    } else if (![...requiredDomains].every(
      (d: CanonicalProductionDomain) => evidence.rollbackPlan?.covers(d) === true)) {
      failures.push(CanonicalTombstoneConflictFailure.MISSING_ROLLBACK)
    }
    if (!evidence.noCommitEvidenceAvailable) {
      failures.push(CanonicalTombstoneConflictFailure.MISSING_NO_COMMIT_EVIDENCE)
    }
    if (!evidence.dryRunEquivalenceVerified) {
      failures.push(CanonicalTombstoneConflictFailure.MISSING_DRY_RUN_EQUIVALENCE)
    }
    if (!evidence.executionShadowVerified) {
      failures.push(CanonicalTombstoneConflictFailure.MISSING_EXECUTION_SHADOW_EVIDENCE)
    }
    if (!evidence.realDataShadowCopyVerified) {
      failures.push(CanonicalTombstoneConflictFailure.MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE)
    }
    if (!evidence.noBlockingDivergence) {
      failures.push(CanonicalTombstoneConflictFailure.BLOCKING_DIVERGENCE)
    }
    if (!evidence.noUnresolvedConflict) {
      failures.push(CanonicalTombstoneConflictFailure.UNRESOLVED_CONFLICT)
    }
    if (!evidence.metadataRouteEvidenceAvailable) {
      failures.push(CanonicalTombstoneConflictFailure.MISSING_METADATA_ROUTE_EVIDENCE)
    }
    if (!evidence.productionPortAvailable) {
      failures.push(CanonicalTombstoneConflictFailure.PRODUCTION_PORT_UNAVAILABLE)
    }
    if (!evidence.realRootBoundApplyPortAvailable) {
      failures.push(CanonicalTombstoneConflictFailure.ROOT_BOUND_WRITE_UNAVAILABLE)
    }
    if (!tcApplyPortIsNonDryRunRootBound(evidence.applyPortMode)) {
      failures.push(CanonicalTombstoneConflictFailure.APPLY_PORT_DRY_RUN_ONLY)
    }
    if (!evidence.rootBoundWriteAvailable) {
      failures.push(CanonicalTombstoneConflictFailure.ROOT_BOUND_WRITE_UNAVAILABLE)
    }
    if (!evidence.atomicReplaceAvailable) {
      failures.push(CanonicalTombstoneConflictFailure.ATOMIC_REPLACE_UNAVAILABLE)
    }
    if (!evidence.rollbackCheckpointAvailable) {
      failures.push(CanonicalTombstoneConflictFailure.ROLLBACK_CHECKPOINT_UNAVAILABLE)
    }
    if (!evidence.rollbackVerified) {
      failures.push(CanonicalTombstoneConflictFailure.ROLLBACK_VERIFICATION_MISSING)
    }
    if (!evidence.productionRootDisabledByDefault) {
      failures.push(CanonicalTombstoneConflictFailure.PRODUCTION_ROOT_ENABLED_BY_DEFAULT)
    }
    if (evidence.applyPortMode === CanonicalTombstoneConflictApplyPortMode.TEST_ROOT_BOUND &&
      !evidence.testRootUsed) {
      failures.push(CanonicalTombstoneConflictFailure.TEST_ROOT_MISSING)
    }
    if (!evidence.softTombstoneStoreSupported) {
      failures.push(CanonicalTombstoneConflictFailure.SOFT_DELETE_MARKER_UNSUPPORTED)
      failures.push(CanonicalTombstoneConflictFailure.SOFT_TOMBSTONE_STORE_UNSUPPORTED)
    }
    if (candidates.some((c: CanonicalTombstoneConflictCandidate) => domainRequiresConflictLedger(c.domain)) &&
      !evidence.conflictLedgerSupported) {
      failures.push(CanonicalTombstoneConflictFailure.CONFLICT_LEDGER_UNSUPPORTED)
    }
    if (!evidence.legacyFallbackAvailable) {
      failures.push(CanonicalTombstoneConflictFailure.MISSING_ROLLBACK)
    }
    if (trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH ||
      trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) {
      failures.push(CanonicalTombstoneConflictFailure.UNSUPPORTED_MODE)
    }
    if (candidates.some((c: CanonicalTombstoneConflictCandidate) =>
      c.actionKind === CanonicalTombstoneConflictActionKind.GENERATED_ARTIFACT_TOMBSTONE_MARK_UNSUPPORTED)) {
      failures.push(CanonicalTombstoneConflictFailure.GENERATED_ARTIFACT_TOMBSTONE_UNSUPPORTED)
    }
    if (candidates.some((c: CanonicalTombstoneConflictCandidate) =>
      c.actionKind === CanonicalTombstoneConflictActionKind.UNSUPPORTED)) {
      failures.push(CanonicalTombstoneConflictFailure.UNSUPPORTED_ACTION)
    }
    return new CanonicalTombstoneConflictCutoverGate(mode, failures, evidence.legacyFallbackAvailable,
      failures.length === 0 ? 'allowed' : failures.join(','))
  }
}

export class CanonicalTombstoneConflictCutoverAppSeamPolicy {
  recordDiagnostics: boolean
  maxDiagnosticsEvents: number
  canaryPolicy: CanonicalTombstoneConflictCanaryPolicy

  constructor(
    recordDiagnostics: boolean = true, maxDiagnosticsEvents: number = 200,
    canaryPolicy: CanonicalTombstoneConflictCanaryPolicy = CanonicalTombstoneConflictCanaryPolicy.disabled
  ) {
    this.recordDiagnostics = recordDiagnostics
    this.maxDiagnosticsEvents = Math.max(1, maxDiagnosticsEvents)
    this.canaryPolicy = canaryPolicy
  }
}

export class CanonicalTombstoneConflictCutoverAppSeamConfiguration {
  isEnabled: boolean
  mode: CanonicalCutoverAppSeamMode
  policy: CanonicalTombstoneConflictCutoverAppSeamPolicy
  evidence: CanonicalTombstoneConflictCutoverEvidence
  cutoverToken: CanonicalCutoverToken | null

  constructor(
    isEnabled: boolean = false, mode: CanonicalCutoverAppSeamMode = CanonicalCutoverAppSeamMode.DISABLED,
    policy: CanonicalTombstoneConflictCutoverAppSeamPolicy = new CanonicalTombstoneConflictCutoverAppSeamPolicy(),
    evidence: CanonicalTombstoneConflictCutoverEvidence = new CanonicalTombstoneConflictCutoverEvidence(),
    cutoverToken: CanonicalCutoverToken | null = null
  ) {
    this.isEnabled = isEnabled
    this.mode = isEnabled ? mode : CanonicalCutoverAppSeamMode.DISABLED
    this.policy = policy
    this.evidence = evidence
    this.cutoverToken = cutoverToken
  }

  static get disabled(): CanonicalTombstoneConflictCutoverAppSeamConfiguration {
    return new CanonicalTombstoneConflictCutoverAppSeamConfiguration()
  }

  get effectiveMode(): CanonicalCutoverAppSeamMode {
    return this.isEnabled ? this.mode : CanonicalCutoverAppSeamMode.DISABLED
  }
}
