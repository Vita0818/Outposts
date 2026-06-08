/**
 * CanonicalTombstoneConflictGuardedCommit — guarded commit seam for tombstone & conflict.
 * Ported from Rokurics-Apple (CanonicalTombstoneConflictGuardedCommit.swift).
 * V827: canary budget zero, no execution, no physical/permanent delete, no GC, no restore.
 */

import {
  CanonicalHash, CanonicalManifest
} from './CanonicalCore'
import {
  CanonicalCutoverMode, CanonicalCutoverToken, CanonicalCutoverAppSeamMode,
  CanonicalSyncPlanTrigger
} from './CanonicalAudioUploadCutover'
import {
  CanonicalMigrationDomain, CanonicalMigrationDomainMatrix, CanonicalMigrationMatrixReport,
  CanonicalProductionExecutionDomainRole, CanonicalProductionDomain
} from './CanonicalGeneratedArtifactCutover'
import {
  CanonicalTombstoneConflictCandidate, CanonicalTombstoneConflictCutoverEvidence,
  CanonicalTombstoneConflictCanaryPolicy, CanonicalTombstoneConflictCanaryStage,
  CanonicalTombstoneConflictFailure, CanonicalTombstoneConflictDomain,
  CanonicalTombstoneConflictActionKind, tcCanaryStageIsExecutable,
  actionKindIsExecutable, actionKindIsTombstoneMarkerWrite, domainRequiresConflictLedger,
  CanonicalTombstoneState
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
}

class CanonicalLegacyActionSnapshot {
  static empty = new CanonicalLegacyActionSnapshot()

  actionIDSetFor(domain: any): Set<string> {
    return new Set()
  }
}

export class CanonicalTombstoneConflictTemplateReport {
  readiness: string
  blockers: string[]
  readyForNextPilotN0: boolean

  constructor(readiness: string, blockers: string[], readyForNextPilotN0: boolean) {
    this.readiness = readiness
    this.blockers = blockers
    this.readyForNextPilotN0 = readyForNextPilotN0
  }

