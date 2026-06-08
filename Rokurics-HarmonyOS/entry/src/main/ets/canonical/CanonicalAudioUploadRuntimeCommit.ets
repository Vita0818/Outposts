/**
 * CanonicalAudioUploadRuntimeCommit — audio upload runtime commit domain.
 * Ported from Rokurics-Apple (CanonicalAudioUploadRuntimeCommit.swift).
 * Runtime mode evaluation, chunk-based transport, session management, retry, and finalize.
 */

import {
  CanonicalTimestamp, CanonicalHash, nilIfEmpty, hashPrefix
} from './CanonicalCore'
import {
  CanonicalAudioUploadCutoverCandidate, CanonicalAudioUploadTriggerSource,
  CanonicalAudioUploadDiagnostic, CanonicalAudioUploadDiagnosticKind,
  CanonicalAudioUploadNodeRole, CanonicalAudioUploadLedgerPhase,
  CanonicalAudioUploadPeerState, CanonicalAudioUploadActionKind,
  isViewRefresh, isRetryDrainer, isExplicitManualUploadButton,
} from './CanonicalAudioUploadCutover'

// ═══════════════════════════════════════════════════════════════════════════════
// FORWARD TYPES / MINIMAL INTERFACES
// ═══════════════════════════════════════════════════════════════════════════════

export class CanonicalUploadSessionID {
  rawValue: string

  constructor(rawValue: string) {
    this.rawValue = rawValue
  }
}

export class CanonicalFileReference {
  rootToken: string
  logicalPathToken: string

  constructor(rootToken: string, logicalPathToken: string) {
    this.rootToken = rootToken
    this.logicalPathToken = logicalPathToken
  }
}

export interface CanonicalProductionUploadPort {
  resumableSessionSupported: boolean
  isDryRunOnly: boolean
  chunkSizePolicy: number
  startResumableUpload(req: CanonicalUploadStartRequest, now: Date): Promise<CanonicalUploadSessionStatus>
  resumeUpload(req: CanonicalUploadStatusRequest, now: Date): Promise<CanonicalUploadSessionStatus>
  uploadChunk(chunk: CanonicalUploadChunk, now: Date): Promise<CanonicalUploadChunkStatus>
  finalizeUpload(req: CanonicalUploadFinalizeRequest, now: Date): Promise<CanonicalUploadFinalizeResponse>
  writeUploadLedger(snapshot: CanonicalProductionUploadLedgerSnapshot): Promise<void>
}

export class CanonicalUploadStartRequest {
  objectID: string
  targetReference: CanonicalFileReference
  totalBytes: number
  totalHash: CanonicalHash
  chunkSize: number
  idempotencyKey: string

