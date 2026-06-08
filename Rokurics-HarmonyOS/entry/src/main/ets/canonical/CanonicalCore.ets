/**
 * CanonicalCore — canonical kernel type definitions for Rokurics-HarmonyOS.
 * Ported from Rokurics-Apple SyncCore.
 * Single-file kernel: all canonical types, contracts, and library objects.
 * Canonical is the authoritative truth for cross-device recording identity and sync.
 */

import { util } from '@kit.ArkTS'
import { cryptoFramework } from '@kit.CryptoArchitectureKit'

// =============================================================================
// SECTION 1: UTILITY FUNCTIONS
// =============================================================================

export function nilIfEmpty(value: string): string | null {
  return value.length === 0 ? null : value
}

export function normalizedRequired(value: string, fallback: string): string {
  const trimmed = value.trim()
  return nilIfEmpty(trimmed) ?? fallback
}

export function normalizedRequiredPreservingInput(value: string, fallback: string): string {
  return value.trim().length === 0 ? fallback : value
}

export function hashPrefix(value: string, length: number = 12): string {
  return value.trim().substring(0, length)
}

export function timestampString(timestamp: CanonicalTimestamp): string {
  return (timestamp.date.getTime() / 1000.0).toFixed(6)
}

export function stringToUint8Array(str: string): Uint8Array {
  const encoder = new util.TextEncoder()
  return encoder.encodeInto(str)
}

