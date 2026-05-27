using Rokurics.Models;

namespace Rokurics.Services;

/// <summary>
/// Infrastructure interface stubs for Windows/.NET components that require
/// the .NET SDK and Windows runtime to build/test.
///
/// These interfaces mirror the Apple source counterparts:
/// - SecureReceiverService → IKestrelReceiverService
/// - TranscriptionProvider/WhisperCppTranscriptionProvider → IWhisperCppProvider
/// - PairingManager → IPairingService
/// - Audio capture (WASAPI) → IWindowsAudioCapture
///
/// WINDOWS/.NET VALIDATION GAP: These cannot be built or tested on macOS
/// without the .NET SDK. Each stub documents what's needed and the
/// corresponding Apple source file for reference.
/// </summary>

// ──────────────────────────────────────────────────────────────────
// Kestrel HTTPS Receiver Service
// Apple source: SecureReceiverService.swift, SecureLocalHTTPServer.swift
// ──────────────────────────────────────────────────────────────────

/// Mirrors SecureReceiverService from Apple source.
/// Handles HTTPS server lifecycle, pairing, device management.
public interface IKestrelReceiverService
{
    /// Whether the HTTPS server is currently running.
    bool IsRunning { get; }

    /// Local IP address for pairing display.
    string LocalIPAddress { get; }

    /// HTTPS port (default 8787).
    int Port { get; }

    /// Certificate fingerprint for pairing verification.
    string Fingerprint { get; }

    /// Active 6-digit pairing code, if any.
    string PairingCode { get; }

    /// Whether a pairing code is currently active.
    bool HasActivePairingCode { get; }

    /// Whether the service can start pairing.
    bool CanPair { get; }

    /// Whether the service can start HTTPS.
    bool CanStartHttps { get; }

    /// Latest paired device, if any.
    PairedDeviceInfo? LatestPairedDevice { get; }

    /// Start the Kestrel HTTPS server with self-signed certificate.
    void StartSecureReceiving();

    /// Stop the HTTPS server.
    void StopSecureReceiving();

    /// Begin pairing flow — generates a 6-digit code.
    void BeginPairing();

    /// Disconnect all paired devices.
    void DisconnectPairedDevices();

    /// Number of accepted upload requests (diagnostics).
    int AcceptedUploadCount { get; }

    /// Last accepted file name (diagnostics).
    string LastAcceptedFileName { get; }
}

/// Represents a paired device for connection status display.
public sealed class PairedDeviceInfo
{
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "iPhone";
    public string IdPrefix => DeviceId.Length >= 8 ? DeviceId[..8] : DeviceId;
    public DateTime PairedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncStatus { get; set; }
}

/// Kestrel-based HTTPS server implementation stub.
/// REQUIRES: .NET 8+, Microsoft.AspNetCore.App, Windows or Linux host.
/// VALIDATION GAP: Cannot run on macOS without .NET SDK.
public sealed class KestrelReceiverService : IKestrelReceiverService
{
    public bool IsRunning => false;
    public string LocalIPAddress => "未知";
    public int Port => 8787;
    public string Fingerprint => "未生成";
    public string PairingCode => "";
    public bool HasActivePairingCode => false;
    public bool CanPair => false;
    public bool CanStartHttps => false;
    public PairedDeviceInfo? LatestPairedDevice => null;
    public int AcceptedUploadCount => 0;
    public string LastAcceptedFileName => "暂无";

    public void StartSecureReceiving()
    {
        // TODO: Configure Kestrel with self-signed certificate
        // 1. Generate X.509 certificate (SelfSignedCertificateBuilder in Apple source)
        // 2. Configure Kestrel to listen on HTTPS with that certificate
        // 3. Set up endpoint routing for /health, /pairing, /upload, /sync
        throw new NotImplementedException("Kestrel server requires .NET runtime on Windows/Linux");
    }

    public void StopSecureReceiving()
    {
        throw new NotImplementedException("Kestrel server requires .NET runtime on Windows/Linux");
    }

    public void BeginPairing()
    {
        throw new NotImplementedException("Pairing requires Kestrel server running");
    }

    public void DisconnectPairedDevices()
    {
        throw new NotImplementedException("Kestrel server requires .NET runtime on Windows/Linux");
    }
}

// ──────────────────────────────────────────────────────────────────
// Self-Signed Certificate Builder
// Apple source: SelfSignedCertificateBuilder.swift
// ──────────────────────────────────────────────────────────────────

