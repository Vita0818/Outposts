/**
 * CanonicalUploadRuntime — canonical resumable upload runtime.
 * Ported from Rokurics-Apple (CanonicalUploadRuntime.swift).
 */

import {
  CanonicalHash, CanonicalTimestamp, nilIfEmpty, timestampString, bufferToHex
} from './CanonicalCore'
import {
  CanonicalRootToken, CanonicalFileReference, CanonicalFilePurpose,
  CanonicalFileWriteIntent, CanonicalFileReadRequest,
  CanonicalFileHashPolicy, CanonicalFileConflictPolicy,
  CanonicalFileWriteDisposition, CanonicalFileStorePort,
  InMemoryCanonicalFileStore, CanonicalFileRuntimeError
} from './CanonicalFileRuntime'

// =============================================================================
// UPLOAD SESSION ID
// =============================================================================

export class CanonicalUploadSessionID {
  rawValue: string

  constructor(rawValue: string) {
    this.rawValue = (nilIfEmpty(rawValue.trim()) ?? 'upload-session:unknown')
  }
}

// =============================================================================
// UPLOAD ENUMS
// =============================================================================

export enum CanonicalUploadDisposition {
  ACCEPTED_NEW = 'acceptedNew',
  ACCEPTED_EXISTING = 'acceptedExisting',
  RESUMED = 'resumed',
  FINALIZED = 'finalized'
}

export enum CanonicalUploadSessionPhase {
  ACTIVE = 'active',
  RETRY_PENDING = 'retryPending',
  FINALIZING = 'finalizing',
  COMPLETED = 'completed',
  CONFLICT = 'conflict',
  FAILED = 'failed'
}

// =============================================================================
// UPLOAD RETRY POLICY
// =============================================================================

export class CanonicalUploadRetryPolicy {
  maxAttempts: number
  retryDelaySeconds: number

  constructor(maxAttempts: number = 3, retryDelaySeconds: number = 5) {
    this.maxAttempts = Math.max(1, maxAttempts)
    this.retryDelaySeconds = Math.max(0, retryDelaySeconds)
  }
}

export class CanonicalRetryPolicySnapshot {
  retryCount: number
  nextRetryAt: CanonicalTimestamp | null
  maxAttempts: number

  constructor(retryCount: number, nextRetryAt: CanonicalTimestamp | null, maxAttempts: number) {
    this.retryCount = retryCount
    this.nextRetryAt = nextRetryAt
    this.maxAttempts = maxAttempts
  }
}

// =============================================================================
// UPLOAD CHUNK RECORD
// =============================================================================

export class CanonicalUploadChunkRecord {
  offset: number
  length: number
  sha256: CanonicalHash

  constructor(offset: number, length: number, sha256: CanonicalHash) {
    this.offset = offset
    this.length = length
    this.sha256 = sha256
  }
}

// =============================================================================
// UPLOAD SESSION
// =============================================================================

export class CanonicalUploadSession {
  sessionID: CanonicalUploadSessionID
  objectID: string
  targetReference: CanonicalFileReference
  totalBytes: number
  totalHash: CanonicalHash
  chunkSize: number
  confirmedBytes: number
  chunks: CanonicalUploadChunkRecord[]
  phase: CanonicalUploadSessionPhase
  retryCount: number
  nextRetryAt: CanonicalTimestamp | null
  createdAt: CanonicalTimestamp
  updatedAt: CanonicalTimestamp
  finalizedAt: CanonicalTimestamp | null
  lastError: string | null

