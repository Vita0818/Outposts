/**
 * CanonicalGeneratedArtifactGuardedCommit — guarded commit seam for generated artifacts.
 * Ported from Rokurics-Apple (CanonicalGeneratedArtifactGuardedCommit.swift).
 * V822: canary budget is zero; no execution, no download, no apply.
 */

import {
  CanonicalHash, CanonicalNode, CanonicalArtifact, CanonicalArtifactKind,
  CanonicalRecordingObject, CanonicalManifest, generatedArtifactKinds, safeLogicalPathToken
} from './CanonicalCore'
import {
  CanonicalCutoverMode, CanonicalCutoverToken, CanonicalCutoverAppSeamMode,
  CanonicalSyncPlanTrigger
} from './CanonicalAudioUploadCutover'
import {
  CanonicalGeneratedArtifactCutoverCandidate, CanonicalGeneratedArtifactCutoverEvidence,
  CanonicalGeneratedArtifactCanaryPolicy, CanonicalGeneratedArtifactCanaryStage,
  CanonicalGeneratedArtifactCanaryStagePolicy, CanonicalGeneratedArtifactStageEvidenceStatus,
  CanonicalGeneratedArtifactStageEvidenceBlocker, CanonicalGeneratedArtifactStageObservationWindow,
  CanonicalGeneratedArtifactCanaryStageEvidence, canaryStageIsExecutable,
  CanonicalProductionExecutionDomainRole, CanonicalProductionDomain, CanonicalMigrationDomain,
  CanonicalMigrationDomainMatrix, CanonicalMigrationMatrixReport, CanonicalGeneratedArtifactTemplateReport,
  CanonicalApplyActionKind, CanonicalApplyAction
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

class CanonicalLegacyActionSnapshot {
  static empty = new CanonicalLegacyActionSnapshot()

  actionIDSetFor(domain: any): Set<string> {
    return new Set()
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS
// ═══════════════════════════════════════════════════════════════════════════════

export enum CanonicalGeneratedArtifactPilotActivationBlocker {
  TEMPLATE_NOT_READY_FOR_NEXT_PILOT_N0 = 'templateNotReadyForNextPilotN0',
  LIBRARY_METADATA_OBSERVATION_MISSING = 'libraryMetadataObservationMissing',
  MATRIX_VALIDATION_BLOCKED = 'matrixValidationBlocked',
  ACTIVE_PILOT_NOT_GENERATED_ARTIFACTS = 'activePilotNotGeneratedArtifacts',
  CANARY_N0_NOT_REACHED = 'canaryN0NotReached',
  CANARY_N1_REACHED = 'canaryN1Reached',
  RELEASE_DEFAULT_CUTOVER_ENABLED = 'releaseDefaultCutoverEnabled',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  LEGACY_SUPPRESSION_ENABLED = 'legacySuppressionEnabled',
  READ_PATH_NOT_LEGACY = 'readPathNotLegacy',
  PRODUCTION_INJECTION_PRESENT = 'productionInjectionPresent'
}

export enum CanonicalGeneratedArtifactGuardedCommitEvidenceStatus {
  COMPLETE = 'complete',
  INCOMPLETE = 'incomplete'
}

export enum CanonicalGeneratedArtifactGateResult {
  BLOCKED = 'blocked',
  ALLOWED_BUT_CANARY_BUDGET_ZERO = 'allowedButCanaryBudgetZero',
  MISSING_EVIDENCE = 'missingEvidence',
  UNSUPPORTED_ARTIFACT_KIND = 'unsupportedArtifactKind',
  CONTENT_LEAK_BLOCKED = 'contentLeakBlocked',
  UNSAFE_PATH_BLOCKED = 'unsafePathBlocked',
  PARENT_TOMBSTONE_BLOCKED = 'parentTombstoneBlocked',
  AUDIO_CONFUSION_BLOCKED = 'audioConfusionBlocked',
  READY_FOR_N1_AFTER_AUDIT = 'readyForN1AfterAudit'
}

export enum CanonicalGeneratedArtifactGuardedCommitSeamFailure {
  DISABLED = 'disabled',
  UNSUPPORTED_MODE = 'unsupportedMode',
  PRODUCTION_EXECUTE_DENIED = 'productionExecuteDenied',
  VIEW_REFRESH_TRIGGER_DENIED = 'viewRefreshTriggerDenied',
  RETRY_DRAINER_FRESH_ARTIFACT_DENIED = 'retryDrainerFreshArtifactDenied',
  INSUFFICIENT_LOCAL_SNAPSHOT = 'insufficientLocalSnapshot',
  INSUFFICIENT_PEER_SNAPSHOT = 'insufficientPeerSnapshot',
  MATRIX_VALIDATION_BLOCKED = 'matrixValidationBlocked',
  ACTIVE_PILOT_NOT_GENERATED_ARTIFACTS = 'activePilotNotGeneratedArtifacts',
  MISSING_TOKEN = 'missingToken',
  MISSING_OWNER_APPROVAL = 'missingOwnerApproval',
  MISSING_NO_COMMIT_EVIDENCE = 'missingNoCommitEvidence',
  MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE = 'missingRealDataShadowCopyEvidence',
  MISSING_EXECUTION_SHADOW_EVIDENCE = 'missingExecutionShadowEvidence',
  MISSING_DRY_RUN_EQUIVALENCE = 'missingDryRunEquivalence',
  BLOCKING_DIVERGENCE = 'blockingDivergence',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  MISSING_ARTIFACT_REQUEST_ROUTE_EVIDENCE = 'missingArtifactRequestRouteEvidence',
  PRODUCTION_PORT_UNAVAILABLE = 'productionPortUnavailable',
  REAL_APPLY_PORT_UNAVAILABLE = 'realApplyPortUnavailable',
  APPLY_PORT_DRY_RUN_ONLY = 'applyPortDryRunOnly',
  ROOT_BOUND_WRITE_UNAVAILABLE = 'rootBoundWriteUnavailable',
  ATOMIC_REPLACE_UNAVAILABLE = 'atomicReplaceUnavailable',
  ROLLBACK_CHECKPOINT_UNAVAILABLE = 'rollbackCheckpointUnavailable',
  MISSING_ROLLBACK = 'missingRollback',
  ROLLBACK_VERIFICATION_MISSING = 'rollbackVerificationMissing',
  ROLLBACK_REHEARSAL_MISSING = 'rollbackRehearsalMissing',
  PRODUCTION_ROOT_ENABLED_BY_DEFAULT = 'productionRootEnabledByDefault',
  TEST_ROOT_MISSING = 'testRootMissing',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  COMMIT_EXECUTOR_UNAVAILABLE = 'commitExecutorUnavailable',
  MISSING_FAILURE_INJECTION_EVIDENCE = 'missingFailureInjectionEvidence',
  MISSING_READ_SIDE_PARALLEL = 'missingReadSideParallel',
  MISSING_OBSERVATION_EVIDENCE = 'missingObservationEvidence',
  CONTENT_LEAK_GUARD_MISSING = 'contentLeakGuardMissing',
  AUDIO_CONFUSION_GUARD_MISSING = 'audioConfusionGuardMissing',
  UNSUPPORTED_ACTION = 'unsupportedAction',
  UNSUPPORTED_ARTIFACT_KIND = 'unsupportedArtifactKind',
  CONTENT_LEAK_BLOCKED = 'contentLeakBlocked',
  UNSAFE_PATH_BLOCKED = 'unsafePathBlocked',
  PARENT_TOMBSTONE_BLOCKED = 'parentTombstoneBlocked',
  AUDIO_CONFUSION_BLOCKED = 'audioConfusionBlocked',
  PEER_UNKNOWN = 'peerUnknown',
  PEER_NOT_AUTHORITATIVE = 'peerNotAuthoritative',
  EXPECTED_HASH_MISSING = 'expectedHashMissing',
  EXPECTED_BYTE_SIZE_MISSING = 'expectedByteSizeMissing',
  CANARY_BUDGET_NON_ZERO_DENIED = 'canaryBudgetNonZeroDenied',
  INTERNAL_N1_EXECUTION_DENIED = 'internalN1ExecutionDenied',
  STAGE_POLICY_EXECUTION_DENIED = 'stagePolicyExecutionDenied',
  RUNTIME_SWITCH_DENIED = 'runtimeSwitchDenied'
}

export enum CanonicalGeneratedArtifactGuardedCommitDiagnosticKind {
  CANONICAL_GENERATED_ARTIFACT_V822_SEAM_STARTED = 'canonicalGeneratedArtifactV822SeamStarted',
  CANONICAL_GENERATED_ARTIFACT_V822_SEAM_COMPLETED = 'canonicalGeneratedArtifactV822SeamCompleted',
  CANONICAL_GENERATED_ARTIFACT_V822_SEAM_BLOCKED = 'canonicalGeneratedArtifactV822SeamBlocked',
  CANONICAL_GENERATED_ARTIFACT_V822_GATE_EVALUATED = 'canonicalGeneratedArtifactV822GateEvaluated',
  CANONICAL_GENERATED_ARTIFACT_V822_GATE_ALLOWED_BUDGET_ZERO = 'canonicalGeneratedArtifactV822GateAllowedBudgetZero',
  CANONICAL_GENERATED_ARTIFACT_V822_GATE_BLOCKED = 'canonicalGeneratedArtifactV822GateBlocked',
  CANONICAL_GENERATED_ARTIFACT_V822_CANARY_BUDGET_ZERO = 'canonicalGeneratedArtifactV822CanaryBudgetZero',
  CANONICAL_GENERATED_ARTIFACT_V822_COMMIT_NOT_EXECUTED = 'canonicalGeneratedArtifactV822CommitNotExecuted',
  CANONICAL_GENERATED_ARTIFACT_V822_DOWNLOAD_NOT_EXECUTED = 'canonicalGeneratedArtifactV822DownloadNotExecuted',
  CANONICAL_GENERATED_ARTIFACT_V822_APPLY_NOT_EXECUTED = 'canonicalGeneratedArtifactV822ApplyNotExecuted',
  CANONICAL_GENERATED_ARTIFACT_V822_LEGACY_FALLBACK_PRESERVED = 'canonicalGeneratedArtifactV822LegacyFallbackPreserved',
  CANONICAL_GENERATED_ARTIFACT_V822_DUPLICATE_SUPPRESSION_NOT_APPLIED = 'canonicalGeneratedArtifactV822DuplicateSuppressionNotApplied',
  CANONICAL_GENERATED_ARTIFACT_V822_EVIDENCE_REPORT_BUILT = 'canonicalGeneratedArtifactV822EvidenceReportBuilt',
  CANONICAL_GENERATED_ARTIFACT_V822_N1_READINESS_REPORT_BUILT = 'canonicalGeneratedArtifactV822N1ReadinessReportBuilt',
  CANONICAL_GENERATED_ARTIFACT_CANARY_BUDGET_ZERO = 'canonicalGeneratedArtifactCanaryBudgetZero',
  CANONICAL_GENERATED_ARTIFACT_GATE_ALLOWED_BUT_NO_EXECUTION = 'canonicalGeneratedArtifactGateAllowedButNoExecution',
  CANONICAL_GENERATED_ARTIFACT_COMMIT_SKIPPED_BECAUSE_CANARY_BUDGET_ZERO = 'canonicalGeneratedArtifactCommitSkippedBecauseCanaryBudgetZero',
  CANONICAL_GENERATED_ARTIFACT_DOWNLOAD_SKIPPED_BECAUSE_CANARY_BUDGET_ZERO = 'canonicalGeneratedArtifactDownloadSkippedBecauseCanaryBudgetZero',
  CANONICAL_GENERATED_ARTIFACT_APPLY_SKIPPED_BECAUSE_CANARY_BUDGET_ZERO = 'canonicalGeneratedArtifactApplySkippedBecauseCanaryBudgetZero'
}

export enum CanonicalGeneratedArtifactN1ReadinessStatus {
  READY_FOR_N1_AFTER_AUDIT = 'readyForN1AfterAudit',
  NO_ELIGIBLE_CANDIDATE = 'noEligibleCandidate',
  INSUFFICIENT_PEER_SNAPSHOT = 'insufficientPeerSnapshot',
  INSUFFICIENT_EVIDENCE = 'insufficientEvidence',
  BLOCKED = 'blocked'
}

export enum CanonicalGeneratedArtifactNoExecutionViolation {
  WILL_EXECUTE_NOW = 'willExecuteNow',
  COMMIT_ATTEMPTED = 'commitAttempted',
  DOWNLOAD_ATTEMPTED = 'downloadAttempted',
  APPLY_ATTEMPTED = 'applyAttempted',
  COMMITTED_ARTIFACT = 'committedArtifact',
  PRODUCTION_COMMIT_CALLED = 'productionCommitCalled',
  REAL_APPLY_PORT_COMMIT_CALLED = 'realApplyPortCommitCalled',
  NETWORK_REQUEST_CALLED = 'networkRequestCalled',
  ARTIFACT_REQUEST_ROUTE_CALLED = 'artifactRequestRouteCalled',
  GENERATED_ARTIFACT_DOWNLOADED = 'generatedArtifactDownloaded',
  GENERATED_ARTIFACT_APPLIED = 'generatedArtifactApplied',
  GENERATED_ARTIFACT_FILE_WRITTEN = 'generatedArtifactFileWritten',
  GENERATED_ARTIFACT_UPLOAD_JOB_CREATED = 'generatedArtifactUploadJobCreated',
  AUDIO_AUTO_DOWNLOAD_TRIGGERED = 'audioAutoDownloadTriggered',
  DUPLICATE_LEGACY_SUPPRESSED = 'duplicateLegacySuppressed',
  LEGACY_FALLBACK_NOT_PRESERVED = 'legacyFallbackNotPreserved',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  LEGACY_PLAN_CHANGED = 'legacyPlanChanged',
  PRODUCTION_PLAN_CHANGED = 'productionPlanChanged'
}

// ═══════════════════════════════════════════════════════════════════════════════
// CLASSES
// ═══════════════════════════════════════════════════════════════════════════════

export class CanonicalGeneratedArtifactPilotActivationResult {
  activated: boolean
  matrix: CanonicalMigrationDomainMatrix
  matrixReport: CanonicalMigrationMatrixReport
  blockers: CanonicalGeneratedArtifactPilotActivationBlocker[]
  diagnosticsSummary: string

  constructor(matrix: CanonicalMigrationDomainMatrix,
    blockers: CanonicalGeneratedArtifactPilotActivationBlocker[]) {
    const matrixReport = matrix.validate()
    const normalizedBlockers = [...new Set(blockers)].sort()
    this.matrix = matrix
    this.matrixReport = matrixReport
    this.blockers = normalizedBlockers
    this.activated = normalizedBlockers.length === 0 && matrixReport.allowed &&
      matrixReport.activePilotDomain === CanonicalMigrationDomain.GENERATED_ARTIFACTS
    this.diagnosticsSummary = [
      'domain=generatedArtifacts', 'version=v8.22', 'activated=' + this.activated,
      'activePilot=' + (matrixReport.activePilotDomain ?? 'none'),
      'matrixAllowed=' + matrixReport.allowed,
      'blockers=' + normalizedBlockers.join('+')
    ].join(',')
  }
}

export class CanonicalGeneratedArtifactPilotActivationGate {
  constructor() {}

  evaluate(
    libraryMetadataObservationCompleteOrRetirementCandidateReady: boolean,
    templateReport: CanonicalGeneratedArtifactTemplateReport = CanonicalGeneratedArtifactTemplateReport.currentV821Audit()
  ): CanonicalGeneratedArtifactPilotActivationResult {
    const matrix = CanonicalMigrationDomainMatrix.v822GeneratedArtifactsActivePilot(
      libraryMetadataObservationCompleteOrRetirementCandidateReady, templateReport)
    const report = matrix.validate()
    const policy = matrix.policyFor(CanonicalMigrationDomain.GENERATED_ARTIFACTS)
    let blockers: CanonicalGeneratedArtifactPilotActivationBlocker[] = []
    if (!templateReport.readyForNextPilotN0) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.TEMPLATE_NOT_READY_FOR_NEXT_PILOT_N0)
    }
    if (!libraryMetadataObservationCompleteOrRetirementCandidateReady) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.LIBRARY_METADATA_OBSERVATION_MISSING)
    }
    if (!report.allowed) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.MATRIX_VALIDATION_BLOCKED)
    }
    if (report.activePilotDomain !== CanonicalMigrationDomain.GENERATED_ARTIFACTS) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.ACTIVE_PILOT_NOT_GENERATED_ARTIFACTS)
    }
    if (policy?.hasReached('canaryN0') !== true) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.CANARY_N0_NOT_REACHED)
    }
    if (policy?.hasReached('canaryN1') === true) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.CANARY_N1_REACHED)
    }
    if (policy?.defaultCutoverEnabled === true || policy?.releaseDefaultEnabledCutover === true) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.RELEASE_DEFAULT_CUTOVER_ENABLED)
    }
    if (policy?.runtimeSwitchEnabled === true) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.RUNTIME_SWITCH_ENABLED)
    }
    if (policy?.legacySuppressionAllowed === true) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.LEGACY_SUPPRESSION_ENABLED)
    }
    if (policy?.readPathLegacy !== true) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.READ_PATH_NOT_LEGACY)
    }
    if (policy?.noProductionInjection !== true) {
      blockers.push(CanonicalGeneratedArtifactPilotActivationBlocker.PRODUCTION_INJECTION_PRESENT)
    }
    return new CanonicalGeneratedArtifactPilotActivationResult(matrix, blockers)
  }
}

