/**
 * CanonicalShadowMigration — shadow migration modes, policies, gate, runner.
 * Ported from Rokurics-Apple (CanonicalShadowMigration.swift).
 */

import {
  CanonicalTimestamp, CanonicalHash, nilIfEmpty
} from './CanonicalCore'
import {
  CanonicalProductionDomain, CanonicalProductionPortSet,
  CanonicalProductionPortReadiness, CanonicalProductionSnapshot,
  CanonicalProductionRedaction, CanonicalProductionPortKind,
  CanonicalProductionDiagnosticsEvent, CanonicalProductionDiagnosticEventKind
} from './CanonicalProductionPorts'
import {
  CanonicalProductionExecutionDomainRole
} from './CanonicalProductionExecution'
import {
  CanonicalDryRunMigrationPlanner, CanonicalDryRunMigrationContext,
  CanonicalDryRunMigrationPlan, CanonicalDryRunReadinessReport,
  CanonicalRuntimeReadinessReport, CanonicalRuntimeReadinessEvaluator,
  CanonicalRuntimeReadinessEvidence
} from './CanonicalDryRunMigrationPlanner'

// =============================================================================
// SHADOW MIGRATION MODE
// =============================================================================

export enum CanonicalShadowMigrationMode {
  DISABLED = 'disabled',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  DRY_RUN_COMPARE = 'dryRunCompare',
  SHADOW_READ_ONLY = 'shadowReadOnly',
  SHADOW_READ_ONLY_WITH_NETWORK_PROBE = 'shadowReadOnlyWithNetworkProbe',
  EXECUTION_SHADOW_DRY_RUN = 'executionShadowDryRun',
  EXECUTION_SHADOW_WITH_SHADOW_FILE_STORE = 'executionShadowWithShadowFileStore',
  EXECUTION_SHADOW_WITH_READ_ONLY_TRANSPORT_PROBE = 'executionShadowWithReadOnlyTransportProbe',
  BLOCKED_PRODUCTION_EXECUTE = 'blockedProductionExecute',
  BLOCKED_EXECUTION_SHADOW_WRITE = 'blockedExecutionShadowWrite',
  BLOCKED_EXECUTION_SHADOW_UPLOAD = 'blockedExecutionShadowUpload',
  BLOCKED_EXECUTION_SHADOW_APPLY = 'blockedExecutionShadowApply'
}

export enum CanonicalShadowMigrationTrigger {
  IPHONE_SYNC_TICK = 'iPhoneSyncTick',
  MAC_INVENTORY = 'macInventory',
  MAC_RECEIVER = 'macReceiver',
  MANUAL = 'manual',
  PERIODIC = 'periodic',
  APP_ACTIVATION = 'appActivation',
  RETRY_DRAINER = 'retryDrainer',
  VIEW_REFRESH = 'viewRefresh',
  TEST_HARNESS = 'testHarness'
}

export enum CanonicalShadowSuppressedSideEffectKind {
  NO_WRITE = 'noWrite',
  NO_UPLOAD = 'noUpload',
  NO_APPLY = 'noApply',
  NO_ROUTE_MUTATION = 'noRouteMutation',
  NO_RUNTIME_SWITCH = 'noRuntimeSwitch',
  NO_PRODUCTION_EXECUTE = 'noProductionExecute'
}

// =============================================================================
// SHADOW MIGRATION POLICY
// =============================================================================

export class CanonicalShadowMigrationPolicy {
  failureIsFatal: boolean
  recordDiagnostics: boolean
  maxDiagnosticsEvents: number
  suppressedSideEffects: CanonicalShadowSuppressedSideEffectKind[]

  constructor(
    failureIsFatal: boolean = false,
    recordDiagnostics: boolean = true,
    maxDiagnosticsEvents: number = 200,
    suppressedSideEffects: CanonicalShadowSuppressedSideEffectKind[] = Object.values(CanonicalShadowSuppressedSideEffectKind)
  ) {
    this.failureIsFatal = failureIsFatal
    this.recordDiagnostics = recordDiagnostics
    this.maxDiagnosticsEvents = Math.max(1, maxDiagnosticsEvents)
    this.suppressedSideEffects = Array.from(new Set(suppressedSideEffects)).sort((a, b) => a.localeCompare(b))
  }
}

