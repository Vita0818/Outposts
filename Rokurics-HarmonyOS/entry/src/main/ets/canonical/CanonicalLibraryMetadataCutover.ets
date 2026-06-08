/**
 * CanonicalLibraryMetadataCutover — library metadata cutover domain types.
 * Ported from Rokurics-Apple (CanonicalLibraryMetadataCutover.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalLibraryObjectID, CanonicalObjectKind,
  CanonicalLibraryObject, CanonicalLibraryMetadata, CanonicalFolderObject,
  CanonicalStudyItemObject, nilIfEmpty, hashPrefix
} from './CanonicalCore'
import {
  CanonicalApplyAction, CanonicalApplyActionKind, CanonicalApplyTarget,
  CanonicalApplySource, CanonicalApplyBridgeHint
} from './CanonicalApplyPlan'
import { CanonicalLibrarySyncPlan, CanonicalSyncPlanTrigger } from './CanonicalLibrarySyncPlanner'

// ── Types referenced but potentially from other files ──
// Minimal local interfaces for types not yet ported

export interface CanonicalRollbackPlan {
  planID: string
  checkpoints: string[]
  actions: string[]
  covers(domain: string): boolean
}

export interface CanonicalRollbackResult {
  checkpointID: string
  succeeded: boolean
  fatal: boolean
}

export enum CanonicalProductionDomain {
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

export enum CanonicalMigrationDomain {
  RECORDING_METADATA = 'recordingMetadata',
  RECORDING_AUDIO = 'recordingAudio',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  LIBRARY_METADATA = 'libraryMetadata',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  AUDIO_UPLOAD = 'audioUpload',
  OBJECT_PROJECTION = 'objectProjection',
  INVENTORY = 'inventory'
}

export enum CanonicalMigrationStage {
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
  RETIREMENT_CANDIDATE = 'retirementCandidate'
}

export enum CanonicalMigrationStageStatus {
  BLOCKED = 'blocked',
  PROJECTED = 'projected',
  PLANNED = 'planned',
  NO_COMMIT_EVIDENCE_AVAILABLE = 'noCommitEvidenceAvailable',
  REAL_APPLY_PORT_PASSED = 'realApplyPortPassed',
  COMMIT_EXECUTOR_PASSED = 'commitExecutorPassed',
  APP_SEAM_DEFAULT_OFF_PASSED = 'appSeamDefaultOffPassed',
  NEXT_PILOT_CANDIDATE_PASSED = 'nextPilotCandidatePassed',
  CANARY_N0_PASSED = 'canaryN0Passed',
  WRITE_SIDE_CANARY_OBSERVED = 'writeSideCanaryObserved',
  READ_SIDE_OBSERVED = 'readSideObserved',
  OBSERVATION_COMPLETE = 'observationComplete',
  RETIREMENT_BLOCKED = 'retirementBlocked',
  RETIREMENT_CANDIDATE_READY = 'retirementCandidateReady',
  COMPLETE = 'complete'
}

export enum CanonicalProductionExecutionDomainRole {
  IPHONE = 'iphone',
  MAC = 'mac',
  TEST_HARNESS = 'testHarness',
  MANUAL = 'manual'
}

export enum CanonicalCutoverMode {
  DISABLED = 'disabled',
  SHADOW_ONLY = 'shadowOnly',
  GUARDED_EXECUTE_NO_COMMIT = 'guardedExecuteNoCommit',
  GUARDED_EXECUTE_COMMIT = 'guardedExecuteCommit',
  CANARY = 'canary',
  ROLLBACK_ONLY = 'rollbackOnly',
  LEGACY_FALLBACK_ONLY = 'legacyFallbackOnly'
}

export enum CanonicalShadowMigrationMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  DRY_RUN_COMPARE = 'dryRunCompare',
  EXECUTION_SHADOW_DRY_RUN = 'executionShadowDryRun',
  EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE = 'executionShadowWithShadowFileStore',
  SHADOW_READ_ONLY = 'shadowReadOnly',
  SHADOW_READ_ONLY_WITH_NETWORK_PROBE = 'shadowReadOnlyWithNetworkProbe',
  EXECUTION_SHADOW_WITH_READ_ONLY_TRANSPORT_PROBE = 'executionShadowWithReadOnlyTransportProbe',
  BLOCKED_PRODUCTION_EXECUTE = 'blockedProductionExecute',
  BLOCKED_EXECUTION_SHADOW_WRITE = 'blockedExecutionShadowWrite',
  BLOCKED_EXECUTION_SHADOW_UPLOAD = 'blockedExecutionShadowUpload',
  BLOCKED_EXECUTION_SHADOW_APPLY = 'blockedExecutionShadowApply'
}

export enum CanonicalShadowMigrationTrigger {
  PERIODIC = 'periodic',
  APP_ACTIVATION = 'appActivation',
  RETRY_DRAINER = 'retryDrainer',
  VIEW_REFRESH = 'viewRefresh',
  IPHONE_SYNC_TICK = 'iPhoneSyncTick',
  MAC_INVENTORY = 'macInventory',
  MAC_RECEIVER = 'macReceiver',
  MANUAL = 'manual',
  TEST_HARNESS = 'testHarness'
}

// ── Safe redaction utilities ──

class CanonicalProductionRedaction {
  static safeIdentifier(value: string, fallback: string): string {
    const trimmed = value.trim()
    if (trimmed.length === 0) {
      return fallback
    }
    return trimmed
  }

  static safeDiagnosticText(value: string | null | undefined): string | null {
    if (!value) {
      return null
    }
    const trimmed = value.trim()
    return trimmed.length === 0 ? null : trimmed
  }

  static hashPrefix(value: string | null | undefined): string | null {
    if (!value) {
      return null
    }
    return hashPrefix(value, 12)
  }

  static containsSensitivePathSignal(value: string): boolean {
    return value.includes('/private/') ||
      value.includes('/Library/Application Support/') ||
      value.includes('/.gradle/')
  }
}

// ── Canonical Migration Domain Policy ──

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
  hasActiveCanaryOrCutover: boolean

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
    fallbackReady: boolean = false,
    hasActiveCanaryOrCutover: boolean = false
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
    this.hasActiveCanaryOrCutover = hasActiveCanaryOrCutover
  }
}

// ── Canonical Migration Domain Matrix Validation ──

export class CanonicalMigrationDomainMatrixValidationReport {
  allowed: boolean
  activePilotDomain: CanonicalMigrationDomain | null
  blockers: string[]

  constructor(allowed: boolean, activePilotDomain: CanonicalMigrationDomain | null = null, blockers: string[] = []) {
    this.allowed = allowed
    this.activePilotDomain = activePilotDomain
    this.blockers = [...new Set(blockers)].sort()
  }
}

export class CanonicalMigrationDomainMatrix {
  policies: CanonicalMigrationDomainPolicy[]
  libraryMetadataPilotComplete: boolean

  constructor(policies: CanonicalMigrationDomainPolicy[] = [], libraryMetadataPilotComplete: boolean = false) {
    this.policies = policies
    this.libraryMetadataPilotComplete = libraryMetadataPilotComplete
  }

  static defaultV813(): CanonicalMigrationDomainMatrix {
    return new CanonicalMigrationDomainMatrix([], false)
  }

  policyFor(domain: CanonicalMigrationDomain): CanonicalMigrationDomainPolicy | null {
    for (const p of this.policies) {
      if (p.domain === domain) {
        return p
      }
    }
    return null
  }

  validate(): CanonicalMigrationDomainMatrixValidationReport {
    const active = this.policies.filter(p => p.activePilot)
    const activeDomain = active.length === 1 ? active[0].domain : null
    const blockers: string[] = []
    if (active.length === 0) {
      blockers.push('noActivePilot')
    }
    if (active.length > 1) {
      blockers.push('multipleActivePilots')
    }
    const nonLibraryNotStatic = this.policies.filter(p =>
      p.domain !== CanonicalMigrationDomain.LIBRARY_METADATA && !p.staticOnly
    )
    if (nonLibraryNotStatic.length > 0) {
      blockers.push('nonPilotDomainNotStaticOnly')
    }
    const runtimeSwitchEnabled = this.policies.some(p => p.runtimeSwitchEnabled)
    if (runtimeSwitchEnabled) {
      blockers.push('runtimeSwitchEnabled')
    }
    return new CanonicalMigrationDomainMatrixValidationReport(
      blockers.length === 0,
      activeDomain,
      blockers
    )
  }
}

// ── Canonical Library Metadata Cutover Domain ──

export enum CanonicalLibraryMetadataCutoverDomain {
  FOLDER_METADATA = 'folderMetadata',
  STUDY_ITEM_METADATA = 'studyItemMetadata',
  STANDALONE_NOTE_METADATA = 'standaloneNoteMetadata'
}

// ── Canonical Library Metadata Cutover Action Kind ──

export enum CanonicalLibraryMetadataCutoverActionKind {
  FOLDER_APPLY = 'folderApply',
  FOLDER_SEND = 'folderSend',
  STUDY_ITEM_APPLY = 'studyItemApply',
  STUDY_ITEM_SEND = 'studyItemSend',
  STANDALONE_NOTE_APPLY = 'standaloneNoteApply',
  STANDALONE_NOTE_SEND = 'standaloneNoteSend',
  CONFLICT_RECORD = 'conflictRecord',
  TOMBSTONE_MARKER_UNSUPPORTED_FOR_THIS_ROUND = 'tombstoneMarkerUnsupportedForThisRound',
  UNSUPPORTED = 'unsupported'
}

// ── Canonical Library Metadata Cutover Failure ──

export enum CanonicalLibraryMetadataCutoverFailure {
  DISABLED = 'disabled',
  UNSUPPORTED_MODE = 'unsupportedMode',
  UNSUPPORTED_DOMAIN = 'unsupportedDomain',
  UNSUPPORTED_OBJECT_KIND = 'unsupportedObjectKind',
  UNSUPPORTED_ACTION = 'unsupportedAction',
  MISSING_TOKEN = 'missingToken',
  MISSING_OWNER_APPROVAL = 'missingOwnerApproval',
  MISSING_ROLLBACK = 'missingRollback',
  MISSING_NO_COMMIT_EVIDENCE = 'missingNoCommitEvidence',
  MISSING_DRY_RUN_EQUIVALENCE = 'missingDryRunEquivalence',
  MISSING_EXECUTION_SHADOW_EVIDENCE = 'missingExecutionShadowEvidence',
  MISSING_REAL_DATA_SHADOW_COPY_EVIDENCE = 'missingRealDataShadowCopyEvidence',
  BLOCKING_DIVERGENCE = 'blockingDivergence',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  ACTIVE_VS_TOMBSTONE_CONFLICT = 'activeVsTombstoneConflict',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  MISSING_METADATA_MANIFEST_ROUTE_EVIDENCE = 'missingMetadataManifestRouteEvidence',
  PRODUCTION_PORT_UNAVAILABLE = 'productionPortUnavailable',
  APPLY_PORT_DRY_RUN_ONLY = 'applyPortDryRunOnly',
  ROOT_BOUND_WRITE_UNAVAILABLE = 'rootBoundWriteUnavailable',
  ATOMIC_REPLACE_UNAVAILABLE = 'atomicReplaceUnavailable',
  ROLLBACK_CHECKPOINT_UNAVAILABLE = 'rollbackCheckpointUnavailable',
  ROLLBACK_VERIFICATION_MISSING = 'rollbackVerificationMissing',
  PRODUCTION_ROOT_ENABLED_BY_DEFAULT = 'productionRootEnabledByDefault',
  TEST_ROOT_MISSING = 'testRootMissing',
  OBJECT_ID_MISMATCH = 'objectIDMismatch',
  OBJECT_KIND_MISMATCH = 'objectKindMismatch',
  EXPECTED_METADATA_HASH_MISSING = 'expectedMetadataHashMissing',
  EXPECTED_BUSINESS_MODIFIED_AT_MISSING = 'expectedBusinessModifiedAtMissing',
  BUSINESS_MODIFIED_AT_DIRECTION_MISMATCH = 'businessModifiedAtDirectionMismatch',
  APPLY_FAILURE_BEFORE_COMMIT = 'applyFailureBeforeCommit',
  APPLY_FAILURE_AFTER_PARTIAL_COMMIT = 'applyFailureAfterPartialCommit',
  POSTCONDITION_MISMATCH = 'postconditionMismatch',
  ROLLBACK_FAILURE = 'rollbackFailure',
  PARENT_MISSING = 'parentMissing',
  CYCLE_DETECTED = 'cycleDetected',
  RESOURCE_MOVE_ATTEMPTED = 'resourceMoveAttempted',
  TOMBSTONE_UNSUPPORTED_FOR_THIS_ROUND = 'tombstoneUnsupportedForThisRound',
  CONFLICT_DETECTED = 'conflictDetected',
  MISSING_INTERNAL_CANARY_CONFIGURATION = 'missingInternalCanaryConfiguration',
  CANARY_BUDGET_ABOVE_ONE_DENIED = 'canaryBudgetAboveOneDenied',
  MISSING_CANARY_STAGE_EVIDENCE = 'missingCanaryStageEvidence',
  CANARY_STAGE_BLOCKED = 'canaryStageBlocked',
  CANARY_STAGE_ORDER_VIOLATION = 'canaryStageOrderViolation',
  OBSERVATION_WINDOW_INCOMPLETE = 'observationWindowIncomplete',
  PREVIOUS_STAGE_FAILURE = 'previousStageFailure',
  PREVIOUS_STAGE_ROLLBACK_FAILURE = 'previousStageRollbackFailure',
  PREVIOUS_STAGE_BLOCKING_DIVERGENCE = 'previousStageBlockingDivergence',
  PREVIOUS_STAGE_UNRESOLVED_CONFLICT = 'previousStageUnresolvedConflict',
  PREVIOUS_STAGE_POSTCONDITION_FAILURE = 'previousStagePostconditionFailure',
  PREVIOUS_STAGE_UNSUPPORTED_OBJECT = 'previousStageUnsupportedObject',
  ALL_ELIGIBLE_CANARY_DENIED = 'allEligibleCanaryDenied',
  ACTIVE_PILOT_NOT_LIBRARY_METADATA = 'activePilotNotLibraryMetadata',
  MATRIX_VALIDATION_BLOCKED = 'matrixValidationBlocked',
  DEFAULT_ENABLEMENT_DENIED = 'defaultEnablementDenied',
  MISSING_READ_SIDE_PARALLEL_EVIDENCE = 'missingReadSideParallelEvidence',
  READ_SIDE_PARALLEL_DIVERGENCE = 'readSideParallelDivergence',
  MULTIPLE_ELIGIBLE_CANDIDATES_DENIED = 'multipleEligibleCandidatesDenied',
  FOLDER_HIERARCHY_MUTATION_UNSUPPORTED = 'folderHierarchyMutationUnsupported',
  STANDALONE_NOTE_CONTENT_MUTATION_UNSUPPORTED = 'standaloneNoteContentMutationUnsupported',
  COMMIT_EXECUTOR_UNAVAILABLE = 'commitExecutorUnavailable',
  OBJECT_ID_INSTABILITY = 'objectIDInstability',
  RUNTIME_SWITCH_DENIED = 'runtimeSwitchDenied',
  PEER_SNAPSHOT_UNAVAILABLE = 'peerSnapshotUnavailable'
}

// ── Canonical Library Metadata Cutover Candidate ──

export class CanonicalLibraryMetadataCutoverCandidate {
  action: CanonicalApplyAction
  localObject: CanonicalLibraryObject | null
  peerObject: CanonicalLibraryObject | null
  rollbackCheckpointID: string | null
  unresolvedConflict: boolean
  parentMissingKnown: boolean
  routePath: string

  constructor(
    action: CanonicalApplyAction,
    localObject: CanonicalLibraryObject | null,
    peerObject: CanonicalLibraryObject | null,
    rollbackCheckpointID: string | null = null,
    unresolvedConflict: boolean = false,
    parentMissingKnown: boolean = false,
    routePath: string = '/sync/apply-metadata'
  ) {
    this.action = action
    this.localObject = localObject
    this.peerObject = peerObject
    this.rollbackCheckpointID = rollbackCheckpointID
      ? CanonicalProductionRedaction.safeIdentifier(rollbackCheckpointID, 'library-metadata-checkpoint')
      : null
    this.unresolvedConflict = unresolvedConflict
    this.parentMissingKnown = parentMissingKnown
    this.routePath = CanonicalProductionRedaction.safeDiagnosticText(routePath) ?? '/sync/apply-metadata'
  }

  get id(): string {
    return this.action.actionID
  }

  get objectID(): string {
    return this.action.target.objectID
  }

  get objectKind(): CanonicalObjectKind {
    return this.expectedObject?.kind ?? this.localObject?.kind ??
      this.peerObject?.kind ?? this.inferredObjectKind
  }

  get domain(): CanonicalLibraryMetadataCutoverDomain {
    switch (this.objectKind) {
      case CanonicalObjectKind.FOLDER:
        return CanonicalLibraryMetadataCutoverDomain.FOLDER_METADATA
      case CanonicalObjectKind.STANDALONE_NOTE:
        return CanonicalLibraryMetadataCutoverDomain.STANDALONE_NOTE_METADATA
      default:
        return CanonicalLibraryMetadataCutoverDomain.STUDY_ITEM_METADATA
    }
  }

  get cutoverActionKind(): CanonicalLibraryMetadataCutoverActionKind {
    const ak = this.action.kind
    if (ak === CanonicalApplyActionKind.FOLDER_METADATA_APPLY) {
      return this.objectKind === CanonicalObjectKind.FOLDER
        ? CanonicalLibraryMetadataCutoverActionKind.FOLDER_APPLY
        : CanonicalLibraryMetadataCutoverActionKind.UNSUPPORTED
    }
    if (ak === CanonicalApplyActionKind.FOLDER_METADATA_SEND) {
      return this.objectKind === CanonicalObjectKind.FOLDER
        ? CanonicalLibraryMetadataCutoverActionKind.FOLDER_SEND
        : CanonicalLibraryMetadataCutoverActionKind.UNSUPPORTED
    }
    if (ak === CanonicalApplyActionKind.STUDY_ITEM_METADATA_APPLY) {
      return this.objectKind === CanonicalObjectKind.STANDALONE_NOTE
        ? CanonicalLibraryMetadataCutoverActionKind.STANDALONE_NOTE_APPLY
        : CanonicalLibraryMetadataCutoverActionKind.STUDY_ITEM_APPLY
    }
    if (ak === CanonicalApplyActionKind.STUDY_ITEM_METADATA_SEND) {
      return this.objectKind === CanonicalObjectKind.STANDALONE_NOTE
        ? CanonicalLibraryMetadataCutoverActionKind.STANDALONE_NOTE_SEND
        : CanonicalLibraryMetadataCutoverActionKind.STUDY_ITEM_SEND
    }
    if (ak === CanonicalApplyActionKind.CONFLICT_RECORD) {
      return CanonicalLibraryMetadataCutoverActionKind.CONFLICT_RECORD
    }
    if (ak === CanonicalApplyActionKind.LIBRARY_TOMBSTONE_APPLY ||
      ak === CanonicalApplyActionKind.LIBRARY_TOMBSTONE_SEND) {
      return CanonicalLibraryMetadataCutoverActionKind.TOMBSTONE_MARKER_UNSUPPORTED_FOR_THIS_ROUND
    }
    return CanonicalLibraryMetadataCutoverActionKind.UNSUPPORTED
  }

  get expectedObject(): CanonicalLibraryObject | null {
    switch (this.action.source) {
      case CanonicalApplySource.PEER:
        return this.peerObject ?? this.localObject
      case CanonicalApplySource.LOCAL:
        return this.localObject ?? this.peerObject
      case CanonicalApplySource.PLANNER:
        return this.peerObject ?? this.localObject
    }
  }

  get expectedMetadataHash(): CanonicalHash | null {
    return this.expectedObject?.metadataHash ?? null
  }

  get expectedBusinessModifiedAt(): CanonicalTimestamp | null {
    return this.expectedObject?.businessModifiedAt ?? null
  }

  get effectiveRollbackCheckpointID(): string {
    return this.rollbackCheckpointID ??
      'library-metadata-cutover-' + this.objectID + '-' + this.cutoverActionKind
  }

  get metadataTitle(): string {
    return this.expectedObject?.metadata?.title ?? 'metadata'
  }

  get parentSummary(): string {
    const obj = this.expectedObject
    if (!obj) {
      return 'parent=unknown'
    }
    if (obj.folder?.metadata) {
      const folder = obj.folder.metadata
      return 'parent=' + (folder.parentID?.rawValue ?? 'root')
    }
    if (obj.studyItem?.metadata) {
      const item = obj.studyItem.metadata
      const folders = item.folderIDs.map(id => id.rawValue).slice(0, 3).join('|')
      const parentRefs = item.parentReferences.map(r => r.parentID.rawValue).slice(0, 3).join('|')
      return 'folders=' + (folders.length === 0 ? 'none' : folders) +
        ',parents=' + (parentRefs.length === 0 ? 'none' : parentRefs)
    }
    return 'parent=none'
  }

  get tagCount(): number {
    return this.expectedObject?.studyItem?.metadata.tags.length ?? 0
  }

  get filingSummary(): string {
    return this.expectedObject?.studyItem?.metadata.filingPath.components.join('/') ?? 'none'
  }

  get colorSummary(): string {
    return this.expectedObject?.folder?.metadata.colorToken ?? 'none'
  }

  get logicalResourceTokens(): string[] {
    return this.expectedObject?.studyItem?.metadata.logicalResourceTokens ?? []
  }

  get hasResourceMoveAttempt(): boolean {
    const local = this.localObject?.studyItem?.metadata.logicalResourceTokens
    const peer = this.peerObject?.studyItem?.metadata.logicalResourceTokens
    if (!local && !peer) {
      return false
    }
    if ((!local || local.length === 0) && (!peer || peer.length === 0)) {
      return false
    }
    const localStr = (local ?? []).join(',')
    const peerStr = (peer ?? []).join(',')
    return localStr !== peerStr
  }

  get folderHierarchyMutationAttempted(): boolean {
    const local = this.localObject?.folder?.metadata
    const peer = this.peerObject?.folder?.metadata
    if (!local || !peer) {
      return false
    }
    return local.parentID?.rawValue !== peer.parentID?.rawValue ||
      local.hierarchyPath?.stableKey !== peer.hierarchyPath?.stableKey ||
      local.hierarchyLevel !== peer.hierarchyLevel
  }

  get hasObjectIDInstability(): boolean {
    switch (this.objectKind) {
      case CanonicalObjectKind.FOLDER:
        return this.expectedObject?.folder?.folderID.rawValue !== this.objectID
      case CanonicalObjectKind.STANDALONE_NOTE:
        return this.expectedObject?.standaloneNote?.noteID.rawValue !== this.objectID
      case CanonicalObjectKind.STANDALONE_STUDY_ITEM:
      case CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM:
        return this.expectedObject?.studyItem?.itemID.rawValue !== this.objectID
      default:
        return false
    }
  }

  get hasActiveVsTombstoneConflict(): boolean {
    if (!this.localObject || !this.peerObject) {
      return false
    }
    return this.localObject.isDeleted !== this.peerObject.isDeleted &&
      this.cutoverActionKind === CanonicalLibraryMetadataCutoverActionKind.CONFLICT_RECORD
  }

  static candidates(
    libraryPlan: CanonicalLibrarySyncPlan,
    localManifest: CanonicalManifest,
    peerManifest: CanonicalManifest,
    rollbackCheckpointPrefix: string = 'library-metadata-cutover'
  ): CanonicalLibraryMetadataCutoverCandidate[] {
    const localObjects: Record<string, CanonicalLibraryObject> = {}
    for (const obj of localManifest.libraryObjects) {
      localObjects[obj.objectID.rawValue] = obj
    }
    const peerObjects: Record<string, CanonicalLibraryObject> = {}
    for (const obj of peerManifest.libraryObjects) {
      peerObjects[obj.objectID.rawValue] = obj
    }
    const result: CanonicalLibraryMetadataCutoverCandidate[] = []
    for (const action of libraryPlan.applyActions) {
      if (action.kind !== CanonicalApplyActionKind.FOLDER_METADATA_APPLY &&
        action.kind !== CanonicalApplyActionKind.FOLDER_METADATA_SEND &&
        action.kind !== CanonicalApplyActionKind.STUDY_ITEM_METADATA_APPLY &&
        action.kind !== CanonicalApplyActionKind.STUDY_ITEM_METADATA_SEND &&
        action.kind !== CanonicalApplyActionKind.CONFLICT_RECORD &&
        action.kind !== CanonicalApplyActionKind.LIBRARY_TOMBSTONE_APPLY &&
        action.kind !== CanonicalApplyActionKind.LIBRARY_TOMBSTONE_SEND) {
        continue
      }
      const objectID = new CanonicalLibraryObjectID(action.target.objectID)
      result.push(new CanonicalLibraryMetadataCutoverCandidate(
        action,
        localObjects[action.target.objectID] ?? null,
        peerObjects[action.target.objectID] ?? null,
        rollbackCheckpointPrefix + '-' + action.target.objectID
      ))
    }
    return result
  }

  static folderHierarchyCycleDetected(candidates: CanonicalLibraryMetadataCutoverCandidate[]): boolean {
    const parentsByFolderID: Record<string, string> = {}
    for (const candidate of candidates) {
      const folder = candidate.expectedObject?.folder?.metadata
      if (!folder) {
        continue
      }
      if (folder.parentID?.rawValue === folder.folderID.rawValue) {
        return true
      }
      if (folder.parentID?.rawValue) {
        parentsByFolderID[folder.folderID.rawValue] = folder.parentID.rawValue
      }
    }
    for (const folderID of Object.keys(parentsByFolderID)) {
      const seen = new Set<string>()
      let cursor: string | undefined = folderID
      while (cursor) {
        if (seen.has(cursor)) {
          return true
        }
        seen.add(cursor)
        cursor = parentsByFolderID[cursor]
      }
    }
    return false
  }

  private get inferredObjectKind(): CanonicalObjectKind {
    const ak = this.action.kind
    if (ak === CanonicalApplyActionKind.FOLDER_METADATA_APPLY ||
      ak === CanonicalApplyActionKind.FOLDER_METADATA_SEND) {
      return CanonicalObjectKind.FOLDER
    }
    if (ak === CanonicalApplyActionKind.STUDY_ITEM_METADATA_APPLY ||
      ak === CanonicalApplyActionKind.STUDY_ITEM_METADATA_SEND) {
      return CanonicalObjectKind.STANDALONE_STUDY_ITEM
    }
    return CanonicalObjectKind.UNKNOWN_UNSUPPORTED
  }
}

// ── Canonical Library Metadata Apply Port Mode ──

export enum CanonicalLibraryMetadataApplyPortMode {
  DISABLED = 'disabled',
  DRY_RUN = 'dryRun',
  FAKE_IN_MEMORY = 'fakeInMemory',
  TEST_ROOT_BOUND = 'testRootBound',
  PRODUCTION_ROOT_DISABLED = 'productionRootDisabled',
  PRODUCTION_ROOT_BOUND = 'productionRootBound',
  PRODUCTION_ROOT_UNSUPPORTED = 'productionRootUnsupported'
}

// ── Canonical Library Metadata Cutover Evidence ──

export class CanonicalLibraryMetadataCutoverEvidence {
  noCommitEvidenceAvailable: boolean
  realDataShadowCopyVerified: boolean
  executionShadowVerified: boolean
  dryRunEquivalenceVerified: boolean
  noBlockingDivergence: boolean
  noUnresolvedConflict: boolean
  metadataManifestRouteEvidenceAvailable: boolean
  productionPortAvailable: boolean
  realRootBoundApplyPortAvailable: boolean
  applyPortMode: CanonicalLibraryMetadataApplyPortMode
  rootBoundWriteAvailable: boolean
  atomicReplaceAvailable: boolean
  rollbackCheckpointAvailable: boolean
  rollbackVerified: boolean
  productionRootDisabledByDefault: boolean
  testRootUsed: boolean
  legacyFallbackAvailable: boolean
  rollbackPlan: CanonicalRollbackPlan | null
  rollbackRehearsalPassed: boolean
  readSideParallelEquivalent: boolean
  canaryStageEvidence: CanonicalLibraryMetadataCanaryStageEvidence | null

  constructor(
    noCommitEvidenceAvailable: boolean = false,
    realDataShadowCopyVerified: boolean = false,
    executionShadowVerified: boolean = false,
    dryRunEquivalenceVerified: boolean = false,
    noBlockingDivergence: boolean = false,
    noUnresolvedConflict: boolean = false,
    metadataManifestRouteEvidenceAvailable: boolean = false,
    productionPortAvailable: boolean = false,
    realRootBoundApplyPortAvailable: boolean = false,
    applyPortMode: CanonicalLibraryMetadataApplyPortMode = CanonicalLibraryMetadataApplyPortMode.DISABLED,
    rootBoundWriteAvailable: boolean = false,
    atomicReplaceAvailable: boolean = false,
    rollbackCheckpointAvailable: boolean = false,
    rollbackVerified: boolean = false,
    productionRootDisabledByDefault: boolean = false,
    testRootUsed: boolean = false,
    legacyFallbackAvailable: boolean = false,
    rollbackPlan: CanonicalRollbackPlan | null = null,
    rollbackRehearsalPassed: boolean = false,
    readSideParallelEquivalent: boolean = false,
    canaryStageEvidence: CanonicalLibraryMetadataCanaryStageEvidence | null = null
  ) {
    this.noCommitEvidenceAvailable = noCommitEvidenceAvailable
    this.realDataShadowCopyVerified = realDataShadowCopyVerified
    this.executionShadowVerified = executionShadowVerified
    this.dryRunEquivalenceVerified = dryRunEquivalenceVerified
    this.noBlockingDivergence = noBlockingDivergence
    this.noUnresolvedConflict = noUnresolvedConflict
    this.metadataManifestRouteEvidenceAvailable = metadataManifestRouteEvidenceAvailable
    this.productionPortAvailable = productionPortAvailable
    this.realRootBoundApplyPortAvailable = realRootBoundApplyPortAvailable
    this.applyPortMode = applyPortMode
    this.rootBoundWriteAvailable = rootBoundWriteAvailable
    this.atomicReplaceAvailable = atomicReplaceAvailable
    this.rollbackCheckpointAvailable = rollbackCheckpointAvailable
    this.rollbackVerified = rollbackVerified
    this.productionRootDisabledByDefault = productionRootDisabledByDefault
    this.testRootUsed = testRootUsed
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.rollbackPlan = rollbackPlan
    this.rollbackRehearsalPassed = rollbackRehearsalPassed
    this.readSideParallelEquivalent = readSideParallelEquivalent
    this.canaryStageEvidence = canaryStageEvidence
  }

  static passing(rollbackPlan: CanonicalRollbackPlan): CanonicalLibraryMetadataCutoverEvidence {
    return new CanonicalLibraryMetadataCutoverEvidence(
      true, true, true, true, true, true, true, true, true,
      CanonicalLibraryMetadataApplyPortMode.TEST_ROOT_BOUND,
      true, true, true, true, true, true, true, rollbackPlan, true, true
    )
  }
}

// ── Canonical Library Metadata Canary Stage ──

export enum CanonicalLibraryMetadataCanaryStage {
  DISABLED = 'disabled',
  N1 = 'n1',
  N3 = 'n3',
  N10 = 'n10',
  ALL_ELIGIBLE = 'allEligible'
}

// ── Canonical Library Metadata Canary Stage Policy ──

export class CanonicalLibraryMetadataCanaryStagePolicy {
  requestedStage: CanonicalLibraryMetadataCanaryStage
  allowCandidateExecution: boolean
  runtimeSwitchEnabled: boolean

  constructor(
    requestedStage: CanonicalLibraryMetadataCanaryStage = CanonicalLibraryMetadataCanaryStage.DISABLED,
    allowCandidateExecution: boolean = false,
    runtimeSwitchEnabled: boolean = false
  ) {
    this.requestedStage = requestedStage
    this.allowCandidateExecution = allowCandidateExecution
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
  }

  static readonly DISABLED = new CanonicalLibraryMetadataCanaryStagePolicy()

  get canaryBudget(): number {
    return canonicalCanaryBudget(this.requestedStage)
  }
}

function canonicalCanaryBudget(stage: CanonicalLibraryMetadataCanaryStage): number {
  switch (stage) {
    case CanonicalLibraryMetadataCanaryStage.DISABLED: return 0
    case CanonicalLibraryMetadataCanaryStage.N1: return 1
    case CanonicalLibraryMetadataCanaryStage.N3: return 3
    case CanonicalLibraryMetadataCanaryStage.N10: return 10
    case CanonicalLibraryMetadataCanaryStage.ALL_ELIGIBLE: return Number.MAX_SAFE_INTEGER
  }
}

// ── Canonical Library Metadata Stage Evidence Status ──

export enum CanonicalLibraryMetadataStageEvidenceStatus {
  MISSING = 'missing',
  INCOMPLETE = 'incomplete',
  PASSED = 'passed',
  FAILED = 'failed',
  BLOCKED = 'blocked'
}

// ── Canonical Library Metadata Canary Stage Evidence ──

export class CanonicalLibraryMetadataCanaryStageEvidence {
  stage: CanonicalLibraryMetadataCanaryStage
  previousStage: CanonicalLibraryMetadataCanaryStage | null
  status: CanonicalLibraryMetadataStageEvidenceStatus
  successfulCommitCount: number
  failedCommitCount: number
  rollbackFailureCount: number
  blockingDivergenceCount: number
  unresolvedConflictCount: number
  postconditionFailureCount: number
  unsupportedObjectCount: number
  resourceMoveAttemptCount: number
  folderCycleCount: number
  objectIDInstabilityCount: number
  suppressedLegacyDuplicateCount: number
  readSideParallelDivergenceCount: number
  noCommitEvidenceAvailable: boolean
  observationWindowComplete: boolean
  observationWindowID: string | null

  constructor(
    stage: CanonicalLibraryMetadataCanaryStage,
    previousStage: CanonicalLibraryMetadataCanaryStage | null = null,
    status: CanonicalLibraryMetadataStageEvidenceStatus,
    successfulCommitCount: number = 0,
    failedCommitCount: number = 0,
    rollbackFailureCount: number = 0,
    blockingDivergenceCount: number = 0,
    unresolvedConflictCount: number = 0,
    postconditionFailureCount: number = 0,
    unsupportedObjectCount: number = 0,
    resourceMoveAttemptCount: number = 0,
    folderCycleCount: number = 0,
    objectIDInstabilityCount: number = 0,
    suppressedLegacyDuplicateCount: number = 0,
    readSideParallelDivergenceCount: number = 0,
    noCommitEvidenceAvailable: boolean = false,
    observationWindowComplete: boolean = false,
    observationWindowID: string | null = null
  ) {
    this.stage = stage
    this.previousStage = previousStage
    this.status = status
    this.successfulCommitCount = Math.max(0, successfulCommitCount)
    this.failedCommitCount = Math.max(0, failedCommitCount)
    this.rollbackFailureCount = Math.max(0, rollbackFailureCount)
    this.blockingDivergenceCount = Math.max(0, blockingDivergenceCount)
    this.unresolvedConflictCount = Math.max(0, unresolvedConflictCount)
    this.postconditionFailureCount = Math.max(0, postconditionFailureCount)
    this.unsupportedObjectCount = Math.max(0, unsupportedObjectCount)
    this.resourceMoveAttemptCount = Math.max(0, resourceMoveAttemptCount)
    this.folderCycleCount = Math.max(0, folderCycleCount)
    this.objectIDInstabilityCount = Math.max(0, objectIDInstabilityCount)
    this.suppressedLegacyDuplicateCount = Math.max(0, suppressedLegacyDuplicateCount)
    this.readSideParallelDivergenceCount = Math.max(0, readSideParallelDivergenceCount)
    this.noCommitEvidenceAvailable = noCommitEvidenceAvailable
    this.observationWindowComplete = observationWindowComplete
    this.observationWindowID = observationWindowID
      ? CanonicalProductionRedaction.safeIdentifier(observationWindowID, 'stage-observation')
      : null
  }

  static passing(
    stage: CanonicalLibraryMetadataCanaryStage,
    successfulCommitCount: number
  ): CanonicalLibraryMetadataCanaryStageEvidence {
    return new CanonicalLibraryMetadataCanaryStageEvidence(
      stage,
      canonicalPreviousStage(stage),
      CanonicalLibraryMetadataStageEvidenceStatus.PASSED,
      successfulCommitCount,
      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, true, true, stage + '-observation'
    )
  }
}

function canonicalPreviousStage(stage: CanonicalLibraryMetadataCanaryStage): CanonicalLibraryMetadataCanaryStage | null {
  switch (stage) {
    case CanonicalLibraryMetadataCanaryStage.DISABLED: return null
    case CanonicalLibraryMetadataCanaryStage.N1: return CanonicalLibraryMetadataCanaryStage.DISABLED
    case CanonicalLibraryMetadataCanaryStage.N3: return CanonicalLibraryMetadataCanaryStage.N1
    case CanonicalLibraryMetadataCanaryStage.N10: return CanonicalLibraryMetadataCanaryStage.N3
    case CanonicalLibraryMetadataCanaryStage.ALL_ELIGIBLE: return CanonicalLibraryMetadataCanaryStage.N10
  }
}

// ── Stage Evidence Probe Status ──

export enum CanonicalLibraryMetadataStageEvidenceProbeStatus {
  MISSING = 'missing',
  EQUIVALENT = 'equivalent',
  VERIFIED = 'verified',
  DIVERGENT = 'divergent',
  BLOCKED = 'blocked'
}

// ── Stage Evidence Blocker ──

export enum CanonicalLibraryMetadataStageEvidenceBlocker {
  MISSING_PREVIOUS_STAGE_EVIDENCE = 'missingPreviousStageEvidence',
  OBSERVATION_WINDOW_INCOMPLETE = 'observationWindowIncomplete',
  PREVIOUS_STAGE_FAILED = 'previousStageFailed',
  ROLLBACK_FAILED = 'rollbackFailed',
  BLOCKING_DIVERGENCE = 'blockingDivergence',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  POSTCONDITION_FAILED = 'postconditionFailed',
  UNSUPPORTED_OBJECT = 'unsupportedObject',
  RESOURCE_MOVE_ATTEMPTED = 'resourceMoveAttempted',
  HIERARCHY_CYCLE = 'hierarchyCycle',
  OBJECT_ID_INSTABILITY = 'objectIDInstability',
  MISSING_NO_COMMIT_EVIDENCE = 'missingNoCommitEvidence',
  DRY_RUN_EQUIVALENCE_MISSING = 'dryRunEquivalenceMissing',
  EXECUTION_SHADOW_MISSING = 'executionShadowMissing',
  REAL_DATA_SHADOW_COPY_MISSING = 'realDataShadowCopyMissing',
  READ_ONLY_TRANSPORT_PROBE_MISSING = 'readOnlyTransportProbeMissing',
  ROLLBACK_PLAN_MISSING = 'rollbackPlanMissing',
  PRODUCTION_APPLY_PORT_MISSING = 'productionApplyPortMissing',
  LEGACY_FALLBACK_MISSING = 'legacyFallbackMissing',
  READ_SIDE_PARALLEL_DIVERGENT = 'readSideParallelDivergent'
}

// ── Stage Observation Window ──

export class CanonicalLibraryMetadataStageObservationWindow {
  observationWindowID: string
  complete: boolean

  constructor(observationWindowID: string, complete: boolean) {
    this.observationWindowID = CanonicalProductionRedaction.safeIdentifier(
      observationWindowID, 'stage-observation')
    this.complete = complete
  }
}

// ── Stage Evidence Report ──

export class CanonicalLibraryMetadataStageEvidenceReport {
  previousStage: CanonicalLibraryMetadataCanaryStage | null
  requestedStage: CanonicalLibraryMetadataCanaryStage
  previousStageSuccessCount: number
  previousStageFailureCount: number
  previousStageRollbackFailureCount: number
  previousStageBlockingDivergenceCount: number
  previousStageResourceMoveAttemptCount: number
  previousStageObjectIDInstabilityCount: number
  previousStageHierarchyCycleCount: number
  previousStageSuppressedLegacyDuplicateCount: number
  unresolvedConflictCount: number
  postconditionFailureCount: number
  unsupportedObjectCount: number
  dryRunEquivalenceStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus
  executionShadowStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus
  realDataShadowCopyStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus
  readOnlyTransportProbeStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus
  rollbackPlanStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus
  productionApplyPortStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus
  legacyFallbackStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus
  readSideParallelStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus
  observationWindow: CanonicalLibraryMetadataStageObservationWindow
  blockers: CanonicalLibraryMetadataStageEvidenceBlocker[]
  redacted: boolean

  constructor(
    previousStage: CanonicalLibraryMetadataCanaryStage | null,
    requestedStage: CanonicalLibraryMetadataCanaryStage,
    previousStageSuccessCount: number = 0,
    previousStageFailureCount: number = 0,
    previousStageRollbackFailureCount: number = 0,
    previousStageBlockingDivergenceCount: number = 0,
    previousStageResourceMoveAttemptCount: number = 0,
    previousStageObjectIDInstabilityCount: number = 0,
    previousStageHierarchyCycleCount: number = 0,
    previousStageSuppressedLegacyDuplicateCount: number = 0,
    unresolvedConflictCount: number = 0,
    postconditionFailureCount: number = 0,
    unsupportedObjectCount: number = 0,
    dryRunEquivalenceStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
    executionShadowStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
    realDataShadowCopyStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
    readOnlyTransportProbeStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
    rollbackPlanStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
    productionApplyPortStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
    legacyFallbackStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
    readSideParallelStatus: CanonicalLibraryMetadataStageEvidenceProbeStatus = CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
    observationWindow: CanonicalLibraryMetadataStageObservationWindow = new CanonicalLibraryMetadataStageObservationWindow('stage-observation', false),
    blockers: CanonicalLibraryMetadataStageEvidenceBlocker[] = [],
    redacted: boolean = true
  ) {
    this.previousStage = previousStage
    this.requestedStage = requestedStage
    this.previousStageSuccessCount = Math.max(0, previousStageSuccessCount)
    this.previousStageFailureCount = Math.max(0, previousStageFailureCount)
    this.previousStageRollbackFailureCount = Math.max(0, previousStageRollbackFailureCount)
    this.previousStageBlockingDivergenceCount = Math.max(0, previousStageBlockingDivergenceCount)
    this.previousStageResourceMoveAttemptCount = Math.max(0, previousStageResourceMoveAttemptCount)
    this.previousStageObjectIDInstabilityCount = Math.max(0, previousStageObjectIDInstabilityCount)
    this.previousStageHierarchyCycleCount = Math.max(0, previousStageHierarchyCycleCount)
    this.previousStageSuppressedLegacyDuplicateCount = Math.max(0, previousStageSuppressedLegacyDuplicateCount)
    this.unresolvedConflictCount = Math.max(0, unresolvedConflictCount)
    this.postconditionFailureCount = Math.max(0, postconditionFailureCount)
    this.unsupportedObjectCount = Math.max(0, unsupportedObjectCount)
    this.dryRunEquivalenceStatus = dryRunEquivalenceStatus
    this.executionShadowStatus = executionShadowStatus
    this.realDataShadowCopyStatus = realDataShadowCopyStatus
    this.readOnlyTransportProbeStatus = readOnlyTransportProbeStatus
    this.rollbackPlanStatus = rollbackPlanStatus
    this.productionApplyPortStatus = productionApplyPortStatus
    this.legacyFallbackStatus = legacyFallbackStatus
    this.readSideParallelStatus = readSideParallelStatus
    this.observationWindow = observationWindow
    this.blockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
    this.redacted = redacted
  }

  static from(
    evidence: CanonicalLibraryMetadataCutoverEvidence,
    policy: CanonicalLibraryMetadataCanaryStagePolicy
  ): CanonicalLibraryMetadataStageEvidenceReport {
    const stageEvidence = evidence.canaryStageEvidence
    const expectedPreviousStage = canonicalPreviousStage(policy.requestedStage)
    const blockers: CanonicalLibraryMetadataStageEvidenceBlocker[] = []
    if (!stageEvidence) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.MISSING_PREVIOUS_STAGE_EVIDENCE)
    }
    if (stageEvidence?.observationWindowComplete !== true) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.OBSERVATION_WINDOW_INCOMPLETE)
    }
    if ((stageEvidence?.failedCommitCount ?? 0) > 0) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.PREVIOUS_STAGE_FAILED)
    }
    if ((stageEvidence?.rollbackFailureCount ?? 0) > 0) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.ROLLBACK_FAILED)
    }
    if ((stageEvidence?.blockingDivergenceCount ?? 0) > 0 || !evidence.noBlockingDivergence) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.BLOCKING_DIVERGENCE)
    }
    if ((stageEvidence?.unresolvedConflictCount ?? 0) > 0 || !evidence.noUnresolvedConflict) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.UNRESOLVED_CONFLICT)
    }
    if ((stageEvidence?.postconditionFailureCount ?? 0) > 0) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.POSTCONDITION_FAILED)
    }
    if ((stageEvidence?.unsupportedObjectCount ?? 0) > 0) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.UNSUPPORTED_OBJECT)
    }
    if ((stageEvidence?.resourceMoveAttemptCount ?? 0) > 0) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.RESOURCE_MOVE_ATTEMPTED)
    }
    if ((stageEvidence?.folderCycleCount ?? 0) > 0) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.HIERARCHY_CYCLE)
    }
    if ((stageEvidence?.objectIDInstabilityCount ?? 0) > 0) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.OBJECT_ID_INSTABILITY)
    }
    if (!evidence.noCommitEvidenceAvailable) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.MISSING_NO_COMMIT_EVIDENCE)
    }
    if (!evidence.dryRunEquivalenceVerified) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.DRY_RUN_EQUIVALENCE_MISSING)
    }
    if (!evidence.executionShadowVerified) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.EXECUTION_SHADOW_MISSING)
    }
    if (!evidence.realDataShadowCopyVerified) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.REAL_DATA_SHADOW_COPY_MISSING)
    }
    if (!evidence.metadataManifestRouteEvidenceAvailable) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.READ_ONLY_TRANSPORT_PROBE_MISSING)
    }
    if (!evidence.rollbackPlan || !evidence.rollbackCheckpointAvailable ||
      !evidence.rollbackVerified || !evidence.rollbackRehearsalPassed) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.ROLLBACK_PLAN_MISSING)
    }
    if (!evidence.productionPortAvailable || !evidence.realRootBoundApplyPortAvailable ||
      evidence.applyPortMode !== CanonicalLibraryMetadataApplyPortMode.TEST_ROOT_BOUND &&
      evidence.applyPortMode !== CanonicalLibraryMetadataApplyPortMode.PRODUCTION_ROOT_BOUND) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.PRODUCTION_APPLY_PORT_MISSING)
    }
    if (!evidence.legacyFallbackAvailable) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.LEGACY_FALLBACK_MISSING)
    }
    if ((stageEvidence?.readSideParallelDivergenceCount ?? 0) > 0 || !evidence.readSideParallelEquivalent) {
      blockers.push(CanonicalLibraryMetadataStageEvidenceBlocker.READ_SIDE_PARALLEL_DIVERGENT)
    }
    return new CanonicalLibraryMetadataStageEvidenceReport(
      expectedPreviousStage,
      policy.requestedStage,
      stageEvidence?.successfulCommitCount ?? 0,
      stageEvidence?.failedCommitCount ?? 0,
      stageEvidence?.rollbackFailureCount ?? 0,
      stageEvidence?.blockingDivergenceCount ?? 0,
      stageEvidence?.resourceMoveAttemptCount ?? 0,
      stageEvidence?.objectIDInstabilityCount ?? 0,
      stageEvidence?.folderCycleCount ?? 0,
      stageEvidence?.suppressedLegacyDuplicateCount ?? 0,
      stageEvidence?.unresolvedConflictCount ?? 0,
      stageEvidence?.postconditionFailureCount ?? 0,
      stageEvidence?.unsupportedObjectCount ?? 0,
      evidence.dryRunEquivalenceVerified ? CanonicalLibraryMetadataStageEvidenceProbeStatus.EQUIVALENT : CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
      evidence.executionShadowVerified ? CanonicalLibraryMetadataStageEvidenceProbeStatus.VERIFIED : CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
      evidence.realDataShadowCopyVerified ? CanonicalLibraryMetadataStageEvidenceProbeStatus.VERIFIED : CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
      evidence.metadataManifestRouteEvidenceAvailable ? CanonicalLibraryMetadataStageEvidenceProbeStatus.VERIFIED : CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
      blockers.includes(CanonicalLibraryMetadataStageEvidenceBlocker.ROLLBACK_PLAN_MISSING)
        ? CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING : CanonicalLibraryMetadataStageEvidenceProbeStatus.VERIFIED,
      blockers.includes(CanonicalLibraryMetadataStageEvidenceBlocker.PRODUCTION_APPLY_PORT_MISSING)
        ? CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING : CanonicalLibraryMetadataStageEvidenceProbeStatus.VERIFIED,
      evidence.legacyFallbackAvailable ? CanonicalLibraryMetadataStageEvidenceProbeStatus.VERIFIED : CanonicalLibraryMetadataStageEvidenceProbeStatus.MISSING,
      evidence.readSideParallelEquivalent ? CanonicalLibraryMetadataStageEvidenceProbeStatus.EQUIVALENT : CanonicalLibraryMetadataStageEvidenceProbeStatus.DIVERGENT,
      new CanonicalLibraryMetadataStageObservationWindow(
        stageEvidence?.observationWindowID ?? policy.requestedStage + '-observation',
        stageEvidence?.observationWindowComplete === true
      ),
      blockers,
      true
    )
  }

  get canaryStageEvidence(): CanonicalLibraryMetadataCanaryStageEvidence {
    return new CanonicalLibraryMetadataCanaryStageEvidence(
      this.previousStage ?? CanonicalLibraryMetadataCanaryStage.DISABLED,
      this.previousStage ? canonicalPreviousStage(this.previousStage) : null,
      this.blockers.length === 0 ? CanonicalLibraryMetadataStageEvidenceStatus.PASSED : CanonicalLibraryMetadataStageEvidenceStatus.BLOCKED,
      this.previousStageSuccessCount,
      this.previousStageFailureCount,
      this.previousStageRollbackFailureCount,
      this.previousStageBlockingDivergenceCount,
      this.unresolvedConflictCount,
      this.postconditionFailureCount,
      this.unsupportedObjectCount,
      this.previousStageResourceMoveAttemptCount,
      this.previousStageHierarchyCycleCount,
      this.previousStageObjectIDInstabilityCount,
      this.previousStageSuppressedLegacyDuplicateCount,
      this.readSideParallelStatus === CanonicalLibraryMetadataStageEvidenceProbeStatus.DIVERGENT ? 1 : 0,
      !this.blockers.includes(CanonicalLibraryMetadataStageEvidenceBlocker.MISSING_NO_COMMIT_EVIDENCE),
      this.observationWindow.complete,
      this.observationWindow.observationWindowID
    )
  }

  get diagnosticsSummary(): string {
    return [
      'previousStage=' + (this.previousStage ?? 'none'),
      'requestedStage=' + this.requestedStage,
      'successCount=' + this.previousStageSuccessCount,
      'failureCount=' + this.previousStageFailureCount,
      'rollbackFailureCount=' + this.previousStageRollbackFailureCount,
      'resourceMoveCount=' + this.previousStageResourceMoveAttemptCount,
      'objectIDInstabilityCount=' + this.previousStageObjectIDInstabilityCount,
      'hierarchyCycleCount=' + this.previousStageHierarchyCycleCount,
      'readSideParallel=' + this.readSideParallelStatus,
      'observationComplete=' + this.observationWindow.complete,
      'blockers=' + this.blockers.join('|'),
      'redacted=' + this.redacted
    ].join(',')
  }
}

// ── Canary Stage Gate ──

export class CanonicalLibraryMetadataCanaryStageGate {
  allowed: boolean
  selectedCandidateLimit: number
  failures: CanonicalLibraryMetadataCutoverFailure[]

  constructor(
    policy: CanonicalLibraryMetadataCanaryStagePolicy,
    evidence: CanonicalLibraryMetadataCutoverEvidence
  ) {
    const failures: CanonicalLibraryMetadataCutoverFailure[] = []
    if (policy.requestedStage === CanonicalLibraryMetadataCanaryStage.DISABLED) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.DISABLED)
    }
    if (policy.runtimeSwitchEnabled) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.RUNTIME_SWITCH_DENIED)
    }
    if (!policy.allowCandidateExecution) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.MISSING_INTERNAL_CANARY_CONFIGURATION)
    }
    const stageEvidence = evidence.canaryStageEvidence
    if (!stageEvidence) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.MISSING_CANARY_STAGE_EVIDENCE)
      this.failures = [...new Set(failures)].sort((a, b) => a.localeCompare(b))
      this.allowed = false
      this.selectedCandidateLimit = 0
      return
    }
    if (stageEvidence.status !== CanonicalLibraryMetadataStageEvidenceStatus.PASSED) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.CANARY_STAGE_BLOCKED)
    }
    if (stageEvidence.stage !== canonicalPreviousStage(policy.requestedStage)) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.CANARY_STAGE_ORDER_VIOLATION)
    }
    if (!stageEvidence.observationWindowComplete) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.OBSERVATION_WINDOW_INCOMPLETE)
    }
    if (stageEvidence.successfulCommitCount < canonicalMinPreviousSuccess(policy.requestedStage)) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.CANARY_STAGE_ORDER_VIOLATION)
    }
    if (stageEvidence.failedCommitCount > 0) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.PREVIOUS_STAGE_FAILURE)
    }
    if (stageEvidence.rollbackFailureCount > 0) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.PREVIOUS_STAGE_ROLLBACK_FAILURE)
    }
    if (stageEvidence.blockingDivergenceCount > 0) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.PREVIOUS_STAGE_BLOCKING_DIVERGENCE)
    }
    if (stageEvidence.unresolvedConflictCount > 0) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.PREVIOUS_STAGE_UNRESOLVED_CONFLICT)
    }
    if (stageEvidence.postconditionFailureCount > 0) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.PREVIOUS_STAGE_POSTCONDITION_FAILURE)
    }
    if (stageEvidence.unsupportedObjectCount > 0) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.PREVIOUS_STAGE_UNSUPPORTED_OBJECT)
    }
    if (stageEvidence.resourceMoveAttemptCount > 0) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.RESOURCE_MOVE_ATTEMPTED)
    }
    if (stageEvidence.folderCycleCount > 0) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.CYCLE_DETECTED)
    }
    if (stageEvidence.objectIDInstabilityCount > 0) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.OBJECT_ID_INSTABILITY)
    }
    if (!stageEvidence.noCommitEvidenceAvailable || !evidence.noCommitEvidenceAvailable) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.MISSING_NO_COMMIT_EVIDENCE)
    }
    if (stageEvidence.readSideParallelDivergenceCount > 0 || !evidence.readSideParallelEquivalent) {
      failures.push(CanonicalLibraryMetadataCutoverFailure.READ_SIDE_PARALLEL_DIVERGENCE)
    }
    this.failures = [...new Set(failures)].sort((a, b) => a.localeCompare(b))
    this.allowed = this.failures.length === 0
    this.selectedCandidateLimit = this.allowed ? canonicalCanaryBudget(policy.requestedStage) : 0
  }
}

function canonicalMinPreviousSuccess(stage: CanonicalLibraryMetadataCanaryStage): number {
  switch (stage) {
    case CanonicalLibraryMetadataCanaryStage.DISABLED:
    case CanonicalLibraryMetadataCanaryStage.N1:
      return 0
    case CanonicalLibraryMetadataCanaryStage.N3:
      return 1
    case CanonicalLibraryMetadataCanaryStage.N10:
      return 3
    case CanonicalLibraryMetadataCanaryStage.ALL_ELIGIBLE:
      return 10
  }
}

// ── Canary Policy ──

export class CanonicalLibraryMetadataCanaryPolicy {
  stagePolicy: CanonicalLibraryMetadataCanaryStagePolicy
  canaryMaxObjectsPerSyncRun: number
  allowsInternalN1Execution: boolean
  explicitInternalTestConfiguration: boolean
  runtimeSwitchEnabled: boolean
  allowAllEligible: boolean

  constructor(
    stagePolicy: CanonicalLibraryMetadataCanaryStagePolicy = CanonicalLibraryMetadataCanaryStagePolicy.DISABLED,
    canaryMaxObjectsPerSyncRun: number = 0,
    allowsInternalN1Execution: boolean = false,
    explicitInternalTestConfiguration: boolean = false,
    runtimeSwitchEnabled: boolean = false,
    allowAllEligible: boolean = false
  ) {
    this.stagePolicy = stagePolicy
    this.canaryMaxObjectsPerSyncRun = Math.max(0, canaryMaxObjectsPerSyncRun)
    this.allowsInternalN1Execution = allowsInternalN1Execution
    this.explicitInternalTestConfiguration = explicitInternalTestConfiguration
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.allowAllEligible = allowAllEligible
  }

  static readonly DISABLED = new CanonicalLibraryMetadataCanaryPolicy()
}

// ── Canary Candidate Safety ──

export enum CanonicalLibraryMetadataCanaryCandidateSafetyKind {
  FOLDER_RENAME_OR_COLOR_METADATA = 'folderRenameOrColorMetadata',
  STUDY_ITEM_TAGS_FILING_OR_FOLDER_MEMBERSHIP_METADATA = 'studyItemTagsFilingOrFolderMembershipMetadata',
  STANDALONE_NOTE_TITLE_TAGS_OR_FILING_METADATA = 'standaloneNoteTitleTagsOrFilingMetadata',
  BLOCKED = 'blocked'
}

// ── Canary Blocker ──

export enum CanonicalLibraryMetadataCanaryBlocker {
  DISABLED = 'disabled',
  UNSUPPORTED_MODE = 'unsupportedMode',
  CANARY_BUDGET_ZERO = 'canaryBudgetZero',
  MISSING_INTERNAL_CANARY_CONFIGURATION = 'missingInternalCanaryConfiguration',
  CANARY_BUDGET_ABOVE_ONE_DENIED = 'canaryBudgetAboveOneDenied',
  CANARY_STAGE_EVIDENCE_MISSING = 'canaryStageEvidenceMissing',
  CANARY_STAGE_BLOCKED = 'canaryStageBlocked',
  UNSUPPORTED_TRIGGER = 'unsupportedTrigger',
  UNSUPPORTED_ACTION = 'unsupportedAction',
  INSUFFICIENT_EVIDENCE = 'insufficientEvidence',
  MISSING_OWNER_APPROVAL = 'missingOwnerApproval',
  MATRIX_BLOCKED = 'matrixBlocked',
  ACTIVE_PILOT_NOT_LIBRARY_METADATA = 'activePilotNotLibraryMetadata',
  COMMIT_EXECUTOR_UNAVAILABLE = 'commitExecutorUnavailable',
  PEER_SNAPSHOT_UNAVAILABLE = 'peerSnapshotUnavailable',
  RUNTIME_SWITCH_DENIED = 'runtimeSwitchDenied',
  ALL_ELIGIBLE_DENIED = 'allEligibleDenied',
  DEFAULT_ENABLEMENT_DENIED = 'defaultEnablementDenied',
  READ_SIDE_PARALLEL_MISSING = 'readSideParallelMissing',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  NO_ROLLBACK_CHECKPOINT = 'noRollbackCheckpoint',
  REAL_APPLY_PORT_UNAVAILABLE = 'realApplyPortUnavailable',
  CONFLICT_DETECTED = 'conflictDetected',
  ACTIVE_VS_TOMBSTONE_CONFLICT = 'activeVsTombstoneConflict',
  RESOURCE_MOVE_ATTEMPTED = 'resourceMoveAttempted',
  FOLDER_HIERARCHY_MUTATION_UNSUPPORTED = 'folderHierarchyMutationUnsupported',
  PARENT_MISSING = 'parentMissing',
  CYCLE_DETECTED = 'cycleDetected',
  OBJECT_ID_INSTABILITY = 'objectIDInstability',
  ALREADY_ATTEMPTED_FAILED_CANDIDATE = 'alreadyAttemptedFailedCandidate',
  NO_ELIGIBLE_CANDIDATE = 'noEligibleCandidate'
}

// ── Canary Candidate ──

export class CanonicalLibraryMetadataCanaryCandidate {
  cutoverCandidate: CanonicalLibraryMetadataCutoverCandidate
  objectID: string
  objectKind: CanonicalObjectKind
  actionKind: CanonicalLibraryMetadataCutoverActionKind
  domain: CanonicalLibraryMetadataCutoverDomain
  metadataHashPrefix: string | null

  constructor(cutoverCandidate: CanonicalLibraryMetadataCutoverCandidate) {
    this.cutoverCandidate = cutoverCandidate
    this.objectID = CanonicalProductionRedaction.safeIdentifier(cutoverCandidate.objectID, 'library-object')
    this.objectKind = cutoverCandidate.objectKind
    this.actionKind = cutoverCandidate.cutoverActionKind
    this.domain = cutoverCandidate.domain
    this.metadataHashPrefix = cutoverCandidate.expectedMetadataHash
      ? CanonicalProductionRedaction.hashPrefix(cutoverCandidate.expectedMetadataHash.value)
      : null
  }

  get id(): string {
    return this.cutoverCandidate.action.actionID
  }
}

// ── Canary Selection Blocker / Result ──

export class CanonicalLibraryMetadataCanarySelectionBlocker {
  objectID: string | null
  reason: CanonicalLibraryMetadataCanaryBlocker

  constructor(objectID: string | null, reason: CanonicalLibraryMetadataCanaryBlocker) {
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'library-object') : null
    this.reason = reason
  }

  get id(): string {
    return [(this.objectID ?? 'run'), this.reason].join('|')
  }
}

export class CanonicalLibraryMetadataCanarySelectionResult {
  selectedCandidates: CanonicalLibraryMetadataCanaryCandidate[]
  blockers: CanonicalLibraryMetadataCanarySelectionBlocker[]
  evaluatedCandidateCount: number
  noEligibleCandidate: boolean

  constructor(
    selectedCandidates: CanonicalLibraryMetadataCanaryCandidate[] = [],
    blockers: CanonicalLibraryMetadataCanarySelectionBlocker[] = [],
    evaluatedCandidateCount: number = 0,
    noEligibleCandidate: boolean = false
  ) {
    this.selectedCandidates = selectedCandidates
    this.blockers = blockers
    this.evaluatedCandidateCount = evaluatedCandidateCount
    this.noEligibleCandidate = noEligibleCandidate
  }

  get selectedCutoverCandidates(): CanonicalLibraryMetadataCutoverCandidate[] {
    return this.selectedCandidates.map(c => c.cutoverCandidate)
  }
}

// ── Canary Candidate Safety ──

export class CanonicalLibraryMetadataCanaryCandidateSafety {
  candidate: CanonicalLibraryMetadataCanaryCandidate
  safe: boolean
  kind: CanonicalLibraryMetadataCanaryCandidateSafetyKind
  blockers: CanonicalLibraryMetadataCanaryBlocker[]
  metadataOnly: boolean
  resourceMoveAttempted: boolean
  physicalDeleteAttempted: boolean
  contentBytesMutated: boolean

  constructor(
    candidate: CanonicalLibraryMetadataCutoverCandidate,
    evidence: CanonicalLibraryMetadataCutoverEvidence,
    attemptedFailedActionIDs: Set<string> = new Set()
  ) {
    const blockers = CanonicalLibraryMetadataCanarySelector.candidateBlockers(
      candidate, evidence, attemptedFailedActionIDs)
    let metadataKind: CanonicalLibraryMetadataCanaryCandidateSafetyKind
    switch (candidate.objectKind) {
      case CanonicalObjectKind.FOLDER:
        if (candidate.folderHierarchyMutationAttempted) {
          blockers.push(CanonicalLibraryMetadataCanaryBlocker.FOLDER_HIERARCHY_MUTATION_UNSUPPORTED)
        }
        metadataKind = CanonicalLibraryMetadataCanaryCandidateSafetyKind.FOLDER_RENAME_OR_COLOR_METADATA
        break
      case CanonicalObjectKind.STANDALONE_NOTE:
        metadataKind = CanonicalLibraryMetadataCanaryCandidateSafetyKind.STANDALONE_NOTE_TITLE_TAGS_OR_FILING_METADATA
        break
      case CanonicalObjectKind.STANDALONE_STUDY_ITEM:
      case CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM:
        metadataKind = CanonicalLibraryMetadataCanaryCandidateSafetyKind.STUDY_ITEM_TAGS_FILING_OR_FOLDER_MEMBERSHIP_METADATA
        break
      default:
        blockers.push(CanonicalLibraryMetadataCanaryBlocker.UNSUPPORTED_ACTION)
        metadataKind = CanonicalLibraryMetadataCanaryCandidateSafetyKind.BLOCKED
        break
    }
    if (candidate.expectedObject?.isDeleted === true) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.ACTIVE_VS_TOMBSTONE_CONFLICT)
    }
    this.candidate = new CanonicalLibraryMetadataCanaryCandidate(candidate)
    this.blockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
    this.safe = this.blockers.length === 0
    this.kind = this.safe ? metadataKind : CanonicalLibraryMetadataCanaryCandidateSafetyKind.BLOCKED
    this.metadataOnly = true
    this.resourceMoveAttempted = candidate.hasResourceMoveAttempt
    this.physicalDeleteAttempted = candidate.expectedObject?.isDeleted === true
    this.contentBytesMutated = false
  }
}

// ── Canary Selector ──

export class CanonicalLibraryMetadataCanarySelector {
  constructor() {}

  select(
    mode: CanonicalCutoverMode,
    policy: CanonicalLibraryMetadataCanaryPolicy,
    trigger: CanonicalSyncPlanTrigger,
    evidence: CanonicalLibraryMetadataCutoverEvidence,
    candidates: CanonicalLibraryMetadataCutoverCandidate[],
    attemptedFailedActionIDs: Set<string> = new Set()
  ): CanonicalLibraryMetadataCanarySelectionResult {
    const blockers: CanonicalLibraryMetadataCanarySelectionBlocker[] = []
    const usesStagePolicy = policy.stagePolicy.requestedStage !== CanonicalLibraryMetadataCanaryStage.DISABLED
    const stageGate = usesStagePolicy
      ? new CanonicalLibraryMetadataCanaryStageGate(policy.stagePolicy, evidence)
      : null

    if (mode === CanonicalCutoverMode.DISABLED) {
      blockers.push(new CanonicalLibraryMetadataCanarySelectionBlocker(
        null, CanonicalLibraryMetadataCanaryBlocker.DISABLED))
    }
    if (mode !== CanonicalCutoverMode.CANARY) {
      blockers.push(new CanonicalLibraryMetadataCanarySelectionBlocker(
        null, CanonicalLibraryMetadataCanaryBlocker.UNSUPPORTED_MODE))
    }
    if (policy.canaryMaxObjectsPerSyncRun === 0 && !usesStagePolicy) {
      blockers.push(new CanonicalLibraryMetadataCanarySelectionBlocker(
        null, CanonicalLibraryMetadataCanaryBlocker.CANARY_BUDGET_ZERO))
    }
    if (policy.canaryMaxObjectsPerSyncRun > 1 && !usesStagePolicy) {
      blockers.push(new CanonicalLibraryMetadataCanarySelectionBlocker(
        null, CanonicalLibraryMetadataCanaryBlocker.CANARY_BUDGET_ABOVE_ONE_DENIED))
    }
    if (policy.canaryMaxObjectsPerSyncRun === 1 && !usesStagePolicy && !policy.allowsInternalN1Execution) {
      blockers.push(new CanonicalLibraryMetadataCanarySelectionBlocker(
        null, CanonicalLibraryMetadataCanaryBlocker.MISSING_INTERNAL_CANARY_CONFIGURATION))
    }
    if (usesStagePolicy && stageGate?.allowed !== true) {
      blockers.push(new CanonicalLibraryMetadataCanarySelectionBlocker(
        null, evidence.canaryStageEvidence === null
          ? CanonicalLibraryMetadataCanaryBlocker.CANARY_STAGE_EVIDENCE_MISSING
          : CanonicalLibraryMetadataCanaryBlocker.CANARY_STAGE_BLOCKED))
    }
    if (trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH ||
      trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) {
      blockers.push(new CanonicalLibraryMetadataCanarySelectionBlocker(
        null, CanonicalLibraryMetadataCanaryBlocker.UNSUPPORTED_TRIGGER))
    }

    const runBlocked = blockers.length > 0
    const selectionLimit = usesStagePolicy ? (stageGate?.selectedCandidateLimit ?? 0) : policy.canaryMaxObjectsPerSyncRun
    const ordered = [...candidates].sort((a, b) => {
      if (a.objectKind !== b.objectKind) {
        return a.objectKind.localeCompare(b.objectKind)
      }
      if (a.objectID !== b.objectID) {
        return a.objectID.localeCompare(b.objectID)
      }
      return a.action.actionID.localeCompare(b.action.actionID)
    })
    const cycleDetected = CanonicalLibraryMetadataCutoverCandidate.folderHierarchyCycleDetected(candidates)
    const selected: CanonicalLibraryMetadataCanaryCandidate[] = []
    for (const candidate of ordered) {
      const reasons = CanonicalLibraryMetadataCanarySelector.candidateBlockers(
        candidate, evidence, attemptedFailedActionIDs)
      if (cycleDetected && candidate.objectKind === CanonicalObjectKind.FOLDER) {
        reasons.push(CanonicalLibraryMetadataCanaryBlocker.CYCLE_DETECTED)
      }
      if (reasons.length === 0 && !runBlocked && selected.length < selectionLimit) {
        selected.push(new CanonicalLibraryMetadataCanaryCandidate(candidate))
      } else {
        for (const reason of reasons) {
          blockers.push(new CanonicalLibraryMetadataCanarySelectionBlocker(candidate.objectID, reason))
        }
      }
    }
    if (selected.length === 0) {
      blockers.push(new CanonicalLibraryMetadataCanarySelectionBlocker(
        null, CanonicalLibraryMetadataCanaryBlocker.NO_ELIGIBLE_CANDIDATE))
    }
    return new CanonicalLibraryMetadataCanarySelectionResult(
      selected, blockers, candidates.length, selected.length === 0)
  }

  static candidateBlockers(
    candidate: CanonicalLibraryMetadataCutoverCandidate,
    evidence: CanonicalLibraryMetadataCutoverEvidence,
    attemptedFailedActionIDs: Set<string>
  ): CanonicalLibraryMetadataCanaryBlocker[] {
    const blockers: CanonicalLibraryMetadataCanaryBlocker[] = []
    const cak = candidate.cutoverActionKind
    if (cak === CanonicalLibraryMetadataCutoverActionKind.CONFLICT_RECORD) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.CONFLICT_DETECTED)
    } else if (cak === CanonicalLibraryMetadataCutoverActionKind.UNSUPPORTED ||
      cak === CanonicalLibraryMetadataCutoverActionKind.TOMBSTONE_MARKER_UNSUPPORTED_FOR_THIS_ROUND) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.UNSUPPORTED_ACTION)
    }
    if (candidate.unresolvedConflict) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.UNRESOLVED_CONFLICT)
    }
    if (candidate.hasActiveVsTombstoneConflict) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.ACTIVE_VS_TOMBSTONE_CONFLICT)
    }
    if (!candidate.rollbackCheckpointID) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.NO_ROLLBACK_CHECKPOINT)
    }
    if (!evidence.realRootBoundApplyPortAvailable ||
      (evidence.applyPortMode !== CanonicalLibraryMetadataApplyPortMode.TEST_ROOT_BOUND &&
       evidence.applyPortMode !== CanonicalLibraryMetadataApplyPortMode.PRODUCTION_ROOT_BOUND)) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.REAL_APPLY_PORT_UNAVAILABLE)
    }
    if (candidate.hasResourceMoveAttempt) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.RESOURCE_MOVE_ATTEMPTED)
    }
    if (candidate.folderHierarchyMutationAttempted) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.FOLDER_HIERARCHY_MUTATION_UNSUPPORTED)
    }
    if (candidate.parentMissingKnown) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.PARENT_MISSING)
    }
    if (candidate.expectedObject?.isDeleted === true) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.ACTIVE_VS_TOMBSTONE_CONFLICT)
    }
    if (candidate.hasObjectIDInstability) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.OBJECT_ID_INSTABILITY)
    }
    if (attemptedFailedActionIDs.has(candidate.action.actionID)) {
      blockers.push(CanonicalLibraryMetadataCanaryBlocker.ALREADY_ATTEMPTED_FAILED_CANDIDATE)
    }
    return [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
  }
}

// ── Cutover Gate ──

export class CanonicalLibraryMetadataCutoverGate {
  mode: CanonicalCutoverMode
  allowed: boolean
  failures: CanonicalLibraryMetadataCutoverFailure[]
  legacyFallbackAvailable: boolean
  reason: string

  constructor(
    mode: CanonicalCutoverMode,
    failures: CanonicalLibraryMetadataCutoverFailure[],
    legacyFallbackAvailable: boolean,
    reason: string
  ) {
    this.mode = mode
    this.failures = [...new Set(failures)].sort((a, b) => a.localeCompare(b))
    this.allowed = this.failures.length === 0
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? (this.allowed ? 'allowed' : 'blocked')
  }
}

// ── Production Commit Result ──

export class CanonicalProductionSideEffect {
  kind: string
  domain?: CanonicalProductionDomain

  constructor(kind: string, domain?: CanonicalProductionDomain) {
    this.kind = kind
    this.domain = domain
  }
}

export class CanonicalLibraryMetadataProductionCommitResult {
  actionID: string
  objectID: string
  objectKind: CanonicalObjectKind
  domain: CanonicalLibraryMetadataCutoverDomain
  actionKind: CanonicalLibraryMetadataCutoverActionKind
  committed: boolean
  partialCommit: boolean
  preconditionVerified: boolean
  postconditionVerified: boolean
  routePath: string | null
  metadataHashPrefix: string | null
  parentSummary: string
  tagCount: number
  filingSummary: string
  payloadByteCount: number
  sideEffect: CanonicalProductionSideEffect | null
  sideEffects: CanonicalProductionSideEffect[]
  failureKind: CanonicalLibraryMetadataCutoverFailure | null
  reason: string

  constructor(
    actionID: string,
    objectID: string,
    objectKind: CanonicalObjectKind,
    domain: CanonicalLibraryMetadataCutoverDomain,
    actionKind: CanonicalLibraryMetadataCutoverActionKind,
    committed: boolean,
    partialCommit: boolean = false,
    preconditionVerified: boolean = true,
    postconditionVerified: boolean = true,
    routePath: string | null = '/sync/apply-metadata',
    metadataHash: CanonicalHash | null = null,
    metadataHashPrefix: string | null = null,
    parentSummary: string = 'parent=none',
    tagCount: number = 0,
    filingSummary: string = 'none',
    payloadByteCount: number = 0,
    sideEffect: CanonicalProductionSideEffect | null = null,
    sideEffects: CanonicalProductionSideEffect[] | null = null,
    failureKind: CanonicalLibraryMetadataCutoverFailure | null = null,
    reason: string
  ) {
    this.actionID = CanonicalProductionRedaction.safeIdentifier(actionID, actionKind)
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'library-object')
    this.objectKind = objectKind
    this.domain = domain
    this.actionKind = actionKind
    this.committed = committed
    this.partialCommit = partialCommit
    this.preconditionVerified = preconditionVerified
    this.postconditionVerified = postconditionVerified
    this.routePath = CanonicalProductionRedaction.safeDiagnosticText(routePath)
    this.metadataHashPrefix = metadataHash
      ? CanonicalProductionRedaction.hashPrefix(metadataHash.value)
      : CanonicalProductionRedaction.hashPrefix(metadataHashPrefix)
    this.parentSummary = CanonicalProductionRedaction.safeDiagnosticText(parentSummary) ?? 'parent=none'
    this.tagCount = Math.max(0, tagCount)
    this.filingSummary = CanonicalProductionRedaction.safeDiagnosticText(filingSummary) ?? 'none'
    this.payloadByteCount = Math.max(0, payloadByteCount)
    this.sideEffect = sideEffect
    this.sideEffects = sideEffects ?? (sideEffect ? [sideEffect] : [])
    this.failureKind = failureKind
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? (committed ? 'committed' : 'failed')
  }
}

// ── Rollback Execution Result ──

export class CanonicalLibraryMetadataRollbackExecutionResult {
  checkpointID: string
  succeeded: boolean
  fatal: boolean
  reason: string
  rollbackResult: CanonicalRollbackResult | null

  constructor(
    checkpointID: string,
    succeeded: boolean,
    fatal: boolean = false,
    reason: string,
    rollbackResult: CanonicalRollbackResult | null = null
  ) {
    this.checkpointID = CanonicalProductionRedaction.safeIdentifier(checkpointID, 'library-metadata-checkpoint')
    this.succeeded = succeeded
    this.fatal = fatal
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? (succeeded ? 'rollbackCompleted' : 'rollbackFailed')
    this.rollbackResult = rollbackResult
  }
}

// ── Read Side Parallel Projection Result ──

export class CanonicalLibraryMetadataReadSideParallelProjectionResult {
  objectID: string
  objectKind: CanonicalObjectKind
  domain: CanonicalLibraryMetadataCutoverDomain
  equivalent: boolean
  mutatedUI: boolean
  canonicalHashPrefix: string | null
  legacyHashPrefix: string | null
  parentSummary: string
  tagCount: number
  filingSummary: string
  noResourceFileMove: boolean
  syncOrUploadTriggered: boolean
  reason: string

  constructor(
    candidate: CanonicalLibraryMetadataCutoverCandidate,
    equivalent: boolean,
    legacyHash: CanonicalHash | null,
    reason: string
  ) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(candidate.objectID, 'library-object')
    this.objectKind = candidate.objectKind
    this.domain = candidate.domain
    this.equivalent = equivalent
    this.mutatedUI = false
    this.canonicalHashPrefix = candidate.expectedMetadataHash
      ? CanonicalProductionRedaction.hashPrefix(candidate.expectedMetadataHash.value)
      : null
    this.legacyHashPrefix = legacyHash
      ? CanonicalProductionRedaction.hashPrefix(legacyHash.value)
      : null
    this.parentSummary = candidate.parentSummary
    this.tagCount = candidate.tagCount
    this.filingSummary = candidate.filingSummary
    this.noResourceFileMove = true
    this.syncOrUploadTriggered = false
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? (equivalent ? 'equivalent' : 'divergent')
  }
}

// ── Cutover Result ──

export class CanonicalLibraryMetadataCutoverResult {
  gate: CanonicalLibraryMetadataCutoverGate
  commits: CanonicalLibraryMetadataProductionCommitResult[]
  rollbackResults: CanonicalLibraryMetadataRollbackExecutionResult[]
  diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[]
  legacyFallbackUsed: boolean
  duplicateLegacySuppressedActionIDs: string[]
  canaryAttemptedCount: number
  canarySucceeded: boolean
  fatalBlocker: boolean
  readSideProjection: CanonicalLibraryMetadataReadSideParallelProjectionResult | null
  canaryConfiguration: CanonicalLibraryMetadataCanaryConfiguration | null
  canarySelection: CanonicalLibraryMetadataCanarySelectionResult | null
  candidateSafetyReports: CanonicalLibraryMetadataCanaryCandidateSafety[] | null
  observationReport: CanonicalLibraryMetadataCanaryObservationReport | null
  stageObservationReport: CanonicalLibraryMetadataCanaryStageObservationReport | null

  constructor(
    gate: CanonicalLibraryMetadataCutoverGate,
    commits: CanonicalLibraryMetadataProductionCommitResult[] = [],
    rollbackResults: CanonicalLibraryMetadataRollbackExecutionResult[] = [],
    diagnostics: CanonicalLibraryMetadataCutoverDiagnostic[] = [],
    legacyFallbackUsed: boolean = false,
    duplicateLegacySuppressedActionIDs: string[] = [],
    canaryAttemptedCount: number = 0,
    canarySucceeded: boolean = false,
    fatalBlocker: boolean = false,
    readSideProjection: CanonicalLibraryMetadataReadSideParallelProjectionResult | null = null,
    canaryConfiguration: CanonicalLibraryMetadataCanaryConfiguration | null = null,
    canarySelection: CanonicalLibraryMetadataCanarySelectionResult | null = null,
    candidateSafetyReports: CanonicalLibraryMetadataCanaryCandidateSafety[] | null = null,
    observationReport: CanonicalLibraryMetadataCanaryObservationReport | null = null,
    stageObservationReport: CanonicalLibraryMetadataCanaryStageObservationReport | null = null
  ) {
    this.gate = gate
    this.commits = commits
    this.rollbackResults = rollbackResults
    this.diagnostics = diagnostics
    this.legacyFallbackUsed = legacyFallbackUsed
    this.duplicateLegacySuppressedActionIDs = duplicateLegacySuppressedActionIDs
    this.canaryAttemptedCount = canaryAttemptedCount
    this.canarySucceeded = canarySucceeded
    this.fatalBlocker = fatalBlocker
    this.readSideProjection = readSideProjection
    this.canaryConfiguration = canaryConfiguration
    this.canarySelection = canarySelection
    this.candidateSafetyReports = candidateSafetyReports
    this.observationReport = observationReport
    this.stageObservationReport = stageObservationReport
  }

  get succeeded(): boolean {
    return this.gate.allowed && !this.fatalBlocker && this.commits.length > 0 &&
      this.commits.every(c => c.committed && c.preconditionVerified && c.postconditionVerified)
  }
}

// ── Forward declarations for types used before definition ──

export class CanonicalLibraryMetadataCanaryConfiguration {
  mode: string = 'disabled'
  domain: CanonicalMigrationDomain = CanonicalMigrationDomain.LIBRARY_METADATA
  canaryMaxObjectsPerSyncRun: number = 0
  explicitInternalTestConfiguration: boolean = false
  productionTokenRequired: boolean = true
  ownerApprovalRequired: boolean = true
  rollbackPlanRequired: boolean = true
  runtimeSwitchEnabled: boolean = false
  allowAllEligible: boolean = false
  releaseDefaultEnabled: boolean = false

  static readonly DISABLED = new CanonicalLibraryMetadataCanaryConfiguration()
}

export enum CanonicalLibraryMetadataCanaryObservationStatus {
  DISABLED = 'disabled',
  BLOCKED = 'blocked',
  NO_ELIGIBLE_CANDIDATE = 'noEligibleCandidate',
  COMMITTED = 'committed',
  FAILED_ROLLED_BACK = 'failedRolledBack',
  FATAL_ROLLBACK_FAILURE = 'fatalRollbackFailure'
}

export class CanonicalLibraryMetadataCanaryObservationReport {
  status: CanonicalLibraryMetadataCanaryObservationStatus = CanonicalLibraryMetadataCanaryObservationStatus.DISABLED
  syncRunID: string | null = null
  nodeRole: CanonicalProductionExecutionDomainRole = CanonicalProductionExecutionDomainRole.TEST_HARNESS
  selectedCandidateCount: number = 0
  blockedCandidateCount: number = 0
  attemptedCommitCount: number = 0
  successfulCommitCount: number = 0
  rollbackCount: number = 0
  duplicateSuppressionApplied: boolean = false
  legacyFallbackPreserved: boolean = true
  readSideParallelEquivalent: boolean = false
  uiMutated: boolean = false
  resourceMoved: boolean = false
  physicalDeleteAttempted: boolean = false
  contentBytesMutated: boolean = false
  fatalBlocker: boolean = false
  reason: string = ''

  constructor(
    status: CanonicalLibraryMetadataCanaryObservationStatus,
    syncRunID: string | null = null,
    nodeRole: CanonicalProductionExecutionDomainRole = CanonicalProductionExecutionDomainRole.TEST_HARNESS,
    selectedCandidateCount: number = 0,
    blockedCandidateCount: number = 0,
    attemptedCommitCount: number = 0,
    successfulCommitCount: number = 0,
    rollbackCount: number = 0,
    duplicateSuppressionApplied: boolean = false,
    legacyFallbackPreserved: boolean = true,
    readSideParallelEquivalent: boolean = false,
    uiMutated: boolean = false,
    resourceMoved: boolean = false,
    physicalDeleteAttempted: boolean = false,
    contentBytesMutated: boolean = false,
    fatalBlocker: boolean = false,
    reason: string = ''
  ) {
    this.status = status
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.nodeRole = nodeRole
    this.selectedCandidateCount = Math.max(0, selectedCandidateCount)
    this.blockedCandidateCount = Math.max(0, blockedCandidateCount)
    this.attemptedCommitCount = Math.max(0, attemptedCommitCount)
    this.successfulCommitCount = Math.max(0, successfulCommitCount)
    this.rollbackCount = Math.max(0, rollbackCount)
    this.duplicateSuppressionApplied = duplicateSuppressionApplied
    this.legacyFallbackPreserved = legacyFallbackPreserved
    this.readSideParallelEquivalent = readSideParallelEquivalent
    this.uiMutated = uiMutated
    this.resourceMoved = resourceMoved
    this.physicalDeleteAttempted = physicalDeleteAttempted
    this.contentBytesMutated = contentBytesMutated
    this.fatalBlocker = fatalBlocker
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? status
  }
}

export enum CanonicalLibraryMetadataCanaryStageRecommendation {
  STAY_DISABLED = 'stayDisabled',
  OBSERVE_CURRENT_STAGE = 'observeCurrentStage',
  ADVANCE_TO_N3 = 'advanceToN3',
  ADVANCE_TO_N10 = 'advanceToN10',
  ADVANCE_TO_ALL_ELIGIBLE = 'advanceToAllEligible',
  HOLD_FOR_INVESTIGATION = 'holdForInvestigation',
  STOP_FOR_FATAL_BLOCKER = 'stopForFatalBlocker'
}

export class CanonicalLibraryMetadataCanaryStageFailure {
  objectID: string | null
  blocker: CanonicalLibraryMetadataCanaryBlocker

  constructor(objectID: string | null, blocker: CanonicalLibraryMetadataCanaryBlocker) {
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'library-object') : null
    this.blocker = blocker
  }

  get id(): string {
    return [(this.objectID ?? 'run'), this.blocker].join('|')
  }
}

export class CanonicalLibraryMetadataCanaryStageObservationReport {
  stage: CanonicalLibraryMetadataCanaryStage = CanonicalLibraryMetadataCanaryStage.DISABLED
  budget: number = 0
  selectedCount: number = 0
  executedCount: number = 0
  successCount: number = 0
  failureCount: number = 0
  rollbackCount: number = 0
  rollbackFailureCount: number = 0
  legacyFallbackCount: number = 0
  duplicateSuppressionCount: number = 0
  skippedCount: number = 0
  noEligibleCount: number = 0
  unsafeCandidateSkippedCount: number = 0
  resourceMoveBlockedCount: number = 0
  hierarchyCycleBlockedCount: number = 0
  objectIDInstabilityBlockedCount: number = 0
  fatalBlockerCount: number = 0
  readSideParallelEquivalentCount: number = 0
  readSideParallelDivergentCount: number = 0
  nextStageEligible: boolean = false
  nextStageBlockers: CanonicalLibraryMetadataCutoverFailure[] = []
  recommendation: CanonicalLibraryMetadataCanaryStageRecommendation = CanonicalLibraryMetadataCanaryStageRecommendation.STAY_DISABLED
  runtimeSwitchEnabled: boolean = false
  domain: CanonicalMigrationDomain = CanonicalMigrationDomain.LIBRARY_METADATA
  uiMutated: boolean = false
  resourceMoved: boolean = false
  uploadJobCreated: boolean = false
  evidenceReport: CanonicalLibraryMetadataStageEvidenceReport | null = null
  redacted: boolean = true
  failures: CanonicalLibraryMetadataCanaryStageFailure[] = []

  constructor(
    stage: CanonicalLibraryMetadataCanaryStage,
    budget: number,
    selectedCount: number = 0,
    executedCount: number = 0,
    successCount: number = 0,
    failureCount: number = 0,
    rollbackCount: number = 0,
    rollbackFailureCount: number = 0,
    legacyFallbackCount: number = 0,
    duplicateSuppressionCount: number = 0,
    skippedCount: number = 0,
    noEligibleCount: number = 0,
    unsafeCandidateSkippedCount: number = 0,
    resourceMoveBlockedCount: number = 0,
    hierarchyCycleBlockedCount: number = 0,
    objectIDInstabilityBlockedCount: number = 0,
    fatalBlockerCount: number = 0,
    readSideParallelEquivalentCount: number = 0,
    readSideParallelDivergentCount: number = 0,
    nextStageEligible: boolean = false,
    nextStageBlockers: CanonicalLibraryMetadataCutoverFailure[] = [],
    recommendation: CanonicalLibraryMetadataCanaryStageRecommendation = CanonicalLibraryMetadataCanaryStageRecommendation.STAY_DISABLED,
    runtimeSwitchEnabled: boolean = false,
    failures: CanonicalLibraryMetadataCanaryStageFailure[] = [],
    evidenceReport: CanonicalLibraryMetadataStageEvidenceReport | null = null,
    redacted: boolean = true
  ) {
    this.stage = stage
    this.budget = Math.max(0, budget)
    this.selectedCount = Math.max(0, selectedCount)
    this.executedCount = Math.max(0, executedCount)
    this.successCount = Math.max(0, successCount)
    this.failureCount = Math.max(0, failureCount)
    this.rollbackCount = Math.max(0, rollbackCount)
    this.rollbackFailureCount = Math.max(0, rollbackFailureCount)
    this.legacyFallbackCount = Math.max(0, legacyFallbackCount)
    this.duplicateSuppressionCount = Math.max(0, duplicateSuppressionCount)
    this.skippedCount = Math.max(0, skippedCount)
    this.noEligibleCount = Math.max(0, noEligibleCount)
    this.unsafeCandidateSkippedCount = Math.max(0, unsafeCandidateSkippedCount)
    this.resourceMoveBlockedCount = Math.max(0, resourceMoveBlockedCount)
    this.hierarchyCycleBlockedCount = Math.max(0, hierarchyCycleBlockedCount)
    this.objectIDInstabilityBlockedCount = Math.max(0, objectIDInstabilityBlockedCount)
    this.fatalBlockerCount = Math.max(0, fatalBlockerCount)
    this.readSideParallelEquivalentCount = Math.max(0, readSideParallelEquivalentCount)
    this.readSideParallelDivergentCount = Math.max(0, readSideParallelDivergentCount)
    this.nextStageEligible = nextStageEligible
    this.nextStageBlockers = [...new Set(nextStageBlockers)].sort((a, b) => a.localeCompare(b))
    this.recommendation = recommendation
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.domain = CanonicalMigrationDomain.LIBRARY_METADATA
    this.uiMutated = false
    this.resourceMoved = false
    this.uploadJobCreated = false
    this.failures = failures
    this.evidenceReport = evidenceReport
    this.redacted = redacted
  }
}

// ── Cutover Diagnostic Kind ──

export enum CanonicalLibraryMetadataCutoverDiagnosticKind {
  CANONICAL_LIBRARY_METADATA_CUTOVER_GATE_EVALUATED = 'canonicalLibraryMetadataCutoverGateEvaluated',
  CANONICAL_LIBRARY_METADATA_CUTOVER_GATE_BLOCKED = 'canonicalLibraryMetadataCutoverGateBlocked',
  CANONICAL_LIBRARY_METADATA_NO_COMMIT_STARTED = 'canonicalLibraryMetadataNoCommitStarted',
  CANONICAL_LIBRARY_METADATA_NO_COMMIT_COMPLETED = 'canonicalLibraryMetadataNoCommitCompleted',
  CANONICAL_LIBRARY_METADATA_COMMIT_STARTED = 'canonicalLibraryMetadataCommitStarted',
  CANONICAL_LIBRARY_METADATA_COMMIT_COMPLETED = 'canonicalLibraryMetadataCommitCompleted',
  CANONICAL_LIBRARY_METADATA_COMMIT_FAILED = 'canonicalLibraryMetadataCommitFailed',
  CANONICAL_LIBRARY_METADATA_ROLLBACK_STARTED = 'canonicalLibraryMetadataRollbackStarted',
  CANONICAL_LIBRARY_METADATA_ROLLBACK_COMPLETED = 'canonicalLibraryMetadataRollbackCompleted',
  CANONICAL_LIBRARY_METADATA_ROLLBACK_FAILED = 'canonicalLibraryMetadataRollbackFailed',
  CANONICAL_LIBRARY_METADATA_CANARY_STARTED = 'canonicalLibraryMetadataCanaryStarted',
  CANONICAL_LIBRARY_METADATA_CANARY_COMPLETED = 'canonicalLibraryMetadataCanaryCompleted',
  CANONICAL_LIBRARY_METADATA_DUPLICATE_LEGACY_SUPPRESSED = 'canonicalLibraryMetadataDuplicateLegacySuppressed',
  CANONICAL_LIBRARY_METADATA_LEGACY_FALLBACK_USED = 'canonicalLibraryMetadataLegacyFallbackUsed',
  CANONICAL_LIBRARY_METADATA_RESOURCE_MOVE_BLOCKED = 'canonicalLibraryMetadataResourceMoveBlocked',
  CANONICAL_LIBRARY_METADATA_HIERARCHY_CYCLE_BLOCKED = 'canonicalLibraryMetadataHierarchyCycleBlocked',
  CANONICAL_LIBRARY_METADATA_OBJECT_ID_INSTABILITY_BLOCKED = 'canonicalLibraryMetadataObjectIDInstabilityBlocked',
  CANONICAL_LIBRARY_METADATA_UNSAFE_CANDIDATE_SKIPPED = 'canonicalLibraryMetadataUnsafeCandidateSkipped',
  CANONICAL_LIBRARY_METADATA_CYCLE_BLOCKED = 'canonicalLibraryMetadataCycleBlocked',
  CANONICAL_LIBRARY_METADATA_CONFLICT_BLOCKED = 'canonicalLibraryMetadataConflictBlocked',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_PARALLEL_EQUIVALENT = 'canonicalLibraryMetadataReadSideParallelEquivalent',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_PARALLEL_DIVERGENT = 'canonicalLibraryMetadataReadSideParallelDivergent',
  CANONICAL_LIBRARY_METADATA_UI_PROJECTION_PARALLEL_READ_STARTED = 'canonicalLibraryMetadataUIProjectionParallelReadStarted',
  CANONICAL_LIBRARY_METADATA_UI_PROJECTION_PARALLEL_READ_EQUIVALENT = 'canonicalLibraryMetadataUIProjectionParallelReadEquivalent',
  CANONICAL_LIBRARY_METADATA_UI_PROJECTION_PARALLEL_READ_DIVERGENT = 'canonicalLibraryMetadataUIProjectionParallelReadDivergent',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_EVALUATED = 'canonicalLibraryMetadataCanaryStageEvaluated',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_BLOCKED = 'canonicalLibraryMetadataCanaryStageBlocked',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_ALLOWED = 'canonicalLibraryMetadataCanaryStageAllowed',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_STARTED = 'canonicalLibraryMetadataCanaryStageStarted',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_COMPLETED = 'canonicalLibraryMetadataCanaryStageCompleted',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_FAILED = 'canonicalLibraryMetadataCanaryStageFailed',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_OBSERVATION_RECORDED = 'canonicalLibraryMetadataCanaryStageObservationRecorded',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_CANDIDATE_SKIPPED = 'canonicalLibraryMetadataCanaryStageCandidateSkipped',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_CANDIDATE_EXECUTED = 'canonicalLibraryMetadataCanaryStageCandidateExecuted',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_STOPPED_AFTER_FAILURE = 'canonicalLibraryMetadataCanaryStageStoppedAfterFailure',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_NEXT_STAGE_ELIGIBLE = 'canonicalLibraryMetadataCanaryStageNextStageEligible',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_NEXT_STAGE_BLOCKED = 'canonicalLibraryMetadataCanaryStageNextStageBlocked',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_ALL_ELIGIBLE_STARTED = 'canonicalLibraryMetadataCanaryStageAllEligibleStarted',
  CANONICAL_LIBRARY_METADATA_CANARY_STAGE_ALL_ELIGIBLE_COMPLETED = 'canonicalLibraryMetadataCanaryStageAllEligibleCompleted',
  CANONICAL_LIBRARY_METADATA_EXPANDED_READ_SIDE_PARALLEL_STARTED = 'canonicalLibraryMetadataExpandedReadSideParallelStarted',
  CANONICAL_LIBRARY_METADATA_EXPANDED_READ_SIDE_PARALLEL_EQUIVALENT = 'canonicalLibraryMetadataExpandedReadSideParallelEquivalent',
  CANONICAL_LIBRARY_METADATA_EXPANDED_READ_SIDE_PARALLEL_DIVERGENT = 'canonicalLibraryMetadataExpandedReadSideParallelDivergent',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_PARALLEL_STARTED = 'canonicalLibraryMetadataReadSideParallelStarted',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_PARALLEL_COMPLETED = 'canonicalLibraryMetadataReadSideParallelCompleted',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_EQUIVALENT = 'canonicalLibraryMetadataReadSideEquivalent',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_DIVERGENT = 'canonicalLibraryMetadataReadSideDivergent',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_UNSUPPORTED_OBJECT = 'canonicalLibraryMetadataReadSideUnsupportedObject',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_PATH_LEAK_BLOCKED = 'canonicalLibraryMetadataReadSidePathLeakBlocked',
  CANONICAL_LIBRARY_METADATA_N1_CANARY_CONFIGURED = 'canonicalLibraryMetadataN1CanaryConfigured',
  CANONICAL_LIBRARY_METADATA_N1_MAC_PEER_SNAPSHOT_UNAVAILABLE = 'canonicalLibraryMetadataN1MacPeerSnapshotUnavailable',
  CANONICAL_LIBRARY_METADATA_OBSERVATION_GATE_EVALUATED = 'canonicalLibraryMetadataObservationGateEvaluated',
  CANONICAL_LIBRARY_METADATA_OBSERVATION_GATE_BLOCKED = 'canonicalLibraryMetadataObservationGateBlocked',
  CANONICAL_LIBRARY_METADATA_OBSERVATION_GATE_READY = 'canonicalLibraryMetadataObservationGateReady',
  CANONICAL_LIBRARY_METADATA_OBSERVATION_WINDOW_STARTED = 'canonicalLibraryMetadataObservationWindowStarted',
  CANONICAL_LIBRARY_METADATA_OBSERVATION_WRITE_SIDE_RECORDED = 'canonicalLibraryMetadataObservationWriteSideRecorded',
  CANONICAL_LIBRARY_METADATA_OBSERVATION_READ_SIDE_RECORDED = 'canonicalLibraryMetadataObservationReadSideRecorded',
  CANONICAL_LIBRARY_METADATA_OBSERVATION_WINDOW_COMPLETED = 'canonicalLibraryMetadataObservationWindowCompleted',
  CANONICAL_LIBRARY_METADATA_RETIREMENT_CANDIDATE_EVALUATED = 'canonicalLibraryMetadataRetirementCandidateEvaluated',
  CANONICAL_LIBRARY_METADATA_RETIREMENT_CANDIDATE_BLOCKED = 'canonicalLibraryMetadataRetirementCandidateBlocked',
  CANONICAL_LIBRARY_METADATA_RETIREMENT_CANDIDATE_READY = 'canonicalLibraryMetadataRetirementCandidateReady',
  CANONICAL_LIBRARY_METADATA_RETIREMENT_CANDIDATE_GATE_EVALUATED = 'canonicalLibraryMetadataRetirementCandidateGateEvaluated',
  CANONICAL_LIBRARY_METADATA_RETIREMENT_CANDIDATE_GATE_BLOCKED = 'canonicalLibraryMetadataRetirementCandidateGateBlocked',
  CANONICAL_LIBRARY_METADATA_END_TO_END_PILOT_REPORT_GENERATED = 'canonicalLibraryMetadataEndToEndPilotReportGenerated',
  CANONICAL_LIBRARY_METADATA_LANDING_CONFIG_EVALUATED = 'canonicalLibraryMetadataLandingConfigEvaluated',
  CANONICAL_LIBRARY_METADATA_LANDING_DISABLED = 'canonicalLibraryMetadataLandingDisabled',
  CANONICAL_LIBRARY_METADATA_LANDING_ARMED = 'canonicalLibraryMetadataLandingArmed',
  CANONICAL_LIBRARY_METADATA_LANDING_BLOCKED = 'canonicalLibraryMetadataLandingBlocked',
  CANONICAL_LIBRARY_METADATA_LANDING_N1_STARTED = 'canonicalLibraryMetadataLandingN1Started',
  CANONICAL_LIBRARY_METADATA_LANDING_CANDIDATE_SELECTED = 'canonicalLibraryMetadataLandingCandidateSelected',
  CANONICAL_LIBRARY_METADATA_LANDING_NO_ELIGIBLE_CANDIDATE = 'canonicalLibraryMetadataLandingNoEligibleCandidate',
  CANONICAL_LIBRARY_METADATA_LANDING_COMMIT_STARTED = 'canonicalLibraryMetadataLandingCommitStarted',
  CANONICAL_LIBRARY_METADATA_LANDING_COMMIT_COMPLETED = 'canonicalLibraryMetadataLandingCommitCompleted',
  CANONICAL_LIBRARY_METADATA_LANDING_COMMIT_FAILED = 'canonicalLibraryMetadataLandingCommitFailed',
  CANONICAL_LIBRARY_METADATA_LANDING_ROLLBACK_STARTED = 'canonicalLibraryMetadataLandingRollbackStarted',
  CANONICAL_LIBRARY_METADATA_LANDING_ROLLBACK_COMPLETED = 'canonicalLibraryMetadataLandingRollbackCompleted',
  CANONICAL_LIBRARY_METADATA_LANDING_ROLLBACK_FAILED = 'canonicalLibraryMetadataLandingRollbackFailed',
  CANONICAL_LIBRARY_METADATA_LANDING_LEGACY_FALLBACK_USED = 'canonicalLibraryMetadataLandingLegacyFallbackUsed',
  CANONICAL_LIBRARY_METADATA_LANDING_DUPLICATE_SUPPRESSED = 'canonicalLibraryMetadataLandingDuplicateSuppressed',
  CANONICAL_LIBRARY_METADATA_LANDING_READ_SIDE_EQUIVALENT = 'canonicalLibraryMetadataLandingReadSideEquivalent',
  CANONICAL_LIBRARY_METADATA_LANDING_READ_SIDE_DIVERGENT = 'canonicalLibraryMetadataLandingReadSideDivergent',
  CANONICAL_LIBRARY_METADATA_LANDING_REPORT_BUILT = 'canonicalLibraryMetadataLandingReportBuilt',
  CANONICAL_LIBRARY_METADATA_REAL_CANARY_INJECTION_CONFIGURED = 'canonicalLibraryMetadataRealCanaryInjectionConfigured',
  CANONICAL_LIBRARY_METADATA_REAL_CANARY_BLOCKED = 'canonicalLibraryMetadataRealCanaryBlocked',
  CANONICAL_LIBRARY_METADATA_REAL_CANARY_ARMED = 'canonicalLibraryMetadataRealCanaryArmed',
  CANONICAL_LIBRARY_METADATA_REAL_CANARY_EXECUTION_STARTED = 'canonicalLibraryMetadataRealCanaryExecutionStarted',
  CANONICAL_LIBRARY_METADATA_REAL_CANARY_EXECUTION_COMPLETED = 'canonicalLibraryMetadataRealCanaryExecutionCompleted',
  CANONICAL_LIBRARY_METADATA_REAL_CANARY_EXECUTION_FAILED = 'canonicalLibraryMetadataRealCanaryExecutionFailed',
  CANONICAL_LIBRARY_METADATA_REAL_CANARY_NO_ELIGIBLE_CANDIDATE = 'canonicalLibraryMetadataRealCanaryNoEligibleCandidate',
  CANONICAL_LIBRARY_METADATA_REAL_CANARY_UNSAFE_CANDIDATE_SKIPPED = 'canonicalLibraryMetadataRealCanaryUnsafeCandidateSkipped',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_GATE_EVALUATED = 'canonicalLibraryMetadataProductionRootGateEvaluated',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_GATE_BLOCKED = 'canonicalLibraryMetadataProductionRootGateBlocked',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_GATE_ALLOWED = 'canonicalLibraryMetadataProductionRootGateAllowed',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_N1_STARTED = 'canonicalLibraryMetadataProductionRootN1Started',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_N1_COMPLETED = 'canonicalLibraryMetadataProductionRootN1Completed',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_N1_FAILED = 'canonicalLibraryMetadataProductionRootN1Failed',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_SAFETY_PROOF_BUILT = 'canonicalLibraryMetadataProductionRootSafetyProofBuilt',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_CHECKPOINT_CREATED = 'canonicalLibraryMetadataProductionRootCheckpointCreated',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_ATOMIC_WRITE_STARTED = 'canonicalLibraryMetadataProductionRootAtomicWriteStarted',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_ATOMIC_WRITE_COMPLETED = 'canonicalLibraryMetadataProductionRootAtomicWriteCompleted',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_POSTCONDITION_VERIFIED = 'canonicalLibraryMetadataProductionRootPostconditionVerified',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_ROLLBACK_STARTED = 'canonicalLibraryMetadataProductionRootRollbackStarted',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_ROLLBACK_COMPLETED = 'canonicalLibraryMetadataProductionRootRollbackCompleted',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_ROLLBACK_FAILED = 'canonicalLibraryMetadataProductionRootRollbackFailed',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_LEGACY_FALLBACK_USED = 'canonicalLibraryMetadataProductionRootLegacyFallbackUsed',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_DUPLICATE_SUPPRESSED = 'canonicalLibraryMetadataProductionRootDuplicateSuppressed',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_READ_SIDE_EQUIVALENT = 'canonicalLibraryMetadataProductionRootReadSideEquivalent',
  CANONICAL_LIBRARY_METADATA_PRODUCTION_ROOT_READ_SIDE_DIVERGENT = 'canonicalLibraryMetadataProductionRootReadSideDivergent',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_CUTOVER_CANDIDATE_EVALUATED = 'canonicalLibraryMetadataReadSideCutoverCandidateEvaluated',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_CUTOVER_CANDIDATE_BLOCKED = 'canonicalLibraryMetadataReadSideCutoverCandidateBlocked',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_CUTOVER_CANDIDATE_READY = 'canonicalLibraryMetadataReadSideCutoverCandidateReady',
  CANONICAL_LIBRARY_METADATA_GUARDED_CANONICAL_READ_SUPPRESSED = 'canonicalLibraryMetadataGuardedCanonicalReadSuppressed',
  CANONICAL_LIBRARY_METADATA_LEGACY_READ_FALLBACK_AVAILABLE = 'canonicalLibraryMetadataLegacyReadFallbackAvailable',
  CANONICAL_LIBRARY_METADATA_READ_SOURCE_EVALUATED = 'canonicalLibraryMetadataReadSourceEvaluated',
  CANONICAL_LIBRARY_METADATA_READ_SOURCE_LEGACY_RETURNED = 'canonicalLibraryMetadataReadSourceLegacyReturned',
  CANONICAL_LIBRARY_METADATA_READ_SOURCE_CANONICAL_CANDIDATE_BUILT = 'canonicalLibraryMetadataReadSourceCanonicalCandidateBuilt',
  CANONICAL_LIBRARY_METADATA_GUARDED_CANONICAL_READ_ALLOWED = 'canonicalLibraryMetadataGuardedCanonicalReadAllowed',
  CANONICAL_LIBRARY_METADATA_GUARDED_CANONICAL_READ_BLOCKED = 'canonicalLibraryMetadataGuardedCanonicalReadBlocked',
  CANONICAL_LIBRARY_METADATA_GUARDED_CANONICAL_READ_SERVED = 'canonicalLibraryMetadataGuardedCanonicalReadServed',
  CANONICAL_LIBRARY_METADATA_GUARDED_CANONICAL_READ_FALLBACK = 'canonicalLibraryMetadataGuardedCanonicalReadFallback',
  CANONICAL_LIBRARY_METADATA_READ_CUTOVER_GATE_EVALUATED = 'canonicalLibraryMetadataReadCutoverGateEvaluated',
  CANONICAL_LIBRARY_METADATA_READ_CUTOVER_GATE_BLOCKED = 'canonicalLibraryMetadataReadCutoverGateBlocked',
  CANONICAL_LIBRARY_METADATA_READ_CUTOVER_GATE_ALLOWED = 'canonicalLibraryMetadataReadCutoverGateAllowed',
  CANONICAL_LIBRARY_METADATA_READ_OUTPUT_EQUIVALENT = 'canonicalLibraryMetadataReadOutputEquivalent',
  CANONICAL_LIBRARY_METADATA_READ_OUTPUT_DIVERGENT = 'canonicalLibraryMetadataReadOutputDivergent',
  CANONICAL_LIBRARY_METADATA_RETIREMENT_CANDIDATE_UPDATED = 'canonicalLibraryMetadataRetirementCandidateUpdated',
  CANONICAL_LIBRARY_METADATA_RETIREMENT_STILL_BLOCKED = 'canonicalLibraryMetadataRetirementStillBlocked',
  CANONICAL_LIBRARY_METADATA_ROLLBACK_DRILL_SUMMARIZED = 'canonicalLibraryMetadataRollbackDrillSummarized',
  CANONICAL_MIGRATION_LANDING_FREEZE_VIOLATION = 'canonicalMigrationLandingFreezeViolation',
  CANONICAL_LIBRARY_METADATA_READ_SIDE_PARALLEL_FAILED = 'canonicalLibraryMetadataReadSideParallelFailed'
}

// ── Cutover Diagnostic ──

export class CanonicalLibraryMetadataCutoverDiagnostic {
  kind: CanonicalLibraryMetadataCutoverDiagnosticKind
  syncRunID: string | null
  trigger: CanonicalSyncPlanTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  domain: CanonicalLibraryMetadataCutoverDomain | null
  objectID: string | null
  objectKind: CanonicalObjectKind | null
  action: string | null
  result: string | null
  reason: string | null
  hashPrefix: string | null

  constructor(
    kind: CanonicalLibraryMetadataCutoverDiagnosticKind,
    syncRunID: string | null = null,
    trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC,
    nodeRole: CanonicalProductionExecutionDomainRole = CanonicalProductionExecutionDomainRole.TEST_HARNESS,
    domain: CanonicalLibraryMetadataCutoverDomain | null = null,
    objectID: string | null = null,
    objectKind: CanonicalObjectKind | null = null,
    action: string | null = null,
    result: string | null = null,
    reason: string | null = null,
    hash: CanonicalHash | null = null
  ) {
    this.kind = kind
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.domain = domain
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'library-object') : null
    this.objectKind = objectKind
    this.action = CanonicalProductionRedaction.safeDiagnosticText(action)
    this.result = CanonicalProductionRedaction.safeDiagnosticText(result)
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason)
    this.hashPrefix = hash ? CanonicalProductionRedaction.hashPrefix(hash.value) : null
  }

  get id(): string {
    return [this.kind, this.objectID ?? 'run', this.result ?? '', this.reason ?? ''].join('|')
  }

  get diagnosticsSummary(): string {
    return [
      'syncRunID=' + (this.syncRunID ?? 'none'),
      'trigger=' + this.trigger,
      'nodeRole=' + this.nodeRole,
      'domain=' + (this.domain ?? 'none'),
      'objectID=' + (this.objectID ?? 'none'),
      'objectKind=' + (this.objectKind ?? 'none'),
      'action=' + (this.action ?? 'none'),
      'result=' + (this.result ?? 'none'),
      'reason=' + (this.reason ?? 'none'),
      'hashPrefix=' + (this.hashPrefix ?? 'none')
    ].join(',')
  }
}
