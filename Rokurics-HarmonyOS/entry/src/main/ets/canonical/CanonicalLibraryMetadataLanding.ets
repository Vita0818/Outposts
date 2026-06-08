/**
 * CanonicalLibraryMetadataLanding — debug pilot bootstrap and landing freeze.
 * Ported from Rokurics-Apple (CanonicalLibraryMetadataLanding.swift).
 */

import {
  CanonicalLibraryObjectID, CanonicalObjectKind, CanonicalHash,
  CanonicalLibraryObject, nilIfEmpty, hashPrefix
} from './CanonicalCore'
import {
  CanonicalProductionDomain,
  CanonicalMigrationDomain,
  CanonicalMigrationDomainMatrix,
  CanonicalLibraryMetadataCutoverDomain,
  CanonicalLibraryMetadataCutoverActionKind,
  CanonicalLibraryMetadataCutoverFailure,
  CanonicalLibraryMetadataCutoverEvidence,
  CanonicalLibraryMetadataCutoverCandidate,
  CanonicalLibraryMetadataCutoverGate,
  CanonicalLibraryMetadataCutoverResult,
  CanonicalLibraryMetadataCutoverDiagnostic,
  CanonicalLibraryMetadataCutoverDiagnosticKind,
  CanonicalLibraryMetadataCanaryCandidateSafetyKind,
  CanonicalLibraryMetadataStageEvidenceReport,
  CanonicalLibraryMetadataProductionCommitResult,
  CanonicalLibraryMetadataRollbackExecutionResult,
  CanonicalProductionExecutionDomainRole,
  CanonicalCutoverMode
} from './CanonicalLibraryMetadataCutover'
import { CanonicalSyncPlanTrigger } from './CanonicalLibrarySyncPlanner'

// ── Types from other files (minimal definitions) ──

export interface CanonicalCutoverToken {
  tokenID: string
  syncRunID: string
  ownerApproved: boolean
}

// ── Landing Freeze Violation ──

export enum CanonicalMigrationLandingFreezeViolation {
  NO_LIBRARY_METADATA_ACTIVE_PILOT = 'noLibraryMetadataActivePilot',
  MULTIPLE_ACTIVE_PILOTS = 'multipleActivePilots',
  NON_LIBRARY_METADATA_ACTIVE_PILOT = 'nonLibraryMetadataActivePilot',
  NON_LIBRARY_METADATA_DOMAIN_NOT_STATIC_ONLY = 'nonLibraryMetadataDomainNotStaticOnly',
  GENERATED_ARTIFACTS_NOT_STATIC_ONLY = 'generatedArtifactsNotStaticOnly',
  TOMBSTONE_CONFLICT_NOT_STATIC_ONLY = 'tombstoneConflictNotStaticOnly',
  AUDIO_UPLOAD_NOT_STATIC_ONLY = 'audioUploadNotStaticOnly',
  RECORDING_METADATA_ACTIVE = 'recordingMetadataActive',
  DEFAULT_CUTOVER_ENABLED = 'defaultCutoverEnabled',
  RELEASE_DEFAULT_ENABLED = 'releaseDefaultEnabled',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  LEGACY_DUPLICATE_SUPPRESSION_ENABLED = 'legacyDuplicateSuppressionEnabled',
  READ_PATH_NOT_LEGACY = 'readPathNotLegacy',
  PRODUCTION_INJECTION_PRESENT = 'productionInjectionPresent',
  PRODUCTION_EXECUTOR_INJECTED_BY_DEFAULT = 'productionExecutorInjectedByDefault',
  PRODUCTION_ROOT_WRITE_ENABLED_BY_DEFAULT = 'productionRootWriteEnabledByDefault',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  CANARY_BUDGET_ABOVE_ONE_DENIED = 'canaryBudgetAboveOneDenied',
  ALL_ELIGIBLE_ENABLED = 'allEligibleEnabled',
  UNSAFE_CANDIDATE_ALLOWED = 'unsafeCandidateAllowed',
  RESOURCE_MOVE_ALLOWED = 'resourceMoveAllowed',
  CONTENT_WRITE_ALLOWED = 'contentWriteAllowed',
  TOMBSTONE_DELETE_ALLOWED = 'tombstoneDeleteAllowed'
}

export class CanonicalMigrationLandingFreezeResult {
  allowed: boolean
  activePilotDomain: CanonicalMigrationDomain | null
  violations: CanonicalMigrationLandingFreezeViolation[]
  otherDomainsStaticOnly: boolean
  runtimeSwitchEnabled: boolean
  diagnosticsSummary: string
  redacted: boolean

