/**
 * CanonicalKernelSwitch — kernel switch mode, policy, and resolution.
 * Ported from Rokurics-Apple (CanonicalKernelSwitch.swift).
 * Controls the master kernel switch between old and canonical sync runtimes.
 */

import {
  CanonicalHash, nilIfEmpty, hashPrefix
} from './CanonicalCore'

// ── Forward declarations for runtime configuration types (not yet ported) ──

export enum CanonicalSyncRuntimeMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  CANONICAL_PLAN_NO_COMMIT = 'canonicalPlanNoCommit',
  CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK = 'canonicalPlanPrimaryWithLegacyFallback',
  BLOCKED = 'blocked'
}

export enum CanonicalApplyRuntimeMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  NO_COMMIT = 'noCommit',
  TEST_ROOT_APPLY = 'testRootApply',
  PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK = 'productionRootApplyWithLegacyFallback',
  BLOCKED = 'blocked'
}

export enum CanonicalExistenceApplyRuntimeModeFW {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  NO_COMMIT = 'noCommit',
  TEST_ROOT_APPLY = 'testRootApply',
  PRODUCTION_ROOT_APPLY = 'productionRootApply',
  BLOCKED = 'blocked'
}

export enum CanonicalAudioUploadRuntimeMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  NO_COMMIT = 'noCommit',
  CANONICAL_UPLOAD_WITH_LEGACY_FALLBACK = 'canonicalUploadWithLegacyFallback',
  BLOCKED = 'blocked'
}

export enum CanonicalReadRuntimeMode {
  DISABLED = 'disabled',
  PARALLEL_COMPARE = 'parallelCompare',
  GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK = 'guardedCanonicalReadWithLegacyFallback',
  BLOCKED = 'blocked'
}

export enum CanonicalLibraryMetadataDebugPilotMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  BLOCKED = 'blocked'
}

export enum CanonicalMigrationDomain {
  RECORDING_METADATA = 'recordingMetadata',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  LIBRARY_METADATA = 'libraryMetadata',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  AUDIO_UPLOAD = 'audioUpload',
  UI_PROJECTION = 'uiProjection'
}

export class CanonicalSyncRuntimePolicy {
  debugInternalBuild: boolean
  ownerApproved: boolean
  releaseDefaultBuild: boolean
  legacyFallbackAvailable: boolean
  diagnosticsRedacted: boolean
  runtimeSwitchEnabled: boolean
  readPathLegacy: boolean
  otherActiveMigrationDomainConflicting: boolean
  allowDocumentedModifiedAtFallback: boolean

  constructor(
    debugInternalBuild: boolean = false,
    ownerApproved: boolean = false,
    releaseDefaultBuild: boolean = true,
    legacyFallbackAvailable: boolean = true,
    diagnosticsRedacted: boolean = true,
    runtimeSwitchEnabled: boolean = false,
    readPathLegacy: boolean = true,
    otherActiveMigrationDomainConflicting: boolean = false,
    allowDocumentedModifiedAtFallback: boolean = true
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApproved = ownerApproved
    this.releaseDefaultBuild = releaseDefaultBuild
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.diagnosticsRedacted = diagnosticsRedacted
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.readPathLegacy = readPathLegacy
    this.otherActiveMigrationDomainConflicting = otherActiveMigrationDomainConflicting
    this.allowDocumentedModifiedAtFallback = allowDocumentedModifiedAtFallback
  }
}

export class CanonicalApplyRuntimePolicy {
  debugInternalBuild: boolean
  ownerApproved: boolean
  releaseDefaultBuild: boolean
  legacyFallbackAvailable: boolean
  diagnosticsRedacted: boolean
  runtimeSwitchEnabled: boolean
  readPathLegacy: boolean
  enabledDomains: CanonicalMigrationDomain[]
  allowConflictRecordAction: boolean
  allowTestRootApply: boolean

  constructor(
    debugInternalBuild: boolean = false,
    ownerApproved: boolean = false,
    releaseDefaultBuild: boolean = true,
    legacyFallbackAvailable: boolean = true,
    diagnosticsRedacted: boolean = true,
    runtimeSwitchEnabled: boolean = false,
    readPathLegacy: boolean = true,
    enabledDomains: CanonicalMigrationDomain[] = [],
    allowConflictRecordAction: boolean = true,
    allowTestRootApply: boolean = false
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApproved = ownerApproved
    this.releaseDefaultBuild = releaseDefaultBuild
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.diagnosticsRedacted = diagnosticsRedacted
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.readPathLegacy = readPathLegacy
    this.enabledDomains = enabledDomains
    this.allowConflictRecordAction = allowConflictRecordAction
    this.allowTestRootApply = allowTestRootApply
  }
}

export class CanonicalExistenceApplyRuntimePolicyFW {
  debugInternalBuild: boolean
  ownerApproved: boolean
  releaseDefaultBuild: boolean
  diagnosticsRedacted: boolean
  legacyFallbackAvailable: boolean
  rootBoundRequired: boolean
  rollbackRequired: boolean
  atomicWriteRequired: boolean
  postconditionRequired: boolean
  writeAudioAllowed: boolean
  markAudioAvailableAllowed: boolean

  constructor(
    debugInternalBuild: boolean = false,
    ownerApproved: boolean = false,
    releaseDefaultBuild: boolean = true,
    diagnosticsRedacted: boolean = true,
    legacyFallbackAvailable: boolean = true,
    rootBoundRequired: boolean = true,
    rollbackRequired: boolean = true,
    atomicWriteRequired: boolean = true,
    postconditionRequired: boolean = true,
    writeAudioAllowed: boolean = false,
    markAudioAvailableAllowed: boolean = false
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApproved = ownerApproved
    this.releaseDefaultBuild = releaseDefaultBuild
    this.diagnosticsRedacted = diagnosticsRedacted
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.rootBoundRequired = rootBoundRequired
    this.rollbackRequired = rollbackRequired
    this.atomicWriteRequired = atomicWriteRequired
    this.postconditionRequired = postconditionRequired
    this.writeAudioAllowed = writeAudioAllowed
    this.markAudioAvailableAllowed = markAudioAvailableAllowed
  }
}

export class CanonicalAudioUploadRuntimePolicy {
  debugInternalBuild: boolean
  ownerApprovedCanonicalCommit: boolean
  allowTestTransportUpload: boolean
  allowCanonicalUploadWithLegacyFallback: boolean
  legacyFallbackEnabled: boolean
  requireExistingSecureUploadRoutes: boolean
  retryDrainerRequiresExistingRetry: boolean

