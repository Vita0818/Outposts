namespace Rokurics.CanonicalCore;

public class CanonicalRecordingMetadataCutoverRunner
{
    public CanonicalRecordingMetadataCutoverRunner() { }

    public CanonicalCutoverGate EvaluateGate(
        CanonicalSingleDomainCutoverConfiguration configuration,
        CanonicalCutoverToken? token,
        CanonicalRecordingMetadataCutoverEvidence evidence,
        List<CanonicalRecordingMetadataCutoverCandidate> candidates,
        CanonicalSyncPlanTrigger trigger)
    {
        var failures = new List<CanonicalCutoverFailure>();

        if (configuration.Mode == CanonicalCutoverMode.disabled)
            failures.Add(CanonicalCutoverFailure.disabled);
        if (configuration.Domain != CanonicalCutoverDomain.recordingMetadata)
            failures.Add(CanonicalCutoverFailure.unsupportedDomain);
        if (!configuration.Mode.PermitsProductionCommit())
            failures.Add(CanonicalCutoverFailure.modeNotExecutable);
        if (token == null)
            failures.Add(CanonicalCutoverFailure.missingToken);
        if (token?.OwnerApproved != true)
            failures.Add(CanonicalCutoverFailure.missingOwnerApproval);
        if (evidence.RollbackPlan == null || evidence.RollbackPlan.Covers(CanonicalProductionDomain.recordingMetadata) != true)
            failures.Add(CanonicalCutoverFailure.missingRollback);
        if (!evidence.RealDataShadowCopyVerified)
            failures.Add(CanonicalCutoverFailure.missingRealDataShadowCopyEvidence);
        if (!evidence.ExecutionShadowVerified)
            failures.Add(CanonicalCutoverFailure.missingExecutionShadowEvidence);
        if (!evidence.DryRunEquivalenceVerified)
            failures.Add(CanonicalCutoverFailure.missingDryRunEquivalence);
        if (!evidence.NoBlockingDivergence)
            failures.Add(CanonicalCutoverFailure.blockingDivergence);
        if (!evidence.NoUnresolvedConflict || candidates.Any(c => c.UnresolvedConflict))
            failures.Add(CanonicalCutoverFailure.unresolvedConflict);

        var sendNeeded = candidates.Any(c => c.RequiresNetworkSend);
        if (configuration.Policy.RequireReadOnlyProbeForSend && sendNeeded && !evidence.ReadOnlyTransportProbePassed)
            failures.Add(CanonicalCutoverFailure.missingReadOnlyTransportProbe);
        if (!evidence.ProductionPortAvailable)
            failures.Add(CanonicalCutoverFailure.productionPortUnavailable);
        if (!evidence.RealRootBoundApplyPortAvailable || !CanonicalRecordingMetadataApplyPortModeExtensions.IsNonDryRunRootBound(evidence.ApplyPortMode))
            failures.Add(evidence.ApplyPortMode == CanonicalRecordingMetadataApplyPortMode.disabled || evidence.ApplyPortMode == CanonicalRecordingMetadataApplyPortMode.dryRun
                ? CanonicalCutoverFailure.applyPortDryRunOnly
                : CanonicalCutoverFailure.rootBoundWriteUnavailable);
        if (!evidence.RootBoundWriteAvailable)
            failures.Add(CanonicalCutoverFailure.rootBoundWriteUnavailable);
        if (!evidence.AtomicReplaceAvailable)
            failures.Add(CanonicalCutoverFailure.atomicReplaceUnavailable);
        if (!evidence.RollbackCheckpointAvailable)
            failures.Add(CanonicalCutoverFailure.rollbackCheckpointUnavailable);
        if (!evidence.RollbackVerified)
            failures.Add(CanonicalCutoverFailure.rollbackVerificationMissing);
        if (!evidence.ProductionRootDisabledByDefault)
            failures.Add(CanonicalCutoverFailure.productionRootEnabledByDefault);
        if (!evidence.TestRootUsed && evidence.ApplyPortMode == CanonicalRecordingMetadataApplyPortMode.testRootBound)
            failures.Add(CanonicalCutoverFailure.testRootMissing);

        if (configuration.Mode == CanonicalCutoverMode.canary)
        {
            var stagePolicy = configuration.Policy.EffectiveRecordingMetadataCanaryStagePolicy;
            if (stagePolicy.RequestedStage.IsExecutable())
            {
                var stageGate = new CanonicalRecordingMetadataCanaryStageGate(
                    stagePolicy, configuration.Domain, token, evidence);
                if (!stageGate.Allowed)
                    failures.AddRange(stageGate.Blockers.Select(BlockersToCutoverFailure));
            }
            else
            {
                if (configuration.Policy.CanaryMaxObjectsPerSyncRun > 1)
                    failures.Add(CanonicalCutoverFailure.canaryBudgetAboveOneDenied);
                if (configuration.Policy.CanaryMaxObjectsPerSyncRun == 1 &&
                    !configuration.Policy.AllowsV87CanaryN1InternalExecution)
                    failures.Add(CanonicalCutoverFailure.missingInternalCanaryConfiguration);
            }
        }

        if (!evidence.LegacyFallbackAvailable)
            failures.Add(CanonicalCutoverFailure.legacyFallbackUnavailable);
        if (configuration.Policy.RequireRollbackRehearsal && !evidence.RollbackRehearsalPassed)
            failures.Add(CanonicalCutoverFailure.missingRollback);
        if (configuration.Policy.RequireProductionExecutionGuardPass && !evidence.ProductionExecutionGuardPassed)
            failures.Add(CanonicalCutoverFailure.productionPortUnavailable);
        if (trigger == CanonicalSyncPlanTrigger.viewRefresh)
            failures.Add(CanonicalCutoverFailure.viewRefreshTriggerDenied);
        if (trigger == CanonicalSyncPlanTrigger.retryDrainer)
            failures.Add(CanonicalCutoverFailure.retryDrainerFreshMetadataDenied);
        if (candidates.Any(c => c.CutoverActionKind == null))
            failures.Add(CanonicalCutoverFailure.unsupportedAction);
        if (candidates.Any(c => c.StableMetadataHash == null))
            failures.Add(CanonicalCutoverFailure.unstableMetadataHash);

        return new CanonicalCutoverGate(
            configuration.Domain,
            configuration.Mode,
            failures,
            evidence.LegacyFallbackAvailable,
            failures.Count == 0 ? "recordingMetadataCutoverGateAllowed" : "recordingMetadataCutoverGateBlocked");
    }

    public async Task<CanonicalCutoverResult> Run(
        CanonicalSingleDomainCutoverConfiguration configuration,
        CanonicalCutoverToken? token,
        CanonicalRecordingMetadataCutoverEvidence evidence,
        List<CanonicalRecordingMetadataCutoverCandidate> candidates,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        ICanonicalRecordingMetadataCutoverExecutor executor)
    {
        var gate = EvaluateGate(configuration, token, evidence, candidates, trigger);
        var syncRunID = token?.SyncRunID;
        var diagnostics = new List<CanonicalRecordingMetadataCutoverDiagnostic>
        {
            Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCutoverGateEvaluated,
                syncRunID, trigger, nodeRole, result: gate.Allowed ? "allowed" : "blocked", reason: gate.Reason)
        };