/// Generates self-signed X.509 certificates for HTTPS.
/// Apple source uses Security framework (SecCertificate, SecKey).
/// Windows/.NET uses System.Security.Cryptography.X509Certificates.
public static class SelfSignedCertificateHelper
{
    /// Generate a self-signed certificate for local HTTPS.
    /// REQUIRES: .NET 8+, Windows/Linux (macOS may have keychain restrictions).
    public static byte[] GeneratePfx(string commonName, int validityDays = 365)
    {
        // TODO: Use CertificateRequest from System.Security.Cryptography.X509Certificates
        // var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        // var req = new CertificateRequest($"CN={commonName}", ecdsa, HashAlgorithmName.SHA256);
        // var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(validityDays));
        // return cert.Export(X509ContentType.Pfx);
        throw new NotImplementedException("Certificate generation requires .NET runtime on Windows/Linux");
    }

    /// Compute fingerprint (SHA-256 hex) matching Apple source format.
    public static string ComputeFingerprint(byte[] certificateDer)
    {
        // TODO: SHA256.HashData(certificateDer) → uppercase hex with space grouping
        throw new NotImplementedException("Certificate fingerprint requires .NET runtime on Windows/Linux");
    }
}

// ──────────────────────────────────────────────────────────────────
// Whisper.cpp Transcription Provider
// Apple source: WhisperCppTranscriptionProvider.swift
// ──────────────────────────────────────────────────────────────────

/// Configuration for Whisper.cpp provider.
/// Mirrors WhisperCppTranscriptionConfiguration from Apple source.
public sealed class WhisperCppConfiguration
{
    /// Path to the whisper.cpp model file (e.g., ggml-large-v3-turbo.bin).
    public string ModelPath { get; set; } = "";

    /// Path to the whisper.cpp executable or library.
    public string RuntimePath { get; set; } = "";

    /// Model display name for UI.
    public string CurrentModelDisplayName
    {
        get
        {
            try { return System.IO.Path.GetFileNameWithoutExtension(ModelPath); }
            catch { return "未选择模型"; }
        }
    }

    /// Whether the model file exists on disk.
    public bool IsModelFileResolved =>
        !string.IsNullOrEmpty(ModelPath) && System.IO.File.Exists(ModelPath);

    /// Whether the runtime binary exists on disk.
    public bool IsRuntimeResolved =>
        !string.IsNullOrEmpty(RuntimePath) && System.IO.File.Exists(RuntimePath);
}

/// Whisper.cpp transcription provider stub.
/// Apple source spawns whisper.cpp CLI process with model + audio file.
/// Windows approach: Process.Start with whisper.cpp executable or
/// bindings via P/Invoke or whisper.net library.
///
/// REQUIRES: Windows x64, whisper.cpp binary, ggml model file.
/// VALIDATION GAP: whisper.cpp binary not available on macOS for Windows target.
/// Options:
///   A) Cross-compile whisper.cpp for Windows from macOS
///   B) Use whisper.net NuGet package (managed bindings)
///   C) HTTP-based whisper service (delegate to a running whisper server)
public sealed class WhisperCppProvider : ITranscriptionProvider
{
    private readonly WhisperCppConfiguration _config;

    public string Id => "whisperCpp";
    public string DisplayName => "Whisper.cpp (本地)";

    public WhisperCppProvider(WhisperCppConfiguration config)
    {
        _config = config;
    }

    public Task<TranscriptionResult> TranscribeAsync(TranscriptionRequest request)
    {
        // TODO: Execute whisper.cpp process
        // 1. Validate model file and runtime exist
        // 2. Build process arguments: -m <model> -f <audio> -l zh --output-txt
        // 3. Run process, capture stdout (transcript)
        // 4. Parse output into TranscriptionResult with segments
        throw new NotImplementedException("Whisper.cpp requires whisper binary for Windows target");
    }

    public Task ValidateConfigurationAsync()
    {
        if (!_config.IsModelFileResolved)
            throw new InvalidOperationException("Whisper model file not found");
        if (!_config.IsRuntimeResolved)
            throw new InvalidOperationException("Whisper runtime not found");
        return Task.CompletedTask;
    }

    public Task CancelAsync(string recordingId)
    {
        // TODO: Track running processes by recordingId and kill them
        return Task.CompletedTask;
    }
}

