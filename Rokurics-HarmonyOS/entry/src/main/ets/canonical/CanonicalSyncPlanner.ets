/**
 * CanonicalSyncPlanner — canonical sync planner for Rokurics-HarmonyOS.
 * Ported from Rokurics-Apple (CanonicalSyncPlanner.swift).
 * Plans sync actions by comparing local and peer canonical manifests.
 */

import {
  CanonicalHash, CanonicalTimestamp, CanonicalCapability,
  CanonicalManifest, CanonicalRecordingObject, CanonicalNode,
  CanonicalArtifact, CanonicalArtifactKind, nilIfEmpty, hashPrefix,
  artifactID, artifactKey, provesGeneratedArtifactAvailability,
  isAuthoritativeProducer, sameContent, generatedArtifactKinds
} from './CanonicalCore'

// ── Forward declarations for unported types ──

export class CanonicalShadowLegacyObjectFact {
  objectID: string
  hasStudyItem: boolean
  hasReceiveRecord: boolean

  constructor(objectID: string, hasStudyItem: boolean = false, hasReceiveRecord: boolean = false) {
    this.objectID = objectID
    this.hasStudyItem = hasStudyItem
    this.hasReceiveRecord = hasReceiveRecord
  }

  static merged(facts: CanonicalShadowLegacyObjectFact[]): CanonicalShadowLegacyObjectFact[] {
    const seen = new Set<string>()
    const result: CanonicalShadowLegacyObjectFact[] = []
    for (const f of facts) {
      if (!seen.has(f.objectID)) {
        seen.add(f.objectID)
        result.push(f)
      }
    }
    return result
  }
}

// ── Canonical Sync Plan Trigger ──

export enum CanonicalSyncPlanTrigger {
  MANUAL = 'manual',
  PERIODIC = 'periodic',
  APP_ACTIVATION = 'appActivation',
  RETRY_DRAINER = 'retryDrainer',
  VIEW_REFRESH = 'viewRefresh'
}

export function allowsAudioUpload(trigger: CanonicalSyncPlanTrigger): boolean {
  switch (trigger) {
    case CanonicalSyncPlanTrigger.MANUAL:
    case CanonicalSyncPlanTrigger.PERIODIC:
    case CanonicalSyncPlanTrigger.APP_ACTIVATION:
      return true
    case CanonicalSyncPlanTrigger.RETRY_DRAINER:
    case CanonicalSyncPlanTrigger.VIEW_REFRESH:
      return false
  }
}

// ── Canonical Sync Plan Reason ──

export enum CanonicalSyncPlanReason {
  CANONICAL_PLAN_USED = 'canonicalPlanUsed',
  CANONICAL_PLAN_FALLBACK = 'canonicalPlanFallback',
  INCOMPATIBLE_SCHEMA = 'incompatibleSchema',
  INVALID_MANIFEST_HASH = 'invalidManifestHash',
  MISSING_RECORDING_METADATA_CAPABILITY = 'missingRecordingMetadataCapability',
  MISSING_AUDIO_ARTIFACT_CAPABILITY = 'missingAudioArtifactCapability',
  CANONICAL_METADATA_HASH_CONVERGED = 'canonicalMetadataHashConverged',
  CANONICAL_CREATED_AT_IGNORED_FOR_METADATA_HASH = 'canonicalCreatedAtIgnoredForMetadataHash',
  CANONICAL_MODIFIED_AT_IGNORED_PROCESSING_STATE = 'canonicalModifiedAtIgnoredProcessingState',
  CANONICAL_BUSINESS_MODIFIED_AT_USED = 'canonicalBusinessModifiedAtUsed',
  PEER_MISSING_METADATA = 'peerMissingMetadata',
  LOCAL_MISSING_METADATA = 'localMissingMetadata',
  METADATA_HASH_EQUAL = 'metadataHashEqual',
  LOCAL_METADATA_NEWER = 'localMetadataNewer',
  PEER_METADATA_NEWER = 'peerMetadataNewer',
  METADATA_TIE_CONFLICT = 'metadataTieConflict',
  LEGACY_WOULD_UPLOAD_METADATA_BUT_CANONICAL_NO_OP = 'legacyWouldUploadMetadataButCanonicalNoOp',
  LEGACY_METADATA_HASH_MISMATCH_BUT_CANONICAL_HASH_MATCH = 'legacyMetadataHashMismatchButCanonicalHashMatch',
  LOCAL_AUDIO_UNAVAILABLE = 'localAudioUnavailable',
  PEER_OBJECT_ABSENT = 'peerObjectAbsent',
  PEER_AUDIO_MISSING = 'peerAudioMissing',
  PEER_AUDIO_METADATA_ONLY = 'peerAudioMetadataOnly',
  PEER_STUDY_ITEM_ONLY_WITHOUT_RECEIVE_RECORD = 'peerStudyItemOnlyWithoutReceiveRecord',
  PEER_AUDIO_UNKNOWN_DEFERRED = 'peerAudioUnknownDeferred',
  PEER_AUDIO_SAME_HASH_SAME_SIZE = 'peerAudioSameHashSameSize',
  PEER_AUDIO_HASH_CONFLICT = 'peerAudioHashConflict',
  PEER_AUDIO_SIZE_CONFLICT = 'peerAudioSizeConflict',
  VIEW_REFRESH_SUPPRESSED = 'viewRefreshSuppressed',
  RETRY_DRAINER_SUPPRESSED_NEW_JOB = 'retryDrainerSuppressedNewJob',
  CANONICAL_GENERATED_ARTIFACT_DOWNLOAD = 'canonicalGeneratedArtifactDownload',
  CANONICAL_GENERATED_ARTIFACT_PEER_SAME_NO_OP = 'canonicalGeneratedArtifactPeerSameNoOp',
  CANONICAL_GENERATED_ARTIFACT_PEER_UNKNOWN_DEFERRED = 'canonicalGeneratedArtifactPeerUnknownDeferred',
  CANONICAL_GENERATED_ARTIFACT_CONFLICT = 'canonicalGeneratedArtifactConflict',
  CANONICAL_GENERATED_ARTIFACT_UNSUPPORTED_UPLOAD = 'canonicalGeneratedArtifactUnsupportedUpload',
  CANONICAL_GENERATED_ARTIFACT_AUTHORITATIVE_PEER_NEWER = 'canonicalGeneratedArtifactAuthoritativePeerNewer',
  CANONICAL_GENERATED_ARTIFACT_LOCAL_PRODUCER_NO_ROUTE = 'canonicalGeneratedArtifactLocalProducerNoRoute',
  LEGACY_WOULD_DOWNLOAD_ARTIFACT_BUT_CANONICAL_NO_OP = 'legacyWouldDownloadArtifactButCanonicalNoOp',
  LEGACY_ARTIFACT_MISMATCH_BUT_CANONICAL_RESOLVED = 'legacyArtifactMismatchButCanonicalResolved'
}

