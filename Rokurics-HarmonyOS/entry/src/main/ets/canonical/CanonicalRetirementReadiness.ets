/**
 * CanonicalRetirementReadiness — legacy retirement readiness evaluator.
 * Ported from Rokurics-Apple (CanonicalRetirementReadiness.swift).
 * Tracks per-domain retirement status and blockers for legacy path removal.
 */

import {
  CanonicalTimestamp, CanonicalManifest, CanonicalLibraryTombstone
} from './CanonicalCore'
import { CanonicalTransferProjection } from './CanonicalTransferStateMachine'

// ── Retirement Domain ──

export enum CanonicalRetirementDomain {
  RECORDING_METADATA = 'recordingMetadata',
  RECORDING_AUDIO = 'recordingAudio',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  FOLDERS = 'folders',
  STUDY_ITEMS = 'studyItems',
  TOMBSTONES = 'tombstones',
  CONFLICTS = 'conflicts',
  APPLY = 'apply',
  TRANSFER_STATE = 'transferState',
  OBJECT_PROJECTION = 'objectProjection',
  INVENTORY = 'inventory',
  TRANSPORT = 'transport',
  UPLOAD_RUNTIME = 'uploadRuntime',
  PHYSICAL_STORAGE = 'physicalStorage'
}

// ── Retirement Status ──

export enum CanonicalRetirementStatus {
  NOT_STARTED = 'notStarted',
  SHADOW_ONLY = 'shadowOnly',
  PLANNING_ONLY = 'planningOnly',
  APPLY_BRIDGED = 'applyBridged',
  SEMANTICS_COMPLETE = 'semanticsComplete',
  RUNTIME_COMPLETE = 'runtimeComplete',
  READY_TO_RETIRE_LEGACY = 'readyToRetireLegacy',
  BLOCKED = 'blocked'
}

// ── Retirement Blocker Kind ──

export enum CanonicalRetirementBlockerKind {
  MISSING_CANONICAL_MANIFEST = 'missingCanonicalManifest',
  UNSUPPORTED_OBJECT_KINDS = 'unsupportedObjectKinds',
  FALLBACK_USED = 'fallbackUsed',
  CONFLICTS_UNRESOLVED = 'conflictsUnresolved',
  APPLY_BRIDGE_MISSING = 'applyBridgeMissing',
  TRANSFER_STATE_UNMAPPED = 'transferStateUnmapped',
  UI_STILL_READS_LEGACY_STATUS = 'uiStillReadsLegacyStatus',
  ROUTE_STILL_LEGACY = 'routeStillLegacy',
  PHYSICAL_STORE_STILL_LEGACY = 'physicalStoreStillLegacy'
}

// ── Retirement Blocker ──

export class CanonicalRetirementBlocker {
  domain: CanonicalRetirementDomain
  kind: CanonicalRetirementBlockerKind
  detail: string | null

  constructor(
    domain: CanonicalRetirementDomain,
    kind: CanonicalRetirementBlockerKind,
    detail: string | null = null
  ) {
    this.domain = domain
    this.kind = kind
    this.detail = detail
  }

  get id(): string {
    return [this.domain, this.kind, this.detail ?? ''].join('|')
  }
}

// ── Retirement Readiness Report ──

export class CanonicalRetirementReadinessReport {
  generatedAt: CanonicalTimestamp
  statuses: Record<string, CanonicalRetirementStatus>
  blockers: CanonicalRetirementBlocker[]

  constructor(
    generatedAt: CanonicalTimestamp,
    statuses: Record<string, CanonicalRetirementStatus>,
    blockers: CanonicalRetirementBlocker[]
  ) {
    this.generatedAt = generatedAt
    this.statuses = statuses
    this.blockers = blockers
  }

  statusFor(domain: CanonicalRetirementDomain): CanonicalRetirementStatus {
    return this.statuses[domain] ?? CanonicalRetirementStatus.NOT_STARTED
  }
}

// ── Forward declarations for types from downstream files ──
// These would be imported when CanonicalApplyPlan and CanonicalLibrarySyncPlan are ported.

