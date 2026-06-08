using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataObservationEventKind
{
    windowStarted,
    windowCompleted,
    canonicalCommitAttempted,
    canonicalCommitSucceeded,
    canonicalCommitFailed,
    rollbackAttempted,
    rollbackSucceeded,
    rollbackFailed,
    rollbackFatal,
    legacyFallbackUsed,
    duplicateLegacySuppressed,
    unresolvedConflictObserved,
    resourceMoveAttempted,
    canonicalReadCandidateBuilt,
    canonicalReadServed,
    legacyReadFallbackUsed,
    readSideParallelEquivalent,
    readSideParallelDivergent,
    readSideUnsupportedObject,
    readSidePathLeakRisk,
    unsafeSideEffectObserved,
    syncOrUploadTriggered,
    uiMutated,
    contentWritten,
    tombstoneDeleteAttempted,
}

public sealed class CanonicalLibraryMetadataObservationEvent : IEquatable<CanonicalLibraryMetadataObservationEvent>
{
    public CanonicalLibraryMetadataObservationEventKind Kind { get; set; }
    public int Count { get; set; }
    public string? SyncRunID { get; set; }
    public CanonicalSyncPlanTrigger Trigger { get; set; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; set; }
    public string? Reason { get; set; }

    public CanonicalLibraryMetadataObservationEvent(
        CanonicalLibraryMetadataObservationEventKind kind,
        int count = 1,
        string? syncRunID = null,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? reason = null)
    {
        Kind = kind;
        Count = Math.Max(0, count);
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataObservationEvent other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataObservationEvent? other) =>
        other is not null && Kind == other.Kind && Count == other.Count &&
        SyncRunID == other.SyncRunID && Trigger == other.Trigger &&
        NodeRole == other.NodeRole && Reason == other.Reason;
    public override int GetHashCode() => HashCode.Combine(Kind, Count, SyncRunID, Trigger, NodeRole, Reason);
    public static bool operator ==(CanonicalLibraryMetadataObservationEvent left, CanonicalLibraryMetadataObservationEvent right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataObservationEvent left, CanonicalLibraryMetadataObservationEvent right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataObservationPolicy : IEquatable<CanonicalLibraryMetadataObservationPolicy>
{
    public bool Enabled { get; set; }
    public bool ExplicitInternalTestConfiguration { get; set; }
    public int MinimumWriteCanonicalCommitCount { get; set; }
    public int MinimumReadCanonicalEvidenceCount { get; set; }
    public int MinimumTotalEventCount { get; set; }
    public bool RequireLegacyFallbackAvailable { get; set; }
    public bool LegacyFallbackAvailable { get; set; }
    public bool RequireOnlyLibraryMetadataActivePilot { get; set; }
    public bool AllowParallelReadOnlyEvidence { get; set; }
    public bool ManualAuditRequired { get; set; }
    public bool RecordDiagnostics { get; set; }
    public int MaxDiagnosticsEvents { get; set; }

    public CanonicalLibraryMetadataObservationPolicy(
        bool enabled = false,
        bool explicitInternalTestConfiguration = false,
        int minimumWriteCanonicalCommitCount = 1,
        int minimumReadCanonicalEvidenceCount = 1,
        int minimumTotalEventCount = 2,
        bool requireLegacyFallbackAvailable = true,
        bool legacyFallbackAvailable = true,
        bool requireOnlyLibraryMetadataActivePilot = true,
        bool allowParallelReadOnlyEvidence = true,
        bool manualAuditRequired = true,
        bool recordDiagnostics = true,
        int maxDiagnosticsEvents = 16)
    {
        Enabled = enabled;
        ExplicitInternalTestConfiguration = explicitInternalTestConfiguration;
        MinimumWriteCanonicalCommitCount = Math.Max(0, minimumWriteCanonicalCommitCount);
        MinimumReadCanonicalEvidenceCount = Math.Max(0, minimumReadCanonicalEvidenceCount);
        MinimumTotalEventCount = Math.Max(0, minimumTotalEventCount);
        RequireLegacyFallbackAvailable = requireLegacyFallbackAvailable;
        LegacyFallbackAvailable = legacyFallbackAvailable;
        RequireOnlyLibraryMetadataActivePilot = requireOnlyLibraryMetadataActivePilot;
        AllowParallelReadOnlyEvidence = allowParallelReadOnlyEvidence;
        ManualAuditRequired = manualAuditRequired;
        RecordDiagnostics = recordDiagnostics;
        MaxDiagnosticsEvents = Math.Max(0, maxDiagnosticsEvents);
    }

    public static readonly CanonicalLibraryMetadataObservationPolicy Disabled = new();

    public static CanonicalLibraryMetadataObservationPolicy ExplicitInternalTest(
        int minimumWriteCanonicalCommitCount = 1,
        int minimumReadCanonicalEvidenceCount = 1,
        int minimumTotalEventCount = 2,
        bool legacyFallbackAvailable = true) =>
        new(true, true, minimumWriteCanonicalCommitCount, minimumReadCanonicalEvidenceCount,
            minimumTotalEventCount, true, legacyFallbackAvailable, true, true, true, true);

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataObservationPolicy other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataObservationPolicy? other) =>
        other is not null && Enabled == other.Enabled &&
        ExplicitInternalTestConfiguration == other.ExplicitInternalTestConfiguration &&
        MinimumWriteCanonicalCommitCount == other.MinimumWriteCanonicalCommitCount &&
        MinimumReadCanonicalEvidenceCount == other.MinimumReadCanonicalEvidenceCount &&
        MinimumTotalEventCount == other.MinimumTotalEventCount &&
        RequireLegacyFallbackAvailable == other.RequireLegacyFallbackAvailable &&
        LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        RequireOnlyLibraryMetadataActivePilot == other.RequireOnlyLibraryMetadataActivePilot &&
        AllowParallelReadOnlyEvidence == other.AllowParallelReadOnlyEvidence &&
        ManualAuditRequired == other.ManualAuditRequired &&
        RecordDiagnostics == other.RecordDiagnostics &&
        MaxDiagnosticsEvents == other.MaxDiagnosticsEvents;
    public override int GetHashCode() =>
        HashCode.Combine(Enabled, ExplicitInternalTestConfiguration, MinimumWriteCanonicalCommitCount,
            MinimumReadCanonicalEvidenceCount, MinimumTotalEventCount, RequireLegacyFallbackAvailable,
            LegacyFallbackAvailable, RequireOnlyLibraryMetadataActivePilot, AllowParallelReadOnlyEvidence,
            ManualAuditRequired, RecordDiagnostics, MaxDiagnosticsEvents);
    public static bool operator ==(CanonicalLibraryMetadataObservationPolicy left, CanonicalLibraryMetadataObservationPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataObservationPolicy left, CanonicalLibraryMetadataObservationPolicy right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataObservationWindow : IEquatable<CanonicalLibraryMetadataObservationWindow>
{
    public string ObservationWindowID { get; set; }
    public CanonicalMigrationDomain Domain { get; set; }
    public bool Enabled { get; set; }
    public bool ExplicitInternalTestConfiguration { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool DefaultCanonicalReadEnabled { get; set; }
    public bool DefaultCanonicalWriteEnabled { get; set; }
    public CanonicalMigrationDomain? ActivePilotDomain { get; set; }
    public bool OtherDomainsStaticOnly { get; set; }
    public int WriteCanonicalCommitAttemptCount { get; set; }
    public int WriteCanonicalCommitSucceededCount { get; set; }
    public int WriteCanonicalCommitFailedCount { get; set; }
    public int RollbackAttemptCount { get; set; }
    public int RollbackSucceededCount { get; set; }
    public int RollbackFailedCount { get; set; }
    public int RollbackFatalCount { get; set; }
    public int LegacyFallbackUsedCount { get; set; }
    public int DuplicateLegacySuppressedCount { get; set; }
    public int UnresolvedConflictCount { get; set; }
    public int ResourceMoveAttemptedCount { get; set; }
    public int CanonicalReadCandidateBuiltCount { get; set; }
    public int CanonicalReadServedCount { get; set; }
    public int LegacyReadFallbackCount { get; set; }
    public int ReadSideParallelEquivalentCount { get; set; }
    public int ReadSideParallelDivergentCount { get; set; }
    public int ReadSideDivergenceCount { get; set; }
    public int ReadSideUnsupportedObjectCount { get; set; }
    public int ReadSidePathLeakRiskCount { get; set; }
    public int UnsafeSideEffectCount { get; set; }
    public int SyncOrUploadTriggeredCount { get; set; }
    public int UiMutatedCount { get; set; }
    public int ContentWrittenCount { get; set; }
    public int TombstoneDeleteAttemptedCount { get; set; }
    public List<CanonicalLibraryMetadataCutoverDiagnostic> Diagnostics { get; set; }
    public string DiagnosticsSummary { get; set; }

    public CanonicalLibraryMetadataObservationWindow(
        string observationWindowID,
        CanonicalLibraryMetadataObservationPolicy? policy = null,
        CanonicalMigrationDomainMatrix? matrix = null)
    {
        var p = policy ?? CanonicalLibraryMetadataObservationPolicy.Disabled;
        var m = matrix ?? CanonicalMigrationDomainMatrix.DefaultV813();
        var matrixReport = m.Validate();
        ObservationWindowID = CanonicalProductionRedaction.SafeIdentifier(observationWindowID, "library-metadata-observation")!;
        Domain = CanonicalMigrationDomain.libraryMetadata;
        Enabled = p.Enabled;
        ExplicitInternalTestConfiguration = p.ExplicitInternalTestConfiguration;
        RuntimeSwitchEnabled = m.Policies.Any(pol => pol.RuntimeSwitchEnabled);
        DefaultCanonicalReadEnabled = m.Policies.Any(pol => pol.DefaultCutoverEnabled);
        DefaultCanonicalWriteEnabled = m.Policies.Any(pol => pol.ReleaseDefaultEnabledCutover);
        ActivePilotDomain = matrixReport.ActivePilotDomain;
        OtherDomainsStaticOnly = m.Policies
            .Where(pol => pol.Domain != CanonicalMigrationDomain.libraryMetadata)
            .All(pol => pol.StaticOnly && !pol.ActivePilot && !pol.HasActiveCanaryOrCutover);
        // All counts initialized to 0
        WriteCanonicalCommitAttemptCount = 0;
        WriteCanonicalCommitSucceededCount = 0;
        WriteCanonicalCommitFailedCount = 0;
        RollbackAttemptCount = 0;
        RollbackSucceededCount = 0;
        RollbackFailedCount = 0;
        RollbackFatalCount = 0;
        LegacyFallbackUsedCount = 0;
        DuplicateLegacySuppressedCount = 0;
        UnresolvedConflictCount = 0;
        ResourceMoveAttemptedCount = 0;
        CanonicalReadCandidateBuiltCount = 0;
        CanonicalReadServedCount = 0;
        LegacyReadFallbackCount = 0;
        ReadSideParallelEquivalentCount = 0;
        ReadSideParallelDivergentCount = 0;
        ReadSideDivergenceCount = 0;
        ReadSideUnsupportedObjectCount = 0;
        ReadSidePathLeakRiskCount = 0;
        UnsafeSideEffectCount = 0;
        SyncOrUploadTriggeredCount = 0;
        UiMutatedCount = 0;
        ContentWrittenCount = 0;
        TombstoneDeleteAttemptedCount = 0;
        Diagnostics = new List<CanonicalLibraryMetadataCutoverDiagnostic>();
        DiagnosticsSummary = $"v8.20,domain=libraryMetadata,enabled={p.Enabled},explicitInternalTest={p.ExplicitInternalTestConfiguration}";
    }

    public static CanonicalLibraryMetadataObservationWindow DisabledWindow(
        string observationWindowID = "libraryMetadataObservationDisabled",
        CanonicalMigrationDomainMatrix? matrix = null) =>
        new(observationWindowID, CanonicalLibraryMetadataObservationPolicy.Disabled, matrix);

    public int TotalEventCount =>
        WriteCanonicalCommitAttemptCount + WriteCanonicalCommitSucceededCount + WriteCanonicalCommitFailedCount +
        RollbackAttemptCount + RollbackSucceededCount + RollbackFailedCount + RollbackFatalCount +
        LegacyFallbackUsedCount + DuplicateLegacySuppressedCount + UnresolvedConflictCount +
        ResourceMoveAttemptedCount + CanonicalReadCandidateBuiltCount + CanonicalReadServedCount +
        LegacyReadFallbackCount + ReadSideParallelEquivalentCount + ReadSideParallelDivergentCount +
        ReadSideUnsupportedObjectCount + ReadSidePathLeakRiskCount + UnsafeSideEffectCount +
        SyncOrUploadTriggeredCount + UiMutatedCount + ContentWrittenCount + TombstoneDeleteAttemptedCount;

    public int ReadEvidenceCount =>
        CanonicalReadServedCount + CanonicalReadCandidateBuiltCount + ReadSideParallelEquivalentCount;

    public bool NoUnsafeSideEffects =>
        UnsafeSideEffectCount == 0 && ResourceMoveAttemptedCount == 0 &&
        SyncOrUploadTriggeredCount == 0 && ContentWrittenCount == 0 &&
        TombstoneDeleteAttemptedCount == 0 && UiMutatedCount == 0;

    public CanonicalLibraryMetadataObservationWindow Recording(CanonicalLibraryMetadataObservationEvent evt)
    {
        if (!Enabled || evt.Count <= 0) return this;
        var copy = Clone();
        copy.Apply(evt);
        copy.RefreshDiagnosticsSummary();
        return copy;
    }

    public CanonicalLibraryMetadataObservationWindow RecordingWriteSideResult(
        CanonicalLibraryMetadataCutoverResult result,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? syncRunID = null)
    {
        if (!Enabled) return this;
        var copy = Clone();
        var attemptedCount = Math.Max(result.CanaryAttemptedCount, result.Commits.Count);
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.canonicalCommitAttempted, attemptedCount, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.canonicalCommitSucceeded, result.Commits.Count(c => c.Committed), syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.canonicalCommitFailed, result.Commits.Count(c => !c.Committed), syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.rollbackAttempted, result.RollbackResults.Count, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.rollbackSucceeded, result.RollbackResults.Count(r => r.Succeeded), syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.rollbackFailed, result.RollbackResults.Count(r => !r.Succeeded), syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.rollbackFatal, result.RollbackResults.Count(r => r.Fatal), syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.legacyFallbackUsed, result.LegacyFallbackUsed ? 1 : 0, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.duplicateLegacySuppressed, result.DuplicateLegacySuppressedActionIDs.Count, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.resourceMoveAttempted,
            result.Diagnostics.Count(d => d.Kind == CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataResourceMoveBlocked), syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.unsafeSideEffectObserved,
            result.Commits.SelectMany(c => c.SideEffects).Count(se => se.IsUnsafeForLibraryMetadataObservation()), syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.tombstoneDeleteAttempted,
            result.Commits.Count(c => c.ActionKind == CanonicalLibraryMetadataCutoverActionKind.tombstoneMarkerUnsupportedForThisRound), syncRunID, trigger, nodeRole));
        copy.AppendDiagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataObservationWriteSideRecorded,
            trigger, nodeRole, syncRunID, "recorded",
            $"attempted={attemptedCount},succeeded={result.Commits.Count(c => c.Committed)},rollbackFailures={result.RollbackResults.Count(r => !r.Succeeded)}");
        copy.RefreshDiagnosticsSummary();
        return copy;
    }

    public CanonicalLibraryMetadataObservationWindow RecordingReadSourceResult(
        CanonicalLibraryMetadataReadSourceResult result,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? syncRunID = null)
    {
        if (!Enabled) return this;
        var copy = Clone();
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.canonicalReadCandidateBuilt, result.CanonicalCandidateBuilt ? 1 : 0, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.canonicalReadServed, result.CanonicalReadServed ? 1 : 0, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.legacyReadFallbackUsed, result.FallbackCount, syncRunID, trigger, nodeRole));
        copy.Apply(new(result.DiffReport?.Equivalent == false
            ? CanonicalLibraryMetadataObservationEventKind.readSideParallelDivergent
            : CanonicalLibraryMetadataObservationEventKind.readSideParallelEquivalent,
            result.DiffReport == null ? 0 : 1, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.readSideUnsupportedObject, result.DiffReport?.UnsupportedObjectCount ?? 0, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.readSidePathLeakRisk, result.DiffReport?.PathLeakRiskCount ?? 0, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.syncOrUploadTriggered, result.SyncOrUploadTriggered ? 1 : 0, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.resourceMoveAttempted, result.ResourceMoved ? 1 : 0, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.contentWritten, result.ContentWritten ? 1 : 0, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.uiMutated, result.UiMutated ? 1 : 0, syncRunID, trigger, nodeRole));
        copy.AppendDiagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataObservationReadSideRecorded,
            trigger, nodeRole, syncRunID, result.CanonicalReadServed ? "canonicalReadServed" : "reportOnly",
            $"candidateBuilt={result.CanonicalCandidateBuilt},fallback={result.FallbackCount},divergence={result.DiffReport?.DivergenceCount ?? 0}");
        copy.RefreshDiagnosticsSummary();
        return copy;
    }

    public CanonicalLibraryMetadataObservationWindow RecordingReadSideCutoverResult(
        CanonicalLibraryMetadataReadSideCutoverResult result,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? syncRunID = null)
    {
        if (!Enabled) return this;
        var copy = Clone();
        if (result.DiffReport is { } report)
        {
            copy.Apply(new(report.Equivalent
                ? CanonicalLibraryMetadataObservationEventKind.readSideParallelEquivalent
                : CanonicalLibraryMetadataObservationEventKind.readSideParallelDivergent, 1, syncRunID, trigger, nodeRole));
            copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.readSideUnsupportedObject, report.UnsupportedObjectCount, syncRunID, trigger, nodeRole));
            copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.readSidePathLeakRisk, report.PathLeakRiskCount, syncRunID, trigger, nodeRole));
        }
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.legacyReadFallbackUsed, result.LegacyReadFallbackAvailable ? 1 : 0, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.syncOrUploadTriggered, result.SyncOrUploadTriggered ? 1 : 0, syncRunID, trigger, nodeRole));
        copy.Apply(new(CanonicalLibraryMetadataObservationEventKind.uiMutated, result.UiMutated ? 1 : 0, syncRunID, trigger, nodeRole));
        copy.AppendDiagnostic(CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataObservationReadSideRecorded,
            trigger, nodeRole, syncRunID, result.Candidate.Ready ? "readCandidateReady" : "readCandidateBlocked",
            result.Candidate.DiagnosticsSummary);
        copy.RefreshDiagnosticsSummary();
        return copy;
    }

    private CanonicalLibraryMetadataObservationWindow Clone()
    {
        var clone = (CanonicalLibraryMetadataObservationWindow)MemberwiseClone();
        clone.Diagnostics = new List<CanonicalLibraryMetadataCutoverDiagnostic>(Diagnostics);
        return clone;
    }

    private void Apply(CanonicalLibraryMetadataObservationEvent evt)
    {
        switch (evt.Kind)
        {
            case CanonicalLibraryMetadataObservationEventKind.canonicalCommitAttempted: WriteCanonicalCommitAttemptCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.canonicalCommitSucceeded: WriteCanonicalCommitSucceededCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.canonicalCommitFailed: WriteCanonicalCommitFailedCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.rollbackAttempted: RollbackAttemptCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.rollbackSucceeded: RollbackSucceededCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.rollbackFailed: RollbackFailedCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.rollbackFatal: RollbackFatalCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.legacyFallbackUsed: LegacyFallbackUsedCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.duplicateLegacySuppressed: DuplicateLegacySuppressedCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.unresolvedConflictObserved: UnresolvedConflictCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.resourceMoveAttempted: ResourceMoveAttemptedCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.canonicalReadCandidateBuilt: CanonicalReadCandidateBuiltCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.canonicalReadServed: CanonicalReadServedCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.legacyReadFallbackUsed: LegacyReadFallbackCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.readSideParallelEquivalent: ReadSideParallelEquivalentCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.readSideParallelDivergent: ReadSideParallelDivergentCount += evt.Count; ReadSideDivergenceCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.readSideUnsupportedObject: ReadSideUnsupportedObjectCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.readSidePathLeakRisk: ReadSidePathLeakRiskCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.unsafeSideEffectObserved: UnsafeSideEffectCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.syncOrUploadTriggered: SyncOrUploadTriggeredCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.uiMutated: UiMutatedCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.contentWritten: ContentWrittenCount += evt.Count; break;
            case CanonicalLibraryMetadataObservationEventKind.tombstoneDeleteAttempted: TombstoneDeleteAttemptedCount += evt.Count; break;
        }
    }

    private void AppendDiagnostic(
        CanonicalLibraryMetadataCutoverDiagnosticKind kind,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        string? syncRunID, string result, string reason)
    {
        Diagnostics.Add(new CanonicalLibraryMetadataCutoverDiagnostic(
            kind, syncRunID, trigger, nodeRole,
            domain: CanonicalLibraryMetadataCutoverDomain.folderMetadata,
            result: result, reason: reason));
    }

    private void RefreshDiagnosticsSummary()
    {
        DiagnosticsSummary = string.Join(",",
            "v8.20",
            "domain=libraryMetadata",
            $"enabled={Enabled}",
            $"explicitInternalTest={ExplicitInternalTestConfiguration}",
            $"writeAttempts={WriteCanonicalCommitAttemptCount}",
            $"writeSucceeded={WriteCanonicalCommitSucceededCount}",
            $"readEvidence={ReadEvidenceCount}",
            $"divergence={ReadSideDivergenceCount}",
            $"rollbackFailures={RollbackFailedCount}",
            $"unsafeSideEffects={UnsafeSideEffectCount}",
            $"fallback={LegacyFallbackUsedCount + LegacyReadFallbackCount}",
            $"runtimeSwitch={RuntimeSwitchEnabled}",
            $"defaultRead={DefaultCanonicalReadEnabled}",
            $"defaultWrite={DefaultCanonicalWriteEnabled}");
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataObservationWindow other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataObservationWindow? other) =>
        other is not null && ObservationWindowID == other.ObservationWindowID && Domain == other.Domain &&
        Enabled == other.Enabled && ExplicitInternalTestConfiguration == other.ExplicitInternalTestConfiguration &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled && DefaultCanonicalReadEnabled == other.DefaultCanonicalReadEnabled &&
        DefaultCanonicalWriteEnabled == other.DefaultCanonicalWriteEnabled && ActivePilotDomain == other.ActivePilotDomain &&
        OtherDomainsStaticOnly == other.OtherDomainsStaticOnly &&
        WriteCanonicalCommitAttemptCount == other.WriteCanonicalCommitAttemptCount &&
        WriteCanonicalCommitSucceededCount == other.WriteCanonicalCommitSucceededCount &&
        WriteCanonicalCommitFailedCount == other.WriteCanonicalCommitFailedCount &&
        RollbackAttemptCount == other.RollbackAttemptCount && RollbackSucceededCount == other.RollbackSucceededCount &&
        RollbackFailedCount == other.RollbackFailedCount && RollbackFatalCount == other.RollbackFatalCount &&
        LegacyFallbackUsedCount == other.LegacyFallbackUsedCount && DuplicateLegacySuppressedCount == other.DuplicateLegacySuppressedCount &&
        UnresolvedConflictCount == other.UnresolvedConflictCount && ResourceMoveAttemptedCount == other.ResourceMoveAttemptedCount &&
        CanonicalReadCandidateBuiltCount == other.CanonicalReadCandidateBuiltCount && CanonicalReadServedCount == other.CanonicalReadServedCount &&
        LegacyReadFallbackCount == other.LegacyReadFallbackCount && ReadSideParallelEquivalentCount == other.ReadSideParallelEquivalentCount &&
        ReadSideParallelDivergentCount == other.ReadSideParallelDivergentCount && ReadSideDivergenceCount == other.ReadSideDivergenceCount &&
        ReadSideUnsupportedObjectCount == other.ReadSideUnsupportedObjectCount && ReadSidePathLeakRiskCount == other.ReadSidePathLeakRiskCount &&
        UnsafeSideEffectCount == other.UnsafeSideEffectCount && SyncOrUploadTriggeredCount == other.SyncOrUploadTriggeredCount &&
        UiMutatedCount == other.UiMutatedCount && ContentWrittenCount == other.ContentWrittenCount &&
        TombstoneDeleteAttemptedCount == other.TombstoneDeleteAttemptedCount &&
        Diagnostics.SequenceEqual(other.Diagnostics) && DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() =>
        HashCode.Combine(ObservationWindowID, Domain, Enabled, ExplicitInternalTestConfiguration, RuntimeSwitchEnabled,
            DefaultCanonicalReadEnabled, DefaultCanonicalWriteEnabled, ActivePilotDomain, OtherDomainsStaticOnly,
            WriteCanonicalCommitAttemptCount, WriteCanonicalCommitSucceededCount, WriteCanonicalCommitFailedCount,
            RollbackAttemptCount, RollbackSucceededCount, RollbackFailedCount, RollbackFatalCount,
            LegacyFallbackUsedCount, DuplicateLegacySuppressedCount, UnresolvedConflictCount, ResourceMoveAttemptedCount,
            CanonicalReadCandidateBuiltCount, CanonicalReadServedCount, LegacyReadFallbackCount,
            ReadSideParallelEquivalentCount, ReadSideParallelDivergentCount, ReadSideDivergenceCount,
            ReadSideUnsupportedObjectCount, ReadSidePathLeakRiskCount, UnsafeSideEffectCount,
            SyncOrUploadTriggeredCount, UiMutatedCount, ContentWrittenCount, TombstoneDeleteAttemptedCount,
            Diagnostics.Count, DiagnosticsSummary);
    public static bool operator ==(CanonicalLibraryMetadataObservationWindow left, CanonicalLibraryMetadataObservationWindow right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataObservationWindow left, CanonicalLibraryMetadataObservationWindow right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataObservationFailure
{
    disabled,
    missingExplicitInternalTestConfiguration,
    nonLibraryMetadataActivePilot,
    otherActiveDomain,
    otherDomainsNotStaticOnly,
    writeSideEvidenceMissing,
    readSideEvidenceMissing,
    observationWindowIncomplete,
    fallbackMissing,
    divergencePresent,
    rollbackFailure,
    unsupportedObject,
    pathLeakRisk,
    unsafeSideEffect,
    runtimeSwitchEnabled,
    defaultCutoverEnabled,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataObservationGateState
{
    incomplete,
    completeButRetirementBlocked,
    completeReadyForRetirementCandidate,
    blockedByDivergence,
    blockedByRollbackFailure,
    blockedByUnsupportedObject,
    blockedByFallbackMissing,
    blockedByOtherActiveDomain,
    blockedByUnsafeSideEffect,
}

public sealed class CanonicalLibraryMetadataObservationGateResult : IEquatable<CanonicalLibraryMetadataObservationGateResult>
{
    public CanonicalLibraryMetadataObservationGateState State { get; set; }
    public bool Complete { get; set; }
    public bool RetirementCandidateReady { get; set; }
    public List<CanonicalLibraryMetadataObservationFailure> Blockers { get; set; }
    public List<CanonicalLibraryMetadataCutoverDiagnostic> Diagnostics { get; set; }
    public string DiagnosticsSummary { get; set; }

    public CanonicalLibraryMetadataObservationGateResult(
        CanonicalLibraryMetadataObservationGateState state = CanonicalLibraryMetadataObservationGateState.incomplete,
        bool complete = false,
        bool retirementCandidateReady = false,
        List<CanonicalLibraryMetadataObservationFailure>? blockers = null,
        List<CanonicalLibraryMetadataCutoverDiagnostic>? diagnostics = null,
        string diagnosticsSummary = "")
    {
        State = state;
        Complete = complete;
        RetirementCandidateReady = retirementCandidateReady;
        Blockers = blockers ?? new List<CanonicalLibraryMetadataObservationFailure>();
        Diagnostics = diagnostics ?? new List<CanonicalLibraryMetadataCutoverDiagnostic>();
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataObservationGateResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataObservationGateResult? other) =>
        other is not null && State == other.State && Complete == other.Complete &&
        RetirementCandidateReady == other.RetirementCandidateReady &&
        Blockers.SequenceEqual(other.Blockers) && Diagnostics.SequenceEqual(other.Diagnostics) &&
        DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() =>
        HashCode.Combine(State, Complete, RetirementCandidateReady, Blockers.Count, Diagnostics.Count, DiagnosticsSummary);
    public static bool operator ==(CanonicalLibraryMetadataObservationGateResult left, CanonicalLibraryMetadataObservationGateResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataObservationGateResult left, CanonicalLibraryMetadataObservationGateResult right) => !left.Equals(right);
}

public static class CanonicalLibraryMetadataObservationGate
{
    public static CanonicalLibraryMetadataObservationGateResult Evaluate(
        CanonicalLibraryMetadataObservationWindow window,
        CanonicalLibraryMetadataObservationPolicy policy,
        CanonicalMigrationDomainMatrix? matrix = null,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? syncRunID = null)
    {
        matrix ??= CanonicalMigrationDomainMatrix.DefaultV813();
        var blockers = new List<CanonicalLibraryMetadataObservationFailure>();
        var matrixReport = matrix.Validate();

        if (!policy.Enabled || !window.Enabled) blockers.Add(CanonicalLibraryMetadataObservationFailure.disabled);
        if (!policy.ExplicitInternalTestConfiguration || !window.ExplicitInternalTestConfiguration) blockers.Add(CanonicalLibraryMetadataObservationFailure.missingExplicitInternalTestConfiguration);
        if (policy.RequireOnlyLibraryMetadataActivePilot)
        {
            if (matrixReport.ActivePilotDomain != CanonicalMigrationDomain.libraryMetadata) blockers.Add(CanonicalLibraryMetadataObservationFailure.nonLibraryMetadataActivePilot);
            if (matrixReport.Blockers.Contains(CanonicalMigrationDomainMatrixBlocker.multipleActivePilots)) blockers.Add(CanonicalLibraryMetadataObservationFailure.otherActiveDomain);
        }
        if (!window.OtherDomainsStaticOnly) blockers.Add(CanonicalLibraryMetadataObservationFailure.otherDomainsNotStaticOnly);
        if (window.RuntimeSwitchEnabled) blockers.Add(CanonicalLibraryMetadataObservationFailure.runtimeSwitchEnabled);
        if (window.DefaultCanonicalReadEnabled || window.DefaultCanonicalWriteEnabled) blockers.Add(CanonicalLibraryMetadataObservationFailure.defaultCutoverEnabled);
        if (window.WriteCanonicalCommitSucceededCount < policy.MinimumWriteCanonicalCommitCount) blockers.Add(CanonicalLibraryMetadataObservationFailure.writeSideEvidenceMissing);
        if (window.ReadEvidenceCount < policy.MinimumReadCanonicalEvidenceCount) blockers.Add(CanonicalLibraryMetadataObservationFailure.readSideEvidenceMissing);
        if (window.TotalEventCount < policy.MinimumTotalEventCount) blockers.Add(CanonicalLibraryMetadataObservationFailure.observationWindowIncomplete);
        if (policy.RequireLegacyFallbackAvailable && !policy.LegacyFallbackAvailable) blockers.Add(CanonicalLibraryMetadataObservationFailure.fallbackMissing);
        if (window.LegacyFallbackUsedCount + window.LegacyReadFallbackCount == 0 && policy.RequireLegacyFallbackAvailable) blockers.Add(CanonicalLibraryMetadataObservationFailure.fallbackMissing);
        if (window.ReadSideDivergenceCount > 0) blockers.Add(CanonicalLibraryMetadataObservationFailure.divergencePresent);
        if (window.RollbackFailedCount > 0 || window.RollbackFatalCount > 0) blockers.Add(CanonicalLibraryMetadataObservationFailure.rollbackFailure);
        if (window.ReadSideUnsupportedObjectCount > 0) blockers.Add(CanonicalLibraryMetadataObservationFailure.unsupportedObject);
        if (window.ReadSidePathLeakRiskCount > 0) blockers.Add(CanonicalLibraryMetadataObservationFailure.pathLeakRisk);
        if (!window.NoUnsafeSideEffects) blockers.Add(CanonicalLibraryMetadataObservationFailure.unsafeSideEffect);

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataObservationFailure>(blockers).OrderBy(b => b.ToString()).ToList();
        var complete = uniqueBlockers.Count == 0;

        var state = uniqueBlockers switch
        {
            var b when b.Contains(CanonicalLibraryMetadataObservationFailure.unsafeSideEffect) ||
                         b.Contains(CanonicalLibraryMetadataObservationFailure.pathLeakRisk) ||
                         b.Contains(CanonicalLibraryMetadataObservationFailure.runtimeSwitchEnabled) ||
                         b.Contains(CanonicalLibraryMetadataObservationFailure.defaultCutoverEnabled) =>
                CanonicalLibraryMetadataObservationGateState.blockedByUnsafeSideEffect,
            var b when b.Contains(CanonicalLibraryMetadataObservationFailure.otherActiveDomain) ||
                         b.Contains(CanonicalLibraryMetadataObservationFailure.nonLibraryMetadataActivePilot) ||
                         b.Contains(CanonicalLibraryMetadataObservationFailure.otherDomainsNotStaticOnly) =>
                CanonicalLibraryMetadataObservationGateState.blockedByOtherActiveDomain,
            var b when b.Contains(CanonicalLibraryMetadataObservationFailure.rollbackFailure) =>
                CanonicalLibraryMetadataObservationGateState.blockedByRollbackFailure,
            var b when b.Contains(CanonicalLibraryMetadataObservationFailure.divergencePresent) =>
                CanonicalLibraryMetadataObservationGateState.blockedByDivergence,
            var b when b.Contains(CanonicalLibraryMetadataObservationFailure.unsupportedObject) =>
                CanonicalLibraryMetadataObservationGateState.blockedByUnsupportedObject,
            var b when b.Contains(CanonicalLibraryMetadataObservationFailure.fallbackMissing) =>
                CanonicalLibraryMetadataObservationGateState.blockedByFallbackMissing,
            var b when b.Count == 0 =>
                CanonicalLibraryMetadataObservationGateState.completeReadyForRetirementCandidate,
            _ => CanonicalLibraryMetadataObservationGateState.incomplete
        };

        var evaluated = new CanonicalLibraryMetadataCutoverDiagnostic(
            CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataObservationGateEvaluated,
            syncRunID, trigger, nodeRole,
            result: state.ToString(), reason: "reportOnly=true");
        var outcome = new CanonicalLibraryMetadataCutoverDiagnostic(
            complete ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataObservationGateReady
                : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataObservationGateBlocked,
            syncRunID, trigger, nodeRole,
            result: complete ? "ready" : "blocked",
            reason: string.Join("+", uniqueBlockers.Select(b => b.ToString())));
        var summary = string.Join(",",
                "v8.20", $"observationGate={state}", $"complete={complete}",
                $"candidateReady={complete}", $"blockers={string.Join("+", uniqueBlockers.Select(b => b.ToString()))}",
                "reportOnly=true");

        return new CanonicalLibraryMetadataObservationGateResult(
            state, complete, complete, uniqueBlockers,
            new List<CanonicalLibraryMetadataCutoverDiagnostic> { evaluated, outcome }, summary);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataRetirementCandidateGateStatus
{
    ready,
    blocked,
}

public sealed class CanonicalLibraryMetadataRetirementCandidateReport : IEquatable<CanonicalLibraryMetadataRetirementCandidateReport>
{
    public CanonicalLibraryMetadataRetirementCandidateGateStatus Status { get; set; }
    public bool RetirementCandidateReady { get; set; }
    public bool RetirementExecutionPerformed { get; set; }
    public bool LegacyDeleted { get; set; }
    public bool LegacyDisabled { get; set; }
    public bool ReportOnly { get; set; }
    public bool ManualAuditRequired { get; set; }
    public List<CanonicalLibraryMetadataRetirementBlocker> Blockers { get; set; }
    public CanonicalLibraryMetadataObservationGateResult ObservationGate { get; set; }
    public List<CanonicalLibraryMetadataCutoverDiagnostic> Diagnostics { get; set; }
    public string DiagnosticsSummary { get; set; }

    public CanonicalLibraryMetadataRetirementCandidateReport(
        CanonicalLibraryMetadataRetirementCandidateGateStatus status = CanonicalLibraryMetadataRetirementCandidateGateStatus.blocked,
        bool retirementCandidateReady = false,
        bool retirementExecutionPerformed = false,
        bool legacyDeleted = false,
        bool legacyDisabled = false,
        bool reportOnly = true,
        bool manualAuditRequired = true,
        List<CanonicalLibraryMetadataRetirementBlocker>? blockers = null,
        CanonicalLibraryMetadataObservationGateResult? observationGate = null,
        List<CanonicalLibraryMetadataCutoverDiagnostic>? diagnostics = null,
        string diagnosticsSummary = "")
    {
        Status = status;
        RetirementCandidateReady = retirementCandidateReady;
        RetirementExecutionPerformed = retirementExecutionPerformed;
        LegacyDeleted = legacyDeleted;
        LegacyDisabled = legacyDisabled;
        ReportOnly = reportOnly;
        ManualAuditRequired = manualAuditRequired;
        Blockers = blockers ?? new List<CanonicalLibraryMetadataRetirementBlocker>();
        ObservationGate = observationGate ?? new CanonicalLibraryMetadataObservationGateResult();
        Diagnostics = diagnostics ?? new List<CanonicalLibraryMetadataCutoverDiagnostic>();
        DiagnosticsSummary = diagnosticsSummary;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataRetirementCandidateReport other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataRetirementCandidateReport? other) =>
        other is not null && Status == other.Status && RetirementCandidateReady == other.RetirementCandidateReady &&
        RetirementExecutionPerformed == other.RetirementExecutionPerformed && LegacyDeleted == other.LegacyDeleted &&
        LegacyDisabled == other.LegacyDisabled && ReportOnly == other.ReportOnly &&
        ManualAuditRequired == other.ManualAuditRequired && Blockers.SequenceEqual(other.Blockers) &&
        EqualityComparer<CanonicalLibraryMetadataObservationGateResult>.Default.Equals(ObservationGate, other.ObservationGate) &&
        Diagnostics.SequenceEqual(other.Diagnostics) && DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() =>
        HashCode.Combine(Status, RetirementCandidateReady, RetirementExecutionPerformed, LegacyDeleted, LegacyDisabled,
            ReportOnly, ManualAuditRequired, Blockers.Count, ObservationGate, Diagnostics.Count, DiagnosticsSummary);
    public static bool operator ==(CanonicalLibraryMetadataRetirementCandidateReport left, CanonicalLibraryMetadataRetirementCandidateReport right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataRetirementCandidateReport left, CanonicalLibraryMetadataRetirementCandidateReport right) => !left.Equals(right);
}

public static class CanonicalLibraryMetadataRetirementCandidateGate
{
    public static CanonicalLibraryMetadataRetirementCandidateReport Evaluate(
        CanonicalLibraryMetadataObservationGateResult observationGate,
        CanonicalLibraryMetadataObservationPolicy policy,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness,
        string? syncRunID = null)
    {
        var blockers = new List<CanonicalLibraryMetadataRetirementBlocker>();
        if (!observationGate.Complete || !observationGate.RetirementCandidateReady) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.observationWindowIncomplete);
        if (!policy.ManualAuditRequired) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.manualAuditRequired);
        if (observationGate.Blockers.Contains(CanonicalLibraryMetadataObservationFailure.fallbackMissing)) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.fallbackMissing);
        if (observationGate.Blockers.Contains(CanonicalLibraryMetadataObservationFailure.divergencePresent)) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.divergencePresent);
        if (observationGate.Blockers.Contains(CanonicalLibraryMetadataObservationFailure.unsupportedObject)) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.unsupportedObject);
        if (observationGate.Blockers.Contains(CanonicalLibraryMetadataObservationFailure.rollbackFailure)) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.rollbackFatal);
        if (observationGate.Blockers.Contains(CanonicalLibraryMetadataObservationFailure.otherActiveDomain) ||
            observationGate.Blockers.Contains(CanonicalLibraryMetadataObservationFailure.otherDomainsNotStaticOnly)) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.otherDomainsAffected);
        if (observationGate.Blockers.Contains(CanonicalLibraryMetadataObservationFailure.unsafeSideEffect)) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.unsafeSideEffect);
        if (observationGate.Blockers.Contains(CanonicalLibraryMetadataObservationFailure.pathLeakRisk)) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.pathLeakRisk);
        if (observationGate.Blockers.Contains(CanonicalLibraryMetadataObservationFailure.runtimeSwitchEnabled)) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.runtimeSwitchEnabled);
        if (observationGate.Blockers.Contains(CanonicalLibraryMetadataObservationFailure.defaultCutoverEnabled)) blockers.Add(CanonicalLibraryMetadataRetirementBlocker.defaultReadOrWriteCutoverEnabled);

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataRetirementBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        var ready = uniqueBlockers.Count == 0;
        var evaluated = new CanonicalLibraryMetadataCutoverDiagnostic(
            CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRetirementCandidateGateEvaluated,
            syncRunID, trigger, nodeRole,
            result: ready ? "ready" : "blocked",
            reason: $"reportOnly=true,manualAuditRequired={policy.ManualAuditRequired}");
        var outcome = new CanonicalLibraryMetadataCutoverDiagnostic(
            ready ? CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRetirementCandidateReady
                : CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataRetirementCandidateGateBlocked,
            syncRunID, trigger, nodeRole,
            result: ready ? "candidate" : "blocked",
            reason: string.Join("+", uniqueBlockers.Select(b => b.ToString())));
        var summary = string.Join(",",
            "v8.20", $"retirementCandidateReady={ready}", "retirementExecutionPerformed=false",
            "legacyDeleted=false", "legacyDisabled=false",
            $"manualAuditRequired={policy.ManualAuditRequired}", "reportOnly=true",
            $"blockers={string.Join("+", uniqueBlockers.Select(b => b.ToString()))}");

        return new CanonicalLibraryMetadataRetirementCandidateReport(
            ready ? CanonicalLibraryMetadataRetirementCandidateGateStatus.ready : CanonicalLibraryMetadataRetirementCandidateGateStatus.blocked,
            ready, false, false, false, true, policy.ManualAuditRequired,
            uniqueBlockers, observationGate,
            new List<CanonicalLibraryMetadataCutoverDiagnostic> { evaluated, outcome }, summary);
    }
}

