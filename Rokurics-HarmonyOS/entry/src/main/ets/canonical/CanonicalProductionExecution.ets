/**
 * CanonicalProductionExecution — execution tokens, policies, rollback, and guards.
 * Ported from Rokurics-Apple (CanonicalProductionExecution.swift).
 */

import {
  CanonicalHash, CanonicalTimestamp, nilIfEmpty
} from './CanonicalCore'
import {
  CanonicalProductionDomain, CanonicalProductionPortKind,
  CanonicalProductionSideEffect, CanonicalProductionDiagnosticsEvent,
  CanonicalProductionPortSet, CanonicalProductionRedaction,
  CanonicalFileReference, CanonicalPathResolutionResult
} from './CanonicalProductionPorts'

// =============================================================================
// SIDE EFFECT
// =============================================================================

export enum CanonicalProductionSideEffectKind {
  FILE_READ = 'fileRead',
  FILE_WRITE = 'fileWrite',
  METADATA_APPLY = 'metadataApply',
  GENERATED_ARTIFACT_APPLY = 'generatedArtifactApply',
  NETWORK_REQUEST = 'networkRequest',
  UPLOAD_SESSION_START = 'uploadSessionStart',
  UPLOAD_CHUNK_SEND = 'uploadChunkSend',
  UPLOAD_FINALIZE = 'uploadFinalize',
  TOMBSTONE_MARK = 'tombstoneMark',
  CONFLICT_RECORD = 'conflictRecord',
  DIAGNOSTICS_WRITE = 'diagnosticsWrite'
}

// =============================================================================
// EXECUTION TRACE
// =============================================================================

export enum CanonicalKernelExecutionMode {
  DRY_RUN = 'dryRun',
  PRODUCTION_EXECUTE = 'productionExecute',
  SHADOW_PREPARATION = 'shadowPreparation'
}

export class CanonicalProductionExecutionTrace {
  operationID: string
  mode: CanonicalKernelExecutionMode
  generatedAt: CanonicalTimestamp
  sideEffects: CanonicalProductionSideEffect[]

  constructor(
    operationID: string,
    mode: CanonicalKernelExecutionMode,
    sideEffects: CanonicalProductionSideEffect[] = [],
    generatedAt: Date = new Date()
  ) {
    this.operationID = CanonicalProductionRedaction.safeIdentifier(operationID, 'canonical-operation')
    this.mode = mode
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.sideEffects = sideEffects
  }

  get redactedSummaries(): string[] {
    return this.sideEffects.map(s => s.redactedSummary)
  }
}

// =============================================================================
// EXECUTION FAILURE
// =============================================================================

export class CanonicalProductionExecutionFailure {
  operationID: string
  domain: CanonicalProductionDomain | null
  reason: string

  constructor(operationID: string, domain: CanonicalProductionDomain | null = null, reason: string) {
    this.operationID = CanonicalProductionRedaction.safeIdentifier(operationID, 'canonical-operation')
    this.domain = domain
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? 'productionExecutionFailed'
  }

  get id(): string {
    return [this.operationID, this.reason, this.domain ?? ''].join('|')
  }
}

// =============================================================================
// EXECUTION RESULT
// =============================================================================

export class CanonicalProductionExecutionResult {
  operationID: string
  mode: CanonicalKernelExecutionMode
  succeeded: boolean
  trace: CanonicalProductionExecutionTrace
  failures: CanonicalProductionExecutionFailure[]
  guardAudit: CanonicalProductionExecutionAudit | null

  constructor(
    operationID: string,
    mode: CanonicalKernelExecutionMode,
    succeeded: boolean,
    sideEffects: CanonicalProductionSideEffect[] = [],
    failures: CanonicalProductionExecutionFailure[] = [],
    guardAudit: CanonicalProductionExecutionAudit | null = null,
    generatedAt: Date = new Date()
  ) {
    this.operationID = CanonicalProductionRedaction.safeIdentifier(operationID, 'canonical-operation')
    this.mode = mode
    this.succeeded = succeeded
    this.trace = new CanonicalProductionExecutionTrace(this.operationID, mode, sideEffects, generatedAt)
    this.failures = failures
    this.guardAudit = guardAudit
  }
}

