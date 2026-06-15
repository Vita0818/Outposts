using Flotis.Interop;
using Flotis.Services;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using Flotis.Models;

namespace Flotis;

public sealed partial class MainWindow : Window
{
    private readonly AppState _state = new();
    private readonly ClipboardPasteService _clipboardService = new();
    private readonly VoiceInputController _voiceController;
    private readonly TranscriptionProviderStore _providerStore = TranscriptionProviderStore.Shared;
    private HotkeyManager? _hotkeyManager;
    private readonly DispatcherTimer _permissionTimer = new();
    private bool _isLoaded;

    public MainWindow()
    {
        InitializeComponent();

        _state.VoiceMode = VoiceInputMode.WindowsSpeech;
        _state.SelectedSpeechLocale = "zh-CN";
        _state.IsPanelVisible = true;
        _state.HasAccessibilityPermission = ClipboardPasteService.CheckPasteCapability();

        _voiceController = new VoiceInputController(_state, _clipboardService, DispatcherQueue.GetForCurrentThread(), RefreshUi);

        Width = 420;
        Height = 320;
        Activate();
        SetTopmostWindow();
        Loaded += OnLoaded;
        Closed += OnClosed;

        RefreshUi();
        BuildCommandButtons();
        BuildModeCombo();
        RefreshModeCombo();
        RefreshLocaleCombo();
        StartPermissionTimer();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        var hwnd = WindowNative.GetWindowHandle(this);
        SetTopmostWindow(hwnd);
        InitializeHotkeys(hwnd);
        RefreshAccessibilityState();
    }

    private void OnClosed(object? sender, WindowEventArgs args)
    {
        _permissionTimer.Stop();
        _hotkeyManager?.Dispose();
        _voiceController.Cancel();
    }

    private void StartPermissionTimer()
    {
        _permissionTimer.Interval = TimeSpan.FromSeconds(1);
        _permissionTimer.Tick += (_, _) => RefreshAccessibilityState();
        _permissionTimer.Start();
    }

    private void RefreshAccessibilityState()
    {
        _state.HasAccessibilityPermission = ClipboardPasteService.CheckPasteCapability();
        RefreshUi();
    }

    private void BuildCommandButtons()
    {
        CommandGrid.Children.Clear();
        CommandGrid.RowDefinitions.Clear();
        CommandGrid.ColumnDefinitions.Clear();

        CommandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        CommandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var commands = CommandStore.DefaultCommands;
        int rows = (int)Math.Ceiling(commands.Count / 2.0);
        for (int i = 0; i < rows; i++)
        {
            CommandGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (int i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            int row = i / 2;
            int column = i % 2;

            var button = new Button
            {
                Content = $"{command.Title} Ctrl+Shift+{command.ShortcutIndex ?? 0}",
                FontSize = 12,
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            button.Click += (_, _) => _ = InjectCommandAsync(command);
            Grid.SetRow(button, row);
            Grid.SetColumn(button, column);
            CommandGrid.Children.Add(button);
        }
    }

    private void BuildModeCombo()
    {
        ModeCombo.Items.Clear();
        ModeCombo.Items.Add(new ComboBoxItem
        {
            Content = "Windows Speech",
            Tag = nameof(VoiceInputMode.WindowsSpeech),
            IsSelected = true
        });
        ModeCombo.Items.Add(new ComboBoxItem
        {
            Content = "External Provider",
            Tag = nameof(VoiceInputMode.ExternalProvider)
        });
    }

    private async void OnVoiceButtonClick(object sender, RoutedEventArgs e)
    {
        _voiceController.ToggleRecording();
        await Task.CompletedTask;
    }

    private async void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var dialog = new VoiceSettingsDialog(_state);
        dialog.XamlRoot = Content.XamlRoot;
        await OpenSettingsAsync(dialog);
    }

    private async Task OpenSettingsAsync(VoiceSettingsDialog dialog)
    {
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _state.VoiceMode = dialog.SelectedMode;
            _state.SelectedSpeechLocale = dialog.SelectedLocale;
            _providerStore.SaveConfig(dialog.ProviderConfig);

            if (!string.IsNullOrWhiteSpace(dialog.ApiKey))
            {
                SecureSecretStore.Save("flotis.externalprovider.apikey", dialog.ApiKey);
            }
            else
            {
                SecureSecretStore.Delete("flotis.externalprovider.apikey");
            }

            RefreshModeCombo();
            RefreshLocaleCombo();
            RefreshUi();
        }
    }