        if (!gate.Allowed)
        {
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCutoverGateBlocked,
                syncRunID, trigger, nodeRole, result: "blocked",
                reason: string.Join(",", gate.Failures.Select(f => f.ToString()))));
            var fallback = evidence.LegacyFallbackAvailable;
            if (fallback)
            {
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataLegacyFallbackUsed,
                    syncRunID, trigger, nodeRole, result: "legacyFallback", reason: "cutoverGateBlocked"));
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataLegacyFallbackPreserved,
                    syncRunID, trigger, nodeRole, result: "legacyFallbackPreserved", reason: "cutoverGateBlocked"));
            }

            return MakeResult(gate, configuration, evidence,
                new List<CanonicalRecordingMetadataProductionCommitResult>(),
                new List<CanonicalRecordingMetadataRollbackExecutionResult>(),
                Bounded(diagnostics, configuration.Policy.MaxDiagnosticsEvents),
                fallback, new List<string>(), 0, false, false, null,
                gate.Failures,
                selection: null,
                stageGate: new CanonicalRecordingMetadataCanaryStageGate(
                    configuration.Policy.EffectiveRecordingMetadataCanaryStagePolicy,
                    configuration.Domain, token, evidence),
                syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole);
        }

        diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCutoverGateAllowed,
            syncRunID, trigger, nodeRole, result: "allowed", reason: "allCutoverEvidencePresent"));

        var selection = new CanonicalRecordingMetadataCanarySelectionResult(
            selectedCandidates: new List<CanonicalRecordingMetadataCanaryCandidate>(),
            blockers: new List<CanonicalRecordingMetadataCanarySelectionBlocker>(),
            evaluatedCandidateCount: candidates.Count,
            noEligibleCandidate: candidates.Count == 0);

        List<CanonicalRecordingMetadataCutoverCandidate> selected;
        var stageGate = new CanonicalRecordingMetadataCanaryStageGate(
            configuration.Policy.EffectiveRecordingMetadataCanaryStagePolicy,
            configuration.Domain, token, evidence);

        if (configuration.Mode == CanonicalCutoverMode.canary)
        {
            if (configuration.Policy.CanaryMaxObjectsPerSyncRun == 1 &&
                configuration.Policy.AllowsV87CanaryN1InternalExecution)
            {
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryN1Configured,
                    syncRunID, trigger, nodeRole, result: "configured", reason: "explicitInternalN1"));
            }

            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryCandidateSelectionStarted,
                syncRunID, trigger, nodeRole, result: "started", reason: $"candidateCount={candidates.Count}"));

            selection = new CanonicalRecordingMetadataCanarySelector().Select(
                configuration, trigger, evidence, candidates);

            if (selection.SelectedCandidates.Count > 0)
            {
                foreach (var sc in selection.SelectedCandidates)
                {
                    diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryCandidateSelected,
                        syncRunID, trigger, nodeRole, objectID: sc.ObjectID, action: sc.ActionKind.ToString(),
                        result: "selected",
                        reason: stageGate.RequestedStage.IsExecutable() ? stageGate.RequestedStage.ToString() : "stableOrder",
                        hash: sc.CutoverCandidate.StableMetadataHash));
                }
            }
            else
            {
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryNoEligibleCandidate,
                    syncRunID, trigger, nodeRole, result: "noEligibleCandidate",
                    reason: string.Join(",", selection.Blockers.Select(b => b.Reason.ToString()))));
            }

            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryStarted,
                syncRunID, trigger, nodeRole, result: "started",
                reason: $"max={configuration.Policy.CanaryMaxObjectsPerSyncRun}"));

            selected = selection.SelectedCutoverCandidates;
            if (selected.Count < candidates.Count)
            {
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryBudgetExhausted,
                    syncRunID, trigger, nodeRole, result: "budgetExhausted",
                    reason: $"selected={selected.Count},available={candidates.Count}"));
            }
        }
        else
        {
            selected = candidates;
        }

        if (selected.Count == 0)
        {
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataLegacyFallbackPreserved,
                syncRunID, trigger, nodeRole, result: "legacyFallbackPreserved", reason: "noCanonicalCommitSelected"));
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataDuplicateSuppressionSkipped,
                syncRunID, trigger, nodeRole, result: "skipped", reason: "noCanonicalCommitSelected"));
        }
        else
        {
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCommitExecutorCreated,
                syncRunID, trigger, nodeRole, result: "created", reason: "recordingMetadataExecutor"));
        }

        var commits = new List<CanonicalRecordingMetadataProductionCommitResult>();
        var rollbacks = new List<CanonicalRecordingMetadataRollbackExecutionResult>();
        var duplicateSuppressed = new List<string>();
        var legacyFallbackUsed = false;
        var fatalBlocker = false;
        var retirementBlockers = new List<CanonicalCutoverFailure>();

        foreach (var candidate in selected)
        {
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataRollbackCheckpointCreated,
                syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                result: "checkpointCreated", reason: candidate.EffectiveRollbackCheckpointID));
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCommitPreconditionEvaluated,
                syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                result: "evaluated", reason: "objectHashRouteRollbackCanary", hash: candidate.StableMetadataHash));
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryCommitStarted,
                syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                result: "started", reason: "recordingMetadataOnly", hash: candidate.StableMetadataHash));
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataProductionCommitStarted,
                syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                result: "started", reason: "recordingMetadataOnly", hash: candidate.StableMetadataHash));

            var commit = await executor.CommitRecordingMetadata(candidate);
            commits.Add(commit);

            if (commit.Committed && commit.PreconditionVerified && commit.PostconditionVerified)
            {
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryPostconditionVerified,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "verified", reason: commit.Reason, hash: candidate.StableMetadataHash));
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataPostconditionVerified,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "verified", reason: commit.Reason, hash: candidate.StableMetadataHash));
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryCommitCompleted,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "committed", reason: commit.Reason, hash: candidate.StableMetadataHash));
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataProductionCommitCompleted,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "committed", reason: commit.Reason, hash: candidate.StableMetadataHash));
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataDuplicateSuppressionAllowed,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "allowed", reason: "canonicalCommitSucceeded"));
                duplicateSuppressed.Add(candidate.Action.ActionID);
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataDuplicateLegacySuppressed,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "suppressed", reason: "canonicalCommitSucceeded"));
                continue;
            }

            var failure = CutoverFailureFor(commit.FailureKind);
            retirementBlockers.Add(failure);

            if (failure == CanonicalCutoverFailure.preconditionMismatch)
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCommitPreconditionFailed,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "failed", reason: commit.Reason, hash: candidate.StableMetadataHash));
            if (failure == CanonicalCutoverFailure.postconditionMismatch)
            {
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryPostconditionFailed,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "failed", reason: commit.Reason, hash: candidate.StableMetadataHash));
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataPostconditionFailed,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "failed", reason: commit.Reason, hash: candidate.StableMetadataHash));
            }

            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataDuplicateSuppressionSkipped,
                syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                result: "skipped", reason: failure.ToString()));
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryCommitFailed,
                syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                result: "failed", reason: failure.ToString(), hash: candidate.StableMetadataHash));
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataProductionCommitFailed,
                syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                result: "failed", reason: failure.ToString(), hash: candidate.StableMetadataHash));
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryRollbackStarted,
                syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                result: "started", reason: failure.ToString()));
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataRollbackStarted,
                syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                result: "started", reason: failure.ToString()));

            var rollback = await executor.RollbackRecordingMetadata(candidate, failure);
            rollbacks.Add(rollback);

            if (rollback.Succeeded)
            {
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryRollbackCompleted,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "rolledBack", reason: rollback.Reason));
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataRollbackCompleted,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "rolledBack", reason: rollback.Reason));

                if (evidence.LegacyFallbackAvailable)
                {
                    legacyFallbackUsed = true;
                    diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryLegacyFallbackUsed,
                        syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                        result: "legacyFallback", reason: "canonicalPrecommitOrCanaryFailed"));
                    diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataLegacyFallbackUsed,
                        syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                        result: "legacyFallback", reason: "canonicalPrecommitOrCanaryFailed"));
                    diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataLegacyFallbackPreserved,
                        syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                        result: "legacyFallbackPreserved", reason: "canonicalPrecommitOrCanaryFailed"));
                }
            }
            else
            {
                fatalBlocker = true;
                retirementBlockers.Add(CanonicalCutoverFailure.rollbackFailed);
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryRollbackFailed,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "fatal", reason: rollback.Reason));
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataRollbackFailed,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "fatal", reason: rollback.Reason));
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryFatalBlocker,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "fatalBlocker", reason: rollback.Reason));
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataRollbackFatalBlocker,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "fatalBlocker", reason: rollback.Reason));
            }

            if (configuration.Mode == CanonicalCutoverMode.canary)
                diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryFailed,
                    syncRunID, trigger, nodeRole, objectID: candidate.ObjectID, action: candidate.Action.Kind.ToString(),
                    result: "failed", reason: failure.ToString()));

            break;
        }

        var canarySucceeded = configuration.Mode == CanonicalCutoverMode.canary
            && selected.Count > 0
            && commits.Count == selected.Count
            && commits.All(c => c.Committed && c.PreconditionVerified && c.PostconditionVerified)
            && !fatalBlocker;

        if (configuration.Mode == CanonicalCutoverMode.canary)
        {
            diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataCanaryCompleted,
                syncRunID, trigger, nodeRole, result: canarySucceeded ? "passed" : "completed",
                reason: $"attempted={selected.Count}"));
        }

        var uiProjection = MakeUIProjection(evidence, candidates.FirstOrDefault());
        diagnostics.Add(Diagnostic(CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalUIProjectionParallelReadStarted,
            syncRunID, trigger, nodeRole, objectID: candidates.FirstOrDefault()?.ObjectID,
            result: "started", reason: "diagnosticsOnly"));
        diagnostics.Add(Diagnostic(
            evidence.UiParallelReadEquivalent
                ? CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalUIProjectionParallelReadEquivalent
                : CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalUIProjectionParallelReadDivergent,
            syncRunID, trigger, nodeRole, objectID: candidates.FirstOrDefault()?.ObjectID,
            result: evidence.UiParallelReadEquivalent ? "equivalent" : "divergent",
            reason: "displayUnchanged", hash: candidates.FirstOrDefault()?.StableMetadataHash));

        return MakeResult(gate, configuration, evidence, commits, rollbacks,
            Bounded(diagnostics, configuration.Policy.MaxDiagnosticsEvents),
            legacyFallbackUsed, duplicateSuppressed, selected.Count, canarySucceeded, fatalBlocker,
            uiProjection, retirementBlockers,
            selection: selection, stageGate: stageGate, syncRunID: syncRunID, trigger: trigger, nodeRole: nodeRole);
    }

    private CanonicalCutoverResult MakeResult(
        CanonicalCutoverGate gate,
        CanonicalSingleDomainCutoverConfiguration configuration,
        CanonicalRecordingMetadataCutoverEvidence evidence,
        List<CanonicalRecordingMetadataProductionCommitResult> commits,
        List<CanonicalRecordingMetadataRollbackExecutionResult> rollbackResults,
        List<CanonicalRecordingMetadataCutoverDiagnostic> diagnostics,
        bool legacyFallbackUsed,
        List<string> duplicateSuppressed,
        int canaryAttemptedCount,
        bool canarySucceeded,
        bool fatalBlocker,
        CanonicalRecordingMetadataUIParallelProjectionResult? uiProjection,
        List<CanonicalCutoverFailure> retirementBlockers,
        CanonicalRecordingMetadataCanarySelectionResult? selection = null,
        CanonicalRecordingMetadataCanaryStageGate stageGate = null!,
        string? syncRunID = null,
        CanonicalSyncPlanTrigger trigger = CanonicalSyncPlanTrigger.periodic,
        CanonicalProductionExecutionDomainRole nodeRole = CanonicalProductionExecutionDomainRole.testHarness)
    {
        var blockers = new List<CanonicalCutoverFailure>(retirementBlockers);
        blockers.AddRange(gate.Failures);
        if (!canarySucceeded)
            blockers.Add(CanonicalCutoverFailure.modeNotExecutable);
        if (!evidence.UiParallelReadEquivalent)
            blockers.Add(CanonicalCutoverFailure.blockingDivergence);

        var retirementCandidate = gate.Allowed
            && canarySucceeded
            && !fatalBlocker
            && blockers.Count == 0
            && evidence.RollbackRehearsalPassed
            && evidence.LegacyFallbackAvailable;

        var allDiagnostics = new List<CanonicalRecordingMetadataCutoverDiagnostic>(diagnostics);
        allDiagnostics.Add(Diagnostic(
            retirementCandidate
                ? CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataRetirementCandidate
                : CanonicalRecordingMetadataCutoverDiagnosticKind.canonicalRecordingMetadataRetirementBlocked,
            syncRunID, trigger, nodeRole,
            result: retirementCandidate ? "candidate" : "blocked",
            reason: retirementCandidate
                ? "recordingMetadataOnly"
                : string.Join(",", new HashSet<CanonicalCutoverFailure>(blockers).OrderBy(b => b.ToString()).Select(b => b.ToString()))));

        var provisionalResult = new CanonicalCutoverResult(
            gate: gate,
            commits: commits,
            rollbackResults: rollbackResults,
            diagnostics: allDiagnostics,
            legacyFallbackUsed: legacyFallbackUsed,
            duplicateLegacySuppressedActionIDs: new HashSet<string>(duplicateSuppressed).OrderBy(x => x).ToList(),
            canaryAttemptedCount: canaryAttemptedCount,
            canarySucceeded: canarySucceeded,
            fatalBlocker: fatalBlocker,
            uiProjection: uiProjection,
            retirementReadiness: new CanonicalRecordingMetadataRetirementReadiness(
                retirementCandidate, canarySucceeded, evidence.LegacyFallbackAvailable, blockers));

        var finalSelection = selection ?? new CanonicalRecordingMetadataCanarySelectionResult(
            selectedCandidates: commits.Select(commit => new CanonicalRecordingMetadataCanaryCandidate(
                new CanonicalRecordingMetadataCutoverCandidate(
                    new CanonicalApplyAction(
                        commit.ActionKind == CanonicalRecordingMetadataCutoverActionKind.send
                            ? CanonicalApplyActionKind.recordingMetadataSend
                            : CanonicalApplyActionKind.recordingMetadataApply,
                        // Source is simplified - caller should provide proper values
                        CanonicalActionSource.local,
                        new CanonicalApplyTarget(commit.ObjectID),
                        commit.ActionKind == CanonicalRecordingMetadataCutoverActionKind.send
                            ? CanonicalApplyBridgeHint.legacyMetadataManifestSend
                            : CanonicalApplyBridgeHint.legacyMetadataManifestApply,
                        commit.ActionKind == CanonicalRecordingMetadataCutoverActionKind.send
                            ? CanonicalApplyActionKind.recordingMetadataSend.ToString()
                            : CanonicalApplyActionKind.recordingMetadataApply.ToString()),
                    null, null))).ToList(),
            blockers: new List<CanonicalRecordingMetadataCanarySelectionBlocker>(),
            evaluatedCandidateCount: commits.Count,
            noEligibleCandidate: commits.Count == 0);

        var result = provisionalResult;
        result.ObservationReport = new CanonicalRecordingMetadataCanaryObservationReport(
            configuration, finalSelection, provisionalResult);

        if (configuration.Policy.EffectiveRecordingMetadataCanaryStagePolicy.RequestedStage.IsExecutable())
        {
            result.CanaryStageResult = new CanonicalRecordingMetadataCanaryStageResult(
                stageGate, finalSelection, provisionalResult);
        }

        return result;
    }

    private CanonicalRecordingMetadataUIParallelProjectionResult? MakeUIProjection(
        CanonicalRecordingMetadataCutoverEvidence evidence,
        CanonicalRecordingMetadataCutoverCandidate? candidate)
    {
        if (candidate == null || candidate.ExpectedObject == null)
            return null;

        return new CanonicalRecordingMetadataUIParallelProjectionResult(
            candidate.ObjectID,
            evidence.UiParallelReadEquivalent,
            candidate.ExpectedObject.MetadataHash,
            evidence.UiParallelReadEquivalent ? candidate.ExpectedObject.MetadataHash : candidate.LocalObject?.MetadataHash,
            evidence.UiParallelReadEquivalent ? "uiProjectionEquivalent" : "uiProjectionDivergent");
    }

    private CanonicalCutoverFailure CutoverFailureFor(CanonicalRecordingMetadataProductionCommitFailureKind? failureKind) =>
        failureKind switch
        {
            CanonicalRecordingMetadataProductionCommitFailureKind.preconditionMismatch => CanonicalCutoverFailure.preconditionMismatch,
            CanonicalRecordingMetadataProductionCommitFailureKind.postconditionMismatch => CanonicalCutoverFailure.postconditionMismatch,
            CanonicalRecordingMetadataProductionCommitFailureKind.transportFailureBeforeSend => CanonicalCutoverFailure.transportFailureBeforeSend,
            CanonicalRecordingMetadataProductionCommitFailureKind.applyFailureBeforeCommit => CanonicalCutoverFailure.applyFailureBeforeCommit,
            CanonicalRecordingMetadataProductionCommitFailureKind.applyFailureAfterPartialCommit => CanonicalCutoverFailure.applyFailureAfterPartialCommit,
            _ => CanonicalCutoverFailure.postconditionMismatch
        };

    private static CanonicalCutoverFailure BlockersToCutoverFailure(CanonicalRecordingMetadataStageEvidenceBlocker blocker) =>
        blocker switch
        {
            CanonicalRecordingMetadataStageEvidenceBlocker.stageDisabled or
            CanonicalRecordingMetadataStageEvidenceBlocker.candidateExecutionNotApproved => CanonicalCutoverFailure.modeNotExecutable,
            CanonicalRecordingMetadataStageEvidenceBlocker.unsupportedDomain => CanonicalCutoverFailure.unsupportedDomain,
            CanonicalRecordingMetadataStageEvidenceBlocker.runtimeSwitchEnabled => CanonicalCutoverFailure.runtimeSwitchDenied,
            CanonicalRecordingMetadataStageEvidenceBlocker.previousStageEvidenceMissing => CanonicalCutoverFailure.missingCanaryStageEvidence,
            CanonicalRecordingMetadataStageEvidenceBlocker.stageOrderViolation => CanonicalCutoverFailure.canaryStageOrderViolation,
            CanonicalRecordingMetadataStageEvidenceBlocker.previousStageObservationIncomplete or
            CanonicalRecordingMetadataStageEvidenceBlocker.observationWindowIncomplete => CanonicalCutoverFailure.observationWindowIncomplete,
            CanonicalRecordingMetadataStageEvidenceBlocker.previousStageInsufficientSuccess => CanonicalCutoverFailure.canaryStageBlocked,
            CanonicalRecordingMetadataStageEvidenceBlocker.previousStageFailure => CanonicalCutoverFailure.previousStageFailure,
            CanonicalRecordingMetadataStageEvidenceBlocker.previousStageRollbackFailure => CanonicalCutoverFailure.previousStageRollbackFailure,
            CanonicalRecordingMetadataStageEvidenceBlocker.previousStageBlockingDivergence => CanonicalCutoverFailure.previousStageBlockingDivergence,
            CanonicalRecordingMetadataStageEvidenceBlocker.previousStageUnresolvedConflict => CanonicalCutoverFailure.previousStageUnresolvedConflict,
            CanonicalRecordingMetadataStageEvidenceBlocker.previousStagePostconditionFailure => CanonicalCutoverFailure.postconditionMismatch,
            CanonicalRecordingMetadataStageEvidenceBlocker.previousStageUnsupportedObject => CanonicalCutoverFailure.unsupportedObject,
            CanonicalRecordingMetadataStageEvidenceBlocker.ownerApprovalMissing => CanonicalCutoverFailure.missingOwnerApproval,
            CanonicalRecordingMetadataStageEvidenceBlocker.rollbackPlanMissing => CanonicalCutoverFailure.missingRollback,
            CanonicalRecordingMetadataStageEvidenceBlocker.dryRunEquivalenceMissing => CanonicalCutoverFailure.missingDryRunEquivalence,
            CanonicalRecordingMetadataStageEvidenceBlocker.executionShadowMissing => CanonicalCutoverFailure.missingExecutionShadowEvidence,
            CanonicalRecordingMetadataStageEvidenceBlocker.realDataShadowCopyMissing => CanonicalCutoverFailure.missingRealDataShadowCopyEvidence,
            CanonicalRecordingMetadataStageEvidenceBlocker.readOnlyTransportProbeMissing => CanonicalCutoverFailure.missingReadOnlyTransportProbe,
            CanonicalRecordingMetadataStageEvidenceBlocker.productionApplyPortUnavailable => CanonicalCutoverFailure.productionPortUnavailable,
            CanonicalRecordingMetadataStageEvidenceBlocker.legacyFallbackUnavailable => CanonicalCutoverFailure.legacyFallbackUnavailable,
            _ => CanonicalCutoverFailure.disabled
        };

    private CanonicalRecordingMetadataCutoverDiagnostic Diagnostic(
        CanonicalRecordingMetadataCutoverDiagnosticKind kind,
        string? syncRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        string? objectID = null,
        string? action = null,
        string? result = null,
        string? reason = null,
        CanonicalHash? hash = null) =>
        new(kind, syncRunID, trigger, nodeRole,
            objectID: objectID, action: action, result: result, reason: reason, hash: hash);

    private static List<CanonicalRecordingMetadataCutoverDiagnostic> Bounded(
        List<CanonicalRecordingMetadataCutoverDiagnostic> diagnostics, int max) =>
        diagnostics.Take(max).ToList();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalCutoverAppSeamMode
{
    disabled,
    guardedExecuteNoCommit,
    guardedExecuteCommit,
    productionExecute,
    canaryCommit,
}

