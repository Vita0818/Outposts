/**
 * CanonicalRootBoundMetadataWrite — root-bound metadata write port.
 * Ported from Rokurics-Apple (CanonicalRootBoundMetadataWrite.swift).
 * Handles root-bound file writes with checkpoint, rollback and SHA256 verification.
 * On HarmonyOS, file I/O uses @ohos.file.fs; SHA256 uses @kit.CryptoArchitectureKit.
 */

import { fileIo } from '@kit.CoreFileKit'
import { cryptoFramework } from '@kit.CryptoArchitectureKit'
import {
  CanonicalTimestamp, CanonicalHash, nilIfEmpty, hashPrefix, safeLogicalPathToken
} from './CanonicalCore'
import {
  CanonicalApplyAction, CanonicalApplyTarget
} from './CanonicalApplyPlan'
import { CanonicalRootToken } from './CanonicalApplyRuntime'

// =============================================================================
// SECTION 1: Apply Port Mode
// =============================================================================

export enum CanonicalRecordingMetadataApplyPortMode {
  DISABLED = 'disabled',
  DRY_RUN = 'dryRun',
  FAKE_IN_MEMORY = 'fakeInMemory',
  TEST_ROOT_BOUND = 'testRootBound',
  PRODUCTION_ROOT_DISABLED = 'productionRootDisabled',
  PRODUCTION_ROOT_BOUND = 'productionRootBound',
  PRODUCTION_ROOT_UNSUPPORTED = 'productionRootUnsupported'
}

export function isNonDryRunRootBound(mode: CanonicalRecordingMetadataApplyPortMode): boolean {
  return mode === CanonicalRecordingMetadataApplyPortMode.TEST_ROOT_BOUND ||
    mode === CanonicalRecordingMetadataApplyPortMode.PRODUCTION_ROOT_BOUND
}

export function isDefaultDisabled(mode: CanonicalRecordingMetadataApplyPortMode): boolean {
  return mode === CanonicalRecordingMetadataApplyPortMode.DISABLED ||
    mode === CanonicalRecordingMetadataApplyPortMode.DRY_RUN ||
    mode === CanonicalRecordingMetadataApplyPortMode.PRODUCTION_ROOT_DISABLED
}

// =============================================================================
// SECTION 2: Write Failure
// =============================================================================

export enum CanonicalRootBoundMetadataWriteFailure {
  ROOT_ESCAPE = 'rootEscape',
  PRODUCTION_ROOT_DISABLED = 'productionRootDisabled',
  CHECKPOINT_FAILED = 'checkpointFailed',
  ATOMIC_WRITE_FAILED = 'atomicWriteFailed',
  POSTCONDITION_FAILED = 'postconditionFailed',
  ROLLBACK_FAILED = 'rollbackFailed',
  UNSUPPORTED_STORE_API = 'unsupportedStoreAPI',
  SCHEMA_MISMATCH = 'schemaMismatch',
  DECODING_FAILED = 'decodingFailed',
  PERMISSION_DENIED = 'permissionDenied',
  UNKNOWN = 'unknown'
}

// =============================================================================
// SECTION 3: Production Domain & Redaction (local)
// =============================================================================

export enum CanonicalProductionDomain {
  RECORDING_METADATA = 'recordingMetadata',
  LIBRARY_METADATA = 'libraryMetadata',
  GENERATED_ARTIFACT = 'generatedArtifact',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  FILE_RUNTIME = 'fileRuntime',
  TRANSPORT_RUNTIME = 'transportRuntime',
  UPLOAD_RUNTIME = 'uploadRuntime',
  APPLY = 'apply'
}

export class CanonicalProductionRedaction {
  static safeIdentifier(value: string, fallback: string = 'unknown'): string {
    const trimmed = value.trim()
    return trimmed.length > 0 ? trimmed : fallback
  }

  static hashPrefix(value: string): string | null {
    const trimmed = value.trim()
    return trimmed.length >= 8 ? trimmed.substring(0, 12) : null
  }

  static safeText(value: string | null | undefined): string | null {
    const trimmed = value?.trim()
    if (!trimmed || trimmed.length === 0) {
      return null
    }
    return trimmed
  }
}

// =============================================================================
// SECTION 4: CanonicalRecordingMetadataCutoverActionKind
// =============================================================================

export enum CanonicalRecordingMetadataCutoverActionKind {
  APPLY = 'apply',
  SEND = 'send'
}

// =============================================================================
// SECTION 5: CanonicalRootBoundMetadataTarget
// =============================================================================

