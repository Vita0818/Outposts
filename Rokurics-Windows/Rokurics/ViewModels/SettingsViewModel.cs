using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rokurics.Services;

namespace Rokurics.ViewModels;

/// <summary>
/// Validation status for transcription configuration resources.
/// Mirrors TranscriptionConfigurationValidator from Apple source.
/// </summary>
public enum WhisperResourceStatus
{
    NotConfigured,
    Valid,
    NotFound,
    AccessDenied,
    NotExecutable
}

/// <summary>
/// AI provider presets matching NoteGenerationProviderKind.Preset from Apple source.
/// </summary>
public enum AIProviderPreset
{
    CustomOpenAICompatible,
    LmStudioLocal,
    DeepSeek,
    OpenAI,
    Gemini
}

/// <summary>
/// ViewModel for settings page.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _selectedTranscriptionProvider = "Mock";
    [ObservableProperty] private string _selectedNoteProvider = "Mock";
    [ObservableProperty] private string _selectedChatProvider = "Mock";
    [ObservableProperty] private string _openAiBaseUrl = "";
    [ObservableProperty] private string _openAiApiKey = "";
    [ObservableProperty] private string _openAiModelName = "gpt-4o";
    [ObservableProperty] private string _anthropicBaseUrl = "https://api.anthropic.com";
    [ObservableProperty] private string _anthropicApiKey = "";
    [ObservableProperty] private string _anthropicModelName = "claude-sonnet-4-6";
    [ObservableProperty] private string _userDisplayName = "";
    [ObservableProperty] private string _userHandle = "";
    [ObservableProperty] private string _whisperModelName = "ggml-large-v3-turbo";
    [ObservableProperty] private string _whisperModelPath = "";
    [ObservableProperty] private string _whisperDefaultLanguage = "auto";
    [ObservableProperty] private bool _whisperPreferSegmentOutput;
    [ObservableProperty] private string _whisperExecutablePath = "";
    [ObservableProperty] private string _whisperCppRootDirectory = "";
    [ObservableProperty] private string _ffmpegExecutablePath = "";
    [ObservableProperty] private string _lastTranscriptionValidation = "";
    [ObservableProperty] private string _lastConnectionTestResult = "";
    [ObservableProperty] private string _lastModelTestResult = "";
    [ObservableProperty] private string _lastGenerationTestResult = "";
    [ObservableProperty] private string _macAddress = "localhost";
    [ObservableProperty] private int _macPort = 8787;
    [ObservableProperty] private bool _isMacConnected;
    [ObservableProperty] private string _connectionTestResult = "";
    [ObservableProperty] private string _anthropicVersion = "2023-06-01";
    [ObservableProperty] private AIProviderPreset _selectedProviderPreset = AIProviderPreset.CustomOpenAICompatible;

    // Whisper resource authorization states
    [ObservableProperty] private WhisperResourceStatus _whisperCliStatus = WhisperResourceStatus.NotConfigured;
    [ObservableProperty] private WhisperResourceStatus _whisperModelStatus = WhisperResourceStatus.NotConfigured;
    [ObservableProperty] private WhisperResourceStatus _whisperRootDirStatus = WhisperResourceStatus.NotConfigured;
    [ObservableProperty] private WhisperResourceStatus _ffmpegStatus = WhisperResourceStatus.NotConfigured;

    // Model candidates fetched from provider
    [ObservableProperty] private IReadOnlyList<AvailableModelInfo> _noteModelCandidates = Array.Empty<AvailableModelInfo>();
    [ObservableProperty] private IReadOnlyList<AvailableModelInfo> _chatModelCandidates = Array.Empty<AvailableModelInfo>();
    [ObservableProperty] private bool _isRefreshingModels;
    [ObservableProperty] private string _modelRefreshError = "";

    public List<string> TranscriptionProviderOptions { get; } = new() { "Mock", "Whisper.cpp" };
    public List<string> NoteProviderOptions { get; } = new() { "Mock", "OpenAI-compatible", "Claude / Anthropic" };
    public List<string> ChatProviderOptions { get; } = new() { "Mock", "OpenAI-compatible", "Claude / Anthropic" };

    public SettingsViewModel()
    {
        LoadSettings();
        _userDisplayName = Environment.UserName;
        _userHandle = Environment.UserName.ToLowerInvariant();
    }

    [RelayCommand]
    private void Save()
    {
        var settings = new AppSettings
        {
            TranscriptionProvider = SelectedTranscriptionProvider,
            NoteProvider = SelectedNoteProvider,
            ChatProvider = SelectedChatProvider,
            OpenAIBaseUrl = OpenAiBaseUrl,
            OpenAIApiKey = OpenAiApiKey,
            OpenAIModelName = OpenAiModelName,
            AnthropicBaseUrl = AnthropicBaseUrl,
            AnthropicApiKey = AnthropicApiKey,
            AnthropicModelName = AnthropicModelName,
            AnthropicVersion = AnthropicVersion,
            UserDisplayName = UserDisplayName,
            WhisperModelName = WhisperModelName,
            WhisperModelPath = WhisperModelPath,
            WhisperDefaultLanguage = WhisperDefaultLanguage,
            WhisperPreferSegmentOutput = WhisperPreferSegmentOutput,
            WhisperExecutablePath = WhisperExecutablePath,
            WhisperCppRootDirectory = WhisperCppRootDirectory,
            FfmpegExecutablePath = FfmpegExecutablePath,
            ProviderPreset = SelectedProviderPreset,
            MacAddress = MacAddress,
            MacPort = MacPort
        };
        AppSettings.Save(settings);
    }

    [RelayCommand]
    private async Task RefreshModels()
    {
        IsRefreshingModels = true;
        ModelRefreshError = "";
        try
        {
            var settings = new AppSettings
            {
                NoteProvider = SelectedNoteProvider,
                ChatProvider = SelectedChatProvider,
                OpenAIBaseUrl = OpenAiBaseUrl,
                OpenAIApiKey = OpenAiApiKey,
                AnthropicBaseUrl = AnthropicBaseUrl,
                AnthropicApiKey = AnthropicApiKey,
                AnthropicVersion = AnthropicVersion
            };

            var noteProvider = ProviderFactory.CreateNoteProvider(settings);
            var noteModels = await noteProvider.GetAvailableModelsAsync();
            NoteModelCandidates = noteModels;

            var chatProvider = ProviderFactory.CreateChatProvider(settings);
            var chatModels = await chatProvider.GetAvailableModelsAsync();
            ChatModelCandidates = chatModels;
        }
        catch (Exception ex)
        {
            ModelRefreshError = $"获取模型列表失败: {ex.Message}";
        }
        finally
        {
            IsRefreshingModels = false;
        }
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        ConnectionTestResult = "测试中...";
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var url = $"http://{MacAddress}:{MacPort}/health";
            var response = await client.GetAsync(url);
            ConnectionTestResult = response.IsSuccessStatusCode ? "连接成功" : $"失败: {response.StatusCode}";
            LastConnectionTestResult = ConnectionTestResult;
            IsMacConnected = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            ConnectionTestResult = $"连接失败: {ex.Message}";
            LastConnectionTestResult = ConnectionTestResult;
            IsMacConnected = false;
        }
    }

    private void LoadSettings()
    {
        var settings = AppSettings.Load();
        SelectedTranscriptionProvider = settings.TranscriptionProvider;
        SelectedNoteProvider = settings.NoteProvider;
        SelectedChatProvider = settings.ChatProvider;
        OpenAiBaseUrl = settings.OpenAIBaseUrl;
        OpenAiApiKey = settings.OpenAIApiKey;
        OpenAiModelName = settings.OpenAIModelName;
        AnthropicBaseUrl = settings.AnthropicBaseUrl;
        AnthropicApiKey = settings.AnthropicApiKey;
        AnthropicModelName = settings.AnthropicModelName;
        AnthropicVersion = settings.AnthropicVersion;
        UserDisplayName = settings.UserDisplayName;
        WhisperModelName = settings.WhisperModelName;
        WhisperModelPath = settings.WhisperModelPath;
        WhisperDefaultLanguage = settings.WhisperDefaultLanguage;
        WhisperPreferSegmentOutput = settings.WhisperPreferSegmentOutput;
        WhisperExecutablePath = settings.WhisperExecutablePath;
        WhisperCppRootDirectory = settings.WhisperCppRootDirectory;
        FfmpegExecutablePath = settings.FfmpegExecutablePath;
        SelectedProviderPreset = settings.ProviderPreset;
        MacAddress = settings.MacAddress;
        MacPort = settings.MacPort;
    }
}

