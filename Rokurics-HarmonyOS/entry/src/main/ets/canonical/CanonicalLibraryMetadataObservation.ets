/**
 * CanonicalLibraryMetadataObservation — observation window, gate, and end-to-end pilot.
 * Ported from Rokurics-Apple (CanonicalLibraryMetadataObservation.swift).
 */

import {
  CanonicalHash, nilIfEmpty, hashPrefix
} from './CanonicalCore'
import {
  CanonicalMigrationDomain,
  CanonicalMigrationDomainMatrix,
  CanonicalLibraryMetadataCutoverDomain,
  CanonicalLibraryMetadataCutoverResult,
  CanonicalLibraryMetadataCutoverDiagnostic,
  CanonicalLibraryMetadataCutoverDiagnosticKind,
  CanonicalProductionExecutionDomainRole,
  CanonicalProductionSideEffect,
  CanonicalProductionDomain,
  CanonicalMigrationDomainPolicy,
  CanonicalMigrationStage,
  CanonicalMigrationStageStatus
} from './CanonicalLibraryMetadataCutover'
import { CanonicalSyncPlanTrigger } from './CanonicalLibrarySyncPlanner'

// ── Safe redaction ──

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
  static containsSensitivePathSignal(value: string): boolean {
    return value.includes('/private/') || value.includes('/.gradle/')
  }
}

// ── Observation Event ──

export enum CanonicalLibraryMetadataObservationEventKind {
  WINDOW_STARTED = 'windowStarted',
  WINDOW_COMPLETED = 'windowCompleted',
  CANONICAL_COMMIT_ATTEMPTED = 'canonicalCommitAttempted',
  CANONICAL_COMMIT_SUCCEEDED = 'canonicalCommitSucceeded',
  CANONICAL_COMMIT_FAILED = 'canonicalCommitFailed',
  ROLLBACK_ATTEMPTED = 'rollbackAttempted',
  ROLLBACK_SUCCEEDED = 'rollbackSucceeded',
  ROLLBACK_FAILED = 'rollbackFailed',
  ROLLBACK_FATAL = 'rollbackFatal',
  LEGACY_FALLBACK_USED = 'legacyFallbackUsed',
  DUPLICATE_LEGACY_SUPPRESSED = 'duplicateLegacySuppressed',
  UNRESOLVED_CONFLICT_OBSERVED = 'unresolvedConflictObserved',
  RESOURCE_MOVE_ATTEMPTED = 'resourceMoveAttempted',
  CANONICAL_READ_CANDIDATE_BUILT = 'canonicalReadCandidateBuilt',
  CANONICAL_READ_SERVED = 'canonicalReadServed',
  LEGACY_READ_FALLBACK_USED = 'legacyReadFallbackUsed',
  READ_SIDE_PARALLEL_EQUIVALENT = 'readSideParallelEquivalent',
  READ_SIDE_PARALLEL_DIVERGENT = 'readSideParallelDivergent',
  READ_SIDE_UNSUPPORTED_OBJECT = 'readSideUnsupportedObject',
  READ_SIDE_PATH_LEAK_RISK = 'readSidePathLeakRisk',
  UNSAFE_SIDE_EFFECT_OBSERVED = 'unsafeSideEffectObserved',
  SYNC_OR_UPLOAD_TRIGGERED = 'syncOrUploadTriggered',
  UI_MUTATED = 'uiMutated',
  CONTENT_WRITTEN = 'contentWritten',
  TOMBSTONE_DELETE_ATTEMPTED = 'tombstoneDeleteAttempted'
}

export class CanonicalLibraryMetadataObservationEvent {
  kind: CanonicalLibraryMetadataObservationEventKind
  count: number
  syncRunID: string | null
  trigger: CanonicalSyncPlanTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  reason: string | null

  constructor(
    kind: CanonicalLibraryMetadataObservationEventKind,
    count: number = 1,
    syncRunID: string | null = null,
    trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC,
    nodeRole: CanonicalProductionExecutionDomainRole = CanonicalProductionExecutionDomainRole.TEST_HARNESS,
    reason: string | null = null
  ) {
    this.kind = kind
    this.count = Math.max(0, count)
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason)
  }
}

// ── Observation Policy ──

export class CanonicalLibraryMetadataObservationPolicy {
  enabled: boolean
  explicitInternalTestConfiguration: boolean
  minimumWriteCanonicalCommitCount: number
  minimumReadCanonicalEvidenceCount: number
  minimumTotalEventCount: number
  requireLegacyFallbackAvailable: boolean
  legacyFallbackAvailable: boolean
  requireOnlyLibraryMetadataActivePilot: boolean
  allowParallelReadOnlyEvidence: boolean
  manualAuditRequired: boolean
  recordDiagnostics: boolean
  maxDiagnosticsEvents: number

  constructor(
    enabled: boolean = false,
    explicitInternalTestConfiguration: boolean = false,
    minimumWriteCanonicalCommitCount: number = 1,
    minimumReadCanonicalEvidenceCount: number = 1,
    minimumTotalEventCount: number = 2,
    requireLegacyFallbackAvailable: boolean = true,
    legacyFallbackAvailable: boolean = true,
    requireOnlyLibraryMetadataActivePilot: boolean = true,
    allowParallelReadOnlyEvidence: boolean = true,
    manualAuditRequired: boolean = true,
    recordDiagnostics: boolean = true,
    maxDiagnosticsEvents: number = 16
  ) {
    this.enabled = enabled
    this.explicitInternalTestConfiguration = explicitInternalTestConfiguration
    this.minimumWriteCanonicalCommitCount = Math.max(0, minimumWriteCanonicalCommitCount)
    this.minimumReadCanonicalEvidenceCount = Math.max(0, minimumReadCanonicalEvidenceCount)
    this.minimumTotalEventCount = Math.max(0, minimumTotalEventCount)
    this.requireLegacyFallbackAvailable = requireLegacyFallbackAvailable
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.requireOnlyLibraryMetadataActivePilot = requireOnlyLibraryMetadataActivePilot
    this.allowParallelReadOnlyEvidence = allowParallelReadOnlyEvidence
    this.manualAuditRequired = manualAuditRequired
    this.recordDiagnostics = recordDiagnostics
    this.maxDiagnosticsEvents = Math.max(0, maxDiagnosticsEvents)
  }