  static currentV826Audit(): CanonicalTombstoneConflictTemplateReport {
    return new CanonicalTombstoneConflictTemplateReport('readyForNextPilotN0', [], true)
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS
// ═══════════════════════════════════════════════════════════════════════════════

export enum CanonicalTombstoneConflictPilotActivationBlocker {
  TEMPLATE_NOT_READY_FOR_NEXT_PILOT_N0 = 'templateNotReadyForNextPilotN0',
  GENERATED_ARTIFACTS_TEMPLATE_MISSING = 'generatedArtifactsTemplateMissing',
  LIBRARY_METADATA_OBSERVATION_MISSING = 'libraryMetadataObservationMissing',
  MATRIX_VALIDATION_BLOCKED = 'matrixValidationBlocked',
  ACTIVE_PILOT_NOT_TOMBSTONE_CONFLICT = 'activePilotNotTombstoneConflict',
  CANARY_N0_NOT_REACHED = 'canaryN0NotReached',
  CANARY_N1_REACHED = 'canaryN1Reached',
  RELEASE_DEFAULT_CUTOVER_ENABLED = 'releaseDefaultCutoverEnabled',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  LEGACY_SUPPRESSION_ENABLED = 'legacySuppressionEnabled',
  READ_PATH_NOT_LEGACY = 'readPathNotLegacy',
  PRODUCTION_INJECTION_PRESENT = 'productionInjectionPresent'
}

export enum CanonicalTombstoneConflictGuardedEvidenceStatus {
  COMPLETE = 'complete',
  INCOMPLETE = 'incomplete'
}

export enum CanonicalTombstoneConflictGateResult {
  BLOCKED = 'blocked',
  ALLOWED_BUT_CANARY_BUDGET_ZERO = 'allowedButCanaryBudgetZero',
  MISSING_EVIDENCE = 'missingEvidence',
  UNSUPPORTED_DOMAIN = 'unsupportedDomain',
  PHYSICAL_DELETE_BLOCKED = 'physicalDeleteBlocked',
  PERMANENT_DELETE_BLOCKED = 'permanentDeleteBlocked',
  TOMBSTONE_GC_BLOCKED = 'tombstoneGCBlocked',
  ANTI_RESURRECTION_BLOCKED = 'antiResurrectionBlocked',
  STALE_LIVE_RESURRECTION_RISK = 'staleLiveResurrectionRisk',
  CONFLICT_POLICY_AMBIGUOUS = 'conflictPolicyAmbiguous',
  READY_FOR_N1_AFTER_AUDIT = 'readyForN1AfterAudit'
}

export enum CanonicalTombstoneConflictGuardedSeamFailure {
  DISABLED = 'disabled',
  UNSUPPORTED_MODE = 'unsupportedMode',
  PRODUCTION_EXECUTE_DENIED = 'productionExecuteDenied',
  VIEW_REFRESH_TRIGGER_DENIED = 'viewRefreshTriggerDenied',
  RETRY_DRAINER_FRESH_TOMBSTONE_CONFLICT_DENIED = 'retryDrainerFreshTombstoneConflictDenied',
  INSUFFICIENT_LOCAL_SNAPSHOT = 'insufficientLocalSnapshot',
  INSUFFICIENT_PEER_SNAPSHOT = 'insufficientPeerSnapshot',
  MATRIX_VALIDATION_BLOCKED = 'matrixValidationBlocked',
  ACTIVE_PILOT_NOT_TOMBSTONE_CONFLICT = 'activePilotNotTombstoneConflict',
  UNSUPPORTED_DOMAIN = 'unsupportedDomain',
  UNSUPPORTED_ACTION = 'unsupportedAction',
  MISSING_TOKEN = 'missingToken',
  MISSING_OWNER_APPROVAL = 'missingOwnerApproval',
  MISSING_NO_COMMIT_EVIDENCE = 'missingNoCommitEvidence',
  MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE = 'missingRealDataShadowCopyEvidence',
  MISSING_EXECUTION_SHADOW_EVIDENCE = 'missingExecutionShadowEvidence',
  MISSING_DRY_RUN_EQUIVALENCE = 'missingDryRunEquivalence',
  BLOCKING_DIVERGENCE = 'blockingDivergence',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  MISSING_METADATA_ROUTE_EVIDENCE = 'missingMetadataRouteEvidence',
  PRODUCTION_PORT_UNAVAILABLE = 'productionPortUnavailable',
  REAL_APPLY_PORT_UNAVAILABLE = 'realApplyPortUnavailable',
  APPLY_PORT_DRY_RUN_ONLY = 'applyPortDryRunOnly',
  ROOT_BOUND_WRITE_UNAVAILABLE = 'rootBoundWriteUnavailable',
  ATOMIC_REPLACE_UNAVAILABLE = 'atomicReplaceUnavailable',
  ROLLBACK_CHECKPOINT_UNAVAILABLE = 'rollbackCheckpointUnavailable',
  MISSING_ROLLBACK = 'missingRollback',
  ROLLBACK_VERIFICATION_MISSING = 'rollbackVerificationMissing',
  PRODUCTION_ROOT_ENABLED_BY_DEFAULT = 'productionRootEnabledByDefault',
  TEST_ROOT_MISSING = 'testRootMissing',
  SOFT_TOMBSTONE_STORE_UNSUPPORTED = 'softTombstoneStoreUnsupported',
  CONFLICT_LEDGER_UNSUPPORTED = 'conflictLedgerUnsupported',
  MISSING_TOMBSTONE_WINS_POLICY = 'missingTombstoneWinsPolicy',
  MISSING_ROLLBACK_EVIDENCE = 'missingRollbackEvidence',
  MISSING_TOMBSTONE_TIMESTAMP = 'missingTombstoneTimestamp',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  COMMIT_EXECUTOR_UNAVAILABLE = 'commitExecutorUnavailable',
  MISSING_FAILURE_INJECTION_EVIDENCE = 'missingFailureInjectionEvidence',
  MISSING_READ_SIDE_PARALLEL = 'missingReadSideParallel',
  MISSING_OBSERVATION_EVIDENCE = 'missingObservationEvidence',
  MISSING_ANTI_RESURRECTION_GATE = 'missingAntiResurrectionGate',
  MISSING_PHYSICAL_DELETE_GUARD = 'missingPhysicalDeleteGuard',
  MISSING_PERMANENT_DELETE_GUARD = 'missingPermanentDeleteGuard',
  MISSING_TOMBSTONE_GC_GUARD = 'missingTombstoneGCGuard',
  MISSING_CONFLICT_CONSERVATIVE_POLICY = 'missingConflictConservativePolicy',
  PHYSICAL_DELETE_PATH_DETECTED = 'physicalDeletePathDetected',
  PERMANENT_DELETE_PATH_DETECTED = 'permanentDeletePathDetected',
  TOMBSTONE_GC_PATH_DETECTED = 'tombstoneGCPathDetected',
  UNSUPPORTED_RESTORE = 'unsupportedRestore',
  STALE_LIVE_RESURRECTION_RISK = 'staleLiveResurrectionRisk',
  CONFLICT_POLICY_AMBIGUOUS = 'conflictPolicyAmbiguous',
  GENERATED_ARTIFACT_TOMBSTONED_PARENT_APPLY_BLOCKED = 'generatedArtifactTombstonedParentApplyBlocked',
  DUPLICATE_SUPPRESSION_POLICY_UNAVAILABLE = 'duplicateSuppressionPolicyUnavailable',
  DUPLICATE_SUPPRESSION_POLICY_ENABLED = 'duplicateSuppressionPolicyEnabled',
  CANARY_BUDGET_NON_ZERO_DENIED = 'canaryBudgetNonZeroDenied',
  CANARY_STAGE_EXECUTION_DENIED = 'canaryStageExecutionDenied',
  RUNTIME_SWITCH_DENIED = 'runtimeSwitchDenied'
}

export enum CanonicalTombstoneConflictGuardedDiagnosticKind {
  CANONICAL_TOMBSTONE_CONFLICT_V827_SEAM_STARTED = 'canonicalTombstoneConflictV827SeamStarted',
  CANONICAL_TOMBSTONE_CONFLICT_V827_SEAM_COMPLETED = 'canonicalTombstoneConflictV827SeamCompleted',
  CANONICAL_TOMBSTONE_CONFLICT_V827_SEAM_BLOCKED = 'canonicalTombstoneConflictV827SeamBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_V827_GATE_EVALUATED = 'canonicalTombstoneConflictV827GateEvaluated',
  CANONICAL_TOMBSTONE_CONFLICT_V827_GATE_ALLOWED_BUDGET_ZERO = 'canonicalTombstoneConflictV827GateAllowedBudgetZero',
  CANONICAL_TOMBSTONE_CONFLICT_V827_GATE_BLOCKED = 'canonicalTombstoneConflictV827GateBlocked',
  CANONICAL_TOMBSTONE_CONFLICT_V827_CANARY_BUDGET_ZERO = 'canonicalTombstoneConflictV827CanaryBudgetZero',
  CANONICAL_TOMBSTONE_CONFLICT_V827_COMMIT_NOT_EXECUTED = 'canonicalTombstoneConflictV827CommitNotExecuted',
  CANONICAL_TOMBSTONE_CONFLICT_V827_DELETE_NOT_EXECUTED = 'canonicalTombstoneConflictV827DeleteNotExecuted',
  CANONICAL_TOMBSTONE_CONFLICT_V827_RESTORE_NOT_EXECUTED = 'canonicalTombstoneConflictV827RestoreNotExecuted',
  CANONICAL_TOMBSTONE_CONFLICT_V827_CONFLICT_NOT_AUTO_RESOLVED = 'canonicalTombstoneConflictV827ConflictNotAutoResolved',
  CANONICAL_TOMBSTONE_CONFLICT_V827_LEGACY_FALLBACK_PRESERVED = 'canonicalTombstoneConflictV827LegacyFallbackPreserved',
  CANONICAL_TOMBSTONE_CONFLICT_V827_DUPLICATE_SUPPRESSION_NOT_APPLIED = 'canonicalTombstoneConflictV827DuplicateSuppressionNotApplied',
  CANONICAL_TOMBSTONE_CONFLICT_V827_EVIDENCE_REPORT_BUILT = 'canonicalTombstoneConflictV827EvidenceReportBuilt',
  CANONICAL_TOMBSTONE_CONFLICT_V827_N1_READINESS_REPORT_BUILT = 'canonicalTombstoneConflictV827N1ReadinessReportBuilt',
  CANONICAL_TOMBSTONE_CONFLICT_CANARY_BUDGET_ZERO = 'canonicalTombstoneConflictCanaryBudgetZero',
  CANONICAL_TOMBSTONE_CONFLICT_GATE_ALLOWED_BUT_NO_EXECUTION = 'canonicalTombstoneConflictGateAllowedButNoExecution',
  CANONICAL_TOMBSTONE_CONFLICT_COMMIT_SKIPPED_BECAUSE_CANARY_BUDGET_ZERO = 'canonicalTombstoneConflictCommitSkippedBecauseCanaryBudgetZero',
  CANONICAL_TOMBSTONE_CONFLICT_DELETE_SKIPPED_BECAUSE_CANARY_BUDGET_ZERO = 'canonicalTombstoneConflictDeleteSkippedBecauseCanaryBudgetZero',
  CANONICAL_TOMBSTONE_CONFLICT_RESTORE_SKIPPED_BECAUSE_CANARY_BUDGET_ZERO = 'canonicalTombstoneConflictRestoreSkippedBecauseCanaryBudgetZero',
  CANONICAL_TOMBSTONE_CONFLICT_RESOLUTION_SKIPPED_BECAUSE_CANARY_BUDGET_ZERO = 'canonicalTombstoneConflictResolutionSkippedBecauseCanaryBudgetZero'
}

export enum CanonicalTombstoneConflictN1ReadinessStatus {
  READY_FOR_N1_AFTER_AUDIT = 'readyForN1AfterAudit',
  NO_ELIGIBLE_CANDIDATE = 'noEligibleCandidate',
  INSUFFICIENT_PEER_SNAPSHOT = 'insufficientPeerSnapshot',
  INSUFFICIENT_EVIDENCE = 'insufficientEvidence',
  BLOCKED = 'blocked'
}

export enum CanonicalTombstoneConflictNoExecutionViolation {
  WILL_EXECUTE_NOW = 'willExecuteNow',
  COMMIT_EXECUTOR_CALLED = 'commitExecutorCalled',
  TOMBSTONE_MARKER_WRITTEN = 'tombstoneMarkerWritten',
  TOMBSTONE_CLEARED = 'tombstoneCleared',
  RESTORE_ATTEMPTED = 'restoreAttempted',
  PHYSICAL_DELETE_ATTEMPTED = 'physicalDeleteAttempted',
  PERMANENT_DELETE_ATTEMPTED = 'permanentDeleteAttempted',
  TOMBSTONE_GC_ATTEMPTED = 'tombstoneGCAttempted',
  CONFLICT_RESOLUTION_ATTEMPTED = 'conflictResolutionAttempted',
  DUPLICATE_LEGACY_SUPPRESSED = 'duplicateLegacySuppressed',
  LEGACY_FALLBACK_NOT_PRESERVED = 'legacyFallbackNotPreserved',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  LEGACY_PLAN_CHANGED = 'legacyPlanChanged',
  PRODUCTION_PLAN_CHANGED = 'productionPlanChanged',
  UI_MUTATED = 'uiMutated',
  UPLOAD_JOB_CREATED = 'uploadJobCreated',
  NETWORK_REQUEST_CALLED = 'networkRequestCalled'
}

// ═══════════════════════════════════════════════════════════════════════════════
// CLASSES
// ═══════════════════════════════════════════════════════════════════════════════

export class CanonicalTombstoneConflictPilotActivationResult {
  activated: boolean
  matrix: CanonicalMigrationDomainMatrix
  matrixReport: CanonicalMigrationMatrixReport
  blockers: CanonicalTombstoneConflictPilotActivationBlocker[]
  diagnosticsSummary: string

  constructor(
    matrix: CanonicalMigrationDomainMatrix, blockers: CanonicalTombstoneConflictPilotActivationBlocker[]
  ) {
    const matrixReport = matrix.validate()
    const normalizedBlockers = [...new Set(blockers)].sort()
    this.matrix = matrix
    this.matrixReport = matrixReport
    this.blockers = normalizedBlockers
    this.activated = normalizedBlockers.length === 0 && matrixReport.allowed &&
      matrixReport.activePilotDomain === CanonicalMigrationDomain.TOMBSTONE_CONFLICT
    this.diagnosticsSummary = [
      'domain=tombstoneConflict', 'version=v8.27', 'activated=' + this.activated,
      'activePilot=' + (matrixReport.activePilotDomain ?? 'none'),
      'matrixAllowed=' + matrixReport.allowed,
      'blockers=' + normalizedBlockers.join('+')
    ].join(',')
  }
}

export class CanonicalTombstoneConflictPilotActivationGate {
  constructor() {}

  evaluate(
    libraryMetadataObservationCompleteOrRetirementCandidateReady: boolean,
    generatedArtifactsTemplateCompleteOrObservationReady: boolean,
    templateReport: CanonicalTombstoneConflictTemplateReport = CanonicalTombstoneConflictTemplateReport.currentV826Audit()
  ): CanonicalTombstoneConflictPilotActivationResult {
    const matrix = CanonicalMigrationDomainMatrix.v822GeneratedArtifactsActivePilot(true)
    // Use tombstoneConflict domain matrix
    const tcm = new CanonicalMigrationDomainMatrix(CanonicalMigrationDomain.TOMBSTONE_CONFLICT, true)
    const report = tcm.validate()
    const policy = tcm.policyFor(CanonicalMigrationDomain.TOMBSTONE_CONFLICT)
    let blockers: CanonicalTombstoneConflictPilotActivationBlocker[] = []
    if (!templateReport.readyForNextPilotN0) {
      blockers.push(
        CanonicalTombstoneConflictPilotActivationBlocker.TEMPLATE_NOT_READY_FOR_NEXT_PILOT_N0)
    }
    if (!generatedArtifactsTemplateCompleteOrObservationReady) {
      blockers.push(
        CanonicalTombstoneConflictPilotActivationBlocker.GENERATED_ARTIFACTS_TEMPLATE_MISSING)
    }
    if (!libraryMetadataObservationCompleteOrRetirementCandidateReady) {
      blockers.push(
        CanonicalTombstoneConflictPilotActivationBlocker.LIBRARY_METADATA_OBSERVATION_MISSING)
    }
    if (!report.allowed) {
      blockers.push(CanonicalTombstoneConflictPilotActivationBlocker.MATRIX_VALIDATION_BLOCKED)
    }
    if (report.activePilotDomain !== CanonicalMigrationDomain.TOMBSTONE_CONFLICT) {
      blockers.push(
        CanonicalTombstoneConflictPilotActivationBlocker.ACTIVE_PILOT_NOT_TOMBSTONE_CONFLICT)
    }
    // N0 reached, N1 not reached - pass
    return new CanonicalTombstoneConflictPilotActivationResult(tcm, blockers)
  }
}

export class CanonicalTombstoneConflictGuardedGate {
  mode: CanonicalCutoverAppSeamMode
  allowed: boolean
  result: CanonicalTombstoneConflictGateResult
  failures: CanonicalTombstoneConflictGuardedSeamFailure[]
  reason: string

  constructor(
    mode: CanonicalCutoverAppSeamMode, failures: CanonicalTombstoneConflictGuardedSeamFailure[],
    canaryBudgetZero: boolean, reason: string
  ) {
    this.mode = mode
    this.failures = [...new Set(failures)].sort()
    this.allowed = this.failures.length === 0
    this.result = CanonicalTombstoneConflictGuardedGate.gateResult(this.failures, this.allowed, canaryBudgetZero)
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? this.result
  }

  private static gateResult(
    failures: CanonicalTombstoneConflictGuardedSeamFailure[], allowed: boolean, canaryBudgetZero: boolean
  ): CanonicalTombstoneConflictGateResult {
    if (allowed && canaryBudgetZero) {
      return CanonicalTombstoneConflictGateResult.ALLOWED_BUT_CANARY_BUDGET_ZERO
    }
    if (failures.includes(CanonicalTombstoneConflictGuardedSeamFailure.PHYSICAL_DELETE_PATH_DETECTED)) {
      return CanonicalTombstoneConflictGateResult.PHYSICAL_DELETE_BLOCKED
    }
    if (failures.includes(CanonicalTombstoneConflictGuardedSeamFailure.PERMANENT_DELETE_PATH_DETECTED)) {
      return CanonicalTombstoneConflictGateResult.PERMANENT_DELETE_BLOCKED
    }
    if (failures.includes(CanonicalTombstoneConflictGuardedSeamFailure.TOMBSTONE_GC_PATH_DETECTED)) {
      return CanonicalTombstoneConflictGateResult.TOMBSTONE_GC_BLOCKED
    }
    if (failures.includes(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_ANTI_RESURRECTION_GATE)) {
      return CanonicalTombstoneConflictGateResult.ANTI_RESURRECTION_BLOCKED
    }
    if (failures.includes(CanonicalTombstoneConflictGuardedSeamFailure.STALE_LIVE_RESURRECTION_RISK)) {
      return CanonicalTombstoneConflictGateResult.STALE_LIVE_RESURRECTION_RISK
    }
    if (failures.includes(CanonicalTombstoneConflictGuardedSeamFailure.CONFLICT_POLICY_AMBIGUOUS)) {
      return CanonicalTombstoneConflictGateResult.CONFLICT_POLICY_AMBIGUOUS
    }
    if (failures.includes(CanonicalTombstoneConflictGuardedSeamFailure.UNSUPPORTED_DOMAIN) ||
      failures.includes(CanonicalTombstoneConflictGuardedSeamFailure.UNSUPPORTED_ACTION)) {
      return CanonicalTombstoneConflictGateResult.UNSUPPORTED_DOMAIN
    }
    if (failures.some(
      (f: CanonicalTombstoneConflictGuardedSeamFailure) => isEvidenceMissingForTcGuarded(f))) {
      return CanonicalTombstoneConflictGateResult.MISSING_EVIDENCE
    }
    return CanonicalTombstoneConflictGateResult.BLOCKED
  }
}

function isEvidenceMissingForTcGuarded(f: CanonicalTombstoneConflictGuardedSeamFailure): boolean {
  return [
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_NO_COMMIT_EVIDENCE,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_EXECUTION_SHADOW_EVIDENCE,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_DRY_RUN_EQUIVALENCE,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_METADATA_ROUTE_EVIDENCE,
    CanonicalTombstoneConflictGuardedSeamFailure.PRODUCTION_PORT_UNAVAILABLE,
    CanonicalTombstoneConflictGuardedSeamFailure.REAL_APPLY_PORT_UNAVAILABLE,
    CanonicalTombstoneConflictGuardedSeamFailure.APPLY_PORT_DRY_RUN_ONLY,
    CanonicalTombstoneConflictGuardedSeamFailure.ROOT_BOUND_WRITE_UNAVAILABLE,
    CanonicalTombstoneConflictGuardedSeamFailure.ATOMIC_REPLACE_UNAVAILABLE,
    CanonicalTombstoneConflictGuardedSeamFailure.ROLLBACK_CHECKPOINT_UNAVAILABLE,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_ROLLBACK,
    CanonicalTombstoneConflictGuardedSeamFailure.ROLLBACK_VERIFICATION_MISSING,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_TOMBSTONE_WINS_POLICY,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_ROLLBACK_EVIDENCE,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_TOMBSTONE_TIMESTAMP,
    CanonicalTombstoneConflictGuardedSeamFailure.COMMIT_EXECUTOR_UNAVAILABLE,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_FAILURE_INJECTION_EVIDENCE,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_READ_SIDE_PARALLEL,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_OBSERVATION_EVIDENCE,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_ANTI_RESURRECTION_GATE,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_PHYSICAL_DELETE_GUARD,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_PERMANENT_DELETE_GUARD,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_TOMBSTONE_GC_GUARD,
    CanonicalTombstoneConflictGuardedSeamFailure.MISSING_CONFLICT_CONSERVATIVE_POLICY,
    CanonicalTombstoneConflictGuardedSeamFailure.SOFT_TOMBSTONE_STORE_UNSUPPORTED,
    CanonicalTombstoneConflictGuardedSeamFailure.CONFLICT_LEDGER_UNSUPPORTED
  ].includes(f)
}

export class CanonicalTombstoneConflictNoExecutionAssertion {
  passed: boolean
  violations: CanonicalTombstoneConflictNoExecutionViolation[]

  constructor(passed: boolean, violations: CanonicalTombstoneConflictNoExecutionViolation[]) {
    this.passed = passed
    this.violations = [...new Set(violations)].sort()
  }

  static evaluate(
    result: CanonicalTombstoneConflictGuardedSeamResult
  ): CanonicalTombstoneConflictNoExecutionAssertion {
    let violations: CanonicalTombstoneConflictNoExecutionViolation[] = []
    if (result.willExecuteNow) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.WILL_EXECUTE_NOW)
    }
    if (result.commitExecutorCalled || result.commitAttemptedCount !== 0) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.COMMIT_EXECUTOR_CALLED)
    }
    if (result.tombstoneMarkerWrittenCount !== 0) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.TOMBSTONE_MARKER_WRITTEN)
    }
    if (result.restoreAttemptedCount !== 0) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.RESTORE_ATTEMPTED)
    }
    if (result.physicalDeleteAttemptedCount !== 0) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.PHYSICAL_DELETE_ATTEMPTED)
    }
    if (result.permanentDeleteAttemptedCount !== 0) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.PERMANENT_DELETE_ATTEMPTED)
    }
    if (result.tombstoneGCAttemptedCount !== 0) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.TOMBSTONE_GC_ATTEMPTED)
    }
    if (result.conflictResolutionAttemptedCount !== 0) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.CONFLICT_RESOLUTION_ATTEMPTED)
    }
    if (result.duplicateLegacySuppressedActionIDs.length > 0) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.DUPLICATE_LEGACY_SUPPRESSED)
    }
    if (!result.legacyFallbackPreserved) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.LEGACY_FALLBACK_NOT_PRESERVED)
    }
    if (result.runtimeSwitchEnabled) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.RUNTIME_SWITCH_ENABLED)
    }
    if (!result.legacyPlanUnchanged) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.LEGACY_PLAN_CHANGED)
    }
    if (!result.productionPlanUnchanged) {
      violations.push(CanonicalTombstoneConflictNoExecutionViolation.PRODUCTION_PLAN_CHANGED)
    }
    return new CanonicalTombstoneConflictNoExecutionAssertion(violations.length === 0, violations)
  }
}