// ── Canonical Sync Plan Error ──

export class CanonicalSyncPlanError extends Error {
  kind: string
  localSchemaVersion: number | null
  peerSchemaVersion: number | null
  side: string | null
  capability: CanonicalCapability | null

  private constructor(
    kind: string,
    localSchemaVersion: number | null = null,
    peerSchemaVersion: number | null = null,
    side: string | null = null,
    capability: CanonicalCapability | null = null
  ) {
    let message = kind
    if (localSchemaVersion !== null && peerSchemaVersion !== null) {
      message = `incompatibleSchema(local=${localSchemaVersion},peer=${peerSchemaVersion})`
    }
    if (side !== null) {
      message = `invalidManifestHash(side=${side})`
    }
    if (side !== null && capability !== null) {
      message = `missingCapability(side=${side},capability=${capability})`
    }
    super(message)
    this.kind = kind
    this.localSchemaVersion = localSchemaVersion
    this.peerSchemaVersion = peerSchemaVersion
    this.side = side
    this.capability = capability
  }

  static incompatibleSchema(local: number, peer: number): CanonicalSyncPlanError {
    return new CanonicalSyncPlanError('incompatibleSchema', local, peer)
  }

  static invalidManifestHash(side: string): CanonicalSyncPlanError {
    return new CanonicalSyncPlanError('invalidManifestHash', null, null, side)
  }

  static missingCapability(side: string, capability: CanonicalCapability): CanonicalSyncPlanError {
    return new CanonicalSyncPlanError('missingCapability', null, null, side, capability)
  }
}

// ── Canonical Recording Metadata Action ──

export class CanonicalRecordingMetadataAction {
  objectID: string
  reason: CanonicalSyncPlanReason
  localMetadataHash: CanonicalHash | null
  peerMetadataHash: CanonicalHash | null
  localModifiedAt: CanonicalTimestamp | null
  peerModifiedAt: CanonicalTimestamp | null

  constructor(
    objectID: string,
    reason: CanonicalSyncPlanReason,
    localMetadataHash: CanonicalHash | null = null,
    peerMetadataHash: CanonicalHash | null = null,
    localModifiedAt: CanonicalTimestamp | null = null,
    peerModifiedAt: CanonicalTimestamp | null = null
  ) {
    this.objectID = objectID
    this.reason = reason
    this.localMetadataHash = localMetadataHash
    this.peerMetadataHash = peerMetadataHash
    this.localModifiedAt = localModifiedAt
    this.peerModifiedAt = peerModifiedAt
  }

  get id(): string {
    return `${this.objectID}:${this.reason}`
  }
}

// ── Canonical Artifact Transfer Action ──

export class CanonicalArtifactTransferAction {
  objectID: string
  artifactID: string | null
  kind: CanonicalArtifactKind | null
  logicalPathToken: string | null
  reason: CanonicalSyncPlanReason
  localHash: CanonicalHash | null
  peerHash: CanonicalHash | null
  localByteSize: number | null
  peerByteSize: number | null

  constructor(
    objectID: string,
    artifactID: string | null = null,
    kind: CanonicalArtifactKind | null = null,
    logicalPathToken: string | null = null,
    reason: CanonicalSyncPlanReason,
    localHash: CanonicalHash | null = null,
    peerHash: CanonicalHash | null = null,
    localByteSize: number | null = null,
    peerByteSize: number | null = null
  ) {
    this.objectID = objectID
    this.artifactID = artifactID ?? null
    this.kind = kind
    this.logicalPathToken = logicalPathToken ?? null
    this.reason = reason
    this.localHash = localHash ?? null
    this.peerHash = peerHash ?? null
    this.localByteSize = localByteSize ?? null
    this.peerByteSize = peerByteSize ?? null
  }

  get id(): string {
    return `${this.objectID}:${this.artifactID ?? 'audio'}:${this.reason}`
  }
}

