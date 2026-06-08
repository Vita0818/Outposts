/**
 * CanonicalProductionPorts — production port definitions and snapshots.
 * Ported from Rokurics-Apple (CanonicalProductionPorts.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalNode, CanonicalManifest,
  CanonicalArtifactKind, CanonicalRecordingObject, CanonicalLibraryObject,
  CanonicalLibraryProjection, CanonicalArtifact, nilIfEmpty, hashPrefix,
  safeLogicalPathToken, generatesArtifactKinds, artifactID, artifactKey,
  provesGeneratedArtifactAvailability
} from './CanonicalCore'

// =============================================================================
// ENUMS
// =============================================================================

export enum CanonicalProductionDomain {
  RECORDING_METADATA = 'recordingMetadata',
  RECORDING_AUDIO = 'recordingAudio',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  FOLDERS = 'folders',
  STUDY_ITEMS = 'studyItems',
  STANDALONE_NOTES = 'standaloneNotes',
  TOMBSTONES = 'tombstones',
  CONFLICTS = 'conflicts',
  APPLY = 'apply',
  FILE_RUNTIME = 'fileRuntime',
  TRANSPORT_RUNTIME = 'transportRuntime',
  UPLOAD_RUNTIME = 'uploadRuntime',
  OBJECT_PROJECTION = 'objectProjection',
  INVENTORY = 'inventory',
  UI_INTEGRATION = 'uiIntegration'
}

export enum CanonicalProductionPortKind {
  FILE = 'file',
  TRANSPORT = 'transport',
  UPLOAD = 'upload',
  APPLY = 'apply',
  SYNC_CLOCK = 'syncClock',
  DIAGNOSTICS = 'diagnostics',
  CAPABILITY = 'capability'
}

export enum CanonicalProductionOperation {
  METADATA_SNAPSHOT_READ = 'metadataSnapshotRead',
  METADATA_READ = 'metadataRead',
  METADATA_WRITE = 'metadataWrite',
  ARTIFACT_DESCRIPTOR_READ = 'artifactDescriptorRead',
  ARTIFACT_READ = 'artifactRead',
  ARTIFACT_WRITE_ATOMIC = 'artifactWriteAtomic',
  ARTIFACT_VERIFY = 'artifactVerify',
  ARTIFACT_BYTES_READ_DRY_RUN = 'artifactBytesReadDryRun',
  LOGICAL_TOKEN_RESOLVE = 'logicalTokenResolve',
  CONTAINMENT_VERIFY = 'containmentVerify',
  ATOMIC_WRITE_PROJECT = 'atomicWriteProject',
  ATOMIC_WRITE_EXECUTE = 'atomicWriteExecute',
  ARTIFACT_LIST = 'artifactList',
  OBJECT_LIST = 'objectList',
  HASH_COMPUTE = 'hashCompute',
  ROLLBACK_WRITE = 'rollbackWrite',
  TOMBSTONE_APPLY_PROJECT = 'tombstoneApplyProject',
  TOMBSTONE_MARK = 'tombstoneMark',
  PHYSICAL_DELETE_SUPPRESSED = 'physicalDeleteSuppressed',
  ROUTE_ENVELOPE_BUILD = 'routeEnvelopeBuild',
  SIGNED_REQUEST_BUILD = 'signedRequestBuild',
  REQUEST_SEND = 'requestSend',
  RESPONSE_RECEIVE = 'responseReceive',
  RESPONSE_VERIFY = 'responseVerify',
  MANIFEST_EXCHANGE = 'manifestExchange',
  ARTIFACT_REQUEST = 'artifactRequest',
  APPLY_METADATA_SEND = 'applyMetadataSend',
  UPLOAD_SESSION_START = 'uploadSessionStart',
  UPLOAD_SESSION_QUERY = 'uploadSessionQuery',
  UPLOAD_CHUNK_SEND = 'uploadChunkSend',
  UPLOAD_SESSION_FINALIZE = 'uploadSessionFinalize',
  UPLOAD_SESSION_CANCEL = 'uploadSessionCancel',
  ROUTE_RESPONSE_DECODE = 'routeResponseDecode',
  UPLOAD_START_PROJECT = 'uploadStartProject',
  RESUMABLE_UPLOAD_START = 'resumableUploadStart',
  RESUMABLE_UPLOAD_RESUME = 'resumableUploadResume',
  UPLOAD_CHUNK_PROJECT = 'uploadChunkProject',
  RESUMABLE_UPLOAD_CHUNK = 'resumableUploadChunk',
  UPLOAD_FINALIZE_PROJECT = 'uploadFinalizeProject',
  RESUMABLE_UPLOAD_FINALIZE = 'resumableUploadFinalize',
  RESUMABLE_UPLOAD_CANCEL = 'resumableUploadCancel',
  UPLOAD_LEDGER_READ = 'uploadLedgerRead',
  UPLOAD_LEDGER_WRITE = 'uploadLedgerWrite',
  RETRY_PROJECT = 'retryProject',
  UPLOAD_STATE_ROLLBACK = 'uploadStateRollback',
  APPLY_METADATA_PROJECT = 'applyMetadataProject',
  METADATA_APPLY = 'metadataApply',
  METADATA_SEND = 'metadataSend',
  APPLY_GENERATED_ARTIFACT_PROJECT = 'applyGeneratedArtifactProject',
  GENERATED_ARTIFACT_APPLY = 'generatedArtifactApply',
  GENERATED_ARTIFACT_REQUEST = 'generatedArtifactRequest',
  OBJECT_TOMBSTONE_APPLY = 'objectTombstoneApply',
  LIBRARY_TOMBSTONE_APPLY = 'libraryTombstoneApply',
  RECORD_CONFLICT_PROJECT = 'recordConflictProject',
  CONFLICT_RECORD = 'conflictRecord',
  PRECONDITION_VERIFY = 'preconditionVerify',
  POSTCONDITION_VERIFY = 'postconditionVerify',
  APPLY_ROLLBACK = 'applyRollback',
  DIAGNOSTICS_RECORD = 'diagnosticsRecord'
}

export enum CanonicalProductionCapability {
  DRY_RUN_ONLY = 'dryRunOnly',
  ROOT_BOUND_FILE_ACCESS = 'rootBoundFileAccess',
  ROOT_BOUND_READ = 'rootBoundRead',
  ROOT_BOUND_WRITE = 'rootBoundWrite',
  LOGICAL_TOKEN_VALIDATION = 'logicalTokenValidation',
  CONTAINMENT_VERIFICATION = 'containmentVerification',
  ATOMIC_WRITE_PROJECTION = 'atomicWriteProjection',
  ATOMIC_WRITE_EXECUTION = 'atomicWriteExecution',
  HASH_SIZE_VERIFICATION = 'hashSizeVerification',
  STREAMING_HASH = 'streamingHash',
  ROLLBACK_CHECKPOINT = 'rollbackCheckpoint',
  NO_PHYSICAL_DELETE = 'noPhysicalDelete',
  ROUTE_SIGNING = 'routeSigning',
  ROUTE_VERIFICATION = 'routeVerification',
  EXTERNAL_SIGNER_REQUIRED = 'externalSignerRequired',
  SIGNED_REQUEST_EXECUTION = 'signedRequestExecution',
  RESPONSE_VERIFICATION = 'responseVerification',
  RESUMABLE_UPLOAD_PROJECTION = 'resumableUploadProjection',
  RESUMABLE_UPLOAD_EXECUTION = 'resumableUploadExecution',
  CHUNK_RESUME_PROJECTION = 'chunkResumeProjection',
  CONFIRMED_BYTES_QUERY = 'confirmedBytesQuery',
  FINALIZATION_PROJECTION = 'finalizationProjection',
  FINALIZATION_EXECUTION = 'finalizationExecution',
  RETRY_PROJECTION = 'retryProjection',
  UPLOAD_LEDGER_MUTATION = 'uploadLedgerMutation',
  METADATA_APPLY_PROJECTION = 'metadataApplyProjection',
  METADATA_APPLY_EXECUTION = 'metadataApplyExecution',
  GENERATED_ARTIFACT_APPLY_PROJECTION = 'generatedArtifactApplyProjection',
  GENERATED_ARTIFACT_APPLY_EXECUTION = 'generatedArtifactApplyExecution',
  TOMBSTONE_APPLY_PROJECTION = 'tombstoneApplyProjection',
  TOMBSTONE_APPLY_EXECUTION = 'tombstoneApplyExecution',
  CONFLICT_RECORD_PROJECTION = 'conflictRecordProjection',
  CONFLICT_RECORD_EXECUTION = 'conflictRecordExecution',
  PRECONDITION_VERIFICATION = 'preconditionVerification',
  POSTCONDITION_VERIFICATION = 'postconditionVerification',
  IN_MEMORY_DIAGNOSTICS = 'inMemoryDiagnostics',
  PRODUCTION_DIAGNOSTICS = 'productionDiagnostics'
}

export enum CanonicalProductionPortError {
  MISSING_PORT = 'missingPort',
  CAPABILITY_MISSING = 'capabilityMissing',
  UNSAFE_LOGICAL_TOKEN = 'unsafeLogicalToken',
  PRODUCTION_MUTATION_ATTEMPTED = 'productionMutationAttempted',
  NETWORK_EXECUTION_SUPPRESSED = 'networkExecutionSuppressed',
  UNSUPPORTED_OBJECT = 'unsupportedObject',
  FULL_CONTENT_REJECTED = 'fullContentRejected',
  ROUTE_BYPASS_RISK = 'routeBypassRisk',
  PATH_ESCAPE_RISK = 'pathEscapeRisk'
}

export enum CanonicalProductionDiagnosticEventKind {
  CANONICAL_PRODUCTION_PORTS_DECLARED = 'canonicalProductionPortsDeclared',
  CANONICAL_PRODUCTION_SNAPSHOT_BUILT = 'canonicalProductionSnapshotBuilt',
  CANONICAL_DRY_RUN_STARTED = 'canonicalDryRunStarted',
  CANONICAL_DRY_RUN_COMPLETED = 'canonicalDryRunCompleted',
  CANONICAL_DRY_RUN_BLOCKED = 'canonicalDryRunBlocked',
  CANONICAL_DRY_RUN_DIVERGENCE_DETECTED = 'canonicalDryRunDivergenceDetected',
  CANONICAL_LEGACY_EQUIVALENT = 'canonicalLegacyEquivalent',
  CANONICAL_LEGACY_DIVERGENT = 'canonicalLegacyDivergent',
  CANONICAL_PRODUCTION_MIGRATION_BLOCKED = 'canonicalProductionMigrationBlocked',
  CANONICAL_ELIGIBLE_FOR_MANUAL_MIGRATION_DESIGN = 'canonicalEligibleForManualMigrationDesign',
  CANONICAL_PORT_MISSING = 'canonicalPortMissing',
  CANONICAL_PORT_CAPABILITY_MISSING = 'canonicalPortCapabilityMissing',
  CANONICAL_DRY_RUN_WOULD_WRITE_BUT_SUPPRESSED = 'canonicalDryRunWouldWriteButSuppressed',
  CANONICAL_DRY_RUN_WOULD_UPLOAD_BUT_SUPPRESSED = 'canonicalDryRunWouldUploadButSuppressed',
  CANONICAL_DRY_RUN_WOULD_SEND_NETWORK_BUT_SUPPRESSED = 'canonicalDryRunWouldSendNetworkButSuppressed'
}

export enum CanonicalProductionArtifactAvailability {
  AVAILABLE = 'available',
  MISSING = 'missing',
  UNKNOWN = 'unknown',
  UNSUPPORTED = 'unsupported'
}

export enum CanonicalProductionHTTPMethod {
  GET = 'GET',
  POST = 'POST'
}

export enum CanonicalProductionUploadFailureKind {
  RETRYABLE = 'retryable',
  CONFLICT = 'conflict',
  FATAL = 'fatal'
}

// =============================================================================
// DIAGNOSTICS EVENT
// =============================================================================

export class CanonicalProductionDiagnosticsEvent {
  kind: CanonicalProductionDiagnosticEventKind
  domain: CanonicalProductionDomain | null
  action: string | null
  blocker: string | null
  reason: string | null
  hashPrefix: string | null
  dryRun: boolean
  generatedAt: CanonicalTimestamp

  constructor(
    kind: CanonicalProductionDiagnosticEventKind,
    domain: CanonicalProductionDomain | null = null,
    action: string | null = null,
    blocker: string | null = null,
    reason: string | null = null,
    hashValue: string | null = null,
    hashPrefixOverride: string | null = null,
    dryRun: boolean = true,
    generatedAt: Date = new Date()
  ) {
    this.kind = kind
    this.domain = domain
    this.action = CanonicalProductionRedaction.safeDiagnosticText(action)
    this.blocker = CanonicalProductionRedaction.safeDiagnosticText(blocker)
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason)
    this.hashPrefix = hashValue
      ? CanonicalProductionRedaction.hashPrefix(hashValue)
      : CanonicalProductionRedaction.hashPrefix(hashPrefixOverride)
    this.dryRun = dryRun
    this.generatedAt = new CanonicalTimestamp(generatedAt)
  }

  get id(): string {
    return [this.kind, this.domain ?? '', this.action ?? '', this.blocker ?? '', this.reason ?? ''].join('|')
  }
}

// =============================================================================
// STRUCTS
// =============================================================================

export class CanonicalProductionArtifactDescriptor {
  artifactID: string
  objectID: string
  kind: CanonicalArtifactKind
  logicalPathToken: string | null
  logicalName: string | null
  contentHashPrefix: string | null
  byteSize: number | null
  availability: CanonicalProductionArtifactAvailability
  unsupportedReason: string | null

  constructor(
    artifactID: string,
    objectID: string,
    kind: CanonicalArtifactKind,
    logicalPathToken: string | null = null,
    logicalName: string | null = null,
    contentHash: CanonicalHash | null = null,
    contentHashPrefix: string | null = null,
    byteSize: number | null = null,
    availability: CanonicalProductionArtifactAvailability,
    unsupportedReason: string | null = null
  ) {
    this.artifactID = CanonicalProductionRedaction.safeIdentifier(artifactID, kind + ':unknown')
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.kind = kind
    this.logicalPathToken = logicalPathToken ? safeLogicalPathToken(logicalPathToken) : null
    this.logicalName = CanonicalProductionRedaction.safeFileName(logicalName)
    this.contentHashPrefix = contentHash
      ? CanonicalProductionRedaction.hashPrefix(contentHash.value)
      : CanonicalProductionRedaction.hashPrefix(contentHashPrefix)
    this.byteSize = byteSize
    this.availability = availability
    this.unsupportedReason = CanonicalProductionRedaction.safeDiagnosticText(unsupportedReason)
    if (logicalPathToken && !this.logicalPathToken && !this.unsupportedReason) {
      this.unsupportedReason = 'unsafeLogicalPathToken'
    }
  }

  static fromArtifact(artifact: CanonicalArtifact): CanonicalProductionArtifactDescriptor {
    let availability: CanonicalProductionArtifactAvailability
    switch (artifact.availability) {
      case 'available':
        availability = CanonicalProductionArtifactAvailability.AVAILABLE
        break
      case 'availableWithoutHash':
        availability = CanonicalProductionArtifactAvailability.AVAILABLE
        break
      case 'missing':
        availability = CanonicalProductionArtifactAvailability.MISSING
        break
      default:
        availability = CanonicalProductionArtifactAvailability.UNKNOWN
        break
    }
    return new CanonicalProductionArtifactDescriptor(
      artifact.artifactID,
      artifact.objectID,
      artifact.kind,
      artifact.logicalPathToken,
      artifact.logicalName,
      artifact.contentHash,
      null,
      artifact.byteSize,
      availability,
      null
    )
  }

  get id(): string {
    return this.artifactID
  }

  get hasUnsafePathSignal(): boolean {
    return this.logicalPathToken === null && this.unsupportedReason === 'unsafeLogicalPathToken'
  }
}

// -----------------------------------------------------------------------------

export class CanonicalProductionReadProjection {
  reference: CanonicalFileReference
  wouldReadBytes: boolean
  byteSize: number | null
  contentHashPrefix: string | null
  dryRun: boolean

  constructor(
    reference: CanonicalFileReference,
    wouldReadBytes: boolean,
    byteSize: number | null = null,
    contentHash: CanonicalHash | null = null,
    dryRun: boolean = true
  ) {
    this.reference = reference
    this.wouldReadBytes = wouldReadBytes
    this.byteSize = byteSize
    this.contentHashPrefix = contentHash ? CanonicalProductionRedaction.hashPrefix(contentHash.value) : null
    this.dryRun = dryRun
  }
}

export class CanonicalProductionWriteIntentProjection {
  reference: CanonicalFileReference
  purpose: CanonicalFilePurpose
  wouldWrite: boolean
  suppressedBecauseDryRun: boolean
  noPhysicalDelete: boolean
  byteSize: number | null
  contentHashPrefix: string | null
  disposition: CanonicalFileWriteDisposition | null
  reason: string | null

  constructor(
    reference: CanonicalFileReference,
    purpose: CanonicalFilePurpose,
    wouldWrite: boolean,
    suppressedBecauseDryRun: boolean = true,
    noPhysicalDelete: boolean = true,
    byteSize: number | null = null,
    contentHash: CanonicalHash | null = null,
    disposition: CanonicalFileWriteDisposition | null = null,
    reason: string | null = null
  ) {
    this.reference = reference
    this.purpose = purpose
    this.wouldWrite = wouldWrite
    this.suppressedBecauseDryRun = suppressedBecauseDryRun
    this.noPhysicalDelete = noPhysicalDelete
    this.byteSize = byteSize
    this.contentHashPrefix = contentHash ? CanonicalProductionRedaction.hashPrefix(contentHash.value) : null
    this.disposition = disposition
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason)
  }
}

export class CanonicalProductionTransportRouteCapability {
  route: CanonicalTransportRoute
  requiresSigning: boolean
  requiresVerification: boolean
  dryRunOnly: boolean

  constructor(
    route: CanonicalTransportRoute,
    requiresSigning: boolean = true,
    requiresVerification: boolean = true,
    dryRunOnly: boolean = true
  ) {
    this.route = route
    this.requiresSigning = requiresSigning
    this.requiresVerification = requiresVerification
    this.dryRunOnly = dryRunOnly
  }
}

export class CanonicalProductionTransportEnvelopeDryRun {
  route: CanonicalTransportRoute
  sourceNodeID: string
  destinationNodeID: string
  requiresSigning: boolean
  requiresVerification: boolean
  bodyHashPrefix: string | null
  wouldSendNetwork: boolean
  suppressedBecauseDryRun: boolean
  reason: string

  constructor(
    route: CanonicalTransportRoute,
    sourceNodeID: string,
    destinationNodeID: string,
    bodyHash: CanonicalHash | null = null,
    requiresSigning: boolean = true,
    requiresVerification: boolean = true,
    reason: string = 'networkSuppressedDryRun'
  ) {
    this.route = route
    this.sourceNodeID = CanonicalProductionRedaction.safeIdentifier(sourceNodeID, 'source:unknown')
    this.destinationNodeID = CanonicalProductionRedaction.safeIdentifier(destinationNodeID, 'destination:unknown')
    this.requiresSigning = requiresSigning
    this.requiresVerification = requiresVerification
    this.bodyHashPrefix = bodyHash ? CanonicalProductionRedaction.hashPrefix(bodyHash.value) : null
    this.wouldSendNetwork = false
    this.suppressedBecauseDryRun = true
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? 'networkSuppressedDryRun'
  }
}

export class CanonicalProductionUploadTrace {
  objectID: string
  artifactID: string | null
  totalBytes: number | null
  totalHashPrefix: string | null
  chunkSize: number | null
  resumable: boolean
  wouldUpload: boolean
  suppressedBecauseDryRun: boolean
  mappedToLegacyUploadCapability: boolean
  route: CanonicalTransportRoute | null
  reason: string

  constructor(
    objectID: string,
    artifactID: string | null = null,
    totalBytes: number | null = null,
    totalHash: CanonicalHash | null = null,
    chunkSize: number | null = null,
    resumable: boolean = true,
    route: CanonicalTransportRoute | null = null,
    reason: string = 'uploadSuppressedDryRun'
  ) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.totalBytes = totalBytes
    this.totalHashPrefix = totalHash ? CanonicalProductionRedaction.hashPrefix(totalHash.value) : null
    this.chunkSize = chunkSize
    this.resumable = resumable
    this.wouldUpload = false
    this.suppressedBecauseDryRun = true
    this.mappedToLegacyUploadCapability = true
    this.route = route
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? 'uploadSuppressedDryRun'
  }
}

export class CanonicalProductionApplyTrace {
  actionID: string
  kind: string
  target: CanonicalApplyTarget
  bridgeHint: string | null
  wouldWrite: boolean
  wouldCallApplySyncManifest: boolean
  suppressedBecauseDryRun: boolean
  reason: string

  constructor(
    actionID: string,
    kind: string,
    target: CanonicalApplyTarget,
    bridgeHint: string | null = null,
    reason: string = 'applySuppressedDryRun'
  ) {
    this.actionID = CanonicalProductionRedaction.safeIdentifier(actionID, kind)
    this.kind = kind
    this.target = target
    this.bridgeHint = bridgeHint
    this.wouldWrite = true
    this.wouldCallApplySyncManifest = false
    this.suppressedBecauseDryRun = true
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? 'applySuppressedDryRun'
  }
}

export class CanonicalProductionUnsupportedFact {
  objectID: string
  domain: CanonicalProductionDomain
  reason: string

  constructor(objectID: string, domain: CanonicalProductionDomain, reason: string) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown')
    this.domain = domain
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? 'unsupported'
  }

  get id(): string {
    return [this.domain, this.objectID, this.reason].join('|')
  }
}

export class CanonicalLegacyActionSnapshot {
  actionIDsByDomain: Map<CanonicalProductionDomain, string[]>

  constructor(actionIDsByDomain: Map<CanonicalProductionDomain, string[]> = new Map()) {
    this.actionIDsByDomain = new Map()
    for (const [domain, ids] of actionIDsByDomain) {
      this.actionIDsByDomain.set(domain, CanonicalLegacyActionSnapshot.normalized(ids))
    }
  }

  static readonly EMPTY = new CanonicalLegacyActionSnapshot()

  actionIDs(domain: CanonicalProductionDomain): string[] {
    return this.actionIDsByDomain.get(domain) ?? []
  }

  actionIDSet(domain: CanonicalProductionDomain): Set<string> {
    return new Set(this.actionIDs(domain))
  }

  adding(ids: string[], domain: CanonicalProductionDomain): CanonicalLegacyActionSnapshot {
    const next = new Map(this.actionIDsByDomain)
    next.set(domain, CanonicalLegacyActionSnapshot.normalized((next.get(domain) ?? []).concat(ids)))
    return new CanonicalLegacyActionSnapshot(next)
  }

  private static normalized(ids: string[]): string[] {
    const set = new Set<string>()
    for (const id of ids) {
      const safe = CanonicalProductionRedaction.safeDiagnosticText(id)
      if (safe) {
        set.add(safe)
      }
    }
    return Array.from(set).sort()
  }
}

export class CanonicalRuntimeNodeState {
  node: CanonicalNode
  manifest: CanonicalManifest
  transferProjection: CanonicalTransferProjection
  inventoryCoverage: CanonicalInventoryCoverageReport
  retirementReadiness: CanonicalRetirementReadinessReport
  objectProjection: CanonicalLibraryProjection
  unsupportedFacts: CanonicalProductionUnsupportedFact[]
  diagnostics: CanonicalProductionDiagnosticsEvent[]

  constructor(
    node: CanonicalNode,
    manifest: CanonicalManifest,
    transferProjection: CanonicalTransferProjection,
    inventoryCoverage: CanonicalInventoryCoverageReport,
    retirementReadiness: CanonicalRetirementReadinessReport,
    objectProjection: CanonicalLibraryProjection,
    unsupportedFacts: CanonicalProductionUnsupportedFact[] = [],
    diagnostics: CanonicalProductionDiagnosticsEvent[] = []
  ) {
    this.node = node
    this.manifest = manifest
    this.transferProjection = transferProjection
    this.inventoryCoverage = inventoryCoverage
    this.retirementReadiness = retirementReadiness
    this.objectProjection = objectProjection
    this.unsupportedFacts = unsupportedFacts.sort((a, b) => a.id.localeCompare(b.id))
    this.diagnostics = diagnostics
  }
}

export class CanonicalProductionSnapshot {
  node: CanonicalNode
  manifest: CanonicalManifest
  runtimeNodeState: CanonicalRuntimeNodeState
  legacyActions: CanonicalLegacyActionSnapshot
  unsupportedFacts: CanonicalProductionUnsupportedFact[]
  diagnostics: CanonicalProductionDiagnosticsEvent[]

  constructor(
    node: CanonicalNode,
    manifest: CanonicalManifest,
    runtimeNodeState: CanonicalRuntimeNodeState,
    legacyActions: CanonicalLegacyActionSnapshot = CanonicalLegacyActionSnapshot.EMPTY,
    unsupportedFacts: CanonicalProductionUnsupportedFact[] = [],
    diagnostics: CanonicalProductionDiagnosticsEvent[] = []
  ) {
    this.node = node
    this.manifest = manifest
    this.runtimeNodeState = runtimeNodeState
    this.legacyActions = legacyActions
    this.unsupportedFacts = unsupportedFacts.sort((a, b) => a.id.localeCompare(b.id))
    this.diagnostics = diagnostics
  }
}

export class CanonicalProductionCapabilitySummary {
  nodeID: string
  capabilities: CanonicalProductionCapability[]
  supportedDomains: CanonicalProductionDomain[]
  dryRunOnly: boolean

  constructor(
    nodeID: string,
    capabilities: CanonicalProductionCapability[] = [],
    supportedDomains: CanonicalProductionDomain[] = [],
    dryRunOnly: boolean = true
  ) {
    this.nodeID = CanonicalProductionRedaction.safeIdentifier(nodeID, 'node:unknown')
    this.capabilities = Array.from(new Set(capabilities)).sort((a, b) => a.localeCompare(b))
    this.supportedDomains = Array.from(new Set(supportedDomains)).sort((a, b) => a.localeCompare(b))
    this.dryRunOnly = dryRunOnly
  }
}

export class CanonicalProductionPortReadiness {
  generatedAt: CanonicalTimestamp
  declaredPorts: Map<CanonicalProductionPortKind, boolean>
  missingPorts: CanonicalProductionPortKind[]
  dryRunOnly: boolean

  constructor(
    declaredPorts: Map<CanonicalProductionPortKind, boolean>,
    missingPorts: CanonicalProductionPortKind[],
    dryRunOnly: boolean = true,
    generatedAt: Date = new Date()
  ) {
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.declaredPorts = declaredPorts
    this.missingPorts = Array.from(new Set(missingPorts)).sort((a, b) => a.localeCompare(b))
    this.dryRunOnly = dryRunOnly
  }

  get hasAllRequiredDryRunPorts(): boolean {
    const required = [CanonicalProductionPortKind.FILE, CanonicalProductionPortKind.TRANSPORT,
    CanonicalProductionPortKind.UPLOAD, CanonicalProductionPortKind.APPLY]
    return !this.missingPorts.some(p => required.indexOf(p) >= 0)
  }

  get hasAllRequiredProductionPorts(): boolean {
    return this.hasAllRequiredDryRunPorts && !this.dryRunOnly
  }
}

export class CanonicalProductionFileVerificationEvidence {
  reference: CanonicalFileReference
  resolution: CanonicalPathResolutionResult
  expectedHashPrefix: string | null
  actualHashPrefix: string | null
  expectedByteSize: number | null
  actualByteSize: number | null
  hashVerified: boolean
  sizeVerified: boolean
  computedStreaming: boolean

  constructor(
    reference: CanonicalFileReference,
    resolution: CanonicalPathResolutionResult,
    expectedHash: CanonicalHash | null = null,
    actualHash: CanonicalHash | null = null,
    expectedByteSize: number | null = null,
    actualByteSize: number | null = null,
    computedStreaming: boolean = false
  ) {
    this.reference = reference
    this.resolution = resolution
    this.expectedHashPrefix = expectedHash ? CanonicalProductionRedaction.hashPrefix(expectedHash.value) : null
    this.actualHashPrefix = actualHash ? CanonicalProductionRedaction.hashPrefix(actualHash.value) : null
    this.expectedByteSize = expectedByteSize
    this.actualByteSize = actualByteSize
    this.hashVerified = !expectedHash || expectedHash === actualHash
    this.sizeVerified = !expectedByteSize || expectedByteSize === actualByteSize
    this.computedStreaming = computedStreaming
  }
}

export class CanonicalProductionFileReadResult {
  bytes: Uint8Array
  purpose: CanonicalFilePurpose
  evidence: CanonicalProductionFileVerificationEvidence
  metadataBlob: CanonicalMetadataBlob | null
  tombstoned: boolean

  constructor(
    bytes: Uint8Array,
    purpose: CanonicalFilePurpose,
    evidence: CanonicalProductionFileVerificationEvidence,
    metadataBlob: CanonicalMetadataBlob | null = null,
    tombstoned: boolean = false
  ) {
    this.bytes = bytes
    this.purpose = purpose
    this.evidence = evidence
    this.metadataBlob = metadataBlob
    this.tombstoned = tombstoned
  }
}

export class CanonicalProductionFileWriteResult {
  disposition: CanonicalFileWriteDisposition
  purpose: CanonicalFilePurpose
  evidence: CanonicalProductionFileVerificationEvidence
  rollbackCheckpointID: string | null
  tombstoned: boolean

  constructor(
    disposition: CanonicalFileWriteDisposition,
    purpose: CanonicalFilePurpose,
    evidence: CanonicalProductionFileVerificationEvidence,
    rollbackCheckpointID: string | null = null,
    tombstoned: boolean = false
  ) {
    this.disposition = disposition
    this.purpose = purpose
    this.evidence = evidence
    this.rollbackCheckpointID = rollbackCheckpointID
    this.tombstoned = tombstoned
  }
}

export class CanonicalProductionMetadataReadRequest {
  objectID: string
  reference: CanonicalFileReference

  constructor(objectID: string, reference: CanonicalFileReference) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.reference = reference
  }
}

export class CanonicalProductionArtifactReadRequest {
  artifactID: string
  reference: CanonicalFileReference
  expectedContentHash: CanonicalHash | null
  expectedByteSize: number | null

  constructor(
    artifactID: string,
    reference: CanonicalFileReference,
    expectedContentHash: CanonicalHash | null = null,
    expectedByteSize: number | null = null
  ) {
    this.artifactID = CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown')
    this.reference = reference
    this.expectedContentHash = expectedContentHash
    this.expectedByteSize = expectedByteSize
  }
}

export class CanonicalProductionArtifactVerifyRequest {
  reference: CanonicalFileReference
  expectedContentHash: CanonicalHash | null
  expectedByteSize: number | null
  requireStreamingHash: boolean

  constructor(
    reference: CanonicalFileReference,
    expectedContentHash: CanonicalHash | null = null,
    expectedByteSize: number | null = null,
    requireStreamingHash: boolean = true
  ) {
    this.reference = reference
    this.expectedContentHash = expectedContentHash
    this.expectedByteSize = expectedByteSize
    this.requireStreamingHash = requireStreamingHash
  }
}

export class CanonicalProductionHashRequest {
  reference: CanonicalFileReference
  requireStreaming: boolean
  expectedByteSize: number | null

  constructor(reference: CanonicalFileReference, requireStreaming: boolean = true, expectedByteSize: number | null = null) {
    this.reference = reference
    this.requireStreaming = requireStreaming
    this.expectedByteSize = expectedByteSize
  }
}

export class CanonicalProductionHashResult {
  contentHash: CanonicalHash
  byteSize: number
  computedStreaming: boolean
  evidence: CanonicalProductionFileVerificationEvidence

  constructor(
    contentHash: CanonicalHash,
    byteSize: number,
    computedStreaming: boolean,
    evidence: CanonicalProductionFileVerificationEvidence
  ) {
    this.contentHash = contentHash
    this.byteSize = byteSize
    this.computedStreaming = computedStreaming
    this.evidence = evidence
  }
}

export class CanonicalProductionTombstoneRequest {
  reference: CanonicalFileReference
  reason: string

  constructor(reference: CanonicalFileReference, reason: string) {
    this.reference = reference
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? 'softTombstone'
  }
}

export class CanonicalProductionFileRollbackRequest {
  checkpointID: string
  reference: CanonicalFileReference

  constructor(checkpointID: string, reference: CanonicalFileReference) {
    this.checkpointID = CanonicalProductionRedaction.safeIdentifier(checkpointID, 'rollback-checkpoint')
    this.reference = reference
  }
}

export class CanonicalProductionTransportBuildRequest {
  source: CanonicalNode
  destination: CanonicalNode
  route: string
  method: CanonicalProductionHTTPMethod
  existingRoutePath: string
  contentType: string
  body: Uint8Array
  timestamp: CanonicalTimestamp
  nonce: string
  requiresExternalSigner: boolean

  constructor(
    source: CanonicalNode,
    destination: CanonicalNode,
    route: string,
    method: CanonicalProductionHTTPMethod = CanonicalProductionHTTPMethod.POST,
    existingRoutePath: string,
    contentType: string = 'application/json',
    body: Uint8Array = new Uint8Array(0),
    timestamp: Date = new Date(),
    nonce: string,
    requiresExternalSigner: boolean = true
  ) {
    this.source = source
    this.destination = destination
    this.route = route
    this.method = method
    this.existingRoutePath = CanonicalProductionRedaction.safeDiagnosticText(existingRoutePath) ?? route
    this.contentType = CanonicalProductionRedaction.safeDiagnosticText(contentType) ?? 'application/json'
    this.body = body
    this.timestamp = new CanonicalTimestamp(timestamp)
    this.nonce = CanonicalProductionRedaction.safeIdentifier(nonce, 'nonce:required')
    this.requiresExternalSigner = requiresExternalSigner
  }
}

export class CanonicalProductionSignedRequest {
  buildRequest: CanonicalProductionTransportBuildRequest
  bodyHash: CanonicalHash
  signaturePrefix: string | null
  signerDescription: string | null

  constructor(
    buildRequest: CanonicalProductionTransportBuildRequest,
    bodyHash: CanonicalHash | null = null,
    signature: string | null = null,
    signerDescription: string | null = null
  ) {
    this.buildRequest = buildRequest
    this.bodyHash = bodyHash ?? CanonicalProductionRedaction.defaultHash(buildRequest.body)
    this.signaturePrefix = CanonicalProductionRedaction.hashPrefix(signature)
    this.signerDescription = CanonicalProductionRedaction.safeDiagnosticText(signerDescription)
  }
}

export class CanonicalProductionTransportExchangeResult {
  request: CanonicalProductionSignedRequest
  response: CanonicalTransportResponse
  responseVerified: boolean
  usedExistingRoute: boolean
  sideEffect: CanonicalProductionSideEffect | null

  constructor(
    request: CanonicalProductionSignedRequest,
    response: CanonicalTransportResponse,
    responseVerified: boolean,
    usedExistingRoute: boolean,
    sideEffect: CanonicalProductionSideEffect | null = null
  ) {
    this.request = request
    this.response = response
    this.responseVerified = responseVerified
    this.usedExistingRoute = usedExistingRoute
    this.sideEffect = sideEffect
  }
}

export class CanonicalProductionTransportVerification {
  route: string
  bodyHashVerified: boolean
  responseHashVerified: boolean
  timestampAccepted: boolean
  externalVerifierRequired: boolean

  constructor(
    route: string,
    bodyHashVerified: boolean,
    responseHashVerified: boolean,
    timestampAccepted: boolean,
    externalVerifierRequired: boolean
  ) {
    this.route = route
    this.bodyHashVerified = bodyHashVerified
    this.responseHashVerified = responseHashVerified
    this.timestampAccepted = timestampAccepted
    this.externalVerifierRequired = externalVerifierRequired
  }
}

export class CanonicalProductionManifestExchangeRequest {
  localManifest: CanonicalManifest
  peerNode: CanonicalNode
  trigger: string

  constructor(localManifest: CanonicalManifest, peerNode: CanonicalNode, trigger: string) {
    this.localManifest = localManifest
    this.peerNode = peerNode
    this.trigger = trigger
  }
}

export class CanonicalProductionArtifactRequest {
  objectID: string
  artifactID: string
  kind: CanonicalArtifactKind
  logicalPathToken: string | null

  constructor(objectID: string, artifactID: string, kind: CanonicalArtifactKind, logicalPathToken: string | null = null) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.artifactID = CanonicalProductionRedaction.safeIdentifier(artifactID, kind)
    this.kind = kind
    this.logicalPathToken = logicalPathToken ? safeLogicalPathToken(logicalPathToken) : null
  }
}

export class CanonicalProductionUploadCancelRequest {
  objectID: string
  sessionID: CanonicalUploadSessionID
  reason: string

  constructor(objectID: string, sessionID: CanonicalUploadSessionID, reason: string) {
    this.objectID = objectID
    this.sessionID = sessionID
    this.reason = reason
  }
}

export class CanonicalProductionUploadFailure {
  objectID: string
  code: string
  message: string | null

  constructor(objectID: string, code: string, message: string | null = null) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.code = CanonicalProductionRedaction.safeDiagnosticText(code) ?? 'unknown'
    this.message = CanonicalProductionRedaction.safeDiagnosticText(message)
  }
}

export class CanonicalProductionUploadFailureClassification {
  kind: CanonicalProductionUploadFailureKind
  retry: CanonicalRetryPolicySnapshot | null
  reason: string

  constructor(kind: CanonicalProductionUploadFailureKind, retry: CanonicalRetryPolicySnapshot | null, reason: string) {
    this.kind = kind
    this.retry = retry
    this.reason = reason
  }
}

export class CanonicalProductionUploadLedgerSnapshot {
  objectID: string
  sessionID: CanonicalUploadSessionID | null
  confirmedBytes: number
  totalBytes: number | null
  contentHashPrefix: string | null
  phase: CanonicalUploadSessionPhase | null
  retry: CanonicalRetryPolicySnapshot | null

  constructor(
    objectID: string,
    sessionID: CanonicalUploadSessionID | null = null,
    confirmedBytes: number = 0,
    totalBytes: number | null = null,
    contentHash: CanonicalHash | null = null,
    phase: CanonicalUploadSessionPhase | null = null,
    retry: CanonicalRetryPolicySnapshot | null = null
  ) {
    this.objectID = CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown-recording')
    this.sessionID = sessionID
    this.confirmedBytes = confirmedBytes
    this.totalBytes = totalBytes
    this.contentHashPrefix = contentHash ? CanonicalProductionRedaction.hashPrefix(contentHash.value) : null
    this.phase = phase
    this.retry = retry
  }
}

export class CanonicalProductionUploadRollbackRequest {
  objectID: string
  sessionID: CanonicalUploadSessionID | null
  checkpointID: string

  constructor(objectID: string, sessionID: CanonicalUploadSessionID | null, checkpointID: string) {
    this.objectID = objectID
    this.sessionID = sessionID
    this.checkpointID = checkpointID
  }
}

export class CanonicalProductionApplyExecutionRequest {
  action: CanonicalApplyAction
  rollbackCheckpointID: string | null

  constructor(action: CanonicalApplyAction, rollbackCheckpointID: string | null = null) {
    this.action = action
    this.rollbackCheckpointID = rollbackCheckpointID
  }
}

export class CanonicalProductionApplyPrecondition {
  actionID: string
  target: CanonicalApplyTarget
  expectedHashPrefix: string | null
  accepted: boolean
  reason: string | null

  constructor(
    actionID: string,
    target: CanonicalApplyTarget,
    expectedHashPrefix: string | null = null,
    accepted: boolean,
    reason: string | null = null
  ) {
    this.actionID = CanonicalProductionRedaction.safeIdentifier(actionID, 'apply-precondition')
    this.target = target
    this.expectedHashPrefix = CanonicalProductionRedaction.hashPrefix(expectedHashPrefix)
    this.accepted = accepted
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason)
  }
}

export class CanonicalProductionApplyPostcondition {
  actionID: string
  target: CanonicalApplyTarget
  actualHashPrefix: string | null
  accepted: boolean
  reason: string | null

  constructor(
    actionID: string,
    target: CanonicalApplyTarget,
    actualHashPrefix: string | null = null,
    accepted: boolean,
    reason: string | null = null
  ) {
    this.actionID = CanonicalProductionRedaction.safeIdentifier(actionID, 'apply-postcondition')
    this.target = target
    this.actualHashPrefix = CanonicalProductionRedaction.hashPrefix(actualHashPrefix)
    this.accepted = accepted
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason)
  }
}

export class CanonicalProductionApplyResult {
  actionID: string
  status: string
  precondition: CanonicalProductionApplyPrecondition | null
  postcondition: CanonicalProductionApplyPostcondition | null
  sideEffect: CanonicalProductionSideEffect | null
  rollbackCheckpointID: string | null

  constructor(
    actionID: string,
    status: string,
    precondition: CanonicalProductionApplyPrecondition | null = null,
    postcondition: CanonicalProductionApplyPostcondition | null = null,
    sideEffect: CanonicalProductionSideEffect | null = null,
    rollbackCheckpointID: string | null = null
  ) {
    this.actionID = actionID
    this.status = status
    this.precondition = precondition
    this.postcondition = postcondition
    this.sideEffect = sideEffect
    this.rollbackCheckpointID = rollbackCheckpointID
  }
}

export class CanonicalProductionPortSet {
  file: CanonicalProductionFilePort | null
  transport: CanonicalProductionTransportPort | null
  upload: CanonicalProductionUploadPort | null
  apply: CanonicalProductionApplyPort | null
  syncClock: CanonicalProductionSyncClockPort | null
  diagnostics: CanonicalProductionDiagnosticsPort | null
  capability: CanonicalProductionCapabilityPort | null

  constructor(
    file: CanonicalProductionFilePort | null = null,
    transport: CanonicalProductionTransportPort | null = null,
    upload: CanonicalProductionUploadPort | null = null,
    apply: CanonicalProductionApplyPort | null = null,
    syncClock: CanonicalProductionSyncClockPort | null = null,
    diagnostics: CanonicalProductionDiagnosticsPort | null = null,
    capability: CanonicalProductionCapabilityPort | null = null
  ) {
    this.file = file
    this.transport = transport
    this.upload = upload
    this.apply = apply
    this.syncClock = syncClock
    this.diagnostics = diagnostics
    this.capability = capability
  }

  get missingRequiredPorts(): CanonicalProductionPortKind[] {
    const missing: CanonicalProductionPortKind[] = []
    if (!this.file) { missing.push(CanonicalProductionPortKind.FILE) }
    if (!this.transport) { missing.push(CanonicalProductionPortKind.TRANSPORT) }
    if (!this.upload) { missing.push(CanonicalProductionPortKind.UPLOAD) }
    if (!this.apply) { missing.push(CanonicalProductionPortKind.APPLY) }
    return missing
  }

  readiness(generatedAt: Date = new Date()): CanonicalProductionPortReadiness {
    const declared = new Map<CanonicalProductionPortKind, boolean>()
    declared.set(CanonicalProductionPortKind.FILE, this.file !== null)
    declared.set(CanonicalProductionPortKind.TRANSPORT, this.transport !== null)
    declared.set(CanonicalProductionPortKind.UPLOAD, this.upload !== null)
    declared.set(CanonicalProductionPortKind.APPLY, this.apply !== null)
    declared.set(CanonicalProductionPortKind.SYNC_CLOCK, this.syncClock !== null)
    declared.set(CanonicalProductionPortKind.DIAGNOSTICS, this.diagnostics !== null)
    declared.set(CanonicalProductionPortKind.CAPABILITY, this.capability !== null)
    const dryRunOnly = (this.file?.isDryRunOnly ?? true) &&
      (this.transport?.isDryRunOnly ?? true) &&
      (this.upload?.isDryRunOnly ?? true) &&
      (this.apply?.isDryRunOnly ?? true) &&
      !(this.transport?.realNetworkExecutionEnabled ?? false)
    return new CanonicalProductionPortReadiness(declared, this.missingRequiredPorts, dryRunOnly, generatedAt)
  }
}

// =============================================================================
// INTERFACES (Protocol-like)
// =============================================================================

export interface CanonicalProductionFilePort {
  isDryRunOnly: boolean
  capabilities: CanonicalProductionCapability[]
}

export interface CanonicalProductionTransportPort {
  isDryRunOnly: boolean
  routeCapabilities: CanonicalProductionTransportRouteCapability[]
  realNetworkExecutionEnabled: boolean
}

export interface CanonicalProductionUploadPort {
  isDryRunOnly: boolean
  resumableSessionSupported: boolean
  chunkSizePolicy: number
}

export interface CanonicalProductionApplyPort {
  isDryRunOnly: boolean
  metadataApplySupported: boolean
  generatedArtifactApplySupported: boolean
  tombstoneApplySupported: boolean
  conflictRecordSupported: boolean
}

export interface CanonicalProductionSyncClockPort {
}

export interface CanonicalProductionDiagnosticsPort {
}

export interface CanonicalProductionCapabilityPort {
}

// =============================================================================
// REDACTION UTILITIES
// =============================================================================

export class CanonicalProductionRedaction {
  static hashPrefix(value: string | null): string | null {
    if (!value) {
      return null
    }
    const normalized = value.trim().toLowerCase()
    if (normalized.length === 0) {
      return null
    }
    return normalized.substring(0, 12)
  }

  static safeIdentifier(value: string, fallback: string): string {
    return CanonicalProductionRedaction.safeDiagnosticText(value) ?? fallback
  }

  static safeFileName(value: string | null): string | null {
    if (!value) {
      return null
    }
    const text = CanonicalProductionRedaction.safeDiagnosticText(value)
    if (!text) {
      return null
    }
    if (text.includes('/') || text.includes('\\')) {
      return 'redacted-file-' + (CanonicalProductionRedaction.hashPrefix(CanonicalHash.sha256OfString(text).value) ?? 'unknown')
    }
    return text
  }

  static safeDiagnosticText(value: string | null): string | null {
    if (!value) {
      return null
    }
    const trimmed = value.trim().replace(/\n/g, ' ').replace(/\r/g, ' ')
    if (trimmed.length === 0) {
      return null
    }
    if (CanonicalProductionRedaction.containsSensitivePathSignal(trimmed)) {
      return 'redacted-' + (CanonicalProductionRedaction.hashPrefix(CanonicalHash.sha256OfString(trimmed).value) ?? 'diagnostic')
    }
    return trimmed.substring(0, 160)
  }

  static containsSensitivePathSignal(value: string): boolean {
    const lowercased = value.toLowerCase()
    return lowercased.includes('file://') ||
      lowercased.includes('/users/') ||
      lowercased.includes('/private/') ||
      lowercased.includes('\\') ||
      lowercased.startsWith('~')
  }

  static defaultHash(data: Uint8Array): CanonicalHash {
    return CanonicalHash.sha256(data)
  }
}

// =============================================================================
// FORWARD REFERENCES (minimal stubs for types defined elsewhere)
// =============================================================================

export enum CanonicalTransportRoute {
  MANIFEST_EXCHANGE = 'manifestExchange',
  APPLY_PLAN = 'applyPlan',
  APPLY_METADATA = 'applyMetadata',
  FILE_READ = 'fileRead',
  UPLOAD_START = 'uploadStart',
  UPLOAD_STATUS = 'uploadStatus',
  UPLOAD_CHUNK = 'uploadChunk',
  UPLOAD_FINALIZE = 'uploadFinalize'
}

export enum CanonicalFilePurpose {
  ARTIFACT_BYTES = 'artifactBytes',
  GENERATED_ARTIFACT = 'generatedArtifact',
  METADATA_BLOB = 'metadataBlob',
  TOMBSTONE_MARKER = 'tombstoneMarker'
}

export enum CanonicalFileWriteDisposition {
  CREATED = 'created',
  REPLACED = 'replaced',
  ACCEPTED_EXISTING = 'acceptedExisting',
  TOMBSTONE_MARKED = 'tombstoneMarked'
}

export class CanonicalApplyTarget {
  objectID: string
  artifactID: string | null
  artifactKind: string | null

  constructor(objectID: string, artifactID: string | null = null, artifactKind: string | null = null) {
    this.objectID = objectID
    this.artifactID = artifactID
    this.artifactKind = artifactKind
  }
}

export class CanonicalApplyAction {
  actionID: string
  kind: string
  target: CanonicalApplyTarget
  bridgeHint: string | null
  reason: string

  constructor(
    actionID: string,
    kind: string,
    target: CanonicalApplyTarget,
    bridgeHint: string | null = null,
    reason: string = ''
  ) {
    this.actionID = actionID
    this.kind = kind
    this.target = target
    this.bridgeHint = bridgeHint
    this.reason = reason
  }

  get id(): string {
    return this.actionID
  }
}

export class CanonicalTransportResponse {
  ok: boolean
  status: string
  body: Uint8Array
  bodyHash: CanonicalHash
  error: string | null

  constructor(ok: boolean, status: string, body: Uint8Array = new Uint8Array(0), error: string | null = null) {
    this.ok = ok
    this.status = status
    this.body = body
    this.bodyHash = CanonicalHash.sha256(body)
    this.error = error
  }
}

export class CanonicalProductionSideEffect {
  kind: string
  domain: string
  objectID: string | null
  artifactID: string | null
  route: string | null
  byteSize: number | null
  hashPrefix: string | null
  redactedSummary: string

  constructor(
    kind: string,
    domain: string,
    objectID: string | null = null,
    artifactID: string | null = null,
    route: string | null = null,
    byteSize: number | null = null,
    hash: CanonicalHash | null = null,
    hashPrefixOverride: string | null = null,
    summary: string = ''
  ) {
    this.kind = kind
    this.domain = domain
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown') : null
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.route = route
    this.byteSize = byteSize
    this.hashPrefix = hash
      ? CanonicalProductionRedaction.hashPrefix(hash.value)
      : CanonicalProductionRedaction.hashPrefix(hashPrefixOverride)
    this.redactedSummary = CanonicalProductionRedaction.safeDiagnosticText(summary) ?? kind
  }

  get id(): string {
    return [this.kind, this.domain, this.objectID ?? '', this.artifactID ?? '', this.route ?? ''].join('|')
  }
}

export class CanonicalUploadSessionID {
  rawValue: string

  constructor(rawValue: string) {
    this.rawValue = (nilIfEmpty(rawValue.trim()) ?? 'upload-session:unknown')
  }
}

export enum CanonicalUploadSessionPhase {
  ACTIVE = 'active',
  RETRY_PENDING = 'retryPending',
  FINALIZING = 'finalizing',
  COMPLETED = 'completed',
  CONFLICT = 'conflict',
  FAILED = 'failed'
}

export class CanonicalRetryPolicySnapshot {
  retryCount: number
  nextRetryAt: CanonicalTimestamp | null
  maxAttempts: number

  constructor(retryCount: number, nextRetryAt: CanonicalTimestamp | null, maxAttempts: number) {
    this.retryCount = retryCount
    this.nextRetryAt = nextRetryAt
    this.maxAttempts = maxAttempts
  }
}

export class CanonicalFileReference {
  rootToken: string
  logicalPathToken: string
  artifactID: string | null
  artifactKind: string | null

  constructor(
    rootToken: string,
    logicalPathToken: string,
    artifactID: string | null = null,
    artifactKind: string | null = null
  ) {
    this.rootToken = rootToken
    this.logicalPathToken = logicalPathToken
    this.artifactID = (artifactID ? nilIfEmpty(artifactID.trim()) : null) ?? null
    this.artifactKind = artifactKind
  }
}

export class CanonicalPathResolutionResult {
  rootToken: string
  logicalPathToken: string
  resolvedPathToken: string
  isInsideRoot: boolean

  constructor(rootToken: string, logicalPathToken: string, resolvedPathToken: string, isInsideRoot: boolean) {
    this.rootToken = rootToken
    this.logicalPathToken = logicalPathToken
    this.resolvedPathToken = resolvedPathToken
    this.isInsideRoot = isInsideRoot
  }
}

export class CanonicalMetadataBlob {
  fields: Record<string, string>

  constructor(fields: Record<string, string> = {}) {
    this.fields = {}
    for (const key of Object.keys(fields)) {
      const trimmedKey = key.trim()
      if (trimmedKey.length > 0) {
        this.fields[trimmedKey] = fields[key]
      }
    }
  }
}

// Forward declarations for types that come from other files
export class CanonicalTransferProjection {
  jobs: CanonicalTransferJob[] = []
}

export class CanonicalTransferJob {
  objectID: string = ''
  phase: string = ''
}

export class CanonicalInventoryCoverageReport {
  recordingCoverage: number = 0
  audioCoverage: number = 0
  generatedArtifactCoverage: number = 0
  folderCoverage: number = 0
  studyItemCoverage: number = 0
  tombstoneCoverage: number = 0
  unsupportedLegacyObjectCount: number = 0
  fallbackRequiredCount: number = 0

  constructor(
    rc: number = 0, ac: number = 0, gc: number = 0,
    fc: number = 0, sc: number = 0, tc: number = 0,
    uc: number = 0, frc: number = 0
  ) {
    this.recordingCoverage = rc
    this.audioCoverage = ac
    this.generatedArtifactCoverage = gc
    this.folderCoverage = fc
    this.studyItemCoverage = sc
    this.tombstoneCoverage = tc
    this.unsupportedLegacyObjectCount = uc
    this.fallbackRequiredCount = frc
  }
}

export class CanonicalRetirementReadinessReport {
  nodeID: string = ''
  readinessStates: string[] = []

  constructor(nodeID: string = '', readinessStates: string[] = []) {
    this.nodeID = nodeID
    this.readinessStates = readinessStates
  }
}
