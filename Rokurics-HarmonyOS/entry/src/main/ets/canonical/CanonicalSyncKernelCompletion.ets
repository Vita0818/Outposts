/**
 * CanonicalSyncKernelCompletion — kernel completion scorecard and readiness tracking.
 * Ported from Rokurics-Apple (CanonicalSyncKernelCompletion.swift).
 * Tracks code completeness, device evidence, and readiness for kernel retirement.
 */

import { CanonicalHash, nilIfEmpty, hashPrefix } from './CanonicalCore'
import { CanonicalKernelSwitchMode } from './CanonicalKernelSwitch'
import {
  CanonicalLegacyCompatibilityMatrix,
  CanonicalLegacySwitchBackProofResult
} from './CanonicalLegacyCompatibility'

// ── Forward declaration: CanonicalProductionRedaction ──

export class CanonicalProductionRedaction {
  static containsSensitivePathSignal(value: string): boolean {
    const lower = value.toLowerCase()
    return lower.includes('/containers/') ||
      lower.includes('/data/documents/') ||
      lower.includes('/library/') ||
      lower.includes('/application support/') ||
      lower.includes('/tmp/') ||
      lower.includes('users/') ||
      lower.includes('file://')
  }

  static safeDiagnosticText(value: string): string | null {
    if (CanonicalProductionRedaction.containsSensitivePathSignal(value)) {
      return null
    }
    const trimmed = value.trim()
    return trimmed.length > 0 ? trimmed : null
  }

  static containsSensitiveSignal(value: string): boolean {
    const lowercased = value.toLowerCase()
    if (CanonicalProductionRedaction.containsSensitivePathSignal(value)) {
      return true
    }
    const sensitiveTokens = [
      'secret', 'token=', 'api_key', 'apikey', 'password',
      'privatekey', 'private key', 'requestbody', 'responsebody',
      'fulltranscript', 'fullnote', 'fullsummary',
      'providerresponse', 'audio bytes', '-----begin'
    ]
    if (sensitiveTokens.some(t => lowercased.indexOf(t) >= 0)) {
      return true
    }
    return CanonicalProductionRedaction.containsLongHexToken(value)
  }

  static redact(value: string): string {
    if (CanonicalProductionRedaction.containsSensitiveSignal(value)) {
      const shaHash = CanonicalHash.sha256OfString(value)
      const prefix = hashPrefix(shaHash.value) ?? 'diagnostic'
      return `redacted-${prefix}`
    }
    return CanonicalProductionRedaction.safeDiagnosticText(value) ?? 'redacted'
  }

  private static containsLongHexToken(value: string): boolean {
    const tokens = value.split(/[^0-9a-fA-F]+/)
    return tokens.some(t => t.length > 16 && /^[0-9a-fA-F]+$/.test(t))
  }

  static safeIdentifier(value: string, fallback: string): string {
    const trimmed = nilIfEmpty(value.trim())
    if (trimmed && !CanonicalProductionRedaction.containsSensitiveSignal(trimmed)) {
      return trimmed
    }
    return fallback
  }
}

// ── Canonical Sync Kernel Completion Status ──

export enum CanonicalSyncKernelCompletionStatus {
  INCOMPLETE = 'incomplete',
  CODE_COMPLETE_NEEDS_DEVICE_EVIDENCE = 'codeCompleteNeedsDeviceEvidence',
  READY_FOR_MANUAL_SWITCH_TRIAL = 'readyForManualSwitchTrial',
  BLOCKED = 'blocked',
  READY_TO_RETIRE_LEGACY_REPORT_ONLY = 'readyToRetireLegacyReportOnly',
  UNSAFE = 'unsafe'
}

// ── Canonical Sync Kernel Completion Blocker ──

export enum CanonicalSyncKernelCompletionBlocker {
  INVENTORY_RUNTIME_INCOMPLETE = 'inventoryRuntimeIncomplete',
  DIFF_LWW_RUNTIME_INCOMPLETE = 'diffLWWRuntimeIncomplete',
  EXISTENCE_TRUTH_INCOMPLETE = 'existenceTruthIncomplete',
  NON_AUDIO_APPLY_RUNTIME_INCOMPLETE = 'nonAudioApplyRuntimeIncomplete',
  AUDIO_UPLOAD_RUNTIME_INCOMPLETE = 'audioUploadRuntimeIncomplete',
  READ_RUNTIME_INCOMPLETE = 'readRuntimeIncomplete',
  MASTER_SWITCH_INCOMPLETE = 'masterSwitchIncomplete',
  LEGACY_COMPATIBILITY_PROOF_MISSING = 'legacyCompatibilityProofMissing',
  SWITCH_BACK_PROOF_MISSING = 'switchBackProofMissing',
  DIAGNOSTICS_NOT_REDACTED = 'diagnosticsNotRedacted',
  REAL_DEVICE_EVIDENCE_MISSING = 'realDeviceEvidenceMissing',
  DOMAIN_INCOMPLETE = 'domainIncomplete',
  COMPATIBILITY_PROOF_MISSING = 'compatibilityProofMissing',
  DEFAULT_OLD_KERNEL_MISSING = 'defaultOldKernelMissing',
  RELEASE_DEFAULT_CANONICAL = 'releaseDefaultCanonical',
  LEGACY_FALLBACK_UNAVAILABLE = 'legacyFallbackUnavailable',
  UNRESOLVED_BLOCKER = 'unresolvedBlocker',
  OWNER_APPROVAL_MISSING = 'ownerApprovalMissing',
  MANUAL_BACKUP_ACKNOWLEDGEMENT_MISSING = 'manualBackupAcknowledgementMissing',
  RETIREMENT_EXECUTION_ATTEMPTED = 'retirementExecutionAttempted',
  LEGACY_DELETION_ATTEMPTED = 'legacyDeletionAttempted',
  SENSITIVE_EVIDENCE_LEAK = 'sensitiveEvidenceLeak',
  UNSAFE_CANONICAL_DEFAULT = 'unsafeCanonicalDefault',
  SECURITY_BYPASS_DETECTED = 'securityBypassDetected',
  REALISTIC_ROOT_SWITCH_BACK_PROOF_MISSING = 'realisticRootSwitchBackProofMissing',
  TESTS_NOT_PASSING = 'testsNotPassing',
  DOCS_NOT_UPDATED = 'docsNotUpdated'
}

// ── Canonical Sync Kernel Completion Scorecard Item ──

