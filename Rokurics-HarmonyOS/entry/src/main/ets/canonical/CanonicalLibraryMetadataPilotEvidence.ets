/**
 * CanonicalLibraryMetadataPilotEvidence — N1 evidence validation, N3 readiness gate.
 * Ported from Rokurics-Apple (CanonicalLibraryMetadataPilotEvidence.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalObjectKind,
  nilIfEmpty, hashPrefix
} from './CanonicalCore'
import {
  CanonicalMigrationDomain,
  CanonicalLibraryMetadataCutoverDomain,
  CanonicalLibraryMetadataCutoverFailure,
  CanonicalLibraryMetadataCutoverEvidence,
  CanonicalLibraryMetadataCutoverDiagnostic,
  CanonicalLibraryMetadataCanaryCandidateSafetyKind,
  CanonicalLibraryMetadataDebugPilotMode,
  CanonicalLibraryMetadataProductionCanaryRootMode,
  CanonicalLibraryMetadataLandingReport,
  CanonicalLibraryMetadataRealCanaryObservationReport,
  CanonicalMigrationLandingFreezeResult,
  CanonicalProductionExecutionDomainRole
} from './CanonicalLibraryMetadataLanding'
import { CanonicalSyncPlanTrigger } from './CanonicalLibrarySyncPlanner'
import { CanonicalLibraryMetadataCanaryCandidate, CanonicalLibraryMetadataCanaryCandidateSafety } from './CanonicalLibraryMetadataCutover'

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

// ── N1 Evidence ──

export enum CanonicalLibraryMetadataN1EvidenceStatus {
  MISSING = 'missing',
  PRESENT = 'present',
  VALID = 'valid',
  INVALID = 'invalid',
  INSUFFICIENT = 'insufficient',
  BLOCKED = 'blocked'
}

export enum CanonicalLibraryMetadataN1EvidenceBlocker {
  MISSING_N1_EVIDENCE = 'missingN1Evidence',
  INVALID_EVIDENCE = 'invalidEvidence',
  READ_SIDE_DIVERGENCE = 'readSideDivergence',
  ROLLBACK_FAILURE = 'rollbackFailure',
  DUPLICATE_SUPPRESSION_WITHOUT_COMMIT_SUCCESS = 'duplicateSuppressionWithoutCommitSuccess',
  OTHER_ACTIVE_DOMAIN = 'otherActiveDomain',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  RELEASE_DEFAULT_ENABLED = 'releaseDefaultEnabled',
  SENSITIVE_DATA_LEAK = 'sensitiveDataLeak',
  PRODUCTION_ROOT_SAFETY_PROOF_MISSING = 'productionRootSafetyProofMissing',
  PRODUCTION_ROOT_SAFETY_PROOF_INVALID = 'productionRootSafetyProofInvalid',
  LANDING_FREEZE_MISSING = 'landingFreezeMissing',
  LANDING_FREEZE_BLOCKED = 'landingFreezeBlocked',
  OTHER_DOMAINS_NOT_STATIC_ONLY = 'otherDomainsNotStaticOnly',
  UNSAFE_CANDIDATE_EXECUTED = 'unsafeCandidateExecuted',
  UNSAFE_CANDIDATE_KIND = 'unsafeCandidateKind',
  RESOURCE_MOVE = 'resourceMove',
  CONTENT_WRITE = 'contentWrite',
  GENERATED_ARTIFACT_WRITE = 'generatedArtifactWrite',
  AUDIO_CHANGE = 'audioChange',
  READ_PATH_SWITCHED = 'readPathSwitched',
  UI_MUTATED = 'uiMutated',
  TOMBSTONE_DELETE = 'tombstoneDelete',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  DEFAULT_RELEASE_ENABLED = 'defaultReleaseEnabled',
  REQUEST_VERIFIER_ROUTE_BOUNDARY_VIOLATION = 'requestVerifierRouteBoundaryViolation',
  RECEIVE_JSON_MUTATION = 'receiveJSONMutation',
  N3_EXECUTION_REPORTED = 'n3ExecutionReported',
  N3_DISABLED_BY_DEFAULT_MISSING = 'n3DisabledByDefaultMissing',
  MANUAL_AUDIT_MISSING = 'manualAuditMissing',
  OWNER_APPROVAL_MISSING = 'ownerApprovalMissing'
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
    this.redactedTargetSummary = CanonicalLibraryMetadataN1EvidenceRedactor.staticRedact(redactedTargetSummary)
    this.redacted = redacted
  }

  static redactedValidN1(
    checkpointID: string = 'library-metadata-n1-checkpoint',
    candidateKind: CanonicalLibraryMetadataCanaryCandidateSafetyKind = CanonicalLibraryMetadataCanaryCandidateSafetyKind.FOLDER_RENAME_OR_COLOR_METADATA,
    objectKind: CanonicalObjectKind = CanonicalObjectKind.FOLDER,
    domain: CanonicalLibraryMetadataCutoverDomain = CanonicalLibraryMetadataCutoverDomain.FOLDER_METADATA
  ): CanonicalLibraryMetadataProductionRootSafetyProof {
    return new CanonicalLibraryMetadataProductionRootSafetyProof(
      true, true, true, checkpointID, true, true, true, true, true,
      true, true, true,
      ['domain=' + domain, 'objectKind=' + objectKind, 'candidateKind=' + candidateKind, 'hashPrefix=fixture'].join(','),
      true
    )
  }
}

// ── N1 Evidence Redactor ──

export class CanonicalLibraryMetadataN1EvidenceRedactor {
  constructor() {}

  redact(value: string | null): string {
    if (!value || value.length === 0) return 'redacted'
    if (CanonicalLibraryMetadataN1EvidenceRedactor.containsSensitiveSignalLocal(value)) {
      const digest = CanonicalHash.sha256OfString(value).value
      return 'redacted-' + (CanonicalProductionRedaction.hashPrefix(digest) ?? 'evidence')
    }
    return CanonicalProductionRedaction.safeDiagnosticText(value) ?? 'redacted'
  }

  static staticRedact(value: string | null): string {
    return new CanonicalLibraryMetadataN1EvidenceRedactor().redact(value)
  }

  static containsSensitiveSignalLocal(value: string): boolean {
    if (!value || value.length === 0) return false
    if (CanonicalProductionRedaction.containsSensitivePathSignal(value)) return true
    const lowercased = value.toLowerCase()
    const tokens = ['api_key', 'apikey', 'secret', 'shared secret', 'token=', 'bearer ',
      'fingerprint', 'private-key', 'private key', 'request body', 'response body',
      'metadata json', 'transcript', 'provider response', 'standalone note content',
      'note content', 'summary content']
    if (tokens.some(t => lowercased.includes(t))) return true
    let hexRun = 0
    for (let i = 0; i < lowercased.length; i++) {
      const code = lowercased.charCodeAt(i)
      if ((code >= 48 && code <= 57) || (code >= 97 && code <= 102)) {
        hexRun++
        if (hexRun >= 32) return true
      } else {
        hexRun = 0
      }
    }
    return false
  }
}

// ── N1 Evidence Source ──

export class CanonicalLibraryMetadataN1EvidenceSource {
  sourceReportID: string | null
  sourceDiagnosticsID: string | null
  landingReport: CanonicalLibraryMetadataLandingReport | null
  observationReport: CanonicalLibraryMetadataRealCanaryObservationReport | null
  productionRootSafetyProof: CanonicalLibraryMetadataProductionRootSafetyProof | null
  landingFreezeResult: CanonicalMigrationLandingFreezeResult | null
  diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[]
  validNoChangeOrNoEligibleEvidenceAccepted: boolean
  routeBoundaryViolationReported: boolean
  receiveJSONMutationReported: boolean
  generatedArtifactWriteReported: boolean
  audioChangeReported: boolean
  standaloneNoteContentWriteReported: boolean
  tombstoneDeleteReported: boolean
  defaultCutoverEnabledReported: boolean
  releaseDefaultEnabledReported: boolean
  sensitiveDataLeakReported: boolean
  n3ExecutionReported: boolean
  manualAuditRequired: boolean
  ownerApprovalRequiredForNextStage: boolean
  n3DisabledByDefault: boolean

  constructor(
    sourceReportID: string | null = null,
    sourceDiagnosticsID: string | null = null,
    landingReport: CanonicalLibraryMetadataLandingReport | null = null,
    observationReport: CanonicalLibraryMetadataRealCanaryObservationReport | null = null,
    productionRootSafetyProof: CanonicalLibraryMetadataProductionRootSafetyProof | null = null,
    landingFreezeResult: CanonicalMigrationLandingFreezeResult | null = null,
    diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[] = [],
    validNoChangeOrNoEligibleEvidenceAccepted: boolean = false,
    routeBoundaryViolationReported: boolean = false,
    receiveJSONMutationReported: boolean = false,
    generatedArtifactWriteReported: boolean = false,
    audioChangeReported: boolean = false,
    standaloneNoteContentWriteReported: boolean = false,
    tombstoneDeleteReported: boolean = false,
    defaultCutoverEnabledReported: boolean = false,
    releaseDefaultEnabledReported: boolean = false,
    sensitiveDataLeakReported: boolean = false,
    n3ExecutionReported: boolean = false,
    manualAuditRequired: boolean = true,
    ownerApprovalRequiredForNextStage: boolean = true,
    n3DisabledByDefault: boolean = true
  ) {
    const redactor = new CanonicalLibraryMetadataN1EvidenceRedactor()
    const rawSensitiveDataLeak = sensitiveDataLeakReported ||
      redactor.redact(sourceReportID).startsWith('redacted') ||
      redactor.redact(sourceDiagnosticsID).startsWith('redacted')
    this.sourceReportID = sourceReportID ? redactor.redact(sourceReportID) : null
    this.sourceDiagnosticsID = sourceDiagnosticsID ? redactor.redact(sourceDiagnosticsID) : null
    this.landingReport = landingReport
    this.observationReport = observationReport
    this.productionRootSafetyProof = productionRootSafetyProof
    this.landingFreezeResult = landingFreezeResult
    this.diagnostics = diagnostics
    this.validNoChangeOrNoEligibleEvidenceAccepted = validNoChangeOrNoEligibleEvidenceAccepted
    this.routeBoundaryViolationReported = routeBoundaryViolationReported
    this.receiveJSONMutationReported = receiveJSONMutationReported
    this.generatedArtifactWriteReported = generatedArtifactWriteReported
    this.audioChangeReported = audioChangeReported
    this.standaloneNoteContentWriteReported = standaloneNoteContentWriteReported
    this.tombstoneDeleteReported = tombstoneDeleteReported
    this.defaultCutoverEnabledReported = defaultCutoverEnabledReported
    this.releaseDefaultEnabledReported = releaseDefaultEnabledReported
    this.sensitiveDataLeakReported = rawSensitiveDataLeak
    this.n3ExecutionReported = n3ExecutionReported
    this.manualAuditRequired = manualAuditRequired
    this.ownerApprovalRequiredForNextStage = ownerApprovalRequiredForNextStage
    this.n3DisabledByDefault = n3DisabledByDefault
  }

  get hasEvidence(): boolean {
    return this.landingReport !== null || this.observationReport !== null ||
      this.productionRootSafetyProof !== null || this.landingFreezeResult !== null ||
      this.diagnostics.length > 0
  }
}

// ── N1 Evidence Bundle ──

export class CanonicalLibraryMetadataN1EvidenceBundle {
  status: CanonicalLibraryMetadataN1EvidenceStatus = CanonicalLibraryMetadataN1EvidenceStatus.PRESENT
  landingMode: CanonicalLibraryMetadataDebugPilotMode | null = null
  rootMode: CanonicalLibraryMetadataProductionCanaryRootMode | null = null
  activePilot: CanonicalMigrationDomain | null = null
  selectedCandidateCount: number = 0
  executedCandidateCount: number = 0
  selectedCandidateKind: CanonicalLibraryMetadataCanaryCandidateSafetyKind | null = null
  selectedCandidateDomain: CanonicalLibraryMetadataCutoverDomain | null = null
  canaryAttempted: boolean = false
  canarySucceeded: boolean = false
  commitSuccessCount: number = 0
  rollbackAttempted: boolean = false
  rollbackSucceeded: boolean = false
  rollbackFailureCount: number = 0
  legacyFallbackUsed: boolean = false
  legacyFallbackAvailable: boolean = false
  duplicateSuppressionCount: number = 0
  readSideEquivalenceStatus: CanonicalLibraryMetadataN1EvidenceStatus = CanonicalLibraryMetadataN1EvidenceStatus.MISSING
  readSideParallelExecuted: boolean = false
  readSideDivergenceCount: number = 0
  productionRootSafetyProofStatus: CanonicalLibraryMetadataN1EvidenceStatus = CanonicalLibraryMetadataN1EvidenceStatus.MISSING
  landingFreezeStatus: CanonicalLibraryMetadataN1EvidenceStatus = CanonicalLibraryMetadataN1EvidenceStatus.MISSING
  otherDomainsStaticOnlyStatus: CanonicalLibraryMetadataN1EvidenceStatus = CanonicalLibraryMetadataN1EvidenceStatus.MISSING
  runtimeSwitchStatus: CanonicalLibraryMetadataN1EvidenceStatus = CanonicalLibraryMetadataN1EvidenceStatus.MISSING
  releaseDefaultDisabledStatus: CanonicalLibraryMetadataN1EvidenceStatus = CanonicalLibraryMetadataN1EvidenceStatus.MISSING
  diagnosticsRedactedStatus: CanonicalLibraryMetadataN1EvidenceStatus = CanonicalLibraryMetadataN1EvidenceStatus.MISSING
  resourceMoveCount: number = 0
  contentWriteCount: number = 0
  generatedArtifactWriteCount: number = 0
  audioChangeCount: number = 0
  tombstoneDeleteCount: number = 0
  unsafeCandidateExecutionCount: number = 0
  readPathSwitched: boolean = false
  uiMutated: boolean = false
  routeBoundaryViolationReported: boolean = false
  receiveJSONMutationReported: boolean = false
  validNoChangeOrNoEligibleEvidenceAccepted: boolean = false
  n3ExecutionReported: boolean = false
  manualAuditRequired: boolean = true
  ownerApprovalRequiredForNextStage: boolean = true
  n3DisabledByDefault: boolean = true
  source: CanonicalLibraryMetadataN1EvidenceSource | null = null
  sourceReportID: string | null = null
  sourceDiagnosticsID: string | null = null
  blockers: CanonicalLibraryMetadataN1EvidenceBlocker[] = []
  diagnosticsSummary: string = ''
  redacted: boolean = true

  constructor(source: CanonicalLibraryMetadataN1EvidenceSource) {
    this.source = source
    this.sourceReportID = source.sourceReportID
    this.sourceDiagnosticsID = source.sourceDiagnosticsID
    this.landingMode = source.landingReport?.mode ?? null
    this.rootMode = source.landingReport?.rootMode ?? source.observationReport?.rootMode as any ?? null
    this.activePilot = source.landingReport?.activePilot ??
      source.observationReport?.domain as any ?? null
    this.selectedCandidateCount = source.landingReport?.candidate.selected ? 1 : source.observationReport?.selectedCandidateCount ?? 0
    this.executedCandidateCount = source.observationReport?.executedCandidateCount ?? (source.landingReport?.commitAttempted === true ? 1 : 0)
    this.selectedCandidateKind = source.landingReport?.candidate.kind ?? null
    this.selectedCandidateDomain = source.landingReport?.candidate.domain ?? null
    this.canaryAttempted = source.landingReport?.commitAttempted ?? ((source.observationReport?.productionRootWriteAttempted ?? false) || (source.observationReport?.executedCandidateCount ?? 0) > 0)
    this.canarySucceeded = source.landingReport?.commitSucceeded ?? ((source.observationReport?.successfulCommitCount ?? 0) > 0)
    this.commitSuccessCount = Math.max(0, source.observationReport?.successfulCommitCount ?? (source.landingReport?.commitSucceeded === true ? 1 : 0))
    this.rollbackAttempted = source.landingReport?.rollbackAttempted ?? ((source.observationReport?.rollbackCount ?? 0) > 0)
    this.rollbackFailureCount = Math.max(0, source.observationReport?.rollbackFailureCount ?? 0)
    this.rollbackSucceeded = source.landingReport?.rollbackSucceeded ?? ((source.observationReport?.rollbackCount ?? 0) > 0 && this.rollbackFailureCount === 0)
    this.legacyFallbackUsed = source.landingReport?.legacyFallbackUsed ?? ((source.observationReport?.legacyFallbackCount ?? 0) > 0)
    this.legacyFallbackAvailable = source.landingReport?.legacyReadPathPreserved ?? source.observationReport?.legacyFallbackPreserved ?? false
    this.duplicateSuppressionCount = Math.max(0, source.landingReport?.duplicateSuppressedCount ?? source.observationReport?.duplicateSuppressionCount ?? 0)
    const readEquivalent = source.landingReport?.readSideEquivalent ?? source.observationReport?.readSideParallelEquivalent
    const readDivergence = Math.max(0, source.landingReport?.readSideDivergenceCount ?? (source.observationReport?.readSideParallelDivergent === true ? 1 : 0))
    this.readSideEquivalenceStatus = readEquivalent === null || readEquivalent === undefined
      ? CanonicalLibraryMetadataN1EvidenceStatus.MISSING
      : (readEquivalent === true && readDivergence === 0 ? CanonicalLibraryMetadataN1EvidenceStatus.VALID : CanonicalLibraryMetadataN1EvidenceStatus.INVALID)
    this.readSideParallelExecuted = readEquivalent !== null && readEquivalent !== undefined
    this.readSideDivergenceCount = readDivergence

    // Production root safety proof
    if (source.productionRootSafetyProof) {
      const p = source.productionRootSafetyProof
      this.productionRootSafetyProofStatus = p.redacted && p.rootContainmentVerified && p.productionRootModeExplicit &&
        p.logicalTokenSafety && p.atomicWriteUsed && p.postconditionVerified && p.rollbackAvailable &&
        p.rollbackVerifiedIfUsed && p.sideEffectWhitelistPassed && p.noResourceMove && p.noContentWrite &&
        p.noOtherDomainMutation ? CanonicalLibraryMetadataN1EvidenceStatus.VALID : CanonicalLibraryMetadataN1EvidenceStatus.INVALID
    } else {
      this.productionRootSafetyProofStatus = CanonicalLibraryMetadataN1EvidenceStatus.MISSING
    }

    // Landing freeze
    if (source.landingFreezeResult) {
      this.landingFreezeStatus = source.landingFreezeResult.allowed ? CanonicalLibraryMetadataN1EvidenceStatus.VALID : CanonicalLibraryMetadataN1EvidenceStatus.BLOCKED
      this.otherDomainsStaticOnlyStatus = source.landingFreezeResult.otherDomainsStaticOnly ? CanonicalLibraryMetadataN1EvidenceStatus.VALID : CanonicalLibraryMetadataN1EvidenceStatus.INVALID
      this.runtimeSwitchStatus = source.landingFreezeResult.runtimeSwitchEnabled ? CanonicalLibraryMetadataN1EvidenceStatus.INVALID : CanonicalLibraryMetadataN1EvidenceStatus.VALID
    } else if (source.landingReport) {
      this.landingFreezeStatus = source.landingReport.freezeViolations.length === 0 ? CanonicalLibraryMetadataN1EvidenceStatus.VALID : CanonicalLibraryMetadataN1EvidenceStatus.BLOCKED
      this.otherDomainsStaticOnlyStatus = source.landingReport.otherDomainsStaticOnly ? CanonicalLibraryMetadataN1EvidenceStatus.VALID : CanonicalLibraryMetadataN1EvidenceStatus.INVALID
      this.runtimeSwitchStatus = source.landingReport.runtimeSwitchEnabled ? CanonicalLibraryMetadataN1EvidenceStatus.INVALID : CanonicalLibraryMetadataN1EvidenceStatus.VALID
    }

    const releaseDefaultEnabled = source.defaultCutoverEnabledReported || source.releaseDefaultEnabledReported
    this.releaseDefaultDisabledStatus = releaseDefaultEnabled ? CanonicalLibraryMetadataN1EvidenceStatus.INVALID : CanonicalLibraryMetadataN1EvidenceStatus.VALID
    this.diagnosticsRedactedStatus = source.sensitiveDataLeakReported ? CanonicalLibraryMetadataN1EvidenceStatus.INVALID : CanonicalLibraryMetadataN1EvidenceStatus.VALID
    this.resourceMoveCount = (source.landingReport?.candidate.resourceMoveAttempted === true || source.observationReport?.resourceMoved === true) ? 1 : 0
    this.contentWriteCount = (source.landingReport?.candidate.contentBytesMutated === true || source.standaloneNoteContentWriteReported) ? 1 : 0
    this.generatedArtifactWriteCount = source.generatedArtifactWriteReported ? 1 : 0
    this.audioChangeCount = source.audioChangeReported ? 1 : 0
    this.tombstoneDeleteCount = source.tombstoneDeleteReported ? 1 : 0
    this.unsafeCandidateExecutionCount = (this.resourceMoveCount + this.contentWriteCount + this.generatedArtifactWriteCount + this.audioChangeCount + this.tombstoneDeleteCount) > 0 && this.canaryAttempted ? 1 : 0
    this.readPathSwitched = source.landingReport?.uiReadPathSwitched === true
    this.uiMutated = source.observationReport?.uiMutated === true
    this.routeBoundaryViolationReported = source.routeBoundaryViolationReported
    this.receiveJSONMutationReported = source.receiveJSONMutationReported
    this.validNoChangeOrNoEligibleEvidenceAccepted = source.validNoChangeOrNoEligibleEvidenceAccepted
    this.n3ExecutionReported = source.n3ExecutionReported
    this.manualAuditRequired = source.manualAuditRequired
    this.ownerApprovalRequiredForNextStage = source.ownerApprovalRequiredForNextStage
    this.n3DisabledByDefault = source.n3DisabledByDefault

    // Validate
    const blockers: CanonicalLibraryMetadataN1EvidenceBlocker[] = []
    if (!source.hasEvidence) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.MISSING_N1_EVIDENCE)
    if (this.activePilot !== null && this.activePilot !== CanonicalMigrationDomain.LIBRARY_METADATA) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.OTHER_ACTIVE_DOMAIN)
    if (this.selectedCandidateCount > 1) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.INVALID_EVIDENCE)
    if (this.executedCandidateCount > 1) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.INVALID_EVIDENCE)
    if (this.selectedCandidateKind === CanonicalLibraryMetadataCanaryCandidateSafetyKind.BLOCKED) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.UNSAFE_CANDIDATE_KIND)
    if (this.readSideDivergenceCount > 0 || this.readSideEquivalenceStatus === CanonicalLibraryMetadataN1EvidenceStatus.INVALID) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.READ_SIDE_DIVERGENCE)
    if (this.rollbackFailureCount > 0 || (this.rollbackAttempted && !this.rollbackSucceeded)) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.ROLLBACK_FAILURE)
    if (this.duplicateSuppressionCount > 0 && this.commitSuccessCount === 0) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.DUPLICATE_SUPPRESSION_WITHOUT_COMMIT_SUCCESS)
    if (this.productionRootSafetyProofStatus === CanonicalLibraryMetadataN1EvidenceStatus.MISSING) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.PRODUCTION_ROOT_SAFETY_PROOF_MISSING)
    else if (this.productionRootSafetyProofStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.PRODUCTION_ROOT_SAFETY_PROOF_INVALID)
    if (this.landingFreezeStatus === CanonicalLibraryMetadataN1EvidenceStatus.MISSING) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.LANDING_FREEZE_MISSING)
    else if (this.landingFreezeStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.LANDING_FREEZE_BLOCKED)
    if (this.otherDomainsStaticOnlyStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.OTHER_DOMAINS_NOT_STATIC_ONLY)
    if (this.runtimeSwitchStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.RUNTIME_SWITCH_ENABLED)
    if (this.releaseDefaultDisabledStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.RELEASE_DEFAULT_ENABLED)
    if (this.diagnosticsRedactedStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.SENSITIVE_DATA_LEAK)
    if (this.resourceMoveCount > 0) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.RESOURCE_MOVE)
    if (this.contentWriteCount > 0) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.CONTENT_WRITE)
    if (this.generatedArtifactWriteCount > 0) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.GENERATED_ARTIFACT_WRITE)
    if (this.audioChangeCount > 0) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.AUDIO_CHANGE)
    if (this.tombstoneDeleteCount > 0) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.TOMBSTONE_DELETE)
    if (this.unsafeCandidateExecutionCount > 0) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.UNSAFE_CANDIDATE_EXECUTED)
    if (this.readPathSwitched) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.READ_PATH_SWITCHED)
    if (this.uiMutated) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.UI_MUTATED)
    if (!this.legacyFallbackAvailable) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.LEGACY_FALLBACK_UNAVAILABLE)
    if (this.routeBoundaryViolationReported) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.REQUEST_VERIFIER_ROUTE_BOUNDARY_VIOLATION)
    if (this.receiveJSONMutationReported) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.RECEIVE_JSON_MUTATION)
    if (this.n3ExecutionReported) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.N3_EXECUTION_REPORTED)
    if (!this.manualAuditRequired) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.MANUAL_AUDIT_MISSING)
    if (!this.ownerApprovalRequiredForNextStage) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.OWNER_APPROVAL_MISSING)
    if (!this.n3DisabledByDefault) blockers.push(CanonicalLibraryMetadataN1EvidenceBlocker.N3_DISABLED_BY_DEFAULT_MISSING)

    const uniqueBlockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
    if (uniqueBlockers.includes(CanonicalLibraryMetadataN1EvidenceBlocker.MISSING_N1_EVIDENCE)) this.status = CanonicalLibraryMetadataN1EvidenceStatus.MISSING
    else if (uniqueBlockers.includes(CanonicalLibraryMetadataN1EvidenceBlocker.SENSITIVE_DATA_LEAK) ||
      uniqueBlockers.includes(CanonicalLibraryMetadataN1EvidenceBlocker.DUPLICATE_SUPPRESSION_WITHOUT_COMMIT_SUCCESS) ||
      uniqueBlockers.includes(CanonicalLibraryMetadataN1EvidenceBlocker.INVALID_EVIDENCE) ||
      uniqueBlockers.includes(CanonicalLibraryMetadataN1EvidenceBlocker.REQUEST_VERIFIER_ROUTE_BOUNDARY_VIOLATION) ||
      uniqueBlockers.includes(CanonicalLibraryMetadataN1EvidenceBlocker.RECEIVE_JSON_MUTATION)) this.status = CanonicalLibraryMetadataN1EvidenceStatus.INVALID
    else if (uniqueBlockers.includes(CanonicalLibraryMetadataN1EvidenceBlocker.PRODUCTION_ROOT_SAFETY_PROOF_MISSING) ||
      uniqueBlockers.includes(CanonicalLibraryMetadataN1EvidenceBlocker.LANDING_FREEZE_MISSING) ||
      this.readSideEquivalenceStatus === CanonicalLibraryMetadataN1EvidenceStatus.MISSING) this.status = CanonicalLibraryMetadataN1EvidenceStatus.INSUFFICIENT
    else if (uniqueBlockers.length === 0) this.status = CanonicalLibraryMetadataN1EvidenceStatus.VALID
    else this.status = CanonicalLibraryMetadataN1EvidenceStatus.BLOCKED

    this.blockers = uniqueBlockers
    this.diagnosticsSummary = 'status=' + this.status + ',mode=' + (this.landingMode ?? 'none') +
      ',rootMode=' + (this.rootMode ?? 'none') + ',activePilot=' + (this.activePilot ?? 'none') +
      ',selected=' + this.selectedCandidateCount + ',executed=' + this.executedCandidateCount +
      ',commitSuccess=' + this.commitSuccessCount + ',rollbackFailure=' + this.rollbackFailureCount +
      ',readSideDivergence=' + this.readSideDivergenceCount + ',duplicateSuppression=' + this.duplicateSuppressionCount +
      ',runtimeSwitchStatus=' + this.runtimeSwitchStatus + ',releaseDefaultStatus=' + this.releaseDefaultDisabledStatus +
      ',blockers=' + this.blockers.join('|') + ',redacted=true'
    this.redacted = true
  }
}

// ── N1 Post-Run Invariant ──

export enum CanonicalLibraryMetadataN1PostRunInvariant {
  CANDIDATE_COUNT_AT_MOST_ONE = 'candidateCountAtMostOne',
  CANDIDATE_DOMAIN_LIBRARY_METADATA = 'candidateDomainLibraryMetadata',
  CANDIDATE_KIND_SAFE_METADATA_ONLY = 'candidateKindSafeMetadataOnly',
  NO_RESOURCE_MOVE = 'noResourceMove',
  NO_STANDALONE_NOTE_CONTENT_WRITE = 'noStandaloneNoteContentWrite',
  NO_TOMBSTONE_DELETE = 'noTombstoneDelete',
  NO_GENERATED_ARTIFACT_WRITE = 'noGeneratedArtifactWrite',
  NO_AUDIO_CHANGE = 'noAudioChange',
  NO_READ_PATH_SWITCH = 'noReadPathSwitch',
  UI_NOT_MUTATED = 'uiNotMutated',
  RUNTIME_SWITCH_FALSE = 'runtimeSwitchFalse',
  LEGACY_FALLBACK_AVAILABLE = 'legacyFallbackAvailable',
  READ_SIDE_PARALLEL_EXECUTED = 'readSideParallelExecuted',
  READ_SIDE_NO_DIVERGENCE = 'readSideNoDivergence',
  ROLLBACK_FAILURE_ZERO = 'rollbackFailureZero',
  DUPLICATE_SUPPRESSION_REQUIRES_COMMIT_SUCCESS = 'duplicateSuppressionRequiresCommitSuccess',
  OTHER_DOMAINS_STATIC_ONLY = 'otherDomainsStaticOnly',
  DEFAULT_RELEASE_DISABLED = 'defaultReleaseDisabled',
  REQUEST_VERIFIER_ROUTE_BOUNDARY_PRESERVED = 'requestVerifierRouteBoundaryPreserved',
  RECEIVE_JSON_UNCHANGED = 'receiveJSONUnchanged'
}

export class CanonicalLibraryMetadataN1PostRunViolation {
  invariant: CanonicalLibraryMetadataN1PostRunInvariant
  summary: string
  redacted: boolean

  constructor(invariant: CanonicalLibraryMetadataN1PostRunInvariant, summary: string) {
    this.invariant = invariant
    this.summary = CanonicalLibraryMetadataN1EvidenceRedactor.staticRedact(summary)
    this.redacted = true
  }

  get id(): string {
    return this.invariant
  }
}

export function validateN1PostRunInvariants(bundle: CanonicalLibraryMetadataN1EvidenceBundle): CanonicalLibraryMetadataN1PostRunViolation[] {
  const violations: CanonicalLibraryMetadataN1PostRunViolation[] = []
  function a(invariant: CanonicalLibraryMetadataN1PostRunInvariant, summary: string): void {
    violations.push(new CanonicalLibraryMetadataN1PostRunViolation(invariant, summary))
  }
  if (bundle.executedCandidateCount > 1 || bundle.selectedCandidateCount > 1) a(CanonicalLibraryMetadataN1PostRunInvariant.CANDIDATE_COUNT_AT_MOST_ONE, 'selectedCandidateCount=' + bundle.selectedCandidateCount + ',executedCandidateCount=' + bundle.executedCandidateCount)
  if (bundle.executedCandidateCount > 0 && bundle.activePilot !== CanonicalMigrationDomain.LIBRARY_METADATA) a(CanonicalLibraryMetadataN1PostRunInvariant.CANDIDATE_DOMAIN_LIBRARY_METADATA, 'activePilot=' + (bundle.activePilot ?? 'none'))
  if (bundle.selectedCandidateKind === CanonicalLibraryMetadataCanaryCandidateSafetyKind.BLOCKED || bundle.unsafeCandidateExecutionCount > 0) a(CanonicalLibraryMetadataN1PostRunInvariant.CANDIDATE_KIND_SAFE_METADATA_ONLY, 'kind=' + (bundle.selectedCandidateKind ?? 'none'))
  if (bundle.resourceMoveCount > 0) a(CanonicalLibraryMetadataN1PostRunInvariant.NO_RESOURCE_MOVE, 'resourceMoveCount=' + bundle.resourceMoveCount)
  if (bundle.contentWriteCount > 0) a(CanonicalLibraryMetadataN1PostRunInvariant.NO_STANDALONE_NOTE_CONTENT_WRITE, 'contentWriteCount=' + bundle.contentWriteCount)
  if (bundle.tombstoneDeleteCount > 0) a(CanonicalLibraryMetadataN1PostRunInvariant.NO_TOMBSTONE_DELETE, 'tombstoneDeleteCount=' + bundle.tombstoneDeleteCount)
  if (bundle.generatedArtifactWriteCount > 0) a(CanonicalLibraryMetadataN1PostRunInvariant.NO_GENERATED_ARTIFACT_WRITE, 'generatedArtifactWriteCount=' + bundle.generatedArtifactWriteCount)
  if (bundle.audioChangeCount > 0) a(CanonicalLibraryMetadataN1PostRunInvariant.NO_AUDIO_CHANGE, 'audioChangeCount=' + bundle.audioChangeCount)
  if (bundle.readPathSwitched) a(CanonicalLibraryMetadataN1PostRunInvariant.NO_READ_PATH_SWITCH, 'readPathSwitched=true')
  if (bundle.uiMutated) a(CanonicalLibraryMetadataN1PostRunInvariant.UI_NOT_MUTATED, 'uiMutated=true')
  if (bundle.runtimeSwitchStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) a(CanonicalLibraryMetadataN1PostRunInvariant.RUNTIME_SWITCH_FALSE, 'runtimeSwitchStatus=' + bundle.runtimeSwitchStatus)
  if (!bundle.legacyFallbackAvailable) a(CanonicalLibraryMetadataN1PostRunInvariant.LEGACY_FALLBACK_AVAILABLE, 'legacyFallbackAvailable=false')
  if (!bundle.readSideParallelExecuted) a(CanonicalLibraryMetadataN1PostRunInvariant.READ_SIDE_PARALLEL_EXECUTED, 'readSideParallelExecuted=false')
  if (bundle.readSideDivergenceCount > 0) a(CanonicalLibraryMetadataN1PostRunInvariant.READ_SIDE_NO_DIVERGENCE, 'readSideDivergenceCount=' + bundle.readSideDivergenceCount)
  if (bundle.rollbackFailureCount > 0) a(CanonicalLibraryMetadataN1PostRunInvariant.ROLLBACK_FAILURE_ZERO, 'rollbackFailureCount=' + bundle.rollbackFailureCount)
  if (bundle.duplicateSuppressionCount > 0 && bundle.commitSuccessCount === 0) a(CanonicalLibraryMetadataN1PostRunInvariant.DUPLICATE_SUPPRESSION_REQUIRES_COMMIT_SUCCESS, 'duplicateSuppressionWithoutCommitSuccess')
  if (bundle.otherDomainsStaticOnlyStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) a(CanonicalLibraryMetadataN1PostRunInvariant.OTHER_DOMAINS_STATIC_ONLY, 'otherDomainsStaticOnlyStatus=' + bundle.otherDomainsStaticOnlyStatus)
  if (bundle.releaseDefaultDisabledStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) a(CanonicalLibraryMetadataN1PostRunInvariant.DEFAULT_RELEASE_DISABLED, 'releaseDefaultDisabledStatus=' + bundle.releaseDefaultDisabledStatus)
  if (bundle.routeBoundaryViolationReported) a(CanonicalLibraryMetadataN1PostRunInvariant.REQUEST_VERIFIER_ROUTE_BOUNDARY_PRESERVED, 'routeBoundaryViolationReported=true')
  if (bundle.receiveJSONMutationReported) a(CanonicalLibraryMetadataN1PostRunInvariant.RECEIVE_JSON_UNCHANGED, 'receiveJSONMutationReported=true')
  return violations
}

// ── N3 Readiness ──

export enum CanonicalLibraryMetadataN3ReadinessStatus {
  READY_FOR_N3_AFTER_MANUAL_AUDIT = 'readyForN3AfterManualAudit',
  BLOCKED_MISSING_EVIDENCE = 'blockedMissingEvidence',
  BLOCKED_INVALID_EVIDENCE = 'blockedInvalidEvidence',
  BLOCKED_ROLLBACK_FAILURE = 'blockedRollbackFailure',
  BLOCKED_READ_SIDE_DIVERGENCE = 'blockedReadSideDivergence',
  BLOCKED_UNSAFE_SIDE_EFFECT = 'blockedUnsafeSideEffect',
  BLOCKED_OTHER_DOMAIN_ACTIVE = 'blockedOtherDomainActive',
  BLOCKED_RELEASE_DEFAULT_ENABLED = 'blockedReleaseDefaultEnabled',
  BLOCKED_SENSITIVE_DATA_LEAK = 'blockedSensitiveDataLeak',
  BLOCKED = 'blocked'
}

export enum CanonicalLibraryMetadataN3ReadinessBlocker {
  MISSING_EVIDENCE = 'missingEvidence',
  INVALID_EVIDENCE = 'invalidEvidence',
  COMMIT_SUCCESS_OR_ACCEPTED_NO_CHANGE_MISSING = 'commitSuccessOrAcceptedNoChangeMissing',
  ROLLBACK_FAILURE = 'rollbackFailure',
  READ_SIDE_DIVERGENCE = 'readSideDivergence',
  UNSAFE_CANDIDATE_EXECUTION = 'unsafeCandidateExecution',
  RESOURCE_MOVE = 'resourceMove',
  CONTENT_WRITE = 'contentWrite',
  TOMBSTONE_DELETE = 'tombstoneDelete',
  GENERATED_ARTIFACT_WRITE = 'generatedArtifactWrite',
  AUDIO_CHANGE = 'audioChange',
  ROUTE_BOUNDARY_VIOLATION = 'routeBoundaryViolation',
  RECEIVE_JSON_MUTATION = 'receiveJSONMutation',
  OTHER_DOMAIN_ACTIVE = 'otherDomainActive',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  RELEASE_DEFAULT_ENABLED = 'releaseDefaultEnabled',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  MANUAL_AUDIT_REQUIRED = 'manualAuditRequired',
  OWNER_APPROVAL_REQUIRED = 'ownerApprovalRequired',
  N3_NOT_DISABLED_BY_DEFAULT = 'n3NotDisabledByDefault',
  SENSITIVE_DATA_LEAK = 'sensitiveDataLeak',
  N3_EXECUTION_ATTEMPTED = 'n3ExecutionAttempted'
}

export class CanonicalLibraryMetadataN3ReadinessResult {
  status: CanonicalLibraryMetadataN3ReadinessStatus = CanonicalLibraryMetadataN3ReadinessStatus.BLOCKED
  blockers: CanonicalLibraryMetadataN3ReadinessBlocker[] = []
  reportOnly: boolean = true
  n3ExecutionAttempted: boolean = false
  n3DisabledByDefault: boolean = true
  manualAuditRequired: boolean = true
  ownerApprovalRequired: boolean = true
  diagnosticsSummary: string = ''
  redacted: boolean = true
}

export function evaluateN3Readiness(bundle: CanonicalLibraryMetadataN1EvidenceBundle): CanonicalLibraryMetadataN3ReadinessResult {
  const blockers: CanonicalLibraryMetadataN3ReadinessBlocker[] = []
  if (bundle.status === CanonicalLibraryMetadataN1EvidenceStatus.MISSING) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.MISSING_EVIDENCE)
  if (bundle.status === CanonicalLibraryMetadataN1EvidenceStatus.INVALID) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.INVALID_EVIDENCE)
  if (bundle.status !== CanonicalLibraryMetadataN1EvidenceStatus.VALID && bundle.status !== CanonicalLibraryMetadataN1EvidenceStatus.BLOCKED) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.INVALID_EVIDENCE)
  if (bundle.commitSuccessCount === 0 && !bundle.validNoChangeOrNoEligibleEvidenceAccepted) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.COMMIT_SUCCESS_OR_ACCEPTED_NO_CHANGE_MISSING)
  if (bundle.rollbackFailureCount > 0 || (bundle.rollbackAttempted && !bundle.rollbackSucceeded)) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.ROLLBACK_FAILURE)
  if (bundle.readSideDivergenceCount > 0 || bundle.readSideEquivalenceStatus === CanonicalLibraryMetadataN1EvidenceStatus.INVALID) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.READ_SIDE_DIVERGENCE)
  if (bundle.unsafeCandidateExecutionCount > 0 || bundle.blockers.includes(CanonicalLibraryMetadataN1EvidenceBlocker.UNSAFE_CANDIDATE_KIND) || bundle.blockers.includes(CanonicalLibraryMetadataN1EvidenceBlocker.UNSAFE_CANDIDATE_EXECUTED)) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.UNSAFE_CANDIDATE_EXECUTION)
  if (bundle.resourceMoveCount > 0) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.RESOURCE_MOVE)
  if (bundle.contentWriteCount > 0) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.CONTENT_WRITE)
  if (bundle.tombstoneDeleteCount > 0) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.TOMBSTONE_DELETE)
  if (bundle.generatedArtifactWriteCount > 0) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.GENERATED_ARTIFACT_WRITE)
  if (bundle.audioChangeCount > 0) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.AUDIO_CHANGE)
  if (bundle.routeBoundaryViolationReported) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.ROUTE_BOUNDARY_VIOLATION)
  if (bundle.receiveJSONMutationReported) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.RECEIVE_JSON_MUTATION)
  if (bundle.activePilot !== CanonicalMigrationDomain.LIBRARY_METADATA) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.OTHER_DOMAIN_ACTIVE)
  if (bundle.runtimeSwitchStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.RUNTIME_SWITCH_ENABLED)
  if (bundle.releaseDefaultDisabledStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.RELEASE_DEFAULT_ENABLED)
  if (!bundle.legacyFallbackAvailable) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.LEGACY_FALLBACK_UNAVAILABLE)
  if (!bundle.manualAuditRequired) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.MANUAL_AUDIT_REQUIRED)
  if (!bundle.ownerApprovalRequiredForNextStage) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.OWNER_APPROVAL_REQUIRED)
  if (!bundle.n3DisabledByDefault) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.N3_NOT_DISABLED_BY_DEFAULT)
  if (bundle.diagnosticsRedactedStatus !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.SENSITIVE_DATA_LEAK)
  if (bundle.n3ExecutionReported) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.N3_EXECUTION_ATTEMPTED)
  if (blockers.length === 0 && bundle.status !== CanonicalLibraryMetadataN1EvidenceStatus.VALID) blockers.push(CanonicalLibraryMetadataN3ReadinessBlocker.INVALID_EVIDENCE)

  const uniqueBlockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
  let status: CanonicalLibraryMetadataN3ReadinessStatus
  if (uniqueBlockers.length === 0) status = CanonicalLibraryMetadataN3ReadinessStatus.READY_FOR_N3_AFTER_MANUAL_AUDIT
  else if (uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.MISSING_EVIDENCE)) status = CanonicalLibraryMetadataN3ReadinessStatus.BLOCKED_MISSING_EVIDENCE
  else if (uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.SENSITIVE_DATA_LEAK)) status = CanonicalLibraryMetadataN3ReadinessStatus.BLOCKED_SENSITIVE_DATA_LEAK
  else if (uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.INVALID_EVIDENCE) || uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.COMMIT_SUCCESS_OR_ACCEPTED_NO_CHANGE_MISSING)) status = CanonicalLibraryMetadataN3ReadinessStatus.BLOCKED_INVALID_EVIDENCE
  else if (uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.ROLLBACK_FAILURE)) status = CanonicalLibraryMetadataN3ReadinessStatus.BLOCKED_ROLLBACK_FAILURE
  else if (uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.READ_SIDE_DIVERGENCE)) status = CanonicalLibraryMetadataN3ReadinessStatus.BLOCKED_READ_SIDE_DIVERGENCE
  else if (uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.UNSAFE_CANDIDATE_EXECUTION) || uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.RESOURCE_MOVE) || uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.CONTENT_WRITE) || uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.TOMBSTONE_DELETE) || uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.GENERATED_ARTIFACT_WRITE) || uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.AUDIO_CHANGE) || uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.ROUTE_BOUNDARY_VIOLATION) || uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.RECEIVE_JSON_MUTATION)) status = CanonicalLibraryMetadataN3ReadinessStatus.BLOCKED_UNSAFE_SIDE_EFFECT
  else if (uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.OTHER_DOMAIN_ACTIVE)) status = CanonicalLibraryMetadataN3ReadinessStatus.BLOCKED_OTHER_DOMAIN_ACTIVE
  else if (uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.RELEASE_DEFAULT_ENABLED) || uniqueBlockers.includes(CanonicalLibraryMetadataN3ReadinessBlocker.RUNTIME_SWITCH_ENABLED)) status = CanonicalLibraryMetadataN3ReadinessStatus.BLOCKED_RELEASE_DEFAULT_ENABLED
  else status = CanonicalLibraryMetadataN3ReadinessStatus.BLOCKED

  return {
    status, blockers: uniqueBlockers, reportOnly: true, n3ExecutionAttempted: false,
    n3DisabledByDefault: bundle.n3DisabledByDefault, manualAuditRequired: true, ownerApprovalRequired: true,
    diagnosticsSummary: 'status=' + status + ',reportOnly=true,n3ExecutionAttempted=false,n3DisabledByDefault=' +
      bundle.n3DisabledByDefault + ',manualAuditRequired=true,ownerApprovalRequired=true,blockers=' +
      uniqueBlockers.join('|') + ',redacted=true',
    redacted: true
  }
}

// ── N1 Evidence Exporter ──

export class CanonicalLibraryMetadataN1EvidenceExporter {
  constructor() {}

  export(
    bundle: CanonicalLibraryMetadataN1EvidenceBundle,
    readiness: CanonicalLibraryMetadataN3ReadinessResult | null = null
  ): object {
    const readinessResult = readiness ?? evaluateN3Readiness(bundle)
    const summary = [
      'mode=' + (bundle.landingMode ?? 'none'),
      'rootMode=' + (bundle.rootMode ?? 'none'),
      'candidateCount=' + bundle.selectedCandidateCount,
      'executedCandidateCount=' + bundle.executedCandidateCount,
      'candidateKind=' + (bundle.selectedCandidateKind ?? 'none'),
      'successCount=' + bundle.commitSuccessCount,
      'rollbackAttempted=' + bundle.rollbackAttempted,
      'rollbackSucceeded=' + bundle.rollbackSucceeded,
      'rollbackFailureCount=' + bundle.rollbackFailureCount,
      'legacyFallbackUsed=' + bundle.legacyFallbackUsed,
      'duplicateSuppressionCount=' + bundle.duplicateSuppressionCount,
      'readSideStatus=' + bundle.readSideEquivalenceStatus,
      'readSideDivergenceCount=' + bundle.readSideDivergenceCount,
      'landingFreezeStatus=' + bundle.landingFreezeStatus,
      'otherDomainsStaticOnly=' + bundle.otherDomainsStaticOnlyStatus,
      'runtimeSwitchFalse=' + (bundle.runtimeSwitchStatus === CanonicalLibraryMetadataN1EvidenceStatus.VALID),
      'releaseDefaultDisabled=' + (bundle.releaseDefaultDisabledStatus === CanonicalLibraryMetadataN1EvidenceStatus.VALID),
      'n3ReadinessStatus=' + readinessResult.status,
      'reportOnly=' + readinessResult.reportOnly
    ]
    return {
      status: bundle.status,
      n3ReadinessStatus: readinessResult.status,
      summary: summary,
      blockers: [...bundle.blockers, ...readinessResult.blockers],
      redacted: bundle.redacted && readinessResult.redacted
    }
  }
}