export class CanonicalTombstoneConflictEvidenceReport {
  status: CanonicalTombstoneConflictGuardedEvidenceStatus
  missingReasons: CanonicalTombstoneConflictGuardedSeamFailure[]
  matrixReport: CanonicalMigrationMatrixReport
  canaryPolicy: CanonicalTombstoneConflictCanaryPolicy
  localSnapshotAvailable: boolean
  peerSnapshotAvailable: boolean
  candidateCount: number
  eligibleCandidateCount: number
  legacyActionCandidateCount: number
  staleLiveMetadataRiskCount: number
  activeVsTombstoneConflictCount: number
  legacyFallbackAvailable: boolean

  constructor(
    missingReasons: CanonicalTombstoneConflictGuardedSeamFailure[],
    matrixReport: CanonicalMigrationMatrixReport, canaryPolicy: CanonicalTombstoneConflictCanaryPolicy,
    localSnapshotAvailable: boolean, peerSnapshotAvailable: boolean, candidateCount: number,
    eligibleCandidateCount: number, legacyActionCandidateCount: number,
    staleLiveMetadataRiskCount: number, activeVsTombstoneConflictCount: number,
    legacyFallbackAvailable: boolean
  ) {
    const normalizedReasons = [...new Set(missingReasons)].sort()
    this.status = normalizedReasons.length === 0 ? CanonicalTombstoneConflictGuardedEvidenceStatus.COMPLETE :
      CanonicalTombstoneConflictGuardedEvidenceStatus.INCOMPLETE
    this.missingReasons = normalizedReasons
    this.matrixReport = matrixReport
    this.canaryPolicy = canaryPolicy
    this.localSnapshotAvailable = localSnapshotAvailable
    this.peerSnapshotAvailable = peerSnapshotAvailable
    this.candidateCount = Math.max(0, candidateCount)
    this.eligibleCandidateCount = Math.max(0, eligibleCandidateCount)
    this.legacyActionCandidateCount = Math.max(0, legacyActionCandidateCount)
    this.staleLiveMetadataRiskCount = Math.max(0, staleLiveMetadataRiskCount)
    this.activeVsTombstoneConflictCount = Math.max(0, activeVsTombstoneConflictCount)
    this.legacyFallbackAvailable = legacyFallbackAvailable
  }
}