// =============================================================================
// EXECUTION TOKEN & POLICY
// =============================================================================

export enum CanonicalProductionExecutionDomainRole {
  IPHONE = 'iPhone',
  MAC = 'mac',
  TEST_HARNESS = 'testHarness'
}

export class CanonicalProductionExecutionToken {
  mode: CanonicalKernelExecutionMode
  domainAllowlist: CanonicalProductionDomain[]
  nodeRole: CanonicalProductionExecutionDomainRole
  syncRunID: string
  dryRunEquivalentReportID: string | null
  rollbackPlanID: string | null
  ownerApproved: boolean

  constructor(
    mode: CanonicalKernelExecutionMode,
    domainAllowlist: CanonicalProductionDomain[],
    nodeRole: CanonicalProductionExecutionDomainRole,
    syncRunID: string,
    dryRunEquivalentReportID: string | null = null,
    rollbackPlanID: string | null = null,
    ownerApproved: boolean = false
  ) {
    this.mode = mode
    this.domainAllowlist = Array.from(new Set(domainAllowlist)).sort((a, b) => a.localeCompare(b))
    this.nodeRole = nodeRole
    this.syncRunID = CanonicalProductionRedaction.safeIdentifier(syncRunID, 'sync-run')
    this.dryRunEquivalentReportID = dryRunEquivalentReportID
      ? CanonicalProductionRedaction.safeIdentifier(dryRunEquivalentReportID, 'dry-run-report')
      : null
    this.rollbackPlanID = rollbackPlanID
      ? CanonicalProductionRedaction.safeIdentifier(rollbackPlanID, 'rollback-plan')
      : null
    this.ownerApproved = ownerApproved
  }
}

export class CanonicalProductionExecutionPolicy {
  requiredDomains: CanonicalProductionDomain[]
  requiredPorts: CanonicalProductionPortKind[]
  requireOwnerApproval: boolean
  requireRollbackPlan: boolean
  requireDryRunEquivalence: boolean
  requireMigrationGateUnblocked: boolean
  rejectUnresolvedConflicts: boolean

  constructor(
    requiredDomains: CanonicalProductionDomain[] = [CanonicalProductionDomain.RECORDING_METADATA, CanonicalProductionDomain.FILE_RUNTIME],
    requiredPorts: CanonicalProductionPortKind[] = [
      CanonicalProductionPortKind.FILE, CanonicalProductionPortKind.TRANSPORT,
      CanonicalProductionPortKind.UPLOAD, CanonicalProductionPortKind.APPLY
    ],
    requireOwnerApproval: boolean = true,
    requireRollbackPlan: boolean = true,
    requireDryRunEquivalence: boolean = true,
    requireMigrationGateUnblocked: boolean = true,
    rejectUnresolvedConflicts: boolean = true
  ) {
    this.requiredDomains = Array.from(new Set(requiredDomains)).sort((a, b) => a.localeCompare(b))
    this.requiredPorts = Array.from(new Set(requiredPorts)).sort((a, b) => a.localeCompare(b))
    this.requireOwnerApproval = requireOwnerApproval
    this.requireRollbackPlan = requireRollbackPlan
    this.requireDryRunEquivalence = requireDryRunEquivalence
    this.requireMigrationGateUnblocked = requireMigrationGateUnblocked
    this.rejectUnresolvedConflicts = rejectUnresolvedConflicts
  }
}

// =============================================================================
// EXECUTION AUDIT
// =============================================================================

export enum CanonicalProductionExecutionRejectionReason {
  MODE_DISABLED = 'modeDisabled',
  BLOCKED_PRODUCTION_EXECUTE = 'blockedProductionExecute',
  MISSING_APPROVAL = 'missingApproval',
  MISSING_ROLLBACK_PLAN = 'missingRollbackPlan',
  DRY_RUN_NOT_EQUIVALENT = 'dryRunNotEquivalent',
  UNSUPPORTED_DOMAIN = 'unsupportedDomain',
  UNRESOLVED_CONFLICT = 'unresolvedConflict',
  MISSING_PRODUCTION_PORT = 'missingProductionPort',
  PRODUCTION_MIGRATION_BLOCKED = 'productionMigrationBlocked'
}

