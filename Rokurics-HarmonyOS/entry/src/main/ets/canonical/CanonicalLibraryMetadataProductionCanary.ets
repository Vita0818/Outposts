/**
 * CanonicalLibraryMetadataProductionCanary — production canary injection & root gate.
 * Ported from Rokurics-Apple (CanonicalLibraryMetadataProductionCanary.swift).
 */

import {
  CanonicalHash, nilIfEmpty, hashPrefix, timestampString
} from './CanonicalCore'
import {
  CanonicalMigrationDomain,
  CanonicalMigrationDomainMatrix,
  CanonicalLibraryMetadataCutoverDomain,
  CanonicalLibraryMetadataCutoverFailure,
  CanonicalLibraryMetadataCutoverEvidence,
  CanonicalLibraryMetadataCutoverCandidate,
  CanonicalLibraryMetadataCutoverResult,
  CanonicalLibraryMetadataCutoverDiagnostic,
  CanonicalLibraryMetadataCutoverDiagnosticKind,
  CanonicalLibraryMetadataCanaryCandidateSafetyKind,
  CanonicalLibraryMetadataCanaryCandidate,
  CanonicalLibraryMetadataCanaryCandidateSafety,
  CanonicalLibraryMetadataCanarySelector,
  CanonicalLibraryMetadataCanarySelectionResult,
  CanonicalLibraryMetadataCanaryPolicy,
  CanonicalLibraryMetadataCanaryStagePolicy,
  CanonicalLibraryMetadataProductionCommitResult,
  CanonicalLibraryMetadataRollbackExecutionResult,
  CanonicalProductionExecutionDomainRole,
  CanonicalProductionSideEffect,
  CanonicalProductionDomain,
  CanonicalCutoverMode
} from './CanonicalLibraryMetadataCutover'
import {
  CanonicalMigrationLandingFreeze,
  CanonicalMigrationLandingFreezeResult,
  CanonicalLibraryMetadataRealCanaryBlocker,
  CanonicalLibraryMetadataRealCanaryObservationStatus,
  CanonicalLibraryMetadataRealCanaryRecommendation,
  CanonicalLibraryMetadataRealCanaryObservationReport
} from './CanonicalLibraryMetadataLanding'
import { CanonicalSyncPlanTrigger } from './CanonicalLibrarySyncPlanner'

class CanonicalProductionRedaction {
  static safeIdentifier(value: string, fallback: string): string {
    const t = value.trim()
    return t.length === 0 ? fallback : t
  }
  static safeDiagnosticText(value: string | null | undefined): string | null {
    if (!value) return null
    const t = value.trim()
    return t.length === 0 ? null : t
  }
  static hashPrefix(value: string | null | undefined): string | null {
    if (!value) return null
    return hashPrefix(value, 12)
  }
}

// ── Production Canary Mode ──

export enum CanonicalLibraryMetadataProductionCanaryMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  CANARY_N1_ARMED = 'canaryN1Armed',
  CANARY_N1_EXECUTE = 'canaryN1Execute',
  BLOCKED = 'blocked'
}

// ── Production Canary Root Mode ──

export enum CanonicalLibraryMetadataProductionCanaryRootMode {
  DISABLED = 'disabled',
  TEST_ROOT = 'testRoot',
  PRODUCTION_ROOT_EXPLICIT = 'productionRootExplicit'
}

// ── Production Canary Policy ──

export class CanonicalLibraryMetadataProductionCanaryPolicy {
  domain: CanonicalMigrationDomain
  canaryMaxObjectsPerSyncRun: number
  requiresExplicitInternalDebugConfiguration: boolean
  requiresProductionToken: boolean
  requiresOwnerApproval: boolean
  requiresRollbackPlan: boolean
  requiresReadSideParallelEquivalent: boolean
  productionRootDisabledByDefault: boolean
  runtimeSwitchEnabled: boolean
  allowAllEligible: boolean
  releaseDefaultEnabled: boolean

