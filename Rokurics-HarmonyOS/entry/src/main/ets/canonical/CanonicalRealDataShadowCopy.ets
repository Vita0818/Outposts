/**
 * CanonicalRealDataShadowCopy — real data shadow copy policy and execution.
 * Ported from Rokurics-Apple (CanonicalRealDataShadowCopy.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalProjectionContract,
  nilIfEmpty, hashPrefix, safeLogicalPathToken
} from './CanonicalCore'
import {
  CanonicalProductionRedaction, CanonicalProductionPortError
} from './CanonicalProductionPorts'
import {
  CanonicalRootToken, CanonicalFileReference,
  InMemoryCanonicalFileStore, CanonicalFileHashPolicy
} from './CanonicalFileRuntime'
import {
  CanonicalShadowMigrationRedaction
} from './CanonicalShadowMigration'

// =============================================================================
// SHADOW ROOT KIND
// =============================================================================

export enum CanonicalShadowRootKind {
  SHADOW_COPY = 'shadowCopy',
  EXECUTION_SHADOW = 'executionShadow',
  READ_ONLY_PROBE = 'readOnlyProbe'
}

// =============================================================================
// SHADOW COPY KINDS
// =============================================================================

export enum CanonicalRealDataShadowCopyKind {
  RECORDING_METADATA = 'recordingMetadata',
  STUDY_METADATA = 'studyMetadata',
  RECEIVE_RECORD = 'receiveRecord',
  GENERATED_ARTIFACT = 'generatedArtifact',
  AUDIO_DESCRIPTOR = 'audioDescriptor',
  AUDIO_BYTES = 'audioBytes'
}

export enum CanonicalRealDataShadowCopyBytesMode {
  INLINE_BYTES = 'inlineBytes',
  FILE_BYTES = 'fileBytes',
  DESCRIPTOR_ONLY = 'descriptorOnly'
}

export enum CanonicalRealDataShadowCopyHashPolicy {
  USE_PROVIDED_HASH = 'useProvidedHash',
  COMPUTE_IF_BOUNDED = 'computeIfBounded',
  HASH_UNAVAILABLE = 'hashUnavailable'
}

export enum CanonicalRealDataShadowCopyCleanupPolicyKind {
  CLEANUP_IMMEDIATELY = 'cleanupImmediately',
  RETAIN_FOR_DIAGNOSTICS = 'retainForDiagnostics',
  CLEANUP_ON_NEXT_LAUNCH = 'cleanupOnNextLaunch'
}

export class CanonicalRealDataShadowCopyCleanupPolicy {
  kind: CanonicalRealDataShadowCopyCleanupPolicyKind
  maxAge: number
  maxBytes: number

  constructor(kind: CanonicalRealDataShadowCopyCleanupPolicyKind, maxAge: number = 0, maxBytes: number = 0) {
    this.kind = kind
    this.maxAge = maxAge
    this.maxBytes = maxBytes
  }

  static readonly CLEANUP_IMMEDIATELY = new CanonicalRealDataShadowCopyCleanupPolicy(
    CanonicalRealDataShadowCopyCleanupPolicyKind.CLEANUP_IMMEDIATELY
  )
}

// =============================================================================
// SHADOW COPY POLICY
// =============================================================================

export class CanonicalRealDataShadowCopyPolicy {
  isEnabled: boolean
  maxMetadataBytes: number
  maxGeneratedArtifactBytes: number
  maxAudioBytes: number
  copyAudioBytesByDefault: boolean
  allowHashComputationForBoundedBytes: boolean
  requireHashForEqualityProof: boolean
  cleanupPolicy: CanonicalRealDataShadowCopyCleanupPolicy

  constructor(
    isEnabled: boolean = false,
    maxMetadataBytes: number = 1 * 1024 * 1024,
    maxGeneratedArtifactBytes: number = 512 * 1024,
    maxAudioBytes: number = 8 * 1024 * 1024,
    copyAudioBytesByDefault: boolean = false,
    allowHashComputationForBoundedBytes: boolean = true,
    requireHashForEqualityProof: boolean = true,
    cleanupPolicy: CanonicalRealDataShadowCopyCleanupPolicy = CanonicalRealDataShadowCopyCleanupPolicy.CLEANUP_IMMEDIATELY
  ) {
    this.isEnabled = isEnabled
    this.maxMetadataBytes = Math.max(0, maxMetadataBytes)
    this.maxGeneratedArtifactBytes = Math.max(0, maxGeneratedArtifactBytes)
    this.maxAudioBytes = Math.max(0, maxAudioBytes)
    this.copyAudioBytesByDefault = copyAudioBytesByDefault
    this.allowHashComputationForBoundedBytes = allowHashComputationForBoundedBytes
    this.requireHashForEqualityProof = requireHashForEqualityProof
    this.cleanupPolicy = cleanupPolicy
  }

  static readonly DISABLED = new CanonicalRealDataShadowCopyPolicy()

  maxBytes(kind: CanonicalRealDataShadowCopyKind): number {
    switch (kind) {
      case CanonicalRealDataShadowCopyKind.RECORDING_METADATA:
      case CanonicalRealDataShadowCopyKind.STUDY_METADATA:
      case CanonicalRealDataShadowCopyKind.RECEIVE_RECORD:
      case CanonicalRealDataShadowCopyKind.AUDIO_DESCRIPTOR:
        return this.maxMetadataBytes
      case CanonicalRealDataShadowCopyKind.GENERATED_ARTIFACT:
        return this.maxGeneratedArtifactBytes
      case CanonicalRealDataShadowCopyKind.AUDIO_BYTES:
        return this.copyAudioBytesByDefault ? this.maxAudioBytes : 0
    }
  }
}

// =============================================================================
// SHADOW COPY FAILURE & VERIFICATION
// =============================================================================

export enum CanonicalRealDataShadowCopyFailure {
  DISABLED = 'disabled',
  SOURCE_EQUALS_TARGET = 'sourceEqualsTarget',
  TARGET_IS_PRODUCTION_ROOT = 'targetIsProductionRoot',
  TARGET_INSIDE_PRODUCTION_ROOT = 'targetInsideProductionRoot',
  TARGET_PATH_INVALID = 'targetPathInvalid',
  SOURCE_OUTSIDE_PRODUCTION_ROOT = 'sourceOutsideProductionRoot',
  UNSAFE_LOGICAL_PATH_TOKEN = 'unsafeLogicalPathToken',
  SOURCE_READ_FAILED = 'sourceReadFailed',
  SOURCE_TOO_LARGE = 'sourceTooLarge',
  WRITE_FAILED = 'writeFailed',
  VERIFICATION_FAILED = 'verificationFailed',
  HASH_MISMATCH = 'hashMismatch',
  HASH_UNAVAILABLE_WHERE_REQUIRED = 'hashUnavailableWhereRequired',
  CLEANUP_FAILED = 'cleanupFailed',
  UNEXPECTED = 'unexpected'
}

export enum CanonicalRealDataShadowCopyVerificationStatus {
  VERIFIED = 'verified',
  EVIDENCE_ONLY = 'evidenceOnly',
  HASH_UNAVAILABLE = 'hashUnavailable',
  MISMATCH = 'mismatch',
  DESCRIPTOR_ONLY = 'descriptorOnly',
  FAILED = 'failed'
}

export class CanonicalRealDataShadowCopyVerification {
  sourceID: string
  kind: CanonicalRealDataShadowCopyKind
  logicalName: string
  byteSize: number
  modifiedAt: CanonicalTimestamp | null
  hashPrefix: string | null
  copiedBytes: boolean
  descriptorOnly: boolean
  equalityProof: boolean
  status: CanonicalRealDataShadowCopyVerificationStatus
  reason: string | null

  constructor(
    sourceID: string,
    kind: CanonicalRealDataShadowCopyKind,
    logicalName: string,
    byteSize: number,
    modifiedAt: Date | null = null,
    contentHash: CanonicalHash | null = null,
    copiedBytes: boolean,
    descriptorOnly: boolean,
    equalityProof: boolean,
    status: CanonicalRealDataShadowCopyVerificationStatus,
    reason: string | null = null
  ) {
    this.sourceID = CanonicalProductionRedaction.safeIdentifier(sourceID, 'source')
    this.kind = kind
    this.logicalName = logicalName
    this.byteSize = Math.max(0, byteSize)
    this.modifiedAt = modifiedAt ? new CanonicalTimestamp(modifiedAt) : null
    this.hashPrefix = contentHash ? CanonicalProductionRedaction.hashPrefix(contentHash.value) : null
    this.copiedBytes = copiedBytes
    this.descriptorOnly = descriptorOnly
    this.equalityProof = equalityProof
    this.status = status
    this.reason = CanonicalShadowMigrationRedaction.safeText(reason)
  }

  get id(): string {
    return this.sourceID
  }
}

// =============================================================================
// SHADOW COPY RESULT
// =============================================================================

export class CanonicalRealDataShadowCopyResult {
  planID: string
  rootID: string
  rootKind: CanonicalShadowRootKind
  started: boolean
  completed: boolean
  unavailable: boolean
  verificationStatus: string
  copiedEntryCount: number
  descriptorOnlyAudioCount: number
  bytesCopied: number
  hashUnavailableCount: number
  equalityProofCount: number
  failure: CanonicalRealDataShadowCopyFailure | null
  failureReason: string | null
  verifications: CanonicalRealDataShadowCopyVerification[]

  constructor(
    planID: string,
    rootID: string,
    rootKind: CanonicalShadowRootKind,
    started: boolean,
    completed: boolean,
    unavailable: boolean = false,
    failure: CanonicalRealDataShadowCopyFailure | null = null,
    failureReason: string | null = null,
    verifications: CanonicalRealDataShadowCopyVerification[] = []
  ) {
    this.planID = CanonicalProductionRedaction.safeIdentifier(planID, 'real-data-shadow-copy')
    this.rootID = CanonicalProductionRedaction.safeIdentifier(rootID, 'shadow-root')
    this.rootKind = rootKind
    this.started = started
    this.completed = completed
    this.unavailable = unavailable
    this.failure = failure
    this.failureReason = CanonicalShadowMigrationRedaction.safeText(failureReason)
    this.verifications = verifications.sort((a, b) => a.sourceID.localeCompare(b.sourceID))
    this.copiedEntryCount = verifications.filter(v => v.copiedBytes).length
    this.descriptorOnlyAudioCount = verifications.filter(v => v.descriptorOnly).length
    this.bytesCopied = verifications.filter(v => v.copiedBytes).reduce((sum, v) => sum + v.byteSize, 0)
    this.hashUnavailableCount = verifications.filter(v => v.status === CanonicalRealDataShadowCopyVerificationStatus.HASH_UNAVAILABLE).length
    this.equalityProofCount = verifications.filter(v => v.equalityProof).length
    if (failure) {
      this.verificationStatus = failure
    } else if (verifications.some(v => v.status === CanonicalRealDataShadowCopyVerificationStatus.MISMATCH || v.status === CanonicalRealDataShadowCopyVerificationStatus.FAILED)) {
      this.verificationStatus = 'failed'
    } else if (verifications.some(v => v.status === CanonicalRealDataShadowCopyVerificationStatus.HASH_UNAVAILABLE)) {
      this.verificationStatus = 'hashUnavailable'
    } else if (completed) {
      this.verificationStatus = 'verified'
    } else {
      this.verificationStatus = 'notStarted'
    }
  }

  static unavailable(
    planID: string,
    rootID: string,
    rootKind: CanonicalShadowRootKind,
    failure: CanonicalRealDataShadowCopyFailure,
    reason: string
  ): CanonicalRealDataShadowCopyResult {
    return new CanonicalRealDataShadowCopyResult(planID, rootID, rootKind, false, false, true, failure, reason)
  }

  get diagnosticsSummary(): string {
    return [
      'realDataCopy=' + (this.completed ? 'completed' : (this.unavailable ? 'unavailable' : 'failed')),
      'rootKind=' + this.rootKind, 'entries=' + this.copiedEntryCount,
      'audioDescriptors=' + this.descriptorOnlyAudioCount, 'bytes=' + this.bytesCopied,
      'hashUnavailable=' + this.hashUnavailableCount, 'equalityProofs=' + this.equalityProofCount,
      'verification=' + this.verificationStatus,
      'failure=' + (this.failure ?? 'none')
    ].join(',')
  }
}

// =============================================================================
// SHADOW ROOT LIFECYCLE
// =============================================================================

export class CanonicalShadowRootBinding {
  rootToken: CanonicalRootToken
  rootKind: CanonicalShadowRootKind
  rootURL: string
  prohibitedProductionRootURL: string | null

  constructor(
    rootToken: CanonicalRootToken,
    rootKind: CanonicalShadowRootKind,
    rootURL: string,
    prohibitedProductionRootURL: string | null = null
  ) {
    this.rootToken = rootToken
    this.rootKind = rootKind
    this.rootURL = rootURL
    this.prohibitedProductionRootURL = prohibitedProductionRootURL
  }
}

export class CanonicalShadowRootLifecycle {
  rootID: string
  rootKind: CanonicalShadowRootKind
  rootURL: string
  productionRootURL: string | null
  createdAt: Date

  constructor(
    rootID: string = CanonicalShadowRootLifecycle.generateUUID(),
    rootKind: CanonicalShadowRootKind,
    rootURL: string,
    productionRootURL: string | null = null,
    createdAt: Date = new Date()
  ) {
    this.rootID = CanonicalProductionRedaction.safeIdentifier(rootID, 'shadow-root')
    this.rootKind = rootKind
    this.rootURL = rootURL
    this.productionRootURL = productionRootURL
    this.createdAt = createdAt
  }

  private static generateUUID(): string {
    return 'shadow-root-' + Math.random().toString(36).substring(2, 10) + Date.now().toString(36)
  }
}

// =============================================================================
// SHADOW ROOT CLEANUP
// =============================================================================

export enum CanonicalShadowRootCleanupStatus {
  REMOVED = 'removed',
  RETAINED_FOR_DIAGNOSTICS = 'retainedForDiagnostics',
  RETAINED_FOR_NEXT_LAUNCH = 'retainedForNextLaunch',
  REFUSED_PRODUCTION_ROOT = 'refusedProductionRoot',
  FAILED = 'failed'
}

export class CanonicalShadowRootCleanupResult {
  rootID: string
  rootKind: CanonicalShadowRootKind
  status: CanonicalShadowRootCleanupStatus
  removedRootCount: number
  retainedRootCount: number
  removedBytes: number
  retainedBytes: number
  failureReason: string | null

  constructor(
    rootID: string,
    rootKind: CanonicalShadowRootKind,
    status: CanonicalShadowRootCleanupStatus,
    removedRootCount: number = 0,
    retainedRootCount: number = 0,
    removedBytes: number = 0,
    retainedBytes: number = 0,
    failureReason: string | null = null
  ) {
    this.rootID = CanonicalProductionRedaction.safeIdentifier(rootID, 'shadow-root')
    this.rootKind = rootKind
    this.status = status
    this.removedRootCount = Math.max(0, removedRootCount)
    this.retainedRootCount = Math.max(0, retainedRootCount)
    this.removedBytes = Math.max(0, removedBytes)
    this.retainedBytes = Math.max(0, retainedBytes)
    this.failureReason = CanonicalShadowMigrationRedaction.safeText(failureReason)
  }
}
