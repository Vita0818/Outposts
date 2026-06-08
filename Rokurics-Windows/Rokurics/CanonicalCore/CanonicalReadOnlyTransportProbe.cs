using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalReadOnlyTransportProbeRouteStatus
    {
        allowedReadOnly,
        rejectedMutating,
        rejectedUnknown,
        suppressedDisabled
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalReadOnlyTransportProbeFailure
    {
        disabled,
        mutatingRouteRejected,
        unknownRouteRejected,
        artifactFetchNotAllowed,
        artifactFetchTooLarge,
        authBoundaryMissing,
        manifestHashUsedAsAuth,
        networkSuppressed,
        sendFailed
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalLiveReadOnlyTransportProbeMode
    {
        disabled,
        classifyOnly,
        buildSignedEnvelopeOnly,
        sendReadOnlyProbe,
        blockedMutatingRoute
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalLiveReadOnlyTransportProbeFailure
    {
        disabled,
        mutatingRouteRejected,
        unknownRouteRejected,
        artifactFetchNotAllowed,
        artifactFetchTooLarge,
        internalConfigurationMissing,
        authBoundaryMissing,
        manifestHashUsedAsAuth,
        signedEnvelopeBuildFailed,
        networkSuppressed,
        sendFailed,
        responseRejected
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalLiveReadOnlyTransportProbeDiagnosticKind
    {
        canonicalLiveReadOnlyProbePolicyEvaluated,
        canonicalLiveReadOnlyProbeRouteAllowed,
        canonicalLiveReadOnlyProbeRouteRejected,
        canonicalLiveReadOnlyProbeEnvelopeBuilt,
        canonicalLiveReadOnlyProbeSendSuppressed,
        canonicalLiveReadOnlyProbeSendStarted,
        canonicalLiveReadOnlyProbeSendCompleted,
        canonicalLiveReadOnlyProbeSendFailed,
        canonicalLiveReadOnlyProbeAuthBoundaryPreserved,
        canonicalLiveReadOnlyProbeMacAuditStarted,
        canonicalLiveReadOnlyProbeMacAuditCompleted,
        canonicalLiveReadOnlyProbeNoMutationVerified,
        canonicalLiveReadOnlyProbeMutationRiskBlocked,
        canonicalLiveReadOnlyProbeStateSnapshotUnavailable
    }

    public static class CanonicalLiveReadOnlyTransportProbeHTTP
    {
        public const string MarkerHeader = "X-Rokurics-Canonical-Probe";
        public const string MarkerValue = "live-read-only-v8.1";
        public const string ModeHeader = "X-Rokurics-Canonical-Probe-Mode";
        public const string RouteHeader = "X-Rokurics-Canonical-Probe-Route";
        public const string SyncRunIDHeader = "X-Rokurics-Canonical-Probe-Sync-Run-ID";

        public static Dictionary<string, string> MarkerHeaders(
            CanonicalLiveReadOnlyTransportProbeMode mode,
            CanonicalReadOnlyTransportProbeRoute route,
            string? syncRunID)
        {
            var headers = new Dictionary<string, string>
            {
                [MarkerHeader] = MarkerValue,
                [ModeHeader] = mode.ToString(),
                [RouteHeader] = $"{route.Method} {route.Path}"
            };
            var safeSyncRunID = CanonicalShadowMigrationRedaction.SafeIdentifier(syncRunID);
            if (safeSyncRunID != null)
                headers[SyncRunIDHeader] = safeSyncRunID;
            return headers;
        }

        public static bool IsMarked(Dictionary<string, string> headers)
        {
            var normalized = Normalized(headers);
            return normalized.GetValueOrDefault(MarkerHeader.ToLower()) == MarkerValue;
        }

        public static string? SyncRunID(Dictionary<string, string> headers)
        {
            var normalized = Normalized(headers);
            return CanonicalShadowMigrationRedaction.SafeIdentifier(
                normalized.GetValueOrDefault(SyncRunIDHeader.ToLower()));
        }

        private static Dictionary<string, string> Normalized(Dictionary<string, string> headers)
        {
            return headers.ToDictionary(kvp => kvp.Key.ToLower(), kvp => kvp.Value);
        }
    }

    public record CanonicalReadOnlyTransportProbeRoute : IEquatable<CanonicalReadOnlyTransportProbeRoute>
    {
        public string Method { get; init; }
        public string Path { get; init; }

        public CanonicalReadOnlyTransportProbeRoute(string method, string path)
        {
            Method = method.Trim().ToUpper();
            Path = CanonicalShadowMigrationRedaction.SafeText(path) ?? "/unknown";
        }

        public static readonly CanonicalReadOnlyTransportProbeRoute Health = new("GET", "/health");
        public static readonly CanonicalReadOnlyTransportProbeRoute Fingerprint = new("GET", "/fingerprint");
        public static readonly CanonicalReadOnlyTransportProbeRoute SyncStatus = new("POST", "/sync/status");
        public static readonly CanonicalReadOnlyTransportProbeRoute SyncInventory = new("POST", "/sync/inventory");
        public static readonly CanonicalReadOnlyTransportProbeRoute DeviceStatus = new("POST", "/device/status");
        public static readonly CanonicalReadOnlyTransportProbeRoute ArtifactRequest = new("POST", "/sync/artifact-request");

        public bool IsKnownReadOnlyRoute
        {
            get
            {
                return (Method, Path) switch
                {
                    ("GET", "/health") or ("GET", "/fingerprint") or ("POST", "/sync/inventory") or ("POST", "/sync/artifact-request") => true,
                    _ => false
                };
            }
        }

        public bool IsKnownMutatingRoute
        {
            get
            {
                return (Method, Path) switch
                {
                    ("POST", "/pair") or ("POST", "/upload-secure-test") or ("POST", "/upload-recording-metadata")
                        or ("POST", "/upload-recording-audio") or ("POST", "/upload-recording-audio-session/start")
                        or ("POST", "/upload-recording-audio-session/status") or ("POST", "/upload-recording-audio-session/chunk")
                        or ("POST", "/upload-recording-audio-session/finalize") or ("POST", "/device/status")
                        or ("POST", "/sync/status") or ("POST", "/sync/apply") or ("POST", "/sync/apply-metadata")
                        or ("POST", "/sync/manifest") => true,
                    _ => false
                };
            }
        }
    }

    public record CanonicalLiveReadOnlyTransportProbePolicy : IEquatable<CanonicalLiveReadOnlyTransportProbePolicy>
    {
        public CanonicalLiveReadOnlyTransportProbeMode Mode { get; init; }
        public CanonicalReadOnlyTransportProbeRoute Route { get; init; }
        public bool AllowBoundedArtifactFetch { get; init; }
        public int ArtifactFetchMaxBytes { get; init; }
        public bool InternalConfigurationEnabled { get; init; }
        public double RequestTimeoutSeconds { get; init; }
        public double ResourceTimeoutSeconds { get; init; }

        public CanonicalLiveReadOnlyTransportProbePolicy(
            CanonicalLiveReadOnlyTransportProbeMode mode = CanonicalLiveReadOnlyTransportProbeMode.disabled,
            CanonicalReadOnlyTransportProbeRoute? route = null,
            bool allowBoundedArtifactFetch = false,
            int artifactFetchMaxBytes = 256 * 1024,
            bool internalConfigurationEnabled = false,
            double requestTimeoutSeconds = 5,
            double resourceTimeoutSeconds = 8)
        {
            Mode = mode;
            Route = route ?? CanonicalReadOnlyTransportProbeRoute.SyncInventory;
            AllowBoundedArtifactFetch = allowBoundedArtifactFetch;
            ArtifactFetchMaxBytes = Math.Max(0, artifactFetchMaxBytes);
            InternalConfigurationEnabled = internalConfigurationEnabled;
            RequestTimeoutSeconds = Math.Max(1, requestTimeoutSeconds);
            ResourceTimeoutSeconds = Math.Max(RequestTimeoutSeconds, resourceTimeoutSeconds);
        }

        public static readonly CanonicalLiveReadOnlyTransportProbePolicy Disabled = new();

        public static CanonicalLiveReadOnlyTransportProbePolicy ClassifyOnly(
            CanonicalReadOnlyTransportProbeRoute? route = null,
            bool allowBoundedArtifactFetch = false)
        {
            return new CanonicalLiveReadOnlyTransportProbePolicy(
                mode: CanonicalLiveReadOnlyTransportProbeMode.classifyOnly,
                route: route ?? CanonicalReadOnlyTransportProbeRoute.SyncInventory,
                allowBoundedArtifactFetch: allowBoundedArtifactFetch);
        }

        public static CanonicalLiveReadOnlyTransportProbePolicy BuildSignedEnvelopeOnly(
            CanonicalReadOnlyTransportProbeRoute? route = null,
            bool allowBoundedArtifactFetch = false,
            bool internalConfigurationEnabled = true)
        {
            return new CanonicalLiveReadOnlyTransportProbePolicy(
                mode: CanonicalLiveReadOnlyTransportProbeMode.buildSignedEnvelopeOnly,
                route: route ?? CanonicalReadOnlyTransportProbeRoute.SyncInventory,
                allowBoundedArtifactFetch: allowBoundedArtifactFetch,
                internalConfigurationEnabled: internalConfigurationEnabled);
        }

        public static CanonicalLiveReadOnlyTransportProbePolicy SendReadOnlyProbe(
            CanonicalReadOnlyTransportProbeRoute? route = null,
            bool allowBoundedArtifactFetch = false,
            bool internalConfigurationEnabled = true)
        {
            return new CanonicalLiveReadOnlyTransportProbePolicy(
                mode: CanonicalLiveReadOnlyTransportProbeMode.sendReadOnlyProbe,
                route: route ?? CanonicalReadOnlyTransportProbeRoute.SyncInventory,
                allowBoundedArtifactFetch: allowBoundedArtifactFetch,
                internalConfigurationEnabled: internalConfigurationEnabled);
        }

        public CanonicalReadOnlyTransportProbePolicy ClassificationPolicy =>
            CanonicalReadOnlyTransportProbePolicy.Enabled(
                AllowBoundedArtifactFetch,
                Mode == CanonicalLiveReadOnlyTransportProbeMode.sendReadOnlyProbe && InternalConfigurationEnabled);
    }

    public record CanonicalLiveReadOnlyTransportProbeGate : IEquatable<CanonicalLiveReadOnlyTransportProbeGate>
    {
        public CanonicalLiveReadOnlyTransportProbeMode Mode { get; init; }
        public CanonicalReadOnlyTransportProbeRoute Route { get; init; }
        public CanonicalReadOnlyTransportProbeRouteStatus RouteStatus { get; init; }
        public bool ShouldClassify { get; init; }
        public bool ShouldBuildEnvelope { get; init; }
        public bool ShouldSend { get; init; }
        public bool Blocked { get; init; }
        public bool Suppressed { get; init; }
        public CanonicalLiveReadOnlyTransportProbeFailure? Failure { get; init; }
        public string Reason { get; init; }

        public CanonicalLiveReadOnlyTransportProbeGate(
            CanonicalLiveReadOnlyTransportProbeMode mode,
            CanonicalReadOnlyTransportProbeRoute route,
            CanonicalReadOnlyTransportProbeRouteStatus routeStatus,
            bool shouldClassify,
            bool shouldBuildEnvelope,
            bool shouldSend,
            bool blocked,
            bool suppressed,
            CanonicalLiveReadOnlyTransportProbeFailure? failure,
            string reason)
        {
            Mode = mode;
            Route = route;
            RouteStatus = routeStatus;
            ShouldClassify = shouldClassify;
            ShouldBuildEnvelope = shouldBuildEnvelope;
            ShouldSend = shouldSend;
            Blocked = blocked;
            Suppressed = suppressed;
            Failure = failure;
            Reason = CanonicalShadowMigrationRedaction.SafeText(reason)
                ?? failure?.ToString() ?? mode.ToString();
        }

        public static CanonicalLiveReadOnlyTransportProbeGate Evaluate(
            CanonicalLiveReadOnlyTransportProbePolicy policy,
            int bodyByteCount)
        {
            if (policy.Mode == CanonicalLiveReadOnlyTransportProbeMode.disabled)
            {
                return new CanonicalLiveReadOnlyTransportProbeGate(
                    CanonicalLiveReadOnlyTransportProbeMode.disabled, policy.Route,
                    CanonicalReadOnlyTransportProbeRouteStatus.suppressedDisabled,
                    false, false, false, false, true,
                    CanonicalLiveReadOnlyTransportProbeFailure.disabled,
                    "liveReadOnlyProbeDisabled");
            }

            var routeDecision = policy.ClassificationPolicy.RouteStatusFor(policy.Route, bodyByteCount);
            if (routeDecision.Status == CanonicalReadOnlyTransportProbeRouteStatus.rejectedMutating
                || policy.Mode == CanonicalLiveReadOnlyTransportProbeMode.blockedMutatingRoute)
            {
                return new CanonicalLiveReadOnlyTransportProbeGate(
                    CanonicalLiveReadOnlyTransportProbeMode.blockedMutatingRoute, policy.Route,
                    CanonicalReadOnlyTransportProbeRouteStatus.rejectedMutating,
                    true, false, false, true, true,
                    CanonicalLiveReadOnlyTransportProbeFailure.mutatingRouteRejected,
                    "mutatingRouteProbeBlocked");
            }

            if (routeDecision.Failure != null)
            {
                return new CanonicalLiveReadOnlyTransportProbeGate(
                    policy.Mode, policy.Route, routeDecision.Status,
                    true, false, false, true, true,
                    ReadOnlyToLiveFailure(routeDecision.Failure),
                    routeDecision.Failure.ToString());
            }

            if (policy.Mode == CanonicalLiveReadOnlyTransportProbeMode.sendReadOnlyProbe
                && !policy.InternalConfigurationEnabled)
            {
                return new CanonicalLiveReadOnlyTransportProbeGate(
                    policy.Mode, policy.Route, routeDecision.Status,
                    true, false, false, true, true,
                    CanonicalLiveReadOnlyTransportProbeFailure.internalConfigurationMissing,
                    "internalConfigurationMissing");
            }

            return policy.Mode switch
            {
                CanonicalLiveReadOnlyTransportProbeMode.classifyOnly => new CanonicalLiveReadOnlyTransportProbeGate(
                    policy.Mode, policy.Route, routeDecision.Status,
                    true, false, false, false, true,
                    CanonicalLiveReadOnlyTransportProbeFailure.networkSuppressed,
                    "classifyOnly"),
                CanonicalLiveReadOnlyTransportProbeMode.buildSignedEnvelopeOnly => new CanonicalLiveReadOnlyTransportProbeGate(
                    policy.Mode, policy.Route, routeDecision.Status,
                    true, true, false, false, true,
                    CanonicalLiveReadOnlyTransportProbeFailure.networkSuppressed,
                    "buildSignedEnvelopeOnly"),
                CanonicalLiveReadOnlyTransportProbeMode.sendReadOnlyProbe => new CanonicalLiveReadOnlyTransportProbeGate(
                    policy.Mode, policy.Route, routeDecision.Status,
                    true, true, true, false, false,
                    null, "sendReadOnlyProbe"),
                _ => new CanonicalLiveReadOnlyTransportProbeGate(
                    CanonicalLiveReadOnlyTransportProbeMode.disabled, policy.Route,
                    CanonicalReadOnlyTransportProbeRouteStatus.suppressedDisabled,
                    false, false, false, false, true,
                    CanonicalLiveReadOnlyTransportProbeFailure.disabled,
                    "liveReadOnlyProbeDisabled")
            };
        }

        private static CanonicalLiveReadOnlyTransportProbeFailure ReadOnlyToLiveFailure(
            CanonicalReadOnlyTransportProbeFailure readOnlyFailure)
        {
            return readOnlyFailure switch
            {
                CanonicalReadOnlyTransportProbeFailure.disabled => CanonicalLiveReadOnlyTransportProbeFailure.disabled,
                CanonicalReadOnlyTransportProbeFailure.mutatingRouteRejected => CanonicalLiveReadOnlyTransportProbeFailure.mutatingRouteRejected,
                CanonicalReadOnlyTransportProbeFailure.unknownRouteRejected => CanonicalLiveReadOnlyTransportProbeFailure.unknownRouteRejected,
                CanonicalReadOnlyTransportProbeFailure.artifactFetchNotAllowed => CanonicalLiveReadOnlyTransportProbeFailure.artifactFetchNotAllowed,
                CanonicalReadOnlyTransportProbeFailure.artifactFetchTooLarge => CanonicalLiveReadOnlyTransportProbeFailure.artifactFetchTooLarge,
                CanonicalReadOnlyTransportProbeFailure.authBoundaryMissing => CanonicalLiveReadOnlyTransportProbeFailure.authBoundaryMissing,
                CanonicalReadOnlyTransportProbeFailure.manifestHashUsedAsAuth => CanonicalLiveReadOnlyTransportProbeFailure.manifestHashUsedAsAuth,
                CanonicalReadOnlyTransportProbeFailure.networkSuppressed => CanonicalLiveReadOnlyTransportProbeFailure.networkSuppressed,
                CanonicalReadOnlyTransportProbeFailure.sendFailed => CanonicalLiveReadOnlyTransportProbeFailure.sendFailed,
                _ => CanonicalLiveReadOnlyTransportProbeFailure.disabled
            };
        }
    }

    public record CanonicalLiveReadOnlyTransportProbeResult : IEquatable<CanonicalLiveReadOnlyTransportProbeResult>
    {
        public CanonicalLiveReadOnlyTransportProbeMode Mode { get; init; }
        public CanonicalReadOnlyTransportProbeRoute Route { get; init; }
        public CanonicalReadOnlyTransportProbeRouteStatus RouteStatus { get; init; }
        public bool EnvelopeBuilt { get; init; }
        public bool SentNetwork { get; init; }
        public bool Completed { get; init; }
        public bool Blocked { get; init; }
        public bool Suppressed { get; init; }
        public bool AuthBoundaryPreserved { get; init; }
        public CanonicalLiveReadOnlyTransportProbeFailure? Failure { get; init; }
        public int? HttpStatusCode { get; init; }
        public int? ResponseByteCount { get; init; }
        public List<CanonicalLiveReadOnlyTransportProbeDiagnosticKind> Diagnostics { get; init; }
        public string Reason { get; init; }

        public CanonicalLiveReadOnlyTransportProbeResult(
            CanonicalLiveReadOnlyTransportProbeMode mode,
            CanonicalReadOnlyTransportProbeRoute route,
            CanonicalReadOnlyTransportProbeRouteStatus routeStatus,
            bool envelopeBuilt = false,
            bool sentNetwork = false,
            bool completed = false,
            bool blocked = false,
            bool suppressed = true,
            bool authBoundaryPreserved = false,
            CanonicalLiveReadOnlyTransportProbeFailure? failure = null,
            int? httpStatusCode = null,
            int? responseByteCount = null,
            List<CanonicalLiveReadOnlyTransportProbeDiagnosticKind>? diagnostics = null,
            string reason = "")
        {
            Mode = mode;
            Route = route;
            RouteStatus = routeStatus;
            EnvelopeBuilt = envelopeBuilt;
            SentNetwork = sentNetwork;
            Completed = completed;
            Blocked = blocked;
            Suppressed = suppressed;
            AuthBoundaryPreserved = authBoundaryPreserved;
            Failure = failure;
            HttpStatusCode = httpStatusCode;
            ResponseByteCount = responseByteCount;
            Diagnostics = new HashSet<CanonicalLiveReadOnlyTransportProbeDiagnosticKind>(
                diagnostics ?? new List<CanonicalLiveReadOnlyTransportProbeDiagnosticKind>())
                .OrderBy(d => d.ToString()).ToList();
            Reason = CanonicalShadowMigrationRedaction.SafeText(reason)
                ?? failure?.ToString() ?? mode.ToString();
        }

        public string DiagnosticsSummary =>
            string.Join(",", new[]
            {
                $"mode={Mode}",
                $"route={Route.Method} {Route.Path}",
                $"routeStatus={RouteStatus}",
                $"envelopeBuilt={EnvelopeBuilt}",
                $"sent={SentNetwork}",
                $"completed={Completed}",
                $"blocked={Blocked}",
                $"suppressed={Suppressed}",
                $"authBoundaryPreserved={AuthBoundaryPreserved}",
                $"httpStatus={HttpStatusCode?.ToString() ?? "none"}",
                $"responseBytes={ResponseByteCount?.ToString() ?? "none"}",
                $"failure={Failure?.ToString() ?? "none"}",
                $"reason={Reason}"
            });
    }

    public record CanonicalReadOnlyTransportProbePolicy : IEquatable<CanonicalReadOnlyTransportProbePolicy>
    {
        public bool IsEnabled { get; init; }
        public bool AllowBoundedArtifactFetch { get; init; }
        public int ArtifactFetchMaxBytes { get; init; }
        public bool AllowNetworkSend { get; init; }
        public List<CanonicalReadOnlyTransportProbeRoute> AllowedRoutes { get; init; }

        public CanonicalReadOnlyTransportProbePolicy(
            bool isEnabled = false,
            bool allowBoundedArtifactFetch = false,
            int artifactFetchMaxBytes = 256 * 1024,
            bool allowNetworkSend = false,
            List<CanonicalReadOnlyTransportProbeRoute>? allowedRoutes = null)
        {
            IsEnabled = isEnabled;
            AllowBoundedArtifactFetch = allowBoundedArtifactFetch;
            ArtifactFetchMaxBytes = Math.Max(0, artifactFetchMaxBytes);
            AllowNetworkSend = allowNetworkSend;
            var routes = allowedRoutes ?? new List<CanonicalReadOnlyTransportProbeRoute>
            {
                CanonicalReadOnlyTransportProbeRoute.Health,
                CanonicalReadOnlyTransportProbeRoute.Fingerprint,
                CanonicalReadOnlyTransportProbeRoute.SyncInventory
            };
            AllowedRoutes = new HashSet<CanonicalReadOnlyTransportProbeRoute>(routes)
                .OrderBy(r => $"{r.Method} {r.Path}").ToList();
        }

        public static readonly CanonicalReadOnlyTransportProbePolicy Disabled = new();

        public static CanonicalReadOnlyTransportProbePolicy Enabled(
            bool allowBoundedArtifactFetch = false,
            bool allowNetworkSend = false)
        {
            var routes = new List<CanonicalReadOnlyTransportProbeRoute>
            {
                CanonicalReadOnlyTransportProbeRoute.Health,
                CanonicalReadOnlyTransportProbeRoute.Fingerprint,
                CanonicalReadOnlyTransportProbeRoute.SyncInventory
            };
            if (allowBoundedArtifactFetch)
                routes.Add(CanonicalReadOnlyTransportProbeRoute.ArtifactRequest);
            return new CanonicalReadOnlyTransportProbePolicy(
                isEnabled: true,
                allowBoundedArtifactFetch: allowBoundedArtifactFetch,
                allowNetworkSend: allowNetworkSend,
                allowedRoutes: routes);
        }

        public (CanonicalReadOnlyTransportProbeRouteStatus Status, CanonicalReadOnlyTransportProbeFailure? Failure)
            RouteStatusFor(CanonicalReadOnlyTransportProbeRoute route, int bodyByteCount)
        {
            if (!IsEnabled)
                return (CanonicalReadOnlyTransportProbeRouteStatus.suppressedDisabled,
                    CanonicalReadOnlyTransportProbeFailure.disabled);
            if (!route.IsKnownReadOnlyRoute)
            {
                return (route.IsKnownMutatingRoute
                    ? CanonicalReadOnlyTransportProbeRouteStatus.rejectedMutating
                    : CanonicalReadOnlyTransportProbeRouteStatus.rejectedUnknown,
                    route.IsKnownMutatingRoute
                        ? CanonicalReadOnlyTransportProbeFailure.mutatingRouteRejected
                        : CanonicalReadOnlyTransportProbeFailure.unknownRouteRejected);
            }
            if (route.Equals(CanonicalReadOnlyTransportProbeRoute.ArtifactRequest))
            {
                if (!AllowBoundedArtifactFetch)
                    return (CanonicalReadOnlyTransportProbeRouteStatus.rejectedMutating,
                        CanonicalReadOnlyTransportProbeFailure.artifactFetchNotAllowed);
                if (bodyByteCount > ArtifactFetchMaxBytes)
                    return (CanonicalReadOnlyTransportProbeRouteStatus.rejectedMutating,
                        CanonicalReadOnlyTransportProbeFailure.artifactFetchTooLarge);
            }
            if (!AllowedRoutes.Contains(route))
                return (CanonicalReadOnlyTransportProbeRouteStatus.rejectedUnknown,
                    CanonicalReadOnlyTransportProbeFailure.unknownRouteRejected);
            return (CanonicalReadOnlyTransportProbeRouteStatus.allowedReadOnly, null);
        }
    }

    public record CanonicalReadOnlyTransportProbeRequest : IEquatable<CanonicalReadOnlyTransportProbeRequest>
    {
        public CanonicalReadOnlyTransportProbeRoute Route { get; init; }
        public int BodyByteCount { get; init; }
        public string? BodyHashPrefix { get; init; }
        public bool TimestampPresent { get; init; }
        public bool NoncePresent { get; init; }
        public bool SignaturePresent { get; init; }
        public bool TlsPinningPreserved { get; init; }
        public bool HmacPreserved { get; init; }
        public bool BodyHashPreserved { get; init; }
        public bool ManifestHashPresent { get; init; }
        public bool ManifestHashUsedAsAuth { get; init; }

        public CanonicalReadOnlyTransportProbeRequest(
            CanonicalReadOnlyTransportProbeRoute route,
            int bodyByteCount = 0,
            CanonicalHash? bodyHash = null,
            bool timestampPresent = true,
            bool noncePresent = true,
            bool signaturePresent = true,
            bool tlsPinningPreserved = true,
            bool hmacPreserved = true,
            bool bodyHashPreserved = true,
            bool manifestHashPresent = false,
            bool manifestHashUsedAsAuth = false)
        {
            Route = route;
            BodyByteCount = Math.Max(0, bodyByteCount);
            BodyHashPrefix = bodyHash != null
                ? CanonicalProductionRedaction.HashPrefix(bodyHash.Value)
                : null;
            TimestampPresent = timestampPresent;
            NoncePresent = noncePresent;
            SignaturePresent = signaturePresent;
            TlsPinningPreserved = tlsPinningPreserved;
            HmacPreserved = hmacPreserved;
            BodyHashPreserved = bodyHashPreserved;
            ManifestHashPresent = manifestHashPresent;
            ManifestHashUsedAsAuth = manifestHashUsedAsAuth;
        }

        public bool AuthBoundaryPreserved =>
            TlsPinningPreserved && HmacPreserved && BodyHashPreserved
            && TimestampPresent && NoncePresent && SignaturePresent && !ManifestHashUsedAsAuth;
    }

    public record CanonicalReadOnlyTransportProbeAudit : IEquatable<CanonicalReadOnlyTransportProbeAudit>
    {
        public CanonicalReadOnlyTransportProbeRoute Route { get; init; }
        public CanonicalReadOnlyTransportProbeRouteStatus RouteStatus { get; init; }
        public bool RequestSent { get; init; }
        public bool RequestSuppressed { get; init; }
        public bool AuthBoundaryPreserved { get; init; }
        public bool ManifestHashUsedAsAuth { get; init; }
        public string Reason { get; init; }

        public CanonicalReadOnlyTransportProbeAudit(
            CanonicalReadOnlyTransportProbeRoute route,
            CanonicalReadOnlyTransportProbeRouteStatus routeStatus,
            bool requestSent,
            bool requestSuppressed,
            bool authBoundaryPreserved,
            bool manifestHashUsedAsAuth,
            string reason)
        {
            Route = route;
            RouteStatus = routeStatus;
            RequestSent = requestSent;
            RequestSuppressed = requestSuppressed;
            AuthBoundaryPreserved = authBoundaryPreserved;
            ManifestHashUsedAsAuth = manifestHashUsedAsAuth;
            Reason = CanonicalShadowMigrationRedaction.SafeText(reason)
                ?? routeStatus.ToString();
        }
    }

    public record CanonicalReadOnlyTransportProbeResult : IEquatable<CanonicalReadOnlyTransportProbeResult>
    {
        public string Status { get; init; }
        public CanonicalReadOnlyTransportProbeRouteStatus RouteStatus { get; init; }
        public bool SentNetwork { get; init; }
        public bool Blocked { get; init; }
        public bool Suppressed { get; init; }
        public bool AuthBoundaryPreserved { get; init; }
        public CanonicalReadOnlyTransportProbeFailure? Failure { get; init; }
        public CanonicalReadOnlyTransportProbeAudit Audit { get; init; }

        public CanonicalReadOnlyTransportProbeResult(
            string status,
            CanonicalReadOnlyTransportProbeRouteStatus routeStatus,
            bool sentNetwork,
            bool blocked,
            bool suppressed,
            bool authBoundaryPreserved,
            CanonicalReadOnlyTransportProbeFailure? failure,
            CanonicalReadOnlyTransportProbeAudit audit)
        {
            Status = CanonicalShadowMigrationRedaction.SafeText(status)
                ?? routeStatus.ToString();
            RouteStatus = routeStatus;
            SentNetwork = sentNetwork;
            Blocked = blocked;
            Suppressed = suppressed;
            AuthBoundaryPreserved = authBoundaryPreserved;
            Failure = failure;
            Audit = audit;
        }

        public string DiagnosticsSummary =>
            string.Join(",", new[]
            {
                $"probe={Status}",
                $"route={Audit.Route.Method} {Audit.Route.Path}",
                $"routeStatus={RouteStatus}",
                $"sent={SentNetwork}",
                $"suppressed={Suppressed}",
                $"blocked={Blocked}",
                $"authBoundaryPreserved={AuthBoundaryPreserved}",
                $"manifestHashAuth={Audit.ManifestHashUsedAsAuth}",
                $"failure={Failure?.ToString() ?? "none"}"
            });
    }

    public class CanonicalReadOnlyTransportProbe
    {
        public CanonicalReadOnlyTransportProbe() { }

        public CanonicalReadOnlyTransportProbeResult Evaluate(
            CanonicalReadOnlyTransportProbeRequest request,
            CanonicalReadOnlyTransportProbePolicy? policy = null)
        {
            policy ??= CanonicalReadOnlyTransportProbePolicy.Disabled;
            var routeDecision = policy.RouteStatusFor(request.Route, request.BodyByteCount);

            if (request.ManifestHashUsedAsAuth)
            {
                return MakeResult(request, routeDecision.Status, false, true, true,
                    CanonicalReadOnlyTransportProbeFailure.manifestHashUsedAsAuth,
                    "manifestHashNotAuth");
            }
            if (!request.AuthBoundaryPreserved)
            {
                return MakeResult(request, routeDecision.Status, false, true, true,
                    CanonicalReadOnlyTransportProbeFailure.authBoundaryMissing,
                    "authBoundaryMissing");
            }
            if (routeDecision.Failure != null)
            {
                return MakeResult(request, routeDecision.Status, false,
                    routeDecision.Status != CanonicalReadOnlyTransportProbeRouteStatus.suppressedDisabled,
                    true, routeDecision.Failure, routeDecision.Failure.ToString());
            }
            var shouldSend = policy.AllowNetworkSend;
            return MakeResult(request, routeDecision.Status, shouldSend, false, !shouldSend,
                shouldSend ? null : CanonicalReadOnlyTransportProbeFailure.networkSuppressed,
                shouldSend ? "readOnlyProbeSent" : "readOnlyProbeSuppressed");
        }

        public CanonicalReadOnlyTransportProbeRequest Request(
            CanonicalProductionSignedRequest signedRequest,
            CanonicalReadOnlyTransportProbeRoute route,
            bool tlsPinningPreserved = true,
            bool hmacPreserved = true,
            bool manifestHashPresent = false,
            bool manifestHashUsedAsAuth = false)
        {
            return new CanonicalReadOnlyTransportProbeRequest(
                route: route,
                bodyByteCount: signedRequest.BuildRequest.Body.Length,
                bodyHash: signedRequest.BodyHash,
                timestampPresent: true,
                noncePresent: !string.IsNullOrEmpty(signedRequest.BuildRequest.Nonce),
                signaturePresent: signedRequest.SignaturePrefix != null
                    || signedRequest.SignerDescription != null,
                tlsPinningPreserved: tlsPinningPreserved,
                hmacPreserved: hmacPreserved,
                bodyHashPreserved:
                    Equals(CanonicalTransportEnvelope.Hash(signedRequest.BuildRequest.Body),
                        signedRequest.BodyHash),
                manifestHashPresent: manifestHashPresent,
                manifestHashUsedAsAuth: manifestHashUsedAsAuth);
        }

        private CanonicalReadOnlyTransportProbeResult MakeResult(
            CanonicalReadOnlyTransportProbeRequest request,
            CanonicalReadOnlyTransportProbeRouteStatus routeStatus,
            bool sentNetwork, bool blocked, bool suppressed,
            CanonicalReadOnlyTransportProbeFailure? failure, string reason)
        {
            string status;
            if (blocked) status = "blocked";
            else if (sentNetwork) status = "completed";
            else if (suppressed)
                status = routeStatus == CanonicalReadOnlyTransportProbeRouteStatus.suppressedDisabled
                    ? "disabled" : "suppressed";
            else status = "completed";

            var audit = new CanonicalReadOnlyTransportProbeAudit(
                request.Route, routeStatus, sentNetwork, suppressed,
                request.AuthBoundaryPreserved, request.ManifestHashUsedAsAuth, reason);
            return new CanonicalReadOnlyTransportProbeResult(
                status, routeStatus, sentNetwork, blocked, suppressed,
                request.AuthBoundaryPreserved, failure, audit);
        }
    }
}
