/**
 * CanonicalDryRunMigrationPlanner — dry-run equivalence analysis and migration planning.
 * Ported from Rokurics-Apple (CanonicalDryRunMigrationPlanner.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalManifest, nilIfEmpty
} from './CanonicalCore'
import {
  CanonicalProductionDomain, CanonicalProductionPortKind,
  CanonicalProductionDiagnosticsEvent, CanonicalProductionDiagnosticEventKind,
  CanonicalProductionPortReadiness, CanonicalProductionPortSet,
  CanonicalProductionRedaction, CanonicalProductionSnapshot,
  CanonicalLegacyActionSnapshot
} from './CanonicalProductionPorts'
import {
  CanonicalRuntimeReadinessReport, CanonicalRuntimeReadinessEvaluator,
  CanonicalRuntimeReadinessEvidence, CanonicalSyncPlan, CanonicalApplyPlan,
  CanonicalLibrarySyncPlan
} from './CanonicalRuntimeHarness'

// =============================================================================
// LEGACY EQUIVALENCE
// =============================================================================

export enum CanonicalLegacyEquivalenceDomain {
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
  UI_INTEGRATION = 'uiIntegration'
}

export enum CanonicalLegacyEquivalenceStatus {
  EQUIVALENT = 'equivalent',
  CANONICAL_MORE_CONSERVATIVE = 'canonicalMoreConservative',
  CANONICAL_MORE_AGGRESSIVE = 'canonicalMoreAggressive',
  LEGACY_ONLY = 'legacyOnly',
  CANONICAL_ONLY = 'canonicalOnly',
  UNSUPPORTED = 'unsupported',
  CONFLICT = 'conflict',
  BLOCKED = 'blocked',
  UNKNOWN = 'unknown'
}

export enum CanonicalLegacyDivergenceSeverity {
  INFO = 'info',
  WARNING = 'warning',
  BLOCKING = 'blocking'
}

export class CanonicalLegacyDivergence {
  domain: CanonicalLegacyEquivalenceDomain
  status: CanonicalLegacyEquivalenceStatus
  severity: CanonicalLegacyDivergenceSeverity
  reason: string
  canonicalActionIDs: string[]
  legacyActionIDs: string[]
  hashPrefix: string | null

  constructor(
    domain: CanonicalLegacyEquivalenceDomain,
    status: CanonicalLegacyEquivalenceStatus,
    severity: CanonicalLegacyDivergenceSeverity,
    reason: string,
    canonicalActionIDs: string[] = [],
    legacyActionIDs: string[] = [],
    hash: CanonicalHash | null = null
  ) {
    this.domain = domain
    this.status = status
    this.severity = severity
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? status
    this.canonicalActionIDs = CanonicalLegacyDivergence.normalized(canonicalActionIDs)
    this.legacyActionIDs = CanonicalLegacyDivergence.normalized(legacyActionIDs)
    this.hashPrefix = hash ? CanonicalProductionRedaction.hashPrefix(hash.value) : null
  }

  get id(): string {
    return [this.domain, this.status, this.severity, this.reason].join('|')
  }

  get isBlocking(): boolean {
    return this.severity === CanonicalLegacyDivergenceSeverity.BLOCKING
  }

  private static normalized(ids: string[]): string[] {
    return Array.from(new Set(
      ids.map(id => CanonicalProductionRedaction.safeDiagnosticText(id)).filter((id): id is string => id !== null)
    )).sort()
  }
}

export class CanonicalLegacyEquivalenceDomainReport {
  domain: CanonicalLegacyEquivalenceDomain
  status: CanonicalLegacyEquivalenceStatus
  canonicalActionIDs: string[]
  legacyActionIDs: string[]
  divergences: CanonicalLegacyDivergence[]

  constructor(
    domain: CanonicalLegacyEquivalenceDomain,
    status: CanonicalLegacyEquivalenceStatus,
    canonicalActionIDs: string[] = [],
    legacyActionIDs: string[] = [],
    divergences: CanonicalLegacyDivergence[] = []
  ) {
    this.domain = domain
    this.status = status
    this.canonicalActionIDs = CanonicalLegacyEquivalenceDomainReport.normalized(canonicalActionIDs)
    this.legacyActionIDs = CanonicalLegacyEquivalenceDomainReport.normalized(legacyActionIDs)
    this.divergences = divergences
  }

  get id(): string {
    return this.domain
  }

  get isBlocking(): boolean {
    return this.divergences.some(d => d.isBlocking) ||
      [CanonicalLegacyEquivalenceStatus.CANONICAL_MORE_AGGRESSIVE,
      CanonicalLegacyEquivalenceStatus.LEGACY_ONLY,
      CanonicalLegacyEquivalenceStatus.CANONICAL_ONLY,
      CanonicalLegacyEquivalenceStatus.UNSUPPORTED,
      CanonicalLegacyEquivalenceStatus.CONFLICT,
      CanonicalLegacyEquivalenceStatus.BLOCKED,
      CanonicalLegacyEquivalenceStatus.UNKNOWN].indexOf(this.status) >= 0
  }

  private static normalized(ids: string[]): string[] {
    return Array.from(new Set(
      ids.map(id => CanonicalProductionRedaction.safeDiagnosticText(id)).filter((id): id is string => id !== null)
    )).sort()
  }
}

export class CanonicalLegacyEquivalenceReport {
  generatedAt: CanonicalTimestamp
  domainReports: CanonicalLegacyEquivalenceDomainReport[]
  divergences: CanonicalLegacyDivergence[]
  allEquivalent: boolean
  hasBlockingDivergence: boolean

  constructor(domainReports: CanonicalLegacyEquivalenceDomainReport[], generatedAt: Date = new Date()) {
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.domainReports = domainReports.sort((a, b) => a.domain.localeCompare(b.domain))
    this.divergences = domainReports.flatMap(r => r.divergences).sort((a, b) => a.id.localeCompare(b.id))
    this.allEquivalent = domainReports.every(r =>
      r.status === CanonicalLegacyEquivalenceStatus.EQUIVALENT ||
      r.status === CanonicalLegacyEquivalenceStatus.CANONICAL_MORE_CONSERVATIVE
    )
    this.hasBlockingDivergence = domainReports.some(r => r.isBlocking)
  }

  status(domain: CanonicalLegacyEquivalenceDomain): CanonicalLegacyEquivalenceStatus {
    return this.domainReports.find(r => r.domain === domain)?.status ?? CanonicalLegacyEquivalenceStatus.UNKNOWN
  }
}

// =============================================================================
// DRY RUN EQUIVALENCE
// =============================================================================

export class CanonicalDryRunEquivalenceReport {
  legacyEquivalence: CanonicalLegacyEquivalenceReport
  equivalentDomains: CanonicalLegacyEquivalenceDomain[]
  divergentDomains: CanonicalLegacyEquivalenceDomain[]

  constructor(legacyEquivalence: CanonicalLegacyEquivalenceReport) {
    this.legacyEquivalence = legacyEquivalence
    this.equivalentDomains = legacyEquivalence.domainReports
      .filter(r => r.status === CanonicalLegacyEquivalenceStatus.EQUIVALENT ||
        r.status === CanonicalLegacyEquivalenceStatus.CANONICAL_MORE_CONSERVATIVE)
      .map(r => r.domain).sort((a, b) => a.localeCompare(b))
    this.divergentDomains = legacyEquivalence.domainReports
      .filter(r => r.isBlocking)
      .map(r => r.domain).sort((a, b) => a.localeCompare(b))
  }
}

// =============================================================================
// DRY RUN ACTION
// =============================================================================

export enum CanonicalDryRunActionKind {
  WOULD_NO_OP = 'wouldNoOp',
  WOULD_UPLOAD = 'wouldUpload',
  WOULD_DOWNLOAD = 'wouldDownload',
  WOULD_APPLY = 'wouldApply',
  WOULD_SEND = 'wouldSend',
  WOULD_RECORD_CONFLICT = 'wouldRecordConflict',
  WOULD_SUPPRESS = 'wouldSuppress'
}

export class CanonicalDryRunAction {
  actionID: string
  domain: CanonicalProductionDomain
  kind: CanonicalDryRunActionKind
  objectID: string | null
  artifactID: string | null
  reason: string
  suppressedBecauseDryRun: boolean

  constructor(
    domain: CanonicalProductionDomain,
    kind: CanonicalDryRunActionKind,
    objectID: string | null = null,
    artifactID: string | null = null,
    reason: string
  ) {
    this.domain = domain
    this.kind = kind
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown') : null
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? kind
    this.suppressedBecauseDryRun = true
    this.actionID = [domain, kind, this.objectID ?? '', this.artifactID ?? '', this.reason].join('|')
  }

  get id(): string {
    return this.actionID
  }
}

// =============================================================================
// DRY RUN RISK & BLOCKER
// =============================================================================

export enum CanonicalDryRunRiskKind {
  DRY_RUN_ONLY = 'dryRunOnly',
  LEGACY_RUNTIME_STILL_OWNER = 'legacyRuntimeStillOwner',
  CANONICAL_MORE_AGGRESSIVE = 'canonicalMoreAggressive',
  CANONICAL_ONLY = 'canonicalOnly',
  LEGACY_ONLY = 'legacyOnly',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  UNSUPPORTED_OBJECT = 'unsupportedObject'
}

export class CanonicalDryRunRisk {
  domain: CanonicalProductionDomain
  kind: CanonicalDryRunRiskKind
  reason: string

  constructor(domain: CanonicalProductionDomain, kind: CanonicalDryRunRiskKind, reason: string) {
    this.domain = domain
    this.kind = kind
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? kind
  }

  get id(): string {
    return [this.domain, this.kind, this.reason].join('|')
  }
}

export enum CanonicalDryRunBlockerKind {
  MISSING_PRODUCTION_FILE_PORT = 'missingProductionFilePort',
  MISSING_PRODUCTION_TRANSPORT_PORT = 'missingProductionTransportPort',
  MISSING_PRODUCTION_UPLOAD_PORT = 'missingProductionUploadPort',
  MISSING_PRODUCTION_APPLY_PORT = 'missingProductionApplyPort',
  DRY_RUN_DIVERGENCE = 'dryRunDivergence',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  UNSUPPORTED_OBJECT = 'unsupportedObject',
  FULL_CONTENT_LEAK = 'fullContentLeak',
  ROUTE_BYPASS_RISK = 'routeBypassRisk',
  PATH_ESCAPE_RISK = 'pathEscapeRisk',
  UI_LEGACY_RUNTIME = 'uiLegacyRuntime',
  RETRY_RUNTIME_NOT_MIGRATED = 'retryRuntimeNotMigrated',
  MAC_PENDING_SYNC_LEGACY = 'macPendingSyncLegacy',
  USER_DATA_MIGRATION_NOT_DESIGNED = 'userDataMigrationNotDesigned'
}

export class CanonicalDryRunBlocker {
  domain: CanonicalProductionDomain
  kind: CanonicalDryRunBlockerKind
  reason: string

  constructor(domain: CanonicalProductionDomain, kind: CanonicalDryRunBlockerKind, reason: string) {
    this.domain = domain
    this.kind = kind
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? kind
  }

  get id(): string {
    return [this.domain, this.kind, this.reason].join('|')
  }
}

// =============================================================================
// DRY RUN MIGRATION CONTEXT
// =============================================================================

export class CanonicalDryRunMigrationContext {
  dryRunID: string
  legacyRuntimeStillProductionOwner: boolean
  retryRuntimeMigrated: boolean
  macPendingSyncMigrated: boolean
  userDataMigrationDesigned: boolean
  uiIntegrationMigrated: boolean

  constructor(
    dryRunID: string = CanonicalDryRunMigrationContext.generateUUID(),
    legacyRuntimeStillProductionOwner: boolean = true,
    retryRuntimeMigrated: boolean = false,
    macPendingSyncMigrated: boolean = false,
    userDataMigrationDesigned: boolean = false,
    uiIntegrationMigrated: boolean = false
  ) {
    this.dryRunID = CanonicalProductionRedaction.safeIdentifier(dryRunID, CanonicalDryRunMigrationContext.generateUUID())
    this.legacyRuntimeStillProductionOwner = legacyRuntimeStillProductionOwner
    this.retryRuntimeMigrated = retryRuntimeMigrated
    this.macPendingSyncMigrated = macPendingSyncMigrated
    this.userDataMigrationDesigned = userDataMigrationDesigned
    this.uiIntegrationMigrated = uiIntegrationMigrated
  }

  private static generateUUID(): string {
    return 'dry-run-' + Math.random().toString(36).substring(2, 10) + Date.now().toString(36)
  }
}

// =============================================================================
// DRY RUN READINESS REPORT
// =============================================================================

export enum CanonicalRuntimeReadinessStatus {
  NOT_EVALUATED = 'notEvaluated',
  OFFLINE_KERNEL_READY = 'offlineKernelReady',
  PRODUCTION_PORTS_DECLARED = 'productionPortsDeclared',
  DRY_RUN_AVAILABLE = 'dryRunAvailable',
  DRY_RUN_EQUIVALENT = 'dryRunEquivalent',
  PRODUCTION_ADAPTER_MISSING = 'productionAdapterMissing',
  PRODUCTION_BLOCKED = 'productionBlocked',
  ELIGIBLE_FOR_MANUAL_MIGRATION_DESIGN = 'eligibleForManualMigrationDesign'
}

export enum CanonicalRuntimeReadinessDomain {
  FILE_RUNTIME = 'fileRuntime',
  TRANSPORT_RUNTIME = 'transportRuntime',
  UPLOAD_RUNTIME = 'uploadRuntime',
  APPLY_EXECUTOR = 'applyExecutor',
  CONFLICT_RESOLVER = 'conflictResolver',
  SIMULATION_HARNESS = 'simulationHarness'
}

export class CanonicalDryRunReadinessReport {
  generatedAt: CanonicalTimestamp
  states: CanonicalRuntimeReadinessStatus[]
  portReadiness: CanonicalProductionPortReadiness
  blockers: CanonicalDryRunBlocker[]
  eligibleForRuntimeSwitch: boolean
  retired: boolean

  constructor(
    states: CanonicalRuntimeReadinessStatus[],
    portReadiness: CanonicalProductionPortReadiness,
    blockers: CanonicalDryRunBlocker[],
    eligibleForRuntimeSwitch: boolean = false,
    retired: boolean = false,
    generatedAt: Date = new Date()
  ) {
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.states = Array.from(new Set(states)).sort((a, b) => a.localeCompare(b))
    this.portReadiness = portReadiness
    this.blockers = blockers.sort((a, b) => a.id.localeCompare(b.id))
    this.eligibleForRuntimeSwitch = eligibleForRuntimeSwitch
    this.retired = retired
  }

  get productionMigrationBlocked(): boolean {
    return this.states.indexOf(CanonicalRuntimeReadinessStatus.PRODUCTION_BLOCKED) >= 0 ||
      this.blockers.length > 0 || !this.eligibleForRuntimeSwitch
  }
}

// =============================================================================
// DRY RUN MIGRATION PLAN
// =============================================================================

export class CanonicalDryRunMigrationPlan {
  dryRunID: string
  trigger: string
  syncPlan: CanonicalSyncPlan
  applyPlan: CanonicalApplyPlan
  libraryPlan: CanonicalLibrarySyncPlan
  actions: CanonicalDryRunAction[]
  risks: CanonicalDryRunRisk[]
  blockers: CanonicalDryRunBlocker[]
  equivalenceReport: CanonicalDryRunEquivalenceReport
  readinessReport: CanonicalDryRunReadinessReport
  diagnostics: CanonicalProductionDiagnosticsEvent[]

  constructor(
    dryRunID: string,
    trigger: string,
    syncPlan: CanonicalSyncPlan,
    applyPlan: CanonicalApplyPlan,
    libraryPlan: CanonicalLibrarySyncPlan,
    actions: CanonicalDryRunAction[],
    risks: CanonicalDryRunRisk[],
    blockers: CanonicalDryRunBlocker[],
    equivalenceReport: CanonicalDryRunEquivalenceReport,
    readinessReport: CanonicalDryRunReadinessReport,
    diagnostics: CanonicalProductionDiagnosticsEvent[]
  ) {
    this.dryRunID = dryRunID
    this.trigger = trigger
    this.syncPlan = syncPlan
    this.applyPlan = applyPlan
    this.libraryPlan = libraryPlan
    this.actions = actions
    this.risks = risks
    this.blockers = blockers
    this.equivalenceReport = equivalenceReport
    this.readinessReport = readinessReport
    this.diagnostics = diagnostics
  }
}

// =============================================================================
// DRY RUN MIGRATION PLANNER
// =============================================================================

export class CanonicalDryRunMigrationPlanner {
  plan(
    local: CanonicalProductionSnapshot,
    peer: CanonicalProductionSnapshot,
    ports: CanonicalProductionPortSet,
    currentRuntimeReadiness: CanonicalRuntimeReadinessReport,
    trigger: string,
    context: CanonicalDryRunMigrationContext = new CanonicalDryRunMigrationContext(),
    generatedAt: Date = new Date()
  ): CanonicalDryRunMigrationPlan {
    const resolvedTrigger = trigger
    const syncPlan = new CanonicalSyncPlan()
    const applyPlan = new CanonicalApplyPlan()
    const libraryPlan = new CanonicalLibrarySyncPlan()
    const portReadiness = ports.readiness(generatedAt)
    const equivalence = CanonicalDryRunMigrationPlanner.equivalenceReport(
      syncPlan, applyPlan, libraryPlan, local.legacyActions, portReadiness, generatedAt
    )
    const dryRunEquivalence = new CanonicalDryRunEquivalenceReport(equivalence)
    const blockers = CanonicalDryRunMigrationPlanner.blockers(
      portReadiness, equivalence, local, peer, context
    )
    const risks = CanonicalDryRunMigrationPlanner.risks(equivalence, blockers)
    const readiness = CanonicalDryRunMigrationPlanner.readiness(
      currentRuntimeReadiness, portReadiness, equivalence, blockers, generatedAt
    )
    const diagnostics = CanonicalDryRunMigrationPlanner.diagnostics(
      context.dryRunID, equivalence, readiness, blockers, generatedAt
    )
    return new CanonicalDryRunMigrationPlan(
      context.dryRunID, resolvedTrigger, syncPlan, applyPlan, libraryPlan,
      [], risks, blockers, dryRunEquivalence, readiness, diagnostics
    )
  }

  static equivalenceReport(
    syncPlan: CanonicalSyncPlan,
    applyPlan: CanonicalApplyPlan,
    libraryPlan: CanonicalLibrarySyncPlan,
    localLegacyActions: CanonicalLegacyActionSnapshot,
    portReadiness: CanonicalProductionPortReadiness,
    generatedAt: Date = new Date()
  ): CanonicalLegacyEquivalenceReport {
    const reports = Object.values(CanonicalLegacyEquivalenceDomain).map(domain => {
      return new CanonicalLegacyEquivalenceDomainReport(
        domain, CanonicalLegacyEquivalenceStatus.UNKNOWN, [], []
      )
    })
    return new CanonicalLegacyEquivalenceReport(reports, generatedAt)
  }

  static blockers(
    portReadiness: CanonicalProductionPortReadiness,
    equivalence: CanonicalLegacyEquivalenceReport,
    local: CanonicalProductionSnapshot,
    peer: CanonicalProductionSnapshot,
    context: CanonicalDryRunMigrationContext
  ): CanonicalDryRunBlocker[] {
    const blockers: CanonicalDryRunBlocker[] = []
    if (portReadiness.missingPorts.indexOf(CanonicalProductionPortKind.FILE) >= 0) {
      blockers.push(new CanonicalDryRunBlocker(
        CanonicalProductionDomain.FILE_RUNTIME,
        CanonicalDryRunBlockerKind.MISSING_PRODUCTION_FILE_PORT, 'filePortMissing'
      ))
    }
    blockers.push(...equivalence.divergences.filter(d => d.isBlocking).map(d => {
      return new CanonicalDryRunBlocker(
        CanonicalProductionDomain.APPLY, CanonicalDryRunBlockerKind.DRY_RUN_DIVERGENCE, d.reason
      )
    }))
    if (!context.uiIntegrationMigrated) {
      blockers.push(new CanonicalDryRunBlocker(
        CanonicalProductionDomain.UI_INTEGRATION,
        CanonicalDryRunBlockerKind.UI_LEGACY_RUNTIME, 'legacyUIStillRuntimeOwner'
      ))
    }
    if (!context.retryRuntimeMigrated) {
      blockers.push(new CanonicalDryRunBlocker(
        CanonicalProductionDomain.UPLOAD_RUNTIME,
        CanonicalDryRunBlockerKind.RETRY_RUNTIME_NOT_MIGRATED, 'legacyRetryRuntimePreserved'
      ))
    }
    if (!context.macPendingSyncMigrated) {
      blockers.push(new CanonicalDryRunBlocker(
        CanonicalProductionDomain.TRANSPORT_RUNTIME,
        CanonicalDryRunBlockerKind.MAC_PENDING_SYNC_LEGACY, 'macPendingSyncStillLegacy'
      ))
    }
    if (!context.userDataMigrationDesigned) {
      blockers.push(new CanonicalDryRunBlocker(
        CanonicalProductionDomain.INVENTORY,
        CanonicalDryRunBlockerKind.USER_DATA_MIGRATION_NOT_DESIGNED, 'noUserDataMigrationDesign'
      ))
    }
    const deduped = new Map<string, CanonicalDryRunBlocker>()
    for (const b of blockers) {
      if (!deduped.has(b.id)) {
        deduped.set(b.id, b)
      }
    }
    return Array.from(deduped.values()).sort((a, b) => a.id.localeCompare(b.id))
  }

  static risks(
    equivalence: CanonicalLegacyEquivalenceReport,
    blockers: CanonicalDryRunBlocker[]
  ): CanonicalDryRunRisk[] {
    const risks = [
      new CanonicalDryRunRisk(CanonicalProductionDomain.APPLY, CanonicalDryRunRiskKind.DRY_RUN_ONLY, 'productionMutationSuppressed')
    ]
    risks.push(...equivalence.divergences.map(d => {
      let kind: CanonicalDryRunRiskKind
      switch (d.status) {
        case CanonicalLegacyEquivalenceStatus.CANONICAL_MORE_AGGRESSIVE:
          kind = CanonicalDryRunRiskKind.CANONICAL_MORE_AGGRESSIVE
          break
        case CanonicalLegacyEquivalenceStatus.CANONICAL_ONLY:
          kind = CanonicalDryRunRiskKind.CANONICAL_ONLY
          break
        case CanonicalLegacyEquivalenceStatus.LEGACY_ONLY:
          kind = CanonicalDryRunRiskKind.LEGACY_ONLY
          break
        case CanonicalLegacyEquivalenceStatus.CONFLICT:
          kind = CanonicalDryRunRiskKind.UNRESOLVED_CONFLICT
          break
        case CanonicalLegacyEquivalenceStatus.UNSUPPORTED:
          kind = CanonicalDryRunRiskKind.UNSUPPORTED_OBJECT
          break
        default:
          kind = CanonicalDryRunRiskKind.LEGACY_RUNTIME_STILL_OWNER
          break
      }
      return new CanonicalDryRunRisk(CanonicalProductionDomain.APPLY, kind, d.reason)
    }))
    const deduped = new Map<string, CanonicalDryRunRisk>()
    for (const r of risks) {
      if (!deduped.has(r.id)) {
        deduped.set(r.id, r)
      }
    }
    return Array.from(deduped.values()).sort((a, b) => a.id.localeCompare(b.id))
  }

  static readiness(
    runtimeReadiness: CanonicalRuntimeReadinessReport,
    portReadiness: CanonicalProductionPortReadiness,
    equivalence: CanonicalLegacyEquivalenceReport,
    blockers: CanonicalDryRunBlocker[],
    generatedAt: Date
  ): CanonicalDryRunReadinessReport {
    const states: CanonicalRuntimeReadinessStatus[] = [CanonicalRuntimeReadinessStatus.NOT_EVALUATED]
    states.push(CanonicalRuntimeReadinessStatus.PRODUCTION_BLOCKED)
    return new CanonicalDryRunReadinessReport(states, portReadiness, blockers, false, false, generatedAt)
  }

  static diagnostics(
    dryRunID: string,
    equivalence: CanonicalLegacyEquivalenceReport,
    readiness: CanonicalDryRunReadinessReport,
    blockers: CanonicalDryRunBlocker[],
    generatedAt: Date
  ): CanonicalProductionDiagnosticsEvent[] {
    const events: CanonicalProductionDiagnosticsEvent[] = [
      new CanonicalProductionDiagnosticsEvent(
        CanonicalProductionDiagnosticEventKind.CANONICAL_DRY_RUN_STARTED,
        null, dryRunID, null, 'dryRunMigrationPlanner', null, null, true, generatedAt
      ),
      new CanonicalProductionDiagnosticsEvent(
        CanonicalProductionDiagnosticEventKind.CANONICAL_DRY_RUN_COMPLETED,
        null, null, null, 'dryRunOnly', null, null, true, generatedAt
      )
    ]
    return events
  }
}