export class CanonicalShadowMigrationConfiguration {
  isEnabled: boolean
  mode: CanonicalShadowMigrationMode
  policy: CanonicalShadowMigrationPolicy

  constructor(
    isEnabled: boolean = false,
    mode: CanonicalShadowMigrationMode = CanonicalShadowMigrationMode.DISABLED,
    policy: CanonicalShadowMigrationPolicy = new CanonicalShadowMigrationPolicy()
  ) {
    this.isEnabled = isEnabled
    this.mode = isEnabled ? mode : CanonicalShadowMigrationMode.DISABLED
    this.policy = policy
  }

  static readonly DISABLED = new CanonicalShadowMigrationConfiguration()

  get effectiveMode(): CanonicalShadowMigrationMode {
    return this.isEnabled ? this.mode : CanonicalShadowMigrationMode.DISABLED
  }
}

// =============================================================================
// SHADOW MIGRATION FAILURE
// =============================================================================

export enum CanonicalShadowMigrationFailure {
  DISABLED = 'disabled',
  BLOCKED_PRODUCTION_EXECUTE = 'blockedProductionExecute',
  BLOCKED_EXECUTION_SHADOW_WRITE = 'blockedExecutionShadowWrite',
  BLOCKED_EXECUTION_SHADOW_UPLOAD = 'blockedExecutionShadowUpload',
  BLOCKED_EXECUTION_SHADOW_APPLY = 'blockedExecutionShadowApply',
  ROLE_NOT_ALLOWED = 'roleNotAllowed',
  INSUFFICIENT_LOCAL_SNAPSHOT = 'insufficientLocalSnapshot',
  INSUFFICIENT_PEER_SNAPSHOT = 'insufficientPeerSnapshot',
  DRY_RUN_FAILED = 'dryRunFailed',
  DIAGNOSTICS_ONLY = 'diagnosticsOnly',
  NETWORK_PROBE_REJECTED = 'networkProbeRejected',
  REAL_DATA_SHADOW_COPY_UNAVAILABLE = 'realDataShadowCopyUnavailable',
  REAL_DATA_SHADOW_COPY_FAILED = 'realDataShadowCopyFailed',
  READ_ONLY_TRANSPORT_PROBE_REJECTED = 'readOnlyTransportProbeRejected',
  SHADOW_ROOT_CLEANUP_FAILED = 'shadowRootCleanupFailed',
  UNEXPECTED = 'unexpected'
}

// =============================================================================
// SHADOW MIGRATION GATE
// =============================================================================

export class CanonicalShadowMigrationGate {
  allowed: boolean
  mode: CanonicalShadowMigrationMode
  trigger: CanonicalShadowMigrationTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  failure: CanonicalShadowMigrationFailure | null
  reason: string
  suppressedSideEffects: CanonicalShadowSuppressedSideEffectKind[]

  constructor(
    allowed: boolean,
    mode: CanonicalShadowMigrationMode,
    trigger: CanonicalShadowMigrationTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole,
    failure: CanonicalShadowMigrationFailure | null,
    reason: string,
    suppressedSideEffects: CanonicalShadowSuppressedSideEffectKind[]
  ) {
    this.allowed = allowed
    this.mode = mode
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.failure = failure
    this.reason = reason
    this.suppressedSideEffects = suppressedSideEffects
  }