export class CanonicalProductionExecutionAudit {
  generatedAt: CanonicalTimestamp
  allowed: boolean
  mode: CanonicalKernelExecutionMode
  nodeRole: CanonicalProductionExecutionDomainRole | null
  requestedMode: CanonicalKernelExecutionMode
  allowedMode: CanonicalKernelExecutionMode | null
  tokenSyncRunID: string | null
  rejectionReasons: CanonicalProductionExecutionRejectionReason[]
  deniedSideEffects: CanonicalProductionSideEffectKind[]
  rollbackAvailable: boolean
  dryRunEquivalent: boolean
  unresolvedConflictCount: number
  domains: CanonicalProductionDomain[]

  constructor(
    allowed: boolean,
    mode: CanonicalKernelExecutionMode,
    nodeRole: CanonicalProductionExecutionDomainRole | null = null,
    requestedMode: CanonicalKernelExecutionMode | null = null,
    allowedMode: CanonicalKernelExecutionMode | null = null,
    tokenSyncRunID: string | null = null,
    rejectionReasons: CanonicalProductionExecutionRejectionReason[],
    deniedSideEffects: CanonicalProductionSideEffectKind[] = [],
    rollbackAvailable: boolean = false,
    dryRunEquivalent: boolean = false,
    unresolvedConflictCount: number = 0,
    domains: CanonicalProductionDomain[],
    generatedAt: Date = new Date()
  ) {
    this.generatedAt = new CanonicalTimestamp(generatedAt)
    this.allowed = allowed
    this.mode = mode
    this.nodeRole = nodeRole
    this.requestedMode = requestedMode ?? mode
    this.allowedMode = allowedMode
    this.tokenSyncRunID = tokenSyncRunID
      ? CanonicalProductionRedaction.safeIdentifier(tokenSyncRunID, 'sync-run')
      : null
    this.rejectionReasons = Array.from(new Set(rejectionReasons)).sort((a, b) => a.localeCompare(b))
    this.deniedSideEffects = Array.from(new Set(deniedSideEffects)).sort((a, b) => a.localeCompare(b))
    this.rollbackAvailable = rollbackAvailable
    this.dryRunEquivalent = dryRunEquivalent
    this.unresolvedConflictCount = Math.max(0, unresolvedConflictCount)
    this.domains = Array.from(new Set(domains)).sort((a, b) => a.localeCompare(b))
  }
}

// =============================================================================
// ROLLBACK
// =============================================================================

export class CanonicalRollbackCheckpoint {
  checkpointID: string
  domain: CanonicalProductionDomain
  objectID: string | null
  artifactID: string | null
  atomicBackupToken: string | null

  constructor(
    checkpointID: string,
    domain: CanonicalProductionDomain,
    objectID: string | null = null,
    artifactID: string | null = null,
    atomicBackupToken: string | null = null
  ) {
    this.checkpointID = CanonicalProductionRedaction.safeIdentifier(checkpointID, 'checkpoint')
    this.domain = domain
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown') : null
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
    this.atomicBackupToken = atomicBackupToken ? CanonicalProductionRedaction.safeIdentifier(atomicBackupToken, 'backup-token') : null
  }

  get id(): string {
    return this.checkpointID
  }
}

export enum CanonicalRollbackActionKind {
  METADATA_ROLLBACK = 'metadataRollback',
  GENERATED_ARTIFACT_ROLLBACK = 'generatedArtifactRollback',
  TOMBSTONE_ROLLBACK = 'tombstoneRollback',
  UPLOAD_SESSION_CANCEL = 'uploadSessionCancel',
  TRANSPORT_NO_OP_ROLLBACK = 'transportNoOpRollback',
  CONFLICT_LEDGER_NO_OP = 'conflictLedgerNoOp',
  FILE_WRITE_ROLLBACK = 'fileWriteRollback'
}

export class CanonicalRollbackAction {
  actionID: string
  kind: CanonicalRollbackActionKind
  domain: CanonicalProductionDomain
  checkpointID: string | null
  objectID: string | null
  artifactID: string | null

