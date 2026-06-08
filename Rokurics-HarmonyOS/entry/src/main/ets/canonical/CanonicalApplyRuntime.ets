/**
 * CanonicalApplyRuntime — apply runtime: gate, execution, executor registry.
 * Ported from Rokurics-Apple (CanonicalApplyRuntime.swift).
 * Manages apply gateway evaluation, execution, and executor registration.
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalManifest, CanonicalRecordingObject,
  CanonicalArtifact, CanonicalArtifactKind, CanonicalSyncRuntimeMode,
  CanonicalSyncRuntimeDiagnosticKind, CanonicalSyncRuntimeDiagnostic,
  nilIfEmpty, hashPrefix
} from './CanonicalCore'
import {
  CanonicalApplyAction, CanonicalApplyActionKind, CanonicalApplyTarget,
  CanonicalApplyFailureReason, CanonicalApplyPlan, CanonicalConflictRecord
} from './CanonicalApplyPlan'
import {
  CanonicalConflictResolver, CanonicalConflictResolverReport
} from './CanonicalConflictResolver'

// ── Forward Declarations for Types from Other Files ──

export interface CanonicalFileStorePort {
  resolve(reference: CanonicalFileReference): object
  read(request: CanonicalFileReadRequest): object
  write(intent: CanonicalFileWriteIntent): object
  markTombstone(reference: CanonicalFileReference, reason: string | null): object
  contains(reference: CanonicalFileReference): object
}

export class CanonicalRootToken {
  rawValue: string

  constructor(rawValue: string) {
    this.rawValue = nilIfEmpty(rawValue.trim()) ?? 'canonical-root'
  }
}

export class CanonicalFileReference {
  rootToken: CanonicalRootToken
  logicalPathToken: string
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null

  constructor(
    rootToken: CanonicalRootToken,
    logicalPathToken: string,
    artifactID: string | null = null,
    artifactKind: CanonicalArtifactKind | null = null
  ) {
    this.rootToken = rootToken
    this.logicalPathToken = logicalPathToken
    this.artifactID = artifactID
    this.artifactKind = artifactKind
  }
}

export enum CanonicalFilePurpose {
  METADATA_BLOB = 'metadataBlob',
  ARTIFACT_BYTES = 'artifactBytes',
  GENERATED_ARTIFACT = 'generatedArtifact',
  TOMBSTONE_MARKER = 'tombstoneMarker'
}

export class CanonicalFileWriteIntent {
  reference: CanonicalFileReference
  bytes: Uint8Array
  purpose: CanonicalFilePurpose
  expectedContentHash: CanonicalHash | null
  expectedByteSize: number
  conflictPolicy: string
  metadataBlob: CanonicalMetadataBlob | null

  constructor(
    reference: CanonicalFileReference,
    bytes: Uint8Array,
    purpose: CanonicalFilePurpose,
    expectedContentHash: CanonicalHash | null = null,
    expectedByteSize: number = 0,
    conflictPolicy: string = 'replace',
    metadataBlob: CanonicalMetadataBlob | null = null
  ) {
    this.reference = reference
    this.bytes = bytes
    this.purpose = purpose
    this.expectedContentHash = expectedContentHash
    this.expectedByteSize = expectedByteSize
    this.conflictPolicy = conflictPolicy
    this.metadataBlob = metadataBlob
  }
}

export class CanonicalFileReadRequest {
  reference: CanonicalFileReference

  constructor(reference: CanonicalFileReference) {
    this.reference = reference
  }
}

export class CanonicalFileReadResult {
  bytes: Uint8Array
  contentHash: CanonicalHash | null
  byteSize: number

  constructor(bytes: Uint8Array, contentHash: CanonicalHash | null, byteSize: number) {
    this.bytes = bytes
    this.contentHash = contentHash
    this.byteSize = byteSize
  }
}

export enum CanonicalFileWriteDisposition {
  CREATED = 'created',
  REPLACED = 'replaced',
  CONFLICT = 'conflict',
  UNCHANGED = 'unchanged'
}

export class CanonicalFileWriteResult {
  contentHash: CanonicalHash | null
  byteSize: number
  disposition: CanonicalFileWriteDisposition

  constructor(contentHash: CanonicalHash | null, byteSize: number, disposition: CanonicalFileWriteDisposition) {
    this.contentHash = contentHash
    this.byteSize = byteSize
    this.disposition = disposition
  }
}

export class CanonicalMetadataBlob {
  fields: Record<string, string>

  constructor(fields: Record<string, string>) {
    this.fields = fields
  }
}

export class InMemoryCanonicalFileStore {
  private rootBindings: Record<string, string>
  private contents: Map<string, Uint8Array> = new Map()
  private tombstoneEntries: Set<string> = new Set()

  constructor(rootBindings: Record<string, string>) {
    this.rootBindings = rootBindings
  }

  resolve(reference: CanonicalFileReference): { path: string } {
    return { path: reference.rootToken.rawValue + '/' + reference.logicalPathToken }
  }

  async read(request: CanonicalFileReadRequest): Promise<CanonicalFileReadResult> {
    const key = request.reference.rootToken.rawValue + '/' + request.reference.logicalPathToken
    const bytes = this.contents.get(key)
    if (!bytes) {
      throw new Error('file not found: ' + key)
    }
    const hash = InMemoryCanonicalFileStore.hash(bytes, 'sha256')
    return new CanonicalFileReadResult(bytes, hash, bytes.length)
  }

  async write(intent: CanonicalFileWriteIntent): Promise<CanonicalFileWriteResult> {
    const key = intent.reference.rootToken.rawValue + '/' + intent.reference.logicalPathToken
    this.contents.set(key, intent.bytes)
    const hash = InMemoryCanonicalFileStore.hash(intent.bytes, 'sha256')
    return new CanonicalFileWriteResult(hash, intent.bytes.length, CanonicalFileWriteDisposition.CREATED)
  }

  async markTombstone(reference: CanonicalFileReference, reason: string | null): Promise<CanonicalFileWriteResult> {
    const key = reference.rootToken.rawValue + '/' + reference.logicalPathToken
    this.tombstoneEntries.add(key)
    return new CanonicalFileWriteResult(null, 0, CanonicalFileWriteDisposition.CREATED)
  }

  async contains(reference: CanonicalFileReference): Promise<boolean> {
    const key = reference.rootToken.rawValue + '/' + reference.logicalPathToken
    return this.contents.has(key) || this.tombstoneEntries.has(key)
  }

  static hash(data: Uint8Array, policy: string): CanonicalHash | null {
    return CanonicalHash.sha256OfString(bufferToHexString(data))
  }

  static async sha256(data: Uint8Array): Promise<CanonicalHash> {
    return CanonicalHash.sha256OfString(bufferToHexString(data))
  }
}

function bufferToHexString(data: Uint8Array): string {
  let hex = ''
  for (let i = 0; i < data.length; i++) {
    hex += data[i].toString(16).padStart(2, '0')
  }
  return hex
}

// ── Forward: Library Sync Plan ──

export interface CanonicalLibrarySyncPlan {
  conflicts: object[]
  applyActions: CanonicalApplyAction[]
}

// ── Forward: Cutover Action Kinds ──

export enum CanonicalRecordingMetadataCutoverActionKind {
  APPLY = 'apply',
  SEND = 'send'
}

export enum CanonicalCutoverFailure {
  PRECONDITION_MISMATCH = 'preconditionMismatch',
  POSTCONDITION_MISMATCH = 'postconditionMismatch',
  APPLY_FAILURE_BEFORE_COMMIT = 'applyFailureBeforeCommit',
  APPLY_FAILURE_AFTER_PARTIAL_COMMIT = 'applyFailureAfterPartialCommit'
}

// ── Forward: Recording Metadata Cutover Types ──

export interface CanonicalRecordingMetadataCutoverCandidate {
  action: CanonicalApplyAction
  localObject: CanonicalRecordingObject | null
  peerObject: CanonicalRecordingObject | null
  rollbackCheckpointID: string
  cutoverActionKind: CanonicalRecordingMetadataCutoverActionKind | null
}

export interface CanonicalRecordingMetadataProductionCommitResult {
  committed: boolean
  preconditionVerified: boolean
  postconditionVerified: boolean
  partialCommit: boolean
  reason: string
  failureKind: CanonicalCutoverFailure | null
}

export interface CanonicalRecordingMetadataCutoverExecutor {
  commitRecordingMetadata(candidate: CanonicalRecordingMetadataCutoverCandidate): object
  rollbackRecordingMetadata(candidate: CanonicalRecordingMetadataCutoverCandidate, reason: CanonicalCutoverFailure): object
}

export interface CanonicalRecordingMetadataRollbackResult {
  succeeded: boolean
  fatal: boolean
}

// ── Forward: Library Metadata Cutover Types ──

export interface CanonicalLibraryMetadataCutoverCandidate {
  action: CanonicalApplyAction
}

export interface CanonicalLibraryMetadataCutoverResult {
  committed: boolean
  preconditionVerified: boolean
  postconditionVerified: boolean
  reason: string
  failureKind: CanonicalCutoverFailure | null
}

export interface CanonicalLibraryMetadataCutoverExecutor {
  commitLibraryMetadata(candidate: object): object
  rollbackLibraryMetadata(candidate: object, reason: CanonicalCutoverFailure): object
}

export interface CanonicalLibraryMetadataRollbackResult {
  succeeded: boolean
  fatal: boolean
}

// ── Forward: Generated Artifact Cutover Types ──

export interface CanonicalGeneratedArtifactCutoverCandidate {
  action: CanonicalApplyAction
}

export interface CanonicalGeneratedArtifactCutoverExecutor {
  commitGeneratedArtifact(candidate: object): object
  rollbackGeneratedArtifact(candidate: object, reason: CanonicalCutoverFailure): object
}

export interface CanonicalGeneratedArtifactRollbackResult {
  succeeded: boolean
  fatal: boolean
}

// ── Forward: Tombstone Conflict Cutover Types ──

export interface CanonicalTombstoneConflictCandidate {
  action: CanonicalApplyAction
}

export interface CanonicalTombstoneConflictCutoverExecutor {
  commitTombstoneConflict(candidate: object): object
  rollbackTombstoneConflict(candidate: object, reason: CanonicalCutoverFailure): object
}

export interface CanonicalTombstoneConflictRollbackResult {
  succeeded: boolean
  fatal: boolean
}

// ── Production Redaction Helpers ──

export class CanonicalProductionRedaction {
  static safeIdentifier(value: string | null | undefined, fallback: string = 'unknown'): string {
    const trimmed = (value ?? fallback).trim()
    return trimmed.length > 0 ? trimmed : fallback
  }

  static safeDiagnosticText(value: string | null | undefined): string | null {
    const trimmed = value?.trim()
    if (!trimmed || trimmed.length === 0) {
      return null
    }
    return trimmed
  }

  static hashPrefix(value: string): string | null {
    const trimmed = value.trim()
    return trimmed.length >= 8 ? trimmed.substring(0, 12) : null
  }
}

// ── Canonical Rollback Result ──

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

// =============================================================================
// Canonical Apply Execution Status
// =============================================================================

export enum CanonicalApplyExecutionStatus {
  APPLIED = 'applied',
  SENT = 'sent',
  NO_OP = 'noOp',
  CONFLICT_RECORDED = 'conflictRecorded',
  DEFERRED_UNSUPPORTED = 'deferredUnsupported',
  FAILED = 'failed'
}

// ── Canonical Apply Execution Record ──

export class CanonicalApplyExecutionRecord {
  actionID: string
  kind: CanonicalApplyActionKind
  target: CanonicalApplyTarget
  status: CanonicalApplyExecutionStatus
  contentHashPrefix: string | null
  byteSize: number | null
  failure: CanonicalApplyFailureReason | null
  detail: string | null

  constructor(
    actionID: string,
    kind: CanonicalApplyActionKind,
    target: CanonicalApplyTarget,
    status: CanonicalApplyExecutionStatus,
    contentHashPrefix: string | null = null,
    byteSize: number | null = null,
    failure: CanonicalApplyFailureReason | null = null,
    detail: string | null = null
  ) {
    this.actionID = actionID
    this.kind = kind
    this.target = target
    this.status = status
    this.contentHashPrefix = contentHashPrefix
    this.byteSize = byteSize
    this.failure = failure
    this.detail = detail
  }

  get id(): string {
    return this.actionID
  }
}

// ── Canonical Apply Execution Report ──

export class CanonicalApplyExecutionReport {
  records: CanonicalApplyExecutionRecord[]
  conflictReport: CanonicalConflictResolverReport
  appliedCount: number
  failedCount: number

  constructor(
    records: CanonicalApplyExecutionRecord[],
    conflictReport: CanonicalConflictResolverReport,
    appliedCount: number,
    failedCount: number
  ) {
    this.records = records
    this.conflictReport = conflictReport
    this.appliedCount = appliedCount
    this.failedCount = failedCount
  }
}

// =============================================================================
// Canonical Apply Runtime Context
// =============================================================================

export class CanonicalApplyRuntimeContext {
  localManifest: CanonicalManifest
  peerManifest: CanonicalManifest
  localFileStore: CanonicalFileStorePort
  peerFileStore: CanonicalFileStorePort
  localMetadataRoot: CanonicalRootToken
  peerMetadataRoot: CanonicalRootToken
  localGeneratedRoot: CanonicalRootToken
  peerGeneratedRoot: CanonicalRootToken

  constructor(
    localManifest: CanonicalManifest,
    peerManifest: CanonicalManifest,
    localFileStore: CanonicalFileStorePort,
    peerFileStore: CanonicalFileStorePort,
    localMetadataRoot: CanonicalRootToken,
    peerMetadataRoot: CanonicalRootToken,
    localGeneratedRoot: CanonicalRootToken,
    peerGeneratedRoot: CanonicalRootToken
  ) {
    this.localManifest = localManifest
    this.peerManifest = peerManifest
    this.localFileStore = localFileStore
    this.peerFileStore = peerFileStore
    this.localMetadataRoot = localMetadataRoot
    this.peerMetadataRoot = peerMetadataRoot
    this.localGeneratedRoot = localGeneratedRoot
    this.peerGeneratedRoot = peerGeneratedRoot
  }
}

// =============================================================================
// Canonical Apply Runtime Error
// =============================================================================

export enum CanonicalApplyRuntimeError {
  MISSING_SOURCE_OBJECT = 'missingSourceObject',
  MISSING_SOURCE_ARTIFACT = 'missingSourceArtifact',
  MISSING_LOGICAL_PATH_TOKEN = 'missingLogicalPathToken',
  HASH_OR_SIZE_MISMATCH = 'hashOrSizeMismatch'
}

// =============================================================================
// Canonical Apply Runtime Mode
// =============================================================================

export enum CanonicalApplyRuntimeMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  NO_COMMIT = 'noCommit',
  TEST_ROOT_APPLY = 'testRootApply',
  PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK = 'productionRootApplyWithLegacyFallback',
  BLOCKED = 'blocked'
}

export function canonicalApplyRuntimeModeExecutesCommit(mode: CanonicalApplyRuntimeMode): boolean {
  return mode === CanonicalApplyRuntimeMode.TEST_ROOT_APPLY ||
    mode === CanonicalApplyRuntimeMode.PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK
}

export function canonicalApplyRuntimeModeSyncDiagnosticMode(mode: CanonicalApplyRuntimeMode): CanonicalSyncRuntimeMode {
  switch (mode) {
    case CanonicalApplyRuntimeMode.DISABLED:
      return CanonicalSyncRuntimeMode.DISABLED
    case CanonicalApplyRuntimeMode.DIAGNOSTICS_ONLY:
      return CanonicalSyncRuntimeMode.DIAGNOSTICS_ONLY
    case CanonicalApplyRuntimeMode.NO_COMMIT:
      return CanonicalSyncRuntimeMode.CANONICAL_PLAN_NO_COMMIT
    case CanonicalApplyRuntimeMode.TEST_ROOT_APPLY:
    case CanonicalApplyRuntimeMode.PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK:
      return CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK
    case CanonicalApplyRuntimeMode.BLOCKED:
      return CanonicalSyncRuntimeMode.BLOCKED
  }
}

// =============================================================================
// Canonical Apply Runtime Domain
// =============================================================================

export enum CanonicalApplyRuntimeDomain {
  RECORDING_METADATA = 'recordingMetadata',
  LIBRARY_METADATA = 'libraryMetadata',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  RECORDING_EXISTENCE = 'recordingExistence',
  AUDIO_UPLOAD = 'audioUpload'
}

export enum CanonicalApplyRuntimeUnsupportedDomain {
  AUDIO_UPLOAD = 'audioUpload',
  READ_SIDE_CUTOVER = 'readSideCutover',
  RESOURCE_MOVE = 'resourceMove',
  STANDALONE_NOTE_CONTENT = 'standaloneNoteContent',
  PERMANENT_DELETE = 'permanentDelete',
  TOMBSTONE_GARBAGE_COLLECTION = 'tombstoneGarbageCollection'
}

// ── Canonical Apply Runtime Blocker ──

export enum CanonicalApplyRuntimeBlocker {
  DISABLED_MODE = 'disabledMode',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  NO_COMMIT = 'noCommit',
  BLOCKED_MODE = 'blockedMode',
  CANONICAL_PLAN_AUTHORITY_UNAVAILABLE = 'canonicalPlanAuthorityUnavailable',
  INVENTORY_SNAPSHOT_INVALID = 'inventorySnapshotInvalid',
  APPLY_PLAN_INVALID = 'applyPlanInvalid',
  ENABLED_DOMAINS_MISSING = 'enabledDomainsMissing',
  DOMAIN_NOT_ENABLED = 'domainNotEnabled',
  MISSING_EXECUTOR = 'missingExecutor',
  DRY_RUN_ONLY_EXECUTOR = 'dryRunOnlyExecutor',
  ROOT_BOUND_APPLY_PORT_UNAVAILABLE = 'rootBoundApplyPortUnavailable',
  ROLLBACK_UNAVAILABLE = 'rollbackUnavailable',
  POSTCONDITION_UNAVAILABLE = 'postconditionUnavailable',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  AUDIO_ACTION_BLOCKED = 'audioActionBlocked',
  RESOURCE_MOVE_BLOCKED = 'resourceMoveBlocked',
  STANDALONE_NOTE_CONTENT_WRITE_BLOCKED = 'standaloneNoteContentWriteBlocked',
  PERMANENT_DELETE_BLOCKED = 'permanentDeleteBlocked',
  TOMBSTONE_GARBAGE_COLLECTION_BLOCKED = 'tombstoneGarbageCollectionBlocked',
  DIAGNOSTICS_NOT_REDACTED = 'diagnosticsNotRedacted',
  RELEASE_DEFAULT_PRODUCTION_APPLY_BLOCKED = 'releaseDefaultProductionApplyBlocked',
  DEBUG_INTERNAL_APPROVAL_MISSING = 'debugInternalApprovalMissing',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  READ_PATH_NOT_LEGACY = 'readPathNotLegacy',
  UNSUPPORTED_DOMAIN = 'unsupportedDomain',
  ROLLBACK_FAILURE_FATAL = 'rollbackFailureFatal',
  ACTION_FAILED = 'actionFailed'
}

// ── Canonical Apply Runtime Policy ──

export class CanonicalApplyRuntimePolicy {
  debugInternalBuild: boolean
  ownerApproved: boolean
  releaseDefaultBuild: boolean
  legacyFallbackAvailable: boolean
  diagnosticsRedacted: boolean
  runtimeSwitchEnabled: boolean
  readPathLegacy: boolean
  enabledDomains: CanonicalApplyRuntimeDomain[]
  allowConflictRecordAction: boolean
  allowTestRootApply: boolean

  constructor(
    debugInternalBuild: boolean = false,
    ownerApproved: boolean = false,
    releaseDefaultBuild: boolean = true,
    legacyFallbackAvailable: boolean = true,
    diagnosticsRedacted: boolean = true,
    runtimeSwitchEnabled: boolean = false,
    readPathLegacy: boolean = true,
    enabledDomains: CanonicalApplyRuntimeDomain[] = [],
    allowConflictRecordAction: boolean = true,
    allowTestRootApply: boolean = true
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApproved = ownerApproved
    this.releaseDefaultBuild = releaseDefaultBuild
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.diagnosticsRedacted = diagnosticsRedacted
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.readPathLegacy = readPathLegacy
    this.enabledDomains = [...new Set(enabledDomains)].sort()
    this.allowConflictRecordAction = allowConflictRecordAction
    this.allowTestRootApply = allowTestRootApply
  }
}

// ── Canonical Apply Runtime Configuration ──

export class CanonicalApplyRuntimeConfiguration {
  mode: CanonicalApplyRuntimeMode
  policy: CanonicalApplyRuntimePolicy

  constructor(
    mode: CanonicalApplyRuntimeMode = CanonicalApplyRuntimeMode.DISABLED,
    policy: CanonicalApplyRuntimePolicy = new CanonicalApplyRuntimePolicy()
  ) {
    this.mode = mode
    this.policy = policy
  }

  static readonly DISABLED = new CanonicalApplyRuntimeConfiguration()
}

// =============================================================================
// Canonical Apply Runtime Gate State / Result
// =============================================================================

export enum CanonicalApplyRuntimeGateState {
  LEGACY_OWNER = 'legacyOwner',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  NO_COMMIT = 'noCommit',
  ALLOWED = 'allowed',
  BLOCKED = 'blocked'
}

export class CanonicalApplyRuntimeGateResult {
  state: CanonicalApplyRuntimeGateState
  blockers: CanonicalApplyRuntimeBlocker[]
  mode: CanonicalApplyRuntimeMode

  constructor(
    state: CanonicalApplyRuntimeGateState,
    blockers: CanonicalApplyRuntimeBlocker[],
    mode: CanonicalApplyRuntimeMode
  ) {
    this.state = state
    this.blockers = [...new Set(blockers)].sort()
    this.mode = mode
  }

  get isAllowed(): boolean {
    return this.blockers.length === 0 && this.state === CanonicalApplyRuntimeGateState.ALLOWED
  }

  get executesCommit(): boolean {
    return this.isAllowed && canonicalApplyRuntimeModeExecutesCommit(this.mode)
  }

  get usesLegacyFallback(): boolean {
    return !this.executesCommit
  }
}

// =============================================================================
// Canonical Apply Runtime Owner Context
// =============================================================================

export class CanonicalApplyRuntimeOwnerContext {
  configuration: CanonicalApplyRuntimeConfiguration
  applyPlan: CanonicalApplyPlan
  libraryPlan: CanonicalLibrarySyncPlan | null
  localManifest: CanonicalManifest | null
  peerManifest: CanonicalManifest | null
  inventorySnapshotValid: boolean
  canonicalPlanAuthorityAllowed: boolean
  legacyFallbackAvailable: boolean
  registry: CanonicalApplyRuntimeExecutorRegistry
  syncRunID: string | null

  constructor(
    configuration: CanonicalApplyRuntimeConfiguration,
    applyPlan: CanonicalApplyPlan,
    libraryPlan: CanonicalLibrarySyncPlan | null = null,
    localManifest: CanonicalManifest | null = null,
    peerManifest: CanonicalManifest | null = null,
    inventorySnapshotValid: boolean = false,
    canonicalPlanAuthorityAllowed: boolean = false,
    legacyFallbackAvailable: boolean = true,
    registry: CanonicalApplyRuntimeExecutorRegistry = new CanonicalApplyRuntimeExecutorRegistry(),
    syncRunID: string | null = null
  ) {
    this.configuration = configuration
    this.applyPlan = applyPlan
    this.libraryPlan = libraryPlan
    this.localManifest = localManifest
    this.peerManifest = peerManifest
    this.inventorySnapshotValid = inventorySnapshotValid
    this.canonicalPlanAuthorityAllowed = canonicalPlanAuthorityAllowed
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.registry = registry
    this.syncRunID = syncRunID
  }

  get allActions(): CanonicalApplyAction[] {
    const seen = new Set<string>()
    const all = [...this.applyPlan.actions, ...(this.libraryPlan?.applyActions ?? [])]
    return all.filter(a => {
      if (seen.has(a.actionID)) {
        return false
      }
      seen.add(a.actionID)
      return true
    })
  }
}

// =============================================================================
// Canonical Apply Runtime Executor Context
// =============================================================================

export class CanonicalApplyRuntimeExecutorContext {
  action: CanonicalApplyAction
  applyPlan: CanonicalApplyPlan
  libraryPlan: CanonicalLibrarySyncPlan | null
  localManifest: CanonicalManifest
  peerManifest: CanonicalManifest
  syncRunID: string | null

  constructor(
    action: CanonicalApplyAction,
    applyPlan: CanonicalApplyPlan,
    libraryPlan: CanonicalLibrarySyncPlan | null,
    localManifest: CanonicalManifest,
    peerManifest: CanonicalManifest,
    syncRunID: string | null = null
  ) {
    this.action = action
    this.applyPlan = applyPlan
    this.libraryPlan = libraryPlan
    this.localManifest = localManifest
    this.peerManifest = peerManifest
    this.syncRunID = syncRunID
  }
}

// =============================================================================
// Canonical Apply Runtime Executor Result
// =============================================================================

export class CanonicalApplyRuntimeExecutorResult {
  actionID: string
  objectID: string
  domain: CanonicalApplyRuntimeDomain
  committed: boolean
  preconditionVerified: boolean
  postconditionVerified: boolean
  rollbackAttempted: boolean
  rollbackSucceeded: boolean | null
  rollbackFatal: boolean
  failureReason: string | null
  detail: string | null

  constructor(
    actionID: string,
    objectID: string,
    domain: CanonicalApplyRuntimeDomain,
    committed: boolean,
    preconditionVerified: boolean = true,
    postconditionVerified: boolean = true,
    rollbackAttempted: boolean = false,
    rollbackSucceeded: boolean | null = null,
    rollbackFatal: boolean = false,
    failureReason: string | null = null,
    detail: string | null = null
  ) {
    this.actionID = CanonicalProductionRedaction.safeIdentifier(actionID, domain)
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'canonical-object')
    this.domain = domain
    this.committed = committed
    this.preconditionVerified = preconditionVerified
    this.postconditionVerified = postconditionVerified
    this.rollbackAttempted = rollbackAttempted
    this.rollbackSucceeded = rollbackSucceeded
    this.rollbackFatal = rollbackFatal
    this.failureReason = CanonicalProductionRedaction.safeDiagnosticText(failureReason)
    this.detail = CanonicalProductionRedaction.safeDiagnosticText(detail)
  }

  get succeeded(): boolean {
    return this.committed && this.preconditionVerified && this.postconditionVerified && !this.rollbackFatal
  }

  static success(
    action: CanonicalApplyAction,
    domain: CanonicalApplyRuntimeDomain,
    detail: string | null = null
  ): CanonicalApplyRuntimeExecutorResult {
    return new CanonicalApplyRuntimeExecutorResult(
      action.actionID, action.target.objectID, domain, true, true, true,
      false, null, false, null, detail
    )
  }

  static failure(
    action: CanonicalApplyAction,
    domain: CanonicalApplyRuntimeDomain,
    preconditionVerified: boolean = true,
    postconditionVerified: boolean = true,
    rollbackAttempted: boolean = false,
    rollbackSucceeded: boolean | null = null,
    rollbackFatal: boolean = false,
    reason: string
  ): CanonicalApplyRuntimeExecutorResult {
    return new CanonicalApplyRuntimeExecutorResult(
      action.actionID, action.target.objectID, domain, false,
      preconditionVerified, postconditionVerified,
      rollbackAttempted, rollbackSucceeded, rollbackFatal, reason
    )
  }
}

// =============================================================================
// Canonical Apply Runtime Executor Entry
// =============================================================================

export class CanonicalApplyRuntimeExecutorEntry {
  domain: CanonicalApplyRuntimeDomain
  dryRunOnly: boolean
  rollbackAvailable: boolean
  postconditionAvailable: boolean
  rootBoundApplyPortAvailable: boolean
  private executeClosure: (context: CanonicalApplyRuntimeExecutorContext) =>
    Promise<CanonicalApplyRuntimeExecutorResult>

  constructor(
    domain: CanonicalApplyRuntimeDomain,
    dryRunOnly: boolean = false,
    rollbackAvailable: boolean = true,
    postconditionAvailable: boolean = true,
    rootBoundApplyPortAvailable: boolean = true,
    execute: (context: CanonicalApplyRuntimeExecutorContext) => Promise<CanonicalApplyRuntimeExecutorResult>
  ) {
    this.domain = domain
    this.dryRunOnly = dryRunOnly
    this.rollbackAvailable = rollbackAvailable
    this.postconditionAvailable = postconditionAvailable
    this.rootBoundApplyPortAvailable = rootBoundApplyPortAvailable
    this.executeClosure = execute
  }

  async execute(context: CanonicalApplyRuntimeExecutorContext): Promise<CanonicalApplyRuntimeExecutorResult> {
    return this.executeClosure(context)
  }

  static unsupportedAudioUpload(): CanonicalApplyRuntimeExecutorEntry {
    return new CanonicalApplyRuntimeExecutorEntry(
      CanonicalApplyRuntimeDomain.AUDIO_UPLOAD,
      true, false, false, false,
      async (context: CanonicalApplyRuntimeExecutorContext) => {
        return CanonicalApplyRuntimeExecutorResult.failure(
          context.action,
          CanonicalApplyRuntimeDomain.AUDIO_UPLOAD,
          true, true, false, null, false,
          CanonicalApplyRuntimeBlocker.AUDIO_ACTION_BLOCKED
        )
      }
    )
  }
}

// =============================================================================
// Canonical Apply Runtime Executor Registry
// =============================================================================

export class CanonicalApplyRuntimeExecutorRegistry {
  private entries: Map<CanonicalApplyRuntimeDomain, CanonicalApplyRuntimeExecutorEntry>

  constructor(entries: CanonicalApplyRuntimeExecutorEntry[] = []) {
    const mapped = new Map<CanonicalApplyRuntimeDomain, CanonicalApplyRuntimeExecutorEntry>()
    for (const entry of entries) {
      mapped.set(entry.domain, entry)
    }
    if (!mapped.has(CanonicalApplyRuntimeDomain.AUDIO_UPLOAD)) {
      mapped.set(CanonicalApplyRuntimeDomain.AUDIO_UPLOAD,
        CanonicalApplyRuntimeExecutorEntry.unsupportedAudioUpload())
    }
    this.entries = mapped
  }

  entryFor(domain: CanonicalApplyRuntimeDomain): CanonicalApplyRuntimeExecutorEntry | null {
    return this.entries.get(domain) ?? null
  }

  contains(domain: CanonicalApplyRuntimeDomain): boolean {
    return this.entries.has(domain)
  }

  adding(entry: CanonicalApplyRuntimeExecutorEntry): CanonicalApplyRuntimeExecutorRegistry {
    const next = new Map(this.entries)
    next.set(entry.domain, entry)
    return new CanonicalApplyRuntimeExecutorRegistry([...next.values()])
  }
}

// =============================================================================
// Canonical Apply Runtime Action Status
// =============================================================================

export enum CanonicalApplyRuntimeActionStatus {
  NOT_EXECUTED = 'notExecuted',
  COMPLETED = 'completed',
  FAILED = 'failed',
  BLOCKED = 'blocked'
}

export class CanonicalApplyRuntimeActionRecord {
  actionID: string
  objectID: string
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null
  actionKind: CanonicalApplyActionKind
  domain: CanonicalApplyRuntimeDomain
  status: CanonicalApplyRuntimeActionStatus
  duplicateLegacySuppressionAllowed: boolean
  blocker: CanonicalApplyRuntimeBlocker | null
  detail: string | null

  constructor(
    action: CanonicalApplyAction,
    domain: CanonicalApplyRuntimeDomain,
    status: CanonicalApplyRuntimeActionStatus,
    duplicateLegacySuppressionAllowed: boolean = false,
    blocker: CanonicalApplyRuntimeBlocker | null = null,
    detail: string | null = null
  ) {
    this.actionID = CanonicalProductionRedaction.safeIdentifier(action.actionID, action.kind)
    this.objectID = CanonicalProductionRedaction.safeIdentifier(action.target.objectID, 'canonical-object')
    this.artifactID = action.target.artifactID
      ? CanonicalProductionRedaction.safeIdentifier(action.target.artifactID, 'artifact')
      : null
    this.artifactKind = action.target.artifactKind
    this.actionKind = action.kind
    this.domain = domain
    this.status = status
    this.duplicateLegacySuppressionAllowed = duplicateLegacySuppressionAllowed
    this.blocker = blocker
    this.detail = CanonicalProductionRedaction.safeDiagnosticText(detail)
  }

  get id(): string {
    return this.actionID
  }
}

// =============================================================================
// Canonical Apply Runtime Report / Result
// =============================================================================

export class CanonicalApplyRuntimeReport {
  mode: CanonicalApplyRuntimeMode
  gateResult: CanonicalApplyRuntimeGateResult
  actionRecords: CanonicalApplyRuntimeActionRecord[]
  legacyFallbackUsed: boolean
  duplicateLegacySuppressedActionIDs: string[]
  fatalBlocker: boolean
  diagnostics: CanonicalSyncRuntimeDiagnostic[]

  constructor(
    mode: CanonicalApplyRuntimeMode,
    gateResult: CanonicalApplyRuntimeGateResult,
    actionRecords: CanonicalApplyRuntimeActionRecord[],
    legacyFallbackUsed: boolean,
    duplicateLegacySuppressedActionIDs: string[],
    fatalBlocker: boolean,
    diagnostics: CanonicalSyncRuntimeDiagnostic[]
  ) {
    this.mode = mode
    this.gateResult = gateResult
    this.actionRecords = actionRecords
    this.legacyFallbackUsed = legacyFallbackUsed
    this.duplicateLegacySuppressedActionIDs = duplicateLegacySuppressedActionIDs
    this.fatalBlocker = fatalBlocker
    this.diagnostics = diagnostics
  }
}

export class CanonicalApplyRuntimeResult {
  mode: CanonicalApplyRuntimeMode
  gateResult: CanonicalApplyRuntimeGateResult
  report: CanonicalApplyRuntimeReport

  constructor(
    mode: CanonicalApplyRuntimeMode,
    gateResult: CanonicalApplyRuntimeGateResult,
    report: CanonicalApplyRuntimeReport
  ) {
    this.mode = mode
    this.gateResult = gateResult
    this.report = report
  }

  get executedActionIDs(): string[] {
    return this.report.actionRecords.filter(r => r.status === CanonicalApplyRuntimeActionStatus.COMPLETED)
      .map(r => r.actionID)
  }

  get duplicateLegacySuppressedActionIDs(): string[] {
    return this.report.duplicateLegacySuppressedActionIDs
  }

  get legacyFallbackUsed(): boolean {
    return this.report.legacyFallbackUsed
  }
}

// =============================================================================
// Canonical Apply Runtime Gate
// =============================================================================

export class CanonicalApplyRuntimeGate {
  evaluate(context: CanonicalApplyRuntimeOwnerContext): CanonicalApplyRuntimeGateResult {
    const configuration = context.configuration
    switch (configuration.mode) {
      case CanonicalApplyRuntimeMode.DISABLED:
        return new CanonicalApplyRuntimeGateResult(
          CanonicalApplyRuntimeGateState.LEGACY_OWNER,
          [CanonicalApplyRuntimeBlocker.DISABLED_MODE],
          configuration.mode
        )
      case CanonicalApplyRuntimeMode.DIAGNOSTICS_ONLY:
        return new CanonicalApplyRuntimeGateResult(
          CanonicalApplyRuntimeGateState.DIAGNOSTICS_ONLY,
          [CanonicalApplyRuntimeBlocker.DIAGNOSTICS_ONLY],
          configuration.mode
        )
      case CanonicalApplyRuntimeMode.NO_COMMIT:
        return new CanonicalApplyRuntimeGateResult(
          CanonicalApplyRuntimeGateState.NO_COMMIT,
          [CanonicalApplyRuntimeBlocker.NO_COMMIT],
          configuration.mode
        )
      case CanonicalApplyRuntimeMode.BLOCKED:
        return new CanonicalApplyRuntimeGateResult(
          CanonicalApplyRuntimeGateState.BLOCKED,
          [CanonicalApplyRuntimeBlocker.BLOCKED_MODE],
          configuration.mode
        )
      case CanonicalApplyRuntimeMode.TEST_ROOT_APPLY:
      case CanonicalApplyRuntimeMode.PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK:
        break
    }

    const blockers: CanonicalApplyRuntimeBlocker[] = []
    if (!context.canonicalPlanAuthorityAllowed) {
      blockers.push(CanonicalApplyRuntimeBlocker.CANONICAL_PLAN_AUTHORITY_UNAVAILABLE)
    }
    if (!context.inventorySnapshotValid) {
      blockers.push(CanonicalApplyRuntimeBlocker.INVENTORY_SNAPSHOT_INVALID)
    }
    if (context.localManifest === null || context.peerManifest === null) {
      blockers.push(CanonicalApplyRuntimeBlocker.INVENTORY_SNAPSHOT_INVALID)
    }
    if (context.applyPlan.schemaVersion !== CanonicalApplyPlan.CURRENT_SCHEMA_VERSION) {
      blockers.push(CanonicalApplyRuntimeBlocker.APPLY_PLAN_INVALID)
    }
    if (configuration.policy.enabledDomains.length === 0) {
      blockers.push(CanonicalApplyRuntimeBlocker.ENABLED_DOMAINS_MISSING)
    }
    if (!context.legacyFallbackAvailable || !configuration.policy.legacyFallbackAvailable) {
      blockers.push(CanonicalApplyRuntimeBlocker.LEGACY_FALLBACK_UNAVAILABLE)
    }
    if (!configuration.policy.diagnosticsRedacted) {
      blockers.push(CanonicalApplyRuntimeBlocker.DIAGNOSTICS_NOT_REDACTED)
    }
    if (configuration.policy.runtimeSwitchEnabled) {
      blockers.push(CanonicalApplyRuntimeBlocker.RUNTIME_SWITCH_ENABLED)
    }
    if (!configuration.policy.readPathLegacy) {
      blockers.push(CanonicalApplyRuntimeBlocker.READ_PATH_NOT_LEGACY)
    }
    if (configuration.mode === CanonicalApplyRuntimeMode.PRODUCTION_ROOT_APPLY_WITH_LEGACY_FALLBACK) {
      if (configuration.policy.releaseDefaultBuild) {
        blockers.push(CanonicalApplyRuntimeBlocker.RELEASE_DEFAULT_PRODUCTION_APPLY_BLOCKED)
      }
      if (!configuration.policy.debugInternalBuild || !configuration.policy.ownerApproved) {
        blockers.push(CanonicalApplyRuntimeBlocker.DEBUG_INTERNAL_APPROVAL_MISSING)
      }
    }
    if (configuration.mode === CanonicalApplyRuntimeMode.TEST_ROOT_APPLY &&
      !configuration.policy.allowTestRootApply) {
      blockers.push(CanonicalApplyRuntimeBlocker.DEBUG_INTERNAL_APPROVAL_MISSING)
    }

    const enabledDomains = new Set(configuration.policy.enabledDomains)
    const actions = context.allActions
    for (const action of actions) {
      const domain = CanonicalApplyRuntimeGate.domainFor(action)
      if (domain === CanonicalApplyRuntimeDomain.AUDIO_UPLOAD) {
        blockers.push(CanonicalApplyRuntimeBlocker.AUDIO_ACTION_BLOCKED)
        continue
      }
      if (!enabledDomains.has(domain)) {
        blockers.push(CanonicalApplyRuntimeBlocker.DOMAIN_NOT_ENABLED)
      }
      const entry = context.registry.entryFor(domain)
      if (!entry) {
        blockers.push(CanonicalApplyRuntimeBlocker.MISSING_EXECUTOR)
        continue
      }
      if (entry.dryRunOnly && canonicalApplyRuntimeModeExecutesCommit(configuration.mode)) {
        blockers.push(CanonicalApplyRuntimeBlocker.DRY_RUN_ONLY_EXECUTOR)
      }
      if (!entry.rootBoundApplyPortAvailable && canonicalApplyRuntimeModeExecutesCommit(configuration.mode)) {
        blockers.push(CanonicalApplyRuntimeBlocker.ROOT_BOUND_APPLY_PORT_UNAVAILABLE)
      }
      if (!entry.rollbackAvailable) {
        blockers.push(CanonicalApplyRuntimeBlocker.ROLLBACK_UNAVAILABLE)
      }
      if (!entry.postconditionAvailable) {
        blockers.push(CanonicalApplyRuntimeBlocker.POSTCONDITION_UNAVAILABLE)
      }
      if (action.kind === CanonicalApplyActionKind.DEFERRED_UNSUPPORTED) {
        blockers.push(CanonicalApplyRuntimeBlocker.UNSUPPORTED_DOMAIN)
      }
    }
    if (this.hasAudioConflict(context.applyPlan)) {
      blockers.push(CanonicalApplyRuntimeBlocker.AUDIO_ACTION_BLOCKED)
    }
    if (this.hasUnresolvedConflictRequiringRecord(context) &&
      (!enabledDomains.has(CanonicalApplyRuntimeDomain.TOMBSTONE_CONFLICT) ||
        !configuration.policy.allowConflictRecordAction)) {
      blockers.push(CanonicalApplyRuntimeBlocker.UNRESOLVED_CONFLICT)
    }

    return new CanonicalApplyRuntimeGateResult(
      blockers.length === 0 ? CanonicalApplyRuntimeGateState.ALLOWED : CanonicalApplyRuntimeGateState.BLOCKED,
      blockers,
      configuration.mode
    )
  }

  static domainFor(action: CanonicalApplyAction): CanonicalApplyRuntimeDomain {
    if (action.target.artifactKind === CanonicalArtifactKind.AUDIO) {
      return CanonicalApplyRuntimeDomain.AUDIO_UPLOAD
    }
    if (action.reason === CanonicalApplyRuntimeOwner.RECORDING_EXISTENCE_BRIDGE_REASON) {
      return CanonicalApplyRuntimeDomain.RECORDING_EXISTENCE
    }
    switch (action.kind) {
      case CanonicalApplyActionKind.RECORDING_METADATA_APPLY:
      case CanonicalApplyActionKind.RECORDING_METADATA_SEND:
        return CanonicalApplyRuntimeDomain.RECORDING_METADATA
      case CanonicalApplyActionKind.FOLDER_METADATA_APPLY:
      case CanonicalApplyActionKind.FOLDER_METADATA_SEND:
      case CanonicalApplyActionKind.STUDY_ITEM_METADATA_APPLY:
      case CanonicalApplyActionKind.STUDY_ITEM_METADATA_SEND:
        return CanonicalApplyRuntimeDomain.LIBRARY_METADATA
      case CanonicalApplyActionKind.GENERATED_ARTIFACT_DOWNLOAD_APPLY:
      case CanonicalApplyActionKind.GENERATED_ARTIFACT_NO_OP:
        return CanonicalApplyRuntimeDomain.GENERATED_ARTIFACTS
      case CanonicalApplyActionKind.LIBRARY_TOMBSTONE_APPLY:
      case CanonicalApplyActionKind.LIBRARY_TOMBSTONE_SEND:
      case CanonicalApplyActionKind.OBJECT_TOMBSTONE_APPLY:
      case CanonicalApplyActionKind.OBJECT_TOMBSTONE_SEND:
      case CanonicalApplyActionKind.ARTIFACT_TOMBSTONE_APPLY:
      case CanonicalApplyActionKind.CONFLICT_RECORD:
        return CanonicalApplyRuntimeDomain.TOMBSTONE_CONFLICT
      case CanonicalApplyActionKind.DEFERRED_UNSUPPORTED:
        return action.failureReason === CanonicalApplyFailureReason.TOMBSTONE_BLOCKS_RESURRECTION
          ? CanonicalApplyRuntimeDomain.TOMBSTONE_CONFLICT
          : CanonicalApplyRuntimeDomain.AUDIO_UPLOAD
    }
  }

  private hasAudioConflict(plan: CanonicalApplyPlan): boolean {
    return plan.conflicts.some(c =>
      c.kind === 'recordingAudioContentMismatch' || c.target.artifactKind === CanonicalArtifactKind.AUDIO
    )
  }

  private hasUnresolvedConflictRequiringRecord(context: CanonicalApplyRuntimeOwnerContext): boolean {
    const unresolvedApply = context.applyPlan.conflicts.some(c =>
      c.resolutionState === 'unresolved' && c.kind !== 'recordingAudioContentMismatch'
    )
    const unresolvedLibrary = (context.libraryPlan?.conflicts?.length ?? 0) > 0
    if (!unresolvedApply && !unresolvedLibrary) {
      return false
    }
    return !context.allActions.some(action =>
      action.kind === CanonicalApplyActionKind.CONFLICT_RECORD &&
      CanonicalApplyRuntimeGate.domainFor(action) === CanonicalApplyRuntimeDomain.TOMBSTONE_CONFLICT
    )
  }
}

// =============================================================================
// Canonical Apply Runtime Owner
// =============================================================================

export class CanonicalApplyRuntimeOwner {
  static readonly RECORDING_EXISTENCE_BRIDGE_REASON = 'recordingExistenceMetadataOnlyBridge'

  async execute(context: CanonicalApplyRuntimeOwnerContext): Promise<CanonicalApplyRuntimeResult> {
    const gate = new CanonicalApplyRuntimeGate()
    const gateResult = gate.evaluate(context)
    let diagnostics = this.baseDiagnostics(context, gateResult)
    const records: CanonicalApplyRuntimeActionRecord[] = []
    const duplicateSuppressionActionIDs: string[] = []
    let fatalBlocker = false

    if (!gateResult.executesCommit || !context.localManifest || !context.peerManifest) {
      diagnostics.push(
        this.makeDiagnostic(
          CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_LEGACY_FALLBACK_USED,
          context, null,
          CanonicalApplyRuntimeOwner.nonEmpty(gateResult.blockers.join('+')) ?? 'legacyOwner'
        )
      )
      diagnostics.push(this.makeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_REPORT_BUILT,
        context, null, records.length, 'legacyFallback'
      ))
      return this.buildResult(
        context, gateResult, records, true, [], false, diagnostics
      )
    }

    const localManifest = context.localManifest
    const peerManifest = context.peerManifest

    for (const action of context.allActions) {
      const domain = CanonicalApplyRuntimeGate.domainFor(action)
      if (domain === CanonicalApplyRuntimeDomain.AUDIO_UPLOAD) {
        diagnostics.push(this.makeDiagnostic(
          CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_AUDIO_ACTION_BLOCKED,
          context, action, undefined, 'audioUploadUnsupported'
        ))
        records.push(new CanonicalApplyRuntimeActionRecord(
          action, domain, CanonicalApplyRuntimeActionStatus.BLOCKED, false,
          CanonicalApplyRuntimeBlocker.AUDIO_ACTION_BLOCKED
        ))
        break
      }
      const entry = context.registry.entryFor(domain)
      if (!entry) {
        diagnostics.push(this.makeDiagnostic(
          CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_ACTION_FAILED,
          context, action, undefined, 'missingExecutor'
        ))
        records.push(new CanonicalApplyRuntimeActionRecord(
          action, domain, CanonicalApplyRuntimeActionStatus.BLOCKED, false,
          CanonicalApplyRuntimeBlocker.MISSING_EXECUTOR
        ))
        break
      }

      diagnostics.push(this.makeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_ACTION_STARTED,
        context, action, undefined, domain
      ))
      const executorResult = await entry.execute(
        new CanonicalApplyRuntimeExecutorContext(
          action, context.applyPlan, context.libraryPlan,
          localManifest, peerManifest, context.syncRunID
        )
      )

      if (executorResult.rollbackAttempted) {
        diagnostics.push(this.makeDiagnostic(
          CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_ROLLBACK_STARTED,
          context, action, undefined, domain
        ))
        diagnostics.push(
          this.makeDiagnostic(
            executorResult.rollbackSucceeded === true
              ? CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_ROLLBACK_COMPLETED
              : CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_ROLLBACK_FAILED,
            context, action, undefined,
            executorResult.failureReason ?? executorResult.detail
          )
        )
      }

      if (executorResult.succeeded) {
        diagnostics.push(this.makeDiagnostic(
          CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_ACTION_COMPLETED,
          context, action, undefined, executorResult.detail ?? 'committed'
        ))
        diagnostics.push(this.makeDiagnostic(
          CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_DUPLICATE_LEGACY_SUPPRESSED,
          context, action, undefined, 'eligible'
        ))
        records.push(
          new CanonicalApplyRuntimeActionRecord(
            action, domain, CanonicalApplyRuntimeActionStatus.COMPLETED, true,
            null, executorResult.detail
          )
        )
        duplicateSuppressionActionIDs.push(action.actionID)
        continue
      }

      fatalBlocker = executorResult.rollbackFatal
      diagnostics.push(
        this.makeDiagnostic(
          CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_ACTION_FAILED,
          context, action, undefined,
          executorResult.failureReason ?? executorResult.detail ?? 'failed'
        )
      )
      records.push(
        new CanonicalApplyRuntimeActionRecord(
          action, domain, CanonicalApplyRuntimeActionStatus.FAILED, false,
          executorResult.rollbackFatal
            ? CanonicalApplyRuntimeBlocker.ROLLBACK_FAILURE_FATAL
            : CanonicalApplyRuntimeBlocker.ACTION_FAILED,
          executorResult.failureReason
        )
      )
      break
    }

    const completedCount = records.filter(r => r.status === CanonicalApplyRuntimeActionStatus.COMPLETED).length
    const legacyFallbackUsed = completedCount < context.allActions.length || fatalBlocker
    if (legacyFallbackUsed) {
      diagnostics.push(this.makeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_LEGACY_FALLBACK_USED,
        context, null, context.allActions.length - completedCount,
        fatalBlocker ? 'fatalBlocker' : 'unexecutedActions'
      ))
    }
    diagnostics.push(this.makeDiagnostic(
      CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_REPORT_BUILT,
      context, null, records.length, 'canonicalRuntime'
    ))
    return this.buildResult(
      context, gateResult, records, legacyFallbackUsed,
      [...new Set(duplicateSuppressionActionIDs)].sort(), fatalBlocker, diagnostics
    )
  }

  private baseDiagnostics(
    context: CanonicalApplyRuntimeOwnerContext,
    gateResult: CanonicalApplyRuntimeGateResult
  ): CanonicalSyncRuntimeDiagnostic[] {
    return [
      this.makeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_MODE_EVALUATED,
        context, null, context.allActions.length, gateResult.state
      ),
      this.makeDiagnostic(
        gateResult.isAllowed
          ? CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_GATE_ALLOWED
          : CanonicalSyncRuntimeDiagnosticKind.CANONICAL_APPLY_RUNTIME_GATE_BLOCKED,
        context, null, gateResult.blockers.length,
        CanonicalApplyRuntimeOwner.nonEmpty(gateResult.blockers.join('+')) ?? 'none'
      )
    ]
  }

  private makeDiagnostic(
    kind: CanonicalSyncRuntimeDiagnosticKind,
    context: CanonicalApplyRuntimeOwnerContext,
    action: CanonicalApplyAction | null = null,
    count: number | undefined = undefined,
    detail: string | null = null
  ): CanonicalSyncRuntimeDiagnostic {
    return new CanonicalSyncRuntimeDiagnostic(
      kind,
      context.syncRunID,
      canonicalApplyRuntimeModeSyncDiagnosticMode(context.configuration.mode),
      action?.target.objectID ?? null,
      action?.kind ?? null,
      count ?? null,
      detail
    )
  }

  private buildResult(
    context: CanonicalApplyRuntimeOwnerContext,
    gateResult: CanonicalApplyRuntimeGateResult,
    records: CanonicalApplyRuntimeActionRecord[],
    legacyFallbackUsed: boolean,
    duplicateSuppressionActionIDs: string[],
    fatalBlocker: boolean,
    diagnostics: CanonicalSyncRuntimeDiagnostic[]
  ): CanonicalApplyRuntimeResult {
    const report = new CanonicalApplyRuntimeReport(
      context.configuration.mode,
      gateResult,
      records,
      legacyFallbackUsed,
      duplicateSuppressionActionIDs,
      fatalBlocker,
      diagnostics
    )
    return new CanonicalApplyRuntimeResult(
      context.configuration.mode,
      gateResult,
      report
    )
  }

  private static nonEmpty(value: string): string | null {
    return value.trim().length === 0 ? null : value
  }
}

// =============================================================================
// Canonical Apply Runtime Executor Adapters
// =============================================================================

export class CanonicalApplyRuntimeExecutorAdapters {
  static recordingMetadata(
    executor: CanonicalRecordingMetadataCutoverExecutor,
    dryRunOnly: boolean = false,
    rootBoundApplyPortAvailable: boolean = true
  ): CanonicalApplyRuntimeExecutorEntry {
    return new CanonicalApplyRuntimeExecutorEntry(
      CanonicalApplyRuntimeDomain.RECORDING_METADATA,
      dryRunOnly, true, true, rootBoundApplyPortAvailable,
      async (context: CanonicalApplyRuntimeExecutorContext) => {
        const action = context.action
        const localObjectsMap = new Map<string, CanonicalRecordingObject>()
        for (const obj of context.localManifest.objects) {
          localObjectsMap.set(obj.objectID, obj)
        }
        const peerObjectsMap = new Map<string, CanonicalRecordingObject>()
        for (const obj of context.peerManifest.objects) {
          peerObjectsMap.set(obj.objectID, obj)
        }
        const candidate: CanonicalRecordingMetadataCutoverCandidate = {
          action: action,
          localObject: localObjectsMap.get(action.target.objectID) ?? null,
          peerObject: peerObjectsMap.get(action.target.objectID) ?? null,
          rollbackCheckpointID: 'apply-runtime-recording-' + action.target.objectID,
          cutoverActionKind: null
        }
        if (!candidate.cutoverActionKind && action.kind === CanonicalApplyActionKind.RECORDING_METADATA_APPLY) {
          candidate.cutoverActionKind = CanonicalRecordingMetadataCutoverActionKind.APPLY
        } else if (!candidate.cutoverActionKind && action.kind === CanonicalApplyActionKind.RECORDING_METADATA_SEND) {
          candidate.cutoverActionKind = CanonicalRecordingMetadataCutoverActionKind.SEND
        }
        if (!candidate.cutoverActionKind) {
          return CanonicalApplyRuntimeExecutorResult.failure(
            action, CanonicalApplyRuntimeDomain.RECORDING_METADATA,
            true, true, false, null, false,
            CanonicalApplyRuntimeBlocker.UNSUPPORTED_DOMAIN
          )
        }
        const commit = await executor.commitRecordingMetadata(candidate) as
          CanonicalRecordingMetadataProductionCommitResult
        if (commit.committed && commit.preconditionVerified && commit.postconditionVerified) {
          return CanonicalApplyRuntimeExecutorResult.success(action,
            CanonicalApplyRuntimeDomain.RECORDING_METADATA, commit.reason)
        }
        const rollbackResult = await executor.rollbackRecordingMetadata(candidate,
          CanonicalApplyRuntimeExecutorAdapters.recordingRollbackReason(commit)) as
          CanonicalRecordingMetadataRollbackResult
        return CanonicalApplyRuntimeExecutorResult.failure(
          action, CanonicalApplyRuntimeDomain.RECORDING_METADATA,
          commit.preconditionVerified, commit.postconditionVerified,
          true, rollbackResult.succeeded,
          rollbackResult.fatal || !rollbackResult.succeeded,
          commit.failureKind ?? commit.reason
        )
      }
    )
  }

  static libraryMetadata(
    executor: CanonicalLibraryMetadataCutoverExecutor,
    dryRunOnly: boolean = false,
    rootBoundApplyPortAvailable: boolean = true
  ): CanonicalApplyRuntimeExecutorEntry {
    return new CanonicalApplyRuntimeExecutorEntry(
      CanonicalApplyRuntimeDomain.LIBRARY_METADATA,
      dryRunOnly, true, true, rootBoundApplyPortAvailable,
      async (context: CanonicalApplyRuntimeExecutorContext) => {
        if (!context.libraryPlan) {
          return CanonicalApplyRuntimeExecutorResult.failure(
            context.action, CanonicalApplyRuntimeDomain.LIBRARY_METADATA,
            true, true, false, null, false,
            CanonicalApplyRuntimeBlocker.MISSING_EXECUTOR
          )
        }
        const commit = await executor.commitLibraryMetadata({ action: context.action }) as
          CanonicalLibraryMetadataCutoverResult
        if (commit.committed && commit.preconditionVerified && commit.postconditionVerified) {
          return CanonicalApplyRuntimeExecutorResult.success(context.action,
            CanonicalApplyRuntimeDomain.LIBRARY_METADATA, commit.reason)
        }
        const rollbackResult = await executor.rollbackLibraryMetadata(
          { action: context.action }, commit.failureKind ?? CanonicalCutoverFailure.APPLY_FAILURE_BEFORE_COMMIT
        ) as CanonicalLibraryMetadataRollbackResult
        return CanonicalApplyRuntimeExecutorResult.failure(
          context.action, CanonicalApplyRuntimeDomain.LIBRARY_METADATA,
          commit.preconditionVerified, commit.postconditionVerified,
          true, rollbackResult.succeeded,
          rollbackResult.fatal || !rollbackResult.succeeded,
          commit.failureKind ?? commit.reason
        )
      }
    )
  }

  static generatedArtifacts(
    executor: CanonicalGeneratedArtifactCutoverExecutor,
    dryRunOnly: boolean = false,
    rootBoundApplyPortAvailable: boolean = true
  ): CanonicalApplyRuntimeExecutorEntry {
    return new CanonicalApplyRuntimeExecutorEntry(
      CanonicalApplyRuntimeDomain.GENERATED_ARTIFACTS,
      dryRunOnly, true, true, rootBoundApplyPortAvailable,
      async (context: CanonicalApplyRuntimeExecutorContext) => {
        const commit = await executor.commitGeneratedArtifact({ action: context.action }) as
          { committed: boolean; preconditionVerified: boolean; postconditionVerified: boolean;
            reason: string; failureKind: CanonicalCutoverFailure | null }
        if (commit.committed && commit.preconditionVerified && commit.postconditionVerified) {
          return CanonicalApplyRuntimeExecutorResult.success(context.action,
            CanonicalApplyRuntimeDomain.GENERATED_ARTIFACTS, commit.reason)
        }
        const rollbackResult = await executor.rollbackGeneratedArtifact(
          { action: context.action }, commit.failureKind ?? CanonicalCutoverFailure.APPLY_FAILURE_BEFORE_COMMIT
        ) as CanonicalGeneratedArtifactRollbackResult
        return CanonicalApplyRuntimeExecutorResult.failure(
          context.action, CanonicalApplyRuntimeDomain.GENERATED_ARTIFACTS,
          commit.preconditionVerified, commit.postconditionVerified,
          true, rollbackResult.succeeded,
          rollbackResult.fatal || !rollbackResult.succeeded,
          commit.failureKind ?? commit.reason
        )
      }
    )
  }

  static tombstoneConflict(
    executor: CanonicalTombstoneConflictCutoverExecutor,
    dryRunOnly: boolean = false,
    rootBoundApplyPortAvailable: boolean = true
  ): CanonicalApplyRuntimeExecutorEntry {
    return new CanonicalApplyRuntimeExecutorEntry(
      CanonicalApplyRuntimeDomain.TOMBSTONE_CONFLICT,
      dryRunOnly, true, true, rootBoundApplyPortAvailable,
      async (context: CanonicalApplyRuntimeExecutorContext) => {
        const commit = await executor.commitTombstoneConflict({ action: context.action }) as
          { committed: boolean; preconditionVerified: boolean; postconditionVerified: boolean;
            reason: string; failureKind: CanonicalCutoverFailure | null }
        if (commit.committed && commit.preconditionVerified && commit.postconditionVerified) {
          return CanonicalApplyRuntimeExecutorResult.success(context.action,
            CanonicalApplyRuntimeDomain.TOMBSTONE_CONFLICT, commit.reason)
        }
        const rollbackResult = await executor.rollbackTombstoneConflict(
          { action: context.action }, commit.failureKind ?? CanonicalCutoverFailure.APPLY_FAILURE_BEFORE_COMMIT
        ) as CanonicalTombstoneConflictRollbackResult
        return CanonicalApplyRuntimeExecutorResult.failure(
          context.action, CanonicalApplyRuntimeDomain.TOMBSTONE_CONFLICT,
          commit.preconditionVerified, commit.postconditionVerified,
          true, rollbackResult.succeeded,
          rollbackResult.fatal || !rollbackResult.succeeded,
          commit.failureKind ?? commit.reason
        )
      }
    )
  }

  private static recordingRollbackReason(
    commit: CanonicalRecordingMetadataProductionCommitResult
  ): CanonicalCutoverFailure {
    if (commit.failureKind === CanonicalCutoverFailure.PRECONDITION_MISMATCH || !commit.preconditionVerified) {
      return CanonicalCutoverFailure.PRECONDITION_MISMATCH
    }
    if (commit.failureKind === CanonicalCutoverFailure.POSTCONDITION_MISMATCH || !commit.postconditionVerified) {
      return CanonicalCutoverFailure.POSTCONDITION_MISMATCH
    }
    if (commit.partialCommit) {
      return CanonicalCutoverFailure.APPLY_FAILURE_AFTER_PARTIAL_COMMIT
    }
    return CanonicalCutoverFailure.APPLY_FAILURE_BEFORE_COMMIT
  }
}

// =============================================================================
// Canonical Apply Executor
// =============================================================================

export class CanonicalApplyExecutor {
  private conflictResolver: CanonicalConflictResolver

  constructor(conflictResolver: CanonicalConflictResolver = new CanonicalConflictResolver()) {
    this.conflictResolver = conflictResolver
  }

  async execute(
    applyPlan: CanonicalApplyPlan,
    libraryPlan: CanonicalLibrarySyncPlan | null,
    context: CanonicalApplyRuntimeContext
  ): Promise<CanonicalApplyExecutionReport> {
    const records: CanonicalApplyExecutionRecord[] = []
    const allActions = [...applyPlan.actions, ...(libraryPlan?.applyActions ?? [])]
    for (const action of allActions) {
      let record: CanonicalApplyExecutionRecord
      try {
        record = await this.executeAction(action, context)
      } catch (error) {
        record = new CanonicalApplyExecutionRecord(
          action.actionID,
          action.kind,
          action.target,
          CanonicalApplyExecutionStatus.FAILED,
          null, null,
          CanonicalApplyFailureReason.HASH_OR_SIZE_MISMATCH,
          String(error)
        )
      }
      records.push(record)
    }
    const conflictReport = this.conflictResolver.resolve(
      applyPlan.conflicts as unknown as object[] as any,
      (libraryPlan?.conflicts ?? []) as unknown as object[] as any
    )
    return new CanonicalApplyExecutionReport(
      this.deduplicated(records),
      conflictReport,
      records.filter(r => r.status === CanonicalApplyExecutionStatus.APPLIED ||
        r.status === CanonicalApplyExecutionStatus.SENT).length,
      records.filter(r => r.status === CanonicalApplyExecutionStatus.FAILED).length
    )
  }

  private async executeAction(
    action: CanonicalApplyAction,
    context: CanonicalApplyRuntimeContext
  ): Promise<CanonicalApplyExecutionRecord> {
    switch (action.kind) {
      case CanonicalApplyActionKind.RECORDING_METADATA_APPLY:
      case CanonicalApplyActionKind.FOLDER_METADATA_APPLY:
      case CanonicalApplyActionKind.STUDY_ITEM_METADATA_APPLY:
        return this.writeMetadata(action, context.peerManifest, context.localFileStore,
          context.localMetadataRoot, CanonicalApplyExecutionStatus.APPLIED)
      case CanonicalApplyActionKind.RECORDING_METADATA_SEND:
      case CanonicalApplyActionKind.FOLDER_METADATA_SEND:
      case CanonicalApplyActionKind.STUDY_ITEM_METADATA_SEND:
        return this.writeMetadata(action, context.localManifest, context.peerFileStore,
          context.peerMetadataRoot, CanonicalApplyExecutionStatus.SENT)
      case CanonicalApplyActionKind.OBJECT_TOMBSTONE_APPLY:
      case CanonicalApplyActionKind.LIBRARY_TOMBSTONE_APPLY:
        return this.markTombstone(action, context.localFileStore, context.localMetadataRoot,
          CanonicalApplyExecutionStatus.APPLIED)
      case CanonicalApplyActionKind.OBJECT_TOMBSTONE_SEND:
      case CanonicalApplyActionKind.LIBRARY_TOMBSTONE_SEND:
        return this.markTombstone(action, context.peerFileStore, context.peerMetadataRoot,
          CanonicalApplyExecutionStatus.SENT)
      case CanonicalApplyActionKind.GENERATED_ARTIFACT_DOWNLOAD_APPLY:
        return this.downloadGeneratedArtifact(action, context)
      case CanonicalApplyActionKind.GENERATED_ARTIFACT_NO_OP:
        return this.makeRecord(action, CanonicalApplyExecutionStatus.NO_OP, 'generatedArtifactSameContent')
      case CanonicalApplyActionKind.ARTIFACT_TOMBSTONE_APPLY:
        return this.markTombstone(action, context.localFileStore, context.localGeneratedRoot,
          CanonicalApplyExecutionStatus.DEFERRED_UNSUPPORTED)
      case CanonicalApplyActionKind.CONFLICT_RECORD:
        return this.makeRecord(action, CanonicalApplyExecutionStatus.CONFLICT_RECORDED, action.conflictID)
      case CanonicalApplyActionKind.DEFERRED_UNSUPPORTED:
        return this.makeRecord(action, CanonicalApplyExecutionStatus.DEFERRED_UNSUPPORTED,
          action.failureReason ?? null)
    }
  }

  private async writeMetadata(
    action: CanonicalApplyAction,
    sourceManifest: CanonicalManifest,
    targetStore: CanonicalFileStorePort,
    root: CanonicalRootToken,
    status: CanonicalApplyExecutionStatus
  ): Promise<CanonicalApplyExecutionRecord> {
    const metadataObj = sourceManifest.objects.find(o => o.objectID === action.target.objectID)
    if (!metadataObj) {
      throw new Error(CanonicalApplyRuntimeError.MISSING_SOURCE_OBJECT + action.target.objectID)
    }
    const json = JSON.stringify(metadataObj.metadata)
    const data = new Uint8Array(json.length)
    for (let i = 0; i < json.length; i++) {
      data[i] = json.charCodeAt(i)
    }
    const hash = InMemoryCanonicalFileStore.hash(data, 'sha256')
    const reference = new CanonicalFileReference(
      root,
      this.metadataPathTokenFor(action),
      action.target.artifactID,
      action.target.artifactKind
    )
    const intent = new CanonicalFileWriteIntent(
      reference, data, CanonicalFilePurpose.METADATA_BLOB,
      hash, data.length, 'replace',
      new CanonicalMetadataBlob({
        'action': action.kind,
        'objectID': action.target.objectID,
        'hashPrefix': hash ? hashPrefix(hash.value, 12) : ''
      })
    )
    const result = await targetStore.write(intent) as CanonicalFileWriteResult
    return new CanonicalApplyExecutionRecord(
      action.actionID, action.kind, action.target, status,
      result.contentHash ? hashPrefix(result.contentHash.value, 12) : null,
      result.byteSize, null, result.disposition
    )
  }

  private async downloadGeneratedArtifact(
    action: CanonicalApplyAction,
    context: CanonicalApplyRuntimeContext
  ): Promise<CanonicalApplyExecutionRecord> {
    const kind = action.target.artifactKind
    if (!kind) {
      throw new Error(CanonicalApplyRuntimeError.MISSING_SOURCE_ARTIFACT + action.target.objectID)
    }
    const peerArtifact = this.findArtifact(action.target.objectID, kind, context.peerManifest)
    if (!peerArtifact) {
      throw new Error(CanonicalApplyRuntimeError.MISSING_SOURCE_ARTIFACT + action.target.objectID)
    }
    const token = peerArtifact.logicalPathToken
    if (!token) {
      throw new Error(CanonicalApplyRuntimeError.MISSING_LOGICAL_PATH_TOKEN + action.target.objectID)
    }
    const peerReference = new CanonicalFileReference(
      context.peerGeneratedRoot, token, peerArtifact.artifactID, peerArtifact.kind
    )
    const readResult = await context.peerFileStore.read(
      new CanonicalFileReadRequest(peerReference)
    ) as CanonicalFileReadResult
    this.validateReadResult(readResult, peerArtifact)
    const localReference = new CanonicalFileReference(
      context.localGeneratedRoot, token, peerArtifact.artifactID, peerArtifact.kind
    )
    const writeResult = await context.localFileStore.write(
      new CanonicalFileWriteIntent(
        localReference, readResult.bytes, CanonicalFilePurpose.GENERATED_ARTIFACT,
        peerArtifact.contentHash, peerArtifact.byteSize ?? 0,
        'idempotentIfSameContent'
      )
    ) as CanonicalFileWriteResult
    return new CanonicalApplyExecutionRecord(
      action.actionID, action.kind, action.target, CanonicalApplyExecutionStatus.APPLIED,
      writeResult.contentHash ? hashPrefix(writeResult.contentHash.value, 12) : null,
      writeResult.byteSize, null, writeResult.disposition
    )
  }

  private async markTombstone(
    action: CanonicalApplyAction,
    targetStore: CanonicalFileStorePort,
    root: CanonicalRootToken,
    status: CanonicalApplyExecutionStatus
  ): Promise<CanonicalApplyExecutionRecord> {
    const reference = new CanonicalFileReference(
      root,
      action.target.artifactKind === null
        ? this.metadataPathTokenFor(action)
        : this.artifactTombstoneTokenFor(action),
      action.target.artifactID,
      action.target.artifactKind
    )
    const result = await targetStore.markTombstone(reference, action.reason) as CanonicalFileWriteResult
    return new CanonicalApplyExecutionRecord(
      action.actionID, action.kind, action.target, status,
      result.contentHash ? hashPrefix(result.contentHash.value, 12) : null,
      result.byteSize, action.failureReason, 'noPhysicalDelete'
    )
  }

  private findArtifact(
    objectID: string,
    kind: CanonicalArtifactKind,
    manifest: CanonicalManifest
  ): CanonicalArtifact | null {
    const obj = manifest.objects.find(o => o.objectID === objectID)
    if (!obj) {
      return null
    }
    return obj.artifacts.find(a => a.kind === kind) ?? null
  }

  private validateReadResult(read: CanonicalFileReadResult, artifact: CanonicalArtifact): void {
    if (artifact.byteSize !== null && read.byteSize !== artifact.byteSize) {
      throw new Error(CanonicalApplyRuntimeError.HASH_OR_SIZE_MISMATCH + artifact.artifactID)
    }
    if (artifact.contentHash !== null && read.contentHash !== null &&
      !artifact.contentHash.equals(read.contentHash)) {
      throw new Error(CanonicalApplyRuntimeError.HASH_OR_SIZE_MISMATCH + artifact.artifactID)
    }
  }

  private makeRecord(
    action: CanonicalApplyAction,
    status: CanonicalApplyExecutionStatus,
    detail: string | null
  ): CanonicalApplyExecutionRecord {
    return new CanonicalApplyExecutionRecord(
      action.actionID, action.kind, action.target, status,
      null, null, action.failureReason, detail
    )
  }

  private metadataPathTokenFor(action: CanonicalApplyAction): string {
    const kindStr = action.kind
    return 'metadata/' + safePathComponentFor(kindStr) + '/' +
      safePathComponentFor(action.target.objectID) + '.json'
  }

  private artifactTombstoneTokenFor(action: CanonicalApplyAction): string {
    const kindStr = action.target.artifactKind ?? 'artifact'
    return 'tombstones/' + safePathComponentFor(action.target.objectID) + '/' +
      safePathComponentFor(kindStr) + '.marker'
  }

  private deduplicated(records: CanonicalApplyExecutionRecord[]): CanonicalApplyExecutionRecord[] {
    const seen = new Set<string>()
    return records.filter(r => {
      if (seen.has(r.actionID)) {
        return false
      }
      seen.add(r.actionID)
      return true
    })
  }
}

function safePathComponentFor(value: string): string {
  const alphanumeric = /^[a-zA-Z0-9\-_]+$/
  let result = ''
  for (const ch of value) {
    result += alphanumeric.test(ch) ? ch : '-'
  }
  const trimmed = result.replace(/^-+|-+$/g, '')
  if (trimmed.length === 0) {
    return hashPrefix(CanonicalHash.sha256OfString(value).value, 12)
  }
  return trimmed
}