export class CanonicalRootBoundMetadataTarget {
  rootToken: CanonicalRootToken
  objectID: string
  domain: CanonicalProductionDomain
  logicalPathToken: string

  constructor(
    rootToken: CanonicalRootToken,
    objectID: string,
    domain: CanonicalProductionDomain = CanonicalProductionDomain.RECORDING_METADATA,
    logicalPathToken: string
  ) {
    const sanitizedObjectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    if (domain !== CanonicalProductionDomain.RECORDING_METADATA) {
      throw new Error(CanonicalRootBoundMetadataWriteFailure.UNSUPPORTED_STORE_API)
    }
    const safePath = safeLogicalPathToken(logicalPathToken)
    if (!safePath) {
      throw new Error(CanonicalRootBoundMetadataWriteFailure.ROOT_ESCAPE)
    }
    this.rootToken = rootToken
    this.objectID = sanitizedObjectID
    this.domain = domain
    this.logicalPathToken = safePath
  }

  static defaultLogicalPathToken(objectID: string): string {
    return 'recordingMetadata/' + CanonicalRootBoundMetadataTarget.safePathComponent(objectID) + '.json'
  }

  private static safePathComponent(value: string): string {
    const trimmed = value.trim()
    const allowedRegex = /^[a-zA-Z0-9\-_.]+$/
    let result = ''
    for (const ch of trimmed) {
      result += allowedRegex.test(ch) ? ch : '-'
    }
    const candidate = result.replace(/^[\-.]+|[\-.]+$/g, '')
    if (candidate.length > 0) {
      return candidate
    }
    const sha = CanonicalHash.sha256OfString(trimmed)
    return 'recording-' + (CanonicalProductionRedaction.hashPrefix(sha.value) ?? 'unknown')
  }
}

// =============================================================================
// SECTION 6: CanonicalRootBoundMetadataWrite (payload)
// =============================================================================

export class CanonicalRootBoundMetadataWrite {
  target: CanonicalRootBoundMetadataTarget
  actionKind: CanonicalRecordingMetadataCutoverActionKind
  metadataBytes: Uint8Array
  metadataHash: CanonicalHash | null
  tombstone: boolean
  modifiedAt: CanonicalTimestamp | null

  constructor(
    target: CanonicalRootBoundMetadataTarget,
    actionKind: CanonicalRecordingMetadataCutoverActionKind,
    metadataBytes: Uint8Array,
    metadataHash: CanonicalHash | null = null,
    tombstone: boolean = false,
    modifiedAt: CanonicalTimestamp | null = null
  ) {
    this.target = target
    this.actionKind = actionKind
    this.metadataBytes = metadataBytes
    this.metadataHash = metadataHash
    this.tombstone = tombstone
    this.modifiedAt = modifiedAt
  }
}

// =============================================================================
// SECTION 7: CanonicalRootBoundMetadataCheckpoint
// =============================================================================

export class CanonicalRootBoundMetadataCheckpoint {
  checkpointID: string
  objectID: string
  domain: CanonicalProductionDomain
  rollbackID: string
  hashPrefixBefore: string | null
  byteCountBefore: number | null
  existedBeforeWrite: boolean
  rollbackAvailable: boolean

  constructor(
    checkpointID: string,
    objectID: string,
    domain: CanonicalProductionDomain = CanonicalProductionDomain.RECORDING_METADATA,
    rollbackID: string,
    hashBefore: CanonicalHash | null = null,
    byteCountBefore: number | null = null,
    existedBeforeWrite: boolean,
    rollbackAvailable: boolean
  ) {
    this.checkpointID = CanonicalProductionRedaction.safeIdentifier(checkpointID, 'metadata-checkpoint')
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.domain = domain
    this.rollbackID = CanonicalProductionRedaction.safeIdentifier(rollbackID, 'metadata-rollback')
    this.hashPrefixBefore = hashBefore ? CanonicalProductionRedaction.hashPrefix(hashBefore.value) : null
    this.byteCountBefore = byteCountBefore
    this.existedBeforeWrite = existedBeforeWrite
    this.rollbackAvailable = rollbackAvailable
  }

  get id(): string {
    return this.checkpointID
  }
}

// =============================================================================
// SECTION 8: CanonicalRootBoundMetadataWriteResult
// =============================================================================

