/**
 * CanonicalSyncRuntime — sync plan authority gate & duplication guard.
 * Ported from Rokurics-Apple (CanonicalSyncRuntime.swift).
 * Core runtime types (Mode, DiagnosticKind, Diagnostic) are defined in CanonicalCore.ets.
 * This file defines the authority gate, policy, configuration, and execution guard.
 */

import {
  CanonicalTimestamp, CanonicalHash, CanonicalManifest, CanonicalRecordingMetadata,
  CanonicalSyncRuntimeMode, CanonicalSyncRuntimeDiagnosticKind, CanonicalSyncRuntimeDiagnostic,
  nilIfEmpty, hashPrefix
} from './CanonicalCore'

// ── Canonical Sync Runtime Decision Scope ──

export enum CanonicalSyncRuntimeDecisionScope {
  RECORDING_METADATA = 'recordingMetadata',
  LIBRARY_METADATA = 'libraryMetadata',
  RECORDING_EXISTENCE = 'recordingExistence'
}

// ── Canonical Sync Runtime Policy ──

export class CanonicalSyncRuntimePolicy {
  debugInternalBuild: boolean
  ownerApproved: boolean
  releaseDefaultBuild: boolean
  legacyFallbackAvailable: boolean
  diagnosticsRedacted: boolean
  runtimeSwitchEnabled: boolean
  readPathLegacy: boolean
  otherActiveMigrationDomainConflicting: boolean
  allowDocumentedModifiedAtFallback: boolean
  enabledScopes: CanonicalSyncRuntimeDecisionScope[]

  constructor(
    debugInternalBuild: boolean = false,
    ownerApproved: boolean = false,
    releaseDefaultBuild: boolean = true,
    legacyFallbackAvailable: boolean = true,
    diagnosticsRedacted: boolean = true,
    runtimeSwitchEnabled: boolean = false,
    readPathLegacy: boolean = true,
    otherActiveMigrationDomainConflicting: boolean = false,
    allowDocumentedModifiedAtFallback: boolean = false,
    enabledScopes: CanonicalSyncRuntimeDecisionScope[] = [
      CanonicalSyncRuntimeDecisionScope.RECORDING_METADATA,
      CanonicalSyncRuntimeDecisionScope.LIBRARY_METADATA,
      CanonicalSyncRuntimeDecisionScope.RECORDING_EXISTENCE
    ]
  ) {
    this.debugInternalBuild = debugInternalBuild
    this.ownerApproved = ownerApproved
    this.releaseDefaultBuild = releaseDefaultBuild
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.diagnosticsRedacted = diagnosticsRedacted
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.readPathLegacy = readPathLegacy
    this.otherActiveMigrationDomainConflicting = otherActiveMigrationDomainConflicting
    this.allowDocumentedModifiedAtFallback = allowDocumentedModifiedAtFallback
    this.enabledScopes = [...new Set(enabledScopes)].sort((a, b) => a.localeCompare(b))
  }
}

// ── Canonical Sync Runtime Configuration ──

export class CanonicalSyncRuntimeConfiguration {
  mode: CanonicalSyncRuntimeMode
  policy: CanonicalSyncRuntimePolicy

  constructor(
    mode: CanonicalSyncRuntimeMode = CanonicalSyncRuntimeMode.DISABLED,
    policy: CanonicalSyncRuntimePolicy = new CanonicalSyncRuntimePolicy()
  ) {
    this.mode = mode
    this.policy = policy
  }

  static readonly DISABLED = new CanonicalSyncRuntimeConfiguration()
}

// ── Canonical Sync Plan Authority Gate State ──

export enum CanonicalSyncPlanAuthorityGateState {
  ALLOWED = 'allowed',
  ALLOWED_NO_COMMIT = 'allowedNoCommit',
  BLOCKED_MISSING_SNAPSHOT = 'blockedMissingSnapshot',
  BLOCKED_INVALID_MANIFEST = 'blockedInvalidManifest',
  BLOCKED_PEER_UNAVAILABLE = 'blockedPeerUnavailable',
  BLOCKED_SCHEMA_MISMATCH = 'blockedSchemaMismatch',
  BLOCKED_UNSUPPORTED_OBJECTS = 'blockedUnsupportedObjects',
  BLOCKED_FALLBACK_REQUIRED_OBJECTS = 'blockedFallbackRequiredObjects',
  BLOCKED_CONFLICTS = 'blockedConflicts',
  BLOCKED_PEER_UNKNOWN = 'blockedPeerUnknown',
  BLOCKED_RELEASE_DEFAULT = 'blockedReleaseDefault',
  BLOCKED = 'blocked'
}