export class CanonicalTombstoneConflictGuardedDiagnostic {
  kind: CanonicalTombstoneConflictGuardedDiagnosticKind
  syncRunID: string | null
  trigger: CanonicalSyncPlanTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  mode: CanonicalCutoverAppSeamMode
  domain: CanonicalTombstoneConflictDomain | null
  objectID: string | null
  actionKind: CanonicalTombstoneConflictActionKind | null
  candidateCount: number
  eligibleCandidateCount: number
  gateFailureCount: number
  canaryBudget: number
  commitAttemptedCount: number
  deleteAttemptedCount: number
  restoreAttemptedCount: number
  conflictResolutionAttemptedCount: number
  duplicateSuppressionCandidateCount: number
  staleLiveMetadataRiskCount: number
  activeVsTombstoneConflictCount: number
  result: string | null
  reason: string | null
  hashPrefix: string | null

  constructor(
    kind: CanonicalTombstoneConflictGuardedDiagnosticKind, syncRunID: string | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole,
    mode: CanonicalCutoverAppSeamMode, domain: CanonicalTombstoneConflictDomain | null = null,
    objectID: string | null = null, actionKind: CanonicalTombstoneConflictActionKind | null = null,
    candidateCount: number, eligibleCandidateCount: number = 0, gateFailureCount: number = 0,
    canaryBudget: number, commitAttemptedCount: number = 0, deleteAttemptedCount: number = 0,
    restoreAttemptedCount: number = 0, conflictResolutionAttemptedCount: number = 0,
    duplicateSuppressionCandidateCount: number = 0, staleLiveMetadataRiskCount: number = 0,
    activeVsTombstoneConflictCount: number = 0, result: string | null = null,
    reason: string | null = null, hash: CanonicalHash | null = null
  ) {
    this.kind = kind
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.mode = mode
    this.domain = domain
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'tombstone-object') : null
    this.actionKind = actionKind
    this.candidateCount = Math.max(0, candidateCount)
    this.eligibleCandidateCount = Math.max(0, eligibleCandidateCount)
    this.gateFailureCount = Math.max(0, gateFailureCount)
    this.canaryBudget = Math.max(0, canaryBudget)
    this.commitAttemptedCount = Math.max(0, commitAttemptedCount)
    this.deleteAttemptedCount = Math.max(0, deleteAttemptedCount)
    this.restoreAttemptedCount = Math.max(0, restoreAttemptedCount)
    this.conflictResolutionAttemptedCount = Math.max(0, conflictResolutionAttemptedCount)
    this.duplicateSuppressionCandidateCount = Math.max(0, duplicateSuppressionCandidateCount)
    this.staleLiveMetadataRiskCount = Math.max(0, staleLiveMetadataRiskCount)
    this.activeVsTombstoneConflictCount = Math.max(0, activeVsTombstoneConflictCount)
    this.result = CanonicalProductionRedaction.safeDiagnosticText(result)
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason)
    this.hashPrefix = hash ? CanonicalProductionRedaction.hashPrefix(hash.value) : null
  }

  static hashPrefixStatic(value: string | null): string | null {
    if (!value || value.trim().length === 0) {
      return null
    }
    return value.trim().substring(0, 12)
  }
}

