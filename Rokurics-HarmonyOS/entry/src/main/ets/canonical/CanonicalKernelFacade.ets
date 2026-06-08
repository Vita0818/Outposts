/**
 * CanonicalKernelFacade — kernel facade for canonical sync operations.
 * Ported from Rokurics-Apple (CanonicalKernelFacade.swift).
 * Provides a unified API for building snapshots, plans, and executing operations.
 */

import { CanonicalTimestamp, nilIfEmpty } from './CanonicalCore'
import { CanonicalSyncPlanTrigger, CanonicalSyncPlan, CanonicalSyncPlanner } from './CanonicalSyncPlanner'
import { CanonicalProductionRedaction } from './CanonicalSyncKernelCompletion'

// ── Forward declarations for production types (not yet ported) ──

export enum CanonicalKernelExecutionMode {
  DISABLED = 'disabled',
  DRY_RUN = 'dryRun',
  OFFLINE_RUNTIME = 'offlineRuntime',
  PRODUCTION_SHADOW = 'productionShadow',
  EXECUTION_SHADOW_DRY_RUN = 'executionShadowDryRun',
  EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE = 'executionShadowWithShadowFileStore',
  EXECUTION_SHADOW_WITH_READ_ONLY_TRANSPORT_PROBE = 'executionShadowWithReadOnlyTransportProbe',
  PRODUCTION_EXECUTE = 'productionExecute'
}

export function allowsDryRunPlanning(mode: CanonicalKernelExecutionMode): boolean {
  switch (mode) {
    case CanonicalKernelExecutionMode.DRY_RUN:
    case CanonicalKernelExecutionMode.PRODUCTION_SHADOW:
    case CanonicalKernelExecutionMode.EXECUTION_SHADOW_DRY_RUN:
    case CanonicalKernelExecutionMode.EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE:
    case CanonicalKernelExecutionMode.EXECUTION_SHADOW_WITH_READ_ONLY_TRANSPORT_PROBE:
    case CanonicalKernelExecutionMode.PRODUCTION_EXECUTE:
      return true
    case CanonicalKernelExecutionMode.DISABLED:
    case CanonicalKernelExecutionMode.OFFLINE_RUNTIME:
      return false
  }
}

export function isShadowPreparationMode(mode: CanonicalKernelExecutionMode): boolean {
  switch (mode) {
    case CanonicalKernelExecutionMode.PRODUCTION_SHADOW:
    case CanonicalKernelExecutionMode.EXECUTION_SHADOW_DRY_RUN:
    case CanonicalKernelExecutionMode.EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE:
    case CanonicalKernelExecutionMode.EXECUTION_SHADOW_WITH_READ_ONLY_TRANSPORT_PROBE:
      return true
    case CanonicalKernelExecutionMode.DISABLED:
    case CanonicalKernelExecutionMode.DRY_RUN:
    case CanonicalKernelExecutionMode.OFFLINE_RUNTIME:
    case CanonicalKernelExecutionMode.PRODUCTION_EXECUTE:
      return false
  }
}

export enum CanonicalKernelOperation {
  BUILD_SNAPSHOT = 'buildSnapshot',
  BUILD_MANIFEST = 'buildManifest',
  PLAN_SYNC = 'planSync',
  BUILD_APPLY_PLAN = 'buildApplyPlan',
  BUILD_LIBRARY_PLAN = 'buildLibraryPlan',
  BUILD_TRANSFER_PROJECTION = 'buildTransferProjection',
  BUILD_OBJECT_PROJECTION = 'buildObjectProjection',
  BUILD_RUNTIME_READINESS = 'buildRuntimeReadiness',
  BUILD_PRODUCTION_READINESS = 'buildProductionReadiness',
  DRY_RUN_MIGRATION = 'dryRunMigration',
  COMPARE_LEGACY = 'compareLegacy',
  EXECUTE_OFFLINE = 'executeOffline',
  EXECUTE_PRODUCTION = 'executeProduction',
  ROLLBACK_PREVIEW = 'rollbackPreview'
}

export class CanonicalKernelError extends Error {
  kind: string
  detail: string | null

  constructor(kind: string, detail: string | null = null) {
    let message = kind
    if (detail) {
      message = `${kind}(${detail})`
    }
    super(message)
    this.kind = kind
    this.detail = detail
  }

  static disabled(detail: string): CanonicalKernelError {
    return new CanonicalKernelError('disabled', detail)
  }

  static modeNotAllowed(detail: string): CanonicalKernelError {
    return new CanonicalKernelError('modeNotAllowed', detail)
  }

  static productionExecutionRejected(reasons: string[]): CanonicalKernelError {
    return new CanonicalKernelError('productionExecutionRejected', reasons.join(','))
  }

  static missingInput(detail: string): CanonicalKernelError {
    return new CanonicalKernelError('missingInput', detail)
  }

  static portMissing(detail: string): CanonicalKernelError {
    return new CanonicalKernelError('portMissing', detail)
  }

  static operationFailed(detail: string): CanonicalKernelError {
    return new CanonicalKernelError('operationFailed', detail)
  }
}

// ── Production ports and types (minimal forward declarations) ──

export enum CanonicalProductionPortKind {
  FILE = 'file',
  TRANSPORT = 'transport',
  UPLOAD = 'upload',
  APPLY = 'apply'
}

export enum CanonicalProductionDomain {
  RECORDING_METADATA = 'recordingMetadata',
  LIBRARY_METADATA = 'libraryMetadata',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  RECORDING_EXISTENCE = 'recordingExistence',
  AUDIO_UPLOAD = 'audioUpload',
  READ_RUNTIME = 'readRuntime'
}

