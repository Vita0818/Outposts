/**
 * CanonicalTransferStateMachine — transfer job lifecycle & projection.
 * Ported from Rokurics-Apple (CanonicalTransferStateMachine.swift).
 * Maps legacy transfer states to canonical phases and builds transfer projections.
 */

import {
  CanonicalTimestamp, nilIfEmpty
} from './CanonicalCore'

// ── Canonical Transfer Job ID ──

export class CanonicalTransferJobID {
  rawValue: string

  constructor(rawValue: string) {
    this.rawValue = nilIfEmpty(rawValue.trim()) ?? 'transfer:unknown'
  }
}

// ── Canonical Transfer Kind ──

export enum CanonicalTransferKind {
  RECORDING_AUDIO_UPLOAD = 'recordingAudioUpload',
  GENERATED_ARTIFACT_DOWNLOAD = 'generatedArtifactDownload',
  METADATA_SEND = 'metadataSend',
  METADATA_APPLY = 'metadataApply',
  FOLDER_METADATA_SEND = 'folderMetadataSend',
  FOLDER_METADATA_APPLY = 'folderMetadataApply',
  STUDY_ITEM_METADATA_SEND = 'studyItemMetadataSend',
  STUDY_ITEM_METADATA_APPLY = 'studyItemMetadataApply',
  TOMBSTONE_SEND = 'tombstoneSend',
  TOMBSTONE_APPLY = 'tombstoneApply'
}

// ── Canonical Transfer Direction ──

export enum CanonicalTransferDirection {
  LOCAL_TO_PEER = 'localToPeer',
  PEER_TO_LOCAL = 'peerToLocal',
  LOCAL_ONLY = 'localOnly',
  PEER_ONLY = 'peerOnly'
}

// ── Canonical Transfer Phase ──

export enum CanonicalTransferPhase {
  NONE = 'none',
  PLANNED = 'planned',
  QUEUED = 'queued',
  IN_FLIGHT = 'inFlight',
  FINALIZING = 'finalizing',
  COMPLETED = 'completed',
  FAILED_RETRYABLE = 'failedRetryable',
  FAILED_FATAL = 'failedFatal',
  CONFLICT = 'conflict',
  DEFERRED = 'deferred',
  UNSUPPORTED = 'unsupported'
}

// ── Canonical Transfer Failure ──

export class CanonicalTransferFailure {
  code: string
  retryable: boolean
  detail: string | null

  constructor(code: string, retryable: boolean, detail: string | null = null) {
    this.code = nilIfEmpty(code.trim()) ?? 'unknown'
    this.retryable = retryable
    this.detail = nilIfEmpty(detail?.trim() ?? '') ?? null
  }
}

// ── Canonical Retry Policy Snapshot ──

export class CanonicalRetryPolicySnapshot {
  retryCount: number
  nextRetryAt: CanonicalTimestamp | null
  maxAttempts: number | null

  constructor(retryCount: number, nextRetryAt: CanonicalTimestamp | null = null, maxAttempts: number | null = null) {
    this.retryCount = retryCount
    this.nextRetryAt = nextRetryAt
    this.maxAttempts = maxAttempts
  }
}

// ── Canonical Transfer Job ──

export class CanonicalTransferJob {
  jobID: CanonicalTransferJobID
  objectID: string
  artifactID: string | null
  kind: CanonicalTransferKind
  direction: CanonicalTransferDirection
  phase: CanonicalTransferPhase
  failure: CanonicalTransferFailure | null
  retryPolicy: CanonicalRetryPolicySnapshot | null
  source: string | null

  constructor(
    jobID: CanonicalTransferJobID,
    objectID: string,
    artifactID: string | null = null,
    kind: CanonicalTransferKind,
    direction: CanonicalTransferDirection,
    phase: CanonicalTransferPhase,
    failure: CanonicalTransferFailure | null = null,
    retryPolicy: CanonicalRetryPolicySnapshot | null = null,
    source: string | null = null
  ) {
    this.jobID = jobID
    this.objectID = nilIfEmpty(objectID.trim()) ?? 'unknown'
    this.artifactID = nilIfEmpty(artifactID?.trim() ?? '') ?? null
    this.kind = kind
    this.direction = direction
    this.phase = phase
    this.failure = failure
    this.retryPolicy = retryPolicy
    this.source = nilIfEmpty(source?.trim() ?? '') ?? null
  }