public sealed class AppSettings
{
    public string TranscriptionProvider { get; set; } = "Mock";
    public string NoteProvider { get; set; } = "Mock";
    public string ChatProvider { get; set; } = "Mock";
    public string OpenAIBaseUrl { get; set; } = "";
    public string OpenAIApiKey { get; set; } = "";
    public string OpenAIModelName { get; set; } = "gpt-4o";
    public string AnthropicBaseUrl { get; set; } = "https://api.anthropic.com";
    public string AnthropicApiKey { get; set; } = "";
    public string AnthropicModelName { get; set; } = "claude-sonnet-4-6";
    public string AnthropicVersion { get; set; } = "2023-06-01";
    public string UserDisplayName { get; set; } = "";
    public string WhisperModelName { get; set; } = "ggml-large-v3-turbo";
    public string WhisperModelPath { get; set; } = "";
    public string WhisperDefaultLanguage { get; set; } = "auto";
    public bool WhisperPreferSegmentOutput { get; set; }
    public string WhisperExecutablePath { get; set; } = "";
    public string WhisperCppRootDirectory { get; set; } = "";
    public string FfmpegExecutablePath { get; set; } = "";
    public AIProviderPreset ProviderPreset { get; set; } = AIProviderPreset.CustomOpenAICompatible;
    public string MacAddress { get; set; } = "localhost";
    public int MacPort { get; set; } = 8787;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rokurics", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)
                    ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        var json = System.Text.Json.JsonSerializer.Serialize(settings,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