// ── Canonical Sync Plan Authority Blocker ──

export enum CanonicalSyncPlanAuthorityBlocker {
  MISSING_INVENTORY_SNAPSHOT = 'missingInventorySnapshot',
  INVALID_LOCAL_MANIFEST = 'invalidLocalManifest',
  INVALID_PEER_MANIFEST = 'invalidPeerManifest',
  PEER_UNAVAILABLE = 'peerUnavailable',
  SCHEMA_MISMATCH = 'schemaMismatch',
  UNSUPPORTED_OBJECTS = 'unsupportedObjects',
  FALLBACK_REQUIRED_OBJECTS = 'fallbackRequiredObjects',
  UNRESOLVED_CONFLICTS = 'unresolvedConflicts',
  PEER_UNKNOWN_AUDIO = 'peerUnknownAudio',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  DIAGNOSTICS_NOT_REDACTED = 'diagnosticsNotRedacted',
  RUNTIME_SWITCH_ENABLED = 'runtimeSwitchEnabled',
  READ_PATH_NOT_LEGACY = 'readPathNotLegacy',
  OTHER_ACTIVE_MIGRATION_DOMAIN = 'otherActiveMigrationDomain',
  RELEASE_DEFAULT_PRIMARY = 'releaseDefaultPrimary',
  DEBUG_INTERNAL_APPROVAL_MISSING = 'debugInternalApprovalMissing',
  BLOCKED_MODE = 'blockedMode',
  CANONICAL_MODIFIED_AT_UNAVAILABLE = 'canonicalModifiedAtUnavailable'
}

// ── Canonical Sync Plan Authority Gate Result ──

export class CanonicalSyncPlanAuthorityGateResult {
  state: CanonicalSyncPlanAuthorityGateState
  blockers: CanonicalSyncPlanAuthorityBlocker[]
  mode: CanonicalSyncRuntimeMode

  constructor(
    state: CanonicalSyncPlanAuthorityGateState,
    blockers: CanonicalSyncPlanAuthorityBlocker[],
    mode: CanonicalSyncRuntimeMode
  ) {
    this.state = state
    this.blockers = blockers
    this.mode = mode
  }

  get isAllowed(): boolean {
    return this.state === CanonicalSyncPlanAuthorityGateState.ALLOWED ||
      this.state === CanonicalSyncPlanAuthorityGateState.ALLOWED_NO_COMMIT
  }

  get shouldUseCanonicalPrimary(): boolean {
    return this.state === CanonicalSyncPlanAuthorityGateState.ALLOWED &&
      this.mode === CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK
  }

  get shouldRecordNoCommit(): boolean {
    return this.state === CanonicalSyncPlanAuthorityGateState.ALLOWED_NO_COMMIT ||
      this.mode === CanonicalSyncRuntimeMode.CANONICAL_PLAN_NO_COMMIT
  }
}

// ── Canonical Sync Plan Authority Gate Context ──

export class CanonicalSyncPlanAuthorityGateContext {
  inventorySnapshotAvailable: boolean
  localManifest: CanonicalManifest | null
  peerManifest: CanonicalManifest | null
  peerAbsenceExplicitlyModeled: boolean
  localMetadataHashSchemaVersion: string
  peerMetadataHashSchemaVersion: string | null
  canonicalModifiedAtSemanticsAvailable: boolean
  unsupportedLegacyObjectCount: number
  libraryFallbackRequiredObjectCount: number
  conflictCount: number
  peerUnknownAudioCount: number
  legacyFallbackAvailable: boolean
  diagnosticsRedacted: boolean
  runtimeSwitchEnabled: boolean
  readPathLegacy: boolean
  otherActiveMigrationDomainConflicting: boolean
  debugInternalBuild: boolean
  ownerApproved: boolean
  releaseDefaultBuild: boolean