export enum CanonicalProductionSideEffectKind {
  FILE_READ = 'fileRead',
  FILE_WRITE = 'fileWrite',
  NETWORK_REQUEST = 'networkRequest',
  UPLOAD_SESSION_START = 'uploadSessionStart',
  UPLOAD_CHUNK_SEND = 'uploadChunkSend',
  UPLOAD_FINALIZE = 'uploadFinalize',
  METADATA_APPLY = 'metadataApply',
  GENERATED_ARTIFACT_APPLY = 'generatedArtifactApply',
  TOMBSTONE_MARK = 'tombstoneMark',
  CONFLICT_RECORD = 'conflictRecord',
  DIAGNOSTICS_WRITE = 'diagnosticsWrite'
}

export class CanonicalProductionExecutionPolicy {
  constructor() {}
}

export interface CanonicalProductionFilePort {
  readMetadata(request: CanonicalProductionMetadataReadRequest): Promise<CanonicalProductionFileEvidence>
  writeMetadata(intent: CanonicalFileWriteIntent, rollbackCheckpoint: string | null): Promise<CanonicalProductionWriteResult>
  markTombstone(request: CanonicalProductionTombstoneRequest): Promise<CanonicalProductionWriteResult>
}

export interface CanonicalProductionTransportPort {
  buildSignedRequest(request: CanonicalProductionTransportBuildRequest): Promise<CanonicalProductionSignedRequest>
  sendRequest(request: CanonicalProductionSignedRequest): Promise<CanonicalProductionTransportExchange>
}

export interface CanonicalProductionUploadPort {
  startResumableUpload(request: CanonicalUploadStartRequest, now: Date): Promise<CanonicalUploadSessionStatus>
  uploadChunk(chunk: CanonicalUploadChunk, now: Date): Promise<CanonicalUploadChunkStatus>
  finalizeUpload(request: CanonicalUploadFinalizeRequest, now: Date): Promise<CanonicalUploadSessionStatus>
}

export interface CanonicalProductionApplyPort {
  sendMetadata(request: CanonicalProductionApplyExecutionRequest): Promise<CanonicalProductionApplyResult>
  applyMetadata(request: CanonicalProductionApplyExecutionRequest): Promise<CanonicalProductionApplyResult>
  applyGeneratedArtifact(request: CanonicalProductionApplyExecutionRequest): Promise<CanonicalProductionApplyResult>
  recordConflict(request: CanonicalProductionApplyExecutionRequest): Promise<CanonicalProductionApplyResult>
}

export class CanonicalFileWriteIntent {
  reference: string
  constructor(reference: string) {
    this.reference = reference
  }
}

export class CanonicalProductionTransportBuildRequest {
  route: string
  constructor(route: string) {
    this.route = route
  }
}

export class CanonicalProductionSignedRequest {
  route: string
  body: string
  constructor(route: string, body: string = '') {
    this.route = route
    this.body = body
  }
}

export class CanonicalProductionTransportExchange {
  sideEffect: CanonicalProductionSideEffect | null
  constructor(sideEffect: CanonicalProductionSideEffect | null = null) {
    this.sideEffect = sideEffect
  }
}

export class CanonicalProductionSideEffect {
  kind: CanonicalProductionSideEffectKind
  domain: CanonicalProductionDomain
  objectID: string | null
  artifactID: string | null
  byteSize: number | null
  hashPrefix: string | null
  hash: string | null
  summary: string | null
  route: string | null

  constructor(
    kind: CanonicalProductionSideEffectKind,
    domain: CanonicalProductionDomain,
    objectID: string | null = null,
    artifactID: string | null = null,
    byteSize: number | null = null,
    hashPrefix: string | null = null,
    hash: string | null = null,
    summary: string | null = null,
    route: string | null = null
  ) {
    this.kind = kind
    this.domain = domain
    this.objectID = objectID
    this.artifactID = artifactID
    this.byteSize = byteSize
    this.hashPrefix = hashPrefix
    this.hash = hash
    this.summary = summary
    this.route = route
  }
}

export class CanonicalProductionMetadataReadRequest {
  objectID: string
  reference: string
  constructor(objectID: string, reference: string) {
    this.objectID = objectID
    this.reference = reference
  }
}

export class CanonicalProductionFileEvidence {
  actualByteSize: number | null
  actualHashPrefix: string | null
  constructor(actualByteSize: number | null = null, actualHashPrefix: string | null = null) {
    this.actualByteSize = actualByteSize
    this.actualHashPrefix = actualHashPrefix
  }
}

export class CanonicalProductionWriteResult {
  evidence: CanonicalProductionFileEvidence
  disposition: string
  constructor(evidence: CanonicalProductionFileEvidence, disposition: string = 'written') {
    this.evidence = evidence
    this.disposition = disposition
  }
}

export class CanonicalProductionTombstoneRequest {
  objectID: string
  constructor(objectID: string) {
    this.objectID = objectID
  }
}

export class CanonicalUploadStartRequest {
  objectID: string
  constructor(objectID: string) {
    this.objectID = objectID
  }
}

export class CanonicalUploadChunk {
  objectID: string
  chunkHash: string | null
  constructor(objectID: string, chunkHash: string | null = null) {
    this.objectID = objectID
    this.chunkHash = chunkHash
  }
}

export class CanonicalUploadFinalizeRequest {
  objectID: string
  constructor(objectID: string) {
    this.objectID = objectID
  }
}

export class CanonicalUploadSessionStatus {
  fileSize: number | null
  checksum: { value: string | null } | null
  constructor(fileSize: number | null = null, checksum: { value: string | null } | null = null) {
    this.fileSize = fileSize
    this.checksum = checksum
  }
}

export class CanonicalUploadChunkStatus {
  confirmedBytes: number | null
  constructor(confirmedBytes: number | null = null) {
    this.confirmedBytes = confirmedBytes
  }
}

export class CanonicalApplyAction {
  kind: string
  target: CanonicalApplyTarget
  constructor(kind: string, target: CanonicalApplyTarget) {
    this.kind = kind
    this.target = target
  }
}

export class CanonicalApplyTarget {
  objectID: string
  artifactID: string | null
  constructor(objectID: string, artifactID: string | null = null) {
    this.objectID = objectID
    this.artifactID = artifactID
  }
}

