/**
 * CanonicalGeneratedArtifactReadProjection — read-side projection for generated artifacts.
 * Ported from Rokurics-Apple (CanonicalGeneratedArtifactReadProjection.swift).
 * Builds snapshots, compares legacy vs canonical, and evaluates read cutover gates.
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalNode, CanonicalRecordingObject, CanonicalArtifact,
  CanonicalArtifactKind, CanonicalArtifactAvailability, CanonicalArtifactProducer, CanonicalManifest,
  hashPrefix, generatedArtifactKinds, safeLogicalPathToken
} from './CanonicalCore'
import {
  CanonicalSyncPlanTrigger
} from './CanonicalAudioUploadCutover'
import {
  CanonicalProductionExecutionDomainRole
} from './CanonicalGeneratedArtifactCutover'

// ── Minimal forward types ──

class CanonicalProductionRedaction {
  static safeIdentifier(value: string, fallback: string): string {
    const trimmed = value.trim()
    if (trimmed.length === 0) {
      return fallback
    }
    return trimmed.replace(/[^a-zA-Z0-9\-_:.]/g, '-').substring(0, 96) || fallback
  }

  static safeDiagnosticText(value: string | null): string | null {
    if (!value) {
      return null
    }
    const trimmed = value.trim()
    return trimmed.length === 0 ? null : trimmed.substring(0, 160)
  }

  static hashPrefix(value: string | null): string | null {
    if (!value || value.trim().length === 0) {
      return null
    }
    return value.trim().substring(0, 12)
  }
}

class CanonicalProjectionContract {
  static safeLogicalPathToken = safeLogicalPathToken
  static generatedArtifactKinds = generatedArtifactKinds()

  static provesGeneratedArtifactAvailability(artifact: CanonicalArtifact): boolean {
    return artifact.availability === CanonicalArtifactAvailability.AVAILABLE ||
      artifact.availability === CanonicalArtifactAvailability.AVAILABLE_WITHOUT_HASH
  }

  static isAuthoritativeProducer(artifact: CanonicalArtifact, node: CanonicalNode | null): boolean {
    if (!node) {
      return false
    }
    return true
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS
// ═══════════════════════════════════════════════════════════════════════════════

export enum CanonicalGeneratedArtifactReadProjectionSource {
  LEGACY = 'legacy',
  CANONICAL = 'canonical'
}

export enum CanonicalGeneratedArtifactReadProjectionFailureKind {
  SNAPSHOT_MISSING = 'snapshotMissing',
  UNSUPPORTED_ARTIFACT_KIND = 'unsupportedArtifactKind',
  UNSAFE_PATH_TOKEN = 'unsafePathToken',
  CONTENT_LEAK_RISK = 'contentLeakRisk',
  AUDIO_CONFUSION_RISK = 'audioConfusionRisk',
  TOMBSTONED_PARENT_RESURRECTION_RISK = 'tombstonedParentResurrectionRisk'
}

export enum CanonicalGeneratedArtifactReadSideDivergenceKind {
  MISSING_CANONICAL = 'missingCanonical',
  MISSING_LEGACY = 'missingLegacy',
  AVAILABILITY_MISMATCH = 'availabilityMismatch',
  BYTE_SIZE_MISMATCH = 'byteSizeMismatch',
  HASH_PREFIX_MISMATCH = 'hashPrefixMismatch',
  PRODUCER_MISMATCH = 'producerMismatch',
  ARTIFACT_KIND_MISMATCH = 'artifactKindMismatch',
  LOGICAL_TOKEN_MISMATCH = 'logicalTokenMismatch',
  LOCAL_DOWNLOADED_STATE_MISMATCH = 'localDownloadedStateMismatch',
  PEER_AUTHORITATIVE_STATE_MISMATCH = 'peerAuthoritativeStateMismatch',
  PARENT_STATE_MISMATCH = 'parentStateMismatch',
  UNSAFE_PATH_TOKEN = 'unsafePathToken',
  CONTENT_LEAK_RISK = 'contentLeakRisk',
  UNSUPPORTED_ARTIFACT_KIND = 'unsupportedArtifactKind',
  TOMBSTONED_PARENT_RESURRECTION_RISK = 'tombstonedParentResurrectionRisk',
  AUDIO_CONFUSION_RISK = 'audioConfusionRisk',
  UNKNOWN_BUT_REQUIRED = 'unknownButRequired'
}

export enum CanonicalGeneratedArtifactReadSideBlocker {
  MISSING_LEGACY_SNAPSHOT = 'missingLegacySnapshot',
  MISSING_CANONICAL_SNAPSHOT = 'missingCanonicalSnapshot',
  BLOCKING_DIVERGENCE = 'blockingDivergence',
  UNSAFE_PATH_TOKEN = 'unsafePathToken',
  CONTENT_LEAK_RISK = 'contentLeakRisk',
  UNSUPPORTED_ARTIFACT_KIND = 'unsupportedArtifactKind',
  TOMBSTONED_PARENT_RESURRECTION_RISK = 'tombstonedParentResurrectionRisk',
  AUDIO_CONFUSION_RISK = 'audioConfusionRisk'
}

export enum CanonicalGeneratedArtifactReadSideDiagnosticKind {
  CANONICAL_GENERATED_ARTIFACT_READ_SIDE_PARALLEL_STARTED = 'canonicalGeneratedArtifactReadSideParallelStarted',
  CANONICAL_GENERATED_ARTIFACT_READ_SIDE_PARALLEL_EQUIVALENT = 'canonicalGeneratedArtifactReadSideParallelEquivalent',
  CANONICAL_GENERATED_ARTIFACT_READ_SIDE_PARALLEL_DIVERGENT = 'canonicalGeneratedArtifactReadSideParallelDivergent',
  CANONICAL_GENERATED_ARTIFACT_READ_SIDE_FATAL_BLOCKER = 'canonicalGeneratedArtifactReadSideFatalBlocker',
  CANONICAL_GENERATED_ARTIFACT_READ_SIDE_CONTENT_EXCLUDED = 'canonicalGeneratedArtifactReadSideContentExcluded',
  CANONICAL_GENERATED_ARTIFACT_READ_SIDE_NO_MUTATION_ASSERTED = 'canonicalGeneratedArtifactReadSideNoMutationAsserted',
  CANONICAL_GENERATED_ARTIFACT_RUNTIME_SWITCH_DENIED = 'canonicalGeneratedArtifactRuntimeSwitchDenied'
}

export enum CanonicalGeneratedArtifactReadSideMode {
  DISABLED = 'disabled',
  PARALLEL_ONLY = 'parallelOnly'
}

// ═══════════════════════════════════════════════════════════════════════════════
// CLASSES
// ═══════════════════════════════════════════════════════════════════════════════

export class CanonicalGeneratedArtifactReadProjectionFailure {
  kind: CanonicalGeneratedArtifactReadProjectionFailureKind
  source: CanonicalGeneratedArtifactReadProjectionSource
  objectID: string | null
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null
  reason: string

  constructor(
    kind: CanonicalGeneratedArtifactReadProjectionFailureKind,
    source: CanonicalGeneratedArtifactReadProjectionSource, objectID: string | null = null,
    artifactID: string | null = null, artifactKind: CanonicalArtifactKind | null = null, reason: string
  ) {
    this.kind = kind
    this.source = source
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording') : null
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.artifactKind = artifactKind
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? kind
  }

  get id(): string {
    return [this.kind, this.objectID ?? '', this.artifactID ?? '', this.artifactKind ?? ''].join('|')
  }
}

export class CanonicalGeneratedArtifactReadProjectionArtifactFact {
  artifact: CanonicalArtifact
  parentTombstoned: boolean
  localAvailability: boolean
  peerAuthoritativeAvailability: boolean
  producerSummary: string | null
  unsafePathTokenObserved: boolean

  constructor(
    artifact: CanonicalArtifact, parentTombstoned: boolean = false, localAvailability: boolean = false,
    peerAuthoritativeAvailability: boolean = false, producerSummary: string | null = null,
    unsafePathTokenObserved: boolean = false
  ) {
    this.artifact = artifact
    this.parentTombstoned = parentTombstoned
    this.localAvailability = localAvailability
    this.peerAuthoritativeAvailability = peerAuthoritativeAvailability
    this.producerSummary = producerSummary ? CanonicalProductionRedaction.safeDiagnosticText(producerSummary) : null
    this.unsafePathTokenObserved = unsafePathTokenObserved
  }
}

export class CanonicalGeneratedArtifactReadProjectionItem {
  source: CanonicalGeneratedArtifactReadProjectionSource
  objectID: string
  artifactID: string
  artifactKind: CanonicalArtifactKind
  availability: CanonicalArtifactAvailability
  byteSize: number | null
  hashPrefix: string | null
  producerSummary: string | null
  logicalNameSummary: string | null
  logicalTokenSummary: string | null
  localDownloadedState: string
  peerAuthoritativeState: string
  updatedAtSummary: string | null
  parentObjectStateSummary: string
  localAvailability: boolean
  peerAuthoritativeAvailability: boolean
  parentTombstoned: boolean
  contentIncluded: boolean
  unsafePathTokenObserved: boolean

  constructor(source: CanonicalGeneratedArtifactReadProjectionSource,
    fact: CanonicalGeneratedArtifactReadProjectionArtifactFact) {
    const artifact = fact.artifact
    this.source = source
    this.objectID = CanonicalProductionRedaction.safeIdentifier(artifact.objectID, 'unknown-recording')
    this.artifactID = CanonicalProductionRedaction.safeIdentifier(artifact.artifactID, 'artifact:unknown')
    this.artifactKind = artifact.kind
    this.availability = artifact.availability
    this.byteSize = artifact.byteSize ?? null
    this.hashPrefix = artifact.contentHash ? hashPrefix(artifact.contentHash.value) : null
    this.producerSummary = fact.producerSummary ??
      (artifact.producedBy ?? artifact.producedByNodeID ?
        'node:' + CanonicalProductionRedaction.safeIdentifier(artifact.producedByNodeID ?? 'node', 'node') : null)
    this.logicalNameSummary = artifact.logicalName ?
      CanonicalProductionRedaction.safeDiagnosticText(artifact.logicalName) : null
    this.logicalTokenSummary = artifact.logicalPathToken ?
      CanonicalProjectionContract.safeLogicalPathToken(artifact.logicalPathToken) : null
    this.localDownloadedState = fact.localAvailability ? 'downloadedOrLocalAvailable' : 'notDownloadedOrUnavailable'
    this.peerAuthoritativeState = fact.peerAuthoritativeAvailability ?
      'peerAuthoritativeAvailable' : 'peerNotAuthoritativeOrUnavailable'
    this.updatedAtSummary = artifact.modifiedAt ?
      'unixSeconds=' + Math.floor(artifact.modifiedAt.date.getTime() / 1000) : null
    this.parentObjectStateSummary = fact.parentTombstoned ? 'parentTombstoned' : 'parentActiveOrUnknown'
    this.localAvailability = fact.localAvailability
    this.peerAuthoritativeAvailability = fact.peerAuthoritativeAvailability
    this.parentTombstoned = fact.parentTombstoned
    this.contentIncluded = false
    this.unsafePathTokenObserved = fact.unsafePathTokenObserved
  }

  get id(): string {
    return [this.objectID, this.artifactKind].join('|')
  }
}

export class CanonicalGeneratedArtifactReadSnapshot {
  source: CanonicalGeneratedArtifactReadProjectionSource
  generatedAt: CanonicalTimestamp
  items: CanonicalGeneratedArtifactReadProjectionItem[]
  failures: CanonicalGeneratedArtifactReadProjectionFailure[]
  contentExcludedCount: number

  constructor(
    source: CanonicalGeneratedArtifactReadProjectionSource, generatedAt: Date = new Date(),
    items: CanonicalGeneratedArtifactReadProjectionItem[],
    failures: CanonicalGeneratedArtifactReadProjectionFailure[], contentExcludedCount: number
  ) {
    this.source = source
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.items = [...items].sort((a, b) => a.id.localeCompare(b.id))
    this.failures = CanonicalGeneratedArtifactReadSnapshot.uniqueFailures(failures)
    this.contentExcludedCount = Math.max(0, contentExcludedCount)
  }

  get itemCount(): number {
    return this.items.length
  }

  get failureCount(): number {
    return this.failures.length
  }

  get contentIncludedCount(): number {
    return this.items.filter((i: CanonicalGeneratedArtifactReadProjectionItem) => i.contentIncluded).length
  }

  private static uniqueFailures(
    failures: CanonicalGeneratedArtifactReadProjectionFailure[]
  ): CanonicalGeneratedArtifactReadProjectionFailure[] {
    const map: Record<string, CanonicalGeneratedArtifactReadProjectionFailure> = {}
    for (const f of failures) {
      if (!map[f.id]) {
        map[f.id] = f
      }
    }
    return Object.values(map).sort((a, b) => a.id.localeCompare(b.id))
  }
}

export class CanonicalGeneratedArtifactReadSideDivergence {
  kind: CanonicalGeneratedArtifactReadSideDivergenceKind
  objectID: string | null
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null
  legacyValue: string | null
  canonicalValue: string | null
  fatal: boolean

  constructor(
    kind: CanonicalGeneratedArtifactReadSideDivergenceKind, objectID: string | null = null,
    artifactID: string | null = null, artifactKind: CanonicalArtifactKind | null = null,
    legacyValue: string | null = null, canonicalValue: string | null = null, fatal: boolean = false
  ) {
    this.kind = kind
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording') : null
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.artifactKind = artifactKind
    this.legacyValue = legacyValue ? CanonicalProductionRedaction.safeDiagnosticText(legacyValue) : null
    this.canonicalValue = canonicalValue ? CanonicalProductionRedaction.safeDiagnosticText(canonicalValue) : null
    this.fatal = fatal
  }

  get id(): string {
    return [this.kind, this.objectID ?? '', this.artifactID ?? '', this.artifactKind ?? ''].join('|')
  }
}

export class CanonicalGeneratedArtifactReadSideDiffReport {
  equivalent: boolean
  divergenceCount: number
  fatalDivergenceCount: number
  divergences: CanonicalGeneratedArtifactReadSideDivergence[]
  blockers: CanonicalGeneratedArtifactReadSideBlocker[]
  diagnosticsSummary: string

  constructor(
    divergences: CanonicalGeneratedArtifactReadSideDivergence[],
    blockers: CanonicalGeneratedArtifactReadSideBlocker[]
  ) {
    const uniqueMap: Record<string, CanonicalGeneratedArtifactReadSideDivergence> = {}
    for (const d of divergences) {
      if (!uniqueMap[d.id]) {
        uniqueMap[d.id] = d
      }
    }
    this.divergences = Object.values(uniqueMap).sort((a, b) => a.id.localeCompare(b.id))
    this.blockers = [...new Set(blockers)].sort()
    this.equivalent = this.divergences.length === 0 && this.blockers.length === 0
    this.divergenceCount = this.divergences.length
    this.fatalDivergenceCount = this.divergences.filter(
      (d: CanonicalGeneratedArtifactReadSideDivergence) => d.fatal).length
    this.diagnosticsSummary = [
      'domain=generatedArtifacts', 'equivalent=' + this.equivalent,
      'divergences=' + this.divergenceCount, 'fatal=' + this.fatalDivergenceCount,
      'blockers=' + this.blockers.join('+')
    ].join(',')
  }

  get hasFatalBlocker(): boolean {
    return this.fatalDivergenceCount > 0 || this.blockers.some(
      (b: CanonicalGeneratedArtifactReadSideBlocker) =>
        b === CanonicalGeneratedArtifactReadSideBlocker.UNSAFE_PATH_TOKEN ||
        b === CanonicalGeneratedArtifactReadSideBlocker.CONTENT_LEAK_RISK ||
        b === CanonicalGeneratedArtifactReadSideBlocker.UNSUPPORTED_ARTIFACT_KIND ||
        b === CanonicalGeneratedArtifactReadSideBlocker.TOMBSTONED_PARENT_RESURRECTION_RISK ||
        b === CanonicalGeneratedArtifactReadSideBlocker.AUDIO_CONFUSION_RISK)
  }

  get unsupportedArtifactCount(): number {
    return this.divergences.filter(
      (d: CanonicalGeneratedArtifactReadSideDivergence) =>
        d.kind === CanonicalGeneratedArtifactReadSideDivergenceKind.UNSUPPORTED_ARTIFACT_KIND).length
  }

  get unsafePathTokenCount(): number {
    return this.divergences.filter(
      (d: CanonicalGeneratedArtifactReadSideDivergence) =>
        d.kind === CanonicalGeneratedArtifactReadSideDivergenceKind.UNSAFE_PATH_TOKEN).length
  }

  get contentLeakRiskCount(): number {
    return this.divergences.filter(
      (d: CanonicalGeneratedArtifactReadSideDivergence) =>
        d.kind === CanonicalGeneratedArtifactReadSideDivergenceKind.CONTENT_LEAK_RISK).length
  }

  get parentTombstoneBlockCount(): number {
    return this.divergences.filter(
      (d: CanonicalGeneratedArtifactReadSideDivergence) =>
        d.kind === CanonicalGeneratedArtifactReadSideDivergenceKind.TOMBSTONED_PARENT_RESURRECTION_RISK ||
        d.kind === CanonicalGeneratedArtifactReadSideDivergenceKind.PARENT_STATE_MISMATCH).length
  }

  get audioConfusionRiskCount(): number {
    return this.divergences.filter(
      (d: CanonicalGeneratedArtifactReadSideDivergence) =>
        d.kind === CanonicalGeneratedArtifactReadSideDivergenceKind.AUDIO_CONFUSION_RISK).length
  }
}

export class CanonicalGeneratedArtifactReadSideDiagnostic {
  kind: CanonicalGeneratedArtifactReadSideDiagnosticKind
  syncRunID: string | null
  trigger: CanonicalSyncPlanTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  objectID: string | null
  artifactID: string | null
  artifactKind: CanonicalArtifactKind | null
  result: string | null
  reason: string | null
  hashPrefix: string | null

  constructor(
    kind: CanonicalGeneratedArtifactReadSideDiagnosticKind, syncRunID: string | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole,
    objectID: string | null = null, artifactID: string | null = null,
    artifactKind: CanonicalArtifactKind | null = null, result: string | null = null,
    reason: string | null = null, hash: CanonicalHash | null = null, hashPrefix: string | null = null
  ) {
    this.kind = kind
    this.syncRunID = syncRunID ? CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run') : null
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording') : null
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.artifactKind = artifactKind
    this.result = result ? CanonicalProductionRedaction.safeDiagnosticText(result) : null
    this.reason = reason ? CanonicalProductionRedaction.safeDiagnosticText(reason) : null
    this.hashPrefix = hash ? CanonicalProductionRedaction.hashPrefix(hash.value) :
      CanonicalProductionRedaction.hashPrefix(hashPrefix)
  }

  get id(): string {
    return [this.kind, this.syncRunID ?? '', this.objectID ?? '', this.artifactID ?? ''].join('|')
  }
}

export class CanonicalGeneratedArtifactReadSidePolicy {
  recordDiagnostics: boolean
  maxDiagnosticsEvents: number

  constructor(recordDiagnostics: boolean = true, maxDiagnosticsEvents: number = 200) {
    this.recordDiagnostics = recordDiagnostics
    this.maxDiagnosticsEvents = Math.max(1, maxDiagnosticsEvents)
  }
}

export class CanonicalGeneratedArtifactReadSideConfiguration {
  isEnabled: boolean
  mode: CanonicalGeneratedArtifactReadSideMode
  policy: CanonicalGeneratedArtifactReadSidePolicy

  constructor(
    isEnabled: boolean = false,
    mode: CanonicalGeneratedArtifactReadSideMode = CanonicalGeneratedArtifactReadSideMode.DISABLED,
    policy: CanonicalGeneratedArtifactReadSidePolicy = new CanonicalGeneratedArtifactReadSidePolicy()
  ) {
    this.isEnabled = isEnabled
    this.mode = isEnabled ? mode : CanonicalGeneratedArtifactReadSideMode.DISABLED
    this.policy = policy
  }

  static get disabled(): CanonicalGeneratedArtifactReadSideConfiguration {
    return new CanonicalGeneratedArtifactReadSideConfiguration()
  }

  static enabled(
    mode: CanonicalGeneratedArtifactReadSideMode = CanonicalGeneratedArtifactReadSideMode.PARALLEL_ONLY,
    policy: CanonicalGeneratedArtifactReadSidePolicy = new CanonicalGeneratedArtifactReadSidePolicy()
  ): CanonicalGeneratedArtifactReadSideConfiguration {
    return new CanonicalGeneratedArtifactReadSideConfiguration(true, mode, policy)
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// STATIC FUNCTIONS / NAMESPACES
// ═══════════════════════════════════════════════════════════════════════════════

function snapshotFromManifest(
  source: CanonicalGeneratedArtifactReadProjectionSource, localManifest: CanonicalManifest | null,
  peerManifest: CanonicalManifest | null, generatedAt: Date = new Date()
): CanonicalGeneratedArtifactReadSnapshot {
  let facts: CanonicalGeneratedArtifactReadProjectionArtifactFact[] = []
  let failures: CanonicalGeneratedArtifactReadProjectionFailure[] = []

  if (!localManifest && !peerManifest) {
    failures.push(new CanonicalGeneratedArtifactReadProjectionFailure(
      CanonicalGeneratedArtifactReadProjectionFailureKind.SNAPSHOT_MISSING, source, null, null, null,
      'generatedArtifactReadProjectionSnapshotMissing'))
  }

  if (localManifest) {
    appendManifestFacts(localManifest, false, facts, failures, source)
  }
  if (peerManifest) {
    appendManifestFacts(peerManifest, true, facts, failures, source)
  }
  return snapshotFromFacts(source, facts, failures, generatedAt)
}

function snapshotFromFacts(
  source: CanonicalGeneratedArtifactReadProjectionSource,
  facts: CanonicalGeneratedArtifactReadProjectionArtifactFact[],
  seedFailures: CanonicalGeneratedArtifactReadProjectionFailure[] = [],
  generatedAt: Date = new Date()
): CanonicalGeneratedArtifactReadSnapshot {
  let failures = [...seedFailures]
  let items: CanonicalGeneratedArtifactReadProjectionItem[] = []

  for (const fact of facts) {
    const artifact = fact.artifact
    if (artifact.kind === CanonicalArtifactKind.AUDIO) {
      failures.push(new CanonicalGeneratedArtifactReadProjectionFailure(
        CanonicalGeneratedArtifactReadProjectionFailureKind.AUDIO_CONFUSION_RISK, source, artifact.objectID,
        artifact.artifactID, artifact.kind, 'audioArtifactExcludedFromGeneratedArtifactReadProjection'))
      continue
    }
    if (!CanonicalProjectionContract.generatedArtifactKinds.includes(artifact.kind)) {
      failures.push(new CanonicalGeneratedArtifactReadProjectionFailure(
        CanonicalGeneratedArtifactReadProjectionFailureKind.UNSUPPORTED_ARTIFACT_KIND, source,
        artifact.objectID, artifact.artifactID, artifact.kind, 'unsupportedArtifactKind'))
      continue
    }
    if (fact.unsafePathTokenObserved) {
      failures.push(new CanonicalGeneratedArtifactReadProjectionFailure(
        CanonicalGeneratedArtifactReadProjectionFailureKind.UNSAFE_PATH_TOKEN, source, artifact.objectID,
        artifact.artifactID, artifact.kind, 'unsafePathTokenObserved'))
    }
    if (fact.parentTombstoned && artifact.availability !== CanonicalArtifactAvailability.MISSING &&
      artifact.tombstone !== true) {
      failures.push(new CanonicalGeneratedArtifactReadProjectionFailure(
        CanonicalGeneratedArtifactReadProjectionFailureKind.TOMBSTONED_PARENT_RESURRECTION_RISK, source,
        artifact.objectID, artifact.artifactID, artifact.kind, 'availableArtifactUnderTombstonedParent'))
    }
    items.push(new CanonicalGeneratedArtifactReadProjectionItem(source, fact))
  }

  if (items.some((i: CanonicalGeneratedArtifactReadProjectionItem) => i.contentIncluded)) {
    failures.push(new CanonicalGeneratedArtifactReadProjectionFailure(
      CanonicalGeneratedArtifactReadProjectionFailureKind.CONTENT_LEAK_RISK, source, null, null, null,
      'contentIncludedInGeneratedArtifactReadProjection'))
  }
  return new CanonicalGeneratedArtifactReadSnapshot(source, generatedAt, items, failures, items.length)
}

function appendManifestFacts(
  manifest: CanonicalManifest, peerAuthoritative: boolean,
  facts: CanonicalGeneratedArtifactReadProjectionArtifactFact[],
  failures: CanonicalGeneratedArtifactReadProjectionFailure[],
  source: CanonicalGeneratedArtifactReadProjectionSource
): void {
  for (const obj of manifest.objects) {
    for (const artifact of obj.artifacts) {
      const localAvailability = !peerAuthoritative &&
        CanonicalProjectionContract.provesGeneratedArtifactAvailability(artifact)
      const peerAvailability = peerAuthoritative &&
        CanonicalProjectionContract.isAuthoritativeProducer(artifact, manifest.node)
      facts.push(new CanonicalGeneratedArtifactReadProjectionArtifactFact(
        artifact, obj.metadata.isDeleted || obj.syncState === 'deleted', localAvailability, peerAvailability,
        artifact.producedBy ?? manifest.node.platform, false))
    }
  }
}

export function compareReadSides(
  legacy: CanonicalGeneratedArtifactReadSnapshot | null,
  canonical: CanonicalGeneratedArtifactReadSnapshot | null
): CanonicalGeneratedArtifactReadSideDiffReport {
  let divergences: CanonicalGeneratedArtifactReadSideDivergence[] = []
  let blockers: CanonicalGeneratedArtifactReadSideBlocker[] = []

  if (!legacy) {
    return new CanonicalGeneratedArtifactReadSideDiffReport([], [
      CanonicalGeneratedArtifactReadSideBlocker.MISSING_LEGACY_SNAPSHOT])
  }
  if (!canonical) {
    return new CanonicalGeneratedArtifactReadSideDiffReport([], [
      CanonicalGeneratedArtifactReadSideBlocker.MISSING_CANONICAL_SNAPSHOT])
  }

  appendFailureDivergences(legacy.failures.concat(canonical.failures), divergences, blockers)

  const legacyMap: Record<string, CanonicalGeneratedArtifactReadProjectionItem> = {}
  for (const item of legacy.items) {
    legacyMap[item.id] = item
  }
  const canonicalMap: Record<string, CanonicalGeneratedArtifactReadProjectionItem> = {}
  for (const item of canonical.items) {
    canonicalMap[item.id] = item
  }
  const keys = [...new Set([...Object.keys(legacyMap), ...Object.keys(canonicalMap)])].sort()
  for (const key of keys) {
    const legacyItem = legacyMap[key]
    const canonicalItem = canonicalMap[key]
    if (!legacyItem && canonicalItem) {
      divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
        CanonicalGeneratedArtifactReadSideDivergenceKind.MISSING_LEGACY, canonicalItem.objectID,
        canonicalItem.artifactID, canonicalItem.artifactKind, null, 'present'))
      continue
    }
    if (!canonicalItem && legacyItem) {
      divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
        CanonicalGeneratedArtifactReadSideDivergenceKind.MISSING_CANONICAL, legacyItem.objectID,
        legacyItem.artifactID, legacyItem.artifactKind, 'present'))
      continue
    }
    if (legacyItem && canonicalItem) {
      compareItems(legacyItem, canonicalItem, divergences)
    }
  }
  if (divergences.length > 0) {
    blockers.push(CanonicalGeneratedArtifactReadSideBlocker.BLOCKING_DIVERGENCE)
  }
  return new CanonicalGeneratedArtifactReadSideDiffReport(divergences, blockers)
}

function compareItems(
  legacy: CanonicalGeneratedArtifactReadProjectionItem,
  canonical: CanonicalGeneratedArtifactReadProjectionItem,
  divergences: CanonicalGeneratedArtifactReadSideDivergence[]
): void {
  if (legacy.artifactKind !== canonical.artifactKind) {
    divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
      CanonicalGeneratedArtifactReadSideDivergenceKind.ARTIFACT_KIND_MISMATCH, legacy.objectID,
      legacy.artifactID, legacy.artifactKind, legacy.artifactKind, canonical.artifactKind))
  }
  if (legacy.availability !== canonical.availability) {
    divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
      CanonicalGeneratedArtifactReadSideDivergenceKind.AVAILABILITY_MISMATCH, legacy.objectID,
      legacy.artifactID, legacy.artifactKind, legacy.availability, canonical.availability))
  }
  if (legacy.byteSize !== canonical.byteSize) {
    divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
      CanonicalGeneratedArtifactReadSideDivergenceKind.BYTE_SIZE_MISMATCH, legacy.objectID,
      legacy.artifactID, legacy.artifactKind, legacy.byteSize?.toString() ?? null,
      canonical.byteSize?.toString() ?? null))
  }
  if (legacy.hashPrefix && canonical.hashPrefix && legacy.hashPrefix !== canonical.hashPrefix) {
    divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
      CanonicalGeneratedArtifactReadSideDivergenceKind.HASH_PREFIX_MISMATCH, legacy.objectID,
      legacy.artifactID, legacy.artifactKind, legacy.hashPrefix, canonical.hashPrefix))
  }
  if (legacy.localAvailability !== canonical.localAvailability) {
    divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
      CanonicalGeneratedArtifactReadSideDivergenceKind.LOCAL_DOWNLOADED_STATE_MISMATCH, legacy.objectID,
      legacy.artifactID, legacy.artifactKind, legacy.localDownloadedState, canonical.localDownloadedState))
  }
  if (legacy.peerAuthoritativeAvailability !== canonical.peerAuthoritativeAvailability) {
    divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
      CanonicalGeneratedArtifactReadSideDivergenceKind.PEER_AUTHORITATIVE_STATE_MISMATCH, legacy.objectID,
      legacy.artifactID, legacy.artifactKind, legacy.peerAuthoritativeState, canonical.peerAuthoritativeState))
  }
  if (legacy.parentTombstoned !== canonical.parentTombstoned) {
    divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
      CanonicalGeneratedArtifactReadSideDivergenceKind.PARENT_STATE_MISMATCH, legacy.objectID,
      legacy.artifactID, legacy.artifactKind, legacy.parentObjectStateSummary,
      canonical.parentObjectStateSummary))
  }
}

function appendFailureDivergences(
  failures: CanonicalGeneratedArtifactReadProjectionFailure[],
  divergences: CanonicalGeneratedArtifactReadSideDivergence[],
  blockers: CanonicalGeneratedArtifactReadSideBlocker[]
): void {
  for (const failure of failures) {
    switch (failure.kind) {
      case CanonicalGeneratedArtifactReadProjectionFailureKind.SNAPSHOT_MISSING:
        continue
      case CanonicalGeneratedArtifactReadProjectionFailureKind.UNSUPPORTED_ARTIFACT_KIND:
        blockers.push(CanonicalGeneratedArtifactReadSideBlocker.UNSUPPORTED_ARTIFACT_KIND)
        divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
          CanonicalGeneratedArtifactReadSideDivergenceKind.UNSUPPORTED_ARTIFACT_KIND, failure.objectID,
          failure.artifactID, failure.artifactKind,
          failure.source === CanonicalGeneratedArtifactReadProjectionSource.LEGACY ? failure.reason : null,
          failure.source === CanonicalGeneratedArtifactReadProjectionSource.CANONICAL ? failure.reason : null, true))
        break
      case CanonicalGeneratedArtifactReadProjectionFailureKind.UNSAFE_PATH_TOKEN:
        blockers.push(CanonicalGeneratedArtifactReadSideBlocker.UNSAFE_PATH_TOKEN)
        divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
          CanonicalGeneratedArtifactReadSideDivergenceKind.UNSAFE_PATH_TOKEN, failure.objectID,
          failure.artifactID, failure.artifactKind,
          failure.source === CanonicalGeneratedArtifactReadProjectionSource.LEGACY ? failure.reason : null,
          failure.source === CanonicalGeneratedArtifactReadProjectionSource.CANONICAL ? failure.reason : null, true))
        break
      case CanonicalGeneratedArtifactReadProjectionFailureKind.CONTENT_LEAK_RISK:
        blockers.push(CanonicalGeneratedArtifactReadSideBlocker.CONTENT_LEAK_RISK)
        divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
          CanonicalGeneratedArtifactReadSideDivergenceKind.CONTENT_LEAK_RISK, failure.objectID,
          failure.artifactID, failure.artifactKind,
          failure.source === CanonicalGeneratedArtifactReadProjectionSource.LEGACY ? failure.reason : null,
          failure.source === CanonicalGeneratedArtifactReadProjectionSource.CANONICAL ? failure.reason : null, true))
        break
      case CanonicalGeneratedArtifactReadProjectionFailureKind.AUDIO_CONFUSION_RISK:
        blockers.push(CanonicalGeneratedArtifactReadSideBlocker.AUDIO_CONFUSION_RISK)
        divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
          CanonicalGeneratedArtifactReadSideDivergenceKind.AUDIO_CONFUSION_RISK, failure.objectID,
          failure.artifactID, failure.artifactKind,
          failure.source === CanonicalGeneratedArtifactReadProjectionSource.LEGACY ? failure.reason : null,
          failure.source === CanonicalGeneratedArtifactReadProjectionSource.CANONICAL ? failure.reason : null, true))
        break
      case CanonicalGeneratedArtifactReadProjectionFailureKind.TOMBSTONED_PARENT_RESURRECTION_RISK:
        blockers.push(CanonicalGeneratedArtifactReadSideBlocker.TOMBSTONED_PARENT_RESURRECTION_RISK)
        divergences.push(new CanonicalGeneratedArtifactReadSideDivergence(
          CanonicalGeneratedArtifactReadSideDivergenceKind.TOMBSTONED_PARENT_RESURRECTION_RISK, failure.objectID,
          failure.artifactID, failure.artifactKind,
          failure.source === CanonicalGeneratedArtifactReadProjectionSource.LEGACY ? failure.reason : null,
          failure.source === CanonicalGeneratedArtifactReadProjectionSource.CANONICAL ? failure.reason : null, true))
        break
    }
  }
}

export class CanonicalGeneratedArtifactReadSideEvaluator {
  constructor() {}

  evaluate(
    configuration: CanonicalGeneratedArtifactReadSideConfiguration,
    legacySnapshot: CanonicalGeneratedArtifactReadSnapshot | null,
    canonicalSnapshot: CanonicalGeneratedArtifactReadSnapshot | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole,
    syncRunID: string | null
  ): CanonicalGeneratedArtifactReadSideEvaluationResult {
    if (!configuration.isEnabled || configuration.mode !== CanonicalGeneratedArtifactReadSideMode.PARALLEL_ONLY) {
      return CanonicalGeneratedArtifactReadSideEvaluationResult.disabled(configuration, syncRunID, trigger, nodeRole)
    }
    const diff = compareReadSides(legacySnapshot, canonicalSnapshot)
    return CanonicalGeneratedArtifactReadSideEvaluationResult.evaluated(configuration, diff, syncRunID, trigger, nodeRole)
  }
}

export class CanonicalGeneratedArtifactReadSideEvaluationResult {
  configuration: CanonicalGeneratedArtifactReadSideConfiguration
  diffReport: CanonicalGeneratedArtifactReadSideDiffReport | null
  diagnostics: CanonicalGeneratedArtifactReadSideDiagnostic[]
  storeMutated: boolean
  uiMutated: boolean
  artifactDownloaded: boolean
  artifactApplied: boolean
  uploadJobCreated: boolean

  constructor(
    configuration: CanonicalGeneratedArtifactReadSideConfiguration,
    diffReport: CanonicalGeneratedArtifactReadSideDiffReport | null,
    diagnostics: CanonicalGeneratedArtifactReadSideDiagnostic[], storeMutated: boolean = false,
    uiMutated: boolean = false, artifactDownloaded: boolean = false, artifactApplied: boolean = false,
    uploadJobCreated: boolean = false
  ) {
    this.configuration = configuration
    this.diffReport = diffReport
    this.diagnostics = diagnostics
    this.storeMutated = storeMutated
    this.uiMutated = uiMutated
    this.artifactDownloaded = artifactDownloaded
    this.artifactApplied = artifactApplied
    this.uploadJobCreated = uploadJobCreated
  }

  get noMutationAsserted(): boolean {
    return !this.storeMutated && !this.uiMutated && !this.artifactDownloaded && !this.artifactApplied &&
      !this.uploadJobCreated
  }

  static disabled(
    configuration: CanonicalGeneratedArtifactReadSideConfiguration, syncRunID: string | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole
  ): CanonicalGeneratedArtifactReadSideEvaluationResult {
    return new CanonicalGeneratedArtifactReadSideEvaluationResult(configuration, null, [
      new CanonicalGeneratedArtifactReadSideDiagnostic(
        CanonicalGeneratedArtifactReadSideDiagnosticKind.CANONICAL_GENERATED_ARTIFACT_RUNTIME_SWITCH_DENIED,
        syncRunID, trigger, nodeRole, null, null, null, 'disabled', 'readSideParallelDefaultOff')
    ], false, false, false, false, false)
  }

  static evaluated(
    configuration: CanonicalGeneratedArtifactReadSideConfiguration,
    diffReport: CanonicalGeneratedArtifactReadSideDiffReport, syncRunID: string | null,
    trigger: CanonicalSyncPlanTrigger, nodeRole: CanonicalProductionExecutionDomainRole
  ): CanonicalGeneratedArtifactReadSideEvaluationResult {
    let diagnostics: CanonicalGeneratedArtifactReadSideDiagnostic[] = [
      new CanonicalGeneratedArtifactReadSideDiagnostic(
        CanonicalGeneratedArtifactReadSideDiagnosticKind.CANONICAL_GENERATED_ARTIFACT_READ_SIDE_PARALLEL_STARTED,
        syncRunID, trigger, nodeRole, null, null, null, 'started', 'mutatedUI=false,storeMutated=false'),
      new CanonicalGeneratedArtifactReadSideDiagnostic(
        diffReport.equivalent ?
          CanonicalGeneratedArtifactReadSideDiagnosticKind.CANONICAL_GENERATED_ARTIFACT_READ_SIDE_PARALLEL_EQUIVALENT :
          CanonicalGeneratedArtifactReadSideDiagnosticKind.CANONICAL_GENERATED_ARTIFACT_READ_SIDE_PARALLEL_DIVERGENT,
        syncRunID, trigger, nodeRole, null, null, null,
        diffReport.equivalent ? 'equivalent' : 'divergent', diffReport.diagnosticsSummary),
      new CanonicalGeneratedArtifactReadSideDiagnostic(
        CanonicalGeneratedArtifactReadSideDiagnosticKind.CANONICAL_GENERATED_ARTIFACT_READ_SIDE_NO_MUTATION_ASSERTED,
        syncRunID, trigger, nodeRole, null, null, null, 'true', 'noDownloadNoApplyNoStoreNoUI')
    ]
    if (diffReport.hasFatalBlocker) {
      diagnostics.push(new CanonicalGeneratedArtifactReadSideDiagnostic(
        CanonicalGeneratedArtifactReadSideDiagnosticKind.CANONICAL_GENERATED_ARTIFACT_READ_SIDE_FATAL_BLOCKER,
        syncRunID, trigger, nodeRole, null, null, null, 'blocked', diffReport.blockers.join('+')))
    }
    return new CanonicalGeneratedArtifactReadSideEvaluationResult(configuration, diffReport, diagnostics, false, false,
      false, false, false)
  }
}

// ── Export snapshot builder ──

export {
  snapshotFromManifest as buildReadSnapshot,
  snapshotFromFacts as buildReadSnapshotFromFacts
}