  constructor(
    objectID: string, targetReference: CanonicalFileReference, totalBytes: number,
    totalHash: CanonicalHash, chunkSize: number, idempotencyKey: string
  ) {
    this.objectID = objectID
    this.targetReference = targetReference
    this.totalBytes = totalBytes
    this.totalHash = totalHash
    this.chunkSize = chunkSize
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

export class CanonicalUploadSessionStatus {
  sessionID: CanonicalUploadSessionID | null
  confirmedBytes: number
  state: string

  constructor(sessionID: CanonicalUploadSessionID | null, confirmedBytes: number, state: string) {
    this.sessionID = sessionID
    this.confirmedBytes = Math.max(0, confirmedBytes)
    this.state = state
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
    objectID: string, sessionID: CanonicalUploadSessionID, offset: number,
    bytes: Uint8Array, chunkHash: CanonicalHash, totalHash: CanonicalHash, idempotencyKey: string
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

export class CanonicalUploadChunkStatus {
  confirmedBytes: number

  constructor(confirmedBytes: number) {
    this.confirmedBytes = Math.max(0, confirmedBytes)
  }
}

export class CanonicalUploadFinalizeRequest {
  objectID: string
  sessionID: CanonicalUploadSessionID
  totalBytes: number
  totalHash: CanonicalHash

  constructor(objectID: string, sessionID: CanonicalUploadSessionID, totalBytes: number, totalHash: CanonicalHash) {
    this.objectID = objectID
    this.sessionID = sessionID
    this.totalBytes = totalBytes
    this.totalHash = totalHash
  }
}

export class CanonicalUploadFinalizeResponse {
  completed: boolean
  fileSize: number | null
  checksum: CanonicalHash | null

  constructor(completed: boolean, fileSize: number | null = null, checksum: CanonicalHash | null = null) {
    this.completed = completed
    this.fileSize = fileSize
    this.checksum = checksum
  }
}

export class CanonicalProductionUploadLedgerSnapshot {
  objectID: string
  sessionID: CanonicalUploadSessionID
  confirmedBytes: number
  totalBytes: number
  contentHash: CanonicalHash
  phase: CanonicalAudioUploadLedgerPhase

  constructor(
    objectID: string, sessionID: CanonicalUploadSessionID, confirmedBytes: number,
    totalBytes: number, contentHash: CanonicalHash, phase: CanonicalAudioUploadLedgerPhase
  ) {
    this.objectID = objectID
    this.sessionID = sessionID
    this.confirmedBytes = confirmedBytes
    this.totalBytes = totalBytes
    this.contentHash = contentHash
    this.phase = phase
  }
}

export class CanonicalTransportEnvelope {
  static hash(data: Uint8Array): CanonicalHash {
    const content: Record<string, string> = { 'bytes': data.length.toString() }
    return CanonicalHash.sha256OfMap(content)
  }
}

export enum CanonicalUploadRuntimeErrorKind {
  INVALID_REQUEST = 'invalidRequest',
  INVALID_SESSION = 'invalidSession',
  SESSION_MISSING = 'sessionMissing',
  SESSION_CONFLICT = 'sessionConflict',
  CHUNK_OFFSET_MISMATCH = 'chunkOffsetMismatch',
  CHUNK_HASH_MISMATCH = 'chunkHashMismatch',
  FINAL_HASH_MISMATCH = 'finalHashMismatch',
  TARGET_CONFLICT = 'targetConflict',
  SESSION_INCOMPLETE = 'sessionIncomplete',
  RETRY_LIMIT_EXCEEDED = 'retryLimitExceeded'
}

// ═══════════════════════════════════════════════════════════════════════════════
// MAIN TYPES
// ═══════════════════════════════════════════════════════════════════════════════

export enum CanonicalAudioUploadRuntimeMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  NO_COMMIT = 'noCommit',
  TEST_TRANSPORT_UPLOAD = 'testTransportUpload',
  CANONICAL_UPLOAD_WITH_LEGACY_FALLBACK = 'canonicalUploadWithLegacyFallback',
  BLOCKED = 'blocked'
}

export function runtimeModeCreatesJob(mode: CanonicalAudioUploadRuntimeMode): boolean {
  return mode === CanonicalAudioUploadRuntimeMode.TEST_TRANSPORT_UPLOAD ||
    mode === CanonicalAudioUploadRuntimeMode.CANONICAL_UPLOAD_WITH_LEGACY_FALLBACK
}

export function runtimeModeSendsTransport(mode: CanonicalAudioUploadRuntimeMode): boolean {
  return mode === CanonicalAudioUploadRuntimeMode.TEST_TRANSPORT_UPLOAD ||
    mode === CanonicalAudioUploadRuntimeMode.CANONICAL_UPLOAD_WITH_LEGACY_FALLBACK
}

export class CanonicalAudioUploadRetryPolicy {
  maxAttempts: number
  retryDelaySeconds: number

  constructor(maxAttempts: number = 3, retryDelaySeconds: number = 5) {
    this.maxAttempts = Math.max(1, maxAttempts)
    this.retryDelaySeconds = Math.max(0, retryDelaySeconds)
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
  chunkSize: number
  retryPolicy: CanonicalAudioUploadRetryPolicy

  constructor(
    debugInternalBuild: boolean = false, ownerApprovedCanonicalCommit: boolean = false,
    allowTestTransportUpload: boolean = false, allowCanonicalUploadWithLegacyFallback: boolean = false,
    legacyFallbackEnabled: boolean = true, requireExistingSecureUploadRoutes: boolean = true,
    retryDrainerRequiresExistingRetry: boolean = true, chunkSize: number = 4 * 1024 * 1024,
    retryPolicy: CanonicalAudioUploadRetryPolicy = new CanonicalAudioUploadRetryPolicy()
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApprovedCanonicalCommit = ownerApprovedCanonicalCommit
    this.allowTestTransportUpload = allowTestTransportUpload
    this.allowCanonicalUploadWithLegacyFallback = allowCanonicalUploadWithLegacyFallback
    this.legacyFallbackEnabled = legacyFallbackEnabled
    this.requireExistingSecureUploadRoutes = requireExistingSecureUploadRoutes
    this.retryDrainerRequiresExistingRetry = retryDrainerRequiresExistingRetry
    this.chunkSize = Math.max(1, chunkSize)
    this.retryPolicy = retryPolicy
  }

  static get releaseDefault(): CanonicalAudioUploadRuntimePolicy {
    return new CanonicalAudioUploadRuntimePolicy()
  }

  static testTransport(
    chunkSize: number = 4 * 1024 * 1024,
    retryPolicy: CanonicalAudioUploadRetryPolicy = new CanonicalAudioUploadRetryPolicy()
  ): CanonicalAudioUploadRuntimePolicy {
    return new CanonicalAudioUploadRuntimePolicy(true, true, true, false, true, true, true, chunkSize, retryPolicy)
  }
}

export class CanonicalAudioUploadRuntimeConfiguration {
  mode: CanonicalAudioUploadRuntimeMode
  policy: CanonicalAudioUploadRuntimePolicy

  constructor(
    mode: CanonicalAudioUploadRuntimeMode = CanonicalAudioUploadRuntimeMode.DISABLED,
    policy: CanonicalAudioUploadRuntimePolicy = CanonicalAudioUploadRuntimePolicy.releaseDefault
  ) {
    this.mode = mode
    this.policy = policy
  }

  static get disabled(): CanonicalAudioUploadRuntimeConfiguration {
    return new CanonicalAudioUploadRuntimeConfiguration()
  }

  static diagnosticsOnly(
    policy: CanonicalAudioUploadRuntimePolicy = CanonicalAudioUploadRuntimePolicy.releaseDefault
  ): CanonicalAudioUploadRuntimeConfiguration {
    return new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.DIAGNOSTICS_ONLY, policy)
  }

  static noCommit(
    policy: CanonicalAudioUploadRuntimePolicy = CanonicalAudioUploadRuntimePolicy.releaseDefault
  ): CanonicalAudioUploadRuntimeConfiguration {
    return new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.NO_COMMIT, policy)
  }

  static testTransportUpload(
    chunkSize: number = 4 * 1024 * 1024,
    retryPolicy: CanonicalAudioUploadRetryPolicy = new CanonicalAudioUploadRetryPolicy()
  ): CanonicalAudioUploadRuntimeConfiguration {
    return new CanonicalAudioUploadRuntimeConfiguration(CanonicalAudioUploadRuntimeMode.TEST_TRANSPORT_UPLOAD,
      CanonicalAudioUploadRuntimePolicy.testTransport(chunkSize, retryPolicy))
  }
}

export enum CanonicalAudioUploadRuntimeOutcome {
  LEGACY_FALLBACK = 'legacyFallback',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  NO_COMMIT = 'noCommit',
  NO_OP = 'noOp',
  DEFERRED = 'deferred',
  UPLOADED = 'uploaded',
  RETRY_SCHEDULED = 'retryScheduled',
  CONFLICT = 'conflict',
  BLOCKED = 'blocked',
  FAILED = 'failed'
}

export class CanonicalAudioUploadRuntimeResult {
  mode: CanonicalAudioUploadRuntimeMode
  outcome: CanonicalAudioUploadRuntimeOutcome
  objectID: string
  sessionID: CanonicalUploadSessionID | null
  createdJob: boolean
  startedTransport: boolean
  sentChunkCount: number
  confirmedBytes: number
  completed: boolean
  usedLegacyFallback: boolean
  legacyFallbackReason: string | null
  finalizeProof: CanonicalAudioUploadFinalizeProof | null
  retryRecord: CanonicalAudioUploadRetryRecord | null
  diagnostics: CanonicalAudioUploadDiagnostic[]

  constructor(
    mode: CanonicalAudioUploadRuntimeMode, outcome: CanonicalAudioUploadRuntimeOutcome, objectID: string,
    sessionID: CanonicalUploadSessionID | null = null, createdJob: boolean = false,
    startedTransport: boolean = false, sentChunkCount: number = 0, confirmedBytes: number = 0,
    completed: boolean = false, usedLegacyFallback: boolean = false,
    legacyFallbackReason: string | null = null, finalizeProof: CanonicalAudioUploadFinalizeProof | null = null,
    retryRecord: CanonicalAudioUploadRetryRecord | null = null,
    diagnostics: CanonicalAudioUploadDiagnostic[] = []
  ) {
    this.mode = mode
    this.outcome = outcome
    this.objectID = safeIdentifier(objectID, 'unknown-recording')
    this.sessionID = sessionID
    this.createdJob = createdJob
    this.startedTransport = startedTransport
    this.sentChunkCount = sentChunkCount
    this.confirmedBytes = Math.max(0, confirmedBytes)
    this.completed = completed
    this.usedLegacyFallback = usedLegacyFallback
    this.legacyFallbackReason = safeText(legacyFallbackReason)
    this.finalizeProof = finalizeProof
    this.retryRecord = retryRecord
    this.diagnostics = diagnostics
  }
}

export class CanonicalAudioUploadOffset {
  value: number

  constructor(value: number = 0) {
    this.value = Math.max(0, value)
  }

  compareTo(other: CanonicalAudioUploadOffset): number {
    return this.value - other.value
  }
}

export enum CanonicalAudioUploadSessionState {
  IDLE = 'idle',
  STARTING = 'starting',
  STARTED = 'started',
  CHUNKING = 'chunking',
  INTERRUPTED = 'interrupted',
  RESUMING = 'resuming',
  FINALIZING = 'finalizing',
  FINALIZED = 'finalized',
  FAILED = 'failed',
  ABORTED = 'aborted',
  CONFLICT = 'conflict',
  BLOCKED = 'blocked'
}

export function sessionStateIsTerminal(state: CanonicalAudioUploadSessionState): boolean {
  switch (state) {
    case CanonicalAudioUploadSessionState.FINALIZED:
    case CanonicalAudioUploadSessionState.FAILED:
    case CanonicalAudioUploadSessionState.ABORTED:
    case CanonicalAudioUploadSessionState.CONFLICT:
    case CanonicalAudioUploadSessionState.BLOCKED:
      return true
    default:
      return false
  }
}

export class CanonicalAudioUploadChunk {
  objectID: string
  sessionID: CanonicalUploadSessionID
  offset: CanonicalAudioUploadOffset
  length: number
  chunkHashPrefix: string | null
  idempotencyKey: string

  constructor(
    objectID: string, sessionID: CanonicalUploadSessionID, offset: CanonicalAudioUploadOffset,
    length: number, chunkHash: CanonicalHash | null = null, idempotencyKey: string | null = null
  ) {
    this.objectID = safeIdentifier(objectID, 'unknown-recording')
    this.sessionID = sessionID
    this.offset = offset
    this.length = Math.max(0, length)
    this.chunkHashPrefix = chunkHash ? hashPrefix(chunkHash.value) : null
    this.idempotencyKey = idempotencyKey?.trim() || (this.objectID + ':' + sessionID.rawValue + ':' +
      offset.value + ':' + this.length)
  }

  get endOffset(): number {
    return this.offset.value + this.length
  }
}

export class CanonicalAudioUploadFinalizeProof {
  objectID: string
  sessionID: CanonicalUploadSessionID
  byteSize: number
  contentHashPrefix: string | null
  macFileSizeVerified: boolean
  macHashVerified: boolean
  macProofReceived: boolean
  receiveRecordMatchesAudioAvailability: boolean

  constructor(
    objectID: string, sessionID: CanonicalUploadSessionID, byteSize: number,
    contentHash: CanonicalHash | null, macFileSizeVerified: boolean, macHashVerified: boolean,
    macProofReceived: boolean, receiveRecordMatchesAudioAvailability: boolean
  ) {
    this.objectID = safeIdentifier(objectID, 'unknown-recording')
    this.sessionID = sessionID
    this.byteSize = Math.max(0, byteSize)
    this.contentHashPrefix = contentHash ? hashPrefix(contentHash.value) : null
    this.macFileSizeVerified = macFileSizeVerified
    this.macHashVerified = macHashVerified
    this.macProofReceived = macProofReceived
    this.receiveRecordMatchesAudioAvailability = receiveRecordMatchesAudioAvailability
  }

  get accepted(): boolean {
    return this.macFileSizeVerified && this.macHashVerified && this.macProofReceived &&
      this.receiveRecordMatchesAudioAvailability
  }
}

export class CanonicalAudioUploadAbort {
  objectID: string
  sessionID: CanonicalUploadSessionID | null
  reason: string
  preFinalizeOnly: boolean
  productionAudioDeleted: boolean
  receiveRecordDeleted: boolean

  constructor(
    objectID: string, sessionID: CanonicalUploadSessionID | null, reason: string,
    preFinalizeOnly: boolean = true, productionAudioDeleted: boolean = false,
    receiveRecordDeleted: boolean = false
  ) {
    this.objectID = safeIdentifier(objectID, 'unknown-recording')
    this.sessionID = sessionID
    this.reason = safeText(reason) ?? 'abort'
    this.preFinalizeOnly = preFinalizeOnly
    this.productionAudioDeleted = productionAudioDeleted
    this.receiveRecordDeleted = receiveRecordDeleted
  }
}

export class CanonicalAudioUploadResumeToken {
  objectID: string
  sessionID: CanonicalUploadSessionID
  offset: CanonicalAudioUploadOffset
  byteSize: number
  contentHashPrefix: string | null

  constructor(
    objectID: string, sessionID: CanonicalUploadSessionID, offset: CanonicalAudioUploadOffset,
    byteSize: number, contentHashPrefix: string | null
  ) {
    this.objectID = safeIdentifier(objectID, 'unknown-recording')
    this.sessionID = sessionID
    this.offset = offset
    this.byteSize = Math.max(0, byteSize)
    this.contentHashPrefix = hashPrefix(contentHashPrefix)
  }
}

export class CanonicalAudioUploadRetryRecord {
  objectID: string
  sessionID: CanonicalUploadSessionID | null
  offset: CanonicalAudioUploadOffset
  chunkSize: number
  contentHashPrefix: string | null
  byteSize: number
  state: CanonicalAudioUploadSessionState
  attemptCount: number
  nextRetryAt: CanonicalTimestamp | null
  lastErrorCode: string | null
  terminalConflict: boolean
  updatedAt: CanonicalTimestamp

  constructor(
    objectID: string, sessionID: CanonicalUploadSessionID | null = null,
    offset: CanonicalAudioUploadOffset = new CanonicalAudioUploadOffset(),
    chunkSize: number, contentHash: CanonicalHash | null = null, contentHashPrefix: string | null = null,
    byteSize: number, state: CanonicalAudioUploadSessionState, attemptCount: number = 0,
    nextRetryAt: CanonicalTimestamp | null = null, lastErrorCode: string | null = null,
    terminalConflict: boolean = false, updatedAt: Date = new Date()
  ) {
    this.objectID = safeIdentifier(objectID, 'unknown-recording')
    this.sessionID = sessionID
    this.offset = offset
    this.chunkSize = Math.max(1, chunkSize)
    this.contentHashPrefix = contentHash ? hashPrefix(contentHash.value) : hashPrefix(contentHashPrefix)
    this.byteSize = Math.max(0, byteSize)
    this.state = state
    this.attemptCount = Math.max(0, attemptCount)
    this.nextRetryAt = nextRetryAt
    this.lastErrorCode = safeText(lastErrorCode)
    this.terminalConflict = terminalConflict
    this.updatedAt = new CanonicalTimestamp(updatedAt)
  }

  get resumeToken(): CanonicalAudioUploadResumeToken | null {
    if (!this.sessionID) {
      return null
    }
    return new CanonicalAudioUploadResumeToken(this.objectID, this.sessionID, this.offset, this.byteSize,
      this.contentHashPrefix)
  }

  isEligibleRetry(now: Date): boolean {
    if (this.terminalConflict) {
      return false
    }
    if (sessionStateIsTerminal(this.state) && this.state !== CanonicalAudioUploadSessionState.INTERRUPTED) {
      return false
    }
    if (this.state === CanonicalAudioUploadSessionState.CONFLICT ||
      this.state === CanonicalAudioUploadSessionState.BLOCKED ||
      this.state === CanonicalAudioUploadSessionState.ABORTED ||
      this.state === CanonicalAudioUploadSessionState.FINALIZED ||
      this.state === CanonicalAudioUploadSessionState.FAILED) {
      return false
    }
    if (!this.nextRetryAt) {
      return this.state === CanonicalAudioUploadSessionState.INTERRUPTED
    }
    return this.nextRetryAt.date.getTime() <= now.getTime()
  }
}

export class CanonicalAudioUploadSession {
  objectID: string
  sessionID: CanonicalUploadSessionID | null
  state: CanonicalAudioUploadSessionState
  confirmedBytes: number
  offset: CanonicalAudioUploadOffset
  chunkSize: number
  expectedByteSize: number
  contentHashPrefix: string | null
  finalizedProof: CanonicalAudioUploadFinalizeProof | null
  lastErrorCode: string | null

  constructor(
    objectID: string, sessionID: CanonicalUploadSessionID | null = null,
    state: CanonicalAudioUploadSessionState = CanonicalAudioUploadSessionState.IDLE,
    confirmedBytes: number = 0, chunkSize: number, expectedByteSize: number,
    contentHash: CanonicalHash | null = null, contentHashPrefix: string | null = null
  ) {
    this.objectID = safeIdentifier(objectID, 'unknown-recording')
    this.sessionID = sessionID
    this.state = state
    this.confirmedBytes = Math.max(0, confirmedBytes)
    this.offset = new CanonicalAudioUploadOffset(confirmedBytes)
    this.chunkSize = Math.max(1, chunkSize)
    this.expectedByteSize = Math.max(0, expectedByteSize)
    this.contentHashPrefix = contentHash ? hashPrefix(contentHash.value) : hashPrefix(contentHashPrefix)
    this.finalizedProof = null
    this.lastErrorCode = null
  }

  markStarted(sessionID: CanonicalUploadSessionID, confirmedBytes: number = 0): void {
    this.sessionID = sessionID
    this.state = CanonicalAudioUploadSessionState.STARTED
    this.updateConfirmedBytes(confirmedBytes)
  }

  updateConfirmedBytes(newConfirmedBytes: number): void {
    const bounded = Math.min(Math.max(0, newConfirmedBytes), this.expectedByteSize)
    if (bounded < this.confirmedBytes) {
      throw new Error('confirmedBytesRegressed: previous=' + this.confirmedBytes + ', actual=' + bounded)
    }
    this.confirmedBytes = bounded
    this.offset = new CanonicalAudioUploadOffset(bounded)
  }

  confirm(chunk: CanonicalAudioUploadChunk, serverConfirmedBytes: number): void {
    if (chunk.objectID !== this.objectID) {
      throw new Error('sessionConflict: objectMismatch')
    }
    if (chunk.offset.value < this.confirmedBytes) {
      if (chunk.endOffset > this.confirmedBytes) {
        throw new Error('chunkOffsetMismatch: expected=' + this.confirmedBytes + ', actual=' + chunk.offset.value)
      }
      return
    }
    if (chunk.offset.value !== this.confirmedBytes) {
      throw new Error('chunkOffsetMismatch: expected=' + this.confirmedBytes + ', actual=' + chunk.offset.value)
    }
    if (chunk.endOffset > this.expectedByteSize) {
      throw new Error('chunkOffsetMismatch: expected=' + this.expectedByteSize + ', actual=' + chunk.endOffset)
    }
    this.state = CanonicalAudioUploadSessionState.CHUNKING
    this.updateConfirmedBytes(Math.max(serverConfirmedBytes, chunk.endOffset))
  }

  markFinalized(proof: CanonicalAudioUploadFinalizeProof): void {
    if (!proof.accepted) {
      this.state = CanonicalAudioUploadSessionState.CONFLICT
      this.lastErrorCode = 'finalizeProofRejected'
      throw new Error('finalizeProofRejected')
    }
    if (proof.byteSize !== this.expectedByteSize) {
      this.state = CanonicalAudioUploadSessionState.CONFLICT
      this.lastErrorCode = 'finalByteSizeMismatch'
      throw new Error('finalByteSizeMismatch: expected=' + this.expectedByteSize + ', actual=' + proof.byteSize)
    }
    this.finalizedProof = proof
    this.state = CanonicalAudioUploadSessionState.FINALIZED
    this.confirmedBytes = this.expectedByteSize
    this.offset = new CanonicalAudioUploadOffset(this.expectedByteSize)
    this.lastErrorCode = null
  }
}

export interface CanonicalAudioUploadByteSource {
  readonly objectID: string
  readonly targetReference: CanonicalFileReference
  readonly byteSize: number
  readonly contentHash: CanonicalHash
  readonly preferredChunkSize: number
  readChunk(offset: CanonicalAudioUploadOffset, maxLength: number): Promise<Uint8Array>
}

export class CanonicalAudioUploadJobStore {
  private records: Record<string, CanonicalAudioUploadRetryRecord> = {}

  constructor(initialRecords: CanonicalAudioUploadRetryRecord[] = []) {
    for (const r of initialRecords) {
      this.records[r.objectID] = r
    }
  }

  recordFor(objectID: string): CanonicalAudioUploadRetryRecord | null {
    return this.records[safeIdentifier(objectID, 'unknown-recording')] ?? null
  }

  allRecords(): CanonicalAudioUploadRetryRecord[] {
    return Object.values(this.records).sort((a, b) => a.objectID.localeCompare(b.objectID))
  }

  async upsert(record: CanonicalAudioUploadRetryRecord): Promise<CanonicalAudioUploadRetryRecord> {
    this.records[record.objectID] = record
    return record
  }

  async remove(objectID: string): Promise<void> {
    delete this.records[safeIdentifier(objectID, 'unknown-recording')]
  }

  hasEligibleRetry(objectID: string, now: Date): boolean {
    const r = this.recordFor(objectID)
    return r !== null && r.isEligibleRetry(now)
  }

  eligibleRetryRecords(now: Date): CanonicalAudioUploadRetryRecord[] {
    return Object.values(this.records)
      .filter((r: CanonicalAudioUploadRetryRecord) => r.isEligibleRetry(now))
      .sort((a, b) => a.objectID.localeCompare(b.objectID))
  }

  async recordProgress(
    objectID: string, sessionID: CanonicalUploadSessionID | null, offset: CanonicalAudioUploadOffset,
    chunkSize: number, contentHash: CanonicalHash, byteSize: number,
    state: CanonicalAudioUploadSessionState, now: Date
  ): Promise<CanonicalAudioUploadRetryRecord> {
    const existing = this.recordFor(objectID)
    const monotonicOffset = new CanonicalAudioUploadOffset(
      Math.max(existing?.offset.value ?? 0, offset.value))
    const record = new CanonicalAudioUploadRetryRecord(objectID, sessionID, monotonicOffset, chunkSize, contentHash,
      null, byteSize, state, existing?.attemptCount ?? 0, existing?.nextRetryAt ?? null,
      existing?.lastErrorCode ?? null, existing?.terminalConflict ?? false, now)
    return this.upsert(record)
  }

  async scheduleRetry(
    objectID: string, sessionID: CanonicalUploadSessionID | null, offset: CanonicalAudioUploadOffset,
    chunkSize: number, contentHash: CanonicalHash, byteSize: number,
    policy: CanonicalAudioUploadRetryPolicy, errorCode: string, now: Date
  ): Promise<CanonicalAudioUploadRetryRecord> {
    const existing = this.recordFor(objectID)
    const nextAttempt = (existing?.attemptCount ?? 0) + 1
    const state: CanonicalAudioUploadSessionState =
      nextAttempt >= policy.maxAttempts ? CanonicalAudioUploadSessionState.FAILED :
        CanonicalAudioUploadSessionState.INTERRUPTED
    const nextRetryAt = state === CanonicalAudioUploadSessionState.FAILED ? null :
      new CanonicalTimestamp(new Date(now.getTime() + policy.retryDelaySeconds * 1000))
    return this.upsert(new CanonicalAudioUploadRetryRecord(objectID, sessionID,
      new CanonicalAudioUploadOffset(Math.max(existing?.offset.value ?? 0, offset.value)),
      chunkSize, contentHash, null, byteSize, state, nextAttempt, nextRetryAt, errorCode, false, now))
  }

  async markConflict(
    objectID: string, sessionID: CanonicalUploadSessionID | null, offset: CanonicalAudioUploadOffset,
    chunkSize: number, contentHash: CanonicalHash, byteSize: number, errorCode: string, now: Date
  ): Promise<CanonicalAudioUploadRetryRecord> {
    const existing = this.recordFor(objectID)
    return this.upsert(new CanonicalAudioUploadRetryRecord(objectID, sessionID,
      new CanonicalAudioUploadOffset(Math.max(existing?.offset.value ?? 0, offset.value)),
      chunkSize, contentHash, null, byteSize, CanonicalAudioUploadSessionState.CONFLICT,
      existing?.attemptCount ?? 0, null, errorCode, true, now))
  }
}

export class CanonicalAudioUploadRuntimeExecutor {
  constructor() {}

  async execute(
    candidate: CanonicalAudioUploadCutoverCandidate,
    source: CanonicalAudioUploadByteSource,
    uploadPort: CanonicalProductionUploadPort,
    jobStore: CanonicalAudioUploadJobStore,
    configuration: CanonicalAudioUploadRuntimeConfiguration = CanonicalAudioUploadRuntimeConfiguration.disabled,
    syncRunID: string | null = null, nodeRole: CanonicalAudioUploadNodeRole = CanonicalAudioUploadNodeRole.I_PHONE,
    now: Date = new Date()
  ): Promise<CanonicalAudioUploadRuntimeResult> {
    let diagnostics = this.candidateDiagnostics(candidate,
      CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_MODE_EVALUATED, syncRunID, nodeRole,
      configuration.mode, 'defaultReleaseDisabledUnlessExplicitlyEnabled')

    const mode = configuration.mode
    const policy = configuration.policy

    if (mode === CanonicalAudioUploadRuntimeMode.DISABLED) {
      diagnostics.push(this.legacyFallbackDiagnostic(candidate, syncRunID, nodeRole, 'runtimeDisabled'))
      return new CanonicalAudioUploadRuntimeResult(mode, CanonicalAudioUploadRuntimeOutcome.LEGACY_FALLBACK,
        candidate.objectID, null, false, false, 0, 0, false, policy.legacyFallbackEnabled,
        'runtimeDisabled', null, null, diagnostics)
    }

    if (mode === CanonicalAudioUploadRuntimeMode.BLOCKED) {
      return this.blockedResult(mode, candidate, diagnostics, 'runtimeBlocked')
    }

    const decision = await this.candidateDecision(candidate, jobStore, policy, now)
    diagnostics.push(...decision.diagnostics.map((k: CanonicalAudioUploadDiagnosticKind) =>
      this.candidateDiagnostics(candidate, k, syncRunID, nodeRole)[0]))

    switch (decision.action) {
      case 'noOp':
        return new CanonicalAudioUploadRuntimeResult(mode, CanonicalAudioUploadRuntimeOutcome.NO_OP,
          candidate.objectID, null, false, false, 0, candidate.localTruth.byteSize ?? 0, true, false,
          null, null, null, diagnostics)
      case 'deferred':
        return new CanonicalAudioUploadRuntimeResult(mode, CanonicalAudioUploadRuntimeOutcome.DEFERRED,
          candidate.objectID, null, false, false, 0, 0, false, false, null, null, null, diagnostics)
      case 'conflict':
        return new CanonicalAudioUploadRuntimeResult(mode, CanonicalAudioUploadRuntimeOutcome.CONFLICT,
          candidate.objectID, null, false, false, 0, 0, false, false, null, null, null, diagnostics)
      case 'blocked': {
        const reason = decision.reason ?? ''
        if (this.shouldUseLegacyFallback(mode, policy, reason)) {
          diagnostics.push(this.legacyFallbackDiagnostic(candidate, syncRunID, nodeRole, reason))
          return new CanonicalAudioUploadRuntimeResult(mode, CanonicalAudioUploadRuntimeOutcome.LEGACY_FALLBACK,
            candidate.objectID, null, false, false, 0, 0, false, true, reason, null, null, diagnostics)
        }
        return this.blockedResult(mode, candidate, diagnostics, reason)
      }
    }

    if (mode === CanonicalAudioUploadRuntimeMode.DIAGNOSTICS_ONLY) {
      diagnostics.push(this.candidateDiagnostics(candidate,
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_CANDIDATE_SELECTED,
        syncRunID, nodeRole, 'wouldUpload', candidate.reason)[0])
      return new CanonicalAudioUploadRuntimeResult(mode, CanonicalAudioUploadRuntimeOutcome.DIAGNOSTICS_ONLY,
        candidate.objectID, null, false, false, 0, 0, false, false, null, null, null, diagnostics)
    }

    if (mode === CanonicalAudioUploadRuntimeMode.NO_COMMIT) {
      diagnostics.push(this.candidateDiagnostics(candidate,
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_CANDIDATE_SELECTED,
        syncRunID, nodeRole, 'noCommitWouldUpload', candidate.reason)[0])
      return new CanonicalAudioUploadRuntimeResult(mode, CanonicalAudioUploadRuntimeOutcome.NO_COMMIT,
        candidate.objectID, null, false, false, 0, 0, false, false, null, null, null, diagnostics)
    }

    const modeBlocker = this.findModeBlocker(mode, policy, uploadPort)
    if (modeBlocker) {
      if (this.shouldUseLegacyFallback(mode, policy, modeBlocker)) {
        diagnostics.push(this.legacyFallbackDiagnostic(candidate, syncRunID, nodeRole, modeBlocker))
        return new CanonicalAudioUploadRuntimeResult(mode, CanonicalAudioUploadRuntimeOutcome.LEGACY_FALLBACK,
          candidate.objectID, null, false, false, 0, 0, false, true, modeBlocker, null, null, diagnostics)
      }
      return this.blockedResult(mode, candidate, diagnostics, modeBlocker)
    }

    return this.performUpload(candidate, source, uploadPort, jobStore, configuration, diagnostics, syncRunID,
      nodeRole, now)
  }

  private async performUpload(
    candidate: CanonicalAudioUploadCutoverCandidate, source: CanonicalAudioUploadByteSource,
    uploadPort: CanonicalProductionUploadPort, jobStore: CanonicalAudioUploadJobStore,
    configuration: CanonicalAudioUploadRuntimeConfiguration, diagnostics: CanonicalAudioUploadDiagnostic[],
    syncRunID: string | null, nodeRole: CanonicalAudioUploadNodeRole, now: Date
  ): Promise<CanonicalAudioUploadRuntimeResult> {
    const policy = configuration.policy
    const chunkSize = Math.min(Math.max(1, source.preferredChunkSize), Math.max(1, uploadPort.chunkSizePolicy),
      Math.max(1, policy.chunkSize))
    let sentChunkCount = 0
    let createdJob = false
    let startedTransport = false

    if (source.objectID !== candidate.objectID) {
      return this.blockedResult(configuration.mode, candidate, diagnostics, 'sourceObjectMismatch')
    }
    if (source.byteSize <= 0) {
      return this.blockedResult(configuration.mode, candidate, diagnostics, 'localAudioByteSizeUnavailable')
    }

    try {
      diagnostics.push(this.candidateDiagnostics(candidate,
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_CANDIDATE_SELECTED,
        syncRunID, nodeRole, 'upload', candidate.reason)[0])

      let existingRecord = await jobStore.recordFor(candidate.objectID)
      let session = new CanonicalAudioUploadSession(candidate.objectID, existingRecord?.sessionID ?? null,
        (existingRecord?.sessionID === null || existingRecord?.sessionID === undefined) ?
          CanonicalAudioUploadSessionState.IDLE : CanonicalAudioUploadSessionState.INTERRUPTED,
        existingRecord?.offset.value ?? 0, chunkSize, source.byteSize, source.contentHash)

      if (isRetryDrainer(candidate.trigger) && existingRecord?.isEligibleRetry(now) !== true) {
        diagnostics.push(this.candidateDiagnostics(candidate,
          CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RETRY_DRAINER_FRESH_JOB_SUPPRESSED,
          syncRunID, nodeRole, 'blocked', 'retryDrainerCannotCreateFreshAudioUploadJob')[0])
        return this.blockedResult(configuration.mode, candidate, diagnostics,
          'retryDrainerCannotCreateFreshAudioUploadJob')
      }

      let status: CanonicalUploadSessionStatus
      const resumeToken = existingRecord?.resumeToken ?? null
      if (resumeToken) {
        session.state = CanonicalAudioUploadSessionState.RESUMING
        diagnostics.push(this.candidateDiagnostics(candidate,
          CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_RESUME_STARTED,
          syncRunID, nodeRole, 'status', 'staleSessionStatusRefresh')[0])
        status = await uploadPort.resumeUpload(
          new CanonicalUploadStatusRequest(candidate.objectID, resumeToken.sessionID, source.contentHash), now)
      } else {
        session.state = CanonicalAudioUploadSessionState.STARTING
        diagnostics.push(this.candidateDiagnostics(candidate,
          CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_STARTED,
          syncRunID, nodeRole, 'start',
          'existingSecureRoute:/upload-recording-audio-session/start')[0])
        status = await uploadPort.startResumableUpload(new CanonicalUploadStartRequest(
          candidate.objectID, source.targetReference, source.byteSize, source.contentHash, chunkSize,
          'audio-start:' + candidate.objectID + ':' + source.contentHash.value.substring(0, 12)), now)
        createdJob = true
        startedTransport = true
      }

      let sessionID = status.sessionID ?? session.sessionID
      if (!sessionID) {
        throw new Error('missingSessionID')
      }
      if (status.confirmedBytes < session.confirmedBytes) {
        throw new Error('confirmedBytesRegressed: previous=' + session.confirmedBytes + ', actual=' +
          status.confirmedBytes)
      }
      session.markStarted(sessionID, status.confirmedBytes)
      existingRecord = await jobStore.recordProgress(candidate.objectID, sessionID, session.offset, chunkSize,
        source.contentHash, source.byteSize, session.state, now)

      while (session.confirmedBytes < source.byteSize) {
        const remaining = source.byteSize - session.confirmedBytes
        const readLength = Math.min(chunkSize, remaining)
        const data = await source.readChunk(session.offset, readLength)
        if (data.length === 0) {
          throw new Error('chunkReadReturnedEmpty: ' + session.offset.value)
        }
        const chunkHash = CanonicalTransportEnvelope.hash(data)
        const runtimeChunk = new CanonicalAudioUploadChunk(candidate.objectID, sessionID, session.offset, data.length,
          chunkHash)
        diagnostics.push(this.candidateDiagnostics(candidate,
          CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_CHUNK_SENT,
          syncRunID, nodeRole, 'offset:' + runtimeChunk.offset.value,
          'existingSecureRoute:/upload-recording-audio-session/chunk')[0])
        const chunkStatus = await uploadPort.uploadChunk(new CanonicalUploadChunk(
          candidate.objectID, sessionID, session.offset.value, data, chunkHash, source.contentHash,
          runtimeChunk.idempotencyKey), now)
        startedTransport = true
        sentChunkCount++
        session.confirm(runtimeChunk, chunkStatus.confirmedBytes)
        diagnostics.push(this.candidateDiagnostics(candidate,
          CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_CHUNK_CONFIRMED,
          syncRunID, nodeRole, 'confirmed:' + session.confirmedBytes, 'monotonicConfirmedBytes')[0])
        existingRecord = await jobStore.recordProgress(candidate.objectID, sessionID, session.offset, chunkSize,
          source.contentHash, source.byteSize, CanonicalAudioUploadSessionState.CHUNKING, now)
      }

      session.state = CanonicalAudioUploadSessionState.FINALIZING
      diagnostics.push(this.candidateDiagnostics(candidate,
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_FINALIZE_STARTED,
        syncRunID, nodeRole, 'finalize',
        'existingSecureRoute:/upload-recording-audio-session/finalize')[0])
      await jobStore.recordProgress(candidate.objectID, sessionID!, session.offset, chunkSize,
        source.contentHash, source.byteSize, CanonicalAudioUploadSessionState.FINALIZING, now)
      const finalize = await uploadPort.finalizeUpload(
        new CanonicalUploadFinalizeRequest(candidate.objectID, sessionID!, source.byteSize, source.contentHash), now)
      const actualFileSize = finalize.fileSize ?? -1
      if (!finalize.completed || actualFileSize !== source.byteSize) {
        throw new Error('finalByteSizeMismatch: expected=' + source.byteSize + ', actual=' + actualFileSize)
      }
      const actualChecksum = finalize.checksum
      if (!actualChecksum || actualChecksum.value !== source.contentHash.value) {
        throw new Error('finalHashMismatch: expected=' + hashPrefix(source.contentHash.value) + ', actual=' +
          (actualChecksum ? hashPrefix(actualChecksum.value) : 'null'))
      }
      const proof = new CanonicalAudioUploadFinalizeProof(candidate.objectID, sessionID!, source.byteSize,
        source.contentHash, true, true, true, finalize.completed)
      session.markFinalized(proof)
      await uploadPort.writeUploadLedger(new CanonicalProductionUploadLedgerSnapshot(candidate.objectID,
        sessionID!, source.byteSize, source.byteSize, source.contentHash,
        CanonicalAudioUploadLedgerPhase.COMPLETED))
      const finalizedRecord = await jobStore.recordProgress(candidate.objectID, sessionID!,
        new CanonicalAudioUploadOffset(source.byteSize), chunkSize, source.contentHash, source.byteSize,
        CanonicalAudioUploadSessionState.FINALIZED, now)
      diagnostics.push(this.candidateDiagnostics(candidate,
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_FINALIZE_COMPLETED,
        syncRunID, nodeRole, 'verified', 'hashAndByteSizeVerified')[0])
      return new CanonicalAudioUploadRuntimeResult(configuration.mode,
        CanonicalAudioUploadRuntimeOutcome.UPLOADED, candidate.objectID, sessionID!, createdJob,
        startedTransport, sentChunkCount, source.byteSize, true, false, null, proof, finalizedRecord, diagnostics)
    } catch (error) {
      const sessionID = (await jobStore.recordFor(candidate.objectID))?.sessionID ?? null
      const offset = (await jobStore.recordFor(candidate.objectID))?.offset ?? new CanonicalAudioUploadOffset()
      const code = error instanceof Error ? error.message : 'uploadRuntimeError'
      const isConflict = code.includes('conflict') || code.includes('mismatch') || code.includes('different')
      diagnostics.push(this.candidateDiagnostics(candidate,
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_FINALIZE_FAILED,
        syncRunID, nodeRole, 'failed', code)[0])
      if (isConflict) {
        const conflict = await jobStore.markConflict(candidate.objectID, sessionID, offset, chunkSize,
          source.contentHash, source.byteSize, code, now)
        diagnostics.push(this.candidateDiagnostics(candidate,
          CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_CONFLICT_BLOCKED,
          syncRunID, nodeRole, 'blocked', code)[0])
        return new CanonicalAudioUploadRuntimeResult(configuration.mode,
          CanonicalAudioUploadRuntimeOutcome.CONFLICT, candidate.objectID, sessionID, createdJob,
          startedTransport, sentChunkCount, offset.value, false, false, null, null, conflict, diagnostics)
      }
      const retry = await jobStore.scheduleRetry(candidate.objectID, sessionID, offset, chunkSize,
        source.contentHash, source.byteSize, policy.retryPolicy, code, now)
      diagnostics.push(this.candidateDiagnostics(candidate,
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_RETRY_SCHEDULED,
        syncRunID, nodeRole, retry?.state ?? 'failed', code)[0])
      if (policy.legacyFallbackEnabled) {
        diagnostics.push(this.legacyFallbackDiagnostic(candidate, syncRunID, nodeRole, code))
      }
      return new CanonicalAudioUploadRuntimeResult(configuration.mode,
        retry?.state === CanonicalAudioUploadSessionState.FAILED ?
          CanonicalAudioUploadRuntimeOutcome.FAILED : CanonicalAudioUploadRuntimeOutcome.RETRY_SCHEDULED,
        candidate.objectID, sessionID, createdJob, startedTransport, sentChunkCount,
        retry?.offset.value ?? offset.value, false, policy.legacyFallbackEnabled,
        policy.legacyFallbackEnabled ? code : null, null, retry, diagnostics)
    }
  }

  private async candidateDecision(
    candidate: CanonicalAudioUploadCutoverCandidate, jobStore: CanonicalAudioUploadJobStore,
    policy: CanonicalAudioUploadRuntimePolicy, now: Date
  ): Promise<{ action: string, diagnostics: CanonicalAudioUploadDiagnosticKind[], reason?: string }> {
    let diagnostics: CanonicalAudioUploadDiagnosticKind[] = []

    if (isViewRefresh(candidate.trigger)) {
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_VIEW_REFRESH_SUPPRESSED)
      return { action: 'blocked', diagnostics, reason: 'viewRefreshNeverCreatesAudioUploadCandidate' }
    }
    if (isRetryDrainer(candidate.trigger)) {
      const storeHasRetry = await jobStore.hasEligibleRetry(candidate.objectID, now)
      if (policy.retryDrainerRequiresExistingRetry && !candidate.retryTruth.hasExistingEligibleRetry &&
        !storeHasRetry) {
        diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RETRY_DRAINER_FRESH_JOB_SUPPRESSED)
        return { action: 'blocked', diagnostics, reason: 'retryDrainerCannotCreateFreshAudioUploadJob' }
      }
    }
    if (candidate.evidenceBlockers.includes(
      CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_COMPLETED_LEDGER_REJECTED_AS_NO_OP as unknown as
      import('./CanonicalAudioUploadCutover').CanonicalAudioUploadEvidenceBlocker)) {
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_COMPLETED_LEDGER_REJECTED_AS_NO_OP)
    }
    if (candidate.actionKind === CanonicalAudioUploadActionKind.AUDIO_UPLOAD_NO_OP) {
      return { action: 'noOp', diagnostics }
    }
    if (candidate.actionKind === CanonicalAudioUploadActionKind.AUDIO_UPLOAD_DEFERRED_PEER_UNKNOWN) {
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_PEER_UNKNOWN_DEFERRED)
      return { action: 'deferred', diagnostics }
    }
    if (candidate.actionKind === CanonicalAudioUploadActionKind.AUDIO_UPLOAD_CONFLICT_RECORD ||
      candidate.evidenceStatus === 'conflict') {
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_EXISTING_DIFFERENT_AUDIO_BLOCKED)
      diagnostics.push(CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_CONFLICT_BLOCKED)
      return { action: 'conflict', diagnostics }
    }
    if (candidate.evidenceStatus === 'blocked' && candidate.evidenceBlockers.length > 0) {
      return { action: 'blocked', diagnostics, reason: candidate.evidenceBlockers.join(',') }
    }
    if (candidate.actionKind !== CanonicalAudioUploadActionKind.AUDIO_UPLOAD_CANARY_CANDIDATE ||
      candidate.evidenceStatus !== 'complete' || !candidate.localTruth.sufficientForUploadCandidate) {
      return { action: 'blocked', diagnostics, reason: candidate.reason }
    }
    return { action: 'upload', diagnostics }
  }

  private findModeBlocker(mode: CanonicalAudioUploadRuntimeMode,
    policy: CanonicalAudioUploadRuntimePolicy, uploadPort: CanonicalProductionUploadPort): string | null {
    if (policy.requireExistingSecureUploadRoutes && !uploadPort.resumableSessionSupported) {
      return 'existingSecureUploadRoutesRequired'
    }
    switch (mode) {
      case CanonicalAudioUploadRuntimeMode.TEST_TRANSPORT_UPLOAD:
        return policy.allowTestTransportUpload ? null : 'testTransportUploadNotAllowed'
      case CanonicalAudioUploadRuntimeMode.CANONICAL_UPLOAD_WITH_LEGACY_FALLBACK:
        if (!policy.debugInternalBuild) {
          return 'canonicalUploadRequiresDebugInternalBuild'
        }
        if (!policy.ownerApprovedCanonicalCommit) {
          return 'canonicalUploadOwnerApprovalMissing'
        }
        if (!policy.allowCanonicalUploadWithLegacyFallback) {
          return 'canonicalUploadPolicyDisabled'
        }
        if (uploadPort.isDryRunOnly) {
          return 'canonicalUploadRequiresRealSecureUploadPort'
        }
        return null
      default:
        return null
    }
  }

  private shouldUseLegacyFallback(mode: CanonicalAudioUploadRuntimeMode,
    policy: CanonicalAudioUploadRuntimePolicy, reason: string): boolean {
    if (!policy.legacyFallbackEnabled) {
      return false
    }
    if (mode === CanonicalAudioUploadRuntimeMode.CANONICAL_UPLOAD_WITH_LEGACY_FALLBACK) {
      return true
    }
    if (reason.includes('manualUploadButton')) {
      return true
    }
    return mode === CanonicalAudioUploadRuntimeMode.DISABLED
  }

  private blockedResult(mode: CanonicalAudioUploadRuntimeMode,
    candidate: CanonicalAudioUploadCutoverCandidate, diagnostics: CanonicalAudioUploadDiagnostic[],
    reason: string): CanonicalAudioUploadRuntimeResult {
    return new CanonicalAudioUploadRuntimeResult(mode, CanonicalAudioUploadRuntimeOutcome.BLOCKED,
      candidate.objectID, null, false, false, 0, 0, false, false, null, null, null,
      [...diagnostics, ...this.candidateDiagnostics(candidate,
        CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_CONFLICT_BLOCKED,
        null, CanonicalAudioUploadNodeRole.I_PHONE, 'blocked', reason)])
  }

  private legacyFallbackDiagnostic(candidate: CanonicalAudioUploadCutoverCandidate, syncRunID: string | null,
    nodeRole: CanonicalAudioUploadNodeRole, reason: string): CanonicalAudioUploadDiagnostic {
    return new CanonicalAudioUploadDiagnostic(
      CanonicalAudioUploadDiagnosticKind.CANONICAL_AUDIO_UPLOAD_RUNTIME_LEGACY_FALLBACK_USED,
      syncRunID, candidate.trigger, nodeRole, CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD,
      candidate.objectID, candidate.peerTruth.state, candidate.ledgerTruth.phase, candidate.actionKind,
      'legacyFallback', reason, candidate.hashPrefix)
  }

  private candidateDiagnostics(candidate: CanonicalAudioUploadCutoverCandidate,
    kind: CanonicalAudioUploadDiagnosticKind, syncRunID: string | null = null,
    nodeRole: CanonicalAudioUploadNodeRole = CanonicalAudioUploadNodeRole.I_PHONE,
    result: string | null = null, reason: string | null = null): CanonicalAudioUploadDiagnostic[] {
    return [new CanonicalAudioUploadDiagnostic(kind, syncRunID, candidate.trigger, nodeRole,
      CanonicalAudioUploadCutoverDomain.AUDIO_UPLOAD, candidate.objectID, candidate.peerTruth.state,
      candidate.ledgerTruth.phase, candidate.actionKind, result, reason, candidate.hashPrefix)]
  }
}