export class CanonicalProductionApplyExecutionRequest {
  action: CanonicalApplyAction
  rollbackCheckpointID: string | null
  constructor(action: CanonicalApplyAction, rollbackCheckpointID: string | null = null) {
    this.action = action
    this.rollbackCheckpointID = rollbackCheckpointID
  }
}

export class CanonicalProductionApplyResult {
  sideEffect: CanonicalProductionSideEffect | null
  constructor(sideEffect: CanonicalProductionSideEffect | null = null) {
    this.sideEffect = sideEffect
  }
}

export enum CanonicalProductionExecutionRejectionReason {
  DISABLED = 'disabled',
  DRY_RUN_NOT_ALLOWED = 'dryRunNotAllowed',
  PRODUCTION_PORTS_MISSING = 'productionPortsMissing',
  ROLLBACK_MISSING = 'rollbackMissing',
  DRY_RUN_EQUIVALENCE_FAILED = 'dryRunEquivalenceFailed',
  UNRESOLVED_CONFLICTS = 'unresolvedConflicts'
}

export class CanonicalProductionExecutionAudit {
  allowed: boolean
  rejectionReasons: CanonicalProductionExecutionRejectionReason[]
  constructor(allowed: boolean, rejectionReasons: CanonicalProductionExecutionRejectionReason[] = []) {
    this.allowed = allowed
    this.rejectionReasons = rejectionReasons
  }
}

export class CanonicalProductionExecutionGuard {
  static evaluate(
    mode: CanonicalKernelExecutionMode,
    token: CanonicalProductionExecutionToken | null,
    policy: CanonicalProductionExecutionPolicy,
    domains: CanonicalProductionDomain[],
    ports: CanonicalProductionPortSet,
    rollbackPlan: CanonicalRollbackPlan | null,
    dryRunReportID: string | null,
    dryRunEquivalence: CanonicalDryRunEquivalenceReport | null,
    readinessReport: CanonicalDryRunReadinessReport | null,
    unresolvedConflictCount: number
  ): CanonicalProductionExecutionAudit {
    const reasons: CanonicalProductionExecutionRejectionReason[] = []
    if (mode !== CanonicalKernelExecutionMode.PRODUCTION_EXECUTE) {
      reasons.push(CanonicalProductionExecutionRejectionReason.DISABLED)
    }
    if (!ports.file || !ports.transport || !ports.upload || !ports.apply) {
      reasons.push(CanonicalProductionExecutionRejectionReason.PRODUCTION_PORTS_MISSING)
    }
    if (!rollbackPlan) {
      reasons.push(CanonicalProductionExecutionRejectionReason.ROLLBACK_MISSING)
    }
    if (unresolvedConflictCount > 0) {
      reasons.push(CanonicalProductionExecutionRejectionReason.UNRESOLVED_CONFLICTS)
    }
    return new CanonicalProductionExecutionAudit(reasons.length === 0, reasons)
  }
}

export class CanonicalProductionExecutionToken {
  constructor() {}
}

export class CanonicalRollbackPlan {
  constructor() {}
}

export class CanonicalDryRunEquivalenceReport {
  constructor() {}
}

export class CanonicalDryRunReadinessReport {
  constructor() {}
}

export class CanonicalProductionExecutionFailure {
  operationID: string
  domain: CanonicalProductionDomain | null
  reason: string
  constructor(operationID: string, domain: CanonicalProductionDomain | null = null, reason: string = '') {
    this.operationID = operationID
    this.domain = domain
    this.reason = reason
  }
}

export class CanonicalProductionExecutionResult {
  operationID: string
  mode: CanonicalKernelExecutionMode
  succeeded: boolean
  sideEffects: CanonicalProductionSideEffect[]
  failures: CanonicalProductionExecutionFailure[]
  guardAudit: CanonicalProductionExecutionAudit | null
  constructor(
    operationID: string,
    mode: CanonicalKernelExecutionMode,
    succeeded: boolean,
    sideEffects: CanonicalProductionSideEffect[] = [],
    failures: CanonicalProductionExecutionFailure[] = [],
    guardAudit: CanonicalProductionExecutionAudit | null = null
  ) {
    this.operationID = operationID
    this.mode = mode
    this.succeeded = succeeded
    this.sideEffects = sideEffects
    this.failures = failures
    this.guardAudit = guardAudit
  }
}

// ── Canonical Port Set ──

export class CanonicalProductionPortSet {
  file: CanonicalProductionFilePort | null
  transport: CanonicalProductionTransportPort | null
  upload: CanonicalProductionUploadPort | null
  apply: CanonicalProductionApplyPort | null

  constructor(
    file: CanonicalProductionFilePort | null = null,
    transport: CanonicalProductionTransportPort | null = null,
    upload: CanonicalProductionUploadPort | null = null,
    apply: CanonicalProductionApplyPort | null = null
  ) {
    this.file = file
    this.transport = transport
    this.upload = upload
    this.apply = apply
  }

  readiness(): CanonicalProductionPortReadiness {
    return new CanonicalProductionPortReadiness(
      this.file !== null,
      this.transport !== null,
      this.upload !== null,
      this.apply !== null
    )
  }
}

export class CanonicalProductionPortReadiness {
  fileReady: boolean
  transportReady: boolean
  uploadReady: boolean
  applyReady: boolean
  constructor(fileReady: boolean, transportReady: boolean, uploadReady: boolean, applyReady: boolean) {
    this.fileReady = fileReady
    this.transportReady = transportReady
    this.uploadReady = uploadReady
    this.applyReady = applyReady
  }

  get allReady(): boolean {
    return this.fileReady && this.transportReady && this.uploadReady && this.applyReady
  }
}

// ── Runtime Harness types ──

export enum CanonicalRuntimeHarnessNodeRole {
  IPHONE = 'iPhone',
  MAC = 'mac'
}

export class CanonicalRuntimeHarnessTickResult {
  constructor() {}
}

