using Flotis.Models;
using Flotis.Services;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Flotis.Interop;

namespace Flotis;

public sealed partial class VoiceSettingsDialog : ContentDialog
{
    private readonly TranscriptionProviderStore _providerStore = TranscriptionProviderStore.Shared;

    public VoiceSettingsDialog(AppState appState)
    {
        InitializeComponent();

        var config = _providerStore.LoadConfig();

        foreach (var item in LocaleCombo.Items)
        {
            if (item is ComboBoxItem locale && locale.Tag?.ToString() == appState.SelectedSpeechLocale)
            {
                LocaleCombo.SelectedItem = locale;
            }
        }

        foreach (var item in ModeCombo.Items)
        {
            if (item is ComboBoxItem mode && mode.Tag?.ToString() == appState.VoiceMode.ToString())
            {
                ModeCombo.SelectedItem = mode;
            }
        }

        ProviderNameBox.Text = config.Name;
        BaseUrlBox.Text = config.BaseURL;
        EndpointBox.Text = config.EndpointPath;
        ModelBox.Text = config.Model;
        LanguageBox.Text = config.Language ?? string.Empty;
        TemperatureBox.Text = config.Temperature?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

        PrimaryButtonClick += OnPrimaryButtonClick;
        Loaded += OnLoaded;
    }

    public VoiceInputMode SelectedMode { get; private set; }
    public string SelectedLocale { get; private set; } = "zh-CN";
    public TranscriptionProviderConfig ProviderConfig { get; private set; } = new();
    public string ApiKey { get; private set; } = string.Empty;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var loadedConfig = _providerStore.LoadConfig();
        ApiKey = SecureSecretStore.Load(loadedConfig.ApiKeyReference ?? "flotis.externalprovider.apikey") ?? string.Empty;
        ApiKeyBox.Password = ApiKey;

        RefreshExternalPanelVisibility();
    }

    private void OnModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshExternalPanelVisibility();
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (ModeCombo.SelectedItem is not ComboBoxItem modeItem || modeItem.Tag is not string modeTag)
        {
            args.Cancel = true;
            return;
        }

        SelectedMode = modeTag == nameof(VoiceInputMode.ExternalProvider)
            ? VoiceInputMode.ExternalProvider
            : VoiceInputMode.WindowsSpeech;

        RefreshExternalPanelVisibility();

        if (LocaleCombo.SelectedItem is ComboBoxItem localeItem && localeItem.Tag is string localeTag)
        {
            SelectedLocale = localeTag;
        }

        ProviderConfig = new TranscriptionProviderConfig
        {
            Name = ProviderNameBox.Text,
            BaseURL = BaseUrlBox.Text,
            EndpointPath = EndpointBox.Text,
            Model = ModelBox.Text,
            ApiKeyReference = "flotis.externalprovider.apikey",
            Language = string.IsNullOrWhiteSpace(LanguageBox.Text) ? null : LanguageBox.Text,
            Temperature = ParseTemperature()
        };
        ApiKey = ApiKeyBox.Password ?? string.Empty;
    }

    private void RefreshExternalPanelVisibility()
    {
        if (ModeCombo.SelectedItem is not ComboBoxItem modeItem || modeItem.Tag is not string modeTag)
        {
            ExternalPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }

        ExternalPanel.Visibility = modeTag == nameof(VoiceInputMode.ExternalProvider)
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private double? ParseTemperature()
    {
        if (double.TryParse(TemperatureBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }
        return null;
    }
}