public sealed class CanonicalLibraryMetadataRollbackDrillSummary : IEquatable<CanonicalLibraryMetadataRollbackDrillSummary>
{
    public int AttemptedCount { get; set; }
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public int FatalCount { get; set; }
    public bool Clean { get; set; }
    public string DiagnosticsSummary { get; set; }

    public CanonicalLibraryMetadataRollbackDrillSummary(CanonicalLibraryMetadataObservationWindow window)
    {
        AttemptedCount = window.RollbackAttemptCount;
        SucceededCount = window.RollbackSucceededCount;
        FailedCount = window.RollbackFailedCount;
        FatalCount = window.RollbackFatalCount;
        Clean = window.RollbackFailedCount == 0 && window.RollbackFatalCount == 0;
        DiagnosticsSummary = $"v8.20,rollbackDrill,attempted={window.RollbackAttemptCount},succeeded={window.RollbackSucceededCount},failed={window.RollbackFailedCount},fatal={window.RollbackFatalCount},clean={Clean}";
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataRollbackDrillSummary other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataRollbackDrillSummary? other) =>
        other is not null && AttemptedCount == other.AttemptedCount && SucceededCount == other.SucceededCount &&
        FailedCount == other.FailedCount && FatalCount == other.FatalCount &&
        Clean == other.Clean && DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() =>
        HashCode.Combine(AttemptedCount, SucceededCount, FailedCount, FatalCount, Clean, DiagnosticsSummary);
    public static bool operator ==(CanonicalLibraryMetadataRollbackDrillSummary left, CanonicalLibraryMetadataRollbackDrillSummary right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataRollbackDrillSummary left, CanonicalLibraryMetadataRollbackDrillSummary right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataEndToEndPilotStatus
{
    pilotIncomplete,
    pilotWriteSideOnly,
    pilotReadSideParallelOnly,
    pilotObservationReady,
    pilotRetirementCandidateReady,
    blocked,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataEndToEndPilotBlocker
{
    observationBlocked,
    retirementCandidateBlocked,
    writeSideMissing,
    readSideMissing,
}

public sealed class CanonicalLibraryMetadataEndToEndPilotReport : IEquatable<CanonicalLibraryMetadataEndToEndPilotReport>
{
    public CanonicalLibraryMetadataEndToEndPilotStatus Status { get; set; }
    public CanonicalLibraryMetadataObservationWindow ObservationWindow { get; set; }
    public CanonicalLibraryMetadataObservationGateResult ObservationGate { get; set; }
    public CanonicalLibraryMetadataRetirementCandidateReport RetirementCandidateReport { get; set; }
    public CanonicalLibraryMetadataRollbackDrillSummary RollbackDrillSummary { get; set; }
    public List<CanonicalLibraryMetadataEndToEndPilotBlocker> Blockers { get; set; }
    public List<CanonicalLibraryMetadataCutoverDiagnostic> Diagnostics { get; set; }
    public string DiagnosticsSummary { get; set; }

    public CanonicalLibraryMetadataEndToEndPilotReport(
        CanonicalLibraryMetadataObservationWindow observationWindow,
        CanonicalLibraryMetadataObservationGateResult observationGate,
        CanonicalLibraryMetadataRetirementCandidateReport retirementCandidateReport)
    {
        ObservationWindow = observationWindow;
        ObservationGate = observationGate;
        RetirementCandidateReport = retirementCandidateReport;
        RollbackDrillSummary = new CanonicalLibraryMetadataRollbackDrillSummary(observationWindow);

        var blockers = new List<CanonicalLibraryMetadataEndToEndPilotBlocker>();
        if (observationWindow.WriteCanonicalCommitSucceededCount == 0) blockers.Add(CanonicalLibraryMetadataEndToEndPilotBlocker.writeSideMissing);
        if (observationWindow.ReadEvidenceCount == 0) blockers.Add(CanonicalLibraryMetadataEndToEndPilotBlocker.readSideMissing);
        if (!observationGate.Complete) blockers.Add(CanonicalLibraryMetadataEndToEndPilotBlocker.observationBlocked);
        if (!retirementCandidateReport.RetirementCandidateReady) blockers.Add(CanonicalLibraryMetadataEndToEndPilotBlocker.retirementCandidateBlocked);

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataEndToEndPilotBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        Blockers = uniqueBlockers;

        Status = retirementCandidateReport.RetirementCandidateReady
            ? CanonicalLibraryMetadataEndToEndPilotStatus.pilotRetirementCandidateReady
            : observationGate.Complete
                ? CanonicalLibraryMetadataEndToEndPilotStatus.pilotObservationReady
                : observationWindow.WriteCanonicalCommitSucceededCount > 0 && observationWindow.ReadEvidenceCount == 0
                    ? CanonicalLibraryMetadataEndToEndPilotStatus.pilotWriteSideOnly
                    : observationWindow.WriteCanonicalCommitSucceededCount == 0 && observationWindow.ReadEvidenceCount > 0
                        ? CanonicalLibraryMetadataEndToEndPilotStatus.pilotReadSideParallelOnly
                        : uniqueBlockers.Contains(CanonicalLibraryMetadataEndToEndPilotBlocker.observationBlocked) ||
                          uniqueBlockers.Contains(CanonicalLibraryMetadataEndToEndPilotBlocker.retirementCandidateBlocked)
                            ? CanonicalLibraryMetadataEndToEndPilotStatus.blocked
                            : CanonicalLibraryMetadataEndToEndPilotStatus.pilotIncomplete;

        var generated = new CanonicalLibraryMetadataCutoverDiagnostic(
            CanonicalLibraryMetadataCutoverDiagnosticKind.canonicalLibraryMetadataEndToEndPilotReportGenerated,
            null, CanonicalSyncPlanTrigger.periodic, CanonicalProductionExecutionDomainRole.testHarness,
            result: Status.ToString(),
            reason: string.Join("+", uniqueBlockers.Select(b => b.ToString())));

        Diagnostics = new List<CanonicalLibraryMetadataCutoverDiagnostic>(observationWindow.Diagnostics);
        Diagnostics.AddRange(observationGate.Diagnostics);
        Diagnostics.AddRange(retirementCandidateReport.Diagnostics);
        Diagnostics.Add(generated);

        DiagnosticsSummary = string.Join(",",
            "v8.20", $"endToEndStatus={Status}", $"retirementCandidateReady={retirementCandidateReport.RetirementCandidateReady}",
            "legacyDeleted=false", "legacyDisabled=false", "runtimeSwitch=false",
            $"blockers={string.Join("+", uniqueBlockers.Select(b => b.ToString()))}");
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataEndToEndPilotReport other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataEndToEndPilotReport? other) =>
        other is not null && Status == other.Status &&
        EqualityComparer<CanonicalLibraryMetadataObservationWindow>.Default.Equals(ObservationWindow, other.ObservationWindow) &&
        EqualityComparer<CanonicalLibraryMetadataObservationGateResult>.Default.Equals(ObservationGate, other.ObservationGate) &&
        EqualityComparer<CanonicalLibraryMetadataRetirementCandidateReport>.Default.Equals(RetirementCandidateReport, other.RetirementCandidateReport) &&
        EqualityComparer<CanonicalLibraryMetadataRollbackDrillSummary>.Default.Equals(RollbackDrillSummary, other.RollbackDrillSummary) &&
        Blockers.SequenceEqual(other.Blockers) && Diagnostics.SequenceEqual(other.Diagnostics) &&
        DiagnosticsSummary == other.DiagnosticsSummary;
    public override int GetHashCode() =>
        HashCode.Combine(Status, ObservationWindow, ObservationGate, RetirementCandidateReport, RollbackDrillSummary,
            Blockers.Count, Diagnostics.Count, DiagnosticsSummary);
    public static bool operator ==(CanonicalLibraryMetadataEndToEndPilotReport left, CanonicalLibraryMetadataEndToEndPilotReport right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataEndToEndPilotReport left, CanonicalLibraryMetadataEndToEndPilotReport right) => !left.Equals(right);
}

public static class CanonicalProductionSideEffectExtensions
{
    public static bool IsUnsafeForLibraryMetadataObservation(this CanonicalProductionSideEffect se) => se.Kind switch
    {
        CanonicalProductionSideEffectKind.networkRequest or
        CanonicalProductionSideEffectKind.uploadSessionStart or
        CanonicalProductionSideEffectKind.uploadChunkSend or
        CanonicalProductionSideEffectKind.uploadFinalize or
        CanonicalProductionSideEffectKind.generatedArtifactApply or
        CanonicalProductionSideEffectKind.tombstoneMark => true,
        _ => false
    };
}