export class CanonicalRuntimeHarness {
  constructor() {}
  async runApplyTick(
    localRole: CanonicalRuntimeHarnessNodeRole,
    peerRole: CanonicalRuntimeHarnessNodeRole,
    trigger: CanonicalSyncPlanTrigger
  ): Promise<CanonicalRuntimeHarnessTickResult> {
    return new CanonicalRuntimeHarnessTickResult()
  }
}

// ── Canonical Manifest export from CanonicalCore ──

import { CanonicalManifest } from './CanonicalCore'

// ── Forward declaration for other output types ──

export class CanonicalProductionSnapshot {
  manifest: CanonicalManifest
  constructor(manifest: CanonicalManifest) {
    this.manifest = manifest
  }
}

export class CanonicalApplyPlan {
  constructor() {}
}

export class CanonicalLibrarySyncPlan {
  constructor() {}
}

export class CanonicalDryRunMigrationPlan {
  constructor() {}
}

export class CanonicalTransferProjection {
  constructor() {}
}

export class CanonicalLibraryProjection {
  constructor() {}
}

export class CanonicalRuntimeReadinessEvidence {
  constructor() {}
}

export class CanonicalRuntimeReadinessReport {
  constructor() {}
}

export class CanonicalRuntimeReadinessEvaluator {
  evaluate(evidence: CanonicalRuntimeReadinessEvidence): CanonicalRuntimeReadinessReport {
    return new CanonicalRuntimeReadinessReport()
  }
}

export class CanonicalDryRunMigrationContext {
  constructor() {}
}

export class CanonicalDryRunMigrationPlanner {
  plan(
    local: CanonicalProductionSnapshot,
    peer: CanonicalProductionSnapshot,
    ports: CanonicalProductionPortSet,
    currentRuntimeReadiness: CanonicalRuntimeReadinessReport,
    trigger: CanonicalSyncPlanTrigger,
    context: CanonicalDryRunMigrationContext
  ): CanonicalDryRunMigrationPlan {
    return new CanonicalDryRunMigrationPlan()
  }

  static equivalenceReport(
    syncPlan: CanonicalSyncPlan,
    applyPlan: CanonicalApplyPlan,
    libraryPlan: CanonicalLibrarySyncPlan,
    localLegacyActions: CanonicalLegacyActionSnapshot,
    portReadiness: CanonicalProductionPortReadiness
  ): CanonicalLegacyEquivalenceReport {
    return new CanonicalLegacyEquivalenceReport()
  }
}

export class CanonicalLegacyActionSnapshot {
  constructor() {}
}

export class CanonicalLegacyEquivalenceReport {
  constructor() {}
}

export class CanonicalApplyPlanner {
  plan(
    local: CanonicalManifest,
    peer: CanonicalManifest,
    syncPlan: CanonicalSyncPlan,
    trigger: CanonicalSyncPlanTrigger
  ): CanonicalApplyPlan {
    return new CanonicalApplyPlan()
  }
}

export class CanonicalLibrarySyncPlanner {
  plan(
    local: CanonicalManifest,
    peer: CanonicalManifest,
    trigger: CanonicalSyncPlanTrigger
  ): CanonicalLibrarySyncPlan {
    return new CanonicalLibrarySyncPlan()
  }
}

export class CanonicalObjectProjectionBuilder {
  static build(
    manifest: CanonicalManifest,
    applyPlan: CanonicalApplyPlan | null,
    libraryPlan: CanonicalLibrarySyncPlan | null,
    transferProjection: CanonicalTransferProjection | null
  ): CanonicalLibraryProjection {
    return new CanonicalLibraryProjection()
  }
}

import { CanonicalTransferJob, transferProjectionFromJobs } from './CanonicalTransferStateMachine'

export class CanonicalRollbackAudit {
  constructor(plan: CanonicalRollbackPlan | null = null, requiredDomains: CanonicalProductionDomain[] = []) {}
}

// ── Canonical Production Diagnostics Event ──

export class CanonicalProductionDiagnosticsEvent {
  constructor() {}
}

// ── Canonical Kernel Configuration & Environment ──

export class CanonicalKernelConfiguration {
  mode: CanonicalKernelExecutionMode
  productionPolicy: CanonicalProductionExecutionPolicy

  constructor(
    mode: CanonicalKernelExecutionMode = CanonicalKernelExecutionMode.DISABLED,
    productionPolicy: CanonicalProductionExecutionPolicy = new CanonicalProductionExecutionPolicy()
  ) {
    this.mode = mode
    this.productionPolicy = productionPolicy
  }
}

export class CanonicalKernelEnvironment {
  ports: CanonicalProductionPortSet
  runtimeHarness: CanonicalRuntimeHarness | null

  constructor(
    ports: CanonicalProductionPortSet = new CanonicalProductionPortSet(),
    runtimeHarness: CanonicalRuntimeHarness | null = null
  ) {
    this.ports = ports
    this.runtimeHarness = runtimeHarness
  }
}

// ── Canonical Kernel Audit Report ──

export class CanonicalKernelAuditReport {
  operation: CanonicalKernelOperation
  mode: CanonicalKernelExecutionMode
  generatedAt: CanonicalTimestamp
  productionAudit: CanonicalProductionExecutionAudit | null
  sideEffects: CanonicalProductionSideEffect[]
  diagnostics: CanonicalProductionDiagnosticsEvent[]

  constructor(
    operation: CanonicalKernelOperation,
    mode: CanonicalKernelExecutionMode,
    productionAudit: CanonicalProductionExecutionAudit | null = null,
    sideEffects: CanonicalProductionSideEffect[] = [],
    diagnostics: CanonicalProductionDiagnosticsEvent[] = [],
    generatedAt: Date = new Date()
  ) {
    this.operation = operation
    this.mode = mode
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.productionAudit = productionAudit
    this.sideEffects = sideEffects
    this.diagnostics = diagnostics
  }
}

// ── Canonical Kernel Operation Result ──

