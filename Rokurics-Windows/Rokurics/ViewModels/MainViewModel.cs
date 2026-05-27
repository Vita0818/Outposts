using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rokurics.Models;
using Rokurics.Services;
using Rokurics.Stores;

namespace Rokurics.ViewModels;

/// <summary>
/// Main navigation ViewModel — manages page state and app-wide services.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly RecordingManager _recordingManager;
    private readonly DeviceConnectionStatusStore _connectionStore;
    private readonly StudyLibrarySyncStateStore _syncStateStore;

    [ObservableProperty] private string _currentPage = "home";
    [ObservableProperty] private string _statusMessage = "录音默认仅保存在本地";
    [ObservableProperty] private string _connectionStatusText = "未连接";
    [ObservableProperty] private bool _isConnected;

    public RecordingManager RecordingManager => _recordingManager;
    public DeviceConnectionStatusStore ConnectionStore => _connectionStore;
    public StudyLibraryStore StudyLibraryStore => _recordingManager.StudyLibraryStore;

    public MainViewModel()
    {
        _recordingManager = new RecordingManager();
        _connectionStore = new DeviceConnectionStatusStore();
        _syncStateStore = new StudyLibrarySyncStateStore();

        _recordingManager.StateChanged += () =>
        {
            StatusMessage = _recordingManager.StatusMessage;
        };
    }

    [RelayCommand]
    private void Navigate(string page)
    {
        CurrentPage = page;
    }

    [RelayCommand]
    private void ToggleRecording()
    {
        switch (_recordingManager.State)
        {
            case RokuricsRecordingState.Recording:
                _recordingManager.StopRecording();
                break;
            case RokuricsRecordingState.Paused:
            case RokuricsRecordingState.Idle:
            case RokuricsRecordingState.Saved:
            case RokuricsRecordingState.Failed:
            case RokuricsRecordingState.PermissionDenied:
                _recordingManager.StartRecording();
                break;
        }
    }

    public bool IsRecording => _recordingManager.State == RokuricsRecordingState.Recording;
    public bool IsPaused => _recordingManager.State == RokuricsRecordingState.Paused;
    public string RecordingStateText => _recordingManager.State switch
    {
        RokuricsRecordingState.Idle => "开始录音",
        RokuricsRecordingState.Recording => "停止录音",
        RokuricsRecordingState.Paused => "继续录音",
        RokuricsRecordingState.ConfiguringSession => "配置中...",
        RokuricsRecordingState.Stopping => "停止中...",
        _ => "开始录音"
    };

    public string ElapsedDisplay => _recordingManager.ElapsedTime.TotalHours >= 1
        ? _recordingManager.ElapsedTime.ToString(@"h\:mm\:ss")
        : _recordingManager.ElapsedTime.ToString(@"m\:ss");

    public RokuricsRecordingState State => _recordingManager.State;
    public List<RecordingMetadata> Recordings => _recordingManager.Recordings;
    public RecordingMetadata? LatestRecording => _recordingManager.LatestRecordingMetadata;
}