// ── Canonical Sync Plan Bridge Diagnostics ──

export class CanonicalSyncPlanBridgeDiagnostics {
  phase: string
  reason: CanonicalSyncPlanReason
  objectID: string | null
  artifactID: string | null
  detail: string | null

  constructor(
    phase: string,
    reason: CanonicalSyncPlanReason,
    objectID: string | null = null,
    artifactID: string | null = null,
    detail: string | null = null
  ) {
    this.phase = phase
    this.reason = reason
    this.objectID = objectID ?? null
    this.artifactID = artifactID ?? null
    this.detail = detail ?? null
  }

  get id(): string {
    return [this.phase, this.reason, this.objectID ?? '', this.artifactID ?? ''].join('|')
  }
}

// ── Canonical Sync Planner Legacy Context ──

export class CanonicalSyncPlannerLegacyContext {
  legacyUploadMetadataObjectIDs: string[]
  legacyDownloadMetadataObjectIDs: string[]
  legacyUploadAudioObjectIDs: string[]
  legacyDownloadGeneratedArtifactKeys: string[]
  legacyConflictGeneratedArtifactKeys: string[]
  peerObjectFacts: CanonicalShadowLegacyObjectFact[]

  constructor(
    legacyUploadMetadataObjectIDs: string[] = [],
    legacyDownloadMetadataObjectIDs: string[] = [],
    legacyUploadAudioObjectIDs: string[] = [],
    legacyDownloadGeneratedArtifactKeys: string[] = [],
    legacyConflictGeneratedArtifactKeys: string[] = [],
    peerObjectFacts: CanonicalShadowLegacyObjectFact[] = []
  ) {
    this.legacyUploadMetadataObjectIDs = CanonicalSyncPlannerLegacyContext.normalizedIDs(legacyUploadMetadataObjectIDs)
    this.legacyDownloadMetadataObjectIDs = CanonicalSyncPlannerLegacyContext.normalizedIDs(legacyDownloadMetadataObjectIDs)
    this.legacyUploadAudioObjectIDs = CanonicalSyncPlannerLegacyContext.normalizedIDs(legacyUploadAudioObjectIDs)
    this.legacyDownloadGeneratedArtifactKeys = CanonicalSyncPlannerLegacyContext.normalizedIDs(legacyDownloadGeneratedArtifactKeys)
    this.legacyConflictGeneratedArtifactKeys = CanonicalSyncPlannerLegacyContext.normalizedIDs(legacyConflictGeneratedArtifactKeys)
    this.peerObjectFacts = CanonicalShadowLegacyObjectFact.merged(peerObjectFacts)
  }

  peerFact(objectID: string): CanonicalShadowLegacyObjectFact | null {
    for (const fact of this.peerObjectFacts) {
      if (fact.objectID === objectID) {
        return fact
      }
    }
    return null
  }

  legacyWouldMoveMetadata(objectID: string): boolean {
    return this.legacyUploadMetadataObjectIDs.indexOf(objectID) >= 0 ||
      this.legacyDownloadMetadataObjectIDs.indexOf(objectID) >= 0
  }

  legacyWouldDownloadGeneratedArtifact(objectID: string, kind: CanonicalArtifactKind): boolean {
    return this.legacyDownloadGeneratedArtifactKeys.indexOf(artifactKey(objectID, kind)) >= 0
  }

  legacyHadGeneratedArtifactConflict(objectID: string, kind: CanonicalArtifactKind): boolean {
    return this.legacyConflictGeneratedArtifactKeys.indexOf(artifactKey(objectID, kind)) >= 0
  }

  private static normalizedIDs(ids: string[]): string[] {
    const filtered = ids.map((id: string) => nilIfEmpty(id.trim())).filter(
      (id: string | null): boolean => id !== null
    ) as string[]
    return [...new Set(filtered)].sort()
  }
}

// ── Canonical Sync Plan ──

export class CanonicalSyncPlan {
  uploadRecordingMetadata: CanonicalRecordingMetadataAction[] = []
  downloadRecordingMetadata: CanonicalRecordingMetadataAction[] = []
  noOpRecordingMetadata: CanonicalRecordingMetadataAction[] = []
  conflictRecordingMetadata: CanonicalRecordingMetadataAction[] = []
  uploadAudioArtifact: CanonicalArtifactTransferAction[] = []
  deferAudioArtifact: CanonicalArtifactTransferAction[] = []
  noOpAudioArtifact: CanonicalArtifactTransferAction[] = []
  conflictAudioArtifact: CanonicalArtifactTransferAction[] = []
  downloadGeneratedArtifact: CanonicalArtifactTransferAction[] = []
  deferGeneratedArtifact: CanonicalArtifactTransferAction[] = []
  noOpGeneratedArtifact: CanonicalArtifactTransferAction[] = []
  conflictGeneratedArtifact: CanonicalArtifactTransferAction[] = []
  diagnostics: CanonicalSyncPlanBridgeDiagnostics[] = []
}

// ── Canonical Sync Planner ──

export class CanonicalSyncPlanner {
  constructor() {}