export enum CanonicalSyncKernelCompletionScorecardItem {
  INVENTORY_RUNTIME_COMPLETE = 'inventoryRuntimeComplete',
  DIFF_LWW_RUNTIME_COMPLETE = 'diffLWWRuntimeComplete',
  EXISTENCE_TRUTH_COMPLETE = 'existenceTruthComplete',
  NON_AUDIO_APPLY_RUNTIME_COMPLETE = 'nonAudioApplyRuntimeComplete',
  AUDIO_UPLOAD_RUNTIME_COMPLETE = 'audioUploadRuntimeComplete',
  READ_RUNTIME_COMPLETE = 'readRuntimeComplete',
  MASTER_SWITCH_COMPLETE = 'masterSwitchComplete',
  LEGACY_COMPATIBILITY_PROOF_COMPLETE = 'legacyCompatibilityProofComplete',
  SWITCH_BACK_PROOF_COMPLETE = 'switchBackProofComplete',
  DIAGNOSTICS_REDACTED = 'diagnosticsRedacted',
  REAL_DEVICE_EVIDENCE_REQUIRED = 'realDeviceEvidenceRequired'
}

export function isCodeCompletionItem(item: CanonicalSyncKernelCompletionScorecardItem): boolean {
  return item !== CanonicalSyncKernelCompletionScorecardItem.REAL_DEVICE_EVIDENCE_REQUIRED
}

export function scorecardItemBlocker(item: CanonicalSyncKernelCompletionScorecardItem): CanonicalSyncKernelCompletionBlocker {
  switch (item) {
    case CanonicalSyncKernelCompletionScorecardItem.INVENTORY_RUNTIME_COMPLETE:
      return CanonicalSyncKernelCompletionBlocker.INVENTORY_RUNTIME_INCOMPLETE
    case CanonicalSyncKernelCompletionScorecardItem.DIFF_LWW_RUNTIME_COMPLETE:
      return CanonicalSyncKernelCompletionBlocker.DIFF_LWW_RUNTIME_INCOMPLETE
    case CanonicalSyncKernelCompletionScorecardItem.EXISTENCE_TRUTH_COMPLETE:
      return CanonicalSyncKernelCompletionBlocker.EXISTENCE_TRUTH_INCOMPLETE
    case CanonicalSyncKernelCompletionScorecardItem.NON_AUDIO_APPLY_RUNTIME_COMPLETE:
      return CanonicalSyncKernelCompletionBlocker.NON_AUDIO_APPLY_RUNTIME_INCOMPLETE
    case CanonicalSyncKernelCompletionScorecardItem.AUDIO_UPLOAD_RUNTIME_COMPLETE:
      return CanonicalSyncKernelCompletionBlocker.AUDIO_UPLOAD_RUNTIME_INCOMPLETE
    case CanonicalSyncKernelCompletionScorecardItem.READ_RUNTIME_COMPLETE:
      return CanonicalSyncKernelCompletionBlocker.READ_RUNTIME_INCOMPLETE
    case CanonicalSyncKernelCompletionScorecardItem.MASTER_SWITCH_COMPLETE:
      return CanonicalSyncKernelCompletionBlocker.MASTER_SWITCH_INCOMPLETE
    case CanonicalSyncKernelCompletionScorecardItem.LEGACY_COMPATIBILITY_PROOF_COMPLETE:
      return CanonicalSyncKernelCompletionBlocker.LEGACY_COMPATIBILITY_PROOF_MISSING
    case CanonicalSyncKernelCompletionScorecardItem.SWITCH_BACK_PROOF_COMPLETE:
      return CanonicalSyncKernelCompletionBlocker.SWITCH_BACK_PROOF_MISSING
    case CanonicalSyncKernelCompletionScorecardItem.DIAGNOSTICS_REDACTED:
      return CanonicalSyncKernelCompletionBlocker.DIAGNOSTICS_NOT_REDACTED
    case CanonicalSyncKernelCompletionScorecardItem.REAL_DEVICE_EVIDENCE_REQUIRED:
      return CanonicalSyncKernelCompletionBlocker.REAL_DEVICE_EVIDENCE_MISSING
  }
}

// ── Canonical Sync Kernel Completion Scorecard Item Result ──

export class CanonicalSyncKernelCompletionScorecardItemResult {
  item: CanonicalSyncKernelCompletionScorecardItem
  complete: boolean
  diagnosticsSummary: string

  constructor(
    item: CanonicalSyncKernelCompletionScorecardItem,
    complete: boolean,
    diagnosticsSummary: string | null = null
  ) {
    this.item = item
    this.complete = complete
    this.diagnosticsSummary = CanonicalProductionRedaction.redact(
      diagnosticsSummary ?? `item=${item},complete=${complete}`
    )
  }
}

// ── Canonical Sync Kernel Completion Domain ──

export enum CanonicalSyncKernelCompletionDomain {
  RECORDING_METADATA = 'recordingMetadata',
  LIBRARY_METADATA = 'libraryMetadata',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  RECORDING_EXISTENCE = 'recordingExistence',
  AUDIO_UPLOAD = 'audioUpload',
  READ_RUNTIME = 'readRuntime',
  INVENTORY_RUNTIME = 'inventoryRuntime',
  SYNC_DECISION_RUNTIME = 'syncDecisionRuntime',
  APPLY_RUNTIME = 'applyRuntime',
  KERNEL_SWITCH = 'kernelSwitch',
  LEGACY_COMPATIBILITY = 'legacyCompatibility'
}

// ── Canonical Sync Kernel Completion Domain Readiness ──

export class CanonicalSyncKernelCompletionDomainReadiness {
  domain: CanonicalSyncKernelCompletionDomain
  writeExecutorReady: boolean
  readRuntimeReady: boolean
  syncRuntimeOwnerReady: boolean
  applyRuntimeReady: boolean
  audioRuntimeReady: boolean
  legacyFallbackReady: boolean
  switchBackProofReady: boolean
  diagnosticsRedacted: boolean
  testsPass: boolean
  docsUpdated: boolean
  realDeviceEvidencePresent: boolean
  codeComplete: boolean
  blockers: CanonicalSyncKernelCompletionBlocker[]
  diagnosticsSummary: string

