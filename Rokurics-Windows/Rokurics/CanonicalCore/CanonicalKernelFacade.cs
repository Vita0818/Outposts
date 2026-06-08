using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

// ─── Forward-reference stubs for types from other files ─────────────────────

public sealed class CanonicalDryRunMigrationContext : IEquatable<CanonicalDryRunMigrationContext>
{
    public CanonicalDryRunMigrationContext() { }
    public override bool Equals(object? obj) => obj is CanonicalDryRunMigrationContext;
    public bool Equals(CanonicalDryRunMigrationContext? other) => other is not null;
    public override int GetHashCode() => 0;
    public static bool operator ==(CanonicalDryRunMigrationContext l, CanonicalDryRunMigrationContext r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunMigrationContext l, CanonicalDryRunMigrationContext r) => !l.Equals(r);
}

public sealed class CanonicalDryRunMigrationPlan : IEquatable<CanonicalDryRunMigrationPlan>
{
    public CanonicalDryRunMigrationPlan() { }
    public override bool Equals(object? obj) => obj is CanonicalDryRunMigrationPlan;
    public bool Equals(CanonicalDryRunMigrationPlan? other) => other is not null;
    public override int GetHashCode() => 0;
    public static bool operator ==(CanonicalDryRunMigrationPlan l, CanonicalDryRunMigrationPlan r) => l.Equals(r);
    public static bool operator !=(CanonicalDryRunMigrationPlan l, CanonicalDryRunMigrationPlan r) => !l.Equals(r);
}

public sealed class CanonicalDryRunMigrationPlanner
{
    public CanonicalDryRunMigrationPlan Plan(
        CanonicalProductionSnapshot local,
        CanonicalProductionSnapshot peer,
        CanonicalProductionPortSet ports,
        CanonicalRuntimeReadinessReport currentRuntimeReadiness,
        CanonicalSyncPlanTrigger trigger,
        CanonicalDryRunMigrationContext context)
    {
        return new CanonicalDryRunMigrationPlan();
    }

    public static CanonicalLegacyEquivalenceReport EquivalenceReport(
        CanonicalSyncPlan syncPlan,
        CanonicalApplyPlan applyPlan,
        CanonicalLibrarySyncPlan libraryPlan,
        CanonicalLegacyActionSnapshot localLegacyActions,
        CanonicalProductionPortReadiness portReadiness)
    {
        return new CanonicalLegacyEquivalenceReport();
    }
}

// ─── CanonicalKernelFacade Types ─────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalKernelExecutionMode
{
    disabled,
    dryRun,
    offlineRuntime,
    productionShadow,
    executionShadowDryRun,
    executionShadowWithShadowFileStore,
    executionShadowWithReadOnlyTransportProbe,
    productionExecute
}

public static class CanonicalKernelExecutionModeExtensions
{
    public static bool AllowsDryRunPlanning(this CanonicalKernelExecutionMode mode) =>
        mode switch
        {
            CanonicalKernelExecutionMode.dryRun or CanonicalKernelExecutionMode.productionShadow
                or CanonicalKernelExecutionMode.executionShadowDryRun
                or CanonicalKernelExecutionMode.executionShadowWithShadowFileStore
                or CanonicalKernelExecutionMode.executionShadowWithReadOnlyTransportProbe
                or CanonicalKernelExecutionMode.productionExecute => true,
            CanonicalKernelExecutionMode.disabled or CanonicalKernelExecutionMode.offlineRuntime => false,
            _ => false
        };

    public static bool IsShadowPreparationMode(this CanonicalKernelExecutionMode mode) =>
        mode switch
        {
            CanonicalKernelExecutionMode.productionShadow or CanonicalKernelExecutionMode.executionShadowDryRun
                or CanonicalKernelExecutionMode.executionShadowWithShadowFileStore
                or CanonicalKernelExecutionMode.executionShadowWithReadOnlyTransportProbe => true,
            CanonicalKernelExecutionMode.disabled or CanonicalKernelExecutionMode.dryRun
                or CanonicalKernelExecutionMode.offlineRuntime or CanonicalKernelExecutionMode.productionExecute => false,
            _ => false
        };
}

public sealed class CanonicalKernelConfiguration : IEquatable<CanonicalKernelConfiguration>
{
    public CanonicalKernelExecutionMode Mode { get; }
    public CanonicalProductionExecutionPolicy ProductionPolicy { get; }

    public CanonicalKernelConfiguration(
        CanonicalKernelExecutionMode mode = CanonicalKernelExecutionMode.disabled,
        CanonicalProductionExecutionPolicy? productionPolicy = null)
    {
        Mode = mode;
        ProductionPolicy = productionPolicy ?? new CanonicalProductionExecutionPolicy();
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelConfiguration other && Equals(other);
    public bool Equals(CanonicalKernelConfiguration? other) =>
        other is not null && Mode == other.Mode && ProductionPolicy.Equals(other.ProductionPolicy);
    public override int GetHashCode() => HashCode.Combine(Mode, ProductionPolicy);
    public static bool operator ==(CanonicalKernelConfiguration l, CanonicalKernelConfiguration r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelConfiguration l, CanonicalKernelConfiguration r) => !l.Equals(r);
}

public sealed class CanonicalKernelEnvironment : IEquatable<CanonicalKernelEnvironment>
{
    public CanonicalProductionPortSet Ports { get; }
    public CanonicalRuntimeHarness? RuntimeHarness { get; }