  static readonly DISABLED = new CanonicalLibraryMetadataObservationPolicy()

  static explicitInternalTest(
    minimumWriteCanonicalCommitCount: number = 1,
    minimumReadCanonicalEvidenceCount: number = 1,
    minimumTotalEventCount: number = 2,
    legacyFallbackAvailable: boolean = true
  ): CanonicalLibraryMetadataObservationPolicy {
    return new CanonicalLibraryMetadataObservationPolicy(
      true, true, minimumWriteCanonicalCommitCount,
      minimumReadCanonicalEvidenceCount, minimumTotalEventCount,
      true, legacyFallbackAvailable, true, true, true
    )
  }
}

// ── Observation Window ──

export class CanonicalLibraryMetadataObservationWindow {
  observationWindowID: string
  domain: CanonicalMigrationDomain
  enabled: boolean
  explicitInternalTestConfiguration: boolean
  runtimeSwitchEnabled: boolean
  defaultCanonicalReadEnabled: boolean
  defaultCanonicalWriteEnabled: boolean
  activePilotDomain: CanonicalMigrationDomain | null
  otherDomainsStaticOnly: boolean
  writeCanonicalCommitAttemptCount: number
  writeCanonicalCommitSucceededCount: number
  writeCanonicalCommitFailedCount: number
  rollbackAttemptCount: number
  rollbackSucceededCount: number
  rollbackFailedCount: number
  rollbackFatalCount: number
  legacyFallbackUsedCount: number
  duplicateLegacySuppressedCount: number
  unresolvedConflictCount: number
  resourceMoveAttemptedCount: number
  canonicalReadCandidateBuiltCount: number
  canonicalReadServedCount: number
  legacyReadFallbackCount: number
  readSideParallelEquivalentCount: number
  readSideParallelDivergentCount: number
  readSideDivergenceCount: number
  readSideUnsupportedObjectCount: number
  readSidePathLeakRiskCount: number
  unsafeSideEffectCount: number
  syncOrUploadTriggeredCount: number
  uiMutatedCount: number
  contentWrittenCount: number
  tombstoneDeleteAttemptedCount: number
  diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[]
  diagnosticsSummary: string

  constructor(
    observationWindowID: string,
    policy: CanonicalLibraryMetadataObservationPolicy = CanonicalLibraryMetadataObservationPolicy.DISABLED,
    matrix: CanonicalMigrationDomainMatrix = CanonicalMigrationDomainMatrix.defaultV813()
  ) {
    const matrixReport = matrix.validate()
    this.observationWindowID = CanonicalProductionRedaction.safeIdentifier(observationWindowID, 'library-metadata-observation')
    this.domain = CanonicalMigrationDomain.LIBRARY_METADATA
    this.enabled = policy.enabled
    this.explicitInternalTestConfiguration = policy.explicitInternalTestConfiguration
    this.runtimeSwitchEnabled = matrix.policies.some(p => p.runtimeSwitchEnabled)
    this.defaultCanonicalReadEnabled = matrix.policies.some(p => p.defaultCutoverEnabled)
    this.defaultCanonicalWriteEnabled = matrix.policies.some(p => p.releaseDefaultEnabledCutover)
    this.activePilotDomain = matrixReport.activePilotDomain
    this.otherDomainsStaticOnly = matrix.policies
      .filter(p => p.domain !== CanonicalMigrationDomain.LIBRARY_METADATA)
      .every(p => p.staticOnly && !p.activePilot && !p.hasActiveCanaryOrCutover)
    this.writeCanonicalCommitAttemptCount = 0
    this.writeCanonicalCommitSucceededCount = 0
    this.writeCanonicalCommitFailedCount = 0
    this.rollbackAttemptCount = 0
    this.rollbackSucceededCount = 0
    this.rollbackFailedCount = 0
    this.rollbackFatalCount = 0
    this.legacyFallbackUsedCount = 0
    this.duplicateLegacySuppressedCount = 0
    this.unresolvedConflictCount = 0
    this.resourceMoveAttemptedCount = 0
    this.canonicalReadCandidateBuiltCount = 0
    this.canonicalReadServedCount = 0
    this.legacyReadFallbackCount = 0
    this.readSideParallelEquivalentCount = 0
    this.readSideParallelDivergentCount = 0
    this.readSideDivergenceCount = 0
    this.readSideUnsupportedObjectCount = 0
    this.readSidePathLeakRiskCount = 0
    this.unsafeSideEffectCount = 0
    this.syncOrUploadTriggeredCount = 0
    this.uiMutatedCount = 0
    this.contentWrittenCount = 0
    this.tombstoneDeleteAttemptedCount = 0
    this.diagnostics = []
    this.diagnosticsSummary = 'v8.20,domain=libraryMetadata,enabled=' + policy.enabled +
      ',explicitInternalTest=' + policy.explicitInternalTestConfiguration
  }