  constructor(
    actionID: string,
    kind: CanonicalRollbackActionKind,
    domain: CanonicalProductionDomain,
    checkpointID: string | null = null,
    objectID: string | null = null,
    artifactID: string | null = null
  ) {
    this.actionID = CanonicalProductionRedaction.safeIdentifier(actionID, kind)
    this.kind = kind
    this.domain = domain
    this.checkpointID = checkpointID ? CanonicalProductionRedaction.safeIdentifier(checkpointID, 'checkpoint') : null
    this.objectID = objectID ? CanonicalProductionRedaction.safeIdentifier(objectID, 'unknown') : null
    this.artifactID = artifactID ? CanonicalProductionRedaction.safeIdentifier(artifactID, 'artifact:unknown') : null
  }

  get id(): string {
    return this.actionID
  }
}

export class CanonicalRollbackPlan {
  planID: string
  checkpoints: CanonicalRollbackCheckpoint[]
  actions: CanonicalRollbackAction[]
  generatedAt: CanonicalTimestamp

  constructor(
    planID: string,
    checkpoints: CanonicalRollbackCheckpoint[],
    actions: CanonicalRollbackAction[],
    generatedAt: Date = new Date()
  ) {
    this.planID = CanonicalProductionRedaction.safeIdentifier(planID, 'rollback-plan')
    this.checkpoints = checkpoints.sort((a, b) => a.id.localeCompare(b.id))
    this.actions = actions.sort((a, b) => a.id.localeCompare(b.id))
    this.generatedAt = new CanonicalTimestamp(generatedAt)
  }

  get id(): string {
    return this.planID
  }

  covers(domain: CanonicalProductionDomain): boolean {
    return this.actions.some(a => a.domain === domain)
  }

  coversAll(domains: CanonicalProductionDomain[]): boolean {
    return domains.every(d => this.covers(d))
  }
}

export class CanonicalRollbackFailure {
  actionID: string
  reason: string

  constructor(actionID: string, reason: string) {
    this.actionID = CanonicalProductionRedaction.safeIdentifier(actionID, 'rollback-action')
    this.reason = CanonicalProductionRedaction.safeDiagnosticText(reason) ?? 'rollbackFailed'
  }

  get id(): string {
    return [this.actionID, this.reason].join('|')
  }
}

export class CanonicalRollbackResult {
  planID: string
  succeeded: boolean
  completedActionIDs: string[]
  failures: CanonicalRollbackFailure[]

  constructor(
    planID: string,
    succeeded: boolean,
    completedActionIDs: string[] = [],
    failures: CanonicalRollbackFailure[] = []
  ) {
    this.planID = CanonicalProductionRedaction.safeIdentifier(planID, 'rollback-plan')
    this.succeeded = succeeded
    this.completedActionIDs = Array.from(new Set(
      completedActionIDs.map(id => CanonicalProductionRedaction.safeIdentifier(id, 'rollback-action'))
    )).sort()
    this.failures = failures.sort((a, b) => a.id.localeCompare(b.id))
  }
}

export class CanonicalRollbackAudit {
  planID: string
  requiredDomains: CanonicalProductionDomain[]
  missingDomains: CanonicalProductionDomain[]
  rollbackRequiredForProduction: boolean

  constructor(plan: CanonicalRollbackPlan | null, requiredDomains: CanonicalProductionDomain[]) {
    const required = Array.from(new Set(requiredDomains)).sort((a, b) => a.localeCompare(b))
    this.planID = plan?.planID ?? 'missing'
    this.requiredDomains = required
    this.missingDomains = required.filter(d => plan?.covers(d) !== true)
    this.rollbackRequiredForProduction = true
  }
}

// =============================================================================
// EXECUTION GUARD
// =============================================================================