  constructor(
    allowed: boolean = false,
    activePilotDomain: CanonicalMigrationDomain | null = null,
    violations: CanonicalMigrationLandingFreezeViolation[] = [],
    otherDomainsStaticOnly: boolean = false,
    runtimeSwitchEnabled: boolean = false,
    diagnosticsSummary: string = '',
    redacted: boolean = true
  ) {
    this.allowed = allowed
    this.activePilotDomain = activePilotDomain
    this.violations = violations
    this.otherDomainsStaticOnly = otherDomainsStaticOnly
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.diagnosticsSummary = diagnosticsSummary
    this.redacted = redacted
  }
}

export class CanonicalMigrationLandingFreeze {
  constructor() {}

  evaluate(
    matrix: CanonicalMigrationDomainMatrix,
    releaseDefaultEnabled: boolean = false,
    runtimeSwitchEnabled: boolean = false,
    productionInjectionPresent: boolean = false,
    productionExecutorInjectedByDefault: boolean = false,
    productionRootWriteEnabledByDefault: boolean = false,
    legacyFallbackAvailable: boolean = true,
    canaryMaxObjectsPerSyncRun: number = 1,
    allEligibleEnabled: boolean = false,
    unsafeCandidateAllowed: boolean = false,
    resourceMoveAllowed: boolean = false,
    contentWriteAllowed: boolean = false,
    tombstoneDeleteAllowed: boolean = false
  ): CanonicalMigrationLandingFreezeResult {
    const violations: CanonicalMigrationLandingFreezeViolation[] = []
    const activePolicies = matrix.policies.filter(p => p.activePilot)
    const activePilotDomain = activePolicies.length === 1 ? activePolicies[0].domain : null

    if (activePolicies.length === 0) {
      violations.push(CanonicalMigrationLandingFreezeViolation.NO_LIBRARY_METADATA_ACTIVE_PILOT)
    }
    if (activePolicies.length > 1) {
      violations.push(CanonicalMigrationLandingFreezeViolation.MULTIPLE_ACTIVE_PILOTS)
    }
    if (activePolicies.some(p => p.domain !== CanonicalMigrationDomain.LIBRARY_METADATA)) {
      violations.push(CanonicalMigrationLandingFreezeViolation.NON_LIBRARY_METADATA_ACTIVE_PILOT)
    }

    const nonLibraryPolicies = matrix.policies.filter(p => p.domain !== CanonicalMigrationDomain.LIBRARY_METADATA)
    if (nonLibraryPolicies.some(p => !p.staticOnly || !p.blockedForRealMigration)) {
      violations.push(CanonicalMigrationLandingFreezeViolation.NON_LIBRARY_METADATA_DOMAIN_NOT_STATIC_ONLY)
    }
    if (matrix.policyFor(CanonicalMigrationDomain.GENERATED_ARTIFACTS)?.staticOnly === false ||
      matrix.policyFor(CanonicalMigrationDomain.GENERATED_ARTIFACTS)?.activePilot === true) {
      violations.push(CanonicalMigrationLandingFreezeViolation.GENERATED_ARTIFACTS_NOT_STATIC_ONLY)
    }
    if (matrix.policyFor(CanonicalMigrationDomain.TOMBSTONE_CONFLICT)?.staticOnly === false ||
      matrix.policyFor(CanonicalMigrationDomain.TOMBSTONE_CONFLICT)?.activePilot === true) {
      violations.push(CanonicalMigrationLandingFreezeViolation.TOMBSTONE_CONFLICT_NOT_STATIC_ONLY)
    }
    if (matrix.policyFor(CanonicalMigrationDomain.AUDIO_UPLOAD)?.staticOnly === false ||
      matrix.policyFor(CanonicalMigrationDomain.AUDIO_UPLOAD)?.activePilot === true) {
      violations.push(CanonicalMigrationLandingFreezeViolation.AUDIO_UPLOAD_NOT_STATIC_ONLY)
    }
    if (matrix.policyFor(CanonicalMigrationDomain.RECORDING_METADATA)?.activePilot === true ||
      matrix.policyFor(CanonicalMigrationDomain.RECORDING_METADATA)?.staticOnly === false) {
      violations.push(CanonicalMigrationLandingFreezeViolation.RECORDING_METADATA_ACTIVE)
    }
    if (matrix.policies.some(p => p.defaultCutoverEnabled)) {
      violations.push(CanonicalMigrationLandingFreezeViolation.DEFAULT_CUTOVER_ENABLED)
    }
    if (matrix.policies.some(p => p.releaseDefaultEnabledCutover) || releaseDefaultEnabled) {
      violations.push(CanonicalMigrationLandingFreezeViolation.RELEASE_DEFAULT_ENABLED)
    }
    if (matrix.policies.some(p => p.runtimeSwitchEnabled) || runtimeSwitchEnabled) {
      violations.push(CanonicalMigrationLandingFreezeViolation.RUNTIME_SWITCH_ENABLED)
    }
    if (matrix.policies.some(p => p.legacySuppressionAllowed)) {
      violations.push(CanonicalMigrationLandingFreezeViolation.LEGACY_DUPLICATE_SUPPRESSION_ENABLED)
    }
    if (matrix.policies.some(p => !p.readPathLegacy)) {
      violations.push(CanonicalMigrationLandingFreezeViolation.READ_PATH_NOT_LEGACY)
    }
    if (matrix.policies.some(p => !p.noProductionInjection && p.domain !== CanonicalMigrationDomain.LIBRARY_METADATA) ||
      productionInjectionPresent) {
      violations.push(CanonicalMigrationLandingFreezeViolation.PRODUCTION_INJECTION_PRESENT)
    }
    if (productionExecutorInjectedByDefault || productionInjectionPresent) {
      violations.push(CanonicalMigrationLandingFreezeViolation.PRODUCTION_EXECUTOR_INJECTED_BY_DEFAULT)
    }
    if (productionRootWriteEnabledByDefault) {
      violations.push(CanonicalMigrationLandingFreezeViolation.PRODUCTION_ROOT_WRITE_ENABLED_BY_DEFAULT)
    }
    if (!legacyFallbackAvailable) {
      violations.push(CanonicalMigrationLandingFreezeViolation.LEGACY_FALLBACK_UNAVAILABLE)
    }
    if (canaryMaxObjectsPerSyncRun > 1) {
      violations.push(CanonicalMigrationLandingFreezeViolation.CANARY_BUDGET_ABOVE_ONE_DENIED)
    }
    if (allEligibleEnabled) {
      violations.push(CanonicalMigrationLandingFreezeViolation.ALL_ELIGIBLE_ENABLED)
    }
    if (unsafeCandidateAllowed) {
      violations.push(CanonicalMigrationLandingFreezeViolation.UNSAFE_CANDIDATE_ALLOWED)
    }
    if (resourceMoveAllowed) {
      violations.push(CanonicalMigrationLandingFreezeViolation.RESOURCE_MOVE_ALLOWED)
    }
    if (contentWriteAllowed) {
      violations.push(CanonicalMigrationLandingFreezeViolation.CONTENT_WRITE_ALLOWED)
    }
    if (tombstoneDeleteAllowed) {
      violations.push(CanonicalMigrationLandingFreezeViolation.TOMBSTONE_DELETE_ALLOWED)
    }

    const uniqueViolations = [...new Set(violations)].sort((a, b) => a.localeCompare(b))
    const otherDomainsStaticOnly = nonLibraryPolicies.every(p =>
      p.staticOnly && p.blockedForRealMigration && !p.activePilot &&
      !p.defaultCutoverEnabled && !p.releaseDefaultEnabledCutover &&
      !p.runtimeSwitchEnabled && !p.legacySuppressionAllowed && p.readPathLegacy)
    const runtimeEnabled = matrix.policies.some(p => p.runtimeSwitchEnabled) || runtimeSwitchEnabled
    return new CanonicalMigrationLandingFreezeResult(
      uniqueViolations.length === 0,
      activePilotDomain,
      uniqueViolations,
      otherDomainsStaticOnly,
      runtimeEnabled,
      [
        'activePilot=' + (activePilotDomain ?? 'none'),
        'otherDomainsStaticOnly=' + otherDomainsStaticOnly,
        'runtimeSwitch=' + runtimeEnabled,
        'legacyFallbackAvailable=' + legacyFallbackAvailable,
        'canaryMaxObjectsPerSyncRun=' + canaryMaxObjectsPerSyncRun,
        'violations=' + uniqueViolations.join('|'),
        'redacted=true'
      ].join(','),
      true
    )
  }
}

