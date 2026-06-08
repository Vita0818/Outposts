/**
 * CanonicalObjectProjection — display-ready projections for UI consumption.
 * Ported from Rokurics-Apple (CanonicalObjectProjection.swift).
 * Translates canonical facts into display states without driving sync logic.
 */

import {
  CanonicalTimestamp, CanonicalManifest,
  CanonicalRecordingObject, CanonicalArtifactKind,
  CanonicalLibraryObject, CanonicalObjectKind,
  CanonicalFolderObject, CanonicalStudyItemObject,
  CanonicalLibraryObjectID
} from './CanonicalCore'
import { CanonicalTransferJob, CanonicalTransferPhase, CanonicalTransferProjection } from './CanonicalTransferStateMachine'

// ── Canonical Display State ──

export enum CanonicalDisplayState {
  LOCAL_ONLY = 'localOnly',
  WAITING_FOR_AUDIO = 'waitingForAudio',
  UPLOADING_AUDIO = 'uploadingAudio',
  AUDIO_AVAILABLE = 'audioAvailable',
  METADATA_SYNCED = 'metadataSynced',
  TRANSCRIPT_AVAILABLE = 'transcriptAvailable',
  NOTE_AVAILABLE = 'noteAvailable',
  SUMMARY_AVAILABLE = 'summaryAvailable',
  PROCESSING = 'processing',
  FAILED = 'failed',
  RETRY_PENDING = 'retryPending',
  CONFLICT = 'conflict',
  DELETED = 'deleted',
  TOMBSTONED = 'tombstoned',
  AVAILABLE = 'available',
  SYNCING = 'syncing',
  UNSUPPORTED = 'unsupported',
  UNKNOWN = 'unknown'
}

// ── Canonical Action Availability ──

export class CanonicalActionAvailability {
  canUploadAudio: boolean
  canRequestGeneratedArtifact: boolean
  canApplyMetadata: boolean
  canResolveConflict: boolean

  constructor(
    canUploadAudio: boolean,
    canRequestGeneratedArtifact: boolean,
    canApplyMetadata: boolean,
    canResolveConflict: boolean
  ) {
    this.canUploadAudio = canUploadAudio
    this.canRequestGeneratedArtifact = canRequestGeneratedArtifact
    this.canApplyMetadata = canApplyMetadata
    this.canResolveConflict = canResolveConflict
  }

  static readonly READ_ONLY = new CanonicalActionAvailability(false, false, false, false)
}

// ── Projection Types ──

export class CanonicalRecordingProjection {
  objectID: string
  title: string
  displayStates: CanonicalDisplayState[]
  actionAvailability: CanonicalActionAvailability
  metadataHashPrefix: string | null
  audioHashPrefix: string | null

  constructor(
    objectID: string,
    title: string,
    displayStates: CanonicalDisplayState[],
    actionAvailability: CanonicalActionAvailability,
    metadataHashPrefix: string | null,
    audioHashPrefix: string | null
  ) {
    this.objectID = objectID
    this.title = title
    this.displayStates = displayStates
    this.actionAvailability = actionAvailability
    this.metadataHashPrefix = metadataHashPrefix
    this.audioHashPrefix = audioHashPrefix
  }

  get id(): string {
    return this.objectID
  }
}

export class CanonicalFolderProjection {
  folderID: CanonicalLibraryObjectID
  title: string
  displayState: CanonicalDisplayState
  actionAvailability: CanonicalActionAvailability

  constructor(
    folderID: CanonicalLibraryObjectID,
    title: string,
    displayState: CanonicalDisplayState,
    actionAvailability: CanonicalActionAvailability
  ) {
    this.folderID = folderID
    this.title = title
    this.displayState = displayState
    this.actionAvailability = actionAvailability
  }

  get id(): string {
    return this.folderID.rawValue
  }
}

export class CanonicalStudyItemProjection {
  itemID: CanonicalLibraryObjectID
  title: string
  displayState: CanonicalDisplayState
  actionAvailability: CanonicalActionAvailability

