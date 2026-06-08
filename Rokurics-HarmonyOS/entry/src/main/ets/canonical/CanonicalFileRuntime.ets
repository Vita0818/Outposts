/**
 * CanonicalFileRuntime — canonical file store runtime with path resolution and validation.
 * Ported from Rokurics-Apple (CanonicalFileRuntime.swift).
 */

import {
  CanonicalHash, CanonicalTimestamp, nilIfEmpty, hashPrefix,
  safeLogicalPathToken, bufferToHex
} from './CanonicalCore'
import { cryptoFramework } from '@kit.CryptoArchitectureKit'

// =============================================================================
// ROOT TOKEN
// =============================================================================

export class CanonicalRootToken {
  rawValue: string

  constructor(rawValue: string) {
    this.rawValue = (nilIfEmpty(rawValue.trim()) ?? 'root:unknown')
  }
}

// =============================================================================
// FILE PURPOSE
// =============================================================================

export enum CanonicalFilePurpose {
  ARTIFACT_BYTES = 'artifactBytes',
  GENERATED_ARTIFACT = 'generatedArtifact',
  METADATA_BLOB = 'metadataBlob',
  TOMBSTONE_MARKER = 'tombstoneMarker'
}

// =============================================================================
// FILE REFERENCE
// =============================================================================

export class CanonicalFileReference {
  rootToken: CanonicalRootToken
  logicalPathToken: string
  artifactID: string | null
  artifactKind: string | null

  constructor(
    rootToken: CanonicalRootToken,
    logicalPathToken: string,
    artifactID: string | null = null,
    artifactKind: string | null = null
  ) {
    this.rootToken = rootToken
    this.logicalPathToken = logicalPathToken.trim()
    this.artifactID = (artifactID ? nilIfEmpty(artifactID.trim()) : null) ?? null
    this.artifactKind = artifactKind
  }
}

// =============================================================================
// WRITE POLICIES
// =============================================================================

export enum CanonicalAtomicWritePolicy {
  ATOMIC_REPLACE = 'atomicReplace',
  DIRECT_IN_MEMORY_REPLACE = 'directInMemoryReplace'
}

export enum CanonicalFileHashPolicy {
  SHA256 = 'sha256',
  NONE = 'none'
}

export enum CanonicalFileConflictPolicy {
  NO_OVERWRITE = 'noOverwrite',
  REPLACE = 'replace',
  REPLACE_IF_EXISTING_HASH_MATCHES = 'replaceIfExistingHashMatches',
  IDEMPOTENT_IF_SAME_CONTENT = 'idempotentIfSameContent'
}

// =============================================================================
// METADATA BLOB
// =============================================================================

export class CanonicalMetadataBlob {
  fields: Record<string, string>

  constructor(fields: Record<string, string> = {}) {
    const result: Record<string, string> = {}
    for (const key of Object.keys(fields)) {
      const trimmedKey = key.trim()
      if (trimmedKey.length > 0) {
        result[trimmedKey] = fields[key]
      }
    }
    this.fields = result
  }
}

// =============================================================================
// FILE INTENT & RESULTS
// =============================================================================

export class CanonicalFileWriteIntent {
  reference: CanonicalFileReference
  bytes: Uint8Array
  purpose: CanonicalFilePurpose
  expectedContentHash: CanonicalHash | null
  expectedByteSize: number | null
  expectedExistingHash: CanonicalHash | null
  atomicPolicy: CanonicalAtomicWritePolicy
  hashPolicy: CanonicalFileHashPolicy
  conflictPolicy: CanonicalFileConflictPolicy
  metadataBlob: CanonicalMetadataBlob | null

  constructor(
    reference: CanonicalFileReference,
    bytes: Uint8Array,
    purpose: CanonicalFilePurpose = CanonicalFilePurpose.ARTIFACT_BYTES,
    expectedContentHash: CanonicalHash | null = null,
    expectedByteSize: number | null = null,
    expectedExistingHash: CanonicalHash | null = null,
    atomicPolicy: CanonicalAtomicWritePolicy = CanonicalAtomicWritePolicy.ATOMIC_REPLACE,
    hashPolicy: CanonicalFileHashPolicy = CanonicalFileHashPolicy.SHA256,
    conflictPolicy: CanonicalFileConflictPolicy = CanonicalFileConflictPolicy.NO_OVERWRITE,
    metadataBlob: CanonicalMetadataBlob | null = null
  ) {
    this.reference = reference
    this.bytes = bytes
    this.purpose = purpose
    this.expectedContentHash = expectedContentHash
    this.expectedByteSize = expectedByteSize
    this.expectedExistingHash = expectedExistingHash
    this.atomicPolicy = atomicPolicy
    this.hashPolicy = hashPolicy
    this.conflictPolicy = conflictPolicy
    this.metadataBlob = metadataBlob
  }
}