export class CanonicalProductionExecutionGuard {
  static evaluate(
    mode: CanonicalKernelExecutionMode,
    token: CanonicalProductionExecutionToken | null,
    policy: CanonicalProductionExecutionPolicy,
    domains: CanonicalProductionDomain[],
    ports: CanonicalProductionPortSet,
    rollbackPlan: CanonicalRollbackPlan | null,
    dryRunReportID: string | null,
    dryRunEquivalence: CanonicalDryRunEquivalenceReport | null,
    readinessReport: CanonicalDryRunReadinessReport | null,
    unresolvedConflictCount: number,
    generatedAt: Date = new Date()
  ): CanonicalProductionExecutionAudit {
    const reasons: CanonicalProductionExecutionRejectionReason[] = []
    const rollbackAvailable = rollbackPlan?.coversAll(policy.requiredDomains) === true
    const dryRunEquivalent = dryRunEquivalence?.legacyEquivalence.allEquivalent === true &&
      dryRunEquivalence?.legacyEquivalence.hasBlockingDivergence !== true
    if (mode !== CanonicalKernelExecutionMode.PRODUCTION_EXECUTE || !token || token.mode !== CanonicalKernelExecutionMode.PRODUCTION_EXECUTE) {
      return new CanonicalProductionExecutionAudit(
        false, mode, token?.nodeRole ?? null, token?.mode ?? mode, null,
        token?.syncRunID ?? null,
        [CanonicalProductionExecutionRejectionReason.MODE_DISABLED],
        Object.values(CanonicalProductionSideEffectKind),
        rollbackAvailable, dryRunEquivalent, unresolvedConflictCount, domains, generatedAt
      )
    }

    if (token.nodeRole !== CanonicalProductionExecutionDomainRole.TEST_HARNESS) {
      reasons.push(CanonicalProductionExecutionRejectionReason.BLOCKED_PRODUCTION_EXECUTE)
    }
    if (policy.requireOwnerApproval && !token.ownerApproved) {
      reasons.push(CanonicalProductionExecutionRejectionReason.MISSING_APPROVAL)
    }
    const requiredSet = new Set(policy.requiredDomains)
    const allowlistSet = new Set(token.domainAllowlist)
    const domainSet = new Set(domains)
    if (!CanonicalProductionExecutionGuard.isSubsetOf(requiredSet, allowlistSet) ||
      !CanonicalProductionExecutionGuard.isSubsetOf(domainSet, allowlistSet)) {
      reasons.push(CanonicalProductionExecutionRejectionReason.UNSUPPORTED_DOMAIN)
    }
    if (policy.requireRollbackPlan) {
      if (!rollbackPlan ||
        !token.rollbackPlanID ||
        token.rollbackPlanID !== rollbackPlan.planID ||
        rollbackPlan.coversAll(policy.requiredDomains) !== true) {
        reasons.push(CanonicalProductionExecutionRejectionReason.MISSING_ROLLBACK_PLAN)
      }
    }
    if (policy.requireDryRunEquivalence) {
      if (!dryRunEquivalence ||
        dryRunEquivalence.legacyEquivalence.allEquivalent !== true ||
        dryRunEquivalence.legacyEquivalence.hasBlockingDivergence === true ||
        !token.dryRunEquivalentReportID ||
        token.dryRunEquivalentReportID !== dryRunReportID) {
        reasons.push(CanonicalProductionExecutionRejectionReason.DRY_RUN_NOT_EQUIVALENT)
      }
    }
    if (policy.rejectUnresolvedConflicts && unresolvedConflictCount > 0) {
      reasons.push(CanonicalProductionExecutionRejectionReason.UNRESOLVED_CONFLICT)
    }
    if (policy.requiredPorts.some(p => CanonicalProductionExecutionGuard.portMissingOrDryRun(p, ports))) {
      reasons.push(CanonicalProductionExecutionRejectionReason.MISSING_PRODUCTION_PORT)
    }
    if (policy.requireMigrationGateUnblocked && readinessReport?.productionMigrationBlocked !== false) {
      reasons.push(CanonicalProductionExecutionRejectionReason.PRODUCTION_MIGRATION_BLOCKED)
    }

    return new CanonicalProductionExecutionAudit(
      reasons.length === 0, mode, token.nodeRole, token.mode,
      reasons.length === 0 ? CanonicalKernelExecutionMode.PRODUCTION_EXECUTE : null,
      token.syncRunID, reasons,
      reasons.length === 0 ? [] : Object.values(CanonicalProductionSideEffectKind),
      rollbackAvailable, dryRunEquivalent, unresolvedConflictCount, domains, generatedAt
    )
  }