  constructor(
    debugInternalBuild: boolean = false,
    ownerApprovedCanonicalCommit: boolean = false,
    allowTestTransportUpload: boolean = false,
    allowCanonicalUploadWithLegacyFallback: boolean = false,
    legacyFallbackEnabled: boolean = true,
    requireExistingSecureUploadRoutes: boolean = true,
    retryDrainerRequiresExistingRetry: boolean = true
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApprovedCanonicalCommit = ownerApprovedCanonicalCommit
    this.allowTestTransportUpload = allowTestTransportUpload
    this.allowCanonicalUploadWithLegacyFallback = allowCanonicalUploadWithLegacyFallback
    this.legacyFallbackEnabled = legacyFallbackEnabled
    this.requireExistingSecureUploadRoutes = requireExistingSecureUploadRoutes
    this.retryDrainerRequiresExistingRetry = retryDrainerRequiresExistingRetry
  }
}

export class CanonicalReadRuntimePolicy {
  debugInternalBuild: boolean
  ownerApproved: boolean
  manualOwnerApproval: boolean
  releaseDefaultBuild: boolean
  legacyFallbackAvailable: boolean
  diagnosticsRedacted: boolean
  applyRuntimeEvidenceValidForNonAudio: boolean
  uploadRuntimeEvidenceValidForAudioStatus: boolean
  inventorySnapshotAvailable: boolean
  planAuthorityEvidenceValid: boolean
  existenceTruthEvidenceValid: boolean
  otherDomainsNotConflicting: boolean
  readMustNotTriggerSyncUpload: boolean
  readMustNotMutateStore: boolean

  constructor(
    debugInternalBuild: boolean = false,
    ownerApproved: boolean = false,
    manualOwnerApproval: boolean = false,
    releaseDefaultBuild: boolean = true,
    legacyFallbackAvailable: boolean = true,
    diagnosticsRedacted: boolean = true,
    applyRuntimeEvidenceValidForNonAudio: boolean = true,
    uploadRuntimeEvidenceValidForAudioStatus: boolean = true,
    inventorySnapshotAvailable: boolean = true,
    planAuthorityEvidenceValid: boolean = true,
    existenceTruthEvidenceValid: boolean = true,
    otherDomainsNotConflicting: boolean = true,
    readMustNotTriggerSyncUpload: boolean = true,
    readMustNotMutateStore: boolean = true
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApproved = ownerApproved
    this.manualOwnerApproval = manualOwnerApproval
    this.releaseDefaultBuild = releaseDefaultBuild
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.diagnosticsRedacted = diagnosticsRedacted
    this.applyRuntimeEvidenceValidForNonAudio = applyRuntimeEvidenceValidForNonAudio
    this.uploadRuntimeEvidenceValidForAudioStatus = uploadRuntimeEvidenceValidForAudioStatus
    this.inventorySnapshotAvailable = inventorySnapshotAvailable
    this.planAuthorityEvidenceValid = planAuthorityEvidenceValid
    this.existenceTruthEvidenceValid = existenceTruthEvidenceValid
    this.otherDomainsNotConflicting = otherDomainsNotConflicting
    this.readMustNotTriggerSyncUpload = readMustNotTriggerSyncUpload
    this.readMustNotMutateStore = readMustNotMutateStore
  }
}

export class CanonicalInventoryRuntimeConfiguration {
  redactedDiagnostics: boolean

  constructor(redactedDiagnostics: boolean = true) {
    this.redactedDiagnostics = redactedDiagnostics
  }

  static readonly ENABLED = new CanonicalInventoryRuntimeConfiguration(true)
}

export class CanonicalSyncRuntimeConfiguration {
  mode: CanonicalSyncRuntimeMode
  policy: CanonicalSyncRuntimePolicy

  constructor(
    mode: CanonicalSyncRuntimeMode = CanonicalSyncRuntimeMode.DISABLED,
    policy: CanonicalSyncRuntimePolicy = new CanonicalSyncRuntimePolicy()
  ) {
    this.mode = mode
    this.policy = policy
  }

  static readonly DISABLED = new CanonicalSyncRuntimeConfiguration()
}

export class CanonicalApplyRuntimeConfiguration {
  mode: CanonicalApplyRuntimeMode
  policy: CanonicalApplyRuntimePolicy

  constructor(
    mode: CanonicalApplyRuntimeMode = CanonicalApplyRuntimeMode.DISABLED,
    policy: CanonicalApplyRuntimePolicy = new CanonicalApplyRuntimePolicy()
  ) {
    this.mode = mode
    this.policy = policy
  }

  static readonly DISABLED = new CanonicalApplyRuntimeConfiguration()
}

export class CanonicalExistenceApplyRuntimeConfiguration {
  mode: CanonicalExistenceApplyRuntimeModeFW
  policy: CanonicalExistenceApplyRuntimePolicyFW

  constructor(
    mode: CanonicalExistenceApplyRuntimeModeFW = CanonicalExistenceApplyRuntimeModeFW.DISABLED,
    policy: CanonicalExistenceApplyRuntimePolicyFW = new CanonicalExistenceApplyRuntimePolicyFW()
  ) {
    this.mode = mode
    this.policy = policy
  }

  static readonly DISABLED = new CanonicalExistenceApplyRuntimeConfiguration()
}

export class CanonicalAudioUploadRuntimeConfiguration {
  mode: CanonicalAudioUploadRuntimeMode
  policy: CanonicalAudioUploadRuntimePolicy

  constructor(
    mode: CanonicalAudioUploadRuntimeMode = CanonicalAudioUploadRuntimeMode.DISABLED,
    policy: CanonicalAudioUploadRuntimePolicy = new CanonicalAudioUploadRuntimePolicy()
  ) {
    this.mode = mode
    this.policy = policy
  }

  static readonly DISABLED = new CanonicalAudioUploadRuntimeConfiguration()
}

export class CanonicalReadRuntimeConfiguration {
  mode: CanonicalReadRuntimeMode
  policy: CanonicalReadRuntimePolicy

  constructor(
    mode: CanonicalReadRuntimeMode = CanonicalReadRuntimeMode.DISABLED,
    policy: CanonicalReadRuntimePolicy = new CanonicalReadRuntimePolicy()
  ) {
    this.mode = mode
    this.policy = policy
  }

  static readonly DISABLED = new CanonicalReadRuntimeConfiguration()
}

export class CanonicalLibraryMetadataDebugPilotConfiguration {
  mode: CanonicalLibraryMetadataDebugPilotMode

  constructor(mode: CanonicalLibraryMetadataDebugPilotMode = CanonicalLibraryMetadataDebugPilotMode.DISABLED) {
    this.mode = mode
  }

  static readonly DISABLED = new CanonicalLibraryMetadataDebugPilotConfiguration()
  static diagnosticsOnly(): CanonicalLibraryMetadataDebugPilotConfiguration {
    return new CanonicalLibraryMetadataDebugPilotConfiguration(CanonicalLibraryMetadataDebugPilotMode.DIAGNOSTICS_ONLY)
  }
}

// ── Canonical Kernel Switch Mode ──