  get id(): string {
    return this.jobID.rawValue
  }
}

// ── Canonical Ledger Projection ──

export class CanonicalLedgerProjection {
  source: string
  state: string
  phase: CanonicalTransferPhase
  nextRetryAt: CanonicalTimestamp | null
  failure: CanonicalTransferFailure | null

  constructor(
    source: string,
    state: string,
    phase: CanonicalTransferPhase,
    nextRetryAt: CanonicalTimestamp | null = null,
    failure: CanonicalTransferFailure | null = null
  ) {
    this.source = source
    this.state = state
    this.phase = phase
    this.nextRetryAt = nextRetryAt
    this.failure = failure
  }
}

// ── Canonical Transfer Projection ──

export class CanonicalTransferProjection {
  jobs: CanonicalTransferJob[]
  ledgers: CanonicalLedgerProjection[]

  constructor(jobs: CanonicalTransferJob[] = [], ledgers: CanonicalLedgerProjection[] = []) {
    this.jobs = [...jobs].sort((a, b) => a.jobID.rawValue.localeCompare(b.jobID.rawValue))
    this.ledgers = ledgers
  }
}

// ── Canonical Transfer State Machine (namespace functions) ──

export function transferPhaseFromLegacyState(state: string | null): CanonicalTransferPhase {
  switch (state?.trim() ?? '') {
    case 'planned':
      return CanonicalTransferPhase.PLANNED
    case 'queued':
    case 'pending':
      return CanonicalTransferPhase.QUEUED
    case 'inFlight':
    case 'uploading':
    case 'downloading':
    case 'transferring':
    case 'resuming':
      return CanonicalTransferPhase.IN_FLIGHT
    case 'finalizing':
    case 'verifying':
      return CanonicalTransferPhase.FINALIZING
    case 'completed':
    case 'complete':
    case 'uploaded':
      return CanonicalTransferPhase.COMPLETED
    case 'retryPending':
    case 'retryableFailed':
      return CanonicalTransferPhase.FAILED_RETRYABLE
    case 'fatalFailed':
      return CanonicalTransferPhase.FAILED_FATAL
    case 'failed':
      return CanonicalTransferPhase.FAILED_FATAL
    case 'conflict':
      return CanonicalTransferPhase.CONFLICT
    case 'deferred':
      return CanonicalTransferPhase.DEFERRED
    case 'unsupported':
      return CanonicalTransferPhase.UNSUPPORTED
    default:
      return CanonicalTransferPhase.NONE
  }
}

export function makeTransferJob(
  objectID: string,
  artifactID: string | null = null,
  kind: CanonicalTransferKind,
  direction: CanonicalTransferDirection,
  legacyState: string | null,
  nextRetryAt: Date | null = null,
  failureCode: string | null = null,
  source: string
): CanonicalTransferJob {
  const phase = transferPhaseFromLegacyState(legacyState)
  const retry = phase === CanonicalTransferPhase.FAILED_RETRYABLE
    ? new CanonicalRetryPolicySnapshot(0, nextRetryAt ? new CanonicalTimestamp(nextRetryAt) : null, null)
    : null
  const failure = failureCode
    ? new CanonicalTransferFailure(failureCode, phase === CanonicalTransferPhase.FAILED_RETRYABLE)
    : null
  const jobID = new CanonicalTransferJobID(
    [source, kind, objectID, artifactID ?? ''].join('|')
  )
  return new CanonicalTransferJob(
    jobID,
    objectID,
    artifactID,
    kind,
    direction,
    phase,
    failure,
    retry,
    source
  )
}

export function transferProjectionFromJobs(jobs: CanonicalTransferJob[]): CanonicalTransferProjection {
  return new CanonicalTransferProjection(jobs)
}