  constructor(
    itemID: CanonicalLibraryObjectID,
    title: string,
    displayState: CanonicalDisplayState,
    actionAvailability: CanonicalActionAvailability
  ) {
    this.itemID = itemID
    this.title = title
    this.displayState = displayState
    this.actionAvailability = actionAvailability
  }

  get id(): string {
    return this.itemID.rawValue
  }
}

export class CanonicalLibraryProjection {
  recordings: CanonicalRecordingProjection[]
  folders: CanonicalFolderProjection[]
  studyItems: CanonicalStudyItemProjection[]
  builtAt: CanonicalTimestamp

  constructor(
    recordings: CanonicalRecordingProjection[],
    folders: CanonicalFolderProjection[],
    studyItems: CanonicalStudyItemProjection[],
    builtAt: CanonicalTimestamp
  ) {
    this.recordings = recordings
    this.folders = folders
    this.studyItems = studyItems
    this.builtAt = builtAt
  }
}

// ── Object Projection Builder ──

// Forward type for apply plan conflicts
interface ApplyPlanConflictLike {
  target: { objectID: string }
}

interface LibraryPlanConflictLike {
  objectID: { rawValue: string }
}

export class CanonicalObjectProjectionBuilder {
  static build(
    manifest: CanonicalManifest,
    applyPlan: { conflicts: ApplyPlanConflictLike[] } | null = null,
    libraryPlan: { conflicts: LibraryPlanConflictLike[] } | null = null,
    transferProjection: CanonicalTransferProjection | null = null,
    builtAt: Date = new Date()
  ): CanonicalLibraryProjection {
    const conflictIDs = new Set((applyPlan?.conflicts ?? []).map(c => c.target.objectID))
    const libraryConflictIDs = new Set((libraryPlan?.conflicts ?? []).map(c => c.objectID.rawValue))
    const transferByObject: Record<string, CanonicalTransferJob[]> = {}
    for (const job of (transferProjection?.jobs ?? [])) {
      const key = job.objectID
      if (!transferByObject[key]) {
        transferByObject[key] = []
      }
      transferByObject[key].push(job)
    }

    const recordings = manifest.objects.map(obj => {
      return CanonicalObjectProjectionBuilder.recordingProjection(
        obj, conflictIDs, transferByObject[obj.objectID] ?? []
      )
    }).sort((a, b) => a.objectID.localeCompare(b.objectID))

    const folders = manifest.libraryObjects
      .filter(o => o.kind === CanonicalObjectKind.FOLDER && o.folder !== null)
      .map(o => CanonicalObjectProjectionBuilder.folderProjection(
        o.folder!, libraryConflictIDs.has(o.objectID.rawValue)
      ))
      .sort((a, b) => a.folderID.rawValue.localeCompare(b.folderID.rawValue))

    const studyItems = manifest.libraryObjects
      .filter(o => (o.kind === CanonicalObjectKind.STANDALONE_STUDY_ITEM ||
        o.kind === CanonicalObjectKind.STANDALONE_NOTE ||
        o.kind === CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM) &&
        (o.studyItem || o.standaloneNote?.studyItem))
      .map(o => CanonicalObjectProjectionBuilder.studyItemProjection(
        (o.studyItem ?? o.standaloneNote!.studyItem)!,
        libraryConflictIDs.has(o.objectID.rawValue)
      ))
      .sort((a, b) => a.itemID.rawValue.localeCompare(b.itemID.rawValue))

    return new CanonicalLibraryProjection(
      recordings,
      folders,
      studyItems,
      new CanonicalTimestamp(builtAt)
    )
  }