export function escapeJson(str: string): string {
  return str.replace(/\\/g, '\\\\')
    .replace(/"/g, '\\"')
    .replace(/\n/g, '\\n')
    .replace(/\r/g, '\\r')
    .replace(/\t/g, '\\t')
}

export function bufferToHex(data: ArrayBuffer): string {
  const bytes = new Uint8Array(data)
  let hex = ''
  for (let i = 0; i < bytes.length; i++) {
    hex += bytes[i].toString(16).padStart(2, '0')
  }
  return hex
}

// =============================================================================
// SECTION 2: CANONICAL TIMESTAMP & HASH
// =============================================================================

export class CanonicalTimestamp {
  date: Date

  constructor(date: Date) {
    this.date = date
  }

  compareTo(other: CanonicalTimestamp): number {
    return this.date.getTime() - other.date.getTime()
  }
}

export class CanonicalHash {
  algorithm: string
  value: string

  constructor(value: string, algorithm: string = 'sha256') {
    this.algorithm = algorithm.trim().toLowerCase()
    this.value = value.trim().toLowerCase()
  }

  static sha256OfString(value: string): CanonicalHash {
    return CanonicalHash.sha256(stringToUint8Array(value))
  }

  static sha256OfJson(value: Object): CanonicalHash {
    const json = JSON.stringify(value)
    return CanonicalHash.sha256(stringToUint8Array(json))
  }

  static sha256OfMap(map: Record<string, string>): CanonicalHash {
    const sortedKeys = Object.keys(map).sort()
    let json = '{'
    for (let i = 0; i < sortedKeys.length; i++) {
      if (i > 0) {
        json += ','
      }
      const key = sortedKeys[i]
      json += '"' + escapeJson(key) + '":"' + escapeJson(map[key] ?? '') + '"'
    }
    json += '}'
    return CanonicalHash.sha256(stringToUint8Array(json))
  }

  private static sha256(data: Uint8Array): CanonicalHash {
    const md = cryptoFramework.createMd('SHA256')
    md.update({ data: data.buffer as ArrayBuffer })
    const result = md.digest()
    const hex = bufferToHex(result.data)
    return new CanonicalHash(hex)
  }

  get shortCanonicalPrefix(): string {
    return hashPrefix(this.value)
  }

  equals(other: CanonicalHash): boolean {
    return this.algorithm === other.algorithm && this.value === other.value
  }
}

// =============================================================================
// SECTION 3: CANONICAL CAPABILITY
// =============================================================================

export enum CanonicalCapability {
  RECORDING_METADATA = 'recordingMetadata',
  AUDIO_ARTIFACT = 'audioArtifact',
  RECEIVE_RECORD = 'receiveRecord',
  TRANSCRIPT_ARTIFACT = 'transcriptArtifact',
  NOTE_ARTIFACT = 'noteArtifact',
  SUMMARY_ARTIFACT = 'summaryArtifact',
  OBJECT_PROJECTION = 'objectProjection',
  CANONICAL_LIBRARY_OBJECTS_V1 = 'canonicalLibraryObjectsV1',
  CANONICAL_FOLDER_OBJECTS_V1 = 'canonicalFolderObjectsV1',
  CANONICAL_STUDY_ITEM_OBJECTS_V1 = 'canonicalStudyItemObjectsV1',
  CANONICAL_TRANSFER_STATE_V1 = 'canonicalTransferStateV1',
  CANONICAL_OBJECT_PROJECTION_V1 = 'canonicalObjectProjectionV1',
  CANONICAL_INVENTORY_BUILDER_V1 = 'canonicalInventoryBuilderV1',
  CANONICAL_RETIREMENT_READINESS_V1 = 'canonicalRetirementReadinessV1'
}

// =============================================================================
// SECTION 4: CANONICAL NODE
// =============================================================================

export class CanonicalNode {
  nodeID: string
  platform: string
  displayName: string | null
  capabilities: CanonicalCapability[]

  constructor(
    nodeID: string,
    platform: string,
    displayName: string | null = null,
    capabilities: CanonicalCapability[] = []
  ) {
    this.nodeID = nodeID
    this.platform = platform
    this.displayName = nilIfEmpty((displayName ?? '').trim()) ?? null
    this.capabilities = [...capabilities].sort((a, b) => a.localeCompare(b))
  }

  get id(): string {
    return this.nodeID
  }
}

// =============================================================================
// SECTION 5: CANONICAL ARTIFACT PRODUCER
// =============================================================================

export enum CanonicalArtifactProducer {
  AUDIO_CAPTURE = 'audioCapture',
  TRANSCRIPTION = 'transcription',
  NOTE_GENERATION = 'noteGeneration',
  UNKNOWN = 'unknown'
}

// =============================================================================
// SECTION 6: CANONICAL ARTIFACT KINDS & AVAILABILITY
// =============================================================================

export enum CanonicalArtifactKind {
  AUDIO = 'audio',
  METADATA = 'metadata',
  RECEIVE_RECORD = 'receiveRecord',
  TRANSCRIPT_JSON = 'transcriptJSON',
  TRANSCRIPT_MARKDOWN = 'transcriptMarkdown',
  NOTE_MARKDOWN = 'noteMarkdown',
  NOTE_JSON = 'noteJSON',
  SUMMARY_JSON = 'summaryJSON'
}

export enum CanonicalArtifactAvailability {
  UNKNOWN = 'unknown',
  MISSING = 'missing',
  AVAILABLE_WITHOUT_HASH = 'availableWithoutHash',
  AVAILABLE = 'available'
}

// =============================================================================
// SECTION 7: CANONICAL PROJECTION CONTRACT (standalone exported functions)
// =============================================================================

export function generatedArtifactKinds(): CanonicalArtifactKind[] {
  return [
    CanonicalArtifactKind.TRANSCRIPT_JSON,
    CanonicalArtifactKind.TRANSCRIPT_MARKDOWN,
    CanonicalArtifactKind.NOTE_MARKDOWN,
    CanonicalArtifactKind.NOTE_JSON,
    CanonicalArtifactKind.SUMMARY_JSON
  ]
}

export function artifactID(objectID: string, kind: CanonicalArtifactKind): string {
  return kind + ':' + normalizedRequired(objectID, 'unknown-recording')
}

export function artifactKey(objectID: string, kind: CanonicalArtifactKind): string {
  return normalizedRequired(objectID, 'unknown-recording') + '|' + kind
}

export function makeCanonicalFolderID(folderID: string): CanonicalLibraryObjectID {
  return new CanonicalLibraryObjectID('folder:' + folderID)
}

export function makeCanonicalStudyItemID(itemID: string): CanonicalLibraryObjectID {
  return new CanonicalLibraryObjectID('studyItem:' + itemID)
}

export function normalizeFolderName(name: string): string {
  const trimmed = nilIfEmpty(name.trim())
  return trimmed ?? '未命名文件夹'
}

export function normalizeStudyItemTitle(title: string, itemKind?: CanonicalStudyItemKind): string {
  const trimmed = nilIfEmpty(title.trim())
  if (trimmed) {
    return title
  }
  if (itemKind === CanonicalStudyItemKind.STANDALONE_NOTE) {
    return '未命名笔记'
  }
  return '未命名条目'
}

export function normalizeTags(tags: string[]): string[] {
  const normalized = tags
    .map((t: string) => nilIfEmpty(t.trim().toLowerCase()))
    .filter((t: string | null): boolean => t !== null) as string[]
  return [...new Set(normalized)].sort()
}

export function normalizeFilingPath(path: CanonicalHierarchyPath): CanonicalHierarchyPath {
  return new CanonicalHierarchyPath(path.components)
}

export function normalizeParentReferences(
  references: CanonicalParentReference[]
): CanonicalParentReference[] {
  const seen = new Set<string>()
  const result: CanonicalParentReference[] = []
  for (const ref of references) {
    const key = ref.relation + '|' + ref.parentID.rawValue
    if (!seen.has(key)) {
      seen.add(key)
      result.push(ref)
    }
  }
  result.sort((a, b) => a.parentID.rawValue.localeCompare(b.parentID.rawValue))
  return result
}

export function normalizeTombstone(
  isDeleted: boolean,
  deletedAt: CanonicalTimestamp | null
): CanonicalTimestamp | null {
  if (!isDeleted) {
    return null
  }
  return deletedAt ?? new CanonicalTimestamp(new Date())
}

export function metadataHashPayloadForFolder(
  folder: CanonicalFolderMetadata
): Record<string, string> {
  return {
    'schema': 'canonical-folder-metadata-v1',
    'folderID': folder.folderID.rawValue,
    'name': folder.name,
    'parentID': folder.parentID?.rawValue ?? '',
    'hierarchyPath': folder.hierarchyPath.stableKey
  }
}

export function metadataHashPayloadForItem(
  item: CanonicalStudyItemMetadata
): Record<string, string> {
  return {
    'schema': 'canonical-study-item-metadata-v1',
    'itemID': item.itemID.rawValue,
    'title': item.title,
    'itemKind': item.itemKind,
    'associatedRecordingID': item.associatedRecordingID ?? ''
  }
}

export function objectKind(
  legacyItemKind: string,
  recordingID: string | null
): CanonicalObjectKind {
  const kind = legacyItemKind.trim().toLowerCase()
  if (kind === 'recording') {
    return CanonicalObjectKind.RECORDING
  }
  if (kind === 'folder') {
    return CanonicalObjectKind.FOLDER
  }
  if (kind === 'standalonenote' || kind === 'note') {
    return recordingID
      ? CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM
      : CanonicalObjectKind.STANDALONE_NOTE
  }
  if (kind === 'studyitem' || kind === 'studycourse') {
    return recordingID
      ? CanonicalObjectKind.RECORDING_ASSOCIATED_STUDY_ITEM
      : CanonicalObjectKind.STANDALONE_STUDY_ITEM
  }
  return CanonicalObjectKind.UNKNOWN_UNSUPPORTED
}

export function safeLogicalResourceToken(token: string | null): string | null {
  const trimmed = token?.trim()
  if (!trimmed || trimmed.length === 0 || trimmed.length > 1024) {
    return null
  }
  return trimmed
}

export function safeLogicalPathToken(token: string | null): string | null {
  const trimmed = token?.trim()
  if (!trimmed || trimmed.length === 0) {
    return null
  }
  if (trimmed.startsWith('/') || trimmed.includes('://') || trimmed.includes('\\')) {
    return null
  }
  const components = trimmed.split('/')
  if (components.length === 0) {
    return null
  }
  for (const c of components) {
    if (c.length === 0 || c === '.' || c === '..') {
      return null
    }
  }
  return trimmed
}

export function logicalNameFromToken(token: string | null): string | null {
  const safe = safeLogicalPathToken(token)
  if (!safe) {
    return null
  }
  const components = safe.split('/')
  return nilIfEmpty(components[components.length - 1]) ?? null
}

export function producerFor(
  kind: CanonicalArtifactKind,
  platform: string
): CanonicalArtifactProducer {
  if (kind === CanonicalArtifactKind.AUDIO) {
    return CanonicalArtifactProducer.AUDIO_CAPTURE
  }
  if (kind === CanonicalArtifactKind.TRANSCRIPT_JSON ||
    kind === CanonicalArtifactKind.TRANSCRIPT_MARKDOWN) {
    return CanonicalArtifactProducer.TRANSCRIPTION
  }
  if (kind === CanonicalArtifactKind.NOTE_MARKDOWN ||
    kind === CanonicalArtifactKind.NOTE_JSON ||
    kind === CanonicalArtifactKind.SUMMARY_JSON) {
    return CanonicalArtifactProducer.NOTE_GENERATION
  }
  return CanonicalArtifactProducer.UNKNOWN
}

export function requiredCapabilityFor(kind: CanonicalArtifactKind): CanonicalCapability | null {
  if (kind === CanonicalArtifactKind.AUDIO) {
    return CanonicalCapability.AUDIO_ARTIFACT
  }
  if (kind === CanonicalArtifactKind.METADATA) {
    return CanonicalCapability.RECORDING_METADATA
  }
  if (kind === CanonicalArtifactKind.RECEIVE_RECORD) {
    return CanonicalCapability.RECEIVE_RECORD
  }
  if (kind === CanonicalArtifactKind.TRANSCRIPT_JSON ||
    kind === CanonicalArtifactKind.TRANSCRIPT_MARKDOWN) {
    return CanonicalCapability.TRANSCRIPT_ARTIFACT
  }
  if (kind === CanonicalArtifactKind.NOTE_MARKDOWN ||
    kind === CanonicalArtifactKind.NOTE_JSON) {
    return CanonicalCapability.NOTE_ARTIFACT
  }
  if (kind === CanonicalArtifactKind.SUMMARY_JSON) {
    return CanonicalCapability.SUMMARY_ARTIFACT
  }
  return null
}

export function availabilityFrom(
  isPresent: boolean,
  contentHash: CanonicalHash | null,
  byteSize: number | null
): CanonicalArtifactAvailability {
  if (!isPresent) {
    return CanonicalArtifactAvailability.MISSING
  }
  if (contentHash !== null && byteSize !== null) {
    return CanonicalArtifactAvailability.AVAILABLE
  }
  return CanonicalArtifactAvailability.AVAILABLE_WITHOUT_HASH
}

export function makeArtifact(
  objectID: string,
  kind: CanonicalArtifactKind,
  availability: CanonicalArtifactAvailability,
  contentHash?: CanonicalHash | null,
  byteSize?: number | null,
  logicalPathToken?: string | null,
  modifiedAt?: CanonicalTimestamp | null,
  observedAt?: CanonicalTimestamp | null,
  producedByNodeID?: string | null,
  platform: string = 'harmonyos'
): CanonicalArtifact {
  return new CanonicalArtifact(
    artifactID(objectID, kind),
    objectID,
    kind,
    availability,
    contentHash ?? null,
    byteSize ?? null,
    null,
    logicalPathToken ?? null,
    modifiedAt ?? null,
    observedAt ?? null,
    producerFor(kind, platform),
    producedByNodeID ?? null,
    null
  )
}

export function provesGeneratedArtifactAvailability(
  artifact: CanonicalArtifact | null
): boolean {
  if (!artifact) {
    return false
  }
  if (generatedArtifactKinds().indexOf(artifact.kind) < 0) {
    return false
  }
  if (artifact.availability !== CanonicalArtifactAvailability.AVAILABLE) {
    return false
  }
  if (artifact.contentHash === null) {
    return false
  }
  if (artifact.byteSize === null) {
    return false
  }
  if (artifact.tombstone === true) {
    return false
  }
  return true
}

export function sameContent(left: CanonicalArtifact, right: CanonicalArtifact): boolean {
  const sameHash = left.contentHash !== null &&
    right.contentHash !== null &&
    left.contentHash.equals(right.contentHash)
  return sameHash && left.byteSize === right.byteSize
}

export function isAuthoritativeProducer(
  artifact: CanonicalArtifact,
  node: CanonicalNode
): boolean {
  if (!artifact.producedByNodeID) {
    return false
  }
  return artifact.producedByNodeID === node.nodeID
}

// =============================================================================
// SECTION 8: CANONICAL ARTIFACT & FACT
// =============================================================================

export class CanonicalArtifact {
  artifactID: string
  objectID: string
  kind: CanonicalArtifactKind
  availability: CanonicalArtifactAvailability
  contentHash: CanonicalHash | null
  byteSize: number | null
  logicalName: string | null
  logicalPathToken: string | null
  modifiedAt: CanonicalTimestamp | null
  observedAt: CanonicalTimestamp | null
  producedBy: CanonicalArtifactProducer | null
  producedByNodeID: string | null
  tombstone: boolean | null

  constructor(
    artifactID: string,
    objectID: string,
    kind: CanonicalArtifactKind,
    availability: CanonicalArtifactAvailability,
    contentHash: CanonicalHash | null = null,
    byteSize: number | null = null,
    logicalName: string | null = null,
    logicalPathToken: string | null = null,
    modifiedAt: CanonicalTimestamp | null = null,
    observedAt: CanonicalTimestamp | null = null,
    producedBy: CanonicalArtifactProducer | null = null,
    producedByNodeID: string | null = null,
    tombstone: boolean | null = null
  ) {
    this.artifactID = nilIfEmpty(artifactID.trim()) ?? artifactID(kind, objectID)
    this.objectID = objectID.trim()
    this.kind = kind
    this.availability = availability
    this.contentHash = contentHash
    this.byteSize = byteSize
    this.logicalName = nilIfEmpty((logicalName ?? '').trim()) ?? null
    this.logicalPathToken = safeLogicalPathToken(logicalPathToken)
    this.modifiedAt = modifiedAt
    this.observedAt = observedAt
    this.producedBy = producedBy
    this.producedByNodeID = nilIfEmpty((producedByNodeID ?? '').trim()) ?? null
    this.tombstone = tombstone
  }

  get id(): string {
    return this.artifactID
  }

  get provesCanonicalAudioAvailability(): boolean {
    return this.kind === CanonicalArtifactKind.AUDIO &&
      this.availability === CanonicalArtifactAvailability.AVAILABLE &&
      this.contentHash !== null &&
      this.byteSize !== null &&
      this.tombstone !== true
  }

  get provesCanonicalGeneratedArtifactAvailability(): boolean {
    return provesGeneratedArtifactAvailability(this)
  }

  get isCanonicalGeneratedArtifact(): boolean {
    return generatedArtifactKinds().indexOf(this.kind) >= 0
  }
}

export class CanonicalArtifactFact {
  kind: CanonicalArtifactKind
  availability: CanonicalArtifactAvailability
  contentHash: CanonicalHash | null
  byteSize: number | null
  logicalName: string | null
  logicalPathToken: string | null
  modifiedAt: CanonicalTimestamp | null
  observedAt: CanonicalTimestamp | null
  producedBy: CanonicalArtifactProducer | null
  producedByNodeID: string | null
  tombstone: boolean | null

  constructor(
    kind: CanonicalArtifactKind,
    availability: CanonicalArtifactAvailability,
    contentHash: CanonicalHash | null = null,
    byteSize: number | null = null,
    logicalName: string | null = null,
    logicalPathToken: string | null = null,
    modifiedAt: CanonicalTimestamp | null = null,
    observedAt: CanonicalTimestamp | null = null,
    producedBy: CanonicalArtifactProducer | null = null,
    producedByNodeID: string | null = null,
    tombstone: boolean | null = null
  ) {
    this.kind = kind
    this.availability = availability
    this.contentHash = contentHash
    this.byteSize = byteSize
    this.logicalName = nilIfEmpty((logicalName ?? '').trim()) ?? null
    this.logicalPathToken = safeLogicalPathToken(logicalPathToken)
    this.modifiedAt = modifiedAt
    this.observedAt = observedAt
    this.producedBy = producedBy
    this.producedByNodeID = nilIfEmpty((producedByNodeID ?? '').trim()) ?? null
    this.tombstone = tombstone
  }

  static audio(
    availability: CanonicalArtifactAvailability,
    contentHash: CanonicalHash | null = null,
    byteSize: number | null = null,
    logicalName: string | null = null,
    logicalPathToken: string | null = null,
    modifiedAt: CanonicalTimestamp | null = null,
    observedAt: CanonicalTimestamp | null = null,
    producedByNodeID: string | null = null
  ): CanonicalArtifactFact {
    return new CanonicalArtifactFact(
      CanonicalArtifactKind.AUDIO,
      availability,
      contentHash,
      byteSize,
      logicalName,
      logicalPathToken,
      modifiedAt,
      observedAt,
      CanonicalArtifactProducer.AUDIO_CAPTURE,
      producedByNodeID
    )
  }

  makeArtifact(
    objectID: string,
    fallbackProducedByNodeID: string | null = null
  ): CanonicalArtifact {
    return new CanonicalArtifact(
      artifactID(objectID, this.kind),
      objectID,
      this.kind,
      this.availability,
      this.contentHash,
      this.byteSize,
      this.logicalName,
      this.logicalPathToken,
      this.modifiedAt,
      this.observedAt,
      this.producedBy,
      this.producedByNodeID ?? fallbackProducedByNodeID,
      this.tombstone
    )
  }
}

// =============================================================================
// SECTION 9: STATE ENUMS
// =============================================================================

export enum CanonicalSyncState {
  UNKNOWN = 'unknown',
  LOCAL_ONLY = 'localOnly',
  SYNCED = 'synced',
  DIVERGED = 'diverged',
  DELETED = 'deleted',
  CONFLICT = 'conflict'
}

export enum CanonicalTransferState {
  NONE = 'none',
  QUEUED = 'queued',
  IN_FLIGHT = 'inFlight',
  RETRY_PENDING = 'retryPending',
  COMPLETED = 'completed',
  FAILED = 'failed',
  CONFLICT = 'conflict'
}

export enum CanonicalProcessingStage {
  NOT_STARTED = 'notStarted',
  QUEUED = 'queued',
  PROCESSING = 'processing',
  COMPLETED = 'completed',
  FAILED = 'failed',
  UNKNOWN = 'unknown'
}

export class CanonicalProcessingState {
  transcription: CanonicalProcessingStage
  note: CanonicalProcessingStage

  constructor(transcription: CanonicalProcessingStage, note: CanonicalProcessingStage) {
    this.transcription = transcription
    this.note = note
  }

  static readonly UNKNOWN = new CanonicalProcessingState(
    CanonicalProcessingStage.UNKNOWN,
    CanonicalProcessingStage.UNKNOWN
  )
}

// =============================================================================
// SECTION 10: CANONICAL RECORDING OBJECT
// =============================================================================

export class CanonicalRecordingObject {
  objectID: string
  nodeID: string | null
  metadata: CanonicalRecordingMetadata
  metadataHash: CanonicalHash
  artifacts: CanonicalArtifact[]
  syncState: CanonicalSyncState
  transferState: CanonicalTransferState
  processingState: CanonicalProcessingState
  receivedAt: CanonicalTimestamp | null
  observedAt: CanonicalTimestamp | null

  constructor(
    objectID: string,
    nodeID: string | null = null,
    metadata: CanonicalRecordingMetadata,
    artifacts: CanonicalArtifact[] = [],
    syncState: CanonicalSyncState = CanonicalSyncState.UNKNOWN,
    transferState: CanonicalTransferState = CanonicalTransferState.NONE,
    processingState: CanonicalProcessingState = CanonicalProcessingState.UNKNOWN,
    receivedAt: CanonicalTimestamp | null = null,
    observedAt: CanonicalTimestamp | null = null
  ) {
    this.objectID = nilIfEmpty(objectID.trim()) ?? metadata.objectID
    this.nodeID = nilIfEmpty((nodeID ?? '').trim()) ?? null
    this.metadata = metadata
    this.metadataHash = metadata.metadataHash
    this.artifacts = [...artifacts].sort((a, b) => a.artifactID.localeCompare(b.artifactID))
    this.syncState = syncState
    this.transferState = transferState
    this.processingState = processingState
    this.receivedAt = receivedAt
    this.observedAt = observedAt
  }

  get id(): string {
    return this.objectID
  }

  get audioArtifact(): CanonicalArtifact | null {
    for (const a of this.artifacts) {
      if (a.kind === CanonicalArtifactKind.AUDIO) {
        return a
      }
    }
    return null
  }

  get audioAvailable(): boolean {
    return this.audioArtifact?.provesCanonicalAudioAvailability === true
  }

  replacingArtifacts(newArtifacts: CanonicalArtifact[]): CanonicalRecordingObject {
    return new CanonicalRecordingObject(
      this.objectID,
      this.nodeID,
      this.metadata,
      newArtifacts,
      this.syncState,
      this.transferState,
      this.processingState,
      this.receivedAt,
      this.observedAt
    )
  }
}

// =============================================================================
// SECTION 11: CANONICAL RECORDING METADATA
// =============================================================================

export class CanonicalRecordingMetadata {
  static readonly BUSINESS_METADATA_HASH_SCHEMA_VERSION =
    'canonical-recording-business-metadata-v1'

  objectID: string
  title: string
  createdAt: CanonicalTimestamp
  modifiedAt: CanonicalTimestamp
  duration: number | null
  filing: CanonicalRecordingMetadata.Filing | null
  tags: string[]
  isDeleted: boolean
  deletedAt: CanonicalTimestamp | null

  constructor(
    objectID: string,
    title: string,
    createdAt: CanonicalTimestamp,
    modifiedAt: CanonicalTimestamp,
    duration: number | null = null,
    filing: CanonicalRecordingMetadata.Filing | null = null,
    tags: string[] = [],
    isDeleted: boolean = false,
    deletedAt: CanonicalTimestamp | null = null
  ) {
    this.objectID = normalizedRequired(objectID, 'unknown-recording')
    this.title = normalizedRequiredPreservingInput(title, '未命名录音')
    this.createdAt = createdAt
    this.modifiedAt = modifiedAt
    this.duration = duration
    this.filing = filing?.isEmpty ? null : filing
    this.tags = CanonicalRecordingMetadata.normalizedTags(tags)
    this.isDeleted = isDeleted
    this.deletedAt = deletedAt
  }

  get metadataHash(): CanonicalHash {
    return CanonicalHash.sha256OfMap({
      'schema': CanonicalRecordingMetadata.BUSINESS_METADATA_HASH_SCHEMA_VERSION,
      'objectID': this.objectID,
      'title': this.title,
      'filing.type': this.filing?.type ?? '',
      'filing.subject': this.filing?.subject ?? '',
      'filing.chapter': this.filing?.chapter ?? '',
      'filing.topic': this.filing?.topic ?? '',
      'tags': this.tags.join('\u001F'),
      'isDeleted': this.isDeleted ? 'true' : 'false',
      'deletedAt': this.deletedAt ? timestampString(this.deletedAt) : ''
    })
  }

  private static normalizedTags(tags: string[]): string[] {
    const normalized = tags
      .map((t: string) => nilIfEmpty(t.trim().toLowerCase()))
      .filter((t: string | null): boolean => t !== null) as string[]
    return [...new Set(normalized)].sort()
  }
}

export namespace CanonicalRecordingMetadata {
  export class Filing {
    type: string | null
    subject: string | null
    chapter: string | null
    topic: string | null

    constructor(
      type: string | null = null,
      subject: string | null = null,
      chapter: string | null = null,
      topic: string | null = null
    ) {
      this.type = Filing.normalized(type)
      this.subject = Filing.normalized(subject)
      this.chapter = Filing.normalized(chapter)
      this.topic = Filing.normalized(topic)
    }

    get isEmpty(): boolean {
      return this.type === null &&
        this.subject === null &&
        this.chapter === null &&
        this.topic === null
    }

    private static normalized(value: string | null): string | null {
      if (!value) {
        return null
      }
      return nilIfEmpty(value.trim())
    }
  }
}

// =============================================================================
// SECTION 12: LIBRARY OBJECT ID & KINDS
// =============================================================================

export enum CanonicalObjectKind {
  RECORDING = 'recording',
  FOLDER = 'folder',
  STANDALONE_STUDY_ITEM = 'standaloneStudyItem',
  STANDALONE_NOTE = 'standaloneNote',
  RECORDING_ASSOCIATED_STUDY_ITEM = 'recordingAssociatedStudyItem',
  GENERATED_ARTIFACT_ENVELOPE = 'generatedArtifactEnvelope',
  UNKNOWN_UNSUPPORTED = 'unknownUnsupported'
}

export enum CanonicalStudyItemKind {
  RECORDING_BUNDLE = 'recordingBundle',
  STANDALONE_NOTE = 'standaloneNote',
  EXTERNAL_RESOURCE = 'externalResource',
  UNKNOWN = 'unknown'
}

export class CanonicalLibraryObjectID {
  rawValue: string

  constructor(rawValue: string, fallback: string = 'unknownUnsupported:unknown') {
    this.rawValue = nilIfEmpty(rawValue.trim()) ?? fallback
  }
}

// =============================================================================
// SECTION 13: LIBRARY HIERARCHY TYPES
// =============================================================================

export class CanonicalParentReference {
  parentID: CanonicalLibraryObjectID
  relation: string

  constructor(parentID: CanonicalLibraryObjectID, relation: string = 'parent') {
    this.parentID = parentID
    this.relation = nilIfEmpty(relation.trim()) ?? 'parent'
  }
}

export class CanonicalHierarchyPath {
  components: string[]

  constructor(components: string[] = []) {
    this.components = components
      .map((c: string) => CanonicalHierarchyPath.normalized(c))
      .filter((c: string | null): boolean => c !== null) as string[]
  }

  get stableKey(): string {
    return this.components.join('\u001F')
  }

  private static normalized(value: string): string | null {
    return nilIfEmpty(value.trim())
  }
}

// =============================================================================
// SECTION 14: LIBRARY METADATA TYPES
// =============================================================================

export class CanonicalFolderMetadata {
  folderID: CanonicalLibraryObjectID
  name: string
  parentID: CanonicalLibraryObjectID | null
  hierarchyPath: CanonicalHierarchyPath
  hierarchyLevel: number
  colorToken: string | null
  orderingKey: string | null
  isDeleted: boolean
  deletedAt: CanonicalTimestamp | null
  businessModifiedAt: CanonicalTimestamp

  constructor(
    folderID: CanonicalLibraryObjectID,
    name: string,
    parentID: CanonicalLibraryObjectID | null = null,
    hierarchyPath: CanonicalHierarchyPath = new CanonicalHierarchyPath(),
    hierarchyLevel: number = 0,
    colorToken: string | null = null,
    orderingKey: string | null = null,
    isDeleted: boolean = false,
    deletedAt: CanonicalTimestamp | null = null,
    businessModifiedAt: CanonicalTimestamp = new CanonicalTimestamp(new Date())
  ) {
    this.folderID = folderID
    this.name = normalizeFolderName(name)
    this.parentID = parentID
    this.hierarchyPath = hierarchyPath
    this.hierarchyLevel = hierarchyLevel
    this.colorToken = nilIfEmpty((colorToken ?? '').trim()) ?? null
    this.orderingKey = nilIfEmpty((orderingKey ?? '').trim()) ?? null
    this.isDeleted = isDeleted
    this.deletedAt = normalizeTombstone(isDeleted, deletedAt)
    this.businessModifiedAt = businessModifiedAt
  }

  get metadataHash(): CanonicalHash {
    return CanonicalHash.sha256OfMap({
      'schema': 'canonical-folder-metadata-v1',
      'folderID': this.folderID.rawValue,
      'name': this.name,
      'parentID': this.parentID?.rawValue ?? '',
      'hierarchyPath': this.hierarchyPath.stableKey,
      'hierarchyLevel': this.hierarchyLevel.toString(),
      'colorToken': this.colorToken ?? '',
      'orderingKey': this.orderingKey ?? '',
      'isDeleted': this.isDeleted ? 'true' : 'false',
      'deletedAt': this.deletedAt ? timestampString(this.deletedAt) : '',
      'businessModifiedAt': timestampString(this.businessModifiedAt)
    })
  }
}

export class CanonicalStudyItemMetadata {
  itemID: CanonicalLibraryObjectID
  itemKind: CanonicalStudyItemKind
  title: string
  filingPath: CanonicalHierarchyPath
  folderIDs: CanonicalLibraryObjectID[]
  parentReferences: CanonicalParentReference[]
  tags: string[]
  logicalResourceTokens: string[]
  associatedRecordingID: string | null
  isDeleted: boolean
  deletedAt: CanonicalTimestamp | null
  businessModifiedAt: CanonicalTimestamp

  constructor(
    itemID: CanonicalLibraryObjectID,
    itemKind: CanonicalStudyItemKind,
    title: string,
    filingPath: CanonicalHierarchyPath = new CanonicalHierarchyPath(),
    folderIDs: CanonicalLibraryObjectID[] = [],
    parentReferences: CanonicalParentReference[] = [],
    tags: string[] = [],
    logicalResourceTokens: string[] = [],
    associatedRecordingID: string | null = null,
    isDeleted: boolean = false,
    deletedAt: CanonicalTimestamp | null = null,
    businessModifiedAt: CanonicalTimestamp = new CanonicalTimestamp(new Date())
  ) {
    this.itemID = itemID
    this.itemKind = itemKind
    this.title = normalizeStudyItemTitle(title, itemKind)
    this.filingPath = normalizeFilingPath(filingPath)
    this.folderIDs = [...folderIDs].sort(
      (a, b) => a.rawValue.localeCompare(b.rawValue)
    )
    this.parentReferences = normalizeParentReferences(parentReferences)
    this.tags = normalizeTags(tags)
    this.logicalResourceTokens = logicalResourceTokens
      .map((t: string) => safeLogicalResourceToken(t))
      .filter((t: string | null): boolean => t !== null) as string[]
    this.associatedRecordingID = nilIfEmpty((associatedRecordingID ?? '').trim()) ?? null
    this.isDeleted = isDeleted
    this.deletedAt = normalizeTombstone(isDeleted, deletedAt)
    this.businessModifiedAt = businessModifiedAt
  }

  get metadataHash(): CanonicalHash {
    return CanonicalHash.sha256OfMap({
      'schema': 'canonical-study-item-metadata-v1',
      'itemID': this.itemID.rawValue,
      'itemKind': this.itemKind,
      'title': this.title,
      'filingPath': this.filingPath.stableKey,
      'folderIDs': this.folderIDs.map((id: CanonicalLibraryObjectID) => id.rawValue)
        .join('\u001E'),
      'parentReferences': this.parentReferences
        .map((r: CanonicalParentReference) => r.relation + '\u001F' + r.parentID.rawValue)
        .join('\u001E'),
      'tags': this.tags.join('\u001F'),
      'logicalResourceTokens': this.logicalResourceTokens.join('\u001E'),
      'associatedRecordingID': this.associatedRecordingID ?? '',
      'isDeleted': this.isDeleted ? 'true' : 'false',
      'deletedAt': this.deletedAt ? timestampString(this.deletedAt) : '',
      'businessModifiedAt': timestampString(this.businessModifiedAt)
    })
  }
}

export class CanonicalLibraryMetadata {
  objectID: CanonicalLibraryObjectID
  objectKind: CanonicalObjectKind
  title: string
  metadataHash: CanonicalHash
  businessModifiedAt: CanonicalTimestamp | null
  isDeleted: boolean
  deletedAt: CanonicalTimestamp | null

  constructor(
    objectID: CanonicalLibraryObjectID,
    objectKind: CanonicalObjectKind,
    title: string,
    metadataHash: CanonicalHash,
    businessModifiedAt: CanonicalTimestamp | null = null,
    isDeleted: boolean = false,
    deletedAt: CanonicalTimestamp | null = null
  ) {
    this.objectID = objectID
    this.objectKind = objectKind
    this.title = nilIfEmpty(title.trim()) ?? 'unknown'
    this.metadataHash = metadataHash
    this.businessModifiedAt = businessModifiedAt
    this.isDeleted = isDeleted
    this.deletedAt = deletedAt
  }
}

// =============================================================================
// SECTION 15: LIBRARY OBJECT TYPES
// =============================================================================

export class CanonicalFolderObject {
  folderID: CanonicalLibraryObjectID
  metadata: CanonicalFolderMetadata
  metadataHash: CanonicalHash

  constructor(metadata: CanonicalFolderMetadata) {
    this.folderID = metadata.folderID
    this.metadata = metadata
    this.metadataHash = metadata.metadataHash
  }

  get id(): string {
    return this.folderID.rawValue
  }
}

export class CanonicalStudyItemObject {
  itemID: CanonicalLibraryObjectID
  metadata: CanonicalStudyItemMetadata
  metadataHash: CanonicalHash

  constructor(metadata: CanonicalStudyItemMetadata) {
    this.itemID = metadata.itemID
    this.metadata = metadata
    this.metadataHash = metadata.metadataHash
  }

  get id(): string {
    return this.itemID.rawValue
  }
}

export class CanonicalStandaloneNoteObject {
  noteID: CanonicalLibraryObjectID
  studyItem: CanonicalStudyItemObject
  metadataHash: CanonicalHash

  constructor(studyItem: CanonicalStudyItemObject) {
    this.noteID = studyItem.itemID
    this.studyItem = studyItem
    this.metadataHash = studyItem.metadataHash
  }

  get id(): string {
    return this.noteID.rawValue
  }
}

export class CanonicalRecordingEnvelopeObject {
  recordingID: string
  studyItemID: CanonicalLibraryObjectID | null
  folderIDs: CanonicalLibraryObjectID[]
  filingPath: CanonicalHierarchyPath
  tags: string[]

  constructor(
    recordingID: string,
    studyItemID: CanonicalLibraryObjectID | null = null,
    folderIDs: CanonicalLibraryObjectID[] = [],
    filingPath: CanonicalHierarchyPath = new CanonicalHierarchyPath(),
    tags: string[] = []
  ) {
    this.recordingID = normalizedRequired(recordingID, 'unknown-recording')
    this.studyItemID = studyItemID
    this.folderIDs = [...folderIDs].sort(
      (a, b) => a.rawValue.localeCompare(b.rawValue)
    )
    this.filingPath = filingPath
    this.tags = normalizeTags(tags)
  }

  get id(): string {
    return this.recordingID
  }
}

export class CanonicalLibraryObject {
  objectID: CanonicalLibraryObjectID
  kind: CanonicalObjectKind
  folder: CanonicalFolderObject | null
  studyItem: CanonicalStudyItemObject | null
  standaloneNote: CanonicalStandaloneNoteObject | null
  recordingEnvelope: CanonicalRecordingEnvelopeObject | null
  unsupportedReason: string | null

  constructor(
    objectID: CanonicalLibraryObjectID,
    kind: CanonicalObjectKind,
    folder: CanonicalFolderObject | null = null,
    studyItem: CanonicalStudyItemObject | null = null,
    standaloneNote: CanonicalStandaloneNoteObject | null = null,
    recordingEnvelope: CanonicalRecordingEnvelopeObject | null = null,
    unsupportedReason: string | null = null
  ) {
    this.objectID = objectID
    this.kind = kind
    this.folder = folder
    this.studyItem = studyItem
    this.standaloneNote = standaloneNote
    this.recordingEnvelope = recordingEnvelope
    this.unsupportedReason = nilIfEmpty((unsupportedReason ?? '').trim()) ?? null
  }

  get id(): string {
    return this.objectID.rawValue
  }

  get metadataHash(): CanonicalHash {
    if (this.folder) {
      return this.folder.metadataHash
    }
    if (this.studyItem) {
      return this.studyItem.metadataHash
    }
    if (this.standaloneNote) {
      return this.standaloneNote.metadataHash
    }
    if (this.recordingEnvelope) {
      return CanonicalHash.sha256OfString(this.recordingEnvelope.recordingID)
    }
    return CanonicalHash.sha256OfString(this.objectID.rawValue)
  }

  get businessModifiedAt(): CanonicalTimestamp | null {
    if (this.folder) {
      return this.folder.metadata.businessModifiedAt
    }
    if (this.studyItem) {
      return this.studyItem.metadata.businessModifiedAt
    }
    return null
  }

  get isDeleted(): boolean {
    if (this.folder) {
      return this.folder.metadata.isDeleted
    }
    if (this.studyItem) {
      return this.studyItem.metadata.isDeleted
    }
    return false
  }

  get deletedAt(): CanonicalTimestamp | null {
    if (this.folder) {
      return this.folder.metadata.deletedAt
    }
    if (this.studyItem) {
      return this.studyItem.metadata.deletedAt
    }
    return null
  }
}

// =============================================================================
// SECTION 16: LIBRARY TOMBSTONE & CONFLICT TYPES
// =============================================================================

export enum CanonicalLibraryTombstoneReason {
  SOFT_DELETE = 'softDelete',
  PEER_TOMBSTONE_NEWER = 'peerTombstoneNewer',
  LOCAL_TOMBSTONE_NEWER = 'localTombstoneNewer',
  ANTI_RESURRECTION = 'antiResurrection'
}

export enum CanonicalTombstonePolicy {
  SOFT_DELETE_ONLY = 'softDeleteOnly',
  ANTI_RESURRECTION = 'antiResurrection',
  NO_PHYSICAL_DELETE = 'noPhysicalDelete',
  NO_PERMANENT_DELETE = 'noPermanentDelete',
  NO_GARBAGE_COLLECTION = 'noGarbageCollection'
}

export class CanonicalLibraryTombstone {
  tombstoneID: string
  objectID: CanonicalLibraryObjectID
  objectKind: CanonicalObjectKind
  deletedAt: CanonicalTimestamp | null
  sourceNodeID: string | null
  reason: CanonicalLibraryTombstoneReason
  policies: CanonicalTombstonePolicy[]

  constructor(
    objectID: CanonicalLibraryObjectID,
    objectKind: CanonicalObjectKind,
    deletedAt: CanonicalTimestamp | null,
    sourceNodeID: string | null = null,
    reason: CanonicalLibraryTombstoneReason = CanonicalLibraryTombstoneReason.SOFT_DELETE,
    policies: CanonicalTombstonePolicy[] = [CanonicalTombstonePolicy.SOFT_DELETE_ONLY]
  ) {
    this.tombstoneID = objectKind + ':' + objectID.rawValue
    this.objectID = objectID
    this.objectKind = objectKind
    this.deletedAt = deletedAt
    this.sourceNodeID = nilIfEmpty((sourceNodeID ?? '').trim()) ?? null
    this.reason = reason
    this.policies = [...new Set(policies)].sort()
  }

  get id(): string {
    return this.tombstoneID
  }
}

export enum CanonicalLibraryConflictKind {
  FOLDER_METADATA_CONCURRENT_EDIT = 'folderMetadataConcurrentEdit',
  STUDY_ITEM_METADATA_CONCURRENT_EDIT = 'studyItemMetadataConcurrentEdit',
  ACTIVE_VS_TOMBSTONE = 'activeVsTombstone',
  RECORDING_ENVELOPE_METADATA_DISAGREEMENT = 'recordingEnvelopeMetadataDisagreement',
  UNSUPPORTED_LIBRARY_OBJECT = 'unsupportedLibraryObject'
}

export class CanonicalLibraryConflict {
  conflictID: string
  kind: CanonicalLibraryConflictKind
  objectID: CanonicalLibraryObjectID
  objectKind: CanonicalObjectKind
  localHashPrefix: string | null
  peerHashPrefix: string | null
  localModifiedAt: CanonicalTimestamp | null
  peerModifiedAt: CanonicalTimestamp | null
  detail: string | null

  constructor(
    kind: CanonicalLibraryConflictKind,
    objectID: CanonicalLibraryObjectID,
    objectKind: CanonicalObjectKind,
    localHash: CanonicalHash | null = null,
    peerHash: CanonicalHash | null = null,
    localModifiedAt: CanonicalTimestamp | null = null,
    peerModifiedAt: CanonicalTimestamp | null = null,
    detail: string | null = null
  ) {
    this.conflictID = kind + ':' + objectID.rawValue + ':' +
      (localHash?.shortCanonicalPrefix ?? '') + ':' +
      (peerHash?.shortCanonicalPrefix ?? '')
    this.kind = kind
    this.objectID = objectID
    this.objectKind = objectKind
    this.localHashPrefix = localHash?.shortCanonicalPrefix ?? null
    this.peerHashPrefix = peerHash?.shortCanonicalPrefix ?? null
    this.localModifiedAt = localModifiedAt ?? null
    this.peerModifiedAt = peerModifiedAt ?? null
    this.detail = nilIfEmpty((detail ?? '').trim()) ?? null
  }

  get id(): string {
    return this.conflictID
  }
}

// =============================================================================
// SECTION 17: CANONICAL MANIFEST
// =============================================================================

export class CanonicalManifest {
  static readonly CURRENT_SCHEMA_VERSION = 1

  schemaVersion: number
  node: CanonicalNode
  generatedAt: CanonicalTimestamp
  objects: CanonicalRecordingObject[]
  libraryObjects: CanonicalLibraryObject[]
  folders: CanonicalFolderObject[]
  studyItems: CanonicalStudyItemObject[]
  standaloneNotes: CanonicalStandaloneNoteObject[]
  libraryTombstones: CanonicalLibraryTombstone[]
  manifestCapabilities: CanonicalCapability[]
  manifestHash: CanonicalHash

  constructor(
    schemaVersion: number,
    node: CanonicalNode,
    generatedAt: CanonicalTimestamp,
    objects: CanonicalRecordingObject[],
    libraryObjects: CanonicalLibraryObject[] = [],
    folders: CanonicalFolderObject[] = [],
    studyItems: CanonicalStudyItemObject[] = [],
    standaloneNotes: CanonicalStandaloneNoteObject[] = [],
    libraryTombstones: CanonicalLibraryTombstone[] = [],
    manifestCapabilities: CanonicalCapability[] = [],
    manifestHash: CanonicalHash
  ) {
    this.schemaVersion = schemaVersion
    this.node = node
    this.generatedAt = generatedAt
    this.objects = [...objects].sort((a, b) => a.objectID.localeCompare(b.objectID))
    this.libraryObjects = [...libraryObjects].sort(
      (a, b) => a.objectID.rawValue.localeCompare(b.objectID.rawValue)
    )
    this.folders = [...folders].sort(
      (a, b) => a.folderID.rawValue.localeCompare(b.folderID.rawValue)
    )
    this.studyItems = [...studyItems].sort(
      (a, b) => a.itemID.rawValue.localeCompare(b.itemID.rawValue)
    )
    this.standaloneNotes = [...standaloneNotes].sort(
      (a, b) => a.noteID.rawValue.localeCompare(b.noteID.rawValue)
    )
    this.libraryTombstones = [...libraryTombstones].sort(
      (a, b) => a.tombstoneID.localeCompare(b.tombstoneID)
    )
    this.manifestCapabilities = [...new Set(manifestCapabilities)].sort(
      (a, b) => a.localeCompare(b)
    )
    this.manifestHash = manifestHash
  }

  static make(
    node: CanonicalNode,
    generatedAt: Date = new Date(),
    objects: CanonicalRecordingObject[],
    libraryObjects: CanonicalLibraryObject[] = [],
    folders: CanonicalFolderObject[] = [],
    studyItems: CanonicalStudyItemObject[] = [],
    standaloneNotes: CanonicalStandaloneNoteObject[] = [],
    libraryTombstones: CanonicalLibraryTombstone[] = [],
    manifestCapabilities: CanonicalCapability[] = []
  ): CanonicalManifest {
    const manifest = new CanonicalManifest(
      CanonicalManifest.CURRENT_SCHEMA_VERSION,
      node,
      new CanonicalTimestamp(generatedAt),
      objects,
      libraryObjects,
      folders,
      studyItems,
      standaloneNotes,
      libraryTombstones,
      manifestCapabilities,
      new CanonicalHash('')
    )
    manifest.manifestHash = manifest.computedManifestHash()
    return manifest
  }

  objectWithID(objectID: string): CanonicalRecordingObject | null {
    for (const o of this.objects) {
      if (o.objectID === objectID) {
        return o
      }
    }
    return null
  }

  computedManifestHash(): CanonicalHash {
    return CanonicalHash.sha256OfMap({
      'schemaVersion': this.schemaVersion.toString(),
      'nodeID': this.node.nodeID,
      'nodePlatform': this.node.platform,
      'nodeCapabilities': this.node.capabilities.join('\u001F'),
      'manifestCapabilities': this.manifestCapabilities.join('\u001F'),
      'generatedAt': CanonicalManifest.composeTimestampString(this.generatedAt),
      'objects': this.objects
        .map((o: CanonicalRecordingObject) => CanonicalManifest.objectHashSummary(o))
        .join('\u001E'),
      'libraryObjects': this.libraryObjects
        .map((o: CanonicalLibraryObject) => CanonicalManifest.libraryObjectHashSummary(o))
        .join('\u001E'),
      'folders': this.folders
        .map((f: CanonicalFolderObject) => f.metadataHash.value)
        .join('\u001E'),
      'studyItems': this.studyItems
        .map((s: CanonicalStudyItemObject) => s.metadataHash.value)
        .join('\u001E'),
      'standaloneNotes': this.standaloneNotes
        .map((s: CanonicalStandaloneNoteObject) => s.metadataHash.value)
        .join('\u001E'),
      'libraryTombstones': this.libraryTombstones
        .map((t: CanonicalLibraryTombstone) =>
          CanonicalManifest.libraryTombstoneHashSummary(t))
        .join('\u001E')
    })
  }

  get hasValidManifestHash(): boolean {
    const computed = this.computedManifestHash()
    return this.manifestHash.equals(computed)
  }

  private static objectHashSummary(obj: CanonicalRecordingObject): string {
    const artifactsPart = obj.artifacts.map((a: CanonicalArtifact) => {
      return [
        a.artifactID,
        a.kind,
        a.availability,
        a.contentHash?.value ?? '',
        a.byteSize?.toString() ?? ''
      ].join('\u001F')
    }).join('\u001D')
    return [
      obj.objectID,
      obj.metadataHash.value,
      obj.metadataHash.algorithm,
      obj.syncState,
      obj.transferState,
      obj.metadata.isDeleted ? 'deleted' : 'active',
      artifactsPart
    ].join('\u001F')
  }

  private static libraryObjectHashSummary(obj: CanonicalLibraryObject): string {
    return [
      obj.objectID.rawValue,
      obj.kind,
      obj.metadataHash.value,
      obj.isDeleted ? 'deleted' : 'active',
      obj.businessModifiedAt
        ? CanonicalManifest.composeTimestampString(obj.businessModifiedAt)
        : ''
    ].join('\u001F')
  }

  private static libraryTombstoneHashSummary(
    tombstone: CanonicalLibraryTombstone
  ): string {
    return [
      tombstone.tombstoneID,
      tombstone.objectID.rawValue,
      tombstone.objectKind,
      tombstone.deletedAt
        ? CanonicalManifest.composeTimestampString(tombstone.deletedAt)
        : '',
      tombstone.reason
    ].join('\u001F')
  }

  private static composeTimestampString(ts: CanonicalTimestamp): string {
    return (ts.date.getTime() / 1000.0).toFixed(6)
  }
}

// =============================================================================
// SECTION 18: CONFLICT REASONS & DECISIONS
// =============================================================================

export enum ConflictReason {
  OBJECT_IDENTITY_COLLISION = 'objectIdentityCollision',
  METADATA_MODIFIED_ON_BOTH_SIDES = 'metadataModifiedOnBothSides',
  ARTIFACT_HASH_MISMATCH = 'artifactHashMismatch',
  ARTIFACT_SIZE_MISMATCH = 'artifactSizeMismatch',
  ARTIFACT_UNAVAILABLE_MISMATCH = 'artifactUnavailableMismatch'
}

export enum SyncDecisionKind {
  NO_OP = 'noOp',
  UPLOAD_METADATA = 'uploadMetadata',
  DOWNLOAD_METADATA = 'downloadMetadata',
  DEFER_UNTIL_PEER_KNOWN = 'deferUntilPeerKnown',
  CONFLICT = 'conflict'
}

export class SyncDecision {
  kind: SyncDecisionKind
  objectID: string
  reason: string
  conflictReason: ConflictReason | null

  constructor(
    kind: SyncDecisionKind,
    objectID: string,
    reason: string,
    conflictReason: ConflictReason | null = null
  ) {
    this.kind = kind
    this.objectID = objectID
    this.reason = reason
    this.conflictReason = conflictReason
  }

  static metadata(
    local: CanonicalRecordingObject,
    peer: CanonicalRecordingObject | null
  ): SyncDecision {
    if (!peer) {
      return new SyncDecision(
        SyncDecisionKind.UPLOAD_METADATA,
        local.objectID,
        'peer_missing_metadata'
      )
    }
    if (SyncDecision.sameHash(local.metadataHash, peer.metadataHash)) {
      return new SyncDecision(
        SyncDecisionKind.NO_OP,
        local.objectID,
        'metadata_hash_equal'
      )
    }
    if (local.metadata.modifiedAt.date.getTime() >
      peer.metadata.modifiedAt.date.getTime()) {
      return new SyncDecision(
        SyncDecisionKind.UPLOAD_METADATA,
        local.objectID,
        'local_metadata_newer'
      )
    }
    if (peer.metadata.modifiedAt.date.getTime() >
      local.metadata.modifiedAt.date.getTime()) {
      return new SyncDecision(
        SyncDecisionKind.DOWNLOAD_METADATA,
        local.objectID,
        'peer_metadata_newer'
      )
    }
    return new SyncDecision(
      SyncDecisionKind.CONFLICT,
      local.objectID,
      'metadata_hash_mismatch_same_modified_at',
      ConflictReason.METADATA_MODIFIED_ON_BOTH_SIDES
    )
  }

  private static sameHash(left: CanonicalHash, right: CanonicalHash): boolean {
    return left.algorithm === right.algorithm && left.value === right.value
  }
}

export enum TransferDecisionKind {
  NO_OP = 'noOp',
  UPLOAD = 'upload',
  DOWNLOAD = 'download',
  DEFER_UNTIL_PEER_KNOWN = 'deferUntilPeerKnown',
  CONFLICT = 'conflict',
  LOCAL_UNAVAILABLE = 'localUnavailable'
}

export class TransferDecision {
  kind: TransferDecisionKind
  objectID: string
  artifactID: string | null
  reason: string
  conflictReason: ConflictReason | null

  constructor(
    kind: TransferDecisionKind,
    objectID: string,
    artifactID: string | null,
    reason: string,
    conflictReason: ConflictReason | null = null
  ) {
    this.kind = kind
    this.objectID = objectID
    this.artifactID = artifactID
    this.reason = reason
    this.conflictReason = conflictReason
  }

  static audio(
    local: CanonicalRecordingObject,
    peer: CanonicalRecordingObject | null
  ): TransferDecision {
    const localAudio = local.audioArtifact
    const peerAudio = peer?.audioArtifact ?? null
    const artifactID = localAudio?.artifactID ?? peerAudio?.artifactID ?? null

    if (!localAudio || !localAudio.provesCanonicalAudioAvailability) {
      return new TransferDecision(
        TransferDecisionKind.LOCAL_UNAVAILABLE,
        local.objectID,
        artifactID,
        'local_audio_unproven'
      )
    }
    if (!peer) {
      return new TransferDecision(
        TransferDecisionKind.DEFER_UNTIL_PEER_KNOWN,
        local.objectID,
        artifactID,
        'peer_unknown_is_not_missing'
      )
    }
    if (!peerAudio || peerAudio.availability === CanonicalArtifactAvailability.MISSING) {
      return new TransferDecision(
        TransferDecisionKind.UPLOAD,
        local.objectID,
        artifactID,
        'peer_audio_missing'
      )
    }
    if (!peerAudio.provesCanonicalAudioAvailability) {
      return new TransferDecision(
        TransferDecisionKind.DEFER_UNTIL_PEER_KNOWN,
        local.objectID,
        artifactID,
        'peer_audio_unproven'
      )
    }
    if (!TransferDecision.sameHash(localAudio.contentHash, peerAudio.contentHash)) {
      return new TransferDecision(
        TransferDecisionKind.CONFLICT,
        local.objectID,
        artifactID,
        'audio_hash_mismatch',
        ConflictReason.ARTIFACT_HASH_MISMATCH
      )
    }
    if (localAudio.byteSize !== peerAudio.byteSize) {
      return new TransferDecision(
        TransferDecisionKind.CONFLICT,
        local.objectID,
        artifactID,
        'audio_size_mismatch',
        ConflictReason.ARTIFACT_SIZE_MISMATCH
      )
    }
    return new TransferDecision(
      TransferDecisionKind.NO_OP,
      local.objectID,
      artifactID,
      'peer_audio_same_hash_and_size'
    )
  }

  private static sameHash(
    left: CanonicalHash | null,
    right: CanonicalHash | null
  ): boolean {
    return left?.algorithm === right?.algorithm && left?.value === right?.value
  }
}

// =============================================================================
// SECTION 19: OBJECT PROJECTION
// =============================================================================

export class ObjectProjection {
  objectID: string
  displayTitle: string
  metadataHash: CanonicalHash
  audioAvailable: boolean
  syncState: CanonicalSyncState
  transferState: CanonicalTransferState
  processingState: CanonicalProcessingState
  conflictReasons: ConflictReason[]

  constructor(
    objectID: string,
    displayTitle: string,
    metadataHash: CanonicalHash,
    audioAvailable: boolean,
    syncState: CanonicalSyncState,
    transferState: CanonicalTransferState,
    processingState: CanonicalProcessingState,
    conflictReasons: ConflictReason[]
  ) {
    this.objectID = objectID
    this.displayTitle = displayTitle
    this.metadataHash = metadataHash
    this.audioAvailable = audioAvailable
    this.syncState = syncState
    this.transferState = transferState
    this.processingState = processingState
    this.conflictReasons = conflictReasons
  }

  get id(): string {
    return this.objectID
  }

  static make(
    obj: CanonicalRecordingObject,
    conflictReasons: ConflictReason[] = []
  ): ObjectProjection {
    return new ObjectProjection(
      obj.objectID,
      obj.metadata.title,
      obj.metadataHash,
      obj.audioAvailable,
      obj.syncState,
      obj.transferState,
      obj.processingState,
      conflictReasons
    )
  }
}

// =============================================================================
// SECTION 20: RECORDING EXISTENCE TYPES
// =============================================================================

export enum CanonicalRecordingExistenceState {
  ABSENT = 'absent',
  METADATA_ONLY = 'metadataOnly',
  RECEIVE_RECORD_ONLY = 'receiveRecordOnly',
  STUDY_ITEM_ONLY = 'studyItemOnly',
  METADATA_AND_STUDY_ITEM = 'metadataAndStudyItem',
  AUDIO_AVAILABLE = 'audioAvailable',
  AUDIO_HASH_SIZE_MATCHED = 'audioHashSizeMatched',
  AUDIO_CONFLICT = 'audioConflict',
  PEER_UNKNOWN = 'peerUnknown',
  TOMBSTONED = 'tombstoned',
  UNSUPPORTED = 'unsupported'
}

export function isAudioProof(state: CanonicalRecordingExistenceState): boolean {
  return state === CanonicalRecordingExistenceState.AUDIO_AVAILABLE ||
    state === CanonicalRecordingExistenceState.AUDIO_HASH_SIZE_MATCHED
}

export enum CanonicalRecordingExistenceSource {
  CANONICAL_MANIFEST = 'canonicalManifest',
  STUDY_LIBRARY_MANIFEST = 'studyLibraryManifest',
  LOCAL_INVENTORY = 'localInventory',
  PEER_INVENTORY = 'peerInventory',
  RECORDING_METADATA = 'recordingMetadata',
  RECEIVE_RECORD = 'receiveRecord',
  STUDY_ITEM = 'studyItem',
  AUDIO_ARTIFACT = 'audioArtifact',
  COMPLETED_UPLOAD_LEDGER = 'completedUploadLedger',
  CANONICAL_EXISTENCE_LEDGER = 'canonicalExistenceLedger'
}

export enum CanonicalRecordingExistenceDecision {
  NO_OP = 'noOp',
  APPLY_METADATA_ONLY_BRIDGE = 'applyMetadataOnlyBridge',
  UPLOAD_AUDIO_CANDIDATE = 'uploadAudioCandidate',
  AUDIO_SAME_NO_OP = 'audioSameNoOp',
  CONFLICT = 'conflict',
  DEFERRED = 'deferred',
  BLOCKED = 'blocked',
  UNSUPPORTED = 'unsupported'
}

export enum CanonicalRecordingExistenceBlocker {
  TOMBSTONED_PARENT = 'tombstonedParent',
  PEER_UNKNOWN = 'peerUnknown',
  MISSING_LOCAL_AUDIO = 'missingLocalAudio',
  LOCAL_AUDIO_UNPROVEN = 'localAudioUnproven',
  PEER_AUDIO_UNPROVEN = 'peerAudioUnproven',
  AUDIO_HASH_MISMATCH = 'audioHashMismatch',
  AUDIO_SIZE_MISMATCH = 'audioSizeMismatch',
  COMPLETED_LEDGER_NOT_AUDIO_PROOF = 'completedLedgerNotAudioProof',
  METADATA_ONLY_NOT_AUDIO_PROOF = 'metadataOnlyNotAudioProof',
  RECEIVE_RECORD_NOT_AUDIO_PROOF = 'receiveRecordNotAudioProof',
  STUDY_ITEM_NOT_AUDIO_PROOF = 'studyItemNotAudioProof',
  UNSUPPORTED_OBJECT = 'unsupportedObject'
}

export class CanonicalRecordingExistenceTruth {
  objectID: string
  localState: CanonicalRecordingExistenceState
  peerState: CanonicalRecordingExistenceState
  decision: CanonicalRecordingExistenceDecision
  sources: CanonicalRecordingExistenceSource[]
  blockers: CanonicalRecordingExistenceBlocker[]
  localMetadataHashPrefix: string | null
  peerMetadataHashPrefix: string | null
  localAudioHashPrefix: string | null
  peerAudioHashPrefix: string | null
  localByteSize: number | null
  peerByteSize: number | null

  constructor(
    objectID: string,
    localState: CanonicalRecordingExistenceState,
    peerState: CanonicalRecordingExistenceState,
    decision: CanonicalRecordingExistenceDecision,
    sources: CanonicalRecordingExistenceSource[],
    blockers: CanonicalRecordingExistenceBlocker[],
    localMetadataHashPrefix: string | null,
    peerMetadataHashPrefix: string | null,
    localAudioHashPrefix: string | null,
    peerAudioHashPrefix: string | null,
    localByteSize: number | null,
    peerByteSize: number | null
  ) {
    this.objectID = objectID
    this.localState = localState
    this.peerState = peerState
    this.decision = decision
    this.sources = [...sources].sort()
    this.blockers = [...new Set(blockers)].sort()
    this.localMetadataHashPrefix = localMetadataHashPrefix
    this.peerMetadataHashPrefix = peerMetadataHashPrefix
    this.localAudioHashPrefix = localAudioHashPrefix
    this.peerAudioHashPrefix = peerAudioHashPrefix
    this.localByteSize = localByteSize
    this.peerByteSize = peerByteSize
  }

  get peerAudioAvailable(): boolean {
    return isAudioProof(this.peerState)
  }

  get shouldCreateUploadCandidate(): boolean {
    return this.decision === CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE
  }

  get requiresMetadataApplyBridge(): boolean {
    return this.decision === CanonicalRecordingExistenceDecision.APPLY_METADATA_ONLY_BRIDGE
  }

  static evaluate(
    objectID: string,
    local: CanonicalRecordingObject | null,
    peer: CanonicalRecordingObject | null,
    peerKnown: boolean = true,
    peerStudyItemExists: boolean = false,
    peerReceiveRecordExists: boolean = false,
    peerCompletedLedgerOnly: boolean = false,
    tombstonedParent: boolean = false
  ): CanonicalRecordingExistenceTruth {
    const normalizedObjectID = objectID.trim()
    const sources = new Set<CanonicalRecordingExistenceSource>()
    sources.add(CanonicalRecordingExistenceSource.CANONICAL_MANIFEST)
    if (local !== null) {
      sources.add(CanonicalRecordingExistenceSource.LOCAL_INVENTORY)
      sources.add(CanonicalRecordingExistenceSource.RECORDING_METADATA)
    }
    if (peer !== null) {
      sources.add(CanonicalRecordingExistenceSource.PEER_INVENTORY)
      sources.add(CanonicalRecordingExistenceSource.RECORDING_METADATA)
    }
    if (peerStudyItemExists) {
      sources.add(CanonicalRecordingExistenceSource.STUDY_ITEM)
    }
    if (peerReceiveRecordExists) {
      sources.add(CanonicalRecordingExistenceSource.RECEIVE_RECORD)
    }
    if (peerCompletedLedgerOnly) {
      sources.add(CanonicalRecordingExistenceSource.COMPLETED_UPLOAD_LEDGER)
    }

    const localState = CanonicalRecordingExistenceTruth.existenceState(
      local, true, false, false, false, tombstonedParent
    )
    let peerState = CanonicalRecordingExistenceTruth.existenceState(
      peer, peerKnown, peerStudyItemExists, peerReceiveRecordExists,
      peerCompletedLedgerOnly, tombstonedParent
    )
    const blockers = new Set<CanonicalRecordingExistenceBlocker>()

    if (tombstonedParent) {
      blockers.add(CanonicalRecordingExistenceBlocker.TOMBSTONED_PARENT)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState,
        CanonicalRecordingExistenceState.TOMBSTONED,
        CanonicalRecordingExistenceDecision.BLOCKED,
        sources, blockers
      )
    }

    if (!peerKnown) {
      blockers.add(CanonicalRecordingExistenceBlocker.PEER_UNKNOWN)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState,
        CanonicalRecordingExistenceState.PEER_UNKNOWN,
        CanonicalRecordingExistenceDecision.DEFERRED,
        sources, blockers
      )
    }

    const localAudio = local?.audioArtifact ?? null
    if (!localAudio) {
      blockers.add(CanonicalRecordingExistenceBlocker.MISSING_LOCAL_AUDIO)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        peerState === CanonicalRecordingExistenceState.ABSENT
          ? CanonicalRecordingExistenceDecision.APPLY_METADATA_ONLY_BRIDGE
          : CanonicalRecordingExistenceDecision.NO_OP,
        sources, blockers
      )
    }

    if (!localAudio.provesCanonicalAudioAvailability) {
      blockers.add(CanonicalRecordingExistenceBlocker.LOCAL_AUDIO_UNPROVEN)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        CanonicalRecordingExistenceDecision.BLOCKED,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.ABSENT) {
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        CanonicalRecordingExistenceDecision.APPLY_METADATA_ONLY_BRIDGE,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.METADATA_ONLY) {
      blockers.add(CanonicalRecordingExistenceBlocker.METADATA_ONLY_NOT_AUDIO_PROOF)
      if (peerCompletedLedgerOnly) {
        blockers.add(CanonicalRecordingExistenceBlocker.COMPLETED_LEDGER_NOT_AUDIO_PROOF)
      }
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.RECEIVE_RECORD_ONLY) {
      blockers.add(CanonicalRecordingExistenceBlocker.RECEIVE_RECORD_NOT_AUDIO_PROOF)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.STUDY_ITEM_ONLY) {
      blockers.add(CanonicalRecordingExistenceBlocker.STUDY_ITEM_NOT_AUDIO_PROOF)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.METADATA_AND_STUDY_ITEM) {
      blockers.add(CanonicalRecordingExistenceBlocker.METADATA_ONLY_NOT_AUDIO_PROOF)
      blockers.add(CanonicalRecordingExistenceBlocker.STUDY_ITEM_NOT_AUDIO_PROOF)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.AUDIO_AVAILABLE) {
      const peerAudio = peer?.audioArtifact ?? null
      if (!peerAudio || !peerAudio.provesCanonicalAudioAvailability) {
        blockers.add(CanonicalRecordingExistenceBlocker.PEER_AUDIO_UNPROVEN)
        return CanonicalRecordingExistenceTruth.truth(
          normalizedObjectID, local, peer, localState,
          CanonicalRecordingExistenceState.UNSUPPORTED,
          CanonicalRecordingExistenceDecision.DEFERRED,
          sources, blockers
        )
      }
      if (!localAudio.contentHash || !peerAudio.contentHash ||
        localAudio.contentHash.value !== peerAudio.contentHash.value) {
        blockers.add(CanonicalRecordingExistenceBlocker.AUDIO_HASH_MISMATCH)
        peerState = CanonicalRecordingExistenceState.AUDIO_CONFLICT
        return CanonicalRecordingExistenceTruth.truth(
          normalizedObjectID, local, peer, localState, peerState,
          CanonicalRecordingExistenceDecision.CONFLICT,
          sources, blockers
        )
      }
      if (localAudio.byteSize !== peerAudio.byteSize) {
        blockers.add(CanonicalRecordingExistenceBlocker.AUDIO_SIZE_MISMATCH)
        peerState = CanonicalRecordingExistenceState.AUDIO_CONFLICT
        return CanonicalRecordingExistenceTruth.truth(
          normalizedObjectID, local, peer, localState, peerState,
          CanonicalRecordingExistenceDecision.CONFLICT,
          sources, blockers
        )
      }
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState,
        CanonicalRecordingExistenceState.AUDIO_HASH_SIZE_MATCHED,
        CanonicalRecordingExistenceDecision.AUDIO_SAME_NO_OP,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.AUDIO_HASH_SIZE_MATCHED) {
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState,
        CanonicalRecordingExistenceState.AUDIO_HASH_SIZE_MATCHED,
        CanonicalRecordingExistenceDecision.AUDIO_SAME_NO_OP,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.AUDIO_CONFLICT) {
      blockers.add(CanonicalRecordingExistenceBlocker.AUDIO_HASH_MISMATCH)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        CanonicalRecordingExistenceDecision.CONFLICT,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.PEER_UNKNOWN) {
      blockers.add(CanonicalRecordingExistenceBlocker.PEER_UNKNOWN)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        CanonicalRecordingExistenceDecision.DEFERRED,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.TOMBSTONED) {
      blockers.add(CanonicalRecordingExistenceBlocker.TOMBSTONED_PARENT)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        CanonicalRecordingExistenceDecision.BLOCKED,
        sources, blockers
      )
    }

    if (peerState === CanonicalRecordingExistenceState.UNSUPPORTED) {
      blockers.add(CanonicalRecordingExistenceBlocker.UNSUPPORTED_OBJECT)
      return CanonicalRecordingExistenceTruth.truth(
        normalizedObjectID, local, peer, localState, peerState,
        CanonicalRecordingExistenceDecision.UNSUPPORTED,
        sources, blockers
      )
    }

    return CanonicalRecordingExistenceTruth.truth(
      normalizedObjectID, local, peer, localState,
      CanonicalRecordingExistenceState.UNSUPPORTED,
      CanonicalRecordingExistenceDecision.UNSUPPORTED,
      sources, blockers
    )
  }

  diagnostics(
    syncRunID: string | null,
    mode: CanonicalSyncRuntimeMode
  ): CanonicalSyncRuntimeDiagnostic[] {
    const output: CanonicalSyncRuntimeDiagnostic[] = []

    output.push(new CanonicalSyncRuntimeDiagnostic(
      CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_TRUTH_EVALUATED,
      syncRunID,
      mode,
      this.objectID,
      this.localAudioHashPrefix ?? this.peerAudioHashPrefix ??
        this.localMetadataHashPrefix ?? this.peerMetadataHashPrefix,
      this.localByteSize ?? undefined,
      this.localState + '->' + this.peerState + ':' + this.decision
    ))

    if (this.decision === CanonicalRecordingExistenceDecision.APPLY_METADATA_ONLY_BRIDGE) {
      output.push(new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_PEER_ABSENT_METADATA_BRIDGE_REQUIRED,
        syncRunID,
        mode,
        this.objectID,
        this.localMetadataHashPrefix,
        undefined,
        this.peerState
      ))
    } else if (this.decision === CanonicalRecordingExistenceDecision.UPLOAD_AUDIO_CANDIDATE) {
      output.push(new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_PEER_METADATA_ONLY_UPLOAD_CANDIDATE,
        syncRunID,
        mode,
        this.objectID,
        this.localAudioHashPrefix,
        this.localByteSize ?? undefined,
        this.peerState
      ))
    } else if (this.decision === CanonicalRecordingExistenceDecision.AUDIO_SAME_NO_OP) {
      output.push(new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_AUDIO_SAME_NO_OP,
        syncRunID,
        mode,
        this.objectID,
        this.localAudioHashPrefix,
        this.localByteSize ?? undefined,
        'sameHashAndSize'
      ))
    } else if (this.decision === CanonicalRecordingExistenceDecision.CONFLICT) {
      output.push(new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_AUDIO_CONFLICT,
        syncRunID,
        mode,
        this.objectID,
        this.localAudioHashPrefix,
        this.localByteSize ?? undefined,
        this.blockers.join('+')
      ))
    } else if (this.decision === CanonicalRecordingExistenceDecision.DEFERRED) {
      if (this.peerState === CanonicalRecordingExistenceState.PEER_UNKNOWN) {
        output.push(new CanonicalSyncRuntimeDiagnostic(
          CanonicalSyncRuntimeDiagnosticKind.CANONICAL_EXISTENCE_PEER_UNKNOWN_DEFERRED,
          syncRunID,
          mode,
          this.objectID,
          undefined,
          undefined,
          'peerUnknown'
        ))
      }
    }

    return output
  }

  private static existenceState(
    obj: CanonicalRecordingObject | null,
    known: boolean,
    studyItemExists: boolean,
    receiveRecordExists: boolean,
    completedLedgerOnly: boolean,
    tombstonedParent: boolean
  ): CanonicalRecordingExistenceState {
    if (!known) {
      return CanonicalRecordingExistenceState.PEER_UNKNOWN
    }
    if (tombstonedParent || obj?.syncState === CanonicalSyncState.DELETED) {
      return CanonicalRecordingExistenceState.TOMBSTONED
    }
    if (!obj) {
      if (receiveRecordExists) {
        return CanonicalRecordingExistenceState.RECEIVE_RECORD_ONLY
      }
      if (studyItemExists) {
        return CanonicalRecordingExistenceState.STUDY_ITEM_ONLY
      }
      return completedLedgerOnly
        ? CanonicalRecordingExistenceState.METADATA_ONLY
        : CanonicalRecordingExistenceState.ABSENT
    }
    if (obj.audioAvailable) {
      return CanonicalRecordingExistenceState.AUDIO_AVAILABLE
    }
    if (receiveRecordExists && studyItemExists) {
      return CanonicalRecordingExistenceState.METADATA_AND_STUDY_ITEM
    }
    if (receiveRecordExists) {
      return CanonicalRecordingExistenceState.RECEIVE_RECORD_ONLY
    }
    if (studyItemExists) {
      return CanonicalRecordingExistenceState.METADATA_AND_STUDY_ITEM
    }
    return CanonicalRecordingExistenceState.METADATA_ONLY
  }

  private static truth(
    objectID: string,
    local: CanonicalRecordingObject | null,
    peer: CanonicalRecordingObject | null,
    localState: CanonicalRecordingExistenceState,
    peerState: CanonicalRecordingExistenceState,
    decision: CanonicalRecordingExistenceDecision,
    sources: Set<CanonicalRecordingExistenceSource>,
    blockers: Set<CanonicalRecordingExistenceBlocker>
  ): CanonicalRecordingExistenceTruth {
    return new CanonicalRecordingExistenceTruth(
      objectID,
      localState,
      peerState,
      decision,
      [...sources].sort(),
      [...blockers].sort(),
      local?.metadataHash?.value ? hashPrefix(local.metadataHash.value) : null,
      peer?.metadataHash?.value ? hashPrefix(peer.metadataHash.value) : null,
      local?.audioArtifact?.contentHash?.value
        ? hashPrefix(local.audioArtifact.contentHash.value) : null,
      peer?.audioArtifact?.contentHash?.value
        ? hashPrefix(peer.audioArtifact.contentHash.value) : null,
      local?.audioArtifact?.byteSize ?? null,
      peer?.audioArtifact?.byteSize ?? null
    )
  }
}

