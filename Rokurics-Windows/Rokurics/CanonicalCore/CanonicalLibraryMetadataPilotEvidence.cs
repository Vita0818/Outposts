using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataN1EvidenceStatus
{
    missing,
    present,
    valid,
    invalid,
    insufficient,
    blocked,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataN1EvidenceBlocker
{
    missingN1Evidence,
    invalidEvidence,
    readSideDivergence,
    rollbackFailure,
    duplicateSuppressionWithoutCommitSuccess,
    otherActiveDomain,
    runtimeSwitchEnabled,
    releaseDefaultEnabled,
    sensitiveDataLeak,
    productionRootSafetyProofMissing,
    productionRootSafetyProofInvalid,
    landingFreezeMissing,
    landingFreezeBlocked,
    otherDomainsNotStaticOnly,
    unsafeCandidateExecuted,
    unsafeCandidateKind,
    resourceMove,
    contentWrite,
    generatedArtifactWrite,
    audioChange,
    readPathSwitched,
    uiMutated,
    tombstoneDelete,
    legacyFallbackUnavailable,
    defaultReleaseEnabled,
    requestVerifierRouteBoundaryViolation,
    receiveJSONMutation,
    n3ExecutionReported,
    n3DisabledByDefaultMissing,
    manualAuditMissing,
    ownerApprovalMissing,
}

public class CanonicalLibraryMetadataN1EvidenceRedactor
{
    public string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "redacted";
        if (ContainsSensitiveSignal(value))
        {
            var digest = CanonicalHash.Sha256String(value).Value;
            return $"redacted-{CanonicalProductionRedaction.HashPrefix(digest) ?? "evidence"}";
        }
        return CanonicalProductionRedaction.SafeDiagnosticText(value) ?? "redacted";
    }

    public bool ContainsSensitiveSignal(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (CanonicalProductionRedaction.ContainsSensitivePathSignal(value)) return true;

        var lowercased = value.ToLowerInvariant();
        var sensitiveTokens = new[]
        {
            "api_key", "apikey", "secret", "shared secret", "token=", "bearer ",
            "fingerprint", "private-key", "private key", "request body", "response body",
            "metadata json", "transcript", "provider response",
            "standalone note content", "note content", "summary content"
        };
        if (sensitiveTokens.Any(t => lowercased.Contains(t))) return true;

        int hexRun = 0;
        foreach (char c in lowercased)
        {
            if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))
            {
                hexRun++;
                if (hexRun >= 32) return true;
            }
            else hexRun = 0;
        }
        return false;
    }
}

public sealed class CanonicalLibraryMetadataN1EvidenceSource : IEquatable<CanonicalLibraryMetadataN1EvidenceSource>
{
    public string? SourceReportID { get; set; }
    public string? SourceDiagnosticsID { get; set; }
    public CanonicalLibraryMetadataLandingReport? LandingReport { get; set; }
    public CanonicalLibraryMetadataRealCanaryObservationReport? ObservationReport { get; set; }
    public CanonicalLibraryMetadataProductionRootSafetyProof? ProductionRootSafetyProof { get; set; }
    public CanonicalMigrationLandingFreezeResult? LandingFreezeResult { get; set; }
    public List<CanonicalLibraryMetadataCutoverDiagnostic> Diagnostics { get; set; }
    public bool ValidNoChangeOrNoEligibleEvidenceAccepted { get; set; }
    public bool RouteBoundaryViolationReported { get; set; }
    public bool ReceiveJSONMutationReported { get; set; }
    public bool GeneratedArtifactWriteReported { get; set; }
    public bool AudioChangeReported { get; set; }
    public bool StandaloneNoteContentWriteReported { get; set; }
    public bool TombstoneDeleteReported { get; set; }
    public bool DefaultCutoverEnabledReported { get; set; }
    public bool ReleaseDefaultEnabledReported { get; set; }
    public bool SensitiveDataLeakReported { get; set; }
    public bool N3ExecutionReported { get; set; }
    public bool ManualAuditRequired { get; set; }
    public bool OwnerApprovalRequiredForNextStage { get; set; }
    public bool N3DisabledByDefault { get; set; }

    public CanonicalLibraryMetadataN1EvidenceSource(
        string? sourceReportID = null,
        string? sourceDiagnosticsID = null,
        CanonicalLibraryMetadataLandingReport? landingReport = null,
        CanonicalLibraryMetadataRealCanaryObservationReport? observationReport = null,
        CanonicalLibraryMetadataProductionRootSafetyProof? productionRootSafetyProof = null,
        CanonicalMigrationLandingFreezeResult? landingFreezeResult = null,
        List<CanonicalLibraryMetadataCutoverDiagnostic>? diagnostics = null,
        bool validNoChangeOrNoEligibleEvidenceAccepted = false,
        bool routeBoundaryViolationReported = false,
        bool receiveJSONMutationReported = false,
        bool generatedArtifactWriteReported = false,
        bool audioChangeReported = false,
        bool standaloneNoteContentWriteReported = false,
        bool tombstoneDeleteReported = false,
        bool defaultCutoverEnabledReported = false,
        bool releaseDefaultEnabledReported = false,
        bool sensitiveDataLeakReported = false,
        bool n3ExecutionReported = false,
        bool manualAuditRequired = true,
        bool ownerApprovalRequiredForNextStage = true,
        bool n3DisabledByDefault = true)
    {
        var redactor = new CanonicalLibraryMetadataN1EvidenceRedactor();
        var rawSensitiveDataLeak = sensitiveDataLeakReported
            || redactor.ContainsSensitiveSignal(sourceReportID)
            || redactor.ContainsSensitiveSignal(sourceDiagnosticsID)
            || redactor.ContainsSensitiveSignal(landingReport?.DiagnosticsSummary)
            || redactor.ContainsSensitiveSignal(observationReport?.DiagnosticsSummary)
            || redactor.ContainsSensitiveSignal(observationReport?.Reason)
            || redactor.ContainsSensitiveSignal(productionRootSafetyProof?.RedactedTargetSummary)
            || redactor.ContainsSensitiveSignal(landingFreezeResult?.DiagnosticsSummary)
            || (diagnostics?.Any(d =>
                redactor.ContainsSensitiveSignal(d.DiagnosticsSummary) ||
                redactor.ContainsSensitiveSignal(d.Reason) ||
                redactor.ContainsSensitiveSignal(d.Result) ||
                redactor.ContainsSensitiveSignal(d.HashPrefix)) ?? false);

        SourceReportID = sourceReportID != null ? redactor.Redact(sourceReportID) : null;
        SourceDiagnosticsID = sourceDiagnosticsID != null ? redactor.Redact(sourceDiagnosticsID) : null;

        LandingReport = landingReport != null ? SanitizeLandingReport(landingReport, redactor) : null;
        ObservationReport = observationReport != null ? SanitizeObservationReport(observationReport, redactor) : null;
        ProductionRootSafetyProof = productionRootSafetyProof != null ? SanitizeSafetyProof(productionRootSafetyProof, redactor) : null;
        LandingFreezeResult = landingFreezeResult != null ? SanitizeFreeze(landingFreezeResult, redactor) : null;

        Diagnostics = (diagnostics ?? new List<CanonicalLibraryMetadataCutoverDiagnostic>())
            .Select(d => new CanonicalLibraryMetadataCutoverDiagnostic(
                d.Kind, d.SyncRunID, d.Trigger, d.NodeRole,
                d.Domain, d.ObjectID, d.ObjectKind, d.Action,
                d.Result, redactor.Redact(d.Reason)))
            .ToList();

        ValidNoChangeOrNoEligibleEvidenceAccepted = validNoChangeOrNoEligibleEvidenceAccepted;
        RouteBoundaryViolationReported = routeBoundaryViolationReported;
        ReceiveJSONMutationReported = receiveJSONMutationReported;
        GeneratedArtifactWriteReported = generatedArtifactWriteReported;
        AudioChangeReported = audioChangeReported;
        StandaloneNoteContentWriteReported = standaloneNoteContentWriteReported;
        TombstoneDeleteReported = tombstoneDeleteReported;
        DefaultCutoverEnabledReported = defaultCutoverEnabledReported;
        ReleaseDefaultEnabledReported = releaseDefaultEnabledReported;
        SensitiveDataLeakReported = rawSensitiveDataLeak;
        N3ExecutionReported = n3ExecutionReported;
        ManualAuditRequired = manualAuditRequired;
        OwnerApprovalRequiredForNextStage = ownerApprovalRequiredForNextStage;
        N3DisabledByDefault = n3DisabledByDefault;
    }