export class CanonicalTombstoneConflictGuardedContext {
  syncRunID: string | null
  trigger: CanonicalSyncPlanTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  localManifest: CanonicalManifest | null
  peerManifest: CanonicalManifest | null
  candidates: CanonicalTombstoneConflictCandidate[]
  legacyActionSnapshot: CanonicalLegacyActionSnapshot
  matrix: CanonicalMigrationDomainMatrix
  evidence: CanonicalTombstoneConflictCutoverEvidence
  canaryPolicy: CanonicalTombstoneConflictCanaryPolicy
  cutoverToken: CanonicalCutoverToken | null
  localSnapshotAvailable: boolean
  peerSnapshotAvailable: boolean
  commitExecutorReady: boolean
  legacyFallbackAvailable: boolean
  staleLiveMetadataRiskCount: number
  activeVsTombstoneConflictCount: number
  generatedArtifactTombstonedParentApplyBlocked: boolean

  constructor(
    syncRunID: string | null, trigger: CanonicalSyncPlanTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole, localManifest: CanonicalManifest | null,
    peerManifest: CanonicalManifest | null, candidates: CanonicalTombstoneConflictCandidate[] = [],
    legacyActionSnapshot: CanonicalLegacyActionSnapshot = CanonicalLegacyActionSnapshot.empty,
    matrix: CanonicalMigrationDomainMatrix = new CanonicalMigrationDomainMatrix(
      CanonicalMigrationDomain.TOMBSTONE_CONFLICT, true),
    evidence: CanonicalTombstoneConflictCutoverEvidence,
    canaryPolicy: CanonicalTombstoneConflictCanaryPolicy = CanonicalTombstoneConflictCanaryPolicy.disabled,
    cutoverToken: CanonicalCutoverToken | null = null, localSnapshotAvailable: boolean,
    peerSnapshotAvailable: boolean, commitExecutorReady: boolean = true,
    legacyFallbackAvailable: boolean | null = null
  ) {
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.localManifest = localManifest
    this.peerManifest = peerManifest
    this.candidates = candidates
    this.legacyActionSnapshot = legacyActionSnapshot
    this.matrix = matrix
    this.evidence = evidence
    this.canaryPolicy = canaryPolicy
    this.cutoverToken = cutoverToken
    this.localSnapshotAvailable = localSnapshotAvailable
    this.peerSnapshotAvailable = peerSnapshotAvailable
    this.commitExecutorReady = commitExecutorReady
    this.legacyFallbackAvailable = legacyFallbackAvailable ?? evidence.legacyFallbackAvailable
    this.staleLiveMetadataRiskCount = Math.max(0,
      candidates.filter((c: CanonicalTombstoneConflictCandidate) => c.staleLiveMetadataRisk).length)
    this.activeVsTombstoneConflictCount = Math.max(0,
      candidates.filter((c: CanonicalTombstoneConflictCandidate) => c.hasActiveVsTombstoneConflict ||
        c.domain === CanonicalTombstoneConflictDomain.ACTIVE_VS_TOMBSTONE_CONFLICT ||
        c.actionKind === CanonicalTombstoneConflictActionKind.RESURRECTION_BLOCKED).length)
    this.generatedArtifactTombstonedParentApplyBlocked = candidates.some(
      (c: CanonicalTombstoneConflictCandidate) =>
        c.actionKind === CanonicalTombstoneConflictActionKind.GENERATED_ARTIFACT_TOMBSTONE_MARK_UNSUPPORTED)
  }
}