// =============================================================================
// SECTION 21: EXISTENCE APPLY RUNTIME TYPES
// =============================================================================

export enum CanonicalExistenceApplyRuntimeMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  NO_COMMIT = 'noCommit',
  TEST_ROOT_APPLY = 'testRootApply',
  PRODUCTION_ROOT_APPLY = 'productionRootApply',
  BLOCKED = 'blocked'
}

export function evaluatesCandidates(mode: CanonicalExistenceApplyRuntimeMode): boolean {
  return mode !== CanonicalExistenceApplyRuntimeMode.BLOCKED
}

export function canCommitMetadataOnlyRecord(
  mode: CanonicalExistenceApplyRuntimeMode
): boolean {
  return mode === CanonicalExistenceApplyRuntimeMode.TEST_ROOT_APPLY ||
    mode === CanonicalExistenceApplyRuntimeMode.PRODUCTION_ROOT_APPLY
}

export class CanonicalExistenceApplyRuntimePolicy {
  debugInternalBuild: boolean
  ownerApproved: boolean
  releaseDefaultBuild: boolean
  diagnosticsRedacted: boolean
  legacyFallbackAvailable: boolean
  rootBoundRequired: boolean
  rollbackRequired: boolean
  atomicWriteRequired: boolean
  postconditionRequired: boolean
  writeAudioAllowed: boolean
  markAudioAvailableAllowed: boolean