  static disabled(
    observationWindowID: string = 'libraryMetadataObservationDisabled',
    matrix: CanonicalMigrationDomainMatrix = CanonicalMigrationDomainMatrix.defaultV813()
  ): CanonicalLibraryMetadataObservationWindow {
    return new CanonicalLibraryMetadataObservationWindow(observationWindowID, CanonicalLibraryMetadataObservationPolicy.DISABLED, matrix)
  }

  get totalEventCount(): number {
    return this.writeCanonicalCommitAttemptCount + this.writeCanonicalCommitSucceededCount +
      this.writeCanonicalCommitFailedCount + this.rollbackAttemptCount + this.rollbackSucceededCount +
      this.rollbackFailedCount + this.rollbackFatalCount + this.legacyFallbackUsedCount +
      this.duplicateLegacySuppressedCount + this.unresolvedConflictCount + this.resourceMoveAttemptedCount +
      this.canonicalReadCandidateBuiltCount + this.canonicalReadServedCount + this.legacyReadFallbackCount +
      this.readSideParallelEquivalentCount + this.readSideParallelDivergentCount +
      this.readSideUnsupportedObjectCount + this.readSidePathLeakRiskCount + this.unsafeSideEffectCount +
      this.syncOrUploadTriggeredCount + this.uiMutatedCount + this.contentWrittenCount +
      this.tombstoneDeleteAttemptedCount
  }

  get readEvidenceCount(): number {
    return this.canonicalReadServedCount + this.canonicalReadCandidateBuiltCount + this.readSideParallelEquivalentCount
  }

  get noUnsafeSideEffects(): boolean {
    return this.unsafeSideEffectCount === 0 && this.resourceMoveAttemptedCount === 0 &&
      this.syncOrUploadTriggeredCount === 0 && this.contentWrittenCount === 0 &&
      this.tombstoneDeleteAttemptedCount === 0 && this.uiMutatedCount === 0
  }

  recording(event: CanonicalLibraryMetadataObservationEvent): CanonicalLibraryMetadataObservationWindow {
    if (!this.enabled || event.count <= 0) {
      return this
    }
    let copy = this.clone()
    copy.apply(event)
    copy.refreshDiagnosticsSummary()
    return copy
  }