    private static CanonicalLibraryMetadataLandingReport SanitizeLandingReport(CanonicalLibraryMetadataLandingReport report, CanonicalLibraryMetadataN1EvidenceRedactor r)
    {
        report.Blockers = report.Blockers.Select(b => r.Redact(b)).ToList();
        report.DiagnosticsSummary = r.Redact(report.DiagnosticsSummary);
        return report;
    }

    private static CanonicalLibraryMetadataRealCanaryObservationReport SanitizeObservationReport(CanonicalLibraryMetadataRealCanaryObservationReport report, CanonicalLibraryMetadataN1EvidenceRedactor r)
    {
        report.Reason = r.Redact(report.Reason);
        return report;
    }

    private static CanonicalLibraryMetadataProductionRootSafetyProof SanitizeSafetyProof(CanonicalLibraryMetadataProductionRootSafetyProof proof, CanonicalLibraryMetadataN1EvidenceRedactor r)
    {
        proof.RedactedTargetSummary = r.Redact(proof.RedactedTargetSummary);
        return proof;
    }

    private static CanonicalMigrationLandingFreezeResult SanitizeFreeze(CanonicalMigrationLandingFreezeResult result, CanonicalLibraryMetadataN1EvidenceRedactor r)
    {
        result.DiagnosticsSummary = r.Redact(result.DiagnosticsSummary);
        return result;
    }

    public bool HasEvidence =>
        LandingReport != null || ObservationReport != null || ProductionRootSafetyProof != null ||
        LandingFreezeResult != null || Diagnostics.Count > 0;