export class CanonicalGeneratedArtifactGuardedCommitGate {
  mode: CanonicalCutoverAppSeamMode
  allowed: boolean
  result: CanonicalGeneratedArtifactGateResult
  failures: CanonicalGeneratedArtifactGuardedCommitSeamFailure[]
  reason: string

  constructor(
    mode: CanonicalCutoverAppSeamMode, failures: CanonicalGeneratedArtifactGuardedCommitSeamFailure[],
    canaryBudgetZero: boolean, reason: string
  ) {
    this.mode = mode
    this.failures = [...new Set(failures)].sort()
    this.allowed = this.failures.length === 0
    this.result = CanonicalGeneratedArtifactGuardedCommitGate.gateResult(this.failures, this.allowed,
      canaryBudgetZero)
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? this.result
  }

  private static gateResult(
    failures: CanonicalGeneratedArtifactGuardedCommitSeamFailure[], allowed: boolean, canaryBudgetZero: boolean
  ): CanonicalGeneratedArtifactGateResult {
    if (allowed && canaryBudgetZero) {
      return CanonicalGeneratedArtifactGateResult.ALLOWED_BUT_CANARY_BUDGET_ZERO
    }
    if (failures.includes(CanonicalGeneratedArtifactGuardedCommitSeamFailure.AUDIO_CONFUSION_BLOCKED)) {
      return CanonicalGeneratedArtifactGateResult.AUDIO_CONFUSION_BLOCKED
    }
    if (failures.includes(CanonicalGeneratedArtifactGuardedCommitSeamFailure.PARENT_TOMBSTONE_BLOCKED)) {
      return CanonicalGeneratedArtifactGateResult.PARENT_TOMBSTONE_BLOCKED
    }
    if (failures.includes(CanonicalGeneratedArtifactGuardedCommitSeamFailure.UNSAFE_PATH_BLOCKED)) {
      return CanonicalGeneratedArtifactGateResult.UNSAFE_PATH_BLOCKED
    }
    if (failures.includes(CanonicalGeneratedArtifactGuardedCommitSeamFailure.CONTENT_LEAK_BLOCKED) ||
      failures.includes(CanonicalGeneratedArtifactGuardedCommitSeamFailure.CONTENT_LEAK_GUARD_MISSING)) {
      return CanonicalGeneratedArtifactGateResult.CONTENT_LEAK_BLOCKED
    }
    if (failures.includes(CanonicalGeneratedArtifactGuardedCommitSeamFailure.UNSUPPORTED_ARTIFACT_KIND)) {
      return CanonicalGeneratedArtifactGateResult.UNSUPPORTED_ARTIFACT_KIND
    }
    if (failures.some((f: CanonicalGeneratedArtifactGuardedCommitSeamFailure) => isEvidenceMissingForGuarded(f))) {
      return CanonicalGeneratedArtifactGateResult.MISSING_EVIDENCE
    }
    return CanonicalGeneratedArtifactGateResult.BLOCKED
  }
}

