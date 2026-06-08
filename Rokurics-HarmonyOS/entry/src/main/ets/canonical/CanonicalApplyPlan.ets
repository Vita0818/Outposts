/**
 * CanonicalApplyPlan — apply plan types and planner.
 * Ported from Rokurics-Apple (CanonicalApplyPlan.swift).
 * Generates apply plans from local/peer manifests and sync plans.
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalManifest, CanonicalRecordingObject,
  CanonicalArtifactKind, nilIfEmpty, hashPrefix, timestampString, generatedArtifactKinds
} from './CanonicalCore'

// ── Canonical Apply Action Kind ──

export enum CanonicalApplyActionKind {
  RECORDING_METADATA_APPLY = 'recordingMetadataApply',
  RECORDING_METADATA_SEND = 'recordingMetadataSend',
  FOLDER_METADATA_APPLY = 'folderMetadataApply',
  FOLDER_METADATA_SEND = 'folderMetadataSend',
  STUDY_ITEM_METADATA_APPLY = 'studyItemMetadataApply',
  STUDY_ITEM_METADATA_SEND = 'studyItemMetadataSend',
  LIBRARY_TOMBSTONE_APPLY = 'libraryTombstoneApply',
  LIBRARY_TOMBSTONE_SEND = 'libraryTombstoneSend',
  GENERATED_ARTIFACT_DOWNLOAD_APPLY = 'generatedArtifactDownloadApply',
  GENERATED_ARTIFACT_NO_OP = 'generatedArtifactNoOp',
  OBJECT_TOMBSTONE_APPLY = 'objectTombstoneApply',
  OBJECT_TOMBSTONE_SEND = 'objectTombstoneSend',
  ARTIFACT_TOMBSTONE_APPLY = 'artifactTombstoneApply',
  CONFLICT_RECORD = 'conflictRecord',
  DEFERRED_UNSUPPORTED = 'deferredUnsupported'
}

// ── Canonical Apply Source ──

export enum CanonicalApplySource {
  LOCAL = 'local',
  PEER = 'peer',
  PLANNER = 'planner'
}

// ── Canonical Apply Target ──

export class CanonicalApplyTarget {
  objectID: string
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null

  constructor(
    objectID: string,
    artifactID: string | null = null,
    artifactKind: CanonicalArtifactKind | null = null
  ) {
    this.objectID = normalizedApplyRequired(objectID, 'unknown-recording')
    this.artifactID = nilIfEmpty((artifactID ?? '').trim()) ?? null
    this.artifactKind = artifactKind
  }
}

function normalizedApplyRequired(value: string, fallback: string): string {
  const trimmed = value.trim()
  return nilIfEmpty(trimmed) ?? fallback
}

// ── Canonical Apply Precondition ──

export enum CanonicalApplyPreconditionKind {
  LOCAL_MODIFIED_AT = 'localModifiedAt',
  PEER_MODIFIED_AT = 'peerModifiedAt',
  LOCAL_HASH_PREFIX = 'localHashPrefix',
  PEER_HASH_PREFIX = 'peerHashPrefix',
  PEER_BYTE_SIZE = 'peerByteSize',
  TOMBSTONE_TIMESTAMP = 'tombstoneTimestamp',
  NO_PHYSICAL_DELETE = 'noPhysicalDelete',
  LEGACY_BRIDGE = 'legacyBridge'
}

export class CanonicalApplyPrecondition {
  kind: CanonicalApplyPreconditionKind
  value: string

  constructor(kind: CanonicalApplyPreconditionKind, value: string) {
    this.kind = kind
    this.value = value.trim()
  }
}

// ── Canonical Apply Result ──

export enum CanonicalApplyResult {
  PLANNED = 'planned',
  NO_OP = 'noOp',
  CONFLICT_RECORDED = 'conflictRecorded',
  DEFERRED_UNSUPPORTED = 'deferredUnsupported'
}

// ── Canonical Apply Failure Reason ──

export enum CanonicalApplyFailureReason {
  UNSUPPORTED_ROUTE = 'unsupportedRoute',
  CONFLICT_DETECTED = 'conflictDetected',
  TOMBSTONE_BLOCKS_RESURRECTION = 'tombstoneBlocksResurrection',
  LEGACY_ARTIFACT_MISSING = 'legacyArtifactMissing',
  NO_PHYSICAL_DELETE_POLICY = 'noPhysicalDeletePolicy',
  HASH_OR_SIZE_MISMATCH = 'hashOrSizeMismatch'
}

// ── Canonical Apply Bridge Hint ──

export enum CanonicalApplyBridgeHint {
  LEGACY_METADATA_MANIFEST_APPLY = 'legacyMetadataManifestApply',
  LEGACY_METADATA_MANIFEST_SEND = 'legacyMetadataManifestSend',
  LEGACY_ARTIFACT_REQUEST_APPLY = 'legacyArtifactRequestApply',
  NO_GENERATED_ARTIFACT_UPLOAD_JOB = 'noGeneratedArtifactUploadJob',
  NO_PHYSICAL_DELETE = 'noPhysicalDelete',
  UNSUPPORTED_NO_ROUTE = 'unsupportedNoRoute',
  LEGACY_FALLBACK_PRESERVED = 'legacyFallbackPreserved'
}

// ── Canonical Apply Action ──

export class CanonicalApplyAction {
  actionID: string
  kind: CanonicalApplyActionKind
  source: CanonicalApplySource
  target: CanonicalApplyTarget
  bridgeHint: CanonicalApplyBridgeHint | null
  preconditions: CanonicalApplyPrecondition[]
  result: CanonicalApplyResult
  failureReason: CanonicalApplyFailureReason | null
  conflictID: string | null
  tombstoneID: string | null
  reason: string

  constructor(
    kind: CanonicalApplyActionKind,
    source: CanonicalApplySource,
    target: CanonicalApplyTarget,
    bridgeHint: CanonicalApplyBridgeHint | null = null,
    preconditions: CanonicalApplyPrecondition[] = [],
    result: CanonicalApplyResult = CanonicalApplyResult.PLANNED,
    failureReason: CanonicalApplyFailureReason | null = null,
    conflictID: string | null = null,
    tombstoneID: string | null = null,
    reason: string
  ) {
    this.kind = kind
    this.source = source
    this.target = target
    this.bridgeHint = bridgeHint
    this.preconditions = preconditions
    this.result = result
    this.failureReason = failureReason
    this.conflictID = nilIfEmpty((conflictID ?? '').trim()) ?? null
    this.tombstoneID = nilIfEmpty((tombstoneID ?? '').trim()) ?? null
    this.reason = nilIfEmpty(reason.trim()) ?? kind
    this.actionID = makeActionIDInternal(kind, target, this.reason)
  }

  get id(): string {
    return this.actionID
  }
}

function makeActionIDInternal(
  kind: CanonicalApplyActionKind,
  target: CanonicalApplyTarget,
  reason: string
): string {
  return [
    kind,
    target.objectID,
    target.artifactKind ?? 'object',
    target.artifactID ?? 'metadata',
    reason
  ].join('|')
}

// ── Canonical Conflict Kind ──

export enum CanonicalConflictKind {
  RECORDING_METADATA_CONCURRENT_EDIT = 'recordingMetadataConcurrentEdit',
  RECORDING_AUDIO_CONTENT_MISMATCH = 'recordingAudioContentMismatch',
  GENERATED_ARTIFACT_CONTENT_MISMATCH = 'generatedArtifactContentMismatch',
  ACTIVE_VS_TOMBSTONE = 'activeVsTombstone'
}

// ── Canonical Conflict Severity ──

export enum CanonicalConflictSeverity {
  WARNING = 'warning',
  BLOCKING = 'blocking'
}

// ── Canonical Conflict Resolution Policy ──

export enum CanonicalConflictResolutionPolicy {
  MANUAL_REVIEW = 'manualReview',
  KEEP_BOTH_NO_OVERWRITE = 'keepBothNoOverwrite',
  TOMBSTONE_REQUIRES_MANUAL_REVIEW = 'tombstoneRequiresManualReview'
}

// ── Canonical Conflict Resolution State ──

export enum CanonicalConflictResolutionState {
  UNRESOLVED = 'unresolved',
  RESOLVED = 'resolved',
  IGNORED = 'ignored'
}

// ── Canonical Conflict Record ──

export class CanonicalConflictRecord {
  conflictID: string
  kind: CanonicalConflictKind
  severity: CanonicalConflictSeverity
  resolutionPolicy: CanonicalConflictResolutionPolicy
  resolutionState: CanonicalConflictResolutionState
  target: CanonicalApplyTarget
  localHashPrefix: string | null
  peerHashPrefix: string | null
  localModifiedAt: CanonicalTimestamp | null
  peerModifiedAt: CanonicalTimestamp | null
  detail: string | null

  constructor(
    kind: CanonicalConflictKind,
    target: CanonicalApplyTarget,
    severity: CanonicalConflictSeverity = CanonicalConflictSeverity.BLOCKING,
    resolutionPolicy: CanonicalConflictResolutionPolicy,
    resolutionState: CanonicalConflictResolutionState = CanonicalConflictResolutionState.UNRESOLVED,
    localHash: CanonicalHash | null = null,
    peerHash: CanonicalHash | null = null,
    localModifiedAt: CanonicalTimestamp | null = null,
    peerModifiedAt: CanonicalTimestamp | null = null,
    detail: string | null = null
  ) {
    this.kind = kind
    this.severity = severity
    this.resolutionPolicy = resolutionPolicy
    this.resolutionState = resolutionState
    this.target = target
    this.localHashPrefix = localHash ? hashPrefix(localHash.value, 12) : null
    this.peerHashPrefix = peerHash ? hashPrefix(peerHash.value, 12) : null
    this.localModifiedAt = localModifiedAt
    this.peerModifiedAt = peerModifiedAt
    this.detail = nilIfEmpty((detail ?? '').trim()) ?? null
    this.conflictID = [
      'conflict',
      kind,
      target.objectID,
      target.artifactKind ?? 'metadata',
      target.artifactID ?? 'object'
    ].join('|')
  }

  get id(): string {
    return this.conflictID
  }
}

// ── Canonical Conflict Diagnostics ──

export class CanonicalConflictDiagnostics {
  total: number
  metadata: number
  audio: number
  generatedArtifact: number
  tombstone: number

  constructor(total: number, metadata: number, audio: number, generatedArtifact: number, tombstone: number) {
    this.total = total
    this.metadata = metadata
    this.audio = audio
    this.generatedArtifact = generatedArtifact
    this.tombstone = tombstone
  }
}

// ── Canonical Tombstone State ──

export enum CanonicalTombstoneState {
  ACTIVE = 'active',
  TOMBSTONED = 'tombstoned'
}

// ── Canonical Deletion Reason ──

export enum CanonicalDeletionReason {
  SOFT_DELETE = 'softDelete',
  PEER_TOMBSTONE_NEWER = 'peerTombstoneNewer',
  LOCAL_TOMBSTONE_NEWER = 'localTombstoneNewer',
  ARTIFACT_TOMBSTONE_PRESENT = 'artifactTombstonePresent'
}

// ── Canonical Tombstone Policy (canonical apply level) ──

export enum CanonicalTombstonePolicy {
  SOFT_DELETE_ONLY = 'softDeleteOnly',
  ANTI_RESURRECTION = 'antiResurrection',
  NO_PHYSICAL_DELETE = 'noPhysicalDelete',
  NO_PERMANENT_DELETE = 'noPermanentDelete',
  NO_GARBAGE_COLLECTION = 'noGarbageCollection'
}

// ── Canonical Tombstone ──

export class CanonicalTombstone {
  tombstoneID: string
  target: CanonicalApplyTarget
  state: CanonicalTombstoneState
  reason: CanonicalDeletionReason
  deletedAt: CanonicalTimestamp | null
  sourceNodeID: string | null
  policies: CanonicalTombstonePolicy[]

  constructor(
    target: CanonicalApplyTarget,
    state: CanonicalTombstoneState,
    reason: CanonicalDeletionReason,
    deletedAt: CanonicalTimestamp | null,
    sourceNodeID: string | null,
    policies: CanonicalTombstonePolicy[] = [
      CanonicalTombstonePolicy.SOFT_DELETE_ONLY,
      CanonicalTombstonePolicy.ANTI_RESURRECTION,
      CanonicalTombstonePolicy.NO_PHYSICAL_DELETE,
      CanonicalTombstonePolicy.NO_PERMANENT_DELETE,
      CanonicalTombstonePolicy.NO_GARBAGE_COLLECTION
    ]
  ) {
    this.target = target
    this.state = state
    this.reason = reason
    this.deletedAt = deletedAt
    this.sourceNodeID = nilIfEmpty((sourceNodeID ?? '').trim()) ?? null
    this.policies = [...new Set(policies)].sort()
    this.tombstoneID = [
      'tombstone',
      target.objectID,
      target.artifactKind ?? 'object',
      target.artifactID ?? 'metadata'
    ].join('|')
  }

  get id(): string {
    return this.tombstoneID
  }
}

// ── Canonical Apply Diagnostic ──

export class CanonicalApplyDiagnostic {
  phase: string
  target: CanonicalApplyTarget
  detail: string | null

  constructor(phase: string, target: CanonicalApplyTarget, detail: string | null = null) {
    this.phase = phase
    this.target = target
    this.detail = detail
  }

  get id(): string {
    return [this.phase, this.target.objectID, this.target.artifactID ?? '', this.detail ?? ''].join('|')
  }
}

// ── Canonical Apply Plan ──

// Forward declaration: CanonicalSyncPlanTrigger — referenced but defined elsewhere.
// Minimal interface for type compatibility.
export interface CanonicalSyncPlanTrigger {
  rawValue: string
}

// Forward: CanonicalRecordingMetadataAction
export interface CanonicalRecordingMetadataAction {
  objectID: string
  kind: string
  reason: { rawValue: string }
  localMetadataHash: CanonicalHash | null
  peerMetadataHash: CanonicalHash | null
  localModifiedAt: CanonicalTimestamp | null
  peerModifiedAt: CanonicalTimestamp | null
}

// Forward: CanonicalArtifactTransferAction
export interface CanonicalArtifactTransferAction {
  objectID: string
  artifactID: string | null
  kind: CanonicalArtifactKind
  reason: { rawValue: string }
  localHash: CanonicalHash | null
  peerHash: CanonicalHash | null
  peerByteSize: number | null
}

// Forward: CanonicalSyncPlan
export interface CanonicalSyncPlan {
  uploadRecordingMetadata: CanonicalRecordingMetadataAction[]
  downloadRecordingMetadata: CanonicalRecordingMetadataAction[]
  conflictRecordingMetadata: CanonicalRecordingMetadataAction[]
  conflictAudioArtifact: CanonicalArtifactTransferAction[]
  downloadGeneratedArtifact: CanonicalArtifactTransferAction[]
  noOpGeneratedArtifact: CanonicalArtifactTransferAction[]
  deferGeneratedArtifact: CanonicalArtifactTransferAction[]
  conflictGeneratedArtifact: CanonicalArtifactTransferAction[]
}

// Forward: CanonicalSyncPlannerLegacyContext
export interface CanonicalSyncPlannerLegacyContext {
  // legacy context marker — presence signals legacy fallback preserved
}

export class CanonicalApplyPlan {
  static readonly CURRENT_SCHEMA_VERSION = 1

  schemaVersion: number
  trigger: CanonicalSyncPlanTrigger
  actions: CanonicalApplyAction[]
  conflicts: CanonicalConflictRecord[]
  tombstones: CanonicalTombstone[]
  diagnostics: CanonicalApplyDiagnostic[]
  conflictDiagnostics: CanonicalConflictDiagnostics

  constructor(
    trigger: CanonicalSyncPlanTrigger,
    actions: CanonicalApplyAction[] = [],
    conflicts: CanonicalConflictRecord[] = [],
    tombstones: CanonicalTombstone[] = [],
    diagnostics: CanonicalApplyDiagnostic[] = []
  ) {
    this.schemaVersion = CanonicalApplyPlan.CURRENT_SCHEMA_VERSION
    this.trigger = trigger
    this.actions = actions
    this.conflicts = conflicts
    this.tombstones = tombstones
    this.diagnostics = diagnostics
    this.conflictDiagnostics = makeConflictDiagnostics(conflicts)
  }

  deduplicated(): CanonicalApplyPlan {
    const seenActions = new Set<string>()
    const seenConflicts = new Set<string>()
    const seenTombstones = new Set<string>()
    const seenDiagnostics = new Set<string>()
    return new CanonicalApplyPlan(
      this.trigger,
      this.actions.filter(a => {
        if (seenActions.has(a.actionID)) {
          return false
        }
        seenActions.add(a.actionID)
        return true
      }),
      this.conflicts.filter(c => {
        if (seenConflicts.has(c.conflictID)) {
          return false
        }
        seenConflicts.add(c.conflictID)
        return true
      }),
      this.tombstones.filter(t => {
        if (seenTombstones.has(t.tombstoneID)) {
          return false
        }
        seenTombstones.add(t.tombstoneID)
        return true
      }),
      this.diagnostics.filter(d => {
        if (seenDiagnostics.has(d.id)) {
          return false
        }
        seenDiagnostics.add(d.id)
        return true
      })
    )
  }
}

function makeConflictDiagnostics(conflicts: CanonicalConflictRecord[]): CanonicalConflictDiagnostics {
  return new CanonicalConflictDiagnostics(
    conflicts.length,
    conflicts.filter(c => c.kind === CanonicalConflictKind.RECORDING_METADATA_CONCURRENT_EDIT).length,
    conflicts.filter(c => c.kind === CanonicalConflictKind.RECORDING_AUDIO_CONTENT_MISMATCH).length,
    conflicts.filter(c => c.kind === CanonicalConflictKind.GENERATED_ARTIFACT_CONTENT_MISMATCH).length,
    conflicts.filter(c => c.kind === CanonicalConflictKind.ACTIVE_VS_TOMBSTONE).length
  )
}

// ── Canonical Apply Planner ──

export class CanonicalApplyPlanner {
  plan(
    local: CanonicalManifest,
    peer: CanonicalManifest,
    syncPlan: CanonicalSyncPlan,
    trigger: CanonicalSyncPlanTrigger,
    legacyContext: CanonicalSyncPlannerLegacyContext | null = null
  ): CanonicalApplyPlan {
    const localObjectsMap = new Map<string, CanonicalRecordingObject>()
    for (const obj of local.objects) {
      localObjectsMap.set(obj.objectID, obj)
    }
    const peerObjectsMap = new Map<string, CanonicalRecordingObject>()
    for (const obj of peer.objects) {
      peerObjectsMap.set(obj.objectID, obj)
    }
    const objectIDSet = new Set([...localObjectsMap.keys(), ...peerObjectsMap.keys()])
    const objectIDs = [...objectIDSet].sort()

    const actions: CanonicalApplyAction[] = []
    const conflicts: CanonicalConflictRecord[] = []
    const tombstones: CanonicalTombstone[] = []
    const diagnostics: CanonicalApplyDiagnostic[] = []
    const tombstoneOverrideObjectIDs = new Set<string>()

    for (const objectID of objectIDs) {
      this.appendObjectTombstoneDecision(
        objectID,
        localObjectsMap.get(objectID) ?? null,
        peerObjectsMap.get(objectID) ?? null,
        actions, conflicts, tombstones, diagnostics, tombstoneOverrideObjectIDs
      )
    }

    this.appendMetadataActions(syncPlan, tombstoneOverrideObjectIDs, actions, conflicts)
    this.appendAudioConflictActions(syncPlan, actions, conflicts)
    this.appendArtifactTombstones(localObjectsMap, peerObjectsMap, actions, tombstones, diagnostics)
    this.appendGeneratedArtifactActions(
      syncPlan, localObjectsMap, peerObjectsMap, tombstoneOverrideObjectIDs, actions, conflicts, diagnostics
    )

    diagnostics.push(
      new CanonicalApplyDiagnostic(
        'canonicalApplyPlanBuilt',
        new CanonicalApplyTarget('summary'),
        'actions=' + actions.length + ',conflicts=' + conflicts.length +
        ',tombstones=' + tombstones.length + ',trigger=' + trigger.rawValue +
        ',legacyFallbackPreserved=' + (legacyContext !== null)
      )
    )

    return new CanonicalApplyPlan(
      trigger, actions, conflicts, tombstones, diagnostics
    ).deduplicated()
  }

  private appendObjectTombstoneDecision(
    objectID: string,
    localObject: CanonicalRecordingObject | null,
    peerObject: CanonicalRecordingObject | null,
    actions: CanonicalApplyAction[],
    conflicts: CanonicalConflictRecord[],
    tombstones: CanonicalTombstone[],
    diagnostics: CanonicalApplyDiagnostic[],
    tombstoneOverrideObjectIDs: Set<string>
  ): void {
    const localTombstone = this.objectTombstoneFor(localObject, localObject?.nodeID ?? null, CanonicalDeletionReason.LOCAL_TOMBSTONE_NEWER)
    const peerTombstone = this.objectTombstoneFor(peerObject, peerObject?.nodeID ?? null, CanonicalDeletionReason.PEER_TOMBSTONE_NEWER)
    if (localTombstone) {
      tombstones.push(localTombstone)
    }
    if (peerTombstone) {
      tombstones.push(peerTombstone)
    }

    if (localObject !== null && peerObject !== null) {
      const localDeletedAt = this.deletionTimestamp(localObject)
      const peerDeletedAt = this.deletionTimestamp(peerObject)
      if (localDeletedAt !== null && peerDeletedAt !== null) {
        if (peerDeletedAt.date.getTime() > localDeletedAt.date.getTime()) {
          this.appendObjectTombstoneAction(
            CanonicalApplyActionKind.OBJECT_TOMBSTONE_APPLY, CanonicalApplySource.PEER,
            objectID, peerTombstone ?? null, CanonicalDeletionReason.PEER_TOMBSTONE_NEWER, actions
          )
          tombstoneOverrideObjectIDs.add(objectID)
        } else if (localDeletedAt.date.getTime() > peerDeletedAt.date.getTime()) {
          this.appendObjectTombstoneAction(
            CanonicalApplyActionKind.OBJECT_TOMBSTONE_SEND, CanonicalApplySource.LOCAL,
            objectID, localTombstone ?? null, CanonicalDeletionReason.LOCAL_TOMBSTONE_NEWER, actions
          )
          tombstoneOverrideObjectIDs.add(objectID)
        }
      } else if (localDeletedAt === null && peerDeletedAt !== null) {
        if (peerDeletedAt.date.getTime() > localObject.metadata.modifiedAt.date.getTime()) {
          this.appendObjectTombstoneAction(
            CanonicalApplyActionKind.OBJECT_TOMBSTONE_APPLY, CanonicalApplySource.PEER,
            objectID, peerTombstone ?? null, CanonicalDeletionReason.PEER_TOMBSTONE_NEWER, actions
          )
          tombstoneOverrideObjectIDs.add(objectID)
        } else {
          this.appendActiveVsTombstoneConflict(objectID, localObject, peerObject, conflicts, actions)
          tombstoneOverrideObjectIDs.add(objectID)
        }
      } else if (localDeletedAt !== null && peerDeletedAt === null) {
        if (localDeletedAt.date.getTime() > peerObject.metadata.modifiedAt.date.getTime()) {
          this.appendObjectTombstoneAction(
            CanonicalApplyActionKind.OBJECT_TOMBSTONE_SEND, CanonicalApplySource.LOCAL,
            objectID, localTombstone ?? null, CanonicalDeletionReason.LOCAL_TOMBSTONE_NEWER, actions
          )
          tombstoneOverrideObjectIDs.add(objectID)
        } else {
          this.appendActiveVsTombstoneConflict(objectID, localObject, peerObject, conflicts, actions)
          tombstoneOverrideObjectIDs.add(objectID)
        }
      }
    } else if (localObject === null && peerObject !== null && peerObject.metadata.isDeleted) {
      this.appendObjectTombstoneAction(
        CanonicalApplyActionKind.OBJECT_TOMBSTONE_APPLY, CanonicalApplySource.PEER,
        objectID, peerTombstone ?? null, CanonicalDeletionReason.PEER_TOMBSTONE_NEWER, actions
      )
      tombstoneOverrideObjectIDs.add(objectID)
    } else if (localObject !== null && peerObject === null && localObject.metadata.isDeleted) {
      this.appendObjectTombstoneAction(
        CanonicalApplyActionKind.OBJECT_TOMBSTONE_SEND, CanonicalApplySource.LOCAL,
        objectID, localTombstone ?? null, CanonicalDeletionReason.LOCAL_TOMBSTONE_NEWER, actions
      )
      tombstoneOverrideObjectIDs.add(objectID)
    }

    if (localObject?.metadata.isDeleted === true || peerObject?.metadata.isDeleted === true) {
      diagnostics.push(
        new CanonicalApplyDiagnostic(
          'canonicalTombstoneObserved',
          new CanonicalApplyTarget(objectID),
          'softDeleteOnly=true,noPhysicalDelete=true'
        )
      )
    }
  }

  private appendMetadataActions(
    syncPlan: CanonicalSyncPlan,
    tombstoneOverrideObjectIDs: Set<string>,
    actions: CanonicalApplyAction[],
    conflicts: CanonicalConflictRecord[]
  ): void {
    for (const action of syncPlan.uploadRecordingMetadata) {
      if (tombstoneOverrideObjectIDs.has(action.objectID)) {
        continue
      }
      actions.push(
        new CanonicalApplyAction(
          CanonicalApplyActionKind.RECORDING_METADATA_SEND,
          CanonicalApplySource.LOCAL,
          new CanonicalApplyTarget(action.objectID),
          CanonicalApplyBridgeHint.LEGACY_METADATA_MANIFEST_SEND,
          this.metadataPreconditionsFor(action),
          CanonicalApplyResult.PLANNED,
          null, null, null,
          action.reason.rawValue
        )
      )
    }
    for (const action of syncPlan.downloadRecordingMetadata) {
      if (tombstoneOverrideObjectIDs.has(action.objectID)) {
        continue
      }
      actions.push(
        new CanonicalApplyAction(
          CanonicalApplyActionKind.RECORDING_METADATA_APPLY,
          CanonicalApplySource.PEER,
          new CanonicalApplyTarget(action.objectID),
          CanonicalApplyBridgeHint.LEGACY_METADATA_MANIFEST_APPLY,
          this.metadataPreconditionsFor(action),
          CanonicalApplyResult.PLANNED,
          null, null, null,
          action.reason.rawValue
        )
      )
    }
    for (const action of syncPlan.conflictRecordingMetadata) {
      if (tombstoneOverrideObjectIDs.has(action.objectID)) {
        continue
      }
      const conflict = new CanonicalConflictRecord(
        CanonicalConflictKind.RECORDING_METADATA_CONCURRENT_EDIT,
        new CanonicalApplyTarget(action.objectID),
        CanonicalConflictSeverity.BLOCKING,
        CanonicalConflictResolutionPolicy.MANUAL_REVIEW,
        CanonicalConflictResolutionState.UNRESOLVED,
        action.localMetadataHash,
        action.peerMetadataHash,
        action.localModifiedAt,
        action.peerModifiedAt,
        action.reason.rawValue
      )
      conflicts.push(conflict)
      actions.push(this.conflictActionFor(conflict))
    }
  }

  private appendAudioConflictActions(
    syncPlan: CanonicalSyncPlan,
    actions: CanonicalApplyAction[],
    conflicts: CanonicalConflictRecord[]
  ): void {
    for (const action of syncPlan.conflictAudioArtifact) {
      const conflict = new CanonicalConflictRecord(
        CanonicalConflictKind.RECORDING_AUDIO_CONTENT_MISMATCH,
        new CanonicalApplyTarget(action.objectID, action.artifactID, CanonicalArtifactKind.AUDIO),
        CanonicalConflictSeverity.BLOCKING,
        CanonicalConflictResolutionPolicy.KEEP_BOTH_NO_OVERWRITE,
        CanonicalConflictResolutionState.UNRESOLVED,
        action.localHash,
        action.peerHash,
        null, null,
        action.reason.rawValue
      )
      conflicts.push(conflict)
      actions.push(this.conflictActionFor(conflict))
    }
  }

  private appendArtifactTombstones(
    localObjects: Map<string, CanonicalRecordingObject>,
    peerObjects: Map<string, CanonicalRecordingObject>,
    actions: CanonicalApplyAction[],
    tombstones: CanonicalTombstone[],
    diagnostics: CanonicalApplyDiagnostic[]
  ): void {
    const objectIDSet = new Set([...localObjects.keys(), ...peerObjects.keys()])
    const objectIDs = [...objectIDSet].sort()
    const genKinds = generatedArtifactKinds()
    for (const objectID of objectIDs) {
      const localObj = localObjects.get(objectID) ?? null
      const peerObj = peerObjects.get(objectID) ?? null
      const artifacts = [...(localObj?.artifacts ?? []), ...(peerObj?.artifacts ?? [])]
      for (const artifact of artifacts) {
        if (artifact.tombstone !== true || genKinds.indexOf(artifact.kind) < 0) {
          continue
        }
        const tombstone = new CanonicalTombstone(
          new CanonicalApplyTarget(objectID, artifact.artifactID, artifact.kind),
          CanonicalTombstoneState.TOMBSTONED,
          CanonicalDeletionReason.ARTIFACT_TOMBSTONE_PRESENT,
          artifact.modifiedAt,
          artifact.producedByNodeID,
          [
            CanonicalTombstonePolicy.SOFT_DELETE_ONLY,
            CanonicalTombstonePolicy.ANTI_RESURRECTION,
            CanonicalTombstonePolicy.NO_PHYSICAL_DELETE,
            CanonicalTombstonePolicy.NO_PERMANENT_DELETE,
            CanonicalTombstonePolicy.NO_GARBAGE_COLLECTION
          ]
        )
        tombstones.push(tombstone)
        actions.push(
          new CanonicalApplyAction(
            CanonicalApplyActionKind.ARTIFACT_TOMBSTONE_APPLY,
            CanonicalApplySource.PLANNER,
            tombstone.target,
            CanonicalApplyBridgeHint.NO_PHYSICAL_DELETE,
            [
              new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.NO_PHYSICAL_DELETE, 'true')
            ],
            CanonicalApplyResult.DEFERRED_UNSUPPORTED,
            CanonicalApplyFailureReason.NO_PHYSICAL_DELETE_POLICY,
            null,
            tombstone.tombstoneID,
            CanonicalDeletionReason.ARTIFACT_TOMBSTONE_PRESENT
          )
        )
        diagnostics.push(
          new CanonicalApplyDiagnostic(
            'canonicalArtifactTombstoneObserved',
            tombstone.target,
            'noPhysicalDelete=true'
          )
        )
      }
    }
  }

  private appendGeneratedArtifactActions(
    syncPlan: CanonicalSyncPlan,
    localObjects: Map<string, CanonicalRecordingObject>,
    peerObjects: Map<string, CanonicalRecordingObject>,
    tombstoneOverrideObjectIDs: Set<string>,
    actions: CanonicalApplyAction[],
    conflicts: CanonicalConflictRecord[],
    diagnostics: CanonicalApplyDiagnostic[]
  ): void {
    for (const action of syncPlan.downloadGeneratedArtifact) {
      const localObj = localObjects.get(action.objectID) ?? null
      const peerObj = peerObjects.get(action.objectID) ?? null
      if (this.objectIsTombstoned(localObj) || this.objectIsTombstoned(peerObj) ||
        tombstoneOverrideObjectIDs.has(action.objectID)) {
        actions.push(
          this.unsupportedGeneratedActionFor(action, CanonicalApplyFailureReason.TOMBSTONE_BLOCKS_RESURRECTION,
            'tombstoneBlocksResurrection')
        )
        diagnostics.push(
          new CanonicalApplyDiagnostic(
            'canonicalGeneratedArtifactDownloadBlockedByTombstone',
            this.targetFor(action),
            'antiResurrection=true'
          )
        )
      } else {
        actions.push(
          new CanonicalApplyAction(
            CanonicalApplyActionKind.GENERATED_ARTIFACT_DOWNLOAD_APPLY,
            CanonicalApplySource.PEER,
            this.targetFor(action),
            CanonicalApplyBridgeHint.LEGACY_ARTIFACT_REQUEST_APPLY,
            this.artifactPreconditionsFor(action),
            CanonicalApplyResult.PLANNED,
            null, null, null,
            action.reason.rawValue
          )
        )
      }
    }
    for (const action of syncPlan.noOpGeneratedArtifact) {
      actions.push(
        new CanonicalApplyAction(
          CanonicalApplyActionKind.GENERATED_ARTIFACT_NO_OP,
          CanonicalApplySource.PLANNER,
          this.targetFor(action),
          CanonicalApplyBridgeHint.NO_GENERATED_ARTIFACT_UPLOAD_JOB,
          this.artifactPreconditionsFor(action),
          CanonicalApplyResult.NO_OP,
          null, null, null,
          action.reason.rawValue
        )
      )
    }
    for (const action of syncPlan.deferGeneratedArtifact) {
      actions.push(
        this.unsupportedGeneratedActionFor(action, CanonicalApplyFailureReason.UNSUPPORTED_ROUTE,
          action.reason.rawValue)
      )
    }
    for (const action of syncPlan.conflictGeneratedArtifact) {
      const conflict = new CanonicalConflictRecord(
        CanonicalConflictKind.GENERATED_ARTIFACT_CONTENT_MISMATCH,
        this.targetFor(action),
        CanonicalConflictSeverity.BLOCKING,
        CanonicalConflictResolutionPolicy.MANUAL_REVIEW,
        CanonicalConflictResolutionState.UNRESOLVED,
        action.localHash,
        action.peerHash,
        null, null,
        action.reason.rawValue
      )
      conflicts.push(conflict)
      actions.push(this.conflictActionFor(conflict))
    }
  }

  private appendObjectTombstoneAction(
    kind: CanonicalApplyActionKind,
    source: CanonicalApplySource,
    objectID: string,
    tombstone: CanonicalTombstone | null,
    reason: CanonicalDeletionReason,
    actions: CanonicalApplyAction[]
  ): void {
    actions.push(
      new CanonicalApplyAction(
        kind,
        source,
        new CanonicalApplyTarget(objectID),
        kind === CanonicalApplyActionKind.OBJECT_TOMBSTONE_APPLY
          ? CanonicalApplyBridgeHint.LEGACY_METADATA_MANIFEST_APPLY
          : CanonicalApplyBridgeHint.LEGACY_METADATA_MANIFEST_SEND,
        [
          new CanonicalApplyPrecondition(
            CanonicalApplyPreconditionKind.TOMBSTONE_TIMESTAMP,
            tombstone?.deletedAt ? timestampString(tombstone.deletedAt) : 'missing'
          ),
          new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.NO_PHYSICAL_DELETE, 'true')
        ],
        CanonicalApplyResult.PLANNED,
        null, null,
        tombstone?.tombstoneID ?? null,
        reason
      )
    )
  }

  private appendActiveVsTombstoneConflict(
    objectID: string,
    localObject: CanonicalRecordingObject,
    peerObject: CanonicalRecordingObject,
    conflicts: CanonicalConflictRecord[],
    actions: CanonicalApplyAction[]
  ): void {
    const conflict = new CanonicalConflictRecord(
      CanonicalConflictKind.ACTIVE_VS_TOMBSTONE,
      new CanonicalApplyTarget(objectID),
      CanonicalConflictSeverity.BLOCKING,
      CanonicalConflictResolutionPolicy.TOMBSTONE_REQUIRES_MANUAL_REVIEW,
      CanonicalConflictResolutionState.UNRESOLVED,
      localObject.metadataHash,
      peerObject.metadataHash,
      localObject.metadata.modifiedAt,
      peerObject.metadata.modifiedAt,
      'activeVsTombstone'
    )
    conflicts.push(conflict)
    actions.push(this.conflictActionFor(conflict))
  }

  private conflictActionFor(conflict: CanonicalConflictRecord): CanonicalApplyAction {
    const preconditions: CanonicalApplyPrecondition[] = []
    if (conflict.localHashPrefix) {
      preconditions.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.LOCAL_HASH_PREFIX, conflict.localHashPrefix))
    }
    if (conflict.peerHashPrefix) {
      preconditions.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.PEER_HASH_PREFIX, conflict.peerHashPrefix))
    }
    return new CanonicalApplyAction(
      CanonicalApplyActionKind.CONFLICT_RECORD,
      CanonicalApplySource.PLANNER,
      conflict.target,
      CanonicalApplyBridgeHint.LEGACY_FALLBACK_PRESERVED,
      preconditions,
      CanonicalApplyResult.CONFLICT_RECORDED,
      CanonicalApplyFailureReason.CONFLICT_DETECTED,
      conflict.conflictID,
      null,
      conflict.kind
    )
  }

  private unsupportedGeneratedActionFor(
    action: CanonicalArtifactTransferAction,
    failureReason: CanonicalApplyFailureReason,
    reason: string
  ): CanonicalApplyAction {
    return new CanonicalApplyAction(
      CanonicalApplyActionKind.DEFERRED_UNSUPPORTED,
      CanonicalApplySource.PLANNER,
      this.targetFor(action),
      failureReason === CanonicalApplyFailureReason.TOMBSTONE_BLOCKS_RESURRECTION
        ? CanonicalApplyBridgeHint.NO_PHYSICAL_DELETE
        : CanonicalApplyBridgeHint.UNSUPPORTED_NO_ROUTE,
      this.artifactPreconditionsFor(action),
      CanonicalApplyResult.DEFERRED_UNSUPPORTED,
      failureReason,
      null, null,
      reason
    )
  }

  private targetFor(action: CanonicalArtifactTransferAction): CanonicalApplyTarget {
    return new CanonicalApplyTarget(action.objectID, action.artifactID, action.kind)
  }

  private metadataPreconditionsFor(action: CanonicalRecordingMetadataAction): CanonicalApplyPrecondition[] {
    const result: CanonicalApplyPrecondition[] = []
    if (action.localMetadataHash) {
      result.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.LOCAL_HASH_PREFIX,
        hashPrefix(action.localMetadataHash.value, 12)))
    }
    if (action.peerMetadataHash) {
      result.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.PEER_HASH_PREFIX,
        hashPrefix(action.peerMetadataHash.value, 12)))
    }
    if (action.localModifiedAt) {
      result.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.LOCAL_MODIFIED_AT,
        timestampString(action.localModifiedAt)))
    }
    if (action.peerModifiedAt) {
      result.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.PEER_MODIFIED_AT,
        timestampString(action.peerModifiedAt)))
    }
    result.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.LEGACY_BRIDGE, 'metadataManifest'))
    return result
  }

  private artifactPreconditionsFor(action: CanonicalArtifactTransferAction): CanonicalApplyPrecondition[] {
    const result: CanonicalApplyPrecondition[] = []
    if (action.localHash) {
      result.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.LOCAL_HASH_PREFIX,
        hashPrefix(action.localHash.value, 12)))
    }
    if (action.peerHash) {
      result.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.PEER_HASH_PREFIX,
        hashPrefix(action.peerHash.value, 12)))
    }
    if (action.peerByteSize !== null && action.peerByteSize !== undefined) {
      result.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.PEER_BYTE_SIZE,
        String(action.peerByteSize)))
    }
    result.push(new CanonicalApplyPrecondition(CanonicalApplyPreconditionKind.LEGACY_BRIDGE, 'artifactRequest'))
    return result
  }

  private objectTombstoneFor(
    object: CanonicalRecordingObject | null,
    sourceNodeID: string | null,
    reason: CanonicalDeletionReason
  ): CanonicalTombstone | null {
    if (!object || !object.metadata.isDeleted) {
      return null
    }
    return new CanonicalTombstone(
      new CanonicalApplyTarget(object.objectID),
      CanonicalTombstoneState.TOMBSTONED,
      reason,
      this.deletionTimestamp(object),
      sourceNodeID
    )
  }

  private objectIsTombstoned(object: CanonicalRecordingObject | null): boolean {
    if (!object) {
      return false
    }
    return object.metadata.isDeleted === true || object.syncState === 'deleted'
  }

  private deletionTimestamp(object: CanonicalRecordingObject): CanonicalTimestamp | null {
    if (!object.metadata.isDeleted && object.syncState !== 'deleted') {
      return null
    }
    return object.metadata.deletedAt ?? object.metadata.modifiedAt
  }
}
