/**
 * CanonicalLibrarySyncPlanner — library sync planning for the canonical domain.
 * Ported from Rokurics-Apple (CanonicalLibrarySyncPlanner.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalCapability,
  CanonicalNode, CanonicalLibraryObjectID, CanonicalObjectKind,
  CanonicalLibraryObject, CanonicalLibraryConflict, CanonicalLibraryConflictKind,
  CanonicalLibraryTombstone, CanonicalLibraryTombstoneReason,
  CanonicalManifest, hashPrefix, timestampString, nilIfEmpty
} from './CanonicalCore'

// These types are expected from CanonicalApplyPlan; define minimally if not available
import {
  CanonicalApplyAction, CanonicalApplyActionKind, CanonicalApplyTarget,
  CanonicalApplySource, CanonicalApplyPrecondition, CanonicalApplyResult,
  CanonicalApplyBridgeHint, ConflictReason
} from './CanonicalApplyPlan'

// ── Canonical Sync Plan Trigger ──

export enum CanonicalSyncPlanTrigger {
  PERIODIC = 'periodic',
  APP_ACTIVATION = 'appActivation',
  RETRY_DRAINER = 'retryDrainer',
  VIEW_REFRESH = 'viewRefresh',
  MANUAL = 'manual',
  TEST_HARNESS = 'testHarness'
}

// ── Canonical Library Action Kind ──

export enum CanonicalLibraryActionKind {
  FOLDER_METADATA_APPLY = 'folderMetadataApply',
  FOLDER_METADATA_SEND = 'folderMetadataSend',
  FOLDER_METADATA_NO_OP = 'folderMetadataNoOp',
  FOLDER_CONFLICT = 'folderConflict',
  FOLDER_TOMBSTONE_APPLY = 'folderTombstoneApply',
  FOLDER_TOMBSTONE_SEND = 'folderTombstoneSend',
  STUDY_ITEM_METADATA_APPLY = 'studyItemMetadataApply',
  STUDY_ITEM_METADATA_SEND = 'studyItemMetadataSend',
  STUDY_ITEM_METADATA_NO_OP = 'studyItemMetadataNoOp',
  STUDY_ITEM_CONFLICT = 'studyItemConflict',
  STUDY_ITEM_TOMBSTONE_APPLY = 'studyItemTombstoneApply',
  STUDY_ITEM_TOMBSTONE_SEND = 'studyItemTombstoneSend',
  UNSUPPORTED_FALLBACK = 'unsupportedFallback',
  DEFERRED = 'deferred'
}

// ── Canonical Library Sync Action ──

export class CanonicalLibrarySyncAction {
  actionID: string
  kind: CanonicalLibraryActionKind
  objectID: CanonicalLibraryObjectID
  objectKind: CanonicalObjectKind
  source: CanonicalApplySource
  reason: string
  localHashPrefix: string | null
  peerHashPrefix: string | null
  localModifiedAt: CanonicalTimestamp | null
  peerModifiedAt: CanonicalTimestamp | null

  constructor(
    kind: CanonicalLibraryActionKind,
    objectID: CanonicalLibraryObjectID,
    objectKind: CanonicalObjectKind,
    source: CanonicalApplySource,
    reason: string,
    localHash: CanonicalHash | null = null,
    peerHash: CanonicalHash | null = null,
    localModifiedAt: CanonicalTimestamp | null = null,
    peerModifiedAt: CanonicalTimestamp | null = null
  ) {
    this.kind = kind
    this.objectID = objectID
    this.objectKind = objectKind
    this.source = source
    this.reason = nilIfEmpty(reason.trim()) ?? kind
    this.localHashPrefix = localHash ? hashPrefix(localHash.value, 12) : null
    this.peerHashPrefix = peerHash ? hashPrefix(peerHash.value, 12) : null
    this.localModifiedAt = localModifiedAt
    this.peerModifiedAt = peerModifiedAt
    this.actionID = [kind, objectKind, objectID.rawValue, this.reason].join('|')
  }

  get id(): string {
    return this.actionID
  }
}

// ── Canonical Library Sync Diagnostic ──

export class CanonicalLibrarySyncDiagnostic {
  phase: string
  objectID: CanonicalLibraryObjectID | null
  objectKind: CanonicalObjectKind | null
  detail: string | null

  constructor(
    phase: string,
    objectID: CanonicalLibraryObjectID | null = null,
    objectKind: CanonicalObjectKind | null = null,
    detail: string | null = null
  ) {
    this.phase = phase
    this.objectID = objectID
    this.objectKind = objectKind
    this.detail = detail
  }

  get id(): string {
    return [this.phase, this.objectID?.rawValue ?? '', this.detail ?? ''].join('|')
  }
}

// ── Canonical Library Sync Plan ──

export class CanonicalLibrarySyncPlan {
  actions: CanonicalLibrarySyncAction[]
  applyActions: CanonicalApplyAction[]
  conflicts: CanonicalLibraryConflict[]
  tombstones: CanonicalLibraryTombstone[]
  diagnostics: CanonicalLibrarySyncDiagnostic[]
  fallbackRequiredObjectIDs: CanonicalLibraryObjectID[]

  constructor(
    actions: CanonicalLibrarySyncAction[] = [],
    applyActions: CanonicalApplyAction[] = [],
    conflicts: CanonicalLibraryConflict[] = [],
    tombstones: CanonicalLibraryTombstone[] = [],
    diagnostics: CanonicalLibrarySyncDiagnostic[] = [],
    fallbackRequiredObjectIDs: CanonicalLibraryObjectID[] = []
  ) {
    this.actions = actions
    this.applyActions = applyActions
    this.conflicts = conflicts
    this.tombstones = tombstones
    this.diagnostics = diagnostics
    this.fallbackRequiredObjectIDs = [...new Set(fallbackRequiredObjectIDs)]
      .sort((a, b) => a.rawValue.localeCompare(b.rawValue))
  }

  deduplicated(): CanonicalLibrarySyncPlan {
    const seenActions = new Set<string>()
    const seenApplyActions = new Set<string>()
    const seenConflicts = new Set<string>()
    const seenTombstones = new Set<string>()
    const seenDiagnostics = new Set<string>()
    const dedup = <T extends { id: string }>(items: T[], seen: Set<string>): T[] => {
      return items.filter(item => {
        const alreadySeen = seen.has(item.id)
        seen.add(item.id)
        return !alreadySeen
      })
    }
    return new CanonicalLibrarySyncPlan(
      dedup(this.actions, seenActions),
      dedup(this.applyActions, seenApplyActions),
      dedup(this.conflicts, seenConflicts),
      dedup(this.tombstones, seenTombstones),
      dedup(this.diagnostics, seenDiagnostics),
      this.fallbackRequiredObjectIDs
    )
  }
}

// ── Canonical Library Sync Planner ──

export class CanonicalLibrarySyncPlanner {
  constructor() {}

  plan(
    local: CanonicalManifest,
    peer: CanonicalManifest,
    trigger: CanonicalSyncPlanTrigger
  ): CanonicalLibrarySyncPlan {
    let plan = new CanonicalLibrarySyncPlan()
    if (trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH) {
      plan.diagnostics.push(
        new CanonicalLibrarySyncDiagnostic(
          'canonicalDomainFallback',
          null,
          null,
          'viewRefreshProjectionOnly'
        )
      )
      return plan
    }
    if (trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) {
      plan.diagnostics.push(
        new CanonicalLibrarySyncDiagnostic(
          'canonicalDomainFallback',
          null,
          null,
          'retryDrainerNoFreshLibraryTransfer'
        )
      )
      return plan
    }
    if (!this.hasLibraryCapability(local) || !this.hasLibraryCapability(peer)) {
      plan.fallbackRequiredObjectIDs = this.combinedObjectIDs(local, peer)
      plan.diagnostics.push(
        new CanonicalLibrarySyncDiagnostic(
          'canonicalDomainFallback',
          null,
          null,
          'canonicalLibraryObjectsCapabilityMissing'
        )
      )
      return plan
    }

    const localObjects = this.libraryObjectsByID(local.libraryObjects)
    const peerObjects = this.libraryObjectsByID(peer.libraryObjects)
    const allIDs = new Set([...Object.keys(localObjects), ...Object.keys(peerObjects)])
    const sortedIDs = [...allIDs].sort((a, b) => a.localeCompare(b))
    for (const rawID of sortedIDs) {
      const objectID = new CanonicalLibraryObjectID(rawID)
      this.appendDecision(
        objectID,
        localObjects[rawID] ?? null,
        peerObjects[rawID] ?? null,
        plan
      )
    }
    plan.diagnostics.push(
      new CanonicalLibrarySyncDiagnostic(
        'canonicalLibraryObjectsProjected',
        null,
        null,
        'local=' + local.libraryObjects.length + ',peer=' + peer.libraryObjects.length +
        ',actions=' + plan.actions.length
      )
    )
    return plan.deduplicated()
  }

  private appendDecision(
    objectID: CanonicalLibraryObjectID,
    localObject: CanonicalLibraryObject | null,
    peerObject: CanonicalLibraryObject | null,
    plan: CanonicalLibrarySyncPlan
  ): void {
    const obj = localObject ?? peerObject
    if (!obj) {
      return
    }
    if (!this.isSupported(obj) || (peerObject !== null && !this.isSupported(peerObject))) {
      this.appendUnsupported(objectID, obj, plan)
      return
    }
    const kind = obj.kind
    if (kind === CanonicalObjectKind.FOLDER) {
      this.appendMetadataDecision(
        objectID,
        CanonicalObjectKind.FOLDER,
        localObject,
        peerObject,
        CanonicalLibraryActionKind.FOLDER_METADATA_SEND,
        CanonicalLibraryActionKind.FOLDER_METADATA_APPLY,
        CanonicalLibraryActionKind.FOLDER_METADATA_NO_OP,
        CanonicalLibraryActionKind.FOLDER_CONFLICT,
        CanonicalLibraryActionKind.FOLDER_TOMBSTONE_SEND,
        CanonicalLibraryActionKind.FOLDER_TOMBSTONE_APPLY,
        plan
      )
    } else if (kind === CanonicalObjectKind.STANDALONE_STUDY_ITEM ||
      kind === CanonicalObjectKind.STANDALONE_NOTE ||
      kind === CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM) {
      this.appendMetadataDecision(
        objectID,
        obj.kind,
        localObject,
        peerObject,
        CanonicalLibraryActionKind.STUDY_ITEM_METADATA_SEND,
        CanonicalLibraryActionKind.STUDY_ITEM_METADATA_APPLY,
        CanonicalLibraryActionKind.STUDY_ITEM_METADATA_NO_OP,
        CanonicalLibraryActionKind.STUDY_ITEM_CONFLICT,
        CanonicalLibraryActionKind.STUDY_ITEM_TOMBSTONE_SEND,
        CanonicalLibraryActionKind.STUDY_ITEM_TOMBSTONE_APPLY,
        plan
      )
    } else {
      this.appendUnsupported(objectID, obj, plan)
    }
  }

  private appendMetadataDecision(
    objectID: CanonicalLibraryObjectID,
    objectKind: CanonicalObjectKind,
    localObject: CanonicalLibraryObject | null,
    peerObject: CanonicalLibraryObject | null,
    sendKind: CanonicalLibraryActionKind,
    applyKind: CanonicalLibraryActionKind,
    noOpKind: CanonicalLibraryActionKind,
    conflictKind: CanonicalLibraryActionKind,
    tombstoneSendKind: CanonicalLibraryActionKind,
    tombstoneApplyKind: CanonicalLibraryActionKind,
    plan: CanonicalLibrarySyncPlan
  ): void {
    const localDeletedAt = localObject?.deletedAt ?? null
    const peerDeletedAt = peerObject?.deletedAt ?? null
    if (this.appendTombstoneDecision(
      objectID,
      objectKind,
      localObject,
      peerObject,
      localDeletedAt,
      peerDeletedAt,
      tombstoneSendKind,
      tombstoneApplyKind,
      plan
    )) {
      return
    }

    if (localObject !== null && peerObject !== null) {
      const local = localObject
      const peer = peerObject
      if (this.sameHash(local.metadataHash, peer.metadataHash)) {
        this.appendAction(
          noOpKind,
          objectID,
          objectKind,
          CanonicalApplySource.PLANNER,
          'metadataHashEqual',
          local,
          peer,
          plan
        )
      } else {
        const localModifiedAt = local.businessModifiedAt
        const peerModifiedAt = peer.businessModifiedAt
        if (localModifiedAt !== null && peerModifiedAt !== null &&
          localModifiedAt.date.getTime() > peerModifiedAt.date.getTime()) {
          this.appendActionAndApply(
            sendKind,
            objectID,
            objectKind,
            CanonicalApplySource.LOCAL,
            'localMetadataNewer',
            local,
            peer,
            plan
          )
        } else if (localModifiedAt !== null && peerModifiedAt !== null &&
          peerModifiedAt.date.getTime() > localModifiedAt.date.getTime()) {
          this.appendActionAndApply(
            applyKind,
            objectID,
            objectKind,
            CanonicalApplySource.PEER,
            'peerMetadataNewer',
            local,
            peer,
            plan
          )
        } else {
          this.appendConflict(
            conflictKind,
            objectKind === CanonicalObjectKind.FOLDER
              ? CanonicalLibraryConflictKind.FOLDER_METADATA_CONCURRENT_EDIT
              : CanonicalLibraryConflictKind.STUDY_ITEM_METADATA_CONCURRENT_EDIT,
            objectID,
            objectKind,
            local,
            peer,
            'metadataTieConflict',
            plan
          )
        }
      }
    } else if (localObject !== null) {
      this.appendActionAndApply(
        sendKind,
        objectID,
        objectKind,
        CanonicalApplySource.LOCAL,
        'peerMissingMetadata',
        localObject,
        null,
        plan
      )
    } else if (peerObject !== null) {
      this.appendActionAndApply(
        applyKind,
        objectID,
        objectKind,
        CanonicalApplySource.PEER,
        'localMissingMetadata',
        null,
        peerObject,
        plan
      )
    }
  }

  private appendTombstoneDecision(
    objectID: CanonicalLibraryObjectID,
    objectKind: CanonicalObjectKind,
    localObject: CanonicalLibraryObject | null,
    peerObject: CanonicalLibraryObject | null,
    localDeletedAt: CanonicalTimestamp | null,
    peerDeletedAt: CanonicalTimestamp | null,
    tombstoneSendKind: CanonicalLibraryActionKind,
    tombstoneApplyKind: CanonicalLibraryActionKind,
    plan: CanonicalLibrarySyncPlan
  ): boolean {
    if (localObject !== null && localObject.isDeleted) {
      plan.tombstones.push(this.libraryTombstone(localObject, CanonicalLibraryTombstoneReason.LOCAL_TOMBSTONE_NEWER))
    }
    if (peerObject !== null && peerObject.isDeleted) {
      plan.tombstones.push(this.libraryTombstone(peerObject, CanonicalLibraryTombstoneReason.PEER_TOMBSTONE_NEWER))
    }

    if (localObject !== null && peerObject !== null && localDeletedAt !== null && peerDeletedAt !== null) {
      const local = localObject
      const peer = peerObject
      if (localDeletedAt.date.getTime() > peerDeletedAt.date.getTime()) {
        this.appendActionAndApply(tombstoneSendKind, objectID, objectKind, CanonicalApplySource.LOCAL,
          'localTombstoneNewer', local, peer, plan)
      } else if (peerDeletedAt.date.getTime() > localDeletedAt.date.getTime()) {
        this.appendActionAndApply(tombstoneApplyKind, objectID, objectKind, CanonicalApplySource.PEER,
          'peerTombstoneNewer', local, peer, plan)
      }
      return true
    }
    if (localObject !== null && peerObject !== null && localDeletedAt === null && peerDeletedAt !== null) {
      const local = localObject
      const peer = peerObject
      const localModifiedAt = local.businessModifiedAt
      if (localModifiedAt !== null && localModifiedAt.date.getTime() > peerDeletedAt.date.getTime()) {
        this.appendConflict(
          objectKind === CanonicalObjectKind.FOLDER
            ? CanonicalLibraryActionKind.FOLDER_CONFLICT : CanonicalLibraryActionKind.STUDY_ITEM_CONFLICT,
          CanonicalLibraryConflictKind.ACTIVE_VS_TOMBSTONE,
          objectID, objectKind, local, peer, 'activeNewerThanPeerTombstone', plan)
      } else {
        this.appendActionAndApply(tombstoneApplyKind, objectID, objectKind, CanonicalApplySource.PEER,
          'peerTombstoneNewer', local, peer, plan)
      }
      return true
    }
    if (localObject !== null && peerObject !== null && localDeletedAt !== null && peerDeletedAt === null) {
      const local = localObject
      const peer = peerObject
      const peerModifiedAt = peer.businessModifiedAt
      if (peerModifiedAt !== null && peerModifiedAt.date.getTime() > localDeletedAt.date.getTime()) {
        this.appendConflict(
          objectKind === CanonicalObjectKind.FOLDER
            ? CanonicalLibraryActionKind.FOLDER_CONFLICT : CanonicalLibraryActionKind.STUDY_ITEM_CONFLICT,
          CanonicalLibraryConflictKind.ACTIVE_VS_TOMBSTONE,
          objectID, objectKind, local, peer, 'activeNewerThanLocalTombstone', plan)
      } else {
        this.appendActionAndApply(tombstoneSendKind, objectID, objectKind, CanonicalApplySource.LOCAL,
          'localTombstoneNewer', local, peer, plan)
      }
      return true
    }
    if (localObject === null && peerObject !== null && peerDeletedAt !== null) {
      this.appendActionAndApply(tombstoneApplyKind, objectID, objectKind, CanonicalApplySource.PEER,
        'peerTombstoneNewer', null, peerObject, plan)
      return true
    }
    if (localObject !== null && peerObject === null && localDeletedAt !== null) {
      this.appendActionAndApply(tombstoneSendKind, objectID, objectKind, CanonicalApplySource.LOCAL,
        'localTombstoneNewer', localObject, null, plan)
      return true
    }
    return false
  }

  private appendActionAndApply(
    kind: CanonicalLibraryActionKind,
    objectID: CanonicalLibraryObjectID,
    objectKind: CanonicalObjectKind,
    source: CanonicalApplySource,
    reason: string,
    localObject: CanonicalLibraryObject | null,
    peerObject: CanonicalLibraryObject | null,
    plan: CanonicalLibrarySyncPlan
  ): void {
    this.appendAction(kind, objectID, objectKind, source, reason, localObject, peerObject, plan)
    const applyKind = this.applyActionKindFor(kind)
    if (!applyKind) {
      return
    }
    const bridge: CanonicalApplyBridgeHint = source === CanonicalApplySource.PEER
      ? CanonicalApplyBridgeHint.LEGACY_METADATA_MANIFEST_APPLY
      : CanonicalApplyBridgeHint.LEGACY_METADATA_MANIFEST_SEND
    plan.applyActions.push(
      new CanonicalApplyAction(
        applyKind,
        source,
        new CanonicalApplyTarget(objectID.rawValue),
        bridge,
        this.applyPreconditions(localObject, peerObject),
        reason
      )
    )
    plan.diagnostics.push(
      new CanonicalLibrarySyncDiagnostic(
        'canonicalLibraryActionBridged',
        objectID,
        objectKind,
        kind + '->' + bridge
      )
    )
  }

  private appendAction(
    kind: CanonicalLibraryActionKind,
    objectID: CanonicalLibraryObjectID,
    objectKind: CanonicalObjectKind,
    source: CanonicalApplySource,
    reason: string,
    localObject: CanonicalLibraryObject | null,
    peerObject: CanonicalLibraryObject | null,
    plan: CanonicalLibrarySyncPlan
  ): void {
    plan.actions.push(
      new CanonicalLibrarySyncAction(
        kind,
        objectID,
        objectKind,
        source,
        reason,
        localObject?.metadataHash ?? null,
        peerObject?.metadataHash ?? null,
        localObject?.businessModifiedAt ?? null,
        peerObject?.businessModifiedAt ?? null
      )
    )
    let phase: string
    switch (objectKind) {
      case CanonicalObjectKind.FOLDER:
        phase = kind === CanonicalLibraryActionKind.FOLDER_METADATA_NO_OP
          ? 'canonicalFolderMetadataHashConverged' : 'canonicalFolderPlanned'
        break
      case CanonicalObjectKind.STANDALONE_STUDY_ITEM:
      case CanonicalObjectKind.STANDALONE_NOTE:
      case CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM:
        phase = kind === CanonicalLibraryActionKind.STUDY_ITEM_METADATA_NO_OP
          ? 'canonicalStudyItemMetadataHashConverged' : 'canonicalStudyItemPlanned'
        break
      default:
        phase = 'canonicalLibraryObjectPlanned'
        break
    }
    plan.diagnostics.push(
      new CanonicalLibrarySyncDiagnostic(phase, objectID, objectKind, reason)
    )
  }

  private appendConflict(
    actionKind: CanonicalLibraryActionKind,
    conflictKind: CanonicalLibraryConflictKind,
    objectID: CanonicalLibraryObjectID,
    objectKind: CanonicalObjectKind,
    localObject: CanonicalLibraryObject,
    peerObject: CanonicalLibraryObject,
    reason: string,
    plan: CanonicalLibrarySyncPlan
  ): void {
    this.appendAction(actionKind, objectID, objectKind, CanonicalApplySource.PLANNER, reason,
      localObject, peerObject, plan)
    const conflict = new CanonicalLibraryConflict(
      conflictKind,
      objectID,
      objectKind,
      localObject.metadataHash,
      peerObject.metadataHash,
      localObject.businessModifiedAt,
      peerObject.businessModifiedAt,
      reason
    )
    plan.conflicts.push(conflict)
    plan.applyActions.push(
      new CanonicalApplyAction(
        CanonicalApplyActionKind.CONFLICT_RECORD,
        CanonicalApplySource.PLANNER,
        new CanonicalApplyTarget(objectID.rawValue),
        undefined,
        [],
        reason,
        CanonicalApplyResult.CONFLICT_RECORDED,
        undefined,
        ConflictReason.METADATA_MODIFIED_ON_BOTH_SIDES,
        conflict.conflictID
      )
    )
    plan.diagnostics.push(
      new CanonicalLibrarySyncDiagnostic(
        'canonicalLibraryConflictRecorded', objectID, objectKind, conflictKind)
    )
  }

  private appendUnsupported(
    objectID: CanonicalLibraryObjectID,
    object: CanonicalLibraryObject,
    plan: CanonicalLibrarySyncPlan
  ): void {
    plan.fallbackRequiredObjectIDs.push(objectID)
    plan.actions.push(
      new CanonicalLibrarySyncAction(
        CanonicalLibraryActionKind.UNSUPPORTED_FALLBACK,
        objectID,
        object.kind,
        CanonicalApplySource.PLANNER,
        object.unsupportedReason ?? 'unsupportedLibraryObject',
        object.metadataHash
      )
    )
    plan.diagnostics.push(
      new CanonicalLibrarySyncDiagnostic(
        'canonicalLibraryObjectUnsupported',
        objectID,
        object.kind,
        object.unsupportedReason ?? 'unsupportedLibraryObject'
      )
    )
  }

  private applyActionKindFor(kind: CanonicalLibraryActionKind): CanonicalApplyActionKind | null {
    switch (kind) {
      case CanonicalLibraryActionKind.FOLDER_METADATA_APPLY:
        return CanonicalApplyActionKind.FOLDER_METADATA_APPLY
      case CanonicalLibraryActionKind.FOLDER_METADATA_SEND:
        return CanonicalApplyActionKind.FOLDER_METADATA_SEND
      case CanonicalLibraryActionKind.FOLDER_TOMBSTONE_APPLY:
        return CanonicalApplyActionKind.LIBRARY_TOMBSTONE_APPLY
      case CanonicalLibraryActionKind.FOLDER_TOMBSTONE_SEND:
        return CanonicalApplyActionKind.LIBRARY_TOMBSTONE_SEND
      case CanonicalLibraryActionKind.STUDY_ITEM_METADATA_APPLY:
        return CanonicalApplyActionKind.STUDY_ITEM_METADATA_APPLY
      case CanonicalLibraryActionKind.STUDY_ITEM_METADATA_SEND:
        return CanonicalApplyActionKind.STUDY_ITEM_METADATA_SEND
      case CanonicalLibraryActionKind.STUDY_ITEM_TOMBSTONE_APPLY:
        return CanonicalApplyActionKind.LIBRARY_TOMBSTONE_APPLY
      case CanonicalLibraryActionKind.STUDY_ITEM_TOMBSTONE_SEND:
        return CanonicalApplyActionKind.LIBRARY_TOMBSTONE_SEND
      default:
        return null
    }
  }

  private applyPreconditions(
    localObject: CanonicalLibraryObject | null,
    peerObject: CanonicalLibraryObject | null
  ): CanonicalApplyPrecondition[] {
    const result: CanonicalApplyPrecondition[] = []
    if (localObject?.businessModifiedAt) {
      result.push(new CanonicalApplyPrecondition(
        CanonicalApplySource.LOCAL + 'ModifiedAt' as any, timestampString(localObject.businessModifiedAt)))
    }
    if (peerObject?.businessModifiedAt) {
      result.push(new CanonicalApplyPrecondition(
        CanonicalApplySource.PEER + 'ModifiedAt' as any, timestampString(peerObject.businessModifiedAt)))
    }
    if (localObject) {
      result.push(new CanonicalApplyPrecondition(
        'localHashPrefix' as any, hashPrefix(localObject.metadataHash.value, 12)))
    }
    if (peerObject) {
      result.push(new CanonicalApplyPrecondition(
        'peerHashPrefix' as any, hashPrefix(peerObject.metadataHash.value, 12)))
    }
    result.push(new CanonicalApplyPrecondition('legacyBridge' as any, 'metadataManifest'))
    return result
  }

  private libraryObjectsByID(objects: CanonicalLibraryObject[]): Record<string, CanonicalLibraryObject> {
    const result: Record<string, CanonicalLibraryObject> = {}
    for (const obj of objects) {
      const key = obj.objectID.rawValue
      if (!result[key]) {
        result[key] = obj
      }
    }
    return result
  }

  private combinedObjectIDs(local: CanonicalManifest, peer: CanonicalManifest): CanonicalLibraryObjectID[] {
    const ids = new Set<string>()
    for (const obj of local.libraryObjects) {
      ids.add(obj.objectID.rawValue)
    }
    for (const obj of peer.libraryObjects) {
      ids.add(obj.objectID.rawValue)
    }
    return [...ids].sort((a, b) => a.localeCompare(b))
      .map(s => new CanonicalLibraryObjectID(s))
  }

  private hasLibraryCapability(manifest: CanonicalManifest): boolean {
    return manifest.node.capabilities.includes(CanonicalCapability.CANONICAL_LIBRARY_OBJECTS_V1) ||
      manifest.manifestCapabilities.includes(CanonicalCapability.CANONICAL_LIBRARY_OBJECTS_V1)
  }

  private isSupported(obj: CanonicalLibraryObject): boolean {
    return obj.kind !== CanonicalObjectKind.UNKNOWN_UNSUPPORTED &&
      obj.kind !== CanonicalObjectKind.GENERATED_ARTIFACT_ENVELOPE
  }

  private sameHash(left: CanonicalHash, right: CanonicalHash): boolean {
    return left.algorithm === right.algorithm && left.value === right.value
  }

  private libraryTombstone(
    obj: CanonicalLibraryObject,
    reason: CanonicalLibraryTombstoneReason
  ): CanonicalLibraryTombstone {
    return new CanonicalLibraryTombstone(
      obj.objectID,
      obj.kind,
      obj.deletedAt,
      null,
      reason
    )
  }

  private tsString(timestamp: CanonicalTimestamp): string {
    return (timestamp.date.getTime() / 1000.0).toFixed(6)
  }
}