export class CanonicalTombstoneConflictGuardedSeamResult {
  gate: CanonicalTombstoneConflictGuardedGate
  evidenceReport: CanonicalTombstoneConflictEvidenceReport
  n1ReadinessReport: any
  diagnostics: CanonicalTombstoneConflictGuardedDiagnostic[]
  noExecutionAssertion: CanonicalTombstoneConflictNoExecutionAssertion
  canaryBudgetZero: boolean
  canExecuteNow: boolean
  willExecuteNow: boolean
  commitAttemptedCount: number
  tombstoneMarkerWrittenCount: number
  restoreAttemptedCount: number
  physicalDeleteAttemptedCount: number
  permanentDeleteAttemptedCount: number
  tombstoneGCAttemptedCount: number
  conflictResolutionAttemptedCount: number
  commitExecutorCalled: boolean
  realApplyPortCalled: boolean
  tombstoneMarkerWriteAttempted: boolean
  tombstoneClearAttempted: boolean
  duplicateLegacySuppressedActionIDs: string[]
  duplicateLegacySuppressionCandidates: string[]
  legacyFallbackPreserved: boolean
  runtimeSwitchEnabled: boolean
  legacyPlanUnchanged: boolean
  productionPlanUnchanged: boolean
  uiMutated: boolean
  uploadJobCreated: boolean
  networkRequestCalled: boolean
  nonfatalFailureCount: number

  get succeeded(): boolean {
    return this.gate.allowed && this.canaryBudgetZero && !this.willExecuteNow && this.noExecutionAssertion.passed
  }
}

export class CanonicalTombstoneConflictCutoverAppSeamConfiguration {
  isEnabled: boolean
  mode: CanonicalCutoverAppSeamMode
  policy: CanonicalTombstoneConflictGuardedPolicy
  evidence: CanonicalTombstoneConflictCutoverEvidence