  static evaluate(
    configuration: CanonicalShadowMigrationConfiguration,
    trigger: CanonicalShadowMigrationTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole,
    requestedProductionExecute: boolean = false
  ): CanonicalShadowMigrationGate {
    const mode = configuration.effectiveMode
    const suppressed = configuration.policy.suppressedSideEffects
    if (mode === CanonicalShadowMigrationMode.DISABLED) {
      return new CanonicalShadowMigrationGate(
        false, mode, trigger, nodeRole,
        CanonicalShadowMigrationFailure.DISABLED, 'shadowMigrationDisabled', suppressed
      )
    }
    if (requestedProductionExecute || mode === CanonicalShadowMigrationMode.BLOCKED_PRODUCTION_EXECUTE) {
      return new CanonicalShadowMigrationGate(
        false, mode, trigger, nodeRole,
        CanonicalShadowMigrationFailure.BLOCKED_PRODUCTION_EXECUTE,
        'productionExecuteBlockedInShadowStage', suppressed
      )
    }
    return new CanonicalShadowMigrationGate(
      true, mode, trigger, nodeRole, null, 'shadowAllowed', suppressed
    )
  }
}

// =============================================================================
// SHADOW MIGRATION EVENT
// =============================================================================

export enum CanonicalShadowMigrationEventKind {
  CANONICAL_SHADOW_MIGRATION_STARTED = 'canonicalShadowMigrationStarted',
  CANONICAL_SHADOW_MIGRATION_COMPLETED = 'canonicalShadowMigrationCompleted',
  CANONICAL_SHADOW_MIGRATION_BLOCKED = 'canonicalShadowMigrationBlocked',
  CANONICAL_SHADOW_MIGRATION_DIVERGENCE_DETECTED = 'canonicalShadowMigrationDivergenceDetected',
  CANONICAL_SHADOW_MIGRATION_EQUIVALENT = 'canonicalShadowMigrationEquivalent',
  CANONICAL_SHADOW_MIGRATION_SUPPRESSED_SIDE_EFFECTS = 'canonicalShadowMigrationSuppressedSideEffects'
}

export class CanonicalShadowMigrationEvent {
  kind: CanonicalShadowMigrationEventKind
  syncRunID: string | null
  trigger: CanonicalShadowMigrationTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  mode: CanonicalShadowMigrationMode
  domain: CanonicalProductionDomain
  equivalenceStatus: string | null
  migrationGateStatus: string | null
  blockerCount: number
  divergenceCount: number
  suppressedSideEffectCount: number
  reason: string | null
  generatedAt: CanonicalTimestamp

  constructor(
    kind: CanonicalShadowMigrationEventKind,
    syncRunID: string | null,
    trigger: CanonicalShadowMigrationTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole,
    mode: CanonicalShadowMigrationMode,
    domain: CanonicalProductionDomain,
    equivalenceStatus: string | null = null,
    migrationGateStatus: string | null = null,
    blockerCount: number = 0,
    divergenceCount: number = 0,
    suppressedSideEffectCount: number = 0,
    reason: string | null = null,
    generatedAt: Date = new Date()
  ) {
    this.kind = kind
    this.syncRunID = syncRunID
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.mode = mode
    this.domain = domain
    this.equivalenceStatus = equivalenceStatus
    this.migrationGateStatus = migrationGateStatus
    this.blockerCount = Math.max(0, blockerCount)
    this.divergenceCount = Math.max(0, divergenceCount)
    this.suppressedSideEffectCount = Math.max(0, suppressedSideEffectCount)
    this.reason = reason
    this.generatedAt = new CanonicalTimestamp(generatedAt)
  }

  get id(): string {
    return [this.kind, this.syncRunID ?? '', this.trigger, this.nodeRole, this.mode, this.domain, this.reason ?? ''].join('|')
  }
}

// =============================================================================
// SHADOW MIGRATION DIVERGENCE SUMMARY
// =============================================================================

export class CanonicalShadowMigrationDivergenceSummary {
  divergenceCount: number
  blockingDivergenceCount: number
  divergentDomains: CanonicalProductionDomain[]

  constructor(plan: CanonicalDryRunMigrationPlan | null = null) {
    const divergences = plan?.equivalenceReport.legacyEquivalence.divergences ?? []
    this.divergenceCount = divergences.length
    this.blockingDivergenceCount = divergences.filter(d => d.isBlocking).length
    this.divergentDomains = Array.from(new Set(
      divergences.map(d => CanonicalProductionDomain.APPLY)
    )).sort((a, b) => a.localeCompare(b))
  }
}