    public bool DiagnosticsContainSensitiveData
    {
        get
        {
            if (SensitiveDataLeakReported) return true;
            var redactor = new CanonicalLibraryMetadataN1EvidenceRedactor();
            if (redactor.ContainsSensitiveSignal(SourceReportID) || redactor.ContainsSensitiveSignal(SourceDiagnosticsID))
                return true;
            foreach (var d in Diagnostics)
            {
                if (redactor.ContainsSensitiveSignal(d.DiagnosticsSummary) ||
                    redactor.ContainsSensitiveSignal(d.Reason) ||
                    redactor.ContainsSensitiveSignal(d.Result) ||
                    redactor.ContainsSensitiveSignal(d.HashPrefix))
                    return true;
            }
            if (redactor.ContainsSensitiveSignal(LandingReport?.DiagnosticsSummary) ||
                redactor.ContainsSensitiveSignal(ObservationReport?.DiagnosticsSummary) ||
                redactor.ContainsSensitiveSignal(ProductionRootSafetyProof?.RedactedTargetSummary) ||
                redactor.ContainsSensitiveSignal(LandingFreezeResult?.DiagnosticsSummary))
                return true;
            return false;
        }
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataN1EvidenceSource other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataN1EvidenceSource? other) =>
        other is not null && SourceReportID == other.SourceReportID &&
        SourceDiagnosticsID == other.SourceDiagnosticsID &&
        EqualityComparer<CanonicalLibraryMetadataLandingReport?>.Default.Equals(LandingReport, other.LandingReport) &&
        EqualityComparer<CanonicalLibraryMetadataRealCanaryObservationReport?>.Default.Equals(ObservationReport, other.ObservationReport) &&
        EqualityComparer<CanonicalLibraryMetadataProductionRootSafetyProof?>.Default.Equals(ProductionRootSafetyProof, other.ProductionRootSafetyProof) &&
        EqualityComparer<CanonicalMigrationLandingFreezeResult?>.Default.Equals(LandingFreezeResult, other.LandingFreezeResult) &&
        Diagnostics.SequenceEqual(other.Diagnostics);
    public override int GetHashCode() =>
        HashCode.Combine(SourceReportID, SourceDiagnosticsID, LandingReport, ObservationReport,
            ProductionRootSafetyProof, LandingFreezeResult, Diagnostics.Count);
    public static bool operator ==(CanonicalLibraryMetadataN1EvidenceSource left, CanonicalLibraryMetadataN1EvidenceSource right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataN1EvidenceSource left, CanonicalLibraryMetadataN1EvidenceSource right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataN1EvidenceValidationResult : IEquatable<CanonicalLibraryMetadataN1EvidenceValidationResult>
{
    public CanonicalLibraryMetadataN1EvidenceStatus Status { get; set; }
    public List<CanonicalLibraryMetadataN1EvidenceBlocker> Blockers { get; set; }
    public string DiagnosticsSummary { get; set; }
    public bool Redacted { get; set; }

    public CanonicalLibraryMetadataN1EvidenceValidationResult(
        CanonicalLibraryMetadataN1EvidenceStatus status = CanonicalLibraryMetadataN1EvidenceStatus.missing,
        List<CanonicalLibraryMetadataN1EvidenceBlocker>? blockers = null,
        string diagnosticsSummary = "",
        bool redacted = true)
    {
        Status = status;
        Blockers = blockers ?? new List<CanonicalLibraryMetadataN1EvidenceBlocker>();
        DiagnosticsSummary = diagnosticsSummary;
        Redacted = redacted;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataN1EvidenceValidationResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataN1EvidenceValidationResult? other) =>
        other is not null && Status == other.Status && Blockers.SequenceEqual(other.Blockers) &&
        DiagnosticsSummary == other.DiagnosticsSummary && Redacted == other.Redacted;
    public override int GetHashCode() => HashCode.Combine(Status, Blockers.Count, DiagnosticsSummary, Redacted);
    public static bool operator ==(CanonicalLibraryMetadataN1EvidenceValidationResult left, CanonicalLibraryMetadataN1EvidenceValidationResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataN1EvidenceValidationResult left, CanonicalLibraryMetadataN1EvidenceValidationResult right) => !left.Equals(right);
}

public sealed class CanonicalLibraryMetadataN1EvidenceBundle : IEquatable<CanonicalLibraryMetadataN1EvidenceBundle>
{
    public CanonicalLibraryMetadataN1EvidenceStatus Status { get; set; }
    public CanonicalLibraryMetadataDebugPilotMode? LandingMode { get; set; }
    public CanonicalLibraryMetadataProductionCanaryRootMode? RootMode { get; set; }
    public CanonicalMigrationDomain? ActivePilot { get; set; }
    public int SelectedCandidateCount { get; set; }
    public int ExecutedCandidateCount { get; set; }
    public CanonicalLibraryMetadataCanaryCandidateSafetyKind? SelectedCandidateKind { get; set; }
    public CanonicalLibraryMetadataCutoverDomain? SelectedCandidateDomain { get; set; }
    public bool CanaryAttempted { get; set; }
    public bool CanarySucceeded { get; set; }
    public int CommitSuccessCount { get; set; }
    public bool RollbackAttempted { get; set; }
    public bool RollbackSucceeded { get; set; }
    public int RollbackFailureCount { get; set; }
    public bool LegacyFallbackUsed { get; set; }
    public bool LegacyFallbackAvailable { get; set; }
    public int DuplicateSuppressionCount { get; set; }
    public CanonicalLibraryMetadataN1EvidenceStatus ReadSideEquivalenceStatus { get; set; }
    public bool ReadSideParallelExecuted { get; set; }
    public int ReadSideDivergenceCount { get; set; }
    public CanonicalLibraryMetadataN1EvidenceStatus ProductionRootSafetyProofStatus { get; set; }
    public CanonicalLibraryMetadataN1EvidenceStatus LandingFreezeStatus { get; set; }
    public CanonicalLibraryMetadataN1EvidenceStatus OtherDomainsStaticOnlyStatus { get; set; }
    public CanonicalLibraryMetadataN1EvidenceStatus RuntimeSwitchStatus { get; set; }
    public CanonicalLibraryMetadataN1EvidenceStatus ReleaseDefaultDisabledStatus { get; set; }
    public CanonicalLibraryMetadataN1EvidenceStatus DiagnosticsRedactedStatus { get; set; }
    public int ResourceMoveCount { get; set; }
    public int ContentWriteCount { get; set; }
    public int GeneratedArtifactWriteCount { get; set; }
    public int AudioChangeCount { get; set; }
    public int TombstoneDeleteCount { get; set; }
    public int UnsafeCandidateExecutionCount { get; set; }
    public bool ReadPathSwitched { get; set; }
    public bool UiMutated { get; set; }
    public bool RouteBoundaryViolationReported { get; set; }
    public bool ReceiveJSONMutationReported { get; set; }
    public bool ValidNoChangeOrNoEligibleEvidenceAccepted { get; set; }
    public bool N3ExecutionReported { get; set; }
    public bool ManualAuditRequired { get; set; }
    public bool OwnerApprovalRequiredForNextStage { get; set; }
    public bool N3DisabledByDefault { get; set; }
    public CanonicalLibraryMetadataN1EvidenceSource Source { get; set; }
    public string? SourceReportID { get; set; }
    public string? SourceDiagnosticsID { get; set; }
    public List<CanonicalLibraryMetadataN1EvidenceBlocker> Blockers { get; set; }
    public string DiagnosticsSummary { get; set; }
    public bool Redacted { get; set; }

    public CanonicalLibraryMetadataN1EvidenceBundle(CanonicalLibraryMetadataN1EvidenceSource source)
    {
        Source = source;
        SourceReportID = source.SourceReportID;
        SourceDiagnosticsID = source.SourceDiagnosticsID;
        LandingMode = source.LandingReport?.Mode;
        RootMode = source.LandingReport?.RootMode ?? source.ObservationReport?.RootMode;
        ActivePilot = source.LandingReport?.ActivePilot ?? source.ObservationReport?.Domain;
        SelectedCandidateCount = source.LandingReport is { } lr ? (lr.Candidate.Selected ? 1 : 0) : source.ObservationReport?.SelectedCandidateCount ?? 0;
        ExecutedCandidateCount = source.ObservationReport?.ExecutedCandidateCount ?? (source.LandingReport?.CommitAttempted == true ? 1 : 0);
        SelectedCandidateKind = source.LandingReport?.Candidate.Kind;
        SelectedCandidateDomain = source.LandingReport?.Candidate.Domain;
        CanaryAttempted = source.LandingReport?.CommitAttempted ?? (source.ObservationReport?.ProductionRootWriteAttempted == true || (source.ObservationReport?.ExecutedCandidateCount ?? 0) > 0);
        CanarySucceeded = source.LandingReport?.CommitSucceeded ?? ((source.ObservationReport?.SuccessfulCommitCount ?? 0) > 0);
        CommitSuccessCount = Math.Max(0, source.ObservationReport?.SuccessfulCommitCount ?? (source.LandingReport?.CommitSucceeded == true ? 1 : 0));
        RollbackAttempted = source.LandingReport?.RollbackAttempted ?? ((source.ObservationReport?.RollbackCount ?? 0) > 0);
        RollbackFailureCount = Math.Max(0, source.ObservationReport?.RollbackFailureCount ?? 0);
        RollbackSucceeded = source.LandingReport?.RollbackSucceeded ?? ((source.ObservationReport?.RollbackCount ?? 0) > 0 && RollbackFailureCount == 0);
        LegacyFallbackUsed = source.LandingReport?.LegacyFallbackUsed ?? ((source.ObservationReport?.LegacyFallbackCount ?? 0) > 0);
        LegacyFallbackAvailable = source.LandingReport?.LegacyReadPathPreserved ?? source.ObservationReport?.LegacyFallbackPreserved ?? false;
        DuplicateSuppressionCount = Math.Max(0, source.LandingReport?.DuplicateSuppressedCount ?? source.ObservationReport?.DuplicateSuppressionCount ?? 0);

        var readEquivalent = source.LandingReport?.ReadSideEquivalent ?? source.ObservationReport?.ReadSideParallelEquivalent;
        var readDivergence = Math.Max(0, source.LandingReport?.ReadSideDivergenceCount ?? (source.ObservationReport?.ReadSideParallelDivergent == true ? 1 : 0));
        ReadSideEquivalenceStatus = readEquivalent == null ? CanonicalLibraryMetadataN1EvidenceStatus.missing
            : (readEquivalent == true && readDivergence == 0 ? CanonicalLibraryMetadataN1EvidenceStatus.valid : CanonicalLibraryMetadataN1EvidenceStatus.invalid);
        ReadSideParallelExecuted = readEquivalent != null;
        ReadSideDivergenceCount = readDivergence;

        if (source.ProductionRootSafetyProof is { } proof)
        {
            ProductionRootSafetyProofStatus = proof.Redacted && proof.RootContainmentVerified &&
                proof.ProductionRootModeExplicit && proof.LogicalTokenSafety &&
                proof.AtomicWriteUsed && proof.PostconditionVerified &&
                proof.RollbackAvailable && proof.RollbackVerifiedIfUsed &&
                proof.SideEffectWhitelistPassed && proof.NoResourceMove &&
                proof.NoContentWrite && proof.NoOtherDomainMutation
                ? CanonicalLibraryMetadataN1EvidenceStatus.valid : CanonicalLibraryMetadataN1EvidenceStatus.invalid;
        }
        else ProductionRootSafetyProofStatus = CanonicalLibraryMetadataN1EvidenceStatus.missing;

        if (source.LandingFreezeResult is { } freeze)
        {
            LandingFreezeStatus = freeze.Allowed ? CanonicalLibraryMetadataN1EvidenceStatus.valid : CanonicalLibraryMetadataN1EvidenceStatus.blocked;
            OtherDomainsStaticOnlyStatus = freeze.OtherDomainsStaticOnly ? CanonicalLibraryMetadataN1EvidenceStatus.valid : CanonicalLibraryMetadataN1EvidenceStatus.invalid;
            RuntimeSwitchStatus = freeze.RuntimeSwitchEnabled ? CanonicalLibraryMetadataN1EvidenceStatus.invalid : CanonicalLibraryMetadataN1EvidenceStatus.valid;
        }
        else if (source.LandingReport is { } report)
        {
            LandingFreezeStatus = report.FreezeViolations.Count == 0 ? CanonicalLibraryMetadataN1EvidenceStatus.valid : CanonicalLibraryMetadataN1EvidenceStatus.blocked;
            OtherDomainsStaticOnlyStatus = report.OtherDomainsStaticOnly ? CanonicalLibraryMetadataN1EvidenceStatus.valid : CanonicalLibraryMetadataN1EvidenceStatus.invalid;
            RuntimeSwitchStatus = report.RuntimeSwitchEnabled ? CanonicalLibraryMetadataN1EvidenceStatus.invalid : CanonicalLibraryMetadataN1EvidenceStatus.valid;
        }
        else
        {
            LandingFreezeStatus = CanonicalLibraryMetadataN1EvidenceStatus.missing;
            OtherDomainsStaticOnlyStatus = CanonicalLibraryMetadataN1EvidenceStatus.missing;
            RuntimeSwitchStatus = CanonicalLibraryMetadataN1EvidenceStatus.missing;
        }

        var releaseDefaultEnabled = source.DefaultCutoverEnabledReported || source.ReleaseDefaultEnabledReported;
        ReleaseDefaultDisabledStatus = releaseDefaultEnabled ? CanonicalLibraryMetadataN1EvidenceStatus.invalid : CanonicalLibraryMetadataN1EvidenceStatus.valid;
        DiagnosticsRedactedStatus = source.DiagnosticsContainSensitiveData ? CanonicalLibraryMetadataN1EvidenceStatus.invalid : CanonicalLibraryMetadataN1EvidenceStatus.valid;
        ResourceMoveCount = (source.LandingReport?.Candidate.ResourceMoveAttempted == true || source.ObservationReport?.ResourceMoved == true) ? 1 : 0;
        ContentWriteCount = (source.LandingReport?.Candidate.ContentBytesMutated == true || source.StandaloneNoteContentWriteReported) ? 1 : 0;
        GeneratedArtifactWriteCount = source.GeneratedArtifactWriteReported ? 1 : 0;
        AudioChangeCount = source.AudioChangeReported ? 1 : 0;
        TombstoneDeleteCount = source.TombstoneDeleteReported ? 1 : 0;
        UnsafeCandidateExecutionCount = (ResourceMoveCount + ContentWriteCount + GeneratedArtifactWriteCount + AudioChangeCount + TombstoneDeleteCount) > 0 && CanaryAttempted ? 1 : 0;
        ReadPathSwitched = source.LandingReport?.UiReadPathSwitched == true;
        UiMutated = source.ObservationReport?.UiMutated == true;
        RouteBoundaryViolationReported = source.RouteBoundaryViolationReported;
        ReceiveJSONMutationReported = source.ReceiveJSONMutationReported;
        ValidNoChangeOrNoEligibleEvidenceAccepted = source.ValidNoChangeOrNoEligibleEvidenceAccepted;
        N3ExecutionReported = source.N3ExecutionReported;
        ManualAuditRequired = source.ManualAuditRequired;
        OwnerApprovalRequiredForNextStage = source.OwnerApprovalRequiredForNextStage;
        N3DisabledByDefault = source.N3DisabledByDefault;

        var validation = Validate();
        Status = validation.Status;
        Blockers = validation.Blockers;
        DiagnosticsSummary = validation.DiagnosticsSummary;
        Redacted = validation.Redacted;
    }

    private CanonicalLibraryMetadataN1EvidenceValidationResult Validate()
    {
        var blockers = new List<CanonicalLibraryMetadataN1EvidenceBlocker>();
        if (!Source.HasEvidence) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.missingN1Evidence);
        if (ActivePilot != null && ActivePilot != CanonicalMigrationDomain.libraryMetadata) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.otherActiveDomain);
        if (SelectedCandidateCount > 1) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.invalidEvidence);
        if (ExecutedCandidateCount > 1) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.invalidEvidence);
        if (CanaryAttempted && SelectedCandidateCount == 1 && SelectedCandidateDomain == null && SelectedCandidateKind == null)
            blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.invalidEvidence);
        if (SelectedCandidateKind == CanonicalLibraryMetadataCanaryCandidateSafetyKind.blocked) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.unsafeCandidateKind);
        if (ReadSideDivergenceCount > 0 || ReadSideEquivalenceStatus == CanonicalLibraryMetadataN1EvidenceStatus.invalid) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.readSideDivergence);
        if (RollbackFailureCount > 0 || (RollbackAttempted && !RollbackSucceeded)) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.rollbackFailure);
        if (DuplicateSuppressionCount > 0 && CommitSuccessCount == 0) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.duplicateSuppressionWithoutCommitSuccess);
        if (ProductionRootSafetyProofStatus == CanonicalLibraryMetadataN1EvidenceStatus.missing) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.productionRootSafetyProofMissing);
        else if (ProductionRootSafetyProofStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.productionRootSafetyProofInvalid);
        if (LandingFreezeStatus == CanonicalLibraryMetadataN1EvidenceStatus.missing) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.landingFreezeMissing);
        else if (LandingFreezeStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.landingFreezeBlocked);
        if (OtherDomainsStaticOnlyStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.otherDomainsNotStaticOnly);
        if (RuntimeSwitchStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.runtimeSwitchEnabled);
        if (ReleaseDefaultDisabledStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.releaseDefaultEnabled);
        if (DiagnosticsRedactedStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.sensitiveDataLeak);
        if (ResourceMoveCount > 0) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.resourceMove);
        if (ContentWriteCount > 0) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.contentWrite);
        if (GeneratedArtifactWriteCount > 0) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.generatedArtifactWrite);
        if (AudioChangeCount > 0) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.audioChange);
        if (TombstoneDeleteCount > 0) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.tombstoneDelete);
        if (UnsafeCandidateExecutionCount > 0) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.unsafeCandidateExecuted);
        if (ReadPathSwitched) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.readPathSwitched);
        if (UiMutated) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.uiMutated);
        if (!LegacyFallbackAvailable) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.legacyFallbackUnavailable);
        if (RouteBoundaryViolationReported) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.requestVerifierRouteBoundaryViolation);
        if (ReceiveJSONMutationReported) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.receiveJSONMutation);
        if (N3ExecutionReported) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.n3ExecutionReported);
        if (!ManualAuditRequired) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.manualAuditMissing);
        if (!OwnerApprovalRequiredForNextStage) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.ownerApprovalMissing);
        if (!N3DisabledByDefault) blockers.Add(CanonicalLibraryMetadataN1EvidenceBlocker.n3DisabledByDefaultMissing);

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataN1EvidenceBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        var status = uniqueBlockers switch
        {
            var b when b.Contains(CanonicalLibraryMetadataN1EvidenceBlocker.missingN1Evidence) => CanonicalLibraryMetadataN1EvidenceStatus.missing,
            var b when b.Contains(CanonicalLibraryMetadataN1EvidenceBlocker.sensitiveDataLeak) ||
                         b.Contains(CanonicalLibraryMetadataN1EvidenceBlocker.duplicateSuppressionWithoutCommitSuccess) ||
                         b.Contains(CanonicalLibraryMetadataN1EvidenceBlocker.invalidEvidence) ||
                         b.Contains(CanonicalLibraryMetadataN1EvidenceBlocker.requestVerifierRouteBoundaryViolation) ||
                         b.Contains(CanonicalLibraryMetadataN1EvidenceBlocker.receiveJSONMutation) => CanonicalLibraryMetadataN1EvidenceStatus.invalid,
            var b when b.Contains(CanonicalLibraryMetadataN1EvidenceBlocker.productionRootSafetyProofMissing) ||
                         b.Contains(CanonicalLibraryMetadataN1EvidenceBlocker.landingFreezeMissing) ||
                         ReadSideEquivalenceStatus == CanonicalLibraryMetadataN1EvidenceStatus.missing => CanonicalLibraryMetadataN1EvidenceStatus.insufficient,
            var b when b.Count == 0 => CanonicalLibraryMetadataN1EvidenceStatus.valid,
            _ => CanonicalLibraryMetadataN1EvidenceStatus.blocked
        };

        return new CanonicalLibraryMetadataN1EvidenceValidationResult(status, uniqueBlockers,
            string.Join(",", $"status={status}", $"mode={LandingMode?.ToString() ?? "none"}",
                $"rootMode={RootMode?.ToString() ?? "none"}", $"activePilot={ActivePilot?.ToString() ?? "none"}",
                $"selected={SelectedCandidateCount}", $"executed={ExecutedCandidateCount}",
                $"commitSuccess={CommitSuccessCount}", $"rollbackFailure={RollbackFailureCount}",
                $"readSideDivergence={ReadSideDivergenceCount}", $"duplicateSuppression={DuplicateSuppressionCount}",
                $"runtimeSwitchStatus={RuntimeSwitchStatus}", $"releaseDefaultStatus={ReleaseDefaultDisabledStatus}",
                $"blockers={string.Join("|", uniqueBlockers.Select(b => b.ToString()))}", "redacted=true"),
            true);
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataN1EvidenceBundle other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataN1EvidenceBundle? other) =>
        other is not null && Status == other.Status && LandingMode == other.LandingMode &&
        RootMode == other.RootMode && ActivePilot == other.ActivePilot &&
        SelectedCandidateCount == other.SelectedCandidateCount && ExecutedCandidateCount == other.ExecutedCandidateCount &&
        SelectedCandidateKind == other.SelectedCandidateKind && SelectedCandidateDomain == other.SelectedCandidateDomain &&
        CanaryAttempted == other.CanaryAttempted && CanarySucceeded == other.CanarySucceeded &&
        CommitSuccessCount == other.CommitSuccessCount && RollbackAttempted == other.RollbackAttempted &&
        RollbackSucceeded == other.RollbackSucceeded && RollbackFailureCount == other.RollbackFailureCount &&
        LegacyFallbackUsed == other.LegacyFallbackUsed && LegacyFallbackAvailable == other.LegacyFallbackAvailable &&
        DuplicateSuppressionCount == other.DuplicateSuppressionCount && ReadSideEquivalenceStatus == other.ReadSideEquivalenceStatus &&
        ReadSideParallelExecuted == other.ReadSideParallelExecuted && ReadSideDivergenceCount == other.ReadSideDivergenceCount &&
        ProductionRootSafetyProofStatus == other.ProductionRootSafetyProofStatus &&
        LandingFreezeStatus == other.LandingFreezeStatus && OtherDomainsStaticOnlyStatus == other.OtherDomainsStaticOnlyStatus &&
        RuntimeSwitchStatus == other.RuntimeSwitchStatus && ReleaseDefaultDisabledStatus == other.ReleaseDefaultDisabledStatus &&
        DiagnosticsRedactedStatus == other.DiagnosticsRedactedStatus &&
        ResourceMoveCount == other.ResourceMoveCount && ContentWriteCount == other.ContentWriteCount &&
        GeneratedArtifactWriteCount == other.GeneratedArtifactWriteCount && AudioChangeCount == other.AudioChangeCount &&
        TombstoneDeleteCount == other.TombstoneDeleteCount && UnsafeCandidateExecutionCount == other.UnsafeCandidateExecutionCount &&
        ReadPathSwitched == other.ReadPathSwitched && UiMutated == other.UiMutated &&
        RouteBoundaryViolationReported == other.RouteBoundaryViolationReported &&
        ReceiveJSONMutationReported == other.ReceiveJSONMutationReported &&
        ValidNoChangeOrNoEligibleEvidenceAccepted == other.ValidNoChangeOrNoEligibleEvidenceAccepted &&
        N3ExecutionReported == other.N3ExecutionReported && ManualAuditRequired == other.ManualAuditRequired &&
        OwnerApprovalRequiredForNextStage == other.OwnerApprovalRequiredForNextStage &&
        N3DisabledByDefault == other.N3DisabledByDefault &&
        EqualityComparer<CanonicalLibraryMetadataN1EvidenceSource>.Default.Equals(Source, other.Source) &&
        SourceReportID == other.SourceReportID && SourceDiagnosticsID == other.SourceDiagnosticsID &&
        Blockers.SequenceEqual(other.Blockers) && DiagnosticsSummary == other.DiagnosticsSummary && Redacted == other.Redacted;
    public override int GetHashCode() => HashCode.Combine(Status, LandingMode, RootMode, ActivePilot, SelectedCandidateCount,
        ExecutedCandidateCount, SelectedCandidateKind, SelectedCandidateDomain, CanaryAttempted, CanarySucceeded,
        CommitSuccessCount, RollbackAttempted, RollbackSucceeded, RollbackFailureCount, LegacyFallbackUsed,
        LegacyFallbackAvailable, DuplicateSuppressionCount, ReadSideEquivalenceStatus, ReadSideParallelExecuted,
        ReadSideDivergenceCount, ProductionRootSafetyProofStatus, LandingFreezeStatus, OtherDomainsStaticOnlyStatus,
        RuntimeSwitchStatus, ReleaseDefaultDisabledStatus, DiagnosticsRedactedStatus, ResourceMoveCount,
        ContentWriteCount, GeneratedArtifactWriteCount, AudioChangeCount, TombstoneDeleteCount,
        UnsafeCandidateExecutionCount, ReadPathSwitched, UiMutated, RouteBoundaryViolationReported,
        ReceiveJSONMutationReported, ValidNoChangeOrNoEligibleEvidenceAccepted, N3ExecutionReported,
        ManualAuditRequired, OwnerApprovalRequiredForNextStage, N3DisabledByDefault, Source, SourceReportID,
        SourceDiagnosticsID, Blockers.Count, DiagnosticsSummary, Redacted);
    public static bool operator ==(CanonicalLibraryMetadataN1EvidenceBundle left, CanonicalLibraryMetadataN1EvidenceBundle right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataN1EvidenceBundle left, CanonicalLibraryMetadataN1EvidenceBundle right) => !left.Equals(right);
}

