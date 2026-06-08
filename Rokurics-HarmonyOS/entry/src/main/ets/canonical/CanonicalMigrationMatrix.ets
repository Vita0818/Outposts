/**
 * CanonicalMigrationMatrix — migration domain matrix, policies, and validation.
 * Ported from Rokurics-Apple (CanonicalMigrationMatrix.swift).
 */

import {
  CanonicalProductionRedaction
} from './CanonicalProductionPorts'
import {
  CanonicalMigrationStage, CanonicalCutoverDomain,
  CanonicalMigrationStageSideEffect, CanonicalMigrationStageEvidence
} from './CanonicalNoCommitV82'

// =============================================================================
// MIGRATION DOMAIN & STAGE STATUS
// =============================================================================

export enum CanonicalMigrationDomain {
  RECORDING_METADATA = 'recordingMetadata',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  LIBRARY_METADATA = 'libraryMetadata',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  AUDIO_UPLOAD = 'audioUpload',
  UI_PROJECTION = 'uiProjection',
  LEGACY_RETIREMENT = 'legacyRetirement'
}

export enum CanonicalMigrationStageStatus {
  NOT_STARTED = 'notStarted',
  EVIDENCE_MISSING = 'evidenceMissing',
  COMPLETE = 'complete',
  ACTIVE_PILOT = 'activePilot',
  NEXT_PILOT_CANDIDATE = 'nextPilotCandidate',
  STATIC_ONLY = 'staticOnly',
  BLOCKED = 'blocked',
  WRITE_SIDE_CANARY_OBSERVED = 'writeSideCanaryObserved',
  READ_SIDE_OBSERVED = 'readSideObserved',
  OBSERVATION_COMPLETE = 'observationComplete',
  RETIREMENT_CANDIDATE_READY = 'retirementCandidateReady',
  RETIREMENT_BLOCKED = 'retirementBlocked'
}

export enum CanonicalMigrationDomainBlocker {
  MISSING_EXPLICIT_PILOT = 'missingExplicitPilot',
  MULTIPLE_ACTIVE_PILOTS = 'multipleActivePilots',
  NON_LIBRARY_METADATA_ACTIVE_PILOT = 'nonLibraryMetadataActivePilot',
  NON_PILOT_DOMAIN_NOT_STATIC_ONLY = 'nonPilotDomainNotStaticOnly',
  STAGE_SKIPPED = 'stageSkipped',
  CANARY_WITHOUT_PREVIOUS_STAGE_EVIDENCE = 'canaryWithoutPreviousStageEvidence',
  READ_SIDE_CUTOVER_WITHOUT_WRITE_SIDE_CUTOVER = 'readSideCutoverWithoutWriteSideCutover',
  RETIRED_WITHOUT_READ_SIDE_CUTOVER = 'retiredWithoutReadSideCutover',
  RETIRED_WITHOUT_OBSERVATION = 'retiredWithoutObservation',
  RETIRED_WITHOUT_FALLBACK_READINESS = 'retiredWithoutFallbackReadiness',
  AUDIO_UPLOAD_BLOCKED_UNTIL_LIBRARY_METADATA_PILOT_COMPLETE = 'audioUploadBlockedUntilLibraryMetadataPilotComplete',
  GENERATED_ARTIFACTS_NEXT_PILOT_BEFORE_LIBRARY_METADATA_OBSERVATION = 'generatedArtifactsNextPilotBeforeLibraryMetadataObservation',
  GENERATED_ARTIFACTS_ACTIVE_PILOT_DENIED = 'generatedArtifactsActivePilotDenied',
  GENERATED_ARTIFACTS_ACTIVE_PILOT_BEFORE_NEXT_PILOT_CANDIDATE = 'generatedArtifactsActivePilotBeforeNextPilotCandidate',
  GENERATED_ARTIFACTS_ACTIVE_PILOT_BEFORE_LIBRARY_METADATA_OBSERVATION = 'generatedArtifactsActivePilotBeforeLibraryMetadataObservation',
  GENERATED_ARTIFACTS_STAGED_CANARY_BEFORE_N1 = 'generatedArtifactsStagedCanaryBeforeN1',
  TOMBSTONE_CONFLICT_NEXT_PILOT_BEFORE_GENERATED_ARTIFACTS_OBSERVATION = 'tombstoneConflictNextPilotBeforeGeneratedArtifactsObservation',
  AUDIO_UPLOAD_ACTIVE_PILOT_DENIED = 'audioUploadActivePilotDenied',
  TOMBSTONE_CONFLICT_ACTIVE_PILOT_DENIED = 'tombstoneConflictActivePilotDenied',
  TOMBSTONE_CONFLICT_ACTIVE_PILOT_BEFORE_NEXT_PILOT_CANDIDATE = 'tombstoneConflictActivePilotBeforeNextPilotCandidate',
  TOMBSTONE_CONFLICT_ACTIVE_PILOT_BEFORE_GENERATED_ARTIFACTS_OBSERVATION = 'tombstoneConflictActivePilotBeforeGeneratedArtifactsObservation',
  LEGACY_RETIREMENT_ENABLED_DENIED = 'legacyRetirementEnabledDenied',
  RELEASE_DEFAULT_CUTOVER_ENABLED = 'releaseDefaultCutoverEnabled',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  LEGACY_RETIREMENT_BEFORE_READ_SIDE_CUTOVER = 'legacyRetirementBeforeReadSideCutover',
  DIAGNOSTICS_NOT_REDACTED = 'diagnosticsNotRedacted',
  MISSING_MACHINE_PARTS = 'missingMachineParts',
  MISSING_APP_SEAM = 'missingAppSeam',
  CUTOVER_NOT_DEFAULT_OFF = 'cutoverNotDefaultOff',
  PRODUCTION_INJECTION_PRESENT = 'productionInjectionPresent',
  READ_PATH_NOT_LEGACY = 'readPathNotLegacy',
  TESTS_MISSING = 'testsMissing'
}