export class CanonicalKernelOperationResult<Payload> {
  operation: CanonicalKernelOperation
  mode: CanonicalKernelExecutionMode
  payload: Payload | null
  errors: CanonicalKernelError[]
  audit: CanonicalKernelAuditReport

  constructor(
    operation: CanonicalKernelOperation,
    mode: CanonicalKernelExecutionMode,
    payload: Payload | null,
    errors: CanonicalKernelError[],
    audit: CanonicalKernelAuditReport
  ) {
    this.operation = operation
    this.mode = mode
    this.payload = payload
    this.errors = errors
    this.audit = audit
  }

  get succeeded(): boolean {
    return this.errors.length === 0 && this.payload !== null
  }

  static success<T>(
    operation: CanonicalKernelOperation,
    mode: CanonicalKernelExecutionMode,
    payload: T,
    audit: CanonicalKernelAuditReport
  ): CanonicalKernelOperationResult<T> {
    return new CanonicalKernelOperationResult(operation, mode, payload, [], audit)
  }

  static failure<T>(
    operation: CanonicalKernelOperation,
    mode: CanonicalKernelExecutionMode,
    errors: CanonicalKernelError[],
    audit: CanonicalKernelAuditReport
  ): CanonicalKernelOperationResult<T> {
    return new CanonicalKernelOperationResult(operation, mode, null, errors, audit)
  }

  replacingPayload(payload: Payload): CanonicalKernelOperationResult<Payload> {
    return new CanonicalKernelOperationResult<Payload>(
      this.operation, this.mode, payload, [...this.errors], this.audit
    )
  }
}

// ── Canonical Kernel Input / Output ──

export class CanonicalKernelInput {
  localSnapshot: CanonicalProductionSnapshot | null
  peerSnapshot: CanonicalProductionSnapshot | null
  trigger: CanonicalSyncPlanTrigger

  constructor(
    localSnapshot: CanonicalProductionSnapshot | null = null,
    peerSnapshot: CanonicalProductionSnapshot | null = null,
    trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC
  ) {
    this.localSnapshot = localSnapshot
    this.peerSnapshot = peerSnapshot
    this.trigger = trigger
  }
}

export class CanonicalKernelOutput {
  manifest: CanonicalManifest | null
  syncPlan: CanonicalSyncPlan | null
  applyPlan: CanonicalApplyPlan | null
  libraryPlan: CanonicalLibrarySyncPlan | null
  dryRunPlan: CanonicalDryRunMigrationPlan | null
  productionResult: CanonicalProductionExecutionResult | null

  constructor(
    manifest: CanonicalManifest | null = null,
    syncPlan: CanonicalSyncPlan | null = null,
    applyPlan: CanonicalApplyPlan | null = null,
    libraryPlan: CanonicalLibrarySyncPlan | null = null,
    dryRunPlan: CanonicalDryRunMigrationPlan | null = null,
    productionResult: CanonicalProductionExecutionResult | null = null
  ) {
    this.manifest = manifest
    this.syncPlan = syncPlan
    this.applyPlan = applyPlan
    this.libraryPlan = libraryPlan
    this.dryRunPlan = dryRunPlan
    this.productionResult = productionResult
  }
}

// ── Canonical Production Execution Step ──

export class CanonicalProductionExecutionStep {
  stepID: string
  kind: CanonicalProductionSideEffectKind
  domain: CanonicalProductionDomain
  fileIntent: CanonicalFileWriteIntent | null
  transportRequest: CanonicalProductionTransportBuildRequest | null
  uploadStartRequest: CanonicalUploadStartRequest | null
  uploadChunk: CanonicalUploadChunk | null
  uploadFinalizeRequest: CanonicalUploadFinalizeRequest | null
  applyAction: CanonicalApplyAction | null
  tombstoneRequest: CanonicalProductionTombstoneRequest | null

  constructor(
    stepID: string,
    kind: CanonicalProductionSideEffectKind,
    domain: CanonicalProductionDomain,
    fileIntent: CanonicalFileWriteIntent | null = null,
    transportRequest: CanonicalProductionTransportBuildRequest | null = null,
    uploadStartRequest: CanonicalUploadStartRequest | null = null,
    uploadChunk: CanonicalUploadChunk | null = null,
    uploadFinalizeRequest: CanonicalUploadFinalizeRequest | null = null,
    applyAction: CanonicalApplyAction | null = null,
    tombstoneRequest: CanonicalProductionTombstoneRequest | null = null
  ) {
    this.stepID = CanonicalProductionRedaction.safeIdentifier(stepID, kind)
    this.kind = kind
    this.domain = domain
    this.fileIntent = fileIntent
    this.transportRequest = transportRequest
    this.uploadStartRequest = uploadStartRequest
    this.uploadChunk = uploadChunk
    this.uploadFinalizeRequest = uploadFinalizeRequest
    this.applyAction = applyAction
    this.tombstoneRequest = tombstoneRequest
  }

  get id(): string {
    return this.stepID
  }
}

// ── Canonical Production Execution Input ──

export class CanonicalProductionExecutionInput {
  operationID: string
  domains: CanonicalProductionDomain[]
  steps: CanonicalProductionExecutionStep[]
  rollbackPlan: CanonicalRollbackPlan | null
  dryRunReportID: string | null
  dryRunEquivalence: CanonicalDryRunEquivalenceReport | null
  readinessReport: CanonicalDryRunReadinessReport | null
  unresolvedConflictCount: number

  constructor(
    operationID: string,
    domains: CanonicalProductionDomain[],
    steps: CanonicalProductionExecutionStep[],
    rollbackPlan: CanonicalRollbackPlan | null = null,
    dryRunReportID: string | null = null,
    dryRunEquivalence: CanonicalDryRunEquivalenceReport | null = null,
    readinessReport: CanonicalDryRunReadinessReport | null = null,
    unresolvedConflictCount: number = 0
  ) {
    this.operationID = CanonicalProductionRedaction.safeIdentifier(operationID, 'production-operation')
    this.domains = [...new Set(domains)].sort()
    this.steps = steps
    this.rollbackPlan = rollbackPlan
    this.dryRunReportID = dryRunReportID
      ? CanonicalProductionRedaction.safeIdentifier(dryRunReportID, 'dry-run-report')
      : null
    this.dryRunEquivalence = dryRunEquivalence
    this.readinessReport = readinessReport
    this.unresolvedConflictCount = unresolvedConflictCount
  }
}