interface ApplyPlanLike {
  conflicts: { target: { objectID: string } }[]
  tombstones: { tombstoneID: string }[]
}

interface LibraryPlanLike {
  conflicts: { objectID: { rawValue: string } }[]
  fallbackRequiredObjectIDs: { rawValue: string }[]
  tombstones: { tombstoneID: string }[]
}

interface InventoryCoverageLike {
  unsupportedLegacyObjectCount: number
  fallbackRequiredCount: number
}

// ── Retirement Readiness Evaluator ──

export class CanonicalRetirementReadinessEvaluator {
  evaluate(
    manifest: CanonicalManifest | null,
    libraryPlan: LibraryPlanLike | null,
    applyPlan: ApplyPlanLike | null,
    transferProjection: CanonicalTransferProjection | null,
    inventoryCoverage: InventoryCoverageLike | null,
    fallbackUsed: boolean,
    generatedAt: Date = new Date()
  ): CanonicalRetirementReadinessReport {
    const statuses: Record<string, CanonicalRetirementStatus> = {}
    const allDomains = Object.values(CanonicalRetirementDomain)
    for (const d of allDomains) {
      statuses[d] = CanonicalRetirementStatus.NOT_STARTED
    }
    const blockers: CanonicalRetirementBlocker[] = []

    if (!manifest) {
      this.addBlocker(
        CanonicalRetirementDomain.INVENTORY,
        CanonicalRetirementBlockerKind.MISSING_CANONICAL_MANIFEST,
        'manifestMissing',
        statuses,
        blockers
      )
    } else {
      statuses[CanonicalRetirementDomain.RECORDING_METADATA] = !applyPlan
        ? CanonicalRetirementStatus.PLANNING_ONLY
        : CanonicalRetirementStatus.APPLY_BRIDGED
      statuses[CanonicalRetirementDomain.RECORDING_AUDIO] = CanonicalRetirementStatus.PLANNING_ONLY
      statuses[CanonicalRetirementDomain.GENERATED_ARTIFACTS] = !applyPlan
        ? CanonicalRetirementStatus.PLANNING_ONLY
        : CanonicalRetirementStatus.APPLY_BRIDGED
      statuses[CanonicalRetirementDomain.FOLDERS] = !libraryPlan
        ? CanonicalRetirementStatus.SHADOW_ONLY
        : CanonicalRetirementStatus.APPLY_BRIDGED
      statuses[CanonicalRetirementDomain.STUDY_ITEMS] = !libraryPlan
        ? CanonicalRetirementStatus.SHADOW_ONLY
        : CanonicalRetirementStatus.APPLY_BRIDGED
      statuses[CanonicalRetirementDomain.TOMBSTONES] =
        ((applyPlan?.tombstones?.length ?? 0) > 0 || (libraryPlan?.tombstones?.length ?? 0) > 0)
          ? CanonicalRetirementStatus.APPLY_BRIDGED
          : CanonicalRetirementStatus.PLANNING_ONLY
      statuses[CanonicalRetirementDomain.CONFLICTS] =
        ((applyPlan?.conflicts?.length ?? 0) > 0 || (libraryPlan?.conflicts?.length ?? 0) > 0)
          ? CanonicalRetirementStatus.BLOCKED
          : CanonicalRetirementStatus.SEMANTICS_COMPLETE
      statuses[CanonicalRetirementDomain.APPLY] = !applyPlan
        ? CanonicalRetirementStatus.PLANNING_ONLY
        : CanonicalRetirementStatus.APPLY_BRIDGED
      statuses[CanonicalRetirementDomain.TRANSFER_STATE] = !transferProjection
        ? CanonicalRetirementStatus.PLANNING_ONLY
        : CanonicalRetirementStatus.SEMANTICS_COMPLETE
      statuses[CanonicalRetirementDomain.OBJECT_PROJECTION] = CanonicalRetirementStatus.SEMANTICS_COMPLETE
      statuses[CanonicalRetirementDomain.INVENTORY] = !inventoryCoverage
        ? CanonicalRetirementStatus.SHADOW_ONLY
        : CanonicalRetirementStatus.SEMANTICS_COMPLETE
      statuses[CanonicalRetirementDomain.TRANSPORT] = CanonicalRetirementStatus.BLOCKED
      statuses[CanonicalRetirementDomain.UPLOAD_RUNTIME] = CanonicalRetirementStatus.BLOCKED
      statuses[CanonicalRetirementDomain.PHYSICAL_STORAGE] = CanonicalRetirementStatus.BLOCKED
    }

    if (fallbackUsed ||
      (libraryPlan?.fallbackRequiredObjectIDs?.length ?? 0) > 0 ||
      (inventoryCoverage?.fallbackRequiredCount ?? 0) > 0) {
      const domains = [
        CanonicalRetirementDomain.FOLDERS,
        CanonicalRetirementDomain.STUDY_ITEMS,
        CanonicalRetirementDomain.INVENTORY,
        CanonicalRetirementDomain.APPLY
      ]
      for (const domain of domains) {
        this.addBlocker(domain, CanonicalRetirementBlockerKind.FALLBACK_USED, 'legacyFallbackPreserved', statuses, blockers)
      }
    }

    if ((inventoryCoverage?.unsupportedLegacyObjectCount ?? 0) > 0) {
      this.addBlocker(
        CanonicalRetirementDomain.INVENTORY,
        CanonicalRetirementBlockerKind.UNSUPPORTED_OBJECT_KINDS,
        'unsupported=' + (inventoryCoverage?.unsupportedLegacyObjectCount ?? 0),
        statuses,
        blockers
      )
    }

    if ((applyPlan?.conflicts?.length ?? 0) > 0 || (libraryPlan?.conflicts?.length ?? 0) > 0) {
      this.addBlocker(
        CanonicalRetirementDomain.CONFLICTS,
        CanonicalRetirementBlockerKind.CONFLICTS_UNRESOLVED,
        'manualReviewRequired',
        statuses,
        blockers
      )
    }

    if (!transferProjection) {
      this.addBlocker(
        CanonicalRetirementDomain.TRANSFER_STATE,
        CanonicalRetirementBlockerKind.TRANSFER_STATE_UNMAPPED,
        'projectionMissing',
        statuses,
        blockers
      )
    }

    this.addBlocker(
      CanonicalRetirementDomain.OBJECT_PROJECTION,
      CanonicalRetirementBlockerKind.UI_STILL_READS_LEGACY_STATUS,
      'objectProjectionNotUIDriving',
      statuses,
      blockers
    )
    this.addBlocker(
      CanonicalRetirementDomain.TRANSPORT,
      CanonicalRetirementBlockerKind.ROUTE_STILL_LEGACY,
      'signedHTTPRoutesStillLegacy',
      statuses,
      blockers
    )
    this.addBlocker(
      CanonicalRetirementDomain.UPLOAD_RUNTIME,
      CanonicalRetirementBlockerKind.ROUTE_STILL_LEGACY,
      'RecordingUploadCoordinatorStillRuntime',
      statuses,
      blockers
    )
    this.addBlocker(
      CanonicalRetirementDomain.PHYSICAL_STORAGE,
      CanonicalRetirementBlockerKind.PHYSICAL_STORE_STILL_LEGACY,
      'legacyStoresStillOwnFiles',
      statuses,
      blockers
    )

    return new CanonicalRetirementReadinessReport(
      new CanonicalTimestamp(generatedAt),
      statuses,
      blockers
    )
  }

  private addBlocker(
    domain: CanonicalRetirementDomain,
    kind: CanonicalRetirementBlockerKind,
    detail: string,
    statuses: Record<string, CanonicalRetirementStatus>,
    blockers: CanonicalRetirementBlocker[]
  ): void {
    statuses[domain] = CanonicalRetirementStatus.BLOCKED
    blockers.push(new CanonicalRetirementBlocker(domain, kind, detail))
  }
}