  constructor(
    sessionID: CanonicalUploadSessionID,
    objectID: string,
    targetReference: CanonicalFileReference,
    totalBytes: number,
    totalHash: CanonicalHash,
    chunkSize: number,
    confirmedBytes: number,
    chunks: CanonicalUploadChunkRecord[],
    phase: CanonicalUploadSessionPhase,
    retryCount: number,
    nextRetryAt: CanonicalTimestamp | null,
    createdAt: CanonicalTimestamp,
    updatedAt: CanonicalTimestamp,
    finalizedAt: CanonicalTimestamp | null,
    lastError: string | null
  ) {
    this.sessionID = sessionID
    this.objectID = objectID
    this.targetReference = targetReference
    this.totalBytes = totalBytes
    this.totalHash = totalHash
    this.chunkSize = chunkSize
    this.confirmedBytes = confirmedBytes
    this.chunks = chunks
    this.phase = phase
    this.retryCount = retryCount
    this.nextRetryAt = nextRetryAt
    this.createdAt = createdAt
    this.updatedAt = updatedAt
    this.finalizedAt = finalizedAt
    this.lastError = lastError
  }
}

// =============================================================================
// UPLOAD REQUESTS
// =============================================================================

export class CanonicalUploadStartRequest {
  objectID: string
  targetReference: CanonicalFileReference
  totalBytes: number
  totalHash: CanonicalHash
  chunkSize: number
  idempotencyKey: string | null

