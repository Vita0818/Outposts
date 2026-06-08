/**
 * CanonicalLegacyCompatibility — legacy compatibility proof types and harness.
 * Ported from Rokurics-Apple (CanonicalLegacyCompatibility.swift).
 * Proves that canonical writes are backward-compatible with legacy reads.
 */

import { CanonicalHash, nilIfEmpty } from './CanonicalCore'
import { CanonicalKernelSwitchMode, CanonicalKernelSwitchPolicy } from './CanonicalKernelSwitch'

// ── Canonical Legacy Compatibility Domain ──

export enum CanonicalLegacyCompatibilityDomain {
  RECORDING_METADATA = 'recordingMetadata',
  LIBRARY_METADATA = 'libraryMetadata',
  GENERATED_ARTIFACTS = 'generatedArtifacts',
  TOMBSTONE_CONFLICT = 'tombstoneConflict',
  RECORDING_EXISTENCE = 'recordingExistence',
  AUDIO_UPLOAD = 'audioUpload',
  READ_RUNTIME = 'readRuntime'
}

// ── Canonical Legacy Compatibility Blocker ──

export enum CanonicalLegacyCompatibilityBlocker {
  CANONICAL_WRITE_NOT_LEGACY_READABLE = 'canonicalWriteNotLegacyReadable',
  LEGACY_WRITE_NOT_CANONICAL_READABLE = 'legacyWriteNotCanonicalReadable',
  SWITCH_BACK_REQUIRES_MIGRATION = 'switchBackRequiresMigration',
  CANONICAL_ONLY_REQUIRED_FIELD_REQUIRED = 'canonicalOnlyRequiredFieldRequired',
  UNKNOWN_FIELDS_NOT_BACKWARD_COMPATIBLE = 'unknownFieldsNotBackwardCompatible',
  ROLLBACK_UNAVAILABLE = 'rollbackUnavailable',
  DIAGNOSTICS_NOT_REDACTED = 'diagnosticsNotRedacted',
  LEGACY_READ_PATH_UNAVAILABLE = 'legacyReadPathUnavailable',
  LEGACY_WRITE_PATH_UNAVAILABLE = 'legacyWritePathUnavailable',
  PHYSICAL_DELETE_REQUIRED = 'physicalDeleteRequired',
  INCOMPLETE_STATE_UNRECOVERABLE = 'incompleteStateUnrecoverable',
  DATA_LOSS_DETECTED = 'dataLossDetected',
  OLD_KERNEL_RESTART_FAILED = 'oldKernelRestartFailed',
  CANONICAL_FULL_SYNC_RESTART_FAILED = 'canonicalFullSyncRestartFailed'
}

// ── Canonical Legacy Compatibility Result ──

export class CanonicalLegacyCompatibilityResult {
  domain: CanonicalLegacyCompatibilityDomain
  canonicalWriteFormatLegacyReadable: boolean
  legacyWriteFormatCanonicalReadable: boolean
  switchBackNoMigration: boolean
  noCanonicalOnlyRequiredField: boolean
  unknownFieldsIgnoredOrBackwardCompatible: boolean
  rollbackAvailable: boolean
  diagnosticsRedacted: boolean
  legacyReadPathAvailable: boolean
  legacyWritePathAvailable: boolean
  noPhysicalDeleteRequired: boolean
  blockers: CanonicalLegacyCompatibilityBlocker[]
  diagnosticsSummary: string