  constructor(
    inventorySnapshotAvailable: boolean,
    localManifest: CanonicalManifest | null,
    peerManifest: CanonicalManifest | null,
    peerAbsenceExplicitlyModeled: boolean = false,
    localMetadataHashSchemaVersion: string = CanonicalRecordingMetadata.BUSINESS_METADATA_HASH_SCHEMA_VERSION,
    peerMetadataHashSchemaVersion: string | null = CanonicalRecordingMetadata.BUSINESS_METADATA_HASH_SCHEMA_VERSION,
    canonicalModifiedAtSemanticsAvailable: boolean = true,
    unsupportedLegacyObjectCount: number = 0,
    libraryFallbackRequiredObjectCount: number = 0,
    conflictCount: number = 0,
    peerUnknownAudioCount: number = 0,
    legacyFallbackAvailable: boolean = true,
    diagnosticsRedacted: boolean = true,
    runtimeSwitchEnabled: boolean = false,
    readPathLegacy: boolean = true,
    otherActiveMigrationDomainConflicting: boolean = false,
    debugInternalBuild: boolean = false,
    ownerApproved: boolean = false,
    releaseDefaultBuild: boolean = true
  ) {
    this.inventorySnapshotAvailable = inventorySnapshotAvailable
    this.localManifest = localManifest
    this.peerManifest = peerManifest
    this.peerAbsenceExplicitlyModeled = peerAbsenceExplicitlyModeled
    this.localMetadataHashSchemaVersion = localMetadataHashSchemaVersion
    this.peerMetadataHashSchemaVersion = peerMetadataHashSchemaVersion
    this.canonicalModifiedAtSemanticsAvailable = canonicalModifiedAtSemanticsAvailable
    this.unsupportedLegacyObjectCount = unsupportedLegacyObjectCount
    this.libraryFallbackRequiredObjectCount = libraryFallbackRequiredObjectCount
    this.conflictCount = conflictCount
    this.peerUnknownAudioCount = peerUnknownAudioCount
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.diagnosticsRedacted = diagnosticsRedacted
    this.runtimeSwitchEnabled = runtimeSwitchEnabled
    this.readPathLegacy = readPathLegacy
    this.otherActiveMigrationDomainConflicting = otherActiveMigrationDomainConflicting
    this.debugInternalBuild = debugInternalBuild
    this.ownerApproved = ownerApproved
    this.releaseDefaultBuild = releaseDefaultBuild
  }
}

// ── Canonical Sync Plan Authority Gate ──

