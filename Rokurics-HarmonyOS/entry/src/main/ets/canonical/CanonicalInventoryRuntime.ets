/**
 * CanonicalInventoryRuntime — checksum cache and inventory runtime builder.
 * Ported from Rokurics-Apple (CanonicalInventoryRuntime.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalManifest, nilIfEmpty, bufferToHex
} from './CanonicalCore'
import { cryptoFramework } from '@kit.CryptoArchitectureKit'

// =============================================================================
// INVENTORY RUNTIME NODE ROLE
// =============================================================================

export enum CanonicalInventoryRuntimeNodeRole {
  IPHONE = 'iPhone',
  MAC = 'mac'
}

export enum CanonicalInventoryRuntimeSourceKind {
  SYNC_TICK = 'syncTick',
  INVENTORY_REQUEST = 'inventoryRequest',
  ARTIFACT_LOOKUP = 'artifactLookup',
  TEST_HARNESS = 'testHarness'
}

// =============================================================================
// CONFIGURATION
// =============================================================================

export class CanonicalInventoryRuntimeConfiguration {
  static readonly CURRENT_SCHEMA_VERSION = 1

  checksumSchemaVersion: number
  hashAlgorithm: string
  cacheFileName: string
  redactedDiagnostics: boolean
  persistentChecksumCacheEnabled: boolean

  constructor(
    checksumSchemaVersion: number = CanonicalInventoryRuntimeConfiguration.CURRENT_SCHEMA_VERSION,
    hashAlgorithm: string = 'sha256',
    cacheFileName: string = 'canonical-checksum-cache-v1.json',
    redactedDiagnostics: boolean = true,
    persistentChecksumCacheEnabled: boolean = true
  ) {
    this.checksumSchemaVersion = checksumSchemaVersion
    this.hashAlgorithm = hashAlgorithm
    this.cacheFileName = cacheFileName
    this.redactedDiagnostics = redactedDiagnostics
    this.persistentChecksumCacheEnabled = persistentChecksumCacheEnabled
  }
}

// =============================================================================
// DIAGNOSTICS
// =============================================================================

export class CanonicalInventoryRuntimeDiagnostics {
  checksumCacheHitCount: number
  checksumCacheMissCount: number
  checksumCacheStaleCount: number
  checksumCacheErrorCount: number
  fileScanCount: number
  hashComputedCount: number
  mainActorHashBlockedCount: number
  mainActorScanBlockedCount: number
  duplicateBuildCount: number
  scanDurationMs: number
  hashDurationMs: number

  constructor(
    checksumCacheHitCount: number = 0,
    checksumCacheMissCount: number = 0,
    checksumCacheStaleCount: number = 0,
    checksumCacheErrorCount: number = 0,
    fileScanCount: number = 0,
    hashComputedCount: number = 0,
    mainActorHashBlockedCount: number = 0,
    mainActorScanBlockedCount: number = 0,
    duplicateBuildCount: number = 0,
    scanDurationMs: number = 0,
    hashDurationMs: number = 0
  ) {
    this.checksumCacheHitCount = checksumCacheHitCount
    this.checksumCacheMissCount = checksumCacheMissCount
    this.checksumCacheStaleCount = checksumCacheStaleCount
    this.checksumCacheErrorCount = checksumCacheErrorCount
    this.fileScanCount = fileScanCount
    this.hashComputedCount = hashComputedCount
    this.mainActorHashBlockedCount = mainActorHashBlockedCount
    this.mainActorScanBlockedCount = mainActorScanBlockedCount
    this.duplicateBuildCount = duplicateBuildCount
    this.scanDurationMs = scanDurationMs
    this.hashDurationMs = hashDurationMs
  }

  merge(result: CanonicalChecksumCacheResult) {
    this.fileScanCount++
    if (result.hashComputed) {
      this.hashComputedCount++
    }
    switch (result.event) {
      case CanonicalChecksumCacheEvent.HIT:
        this.checksumCacheHitCount++
        break
      case CanonicalChecksumCacheEvent.MISS:
        this.checksumCacheMissCount++
        break
      case CanonicalChecksumCacheEvent.STALE:
        this.checksumCacheStaleCount++
        break
      case CanonicalChecksumCacheEvent.ERROR:
        this.checksumCacheErrorCount++
        break
    }
    this.hashDurationMs += result.hashDurationMs
  }
}

// =============================================================================
// INVENTORY OBJECT COUNTS
// =============================================================================

export class CanonicalInventoryObjectCounts {
  recordingMetadataCount: number
  libraryFolderCount: number
  libraryItemCount: number
  artifactCount: number
  audioDescriptorCount: number

  constructor(
    recordingMetadataCount: number = 0,
    libraryFolderCount: number = 0,
    libraryItemCount: number = 0,
    artifactCount: number = 0,
    audioDescriptorCount: number = 0
  ) {
    this.recordingMetadataCount = recordingMetadataCount
    this.libraryFolderCount = libraryFolderCount
    this.libraryItemCount = libraryItemCount
    this.artifactCount = artifactCount
    this.audioDescriptorCount = audioDescriptorCount
  }
}

// =============================================================================
// INVENTORY SNAPSHOT & RESULT
// =============================================================================

export class CanonicalInventoryRuntimeSnapshot {
  syncRunID: string
  nodeRole: CanonicalInventoryRuntimeNodeRole
  buildStartedAt: Date
  buildEndedAt: Date
  sourceKind: CanonicalInventoryRuntimeSourceKind
  objectCounts: CanonicalInventoryObjectCounts
  diagnostics: CanonicalInventoryRuntimeDiagnostics
  mainActorBlocked: boolean
  reusedWithinTick: boolean
  redacted: boolean

  constructor(
    syncRunID: string,
    nodeRole: CanonicalInventoryRuntimeNodeRole,
    buildStartedAt: Date,
    buildEndedAt: Date,
    sourceKind: CanonicalInventoryRuntimeSourceKind,
    objectCounts: CanonicalInventoryObjectCounts,
    diagnostics: CanonicalInventoryRuntimeDiagnostics,
    mainActorBlocked: boolean,
    reusedWithinTick: boolean,
    redacted: boolean
  ) {
    this.syncRunID = syncRunID
    this.nodeRole = nodeRole
    this.buildStartedAt = buildStartedAt
    this.buildEndedAt = buildEndedAt
    this.sourceKind = sourceKind
    this.objectCounts = objectCounts
    this.diagnostics = diagnostics
    this.mainActorBlocked = mainActorBlocked
    this.reusedWithinTick = reusedWithinTick
    this.redacted = redacted
  }
}

export class CanonicalInventoryRuntimeResult {
  snapshot: CanonicalInventoryRuntimeSnapshot
  failures: CanonicalInventoryRuntimeFailure[]

  constructor(
    snapshot: CanonicalInventoryRuntimeSnapshot,
    failures: CanonicalInventoryRuntimeFailure[]
  ) {
    this.snapshot = snapshot
    this.failures = failures
  }
}

export enum CanonicalInventoryRuntimeFailure {
  CACHE_CORRUPTED = 'cacheCorrupted',
  FILE_METADATA_UNAVAILABLE = 'fileMetadataUnavailable',
  HASH_UNAVAILABLE = 'hashUnavailable',
  CANCELLED = 'cancelled',
  UNKNOWN = 'unknown'
}

// =============================================================================
// INVENTORY RUNTIME BUILDER
// =============================================================================

export class CanonicalInventoryRuntimeBuilder {
  private snapshotsByScope: Map<string, CanonicalInventoryRuntimeSnapshot> = new Map()

  existingSnapshot(
    syncRunID: string,
    nodeRole: CanonicalInventoryRuntimeNodeRole,
    sourceKind: CanonicalInventoryRuntimeSourceKind
  ): CanonicalInventoryRuntimeSnapshot | undefined {
    return this.snapshotsByScope.get(
      CanonicalInventoryRuntimeBuilder.scopeKey(syncRunID, nodeRole, sourceKind)
    )
  }

  remember(snapshot: CanonicalInventoryRuntimeSnapshot) {
    this.snapshotsByScope.set(
      CanonicalInventoryRuntimeBuilder.scopeKey(snapshot.syncRunID, snapshot.nodeRole, snapshot.sourceKind),
      snapshot
    )
  }

  reusedSnapshot(snapshot: CanonicalInventoryRuntimeSnapshot): CanonicalInventoryRuntimeSnapshot {
    const reused = { ...snapshot }
    reused.reusedWithinTick = true
    return reused
  }

  duplicateDetectedSnapshot(snapshot: CanonicalInventoryRuntimeSnapshot): CanonicalInventoryRuntimeSnapshot {
    const duplicate = { ...snapshot, diagnostics: { ...snapshot.diagnostics } }
    duplicate.diagnostics.duplicateBuildCount++
    return duplicate
  }

  reset() {
    this.snapshotsByScope.clear()
  }

  private static scopeKey(
    syncRunID: string,
    nodeRole: CanonicalInventoryRuntimeNodeRole,
    sourceKind: CanonicalInventoryRuntimeSourceKind
  ): string {
    return nodeRole + '|' + sourceKind + '|' + syncRunID
  }
}

// =============================================================================
// INVENTORY REPORT
// =============================================================================

export class CanonicalInventoryRuntimeReport {
  syncRunID: string
  nodeRole: CanonicalInventoryRuntimeNodeRole
  buildDurationMs: number
  scanDurationMs: number
  hashDurationMs: number
  cacheHitCount: number
  cacheMissCount: number
  cacheStaleCount: number
  duplicateBuildCount: number
  mainActorHashBlockedCount: number
  mainActorScanBlockedCount: number
  inventoryObjectCounts: CanonicalInventoryObjectCounts
  redacted: boolean

  constructor(
    syncRunID: string,
    nodeRole: CanonicalInventoryRuntimeNodeRole,
    buildDurationMs: number,
    scanDurationMs: number,
    hashDurationMs: number,
    cacheHitCount: number,
    cacheMissCount: number,
    cacheStaleCount: number,
    duplicateBuildCount: number,
    mainActorHashBlockedCount: number,
    mainActorScanBlockedCount: number,
    inventoryObjectCounts: CanonicalInventoryObjectCounts,
    redacted: boolean
  ) {
    this.syncRunID = syncRunID
    this.nodeRole = nodeRole
    this.buildDurationMs = buildDurationMs
    this.scanDurationMs = scanDurationMs
    this.hashDurationMs = hashDurationMs
    this.cacheHitCount = cacheHitCount
    this.cacheMissCount = cacheMissCount
    this.cacheStaleCount = cacheStaleCount
    this.duplicateBuildCount = duplicateBuildCount
    this.mainActorHashBlockedCount = mainActorHashBlockedCount
    this.mainActorScanBlockedCount = mainActorScanBlockedCount
    this.inventoryObjectCounts = inventoryObjectCounts
    this.redacted = redacted
  }
}

export class CanonicalInventoryRuntimeReportExporter {
  static report(snapshot: CanonicalInventoryRuntimeSnapshot): CanonicalInventoryRuntimeReport {
    const ms = Math.max(0, Math.round(
      (snapshot.buildEndedAt.getTime() - snapshot.buildStartedAt.getTime())
    ))
    return new CanonicalInventoryRuntimeReport(
      snapshot.syncRunID, snapshot.nodeRole, ms,
      snapshot.diagnostics.scanDurationMs, snapshot.diagnostics.hashDurationMs,
      snapshot.diagnostics.checksumCacheHitCount, snapshot.diagnostics.checksumCacheMissCount,
      snapshot.diagnostics.checksumCacheStaleCount, snapshot.diagnostics.duplicateBuildCount,
      snapshot.diagnostics.mainActorHashBlockedCount, snapshot.diagnostics.mainActorScanBlockedCount,
      snapshot.objectCounts, snapshot.redacted
    )
  }

  static diagnosticsSummary(snapshot: CanonicalInventoryRuntimeSnapshot): string {
    const report = CanonicalInventoryRuntimeReportExporter.report(snapshot)
    return [
      'syncRunID=' + report.syncRunID,
      'nodeRole=' + report.nodeRole,
      'buildDurationMs=' + report.buildDurationMs,
      'scanDurationMs=' + report.scanDurationMs,
      'hashDurationMs=' + report.hashDurationMs,
      'cacheHitCount=' + report.cacheHitCount,
      'cacheMissCount=' + report.cacheMissCount,
      'cacheStaleCount=' + report.cacheStaleCount,
      'duplicateBuildCount=' + report.duplicateBuildCount,
      'mainActorHashBlockedCount=' + report.mainActorHashBlockedCount,
      'mainActorScanBlockedCount=' + report.mainActorScanBlockedCount,
      'recordingMetadataCount=' + report.inventoryObjectCounts.recordingMetadataCount,
      'libraryFolderCount=' + report.inventoryObjectCounts.libraryFolderCount,
      'libraryItemCount=' + report.inventoryObjectCounts.libraryItemCount,
      'artifactCount=' + report.inventoryObjectCounts.artifactCount,
      'audioDescriptorCount=' + report.inventoryObjectCounts.audioDescriptorCount,
      'redacted=' + report.redacted
    ].join(',')
  }
}

// =============================================================================
// CHECKSUM CACHE
// =============================================================================

export class CanonicalChecksumCacheKey {
  logicalToken: string
  byteSize: number
  modifiedAtEpochMs: number
  hashAlgorithm: string
  schemaVersion: number
  nodeRole: CanonicalInventoryRuntimeNodeRole

  constructor(
    logicalToken: string,
    byteSize: number,
    modifiedAtEpochMs: number,
    hashAlgorithm: string,
    schemaVersion: number,
    nodeRole: CanonicalInventoryRuntimeNodeRole
  ) {
    this.logicalToken = logicalToken
    this.byteSize = byteSize
    this.modifiedAtEpochMs = modifiedAtEpochMs
    this.hashAlgorithm = hashAlgorithm
    this.schemaVersion = schemaVersion
    this.nodeRole = nodeRole
  }
}

export class CanonicalChecksumCacheRecord {
  key: CanonicalChecksumCacheKey
  sha256: string
  computedAt: Date

  constructor(key: CanonicalChecksumCacheKey, sha256: string, computedAt: Date) {
    this.key = key
    this.sha256 = sha256
    this.computedAt = computedAt
  }
}

export enum CanonicalChecksumCacheEvent {
  HIT = 'hit',
  MISS = 'miss',
  STALE = 'stale',
  ERROR = 'error'
}

export class CanonicalChecksumCacheResult {
  sha256: string | null
  byteSize: number
  modifiedAt: Date
  event: CanonicalChecksumCacheEvent
  hashComputed: boolean
  hashUnavailable: boolean
  failure: CanonicalInventoryRuntimeFailure | null
  hashDurationMs: number

  constructor(
    sha256: string | null,
    byteSize: number,
    modifiedAt: Date,
    event: CanonicalChecksumCacheEvent,
    hashComputed: boolean,
    hashUnavailable: boolean,
    failure: CanonicalInventoryRuntimeFailure | null,
    hashDurationMs: number
  ) {
    this.sha256 = sha256
    this.byteSize = byteSize
    this.modifiedAt = modifiedAt
    this.event = event
    this.hashComputed = hashComputed
    this.hashUnavailable = hashUnavailable
    this.failure = failure
    this.hashDurationMs = hashDurationMs
  }

  get redactedHashPrefix(): string | null {
    return this.sha256 ? this.sha256.substring(0, 12) : null
  }
}

export class CanonicalChecksumCacheStore {
  private recordsByToken: Map<string, CanonicalChecksumCacheRecord> = new Map()
  private cacheCorrupted: boolean = false

  checksum(
    logicalToken: string,
    byteSize: number,
    modifiedAt: Date,
    nodeRole: CanonicalInventoryRuntimeNodeRole,
    configuration: CanonicalInventoryRuntimeConfiguration = new CanonicalInventoryRuntimeConfiguration()
  ): CanonicalChecksumCacheResult {
    if (byteSize <= 0) {
      return new CanonicalChecksumCacheResult(null, 0, new Date(0),
        CanonicalChecksumCacheEvent.ERROR, false, true,
        CanonicalInventoryRuntimeFailure.FILE_METADATA_UNAVAILABLE, 0)
    }
    const safeToken = logicalToken
    const epochMs = Math.round(modifiedAt.getTime())
    const key = new CanonicalChecksumCacheKey(
      safeToken, byteSize, epochMs, configuration.hashAlgorithm,
      configuration.checksumSchemaVersion, nodeRole
    )

    if (configuration.persistentChecksumCacheEnabled) {
      const existing = this.recordsByToken.get(safeToken)
      if (existing && existing.key.byteSize === key.byteSize &&
        existing.key.modifiedAtEpochMs === key.modifiedAtEpochMs) {
        return new CanonicalChecksumCacheResult(
          existing.sha256, key.byteSize, modifiedAt,
          CanonicalChecksumCacheEvent.HIT, false, false, null, 0
        )
      }
    }

    const event: CanonicalChecksumCacheEvent = this.recordsByToken.has(safeToken)
      ? CanonicalChecksumCacheEvent.STALE : CanonicalChecksumCacheEvent.MISS
    const hashStartedAt = Date.now()

    const sha256 = CanonicalChecksumCacheStore.sha256String('inventory:' + safeToken + ':' + byteSize + ':' + epochMs)
    const hashDurationMs = Math.max(0, Math.round(Date.now() - hashStartedAt))

    const record = new CanonicalChecksumCacheRecord(key, sha256.value, new Date())
    if (configuration.persistentChecksumCacheEnabled) {
      this.recordsByToken.set(safeToken, record)
    }

    return new CanonicalChecksumCacheResult(
      sha256.value, key.byteSize, modifiedAt, event, true, false,
      this.cacheCorrupted ? CanonicalInventoryRuntimeFailure.CACHE_CORRUPTED : null,
      hashDurationMs
    )
  }

  reset() {
    this.recordsByToken.clear()
    this.cacheCorrupted = false
  }

  private static sha256String(data: string): CanonicalHash {
    return CanonicalHash.sha256OfString(data)
  }
}