  plan(
    local: CanonicalManifest,
    peer: CanonicalManifest,
    trigger: CanonicalSyncPlanTrigger,
    legacyContext: CanonicalSyncPlannerLegacyContext | null = null
  ): CanonicalSyncPlan {
    this.validate(local, peer)

    const localObjects: Record<string, CanonicalRecordingObject> = {}
    for (const o of local.objects) {
      localObjects[o.objectID] = o
    }
    const peerObjects: Record<string, CanonicalRecordingObject> = {}
    for (const o of peer.objects) {
      peerObjects[o.objectID] = o
    }

    const objectIDSet = new Set(Object.keys(localObjects))
    for (const key of Object.keys(peerObjects)) {
      objectIDSet.add(key)
    }
    const objectIDs = [...objectIDSet].sort()
    const plan = new CanonicalSyncPlan()
    plan.diagnostics.push(
      new CanonicalSyncPlanBridgeDiagnostics(
        'canonicalPlanUsed',
        CanonicalSyncPlanReason.CANONICAL_PLAN_USED,
        null,
        null,
        `objects=${objectIDs.length}`
      )
    )

    for (const objectID of objectIDs) {
      const localObject = localObjects[objectID] ?? null
      const peerObject = peerObjects[objectID] ?? null
      this.appendMetadataDecision(objectID, localObject, peerObject, legacyContext, plan)
      if (localObject) {
        this.appendAudioDecision(objectID, localObject, peerObject, trigger, legacyContext, plan)
      }
      this.appendGeneratedArtifactDecisions(
        objectID, localObject, peerObject, local.node, peer.node, trigger, legacyContext, plan
      )
    }

    return plan
  }

  private validate(local: CanonicalManifest, peer: CanonicalManifest): void {
    if (local.schemaVersion !== CanonicalManifest.CURRENT_SCHEMA_VERSION ||
      peer.schemaVersion !== CanonicalManifest.CURRENT_SCHEMA_VERSION) {
      throw CanonicalSyncPlanError.incompatibleSchema(local.schemaVersion, peer.schemaVersion)
    }
    if (!local.hasValidManifestHash) {
      throw CanonicalSyncPlanError.invalidManifestHash('local')
    }
    if (!peer.hasValidManifestHash) {
      throw CanonicalSyncPlanError.invalidManifestHash('peer')
    }
    if (local.node.capabilities.indexOf(CanonicalCapability.RECORDING_METADATA) < 0) {
      throw CanonicalSyncPlanError.missingCapability('local', CanonicalCapability.RECORDING_METADATA)
    }
    if (peer.node.capabilities.indexOf(CanonicalCapability.RECORDING_METADATA) < 0) {
      throw CanonicalSyncPlanError.missingCapability('peer', CanonicalCapability.RECORDING_METADATA)
    }
    if (local.node.capabilities.indexOf(CanonicalCapability.AUDIO_ARTIFACT) < 0) {
      throw CanonicalSyncPlanError.missingCapability('local', CanonicalCapability.AUDIO_ARTIFACT)
    }
    if (peer.node.capabilities.indexOf(CanonicalCapability.AUDIO_ARTIFACT) < 0) {
      throw CanonicalSyncPlanError.missingCapability('peer', CanonicalCapability.AUDIO_ARTIFACT)
    }
  }

  private appendMetadataDecision(
    objectID: string,
    localObject: CanonicalRecordingObject | null,
    peerObject: CanonicalRecordingObject | null,
    legacyContext: CanonicalSyncPlannerLegacyContext | null,
    plan: CanonicalSyncPlan
  ): void {
    if (localObject && peerObject) {
      if (this.sameHashNullable(localObject.metadataHash, peerObject.metadataHash)) {
        const action = this.metadataAction(objectID, CanonicalSyncPlanReason.METADATA_HASH_EQUAL, localObject, peerObject)
        plan.noOpRecordingMetadata.push(action)
        plan.diagnostics.push(
          new CanonicalSyncPlanBridgeDiagnostics(
            'canonicalMetadataHashConverged',
            CanonicalSyncPlanReason.CANONICAL_METADATA_HASH_CONVERGED,
            objectID,
            null,
            `canonicalMetadataHash=${this.hashPrefix(localObject.metadataHash ?? null)}`
          )
        )
        if (localObject.metadata.createdAt.date.getTime() !== peerObject.metadata.createdAt.date.getTime()) {
          plan.diagnostics.push(
            new CanonicalSyncPlanBridgeDiagnostics(
              'canonicalCreatedAtIgnoredForMetadataHash',
              CanonicalSyncPlanReason.CANONICAL_CREATED_AT_IGNORED_FOR_METADATA_HASH,
              objectID,
              null,
              `canonicalMetadataHash=${this.hashPrefix(localObject.metadataHash ?? null)}`
            )
          )
        }
        if (localObject.processingState.transcription !== peerObject.processingState.transcription ||
          localObject.processingState.note !== peerObject.processingState.note) {
          plan.diagnostics.push(
            new CanonicalSyncPlanBridgeDiagnostics(
              'canonicalModifiedAtIgnoredProcessingState',
              CanonicalSyncPlanReason.CANONICAL_MODIFIED_AT_IGNORED_PROCESSING_STATE,
              objectID,
              null,
              `canonicalMetadataHash=${this.hashPrefix(localObject.metadataHash ?? null)}`
            )
          )
        }
        if (legacyContext?.legacyWouldMoveMetadata(objectID) === true) {
          plan.diagnostics.push(
            new CanonicalSyncPlanBridgeDiagnostics(
              'legacyWouldUploadMetadataButCanonicalNoOp',
              CanonicalSyncPlanReason.LEGACY_WOULD_UPLOAD_METADATA_BUT_CANONICAL_NO_OP,
              objectID,
              null,
              `canonicalMetadataHash=${this.hashPrefix(localObject.metadataHash ?? null)}`
            )
          )
          plan.diagnostics.push(
            new CanonicalSyncPlanBridgeDiagnostics(
              'legacyMetadataHashMismatchButCanonicalHashMatch',
              CanonicalSyncPlanReason.LEGACY_METADATA_HASH_MISMATCH_BUT_CANONICAL_HASH_MATCH,
              objectID,
              null,
              `canonicalMetadataHash=${this.hashPrefix(localObject.metadataHash ?? null)}`
            )
          )
        }
      } else if (localObject.metadata.modifiedAt.date.getTime() > peerObject.metadata.modifiedAt.date.getTime()) {
        plan.uploadRecordingMetadata.push(
          this.metadataAction(objectID, CanonicalSyncPlanReason.LOCAL_METADATA_NEWER, localObject, peerObject)
        )
        this.appendBusinessModifiedAtDiagnostic(objectID, 'upload', localObject, peerObject, plan)
      } else if (peerObject.metadata.modifiedAt.date.getTime() > localObject.metadata.modifiedAt.date.getTime()) {
        plan.downloadRecordingMetadata.push(
          this.metadataAction(objectID, CanonicalSyncPlanReason.PEER_METADATA_NEWER, localObject, peerObject)
        )
        this.appendBusinessModifiedAtDiagnostic(objectID, 'download', localObject, peerObject, plan)
      } else {
        plan.conflictRecordingMetadata.push(
          this.metadataAction(objectID, CanonicalSyncPlanReason.METADATA_TIE_CONFLICT, localObject, peerObject)
        )
      }
    } else if (localObject && !peerObject) {
      plan.uploadRecordingMetadata.push(
        this.metadataAction(objectID, CanonicalSyncPlanReason.PEER_MISSING_METADATA, localObject, null)
      )
    } else if (!localObject && peerObject) {
      plan.downloadRecordingMetadata.push(
        this.metadataAction(objectID, CanonicalSyncPlanReason.LOCAL_MISSING_METADATA, null, peerObject)
      )
    }
  }

