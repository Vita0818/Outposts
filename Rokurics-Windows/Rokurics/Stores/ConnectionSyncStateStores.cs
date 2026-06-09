using System.Text.Json;
using Rokurics.Models;

namespace Rokurics.Stores;

/// <summary>
/// Device connection status store. Mirrors DeviceConnectionStatusStore from source.
/// </summary>
public class DeviceConnectionStatusStore
{
    private readonly string _storePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Dictionary<string, DeviceConnectionStatus> StatusesByDeviceId { get; private set; } = new();
    public string? LastError { get; private set; }

    public DeviceConnectionStatusStore(string? rootPath = null)
    {
        var syncDir = rootPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rokurics", "Sync");
        _storePath = Path.Combine(syncDir, "device-connection-status.json");
        Load();
    }

    public DeviceConnectionStatus? LatestStatus =>
        StatusesByDeviceId.Values
            .OrderByDescending(s => s.LastSeenAt ?? s.LastHeartbeatAt ?? DateTime.MinValue)
            .FirstOrDefault();

    /// <summary>
    /// Convenience accessor for the current/latest paired device.
    /// Returns null if no device is paired.
    /// </summary>
    public DeviceConnectionStatus? CurrentDevice =>
        StatusesByDeviceId.Values
            .Where(s => s.State == "connected" || s.State == "paired")
            .OrderByDescending(s => s.LastSeenAt ?? DateTime.MinValue)
            .FirstOrDefault();

    /// <summary>
    /// Count of accepted upload requests (diagnostics).
    /// Mirrors SecureReceiverService.AcceptedUploadCount.
    /// </summary>
    public int AcceptedUploadCount { get; set; }

    /// <summary>
    /// Last accepted file name (diagnostics).
    /// Mirrors SecureReceiverService.LastAcceptedFileName.
    /// </summary>
    public string? LastAcceptedFileName { get; set; }

    public DeviceConnectionStatus MarkConnected(string deviceId, string displayName)
    {
        var now = DateTime.UtcNow;
        var status = GetOrCreate(deviceId, displayName);
        status.State = "connected";
        status.DisplayName = displayName;
        status.LastSeenAt = now;
        status.LastHeartbeatAt = now;
        status.PresenceState = "online";
        status.MissedHeartbeatCount = 0;
        status.ConsecutiveFailureCount = 0;
        status.LastError = null;
        status.LastErrorCode = null;
        status.ConnectionStatusRevision = (status.ConnectionStatusRevision ?? 0) + 1;
        Save();
        return status;
    }

    public DeviceConnectionStatus MarkOffline(string deviceId, string displayName, string? error = null)
    {
        var status = GetOrCreate(deviceId, displayName);
        status.State = "offline";
        status.PresenceState = "disconnected";
        status.LastError = error;
        status.LastErrorCode = error;
        status.ConnectionStatusRevision = (status.ConnectionStatusRevision ?? 0) + 1;
        Save();
        return status;
    }

    public DeviceConnectionStatus MarkConnecting(string deviceId, string displayName)
    {
        var status = GetOrCreate(deviceId, displayName);
        status.State = "connecting";
        status.PresenceState = "connecting";
        status.MonitoringMode = "foregroundActive";
        status.LastError = null;
        status.LastErrorCode = null;
        status.ConnectionStatusRevision = (status.ConnectionStatusRevision ?? 0) + 1;
        Save();
        return status;
    }

    public DeviceConnectionStatus MarkUnpaired(string displayName = "未配对")
    {
        StatusesByDeviceId.Clear();
        var status = DeviceConnectionStatus.Unpaired(displayName);
        StatusesByDeviceId[""] = status;
        Save();
        return status;
    }

    public DeviceConnectionStatus RecordSyncResult(string deviceId, string displayName,
        string statusText, bool success, string? error = null)
    {
        var status = GetOrCreate(deviceId, displayName);
        status.LastSyncAt = DateTime.UtcNow;
        status.LastSyncStatus = statusText;
        if (success)
        {
            status.State = "connected";
            status.LastSeenAt = DateTime.UtcNow;
            status.LastHeartbeatAt = DateTime.UtcNow;
            status.PresenceState = "online";
            status.MissedHeartbeatCount = 0;
            status.ConsecutiveFailureCount = 0;
            status.LastError = null;
            status.LastErrorCode = null;
        }
        else
        {
            status.LastError = error;
            status.LastErrorCode = error;
        }
        status.ConnectionStatusRevision = (status.ConnectionStatusRevision ?? 0) + 1;
        Save();
        return status;
    }