function isEvidenceMissingForGuarded(f: CanonicalGeneratedArtifactGuardedCommitSeamFailure): boolean {
  return [
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_NO_COMMIT_EVIDENCE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_EXECUTION_SHADOW_EVIDENCE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_DRY_RUN_EQUIVALENCE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_ARTIFACT_REQUEST_ROUTE_EVIDENCE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.PRODUCTION_PORT_UNAVAILABLE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.REAL_APPLY_PORT_UNAVAILABLE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.APPLY_PORT_DRY_RUN_ONLY,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.ROOT_BOUND_WRITE_UNAVAILABLE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.ATOMIC_REPLACE_UNAVAILABLE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.ROLLBACK_CHECKPOINT_UNAVAILABLE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_ROLLBACK,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.ROLLBACK_VERIFICATION_MISSING,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.ROLLBACK_REHEARSAL_MISSING,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.COMMIT_EXECUTOR_UNAVAILABLE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_FAILURE_INJECTION_EVIDENCE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_READ_SIDE_PARALLEL,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_OBSERVATION_EVIDENCE,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.EXPECTED_HASH_MISSING,
    CanonicalGeneratedArtifactGuardedCommitSeamFailure.EXPECTED_BYTE_SIZE_MISSING
  ].includes(f)
}

export class CanonicalGeneratedArtifactGuardedCommitDiagnostic {
  kind: CanonicalGeneratedArtifactGuardedCommitDiagnosticKind
  syncRunID: string | null
  trigger: CanonicalSyncPlanTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  mode: CanonicalCutoverAppSeamMode
  objectID: string | null
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null
  candidateCount: number
  eligibleCandidateCount: number
  gateFailureCount: number
  canaryBudget: number
  commitAttemptedCount: number
  downloadAttemptedCount: number
  applyAttemptedCount: number
  duplicateSuppressionCandidateCount: number
  result: string | null
  reason: string | null
  hashPrefix: string | null

