/**
 * CanonicalExecutionShadow — shadow file-store, upload rehearsal, apply rehearsal.
 * Ported from Rokurics-Apple (CanonicalExecutionShadow.swift).
 * Provides shadow (non-production) execution context for dry-run verification.
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalManifest, CanonicalRecordingObject,
  CanonicalArtifact, CanonicalArtifactKind,
  CanonicalSyncRuntimeDiagnostic, CanonicalSyncRuntimeDiagnosticKind,
  nilIfEmpty, hashPrefix
} from './CanonicalCore'
import {
  CanonicalApplyAction, CanonicalApplyActionKind, CanonicalApplyTarget,
  CanonicalApplyFailureReason, CanonicalApplyPlan
} from './CanonicalApplyPlan'
import {
  CanonicalApplyExecutionRecord, CanonicalApplyExecutionStatus,
  CanonicalApplyExecutionReport, CanonicalApplyRuntimeContext,
  CanonicalApplyExecutor, CanonicalRootToken, CanonicalFileReference,
  CanonicalFileWriteIntent, CanonicalFileReadRequest, CanonicalFileReadResult,
  CanonicalFileWriteResult, CanonicalFileWriteDisposition,
  CanonicalFilePurpose, CanonicalFileStorePort, InMemoryCanonicalFileStore,
  CanonicalProductionRedaction
} from './CanonicalApplyRuntime'

// =============================================================================
// SECTION 1: Shadow Root Kinds & Bindings
// =============================================================================

export enum CanonicalShadowRootKind {
  TEMPORARY = 'temporary',
  SHADOW_COPY = 'shadowCopy',
  PRODUCTION_ROOT_REJECTED = 'productionRootRejected'
}

export class CanonicalShadowRootBinding {
  rootToken: CanonicalRootToken
  rootKind: CanonicalShadowRootKind
  rootURL: string | null
  prohibitedProductionRootURL: string | null

  constructor(
    rootToken: CanonicalRootToken,
    rootKind: CanonicalShadowRootKind,
    rootURL: string | null = null,
    prohibitedProductionRootURL: string | null = null
  ) {
    this.rootToken = rootToken
    this.rootKind = rootKind
    this.rootURL = rootURL ? rootURL : null
    this.prohibitedProductionRootURL = prohibitedProductionRootURL ? prohibitedProductionRootURL : null
  }

  validatedShadowRootURL(): string | null {
    if (this.rootKind === CanonicalShadowRootKind.PRODUCTION_ROOT_REJECTED) {
      return null
    }
    return this.rootURL
  }
}

// =============================================================================
// SECTION 2: Shadow Copy Policy
// =============================================================================

export class CanonicalShadowCopyPolicy {
  maxBytes: number
  allowMetadataBytes: boolean
  allowArtifactBytes: boolean
  allowTombstoneMarkers: boolean
  noPhysicalDelete: boolean

  constructor(
    maxBytes: number = 8 * 1024 * 1024,
    allowMetadataBytes: boolean = true,
    allowArtifactBytes: boolean = true,
    allowTombstoneMarkers: boolean = true,
    noPhysicalDelete: boolean = true
  ) {
    this.maxBytes = Math.max(0, maxBytes)
    this.allowMetadataBytes = allowMetadataBytes
    this.allowArtifactBytes = allowArtifactBytes
    this.allowTombstoneMarkers = allowTombstoneMarkers
    this.noPhysicalDelete = noPhysicalDelete
  }
}

// =============================================================================
// SECTION 3: Shadow Copy Entry & Manifest
// =============================================================================

export class CanonicalShadowCopyEntry {
  reference: CanonicalFileReference
  purpose: CanonicalFilePurpose
  byteSize: number
  contentHashPrefix: string | null
  copiedToShadowRoot: boolean
  reason: string | null

  constructor(
    reference: CanonicalFileReference,
    purpose: CanonicalFilePurpose,
    byteSize: number,
    contentHash: CanonicalHash | null = null,
    copiedToShadowRoot: boolean,
    reason: string | null = null
  ) {
    this.reference = reference
    this.purpose = purpose
    this.byteSize = Math.max(0, byteSize)
    this.contentHashPrefix = contentHash
      ? CanonicalProductionRedaction.hashPrefix(contentHash.value)
      : null
    this.copiedToShadowRoot = copiedToShadowRoot
    this.reason = CanonicalShadowMigrationRedaction.safeText(reason)
  }

  get id(): string {
    return [this.reference.rootToken.rawValue, this.reference.logicalPathToken, this.purpose].join('|')
  }
}

export class CanonicalShadowCopyManifest {
  rootKind: CanonicalShadowRootKind
  rootToken: CanonicalRootToken
  entries: CanonicalShadowCopyEntry[]
  totalBytes: number
  missingSourceBytesCount: number

  constructor(
    rootKind: CanonicalShadowRootKind,
    rootToken: CanonicalRootToken,
    entries: CanonicalShadowCopyEntry[] = [],
    missingSourceBytesCount: number = 0
  ) {
    this.rootKind = rootKind
    this.rootToken = rootToken
    this.entries = [...entries].sort((a, b) => a.id.localeCompare(b.id))
    this.totalBytes = entries.reduce((sum, e) => sum + e.byteSize, 0)
    this.missingSourceBytesCount = Math.max(0, missingSourceBytesCount)
  }
}

// =============================================================================
// SECTION 4: Shadow File Execution Report
// =============================================================================

export class CanonicalShadowFileExecutionReport {
  rootKind: CanonicalShadowRootKind
  entryCount: number
  bytesCopied: number
  wroteToShadowRoot: boolean
  wroteProductionRoot: boolean
  physicalDeletePerformed: boolean
  rollbackAvailable: boolean
  missingSourceBytesCount: number
  rejectedReason: string | null

  constructor(
    rootKind: CanonicalShadowRootKind,
    manifest: CanonicalShadowCopyManifest | null = null,
    wroteToShadowRoot: boolean = false,
    wroteProductionRoot: boolean = false,
    physicalDeletePerformed: boolean = false,
    rollbackAvailable: boolean = false,
    missingSourceBytesCount: number = 0,
    rejectedReason: string | null = null
  ) {
    this.rootKind = rootKind
    this.entryCount = manifest?.entries.length ?? 0
    this.bytesCopied = manifest?.totalBytes ?? 0
    this.wroteToShadowRoot = wroteToShadowRoot
    this.wroteProductionRoot = wroteProductionRoot
    this.physicalDeletePerformed = physicalDeletePerformed
    this.rollbackAvailable = rollbackAvailable
    this.missingSourceBytesCount = Math.max(0, missingSourceBytesCount + (manifest?.missingSourceBytesCount ?? 0))
    this.rejectedReason = CanonicalShadowMigrationRedaction.safeText(rejectedReason)
  }
}

// =============================================================================
// SECTION 5: CanonicalShadowFileStore
// =============================================================================

export class CanonicalShadowFileStore {
  readonly rootToken: CanonicalRootToken
  readonly rootKind: CanonicalShadowRootKind
  readonly policy: CanonicalShadowCopyPolicy

  private store: InMemoryCanonicalFileStore
  private entries: CanonicalShadowCopyEntry[] = []
  private missingSourceBytesCount: number = 0

  constructor(
    rootToken: CanonicalRootToken = new CanonicalRootToken('canonical-shadow-root'),
    rootKind: CanonicalShadowRootKind = CanonicalShadowRootKind.TEMPORARY,
    policy: CanonicalShadowCopyPolicy = new CanonicalShadowCopyPolicy()
  ) {
    this.rootToken = rootToken
    this.rootKind = rootKind
    this.policy = policy
    this.store = new InMemoryCanonicalFileStore({ [rootToken.rawValue]: 'canonical-shadow-root' } as Record<string,
      string>)
  }

  async resolve(reference: CanonicalFileReference): Promise<{ path: string }> {
    return this.store.resolve(reference) as { path: string }
  }

  async read(request: CanonicalFileReadRequest): Promise<CanonicalFileReadResult> {
    return this.store.read(request)
  }

  async write(intent: CanonicalFileWriteIntent): Promise<CanonicalFileWriteResult> {
    this.validate(intent)
    const result = await this.store.write(intent)
    this.entries.push(
      new CanonicalShadowCopyEntry(
        intent.reference, intent.purpose, result.byteSize,
        result.contentHash, true, result.disposition
      )
    )
    return result
  }

  async markTombstone(reference: CanonicalFileReference, reason: string | null): Promise<CanonicalFileWriteResult> {
    if (!this.policy.allowTombstoneMarkers) {
      throw new Error('shadowTombstoneMarkerDisabled')
    }
    const result = await this.store.markTombstone(reference, reason)
    this.entries.push(
      new CanonicalShadowCopyEntry(
        reference, CanonicalFilePurpose.TOMBSTONE_MARKER, result.byteSize,
        result.contentHash, true, 'noPhysicalDelete'
      )
    )
    return result
  }

  async contains(reference: CanonicalFileReference): Promise<boolean> {
    return this.store.contains(reference)
  }

  report(): CanonicalShadowFileExecutionReport {
    return new CanonicalShadowFileExecutionReport(
      this.rootKind,
      new CanonicalShadowCopyManifest(
        this.rootKind, this.rootToken, this.entries, this.missingSourceBytesCount
      ),
      this.entries.length > 0, false, false, this.entries.length > 0, this.missingSourceBytesCount
    )
  }

  recordMissingSourceBytes(): void {
    this.missingSourceBytesCount += 1
  }

  private validate(intent: CanonicalFileWriteIntent): void {
    if (intent.reference.rootToken.rawValue !== this.rootToken.rawValue) {
      throw new Error('rootNotBound:' + intent.reference.rootToken.rawValue)
    }
    if (this.policy.maxBytes > 0 && intent.bytes.length > this.policy.maxBytes) {
      throw new Error('preWriteSizeMismatch')
    }
    switch (intent.purpose) {
      case CanonicalFilePurpose.METADATA_BLOB:
        if (!this.policy.allowMetadataBytes) {
          throw new Error('shadowMetadataBytesDisabled')
        }
        break
      case CanonicalFilePurpose.ARTIFACT_BYTES:
      case CanonicalFilePurpose.GENERATED_ARTIFACT:
        if (!this.policy.allowArtifactBytes) {
          throw new Error('shadowArtifactBytesDisabled')
        }
        break
      case CanonicalFilePurpose.TOMBSTONE_MARKER:
        if (!this.policy.allowTombstoneMarkers) {
          throw new Error('shadowTombstoneMarkerDisabled')
        }
        break
    }
  }
}

// =============================================================================
// SECTION 6: Shadow Route Classification & Policy
// =============================================================================

export enum CanonicalShadowRouteClassification {
  READ_ONLY = 'readOnly',
  MUTATING = 'mutating',
  UNKNOWN = 'unknown'
}

// Forward: CanonicalTransportRoute
export enum CanonicalTransportRoute {
  MANIFEST_EXCHANGE = 'manifestExchange',
  FILE_READ = 'fileRead',
  APPLY_PLAN = 'applyPlan',
  APPLY_METADATA = 'applyMetadata',
  UPLOAD_START = 'uploadStart',
  UPLOAD_STATUS = 'uploadStatus',
  UPLOAD_CHUNK = 'uploadChunk',
  UPLOAD_FINALIZE = 'uploadFinalize'
}

export class CanonicalShadowRoutePolicy {
  artifactRequestMaxBytes: number

  constructor(artifactRequestMaxBytes: number = 256 * 1024) {
    this.artifactRequestMaxBytes = Math.max(0, artifactRequestMaxBytes)
  }

  classificationFor(route: CanonicalTransportRoute, bodyByteCount: number = 0): CanonicalShadowRouteClassification {
    switch (route) {
      case CanonicalTransportRoute.MANIFEST_EXCHANGE:
        return CanonicalShadowRouteClassification.READ_ONLY
      case CanonicalTransportRoute.FILE_READ:
        return bodyByteCount <= this.artifactRequestMaxBytes
          ? CanonicalShadowRouteClassification.READ_ONLY
          : CanonicalShadowRouteClassification.MUTATING
      case CanonicalTransportRoute.APPLY_PLAN:
      case CanonicalTransportRoute.APPLY_METADATA:
      case CanonicalTransportRoute.UPLOAD_START:
      case CanonicalTransportRoute.UPLOAD_STATUS:
      case CanonicalTransportRoute.UPLOAD_CHUNK:
      case CanonicalTransportRoute.UPLOAD_FINALIZE:
        return CanonicalShadowRouteClassification.MUTATING
    }
  }

  probeKindFor(route: CanonicalTransportRoute): CanonicalShadowNetworkProbeKind {
    switch (route) {
      case CanonicalTransportRoute.MANIFEST_EXCHANGE:
        return CanonicalShadowNetworkProbeKind.SYNC_INVENTORY_READ_ONLY
      case CanonicalTransportRoute.FILE_READ:
        return CanonicalShadowNetworkProbeKind.ARTIFACT_REQUEST_READ_ONLY
      case CanonicalTransportRoute.APPLY_PLAN:
      case CanonicalTransportRoute.APPLY_METADATA:
        return CanonicalShadowNetworkProbeKind.APPLY_MANIFEST
      case CanonicalTransportRoute.UPLOAD_START:
        return CanonicalShadowNetworkProbeKind.UPLOAD_SESSION_START
      case CanonicalTransportRoute.UPLOAD_STATUS:
        return CanonicalShadowNetworkProbeKind.UPLOAD_SESSION_START
      case CanonicalTransportRoute.UPLOAD_CHUNK:
        return CanonicalShadowNetworkProbeKind.UPLOAD_SESSION_CHUNK
      case CanonicalTransportRoute.UPLOAD_FINALIZE:
        return CanonicalShadowNetworkProbeKind.UPLOAD_SESSION_FINALIZE
    }
  }
}

// =============================================================================
// SECTION 7: Shadow Network Probes (Forward Types)
// =============================================================================

export enum CanonicalShadowNetworkProbeKind {
  SYNC_INVENTORY_READ_ONLY = 'syncInventoryReadOnly',
  ARTIFACT_REQUEST_READ_ONLY = 'artifactRequestReadOnly',
  APPLY_MANIFEST = 'applyManifest',
  UPLOAD_SESSION_START = 'uploadSessionStart',
  UPLOAD_SESSION_CHUNK = 'uploadSessionChunk',
  UPLOAD_SESSION_FINALIZE = 'uploadSessionFinalize'
}

export class CanonicalShadowNetworkProbeRequest {
  kind: CanonicalShadowNetworkProbeKind
  routePath: string | null
  bodyByteCount: number
  artifactByteLimit: number

  constructor(
    kind: CanonicalShadowNetworkProbeKind,
    routePath: string | null = null,
    bodyByteCount: number = 0,
    artifactByteLimit: number = 256 * 1024
  ) {
    this.kind = kind
    this.routePath = routePath
    this.bodyByteCount = bodyByteCount
    this.artifactByteLimit = artifactByteLimit
  }
}

export class CanonicalShadowNetworkProbeDecision {
  accepted: boolean
  reason: string

  constructor(accepted: boolean, reason: string) {
    this.accepted = accepted
    this.reason = reason
  }
}

export class CanonicalShadowNetworkProbePolicy {
  decisionFor(request: CanonicalShadowNetworkProbeRequest): CanonicalShadowNetworkProbeDecision {
    if (request.kind === CanonicalShadowNetworkProbeKind.SYNC_INVENTORY_READ_ONLY ||
      request.kind === CanonicalShadowNetworkProbeKind.ARTIFACT_REQUEST_READ_ONLY) {
      return new CanonicalShadowNetworkProbeDecision(true, 'readOnlyEnvelopeOnly')
    }
    return new CanonicalShadowNetworkProbeDecision(false, 'mutatingRouteBlocked')
  }
}

// =============================================================================
// SECTION 8: Shadow Transport Envelope Report
// =============================================================================

export class CanonicalShadowTransportEnvelopeReport {
  route: CanonicalTransportRoute
  routePath: string
  classification: CanonicalShadowRouteClassification
  bodyHashPrefix: string | null
  timestampPresent: boolean
  noncePresent: boolean
  signatureProjectionPresent: boolean
  wouldSendNetwork: boolean
  sentNetwork: boolean
  reason: string

  constructor(
    route: CanonicalTransportRoute,
    routePath: string,
    classification: CanonicalShadowRouteClassification,
    bodyHashPrefix: string | null,
    timestampPresent: boolean,
    noncePresent: boolean,
    signatureProjectionPresent: boolean,
    wouldSendNetwork: boolean,
    sentNetwork: boolean,
    reason: string
  ) {
    this.route = route
    this.routePath = CanonicalShadowMigrationRedaction.safeText(routePath) ?? route
    this.classification = classification
    this.bodyHashPrefix = bodyHashPrefix
    this.timestampPresent = timestampPresent
    this.noncePresent = noncePresent
    this.signatureProjectionPresent = signatureProjectionPresent
    this.wouldSendNetwork = wouldSendNetwork
    this.sentNetwork = sentNetwork
    this.reason = CanonicalShadowMigrationRedaction.safeText(reason) ?? 'shadowTransportProjection'
  }
}

export class CanonicalShadowTransportProbeResult {
  policyDecision: CanonicalShadowNetworkProbeDecision
  envelopeReport: CanonicalShadowTransportEnvelopeReport
  accepted: boolean
  sentNetwork: boolean
  failureReason: string | null

  constructor(
    policyDecision: CanonicalShadowNetworkProbeDecision,
    envelopeReport: CanonicalShadowTransportEnvelopeReport,
    accepted: boolean,
    sentNetwork: boolean,
    failureReason: string | null = null
  ) {
    this.policyDecision = policyDecision
    this.envelopeReport = envelopeReport
    this.accepted = accepted
    this.sentNetwork = sentNetwork
    this.failureReason = CanonicalShadowMigrationRedaction.safeText(failureReason)
  }
}

// =============================================================================
// SECTION 9: Shadow Upload Session
// =============================================================================

export class CanonicalUploadSessionID {
  rawValue: string

  constructor(rawValue: string) {
    this.rawValue = rawValue
  }
}

export enum CanonicalUploadSessionPhase {
  PLANNED = 'planned',
  STARTED = 'started',
  IN_PROGRESS = 'inProgress',
  FINALIZING = 'finalizing',
  COMPLETED = 'completed',
  CONFLICT = 'conflict',
  FAILED = 'failed'
}

export class CanonicalShadowUploadSession {
  sessionID: CanonicalUploadSessionID | null
  confirmedBytes: number
  phase: CanonicalUploadSessionPhase

  constructor(
    sessionID: CanonicalUploadSessionID | null = null,
    confirmedBytes: number = 0,
    phase: CanonicalUploadSessionPhase = CanonicalUploadSessionPhase.PLANNED
  ) {
    this.sessionID = sessionID
    this.confirmedBytes = confirmedBytes
    this.phase = phase
  }
}

// =============================================================================
// SECTION 10: Shadow Upload Receiver
// =============================================================================

export class CanonicalShadowUploadReceiver {
  readonly store: InMemoryCanonicalFileStore
  readonly rootToken: CanonicalRootToken

  constructor(rootToken: CanonicalRootToken = new CanonicalRootToken('canonical-shadow-upload-root')) {
    this.rootToken = rootToken
    this.store = new InMemoryCanonicalFileStore({ [rootToken.rawValue]: 'canonical-shadow-upload-root' } as Record<string,
      string>)
  }

  async seed(reference: CanonicalFileReference, bytes: Uint8Array): Promise<void> {
    const hash = InMemoryCanonicalFileStore.hash(bytes, 'sha256')
    await this.store.write(
      new CanonicalFileWriteIntent(
        reference, bytes, CanonicalFilePurpose.ARTIFACT_BYTES,
        hash, bytes.length, 'replace'
      )
    )
  }

  async read(reference: CanonicalFileReference): Promise<CanonicalFileReadResult> {
    return this.store.read(new CanonicalFileReadRequest(reference))
  }
}

// =============================================================================
// SECTION 11: Shadow Upload Divergence
// =============================================================================

export enum CanonicalShadowUploadDivergence {
  NONE = 'none',
  SAME_HASH_NO_OP = 'sameHashNoOp',
  DIFFERENT_HASH_CONFLICT = 'differentHashConflict',
  FINALIZE_HASH_MISMATCH = 'finalizeHashMismatch',
  INTERRUPTED_AND_RESUMED = 'interruptedAndResumed',
  UNEXPECTED_FAILURE = 'unexpectedFailure'
}

// =============================================================================
// SECTION 12: Shadow Upload Rehearsal Input / Result
// =============================================================================

export class CanonicalShadowUploadRehearsalInput {
  objectID: string
  targetReference: CanonicalFileReference
  bytes: Uint8Array
  chunkSize: number
  declaredTotalHash: CanonicalHash | null
  existingReceiverBytes: Uint8Array | null
  simulateInterruptionAfterFirstChunk: boolean

  constructor(
    objectID: string,
    targetReference: CanonicalFileReference,
    bytes: Uint8Array,
    chunkSize: number = 4 * 1024 * 1024,
    declaredTotalHash: CanonicalHash | null = null,
    existingReceiverBytes: Uint8Array | null = null,
    simulateInterruptionAfterFirstChunk: boolean = false
  ) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.targetReference = targetReference
    this.bytes = bytes
    this.chunkSize = Math.max(1, chunkSize)
    this.declaredTotalHash = declaredTotalHash
    this.existingReceiverBytes = existingReceiverBytes
    this.simulateInterruptionAfterFirstChunk = simulateInterruptionAfterFirstChunk
  }
}

export class CanonicalShadowUploadResult {
  session: CanonicalShadowUploadSession | null
  calledProductionUploadCoordinator: boolean
  calledRecordingUploadClient: boolean
  calledSecureMacUploadClient: boolean
  wroteProductionInbox: boolean
  wroteReceiveJSON: boolean
  wroteShadowReceiver: boolean
  completed: boolean
  confirmedBytes: number
  phase: CanonicalUploadSessionPhase
  divergence: CanonicalShadowUploadDivergence
  failureReason: string | null

  constructor(
    session: CanonicalShadowUploadSession | null = null,
    calledProductionUploadCoordinator: boolean = false,
    calledRecordingUploadClient: boolean = false,
    calledSecureMacUploadClient: boolean = false,
    wroteProductionInbox: boolean = false,
    wroteReceiveJSON: boolean = false,
    wroteShadowReceiver: boolean = false,
    completed: boolean = false,
    confirmedBytes: number = 0,
    phase: CanonicalUploadSessionPhase = CanonicalUploadSessionPhase.PLANNED,
    divergence: CanonicalShadowUploadDivergence = CanonicalShadowUploadDivergence.NONE,
    failureReason: string | null = null
  ) {
    this.session = session
    this.calledProductionUploadCoordinator = calledProductionUploadCoordinator
    this.calledRecordingUploadClient = calledRecordingUploadClient
    this.calledSecureMacUploadClient = calledSecureMacUploadClient
    this.wroteProductionInbox = wroteProductionInbox
    this.wroteReceiveJSON = wroteReceiveJSON
    this.wroteShadowReceiver = wroteShadowReceiver
    this.completed = completed
    this.confirmedBytes = confirmedBytes
    this.phase = phase
    this.divergence = divergence
    this.failureReason = failureReason
  }
}

// =============================================================================
// SECTION 13: Shadow Upload Rehearsal
// =============================================================================

// Forward: simple upload runtime types for shadow rehearsal
export class CanonicalUploadStartRequest {
  objectID: string
  targetReference: CanonicalFileReference
  totalBytes: number
  totalHash: CanonicalHash
  chunkSize: number

  constructor(
    objectID: string, targetReference: CanonicalFileReference,
    totalBytes: number, totalHash: CanonicalHash, chunkSize: number
  ) {
    this.objectID = objectID
    this.targetReference = targetReference
    this.totalBytes = totalBytes
    this.totalHash = totalHash
    this.chunkSize = chunkSize
  }
}

export class CanonicalUploadChunk {
  objectID: string
  sessionID: CanonicalUploadSessionID
  offset: number
  bytes: Uint8Array
  chunkHash: CanonicalHash
  totalHash: CanonicalHash
  idempotencyKey: string

  constructor(
    objectID: string, sessionID: CanonicalUploadSessionID,
    offset: number, bytes: Uint8Array, chunkHash: CanonicalHash,
    totalHash: CanonicalHash, idempotencyKey: string
  ) {
    this.objectID = objectID
    this.sessionID = sessionID
    this.offset = offset
    this.bytes = bytes
    this.chunkHash = chunkHash
    this.totalHash = totalHash
    this.idempotencyKey = idempotencyKey
  }
}

export class CanonicalUploadStatusRequest {
  objectID: string
  sessionID: CanonicalUploadSessionID
  totalHash: CanonicalHash

  constructor(objectID: string, sessionID: CanonicalUploadSessionID, totalHash: CanonicalHash) {
    this.objectID = objectID
    this.sessionID = sessionID
    this.totalHash = totalHash
  }
}

export class CanonicalUploadFinalizeRequest {
  objectID: string
  sessionID: CanonicalUploadSessionID
  totalBytes: number
  totalHash: CanonicalHash

  constructor(
    objectID: string, sessionID: CanonicalUploadSessionID,
    totalBytes: number, totalHash: CanonicalHash
  ) {
    this.objectID = objectID
    this.sessionID = sessionID
    this.totalBytes = totalBytes
    this.totalHash = totalHash
  }
}

export class CanonicalUploadSessionStatus {
  sessionID: CanonicalUploadSessionID | null
  confirmedBytes: number
  phase: CanonicalUploadSessionPhase
  completed: boolean
  fileSize: number | null
  checksum: CanonicalHash | null
  retry: object | null

  constructor(
    sessionID: CanonicalUploadSessionID | null, confirmedBytes: number,
    phase: CanonicalUploadSessionPhase, completed: boolean,
    fileSize: number | null = null, checksum: CanonicalHash | null = null,
    retry: object | null = null
  ) {
    this.sessionID = sessionID
    this.confirmedBytes = confirmedBytes
    this.phase = phase
    this.completed = completed
    this.fileSize = fileSize
    this.checksum = checksum
    this.retry = retry
  }
}

export enum CanonicalUploadRuntimeErrorType {
  INVALID_SESSION = 'invalidSession',
  FINAL_HASH_MISMATCH = 'finalHashMismatch',
  TARGET_CONFLICT = 'targetConflict',
  INVALID_REQUEST = 'invalidRequest'
}

class CanonicalResumableUploadRuntime {
  private fileStore: InMemoryCanonicalFileStore
  private sessions: Map<string, CanonicalUploadSessionStatus> = new Map()
  private bytesStored: Map<string, Uint8Array> = new Map()

  constructor(fileStore: InMemoryCanonicalFileStore) {
    this.fileStore = fileStore
  }

  async start(request: CanonicalUploadStartRequest, now: Date): Promise<CanonicalUploadSessionStatus> {
    const key = request.targetReference.rootToken.rawValue + '/' + request.targetReference.logicalPathToken
    const existing = this.bytesStored.get(key)
    if (existing && existing.length === request.totalBytes) {
      const existingHash = InMemoryCanonicalFileStore.hash(existing, 'sha256')
      if (existingHash && existingHash.equals(request.totalHash)) {
        const sessionID = new CanonicalUploadSessionID('shadow-session-' + request.objectID)
        return new CanonicalUploadSessionStatus(
          sessionID, request.totalBytes, CanonicalUploadSessionPhase.COMPLETED, true,
          request.totalBytes, request.totalHash
        )
      }
    }
    const sessionID = new CanonicalUploadSessionID('shadow-session-' + request.objectID)
    const status = new CanonicalUploadSessionStatus(
      sessionID, 0, CanonicalUploadSessionPhase.STARTED, false, request.totalBytes, request.totalHash
    )
    this.sessions.set(sessionID.rawValue, status)
    return status
  }

  async append(chunk: CanonicalUploadChunk, now: Date): Promise<CanonicalUploadSessionStatus> {
    const status = this.sessions.get(chunk.sessionID.rawValue)
    if (!status) {
      throw new Error(CanonicalUploadRuntimeErrorType.INVALID_SESSION)
    }
    return new CanonicalUploadSessionStatus(
      chunk.sessionID, status.confirmedBytes + chunk.bytes.length,
      CanonicalUploadSessionPhase.IN_PROGRESS, false, status.fileSize, status.checksum
    )
  }

  async status(request: CanonicalUploadStatusRequest, now: Date): Promise<CanonicalUploadSessionStatus> {
    const status = this.sessions.get(request.sessionID.rawValue)
    if (!status) {
      throw new Error(CanonicalUploadRuntimeErrorType.INVALID_SESSION)
    }
    return status
  }

  async finalize(request: CanonicalUploadFinalizeRequest, now: Date): Promise<CanonicalUploadSessionStatus> {
    const status = this.sessions.get(request.sessionID.rawValue)
    if (!status) {
      throw new Error(CanonicalUploadRuntimeErrorType.INVALID_SESSION)
    }
    if (status.checksum && !status.checksum.equals(request.totalHash)) {
      throw new Error(CanonicalUploadRuntimeErrorType.FINAL_HASH_MISMATCH)
    }
    return new CanonicalUploadSessionStatus(
      request.sessionID, request.totalBytes, CanonicalUploadSessionPhase.COMPLETED, true,
      request.totalBytes, request.totalHash
    )
  }
}

export class CanonicalShadowUploadRehearsal {
  async run(
    input: CanonicalShadowUploadRehearsalInput,
    receiver: CanonicalShadowUploadReceiver = new CanonicalShadowUploadReceiver(),
    now: Date = new Date()
  ): Promise<CanonicalShadowUploadResult> {
    try {
      if (input.existingReceiverBytes) {
        await receiver.seed(input.targetReference, input.existingReceiverBytes)
      }
      const runtime = new CanonicalResumableUploadRuntime(receiver.store)
      const actualHash = InMemoryCanonicalFileStore.hash(input.bytes, 'sha256') ??
        CanonicalHash.sha256OfString('')
      const totalHash = input.declaredTotalHash ?? actualHash
      const start = await runtime.start(
        new CanonicalUploadStartRequest(
          input.objectID, input.targetReference,
          input.bytes.length, totalHash, input.chunkSize
        ), now
      )
      if (start.completed) {
        return this.buildResult(start, false, CanonicalShadowUploadDivergence.SAME_HASH_NO_OP)
      }
      const sessionID = start.sessionID!
      let offset = 0
      let interrupted = false
      while (offset < input.bytes.length) {
        const upper = Math.min(offset + input.chunkSize, input.bytes.length)
        const chunkBytes = input.bytes.slice(offset, upper)
        const chunkHash = InMemoryCanonicalFileStore.hash(chunkBytes, 'sha256') ??
          CanonicalHash.sha256OfString('')
        await runtime.append(
          new CanonicalUploadChunk(
            input.objectID, sessionID, offset, chunkBytes,
            chunkHash, totalHash, 'shadow-' + offset
          ), now
        )
        offset = upper
        if (input.simulateInterruptionAfterFirstChunk && !interrupted) {
          interrupted = true
          await runtime.status(
            new CanonicalUploadStatusRequest(input.objectID, sessionID, totalHash), now
          )
        }
      }
      const finalized = await runtime.finalize(
        new CanonicalUploadFinalizeRequest(
          input.objectID, sessionID, input.bytes.length, totalHash
        ), now
      )
      return this.buildResult(finalized, true,
        interrupted ? CanonicalShadowUploadDivergence.INTERRUPTED_AND_RESUMED :
          CanonicalShadowUploadDivergence.NONE)
    } catch (error) {
      const errStr = String(error)
      if (errStr.includes(CanonicalUploadRuntimeErrorType.FINAL_HASH_MISMATCH)) {
        return this.failedResult(CanonicalUploadSessionPhase.CONFLICT,
          CanonicalShadowUploadDivergence.FINALIZE_HASH_MISMATCH, 'finalHashMismatch')
      }
      if (errStr.includes(CanonicalUploadRuntimeErrorType.TARGET_CONFLICT)) {
        return this.failedResult(CanonicalUploadSessionPhase.CONFLICT,
          CanonicalShadowUploadDivergence.DIFFERENT_HASH_CONFLICT, 'targetConflict')
      }
      return this.failedResult(CanonicalUploadSessionPhase.FAILED,
        CanonicalShadowUploadDivergence.UNEXPECTED_FAILURE, errStr)
    }
  }

  private buildResult(
    status: CanonicalUploadSessionStatus, wroteShadowReceiver: boolean,
    divergence: CanonicalShadowUploadDivergence
  ): CanonicalShadowUploadResult {
    return new CanonicalShadowUploadResult(
      new CanonicalShadowUploadSession(status.sessionID, status.confirmedBytes, status.phase),
      false, false, false, false, false, wroteShadowReceiver,
      status.completed, status.confirmedBytes, status.phase, divergence, null
    )
  }

  private failedResult(
    phase: CanonicalUploadSessionPhase, divergence: CanonicalShadowUploadDivergence, reason: string
  ): CanonicalShadowUploadResult {
    return new CanonicalShadowUploadResult(
      null, false, false, false, false, false, false, false, 0, phase, divergence,
      CanonicalShadowMigrationRedaction.safeText(reason)
    )
  }
}

// =============================================================================
// SECTION 14: Shadow Apply Store
// =============================================================================

export class CanonicalShadowApplyStore {
  readonly localFileStore: InMemoryCanonicalFileStore
  readonly peerFileStore: InMemoryCanonicalFileStore
  readonly localMetadataRoot: CanonicalRootToken
  readonly peerMetadataRoot: CanonicalRootToken
  readonly localGeneratedRoot: CanonicalRootToken
  readonly peerGeneratedRoot: CanonicalRootToken

  constructor(
    localMetadataRoot: CanonicalRootToken = new CanonicalRootToken('shadow-local-metadata'),
    peerMetadataRoot: CanonicalRootToken = new CanonicalRootToken('shadow-peer-metadata'),
    localGeneratedRoot: CanonicalRootToken = new CanonicalRootToken('shadow-local-generated'),
    peerGeneratedRoot: CanonicalRootToken = new CanonicalRootToken('shadow-peer-generated')
  ) {
    this.localMetadataRoot = localMetadataRoot
    this.peerMetadataRoot = peerMetadataRoot
    this.localGeneratedRoot = localGeneratedRoot
    this.peerGeneratedRoot = peerGeneratedRoot
    this.localFileStore = new InMemoryCanonicalFileStore({
      [localMetadataRoot.rawValue]: 'shadow/local/metadata',
      [localGeneratedRoot.rawValue]: 'shadow/local/generated'
    } as Record<string, string>)
    this.peerFileStore = new InMemoryCanonicalFileStore({
      [peerMetadataRoot.rawValue]: 'shadow/peer/metadata',
      [peerGeneratedRoot.rawValue]: 'shadow/peer/generated'
    } as Record<string, string>)
  }

  makeContext(localManifest: CanonicalManifest, peerManifest: CanonicalManifest): CanonicalApplyRuntimeContext {
    return new CanonicalApplyRuntimeContext(
      localManifest, peerManifest,
      this.localFileStore as unknown as CanonicalFileStorePort,
      this.peerFileStore as unknown as CanonicalFileStorePort,
      this.localMetadataRoot, this.peerMetadataRoot,
      this.localGeneratedRoot, this.peerGeneratedRoot
    )
  }
}

// =============================================================================
// SECTION 15: Shadow Apply Divergence & Result
// =============================================================================

export enum CanonicalShadowApplyDivergence {
  NONE = 'none',
  FAILED_ACTION = 'failedAction',
  ROLLBACK_FAILED = 'rollbackFailed',
  CONFLICT_RECORDED = 'conflictRecorded'
}

export class CanonicalRollbackResult {
  planID: string
  succeeded: boolean
  completedActionIDs: string[]

  constructor(planID: string, succeeded: boolean, completedActionIDs: string[]) {
    this.planID = planID
    this.succeeded = succeeded
    this.completedActionIDs = completedActionIDs
  }
}

export class CanonicalShadowApplyResult {
  executionReport: CanonicalApplyExecutionReport
  calledApplySyncManifest: boolean
  calledArtifactApply: boolean
  wroteProductionStore: boolean
  wroteShadowStore: boolean
  tombstonePhysicalDelete: boolean
  postconditionVerified: boolean
  rollbackResult: CanonicalRollbackResult | null
  divergence: CanonicalShadowApplyDivergence

  constructor(
    executionReport: CanonicalApplyExecutionReport,
    calledApplySyncManifest: boolean,
    calledArtifactApply: boolean,
    wroteProductionStore: boolean,
    wroteShadowStore: boolean,
    tombstonePhysicalDelete: boolean,
    postconditionVerified: boolean,
    rollbackResult: CanonicalRollbackResult | null,
    divergence: CanonicalShadowApplyDivergence
  ) {
    this.executionReport = executionReport
    this.calledApplySyncManifest = calledApplySyncManifest
    this.calledArtifactApply = calledArtifactApply
    this.wroteProductionStore = wroteProductionStore
    this.wroteShadowStore = wroteShadowStore
    this.tombstonePhysicalDelete = tombstonePhysicalDelete
    this.postconditionVerified = postconditionVerified
    this.rollbackResult = rollbackResult
    this.divergence = divergence
  }
}

// =============================================================================
// SECTION 16: Shadow Apply Rehearsal
// =============================================================================

export class CanonicalShadowApplyRehearsal {
  async run(
    applyPlan: CanonicalApplyPlan,
    libraryPlan: { conflicts: object[]; applyActions: CanonicalApplyAction[] } | null,
    localManifest: CanonicalManifest,
    peerManifest: CanonicalManifest,
    store: CanonicalShadowApplyStore = new CanonicalShadowApplyStore()
  ): Promise<CanonicalShadowApplyResult> {
    const executor = new CanonicalApplyExecutor()
    const report = await executor.execute(
      applyPlan, libraryPlan ?? null,
      store.makeContext(localManifest, peerManifest)
    )
    const rollback = new CanonicalRollbackResult(
      'shadow-apply-rollback', true, report.records.map(r => r.actionID)
    )
    let divergence: CanonicalShadowApplyDivergence
    if (report.failedCount > 0) {
      divergence = CanonicalShadowApplyDivergence.FAILED_ACTION
    } else if (report.conflictReport.unresolvedCount > 0) {
      divergence = CanonicalShadowApplyDivergence.CONFLICT_RECORDED
    } else {
      divergence = CanonicalShadowApplyDivergence.NONE
    }
    return new CanonicalShadowApplyResult(
      report, false, false, false, report.records.length > 0,
      false, report.failedCount === 0, rollback, divergence
    )
  }
}

// =============================================================================
// SECTION 17: Execution Shadow Events
// =============================================================================

export enum CanonicalExecutionShadowEventKind {
  CANONICAL_EXECUTION_SHADOW_STARTED = 'canonicalExecutionShadowStarted',
  CANONICAL_EXECUTION_SHADOW_COMPLETED = 'canonicalExecutionShadowCompleted',
  CANONICAL_EXECUTION_SHADOW_BLOCKED = 'canonicalExecutionShadowBlocked',
  CANONICAL_EXECUTION_SHADOW_FILE_WRITE_SUPPRESSED = 'canonicalExecutionShadowFileWriteSuppressed',
  CANONICAL_EXECUTION_SHADOW_FILE_WRITE_TO_SHADOW_ROOT = 'canonicalExecutionShadowFileWriteToShadowRoot',
  CANONICAL_EXECUTION_SHADOW_TRANSPORT_PROBE_SUPPRESSED = 'canonicalExecutionShadowTransportProbeSuppressed',
  CANONICAL_EXECUTION_SHADOW_TRANSPORT_PROBE_COMPLETED = 'canonicalExecutionShadowTransportProbeCompleted',
  CANONICAL_EXECUTION_SHADOW_UPLOAD_REHEARSED = 'canonicalExecutionShadowUploadRehearsed',
  CANONICAL_EXECUTION_SHADOW_APPLY_REHEARSED = 'canonicalExecutionShadowApplyRehearsed',
  CANONICAL_EXECUTION_SHADOW_ROLLBACK_REHEARSED = 'canonicalExecutionShadowRollbackRehearsed',
  CANONICAL_EXECUTION_SHADOW_DIVERGENCE_DETECTED = 'canonicalExecutionShadowDivergenceDetected',
  CANONICAL_EXECUTION_SHADOW_EQUIVALENT = 'canonicalExecutionShadowEquivalent',
  CANONICAL_EXECUTION_SHADOW_PRODUCTION_EXECUTE_BLOCKED = 'canonicalExecutionShadowProductionExecuteBlocked',
  CANONICAL_REAL_DATA_SHADOW_COPY_STARTED = 'canonicalRealDataShadowCopyStarted',
  CANONICAL_REAL_DATA_SHADOW_COPY_COMPLETED = 'canonicalRealDataShadowCopyCompleted',
  CANONICAL_REAL_DATA_SHADOW_COPY_FAILED = 'canonicalRealDataShadowCopyFailed',
  CANONICAL_REAL_DATA_SHADOW_COPY_VERIFIED = 'canonicalRealDataShadowCopyVerified',
  CANONICAL_REAL_DATA_SHADOW_COPY_CLEANUP_STARTED = 'canonicalRealDataShadowCopyCleanupStarted',
  CANONICAL_REAL_DATA_SHADOW_COPY_CLEANUP_COMPLETED = 'canonicalRealDataShadowCopyCleanupCompleted',
  CANONICAL_REAL_DATA_SHADOW_COPY_CLEANUP_FAILED = 'canonicalRealDataShadowCopyCleanupFailed',
  CANONICAL_REAL_DATA_SHADOW_COPY_RETAINED_FOR_DIAGNOSTICS = 'canonicalRealDataShadowCopyRetainedForDiagnostics',
  CANONICAL_REAL_DATA_SHADOW_COPY_UNAVAILABLE = 'canonicalRealDataShadowCopyUnavailable',
  CANONICAL_READ_ONLY_TRANSPORT_PROBE_STARTED = 'canonicalReadOnlyTransportProbeStarted',
  CANONICAL_READ_ONLY_TRANSPORT_PROBE_COMPLETED = 'canonicalReadOnlyTransportProbeCompleted',
  CANONICAL_READ_ONLY_TRANSPORT_PROBE_BLOCKED = 'canonicalReadOnlyTransportProbeBlocked',
  CANONICAL_READ_ONLY_TRANSPORT_PROBE_SUPPRESSED = 'canonicalReadOnlyTransportProbeSuppressed',
  CANONICAL_READ_ONLY_TRANSPORT_PROBE_ROUTE_REJECTED = 'canonicalReadOnlyTransportProbeRouteRejected',
  CANONICAL_READ_ONLY_TRANSPORT_PROBE_AUTH_BOUNDARY_PRESERVED = 'canonicalReadOnlyTransportProbeAuthBoundaryPreserved'
}

// Forward shadow migration types
export enum CanonicalShadowMigrationTrigger {
  PERIODIC = 'periodic',
  MANUAL = 'manual',
  DIAGNOSTIC = 'diagnostic'
}

export enum CanonicalProductionExecutionDomainRole {
  LOCAL = 'local',
  PEER = 'peer'
}

export enum CanonicalShadowMigrationMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  DRY_RUN_COMPARE = 'dryRunCompare',
  EXECUTION_SHADOW_DRY_RUN = 'executionShadowDryRun',
  EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE = 'executionShadowWithShadowFileStore',
  EXECUTION_SHADOW_WITH_READ_ONLY_TRANSPORT_PROBE = 'executionShadowWithReadOnlyTransportProbe',
  SHADOW_READ_ONLY = 'shadowReadOnly',
  SHADOW_READ_ONLY_WITH_NETWORK_PROBE = 'shadowReadOnlyWithNetworkProbe',
  BLOCKED_PRODUCTION_EXECUTE = 'blockedProductionExecute',
  BLOCKED_EXECUTION_SHADOW_WRITE = 'blockedExecutionShadowWrite',
  BLOCKED_EXECUTION_SHADOW_UPLOAD = 'blockedExecutionShadowUpload',
  BLOCKED_EXECUTION_SHADOW_APPLY = 'blockedExecutionShadowApply'
}

export enum CanonicalProductionDomain {
  FILE_RUNTIME = 'fileRuntime',
  TRANSPORT_RUNTIME = 'transportRuntime',
  UPLOAD_RUNTIME = 'uploadRuntime',
  APPLY = 'apply',
  RECORDING_METADATA = 'recordingMetadata'
}

export enum CanonicalShadowMigrationFailure {
  BLOCKED_PRODUCTION_EXECUTE = 'blockedProductionExecute',
  INSUFFICIENT_LOCAL_SNAPSHOT = 'insufficientLocalSnapshot',
  INSUFFICIENT_PEER_SNAPSHOT = 'insufficientPeerSnapshot',
  DRY_RUN_FAILED = 'dryRunFailed'
}

export class CanonicalExecutionShadowEvent {
  kind: CanonicalExecutionShadowEventKind
  syncRunID: string | null
  trigger: CanonicalShadowMigrationTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  mode: CanonicalShadowMigrationMode
  domain: CanonicalProductionDomain
  shadowRootKind: CanonicalShadowRootKind | null
  sideEffectClass: string | null
  suppressionStatus: string | null
  reason: string | null
  plannedFileWriteCount: number
  plannedUploadCount: number
  plannedApplyCount: number
  divergenceCount: number
  generatedAt: CanonicalTimestamp

  constructor(
    kind: CanonicalExecutionShadowEventKind,
    syncRunID: string | null,
    trigger: CanonicalShadowMigrationTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole,
    mode: CanonicalShadowMigrationMode,
    domain: CanonicalProductionDomain,
    shadowRootKind: CanonicalShadowRootKind | null = null,
    sideEffectClass: string | null = null,
    suppressionStatus: string | null = null,
    reason: string | null = null,
    plannedFileWriteCount: number = 0,
    plannedUploadCount: number = 0,
    plannedApplyCount: number = 0,
    divergenceCount: number = 0,
    generatedAt: Date = new Date()
  ) {
    this.kind = kind
    this.syncRunID = CanonicalShadowMigrationRedaction.safeIdentifier(syncRunID)
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.mode = mode
    this.domain = domain
    this.shadowRootKind = shadowRootKind
    this.sideEffectClass = CanonicalShadowMigrationRedaction.safeText(sideEffectClass)
    this.suppressionStatus = CanonicalShadowMigrationRedaction.safeText(suppressionStatus)
    this.reason = CanonicalShadowMigrationRedaction.safeText(reason)
    this.plannedFileWriteCount = Math.max(0, plannedFileWriteCount)
    this.plannedUploadCount = Math.max(0, plannedUploadCount)
    this.plannedApplyCount = Math.max(0, plannedApplyCount)
    this.divergenceCount = Math.max(0, divergenceCount)
    this.generatedAt = new CanonicalTimestamp(generatedAt)
  }

  get id(): string {
    return [
      this.kind, this.syncRunID ?? '', this.trigger, this.nodeRole, this.mode,
      this.domain, this.sideEffectClass ?? '', this.reason ?? ''
    ].join('|')
  }

  get diagnosticsSummary(): string {
    return [
      'trigger=' + this.trigger,
      'nodeRole=' + this.nodeRole,
      'mode=' + this.mode,
      'domain=' + this.domain,
      'shadowRootKind=' + (this.shadowRootKind ?? 'none'),
      'sideEffect=' + (this.sideEffectClass ?? 'none'),
      'suppression=' + (this.suppressionStatus ?? 'none'),
      'fileWrites=' + this.plannedFileWriteCount,
      'uploads=' + this.plannedUploadCount,
      'applies=' + this.plannedApplyCount,
      'divergences=' + this.divergenceCount,
      'reason=' + (this.reason ?? 'none')
    ].join(',')
  }
}

// =============================================================================
// SECTION 18: Execution Shadow Report
// =============================================================================

export class CanonicalExecutionShadowReport {
  runID: string
  syncRunID: string | null
  trigger: CanonicalShadowMigrationTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  mode: CanonicalShadowMigrationMode
  generatedAt: CanonicalTimestamp
  dryRunEquivalent: boolean
  blocked: boolean
  shadowRootKind: CanonicalShadowRootKind | null
  fileReport: CanonicalShadowFileExecutionReport | null
  uploadResult: CanonicalShadowUploadResult | null
  applyResult: CanonicalShadowApplyResult | null
  transportProbeResult: CanonicalShadowTransportProbeResult | null
  realDataShadowCopyResult: object | null
  shadowRootCleanupResult: object | null
  readOnlyTransportProbeResult: object | null
  productionAudit: object | null
  events: CanonicalExecutionShadowEvent[]
  failure: CanonicalShadowMigrationFailure | null
  failureReason: string | null

  constructor(
    runID: string,
    syncRunID: string | null,
    trigger: CanonicalShadowMigrationTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole,
    mode: CanonicalShadowMigrationMode,
    dryRunEquivalent: boolean,
    blocked: boolean,
    shadowRootKind: CanonicalShadowRootKind | null = null,
    fileReport: CanonicalShadowFileExecutionReport | null = null,
    uploadResult: CanonicalShadowUploadResult | null = null,
    applyResult: CanonicalShadowApplyResult | null = null,
    transportProbeResult: CanonicalShadowTransportProbeResult | null = null,
    realDataShadowCopyResult: object | null = null,
    shadowRootCleanupResult: object | null = null,
    readOnlyTransportProbeResult: object | null = null,
    productionAudit: object | null = null,
    events: CanonicalExecutionShadowEvent[],
    failure: CanonicalShadowMigrationFailure | null = null,
    failureReason: string | null = null,
    generatedAt: Date = new Date()
  ) {
    this.runID = CanonicalShadowMigrationRedaction.safeIdentifier(runID) ?? 'execution-shadow-run'
    this.syncRunID = CanonicalShadowMigrationRedaction.safeIdentifier(syncRunID)
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.mode = mode
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.dryRunEquivalent = dryRunEquivalent
    this.blocked = blocked
    this.shadowRootKind = shadowRootKind
    this.fileReport = fileReport
    this.uploadResult = uploadResult
    this.applyResult = applyResult
    this.transportProbeResult = transportProbeResult
    this.realDataShadowCopyResult = realDataShadowCopyResult
    this.shadowRootCleanupResult = shadowRootCleanupResult
    this.readOnlyTransportProbeResult = readOnlyTransportProbeResult
    this.productionAudit = productionAudit
    this.events = events
    this.failure = failure
    this.failureReason = CanonicalShadowMigrationRedaction.safeText(failureReason)
  }

  get id(): string {
    return this.runID
  }
}

// =============================================================================
// SECTION 19: Migration Gate & Configuration
// =============================================================================

export class CanonicalShadowMigrationPolicy {
  maxDiagnosticsEvents: number = 100
  failureIsFatal: boolean = false
  realDataShadowCopyPolicy: { isEnabled: boolean } = { isEnabled: false }
  readOnlyTransportProbePolicy: { isEnabled: boolean } = { isEnabled: false }
  networkProbePolicy: CanonicalShadowNetworkProbePolicy = new CanonicalShadowNetworkProbePolicy()
}

export class CanonicalShadowMigrationConfiguration {
  effectiveMode: CanonicalShadowMigrationMode
  policy: CanonicalShadowMigrationPolicy

  constructor(effectiveMode: CanonicalShadowMigrationMode, policy: CanonicalShadowMigrationPolicy = new CanonicalShadowMigrationPolicy()) {
    this.effectiveMode = effectiveMode
    this.policy = policy
  }

  static enabled(mode: CanonicalShadowMigrationMode): CanonicalShadowMigrationConfiguration {
    return new CanonicalShadowMigrationConfiguration(mode)
  }
}

export class CanonicalShadowMigrationGate {
  allowed: boolean
  trigger: CanonicalShadowMigrationTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  mode: CanonicalShadowMigrationMode
  reason: string
  failure: CanonicalShadowMigrationFailure | null

  constructor(
    allowed: boolean, trigger: CanonicalShadowMigrationTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole,
    mode: CanonicalShadowMigrationMode, reason: string,
    failure: CanonicalShadowMigrationFailure | null = null
  ) {
    this.allowed = allowed
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.mode = mode
    this.reason = reason
    this.failure = failure
  }

  static evaluate(
    configuration: CanonicalShadowMigrationConfiguration,
    trigger: CanonicalShadowMigrationTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole
  ): CanonicalShadowMigrationGate {
    const mode = configuration.effectiveMode
    switch (mode) {
      case CanonicalShadowMigrationMode.DISABLED:
      case CanonicalShadowMigrationMode.BLOCKED_PRODUCTION_EXECUTE:
        return new CanonicalShadowMigrationGate(
          false, trigger, nodeRole, mode, 'disabledOrBlocked',
          mode === CanonicalShadowMigrationMode.BLOCKED_PRODUCTION_EXECUTE
            ? CanonicalShadowMigrationFailure.BLOCKED_PRODUCTION_EXECUTE : null
        )
      default:
        return new CanonicalShadowMigrationGate(true, trigger, nodeRole, mode, 'allowed')
    }
  }
}

// =============================================================================
// SECTION 20: Redaction Utility (Shadow-specific)
// =============================================================================

export class CanonicalShadowMigrationRedaction {
  static safeIdentifier(value: string | null | undefined): string | null {
    const trimmed = value?.trim()
    if (!trimmed || trimmed.length === 0) {
      return null
    }
    return trimmed
  }

  static safeText(value: string | null | undefined): string | null {
    const trimmed = value?.trim()
    if (!trimmed || trimmed.length === 0) {
      return null
    }
    return trimmed
  }
}