    private void OnModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModeCombo.SelectedItem is not ComboBoxItem selected) return;
        _state.VoiceMode = selected.Tag?.ToString() == nameof(VoiceInputMode.ExternalProvider)
            ? VoiceInputMode.ExternalProvider
            : VoiceInputMode.WindowsSpeech;
        RefreshUi();
    }

    private void OnLocaleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LocaleCombo.SelectedItem is not ComboBoxItem selected) return;
        if (selected.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
        {
            _state.SelectedSpeechLocale = tag;
        }
    }

    private async Task InjectCommandAsync(PromptCommand command)
    {
        var success = await _clipboardService.InjectAsync(command.Content);
        if (!success)
        {
            _state.PasteError = "粘贴失败，可能没有权限";
            _state.VoiceState = new();
            _state.VoiceState.Set(VoiceInputStateKind.Failed, "注入失败");
            RefreshUi();
            await Task.Delay(2000);
            _state.PasteError = null;
            if (_state.VoiceState.Kind == VoiceInputStateKind.Failed)
            {
                _state.VoiceState = VoiceInputState.Idle;
            }
            RefreshUi();
        }
        else
        {
            _state.PasteError = null;
            _state.HasAccessibilityPermission = ClipboardPasteService.CheckPasteCapability();
            RefreshUi();
        }
    }

    private void InitializeHotkeys(IntPtr hwnd)
    {
        _hotkeyManager?.Dispose();
        _hotkeyManager = new HotkeyManager(hwnd);
        _hotkeyManager.OnTogglePanel = TogglePanelVisibility;
        _hotkeyManager.OnToggleVoice = () => _voiceController.ToggleRecording();
        _hotkeyManager.OnCommandShortcut = InjectCommandByShortcut;
        _hotkeyManager.Start();
    }

    private void InjectCommandByShortcut(int shortcutIndex)
    {
        var command = CommandStore.DefaultCommands.FirstOrDefault(c => c.ShortcutIndex == shortcutIndex);
        if (command is not null)
        {
            _ = InjectCommandAsync(command);
        }
    }

    private void TogglePanelVisibility()
    {
        _state.IsPanelVisible = !_state.IsPanelVisible;
        if (_state.IsPanelVisible)
        {
            Activate();
            SetTopmostWindow(WindowNative.GetWindowHandle(this));
        }
        else
        {
            Hide();
        }
        RefreshUi();
    }

    private void SelectLocaleItem(string locale)
    {
        for (int i = 0; i < LocaleCombo.Items.Count; i++)
        {
            if (LocaleCombo.Items[i] is ComboBoxItem item && item.Tag?.ToString() == locale)
            {
                LocaleCombo.SelectedIndex = i;
                break;
            }
        }
    }

    public void RefreshUi()
    {
        if (_state.PasteError is not null)
        {
            StatusText.Text = _state.PasteError;
            StatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.IndianRed);
            StatusPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            OpenSettingsButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
        else if (!_state.HasAccessibilityPermission)
        {
            StatusText.Text = "请确认应用可访问剪贴板/麦克风权限。";
            StatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Goldenrod);
            StatusPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            OpenSettingsButton.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
        else
        {
            StatusText.Text = "";
            StatusPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            OpenSettingsButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        VoiceButton.Content = GetVoiceButtonText(_state.VoiceState);
        if (_state.VoiceState.Kind == VoiceInputStateKind.Recording)
        {
            VoiceButton.Background = new SolidColorBrush(Microsoft.UI.Colors.IndianRed);
        }
        else if (_state.VoiceState.Kind == VoiceInputStateKind.Failed)
        {
            VoiceButton.Background = new SolidColorBrush(Microsoft.UI.Colors.Orange);
        }
        else
        {
            VoiceButton.Background = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
        }

        TranscriptText.Text = string.IsNullOrWhiteSpace(_state.TranscriptPreview)
            ? "转写预览文本……"
            : _state.TranscriptPreview;

        LocaleCombo.IsEnabled = _state.VoiceMode != VoiceInputMode.ExternalProvider;
    }

    private void RefreshModeCombo()
    {
        foreach (var item in ModeCombo.Items)
        {
            if (item is ComboBoxItem combo && combo.Tag?.ToString() == _state.VoiceMode.ToString())
            {
                ModeCombo.SelectedItem = combo;
                return;
            }
        }
    }

    private void RefreshLocaleCombo()
    {
        SelectLocaleItem(_state.SelectedSpeechLocale);
    }

    private static string GetVoiceButtonText(VoiceInputStateKind kind)
    {
        return kind switch
        {
            VoiceInputStateKind.RequestingPermission => "请求中",
            VoiceInputStateKind.Recording => "停止",
            VoiceInputStateKind.Transcribing => "转写中",
            VoiceInputStateKind.Injecting => "注入中",
            VoiceInputStateKind.Failed => "重试",
            _ => "开始"
        };
    }

    private static string GetVoiceButtonText(VoiceInputState state)
    {
        return GetVoiceButtonText(state.Kind);
    }

    private static void SetTopmostWindow(IntPtr hwnd)
    {
        NativeWindowExtensions.SetWindowTopmost(hwnd);
    }

    private void SetTopmostWindow()
    {
        SetTopmostWindow(WindowNative.GetWindowHandle(this));
    }

    private async void OnOpenSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-microphone"));
        }
        catch
        {
            _state.PasteError = "无法打开系统设置。";
            RefreshUi();
        }
    }
}