  private appendAudioDecision(
    objectID: string,
    localObject: CanonicalRecordingObject,
    peerObject: CanonicalRecordingObject | null,
    trigger: CanonicalSyncPlanTrigger,
    legacyContext: CanonicalSyncPlannerLegacyContext | null,
    plan: CanonicalSyncPlan
  ): void {
    const localAudio = localObject.audioArtifact
    if (!localAudio || !localAudio.provesCanonicalAudioAvailability) {
      plan.deferAudioArtifact.push(
        this.audioAction(objectID, localObject.audioArtifact, peerObject?.audioArtifact ?? null, CanonicalSyncPlanReason.LOCAL_AUDIO_UNAVAILABLE)
      )
      return
    }

    if (trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH) {
      plan.deferAudioArtifact.push(
        this.audioAction(objectID, localAudio, peerObject?.audioArtifact ?? null, CanonicalSyncPlanReason.VIEW_REFRESH_SUPPRESSED)
      )
      return
    }

    if (trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) {
      plan.deferAudioArtifact.push(
        this.audioAction(objectID, localAudio, peerObject?.audioArtifact ?? null, CanonicalSyncPlanReason.RETRY_DRAINER_SUPPRESSED_NEW_JOB)
      )
      return
    }

    if (!peerObject) {
      plan.uploadAudioArtifact.push(
        this.audioAction(objectID, localAudio, null, CanonicalSyncPlanReason.PEER_OBJECT_ABSENT)
      )
      return
    }

    const peerAudio = peerObject.audioArtifact
    if (!peerAudio) {
      plan.uploadAudioArtifact.push(
        this.audioAction(objectID, localAudio, null,
          this.peerAudioMissingReason(objectID, legacyContext))
      )
      return
    }

    switch (peerAudio.availability) {
      case 'missing':
        plan.uploadAudioArtifact.push(
          this.audioAction(objectID, localAudio, peerAudio,
            this.peerAudioMissingReason(objectID, legacyContext))
        )
        break
      case 'unknown':
      case 'availableWithoutHash':
        plan.deferAudioArtifact.push(
          this.audioAction(objectID, localAudio, peerAudio, CanonicalSyncPlanReason.PEER_AUDIO_UNKNOWN_DEFERRED)
        )
        break
      case 'available':
        if (!peerAudio.contentHash || peerAudio.byteSize === null) {
          plan.deferAudioArtifact.push(
            this.audioAction(objectID, localAudio, peerAudio, CanonicalSyncPlanReason.PEER_AUDIO_UNKNOWN_DEFERRED)
          )
          return
        }
        if (!this.sameHashNullable(localAudio.contentHash, peerAudio.contentHash)) {
          plan.conflictAudioArtifact.push(
            this.audioAction(objectID, localAudio, peerAudio, CanonicalSyncPlanReason.PEER_AUDIO_HASH_CONFLICT)
          )
        } else if (localAudio.byteSize !== peerAudio.byteSize) {
          plan.conflictAudioArtifact.push(
            this.audioAction(objectID, localAudio, peerAudio, CanonicalSyncPlanReason.PEER_AUDIO_SIZE_CONFLICT)
          )
        } else {
          plan.noOpAudioArtifact.push(
            this.audioAction(objectID, localAudio, peerAudio, CanonicalSyncPlanReason.PEER_AUDIO_SAME_HASH_SAME_SIZE)
          )
        }
        break
    }
  }