  constructor(
    debugInternalBuild: boolean = false,
    ownerApproved: boolean = false,
    releaseDefaultBuild: boolean = true,
    diagnosticsRedacted: boolean = true,
    legacyFallbackAvailable: boolean = true,
    rootBoundRequired: boolean = true,
    rollbackRequired: boolean = true,
    atomicWriteRequired: boolean = true,
    postconditionRequired: boolean = true,
    writeAudioAllowed: boolean = false,
    markAudioAvailableAllowed: boolean = false
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApproved = ownerApproved
    this.releaseDefaultBuild = releaseDefaultBuild
    this.diagnosticsRedacted = diagnosticsRedacted
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.rootBoundRequired = rootBoundRequired
    this.rollbackRequired = rollbackRequired
    this.atomicWriteRequired = atomicWriteRequired
    this.postconditionRequired = postconditionRequired
    this.writeAudioAllowed = writeAudioAllowed
    this.markAudioAvailableAllowed = markAudioAvailableAllowed
  }
}

export class CanonicalExistenceApplyRuntimeConfiguration {
  mode: CanonicalExistenceApplyRuntimeMode
  policy: CanonicalExistenceApplyRuntimePolicy

  constructor(
    mode: CanonicalExistenceApplyRuntimeMode = CanonicalExistenceApplyRuntimeMode.DISABLED,
    policy: CanonicalExistenceApplyRuntimePolicy = new CanonicalExistenceApplyRuntimePolicy()
  ) {
    this.mode = mode
    this.policy = policy
  }

