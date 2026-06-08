/**
 * CanonicalAudioUploadCutover — audio upload cutover domain.
 * Ported from Rokurics-Apple (CanonicalAudioUploadCutover.swift).
 * Evaluates audio upload candidates, evidence, gates, shadow rehearsal, and no-commit execution.
 * V812: production commit is blocked; only shadow-only no-commit is permitted.
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalRecordingObject, CanonicalArtifact, CanonicalArtifactKind,
  CanonicalArtifactAvailability, CanonicalManifest, nilIfEmpty, hashPrefix, safeLogicalPathToken,
  artifactID
} from './CanonicalCore'

// ── Types from other canonical modules (minimal forward declarations) ──

export enum CanonicalSyncPlanTrigger {
  MANUAL = 'manual',
  PERIODIC = 'periodic',
  APP_ACTIVATION = 'appActivation',
  RETRY_DRAINER = 'retryDrainer',
  VIEW_REFRESH = 'viewRefresh'
}

export enum CanonicalCutoverMode {
  DISABLED = 'disabled',
  CANARY = 'canary',
  SHADOW_ONLY = 'shadowOnly',
  GUARDED_EXECUTE_COMMIT = 'guardedExecuteCommit',
  GUARDED_EXECUTE_NO_COMMIT = 'guardedExecuteNoCommit'
}

export enum CanonicalCutoverAppSeamMode {
  DISABLED = 'disabled',
  GUARDED_EXECUTE_NO_COMMIT = 'guardedExecuteNoCommit',
  GUARDED_EXECUTE_COMMIT = 'guardedExecuteCommit',
  CANARY_COMMIT = 'canaryCommit',
  PRODUCTION_EXECUTE = 'productionExecute'
}

export class CanonicalCutoverToken {
  syncRunID: string | null
  ownerApproved: boolean

  constructor(syncRunID: string | null = null, ownerApproved: boolean = false) {
    this.syncRunID = syncRunID
    this.ownerApproved = ownerApproved
  }
}

export class CanonicalRootToken {
  value: string

  constructor(value: string) {
    this.value = value
  }
}

export class CanonicalFileReference {
  rootToken: CanonicalRootToken
  logicalPathToken: string
  artifactID: string
  artifactKind: CanonicalArtifactKind

  constructor(
    rootToken: CanonicalRootToken, logicalPathToken: string, artifactID: string, artifactKind: CanonicalArtifactKind
  ) {
    this.rootToken = rootToken
    this.logicalPathToken = logicalPathToken
    this.artifactID = artifactID
    this.artifactKind = artifactKind
  }
}

export class CanonicalFileReadResult {
  bytes: Uint8Array

  constructor(bytes: Uint8Array) {
    this.bytes = bytes
  }
}

// ── Minimal shadow upload types ──

export class CanonicalShadowUploadResult {
  completed: boolean
  divergence: string
  wroteShadowReceiver: boolean
  confirmedBytes: number

  constructor(completed: boolean = true, divergence: string = '', wroteShadowReceiver: boolean = true, confirmedBytes: number = 0) {
    this.completed = completed
    this.divergence = divergence
    this.wroteShadowReceiver = wroteShadowReceiver
    this.confirmedBytes = confirmedBytes
  }
}

export class CanonicalShadowUploadRehearsalInput {
  objectID: string
  targetReference: CanonicalFileReference
  bytes: Uint8Array
  chunkSize: number
  declaredTotalHash: CanonicalHash | null
  existingReceiverBytes: Uint8Array | null
  simulateInterruptionAfterFirstChunk: boolean

  constructor(
    objectID: string, targetReference: CanonicalFileReference, bytes: Uint8Array,
    chunkSize: number = 2 * 1024 * 1024, declaredTotalHash: CanonicalHash | null = null,
    existingReceiverBytes: Uint8Array | null = null, simulateInterruptionAfterFirstChunk: boolean = false
  ) {
    this.objectID = objectID
    this.targetReference = targetReference
    this.bytes = bytes
    this.chunkSize = Math.max(1, chunkSize)
    this.declaredTotalHash = declaredTotalHash
    this.existingReceiverBytes = existingReceiverBytes
    this.simulateInterruptionAfterFirstChunk = simulateInterruptionAfterFirstChunk
  }
}

// ── Interfaces for minimal forward types ──

export class CanonicalShadowUploadReceiver {
  rootToken: CanonicalRootToken

  constructor(rootToken: CanonicalRootToken) {
    this.rootToken = rootToken
  }

  async seed(reference: CanonicalFileReference, bytes: Uint8Array): Promise<void> {
  }

  async read(reference: CanonicalFileReference): Promise<CanonicalFileReadResult> {
    return new CanonicalFileReadResult(new Uint8Array(0))
  }
}

export class CanonicalShadowUploadRehearsal {
  constructor() {}

  async run(input: CanonicalShadowUploadRehearsalInput, receiver: CanonicalShadowUploadReceiver): Promise<CanonicalShadowUploadResult> {
    return new CanonicalShadowUploadResult(true, 'ok', true, input.bytes.length)
  }
}

// ── CanonicalProjectionContract (utility forward) ──

class CanonicalProjectionContract {
  static safeLogicalPathToken(token: string | null): string | null {
    return safeLogicalPathToken(token)
  }

  static artifactID(objectID: string, kind: CanonicalArtifactKind): string {
    return artifactID(objectID, kind)
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS
// ═══════════════════════════════════════════════════════════════════════════════

export enum CanonicalAudioUploadCutoverDomain {
  AUDIO_UPLOAD = 'audioUpload'
}

export enum CanonicalAudioUploadActionKind {
  AUDIO_UPLOAD_NO_OP = 'audioUploadNoOp',
  AUDIO_UPLOAD_SHADOW_REHEARSAL = 'audioUploadShadowRehearsal',
  AUDIO_UPLOAD_CANARY_CANDIDATE = 'audioUploadCanaryCandidate',
  AUDIO_UPLOAD_CONFLICT_RECORD = 'audioUploadConflictRecord',
  AUDIO_UPLOAD_DEFERRED_PEER_UNKNOWN = 'audioUploadDeferredPeerUnknown',
  UNSUPPORTED = 'unsupported'
}

export enum CanonicalAudioUploadPeerState {
  UNKNOWN = 'unknown',
  MISSING = 'missing',
  METADATA_ONLY = 'metadataOnly',
  AVAILABLE = 'available',
  DIFFERENT = 'different',
  DELETED = 'deleted'
}

export enum CanonicalAudioUploadEvidenceStatus {
  COMPLETE = 'complete',
  BLOCKED = 'blocked',
  CONFLICT = 'conflict',
  DEFERRED = 'deferred',
  DISABLED = 'disabled'
}

export enum CanonicalAudioUploadEvidenceBlocker {
  LOCAL_AUDIO_MISSING = 'localAudioMissing',
  LOCAL_HASH_UNAVAILABLE = 'localHashUnavailable',
  LOCAL_BYTE_SIZE_UNAVAILABLE = 'localByteSizeUnavailable',
  PEER_TRUTH_MISSING = 'peerTruthMissing',
  PEER_UNKNOWN = 'peerUnknown',
  COMPLETED_LEDGER_WITHOUT_PEER_MATCH = 'completedLedgerWithoutPeerMatch',
  METADATA_UPLOADED_NOT_AUDIO_PROOF = 'metadataUploadedNotAudioProof',
  RECEIVE_RECORD_NOT_AUDIO_PROOF = 'receiveRecordNotAudioProof',
  UI_UPLOADED_NOT_AUDIO_PROOF = 'uiUploadedNotAudioProof',
  VIEW_REFRESH_SUPPRESSED = 'viewRefreshSuppressed',
  RETRY_DRAINER_FRESH_JOB_SUPPRESSED = 'retryDrainerFreshJobSuppressed',
  MANUAL_UPLOAD_BUTTON_LEGACY_OWNED = 'manualUploadButtonLegacyOwned',
  DIFFERENT_HASH_OR_SIZE = 'differentHashOrSize',
  PRODUCTION_UPLOAD_SUPPRESSED = 'productionUploadSuppressed',
  CANARY_STAGE_BLOCKED = 'canaryStageBlocked',
  UNSUPPORTED_PRODUCTION_COMMIT = 'unsupportedProductionCommit',
  SHADOW_REHEARSAL_FAILED = 'shadowRehearsalFailed'
}

export enum CanonicalAudioUploadLedgerPhase {
  NONE = 'none',
  QUEUED = 'queued',
  IN_FLIGHT = 'inFlight',
  FINALIZING = 'finalizing',
  COMPLETED = 'completed',
  FAILED = 'failed',
  RETRY_PENDING = 'retryPending',
  FATAL_FAILED = 'fatalFailed'
}

export enum CanonicalAudioUploadTriggerSource {
  MANUAL_UPLOAD_BUTTON = 'manualUploadButton',
  RETRY_DRAINER = 'retryDrainer',
  MANUAL_SYNC_I_PHONE = 'manualSyncIPhone',
  MANUAL_SYNC_MAC_HINT = 'manualSyncMacHint',
  PERIODIC_SYNC = 'periodicSync',
  APP_ACTIVATION_REFRESH = 'appActivationRefresh',
  VIEW_REFRESH = 'viewRefresh',
  ORDINARY_SYNC = 'ordinarySync'
}

export function fromSyncPlanTrigger(trigger: CanonicalSyncPlanTrigger): CanonicalAudioUploadTriggerSource {
  switch (trigger) {
    case CanonicalSyncPlanTrigger.MANUAL: return CanonicalAudioUploadTriggerSource.MANUAL_SYNC_I_PHONE
    case CanonicalSyncPlanTrigger.PERIODIC: return CanonicalAudioUploadTriggerSource.PERIODIC_SYNC
    case CanonicalSyncPlanTrigger.APP_ACTIVATION: return CanonicalAudioUploadTriggerSource.APP_ACTIVATION_REFRESH
    case CanonicalSyncPlanTrigger.RETRY_DRAINER: return CanonicalAudioUploadTriggerSource.RETRY_DRAINER
    case CanonicalSyncPlanTrigger.VIEW_REFRESH: return CanonicalAudioUploadTriggerSource.VIEW_REFRESH
  }
}

export function triggerToCanonicalSyncPlanTrigger(source: CanonicalAudioUploadTriggerSource): CanonicalSyncPlanTrigger {
  switch (source) {
    case CanonicalAudioUploadTriggerSource.MANUAL_UPLOAD_BUTTON:
    case CanonicalAudioUploadTriggerSource.MANUAL_SYNC_I_PHONE:
    case CanonicalAudioUploadTriggerSource.MANUAL_SYNC_MAC_HINT:
      return CanonicalSyncPlanTrigger.MANUAL
    case CanonicalAudioUploadTriggerSource.PERIODIC_SYNC:
    case CanonicalAudioUploadTriggerSource.ORDINARY_SYNC:
      return CanonicalSyncPlanTrigger.PERIODIC
    case CanonicalAudioUploadTriggerSource.APP_ACTIVATION_REFRESH:
      return CanonicalSyncPlanTrigger.APP_ACTIVATION
    case CanonicalAudioUploadTriggerSource.RETRY_DRAINER:
      return CanonicalSyncPlanTrigger.RETRY_DRAINER
    case CanonicalAudioUploadTriggerSource.VIEW_REFRESH:
      return CanonicalSyncPlanTrigger.VIEW_REFRESH
  }
}

export function isViewRefresh(source: CanonicalAudioUploadTriggerSource): boolean {
  return source === CanonicalAudioUploadTriggerSource.VIEW_REFRESH
}

export function isRetryDrainer(source: CanonicalAudioUploadTriggerSource): boolean {
  return source === CanonicalAudioUploadTriggerSource.RETRY_DRAINER
}

export function isExplicitManualUploadButton(source: CanonicalAudioUploadTriggerSource): boolean {
  return source === CanonicalAudioUploadTriggerSource.MANUAL_UPLOAD_BUTTON
}

export enum CanonicalAudioUploadNodeRole {
  I_PHONE = 'iPhone',
  MAC = 'mac',
  TEST_HARNESS = 'testHarness'
}

export enum CanonicalAudioUploadDiagnosticKind {
  CANONICAL_AUDIO_UPLOAD_CUTOVER_GATE_EVALUATED = 'canonicalAudioUploadCutoverGateEvaluated',
  CANONICAL_AUDIO_UPLOAD_CUTOVER_GATE_ALLOWED = 'canonicalAudioUploadCutoverGateAllowed',
  CANONICAL_AUDIO_UPLOAD_CUTOVER_GATE_BLOCKED = 'canonicalAudioUploadCutoverGateBlocked',
  CANONICAL_AUDIO_UPLOAD_EVIDENCE_BUILT = 'canonicalAudioUploadEvidenceBuilt',
  CANONICAL_AUDIO_UPLOAD_EVIDENCE_BLOCKED = 'canonicalAudioUploadEvidenceBlocked',
  CANONICAL_AUDIO_UPLOAD_PEER_SAME_NO_OP = 'canonicalAudioUploadPeerSameNoOp',
  CANONICAL_AUDIO_UPLOAD_PEER_UNKNOWN_DEFERRED = 'canonicalAudioUploadPeerUnknownDeferred',
  CANONICAL_AUDIO_UPLOAD_CONFLICT_DETECTED = 'canonicalAudioUploadConflictDetected',
  CANONICAL_AUDIO_UPLOAD_COMPLETED_LEDGER_REJECTED_AS_NO_OP = 'canonicalAudioUploadCompletedLedgerRejectedAsNoOp',
  CANONICAL_AUDIO_UPLOAD_METADATA_UPLOADED_REJECTED_AS_AUDIO_PROOF = 'canonicalAudioUploadMetadataUploadedRejectedAsAudioProof',
  CANONICAL_AUDIO_UPLOAD_RECEIVE_RECORD_REJECTED_AS_AUDIO_PROOF = 'canonicalAudioUploadReceiveRecordRejectedAsAudioProof',
  CANONICAL_AUDIO_UPLOAD_UI_UPLOADED_REJECTED_AS_AUDIO_PROOF = 'canonicalAudioUploadUIUploadedRejectedAsAudioProof',
  CANONICAL_AUDIO_UPLOAD_VIEW_REFRESH_SUPPRESSED = 'canonicalAudioUploadViewRefreshSuppressed',
  CANONICAL_AUDIO_UPLOAD_RETRY_DRAINER_FRESH_JOB_SUPPRESSED = 'canonicalAudioUploadRetryDrainerFreshJobSuppressed',
  CANONICAL_AUDIO_UPLOAD_MANUAL_BUTTON_LEGACY_OWNED = 'canonicalAudioUploadManualButtonLegacyOwned',
  CANONICAL_AUDIO_UPLOAD_SHADOW_REHEARSAL_STARTED = 'canonicalAudioUploadShadowRehearsalStarted',
  CANONICAL_AUDIO_UPLOAD_SHADOW_REHEARSAL_COMPLETED = 'canonicalAudioUploadShadowRehearsalCompleted',
  CANONICAL_AUDIO_UPLOAD_SHADOW_RECEIVER_WROTE = 'canonicalAudioUploadShadowReceiverWrote',
  CANONICAL_AUDIO_UPLOAD_SHADOW_RECEIVER_NO_OP = 'canonicalAudioUploadShadowReceiverNoOp',
  CANONICAL_AUDIO_UPLOAD_NO_COMMIT_STARTED = 'canonicalAudioUploadNoCommitStarted',
  CANONICAL_AUDIO_UPLOAD_NO_COMMIT_COMPLETED = 'canonicalAudioUploadNoCommitCompleted',
  CANONICAL_AUDIO_UPLOAD_PRODUCTION_COORDINATOR_SUPPRESSED = 'canonicalAudioUploadProductionCoordinatorSuppressed',
  CANONICAL_AUDIO_UPLOAD_RECORDING_UPLOAD_CLIENT_SUPPRESSED = 'canonicalAudioUploadRecordingUploadClientSuppressed',
  CANONICAL_AUDIO_UPLOAD_SECURE_MAC_UPLOAD_CLIENT_SUPPRESSED = 'canonicalAudioUploadSecureMacUploadClientSuppressed',
  CANONICAL_AUDIO_UPLOAD_INBOX_WRITE_SUPPRESSED = 'canonicalAudioUploadInboxWriteSuppressed',
  CANONICAL_AUDIO_UPLOAD_RECEIVE_JSON_WRITE_SUPPRESSED = 'canonicalAudioUploadReceiveJSONWriteSuppressed',
  CANONICAL_AUDIO_UPLOAD_LEDGER_MUTATION_SUPPRESSED = 'canonicalAudioUploadLedgerMutationSuppressed',
  CANONICAL_AUDIO_UPLOAD_RETRY_DRAINER_MUTATION_SUPPRESSED = 'canonicalAudioUploadRetryDrainerMutationSuppressed',
  CANONICAL_AUDIO_UPLOAD_LEGACY_FALLBACK_PRESERVED = 'canonicalAudioUploadLegacyFallbackPreserved',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_MODE_EVALUATED = 'canonicalAudioUploadRuntimeModeEvaluated',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_CANDIDATE_SELECTED = 'canonicalAudioUploadRuntimeCandidateSelected',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_STARTED = 'canonicalAudioUploadRuntimeStarted',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_CHUNK_SENT = 'canonicalAudioUploadRuntimeChunkSent',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_CHUNK_CONFIRMED = 'canonicalAudioUploadRuntimeChunkConfirmed',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_RESUME_STARTED = 'canonicalAudioUploadRuntimeResumeStarted',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_FINALIZE_STARTED = 'canonicalAudioUploadRuntimeFinalizeStarted',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_FINALIZE_COMPLETED = 'canonicalAudioUploadRuntimeFinalizeCompleted',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_FINALIZE_FAILED = 'canonicalAudioUploadRuntimeFinalizeFailed',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_RETRY_SCHEDULED = 'canonicalAudioUploadRuntimeRetryScheduled',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_LEGACY_FALLBACK_USED = 'canonicalAudioUploadRuntimeLegacyFallbackUsed',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_PEER_UNKNOWN_DEFERRED = 'canonicalAudioUploadRuntimePeerUnknownDeferred',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_CONFLICT_BLOCKED = 'canonicalAudioUploadRuntimeConflictBlocked',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_EXISTING_DIFFERENT_AUDIO_BLOCKED = 'canonicalAudioUploadRuntimeExistingDifferentAudioBlocked',
  CANONICAL_AUDIO_UPLOAD_RUNTIME_COMPLETED_LEDGER_REJECTED_AS_NO_OP = 'canonicalAudioUploadRuntimeCompletedLedgerRejectedAsNoOp',
  CANONICAL_AUDIO_UPLOAD_READ_SIDE_PROJECTION_STARTED = 'canonicalAudioUploadReadSideProjectionStarted',
  CANONICAL_AUDIO_UPLOAD_READ_SIDE_PROJECTION_EQUIVALENT = 'canonicalAudioUploadReadSideProjectionEquivalent',
  CANONICAL_AUDIO_UPLOAD_READ_SIDE_PROJECTION_DIVERGED = 'canonicalAudioUploadReadSideProjectionDiverged',
  CANONICAL_AUDIO_UPLOAD_ABORT_ROLLBACK_POLICY_EVALUATED = 'canonicalAudioUploadAbortRollbackPolicyEvaluated'
}

export enum CanonicalAudioUploadCutoverFailure {
  DISABLED = 'disabled',
  CANARY_BUDGET_ZERO = 'canaryBudgetZero',
  MISSING_CUTOVER_TOKEN = 'missingCutoverToken',
  INSUFFICIENT_EVIDENCE = 'insufficientEvidence',
  CANDIDATE_EVIDENCE_BLOCKED = 'candidateEvidenceBlocked',
  NO_ELIGIBLE_CANDIDATES = 'noEligibleCandidates',
  PRODUCTION_COMMIT_BLOCKED_V812 = 'productionCommitBlockedV812',
  PEER_SNAPSHOT_UNAVAILABLE = 'peerSnapshotUnavailable',
  MANUAL_USER_ACTION_LEGACY_OWNED = 'manualUserActionLegacyOwned',
  FUTURE_CANARY_STAGE_BLOCKED = 'futureCanaryStageBlocked',
  SHADOW_REHEARSAL_FAILED = 'shadowRehearsalFailed'
}

export enum CanonicalAudioUploadCanaryStage {
  DISABLED = 'disabled',
  SHADOW_ONLY = 'shadowOnly',
  N0 = 'n0',
  N1 = 'n1',
  N3 = 'n3',
  N10 = 'n10',
  ALL_ELIGIBLE = 'allEligible'
}

export function canaryBudgetForStage(stage: CanonicalAudioUploadCanaryStage): number {
  switch (stage) {
    case CanonicalAudioUploadCanaryStage.DISABLED:
    case CanonicalAudioUploadCanaryStage.SHADOW_ONLY:
    case CanonicalAudioUploadCanaryStage.N0:
      return 0
    case CanonicalAudioUploadCanaryStage.N1: return 1
    case CanonicalAudioUploadCanaryStage.N3: return 3
    case CanonicalAudioUploadCanaryStage.N10: return 10
    case CanonicalAudioUploadCanaryStage.ALL_ELIGIBLE: return Number.MAX_SAFE_INTEGER
  }
}

export function requestsProductionCanary(stage: CanonicalAudioUploadCanaryStage): boolean {
  return canaryBudgetForStage(stage) > 0
}

export enum CanonicalAudioUploadAbortPhase {
  BEFORE_START = 'beforeStart',
  BEFORE_FINALIZE = 'beforeFinalize',
  AFTER_FINALIZE = 'afterFinalize'
}

// ═══════════════════════════════════════════════════════════════════════════════
// STRUCTS / CLASSES
// ═══════════════════════════════════════════════════════════════════════════════

function safeTextMax(value: string | null, maxLen: number): string | null {
  if (!value) {
    return null
  }
  const trimmed = value.trim()
  if (trimmed.length === 0) {
    return null
  }
  return trimmed.substring(0, maxLen)
}

export class CanonicalAudioUploadLocalTruth {
  audioAvailable: boolean
  contentHash: CanonicalHash | null
  byteSize: number | null
  modifiedAt: CanonicalTimestamp | null
  logicalPathToken: string | null
  sourceDeviceID: string | null
  diagnosticsSummary: string | null

  constructor(
    audioAvailable: boolean,
    contentHash: CanonicalHash | null = null,
    byteSize: number | null = null,
    modifiedAt: CanonicalTimestamp | null = null,
    logicalPathToken: string | null = null,
    sourceDeviceID: string | null = null,
    diagnosticsSummary: string | null = null
  ) {
    this.audioAvailable = audioAvailable
    this.contentHash = contentHash
    this.byteSize = byteSize
    this.modifiedAt = modifiedAt
    this.logicalPathToken = CanonicalProjectionContract.safeLogicalPathToken(logicalPathToken)
    this.sourceDeviceID = safeTextMax(sourceDeviceID, 160)
    this.diagnosticsSummary = safeTextMax(diagnosticsSummary, 160)
  }

  static available(hash: CanonicalHash, byteSize: number, logicalPathToken: string | null = null,
    sourceDeviceID: string | null = null): CanonicalAudioUploadLocalTruth {
    return new CanonicalAudioUploadLocalTruth(true, hash, byteSize, null, logicalPathToken, sourceDeviceID)
  }

  static from(object: CanonicalRecordingObject): CanonicalAudioUploadLocalTruth {
    const audio = object.audioArtifact
    const available = audio !== null && audio !== undefined &&
      (audio.availability === CanonicalArtifactAvailability.AVAILABLE ||
        audio.availability === CanonicalArtifactAvailability.AVAILABLE_WITHOUT_HASH)
    return new CanonicalAudioUploadLocalTruth(
      available && audio?.tombstone !== true,
      audio?.contentHash ?? null,
      audio?.byteSize ?? null,
      audio?.modifiedAt ?? null,
      audio?.logicalPathToken ?? null,
      object.nodeID,
      'availability=' + (audio?.availability ?? 'missing')
    )
  }

  get hashAvailable(): boolean {
    return this.contentHash !== null
  }

  get byteSizeAvailable(): boolean {
    return this.byteSize !== null
  }

  get sufficientForUploadCandidate(): boolean {
    return this.audioAvailable && this.hashAvailable && this.byteSizeAvailable
  }
}

export class CanonicalAudioUploadPeerTruth {
  state: CanonicalAudioUploadPeerState
  contentHash: CanonicalHash | null
  byteSize: number | null
  receiveRecordExists: boolean
  metadataUploaded: boolean
  uiUploaded: boolean
  diagnosticsSummary: string | null

  constructor(
    state: CanonicalAudioUploadPeerState,
    contentHash: CanonicalHash | null = null,
    byteSize: number | null = null,
    receiveRecordExists: boolean = false,
    metadataUploaded: boolean = false,
    uiUploaded: boolean = false,
    diagnosticsSummary: string | null = null
  ) {
    this.state = state
    this.contentHash = contentHash
    this.byteSize = byteSize
    this.receiveRecordExists = receiveRecordExists
    this.metadataUploaded = metadataUploaded
    this.uiUploaded = uiUploaded
    this.diagnosticsSummary = safeTextMax(diagnosticsSummary, 160)
  }

  static from(object: CanonicalRecordingObject | null): CanonicalAudioUploadPeerTruth {
    if (!object) {
      return new CanonicalAudioUploadPeerTruth(CanonicalAudioUploadPeerState.MISSING, null, null, false, false, false,
        'peerObjectMissing')
    }
    const audio = object.audioArtifact
    if (!audio) {
      return new CanonicalAudioUploadPeerTruth(CanonicalAudioUploadPeerState.METADATA_ONLY, null, null, false, false,
        false, 'peerMetadataOnly')
    }
    let state: CanonicalAudioUploadPeerState
    switch (audio.availability) {
      case CanonicalArtifactAvailability.UNKNOWN:
        state = CanonicalAudioUploadPeerState.UNKNOWN
        break
      case CanonicalArtifactAvailability.MISSING:
        state = CanonicalAudioUploadPeerState.MISSING
        break
      case CanonicalArtifactAvailability.AVAILABLE_WITHOUT_HASH:
      case CanonicalArtifactAvailability.AVAILABLE:
        state = audio.tombstone === true ? CanonicalAudioUploadPeerState.DELETED : CanonicalAudioUploadPeerState.AVAILABLE
        break
    }
    return new CanonicalAudioUploadPeerTruth(state, audio.contentHash ?? null, audio.byteSize ?? null, false, false,
      false, 'availability=' + audio.availability)
  }

  peerTruthSufficientForNoOp(local: CanonicalAudioUploadLocalTruth): boolean {
    if (this.state !== CanonicalAudioUploadPeerState.AVAILABLE) {
      return false
    }
    const peerHash = this.contentHash
    const peerByteSize = this.byteSize
    const localHash = local.contentHash
    const localByteSize = local.byteSize
    if (!peerHash || peerByteSize === null || !localHash || localByteSize === null) {
      return false
    }
    return peerHash.value === localHash.value && peerByteSize === localByteSize
  }
}

export class CanonicalAudioUploadLedgerTruth {
  phase: CanonicalAudioUploadLedgerPhase
  contentHash: CanonicalHash | null
  byteSize: number | null
  metadataUploaded: boolean
  uiUploaded: boolean
  receiveRecordExists: boolean

  constructor(
    phase: CanonicalAudioUploadLedgerPhase = CanonicalAudioUploadLedgerPhase.NONE,
    contentHash: CanonicalHash | null = null,
    byteSize: number | null = null,
    metadataUploaded: boolean = false,
    uiUploaded: boolean = false,
    receiveRecordExists: boolean = false
  ) {
    this.phase = phase
    this.contentHash = contentHash
    this.byteSize = byteSize
    this.metadataUploaded = metadataUploaded
    this.uiUploaded = uiUploaded
    this.receiveRecordExists = receiveRecordExists
  }
}

export class CanonicalAudioUploadRetryTruth {
  hasExistingEligibleRetry: boolean
  retryPending: boolean
  canFreshCreateJob: boolean

  constructor(hasExistingEligibleRetry: boolean = false, retryPending: boolean = false,
    canFreshCreateJob: boolean = false) {
    this.hasExistingEligibleRetry = hasExistingEligibleRetry
    this.retryPending = retryPending
    this.canFreshCreateJob = canFreshCreateJob
  }
}

export class CanonicalAudioUploadDiagnostic {
  kind: CanonicalAudioUploadDiagnosticKind
  syncRunID: string | null
  trigger: CanonicalAudioUploadTriggerSource
  nodeRole: CanonicalAudioUploadNodeRole
  domain: CanonicalAudioUploadCutoverDomain
  objectID: string | null
  peerState: CanonicalAudioUploadPeerState | null
  ledgerPhase: CanonicalAudioUploadLedgerPhase | null
  action: CanonicalAudioUploadActionKind | null
  result: string | null
  reason: string | null
  hashPrefix: string | null

  constructor(
    kind: CanonicalAudioUploadDiagnosticKind,
    syncRunID: string | null = null,
    trigger: CanonicalAudioUploadTriggerSource,
    nodeRole: CanonicalAudioUploadNodeRole,
    domain: CanonicalAudioUploadCutoverDomain = CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD,
    objectID: string | null = null,
    peerState: CanonicalAudioUploadPeerState | null = null,
    ledgerPhase: CanonicalAudioUploadLedgerPhase | null = null,
    action: CanonicalAudioUploadActionKind | null = null,
    result: string | null = null,
    reason: string | null = null,
    hashPrefix: string | null = null
  ) {
    this.kind = kind
    this.syncRunID = safeTextMax(syncRunID, 96)
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.domain = domain
    this.objectID = safeTextMax(objectID, 96)
    this.peerState = peerState
    this.ledgerPhase = ledgerPhase
    this.action = action
    this.result = safeTextMax(result, 96)
    this.reason = safeTextMax(reason, 160)
    this.hashPrefix = hashPrefix ? hashPrefix.substring(0, 12) : null
  }

  get diagnosticsSummary(): string {
    const parts: string[] = [
      'trigger=' + this.trigger,
      'nodeRole=' + this.nodeRole,
      'domain=' + this.domain
    ]
    if (this.objectID) {
      parts.push('objectID=' + this.objectID)
    }
    if (this.peerState) {
      parts.push('peerState=' + this.peerState)
    }
    if (this.ledgerPhase) {
      parts.push('ledgerPhase=' + this.ledgerPhase)
    }
    if (this.action) {
      parts.push('action=' + this.action)
    }
    if (this.result) {
      parts.push('result=' + this.result)
    }
    if (this.reason) {
      parts.push('reason=' + this.reason)
    }
    if (this.hashPrefix) {
      parts.push('hashPrefix=' + this.hashPrefix)
    }
    return parts.join(',')
  }
}

export class CanonicalAudioUploadCutoverCandidate {
  objectID: string
  localTruth: CanonicalAudioUploadLocalTruth
  peerTruth: CanonicalAudioUploadPeerTruth
  ledgerTruth: CanonicalAudioUploadLedgerTruth
  retryTruth: CanonicalAudioUploadRetryTruth
  trigger: CanonicalAudioUploadTriggerSource
  actionKind: CanonicalAudioUploadActionKind
  reason: string
  evidenceStatus: CanonicalAudioUploadEvidenceStatus
  evidenceBlockers: CanonicalAudioUploadEvidenceBlocker[]
  diagnostics: CanonicalAudioUploadDiagnosticKind[]
  manualUserAction: boolean
  ordinarySync: boolean
  retryDrainer: boolean
  productionUploadSuppressed: boolean
  legacyUploadCoordinatorNotCalled: boolean
  recordingUploadClientNotCalled: boolean
  secureMacUploadClientNotCalled: boolean

  constructor(
    objectID: string,
    localTruth: CanonicalAudioUploadLocalTruth,
    peerTruth: CanonicalAudioUploadPeerTruth,
    ledgerTruth: CanonicalAudioUploadLedgerTruth = new CanonicalAudioUploadLedgerTruth(),
    retryTruth: CanonicalAudioUploadRetryTruth = new CanonicalAudioUploadRetryTruth(),
    trigger: CanonicalAudioUploadTriggerSource,
    actionKind: CanonicalAudioUploadActionKind,
    reason: string,
    evidenceStatus: CanonicalAudioUploadEvidenceStatus,
    evidenceBlockers: CanonicalAudioUploadEvidenceBlocker[] = [],
    diagnostics: CanonicalAudioUploadDiagnosticKind[] = []
  ) {
    const trimmedObjectID = objectID.trim()
    this.objectID = trimmedObjectID.length === 0 ? 'object:unknown' : trimmedObjectID
    this.localTruth = localTruth
    this.peerTruth = peerTruth
    this.ledgerTruth = ledgerTruth
    this.retryTruth = retryTruth
    this.trigger = trigger
    this.actionKind = actionKind
    this.reason = reason
    this.evidenceStatus = evidenceStatus
    this.evidenceBlockers = [...new Set(evidenceBlockers)].sort((a, b) => a.localeCompare(b))
    this.diagnostics = [...new Set(diagnostics)].sort((a, b) => a.localeCompare(b))
    this.manualUserAction = isExplicitManualUploadButton(trigger)
    this.ordinarySync = trigger === CanonicalAudioUploadTriggerSource.ORDINARY_SYNC ||
      trigger === CanonicalAudioUploadTriggerSource.PERIODIC_SYNC ||
      trigger === CanonicalAudioUploadTriggerSource.APP_ACTIVATION_REFRESH
    this.retryDrainer = isRetryDrainer(trigger)
    this.productionUploadSuppressed = true
    this.legacyUploadCoordinatorNotCalled = true
    this.recordingUploadClientNotCalled = true
    this.secureMacUploadClientNotCalled = true
  }

  get canaryEligibleInShadowOnlyModel(): boolean {
    return this.actionKind === CanonicalAudioUploadActionKind.AUDIO_UPLOAD_CANARY_CANDIDATE &&
      this.evidenceStatus === CanonicalAudioUploadEvidenceStatus.COMPLETE &&
      this.productionUploadSuppressed
  }

  get hashPrefix(): string | null {
    if (!this.localTruth.contentHash) {
      return null
    }
    return this.localTruth.contentHash.value.substring(0, 12)
  }

  static evaluate(
    objectID: string,
    localTruth: CanonicalAudioUploadLocalTruth,
    peerTruth: CanonicalAudioUploadPeerTruth,
    ledgerTruth: CanonicalAudioUploadLedgerTruth = new CanonicalAudioUploadLedgerTruth(),
    retryTruth: CanonicalAudioUploadRetryTruth = new CanonicalAudioUploadRetryTruth(),
    trigger: CanonicalAudioUploadTriggerSource
  ): CanonicalAudioUploadCutoverCandidate {
    let blockers: CanonicalAudioUploadEvidenceBlocker[] = []
    let diagnostics: CanonicalAudioUploadDiagnosticKind[] = [CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_EVIDENCE_BUILT]

    if (isViewRefresh(trigger)) {
      blockers.push(CanonicalAudioUploadEvidenceBlocker.VIEW_REFRESH_SUPPRESSED)
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_VIEW_REFRESH_SUPPRESSED)
      return CanonicalAudioUploadCutoverCandidate.make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
        trigger, CanonicalAudioUploadActionKind.UNSUPPORTED, 'viewRefreshNeverCreatesAudioUploadCandidate',
        CanonicalAudioUploadEvidenceStatus.BLOCKED, blockers, diagnostics)
    }

    if (isExplicitManualUploadButton(trigger)) {
      blockers.push(CanonicalAudioUploadEvidenceBlocker.MANUAL_UPLOAD_BUTTON_LEGACY_OWNED)
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_MANUAL_BUTTON_LEGACY_OWNED)
      return CanonicalAudioUploadCutoverCandidate.make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
        trigger, CanonicalAudioUploadActionKind.UNSUPPORTED,
        'explicitManualUploadButtonStaysLegacyOwnedInV812',
        CanonicalAudioUploadEvidenceStatus.BLOCKED, blockers, diagnostics)
    }

    if (isRetryDrainer(trigger) && !retryTruth.hasExistingEligibleRetry) {
      blockers.push(CanonicalAudioUploadEvidenceBlocker.RETRY_DRAINER_FRESH_JOB_SUPPRESSED)
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RETRY_DRAINER_FRESH_JOB_SUPPRESSED)
      return CanonicalAudioUploadCutoverCandidate.make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
        trigger, CanonicalAudioUploadActionKind.UNSUPPORTED,
        'retryDrainerCannotCreateFreshAudioUploadJob',
        CanonicalAudioUploadEvidenceStatus.BLOCKED, blockers, diagnostics)
    }

    if (!localTruth.audioAvailable) {
      blockers.push(CanonicalAudioUploadEvidenceBlocker.LOCAL_AUDIO_MISSING)
    }
    if (!localTruth.contentHash) {
      blockers.push(CanonicalAudioUploadEvidenceBlocker.LOCAL_HASH_UNAVAILABLE)
    }
    if (localTruth.byteSize === null) {
      blockers.push(CanonicalAudioUploadEvidenceBlocker.LOCAL_BYTE_SIZE_UNAVAILABLE)
    }

    const peerNoOpProof = peerTruth.peerTruthSufficientForNoOp(localTruth)
    if (ledgerTruth.phase === CanonicalAudioUploadLedgerPhase.COMPLETED && !peerNoOpProof) {
      blockers.push(CanonicalAudioUploadEvidenceBlocker.COMPLETED_LEDGER_WITHOUT_PEER_MATCH)
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_COMPLETED_LEDGER_REJECTED_AS_NO_OP)
    }
    if (ledgerTruth.metadataUploaded || peerTruth.metadataUploaded) {
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_METADATA_UPLOADED_REJECTED_AS_AUDIO_PROOF)
      if (!peerNoOpProof) {
        blockers.push(CanonicalAudioUploadEvidenceBlocker.METADATA_UPLOADED_NOT_AUDIO_PROOF)
      }
    }
    if (ledgerTruth.receiveRecordExists || peerTruth.receiveRecordExists) {
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RECEIVE_RECORD_REJECTED_AS_AUDIO_PROOF)
      if (!peerNoOpProof) {
        blockers.push(CanonicalAudioUploadEvidenceBlocker.RECEIVE_RECORD_NOT_AUDIO_PROOF)
      }
    }
    if (ledgerTruth.uiUploaded || peerTruth.uiUploaded) {
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_UI_UPLOADED_REJECTED_AS_AUDIO_PROOF)
      if (!peerNoOpProof) {
        blockers.push(CanonicalAudioUploadEvidenceBlocker.UI_UPLOADED_NOT_AUDIO_PROOF)
      }
    }

    if (peerNoOpProof && localTruth.sufficientForUploadCandidate) {
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_PEER_SAME_NO_OP)
      return CanonicalAudioUploadCutoverCandidate.make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
        trigger, CanonicalAudioUploadActionKind.AUDIO_UPLOAD_NO_OP, 'peerAudioHashAndSizeMatch',
        CanonicalAudioUploadEvidenceStatus.COMPLETE, [], diagnostics)
    }

    if (!localTruth.sufficientForUploadCandidate) {
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_EVIDENCE_BLOCKED)
      return CanonicalAudioUploadCutoverCandidate.make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
        trigger, CanonicalAudioUploadActionKind.UNSUPPORTED, 'localAudioTruthIncomplete',
        CanonicalAudioUploadEvidenceStatus.BLOCKED, blockers, diagnostics)
    }

    switch (peerTruth.state) {
      case CanonicalAudioUploadPeerState.MISSING:
      case CanonicalAudioUploadPeerState.METADATA_ONLY:
        return CanonicalAudioUploadCutoverCandidate.make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
          trigger, CanonicalAudioUploadActionKind.AUDIO_UPLOAD_CANARY_CANDIDATE,
          peerTruth.state === CanonicalAudioUploadPeerState.MISSING ? 'peerAudioMissing' :
            'peerMetadataOnlyAudioMissing',
          blockers.length === 0 ? CanonicalAudioUploadEvidenceStatus.COMPLETE :
            CanonicalAudioUploadEvidenceStatus.BLOCKED, blockers, diagnostics)
      case CanonicalAudioUploadPeerState.UNKNOWN:
        blockers.push(CanonicalAudioUploadEvidenceBlocker.PEER_UNKNOWN)
        diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_PEER_UNKNOWN_DEFERRED)
        return CanonicalAudioUploadCutoverCandidate.make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
          trigger, CanonicalAudioUploadActionKind.AUDIO_UPLOAD_DEFERRED_PEER_UNKNOWN,
          'peerAudioUnknownIsDeferredInV812',
          CanonicalAudioUploadEvidenceStatus.DEFERRED, blockers, diagnostics)
      case CanonicalAudioUploadPeerState.AVAILABLE:
        if (!peerTruth.contentHash || peerTruth.byteSize === null) {
          blockers.push(CanonicalAudioUploadEvidenceBlocker.PEER_TRUTH_MISSING)
          diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_EVIDENCE_BLOCKED)
          return CanonicalAudioUploadCutoverCandidate.make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
            trigger, CanonicalAudioUploadActionKind.UNSUPPORTED,
            'peerAvailableWithoutHashOrSizeCannotNoOp',
            CanonicalAudioUploadEvidenceStatus.BLOCKED, blockers, diagnostics)
        }
        blockers.push(CanonicalAudioUploadEvidenceBlocker.DIFFERENT_HASH_OR_SIZE)
        diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_CONFLICT_DETECTED)
        return CanonicalAudioUploadCutoverCandidate.make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
          trigger, CanonicalAudioUploadActionKind.AUDIO_UPLOAD_CONFLICT_RECORD,
          'peerAudioHashOrSizeDifferent',
          CanonicalAudioUploadEvidenceStatus.CONFLICT, blockers, diagnostics)
      case CanonicalAudioUploadPeerState.DIFFERENT:
      case CanonicalAudioUploadPeerState.DELETED:
        blockers.push(CanonicalAudioUploadEvidenceBlocker.DIFFERENT_HASH_OR_SIZE)
        diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_CONFLICT_DETECTED)
        return CanonicalAudioUploadCutoverCandidate.make(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
          trigger, CanonicalAudioUploadActionKind.AUDIO_UPLOAD_CONFLICT_RECORD,
          peerTruth.state === CanonicalAudioUploadPeerState.DELETED ?
            'peerAudioDeletedRequiresConflictNotOverwrite' : 'peerAudioDifferent',
          CanonicalAudioUploadEvidenceStatus.CONFLICT, blockers, diagnostics)
    }
  }

  static candidates(
    localManifest: CanonicalManifest,
    peerManifest: CanonicalManifest | null,
    trigger: CanonicalAudioUploadTriggerSource,
    ledgerTruths: Record<string, CanonicalAudioUploadLedgerTruth> = {},
    retryTruths: Record<string, CanonicalAudioUploadRetryTruth> = {}
  ): CanonicalAudioUploadCutoverCandidate[] {
    const peerMap: Record<string, CanonicalRecordingObject> = {}
    if (peerManifest) {
      for (const obj of peerManifest.objects) {
        peerMap[obj.objectID] = obj
      }
    }
    return localManifest.objects
      .filter((obj: CanonicalRecordingObject) => obj.audioArtifact !== null && obj.audioArtifact !== undefined)
      .map((obj: CanonicalRecordingObject) => {
        const objectID = obj.objectID
        return CanonicalAudioUploadCutoverCandidate.evaluate(objectID,
          CanonicalAudioUploadLocalTruth.from(obj),
          !peerManifest ? new CanonicalAudioUploadPeerTruth(CanonicalAudioUploadPeerState.UNKNOWN, null, null, false, false,
            false, 'peerManifestUnavailable')
            : CanonicalAudioUploadPeerTruth.from(peerMap[objectID] ?? null),
          ledgerTruths[objectID] ?? new CanonicalAudioUploadLedgerTruth(),
          retryTruths[objectID] ?? new CanonicalAudioUploadRetryTruth(),
          trigger)
      })
  }

  private static make(
    objectID: string,
    localTruth: CanonicalAudioUploadLocalTruth,
    peerTruth: CanonicalAudioUploadPeerTruth,
    ledgerTruth: CanonicalAudioUploadLedgerTruth,
    retryTruth: CanonicalAudioUploadRetryTruth,
    trigger: CanonicalAudioUploadTriggerSource,
    actionKind: CanonicalAudioUploadActionKind,
    reason: string,
    status: CanonicalAudioUploadEvidenceStatus,
    blockers: CanonicalAudioUploadEvidenceBlocker[],
    diagnostics: CanonicalAudioUploadDiagnosticKind[]
  ): CanonicalAudioUploadCutoverCandidate {
    return new CanonicalAudioUploadCutoverCandidate(objectID, localTruth, peerTruth, ledgerTruth, retryTruth,
      trigger, actionKind, reason, status, blockers, diagnostics)
  }
}

export class CanonicalAudioUploadEvidenceReport {
  candidates: CanonicalAudioUploadCutoverCandidate[]
  completeCount: number
  blockedCount: number
  deferredCount: number
  conflictCount: number
  diagnosticsRedacted: boolean
  diagnosticsSummary: string

  constructor(candidates: CanonicalAudioUploadCutoverCandidate[]) {
    this.candidates = [...candidates].sort((a, b) => a.objectID.localeCompare(b.objectID))
    this.completeCount = candidates.filter(
      (c: CanonicalAudioUploadCutoverCandidate) => c.evidenceStatus === CanonicalAudioUploadEvidenceStatus.COMPLETE).length
    this.blockedCount = candidates.filter(
      (c: CanonicalAudioUploadCutoverCandidate) => c.evidenceStatus === CanonicalAudioUploadEvidenceStatus.BLOCKED).length
    this.deferredCount = candidates.filter(
      (c: CanonicalAudioUploadCutoverCandidate) => c.evidenceStatus === CanonicalAudioUploadEvidenceStatus.DEFERRED).length
    this.conflictCount = candidates.filter(
      (c: CanonicalAudioUploadCutoverCandidate) => c.evidenceStatus === CanonicalAudioUploadEvidenceStatus.CONFLICT).length
    this.diagnosticsRedacted = true
    this.diagnosticsSummary = [
      'candidateCount=' + candidates.length,
      'complete=' + this.completeCount,
      'blocked=' + this.blockedCount,
      'deferred=' + this.deferredCount,
      'conflict=' + this.conflictCount,
      'diagnosticsRedacted=true'
    ].join(',')
  }
}

export class CanonicalAudioUploadCutoverEvidence {
  localAudioDescriptorsReviewed: boolean
  peerAudioDescriptorsReviewed: boolean
  shadowReceiverRehearsalPassed: boolean
  noCommitObserved: boolean
  readSideProjectionObserved: boolean
  abortRollbackPolicyDocumented: boolean
  legacyFallbackAvailable: boolean
  evidenceReport: CanonicalAudioUploadEvidenceReport | null

  constructor(
    localAudioDescriptorsReviewed: boolean = false,
    peerAudioDescriptorsReviewed: boolean = false,
    shadowReceiverRehearsalPassed: boolean = false,
    noCommitObserved: boolean = false,
    readSideProjectionObserved: boolean = false,
    abortRollbackPolicyDocumented: boolean = false,
    legacyFallbackAvailable: boolean = true,
    evidenceReport: CanonicalAudioUploadEvidenceReport | null = null
  ) {
    this.localAudioDescriptorsReviewed = localAudioDescriptorsReviewed
    this.peerAudioDescriptorsReviewed = peerAudioDescriptorsReviewed
    this.shadowReceiverRehearsalPassed = shadowReceiverRehearsalPassed
    this.noCommitObserved = noCommitObserved
    this.readSideProjectionObserved = readSideProjectionObserved
    this.abortRollbackPolicyDocumented = abortRollbackPolicyDocumented
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.evidenceReport = evidenceReport
  }

  static passing(report: CanonicalAudioUploadEvidenceReport | null = null): CanonicalAudioUploadCutoverEvidence {
    return new CanonicalAudioUploadCutoverEvidence(true, true, true, true, true, true, true, report)
  }

  get isPassing(): boolean {
    return this.localAudioDescriptorsReviewed && this.peerAudioDescriptorsReviewed &&
      this.shadowReceiverRehearsalPassed && this.noCommitObserved && this.readSideProjectionObserved &&
      this.abortRollbackPolicyDocumented && this.legacyFallbackAvailable
  }
}

export class CanonicalAudioUploadCanaryPolicy {
  requestedStage: CanonicalAudioUploadCanaryStage
  allowsTestOnlyFutureStage: boolean
  requireEvidence: boolean
  maxDiagnosticsEvents: number

  constructor(
    requestedStage: CanonicalAudioUploadCanaryStage = CanonicalAudioUploadCanaryStage.DISABLED,
    allowsTestOnlyFutureStage: boolean = false,
    requireEvidence: boolean = true,
    maxDiagnosticsEvents: number = 200
  ) {
    this.requestedStage = requestedStage
    this.allowsTestOnlyFutureStage = allowsTestOnlyFutureStage
    this.requireEvidence = requireEvidence
    this.maxDiagnosticsEvents = Math.max(1, maxDiagnosticsEvents)
  }

  static get disabled(): CanonicalAudioUploadCanaryPolicy {
    return new CanonicalAudioUploadCanaryPolicy()
  }

  get canaryMaxObjectsPerSyncRun(): number {
    const budget = canaryBudgetForStage(this.requestedStage)
    return budget === Number.MAX_SAFE_INTEGER ? Number.MAX_SAFE_INTEGER : Math.max(0, budget)
  }

  get productionCommitAllowedInV812(): boolean {
    return false
  }
}

export class CanonicalAudioUploadCutoverGate {
  allowed: boolean
  productionUploadAllowed: boolean
  shadowOnlyAllowed: boolean
  mode: CanonicalCutoverMode
  canaryStage: CanonicalAudioUploadCanaryStage
  failures: CanonicalAudioUploadCutoverFailure[]
  reason: string

  constructor(
    allowed: boolean,
    productionUploadAllowed: boolean = false,
    shadowOnlyAllowed: boolean = false,
    mode: CanonicalCutoverMode,
    canaryStage: CanonicalAudioUploadCanaryStage,
    failures: CanonicalAudioUploadCutoverFailure[] = [],
    reason: string
  ) {
    this.allowed = allowed
    this.productionUploadAllowed = productionUploadAllowed
    this.shadowOnlyAllowed = shadowOnlyAllowed
    this.mode = mode
    this.canaryStage = canaryStage
    this.failures = [...new Set(failures)].sort((a, b) => a.localeCompare(b))
    this.reason = reason
  }
}

export class CanonicalAudioUploadCutoverRunner {
  constructor() {}

  evaluateGate(
    mode: CanonicalCutoverMode,
    policy: CanonicalAudioUploadCanaryPolicy,
    token: CanonicalCutoverToken | null,
    evidence: CanonicalAudioUploadCutoverEvidence,
    candidates: CanonicalAudioUploadCutoverCandidate[],
    trigger: CanonicalAudioUploadTriggerSource
  ): CanonicalAudioUploadCutoverGate {
    let failures: CanonicalAudioUploadCutoverFailure[] = []
    const eligibleCandidates = candidates.filter(
      (c: CanonicalAudioUploadCutoverCandidate) => c.canaryEligibleInShadowOnlyModel)

    if (mode === CanonicalCutoverMode.DISABLED || policy.requestedStage === CanonicalAudioUploadCanaryStage.DISABLED) {
      failures.push(CanonicalAudioUploadCutoverFailure.DISABLED)
    }
    if (mode === CanonicalCutoverMode.GUARDED_EXECUTE_COMMIT || mode === CanonicalCutoverMode.CANARY ||
      requestsProductionCanary(policy.requestedStage)) {
      failures.push(CanonicalAudioUploadCutoverFailure.PRODUCTION_COMMIT_BLOCKED_V812)
    }
    if (requestsProductionCanary(policy.requestedStage) && !policy.allowsTestOnlyFutureStage) {
      failures.push(CanonicalAudioUploadCutoverFailure.FUTURE_CANARY_STAGE_BLOCKED)
    }
    if (mode === CanonicalCutoverMode.CANARY && policy.canaryMaxObjectsPerSyncRun === 0) {
      failures.push(CanonicalAudioUploadCutoverFailure.CANARY_BUDGET_ZERO)
    }
    if (mode === CanonicalCutoverMode.GUARDED_EXECUTE_COMMIT && !token) {
      failures.push(CanonicalAudioUploadCutoverFailure.MISSING_CUTOVER_TOKEN)
    }
    if (policy.requireEvidence && !evidence.isPassing) {
      failures.push(CanonicalAudioUploadCutoverFailure.INSUFFICIENT_EVIDENCE)
    }
    if (candidates.some(
      (c: CanonicalAudioUploadCutoverCandidate) => c.evidenceBlockers.length > 0)) {
      failures.push(CanonicalAudioUploadCutoverFailure.CANDIDATE_EVIDENCE_BLOCKED)
    }
    if (eligibleCandidates.length === 0 && mode !== CanonicalCutoverMode.DISABLED) {
      failures.push(CanonicalAudioUploadCutoverFailure.NO_ELIGIBLE_CANDIDATES)
    }
    if (isExplicitManualUploadButton(trigger)) {
      failures.push(CanonicalAudioUploadCutoverFailure.MANUAL_USER_ACTION_LEGACY_OWNED)
    }

    const shadowAllowed = failures.length === 0 &&
      (mode === CanonicalCutoverMode.SHADOW_ONLY || mode === CanonicalCutoverMode.GUARDED_EXECUTE_NO_COMMIT)
    return new CanonicalAudioUploadCutoverGate(shadowAllowed, false, shadowAllowed, mode,
      policy.requestedStage, failures,
      failures.length === 0 ? 'shadowOnlyNoCommitAllowed' : failures.join(','))
  }
}

export class CanonicalAudioUploadNoCommitCandidate {
  cutoverCandidate: CanonicalAudioUploadCutoverCandidate

  constructor(cutoverCandidate: CanonicalAudioUploadCutoverCandidate) {
    this.cutoverCandidate = cutoverCandidate
  }
}

export class CanonicalAudioUploadNoCommitResult {
  staged: boolean
  objectID: string
  nodeRole: CanonicalAudioUploadNodeRole
  actionKind: CanonicalAudioUploadActionKind
  wouldRequestRoute: string | null
  payloadHashPrefix: string | null
  payloadByteCount: number
  productionUploadSuppressed: boolean
  legacyUploadCoordinatorNotCalled: boolean
  recordingUploadClientNotCalled: boolean
  secureMacUploadClientNotCalled: boolean
  didNotCreateUploadJob: boolean
  didNotWriteInbox: boolean
  didNotWriteReceiveJSON: boolean
  didNotMutateUploadLedger: boolean
  didNotMutateRetryDrainer: boolean

  constructor(candidate: CanonicalAudioUploadCutoverCandidate, nodeRole: CanonicalAudioUploadNodeRole) {
    const payloadHash = CanonicalHash.sha256OfMap({
      'schema': 'canonical-audio-upload-no-commit-v812',
      'objectID': candidate.objectID,
      'action': candidate.actionKind,
      'reason': candidate.reason
    })
    this.staged = candidate.canaryEligibleInShadowOnlyModel ||
      candidate.actionKind === CanonicalAudioUploadActionKind.AUDIO_UPLOAD_NO_OP
    this.objectID = candidate.objectID
    this.nodeRole = nodeRole
    this.actionKind = candidate.actionKind
    this.wouldRequestRoute = candidate.canaryEligibleInShadowOnlyModel ?
      '/upload-recording-audio-session/start' : null
    this.payloadHashPrefix = payloadHash.value.substring(0, 12)
    this.payloadByteCount = candidate.objectID.length + candidate.reason.length
    this.productionUploadSuppressed = true
    this.legacyUploadCoordinatorNotCalled = true
    this.recordingUploadClientNotCalled = true
    this.secureMacUploadClientNotCalled = true
    this.didNotCreateUploadJob = true
    this.didNotWriteInbox = true
    this.didNotWriteReceiveJSON = true
    this.didNotMutateUploadLedger = true
    this.didNotMutateRetryDrainer = true
  }
}

export interface CanonicalAudioUploadNoCommitExecutor {
  stageAudioUploadNoCommit(candidate: CanonicalAudioUploadNoCommitCandidate): CanonicalAudioUploadNoCommitResult
}

export class CanonicalAudioUploadNoCommitRunner {
  constructor() {}

  run(
    mode: CanonicalCutoverMode,
    policy: CanonicalAudioUploadCanaryPolicy,
    token: CanonicalCutoverToken | null,
    evidence: CanonicalAudioUploadCutoverEvidence,
    candidates: CanonicalAudioUploadNoCommitCandidate[],
    trigger: CanonicalAudioUploadTriggerSource,
    nodeRole: CanonicalAudioUploadNodeRole,
    syncRunID: string | null,
    executor: CanonicalAudioUploadNoCommitExecutor
  ): CanonicalAudioUploadCutoverResult {
    const cutoverCandidates = candidates.map(
      (c: CanonicalAudioUploadNoCommitCandidate) => c.cutoverCandidate)
    const gate = new CanonicalAudioUploadCutoverRunner().evaluateGate(mode, policy, token, evidence,
      cutoverCandidates, trigger)
    let diagnostics: CanonicalAudioUploadDiagnostic[] = [
      new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_CUTOVER_GATE_EVALUATED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null,
        gate.allowed ? 'allowed' : 'blocked', gate.reason)
    ]
    diagnostics.push(new CanonicalAudioUploadDiagnostic(
      gate.allowed ? CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_CUTOVER_GATE_ALLOWED :
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_CUTOVER_GATE_BLOCKED,
      syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null,
      gate.allowed ? 'allowed' : 'blocked', gate.failures.join(',')))

    let results: CanonicalAudioUploadNoCommitResult[]
    if (mode === CanonicalCutoverMode.GUARDED_EXECUTE_NO_COMMIT ||
      mode === CanonicalCutoverMode.SHADOW_ONLY) {
      diagnostics.push(new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_NO_COMMIT_STARTED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null,
        'candidateCount=' + candidates.length, 'productionUploadSuppressed'))
      results = candidates.map((c: CanonicalAudioUploadNoCommitCandidate) => executor.stageAudioUploadNoCommit(c))
      diagnostics.push(new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_NO_COMMIT_COMPLETED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null,
        'stagedCount=' + results.filter((r: CanonicalAudioUploadNoCommitResult) => r.staged).length,
        'noCommitOnly'))
    } else {
      results = []
    }

    diagnostics.push(
      new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_PRODUCTION_COORDINATOR_SUPPRESSED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null, 'true'),
      new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RECORDING_UPLOAD_CLIENT_SUPPRESSED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null, 'true'),
      new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_SECURE_MAC_UPLOAD_CLIENT_SUPPRESSED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null, 'true'),
      new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_INBOX_WRITE_SUPPRESSED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null, 'true'),
      new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RECEIVE_JSON_WRITE_SUPPRESSED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null, 'true'),
      new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_LEDGER_MUTATION_SUPPRESSED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null, 'true'),
      new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RETRY_DRAINER_MUTATION_SUPPRESSED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null, 'true'),
      new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_LEGACY_FALLBACK_PRESERVED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null, 'true')
    )

    return new CanonicalAudioUploadCutoverResult(gate, cutoverCandidates, results, diagnostics, true, false)
  }
}

export class CanonicalAudioUploadCutoverResult {
  gate: CanonicalAudioUploadCutoverGate
  candidates: CanonicalAudioUploadCutoverCandidate[]
  noCommitResults: CanonicalAudioUploadNoCommitResult[]
  diagnostics: CanonicalAudioUploadDiagnostic[]
  legacyFallbackPreserved: boolean
  runtimeSwitchEnabled: boolean
  calledProductionUploadCoordinator: boolean
  calledRecordingUploadClient: boolean
  calledSecureMacUploadClient: boolean
  wroteProductionInbox: boolean
  wroteReceiveJSON: boolean
  createdUploadJob: boolean
  mutatedUploadLedger: boolean
  mutatedRetryDrainer: boolean
  suppressedLegacyDuplicate: boolean

  constructor(
    gate: CanonicalAudioUploadCutoverGate,
    candidates: CanonicalAudioUploadCutoverCandidate[] = [],
    noCommitResults: CanonicalAudioUploadNoCommitResult[] = [],
    diagnostics: CanonicalAudioUploadDiagnostic[] = [],
    legacyFallbackPreserved: boolean = true,
    runtimeSwitchEnabled: boolean = false
  ) {
    this.gate = gate
    this.candidates = candidates
    this.noCommitResults = noCommitResults
    this.diagnostics = diagnostics
    this.legacyFallbackPreserved = legacyFallbackPreserved
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.calledProductionUploadCoordinator = false
    this.calledRecordingUploadClient = false
    this.calledSecureMacUploadClient = false
    this.wroteProductionInbox = false
    this.wroteReceiveJSON = false
    this.createdUploadJob = false
    this.mutatedUploadLedger = false
    this.mutatedRetryDrainer = false
    this.suppressedLegacyDuplicate = false
  }
}

export class CanonicalAudioUploadShadowReceiver {
  rootToken: CanonicalRootToken
  receiver: CanonicalShadowUploadReceiver

  constructor(rootToken: CanonicalRootToken = new CanonicalRootToken('canonical-audio-upload-shadow')) {
    this.rootToken = rootToken
    this.receiver = new CanonicalShadowUploadReceiver(rootToken)
  }

  get canonicalReceiver(): CanonicalShadowUploadReceiver {
    return this.receiver
  }

  async seed(reference: CanonicalFileReference, bytes: Uint8Array): Promise<void> {
    await this.receiver.seed(reference, bytes)
  }

  async read(reference: CanonicalFileReference): Promise<CanonicalFileReadResult> {
    return this.receiver.read(reference)
  }
}

export class CanonicalAudioUploadShadowRehearsalInput {
  objectID: string
  logicalPathToken: string
  bytes: Uint8Array
  chunkSize: number
  declaredTotalHash: CanonicalHash | null
  existingReceiverBytes: Uint8Array | null
  simulateInterruptionAfterFirstChunk: boolean

  constructor(
    objectID: string, logicalPathToken: string, bytes: Uint8Array,
    chunkSize: number = 2 * 1024 * 1024, declaredTotalHash: CanonicalHash | null = null,
    existingReceiverBytes: Uint8Array | null = null, simulateInterruptionAfterFirstChunk: boolean = false
  ) {
    const trimmed = objectID.trim()
    this.objectID = trimmed.length === 0 ? 'object:unknown' : trimmed
    this.logicalPathToken = CanonicalProjectionContract.safeLogicalPathToken(logicalPathToken) ??
      'audio/' + this.objectID + '.m4a'
    this.bytes = bytes
    this.chunkSize = Math.max(1, chunkSize)
    this.declaredTotalHash = declaredTotalHash
    this.existingReceiverBytes = existingReceiverBytes
    this.simulateInterruptionAfterFirstChunk = simulateInterruptionAfterFirstChunk
  }
}

export class CanonicalAudioUploadShadowRehearsalResult {
  shadowResult: CanonicalShadowUploadResult
  productionUploadSuppressed: boolean
  calledProductionUploadCoordinator: boolean
  calledRecordingUploadClient: boolean
  calledSecureMacUploadClient: boolean
  wroteProductionInbox: boolean
  wroteReceiveJSON: boolean
  diagnostics: CanonicalAudioUploadDiagnostic[]

  constructor(
    shadowResult: CanonicalShadowUploadResult,
    objectID: string,
    syncRunID: string | null,
    trigger: CanonicalAudioUploadTriggerSource,
    nodeRole: CanonicalAudioUploadNodeRole
  ) {
    const safeObjectID = (() => {
      const trimmed = objectID.trim()
      return trimmed.length === 0 ? 'object:unknown' : trimmed
    })()
    this.shadowResult = shadowResult
    this.productionUploadSuppressed = true
    this.calledProductionUploadCoordinator = false
    this.calledRecordingUploadClient = false
    this.calledSecureMacUploadClient = false
    this.wroteProductionInbox = false
    this.wroteReceiveJSON = false
    this.diagnostics = [
      new CanonicalAudioUploadDiagnostic(
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_SHADOW_REHEARSAL_COMPLETED,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, safeObjectID, null, null,
        CanonicalAudioUploadActionKind.AUDIO_UPLOAD_SHADOW_REHEARSAL,
        shadowResult.completed ? 'completed' : 'failed', shadowResult.divergence),
      new CanonicalAudioUploadDiagnostic(
        shadowResult.wroteShadowReceiver ?
          CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_SHADOW_RECEIVER_WROTE :
          CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_SHADOW_RECEIVER_NO_OP,
        syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, safeObjectID, null, null,
        CanonicalAudioUploadActionKind.AUDIO_UPLOAD_SHADOW_REHEARSAL,
        shadowResult.wroteShadowReceiver ? 'shadowReceiverWrote' : 'shadowReceiverNoOp',
        shadowResult.divergence)
    ]
  }
}

export class CanonicalAudioUploadShadowRehearsal {
  constructor() {}

  async run(
    input: CanonicalAudioUploadShadowRehearsalInput,
    receiver: CanonicalAudioUploadShadowReceiver = new CanonicalAudioUploadShadowReceiver(),
    syncRunID: string | null = null,
    trigger: CanonicalAudioUploadTriggerSource = CanonicalAudioUploadTriggerSource.ORDINARY_SYNC,
    nodeRole: CanonicalAudioUploadNodeRole = CanonicalAudioUploadNodeRole.TEST_HARNESS
  ): Promise<CanonicalAudioUploadShadowRehearsalResult> {
    const reference = new CanonicalFileReference(receiver.rootToken, input.logicalPathToken,
      CanonicalProjectionContract.artifactID(input.objectID, CanonicalArtifactKind.AUDIO),
      CanonicalArtifactKind.AUDIO)
    const result = await new CanonicalShadowUploadRehearsal().run(
      new CanonicalShadowUploadRehearsalInput(input.objectID, reference, input.bytes, input.chunkSize,
        input.declaredTotalHash, input.existingReceiverBytes,
        input.simulateInterruptionAfterFirstChunk),
      receiver.canonicalReceiver)
    return new CanonicalAudioUploadShadowRehearsalResult(result, input.objectID, syncRunID, trigger, nodeRole)
  }
}

export class CanonicalAudioUploadAbortPlan {
  phase: CanonicalAudioUploadAbortPhase
  canCancelSession: boolean
  canDeleteProductionAudio: boolean
  shouldPreserveLegacyFallback: boolean
  reason: string

  static plan(phase: CanonicalAudioUploadAbortPhase): CanonicalAudioUploadAbortPlan {
    switch (phase) {
      case CanonicalAudioUploadAbortPhase.BEFORE_START:
        return new CanonicalAudioUploadAbortPlan(phase, true, false, true, 'abortBeforeStartNoProductionState')
      case CanonicalAudioUploadAbortPhase.BEFORE_FINALIZE:
        return new CanonicalAudioUploadAbortPlan(phase, true, false, true,
          'abortBeforeFinalizeCanCancelShadowSessionOnly')
      case CanonicalAudioUploadAbortPhase.AFTER_FINALIZE:
        return new CanonicalAudioUploadAbortPlan(phase, false, false, true,
          'postFinalizeRollbackNeverDeletesAudio')
    }
  }

  constructor(
    phase: CanonicalAudioUploadAbortPhase, canCancelSession: boolean, canDeleteProductionAudio: boolean,
    shouldPreserveLegacyFallback: boolean, reason: string
  ) {
    this.phase = phase
    this.canCancelSession = canCancelSession
    this.canDeleteProductionAudio = canDeleteProductionAudio
    this.shouldPreserveLegacyFallback = shouldPreserveLegacyFallback
    this.reason = reason
  }
}

export class CanonicalAudioUploadCleanupResult {
  shadowPartialSessionCleaned: boolean
  productionAudioDeleted: boolean
  receiveJSONDeleted: boolean
  legacyFallbackPreserved: boolean
  reason: string

  static shadowOnlyCleanup(reason: string): CanonicalAudioUploadCleanupResult {
    return new CanonicalAudioUploadCleanupResult(true, false, false, true, reason)
  }

  constructor(
    shadowPartialSessionCleaned: boolean, productionAudioDeleted: boolean, receiveJSONDeleted: boolean,
    legacyFallbackPreserved: boolean, reason: string
  ) {
    this.shadowPartialSessionCleaned = shadowPartialSessionCleaned
    this.productionAudioDeleted = productionAudioDeleted
    this.receiveJSONDeleted = receiveJSONDeleted
    this.legacyFallbackPreserved = legacyFallbackPreserved
    this.reason = reason
  }
}

export class CanonicalAudioUploadRollbackPolicy {
  preFinalizeAbort: CanonicalAudioUploadAbortPlan
  postFinalizeRollback: CanonicalAudioUploadAbortPlan
  cleanupShadowPartialSessions: boolean
  neverDeleteProductionAudio: boolean
  neverDeleteReceiveJSON: boolean

  constructor() {
    this.preFinalizeAbort = CanonicalAudioUploadAbortPlan.plan(CanonicalAudioUploadAbortPhase.BEFORE_FINALIZE)
    this.postFinalizeRollback = CanonicalAudioUploadAbortPlan.plan(CanonicalAudioUploadAbortPhase.AFTER_FINALIZE)
    this.cleanupShadowPartialSessions = true
    this.neverDeleteProductionAudio = true
    this.neverDeleteReceiveJSON = true
  }

  cleanupPartialShadowSession(
    reason: string = 'shadowPartialSessionCleanupOnly'): CanonicalAudioUploadCleanupResult {
    return CanonicalAudioUploadCleanupResult.shadowOnlyCleanup(reason)
  }
}

export class CanonicalAudioUploadReadSideParallelProjection {
  candidateCount: number
  equivalent: boolean
  mutatedUI: boolean
  wroteUIState: boolean
  createdUploadJob: boolean
  diagnostics: CanonicalAudioUploadDiagnostic[]

  static project(
    candidates: CanonicalAudioUploadCutoverCandidate[], syncRunID: string | null,
    trigger: CanonicalAudioUploadTriggerSource, nodeRole: CanonicalAudioUploadNodeRole
  ): CanonicalAudioUploadReadSideParallelProjection {
    const diverged = candidates.some(
      (c: CanonicalAudioUploadCutoverCandidate) => c.evidenceStatus === CanonicalAudioUploadEvidenceStatus.CONFLICT)
    return new CanonicalAudioUploadReadSideParallelProjection(
      candidates.length, !diverged, false, false, false,
      [
        new CanonicalAudioUploadDiagnostic(
          CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_READ_SIDE_PROJECTION_STARTED,
          syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null,
          'candidateCount=' + candidates.length),
        new CanonicalAudioUploadDiagnostic(
          diverged ? CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_READ_SIDE_PROJECTION_DIVERGED :
            CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_READ_SIDE_PROJECTION_EQUIVALENT,
          syncRunID, trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, null, null, null, null,
          diverged ? 'diverged' : 'equivalent')
      ])
  }

  constructor(
    candidateCount: number, equivalent: boolean, mutatedUI: boolean, wroteUIState: boolean,
    createdUploadJob: boolean, diagnostics: CanonicalAudioUploadDiagnostic[]
  ) {
    this.candidateCount = candidateCount
    this.equivalent = equivalent
    this.mutatedUI = mutatedUI
    this.wroteUIState = wroteUIState
    this.createdUploadJob = createdUploadJob
    this.diagnostics = diagnostics
  }
}

export class CanonicalAudioUploadCutoverAppSeamPolicy {
  recordDiagnostics: boolean
  maxDiagnosticsEvents: number
  canaryPolicy: CanonicalAudioUploadCanaryPolicy

  constructor(
    recordDiagnostics: boolean = true, maxDiagnosticsEvents: number = 200,
    canaryPolicy: CanonicalAudioUploadCanaryPolicy = CanonicalAudioUploadCanaryPolicy.disabled
  ) {
    this.recordDiagnostics = recordDiagnostics
    this.maxDiagnosticsEvents = Math.max(1, maxDiagnosticsEvents)
    this.canaryPolicy = canaryPolicy
  }
}

export class CanonicalAudioUploadCutoverAppSeamConfiguration {
  isEnabled: boolean
  mode: CanonicalCutoverAppSeamMode
  policy: CanonicalAudioUploadCutoverAppSeamPolicy
  evidence: CanonicalAudioUploadCutoverEvidence
  cutoverToken: CanonicalCutoverToken | null

  constructor(
    isEnabled: boolean = false, mode: CanonicalCutoverAppSeamMode = CanonicalCutoverAppSeamMode.DISABLED,
    policy: CanonicalAudioUploadCutoverAppSeamPolicy = new CanonicalAudioUploadCutoverAppSeamPolicy(),
    evidence: CanonicalAudioUploadCutoverEvidence = new CanonicalAudioUploadCutoverEvidence(),
    cutoverToken: CanonicalCutoverToken | null = null
  ) {
    this.isEnabled = isEnabled
    this.mode = isEnabled ? mode : CanonicalCutoverAppSeamMode.DISABLED
    this.policy = policy
    this.evidence = evidence
    this.cutoverToken = cutoverToken
  }

  static get disabled(): CanonicalAudioUploadCutoverAppSeamConfiguration {
    return new CanonicalAudioUploadCutoverAppSeamConfiguration()
  }

  static enabled(
    mode: CanonicalCutoverAppSeamMode = CanonicalCutoverAppSeamMode.GUARDED_EXECUTE_NO_COMMIT,
    policy: CanonicalAudioUploadCutoverAppSeamPolicy = new CanonicalAudioUploadCutoverAppSeamPolicy(
      new CanonicalAudioUploadCanaryPolicy(CanonicalAudioUploadCanaryStage.SHADOW_ONLY)),
    evidence: CanonicalAudioUploadCutoverEvidence = new CanonicalAudioUploadCutoverEvidence(),
    cutoverToken: CanonicalCutoverToken | null = null
  ): CanonicalAudioUploadCutoverAppSeamConfiguration {
    return new CanonicalAudioUploadCutoverAppSeamConfiguration(true, mode, policy, evidence, cutoverToken)
  }

  get effectiveMode(): CanonicalCutoverAppSeamMode {
    return this.isEnabled ? this.mode : CanonicalCutoverAppSeamMode.DISABLED
  }

  get cutoverMode(): CanonicalCutoverMode {
    switch (this.effectiveMode) {
      case CanonicalCutoverAppSeamMode.DISABLED:
        return CanonicalCutoverMode.DISABLED
      case CanonicalCutoverAppSeamMode.GUARDED_EXECUTE_NO_COMMIT:
        return CanonicalCutoverMode.GUARDED_EXECUTE_NO_COMMIT
      case CanonicalCutoverAppSeamMode.GUARDED_EXECUTE_COMMIT:
      case CanonicalCutoverAppSeamMode.PRODUCTION_EXECUTE:
        return CanonicalCutoverMode.GUARDED_EXECUTE_COMMIT
      case CanonicalCutoverAppSeamMode.CANARY_COMMIT:
        return CanonicalCutoverMode.CANARY
    }
  }
}