export enum CanonicalKernelSwitchMode {
  OLD_KERNEL = 'oldKernel',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  CANONICAL_SHADOW = 'canonicalShadow',
  CANONICAL_DECISION_ONLY = 'canonicalDecisionOnly',
  CANONICAL_APPLY_NO_AUDIO = 'canonicalApplyNoAudio',
  CANONICAL_FULL_SYNC = 'canonicalFullSync',
  BLOCKED = 'blocked'
}

export function switchModeDisplayTitle(mode: CanonicalKernelSwitchMode): string {
  switch (mode) {
    case CanonicalKernelSwitchMode.OLD_KERNEL:
      return '旧内核'
    case CanonicalKernelSwitchMode.DIAGNOSTICS_ONLY:
      return '诊断'
    case CanonicalKernelSwitchMode.CANONICAL_SHADOW:
      return '新内核影子'
    case CanonicalKernelSwitchMode.CANONICAL_DECISION_ONLY:
      return '新内核决策'
    case CanonicalKernelSwitchMode.CANONICAL_APPLY_NO_AUDIO:
      return '新内核写入不含音频'
    case CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC:
      return '新内核完整同步'
    case CanonicalKernelSwitchMode.BLOCKED:
      return '已阻断'
  }
}

// ── Canonical Kernel Switch Owner State ──

export enum CanonicalKernelSwitchOwnerState {
  OLD_KERNEL = 'oldKernel',
  SHADOW = 'shadow',
  CANONICAL_NO_WRITE = 'canonicalNoWrite',
  CANONICAL_READ_WRITE = 'canonicalReadWrite',
  BLOCKED = 'blocked'
}

// ── Canonical Kernel Switch Mode Choice ──

export class CanonicalKernelSwitchModeChoice {
  rawValue: string
  title: string

  constructor(mode: CanonicalKernelSwitchMode) {
    this.rawValue = mode as string
    this.title = switchModeDisplayTitle(mode)
  }

  get id(): string {
    return this.rawValue
  }
}

// ── Canonical Kernel Switch Policy ──

export class CanonicalKernelSwitchPolicy {
  debugInternalBuild: boolean
  ownerApproved: boolean
  releaseDefaultBuild: boolean
  manualFullSyncConfirmation: boolean
  legacyFallbackAvailable: boolean
  diagnosticsRedacted: boolean
  shadowComparisonEnabled: boolean
  legacyReadPathAvailable: boolean
  legacyWritePathAvailable: boolean
  canonicalWritesLegacyReadable: boolean
  noDataFormatMigrationRequired: boolean
  canonicalOnlyRequiredFieldsHaveLegacyFallback: boolean
  physicalMoveDeleteDisabled: boolean
  secretPathHashLeakRedactionEnabled: boolean
  shadowCompareAllowedDuringCanonicalOwner: boolean

  constructor(
    debugInternalBuild: boolean = false,
    ownerApproved: boolean = false,
    releaseDefaultBuild: boolean = true,
    manualFullSyncConfirmation: boolean = false,
    legacyFallbackAvailable: boolean = true,
    diagnosticsRedacted: boolean = true,
    shadowComparisonEnabled: boolean = true,
    legacyReadPathAvailable: boolean = true,
    legacyWritePathAvailable: boolean = true,
    canonicalWritesLegacyReadable: boolean = true,
    noDataFormatMigrationRequired: boolean = true,
    canonicalOnlyRequiredFieldsHaveLegacyFallback: boolean = true,
    physicalMoveDeleteDisabled: boolean = true,
    secretPathHashLeakRedactionEnabled: boolean = true,
    shadowCompareAllowedDuringCanonicalOwner: boolean = true
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApproved = ownerApproved
    this.releaseDefaultBuild = releaseDefaultBuild
    this.manualFullSyncConfirmation = manualFullSyncConfirmation
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.diagnosticsRedacted = diagnosticsRedacted
    this.shadowComparisonEnabled = shadowComparisonEnabled
    this.legacyReadPathAvailable = legacyReadPathAvailable
    this.legacyWritePathAvailable = legacyWritePathAvailable
    this.canonicalWritesLegacyReadable = canonicalWritesLegacyReadable
    this.noDataFormatMigrationRequired = noDataFormatMigrationRequired
    this.canonicalOnlyRequiredFieldsHaveLegacyFallback = canonicalOnlyRequiredFieldsHaveLegacyFallback
    this.physicalMoveDeleteDisabled = physicalMoveDeleteDisabled
    this.secretPathHashLeakRedactionEnabled = secretPathHashLeakRedactionEnabled
    this.shadowCompareAllowedDuringCanonicalOwner = shadowCompareAllowedDuringCanonicalOwner
  }

  static readonly RELEASE_DEFAULT = new CanonicalKernelSwitchPolicy()

  static debugInternal(
    ownerApproved: boolean = true,
    manualFullSyncConfirmation: boolean = false
  ): CanonicalKernelSwitchPolicy {
    return new CanonicalKernelSwitchPolicy(
      true, ownerApproved, false, manualFullSyncConfirmation
    )
  }
}

// ── Canonical Kernel Switch Advanced Overrides ──

export class CanonicalKernelSwitchAdvancedOverrides {
  syncRuntimeConfiguration: CanonicalSyncRuntimeConfiguration | null
  applyRuntimeConfiguration: CanonicalApplyRuntimeConfiguration | null
  existenceApplyRuntimeConfiguration: CanonicalExistenceApplyRuntimeConfiguration | null
  audioUploadRuntimeConfiguration: CanonicalAudioUploadRuntimeConfiguration | null
  readRuntimeConfiguration: CanonicalReadRuntimeConfiguration | null
  libraryMetadataDebugPilotConfiguration: CanonicalLibraryMetadataDebugPilotConfiguration | null

  constructor(
    syncRuntimeConfiguration: CanonicalSyncRuntimeConfiguration | null = null,
    applyRuntimeConfiguration: CanonicalApplyRuntimeConfiguration | null = null,
    existenceApplyRuntimeConfiguration: CanonicalExistenceApplyRuntimeConfiguration | null = null,
    audioUploadRuntimeConfiguration: CanonicalAudioUploadRuntimeConfiguration | null = null,
    readRuntimeConfiguration: CanonicalReadRuntimeConfiguration | null = null,
    libraryMetadataDebugPilotConfiguration: CanonicalLibraryMetadataDebugPilotConfiguration | null = null
  ) {
    this.syncRuntimeConfiguration = syncRuntimeConfiguration ?? null
    this.applyRuntimeConfiguration = applyRuntimeConfiguration ?? null
    this.existenceApplyRuntimeConfiguration = existenceApplyRuntimeConfiguration ?? null
    this.audioUploadRuntimeConfiguration = audioUploadRuntimeConfiguration ?? null
    this.readRuntimeConfiguration = readRuntimeConfiguration ?? null
    this.libraryMetadataDebugPilotConfiguration = libraryMetadataDebugPilotConfiguration ?? null
  }