  constructor(
    domain: CanonicalMigrationDomain = CanonicalMigrationDomain.LIBRARY_METADATA,
    canaryMaxObjectsPerSyncRun: number = 1,
    requiresExplicitInternalDebugConfiguration: boolean = true,
    requiresProductionToken: boolean = true,
    requiresOwnerApproval: boolean = true,
    requiresRollbackPlan: boolean = true,
    requiresReadSideParallelEquivalent: boolean = true,
    productionRootDisabledByDefault: boolean = true,
    runtimeSwitchEnabled: boolean = false,
    allowAllEligible: boolean = false,
    releaseDefaultEnabled: boolean = false
  ) {
    this.domain = domain
    this.canaryMaxObjectsPerSyncRun = Math.max(0, canaryMaxObjectsPerSyncRun)
    this.requiresExplicitInternalDebugConfiguration = requiresExplicitInternalDebugConfiguration
    this.requiresProductionToken = requiresProductionToken
    this.requiresOwnerApproval = requiresOwnerApproval
    this.requiresRollbackPlan = requiresRollbackPlan
    this.requiresReadSideParallelEquivalent = requiresReadSideParallelEquivalent
    this.productionRootDisabledByDefault = productionRootDisabledByDefault
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.allowAllEligible = allowAllEligible
    this.releaseDefaultEnabled = releaseDefaultEnabled
  }

  static readonly STRICT_LIBRARY_METADATA_N1 = new CanonicalLibraryMetadataProductionCanaryPolicy()

  get isStrictN1LibraryMetadata(): boolean {
    return this.domain === CanonicalMigrationDomain.LIBRARY_METADATA &&
      this.canaryMaxObjectsPerSyncRun === 1 &&
      this.requiresExplicitInternalDebugConfiguration &&
      this.productionRootDisabledByDefault &&
      !this.runtimeSwitchEnabled &&
      !this.allowAllEligible &&
      !this.releaseDefaultEnabled
  }
}

// ── Production Canary Configuration ──

export class CanonicalLibraryMetadataProductionCanaryConfiguration {
  mode: CanonicalLibraryMetadataProductionCanaryMode
  rootMode: CanonicalLibraryMetadataProductionCanaryRootMode
  policy: CanonicalLibraryMetadataProductionCanaryPolicy
  explicitInternalDebugConfiguration: boolean
  allowProductionRootWrites: boolean

  constructor(
    mode: CanonicalLibraryMetadataProductionCanaryMode = CanonicalLibraryMetadataProductionCanaryMode.DISABLED,
    rootMode: CanonicalLibraryMetadataProductionCanaryRootMode = CanonicalLibraryMetadataProductionCanaryRootMode.DISABLED,
    policy: CanonicalLibraryMetadataProductionCanaryPolicy = CanonicalLibraryMetadataProductionCanaryPolicy.STRICT_LIBRARY_METADATA_N1,
    explicitInternalDebugConfiguration: boolean = false,
    allowProductionRootWrites: boolean = false
  ) {
    this.mode = mode
    this.rootMode = rootMode
    this.policy = policy
    this.explicitInternalDebugConfiguration = explicitInternalDebugConfiguration
    this.allowProductionRootWrites = allowProductionRootWrites
  }

  static readonly DISABLED = new CanonicalLibraryMetadataProductionCanaryConfiguration()

  get strictExecutableN1(): boolean {
    return this.mode === CanonicalLibraryMetadataProductionCanaryMode.CANARY_N1_EXECUTE &&
      (this.rootMode === CanonicalLibraryMetadataProductionCanaryRootMode.TEST_ROOT ||
       this.rootMode === CanonicalLibraryMetadataProductionCanaryRootMode.PRODUCTION_ROOT_EXPLICIT) &&
      this.explicitInternalDebugConfiguration &&
      this.policy.isStrictN1LibraryMetadata &&
      (this.rootMode === CanonicalLibraryMetadataProductionCanaryRootMode.TEST_ROOT
        ? !this.allowProductionRootWrites : this.allowProductionRootWrites)
  }
}

// ── Production Root Blocker ──

