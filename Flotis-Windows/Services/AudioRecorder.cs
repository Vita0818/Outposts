using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace Flotis.Services;

public sealed class AudioRecorder
{
    private MediaCapture? _mediaCapture;
    private StorageFile? _recordingFile;
    private bool _isRecording;

    public async Task StartRecordingAsync()
    {
        await StopAndCleanupAsync(false);

        var mediaCapture = new MediaCapture();
        var settings = new MediaCaptureInitializationSettings
        {
            StreamingCaptureMode = StreamingCaptureMode.Audio,
            MediaCategory = MediaCategory.Speech,
            AudioProcessing = AudioProcessing.Default,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu
        };

        await mediaCapture.InitializeAsync(settings);
        _mediaCapture = mediaCapture;

        _recordingFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(
            $"flotis-{Guid.NewGuid()}.m4a",
            CreationCollisionOption.GenerateUniqueName
        );

        var profile = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.High);
        await _mediaCapture.StartRecordToStorageFileAsync(profile, _recordingFile);
        _isRecording = true;
    }

    public async Task<string?> StopRecordingAsync()
    {
        if (_mediaCapture is null || !_isRecording)
        {
            return null;
        }

        try
        {
            await _mediaCapture.StopRecordAsync();
        }
        catch
        {
            // Best effort stop.
        }

        var file = _recordingFile;
        _isRecording = false;
        _recordingFile = null;
        _mediaCapture.Dispose();
        _mediaCapture = null;

        return file?.Path;
    }

    public void CancelRecording()
    {
        _ = StopAndCleanupAsync(true);
    }

    private async Task StopAndCleanupAsync(bool deleteCurrent)
    {
        try
        {
            if (_mediaCapture != null && _isRecording)
            {
                await _mediaCapture.StopRecordAsync();
            }
        }
        catch
        {
            // Best effort.
        }

        if (deleteCurrent && _recordingFile != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _recordingFile.DeleteAsync();
                }
                catch
                {
                    // Ignore cleanup failures.
                }
            });
        }

        _mediaCapture?.Dispose();
        _mediaCapture = null;
        _recordingFile = null;
        _isRecording = false;
    }
}