  static readonly NONE = new CanonicalKernelSwitchAdvancedOverrides()
}

// ── Canonical Kernel Switch Blocker ──

export enum CanonicalKernelSwitchBlocker {
  EXPLICIT_BLOCKED_MODE = 'explicitBlockedMode',
  RELEASE_DEFAULT_CANNOT_USE_CANONICAL_FULL_SYNC = 'releaseDefaultCannotUseCanonicalFullSync',
  CANONICAL_FULL_SYNC_REQUIRES_DEBUG_INTERNAL_BUILD = 'canonicalFullSyncRequiresDebugInternalBuild',
  CANONICAL_FULL_SYNC_REQUIRES_OWNER_APPROVAL = 'canonicalFullSyncRequiresOwnerApproval',
  CANONICAL_FULL_SYNC_REQUIRES_MANUAL_CONFIRMATION = 'canonicalFullSyncRequiresManualConfirmation',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  DIAGNOSTICS_NOT_REDACTED = 'diagnosticsNotRedacted',
  LEGACY_READ_PATH_UNAVAILABLE = 'legacyReadPathUnavailable',
  LEGACY_WRITE_PATH_UNAVAILABLE = 'legacyWritePathUnavailable',
  CANONICAL_WRITES_NOT_LEGACY_READABLE = 'canonicalWritesNotLegacyReadable',
  SWITCH_BACK_WOULD_REQUIRE_DATA_FORMAT_MIGRATION = 'switchBackWouldRequireDataFormatMigration',
  CANONICAL_ONLY_REQUIRED_FIELD_WITHOUT_LEGACY_FALLBACK = 'canonicalOnlyRequiredFieldWithoutLegacyFallback',
  PHYSICAL_MOVE_OR_DELETE_WOULD_BE_REQUIRED = 'physicalMoveOrDeleteWouldBeRequired',
  SECRET_PATH_HASH_LEAK_RISK = 'secretPathHashLeakRisk',
  SHADOW_COMPARE_CANNOT_STAY_ENABLED_WITH_CANONICAL_OWNER = 'shadowCompareCannotStayEnabledWithCanonicalOwner',
  ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH = 'advancedOverrideContradictsMasterSwitch'
}

// ── Canonical Kernel Switch Reversibility Proof ──

export class CanonicalKernelSwitchReversibilityProof {
  legacyReadPathStillExists: boolean
  legacyWritePathStillExists: boolean
  canonicalWritesAreLegacyReadable: boolean
  noDataFormatMigrationRequiredToSwitchBack: boolean
  noCanonicalOnlyRequiredFieldWithoutLegacyFallback: boolean
  noPhysicalMoveOrDeleteRequired: boolean
  secretPathHashLeakRedactionEnabled: boolean
  shadowCompareCanStayOnWhileCanonicalOwnerActive: boolean
  requiresDataMigrationToSwitchBack: boolean
  blockers: CanonicalKernelSwitchBlocker[]

  constructor(
    legacyReadPathStillExists: boolean,
    legacyWritePathStillExists: boolean,
    canonicalWritesAreLegacyReadable: boolean,
    noDataFormatMigrationRequiredToSwitchBack: boolean,
    noCanonicalOnlyRequiredFieldWithoutLegacyFallback: boolean,
    noPhysicalMoveOrDeleteRequired: boolean,
    secretPathHashLeakRedactionEnabled: boolean,
    shadowCompareCanStayOnWhileCanonicalOwnerActive: boolean,
    requiresDataMigrationToSwitchBack: boolean,
    blockers: CanonicalKernelSwitchBlocker[]
  ) {
    this.legacyReadPathStillExists = legacyReadPathStillExists
    this.legacyWritePathStillExists = legacyWritePathStillExists
    this.canonicalWritesAreLegacyReadable = canonicalWritesAreLegacyReadable
    this.noDataFormatMigrationRequiredToSwitchBack = noDataFormatMigrationRequiredToSwitchBack
    this.noCanonicalOnlyRequiredFieldWithoutLegacyFallback = noCanonicalOnlyRequiredFieldWithoutLegacyFallback
    this.noPhysicalMoveOrDeleteRequired = noPhysicalMoveOrDeleteRequired
    this.secretPathHashLeakRedactionEnabled = secretPathHashLeakRedactionEnabled
    this.shadowCompareCanStayOnWhileCanonicalOwnerActive = shadowCompareCanStayOnWhileCanonicalOwnerActive
    this.requiresDataMigrationToSwitchBack = requiresDataMigrationToSwitchBack
    this.blockers = blockers
  }

  get isReversible(): boolean {
    return this.blockers.length === 0 && !this.requiresDataMigrationToSwitchBack
  }
}

// ── Canonical Kernel Switch Reversibility Gate ──

export class CanonicalKernelSwitchReversibilityGate {
  constructor() {}

  prove(policy: CanonicalKernelSwitchPolicy): CanonicalKernelSwitchReversibilityProof {
    const blockers: CanonicalKernelSwitchBlocker[] = []
    if (!policy.legacyReadPathAvailable) {
      blockers.push(CanonicalKernelSwitchBlocker.LEGACY_READ_PATH_UNAVAILABLE)
    }
    if (!policy.legacyWritePathAvailable) {
      blockers.push(CanonicalKernelSwitchBlocker.LEGACY_WRITE_PATH_UNAVAILABLE)
    }
    if (!policy.canonicalWritesLegacyReadable) {
      blockers.push(CanonicalKernelSwitchBlocker.CANONICAL_WRITES_NOT_LEGACY_READABLE)
    }
    if (!policy.noDataFormatMigrationRequired) {
      blockers.push(CanonicalKernelSwitchBlocker.SWITCH_BACK_WOULD_REQUIRE_DATA_FORMAT_MIGRATION)
    }
    if (!policy.canonicalOnlyRequiredFieldsHaveLegacyFallback) {
      blockers.push(CanonicalKernelSwitchBlocker.CANONICAL_ONLY_REQUIRED_FIELD_WITHOUT_LEGACY_FALLBACK)
    }
    if (!policy.physicalMoveDeleteDisabled) {
      blockers.push(CanonicalKernelSwitchBlocker.PHYSICAL_MOVE_OR_DELETE_WOULD_BE_REQUIRED)
    }
    if (!policy.secretPathHashLeakRedactionEnabled) {
      blockers.push(CanonicalKernelSwitchBlocker.SECRET_PATH_HASH_LEAK_RISK)
    }
    if (!policy.shadowCompareAllowedDuringCanonicalOwner) {
      blockers.push(CanonicalKernelSwitchBlocker.SHADOW_COMPARE_CANNOT_STAY_ENABLED_WITH_CANONICAL_OWNER)
    }

    return new CanonicalKernelSwitchReversibilityProof(
      policy.legacyReadPathAvailable,
      policy.legacyWritePathAvailable,
      policy.canonicalWritesLegacyReadable,
      policy.noDataFormatMigrationRequired,
      policy.canonicalOnlyRequiredFieldsHaveLegacyFallback,
      policy.physicalMoveDeleteDisabled,
      policy.secretPathHashLeakRedactionEnabled,
      policy.shadowCompareAllowedDuringCanonicalOwner,
      !policy.noDataFormatMigrationRequired,
      blockers
    )
  }
}