  static readonly DISABLED = new CanonicalExistenceApplyRuntimeConfiguration()

  get canWriteMetadataOnlyRecord(): boolean {
    if (!canCommitMetadataOnlyRecord(this.mode) ||
      !this.policy.diagnosticsRedacted ||
      !this.policy.legacyFallbackAvailable ||
      !this.policy.rootBoundRequired ||
      !this.policy.rollbackRequired ||
      !this.policy.atomicWriteRequired ||
      !this.policy.postconditionRequired ||
      this.policy.writeAudioAllowed ||
      this.policy.markAudioAvailableAllowed) {
      return false
    }
    if (this.mode === CanonicalExistenceApplyRuntimeMode.PRODUCTION_ROOT_APPLY) {
      return this.policy.debugInternalBuild &&
        this.policy.ownerApproved &&
        !this.policy.releaseDefaultBuild
    }
    return this.mode === CanonicalExistenceApplyRuntimeMode.TEST_ROOT_APPLY
  }
}

// =============================================================================
// SECTION 22: SYNC RUNTIME TYPES
// =============================================================================

export enum CanonicalSyncRuntimeMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  CANONICAL_PLAN_NO_COMMIT = 'canonicalPlanNoCommit',
  CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK = 'canonicalPlanPrimaryWithLegacyFallback',
  BLOCKED = 'blocked'
}

