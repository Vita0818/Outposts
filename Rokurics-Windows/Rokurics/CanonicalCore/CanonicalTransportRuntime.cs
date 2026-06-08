using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

// ─── CanonicalTransportRoute ─────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTransportRoute
{
    manifestExchange,
    applyPlan,
    applyMetadata,
    fileRead,
    uploadStart,
    uploadStatus,
    uploadChunk,
    uploadFinalize
}

// ─── CanonicalTransportEnvelope ──────────────────────────────────────────────

public sealed class CanonicalTransportEnvelope : IEquatable<CanonicalTransportEnvelope>
{
    public string Id => RequestID;

    public string RequestID { get; }
    public string SourceNodeID { get; }
    public string DestinationNodeID { get; }
    public CanonicalTransportRoute Route { get; }
    public byte[] Body { get; }
    public CanonicalHash BodyHash { get; }
    public string? IdempotencyKey { get; }
    public CanonicalTimestamp SentAt { get; }

    public CanonicalTransportEnvelope(
        string? requestID = null,
        string sourceNodeID = "",
        string destinationNodeID = "",
        CanonicalTransportRoute route = default,
        byte[]? body = null,
        string? idempotencyKey = null,
        DateTime? sentAt = null)
    {
        RequestID = requestID?.Trim().NilIfEmpty() ?? Guid.NewGuid().ToString();
        SourceNodeID = sourceNodeID.Trim().NilIfEmpty() ?? "source:unknown";
        DestinationNodeID = destinationNodeID.Trim().NilIfEmpty() ?? "destination:unknown";
        Route = route;
        Body = body ?? Array.Empty<byte>();
        BodyHash = Hash(Body);
        IdempotencyKey = idempotencyKey?.Trim().NilIfEmpty();
        SentAt = new CanonicalTimestamp(sentAt ?? DateTime.UtcNow);
    }

    public bool HasValidBodyHash => Hash(Body).Equals(BodyHash);

    public static CanonicalHash Hash(byte[] data)
    {
        var digest = SHA256.HashData(data);
        var hex = string.Concat(digest.Select(b => b.ToString("x2")));
        return new CanonicalHash(hex);
    }

    public override bool Equals(object? obj) => obj is CanonicalTransportEnvelope other && Equals(other);
    public bool Equals(CanonicalTransportEnvelope? other) =>
        other is not null && RequestID == other.RequestID;
    public override int GetHashCode() => RequestID.GetHashCode();
    public static bool operator ==(CanonicalTransportEnvelope left, CanonicalTransportEnvelope right) => left.Equals(right);
    public static bool operator !=(CanonicalTransportEnvelope left, CanonicalTransportEnvelope right) => !left.Equals(right);
}

// ─── CanonicalTransportResponse ──────────────────────────────────────────────

public sealed class CanonicalTransportResponse : IEquatable<CanonicalTransportResponse>
{
    public bool Ok { get; }
    public string Status { get; }
    public byte[] Body { get; }
    public CanonicalHash BodyHash { get; }
    public string? Error { get; }

    public CanonicalTransportResponse(bool ok, string status, byte[]? body = null, string? error = null)
    {
        Ok = ok;
        Status = status.Trim().NilIfEmpty() ?? (ok ? "ok" : "failed");
        Body = body ?? Array.Empty<byte>();
        BodyHash = CanonicalTransportEnvelope.Hash(Body);
        Error = error?.Trim().NilIfEmpty();
    }

    public bool HasValidBodyHash => CanonicalTransportEnvelope.Hash(Body).Equals(BodyHash);

    public override bool Equals(object? obj) => obj is CanonicalTransportResponse other && Equals(other);
    public bool Equals(CanonicalTransportResponse? other) =>
        other is not null &&
        Ok == other.Ok &&
        Status == other.Status &&
        Body.SequenceEqual(other.Body) &&
        BodyHash.Equals(other.BodyHash) &&
        Error == other.Error;
    public override int GetHashCode() => HashCode.Combine(Ok, Status, BodyHash, Error);
    public static bool operator ==(CanonicalTransportResponse left, CanonicalTransportResponse right) => left.Equals(right);
    public static bool operator !=(CanonicalTransportResponse left, CanonicalTransportResponse right) => !left.Equals(right);
}

// ─── CanonicalTransportRuntimeError ──────────────────────────────────────────

public sealed class CanonicalTransportRuntimeError : Exception, IEquatable<CanonicalTransportRuntimeError>
{
    public string ErrorKind { get; }
    public string? NodeID { get; }
    public CanonicalTransportRoute? Route { get; }
    public CanonicalCapability? Capability { get; }
    public int? SchemaVersion { get; }