// =============================================================================
// SHADOW MIGRATION SUPPRESSED SIDE EFFECT
// =============================================================================

export class CanonicalShadowMigrationSuppressedSideEffectSummary {
  noWrite: boolean
  noUpload: boolean
  noApply: boolean
  noRouteMutation: boolean
  noRuntimeSwitch: boolean
  noProductionExecute: boolean
  suppressedCount: number

  constructor(effects: CanonicalShadowSuppressedSideEffectKind[] = Object.values(CanonicalShadowSuppressedSideEffectKind)) {
    const set = new Set(effects)
    this.noWrite = set.has(CanonicalShadowSuppressedSideEffectKind.NO_WRITE)
    this.noUpload = set.has(CanonicalShadowSuppressedSideEffectKind.NO_UPLOAD)
    this.noApply = set.has(CanonicalShadowSuppressedSideEffectKind.NO_APPLY)
    this.noRouteMutation = set.has(CanonicalShadowSuppressedSideEffectKind.NO_ROUTE_MUTATION)
    this.noRuntimeSwitch = set.has(CanonicalShadowSuppressedSideEffectKind.NO_RUNTIME_SWITCH)
    this.noProductionExecute = set.has(CanonicalShadowSuppressedSideEffectKind.NO_PRODUCTION_EXECUTE)
    this.suppressedCount = set.size
  }
}

// =============================================================================
// SHADOW MIGRATION REPORT
// =============================================================================

export class CanonicalShadowMigrationReport {
  runID: string
  syncRunID: string | null
  trigger: CanonicalShadowMigrationTrigger
  nodeRole: CanonicalProductionExecutionDomainRole
  mode: CanonicalShadowMigrationMode
  domain: CanonicalProductionDomain
  generatedAt: CanonicalTimestamp
  equivalenceStatus: string
  migrationGateStatus: string
  blockerCount: number
  divergenceSummary: CanonicalShadowMigrationDivergenceSummary
  suppressedSideEffects: CanonicalShadowMigrationSuppressedSideEffectSummary
  events: CanonicalShadowMigrationEvent[]
  failure: CanonicalShadowMigrationFailure | null
  failureReason: string | null

  constructor(
    runID: string,
    syncRunID: string | null,
    trigger: CanonicalShadowMigrationTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole,
    mode: CanonicalShadowMigrationMode,
    domain: CanonicalProductionDomain,
    plan: CanonicalDryRunMigrationPlan | null,
    gate: CanonicalShadowMigrationGate,
    events: CanonicalShadowMigrationEvent[],
    failure: CanonicalShadowMigrationFailure | null = null,
    failureReason: string | null = null,
    generatedAt: Date = new Date()
  ) {
    this.runID = runID
    this.syncRunID = syncRunID
    this.trigger = trigger
    this.nodeRole = nodeRole
    this.mode = mode
    this.domain = domain
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    const equivalence = plan?.equivalenceReport.legacyEquivalence
    this.equivalenceStatus = !equivalence
      ? 'notEvaluated'
      : (equivalence.hasBlockingDivergence ? 'divergent' : 'equivalent')
    this.migrationGateStatus = plan?.readinessReport.productionMigrationBlocked === true
      ? 'blocked' : (plan ? 'manualDesignOnly' : 'notEvaluated')
    this.blockerCount = plan?.blockers.length ?? (failure ? 1 : 0)
    this.divergenceSummary = new CanonicalShadowMigrationDivergenceSummary(plan)
    this.suppressedSideEffects = new CanonicalShadowMigrationSuppressedSideEffectSummary(gate.suppressedSideEffects)
    this.events = events.slice(0, 200)
    this.failure = failure
    this.failureReason = failureReason
  }

  get id(): string {
    return this.runID
  }
}

// =============================================================================
// SHADOW MIGRATION RESULT
// =============================================================================