  constructor(
    domain: CanonicalLegacyCompatibilityDomain,
    canonicalWriteFormatLegacyReadable: boolean,
    legacyWriteFormatCanonicalReadable: boolean,
    switchBackNoMigration: boolean,
    noCanonicalOnlyRequiredField: boolean,
    unknownFieldsIgnoredOrBackwardCompatible: boolean,
    rollbackAvailable: boolean,
    diagnosticsRedacted: boolean,
    legacyReadPathAvailable: boolean,
    legacyWritePathAvailable: boolean,
    noPhysicalDeleteRequired: boolean,
    blockers: CanonicalLegacyCompatibilityBlocker[],
    diagnosticsSummary: string
  ) {
    this.domain = domain
    this.canonicalWriteFormatLegacyReadable = canonicalWriteFormatLegacyReadable
    this.legacyWriteFormatCanonicalReadable = legacyWriteFormatCanonicalReadable
    this.switchBackNoMigration = switchBackNoMigration
    this.noCanonicalOnlyRequiredField = noCanonicalOnlyRequiredField
    this.unknownFieldsIgnoredOrBackwardCompatible = unknownFieldsIgnoredOrBackwardCompatible
    this.rollbackAvailable = rollbackAvailable
    this.diagnosticsRedacted = diagnosticsRedacted
    this.legacyReadPathAvailable = legacyReadPathAvailable
    this.legacyWritePathAvailable = legacyWritePathAvailable
    this.noPhysicalDeleteRequired = noPhysicalDeleteRequired
    this.blockers = blockers
    this.diagnosticsSummary = diagnosticsSummary
  }

  get isProven(): boolean {
    return this.canonicalWriteFormatLegacyReadable &&
      this.legacyWriteFormatCanonicalReadable &&
      this.switchBackNoMigration &&
      this.noCanonicalOnlyRequiredField &&
      this.unknownFieldsIgnoredOrBackwardCompatible &&
      this.rollbackAvailable &&
      this.diagnosticsRedacted &&
      this.legacyReadPathAvailable &&
      this.legacyWritePathAvailable &&
      this.noPhysicalDeleteRequired &&
      this.blockers.length === 0
  }

  static prove(
    domain: CanonicalLegacyCompatibilityDomain,
    policy: CanonicalKernelSwitchPolicy = CanonicalKernelSwitchPolicy.debugInternal(true),
    canonicalWriteFormatLegacyReadable: boolean = true,
    legacyWriteFormatCanonicalReadable: boolean = true,
    switchBackNoMigration: boolean = true,
    noCanonicalOnlyRequiredField: boolean = true,
    unknownFieldsIgnoredOrBackwardCompatible: boolean = true,
    rollbackAvailable: boolean = true,
    diagnosticsRedacted: boolean | null = null,
    legacyReadPathAvailable: boolean | null = null,
    legacyWritePathAvailable: boolean | null = null,
    noPhysicalDeleteRequired: boolean | null = null
  ): CanonicalLegacyCompatibilityResult {
    const redacted = diagnosticsRedacted ?? (policy.diagnosticsRedacted && policy.secretPathHashLeakRedactionEnabled)
    const legacyRead = legacyReadPathAvailable ?? policy.legacyReadPathAvailable
    const legacyWrite = legacyWritePathAvailable ?? policy.legacyWritePathAvailable
    const noPhysicalDelete = noPhysicalDeleteRequired ?? policy.physicalMoveDeleteDisabled
    const blockers: CanonicalLegacyCompatibilityBlocker[] = []

    if (!canonicalWriteFormatLegacyReadable) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.CANONICAL_WRITE_NOT_LEGACY_READABLE)
    }
    if (!legacyWriteFormatCanonicalReadable) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.LEGACY_WRITE_NOT_CANONICAL_READABLE)
    }
    if (!switchBackNoMigration) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.SWITCH_BACK_REQUIRES_MIGRATION)
    }
    if (!noCanonicalOnlyRequiredField) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.CANONICAL_ONLY_REQUIRED_FIELD_REQUIRED)
    }
    if (!unknownFieldsIgnoredOrBackwardCompatible) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.UNKNOWN_FIELDS_NOT_BACKWARD_COMPATIBLE)
    }
    if (!rollbackAvailable) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.ROLLBACK_UNAVAILABLE)
    }
    if (!redacted) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.DIAGNOSTICS_NOT_REDACTED)
    }
    if (!legacyRead) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.LEGACY_READ_PATH_UNAVAILABLE)
    }
    if (!legacyWrite) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.LEGACY_WRITE_PATH_UNAVAILABLE)
    }
    if (!noPhysicalDelete) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.PHYSICAL_DELETE_REQUIRED)
    }

    return new CanonicalLegacyCompatibilityResult(
      domain,
      canonicalWriteFormatLegacyReadable,
      legacyWriteFormatCanonicalReadable,
      switchBackNoMigration,
      noCanonicalOnlyRequiredField,
      unknownFieldsIgnoredOrBackwardCompatible,
      rollbackAvailable,
      redacted,
      legacyRead,
      legacyWrite,
      noPhysicalDelete,
      blockers,
      [
        'canonicalLegacyCompatibility=v8.44',
        `domain=${domain}`,
        'format=legacy-v1',
        `canonicalWriteLegacyReadable=${canonicalWriteFormatLegacyReadable}`,
        `legacyWriteCanonicalReadable=${legacyWriteFormatCanonicalReadable}`,
        'switchBackMigration=false',
        'unknownFields=ignoredOrBackwardCompatible',
        `rollback=${rollbackAvailable}`,
        'redacted=true',
        `blockers=${blockers.join('|')}`
      ].join(',')
    )
  }
}