export class CanonicalAudioUploadRuntimeOwner {
  executor: CanonicalAudioUploadRuntimeExecutor
  jobStore: CanonicalAudioUploadJobStore
  nodeRole: CanonicalAudioUploadNodeRole

  constructor(
    executor: CanonicalAudioUploadRuntimeExecutor = new CanonicalAudioUploadRuntimeExecutor(),
    jobStore: CanonicalAudioUploadJobStore = new CanonicalAudioUploadJobStore(),
    nodeRole: CanonicalAudioUploadNodeRole = CanonicalAudioUploadNodeRole.I_PHONE
  ) {
    this.executor = executor
    this.jobStore = jobStore
    this.nodeRole = nodeRole
  }

  async execute(
    candidate: CanonicalAudioUploadCutoverCandidate, source: CanonicalAudioUploadByteSource,
    uploadPort: CanonicalProductionUploadPort, configuration: CanonicalAudioUploadRuntimeConfiguration,
    syncRunID: string | null = null
  ): Promise<CanonicalAudioUploadRuntimeResult> {
    return this.executor.execute(candidate, source, uploadPort, this.jobStore, configuration, syncRunID, this.nodeRole)
  }
}

// ── Utility functions ──

function safeIdentifier(value: string, fallback: string): string {
  const trimmed = value.trim()
  if (trimmed.length === 0) {
    return fallback
  }
  const filtered = trimmed.replace(/[^a-zA-Z0-9\-_:.]/g, '-')
  return filtered.substring(0, 96) || fallback
}

function safeText(value: string | null): string | null {
  if (!value) {
    return null
  }
  const sanitized = value.replace(/\n/g, ' ').replace(/\r/g, ' ').trim()
  if (sanitized.length === 0) {
    return null
  }
  return sanitized.substring(0, 160)
}

export function hashPrefixFor(value: string | null): string | null {
  if (!value || value.trim().length === 0) {
    return null
  }
  return value.trim().substring(0, 12)
}