public class CanonicalLibraryMetadataN1EvidenceImporter
{
    public CanonicalLibraryMetadataN1EvidenceBundle ImportEvidence(CanonicalLibraryMetadataN1EvidenceSource? source) =>
        new(source ?? new CanonicalLibraryMetadataN1EvidenceSource());
}

public static class CanonicalLibraryMetadataProductionRootSafetyProofExtensions
{
    public static CanonicalLibraryMetadataProductionRootSafetyProof Create(
        bool rootContainmentVerified, bool productionRootModeExplicit, bool logicalTokenSafety,
        string? checkpointID, bool atomicWriteUsed, bool postconditionVerified, bool rollbackAvailable,
        bool rollbackVerifiedIfUsed, bool sideEffectWhitelistPassed, bool noResourceMove,
        bool noContentWrite, bool noOtherDomainMutation, string redactedTargetSummary, bool redacted)
    {
        return new CanonicalLibraryMetadataProductionRootSafetyProof(
            rootContainmentVerified, productionRootModeExplicit, logicalTokenSafety,
            checkpointID != null ? CanonicalProductionRedaction.SafeIdentifier(checkpointID, "library-metadata-checkpoint") : null,
            atomicWriteUsed, postconditionVerified, rollbackAvailable, rollbackVerifiedIfUsed,
            sideEffectWhitelistPassed, noResourceMove, noContentWrite, noOtherDomainMutation,
            new CanonicalLibraryMetadataN1EvidenceRedactor().Redact(redactedTargetSummary), redacted);
    }