  constructor(
    objectID: string,
    targetReference: CanonicalFileReference,
    totalBytes: number,
    totalHash: CanonicalHash,
    chunkSize: number,
    idempotencyKey: string | null = null
  ) {
    this.objectID = (nilIfEmpty(objectID.trim()) ?? 'unknown-recording')
    this.targetReference = targetReference
    this.totalBytes = totalBytes
    this.totalHash = totalHash
    this.chunkSize = chunkSize
    this.idempotencyKey = idempotencyKey ? nilIfEmpty(idempotencyKey.trim()) : null
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

export class CanonicalUploadChunk {
  objectID: string
  sessionID: CanonicalUploadSessionID
  offset: number
  bytes: Uint8Array
  chunkHash: CanonicalHash
  totalHash: CanonicalHash
  idempotencyKey: string | null

  constructor(
    objectID: string,
    sessionID: CanonicalUploadSessionID,
    offset: number,
    bytes: Uint8Array,
    chunkHash: CanonicalHash,
    totalHash: CanonicalHash,
    idempotencyKey: string | null = null
  ) {
    this.objectID = (nilIfEmpty(objectID.trim()) ?? 'unknown-recording')
    this.sessionID = sessionID
    this.offset = offset
    this.bytes = bytes
    this.chunkHash = chunkHash
    this.totalHash = totalHash
    this.idempotencyKey = idempotencyKey ? nilIfEmpty(idempotencyKey.trim()) : null
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

// =============================================================================
// UPLOAD STATUS & RESUME
// =============================================================================

export class CanonicalUploadSessionStatus {
  ok: boolean
  disposition: CanonicalUploadDisposition | null
  phase: CanonicalUploadSessionPhase
  sessionID: CanonicalUploadSessionID | null
  confirmedBytes: number
  nextOffset: number
  chunkSize: number | null
  completed: boolean
  finalFile: CanonicalFileReference | null
  checksum: CanonicalHash | null
  fileSize: number | null
  retry: CanonicalRetryPolicySnapshot | null
  error: string | null

  constructor(
    ok: boolean,
    disposition: CanonicalUploadDisposition | null,
    phase: CanonicalUploadSessionPhase,
    sessionID: CanonicalUploadSessionID | null,
    confirmedBytes: number,
    nextOffset: number,
    chunkSize: number | null,
    completed: boolean,
    finalFile: CanonicalFileReference | null,
    checksum: CanonicalHash | null,
    fileSize: number | null,
    retry: CanonicalRetryPolicySnapshot | null,
    error: string | null
  ) {
    this.ok = ok
    this.disposition = disposition
    this.phase = phase
    this.sessionID = sessionID
    this.confirmedBytes = confirmedBytes
    this.nextOffset = nextOffset
    this.chunkSize = chunkSize
    this.completed = completed
    this.finalFile = finalFile
    this.checksum = checksum
    this.fileSize = fileSize
    this.retry = retry
    this.error = error
  }
}

export class CanonicalUploadResumeState {
  sessionID: CanonicalUploadSessionID
  confirmedBytes: number
  nextOffset: number
  totalBytes: number
  totalHash: CanonicalHash
  chunkSize: number
  phase: CanonicalUploadSessionPhase

  constructor(
    sessionID: CanonicalUploadSessionID,
    confirmedBytes: number,
    nextOffset: number,
    totalBytes: number,
    totalHash: CanonicalHash,
    chunkSize: number,
    phase: CanonicalUploadSessionPhase
  ) {
    this.sessionID = sessionID
    this.confirmedBytes = confirmedBytes
    this.nextOffset = nextOffset
    this.totalBytes = totalBytes
    this.totalHash = totalHash
    this.chunkSize = chunkSize
    this.phase = phase
  }
}

// =============================================================================
// UPLOAD RUNTIME ERROR
// =============================================================================

export enum CanonicalUploadRuntimeError {
  INVALID_REQUEST = 'invalidRequest',
  INVALID_SESSION = 'invalidSession',
  SESSION_MISSING = 'sessionMissing',
  SESSION_CONFLICT = 'sessionConflict',
  CHUNK_OFFSET_MISMATCH = 'chunkOffsetMismatch',
  CHUNK_HASH_MISMATCH = 'chunkHashMismatch',
  SESSION_INCOMPLETE = 'sessionIncomplete',
  FINAL_HASH_MISMATCH = 'finalHashMismatch',
  TARGET_CONFLICT = 'targetConflict',
  RETRY_LIMIT_EXCEEDED = 'retryLimitExceeded'
}

// =============================================================================
// UPLOAD RUNTIME PORT INTERFACE
// =============================================================================

export interface CanonicalUploadRuntimePort {
  start(request: CanonicalUploadStartRequest, now: Date): CanonicalUploadSessionStatus
  status(request: CanonicalUploadStatusRequest, now: Date): CanonicalUploadSessionStatus
  append(chunk: CanonicalUploadChunk, now: Date): CanonicalUploadSessionStatus
  finalize(request: CanonicalUploadFinalizeRequest, now: Date): CanonicalUploadSessionStatus
}

// =============================================================================
// RESUEMABLE UPLOAD RUNTIME
// =============================================================================

interface SessionState {
  session: CanonicalUploadSession
  buffer: Uint8Array
  idempotencyResponses: Map<string, CanonicalUploadSessionStatus>
}

export class CanonicalResumableUploadRuntime implements CanonicalUploadRuntimePort {
  private fileStore: CanonicalFileStorePort
  private retryPolicy: CanonicalUploadRetryPolicy
  private sessions: Map<string, SessionState> = new Map()

  constructor(fileStore: CanonicalFileStorePort, retryPolicy: CanonicalUploadRetryPolicy = new CanonicalUploadRetryPolicy()) {
    this.fileStore = fileStore
    this.retryPolicy = retryPolicy
  }

  start(request: CanonicalUploadStartRequest, now: Date = new Date()): CanonicalUploadSessionStatus {
    CanonicalResumableUploadRuntime.validateStart(request)
    const completed = this.completedStatusIfPresent(request)
    if (completed) {
      return completed
    }

    const sessionID = CanonicalResumableUploadRuntime.makeSessionID(request)
    const existing = this.sessions.get(sessionID.rawValue)
    if (existing) {
      if (existing.session.objectID !== request.objectID ||
        !CanonicalResumableUploadRuntime.sameHash(existing.session.totalHash, request.totalHash) ||
        existing.session.totalBytes !== request.totalBytes ||
        existing.session.chunkSize !== request.chunkSize) {
        throw CanonicalUploadRuntimeError.SESSION_CONFLICT
      }
      return CanonicalResumableUploadRuntime.statusFor(existing.session, CanonicalUploadDisposition.ACCEPTED_EXISTING, this.retryPolicy)
    }

    const session = new CanonicalUploadSession(
      sessionID, request.objectID, request.targetReference,
      request.totalBytes, request.totalHash, request.chunkSize,
      0, [], CanonicalUploadSessionPhase.ACTIVE, 0, null,
      new CanonicalTimestamp(now), new CanonicalTimestamp(now), null, null
    )
    this.sessions.set(sessionID.rawValue, {
      session: session,
      buffer: new Uint8Array(0),
      idempotencyResponses: new Map()
    })
    return CanonicalResumableUploadRuntime.statusFor(session, CanonicalUploadDisposition.ACCEPTED_NEW, this.retryPolicy)
  }

  status(request: CanonicalUploadStatusRequest, now: Date = new Date()): CanonicalUploadSessionStatus {
    const state = this.sessions.get(request.sessionID.rawValue)
    if (!state) {
      throw CanonicalUploadRuntimeError.SESSION_MISSING
    }
    if (state.session.objectID !== request.objectID ||
      !CanonicalResumableUploadRuntime.sameHash(state.session.totalHash, request.totalHash)) {
      throw CanonicalUploadRuntimeError.SESSION_CONFLICT
    }
    return CanonicalResumableUploadRuntime.statusFor(state.session, CanonicalUploadDisposition.RESUMED, this.retryPolicy)
  }

  append(chunk: CanonicalUploadChunk, now: Date = new Date()): CanonicalUploadSessionStatus {
    const state = this.sessions.get(chunk.sessionID.rawValue)
    if (!state) {
      throw CanonicalUploadRuntimeError.SESSION_MISSING
    }
    if (chunk.idempotencyKey) {
      const existing = state.idempotencyResponses.get(chunk.idempotencyKey)
      if (existing) {
        return existing
      }
    }
    if (state.session.phase !== CanonicalUploadSessionPhase.ACTIVE &&
      state.session.phase !== CanonicalUploadSessionPhase.RETRY_PENDING) {
      throw CanonicalUploadRuntimeError.SESSION_CONFLICT
    }
    if (state.session.objectID !== chunk.objectID ||
      !CanonicalResumableUploadRuntime.sameHash(state.session.totalHash, chunk.totalHash)) {
      throw CanonicalUploadRuntimeError.SESSION_CONFLICT
    }
    const actualChunkHash = InMemoryCanonicalFileStore.hash(chunk.bytes, CanonicalFileHashPolicy.SHA256) ?? new CanonicalHash('')
    if (!CanonicalResumableUploadRuntime.sameHash(actualChunkHash, chunk.chunkHash)) {
      state.session.phase = CanonicalUploadSessionPhase.CONFLICT
      state.session.lastError = 'chunkHashMismatch'
      this.sessions.set(chunk.sessionID.rawValue, state)
      throw CanonicalUploadRuntimeError.CHUNK_HASH_MISMATCH
    }

    const existingChunk = state.session.chunks.find(c => c.offset === chunk.offset)
    if (existingChunk) {
      if (existingChunk.length !== chunk.bytes.length ||
        !CanonicalResumableUploadRuntime.sameHash(existingChunk.sha256, chunk.chunkHash)) {
        state.session.phase = CanonicalUploadSessionPhase.CONFLICT
        state.session.lastError = 'chunkConflict'
        this.sessions.set(chunk.sessionID.rawValue, state)
        throw CanonicalUploadRuntimeError.SESSION_CONFLICT
      }
      const response = CanonicalResumableUploadRuntime.statusFor(
        state.session, CanonicalUploadDisposition.ACCEPTED_EXISTING, this.retryPolicy
      )
      if (chunk.idempotencyKey) {
        state.idempotencyResponses.set(chunk.idempotencyKey, response)
        this.sessions.set(chunk.sessionID.rawValue, state)
      }
      return response
    }
    if (chunk.offset !== state.session.confirmedBytes) {
      throw CanonicalUploadRuntimeError.CHUNK_OFFSET_MISMATCH
    }
    if (state.session.confirmedBytes + chunk.bytes.length > state.session.totalBytes) {
      throw CanonicalUploadRuntimeError.SESSION_CONFLICT
    }

    const newBuffer = new Uint8Array(state.buffer.length + chunk.bytes.length)
    newBuffer.set(state.buffer, 0)
    newBuffer.set(chunk.bytes, state.buffer.length)
    state.buffer = newBuffer
    state.session.confirmedBytes += chunk.bytes.length
    state.session.chunks.push(new CanonicalUploadChunkRecord(chunk.offset, chunk.bytes.length, chunk.chunkHash))
    state.session.phase = CanonicalUploadSessionPhase.ACTIVE
    state.session.updatedAt = new CanonicalTimestamp(now)
    state.session.lastError = null

    const response = CanonicalResumableUploadRuntime.statusFor(
      state.session, CanonicalUploadDisposition.ACCEPTED_NEW, this.retryPolicy
    )
    if (chunk.idempotencyKey) {
      state.idempotencyResponses.set(chunk.idempotencyKey, response)
    }
    this.sessions.set(chunk.sessionID.rawValue, state)
    return response
  }

  finalize(request: CanonicalUploadFinalizeRequest, now: Date = new Date()): CanonicalUploadSessionStatus {
    const state = this.sessions.get(request.sessionID.rawValue)
    if (!state) {
      throw CanonicalUploadRuntimeError.SESSION_MISSING
    }
    if (state.session.objectID !== request.objectID ||
      state.session.totalBytes !== request.totalBytes ||
      !CanonicalResumableUploadRuntime.sameHash(state.session.totalHash, request.totalHash)) {
      throw CanonicalUploadRuntimeError.SESSION_CONFLICT
    }
    if (state.session.confirmedBytes !== request.totalBytes) {
      throw CanonicalUploadRuntimeError.SESSION_INCOMPLETE
    }
    const actualHash = InMemoryCanonicalFileStore.hash(state.buffer, CanonicalFileHashPolicy.SHA256) ?? new CanonicalHash('')
    if (!CanonicalResumableUploadRuntime.sameHash(actualHash, request.totalHash)) {
      state.session.phase = CanonicalUploadSessionPhase.CONFLICT
      state.session.lastError = 'finalHashMismatch'
      this.sessions.set(request.sessionID.rawValue, state)
      throw CanonicalUploadRuntimeError.FINAL_HASH_MISMATCH
    }

    state.session.phase = CanonicalUploadSessionPhase.FINALIZING
    this.sessions.set(request.sessionID.rawValue, state)
    try {
      this.fileStore.write(
        new CanonicalFileWriteIntent(
          state.session.targetReference, state.buffer, CanonicalFilePurpose.ARTIFACT_BYTES,
          request.totalHash, request.totalBytes, null,
          undefined, undefined, CanonicalFileConflictPolicy.IDEMPOTENT_IF_SAME_CONTENT
        )
      )
    } catch (_e) {
      state.session.phase = CanonicalUploadSessionPhase.CONFLICT
      state.session.lastError = 'targetConflict'
      this.sessions.set(request.sessionID.rawValue, state)
      throw CanonicalUploadRuntimeError.TARGET_CONFLICT
    }

    state.session.phase = CanonicalUploadSessionPhase.COMPLETED
    state.session.updatedAt = new CanonicalTimestamp(now)
    state.session.finalizedAt = new CanonicalTimestamp(now)
    state.session.lastError = null
    this.sessions.set(request.sessionID.rawValue, state)
    return CanonicalResumableUploadRuntime.statusFor(state.session, CanonicalUploadDisposition.FINALIZED, this.retryPolicy)
  }

  resumeState(sessionID: CanonicalUploadSessionID): CanonicalUploadResumeState {
    const state = this.sessions.get(sessionID.rawValue)
    if (!state) {
      throw CanonicalUploadRuntimeError.SESSION_MISSING
    }
    return new CanonicalUploadResumeState(
      state.session.sessionID, state.session.confirmedBytes, state.session.confirmedBytes,
      state.session.totalBytes, state.session.totalHash, state.session.chunkSize, state.session.phase
    )
  }

  recordRetryableFailure(sessionID: CanonicalUploadSessionID, code: string, now: Date = new Date()): CanonicalUploadSessionStatus {
    const state = this.sessions.get(sessionID.rawValue)
    if (!state) {
      throw CanonicalUploadRuntimeError.SESSION_MISSING
    }
    const nextRetryCount = state.session.retryCount + 1
    if (nextRetryCount > this.retryPolicy.maxAttempts) {
      state.session.phase = CanonicalUploadSessionPhase.FAILED
      state.session.lastError = code
      this.sessions.set(sessionID.rawValue, state)
      throw CanonicalUploadRuntimeError.RETRY_LIMIT_EXCEEDED
    }
    state.session.retryCount = nextRetryCount
    state.session.phase = CanonicalUploadSessionPhase.RETRY_PENDING
    state.session.lastError = (nilIfEmpty(code.trim()) ?? 'retryableFailure')
    state.session.nextRetryAt = new CanonicalTimestamp(new Date(now.getTime() + this.retryPolicy.retryDelaySeconds * 1000))
    state.session.updatedAt = new CanonicalTimestamp(now)
    this.sessions.set(sessionID.rawValue, state)
    return CanonicalResumableUploadRuntime.statusFor(state.session, CanonicalUploadDisposition.RESUMED, this.retryPolicy)
  }

  retryDriveSnapshot(sessionID: CanonicalUploadSessionID): CanonicalRetryPolicySnapshot {
    const state = this.sessions.get(sessionID.rawValue)
    if (!state) {
      throw CanonicalUploadRuntimeError.SESSION_MISSING
    }
    return new CanonicalRetryPolicySnapshot(
      state.session.retryCount, state.session.nextRetryAt, this.retryPolicy.maxAttempts
    )
  }

  private completedStatusIfPresent(request: CanonicalUploadStartRequest): CanonicalUploadSessionStatus | null {
    try {
      const read = this.fileStore.read(new CanonicalFileReadRequest(request.targetReference))
      if (read.byteSize === request.totalBytes && read.contentHash &&
        CanonicalResumableUploadRuntime.sameHash(read.contentHash, request.totalHash)) {
        return new CanonicalUploadSessionStatus(
          true, CanonicalUploadDisposition.ACCEPTED_EXISTING, CanonicalUploadSessionPhase.COMPLETED,
          null, request.totalBytes, request.totalBytes, null, true,
          request.targetReference, read.contentHash, request.totalBytes, null, null
        )
      }
      throw CanonicalUploadRuntimeError.TARGET_CONFLICT
    } catch (_e) {
      return null
    }
  }

  private static validateStart(request: CanonicalUploadStartRequest) {
    if (!(request.totalBytes > 0 && request.chunkSize > 0)) {
      throw CanonicalUploadRuntimeError.INVALID_REQUEST
    }
  }

  static statusFor(
    session: CanonicalUploadSession,
    disposition: CanonicalUploadDisposition | null,
    retryPolicy: CanonicalUploadRetryPolicy
  ): CanonicalUploadSessionStatus {
    return new CanonicalUploadSessionStatus(
      true, disposition, session.phase, session.sessionID,
      session.confirmedBytes, session.confirmedBytes, session.chunkSize,
      session.phase === CanonicalUploadSessionPhase.COMPLETED,
      session.phase === CanonicalUploadSessionPhase.COMPLETED ? session.targetReference : null,
      session.phase === CanonicalUploadSessionPhase.COMPLETED ? session.totalHash : null,
      session.phase === CanonicalUploadSessionPhase.COMPLETED ? session.totalBytes : null,
      session.phase === CanonicalUploadSessionPhase.RETRY_PENDING
        ? new CanonicalRetryPolicySnapshot(session.retryCount, session.nextRetryAt, retryPolicy.maxAttempts)
        : null,
      session.lastError
    )
  }

  private static makeSessionID(request: CanonicalUploadStartRequest): CanonicalUploadSessionID {
    const raw = request.objectID + '|' + request.targetReference.rootToken.rawValue +
      '|' + request.targetReference.logicalPathToken + '|' + request.totalHash.value
    const digest = CanonicalHash.sha256OfString(raw)
    return new CanonicalUploadSessionID(request.objectID + '-' + digest.value.substring(0, 16))
  }

  private static sameHash(left: CanonicalHash, right: CanonicalHash): boolean {
    return left.algorithm === right.algorithm && left.value === right.value
  }
}