// ── Debug Pilot Mode ──

export enum CanonicalLibraryMetadataDebugPilotMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  ARM_N1_CANARY = 'armN1Canary',
  EXECUTE_N1_CANARY = 'executeN1Canary',
  BLOCKED = 'blocked'
}

// ── Production Canary Root Mode (minimal) ──

export enum CanonicalLibraryMetadataProductionCanaryRootMode {
  DISABLED = 'disabled',
  TEST_ROOT = 'testRoot',
  PRODUCTION_ROOT_EXPLICIT = 'productionRootExplicit'
}

// ── Debug Pilot Policy ──

export class CanonicalLibraryMetadataDebugPilotPolicy {
  domain: CanonicalMigrationDomain
  canaryMaxObjectsPerSyncRun: number
  requiresExplicitInternalDebugConfiguration: boolean
  requiresProductionToken: boolean
  requiresOwnerApproval: boolean
  requiresRollbackPlan: boolean
  requiresReadSideParallelEquivalent: boolean
  requiresObservationEvidence: boolean
  requiresRealRootBoundApplyPort: boolean
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
    requiresObservationEvidence: boolean = true,
    requiresRealRootBoundApplyPort: boolean = true,
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
    this.requiresObservationEvidence = requiresObservationEvidence
    this.requiresRealRootBoundApplyPort = requiresRealRootBoundApplyPort
    this.productionRootDisabledByDefault = productionRootDisabledByDefault
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.allowAllEligible = allowAllEligible
    this.releaseDefaultEnabled = releaseDefaultEnabled
  }

  static readonly STRICT_LIBRARY_METADATA_N1 = new CanonicalLibraryMetadataDebugPilotPolicy()

  get isStrictLibraryMetadataN1(): boolean {
    return this.domain === CanonicalMigrationDomain.LIBRARY_METADATA &&
      this.canaryMaxObjectsPerSyncRun === 1 &&
      this.requiresExplicitInternalDebugConfiguration &&
      this.requiresProductionToken &&
      this.requiresOwnerApproval &&
      this.requiresRollbackPlan &&
      this.requiresReadSideParallelEquivalent &&
      this.requiresObservationEvidence &&
      this.requiresRealRootBoundApplyPort &&
      this.productionRootDisabledByDefault &&
      !this.runtimeSwitchEnabled &&
      !this.allowAllEligible &&
      !this.releaseDefaultEnabled
  }
}