    public static CanonicalLibraryMetadataProductionRootSafetyProof RedactedValidN1(
        string checkpointID = "library-metadata-n1-checkpoint",
        CanonicalLibraryMetadataCanaryCandidateSafetyKind candidateKind = CanonicalLibraryMetadataCanaryCandidateSafetyKind.folderRenameOrColorMetadata,
        CanonicalObjectKind objectKind = CanonicalObjectKind.folder,
        CanonicalLibraryMetadataCutoverDomain domain = CanonicalLibraryMetadataCutoverDomain.folderMetadata) =>
        Create(true, true, true,
            checkpointID, true, true, true, true, true, true, true, true,
            string.Join(",", $"domain={domain}", $"objectKind={objectKind}", $"candidateKind={candidateKind}", "hashPrefix=fixture"),
            true);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataN1PostRunInvariant
{
    candidateCountAtMostOne,
    candidateDomainLibraryMetadata,
    candidateKindSafeMetadataOnly,
    noResourceMove,
    noStandaloneNoteContentWrite,
    noTombstoneDelete,
    noGeneratedArtifactWrite,
    noAudioChange,
    noReadPathSwitch,
    uiNotMutated,
    runtimeSwitchFalse,
    legacyFallbackAvailable,
    readSideParallelExecuted,
    readSideNoDivergence,
    rollbackFailureZero,
    duplicateSuppressionRequiresCommitSuccess,
    otherDomainsStaticOnly,
    defaultReleaseDisabled,
    requestVerifierRouteBoundaryPreserved,
    receiveJSONUnchanged,
}

public sealed class CanonicalLibraryMetadataN1PostRunViolation : IEquatable<CanonicalLibraryMetadataN1PostRunViolation>
{
    public string Id => Invariant.ToString();
    public CanonicalLibraryMetadataN1PostRunInvariant Invariant { get; set; }
    public string Summary { get; set; }
    public bool Redacted { get; set; }

