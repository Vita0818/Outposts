/**
 * CanonicalGeneratedArtifactCutover — generated artifact cutover domain.
 * Ported from Rokurics-Apple (CanonicalGeneratedArtifactCutover.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalNode, CanonicalRecordingObject,
  CanonicalArtifact, CanonicalArtifactKind, CanonicalArtifactAvailability,
  CanonicalArtifactProducer, CanonicalManifest, hashPrefix, generatedArtifactKinds,
  safeLogicalPathToken
} from './CanonicalCore'
import {
  CanonicalCutoverMode, CanonicalCutoverToken, CanonicalCutoverAppSeamMode,
  CanonicalSyncPlanTrigger
} from './CanonicalAudioUploadCutover'

// ── Forward types ──

export class CanonicalApplyTarget {
  objectID: string
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null

  constructor(objectID: string, artifactID: string | null = null, artifactKind: CanonicalArtifactKind | null = null) {
    this.objectID = objectID
    this.artifactID = artifactID
    this.artifactKind = artifactKind
  }
}

export enum CanonicalApplyActionKind {
  GENERATED_ARTIFACT_DOWNLOAD_APPLY = 'generatedArtifactDownloadApply',
  GENERATED_ARTIFACT_NO_OP = 'generatedArtifactNoOp',
  CONFLICT_RECORD = 'conflictRecord',
  OBJECT_TOMBSTONE_APPLY = 'objectTombstoneApply',
  OBJECT_TOMBSTONE_SEND = 'objectTombstoneSend',
  ARTIFACT_TOMBSTONE_APPLY = 'artifactTombstoneApply',
  LIBRARY_TOMBSTONE_APPLY = 'libraryTombstoneApply',
  LIBRARY_TOMBSTONE_SEND = 'libraryTombstoneSend',
  DEFERRED_UNSUPPORTED = 'deferredUnsupported'
}

export class CanonicalApplyAction {
  actionID: string
  kind: CanonicalApplyActionKind
  target: CanonicalApplyTarget
  reason: string
  failureReason: string | null

  constructor(actionID: string, kind: CanonicalApplyActionKind, target: CanonicalApplyTarget,
    reason: string = '', failureReason: string | null = null) {
    this.actionID = actionID
    this.kind = kind
    this.target = target
    this.reason = reason
    this.failureReason = failureReason
  }
}

export class CanonicalApplyPlan {
  actions: CanonicalApplyAction[]
  tombstones: any[]
  conflicts: any[]

  constructor(actions: CanonicalApplyAction[] = [], tombstones: any[] = [], conflicts: any[] = []) {
    this.actions = actions
    this.tombstones = tombstones
    this.conflicts = conflicts
  }
}

export class CanonicalRollbackPlan {
  coversDomainFn: (domain: any) => boolean

  constructor(coversDomainFn: (domain: any) => boolean) {
    this.coversDomainFn = coversDomainFn
  }

  covers(domain: any): boolean {
    return this.coversDomainFn(domain)
  }
}

export class CanonicalRollbackResult {
  checkpointID: string
  succeeded: boolean

  constructor(checkpointID: string, succeeded: boolean) {
    this.checkpointID = checkpointID
    this.succeeded = succeeded
  }
}

export enum CanonicalProductionDomain {
  TOMBSTONES = 'tombstones',
  CONFLICTS = 'conflicts',
  GENERATED_ARTIFACTS = 'generatedArtifacts'
}

export enum CanonicalMigrationDomain {
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  TOMBSTONE_CONFLICT = 'tombstoneConflict'
}

export class CanonicalMigrationMatrixReport {
  allowed: boolean
  activePilotDomain: CanonicalMigrationDomain | null
  blockers: string[]

  constructor(allowed: boolean, activePilotDomain: CanonicalMigrationDomain | null, blockers: string[] = []) {
    this.allowed = allowed
    this.activePilotDomain = activePilotDomain
    this.blockers = [...new Set(blockers)].sort()
  }
}

export class CanonicalMigrationDomainMatrix {
  static v822GeneratedArtifactsActivePilot(
    libraryMetadataObservationCompleteOrRetirementCandidateReady: boolean,
    templateReport?: CanonicalGeneratedArtifactTemplateReport
  ): CanonicalMigrationDomainMatrix {
    return new CanonicalMigrationDomainMatrix(CanonicalMigrationDomain.GENERATED_ARTIFACTS, true)
  }

  private domain: CanonicalMigrationDomain
  private allAllowed: boolean

  constructor(domain: CanonicalMigrationDomain, allAllowed: boolean) {
    this.domain = domain
    this.allAllowed = allAllowed
  }

  validate(): CanonicalMigrationMatrixReport {
    return new CanonicalMigrationMatrixReport(this.allAllowed, this.domain)
  }

  policyFor(domain: CanonicalMigrationDomain): any {
    return {
      hasReached: (stage: string) => stage === 'canaryN0',
      defaultCutoverEnabled: false,
      releaseDefaultEnabledCutover: false,
      runtimeSwitchEnabled: false,
      legacySuppressionAllowed: false,
      readPathLegacy: true,
      noProductionInjection: true
    }
  }
}

export class CanonicalGeneratedArtifactTemplateReport {
  readyForNextPilotN0: boolean

  constructor(readyForNextPilotN0: boolean = true) {
    this.readyForNextPilotN0 = readyForNextPilotN0
  }

  static currentV821Audit(): CanonicalGeneratedArtifactTemplateReport {
    return new CanonicalGeneratedArtifactTemplateReport(true)
  }
}

export enum CanonicalProductionExecutionDomainRole {
  I_PHONE = 'iPhone',
  MAC = 'mac',
  TEST_HARNESS = 'testHarness'
}

export class CanonicalProductionSideEffect {
  kind: string
  target: string | null

  constructor(kind: string, target: string | null = null) {
    this.kind = kind
    this.target = target
  }
}

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

class CanonicalProjectionContract {
  static generatedArtifactKinds = generatedArtifactKinds()

  static safeLogicalPathToken(token: string | null): string | null {
    return safeLogicalPathToken(token)
  }

  static isAuthoritativeProducer(artifact: CanonicalArtifact, node: CanonicalNode): boolean {
    if (!artifact.producedBy) {
      return false
    }
    return true
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS
// ═══════════════════════════════════════════════════════════════════════════════

export enum CanonicalGeneratedArtifactCutoverDomain {
  GENERATED_ARTIFACTS = 'generatedArtifacts'
}

export enum CanonicalGeneratedArtifactCutoverActionKind {
  GENERATED_ARTIFACT_APPLY = 'generatedArtifactApply',
  GENERATED_ARTIFACT_DOWNLOAD_APPLY = 'generatedArtifactDownloadApply',
  GENERATED_ARTIFACT_NO_OP = 'generatedArtifactNoOp',
  GENERATED_ARTIFACT_CONFLICT_RECORD = 'generatedArtifactConflictRecord',
  UNSUPPORTED = 'unsupported'
}

export function isExecutableApply(kind: CanonicalGeneratedArtifactCutoverActionKind): boolean {
  return kind === CanonicalGeneratedArtifactCutoverActionKind.GENERATED_ARTIFACT_APPLY ||
    kind === CanonicalGeneratedArtifactCutoverActionKind.GENERATED_ARTIFACT_DOWNLOAD_APPLY
}

export enum CanonicalGeneratedArtifactCutoverFailure {
  DISABLED = 'disabled',
  UNSUPPORTED_DOMAIN = 'unsupportedDomain',
  UNSUPPORTED_MODE = 'unsupportedMode',
  UNSUPPORTED_KIND = 'unsupportedKind',
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
  MISSING_ARTIFACT_REQUEST_ROUTE_EVIDENCE = 'missingArtifactRequestRouteEvidence',
  PRODUCTION_PORT_UNAVAILABLE = 'productionPortUnavailable',
  APPLY_PORT_DRY_RUN_ONLY = 'applyPortDryRunOnly',
  ROOT_BOUND_WRITE_UNAVAILABLE = 'rootBoundWriteUnavailable',
  ATOMIC_REPLACE_UNAVAILABLE = 'atomicReplaceUnavailable',
  ROLLBACK_CHECKPOINT_UNAVAILABLE = 'rollbackCheckpointUnavailable',
  ROLLBACK_VERIFICATION_MISSING = 'rollbackVerificationMissing',
  PRODUCTION_ROOT_ENABLED_BY_DEFAULT = 'productionRootEnabledByDefault',
  TEST_ROOT_MISSING = 'testRootMissing',
  PARENT_TOMBSTONED = 'parentTombstoned',
  PEER_UNKNOWN = 'peerUnknown',
  PEER_NOT_AUTHORITATIVE = 'peerNotAuthoritative',
  PRODUCER_AMBIGUOUS = 'producerAmbiguous',
  ARTIFACT_ID_MISMATCH = 'artifactIDMismatch',
  OBJECT_ID_MISMATCH = 'objectIDMismatch',
  EXPECTED_HASH_MISSING = 'expectedHashMissing',
  EXPECTED_BYTE_SIZE_MISSING = 'expectedByteSizeMissing',
  LOCAL_PREVIOUS_STATE_MISSING = 'localPreviousStateMissing',
  ARTIFACT_BYTES_MISSING = 'artifactBytesMissing',
  HASH_MISMATCH_BEFORE_APPLY = 'hashMismatchBeforeApply',
  APPLY_FAILURE_BEFORE_COMMIT = 'applyFailureBeforeCommit',
  APPLY_FAILURE_AFTER_PARTIAL_COMMIT = 'applyFailureAfterPartialCommit',
  POSTCONDITION_MISMATCH = 'postconditionMismatch',
  ROLLBACK_FAILURE = 'rollbackFailure',
  MISSING_INTERNAL_CANARY_CONFIGURATION = 'missingInternalCanaryConfiguration',
  CANARY_BUDGET_ABOVE_ONE_DENIED = 'canaryBudgetAboveOneDenied',
  ALL_ELIGIBLE_CANARY_DENIED = 'allEligibleCanaryDenied',
  ACTIVE_PILOT_NOT_GENERATED_ARTIFACTS = 'activePilotNotGeneratedArtifacts',
  MATRIX_VALIDATION_BLOCKED = 'matrixValidationBlocked',
  DEFAULT_ENABLEMENT_DENIED = 'defaultEnablementDenied',
  MISSING_READ_SIDE_PARALLEL_EVIDENCE = 'missingReadSideParallelEvidence',
  COMMIT_EXECUTOR_UNAVAILABLE = 'commitExecutorUnavailable',
  PEER_SNAPSHOT_UNAVAILABLE = 'peerSnapshotUnavailable',
  MISSING_CANARY_STAGE_EVIDENCE = 'missingCanaryStageEvidence',
  CANARY_STAGE_BLOCKED = 'canaryStageBlocked',
  CANARY_STAGE_ORDER_VIOLATION = 'canaryStageOrderViolation',
  OBSERVATION_WINDOW_INCOMPLETE = 'observationWindowIncomplete',
  RUNTIME_SWITCH_DENIED = 'runtimeSwitchDenied',
  PREVIOUS_STAGE_FAILURE = 'previousStageFailure',
  PREVIOUS_STAGE_ROLLBACK_FAILURE = 'previousStageRollbackFailure',
  CONTENT_LEAK_RISK = 'contentLeakRisk',
  UNSAFE_PATH_TOKEN = 'unsafePathToken',
  AUDIO_CONFUSION_RISK = 'audioConfusionRisk',
  HASH_UNAVAILABLE = 'hashUnavailable',
  BYTE_SIZE_UNAVAILABLE = 'byteSizeUnavailable'
}

export enum CanonicalGeneratedArtifactCanaryStage {
  DISABLED = 'disabled',
  N1 = 'n1',
  N3 = 'n3',
  N10 = 'n10',
  ALL_ELIGIBLE = 'allEligible'
}

export function canaryStageIsExecutable(stage: CanonicalGeneratedArtifactCanaryStage): boolean {
  return stage !== CanonicalGeneratedArtifactCanaryStage.DISABLED
}

export function canaryStagePrev(stage: CanonicalGeneratedArtifactCanaryStage): CanonicalGeneratedArtifactCanaryStage | null {
  switch (stage) {
    case CanonicalGeneratedArtifactCanaryStage.DISABLED: return null
    case CanonicalGeneratedArtifactCanaryStage.N1: return CanonicalGeneratedArtifactCanaryStage.DISABLED
    case CanonicalGeneratedArtifactCanaryStage.N3: return CanonicalGeneratedArtifactCanaryStage.N1
    case CanonicalGeneratedArtifactCanaryStage.N10: return CanonicalGeneratedArtifactCanaryStage.N3
    case CanonicalGeneratedArtifactCanaryStage.ALL_ELIGIBLE: return CanonicalGeneratedArtifactCanaryStage.N10
  }
}

export function canaryStageBudget(stage: CanonicalGeneratedArtifactCanaryStage): number {
  switch (stage) {
    case CanonicalGeneratedArtifactCanaryStage.DISABLED: return 0
    case CanonicalGeneratedArtifactCanaryStage.N1: return 1
    case CanonicalGeneratedArtifactCanaryStage.N3: return 3
    case CanonicalGeneratedArtifactCanaryStage.N10: return 10
    case CanonicalGeneratedArtifactCanaryStage.ALL_ELIGIBLE: return Number.MAX_SAFE_INTEGER
  }
}

export function canaryStageMinPrevSuccess(stage: CanonicalGeneratedArtifactCanaryStage): number {
  switch (stage) {
    case CanonicalGeneratedArtifactCanaryStage.DISABLED:
    case CanonicalGeneratedArtifactCanaryStage.N1: return 0
    case CanonicalGeneratedArtifactCanaryStage.N3: return 1
    case CanonicalGeneratedArtifactCanaryStage.N10: return 3
    case CanonicalGeneratedArtifactCanaryStage.ALL_ELIGIBLE: return 10
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CUTOVER CANDIDATE
// ═══════════════════════════════════════════════════════════════════════════════

export class CanonicalGeneratedArtifactCutoverCandidate {
  action: CanonicalApplyAction
  localObject: CanonicalRecordingObject | null
  peerObject: CanonicalRecordingObject | null
  localArtifact: CanonicalArtifact | null
  peerArtifact: CanonicalArtifact | null
  rollbackCheckpointID: string | null
  unresolvedConflict: boolean
  routePath: string

  constructor(
    action: CanonicalApplyAction, localObject: CanonicalRecordingObject | null,
    peerObject: CanonicalRecordingObject | null, localArtifact: CanonicalArtifact | null = null,
    peerArtifact: CanonicalArtifact | null = null, rollbackCheckpointID: string | null = null,
    unresolvedConflict: boolean = false, routePath: string = '/sync/artifact-request'
  ) {
    const targetKind = action.target.artifactKind
    this.action = action
    this.localObject = localObject
    this.peerObject = peerObject
    this.localArtifact = localArtifact ?? CanonicalGeneratedArtifactCutoverCandidate.artifact(targetKind,
      localObject)
    this.peerArtifact = peerArtifact ?? CanonicalGeneratedArtifactCutoverCandidate.artifact(targetKind,
      peerObject)
    this.rollbackCheckpointID = rollbackCheckpointID ?
      CanonicalProductionRedaction.safeIdentifier(rollbackCheckpointID, 'generated-artifact-checkpoint') : null
    this.unresolvedConflict = unresolvedConflict
    this.routePath = CanonicalProductionRedaction.safeDiagnosticText(routePath) ?? '/sync/artifact-request'
  }

  get id(): string {
    return this.action.actionID
  }

  get objectID(): string {
    return this.action.target.objectID
  }

  get artifactID(): string | null {
    return this.action.target.artifactID ?? this.peerArtifact?.artifactID ?? this.localArtifact?.artifactID ?? null
  }

  get artifactKind(): CanonicalArtifactKind | null {
    return this.action.target.artifactKind ?? this.peerArtifact?.kind ?? this.localArtifact?.kind ?? null
  }

  get cutoverActionKind(): CanonicalGeneratedArtifactCutoverActionKind {
    switch (this.action.kind) {
      case CanonicalApplyActionKind.GENERATED_ARTIFACT_DOWNLOAD_APPLY:
        return CanonicalGeneratedArtifactCutoverActionKind.GENERATED_ARTIFACT_DOWNLOAD_APPLY
      case CanonicalApplyActionKind.GENERATED_ARTIFACT_NO_OP:
        return CanonicalGeneratedArtifactCutoverActionKind.GENERATED_ARTIFACT_NO_OP
      case CanonicalApplyActionKind.CONFLICT_RECORD:
        return this.action.target.artifactKind ?
          CanonicalGeneratedArtifactCutoverActionKind.GENERATED_ARTIFACT_CONFLICT_RECORD :
          CanonicalGeneratedArtifactCutoverActionKind.UNSUPPORTED
      default:
        return CanonicalGeneratedArtifactCutoverActionKind.UNSUPPORTED
    }
  }

  get expectedArtifact(): CanonicalArtifact | null {
    return this.peerArtifact
  }

  get expectedContentHash(): CanonicalHash | null {
    return this.expectedArtifact?.contentHash ?? null
  }

  get expectedByteSize(): number | null {
    return this.expectedArtifact?.byteSize ?? null
  }

  get expectedLogicalPathToken(): string | null {
    return this.expectedArtifact?.logicalPathToken ?? this.action.target.artifactKind ?
      CanonicalRootBoundGeneratedArtifactTarget.defaultLogicalPathToken(this.objectID,
        this.action.target.artifactKind!) : null
  }

  get effectiveRollbackCheckpointID(): string {
    return this.rollbackCheckpointID ?? 'generated-artifact-cutover-' + this.objectID + '-' +
      (this.artifactKind ?? 'unknown')
  }

  get parentObjectTombstoned(): boolean {
    return this.localObject?.metadata.isDeleted === true || this.peerObject?.metadata.isDeleted === true ||
      this.localObject?.syncState === 'deleted' || this.peerObject?.syncState === 'deleted'
  }

  peerIsAuthoritative(peerNode: CanonicalNode | null): boolean {
    if (!this.peerArtifact || !peerNode) {
      return false
    }
    return CanonicalProjectionContract.isAuthoritativeProducer(this.peerArtifact, peerNode)
  }

  static candidates(
    applyPlan: CanonicalApplyPlan, localManifest: CanonicalManifest, peerManifest: CanonicalManifest,
    rollbackCheckpointPrefix: string = 'generated-artifact-cutover'
  ): CanonicalGeneratedArtifactCutoverCandidate[] {
    const localMap: Record<string, CanonicalRecordingObject> = {}
    for (const obj of localManifest.objects) {
      localMap[obj.objectID] = obj
    }
    const peerMap: Record<string, CanonicalRecordingObject> = {}
    for (const obj of peerManifest.objects) {
      peerMap[obj.objectID] = obj
    }
    return applyPlan.actions
      .filter((a: CanonicalApplyAction) => a.kind === CanonicalApplyActionKind.GENERATED_ARTIFACT_DOWNLOAD_APPLY)
      .map((action: CanonicalApplyAction) => new CanonicalGeneratedArtifactCutoverCandidate(action,
        localMap[action.target.objectID] ?? null, peerMap[action.target.objectID] ?? null, null, null,
        rollbackCheckpointPrefix + '-' + action.target.objectID + '-' +
        (action.target.artifactKind ?? 'artifact')))
  }

  private static artifact(kind: CanonicalArtifactKind | null,
    obj: CanonicalRecordingObject | null): CanonicalArtifact | null {
    if (!kind || !obj) {
      return null
    }
    return obj.artifacts.find((a: CanonicalArtifact) => a.kind === kind) ?? null
  }
}

export class CanonicalRootBoundGeneratedArtifactTarget {
  static defaultLogicalPathToken(objectID: string, kind: CanonicalArtifactKind): string {
    return 'generated-artifacts/' + kind + '/' + objectID + '.json'
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// EVIDENCE, STAGE, CANARY
// ═══════════════════════════════════════════════════════════════════════════════

export enum CanonicalGeneratedArtifactApplyPortMode {
  DISABLED = 'disabled',
  DRY_RUN = 'dryRun',
  FAKE_IN_MEMORY = 'fakeInMemory',
  TEST_ROOT_BOUND = 'testRootBound',
  PRODUCTION_ROOT_DISABLED = 'productionRootDisabled',
  PRODUCTION_ROOT_BOUND = 'productionRootBound',
  PRODUCTION_ROOT_UNSUPPORTED = 'productionRootUnsupported'
}

export function isNonDryRunRootBound(mode: CanonicalGeneratedArtifactApplyPortMode): boolean {
  return mode === CanonicalGeneratedArtifactApplyPortMode.TEST_ROOT_BOUND ||
    mode === CanonicalGeneratedArtifactApplyPortMode.PRODUCTION_ROOT_BOUND
}

export class CanonicalGeneratedArtifactCutoverEvidence {
  noCommitEvidenceAvailable: boolean
  realDataShadowCopyVerified: boolean
  executionShadowVerified: boolean
  dryRunEquivalenceVerified: boolean
  noBlockingDivergence: boolean
  noUnresolvedConflict: boolean
  artifactRequestRouteEvidenceAvailable: boolean
  productionPortAvailable: boolean
  realRootBoundApplyPortAvailable: boolean
  applyPortMode: CanonicalGeneratedArtifactApplyPortMode
  rootBoundWriteAvailable: boolean
  atomicReplaceAvailable: boolean
  rollbackCheckpointAvailable: boolean
  rollbackVerified: boolean
  productionRootDisabledByDefault: boolean
  testRootUsed: boolean
  legacyFallbackAvailable: boolean
  rollbackPlan: CanonicalRollbackPlan | null
  rollbackRehearsalPassed: boolean
  readSideParallelEquivalent: boolean
  canaryStageEvidence: CanonicalGeneratedArtifactCanaryStageEvidence | null

  constructor(
    noCommitEvidenceAvailable: boolean = false, realDataShadowCopyVerified: boolean = false,
    executionShadowVerified: boolean = false, dryRunEquivalenceVerified: boolean = false,
    noBlockingDivergence: boolean = false, noUnresolvedConflict: boolean = false,
    artifactRequestRouteEvidenceAvailable: boolean = false, productionPortAvailable: boolean = false,
    realRootBoundApplyPortAvailable: boolean = false,
    applyPortMode: CanonicalGeneratedArtifactApplyPortMode = CanonicalGeneratedArtifactApplyPortMode.DISABLED,
    rootBoundWriteAvailable: boolean = false, atomicReplaceAvailable: boolean = false,
    rollbackCheckpointAvailable: boolean = false, rollbackVerified: boolean = false,
    productionRootDisabledByDefault: boolean = false, testRootUsed: boolean = false,
    legacyFallbackAvailable: boolean = false, rollbackPlan: CanonicalRollbackPlan | null = null,
    rollbackRehearsalPassed: boolean = false, readSideParallelEquivalent: boolean = false,
    canaryStageEvidence: CanonicalGeneratedArtifactCanaryStageEvidence | null = null
  ) {
    this.noCommitEvidenceAvailable = noCommitEvidenceAvailable
    this.realDataShadowCopyVerified = realDataShadowCopyVerified
    this.executionShadowVerified = executionShadowVerified
    this.dryRunEquivalenceVerified = dryRunEquivalenceVerified
    this.noBlockingDivergence = noBlockingDivergence
    this.noUnresolvedConflict = noUnresolvedConflict
    this.artifactRequestRouteEvidenceAvailable = artifactRequestRouteEvidenceAvailable
    this.productionPortAvailable = productionPortAvailable
    this.realRootBoundApplyPortAvailable = realRootBoundApplyPortAvailable
    this.applyPortMode = applyPortMode
    this.rootBoundWriteAvailable = rootBoundWriteAvailable
    this.atomicReplaceAvailable = atomicReplaceAvailable
    this.rollbackCheckpointAvailable = rollbackCheckpointAvailable
    this.rollbackVerified = rollbackVerified
    this.productionRootDisabledByDefault = productionRootDisabledByDefault
    this.testRootUsed = testRootUsed
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.rollbackPlan = rollbackPlan
    this.rollbackRehearsalPassed = rollbackRehearsalPassed
    this.readSideParallelEquivalent = readSideParallelEquivalent
    this.canaryStageEvidence = canaryStageEvidence
  }

  static passing(rollbackPlan: CanonicalRollbackPlan): CanonicalGeneratedArtifactCutoverEvidence {
    return new CanonicalGeneratedArtifactCutoverEvidence(true, true, true, true, true, true, true, true, true,
      CanonicalGeneratedArtifactApplyPortMode.TEST_ROOT_BOUND, true, true, true, true, true, true, true,
      rollbackPlan, true, true, null)
  }
}

export class CanonicalGeneratedArtifactCanaryStagePolicy {
  requestedStage: CanonicalGeneratedArtifactCanaryStage
  allowCandidateExecution: boolean
  runtimeSwitchEnabled: boolean

  constructor(
    requestedStage: CanonicalGeneratedArtifactCanaryStage = CanonicalGeneratedArtifactCanaryStage.DISABLED,
    allowCandidateExecution: boolean = false, runtimeSwitchEnabled: boolean = false
  ) {
    this.requestedStage = requestedStage
    this.allowCandidateExecution = allowCandidateExecution
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
  }

  static get disabled(): CanonicalGeneratedArtifactCanaryStagePolicy {
    return new CanonicalGeneratedArtifactCanaryStagePolicy()
  }

  get canaryBudget(): number {
    return canaryStageBudget(this.requestedStage)
  }
}

export enum CanonicalGeneratedArtifactStageEvidenceStatus {
  MISSING = 'missing',
  INCOMPLETE = 'incomplete',
  PASSED = 'passed',
  FAILED = 'failed',
  BLOCKED = 'blocked'
}

export function stageEvidenceIsPassing(s: CanonicalGeneratedArtifactStageEvidenceStatus): boolean {
  return s === CanonicalGeneratedArtifactStageEvidenceStatus.PASSED
}

export enum CanonicalGeneratedArtifactStageEvidenceBlocker {
  STAGE_DISABLED = 'stageDisabled',
  UNSUPPORTED_DOMAIN = 'unsupportedDomain',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  CANDIDATE_EXECUTION_NOT_APPROVED = 'candidateExecutionNotApproved',
  PREVIOUS_STAGE_EVIDENCE_MISSING = 'previousStageEvidenceMissing',
  STAGE_ORDER_VIOLATION = 'stageOrderViolation',
  PREVIOUS_STAGE_INSUFFICIENT_SUCCESS = 'previousStageInsufficientSuccess',
  PREVIOUS_STAGE_FAILURE = 'previousStageFailure',
  PREVIOUS_STAGE_ROLLBACK_FAILURE = 'previousStageRollbackFailure',
  PREVIOUS_STAGE_BLOCKING_DIVERGENCE = 'previousStageBlockingDivergence',
  PREVIOUS_STAGE_UNRESOLVED_CONFLICT = 'previousStageUnresolvedConflict',
  PREVIOUS_STAGE_POSTCONDITION_FAILURE = 'previousStagePostconditionFailure',
  PREVIOUS_STAGE_UNSUPPORTED_ARTIFACT = 'previousStageUnsupportedArtifact',
  PREVIOUS_STAGE_CONTENT_LEAK_RISK = 'previousStageContentLeakRisk',
  PREVIOUS_STAGE_UNSAFE_PATH_TOKEN = 'previousStageUnsafePathToken',
  PREVIOUS_STAGE_PARENT_TOMBSTONE = 'previousStageParentTombstone',
  PREVIOUS_STAGE_AUDIO_CONFUSION = 'previousStageAudioConfusion',
  PREVIOUS_STAGE_HASH_UNAVAILABLE = 'previousStageHashUnavailable',
  PREVIOUS_STAGE_BYTE_SIZE_UNAVAILABLE = 'previousStageByteSizeUnavailable',
  OBSERVATION_WINDOW_INCOMPLETE = 'observationWindowIncomplete',
  NO_COMMIT_EVIDENCE_MISSING = 'noCommitEvidenceMissing',
  OWNER_APPROVAL_MISSING = 'ownerApprovalMissing',
  ROLLBACK_PLAN_MISSING = 'rollbackPlanMissing',
  DRY_RUN_EQUIVALENCE_MISSING = 'dryRunEquivalenceMissing',
  EXECUTION_SHADOW_MISSING = 'executionShadowMissing',
  REAL_DATA_SHADOW_COPY_MISSING = 'realDataShadowCopyMissing',
  READ_ONLY_TRANSPORT_PROBE_MISSING = 'readOnlyTransportProbeMissing',
  PRODUCTION_APPLY_PORT_UNAVAILABLE = 'productionApplyPortUnavailable',
  ARTIFACT_REQUEST_ROUTE_EVIDENCE_MISSING = 'artifactRequestRouteEvidenceMissing',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  READ_SIDE_PARALLEL_DIVERGENT = 'readSideParallelDivergent'
}

export class CanonicalGeneratedArtifactStageObservationWindow {
  observationWindowID: string
  complete: boolean

  constructor(observationWindowID: string = 'generated-artifact-stage-window', complete: boolean = false) {
    this.observationWindowID = CanonicalProductionRedaction.safeIdentifier(observationWindowID,
      'generated-artifact-stage-window')
    this.complete = complete
  }

  static completeWindow(id: string): CanonicalGeneratedArtifactStageObservationWindow {
    return new CanonicalGeneratedArtifactStageObservationWindow(id, true)
  }
}

export class CanonicalGeneratedArtifactCanaryStageEvidence {
  previousStage: CanonicalGeneratedArtifactCanaryStage
  requestedStage: CanonicalGeneratedArtifactCanaryStage
  previousStageSuccessCount: number
  previousStageFailureCount: number
  previousStageRollbackFailureCount: number
  previousStageBlockingDivergenceCount: number
  previousStageContentLeakRiskCount: number
  previousStageUnsafePathTokenCount: number
  previousStageParentTombstoneBlockCount: number
  previousStageAudioConfusionBlockCount: number
  previousStageSuppressedLegacyDuplicateCount: number
  previousStagePostconditionFailureCount: number
  previousStageUnsupportedArtifactCount: number
  previousStageHashUnavailableCount: number
  previousStageByteSizeUnavailableCount: number
  unresolvedConflictCount: number
  dryRunEquivalenceStatus: CanonicalGeneratedArtifactStageEvidenceStatus
  executionShadowStatus: CanonicalGeneratedArtifactStageEvidenceStatus
  realDataShadowCopyStatus: CanonicalGeneratedArtifactStageEvidenceStatus
  readOnlyTransportProbeStatus: CanonicalGeneratedArtifactStageEvidenceStatus
  noCommitEvidenceStatus: CanonicalGeneratedArtifactStageEvidenceStatus
  rollbackPlanStatus: CanonicalGeneratedArtifactStageEvidenceStatus
  productionApplyPortStatus: CanonicalGeneratedArtifactStageEvidenceStatus
  artifactRequestRouteEvidenceStatus: CanonicalGeneratedArtifactStageEvidenceStatus
  legacyFallbackStatus: CanonicalGeneratedArtifactStageEvidenceStatus
  readSideParallelStatus: CanonicalGeneratedArtifactStageEvidenceStatus
  observationWindowID: string
  observationWindowComplete: boolean
  ownerApproved: boolean

  constructor(
    previousStage: CanonicalGeneratedArtifactCanaryStage = CanonicalGeneratedArtifactCanaryStage.DISABLED,
    requestedStage: CanonicalGeneratedArtifactCanaryStage = CanonicalGeneratedArtifactCanaryStage.DISABLED,
    previousStageSuccessCount: number = 0, previousStageFailureCount: number = 0,
    previousStageRollbackFailureCount: number = 0, previousStageBlockingDivergenceCount: number = 0,
    previousStageContentLeakRiskCount: number = 0, previousStageUnsafePathTokenCount: number = 0,
    previousStageParentTombstoneBlockCount: number = 0, previousStageAudioConfusionBlockCount: number = 0,
    previousStageSuppressedLegacyDuplicateCount: number = 0,
    previousStagePostconditionFailureCount: number = 0,
    previousStageUnsupportedArtifactCount: number = 0, previousStageHashUnavailableCount: number = 0,
    previousStageByteSizeUnavailableCount: number = 0, unresolvedConflictCount: number = 0,
    dryRunEquivalenceStatus: CanonicalGeneratedArtifactStageEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING,
    executionShadowStatus: CanonicalGeneratedArtifactStageEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING,
    realDataShadowCopyStatus: CanonicalGeneratedArtifactStageEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING,
    readOnlyTransportProbeStatus: CanonicalGeneratedArtifactStageEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING,
    noCommitEvidenceStatus: CanonicalGeneratedArtifactStageEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING,
    rollbackPlanStatus: CanonicalGeneratedArtifactStageEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING,
    productionApplyPortStatus: CanonicalGeneratedArtifactStageEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING,
    artifactRequestRouteEvidenceStatus: CanonicalGeneratedArtifactStageEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING,
    legacyFallbackStatus: CanonicalGeneratedArtifactStageEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING,
    readSideParallelStatus: CanonicalGeneratedArtifactStageEvidenceStatus = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING,
    observationWindow: CanonicalGeneratedArtifactStageObservationWindow = new CanonicalGeneratedArtifactStageObservationWindow(),
    ownerApproved: boolean = false
  ) {
    this.previousStage = previousStage
    this.requestedStage = requestedStage
    this.previousStageSuccessCount = Math.max(0, previousStageSuccessCount)
    this.previousStageFailureCount = Math.max(0, previousStageFailureCount)
    this.previousStageRollbackFailureCount = Math.max(0, previousStageRollbackFailureCount)
    this.previousStageBlockingDivergenceCount = Math.max(0, previousStageBlockingDivergenceCount)
    this.previousStageContentLeakRiskCount = Math.max(0, previousStageContentLeakRiskCount)
    this.previousStageUnsafePathTokenCount = Math.max(0, previousStageUnsafePathTokenCount)
    this.previousStageParentTombstoneBlockCount = Math.max(0, previousStageParentTombstoneBlockCount)
    this.previousStageAudioConfusionBlockCount = Math.max(0, previousStageAudioConfusionBlockCount)
    this.previousStageSuppressedLegacyDuplicateCount = Math.max(0, previousStageSuppressedLegacyDuplicateCount)
    this.previousStagePostconditionFailureCount = Math.max(0, previousStagePostconditionFailureCount)
    this.previousStageUnsupportedArtifactCount = Math.max(0, previousStageUnsupportedArtifactCount)
    this.previousStageHashUnavailableCount = Math.max(0, previousStageHashUnavailableCount)
    this.previousStageByteSizeUnavailableCount = Math.max(0, previousStageByteSizeUnavailableCount)
    this.unresolvedConflictCount = Math.max(0, unresolvedConflictCount)
    this.dryRunEquivalenceStatus = dryRunEquivalenceStatus
    this.executionShadowStatus = executionShadowStatus
    this.realDataShadowCopyStatus = realDataShadowCopyStatus
    this.readOnlyTransportProbeStatus = readOnlyTransportProbeStatus
    this.noCommitEvidenceStatus = noCommitEvidenceStatus
    this.rollbackPlanStatus = rollbackPlanStatus
    this.productionApplyPortStatus = productionApplyPortStatus
    this.artifactRequestRouteEvidenceStatus = artifactRequestRouteEvidenceStatus
    this.legacyFallbackStatus = legacyFallbackStatus
    this.readSideParallelStatus = readSideParallelStatus
    this.observationWindowID = observationWindow.observationWindowID
    this.observationWindowComplete = observationWindow.complete
    this.ownerApproved = ownerApproved
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CANARY STAGE GATE
// ═══════════════════════════════════════════════════════════════════════════════

export class CanonicalGeneratedArtifactStageEvidenceReport {
  status: CanonicalGeneratedArtifactStageEvidenceStatus
  blockers: CanonicalGeneratedArtifactStageEvidenceBlocker[]
  previousStage: CanonicalGeneratedArtifactCanaryStage
  requestedStage: CanonicalGeneratedArtifactCanaryStage
  previousStageSuccessCount: number
  sensitiveFieldsRedacted: boolean

  constructor(
    evidence: CanonicalGeneratedArtifactCanaryStageEvidence | null,
    requestedStage: CanonicalGeneratedArtifactCanaryStage,
    blockers: CanonicalGeneratedArtifactStageEvidenceBlocker[]
  ) {
    const normalizedBlockers = [...new Set(blockers)].sort()
    if (!evidence) {
      this.status = CanonicalGeneratedArtifactStageEvidenceStatus.MISSING
    } else if (normalizedBlockers.length === 0) {
      this.status = CanonicalGeneratedArtifactStageEvidenceStatus.PASSED
    } else if (evidence.observationWindowComplete === false) {
      this.status = CanonicalGeneratedArtifactStageEvidenceStatus.INCOMPLETE
    } else {
      this.status = CanonicalGeneratedArtifactStageEvidenceStatus.BLOCKED
    }
    this.blockers = normalizedBlockers
    this.previousStage = evidence?.previousStage ?? canaryStagePrev(requestedStage) ??
      CanonicalGeneratedArtifactCanaryStage.DISABLED
    this.requestedStage = requestedStage
    this.previousStageSuccessCount = evidence?.previousStageSuccessCount ?? 0
    this.sensitiveFieldsRedacted = true
  }
}

export class CanonicalGeneratedArtifactCanaryStageGate {
  requestedStage: CanonicalGeneratedArtifactCanaryStage
  allowed: boolean
  selectedCandidateLimit: number
  selectsAllEligible: boolean
  blockers: CanonicalGeneratedArtifactStageEvidenceBlocker[]
  evidenceReport: CanonicalGeneratedArtifactStageEvidenceReport
  reason: string

  constructor(
    policy: CanonicalGeneratedArtifactCanaryStagePolicy,
    domain: CanonicalGeneratedArtifactCutoverDomain,
    token: CanonicalCutoverToken | null,
    cutoverEvidence: CanonicalGeneratedArtifactCutoverEvidence
  ) {
    const requestedStage = policy.requestedStage
    let blockers: CanonicalGeneratedArtifactStageEvidenceBlocker[] = []
    if (!canaryStageIsExecutable(requestedStage)) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.STAGE_DISABLED)
    }
    if (domain !== CanonicalGeneratedArtifactCutoverDomain.GENERATED_ARTIFACTS) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.UNSUPPORTED_DOMAIN)
    }
    if (policy.runtimeSwitchEnabled) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.RUNTIME_SWITCH_ENABLED)
    }
    if (!policy.allowCandidateExecution) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.CANDIDATE_EXECUTION_NOT_APPROVED)
    }
    if (token?.ownerApproved !== true && cutoverEvidence.canaryStageEvidence?.ownerApproved !== true) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.OWNER_APPROVAL_MISSING)
    }
    const evidence = cutoverEvidence.canaryStageEvidence
    if (!evidence) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_EVIDENCE_MISSING)
      this.requestedStage = requestedStage
      this.allowed = false
      this.selectedCandidateLimit = 0
      this.selectsAllEligible = false
      this.blockers = [...new Set(blockers)].sort()
      this.evidenceReport = new CanonicalGeneratedArtifactStageEvidenceReport(null, requestedStage, this.blockers)
      this.reason = 'generatedArtifactCanaryStageBlocked'
      return
    }
    if (evidence.requestedStage !== requestedStage ||
      evidence.previousStage !== (canaryStagePrev(requestedStage) ??
        CanonicalGeneratedArtifactCanaryStage.DISABLED)) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.STAGE_ORDER_VIOLATION)
    }
    if (evidence.previousStageSuccessCount < canaryStageMinPrevSuccess(requestedStage)) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_INSUFFICIENT_SUCCESS)
    }
    if (evidence.previousStageFailureCount > 0) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_FAILURE)
    }
    if (evidence.previousStageRollbackFailureCount > 0) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_ROLLBACK_FAILURE)
    }
    if (evidence.previousStageBlockingDivergenceCount > 0 || !cutoverEvidence.noBlockingDivergence) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_BLOCKING_DIVERGENCE)
    }
    if (evidence.unresolvedConflictCount > 0 || !cutoverEvidence.noUnresolvedConflict) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_UNRESOLVED_CONFLICT)
    }
    if (evidence.previousStagePostconditionFailureCount > 0) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_POSTCONDITION_FAILURE)
    }
    if (evidence.previousStageUnsupportedArtifactCount > 0) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_UNSUPPORTED_ARTIFACT)
    }
    if (evidence.previousStageContentLeakRiskCount > 0) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_CONTENT_LEAK_RISK)
    }
    if (evidence.previousStageUnsafePathTokenCount > 0) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_UNSAFE_PATH_TOKEN)
    }
    if (evidence.previousStageParentTombstoneBlockCount > 0) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_PARENT_TOMBSTONE)
    }
    if (evidence.previousStageAudioConfusionBlockCount > 0) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_AUDIO_CONFUSION)
    }
    if (evidence.previousStageHashUnavailableCount > 0) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_HASH_UNAVAILABLE)
    }
    if (evidence.previousStageByteSizeUnavailableCount > 0) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PREVIOUS_STAGE_BYTE_SIZE_UNAVAILABLE)
    }
    if (!evidence.observationWindowComplete) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.OBSERVATION_WINDOW_INCOMPLETE)
    }
    if (!stageEvidenceIsPassing(evidence.noCommitEvidenceStatus) || !cutoverEvidence.noCommitEvidenceAvailable) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.NO_COMMIT_EVIDENCE_MISSING)
    }
    if (!stageEvidenceIsPassing(evidence.dryRunEquivalenceStatus) || !cutoverEvidence.dryRunEquivalenceVerified) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.DRY_RUN_EQUIVALENCE_MISSING)
    }
    if (!stageEvidenceIsPassing(evidence.executionShadowStatus) || !cutoverEvidence.executionShadowVerified) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.EXECUTION_SHADOW_MISSING)
    }
    if (!stageEvidenceIsPassing(evidence.realDataShadowCopyStatus) || !cutoverEvidence.realDataShadowCopyVerified) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.REAL_DATA_SHADOW_COPY_MISSING)
    }
    if (!stageEvidenceIsPassing(evidence.readOnlyTransportProbeStatus) ||
      !cutoverEvidence.artifactRequestRouteEvidenceAvailable) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.READ_ONLY_TRANSPORT_PROBE_MISSING)
    }
    if (!stageEvidenceIsPassing(evidence.rollbackPlanStatus) ||
      cutoverEvidence.rollbackPlan?.covers(CanonicalProductionDomain.GENERATED_ARTIFACTS) !== true ||
      !cutoverEvidence.rollbackRehearsalPassed || !cutoverEvidence.rollbackVerified) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.ROLLBACK_PLAN_MISSING)
    }
    if (!stageEvidenceIsPassing(evidence.productionApplyPortStatus) || !cutoverEvidence.productionPortAvailable ||
      !cutoverEvidence.realRootBoundApplyPortAvailable ||
      !isNonDryRunRootBound(cutoverEvidence.applyPortMode) || !cutoverEvidence.rootBoundWriteAvailable ||
      !cutoverEvidence.atomicReplaceAvailable || !cutoverEvidence.rollbackCheckpointAvailable) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.PRODUCTION_APPLY_PORT_UNAVAILABLE)
    }
    if (!stageEvidenceIsPassing(evidence.artifactRequestRouteEvidenceStatus) ||
      !cutoverEvidence.artifactRequestRouteEvidenceAvailable) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.ARTIFACT_REQUEST_ROUTE_EVIDENCE_MISSING)
    }
    if (!stageEvidenceIsPassing(evidence.legacyFallbackStatus) || !cutoverEvidence.legacyFallbackAvailable) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.LEGACY_FALLBACK_UNAVAILABLE)
    }
    if (!stageEvidenceIsPassing(evidence.readSideParallelStatus) || !cutoverEvidence.readSideParallelEquivalent) {
      blockers.push(CanonicalGeneratedArtifactStageEvidenceBlocker.READ_SIDE_PARALLEL_DIVERGENT)
    }

    this.requestedStage = requestedStage
    this.allowed = blockers.length === 0
    this.selectedCandidateLimit = canaryStageBudget(requestedStage)
    this.selectsAllEligible = requestedStage === CanonicalGeneratedArtifactCanaryStage.ALL_ELIGIBLE
    this.blockers = [...new Set(blockers)].sort()
    this.evidenceReport = new CanonicalGeneratedArtifactStageEvidenceReport(evidence, requestedStage, this.blockers)
    this.reason = this.allowed ? 'generatedArtifactCanaryStageAllowed' : 'generatedArtifactCanaryStageBlocked'
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CANARY POLICY & SELECTOR
// ═══════════════════════════════════════════════════════════════════════════════

export class CanonicalGeneratedArtifactCanaryPolicy {
  stagePolicy: CanonicalGeneratedArtifactCanaryStagePolicy
  canaryMaxObjectsPerSyncRun: number
  allowsInternalN1Execution: boolean
  explicitInternalTestConfiguration: boolean
  runtimeSwitchEnabled: boolean
  allowAllEligible: boolean

  constructor(
    stagePolicy: CanonicalGeneratedArtifactCanaryStagePolicy = CanonicalGeneratedArtifactCanaryStagePolicy.disabled,
    canaryMaxObjectsPerSyncRun: number = 0, allowsInternalN1Execution: boolean = false,
    explicitInternalTestConfiguration: boolean = false, runtimeSwitchEnabled: boolean = false,
    allowAllEligible: boolean = false
  ) {
    this.stagePolicy = stagePolicy
    this.canaryMaxObjectsPerSyncRun = Math.max(0, canaryMaxObjectsPerSyncRun)
    this.allowsInternalN1Execution = allowsInternalN1Execution
    this.explicitInternalTestConfiguration = explicitInternalTestConfiguration
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.allowAllEligible = allowAllEligible
  }

  static get disabled(): CanonicalGeneratedArtifactCanaryPolicy {
    return new CanonicalGeneratedArtifactCanaryPolicy()
  }
}

export enum CanonicalGeneratedArtifactCanaryBlocker {
  DISABLED = 'disabled',
  UNSUPPORTED_MODE = 'unsupportedMode',
  CANARY_BUDGET_ZERO = 'canaryBudgetZero',
  MISSING_INTERNAL_CANARY_CONFIGURATION = 'missingInternalCanaryConfiguration',
  CANARY_BUDGET_ABOVE_ONE_DENIED = 'canaryBudgetAboveOneDenied',
  CANARY_STAGE_EVIDENCE_MISSING = 'canaryStageEvidenceMissing',
  CANARY_STAGE_BLOCKED = 'canaryStageBlocked',
  UNSUPPORTED_TRIGGER = 'unsupportedTrigger',
  UNSUPPORTED_ACTION = 'unsupportedAction',
  UNSUPPORTED_KIND = 'unsupportedKind',
  MISSING_OWNER_APPROVAL = 'missingOwnerApproval',
  MATRIX_BLOCKED = 'matrixBlocked',
  ACTIVE_PILOT_NOT_GENERATED_ARTIFACTS = 'activePilotNotGeneratedArtifacts',
  COMMIT_EXECUTOR_UNAVAILABLE = 'commitExecutorUnavailable',
  PEER_SNAPSHOT_UNAVAILABLE = 'peerSnapshotUnavailable',
  RUNTIME_SWITCH_DENIED = 'runtimeSwitchDenied',
  ALL_ELIGIBLE_DENIED = 'allEligibleDenied',
  DEFAULT_ENABLEMENT_DENIED = 'defaultEnablementDenied',
  READ_SIDE_PARALLEL_MISSING = 'readSideParallelMissing',
  HASH_UNAVAILABLE = 'hashUnavailable',
  BYTE_SIZE_UNAVAILABLE = 'byteSizeUnavailable',
  UNSAFE_LOGICAL_PATH_TOKEN = 'unsafeLogicalPathToken',
  CONTENT_LEAK_RISK = 'contentLeakRisk',
  AUDIO_CONFUSION_RISK = 'audioConfusionRisk',
  PRODUCER_AMBIGUOUS = 'producerAmbiguous',
  GENERATED_ARTIFACT_UPLOAD_DENIED = 'generatedArtifactUploadDenied',
  UNSUPPORTED_ROUTE = 'unsupportedRoute',
  ROLLBACK_CHECKPOINT_MISSING = 'rollbackCheckpointMissing',
  INSUFFICIENT_EVIDENCE = 'insufficientEvidence',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  PARENT_TOMBSTONED = 'parentTombstoned',
  NO_ROLLBACK_CHECKPOINT = 'noRollbackCheckpoint',
  REAL_APPLY_PORT_UNAVAILABLE = 'realApplyPortUnavailable',
  PEER_NOT_AUTHORITATIVE = 'peerNotAuthoritative',
  ALREADY_ATTEMPTED_FAILED_CANDIDATE = 'alreadyAttemptedFailedCandidate',
  NO_ELIGIBLE_CANDIDATE = 'noEligibleCandidate'
}

export class CanonicalGeneratedArtifactCanaryCandidate {
  cutoverCandidate: CanonicalGeneratedArtifactCutoverCandidate
  objectID: string
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null
  hashPrefix: string | null

  constructor(cutoverCandidate: CanonicalGeneratedArtifactCutoverCandidate) {
    this.cutoverCandidate = cutoverCandidate
    this.objectID = CanonicalProductionRedaction.safeIdentifier(cutoverCandidate.objectID, 'unknown-recording')
    this.artifactID = cutoverCandidate.artifactID ?
      CanonicalProductionRedaction.safeIdentifier(cutoverCandidate.artifactID, 'artifact:unknown') : null
    this.artifactKind = cutoverCandidate.artifactKind
    this.hashPrefix = cutoverCandidate.expectedContentHash ?
      CanonicalProductionRedaction.hashPrefix(cutoverCandidate.expectedContentHash.value) : null
  }

  get id(): string {
    return this.cutoverCandidate.action.actionID
  }
}

export class CanonicalGeneratedArtifactCanarySelectionBlocker {
  objectID: string | null
  artifactID: string | null
  reason: CanonicalGeneratedArtifactCanaryBlocker

  constructor(objectID: string | null, artifactID: string | null, reason: CanonicalGeneratedArtifactCanaryBlocker) {
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording') : null
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.reason = reason
  }

  get id(): string {
    return [(this.objectID ?? 'run'), (this.artifactID ?? 'artifact'), this.reason].join('|')
  }
}

export class CanonicalGeneratedArtifactCanarySelectionResult {
  selectedCandidates: CanonicalGeneratedArtifactCanaryCandidate[]
  blockers: CanonicalGeneratedArtifactCanarySelectionBlocker[]
  evaluatedCandidateCount: number
  noEligibleCandidate: boolean

  constructor(
    selectedCandidates: CanonicalGeneratedArtifactCanaryCandidate[],
    blockers: CanonicalGeneratedArtifactCanarySelectionBlocker[], evaluatedCandidateCount: number,
    noEligibleCandidate: boolean
  ) {
    this.selectedCandidates = selectedCandidates
    this.blockers = blockers
    this.evaluatedCandidateCount = evaluatedCandidateCount
    this.noEligibleCandidate = noEligibleCandidate
  }

  get selectedCutoverCandidates(): CanonicalGeneratedArtifactCutoverCandidate[] {
    return this.selectedCandidates.map(
      (c: CanonicalGeneratedArtifactCanaryCandidate) => c.cutoverCandidate)
  }
}

export class CanonicalGeneratedArtifactCanarySelector {
  constructor() {}

  select(
    mode: CanonicalCutoverMode, policy: CanonicalGeneratedArtifactCanaryPolicy,
    trigger: CanonicalSyncPlanTrigger, evidence: CanonicalGeneratedArtifactCutoverEvidence,
    peerNode: CanonicalNode | null, candidates: CanonicalGeneratedArtifactCutoverCandidate[],
    attemptedFailedActionIDs: Set<string> = new Set()
  ): CanonicalGeneratedArtifactCanarySelectionResult {
    let blockers: CanonicalGeneratedArtifactCanarySelectionBlocker[] = []
    const usesStagePolicy = canaryStageIsExecutable(policy.stagePolicy.requestedStage)
    const stageGate = usesStagePolicy ? new CanonicalGeneratedArtifactCanaryStageGate(policy.stagePolicy,
      CanonicalGeneratedArtifactCutoverDomain.GENERATED_ARTIFACTS, null, evidence) : null

    if (mode === CanonicalCutoverMode.DISABLED) {
      blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
        CanonicalGeneratedArtifactCanaryBlocker.DISABLED))
    }
    if (mode !== CanonicalCutoverMode.CANARY) {
      blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
        CanonicalGeneratedArtifactCanaryBlocker.UNSUPPORTED_MODE))
    }
    if (policy.canaryMaxObjectsPerSyncRun === 0 && !usesStagePolicy) {
      blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
        CanonicalGeneratedArtifactCanaryBlocker.CANARY_BUDGET_ZERO))
    }
    if (policy.canaryMaxObjectsPerSyncRun > 1 && !usesStagePolicy) {
      blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
        CanonicalGeneratedArtifactCanaryBlocker.CANARY_BUDGET_ABOVE_ONE_DENIED))
    }
    if (policy.canaryMaxObjectsPerSyncRun === 1 && !usesStagePolicy && !policy.allowsInternalN1Execution) {
      blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
        CanonicalGeneratedArtifactCanaryBlocker.MISSING_INTERNAL_CANARY_CONFIGURATION))
    }
    if (usesStagePolicy && policy.stagePolicy.requestedStage === CanonicalGeneratedArtifactCanaryStage.ALL_ELIGIBLE &&
      !policy.allowAllEligible) {
      blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
        CanonicalGeneratedArtifactCanaryBlocker.ALL_ELIGIBLE_DENIED))
    }
    if (policy.runtimeSwitchEnabled || policy.stagePolicy.runtimeSwitchEnabled) {
      blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
        CanonicalGeneratedArtifactCanaryBlocker.RUNTIME_SWITCH_DENIED))
    }
    if (usesStagePolicy && stageGate?.allowed !== true) {
      blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
        evidence.canaryStageEvidence === null ? CanonicalGeneratedArtifactCanaryBlocker.CANARY_STAGE_EVIDENCE_MISSING :
          CanonicalGeneratedArtifactCanaryBlocker.CANARY_STAGE_BLOCKED))
    }
    if (trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH ||
      trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) {
      blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
        CanonicalGeneratedArtifactCanaryBlocker.UNSUPPORTED_TRIGGER))
    }

    const runBlocked = blockers.length > 0
    const selectionLimit = usesStagePolicy ? (stageGate?.selectedCandidateLimit ?? 0) :
      policy.canaryMaxObjectsPerSyncRun
    const ordered = [...candidates].sort((lhs, rhs) => {
      const lp = CanonicalGeneratedArtifactCanarySelector.artifactSelectionPriority(lhs.artifactKind)
      const rp = CanonicalGeneratedArtifactCanarySelector.artifactSelectionPriority(rhs.artifactKind)
      if (lp !== rp) {
        return lp - rp
      }
      if (lhs.objectID !== rhs.objectID) {
        return lhs.objectID.localeCompare(rhs.objectID)
      }
      const la = lhs.artifactKind ?? ''
      const ra = rhs.artifactKind ?? ''
      if (la !== ra) {
        return la.localeCompare(ra)
      }
      return lhs.action.actionID.localeCompare(rhs.action.actionID)
    })
    let selected: CanonicalGeneratedArtifactCanaryCandidate[] = []
    for (const candidate of ordered) {
      const reasons = CanonicalGeneratedArtifactCanarySelector.candidateBlockers(candidate, evidence, peerNode,
        attemptedFailedActionIDs)
      if (reasons.length === 0 && !runBlocked && selected.length < selectionLimit) {
        selected.push(new CanonicalGeneratedArtifactCanaryCandidate(candidate))
        continue
      }
      for (const r of reasons) {
        blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(candidate.objectID,
          candidate.artifactID, r))
      }
    }
    if (selected.length === 0 && candidates.length > 0 && blockers.length === 0) {
      blockers.push(new CanonicalGeneratedArtifactCanarySelectionBlocker(null, null,
        CanonicalGeneratedArtifactCanaryBlocker.NO_ELIGIBLE_CANDIDATE))
    }
    return new CanonicalGeneratedArtifactCanarySelectionResult(selected, blockers, candidates.length,
      selected.length === 0)
  }

  static candidateBlockers(
    candidate: CanonicalGeneratedArtifactCutoverCandidate,
    evidence: CanonicalGeneratedArtifactCutoverEvidence, peerNode: CanonicalNode | null,
    attemptedFailedActionIDs: Set<string>
  ): CanonicalGeneratedArtifactCanaryBlocker[] {
    let blockers: CanonicalGeneratedArtifactCanaryBlocker[] = []
    if (!isExecutableApply(candidate.cutoverActionKind)) {
      blockers.push(candidate.cutoverActionKind ===
        CanonicalGeneratedArtifactCutoverActionKind.GENERATED_ARTIFACT_NO_OP ?
        CanonicalGeneratedArtifactCanaryBlocker.GENERATED_ARTIFACT_UPLOAD_DENIED :
        CanonicalGeneratedArtifactCanaryBlocker.UNSUPPORTED_ACTION)
    }
    if (candidate.action.kind !== CanonicalApplyActionKind.GENERATED_ARTIFACT_DOWNLOAD_APPLY) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.GENERATED_ARTIFACT_UPLOAD_DENIED)
    }
    if (candidate.artifactKind === CanonicalArtifactKind.AUDIO) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.AUDIO_CONFUSION_RISK)
    }
    if (!candidate.artifactKind || !CanonicalProjectionContract.generatedArtifactKinds.includes(candidate.artifactKind)) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.UNSUPPORTED_KIND)
    }
    if (!candidate.expectedArtifact) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.PRODUCER_AMBIGUOUS)
    }
    if (!candidate.expectedContentHash) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.HASH_UNAVAILABLE)
    }
    if (candidate.expectedByteSize === null) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.BYTE_SIZE_UNAVAILABLE)
    }
    if (!CanonicalProjectionContract.safeLogicalPathToken(candidate.expectedLogicalPathToken)) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.UNSAFE_LOGICAL_PATH_TOKEN)
    }
    if (candidate.unresolvedConflict) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.UNRESOLVED_CONFLICT)
    }
    if (candidate.parentObjectTombstoned) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.PARENT_TOMBSTONED)
    }
    if (!candidate.rollbackCheckpointID || !evidence.rollbackCheckpointAvailable) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.NO_ROLLBACK_CHECKPOINT)
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.ROLLBACK_CHECKPOINT_MISSING)
    }
    if (!evidence.realRootBoundApplyPortAvailable || !isNonDryRunRootBound(evidence.applyPortMode) ||
      !evidence.rootBoundWriteAvailable || !evidence.atomicReplaceAvailable) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.REAL_APPLY_PORT_UNAVAILABLE)
    }
    if (!candidate.peerIsAuthoritative(peerNode)) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.PEER_NOT_AUTHORITATIVE)
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.PRODUCER_AMBIGUOUS)
    }
    if (candidate.routePath !== '/sync/artifact-request') {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.UNSUPPORTED_ROUTE)
    }
    if (attemptedFailedActionIDs.has(candidate.action.actionID)) {
      blockers.push(CanonicalGeneratedArtifactCanaryBlocker.ALREADY_ATTEMPTED_FAILED_CANDIDATE)
    }
    return [...new Set(blockers)].sort()
  }

  private static artifactSelectionPriority(kind: CanonicalArtifactKind | null): number {
    switch (kind) {
      case CanonicalArtifactKind.SUMMARY_JSON: return 0
      case CanonicalArtifactKind.NOTE_JSON: return 1
      case CanonicalArtifactKind.NOTE_MARKDOWN: return 2
      case CanonicalArtifactKind.TRANSCRIPT_JSON: return 3
      case CanonicalArtifactKind.TRANSCRIPT_MARKDOWN: return 4
      case CanonicalArtifactKind.AUDIO: return 90
      default: return 99
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CUTOVER GATE & CUTOVER RESULT
// ═══════════════════════════════════════════════════════════════════════════════

export class CanonicalGeneratedArtifactCutoverGate {
  domain: CanonicalGeneratedArtifactCutoverDomain
  mode: CanonicalCutoverMode
  allowed: boolean
  failures: CanonicalGeneratedArtifactCutoverFailure[]
  legacyFallbackAvailable: boolean
  reason: string

  constructor(
    domain: CanonicalGeneratedArtifactCutoverDomain, mode: CanonicalCutoverMode,
    failures: CanonicalGeneratedArtifactCutoverFailure[], legacyFallbackAvailable: boolean, reason: string
  ) {
    this.domain = domain
    this.mode = mode
    this.failures = [...new Set(failures)].sort()
    this.allowed = this.failures.length === 0
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ??
      (this.allowed ? 'allowed' : 'blocked')
  }
}

export class CanonicalGeneratedArtifactProductionCommitResult {
  actionID: string
  objectID: string
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null
  actionKind: CanonicalGeneratedArtifactCutoverActionKind
  committed: boolean
  partialCommit: boolean
  preconditionVerified: boolean
  postconditionVerified: boolean
  sideEffects: CanonicalProductionSideEffect[]
  failureKind: CanonicalGeneratedArtifactCutoverFailure | null
  reason: string

  constructor(
    actionID: string, objectID: string, artifactID: string | null,
    artifactKind: CanonicalArtifactKind | null,
    actionKind: CanonicalGeneratedArtifactCutoverActionKind, committed: boolean,
    partialCommit: boolean = false, preconditionVerified: boolean = true,
    postconditionVerified: boolean = true, sideEffects: CanonicalProductionSideEffect[] = [],
    failureKind: CanonicalGeneratedArtifactCutoverFailure | null = null, reason: string
  ) {
    this.actionID = CanonicalProductionRedaction.safeIdentifier(actionID, actionKind)
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.artifactKind = artifactKind
    this.actionKind = actionKind
    this.committed = committed
    this.partialCommit = partialCommit
    this.preconditionVerified = preconditionVerified
    this.postconditionVerified = postconditionVerified
    this.sideEffects = sideEffects
    this.failureKind = failureKind
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? (committed ? 'committed' : 'failed')
  }
}

export class CanonicalGeneratedArtifactCutoverResult {
  gate: CanonicalGeneratedArtifactCutoverGate
  commits: CanonicalGeneratedArtifactProductionCommitResult[]
  rollbackResults: any[]
  diagnostics: CanonicalGeneratedArtifactCutoverDiagnostic[]
  legacyFallbackUsed: boolean
  duplicateLegacySuppressedActionIDs: string[]
  canaryAttemptedCount: number
  canarySucceeded: boolean
  fatalBlocker: boolean

  constructor(
    gate: CanonicalGeneratedArtifactCutoverGate,
    commits: CanonicalGeneratedArtifactProductionCommitResult[] = [],
    rollbackResults: any[] = [], diagnostics: CanonicalGeneratedArtifactCutoverDiagnostic[] = [],
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
        (c: CanonicalGeneratedArtifactProductionCommitResult) => c.committed && c.preconditionVerified &&
          c.postconditionVerified)
  }
}

export enum CanonicalGeneratedArtifactCutoverDiagnosticKind {
  CANONICAL_GENERATED_ARTIFACT_CUTOVER_GATE_EVALUATED = 'canonicalGeneratedArtifactCutoverGateEvaluated',
  CANONICAL_GENERATED_ARTIFACT_CUTOVER_GATE_BLOCKED = 'canonicalGeneratedArtifactCutoverGateBlocked',
  CANONICAL_GENERATED_ARTIFACT_CUTOVER_GATE_ALLOWED = 'canonicalGeneratedArtifactCutoverGateAllowed',
  CANONICAL_GENERATED_ARTIFACT_NO_COMMIT_STARTED = 'canonicalGeneratedArtifactNoCommitStarted',
  CANONICAL_GENERATED_ARTIFACT_NO_COMMIT_COMPLETED = 'canonicalGeneratedArtifactNoCommitCompleted',
  CANONICAL_GENERATED_ARTIFACT_COMMIT_STARTED = 'canonicalGeneratedArtifactCommitStarted',
  CANONICAL_GENERATED_ARTIFACT_COMMIT_COMPLETED = 'canonicalGeneratedArtifactCommitCompleted',
  CANONICAL_GENERATED_ARTIFACT_COMMIT_FAILED = 'canonicalGeneratedArtifactCommitFailed',
  CANONICAL_GENERATED_ARTIFACT_ROLLBACK_STARTED = 'canonicalGeneratedArtifactRollbackStarted',
  CANONICAL_GENERATED_ARTIFACT_ROLLBACK_COMPLETED = 'canonicalGeneratedArtifactRollbackCompleted',
  CANONICAL_GENERATED_ARTIFACT_ROLLBACK_FAILED = 'canonicalGeneratedArtifactRollbackFailed',
  CANONICAL_GENERATED_ARTIFACT_LEGACY_FALLBACK_USED = 'canonicalGeneratedArtifactLegacyFallbackUsed',
  CANONICAL_GENERATED_ARTIFACT_LEGACY_FALLBACK_PRESERVED = 'canonicalGeneratedArtifactLegacyFallbackPreserved'
}

export class CanonicalGeneratedArtifactCutoverDiagnostic {
  kind: CanonicalGeneratedArtifactCutoverDiagnosticKind
  syncRunID: string | null
  trigger: CanonicalSyncPlanTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  domain: CanonicalGeneratedArtifactCutoverDomain
  objectID: string | null
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null
  result: string | null
  reason: string | null
  hashPrefix: string | null

  constructor(
    kind: CanonicalGeneratedArtifactCutoverDiagnosticKind, syncRunID: string | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole,
    domain: CanonicalGeneratedArtifactCutoverDomain = CanonicalGeneratedArtifactCutoverDomain.GENERATED_ARTIFACTS,
    objectID: string | null = null, artifactID: string | null = null,
    artifactKind: CanonicalArtifactKind | null = null, result: string | null = null,
    reason: string | null = null, hash: CanonicalHash | null = null
  ) {
    this.kind = kind
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.domain = domain
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording') : null
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.artifactKind = artifactKind
    this.result = CanonicalProductionRedaction.safeDiagnosticText(result)
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason)
    this.hashPrefix = hash ? CanonicalProductionRedaction.hashPrefix(hash.value) : null
  }

  get id(): string {
    return [this.kind, this.objectID ?? 'run', this.artifactID ?? '', this.result ?? '', this.reason ?? ''].join('|')
  }
}

export class CanonicalGeneratedArtifactCutoverRunner {
  constructor() {}

  evaluateGate(
    mode: CanonicalCutoverMode, policy: CanonicalGeneratedArtifactCanaryPolicy,
    token: CanonicalCutoverToken | null, evidence: CanonicalGeneratedArtifactCutoverEvidence,
    candidates: CanonicalGeneratedArtifactCutoverCandidate[], peerNode: CanonicalNode | null,
    trigger: CanonicalSyncPlanTrigger
  ): CanonicalGeneratedArtifactCutoverGate {
    let failures: CanonicalGeneratedArtifactCutoverFailure[] = []
    if (mode === CanonicalCutoverMode.DISABLED) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.DISABLED)
    }
    if (mode !== CanonicalCutoverMode.CANARY && mode !== CanonicalCutoverMode.GUARDED_EXECUTE_COMMIT) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.UNSUPPORTED_MODE)
    }
    if (!token) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.MISSING_TOKEN)
    }
    if (token?.ownerApproved !== true) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.MISSING_OWNER_APPROVAL)
    }
    if (evidence.rollbackPlan?.covers(CanonicalProductionDomain.GENERATED_ARTIFACTS) !== true) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.MISSING_ROLLBACK)
    }
    if (!evidence.noCommitEvidenceAvailable) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.MISSING_NO_COMMIT_EVIDENCE)
    }
    if (!evidence.dryRunEquivalenceVerified) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.MISSING_DRY_RUN_EQUIVALENCE)
    }
    if (!evidence.executionShadowVerified) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.MISSING_EXECUTION_SHADOW_EVIDENCE)
    }
    if (!evidence.realDataShadowCopyVerified) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE)
    }
    if (!evidence.noBlockingDivergence) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.BLOCKING_DIVERGENCE)
    }
    if (!evidence.noUnresolvedConflict || candidates.some(
      (c: CanonicalGeneratedArtifactCutoverCandidate) => c.unresolvedConflict)) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.UNRESOLVED_CONFLICT)
    }
    if (!evidence.artifactRequestRouteEvidenceAvailable) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.MISSING_ARTIFACT_REQUEST_ROUTE_EVIDENCE)
    }
    if (!evidence.productionPortAvailable) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.PRODUCTION_PORT_UNAVAILABLE)
    }
    if (!evidence.legacyFallbackAvailable) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.TEST_ROOT_MISSING)
    }
    if (trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH ||
      trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.UNSUPPORTED_MODE)
    }
    if (candidates.some((c: CanonicalGeneratedArtifactCutoverCandidate) => !isExecutableApply(c.cutoverActionKind))) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.UNSUPPORTED_ACTION)
    }
    if (candidates.some(
      (c: CanonicalGeneratedArtifactCutoverCandidate) => c.artifactKind &&
        !CanonicalProjectionContract.generatedArtifactKinds.includes(c.artifactKind))) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.UNSUPPORTED_KIND)
    }
    if (candidates.some((c: CanonicalGeneratedArtifactCutoverCandidate) => c.parentObjectTombstoned)) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.PARENT_TOMBSTONED)
    }
    if (candidates.some(
      (c: CanonicalGeneratedArtifactCutoverCandidate) => !c.peerIsAuthoritative(peerNode))) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.PEER_NOT_AUTHORITATIVE)
    }
    if (candidates.some(
      (c: CanonicalGeneratedArtifactCutoverCandidate) => !c.expectedContentHash)) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.EXPECTED_HASH_MISSING)
    }
    if (candidates.some(
      (c: CanonicalGeneratedArtifactCutoverCandidate) => c.expectedByteSize === null)) {
      failures.push(CanonicalGeneratedArtifactCutoverFailure.EXPECTED_BYTE_SIZE_MISSING)
    }
    return new CanonicalGeneratedArtifactCutoverGate(
      CanonicalGeneratedArtifactCutoverDomain.GENERATED_ARTIFACTS, mode, failures,
      evidence.legacyFallbackAvailable,
      failures.length === 0 ? 'generatedArtifactCutoverGateAllowed' : 'generatedArtifactCutoverGateBlocked')
  }
}

export interface CanonicalGeneratedArtifactCutoverExecutor {
  commitGeneratedArtifact(candidate: CanonicalGeneratedArtifactCutoverCandidate):
    Promise<CanonicalGeneratedArtifactProductionCommitResult>
}