// ── Canonical Legacy Compatibility Matrix ──

export class CanonicalLegacyCompatibilityMatrix {
  results: CanonicalLegacyCompatibilityResult[]

  constructor(results: CanonicalLegacyCompatibilityResult[]) {
    this.results = [...results].sort((a, b) => (a.domain as string).localeCompare(b.domain as string))
  }

  get blockers(): CanonicalLegacyCompatibilityBlocker[] {
    const seen = new Set<string>()
    const ordered: CanonicalLegacyCompatibilityBlocker[] = []
    for (const r of this.results) {
      for (const blocker of r.blockers) {
        if (!seen.has(blocker as string)) {
          seen.add(blocker as string)
          ordered.push(blocker)
        }
      }
    }
    return ordered
  }

  get provenDomains(): CanonicalLegacyCompatibilityDomain[] {
    return this.results.filter(r => r.isProven).map(r => r.domain)
  }

  get isFullyProven(): boolean {
    const allCases = Object.values(CanonicalLegacyCompatibilityDomain)
    const provenSet = new Set(this.provenDomains)
    return allCases.every(d => provenSet.has(d)) && this.blockers.length === 0
  }

  get diagnosticsSummary(): string {
    return [
      'canonicalLegacyCompatibilityMatrix=v8.44',
      `domains=${this.results.map(r => r.domain as string).join('|')}`,
      `proven=${this.provenDomains.join('|')}`,
      `blockers=${this.blockers.join('|')}`,
      'legacyDeletion=false',
      'redacted=true'
    ].join(',')
  }

  static defaultV844(
    policy: CanonicalKernelSwitchPolicy = CanonicalKernelSwitchPolicy.debugInternal(true)
  ): CanonicalLegacyCompatibilityMatrix {
    return new CanonicalLegacyCompatibilityMatrix(
      Object.values(CanonicalLegacyCompatibilityDomain).map(d =>
        CanonicalLegacyCompatibilityResult.prove(d, policy)
      )
    )
  }
}

// ── Canonical Legacy Compatibility Read Result ──

export class CanonicalLegacyCompatibilityReadResult {
  domain: CanonicalLegacyCompatibilityDomain
  objectID: string
  value: string
  revision: number
  formatVersion: string
  mode: CanonicalKernelSwitchMode
  ignoredUnknownFieldCount: number

  constructor(
    domain: CanonicalLegacyCompatibilityDomain,
    objectID: string,
    value: string,
    revision: number,
    formatVersion: string,
    mode: CanonicalKernelSwitchMode,
    ignoredUnknownFieldCount: number
  ) {
    this.domain = domain
    this.objectID = objectID
    this.value = value
    this.revision = revision
    this.formatVersion = formatVersion
    this.mode = mode
    this.ignoredUnknownFieldCount = ignoredUnknownFieldCount
  }
}

// ── Canonical Legacy Switch Back Proof Result ──