export enum CanonicalLibraryMetadataProductionRootBlocker {
  MISSING_EXPLICIT_DEBUG_INTERNAL_CONFIGURATION = 'missingExplicitDebugInternalConfiguration',
  MODE_NOT_EXECUTE_N1_CANARY = 'modeNotExecuteN1Canary',
  ROOT_MODE_NOT_PRODUCTION_ROOT_EXPLICIT = 'rootModeNotProductionRootExplicit',
  ALLOW_PRODUCTION_ROOT_WRITES_FALSE = 'allowProductionRootWritesFalse',
  MISSING_OWNER_APPROVAL = 'missingOwnerApproval',
  ACTIVE_PILOT_NOT_LIBRARY_METADATA = 'activePilotNotLibraryMetadata',
  MULTIPLE_ACTIVE_PILOTS = 'multipleActivePilots',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  LANDING_FREEZE_NOT_GREEN = 'landingFreezeNotGreen',
  DIAGNOSTICS_ONLY_EVIDENCE_MISSING = 'diagnosticsOnlyEvidenceMissing',
  ARM_N1_EVIDENCE_MISSING = 'armN1EvidenceMissing',
  TEST_ROOT_EXECUTE_EVIDENCE_MISSING = 'testRootExecuteEvidenceMissing',
  READ_SIDE_DIVERGENCE_NON_ZERO = 'readSideDivergenceNonZero',
  ROLLBACK_EVIDENCE_MISSING = 'rollbackEvidenceMissing',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  SAFE_CANDIDATE_MISSING = 'safeCandidateMissing',
  MULTIPLE_SAFE_CANDIDATES_DENIED = 'multipleSafeCandidatesDenied',
  UNSAFE_CANDIDATE_SELECTED = 'unsafeCandidateSelected',
  NO_RESOURCE_MOVE_GUARD_MISSING = 'noResourceMoveGuardMissing',
  RESOURCE_MOVE_ATTEMPTED = 'resourceMoveAttempted',
  NO_CONTENT_WRITE_GUARD_MISSING = 'noContentWriteGuardMissing',
  CONTENT_WRITE_ATTEMPTED = 'contentWriteAttempted',
  TOMBSTONE_DELETE_ATTEMPTED = 'tombstoneDeleteAttempted',
  PRODUCTION_ROOT_CONTAINMENT_UNVERIFIED = 'productionRootContainmentUnverified',
  CHECKPOINT_UNAVAILABLE = 'checkpointUnavailable',
  POSTCONDITION_VERIFICATION_UNAVAILABLE = 'postconditionVerificationUnavailable',
  N1_BUDGET_REQUIRED = 'n1BudgetRequired',
  ALL_ELIGIBLE_DENIED = 'allEligibleDenied',
  NON_LIBRARY_METADATA_DOMAIN = 'nonLibraryMetadataDomain',
  RELEASE_DEFAULT_DENIED = 'releaseDefaultDenied',
  DEFAULT_ENABLEMENT_DENIED = 'defaultEnablementDenied',
  LOCAL_SNAPSHOT_UNAVAILABLE = 'localSnapshotUnavailable',
  PEER_SNAPSHOT_UNAVAILABLE = 'peerSnapshotUnavailable',
  COMMIT_EXECUTOR_UNAVAILABLE = 'commitExecutorUnavailable',
  UNSUPPORTED_TRIGGER = 'unsupportedTrigger',
  PRODUCTION_PORT_UNAVAILABLE = 'productionPortUnavailable',
  REAL_APPLY_PORT_UNAVAILABLE = 'realApplyPortUnavailable',
  ROOT_BOUND_WRITE_UNAVAILABLE = 'rootBoundWriteUnavailable',
  ATOMIC_WRITE_UNAVAILABLE = 'atomicWriteUnavailable'
}

// ── Production Root Gate Result ──

export class CanonicalLibraryMetadataProductionRootGateResult {
  allowed: boolean
  blockers: CanonicalLibraryMetadataProductionRootBlocker[]
  selectedCandidate: CanonicalLibraryMetadataCanaryCandidate | null
  selectedCandidateSafety: CanonicalLibraryMetadataCanaryCandidateSafety | null
  freezeResult: CanonicalMigrationLandingFreezeResult
  diagnosticsSummary: string
  redacted: boolean

  constructor(
    blockers: CanonicalLibraryMetadataProductionRootBlocker[] = [],
    selectedCandidate: CanonicalLibraryMetadataCanaryCandidate | null = null,
    selectedCandidateSafety: CanonicalLibraryMetadataCanaryCandidateSafety | null = null,
    freezeResult: CanonicalMigrationLandingFreezeResult = new CanonicalMigrationLandingFreezeResult()
  ) {
    const uniqueBlockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
    this.allowed = uniqueBlockers.length === 0
    this.blockers = uniqueBlockers
    this.selectedCandidate = selectedCandidate
    this.selectedCandidateSafety = selectedCandidateSafety
    this.freezeResult = freezeResult
    this.diagnosticsSummary = [
      'allowed=' + this.allowed, 'rootMode=productionRootExplicit',
      'candidateKind=' + (selectedCandidateSafety?.kind ?? 'none'),
      'objectKind=' + (selectedCandidate?.objectKind ?? 'none'),
      'domain=' + (selectedCandidate?.domain ?? 'none'),
      'blockers=' + uniqueBlockers.join('|'), 'redacted=true'
    ].join(',')
    this.redacted = true
  }
}

