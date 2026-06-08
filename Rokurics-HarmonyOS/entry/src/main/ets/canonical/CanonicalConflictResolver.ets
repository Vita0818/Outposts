/**
 * CanonicalConflictResolver — conflict resolution decisions.
 * Ported from Rokurics-Apple (CanonicalConflictResolver.swift).
 * Evaluates recording and library conflicts into resolution decisions.
 */

import {
  CanonicalLibraryObjectID, CanonicalLibraryConflict, CanonicalLibraryConflictKind
} from './CanonicalCore'

// ── Conflict Resolver Action ──

export enum CanonicalConflictResolverAction {
  RECORD_ONLY = 'recordOnly',
  KEEP_BOTH_NO_OVERWRITE = 'keepBothNoOverwrite',
  REQUIRE_MANUAL_REVIEW = 'requireManualReview',
  TOMBSTONE_MANUAL_REVIEW = 'tombstoneManualReview'
}

// ── Conflict Resolution State ──

export enum CanonicalConflictResolutionState {
  UNRESOLVED = 'unresolved',
  RESOLVED = 'resolved',
  IGNORED = 'ignored'
}

// ── Apply Target (minimal standalone) ──

export class CanonicalApplyTarget {
  objectID: string
  artifactID: string | null
  artifactKind: string | null

  constructor(objectID: string, artifactID: string | null = null, artifactKind: string | null = null) {
    this.objectID = objectID.trim()
    this.artifactID = artifactID
    this.artifactKind = artifactKind
  }
}

// ── Conflict Record (minimal standalone) ──

export enum CanonicalConflictKind {
  RECORDING_METADATA_CONCURRENT_EDIT = 'recordingMetadataConcurrentEdit',
  RECORDING_AUDIO_CONTENT_MISMATCH = 'recordingAudioContentMismatch',
  GENERATED_ARTIFACT_CONTENT_MISMATCH = 'generatedArtifactContentMismatch',
  ACTIVE_VS_TOMBSTONE = 'activeVsTombstone'
}

export enum CanonicalConflictResolutionPolicy {
  MANUAL_REVIEW = 'manualReview',
  KEEP_BOTH_NO_OVERWRITE = 'keepBothNoOverwrite',
  TOMBSTONE_REQUIRES_MANUAL_REVIEW = 'tombstoneRequiresManualReview'
}

export class CanonicalConflictRecord {
  conflictID: string
  kind: CanonicalConflictKind
  resolutionPolicy: CanonicalConflictResolutionPolicy
  target: CanonicalApplyTarget
  resolutionState: CanonicalConflictResolutionState

  constructor(
    kind: CanonicalConflictKind,
    target: CanonicalApplyTarget,
    resolutionPolicy: CanonicalConflictResolutionPolicy,
    resolutionState: CanonicalConflictResolutionState = CanonicalConflictResolutionState.UNRESOLVED
  ) {
    this.kind = kind
    this.target = target
    this.resolutionPolicy = resolutionPolicy
    this.resolutionState = resolutionState
    this.conflictID = ['conflict', kind, target.objectID, target.artifactKind ?? 'metadata',
      target.artifactID ?? 'object'].join('|')
  }
}

// ── Conflict Resolution Decision ──

export class CanonicalConflictResolutionDecision {
  conflictID: string
  target: CanonicalApplyTarget
  action: CanonicalConflictResolverAction
  state: CanonicalConflictResolutionState
  detail: string | null

  constructor(
    conflictID: string,
    target: CanonicalApplyTarget,
    action: CanonicalConflictResolverAction,
    state: CanonicalConflictResolutionState,
    detail: string | null = null
  ) {
    this.conflictID = conflictID
    this.target = target
    this.action = action
    this.state = state
    this.detail = detail
  }

  get id(): string {
    return this.conflictID
  }
}

// ── Conflict Resolver Report ──

export class CanonicalConflictResolverReport {
  decisions: CanonicalConflictResolutionDecision[]
  unresolvedCount: number
  manualReviewCount: number
  keepBothCount: number

  constructor(
    decisions: CanonicalConflictResolutionDecision[],
    unresolvedCount: number,
    manualReviewCount: number,
    keepBothCount: number
  ) {
    this.decisions = decisions
    this.unresolvedCount = unresolvedCount
    this.manualReviewCount = manualReviewCount
    this.keepBothCount = keepBothCount
  }
}

// ── Conflict Resolver ──

export class CanonicalConflictResolver {
  resolve(
    conflicts: CanonicalConflictRecord[],
    libraryConflicts: CanonicalLibraryConflict[] = []
  ): CanonicalConflictResolverReport {
    const recordingDecisions = conflicts.map(c => this.recordingDecision(c))
    const libraryDecisions = libraryConflicts.map(c => this.libraryDecision(c))
    const decisions = (recordingDecisions.concat(libraryDecisions))
      .sort((a, b) => a.conflictID.localeCompare(b.conflictID))
    return new CanonicalConflictResolverReport(
      decisions,
      decisions.filter(d => d.state === CanonicalConflictResolutionState.UNRESOLVED).length,
      decisions.filter(d => d.action === CanonicalConflictResolverAction.REQUIRE_MANUAL_REVIEW ||
        d.action === CanonicalConflictResolverAction.TOMBSTONE_MANUAL_REVIEW).length,
      decisions.filter(d => d.action === CanonicalConflictResolverAction.KEEP_BOTH_NO_OVERWRITE).length
    )
  }

  private recordingDecision(conflict: CanonicalConflictRecord): CanonicalConflictResolutionDecision {
    let action: CanonicalConflictResolverAction
    switch (conflict.resolutionPolicy) {
      case CanonicalConflictResolutionPolicy.MANUAL_REVIEW:
        action = CanonicalConflictResolverAction.REQUIRE_MANUAL_REVIEW
        break
      case CanonicalConflictResolutionPolicy.KEEP_BOTH_NO_OVERWRITE:
        action = CanonicalConflictResolverAction.KEEP_BOTH_NO_OVERWRITE
        break
      case CanonicalConflictResolutionPolicy.TOMBSTONE_REQUIRES_MANUAL_REVIEW:
        action = CanonicalConflictResolverAction.TOMBSTONE_MANUAL_REVIEW
        break
    }
    return new CanonicalConflictResolutionDecision(
      conflict.conflictID,
      conflict.target,
      action,
      CanonicalConflictResolutionState.UNRESOLVED,
      conflict.kind
    )
  }

  private libraryDecision(conflict: CanonicalLibraryConflict): CanonicalConflictResolutionDecision {
    const action = conflict.kind === CanonicalLibraryConflictKind.ACTIVE_VS_TOMBSTONE
      ? CanonicalConflictResolverAction.TOMBSTONE_MANUAL_REVIEW
      : CanonicalConflictResolverAction.REQUIRE_MANUAL_REVIEW
    return new CanonicalConflictResolutionDecision(
      conflict.conflictID,
      new CanonicalApplyTarget(conflict.objectID.rawValue),
      action,
      CanonicalConflictResolutionState.UNRESOLVED,
      conflict.kind
    )
  }
}
