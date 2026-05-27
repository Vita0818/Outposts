namespace Rokurics.Services;

/// <summary>
/// Kestrel HTTPS endpoint routing definitions.
/// Mirrors SecureReceiverService route registration from Apple source.
///
/// Endpoint layout (matching Apple source receiver):
///   GET  /health              — health check (always 200)
///   POST /pairing/begin       — start pairing, returns 6-digit code + fingerprint
///   POST /pairing/verify      — verify pairing code from device
///   POST /pairing/complete    — complete pairing, exchange shared secret
///   GET  /pairing/status      — current pairing state
///   POST /upload              — receive recording file upload
///   GET  /sync/manifest       — get local study library manifest
///   POST /sync/apply          — apply remote sync changes
///   GET  /sync/status         — sync state summary
///   GET  /devices             — list paired devices
///   POST /devices/disconnect  — disconnect a paired device
///
/// REQUIRES: .NET 8+, Microsoft.AspNetCore.App, Kestrel server.
/// VALIDATION GAP: Cannot run on macOS without .NET SDK.
/// </summary>

/// <summary>
/// HTTP request/response models for Kestrel pairing endpoints.
/// </summary>

public sealed class BeginPairingResponse
{
    public string PairingCode { get; init; } = "";
    public string Fingerprint { get; init; } = "";
    public int ExpiresInSeconds { get; init; } = 300;
    public string MacAddress { get; init; } = "";
    public int Port { get; init; }
}

public sealed class VerifyPairingRequest
{
    public string PairingCode { get; init; } = "";
    public string DeviceId { get; init; } = "";
    public string DeviceName { get; init; } = "iPhone";
}

public sealed class VerifyPairingResponse
{
    public bool Accepted { get; init; }
    public string? SharedSecret { get; init; }
    public string? DeviceToken { get; init; }
    public string? Error { get; init; }
}

public sealed class CompletePairingRequest
{
    public string DeviceId { get; init; } = "";
    public string DeviceToken { get; init; } = "";
    public string SharedSecret { get; init; } = "";
}

public sealed class CompletePairingResponse
{
    public bool Success { get; init; }
    public string? DeviceToken { get; init; }
    public string? Error { get; init; }
}

public sealed class PairingStatusResponse
{
    public bool IsRunning { get; init; }
    public bool HasActiveCode { get; init; }
    public int? PairedDeviceCount { get; init; }
}

public sealed class UploadResponse
{
    public bool Accepted { get; init; }
    public string? RecordingId { get; init; }
    public string? Error { get; init; }
}

public sealed class SyncApplyRequest
{
    public string DeviceId { get; init; } = "";
    public List<SyncApplyItem> Items { get; init; } = new();
    public List<SyncApplyFolder> Folders { get; init; } = new();
    public List<string> ItemIdsToDelete { get; init; } = new();
    public string? Checksum { get; init; }
}

public sealed class SyncApplyItem
{
    public string ItemId { get; init; } = "";
    public string? RecordingId { get; init; }
    public string Title { get; init; } = "";
    public string? FilingType { get; init; }
    public string? FilingSubject { get; init; }
    public string? FilingChapter { get; init; }
    public string? FilingTopic { get; init; }
}

public sealed class SyncApplyFolder
{
    public string FolderId { get; init; } = "";
    public string? ParentFolderId { get; init; }
    public string Title { get; init; } = "";
    public string? ColorToken { get; init; }
}

public sealed class SyncApplyResponse
{
    public bool Accepted { get; init; }
    public int ItemsApplied { get; init; }
    public int FoldersApplied { get; init; }
    public int ItemsDeleted { get; init; }
    public string? Error { get; init; }
}

public sealed class DevicesResponse
{
    public List<PairedDeviceInfo> Devices { get; init; } = new();
    public int Count => Devices.Count;
}

public sealed class DisconnectRequest
{
    public string DeviceId { get; init; } = "";
}