  static evaluateShadow(
    mode: CanonicalKernelExecutionMode,
    token: CanonicalProductionExecutionToken | null,
    domains: CanonicalProductionDomain[],
    rollbackPlan: CanonicalRollbackPlan | null,
    dryRunEquivalence: CanonicalDryRunEquivalenceReport | null,
    unresolvedConflictCount: number,
    generatedAt: Date = new Date()
  ): CanonicalProductionExecutionAudit {
    const requestedMode = token?.mode ?? mode
    const nodeRole = token?.nodeRole ?? null
    const reasons: CanonicalProductionExecutionRejectionReason[] = []
    if (mode === CanonicalKernelExecutionMode.PRODUCTION_EXECUTE || requestedMode === CanonicalKernelExecutionMode.PRODUCTION_EXECUTE) {
      reasons.push(CanonicalProductionExecutionRejectionReason.BLOCKED_PRODUCTION_EXECUTE)
    }
    if (mode !== CanonicalKernelExecutionMode.SHADOW_PREPARATION || requestedMode !== mode) {
      reasons.push(CanonicalProductionExecutionRejectionReason.MODE_DISABLED)
    }
    const roleAllowed = nodeRole === CanonicalProductionExecutionDomainRole.IPHONE ||
      nodeRole === CanonicalProductionExecutionDomainRole.MAC ||
      nodeRole === CanonicalProductionExecutionDomainRole.TEST_HARNESS
    if (!roleAllowed) {
      reasons.push(CanonicalProductionExecutionRejectionReason.PRODUCTION_MIGRATION_BLOCKED)
    }
    const dryRunEquivalent = dryRunEquivalence?.legacyEquivalence.allEquivalent === true &&
      dryRunEquivalence?.legacyEquivalence.hasBlockingDivergence !== true
    return new CanonicalProductionExecutionAudit(
      reasons.length === 0, mode, nodeRole, requestedMode,
      reasons.length === 0 ? mode : null,
      token?.syncRunID ?? null, reasons,
      Object.values(CanonicalProductionSideEffectKind),
      rollbackPlan !== null, dryRunEquivalent, unresolvedConflictCount, domains, generatedAt
    )
  }

  private static portMissingOrDryRun(port: CanonicalProductionPortKind, ports: CanonicalProductionPortSet): boolean {
    switch (port) {
      case CanonicalProductionPortKind.FILE:
        return !ports.file || ports.file.isDryRunOnly === true
      case CanonicalProductionPortKind.TRANSPORT:
        return !ports.transport || ports.transport.isDryRunOnly === true
      case CanonicalProductionPortKind.UPLOAD:
        return !ports.upload || ports.upload.isDryRunOnly === true
      case CanonicalProductionPortKind.APPLY:
        return !ports.apply || ports.apply.isDryRunOnly === true
      case CanonicalProductionPortKind.SYNC_CLOCK:
        return !ports.syncClock
      case CanonicalProductionPortKind.DIAGNOSTICS:
        return !ports.diagnostics
      case CanonicalProductionPortKind.CAPABILITY:
        return !ports.capability
    }
  }

  private static isSubsetOf(subset: Set<CanonicalProductionDomain>, superset: Set<CanonicalProductionDomain>): boolean {
    for (const item of subset) {
      if (!superset.has(item)) {
        return false
      }
    }
    return true
  }
}

// =============================================================================
// FORWARD REFERENCES (minimal stubs)
// =============================================================================

export class CanonicalDryRunEquivalenceReport {
  legacyEquivalence: CanonicalLegacyEquivalenceReport = new CanonicalLegacyEquivalenceReport()
}

export class CanonicalLegacyEquivalenceReport {
  allEquivalent: boolean = true
  hasBlockingDivergence: boolean = false
}

export class CanonicalDryRunReadinessReport {
  productionMigrationBlocked: boolean = true
}