export class CanonicalSyncPlanAuthorityGate {
  evaluate(
    configuration: CanonicalSyncRuntimeConfiguration,
    context: CanonicalSyncPlanAuthorityGateContext
  ): CanonicalSyncPlanAuthorityGateResult {
    const mode = configuration.mode
    if (mode === CanonicalSyncRuntimeMode.BLOCKED) {
      return this.makeResult(CanonicalSyncPlanAuthorityGateState.BLOCKED,
        [CanonicalSyncPlanAuthorityBlocker.BLOCKED_MODE], mode)
    }

    const blockers: CanonicalSyncPlanAuthorityBlocker[] = []

    if (!context.inventorySnapshotAvailable) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.MISSING_INVENTORY_SNAPSHOT)
    }
    if (context.localManifest) {
      if (context.localManifest.schemaVersion !== CanonicalManifest.CURRENT_SCHEMA_VERSION ||
        !context.localManifest.hasValidManifestHash) {
        blockers.push(CanonicalSyncPlanAuthorityBlocker.INVALID_LOCAL_MANIFEST)
      }
    } else {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.INVALID_LOCAL_MANIFEST)
    }
    if (context.peerManifest) {
      if (context.peerManifest.schemaVersion !== CanonicalManifest.CURRENT_SCHEMA_VERSION ||
        !context.peerManifest.hasValidManifestHash) {
        blockers.push(CanonicalSyncPlanAuthorityBlocker.INVALID_PEER_MANIFEST)
      }
    } else if (!context.peerAbsenceExplicitlyModeled) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.PEER_UNAVAILABLE)
    }
    if (context.peerManifest !== null &&
      context.peerMetadataHashSchemaVersion !== context.localMetadataHashSchemaVersion) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.SCHEMA_MISMATCH)
    }
    if (!context.canonicalModifiedAtSemanticsAvailable &&
      !configuration.policy.allowDocumentedModifiedAtFallback) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.CANONICAL_MODIFIED_AT_UNAVAILABLE)
    }
    if (context.unsupportedLegacyObjectCount > 0) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.UNSUPPORTED_OBJECTS)
    }
    if (context.libraryFallbackRequiredObjectCount > 0) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.FALLBACK_REQUIRED_OBJECTS)
    }
    if (this.canUseCanonicalAsPrimary(mode) && context.conflictCount > 0) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.UNRESOLVED_CONFLICTS)
    }
    if (this.canUseCanonicalAsPrimary(mode) && context.peerUnknownAudioCount > 0) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.PEER_UNKNOWN_AUDIO)
    }
    if (!context.legacyFallbackAvailable || !configuration.policy.legacyFallbackAvailable) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.LEGACY_FALLBACK_UNAVAILABLE)
    }
    if (!context.diagnosticsRedacted || !configuration.policy.diagnosticsRedacted) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.DIAGNOSTICS_NOT_REDACTED)
    }
    if (context.runtimeSwitchEnabled || configuration.policy.runtimeSwitchEnabled) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.RUNTIME_SWITCH_ENABLED)
    }
    if (!context.readPathLegacy || !configuration.policy.readPathLegacy) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.READ_PATH_NOT_LEGACY)
    }
    if (context.otherActiveMigrationDomainConflicting || configuration.policy.otherActiveMigrationDomainConflicting) {
      blockers.push(CanonicalSyncPlanAuthorityBlocker.OTHER_ACTIVE_MIGRATION_DOMAIN)
    }
    if (this.canUseCanonicalAsPrimary(mode)) {
      if (context.releaseDefaultBuild || configuration.policy.releaseDefaultBuild) {
        blockers.push(CanonicalSyncPlanAuthorityBlocker.RELEASE_DEFAULT_PRIMARY)
      }
      if (!context.debugInternalBuild || !configuration.policy.debugInternalBuild ||
        !context.ownerApproved || !configuration.policy.ownerApproved) {
        blockers.push(CanonicalSyncPlanAuthorityBlocker.DEBUG_INTERNAL_APPROVAL_MISSING)
      }
    }

    const uniqueBlockers = [...new Set(blockers)].sort((a, b) => a.localeCompare(b))
    const primaryState = this.blockedStateFor(uniqueBlockers)
    if (primaryState !== null) {
      return this.makeResult(primaryState, uniqueBlockers, mode)
    }

    switch (mode) {
      case CanonicalSyncRuntimeMode.DISABLED:
      case CanonicalSyncRuntimeMode.DIAGNOSTICS_ONLY:
      case CanonicalSyncRuntimeMode.CANONICAL_PLAN_NO_COMMIT:
        return this.makeResult(CanonicalSyncPlanAuthorityGateState.ALLOWED_NO_COMMIT, [], mode)
      case CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK:
        return this.makeResult(CanonicalSyncPlanAuthorityGateState.ALLOWED, [], mode)
      case CanonicalSyncRuntimeMode.BLOCKED:
        return this.makeResult(CanonicalSyncPlanAuthorityGateState.BLOCKED,
          [CanonicalSyncPlanAuthorityBlocker.BLOCKED_MODE], mode)
    }
  }

  private canUseCanonicalAsPrimary(mode: CanonicalSyncRuntimeMode): boolean {
    return mode === CanonicalSyncRuntimeMode.CANONICAL_PLAN_PRIMARY_WITH_LEGACY_FALLBACK
  }

  private blockedStateFor(
    blockers: CanonicalSyncPlanAuthorityBlocker[]
  ): CanonicalSyncPlanAuthorityGateState | null {
    if (blockers.length === 0) return null
    const set = new Set(blockers)
    if (set.has(CanonicalSyncPlanAuthorityBlocker.MISSING_INVENTORY_SNAPSHOT)) {
      return CanonicalSyncPlanAuthorityGateState.BLOCKED_MISSING_SNAPSHOT
    }
    if (set.has(CanonicalSyncPlanAuthorityBlocker.INVALID_LOCAL_MANIFEST) ||
      set.has(CanonicalSyncPlanAuthorityBlocker.INVALID_PEER_MANIFEST)) {
      return CanonicalSyncPlanAuthorityGateState.BLOCKED_INVALID_MANIFEST
    }
    if (set.has(CanonicalSyncPlanAuthorityBlocker.PEER_UNAVAILABLE)) {
      return CanonicalSyncPlanAuthorityGateState.BLOCKED_PEER_UNAVAILABLE
    }
    if (set.has(CanonicalSyncPlanAuthorityBlocker.SCHEMA_MISMATCH)) {
      return CanonicalSyncPlanAuthorityGateState.BLOCKED_SCHEMA_MISMATCH
    }
    if (set.has(CanonicalSyncPlanAuthorityBlocker.UNSUPPORTED_OBJECTS)) {
      return CanonicalSyncPlanAuthorityGateState.BLOCKED_UNSUPPORTED_OBJECTS
    }
    if (set.has(CanonicalSyncPlanAuthorityBlocker.FALLBACK_REQUIRED_OBJECTS)) {
      return CanonicalSyncPlanAuthorityGateState.BLOCKED_FALLBACK_REQUIRED_OBJECTS
    }
    if (set.has(CanonicalSyncPlanAuthorityBlocker.UNRESOLVED_CONFLICTS)) {
      return CanonicalSyncPlanAuthorityGateState.BLOCKED_CONFLICTS
    }
    if (set.has(CanonicalSyncPlanAuthorityBlocker.PEER_UNKNOWN_AUDIO)) {
      return CanonicalSyncPlanAuthorityGateState.BLOCKED_PEER_UNKNOWN
    }
    if (set.has(CanonicalSyncPlanAuthorityBlocker.RELEASE_DEFAULT_PRIMARY) ||
      set.has(CanonicalSyncPlanAuthorityBlocker.DEBUG_INTERNAL_APPROVAL_MISSING)) {
      return CanonicalSyncPlanAuthorityGateState.BLOCKED_RELEASE_DEFAULT
    }
    return CanonicalSyncPlanAuthorityGateState.BLOCKED
  }

  private makeResult(
    state: CanonicalSyncPlanAuthorityGateState,
    blockers: CanonicalSyncPlanAuthorityBlocker[],
    mode: CanonicalSyncRuntimeMode
  ): CanonicalSyncPlanAuthorityGateResult {
    return new CanonicalSyncPlanAuthorityGateResult(state, blockers, mode)
  }
}