// ── Canonical Kernel Switch Migration Matrix Policy ──

export class CanonicalKernelSwitchMigrationMatrixPolicy {
  mode: CanonicalKernelSwitchMode
  ownerState: CanonicalKernelSwitchOwnerState
  activeCanonicalOwnershipDomains: CanonicalMigrationDomain[]
  legacyReadPathRetained: boolean
  legacyWritePathRetained: boolean
  migrationRequiredToSwitchBack: boolean
  diskFormatPolicy: string
  diagnosticsRedacted: boolean

  constructor(
    mode: CanonicalKernelSwitchMode,
    ownerState: CanonicalKernelSwitchOwnerState,
    activeCanonicalOwnershipDomains: CanonicalMigrationDomain[],
    legacyReadPathRetained: boolean,
    legacyWritePathRetained: boolean,
    migrationRequiredToSwitchBack: boolean,
    diskFormatPolicy: string,
    diagnosticsRedacted: boolean
  ) {
    this.mode = mode
    this.ownerState = ownerState
    this.activeCanonicalOwnershipDomains = [...activeCanonicalOwnershipDomains].sort()
    this.legacyReadPathRetained = legacyReadPathRetained
    this.legacyWritePathRetained = legacyWritePathRetained
    this.migrationRequiredToSwitchBack = migrationRequiredToSwitchBack
    this.diskFormatPolicy = diskFormatPolicy
    this.diagnosticsRedacted = diagnosticsRedacted
  }

  static make(
    mode: CanonicalKernelSwitchMode,
    ownerState: CanonicalKernelSwitchOwnerState,
    activeCanonicalOwnershipDomains: CanonicalMigrationDomain[],
    policy: CanonicalKernelSwitchPolicy,
    proof: CanonicalKernelSwitchReversibilityProof
  ): CanonicalKernelSwitchMigrationMatrixPolicy {
    return new CanonicalKernelSwitchMigrationMatrixPolicy(
      mode,
      ownerState,
      activeCanonicalOwnershipDomains,
      policy.legacyReadPathAvailable,
      policy.legacyWritePathAvailable,
      proof.requiresDataMigrationToSwitchBack,
      'legacy-readable-or-dual-write-compatible',
      policy.diagnosticsRedacted
    )
  }
}

// ── Canonical Kernel Switch Effective Configuration ──

export class CanonicalKernelSwitchEffectiveConfiguration {
  inventoryRuntimeConfiguration: CanonicalInventoryRuntimeConfiguration
  syncRuntimeConfiguration: CanonicalSyncRuntimeConfiguration
  applyRuntimeConfiguration: CanonicalApplyRuntimeConfiguration
  existenceApplyRuntimeConfiguration: CanonicalExistenceApplyRuntimeConfiguration
  audioUploadRuntimeConfiguration: CanonicalAudioUploadRuntimeConfiguration
  readRuntimeConfiguration: CanonicalReadRuntimeConfiguration
  libraryMetadataDebugPilotConfiguration: CanonicalLibraryMetadataDebugPilotConfiguration
  migrationMatrixPolicy: CanonicalKernelSwitchMigrationMatrixPolicy

  constructor(
    inventoryRuntimeConfiguration: CanonicalInventoryRuntimeConfiguration,
    syncRuntimeConfiguration: CanonicalSyncRuntimeConfiguration,
    applyRuntimeConfiguration: CanonicalApplyRuntimeConfiguration,
    existenceApplyRuntimeConfiguration: CanonicalExistenceApplyRuntimeConfiguration,
    audioUploadRuntimeConfiguration: CanonicalAudioUploadRuntimeConfiguration,
    readRuntimeConfiguration: CanonicalReadRuntimeConfiguration,
    libraryMetadataDebugPilotConfiguration: CanonicalLibraryMetadataDebugPilotConfiguration,
    migrationMatrixPolicy: CanonicalKernelSwitchMigrationMatrixPolicy
  ) {
    this.inventoryRuntimeConfiguration = inventoryRuntimeConfiguration
    this.syncRuntimeConfiguration = syncRuntimeConfiguration
    this.applyRuntimeConfiguration = applyRuntimeConfiguration
    this.existenceApplyRuntimeConfiguration = existenceApplyRuntimeConfiguration
    this.audioUploadRuntimeConfiguration = audioUploadRuntimeConfiguration
    this.readRuntimeConfiguration = readRuntimeConfiguration
    this.libraryMetadataDebugPilotConfiguration = libraryMetadataDebugPilotConfiguration
    this.migrationMatrixPolicy = migrationMatrixPolicy
  }

  static blocked(
    policy: CanonicalKernelSwitchPolicy,
    proof: CanonicalKernelSwitchReversibilityProof
  ): CanonicalKernelSwitchEffectiveConfiguration {
    return new CanonicalKernelSwitchEffectiveConfiguration(
      new CanonicalInventoryRuntimeConfiguration(policy.diagnosticsRedacted),
      new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.BLOCKED),
      new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.BLOCKED),
      new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeModeFW.BLOCKED),
      new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.BLOCKED),
      new CanonicalReadRuntimeConfiguration(CanonicalReadRuntimeMode.BLOCKED),
      new CanonicalLibraryMetadataDebugPilotConfiguration(CanonicalLibraryMetadataDebugPilotMode.BLOCKED),
      CanonicalKernelSwitchMigrationMatrixPolicy.make(
        CanonicalKernelSwitchMode.BLOCKED,
        CanonicalKernelSwitchOwnerState.BLOCKED,
        [],
        policy,
        proof
      )
    )
  }
}

// ── Canonical Kernel Switch Result ──

export class CanonicalKernelSwitchResult {
  requestedMode: CanonicalKernelSwitchMode
  effectiveMode: CanonicalKernelSwitchMode
  ownerState: CanonicalKernelSwitchOwnerState
  blockers: CanonicalKernelSwitchBlocker[]
  effectiveConfiguration: CanonicalKernelSwitchEffectiveConfiguration
  reversibilityProof: CanonicalKernelSwitchReversibilityProof
  diagnosticsSummary: string
  redacted: boolean