// ── Production Root Safety Proof ──

export class CanonicalLibraryMetadataProductionRootSafetyProof {
  rootContainmentVerified: boolean
  productionRootModeExplicit: boolean
  logicalTokenSafety: boolean
  checkpointID: string | null
  atomicWriteUsed: boolean
  postconditionVerified: boolean
  rollbackAvailable: boolean
  rollbackVerifiedIfUsed: boolean
  sideEffectWhitelistPassed: boolean
  noResourceMove: boolean
  noContentWrite: boolean
  noOtherDomainMutation: boolean
  redactedTargetSummary: string
  redacted: boolean

  constructor(
    rootContainmentVerified: boolean = false,
    productionRootModeExplicit: boolean = false,
    logicalTokenSafety: boolean = false,
    checkpointID: string | null = null,
    atomicWriteUsed: boolean = false,
    postconditionVerified: boolean = false,
    rollbackAvailable: boolean = false,
    rollbackVerifiedIfUsed: boolean = false,
    sideEffectWhitelistPassed: boolean = false,
    noResourceMove: boolean = false,
    noContentWrite: boolean = false,
    noOtherDomainMutation: boolean = false,
    redactedTargetSummary: string = '',
    redacted: boolean = false
  ) {
    this.rootContainmentVerified = rootContainmentVerified
    this.productionRootModeExplicit = productionRootModeExplicit
    this.logicalTokenSafety = logicalTokenSafety
    this.checkpointID = checkpointID ? CanonicalProductionRedaction.safeIdentifier(checkpointID, 'library-metadata-checkpoint') : null
    this.atomicWriteUsed = atomicWriteUsed
    this.postconditionVerified = postconditionVerified
    this.rollbackAvailable = rollbackAvailable
    this.rollbackVerifiedIfUsed = rollbackVerifiedIfUsed
    this.sideEffectWhitelistPassed = sideEffectWhitelistPassed
    this.noResourceMove = noResourceMove
    this.noContentWrite = noContentWrite
    this.noOtherDomainMutation = noOtherDomainMutation
    this.redactedTargetSummary = CanonicalProductionRedaction.safeDiagnosticText(redactedTargetSummary) ?? 'redacted'
    this.redacted = redacted
  }
}

// ── Production Canary Injection Result ──

export interface CanonicalCutoverToken {
  tokenID: string
  syncRunID: string
  ownerApproved: boolean
}

export class CanonicalLibraryMetadataProductionCanaryInjectionResult {
  configuration: CanonicalLibraryMetadataProductionCanaryConfiguration
  injectionConfigured: boolean
  executorInjected: boolean
  applyPortInjected: boolean
  armed: boolean
  executed: boolean
  succeeded: boolean
  blockers: CanonicalLibraryMetadataRealCanaryBlocker[]
  diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[]
  canaryResult: any | null
  cutoverResult: CanonicalLibraryMetadataCutoverResult | null
  selection: CanonicalLibraryMetadataCanarySelectionResult | null
  candidateSafetyReports: CanonicalLibraryMetadataCanaryCandidateSafety[] | null
  productionRootGate: CanonicalLibraryMetadataProductionRootGateResult | null
  productionRootSafetyProof: CanonicalLibraryMetadataProductionRootSafetyProof | null
  observationReport: CanonicalLibraryMetadataRealCanaryObservationReport

