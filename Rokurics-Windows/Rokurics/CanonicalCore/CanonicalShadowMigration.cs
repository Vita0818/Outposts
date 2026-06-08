using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowMigrationMode
    {
        disabled,
        diagnosticsOnly,
        dryRunCompare,
        shadowReadOnly,
        shadowReadOnlyWithNetworkProbe,
        executionShadowDryRun,
        executionShadowWithShadowFileStore,
        executionShadowWithReadOnlyTransportProbe,
        blockedProductionExecute,
        blockedExecutionShadowWrite,
        blockedExecutionShadowUpload,
        blockedExecutionShadowApply
    }

    public static class CanonicalShadowMigrationModeExtensions
    {
        public static bool RunsDryRun(this CanonicalShadowMigrationMode mode)
        {
            return mode switch
            {
                CanonicalShadowMigrationMode.dryRunCompare
                    or CanonicalShadowMigrationMode.shadowReadOnly
                    or CanonicalShadowMigrationMode.shadowReadOnlyWithNetworkProbe
                    or CanonicalShadowMigrationMode.executionShadowDryRun
                    or CanonicalShadowMigrationMode.executionShadowWithShadowFileStore
                    or CanonicalShadowMigrationMode.executionShadowWithReadOnlyTransportProbe => true,
                _ => false
            };
        }

        public static bool RunsExecutionShadowPreparation(this CanonicalShadowMigrationMode mode)
        {
            return mode switch
            {
                CanonicalShadowMigrationMode.executionShadowDryRun
                    or CanonicalShadowMigrationMode.executionShadowWithShadowFileStore
                    or CanonicalShadowMigrationMode.executionShadowWithReadOnlyTransportProbe => true,
                _ => false
            };
        }

        public static string NoSideEffectReason(this CanonicalShadowMigrationMode mode)
        {
            return mode switch
            {
                CanonicalShadowMigrationMode.disabled => "shadowMigrationDisabled",
                CanonicalShadowMigrationMode.diagnosticsOnly => "diagnosticsOnlyNoDryRun",
                CanonicalShadowMigrationMode.dryRunCompare => "dryRunCompareSuppressed",
                CanonicalShadowMigrationMode.shadowReadOnly => "shadowReadOnlySuppressed",
                CanonicalShadowMigrationMode.shadowReadOnlyWithNetworkProbe => "readOnlyNetworkProbeOnly",
                CanonicalShadowMigrationMode.executionShadowDryRun => "executionShadowDryRunSuppressed",
                CanonicalShadowMigrationMode.executionShadowWithShadowFileStore => "executionShadowShadowRootOnly",
                CanonicalShadowMigrationMode.executionShadowWithReadOnlyTransportProbe => "executionShadowReadOnlyTransportProbeOnly",
                CanonicalShadowMigrationMode.blockedProductionExecute => "productionExecuteBlocked",
                CanonicalShadowMigrationMode.blockedExecutionShadowWrite => "executionShadowWriteBlocked",
                CanonicalShadowMigrationMode.blockedExecutionShadowUpload => "executionShadowUploadBlocked",
                CanonicalShadowMigrationMode.blockedExecutionShadowApply => "executionShadowApplyBlocked",
                _ => "unknown"
            };
        }

        public static CanonicalKernelExecutionMode KernelShadowMode(this CanonicalShadowMigrationMode mode)
        {
            return mode switch
            {
                CanonicalShadowMigrationMode.executionShadowDryRun => CanonicalKernelExecutionMode.executionShadowDryRun,
                CanonicalShadowMigrationMode.executionShadowWithShadowFileStore => CanonicalKernelExecutionMode.executionShadowWithShadowFileStore,
                CanonicalShadowMigrationMode.executionShadowWithReadOnlyTransportProbe => CanonicalKernelExecutionMode.executionShadowWithReadOnlyTransportProbe,
                CanonicalShadowMigrationMode.shadowReadOnly or CanonicalShadowMigrationMode.shadowReadOnlyWithNetworkProbe => CanonicalKernelExecutionMode.productionShadow,
                _ => CanonicalKernelExecutionMode.dryRun,
            };
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowMigrationTrigger
    {
        iPhoneSyncTick,
        macInventory,
        macReceiver,
        manual,
        periodic,
        appActivation,
        retryDrainer,
        viewRefresh,
        testHarness
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowSuppressedSideEffectKind
    {
        noWrite,
        noUpload,
        noApply,
        noRouteMutation,
        noRuntimeSwitch,
        noProductionExecute
    }

    public record CanonicalShadowMigrationPolicy : IEquatable<CanonicalShadowMigrationPolicy>
    {
        public bool FailureIsFatal { get; init; }
        public bool RecordDiagnostics { get; init; }
        public int MaxDiagnosticsEvents { get; init; }
        public List<CanonicalShadowSuppressedSideEffectKind> SuppressedSideEffects { get; init; }
        public CanonicalShadowNetworkProbePolicy NetworkProbePolicy { get; init; }
        public CanonicalRealDataShadowCopyPolicy RealDataShadowCopyPolicy { get; init; }
        public CanonicalReadOnlyTransportProbePolicy ReadOnlyTransportProbePolicy { get; init; }

        public CanonicalShadowMigrationPolicy(
            bool failureIsFatal = false,
            bool recordDiagnostics = true,
            int maxDiagnosticsEvents = 200,
            List<CanonicalShadowSuppressedSideEffectKind>? suppressedSideEffects = null,
            CanonicalShadowNetworkProbePolicy? networkProbePolicy = null,
            CanonicalRealDataShadowCopyPolicy? realDataShadowCopyPolicy = null,
            CanonicalReadOnlyTransportProbePolicy? readOnlyTransportProbePolicy = null)
        {
            FailureIsFatal = failureIsFatal;
            RecordDiagnostics = recordDiagnostics;
            MaxDiagnosticsEvents = Math.Max(1, maxDiagnosticsEvents);
            SuppressedSideEffects = new HashSet<CanonicalShadowSuppressedSideEffectKind>(
                    suppressedSideEffects
                        ?? Enum.GetValues<CanonicalShadowSuppressedSideEffectKind>().ToList())
                .OrderBy(s => s.ToString()).ToList();
            NetworkProbePolicy = networkProbePolicy ?? new CanonicalShadowNetworkProbePolicy();
            RealDataShadowCopyPolicy = realDataShadowCopyPolicy ?? CanonicalRealDataShadowCopyPolicy.Disabled;
            ReadOnlyTransportProbePolicy = readOnlyTransportProbePolicy ?? CanonicalReadOnlyTransportProbePolicy.Disabled;
        }
    }

    public record CanonicalShadowMigrationConfiguration : IEquatable<CanonicalShadowMigrationConfiguration>
    {
        public bool IsEnabled { get; init; }
        public CanonicalShadowMigrationMode Mode { get; init; }
        public CanonicalShadowMigrationPolicy Policy { get; init; }

        public CanonicalShadowMigrationConfiguration(
            bool isEnabled = false,
            CanonicalShadowMigrationMode mode = CanonicalShadowMigrationMode.disabled,
            CanonicalShadowMigrationPolicy? policy = null)
        {
            IsEnabled = isEnabled;
            Mode = isEnabled ? mode : CanonicalShadowMigrationMode.disabled;
            Policy = policy ?? new CanonicalShadowMigrationPolicy();
        }

        public static readonly CanonicalShadowMigrationConfiguration Disabled = new();

        public static CanonicalShadowMigrationConfiguration Enabled(
            CanonicalShadowMigrationMode mode,
            CanonicalShadowMigrationPolicy? policy = null)
        {
            return new CanonicalShadowMigrationConfiguration(true, mode, policy);
        }

        public CanonicalShadowMigrationMode EffectiveMode =>
            IsEnabled ? Mode : CanonicalShadowMigrationMode.disabled;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowMigrationFailure
    {
        disabled,
        blockedProductionExecute,
        blockedExecutionShadowWrite,
        blockedExecutionShadowUpload,
        blockedExecutionShadowApply,
        roleNotAllowed,
        insufficientLocalSnapshot,
        insufficientPeerSnapshot,
        dryRunFailed,
        diagnosticsOnly,
        networkProbeRejected,
        realDataShadowCopyUnavailable,
        realDataShadowCopyFailed,
        readOnlyTransportProbeRejected,
        shadowRootCleanupFailed,
        unexpected
    }

    public record CanonicalShadowMigrationGate : IEquatable<CanonicalShadowMigrationGate>
    {
        public bool Allowed { get; init; }
        public CanonicalShadowMigrationMode Mode { get; init; }
        public CanonicalShadowMigrationTrigger Trigger { get; init; }
        public CanonicalProductionExecutionDomainRole NodeRole { get; init; }
        public CanonicalShadowMigrationFailure? Failure { get; init; }
        public string Reason { get; init; }
        public List<CanonicalShadowSuppressedSideEffectKind> SuppressedSideEffects { get; init; }

        public static CanonicalShadowMigrationGate Evaluate(
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalShadowMigrationTrigger trigger,
            CanonicalProductionExecutionDomainRole nodeRole,
            bool requestedProductionExecute = false)
        {
            var mode = configuration.EffectiveMode;
            var suppressed = configuration.Policy.SuppressedSideEffects;

            if (mode == CanonicalShadowMigrationMode.disabled)
            {
                return new CanonicalShadowMigrationGate
                {
                    Allowed = false, Mode = mode, Trigger = trigger,
                    NodeRole = nodeRole, Failure = CanonicalShadowMigrationFailure.disabled,
                    Reason = "shadowMigrationDisabled",
                    SuppressedSideEffects = suppressed
                };
            }
            if (requestedProductionExecute || mode == CanonicalShadowMigrationMode.blockedProductionExecute)
            {
                return new CanonicalShadowMigrationGate
                {
                    Allowed = false, Mode = mode, Trigger = trigger,
                    NodeRole = nodeRole,
                    Failure = CanonicalShadowMigrationFailure.blockedProductionExecute,
                    Reason = "productionExecuteBlockedInShadowStage",
                    SuppressedSideEffects = suppressed
                };
            }

            switch (mode)
            {
                case CanonicalShadowMigrationMode.blockedExecutionShadowWrite:
                    return new CanonicalShadowMigrationGate
                    {
                        Allowed = false, Mode = mode, Trigger = trigger,
                        NodeRole = nodeRole,
                        Failure = CanonicalShadowMigrationFailure.blockedExecutionShadowWrite,
                        Reason = mode.NoSideEffectReason(),
                        SuppressedSideEffects = suppressed
                    };
                case CanonicalShadowMigrationMode.blockedExecutionShadowUpload:
                    return new CanonicalShadowMigrationGate
                    {
                        Allowed = false, Mode = mode, Trigger = trigger,
                        NodeRole = nodeRole,
                        Failure = CanonicalShadowMigrationFailure.blockedExecutionShadowUpload,
                        Reason = mode.NoSideEffectReason(),
                        SuppressedSideEffects = suppressed
                    };
                case CanonicalShadowMigrationMode.blockedExecutionShadowApply:
                    return new CanonicalShadowMigrationGate
                    {
                        Allowed = false, Mode = mode, Trigger = trigger,
                        NodeRole = nodeRole,
                        Failure = CanonicalShadowMigrationFailure.blockedExecutionShadowApply,
                        Reason = mode.NoSideEffectReason(),
                        SuppressedSideEffects = suppressed
                    };
            }

            return nodeRole switch
            {
                CanonicalProductionExecutionDomainRole.iPhone
                    or CanonicalProductionExecutionDomainRole.mac
                    or CanonicalProductionExecutionDomainRole.testHarness
                    => new CanonicalShadowMigrationGate
                    {
                        Allowed = true, Mode = mode, Trigger = trigger,
                        NodeRole = nodeRole, Failure = null,
                        Reason = mode.NoSideEffectReason(),
                        SuppressedSideEffects = suppressed
                    },
                _ => new CanonicalShadowMigrationGate
                {
                    Allowed = false, Mode = mode, Trigger = trigger,
                    NodeRole = nodeRole,
                    Failure = CanonicalShadowMigrationFailure.roleNotAllowed,
                    Reason = "nodeRoleNotAllowed",
                    SuppressedSideEffects = suppressed
                }
            };
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowMigrationEventKind
    {
        canonicalShadowMigrationStarted,
        canonicalShadowMigrationCompleted,
        canonicalShadowMigrationBlocked,
        canonicalShadowMigrationDivergenceDetected,
        canonicalShadowMigrationEquivalent,
        canonicalShadowMigrationSuppressedSideEffects
    }

    public record CanonicalShadowMigrationEvent : IEquatable<CanonicalShadowMigrationEvent>
    {
        public string Id => string.Join("|", new[]
        {
            Kind.ToString(), SyncRunID ?? "", Trigger.ToString(),
            NodeRole.ToString(), Mode.ToString(), Domain.ToString(),
            Reason ?? ""
        });
        public CanonicalShadowMigrationEventKind Kind { get; init; }
        public string? SyncRunID { get; init; }
        public CanonicalShadowMigrationTrigger Trigger { get; init; }
        public CanonicalProductionExecutionDomainRole NodeRole { get; init; }
        public CanonicalShadowMigrationMode Mode { get; init; }
        public CanonicalProductionDomain Domain { get; init; }
        public string? EquivalenceStatus { get; init; }
        public string? MigrationGateStatus { get; init; }
        public int BlockerCount { get; init; }
        public int DivergenceCount { get; init; }
        public int SuppressedSideEffectCount { get; init; }
        public string? Reason { get; init; }
        public CanonicalTimestamp GeneratedAt { get; init; }

        public CanonicalShadowMigrationEvent(
            CanonicalShadowMigrationEventKind kind,
            string? syncRunID,
            CanonicalShadowMigrationTrigger trigger,
            CanonicalProductionExecutionDomainRole nodeRole,
            CanonicalShadowMigrationMode mode,
            CanonicalProductionDomain domain,
            string? equivalenceStatus = null,
            string? migrationGateStatus = null,
            int blockerCount = 0,
            int divergenceCount = 0,
            int suppressedSideEffectCount = 0,
            string? reason = null,
            DateTime? generatedAt = null)
        {
            Kind = kind;
            SyncRunID = CanonicalShadowMigrationRedaction.SafeIdentifier(syncRunID);
            Trigger = trigger;
            NodeRole = nodeRole;
            Mode = mode;
            Domain = domain;
            EquivalenceStatus = CanonicalShadowMigrationRedaction.SafeText(equivalenceStatus);
            MigrationGateStatus = CanonicalShadowMigrationRedaction.SafeText(migrationGateStatus);
            BlockerCount = Math.Max(0, blockerCount);
            DivergenceCount = Math.Max(0, divergenceCount);
            SuppressedSideEffectCount = Math.Max(0, suppressedSideEffectCount);
            Reason = CanonicalShadowMigrationRedaction.SafeText(reason);
            GeneratedAt = new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow);
        }

        public string DiagnosticsSummary => string.Join(",", new[]
        {
            $"trigger={Trigger}", $"nodeRole={NodeRole}", $"mode={Mode}",
            $"domain={Domain}",
            $"equivalence={EquivalenceStatus ?? "unknown"}",
            $"gate={MigrationGateStatus ?? "unknown"}",
            $"blockers={BlockerCount}", $"divergences={DivergenceCount}",
            $"suppressed={SuppressedSideEffectCount}",
            $"reason={Reason ?? "none"}"
        });
    }

    public record CanonicalShadowMigrationDivergenceSummary : IEquatable<CanonicalShadowMigrationDivergenceSummary>
    {
        public int DivergenceCount { get; init; }
        public int BlockingDivergenceCount { get; init; }
        public List<CanonicalProductionDomain> DivergentDomains { get; init; }

        public CanonicalShadowMigrationDivergenceSummary(CanonicalDryRunMigrationPlan? plan)
        {
            var divergences = plan?.EquivalenceReport?.LegacyEquivalence?.Divergences
                ?? new List<CanonicalLegacyEquivalenceDivergence>();
            DivergenceCount = divergences.Count;
            BlockingDivergenceCount = divergences.Count(d => d.IsBlocking);
            DivergentDomains = divergences
                .Select(d => d.Domain.ProductionDomain)
                .Distinct()
                .OrderBy(d => d.ToString())
                .ToList();
        }
    }

    public record CanonicalShadowMigrationSuppressedSideEffectSummary : IEquatable<CanonicalShadowMigrationSuppressedSideEffectSummary>
    {
        public bool NoWrite { get; init; }
        public bool NoUpload { get; init; }
        public bool NoApply { get; init; }
        public bool NoRouteMutation { get; init; }
        public bool NoRuntimeSwitch { get; init; }
        public bool NoProductionExecute { get; init; }
        public int SuppressedCount { get; init; }

        public CanonicalShadowMigrationSuppressedSideEffectSummary(
            List<CanonicalShadowSuppressedSideEffectKind>? effects = null)
        {
            var set = new HashSet<CanonicalShadowSuppressedSideEffectKind>(
                effects ?? Enum.GetValues<CanonicalShadowSuppressedSideEffectKind>().ToList());
            NoWrite = set.Contains(CanonicalShadowSuppressedSideEffectKind.noWrite);
            NoUpload = set.Contains(CanonicalShadowSuppressedSideEffectKind.noUpload);
            NoApply = set.Contains(CanonicalShadowSuppressedSideEffectKind.noApply);
            NoRouteMutation = set.Contains(CanonicalShadowSuppressedSideEffectKind.noRouteMutation);
            NoRuntimeSwitch = set.Contains(CanonicalShadowSuppressedSideEffectKind.noRuntimeSwitch);
            NoProductionExecute = set.Contains(CanonicalShadowSuppressedSideEffectKind.noProductionExecute);
            SuppressedCount = set.Count;
        }
    }

    public record CanonicalShadowMigrationReport : IEquatable<CanonicalShadowMigrationReport>
    {
        public string Id => RunID;
        public string RunID { get; init; }
        public string? SyncRunID { get; init; }
        public CanonicalShadowMigrationTrigger Trigger { get; init; }
        public CanonicalProductionExecutionDomainRole NodeRole { get; init; }
        public CanonicalShadowMigrationMode Mode { get; init; }
        public CanonicalProductionDomain Domain { get; init; }
        public CanonicalTimestamp GeneratedAt { get; init; }
        public string EquivalenceStatus { get; init; }
        public string MigrationGateStatus { get; init; }
        public int BlockerCount { get; init; }
        public CanonicalShadowMigrationDivergenceSummary DivergenceSummary { get; init; }
        public CanonicalShadowMigrationSuppressedSideEffectSummary SuppressedSideEffects { get; init; }
        public List<CanonicalShadowMigrationEvent> Events { get; init; }
        public CanonicalShadowMigrationFailure? Failure { get; init; }
        public string? FailureReason { get; init; }

        public CanonicalShadowMigrationReport(
            string runID,
            string? syncRunID,
            CanonicalShadowMigrationTrigger trigger,
            CanonicalProductionExecutionDomainRole nodeRole,
            CanonicalShadowMigrationMode mode,
            CanonicalProductionDomain domain,
            CanonicalDryRunMigrationPlan? plan,
            CanonicalShadowMigrationGate gate,
            List<CanonicalShadowMigrationEvent> events,
            CanonicalShadowMigrationFailure? failure = null,
            string? failureReason = null,
            DateTime? generatedAt = null)
        {
            RunID = CanonicalShadowMigrationRedaction.SafeIdentifier(runID)
                ?? "shadow-migration-run";
            SyncRunID = CanonicalShadowMigrationRedaction.SafeIdentifier(syncRunID);
            Trigger = trigger;
            NodeRole = nodeRole;
            Mode = mode;
            Domain = domain;
            GeneratedAt = new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow);
            var equivalence = plan?.EquivalenceReport?.LegacyEquivalence;
            EquivalenceStatus = equivalence == null
                ? "notEvaluated"
                : (equivalence.HasBlockingDivergence ? "divergent" : "equivalent");
            MigrationGateStatus = plan?.ReadinessReport?.ProductionMigrationBlocked == true
                ? "blocked" : (plan == null ? "notEvaluated" : "manualDesignOnly");
            BlockerCount = plan?.Blockers.Count ?? (failure == null ? 0 : 1);
            DivergenceSummary = new CanonicalShadowMigrationDivergenceSummary(plan);
            SuppressedSideEffects = new CanonicalShadowMigrationSuppressedSideEffectSummary(
                gate.SuppressedSideEffects);
            Events = events.Take(200).ToList();
            Failure = failure;
            FailureReason = CanonicalShadowMigrationRedaction.SafeText(failureReason);
        }
    }

    public record CanonicalShadowMigrationDiagnostics : IEquatable<CanonicalShadowMigrationDiagnostics>
    {
        public List<CanonicalShadowMigrationEvent> Events { get; init; }
        public CanonicalShadowMigrationReport? Report { get; init; }

        public CanonicalShadowMigrationDiagnostics(
            List<CanonicalShadowMigrationEvent>? events = null,
            CanonicalShadowMigrationReport? report = null)
        {
            Events = events ?? new List<CanonicalShadowMigrationEvent>();
            Report = report;
        }
    }

    public record CanonicalShadowMigrationResult
    {
        public CanonicalShadowMigrationConfiguration Configuration { get; init; }
        public CanonicalShadowMigrationGate Gate { get; init; }
        public CanonicalDryRunMigrationPlan? DryRunPlan { get; init; }
        public CanonicalShadowMigrationReport Report { get; init; }
        public CanonicalShadowMigrationFailure? Failure { get; init; }
        public bool IsFatal { get; init; }
        public bool Succeeded => Failure == null;
    }

    public class CanonicalShadowMigrationRunner
    {
        public CanonicalShadowMigrationRunner() { }

        public CanonicalShadowMigrationResult Run(
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalShadowMigrationTrigger trigger,
            CanonicalProductionExecutionDomainRole nodeRole,
            CanonicalProductionDomain domain,
            CanonicalProductionSnapshot? localSnapshot,
            CanonicalProductionSnapshot? peerSnapshot,
            CanonicalProductionPortSet ports,
            CanonicalRuntimeReadinessReport? currentRuntimeReadiness = null,
            CanonicalDryRunMigrationContext? context = null,
            string? syncRunID = null,
            DateTime? generatedAt = null)
        {
            var genAt = generatedAt ?? DateTime.UtcNow;
            currentRuntimeReadiness ??= DefaultRuntimeReadiness();
            context ??= new CanonicalDryRunMigrationContext();

            var gate = CanonicalShadowMigrationGate.Evaluate(
                configuration, trigger, nodeRole);
            var events = new List<CanonicalShadowMigrationEvent>
            {
                MakeEvent(CanonicalShadowMigrationEventKind.canonicalShadowMigrationStarted,
                    configuration, gate, domain, null, syncRunID, gate.Reason, genAt),
                MakeEvent(CanonicalShadowMigrationEventKind.canonicalShadowMigrationSuppressedSideEffects,
                    configuration, gate, domain, null, syncRunID,
                    configuration.EffectiveMode.NoSideEffectReason(), genAt)
            };

            if (!gate.Allowed)
            {
                events.Add(MakeEvent(CanonicalShadowMigrationEventKind.canonicalShadowMigrationBlocked,
                    configuration, gate, domain, null, syncRunID, gate.Reason, genAt));
                return MakeResult(configuration, gate, null, events, gate.Failure,
                    gate.Reason, domain, genAt);
            }

            if (!configuration.EffectiveMode.RunsDryRun())
            {
                events.Add(MakeEvent(CanonicalShadowMigrationEventKind.canonicalShadowMigrationCompleted,
                    configuration, gate, domain, null, syncRunID, "diagnosticsOnly", genAt));
                return MakeResult(configuration, gate, null, events, null, null, domain, genAt);
            }

            if (localSnapshot == null)
            {
                events.Add(MakeEvent(CanonicalShadowMigrationEventKind.canonicalShadowMigrationBlocked,
                    configuration, gate, domain, null, syncRunID,
                    "insufficientLocalSnapshot", genAt));
                return MakeResult(configuration, gate, null, events,
                    CanonicalShadowMigrationFailure.insufficientLocalSnapshot,
                    "insufficientLocalSnapshot", domain, genAt);
            }
            if (peerSnapshot == null)
            {
                events.Add(MakeEvent(CanonicalShadowMigrationEventKind.canonicalShadowMigrationBlocked,
                    configuration, gate, domain, null, syncRunID,
                    "insufficientPeerSnapshot", genAt));
                return MakeResult(configuration, gate, null, events,
                    CanonicalShadowMigrationFailure.insufficientPeerSnapshot,
                    "insufficientPeerSnapshot", domain, genAt);
            }

            try
            {
                var plan = new CanonicalDryRunMigrationPlanner().Plan(
                    localSnapshot, peerSnapshot, ports, currentRuntimeReadiness,
                    CanonicalShadowMigrationTrigger.periodic, context, genAt);

                if (plan.EquivalenceReport.LegacyEquivalence.HasBlockingDivergence)
                    events.Add(MakeEvent(
                        CanonicalShadowMigrationEventKind.canonicalShadowMigrationDivergenceDetected,
                        configuration, gate, domain, plan, syncRunID,
                        "blockingDivergence", genAt));
                else
                    events.Add(MakeEvent(
                        CanonicalShadowMigrationEventKind.canonicalShadowMigrationEquivalent,
                        configuration, gate, domain, plan, syncRunID,
                        "equivalent", genAt));

                if (plan.ReadinessReport.ProductionMigrationBlocked)
                    events.Add(MakeEvent(
                        CanonicalShadowMigrationEventKind.canonicalShadowMigrationBlocked,
                        configuration, gate, domain, plan, syncRunID,
                        "migrationGateBlocked", genAt));

                events.Add(MakeEvent(
                    CanonicalShadowMigrationEventKind.canonicalShadowMigrationCompleted,
                    configuration, gate, domain, plan, syncRunID,
                    "dryRunCompareCompleted", genAt));

                return MakeResult(configuration, gate, plan, events, null, null, domain, genAt);
            }
            catch (Exception ex)
            {
                events.Add(MakeEvent(
                    CanonicalShadowMigrationEventKind.canonicalShadowMigrationBlocked,
                    configuration, gate, domain, null, syncRunID,
                    "dryRunFailed", genAt));
                return MakeResult(configuration, gate, null, events,
                    CanonicalShadowMigrationFailure.dryRunFailed,
                    ex.ToString(), domain, genAt);
            }
        }

        public static CanonicalRuntimeReadinessReport DefaultRuntimeReadiness(
            DateTime? generatedAt = null)
        {
            var genAt = generatedAt ?? DateTime.UtcNow;
            return new CanonicalRuntimeReadinessEvaluator().Evaluate(
                new CanonicalRuntimeReadinessEvidence(
                    fileRootBinding: true, fileHashVerification: true,
                    transportRouteValidation: true, uploadResumableState: true,
                    applyExecutor: true, conflictResolver: true,
                    twoNodeHarness: true, productionStillLegacyOwned: true),
                genAt);
        }

        private CanonicalShadowMigrationResult MakeResult(
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalShadowMigrationGate gate,
            CanonicalDryRunMigrationPlan? plan,
            List<CanonicalShadowMigrationEvent> events,
            CanonicalShadowMigrationFailure? failure,
            string? failureReason,
            CanonicalProductionDomain domain,
            DateTime generatedAt)
        {
            var boundedEvents = events.Take(configuration.Policy.MaxDiagnosticsEvents).ToList();
            var report = new CanonicalShadowMigrationReport(
                plan?.DryRunID ?? gate.Reason,
                boundedEvents.FirstOrDefault()?.SyncRunID,
                gate.Trigger, gate.NodeRole, gate.Mode, domain,
                plan, gate, boundedEvents, failure, failureReason,
                generatedAt);
            return new CanonicalShadowMigrationResult
            {
                Configuration = configuration,
                Gate = gate,
                DryRunPlan = plan,
                Report = report,
                Failure = failure,
                IsFatal = failure != null && configuration.Policy.FailureIsFatal
            };
        }

        private CanonicalShadowMigrationEvent MakeEvent(
            CanonicalShadowMigrationEventKind kind,
            CanonicalShadowMigrationConfiguration configuration,
            CanonicalShadowMigrationGate gate,
            CanonicalProductionDomain domain,
            CanonicalDryRunMigrationPlan? plan,
            string? syncRunID,
            string? reason,
            DateTime generatedAt)
        {
            return new CanonicalShadowMigrationEvent(
                kind, syncRunID, gate.Trigger, gate.NodeRole,
                configuration.EffectiveMode, domain,
                equivalenceStatus: plan == null ? "notEvaluated"
                    : (plan.EquivalenceReport.LegacyEquivalence.HasBlockingDivergence
                        ? "divergent" : "equivalent"),
                migrationGateStatus: plan == null ? "notEvaluated"
                    : (plan.ReadinessReport.ProductionMigrationBlocked
                        ? "blocked" : "manualDesignOnly"),
                blockerCount: plan?.Blockers.Count ?? 0,
                divergenceCount: plan?.EquivalenceReport?.LegacyEquivalence?.Divergences.Count ?? 0,
                suppressedSideEffectCount: gate.SuppressedSideEffects.Count,
                reason: reason,
                generatedAt: generatedAt);
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowNetworkProbeKind
    {
        health,
        fingerprint,
        syncStatusReadOnly,
        syncInventoryReadOnly,
        artifactRequestReadOnly,
        deviceStatusReadOnly,
        uploadMetadata,
        uploadAudio,
        uploadSessionStart,
        uploadSessionChunk,
        uploadSessionFinalize,
        applyMetadata,
        applyManifest,
        mutatingRoute
    }

    public static class CanonicalShadowNetworkProbeKindExtensions
    {
        public static bool IsReadOnlyProbe(this CanonicalShadowNetworkProbeKind kind)
        {
            return kind switch
            {
                CanonicalShadowNetworkProbeKind.health
                    or CanonicalShadowNetworkProbeKind.fingerprint
                    or CanonicalShadowNetworkProbeKind.syncStatusReadOnly
                    or CanonicalShadowNetworkProbeKind.syncInventoryReadOnly
                    or CanonicalShadowNetworkProbeKind.artifactRequestReadOnly
                    or CanonicalShadowNetworkProbeKind.deviceStatusReadOnly => true,
                _ => false
            };
        }
    }

    public record CanonicalShadowNetworkProbeRequest : IEquatable<CanonicalShadowNetworkProbeRequest>
    {
        public CanonicalShadowNetworkProbeKind Kind { get; init; }
        public string RoutePath { get; init; }
        public int BodyByteCount { get; init; }
        public int? ArtifactByteLimit { get; init; }

        public CanonicalShadowNetworkProbeRequest(
            CanonicalShadowNetworkProbeKind kind,
            string routePath,
            int bodyByteCount = 0,
            int? artifactByteLimit = null)
        {
            Kind = kind;
            RoutePath = CanonicalShadowMigrationRedaction.SafeText(routePath)
                ?? kind.ToString();
            BodyByteCount = Math.Max(0, bodyByteCount);
            ArtifactByteLimit = artifactByteLimit.HasValue
                ? Math.Max(0, artifactByteLimit.Value) : null;
        }
    }

    public record CanonicalShadowNetworkProbeDecision : IEquatable<CanonicalShadowNetworkProbeDecision>
    {
        public bool Accepted { get; init; }
        public string Reason { get; init; }
        public bool NoMutation { get; init; }
    }

    public record CanonicalShadowNetworkProbePolicy : IEquatable<CanonicalShadowNetworkProbePolicy>
    {
        public bool IsEnabled { get; init; }
        public int ArtifactRequestMaxBytes { get; init; }
        public List<CanonicalShadowNetworkProbeKind> AllowedKinds { get; init; }

        public CanonicalShadowNetworkProbePolicy(
            bool isEnabled = false,
            int artifactRequestMaxBytes = 256 * 1024,
            List<CanonicalShadowNetworkProbeKind>? allowedKinds = null)
        {
            IsEnabled = isEnabled;
            ArtifactRequestMaxBytes = Math.Max(0, artifactRequestMaxBytes);
            AllowedKinds = new HashSet<CanonicalShadowNetworkProbeKind>(
                allowedKinds ?? new List<CanonicalShadowNetworkProbeKind>
                {
                    CanonicalShadowNetworkProbeKind.health,
                    CanonicalShadowNetworkProbeKind.fingerprint,
                    CanonicalShadowNetworkProbeKind.syncStatusReadOnly,
                    CanonicalShadowNetworkProbeKind.syncInventoryReadOnly,
                    CanonicalShadowNetworkProbeKind.artifactRequestReadOnly,
                    CanonicalShadowNetworkProbeKind.deviceStatusReadOnly
                })
                .OrderBy(k => k.ToString())
                .ToList();
        }

        public CanonicalShadowNetworkProbeDecision DecisionFor(
            CanonicalShadowNetworkProbeRequest request)
        {
            if (!IsEnabled)
                return new CanonicalShadowNetworkProbeDecision
                {
                    Accepted = false, Reason = "networkProbeDisabled", NoMutation = true
                };
            if (!AllowedKinds.Contains(request.Kind) || !request.Kind.IsReadOnlyProbe())
                return new CanonicalShadowNetworkProbeDecision
                {
                    Accepted = false, Reason = "mutatingRouteRejected", NoMutation = true
                };
            if (request.Kind == CanonicalShadowNetworkProbeKind.artifactRequestReadOnly)
            {
                var limit = request.ArtifactByteLimit ?? ArtifactRequestMaxBytes;
                if (request.BodyByteCount > limit)
                    return new CanonicalShadowNetworkProbeDecision
                    {
                        Accepted = false,
                        Reason = "artifactRequestSizeBoundExceeded",
                        NoMutation = true
                    };
            }
            return new CanonicalShadowNetworkProbeDecision
            {
                Accepted = true, Reason = "readOnlyProbeAccepted", NoMutation = true
            };
        }
    }

    public record CanonicalShadowPortFactoryOutput
    {
        public CanonicalProductionPortSet? PortSet { get; init; }
        public CanonicalProductionSnapshot? LocalSnapshot { get; init; }
        public CanonicalProductionSnapshot? PeerSnapshot { get; init; }
        public CanonicalProductionCapabilitySummary? Capabilities { get; init; }
        public CanonicalProductionPortReadiness? MissingPortReport { get; init; }
        public CanonicalShadowMigrationSuppressedSideEffectSummary? SuppressedSideEffects { get; init; }
        public string? DiagnosticsSafeSummary { get; init; }
        public CanonicalShadowNetworkProbePolicy? NetworkProbePolicy { get; init; }
        public CanonicalShadowRootLifecycle? ShadowRootLifecycle { get; init; }
        public CanonicalRealDataShadowCopyResult? RealDataShadowCopyResult { get; init; }
        public CanonicalReadOnlyTransportProbeResult? ReadOnlyTransportProbeResult { get; init; }
    }

    public static class CanonicalShadowMigrationRedaction
    {
        public static string? SafeIdentifier(string? value)
        {
            if (value == null) return null;
            return SafeText(value);
        }

        public static string? SafeText(string? value)
        {
            if (value == null) return null;
            var trimmed = value
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Trim();
            if (trimmed.Length == 0) return null;
            var lowercased = trimmed.ToLower();
            if (CanonicalProductionRedaction.ContainsSensitivePathSignal(trimmed)
                || lowercased.Contains("secret")
                || lowercased.Contains("api-key")
                || lowercased.Contains("token")
                || lowercased.Contains("transcript")
                || lowercased.Contains("provider response")
                || lowercased.Contains("full note")
                || lowercased.Contains("summary content"))
            {
                var hash = CanonicalHash.Sha256String(trimmed);
                return $"redacted-{CanonicalProductionRedaction.HashPrefix(hash.Value) ?? "shadow"}";
            }
            return trimmed.Length > 160 ? trimmed[..160] : trimmed;
        }
    }

    public class CanonicalShadowMigrationReportJSONLWriter
    {
        private readonly int _maxReports;

        public CanonicalShadowMigrationReportJSONLWriter(int maxReports = 200)
        {
            _maxReports = Math.Max(1, maxReports);
        }

        public void Append(CanonicalShadowMigrationReport report, string logPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(logPath);
                if (dir != null) Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                var line = JsonSerializer.Serialize(report, options);
                if (string.IsNullOrEmpty(line)) line = "{}";

                var existingLines = new List<string>();
                if (File.Exists(logPath))
                {
                    try
                    {
                        var raw = File.ReadAllText(logPath);
                        existingLines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                    }
                    catch { }
                }

                existingLines.Add(line);
                var nextLines = existingLines
                    .Skip(Math.Max(0, existingLines.Count - _maxReports))
                    .Take(_maxReports)
                    .ToList();

                File.WriteAllText(logPath, string.Join("\n", nextLines) + "\n");
            }
            catch { }
        }
    }
}