public sealed class CanonicalCutoverAppSeamPolicy : IEquatable<CanonicalCutoverAppSeamPolicy>
{
    public bool RecordDiagnostics { get; set; }
    public int MaxDiagnosticsEvents { get; set; }
    public bool RequireCutoverEvidence { get; set; }
    public bool BlockCanonicalMoreAggressive { get; set; }
    public bool BlockInsufficientEvidence { get; set; }
    public bool BlockUnsupported { get; set; }
    public bool BlockDivergence { get; set; }
    public int? CanaryMaxObjectsPerSyncRun { get; set; }
    public bool AllowsV87CanaryN1InternalExecution { get; set; }
    public CanonicalRecordingMetadataCanaryStagePolicy? RecordingMetadataCanaryStagePolicy { get; set; }

    public CanonicalCutoverAppSeamPolicy(
        bool recordDiagnostics = true,
        int maxDiagnosticsEvents = 200,
        bool requireCutoverEvidence = true,
        bool blockCanonicalMoreAggressive = true,
        bool blockInsufficientEvidence = true,
        bool blockUnsupported = true,
        bool blockDivergence = true,
        int? canaryMaxObjectsPerSyncRun = 0,
        bool allowsV87CanaryN1InternalExecution = false,
        CanonicalRecordingMetadataCanaryStagePolicy? recordingMetadataCanaryStagePolicy = null)
    {
        RecordDiagnostics = recordDiagnostics;
        MaxDiagnosticsEvents = Math.Max(1, maxDiagnosticsEvents);
        RequireCutoverEvidence = requireCutoverEvidence;
        BlockCanonicalMoreAggressive = blockCanonicalMoreAggressive;
        BlockInsufficientEvidence = blockInsufficientEvidence;
        BlockUnsupported = blockUnsupported;
        BlockDivergence = blockDivergence;
        CanaryMaxObjectsPerSyncRun = canaryMaxObjectsPerSyncRun.HasValue
            ? Math.Max(0, canaryMaxObjectsPerSyncRun.Value)
            : null;
        AllowsV87CanaryN1InternalExecution = allowsV87CanaryN1InternalExecution;
        RecordingMetadataCanaryStagePolicy = recordingMetadataCanaryStagePolicy;
    }

    public int EffectiveCanaryMaxObjectsPerSyncRun => Math.Max(0, CanaryMaxObjectsPerSyncRun ?? 0);
    public CanonicalRecordingMetadataCanaryStagePolicy EffectiveRecordingMetadataCanaryStagePolicy =>
        RecordingMetadataCanaryStagePolicy ?? CanonicalRecordingMetadataCanaryStagePolicy.Disabled;

    public override bool Equals(object? obj) => obj is CanonicalCutoverAppSeamPolicy other && Equals(other);
    public bool Equals(CanonicalCutoverAppSeamPolicy? other) =>
        other is not null &&
        RecordDiagnostics == other.RecordDiagnostics && MaxDiagnosticsEvents == other.MaxDiagnosticsEvents &&
        RequireCutoverEvidence == other.RequireCutoverEvidence &&
        BlockCanonicalMoreAggressive == other.BlockCanonicalMoreAggressive &&
        BlockInsufficientEvidence == other.BlockInsufficientEvidence && BlockUnsupported == other.BlockUnsupported &&
        BlockDivergence == other.BlockDivergence && CanaryMaxObjectsPerSyncRun == other.CanaryMaxObjectsPerSyncRun &&
        AllowsV87CanaryN1InternalExecution == other.AllowsV87CanaryN1InternalExecution &&
        EqualityComparer<CanonicalRecordingMetadataCanaryStagePolicy?>.Default.Equals(RecordingMetadataCanaryStagePolicy, other.RecordingMetadataCanaryStagePolicy);
    public override int GetHashCode() =>
        HashCode.Combine(RecordDiagnostics, MaxDiagnosticsEvents, RequireCutoverEvidence, BlockCanonicalMoreAggressive,
            BlockInsufficientEvidence, BlockUnsupported, BlockDivergence, CanaryMaxObjectsPerSyncRun,
            AllowsV87CanaryN1InternalExecution, RecordingMetadataCanaryStagePolicy);
    public static bool operator ==(CanonicalCutoverAppSeamPolicy left, CanonicalCutoverAppSeamPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalCutoverAppSeamPolicy left, CanonicalCutoverAppSeamPolicy right) => !left.Equals(right);
}

public sealed class CanonicalCutoverAppSeamConfiguration : IEquatable<CanonicalCutoverAppSeamConfiguration>
{
    public bool IsEnabled { get; set; }
    public CanonicalCutoverDomain Domain { get; set; }
    public CanonicalCutoverAppSeamMode Mode { get; set; }
    public CanonicalCutoverAppSeamPolicy Policy { get; set; }
    public CanonicalRecordingMetadataCutoverEvidence Evidence { get; set; }
    public CanonicalCutoverToken? CutoverToken { get; set; }

    public CanonicalCutoverAppSeamConfiguration(
        bool isEnabled = false,
        CanonicalCutoverDomain domain = CanonicalCutoverDomain.recordingMetadata,
        CanonicalCutoverAppSeamMode mode = CanonicalCutoverAppSeamMode.disabled,
        CanonicalCutoverAppSeamPolicy? policy = null,
        CanonicalRecordingMetadataCutoverEvidence? evidence = null,
        CanonicalCutoverToken? cutoverToken = null)
    {
        IsEnabled = isEnabled;
        Domain = domain;
        Mode = isEnabled ? mode : CanonicalCutoverAppSeamMode.disabled;
        Policy = policy ?? new CanonicalCutoverAppSeamPolicy();
        Evidence = evidence ?? new CanonicalRecordingMetadataCutoverEvidence();
        CutoverToken = cutoverToken;
    }

    public static readonly CanonicalCutoverAppSeamConfiguration Disabled = new();

    public static CanonicalCutoverAppSeamConfiguration Enabled(
        CanonicalCutoverDomain domain = CanonicalCutoverDomain.recordingMetadata,
        CanonicalCutoverAppSeamMode mode = CanonicalCutoverAppSeamMode.guardedExecuteNoCommit,
        CanonicalCutoverAppSeamPolicy? policy = null,
        CanonicalRecordingMetadataCutoverEvidence? evidence = null,
        CanonicalCutoverToken? cutoverToken = null) =>
        new(true, domain, mode, policy, evidence, cutoverToken);

    public CanonicalCutoverAppSeamMode EffectiveMode => IsEnabled ? Mode : CanonicalCutoverAppSeamMode.disabled;