export class CanonicalFileReadRequest {
  reference: CanonicalFileReference
  allowTombstonedRead: boolean

  constructor(reference: CanonicalFileReference, allowTombstonedRead: boolean = false) {
    this.reference = reference
    this.allowTombstonedRead = allowTombstonedRead
  }
}

export enum CanonicalFileWriteDisposition {
  CREATED = 'created',
  REPLACED = 'replaced',
  ACCEPTED_EXISTING = 'acceptedExisting',
  TOMBSTONE_MARKED = 'tombstoneMarked'
}

export class CanonicalPathResolutionResult {
  rootToken: CanonicalRootToken
  logicalPathToken: string
  resolvedPathToken: string
  isInsideRoot: boolean

  constructor(rootToken: CanonicalRootToken, logicalPathToken: string, resolvedPathToken: string, isInsideRoot: boolean) {
    this.rootToken = rootToken
    this.logicalPathToken = logicalPathToken
    this.resolvedPathToken = resolvedPathToken
    this.isInsideRoot = isInsideRoot
  }
}

export class CanonicalFileWriteResult {
  handle: CanonicalFileReference
  resolution: CanonicalPathResolutionResult
  byteSize: number
  contentHash: CanonicalHash | null
  disposition: CanonicalFileWriteDisposition
  purpose: CanonicalFilePurpose
  tombstoned: boolean

  constructor(
    handle: CanonicalFileReference,
    resolution: CanonicalPathResolutionResult,
    byteSize: number,
    contentHash: CanonicalHash | null,
    disposition: CanonicalFileWriteDisposition,
    purpose: CanonicalFilePurpose,
    tombstoned: boolean
  ) {
    this.handle = handle
    this.resolution = resolution
    this.byteSize = byteSize
    this.contentHash = contentHash
    this.disposition = disposition
    this.purpose = purpose
    this.tombstoned = tombstoned
  }
}

export class CanonicalFileReadResult {
  handle: CanonicalFileReference
  resolution: CanonicalPathResolutionResult
  bytes: Uint8Array
  byteSize: number
  contentHash: CanonicalHash | null
  purpose: CanonicalFilePurpose
  metadataBlob: CanonicalMetadataBlob | null
  tombstoned: boolean
  tombstoneReason: string | null

  constructor(
    handle: CanonicalFileReference,
    resolution: CanonicalPathResolutionResult,
    bytes: Uint8Array,
    byteSize: number,
    contentHash: CanonicalHash | null,
    purpose: CanonicalFilePurpose,
    metadataBlob: CanonicalMetadataBlob | null = null,
    tombstoned: boolean = false,
    tombstoneReason: string | null = null
  ) {
    this.handle = handle
    this.resolution = resolution
    this.bytes = bytes
    this.byteSize = byteSize
    this.contentHash = contentHash
    this.purpose = purpose
    this.metadataBlob = metadataBlob
    this.tombstoned = tombstoned
    this.tombstoneReason = tombstoneReason
  }
}

// =============================================================================
// FILE RUNTIME ERROR
// =============================================================================

export enum CanonicalFileRuntimeError {
  ROOT_NOT_BOUND = 'rootNotBound',
  INVALID_PATH_TOKEN = 'invalidPathToken',
  PATH_TRAVERSAL_REJECTED = 'pathTraversalRejected',
  ABSOLUTE_PATH_REJECTED = 'absolutePathRejected',
  SCHEME_URL_REJECTED = 'schemeURLRejected',
  BACKSLASH_TRAVERSAL_REJECTED = 'backslashTraversalRejected',
  ROOT_ESCAPE_REJECTED = 'rootEscapeRejected',
  FILE_NOT_FOUND = 'fileNotFound',
  TOMBSTONED = 'tombstoned',
  CONFLICT = 'conflict',
  PRE_WRITE_HASH_MISMATCH = 'preWriteHashMismatch',
  PRE_WRITE_SIZE_MISMATCH = 'preWriteSizeMismatch',
  POST_WRITE_HASH_MISMATCH = 'postWriteHashMismatch',
  POST_WRITE_SIZE_MISMATCH = 'postWriteSizeMismatch',
  EXISTING_HASH_MISMATCH = 'existingHashMismatch',
  UNSUPPORTED_HASH_POLICY = 'unsupportedHashPolicy'
}