  constructor(
    kind: CanonicalGeneratedArtifactGuardedCommitDiagnosticKind, syncRunID: string | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole,
    mode: CanonicalCutoverAppSeamMode, objectID: string | null = null,
    artifactID: string | null = null, artifactKind: CanonicalArtifactKind | null = null,
    candidateCount: number, eligibleCandidateCount: number = 0, gateFailureCount: number = 0,
    canaryBudget: number, commitAttemptedCount: number = 0, downloadAttemptedCount: number = 0,
    applyAttemptedCount: number = 0, duplicateSuppressionCandidateCount: number = 0,
    result: string | null = null, reason: string | null = null, hash: CanonicalHash | null = null
  ) {
    this.kind = kind
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.mode = mode
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording') : null
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.artifactKind = artifactKind
    this.candidateCount = Math.max(0, candidateCount)
    this.eligibleCandidateCount = Math.max(0, eligibleCandidateCount)
    this.gateFailureCount = Math.max(0, gateFailureCount)
    this.canaryBudget = Math.max(0, canaryBudget)
    this.commitAttemptedCount = Math.max(0, commitAttemptedCount)
    this.downloadAttemptedCount = Math.max(0, downloadAttemptedCount)
    this.applyAttemptedCount = Math.max(0, applyAttemptedCount)
    this.duplicateSuppressionCandidateCount = Math.max(0, duplicateSuppressionCandidateCount)
    this.result = CanonicalProductionRedaction.safeDiagnosticText(result)
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason)
    this.hashPrefix = hash ? CanonicalProductionRedaction.hashPrefix(hash.value) : null
  }

  get id(): string {
    return [this.kind, this.objectID ?? 'run', this.artifactID ?? '', this.result ?? '', this.reason ?? ''].join('|')
  }
}

export class CanonicalGeneratedArtifactEvidenceReport {
  status: CanonicalGeneratedArtifactGuardedCommitEvidenceStatus
  missingReasons: CanonicalGeneratedArtifactGuardedCommitSeamFailure[]
  matrixReport: CanonicalMigrationMatrixReport
  canaryPolicy: CanonicalGeneratedArtifactCanaryPolicy
  localSnapshotAvailable: boolean
  peerSnapshotAvailable: boolean
  candidateCount: number
  eligibleCandidateCount: number
  legacyActionCandidateCount: number
  unresolvedConflictCount: number
  noCommitEvidenceAvailable: boolean
  legacyFallbackAvailable: boolean