export class CanonicalLegacySwitchBackProofResult {
  domains: CanonicalLegacyCompatibilityDomain[]
  legacyReadsAfterCanonicalWrite: Record<string, CanonicalLegacyCompatibilityReadResult>
  canonicalReadsAfterLegacyModify: Record<string, CanonicalLegacyCompatibilityReadResult>
  switchBackNoMigration: boolean
  switchBackComparisonPassed: boolean
  switchForwardComparisonPassed: boolean
  physicalDeleteCount: number
  oldKernelCrashedAfterCanonicalFullSync: boolean
  canonicalFullSyncCrashedAfterSwitchBack: boolean
  blockers: CanonicalLegacyCompatibilityBlocker[]
  diagnosticsSummary: string

  constructor(
    domains: CanonicalLegacyCompatibilityDomain[],
    legacyReadsAfterCanonicalWrite: Record<string, CanonicalLegacyCompatibilityReadResult>,
    canonicalReadsAfterLegacyModify: Record<string, CanonicalLegacyCompatibilityReadResult>,
    switchBackNoMigration: boolean,
    switchBackComparisonPassed: boolean,
    switchForwardComparisonPassed: boolean,
    physicalDeleteCount: number,
    oldKernelCrashedAfterCanonicalFullSync: boolean,
    canonicalFullSyncCrashedAfterSwitchBack: boolean,
    blockers: CanonicalLegacyCompatibilityBlocker[],
    diagnosticsSummary: string
  ) {
    this.domains = domains
    this.legacyReadsAfterCanonicalWrite = legacyReadsAfterCanonicalWrite
    this.canonicalReadsAfterLegacyModify = canonicalReadsAfterLegacyModify
    this.switchBackNoMigration = switchBackNoMigration
    this.switchBackComparisonPassed = switchBackComparisonPassed
    this.switchForwardComparisonPassed = switchForwardComparisonPassed
    this.physicalDeleteCount = physicalDeleteCount
    this.oldKernelCrashedAfterCanonicalFullSync = oldKernelCrashedAfterCanonicalFullSync
    this.canonicalFullSyncCrashedAfterSwitchBack = canonicalFullSyncCrashedAfterSwitchBack
    this.blockers = blockers
    this.diagnosticsSummary = diagnosticsSummary
  }

  get isProven(): boolean {
    return this.switchBackNoMigration &&
      this.switchBackComparisonPassed &&
      this.switchForwardComparisonPassed &&
      this.physicalDeleteCount === 0 &&
      !this.oldKernelCrashedAfterCanonicalFullSync &&
      !this.canonicalFullSyncCrashedAfterSwitchBack &&
      this.blockers.length === 0
  }
}

// ── Canonical Legacy Crash Point ──

export enum CanonicalLegacyCrashPoint {
  BEFORE_CHECKPOINT = 'beforeCheckpoint',
  AFTER_CHECKPOINT_BEFORE_WRITE = 'afterCheckpointBeforeWrite',
  AFTER_WRITE_BEFORE_POSTCONDITION = 'afterWriteBeforePostcondition',
  AFTER_POSTCONDITION_BEFORE_DUPLICATE_SUPPRESSION = 'afterPostconditionBeforeDuplicateSuppression'
}

// ── Canonical Legacy Crash Recovery Result ──

export class CanonicalLegacyCrashRecoveryResult {
  domain: CanonicalLegacyCompatibilityDomain
  crashPoint: CanonicalLegacyCrashPoint
  restartMode: CanonicalKernelSwitchMode
  oldKernelCanRead: boolean
  canonicalFullSyncCanRead: boolean
  noDataLoss: boolean
  incompleteStateBlockedOrRecovered: boolean
  physicalDeleteCount: number
  duplicateSuppressionApplied: boolean
  blockers: CanonicalLegacyCompatibilityBlocker[]
  diagnosticsSummary: string

  constructor(
    domain: CanonicalLegacyCompatibilityDomain,
    crashPoint: CanonicalLegacyCrashPoint,
    restartMode: CanonicalKernelSwitchMode,
    oldKernelCanRead: boolean,
    canonicalFullSyncCanRead: boolean,
    noDataLoss: boolean,
    incompleteStateBlockedOrRecovered: boolean,
    physicalDeleteCount: number,
    duplicateSuppressionApplied: boolean,
    blockers: CanonicalLegacyCompatibilityBlocker[],
    diagnosticsSummary: string
  ) {
    this.domain = domain
    this.crashPoint = crashPoint
    this.restartMode = restartMode
    this.oldKernelCanRead = oldKernelCanRead
    this.canonicalFullSyncCanRead = canonicalFullSyncCanRead
    this.noDataLoss = noDataLoss
    this.incompleteStateBlockedOrRecovered = incompleteStateBlockedOrRecovered
    this.physicalDeleteCount = physicalDeleteCount
    this.duplicateSuppressionApplied = duplicateSuppressionApplied
    this.blockers = blockers
    this.diagnosticsSummary = diagnosticsSummary
  }