// ── Debug Pilot Configuration ──

export class CanonicalLibraryMetadataDebugPilotConfiguration {
  mode: CanonicalLibraryMetadataDebugPilotMode
  rootMode: CanonicalLibraryMetadataProductionCanaryRootMode
  policy: CanonicalLibraryMetadataDebugPilotPolicy
  explicitInternalDebugConfiguration: boolean
  allowProductionRootWrites: boolean
  evidence: CanonicalLibraryMetadataCutoverEvidence
  cutoverToken: CanonicalCutoverToken | null
  recordDiagnostics: boolean
  maxDiagnosticsEvents: number

  constructor(
    mode: CanonicalLibraryMetadataDebugPilotMode = CanonicalLibraryMetadataDebugPilotMode.DISABLED,
    rootMode: CanonicalLibraryMetadataProductionCanaryRootMode = CanonicalLibraryMetadataProductionCanaryRootMode.DISABLED,
    policy: CanonicalLibraryMetadataDebugPilotPolicy = CanonicalLibraryMetadataDebugPilotPolicy.STRICT_LIBRARY_METADATA_N1,
    explicitInternalDebugConfiguration: boolean = false,
    allowProductionRootWrites: boolean = false,
    evidence: CanonicalLibraryMetadataCutoverEvidence = new CanonicalLibraryMetadataCutoverEvidence(),
    cutoverToken: CanonicalCutoverToken | null = null,
    recordDiagnostics: boolean = true,
    maxDiagnosticsEvents: number = 200
  ) {
    this.mode = mode
    this.rootMode = rootMode
    this.policy = policy
    this.explicitInternalDebugConfiguration = explicitInternalDebugConfiguration
    this.allowProductionRootWrites = allowProductionRootWrites
    this.evidence = evidence
    this.cutoverToken = cutoverToken
    this.recordDiagnostics = recordDiagnostics
    this.maxDiagnosticsEvents = Math.max(1, maxDiagnosticsEvents)
  }

  static readonly DISABLED = new CanonicalLibraryMetadataDebugPilotConfiguration()

  static diagnosticsOnly(evidence: CanonicalLibraryMetadataCutoverEvidence = new CanonicalLibraryMetadataCutoverEvidence()): CanonicalLibraryMetadataDebugPilotConfiguration {
    return new CanonicalLibraryMetadataDebugPilotConfiguration(
      CanonicalLibraryMetadataDebugPilotMode.DIAGNOSTICS_ONLY,
      CanonicalLibraryMetadataProductionCanaryRootMode.DISABLED,
      undefined,
      true,
      false,
      evidence
    )
  }

  static armTestRootN1(token: CanonicalCutoverToken, evidence: CanonicalLibraryMetadataCutoverEvidence): CanonicalLibraryMetadataDebugPilotConfiguration {
    return new CanonicalLibraryMetadataDebugPilotConfiguration(
      CanonicalLibraryMetadataDebugPilotMode.ARM_N1_CANARY,
      CanonicalLibraryMetadataProductionCanaryRootMode.TEST_ROOT,
      undefined,
      true,
      false,
      evidence,
      token
    )
  }