// ── Canonical Kernel Facade ──

export class CanonicalKernelFacade {
  configuration: CanonicalKernelConfiguration
  environment: CanonicalKernelEnvironment

  constructor(
    configuration: CanonicalKernelConfiguration = new CanonicalKernelConfiguration(),
    environment: CanonicalKernelEnvironment = new CanonicalKernelEnvironment()
  ) {
    this.configuration = configuration
    this.environment = environment
  }

  buildSnapshot(snapshot: CanonicalProductionSnapshot): CanonicalKernelOperationResult<CanonicalProductionSnapshot> {
    return this.success(CanonicalKernelOperation.BUILD_SNAPSHOT, snapshot)
  }

  buildManifest(snapshot: CanonicalProductionSnapshot): CanonicalKernelOperationResult<CanonicalManifest> {
    return this.success(CanonicalKernelOperation.BUILD_MANIFEST, snapshot.manifest)
  }

  planSync(
    local: CanonicalManifest,
    peer: CanonicalManifest,
    trigger: CanonicalSyncPlanTrigger
  ): CanonicalKernelOperationResult<CanonicalSyncPlan> {
    try {
      const plan = new CanonicalSyncPlanner().plan(local, peer, trigger)
      return this.success(CanonicalKernelOperation.PLAN_SYNC, plan)
    } catch (e) {
      return this.failure(CanonicalKernelOperation.PLAN_SYNC, CanonicalKernelError.operationFailed(String(e)))
    }
  }

  buildApplyPlan(
    local: CanonicalManifest,
    peer: CanonicalManifest,
    syncPlan: CanonicalSyncPlan,
    trigger: CanonicalSyncPlanTrigger
  ): CanonicalKernelOperationResult<CanonicalApplyPlan> {
    const planner = new CanonicalApplyPlanner()
    return this.success(CanonicalKernelOperation.BUILD_APPLY_PLAN, planner.plan(local, peer, syncPlan, trigger))
  }

  buildLibraryPlan(
    local: CanonicalManifest,
    peer: CanonicalManifest,
    trigger: CanonicalSyncPlanTrigger
  ): CanonicalKernelOperationResult<CanonicalLibrarySyncPlan> {
    const planner = new CanonicalLibrarySyncPlanner()
    return this.success(CanonicalKernelOperation.BUILD_LIBRARY_PLAN, planner.plan(local, peer, trigger))
  }

  buildTransferProjection(
    jobs: CanonicalTransferJob[]
  ): CanonicalKernelOperationResult<CanonicalTransferProjection> {
    return this.success(CanonicalKernelOperation.BUILD_TRANSFER_PROJECTION, transferProjectionFromJobs(jobs))
  }

  buildObjectProjection(
    manifest: CanonicalManifest,
    applyPlan: CanonicalApplyPlan | null = null,
    libraryPlan: CanonicalLibrarySyncPlan | null = null,
    transferProjection: CanonicalTransferProjection | null = null
  ): CanonicalKernelOperationResult<CanonicalLibraryProjection> {
    const projection = CanonicalObjectProjectionBuilder.build(manifest, applyPlan, libraryPlan, transferProjection)
    return this.success(CanonicalKernelOperation.BUILD_OBJECT_PROJECTION, projection)
  }

  buildRuntimeReadiness(
    evidence: CanonicalRuntimeReadinessEvidence
  ): CanonicalKernelOperationResult<CanonicalRuntimeReadinessReport> {
    const report = new CanonicalRuntimeReadinessEvaluator().evaluate(evidence)
    return this.success(CanonicalKernelOperation.BUILD_RUNTIME_READINESS, report)
  }

  buildProductionReadiness(
    ports: CanonicalProductionPortSet | null = null
  ): CanonicalKernelOperationResult<CanonicalProductionPortReadiness> {
    return this.success(
      CanonicalKernelOperation.BUILD_PRODUCTION_READINESS,
      (ports ?? this.environment.ports).readiness()
    )
  }

  dryRunMigration(
    local: CanonicalProductionSnapshot,
    peer: CanonicalProductionSnapshot,
    currentRuntimeReadiness: CanonicalRuntimeReadinessReport,
    trigger: CanonicalSyncPlanTrigger,
    context: CanonicalDryRunMigrationContext = new CanonicalDryRunMigrationContext()
  ): CanonicalKernelOperationResult<CanonicalDryRunMigrationPlan> {
    if (!allowsDryRunPlanning(this.configuration.mode)) {
      return this.failure(CanonicalKernelOperation.DRY_RUN_MIGRATION,
        CanonicalKernelError.modeNotAllowed(this.configuration.mode))
    }
    try {
      const planner = new CanonicalDryRunMigrationPlanner()
      const plan = planner.plan(local, peer, this.environment.ports, currentRuntimeReadiness, trigger, context)
      return this.success(CanonicalKernelOperation.DRY_RUN_MIGRATION, plan)
    } catch (e) {
      return this.failure(CanonicalKernelOperation.DRY_RUN_MIGRATION,
        CanonicalKernelError.operationFailed(String(e)))
    }
  }

  compareLegacy(
    syncPlan: CanonicalSyncPlan,
    applyPlan: CanonicalApplyPlan,
    libraryPlan: CanonicalLibrarySyncPlan,
    localLegacyActions: CanonicalLegacyActionSnapshot,
    portReadiness: CanonicalProductionPortReadiness
  ): CanonicalKernelOperationResult<CanonicalLegacyEquivalenceReport> {
    const report = CanonicalDryRunMigrationPlanner.equivalenceReport(
      syncPlan, applyPlan, libraryPlan, localLegacyActions, portReadiness
    )
    return this.success(CanonicalKernelOperation.COMPARE_LEGACY, report)
  }