  constructor(
    domain: CanonicalSyncKernelCompletionDomain,
    writeExecutorReady: boolean = true,
    readRuntimeReady: boolean = true,
    syncRuntimeOwnerReady: boolean = true,
    applyRuntimeReady: boolean = true,
    audioRuntimeReady: boolean | null = null,
    legacyFallbackReady: boolean = true,
    switchBackProofReady: boolean = true,
    diagnosticsRedacted: boolean = true,
    testsPass: boolean = true,
    docsUpdated: boolean = true,
    realDeviceEvidencePresent: boolean = false,
    blockers: CanonicalSyncKernelCompletionBlocker[] = []
  ) {
    const resolvedAudioReady = audioRuntimeReady ?? true
    const resolvedBlockers: CanonicalSyncKernelCompletionBlocker[] = [...blockers]
    if (!writeExecutorReady || !readRuntimeReady || !syncRuntimeOwnerReady || !applyRuntimeReady) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.DOMAIN_INCOMPLETE)
    }
    if (domain === CanonicalSyncKernelCompletionDomain.AUDIO_UPLOAD && !resolvedAudioReady) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.AUDIO_UPLOAD_RUNTIME_INCOMPLETE)
    }
    if (!legacyFallbackReady) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.LEGACY_FALLBACK_UNAVAILABLE)
    }
    if (!switchBackProofReady) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.SWITCH_BACK_PROOF_MISSING)
    }
    if (!diagnosticsRedacted) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.DIAGNOSTICS_NOT_REDACTED)
    }
    if (!testsPass) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.TESTS_NOT_PASSING)
    }
    if (!docsUpdated) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.DOCS_NOT_UPDATED)
    }
    if (!realDeviceEvidencePresent) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.REAL_DEVICE_EVIDENCE_MISSING)
    }

    const uniqueBlockers = CanonicalSyncKernelCompletionDomainReadiness.unique(resolvedBlockers)
    const codeComplete = writeExecutorReady &&
      readRuntimeReady && syncRuntimeOwnerReady && applyRuntimeReady &&
      resolvedAudioReady && legacyFallbackReady && switchBackProofReady &&
      diagnosticsRedacted && testsPass && docsUpdated &&
      !uniqueBlockers.some(b => b !== CanonicalSyncKernelCompletionBlocker.REAL_DEVICE_EVIDENCE_MISSING)

    this.domain = domain
    this.writeExecutorReady = writeExecutorReady
    this.readRuntimeReady = readRuntimeReady
    this.syncRuntimeOwnerReady = syncRuntimeOwnerReady
    this.applyRuntimeReady = applyRuntimeReady
    this.audioRuntimeReady = resolvedAudioReady
    this.legacyFallbackReady = legacyFallbackReady
    this.switchBackProofReady = switchBackProofReady
    this.diagnosticsRedacted = diagnosticsRedacted
    this.testsPass = testsPass
    this.docsUpdated = docsUpdated
    this.realDeviceEvidencePresent = realDeviceEvidencePresent
    this.codeComplete = codeComplete
    this.blockers = uniqueBlockers
    this.diagnosticsSummary = [
      'canonicalSyncKernelCompletionDomain=v8.45',
      `domain=${domain}`,
      `writeExecutorReady=${writeExecutorReady}`,
      `readRuntimeReady=${readRuntimeReady}`,
      `syncRuntimeOwnerReady=${syncRuntimeOwnerReady}`,
      `applyRuntimeReady=${applyRuntimeReady}`,
      `audioRuntimeReady=${resolvedAudioReady}`,
      `legacyFallbackReady=${legacyFallbackReady}`,
      `switchBackProofReady=${switchBackProofReady}`,
      `diagnosticsRedacted=${diagnosticsRedacted}`,
      `testsPass=${testsPass}`,
      `docsUpdated=${docsUpdated}`,
      `realDeviceEvidencePresent=${realDeviceEvidencePresent}`,
      `codeComplete=${codeComplete}`,
      `blockers=${uniqueBlockers.join('|')}`,
      'redacted=true'
    ].join(',')
  }

  static v845CodeCompleteAwaitingDeviceEvidence(): CanonicalSyncKernelCompletionDomainReadiness[] {
    return Object.values(CanonicalSyncKernelCompletionDomain).map(d =>
      new CanonicalSyncKernelCompletionDomainReadiness(d)
    )
  }

  static v845ReadyWithDeviceEvidence(): CanonicalSyncKernelCompletionDomainReadiness[] {
    return Object.values(CanonicalSyncKernelCompletionDomain).map(d =>
      new CanonicalSyncKernelCompletionDomainReadiness(d, true, true, true, true, null, true, true, true, true, true, true)
    )
  }

  private static unique(blockers: CanonicalSyncKernelCompletionBlocker[]): CanonicalSyncKernelCompletionBlocker[] {
    const seen = new Set<string>()
    const result: CanonicalSyncKernelCompletionBlocker[] = []
    for (const b of blockers) {
      if (!seen.has(b as string)) {
        seen.add(b as string)
        result.push(b)
      }
    }
    return result
  }
}

// ── Canonical Sync Kernel Ready to Retire Domain ──

export enum CanonicalSyncKernelReadyToRetireDomain {
  RECORDING_METADATA = 'recordingMetadata',
  LIBRARY_METADATA = 'libraryMetadata',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  AUDIO_UPLOAD = 'audioUpload',
  RECORDING_EXISTENCE_SYNC_ENGINE = 'recordingExistence/sync engine'
}

// ── Canonical Sync Kernel Domain Ready to Retire Readiness ──

export class CanonicalSyncKernelDomainReadyToRetireReadiness {
  domain: CanonicalSyncKernelReadyToRetireDomain
  writeExecutorReady: boolean
  readCutoverReady: boolean
  canonicalRuntimeOwnerReady: boolean
  legacyFallbackReady: boolean
  switchBackProven: boolean
  diagnosticsClean: boolean
  realDeviceEvidencePresent: boolean
  readyToRetireLegacy: boolean
  retirementExecutionPerformed: boolean
  blockers: CanonicalSyncKernelCompletionBlocker[]
  diagnosticsSummary: string