    private DeviceConnectionStatus GetOrCreate(string deviceId, string displayName)
    {
        if (StatusesByDeviceId.TryGetValue(deviceId, out var existing))
            return existing;

        var status = new DeviceConnectionStatus
        {
            DeviceId = deviceId,
            DisplayName = displayName,
            State = "connecting"
        };
        StatusesByDeviceId[deviceId] = status;
        return status;
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_storePath))
            {
                var json = File.ReadAllText(_storePath);
                StatusesByDeviceId = JsonSerializer.Deserialize<Dictionary<string, DeviceConnectionStatus>>(json, JsonOptions)
                    ?? new Dictionary<string, DeviceConnectionStatus>();
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            StatusesByDeviceId = new Dictionary<string, DeviceConnectionStatus>();
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
            var json = JsonSerializer.Serialize(StatusesByDeviceId, JsonOptions);
            File.WriteAllText(_storePath, json);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }
}

/// <summary>
/// Study library sync state store. Mirrors StudyLibrarySyncStateStore from source.
/// </summary>
public class StudyLibrarySyncStateStore
{
    private readonly string _storePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StudyLibrarySyncState State { get; private set; } = new();
    public string? LastError { get; private set; }

    public StudyLibrarySyncStateStore(string? rootPath = null)
    {
        var syncDir = rootPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rokurics", "Sync");
        _storePath = Path.Combine(syncDir, "study-library-sync-state.json");
        Load();
    }

    public void RecordPull(string deviceId, string? remoteManifestHash, DateTime? date = null)
    {
        State.DeviceId = deviceId;
        State.LastPulledAt = date ?? DateTime.UtcNow;
        State.LastRemoteManifestHash = remoteManifestHash;
        State.LastError = null;
        Save();
    }

    public void RecordPush(string deviceId, string? remoteManifestHash, int pendingUploads = 0, DateTime? date = null)
    {
        var d = date ?? DateTime.UtcNow;
        State.DeviceId = deviceId;
        State.LastPushedAt = d;
        State.LastSuccessfulSyncAt = d;
        State.LastRemoteManifestHash = remoteManifestHash;
        State.PendingLocalChanges = 0;
        State.PendingUploads = pendingUploads;
        State.FailedChanges = 0;
        State.LastError = null;
        Save();
    }

    public void RecordFailure(string deviceId, string error, int failedChanges = 1)
    {
        State.DeviceId = deviceId;
        State.PendingLocalChanges = Math.Max(State.PendingLocalChanges, failedChanges);
        State.FailedChanges = Math.Max(State.FailedChanges, failedChanges);
        State.LastError = error;
        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_storePath))
            {
                var json = File.ReadAllText(_storePath);
                State = JsonSerializer.Deserialize<StudyLibrarySyncState>(json, JsonOptions) ?? new StudyLibrarySyncState();
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
            var json = JsonSerializer.Serialize(State, JsonOptions);
            File.WriteAllText(_storePath, json);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }
}

/// <summary>
/// Local network sync lifecycle store. Mirrors LocalNetworkSyncStateStore from Apple source.
/// Tracks attempt/success/failure with exponential backoff and aggressive scheduling.
/// </summary>
public class LocalNetworkSyncStateStore
{
    private readonly string _storePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LocalNetworkSyncState State { get; private set; } = new();
    public string? LastError { get; private set; }

    public LocalNetworkSyncStateStore(string? rootPath = null)
    {
        var syncDir = rootPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rokurics", "Sync");
        _storePath = Path.Combine(syncDir, "local-network-sync-state.json");
        Load();
    }

    public void RecordAttempt(
        string peerDeviceID,
        string localInventoryHash,
        string peerInventoryHash,
        int pendingUploadCount,
        int pendingDownloadCount,
        string? localDeviceID = null,
        string? planSummary = null,
        int? conflictCount = null,
        DateTime? date = null
    )
    {
        var d = date ?? DateTime.UtcNow;
        State.Version = Math.Max(State.Version, LocalNetworkSyncState.CurrentVersion);
        State.LocalDeviceID = localDeviceID ?? State.LocalDeviceID;
        State.PeerDeviceID = peerDeviceID;
        State.LastSyncStartedAt = d;
        State.LastSyncAt = d;
        State.LastPeerDeviceID = peerDeviceID;
        State.LastLocalInventoryHash = localInventoryHash;
        State.LastPeerInventoryHash = peerInventoryHash;
        State.PendingUploadCount = pendingUploadCount;
        State.PendingDownloadCount = pendingDownloadCount;
        State.LastPlanSummary = planSummary;
        State.LastConflictCount = conflictCount;
        State.ConsecutiveFailureCount = 0;
        State.LastErrorCode = null;
        State.LastErrorMessage = null;
        Save();
    }