    private CanonicalTransportRuntimeError(
        string errorKind,
        string? nodeID = null,
        CanonicalTransportRoute? route = null,
        CanonicalCapability? capability = null,
        int? schemaVersion = null)
        : base(nodeID ?? errorKind)
    {
        ErrorKind = errorKind;
        NodeID = nodeID;
        Route = route;
        Capability = capability;
        SchemaVersion = schemaVersion;
    }

    public static CanonicalTransportRuntimeError NodeNotRegistered(string nodeID) =>
        new("nodeNotRegistered", nodeID: nodeID);

    public static CanonicalTransportRuntimeError RouteNotAllowed(CanonicalTransportRoute route) =>
        new("routeNotAllowed", route: route);

    public static CanonicalTransportRuntimeError CapabilityMissing(string nodeID, CanonicalCapability capability) =>
        new("capabilityMissing", nodeID: nodeID, capability: capability);

    public static CanonicalTransportRuntimeError InvalidBodyHash(string requestID) =>
        new("invalidBodyHash", nodeID: requestID);

    public static CanonicalTransportRuntimeError InvalidManifestHash(string nodeID) =>
        new("invalidManifestHash", nodeID: nodeID);

    public static CanonicalTransportRuntimeError IncompatibleSchema(int schemaVersion) =>
        new("incompatibleSchema", schemaVersion: schemaVersion);

    public static CanonicalTransportRuntimeError HandlerMissing(CanonicalTransportRoute route) =>
        new("handlerMissing", route: route);

    public override bool Equals(object? obj) => obj is CanonicalTransportRuntimeError other && Equals(other);
    public bool Equals(CanonicalTransportRuntimeError? other) =>
        other is not null &&
        ErrorKind == other.ErrorKind &&
        NodeID == other.NodeID &&
        Route == other.Route &&
        Capability == other.Capability &&
        SchemaVersion == other.SchemaVersion;
    public override int GetHashCode() => HashCode.Combine(ErrorKind, NodeID, Route, Capability, SchemaVersion);
    public static bool operator ==(CanonicalTransportRuntimeError left, CanonicalTransportRuntimeError right) => left.Equals(right);
    public static bool operator !=(CanonicalTransportRuntimeError left, CanonicalTransportRuntimeError right) => !left.Equals(right);
}

// ─── ICanonicalTransportPort (protocol → interface) ──────────────────────────

public interface ICanonicalTransportPort
{
    Task RegisterAsync(CanonicalNode node, HashSet<CanonicalTransportRoute>? allowedRoutes = null);
    Task<CanonicalTransportResponse> SendAsync(CanonicalTransportEnvelope envelope);
}

// ─── InMemoryCanonicalTransportRuntime ───────────────────────────────────────

public sealed class InMemoryCanonicalTransportRuntime : ICanonicalTransportPort
{
    public delegate Task<CanonicalTransportResponse> Handler(CanonicalTransportEnvelope envelope);