  constructor(
    domain: CanonicalSyncKernelReadyToRetireDomain,
    writeExecutorReady: boolean = true,
    readCutoverReady: boolean = true,
    canonicalRuntimeOwnerReady: boolean = true,
    legacyFallbackReady: boolean = true,
    switchBackProven: boolean = true,
    diagnosticsClean: boolean = true,
    realDeviceEvidencePresent: boolean = false,
    readyToRetireLegacy: boolean | null = null,
    retirementExecutionPerformed: boolean = false,
    blockers: CanonicalSyncKernelCompletionBlocker[] = []
  ) {
    const resolvedBlockers: CanonicalSyncKernelCompletionBlocker[] = [...blockers]
    if (!writeExecutorReady || !readCutoverReady || !canonicalRuntimeOwnerReady) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.DOMAIN_INCOMPLETE)
    }
    if (!legacyFallbackReady) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.LEGACY_FALLBACK_UNAVAILABLE)
    }
    if (!switchBackProven) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.SWITCH_BACK_PROOF_MISSING)
    }
    if (!diagnosticsClean) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.DIAGNOSTICS_NOT_REDACTED)
    }
    if (!realDeviceEvidencePresent) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.REAL_DEVICE_EVIDENCE_MISSING)
    }
    if (retirementExecutionPerformed) {
      resolvedBlockers.push(CanonicalSyncKernelCompletionBlocker.RETIREMENT_EXECUTION_ATTEMPTED)
    }

    const uniqueBlockers = CanonicalSyncKernelCompletionDomainReadiness['unique'](resolvedBlockers)
    const codeReady = writeExecutorReady && readCutoverReady && canonicalRuntimeOwnerReady &&
      legacyFallbackReady && switchBackProven && diagnosticsClean && !retirementExecutionPerformed
    const computedReady = codeReady && realDeviceEvidencePresent

    this.domain = domain
    this.writeExecutorReady = writeExecutorReady
    this.readCutoverReady = readCutoverReady
    this.canonicalRuntimeOwnerReady = canonicalRuntimeOwnerReady
    this.legacyFallbackReady = legacyFallbackReady
    this.switchBackProven = switchBackProven
    this.diagnosticsClean = diagnosticsClean
    this.realDeviceEvidencePresent = realDeviceEvidencePresent
    this.readyToRetireLegacy = retirementExecutionPerformed ? false : (readyToRetireLegacy ?? computedReady)
    this.retirementExecutionPerformed = retirementExecutionPerformed
    this.blockers = uniqueBlockers
    this.diagnosticsSummary = [
      'canonicalSyncKernelDomainReadyToRetire=v8.45',
      `domain=${domain}`,
      `writeExecutorReady=${writeExecutorReady}`,
      `readCutoverReady=${readCutoverReady}`,
      `canonicalRuntimeOwnerReady=${canonicalRuntimeOwnerReady}`,
      `legacyFallbackReady=${legacyFallbackReady}`,
      `switchBackProven=${switchBackProven}`,
      `diagnosticsClean=${diagnosticsClean}`,
      `realDeviceEvidencePresent=${realDeviceEvidencePresent}`,
      `readyToRetireLegacy=${this.readyToRetireLegacy}`,
      'retirementExecutionPerformed=false',
      'legacyDeleted=false',
      'legacyDisabled=false',
      `blockers=${uniqueBlockers.join('|')}`,
      'redacted=true'
    ].join(',')
  }

  get codeReady(): boolean {
    return this.writeExecutorReady && this.readCutoverReady && this.canonicalRuntimeOwnerReady &&
      this.legacyFallbackReady && this.switchBackProven && this.diagnosticsClean &&
      !this.retirementExecutionPerformed &&
      this.blockers.indexOf(CanonicalSyncKernelCompletionBlocker.RETIREMENT_EXECUTION_ATTEMPTED) < 0 &&
      this.blockers.indexOf(CanonicalSyncKernelCompletionBlocker.LEGACY_DELETION_ATTEMPTED) < 0
  }
}

// ── Canonical Sync Kernel Domain Ready to Retire Report ──

export class CanonicalSyncKernelDomainReadyToRetireReport {
  domains: CanonicalSyncKernelDomainReadyToRetireReadiness[]
  retirementExecutionPerformed: boolean
  legacyDeleted: boolean
  legacyDisabled: boolean
  diagnosticsSummary: string

  constructor(
    domains: CanonicalSyncKernelDomainReadyToRetireReadiness[],
    retirementExecutionPerformed: boolean = false,
    legacyDeleted: boolean = false,
    legacyDisabled: boolean = false
  ) {
    this.domains = [...domains].sort((a, b) => (a.domain as string).localeCompare(b.domain as string))
    this.retirementExecutionPerformed = retirementExecutionPerformed
    this.legacyDeleted = legacyDeleted
    this.legacyDisabled = legacyDisabled
    this.diagnosticsSummary = [
      'canonicalSyncKernelReadyToRetireReport=v8.45',
      `domains=${this.domains.map(d => d.domain as string).join('|')}`,
      `codeReady=${this.domains.every(d => d.codeReady)}`,
      `realDeviceEvidencePresent=${this.domains.every(d => d.realDeviceEvidencePresent)}`,
      `readyToRetireLegacyReportOnly=${this.domains.every(d => d.readyToRetireLegacy)}`,
      'retirementExecutionPerformed=false',
      'legacyDeleted=false',
      'legacyDisabled=false',
      'redacted=true'
    ].join(',')
  }

  get codeReady(): boolean {
    const allCases = Object.values(CanonicalSyncKernelReadyToRetireDomain)
    return this.domains.length === allCases.length &&
      this.domains.every(d => d.codeReady) &&
      !this.retirementExecutionPerformed &&
      !this.legacyDeleted &&
      !this.legacyDisabled
  }

  get allReadyToRetireLegacyReportOnly(): boolean {
    return this.codeReady && this.domains.every(d => d.readyToRetireLegacy)
  }

  get allRealDeviceEvidencePresent(): boolean {
    return this.domains.every(d => d.realDeviceEvidencePresent)
  }

  get blockers(): CanonicalSyncKernelCompletionBlocker[] {
    const allBlockers: CanonicalSyncKernelCompletionBlocker[] = []
    for (const d of this.domains) {
      for (const b of d.blockers) {
        allBlockers.push(b)
      }
    }
    if (this.retirementExecutionPerformed) {
      allBlockers.push(CanonicalSyncKernelCompletionBlocker.RETIREMENT_EXECUTION_ATTEMPTED)
    }
    if (this.legacyDeleted || this.legacyDisabled) {
      allBlockers.push(CanonicalSyncKernelCompletionBlocker.LEGACY_DELETION_ATTEMPTED)
    }
    return CanonicalSyncKernelCompletionDomainReadiness['unique'](allBlockers)
  }

  static v845CodeCompleteAwaitingDeviceEvidence(): CanonicalSyncKernelDomainReadyToRetireReport {
    return new CanonicalSyncKernelDomainReadyToRetireReport(
      Object.values(CanonicalSyncKernelReadyToRetireDomain).map(d =>
        new CanonicalSyncKernelDomainReadyToRetireReadiness(d)
      )
    )
  }

  static v845ReadyWithDeviceEvidence(): CanonicalSyncKernelDomainReadyToRetireReport {
    return new CanonicalSyncKernelDomainReadyToRetireReport(
      Object.values(CanonicalSyncKernelReadyToRetireDomain).map(d =>
        new CanonicalSyncKernelDomainReadyToRetireReadiness(d, true, true, true, true, true, true, true)
      )
    )
  }
}

// ── Canonical Sync Kernel Completion Scorecard ──

export class CanonicalSyncKernelCompletionScorecard {
  itemResults: CanonicalSyncKernelCompletionScorecardItemResult[]
  domainCompletionReadiness: CanonicalSyncKernelCompletionDomainReadiness[]
  domainReadinessReport: CanonicalSyncKernelDomainReadyToRetireReport
  unresolvedBlockers: CanonicalSyncKernelCompletionBlocker[]
  status: CanonicalSyncKernelCompletionStatus
  blockers: CanonicalSyncKernelCompletionBlocker[]
  diagnosticsSummary: string