  private appendGeneratedArtifactDecisions(
    objectID: string,
    localObject: CanonicalRecordingObject | null,
    peerObject: CanonicalRecordingObject | null,
    localNode: CanonicalNode,
    peerNode: CanonicalNode,
    trigger: CanonicalSyncPlanTrigger,
    legacyContext: CanonicalSyncPlannerLegacyContext | null,
    plan: CanonicalSyncPlan
  ): void {
    const localArtifacts = this.generatedArtifactsByKind(localObject)
    const peerArtifacts = this.generatedArtifactsByKind(peerObject)
    const kindSet = new Set<string>()
    for (const k of Object.keys(localArtifacts)) {
      kindSet.add(k)
    }
    for (const k of Object.keys(peerArtifacts)) {
      kindSet.add(k)
    }
    const kinds = [...kindSet].sort()
    for (const kindStr of kinds) {
      const kind = kindStr as CanonicalArtifactKind
      this.appendGeneratedArtifactDecision(
        objectID, kind,
        localArtifacts[kind] ?? null,
        peerArtifacts[kind] ?? null,
        localNode, peerNode, trigger, legacyContext, plan
      )
    }
  }

  private appendGeneratedArtifactDecision(
    objectID: string,
    kind: CanonicalArtifactKind,
    localArtifact: CanonicalArtifact | null,
    peerArtifact: CanonicalArtifact | null,
    localNode: CanonicalNode,
    peerNode: CanonicalNode,
    trigger: CanonicalSyncPlanTrigger,
    legacyContext: CanonicalSyncPlannerLegacyContext | null,
    plan: CanonicalSyncPlan
  ): void {
    if (localArtifact?.tombstone === true || peerArtifact?.tombstone === true) {
      this.appendGeneratedDefer(
        objectID, kind, localArtifact, peerArtifact,
        CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_PEER_UNKNOWN_DEFERRED,
        'tombstonePresent', plan
      )
      return
    }

    const localProven = provesGeneratedArtifactAvailability(localArtifact)
    const peerProven = provesGeneratedArtifactAvailability(peerArtifact)
    const peerAuthoritative = peerArtifact ? isAuthoritativeProducer(peerArtifact, peerNode) : false
    const localAuthoritative = localArtifact ? isAuthoritativeProducer(localArtifact, localNode) : false

    if (peerProven && !localProven) {
      if (!localArtifact || localArtifact.availability === 'missing') {
        this.appendGeneratedDownloadIfAllowed(
          objectID, kind, localArtifact, peerArtifact, trigger,
          CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_DOWNLOAD,
          'localMissingPeerAvailable', legacyContext, plan
        )
      } else if (peerAuthoritative) {
        this.appendGeneratedDownloadIfAllowed(
          objectID, kind, localArtifact, peerArtifact, trigger,
          CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_DOWNLOAD,
          'localUnknownPeerAuthoritative', legacyContext, plan
        )
      } else {
        this.appendGeneratedDefer(
          objectID, kind, localArtifact, peerArtifact,
          CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_PEER_UNKNOWN_DEFERRED,
          'localUnknownPeerNotAuthoritative', plan
        )
      }
      return
    }

    if (!peerProven) {
      if (peerArtifact && peerArtifact.availability !== 'missing') {
        this.appendGeneratedDefer(
          objectID, kind, localArtifact, peerArtifact,
          CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_PEER_UNKNOWN_DEFERRED,
          'peerUnproven', plan
        )
      } else if (localProven && localAuthoritative) {
        this.appendGeneratedDefer(
          objectID, kind, localArtifact, peerArtifact,
          CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_LOCAL_PRODUCER_NO_ROUTE,
          'localAuthoritativeUploadUnsupported', plan
        )
        this.appendGeneratedDiagnosticByArtifact(
          objectID,
          localArtifact?.artifactID ?? peerArtifact?.artifactID ?? null,
          kind,
          CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_UNSUPPORTED_UPLOAD,
          localArtifact, peerArtifact,
          'generatedArtifactUploadNotPlanned', plan
        )
      } else if (localProven) {
        this.appendGeneratedDefer(
          objectID, kind, localArtifact, peerArtifact,
          CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_UNSUPPORTED_UPLOAD,
          'localGeneratedArtifactNotAuthoritative', plan
        )
      } else {
        this.appendGeneratedDefer(
          objectID, kind, localArtifact, peerArtifact,
          CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_PEER_UNKNOWN_DEFERRED,
          'bothUnproven', plan
        )
      }
      return
    }

    if (!localArtifact || !peerArtifact) {
      return
    }

    if (sameContent(localArtifact, peerArtifact)) {
      const action = this.generatedAction(
        objectID, kind, localArtifact, peerArtifact,
        CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_PEER_SAME_NO_OP
      )
      plan.noOpGeneratedArtifact.push(action)
      this.appendGeneratedDiagnosticByAction(action, undefined, 'sameHashAndSize', plan)
      if (legacyContext?.legacyWouldDownloadGeneratedArtifact(objectID, kind) === true) {
        this.appendGeneratedDiagnosticByAction(
          action,
          CanonicalSyncPlanReason.LEGACY_WOULD_DOWNLOAD_ARTIFACT_BUT_CANONICAL_NO_OP,
          'canonicalGeneratedArtifactSame', plan
        )
      }
      return
    }

    if (peerAuthoritative &&
      peerArtifact.modifiedAt && localArtifact.modifiedAt &&
      peerArtifact.modifiedAt.date.getTime() > localArtifact.modifiedAt.date.getTime()) {
      this.appendGeneratedDownloadIfAllowed(
        objectID, kind, localArtifact, peerArtifact, trigger,
        CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_AUTHORITATIVE_PEER_NEWER,
        'peerAuthoritativeNewer', legacyContext, plan
      )
      if (legacyContext?.legacyHadGeneratedArtifactConflict(objectID, kind) === true) {
        this.appendGeneratedDiagnosticByArtifact(
          objectID, peerArtifact.artifactID, kind,
          CanonicalSyncPlanReason.LEGACY_ARTIFACT_MISMATCH_BUT_CANONICAL_RESOLVED,
          localArtifact, peerArtifact, 'peerAuthoritativeNewer', plan
        )
      }
      return
    }

    if (localAuthoritative &&
      peerArtifact.modifiedAt && localArtifact.modifiedAt &&
      localArtifact.modifiedAt.date.getTime() > peerArtifact.modifiedAt.date.getTime()) {
      this.appendGeneratedDefer(
        objectID, kind, localArtifact, peerArtifact,
        CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_LOCAL_PRODUCER_NO_ROUTE,
        'localAuthoritativeNewerUploadUnsupported', plan
      )
      return
    }

    const action = this.generatedAction(
      objectID, kind, localArtifact, peerArtifact,
      CanonicalSyncPlanReason.CANONICAL_GENERATED_ARTIFACT_CONFLICT
    )
    plan.conflictGeneratedArtifact.push(action)
    this.appendGeneratedDiagnosticByAction(action, undefined, 'hashOrSizeMismatch', plan)
  }

