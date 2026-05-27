using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Rokurics.Services;
using Rokurics.ViewModels;

namespace Rokurics.Views;

/// <summary>
/// Reusable provider detail card showing full configuration for a single provider.
/// Mirrors the provider detail sections from Apple source MacSettingsView.
///
/// Supports three provider kinds: Transcription (Whisper.cpp), NoteGeneration (AI),
/// and Chat. Shows model list with metadata, API settings, connection test,
/// and preset selection.
/// </summary>
public sealed partial class ProviderDetailCard : UserControl
{
    public enum ProviderCardKind { Transcription, NoteGeneration, Chat }

    private readonly SettingsViewModel _settingsVm;
    private readonly ProviderCardKind _kind;

    /// <summary>
    /// Fires when the provider selection or settings change and parent should refresh.
    /// </summary>
    public event Action? ProviderSettingsChanged;

    public ProviderDetailCard()
    {
        InitializeComponent();
        _settingsVm = App.Current.Services.GetService<SettingsViewModel>()
            ?? new SettingsViewModel();
        _kind = ProviderCardKind.NoteGeneration;
        Loaded += (_, _) => ConfigureFor(ProviderCardKind.NoteGeneration);
    }

    public void ConfigureFor(ProviderCardKind kind)
    {
        switch (kind)
        {
            case ProviderCardKind.Transcription:
                ConfigureTranscription();
                break;
            case ProviderCardKind.NoteGeneration:
                ConfigureNoteGeneration();
                break;
            case ProviderCardKind.Chat:
                ConfigureChat();
                break;
        }
    }

    // ── Transcription provider ────────────────────────────────────

    private void ConfigureTranscription()
    {
        ProviderNameBlock.Text = "Whisper.cpp (本地)";
        ProviderIdBlock.Text = "whisperCpp";
        TypeBadgeText.Text = "转写";
        TypeBadge.Background = new SolidColorBrush(Color.FromArgb(255, 52, 199, 89));
        ProviderIconGlyph.Glyph = "";
        ProviderIconCircle.Background = new SolidColorBrush(Color.FromArgb(255, 52, 199, 89));

        // Model list: Whisper.cpp models
        var models = new[] { "ggml-large-v3-turbo", "ggml-medium", "ggml-small", "ggml-tiny", "ggml-base" };
        ModelsListView.ItemsSource = models;

        // Select current model
        var currentModel = _settingsVm.WhisperModelName;
        var items = models.ToList();
        var idx = items.IndexOf(currentModel);
        if (idx >= 0) ModelsListView.SelectedIndex = idx;

        ModelCountBlock.Text = $"{models.Length} 个模型";
        ModelNameBox.Text = currentModel;

        // Hide OpenAI preset selector, show only whisper-specific fields
        PresetSelectorRow.Visibility = Visibility.Collapsed;
        VersionRow.Visibility = Visibility.Collapsed;
        BaseUrlBox.Visibility = Visibility.Collapsed;
        ApiKeyBox.Visibility = Visibility.Collapsed;

        UpdateStatusPill(
            _settingsVm.WhisperCliStatus == WhisperResourceStatus.Valid
                ? "已配置" : "未配置",
            _settingsVm.WhisperCliStatus == WhisperResourceStatus.Valid
                ? Color.FromArgb(255, 52, 199, 89)
                : Color.FromArgb(255, 142, 142, 147));
    }

    // ── Note Generation provider ──────────────────────────────────

    private void ConfigureNoteGeneration()
    {
        var isAnthropic = _settingsVm.SelectedNoteProvider == "Claude / Anthropic";
        ConfigureForAIProvider(isAnthropic, "笔记生成", "noteGeneration");
        ModelNameBox.Text = isAnthropic
            ? _settingsVm.AnthropicModelName : _settingsVm.OpenAiModelName;
    }

    // ── Chat provider ─────────────────────────────────────────────

    private void ConfigureChat()
    {
        var isAnthropic = _settingsVm.SelectedChatProvider == "Claude / Anthropic";
        ConfigureForAIProvider(isAnthropic, "AI 对话", "chat");
        ModelNameBox.Text = isAnthropic
            ? _settingsVm.AnthropicModelName : _settingsVm.OpenAiModelName;
    }

    private void ConfigureForAIProvider(bool isAnthropic, string kindLabel, string providerKind)
    {
        if (isAnthropic)
        {
            ProviderNameBlock.Text = "Claude / Anthropic";
            ProviderIdBlock.Text = "anthropicMessages";
            TypeBadgeText.Text = kindLabel;
            TypeBadge.Background = new SolidColorBrush(Color.FromArgb(255, 212, 111, 88));
            ProviderIconGlyph.Glyph = "";
            ProviderIconCircle.Background = new SolidColorBrush(Color.FromArgb(255, 212, 111, 88));

            BaseUrlBox.Text = _settingsVm.AnthropicBaseUrl;
            ModelNameBox.Text = _settingsVm.AnthropicModelName;
            ApiKeyBox.Password = ""; // never pre-fill key

            VersionRow.Visibility = Visibility.Visible;
            VersionBox.Text = _settingsVm.AnthropicVersion;
            PresetSelectorRow.Visibility = Visibility.Collapsed;
        }
        else
        {
            ProviderNameBlock.Text = "OpenAI-compatible";
            ProviderIdBlock.Text = "openAICompatible";
            TypeBadgeText.Text = kindLabel;
            TypeBadge.Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
            ProviderIconGlyph.Glyph = "";
            ProviderIconCircle.Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));