  constructor(
    missingReasons: CanonicalGeneratedArtifactGuardedCommitSeamFailure[],
    matrixReport: CanonicalMigrationMatrixReport, canaryPolicy: CanonicalGeneratedArtifactCanaryPolicy,
    localSnapshotAvailable: boolean, peerSnapshotAvailable: boolean, candidateCount: number,
    eligibleCandidateCount: number, legacyActionCandidateCount: number, unresolvedConflictCount: number,
    noCommitEvidenceAvailable: boolean, legacyFallbackAvailable: boolean
  ) {
    const normalizedReasons = [...new Set(missingReasons)].sort()
    this.status = normalizedReasons.length === 0 ?
      CanonicalGeneratedArtifactGuardedCommitEvidenceStatus.COMPLETE :
      CanonicalGeneratedArtifactGuardedCommitEvidenceStatus.INCOMPLETE
    this.missingReasons = normalizedReasons
    this.matrixReport = matrixReport
    this.canaryPolicy = canaryPolicy
    this.localSnapshotAvailable = localSnapshotAvailable
    this.peerSnapshotAvailable = peerSnapshotAvailable
    this.candidateCount = Math.max(0, candidateCount)
    this.eligibleCandidateCount = Math.max(0, eligibleCandidateCount)
    this.legacyActionCandidateCount = Math.max(0, legacyActionCandidateCount)
    this.unresolvedConflictCount = Math.max(0, unresolvedConflictCount)
    this.noCommitEvidenceAvailable = noCommitEvidenceAvailable
    this.legacyFallbackAvailable = legacyFallbackAvailable
  }
}

export class CanonicalGeneratedArtifactNoExecutionAssertion {
  passed: boolean
  violations: CanonicalGeneratedArtifactNoExecutionViolation[]

  constructor(passed: boolean, violations: CanonicalGeneratedArtifactNoExecutionViolation[]) {
    this.passed = passed
    this.violations = [...new Set(violations)].sort()
  }

  static evaluate(
    result: CanonicalGeneratedArtifactGuardedCommitSeamResult
  ): CanonicalGeneratedArtifactNoExecutionAssertion {
    let violations: CanonicalGeneratedArtifactNoExecutionViolation[] = []
    if (result.willExecuteNow) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.WILL_EXECUTE_NOW)
    }
    if (result.commitAttemptedCount !== 0) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.COMMIT_ATTEMPTED)
    }
    if (result.downloadAttemptedCount !== 0) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.DOWNLOAD_ATTEMPTED)
    }
    if (result.applyAttemptedCount !== 0) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.APPLY_ATTEMPTED)
    }
    if (result.committedArtifactCount !== 0) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.COMMITTED_ARTIFACT)
    }
    if (result.productionCommitCalled) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.PRODUCTION_COMMIT_CALLED)
    }
    if (result.realApplyPortCommitCalled) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.REAL_APPLY_PORT_COMMIT_CALLED)
    }
    if (result.networkRequestCalled) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.NETWORK_REQUEST_CALLED)
    }
    if (result.artifactRequestRouteCalled) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.ARTIFACT_REQUEST_ROUTE_CALLED)
    }
    if (result.generatedArtifactDownloaded) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.GENERATED_ARTIFACT_DOWNLOADED)
    }
    if (result.generatedArtifactApplied) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.GENERATED_ARTIFACT_APPLIED)
    }
    if (result.generatedArtifactFileWritten) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.GENERATED_ARTIFACT_FILE_WRITTEN)
    }
    if (result.generatedArtifactUploadJobCreated) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.GENERATED_ARTIFACT_UPLOAD_JOB_CREATED)
    }
    if (result.audioAutoDownloadTriggered) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.AUDIO_AUTO_DOWNLOAD_TRIGGERED)
    }
    if (result.duplicateLegacySuppressedActionIDs.length > 0) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.DUPLICATE_LEGACY_SUPPRESSED)
    }
    if (!result.legacyFallbackPreserved) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.LEGACY_FALLBACK_NOT_PRESERVED)
    }
    if (result.runtimeSwitchEnabled) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.RUNTIME_SWITCH_ENABLED)
    }
    if (!result.legacyPlanUnchanged) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.LEGACY_PLAN_CHANGED)
    }
    if (!result.productionPlanUnchanged) {
      violations.push(CanonicalGeneratedArtifactNoExecutionViolation.PRODUCTION_PLAN_CHANGED)
    }
    const uniqueViolations = [...new Set(violations)].sort()
    return new CanonicalGeneratedArtifactNoExecutionAssertion(uniqueViolations.length === 0, uniqueViolations)
  }
}

export class CanonicalGeneratedArtifactGuardedCommitContext {
  syncRunID: string | null
  trigger: CanonicalSyncPlanTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  localManifest: CanonicalManifest | null
  peerManifest: CanonicalManifest | null
  legacyActionSnapshot: CanonicalLegacyActionSnapshot
  matrix: CanonicalMigrationDomainMatrix
  evidence: CanonicalGeneratedArtifactCutoverEvidence
  canaryPolicy: CanonicalGeneratedArtifactCanaryPolicy
  cutoverToken: CanonicalCutoverToken | null
  candidates: CanonicalGeneratedArtifactCutoverCandidate[]
  localSnapshotAvailable: boolean
  peerSnapshotAvailable: boolean
  unresolvedConflictCount: number
  commitExecutorReady: boolean
  legacyFallbackAvailable: boolean