  recordingWriteSideResult(
    result: CanonicalLibraryMetadataCutoverResult,
    trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC,
    nodeRole: CanonicalProductionExecutionDomainRole = CanonicalProductionExecutionDomainRole.TEST_HARNESS,
    syncRunID: string | null = null
  ): CanonicalLibraryMetadataObservationWindow {
    if (!this.enabled) return this
    let copy = this.clone()
    const attemptedCount = Math.max(result.canaryAttemptedCount, result.commits.length)
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.CANONICAL_COMMIT_ATTEMPTED, attemptedCount, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.CANONICAL_COMMIT_SUCCEEDED, result.commits.filter(c => c.committed).length, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.CANONICAL_COMMIT_FAILED, result.commits.filter(c => !c.committed).length, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.ROLLBACK_ATTEMPTED, result.rollbackResults.length, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.ROLLBACK_SUCCEEDED, result.rollbackResults.filter(r => r.succeeded).length, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.ROLLBACK_FAILED, result.rollbackResults.filter(r => !r.succeeded).length, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.ROLLBACK_FATAL, result.rollbackResults.filter(r => r.fatal).length, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.LEGACY_FALLBACK_USED, result.legacyFallbackUsed ? 1 : 0, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.DUPLICATE_LEGACY_SUPPRESSED, result.duplicateLegacySuppressedActionIDs.length, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.RESOURCE_MOVE_ATTEMPTED, result.diagnostics.filter(d => d.kind === CanonicalLibraryMetadataCutoverDiagnosticKind.CANONICAL_LIBRARY_METADATA_RESOURCE_MOVE_BLOCKED).length, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.TOMBSTONE_DELETE_ATTEMPTED, result.commits.filter(c => c.actionKind === 'tombstoneMarkerUnsupportedForThisRound').length, syncRunID, trigger, nodeRole))
    copy.appendDiagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.CANONICAL_LIBRARY_METADATA_OBSERVATION_WRITE_SIDE_RECORDED, trigger, nodeRole, syncRunID, 'recorded',
      'attempted=' + attemptedCount + ',succeeded=' + result.commits.filter(c => c.committed).length + ',rollbackFailures=' + result.rollbackResults.filter(r => !r.succeeded).length)
    copy.refreshDiagnosticsSummary()
    return copy
  }

  recordingReadSideCutoverResult(
    result: any,
    trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC,
    nodeRole: CanonicalProductionExecutionDomainRole = CanonicalProductionExecutionDomainRole.TEST_HARNESS,
    syncRunID: string | null = null
  ): CanonicalLibraryMetadataObservationWindow {
    if (!this.enabled) return this
    let copy = this.clone()
    if (result.diffReport) {
      copy.apply(new CanonicalLibraryMetadataObservationEvent(
        result.diffReport.equivalent ? CanonicalLibraryMetadataObservationEventKind.READ_SIDE_PARALLEL_EQUIVALENT : CanonicalLibraryMetadataObservationEventKind.READ_SIDE_PARALLEL_DIVERGENT,
        undefined, syncRunID, trigger, nodeRole))
      copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.READ_SIDE_UNSUPPORTED_OBJECT, result.diffReport.unsupportedObjectCount, syncRunID, trigger, nodeRole))
      copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.READ_SIDE_PATH_LEAK_RISK, result.diffReport.pathLeakRiskCount, syncRunID, trigger, nodeRole))
    }
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.LEGACY_READ_FALLBACK_USED, result.legacyReadFallbackAvailable ? 1 : 0, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.SYNC_OR_UPLOAD_TRIGGERED, result.syncOrUploadTriggered ? 1 : 0, syncRunID, trigger, nodeRole))
    copy.apply(new CanonicalLibraryMetadataObservationEvent(CanonicalLibraryMetadataObservationEventKind.UI_MUTATED, result.uiMutated ? 1 : 0, syncRunID, trigger, nodeRole))
    copy.appendDiagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.CANONICAL_LIBRARY_METADATA_OBSERVATION_READ_SIDE_RECORDED, trigger, nodeRole, syncRunID,
      result.candidate.ready ? 'readCandidateReady' : 'readCandidateBlocked', result.candidate.diagnosticsSummary)
    copy.refreshDiagnosticsSummary()
    return copy
  }

  private apply(event: CanonicalLibraryMetadataObservationEvent): void {
    switch (event.kind) {
      case CanonicalLibraryMetadataObservationEventKind.WINDOW_STARTED: break
      case CanonicalLibraryMetadataObservationEventKind.WINDOW_COMPLETED: break
      case CanonicalLibraryMetadataObservationEventKind.CANONICAL_COMMIT_ATTEMPTED: this.writeCanonicalCommitAttemptCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.CANONICAL_COMMIT_SUCCEEDED: this.writeCanonicalCommitSucceededCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.CANONICAL_COMMIT_FAILED: this.writeCanonicalCommitFailedCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.ROLLBACK_ATTEMPTED: this.rollbackAttemptCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.ROLLBACK_SUCCEEDED: this.rollbackSucceededCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.ROLLBACK_FAILED: this.rollbackFailedCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.ROLLBACK_FATAL: this.rollbackFatalCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.LEGACY_FALLBACK_USED: this.legacyFallbackUsedCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.DUPLICATE_LEGACY_SUPPRESSED: this.duplicateLegacySuppressedCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.UNRESOLVED_CONFLICT_OBSERVED: this.unresolvedConflictCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.RESOURCE_MOVE_ATTEMPTED: this.resourceMoveAttemptedCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.CANONICAL_READ_CANDIDATE_BUILT: this.canonicalReadCandidateBuiltCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.CANONICAL_READ_SERVED: this.canonicalReadServedCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.LEGACY_READ_FALLBACK_USED: this.legacyReadFallbackCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.READ_SIDE_PARALLEL_EQUIVALENT: this.readSideParallelEquivalentCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.READ_SIDE_PARALLEL_DIVERGENT: this.readSideParallelDivergentCount += event.count; this.readSideDivergenceCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.READ_SIDE_UNSUPPORTED_OBJECT: this.readSideUnsupportedObjectCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.READ_SIDE_PATH_LEAK_RISK: this.readSidePathLeakRiskCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.UNSAFE_SIDE_EFFECT_OBSERVED: this.unsafeSideEffectCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.SYNC_OR_UPLOAD_TRIGGERED: this.syncOrUploadTriggeredCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.UI_MUTATED: this.uiMutatedCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.CONTENT_WRITTEN: this.contentWrittenCount += event.count; break
      case CanonicalLibraryMetadataObservationEventKind.TOMBSTONE_DELETE_ATTEMPTED: this.tombstoneDeleteAttemptedCount += event.count; break
    }
  }

  private appendDiagnostic(kind: CanonicalLibraryMetadataCutoverDiagnosticKind, trigger: CanonicalSyncPlanTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole, syncRunID: string | null, result: string, reason: string): void {
    this.diagnostics.push(new CanonicalLibraryMetadataCutoverDiagnostic(kind, syncRunID, trigger, nodeRole, CanonicalLibraryMetadataCutoverDomain.FOLDER_METADATA, undefined, undefined, undefined, result, reason))
  }

  private refreshDiagnosticsSummary(): void {
    this.diagnosticsSummary = [
      'v8.20', 'domain=libraryMetadata', 'enabled=' + this.enabled,
      'explicitInternalTest=' + this.explicitInternalTestConfiguration,
      'writeAttempts=' + this.writeCanonicalCommitAttemptCount,
      'writeSucceeded=' + this.writeCanonicalCommitSucceededCount,
      'readEvidence=' + this.readEvidenceCount,
      'divergence=' + this.readSideDivergenceCount,
      'rollbackFailures=' + this.rollbackFailedCount,
      'unsafeSideEffects=' + this.unsafeSideEffectCount,
      'fallback=' + (this.legacyFallbackUsedCount + this.legacyReadFallbackCount),
      'runtimeSwitch=' + this.runtimeSwitchEnabled,
      'defaultRead=' + this.defaultCanonicalReadEnabled,
      'defaultWrite=' + this.defaultCanonicalWriteEnabled
    ].join(',')
  }

  private clone(): CanonicalLibraryMetadataObservationWindow {
    const c = new CanonicalLibraryMetadataObservationWindow(this.observationWindowID, CanonicalLibraryMetadataObservationPolicy.DISABLED)
    Object.assign(c, this)
    c.diagnostics = [...this.diagnostics]
    return c
  }
}

// ── Observation Failure / Gate ──

export enum CanonicalLibraryMetadataObservationFailure {
  DISABLED = 'disabled',
  MISSING_EXPLICIT_INTERNAL_TEST_CONFIGURATION = 'missingExplicitInternalTestConfiguration',
  NON_LIBRARY_METADATA_ACTIVE_PILOT = 'nonLibraryMetadataActivePilot',
  OTHER_ACTIVE_DOMAIN = 'otherActiveDomain',
  OTHER_DOMAINS_NOT_STATIC_ONLY = 'otherDomainsNotStaticOnly',
  WRITE_SIDE_EVIDENCE_MISSING = 'writeSideEvidenceMissing',
  READ_SIDE_EVIDENCE_MISSING = 'readSideEvidenceMissing',
  OBSERVATION_WINDOW_INCOMPLETE = 'observationWindowIncomplete',
  FALLBACK_MISSING = 'fallbackMissing',
  DIVERGENCE_PRESENT = 'divergencePresent',
  ROLLBACK_FAILURE = 'rollbackFailure',
  UNSUPPORTED_OBJECT = 'unsupportedObject',
  PATH_LEAK_RISK = 'pathLeakRisk',
  UNSAFE_SIDE_EFFECT = 'unsafeSideEffect',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  DEFAULT_CUTOVER_ENABLED = 'defaultCutoverEnabled'
}