  private appendGeneratedDownloadIfAllowed(
    objectID: string,
    kind: CanonicalArtifactKind,
    localArtifact: CanonicalArtifact | null,
    peerArtifact: CanonicalArtifact | null,
    trigger: CanonicalSyncPlanTrigger,
    reason: CanonicalSyncPlanReason,
    detail: string,
    legacyContext: CanonicalSyncPlannerLegacyContext | null,
    plan: CanonicalSyncPlan
  ): void {
    if (trigger === CanonicalSyncPlanTrigger.VIEW_REFRESH) {
      this.appendGeneratedDefer(
        objectID, kind, localArtifact, peerArtifact,
        CanonicalSyncPlanReason.VIEW_REFRESH_SUPPRESSED, detail, plan
      )
      return
    }
    if (trigger === CanonicalSyncPlanTrigger.RETRY_DRAINER) {
      this.appendGeneratedDefer(
        objectID, kind, localArtifact, peerArtifact,
        CanonicalSyncPlanReason.RETRY_DRAINER_SUPPRESSED_NEW_JOB, detail, plan
      )
      return
    }
    const action = this.generatedAction(objectID, kind, localArtifact, peerArtifact, reason)
    plan.downloadGeneratedArtifact.push(action)
    this.appendGeneratedDiagnosticByAction(action, undefined, detail, plan)
    if (legacyContext?.legacyHadGeneratedArtifactConflict(objectID, kind) === true) {
      this.appendGeneratedDiagnosticByAction(
        action, CanonicalSyncPlanReason.LEGACY_ARTIFACT_MISMATCH_BUT_CANONICAL_RESOLVED, detail, plan
      )
    }
  }

  private appendGeneratedDefer(
    objectID: string,
    kind: CanonicalArtifactKind,
    localArtifact: CanonicalArtifact | null,
    peerArtifact: CanonicalArtifact | null,
    reason: CanonicalSyncPlanReason,
    detail: string,
    plan: CanonicalSyncPlan
  ): void {
    const action = this.generatedAction(objectID, kind, localArtifact, peerArtifact, reason)
    plan.deferGeneratedArtifact.push(action)
    this.appendGeneratedDiagnosticByAction(action, undefined, detail, plan)
  }

  private generatedArtifactsByKind(object: CanonicalRecordingObject | null): Record<string, CanonicalArtifact> {
    if (!object) {
      return {}
    }
    const genKinds = generatedArtifactKinds()
    const result: Record<string, CanonicalArtifact> = {}
    for (const artifact of object.artifacts) {
      if (genKinds.indexOf(artifact.kind) < 0) {
        continue
      }
      const existing = result[artifact.kind as string]
      const epochTimestamp = new CanonicalTimestamp(new Date(0))
      if (!existing ||
        (artifact.modifiedAt ?? epochTimestamp).date.getTime() >
        (existing.modifiedAt ?? epochTimestamp).date.getTime()) {
        result[artifact.kind as string] = artifact
      }
    }
    return result
  }

  private peerAudioMissingReason(
    objectID: string,
    legacyContext: CanonicalSyncPlannerLegacyContext | null
  ): CanonicalSyncPlanReason {
    const fact = legacyContext?.peerFact(objectID) ?? null
    if (!fact) {
      return CanonicalSyncPlanReason.PEER_AUDIO_MISSING
    }
    if (fact.hasStudyItem && !fact.hasReceiveRecord) {
      return CanonicalSyncPlanReason.PEER_STUDY_ITEM_ONLY_WITHOUT_RECEIVE_RECORD
    }
    if (fact.hasReceiveRecord) {
      return CanonicalSyncPlanReason.PEER_AUDIO_METADATA_ONLY
    }
    return CanonicalSyncPlanReason.PEER_AUDIO_MISSING
  }