  constructor(
    syncRunID: string | null, trigger: CanonicalSyncPlanTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole, localManifest: CanonicalManifest | null,
    peerManifest: CanonicalManifest | null,
    legacyActionSnapshot: CanonicalLegacyActionSnapshot = CanonicalLegacyActionSnapshot.empty,
    matrix: CanonicalMigrationDomainMatrix = CanonicalMigrationDomainMatrix.v822GeneratedArtifactsActivePilot(true),
    evidence: CanonicalGeneratedArtifactCutoverEvidence,
    canaryPolicy: CanonicalGeneratedArtifactCanaryPolicy = CanonicalGeneratedArtifactCanaryPolicy.disabled,
    cutoverToken: CanonicalCutoverToken | null = null,
    candidates: CanonicalGeneratedArtifactCutoverCandidate[] = [], localSnapshotAvailable: boolean,
    peerSnapshotAvailable: boolean, unresolvedConflictCount: number = 0,
    commitExecutorReady: boolean = true, legacyFallbackAvailable: boolean | null = null
  ) {
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.localManifest = localManifest
    this.peerManifest = peerManifest
    this.legacyActionSnapshot = legacyActionSnapshot
    this.matrix = matrix
    this.evidence = evidence
    this.canaryPolicy = canaryPolicy
    this.cutoverToken = cutoverToken
    this.candidates = candidates
    this.localSnapshotAvailable = localSnapshotAvailable
    this.peerSnapshotAvailable = peerSnapshotAvailable
    this.unresolvedConflictCount = Math.max(0, unresolvedConflictCount)
    this.commitExecutorReady = commitExecutorReady
    this.legacyFallbackAvailable = legacyFallbackAvailable ?? evidence.legacyFallbackAvailable
  }
}

export class CanonicalGeneratedArtifactGuardedCommitSeamResult {
  gate: CanonicalGeneratedArtifactGuardedCommitGate
  evidenceReport: CanonicalGeneratedArtifactEvidenceReport
  n1ReadinessReport: any
  diagnostics: CanonicalGeneratedArtifactGuardedCommitDiagnostic[]
  noExecutionAssertion: CanonicalGeneratedArtifactNoExecutionAssertion
  canaryBudgetZero: boolean
  canExecuteNow: boolean
  willExecuteNow: boolean
  commitAttemptedCount: number
  downloadAttemptedCount: number
  applyAttemptedCount: number
  committedArtifactCount: number
  productionCommitCalled: boolean
  realApplyPortCommitCalled: boolean
  networkRequestCalled: boolean
  artifactRequestRouteCalled: boolean
  generatedArtifactDownloaded: boolean
  generatedArtifactApplied: boolean
  generatedArtifactFileWritten: boolean
  generatedArtifactUploadJobCreated: boolean
  audioAutoDownloadTriggered: boolean
  duplicateLegacySuppressedActionIDs: string[]
  duplicateLegacySuppressionCandidates: string[]
  legacyFallbackPreserved: boolean
  runtimeSwitchEnabled: boolean
  legacyPlanUnchanged: boolean
  productionPlanUnchanged: boolean
  nonfatalFailureCount: number

  get succeeded(): boolean {
    return this.gate.allowed && this.canaryBudgetZero && !this.willExecuteNow && this.noExecutionAssertion.passed
  }
}

export class CanonicalGeneratedArtifactCutoverAppSeamConfiguration {
  isEnabled: boolean
  mode: CanonicalCutoverAppSeamMode
  policy: CanonicalGeneratedArtifactGuardedCommitPolicy
  evidence: CanonicalGeneratedArtifactCutoverEvidence

  constructor(
    isEnabled: boolean = false, mode: CanonicalCutoverAppSeamMode = CanonicalCutoverAppSeamMode.DISABLED,
    policy: CanonicalGeneratedArtifactGuardedCommitPolicy = new CanonicalGeneratedArtifactGuardedCommitPolicy(),
    evidence: CanonicalGeneratedArtifactCutoverEvidence = new CanonicalGeneratedArtifactCutoverEvidence()
  ) {
    this.isEnabled = isEnabled
    this.mode = isEnabled ? mode : CanonicalCutoverAppSeamMode.DISABLED
    this.policy = policy
    this.evidence = evidence
  }

  get effectiveMode(): CanonicalCutoverAppSeamMode {
    return this.isEnabled ? this.mode : CanonicalCutoverAppSeamMode.DISABLED
  }
}

export class CanonicalGeneratedArtifactGuardedCommitPolicy {
  recordDiagnostics: boolean
  maxDiagnosticsEvents: number
  canaryPolicy: CanonicalGeneratedArtifactCanaryPolicy

  constructor(
    recordDiagnostics: boolean = true, maxDiagnosticsEvents: number = 200,
    canaryPolicy: CanonicalGeneratedArtifactCanaryPolicy = CanonicalGeneratedArtifactCanaryPolicy.disabled
  ) {
    this.recordDiagnostics = recordDiagnostics
    this.maxDiagnosticsEvents = Math.max(1, maxDiagnosticsEvents)
    this.canaryPolicy = canaryPolicy
  }
}

export class CanonicalGeneratedArtifactGuardedCommitSeam {
  constructor() {}

  evaluate(
    configuration: CanonicalGeneratedArtifactCutoverAppSeamConfiguration,
    context: CanonicalGeneratedArtifactGuardedCommitContext
  ): CanonicalGeneratedArtifactGuardedCommitSeamResult {
    const canaryPolicy = configuration.policy.canaryPolicy
    const duplicateCandidates = this.duplicateSuppressionCandidates(context)
    const evidenceReport = this.makeEvidenceReport(context, canaryPolicy, duplicateCandidates)
    const canaryBudgetZero = CanonicalGeneratedArtifactGuardedCommitSeam.isCanaryBudgetZero(canaryPolicy)
    const gate = this.evaluateGate(configuration, context, evidenceReport, canaryPolicy, canaryBudgetZero)
    const eligibleCount = this.eligibleCandidateCount(context.candidates, context.peerManifest?.node ?? null)
    const canExecuteNow = gate.allowed
    const willExecuteNow = false
    const emptyAssertion = new CanonicalGeneratedArtifactNoExecutionAssertion(true, [])
    const preliminaryReadiness = this.makeN1ReadinessReport(context, evidenceReport, canaryPolicy,
      canExecuteNow, willExecuteNow, emptyAssertion.passed)
    let result: CanonicalGeneratedArtifactGuardedCommitSeamResult = {
      gate: gate,
      evidenceReport: evidenceReport,
      n1ReadinessReport: preliminaryReadiness,
      diagnostics: [],
      noExecutionAssertion: emptyAssertion,
      canaryBudgetZero: canaryBudgetZero,
      canExecuteNow: canExecuteNow,
      willExecuteNow: willExecuteNow,
      commitAttemptedCount: 0, downloadAttemptedCount: 0, applyAttemptedCount: 0,
      committedArtifactCount: 0, productionCommitCalled: false, realApplyPortCommitCalled: false,
      networkRequestCalled: false, artifactRequestRouteCalled: false,
      generatedArtifactDownloaded: false, generatedArtifactApplied: false,
      generatedArtifactFileWritten: false, generatedArtifactUploadJobCreated: false,
      audioAutoDownloadTriggered: false, duplicateLegacySuppressedActionIDs: [],
      duplicateLegacySuppressionCandidates: duplicateCandidates,
      legacyFallbackPreserved: true, runtimeSwitchEnabled: false,
      legacyPlanUnchanged: true, productionPlanUnchanged: true,
      nonfatalFailureCount: gate.failures.length,
      succeeded: false
    }
    const assertion = CanonicalGeneratedArtifactNoExecutionAssertion.evaluate(result)
    result.noExecutionAssertion = assertion
    result.n1ReadinessReport = this.makeN1ReadinessReport(context, evidenceReport, canaryPolicy,
      canExecuteNow, willExecuteNow, assertion.passed)
    result.diagnostics = this.baseDiagnostics(configuration, context, gate, evidenceReport,
      result.n1ReadinessReport, context.candidates.length, eligibleCount, duplicateCandidates.length,
      canaryPolicy.canaryMaxObjectsPerSyncRun, canaryBudgetZero, willExecuteNow)
    return result
  }

