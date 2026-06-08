/**
 * CanonicalRuntimeReadiness — offline kernel readiness evaluator.
 * Ported from Rokurics-Apple (CanonicalRuntimeReadiness.swift).
 * Evaluates whether each canonical runtime domain is ready for production.
 */

import { CanonicalTimestamp } from './CanonicalCore'

// ── Runtime Readiness Domain ──

export enum CanonicalRuntimeReadinessDomain {
  FILE_RUNTIME = 'fileRuntime',
  TRANSPORT_RUNTIME = 'transportRuntime',
  UPLOAD_RUNTIME = 'uploadRuntime',
  APPLY_EXECUTOR = 'applyExecutor',
  CONFLICT_RESOLVER = 'conflictResolver',
  SIMULATION_HARNESS = 'simulationHarness',
  PRODUCTION_MIGRATION = 'productionMigration'
}

// ── Runtime Readiness Status ──

export enum CanonicalRuntimeReadinessStatus {
  NOT_EVALUATED = 'notEvaluated',
  NOT_STARTED = 'notStarted',
  SEMANTICS_MODELED = 'semanticsModeled',
  OFFLINE_RUNTIME_COMPLETE = 'offlineRuntimeComplete',
  OFFLINE_KERNEL_READY = 'offlineKernelReady',
  PRODUCTION_PORTS_DECLARED = 'productionPortsDeclared',
  DRY_RUN_AVAILABLE = 'dryRunAvailable',
  DRY_RUN_EQUIVALENT = 'dryRunEquivalent',
  PRODUCTION_ADAPTER_MISSING = 'productionAdapterMissing',
  PRODUCTION_BLOCKED = 'productionBlocked',
  ELIGIBLE_FOR_MANUAL_MIGRATION_DESIGN = 'eligibleForManualMigrationDesign',
  ELIGIBLE_FOR_SHADOW_MIGRATION = 'eligibleForShadowMigration',
  ELIGIBLE_FOR_RUNTIME_SWITCH = 'eligibleForRuntimeSwitch',
  RETIRED = 'retired',
  BLOCKED_FOR_PRODUCTION = 'blockedForProduction',
  FAILED = 'failed'
}

// ── Runtime Readiness Blocker Kind ──

export enum CanonicalRuntimeReadinessBlockerKind {
  ROOT_BINDING_MISSING = 'rootBindingMissing',
  HASH_VERIFICATION_MISSING = 'hashVerificationMissing',
  ROUTE_VALIDATION_MISSING = 'routeValidationMissing',
  RESUMABLE_STATE_MISSING = 'resumableStateMissing',
  APPLY_EXECUTOR_MISSING = 'applyExecutorMissing',
  CONFLICT_RESOLVER_MISSING = 'conflictResolverMissing',
  HARNESS_MISSING = 'harnessMissing',
  LEGACY_PRODUCTION_OWNER = 'legacyProductionOwner'
}

// ── Runtime Readiness Blocker ──

export class CanonicalRuntimeReadinessBlocker {
  domain: CanonicalRuntimeReadinessDomain
  kind: CanonicalRuntimeReadinessBlockerKind
  detail: string | null