/// <summary>
/// In-memory pairing state managed by KestrelReceiverService.
/// Mirrors PairingState from Apple source SecureReceiverService.
/// </summary>
public sealed class KestrelPairingState
{
    public string? ActivePairingCode { get; set; }
    public DateTime? PairingCodeExpiresAt { get; set; }
    public string? PendingDeviceId { get; set; }
    public string? PendingDeviceName { get; set; }
    public bool IsVerificationInProgress { get; set; }

    public bool IsCodeExpired =>
        ActivePairingCode is not null &&
        PairingCodeExpiresAt.HasValue &&
        DateTime.UtcNow > PairingCodeExpiresAt.Value;

    public void Clear()
    {
        ActivePairingCode = null;
        PairingCodeExpiresAt = null;
        PendingDeviceId = null;
        PendingDeviceName = null;
        IsVerificationInProgress = false;
    }
}

/// <summary>
/// Kestrel route handler interface.
/// Provides HTTP endpoint mapping for the Kestrel server.
/// Implementations wire these to ASP.NET Core minimal API endpoints.
///
/// REQUIRES: Microsoft.AspNetCore.App for actual HTTP hosting.
/// This interface allows DI and testing without the runtime.
/// </summary>
public interface IKestrelRouteHandler
{
    void MapRoutes(object app); // Maps to WebApplication or IEndpointRouteBuilder
    string[] RegisteredRoutes { get; }
}

/// <summary>
/// Kestrel route handler stub documenting the expected endpoint layout.
/// Actual implementation requires ASP.NET Core WebApplication.
/// </summary>
public sealed class KestrelRouteHandler : IKestrelRouteHandler
{
    public string[] RegisteredRoutes => new[]
    {
        "GET  /health",
        "POST /pairing/begin",
        "POST /pairing/verify",
        "POST /pairing/complete",
        "GET  /pairing/status",
        "POST /upload",
        "GET  /sync/manifest",
        "POST /sync/apply",
        "GET  /sync/status",
        "GET  /devices",
        "POST /devices/disconnect",
    };

    public void MapRoutes(object app)
    {
        // TODO: Wire to ASP.NET Core minimal API endpoints
        // var webApp = (WebApplication)app;
        // webApp.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        // webApp.MapPost("/pairing/begin", HandleBeginPairing);
        // webApp.MapPost("/pairing/verify", HandleVerifyPairing);
        // webApp.MapPost("/pairing/complete", HandleCompletePairing);
        // webApp.MapGet("/pairing/status", HandlePairingStatus);
        // webApp.MapPost("/upload", HandleUpload);
        // webApp.MapGet("/sync/manifest", HandleGetManifest);
        // webApp.MapPost("/sync/apply", HandleApplySync);
        // webApp.MapGet("/sync/status", HandleSyncStatus);
        // webApp.MapGet("/devices", HandleListDevices);
        // webApp.MapPost("/devices/disconnect", HandleDisconnect);
        throw new NotImplementedException("Kestrel route mapping requires ASP.NET Core runtime");
    }
}

/// <summary>
/// Kestrel receiver configuration.
/// Mirrors SecureReceiverConfiguration from Apple source.
/// </summary>
public sealed class KestrelReceiverConfiguration
{
    /// <summary>HTTPS port (default 8787 matching Apple source).</summary>
    public int Port { get; set; } = 8787;

    /// <summary>Certificate common name for self-signed cert.</summary>
    public string CertificateCommonName { get; set; } = "Rokurics-Local";

    /// <summary>Certificate validity in days.</summary>
    public int CertificateValidityDays { get; set; } = 365;

    /// <summary>Max upload file size in bytes (default 500MB).</summary>
    public long MaxUploadSizeBytes { get; set; } = 500 * 1024 * 1024;

    /// <summary>Upload directory for received recordings.</summary>
    public string UploadDirectory { get; set; } =
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rokurics", "Inbox");

    /// <summary>Pairing code length (6 digits).</summary>
    public int PairingCodeLength { get; set; } = 6;

    /// <summary>Pairing code expiry in seconds (default 5 minutes).</summary>
    public int PairingCodeExpirySeconds { get; set; } = 300;

    public string DisplayUrl => $"https://0.0.0.0:{Port}";
    public string HealthUrl => $"{DisplayUrl}/health";
}