export class CanonicalRootBoundMetadataWriteResult {
  objectID: string
  domain: CanonicalProductionDomain
  actionKind: CanonicalRecordingMetadataCutoverActionKind
  hashPrefixBefore: string | null
  hashPrefixAfter: string | null
  byteCount: number
  checkpointID: string
  atomicWriteUsed: boolean
  rollbackAvailable: boolean
  tombstone: boolean
  modifiedAt: CanonicalTimestamp | null
  failure: CanonicalRootBoundMetadataWriteFailure | null

  constructor(
    objectID: string,
    domain: CanonicalProductionDomain = CanonicalProductionDomain.RECORDING_METADATA,
    actionKind: CanonicalRecordingMetadataCutoverActionKind,
    hashBefore: CanonicalHash | null = null,
    hashAfter: CanonicalHash | null = null,
    byteCount: number,
    checkpointID: string,
    atomicWriteUsed: boolean,
    rollbackAvailable: boolean,
    tombstone: boolean,
    modifiedAt: CanonicalTimestamp | null,
    failure: CanonicalRootBoundMetadataWriteFailure | null = null
  ) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.domain = domain
    this.actionKind = actionKind
    this.hashPrefixBefore = hashBefore ? CanonicalProductionRedaction.hashPrefix(hashBefore.value) : null
    this.hashPrefixAfter = hashAfter ? CanonicalProductionRedaction.hashPrefix(hashAfter.value) : null
    this.byteCount = Math.max(0, byteCount)
    this.checkpointID = CanonicalProductionRedaction.safeIdentifier(checkpointID, 'metadata-checkpoint')
    this.atomicWriteUsed = atomicWriteUsed
    this.rollbackAvailable = rollbackAvailable
    this.tombstone = tombstone
    this.modifiedAt = modifiedAt
    this.failure = failure
  }
}

// =============================================================================
// SECTION 9: CanonicalRootBoundMetadataRollbackResult
// =============================================================================

export class CanonicalRootBoundMetadataRollbackResult {
  objectID: string
  domain: CanonicalProductionDomain
  checkpointID: string
  succeeded: boolean
  rollbackVerified: boolean
  hashPrefixAfterRollback: string | null
  byteCount: number | null
  failure: CanonicalRootBoundMetadataWriteFailure | null

  constructor(
    objectID: string,
    domain: CanonicalProductionDomain = CanonicalProductionDomain.RECORDING_METADATA,
    checkpointID: string,
    succeeded: boolean,
    rollbackVerified: boolean,
    hashAfterRollback: CanonicalHash | null = null,
    byteCount: number | null = null,
    failure: CanonicalRootBoundMetadataWriteFailure | null = null
  ) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.domain = domain
    this.checkpointID = CanonicalProductionRedaction.safeIdentifier(checkpointID, 'metadata-checkpoint')
    this.succeeded = succeeded
    this.rollbackVerified = rollbackVerified
    this.hashPrefixAfterRollback = hashAfterRollback
      ? CanonicalProductionRedaction.hashPrefix(hashAfterRollback.value) : null
    this.byteCount = byteCount
    this.failure = failure
  }
}

// =============================================================================
// SECTION 10: In-Memory File Manager (HarmonyOS adaptation)
// =============================================================================

interface StoredCheckpoint {
  publicCheckpoint: CanonicalRootBoundMetadataCheckpoint
  target: CanonicalRootBoundMetadataTarget
  previousBytes: Uint8Array | null
  previousHash: CanonicalHash | null
}

export class CanonicalRootBoundMetadataWriteCore {
  private rootURL: string
  private rootToken: CanonicalRootToken
  private mode: CanonicalRecordingMetadataApplyPortMode
  private payloadsByActionID: Map<string, CanonicalRootBoundMetadataWrite> = new Map()
  private payloadsByObjectAndKind: Map<string, CanonicalRootBoundMetadataWrite> = new Map()
  private checkpoints: Map<string, StoredCheckpoint> = new Map()
  private actionIDsByCheckpointID: Map<string, string> = new Map()
  private lastWriteByActionID: Map<string, CanonicalRootBoundMetadataWriteResult> = new Map()
  private lastRollbackByCheckpointID: Map<string, CanonicalRootBoundMetadataRollbackResult> = new Map()
  private checkpointFailureObjectIDs: Set<string> = new Set()
  private postconditionFailureObjectIDs: Set<string> = new Set()
  private rollbackFailureCheckpointIDs: Set<string> = new Set()
  private inMemoryFiles: Map<string, Uint8Array> = new Map()