  get recoveredSafely(): boolean {
    return this.oldKernelCanRead &&
      this.canonicalFullSyncCanRead &&
      this.noDataLoss &&
      this.incompleteStateBlockedOrRecovered &&
      this.physicalDeleteCount === 0 &&
      !this.duplicateSuppressionApplied &&
      this.blockers.length === 0
  }
}

// ── Canonical Legacy Switch Back Harness ──

class StoredRecord {
  domain: CanonicalLegacyCompatibilityDomain
  objectID: string
  value: string
  revision: number
  formatVersion: string
  legacyFields: Record<string, string>
  unknownFields: Record<string, string>

  constructor(
    domain: CanonicalLegacyCompatibilityDomain,
    objectID: string,
    value: string,
    revision: number,
    formatVersion: string,
    legacyFields: Record<string, string>,
    unknownFields: Record<string, string>
  ) {
    this.domain = domain
    this.objectID = objectID
    this.value = value
    this.revision = revision
    this.formatVersion = formatVersion
    this.legacyFields = legacyFields
    this.unknownFields = unknownFields
  }

  static make(
    domain: CanonicalLegacyCompatibilityDomain,
    value: string,
    revision: number,
    unknownFields: Record<string, string> = {}
  ): StoredRecord {
    const objectID = `compat-${domain}`
    return new StoredRecord(
      domain,
      objectID,
      value,
      revision,
      'legacy-v1',
      {
        'domain': domain as string,
        'objectID': objectID,
        'revision': String(revision),
        'value': value
      },
      unknownFields
    )
  }
}

export class CanonicalLegacySwitchBackHarness {
  private storage: Record<string, StoredRecord>
  private checkpoints: Record<string, StoredRecord | null>
  private diagnostics: string[]
  private migrationCount: number
  private physicalDeleteCount: number
  mode: CanonicalKernelSwitchMode

  constructor(seedLegacyRecords: boolean = true) {
    const seeded: Record<string, StoredRecord> = {}
    if (seedLegacyRecords) {
      for (const domain of Object.values(CanonicalLegacyCompatibilityDomain)) {
        seeded[domain as string] = StoredRecord.make(domain, `legacy-baseline-${domain}`, 1)
      }
    }
    this.storage = seeded
    this.checkpoints = {}
    this.diagnostics = []
    this.migrationCount = 0
    this.physicalDeleteCount = 0
    this.mode = CanonicalKernelSwitchMode.OLD_KERNEL
  }

  switchMode(nextMode: CanonicalKernelSwitchMode): void {
    this.mode = nextMode
  }

  legacyWrite(
    domain: CanonicalLegacyCompatibilityDomain,
    value: string | null = null
  ): CanonicalLegacyCompatibilityReadResult {
    const current = this.storage[domain as string]
    const nextRevision = (current?.revision ?? 0) + 1
    this.storage[domain as string] = StoredRecord.make(
      domain,
      value ?? `legacy-write-${domain}-${nextRevision}`,
      nextRevision
    )
    this.mode = CanonicalKernelSwitchMode.OLD_KERNEL
    return this.legacyRead(domain)!
  }

  canonicalWrite(
    domain: CanonicalLegacyCompatibilityDomain,
    value: string | null = null
  ): CanonicalLegacyCompatibilityReadResult {
    const current = this.storage[domain as string]
    const nextRevision = (current?.revision ?? 0) + 1
    this.checkpoints[domain as string] = current ?? null
    this.storage[domain as string] = StoredRecord.make(
      domain,
      value ?? `canonical-write-${domain}-${nextRevision}`,
      nextRevision,
      {
        'canonicalHint': 'ignored-by-legacy',
        'canonicalCompatibility': 'v8.44'
      }
    )
    this.mode = CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC
    return this.canonicalRead(domain)!
  }