  constructor(
    domain: CanonicalRuntimeReadinessDomain,
    kind: CanonicalRuntimeReadinessBlockerKind,
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

// ── Runtime Readiness Evidence ──

export class CanonicalRuntimeReadinessEvidence {
  fileRootBinding: boolean
  fileHashVerification: boolean
  transportRouteValidation: boolean
  uploadResumableState: boolean
  applyExecutor: boolean
  conflictResolver: boolean
  twoNodeHarness: boolean
  productionStillLegacyOwned: boolean

  constructor(
    fileRootBinding: boolean = false,
    fileHashVerification: boolean = false,
    transportRouteValidation: boolean = false,
    uploadResumableState: boolean = false,
    applyExecutor: boolean = false,
    conflictResolver: boolean = false,
    twoNodeHarness: boolean = false,
    productionStillLegacyOwned: boolean = true
  ) {
    this.fileRootBinding = fileRootBinding
    this.fileHashVerification = fileHashVerification
    this.transportRouteValidation = transportRouteValidation
    this.uploadResumableState = uploadResumableState
    this.applyExecutor = applyExecutor
    this.conflictResolver = conflictResolver
    this.twoNodeHarness = twoNodeHarness
    this.productionStillLegacyOwned = productionStillLegacyOwned
  }
}

// ── Runtime Readiness Report ──

export class CanonicalRuntimeReadinessReport {
  generatedAt: CanonicalTimestamp
  statuses: Record<string, CanonicalRuntimeReadinessStatus>
  blockers: CanonicalRuntimeReadinessBlocker[]

  constructor(
    generatedAt: CanonicalTimestamp,
    statuses: Record<string, CanonicalRuntimeReadinessStatus>,
    blockers: CanonicalRuntimeReadinessBlocker[]
  ) {
    this.generatedAt = generatedAt
    this.statuses = statuses
    this.blockers = blockers
  }

  statusFor(domain: CanonicalRuntimeReadinessDomain): CanonicalRuntimeReadinessStatus {
    return this.statuses[domain] ?? CanonicalRuntimeReadinessStatus.NOT_STARTED
  }
}

// ── Runtime Readiness Evaluator ──

export class CanonicalRuntimeReadinessEvaluator {
  evaluate(
    evidence: CanonicalRuntimeReadinessEvidence,
    generatedAt: Date = new Date()
  ): CanonicalRuntimeReadinessReport {
    const statuses: Record<string, CanonicalRuntimeReadinessStatus> = {}
    const allDomains = Object.values(CanonicalRuntimeReadinessDomain)
    for (const d of allDomains) {
      statuses[d] = CanonicalRuntimeReadinessStatus.NOT_STARTED
    }
    const blockers: CanonicalRuntimeReadinessBlocker[] = []

    this.setDomain(
      CanonicalRuntimeReadinessDomain.FILE_RUNTIME,
      evidence.fileRootBinding && evidence.fileHashVerification,
      [
        evidence.fileRootBinding ? null : CanonicalRuntimeReadinessBlockerKind.ROOT_BINDING_MISSING,
        evidence.fileHashVerification ? null : CanonicalRuntimeReadinessBlockerKind.HASH_VERIFICATION_MISSING
      ],
      statuses,
      blockers
    )
    this.setDomain(
      CanonicalRuntimeReadinessDomain.TRANSPORT_RUNTIME,
      evidence.transportRouteValidation,
      [evidence.transportRouteValidation ? null : CanonicalRuntimeReadinessBlockerKind.ROUTE_VALIDATION_MISSING],
      statuses,
      blockers
    )
    this.setDomain(
      CanonicalRuntimeReadinessDomain.UPLOAD_RUNTIME,
      evidence.uploadResumableState,
      [evidence.uploadResumableState ? null : CanonicalRuntimeReadinessBlockerKind.RESUMABLE_STATE_MISSING],
      statuses,
      blockers
    )
    this.setDomain(
      CanonicalRuntimeReadinessDomain.APPLY_EXECUTOR,
      evidence.applyExecutor,
      [evidence.applyExecutor ? null : CanonicalRuntimeReadinessBlockerKind.APPLY_EXECUTOR_MISSING],
      statuses,
      blockers
    )
    this.setDomain(
      CanonicalRuntimeReadinessDomain.CONFLICT_RESOLVER,
      evidence.conflictResolver,
      [evidence.conflictResolver ? null : CanonicalRuntimeReadinessBlockerKind.CONFLICT_RESOLVER_MISSING],
      statuses,
      blockers
    )
    this.setDomain(
      CanonicalRuntimeReadinessDomain.SIMULATION_HARNESS,
      evidence.twoNodeHarness,
      [evidence.twoNodeHarness ? null : CanonicalRuntimeReadinessBlockerKind.HARNESS_MISSING],
      statuses,
      blockers
    )

    if (evidence.productionStillLegacyOwned) {
      statuses[CanonicalRuntimeReadinessDomain.PRODUCTION_MIGRATION] = CanonicalRuntimeReadinessStatus.BLOCKED_FOR_PRODUCTION
      blockers.push(new CanonicalRuntimeReadinessBlocker(
        CanonicalRuntimeReadinessDomain.PRODUCTION_MIGRATION,
        CanonicalRuntimeReadinessBlockerKind.LEGACY_PRODUCTION_OWNER,
        'offlineRuntimeOnly'
      ))
    } else {
      statuses[CanonicalRuntimeReadinessDomain.PRODUCTION_MIGRATION] = CanonicalRuntimeReadinessStatus.OFFLINE_RUNTIME_COMPLETE
    }

    return new CanonicalRuntimeReadinessReport(
      new CanonicalTimestamp(generatedAt),
      statuses,
      blockers
    )
  }

  private setDomain(
    domain: CanonicalRuntimeReadinessDomain,
    passed: boolean,
    missing: (CanonicalRuntimeReadinessBlockerKind | null)[],
    statuses: Record<string, CanonicalRuntimeReadinessStatus>,
    blockers: CanonicalRuntimeReadinessBlocker[]
  ): void {
    statuses[domain] = passed
      ? CanonicalRuntimeReadinessStatus.OFFLINE_RUNTIME_COMPLETE
      : CanonicalRuntimeReadinessStatus.FAILED
    for (const kind of missing) {
      if (kind !== null) {
        blockers.push(new CanonicalRuntimeReadinessBlocker(domain, kind, null))
      }
    }
  }
}