  async executeOffline(
    localRole: CanonicalRuntimeHarnessNodeRole = CanonicalRuntimeHarnessNodeRole.IPHONE,
    peerRole: CanonicalRuntimeHarnessNodeRole = CanonicalRuntimeHarnessNodeRole.MAC,
    trigger: CanonicalSyncPlanTrigger = CanonicalSyncPlanTrigger.PERIODIC
  ): Promise<CanonicalKernelOperationResult<CanonicalRuntimeHarnessTickResult>> {
    if (this.configuration.mode !== CanonicalKernelExecutionMode.OFFLINE_RUNTIME) {
      return this.failure(CanonicalKernelOperation.EXECUTE_OFFLINE,
        CanonicalKernelError.modeNotAllowed(this.configuration.mode))
    }
    try {
      const harness = this.environment.runtimeHarness ?? new CanonicalRuntimeHarness()
      const result = await harness.runApplyTick(localRole, peerRole, trigger)
      return this.success(CanonicalKernelOperation.EXECUTE_OFFLINE, result)
    } catch (e) {
      return this.failure(CanonicalKernelOperation.EXECUTE_OFFLINE,
        CanonicalKernelError.operationFailed(String(e)))
    }
  }

  async executeProduction(
    input: CanonicalProductionExecutionInput,
    token: CanonicalProductionExecutionToken | null = null
  ): Promise<CanonicalKernelOperationResult<CanonicalProductionExecutionResult>> {
    const guardAudit = CanonicalProductionExecutionGuard.evaluate(
      this.configuration.mode, token, this.configuration.productionPolicy,
      input.domains, this.environment.ports, input.rollbackPlan,
      input.dryRunReportID, input.dryRunEquivalence, input.readinessReport,
      input.unresolvedConflictCount
    )
    if (!guardAudit.allowed) {
      const result = new CanonicalProductionExecutionResult(
        input.operationID, this.configuration.mode, false,
        [],
        [
          new CanonicalProductionExecutionFailure(
            input.operationID, undefined,
            guardAudit.rejectionReasons.join(',')
          )
        ],
        guardAudit
      )
      return CanonicalKernelOperationResult.failure<CanonicalProductionExecutionResult>(
        CanonicalKernelOperation.EXECUTE_PRODUCTION,
        this.configuration.mode,
        [CanonicalKernelError.productionExecutionRejected(guardAudit.rejectionReasons)],
        new CanonicalKernelAuditReport(
          CanonicalKernelOperation.EXECUTE_PRODUCTION,
          this.configuration.mode,
          guardAudit
        )
      ).replacingPayload(result)
    }

    const sideEffects: CanonicalProductionSideEffect[] = []
    const failures: CanonicalProductionExecutionFailure[] = []
    for (const step of input.steps) {
      try {
        const sideEffect = await this.execute(step)
        if (sideEffect) {
          sideEffects.push(sideEffect)
        }
      } catch (e) {
        failures.push(new CanonicalProductionExecutionFailure(
          step.stepID, step.domain, String(e)
        ))
      }
    }

    const result = new CanonicalProductionExecutionResult(
      input.operationID, this.configuration.mode,
      failures.length === 0,
      sideEffects, failures, guardAudit
    )
    const audit = new CanonicalKernelAuditReport(
      CanonicalKernelOperation.EXECUTE_PRODUCTION,
      this.configuration.mode,
      guardAudit,
      sideEffects
    )
    return failures.length === 0
      ? CanonicalKernelOperationResult.success(
        CanonicalKernelOperation.EXECUTE_PRODUCTION, this.configuration.mode, result, audit
      )
      : CanonicalKernelOperationResult.failure<CanonicalProductionExecutionResult>(
        CanonicalKernelOperation.EXECUTE_PRODUCTION, this.configuration.mode,
        failures.map(f => CanonicalKernelError.operationFailed(f.reason)),
        audit
      ).replacingPayload(result)
  }

  rollbackPreview(
    plan: CanonicalRollbackPlan | null,
    requiredDomains: CanonicalProductionDomain[]
  ): CanonicalKernelOperationResult<CanonicalRollbackAudit> {
    const audit = new CanonicalRollbackAudit(plan, requiredDomains)
    return this.success(CanonicalKernelOperation.ROLLBACK_PREVIEW, audit)
  }