// =============================================================================
// MIGRATION DOMAIN POLICY
// =============================================================================

export class CanonicalMigrationDomainPolicy {
  domain: CanonicalMigrationDomain
  stageStatuses: Map<CanonicalMigrationStage, CanonicalMigrationStageStatus>
  activePilot: boolean
  activePilotExplicit: boolean
  staticOnly: boolean
  blockedForRealMigration: boolean
  defaultCutoverEnabled: boolean
  releaseDefaultEnabledCutover: boolean
  runtimeSwitchEnabled: boolean
  legacySuppressionAllowed: boolean
  noProductionInjection: boolean
  readPathLegacy: boolean
  writeSideCutoverSucceeded: boolean
  observationComplete: boolean
  fallbackReady: boolean

  constructor(
    domain: CanonicalMigrationDomain,
    stageStatuses: Map<CanonicalMigrationStage, CanonicalMigrationStageStatus> = new Map(),
    activePilot: boolean = false,
    activePilotExplicit: boolean = false,
    staticOnly: boolean = true,
    blockedForRealMigration: boolean = true,
    defaultCutoverEnabled: boolean = false,
    releaseDefaultEnabledCutover: boolean = false,
    runtimeSwitchEnabled: boolean = false,
    legacySuppressionAllowed: boolean = false,
    noProductionInjection: boolean = true,
    readPathLegacy: boolean = true,
    writeSideCutoverSucceeded: boolean = false,
    observationComplete: boolean = false,
    fallbackReady: boolean = true
  ) {
    this.domain = domain
    this.stageStatuses = stageStatuses
    this.activePilot = activePilot
    this.activePilotExplicit = activePilotExplicit
    this.staticOnly = staticOnly
    this.blockedForRealMigration = blockedForRealMigration
    this.defaultCutoverEnabled = defaultCutoverEnabled
    this.releaseDefaultEnabledCutover = releaseDefaultEnabledCutover
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.legacySuppressionAllowed = legacySuppressionAllowed
    this.noProductionInjection = noProductionInjection
    this.readPathLegacy = readPathLegacy
    this.writeSideCutoverSucceeded = writeSideCutoverSucceeded
    this.observationComplete = observationComplete
    this.fallbackReady = fallbackReady
  }
}

// =============================================================================
// MIGRATION DOMAIN MATRIX
// =============================================================================

export class CanonicalMigrationMatrixReport {
  policies: CanonicalMigrationDomainPolicy[]
  activePilotDomain: CanonicalMigrationDomain | null
  blockers: CanonicalMigrationDomainBlocker[]
  diagnosticsSummary: string
  diagnosticsRedacted: boolean

  constructor(
    policies: CanonicalMigrationDomainPolicy[],
    activePilotDomain: CanonicalMigrationDomain | null,
    blockers: CanonicalMigrationDomainBlocker[],
    diagnosticsSummary: string,
    diagnosticsRedacted: boolean
  ) {
    this.policies = policies
    this.activePilotDomain = activePilotDomain
    this.blockers = blockers
    this.diagnosticsSummary = diagnosticsSummary
    this.diagnosticsRedacted = diagnosticsRedacted
  }