  constructor(
    requestedMode: CanonicalKernelSwitchMode,
    effectiveMode: CanonicalKernelSwitchMode,
    ownerState: CanonicalKernelSwitchOwnerState,
    blockers: CanonicalKernelSwitchBlocker[],
    effectiveConfiguration: CanonicalKernelSwitchEffectiveConfiguration,
    reversibilityProof: CanonicalKernelSwitchReversibilityProof,
    diagnosticsSummary: string,
    redacted: boolean
  ) {
    this.requestedMode = requestedMode
    this.effectiveMode = effectiveMode
    this.ownerState = ownerState
    this.blockers = blockers
    this.effectiveConfiguration = effectiveConfiguration
    this.reversibilityProof = reversibilityProof
    this.diagnosticsSummary = diagnosticsSummary
    this.redacted = redacted
  }

  get isBlocked(): boolean {
    return this.effectiveMode === CanonicalKernelSwitchMode.BLOCKED || this.blockers.length > 0
  }
}

// ── Canonical Kernel Switch Configuration ──

export class CanonicalKernelSwitchConfiguration {
  static readonly DEBUG_MODE_KEY = 'Rokurics.debug.canonicalKernelSwitch.mode'
  static readonly DEBUG_FULL_SYNC_CONFIRMED_KEY = 'Rokurics.debug.canonicalKernelSwitch.fullSyncConfirmed'
  static readonly DIAGNOSTICS_PATH_TEXT = 'Application Support/Rokurics/Diagnostics/canonical-kernel-switch.log'

  mode: CanonicalKernelSwitchMode
  policy: CanonicalKernelSwitchPolicy
  advancedOverrides: CanonicalKernelSwitchAdvancedOverrides

  constructor(
    mode: CanonicalKernelSwitchMode = CanonicalKernelSwitchMode.OLD_KERNEL,
    policy: CanonicalKernelSwitchPolicy = CanonicalKernelSwitchPolicy.RELEASE_DEFAULT,
    advancedOverrides: CanonicalKernelSwitchAdvancedOverrides = CanonicalKernelSwitchAdvancedOverrides.NONE
  ) {
    this.mode = mode
    this.policy = policy
    this.advancedOverrides = advancedOverrides
  }

  static readonly DEFAULT = new CanonicalKernelSwitchConfiguration()
  static readonly OLD_KERNEL = new CanonicalKernelSwitchConfiguration()

  static get debugModeChoices(): CanonicalKernelSwitchModeChoice[] {
    return [
      CanonicalKernelSwitchMode.OLD_KERNEL,
      CanonicalKernelSwitchMode.DIAGNOSTICS_ONLY,
      CanonicalKernelSwitchMode.CANONICAL_SHADOW,
      CanonicalKernelSwitchMode.CANONICAL_DECISION_ONLY,
      CanonicalKernelSwitchMode.CANONICAL_APPLY_NO_AUDIO,
      CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC
    ].map((m: CanonicalKernelSwitchMode) => new CanonicalKernelSwitchModeChoice(m))
  }

  static normalizedDebugMode(rawValue: string): string {
    const valid = Object.values(CanonicalKernelSwitchMode).indexOf(rawValue as CanonicalKernelSwitchMode) >= 0
    return valid ? rawValue : CanonicalKernelSwitchMode.OLD_KERNEL as string
  }

  resolve(
    reversibilityGate: CanonicalKernelSwitchReversibilityGate = new CanonicalKernelSwitchReversibilityGate()
  ): CanonicalKernelSwitchResult {
    const proof = reversibilityGate.prove(this.policy)
    const blockers: CanonicalKernelSwitchBlocker[] = [...proof.blockers]

    if (this.mode === CanonicalKernelSwitchMode.BLOCKED) {
      blockers.push(CanonicalKernelSwitchBlocker.EXPLICIT_BLOCKED_MODE)
    }
    if (!this.policy.legacyFallbackAvailable) {
      blockers.push(CanonicalKernelSwitchBlocker.LEGACY_FALLBACK_UNAVAILABLE)
    }
    if (!this.policy.diagnosticsRedacted) {
      blockers.push(CanonicalKernelSwitchBlocker.DIAGNOSTICS_NOT_REDACTED)
    }
    if (this.mode === CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC) {
      if (this.policy.releaseDefaultBuild) {
        blockers.push(CanonicalKernelSwitchBlocker.RELEASE_DEFAULT_CANNOT_USE_CANONICAL_FULL_SYNC)
      }
      if (!this.policy.debugInternalBuild) {
        blockers.push(CanonicalKernelSwitchBlocker.CANONICAL_FULL_SYNC_REQUIRES_DEBUG_INTERNAL_BUILD)
      }
      if (!this.policy.ownerApproved) {
        blockers.push(CanonicalKernelSwitchBlocker.CANONICAL_FULL_SYNC_REQUIRES_OWNER_APPROVAL)
      }
      if (!this.policy.manualFullSyncConfirmation) {
        blockers.push(CanonicalKernelSwitchBlocker.CANONICAL_FULL_SYNC_REQUIRES_MANUAL_CONFIRMATION)
      }
    }
    if ((this.mode === CanonicalKernelSwitchMode.CANONICAL_SHADOW ||
      this.mode === CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC) &&
      !this.policy.shadowComparisonEnabled) {
      blockers.push(CanonicalKernelSwitchBlocker.SHADOW_COMPARE_CANNOT_STAY_ENABLED_WITH_CANONICAL_OWNER)
    }

    const baseEffective = this.makeEffectiveConfiguration(this.mode, proof, false)
    for (const b of this.advancedOverrideBlockers(baseEffective)) {
      blockers.push(b)
    }
    const effectiveConfiguration = this.makeEffectiveConfiguration(this.mode, proof, true)

    const uniqueBlockers = CanonicalKernelSwitchConfiguration.unique(blockers)
    if (uniqueBlockers.length > 0) {
      const blockedConfiguration = CanonicalKernelSwitchEffectiveConfiguration.blocked(this.policy, proof)
      return new CanonicalKernelSwitchResult(
        this.mode,
        CanonicalKernelSwitchMode.BLOCKED,
        CanonicalKernelSwitchOwnerState.BLOCKED,
        uniqueBlockers,
        blockedConfiguration,
        proof,
        CanonicalKernelSwitchConfiguration.diagnosticsSummary(
          this.mode, CanonicalKernelSwitchMode.BLOCKED,
          CanonicalKernelSwitchOwnerState.BLOCKED,
          blockedConfiguration, uniqueBlockers
        ),
        this.policy.diagnosticsRedacted
      )
    }

    return new CanonicalKernelSwitchResult(
      this.mode,
      this.mode,
      this.ownerStateFor(this.mode),
      [],
      effectiveConfiguration,
      proof,
      CanonicalKernelSwitchConfiguration.diagnosticsSummary(
        this.mode, this.mode,
        this.ownerStateFor(this.mode),
        effectiveConfiguration, []
      ),
      this.policy.diagnosticsRedacted
    )
  }