    public override bool Equals(object? obj) => obj is CanonicalCutoverAppSeamConfiguration other && Equals(other);
    public bool Equals(CanonicalCutoverAppSeamConfiguration? other) =>
        other is not null && IsEnabled == other.IsEnabled && Domain == other.Domain &&
        Mode == other.Mode && EqualityComparer<CanonicalCutoverAppSeamPolicy>.Default.Equals(Policy, other.Policy) &&
        EqualityComparer<CanonicalRecordingMetadataCutoverEvidence>.Default.Equals(Evidence, other.Evidence) &&
        EqualityComparer<CanonicalCutoverToken?>.Default.Equals(CutoverToken, other.CutoverToken);
    public override int GetHashCode() => HashCode.Combine(IsEnabled, Domain, Mode, Policy, Evidence, CutoverToken);
    public static bool operator ==(CanonicalCutoverAppSeamConfiguration left, CanonicalCutoverAppSeamConfiguration right) => left.Equals(right);
    public static bool operator !=(CanonicalCutoverAppSeamConfiguration left, CanonicalCutoverAppSeamConfiguration right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalCutoverAppSeamFailure
{
    disabled,
    unsupportedDomain,
    unsupportedMode,
    guardedExecuteCommitDenied,
    productionExecuteDenied,
    canaryCommitDenied,
    viewRefreshTriggerDenied,
    retryDrainerFreshMetadataDenied,
    insufficientLocalSnapshot,
    insufficientPeerSnapshot,
    insufficientEvidence,
    unresolvedConflict,
    unsupportedAction,
    unstableMetadataHash,
}

public sealed class CanonicalCutoverAppSeamGate : IEquatable<CanonicalCutoverAppSeamGate>
{
    public CanonicalCutoverDomain Domain { get; set; }
    public CanonicalCutoverAppSeamMode Mode { get; set; }
    public bool Allowed { get; set; }
    public List<CanonicalCutoverAppSeamFailure> Failures { get; set; }
    public string Reason { get; set; }

    public CanonicalCutoverAppSeamGate(
        CanonicalCutoverDomain domain,
        CanonicalCutoverAppSeamMode mode,
        List<CanonicalCutoverAppSeamFailure> failures,
        string reason)
    {
        Domain = domain;
        Mode = mode;
        Failures = new HashSet<CanonicalCutoverAppSeamFailure>(failures).OrderBy(f => f.ToString()).ToList();
        Allowed = Failures.Count == 0;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? (Failures.Count == 0 ? "allowed" : "blocked") ?? "unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalCutoverAppSeamGate other && Equals(other);
    public bool Equals(CanonicalCutoverAppSeamGate? other) =>
        other is not null && Domain == other.Domain && Mode == other.Mode &&
        Allowed == other.Allowed && Failures.SequenceEqual(other.Failures) && Reason == other.Reason;
    public override int GetHashCode() => HashCode.Combine(Domain, Mode, Allowed, Failures.Count, Reason);
    public static bool operator ==(CanonicalCutoverAppSeamGate left, CanonicalCutoverAppSeamGate right) => left.Equals(right);
    public static bool operator !=(CanonicalCutoverAppSeamGate left, CanonicalCutoverAppSeamGate right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataNoCommitDirection
{
    none,
    apply,
    send
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataNoCommitOutcome
{
    noCommitWouldApply,
    noCommitWouldSend,
    noCommitEquivalent,
    noCommitDivergent,
    noCommitBlocked,
    noCommitInsufficientEvidence,
    noCommitProductionCommitSuppressed,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataNoCommitEquivalenceStatus
{
    equivalent,
    canonicalMoreConservative,
    canonicalMoreAggressive,
    divergent,
    insufficientEvidence,
    unsupported,
    blocked,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataNoCommitFailure
{
    appSeamBlocked,
    unsupportedAction,
    unsupportedNoCommitPayloadBuilder,
    insufficientEvidence,
    canonicalMoreAggressive,
    divergent,
    stagingFailed,
    productionCommitSuppressed,
}

public sealed class CanonicalRecordingMetadataNoCommitCandidate : IEquatable<CanonicalRecordingMetadataNoCommitCandidate>
{
    public string Id => CutoverCandidate.Id;
    public CanonicalRecordingMetadataCutoverCandidate CutoverCandidate { get; set; }
    public CanonicalRecordingMetadataNoCommitDirection LegacyDirection { get; set; }
    public string? LegacyObjectID { get; set; }
    public int? LegacyPayloadByteCount { get; set; }
    public string? LegacyPayloadHashPrefix { get; set; }
    public string? ExpectedRoutePath { get; set; }

    public CanonicalRecordingMetadataNoCommitCandidate(
        CanonicalRecordingMetadataCutoverCandidate cutoverCandidate,
        CanonicalRecordingMetadataNoCommitDirection legacyDirection,
        string? legacyObjectID = null,
        int? legacyPayloadByteCount = null,
        string? legacyPayloadHashPrefix = null,
        string? expectedRoutePath = null)
    {
        CutoverCandidate = cutoverCandidate;
        LegacyDirection = legacyDirection;
        LegacyObjectID = legacyObjectID != null
            ? CanonicalProductionRedaction.SafeIdentifier(legacyObjectID, cutoverCandidate.ObjectID)
            : null;
        LegacyPayloadByteCount = legacyPayloadByteCount;
        LegacyPayloadHashPrefix = CanonicalProductionRedaction.HashPrefix(legacyPayloadHashPrefix);
        ExpectedRoutePath = expectedRoutePath != null
            ? CanonicalProductionRedaction.SafeDiagnosticText(expectedRoutePath)
            : null;
    }

    public CanonicalRecordingMetadataNoCommitDirection CanonicalDirection => CutoverCandidate.CutoverActionKind switch
    {
        CanonicalRecordingMetadataCutoverActionKind.apply => CanonicalRecordingMetadataNoCommitDirection.apply,
        CanonicalRecordingMetadataCutoverActionKind.send => CanonicalRecordingMetadataNoCommitDirection.send,
        _ => CanonicalRecordingMetadataNoCommitDirection.none
    };

    public string ObjectID => CutoverCandidate.ObjectID;

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataNoCommitCandidate other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataNoCommitCandidate? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalRecordingMetadataNoCommitCandidate left, CanonicalRecordingMetadataNoCommitCandidate right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataNoCommitCandidate left, CanonicalRecordingMetadataNoCommitCandidate right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataNoCommitPayloadSummary : IEquatable<CanonicalRecordingMetadataNoCommitPayloadSummary>
{
    public string Schema { get; set; }
    public string ActionID { get; set; }
    public string ObjectID { get; set; }
    public CanonicalRecordingMetadataNoCommitDirection Direction { get; set; }
    public CanonicalApplyBridgeHint? BridgeHint { get; set; }
    public string? RoutePath { get; set; }
    public string? MetadataHashPrefix { get; set; }
    public string? ModifiedAtUnixSeconds { get; set; }
    public bool Tombstone { get; set; }

    public CanonicalRecordingMetadataNoCommitPayloadSummary(CanonicalRecordingMetadataNoCommitCandidate candidate)
    {
        var expected = candidate.CutoverCandidate.ExpectedObject;
        Schema = "canonical-recording-metadata-no-commit-v8";
        ActionID = CanonicalProductionRedaction.SafeIdentifier(
            candidate.CutoverCandidate.Action.ActionID, candidate.CanonicalDirection.ToString())!;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(candidate.ObjectID, "unknown-recording")!;
        Direction = candidate.CanonicalDirection;
        BridgeHint = candidate.CutoverCandidate.Action.BridgeHint;
        RoutePath = candidate.CanonicalDirection == CanonicalRecordingMetadataNoCommitDirection.send ? "/sync/apply-metadata" : null;
        MetadataHashPrefix = CanonicalProductionRedaction.HashPrefix(expected?.MetadataHash.Value);
        ModifiedAtUnixSeconds = expected?.Metadata.ModifiedAt.Date.HasValue == true
            ? NumberString(expected.Metadata.ModifiedAt.Date.Value.ToUnixTimeSeconds())
            : null;
        Tombstone = expected?.Metadata.IsDeleted ?? false;
    }

    public byte[] EncodedBytes()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    private static string NumberString(double value) =>
        value.ToString("F6", CultureInfo.InvariantCulture);

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataNoCommitPayloadSummary other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataNoCommitPayloadSummary? other) =>
        other is not null && Schema == other.Schema && ActionID == other.ActionID &&
        ObjectID == other.ObjectID && Direction == other.Direction &&
        BridgeHint == other.BridgeHint && RoutePath == other.RoutePath &&
        MetadataHashPrefix == other.MetadataHashPrefix &&
        ModifiedAtUnixSeconds == other.ModifiedAtUnixSeconds && Tombstone == other.Tombstone;
    public override int GetHashCode() =>
        HashCode.Combine(Schema, ActionID, ObjectID, Direction, BridgeHint, RoutePath, MetadataHashPrefix,
            ModifiedAtUnixSeconds, Tombstone);
    public static bool operator ==(CanonicalRecordingMetadataNoCommitPayloadSummary left, CanonicalRecordingMetadataNoCommitPayloadSummary right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataNoCommitPayloadSummary left, CanonicalRecordingMetadataNoCommitPayloadSummary right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataNoCommitStagingResult : IEquatable<CanonicalRecordingMetadataNoCommitStagingResult>
{
    public string ActionID { get; set; }
    public string ObjectID { get; set; }
    public CanonicalRecordingMetadataNoCommitDirection Direction { get; set; }
    public bool Staged { get; set; }
    public bool WroteOnlyStagingRoot { get; set; }
    public bool WouldApply { get; set; }
    public bool WouldSend { get; set; }
    public string? RoutePath { get; set; }
    public string? StagedLogicalPathToken { get; set; }
    public int? PayloadByteCount { get; set; }
    public string? PayloadHashPrefix { get; set; }
    public string? MetadataHashPrefix { get; set; }
    public bool CalledApplySyncManifest { get; set; }
    public bool SentNetworkRequest { get; set; }
    public bool WroteProductionStore { get; set; }
    public bool SuppressedLegacyDuplicate { get; set; }
    public CanonicalNoCommitStagingEvidence? StagingEvidence { get; set; }
    public CanonicalNoCommitCleanupEvidence? CleanupEvidence { get; set; }
    public CanonicalRecordingMetadataNoCommitFailure? Failure { get; set; }
    public string Reason { get; set; }

    public CanonicalRecordingMetadataNoCommitStagingResult(
        CanonicalRecordingMetadataNoCommitCandidate candidate,
        bool staged,
        bool wroteOnlyStagingRoot,
        string? routePath = null,
        string? stagedLogicalPathToken = null,
        int? payloadByteCount = null,
        string? payloadHashPrefix = null,
        CanonicalNoCommitStagingEvidence? stagingEvidence = null,
        CanonicalNoCommitCleanupEvidence? cleanupEvidence = null,
        CanonicalRecordingMetadataNoCommitFailure? failure = null,
        string reason = "")
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(
            candidate.CutoverCandidate.Action.ActionID, candidate.CanonicalDirection.ToString())!;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(candidate.ObjectID, "unknown-recording")!;
        Direction = candidate.CanonicalDirection;
        Staged = staged;
        WroteOnlyStagingRoot = wroteOnlyStagingRoot;
        WouldApply = candidate.CanonicalDirection == CanonicalRecordingMetadataNoCommitDirection.apply;
        WouldSend = candidate.CanonicalDirection == CanonicalRecordingMetadataNoCommitDirection.send;
        RoutePath = routePath != null ? CanonicalProductionRedaction.SafeDiagnosticText(routePath) : null;
        StagedLogicalPathToken = CanonicalProjectionContract.SafeLogicalPathToken(stagedLogicalPathToken);
        PayloadByteCount = payloadByteCount;
        PayloadHashPrefix = CanonicalProductionRedaction.HashPrefix(payloadHashPrefix);
        MetadataHashPrefix = candidate.CutoverCandidate.StableMetadataHash is { } h
            ? CanonicalProductionRedaction.HashPrefix(h.Value) : null;
        CalledApplySyncManifest = false;
        SentNetworkRequest = false;
        WroteProductionStore = false;
        SuppressedLegacyDuplicate = false;
        StagingEvidence = stagingEvidence;
        CleanupEvidence = cleanupEvidence;
        Failure = failure;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason)
            ?? (staged ? "noCommitStaged" : "noCommitFailed") ?? "unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataNoCommitStagingResult other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataNoCommitStagingResult? other) =>
        other is not null && ActionID == other.ActionID && ObjectID == other.ObjectID &&
        Direction == other.Direction && Staged == other.Staged &&
        WroteOnlyStagingRoot == other.WroteOnlyStagingRoot &&
        WouldApply == other.WouldApply && WouldSend == other.WouldSend &&
        RoutePath == other.RoutePath && StagedLogicalPathToken == other.StagedLogicalPathToken &&
        PayloadByteCount == other.PayloadByteCount && PayloadHashPrefix == other.PayloadHashPrefix &&
        MetadataHashPrefix == other.MetadataHashPrefix && CalledApplySyncManifest == other.CalledApplySyncManifest &&
        SentNetworkRequest == other.SentNetworkRequest && WroteProductionStore == other.WroteProductionStore &&
        SuppressedLegacyDuplicate == other.SuppressedLegacyDuplicate &&
        EqualityComparer<CanonicalNoCommitStagingEvidence?>.Default.Equals(StagingEvidence, other.StagingEvidence) &&
        EqualityComparer<CanonicalNoCommitCleanupEvidence?>.Default.Equals(CleanupEvidence, other.CleanupEvidence) &&
        Failure == other.Failure && Reason == other.Reason;
    public override int GetHashCode() =>
        HashCode.Combine(ActionID, ObjectID, Direction, Staged, WroteOnlyStagingRoot, WouldApply, WouldSend,
            RoutePath, StagedLogicalPathToken, PayloadByteCount, PayloadHashPrefix, MetadataHashPrefix,
            CalledApplySyncManifest, SentNetworkRequest, WroteProductionStore, SuppressedLegacyDuplicate,
            StagingEvidence, CleanupEvidence, Failure, Reason);
    public static bool operator ==(CanonicalRecordingMetadataNoCommitStagingResult left, CanonicalRecordingMetadataNoCommitStagingResult right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataNoCommitStagingResult left, CanonicalRecordingMetadataNoCommitStagingResult right) => !left.Equals(right);
}

public interface ICanonicalRecordingMetadataNoCommitExecutor
{
    CanonicalRecordingMetadataNoCommitStagingResult StageNoCommit(CanonicalRecordingMetadataNoCommitCandidate candidate);
}

public sealed class CanonicalRecordingMetadataNoCommitEquivalence : IEquatable<CanonicalRecordingMetadataNoCommitEquivalence>
{
    public CanonicalRecordingMetadataNoCommitEquivalenceStatus Status { get; set; }
    public bool Blocking { get; set; }
    public List<string> Reasons { get; set; }
    public CanonicalRecordingMetadataNoCommitDirection CanonicalDirection { get; set; }
    public CanonicalRecordingMetadataNoCommitDirection LegacyDirection { get; set; }
    public string? MetadataHashPrefix { get; set; }
    public string? ModifiedAtDirection { get; set; }
    public string TombstoneState { get; set; }
    public string? RoutePath { get; set; }
    public int? PayloadByteCount { get; set; }
    public string? PayloadHashPrefix { get; set; }

    public CanonicalRecordingMetadataNoCommitEquivalence(
        CanonicalRecordingMetadataNoCommitEquivalenceStatus status,
        bool blocking,
        List<string> reasons,
        CanonicalRecordingMetadataNoCommitDirection canonicalDirection,
        CanonicalRecordingMetadataNoCommitDirection legacyDirection,
        string? metadataHashPrefix,
        string? modifiedAtDirection,
        string tombstoneState,
        string? routePath,
        int? payloadByteCount,
        string? payloadHashPrefix)
    {
        Status = status;
        Blocking = blocking;
        Reasons = new HashSet<string>(reasons.Select(r => CanonicalProductionRedaction.SafeDiagnosticText(r) ?? r))
            .OrderBy(r => r).ToList();
        CanonicalDirection = canonicalDirection;
        LegacyDirection = legacyDirection;
        MetadataHashPrefix = CanonicalProductionRedaction.HashPrefix(metadataHashPrefix);
        ModifiedAtDirection = CanonicalProductionRedaction.SafeDiagnosticText(modifiedAtDirection);
        TombstoneState = CanonicalProductionRedaction.SafeDiagnosticText(tombstoneState) ?? "unknown";
        RoutePath = routePath != null ? CanonicalProductionRedaction.SafeDiagnosticText(routePath) : null;
        PayloadByteCount = payloadByteCount;
        PayloadHashPrefix = CanonicalProductionRedaction.HashPrefix(payloadHashPrefix);
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataNoCommitEquivalence other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataNoCommitEquivalence? other) =>
        other is not null && Status == other.Status && Blocking == other.Blocking &&
        Reasons.SequenceEqual(other.Reasons) && CanonicalDirection == other.CanonicalDirection &&
        LegacyDirection == other.LegacyDirection && MetadataHashPrefix == other.MetadataHashPrefix &&
        ModifiedAtDirection == other.ModifiedAtDirection && TombstoneState == other.TombstoneState &&
        RoutePath == other.RoutePath && PayloadByteCount == other.PayloadByteCount &&
        PayloadHashPrefix == other.PayloadHashPrefix;
    public override int GetHashCode() =>
        HashCode.Combine(Status, Blocking, Reasons.Count, CanonicalDirection, LegacyDirection,
            MetadataHashPrefix, ModifiedAtDirection, TombstoneState, RoutePath, PayloadByteCount, PayloadHashPrefix);
    public static bool operator ==(CanonicalRecordingMetadataNoCommitEquivalence left, CanonicalRecordingMetadataNoCommitEquivalence right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataNoCommitEquivalence left, CanonicalRecordingMetadataNoCommitEquivalence right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalV8RecordingMetadataNoCommitDiagnosticKind
{
    canonicalV8CutoverSeamStarted,
    canonicalV8CutoverSeamCompleted,
    canonicalV8CutoverSeamBlocked,
    canonicalV8RecordingMetadataNoCommitStarted,
    canonicalV8RecordingMetadataNoCommitCompleted,
    canonicalV8RecordingMetadataNoCommitDivergent,
    canonicalV8RecordingMetadataNoCommitEquivalent,
    canonicalV8RecordingMetadataNoCommitProductionCommitSuppressed,
    canonicalV8RecordingMetadataNoCommitInsufficientEvidence,
    canonicalV8RecordingMetadataNoCommitUnsupported,
    canonicalV8RecordingMetadataNoCommitLegacyFallbackPreserved,
    canonicalV8NoCommitStagingRootCreated,
    canonicalV8NoCommitStagingRootCleaned,
    canonicalV8NoCommitStagingRootCleanupFailed,
    canonicalV8NoCommitEvidenceReportBuilt,
    canonicalV8NoCommitEquivalent,
    canonicalV8NoCommitDivergent,
    canonicalV8NoCommitCommitSuppressed,
    canonicalV8NoCommitLegacyDuplicatePreserved,
    canonicalV8NoCommitConfigStageResolved,
    canonicalV8NoCommitConfigBlocked,
}

public sealed class CanonicalV8RecordingMetadataNoCommitDiagnostic : IEquatable<CanonicalV8RecordingMetadataNoCommitDiagnostic>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "run", Result ?? "", Reason ?? "");

    public CanonicalV8RecordingMetadataNoCommitDiagnosticKind Kind { get; set; }
    public string? SyncRunID { get; set; }
    public CanonicalSyncPlanTrigger Trigger { get; set; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; set; }
    public CanonicalCutoverDomain Domain { get; set; }
    public CanonicalCutoverAppSeamMode Mode { get; set; }
    public string? ObjectID { get; set; }
    public int CandidateCount { get; set; }
    public int EquivalentCount { get; set; }
    public int DivergentCount { get; set; }
    public int BlockerCount { get; set; }
    public string? Result { get; set; }
    public string? Reason { get; set; }
    public string? HashPrefix { get; set; }
    public string? RoutePath { get; set; }

    public CanonicalV8RecordingMetadataNoCommitDiagnostic(
        CanonicalV8RecordingMetadataNoCommitDiagnosticKind kind,
        string? syncRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalCutoverDomain domain = CanonicalCutoverDomain.recordingMetadata,
        CanonicalCutoverAppSeamMode mode = CanonicalCutoverAppSeamMode.disabled,
        string? objectID = null,
        int candidateCount = 0,
        int equivalentCount = 0,
        int divergentCount = 0,
        int blockerCount = 0,
        string? result = null,
        string? reason = null,
        string? hashPrefix = null,
        string? routePath = null)
    {
        Kind = kind;
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        Domain = domain;
        Mode = mode;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording") : null;
        CandidateCount = Math.Max(0, candidateCount);
        EquivalentCount = Math.Max(0, equivalentCount);
        DivergentCount = Math.Max(0, divergentCount);
        BlockerCount = Math.Max(0, blockerCount);
        Result = CanonicalProductionRedaction.SafeDiagnosticText(result);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
        HashPrefix = CanonicalProductionRedaction.HashPrefix(hashPrefix);
        RoutePath = routePath != null ? CanonicalProductionRedaction.SafeDiagnosticText(routePath) : null;
    }

    public string DiagnosticsSummary
    {
        get
        {
            var parts = new List<string?>
            {
                $"trigger={Trigger}",
                $"nodeRole={NodeRole}",
                $"domain={Domain}",
                $"mode={Mode}",
                $"candidateCount={CandidateCount}",
                $"equivalentCount={EquivalentCount}",
                $"divergentCount={DivergentCount}",
                $"blockerCount={BlockerCount}",
                Result != null ? $"result={Result}" : null,
                Reason != null ? $"reason={Reason}" : null,
                HashPrefix != null ? $"hashPrefix={HashPrefix}" : null,
                RoutePath != null ? $"route={RoutePath}" : null,
            };
            return string.Join(",", parts.Where(p => p != null));
        }
    }

    public override bool Equals(object? obj) => obj is CanonicalV8RecordingMetadataNoCommitDiagnostic other && Equals(other);
    public bool Equals(CanonicalV8RecordingMetadataNoCommitDiagnostic? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalV8RecordingMetadataNoCommitDiagnostic left, CanonicalV8RecordingMetadataNoCommitDiagnostic right) => left.Equals(right);
    public static bool operator !=(CanonicalV8RecordingMetadataNoCommitDiagnostic left, CanonicalV8RecordingMetadataNoCommitDiagnostic right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataNoCommitCandidateResult : IEquatable<CanonicalRecordingMetadataNoCommitCandidateResult>
{
    public string Id => ActionID;
    public string ActionID { get; set; }
    public string ObjectID { get; set; }
    public List<CanonicalRecordingMetadataNoCommitOutcome> Outcomes { get; set; }
    public CanonicalRecordingMetadataNoCommitEquivalence Equivalence { get; set; }
    public CanonicalRecordingMetadataNoCommitStagingResult? Staging { get; set; }
    public CanonicalRecordingMetadataNoCommitFailure? Failure { get; set; }

    public CanonicalRecordingMetadataNoCommitCandidateResult(
        CanonicalRecordingMetadataNoCommitCandidate candidate,
        List<CanonicalRecordingMetadataNoCommitOutcome> outcomes,
        CanonicalRecordingMetadataNoCommitEquivalence equivalence,
        CanonicalRecordingMetadataNoCommitStagingResult? staging,
        CanonicalRecordingMetadataNoCommitFailure? failure)
    {
        ActionID = CanonicalProductionRedaction.SafeIdentifier(
            candidate.CutoverCandidate.Action.ActionID, candidate.CanonicalDirection.ToString())!;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(candidate.ObjectID, "unknown-recording")!;
        Outcomes = new HashSet<CanonicalRecordingMetadataNoCommitOutcome>(outcomes)
            .OrderBy(o => o.ToString()).ToList();
        Equivalence = equivalence;
        Staging = staging;
        Failure = failure;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataNoCommitCandidateResult other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataNoCommitCandidateResult? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalRecordingMetadataNoCommitCandidateResult left, CanonicalRecordingMetadataNoCommitCandidateResult right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataNoCommitCandidateResult left, CanonicalRecordingMetadataNoCommitCandidateResult right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataNoCommitResult : IEquatable<CanonicalRecordingMetadataNoCommitResult>
{
    public CanonicalCutoverAppSeamGate Gate { get; set; }
    public List<CanonicalRecordingMetadataNoCommitCandidateResult> CandidateResults { get; set; }
    public List<CanonicalV8RecordingMetadataNoCommitDiagnostic> Diagnostics { get; set; }
    public bool LegacyFallbackPreserved { get; set; }
    public bool ProductionCommitSuppressed { get; set; }
    public List<string> DuplicateLegacySuppressedActionIDs { get; set; }
    public int NonfatalFailureCount { get; set; }
    public CanonicalNoCommitEvidenceReport EvidenceReport { get; set; }

    public bool Succeeded => Gate.Allowed && NonfatalFailureCount == 0;

    public CanonicalRecordingMetadataNoCommitResult(
        CanonicalCutoverAppSeamGate gate,
        List<CanonicalRecordingMetadataNoCommitCandidateResult>? candidateResults = null,
        List<CanonicalV8RecordingMetadataNoCommitDiagnostic>? diagnostics = null,
        bool legacyFallbackPreserved = false,
        bool productionCommitSuppressed = false,
        List<string>? duplicateLegacySuppressedActionIDs = null,
        int nonfatalFailureCount = 0,
        CanonicalNoCommitEvidenceReport? evidenceReport = null)
    {
        Gate = gate;
        CandidateResults = candidateResults ?? new List<CanonicalRecordingMetadataNoCommitCandidateResult>();
        Diagnostics = diagnostics ?? new List<CanonicalV8RecordingMetadataNoCommitDiagnostic>();
        LegacyFallbackPreserved = legacyFallbackPreserved;
        ProductionCommitSuppressed = productionCommitSuppressed;
        DuplicateLegacySuppressedActionIDs = duplicateLegacySuppressedActionIDs ?? new List<string>();
        NonfatalFailureCount = nonfatalFailureCount;
        EvidenceReport = evidenceReport ?? new CanonicalNoCommitEvidenceReport();
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataNoCommitResult other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataNoCommitResult? other) =>
        other is not null &&
        EqualityComparer<CanonicalCutoverAppSeamGate>.Default.Equals(Gate, other.Gate) &&
        CandidateResults.SequenceEqual(other.CandidateResults) &&
        Diagnostics.SequenceEqual(other.Diagnostics) &&
        LegacyFallbackPreserved == other.LegacyFallbackPreserved &&
        ProductionCommitSuppressed == other.ProductionCommitSuppressed &&
        DuplicateLegacySuppressedActionIDs.SequenceEqual(other.DuplicateLegacySuppressedActionIDs) &&
        NonfatalFailureCount == other.NonfatalFailureCount &&
        EqualityComparer<CanonicalNoCommitEvidenceReport>.Default.Equals(EvidenceReport, other.EvidenceReport);
    public override int GetHashCode() =>
        HashCode.Combine(Gate, CandidateResults.Count, Diagnostics.Count, LegacyFallbackPreserved,
            ProductionCommitSuppressed, DuplicateLegacySuppressedActionIDs.Count, NonfatalFailureCount, EvidenceReport);
    public static bool operator ==(CanonicalRecordingMetadataNoCommitResult left, CanonicalRecordingMetadataNoCommitResult right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataNoCommitResult left, CanonicalRecordingMetadataNoCommitResult right) => !left.Equals(right);
}

public sealed class CanonicalCutoverAppSeamResult : IEquatable<CanonicalCutoverAppSeamResult>
{
    public CanonicalRecordingMetadataNoCommitResult NoCommitResult { get; set; }
    public bool LegacyPlanUnchanged { get; set; }
    public bool ProductionPlanUnchanged { get; set; }

    public List<CanonicalV8RecordingMetadataNoCommitDiagnostic> Diagnostics => NoCommitResult.Diagnostics;

    public CanonicalCutoverAppSeamResult(
        CanonicalRecordingMetadataNoCommitResult noCommitResult,
        bool legacyPlanUnchanged = true,
        bool productionPlanUnchanged = true)
    {
        NoCommitResult = noCommitResult;
        LegacyPlanUnchanged = legacyPlanUnchanged;
        ProductionPlanUnchanged = productionPlanUnchanged;
    }

    public override bool Equals(object? obj) => obj is CanonicalCutoverAppSeamResult other && Equals(other);
    public bool Equals(CanonicalCutoverAppSeamResult? other) =>
        other is not null &&
        EqualityComparer<CanonicalRecordingMetadataNoCommitResult>.Default.Equals(NoCommitResult, other.NoCommitResult) &&
        LegacyPlanUnchanged == other.LegacyPlanUnchanged &&
        ProductionPlanUnchanged == other.ProductionPlanUnchanged;
    public override int GetHashCode() =>
        HashCode.Combine(NoCommitResult, LegacyPlanUnchanged, ProductionPlanUnchanged);
    public static bool operator ==(CanonicalCutoverAppSeamResult left, CanonicalCutoverAppSeamResult right) => left.Equals(right);
    public static bool operator !=(CanonicalCutoverAppSeamResult left, CanonicalCutoverAppSeamResult right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataCanaryPolicy : IEquatable<CanonicalRecordingMetadataCanaryPolicy>
{
    public int MaxObjectsPerSyncRun { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool AllowsV87CanaryN1InternalExecution { get; set; }
    public CanonicalRecordingMetadataCanaryStagePolicy? RecordingMetadataCanaryStagePolicy { get; set; }

    public CanonicalRecordingMetadataCanaryPolicy(
        int maxObjectsPerSyncRun = 0,
        bool runtimeSwitchEnabled = false,
        bool allowsV87CanaryN1InternalExecution = false,
        CanonicalRecordingMetadataCanaryStagePolicy? recordingMetadataCanaryStagePolicy = null)
    {
        MaxObjectsPerSyncRun = Math.Max(0, maxObjectsPerSyncRun);
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        AllowsV87CanaryN1InternalExecution = allowsV87CanaryN1InternalExecution;
        RecordingMetadataCanaryStagePolicy = recordingMetadataCanaryStagePolicy;
    }

    public bool IsZeroBudget => MaxObjectsPerSyncRun == 0;

    public string DiagnosticsSummary => string.Join(",",
        $"canaryMaxObjectsPerSyncRun={MaxObjectsPerSyncRun}",
        $"runtimeSwitchEnabled={RuntimeSwitchEnabled}",
        $"allowsV87CanaryN1InternalExecution={AllowsV87CanaryN1InternalExecution}",
        $"canaryStage={RecordingMetadataCanaryStagePolicy?.RequestedStage.ToString() ?? CanonicalRecordingMetadataCanaryStage.disabled.ToString()}");

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCanaryPolicy other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCanaryPolicy? other) =>
        other is not null &&
        MaxObjectsPerSyncRun == other.MaxObjectsPerSyncRun &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        AllowsV87CanaryN1InternalExecution == other.AllowsV87CanaryN1InternalExecution &&
        EqualityComparer<CanonicalRecordingMetadataCanaryStagePolicy?>.Default.Equals(RecordingMetadataCanaryStagePolicy, other.RecordingMetadataCanaryStagePolicy);
    public override int GetHashCode() =>
        HashCode.Combine(MaxObjectsPerSyncRun, RuntimeSwitchEnabled, AllowsV87CanaryN1InternalExecution, RecordingMetadataCanaryStagePolicy);
    public static bool operator ==(CanonicalRecordingMetadataCanaryPolicy left, CanonicalRecordingMetadataCanaryPolicy right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCanaryPolicy left, CanonicalRecordingMetadataCanaryPolicy right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataRollbackPlanSummary : IEquatable<CanonicalRecordingMetadataRollbackPlanSummary>
{
    public string? PlanID { get; set; }
    public int CheckpointCount { get; set; }
    public int ActionCount { get; set; }
    public bool CoversRecordingMetadata { get; set; }
    public bool RollbackVerified { get; set; }
    public bool RollbackRehearsalPassed { get; set; }

    public CanonicalRecordingMetadataRollbackPlanSummary(
        CanonicalRollbackPlan? plan,
        bool rollbackVerified,
        bool rollbackRehearsalPassed)
    {
        PlanID = plan != null ? CanonicalProductionRedaction.SafeDiagnosticText(plan.PlanID) : null;
        CheckpointCount = plan?.Checkpoints.Count ?? 0;
        ActionCount = plan?.Actions.Count ?? 0;
        CoversRecordingMetadata = plan?.Covers(CanonicalProductionDomain.recordingMetadata) ?? false;
        RollbackVerified = rollbackVerified;
        RollbackRehearsalPassed = rollbackRehearsalPassed;
    }

    public string DiagnosticsSummary =>
        string.Join(",",
            new List<string?>
            {
                PlanID != null ? $"rollbackPlan={PlanID}" : null,
                $"checkpointCount={CheckpointCount}",
                $"actionCount={ActionCount}",
                $"coversRecordingMetadata={CoversRecordingMetadata}",
                $"rollbackVerified={RollbackVerified}",
                $"rollbackRehearsalPassed={RollbackRehearsalPassed}"
            }.Where(p => p != null));

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataRollbackPlanSummary other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataRollbackPlanSummary? other) =>
        other is not null && PlanID == other.PlanID && CheckpointCount == other.CheckpointCount &&
        ActionCount == other.ActionCount && CoversRecordingMetadata == other.CoversRecordingMetadata &&
        RollbackVerified == other.RollbackVerified && RollbackRehearsalPassed == other.RollbackRehearsalPassed;
    public override int GetHashCode() =>
        HashCode.Combine(PlanID, CheckpointCount, ActionCount, CoversRecordingMetadata, RollbackVerified, RollbackRehearsalPassed);
    public static bool operator ==(CanonicalRecordingMetadataRollbackPlanSummary left, CanonicalRecordingMetadataRollbackPlanSummary right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataRollbackPlanSummary left, CanonicalRecordingMetadataRollbackPlanSummary right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataProductionApplyPortReadiness : IEquatable<CanonicalRecordingMetadataProductionApplyPortReadiness>
{
    public bool ProductionPortAvailable { get; set; }
    public CanonicalRecordingMetadataApplyPortMode ApplyPortMode { get; set; }
    public bool RealRootBoundApplyPortAvailable { get; set; }
    public bool RootBoundWriteAvailable { get; set; }
    public bool AtomicReplaceAvailable { get; set; }
    public bool RollbackCheckpointAvailable { get; set; }
    public bool ProductionRootDisabledByDefault { get; set; }
    public bool TestRootUsed { get; set; }

    public CanonicalRecordingMetadataProductionApplyPortReadiness(CanonicalRecordingMetadataCutoverEvidence evidence)
    {
        ProductionPortAvailable = evidence.ProductionPortAvailable;
        ApplyPortMode = evidence.ApplyPortMode;
        RealRootBoundApplyPortAvailable = evidence.RealRootBoundApplyPortAvailable;
        RootBoundWriteAvailable = evidence.RootBoundWriteAvailable;
        AtomicReplaceAvailable = evidence.AtomicReplaceAvailable;
        RollbackCheckpointAvailable = evidence.RollbackCheckpointAvailable;
        ProductionRootDisabledByDefault = evidence.ProductionRootDisabledByDefault;
        TestRootUsed = evidence.TestRootUsed;
    }

    public bool ReadyForGuardedCommit =>
        ProductionPortAvailable
        && RealRootBoundApplyPortAvailable
        && canonicalRecordingMetadataApplyPortModeExtensions.IsNonDryRunRootBound(ApplyPortMode)
        && RootBoundWriteAvailable
        && AtomicReplaceAvailable
        && RollbackCheckpointAvailable
        && ProductionRootDisabledByDefault
        && (ApplyPortMode != CanonicalRecordingMetadataApplyPortMode.testRootBound || TestRootUsed);

    public string DiagnosticsSummary => string.Join(",",
        $"productionPortAvailable={ProductionPortAvailable}",
        $"applyPortMode={ApplyPortMode}",
        $"realRootBoundApplyPortAvailable={RealRootBoundApplyPortAvailable}",
        $"rootBoundWriteAvailable={RootBoundWriteAvailable}",
        $"atomicReplaceAvailable={AtomicReplaceAvailable}",
        $"rollbackCheckpointAvailable={RollbackCheckpointAvailable}",
        $"productionRootDisabledByDefault={ProductionRootDisabledByDefault}",
        $"testRootUsed={TestRootUsed}",
        $"readyForGuardedCommit={ReadyForGuardedCommit}");

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataProductionApplyPortReadiness other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataProductionApplyPortReadiness? other) =>
        other is not null && ProductionPortAvailable == other.ProductionPortAvailable &&
        ApplyPortMode == other.ApplyPortMode &&
        RealRootBoundApplyPortAvailable == other.RealRootBoundApplyPortAvailable &&
        RootBoundWriteAvailable == other.RootBoundWriteAvailable &&
        AtomicReplaceAvailable == other.AtomicReplaceAvailable &&
        RollbackCheckpointAvailable == other.RollbackCheckpointAvailable &&
        ProductionRootDisabledByDefault == other.ProductionRootDisabledByDefault &&
        TestRootUsed == other.TestRootUsed;
    public override int GetHashCode() =>
        HashCode.Combine(ProductionPortAvailable, ApplyPortMode, RealRootBoundApplyPortAvailable,
            RootBoundWriteAvailable, AtomicReplaceAvailable, RollbackCheckpointAvailable,
            ProductionRootDisabledByDefault, TestRootUsed);
    public static bool operator ==(CanonicalRecordingMetadataProductionApplyPortReadiness left, CanonicalRecordingMetadataProductionApplyPortReadiness right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataProductionApplyPortReadiness left, CanonicalRecordingMetadataProductionApplyPortReadiness right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataProductionTransportPortReadiness : IEquatable<CanonicalRecordingMetadataProductionTransportPortReadiness>
{
    public bool ProductionPortAvailable { get; set; }
    public bool ApplyMetadataRouteAvailable { get; set; }
    public bool ReadOnlyTransportProbePassed { get; set; }
    public bool RealNetworkExecutionEnabled { get; set; }

    public CanonicalRecordingMetadataProductionTransportPortReadiness(
        CanonicalRecordingMetadataCutoverEvidence evidence,
        bool applyMetadataRouteAvailable = true,
        bool realNetworkExecutionEnabled = false)
    {
        ProductionPortAvailable = evidence.ProductionPortAvailable;
        ApplyMetadataRouteAvailable = applyMetadataRouteAvailable;
        ReadOnlyTransportProbePassed = evidence.ReadOnlyTransportProbePassed;
        RealNetworkExecutionEnabled = realNetworkExecutionEnabled;
    }

    public bool ReadyForGuardedCommit(bool sendNeeded)
    {
        if (!sendNeeded) return true;
        return ProductionPortAvailable
            && ApplyMetadataRouteAvailable
            && ReadOnlyTransportProbePassed;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"productionPortAvailable={ProductionPortAvailable}",
        $"applyMetadataRouteAvailable={ApplyMetadataRouteAvailable}",
        $"readOnlyTransportProbePassed={ReadOnlyTransportProbePassed}",
        $"realNetworkExecutionEnabled={RealNetworkExecutionEnabled}");

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataProductionTransportPortReadiness other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataProductionTransportPortReadiness? other) =>
        other is not null && ProductionPortAvailable == other.ProductionPortAvailable &&
        ApplyMetadataRouteAvailable == other.ApplyMetadataRouteAvailable &&
        ReadOnlyTransportProbePassed == other.ReadOnlyTransportProbePassed &&
        RealNetworkExecutionEnabled == other.RealNetworkExecutionEnabled;
    public override int GetHashCode() =>
        HashCode.Combine(ProductionPortAvailable, ApplyMetadataRouteAvailable, ReadOnlyTransportProbePassed, RealNetworkExecutionEnabled);
    public static bool operator ==(CanonicalRecordingMetadataProductionTransportPortReadiness left, CanonicalRecordingMetadataProductionTransportPortReadiness right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataProductionTransportPortReadiness left, CanonicalRecordingMetadataProductionTransportPortReadiness right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataCommitEvidenceStatus
{
    complete,
    incomplete,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataCommitEvidenceMissingReason
{
    missingOwnerApprovedToken,
    missingRealDataShadowCopyEvidence,
    missingExecutionShadowEvidence,
    missingDryRunEquivalence,
    blockingDivergence,
    unresolvedConflict,
    missingReadOnlyTransportProbe,
    productionPortUnavailable,
    rootBoundApplyPortUnavailable,
    applyPortDryRunOnly,
    rootBoundWriteUnavailable,
    atomicReplaceUnavailable,
    rollbackCheckpointUnavailable,
    rollbackPlanMissing,
    rollbackPlanDoesNotCoverRecordingMetadata,
    rollbackVerificationMissing,
    rollbackRehearsalMissing,
    productionRootEnabledByDefault,
    testRootMissing,
    legacyFallbackUnavailable,
    productionExecutionGuardMissing,
    canaryBudgetNonZero,
    missingInternalCanaryConfiguration,
    canaryBudgetAboveOne,
}

public sealed class CanonicalRecordingMetadataCommitEvidenceReport : IEquatable<CanonicalRecordingMetadataCommitEvidenceReport>
{
    public CanonicalRecordingMetadataCommitEvidenceStatus Status { get; set; }
    public List<CanonicalRecordingMetadataCommitEvidenceMissingReason> MissingReasons { get; set; }
    public CanonicalRecordingMetadataRollbackPlanSummary RollbackPlanSummary { get; set; }
    public CanonicalRecordingMetadataProductionApplyPortReadiness ApplyPortReadiness { get; set; }
    public CanonicalRecordingMetadataProductionTransportPortReadiness TransportPortReadiness { get; set; }
    public CanonicalRecordingMetadataCanaryPolicy CanaryPolicy { get; set; }
    public bool LocalSnapshotAvailable { get; set; }
    public bool PeerSnapshotAvailable { get; set; }
    public int CandidateCount { get; set; }
    public int LegacyActionCandidateCount { get; set; }
    public int UnresolvedConflictCount { get; set; }
    public string? RealDataShadowCopySummary { get; set; }
    public string? ExecutionShadowSummary { get; set; }
    public string? ReadOnlyTransportProbeSummary { get; set; }

    public CanonicalRecordingMetadataCommitEvidenceReport(
        List<CanonicalRecordingMetadataCommitEvidenceMissingReason> missingReasons,
        CanonicalRecordingMetadataRollbackPlanSummary rollbackPlanSummary,
        CanonicalRecordingMetadataProductionApplyPortReadiness applyPortReadiness,
        CanonicalRecordingMetadataProductionTransportPortReadiness transportPortReadiness,
        CanonicalRecordingMetadataCanaryPolicy canaryPolicy,
        bool localSnapshotAvailable,
        bool peerSnapshotAvailable,
        int candidateCount,
        int legacyActionCandidateCount,
        int unresolvedConflictCount,
        string? realDataShadowCopySummary = null,
        string? executionShadowSummary = null,
        string? readOnlyTransportProbeSummary = null)
    {
        var normalizedReasons = new HashSet<CanonicalRecordingMetadataCommitEvidenceMissingReason>(missingReasons)
            .OrderBy(r => r.ToString()).ToList();
        Status = normalizedReasons.Count == 0
            ? CanonicalRecordingMetadataCommitEvidenceStatus.complete
            : CanonicalRecordingMetadataCommitEvidenceStatus.incomplete;
        MissingReasons = normalizedReasons;
        RollbackPlanSummary = rollbackPlanSummary;
        ApplyPortReadiness = applyPortReadiness;
        TransportPortReadiness = transportPortReadiness;
        CanaryPolicy = canaryPolicy;
        LocalSnapshotAvailable = localSnapshotAvailable;
        PeerSnapshotAvailable = peerSnapshotAvailable;
        CandidateCount = Math.Max(0, candidateCount);
        LegacyActionCandidateCount = Math.Max(0, legacyActionCandidateCount);
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        RealDataShadowCopySummary = realDataShadowCopySummary != null
            ? CanonicalProductionRedaction.SafeDiagnosticText(realDataShadowCopySummary) : null;
        ExecutionShadowSummary = executionShadowSummary != null
            ? CanonicalProductionRedaction.SafeDiagnosticText(executionShadowSummary) : null;
        ReadOnlyTransportProbeSummary = readOnlyTransportProbeSummary != null
            ? CanonicalProductionRedaction.SafeDiagnosticText(readOnlyTransportProbeSummary) : null;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"status={Status}",
        $"missingReasons={string.Join("+", MissingReasons.Select(r => r.ToString()))}",
        $"candidateCount={CandidateCount}",
        $"legacyActionCandidateCount={LegacyActionCandidateCount}",
        $"unresolvedConflictCount={UnresolvedConflictCount}",
        $"localSnapshotAvailable={LocalSnapshotAvailable}",
        $"peerSnapshotAvailable={PeerSnapshotAvailable}",
        $"canaryMaxObjectsPerSyncRun={CanaryPolicy.MaxObjectsPerSyncRun}",
        $"runtimeSwitchEnabled={CanaryPolicy.RuntimeSwitchEnabled}",
        $"allowsV87CanaryN1InternalExecution={CanaryPolicy.AllowsV87CanaryN1InternalExecution}",
        $"applyPortReady={ApplyPortReadiness.ReadyForGuardedCommit}",
        $"transportProbePassed={TransportPortReadiness.ReadOnlyTransportProbePassed}");

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataCommitEvidenceReport other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataCommitEvidenceReport? other) =>
        other is not null && Status == other.Status && MissingReasons.SequenceEqual(other.MissingReasons) &&
        EqualityComparer<CanonicalRecordingMetadataRollbackPlanSummary>.Default.Equals(RollbackPlanSummary, other.RollbackPlanSummary) &&
        EqualityComparer<CanonicalRecordingMetadataProductionApplyPortReadiness>.Default.Equals(ApplyPortReadiness, other.ApplyPortReadiness) &&
        EqualityComparer<CanonicalRecordingMetadataProductionTransportPortReadiness>.Default.Equals(TransportPortReadiness, other.TransportPortReadiness) &&
        EqualityComparer<CanonicalRecordingMetadataCanaryPolicy>.Default.Equals(CanaryPolicy, other.CanaryPolicy) &&
        LocalSnapshotAvailable == other.LocalSnapshotAvailable &&
        PeerSnapshotAvailable == other.PeerSnapshotAvailable &&
        CandidateCount == other.CandidateCount &&
        LegacyActionCandidateCount == other.LegacyActionCandidateCount &&
        UnresolvedConflictCount == other.UnresolvedConflictCount &&
        RealDataShadowCopySummary == other.RealDataShadowCopySummary &&
        ExecutionShadowSummary == other.ExecutionShadowSummary &&
        ReadOnlyTransportProbeSummary == other.ReadOnlyTransportProbeSummary;
    public override int GetHashCode() =>
        HashCode.Combine(Status, MissingReasons.Count, RollbackPlanSummary, ApplyPortReadiness, TransportPortReadiness,
            CanaryPolicy, LocalSnapshotAvailable, PeerSnapshotAvailable, CandidateCount, LegacyActionCandidateCount,
            UnresolvedConflictCount, RealDataShadowCopySummary, ExecutionShadowSummary, ReadOnlyTransportProbeSummary);
    public static bool operator ==(CanonicalRecordingMetadataCommitEvidenceReport left, CanonicalRecordingMetadataCommitEvidenceReport right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataCommitEvidenceReport left, CanonicalRecordingMetadataCommitEvidenceReport right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataGuardedCommitSeamFailure
{
    disabled,
    unsupportedDomain,
    unsupportedMode,
    productionExecuteDenied,
    viewRefreshTriggerDenied,
    retryDrainerFreshMetadataDenied,
    insufficientLocalSnapshot,
    insufficientPeerSnapshot,
    missingToken,
    missingOwnerApproval,
    missingRealDataShadowCopyEvidence,
    missingExecutionShadowEvidence,
    missingDryRunEquivalence,
    blockingDivergence,
    unresolvedConflict,
    missingReadOnlyTransportProbe,
    productionPortUnavailable,
    rootBoundApplyPortUnavailable,
    applyPortDryRunOnly,
    rootBoundWriteUnavailable,
    atomicReplaceUnavailable,
    rollbackCheckpointUnavailable,
    missingRollback,
    rollbackVerificationMissing,
    productionRootEnabledByDefault,
    testRootMissing,
    legacyFallbackUnavailable,
    productionExecutionGuardMissing,
    unsupportedAction,
    unstableMetadataHash,
    canaryBudgetNonZeroDenied,
    missingInternalCanaryConfiguration,
    canaryBudgetAboveOneDenied,
}

public sealed class CanonicalRecordingMetadataGuardedCommitGate : IEquatable<CanonicalRecordingMetadataGuardedCommitGate>
{
    public CanonicalCutoverDomain Domain { get; set; }
    public CanonicalCutoverAppSeamMode Mode { get; set; }
    public bool Allowed { get; set; }
    public List<CanonicalRecordingMetadataGuardedCommitSeamFailure> Failures { get; set; }
    public string Reason { get; set; }

    public CanonicalRecordingMetadataGuardedCommitGate(
        CanonicalCutoverDomain domain,
        CanonicalCutoverAppSeamMode mode,
        List<CanonicalRecordingMetadataGuardedCommitSeamFailure> failures,
        string reason)
    {
        Domain = domain;
        Mode = mode;
        Failures = new HashSet<CanonicalRecordingMetadataGuardedCommitSeamFailure>(failures)
            .OrderBy(f => f.ToString()).ToList();
        Allowed = Failures.Count == 0;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason)
            ?? (Failures.Count == 0 ? "allowed" : "blocked") ?? "unknown";
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataGuardedCommitGate other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataGuardedCommitGate? other) =>
        other is not null && Domain == other.Domain && Mode == other.Mode &&
        Allowed == other.Allowed && Failures.SequenceEqual(other.Failures) && Reason == other.Reason;
    public override int GetHashCode() => HashCode.Combine(Domain, Mode, Allowed, Failures.Count, Reason);
    public static bool operator ==(CanonicalRecordingMetadataGuardedCommitGate left, CanonicalRecordingMetadataGuardedCommitGate right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataGuardedCommitGate left, CanonicalRecordingMetadataGuardedCommitGate right) => !left.Equals(right);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalRecordingMetadataGuardedCommitDiagnosticKind
{
    canonicalV86GuardedCommitSeamStarted,
    canonicalV86GuardedCommitSeamCompleted,
    canonicalV86GuardedCommitSeamBlocked,
    canonicalV86GuardedCommitGateEvaluated,
    canonicalV86GuardedCommitGateAllowed,
    canonicalV86GuardedCommitGateBlocked,
    canonicalV86CanaryBudgetZero,
    canonicalV86CommitNotExecuted,
    canonicalV86LegacyFallbackPreserved,
    canonicalV86DuplicateSuppressionNotApplied,
    canonicalV86CommitEvidenceReportBuilt,
    canonicalV86ProductionApplyPortReadinessEvaluated,
    canonicalV86ProductionTransportPortReadinessEvaluated,
    canonicalV86RollbackPlanReadinessEvaluated,
    canonicalRecordingMetadataCanaryBudgetZero,
    canonicalRecordingMetadataGateAllowedButNoExecution,
    canonicalRecordingMetadataCommitSkippedBecauseCanaryBudgetZero,
}

public sealed class CanonicalRecordingMetadataGuardedCommitDiagnostic : IEquatable<CanonicalRecordingMetadataGuardedCommitDiagnostic>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "run", Result ?? "", Reason ?? "");

    public CanonicalRecordingMetadataGuardedCommitDiagnosticKind Kind { get; set; }
    public string? SyncRunID { get; set; }
    public CanonicalSyncPlanTrigger Trigger { get; set; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; set; }
    public CanonicalCutoverDomain Domain { get; set; }
    public CanonicalCutoverAppSeamMode Mode { get; set; }
    public string? ObjectID { get; set; }
    public int CandidateCount { get; set; }
    public int GateFailureCount { get; set; }
    public int CanaryBudget { get; set; }
    public int CommitAttemptedCount { get; set; }
    public int DuplicateSuppressionCandidateCount { get; set; }
    public string? Result { get; set; }
    public string? Reason { get; set; }
    public string? HashPrefix { get; set; }

    public CanonicalRecordingMetadataGuardedCommitDiagnostic(
        CanonicalRecordingMetadataGuardedCommitDiagnosticKind kind,
        string? syncRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalCutoverDomain domain,
        CanonicalCutoverAppSeamMode mode,
        string? objectID = null,
        int candidateCount = 0,
        int gateFailureCount = 0,
        int canaryBudget = 0,
        int commitAttemptedCount = 0,
        int duplicateSuppressionCandidateCount = 0,
        string? result = null,
        string? reason = null,
        CanonicalHash? hash = null)
    {
        Kind = kind;
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        Domain = domain;
        Mode = mode;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording") : null;
        CandidateCount = Math.Max(0, candidateCount);
        GateFailureCount = Math.Max(0, gateFailureCount);
        CanaryBudget = Math.Max(0, canaryBudget);
        CommitAttemptedCount = Math.Max(0, commitAttemptedCount);
        DuplicateSuppressionCandidateCount = Math.Max(0, duplicateSuppressionCandidateCount);
        Result = CanonicalProductionRedaction.SafeDiagnosticText(result);
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason);
        HashPrefix = hash is { } h ? CanonicalProductionRedaction.HashPrefix(h.Value) : null;
    }

    public string DiagnosticsSummary
    {
        get
        {
            var parts = new List<string?>
            {
                $"trigger={Trigger}",
                $"nodeRole={NodeRole}",
                $"domain={Domain}",
                $"mode={Mode}",
                $"candidateCount={CandidateCount}",
                $"gateFailureCount={GateFailureCount}",
                $"canaryBudget={CanaryBudget}",
                $"commitAttemptedCount={CommitAttemptedCount}",
                $"duplicateSuppressionCandidateCount={DuplicateSuppressionCandidateCount}",
                Result != null ? $"result={Result}" : null,
                Reason != null ? $"reason={Reason}" : null,
                HashPrefix != null ? $"hashPrefix={HashPrefix}" : null,
            };
            return string.Join(",", parts.Where(p => p != null));
        }
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataGuardedCommitDiagnostic other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataGuardedCommitDiagnostic? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(CanonicalRecordingMetadataGuardedCommitDiagnostic left, CanonicalRecordingMetadataGuardedCommitDiagnostic right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataGuardedCommitDiagnostic left, CanonicalRecordingMetadataGuardedCommitDiagnostic right) => !left.Equals(right);
}

/// <summary>Context object carrying all state needed for a guarded-commit seam evaluation.</summary>
public sealed class CanonicalRecordingMetadataGuardedCommitContext : IEquatable<CanonicalRecordingMetadataGuardedCommitContext>
{
    public string? SyncRunID { get; set; }
    public CanonicalSyncPlanTrigger Trigger { get; set; }
    public CanonicalProductionExecutionDomainRole NodeRole { get; set; }
    public CanonicalManifest? LocalManifest { get; set; }
    public CanonicalManifest? PeerManifest { get; set; }
    public CanonicalApplyPlan? ApplyPlan { get; set; }
    public CanonicalLegacyActionSnapshot LegacyActionSnapshot { get; set; }
    public CanonicalRecordingMetadataCutoverEvidence Evidence { get; set; }
    public CanonicalRealDataShadowCopyResult? RealDataShadowCopyEvidence { get; set; }
    public CanonicalExecutionShadowReport? ExecutionShadowEvidence { get; set; }
    public CanonicalReadOnlyTransportProbeResult? ReadOnlyTransportProbeEvidence { get; set; }
    public CanonicalRecordingMetadataRollbackPlanSummary RollbackPlanSummary { get; set; }
    public CanonicalRecordingMetadataProductionApplyPortReadiness ProductionApplyPortReadiness { get; set; }
    public CanonicalRecordingMetadataProductionTransportPortReadiness ProductionTransportPortReadiness { get; set; }
    public int UnresolvedConflictCount { get; set; }
    public CanonicalRecordingMetadataCanaryPolicy CanaryPolicy { get; set; }
    public bool LegacyFallbackAvailable { get; set; }
    public CanonicalCutoverToken? CutoverToken { get; set; }
    public List<CanonicalRecordingMetadataCutoverCandidate> Candidates { get; set; }
    public bool LocalSnapshotAvailable { get; set; }
    public bool PeerSnapshotAvailable { get; set; }

    public CanonicalRecordingMetadataGuardedCommitContext(
        string? syncRunID,
        CanonicalSyncPlanTrigger trigger,
        CanonicalProductionExecutionDomainRole nodeRole,
        CanonicalManifest? localManifest,
        CanonicalManifest? peerManifest,
        CanonicalApplyPlan? applyPlan,
        CanonicalLegacyActionSnapshot? legacyActionSnapshot = null,
        CanonicalRecordingMetadataCutoverEvidence? evidence = null,
        CanonicalRealDataShadowCopyResult? realDataShadowCopyEvidence = null,
        CanonicalExecutionShadowReport? executionShadowEvidence = null,
        CanonicalReadOnlyTransportProbeResult? readOnlyTransportProbeEvidence = null,
        CanonicalRecordingMetadataRollbackPlanSummary? rollbackPlanSummary = null,
        CanonicalRecordingMetadataProductionApplyPortReadiness? productionApplyPortReadiness = null,
        CanonicalRecordingMetadataProductionTransportPortReadiness? productionTransportPortReadiness = null,
        int unresolvedConflictCount = 0,
        CanonicalRecordingMetadataCanaryPolicy? canaryPolicy = null,
        bool? legacyFallbackAvailable = null,
        CanonicalCutoverToken? cutoverToken = null,
        List<CanonicalRecordingMetadataCutoverCandidate>? candidates = null,
        bool localSnapshotAvailable = false,
        bool peerSnapshotAvailable = false)
    {
        var evidenceVal = evidence ?? new CanonicalRecordingMetadataCutoverEvidence();
        SyncRunID = syncRunID != null ? CanonicalProductionRedaction.SafeIdentifier(syncRunID, "sync-run") : null;
        Trigger = trigger;
        NodeRole = nodeRole;
        LocalManifest = localManifest;
        PeerManifest = peerManifest;
        ApplyPlan = applyPlan;
        LegacyActionSnapshot = legacyActionSnapshot ?? CanonicalLegacyActionSnapshot.Empty;
        Evidence = evidenceVal;
        RealDataShadowCopyEvidence = realDataShadowCopyEvidence;
        ExecutionShadowEvidence = executionShadowEvidence;
        ReadOnlyTransportProbeEvidence = readOnlyTransportProbeEvidence;
        RollbackPlanSummary = rollbackPlanSummary ?? new CanonicalRecordingMetadataRollbackPlanSummary(
            evidenceVal.RollbackPlan, evidenceVal.RollbackVerified, evidenceVal.RollbackRehearsalPassed);
        ProductionApplyPortReadiness = productionApplyPortReadiness ?? new CanonicalRecordingMetadataProductionApplyPortReadiness(evidenceVal);
        ProductionTransportPortReadiness = productionTransportPortReadiness ?? new CanonicalRecordingMetadataProductionTransportPortReadiness(evidenceVal);
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        CanaryPolicy = canaryPolicy ?? new CanonicalRecordingMetadataCanaryPolicy();
        LegacyFallbackAvailable = legacyFallbackAvailable ?? evidenceVal.LegacyFallbackAvailable;
        CutoverToken = cutoverToken;
        Candidates = candidates ?? new List<CanonicalRecordingMetadataCutoverCandidate>();
        LocalSnapshotAvailable = localSnapshotAvailable;
        PeerSnapshotAvailable = peerSnapshotAvailable;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataGuardedCommitContext other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataGuardedCommitContext? other) =>
        other is not null && SyncRunID == other.SyncRunID && Trigger == other.Trigger &&
        NodeRole == other.NodeRole &&
        EqualityComparer<CanonicalManifest?>.Default.Equals(LocalManifest, other.LocalManifest) &&
        EqualityComparer<CanonicalManifest?>.Default.Equals(PeerManifest, other.PeerManifest) &&
        EqualityComparer<CanonicalApplyPlan?>.Default.Equals(ApplyPlan, other.ApplyPlan) &&
        EqualityComparer<CanonicalLegacyActionSnapshot>.Default.Equals(LegacyActionSnapshot, other.LegacyActionSnapshot) &&
        EqualityComparer<CanonicalRecordingMetadataCutoverEvidence>.Default.Equals(Evidence, other.Evidence);
    public override int GetHashCode() =>
        HashCode.Combine(SyncRunID, Trigger, NodeRole, LocalManifest, PeerManifest, ApplyPlan,
            LegacyActionSnapshot, Evidence);
    public static bool operator ==(CanonicalRecordingMetadataGuardedCommitContext left, CanonicalRecordingMetadataGuardedCommitContext right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataGuardedCommitContext left, CanonicalRecordingMetadataGuardedCommitContext right) => !left.Equals(right);
}

public sealed class CanonicalRecordingMetadataGuardedCommitSeamResult : IEquatable<CanonicalRecordingMetadataGuardedCommitSeamResult>
{
    public CanonicalRecordingMetadataGuardedCommitGate Gate { get; set; }
    public CanonicalRecordingMetadataCommitEvidenceReport EvidenceReport { get; set; }
    public List<CanonicalRecordingMetadataGuardedCommitDiagnostic> Diagnostics { get; set; }
    public bool CanaryBudgetZero { get; set; }
    public bool CanExecuteNow { get; set; }
    public bool WillExecuteNow { get; set; }
    public int CommitAttemptedCount { get; set; }
    public int CommittedObjectCount { get; set; }
    public bool ProductionCommitCalled { get; set; }
    public bool RealApplyPortCommitCalled { get; set; }
    public bool NetworkSendCalled { get; set; }
    public bool ApplySyncManifestCalled { get; set; }
    public bool MetadataJSONWritten { get; set; }
    public List<string> DuplicateLegacySuppressedActionIDs { get; set; }
    public List<string> DuplicateLegacySuppressionCandidates { get; set; }
    public bool LegacyFallbackPreserved { get; set; }
    public bool RuntimeSwitchEnabled { get; set; }
    public bool LegacyPlanUnchanged { get; set; }
    public bool ProductionPlanUnchanged { get; set; }
    public int NonfatalFailureCount { get; set; }

    public bool Succeeded => Gate.Allowed && CanaryBudgetZero && !WillExecuteNow && NonfatalFailureCount == 0;

    public CanonicalRecordingMetadataGuardedCommitSeamResult(
        CanonicalRecordingMetadataGuardedCommitGate gate,
        CanonicalRecordingMetadataCommitEvidenceReport evidenceReport,
        List<CanonicalRecordingMetadataGuardedCommitDiagnostic>? diagnostics = null,
        bool canaryBudgetZero = false,
        bool canExecuteNow = false,
        bool willExecuteNow = false,
        int commitAttemptedCount = 0,
        int committedObjectCount = 0,
        bool productionCommitCalled = false,
        bool realApplyPortCommitCalled = false,
        bool networkSendCalled = false,
        bool applySyncManifestCalled = false,
        bool metadataJSONWritten = false,
        List<string>? duplicateLegacySuppressedActionIDs = null,
        List<string>? duplicateLegacySuppressionCandidates = null,
        bool legacyFallbackPreserved = true,
        bool runtimeSwitchEnabled = false,
        bool legacyPlanUnchanged = true,
        bool productionPlanUnchanged = true,
        int nonfatalFailureCount = 0)
    {
        Gate = gate;
        EvidenceReport = evidenceReport;
        Diagnostics = diagnostics ?? new List<CanonicalRecordingMetadataGuardedCommitDiagnostic>();
        CanaryBudgetZero = canaryBudgetZero;
        CanExecuteNow = canExecuteNow;
        WillExecuteNow = willExecuteNow;
        CommitAttemptedCount = commitAttemptedCount;
        CommittedObjectCount = committedObjectCount;
        ProductionCommitCalled = productionCommitCalled;
        RealApplyPortCommitCalled = realApplyPortCommitCalled;
        NetworkSendCalled = networkSendCalled;
        ApplySyncManifestCalled = applySyncManifestCalled;
        MetadataJSONWritten = metadataJSONWritten;
        DuplicateLegacySuppressedActionIDs = duplicateLegacySuppressedActionIDs ?? new List<string>();
        DuplicateLegacySuppressionCandidates = duplicateLegacySuppressionCandidates ?? new List<string>();
        LegacyFallbackPreserved = legacyFallbackPreserved;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        LegacyPlanUnchanged = legacyPlanUnchanged;
        ProductionPlanUnchanged = productionPlanUnchanged;
        NonfatalFailureCount = nonfatalFailureCount;
    }

    public override bool Equals(object? obj) => obj is CanonicalRecordingMetadataGuardedCommitSeamResult other && Equals(other);
    public bool Equals(CanonicalRecordingMetadataGuardedCommitSeamResult? other) =>
        other is not null &&
        EqualityComparer<CanonicalRecordingMetadataGuardedCommitGate>.Default.Equals(Gate, other.Gate) &&
        EqualityComparer<CanonicalRecordingMetadataCommitEvidenceReport>.Default.Equals(EvidenceReport, other.EvidenceReport) &&
        Diagnostics.SequenceEqual(other.Diagnostics) &&
        CanaryBudgetZero == other.CanaryBudgetZero &&
        CanExecuteNow == other.CanExecuteNow &&
        WillExecuteNow == other.WillExecuteNow &&
        CommitAttemptedCount == other.CommitAttemptedCount &&
        CommittedObjectCount == other.CommittedObjectCount &&
        ProductionCommitCalled == other.ProductionCommitCalled &&
        RealApplyPortCommitCalled == other.RealApplyPortCommitCalled &&
        NetworkSendCalled == other.NetworkSendCalled &&
        ApplySyncManifestCalled == other.ApplySyncManifestCalled &&
        MetadataJSONWritten == other.MetadataJSONWritten &&
        DuplicateLegacySuppressedActionIDs.SequenceEqual(other.DuplicateLegacySuppressedActionIDs) &&
        DuplicateLegacySuppressionCandidates.SequenceEqual(other.DuplicateLegacySuppressionCandidates) &&
        LegacyFallbackPreserved == other.LegacyFallbackPreserved &&
        RuntimeSwitchEnabled == other.RuntimeSwitchEnabled &&
        LegacyPlanUnchanged == other.LegacyPlanUnchanged &&
        ProductionPlanUnchanged == other.ProductionPlanUnchanged &&
        NonfatalFailureCount == other.NonfatalFailureCount;
    public override int GetHashCode() =>
        HashCode.Combine(Gate, EvidenceReport, Diagnostics.Count, CanaryBudgetZero, CanExecuteNow, WillExecuteNow,
            CommitAttemptedCount, CommittedObjectCount, ProductionCommitCalled, RealApplyPortCommitCalled,
            NetworkSendCalled, ApplySyncManifestCalled, MetadataJSONWritten, DuplicateLegacySuppressedActionIDs.Count,
            DuplicateLegacySuppressionCandidates.Count, LegacyFallbackPreserved, RuntimeSwitchEnabled,
            LegacyPlanUnchanged, ProductionPlanUnchanged, NonfatalFailureCount);
    public static bool operator ==(CanonicalRecordingMetadataGuardedCommitSeamResult left, CanonicalRecordingMetadataGuardedCommitSeamResult right) => left.Equals(right);
    public static bool operator !=(CanonicalRecordingMetadataGuardedCommitSeamResult left, CanonicalRecordingMetadataGuardedCommitSeamResult right) => !left.Equals(right);
}