  constructor(
    rootURL: string,
    rootToken: CanonicalRootToken,
    mode: CanonicalRecordingMetadataApplyPortMode
  ) {
    if (!rootURL.startsWith('/') && !rootURL.startsWith('file://')) {
      throw new Error(CanonicalRootBoundMetadataWriteFailure.ROOT_ESCAPE)
    }
    this.rootURL = rootURL
    this.rootToken = rootToken
    this.mode = mode
  }

  get applyPortMode(): CanonicalRecordingMetadataApplyPortMode {
    return this.mode
  }

  setPayload(
    objectID: string,
    actionKind: CanonicalRecordingMetadataCutoverActionKind,
    metadataBytes: Uint8Array,
    metadataHash: CanonicalHash | null = null,
    tombstone: boolean = false,
    modifiedAt: CanonicalTimestamp | null = null,
    logicalPathToken: string | null = null,
    actionID: string | null = null
  ): void {
    const target = new CanonicalRootBoundMetadataTarget(
      this.rootToken, objectID, CanonicalProductionDomain.RECORDING_METADATA,
      logicalPathToken ?? CanonicalRootBoundMetadataTarget.defaultLogicalPathToken(objectID)
    )
    const write = new CanonicalRootBoundMetadataWrite(
      target, actionKind, metadataBytes, metadataHash, tombstone, modifiedAt
    )
    this.payloadsByObjectAndKind.set(this.makeKey(target.objectID, actionKind), write)
    if (actionID) {
      this.payloadsByActionID.set(
        CanonicalProductionRedaction.safeIdentifier(actionID, actionKind), write
      )
    }
  }

  injectCheckpointFailure(objectID: string): void {
    this.checkpointFailureObjectIDs.add(
      CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    )
  }

  injectPostconditionFailure(objectID: string): void {
    this.postconditionFailureObjectIDs.add(
      CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    )
  }

  injectRollbackFailure(checkpointID: string): void {
    this.rollbackFailureCheckpointIDs.add(
      CanonicalProductionRedaction.safeIdentifier(checkpointID, 'metadata-checkpoint')
    )
  }

  async write(
    action: CanonicalApplyAction,
    actionKind: CanonicalRecordingMetadataCutoverActionKind,
    checkpointID: string | null
  ): Promise<CanonicalRootBoundMetadataWriteResult> {
    this.requireWritableMode()
    const objectID = CanonicalProductionRedaction.safeIdentifier(action.target.objectID, 'unknown-recording')
    const payload = this.payloadsByActionID.get(action.actionID) ??
      this.payloadsByObjectAndKind.get(this.makeKey(objectID, actionKind))
    if (!payload) {
      throw new Error(CanonicalRootBoundMetadataWriteFailure.UNSUPPORTED_STORE_API)
    }
    if (payload.actionKind !== actionKind || payload.target.objectID !== objectID) {
      throw new Error(CanonicalRootBoundMetadataWriteFailure.SCHEMA_MISMATCH)
    }
    if (this.checkpointFailureObjectIDs.has(objectID)) {
      throw new Error(CanonicalRootBoundMetadataWriteFailure.CHECKPOINT_FAILED)
    }
    const effectiveCheckpointID = CanonicalProductionRedaction.safeIdentifier(
      checkpointID ?? 'root-bound-metadata-' + objectID + '-' + actionKind,
      'metadata-checkpoint'
    )
    const targetPath = this.resolvedPath(payload.target)
    const previousBytes = this.inMemoryFiles.get(targetPath) ?? null
    const previousHash = previousBytes ? sha256Hash(previousBytes) : null
    const checkpoint = new CanonicalRootBoundMetadataCheckpoint(
      effectiveCheckpointID, objectID, CanonicalProductionDomain.RECORDING_METADATA,
      'rollback-' + effectiveCheckpointID, previousHash,
      previousBytes ? previousBytes.length : null,
      previousBytes !== null, true
    )
    this.checkpoints.set(effectiveCheckpointID, {
      publicCheckpoint: checkpoint,
      target: payload.target,
      previousBytes: previousBytes,
      previousHash: previousHash
    })
    this.actionIDsByCheckpointID.set(effectiveCheckpointID, action.actionID)

    try {
      this.inMemoryFiles.set(targetPath, payload.metadataBytes)
      const reread = this.inMemoryFiles.get(targetPath)
      if (!reread || !bytesEqual(reread, payload.metadataBytes)) {
        this.restore(effectiveCheckpointID)
        throw new Error(CanonicalRootBoundMetadataWriteFailure.POSTCONDITION_FAILED)
      }
      const afterHash = payload.metadataHash ?? sha256Hash(reread)
      const result = new CanonicalRootBoundMetadataWriteResult(
        objectID, CanonicalProductionDomain.RECORDING_METADATA, actionKind,
        previousHash, afterHash, reread.length,
        effectiveCheckpointID, true, true,
        payload.tombstone, payload.modifiedAt
      )
      this.lastWriteByActionID.set(action.actionID, result)
      return result
    } catch (failure) {
      if (typeof failure === 'string' && Object.values(CanonicalRootBoundMetadataWriteFailure).includes(
        failure as CanonicalRootBoundMetadataWriteFailure)) {
        throw failure
      }
      try {
        this.restore(effectiveCheckpointID)
      } catch {
        // restore failed, ignore
      }
      throw new Error(CanonicalRootBoundMetadataWriteFailure.ATOMIC_WRITE_FAILED)
    }
  }

