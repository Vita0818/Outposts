/**
 * CanonicalRuntimeHarness — two-node integration test harness (iPhone/Mac).
 * Ported from Rokurics-Apple (CanonicalRuntimeHarness.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalNode, CanonicalManifest,
  CanonicalRecordingObject, CanonicalArtifact, CanonicalCapability,
  CanonicalRecordingMetadata, CanonicalArtifactKind, CanonicalArtifactAvailability,
  CanonicalArtifactProducer, CanonicalArtifactFact, nilIfEmpty, hashPrefix,
  safeLogicalPathToken, artifactID, makeArtifact, provesGeneratedArtifactAvailability
} from './CanonicalCore'
import {
  CanonicalRootToken, CanonicalFileReference, CanonicalFilePurpose,
  CanonicalFileWriteIntent, CanonicalFileReadRequest,
  CanonicalFileHashPolicy, CanonicalFileConflictPolicy,
  CanonicalFileWriteDisposition, CanonicalFileStorePort,
  InMemoryCanonicalFileStore, CanonicalFileRuntimeError
} from './CanonicalFileRuntime'
import {
  CanonicalTransportRoute, CanonicalTransportPort,
  InMemoryCanonicalTransportRuntime
} from './CanonicalTransportRuntime'
import {
  CanonicalUploadSessionID, CanonicalUploadStartRequest, CanonicalUploadChunk,
  CanonicalUploadFinalizeRequest, CanonicalUploadSessionStatus,
  CanonicalUploadRuntimeError, CanonicalResumableUploadRuntime
} from './CanonicalUploadRuntime'

// =============================================================================
// HARNESS NODE ROLE
// =============================================================================

export enum CanonicalRuntimeHarnessNodeRole {
  IPHONE = 'iPhone',
  MAC = 'mac'
}

// =============================================================================
// HARNESS TICK RESULT
// =============================================================================

export class CanonicalRuntimeHarnessTickResult {
  localRole: CanonicalRuntimeHarnessNodeRole
  peerRole: CanonicalRuntimeHarnessNodeRole
  syncPlan: CanonicalSyncPlan
  applyPlan: CanonicalApplyPlan
  libraryPlan: CanonicalLibrarySyncPlan
  executionReport: CanonicalApplyExecutionReport
  projection: CanonicalLibraryProjection
  runtimeReadiness: CanonicalRuntimeReadinessReport

  constructor(
    localRole: CanonicalRuntimeHarnessNodeRole,
    peerRole: CanonicalRuntimeHarnessNodeRole,
    syncPlan: CanonicalSyncPlan,
    applyPlan: CanonicalApplyPlan,
    libraryPlan: CanonicalLibrarySyncPlan,
    executionReport: CanonicalApplyExecutionReport,
    projection: CanonicalLibraryProjection,
    runtimeReadiness: CanonicalRuntimeReadinessReport
  ) {
    this.localRole = localRole
    this.peerRole = peerRole
    this.syncPlan = syncPlan
    this.applyPlan = applyPlan
    this.libraryPlan = libraryPlan
    this.executionReport = executionReport
    this.projection = projection
    this.runtimeReadiness = runtimeReadiness
  }
}

export class CanonicalRuntimeHarnessUploadResult {
  sessionID: CanonicalUploadSessionID | null
  confirmedBytes: number
  completed: boolean
  checksum: CanonicalHash | null
  fileSize: number | null

  constructor(
    sessionID: CanonicalUploadSessionID | null,
    confirmedBytes: number,
    completed: boolean,
    checksum: CanonicalHash | null,
    fileSize: number | null
  ) {
    this.sessionID = sessionID
    this.confirmedBytes = confirmedBytes
    this.completed = completed
    this.checksum = checksum
    this.fileSize = fileSize
  }
}

// =============================================================================
// HARNESS
// =============================================================================

interface NodeState {
  node: CanonicalNode
  fileStore: InMemoryCanonicalFileStore
  uploadRuntime: CanonicalResumableUploadRuntime
  metadataRoot: CanonicalRootToken
  audioRoot: CanonicalRootToken
  generatedRoot: CanonicalRootToken
  recordings: Map<string, CanonicalRecordingMetadata>
  artifactsByObjectID: Map<string, CanonicalArtifact[]>
  libraryObjects: CanonicalLibraryObject[]
  libraryTombstones: CanonicalLibraryTombstone[]
}

export class CanonicalRuntimeHarness {
  private iphone: NodeState = {} as NodeState
  private mac: NodeState = {} as NodeState
  private transport: InMemoryCanonicalTransportRuntime = new InMemoryCanonicalTransportRuntime()
  private transportRegistered: boolean = false

  constructor() {
    const iphoneRoots = CanonicalRuntimeHarness.roots('iphone')
    const macRoots = CanonicalRuntimeHarness.roots('mac')
    const iphoneStore = new InMemoryCanonicalFileStore(iphoneRoots.bindings)
    const macStore = new InMemoryCanonicalFileStore(macRoots.bindings)

    const iphoneNode = new CanonicalNode('iphone-01', 'iPhone', null, [
      CanonicalCapability.RECORDING_METADATA, CanonicalCapability.AUDIO_ARTIFACT,
      CanonicalCapability.OBJECT_PROJECTION, CanonicalCapability.CANONICAL_LIBRARY_OBJECTS_V1,
      CanonicalCapability.CANONICAL_FOLDER_OBJECTS_V1, CanonicalCapability.CANONICAL_STUDY_ITEM_OBJECTS_V1,
      CanonicalCapability.CANONICAL_TRANSFER_STATE_V1, CanonicalCapability.CANONICAL_OBJECT_PROJECTION_V1,
      CanonicalCapability.CANONICAL_INVENTORY_BUILDER_V1, CanonicalCapability.CANONICAL_RETIREMENT_READINESS_V1
    ])
    const macNode = new CanonicalNode('mac-01', 'Mac', null, [
      CanonicalCapability.RECORDING_METADATA, CanonicalCapability.AUDIO_ARTIFACT,
      CanonicalCapability.RECEIVE_RECORD, CanonicalCapability.TRANSCRIPT_ARTIFACT,
      CanonicalCapability.NOTE_ARTIFACT, CanonicalCapability.SUMMARY_ARTIFACT,
      CanonicalCapability.OBJECT_PROJECTION, CanonicalCapability.CANONICAL_LIBRARY_OBJECTS_V1,
      CanonicalCapability.CANONICAL_FOLDER_OBJECTS_V1, CanonicalCapability.CANONICAL_STUDY_ITEM_OBJECTS_V1,
      CanonicalCapability.CANONICAL_TRANSFER_STATE_V1, CanonicalCapability.CANONICAL_OBJECT_PROJECTION_V1,
      CanonicalCapability.CANONICAL_INVENTORY_BUILDER_V1, CanonicalCapability.CANONICAL_RETIREMENT_READINESS_V1
    ])

    this.iphone = {
      node: iphoneNode, fileStore: iphoneStore,
      uploadRuntime: new CanonicalResumableUploadRuntime(iphoneStore),
      metadataRoot: iphoneRoots.metadata, audioRoot: iphoneRoots.audio,
      generatedRoot: iphoneRoots.generated, recordings: new Map(),
      artifactsByObjectID: new Map(), libraryObjects: [], libraryTombstones: []
    }
    this.mac = {
      node: macNode, fileStore: macStore,
      uploadRuntime: new CanonicalResumableUploadRuntime(macStore),
      metadataRoot: macRoots.metadata, audioRoot: macRoots.audio,
      generatedRoot: macRoots.generated, recordings: new Map(),
      artifactsByObjectID: new Map(), libraryObjects: [], libraryTombstones: []
    }
  }

  seedRecording(
    role: CanonicalRuntimeHarnessNodeRole,
    objectID: string,
    title: string = 'Lecture',
    modifiedAt: Date = new Date(Date.UTC(1970, 0, 1) + 2000 * 1000),
    audioBytes: Uint8Array | null = null
  ) {
    let state = this.nodeState(role)
    const metadata = new CanonicalRecordingMetadata(
      objectID, title,
      new CanonicalTimestamp(new Date(Date.UTC(1970, 0, 1) + 1000 * 1000)),
      new CanonicalTimestamp(modifiedAt),
      audioBytes ? audioBytes.length : null
    )
    state.recordings.set(metadata.objectID, metadata)
    if (audioBytes && audioBytes.length > 0) {
      const safeComponent = CanonicalRuntimeHarness.safePathComponent(objectID)
      const reference = new CanonicalFileReference(
        state.audioRoot, 'audio/' + safeComponent + '.m4a',
        artifactID(objectID, CanonicalArtifactKind.AUDIO), CanonicalArtifactKind.AUDIO
      )
      const hash = InMemoryCanonicalFileStore.hash(audioBytes, CanonicalFileHashPolicy.SHA256)
      state.fileStore.write(
        new CanonicalFileWriteIntent(reference, audioBytes, CanonicalFilePurpose.ARTIFACT_BYTES,
          hash, audioBytes.length, null, undefined, undefined, CanonicalFileConflictPolicy.IDEMPOTENT_IF_SAME_CONTENT)
      )
      const artifact = CanonicalArtifactFact.audio(
        CanonicalArtifactAvailability.AVAILABLE, hash, audioBytes.length,
        'audio.m4a', reference.logicalPathToken,
        null, null, role === CanonicalRuntimeHarnessNodeRole.IPHONE ? state.node.nodeID : 'iphone-01'
      ).makeArtifact(objectID)
      let artifacts = state.artifactsByObjectID.get(objectID) ?? []
      artifacts = artifacts.filter(a => a.kind !== CanonicalArtifactKind.AUDIO)
      artifacts.push(artifact)
      state.artifactsByObjectID.set(objectID, artifacts)
    }
    this.setNodeState(state, role)
  }

  seedGeneratedArtifact(
    role: CanonicalRuntimeHarnessNodeRole,
    objectID: string,
    kind: CanonicalArtifactKind,
    bytes: Uint8Array,
    logicalPathToken: string | null = null,
    modifiedAt: Date = new Date(Date.UTC(1970, 0, 1) + 3000 * 1000)
  ) {
    let state = this.nodeState(role)
    const safeComponent = CanonicalRuntimeHarness.safePathComponent(objectID)
    const token = logicalPathToken ?? 'generated/' + safeComponent + '/' + kind + '.bin'
    const reference = new CanonicalFileReference(
      state.generatedRoot, token,
      artifactID(objectID, kind), kind
    )
    const hash = InMemoryCanonicalFileStore.hash(bytes, CanonicalFileHashPolicy.SHA256)
    state.fileStore.write(
      new CanonicalFileWriteIntent(reference, bytes, CanonicalFilePurpose.GENERATED_ARTIFACT,
        hash, bytes.length, null, undefined, undefined, CanonicalFileConflictPolicy.IDEMPOTENT_IF_SAME_CONTENT)
    )
    const artifact = makeArtifact(
      objectID, kind, CanonicalArtifactAvailability.AVAILABLE, hash, bytes.length,
      token, new CanonicalTimestamp(modifiedAt), new CanonicalTimestamp(modifiedAt),
      state.node.nodeID, state.node.platform
    )
    let artifacts = state.artifactsByObjectID.get(objectID) ?? []
    artifacts = artifacts.filter(a => a.kind !== kind)
    artifacts.push(artifact)
    state.artifactsByObjectID.set(objectID, artifacts)
    this.setNodeState(state, role)
  }

  manifest(role: CanonicalRuntimeHarnessNodeRole, generatedAt: Date = new Date(Date.UTC(1970, 0, 1) + 4000 * 1000)): CanonicalManifest {
    const state = this.nodeState(role)
    const objects: CanonicalRecordingObject[] = []
    for (const [id, metadata] of state.recordings) {
      const artifacts = state.artifactsByObjectID.get(id) ?? []
      objects.push(new CanonicalRecordingObject(id, state.node.nodeID, metadata, artifacts))
    }
    return CanonicalManifest.make(
      state.node, generatedAt, objects,
      state.libraryObjects, [], [], [], state.libraryTombstones,
      [
        CanonicalCapability.CANONICAL_LIBRARY_OBJECTS_V1, CanonicalCapability.CANONICAL_FOLDER_OBJECTS_V1,
        CanonicalCapability.CANONICAL_STUDY_ITEM_OBJECTS_V1, CanonicalCapability.CANONICAL_INVENTORY_BUILDER_V1,
        CanonicalCapability.CANONICAL_RETIREMENT_READINESS_V1
      ]
    )
  }

  private ensureTransportRegistered() {
    if (!this.transportRegistered) {
      this.transport.register(this.iphone.node, new Set(Object.values(CanonicalTransportRoute)))
      this.transport.register(this.mac.node, new Set(Object.values(CanonicalTransportRoute)))
      this.transportRegistered = true
    }
  }

  private nodeState(role: CanonicalRuntimeHarnessNodeRole): NodeState {
    switch (role) {
      case CanonicalRuntimeHarnessNodeRole.IPHONE:
        return this.iphone
      case CanonicalRuntimeHarnessNodeRole.MAC:
        return this.mac
    }
  }

  private setNodeState(state: NodeState, role: CanonicalRuntimeHarnessNodeRole) {
    switch (role) {
      case CanonicalRuntimeHarnessNodeRole.IPHONE:
        this.iphone = state
        break
      case CanonicalRuntimeHarnessNodeRole.MAC:
        this.mac = state
        break
    }
  }

  private static roots(prefix: string): {
    metadata: CanonicalRootToken,
    audio: CanonicalRootToken,
    generated: CanonicalRootToken,
    bindings: Map<CanonicalRootToken, string>
  } {
    const metadata = new CanonicalRootToken(prefix + '-metadata-root')
    const audio = new CanonicalRootToken(prefix + '-audio-root')
    const generated = new CanonicalRootToken(prefix + '-generated-root')
    const bindings = new Map<CanonicalRootToken, string>()
    bindings.set(metadata, prefix + '/metadata')
    bindings.set(audio, prefix + '/audio')
    bindings.set(generated, prefix + '/generated')
    return { metadata, audio, generated, bindings }
  }

  static safePathComponent(value: string): string {
    let component = ''
    for (let i = 0; i < value.length; i++) {
      const ch = value.charAt(i)
      if (/[a-zA-Z0-9\-_]/.test(ch)) {
        component += ch
      } else {
        component += '-'
      }
    }
    component = component.replace(/-+$/g, '')
    if (component.length === 0) {
      component = CanonicalHash.sha256OfString(value).value.substring(0, 12)
    }
    return component
  }
}

// =============================================================================
// FORWARD REFERENCES (stubs for types from other files)
// =============================================================================

export class CanonicalSyncPlan {
  uploadRecordingMetadata: CanonicalSyncPlanObject[] = []
  downloadRecordingMetadata: CanonicalSyncPlanObject[] = []
  uploadAudioArtifact: CanonicalSyncPlanObject[] = []
  downloadGeneratedArtifact: CanonicalSyncPlanObject[] = []
  conflictRecordingMetadata: CanonicalSyncPlanObject[] = []
  conflictAudioArtifact: CanonicalSyncPlanObject[] = []
  conflictGeneratedArtifact: CanonicalSyncPlanObject[] = []
}

export class CanonicalSyncPlanObject {
  objectID: string = ''
  artifactID: string | null = null
  kind: string | null = null
  reason: string = ''
}

export class CanonicalApplyPlan {
  actions: CanonicalApplyAction[] = []
  tombstones: CanonicalApplyAction[] = []
  conflicts: CanonicalApplyAction[] = []
}

export class CanonicalApplyAction {
  actionID: string = ''
  kind: string = ''
  target: CanonicalApplyTarget = new CanonicalApplyTarget()
  bridgeHint: string | null = null
  reason: string = ''
}

export class CanonicalApplyTarget {
  objectID: string = ''
  artifactID: string | null = null
  artifactKind: string | null = null
}

export class CanonicalLibrarySyncPlan {
  actions: CanonicalLibrarySyncAction[] = []
  tombstones: CanonicalLibraryTombstoneRef[] = []
  conflicts: CanonicalLibraryConflictRef[] = []
  applyActions: CanonicalApplyAction[] = []
}

export class CanonicalLibrarySyncAction {
  kind: string = ''
  objectID: string = ''
  objectKind: string = ''
  reason: string = ''
}

export class CanonicalLibraryTombstoneRef {
  objectID: { rawValue: string } = { rawValue: '' }
}

export class CanonicalLibraryConflictRef {
  conflictID: string = ''
  objectID: { rawValue: string } = { rawValue: '' }
}

export class CanonicalApplyExecutionReport {
  records: CanonicalApplyExecutionRecord[] = []
}

export class CanonicalApplyExecutionRecord {
  kind: string = ''
  status: string = ''
  target: CanonicalApplyTarget = new CanonicalApplyTarget()
}

export class CanonicalLibraryObject {
  objectID = { rawValue: '' }
  kind: string = ''
  folder: CanonicalFolderObject | null = null
  studyItem: CanonicalStudyItemObject | null = null
  standaloneNote: CanonicalStandaloneNoteObject | null = null
}

export class CanonicalFolderObject {
  folderID = { rawValue: '' }
  metadata = { name: '', isDeleted: false, deletedAt: null }
}

export class CanonicalStudyItemObject {
  itemID = { rawValue: '' }
  metadata = { title: '', isDeleted: false, deletedAt: null }
}

export class CanonicalStandaloneNoteObject {
  noteID = { rawValue: '' }
  studyItem: CanonicalStudyItemObject | null = null
}

export class CanonicalLibraryObjectID {
  rawValue: string = ''
  constructor(rawValue: string) { this.rawValue = rawValue }
}

export class CanonicalLibraryTombstone {
  tombstoneID: string = ''
  objectID: CanonicalLibraryObjectID = new CanonicalLibraryObjectID('')
  objectKind: string = ''
  deletedAt: CanonicalTimestamp | null = null
  sourceNodeID: string | null = null
  reason: string = ''
}

export class CanonicalLibraryProjection {
  recordings: CanonicalRecordingProjection[] = []
  folders: CanonicalFolderProjection[] = []
  studyItems: CanonicalStudyItemProjection[] = []
  builtAt: CanonicalTimestamp = new CanonicalTimestamp(new Date())
}

export class CanonicalRecordingProjection {
  objectID: string = ''
  title: string = ''
  displayStates: string[] = []
}

export class CanonicalFolderProjection {
  folderID: CanonicalLibraryObjectID = new CanonicalLibraryObjectID('')
  title: string = ''
  displayState: string = ''
}

export class CanonicalStudyItemProjection {
  itemID: CanonicalLibraryObjectID = new CanonicalLibraryObjectID('')
  title: string = ''
  displayState: string = ''
}

export class CanonicalRuntimeReadinessReport {
  states: string[] = []

  status(domain: string): string {
    return 'offlineRuntimeComplete'
  }
}

export class CanonicalRuntimeReadinessEvaluator {
  evaluate(
    evidence: CanonicalRuntimeReadinessEvidence,
    generatedAt: Date = new Date()
  ): CanonicalRuntimeReadinessReport {
    return new CanonicalRuntimeReadinessReport()
  }
}

export class CanonicalRuntimeReadinessEvidence {
  fileRootBinding: boolean
  fileHashVerification: boolean
  transportRouteValidation: boolean
  uploadResumableState: boolean
  applyExecutor: boolean
  conflictResolver: boolean
  twoNodeHarness: boolean
  productionStillLegacyOwned: boolean

  constructor(
    fileRootBinding: boolean, fileHashVerification: boolean,
    transportRouteValidation: boolean, uploadResumableState: boolean,
    applyExecutor: boolean, conflictResolver: boolean,
    twoNodeHarness: boolean, productionStillLegacyOwned: boolean
  ) {
    this.fileRootBinding = fileRootBinding
    this.fileHashVerification = fileHashVerification
    this.transportRouteValidation = transportRouteValidation
    this.uploadResumableState = uploadResumableState
    this.applyExecutor = applyExecutor
    this.conflictResolver = conflictResolver
    this.twoNodeHarness = twoNodeHarness
    this.productionStillLegacyOwned = productionStillLegacyOwned
  }
}
