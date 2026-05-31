using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Rokurics.ViewModels;

namespace Rokurics.Views;

public sealed partial class MacSettingsPage : Page
{
    private readonly SettingsViewModel _viewModel;

    public string UserDisplayName => _viewModel.UserDisplayName;
    public string UserInitials =>
        (_viewModel.UserDisplayName?.FirstOrDefault() ?? 'U').ToString().ToUpper();
    public string UserHandle =>
        $"@{_viewModel.UserDisplayName?.ToLowerInvariant() ?? "user"}";

    public string TranscriptionProvider => _viewModel.SelectedTranscriptionProvider;
    public string NoteProvider => _viewModel.SelectedNoteProvider;
    public string ChatProvider => _viewModel.SelectedChatProvider;
    public string WhisperModelName => _viewModel.WhisperModelName;
    public string AIModelName =>
        _viewModel.SelectedNoteProvider == "Claude / Anthropic"
            ? _viewModel.AnthropicModelName
            : _viewModel.OpenAiModelName;

    public MacSettingsPage()
    {
        _viewModel = App.Current.Services.GetService<SettingsViewModel>()
            ?? new SettingsViewModel();
        InitializeComponent();
        RefreshSettingsRows();
    }

    /// <summary>
    /// Rebuilds all settings row collections from current ViewModel state.
    /// Called on page load and after any dialog that modifies settings.
    /// </summary>
    private void RefreshSettingsRows()
    {
        TranscriptionRows.ItemsSource = new[]
        {
            new SettingsRowItem("Provider", _viewModel.SelectedTranscriptionProvider, "transcriptionProvider"),
            new SettingsRowItem("模型", _viewModel.WhisperModelName, "transcriptionModel"),
            new SettingsRowItem("授权与测试",
                _viewModel.WhisperCliStatus == WhisperResourceStatus.Valid ? "已配置" : "未配置",
                "transcriptionAuthTest", false),
        };

        AIRows.ItemsSource = new[]
        {
            new SettingsRowItem("Provider", _viewModel.SelectedNoteProvider, "aiProvider"),
            new SettingsRowItem("模型", AIModelName, "aiModel"),
            new SettingsRowItem("API 设置", "查看", "aiApiSettings"),
            new SettingsRowItem("测试", "查看", "aiTest", false),
        };

        AboutRows.ItemsSource = new[]
        {
            new SettingsRowItem("存储", "打开", "openStorage"),
            new SettingsRowItem("隐私政策", "查看", "showPrivacyPolicy"),
            new SettingsRowItem("版权", "1.0 (1)", "showCopyright", false),
        };
    }

    /// <summary>
    /// Dispatches DataTemplate button clicks to the appropriate handler based on ActionTag.
    /// </summary>
    private void SettingsRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;