  private evaluateGate(
    configuration: CanonicalGeneratedArtifactCutoverAppSeamConfiguration,
    context: CanonicalGeneratedArtifactGuardedCommitContext,
    evidenceReport: CanonicalGeneratedArtifactEvidenceReport,
    canaryPolicy: CanonicalGeneratedArtifactCanaryPolicy, canaryBudgetZero: boolean
  ): CanonicalGeneratedArtifactGuardedCommitGate {
    let failures: CanonicalGeneratedArtifactGuardedCommitSeamFailure[] = []
    const mode = configuration.effectiveMode
    if (mode === CanonicalCutoverAppSeamMode.DISABLED) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.DISABLED)
    }
    if (mode === CanonicalCutoverAppSeamMode.GUARDED_EXECUTE_NO_COMMIT) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.UNSUPPORTED_MODE)
    }
    if (mode === CanonicalCutoverAppSeamMode.PRODUCTION_EXECUTE) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.PRODUCTION_EXECUTE_DENIED)
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.UNSUPPORTED_MODE)
    }
    if (context.trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.VIEW_REFRESH_TRIGGER_DENIED)
    }
    if (context.trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.RETRY_DRAINER_FRESH_ARTIFACT_DENIED)
    }
    if (!context.localSnapshotAvailable || !context.localManifest) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.INSUFFICIENT_LOCAL_SNAPSHOT)
    }
    if (!context.peerSnapshotAvailable || !context.peerManifest) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.INSUFFICIENT_PEER_SNAPSHOT)
    }
    if (!evidenceReport.matrixReport.allowed) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.MATRIX_VALIDATION_BLOCKED)
    }
    if (evidenceReport.matrixReport.activePilotDomain !== CanonicalMigrationDomain.GENERATED_ARTIFACTS) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.ACTIVE_PILOT_NOT_GENERATED_ARTIFACTS)
    }
    if (!context.cutoverToken) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_TOKEN)
    }
    if (context.cutoverToken?.ownerApproved !== true) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_OWNER_APPROVAL)
    }
    if (canaryPolicy.canaryMaxObjectsPerSyncRun > 0) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.CANARY_BUDGET_NON_ZERO_DENIED)
    }
    if (canaryPolicy.allowsInternalN1Execution) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.INTERNAL_N1_EXECUTION_DENIED)
    }
    if (canaryStageIsExecutable(canaryPolicy.stagePolicy.requestedStage) ||
      canaryPolicy.stagePolicy.allowCandidateExecution) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.STAGE_POLICY_EXECUTION_DENIED)
    }
    if (canaryPolicy.stagePolicy.runtimeSwitchEnabled) {
      failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.RUNTIME_SWITCH_DENIED)
    }
    failures.push(...evidenceReport.missingReasons)
    return new CanonicalGeneratedArtifactGuardedCommitGate(mode, failures, canaryBudgetZero,
      failures.length === 0 ? 'canonicalGeneratedArtifactV822GateAllowedBudgetZero' : failures.join(','))
  }

  private makeEvidenceReport(
    context: CanonicalGeneratedArtifactGuardedCommitContext,
    canaryPolicy: CanonicalGeneratedArtifactCanaryPolicy, duplicateCandidates: string[]
  ): CanonicalGeneratedArtifactEvidenceReport {
    const evidence = context.evidence
    const matrixReport = context.matrix.validate()
    const candidateFailures = this.candidateFailuresFunc(context.candidates, context.peerManifest?.node ?? null)
    let missing: CanonicalGeneratedArtifactGuardedCommitSeamFailure[] = []
    if (!evidence.noCommitEvidenceAvailable) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_NO_COMMIT_EVIDENCE)
    }
    if (!evidence.realDataShadowCopyVerified) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE)
    }
    if (!evidence.executionShadowVerified) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_EXECUTION_SHADOW_EVIDENCE)
    }
    if (!evidence.dryRunEquivalenceVerified) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_DRY_RUN_EQUIVALENCE)
    }
    if (!evidence.noBlockingDivergence) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.BLOCKING_DIVERGENCE)
    }
    if (!evidence.noUnresolvedConflict || context.unresolvedConflictCount > 0 ||
      context.candidates.some((c: CanonicalGeneratedArtifactCutoverCandidate) => c.unresolvedConflict)) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.UNRESOLVED_CONFLICT)
    }
    if (!evidence.artifactRequestRouteEvidenceAvailable) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.MISSING_ARTIFACT_REQUEST_ROUTE_EVIDENCE)
    }
    if (!evidence.productionPortAvailable) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.PRODUCTION_PORT_UNAVAILABLE)
    }
    if (!evidence.realRootBoundApplyPortAvailable) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.REAL_APPLY_PORT_UNAVAILABLE)
    }
    if (!context.commitExecutorReady) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.COMMIT_EXECUTOR_UNAVAILABLE)
    }
    if (!context.legacyFallbackAvailable) {
      missing.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.LEGACY_FALLBACK_UNAVAILABLE)
    }
    missing.push(...candidateFailures)
    return new CanonicalGeneratedArtifactEvidenceReport(missing, matrixReport, canaryPolicy,
      context.localSnapshotAvailable, context.peerSnapshotAvailable, context.candidates.length,
      this.eligibleCandidateCount(context.candidates, context.peerManifest?.node ?? null),
      duplicateCandidates.length, context.unresolvedConflictCount, evidence.noCommitEvidenceAvailable,
      context.legacyFallbackAvailable)
  }

  private makeN1ReadinessReport(
    context: CanonicalGeneratedArtifactGuardedCommitContext,
    evidenceReport: CanonicalGeneratedArtifactEvidenceReport,
    canaryPolicy: CanonicalGeneratedArtifactCanaryPolicy, canExecuteNow: boolean,
    willExecuteNow: boolean, noExecutionAssertionPassed: boolean
  ): any {
    return {
      status: CanonicalGeneratedArtifactN1ReadinessStatus.BLOCKED,
      gateResult: CanonicalGeneratedArtifactGateResult.BLOCKED,
      candidateCount: context.candidates.length,
      eligibleCandidateCount: this.eligibleCandidateCount(context.candidates, context.peerManifest?.node ?? null),
      canaryBudget: canaryPolicy.canaryMaxObjectsPerSyncRun,
      canExecuteNow: canExecuteNow, willExecuteNow: willExecuteNow,
      noExecutionAssertionPassed: noExecutionAssertionPassed
    }
  }

  private baseDiagnostics(
    configuration: CanonicalGeneratedArtifactCutoverAppSeamConfiguration,
    context: CanonicalGeneratedArtifactGuardedCommitContext,
    gate: CanonicalGeneratedArtifactGuardedCommitGate,
    evidenceReport: CanonicalGeneratedArtifactEvidenceReport, n1Report: any,
    candidateCount: number, eligibleCandidateCount: number,
    duplicateCount: number, canaryBudget: number, canaryBudgetZero: boolean, willExecuteNow: boolean
  ): CanonicalGeneratedArtifactGuardedCommitDiagnostic[] {
    let diagnostics: CanonicalGeneratedArtifactGuardedCommitDiagnostic[] = [
      new CanonicalGeneratedArtifactGuardedCommitDiagnostic(
        CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.CANONICAL_GENERATED_ARTIFACT_V822_SEAM_STARTED,
        context.syncRunID, context.trigger, context.nodeRole, configuration.effectiveMode,
        null, null, null, candidateCount, eligibleCandidateCount, gate.failures.length, canaryBudget,
        undefined, undefined, undefined, duplicateCount, gate.allowed ? 'allowed' : 'blocked', gate.reason),
      new CanonicalGeneratedArtifactGuardedCommitDiagnostic(
        CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.CANONICAL_GENERATED_ARTIFACT_V822_LEGACY_FALLBACK_PRESERVED,
        context.syncRunID, context.trigger, context.nodeRole, configuration.effectiveMode,
        null, null, null, candidateCount, eligibleCandidateCount, gate.failures.length, canaryBudget,
        undefined, undefined, undefined, duplicateCount, 'legacyFallbackPreserved',
        'v822DoesNotReplaceLegacyArtifactPlan'),
      new CanonicalGeneratedArtifactGuardedCommitDiagnostic(
        CanonicalGeneratedArtifactGuardedCommitDiagnosticKind.CANONICAL_GENERATED_ARTIFACT_V822_SEAM_COMPLETED,
        context.syncRunID, context.trigger, context.nodeRole, configuration.effectiveMode,
        null, null, null, candidateCount, eligibleCandidateCount, gate.failures.length, canaryBudget,
        undefined, undefined, undefined, duplicateCount, 'completed',
        gate.allowed ? 'nonfatalNoExecution' : 'nonfatalBlocked')
    ]
    return diagnostics
  }

  private candidateFailuresFunc(
    candidates: CanonicalGeneratedArtifactCutoverCandidate[], peerNode: CanonicalNode | null
  ): CanonicalGeneratedArtifactGuardedCommitSeamFailure[] {
    let failures: CanonicalGeneratedArtifactGuardedCommitSeamFailure[] = []
    for (const c of candidates) {
      if (!c.artifactKind) {
        failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.UNSUPPORTED_ARTIFACT_KIND)
      } else if (c.artifactKind === CanonicalArtifactKind.AUDIO) {
        failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.AUDIO_CONFUSION_BLOCKED)
      } else if (!generatedArtifactKinds().includes(c.artifactKind)) {
        failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.UNSUPPORTED_ARTIFACT_KIND)
      }
      if (!c.expectedContentHash) {
        failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.EXPECTED_HASH_MISSING)
      }
      if (c.expectedByteSize === null) {
        failures.push(CanonicalGeneratedArtifactGuardedCommitSeamFailure.EXPECTED_BYTE_SIZE_MISSING)
      }
    }
    return [...new Set(failures)].sort()
  }

  private eligibleCandidateCount(candidates: CanonicalGeneratedArtifactCutoverCandidate[],
    peerNode: CanonicalNode | null): number {
    return candidates.filter(
      (c: CanonicalGeneratedArtifactCutoverCandidate) => this.candidateFailuresFunc([c], peerNode).length === 0).length
  }

  private duplicateSuppressionCandidates(
    context: CanonicalGeneratedArtifactGuardedCommitContext
  ): string[] {
    const legacyIDs = context.legacyActionSnapshot.actionIDSetFor(
      CanonicalProductionDomain.GENERATED_ARTIFACTS)
    return context.candidates.map((c: CanonicalGeneratedArtifactCutoverCandidate) => c.action.actionID)
      .filter((id: string) => legacyIDs.has(id)).map(
      (id: string) => CanonicalProductionRedaction.safeDiagnosticText(id) ?? id).sort()
  }

  static isCanaryBudgetZero(policy: CanonicalGeneratedArtifactCanaryPolicy): boolean {
    return policy.canaryMaxObjectsPerSyncRun === 0 && !policy.allowsInternalN1Execution &&
      !canaryStageIsExecutable(policy.stagePolicy.requestedStage) &&
      !policy.stagePolicy.allowCandidateExecution && !policy.stagePolicy.runtimeSwitchEnabled
  }
}