  constructor(
    configuration: CanonicalLibraryMetadataProductionCanaryConfiguration = new CanonicalLibraryMetadataProductionCanaryConfiguration(),
    injectionConfigured: boolean = false,
    executorInjected: boolean = false,
    applyPortInjected: boolean = false,
    armed: boolean = false,
    executed: boolean = false,
    succeeded: boolean = false,
    blockers: CanonicalLibraryMetadataRealCanaryBlocker[] = [],
    diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[] = [],
    canaryResult: any | null = null,
    cutoverResult: CanonicalLibraryMetadataCutoverResult | null = null,
    selection: CanonicalLibraryMetadataCanarySelectionResult | null = null,
    candidateSafetyReports: CanonicalLibraryMetadataCanaryCandidateSafety[] | null = null,
    productionRootGate: CanonicalLibraryMetadataProductionRootGateResult | null = null,
    productionRootSafetyProof: CanonicalLibraryMetadataProductionRootSafetyProof | null = null,
    observationReport: CanonicalLibraryMetadataRealCanaryObservationReport = new CanonicalLibraryMetadataRealCanaryObservationReport()
  ) {
    this.configuration = configuration
    this.injectionConfigured = injectionConfigured
    this.executorInjected = executorInjected
    this.applyPortInjected = applyPortInjected
    this.armed = armed
    this.executed = executed
    this.succeeded = succeeded
    this.blockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
    this.diagnostics = diagnostics
    this.canaryResult = canaryResult
    this.cutoverResult = cutoverResult
    this.selection = selection
    this.candidateSafetyReports = candidateSafetyReports
    this.productionRootGate = productionRootGate
    this.productionRootSafetyProof = productionRootSafetyProof
    this.observationReport = observationReport
  }
}

// ── Production Root Gate ──

export class CanonicalLibraryMetadataProductionRootGate {
  constructor() {}

