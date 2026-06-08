using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncKernelCompletionStatus
{
    incomplete,
    codeCompleteNeedsDeviceEvidence,
    readyForManualSwitchTrial,
    blocked,
    readyToRetireLegacyReportOnly,
    @unsafe
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncKernelCompletionBlocker
{
    inventoryRuntimeIncomplete,
    diffLWWRuntimeIncomplete,
    existenceTruthIncomplete,
    nonAudioApplyRuntimeIncomplete,
    audioUploadRuntimeIncomplete,
    readRuntimeIncomplete,
    masterSwitchIncomplete,
    legacyCompatibilityProofMissing,
    switchBackProofMissing,
    diagnosticsNotRedacted,
    realDeviceEvidenceMissing,
    domainIncomplete,
    compatibilityProofMissing,
    defaultOldKernelMissing,
    releaseDefaultCanonical,
    legacyFallbackUnavailable,
    unresolvedBlocker,
    ownerApprovalMissing,
    manualBackupAcknowledgementMissing,
    retirementExecutionAttempted,
    legacyDeletionAttempted,
    sensitiveEvidenceLeak,
    unsafeCanonicalDefault,
    securityBypassDetected,
    realisticRootSwitchBackProofMissing,
    testsNotPassing,
    docsNotUpdated
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncKernelCompletionScorecardItem
{
    inventoryRuntimeComplete,
    diffLWWRuntimeComplete,
    existenceTruthComplete,
    nonAudioApplyRuntimeComplete,
    audioUploadRuntimeComplete,
    readRuntimeComplete,
    masterSwitchComplete,
    legacyCompatibilityProofComplete,
    switchBackProofComplete,
    diagnosticsRedacted,
    realDeviceEvidenceRequired
}

public static class CanonicalSyncKernelCompletionScorecardItemExtensions
{
    public static bool IsCodeCompletionItem(this CanonicalSyncKernelCompletionScorecardItem item)
    {
        return item != CanonicalSyncKernelCompletionScorecardItem.realDeviceEvidenceRequired;
    }

    public static CanonicalSyncKernelCompletionBlocker ToBlocker(this CanonicalSyncKernelCompletionScorecardItem item)
    {
        return item switch
        {
            CanonicalSyncKernelCompletionScorecardItem.inventoryRuntimeComplete =>
                CanonicalSyncKernelCompletionBlocker.inventoryRuntimeIncomplete,
            CanonicalSyncKernelCompletionScorecardItem.diffLWWRuntimeComplete =>
                CanonicalSyncKernelCompletionBlocker.diffLWWRuntimeIncomplete,
            CanonicalSyncKernelCompletionScorecardItem.existenceTruthComplete =>
                CanonicalSyncKernelCompletionBlocker.existenceTruthIncomplete,
            CanonicalSyncKernelCompletionScorecardItem.nonAudioApplyRuntimeComplete =>
                CanonicalSyncKernelCompletionBlocker.nonAudioApplyRuntimeIncomplete,
            CanonicalSyncKernelCompletionScorecardItem.audioUploadRuntimeComplete =>
                CanonicalSyncKernelCompletionBlocker.audioUploadRuntimeIncomplete,
            CanonicalSyncKernelCompletionScorecardItem.readRuntimeComplete =>
                CanonicalSyncKernelCompletionBlocker.readRuntimeIncomplete,
            CanonicalSyncKernelCompletionScorecardItem.masterSwitchComplete =>
                CanonicalSyncKernelCompletionBlocker.masterSwitchIncomplete,
            CanonicalSyncKernelCompletionScorecardItem.legacyCompatibilityProofComplete =>
                CanonicalSyncKernelCompletionBlocker.legacyCompatibilityProofMissing,
            CanonicalSyncKernelCompletionScorecardItem.switchBackProofComplete =>
                CanonicalSyncKernelCompletionBlocker.switchBackProofMissing,
            CanonicalSyncKernelCompletionScorecardItem.diagnosticsRedacted =>
                CanonicalSyncKernelCompletionBlocker.diagnosticsNotRedacted,
            CanonicalSyncKernelCompletionScorecardItem.realDeviceEvidenceRequired =>
                CanonicalSyncKernelCompletionBlocker.realDeviceEvidenceMissing,
            _ => CanonicalSyncKernelCompletionBlocker.realDeviceEvidenceMissing
        };
    }
}

public sealed class CanonicalSyncKernelCompletionScorecardItemResult : IEquatable<CanonicalSyncKernelCompletionScorecardItemResult>
{
    public CanonicalSyncKernelCompletionScorecardItem Item { get; }
    public bool Complete { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalSyncKernelCompletionScorecardItemResult(
        CanonicalSyncKernelCompletionScorecardItem item,
        bool complete,
        string? diagnosticsSummary = null)
    {
        Item = item;
        Complete = complete;
        DiagnosticsSummary = CanonicalSyncKernelEvidenceRedactor.Redact(
            diagnosticsSummary ?? $"item={item},complete={complete}");
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelCompletionScorecardItemResult other && Equals(other);
    public bool Equals(CanonicalSyncKernelCompletionScorecardItemResult? other) =>
        other is not null && Item == other.Item && Complete == other.Complete && DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => HashCode.Combine(Item, Complete, DiagnosticsSummary);
    public static bool operator ==(CanonicalSyncKernelCompletionScorecardItemResult l, CanonicalSyncKernelCompletionScorecardItemResult r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelCompletionScorecardItemResult l, CanonicalSyncKernelCompletionScorecardItemResult r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncKernelCompletionDomain
{
    recordingMetadata,
    libraryMetadata,
    generatedArtifacts,
    tombstoneConflict,
    recordingExistence,
    audioUpload,
    readRuntime,
    inventoryRuntime,
    syncDecisionRuntime,
    applyRuntime,
    kernelSwitch,
    legacyCompatibility
}

public sealed class CanonicalSyncKernelCompletionDomainReadiness : IEquatable<CanonicalSyncKernelCompletionDomainReadiness>
{
    public CanonicalSyncKernelCompletionDomain Domain { get; }
    public bool WriteExecutorReady { get; }
    public bool ReadRuntimeReady { get; }
    public bool SyncRuntimeOwnerReady { get; }
    public bool ApplyRuntimeReady { get; }
    public bool AudioRuntimeReady { get; }
    public bool LegacyFallbackReady { get; }
    public bool SwitchBackProofReady { get; }
    public bool DiagnosticsRedacted { get; }
    public bool TestsPass { get; }
    public bool DocsUpdated { get; }
    public bool RealDeviceEvidencePresent { get; }
    public bool CodeComplete { get; }
    public List<CanonicalSyncKernelCompletionBlocker> Blockers { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalSyncKernelCompletionDomainReadiness(
        CanonicalSyncKernelCompletionDomain domain,
        bool writeExecutorReady = true,
        bool readRuntimeReady = true,
        bool syncRuntimeOwnerReady = true,
        bool applyRuntimeReady = true,
        bool? audioRuntimeReady = null,
        bool legacyFallbackReady = true,
        bool switchBackProofReady = true,
        bool diagnosticsRedacted = true,
        bool testsPass = true,
        bool docsUpdated = true,
        bool realDeviceEvidencePresent = false,
        List<CanonicalSyncKernelCompletionBlocker>? blockers = null)
    {
        var resolvedAudioReady = audioRuntimeReady ?? true;
        var resolvedBlockers = blockers ?? new List<CanonicalSyncKernelCompletionBlocker>();
        if (!writeExecutorReady || !readRuntimeReady || !syncRuntimeOwnerReady || !applyRuntimeReady)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.domainIncomplete);
        if (domain == CanonicalSyncKernelCompletionDomain.audioUpload && !resolvedAudioReady)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.audioUploadRuntimeIncomplete);
        if (!legacyFallbackReady)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.legacyFallbackUnavailable);
        if (!switchBackProofReady)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.switchBackProofMissing);
        if (!diagnosticsRedacted)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.diagnosticsNotRedacted);
        if (!testsPass)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.testsNotPassing);
        if (!docsUpdated)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.docsNotUpdated);
        if (!realDeviceEvidencePresent)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.realDeviceEvidenceMissing);
        resolvedBlockers = Unique(resolvedBlockers);

        var codeComplete = writeExecutorReady
            && readRuntimeReady
            && syncRuntimeOwnerReady
            && applyRuntimeReady
            && resolvedAudioReady
            && legacyFallbackReady
            && switchBackProofReady
            && diagnosticsRedacted
            && testsPass
            && docsUpdated
            && !resolvedBlockers.Any(b => b != CanonicalSyncKernelCompletionBlocker.realDeviceEvidenceMissing);

        Domain = domain;
        WriteExecutorReady = writeExecutorReady;
        ReadRuntimeReady = readRuntimeReady;
        SyncRuntimeOwnerReady = syncRuntimeOwnerReady;
        ApplyRuntimeReady = applyRuntimeReady;
        AudioRuntimeReady = resolvedAudioReady;
        LegacyFallbackReady = legacyFallbackReady;
        SwitchBackProofReady = switchBackProofReady;
        DiagnosticsRedacted = diagnosticsRedacted;
        TestsPass = testsPass;
        DocsUpdated = docsUpdated;
        RealDeviceEvidencePresent = realDeviceEvidencePresent;
        CodeComplete = codeComplete;
        Blockers = resolvedBlockers;
        DiagnosticsSummary = string.Join(",",
            "canonicalSyncKernelCompletionDomain=v8.45",
            $"domain={domain}",
            $"writeExecutorReady={writeExecutorReady}",
            $"readRuntimeReady={readRuntimeReady}",
            $"syncRuntimeOwnerReady={syncRuntimeOwnerReady}",
            $"applyRuntimeReady={applyRuntimeReady}",
            $"audioRuntimeReady={resolvedAudioReady}",
            $"legacyFallbackReady={legacyFallbackReady}",
            $"switchBackProofReady={switchBackProofReady}",
            $"diagnosticsRedacted={diagnosticsRedacted}",
            $"testsPass={testsPass}",
            $"docsUpdated={docsUpdated}",
            $"realDeviceEvidencePresent={realDeviceEvidencePresent}",
            $"codeComplete={codeComplete}",
            $"blockers={string.Join("|", resolvedBlockers.Select(b => b.ToString()))}",
            "redacted=true"
        );
    }

    public static List<CanonicalSyncKernelCompletionDomainReadiness> V845CodeCompleteAwaitingDeviceEvidence()
    {
        return Enum.GetValues<CanonicalSyncKernelCompletionDomain>()
            .Select(d => new CanonicalSyncKernelCompletionDomainReadiness(d)).ToList();
    }

    public static List<CanonicalSyncKernelCompletionDomainReadiness> V845ReadyWithDeviceEvidence()
    {
        return Enum.GetValues<CanonicalSyncKernelCompletionDomain>()
            .Select(d => new CanonicalSyncKernelCompletionDomainReadiness(d, realDeviceEvidencePresent: true)).ToList();
    }

    private static List<CanonicalSyncKernelCompletionBlocker> Unique(List<CanonicalSyncKernelCompletionBlocker> blockers)
    {
        var seen = new HashSet<CanonicalSyncKernelCompletionBlocker>();
        var unique = new List<CanonicalSyncKernelCompletionBlocker>();
        foreach (var b in blockers)
        {
            if (!seen.Contains(b))
            {
                seen.Add(b);
                unique.Add(b);
            }
        }
        return unique;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelCompletionDomainReadiness other && Equals(other);
    public bool Equals(CanonicalSyncKernelCompletionDomainReadiness? other) =>
        other is not null &&
        Domain == other.Domain &&
        WriteExecutorReady == other.WriteExecutorReady &&
        ReadRuntimeReady == other.ReadRuntimeReady &&
        SyncRuntimeOwnerReady == other.SyncRuntimeOwnerReady &&
        ApplyRuntimeReady == other.ApplyRuntimeReady &&
        AudioRuntimeReady == other.AudioRuntimeReady &&
        LegacyFallbackReady == other.LegacyFallbackReady &&
        SwitchBackProofReady == other.SwitchBackProofReady &&
        DiagnosticsRedacted == other.DiagnosticsRedacted &&
        TestsPass == other.TestsPass &&
        DocsUpdated == other.DocsUpdated &&
        RealDeviceEvidencePresent == other.RealDeviceEvidencePresent &&
        CodeComplete == other.CodeComplete;
    public override int GetHashCode() => HashCode.Combine(Domain, WriteExecutorReady, ReadRuntimeReady,
        SyncRuntimeOwnerReady, ApplyRuntimeReady, AudioRuntimeReady, LegacyFallbackReady, SwitchBackProofReady,
        DiagnosticsRedacted, TestsPass, DocsUpdated, RealDeviceEvidencePresent, CodeComplete);
    public static bool operator ==(CanonicalSyncKernelCompletionDomainReadiness l, CanonicalSyncKernelCompletionDomainReadiness r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelCompletionDomainReadiness l, CanonicalSyncKernelCompletionDomainReadiness r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalSyncKernelReadyToRetireDomain
{
    recordingMetadata,
    libraryMetadata,
    generatedArtifacts,
    tombstoneConflict,
    audioUpload,
    [JsonStringEnumMemberName("recordingExistence/sync engine")]
    recordingExistenceSyncEngine
}

public sealed class CanonicalSyncKernelDomainReadyToRetireReadiness : IEquatable<CanonicalSyncKernelDomainReadyToRetireReadiness>
{
    public CanonicalSyncKernelReadyToRetireDomain Domain { get; }
    public bool WriteExecutorReady { get; }
    public bool ReadCutoverReady { get; }
    public bool CanonicalRuntimeOwnerReady { get; }
    public bool LegacyFallbackReady { get; }
    public bool SwitchBackProven { get; }
    public bool DiagnosticsClean { get; }
    public bool RealDeviceEvidencePresent { get; }
    public bool ReadyToRetireLegacy { get; }
    public bool RetirementExecutionPerformed { get; }
    public List<CanonicalSyncKernelCompletionBlocker> Blockers { get; }
    public string DiagnosticsSummary { get; }

    public bool CodeReady =>
        WriteExecutorReady
        && ReadCutoverReady
        && CanonicalRuntimeOwnerReady
        && LegacyFallbackReady
        && SwitchBackProven
        && DiagnosticsClean
        && !RetirementExecutionPerformed
        && !Blockers.Contains(CanonicalSyncKernelCompletionBlocker.retirementExecutionAttempted)
        && !Blockers.Contains(CanonicalSyncKernelCompletionBlocker.legacyDeletionAttempted);

    public CanonicalSyncKernelDomainReadyToRetireReadiness(
        CanonicalSyncKernelReadyToRetireDomain domain,
        bool writeExecutorReady = true,
        bool readCutoverReady = true,
        bool canonicalRuntimeOwnerReady = true,
        bool legacyFallbackReady = true,
        bool switchBackProven = true,
        bool diagnosticsClean = true,
        bool realDeviceEvidencePresent = false,
        bool? readyToRetireLegacy = null,
        bool retirementExecutionPerformed = false,
        List<CanonicalSyncKernelCompletionBlocker>? blockers = null)
    {
        var resolvedBlockers = blockers ?? new List<CanonicalSyncKernelCompletionBlocker>();
        if (!writeExecutorReady || !readCutoverReady || !canonicalRuntimeOwnerReady)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.domainIncomplete);
        if (!legacyFallbackReady)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.legacyFallbackUnavailable);
        if (!switchBackProven)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.switchBackProofMissing);
        if (!diagnosticsClean)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.diagnosticsNotRedacted);
        if (!realDeviceEvidencePresent)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.realDeviceEvidenceMissing);
        if (retirementExecutionPerformed)
            resolvedBlockers.Add(CanonicalSyncKernelCompletionBlocker.retirementExecutionAttempted);
        resolvedBlockers = Unique(resolvedBlockers);

        var codeReady = writeExecutorReady
            && readCutoverReady
            && canonicalRuntimeOwnerReady
            && legacyFallbackReady
            && switchBackProven
            && diagnosticsClean
            && !retirementExecutionPerformed;
        var computedReady = codeReady && realDeviceEvidencePresent;

        Domain = domain;
        WriteExecutorReady = writeExecutorReady;
        ReadCutoverReady = readCutoverReady;
        CanonicalRuntimeOwnerReady = canonicalRuntimeOwnerReady;
        LegacyFallbackReady = legacyFallbackReady;
        SwitchBackProven = switchBackProven;
        DiagnosticsClean = diagnosticsClean;
        RealDeviceEvidencePresent = realDeviceEvidencePresent;
        ReadyToRetireLegacy = retirementExecutionPerformed ? false : (readyToRetireLegacy ?? computedReady);
        RetirementExecutionPerformed = retirementExecutionPerformed;
        Blockers = resolvedBlockers;
        DiagnosticsSummary = string.Join(",",
            "canonicalSyncKernelDomainReadyToRetire=v8.45",
            $"domain={domain}",
            $"writeExecutorReady={writeExecutorReady}",
            $"readCutoverReady={readCutoverReady}",
            $"canonicalRuntimeOwnerReady={canonicalRuntimeOwnerReady}",
            $"legacyFallbackReady={legacyFallbackReady}",
            $"switchBackProven={switchBackProven}",
            $"diagnosticsClean={diagnosticsClean}",
            $"realDeviceEvidencePresent={realDeviceEvidencePresent}",
            $"readyToRetireLegacy={ReadyToRetireLegacy}",
            "retirementExecutionPerformed=false",
            "legacyDeleted=false",
            "legacyDisabled=false",
            $"blockers={string.Join("|", resolvedBlockers.Select(b => b.ToString()))}",
            "redacted=true"
        );
    }

    private static List<CanonicalSyncKernelCompletionBlocker> Unique(List<CanonicalSyncKernelCompletionBlocker> blockers)
    {
        var seen = new HashSet<CanonicalSyncKernelCompletionBlocker>();
        var unique = new List<CanonicalSyncKernelCompletionBlocker>();
        foreach (var b in blockers)
        {
            if (!seen.Contains(b))
            {
                seen.Add(b);
                unique.Add(b);
            }
        }
        return unique;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelDomainReadyToRetireReadiness other && Equals(other);
    public bool Equals(CanonicalSyncKernelDomainReadyToRetireReadiness? other) =>
        other is not null &&
        Domain == other.Domain &&
        WriteExecutorReady == other.WriteExecutorReady &&
        ReadCutoverReady == other.ReadCutoverReady &&
        CanonicalRuntimeOwnerReady == other.CanonicalRuntimeOwnerReady &&
        LegacyFallbackReady == other.LegacyFallbackReady &&
        SwitchBackProven == other.SwitchBackProven &&
        DiagnosticsClean == other.DiagnosticsClean &&
        RealDeviceEvidencePresent == other.RealDeviceEvidencePresent &&
        ReadyToRetireLegacy == other.ReadyToRetireLegacy &&
        RetirementExecutionPerformed == other.RetirementExecutionPerformed;
    public override int GetHashCode() => HashCode.Combine(Domain, WriteExecutorReady, ReadCutoverReady,
        CanonicalRuntimeOwnerReady, LegacyFallbackReady, SwitchBackProven, DiagnosticsClean,
        RealDeviceEvidencePresent, ReadyToRetireLegacy, RetirementExecutionPerformed);
    public static bool operator ==(CanonicalSyncKernelDomainReadyToRetireReadiness l, CanonicalSyncKernelDomainReadyToRetireReadiness r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelDomainReadyToRetireReadiness l, CanonicalSyncKernelDomainReadyToRetireReadiness r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelDomainReadyToRetireReport : IEquatable<CanonicalSyncKernelDomainReadyToRetireReport>
{
    public List<CanonicalSyncKernelDomainReadyToRetireReadiness> Domains { get; }
    public bool RetirementExecutionPerformed { get; }
    public bool LegacyDeleted { get; }
    public bool LegacyDisabled { get; }
    public string DiagnosticsSummary { get; }

    public bool CodeReady =>
        Domains.Count == Enum.GetValues<CanonicalSyncKernelReadyToRetireDomain>().Length
        && Domains.All(d => d.CodeReady)
        && !RetirementExecutionPerformed
        && !LegacyDeleted
        && !LegacyDisabled;

    public bool AllReadyToRetireLegacyReportOnly =>
        CodeReady && Domains.All(d => d.ReadyToRetireLegacy);

    public bool AllRealDeviceEvidencePresent =>
        Domains.All(d => d.RealDeviceEvidencePresent);

    public List<CanonicalSyncKernelCompletionBlocker> Blockers
    {
        get
        {
            var blockers = new List<CanonicalSyncKernelCompletionBlocker>(Domains.SelectMany(d => d.Blockers));
            if (RetirementExecutionPerformed)
                blockers.Add(CanonicalSyncKernelCompletionBlocker.retirementExecutionAttempted);
            if (LegacyDeleted || LegacyDisabled)
                blockers.Add(CanonicalSyncKernelCompletionBlocker.legacyDeletionAttempted);
            return Unique(blockers);
        }
    }

    public CanonicalSyncKernelDomainReadyToRetireReport(
        List<CanonicalSyncKernelDomainReadyToRetireReadiness> domains,
        bool retirementExecutionPerformed = false,
        bool legacyDeleted = false,
        bool legacyDisabled = false)
    {
        Domains = domains.OrderBy(d => d.Domain.ToString()).ToList();
        RetirementExecutionPerformed = retirementExecutionPerformed;
        LegacyDeleted = legacyDeleted;
        LegacyDisabled = legacyDisabled;
        DiagnosticsSummary = string.Join(",",
            "canonicalSyncKernelReadyToRetireReport=v8.45",
            $"domains={string.Join("|", Domains.Select(d => d.Domain.ToString()))}",
            $"codeReady={Domains.All(d => d.CodeReady)}",
            $"realDeviceEvidencePresent={Domains.All(d => d.RealDeviceEvidencePresent)}",
            $"readyToRetireLegacyReportOnly={Domains.All(d => d.ReadyToRetireLegacy)}",
            "retirementExecutionPerformed=false",
            "legacyDeleted=false",
            "legacyDisabled=false",
            "redacted=true"
        );
    }

    public static CanonicalSyncKernelDomainReadyToRetireReport V845CodeCompleteAwaitingDeviceEvidence()
    {
        return new CanonicalSyncKernelDomainReadyToRetireReport(
            Enum.GetValues<CanonicalSyncKernelReadyToRetireDomain>()
                .Select(d => new CanonicalSyncKernelDomainReadyToRetireReadiness(d)).ToList()
        );
    }

    public static CanonicalSyncKernelDomainReadyToRetireReport V845ReadyWithDeviceEvidence()
    {
        return new CanonicalSyncKernelDomainReadyToRetireReport(
            Enum.GetValues<CanonicalSyncKernelReadyToRetireDomain>()
                .Select(d => new CanonicalSyncKernelDomainReadyToRetireReadiness(d, realDeviceEvidencePresent: true)).ToList()
        );
    }

    private static List<CanonicalSyncKernelCompletionBlocker> Unique(List<CanonicalSyncKernelCompletionBlocker> blockers)
    {
        var seen = new HashSet<CanonicalSyncKernelCompletionBlocker>();
        var unique = new List<CanonicalSyncKernelCompletionBlocker>();
        foreach (var b in blockers)
        {
            if (!seen.Contains(b))
            {
                seen.Add(b);
                unique.Add(b);
            }
        }
        return unique;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelDomainReadyToRetireReport other && Equals(other);
    public bool Equals(CanonicalSyncKernelDomainReadyToRetireReport? other) =>
        other is not null &&
        Domains.SequenceEqual(other.Domains) &&
        RetirementExecutionPerformed == other.RetirementExecutionPerformed &&
        LegacyDeleted == other.LegacyDeleted &&
        LegacyDisabled == other.LegacyDisabled;
    public override int GetHashCode() => HashCode.Combine(
        string.Join(",", Domains.Select(d => d.Domain)),
        RetirementExecutionPerformed, LegacyDeleted, LegacyDisabled);
    public static bool operator ==(CanonicalSyncKernelDomainReadyToRetireReport l, CanonicalSyncKernelDomainReadyToRetireReport r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelDomainReadyToRetireReport l, CanonicalSyncKernelDomainReadyToRetireReport r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelCompletionScorecard : IEquatable<CanonicalSyncKernelCompletionScorecard>
{
    public List<CanonicalSyncKernelCompletionScorecardItemResult> ItemResults { get; }
    public List<CanonicalSyncKernelCompletionDomainReadiness> DomainCompletionReadiness { get; }
    public CanonicalSyncKernelDomainReadyToRetireReport DomainReadinessReport { get; }
    public List<CanonicalSyncKernelCompletionBlocker> UnresolvedBlockers { get; }
    public CanonicalSyncKernelCompletionStatus Status { get; }
    public List<CanonicalSyncKernelCompletionBlocker> Blockers { get; }
    public string DiagnosticsSummary { get; }

    public bool CodeComplete
    {
        get
        {
            var byItem = ItemResults.ToDictionary(r => r.Item, r => r.Complete);
            return Enum.GetValues<CanonicalSyncKernelCompletionScorecardItem>()
                .Where(i => i.IsCodeCompletionItem())
                .All(i => byItem.GetValueOrDefault(i, false))
                && DomainCompletionReadiness.Count == Enum.GetValues<CanonicalSyncKernelCompletionDomain>().Length
                && DomainCompletionReadiness.All(d => d.CodeComplete)
                && DomainReadinessReport.CodeReady
                && !Blockers.Contains(CanonicalSyncKernelCompletionBlocker.domainIncomplete)
                && !Blockers.Contains(CanonicalSyncKernelCompletionBlocker.diagnosticsNotRedacted)
                && !Blockers.Contains(CanonicalSyncKernelCompletionBlocker.legacyCompatibilityProofMissing)
                && !Blockers.Contains(CanonicalSyncKernelCompletionBlocker.switchBackProofMissing)
                && UnresolvedBlockers.Count == 0;
        }
    }

    public bool RealDeviceEvidencePresent =>
        ItemResults.FirstOrDefault(r => r.Item == CanonicalSyncKernelCompletionScorecardItem.realDeviceEvidenceRequired)?.Complete == true
        && DomainCompletionReadiness.All(d => d.RealDeviceEvidencePresent)
        && DomainReadinessReport.AllRealDeviceEvidencePresent;

    public CanonicalSyncKernelCompletionScorecard(
        List<CanonicalSyncKernelCompletionScorecardItemResult> itemResults,
        List<CanonicalSyncKernelCompletionDomainReadiness>? domainCompletionReadiness = null,
        CanonicalSyncKernelDomainReadyToRetireReport? domainReadinessReport = null,
        List<CanonicalSyncKernelCompletionBlocker>? unresolvedBlockers = null,
        bool retirementReportOnlyReady = false)
    {
        var completedByItem = itemResults.ToDictionary(r => r.Item, r => r.Complete);
        var normalizedItems = Enum.GetValues<CanonicalSyncKernelCompletionScorecardItem>()
            .Select(i => new CanonicalSyncKernelCompletionScorecardItemResult(i,
                completedByItem.GetValueOrDefault(i, false))).ToList();

        var resolvedDomainReadiness = domainCompletionReadiness
            ?? CanonicalSyncKernelCompletionDomainReadiness.V845CodeCompleteAwaitingDeviceEvidence();
        var resolvedDomainReport = domainReadinessReport
            ?? CanonicalSyncKernelDomainReadyToRetireReport.V845CodeCompleteAwaitingDeviceEvidence();
        var resolvedUnresolved = unresolvedBlockers ?? new List<CanonicalSyncKernelCompletionBlocker>();

        var blockers = new List<CanonicalSyncKernelCompletionBlocker>(resolvedUnresolved);
        foreach (var result in normalizedItems.Where(r => !r.Complete))
            blockers.Add(result.Item.ToBlocker());
        if (!resolvedDomainReport.CodeReady)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.domainIncomplete);
        if (resolvedDomainReadiness.Count != Enum.GetValues<CanonicalSyncKernelCompletionDomain>().Length
            || !resolvedDomainReadiness.All(d => d.CodeComplete))
            blockers.Add(CanonicalSyncKernelCompletionBlocker.domainIncomplete);
        blockers.AddRange(resolvedDomainReadiness.SelectMany(d => d.Blockers)
            .Where(b => b != CanonicalSyncKernelCompletionBlocker.realDeviceEvidenceMissing));
        blockers.AddRange(resolvedDomainReport.Blockers
            .Where(b => b != CanonicalSyncKernelCompletionBlocker.realDeviceEvidenceMissing));
        blockers = Unique(blockers);

        var codeItemsComplete = normalizedItems
            .Where(r => r.Item.IsCodeCompletionItem()).All(r => r.Complete);
        var domainsCodeComplete = resolvedDomainReadiness.Count == Enum.GetValues<CanonicalSyncKernelCompletionDomain>().Length
            && resolvedDomainReadiness.All(d => d.CodeComplete);
        var deviceEvidencePresent = normalizedItems
            .FirstOrDefault(r => r.Item == CanonicalSyncKernelCompletionScorecardItem.realDeviceEvidenceRequired)?.Complete == true
            && resolvedDomainReadiness.All(d => d.RealDeviceEvidencePresent)
            && resolvedDomainReport.AllRealDeviceEvidencePresent;

        CanonicalSyncKernelCompletionStatus status;
        if (blockers.Contains(CanonicalSyncKernelCompletionBlocker.legacyDeletionAttempted)
            || blockers.Contains(CanonicalSyncKernelCompletionBlocker.sensitiveEvidenceLeak)
            || blockers.Contains(CanonicalSyncKernelCompletionBlocker.unsafeCanonicalDefault)
            || blockers.Contains(CanonicalSyncKernelCompletionBlocker.releaseDefaultCanonical)
            || blockers.Contains(CanonicalSyncKernelCompletionBlocker.securityBypassDetected))
            status = CanonicalSyncKernelCompletionStatus.@unsafe;
        else if (blockers.Contains(CanonicalSyncKernelCompletionBlocker.diagnosticsNotRedacted)
            || blockers.Contains(CanonicalSyncKernelCompletionBlocker.retirementExecutionAttempted)
            || blockers.Contains(CanonicalSyncKernelCompletionBlocker.legacyDeletionAttempted)
            || blockers.Contains(CanonicalSyncKernelCompletionBlocker.sensitiveEvidenceLeak)
            || resolvedUnresolved.Count != 0)
            status = CanonicalSyncKernelCompletionStatus.blocked;
        else if (!codeItemsComplete || !domainsCodeComplete || !resolvedDomainReport.CodeReady)
            status = CanonicalSyncKernelCompletionStatus.incomplete;
        else if (!deviceEvidencePresent)
            status = CanonicalSyncKernelCompletionStatus.codeCompleteNeedsDeviceEvidence;
        else if (retirementReportOnlyReady || resolvedDomainReport.AllReadyToRetireLegacyReportOnly)
            status = CanonicalSyncKernelCompletionStatus.readyToRetireLegacyReportOnly;
        else
            status = CanonicalSyncKernelCompletionStatus.readyForManualSwitchTrial;

        ItemResults = normalizedItems;
        DomainCompletionReadiness = resolvedDomainReadiness.OrderBy(d => d.Domain.ToString()).ToList();
        DomainReadinessReport = resolvedDomainReport;
        UnresolvedBlockers = resolvedUnresolved;
        Status = status;
        Blockers = status == CanonicalSyncKernelCompletionStatus.codeCompleteNeedsDeviceEvidence
            ? Unique(new List<CanonicalSyncKernelCompletionBlocker>(blockers)
            { CanonicalSyncKernelCompletionBlocker.realDeviceEvidenceMissing })
            : blockers;
        DiagnosticsSummary = string.Join(",",
            "canonicalSyncKernelCompletionScorecard=v8.45",
            $"status={status}",
            $"codeComplete={codeItemsComplete && domainsCodeComplete && resolvedDomainReport.CodeReady}",
            $"realDeviceEvidencePresent={deviceEvidencePresent}",
            $"domainCompletionReady={domainsCodeComplete}",
            $"domainCodeReady={resolvedDomainReport.CodeReady}",
            "retirementExecutionPerformed=false",
            "legacyDeleted=false",
            "legacyDisabled=false",
            $"blockers={string.Join("|", Blockers.Select(b => b.ToString()))}",
            "redacted=true"
        );
    }

    public static CanonicalSyncKernelCompletionScorecard V845(
        bool inventoryRuntimeComplete = true,
        bool diffLWWRuntimeComplete = true,
        bool existenceTruthComplete = true,
        bool nonAudioApplyRuntimeComplete = true,
        bool audioUploadRuntimeComplete = true,
        bool readRuntimeComplete = true,
        bool masterSwitchComplete = true,
        bool legacyCompatibilityProofComplete = true,
        bool switchBackProofComplete = true,
        bool diagnosticsRedacted = true,
        bool realDeviceEvidencePresent = false,
        List<CanonicalSyncKernelCompletionDomainReadiness>? domainCompletionReadiness = null,
        CanonicalSyncKernelDomainReadyToRetireReport? domainReadinessReport = null,
        List<CanonicalSyncKernelCompletionBlocker>? unresolvedBlockers = null)
    {
        var resolvedDomainReadiness = domainCompletionReadiness
            ?? (realDeviceEvidencePresent
                ? CanonicalSyncKernelCompletionDomainReadiness.V845ReadyWithDeviceEvidence()
                : CanonicalSyncKernelCompletionDomainReadiness.V845CodeCompleteAwaitingDeviceEvidence());
        var resolvedDomainReport = domainReadinessReport
            ?? (realDeviceEvidencePresent
                ? CanonicalSyncKernelDomainReadyToRetireReport.V845ReadyWithDeviceEvidence()
                : CanonicalSyncKernelDomainReadyToRetireReport.V845CodeCompleteAwaitingDeviceEvidence());

        return new CanonicalSyncKernelCompletionScorecard(
            itemResults: new List<CanonicalSyncKernelCompletionScorecardItemResult>
            {
                new(CanonicalSyncKernelCompletionScorecardItem.inventoryRuntimeComplete, inventoryRuntimeComplete),
                new(CanonicalSyncKernelCompletionScorecardItem.diffLWWRuntimeComplete, diffLWWRuntimeComplete),
                new(CanonicalSyncKernelCompletionScorecardItem.existenceTruthComplete, existenceTruthComplete),
                new(CanonicalSyncKernelCompletionScorecardItem.nonAudioApplyRuntimeComplete, nonAudioApplyRuntimeComplete),
                new(CanonicalSyncKernelCompletionScorecardItem.audioUploadRuntimeComplete, audioUploadRuntimeComplete),
                new(CanonicalSyncKernelCompletionScorecardItem.readRuntimeComplete, readRuntimeComplete),
                new(CanonicalSyncKernelCompletionScorecardItem.masterSwitchComplete, masterSwitchComplete),
                new(CanonicalSyncKernelCompletionScorecardItem.legacyCompatibilityProofComplete, legacyCompatibilityProofComplete),
                new(CanonicalSyncKernelCompletionScorecardItem.switchBackProofComplete, switchBackProofComplete),
                new(CanonicalSyncKernelCompletionScorecardItem.diagnosticsRedacted, diagnosticsRedacted),
                new(CanonicalSyncKernelCompletionScorecardItem.realDeviceEvidenceRequired, realDeviceEvidencePresent)
            },
            domainCompletionReadiness: resolvedDomainReadiness,
            domainReadinessReport: resolvedDomainReport,
            unresolvedBlockers: unresolvedBlockers
        );
    }

    private static List<CanonicalSyncKernelCompletionBlocker> Unique(List<CanonicalSyncKernelCompletionBlocker> blockers)
    {
        var seen = new HashSet<CanonicalSyncKernelCompletionBlocker>();
        var unique = new List<CanonicalSyncKernelCompletionBlocker>();
        foreach (var b in blockers)
        {
            if (!seen.Contains(b))
            {
                seen.Add(b);
                unique.Add(b);
            }
        }
        return unique;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelCompletionScorecard other && Equals(other);
    public bool Equals(CanonicalSyncKernelCompletionScorecard? other) =>
        other is not null &&
        Status == other.Status &&
        DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => HashCode.Combine(Status, DiagnosticsSummary);
    public static bool operator ==(CanonicalSyncKernelCompletionScorecard l, CanonicalSyncKernelCompletionScorecard r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelCompletionScorecard l, CanonicalSyncKernelCompletionScorecard r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelEvidenceModeTransition : IEquatable<CanonicalSyncKernelEvidenceModeTransition>
{
    public CanonicalKernelSwitchMode FromMode { get; }
    public CanonicalKernelSwitchMode ToMode { get; }
    public string Phase { get; }

    public CanonicalSyncKernelEvidenceModeTransition(
        CanonicalKernelSwitchMode fromMode,
        CanonicalKernelSwitchMode toMode,
        string phase)
    {
        FromMode = fromMode;
        ToMode = toMode;
        Phase = CanonicalSyncKernelEvidenceRedactor.Redact(phase);
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelEvidenceModeTransition other && Equals(other);
    public bool Equals(CanonicalSyncKernelEvidenceModeTransition? other) =>
        other is not null && FromMode == other.FromMode && ToMode == other.ToMode && Phase == other.Phase;
    public override int GetHashCode() => HashCode.Combine(FromMode, ToMode, Phase);
    public static bool operator ==(CanonicalSyncKernelEvidenceModeTransition l, CanonicalSyncKernelEvidenceModeTransition r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelEvidenceModeTransition l, CanonicalSyncKernelEvidenceModeTransition r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelEvidenceObjectCounts : IEquatable<CanonicalSyncKernelEvidenceObjectCounts>
{
    public int RecordingMetadataCount { get; set; }
    public int LibraryMetadataCount { get; set; }
    public int GeneratedArtifactCount { get; set; }
    public int TombstoneConflictCount { get; set; }
    public int AudioUploadCandidateCount { get; set; }
    public int RecordingExistenceCount { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelEvidenceObjectCounts other && Equals(other);
    public bool Equals(CanonicalSyncKernelEvidenceObjectCounts? other) =>
        other is not null &&
        RecordingMetadataCount == other.RecordingMetadataCount &&
        LibraryMetadataCount == other.LibraryMetadataCount &&
        GeneratedArtifactCount == other.GeneratedArtifactCount &&
        TombstoneConflictCount == other.TombstoneConflictCount &&
        AudioUploadCandidateCount == other.AudioUploadCandidateCount &&
        RecordingExistenceCount == other.RecordingExistenceCount;
    public override int GetHashCode() => HashCode.Combine(RecordingMetadataCount, LibraryMetadataCount,
        GeneratedArtifactCount, TombstoneConflictCount, AudioUploadCandidateCount, RecordingExistenceCount);
    public static bool operator ==(CanonicalSyncKernelEvidenceObjectCounts l, CanonicalSyncKernelEvidenceObjectCounts r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelEvidenceObjectCounts l, CanonicalSyncKernelEvidenceObjectCounts r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelEvidenceCacheCounts : IEquatable<CanonicalSyncKernelEvidenceCacheCounts>
{
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public int StaleCount { get; set; }
    public int ErrorCount { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelEvidenceCacheCounts other && Equals(other);
    public bool Equals(CanonicalSyncKernelEvidenceCacheCounts? other) =>
        other is not null &&
        HitCount == other.HitCount && MissCount == other.MissCount &&
        StaleCount == other.StaleCount && ErrorCount == other.ErrorCount;
    public override int GetHashCode() => HashCode.Combine(HitCount, MissCount, StaleCount, ErrorCount);
    public static bool operator ==(CanonicalSyncKernelEvidenceCacheCounts l, CanonicalSyncKernelEvidenceCacheCounts r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelEvidenceCacheCounts l, CanonicalSyncKernelEvidenceCacheCounts r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelEvidencePlanCounts : IEquatable<CanonicalSyncKernelEvidencePlanCounts>
{
    public int CanonicalPlanUsedCount { get; set; }
    public int LegacyFallbackCount { get; set; }
    public int BlockedPlanCount { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelEvidencePlanCounts other && Equals(other);
    public bool Equals(CanonicalSyncKernelEvidencePlanCounts? other) =>
        other is not null &&
        CanonicalPlanUsedCount == other.CanonicalPlanUsedCount &&
        LegacyFallbackCount == other.LegacyFallbackCount &&
        BlockedPlanCount == other.BlockedPlanCount;
    public override int GetHashCode() => HashCode.Combine(CanonicalPlanUsedCount, LegacyFallbackCount, BlockedPlanCount);
    public static bool operator ==(CanonicalSyncKernelEvidencePlanCounts l, CanonicalSyncKernelEvidencePlanCounts r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelEvidencePlanCounts l, CanonicalSyncKernelEvidencePlanCounts r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelEvidenceExecutionCounts : IEquatable<CanonicalSyncKernelEvidenceExecutionCounts>
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelEvidenceExecutionCounts other && Equals(other);
    public bool Equals(CanonicalSyncKernelEvidenceExecutionCounts? other) =>
        other is not null &&
        SuccessCount == other.SuccessCount && FailureCount == other.FailureCount;
    public override int GetHashCode() => HashCode.Combine(SuccessCount, FailureCount);
    public static bool operator ==(CanonicalSyncKernelEvidenceExecutionCounts l, CanonicalSyncKernelEvidenceExecutionCounts r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelEvidenceExecutionCounts l, CanonicalSyncKernelEvidenceExecutionCounts r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelEvidenceReadDivergence : IEquatable<CanonicalSyncKernelEvidenceReadDivergence>
{
    public int EquivalentCount { get; set; }
    public int DivergentCount { get; set; }
    public int PathOrContentLeakRiskCount { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelEvidenceReadDivergence other && Equals(other);
    public bool Equals(CanonicalSyncKernelEvidenceReadDivergence? other) =>
        other is not null &&
        EquivalentCount == other.EquivalentCount &&
        DivergentCount == other.DivergentCount &&
        PathOrContentLeakRiskCount == other.PathOrContentLeakRiskCount;
    public override int GetHashCode() => HashCode.Combine(EquivalentCount, DivergentCount, PathOrContentLeakRiskCount);
    public static bool operator ==(CanonicalSyncKernelEvidenceReadDivergence l, CanonicalSyncKernelEvidenceReadDivergence r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelEvidenceReadDivergence l, CanonicalSyncKernelEvidenceReadDivergence r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelEvidenceRedactionProof : IEquatable<CanonicalSyncKernelEvidenceRedactionProof>
{
    public bool Redacted { get; }
    public bool SensitiveInputDetected { get; }
    public bool SensitiveOutputDetected { get; }
    public List<string> ExcludedSensitivePayloads { get; }

    public CanonicalSyncKernelEvidenceRedactionProof(
        bool redacted,
        bool sensitiveInputDetected,
        bool sensitiveOutputDetected,
        List<string> excludedSensitivePayloads)
    {
        Redacted = redacted;
        SensitiveInputDetected = sensitiveInputDetected;
        SensitiveOutputDetected = sensitiveOutputDetected;
        ExcludedSensitivePayloads = excludedSensitivePayloads;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelEvidenceRedactionProof other && Equals(other);
    public bool Equals(CanonicalSyncKernelEvidenceRedactionProof? other) =>
        other is not null &&
        Redacted == other.Redacted &&
        SensitiveInputDetected == other.SensitiveInputDetected &&
        SensitiveOutputDetected == other.SensitiveOutputDetected &&
        ExcludedSensitivePayloads.SequenceEqual(other.ExcludedSensitivePayloads);
    public override int GetHashCode() => HashCode.Combine(Redacted, SensitiveInputDetected, SensitiveOutputDetected);
    public static bool operator ==(CanonicalSyncKernelEvidenceRedactionProof l, CanonicalSyncKernelEvidenceRedactionProof r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelEvidenceRedactionProof l, CanonicalSyncKernelEvidenceRedactionProof r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelEvidencePackage : IEquatable<CanonicalSyncKernelEvidencePackage>
{
    public List<CanonicalSyncKernelEvidenceModeTransition> ModeTransitions { get; }
    public CanonicalSyncKernelEvidenceObjectCounts ObjectCounts { get; }
    public CanonicalSyncKernelEvidenceCacheCounts CacheCounts { get; }
    public CanonicalSyncKernelEvidencePlanCounts PlanCounts { get; }
    public CanonicalSyncKernelEvidenceExecutionCounts ApplyCounts { get; }
    public CanonicalSyncKernelEvidenceExecutionCounts UploadCounts { get; }
    public CanonicalSyncKernelEvidenceReadDivergence ReadDivergence { get; }
    public string SwitchBackProofSummary { get; }
    public CanonicalSyncKernelEvidenceRedactionProof RedactionProof { get; }
    public List<string> RedactedDiagnostics { get; }
    public bool Redacted { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalSyncKernelEvidencePackage(
        List<CanonicalSyncKernelEvidenceModeTransition> modeTransitions,
        CanonicalSyncKernelEvidenceObjectCounts objectCounts,
        CanonicalSyncKernelEvidenceCacheCounts cacheCounts,
        CanonicalSyncKernelEvidencePlanCounts planCounts,
        CanonicalSyncKernelEvidenceExecutionCounts applyCounts,
        CanonicalSyncKernelEvidenceExecutionCounts uploadCounts,
        CanonicalSyncKernelEvidenceReadDivergence readDivergence,
        string switchBackProofSummary,
        CanonicalSyncKernelEvidenceRedactionProof redactionProof,
        List<string> redactedDiagnostics,
        bool redacted,
        string diagnosticsSummary)
    {
        ModeTransitions = modeTransitions;
        ObjectCounts = objectCounts;
        CacheCounts = cacheCounts;
        PlanCounts = planCounts;
        ApplyCounts = applyCounts;
        UploadCounts = uploadCounts;
        ReadDivergence = readDivergence;
        SwitchBackProofSummary = switchBackProofSummary;
        RedactionProof = redactionProof;
        RedactedDiagnostics = redactedDiagnostics;
        Redacted = redacted;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelEvidencePackage other && Equals(other);
    public bool Equals(CanonicalSyncKernelEvidencePackage? other) =>
        other is not null && DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => DiagnosticsSummary.GetHashCode();
    public static bool operator ==(CanonicalSyncKernelEvidencePackage l, CanonicalSyncKernelEvidencePackage r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelEvidencePackage l, CanonicalSyncKernelEvidencePackage r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelEvidenceExportInput : IEquatable<CanonicalSyncKernelEvidenceExportInput>
{
    public List<CanonicalSyncKernelEvidenceModeTransition> ModeTransitions { get; }
    public CanonicalSyncKernelEvidenceObjectCounts ObjectCounts { get; }
    public CanonicalSyncKernelEvidenceCacheCounts CacheCounts { get; }
    public CanonicalSyncKernelEvidencePlanCounts PlanCounts { get; }
    public CanonicalSyncKernelEvidenceExecutionCounts ApplyCounts { get; }
    public CanonicalSyncKernelEvidenceExecutionCounts UploadCounts { get; }
    public CanonicalSyncKernelEvidenceReadDivergence ReadDivergence { get; }
    public CanonicalLegacySwitchBackProofResult? SwitchBackProof { get; }
    public List<string> RawDiagnosticLines { get; }

    public CanonicalSyncKernelEvidenceExportInput(
        List<CanonicalSyncKernelEvidenceModeTransition>? modeTransitions = null,
        CanonicalSyncKernelEvidenceObjectCounts? objectCounts = null,
        CanonicalSyncKernelEvidenceCacheCounts? cacheCounts = null,
        CanonicalSyncKernelEvidencePlanCounts? planCounts = null,
        CanonicalSyncKernelEvidenceExecutionCounts? applyCounts = null,
        CanonicalSyncKernelEvidenceExecutionCounts? uploadCounts = null,
        CanonicalSyncKernelEvidenceReadDivergence? readDivergence = null,
        CanonicalLegacySwitchBackProofResult? switchBackProof = null,
        List<string>? rawDiagnosticLines = null)
    {
        ModeTransitions = modeTransitions ?? new List<CanonicalSyncKernelEvidenceModeTransition>();
        ObjectCounts = objectCounts ?? new CanonicalSyncKernelEvidenceObjectCounts();
        CacheCounts = cacheCounts ?? new CanonicalSyncKernelEvidenceCacheCounts();
        PlanCounts = planCounts ?? new CanonicalSyncKernelEvidencePlanCounts();
        ApplyCounts = applyCounts ?? new CanonicalSyncKernelEvidenceExecutionCounts();
        UploadCounts = uploadCounts ?? new CanonicalSyncKernelEvidenceExecutionCounts();
        ReadDivergence = readDivergence ?? new CanonicalSyncKernelEvidenceReadDivergence();
        SwitchBackProof = switchBackProof;
        RawDiagnosticLines = rawDiagnosticLines ?? new List<string>();
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelEvidenceExportInput other && Equals(other);
    public bool Equals(CanonicalSyncKernelEvidenceExportInput? other) =>
        other is not null &&
        ModeTransitions.SequenceEqual(other.ModeTransitions) &&
        ObjectCounts.Equals(other.ObjectCounts) &&
        CacheCounts.Equals(other.CacheCounts) &&
        PlanCounts.Equals(other.PlanCounts) &&
        ApplyCounts.Equals(other.ApplyCounts) &&
        UploadCounts.Equals(other.UploadCounts) &&
        ReadDivergence.Equals(other.ReadDivergence) &&
        Equals(SwitchBackProof, other.SwitchBackProof);
    public override int GetHashCode() => HashCode.Combine(ObjectCounts, CacheCounts, PlanCounts);
    public static bool operator ==(CanonicalSyncKernelEvidenceExportInput l, CanonicalSyncKernelEvidenceExportInput r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelEvidenceExportInput l, CanonicalSyncKernelEvidenceExportInput r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelEvidenceExporter
{
    public CanonicalSyncKernelEvidencePackage Export(CanonicalSyncKernelEvidenceExportInput input)
    {
        var redactedLines = input.RawDiagnosticLines
            .Select(CanonicalSyncKernelEvidenceRedactor.Redact).ToList();
        var sensitiveInputDetected = input.RawDiagnosticLines
            .Any(l => CanonicalSyncKernelEvidenceRedactor.ContainsSensitiveSignal(l));
        var sensitiveOutputDetected = redactedLines
            .Any(l => CanonicalSyncKernelEvidenceRedactor.ContainsSensitiveSignal(l));

        var redactionProof = new CanonicalSyncKernelEvidenceRedactionProof(
            redacted: !sensitiveOutputDetected,
            sensitiveInputDetected: sensitiveInputDetected,
            sensitiveOutputDetected: sensitiveOutputDetected,
            excludedSensitivePayloads: new List<string>
            {
                "absolutePaths", "fullHashes", "secrets", "fingerprints",
                "requestResponseBodies", "transcriptNoteSummaryProviderContent", "audioBytes"
            }
        );

        var switchBackSummary = CanonicalSyncKernelEvidenceRedactor.Redact(
            input.SwitchBackProof?.DiagnosticsSummary ?? "switchBackProof=missing");

        var diagnosticsSummary = string.Join(",",
            "canonicalSyncKernelEvidencePackage=v8.45",
            $"modeTransitions={input.ModeTransitions.Count}",
            $"recordingMetadataCount={input.ObjectCounts.RecordingMetadataCount}",
            $"cacheHitCount={input.CacheCounts.HitCount}",
            $"canonicalPlanUsedCount={input.PlanCounts.CanonicalPlanUsedCount}",
            $"legacyFallbackCount={input.PlanCounts.LegacyFallbackCount}",
            $"applySuccess={input.ApplyCounts.SuccessCount}",
            $"applyFailure={input.ApplyCounts.FailureCount}",
            $"uploadSuccess={input.UploadCounts.SuccessCount}",
            $"uploadFailure={input.UploadCounts.FailureCount}",
            $"readDivergent={input.ReadDivergence.DivergentCount}",
            $"switchBackProven={input.SwitchBackProof?.IsProven == true}",
            $"redacted={!sensitiveOutputDetected}"
        );

        return new CanonicalSyncKernelEvidencePackage(
            modeTransitions: input.ModeTransitions,
            objectCounts: input.ObjectCounts,
            cacheCounts: input.CacheCounts,
            planCounts: input.PlanCounts,
            applyCounts: input.ApplyCounts,
            uploadCounts: input.UploadCounts,
            readDivergence: input.ReadDivergence,
            switchBackProofSummary: switchBackSummary,
            redactionProof: redactionProof,
            redactedDiagnostics: redactedLines,
            redacted: redactionProof.Redacted,
            diagnosticsSummary: diagnosticsSummary
        );
    }
}

public static class CanonicalSyncKernelEvidenceRedactor
{
    public static string Redact(string value)
    {
        if (ContainsSensitiveSignal(value))
            return $"redacted-{CanonicalProductionRedaction.HashPrefix(CanonicalHash.Sha256String(value).Value) ?? "diagnostic"}";
        return CanonicalProductionRedaction.SafeDiagnosticText(value) ?? "redacted";
    }

    public static bool ContainsSensitiveSignal(string value)
    {
        var lowered = value.ToLowerInvariant();
        if (CanonicalProductionRedaction.ContainsSensitivePathSignal(value))
            return true;

        var sensitiveTokens = new[]
        {
            "secret", "token=", "api_key", "apikey", "password",
            "privatekey", "private key", "requestbody", "responsebody",
            "fulltranscript", "fullnote", "fullsummary", "providerresponse",
            "audio bytes", "-----begin"
        };
        foreach (var token in sensitiveTokens)
        {
            if (lowered.Contains(token))
                return true;
        }
        return ContainsLongHexToken(value);
    }

    private static bool ContainsLongHexToken(string value)
    {
        var separators = value.Where(c => !char.IsLetterOrDigit(c)).Distinct().ToArray();
        var tokens = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Any(token =>
            token.Length > 16 &&
            token.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
        );
    }
}

public sealed class CanonicalSyncKernelManualSwitchGateContext : IEquatable<CanonicalSyncKernelManualSwitchGateContext>
{
    public CanonicalSyncKernelCompletionScorecard Scorecard { get; }
    public CanonicalLegacyCompatibilityMatrix CompatibilityProof { get; }
    public CanonicalLegacySwitchBackProofResult SwitchBackProof { get; }
    public bool RealisticRootSwitchBackProofReady { get; }
    public CanonicalKernelSwitchMode DefaultMode { get; }
    public CanonicalKernelSwitchMode ReleaseMode { get; }
    public bool AllDiagnosticsRedacted { get; }
    public bool LegacyFallbackAvailable { get; }
    public bool OwnerApproved { get; }
    public bool ManualBackupAcknowledged { get; }
    public List<CanonicalSyncKernelCompletionBlocker> UnresolvedBlockers { get; }

    public CanonicalSyncKernelManualSwitchGateContext(
        CanonicalSyncKernelCompletionScorecard scorecard,
        CanonicalLegacyCompatibilityMatrix? compatibilityProof = null,
        CanonicalLegacySwitchBackProofResult? switchBackProof = null,
        bool realisticRootSwitchBackProofReady = true,
        CanonicalKernelSwitchMode defaultMode = CanonicalKernelSwitchMode.oldKernel,
        CanonicalKernelSwitchMode releaseMode = CanonicalKernelSwitchMode.oldKernel,
        bool allDiagnosticsRedacted = true,
        bool legacyFallbackAvailable = true,
        bool ownerApproved = false,
        bool manualBackupAcknowledged = false,
        List<CanonicalSyncKernelCompletionBlocker>? unresolvedBlockers = null)
    {
        Scorecard = scorecard;
        CompatibilityProof = compatibilityProof ?? CanonicalLegacyCompatibilityMatrix.DefaultV844();
        SwitchBackProof = switchBackProof ?? throw new ArgumentNullException(nameof(switchBackProof));
        RealisticRootSwitchBackProofReady = realisticRootSwitchBackProofReady;
        DefaultMode = defaultMode;
        ReleaseMode = releaseMode;
        AllDiagnosticsRedacted = allDiagnosticsRedacted;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        OwnerApproved = ownerApproved;
        ManualBackupAcknowledged = manualBackupAcknowledged;
        UnresolvedBlockers = unresolvedBlockers ?? new List<CanonicalSyncKernelCompletionBlocker>();
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelManualSwitchGateContext other && Equals(other);
    public bool Equals(CanonicalSyncKernelManualSwitchGateContext? other) =>
        other is not null &&
        Scorecard.Equals(other.Scorecard) &&
        CompatibilityProof.Equals(other.CompatibilityProof) &&
        SwitchBackProof.Equals(other.SwitchBackProof) &&
        RealisticRootSwitchBackProofReady == other.RealisticRootSwitchBackProofReady &&
        DefaultMode == other.DefaultMode && ReleaseMode == other.ReleaseMode &&
        AllDiagnosticsRedacted == other.AllDiagnosticsRedacted &&
        LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        OwnerApproved == other.OwnerApproved &&
        ManualBackupAcknowledged == other.ManualBackupAcknowledged;
    public override int GetHashCode() => HashCode.Combine(Scorecard, CompatibilityProof, SwitchBackProof);
    public static bool operator ==(CanonicalSyncKernelManualSwitchGateContext l, CanonicalSyncKernelManualSwitchGateContext r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelManualSwitchGateContext l, CanonicalSyncKernelManualSwitchGateContext r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelManualSwitchGateResult : IEquatable<CanonicalSyncKernelManualSwitchGateResult>
{
    public bool AllowedForManualTrial { get; }
    public bool ReleaseDefaultAllowed { get; }
    public List<CanonicalSyncKernelCompletionBlocker> Blockers { get; }
    public CanonicalKernelSwitchMode AllowedMode { get; }
    public string DiagnosticsSummary { get; }

    public CanonicalSyncKernelManualSwitchGateResult(
        bool allowedForManualTrial,
        bool releaseDefaultAllowed,
        List<CanonicalSyncKernelCompletionBlocker> blockers,
        CanonicalKernelSwitchMode allowedMode,
        string diagnosticsSummary)
    {
        AllowedForManualTrial = allowedForManualTrial;
        ReleaseDefaultAllowed = releaseDefaultAllowed;
        Blockers = blockers;
        AllowedMode = allowedMode;
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalSyncKernelManualSwitchGateResult other && Equals(other);
    public bool Equals(CanonicalSyncKernelManualSwitchGateResult? other) =>
        other is not null &&
        AllowedForManualTrial == other.AllowedForManualTrial &&
        ReleaseDefaultAllowed == other.ReleaseDefaultAllowed &&
        AllowedMode == other.AllowedMode &&
        DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() => HashCode.Combine(AllowedForManualTrial, ReleaseDefaultAllowed, AllowedMode, DiagnosticsSummary);
    public static bool operator ==(CanonicalSyncKernelManualSwitchGateResult l, CanonicalSyncKernelManualSwitchGateResult r) => l.Equals(r);
    public static bool operator !=(CanonicalSyncKernelManualSwitchGateResult l, CanonicalSyncKernelManualSwitchGateResult r) => !l.Equals(r);
}

public sealed class CanonicalSyncKernelManualSwitchGate
{
    public CanonicalSyncKernelManualSwitchGateResult Evaluate(
        CanonicalSyncKernelManualSwitchGateContext context)
    {
        var blockers = new List<CanonicalSyncKernelCompletionBlocker>();
        if (!context.Scorecard.CodeComplete)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.domainIncomplete);
        if (!context.CompatibilityProof.IsFullyProven)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.compatibilityProofMissing);
        if (!context.SwitchBackProof.IsProven)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.switchBackProofMissing);
        if (!context.RealisticRootSwitchBackProofReady)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.realisticRootSwitchBackProofMissing);
        if (context.DefaultMode != CanonicalKernelSwitchMode.oldKernel)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.defaultOldKernelMissing);
        if (context.ReleaseMode != CanonicalKernelSwitchMode.oldKernel)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.releaseDefaultCanonical);
        if (!context.AllDiagnosticsRedacted)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.diagnosticsNotRedacted);
        if (!context.LegacyFallbackAvailable)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.legacyFallbackUnavailable);
        if (context.UnresolvedBlockers.Count != 0)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.unresolvedBlocker);
        if (!context.OwnerApproved)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.ownerApprovalMissing);
        if (!context.ManualBackupAcknowledged)
            blockers.Add(CanonicalSyncKernelCompletionBlocker.manualBackupAcknowledgementMissing);

        blockers = Unique(blockers);
        var allowed = blockers.Count == 0;

        return new CanonicalSyncKernelManualSwitchGateResult(
            allowedForManualTrial: allowed,
            releaseDefaultAllowed: false,
            blockers: blockers,
            allowedMode: allowed ? CanonicalKernelSwitchMode.canonicalFullSync : CanonicalKernelSwitchMode.blocked,
            diagnosticsSummary: string.Join(",",
                "canonicalSyncKernelManualSwitchGate=v8.45",
                $"allowedForManualTrial={allowed}",
                "releaseDefaultAllowed=false",
                $"allowedMode={(allowed ? CanonicalKernelSwitchMode.canonicalFullSync : CanonicalKernelSwitchMode.blocked)}",
                $"defaultMode={context.DefaultMode}",
                $"releaseMode={context.ReleaseMode}",
                $"realisticRootSwitchBackProofReady={context.RealisticRootSwitchBackProofReady}",
                $"manualBackupAcknowledged={context.ManualBackupAcknowledged}",
                $"ownerApproved={context.OwnerApproved}",
                $"blockers={string.Join("|", blockers.Select(b => b.ToString()))}",
                "redacted=true"
            )
        );
    }

    private static List<CanonicalSyncKernelCompletionBlocker> Unique(List<CanonicalSyncKernelCompletionBlocker> blockers)
    {
        var seen = new HashSet<CanonicalSyncKernelCompletionBlocker>();
        var unique = new List<CanonicalSyncKernelCompletionBlocker>();
        foreach (var b in blockers)
        {
            if (!seen.Contains(b))
            {
                seen.Add(b);
                unique.Add(b);
            }
        }
        return unique;
    }
}