  private metadataAction(
    objectID: string,
    reason: CanonicalSyncPlanReason,
    localObject: CanonicalRecordingObject | null,
    peerObject: CanonicalRecordingObject | null
  ): CanonicalRecordingMetadataAction {
    return new CanonicalRecordingMetadataAction(
      objectID,
      reason,
      localObject?.metadataHash ?? null,
      peerObject?.metadataHash ?? null,
      localObject?.metadata.modifiedAt ?? null,
      peerObject?.metadata.modifiedAt ?? null
    )
  }

  private audioAction(
    objectID: string,
    localAudio: CanonicalArtifact | null,
    peerAudio: CanonicalArtifact | null,
    reason: CanonicalSyncPlanReason
  ): CanonicalArtifactTransferAction {
    return new CanonicalArtifactTransferAction(
      objectID,
      localAudio?.artifactID ?? peerAudio?.artifactID ?? null,
      CanonicalArtifactKind.AUDIO,
      null,
      reason,
      localAudio?.contentHash ?? null,
      peerAudio?.contentHash ?? null,
      localAudio?.byteSize ?? null,
      peerAudio?.byteSize ?? null
    )
  }

  private generatedAction(
    objectID: string,
    kind: CanonicalArtifactKind,
    localArtifact: CanonicalArtifact | null,
    peerArtifact: CanonicalArtifact | null,
    reason: CanonicalSyncPlanReason
  ): CanonicalArtifactTransferAction {
    return new CanonicalArtifactTransferAction(
      objectID,
      peerArtifact?.artifactID ?? localArtifact?.artifactID ?? artifactID(objectID, kind),
      kind,
      peerArtifact?.logicalPathToken ?? localArtifact?.logicalPathToken ?? null,
      reason,
      localArtifact?.contentHash ?? null,
      peerArtifact?.contentHash ?? null,
      localArtifact?.byteSize ?? null,
      peerArtifact?.byteSize ?? null
    )
  }

  private appendGeneratedDiagnosticByAction(
    action: CanonicalArtifactTransferAction,
    overrideReason: CanonicalSyncPlanReason | undefined,
    detail: string,
    plan: CanonicalSyncPlan
  ): void {
    plan.diagnostics.push(
      new CanonicalSyncPlanBridgeDiagnostics(
        (overrideReason ?? action.reason) as string,
        overrideReason ?? action.reason,
        action.objectID,
        action.artifactID,
        [
          `kind=${action.kind ?? 'unknown'}`,
          `detail=${detail}`,
          `localHash=${this.hashPrefix(action.localHash)}`,
          `peerHash=${this.hashPrefix(action.peerHash)}`,
          `localSize=${action.localByteSize !== null ? String(action.localByteSize) : 'missing'}`,
          `peerSize=${action.peerByteSize !== null ? String(action.peerByteSize) : 'missing'}`
        ].join(';')
      )
    )
  }

  private appendGeneratedDiagnosticByArtifact(
    objectID: string,
    artifactID: string | null,
    kind: CanonicalArtifactKind,
    reason: CanonicalSyncPlanReason,
    localArtifact: CanonicalArtifact | null,
    peerArtifact: CanonicalArtifact | null,
    detail: string,
    plan: CanonicalSyncPlan
  ): void {
    const action = this.generatedAction(objectID, kind, localArtifact, peerArtifact, reason)
    const adjustedArtifactID = artifactID ?? action.artifactID
    const diagnosticAction = new CanonicalArtifactTransferAction(
      action.objectID,
      adjustedArtifactID,
      action.kind,
      action.logicalPathToken,
      reason,
      action.localHash,
      action.peerHash,
      action.localByteSize,
      action.peerByteSize
    )
    this.appendGeneratedDiagnosticByAction(diagnosticAction, reason, detail, plan)
  }

  private appendBusinessModifiedAtDiagnostic(
    objectID: string,
    direction: string,
    localObject: CanonicalRecordingObject,
    peerObject: CanonicalRecordingObject,
    plan: CanonicalSyncPlan
  ): void {
    plan.diagnostics.push(
      new CanonicalSyncPlanBridgeDiagnostics(
        'canonicalBusinessModifiedAtUsed',
        CanonicalSyncPlanReason.CANONICAL_BUSINESS_MODIFIED_AT_USED,
        objectID,
        null,
        [
          `direction=${direction}`,
          `localModifiedAt=${this.timestampSeconds(localObject.metadata.modifiedAt)}`,
          `peerModifiedAt=${this.timestampSeconds(peerObject.metadata.modifiedAt)}`
        ].join(';')
      )
    )
  }

  private sameHashNullable(left: CanonicalHash | null, right: CanonicalHash | null): boolean {
    return left?.algorithm === right?.algorithm && left?.value === right?.value
  }

  private hashPrefix(hash: CanonicalHash | null): string {
    const value = hash?.value.trim()
    if (!value || value.length === 0) {
      return 'missing'
    }
    return value.substring(0, 12)
  }

  private timestampSeconds(timestamp: CanonicalTimestamp): string {
    return (timestamp.date.getTime() / 1000.0).toFixed(6)
  }
}