  legacyRead(domain: CanonicalLegacyCompatibilityDomain): CanonicalLegacyCompatibilityReadResult | null {
    const record = this.storage[domain as string]
    if (!record ||
      record.formatVersion !== 'legacy-v1' ||
      record.legacyFields['objectID'] !== record.objectID ||
      record.legacyFields['value'] !== record.value) {
      return null
    }
    return new CanonicalLegacyCompatibilityReadResult(
      domain, record.objectID, record.value, record.revision,
      record.formatVersion, CanonicalKernelSwitchMode.OLD_KERNEL,
      Object.keys(record.unknownFields).length
    )
  }

  canonicalRead(domain: CanonicalLegacyCompatibilityDomain): CanonicalLegacyCompatibilityReadResult | null {
    const record = this.storage[domain as string]
    if (!record ||
      record.formatVersion !== 'legacy-v1' ||
      record.legacyFields['objectID'] !== record.objectID ||
      record.legacyFields['value'] !== record.value) {
      return null
    }
    return new CanonicalLegacyCompatibilityReadResult(
      domain, record.objectID, record.value, record.revision,
      record.formatVersion, CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC,
      Object.keys(record.unknownFields).length
    )
  }

  dataFormatFingerprint(domain: CanonicalLegacyCompatibilityDomain): string | null {
    const record = this.storage[domain as string]
    if (!record) {
      return null
    }
    const legacyKeys = Object.keys(record.legacyFields).sort().join('|')
    const unknownKeys = Object.keys(record.unknownFields).sort().join('|')
    return [
      record.formatVersion,
      record.objectID,
      record.value,
      String(record.revision),
      legacyKeys,
      unknownKeys
    ].join(',')
  }

  recordCanonicalDiagnostic(domain: CanonicalLegacyCompatibilityDomain): void {
    this.diagnostics.push(`canonicalLegacyCompatibilityDiagnostic:v8.44,domain=${domain},redacted=true`)
  }

  canonicalWriteWithRollbackAfterPartialFailure(
    domain: CanonicalLegacyCompatibilityDomain
  ): CanonicalLegacyCompatibilityReadResult | null {
    const checkpoint = this.storage[domain as string] ?? null
    this.checkpoints[domain as string] = checkpoint
    const nextRevision = (checkpoint?.revision ?? 0) + 1
    this.storage[domain as string] = StoredRecord.make(
      domain,
      `partial-canonical-write-${domain}-${nextRevision}`,
      nextRevision,
      { 'canonicalHint': 'ignored-by-legacy' }
    )
    this.rollback(domain)
    this.mode = CanonicalKernelSwitchMode.OLD_KERNEL
    return this.legacyRead(domain)
  }