  private async execute(step: CanonicalProductionExecutionStep): Promise<CanonicalProductionSideEffect | null> {
    switch (step.kind) {
      case CanonicalProductionSideEffectKind.FILE_READ: {
        const intent = step.fileIntent
        if (!intent) {
          throw CanonicalKernelError.missingInput(step.stepID)
        }
        await this.requiredFilePort().readMetadata(
          new CanonicalProductionMetadataReadRequest(step.stepID, intent.reference)
        )
        return new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.FILE_READ, step.domain,
          step.stepID, null, null, null, null, 'fileRead'
        )
      }
      case CanonicalProductionSideEffectKind.FILE_WRITE: {
        const intent = step.fileIntent
        if (!intent) {
          throw CanonicalKernelError.missingInput(step.stepID)
        }
        const writeResult = await this.requiredFilePort().writeMetadata(intent, null)
        return new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.FILE_WRITE, step.domain,
          step.stepID, null,
          writeResult.evidence.actualByteSize,
          writeResult.evidence.actualHashPrefix,
          null,
          `fileWrite:${writeResult.disposition}`
        )
      }
      case CanonicalProductionSideEffectKind.NETWORK_REQUEST: {
        const request = step.transportRequest
        if (!request) {
          throw CanonicalKernelError.missingInput(step.stepID)
        }
        const exchange = await this.requiredTransportPort().sendRequest(
          await this.requiredTransportPort().buildSignedRequest(request)
        )
        return exchange.sideEffect ?? new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.NETWORK_REQUEST, step.domain,
          null, null, null, null, null, 'networkRequest', request.route
        )
      }
      case CanonicalProductionSideEffectKind.UPLOAD_SESSION_START: {
        const request = step.uploadStartRequest
        if (!request) {
          throw CanonicalKernelError.missingInput(step.stepID)
        }
        const status = await this.requiredUploadPort().startResumableUpload(request, new Date())
        return new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.UPLOAD_SESSION_START, step.domain,
          request.objectID, null,
          status.fileSize, status.checksum?.value ?? null,
          null, 'uploadSessionStart'
        )
      }
      case CanonicalProductionSideEffectKind.UPLOAD_CHUNK_SEND: {
        const chunk = step.uploadChunk
        if (!chunk) {
          throw CanonicalKernelError.missingInput(step.stepID)
        }
        const status = await this.requiredUploadPort().uploadChunk(chunk, new Date())
        return new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.UPLOAD_CHUNK_SEND, step.domain,
          chunk.objectID, null,
          status.confirmedBytes, null, chunk.chunkHash, 'uploadChunkSend'
        )
      }
      case CanonicalProductionSideEffectKind.UPLOAD_FINALIZE: {
        const request = step.uploadFinalizeRequest
        if (!request) {
          throw CanonicalKernelError.missingInput(step.stepID)
        }
        const status = await this.requiredUploadPort().finalizeUpload(request, new Date())
        return new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.UPLOAD_FINALIZE, step.domain,
          request.objectID, null,
          status.fileSize, status.checksum?.value ?? null, null, 'uploadFinalize'
        )
      }
      case CanonicalProductionSideEffectKind.METADATA_APPLY: {
        const action = step.applyAction
        if (!action) {
          throw CanonicalKernelError.missingInput(step.stepID)
        }
        const request = new CanonicalProductionApplyExecutionRequest(action, null)
        let result: CanonicalProductionApplyResult
        switch (action.kind) {
          case 'recordingMetadataSend':
          case 'folderMetadataSend':
          case 'studyItemMetadataSend':
          case 'libraryTombstoneSend':
          case 'objectTombstoneSend':
            result = await this.requiredApplyPort().sendMetadata(request)
            break
          default:
            result = await this.requiredApplyPort().applyMetadata(request)
            break
        }
        return result.sideEffect ?? new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.METADATA_APPLY, step.domain,
          action.target.objectID, null, null, null, null, 'metadataApply'
        )
      }
      case CanonicalProductionSideEffectKind.GENERATED_ARTIFACT_APPLY: {
        const action = step.applyAction
        if (!action) {
          throw CanonicalKernelError.missingInput(step.stepID)
        }
        const result = await this.requiredApplyPort().applyGeneratedArtifact(
          new CanonicalProductionApplyExecutionRequest(action, null)
        )
        return result.sideEffect ?? new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.GENERATED_ARTIFACT_APPLY, step.domain,
          action.target.objectID, action.target.artifactID, null, null, null, 'generatedArtifactApply'
        )
      }
      case CanonicalProductionSideEffectKind.TOMBSTONE_MARK: {
        const request = step.tombstoneRequest
        if (!request) {
          throw CanonicalKernelError.missingInput(step.stepID)
        }
        const writeResult = await this.requiredFilePort().markTombstone(request)
        return new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.TOMBSTONE_MARK, step.domain,
          null, null,
          writeResult.evidence.actualByteSize,
          writeResult.evidence.actualHashPrefix,
          null, 'tombstoneMark'
        )
      }
      case CanonicalProductionSideEffectKind.CONFLICT_RECORD: {
        const action = step.applyAction
        if (!action) {
          throw CanonicalKernelError.missingInput(step.stepID)
        }
        const result = await this.requiredApplyPort().recordConflict(
          new CanonicalProductionApplyExecutionRequest(action, null)
        )
        return result.sideEffect ?? new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.CONFLICT_RECORD, step.domain,
          action.target.objectID, null, null, null, null, 'conflictRecord'
        )
      }
      case CanonicalProductionSideEffectKind.DIAGNOSTICS_WRITE:
        return new CanonicalProductionSideEffect(
          CanonicalProductionSideEffectKind.DIAGNOSTICS_WRITE, step.domain,
          null, null, null, null, null, 'diagnosticsWrite'
        )
    }
  }

  private requiredFilePort(): CanonicalProductionFilePort {
    const file = this.environment.ports.file
    if (!file) {
      throw CanonicalKernelError.portMissing(CanonicalProductionPortKind.FILE)
    }
    return file
  }

  private requiredTransportPort(): CanonicalProductionTransportPort {
    const transport = this.environment.ports.transport
    if (!transport) {
      throw CanonicalKernelError.portMissing(CanonicalProductionPortKind.TRANSPORT)
    }
    return transport
  }

  private requiredUploadPort(): CanonicalProductionUploadPort {
    const upload = this.environment.ports.upload
    if (!upload) {
      throw CanonicalKernelError.portMissing(CanonicalProductionPortKind.UPLOAD)
    }
    return upload
  }

  private requiredApplyPort(): CanonicalProductionApplyPort {
    const apply = this.environment.ports.apply
    if (!apply) {
      throw CanonicalKernelError.portMissing(CanonicalProductionPortKind.APPLY)
    }
    return apply
  }

  private success<T>(operation: CanonicalKernelOperation, payload: T): CanonicalKernelOperationResult<T> {
    return CanonicalKernelOperationResult.success(
      operation,
      this.configuration.mode,
      payload,
      new CanonicalKernelAuditReport(operation, this.configuration.mode)
    )
  }

  private failure<T>(operation: CanonicalKernelOperation, error: CanonicalKernelError): CanonicalKernelOperationResult<T> {
    return CanonicalKernelOperationResult.failure(
      operation,
      this.configuration.mode,
      [error],
      new CanonicalKernelAuditReport(operation, this.configuration.mode)
    )
  }
}