  evaluate(
    configuration: CanonicalLibraryMetadataProductionCanaryConfiguration,
    token: CanonicalCutoverToken | null,
    evidence: CanonicalLibraryMetadataCutoverEvidence,
    matrix: CanonicalMigrationDomainMatrix,
    candidates: CanonicalLibraryMetadataCutoverCandidate[],
    trigger: CanonicalSyncPlanTrigger,
    localSnapshotAvailable: boolean,
    peerSnapshotAvailable: boolean,
    executorAvailable: boolean
  ): CanonicalLibraryMetadataProductionRootGateResult {
    const blockers: CanonicalLibraryMetadataProductionRootBlocker[] = []
    if (!configuration.explicitInternalDebugConfiguration) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.MISSING_EXPLICIT_DEBUG_INTERNAL_CONFIGURATION)
    if (configuration.mode !== CanonicalLibraryMetadataProductionCanaryMode.CANARY_N1_EXECUTE) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.MODE_NOT_EXECUTE_N1_CANARY)
    if (configuration.rootMode !== CanonicalLibraryMetadataProductionCanaryRootMode.PRODUCTION_ROOT_EXPLICIT) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.ROOT_MODE_NOT_PRODUCTION_ROOT_EXPLICIT)
    if (!configuration.allowProductionRootWrites) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.ALLOW_PRODUCTION_ROOT_WRITES_FALSE)
    if (configuration.policy.domain !== CanonicalMigrationDomain.LIBRARY_METADATA) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.NON_LIBRARY_METADATA_DOMAIN)
    if (configuration.policy.canaryMaxObjectsPerSyncRun !== 1) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.N1_BUDGET_REQUIRED)
    if (configuration.policy.runtimeSwitchEnabled) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.RUNTIME_SWITCH_ENABLED)
    if (configuration.policy.allowAllEligible) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.ALL_ELIGIBLE_DENIED)
    if (configuration.policy.releaseDefaultEnabled) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.RELEASE_DEFAULT_DENIED)
    if (token?.ownerApproved !== true) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.MISSING_OWNER_APPROVAL)
    if (!localSnapshotAvailable) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.LOCAL_SNAPSHOT_UNAVAILABLE)
    if (!peerSnapshotAvailable) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.PEER_SNAPSHOT_UNAVAILABLE)
    if (!executorAvailable) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.COMMIT_EXECUTOR_UNAVAILABLE)
    if (trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH || trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.UNSUPPORTED_TRIGGER)

    const freeze = new CanonicalMigrationLandingFreeze().evaluate(matrix,
      configuration.policy.releaseDefaultEnabled, configuration.policy.runtimeSwitchEnabled,
      false, false, false, evidence.legacyFallbackAvailable,
      configuration.policy.canaryMaxObjectsPerSyncRun, configuration.policy.allowAllEligible,
      false, false, false, false)
    if (!freeze.allowed) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.LANDING_FREEZE_NOT_GREEN)
    if (freeze.activePilotDomain !== CanonicalMigrationDomain.LIBRARY_METADATA) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.ACTIVE_PILOT_NOT_LIBRARY_METADATA)
    if (matrix.policies.filter(p => p.activePilot).length > 1) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.MULTIPLE_ACTIVE_PILOTS)
    if (matrix.policies.some(p => p.defaultCutoverEnabled)) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.DEFAULT_ENABLEMENT_DENIED)
    if (!evidence.noCommitEvidenceAvailable || !evidence.dryRunEquivalenceVerified) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.DIAGNOSTICS_ONLY_EVIDENCE_MISSING)
    if (!evidence.realDataShadowCopyVerified || !evidence.executionShadowVerified || !evidence.metadataManifestRouteEvidenceAvailable) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.ARM_N1_EVIDENCE_MISSING)
    if (!evidence.testRootUsed) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.TEST_ROOT_EXECUTE_EVIDENCE_MISSING)
    if (!evidence.readSideParallelEquivalent) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.READ_SIDE_DIVERGENCE_NON_ZERO)
    if (!evidence.rollbackCheckpointAvailable || !evidence.rollbackVerified || !evidence.rollbackRehearsalPassed) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.ROLLBACK_EVIDENCE_MISSING)
    if (!evidence.legacyFallbackAvailable) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.LEGACY_FALLBACK_UNAVAILABLE)
    if (!evidence.productionPortAvailable) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.PRODUCTION_PORT_UNAVAILABLE)
    if (!evidence.realRootBoundApplyPortAvailable) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.REAL_APPLY_PORT_UNAVAILABLE)
    if (evidence.applyPortMode !== 'productionRootBound') blockers.push(CanonicalLibraryMetadataProductionRootBlocker.PRODUCTION_ROOT_CONTAINMENT_UNVERIFIED)
    if (!evidence.rootBoundWriteAvailable) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.ROOT_BOUND_WRITE_UNAVAILABLE)
    if (!evidence.atomicReplaceAvailable) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.ATOMIC_WRITE_UNAVAILABLE)

    const selector = new CanonicalLibraryMetadataCanarySelector().select(CanonicalCutoverMode.CANARY,
      new CanonicalLibraryMetadataCanaryPolicy(new CanonicalLibraryMetadataCanaryStagePolicy(), 1, true, true, false, false),
      trigger, evidence, candidates)
    const safetyReports = candidates.map(c => new CanonicalLibraryMetadataCanaryCandidateSafety(c, evidence))
    const safeReports = safetyReports.filter(s => s.safe)
    if (safeReports.length === 0) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.SAFE_CANDIDATE_MISSING)
    if (safeReports.length > 1) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.MULTIPLE_SAFE_CANDIDATES_DENIED)
    if (selector.selectedCandidates.length !== 1) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.SAFE_CANDIDATE_MISSING)
    if (safetyReports.some(s => !s.safe)) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.UNSAFE_CANDIDATE_SELECTED)
    if (safetyReports.some(s => s.resourceMoveAttempted)) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.RESOURCE_MOVE_ATTEMPTED)
    if (safetyReports.some(s => s.contentBytesMutated)) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.CONTENT_WRITE_ATTEMPTED)
    if (safetyReports.some(s => s.physicalDeleteAttempted)) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.TOMBSTONE_DELETE_ATTEMPTED)
    const selected = selector.selectedCandidates[0] ?? null
    const selectedSafety = selected ? safetyReports.find(s => s.candidate.id === selected.id) ?? null : null
    if (!selected || !selectedSafety) {
      return new CanonicalLibraryMetadataProductionRootGateResult(blockers, null, null, freeze)
    }
    if (!selectedSafety.metadataOnly) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.NO_CONTENT_WRITE_GUARD_MISSING)
    if (selectedSafety.resourceMoveAttempted) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.NO_RESOURCE_MOVE_GUARD_MISSING)
    if (selectedSafety.contentBytesMutated) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.CONTENT_WRITE_ATTEMPTED)
    if (selectedSafety.physicalDeleteAttempted) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.TOMBSTONE_DELETE_ATTEMPTED)
    if (!selected.cutoverCandidate.rollbackCheckpointID) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.CHECKPOINT_UNAVAILABLE)
    if (!evidence.atomicReplaceAvailable || !evidence.rootBoundWriteAvailable) blockers.push(CanonicalLibraryMetadataProductionRootBlocker.POSTCONDITION_VERIFICATION_UNAVAILABLE)
    return new CanonicalLibraryMetadataProductionRootGateResult(blockers, selected, selectedSafety, freeze)
  }
}