// =============================================================================
// PATH RESOLVER
// =============================================================================

export interface CanonicalPathResolver {
  resolve(rootToken: CanonicalRootToken, logicalPathToken: string): CanonicalPathResolutionResult
}

export class CanonicalInMemoryPathResolver implements CanonicalPathResolver {
  private rootBindings: Map<CanonicalRootToken, string>

  constructor(rootBindings: Map<CanonicalRootToken, string>) {
    this.rootBindings = rootBindings
  }

  resolve(rootToken: CanonicalRootToken, logicalPathToken: string): CanonicalPathResolutionResult {
    const rawRoot = this.rootBindings.get(rootToken)
    if (!rawRoot) {
      throw CanonicalFileRuntimeError.ROOT_NOT_BOUND
    }
    const rootPath = CanonicalInMemoryPathResolver.validatedRootPath(rawRoot)
    const safeToken = CanonicalInMemoryPathResolver.validatedLogicalPath(logicalPathToken)
    const resolved = rootPath + '/' + safeToken
    if (!resolved.startsWith(rootPath + '/')) {
      throw CanonicalFileRuntimeError.ROOT_ESCAPE_REJECTED
    }
    return new CanonicalPathResolutionResult(rootToken, safeToken, resolved, true)
  }

  private static validatedRootPath(token: string): string {
    const trimmed = token.trim()
    if (trimmed.length === 0) {
      throw CanonicalFileRuntimeError.ROOT_ESCAPE_REJECTED
    }
    if (trimmed.startsWith('/')) {
      throw CanonicalFileRuntimeError.ROOT_ESCAPE_REJECTED
    }
    if (trimmed.includes('://')) {
      throw CanonicalFileRuntimeError.ROOT_ESCAPE_REJECTED
    }
    if (trimmed.includes('\\')) {
      throw CanonicalFileRuntimeError.ROOT_ESCAPE_REJECTED
    }
    const components = trimmed.split('/')
    for (const c of components) {
      if (c.length === 0 || c === '.' || c === '..') {
        throw CanonicalFileRuntimeError.ROOT_ESCAPE_REJECTED
      }
    }
    return trimmed
  }

  private static validatedLogicalPath(token: string): string {
    const trimmed = token.trim()
    if (trimmed.length === 0) {
      throw CanonicalFileRuntimeError.INVALID_PATH_TOKEN
    }
    if (trimmed.startsWith('/')) {
      throw CanonicalFileRuntimeError.ABSOLUTE_PATH_REJECTED
    }
    if (trimmed.includes('://')) {
      throw CanonicalFileRuntimeError.SCHEME_URL_REJECTED
    }
    if (trimmed.includes('\\')) {
      throw CanonicalFileRuntimeError.BACKSLASH_TRAVERSAL_REJECTED
    }
    const components = trimmed.split('/')
    for (const c of components) {
      if (c.length === 0 || c === '.' || c === '..') {
        throw CanonicalFileRuntimeError.PATH_TRAVERSAL_REJECTED
      }
    }
    if (!safeLogicalPathToken(trimmed)) {
      throw CanonicalFileRuntimeError.INVALID_PATH_TOKEN
    }
    return trimmed
  }
}

// =============================================================================
// FILE STORE PORT
// =============================================================================

export interface CanonicalFileStorePort {
  resolve(reference: CanonicalFileReference): CanonicalPathResolutionResult
  read(request: CanonicalFileReadRequest): CanonicalFileReadResult
  write(intent: CanonicalFileWriteIntent): CanonicalFileWriteResult
  markTombstone(reference: CanonicalFileReference, reason: string | null): CanonicalFileWriteResult
  contains(reference: CanonicalFileReference): boolean
}

// =============================================================================
// IN-MEMORY FILE STORE
// =============================================================================