        switch (tag)
        {
            case "transcriptionProvider": TranscriptionProvider_Click(sender, e); break;
            case "transcriptionModel": TranscriptionModel_Click(sender, e); break;
            case "transcriptionAuthTest": TranscriptionAuthTest_Click(sender, e); break;
            case "aiProvider": AIProvider_Click(sender, e); break;
            case "aiModel": AIModel_Click(sender, e); break;
            case "aiApiSettings": AIApiSettings_Click(sender, e); break;
            case "aiTest": AITest_Click(sender, e); break;
            case "openStorage": OpenStorage_Click(sender, e); break;
            case "showPrivacyPolicy": ShowPrivacyPolicy_Click(sender, e); break;
            case "showCopyright": ShowCopyright_Click(sender, e); break;
        }
    }

    // ── Profile ─────────────────────────────────────────────────────

    private async void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        var panel = new StackPanel { Spacing = 14, Width = 420 };

        var nameBox = CreateLabeledField("显示名称", _viewModel.UserDisplayName);
        var handleBox = CreateLabeledField("用户 ID", _viewModel.UserHandle);

        panel.Children.Add(nameBox.stack);
        panel.Children.Add(handleBox.stack);

        var dialog = new ContentDialog
        {
            Title = "编辑个人资料",
            Content = panel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            _viewModel.UserDisplayName = nameBox.input.Text.Trim().Length > 0
                ? nameBox.input.Text.Trim() : _viewModel.UserDisplayName;
            _viewModel.UserHandle = handleBox.input.Text.Trim().Length > 0
                ? handleBox.input.Text.Trim() : _viewModel.UserHandle;
            Bindings.Update();
        }
    }

    // ── Transcription Provider ──────────────────────────────────────

    private async void TranscriptionProvider_Click(object sender, RoutedEventArgs e)
    {
        var providers = new[] { "Whisper.cpp (本地)", "Mock" };
        var current = _viewModel.SelectedTranscriptionProvider;

        var panel = new StackPanel { Spacing = 10, Width = 380 };
        var listView = new ListView
        {
            ItemsSource = providers,
            MaxHeight = 200
        };
        panel.Children.Add(listView);

        var dialog = new ContentDialog
        {
            Title = "转写 Provider",
            Content = panel,
            PrimaryButtonText = "选择",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary && listView.SelectedItem is string selected)
        {
            _viewModel.SelectedTranscriptionProvider = selected;
            RefreshSettingsRows();
            Bindings.Update();
        }
    }

    // ── Transcription Model ─────────────────────────────────────────

    private async void TranscriptionModel_Click(object sender, RoutedEventArgs e)
    {
        var models = new[] { "ggml-large-v3-turbo", "ggml-medium", "ggml-small", "ggml-tiny" };
        var panel = new StackPanel { Spacing = 10, Width = 380 };
        var listView = new ListView { ItemsSource = models, MaxHeight = 240 };
        panel.Children.Add(listView);

        var dialog = new ContentDialog
        {
            Title = "转写模型",
            Content = panel,
            PrimaryButtonText = "选择",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary && listView.SelectedItem is string selected)
        {
            _viewModel.WhisperModelName = selected;
            RefreshSettingsRows();
            Bindings.Update();
        }
    }

    // ── Transcription Auth & Test ───────────────────────────────────

    private async void TranscriptionAuthTest_Click(object sender, RoutedEventArgs e)
    {
        var panel = new StackPanel { Spacing = 12, Width = 480 };

        // ── Model group ──
        panel.Children.Add(new TextBlock
        {
            Text = "模型配置",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"当前模型: {_viewModel.WhisperModelName}",
            FontSize = 13,
            Opacity = 0.7
        });

        // Default language picker
        var langPanel = new StackPanel { Spacing = 4 };
        langPanel.Children.Add(new TextBlock
        {
            Text = "默认语言",
            FontSize = 12,
            Opacity = 0.5
        });
        var langCombo = new ComboBox
        {
            ItemsSource = new[] { "auto", "zh", "en" },
            Width = 160
        };
        langCombo.SelectedItem = _viewModel.WhisperDefaultLanguage;
        langCombo.SelectionChanged += (_, _) =>
        {
            if (langCombo.SelectedItem is string lang)
                _viewModel.WhisperDefaultLanguage = lang;
        };
        langPanel.Children.Add(langCombo);
        panel.Children.Add(langPanel);

        // JSON Segments toggle
        var segmentToggle = new ToggleSwitch
        {
            Header = "JSON Segments 输出",
            IsOn = _viewModel.WhisperPreferSegmentOutput,
            FontSize = 13
        };
        segmentToggle.Toggled += (_, _) =>
            _viewModel.WhisperPreferSegmentOutput = segmentToggle.IsOn;
        panel.Children.Add(segmentToggle);

        // ── Authorization group ──
        panel.Children.Add(new TextBlock
        {
            Text = "授权状态",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Margin = new Thickness(0, 6, 0, 0)
        });

        // whisper-cli path
        var cliRow = CreateAuthPathRow("Whisper CLI 路径", _viewModel.WhisperExecutablePath,
            StatusDisplayText(_viewModel.WhisperCliStatus), StatusColor(_viewModel.WhisperCliStatus));
        cliRow.input.TextChanged += (_, _) =>
        {
            _viewModel.WhisperExecutablePath = cliRow.input.Text;
            _viewModel.WhisperCliStatus = string.IsNullOrWhiteSpace(cliRow.input.Text)
                ? WhisperResourceStatus.NotConfigured
                : WhisperResourceStatus.Valid;
            cliRow.status.Text = StatusDisplayText(_viewModel.WhisperCliStatus);
            cliRow.status.Foreground = StatusColor(_viewModel.WhisperCliStatus);
        };
        panel.Children.Add(cliRow.stack);

        // model file path
        var modelRow = CreateAuthPathRow("模型文件路径", _viewModel.WhisperModelPath,
            StatusDisplayText(_viewModel.WhisperModelStatus), StatusColor(_viewModel.WhisperModelStatus));
        modelRow.input.TextChanged += (_, _) =>
        {
            _viewModel.WhisperModelPath = modelRow.input.Text;
            _viewModel.WhisperModelStatus = string.IsNullOrWhiteSpace(modelRow.input.Text)
                ? WhisperResourceStatus.NotConfigured
                : WhisperResourceStatus.Valid;
            modelRow.status.Text = StatusDisplayText(_viewModel.WhisperModelStatus);
            modelRow.status.Foreground = StatusColor(_viewModel.WhisperModelStatus);
        };
        panel.Children.Add(modelRow.stack);

        // whisper.cpp root directory
        var rootRow = CreateAuthPathRow("Whisper.cpp 根目录", _viewModel.WhisperCppRootDirectory,
            StatusDisplayText(_viewModel.WhisperRootDirStatus), StatusColor(_viewModel.WhisperRootDirStatus));
        rootRow.input.TextChanged += (_, _) =>
        {
            _viewModel.WhisperCppRootDirectory = rootRow.input.Text;
            _viewModel.WhisperRootDirStatus = string.IsNullOrWhiteSpace(rootRow.input.Text)
                ? WhisperResourceStatus.NotConfigured
                : WhisperResourceStatus.Valid;
            rootRow.status.Text = StatusDisplayText(_viewModel.WhisperRootDirStatus);
            rootRow.status.Foreground = StatusColor(_viewModel.WhisperRootDirStatus);
        };
        panel.Children.Add(rootRow.stack);

        // ffmpeg fallback path
        var ffmpegRow = CreateAuthPathRow("FFmpeg 路径", _viewModel.FfmpegExecutablePath,
            StatusDisplayText(_viewModel.FfmpegStatus), StatusColor(_viewModel.FfmpegStatus));
        ffmpegRow.input.TextChanged += (_, _) =>
        {
            _viewModel.FfmpegExecutablePath = ffmpegRow.input.Text;
            _viewModel.FfmpegStatus = string.IsNullOrWhiteSpace(ffmpegRow.input.Text)
                ? WhisperResourceStatus.NotConfigured
                : WhisperResourceStatus.Valid;
            ffmpegRow.status.Text = StatusDisplayText(_viewModel.FfmpegStatus);
            ffmpegRow.status.Foreground = StatusColor(_viewModel.FfmpegStatus);
        };
        panel.Children.Add(ffmpegRow.stack);

        // ── Operations group ──
        panel.Children.Add(new TextBlock
        {
            Text = "操作",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Margin = new Thickness(0, 6, 0, 0)
        });

        var validationResult = new TextBlock
        {
            Text = _viewModel.LastTranscriptionValidation,
            FontSize = 13,
            Opacity = 0.7
        };
        panel.Children.Add(validationResult);

        var actionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var validateBtn = new Button { Content = "检查配置", FontSize = 12 };
        validateBtn.Click += (_, _) =>
        {
            _viewModel.LastTranscriptionValidation = "配置检查通过 (mock)";
            validationResult.Text = _viewModel.LastTranscriptionValidation;
        };
        actionRow.Children.Add(validateBtn);

        var testLaunchBtn = new Button { Content = "测试启动", FontSize = 12 };
        testLaunchBtn.Click += (_, _) =>
        {
            _viewModel.LastTranscriptionValidation = "测试启动通过 (mock)";
            validationResult.Text = _viewModel.LastTranscriptionValidation;
        };
        actionRow.Children.Add(testLaunchBtn);

        var diagnosticsBtn = new Button { Content = "文件诊断", FontSize = 12 };
        diagnosticsBtn.Click += (_, _) =>
        {
            var diag = new List<string>();
            diag.Add($"CLI: {StatusDisplayText(_viewModel.WhisperCliStatus)}");
            diag.Add($"模型: {StatusDisplayText(_viewModel.WhisperModelStatus)}");
            diag.Add($"根目录: {StatusDisplayText(_viewModel.WhisperRootDirStatus)}");
            diag.Add($"FFmpeg: {StatusDisplayText(_viewModel.FfmpegStatus)}");
            _viewModel.LastTranscriptionValidation = string.Join(" | ", diag);
            validationResult.Text = _viewModel.LastTranscriptionValidation;
        };
        actionRow.Children.Add(diagnosticsBtn);
        panel.Children.Add(actionRow);

        var saveBtn = new Button
        {
            Content = "保存设置",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        saveBtn.Click += (_, _) => _viewModel.SaveCommand.Execute(null);
        panel.Children.Add(saveBtn);

        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 520,
            Content = panel
        };

        await new ContentDialog
        {
            Title = "授权与测试 — Whisper.cpp",
            Content = scrollViewer,
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot
        }.ShowAsync();

        RefreshSettingsRows();
    }

    private static string StatusDisplayText(WhisperResourceStatus s) => s switch
    {
        WhisperResourceStatus.Valid => "已授权",
        WhisperResourceStatus.NotFound => "未找到",
        WhisperResourceStatus.AccessDenied => "无访问权限",
        WhisperResourceStatus.NotExecutable => "不可执行",
        _ => "未配置"
    };

    private static SolidColorBrush StatusColor(WhisperResourceStatus s) => s switch
    {
        WhisperResourceStatus.Valid => new SolidColorBrush(Microsoft.UI.Colors.LimeGreen),
        WhisperResourceStatus.NotFound => new SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
        WhisperResourceStatus.AccessDenied => new SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
        WhisperResourceStatus.NotExecutable => new SolidColorBrush(Microsoft.UI.Colors.Orange),
        _ => new SolidColorBrush(Microsoft.UI.Colors.Gray)
    };

    private static (StackPanel stack, TextBox input, TextBlock status) CreateAuthPathRow(
        string label, string value, string statusText, SolidColorBrush statusColor)
    {
        var stack = new StackPanel { Spacing = 4 };
        var labelBlock = new TextBlock
        {
            Text = label,
            FontSize = 12,
            Opacity = 0.5
        };
        var input = new TextBox
        {
            Text = value,
            PlaceholderText = "未配置",
            FontSize = 12,
            Width = 440
        };
        var status = new TextBlock
        {
            Text = statusText,
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = statusColor
        };
        stack.Children.Add(labelBlock);
        stack.Children.Add(input);
        stack.Children.Add(status);
        return (stack, input, status);
    }

    // ── AI Provider ─────────────────────────────────────────────────

    private async void AIProvider_Click(object sender, RoutedEventArgs e)
    {
        var card = new ProviderDetailCard();
        card.ConfigureFor(ProviderDetailCard.ProviderCardKind.NoteGeneration);
        card.Width = 600;

        var providerPicker = new ComboBox
        {
            ItemsSource = new[] { "OpenAI-compatible", "Claude / Anthropic", "Mock" },
            SelectedItem = _viewModel.SelectedNoteProvider,
            Width = 300,
            Margin = new Thickness(0, 0, 0, 14)
        };
        providerPicker.SelectionChanged += async (_, _) =>
        {
            if (providerPicker.SelectedItem is string selected && selected != _viewModel.SelectedNoteProvider)
            {
                _viewModel.SelectedNoteProvider = selected;
                card.ConfigureFor(ProviderCardKindForProvider(selected));
                Bindings.Update();
            }
        };

        var panel = new StackPanel { Spacing = 0 };
        panel.Children.Add(new TextBlock
        {
            Text = "选择 Provider 类型",
            FontSize = 12,
            Opacity = 0.5,
            Margin = new Thickness(0, 0, 0, 6)
        });
        panel.Children.Add(providerPicker);
        panel.Children.Add(card);

        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 580,
            Content = panel
        };

        await new ContentDialog
        {
            Title = "AI Provider",
            Content = scrollViewer,
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot
        }.ShowAsync();

        RefreshSettingsRows();
        Bindings.Update();
    }

    private static ProviderDetailCard.ProviderCardKind ProviderCardKindForProvider(string provider)
        => provider switch
        {
            "Claude / Anthropic" => ProviderDetailCard.ProviderCardKind.NoteGeneration,
            "OpenAI-compatible" => ProviderDetailCard.ProviderCardKind.NoteGeneration,
            _ => ProviderDetailCard.ProviderCardKind.NoteGeneration
        };

    // ── AI Model ────────────────────────────────────────────────────

    private async void AIModel_Click(object sender, RoutedEventArgs e)
    {
        var isAnthropic = _viewModel.SelectedNoteProvider == "Claude / Anthropic";
        var panel = new StackPanel { Spacing = 12, Width = 480 };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        headerRow.Children.Add(new TextBlock
        {
            Text = $"Provider: {_viewModel.SelectedNoteProvider}",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        var hasApiKey = isAnthropic
            ? !string.IsNullOrWhiteSpace(_viewModel.AnthropicApiKey)
            : !string.IsNullOrWhiteSpace(_viewModel.OpenAiApiKey);
        var statusDot = new Border
        {
            Width = 10, Height = 10,
            CornerRadius = new CornerRadius(5),
            Background = new SolidColorBrush(hasApiKey ? Microsoft.UI.Colors.LimeGreen : Microsoft.UI.Colors.Gray),
            VerticalAlignment = VerticalAlignment.Center
        };
        headerRow.Children.Add(statusDot);
        headerRow.Children.Add(new TextBlock
        {
            Text = hasApiKey ? "API Key 已配置" : "API Key 未配置",
            FontSize = 11,
            Opacity = 0.5,
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(headerRow);

        var currentModelName = isAnthropic ? _viewModel.AnthropicModelName : _viewModel.OpenAiModelName;
        panel.Children.Add(new TextBlock
        {
            Text = $"当前模型: {currentModelName}",
            FontSize = 12,
            Opacity = 0.5
        });

        var statusBlock = new TextBlock
        {
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7
        };
        panel.Children.Add(statusBlock);

        var listView = new ListView { MaxHeight = 240 };
        panel.Children.Add(listView);

        var fetchedModels = _viewModel.NoteModelCandidates;
        if (fetchedModels.Count > 0)
        {
            var displayItems = fetchedModels.Select(m =>
            {
                var createdStr = m.CreatedAt.HasValue
                    ? m.CreatedAt.Value.ToString("yyyy-MM-dd")
                    : "";
                var ownedByStr = !string.IsNullOrEmpty(m.OwnedBy) ? $" | {m.OwnedBy}" : "";
                return $"{m.DisplayName}{ownedByStr}  {createdStr}";
            }).ToList();
            listView.ItemsSource = displayItems;
            statusBlock.Text = $"已加载 {fetchedModels.Count} 个模型 (从服务器获取)";
            statusBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);

            for (int i = 0; i < fetchedModels.Count; i++)
            {
                if (fetchedModels[i].ModelId == currentModelName ||
                    fetchedModels[i].DisplayName == currentModelName)
                {
                    listView.SelectedIndex = i;
                    break;
                }
            }
        }
        else
        {
            var fallback = isAnthropic
                ? new[] { "claude-sonnet-4-6", "claude-haiku-4-5", "claude-opus-4-7" }
                : new[] { "gpt-4o", "gpt-4o-mini", "qwen-plus", "deepseek-chat" };
            listView.ItemsSource = fallback;
            statusBlock.Text = "使用内置模型列表（点击刷新获取最新模型）";
        }

        if (!string.IsNullOrEmpty(_viewModel.ModelRefreshError))
        {
            statusBlock.Text = _viewModel.ModelRefreshError;
            statusBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
        }

        var refreshBtn = new Button
        {
            Content = _viewModel.IsRefreshingModels ? "刷新中..." : "刷新模型列表",
            FontSize = 12,
            IsEnabled = !_viewModel.IsRefreshingModels,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        refreshBtn.Click += async (_, _) =>
        {
            refreshBtn.IsEnabled = false;
            refreshBtn.Content = "刷新中...";
            statusBlock.Text = "正在从服务器获取模型列表...";
            statusBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            await _viewModel.RefreshModelsCommand.ExecuteAsync(null);
            var updated = _viewModel.NoteModelCandidates;
            if (updated.Count > 0)
            {
                var displayItems = updated.Select(m =>
                {
                    var createdStr = m.CreatedAt.HasValue
                        ? m.CreatedAt.Value.ToString("yyyy-MM-dd")
                        : "";
                    var ownedByStr = !string.IsNullOrEmpty(m.OwnedBy) ? $" | {m.OwnedBy}" : "";
                    return $"{m.DisplayName}{ownedByStr}  {createdStr}";
                }).ToList();
                listView.ItemsSource = displayItems;
                statusBlock.Text = $"已加载 {updated.Count} 个模型";
                statusBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
            }
            else if (!string.IsNullOrEmpty(_viewModel.ModelRefreshError))
            {
                statusBlock.Text = _viewModel.ModelRefreshError;
                statusBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
            }
            refreshBtn.IsEnabled = true;
            refreshBtn.Content = "刷新模型列表";
        };
        panel.Children.Add(refreshBtn);

        panel.Children.Add(new TextBlock
        {
            Text = "或输入自定义模型名称",
            FontSize = 11,
            Opacity = 0.5,
            Margin = new Thickness(0, 4, 0, 0)
        });
        var customModelBox = new TextBox
        {
            PlaceholderText = isAnthropic ? "claude-..." : "model-name",
            Width = 440,
            Text = currentModelName
        };
        panel.Children.Add(customModelBox);

        var metadataBlock = new TextBlock
        {
            FontSize = 11,
            Opacity = 0.4,
            TextWrapping = TextWrapping.Wrap
        };
        listView.SelectionChanged += (_, _) =>
        {
            if (listView.SelectedIndex >= 0 && listView.SelectedIndex < fetchedModels.Count)
            {
                var m = fetchedModels[listView.SelectedIndex];
                var info = new List<string>();
                if (!string.IsNullOrEmpty(m.OwnedBy)) info.Add($"Owned by: {m.OwnedBy}");
                if (m.CreatedAt.HasValue) info.Add($"Created: {m.CreatedAt:yyyy-MM-dd}");
                metadataBlock.Text = string.Join("  |  ", info);
            }
            else
            {
                metadataBlock.Text = "";
            }
        };
        panel.Children.Add(metadataBlock);

        var dialog = new ContentDialog
        {
            Title = "AI 模型",
            Content = panel,
            PrimaryButtonText = "选择",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var selected = listView.SelectedItem as string
                ?? customModelBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                var modelName = selected.Contains(" | ")
                    ? selected.Split(" | ")[0].Trim()
                    : selected.Split("  ")[0].Trim();

                if (isAnthropic)
                    _viewModel.AnthropicModelName = modelName;
                else
                    _viewModel.OpenAiModelName = modelName;
                _viewModel.SaveCommand.Execute(null);
                RefreshSettingsRows();
                Bindings.Update();
            }
        }
    }

    // ── API Settings ────────────────────────────────────────────────

    private async void AIApiSettings_Click(object sender, RoutedEventArgs e)
    {
        var isAnthropic = _viewModel.SelectedNoteProvider == "Claude / Anthropic";
        var panel = new StackPanel { Spacing = 14, Width = 460 };

        panel.Children.Add(new TextBlock
        {
            Text = $"Provider: {_viewModel.SelectedNoteProvider}",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        });

        if (!isAnthropic)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Provider Preset",
                FontSize = 12,
                Opacity = 0.5
            });
            var presetCombo = new ComboBox
            {
                ItemsSource = Enum.GetValues<AIProviderPreset>(),
                SelectedItem = _viewModel.SelectedProviderPreset,
                Width = 280
            };
            presetCombo.SelectionChanged += (_, _) =>
            {
                if (presetCombo.SelectedItem is AIProviderPreset preset)
                {
                    _viewModel.SelectedProviderPreset = preset;
                    switch (preset)
                    {
                        case AIProviderPreset.LmStudioLocal:
                            if (!isAnthropic) baseUrlBox.Text = "http://127.0.0.1:1234/v1";
                            break;
                        case AIProviderPreset.DeepSeek:
                            if (!isAnthropic) baseUrlBox.Text = "https://api.deepseek.com/v1";
                            break;
                        case AIProviderPreset.OpenAI:
                            if (!isAnthropic) baseUrlBox.Text = "https://api.openai.com/v1";
                            break;
                        case AIProviderPreset.Gemini:
                            if (!isAnthropic) baseUrlBox.Text = "https://generativelanguage.googleapis.com/v1beta";
                            break;
                    }
                }
            };
            panel.Children.Add(presetCombo);
        }

        panel.Children.Add(new TextBlock
        {
            Text = isAnthropic ? "Anthropic Base URL" : "Base URL",
            FontSize = 12,
            Opacity = 0.5
        });
        var baseUrlBox = new TextBox
        {
            Text = isAnthropic ? _viewModel.AnthropicBaseUrl : _viewModel.OpenAiBaseUrl,
            Width = 420
        };
        panel.Children.Add(baseUrlBox);

        panel.Children.Add(new TextBlock
        {
            Text = isAnthropic ? "Anthropic API Key" : "API Key",
            FontSize = 12,
            Opacity = 0.5
        });
        var apiKeyBox = new PasswordBox
        {
            PlaceholderText = isAnthropic ? "sk-ant-..." : "sk-...",
            Width = 420
        };
        panel.Children.Add(apiKeyBox);

        TextBox? versionBox = null;
        if (isAnthropic)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Anthropic Version",
                FontSize = 12,
                Opacity = 0.5
            });
            versionBox = new TextBox
            {
                Text = _viewModel.AnthropicVersion,
                Width = 200
            };
            panel.Children.Add(versionBox);
        }

        var dialog = new ContentDialog
        {
            Title = "API 设置",
            Content = panel,
            PrimaryButtonText = "保存设置",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            if (isAnthropic)
            {
                _viewModel.AnthropicBaseUrl = baseUrlBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(apiKeyBox.Password))
                    _viewModel.AnthropicApiKey = apiKeyBox.Password;
                if (versionBox is not null)
                    _viewModel.AnthropicVersion = versionBox.Text.Trim();
            }
            else
            {
                _viewModel.OpenAiBaseUrl = baseUrlBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(apiKeyBox.Password))
                    _viewModel.OpenAiApiKey = apiKeyBox.Password;
            }
            _viewModel.SaveCommand.Execute(null);
            RefreshSettingsRows();
            Bindings.Update();
        }
    }

    // ── AI Test ─────────────────────────────────────────────────────

    private async void AITest_Click(object sender, RoutedEventArgs e)
    {
        var panel = new StackPanel { Spacing = 14, Width = 440 };

        panel.Children.Add(new TextBlock
        {
            Text = $"Provider: {_viewModel.SelectedNoteProvider}",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"模型: {(_viewModel.SelectedNoteProvider == "Claude / Anthropic" ? _viewModel.AnthropicModelName : _viewModel.OpenAiModelName)}",
            FontSize = 13,
            Opacity = 0.7
        });

        var statusBlock = new TextBlock
        {
            Text = "点击测试按钮验证 AI Provider...",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7
        };
        panel.Children.Add(statusBlock);

        var resultsPanel = new StackPanel { Spacing = 8 };
        var connectionResult = new TextBlock
        {
            Text = _viewModel.LastConnectionTestResult ?? "连接: 未测试",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        var modelResult = new TextBlock
        {
            Text = _viewModel.LastModelTestResult ?? "模型: 未测试",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        var generationResult = new TextBlock
        {
            Text = _viewModel.LastGenerationTestResult ?? "生成: 未测试",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        resultsPanel.Children.Add(connectionResult);
        resultsPanel.Children.Add(modelResult);
        resultsPanel.Children.Add(generationResult);
        panel.Children.Add(resultsPanel);

        var actionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        var testConnectionBtn = new Button { Content = "测试连接", FontSize = 12 };
        testConnectionBtn.Click += async (_, _) =>
        {
            testConnectionBtn.IsEnabled = false;
            connectionResult.Text = "连接: 测试中...";
            await _viewModel.TestConnectionCommand.ExecuteAsync(null);
            connectionResult.Text = $"连接: {_viewModel.LastConnectionTestResult ?? "测试完成"}";
            testConnectionBtn.IsEnabled = true;
        };
        actionRow.Children.Add(testConnectionBtn);

        var testModelBtn = new Button { Content = "测试模型", FontSize = 12 };
        testModelBtn.Click += async (_, _) =>
        {
            testModelBtn.IsEnabled = false;
            modelResult.Text = "模型: 测试中...";
            await Task.Delay(500);
            _viewModel.LastModelTestResult = "模型测试通过 (mock)";
            modelResult.Text = $"模型: {_viewModel.LastModelTestResult}";
            testModelBtn.IsEnabled = true;
        };
        actionRow.Children.Add(testModelBtn);

        var testGenBtn = new Button { Content = "测试生成", FontSize = 12 };
        testGenBtn.Click += async (_, _) =>
        {
            testGenBtn.IsEnabled = false;
            generationResult.Text = "生成: 测试中...";
            await Task.Delay(500);
            _viewModel.LastGenerationTestResult = "生成测试通过 (mock)";
            generationResult.Text = $"生成: {_viewModel.LastGenerationTestResult}";
            testGenBtn.IsEnabled = true;
        };
        actionRow.Children.Add(testGenBtn);
        panel.Children.Add(actionRow);

        var saveBtn = new Button
        {
            Content = "保存设置",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        saveBtn.Click += (_, _) => _viewModel.SaveCommand.Execute(null);
        panel.Children.Add(saveBtn);

        await new ContentDialog
        {
            Title = "测试 — AI Provider",
            Content = panel,
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    // ── About sections ──────────────────────────────────────────────

    private async void OpenStorage_Click(object sender, RoutedEventArgs e)
    {
        var path = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Rokurics");
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        catch
        {
            await new ContentDialog
            {
                Title = "存储位置",
                Content = path,
                CloseButtonText = "关闭",
                XamlRoot = XamlRoot
            }.ShowAsync();
        }
    }

    private async void ShowPrivacyPolicy_Click(object sender, RoutedEventArgs e)
    {
        var panel = new StackPanel { Spacing = 8, Width = 400 };
        AddPolicyRow(panel, "录音", "需用户主动开始");
        AddPolicyRow(panel, "AI", "仅在显式触发时调用");
        AddPolicyRow(panel, "API Key", "不写入日志或笔记文件");

        await new ContentDialog
        {
            Title = "隐私政策",
            Content = panel,
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    private async void ShowCopyright_Click(object sender, RoutedEventArgs e)
    {
        var panel = new StackPanel { Spacing = 8, Width = 400 };
        AddPolicyRow(panel, "Rokurics", "Vela");
        AddPolicyRow(panel, "Vitemis", _viewModel.UserDisplayName);
        AddPolicyRow(panel, "Copyright", "2026");
        AddPolicyRow(panel, "Third-party", "随应用组件保留");
        AddPolicyRow(panel, "Version", "1.0.0 (1)");

        await new ContentDialog
        {
            Title = "版权",
            Content = panel,
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static (StackPanel stack, TextBox input) CreateLabeledField(string label, string defaultValue)
    {
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Opacity = 0.5
        });
        var input = new TextBox { Text = defaultValue, Width = 400 };
        stack.Children.Add(input);
        return (stack, input);
    }

    private static void AddPolicyRow(StackPanel panel, string label, string value)
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        var valueBlock = new TextBlock
        {
            Text = value,
            FontSize = 14,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
        };
        Grid.SetColumn(valueBlock, 1);
        grid.Children.Add(valueBlock);

        panel.Children.Add(grid);
    }
}
