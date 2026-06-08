/**
 * CanonicalLibraryMetadataReadProjection — read projection, snapshot, diff, and cutover evaluation.
 * Ported from Rokurics-Apple (CanonicalLibraryMetadataReadProjection.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalManifest,
  CanonicalLibraryObjectID, CanonicalObjectKind,
  CanonicalStudyItemKind, CanonicalParentReference,
  CanonicalLibraryObject, CanonicalFolderObject,
  CanonicalStudyItemObject, CanonicalStandaloneNoteObject,
  nilIfEmpty, hashPrefix, timestampString
} from './CanonicalCore'
import {
  CanonicalMigrationDomain,
  CanonicalMigrationDomainMatrix,
  CanonicalLibraryMetadataCutoverDomain,
  CanonicalLibraryMetadataCutoverDiagnostic,
  CanonicalLibraryMetadataCutoverDiagnosticKind,
  CanonicalLibraryMetadataStageEvidenceStatus,
  CanonicalLibraryMetadataCanaryStage,
  CanonicalProductionExecutionDomainRole
} from './CanonicalLibraryMetadataCutover'
import { CanonicalSyncPlanTrigger } from './CanonicalLibrarySyncPlanner'

// ── Redaction ──

class CanonicalProductionRedaction {
  static safeIdentifier(value: string, fallback: string): string {
    const t = value.trim()
    return t.length === 0 ? fallback : t
  }
  static safeDiagnosticText(value: string | null | undefined): string | null {
    if (!value) return null
    const t = value.trim()
    return t.length === 0 ? null : t
  }
  static hashPrefix(value: string | null | undefined): string | null {
    if (!value) return null
    return hashPrefix(value, 12)
  }
  static containsSensitivePathSignal(value: string): boolean {
    return value.includes('/private/') || value.includes('/.gradle/')
  }
}

// ── Read Projection Source ──

export enum CanonicalLibraryMetadataReadProjectionSource {
  LEGACY = 'legacy',
  CANONICAL = 'canonical'
}

// ── Read Projection Failure ──

export enum CanonicalLibraryMetadataReadProjectionFailureKind {
  SNAPSHOT_MISSING = 'snapshotMissing',
  MISSING_METADATA = 'missingMetadata',
  UNSUPPORTED_OBJECT = 'unsupportedObject',
  PATH_LEAK_RISK = 'pathLeakRisk',
  FULL_CONTENT_REJECTED = 'fullContentRejected'
}

export class CanonicalLibraryMetadataReadProjectionFailure {
  kind: CanonicalLibraryMetadataReadProjectionFailureKind
  objectID: string | null
  objectKind: CanonicalObjectKind | null
  reason: string

  constructor(kind: CanonicalLibraryMetadataReadProjectionFailureKind, objectID: string | null, objectKind: CanonicalObjectKind | null, reason: string) {
    this.kind = kind
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'library-object') : null
    this.objectKind = objectKind
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? kind
  }

  get id(): string {
    return [this.kind, this.objectID ?? 'run', this.reason].join('|')
  }
}

// ── Read Projection Folder / Item / Note ──

export class CanonicalLibraryMetadataReadProjectionFolder {
  objectID: CanonicalLibraryObjectID
  folderID: CanonicalLibraryObjectID
  title: string
  parentID: CanonicalLibraryObjectID | null
  hierarchyPath: string[]
  hierarchyLevel: string | null
  colorToken: string | null
  orderingKey: string | null
  isDeleted: boolean
  metadataHashPrefix: string | null

  constructor(folder: CanonicalFolderObject) {
    this.objectID = folder.folderID
    this.folderID = folder.folderID
    this.title = folder.metadata.name
    this.parentID = folder.metadata.parentID
    this.hierarchyPath = folder.metadata.hierarchyPath.components
    this.hierarchyLevel = folder.metadata.hierarchyLevel !== undefined && folder.metadata.hierarchyLevel !== null ? folder.metadata.hierarchyLevel.toString() : null
    this.colorToken = folder.metadata.colorToken
    this.orderingKey = folder.metadata.orderingKey
    this.isDeleted = folder.metadata.isDeleted
    this.metadataHashPrefix = CanonicalProductionRedaction.hashPrefix(folder.metadataHash.value)
  }

  get id(): string {
    return this.folderID.rawValue
  }

  get hierarchyKey(): string {
    return this.hierarchyPath.join('>')
  }
}

export class CanonicalLibraryMetadataReadProjectionItem {
  objectID: CanonicalLibraryObjectID
  itemID: CanonicalLibraryObjectID
  title: string
  itemKind: CanonicalStudyItemKind
  folderIDs: CanonicalLibraryObjectID[]
  parentReferences: CanonicalParentReference[]
  tags: string[]
  filingComponents: string[]
  resourceTokenSummary: string
  isDeleted: boolean
  metadataHashPrefix: string | null
  orderingKey: string | null

  constructor(item: CanonicalStudyItemObject, objectID: CanonicalLibraryObjectID | null = null) {
    this.objectID = objectID ?? item.itemID
    this.itemID = item.itemID
    this.title = item.metadata.title
    this.itemKind = item.metadata.itemKind
    this.folderIDs = item.metadata.folderIDs
    this.parentReferences = item.metadata.parentReferences
    this.tags = item.metadata.tags
    this.filingComponents = item.metadata.filingPath.components
    this.resourceTokenSummary = safeResourceTokenSummary(item.metadata.logicalResourceTokens)
    this.isDeleted = item.metadata.isDeleted
    this.metadataHashPrefix = CanonicalProductionRedaction.hashPrefix(item.metadataHash.value)
    this.orderingKey = null
  }

  get id(): string {
    return this.itemID.rawValue
  }

  get filingKey(): string {
    return this.filingComponents.join('>')
  }

  get folderIDKey(): string {
    return this.folderIDs.map(id => id.rawValue).join('|')
  }

  get parentReferenceKey(): string {
    return this.parentReferences.map(r => r.relation + ':' + r.parentID.rawValue).join('|')
  }

  get tagKey(): string {
    return this.tags.join('|')
  }
}

export class CanonicalLibraryMetadataReadProjectionNote {
  objectID: CanonicalLibraryObjectID
  noteItemID: CanonicalLibraryObjectID
  title: string
  folderIDs: CanonicalLibraryObjectID[]
  parentReferences: CanonicalParentReference[]
  tags: string[]
  filingComponents: string[]
  resourceTokenSummary: string
  isDeleted: boolean
  metadataHashPrefix: string | null
  fullContentIncluded: boolean

  constructor(item: CanonicalStudyItemObject, objectID: CanonicalLibraryObjectID | null = null) {
    this.objectID = objectID ?? item.itemID
    this.noteItemID = item.itemID
    this.title = item.metadata.title
    this.folderIDs = item.metadata.folderIDs
    this.parentReferences = item.metadata.parentReferences
    this.tags = item.metadata.tags
    this.filingComponents = item.metadata.filingPath.components
    this.resourceTokenSummary = safeResourceTokenSummary(item.metadata.logicalResourceTokens)
    this.isDeleted = item.metadata.isDeleted
    this.metadataHashPrefix = CanonicalProductionRedaction.hashPrefix(item.metadataHash.value)
    this.fullContentIncluded = false
  }

  get id(): string {
    return this.noteItemID.rawValue
  }

  get filingKey(): string {
    return this.filingComponents.join('>')
  }

  get folderIDKey(): string {
    return this.folderIDs.map(id => id.rawValue).join('|')
  }

  get parentReferenceKey(): string {
    return this.parentReferences.map(r => r.relation + ':' + r.parentID.rawValue).join('|')
  }

  get tagKey(): string {
    return this.tags.join('|')
  }
}

function safeResourceTokenSummary(tokens: string[]): string {
  const safePrefixes: string[] = []
  for (const token of tokens) {
    if (CanonicalProductionRedaction.containsSensitivePathSignal(token)) continue
    safePrefixes.push(CanonicalProductionRedaction.hashPrefix(CanonicalHash.sha256OfString(token).value) ?? '')
  }
  return 'count=' + safePrefixes.length + ',tokens=' + safePrefixes.join('+')
}

// ── Read Snapshot ──

export class CanonicalLibraryMetadataReadSnapshot {
  source: CanonicalLibraryMetadataReadProjectionSource
  generatedAt: CanonicalTimestamp
  folders: CanonicalLibraryMetadataReadProjectionFolder[]
  studyItems: CanonicalLibraryMetadataReadProjectionItem[]
  standaloneNotes: CanonicalLibraryMetadataReadProjectionNote[]
  failures: CanonicalLibraryMetadataReadProjectionFailure[]
  contentExcludedCount: number

  constructor(
    source: CanonicalLibraryMetadataReadProjectionSource,
    generatedAt: Date = new Date(),
    folders: CanonicalLibraryMetadataReadProjectionFolder[] = [],
    studyItems: CanonicalLibraryMetadataReadProjectionItem[] = [],
    standaloneNotes: CanonicalLibraryMetadataReadProjectionNote[] = [],
    failures: CanonicalLibraryMetadataReadProjectionFailure[] = [],
    contentExcludedCount: number = 0
  ) {
    this.source = source
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.folders = folders.sort((a, b) => a.folderID.rawValue.localeCompare(b.folderID.rawValue))
    this.studyItems = studyItems.sort((a, b) => a.itemID.rawValue.localeCompare(b.itemID.rawValue))
    this.standaloneNotes = standaloneNotes.sort((a, b) => a.noteItemID.rawValue.localeCompare(b.noteItemID.rawValue))
    this.failures = failures.sort((a, b) => a.id.localeCompare(b.id))
    this.contentExcludedCount = Math.max(0, contentExcludedCount)
  }

  get objectCount(): number {
    return this.folders.length + this.studyItems.length + this.standaloneNotes.length
  }

  get unsupportedObjectCount(): number {
    return this.failures.filter(f => f.kind === CanonicalLibraryMetadataReadProjectionFailureKind.UNSUPPORTED_OBJECT ||
      f.kind === CanonicalLibraryMetadataReadProjectionFailureKind.MISSING_METADATA).length
  }

  get pathLeakRiskCount(): number {
    return this.failures.filter(f => f.kind === CanonicalLibraryMetadataReadProjectionFailureKind.PATH_LEAK_RISK).length
  }

  get diagnosticsSummary(): string {
    return 'source=' + this.source + ',folders=' + this.folders.length + ',items=' + this.studyItems.length +
      ',notes=' + this.standaloneNotes.length + ',unsupported=' + this.unsupportedObjectCount +
      ',pathLeakRisk=' + this.pathLeakRiskCount + ',contentExcluded=' + this.contentExcludedCount
  }
}

// ── Read Projection Builder ──

export class CanonicalLibraryMetadataReadProjection {
  snapshot: CanonicalLibraryMetadataReadSnapshot

  constructor(snapshot: CanonicalLibraryMetadataReadSnapshot) {
    this.snapshot = snapshot
  }

  static buildFromManifest(
    source: CanonicalLibraryMetadataReadProjectionSource,
    manifest: CanonicalManifest | null,
    generatedAt: Date = new Date()
  ): CanonicalLibraryMetadataReadProjection {
    if (!manifest) {
      return new CanonicalLibraryMetadataReadProjection(new CanonicalLibraryMetadataReadSnapshot(
        source, generatedAt, [], [], [], [
          new CanonicalLibraryMetadataReadProjectionFailure(
            CanonicalLibraryMetadataReadProjectionFailureKind.SNAPSHOT_MISSING, null, null, 'canonicalManifestMissing')
        ]))
    }
    return CanonicalLibraryMetadataReadProjection.buildFromObjects(source, manifest.libraryObjects, generatedAt)
  }

  static buildFromObjects(
    source: CanonicalLibraryMetadataReadProjectionSource,
    objects: CanonicalLibraryObject[],
    generatedAt: Date = new Date()
  ): CanonicalLibraryMetadataReadProjection {
    const folders: CanonicalLibraryMetadataReadProjectionFolder[] = []
    const studyItems: CanonicalLibraryMetadataReadProjectionItem[] = []
    const standaloneNotes: CanonicalLibraryMetadataReadProjectionNote[] = []
    const failures: CanonicalLibraryMetadataReadProjectionFailure[] = []
    const sorted = [...objects].sort((a, b) => a.objectID.rawValue.localeCompare(b.objectID.rawValue))
    for (const obj of sorted) {
      switch (obj.kind) {
        case CanonicalObjectKind.FOLDER:
          if (!obj.folder) {
            failures.push(new CanonicalLibraryMetadataReadProjectionFailure(
              CanonicalLibraryMetadataReadProjectionFailureKind.MISSING_METADATA, obj.objectID.rawValue, obj.kind, 'folderMetadataMissing'))
            break
          }
          folders.push(new CanonicalLibraryMetadataReadProjectionFolder(obj.folder))
          break
        case CanonicalObjectKind.STANDALONE_NOTE: {
          const item = obj.standaloneNote?.studyItem ?? obj.studyItem
          if (!item) {
            failures.push(new CanonicalLibraryMetadataReadProjectionFailure(
              CanonicalLibraryMetadataReadProjectionFailureKind.MISSING_METADATA, obj.objectID.rawValue, obj.kind, 'standaloneNoteMetadataMissing'))
            break
          }
          standaloneNotes.push(new CanonicalLibraryMetadataReadProjectionNote(item, obj.objectID))
          break
        }
        case CanonicalObjectKind.STANDALONE_STUDY_ITEM:
        case CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM:
          if (!obj.studyItem) {
            failures.push(new CanonicalLibraryMetadataReadProjectionFailure(
              CanonicalLibraryMetadataReadProjectionFailureKind.MISSING_METADATA, obj.objectID.rawValue, obj.kind, 'studyItemMetadataMissing'))
            break
          }
          studyItems.push(new CanonicalLibraryMetadataReadProjectionItem(obj.studyItem, obj.objectID))
          break
        default:
          failures.push(new CanonicalLibraryMetadataReadProjectionFailure(
            CanonicalLibraryMetadataReadProjectionFailureKind.UNSUPPORTED_OBJECT, obj.objectID.rawValue, obj.kind,
            obj.unsupportedReason ?? 'unsupportedLibraryMetadataReadObject'))
          break
      }
    }
    return new CanonicalLibraryMetadataReadProjection(new CanonicalLibraryMetadataReadSnapshot(
      source, generatedAt, folders, studyItems, standaloneNotes, failures, standaloneNotes.length))
  }
}

// ── Read Side Divergence ──

export enum CanonicalLibraryMetadataReadSideDivergenceKind {
  MISSING_IN_CANONICAL = 'missingInCanonical',
  MISSING_IN_LEGACY = 'missingInLegacy',
  TITLE_MISMATCH = 'titleMismatch',
  PARENT_MISMATCH = 'parentMismatch',
  FOLDER_MEMBERSHIP_MISMATCH = 'folderMembershipMismatch',
  FILING_MISMATCH = 'filingMismatch',
  TAGS_MISMATCH = 'tagsMismatch',
  COLOR_MISMATCH = 'colorMismatch',
  ORDERING_MISMATCH = 'orderingMismatch',
  TRASH_STATE_MISMATCH = 'trashStateMismatch',
  OBJECT_ID_MISMATCH = 'objectIDMismatch',
  UNSUPPORTED_LEGACY_OBJECT = 'unsupportedLegacyObject',
  UNSUPPORTED_CANONICAL_OBJECT = 'unsupportedCanonicalObject',
  CONTENT_EXCLUDED = 'contentExcluded',
  PATH_LEAK_RISK = 'pathLeakRisk'
}

export class CanonicalLibraryMetadataReadSideDivergence {
  kind: CanonicalLibraryMetadataReadSideDivergenceKind
  objectID: string
  objectKind: CanonicalObjectKind | null
  field: string | null
  legacyValue: string | null
  canonicalValue: string | null
  fatal: boolean

  constructor(kind: CanonicalLibraryMetadataReadSideDivergenceKind, objectID: string, objectKind: CanonicalObjectKind | null = null, field: string | null = null, legacyValue: string | null = null, canonicalValue: string | null = null, fatal: boolean = false) {
    this.kind = kind
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'library-object')
    this.objectKind = objectKind
    this.field = CanonicalProductionRedaction.safeDiagnosticText(field)
    this.legacyValue = CanonicalProductionRedaction.safeDiagnosticText(legacyValue)
    this.canonicalValue = CanonicalProductionRedaction.safeDiagnosticText(canonicalValue)
    this.fatal = fatal || kind === CanonicalLibraryMetadataReadSideDivergenceKind.PATH_LEAK_RISK
  }

  get id(): string {
    return [this.kind, this.objectID, this.field ?? ''].join('|')
  }

  get isBlocking(): boolean {
    return this.kind !== CanonicalLibraryMetadataReadSideDivergenceKind.CONTENT_EXCLUDED
  }
}

// ── Read Side Diff Report ──

export class CanonicalLibraryMetadataReadSideEquivalence {
  equivalent: boolean = false
  folderCount: number = 0
  studyItemCount: number = 0
  standaloneNoteCount: number = 0
  contentExcludedCount: number = 0
  diagnosticsSummary: string = ''
}

export enum CanonicalLibraryMetadataReadSideBlocker {
  BLOCKING_DIVERGENCE = 'blockingDivergence',
  UNSUPPORTED_OBJECT = 'unsupportedObject',
  PATH_LEAK_RISK = 'pathLeakRisk',
  MISSING_LEGACY_SNAPSHOT = 'missingLegacySnapshot',
  MISSING_CANONICAL_SNAPSHOT = 'missingCanonicalSnapshot'
}

export class CanonicalLibraryMetadataReadSideDiffReport {
  equivalence: CanonicalLibraryMetadataReadSideEquivalence = new CanonicalLibraryMetadataReadSideEquivalence()
  divergences: CanonicalLibraryMetadataReadSideDivergence[] = []
  blockers: CanonicalLibraryMetadataReadSideBlocker[] = []
  legacySnapshotSummary: string = ''
  canonicalSnapshotSummary: string = ''
  diagnosticsSummary: string = ''

  get equivalent(): boolean {
    return this.blockers.length === 0 && this.divergences.every(d => !d.isBlocking)
  }

  get divergenceCount(): number {
    return this.divergences.filter(d => d.isBlocking).length
  }

  get unsupportedObjectCount(): number {
    return this.divergences.filter(d => d.kind === CanonicalLibraryMetadataReadSideDivergenceKind.UNSUPPORTED_LEGACY_OBJECT ||
      d.kind === CanonicalLibraryMetadataReadSideDivergenceKind.UNSUPPORTED_CANONICAL_OBJECT).length
  }

  get pathLeakRiskCount(): number {
    return this.divergences.filter(d => d.kind === CanonicalLibraryMetadataReadSideDivergenceKind.PATH_LEAK_RISK).length
  }
}

// ── Read Side Parallel Diff ──

export function compareReadSideParallelDiff(
  legacy: CanonicalLibraryMetadataReadSnapshot,
  canonical: CanonicalLibraryMetadataReadSnapshot
): CanonicalLibraryMetadataReadSideDiffReport {
  const divergences: CanonicalLibraryMetadataReadSideDivergence[] = []
  appendFailureDivergences(legacy, divergences)
  appendFailureDivergences(canonical, divergences)
  compareFolders(legacy.folders, canonical.folders, divergences)
  compareItems(legacy.studyItems, canonical.studyItems, divergences)
  compareNotes(legacy.standaloneNotes, canonical.standaloneNotes, divergences)

  const blockers: CanonicalLibraryMetadataReadSideBlocker[] = []
  if (legacy.failures.some(f => f.kind === CanonicalLibraryMetadataReadProjectionFailureKind.SNAPSHOT_MISSING)) blockers.push(CanonicalLibraryMetadataReadSideBlocker.MISSING_LEGACY_SNAPSHOT)
  if (canonical.failures.some(f => f.kind === CanonicalLibraryMetadataReadProjectionFailureKind.SNAPSHOT_MISSING)) blockers.push(CanonicalLibraryMetadataReadSideBlocker.MISSING_CANONICAL_SNAPSHOT)
  if (divergences.some(d => d.kind === CanonicalLibraryMetadataReadSideDivergenceKind.PATH_LEAK_RISK)) blockers.push(CanonicalLibraryMetadataReadSideBlocker.PATH_LEAK_RISK)
  if (divergences.some(d => d.kind === CanonicalLibraryMetadataReadSideDivergenceKind.UNSUPPORTED_LEGACY_OBJECT || d.kind === CanonicalLibraryMetadataReadSideDivergenceKind.UNSUPPORTED_CANONICAL_OBJECT)) blockers.push(CanonicalLibraryMetadataReadSideBlocker.UNSUPPORTED_OBJECT)
  if (divergences.some(d => d.isBlocking)) blockers.push(CanonicalLibraryMetadataReadSideBlocker.BLOCKING_DIVERGENCE)
  const uniqueBlockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
  const equivalent = uniqueBlockers.length === 0 && divergences.every(d => !d.isBlocking)
  const equivalence = new CanonicalLibraryMetadataReadSideEquivalence()
  equivalence.equivalent = equivalent
  equivalence.folderCount = canonical.folders.length
  equivalence.studyItemCount = canonical.studyItems.length
  equivalence.standaloneNoteCount = canonical.standaloneNotes.length
  equivalence.contentExcludedCount = canonical.contentExcludedCount + legacy.contentExcludedCount
  equivalence.diagnosticsSummary = 'equivalent=' + equivalent + ',folders=' + canonical.folders.length + ',items=' + canonical.studyItems.length + ',notes=' + canonical.standaloneNotes.length + ',contentExcluded=' + equivalence.contentExcludedCount
  const divergenceSummary = [...new Set(divergences.map(d => d.kind as string))].sort().join('+')
  return {
    equivalence,
    divergences: divergences.sort((a, b) => a.id.localeCompare(b.id)),
    blockers: uniqueBlockers,
    legacySnapshotSummary: legacy.diagnosticsSummary,
    canonicalSnapshotSummary: canonical.diagnosticsSummary,
    diagnosticsSummary: 'domain=libraryMetadata,equivalent=' + equivalent + ',divergences=' + divergences.filter(d => d.isBlocking).length + ',unsupported=' + divergences.filter(d => d.kind === CanonicalLibraryMetadataReadSideDivergenceKind.UNSUPPORTED_LEGACY_OBJECT || d.kind === CanonicalLibraryMetadataReadSideDivergenceKind.UNSUPPORTED_CANONICAL_OBJECT).length + ',pathLeakRisk=' + divergences.filter(d => d.kind === CanonicalLibraryMetadataReadSideDivergenceKind.PATH_LEAK_RISK).length + ',kinds=' + divergenceSummary
  }
}

function appendFailureDivergences(snapshot: CanonicalLibraryMetadataReadSnapshot, divergences: CanonicalLibraryMetadataReadSideDivergence[]): void {
  for (const failure of snapshot.failures) {
    switch (failure.kind) {
      case CanonicalLibraryMetadataReadProjectionFailureKind.PATH_LEAK_RISK:
        divergences.push(new CanonicalLibraryMetadataReadSideDivergence(CanonicalLibraryMetadataReadSideDivergenceKind.PATH_LEAK_RISK, failure.objectID ?? 'run', failure.objectKind, 'projection', snapshot.source, failure.reason, true))
        break
      case CanonicalLibraryMetadataReadProjectionFailureKind.UNSUPPORTED_OBJECT:
      case CanonicalLibraryMetadataReadProjectionFailureKind.MISSING_METADATA:
        divergences.push(new CanonicalLibraryMetadataReadSideDivergence(snapshot.source === CanonicalLibraryMetadataReadProjectionSource.LEGACY ? CanonicalLibraryMetadataReadSideDivergenceKind.UNSUPPORTED_LEGACY_OBJECT : CanonicalLibraryMetadataReadSideDivergenceKind.UNSUPPORTED_CANONICAL_OBJECT, failure.objectID ?? 'run', failure.objectKind, 'object', snapshot.source, failure.reason))
        break
      case CanonicalLibraryMetadataReadProjectionFailureKind.SNAPSHOT_MISSING:
        divergences.push(new CanonicalLibraryMetadataReadSideDivergence(snapshot.source === CanonicalLibraryMetadataReadProjectionSource.LEGACY ? CanonicalLibraryMetadataReadSideDivergenceKind.MISSING_IN_LEGACY : CanonicalLibraryMetadataReadSideDivergenceKind.MISSING_IN_CANONICAL, 'snapshot', null, 'snapshot', snapshot.source, failure.reason))
        break
      default: break
    }
  }
}

function compareFolders(legacy: CanonicalLibraryMetadataReadProjectionFolder[], canonical: CanonicalLibraryMetadataReadProjectionFolder[], divergences: CanonicalLibraryMetadataReadSideDivergence[]): void {
  const legMap: Record<string, CanonicalLibraryMetadataReadProjectionFolder> = {}
  for (const f of legacy) legMap[f.folderID.rawValue] = f
  const canMap: Record<string, CanonicalLibraryMetadataReadProjectionFolder> = {}
  for (const f of canonical) canMap[f.folderID.rawValue] = f
  const allIDs = [...new Set([...Object.keys(legMap), ...Object.keys(canMap)])].sort()
  for (const id of allIDs) {
    if (!legMap[id]) { divergences.push(new CanonicalLibraryMetadataReadSideDivergence(CanonicalLibraryMetadataReadSideDivergenceKind.MISSING_IN_LEGACY, id, CanonicalObjectKind.FOLDER)); continue }
    if (!canMap[id]) { divergences.push(new CanonicalLibraryMetadataReadSideDivergence(CanonicalLibraryMetadataReadSideDivergenceKind.MISSING_IN_CANONICAL, id, CanonicalObjectKind.FOLDER)); continue }
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.TITLE_MISMATCH, id, CanonicalObjectKind.FOLDER, 'title', legMap[id].title, canMap[id].title, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.PARENT_MISMATCH, id, CanonicalObjectKind.FOLDER, 'parentID', legMap[id].parentID?.rawValue ?? 'root', canMap[id].parentID?.rawValue ?? 'root', divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.PARENT_MISMATCH, id, CanonicalObjectKind.FOLDER, 'hierarchyPath', legMap[id].hierarchyKey, canMap[id].hierarchyKey, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.PARENT_MISMATCH, id, CanonicalObjectKind.FOLDER, 'hierarchyLevel', legMap[id].hierarchyLevel ?? 'none', canMap[id].hierarchyLevel ?? 'none', divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.COLOR_MISMATCH, id, CanonicalObjectKind.FOLDER, 'color', legMap[id].colorToken ?? 'none', canMap[id].colorToken ?? 'none', divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.ORDERING_MISMATCH, id, CanonicalObjectKind.FOLDER, 'ordering', legMap[id].orderingKey ?? 'none', canMap[id].orderingKey ?? 'none', divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.TRASH_STATE_MISMATCH, id, CanonicalObjectKind.FOLDER, 'trash', String(legMap[id].isDeleted), String(canMap[id].isDeleted), divergences)
  }
}

function compareItems(legacy: CanonicalLibraryMetadataReadProjectionItem[], canonical: CanonicalLibraryMetadataReadProjectionItem[], divergences: CanonicalLibraryMetadataReadSideDivergence[]): void {
  const legMap: Record<string, CanonicalLibraryMetadataReadProjectionItem> = {}
  for (const f of legacy) legMap[f.itemID.rawValue] = f
  const canMap: Record<string, CanonicalLibraryMetadataReadProjectionItem> = {}
  for (const f of canonical) canMap[f.itemID.rawValue] = f
  const allIDs = [...new Set([...Object.keys(legMap), ...Object.keys(canMap)])].sort()
  for (const id of allIDs) {
    if (!legMap[id]) { divergences.push(new CanonicalLibraryMetadataReadSideDivergence(CanonicalLibraryMetadataReadSideDivergenceKind.MISSING_IN_LEGACY, id, CanonicalObjectKind.STANDALONE_STUDY_ITEM)); continue }
    if (!canMap[id]) { divergences.push(new CanonicalLibraryMetadataReadSideDivergence(CanonicalLibraryMetadataReadSideDivergenceKind.MISSING_IN_CANONICAL, id, CanonicalObjectKind.STANDALONE_STUDY_ITEM)); continue }
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.TITLE_MISMATCH, id, CanonicalObjectKind.STANDALONE_STUDY_ITEM, 'title', legMap[id].title, canMap[id].title, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.FOLDER_MEMBERSHIP_MISMATCH, id, CanonicalObjectKind.STANDALONE_STUDY_ITEM, 'folders', legMap[id].folderIDKey, canMap[id].folderIDKey, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.PARENT_MISMATCH, id, CanonicalObjectKind.STANDALONE_STUDY_ITEM, 'parents', legMap[id].parentReferenceKey, canMap[id].parentReferenceKey, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.FILING_MISMATCH, id, CanonicalObjectKind.STANDALONE_STUDY_ITEM, 'filing', legMap[id].filingKey, canMap[id].filingKey, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.TAGS_MISMATCH, id, CanonicalObjectKind.STANDALONE_STUDY_ITEM, 'tags', legMap[id].tagKey, canMap[id].tagKey, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.OBJECT_ID_MISMATCH, id, CanonicalObjectKind.STANDALONE_STUDY_ITEM, 'itemKind', legMap[id].itemKind, canMap[id].itemKind, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.TRASH_STATE_MISMATCH, id, CanonicalObjectKind.STANDALONE_STUDY_ITEM, 'trash', String(legMap[id].isDeleted), String(canMap[id].isDeleted), divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.FOLDER_MEMBERSHIP_MISMATCH, id, CanonicalObjectKind.STANDALONE_STUDY_ITEM, 'resourceSummary', legMap[id].resourceTokenSummary, canMap[id].resourceTokenSummary, divergences)
  }
}

function compareNotes(legacy: CanonicalLibraryMetadataReadProjectionNote[], canonical: CanonicalLibraryMetadataReadProjectionNote[], divergences: CanonicalLibraryMetadataReadSideDivergence[]): void {
  const legMap: Record<string, CanonicalLibraryMetadataReadProjectionNote> = {}
  for (const f of legacy) legMap[f.noteItemID.rawValue] = f
  const canMap: Record<string, CanonicalLibraryMetadataReadProjectionNote> = {}
  for (const f of canonical) canMap[f.noteItemID.rawValue] = f
  const allIDs = [...new Set([...Object.keys(legMap), ...Object.keys(canMap)])].sort()
  for (const id of allIDs) {
    if (!legMap[id]) { divergences.push(new CanonicalLibraryMetadataReadSideDivergence(CanonicalLibraryMetadataReadSideDivergenceKind.MISSING_IN_LEGACY, id, CanonicalObjectKind.STANDALONE_NOTE)); continue }
    if (!canMap[id]) { divergences.push(new CanonicalLibraryMetadataReadSideDivergence(CanonicalLibraryMetadataReadSideDivergenceKind.MISSING_IN_CANONICAL, id, CanonicalObjectKind.STANDALONE_NOTE)); continue }
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.TITLE_MISMATCH, id, CanonicalObjectKind.STANDALONE_NOTE, 'title', legMap[id].title, canMap[id].title, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.FOLDER_MEMBERSHIP_MISMATCH, id, CanonicalObjectKind.STANDALONE_NOTE, 'folders', legMap[id].folderIDKey, canMap[id].folderIDKey, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.PARENT_MISMATCH, id, CanonicalObjectKind.STANDALONE_NOTE, 'parents', legMap[id].parentReferenceKey, canMap[id].parentReferenceKey, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.FILING_MISMATCH, id, CanonicalObjectKind.STANDALONE_NOTE, 'filing', legMap[id].filingKey, canMap[id].filingKey, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.TAGS_MISMATCH, id, CanonicalObjectKind.STANDALONE_NOTE, 'tags', legMap[id].tagKey, canMap[id].tagKey, divergences)
    appendMismatch(CanonicalLibraryMetadataReadSideDivergenceKind.TRASH_STATE_MISMATCH, id, CanonicalObjectKind.STANDALONE_NOTE, 'trash', String(legMap[id].isDeleted), String(canMap[id].isDeleted), divergences)
  }
}

function appendMismatch(kind: CanonicalLibraryMetadataReadSideDivergenceKind, objectID: string, objectKind: CanonicalObjectKind, field: string, legacyValue: string, canonicalValue: string, divergences: CanonicalLibraryMetadataReadSideDivergence[]): void {
  if (legacyValue !== canonicalValue) {
    divergences.push(new CanonicalLibraryMetadataReadSideDivergence(kind, objectID, objectKind, field, legacyValue, canonicalValue))
  }
}

// ── Write Side Evidence Linkage ──

export class CanonicalLibraryMetadataWriteSideEvidenceLinkage {
  canaryStageStatus: CanonicalLibraryMetadataStageEvidenceStatus = CanonicalLibraryMetadataStageEvidenceStatus.MISSING
  latestSuccessfulStage: CanonicalLibraryMetadataCanaryStage | null = null
  rollbackFailureCount: number = 0
  duplicateSuppressionCount: number = 0
  unresolvedConflictCount: number = 0
  resourceMoveBlockedCount: number = 0
  readSideDivergenceCount: number = 0
  writeSideDomainCutoverComplete: boolean = false

  constructor(
    canaryStageStatus: CanonicalLibraryMetadataStageEvidenceStatus = CanonicalLibraryMetadataStageEvidenceStatus.MISSING,
    latestSuccessfulStage: CanonicalLibraryMetadataCanaryStage | null = null,
    rollbackFailureCount: number = 0,
    duplicateSuppressionCount: number = 0,
    unresolvedConflictCount: number = 0,
    resourceMoveBlockedCount: number = 0,
    readSideDivergenceCount: number = 0,
    writeSideDomainCutoverComplete: boolean = false
  ) {
    this.canaryStageStatus = canaryStageStatus
    this.latestSuccessfulStage = latestSuccessfulStage
    this.rollbackFailureCount = Math.max(0, rollbackFailureCount)
    this.duplicateSuppressionCount = Math.max(0, duplicateSuppressionCount)
    this.unresolvedConflictCount = Math.max(0, unresolvedConflictCount)
    this.resourceMoveBlockedCount = Math.max(0, resourceMoveBlockedCount)
    this.readSideDivergenceCount = Math.max(0, readSideDivergenceCount)
    this.writeSideDomainCutoverComplete = writeSideDomainCutoverComplete
  }

  static readonly MISSING = new CanonicalLibraryMetadataWriteSideEvidenceLinkage()

  get hasCleanStagedCanaryEvidence(): boolean {
    return this.canaryStageStatus === CanonicalLibraryMetadataStageEvidenceStatus.PASSED &&
      this.latestSuccessfulStage !== null && this.rollbackFailureCount === 0 &&
      this.unresolvedConflictCount === 0 && this.resourceMoveBlockedCount === 0 &&
      this.readSideDivergenceCount === 0
  }

  get diagnosticsSummary(): string {
    return 'stageStatus=' + this.canaryStageStatus + ',latestStage=' + (this.latestSuccessfulStage ?? 'none') +
      ',rollbackFailures=' + this.rollbackFailureCount + ',duplicateSuppression=' + this.duplicateSuppressionCount +
      ',unresolvedConflicts=' + this.unresolvedConflictCount + ',resourceMoveBlocked=' + this.resourceMoveBlockedCount +
      ',readSideDivergence=' + this.readSideDivergenceCount + ',domainCutover=' + this.writeSideDomainCutoverComplete
  }
}