interface StoreEntry {
  bytes: Uint8Array
  purpose: CanonicalFilePurpose
  contentHash: CanonicalHash | null
  metadataBlob: CanonicalMetadataBlob | null
  tombstoned: boolean
  tombstoneReason: string | null
}

export class InMemoryCanonicalFileStore implements CanonicalFileStorePort {
  private resolver: CanonicalPathResolver
  private entries: Map<string, StoreEntry> = new Map()

  constructor(rootBindings: Map<CanonicalRootToken, string>) {
    this.resolver = new CanonicalInMemoryPathResolver(rootBindings)
  }

  resolve(reference: CanonicalFileReference): CanonicalPathResolutionResult {
    return this.resolver.resolve(reference.rootToken, reference.logicalPathToken)
  }

  contains(reference: CanonicalFileReference): boolean {
    const resolution = this.resolve(reference)
    return this.entries.has(resolution.resolvedPathToken)
  }

  read(request: CanonicalFileReadRequest): CanonicalFileReadResult {
    const resolution = this.resolve(request.reference)
    const entry = this.entries.get(resolution.resolvedPathToken)
    if (!entry) {
      throw CanonicalFileRuntimeError.FILE_NOT_FOUND
    }
    if (entry.tombstoned && !request.allowTombstonedRead) {
      throw CanonicalFileRuntimeError.TOMBSTONED
    }
    return new CanonicalFileReadResult(
      request.reference, resolution, entry.bytes,
      entry.bytes.length, entry.contentHash, entry.purpose,
      entry.metadataBlob, entry.tombstoned, entry.tombstoneReason
    )
  }

  write(intent: CanonicalFileWriteIntent): CanonicalFileWriteResult {
    const resolution = this.resolve(intent.reference)
    const newHash = InMemoryCanonicalFileStore.hash(intent.bytes, intent.hashPolicy)
    InMemoryCanonicalFileStore.validatePreWrite(intent, newHash)

    const existing = this.entries.get(resolution.resolvedPathToken)
    if (existing &&
      InMemoryCanonicalFileStore.isIdempotentSameContent(intent, existing, newHash)) {
      return new CanonicalFileWriteResult(
        intent.reference, resolution, existing.bytes.length,
        existing.contentHash, CanonicalFileWriteDisposition.ACCEPTED_EXISTING,
        existing.purpose, existing.tombstoned
      )
    }
    InMemoryCanonicalFileStore.validateConflictPolicy(intent, existing, newHash)

    const entry: StoreEntry = {
      bytes: intent.bytes,
      purpose: intent.purpose,
      contentHash: newHash,
      metadataBlob: intent.metadataBlob,
      tombstoned: intent.purpose === CanonicalFilePurpose.TOMBSTONE_MARKER,
      tombstoneReason: intent.purpose === CanonicalFilePurpose.TOMBSTONE_MARKER ? 'marker' : null
    }

    this.entries.set(resolution.resolvedPathToken, entry)
    const stored = this.entries.get(resolution.resolvedPathToken) ?? entry
    InMemoryCanonicalFileStore.validatePostWrite(intent, stored)

    return new CanonicalFileWriteResult(
      intent.reference, resolution, stored.bytes.length,
      stored.contentHash, existing ? CanonicalFileWriteDisposition.REPLACED : CanonicalFileWriteDisposition.CREATED,
      stored.purpose, stored.tombstoned
    )
  }

  markTombstone(reference: CanonicalFileReference, reason: string | null): CanonicalFileWriteResult {
    const resolution = this.resolve(reference)
    let entry = this.entries.get(resolution.resolvedPathToken)
    if (!entry) {
      entry = {
        bytes: new Uint8Array(0),
        purpose: CanonicalFilePurpose.TOMBSTONE_MARKER,
        contentHash: InMemoryCanonicalFileStore.hash(new Uint8Array(0), CanonicalFileHashPolicy.SHA256),
        metadataBlob: null,
        tombstoned: false,
        tombstoneReason: null
      }
    }
    entry.tombstoned = true
    entry.tombstoneReason = (reason ? nilIfEmpty(reason.trim()) : null) ?? 'softDelete'
    this.entries.set(resolution.resolvedPathToken, entry)
    return new CanonicalFileWriteResult(
      reference, resolution, entry.bytes.length,
      entry.contentHash, CanonicalFileWriteDisposition.TOMBSTONE_MARKED,
      CanonicalFilePurpose.TOMBSTONE_MARKER, true
    )
  }