    private readonly Dictionary<string, CanonicalNode> _nodes = new();
    private readonly Dictionary<string, HashSet<CanonicalTransportRoute>> _routesByNodeID = new();
    private readonly Dictionary<string, Handler> _handlers = new();
    private readonly Dictionary<string, CanonicalTransportResponse> _idempotencyResponses = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task RegisterAsync(CanonicalNode node, HashSet<CanonicalTransportRoute>? allowedRoutes = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            _nodes[node.NodeID] = node;
            _routesByNodeID[node.NodeID] = allowedRoutes ?? new HashSet<CanonicalTransportRoute>(Enum.GetValues<CanonicalTransportRoute>());
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RegisterHandlerAsync(
        string nodeID,
        CanonicalTransportRoute route,
        Handler handler)
    {
        await _semaphore.WaitAsync();
        try
        {
            _handlers[HandlerKey(nodeID, route)] = handler;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<CanonicalTransportResponse> SendAsync(CanonicalTransportEnvelope envelope)
    {
        if (!envelope.HasValidBodyHash)
            throw CanonicalTransportRuntimeError.InvalidBodyHash(envelope.RequestID);

        await _semaphore.WaitAsync();
        try
        {
            if (!_nodes.TryGetValue(envelope.SourceNodeID, out var source))
                throw CanonicalTransportRuntimeError.NodeNotRegistered(envelope.SourceNodeID);

            if (!_nodes.TryGetValue(envelope.DestinationNodeID, out var destination))
                throw CanonicalTransportRuntimeError.NodeNotRegistered(envelope.DestinationNodeID);

            if (!_routesByNodeID.TryGetValue(envelope.DestinationNodeID, out var routes) ||
                !routes.Contains(envelope.Route))
                throw CanonicalTransportRuntimeError.RouteNotAllowed(envelope.Route);

            ValidateCapabilities(source, destination, envelope.Route);

            if (envelope.IdempotencyKey != null)
            {
                var replayKey = IdempotencyResponseKey(envelope, envelope.IdempotencyKey);
                if (_idempotencyResponses.TryGetValue(replayKey, out var cachedResponse))
                    return cachedResponse;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        // Dispatch outside the lock to allow handler execution without holding the lock
        var response = await DispatchAsync(envelope);

        if (envelope.IdempotencyKey != null)
        {
            var replayKey = IdempotencyResponseKey(envelope, envelope.IdempotencyKey);
            await _semaphore.WaitAsync();
            try
            {
                _idempotencyResponses[replayKey] = response;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return response;
    }

    public void ValidateManifest(CanonicalManifest manifest)
    {
        if (manifest.SchemaVersion != CanonicalManifest.CurrentSchemaVersion)
            throw CanonicalTransportRuntimeError.IncompatibleSchema(manifest.SchemaVersion);

        if (!manifest.HasValidManifestHash)
            throw CanonicalTransportRuntimeError.InvalidManifestHash(manifest.Node.NodeID);
    }

    private async Task<CanonicalTransportResponse> DispatchAsync(CanonicalTransportEnvelope envelope)
    {
        Handler? handler;
        await _semaphore.WaitAsync();
        try
        {
            if (!_handlers.TryGetValue(HandlerKey(envelope.DestinationNodeID, envelope.Route), out handler))
                throw CanonicalTransportRuntimeError.HandlerMissing(envelope.Route);
        }
        finally
        {
            _semaphore.Release();
        }

        var response = await handler!(envelope);

        if (!response.HasValidBodyHash)
            throw CanonicalTransportRuntimeError.InvalidBodyHash(envelope.RequestID);

        return response;
    }

    private static void ValidateCapabilities(
        CanonicalNode source,
        CanonicalNode destination,
        CanonicalTransportRoute route)
    {
        foreach (var capability in RequiredCapabilities(route))
        {
            if (!source.Capabilities.Contains(capability))
                throw CanonicalTransportRuntimeError.CapabilityMissing(source.NodeID, capability);

            if (!destination.Capabilities.Contains(capability))
                throw CanonicalTransportRuntimeError.CapabilityMissing(destination.NodeID, capability);
        }
    }

    private static CanonicalCapability[] RequiredCapabilities(CanonicalTransportRoute route) =>
        route switch
        {
            CanonicalTransportRoute.manifestExchange => new[] { CanonicalCapability.recordingMetadata },
            CanonicalTransportRoute.applyPlan or CanonicalTransportRoute.applyMetadata => new[] { CanonicalCapability.recordingMetadata },
            CanonicalTransportRoute.fileRead => new[] { CanonicalCapability.objectProjection },
            CanonicalTransportRoute.uploadStart or CanonicalTransportRoute.uploadStatus or CanonicalTransportRoute.uploadChunk or CanonicalTransportRoute.uploadFinalize =>
                new[] { CanonicalCapability.audioArtifact },
            _ => Array.Empty<CanonicalCapability>()
        };

    private static string HandlerKey(string nodeID, CanonicalTransportRoute route) =>
        $"{nodeID}|{route.ToString()}";

    private static string IdempotencyResponseKey(CanonicalTransportEnvelope envelope, string idempotencyKey) =>
        string.Join("|", new[]
        {
            envelope.SourceNodeID,
            envelope.DestinationNodeID,
            envelope.Route.ToString(),
            idempotencyKey
        });
}

// ─── CanonicalTransportJSON ──────────────────────────────────────────────────

public static class CanonicalTransportJSON
{
    public static byte[] Encode<T>(T value) where T : notnull
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        options.Converters.Add(new CanonicalTimestampJsonConverter());
        return JsonSerializer.SerializeToUtf8Bytes(value, options);
    }

    public static T? Decode<T>(byte[] data)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new CanonicalTimestampJsonConverter());
        return JsonSerializer.Deserialize<T>(data, options);
    }

    public static T? Decode<T>(string json)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new CanonicalTimestampJsonConverter());
        return JsonSerializer.Deserialize<T>(json, options);
    }

    private sealed class CanonicalTimestampJsonConverter : JsonConverter<CanonicalTimestamp>
    {
        public override CanonicalTimestamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value == null)
                return new CanonicalTimestamp(DateTime.UtcNow);

            // Try Unix timestamp (seconds as double)
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                return new CanonicalTimestamp(DateTime.UnixEpoch.AddSeconds(seconds));

            // Try ISO8601 with fractional seconds
            if (DateTime.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFZ", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
                return new CanonicalTimestamp(date);

            // Try ISO8601 without fractional seconds
            if (DateTime.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
                return new CanonicalTimestamp(date);

            throw new JsonException($"Invalid ISO8601 date: {value}");
        }

        public override void Write(Utf8JsonWriter writer, CanonicalTimestamp value, JsonSerializerOptions options)
        {
            var unixSeconds = (value.Date.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
            writer.WriteStringValue(unixSeconds.ToString("F6", CultureInfo.InvariantCulture));
        }
    }
}