export enum CanonicalLibraryMetadataObservationGateState {
  INCOMPLETE = 'incomplete',
  COMPLETE_BUT_RETIREMENT_BLOCKED = 'completeButRetirementBlocked',
  COMPLETE_READY_FOR_RETIREMENT_CANDIDATE = 'completeReadyForRetirementCandidate',
  BLOCKED_BY_DIVERGENCE = 'blockedByDivergence',
  BLOCKED_BY_ROLLBACK_FAILURE = 'blockedByRollbackFailure',
  BLOCKED_BY_UNSUPPORTED_OBJECT = 'blockedByUnsupportedObject',
  BLOCKED_BY_FALLBACK_MISSING = 'blockedByFallbackMissing',
  BLOCKED_BY_OTHER_ACTIVE_DOMAIN = 'blockedByOtherActiveDomain',
  BLOCKED_BY_UNSAFE_SIDE_EFFECT = 'blockedByUnsafeSideEffect'
}

export class CanonicalLibraryMetadataObservationGateResult {
  state: CanonicalLibraryMetadataObservationGateState = CanonicalLibraryMetadataObservationGateState.INCOMPLETE
  complete: boolean = false
  retirementCandidateReady: boolean = false
  blockers: CanonicalLibraryMetadataObservationFailure[] = []
  diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[] = []
  diagnosticsSummary: string = ''
}

// ── Observation Gate ──