  constructor(
    itemResults: CanonicalSyncKernelCompletionScorecardItemResult[],
    domainCompletionReadiness: CanonicalSyncKernelCompletionDomainReadiness[] | null = null,
    domainReadinessReport: CanonicalSyncKernelDomainReadyToRetireReport | null = null,
    unresolvedBlockers: CanonicalSyncKernelCompletionBlocker[] = [],
    retirementReportOnlyReady: boolean = false
  ) {
    const completedByItem: Record<string, boolean> = {}
    for (const r of itemResults) {
      completedByItem[r.item as string] = r.complete
    }
    const allItems = Object.values(CanonicalSyncKernelCompletionScorecardItem)
    const normalizedItems = allItems.map(item =>
      new CanonicalSyncKernelCompletionScorecardItemResult(item, completedByItem[item as string] ?? false)
    )

    const resolvedDomainReadiness = domainCompletionReadiness ??
      CanonicalSyncKernelCompletionDomainReadiness.v845CodeCompleteAwaitingDeviceEvidence()
    const resolvedReport = domainReadinessReport ??
      CanonicalSyncKernelDomainReadyToRetireReport.v845CodeCompleteAwaitingDeviceEvidence()

    const blockers: CanonicalSyncKernelCompletionBlocker[] = [...unresolvedBlockers]
    for (const result of normalizedItems) {
      if (!result.complete) {
        blockers.push(scorecardItemBlocker(result.item))
      }
    }
    if (!resolvedReport.codeReady) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.DOMAIN_INCOMPLETE)
    }
    if (resolvedDomainReadiness.length !== Object.values(CanonicalSyncKernelCompletionDomain).length ||
      !resolvedDomainReadiness.every(d => d.codeComplete)) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.DOMAIN_INCOMPLETE)
    }
    for (const d of resolvedDomainReadiness) {
      for (const b of d.blockers) {
        if (b !== CanonicalSyncKernelCompletionBlocker.REAL_DEVICE_EVIDENCE_MISSING) {
          blockers.push(b)
        }
      }
    }
    for (const b of resolvedReport.blockers) {
      if (b !== CanonicalSyncKernelCompletionBlocker.REAL_DEVICE_EVIDENCE_MISSING) {
        blockers.push(b)
      }
    }
    const uniqueBlockers = CanonicalSyncKernelCompletionDomainReadiness['unique'](blockers)

    const codeItemsComplete = normalizedItems
      .filter(r => isCodeCompletionItem(r.item))
      .every(r => r.complete)
    const domainsCodeComplete = resolvedDomainReadiness.length ===
      Object.values(CanonicalSyncKernelCompletionDomain).length &&
      resolvedDomainReadiness.every(d => d.codeComplete)
    const deviceEvidencePresent = normalizedItems
      .find(r => r.item === CanonicalSyncKernelCompletionScorecardItem.REAL_DEVICE_EVIDENCE_REQUIRED)?.complete === true &&
      resolvedDomainReadiness.every(d => d.realDeviceEvidencePresent) &&
      resolvedReport.allRealDeviceEvidencePresent

    let status: CanonicalSyncKernelCompletionStatus
    if (uniqueBlockers.some(b =>
      b === CanonicalSyncKernelCompletionBlocker.LEGACY_DELETION_ATTEMPTED ||
      b === CanonicalSyncKernelCompletionBlocker.SENSITIVE_EVIDENCE_LEAK ||
      b === CanonicalSyncKernelCompletionBlocker.UNSAFE_CANONICAL_DEFAULT ||
      b === CanonicalSyncKernelCompletionBlocker.RELEASE_DEFAULT_CANONICAL ||
      b === CanonicalSyncKernelCompletionBlocker.SECURITY_BYPASS_DETECTED)) {
      status = CanonicalSyncKernelCompletionStatus.UNSAFE
    } else if (uniqueBlockers.some(b =>
      b === CanonicalSyncKernelCompletionBlocker.DIAGNOSTICS_NOT_REDACTED ||
      b === CanonicalSyncKernelCompletionBlocker.RETIREMENT_EXECUTION_ATTEMPTED ||
      b === CanonicalSyncKernelCompletionBlocker.LEGACY_DELETION_ATTEMPTED ||
      b === CanonicalSyncKernelCompletionBlocker.SENSITIVE_EVIDENCE_LEAK) ||
      unresolvedBlockers.length > 0) {
      status = CanonicalSyncKernelCompletionStatus.BLOCKED
    } else if (!codeItemsComplete || !domainsCodeComplete || !resolvedReport.codeReady) {
      status = CanonicalSyncKernelCompletionStatus.INCOMPLETE
    } else if (!deviceEvidencePresent) {
      status = CanonicalSyncKernelCompletionStatus.CODE_COMPLETE_NEEDS_DEVICE_EVIDENCE
    } else if (retirementReportOnlyReady || resolvedReport.allReadyToRetireLegacyReportOnly) {
      status = CanonicalSyncKernelCompletionStatus.READY_TO_RETIRE_LEGACY_REPORT_ONLY
    } else {
      status = CanonicalSyncKernelCompletionStatus.READY_FOR_MANUAL_SWITCH_TRIAL
    }

    this.itemResults = normalizedItems
    this.domainCompletionReadiness = [...resolvedDomainReadiness].sort(
      (a, b) => (a.domain as string).localeCompare(b.domain as string)
    )
    this.domainReadinessReport = resolvedReport
    this.unresolvedBlockers = unresolvedBlockers
    this.status = status
    this.blockers = status === CanonicalSyncKernelCompletionStatus.CODE_COMPLETE_NEEDS_DEVICE_EVIDENCE
      ? CanonicalSyncKernelCompletionDomainReadiness['unique']([...uniqueBlockers, CanonicalSyncKernelCompletionBlocker.REAL_DEVICE_EVIDENCE_MISSING])
      : uniqueBlockers
    this.diagnosticsSummary = [
      'canonicalSyncKernelCompletionScorecard=v8.45',
      `status=${status}`,
      `codeComplete=${codeItemsComplete && domainsCodeComplete && resolvedReport.codeReady}`,
      `realDeviceEvidencePresent=${deviceEvidencePresent}`,
      `domainCompletionReady=${domainsCodeComplete}`,
      `domainCodeReady=${resolvedReport.codeReady}`,
      'retirementExecutionPerformed=false',
      'legacyDeleted=false',
      'legacyDisabled=false',
      `blockers=${this.blockers.join('|')}`,
      'redacted=true'
    ].join(',')
  }

  get codeComplete(): boolean {
    const byItem: Record<string, boolean> = {}
    for (const r of this.itemResults) {
      byItem[r.item as string] = r.complete
    }
    return Object.values(CanonicalSyncKernelCompletionScorecardItem)
      .filter(item => isCodeCompletionItem(item))
      .every(item => byItem[item as string] === true) &&
      this.domainCompletionReadiness.length === Object.values(CanonicalSyncKernelCompletionDomain).length &&
      this.domainCompletionReadiness.every(d => d.codeComplete) &&
      this.domainReadinessReport.codeReady &&
      this.blockers.indexOf(CanonicalSyncKernelCompletionBlocker.DOMAIN_INCOMPLETE) < 0 &&
      this.blockers.indexOf(CanonicalSyncKernelCompletionBlocker.DIAGNOSTICS_NOT_REDACTED) < 0 &&
      this.blockers.indexOf(CanonicalSyncKernelCompletionBlocker.LEGACY_COMPATIBILITY_PROOF_MISSING) < 0 &&
      this.blockers.indexOf(CanonicalSyncKernelCompletionBlocker.SWITCH_BACK_PROOF_MISSING) < 0 &&
      this.unresolvedBlockers.length === 0
  }

  get realDeviceEvidencePresent(): boolean {
    return this.itemResults.find(r => r.item === CanonicalSyncKernelCompletionScorecardItem.REAL_DEVICE_EVIDENCE_REQUIRED)?.complete === true &&
      this.domainCompletionReadiness.every(d => d.realDeviceEvidencePresent) &&
      this.domainReadinessReport.allRealDeviceEvidencePresent
  }

  static v845(
    inventoryRuntimeComplete: boolean = true,
    diffLWWRuntimeComplete: boolean = true,
    existenceTruthComplete: boolean = true,
    nonAudioApplyRuntimeComplete: boolean = true,
    audioUploadRuntimeComplete: boolean = true,
    readRuntimeComplete: boolean = true,
    masterSwitchComplete: boolean = true,
    legacyCompatibilityProofComplete: boolean = true,
    switchBackProofComplete: boolean = true,
    diagnosticsRedacted: boolean = true,
    realDeviceEvidencePresent: boolean = false,
    domainCompletionReadiness: CanonicalSyncKernelCompletionDomainReadiness[] | null = null,
    domainReadinessReport: CanonicalSyncKernelDomainReadyToRetireReport | null = null,
    unresolvedBlockers: CanonicalSyncKernelCompletionBlocker[] = []
  ): CanonicalSyncKernelCompletionScorecard {
    const resolvedDomainReadiness = domainCompletionReadiness ??
      (realDeviceEvidencePresent
        ? CanonicalSyncKernelCompletionDomainReadiness.v845ReadyWithDeviceEvidence()
        : CanonicalSyncKernelCompletionDomainReadiness.v845CodeCompleteAwaitingDeviceEvidence())
    const resolvedReport = domainReadinessReport ??
      (realDeviceEvidencePresent
        ? CanonicalSyncKernelDomainReadyToRetireReport.v845ReadyWithDeviceEvidence()
        : CanonicalSyncKernelDomainReadyToRetireReport.v845CodeCompleteAwaitingDeviceEvidence())
    return new CanonicalSyncKernelCompletionScorecard(
      [
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.INVENTORY_RUNTIME_COMPLETE, inventoryRuntimeComplete),
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.DIFF_LWW_RUNTIME_COMPLETE, diffLWWRuntimeComplete),
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.EXISTENCE_TRUTH_COMPLETE, existenceTruthComplete),
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.NON_AUDIO_APPLY_RUNTIME_COMPLETE, nonAudioApplyRuntimeComplete),
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.AUDIO_UPLOAD_RUNTIME_COMPLETE, audioUploadRuntimeComplete),
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.READ_RUNTIME_COMPLETE, readRuntimeComplete),
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.MASTER_SWITCH_COMPLETE, masterSwitchComplete),
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.LEGACY_COMPATIBILITY_PROOF_COMPLETE, legacyCompatibilityProofComplete),
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.SWITCH_BACK_PROOF_COMPLETE, switchBackProofComplete),
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.DIAGNOSTICS_REDACTED, diagnosticsRedacted),
        new CanonicalSyncKernelCompletionScorecardItemResult(CanonicalSyncKernelCompletionScorecardItem.REAL_DEVICE_EVIDENCE_REQUIRED, realDeviceEvidencePresent)
      ],
      resolvedDomainReadiness,
      resolvedReport,
      unresolvedBlockers
    )
  }
}