    public void RecordSuccess(
        string peerDeviceID,
        string localInventoryHash,
        string peerInventoryHash,
        string? appliedPeerRevision = null,
        int pendingUploadCount = 0,
        int pendingDownloadCount = 0,
        DateTime? date = null
    )
    {
        var d = date ?? DateTime.UtcNow;
        State.Version = LocalNetworkSyncState.CurrentVersion;
        State.PeerDeviceID = peerDeviceID;
        State.LastSyncCompletedAt = d;
        State.LastSyncAt = d;
        State.LastSuccessfulSyncAt = d;
        State.LastPeerDeviceID = peerDeviceID;
        State.LastLocalInventoryHash = localInventoryHash;
        State.LastPeerInventoryHash = peerInventoryHash;
        State.LastAppliedPeerRevision = appliedPeerRevision;
        State.ConsecutiveFailureCount = 0;
        State.NextAllowedSyncAt = null;
        State.LastErrorCode = null;
        State.LastErrorMessage = null;
        State.PendingUploadCount = pendingUploadCount;
        State.PendingDownloadCount = pendingDownloadCount;
        if (pendingUploadCount == 0 && pendingDownloadCount == 0)
        {
            State.ActiveTransfers = new List<LocalNetworkSyncTransferProgress>();
        }
        Save();
    }

    public void RecordFailure(
        string? code,
        string? message,
        DateTime? date = null,
        long minimumBackoff = 30L,
        long maximumBackoff = 600L
    )
    {
        var d = date ?? DateTime.UtcNow;
        State.Version = LocalNetworkSyncState.CurrentVersion;
        var failures = State.ConsecutiveFailureCount + 1;
        var exponent = Math.Max(0, failures - 1);
        var delaySeconds = Math.Min(minimumBackoff * (1L << exponent), maximumBackoff);

        State.LastSyncAt = d;
        State.LastSyncCompletedAt = d;
        State.ConsecutiveFailureCount = failures;
        State.NextAllowedSyncAt = d.AddSeconds(delaySeconds);
        State.LastErrorCode = code;
        State.LastErrorMessage = message;
        Save();
    }

    public void RecordControlPlane(string syncRunID, string state, DateTime? date = null)
    {
        var d = date ?? DateTime.UtcNow;
        State.Version = LocalNetworkSyncState.CurrentVersion;
        State.ActiveSyncRunID = syncRunID;
        State.ControlPlaneState = state;
        State.LastControlPlaneUpdatedAt = d;

        if (state.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            State.LastSyncCompletedAt = d;
            State.LastSuccessfulSyncAt = d;
            State.NextAllowedSyncAt = null;
            State.ConsecutiveFailureCount = 0;
            State.LastErrorCode = null;
            State.LastErrorMessage = null;
        }
        else if (state.Equals("failed", StringComparison.OrdinalIgnoreCase))
        {
            State.LastSyncCompletedAt = d;
        }
        else if (state.Equals("syncStartAcked", StringComparison.OrdinalIgnoreCase) ||
                 state.Equals("inventoryExchanging", StringComparison.OrdinalIgnoreCase))
        {
            State.LastSyncStartedAt = State.LastSyncStartedAt ?? d;
            State.LastSyncAt = d;
        }
        Save();
    }

    public void RecordActiveTransfers(List<LocalNetworkSyncTransferProgress> transfers)
    {
        State.ActiveTransfers = transfers;
        Save();
    }

    public void ResetBackoff()
    {
        State.ConsecutiveFailureCount = 0;
        State.LastErrorCode = null;
        State.LastErrorMessage = null;
        State.NextAllowedSyncAt = null;
        Save();
    }

    /// <summary>Compatibility overload retained for older calls.</summary>
    public void RecordAttempt(DateTime? date = null) =>
        RecordAttempt(
            peerDeviceID: State.PeerDeviceID ?? string.Empty,
            localInventoryHash: State.LastLocalInventoryHash ?? string.Empty,
            peerInventoryHash: State.LastPeerInventoryHash ?? string.Empty,
            pendingUploadCount: 0,
            pendingDownloadCount: 0,
            date: date
        );

    /// <summary>Compatibility overload retained for older calls.</summary>
    public void RecordSuccess(DateTime? date = null) =>
        RecordSuccess(
            peerDeviceID: State.PeerDeviceID ?? string.Empty,
            localInventoryHash: State.LastLocalInventoryHash ?? string.Empty,
            peerInventoryHash: State.LastPeerInventoryHash ?? string.Empty,
            date: date
        );

    /// <summary>Compatibility overload retained for older calls.</summary>
    public void RecordFailure(string error, DateTime? date = null) =>
        RecordFailure(code: "sync_error", message: error, date: date);

    private void Load()
    {
        try
        {
            if (File.Exists(_storePath))
            {
                var json = File.ReadAllText(_storePath);
                State = JsonSerializer.Deserialize<LocalNetworkSyncState>(json, JsonOptions)
                    ?? new LocalNetworkSyncState();
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
            var json = JsonSerializer.Serialize(State, JsonOptions);
            File.WriteAllText(_storePath, json);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }
}