  verifyPostcondition(
    postcondition: { target: CanonicalApplyTarget; actionID: string; accepted: boolean;
      reason: string | null; actualHashPrefix: string | null }
  ): { target: CanonicalApplyTarget; actionID: string; accepted: boolean; reason: string | null;
    actualHashPrefix: string | null } {
    const checked = {
      target: postcondition.target,
      actionID: postcondition.actionID,
      accepted: postcondition.accepted,
      reason: postcondition.reason,
      actualHashPrefix: postcondition.actualHashPrefix
    }
    const objectID = CanonicalProductionRedaction.safeIdentifier(
      postcondition.target.objectID, 'unknown-recording'
    )
    if (this.postconditionFailureObjectIDs.has(objectID)) {
      checked.accepted = false
      checked.reason = CanonicalRootBoundMetadataWriteFailure.POSTCONDITION_FAILED
      return checked
    }
    const write = this.lastWriteByActionID.get(postcondition.actionID)
    if (!write) {
      checked.accepted = false
      checked.reason = CanonicalRootBoundMetadataWriteFailure.POSTCONDITION_FAILED
      return checked
    }
    if (postcondition.actualHashPrefix !== null && write.hashPrefixAfter !== null) {
      const expectedPrefix = CanonicalProductionRedaction.hashPrefix(postcondition.actualHashPrefix)
      if (expectedPrefix !== write.hashPrefixAfter) {
        checked.accepted = false
        checked.reason = CanonicalRootBoundMetadataWriteFailure.POSTCONDITION_FAILED
      }
    }
    return checked
  }

  rollback(
    checkpointID: string, objectID: string | null
  ): CanonicalRootBoundMetadataRollbackResult {
    const safeCheckpointID = CanonicalProductionRedaction.safeIdentifier(checkpointID, 'metadata-checkpoint')
    const safeObjectID = objectID
      ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
      : 'unknown-recording'
    if (this.rollbackFailureCheckpointIDs.has(safeCheckpointID)) {
      const result = new CanonicalRootBoundMetadataRollbackResult(
        safeObjectID, CanonicalProductionDomain.RECORDING_METADATA,
        safeCheckpointID, false, false, null, null,
        CanonicalRootBoundMetadataWriteFailure.ROLLBACK_FAILED
      )
      this.lastRollbackByCheckpointID.set(safeCheckpointID, result)
      return result
    }
    try {
      this.restore(safeCheckpointID)
      const storedCheckpoint = this.checkpoints.get(safeCheckpointID)
      if (!storedCheckpoint) {
        throw new Error(CanonicalRootBoundMetadataWriteFailure.ROLLBACK_FAILED)
      }
      const targetPath = this.resolvedPath(storedCheckpoint.target)
      const currentBytes = this.inMemoryFiles.get(targetPath) ?? null
      const verified = bytesEqual(currentBytes ?? null, storedCheckpoint.previousBytes)
      const result = new CanonicalRootBoundMetadataRollbackResult(
        storedCheckpoint.publicCheckpoint.objectID,
        CanonicalProductionDomain.RECORDING_METADATA,
        safeCheckpointID, verified, verified,
        currentBytes ? sha256Hash(currentBytes) : null,
        currentBytes ? currentBytes.length : null,
        verified ? null : CanonicalRootBoundMetadataWriteFailure.ROLLBACK_FAILED
      )
      this.lastRollbackByCheckpointID.set(safeCheckpointID, result)
      return result
    } catch {
      const result = new CanonicalRootBoundMetadataRollbackResult(
        safeObjectID, CanonicalProductionDomain.RECORDING_METADATA,
        safeCheckpointID, false, false, null, null,
        CanonicalRootBoundMetadataWriteFailure.ROLLBACK_FAILED
      )
      this.lastRollbackByCheckpointID.set(safeCheckpointID, result)
      return result
    }
  }