// ── Evidence Types ──

export class CanonicalSyncKernelEvidenceModeTransition {
  fromMode: CanonicalKernelSwitchMode
  toMode: CanonicalKernelSwitchMode
  phase: string

  constructor(fromMode: CanonicalKernelSwitchMode, toMode: CanonicalKernelSwitchMode, phase: string) {
    this.fromMode = fromMode
    this.toMode = toMode
    this.phase = CanonicalProductionRedaction.redact(phase)
  }
}

export class CanonicalSyncKernelEvidenceObjectCounts {
  recordingMetadataCount: number
  libraryMetadataCount: number
  generatedArtifactCount: number
  tombstoneConflictCount: number
  audioUploadCandidateCount: number
  recordingExistenceCount: number

  constructor(
    recordingMetadataCount: number = 0,
    libraryMetadataCount: number = 0,
    generatedArtifactCount: number = 0,
    tombstoneConflictCount: number = 0,
    audioUploadCandidateCount: number = 0,
    recordingExistenceCount: number = 0
  ) {
    this.recordingMetadataCount = recordingMetadataCount
    this.libraryMetadataCount = libraryMetadataCount
    this.generatedArtifactCount = generatedArtifactCount
    this.tombstoneConflictCount = tombstoneConflictCount
    this.audioUploadCandidateCount = audioUploadCandidateCount
    this.recordingExistenceCount = recordingExistenceCount
  }
}

export class CanonicalSyncKernelEvidenceCacheCounts {
  hitCount: number
  missCount: number
  staleCount: number
  errorCount: number

  constructor(hitCount: number = 0, missCount: number = 0, staleCount: number = 0, errorCount: number = 0) {
    this.hitCount = hitCount
    this.missCount = missCount
    this.staleCount = staleCount
    this.errorCount = errorCount
  }
}

export class CanonicalSyncKernelEvidencePlanCounts {
  canonicalPlanUsedCount: number
  legacyFallbackCount: number
  blockedPlanCount: number

  constructor(canonicalPlanUsedCount: number = 0, legacyFallbackCount: number = 0, blockedPlanCount: number = 0) {
    this.canonicalPlanUsedCount = canonicalPlanUsedCount
    this.legacyFallbackCount = legacyFallbackCount
    this.blockedPlanCount = blockedPlanCount
  }
}

export class CanonicalSyncKernelEvidenceExecutionCounts {
  successCount: number
  failureCount: number

  constructor(successCount: number = 0, failureCount: number = 0) {
    this.successCount = successCount
    this.failureCount = failureCount
  }
}

export class CanonicalSyncKernelEvidenceReadDivergence {
  equivalentCount: number
  divergentCount: number
  pathOrContentLeakRiskCount: number