    public CanonicalLibraryMetadataN1PostRunViolation(CanonicalLibraryMetadataN1PostRunInvariant invariant, string summary)
    {
        Invariant = invariant;
        Summary = new CanonicalLibraryMetadataN1EvidenceRedactor().Redact(summary);
        Redacted = true;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataN1PostRunViolation other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataN1PostRunViolation? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalLibraryMetadataN1PostRunViolation left, CanonicalLibraryMetadataN1PostRunViolation right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataN1PostRunViolation left, CanonicalLibraryMetadataN1PostRunViolation right) => !left.Equals(right);
}

public class CanonicalLibraryMetadataN1PostRunInvariantValidator
{
    public List<CanonicalLibraryMetadataN1PostRunViolation> Validate(CanonicalLibraryMetadataN1EvidenceBundle bundle)
    {
        var violations = new List<CanonicalLibraryMetadataN1PostRunViolation>();
        void Append(CanonicalLibraryMetadataN1PostRunInvariant i, string s) => violations.Add(new(i, s));

        if (bundle.ExecutedCandidateCount > 1 || bundle.SelectedCandidateCount > 1)
            Append(CanonicalLibraryMetadataN1PostRunInvariant.candidateCountAtMostOne, $"selectedCandidateCount={bundle.SelectedCandidateCount},executedCandidateCount={bundle.ExecutedCandidateCount}");
        if (bundle.ExecutedCandidateCount > 0 && bundle.ActivePilot != CanonicalMigrationDomain.libraryMetadata)
            Append(CanonicalLibraryMetadataN1PostRunInvariant.candidateDomainLibraryMetadata, $"activePilot={bundle.ActivePilot?.ToString() ?? "none"}");
        if (bundle.SelectedCandidateKind == CanonicalLibraryMetadataCanaryCandidateSafetyKind.blocked || bundle.UnsafeCandidateExecutionCount > 0)
            Append(CanonicalLibraryMetadataN1PostRunInvariant.candidateKindSafeMetadataOnly, $"kind={bundle.SelectedCandidateKind?.ToString() ?? "none"}");
        if (bundle.ResourceMoveCount > 0) Append(CanonicalLibraryMetadataN1PostRunInvariant.noResourceMove, $"resourceMoveCount={bundle.ResourceMoveCount}");
        if (bundle.ContentWriteCount > 0) Append(CanonicalLibraryMetadataN1PostRunInvariant.noStandaloneNoteContentWrite, $"contentWriteCount={bundle.ContentWriteCount}");
        if (bundle.TombstoneDeleteCount > 0) Append(CanonicalLibraryMetadataN1PostRunInvariant.noTombstoneDelete, $"tombstoneDeleteCount={bundle.TombstoneDeleteCount}");
        if (bundle.GeneratedArtifactWriteCount > 0) Append(CanonicalLibraryMetadataN1PostRunInvariant.noGeneratedArtifactWrite, $"generatedArtifactWriteCount={bundle.GeneratedArtifactWriteCount}");
        if (bundle.AudioChangeCount > 0) Append(CanonicalLibraryMetadataN1PostRunInvariant.noAudioChange, $"audioChangeCount={bundle.AudioChangeCount}");
        if (bundle.ReadPathSwitched) Append(CanonicalLibraryMetadataN1PostRunInvariant.noReadPathSwitch, "readPathSwitched=true");
        if (bundle.UiMutated) Append(CanonicalLibraryMetadataN1PostRunInvariant.uiNotMutated, "uiMutated=true");
        if (bundle.RuntimeSwitchStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid) Append(CanonicalLibraryMetadataN1PostRunInvariant.runtimeSwitchFalse, $"runtimeSwitchStatus={bundle.RuntimeSwitchStatus}");
        if (!bundle.LegacyFallbackAvailable) Append(CanonicalLibraryMetadataN1PostRunInvariant.legacyFallbackAvailable, "legacyFallbackAvailable=false");
        if (!bundle.ReadSideParallelExecuted) Append(CanonicalLibraryMetadataN1PostRunInvariant.readSideParallelExecuted, "readSideParallelExecuted=false");
        if (bundle.ReadSideDivergenceCount > 0) Append(CanonicalLibraryMetadataN1PostRunInvariant.readSideNoDivergence, $"readSideDivergenceCount={bundle.ReadSideDivergenceCount}");
        if (bundle.RollbackFailureCount > 0) Append(CanonicalLibraryMetadataN1PostRunInvariant.rollbackFailureZero, $"rollbackFailureCount={bundle.RollbackFailureCount}");
        if (bundle.DuplicateSuppressionCount > 0 && bundle.CommitSuccessCount == 0)
            Append(CanonicalLibraryMetadataN1PostRunInvariant.duplicateSuppressionRequiresCommitSuccess, "duplicateSuppressionWithoutCommitSuccess");
        if (bundle.OtherDomainsStaticOnlyStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid)
            Append(CanonicalLibraryMetadataN1PostRunInvariant.otherDomainsStaticOnly, $"otherDomainsStaticOnlyStatus={bundle.OtherDomainsStaticOnlyStatus}");
        if (bundle.ReleaseDefaultDisabledStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid)
            Append(CanonicalLibraryMetadataN1PostRunInvariant.defaultReleaseDisabled, $"releaseDefaultDisabledStatus={bundle.ReleaseDefaultDisabledStatus}");
        if (bundle.RouteBoundaryViolationReported)
            Append(CanonicalLibraryMetadataN1PostRunInvariant.requestVerifierRouteBoundaryPreserved, "routeBoundaryViolationReported=true");
        if (bundle.ReceiveJSONMutationReported)
            Append(CanonicalLibraryMetadataN1PostRunInvariant.receiveJSONUnchanged, "receiveJSONMutationReported=true");

        return violations;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataN3ReadinessStatus
{
    readyForN3AfterManualAudit,
    blockedMissingEvidence,
    blockedInvalidEvidence,
    blockedRollbackFailure,
    blockedReadSideDivergence,
    blockedUnsafeSideEffect,
    blockedOtherDomainActive,
    blockedReleaseDefaultEnabled,
    blockedSensitiveDataLeak,
    blocked,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalLibraryMetadataN3ReadinessBlocker
{
    missingEvidence,
    invalidEvidence,
    commitSuccessOrAcceptedNoChangeMissing,
    rollbackFailure,
    readSideDivergence,
    unsafeCandidateExecution,
    resourceMove,
    contentWrite,
    tombstoneDelete,
    generatedArtifactWrite,
    audioChange,
    routeBoundaryViolation,
    receiveJSONMutation,
    otherDomainActive,
    runtimeSwitchEnabled,
    releaseDefaultEnabled,
    legacyFallbackUnavailable,
    manualAuditRequired,
    ownerApprovalRequired,
    n3NotDisabledByDefault,
    sensitiveDataLeak,
    n3ExecutionAttempted,
}

public sealed class CanonicalLibraryMetadataN3ReadinessResult : IEquatable<CanonicalLibraryMetadataN3ReadinessResult>
{
    public CanonicalLibraryMetadataN3ReadinessStatus Status { get; set; }
    public List<CanonicalLibraryMetadataN3ReadinessBlocker> Blockers { get; set; }
    public bool ReportOnly { get; set; }
    public bool N3ExecutionAttempted { get; set; }
    public bool N3DisabledByDefault { get; set; }
    public bool ManualAuditRequired { get; set; }
    public bool OwnerApprovalRequired { get; set; }
    public string DiagnosticsSummary { get; set; }
    public bool Redacted { get; set; }

    public CanonicalLibraryMetadataN3ReadinessResult(
        CanonicalLibraryMetadataN3ReadinessStatus status = CanonicalLibraryMetadataN3ReadinessStatus.blocked,
        List<CanonicalLibraryMetadataN3ReadinessBlocker>? blockers = null,
        bool reportOnly = true,
        bool n3ExecutionAttempted = false,
        bool n3DisabledByDefault = true,
        bool manualAuditRequired = true,
        bool ownerApprovalRequired = true,
        string diagnosticsSummary = "",
        bool redacted = true)
    {
        Status = status;
        Blockers = blockers ?? new List<CanonicalLibraryMetadataN3ReadinessBlocker>();
        ReportOnly = reportOnly;
        N3ExecutionAttempted = n3ExecutionAttempted;
        N3DisabledByDefault = n3DisabledByDefault;
        ManualAuditRequired = manualAuditRequired;
        OwnerApprovalRequired = ownerApprovalRequired;
        DiagnosticsSummary = diagnosticsSummary;
        Redacted = redacted;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataN3ReadinessResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataN3ReadinessResult? other) =>
        other is not null && Status == other.Status && Blockers.SequenceEqual(other.Blockers) &&
        ReportOnly == other.ReportOnly && N3ExecutionAttempted == other.N3ExecutionAttempted &&
        N3DisabledByDefault == other.N3DisabledByDefault && ManualAuditRequired == other.ManualAuditRequired &&
        OwnerApprovalRequired == other.OwnerApprovalRequired && DiagnosticsSummary == other.DiagnosticsSummary &&
        Redacted == other.Redacted;
    public override int GetHashCode() =>
        HashCode.Combine(Status, Blockers.Count, ReportOnly, N3ExecutionAttempted, N3DisabledByDefault,
            ManualAuditRequired, OwnerApprovalRequired, DiagnosticsSummary, Redacted);
    public static bool operator ==(CanonicalLibraryMetadataN3ReadinessResult left, CanonicalLibraryMetadataN3ReadinessResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataN3ReadinessResult left, CanonicalLibraryMetadataN3ReadinessResult right) => !left.Equals(right);
}

public class CanonicalLibraryMetadataN3ReadinessGate
{
    public CanonicalLibraryMetadataN3ReadinessResult Evaluate(CanonicalLibraryMetadataN1EvidenceBundle bundle)
    {
        var blockers = new List<CanonicalLibraryMetadataN3ReadinessBlocker>();
        if (bundle.Status == CanonicalLibraryMetadataN1EvidenceStatus.missing) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.missingEvidence);
        if (bundle.Status == CanonicalLibraryMetadataN1EvidenceStatus.invalid) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.invalidEvidence);
        if (bundle.Status != CanonicalLibraryMetadataN1EvidenceStatus.valid && bundle.Status != CanonicalLibraryMetadataN1EvidenceStatus.blocked)
            blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.invalidEvidence);
        if (bundle.CommitSuccessCount == 0 && !bundle.ValidNoChangeOrNoEligibleEvidenceAccepted)
            blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.commitSuccessOrAcceptedNoChangeMissing);
        if (bundle.RollbackFailureCount > 0 || (bundle.RollbackAttempted && !bundle.RollbackSucceeded))
            blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.rollbackFailure);
        if (bundle.ReadSideDivergenceCount > 0 || bundle.ReadSideEquivalenceStatus == CanonicalLibraryMetadataN1EvidenceStatus.invalid)
            blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.readSideDivergence);
        if (bundle.UnsafeCandidateExecutionCount > 0 || bundle.Blockers.Contains(CanonicalLibraryMetadataN1EvidenceBlocker.unsafeCandidateKind) ||
            bundle.Blockers.Contains(CanonicalLibraryMetadataN1EvidenceBlocker.unsafeCandidateExecuted))
            blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.unsafeCandidateExecution);
        if (bundle.ResourceMoveCount > 0) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.resourceMove);
        if (bundle.ContentWriteCount > 0) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.contentWrite);
        if (bundle.TombstoneDeleteCount > 0) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.tombstoneDelete);
        if (bundle.GeneratedArtifactWriteCount > 0) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.generatedArtifactWrite);
        if (bundle.AudioChangeCount > 0) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.audioChange);
        if (bundle.RouteBoundaryViolationReported) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.routeBoundaryViolation);
        if (bundle.ReceiveJSONMutationReported) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.receiveJSONMutation);
        if (bundle.ActivePilot != CanonicalMigrationDomain.libraryMetadata) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.otherDomainActive);
        if (bundle.RuntimeSwitchStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.runtimeSwitchEnabled);
        if (bundle.ReleaseDefaultDisabledStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.releaseDefaultEnabled);
        if (!bundle.LegacyFallbackAvailable) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.legacyFallbackUnavailable);
        if (!bundle.ManualAuditRequired) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.manualAuditRequired);
        if (!bundle.OwnerApprovalRequiredForNextStage) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.ownerApprovalRequired);
        if (!bundle.N3DisabledByDefault) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.n3NotDisabledByDefault);
        if (bundle.DiagnosticsRedactedStatus != CanonicalLibraryMetadataN1EvidenceStatus.valid) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.sensitiveDataLeak);
        if (bundle.N3ExecutionReported) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.n3ExecutionAttempted);
        if (blockers.Count == 0 && bundle.Status != CanonicalLibraryMetadataN1EvidenceStatus.valid) blockers.Add(CanonicalLibraryMetadataN3ReadinessBlocker.invalidEvidence);

        var uniqueBlockers = new HashSet<CanonicalLibraryMetadataN3ReadinessBlocker>(blockers).OrderBy(b => b.ToString()).ToList();
        var status = ReadinessStatus(uniqueBlockers);

        return new CanonicalLibraryMetadataN3ReadinessResult(status, uniqueBlockers,
            true, false, bundle.N3DisabledByDefault, true, true,
            string.Join(",", $"status={status}", "reportOnly=true", "n3ExecutionAttempted=false",
                $"n3DisabledByDefault={bundle.N3DisabledByDefault}", "manualAuditRequired=true",
                "ownerApprovalRequired=true",
                $"blockers={string.Join("|", uniqueBlockers.Select(b => b.ToString()))}", "redacted=true"),
            true);
    }

    private CanonicalLibraryMetadataN3ReadinessStatus ReadinessStatus(List<CanonicalLibraryMetadataN3ReadinessBlocker> blockers)
    {
        if (blockers.Count == 0) return CanonicalLibraryMetadataN3ReadinessStatus.readyForN3AfterManualAudit;
        if (blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.missingEvidence)) return CanonicalLibraryMetadataN3ReadinessStatus.blockedMissingEvidence;
        if (blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.sensitiveDataLeak)) return CanonicalLibraryMetadataN3ReadinessStatus.blockedSensitiveDataLeak;
        if (blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.invalidEvidence) ||
            blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.commitSuccessOrAcceptedNoChangeMissing))
            return CanonicalLibraryMetadataN3ReadinessStatus.blockedInvalidEvidence;
        if (blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.rollbackFailure)) return CanonicalLibraryMetadataN3ReadinessStatus.blockedRollbackFailure;
        if (blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.readSideDivergence)) return CanonicalLibraryMetadataN3ReadinessStatus.blockedReadSideDivergence;
        if (blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.unsafeCandidateExecution) ||
            blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.resourceMove) ||
            blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.contentWrite) ||
            blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.tombstoneDelete) ||
            blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.generatedArtifactWrite) ||
            blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.audioChange) ||
            blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.routeBoundaryViolation) ||
            blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.receiveJSONMutation))
            return CanonicalLibraryMetadataN3ReadinessStatus.blockedUnsafeSideEffect;
        if (blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.otherDomainActive)) return CanonicalLibraryMetadataN3ReadinessStatus.blockedOtherDomainActive;
        if (blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.releaseDefaultEnabled) ||
            blockers.Contains(CanonicalLibraryMetadataN3ReadinessBlocker.runtimeSwitchEnabled))
            return CanonicalLibraryMetadataN3ReadinessStatus.blockedReleaseDefaultEnabled;
        return CanonicalLibraryMetadataN3ReadinessStatus.blocked;
    }
}