// ── Canonical Sync Runtime Result ──

export class CanonicalSyncRuntimeResult {
  mode: CanonicalSyncRuntimeMode
  gateResult: CanonicalSyncPlanAuthorityGateResult
  canonicalPlanUsed: boolean
  canonicalPlanFallback: boolean
  canonicalPlanBlocked: boolean
  canonicalPlanNoCommit: boolean
  diagnostics: CanonicalSyncRuntimeDiagnostic[]

  constructor(
    mode: CanonicalSyncRuntimeMode,
    gateResult: CanonicalSyncPlanAuthorityGateResult,
    canonicalPlanUsed: boolean,
    canonicalPlanFallback: boolean,
    canonicalPlanBlocked: boolean,
    canonicalPlanNoCommit: boolean,
    diagnostics: CanonicalSyncRuntimeDiagnostic[]
  ) {
    this.mode = mode
    this.gateResult = gateResult
    this.canonicalPlanUsed = canonicalPlanUsed
    this.canonicalPlanFallback = canonicalPlanFallback
    this.canonicalPlanBlocked = canonicalPlanBlocked
    this.canonicalPlanNoCommit = canonicalPlanNoCommit
    this.diagnostics = diagnostics
  }

  static make(
    mode: CanonicalSyncRuntimeMode,
    gateResult: CanonicalSyncPlanAuthorityGateResult,
    syncRunID: string | null,
    extraDiagnostics: CanonicalSyncRuntimeDiagnostic[] = []
  ): CanonicalSyncRuntimeResult {
    const used = gateResult.shouldUseCanonicalPrimary
    const noCommit = !used && gateResult.isAllowed
    const blocked = !gateResult.isAllowed
    const diagnostics: CanonicalSyncRuntimeDiagnostic[] = [
      new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_MODE_EVALUATED,
        syncRunID,
        mode,
        undefined,
        undefined,
        undefined,
        undefined,
        undefined,
        'state=' + gateResult.state
      ),
      new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_EVALUATED,
        syncRunID,
        mode,
        undefined,
        undefined,
        undefined,
        gateResult.blockers.length,
        gateResult.state
      ),
      new CanonicalSyncRuntimeDiagnostic(
        gateResult.isAllowed
          ? CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_AUTHORITY_GATE_ALLOWED
          : CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_AUTHORITY_GATE_BLOCKED,
        syncRunID,
        mode,
        undefined,
        undefined,
        undefined,
        gateResult.blockers.length,
        nilIfEmpty(gateResult.blockers.join('+')) ?? 'none'
      ),
      new CanonicalSyncRuntimeDiagnostic(
        gateResult.isAllowed
          ? CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_ALLOWED
          : CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_BLOCKED,
        syncRunID,
        mode,
        undefined,
        undefined,
        undefined,
        gateResult.blockers.length,
        gateResult.state
      )
    ]
    if (used) {
      diagnostics.push(new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_USED,
        syncRunID, mode, undefined, undefined, undefined, undefined, 'primary'))
    } else if (noCommit) {
      diagnostics.push(new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_NO_COMMIT,
        syncRunID, mode, undefined, undefined, undefined, undefined, 'legacyOwner'))
    } else {
      diagnostics.push(new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_PLAN_FALLBACK,
        syncRunID, mode, undefined, undefined, undefined, gateResult.blockers.length, gateResult.state))
    }
    for (const d of extraDiagnostics) {
      diagnostics.push(d)
    }
    return new CanonicalSyncRuntimeResult(
      mode, gateResult, used, !used, blocked, noCommit, diagnostics
    )
  }
}