  constructor(equivalentCount: number = 0, divergentCount: number = 0, pathOrContentLeakRiskCount: number = 0) {
    this.equivalentCount = equivalentCount
    this.divergentCount = divergentCount
    this.pathOrContentLeakRiskCount = pathOrContentLeakRiskCount
  }
}

export class CanonicalSyncKernelEvidenceRedactionProof {
  redacted: boolean
  sensitiveInputDetected: boolean
  sensitiveOutputDetected: boolean
  excludedSensitivePayloads: string[]

  constructor(
    redacted: boolean,
    sensitiveInputDetected: boolean,
    sensitiveOutputDetected: boolean,
    excludedSensitivePayloads: string[]
  ) {
    this.redacted = redacted
    this.sensitiveInputDetected = sensitiveInputDetected
    this.sensitiveOutputDetected = sensitiveOutputDetected
    this.excludedSensitivePayloads = excludedSensitivePayloads
  }
}

export class CanonicalSyncKernelEvidencePackage {
  modeTransitions: CanonicalSyncKernelEvidenceModeTransition[]
  objectCounts: CanonicalSyncKernelEvidenceObjectCounts
  cacheCounts: CanonicalSyncKernelEvidenceCacheCounts
  planCounts: CanonicalSyncKernelEvidencePlanCounts
  applyCounts: CanonicalSyncKernelEvidenceExecutionCounts
  uploadCounts: CanonicalSyncKernelEvidenceExecutionCounts
  readDivergence: CanonicalSyncKernelEvidenceReadDivergence
  switchBackProofSummary: string
  redactionProof: CanonicalSyncKernelEvidenceRedactionProof
  redactedDiagnostics: string[]
  redacted: boolean
  diagnosticsSummary: string

  constructor(
    modeTransitions: CanonicalSyncKernelEvidenceModeTransition[],
    objectCounts: CanonicalSyncKernelEvidenceObjectCounts,
    cacheCounts: CanonicalSyncKernelEvidenceCacheCounts,
    planCounts: CanonicalSyncKernelEvidencePlanCounts,
    applyCounts: CanonicalSyncKernelEvidenceExecutionCounts,
    uploadCounts: CanonicalSyncKernelEvidenceExecutionCounts,
    readDivergence: CanonicalSyncKernelEvidenceReadDivergence,
    switchBackProofSummary: string,
    redactionProof: CanonicalSyncKernelEvidenceRedactionProof,
    redactedDiagnostics: string[],
    redacted: boolean,
    diagnosticsSummary: string
  ) {
    this.modeTransitions = modeTransitions
    this.objectCounts = objectCounts
    this.cacheCounts = cacheCounts
    this.planCounts = planCounts
    this.applyCounts = applyCounts
    this.uploadCounts = uploadCounts
    this.readDivergence = readDivergence
    this.switchBackProofSummary = switchBackProofSummary
    this.redactionProof = redactionProof
    this.redactedDiagnostics = redactedDiagnostics
    this.redacted = redacted
    this.diagnosticsSummary = diagnosticsSummary
  }
}

export class CanonicalSyncKernelEvidenceExportInput {
  modeTransitions: CanonicalSyncKernelEvidenceModeTransition[]
  objectCounts: CanonicalSyncKernelEvidenceObjectCounts
  cacheCounts: CanonicalSyncKernelEvidenceCacheCounts
  planCounts: CanonicalSyncKernelEvidencePlanCounts
  applyCounts: CanonicalSyncKernelEvidenceExecutionCounts
  uploadCounts: CanonicalSyncKernelEvidenceExecutionCounts
  readDivergence: CanonicalSyncKernelEvidenceReadDivergence
  switchBackProof: CanonicalLegacySwitchBackProofResult | null
  rawDiagnosticLines: string[]

  constructor(
    modeTransitions: CanonicalSyncKernelEvidenceModeTransition[] = [],
    objectCounts: CanonicalSyncKernelEvidenceObjectCounts = new CanonicalSyncKernelEvidenceObjectCounts(),
    cacheCounts: CanonicalSyncKernelEvidenceCacheCounts = new CanonicalSyncKernelEvidenceCacheCounts(),
    planCounts: CanonicalSyncKernelEvidencePlanCounts = new CanonicalSyncKernelEvidencePlanCounts(),
    applyCounts: CanonicalSyncKernelEvidenceExecutionCounts = new CanonicalSyncKernelEvidenceExecutionCounts(),
    uploadCounts: CanonicalSyncKernelEvidenceExecutionCounts = new CanonicalSyncKernelEvidenceExecutionCounts(),
    readDivergence: CanonicalSyncKernelEvidenceReadDivergence = new CanonicalSyncKernelEvidenceReadDivergence(),
    switchBackProof: CanonicalLegacySwitchBackProofResult | null = null,
    rawDiagnosticLines: string[] = []
  ) {
    this.modeTransitions = modeTransitions
    this.objectCounts = objectCounts
    this.cacheCounts = cacheCounts
    this.planCounts = planCounts
    this.applyCounts = applyCounts
    this.uploadCounts = uploadCounts
    this.readDivergence = readDivergence
    this.switchBackProof = switchBackProof
    this.rawDiagnosticLines = rawDiagnosticLines
  }
}

export class CanonicalSyncKernelEvidenceExporter {
  constructor() {}

  export(input: CanonicalSyncKernelEvidenceExportInput): CanonicalSyncKernelEvidencePackage {
    const redactedLines = input.rawDiagnosticLines.map(l => CanonicalProductionRedaction.redact(l))
    const sensitiveInputDetected = input.rawDiagnosticLines.some(
      l => CanonicalProductionRedaction.containsSensitiveSignal(l)
    )
    const sensitiveOutputDetected = redactedLines.some(
      l => CanonicalProductionRedaction.containsSensitiveSignal(l)
    )
    const redactionProof = new CanonicalSyncKernelEvidenceRedactionProof(
      !sensitiveOutputDetected,
      sensitiveInputDetected,
      sensitiveOutputDetected,
      [
        'absolutePaths', 'fullHashes', 'secrets', 'fingerprints',
        'requestResponseBodies', 'transcriptNoteSummaryProviderContent', 'audioBytes'
      ]
    )
    const switchBackSummary = CanonicalProductionRedaction.redact(
      input.switchBackProof?.diagnosticsSummary ?? 'switchBackProof=missing'
    )
    const diagnosticsSummary = [
      'canonicalSyncKernelEvidencePackage=v8.45',
      `modeTransitions=${input.modeTransitions.length}`,
      `recordingMetadataCount=${input.objectCounts.recordingMetadataCount}`,
      `cacheHitCount=${input.cacheCounts.hitCount}`,
      `canonicalPlanUsedCount=${input.planCounts.canonicalPlanUsedCount}`,
      `legacyFallbackCount=${input.planCounts.legacyFallbackCount}`,
      `applySuccess=${input.applyCounts.successCount}`,
      `applyFailure=${input.applyCounts.failureCount}`,
      `uploadSuccess=${input.uploadCounts.successCount}`,
      `uploadFailure=${input.uploadCounts.failureCount}`,
      `readDivergent=${input.readDivergence.divergentCount}`,
      `switchBackProven=${input.switchBackProof?.isProven === true}`,
      `redacted=${!sensitiveOutputDetected}`
    ].join(',')

    return new CanonicalSyncKernelEvidencePackage(
      input.modeTransitions,
      input.objectCounts,
      input.cacheCounts,
      input.planCounts,
      input.applyCounts,
      input.uploadCounts,
      input.readDivergence,
      switchBackSummary,
      redactionProof,
      redactedLines,
      redactionProof.redacted,
      diagnosticsSummary
    )
  }
}

