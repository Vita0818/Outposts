using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rokurics.Services;
using Rokurics.ViewModels;

namespace Rokurics.Views;

/// <summary>
/// Full-page provider detail view shown when clicking a provider row in Settings.
/// Embeds ProviderDetailCard with the appropriate kind and provides endpoint/security info.
/// Mirrors drill-down detail sheets from Apple source MacSettingsView.
/// </summary>
public sealed partial class MacProviderDetailPage : Page
{
    private readonly SettingsViewModel _settingsVm;

    /// <summary>
    /// Fires when the user navigates back to settings.
    /// </summary>
    public event Action? NavigateBack;

    public MacProviderDetailPage()
    {
        InitializeComponent();
        _settingsVm = App.Current.Services.GetService<SettingsViewModel>()
            ?? new SettingsViewModel();

        Loaded += (_, _) => DetailCard.ProviderSettingsChanged += RefreshEndpointInfo;
        DetailCard.ProviderSettingsChanged += RefreshEndpointInfo;
    }

    /// <summary>
    /// Configure this page for a specific provider kind.
    /// </summary>
    public void ConfigureFor(ProviderDetailCard.ProviderCardKind kind, string? providerName = null)
    {
        DetailCard.ConfigureFor(kind);

        var (title, subtitle) = kind switch
        {
            ProviderDetailCard.ProviderCardKind.Transcription
                => ("转写 Provider", "Whisper.cpp 本地转写配置"),
            ProviderDetailCard.ProviderCardKind.NoteGeneration
                => ("笔记生成 Provider", $"{providerName ?? "AI"} 配置"),
            ProviderDetailCard.ProviderCardKind.Chat
                => ("AI 对话 Provider", $"{providerName ?? "AI"} 配置"),
            _ => ("Provider 详情", "查看和配置 Provider 连接参数")
        };

        PageTitle.Text = title;
        PageSubtitle.Text = subtitle;

        RefreshEndpointInfo();
    }

    private void RefreshEndpointInfo()
    {
        var isAnthropic = _settingsVm.SelectedNoteProvider == "Claude / Anthropic";

        EndpointUrlBlock.Text = isAnthropic
            ? _settingsVm.AnthropicBaseUrl
            : _settingsVm.OpenAiBaseUrl;

        AuthMethodBlock.Text = isAnthropic
            ? "x-api-key Header (Anthropic)"
            : "Bearer Token Authorization Header";

        KeyStorageBlock.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rokurics", "settings.json");

        LastValidatedBlock.Text = string.IsNullOrEmpty(_settingsVm.LastConnectionTestResult)
            ? "尚未验证"
            : _settingsVm.LastConnectionTestResult;
    }

    private void GoBack_Click(object sender, RoutedEventArgs e)
    {
        NavigateBack?.Invoke();
    }
}
