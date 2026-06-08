/**
 * CanonicalNoCommitV82 — no-commit staging and evidence for cutover domains.
 * Ported from Rokurics-Apple (CanonicalNoCommitV82.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, nilIfEmpty
} from './CanonicalCore'
import {
  CanonicalProductionRedaction, CanonicalProductionDomain
} from './CanonicalProductionPorts'

// =============================================================================
// NO-COMMIT SIDE EFFECT
// =============================================================================

export enum CanonicalNoCommitSideEffectClass {
  STAGING_ONLY = 'stagingOnly'
}

// =============================================================================
// NO-COMMIT BLOCKER
// =============================================================================

export enum CanonicalNoCommitBlockerSeverity {
  WARNING = 'warning',
  BLOCKER = 'blocker'
}

export class CanonicalNoCommitBlocker {
  severity: CanonicalNoCommitBlockerSeverity
  reason: string

  constructor(severity: CanonicalNoCommitBlockerSeverity, reason: string) {
    this.severity = severity
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? severity
  }

  get id(): string {
    return [this.severity, this.reason].join('|')
  }
}

// =============================================================================
// STAGING ROOT
// =============================================================================

export enum CanonicalNoCommitStagingRootKind {
  SYSTEM_TEMPORARY = 'systemTemporary',
  EXPLICIT_STAGING_ROOT = 'explicitStagingRoot',
  REJECTED_PRODUCTION_ROOT = 'rejectedProductionRoot'
}

export enum CanonicalNoCommitStagingRootLifecycleStatus {
  NOT_CREATED = 'notCreated',
  CREATED = 'created',
  VALIDATION_FAILED = 'validationFailed'
}

export enum CanonicalNoCommitStagingRootCleanupStatus {
  REMOVED = 'removed',
  RETAINED_FOR_DIAGNOSTICS = 'retainedForDiagnostics',
  REFUSED_PRODUCTION_ROOT = 'refusedProductionRoot',
  FAILED = 'failed'
}

export class CanonicalNoCommitStagingRootCleanupPolicy {
  kind: string
  maxAge?: number
  maxCount?: number
  maxBytes?: number

  constructor(kind: string, maxAge?: number, maxCount?: number, maxBytes?: number) {
    this.kind = kind
    this.maxAge = maxAge
    this.maxCount = maxCount
    this.maxBytes = maxBytes
  }

  static readonly CLEANUP_IMMEDIATELY = new CanonicalNoCommitStagingRootCleanupPolicy('cleanupImmediately')
  static retainForDiagnostics(maxAge: number, maxCount: number, maxBytes: number): CanonicalNoCommitStagingRootCleanupPolicy {
    return new CanonicalNoCommitStagingRootCleanupPolicy('retainForDiagnostics', maxAge, maxCount, maxBytes)
  }

  get policyName(): string {
    return this.kind
  }
}

// =============================================================================
// STAGING ROOT LIFECYCLE
// =============================================================================

export class CanonicalNoCommitStagingRoot {
  rootID: string
  rootKind: CanonicalNoCommitStagingRootKind
  rootURL: string
  productionRootURL: string | null
  createdAt: Date

  constructor(
    rootID: string = CanonicalNoCommitStagingRoot.generateUUID(),
    rootKind: CanonicalNoCommitStagingRootKind,
    rootURL: string,
    productionRootURL: string | null = null,
    createdAt: Date = new Date()
  ) {
    this.rootID = CanonicalProductionRedaction.safeIdentifier(rootID, 'no-commit-root')
    this.rootKind = rootKind
    this.rootURL = rootURL
    this.productionRootURL = productionRootURL
    this.createdAt = createdAt
  }

  private static generateUUID(): string {
    return 'no-commit-' + Math.random().toString(36).substring(2, 10) + Date.now().toString(36)
  }
}

export class CanonicalNoCommitStagingRootRetentionRecord {
  rootID: string
  rootKind: string
  createdAt: CanonicalTimestamp
  retainedBytes: number
  entryCount: number

  constructor(
    rootID: string,
    rootKind: string,
    createdAt: Date = new Date(),
    retainedBytes: number = 0,
    entryCount: number = 0
  ) {
    this.rootID = CanonicalProductionRedaction.safeIdentifier(rootID, 'no-commit-root')
    this.rootKind = rootKind
    this.createdAt = new CanonicalTimestamp(createdAt)
    this.retainedBytes = Math.max(0, retainedBytes)
    this.entryCount = Math.max(0, entryCount)
  }

  get id(): string {
    return this.rootID
  }
}

// =============================================================================
// STAGING EVIDENCE
// =============================================================================

export class CanonicalNoCommitStagingEvidence {
  rootID: string
  rootKind: string
  lifecycleStatus: string
  fileCount: number
  byteCount: number
  wroteOnlyStagingRoot: boolean
  sideEffectClass: string

  constructor(
    rootID: string,
    rootKind: string,
    lifecycleStatus: string,
    fileCount: number = 0,
    byteCount: number = 0,
    wroteOnlyStagingRoot: boolean = true,
    sideEffectClass: string = CanonicalNoCommitSideEffectClass.STAGING_ONLY
  ) {
    this.rootID = CanonicalProductionRedaction.safeIdentifier(rootID, 'no-commit-root')
    this.rootKind = rootKind
    this.lifecycleStatus = lifecycleStatus
    this.fileCount = Math.max(0, fileCount)
    this.byteCount = Math.max(0, byteCount)
    this.wroteOnlyStagingRoot = wroteOnlyStagingRoot
    this.sideEffectClass = sideEffectClass
  }

  get diagnosticsSummary(): string {
    return [
      'rootKind=' + this.rootKind, 'rootID=' + this.rootID,
      'lifecycle=' + this.lifecycleStatus, 'files=' + this.fileCount,
      'bytes=' + this.byteCount, 'sideEffectClass=' + this.sideEffectClass
    ].join(',')
  }
}

export class CanonicalNoCommitCleanupEvidence {
  rootID: string
  rootKind: string
  policy: string
  status: string
  fileCount: number
  byteCount: number
  removedRootCount: number
  retainedRootCount: number
  warning: CanonicalNoCommitBlocker | null

  constructor(
    rootID: string,
    rootKind: string,
    policy: string,
    status: string,
    fileCount: number = 0,
    byteCount: number = 0,
    removedRootCount: number = 0,
    retainedRootCount: number = 0,
    warning: CanonicalNoCommitBlocker | null = null
  ) {
    this.rootID = rootID
    this.rootKind = rootKind
    this.policy = policy
    this.status = status
    this.fileCount = Math.max(0, fileCount)
    this.byteCount = Math.max(0, byteCount)
    this.removedRootCount = Math.max(0, removedRootCount)
    this.retainedRootCount = Math.max(0, retainedRootCount)
    this.warning = warning
  }

  get diagnosticsSummary(): string {
    return [
      'rootKind=' + this.rootKind, 'rootID=' + this.rootID, 'policy=' + this.policy,
      'cleanup=' + this.status, 'files=' + this.fileCount, 'bytes=' + this.byteCount,
      'removedRoots=' + this.removedRootCount, 'retainedRoots=' + this.retainedRootCount,
      'warning=' + (this.warning?.reason ?? 'none')
    ].join(',')
  }
}

// =============================================================================
// EVIDENCE STATUS & REPORT
// =============================================================================

export enum CanonicalNoCommitEvidenceStatus {
  COMPLETE = 'complete',
  BLOCKED = 'blocked',
  DIVERGENT = 'divergent',
  INSUFFICIENT_EVIDENCE = 'insufficientEvidence',
  UNSUPPORTED = 'unsupported',
  WARNING = 'warning'
}

export enum CanonicalCutoverDomain {
  RECORDING_METADATA = 'recordingMetadata',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  LIBRARY_METADATA = 'libraryMetadata',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  AUDIO_UPLOAD = 'audioUpload',
  UI_PROJECTION = 'uiProjection'
}

export enum CanonicalCutoverAppSeamMode {
  DISABLED = 'disabled',
  GUARDED_EXECUTE_NO_COMMIT = 'guardedExecuteNoCommit',
  GUARDED_EXECUTE_COMMIT = 'guardedExecuteCommit'
}

export enum CanonicalSingleDomainCutoverConfiguration {
  DISABLED = 'disabled',
  GUARDED_EXECUTE_NO_COMMIT = 'guardedExecuteNoCommit',
  GUARDED_EXECUTE_COMMIT = 'guardedExecuteCommit'
}

export class CanonicalNoCommitEquivalenceEvidence {
  equivalentCount: number
  divergentCount: number
  insufficientEvidenceCount: number
  unsupportedCount: number
  hashPrefixes: string[]
  routeProjectionStatus: string
  legacyActionComparisonStatus: string

  constructor(
    equivalentCount: number = 0,
    divergentCount: number = 0,
    insufficientEvidenceCount: number = 0,
    unsupportedCount: number = 0
  ) {
    this.equivalentCount = equivalentCount
    this.divergentCount = divergentCount
    this.insufficientEvidenceCount = insufficientEvidenceCount
    this.unsupportedCount = unsupportedCount
    this.hashPrefixes = []
    this.routeProjectionStatus = 'routeProjectionSafe'
    this.legacyActionComparisonStatus = 'legacyActionComparisonEquivalent'
  }
}

export class CanonicalNoCommitEvidenceReport {
  domain: string
  mode: string
  status: CanonicalNoCommitEvidenceStatus
  candidateCount: number
  wouldApplyCount: number
  wouldSendCount: number
  equivalentCount: number
  divergentCount: number
  insufficientEvidenceCount: number
  unsupportedCount: number
  stagingRootLifecycleStatus: string
  cleanupStatus: string
  routeProjectionStatus: string
  legacyActionComparisonStatus: string
  productionCommitSuppressed: boolean
  legacyDuplicateSuppressed: boolean
  sideEffectClass: CanonicalNoCommitSideEffectClass
  equivalenceEvidence: CanonicalNoCommitEquivalenceEvidence
  stagingEvidence: CanonicalNoCommitStagingEvidence[]
  cleanupEvidence: CanonicalNoCommitCleanupEvidence[]
  blockers: CanonicalNoCommitBlocker[]

  constructor(
    domain: string,
    mode: string,
    status: CanonicalNoCommitEvidenceStatus = CanonicalNoCommitEvidenceStatus.BLOCKED
  ) {
    this.domain = domain
    this.mode = mode
    this.status = status
    this.candidateCount = 0
    this.wouldApplyCount = 0
    this.wouldSendCount = 0
    this.equivalentCount = 0
    this.divergentCount = 0
    this.insufficientEvidenceCount = 0
    this.unsupportedCount = 0
    this.stagingRootLifecycleStatus = ''
    this.cleanupStatus = ''
    this.routeProjectionStatus = 'routeProjectionSafe'
    this.legacyActionComparisonStatus = 'legacyActionComparisonEquivalent'
    this.productionCommitSuppressed = true
    this.legacyDuplicateSuppressed = false
    this.sideEffectClass = CanonicalNoCommitSideEffectClass.STAGING_ONLY
    this.equivalenceEvidence = new CanonicalNoCommitEquivalenceEvidence()
    this.stagingEvidence = []
    this.cleanupEvidence = []
    this.blockers = []
  }

  get diagnosticsSummary(): string {
    return [
      'domain=' + this.domain, 'mode=' + this.mode, 'status=' + this.status,
      'candidateCount=' + this.candidateCount, 'wouldApply=' + this.wouldApplyCount,
      'wouldSend=' + this.wouldSendCount, 'equivalent=' + this.equivalentCount,
      'divergent=' + this.divergentCount, 'insufficientEvidence=' + this.insufficientEvidenceCount,
      'unsupported=' + this.unsupportedCount,
      'staging=' + (this.stagingRootLifecycleStatus.length > 0 ? this.stagingRootLifecycleStatus : 'none'),
      'cleanup=' + (this.cleanupStatus.length > 0 ? this.cleanupStatus : 'none'),
      'routeProjection=' + this.routeProjectionStatus,
      'legacyComparison=' + this.legacyActionComparisonStatus,
      'productionCommitSuppressed=' + this.productionCommitSuppressed,
      'legacyDuplicateSuppressed=' + this.legacyDuplicateSuppressed,
      'sideEffectClass=' + this.sideEffectClass
    ].join(',')
  }
}

// =============================================================================
// MIGRATION STAGE POLICY
// =============================================================================

export enum CanonicalMigrationStage {
  OFF = 'off',
  NOT_STARTED = 'notStarted',
  PROJECTED = 'projected',
  PLANNED = 'planned',
  NO_COMMIT = 'noCommit',
  REAL_APPLY_PORT = 'realApplyPort',
  COMMIT_EXECUTOR = 'commitExecutor',
  APP_SEAM_DEFAULT_OFF = 'appSeamDefaultOff',
  NEXT_PILOT_CANDIDATE = 'nextPilotCandidate',
  CANARY_N0 = 'canaryN0',
  CANARY_N1 = 'canaryN1',
  EXPANDED_CANARY = 'expandedCanary',
  DOMAIN_CUTOVER = 'domainCutover',
  READ_SIDE_PARALLEL = 'readSideParallel',
  READ_SIDE_CUTOVER = 'readSideCutover',
  RETIREMENT_CANDIDATE = 'retirementCandidate',
  RETIRED = 'retired',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  DECISION_SHADOW = 'decisionShadow',
  EXECUTION_SHADOW = 'executionShadow',
  REAL_DATA_SHADOW_COPY = 'realDataShadowCopy',
  READ_ONLY_TRANSPORT_PROBE = 'readOnlyTransportProbe',
  RECORDING_METADATA_NO_COMMIT = 'recordingMetadataNoCommit',
  RECORDING_METADATA_GUARDED_COMMIT = 'recordingMetadataGuardedCommit',
  UNSUPPORTED = 'unsupported'
}

export enum CanonicalMigrationStageSideEffect {
  DIAGNOSTICS_WRITE = 'diagnosticsWrite',
  SHADOW_ROOT_WRITE = 'shadowRootWrite',
  READ_ONLY_NETWORK_PROBE = 'readOnlyNetworkProbe',
  STAGING_ROOT_WRITE = 'stagingRootWrite',
  PRODUCTION_COMMIT = 'productionCommit'
}

export enum CanonicalMigrationStageEvidence {
  NONE = 'none',
  DRY_RUN_EQUIVALENCE = 'dryRunEquivalence',
  EXECUTION_SHADOW = 'executionShadow',
  REAL_DATA_SHADOW_COPY = 'realDataShadowCopy',
  READ_ONLY_TRANSPORT_PROBE = 'readOnlyTransportProbe',
  NO_COMMIT_EVIDENCE_REPORT = 'noCommitEvidenceReport',
  OWNER_APPROVAL = 'ownerApproval',
  ROLLBACK_PLAN = 'rollbackPlan'
}

export class CanonicalMigrationStagePolicy {
  allowedSideEffects: CanonicalMigrationStageSideEffect[]
  requiredEvidence: CanonicalMigrationStageEvidence[]
  allowedDomains: string[]
  forbiddenDomains: string[]
  productionCommitAllowed: boolean
  existingConfigurationKeys: string[]

  constructor(
    allowedSideEffects: CanonicalMigrationStageSideEffect[],
    requiredEvidence: CanonicalMigrationStageEvidence[],
    allowedDomains: string[],
    forbiddenDomains: string[],
    productionCommitAllowed: boolean = false,
    existingConfigurationKeys: string[]
  ) {
    this.allowedSideEffects = Array.from(new Set(allowedSideEffects)).sort((a, b) => a.localeCompare(b))
    this.requiredEvidence = Array.from(new Set(requiredEvidence)).sort((a, b) => a.localeCompare(b))
    this.allowedDomains = Array.from(new Set(allowedDomains)).sort((a, b) => a.localeCompare(b))
    this.forbiddenDomains = Array.from(new Set(forbiddenDomains)).sort((a, b) => a.localeCompare(b))
    this.productionCommitAllowed = productionCommitAllowed
    this.existingConfigurationKeys = Array.from(new Set(
      existingConfigurationKeys.map(k => CanonicalProductionRedaction.safeDiagnosticText(k)).filter((k): k is string => k !== null)
    )).sort()
  }

  static defaultPolicy(stage: CanonicalMigrationStage): CanonicalMigrationStagePolicy {
    const all = Object.values(CanonicalCutoverDomain)
    const recordingOnly = [CanonicalCutoverDomain.RECORDING_METADATA]
    const allExceptRecording = all.filter(d => d !== CanonicalCutoverDomain.RECORDING_METADATA)

    switch (stage) {
      case CanonicalMigrationStage.OFF:
      case CanonicalMigrationStage.NOT_STARTED:
        return new CanonicalMigrationStagePolicy([], [CanonicalMigrationStageEvidence.NONE], [], all, false, ['canonicalShadowMigrationConfiguration.disabled'])
      case CanonicalMigrationStage.DIAGNOSTICS_ONLY:
      case CanonicalMigrationStage.PROJECTED:
      case CanonicalMigrationStage.PLANNED:
      case CanonicalMigrationStage.APP_SEAM_DEFAULT_OFF:
      case CanonicalMigrationStage.NEXT_PILOT_CANDIDATE:
      case CanonicalMigrationStage.CANARY_N0:
      case CanonicalMigrationStage.READ_SIDE_PARALLEL:
      case CanonicalMigrationStage.RETIREMENT_CANDIDATE:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE], [CanonicalMigrationStageEvidence.NONE],
          all, [], false, ['canonicalMigrationMatrix.diagnosticsOnly']
        )
      case CanonicalMigrationStage.NO_COMMIT:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE, CanonicalMigrationStageSideEffect.STAGING_ROOT_WRITE],
          [CanonicalMigrationStageEvidence.NO_COMMIT_EVIDENCE_REPORT], all, [], false,
          ['canonicalMigrationMatrix.noCommit']
        )
      case CanonicalMigrationStage.REAL_APPLY_PORT:
      case CanonicalMigrationStage.COMMIT_EXECUTOR:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE],
          [CanonicalMigrationStageEvidence.NO_COMMIT_EVIDENCE_REPORT, CanonicalMigrationStageEvidence.ROLLBACK_PLAN],
          all, [], false, ['canonicalMigrationMatrix.' + stage]
        )
      case CanonicalMigrationStage.CANARY_N1:
      case CanonicalMigrationStage.EXPANDED_CANARY:
      case CanonicalMigrationStage.DOMAIN_CUTOVER:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE, CanonicalMigrationStageSideEffect.PRODUCTION_COMMIT],
          [
            CanonicalMigrationStageEvidence.DRY_RUN_EQUIVALENCE, CanonicalMigrationStageEvidence.EXECUTION_SHADOW,
            CanonicalMigrationStageEvidence.REAL_DATA_SHADOW_COPY, CanonicalMigrationStageEvidence.READ_ONLY_TRANSPORT_PROBE,
            CanonicalMigrationStageEvidence.NO_COMMIT_EVIDENCE_REPORT, CanonicalMigrationStageEvidence.OWNER_APPROVAL,
            CanonicalMigrationStageEvidence.ROLLBACK_PLAN
          ], all, [], true, ['canonicalMigrationMatrix.' + stage]
        )
      case CanonicalMigrationStage.READ_SIDE_CUTOVER:
      case CanonicalMigrationStage.RETIRED:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE],
          [CanonicalMigrationStageEvidence.OWNER_APPROVAL, CanonicalMigrationStageEvidence.ROLLBACK_PLAN],
          all, [], false, ['canonicalMigrationMatrix.' + stage]
        )
      case CanonicalMigrationStage.DECISION_SHADOW:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE],
          [CanonicalMigrationStageEvidence.DRY_RUN_EQUIVALENCE], all, [], false,
          ['canonicalShadowMigrationConfiguration.dryRunCompare']
        )
      case CanonicalMigrationStage.EXECUTION_SHADOW:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE, CanonicalMigrationStageSideEffect.SHADOW_ROOT_WRITE],
          [CanonicalMigrationStageEvidence.DRY_RUN_EQUIVALENCE, CanonicalMigrationStageEvidence.EXECUTION_SHADOW],
          recordingOnly, allExceptRecording, false, ['canonicalSingleDomainShadowConfiguration.executionShadowDryRun']
        )
      case CanonicalMigrationStage.REAL_DATA_SHADOW_COPY:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE, CanonicalMigrationStageSideEffect.SHADOW_ROOT_WRITE],
          [CanonicalMigrationStageEvidence.EXECUTION_SHADOW, CanonicalMigrationStageEvidence.REAL_DATA_SHADOW_COPY],
          recordingOnly, allExceptRecording, false, ['canonicalShadowMigrationConfiguration.realDataShadowCopyPolicy']
        )
      case CanonicalMigrationStage.READ_ONLY_TRANSPORT_PROBE:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE, CanonicalMigrationStageSideEffect.READ_ONLY_NETWORK_PROBE],
          [CanonicalMigrationStageEvidence.REAL_DATA_SHADOW_COPY, CanonicalMigrationStageEvidence.READ_ONLY_TRANSPORT_PROBE],
          recordingOnly, allExceptRecording, false, ['canonicalShadowMigrationConfiguration.readOnlyTransportProbePolicy']
        )
      case CanonicalMigrationStage.RECORDING_METADATA_NO_COMMIT:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE, CanonicalMigrationStageSideEffect.STAGING_ROOT_WRITE],
          [CanonicalMigrationStageEvidence.REAL_DATA_SHADOW_COPY, CanonicalMigrationStageEvidence.READ_ONLY_TRANSPORT_PROBE,
            CanonicalMigrationStageEvidence.NO_COMMIT_EVIDENCE_REPORT], recordingOnly, allExceptRecording, false,
          ['canonicalV8CutoverAppSeamConfiguration.guardedExecuteNoCommit']
        )
      case CanonicalMigrationStage.RECORDING_METADATA_GUARDED_COMMIT:
        return new CanonicalMigrationStagePolicy(
          [CanonicalMigrationStageSideEffect.DIAGNOSTICS_WRITE, CanonicalMigrationStageSideEffect.PRODUCTION_COMMIT],
          [
            CanonicalMigrationStageEvidence.DRY_RUN_EQUIVALENCE, CanonicalMigrationStageEvidence.EXECUTION_SHADOW,
            CanonicalMigrationStageEvidence.REAL_DATA_SHADOW_COPY, CanonicalMigrationStageEvidence.READ_ONLY_TRANSPORT_PROBE,
            CanonicalMigrationStageEvidence.NO_COMMIT_EVIDENCE_REPORT, CanonicalMigrationStageEvidence.OWNER_APPROVAL,
            CanonicalMigrationStageEvidence.ROLLBACK_PLAN
          ], recordingOnly, allExceptRecording, true, ['CanonicalSingleDomainCutoverConfiguration.guardedExecuteCommit']
        )
      case CanonicalMigrationStage.UNSUPPORTED:
        return new CanonicalMigrationStagePolicy([], [], [], all, false, [])
    }
  }
}

export class CanonicalMigrationConfigurationSummary {
  stage: CanonicalMigrationStage
  domain: string
  allowed: boolean
  blockers: string[]
  allowedSideEffects: CanonicalMigrationStageSideEffect[]
  requiredEvidence: CanonicalMigrationStageEvidence[]
  allowedDomains: string[]
  forbiddenDomains: string[]
  productionCommitAllowed: boolean
  existingConfigurationKeys: string[]

  constructor(
    stage: CanonicalMigrationStage, domain: string, allowed: boolean,
    blockers: string[], policy: CanonicalMigrationStagePolicy
  ) {
    this.stage = stage
    this.domain = domain
    this.allowed = allowed
    this.blockers = Array.from(new Set(
      blockers.map(b => CanonicalProductionRedaction.safeDiagnosticText(b)).filter((b): b is string => b !== null)
    )).sort()
    this.allowedSideEffects = policy.allowedSideEffects
    this.requiredEvidence = policy.requiredEvidence
    this.allowedDomains = policy.allowedDomains
    this.forbiddenDomains = policy.forbiddenDomains
    this.productionCommitAllowed = policy.productionCommitAllowed
    this.existingConfigurationKeys = policy.existingConfigurationKeys
  }
}

export class CanonicalMigrationStageConfiguration {
  stage: CanonicalMigrationStage
  domain: string
  policy: CanonicalMigrationStagePolicy

  constructor(
    stage: CanonicalMigrationStage = CanonicalMigrationStage.OFF,
    domain: string = CanonicalCutoverDomain.RECORDING_METADATA,
    policy: CanonicalMigrationStagePolicy | null = null
  ) {
    this.stage = stage
    this.domain = domain
    this.policy = policy ?? CanonicalMigrationStagePolicy.defaultPolicy(stage)
  }

  static readonly OFF = new CanonicalMigrationStageConfiguration()
}