  lastWriteResult(actionID: string): CanonicalRootBoundMetadataWriteResult | null {
    return this.lastWriteByActionID.get(actionID) ?? null
  }

  lastRollbackResult(checkpointID: string): CanonicalRootBoundMetadataRollbackResult | null {
    const safe = CanonicalProductionRedaction.safeIdentifier(checkpointID, 'metadata-checkpoint')
    return this.lastRollbackByCheckpointID.get(safe) ?? null
  }

  readMetadataBytes(
    objectID: string, actionKind: CanonicalRecordingMetadataCutoverActionKind = CanonicalRecordingMetadataCutoverActionKind.APPLY
  ): Uint8Array | null {
    const safeObjectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    const payload = this.payloadsByObjectAndKind.get(this.makeKey(safeObjectID, actionKind))
    if (!payload) {
      return null
    }
    const path = this.resolvedPath(payload.target)
    return this.inMemoryFiles.get(path) ?? null
  }

  private requireWritableMode(): void {
    switch (this.mode) {
      case CanonicalRecordingMetadataApplyPortMode.TEST_ROOT_BOUND:
      case CanonicalRecordingMetadataApplyPortMode.PRODUCTION_ROOT_BOUND:
        return
      case CanonicalRecordingMetadataApplyPortMode.PRODUCTION_ROOT_DISABLED:
      case CanonicalRecordingMetadataApplyPortMode.DISABLED:
      case CanonicalRecordingMetadataApplyPortMode.DRY_RUN:
        throw new Error(CanonicalRootBoundMetadataWriteFailure.PRODUCTION_ROOT_DISABLED)
      case CanonicalRecordingMetadataApplyPortMode.PRODUCTION_ROOT_UNSUPPORTED:
      case CanonicalRecordingMetadataApplyPortMode.FAKE_IN_MEMORY:
        throw new Error(CanonicalRootBoundMetadataWriteFailure.UNSUPPORTED_STORE_API)
    }
  }

  private restore(checkpointID: string): void {
    const storedCheckpoint = this.checkpoints.get(checkpointID)
    if (!storedCheckpoint) {
      throw new Error(CanonicalRootBoundMetadataWriteFailure.ROLLBACK_FAILED)
    }
    const path = this.resolvedPath(storedCheckpoint.target)
    if (storedCheckpoint.previousBytes) {
      this.inMemoryFiles.set(path, storedCheckpoint.previousBytes)
    } else if (this.inMemoryFiles.has(path)) {
      this.inMemoryFiles.delete(path)
    }
  }

  private resolvedPath(target: CanonicalRootBoundMetadataTarget): string {
    if (target.rootToken.rawValue !== this.rootToken.rawValue || !safeLogicalPathToken(target.logicalPathToken)) {
      throw new Error(CanonicalRootBoundMetadataWriteFailure.ROOT_ESCAPE)
    }
    let rootPath = this.rootURL
    if (!rootPath.endsWith('/')) {
      rootPath += '/'
    }
    const fullPath = rootPath + target.logicalPathToken
    if (!fullPath.startsWith(rootPath) || fullPath === rootPath) {
      throw new Error(CanonicalRootBoundMetadataWriteFailure.ROOT_ESCAPE)
    }
    return fullPath
  }

  private makeKey(objectID: string, actionKind: CanonicalRecordingMetadataCutoverActionKind): string {
    return CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording') + '|' + actionKind
  }
}

// =============================================================================
// SECTION 11: SHA256 Utility
// =============================================================================

function sha256Hash(data: Uint8Array): CanonicalHash {
  return CanonicalHash.sha256OfString(bytesToHex(data))
}

function bytesToHex(data: Uint8Array): string {
  let hex = ''
  for (let i = 0; i < data.length; i++) {
    hex += data[i].toString(16).padStart(2, '0')
  }
  return hex
}

function bytesEqual(a: Uint8Array | null, b: Uint8Array | null): boolean {
  if (a === b) {
    return true
  }
  if (!a || !b || a.length !== b.length) {
    return false
  }
  for (let i = 0; i < a.length; i++) {
    if (a[i] !== b[i]) {
      return false
    }
  }
  return true
}