export function evaluateObservationGate(
  window: CanonicalLibraryMetadataObservationWindow,
  policy: CanonicalLibraryMetadataObservationPolicy,
  matrix: CanonicalMigrationDomainMatrix = CanonicalMigrationDomainMatrix.defaultV813(),
  trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC,
  nodeRole: CanonicalProductionExecutionDomainRole = CanonicalProductionExecutionDomainRole.TEST_HARNESS,
  syncRunID: string | null = null
): CanonicalLibraryMetadataObservationGateResult {
  const blockers: CanonicalLibraryMetadataObservationFailure[] = []
  const matrixReport = matrix.validate()

  if (!policy.enabled || !window.enabled) blockers.push(CanonicalLibraryMetadataObservationFailure.DISABLED)
  if (!policy.explicitInternalTestConfiguration || !window.explicitInternalTestConfiguration) blockers.push(CanonicalLibraryMetadataObservationFailure.MISSING_EXPLICIT_INTERNAL_TEST_CONFIGURATION)
  if (policy.requireOnlyLibraryMetadataActivePilot) {
    if (matrixReport.activePilotDomain !== CanonicalMigrationDomain.LIBRARY_METADATA) blockers.push(CanonicalLibraryMetadataObservationFailure.NON_LIBRARY_METADATA_ACTIVE_PILOT)
    if (matrixReport.blockers.includes('multipleActivePilots')) blockers.push(CanonicalLibraryMetadataObservationFailure.OTHER_ACTIVE_DOMAIN)
  }
  if (!window.otherDomainsStaticOnly) blockers.push(CanonicalLibraryMetadataObservationFailure.OTHER_DOMAINS_NOT_STATIC_ONLY)
  if (window.runtimeSwitchEnabled) blockers.push(CanonicalLibraryMetadataObservationFailure.RUNTIME_SWITCH_ENABLED)
  if (window.defaultCanonicalReadEnabled || window.defaultCanonicalWriteEnabled) blockers.push(CanonicalLibraryMetadataObservationFailure.DEFAULT_CUTOVER_ENABLED)
  if (window.writeCanonicalCommitSucceededCount < policy.minimumWriteCanonicalCommitCount) blockers.push(CanonicalLibraryMetadataObservationFailure.WRITE_SIDE_EVIDENCE_MISSING)
  if (window.readEvidenceCount < policy.minimumReadCanonicalEvidenceCount) blockers.push(CanonicalLibraryMetadataObservationFailure.READ_SIDE_EVIDENCE_MISSING)
  if (window.totalEventCount < policy.minimumTotalEventCount) blockers.push(CanonicalLibraryMetadataObservationFailure.OBSERVATION_WINDOW_INCOMPLETE)
  if (policy.requireLegacyFallbackAvailable && !policy.legacyFallbackAvailable) blockers.push(CanonicalLibraryMetadataObservationFailure.FALLBACK_MISSING)
  if (window.legacyFallbackUsedCount + window.legacyReadFallbackCount === 0 && policy.requireLegacyFallbackAvailable) blockers.push(CanonicalLibraryMetadataObservationFailure.FALLBACK_MISSING)
  if (window.readSideDivergenceCount > 0) blockers.push(CanonicalLibraryMetadataObservationFailure.DIVERGENCE_PRESENT)
  if (window.rollbackFailedCount > 0 || window.rollbackFatalCount > 0) blockers.push(CanonicalLibraryMetadataObservationFailure.ROLLBACK_FAILURE)
  if (window.readSideUnsupportedObjectCount > 0) blockers.push(CanonicalLibraryMetadataObservationFailure.UNSUPPORTED_OBJECT)
  if (window.readSidePathLeakRiskCount > 0) blockers.push(CanonicalLibraryMetadataObservationFailure.PATH_LEAK_RISK)
  if (!window.noUnsafeSideEffects) blockers.push(CanonicalLibraryMetadataObservationFailure.UNSAFE_SIDE_EFFECT)

  const uniqueBlockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
  const complete = uniqueBlockers.length === 0
  let state: CanonicalLibraryMetadataObservationGateState
  if (uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.UNSAFE_SIDE_EFFECT) ||
    uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.PATH_LEAK_RISK) ||
    uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.RUNTIME_SWITCH_ENABLED) ||
    uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.DEFAULT_CUTOVER_ENABLED)) {
    state = CanonicalLibraryMetadataObservationGateState.BLOCKED_BY_UNSAFE_SIDE_EFFECT
  } else if (uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.OTHER_ACTIVE_DOMAIN) ||
    uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.NON_LIBRARY_METADATA_ACTIVE_PILOT) ||
    uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.OTHER_DOMAINS_NOT_STATIC_ONLY)) {
    state = CanonicalLibraryMetadataObservationGateState.BLOCKED_BY_OTHER_ACTIVE_DOMAIN
  } else if (uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.ROLLBACK_FAILURE)) {
    state = CanonicalLibraryMetadataObservationGateState.BLOCKED_BY_ROLLBACK_FAILURE
  } else if (uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.DIVERGENCE_PRESENT)) {
    state = CanonicalLibraryMetadataObservationGateState.BLOCKED_BY_DIVERGENCE
  } else if (uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.UNSUPPORTED_OBJECT)) {
    state = CanonicalLibraryMetadataObservationGateState.BLOCKED_BY_UNSUPPORTED_OBJECT
  } else if (uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.FALLBACK_MISSING)) {
    state = CanonicalLibraryMetadataObservationGateState.BLOCKED_BY_FALLBACK_MISSING
  } else if (uniqueBlockers.length === 0) {
    state = CanonicalLibraryMetadataObservationGateState.COMPLETE_READY_FOR_RETIREMENT_CANDIDATE
  } else if (uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.WRITE_SIDE_EVIDENCE_MISSING) ||
    uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.READ_SIDE_EVIDENCE_MISSING) ||
    uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.OBSERVATION_WINDOW_INCOMPLETE) ||
    uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.DISABLED) ||
    uniqueBlockers.includes(CanonicalLibraryMetadataObservationFailure.MISSING_EXPLICIT_INTERNAL_TEST_CONFIGURATION)) {
    state = CanonicalLibraryMetadataObservationGateState.INCOMPLETE
  } else {
    state = CanonicalLibraryMetadataObservationGateState.COMPLETE_BUT_RETIREMENT_BLOCKED
  }

  const diagnostics = [
    new CanonicalLibraryMetadataCutoverDiagnostic(
      CanonicalLibraryMetadataCutoverDiagnosticKind.CANONICAL_LIBRARY_METADATA_OBSERVATION_GATE_EVALUATED, syncRunID, trigger, nodeRole,
      undefined, undefined, undefined, undefined, state, 'reportOnly=true'),
    new CanonicalLibraryMetadataCutoverDiagnostic(
      complete ? CanonicalLibraryMetadataCutoverDiagnosticKind.CANONICAL_LIBRARY_METADATA_OBSERVATION_GATE_READY : CanonicalLibraryMetadataCutoverDiagnosticKind.CANONICAL_LIBRARY_METADATA_OBSERVATION_GATE_BLOCKED,
      syncRunID, trigger, nodeRole, undefined, undefined, undefined, undefined, complete ? 'ready' : 'blocked', uniqueBlockers.join('+'))
  ]
  return {
    state, complete, retirementCandidateReady: complete, blockers: uniqueBlockers, diagnostics,
    diagnosticsSummary: 'v8.20,observationGate=' + state + ',complete=' + complete +
      ',candidateReady=' + complete + ',blockers=' + uniqueBlockers.join('+') + ',reportOnly=true'
  }
}

// ── Retirement Candidate Report ──

export enum CanonicalLibraryMetadataRetirementCandidateGateStatus {
  READY = 'ready',
  BLOCKED = 'blocked'
}

export enum CanonicalLibraryMetadataRetirementBlocker {
  OBSERVATION_WINDOW_INCOMPLETE = 'observationWindowIncomplete',
  MANUAL_AUDIT_REQUIRED = 'manualAuditRequired',
  FALLBACK_MISSING = 'fallbackMissing',
  DIVERGENCE_PRESENT = 'divergencePresent',
  UNSUPPORTED_OBJECT = 'unsupportedObject',
  ROLLBACK_FATAL = 'rollbackFatal',
  OTHER_DOMAINS_AFFECTED = 'otherDomainsAffected',
  UNSAFE_SIDE_EFFECT = 'unsafeSideEffect',
  PATH_LEAK_RISK = 'pathLeakRisk',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  DEFAULT_READ_OR_WRITE_CUTOVER_ENABLED = 'defaultReadOrWriteCutoverEnabled'
}

export class CanonicalLibraryMetadataRetirementCandidateReport {
  status: CanonicalLibraryMetadataRetirementCandidateGateStatus = CanonicalLibraryMetadataRetirementCandidateGateStatus.BLOCKED
  retirementCandidateReady: boolean = false
  retirementExecutionPerformed: boolean = false
  legacyDeleted: boolean = false
  legacyDisabled: boolean = false
  reportOnly: boolean = true
  manualAuditRequired: boolean = true
  blockers: CanonicalLibraryMetadataRetirementBlocker[] = []
  observationGate: CanonicalLibraryMetadataObservationGateResult | null = null
  diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[] = []
  diagnosticsSummary: string = ''
}