  static hash(data: Uint8Array, policy: CanonicalFileHashPolicy = CanonicalFileHashPolicy.SHA256): CanonicalHash | null {
    switch (policy) {
      case CanonicalFileHashPolicy.NONE:
        return null
      case CanonicalFileHashPolicy.SHA256:
        return CanonicalHash.sha256(data)
    }
  }

  static sameHash(left: CanonicalHash, right: CanonicalHash): boolean {
    return left.algorithm === right.algorithm && left.value === right.value
  }

  private static validatePreWrite(intent: CanonicalFileWriteIntent, actualHash: CanonicalHash | null) {
    if (intent.expectedByteSize !== null && intent.expectedByteSize !== intent.bytes.length) {
      throw CanonicalFileRuntimeError.PRE_WRITE_SIZE_MISMATCH
    }
    if (intent.expectedContentHash) {
      if (!actualHash) {
        throw CanonicalFileRuntimeError.UNSUPPORTED_HASH_POLICY
      }
      if (!InMemoryCanonicalFileStore.sameHash(intent.expectedContentHash, actualHash)) {
        throw CanonicalFileRuntimeError.PRE_WRITE_HASH_MISMATCH
      }
    }
  }

  private static validatePostWrite(intent: CanonicalFileWriteIntent, stored: StoreEntry) {
    if (intent.expectedByteSize !== null && intent.expectedByteSize !== stored.bytes.length) {
      throw CanonicalFileRuntimeError.POST_WRITE_SIZE_MISMATCH
    }
    if (intent.expectedContentHash) {
      if (!stored.contentHash) {
        throw CanonicalFileRuntimeError.UNSUPPORTED_HASH_POLICY
      }
      if (!InMemoryCanonicalFileStore.sameHash(intent.expectedContentHash, stored.contentHash)) {
        throw CanonicalFileRuntimeError.POST_WRITE_HASH_MISMATCH
      }
    }
  }

  private static validateConflictPolicy(
    intent: CanonicalFileWriteIntent,
    existing: StoreEntry | undefined,
    newHash: CanonicalHash | null
  ) {
    if (!existing) {
      return
    }
    if (intent.expectedExistingHash) {
      if (!existing.contentHash || !InMemoryCanonicalFileStore.sameHash(intent.expectedExistingHash, existing.contentHash)) {
        throw CanonicalFileRuntimeError.EXISTING_HASH_MISMATCH
      }
    }
    switch (intent.conflictPolicy) {
      case CanonicalFileConflictPolicy.REPLACE:
        return
      case CanonicalFileConflictPolicy.REPLACE_IF_EXISTING_HASH_MATCHES:
        if (!intent.expectedExistingHash) {
          throw CanonicalFileRuntimeError.EXISTING_HASH_MISMATCH
        }
        return
      case CanonicalFileConflictPolicy.IDEMPOTENT_IF_SAME_CONTENT:
        if (newHash && existing.contentHash &&
          InMemoryCanonicalFileStore.sameHash(existing.contentHash, newHash) &&
          existing.bytes.length === intent.bytes.length) {
          return
        }
        throw CanonicalFileRuntimeError.CONFLICT
      case CanonicalFileConflictPolicy.NO_OVERWRITE:
        if (newHash && existing.contentHash &&
          InMemoryCanonicalFileStore.sameHash(existing.contentHash, newHash) &&
          existing.bytes.length === intent.bytes.length) {
          return
        }
        throw CanonicalFileRuntimeError.CONFLICT
    }
  }

  private static isIdempotentSameContent(
    intent: CanonicalFileWriteIntent,
    existing: StoreEntry,
    newHash: CanonicalHash | null
  ): boolean {
    if (intent.conflictPolicy !== CanonicalFileConflictPolicy.IDEMPOTENT_IF_SAME_CONTENT &&
      intent.conflictPolicy !== CanonicalFileConflictPolicy.NO_OVERWRITE) {
      return false
    }
    if (!newHash || !existing.contentHash) {
      return false
    }
    return InMemoryCanonicalFileStore.sameHash(existing.contentHash, newHash) &&
      existing.bytes.length === intent.bytes.length
  }
}