  private static recordingProjection(
    obj: CanonicalRecordingObject,
    conflictIDs: Set<string>,
    transferJobs: CanonicalTransferJob[]
  ): CanonicalRecordingProjection {
    const states: CanonicalDisplayState[] = []

    if (obj.metadata.isDeleted) {
      states.push(CanonicalDisplayState.DELETED)
    }
    if (conflictIDs.has(obj.objectID)) {
      states.push(CanonicalDisplayState.CONFLICT)
    }
    if (transferJobs.some(j =>
      j.phase === CanonicalTransferPhase.IN_FLIGHT ||
      j.phase === CanonicalTransferPhase.QUEUED ||
      j.phase === CanonicalTransferPhase.PLANNED)) {
      states.push(CanonicalDisplayState.UPLOADING_AUDIO)
    }
    if (transferJobs.some(j => j.phase === CanonicalTransferPhase.FAILED_RETRYABLE)) {
      states.push(CanonicalDisplayState.RETRY_PENDING)
    }
    if (transferJobs.some(j => j.phase === CanonicalTransferPhase.FAILED_FATAL)) {
      states.push(CanonicalDisplayState.FAILED)
    }
    if (obj.audioAvailable) {
      states.push(CanonicalDisplayState.AUDIO_AVAILABLE)
    } else if (!obj.metadata.isDeleted) {
      states.push(CanonicalDisplayState.WAITING_FOR_AUDIO)
    }
    if (CanonicalObjectProjectionBuilder.generatedAvailable(obj, CanonicalArtifactKind.TRANSCRIPT_MARKDOWN) ||
      CanonicalObjectProjectionBuilder.generatedAvailable(obj, CanonicalArtifactKind.TRANSCRIPT_JSON)) {
      states.push(CanonicalDisplayState.TRANSCRIPT_AVAILABLE)
    }
    if (CanonicalObjectProjectionBuilder.generatedAvailable(obj, CanonicalArtifactKind.NOTE_MARKDOWN) ||
      CanonicalObjectProjectionBuilder.generatedAvailable(obj, CanonicalArtifactKind.NOTE_JSON)) {
      states.push(CanonicalDisplayState.NOTE_AVAILABLE)
    }
    if (CanonicalObjectProjectionBuilder.generatedAvailable(obj, CanonicalArtifactKind.SUMMARY_JSON)) {
      states.push(CanonicalDisplayState.SUMMARY_AVAILABLE)
    }
    if (states.length === 0) {
      states.push(CanonicalDisplayState.METADATA_SYNCED)
    }

    return new CanonicalRecordingProjection(
      obj.objectID,
      obj.metadata.title,
      CanonicalObjectProjectionBuilder.uniqueStates(states),
      CanonicalActionAvailability.READ_ONLY,
      obj.metadataHash.value.substring(0, 12),
      obj.audioArtifact?.contentHash?.value.substring(0, 12) ?? null
    )
  }

  private static folderProjection(
    folder: CanonicalFolderObject,
    hasConflict: boolean
  ): CanonicalFolderProjection {
    return new CanonicalFolderProjection(
      folder.folderID,
      folder.metadata.name,
      CanonicalObjectProjectionBuilder.displayStateFor(folder.metadata.isDeleted, hasConflict),
      CanonicalActionAvailability.READ_ONLY
    )
  }

  private static studyItemProjection(
    item: CanonicalStudyItemObject,
    hasConflict: boolean
  ): CanonicalStudyItemProjection {
    return new CanonicalStudyItemProjection(
      item.itemID,
      item.metadata.title,
      CanonicalObjectProjectionBuilder.displayStateFor(item.metadata.isDeleted, hasConflict),
      CanonicalActionAvailability.READ_ONLY
    )
  }

  private static displayStateFor(isDeleted: boolean, hasConflict: boolean): CanonicalDisplayState {
    if (isDeleted) return CanonicalDisplayState.TOMBSTONED
    if (hasConflict) return CanonicalDisplayState.CONFLICT
    return CanonicalDisplayState.AVAILABLE
  }

  private static generatedAvailable(obj: CanonicalRecordingObject, kind: CanonicalArtifactKind): boolean {
    return obj.artifacts.some(a => a.kind === kind && a.provesCanonicalGeneratedArtifactAvailability)
  }

  private static uniqueStates(states: CanonicalDisplayState[]): CanonicalDisplayState[] {
    const seen = new Set<string>()
    return states.filter(s => {
      if (seen.has(s)) return false
      seen.add(s)
      return true
    })
  }
}