    public CanonicalKernelEnvironment(
        CanonicalProductionPortSet? ports = null,
        CanonicalRuntimeHarness? runtimeHarness = null)
    {
        Ports = ports ?? new CanonicalProductionPortSet();
        RuntimeHarness = runtimeHarness;
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelEnvironment other && Equals(other);
    public bool Equals(CanonicalKernelEnvironment? other) => other is not null;
    public override int GetHashCode() => 0;
    public static bool operator ==(CanonicalKernelEnvironment l, CanonicalKernelEnvironment r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelEnvironment l, CanonicalKernelEnvironment r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalKernelOperation
{
    buildSnapshot,
    buildManifest,
    planSync,
    buildApplyPlan,
    buildLibraryPlan,
    buildTransferProjection,
    buildObjectProjection,
    buildRuntimeReadiness,
    buildProductionReadiness,
    dryRunMigration,
    compareLegacy,
    executeOffline,
    executeProduction,
    rollbackPreview
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalKernelError
{
    disabled,
    modeNotAllowed,
    productionExecutionRejected,
    missingInput,
    portMissing,
    operationFailed
}

public sealed class CanonicalKernelException : Exception, IEquatable<CanonicalKernelException>
{
    public CanonicalKernelError ErrorKind { get; }
    public CanonicalProductionExecutionRejectionReason[]? RejectionReasons { get; }
    public CanonicalProductionPortKind? PortKind { get; }
    public string Detail { get; }

    public CanonicalKernelException(CanonicalKernelError errorKind, string message,
        CanonicalProductionExecutionRejectionReason[]? rejectionReasons = null,
        CanonicalProductionPortKind? portKind = null)
        : base(message)
    {
        ErrorKind = errorKind;
        RejectionReasons = rejectionReasons;
        PortKind = portKind;
        Detail = message;
    }

    public static CanonicalKernelException Disabled(string detail) =>
        new(CanonicalKernelError.disabled, detail);
    public static CanonicalKernelException ModeNotAllowed(string detail) =>
        new(CanonicalKernelError.modeNotAllowed, detail);
    public static CanonicalKernelException ProductionExecutionRejected(CanonicalProductionExecutionRejectionReason[] reasons) =>
        new(CanonicalKernelError.productionExecutionRejected, string.Join(",", reasons.Select(r => r.ToString())), rejectionReasons: reasons);
    public static CanonicalKernelException MissingInput(string detail) =>
        new(CanonicalKernelError.missingInput, detail);
    public static CanonicalKernelException PortMissing(CanonicalProductionPortKind portKind) =>
        new(CanonicalKernelError.portMissing, $"Port missing: {portKind}", portKind: portKind);
    public static CanonicalKernelException OperationFailed(string detail) =>
        new(CanonicalKernelError.operationFailed, detail);

    public override bool Equals(object? obj) => obj is CanonicalKernelException other && Equals(other);
    public bool Equals(CanonicalKernelException? other) =>
        other is not null && ErrorKind == other.ErrorKind && Detail == other.Detail;
    public override int GetHashCode() => HashCode.Combine(ErrorKind, Detail);
    public static bool operator ==(CanonicalKernelException l, CanonicalKernelException r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelException l, CanonicalKernelException r) => !l.Equals(r);
}

public sealed class CanonicalKernelAuditReport : IEquatable<CanonicalKernelAuditReport>
{
    public CanonicalKernelOperation Operation { get; }
    public CanonicalKernelExecutionMode Mode { get; }
    public CanonicalTimestamp GeneratedAt { get; }
    public CanonicalProductionExecutionAudit? ProductionAudit { get; }
    public CanonicalProductionSideEffect[] SideEffects { get; }
    public CanonicalProductionDiagnosticsEvent[] Diagnostics { get; }

    public CanonicalKernelAuditReport(
        CanonicalKernelOperation operation,
        CanonicalKernelExecutionMode mode,
        CanonicalProductionExecutionAudit? productionAudit = null,
        CanonicalProductionSideEffect[]? sideEffects = null,
        CanonicalProductionDiagnosticsEvent[]? diagnostics = null,
        DateTime generatedAt = default)
    {
        Operation = operation;
        Mode = mode;
        GeneratedAt = new CanonicalTimestamp(generatedAt == default ? DateTime.UtcNow : generatedAt);
        ProductionAudit = productionAudit;
        SideEffects = sideEffects ?? Array.Empty<CanonicalProductionSideEffect>();
        Diagnostics = diagnostics ?? Array.Empty<CanonicalProductionDiagnosticsEvent>();
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelAuditReport other && Equals(other);
    public bool Equals(CanonicalKernelAuditReport? other) =>
        other is not null && Operation == other.Operation && Mode == other.Mode;
    public override int GetHashCode() => HashCode.Combine(Operation, Mode);
    public static bool operator ==(CanonicalKernelAuditReport l, CanonicalKernelAuditReport r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelAuditReport l, CanonicalKernelAuditReport r) => !l.Equals(r);
}

public sealed class CanonicalKernelOperationResult<TPayload> : IEquatable<CanonicalKernelOperationResult<TPayload>>
{
    public CanonicalKernelOperation Operation { get; }
    public CanonicalKernelExecutionMode Mode { get; }
    public TPayload? Payload { get; }
    public CanonicalKernelError[] Errors { get; }
    public CanonicalKernelAuditReport Audit { get; }

    public CanonicalKernelOperationResult(
        CanonicalKernelOperation operation,
        CanonicalKernelExecutionMode mode,
        TPayload? payload,
        CanonicalKernelError[] errors,
        CanonicalKernelAuditReport audit)
    {
        Operation = operation;
        Mode = mode;
        Payload = payload;
        Errors = errors;
        Audit = audit;
    }

    public bool Succeeded => Errors.Length == 0 && Payload is not null;

    public static CanonicalKernelOperationResult<TPayload> Success(
        CanonicalKernelOperation operation,
        CanonicalKernelExecutionMode mode,
        TPayload payload,
        CanonicalKernelAuditReport audit) =>
        new(operation, mode, payload, Array.Empty<CanonicalKernelError>(), audit);

    public static CanonicalKernelOperationResult<TPayload> Failure(
        CanonicalKernelOperation operation,
        CanonicalKernelExecutionMode mode,
        CanonicalKernelError[] errors,
        CanonicalKernelAuditReport audit) =>
        new(operation, mode, default, errors, audit);

    public override bool Equals(object? obj) => obj is CanonicalKernelOperationResult<TPayload> other && Equals(other);
    public bool Equals(CanonicalKernelOperationResult<TPayload>? other) =>
        other is not null && Operation == other.Operation && Mode == other.Mode && Equals(Payload, other.Payload);
    public override int GetHashCode() => HashCode.Combine(Operation, Mode, Payload);
    public static bool operator ==(CanonicalKernelOperationResult<TPayload> l, CanonicalKernelOperationResult<TPayload> r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelOperationResult<TPayload> l, CanonicalKernelOperationResult<TPayload> r) => !l.Equals(r);
}

public sealed class CanonicalKernelInput : IEquatable<CanonicalKernelInput>
{
    public CanonicalProductionSnapshot? LocalSnapshot { get; }
    public CanonicalProductionSnapshot? PeerSnapshot { get; }
    public CanonicalSyncPlanTrigger Trigger { get; }

    public CanonicalKernelInput(
        CanonicalProductionSnapshot? localSnapshot = null,
        CanonicalProductionSnapshot? peerSnapshot = null,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic)
    {
        LocalSnapshot = localSnapshot;
        PeerSnapshot = peerSnapshot;
        Trigger = trigger;
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelInput other && Equals(other);
    public bool Equals(CanonicalKernelInput? other) =>
        other is not null && Equals(LocalSnapshot, other.LocalSnapshot) && Equals(PeerSnapshot, other.PeerSnapshot) && Trigger == other.Trigger;
    public override int GetHashCode() => HashCode.Combine(LocalSnapshot, PeerSnapshot, Trigger);
    public static bool operator ==(CanonicalKernelInput l, CanonicalKernelInput r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelInput l, CanonicalKernelInput r) => !l.Equals(r);
}

public sealed class CanonicalKernelOutput : IEquatable<CanonicalKernelOutput>
{
    public CanonicalManifest? Manifest { get; set; }
    public CanonicalSyncPlan? SyncPlan { get; set; }
    public CanonicalApplyPlan? ApplyPlan { get; set; }
    public CanonicalLibrarySyncPlan? LibraryPlan { get; set; }
    public CanonicalDryRunMigrationPlan? DryRunPlan { get; set; }
    public CanonicalProductionExecutionResult? ProductionResult { get; set; }

    public override bool Equals(object? obj) => obj is CanonicalKernelOutput other && Equals(other);
    public bool Equals(CanonicalKernelOutput? other) => other is not null;
    public override int GetHashCode() => 0;
    public static bool operator ==(CanonicalKernelOutput l, CanonicalKernelOutput r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelOutput l, CanonicalKernelOutput r) => !l.Equals(r);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalProductionSideEffectKind
{
    fileRead,
    fileWrite,
    metadataApply,
    generatedArtifactApply,
    networkRequest,
    uploadSessionStart,
    uploadChunkSend,
    uploadFinalize,
    tombstoneMark,
    conflictRecord,
    diagnosticsWrite
}

public sealed class CanonicalProductionExecutionStep : IEquatable<CanonicalProductionExecutionStep>
{
    public string Id => StepID;

    public string StepID { get; }
    public CanonicalProductionSideEffectKind Kind { get; }
    public CanonicalProductionDomain Domain { get; }
    public CanonicalFileWriteIntent? FileIntent { get; }
    public CanonicalProductionTransportBuildRequest? TransportRequest { get; }
    public CanonicalUploadStartRequest? UploadStartRequest { get; }
    public CanonicalUploadChunk? UploadChunk { get; }
    public CanonicalUploadFinalizeRequest? UploadFinalizeRequest { get; }
    public CanonicalApplyAction? ApplyAction { get; }
    public CanonicalProductionTombstoneRequest? TombstoneRequest { get; }

    public CanonicalProductionExecutionStep(
        string stepID,
        CanonicalProductionSideEffectKind kind,
        CanonicalProductionDomain domain,
        CanonicalFileWriteIntent? fileIntent = null,
        CanonicalProductionTransportBuildRequest? transportRequest = null,
        CanonicalUploadStartRequest? uploadStartRequest = null,
        CanonicalUploadChunk? uploadChunk = null,
        CanonicalUploadFinalizeRequest? uploadFinalizeRequest = null,
        CanonicalApplyAction? applyAction = null,
        CanonicalProductionTombstoneRequest? tombstoneRequest = null)
    {
        StepID = CanonicalProductionRedaction.SafeIdentifier(stepID, kind.ToString());
        Kind = kind;
        Domain = domain;
        FileIntent = fileIntent;
        TransportRequest = transportRequest;
        UploadStartRequest = uploadStartRequest;
        UploadChunk = uploadChunk;
        UploadFinalizeRequest = uploadFinalizeRequest;
        ApplyAction = applyAction;
        TombstoneRequest = tombstoneRequest;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionExecutionStep other && Equals(other);
    public bool Equals(CanonicalProductionExecutionStep? other) =>
        other is not null && StepID == other.StepID;
    public override int GetHashCode() => StepID.GetHashCode();
    public static bool operator ==(CanonicalProductionExecutionStep l, CanonicalProductionExecutionStep r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionExecutionStep l, CanonicalProductionExecutionStep r) => !l.Equals(r);
}

public sealed class CanonicalProductionExecutionInput : IEquatable<CanonicalProductionExecutionInput>
{
    public string OperationID { get; }
    public CanonicalProductionDomain[] Domains { get; }
    public CanonicalProductionExecutionStep[] Steps { get; }
    public CanonicalRollbackPlan? RollbackPlan { get; }
    public string? DryRunReportID { get; }
    public CanonicalDryRunEquivalenceReport? DryRunEquivalence { get; }
    public CanonicalDryRunReadinessReport? ReadinessReport { get; }
    public int UnresolvedConflictCount { get; }

    public CanonicalProductionExecutionInput(
        string operationID,
        CanonicalProductionDomain[] domains,
        CanonicalProductionExecutionStep[] steps,
        CanonicalRollbackPlan? rollbackPlan = null,
        string? dryRunReportID = null,
        CanonicalDryRunEquivalenceReport? dryRunEquivalence = null,
        CanonicalDryRunReadinessReport? readinessReport = null,
        int unresolvedConflictCount = 0)
    {
        OperationID = CanonicalProductionRedaction.SafeIdentifier(operationID, "production-operation");
        Domains = new HashSet<CanonicalProductionDomain>(domains)
            .OrderBy(d => d.ToString(), StringComparer.Ordinal)
            .ToArray();
        Steps = steps;
        RollbackPlan = rollbackPlan;
        DryRunReportID = dryRunReportID is not null
            ? CanonicalProductionRedaction.SafeIdentifier(dryRunReportID, "dry-run-report")
            : null;
        DryRunEquivalence = dryRunEquivalence;
        ReadinessReport = readinessReport;
        UnresolvedConflictCount = unresolvedConflictCount;
    }

    public override bool Equals(object? obj) => obj is CanonicalProductionExecutionInput other && Equals(other);
    public bool Equals(CanonicalProductionExecutionInput? other) =>
        other is not null && OperationID == other.OperationID;
    public override int GetHashCode() => OperationID.GetHashCode();
    public static bool operator ==(CanonicalProductionExecutionInput l, CanonicalProductionExecutionInput r) => l.Equals(r);
    public static bool operator !=(CanonicalProductionExecutionInput l, CanonicalProductionExecutionInput r) => !l.Equals(r);
}

// ─── CanonicalKernelFacade ───────────────────────────────────────────────────

public sealed class CanonicalKernelFacade : IEquatable<CanonicalKernelFacade>
{
    public CanonicalKernelConfiguration Configuration { get; }
    public CanonicalKernelEnvironment Environment { get; }

    public CanonicalKernelFacade(
        CanonicalKernelConfiguration? configuration = null,
        CanonicalKernelEnvironment? environment = null)
    {
        Configuration = configuration ?? new CanonicalKernelConfiguration();
        Environment = environment ?? new CanonicalKernelEnvironment();
    }

    public CanonicalKernelOperationResult<CanonicalProductionSnapshot> BuildSnapshot(CanonicalProductionSnapshot snapshot)
    {
        return Success(CanonicalKernelOperation.buildSnapshot, snapshot);
    }

    public CanonicalKernelOperationResult<CanonicalManifest> BuildManifest(CanonicalProductionSnapshot snapshot)
    {
        return Success(CanonicalKernelOperation.buildManifest, snapshot.Manifest);
    }

    public CanonicalKernelOperationResult<CanonicalSyncPlan> PlanSync(
        CanonicalManifest local,
        CanonicalManifest peer,
        CanonicalSyncPlanTrigger trigger)
    {
        try
        {
            return Success(CanonicalKernelOperation.planSync,
                new CanonicalSyncPlanner().Plan(local: local, peer: peer, trigger: trigger));
        }
        catch (Exception ex)
        {
            return Failure<CanonicalSyncPlan>(CanonicalKernelOperation.planSync,
                CanonicalKernelException.OperationFailed(ex.Message));
        }
    }

    public CanonicalKernelOperationResult<CanonicalApplyPlan> BuildApplyPlan(
        CanonicalManifest local,
        CanonicalManifest peer,
        CanonicalSyncPlan syncPlan,
        CanonicalSyncPlanTrigger trigger)
    {
        return Success(
            CanonicalKernelOperation.buildApplyPlan,
            new CanonicalApplyPlanner().Plan(local: local, peer: peer, syncPlan: syncPlan, trigger: trigger)
        );
    }

    public CanonicalKernelOperationResult<CanonicalLibrarySyncPlan> BuildLibraryPlan(
        CanonicalManifest local,
        CanonicalManifest peer,
        CanonicalSyncPlanTrigger trigger)
    {
        return Success(CanonicalKernelOperation.buildLibraryPlan,
            new CanonicalLibrarySyncPlanner().Plan(local: local, peer: peer, trigger: trigger));
    }

    public CanonicalKernelOperationResult<CanonicalTransferProjection> BuildTransferProjection(
        CanonicalTransferJob[] jobs)
    {
        return Success(CanonicalKernelOperation.buildTransferProjection,
            CanonicalTransferStateMachine.Projection(jobs));
    }

    public CanonicalKernelOperationResult<CanonicalLibraryProjection> BuildObjectProjection(
        CanonicalManifest manifest,
        CanonicalApplyPlan? applyPlan = null,
        CanonicalLibrarySyncPlan? libraryPlan = null,
        CanonicalTransferProjection? transferProjection = null)
    {
        return Success(
            CanonicalKernelOperation.buildObjectProjection,
            CanonicalObjectProjectionBuilder.Build(
                manifest: manifest,
                applyPlan: applyPlan,
                libraryPlan: libraryPlan,
                transferProjection: transferProjection
            )
        );
    }

    public CanonicalKernelOperationResult<CanonicalRuntimeReadinessReport> BuildRuntimeReadiness(
        CanonicalRuntimeReadinessEvidence evidence)
    {
        return Success(CanonicalKernelOperation.buildRuntimeReadiness,
            new CanonicalRuntimeReadinessEvaluator().Evaluate(evidence: evidence));
    }

    public CanonicalKernelOperationResult<CanonicalProductionPortReadiness> BuildProductionReadiness(
        CanonicalProductionPortSet? ports = null)
    {
        return Success(CanonicalKernelOperation.buildProductionReadiness,
            (ports ?? Environment.Ports).Readiness());
    }

    public CanonicalKernelOperationResult<CanonicalDryRunMigrationPlan> DryRunMigration(
        CanonicalProductionSnapshot local,
        CanonicalProductionSnapshot peer,
        CanonicalRuntimeReadinessReport currentRuntimeReadiness,
        CanonicalSyncPlanTrigger trigger,
        CanonicalDryRunMigrationContext? context = null)
    {
        if (!Configuration.Mode.AllowsDryRunPlanning())
        {
            return Failure<CanonicalDryRunMigrationPlan>(CanonicalKernelOperation.dryRunMigration,
                CanonicalKernelException.ModeNotAllowed(Configuration.Mode.ToString()));
        }
        try
        {
            var plan = new CanonicalDryRunMigrationPlanner().Plan(
                local: local,
                peer: peer,
                ports: Environment.Ports,
                currentRuntimeReadiness: currentRuntimeReadiness,
                trigger: trigger,
                context: context ?? new CanonicalDryRunMigrationContext()
            );
            return Success(CanonicalKernelOperation.dryRunMigration, plan);
        }
        catch (Exception ex)
        {
            return Failure<CanonicalDryRunMigrationPlan>(CanonicalKernelOperation.dryRunMigration,
                CanonicalKernelException.OperationFailed(ex.Message));
        }
    }

    public CanonicalKernelOperationResult<CanonicalLegacyEquivalenceReport> CompareLegacy(
        CanonicalSyncPlan syncPlan,
        CanonicalApplyPlan applyPlan,
        CanonicalLibrarySyncPlan libraryPlan,
        CanonicalLegacyActionSnapshot localLegacyActions,
        CanonicalProductionPortReadiness portReadiness)
    {
        return Success(
            CanonicalKernelOperation.compareLegacy,
            CanonicalDryRunMigrationPlanner.EquivalenceReport(
                syncPlan: syncPlan,
                applyPlan: applyPlan,
                libraryPlan: libraryPlan,
                localLegacyActions: localLegacyActions,
                portReadiness: portReadiness
            )
        );
    }

    public async Task<CanonicalKernelOperationResult<CanonicalRuntimeHarnessTickResult>> ExecuteOffline(
        CanonicalRuntimeHarnessNodeRole localRole = CanonicalRuntimeHarnessNodeRole.iPhone,
        CanonicalRuntimeHarnessNodeRole peerRole = CanonicalRuntimeHarnessNodeRole.mac,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic)
    {
        if (Configuration.Mode != CanonicalKernelExecutionMode.offlineRuntime)
        {
            return Failure<CanonicalRuntimeHarnessTickResult>(CanonicalKernelOperation.executeOffline,
                CanonicalKernelException.ModeNotAllowed(Configuration.Mode.ToString()));
        }
        try
        {
            var harness = Environment.RuntimeHarness ?? new CanonicalRuntimeHarness();
            var result = await harness.RunApplyTick(localRole: localRole, peerRole: peerRole, trigger: trigger);
            return Success(CanonicalKernelOperation.executeOffline, result);
        }
        catch (Exception ex)
        {
            return Failure<CanonicalRuntimeHarnessTickResult>(CanonicalKernelOperation.executeOffline,
                CanonicalKernelException.OperationFailed(ex.Message));
        }
    }

    public async Task<CanonicalKernelOperationResult<CanonicalProductionExecutionResult>> ExecuteProduction(
        CanonicalProductionExecutionInput input,
        CanonicalProductionExecutionToken? token)
    {
        var guardAudit = CanonicalProductionExecutionGuard.Evaluate(
            mode: Configuration.Mode,
            token: token,
            policy: Configuration.ProductionPolicy,
            domains: input.Domains,
            ports: Environment.Ports,
            rollbackPlan: input.RollbackPlan,
            dryRunReportID: input.DryRunReportID,
            dryRunEquivalence: input.DryRunEquivalence,
            readinessReport: input.ReadinessReport,
            unresolvedConflictCount: input.UnresolvedConflictCount
        );

        if (!guardAudit.Allowed)
        {
            var deniedResult = new CanonicalProductionExecutionResult(
                operationID: input.OperationID,
                mode: Configuration.Mode,
                succeeded: false,
                failures: new[]
                {
                    new CanonicalProductionExecutionFailure(
                        operationID: input.OperationID,
                        reason: string.Join(",", guardAudit.RejectionReasons.Select(r => r.ToString()))
                    )
                },
                guardAudit: guardAudit
            );
            return CanonicalKernelOperationResult<CanonicalProductionExecutionResult>.Failure(
                operation: CanonicalKernelOperation.executeProduction,
                mode: Configuration.Mode,
                errors: new[]
                {
                    CanonicalKernelException.ProductionExecutionRejected(guardAudit.RejectionReasons).ErrorKind
                },
                audit: new CanonicalKernelAuditReport(
                    operation: CanonicalKernelOperation.executeProduction,
                    mode: Configuration.Mode,
                    productionAudit: guardAudit
                )
            ).ReplacingPayload(deniedResult);
        }

        var sideEffects = new List<CanonicalProductionSideEffect>();
        var failures = new List<CanonicalProductionExecutionFailure>();
        foreach (var step in input.Steps)
        {
            try
            {
                var sideEffect = await Execute(step);
                if (sideEffect is not null)
                {
                    sideEffects.Add(sideEffect);
                }
            }
            catch (Exception ex)
            {
                failures.Add(new CanonicalProductionExecutionFailure(
                    operationID: step.StepID,
                    domain: step.Domain,
                    reason: ex.Message
                ));
            }
        }

        var result = new CanonicalProductionExecutionResult(
            operationID: input.OperationID,
            mode: Configuration.Mode,
            succeeded: failures.Count == 0,
            sideEffects: sideEffects.ToArray(),
            failures: failures.ToArray(),
            guardAudit: guardAudit
        );

        var audit = new CanonicalKernelAuditReport(
            operation: CanonicalKernelOperation.executeProduction,
            mode: Configuration.Mode,
            productionAudit: guardAudit,
            sideEffects: sideEffects.ToArray()
        );

        return failures.Count == 0
            ? CanonicalKernelOperationResult<CanonicalProductionExecutionResult>.Success(
                operation: CanonicalKernelOperation.executeProduction,
                mode: Configuration.Mode,
                payload: result,
                audit: audit)
            : CanonicalKernelOperationResult<CanonicalProductionExecutionResult>.Failure(
                operation: CanonicalKernelOperation.executeProduction,
                mode: Configuration.Mode,
                errors: failures.Select(f => CanonicalKernelException.OperationFailed(f.Reason).ErrorKind).ToArray(),
                audit: audit)
                .ReplacingPayload(result);
    }

    public CanonicalKernelOperationResult<CanonicalRollbackAudit> RollbackPreview(
        CanonicalRollbackPlan? plan,
        CanonicalProductionDomain[] requiredDomains)
    {
        return Success(CanonicalKernelOperation.rollbackPreview,
            new CanonicalRollbackAudit(plan: plan, requiredDomains: requiredDomains));
    }

    private async Task<CanonicalProductionSideEffect?> Execute(CanonicalProductionExecutionStep step)
    {
        switch (step.Kind)
        {
            case CanonicalProductionSideEffectKind.fileRead:
            {
                var intent = step.FileIntent
                    ?? throw CanonicalKernelException.MissingInput(step.StepID);
                _ = await RequiredFilePort().ReadMetadata(
                    new CanonicalProductionMetadataReadRequest(objectID: step.StepID, reference: intent.Reference));
                return new CanonicalProductionSideEffect(
                    kind: CanonicalProductionSideEffectKind.fileRead,
                    domain: step.Domain,
                    objectID: step.StepID,
                    summary: "fileRead");
            }
            case CanonicalProductionSideEffectKind.fileWrite:
            {
                var intent = step.FileIntent
                    ?? throw CanonicalKernelException.MissingInput(step.StepID);
                var writeResult = await RequiredFilePort().WriteMetadata(intent, rollbackCheckpoint: null);
                return new CanonicalProductionSideEffect(
                    kind: CanonicalProductionSideEffectKind.fileWrite,
                    domain: step.Domain,
                    objectID: step.StepID,
                    byteSize: writeResult.Evidence.ActualByteSize,
                    hashPrefix: writeResult.Evidence.ActualHashPrefix,
                    summary: $"fileWrite:{writeResult.Disposition}");
            }
            case CanonicalProductionSideEffectKind.networkRequest:
            {
                var request = step.TransportRequest
                    ?? throw CanonicalKernelException.MissingInput(step.StepID);
                var exchange = await RequiredTransportPort().SendRequest(
                    await RequiredTransportPort().BuildSignedRequest(request));
                return exchange.SideEffect
                    ?? new CanonicalProductionSideEffect(
                        kind: CanonicalProductionSideEffectKind.networkRequest,
                        domain: step.Domain,
                        route: request.Route,
                        summary: "networkRequest");
            }
            case CanonicalProductionSideEffectKind.uploadSessionStart:
            {
                var request = step.UploadStartRequest
                    ?? throw CanonicalKernelException.MissingInput(step.StepID);
                var status = await RequiredUploadPort().StartResumableUpload(request, DateTime.UtcNow);
                return new CanonicalProductionSideEffect(
                    kind: CanonicalProductionSideEffectKind.uploadSessionStart,
                    domain: step.Domain,
                    objectID: request.ObjectID,
                    byteSize: status.FileSize,
                    hashPrefix: status.Checksum?.Value,
                    summary: "uploadSessionStart");
            }
            case CanonicalProductionSideEffectKind.uploadChunkSend:
            {
                var chunk = step.UploadChunk
                    ?? throw CanonicalKernelException.MissingInput(step.StepID);
                var status = await RequiredUploadPort().UploadChunk(chunk, DateTime.UtcNow);
                return new CanonicalProductionSideEffect(
                    kind: CanonicalProductionSideEffectKind.uploadChunkSend,
                    domain: step.Domain,
                    objectID: chunk.ObjectID,
                    byteSize: status.ConfirmedBytes,
                    hash: chunk.ChunkHash,
                    summary: "uploadChunkSend");
            }
            case CanonicalProductionSideEffectKind.uploadFinalize:
            {
                var request = step.UploadFinalizeRequest
                    ?? throw CanonicalKernelException.MissingInput(step.StepID);
                var status = await RequiredUploadPort().FinalizeUpload(request, DateTime.UtcNow);
                return new CanonicalProductionSideEffect(
                    kind: CanonicalProductionSideEffectKind.uploadFinalize,
                    domain: step.Domain,
                    objectID: request.ObjectID,
                    byteSize: status.FileSize,
                    hashPrefix: status.Checksum?.Value,
                    summary: "uploadFinalize");
            }
            case CanonicalProductionSideEffectKind.metadataApply:
            {
                var action = step.ApplyAction
                    ?? throw CanonicalKernelException.MissingInput(step.StepID);
                var request = new CanonicalProductionApplyExecutionRequest(action: action, rollbackCheckpointID: null);
                CanonicalProductionApplyResult result;
                switch (action.Kind)
                {
                    case CanonicalApplyActionKind.recordingMetadataSend:
                    case CanonicalApplyActionKind.folderMetadataSend:
                    case CanonicalApplyActionKind.studyItemMetadataSend:
                    case CanonicalApplyActionKind.libraryTombstoneSend:
                    case CanonicalApplyActionKind.objectTombstoneSend:
                        result = await RequiredApplyPort().SendMetadata(request);
                        break;
                    default:
                        result = await RequiredApplyPort().ApplyMetadata(request);
                        break;
                }
                return result.SideEffect
                    ?? new CanonicalProductionSideEffect(
                        kind: CanonicalProductionSideEffectKind.metadataApply,
                        domain: step.Domain,
                        objectID: action.Target.ObjectID,
                        summary: "metadataApply");
            }
            case CanonicalProductionSideEffectKind.generatedArtifactApply:
            {
                var action = step.ApplyAction
                    ?? throw CanonicalKernelException.MissingInput(step.StepID);
                var result = await RequiredApplyPort().ApplyGeneratedArtifact(
                    new CanonicalProductionApplyExecutionRequest(action: action, rollbackCheckpointID: null));
                return result.SideEffect
                    ?? new CanonicalProductionSideEffect(
                        kind: CanonicalProductionSideEffectKind.generatedArtifactApply,
                        domain: step.Domain,
                        objectID: action.Target.ObjectID,
                        artifactID: action.Target.ArtifactID,
                        summary: "generatedArtifactApply");
            }
            case CanonicalProductionSideEffectKind.tombstoneMark:
            {
                var request = step.TombstoneRequest
                    ?? throw CanonicalKernelException.MissingInput(step.StepID);
                var markResult = await RequiredFilePort().MarkTombstone(request);
                return new CanonicalProductionSideEffect(
                    kind: CanonicalProductionSideEffectKind.tombstoneMark,
                    domain: step.Domain,
                    byteSize: markResult.Evidence.ActualByteSize,
                    hashPrefix: markResult.Evidence.ActualHashPrefix,
                    summary: "tombstoneMark");
            }
            case CanonicalProductionSideEffectKind.conflictRecord:
            {
                var action = step.ApplyAction
                    ?? throw CanonicalKernelException.MissingInput(step.StepID);
                var result = await RequiredApplyPort().RecordConflict(
                    new CanonicalProductionApplyExecutionRequest(action: action, rollbackCheckpointID: null));
                return result.SideEffect
                    ?? new CanonicalProductionSideEffect(
                        kind: CanonicalProductionSideEffectKind.conflictRecord,
                        domain: step.Domain,
                        objectID: action.Target.ObjectID,
                        summary: "conflictRecord");
            }
            case CanonicalProductionSideEffectKind.diagnosticsWrite:
            {
                return new CanonicalProductionSideEffect(
                    kind: CanonicalProductionSideEffectKind.diagnosticsWrite,
                    domain: step.Domain,
                    summary: "diagnosticsWrite");
            }
            default:
                return null;
        }
    }

    private ICanonicalProductionFilePort RequiredFilePort()
    {
        return Environment.Ports.File
            ?? throw CanonicalKernelException.PortMissing(CanonicalProductionPortKind.file);
    }

    private ICanonicalProductionTransportPort RequiredTransportPort()
    {
        return Environment.Ports.Transport
            ?? throw CanonicalKernelException.PortMissing(CanonicalProductionPortKind.transport);
    }

    private ICanonicalProductionUploadPort RequiredUploadPort()
    {
        return Environment.Ports.Upload
            ?? throw CanonicalKernelException.PortMissing(CanonicalProductionPortKind.upload);
    }

    private ICanonicalProductionApplyPort RequiredApplyPort()
    {
        return Environment.Ports.Apply
            ?? throw CanonicalKernelException.PortMissing(CanonicalProductionPortKind.apply);
    }

    private CanonicalKernelOperationResult<TPayload> Success<TPayload>(
        CanonicalKernelOperation operation,
        TPayload payload)
    {
        return CanonicalKernelOperationResult<TPayload>.Success(
            operation: operation,
            mode: Configuration.Mode,
            payload: payload,
            audit: new CanonicalKernelAuditReport(operation: operation, mode: Configuration.Mode)
        );
    }

    private CanonicalKernelOperationResult<TPayload> Failure<TPayload>(
        CanonicalKernelOperation operation,
        CanonicalKernelException error)
    {
        return CanonicalKernelOperationResult<TPayload>.Failure(
            operation: operation,
            mode: Configuration.Mode,
            errors: new[] { error.ErrorKind },
            audit: new CanonicalKernelAuditReport(operation: operation, mode: Configuration.Mode)
        );
    }

    public override bool Equals(object? obj) => obj is CanonicalKernelFacade other && Equals(other);
    public bool Equals(CanonicalKernelFacade? other) =>
        other is not null && Configuration.Equals(other.Configuration);
    public override int GetHashCode() => Configuration.GetHashCode();
    public static bool operator ==(CanonicalKernelFacade l, CanonicalKernelFacade r) => l.Equals(r);
    public static bool operator !=(CanonicalKernelFacade l, CanonicalKernelFacade r) => !l.Equals(r);
}

// ─── Extension for CanonicalKernelOperationResult replacing payload ──────────

public static class CanonicalKernelOperationResultExtensions
{
    public static CanonicalKernelOperationResult<TPayload> ReplacingPayload<TPayload>(
        this CanonicalKernelOperationResult<TPayload> source,
        TPayload payload)
    {
        return new CanonicalKernelOperationResult<TPayload>(
            operation: source.Operation,
            mode: source.Mode,
            payload: payload,
            errors: source.Errors,
            audit: source.Audit
        );
    }
}