export function evaluateRetirementCandidateGate(
  observationGate: CanonicalLibraryMetadataObservationGateResult,
  policy: CanonicalLibraryMetadataObservationPolicy,
  trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC,
  nodeRole: CanonicalProductionExecutionDomainRole = CanonicalProductionExecutionDomainRole.TEST_HARNESS,
  syncRunID: string | null = null
): CanonicalLibraryMetadataRetirementCandidateReport {
  const blockers: CanonicalLibraryMetadataRetirementBlocker[] = []
  if (!observationGate.complete || !observationGate.retirementCandidateReady) blockers.push(CanonicalLibraryMetadataRetirementBlocker.OBSERVATION_WINDOW_INCOMPLETE)
  if (!policy.manualAuditRequired) blockers.push(CanonicalLibraryMetadataRetirementBlocker.MANUAL_AUDIT_REQUIRED)
  if (observationGate.blockers.includes(CanonicalLibraryMetadataObservationFailure.FALLBACK_MISSING)) blockers.push(CanonicalLibraryMetadataRetirementBlocker.FALLBACK_MISSING)
  if (observationGate.blockers.includes(CanonicalLibraryMetadataObservationFailure.DIVERGENCE_PRESENT)) blockers.push(CanonicalLibraryMetadataRetirementBlocker.DIVERGENCE_PRESENT)
  if (observationGate.blockers.includes(CanonicalLibraryMetadataObservationFailure.UNSUPPORTED_OBJECT)) blockers.push(CanonicalLibraryMetadataRetirementBlocker.UNSUPPORTED_OBJECT)
  if (observationGate.blockers.includes(CanonicalLibraryMetadataObservationFailure.ROLLBACK_FAILURE)) blockers.push(CanonicalLibraryMetadataRetirementBlocker.ROLLBACK_FATAL)
  if (observationGate.blockers.includes(CanonicalLibraryMetadataObservationFailure.OTHER_ACTIVE_DOMAIN) ||
    observationGate.blockers.includes(CanonicalLibraryMetadataObservationFailure.OTHER_DOMAINS_NOT_STATIC_ONLY)) blockers.push(CanonicalLibraryMetadataRetirementBlocker.OTHER_DOMAINS_AFFECTED)
  if (observationGate.blockers.includes(CanonicalLibraryMetadataObservationFailure.UNSAFE_SIDE_EFFECT)) blockers.push(CanonicalLibraryMetadataRetirementBlocker.UNSAFE_SIDE_EFFECT)
  if (observationGate.blockers.includes(CanonicalLibraryMetadataObservationFailure.PATH_LEAK_RISK)) blockers.push(CanonicalLibraryMetadataRetirementBlocker.PATH_LEAK_RISK)
  if (observationGate.blockers.includes(CanonicalLibraryMetadataObservationFailure.RUNTIME_SWITCH_ENABLED)) blockers.push(CanonicalLibraryMetadataRetirementBlocker.RUNTIME_SWITCH_ENABLED)
  if (observationGate.blockers.includes(CanonicalLibraryMetadataObservationFailure.DEFAULT_CUTOVER_ENABLED)) blockers.push(CanonicalLibraryMetadataRetirementBlocker.DEFAULT_READ_OR_WRITE_CUTOVER_ENABLED)

  const uniqueBlockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
  const ready = uniqueBlockers.length === 0
  const diagnostics = [
    new CanonicalLibraryMetadataCutoverDiagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.CANONICAL_LIBRARY_METADATA_RETIREMENT_CANDIDATE_GATE_EVALUATED, syncRunID, trigger, nodeRole, undefined, undefined, undefined, undefined, ready ? 'ready' : 'blocked', 'reportOnly=true,manualAuditRequired=' + policy.manualAuditRequired),
    new CanonicalLibraryMetadataCutoverDiagnostic(ready ? CanonicalLibraryMetadataCutoverDiagnosticKind.CANONICAL_LIBRARY_METADATA_RETIREMENT_CANDIDATE_READY : CanonicalLibraryMetadataCutoverDiagnosticKind.CANONICAL_LIBRARY_METADATA_RETIREMENT_CANDIDATE_GATE_BLOCKED, syncRunID, trigger, nodeRole, undefined, undefined, undefined, undefined, ready ? 'candidate' : 'blocked', uniqueBlockers.join('+'))
  ]
  return {
    status: ready ? CanonicalLibraryMetadataRetirementCandidateGateStatus.READY : CanonicalLibraryMetadataRetirementCandidateGateStatus.BLOCKED,
    retirementCandidateReady: ready, retirementExecutionPerformed: false, legacyDeleted: false,
    legacyDisabled: false, reportOnly: true, manualAuditRequired: policy.manualAuditRequired,
    blockers: uniqueBlockers, observationGate, diagnostics,
    diagnosticsSummary: 'v8.20,retirementCandidateReady=' + ready +
      ',retirementExecutionPerformed=false,legacyDeleted=false,legacyDisabled=false,manualAuditRequired=' +
      policy.manualAuditRequired + ',reportOnly=true,blockers=' + uniqueBlockers.join('+')
  }
}

// ── Rollback Drill Summary ──

export class CanonicalLibraryMetadataRollbackDrillSummary {
  attemptedCount: number
  succeededCount: number
  failedCount: number
  fatalCount: number
  clean: boolean
  diagnosticsSummary: string

  constructor(window: CanonicalLibraryMetadataObservationWindow) {
    this.attemptedCount = window.rollbackAttemptCount
    this.succeededCount = window.rollbackSucceededCount
    this.failedCount = window.rollbackFailedCount
    this.fatalCount = window.rollbackFatalCount
    this.clean = window.rollbackFailedCount === 0 && window.rollbackFatalCount === 0
    this.diagnosticsSummary = 'v8.20,rollbackDrill,attempted=' + this.attemptedCount + ',succeeded=' +
      this.succeededCount + ',failed=' + this.failedCount + ',fatal=' + this.fatalCount + ',clean=' + this.clean
  }
}