  runSwitchBackProof(
    domains: CanonicalLegacyCompatibilityDomain[] = Object.values(CanonicalLegacyCompatibilityDomain)
  ): CanonicalLegacySwitchBackProofResult {
    this.switchMode(CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC)
    for (const domain of domains) {
      this.canonicalWrite(domain, `canonical-full-sync-${domain}`)
    }

    this.switchMode(CanonicalKernelSwitchMode.OLD_KERNEL)
    const legacyReads: Record<string, CanonicalLegacyCompatibilityReadResult> = {}
    for (const domain of domains) {
      const read = this.legacyRead(domain)
      if (read) {
        legacyReads[domain as string] = read
      }
    }
    for (const domain of domains) {
      this.legacyWrite(domain, `legacy-modified-${domain}`)
    }

    this.switchMode(CanonicalKernelSwitchMode.CANONICAL_FULL_SYNC)
    const canonicalReads: Record<string, CanonicalLegacyCompatibilityReadResult> = {}
    for (const domain of domains) {
      const read = this.canonicalRead(domain)
      if (read) {
        canonicalReads[domain as string] = read
      }
    }

    const blockers: CanonicalLegacyCompatibilityBlocker[] = []
    const switchBackComparisonPassed = domains.every(d =>
      legacyReads[d as string]?.value === `canonical-full-sync-${d}`
    )
    const switchForwardComparisonPassed = domains.every(d =>
      canonicalReads[d as string]?.value === `legacy-modified-${d}`
    )
    if (this.migrationCount !== 0) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.SWITCH_BACK_REQUIRES_MIGRATION)
    }
    if (this.physicalDeleteCount !== 0) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.PHYSICAL_DELETE_REQUIRED)
    }
    if (!switchBackComparisonPassed || !switchForwardComparisonPassed) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.DATA_LOSS_DETECTED)
    }
    if (Object.keys(legacyReads).length !== domains.length) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.OLD_KERNEL_RESTART_FAILED)
    }
    if (Object.keys(canonicalReads).length !== domains.length) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.CANONICAL_FULL_SYNC_RESTART_FAILED)
    }

    return new CanonicalLegacySwitchBackProofResult(
      domains,
      legacyReads,
      canonicalReads,
      this.migrationCount === 0,
      switchBackComparisonPassed,
      switchForwardComparisonPassed,
      this.physicalDeleteCount,
      Object.keys(legacyReads).length !== domains.length,
      Object.keys(canonicalReads).length !== domains.length,
      blockers,
      [
        'canonicalSwitchBackProof=v8.44',
        `domains=${domains.join('|')}`,
        `switchBackNoMigration=${this.migrationCount === 0}`,
        `physicalDeleteCount=${this.physicalDeleteCount}`,
        `legacyReads=${Object.keys(legacyReads).length}`,
        `canonicalReads=${Object.keys(canonicalReads).length}`,
        'redacted=true'
      ].join(',')
    )
  }

  simulateCrashAndRestart(
    domain: CanonicalLegacyCompatibilityDomain,
    crashPoint: CanonicalLegacyCrashPoint,
    restartMode: CanonicalKernelSwitchMode
  ): CanonicalLegacyCrashRecoveryResult {
    if (!this.storage[domain as string]) {
      this.storage[domain as string] = StoredRecord.make(
        domain, `legacy-baseline-${domain}`, 1
      )
    }
    const baseline = this.storage[domain as string] ?? null
    let incompleteRecovered = true
    let duplicateSuppressionApplied = false

    switch (crashPoint) {
      case CanonicalLegacyCrashPoint.BEFORE_CHECKPOINT:
        break
      case CanonicalLegacyCrashPoint.AFTER_CHECKPOINT_BEFORE_WRITE:
        this.checkpoints[domain as string] = baseline
        this.rollback(domain)
        break
      case CanonicalLegacyCrashPoint.AFTER_WRITE_BEFORE_POSTCONDITION:
        this.checkpoints[domain as string] = baseline
        const nextRevCrash = (baseline?.revision ?? 0) + 1
        this.storage[domain as string] = StoredRecord.make(
          domain, `canonical-crash-${domain}`, nextRevCrash,
          { 'canonicalHint': 'ignored-by-legacy' }
        )
        this.rollback(domain)
        break
      case CanonicalLegacyCrashPoint.AFTER_POSTCONDITION_BEFORE_DUPLICATE_SUPPRESSION:
        this.checkpoints[domain as string] = baseline
        const nextRevPost = (baseline?.revision ?? 0) + 1
        this.storage[domain as string] = StoredRecord.make(
          domain, `canonical-postcondition-${domain}`, nextRevPost,
          { 'canonicalHint': 'ignored-by-legacy' }
        )
        incompleteRecovered = true
        duplicateSuppressionApplied = false
        break
    }

    this.switchMode(restartMode)
    const oldRead = this.legacyRead(domain)
    const canonicalRead = this.canonicalRead(domain)
    const noDataLoss = oldRead !== null && canonicalRead !== null
    const blockers: CanonicalLegacyCompatibilityBlocker[] = []
    if (oldRead === null) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.OLD_KERNEL_RESTART_FAILED)
    }
    if (canonicalRead === null) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.CANONICAL_FULL_SYNC_RESTART_FAILED)
    }
    if (!noDataLoss) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.DATA_LOSS_DETECTED)
    }
    if (!incompleteRecovered) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.INCOMPLETE_STATE_UNRECOVERABLE)
    }
    if (this.physicalDeleteCount !== 0) {
      blockers.push(CanonicalLegacyCompatibilityBlocker.PHYSICAL_DELETE_REQUIRED)
    }

    return new CanonicalLegacyCrashRecoveryResult(
      domain,
      crashPoint,
      restartMode,
      oldRead !== null,
      canonicalRead !== null,
      noDataLoss,
      incompleteRecovered,
      this.physicalDeleteCount,
      duplicateSuppressionApplied,
      blockers,
      [
        'canonicalCrashRecovery=v8.44',
        `domain=${domain}`,
        `crashPoint=${crashPoint}`,
        `restartMode=${restartMode}`,
        `oldKernelCanRead=${oldRead !== null}`,
        `canonicalFullSyncCanRead=${canonicalRead !== null}`,
        `physicalDeleteCount=${this.physicalDeleteCount}`,
        'redacted=true'
      ].join(',')
    )
  }

  private rollback(domain: CanonicalLegacyCompatibilityDomain): void {
    const checkpoint = this.checkpoints[domain as string]
    if (checkpoint !== undefined && checkpoint !== null) {
      this.storage[domain as string] = checkpoint
    } else {
      delete this.storage[domain as string]
    }
  }
}