  get allowed(): boolean {
    return this.blockers.length === 0
  }
}

export class CanonicalMigrationDomainMatrix {
  policies: CanonicalMigrationDomainPolicy[]
  libraryMetadataPilotComplete: boolean
  libraryMetadataObservationCompleteOrRetirementCandidateReady: boolean
  generatedArtifactsTemplateCompleteOrObservationReady: boolean

  constructor(
    policies: CanonicalMigrationDomainPolicy[],
    libraryMetadataPilotComplete: boolean = false,
    libraryMetadataObservationCompleteOrRetirementCandidateReady: boolean = false,
    generatedArtifactsTemplateCompleteOrObservationReady: boolean = false
  ) {
    this.policies = policies.sort((a, b) => a.domain.localeCompare(b.domain))
    this.libraryMetadataPilotComplete = libraryMetadataPilotComplete
    this.libraryMetadataObservationCompleteOrRetirementCandidateReady =
      libraryMetadataObservationCompleteOrRetirementCandidateReady
    this.generatedArtifactsTemplateCompleteOrObservationReady =
      generatedArtifactsTemplateCompleteOrObservationReady
  }

  static readonly ORDERED_STAGES: CanonicalMigrationStage[] = [
    CanonicalMigrationStage.NOT_STARTED, CanonicalMigrationStage.PROJECTED,
    CanonicalMigrationStage.PLANNED, CanonicalMigrationStage.NO_COMMIT,
    CanonicalMigrationStage.REAL_APPLY_PORT, CanonicalMigrationStage.COMMIT_EXECUTOR,
    CanonicalMigrationStage.APP_SEAM_DEFAULT_OFF, CanonicalMigrationStage.NEXT_PILOT_CANDIDATE,
    CanonicalMigrationStage.CANARY_N0, CanonicalMigrationStage.CANARY_N1,
    CanonicalMigrationStage.EXPANDED_CANARY, CanonicalMigrationStage.DOMAIN_CUTOVER,
    CanonicalMigrationStage.READ_SIDE_PARALLEL, CanonicalMigrationStage.READ_SIDE_CUTOVER,
    CanonicalMigrationStage.RETIREMENT_CANDIDATE, CanonicalMigrationStage.RETIRED
  ]

  static defaultV813(): CanonicalMigrationDomainMatrix {
    return new CanonicalMigrationDomainMatrix([
      new CanonicalMigrationDomainPolicy(CanonicalMigrationDomain.RECORDING_METADATA),
      new CanonicalMigrationDomainPolicy(CanonicalMigrationDomain.GENERATED_ARTIFACTS),
      new CanonicalMigrationDomainPolicy(CanonicalMigrationDomain.LIBRARY_METADATA),
      new CanonicalMigrationDomainPolicy(CanonicalMigrationDomain.TOMBSTONE_CONFLICT),
      new CanonicalMigrationDomainPolicy(CanonicalMigrationDomain.AUDIO_UPLOAD),
      new CanonicalMigrationDomainPolicy(CanonicalMigrationDomain.UI_PROJECTION),
      new CanonicalMigrationDomainPolicy(CanonicalMigrationDomain.LEGACY_RETIREMENT)
    ])
  }

  policy(domain: CanonicalMigrationDomain): CanonicalMigrationDomainPolicy | undefined {
    return this.policies.find(p => p.domain === domain)
  }

  validate(): CanonicalMigrationMatrixReport {
    const blockers: CanonicalMigrationDomainBlocker[] = []
    const activePilotPolicies = this.policies.filter(p => p.activePilot)

    if (activePilotPolicies.length === 0) {
      blockers.push(CanonicalMigrationDomainBlocker.MISSING_EXPLICIT_PILOT)
    }
    if (activePilotPolicies.length > 1) {
      blockers.push(CanonicalMigrationDomainBlocker.MULTIPLE_ACTIVE_PILOTS)
    }

    for (const policy of this.policies) {
      if (policy.releaseDefaultEnabledCutover || policy.defaultCutoverEnabled) {
        blockers.push(CanonicalMigrationDomainBlocker.RELEASE_DEFAULT_CUTOVER_ENABLED)
      }
      if (policy.runtimeSwitchEnabled) {
        blockers.push(CanonicalMigrationDomainBlocker.RUNTIME_SWITCH_ENABLED)
      }
    }

    const summary = 'v8.13,domains=' + this.policies.map(p =>
      p.domain + ':active=' + p.activePilot + ':static=' + p.staticOnly
    ).join('|') + ',blockers=' + Array.from(new Set(blockers)).join('+')

    return new CanonicalMigrationMatrixReport(
      this.policies,
      activePilotPolicies.length === 1 ? activePilotPolicies[0].domain : null,
      Array.from(new Set(blockers)).sort((a, b) => a.localeCompare(b)),
      summary, true
    )
  }
}