// ── End-to-End Pilot ──

export enum CanonicalLibraryMetadataEndToEndPilotStatus {
  PILOT_INCOMPLETE = 'pilotIncomplete',
  PILOT_WRITE_SIDE_ONLY = 'pilotWriteSideOnly',
  PILOT_READ_SIDE_PARALLEL_ONLY = 'pilotReadSideParallelOnly',
  PILOT_OBSERVATION_READY = 'pilotObservationReady',
  PILOT_RETIREMENT_CANDIDATE_READY = 'pilotRetirementCandidateReady',
  BLOCKED = 'blocked'
}

export enum CanonicalLibraryMetadataEndToEndPilotBlocker {
  OBSERVATION_BLOCKED = 'observationBlocked',
  RETIREMENT_CANDIDATE_BLOCKED = 'retirementCandidateBlocked',
  WRITE_SIDE_MISSING = 'writeSideMissing',
  READ_SIDE_MISSING = 'readSideMissing'
}

export class CanonicalLibraryMetadataEndToEndPilotReport {
  status: CanonicalLibraryMetadataEndToEndPilotStatus = CanonicalLibraryMetadataEndToEndPilotStatus.PILOT_INCOMPLETE
  observationWindow: CanonicalLibraryMetadataObservationWindow | null = null
  observationGate: CanonicalLibraryMetadataObservationGateResult | null = null
  retirementCandidateReport: CanonicalLibraryMetadataRetirementCandidateReport | null = null
  rollbackDrillSummary: CanonicalLibraryMetadataRollbackDrillSummary | null = null
  blockers: CanonicalLibraryMetadataEndToEndPilotBlocker[] = []
  diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[] = []
  diagnosticsSummary: string = ''

  constructor(
    observationWindow: CanonicalLibraryMetadataObservationWindow,
    observationGate: CanonicalLibraryMetadataObservationGateResult,
    retirementCandidateReport: CanonicalLibraryMetadataRetirementCandidateReport
  ) {
    this.observationWindow = observationWindow
    this.observationGate = observationGate
    this.retirementCandidateReport = retirementCandidateReport
    this.rollbackDrillSummary = new CanonicalLibraryMetadataRollbackDrillSummary(observationWindow)
    const blockers: CanonicalLibraryMetadataEndToEndPilotBlocker[] = []
    if (observationWindow.writeCanonicalCommitSucceededCount === 0) blockers.push(CanonicalLibraryMetadataEndToEndPilotBlocker.WRITE_SIDE_MISSING)
    if (observationWindow.readEvidenceCount === 0) blockers.push(CanonicalLibraryMetadataEndToEndPilotBlocker.READ_SIDE_MISSING)
    if (!observationGate.complete) blockers.push(CanonicalLibraryMetadataEndToEndPilotBlocker.OBSERVATION_BLOCKED)
    if (!retirementCandidateReport.retirementCandidateReady) blockers.push(CanonicalLibraryMetadataEndToEndPilotBlocker.RETIREMENT_CANDIDATE_BLOCKED)
    this.blockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
    if (retirementCandidateReport.retirementCandidateReady) {
      this.status = CanonicalLibraryMetadataEndToEndPilotStatus.PILOT_RETIREMENT_CANDIDATE_READY
    } else if (observationGate.complete) {
      this.status = CanonicalLibraryMetadataEndToEndPilotStatus.PILOT_OBSERVATION_READY
    } else if (observationWindow.writeCanonicalCommitSucceededCount > 0 && observationWindow.readEvidenceCount === 0) {
      this.status = CanonicalLibraryMetadataEndToEndPilotStatus.PILOT_WRITE_SIDE_ONLY
    } else if (observationWindow.writeCanonicalCommitSucceededCount === 0 && observationWindow.readEvidenceCount > 0) {
      this.status = CanonicalLibraryMetadataEndToEndPilotStatus.PILOT_READ_SIDE_PARALLEL_ONLY
    } else if (this.blockers.includes(CanonicalLibraryMetadataEndToEndPilotBlocker.OBSERVATION_BLOCKED) ||
      this.blockers.includes(CanonicalLibraryMetadataEndToEndPilotBlocker.RETIREMENT_CANDIDATE_BLOCKED)) {
      this.status = CanonicalLibraryMetadataEndToEndPilotStatus.BLOCKED
    } else {
      this.status = CanonicalLibraryMetadataEndToEndPilotStatus.PILOT_INCOMPLETE
    }
    const generated = new CanonicalLibraryMetadataCutoverDiagnostic(
      CanonicalLibraryMetadataCutoverDiagnosticKind.CANONICAL_LIBRARY_METADATA_END_TO_END_PILOT_REPORT_GENERATED,
      null, CanonicalSyncPlanTrigger.PERIODIC, CanonicalProductionExecutionDomainRole.TEST_HARNESS,
      undefined, undefined, undefined, undefined, this.status, this.blockers.join('+'))
    this.diagnostics = [...observationWindow.diagnostics, ...observationGate.diagnostics,
      ...retirementCandidateReport.diagnostics, generated]
    this.diagnosticsSummary = ['v8.20', 'endToEndStatus=' + this.status,
      'retirementCandidateReady=' + retirementCandidateReport.retirementCandidateReady,
      'legacyDeleted=false', 'legacyDisabled=false', 'runtimeSwitch=false',
      'blockers=' + this.blockers.join('+')].join(',')
  }
}