public sealed class CanonicalLibraryMetadataN1EvidenceExportResult : IEquatable<CanonicalLibraryMetadataN1EvidenceExportResult>
{
    public CanonicalLibraryMetadataN1EvidenceStatus Status { get; set; }
    public CanonicalLibraryMetadataN3ReadinessStatus N3ReadinessStatus { get; set; }
    public List<string> Summary { get; set; }
    public List<string> Blockers { get; set; }
    public bool Redacted { get; set; }

    public CanonicalLibraryMetadataN1EvidenceExportResult(
        CanonicalLibraryMetadataN1EvidenceStatus status = CanonicalLibraryMetadataN1EvidenceStatus.missing,
        CanonicalLibraryMetadataN3ReadinessStatus n3ReadinessStatus = CanonicalLibraryMetadataN3ReadinessStatus.blocked,
        List<string>? summary = null,
        List<string>? blockers = null,
        bool redacted = true)
    {
        Status = status;
        N3ReadinessStatus = n3ReadinessStatus;
        Summary = summary ?? new List<string>();
        Blockers = blockers ?? new List<string>();
        Redacted = redacted;
    }

    public override bool Equals(object? obj) => obj is CanonicalLibraryMetadataN1EvidenceExportResult other && Equals(other);
    public bool Equals(CanonicalLibraryMetadataN1EvidenceExportResult? other) =>
        other is not null && Status == other.Status && N3ReadinessStatus == other.N3ReadinessStatus &&
        Summary.SequenceEqual(other.Summary) && Blockers.SequenceEqual(other.Blockers) && Redacted == other.Redacted;
    public override int GetHashCode() =>
        HashCode.Combine(Status, N3ReadinessStatus, Summary.Count, Blockers.Count, Redacted);
    public static bool operator ==(CanonicalLibraryMetadataN1EvidenceExportResult left, CanonicalLibraryMetadataN1EvidenceExportResult right) => left.Equals(right);
    public static bool operator !=(CanonicalLibraryMetadataN1EvidenceExportResult left, CanonicalLibraryMetadataN1EvidenceExportResult right) => !left.Equals(right);
}