// ──────────────────────────────────────────────────────────────────
// WASAPI Audio Capture
// Apple source: AVAudioEngine (macOS), no direct Windows equivalent
// ──────────────────────────────────────────────────────────────────

/// Windows audio capture using WASAPI.
/// Mirrors audio recording capability from Apple source (AVAudioEngine).
///
/// REQUIRES: Windows 10+, NAudio or Windows.Media.Audio namespace.
/// VALIDATION GAP: Cannot test on macOS. NAudio NuGet package available
/// but requires Windows runtime for actual audio device access.
public interface IWindowsAudioCapture
{
    bool IsRecording { get; }
    bool IsSupported { get; }
    string CurrentDeviceId { get; }
    IReadOnlyList<AudioDeviceInfo> AvailableDevices { get; }

    Task StartCaptureAsync(string outputFilePath);
    Task StopCaptureAsync();
    IReadOnlyList<AudioDeviceInfo> EnumerateDevices();
}

public sealed class AudioDeviceInfo
{
    public string DeviceId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsDefault { get; set; }
}

/// WASAPI audio capture stub.
/// Uses NAudio for Windows audio capture.
public sealed class WindowsAudioCapture : IWindowsAudioCapture
{
    public bool IsRecording => false;
    public bool IsSupported => OperatingSystem.IsWindows();
    public string CurrentDeviceId => "";
    public IReadOnlyList<AudioDeviceInfo> AvailableDevices => Array.Empty<AudioDeviceInfo>();

    public Task StartCaptureAsync(string outputFilePath)
    {
        // TODO: NAudio WasapiCapture or Windows.Media.Audio.AudioGraph
        // 1. Enumerate capture devices via MMDeviceEnumerator
        // 2. Create WasapiCapture with desired format (16kHz mono for speech)
        // 3. Start recording to WAV file
        throw new NotImplementedException("WASAPI capture requires Windows runtime and NAudio");
    }

    public Task StopCaptureAsync()
    {
        throw new NotImplementedException("WASAPI capture requires Windows runtime and NAudio");
    }

    public IReadOnlyList<AudioDeviceInfo> EnumerateDevices()
    {
        // TODO: MMDeviceEnumerator to list capture devices
        throw new NotImplementedException("Device enumeration requires Windows runtime");
    }
}

// ──────────────────────────────────────────────────────────────────
// Pairing Protocol
// Apple source: PairingManager.swift, RequestVerifier.swift
// ──────────────────────────────────────────────────────────────────

/// Pairing protocol management.
/// Mirrors PairingManager from Apple source.
///
/// Protocol flow (Apple parity):
/// 1. Mac starts HTTPS server, generates pairing code (6 digits)
/// 2. iPhone connects, sends pairing request with code
/// 3. Mac verifies code, generates shared secret
/// 4. Fingerprint exchange for MITM protection
/// 5. Device trust store persists paired devices
///
/// REQUIRES: Kestrel HTTPS server running, System.Security.Cryptography.
/// VALIDATION GAP: Full end-to-end pairing cannot be tested without
/// actual iPhone device and Kestrel server on Windows.
public interface IPairingService
{
    string GeneratePairingCode();
    bool IsPairingCodeValid(string code);
    Task<PairedDeviceInfo> CompletePairingAsync(string deviceId, string sharedSecret);
    Task<bool> VerifyFingerprintAsync(string deviceId, string fingerprint);
    IReadOnlyList<PairedDeviceInfo> PairedDevices { get; }
}

/// Pairing protocol stub.
public sealed class PairingService : IPairingService
{
    private readonly List<PairedDeviceInfo> _devices = new();

    public IReadOnlyList<PairedDeviceInfo> PairedDevices => _devices;

    public string GeneratePairingCode()
    {
        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes) % 1_000_000;
        return value.ToString("D6");
    }

    public bool IsPairingCodeValid(string code) =>
        code.Length == 6 && code.All(char.IsDigit);

    public Task<PairedDeviceInfo> CompletePairingAsync(string deviceId, string sharedSecret)
    {
        var device = new PairedDeviceInfo
        {
            DeviceId = deviceId,
            DeviceName = "iPhone",
            PairedAt = DateTime.Now,
            LastSeenAt = DateTime.Now
        };
        _devices.Add(device);
        return Task.FromResult(device);
    }

    public Task<bool> VerifyFingerprintAsync(string deviceId, string fingerprint) =>
        Task.FromResult(true);
}