  static executeTestRootN1(token: CanonicalCutoverToken, evidence: CanonicalLibraryMetadataCutoverEvidence): CanonicalLibraryMetadataDebugPilotConfiguration {
    return new CanonicalLibraryMetadataDebugPilotConfiguration(
      CanonicalLibraryMetadataDebugPilotMode.EXECUTE_N1_CANARY,
      CanonicalLibraryMetadataProductionCanaryRootMode.TEST_ROOT,
      undefined,
      true,
      false,
      evidence,
      token
    )
  }

  get isStrictExecutableN1(): boolean {
    return this.mode === CanonicalLibraryMetadataDebugPilotMode.EXECUTE_N1_CANARY &&
      (this.rootMode === CanonicalLibraryMetadataProductionCanaryRootMode.TEST_ROOT ||
       this.rootMode === CanonicalLibraryMetadataProductionCanaryRootMode.PRODUCTION_ROOT_EXPLICIT) &&
      this.explicitInternalDebugConfiguration &&
      this.policy.isStrictLibraryMetadataN1 &&
      (this.rootMode === CanonicalLibraryMetadataProductionCanaryRootMode.TEST_ROOT
        ? !this.allowProductionRootWrites : this.allowProductionRootWrites)
  }
}

// ── Landing Status / Report ──

export enum CanonicalLibraryMetadataLandingStatus {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  ARMED = 'armed',
  BLOCKED = 'blocked',
  NO_ELIGIBLE_CANDIDATE = 'noEligibleCandidate',
  UNSAFE_CANDIDATE_SKIPPED = 'unsafeCandidateSkipped',
  EXECUTED_SUCCEEDED = 'executedSucceeded',
  EXECUTED_FAILED_ROLLED_BACK = 'executedFailedRolledBack',
  FATAL_ROLLBACK_FAILURE = 'fatalRollbackFailure'
}

export enum CanonicalLibraryMetadataLandingRecommendation {
  REMAIN_DISABLED = 'remainDisabled',
  FIX_BLOCKERS = 'fixBlockers',
  RUN_ANOTHER_N1 = 'runAnotherN1',
  CONSIDER_READ_SIDE_CUTOVER_AFTER_AUDIT = 'considerReadSideCutoverAfterAudit'
}

export class CanonicalLibraryMetadataLandingCandidateSummary {
  selected: boolean
  kind: CanonicalLibraryMetadataCanaryCandidateSafetyKind | null
  objectKind: CanonicalObjectKind | null
  domain: CanonicalLibraryMetadataCutoverDomain | null
  actionKind: CanonicalLibraryMetadataCutoverActionKind | null
  metadataOnly: boolean
  resourceMoveAttempted: boolean
  contentBytesMutated: boolean

  constructor(
    selected: boolean = false,
    kind: CanonicalLibraryMetadataCanaryCandidateSafetyKind | null = null,
    objectKind: CanonicalObjectKind | null = null,
    domain: CanonicalLibraryMetadataCutoverDomain | null = null,
    actionKind: CanonicalLibraryMetadataCutoverActionKind | null = null,
    metadataOnly: boolean = false,
    resourceMoveAttempted: boolean = false,
    contentBytesMutated: boolean = false
  ) {
    this.selected = selected
    this.kind = kind
    this.objectKind = objectKind
    this.domain = domain
    this.actionKind = actionKind
    this.metadataOnly = metadataOnly
    this.resourceMoveAttempted = resourceMoveAttempted
    this.contentBytesMutated = contentBytesMutated
  }
}

export class CanonicalLibraryMetadataLandingReport {
  status: CanonicalLibraryMetadataLandingStatus = CanonicalLibraryMetadataLandingStatus.DISABLED
  mode: CanonicalLibraryMetadataDebugPilotMode = CanonicalLibraryMetadataDebugPilotMode.DISABLED
  rootMode: CanonicalLibraryMetadataProductionCanaryRootMode = CanonicalLibraryMetadataProductionCanaryRootMode.DISABLED
  activePilot: CanonicalMigrationDomain | null = null
  candidate: CanonicalLibraryMetadataLandingCandidateSummary = new CanonicalLibraryMetadataLandingCandidateSummary()
  commitAttempted: boolean = false
  commitSucceeded: boolean = false
  rollbackAttempted: boolean = false
  rollbackSucceeded: boolean = false
  legacyFallbackUsed: boolean = false
  duplicateSuppressed: boolean = false
  duplicateSuppressedCount: number = 0
  readSideEquivalent: boolean = false
  readSideDivergenceCount: number = 0
  uiReadPathSwitched: boolean = false
  legacyReadPathPreserved: boolean = true
  otherDomainsStaticOnly: boolean = false
  runtimeSwitchEnabled: boolean = false
  generatedArtifactsStaticOnly: boolean = false
  tombstoneConflictStaticOnly: boolean = false
  audioUploadStaticOnly: boolean = false
  recordingMetadataStaticOnly: boolean = false
  recommendation: CanonicalLibraryMetadataLandingRecommendation = CanonicalLibraryMetadataLandingRecommendation.REMAIN_DISABLED
  freezeViolations: CanonicalMigrationLandingFreezeViolation[] = []
  blockers: string[] = []
  diagnosticsSummary: string = ''
  redacted: boolean = true
}

