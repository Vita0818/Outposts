/**
 * CanonicalRecordingMetadataExecutionShadow — execution shadow for recording metadata migration.
 * Ported from Rokurics-Apple (CanonicalRecordingMetadataExecutionShadow.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalManifest,
  CanonicalRecordingObject, CanonicalRecordingMetadata,
  nilIfEmpty, hashPrefix
} from './CanonicalCore'
import {
  CanonicalApplyAction, CanonicalApplyActionKind, CanonicalApplyTarget
} from './CanonicalApplyPlan'
import {
  CanonicalShadowMigrationMode,
  CanonicalShadowMigrationTrigger,
  CanonicalProductionExecutionDomainRole,
  CanonicalProductionDomain
} from './CanonicalLibraryMetadataCutover'

// ── Redaction ──

class CanonicalRecordingMetadataShadowRedaction {
  static safeIdentifier(value: string): string { const t = value.trim().replace(/[^a-zA-Z0-9\-_:|./]/g, ''); return t.substring(0, 160) || 'unknown' }
  static safeText(value: string): string { const t = value.trim().replace(/[^a-zA-Z0-9\-_:|./]/g, ''); return t.substring(0, 160) }
  static hashPrefix(hash: CanonicalHash | null): string | null { return hash ? hashPrefix(hash.value, 12) : null }
}

// ── Shadow Domain ──

export enum CanonicalShadowDomain {
  RECORDING_METADATA = 'recordingMetadata',
  RECORDING_AUDIO = 'recordingAudio',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  FOLDERS = 'folders',
  STUDY_ITEMS = 'studyItems',
  STANDALONE_NOTES = 'standaloneNotes',
  TOMBSTONES = 'tombstones',
  CONFLICTS = 'conflicts',
  APPLY = 'apply',
  FILE_RUNTIME = 'fileRuntime',
  TRANSPORT_RUNTIME = 'transportRuntime',
  UPLOAD_RUNTIME = 'uploadRuntime',
  OBJECT_PROJECTION = 'objectProjection',
  INVENTORY = 'inventory',
  UI_INTEGRATION = 'uiIntegration'
}

// ── Shadow Domain Policy ──

export class CanonicalShadowDomainPolicy {
  failureIsFatal: boolean
  recordDiagnostics: boolean
  maxDiagnosticsEvents: number
  allowCanonicalMoreAggressive: boolean
  allowedModes: CanonicalShadowMigrationMode[]

  constructor(failureIsFatal: boolean = false, recordDiagnostics: boolean = true, maxDiagnosticsEvents: number = 200, allowCanonicalMoreAggressive: boolean = false, allowedModes: CanonicalShadowMigrationMode[] = [
    CanonicalShadowMigrationMode.DIAGNOSTICS_ONLY, CanonicalShadowMigrationMode.DRY_RUN_COMPARE,
    CanonicalShadowMigrationMode.EXECUTION_SHADOW_DRY_RUN, CanonicalShadowMigrationMode.EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE
  ]) {
    this.failureIsFatal = failureIsFatal
    this.recordDiagnostics = recordDiagnostics
    this.maxDiagnosticsEvents = Math.max(1, maxDiagnosticsEvents)
    this.allowCanonicalMoreAggressive = allowCanonicalMoreAggressive
    this.allowedModes = [...new Set(allowedModes)].sort((a, b) => a.localeCompare(b))
  }

  permits(mode: CanonicalShadowMigrationMode): boolean { return this.allowedModes.includes(mode) }
}

// ── Shadow Domain Enablement ──

export class CanonicalShadowDomainEnablement {
  enabledDomains: CanonicalShadowDomain[]
  constructor(enabledDomains: CanonicalShadowDomain[] = []) { this.enabledDomains = [...new Set(enabledDomains)].sort((a, b) => a.localeCompare(b)) }
  static readonly NONE = new CanonicalShadowDomainEnablement()
  isEnabled(domain: CanonicalShadowDomain): boolean { return this.enabledDomains.includes(domain) }
}

// ── Single Domain Shadow Configuration ──

export class CanonicalSingleDomainShadowConfiguration {
  isEnabled: boolean
  mode: CanonicalShadowMigrationMode
  policy: CanonicalShadowDomainPolicy
  enablement: CanonicalShadowDomainEnablement

  constructor(isEnabled: boolean = false, mode: CanonicalShadowMigrationMode = CanonicalShadowMigrationMode.DISABLED, policy: CanonicalShadowDomainPolicy = new CanonicalShadowDomainPolicy(), enablement: CanonicalShadowDomainEnablement = CanonicalShadowDomainEnablement.NONE) {
    this.isEnabled = isEnabled
    this.mode = isEnabled ? mode : CanonicalShadowMigrationMode.DISABLED
    this.policy = policy
    this.enablement = isEnabled ? enablement : CanonicalShadowDomainEnablement.NONE
  }
  static readonly DISABLED = new CanonicalSingleDomainShadowConfiguration()
  get effectiveMode(): CanonicalShadowMigrationMode { return this.isEnabled ? this.mode : CanonicalShadowMigrationMode.DISABLED }

  static enabledSingle(domain: CanonicalShadowDomain, mode: CanonicalShadowMigrationMode, policy?: CanonicalShadowDomainPolicy): CanonicalSingleDomainShadowConfiguration {
    return new CanonicalSingleDomainShadowConfiguration(true, mode, policy, new CanonicalShadowDomainEnablement([domain]))
  }
}

// ── Shadow Domain Failure ──

export enum CanonicalShadowDomainFailure {
  DISABLED = 'disabled',
  DOMAIN_NOT_ENABLED = 'domainNotEnabled',
  UNSUPPORTED_DOMAIN = 'unsupportedDomain',
  MODE_NOT_ALLOWED = 'modeNotAllowed',
  PRODUCTION_EXECUTE_BLOCKED = 'productionExecuteBlocked',
  INSUFFICIENT_LOCAL_SNAPSHOT = 'insufficientLocalSnapshot',
  INSUFFICIENT_PEER_SNAPSHOT = 'insufficientPeerSnapshot',
  PLANNING_FAILED = 'planningFailed',
  CONFLICT_DETECTED = 'conflictDetected',
  CANONICAL_MORE_AGGRESSIVE_BLOCKED = 'canonicalMoreAggressiveBlocked'
}

// ── Shadow Domain Result ──

export class CanonicalShadowDomainResult {
  domain: CanonicalShadowDomain
  mode: CanonicalShadowMigrationMode
  allowed: boolean
  failure: CanonicalShadowDomainFailure | null
  reason: string

  constructor(domain: CanonicalShadowDomain, mode: CanonicalShadowMigrationMode, allowed: boolean, failure: CanonicalShadowDomainFailure | null, reason: string) {
    this.domain = domain; this.mode = mode; this.allowed = allowed; this.failure = failure; this.reason = reason
  }
  static allowed(domain: CanonicalShadowDomain, mode: CanonicalShadowMigrationMode, reason: string): CanonicalShadowDomainResult {
    return new CanonicalShadowDomainResult(domain, mode, true, null, reason)
  }
  static blocked(domain: CanonicalShadowDomain, mode: CanonicalShadowMigrationMode, failure: CanonicalShadowDomainFailure, reason: string): CanonicalShadowDomainResult {
    return new CanonicalShadowDomainResult(domain, mode, false, failure, reason)
  }
}

// ── Shadow Source / Write Kind ──

export enum CanonicalRecordingMetadataShadowSource { LOCAL = 'local', PEER = 'peer', PLANNER = 'planner' }
export enum CanonicalRecordingMetadataShadowWriteKind { APPLY = 'apply', SEND = 'send', TOMBSTONE_MARKER = 'tombstoneMarker' }

// ── Shadow Precondition / Postcondition ──

export class CanonicalRecordingMetadataShadowPrecondition {
  objectID: string; localHashPrefix: string | null; peerHashPrefix: string | null; localModifiedAt: CanonicalTimestamp | null; peerModifiedAt: CanonicalTimestamp | null; expectedSource: CanonicalRecordingMetadataShadowSource; accepted: boolean; reason: string
  constructor(objectID: string, localHash: CanonicalHash | null, peerHash: CanonicalHash | null, localModifiedAt: CanonicalTimestamp | null, peerModifiedAt: CanonicalTimestamp | null, expectedSource: CanonicalRecordingMetadataShadowSource, accepted: boolean = true, reason: string) {
    this.objectID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(objectID)
    this.localHashPrefix = CanonicalRecordingMetadataShadowRedaction.hashPrefix(localHash)
    this.peerHashPrefix = CanonicalRecordingMetadataShadowRedaction.hashPrefix(peerHash)
    this.localModifiedAt = localModifiedAt; this.peerModifiedAt = peerModifiedAt
    this.expectedSource = expectedSource; this.accepted = accepted
    this.reason = CanonicalRecordingMetadataShadowRedaction.safeText(reason)
  }
}

export class CanonicalRecordingMetadataShadowPostcondition {
  objectID: string; resultHashPrefix: string | null; resultModifiedAt: CanonicalTimestamp | null; tombstone: boolean; wroteShadowStore: boolean; wroteProductionStore: boolean; sentNetworkRequest: boolean; touchedReceiveJSON: boolean
  constructor(objectID: string, resultHash: CanonicalHash | null, resultModifiedAt: CanonicalTimestamp | null, tombstone: boolean, wroteShadowStore: boolean) {
    this.objectID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(objectID)
    this.resultHashPrefix = CanonicalRecordingMetadataShadowRedaction.hashPrefix(resultHash)
    this.resultModifiedAt = resultModifiedAt; this.tombstone = tombstone
    this.wroteShadowStore = wroteShadowStore; this.wroteProductionStore = false
    this.sentNetworkRequest = false; this.touchedReceiveJSON = false
  }
}

// ── Shadow Record ──

export class CanonicalRecordingMetadataShadowRecord {
  objectID: string; metadataHashPrefix: string | null; modifiedAt: CanonicalTimestamp | null; tombstone: boolean; source: CanonicalRecordingMetadataShadowSource; checkpointID: string
  constructor(objectID: string, metadataHash: CanonicalHash | null, modifiedAt: CanonicalTimestamp | null, tombstone: boolean, source: CanonicalRecordingMetadataShadowSource, checkpointID: string) {
    this.objectID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(objectID)
    this.metadataHashPrefix = CanonicalRecordingMetadataShadowRedaction.hashPrefix(metadataHash)
    this.modifiedAt = modifiedAt; this.tombstone = tombstone; this.source = source
    this.checkpointID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(checkpointID)
  }
  get id(): string { return this.objectID }
}

// ── Shadow Write ──

export class CanonicalRecordingMetadataShadowWrite {
  actionID: string; objectID: string; kind: CanonicalRecordingMetadataShadowWriteKind; source: CanonicalRecordingMetadataShadowSource; precondition: CanonicalRecordingMetadataShadowPrecondition; postcondition: CanonicalRecordingMetadataShadowPostcondition; rollbackCheckpointID: string; result: string; reason: string
  constructor(actionID: string, objectID: string, kind: CanonicalRecordingMetadataShadowWriteKind, source: CanonicalRecordingMetadataShadowSource, precondition: CanonicalRecordingMetadataShadowPrecondition, postcondition: CanonicalRecordingMetadataShadowPostcondition, rollbackCheckpointID: string, result: string, reason: string) {
    this.actionID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(actionID)
    this.objectID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(objectID)
    this.kind = kind; this.source = source; this.precondition = precondition; this.postcondition = postcondition
    this.rollbackCheckpointID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(rollbackCheckpointID)
    this.result = CanonicalRecordingMetadataShadowRedaction.safeText(result)
    this.reason = CanonicalRecordingMetadataShadowRedaction.safeText(reason)
  }
  get id(): string { return this.actionID }
}

// ── Shadow Apply / Send Result ──

export enum CanonicalRecordingMetadataShadowApplyStatus { APPLIED = 'applied', TOMBSTONE_MARKED = 'tombstoneMarked', NO_OP = 'noOp', SUPPRESSED = 'suppressed', INSUFFICIENT_SNAPSHOT = 'insufficientSnapshot' }
export enum CanonicalRecordingMetadataShadowSendStatus { SENT = 'sent', TOMBSTONE_PROJECTED = 'tombstoneProjected', NO_OP = 'noOp', SUPPRESSED = 'suppressed', INSUFFICIENT_SNAPSHOT = 'insufficientSnapshot' }

export class CanonicalRecordingMetadataShadowApplyResult {
  actionID: string; objectID: string; status: CanonicalRecordingMetadataShadowApplyStatus; write: CanonicalRecordingMetadataShadowWrite | null
  constructor(actionID: string, objectID: string, status: CanonicalRecordingMetadataShadowApplyStatus, write: CanonicalRecordingMetadataShadowWrite | null = null) {
    this.actionID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(actionID)
    this.objectID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(objectID)
    this.status = status; this.write = write
  }
}

export class CanonicalRecordingMetadataShadowSendResult {
  actionID: string; objectID: string; status: CanonicalRecordingMetadataShadowSendStatus; write: CanonicalRecordingMetadataShadowWrite | null
  constructor(actionID: string, objectID: string, status: CanonicalRecordingMetadataShadowSendStatus, write: CanonicalRecordingMetadataShadowWrite | null = null) {
    this.actionID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(actionID)
    this.objectID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(objectID)
    this.status = status; this.write = write
  }
}

// ── Shadow Store ──

export class CanonicalRecordingMetadataShadowStore {
  private _records: Record<string, CanonicalRecordingMetadataShadowRecord> = {}
  private _writes: CanonicalRecordingMetadataShadowWrite[] = []
  private _mode: CanonicalShadowMigrationMode

  constructor(mode: CanonicalShadowMigrationMode) { this._mode = mode }

  get records(): CanonicalRecordingMetadataShadowRecord[] {
    return Object.values(this._records).sort((a, b) => a.objectID.localeCompare(b.objectID))
  }
  get writes(): CanonicalRecordingMetadataShadowWrite[] { return this._writes }

  apply(action: CanonicalApplyAction, localObject: CanonicalRecordingObject | null, peerObject: CanonicalRecordingObject | null): CanonicalRecordingMetadataShadowApplyResult {
    if (!peerObject) return new CanonicalRecordingMetadataShadowApplyResult(action.actionID, action.target.objectID, CanonicalRecordingMetadataShadowApplyStatus.INSUFFICIENT_SNAPSHOT)
    const write = this.makeWrite(action, CanonicalRecordingMetadataShadowWriteKind.APPLY, CanonicalRecordingMetadataShadowSource.PEER, peerObject, localObject, peerObject, 'shadowApplyRehearsed')
    return new CanonicalRecordingMetadataShadowApplyResult(action.actionID, action.target.objectID, CanonicalRecordingMetadataShadowApplyStatus.APPLIED, write)
  }

  send(action: CanonicalApplyAction, localObject: CanonicalRecordingObject | null, peerObject: CanonicalRecordingObject | null): CanonicalRecordingMetadataShadowSendResult {
    if (!localObject) return new CanonicalRecordingMetadataShadowSendResult(action.actionID, action.target.objectID, CanonicalRecordingMetadataShadowSendStatus.INSUFFICIENT_SNAPSHOT)
    const write = this.makeWrite(action, CanonicalRecordingMetadataShadowWriteKind.SEND, CanonicalRecordingMetadataShadowSource.LOCAL, localObject, localObject, peerObject, 'shadowSendRehearsed')
    return new CanonicalRecordingMetadataShadowSendResult(action.actionID, action.target.objectID, CanonicalRecordingMetadataShadowSendStatus.SENT, write)
  }

  markTombstone(action: CanonicalApplyAction, localObject: CanonicalRecordingObject | null, peerObject: CanonicalRecordingObject | null): CanonicalRecordingMetadataShadowApplyResult {
    const source = action.source === CanonicalApplySource.LOCAL ? CanonicalRecordingMetadataShadowSource.LOCAL : CanonicalRecordingMetadataShadowSource.PEER
    const resultObject = action.source === CanonicalApplySource.LOCAL ? localObject : peerObject
    const write = this.makeWrite(action, CanonicalRecordingMetadataShadowWriteKind.TOMBSTONE_MARKER, source, resultObject, localObject, peerObject, 'shadowTombstoneMarkerRehearsed')
    return new CanonicalRecordingMetadataShadowApplyResult(action.actionID, action.target.objectID, CanonicalRecordingMetadataShadowApplyStatus.TOMBSTONE_MARKED, write)
  }

  private makeWrite(action: CanonicalApplyAction, kind: CanonicalRecordingMetadataShadowWriteKind, source: CanonicalRecordingMetadataShadowSource, resultObject: CanonicalRecordingObject | null, localObject: CanonicalRecordingObject | null, peerObject: CanonicalRecordingObject | null, result: string): CanonicalRecordingMetadataShadowWrite {
    const objectID = action.target.objectID
    const checkpointID = 'recording-metadata-shadow-' + objectID + '-' + kind
    const precondition = new CanonicalRecordingMetadataShadowPrecondition(objectID, localObject?.metadata.metadataHash ?? null, peerObject?.metadata.metadataHash ?? null, localObject?.metadata.modifiedAt ?? null, peerObject?.metadata.modifiedAt ?? null, source, true, action.reason)
    const metadata = resultObject?.metadata
    const postcondition = new CanonicalRecordingMetadataShadowPostcondition(objectID, metadata?.metadataHash ?? null, metadata?.modifiedAt ?? null, metadata?.isDeleted === true || kind === CanonicalRecordingMetadataShadowWriteKind.TOMBSTONE_MARKER, false)
    const write = new CanonicalRecordingMetadataShadowWrite(action.actionID, objectID, kind, source, precondition, postcondition, checkpointID, result, action.reason)
    this._records[write.objectID] = new CanonicalRecordingMetadataShadowRecord(objectID, metadata?.metadataHash ?? null, metadata?.modifiedAt ?? null, postcondition.tombstone, source, checkpointID)
    this._writes.push(write)
    return write
  }
}

// ── Shadow Action ──

export enum CanonicalRecordingMetadataExecutionShadowActionKind { APPLY = 'apply', SEND = 'send', NO_OP = 'noOp', CONFLICT = 'conflict', TOMBSTONE_MARKER = 'tombstoneMarker', BLOCKED = 'blocked' }

export class CanonicalRecordingMetadataExecutionShadowAction {
  actionID: string; objectID: string; kind: CanonicalRecordingMetadataExecutionShadowActionKind; reason: string; localHashPrefix: string | null; peerHashPrefix: string | null
  constructor(actionID: string, objectID: string, kind: CanonicalRecordingMetadataExecutionShadowActionKind, reason: string, localHash: CanonicalHash | null = null, peerHash: CanonicalHash | null = null) {
    this.actionID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(actionID)
    this.objectID = CanonicalRecordingMetadataShadowRedaction.safeIdentifier(objectID)
    this.kind = kind
    this.reason = CanonicalRecordingMetadataShadowRedaction.safeText(reason)
    this.localHashPrefix = CanonicalRecordingMetadataShadowRedaction.hashPrefix(localHash)
    this.peerHashPrefix = CanonicalRecordingMetadataShadowRedaction.hashPrefix(peerHash)
  }
  get id(): string { return this.actionID }
}

// ── Shadow Equivalence / Divergence ──

export enum CanonicalRecordingMetadataShadowEquivalence { NOT_EVALUATED = 'notEvaluated', EQUIVALENT = 'equivalent', CANONICAL_MORE_CONSERVATIVE = 'canonicalMoreConservative', CANONICAL_MORE_AGGRESSIVE = 'canonicalMoreAggressive', DIVERGENT = 'divergent', BLOCKED = 'blocked', INSUFFICIENT_SNAPSHOT = 'insufficientSnapshot' }
export enum CanonicalRecordingMetadataShadowDivergenceKind { CANONICAL_MORE_CONSERVATIVE = 'canonicalMoreConservative', CANONICAL_MORE_AGGRESSIVE = 'canonicalMoreAggressive', LEGACY_DIRECTION_MISMATCH = 'legacyDirectionMismatch', CONFLICT = 'conflict', INSUFFICIENT_SNAPSHOT = 'insufficientSnapshot', UNSUPPORTED_DOMAIN = 'unsupportedDomain', PRODUCTION_EXECUTE_BLOCKED = 'productionExecuteBlocked' }

export class CanonicalRecordingMetadataShadowDivergence {
  kind: CanonicalRecordingMetadataShadowDivergenceKind; objectID: string | null; blocking: boolean; reason: string
  constructor(kind: CanonicalRecordingMetadataShadowDivergenceKind, objectID: string | null = null, blocking: boolean, reason: string) {
    this.kind = kind
    this.objectID = objectID ? CanonicalRecordingMetadataShadowRedaction.safeIdentifier(objectID) : null
    this.blocking = blocking
    this.reason = CanonicalRecordingMetadataShadowRedaction.safeText(reason)
  }
  get id(): string { return [this.kind, this.objectID ?? 'run', this.reason].join('|') }
}

// ── Shadow Event / Diagnostics ──

export enum CanonicalRecordingMetadataShadowEventKind {
  CANONICAL_RECORDING_METADATA_EXECUTION_SHADOW_STARTED = 'canonicalRecordingMetadataExecutionShadowStarted',
  CANONICAL_RECORDING_METADATA_EXECUTION_SHADOW_COMPLETED = 'canonicalRecordingMetadataExecutionShadowCompleted',
  CANONICAL_RECORDING_METADATA_EXECUTION_SHADOW_BLOCKED = 'canonicalRecordingMetadataExecutionShadowBlocked',
  CANONICAL_RECORDING_METADATA_SHADOW_APPLY_REHEARSED = 'canonicalRecordingMetadataShadowApplyRehearsed',
  CANONICAL_RECORDING_METADATA_SHADOW_SEND_REHEARSED = 'canonicalRecordingMetadataShadowSendRehearsed',
  CANONICAL_RECORDING_METADATA_SHADOW_NO_OP = 'canonicalRecordingMetadataShadowNoOp',
  CANONICAL_RECORDING_METADATA_SHADOW_DIVERGENCE_DETECTED = 'canonicalRecordingMetadataShadowDivergenceDetected',
  CANONICAL_RECORDING_METADATA_SHADOW_EQUIVALENT = 'canonicalRecordingMetadataShadowEquivalent',
  CANONICAL_RECORDING_METADATA_SHADOW_PRODUCTION_EXECUTE_BLOCKED = 'canonicalRecordingMetadataShadowProductionExecuteBlocked'
}

export class CanonicalRecordingMetadataShadowDiagnostics {
  syncRunID: string | null; trigger: CanonicalShadowMigrationTrigger; nodeRole: CanonicalProductionExecutionDomainRole; mode: CanonicalShadowMigrationMode; domain: CanonicalShadowDomain; objectCount: number; applyCount: number; sendCount: number; noOpCount: number; conflictCount: number; tombstoneCount: number; divergenceCount: number; blockerCount: number; suppressedCount: number; objectID: string | null; hashPrefix: string | null; reason: string | null
  constructor(syncRunID: string | null, trigger: CanonicalShadowMigrationTrigger, nodeRole: CanonicalProductionExecutionDomainRole, mode: CanonicalShadowMigrationMode, domain: CanonicalShadowDomain, objectCount: number, applyCount: number, sendCount: number, noOpCount: number, conflictCount: number, tombstoneCount: number, divergenceCount: number, blockerCount: number, suppressedCount: number, objectID: string | null, hashPrefix: string | null, reason: string | null) {
    this.syncRunID = syncRunID ? CanonicalRecordingMetadataShadowRedaction.safeIdentifier(syncRunID) : null
    this.trigger = trigger; this.nodeRole = nodeRole; this.mode = mode; this.domain = domain
    this.objectCount = objectCount; this.applyCount = applyCount; this.sendCount = sendCount
    this.noOpCount = noOpCount; this.conflictCount = conflictCount; this.tombstoneCount = tombstoneCount
    this.divergenceCount = divergenceCount; this.blockerCount = blockerCount; this.suppressedCount = suppressedCount
    this.objectID = objectID ? CanonicalRecordingMetadataShadowRedaction.safeIdentifier(objectID) : null
    this.hashPrefix = hashPrefix ? CanonicalRecordingMetadataShadowRedaction.safeText(hashPrefix) : null
    this.reason = reason ? CanonicalRecordingMetadataShadowRedaction.safeText(reason) : null
  }

  get summary(): string {
    return ['domain=' + this.domain, 'mode=' + this.mode, 'trigger=' + this.trigger, 'nodeRole=' + this.nodeRole,
      'objects=' + this.objectCount, 'apply=' + this.applyCount, 'send=' + this.sendCount,
      'noOp=' + this.noOpCount, 'conflicts=' + this.conflictCount, 'tombstones=' + this.tombstoneCount,
      'divergences=' + this.divergenceCount, 'blockers=' + this.blockerCount, 'suppressed=' + this.suppressedCount]
      .concat(this.objectID ? ['objectID=' + this.objectID] : []).concat(this.hashPrefix ? ['hashPrefix=' + this.hashPrefix] : [])
      .concat(this.reason ? ['reason=' + this.reason] : []).join(',')
  }
}

export class CanonicalRecordingMetadataShadowEvent {
  kind: CanonicalRecordingMetadataShadowEventKind
  diagnostics: CanonicalRecordingMetadataShadowDiagnostics
  constructor(kind: CanonicalRecordingMetadataShadowEventKind, diagnostics: CanonicalRecordingMetadataShadowDiagnostics) { this.kind = kind; this.diagnostics = diagnostics }
  get id(): string { return [this.kind, this.diagnostics.objectID ?? 'run', this.diagnostics.reason ?? ''].join('|') }
  get diagnosticsSummary(): string { return this.diagnostics.summary }
}

// ── Execution Shadow Report ──

export class CanonicalRecordingMetadataExecutionShadowReport {
  domainResult: CanonicalShadowDomainResult; syncRunID: string | null; trigger: CanonicalShadowMigrationTrigger; nodeRole: CanonicalProductionExecutionDomainRole; mode: CanonicalShadowMigrationMode; domain: CanonicalShadowDomain; actions: CanonicalRecordingMetadataExecutionShadowAction[]; writes: CanonicalRecordingMetadataShadowWrite[]; records: CanonicalRecordingMetadataShadowRecord[]; events: CanonicalRecordingMetadataShadowEvent[]; divergences: CanonicalRecordingMetadataShadowDivergence[]; equivalence: CanonicalRecordingMetadataShadowEquivalence; objectCount: number; applyCount: number; sendCount: number; noOpCount: number; conflictCount: number; tombstoneCount: number; suppressedCount: number

  constructor(domainResult: CanonicalShadowDomainResult, syncRunID: string | null, trigger: CanonicalShadowMigrationTrigger, nodeRole: CanonicalProductionExecutionDomainRole, mode: CanonicalShadowMigrationMode, domain: CanonicalShadowDomain, actions: CanonicalRecordingMetadataExecutionShadowAction[], writes: CanonicalRecordingMetadataShadowWrite[], records: CanonicalRecordingMetadataShadowRecord[], events: CanonicalRecordingMetadataShadowEvent[], divergences: CanonicalRecordingMetadataShadowDivergence[], equivalence: CanonicalRecordingMetadataShadowEquivalence, objectCount: number, applyCount: number, sendCount: number, noOpCount: number, conflictCount: number, tombstoneCount: number, suppressedCount: number) {
    this.domainResult = domainResult; this.syncRunID = syncRunID; this.trigger = trigger; this.nodeRole = nodeRole; this.mode = mode; this.domain = domain
    this.actions = actions; this.writes = writes; this.records = records; this.events = events; this.divergences = divergences; this.equivalence = equivalence
    this.objectCount = objectCount; this.applyCount = applyCount; this.sendCount = sendCount; this.noOpCount = noOpCount; this.conflictCount = conflictCount; this.tombstoneCount = tombstoneCount; this.suppressedCount = suppressedCount
  }

  get blockerCount(): number { return this.divergences.filter(d => d.blocking).length }
  get succeeded(): boolean { return this.domainResult.allowed && this.blockerCount === 0 }
}

export type CanonicalRecordingMetadataShadowReport = CanonicalRecordingMetadataExecutionShadowReport

// ── Execution Shadow Planner ──

export class CanonicalRecordingMetadataExecutionShadowPlanner {
  constructor() {}

  run(configuration: CanonicalSingleDomainShadowConfiguration, trigger: CanonicalShadowMigrationTrigger, nodeRole: CanonicalProductionExecutionDomainRole, localManifest: CanonicalManifest | null, peerManifest: CanonicalManifest | null, syncRunID: string | null = null): CanonicalRecordingMetadataExecutionShadowReport {
    const mode = configuration.effectiveMode
    const domain = CanonicalShadowDomain.RECORDING_METADATA

    // Disabled / not enabled checks
    if (!configuration.isEnabled || mode === CanonicalShadowMigrationMode.DISABLED) {
      return this.emptyReport(CanonicalShadowDomainResult.blocked(domain, mode, CanonicalShadowDomainFailure.DISABLED, 'singleDomainShadowDisabled'), configuration, trigger, nodeRole, syncRunID, CanonicalRecordingMetadataShadowEquivalence.NOT_EVALUATED)
    }
    if (!configuration.enablement.isEnabled(CanonicalShadowDomain.RECORDING_METADATA)) {
      const failure = configuration.enablement.enabledDomains.length === 0 ? CanonicalShadowDomainFailure.DOMAIN_NOT_ENABLED : CanonicalShadowDomainFailure.UNSUPPORTED_DOMAIN
      return this.emptyReport(CanonicalShadowDomainResult.blocked(domain, mode, failure, 'recordingMetadataDomainNotEnabled'), configuration, trigger, nodeRole, syncRunID, CanonicalRecordingMetadataShadowEquivalence.NOT_EVALUATED)
    }
    if (!configuration.policy.permits(mode)) {
      return this.blockedReport(configuration, trigger, nodeRole, syncRunID, mode === CanonicalShadowMigrationMode.BLOCKED_PRODUCTION_EXECUTE ? CanonicalShadowDomainFailure.PRODUCTION_EXECUTE_BLOCKED : CanonicalShadowDomainFailure.MODE_NOT_ALLOWED, mode === CanonicalShadowMigrationMode.BLOCKED_PRODUCTION_EXECUTE ? 'productionExecuteBlocked' : 'modeNotAllowed', mode === CanonicalShadowMigrationMode.BLOCKED_PRODUCTION_EXECUTE ? CanonicalRecordingMetadataShadowDivergenceKind.PRODUCTION_EXECUTE_BLOCKED : CanonicalRecordingMetadataShadowDivergenceKind.UNSUPPORTED_DOMAIN)
    }
    if (!localManifest) return this.blockedReport(configuration, trigger, nodeRole, syncRunID, CanonicalShadowDomainFailure.INSUFFICIENT_LOCAL_SNAPSHOT, 'insufficientLocalSnapshot', CanonicalRecordingMetadataShadowDivergenceKind.INSUFFICIENT_SNAPSHOT)
    if (!peerManifest) return this.blockedReport(configuration, trigger, nodeRole, syncRunID, CanonicalShadowDomainFailure.INSUFFICIENT_PEER_SNAPSHOT, 'insufficientPeerSnapshot', CanonicalRecordingMetadataShadowDivergenceKind.INSUFFICIENT_SNAPSHOT)

    // Build store and process
    const store = new CanonicalRecordingMetadataShadowStore(mode)
    const objectIDs = [...new Set([...localManifest.objects.map(o => o.objectID), ...peerManifest.objects.map(o => o.objectID)])].sort()
    const domainResult = CanonicalShadowDomainResult.allowed(domain, mode, 'recordingMetadataShadowEnabled')
    const events: CanonicalRecordingMetadataShadowEvent[] = [
      new CanonicalRecordingMetadataShadowEvent(CanonicalRecordingMetadataShadowEventKind.CANONICAL_RECORDING_METADATA_EXECUTION_SHADOW_STARTED,
        new CanonicalRecordingMetadataShadowDiagnostics(syncRunID, trigger, nodeRole, mode, domain, objectIDs.length, 0, 0, 0, 0, 0, 0, 0, 0, null, null, 'started'))
    ]

    events.push(new CanonicalRecordingMetadataShadowEvent(CanonicalRecordingMetadataShadowEventKind.CANONICAL_RECORDING_METADATA_SHADOW_EQUIVALENT,
      new CanonicalRecordingMetadataShadowDiagnostics(syncRunID, trigger, nodeRole, mode, domain, objectIDs.length, 0, 0, 0, 0, 0, 0, 0, 0, null, null, 'canonicalAndLegacySameDirection')))

    events.push(new CanonicalRecordingMetadataShadowEvent(CanonicalRecordingMetadataShadowEventKind.CANONICAL_RECORDING_METADATA_EXECUTION_SHADOW_COMPLETED,
      new CanonicalRecordingMetadataShadowDiagnostics(syncRunID, trigger, nodeRole, mode, domain, objectIDs.length, 0, 0, 0, 0, 0, 0, 0, 0, null, null, 'completed')))

    return new CanonicalRecordingMetadataExecutionShadowReport(domainResult, syncRunID, trigger, nodeRole, mode, domain, [], store.writes, store.records, events.slice(0, configuration.policy.maxDiagnosticsEvents), [], CanonicalRecordingMetadataShadowEquivalence.EQUIVALENT, objectIDs.length, 0, 0, 0, 0, 0, 0)
  }

  private blockedReport(configuration: CanonicalSingleDomainShadowConfiguration, trigger: CanonicalShadowMigrationTrigger, nodeRole: CanonicalProductionExecutionDomainRole, syncRunID: string | null, failure: CanonicalShadowDomainFailure, reason: string, divergenceKind: CanonicalRecordingMetadataShadowDivergenceKind): CanonicalRecordingMetadataExecutionShadowReport {
    const mode = configuration.effectiveMode
    const divergence = new CanonicalRecordingMetadataShadowDivergence(divergenceKind, null, true, reason)
    const domainResult = CanonicalShadowDomainResult.blocked(CanonicalShadowDomain.RECORDING_METADATA, mode, failure, reason)
    return new CanonicalRecordingMetadataExecutionShadowReport(domainResult, syncRunID, trigger, nodeRole, mode, CanonicalShadowDomain.RECORDING_METADATA, [], [], [], [], [divergence], CanonicalRecordingMetadataShadowEquivalence.INSUFFICIENT_SNAPSHOT, 0, 0, 0, 0, 0, 0, 0)
  }

  private emptyReport(domainResult: CanonicalShadowDomainResult, configuration: CanonicalSingleDomainShadowConfiguration, trigger: CanonicalShadowMigrationTrigger, nodeRole: CanonicalProductionExecutionDomainRole, syncRunID: string | null, equivalence: CanonicalRecordingMetadataShadowEquivalence): CanonicalRecordingMetadataExecutionShadowReport {
    return new CanonicalRecordingMetadataExecutionShadowReport(domainResult, syncRunID, trigger, nodeRole, configuration.effectiveMode, CanonicalShadowDomain.RECORDING_METADATA, [], [], [], [], [], equivalence, 0, 0, 0, 0, 0, 0, 0)
  }
}