            BaseUrlBox.Text = _settingsVm.OpenAiBaseUrl;
            ModelNameBox.Text = _settingsVm.OpenAiModelName;
            ApiKeyBox.Password = "";

            VersionRow.Visibility = Visibility.Collapsed;
            PresetSelectorRow.Visibility = Visibility.Visible;

            // Populate preset combo
            PresetCombo.ItemsSource = Enum.GetValues<AIProviderPreset>();
            PresetCombo.SelectedItem = _settingsVm.SelectedProviderPreset;
        }

        // Load model list
        LoadModelCandidates(isAnthropic, providerKind);
        UpdateStatusPill("未配置", Color.FromArgb(255, 142, 142, 147));
    }

    // ── Model list loading ────────────────────────────────────────

    private async void LoadModelCandidates(bool isAnthropic, string providerKind)
    {
        ModelStatusBlock.Visibility = Visibility.Visible;
        ModelStatusBlock.Text = "加载模型列表中...";
        ModelStatusBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 142, 142, 147));
        RefreshModelsBtn.IsEnabled = false;

        try
        {
            var candidates = providerKind switch
            {
                "noteGeneration" => _settingsVm.NoteModelCandidates,
                "chat" => _settingsVm.ChatModelCandidates,
                _ => Array.Empty<AvailableModelInfo>()
            };

            if (candidates.Count == 0)
            {
                // Try fetching
                await _settingsVm.RefreshModelsCommand.ExecuteAsync(null);
                candidates = providerKind switch
                {
                    "noteGeneration" => _settingsVm.NoteModelCandidates,
                    "chat" => _settingsVm.ChatModelCandidates,
                    _ => Array.Empty<AvailableModelInfo>()
                };
            }

            if (candidates.Count > 0)
            {
                var displayItems = candidates.Select(m =>
                    !string.IsNullOrEmpty(m.OwnedBy)
                        ? $"{m.DisplayName}  ({m.OwnedBy})"
                        : m.DisplayName).ToList();
                ModelsListView.ItemsSource = displayItems;
                ModelCountBlock.Text = $"{candidates.Count} 个模型";
                ModelStatusBlock.Text = $"已加载 {candidates.Count} 个模型";
                ModelStatusBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 52, 199, 89));

                // Pre-select current model
                var currentModelName = isAnthropic
                    ? _settingsVm.AnthropicModelName : _settingsVm.OpenAiModelName;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].ModelId == currentModelName ||
                        candidates[i].DisplayName == currentModelName)
                    {
                        ModelsListView.SelectedIndex = i;
                        break;
                    }
                }

                // Store model metadata for later use
                ModelsListView.Tag = candidates;
            }
            else if (!string.IsNullOrEmpty(_settingsVm.ModelRefreshError))
            {
                ModelStatusBlock.Text = _settingsVm.ModelRefreshError;
                ModelStatusBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 69, 58));

                // Fallback list
                var fallback = isAnthropic
                    ? new[] { "claude-sonnet-4-6", "claude-haiku-4-5", "claude-opus-4-7" }
                    : new[] { "gpt-4o", "gpt-4o-mini", "qwen-plus", "deepseek-chat" };
                ModelsListView.ItemsSource = fallback;
                ModelCountBlock.Text = $"内置列表 ({fallback.Length} 个)";
            }
            else
            {
                ModelStatusBlock.Visibility = Visibility.Collapsed;
                var fallback = isAnthropic
                    ? new[] { "claude-sonnet-4-6", "claude-haiku-4-5", "claude-opus-4-7" }
                    : new[] { "gpt-4o", "gpt-4o-mini", "qwen-plus", "deepseek-chat" };
                ModelsListView.ItemsSource = fallback;
            }
        }
        catch
        {
            ModelStatusBlock.Text = "模型列表加载失败（网络不可用或 API 未配置）";
            ModelStatusBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 149, 0));
        }
        finally
        {
            RefreshModelsBtn.IsEnabled = true;
        }
    }

    private async void RefreshModels_Click(object sender, RoutedEventArgs e)
    {
        RefreshModelsBtn.IsEnabled = false;
        ModelStatusBlock.Visibility = Visibility.Visible;
        ModelStatusBlock.Text = "刷新中...";
        ModelStatusBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 142, 142, 147));

        try
        {
            await _settingsVm.RefreshModelsCommand.ExecuteAsync(null);

            var isAnthropic = _settingsVm.SelectedNoteProvider == "Claude / Anthropic";
            var kind = TypeBadgeText.Text.Contains("对话") ? "chat" : "noteGeneration";
            LoadModelCandidates(isAnthropic, kind);
        }
        catch
        {
            ModelStatusBlock.Text = "刷新失败";
            ModelStatusBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 69, 58));
        }
        finally
        {
            RefreshModelsBtn.IsEnabled = true;
        }
    }

    private void ModelSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModelsListView.SelectedItem is string selected && !string.IsNullOrWhiteSpace(selected))
        {
            // Extract just the model name (before any parenthetical owned_by)
            var modelName = selected.Split("  (")[0].Trim();
            ModelNameBox.Text = modelName;
        }
    }

    // ── Preset selection ──────────────────────────────────────────

    private void PresetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetCombo.SelectedItem is not AIProviderPreset preset) return;

        _settingsVm.SelectedProviderPreset = preset;
        switch (preset)
        {
            case AIProviderPreset.LmStudioLocal:
                BaseUrlBox.Text = "http://127.0.0.1:1234/v1";
                break;
            case AIProviderPreset.DeepSeek:
                BaseUrlBox.Text = "https://api.deepseek.com/v1";
                break;
            case AIProviderPreset.OpenAI:
                BaseUrlBox.Text = "https://api.openai.com/v1";
                break;
            case AIProviderPreset.Gemini:
                BaseUrlBox.Text = "https://generativelanguage.googleapis.com/v1beta";
                break;
        }
    }

    // ── Actions ────────────────────────────────────────────────────

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        TestConnectionBtn.IsEnabled = false;
        TestResultBlock.Visibility = Visibility.Visible;
        TestResultBlock.Text = "测试连接中...";
        TestResultBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 142, 142, 147));

        try
        {
            await _settingsVm.TestConnectionCommand.ExecuteAsync(null);
            var result = _settingsVm.LastConnectionTestResult;
            TestResultBlock.Text = $"连接测试: {result}";
            TestResultBlock.Foreground = result is not null && result.Contains("成功")
                ? new SolidColorBrush(Color.FromArgb(255, 52, 199, 89))
                : new SolidColorBrush(Color.FromArgb(255, 255, 149, 0));

            UpdateStatusPill(
                result is not null && result.Contains("成功") ? "已连接" : "连接失败",
                result is not null && result.Contains("成功")
                    ? Color.FromArgb(255, 52, 199, 89)
                    : Color.FromArgb(255, 255, 69, 58));
        }
        catch (Exception ex)
        {
            TestResultBlock.Text = $"连接测试失败: {ex.Message}";
            TestResultBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 69, 58));
            UpdateStatusPill("错误", Color.FromArgb(255, 255, 69, 58));
        }
        finally
        {
            TestConnectionBtn.IsEnabled = true;
        }
    }

    private void ValidateConfig_Click(object sender, RoutedEventArgs e)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(BaseUrlBox.Text))
            issues.Add("Base URL 未配置");
        if (string.IsNullOrWhiteSpace(ModelNameBox.Text))
            issues.Add("模型名称未配置");

        TestResultBlock.Visibility = Visibility.Visible;
        if (issues.Count == 0)
        {
            TestResultBlock.Text = "配置验证通过";
            TestResultBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 52, 199, 89));
            UpdateStatusPill("已配置", Color.FromArgb(255, 52, 199, 89));
        }
        else
        {
            TestResultBlock.Text = $"配置问题:\n{string.Join("\n", issues.Select(i => $"  • {i}"))}";
            TestResultBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 149, 0));
        }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        var isAnthropic = _settingsVm.SelectedNoteProvider == "Claude / Anthropic";

        if (isAnthropic)
        {
            _settingsVm.AnthropicBaseUrl = BaseUrlBox.Text.Trim();
            _settingsVm.AnthropicModelName = ModelNameBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(ApiKeyBox.Password))
                _settingsVm.AnthropicApiKey = ApiKeyBox.Password;
            if (VersionRow.Visibility == Visibility.Visible)
                _settingsVm.AnthropicVersion = VersionBox.Text.Trim();
        }
        else
        {
            _settingsVm.OpenAiBaseUrl = BaseUrlBox.Text.Trim();
            _settingsVm.OpenAiModelName = ModelNameBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(ApiKeyBox.Password))
                _settingsVm.OpenAiApiKey = ApiKeyBox.Password;
        }

        _settingsVm.SaveCommand.Execute(null);

        TestResultBlock.Visibility = Visibility.Visible;
        TestResultBlock.Text = "设置已保存";
        TestResultBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 52, 199, 89));

        ProviderSettingsChanged?.Invoke();
    }

    // ── Helpers ────────────────────────────────────────────────────

    private void UpdateStatusPill(string text, Color color)
    {
        StatusText.Text = text;
        StatusDot.Background = new SolidColorBrush(color);
        StatusPill.Background = new SolidColorBrush(Color.FromArgb(
            (byte)(color == Color.FromArgb(255, 52, 199, 89) ? 30 : 20),
            color.R, color.G, color.B));
    }
}