// =============================================================================
// MIGRATION STATIC DOMAIN AUDIT
// =============================================================================

export class CanonicalMigrationStaticDomainAudit {
  domain: CanonicalMigrationDomain
  machinePartsPresent: boolean
  appSeamPresent: boolean
  defaultOff: boolean
  noProductionInjection: boolean
  readPathLegacy: boolean
  testsPresent: boolean
  blockers: CanonicalMigrationDomainBlocker[]
  staticReviewRecommended: boolean
  realMigrationBlocked: boolean
  diagnosticsSummary: string

  constructor(
    domain: CanonicalMigrationDomain,
    machinePartsPresent: boolean,
    appSeamPresent: boolean,
    defaultOff: boolean,
    noProductionInjection: boolean,
    readPathLegacy: boolean,
    testsPresent: boolean,
    staticReviewRecommended: boolean = true,
    realMigrationBlocked: boolean = true
  ) {
    const blockers: CanonicalMigrationDomainBlocker[] = []
    if (!machinePartsPresent) { blockers.push(CanonicalMigrationDomainBlocker.MISSING_MACHINE_PARTS) }
    if (!appSeamPresent) { blockers.push(CanonicalMigrationDomainBlocker.MISSING_APP_SEAM) }
    if (!defaultOff) { blockers.push(CanonicalMigrationDomainBlocker.CUTOVER_NOT_DEFAULT_OFF) }
    if (!noProductionInjection) { blockers.push(CanonicalMigrationDomainBlocker.PRODUCTION_INJECTION_PRESENT) }
    if (!readPathLegacy) { blockers.push(CanonicalMigrationDomainBlocker.READ_PATH_NOT_LEGACY) }
    if (!testsPresent) { blockers.push(CanonicalMigrationDomainBlocker.TESTS_MISSING) }
    this.domain = domain
    this.machinePartsPresent = machinePartsPresent
    this.appSeamPresent = appSeamPresent
    this.defaultOff = defaultOff
    this.noProductionInjection = noProductionInjection
    this.readPathLegacy = readPathLegacy
    this.testsPresent = testsPresent
    this.blockers = Array.from(new Set(blockers)).sort((a, b) => a.localeCompare(b))
    this.staticReviewRecommended = staticReviewRecommended
    this.realMigrationBlocked = realMigrationBlocked
    this.diagnosticsSummary = [
      'domain=' + domain, 'machinePartsPresent=' + machinePartsPresent,
      'appSeamPresent=' + appSeamPresent, 'defaultOff=' + defaultOff,
      'readPathLegacy=' + readPathLegacy, 'realMigrationBlocked=' + realMigrationBlocked
    ].join(',')
  }
}

export class CanonicalOtherDomainsStaticAuditReport {
  audits: CanonicalMigrationStaticDomainAudit[]

  constructor(audits: CanonicalMigrationStaticDomainAudit[]) {
    this.audits = audits
  }

  static v813Default(): CanonicalOtherDomainsStaticAuditReport {
    return new CanonicalOtherDomainsStaticAuditReport([
      new CanonicalMigrationStaticDomainAudit(
        CanonicalMigrationDomain.RECORDING_METADATA, true, true, true, true, true, true
      ),
      new CanonicalMigrationStaticDomainAudit(
        CanonicalMigrationDomain.GENERATED_ARTIFACTS, true, true, true, true, true, true
      ),
      new CanonicalMigrationStaticDomainAudit(
        CanonicalMigrationDomain.TOMBSTONE_CONFLICT, true, true, true, true, true, true
      ),
      new CanonicalMigrationStaticDomainAudit(
        CanonicalMigrationDomain.AUDIO_UPLOAD, false, true, true, true, true, true
      )
    ])
  }

  audit(domain: CanonicalMigrationDomain): CanonicalMigrationStaticDomainAudit | undefined {
    return this.audits.find(a => a.domain === domain)
  }

  get diagnosticsSummary(): string {
    return this.audits.map(a => a.diagnosticsSummary).join('|')
  }
}
