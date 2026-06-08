/**
 * CanonicalRecordingMetadataCutover — recording metadata cutover gateway & canary runner.
 * Ported from Rokurics-Apple (CanonicalRecordingMetadataCutover.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalManifest,
  CanonicalRecordingObject, CanonicalRecordingMetadata,
  nilIfEmpty, hashPrefix
} from './CanonicalCore'
import {
  CanonicalApplyAction, CanonicalApplyActionKind, CanonicalApplyTarget,
  CanonicalApplySource, CanonicalApplyBridgeHint
} from './CanonicalApplyPlan'
import { CanonicalSyncPlanTrigger } from './CanonicalLibrarySyncPlanner'
import {
  CanonicalProductionDomain,
  CanonicalProductionExecutionDomainRole,
  CanonicalCutoverMode
} from './CanonicalLibraryMetadataCutover'

// ── Redaction ──

class CanonicalProductionRedaction {
  static safeIdentifier(value: string, fallback: string): string { const t = value.trim(); return t.length === 0 ? fallback : t }
  static safeDiagnosticText(value: string | null | undefined): string | null { if (!value) return null; const t = value.trim(); return t.length === 0 ? null : t }
  static hashPrefix(value: string | null | undefined): string | null { if (!value) return null; return hashPrefix(value, 12) }
}

export interface CanonicalProjectionContract {
  safeLogicalPathToken(token: string | null | undefined): string | null
  static safeLogicalPathToken(token: string | null | undefined): string | null { return token ?? null }
}

// ── Cutover Domain ──

export enum CanonicalCutoverDomain {
  RECORDING_METADATA = 'recordingMetadata',
  RECORDING_AUDIO = 'recordingAudio',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  FOLDERS = 'folders',
  STUDY_ITEMS = 'studyItems',
  STANDALONE_NOTES = 'standaloneNotes',
  TOMBSTONES = 'tombstones',
  CONFLICTS = 'conflicts',
  APPLY = 'apply',
  FILE_RUNTIME = 'fileRuntime',
  TRANSPORT_RUNTIME = 'transportRuntime',
  UPLOAD_RUNTIME = 'uploadRuntime',
  OBJECT_PROJECTION = 'objectProjection',
  INVENTORY = 'inventory',
  UI_INTEGRATION = 'uiIntegration'
}

// ── Cutover Evidence ──

export enum CanonicalRecordingMetadataApplyPortMode {
  DISABLED = 'disabled',
  DRY_RUN = 'dryRun',
  TEST_ROOT_BOUND = 'testRootBound',
  PRODUCTION_ROOT_BOUND = 'productionRootBound'
}

export class CanonicalRecordingMetadataCutoverEvidence {
  realDataShadowCopyVerified: boolean = false
  executionShadowVerified: boolean = false
  dryRunEquivalenceVerified: boolean = false
  noBlockingDivergence: boolean = false
  noUnresolvedConflict: boolean = false
  readOnlyTransportProbePassed: boolean = false
  productionPortAvailable: boolean = false
  realRootBoundApplyPortAvailable: boolean = false
  applyPortMode: CanonicalRecordingMetadataApplyPortMode = CanonicalRecordingMetadataApplyPortMode.DISABLED
  rootBoundWriteAvailable: boolean = false
  atomicReplaceAvailable: boolean = false
  rollbackCheckpointAvailable: boolean = false
  rollbackVerified: boolean = false
  productionRootDisabledByDefault: boolean = false
  testRootUsed: boolean = false
  legacyFallbackAvailable: boolean = false
  rollbackPlan: CanonicalRecordingMetadataRollbackPlan | null = null
  rollbackRehearsalPassed: boolean = false
  productionExecutionGuardPassed: boolean = false
  uiParallelReadEquivalent: boolean = false
  canaryStageEvidence: CanonicalRecordingMetadataCanaryStageEvidence | null = null

  constructor(fields?: Partial<CanonicalRecordingMetadataCutoverEvidence>) { if (fields) Object.assign(this, fields) }

  static passing(rollbackPlan: CanonicalRecordingMetadataRollbackPlan): CanonicalRecordingMetadataCutoverEvidence {
    return new CanonicalRecordingMetadataCutoverEvidence({
      realDataShadowCopyVerified: true, executionShadowVerified: true, dryRunEquivalenceVerified: true,
      noBlockingDivergence: true, noUnresolvedConflict: true, readOnlyTransportProbePassed: true,
      productionPortAvailable: true, realRootBoundApplyPortAvailable: true,
      applyPortMode: CanonicalRecordingMetadataApplyPortMode.TEST_ROOT_BOUND,
      rootBoundWriteAvailable: true, atomicReplaceAvailable: true, rollbackCheckpointAvailable: true,
      rollbackVerified: true, productionRootDisabledByDefault: true, testRootUsed: true,
      legacyFallbackAvailable: true, rollbackPlan, rollbackRehearsalPassed: true,
      productionExecutionGuardPassed: true, uiParallelReadEquivalent: true
    })
  }
}

// ── Cutover Token ──

export class CanonicalCutoverToken {
  tokenID: string
  syncRunID: string
  ownerApproved: boolean

  constructor(tokenID: string, syncRunID: string, ownerApproved: boolean = false) {
    this.tokenID = CanonicalProductionRedaction.safeIdentifier(tokenID, 'cutover-token')
    this.syncRunID = CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run')
    this.ownerApproved = ownerApproved
  }
}

// ── Cutover Failure ──

export enum CanonicalCutoverFailure {
  DISABLED = 'disabled',
  UNSUPPORTED_DOMAIN = 'unsupportedDomain',
  MODE_NOT_EXECUTABLE = 'modeNotExecutable',
  MISSING_TOKEN = 'missingToken',
  MISSING_OWNER_APPROVAL = 'missingOwnerApproval',
  MISSING_ROLLBACK = 'missingRollback',
  MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE = 'missingRealDataShadowCopyEvidence',
  MISSING_EXECUTION_SHADOW_EVIDENCE = 'missingExecutionShadowEvidence',
  MISSING_DRY_RUN_EQUIVALENCE = 'missingDryRunEquivalence',
  BLOCKING_DIVERGENCE = 'blockingDivergence',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  MISSING_READ_ONLY_TRANSPORT_PROBE = 'missingReadOnlyTransportProbe',
  PRODUCTION_PORT_UNAVAILABLE = 'productionPortUnavailable',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  VIEW_REFRESH_TRIGGER_DENIED = 'viewRefreshTriggerDenied',
  RETRY_DRAINER_FRESH_METADATA_DENIED = 'retryDrainerFreshMetadataDenied',
  UNSUPPORTED_ACTION = 'unsupportedAction',
  UNSTABLE_METADATA_HASH = 'unstableMetadataHash',
  APPLY_PORT_DRY_RUN_ONLY = 'applyPortDryRunOnly',
  ROOT_BOUND_WRITE_UNAVAILABLE = 'rootBoundWriteUnavailable',
  ATOMIC_REPLACE_UNAVAILABLE = 'atomicReplaceUnavailable',
  ROLLBACK_CHECKPOINT_UNAVAILABLE = 'rollbackCheckpointUnavailable',
  ROLLBACK_VERIFICATION_MISSING = 'rollbackVerificationMissing',
  PRODUCTION_ROOT_ENABLED_BY_DEFAULT = 'productionRootEnabledByDefault',
  TEST_ROOT_MISSING = 'testRootMissing',
  MISSING_INTERNAL_CANARY_CONFIGURATION = 'missingInternalCanaryConfiguration',
  CANARY_BUDGET_ABOVE_ONE_DENIED = 'canaryBudgetAboveOneDenied',
  MISSING_CANARY_STAGE_EVIDENCE = 'missingCanaryStageEvidence',
  CANARY_STAGE_BLOCKED = 'canaryStageBlocked',
  CANARY_STAGE_ORDER_VIOLATION = 'canaryStageOrderViolation',
  OBSERVATION_WINDOW_INCOMPLETE = 'observationWindowIncomplete',
  RUNTIME_SWITCH_DENIED = 'runtimeSwitchDenied',
  UNSUPPORTED_OBJECT = 'unsupportedObject',
  PREVIOUS_STAGE_FAILURE = 'previousStageFailure',
  PREVIOUS_STAGE_ROLLBACK_FAILURE = 'previousStageRollbackFailure',
  PREVIOUS_STAGE_BLOCKING_DIVERGENCE = 'previousStageBlockingDivergence',
  PREVIOUS_STAGE_UNRESOLVED_CONFLICT = 'previousStageUnresolvedConflict',
  PRECONDITION_MISMATCH = 'preconditionMismatch',
  POSTCONDITION_MISMATCH = 'postconditionMismatch',
  TRANSPORT_FAILURE_BEFORE_SEND = 'transportFailureBeforeSend',
  APPLY_FAILURE_BEFORE_COMMIT = 'applyFailureBeforeCommit',
  APPLY_FAILURE_AFTER_PARTIAL_COMMIT = 'applyFailureAfterPartialCommit',
  ROLLBACK_FAILED = 'rollbackFailed'
}

// ── Cutover Gate ──

export class CanonicalCutoverGate {
  domain: CanonicalCutoverDomain
  mode: CanonicalCutoverMode
  allowed: boolean
  failures: CanonicalCutoverFailure[]
  legacyFallbackAvailable: boolean
  reason: string

  constructor(domain: CanonicalCutoverDomain, mode: CanonicalCutoverMode, failures: CanonicalCutoverFailure[], legacyFallbackAvailable: boolean, reason: string) {
    this.domain = domain
    this.mode = mode
    this.failures = [...new Set(failures)].sort((a, b) => a.localeCompare(b))
    this.allowed = this.failures.length === 0
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? (this.allowed ? 'allowed' : 'blocked')
  }
}

// ── Cutover Candidate ──

export enum CanonicalRecordingMetadataCutoverActionKind {
  APPLY = 'apply',
  SEND = 'send'
}

export class CanonicalRecordingMetadataCutoverCandidate {
  action: CanonicalApplyAction
  localObject: CanonicalRecordingObject | null
  peerObject: CanonicalRecordingObject | null
  rollbackCheckpointID: string | null
  unresolvedConflict: boolean

  constructor(action: CanonicalApplyAction, localObject: CanonicalRecordingObject | null, peerObject: CanonicalRecordingObject | null, rollbackCheckpointID: string | null = null, unresolvedConflict: boolean = false) {
    this.action = action
    this.localObject = localObject
    this.peerObject = peerObject
    this.rollbackCheckpointID = rollbackCheckpointID ? CanonicalProductionRedaction.safeIdentifier(rollbackCheckpointID, 'recording-metadata-checkpoint') : null
    this.unresolvedConflict = unresolvedConflict
  }

  get id(): string { return this.action.actionID }
  get objectID(): string { return this.action.target.objectID }
  get cutoverActionKind(): CanonicalRecordingMetadataCutoverActionKind | null {
    switch (this.action.kind) {
      case CanonicalApplyActionKind.RECORDING_METADATA_APPLY: return CanonicalRecordingMetadataCutoverActionKind.APPLY
      case CanonicalApplyActionKind.RECORDING_METADATA_SEND: return CanonicalRecordingMetadataCutoverActionKind.SEND
      default: return null
    }
  }
  get requiresNetworkSend(): boolean { return this.cutoverActionKind === CanonicalRecordingMetadataCutoverActionKind.SEND }
  get expectedObject(): CanonicalRecordingObject | null {
    switch (this.cutoverActionKind) {
      case CanonicalRecordingMetadataCutoverActionKind.APPLY: return this.peerObject
      case CanonicalRecordingMetadataCutoverActionKind.SEND: return this.localObject
      default: return null
    }
  }
  get stableMetadataHash(): CanonicalHash | null { return this.expectedObject?.metadataHash ?? null }
  get effectiveRollbackCheckpointID(): string { return this.rollbackCheckpointID ?? 'recording-metadata-cutover-' + this.objectID }
}

// ── Canary Stage ──

export enum CanonicalRecordingMetadataCanaryStage {
  DISABLED = 'disabled', N1 = 'n1', N3 = 'n3', N10 = 'n10', ALL_ELIGIBLE = 'allEligible'
}

export class CanonicalRecordingMetadataCanaryStagePolicy {
  requestedStage: CanonicalRecordingMetadataCanaryStage
  allowCandidateExecution: boolean
  runtimeSwitchEnabled: boolean

  constructor(requestedStage: CanonicalRecordingMetadataCanaryStage = CanonicalRecordingMetadataCanaryStage.DISABLED, allowCandidateExecution: boolean = false, runtimeSwitchEnabled: boolean = false) {
    this.requestedStage = requestedStage
    this.allowCandidateExecution = allowCandidateExecution
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
  }
  static readonly DISABLED = new CanonicalRecordingMetadataCanaryStagePolicy()
  get canaryBudget(): number { return recordingCanaryBudget(this.requestedStage) }
}

function recordingCanaryBudget(stage: CanonicalRecordingMetadataCanaryStage): number {
  switch (stage) { case CanonicalRecordingMetadataCanaryStage.DISABLED: return 0; case CanonicalRecordingMetadataCanaryStage.N1: return 1; case CanonicalRecordingMetadataCanaryStage.N3: return 3; case CanonicalRecordingMetadataCanaryStage.N10: return 10; case CanonicalRecordingMetadataCanaryStage.ALL_ELIGIBLE: return Number.MAX_SAFE_INTEGER }
}

function recordingPreviousStage(stage: CanonicalRecordingMetadataCanaryStage): CanonicalRecordingMetadataCanaryStage | null {
  switch (stage) { case CanonicalRecordingMetadataCanaryStage.DISABLED: return null; case CanonicalRecordingMetadataCanaryStage.N1: return CanonicalRecordingMetadataCanaryStage.DISABLED; case CanonicalRecordingMetadataCanaryStage.N3: return CanonicalRecordingMetadataCanaryStage.N1; case CanonicalRecordingMetadataCanaryStage.N10: return CanonicalRecordingMetadataCanaryStage.N3; case CanonicalRecordingMetadataCanaryStage.ALL_ELIGIBLE: return CanonicalRecordingMetadataCanaryStage.N10 }
}

// ── Stage Evidence ──

export enum CanonicalRecordingMetadataStageEvidenceStatus {
  MISSING = 'missing', INCOMPLETE = 'incomplete', PASSED = 'passed', FAILED = 'failed', BLOCKED = 'blocked'
}

export class CanonicalRecordingMetadataCanaryStageEvidence {
  previousStage: CanonicalRecordingMetadataCanaryStage = CanonicalRecordingMetadataCanaryStage.DISABLED
  requestedStage: CanonicalRecordingMetadataCanaryStage = CanonicalRecordingMetadataCanaryStage.DISABLED
  previousStageSuccessCount: number = 0
  previousStageFailureCount: number = 0
  previousStageRollbackFailureCount: number = 0
  previousStageBlockingDivergenceCount: number = 0
  previousStageSuppressedLegacyDuplicateCount: number = 0
  unresolvedConflictCount: number = 0
  previousStagePostconditionFailureCount: number = 0
  previousStageUnsupportedObjectCount: number = 0
  dryRunEquivalenceStatus: CanonicalRecordingMetadataStageEvidenceStatus = CanonicalRecordingMetadataStageEvidenceStatus.MISSING
  executionShadowStatus: CanonicalRecordingMetadataStageEvidenceStatus = CanonicalRecordingMetadataStageEvidenceStatus.MISSING
  realDataShadowCopyStatus: CanonicalRecordingMetadataStageEvidenceStatus = CanonicalRecordingMetadataStageEvidenceStatus.MISSING
  readOnlyTransportProbeStatus: CanonicalRecordingMetadataStageEvidenceStatus = CanonicalRecordingMetadataStageEvidenceStatus.MISSING
  rollbackPlanStatus: CanonicalRecordingMetadataStageEvidenceStatus = CanonicalRecordingMetadataStageEvidenceStatus.MISSING
  productionApplyPortStatus: CanonicalRecordingMetadataStageEvidenceStatus = CanonicalRecordingMetadataStageEvidenceStatus.MISSING
  legacyFallbackStatus: CanonicalRecordingMetadataStageEvidenceStatus = CanonicalRecordingMetadataStageEvidenceStatus.MISSING
  observationWindowID: string = ''
  observationWindowComplete: boolean = false
  ownerApproved: boolean = false

  constructor(fields?: Partial<CanonicalRecordingMetadataCanaryStageEvidence>) { if (fields) Object.assign(this, fields) }

  static passing(previousStage: CanonicalRecordingMetadataCanaryStage, requestedStage: CanonicalRecordingMetadataCanaryStage, previousStageSuccessCount: number, previousStageSuppressedLegacyDuplicateCount: number = 0, observationWindowID: string): CanonicalRecordingMetadataCanaryStageEvidence {
    return new CanonicalRecordingMetadataCanaryStageEvidence({
      previousStage, requestedStage, previousStageSuccessCount,
      previousStageSuppressedLegacyDuplicateCount,
      dryRunEquivalenceStatus: CanonicalRecordingMetadataStageEvidenceStatus.PASSED,
      executionShadowStatus: CanonicalRecordingMetadataStageEvidenceStatus.PASSED,
      realDataShadowCopyStatus: CanonicalRecordingMetadataStageEvidenceStatus.PASSED,
      readOnlyTransportProbeStatus: CanonicalRecordingMetadataStageEvidenceStatus.PASSED,
      rollbackPlanStatus: CanonicalRecordingMetadataStageEvidenceStatus.PASSED,
      productionApplyPortStatus: CanonicalRecordingMetadataStageEvidenceStatus.PASSED,
      legacyFallbackStatus: CanonicalRecordingMetadataStageEvidenceStatus.PASSED,
      observationWindowID, observationWindowComplete: true, ownerApproved: true
    })
  }
}

// ── Canary Stage Gate ──

export class CanonicalRecordingMetadataCanaryStageGate {
  requestedStage: CanonicalRecordingMetadataCanaryStage
  allowed: boolean
  selectedCandidateLimit: number
  blockers: string[] = []
  reason: string = ''

  constructor(policy: CanonicalRecordingMetadataCanaryStagePolicy, domain: CanonicalCutoverDomain, token: CanonicalCutoverToken | null, cutoverEvidence: CanonicalRecordingMetadataCutoverEvidence) {
    this.requestedStage = policy.requestedStage
    const stageEvidence = cutoverEvidence.canaryStageEvidence
    if (!policy.requestedStage.isExecutable) this.blockers.push('stageDisabled')
    if (domain !== CanonicalCutoverDomain.RECORDING_METADATA) this.blockers.push('unsupportedDomain')
    if (policy.runtimeSwitchEnabled) this.blockers.push('runtimeSwitchEnabled')
    if (!policy.allowCandidateExecution) this.blockers.push('candidateExecutionNotApproved')
    if (!stageEvidence) {
      this.blockers.push('previousStageEvidenceMissing')
      this.allowed = false
      this.selectedCandidateLimit = 0
      this.reason = 'recordingMetadataCanaryStageBlocked'
      return
    }
    if (stageEvidence.requestedStage !== policy.requestedStage) this.blockers.push('stageOrderViolation')
    if (stageEvidence.previousStageSuccessCount < recordingMinPrevSuccess(policy.requestedStage)) this.blockers.push('previousStageInsufficientSuccess')
    if (!stageEvidence.observationWindowComplete) this.blockers.push('observationWindowIncomplete')
    if (!stageEvidence.ownerApproved) this.blockers.push('ownerApprovalMissing')
    this.allowed = this.blockers.length === 0
    this.selectedCandidateLimit = recordingCanaryBudget(policy.requestedStage)
    this.reason = this.allowed ? 'recordingMetadataCanaryStageAllowed' : 'recordingMetadataCanaryStageBlocked'
  }
}

function recordingMinPrevSuccess(stage: CanonicalRecordingMetadataCanaryStage): number {
  switch (stage) { case CanonicalRecordingMetadataCanaryStage.DISABLED: case CanonicalRecordingMetadataCanaryStage.N1: return 0; case CanonicalRecordingMetadataCanaryStage.N3: return 1; case CanonicalRecordingMetadataCanaryStage.N10: return 3; case CanonicalRecordingMetadataCanaryStage.ALL_ELIGIBLE: return 10 }
}

Object.defineProperty(CanonicalRecordingMetadataCanaryStage.prototype, 'isExecutable', {
  get: function (this: CanonicalRecordingMetadataCanaryStage) { return this !== CanonicalRecordingMetadataCanaryStage.DISABLED }
})

// ── Rollback Plan (minimal interface) ──

export interface CanonicalRecordingMetadataRollbackPlan {
  checkpoints: string[]
  actions: string[]
  covers(domain: any): boolean
}

// ── Cutover Runner ──

export class CanonicalRecordingMetadataCutoverRunner {
  constructor() {}

  evaluateGate(configuration: any, token: CanonicalCutoverToken | null, evidence: CanonicalRecordingMetadataCutoverEvidence, candidates: CanonicalRecordingMetadataCutoverCandidate[], trigger: CanonicalSyncPlanTrigger): CanonicalCutoverGate {
    const failures: CanonicalCutoverFailure[] = []
    if (configuration.mode === CanonicalCutoverMode.DISABLED) failures.push(CanonicalCutoverFailure.DISABLED)
    if (configuration.domain !== CanonicalCutoverDomain.RECORDING_METADATA) failures.push(CanonicalCutoverFailure.UNSUPPORTED_DOMAIN)
    if (token === null) failures.push(CanonicalCutoverFailure.MISSING_TOKEN)
    if (token?.ownerApproved !== true) failures.push(CanonicalCutoverFailure.MISSING_OWNER_APPROVAL)
    if (evidence.rollbackPlan === null) failures.push(CanonicalCutoverFailure.MISSING_ROLLBACK)
    if (!evidence.realDataShadowCopyVerified) failures.push(CanonicalCutoverFailure.MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE)
    if (!evidence.executionShadowVerified) failures.push(CanonicalCutoverFailure.MISSING_EXECUTION_SHADOW_EVIDENCE)
    if (!evidence.dryRunEquivalenceVerified) failures.push(CanonicalCutoverFailure.MISSING_DRY_RUN_EQUIVALENCE)
    if (!evidence.noBlockingDivergence) failures.push(CanonicalCutoverFailure.BLOCKING_DIVERGENCE)
    if (!evidence.noUnresolvedConflict || candidates.some(c => c.unresolvedConflict)) failures.push(CanonicalCutoverFailure.UNRESOLVED_CONFLICT)
    if (!evidence.productionPortAvailable) failures.push(CanonicalCutoverFailure.PRODUCTION_PORT_UNAVAILABLE)
    if (!evidence.legacyFallbackAvailable) failures.push(CanonicalCutoverFailure.LEGACY_FALLBACK_UNAVAILABLE)
    if (trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH) failures.push(CanonicalCutoverFailure.VIEW_REFRESH_TRIGGER_DENIED)
    if (trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) failures.push(CanonicalCutoverFailure.RETRY_DRAINER_FRESH_METADATA_DENIED)
    return new CanonicalCutoverGate(CanonicalCutoverDomain.RECORDING_METADATA, CanonicalCutoverMode.CANARY, failures, evidence.legacyFallbackAvailable, failures.length === 0 ? 'recordingMetadataCutoverGateAllowed' : 'recordingMetadataCutoverGateBlocked')
  }
}

// ── Canary Selector ──

export class CanonicalRecordingMetadataCanarySelector {
  constructor() {}

  select(configuration: any, trigger: CanonicalSyncPlanTrigger, evidence: CanonicalRecordingMetadataCutoverEvidence, candidates: CanonicalRecordingMetadataCutoverCandidate[], attemptedFailedActionIDs: Set<string> = new Set()): any {
    const blockers: any[] = []
    if (configuration.mode === CanonicalCutoverMode.DISABLED) blockers.push({ objectID: null, actionKind: null, reason: 'disabled' })
    if (configuration.mode !== CanonicalCutoverMode.CANARY) blockers.push({ objectID: null, actionKind: null, reason: 'unsupportedMode' })
    if (configuration.domain !== CanonicalCutoverDomain.RECORDING_METADATA) blockers.push({ objectID: null, actionKind: null, reason: 'unsupportedDomain' })
    const stagePolicy = configuration.policy.effectiveRecordingMetadataCanaryStagePolicy
    if (stagePolicy.requestedStage.isExecutable) {
      const stageGate = new CanonicalRecordingMetadataCanaryStageGate(stagePolicy, configuration.domain, null, evidence)
      if (!stageGate.allowed) blockers.push({ objectID: null, actionKind: null, reason: 'canaryStageBlocked' })
    }
    const selectionLimit = stagePolicy.requestedStage.isExecutable ? recordingCanaryBudget(stagePolicy.requestedStage) : configuration.policy.canaryMaxObjectsPerSyncRun
    const selected: CanonicalRecordingMetadataCutoverCandidate[] = []
    const ordered = [...candidates].sort((a, b) => a.objectID.localeCompare(b.objectID))
    for (const candidate of ordered) {
      if (selected.length < selectionLimit && blockers.length === 0) {
        selected.push(candidate)
      }
    }
    return { selectedCandidates: selected, blockers, evaluatedCandidateCount: candidates.length, noEligibleCandidate: selected.length === 0, selectedCutoverCandidates: selected }
  }
}