export class CanonicalLibraryMetadataDebugPilotBootstrapResult {
  configuration: CanonicalLibraryMetadataDebugPilotConfiguration = new CanonicalLibraryMetadataDebugPilotConfiguration()
  freezeResult: CanonicalMigrationLandingFreezeResult = new CanonicalMigrationLandingFreezeResult()
  injectionResult: CanonicalLibraryMetadataRealCanaryBlocker | null = null
  report: CanonicalLibraryMetadataLandingReport = new CanonicalLibraryMetadataLandingReport()
  diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[] = []
  cutoverResult: CanonicalLibraryMetadataCutoverResult | null = null
}

// ── Pilot Diagnostic Exporter ──

export enum CanonicalLibraryMetadataRealCanaryBlocker {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY_NO_EXECUTION = 'diagnosticsOnlyNoExecution',
  ARMED_NO_EXECUTION = 'armedNoExecution',
  BLOCKED_MODE = 'blockedMode',
  NON_LIBRARY_METADATA_DOMAIN = 'nonLibraryMetadataDomain',
  N1_BUDGET_REQUIRED = 'n1BudgetRequired',
  CANARY_BUDGET_ABOVE_ONE_DENIED = 'canaryBudgetAboveOneDenied',
  MISSING_EXPLICIT_INTERNAL_DEBUG_CONFIGURATION = 'missingExplicitInternalDebugConfiguration',
  RUNTIME_SWITCH_DENIED = 'runtimeSwitchDenied',
  ALL_ELIGIBLE_DENIED = 'allEligibleDenied',
  RELEASE_DEFAULT_DENIED = 'releaseDefaultDenied',
  DEFAULT_ENABLEMENT_DENIED = 'defaultEnablementDenied',
  MISSING_TOKEN = 'missingToken',
  MISSING_OWNER_APPROVAL = 'missingOwnerApproval',
  MATRIX_VALIDATION_BLOCKED = 'matrixValidationBlocked',
  ACTIVE_PILOT_NOT_LIBRARY_METADATA = 'activePilotNotLibraryMetadata',
  LOCAL_SNAPSHOT_UNAVAILABLE = 'localSnapshotUnavailable',
  PEER_SNAPSHOT_UNAVAILABLE = 'peerSnapshotUnavailable',
  MISSING_NO_COMMIT_EVIDENCE = 'missingNoCommitEvidence',
  MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE = 'missingRealDataShadowCopyEvidence',
  MISSING_EXECUTION_SHADOW_EVIDENCE = 'missingExecutionShadowEvidence',
  MISSING_DRY_RUN_EQUIVALENCE = 'missingDryRunEquivalence',
  BLOCKING_DIVERGENCE = 'blockingDivergence',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  MISSING_METADATA_MANIFEST_ROUTE_EVIDENCE = 'missingMetadataManifestRouteEvidence',
  PRODUCTION_PORT_UNAVAILABLE = 'productionPortUnavailable',
  REAL_APPLY_PORT_UNAVAILABLE = 'realApplyPortUnavailable',
  APPLY_PORT_DRY_RUN_ONLY = 'applyPortDryRunOnly',
  ROOT_BOUND_WRITE_UNAVAILABLE = 'rootBoundWriteUnavailable',
  ATOMIC_REPLACE_UNAVAILABLE = 'atomicReplaceUnavailable',
  ROLLBACK_CHECKPOINT_UNAVAILABLE = 'rollbackCheckpointUnavailable',
  ROLLBACK_PLAN_MISSING = 'rollbackPlanMissing',
  ROLLBACK_VERIFICATION_MISSING = 'rollbackVerificationMissing',
  PRODUCTION_ROOT_GUARD_MISSING = 'productionRootGuardMissing',
  PRODUCTION_ROOT_WRITES_DISABLED = 'productionRootWritesDisabled',
  PRODUCTION_ROOT_NOT_EXPLICIT = 'productionRootNotExplicit',
  PRODUCTION_ROOT_EXPLICIT_BLOCKED_V830 = 'productionRootExplicitBlockedV830',
  ALLOW_PRODUCTION_ROOT_WRITES_DENIED_V830 = 'allowProductionRootWritesDeniedV830',
  TEST_ROOT_MISSING = 'testRootMissing',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  READ_SIDE_PARALLEL_MISSING = 'readSideParallelMissing',
  READ_SIDE_PARALLEL_DIVERGENT = 'readSideParallelDivergent',
  COMMIT_EXECUTOR_UNAVAILABLE = 'commitExecutorUnavailable',
  UNSUPPORTED_TRIGGER = 'unsupportedTrigger',
  NO_ELIGIBLE_CANDIDATE = 'noEligibleCandidate',
  UNSAFE_CANDIDATE_SKIPPED = 'unsafeCandidateSkipped',
  ROLLBACK_FAILURE = 'rollbackFailure',
  FATAL_BLOCKER = 'fatalBlocker'
}