export enum CanonicalSyncRuntimeDiagnosticKind {
  CANONICAL_SYNC_RUNTIME_MODE_EVALUATED = 'canonicalSyncRuntimeModeEvaluated',
  CANONICAL_SYNC_RUNTIME_AUTHORITY_GATE_ALLOWED = 'canonicalSyncRuntimeAuthorityGateAllowed',
  CANONICAL_SYNC_RUNTIME_AUTHORITY_GATE_BLOCKED = 'canonicalSyncRuntimeAuthorityGateBlocked',
  CANONICAL_SYNC_RUNTIME_PLAN_EVALUATED = 'canonicalSyncRuntimePlanEvaluated',
  CANONICAL_SYNC_RUNTIME_PLAN_ALLOWED = 'canonicalSyncRuntimePlanAllowed',
  CANONICAL_SYNC_RUNTIME_PLAN_USED = 'canonicalSyncRuntimePlanUsed',
  CANONICAL_SYNC_RUNTIME_PLAN_NO_COMMIT = 'canonicalSyncRuntimePlanNoCommit',
  CANONICAL_SYNC_RUNTIME_PLAN_FALLBACK = 'canonicalSyncRuntimePlanFallback',
  CANONICAL_SYNC_RUNTIME_PLAN_BLOCKED = 'canonicalSyncRuntimePlanBlocked',
  CANONICAL_SYNC_RUNTIME_LEGACY_HASH_MISMATCH_IGNORED = 'canonicalSyncRuntimeLegacyHashMismatchIgnored',
  CANONICAL_SYNC_RUNTIME_UNSUPPORTED_OBJECT_BLOCKED = 'canonicalSyncRuntimeUnsupportedObjectBlocked',
  CANONICAL_SYNC_RUNTIME_CONFLICT_BLOCKED = 'canonicalSyncRuntimeConflictBlocked',
  CANONICAL_SYNC_RUNTIME_PEER_SNAPSHOT_UNAVAILABLE = 'canonicalSyncRuntimePeerSnapshotUnavailable',
  CANONICAL_SYNC_RUNTIME_DUPLICATE_LEGACY_SUPPRESSED = 'canonicalSyncRuntimeDuplicateLegacySuppressed',
  CANONICAL_SYNC_RUNTIME_DUPLICATE_EXECUTION_PREVENTED = 'canonicalSyncRuntimeDuplicateExecutionPrevented',
  CANONICAL_SYNC_RUNTIME_METADATA_HASH_EQUAL = 'canonicalSyncRuntimeMetadataHashEqual',
  CANONICAL_SYNC_RUNTIME_MODIFIED_AT_LWW_APPLIED = 'canonicalSyncRuntimeModifiedAtLWWApplied',
  CANONICAL_SYNC_RUNTIME_MODIFIED_AT_UNAVAILABLE = 'canonicalSyncRuntimeModifiedAtUnavailable',
  CANONICAL_SYNC_RUNTIME_SCHEMA_MISMATCH = 'canonicalSyncRuntimeSchemaMismatch',
  CANONICAL_EXISTENCE_TRUTH_EVALUATED = 'canonicalExistenceTruthEvaluated',
  CANONICAL_EXISTENCE_APPLY_BRIDGE_EVALUATED = 'canonicalExistenceApplyBridgeEvaluated',
  CANONICAL_EXISTENCE_APPLY_BRIDGE_BLOCKED = 'canonicalExistenceApplyBridgeBlocked',
  CANONICAL_EXISTENCE_METADATA_ONLY_RECORD_WRITTEN = 'canonicalExistenceMetadataOnlyRecordWritten',
  CANONICAL_EXISTENCE_METADATA_ONLY_RECORD_NO_OP = 'canonicalExistenceMetadataOnlyRecordNoOp',
  CANONICAL_EXISTENCE_APPLY_BRIDGE_ROLLBACK_STARTED = 'canonicalExistenceApplyBridgeRollbackStarted',
  CANONICAL_EXISTENCE_APPLY_BRIDGE_ROLLBACK_COMPLETED = 'canonicalExistenceApplyBridgeRollbackCompleted',
  CANONICAL_EXISTENCE_APPLY_BRIDGE_ROLLBACK_FAILED = 'canonicalExistenceApplyBridgeRollbackFailed',
  CANONICAL_EXISTENCE_PEER_METADATA_ONLY_UPLOAD_CANDIDATE = 'canonicalExistencePeerMetadataOnlyUploadCandidate',
  CANONICAL_EXISTENCE_PEER_ABSENT_METADATA_BRIDGE_REQUIRED = 'canonicalExistencePeerAbsentMetadataBridgeRequired',
  CANONICAL_EXISTENCE_PEER_UNKNOWN_DEFERRED = 'canonicalExistencePeerUnknownDeferred',
  CANONICAL_EXISTENCE_AUDIO_SAME_NO_OP = 'canonicalExistenceAudioSameNoOp',
  CANONICAL_EXISTENCE_AUDIO_CONFLICT = 'canonicalExistenceAudioConflict',
  CANONICAL_EXISTENCE_MANIFEST_RECORDINGS_CONSUMED = 'canonicalExistenceManifestRecordingsConsumed',
  CANONICAL_EXISTENCE_MANIFEST_RECORDINGS_IGNORED_BLOCKED = 'canonicalExistenceManifestRecordingsIgnoredBlocked',
  CANONICAL_EXISTENCE_DID_NOT_WRITE_AUDIO = 'canonicalExistenceDidNotWriteAudio',
  CANONICAL_EXISTENCE_DID_NOT_MARK_AUDIO_AVAILABLE = 'canonicalExistenceDidNotMarkAudioAvailable',
  CANONICAL_APPLY_RUNTIME_MODE_EVALUATED = 'canonicalApplyRuntimeModeEvaluated',
  CANONICAL_APPLY_RUNTIME_GATE_ALLOWED = 'canonicalApplyRuntimeGateAllowed',
  CANONICAL_APPLY_RUNTIME_GATE_BLOCKED = 'canonicalApplyRuntimeGateBlocked',
  CANONICAL_APPLY_RUNTIME_ACTION_STARTED = 'canonicalApplyRuntimeActionStarted',
  CANONICAL_APPLY_RUNTIME_ACTION_COMPLETED = 'canonicalApplyRuntimeActionCompleted',
  CANONICAL_APPLY_RUNTIME_ACTION_FAILED = 'canonicalApplyRuntimeActionFailed',
  CANONICAL_APPLY_RUNTIME_ROLLBACK_STARTED = 'canonicalApplyRuntimeRollbackStarted',
  CANONICAL_APPLY_RUNTIME_ROLLBACK_COMPLETED = 'canonicalApplyRuntimeRollbackCompleted',
  CANONICAL_APPLY_RUNTIME_ROLLBACK_FAILED = 'canonicalApplyRuntimeRollbackFailed',
  CANONICAL_APPLY_RUNTIME_LEGACY_FALLBACK_USED = 'canonicalApplyRuntimeLegacyFallbackUsed',
  CANONICAL_APPLY_RUNTIME_DUPLICATE_LEGACY_SUPPRESSED = 'canonicalApplyRuntimeDuplicateLegacySuppressed',
  CANONICAL_APPLY_RUNTIME_AUDIO_ACTION_BLOCKED = 'canonicalApplyRuntimeAudioActionBlocked',
  CANONICAL_APPLY_RUNTIME_REPORT_BUILT = 'canonicalApplyRuntimeReportBuilt'
}

export class CanonicalSyncRuntimeDiagnostic {
  kind: CanonicalSyncRuntimeDiagnosticKind
  syncRunID: string | null
  mode: CanonicalSyncRuntimeMode
  objectID: string | null
  hashPrefix: string | null
  count: number | undefined
  detail: string | null

  constructor(
    kind: CanonicalSyncRuntimeDiagnosticKind,
    syncRunID: string | null,
    mode: CanonicalSyncRuntimeMode,
    objectID: string | null = null,
    hashPrefix: string | null = null,
    count: number | undefined = undefined,
    detail: string | null = null
  ) {
    this.kind = kind
    this.syncRunID = syncRunID
    this.mode = mode
    this.objectID = objectID
    this.hashPrefix = hashPrefix
    this.count = count
    this.detail = detail
  }

  get id(): string {
    return [this.kind, this.syncRunID ?? '', this.objectID ?? '', this.detail ?? '']
      .join('|')
  }
}