export class CanonicalShadowMigrationResult {
  configuration: CanonicalShadowMigrationConfiguration
  gate: CanonicalShadowMigrationGate
  dryRunPlan: CanonicalDryRunMigrationPlan | null
  report: CanonicalShadowMigrationReport
  failure: CanonicalShadowMigrationFailure | null
  isFatal: boolean

  constructor(
    configuration: CanonicalShadowMigrationConfiguration,
    gate: CanonicalShadowMigrationGate,
    dryRunPlan: CanonicalDryRunMigrationPlan | null,
    report: CanonicalShadowMigrationReport,
    failure: CanonicalShadowMigrationFailure | null,
    isFatal: boolean
  ) {
    this.configuration = configuration
    this.gate = gate
    this.dryRunPlan = dryRunPlan
    this.report = report
    this.failure = failure
    this.isFatal = isFatal
  }

  get succeeded(): boolean {
    return this.failure === null
  }
}

// =============================================================================
// SHADOW MIGRATION RUNNER
// =============================================================================

export class CanonicalShadowMigrationRunner {
  run(
    configuration: CanonicalShadowMigrationConfiguration,
    trigger: CanonicalShadowMigrationTrigger,
    nodeRole: CanonicalProductionExecutionDomainRole,
    domain: CanonicalProductionDomain,
    localSnapshot: CanonicalProductionSnapshot | null,
    peerSnapshot: CanonicalProductionSnapshot | null,
    ports: CanonicalProductionPortSet,
    currentRuntimeReadiness: CanonicalRuntimeReadinessReport,
    context: CanonicalDryRunMigrationContext = new CanonicalDryRunMigrationContext(),
    syncRunID: string | null = null,
    generatedAt: Date = new Date()
  ): CanonicalShadowMigrationResult {
    const gate = CanonicalShadowMigrationGate.evaluate(configuration, trigger, nodeRole)
    const events: CanonicalShadowMigrationEvent[] = []

    events.push(new CanonicalShadowMigrationEvent(
      CanonicalShadowMigrationEventKind.CANONICAL_SHADOW_MIGRATION_STARTED,
      syncRunID, trigger, nodeRole, configuration.effectiveMode, domain,
      null, null, 0, 0, 0, gate.reason, generatedAt
    ))
    events.push(new CanonicalShadowMigrationEvent(
      CanonicalShadowMigrationEventKind.CANONICAL_SHADOW_MIGRATION_SUPPRESSED_SIDE_EFFECTS,
      syncRunID, trigger, nodeRole, configuration.effectiveMode, domain,
      null, null, 0, 0, gate.suppressedSideEffects.length,
      'sideEffectsSuppressed', generatedAt
    ))

    if (!gate.allowed) {
      events.push(new CanonicalShadowMigrationEvent(
        CanonicalShadowMigrationEventKind.CANONICAL_SHADOW_MIGRATION_BLOCKED,
        syncRunID, trigger, nodeRole, configuration.effectiveMode, domain,
        null, null, 0, 0, 0, gate.reason, generatedAt
      ))
      return new CanonicalShadowMigrationResult(
        configuration, gate, null,
        new CanonicalShadowMigrationReport(
          gate.reason, syncRunID, trigger, nodeRole, gate.mode, domain,
          null, gate, events, gate.failure, gate.reason, generatedAt
        ),
        gate.failure, false
      )
    }

    if (!localSnapshot) {
      return new CanonicalShadowMigrationResult(
        configuration, gate, null,
        new CanonicalShadowMigrationReport(
          gate.reason, syncRunID, trigger, nodeRole, gate.mode, domain,
          null, gate, events,
          CanonicalShadowMigrationFailure.INSUFFICIENT_LOCAL_SNAPSHOT,
          'insufficientLocalSnapshot', generatedAt
        ),
        CanonicalShadowMigrationFailure.INSUFFICIENT_LOCAL_SNAPSHOT, false
      )
    }

    if (!peerSnapshot) {
      return new CanonicalShadowMigrationResult(
        configuration, gate, null,
        new CanonicalShadowMigrationReport(
          gate.reason, syncRunID, trigger, nodeRole, gate.mode, domain,
          null, gate, events,
          CanonicalShadowMigrationFailure.INSUFFICIENT_PEER_SNAPSHOT,
          'insufficientPeerSnapshot', generatedAt
        ),
        CanonicalShadowMigrationFailure.INSUFFICIENT_PEER_SNAPSHOT, false
      )
    }

    try {
      const plan = new CanonicalDryRunMigrationPlanner().plan(
        localSnapshot, peerSnapshot, ports, currentRuntimeReadiness,
        'periodic', context, generatedAt
      )
      if (plan.equivalenceReport.legacyEquivalence.hasBlockingDivergence) {
        events.push(new CanonicalShadowMigrationEvent(
          CanonicalShadowMigrationEventKind.CANONICAL_SHADOW_MIGRATION_DIVERGENCE_DETECTED,
          syncRunID, trigger, nodeRole, configuration.effectiveMode, domain,
          null, null, plan.blockers.length,
          plan.equivalenceReport.legacyEquivalence.divergences.length,
          0, 'blockingDivergence', generatedAt
        ))
      } else {
        events.push(new CanonicalShadowMigrationEvent(
          CanonicalShadowMigrationEventKind.CANONICAL_SHADOW_MIGRATION_EQUIVALENT,
          syncRunID, trigger, nodeRole, configuration.effectiveMode, domain,
          null, null, 0, 0, 0, 'equivalent', generatedAt
        ))
      }
      events.push(new CanonicalShadowMigrationEvent(
        CanonicalShadowMigrationEventKind.CANONICAL_SHADOW_MIGRATION_COMPLETED,
        syncRunID, trigger, nodeRole, configuration.effectiveMode, domain,
        null, null, 0, 0, 0, 'dryRunCompareCompleted', generatedAt
      ))
      return new CanonicalShadowMigrationResult(
        configuration, gate, plan,
        new CanonicalShadowMigrationReport(
          plan.dryRunID, syncRunID, trigger, nodeRole, gate.mode, domain,
          plan, gate, events, null, null, generatedAt
        ),
        null, false
      )
    } catch (error) {
      events.push(new CanonicalShadowMigrationEvent(
        CanonicalShadowMigrationEventKind.CANONICAL_SHADOW_MIGRATION_BLOCKED,
        syncRunID, trigger, nodeRole, configuration.effectiveMode, domain,
        null, null, 0, 0, 0, 'dryRunFailed', generatedAt
      ))
      return new CanonicalShadowMigrationResult(
        configuration, gate, null,
        new CanonicalShadowMigrationReport(
          gate.reason, syncRunID, trigger, nodeRole, gate.mode, domain,
          null, gate, events,
          CanonicalShadowMigrationFailure.DRY_RUN_FAILED,
          String(error), generatedAt
        ),
        CanonicalShadowMigrationFailure.DRY_RUN_FAILED, false
      )
    }
  }
}

// =============================================================================
// SHADOW MIGRATION REDACTION
// =============================================================================

export class CanonicalShadowMigrationRedaction {
  static safeIdentifier(value: string | null): string | null {
    if (!value) {
      return null
    }
    return CanonicalShadowMigrationRedaction.safeText(value)
  }

  static safeText(value: string | null): string | null {
    if (!value) {
      return null
    }
    const trimmed = value.trim().replace(/\n/g, ' ').replace(/\r/g, ' ')
    if (trimmed.length === 0) {
      return null
    }
    const lowercased = trimmed.toLowerCase()
    if (CanonicalProductionRedaction.containsSensitivePathSignal(trimmed) ||
      lowercased.includes('secret') || lowercased.includes('api-key') ||
      lowercased.includes('token') || lowercased.includes('transcript') ||
      lowercased.includes('provider response') || lowercased.includes('full note') ||
      lowercased.includes('summary content')) {
      return 'redacted-' + (CanonicalProductionRedaction.hashPrefix(
        CanonicalHash.sha256OfString(trimmed).value
      ) ?? 'shadow')
    }
    return trimmed.substring(0, 160)
  }
}