export enum CanonicalLibraryMetadataRealCanaryObservationStatus {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  ARMED = 'armed',
  BLOCKED = 'blocked',
  NO_ELIGIBLE_CANDIDATE = 'noEligibleCandidate',
  UNSAFE_CANDIDATE_SKIPPED = 'unsafeCandidateSkipped',
  EXECUTED_SUCCEEDED = 'executedSucceeded',
  EXECUTED_FAILED_ROLLED_BACK = 'executedFailedRolledBack',
  FATAL_ROLLBACK_FAILURE = 'fatalRollbackFailure'
}

export enum CanonicalLibraryMetadataRealCanaryRecommendation {
  REMAIN_DISABLED = 'remainDisabled',
  STAY_N1 = 'stayN1',
  READY_FOR_N3_AFTER_AUDIT = 'readyForN3AfterAudit',
  FIX_BLOCKERS = 'fixBlockers'
}

export class CanonicalLibraryMetadataRealCanaryObservationReport {
  status: CanonicalLibraryMetadataRealCanaryObservationStatus = CanonicalLibraryMetadataRealCanaryObservationStatus.DISABLED
  recommendation: CanonicalLibraryMetadataRealCanaryRecommendation = CanonicalLibraryMetadataRealCanaryRecommendation.REMAIN_DISABLED
  mode: string = 'disabled'
  rootMode: CanonicalLibraryMetadataProductionCanaryRootMode = CanonicalLibraryMetadataProductionCanaryRootMode.DISABLED
  domain: CanonicalMigrationDomain = CanonicalMigrationDomain.LIBRARY_METADATA
  canaryMaxObjectsPerSyncRun: number = 0
  selectedCandidateCount: number = 0
  executedCandidateCount: number = 0
  successfulCommitCount: number = 0
  failedCommitCount: number = 0
  rollbackCount: number = 0
  rollbackFailureCount: number = 0
  legacyFallbackCount: number = 0
  duplicateSuppressionCount: number = 0
  noEligibleCandidateCount: number = 0
  unsafeCandidateSkippedCount: number = 0
  fatalBlockerCount: number = 0
  readSideParallelEquivalent: boolean = false
  readSideParallelDivergent: boolean = false
  legacyFallbackPreserved: boolean = true
  duplicateSuppressionApplied: boolean = false
  productionRootWriteAttempted: boolean = false
  productionRootWriteExplicitlyAllowed: boolean = false
  runtimeSwitchEnabled: boolean = false
  allEligibleEnabled: boolean = false
  uiMutated: boolean = false
  resourceMoved: boolean = false
  uploadJobCreated: boolean = false
  blockers: CanonicalLibraryMetadataRealCanaryBlocker[] = []
  reason: string = ''
  redacted: boolean = true