  private makeEffectiveConfiguration(
    mode: CanonicalKernelSwitchMode,
    proof: CanonicalKernelSwitchReversibilityProof,
    applyingOverrides: boolean
  ): CanonicalKernelSwitchEffectiveConfiguration {
    const inventory = new CanonicalInventoryRuntimeConfiguration(this.policy.diagnosticsRedacted)
    const syncPolicy = this.canonicalSyncPolicy()
    const applyPolicy = this.canonicalApplyPolicy()
    const existencePolicy = this.canonicalExistencePolicy()
    const audioPolicy = this.canonicalAudioPolicy()
    const readPolicy = this.canonicalReadPolicy()

    let sync: CanonicalSyncRuntimeConfiguration
    let apply: CanonicalApplyRuntimeConfiguration
    let existence: CanonicalExistenceApplyRuntimeConfiguration
    let audio: CanonicalAudioUploadRuntimeConfiguration
    let read: CanonicalReadRuntimeConfiguration
    let libraryPilot: CanonicalLibraryMetadataDebugPilotConfiguration
    let activeDomains: CanonicalMigrationDomain[]

    switch (mode) {
      case CanonicalKernelSwitchMode.OLD_KERNEL:
        sync = CanonicalSyncRuntimeConfiguration.DISABLED
        apply = CanonicalApplyRuntimeConfiguration.DISABLED
        existence = CanonicalExistenceApplyRuntimeConfiguration.DISABLED
        audio = CanonicalAudioUploadRuntimeConfiguration.DISABLED
        read = CanonicalReadRuntimeConfiguration.DISABLED
        libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DISABLED
        activeDomains = []
        break
      case CanonicalKernelSwitchMode.DIAGNOSTICS_ONLY:
        sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.DIAGNOSTICS_ONLY, syncPolicy)
        apply = new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.DIAGNOSTICS_ONLY, applyPolicy)
        existence = new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeModeFW.DIAGNOSTICS_ONLY, existencePolicy)
        audio = new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.DIAGNOSTICS_ONLY, audioPolicy)
        read = CanonicalReadRuntimeConfiguration.DISABLED
        libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.diagnosticsOnly()
        activeDomains = []
        break
      case CanonicalKernelSwitchMode.CANONICAL_SHADOW:
        sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.CANONICAL_PLAN_NO_COMMIT, syncPolicy)
        apply = new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.NO_COMMIT, applyPolicy)
        existence = new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeModeFW.NO_COMMIT, existencePolicy)
        audio = new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.NO_COMMIT, audioPolicy)
        read = new CanonicalReadRuntimeConfiguration(
          this.policy.shadowComparisonEnabled ? CanonicalReadRuntimeMode.PARALLEL_COMPARE : CanonicalReadRuntimeMode.DISABLED,
          readPolicy
        )
        libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DISABLED
        activeDomains = []
        break
      case CanonicalKernelSwitchMode.CANONICAL_DECISION_ONLY:
        sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK, syncPolicy)
        apply = CanonicalApplyRuntimeConfiguration.DISABLED
        existence = CanonicalExistenceApplyRuntimeConfiguration.DISABLED
        audio = CanonicalAudioUploadRuntimeConfiguration.DISABLED
        read = CanonicalReadRuntimeConfiguration.DISABLED
        libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DISABLED
        activeDomains = []
        break
      case CanonicalKernelSwitchMode.CANONICAL_APPLY_NO_AUDIO:
        sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK, syncPolicy)
        apply = new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK, applyPolicy)
        existence = new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeModeFW.PRODUCTION_ROOT_APPLY, existencePolicy)
        audio = CanonicalAudioUploadRuntimeConfiguration.DISABLED
        read = CanonicalReadRuntimeConfiguration.DISABLED
        libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DISABLED
        activeDomains = [
          CanonicalMigrationDomain.RECORDING_METADATA,
          CanonicalMigrationDomain.GENERATED_ARTIFACTS,
          CanonicalMigrationDomain.LIBRARY_METADATA,
          CanonicalMigrationDomain.TOMBSTONE_CONFLICT
        ]
        break
      case CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC:
        sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK, syncPolicy)
        apply = new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK, applyPolicy)
        existence = new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeModeFW.PRODUCTION_ROOT_APPLY, existencePolicy)
        audio = new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.CANONICAL_UPLOAD_WITH_LEGACY_FALLBACK, audioPolicy)
        read = new CanonicalReadRuntimeConfiguration(CanonicalReadRuntimeMode.GUARDED_CANONICAL_READ_WITH_LEGACY_FALLBACK, readPolicy)
        libraryPilot = CanonicalLibraryMetadataDebugPilotConfiguration.DISABLED
        activeDomains = [
          CanonicalMigrationDomain.RECORDING_METADATA,
          CanonicalMigrationDomain.GENERATED_ARTIFACTS,
          CanonicalMigrationDomain.LIBRARY_METADATA,
          CanonicalMigrationDomain.TOMBSTONE_CONFLICT,
          CanonicalMigrationDomain.AUDIO_UPLOAD,
          CanonicalMigrationDomain.UI_PROJECTION
        ]
        break
      case CanonicalKernelSwitchMode.BLOCKED:
      default:
        sync = new CanonicalSyncRuntimeConfiguration(CanonicalSyncRuntimeMode.BLOCKED)
        apply = new CanonicalApplyRuntimeConfiguration(CanonicalApplyRuntimeMode.BLOCKED)
        existence = new CanonicalExistenceApplyRuntimeConfiguration(CanonicalExistenceApplyRuntimeModeFW.BLOCKED)
        audio = new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.BLOCKED)
        read = new CanonicalReadRuntimeConfiguration(CanonicalReadRuntimeMode.BLOCKED)
        libraryPilot = new CanonicalLibraryMetadataDebugPilotConfiguration(CanonicalLibraryMetadataDebugPilotMode.BLOCKED)
        activeDomains = []
        break
    }

    return new CanonicalKernelSwitchEffectiveConfiguration(
      inventory,
      applyingOverrides ? (this.advancedOverrides.syncRuntimeConfiguration ?? sync) : sync,
      applyingOverrides ? (this.advancedOverrides.applyRuntimeConfiguration ?? apply) : apply,
      applyingOverrides ? (this.advancedOverrides.existenceApplyRuntimeConfiguration ?? existence) : existence,
      applyingOverrides ? (this.advancedOverrides.audioUploadRuntimeConfiguration ?? audio) : audio,
      applyingOverrides ? (this.advancedOverrides.readRuntimeConfiguration ?? read) : read,
      applyingOverrides ? (this.advancedOverrides.libraryMetadataDebugPilotConfiguration ?? libraryPilot) : libraryPilot,
      CanonicalKernelSwitchMigrationMatrixPolicy.make(
        mode,
        this.ownerStateFor(mode),
        activeDomains,
        this.policy,
        proof
      )
    )
  }

  private canonicalSyncPolicy(): CanonicalSyncRuntimePolicy {
    return new CanonicalSyncRuntimePolicy(
      this.policy.debugInternalBuild,
      this.policy.ownerApproved,
      this.policy.releaseDefaultBuild,
      this.policy.legacyFallbackAvailable,
      this.policy.diagnosticsRedacted,
      false,
      true,
      false,
      true
    )
  }

  private canonicalApplyPolicy(): CanonicalApplyRuntimePolicy {
    return new CanonicalApplyRuntimePolicy(
      this.policy.debugInternalBuild,
      this.policy.ownerApproved,
      this.policy.releaseDefaultBuild,
      this.policy.legacyFallbackAvailable,
      this.policy.diagnosticsRedacted,
      false,
      true,
      [
        CanonicalMigrationDomain.RECORDING_METADATA,
        CanonicalMigrationDomain.LIBRARY_METADATA,
        CanonicalMigrationDomain.GENERATED_ARTIFACTS,
        CanonicalMigrationDomain.TOMBSTONE_CONFLICT,
        CanonicalMigrationDomain.RECORDING_METADATA
      ].filter((v, i, a) => a.indexOf(v) === i),
      true,
      false
    )
  }

  private canonicalExistencePolicy(): CanonicalExistenceApplyRuntimePolicyFW {
    return new CanonicalExistenceApplyRuntimePolicyFW(
      this.policy.debugInternalBuild,
      this.policy.ownerApproved,
      this.policy.releaseDefaultBuild,
      this.policy.diagnosticsRedacted,
      this.policy.legacyFallbackAvailable,
      true,
      true,
      true,
      true,
      false,
      false
    )
  }

  private canonicalAudioPolicy(): CanonicalAudioUploadRuntimePolicy {
    return new CanonicalAudioUploadRuntimePolicy(
      this.policy.debugInternalBuild,
      this.policy.ownerApproved,
      false,
      this.policy.debugInternalBuild && this.policy.ownerApproved &&
      !this.policy.releaseDefaultBuild && this.policy.manualFullSyncConfirmation,
      this.policy.legacyFallbackAvailable,
      true,
      true
    )
  }

  private canonicalReadPolicy(): CanonicalReadRuntimePolicy {
    return new CanonicalReadRuntimePolicy(
      this.policy.debugInternalBuild,
      this.policy.ownerApproved,
      this.policy.manualFullSyncConfirmation,
      this.policy.releaseDefaultBuild,
      this.policy.legacyFallbackAvailable,
      this.policy.diagnosticsRedacted,
      true,
      true,
      true,
      true,
      true,
      true,
      true,
      true
    )
  }

  private advancedOverrideBlockers(
    base: CanonicalKernelSwitchEffectiveConfiguration
  ): CanonicalKernelSwitchBlocker[] {
    const blockers: CanonicalKernelSwitchBlocker[] = []
    const overrides = this.advancedOverrides
    if (overrides.syncRuntimeConfiguration && overrides.syncRuntimeConfiguration.mode !== base.syncRuntimeConfiguration.mode) {
      blockers.push(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
    }
    if (overrides.applyRuntimeConfiguration && overrides.applyRuntimeConfiguration.mode !== base.applyRuntimeConfiguration.mode) {
      blockers.push(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
    }
    if (overrides.existenceApplyRuntimeConfiguration && overrides.existenceApplyRuntimeConfiguration.mode !== base.existenceApplyRuntimeConfiguration.mode) {
      blockers.push(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
    }
    if (overrides.audioUploadRuntimeConfiguration && overrides.audioUploadRuntimeConfiguration.mode !== base.audioUploadRuntimeConfiguration.mode) {
      blockers.push(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
    }
    if (overrides.readRuntimeConfiguration && overrides.readRuntimeConfiguration.mode !== base.readRuntimeConfiguration.mode) {
      blockers.push(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
    }
    if (overrides.libraryMetadataDebugPilotConfiguration &&
      overrides.libraryMetadataDebugPilotConfiguration.mode !== base.libraryMetadataDebugPilotConfiguration.mode) {
      blockers.push(CanonicalKernelSwitchBlocker.ADVANCED_OVERRIDE_CONTRADICTS_MASTER_SWITCH)
    }
    return blockers
  }

  private ownerStateFor(mode: CanonicalKernelSwitchMode): CanonicalKernelSwitchOwnerState {
    switch (mode) {
      case CanonicalKernelSwitchMode.OLD_KERNEL:
        return CanonicalKernelSwitchOwnerState.OLD_KERNEL
      case CanonicalKernelSwitchMode.DIAGNOSTICS_ONLY:
      case CanonicalKernelSwitchMode.CANONICAL_DECISION_ONLY:
        return CanonicalKernelSwitchOwnerState.CANONICAL_NO_WRITE
      case CanonicalKernelSwitchMode.CANONICAL_SHADOW:
        return CanonicalKernelSwitchOwnerState.SHADOW
      case CanonicalKernelSwitchMode.CANONICAL_APPLY_NO_AUDIO:
      case CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC:
        return CanonicalKernelSwitchOwnerState.CANONICAL_READ_WRITE
      case CanonicalKernelSwitchMode.BLOCKED:
      default:
        return CanonicalKernelSwitchOwnerState.BLOCKED
    }
  }

  private static unique(blockers: CanonicalKernelSwitchBlocker[]): CanonicalKernelSwitchBlocker[] {
    const seen = new Set<string>()
    const result: CanonicalKernelSwitchBlocker[] = []
    for (const b of blockers) {
      if (!seen.has(b as string)) {
        seen.add(b as string)
        result.push(b)
      }
    }
    return result
  }

  private static diagnosticsSummary(
    requestedMode: CanonicalKernelSwitchMode,
    effectiveMode: CanonicalKernelSwitchMode,
    ownerState: CanonicalKernelSwitchOwnerState,
    configuration: CanonicalKernelSwitchEffectiveConfiguration,
    blockers: CanonicalKernelSwitchBlocker[]
  ): string {
    return [
      'canonicalKernelSwitch=v8.43',
      `requested=${requestedMode}`,
      `effective=${effectiveMode}`,
      `ownerState=${ownerState}`,
      `sync=${configuration.syncRuntimeConfiguration.mode}`,
      `apply=${configuration.applyRuntimeConfiguration.mode}`,
      `existence=${configuration.existenceApplyRuntimeConfiguration.mode}`,
      `audio=${configuration.audioUploadRuntimeConfiguration.mode}`,
      `read=${configuration.readRuntimeConfiguration.mode}`,
      `libraryMetadataPilot=${configuration.libraryMetadataDebugPilotConfiguration.mode}`,
      `diskFormat=${configuration.migrationMatrixPolicy.diskFormatPolicy}`,
      `switchBackMigration=${configuration.migrationMatrixPolicy.migrationRequiredToSwitchBack}`,
      `blockers=${blockers.join('|')}`,
      'redacted=true'
    ].join(',')
  }
}