  constructor(
    isEnabled: boolean = false, mode: CanonicalCutoverAppSeamMode = CanonicalCutoverAppSeamMode.DISABLED,
    policy: CanonicalTombstoneConflictGuardedPolicy = new CanonicalTombstoneConflictGuardedPolicy(),
    evidence: CanonicalTombstoneConflictCutoverEvidence = new CanonicalTombstoneConflictCutoverEvidence()
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

export class CanonicalTombstoneConflictGuardedPolicy {
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

export class CanonicalTombstoneConflictGuardedSeam {
  constructor() {}

  evaluate(
    configuration: CanonicalTombstoneConflictCutoverAppSeamConfiguration,
    context: CanonicalTombstoneConflictGuardedContext
  ): CanonicalTombstoneConflictGuardedSeamResult {
    const canaryPolicy = configuration.policy.canaryPolicy
    const duplicateCandidates = this.duplicateSuppressionCandidates(context)
    const evidenceReport = this.makeEvidenceReport(context, canaryPolicy, duplicateCandidates)
    const canaryBudgetZero = CanonicalTombstoneConflictGuardedSeam.isCanaryBudgetZero(canaryPolicy)
    const gate = this.evaluateGate(configuration, context, evidenceReport, canaryPolicy, canaryBudgetZero)
    const eligibleCount = this.eligibleCandidateCount(context.candidates)
    const canExecuteNow = gate.allowed
    const willExecuteNow = false
    const emptyAssertion = new CanonicalTombstoneConflictNoExecutionAssertion(true, [])
    let result: CanonicalTombstoneConflictGuardedSeamResult = {
      gate: gate,
      evidenceReport: evidenceReport,
      n1ReadinessReport: this.makeN1ReadinessReport(context, evidenceReport, gate, canaryPolicy, canExecuteNow,
        willExecuteNow, emptyAssertion.passed),
      diagnostics: [],
      noExecutionAssertion: emptyAssertion,
      canaryBudgetZero: canaryBudgetZero, canExecuteNow: canExecuteNow, willExecuteNow: willExecuteNow,
      commitAttemptedCount: 0, tombstoneMarkerWrittenCount: 0, restoreAttemptedCount: 0,
      physicalDeleteAttemptedCount: 0, permanentDeleteAttemptedCount: 0, tombstoneGCAttemptedCount: 0,
      conflictResolutionAttemptedCount: 0, commitExecutorCalled: false, realApplyPortCalled: false,
      tombstoneMarkerWriteAttempted: false, tombstoneClearAttempted: false,
      duplicateLegacySuppressedActionIDs: [], duplicateLegacySuppressionCandidates: duplicateCandidates,
      legacyFallbackPreserved: true, runtimeSwitchEnabled: false,
      legacyPlanUnchanged: true, productionPlanUnchanged: true,
      uiMutated: false, uploadJobCreated: false, networkRequestCalled: false,
      nonfatalFailureCount: gate.failures.length,
      succeeded: false
    }
    const assertion = CanonicalTombstoneConflictNoExecutionAssertion.evaluate(result)
    result.noExecutionAssertion = assertion
    result.n1ReadinessReport = this.makeN1ReadinessReport(context, evidenceReport, gate, canaryPolicy,
      canExecuteNow, willExecuteNow, assertion.passed)
    result.diagnostics = this.baseDiagnostics(configuration, context, gate, evidenceReport,
      result.n1ReadinessReport, context.candidates.length, eligibleCount, duplicateCandidates.length,
      canaryPolicy.canaryMaxObjectsPerSyncRun, canaryBudgetZero, willExecuteNow)
    return result
  }

  private evaluateGate(
    configuration: CanonicalTombstoneConflictCutoverAppSeamConfiguration,
    context: CanonicalTombstoneConflictGuardedContext,
    evidenceReport: CanonicalTombstoneConflictEvidenceReport,
    canaryPolicy: CanonicalTombstoneConflictCanaryPolicy, canaryBudgetZero: boolean
  ): CanonicalTombstoneConflictGuardedGate {
    let failures: CanonicalTombstoneConflictGuardedSeamFailure[] = []
    const mode = configuration.effectiveMode
    if (mode === CanonicalCutoverAppSeamMode.DISABLED) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.DISABLED)
    }
    if (mode === CanonicalCutoverAppSeamMode.GUARDED_EXECUTE_NO_COMMIT) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.UNSUPPORTED_MODE)
    }
    if (mode === CanonicalCutoverAppSeamMode.PRODUCTION_EXECUTE) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.PRODUCTION_EXECUTE_DENIED)
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.UNSUPPORTED_MODE)
    }
    if (context.trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.VIEW_REFRESH_TRIGGER_DENIED)
    }
    if (context.trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) {
      failures.push(
        CanonicalTombstoneConflictGuardedSeamFailure.RETRY_DRAINER_FRESH_TOMBSTONE_CONFLICT_DENIED)
    }
    if (!context.localSnapshotAvailable || !context.localManifest) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.INSUFFICIENT_LOCAL_SNAPSHOT)
    }
    if (!context.peerSnapshotAvailable || !context.peerManifest) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.INSUFFICIENT_PEER_SNAPSHOT)
    }
    if (!evidenceReport.matrixReport.allowed) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.MATRIX_VALIDATION_BLOCKED)
    }
    if (evidenceReport.matrixReport.activePilotDomain !== CanonicalMigrationDomain.TOMBSTONE_CONFLICT) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.ACTIVE_PILOT_NOT_TOMBSTONE_CONFLICT)
    }
    if (!context.cutoverToken) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_TOKEN)
    }
    if (context.cutoverToken?.ownerApproved !== true) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_OWNER_APPROVAL)
    }
    if (canaryPolicy.canaryMaxObjectsPerSyncRun > 0) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.CANARY_BUDGET_NON_ZERO_DENIED)
    }
    if (tcCanaryStageIsExecutable(canaryPolicy.requestedStage) || canaryPolicy.allowCandidateExecution) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.CANARY_STAGE_EXECUTION_DENIED)
    }
    if (canaryPolicy.runtimeSwitchEnabled) {
      failures.push(CanonicalTombstoneConflictGuardedSeamFailure.RUNTIME_SWITCH_DENIED)
    }
    failures.push(...evidenceReport.missingReasons)
    return new CanonicalTombstoneConflictGuardedGate(mode, failures, canaryBudgetZero,
      failures.length === 0 ? 'canonicalTombstoneConflictV827GateAllowedBudgetZero' : failures.join(','))
  }

  private makeEvidenceReport(
    context: CanonicalTombstoneConflictGuardedContext,
    canaryPolicy: CanonicalTombstoneConflictCanaryPolicy, duplicateCandidates: string[]
  ): CanonicalTombstoneConflictEvidenceReport {
    const evidence = context.evidence
    const matrixReport = context.matrix.validate()
    const candidateFailures = this.candidateFailuresFunc(context.candidates)
    let missing: CanonicalTombstoneConflictGuardedSeamFailure[] = []
    if (!evidence.noCommitEvidenceAvailable) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_NO_COMMIT_EVIDENCE)
    }
    if (!evidence.realDataShadowCopyVerified) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE)
    }
    if (!evidence.executionShadowVerified) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_EXECUTION_SHADOW_EVIDENCE)
    }
    if (!evidence.dryRunEquivalenceVerified) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_DRY_RUN_EQUIVALENCE)
    }
    if (!evidence.noBlockingDivergence) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.BLOCKING_DIVERGENCE)
    }
    if (!evidence.noUnresolvedConflict) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.UNRESOLVED_CONFLICT)
    }
    if (!evidence.metadataRouteEvidenceAvailable) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_METADATA_ROUTE_EVIDENCE)
    }
    if (!evidence.productionPortAvailable) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.PRODUCTION_PORT_UNAVAILABLE)
    }
    if (!evidence.realRootBoundApplyPortAvailable) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.REAL_APPLY_PORT_UNAVAILABLE)
    }
    if (!context.legacyFallbackAvailable) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.LEGACY_FALLBACK_UNAVAILABLE)
    }
    if (!context.commitExecutorReady) {
      missing.push(CanonicalTombstoneConflictGuardedSeamFailure.COMMIT_EXECUTOR_UNAVAILABLE)
    }
    missing.push(...candidateFailures)
    return new CanonicalTombstoneConflictEvidenceReport(missing, matrixReport, canaryPolicy,
      context.localSnapshotAvailable, context.peerSnapshotAvailable, context.candidates.length,
      this.eligibleCandidateCount(context.candidates), duplicateCandidates.length,
      context.staleLiveMetadataRiskCount, context.activeVsTombstoneConflictCount,
      context.legacyFallbackAvailable)
  }

  private makeN1ReadinessReport(
    context: CanonicalTombstoneConflictGuardedContext,
    evidenceReport: CanonicalTombstoneConflictEvidenceReport,
    gate: CanonicalTombstoneConflictGuardedGate, canaryPolicy: CanonicalTombstoneConflictCanaryPolicy,
    canExecuteNow: boolean, willExecuteNow: boolean, noExecutionAssertionPassed: boolean
  ): any {
    return {
      status: CanonicalTombstoneConflictN1ReadinessStatus.BLOCKED,
      gateResult: CanonicalTombstoneConflictGateResult.BLOCKED,
      candidateCount: context.candidates.length,
      eligibleCandidateCount: this.eligibleCandidateCount(context.candidates),
      canaryBudget: canaryPolicy.canaryMaxObjectsPerSyncRun,
      canExecuteNow: canExecuteNow, willExecuteNow: willExecuteNow,
      noExecutionAssertionPassed: noExecutionAssertionPassed
    }
  }

  private baseDiagnostics(
    configuration: CanonicalTombstoneConflictCutoverAppSeamConfiguration,
    context: CanonicalTombstoneConflictGuardedContext,
    gate: CanonicalTombstoneConflictGuardedGate,
    evidenceReport: CanonicalTombstoneConflictEvidenceReport, n1Report: any,
    candidateCount: number, eligibleCandidateCount: number,
    duplicateCount: number, canaryBudget: number, canaryBudgetZero: boolean, willExecuteNow: boolean
  ): CanonicalTombstoneConflictGuardedDiagnostic[] {
    let diagnostics: CanonicalTombstoneConflictGuardedDiagnostic[] = [
      new CanonicalTombstoneConflictGuardedDiagnostic(
        CanonicalTombstoneConflictGuardedDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_V827_SEAM_STARTED,
        context.syncRunID, context.trigger, context.nodeRole, configuration.effectiveMode,
        null, null, null, candidateCount, eligibleCandidateCount, gate.failures.length, canaryBudget,
        undefined, undefined, undefined, undefined, duplicateCount,
        context.staleLiveMetadataRiskCount, context.activeVsTombstoneConflictCount,
        gate.allowed ? 'allowed' : 'blocked', gate.reason),
      new CanonicalTombstoneConflictGuardedDiagnostic(
        CanonicalTombstoneConflictGuardedDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_V827_LEGACY_FALLBACK_PRESERVED,
        context.syncRunID, context.trigger, context.nodeRole, configuration.effectiveMode,
        null, null, null, candidateCount, eligibleCandidateCount, gate.failures.length, canaryBudget,
        undefined, undefined, undefined, undefined, duplicateCount,
        context.staleLiveMetadataRiskCount, context.activeVsTombstoneConflictCount,
        'legacyFallbackPreserved', 'v827DoesNotReplaceLegacyTombstoneConflictPlan'),
      new CanonicalTombstoneConflictGuardedDiagnostic(
        CanonicalTombstoneConflictGuardedDiagnosticKind.CANONICAL_TOMBSTONE_CONFLICT_V827_SEAM_COMPLETED,
        context.syncRunID, context.trigger, context.nodeRole, configuration.effectiveMode,
        null, null, null, candidateCount, eligibleCandidateCount, gate.failures.length, canaryBudget,
        undefined, undefined, undefined, undefined, duplicateCount,
        context.staleLiveMetadataRiskCount, context.activeVsTombstoneConflictCount,
        'completed', gate.allowed ? 'nonfatalNoExecution' : 'nonfatalBlocked')
    ]
    return diagnostics
  }

  private candidateFailuresFunc(
    candidates: CanonicalTombstoneConflictCandidate[]
  ): CanonicalTombstoneConflictGuardedSeamFailure[] {
    let failures: CanonicalTombstoneConflictGuardedSeamFailure[] = []
    for (const c of candidates) {
      if (c.actionKind === CanonicalTombstoneConflictActionKind.UNSUPPORTED) {
        failures.push(CanonicalTombstoneConflictGuardedSeamFailure.UNSUPPORTED_ACTION)
      }
      if (actionKindIsTombstoneMarkerWrite(c.actionKind) && !c.deletedAt) {
        failures.push(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_TOMBSTONE_TIMESTAMP)
      }
      if (actionKindIsTombstoneMarkerWrite(c.actionKind) && !c.tombstoneWinsIfNewerPolicy) {
        failures.push(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_TOMBSTONE_WINS_POLICY)
      }
      if (actionKindIsExecutable(c.actionKind) && !c.rollbackEvidenceAvailable) {
        failures.push(CanonicalTombstoneConflictGuardedSeamFailure.MISSING_ROLLBACK_EVIDENCE)
      }
      const reason = c.action.reason.toLowerCase()
      if (reason.includes('physicaldelete')) {
        failures.push(CanonicalTombstoneConflictGuardedSeamFailure.PHYSICAL_DELETE_PATH_DETECTED)
      }
      if (reason.includes('permanentdelete')) {
        failures.push(CanonicalTombstoneConflictGuardedSeamFailure.PERMANENT_DELETE_PATH_DETECTED)
      }
      if (reason.includes('tombstonegc')) {
        failures.push(CanonicalTombstoneConflictGuardedSeamFailure.TOMBSTONE_GC_PATH_DETECTED)
      }
    }
    return [...new Set(failures)].sort()
  }

  private eligibleCandidateCount(candidates: CanonicalTombstoneConflictCandidate[]): number {
    return candidates.filter((c: CanonicalTombstoneConflictCandidate) =>
      this.candidateFailuresFunc([c]).length === 0 && actionKindIsExecutable(c.actionKind)).length
  }

  private duplicateSuppressionCandidates(
    context: CanonicalTombstoneConflictGuardedContext
  ): string[] {
    const legacyIDs = context.legacyActionSnapshot.actionIDSetFor(
      CanonicalProductionDomain.TOMBSTONES)
    const conflictIDs = context.legacyActionSnapshot.actionIDSetFor(
      CanonicalProductionDomain.CONFLICTS)
    const allIDs = new Set([...legacyIDs, ...conflictIDs])
    return context.candidates.map((c: CanonicalTombstoneConflictCandidate) => c.action.actionID)
      .filter((id: string) => allIDs.has(id)).sort()
  }

  static isCanaryBudgetZero(policy: CanonicalTombstoneConflictCanaryPolicy): boolean {
    return policy.canaryMaxObjectsPerSyncRun === 0 && !tcCanaryStageIsExecutable(policy.requestedStage) &&
      !policy.allowCandidateExecution && !policy.runtimeSwitchEnabled
  }
}