  constructor(
    status: CanonicalLibraryMetadataRealCanaryObservationStatus = CanonicalLibraryMetadataRealCanaryObservationStatus.DISABLED,
    recommendation: CanonicalLibraryMetadataRealCanaryRecommendation = CanonicalLibraryMetadataRealCanaryRecommendation.REMAIN_DISABLED,
    configuration: any = null,
    selectedCandidateCount: number = 0,
    executedCandidateCount: number = 0,
    successfulCommitCount: number = 0,
    failedCommitCount: number = 0,
    rollbackCount: number = 0,
    rollbackFailureCount: number = 0,
    legacyFallbackCount: number = 0,
    duplicateSuppressionCount: number = 0,
    noEligibleCandidateCount: number = 0,
    unsafeCandidateSkippedCount: number = 0,
    fatalBlockerCount: number = 0,
    readSideParallelEquivalent: boolean = false,
    readSideParallelDivergent: boolean = false,
    legacyFallbackPreserved: boolean = true,
    duplicateSuppressionApplied: boolean = false,
    productionRootWriteAttempted: boolean = false,
    blockers: CanonicalLibraryMetadataRealCanaryBlocker[] = [],
    reason: string = '',
    redacted: boolean = true
  ) {
    this.status = status
    this.recommendation = recommendation
    this.selectedCandidateCount = Math.max(0, selectedCandidateCount)
    this.executedCandidateCount = Math.max(0, executedCandidateCount)
    this.successfulCommitCount = Math.max(0, successfulCommitCount)
    this.failedCommitCount = Math.max(0, failedCommitCount)
    this.rollbackCount = Math.max(0, rollbackCount)
    this.rollbackFailureCount = Math.max(0, rollbackFailureCount)
    this.legacyFallbackCount = Math.max(0, legacyFallbackCount)
    this.duplicateSuppressionCount = Math.max(0, duplicateSuppressionCount)
    this.noEligibleCandidateCount = Math.max(0, noEligibleCandidateCount)
    this.unsafeCandidateSkippedCount = Math.max(0, unsafeCandidateSkippedCount)
    this.fatalBlockerCount = Math.max(0, fatalBlockerCount)
    this.readSideParallelEquivalent = readSideParallelEquivalent
    this.readSideParallelDivergent = readSideParallelDivergent
    this.legacyFallbackPreserved = legacyFallbackPreserved
    this.duplicateSuppressionApplied = duplicateSuppressionApplied
    this.productionRootWriteAttempted = productionRootWriteAttempted
    this.blockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
    this.reason = reason
    this.redacted = redacted
  }

  get diagnosticsSummary(): string {
    return [
      'status=' + this.status,
      'recommendation=' + this.recommendation,
      'mode=' + this.mode,
      'rootMode=' + this.rootMode,
      'domain=' + this.domain,
      'budget=' + this.canaryMaxObjectsPerSyncRun,
      'selected=' + this.selectedCandidateCount,
      'executed=' + this.executedCandidateCount,
      'success=' + this.successfulCommitCount,
      'failure=' + this.failedCommitCount,
      'rollback=' + this.rollbackCount,
      'rollbackFailure=' + this.rollbackFailureCount,
      'legacyFallback=' + this.legacyFallbackCount,
      'duplicateSuppression=' + this.duplicateSuppressionCount,
      'noEligible=' + this.noEligibleCandidateCount,
      'unsafeSkipped=' + this.unsafeCandidateSkippedCount,
      'fatal=' + this.fatalBlockerCount,
      'readSideEquivalent=' + this.readSideParallelEquivalent,
      'readSideDivergent=' + this.readSideParallelDivergent,
      'runtimeSwitch=' + this.runtimeSwitchEnabled,
      'allEligible=' + this.allEligibleEnabled,
      'uiMutated=' + this.uiMutated,
      'resourceMoved=' + this.resourceMoved,
      'uploadJobCreated=' + this.uploadJobCreated,
      'blockers=' + this.blockers.join('|'),
      'redacted=' + this.redacted
    ].join(',')
  }
}

// ── Pilot Diagnostic Redactor ──

export class CanonicalLibraryMetadataPilotDiagnosticRedactor {
  constructor() {}

  redact(value: string | null): string {
    if (!value) {
      return 'redacted'
    }
    if (CanonicalLibraryMetadataPilotDiagnosticRedactor.containsUnsafeSignal(value)) {
      const digest = CanonicalHash.sha256OfString(value).value
      const prefix = hashPrefix(digest, 12)
      return 'redacted-' + prefix
    }
    return value
  }

  static containsUnsafeSignal(value: string): boolean {
    const lowercased = value.toLowerCase()
    if (lowercased.includes('/private/') || lowercased.includes('/library/application support/')) {
      return true
    }
    if (lowercased.includes('api_key') || lowercased.includes('apikey') ||
      lowercased.includes('secret') || lowercased.includes('token=') ||
      lowercased.includes('fingerprint') || lowercased.includes('transcript') ||
      lowercased.includes('provider response')) {
      return true
    }
    let hexRun = 0
    for (let i = 0; i < lowercased.length; i++) {
      const code = lowercased.charCodeAt(i)
      if ((code >= 48 && code <= 57) || (code >= 97 && code <= 102)) {
        hexRun++
        if (hexRun >= 32) {
          return true
        }
      } else {
        hexRun = 0
      }
    }
    return false
  }
}