// ── Canonical Sync Kernel Manual Switch Gate ──

export class CanonicalSyncKernelManualSwitchGateContext {
  scorecard: CanonicalSyncKernelCompletionScorecard
  compatibilityProof: CanonicalLegacyCompatibilityMatrix
  switchBackProof: CanonicalLegacySwitchBackProofResult
  realisticRootSwitchBackProofReady: boolean
  defaultMode: CanonicalKernelSwitchMode
  releaseMode: CanonicalKernelSwitchMode
  allDiagnosticsRedacted: boolean
  legacyFallbackAvailable: boolean
  ownerApproved: boolean
  manualBackupAcknowledged: boolean
  unresolvedBlockers: CanonicalSyncKernelCompletionBlocker[]

  constructor(
    scorecard: CanonicalSyncKernelCompletionScorecard,
    compatibilityProof: CanonicalLegacyCompatibilityMatrix = CanonicalLegacyCompatibilityMatrix.defaultV844(),
    switchBackProof: CanonicalLegacySwitchBackProofResult,
    realisticRootSwitchBackProofReady: boolean = true,
    defaultMode: CanonicalKernelSwitchMode = CanonicalKernelSwitchMode.OLD_KERNEL,
    releaseMode: CanonicalKernelSwitchMode = CanonicalKernelSwitchMode.OLD_KERNEL,
    allDiagnosticsRedacted: boolean = true,
    legacyFallbackAvailable: boolean = true,
    ownerApproved: boolean = false,
    manualBackupAcknowledged: boolean = false,
    unresolvedBlockers: CanonicalSyncKernelCompletionBlocker[] = []
  ) {
    this.scorecard = scorecard
    this.compatibilityProof = compatibilityProof
    this.switchBackProof = switchBackProof
    this.realisticRootSwitchBackProofReady = realisticRootSwitchBackProofReady
    this.defaultMode = defaultMode
    this.releaseMode = releaseMode
    this.allDiagnosticsRedacted = allDiagnosticsRedacted
    this.legacyFallbackAvailable = legacyFallbackAvailable
    this.ownerApproved = ownerApproved
    this.manualBackupAcknowledged = manualBackupAcknowledged
    this.unresolvedBlockers = unresolvedBlockers
  }
}

export class CanonicalSyncKernelManualSwitchGateResult {
  allowedForManualTrial: boolean
  releaseDefaultAllowed: boolean
  blockers: CanonicalSyncKernelCompletionBlocker[]
  allowedMode: CanonicalKernelSwitchMode
  diagnosticsSummary: string

  constructor(
    allowedForManualTrial: boolean,
    releaseDefaultAllowed: boolean,
    blockers: CanonicalSyncKernelCompletionBlocker[],
    allowedMode: CanonicalKernelSwitchMode,
    diagnosticsSummary: string
  ) {
    this.allowedForManualTrial = allowedForManualTrial
    this.releaseDefaultAllowed = releaseDefaultAllowed
    this.blockers = blockers
    this.allowedMode = allowedMode
    this.diagnosticsSummary = diagnosticsSummary
  }
}

export class CanonicalSyncKernelManualSwitchGate {
  constructor() {}

  evaluate(context: CanonicalSyncKernelManualSwitchGateContext): CanonicalSyncKernelManualSwitchGateResult {
    const blockers: CanonicalSyncKernelCompletionBlocker[] = []
    if (!context.scorecard.codeComplete) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.DOMAIN_INCOMPLETE)
    }
    if (!context.compatibilityProof.isFullyProven) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.COMPATIBILITY_PROOF_MISSING)
    }
    if (!context.switchBackProof.isProven) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.SWITCH_BACK_PROOF_MISSING)
    }
    if (!context.realisticRootSwitchBackProofReady) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.REALISTIC_ROOT_SWITCH_BACK_PROOF_MISSING)
    }
    if (context.defaultMode !== CanonicalKernelSwitchMode.OLD_KERNEL) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.DEFAULT_OLD_KERNEL_MISSING)
    }
    if (context.releaseMode !== CanonicalKernelSwitchMode.OLD_KERNEL) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.RELEASE_DEFAULT_CANONICAL)
    }
    if (!context.allDiagnosticsRedacted) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.DIAGNOSTICS_NOT_REDACTED)
    }
    if (!context.legacyFallbackAvailable) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.LEGACY_FALLBACK_UNAVAILABLE)
    }
    if (context.unresolvedBlockers.length > 0) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.UNRESOLVED_BLOCKER)
    }
    if (!context.ownerApproved) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.OWNER_APPROVAL_MISSING)
    }
    if (!context.manualBackupAcknowledged) {
      blockers.push(CanonicalSyncKernelCompletionBlocker.MANUAL_BACKUP_ACKNOWLEDGEMENT_MISSING)
    }

    const uniqueBlockers = CanonicalSyncKernelCompletionDomainReadiness['unique'](blockers)
    const allowed = uniqueBlockers.length === 0
    return new CanonicalSyncKernelManualSwitchGateResult(
      allowed,
      false,
      uniqueBlockers,
      allowed ? CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC : CanonicalKernelSwitchMode.BLOCKED,
      [
        'canonicalSyncKernelManualSwitchGate=v8.45',
        `allowedForManualTrial=${allowed}`,
        'releaseDefaultAllowed=false',
        `allowedMode=${allowed ? CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC : CanonicalKernelSwitchMode.BLOCKED}`,
        `defaultMode=${context.defaultMode}`,
        `releaseMode=${context.releaseMode}`,
        `realisticRootSwitchBackProofReady=${context.realisticRootSwitchBackProofReady}`,
        `manualBackupAcknowledged=${context.manualBackupAcknowledged}`,
        `ownerApproved=${context.ownerApproved}`,
        `blockers=${uniqueBlockers.join('|')}`,
        'redacted=true'
      ].join(',')
    )
  }
}