public class CanonicalLibraryMetadataN1EvidenceExporter
{
    public CanonicalLibraryMetadataN1EvidenceExportResult Export(
        CanonicalLibraryMetadataN1EvidenceBundle bundle,
        CanonicalLibraryMetadataN3ReadinessResult? readiness = null)
    {
        var redactor = new CanonicalLibraryMetadataN1EvidenceRedactor();
        var readinessResult = readiness ?? new CanonicalLibraryMetadataN3ReadinessGate().Evaluate(bundle);
        var summary = new List<string>
        {
            $"mode={bundle.LandingMode?.ToString() ?? "none"}",
            $"rootMode={bundle.RootMode?.ToString() ?? "none"}",
            $"candidateCount={bundle.SelectedCandidateCount}",
            $"executedCandidateCount={bundle.ExecutedCandidateCount}",
            $"candidateKind={bundle.SelectedCandidateKind?.ToString() ?? "none"}",
            $"successCount={bundle.CommitSuccessCount}",
            $"rollbackAttempted={bundle.RollbackAttempted}",
            $"rollbackSucceeded={bundle.RollbackSucceeded}",
            $"rollbackFailureCount={bundle.RollbackFailureCount}",
            $"legacyFallbackUsed={bundle.LegacyFallbackUsed}",
            $"duplicateSuppressionCount={bundle.DuplicateSuppressionCount}",
            $"readSideStatus={bundle.ReadSideEquivalenceStatus}",
            $"readSideDivergenceCount={bundle.ReadSideDivergenceCount}",
            $"landingFreezeStatus={bundle.LandingFreezeStatus}",
            $"otherDomainsStaticOnly={bundle.OtherDomainsStaticOnlyStatus}",
            $"runtimeSwitchFalse={bundle.RuntimeSwitchStatus == CanonicalLibraryMetadataN1EvidenceStatus.valid}",
            $"releaseDefaultDisabled={bundle.ReleaseDefaultDisabledStatus == CanonicalLibraryMetadataN1EvidenceStatus.valid}",
            $"n3ReadinessStatus={readinessResult.Status}",
            $"reportOnly={readinessResult.ReportOnly}"
        }.Select(s => redactor.Redact(s)).ToList();

        return new CanonicalLibraryMetadataN1EvidenceExportResult(
            bundle.Status, readinessResult.Status, summary,
            bundle.Blockers.Select(b => b.ToString())
                .Concat(readinessResult.Blockers.Select(b => b.ToString()))
                .Select(b => redactor.Redact(b)).ToList(),
            bundle.Redacted && readinessResult.Redacted);
    }
}