// ── Canonical Sync Runtime Action Identity ──

export class CanonicalSyncRuntimeActionIdentity {
  scope: CanonicalSyncRuntimeDecisionScope
  objectID: string
  actionKind: string

  constructor(scope: CanonicalSyncRuntimeDecisionScope, objectID: string, actionKind: string) {
    this.scope = scope
    this.objectID = objectID.trim()
    this.actionKind = actionKind.trim()
  }

  equals(other: CanonicalSyncRuntimeActionIdentity): boolean {
    return this.scope === other.scope && this.objectID === other.objectID && this.actionKind === other.actionKind
  }

  stableKey(): string {
    return [this.scope, this.objectID, this.actionKind].join('|')
  }
}

// ── Canonical Sync Runtime Duplicate Execution Guard Result ──

export class CanonicalSyncRuntimeDuplicateExecutionGuardResult {
  suppressedLegacyActions: CanonicalSyncRuntimeActionIdentity[]
  preventedDuplicateActions: CanonicalSyncRuntimeActionIdentity[]
  diagnostics: CanonicalSyncRuntimeDiagnostic[]

  constructor(
    suppressedLegacyActions: CanonicalSyncRuntimeActionIdentity[],
    preventedDuplicateActions: CanonicalSyncRuntimeActionIdentity[],
    diagnostics: CanonicalSyncRuntimeDiagnostic[]
  ) {
    this.suppressedLegacyActions = suppressedLegacyActions
    this.preventedDuplicateActions = preventedDuplicateActions
    this.diagnostics = diagnostics
  }
}

// ── Canonical Sync Runtime Duplicate Execution Guard ──

export class CanonicalSyncRuntimeDuplicateExecutionGuard {
  evaluate(
    canonicalOwnerUsed: boolean,
    mode: CanonicalSyncRuntimeMode,
    syncRunID: string | null,
    canonicalActions: CanonicalSyncRuntimeActionIdentity[],
    legacyActions: CanonicalSyncRuntimeActionIdentity[],
    enabledScopes: CanonicalSyncRuntimeDecisionScope[]
  ): CanonicalSyncRuntimeDuplicateExecutionGuardResult {
    if (!canonicalOwnerUsed) {
      return new CanonicalSyncRuntimeDuplicateExecutionGuardResult([], [], [])
    }
    const enabled = new Set(enabledScopes)
    const canonicalKeys = new Set<string>()
    for (const a of canonicalActions) {
      if (enabled.has(a.scope)) {
        canonicalKeys.add(a.stableKey())
      }
    }
    const duplicates = legacyActions.filter(a => canonicalKeys.has(a.stableKey()))
      .sort((a, b) => a.stableKey().localeCompare(b.stableKey()))

    const diagnostics: CanonicalSyncRuntimeDiagnostic[] = duplicates.map(d =>
      new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_DUPLICATE_EXECUTION_PREVENTED,
        syncRunID, mode, d.objectID, d.actionKind, undefined, undefined, d.scope
      )
    )
    if (duplicates.length === 0) {
      // no suppressed diagnostics
    } else {
      diagnostics.push(new CanonicalSyncRuntimeDiagnostic(
        CanonicalSyncRuntimeDiagnosticKind.CANONICAL_SYNC_RUNTIME_DUPLICATE_LEGACY_SUPPRESSED,
        syncRunID, mode, undefined, undefined, undefined, duplicates.length, 'exactScopeObjectAction'
      ))
    }

    return new CanonicalSyncRuntimeDuplicateExecutionGuardResult(
      duplicates, duplicates, diagnostics
    )
  }
}