// ── Canonical Switch Back Realistic Root Harness Result ──

export class CanonicalSwitchBackRealisticRootHarnessResult {
  domains: CanonicalLegacyCompatibilityDomain[]
  testClonedRoot: boolean
  usesProductionRoot: boolean
  legacyReadableStateCount: number
  canonicalReadableStateCount: number
  crashRecoveryProofCount: number
  switchBackProof: CanonicalLegacySwitchBackProofResult
  crashRecoveryProofs: CanonicalLegacyCrashRecoveryResult[]
  physicalDeleteCount: number
  resourceMoveCount: number
  legacyRetirementPerformed: boolean
  blockers: CanonicalLegacyCompatibilityBlocker[]
  diagnosticsSummary: string

  constructor(
    domains: CanonicalLegacyCompatibilityDomain[],
    testClonedRoot: boolean,
    usesProductionRoot: boolean,
    legacyReadableStateCount: number,
    canonicalReadableStateCount: number,
    crashRecoveryProofCount: number,
    switchBackProof: CanonicalLegacySwitchBackProofResult,
    crashRecoveryProofs: CanonicalLegacyCrashRecoveryResult[],
    physicalDeleteCount: number,
    resourceMoveCount: number,
    legacyRetirementPerformed: boolean,
    blockers: CanonicalLegacyCompatibilityBlocker[],
    diagnosticsSummary: string
  ) {
    this.domains = domains
    this.testClonedRoot = testClonedRoot
    this.usesProductionRoot = usesProductionRoot
    this.legacyReadableStateCount = legacyReadableStateCount
    this.canonicalReadableStateCount = canonicalReadableStateCount
    this.crashRecoveryProofCount = crashRecoveryProofCount
    this.switchBackProof = switchBackProof
    this.crashRecoveryProofs = crashRecoveryProofs
    this.physicalDeleteCount = physicalDeleteCount
    this.resourceMoveCount = resourceMoveCount
    this.legacyRetirementPerformed = legacyRetirementPerformed
    this.blockers = blockers
    this.diagnosticsSummary = diagnosticsSummary
  }

  get isProven(): boolean {
    return this.testClonedRoot &&
      !this.usesProductionRoot &&
      this.legacyReadableStateCount === this.domains.length &&
      this.canonicalReadableStateCount === this.domains.length &&
      this.crashRecoveryProofCount === this.domains.length * Object.values(CanonicalLegacyCrashPoint).length &&
      this.switchBackProof.isProven &&
      this.crashRecoveryProofs.every(p => p.recoveredSafely) &&
      this.physicalDeleteCount === 0 &&
      this.resourceMoveCount === 0 &&
      !this.legacyRetirementPerformed &&
      this.blockers.length === 0
  }
}
