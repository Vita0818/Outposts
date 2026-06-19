using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Rokurics.Helpers;
using Rokurics.Models;
using Rokurics.Services;

namespace Rokurics.Views;

/// <summary>
/// Study library page matching MacStudyLibraryView from Apple source.
/// Browser with folder tiles + recording cards, detail page, transcript/note views.
/// Round 2: markdown content loader, summary.json preview, inline create-new-value, color dots.
/// </summary>
public sealed partial class MacStudyLibraryPage : Page
{
    private readonly StudyLibraryStore _studyStore;
    private readonly AudioFileStore _audioStore;
    private readonly Stores.StudyLibrarySyncStateStore _syncStore;
    private StudyBrowserPath _currentPath = new();
    private string? _selectedDetailId;

    public bool CanGoBack => !_currentPath.IsRoot;
    public List<string> Breadcrumbs => BuildBreadcrumbs();
    public string DetailTitle { get; private set; } = "";
    public string DetailSubtitle { get; private set; } = "";
    public string FileStatusText { get; private set; } = "";
    public string SummaryPreviewText { get; private set; } = "暂无摘要";
    public string SummaryKeyPointsText { get; private set; } = "";

    // Current create-new-value level (null = none active)
    private string? _createNewLevel;
    private ActivePanel _activePanel = ActivePanel.Browser;

    private enum ActivePanel
    {
        Browser,
        Detail,
        Transcript,
        Note
    }

    public MacStudyLibraryPage()
    {
        InitializeComponent();
        _audioStore = App.Current.Services.GetService<AudioFileStore>() ?? new AudioFileStore();
        _studyStore = App.Current.Services.GetService<StudyLibraryStore>() ?? new StudyLibraryStore(_audioStore);
        _syncStore = App.Current.Services.GetService<Stores.StudyLibrarySyncStateStore>()
            ?? new Stores.StudyLibrarySyncStateStore();

        Loaded += (_, _) => RefreshBrowser();
    }

    private void SetActivePanel(ActivePanel panel)
    {
        if (_activePanel == panel) return;
        _activePanel = panel;
        BrowserPanel.Visibility = panel == ActivePanel.Browser
            ? Visibility.Visible
            : Visibility.Collapsed;
        DetailPanel.Visibility = panel == ActivePanel.Detail
            ? Visibility.Visible
            : Visibility.Collapsed;
        TranscriptPanel.Visibility = panel == ActivePanel.Transcript
            ? Visibility.Visible
            : Visibility.Collapsed;
        NotePanel.Visibility = panel == ActivePanel.Note
            ? Visibility.Visible
            : Visibility.Collapsed;
        Bindings.Update();
    }

    // ── Browser ──────────────────────────────────────────────────

    private void RefreshBrowser()
    {
        _studyStore.Refresh();
        var content = StudyLibraryBrowser.Browse(
            _studyStore.AllStudyItems, _studyStore.AllStudyFolders, _currentPath);

        if (_studyStore.AllStudyItems.Count == 0 && _studyStore.AllStudyFolders.Count == 0)
        {
            EmptyState.Visibility = Visibility.Visible;
            SetActivePanel(ActivePanel.Browser);
        }
        else if (content.Folders.Count == 0 && content.Items.Count == 0)
        {
            EmptyState.Visibility = Visibility.Collapsed;
            SetActivePanel(ActivePanel.Browser);
        }
        else
        {
            EmptyState.Visibility = Visibility.Collapsed;
            SetActivePanel(ActivePanel.Browser);
            RenderContent(content);
        }

        RefreshSyncStatus();
        Bindings.Update();
    }

    private void RefreshSyncStatus()
    {
        var state = _syncStore.State;
        var lastSync = state.LastSuccessfulSyncAt;
        if (lastSync.HasValue)
        {
            SyncStatusText.Text = $"上次同步: {lastSync.Value:yyyy-MM-dd HH:mm}";
            SyncStatusBar.Visibility = Visibility.Visible;
        }
        else if (state.LastPushedAt.HasValue)
        {
            SyncStatusText.Text = $"上次推送: {state.LastPushedAt.Value:yyyy-MM-dd HH:mm}";
            SyncStatusBar.Visibility = Visibility.Visible;
        }
        else
        {
            SyncStatusBar.Visibility = Visibility.Collapsed;
        }
    }

    private async void SyncStudyLibrary_Click(object sender, RoutedEventArgs e)
    {
        SyncNowStudyButton.IsEnabled = false;
        SyncStatusText.Text = "同步中...";
        try
        {
            // Placeholder: trigger sync via Kestrel pairing service
            await Task.Delay(500);
            _syncStore.RecordPush("local", null);
            RefreshSyncStatus();
        }
        catch
        {
            SyncStatusText.Text = "同步失败";
        }
        finally
        {
            SyncNowStudyButton.IsEnabled = true;
        }
    }

    private void RenderContent(StudyBrowserContent content)
    {
        var folderTiles = new List<Border>();
        foreach (var folder in content.Folders)
            folderTiles.Add(CreateFolderTile(folder));
        FoldersGrid.ItemsSource = folderTiles;

        var cards = new List<Border>();
        foreach (var item in content.Items)
        {
            if (item.Kind == StudyItemKind.RecordingBundle && item.RecordingId is not null)
                cards.Add(CreateRecordingCard(item));
            else
                cards.Add(CreateNoteCard(item));
        }
        ItemsList.ItemsSource = cards;
    }

    // ── Folder tile with color dot overlay ──────────────────────

    private Border CreateFolderTile(StudyBrowserFolder folder)
    {
        // Folder icon container with color dot overlay
        var iconContainer = new Grid
        {
            Width = 60, Height = 54,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Main folder icon
        iconContainer.Children.Add(new FontIcon
        {
            Glyph = "",
            FontSize = 36,
            Foreground = new SolidColorBrush(Helpers.RokuricsColors.FolderColorFor(folder.ColorToken)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        // Color dot overlay (matching MacSystemFolderIconView)
        if (folder.ColorToken is not null && folder.ColorToken != StudyFolderColorToken.Default)
        {
            var dotColor = ColorTokenBrush(folder.ColorToken.Value);
            var dot = new Border
            {
                Width = 12, Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = dotColor,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 4, 2),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)) { Opacity = 0.6 },
                BorderThickness = new Thickness(1)
            };
            iconContainer.Children.Add(dot);
        }

        var stack = new StackPanel { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(iconContainer);
        stack.Children.Add(new TextBlock
        {
            Text = folder.Title,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            MaxLines = 2,
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"{folder.ItemCount} 项",
            FontSize = 11,
            Opacity = 0.5,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var border = new Border
        {
            Style = (Style)Resources["RokuricsCardStyle"],
            Padding = new Thickness(12, 16, 12, 16),
            Child = stack,
            Tag = folder
        };
        border.PointerPressed += (s, e) =>
        {
            if (s is Border b && b.Tag is StudyBrowserFolder f)
            {
                _currentPath = f.Path;
                RefreshBrowser();
            }
        };

        // Right-click context menu: rename, set color, delete
        var contextMenu = new MenuFlyout();

        var renameItem = new MenuFlyoutItem { Text = "重命名" };
        renameItem.Click += async (_, _) => await ShowRenameFolderDialog(folder);
        contextMenu.Items.Add(renameItem);

        var colorItem = new MenuFlyoutItem { Text = "设置颜色..." };
        colorItem.Click += async (_, _) => await ShowColorPickerDialog(folder);
        contextMenu.Items.Add(colorItem);

        contextMenu.Items.Add(new MenuFlyoutSeparator());

        var deleteItem = new MenuFlyoutItem { Text = "删除" };
        deleteItem.Click += async (_, _) => await ShowDeleteFolderDialog(folder);
        contextMenu.Items.Add(deleteItem);

        FlyoutBase.SetAttachedFlyout(border, contextMenu);
        border.RightTapped += (s, e) =>
        {
            FlyoutBase.ShowAttachedFlyout(s as FrameworkElement ?? border);
        };

        return border;
    }

    private static SolidColorBrush ColorTokenBrush(StudyFolderColorToken token) => token switch
    {
        StudyFolderColorToken.Red => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 69, 58)),
        StudyFolderColorToken.Orange => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 133, 41)),
        StudyFolderColorToken.Yellow => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 200, 66)),
        StudyFolderColorToken.Green => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 52, 199, 89)),
        StudyFolderColorToken.Mint => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 84, 209, 158)),
        StudyFolderColorToken.Teal => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 43, 173, 168)),
        StudyFolderColorToken.Cyan => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 184, 235)),
        StudyFolderColorToken.Blue => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 122, 255)),
        StudyFolderColorToken.Indigo => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 87, 107, 219)),
        StudyFolderColorToken.Purple => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 140, 110, 240)),
        StudyFolderColorToken.Gray => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 142, 142, 147)),
        _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128))
    };

    private async Task ShowRenameFolderDialog(StudyBrowserFolder folder)
    {
        var input = new TextBox { Text = folder.Title, Width = 300 };
        var dialog = new ContentDialog
        {
            Title = "重命名文件夹",
            Content = input,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(input.Text))
        {
            _studyStore.RenameFolder(folder.FolderId, input.Text.Trim());
            RefreshBrowser();
        }
    }

    private async Task ShowDeleteFolderDialog(StudyBrowserFolder folder)
    {
        var dialog = new ContentDialog
        {
            Title = "删除文件夹",
            Content = $"确定要删除「{folder.Title}」吗？\n此操作不可撤销。",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            _studyStore.RemoveFolder(folder.FolderId);
            RefreshBrowser();
        }
    }

    private async Task ShowColorPickerDialog(StudyBrowserFolder folder)
    {
        var picker = new FolderColorPicker();
        picker.Initialize(folder.ColorToken ?? StudyFolderColorToken.Default);
        picker.Width = 340;

        var dialog = new ContentDialog
        {
            Title = "文件夹颜色",
            Content = picker,
            CloseButtonText = "取消",
            XamlRoot = XamlRoot
        };

        picker.ColorSelected += async (token) =>
        {
            _studyStore.SetFolderColor(folder.FolderId, token);
            RefreshBrowser();
            dialog.Hide();
        };

        picker.ResetToDefault += () =>
        {
            _studyStore.SetFolderColor(folder.FolderId, StudyFolderColorToken.Default);
        };

        await dialog.ShowAsync();
    }

    private static string ColorTokenDisplayName(StudyFolderColorToken token) => token switch
    {
        StudyFolderColorToken.Default => "默认",
        StudyFolderColorToken.Red => "红色",
        StudyFolderColorToken.Orange => "橙色",
        StudyFolderColorToken.Yellow => "黄色",
        StudyFolderColorToken.Green => "绿色",
        StudyFolderColorToken.Mint => "薄荷",
        StudyFolderColorToken.Teal => "青蓝",
        StudyFolderColorToken.Cyan => "青色",
        StudyFolderColorToken.Blue => "蓝色",
        StudyFolderColorToken.Indigo => "靛蓝",
        StudyFolderColorToken.Purple => "紫色",
        StudyFolderColorToken.Gray => "灰色",
        _ => token.ToString()
    };

    private Button CreateSmallButton(string content, string tooltip, Action clickHandler)
    {
        var btn = new Button
        {
            Content = content,
            FontSize = 12,
            Width = 32,
            Height = 32,
            Padding = new Thickness(4),
        };
        ToolTipService.SetToolTip(btn, tooltip);
        btn.Click += (_, _) => clickHandler();
        return btn;
    }

    private Border CreateRecordingCard(StudyItemMetadata item)
    {
        var grid = new Grid { ColumnSpacing = 14 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Icon with hover swap — mirrors Mac waveform→trash on hover (0.12s easeInOut)
        var icon = new FontIcon
        {
            Glyph = "", // waveform
            FontSize = 16,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Helpers.RokuricsColors.Aqua),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);

        var info = new StackPanel { Spacing = 4 };
        info.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            MaxLines = 1
        });
        info.Children.Add(new TextBlock
        {
            Text = $"{item.CreatedAt:MM-dd HH:mm} · {DurationText(item.Duration ?? TimeSpan.Zero)}",
            FontSize = 13,
            Opacity = 0.5
        });
        Grid.SetColumn(info, 1);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Width = 160,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        actions.Children.Add(CreateSmallButton("▶", "播放 / 打开文件", () => OpenRecording(item)));
        actions.Children.Add(CreateSmallButton("📝", "转写", () => StartTranscription(item)));
        actions.Children.Add(CreateSmallButton("✨", "AI 总结", () => StartNoteGeneration(item)));
        actions.Children.Add(CreateSmallButton("💬", "导入对话", () => ImportToChat(item)));
        Grid.SetColumn(actions, 2);

        grid.Children.Add(icon);
        grid.Children.Add(info);
        grid.Children.Add(actions);

        var border = new Border
        {
            Style = (Style)Resources["RokuricsCardStyle"],
            Padding = new Thickness(16, 12, 16, 12),
            Child = grid,
            Tag = item
        };

        // Hover: waveform → trash icon swap, matching Mac behavior
        border.PointerEntered += (s, e) =>
        {
            icon.Glyph = ""; // trash
            icon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Helpers.RokuricsColors.Coral);
        };
        border.PointerExited += (s, e) =>
        {
            icon.Glyph = ""; // waveform
            icon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Helpers.RokuricsColors.Aqua);
        };
        border.PointerPressed += (s, e) =>
        {
            if (s is Border b && b.Tag is StudyItemMetadata m && m.RecordingId is not null)
                OpenDetail(m);
        };
        return border;
    }

    private Border CreateNoteCard(StudyItemMetadata item)
    {
        var grid = new Grid { ColumnSpacing = 14 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new FontIcon
        {
            Glyph = "",
            FontSize = 21,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Helpers.RokuricsColors.Leaf),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);

        var info = new StackPanel { Spacing = 4 };
        info.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            MaxLines = 1
        });
        info.Children.Add(new TextBlock
        {
            Text = $"{item.CreatedAt:yyyy-MM-dd HH:mm}",
            FontSize = 12,
            Opacity = 0.5
        });
        Grid.SetColumn(info, 1);

        grid.Children.Add(icon);
        grid.Children.Add(info);

        return new Border
        {
            Style = (Style)Resources["RokuricsCardStyle"],
            Padding = new Thickness(16),
            Child = grid,
            Tag = item
        };
    }

    // ── Recording card actions ────────────────────────────────────

    private void OpenRecording(StudyItemMetadata item)
    {
        if (item.AudioRelativePath is null) return;
        try
        {
            var audioPath = _audioStore.AbsolutePath(item.AudioRelativePath);
            if (System.IO.File.Exists(audioPath))
                System.Diagnostics.Process.Start("explorer.exe", audioPath);
        }
        catch { }
    }

    private void StartTranscription(StudyItemMetadata item)
    {
        // Placeholder: transcription would be started via TranscriptionCoordinator
        // On Windows we'd call the Whisper.cpp provider through the transcription pipeline
    }

    private void StartNoteGeneration(StudyItemMetadata item)
    {
        // Placeholder: note generation would be started via NoteGenerationCoordinator
        // Uses the configured AI provider (OpenAI-compatible or Anthropic)
    }

    private void ImportToChat(StudyItemMetadata item)
    {
        // Build chat context from this item and navigate to chat page
        // This would create a ChatContext with the item's transcript/note content
    }

    // ── Detail view with summary preview loading ────────────────

    private void OpenDetail(StudyItemMetadata item)
    {
        _selectedDetailId = item.ItemId;
        DetailTitle = item.Title;
        DetailSubtitle = $"{item.CreatedAt:yyyy-MM-dd HH:mm} · {DurationText(item.Duration ?? TimeSpan.Zero)}";

        // File status panel
        FileStatusText = $"recordingID: {item.RecordingId}\n" +
            $"audio: {(item.AudioRelativePath is not null ? "可用" : "缺失")}\n" +
            $"audio path: {item.AudioRelativePath ?? "无"}\n" +
            $"transcript: {(item.IsTranscribed ? "已生成" : item.TranscriptionStatus ?? "未生成")}\n" +
            $"transcript path: {item.TranscriptRelativePath ?? "未生成"}\n" +
            $"note: {item.NoteStatus ?? "未生成"}\n" +
            $"note path: {item.NoteRelativePath ?? "未生成"}\n" +
            $"receive: {item.ReceiveRelativePath ?? "无"}";

        // Load AI summary preview from summary.json
        LoadSummaryPreview(item);

        this.FindName("DetailPanel");
        SetActivePanel(ActivePanel.Detail);

        // Load filing draft
        var filing = item.FilingPath;
        TypeBox.Text = filing.Type ?? "";
        SubjectBox.Text = filing.Subject ?? "";
        ChapterBox.Text = filing.Chapter ?? "";
        TopicBox.Text = filing.Topic ?? "";

        // Hide create-new-value row
        CreateNewPanel.Visibility = Visibility.Collapsed;
        _createNewLevel = null;

        Bindings.Update();
    }

    /// <summary>
    /// Load AI summary preview from summary.json in note directory.
    /// Mirrors MacStudyNoteSummaryPreviewCard from Apple source.
    /// </summary>
    private void LoadSummaryPreview(StudyItemMetadata item)
    {
        try
        {
            if (item.NoteRelativePath is null)
            {
                SummaryPreviewText = "暂无摘要";
                SummaryKeyPointsText = "";
                return;
            }

            var noteAbsPath = _audioStore.AbsolutePath(item.NoteRelativePath);
            var noteDir = Path.GetDirectoryName(noteAbsPath);
            if (noteDir is null || !Directory.Exists(noteDir))
            {
                SummaryPreviewText = "暂无摘要";
                SummaryKeyPointsText = "";
                return;
            }

            var summaryPath = Path.Combine(noteDir, "summary.json");
            if (!File.Exists(summaryPath))
            {
                SummaryPreviewText = "暂无摘要";
                SummaryKeyPointsText = "";
                return;
            }

            var json = File.ReadAllText(summaryPath);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? shortSummary = null;
            var keyPoints = new List<string>();

            if (root.TryGetProperty("short_summary", out var ss))
                shortSummary = ss.GetString();
            else if (root.TryGetProperty("summary", out var s))
                shortSummary = s.GetString();

            if (root.TryGetProperty("key_points", out var kp) && kp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var point in kp.EnumerateArray())
                {
                    var text = point.GetString();
                    if (!string.IsNullOrEmpty(text))
                        keyPoints.Add(text);
                }
            }

            if (!string.IsNullOrEmpty(shortSummary))
            {
                SummaryPreviewText = MarkdownRenderer.ExtractSummary(shortSummary, 300);
            }
            else
            {
                SummaryPreviewText = "暂无摘要";
            }

            SummaryKeyPointsText = keyPoints.Count > 0
                ? string.Join("\n", keyPoints.Take(4).Select(p => $"• {p}"))
                : "";
            SummaryKeyPoints.Visibility = keyPoints.Count > 0
                ? Visibility.Visible : Visibility.Collapsed;

            Bindings.Update();
        }
        catch
        {
            SummaryPreviewText = "暂无摘要";
            SummaryKeyPointsText = "";
        }
    }

    private void CloseDetail_Click(object sender, RoutedEventArgs e)
    {
        _selectedDetailId = null;
        SetActivePanel(ActivePanel.Browser);
        RefreshBrowser();
    }

    private void SaveFiling_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDetailId is null) return;

        var item = _studyStore.FindItem(_selectedDetailId);
        if (item?.RecordingId is null) return;

        var filing = new StudyFilingPath(
            TypeBox.Text.NullIfEmpty(),
            SubjectBox.Text.NullIfEmpty(),
            ChapterBox.Text.NullIfEmpty(),
            TopicBox.Text.NullIfEmpty());

        var recordingMgr = App.Current.Services.GetService<RecordingManager>();
        recordingMgr?.UpdateStudyFiling(item.RecordingId, filing);
        _studyStore.UpdateFiling(item.RecordingId, filing);

        // Reload to reflect saved filing in item
        _studyStore.Refresh();
        var updated = _studyStore.FindItem(_selectedDetailId);
        if (updated is not null)
        {
            FileStatusText = $"recordingID: {updated.RecordingId}\n" +
                $"audio: {(updated.AudioRelativePath is not null ? "可用" : "缺失")}\n" +
                $"transcript: {(updated.IsTranscribed ? "已生成" : updated.TranscriptionStatus ?? "未生成")}\n" +
                $"note: {updated.NoteStatus ?? "未生成"}\n" +
                "\n归档已保存";
        }

        Bindings.Update();
    }

    // ── Inline create-new-value (matching MacStudyFilingPicker) ───

    private void ShowCreateNewValue_Click(object sender, RoutedEventArgs e)
    {
        // Determine which level to create for based on which box is empty
        var currentDraft = new StudyFilingPath(
            TypeBox.Text.NullIfEmpty(),
            SubjectBox.Text.NullIfEmpty(),
            ChapterBox.Text.NullIfEmpty(),
            TopicBox.Text.NullIfEmpty());

        if (string.IsNullOrEmpty(currentDraft.Type))
            _createNewLevel = "type";
        else if (string.IsNullOrEmpty(currentDraft.Subject))
            _createNewLevel = "subject";
        else if (string.IsNullOrEmpty(currentDraft.Chapter))
            _createNewLevel = "chapter";
        else
            _createNewLevel = "topic";

        var levelDisplay = _createNewLevel switch
        {
            "type" => "门类",
            "subject" => "课程",
            "chapter" => "章节",
            "topic" => "主题",
            _ => "值"
        };

        CreateNewLabel.Text = $"新建{levelDisplay}";
        CreateNewTextBox.Text = "";
        CreateNewPanel.Visibility = Visibility.Visible;
    }

    private void CreateNewValueConfirm_Click(object sender, RoutedEventArgs e)
    {
        var name = CreateNewTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name) || _createNewLevel is null || _selectedDetailId is null)
            return;

        // Create folder at the appropriate level
        var currentDraft = new StudyFilingPath(
            TypeBox.Text.NullIfEmpty(),
            SubjectBox.Text.NullIfEmpty(),
            ChapterBox.Text.NullIfEmpty(),
            TopicBox.Text.NullIfEmpty());

        // Build parent path for the new value
        var parentComponents = new List<string>();
        if (_createNewLevel != "type" && !string.IsNullOrEmpty(currentDraft.Type))
            parentComponents.Add(currentDraft.Type);
        if (_createNewLevel != "subject" && !string.IsNullOrEmpty(currentDraft.Subject))
            parentComponents.Add(currentDraft.Subject);
        if (_createNewLevel != "chapter" && !string.IsNullOrEmpty(currentDraft.Chapter))
            parentComponents.Add(currentDraft.Chapter);

        var parentPath = new StudyBrowserPath(parentComponents);
        _studyStore.CreateFolder(name, parentPath);

        // Set the value in the appropriate text box
        switch (_createNewLevel)
        {
            case "type": TypeBox.Text = name; break;
            case "subject": SubjectBox.Text = name; break;
            case "chapter": ChapterBox.Text = name; break;
            case "topic": TopicBox.Text = name; break;
        }

        CreateNewPanel.Visibility = Visibility.Collapsed;
        _createNewLevel = null;

        // Auto-save the filing
        SaveFiling_Click(sender, e);
    }

    private void CreateNewValueCancel_Click(object sender, RoutedEventArgs e)
    {
        CreateNewPanel.Visibility = Visibility.Collapsed;
        _createNewLevel = null;
    }

    // ── Detail action buttons ────────────────────────────────────

    private void TranscribeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDetailId is null) return;
        var item = _studyStore.FindItem(_selectedDetailId);
        if (item is not null)
            StartTranscription(item);
    }

    private void ViewTranscriptButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDetailId is null) return;
        var item = _studyStore.FindItem(_selectedDetailId);
        if (item is null) return;

        ShowTranscriptView(item);
    }

    private void GenerateNoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDetailId is null) return;
        var item = _studyStore.FindItem(_selectedDetailId);
        if (item is not null)
            StartNoteGeneration(item);
    }

    private void ViewNoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDetailId is null) return;
        var item = _studyStore.FindItem(_selectedDetailId);
        if (item is null) return;

        ShowNoteView(item);
    }

    // ── Transcript / Note views with markdown content loader ─────

    private void ShowTranscriptView(StudyItemMetadata item)
    {
        this.FindName("TranscriptPanel");
        SetActivePanel(ActivePanel.Transcript);

        TranscriptTitle.Text = item.Title;
        TranscriptSubtitle.Text = $"{item.CreatedAt:yyyy-MM-dd HH:mm} · 转写";

        // Populate metadata
        TranscriptMetadataGrid.Children.Clear();
        TranscriptMetadataGrid.RowDefinitions.Clear();
        AddMetadataRow(TranscriptMetadataGrid, "recordingID", item.RecordingId ?? "未知");
        AddMetadataRow(TranscriptMetadataGrid, "状态", item.IsTranscribed ? "已生成" : item.TranscriptionStatus ?? "未生成");
        AddMetadataRow(TranscriptMetadataGrid, "音频", item.AudioRelativePath ?? "缺失");
        AddMetadataRow(TranscriptMetadataGrid, "转写文件", item.TranscriptRelativePath ?? "未生成");

        // Try loading transcript result JSON (segments with timing)
        var transcriptResult = LoadTranscriptResult(item);

        // Try loading markdown content (prefer .md over .txt)
        var transcript = TryLoadTranscript(item);
        if (!string.IsNullOrEmpty(transcript))
        {
            TranscriptContent.Text = MarkdownRenderer.Render(transcript);

            // Add segment info if available
            if (transcriptResult?.Segments is { Count: > 0 })
            {
                AddMetadataRow(TranscriptMetadataGrid, "段落数", transcriptResult.Segments.Count.ToString());
                AddMetadataRow(TranscriptMetadataGrid, "Provider", transcriptResult.ProviderName ?? "未知");
                if (transcriptResult.ModelName is not null)
                    AddMetadataRow(TranscriptMetadataGrid, "模型", transcriptResult.ModelName);
                if (transcriptResult.Language is not null)
                    AddMetadataRow(TranscriptMetadataGrid, "语言", transcriptResult.Language);
                if (transcriptResult.DurationSeconds.HasValue)
                    AddMetadataRow(TranscriptMetadataGrid, "时长",
                        FormatDuration(transcriptResult.DurationSeconds.Value));
            }
        }
        else
        {
            TranscriptContent.Text = "转写内容尚未生成。\n请先点击「转写」按钮开始转写。";
        }
    }

    private TranscriptResult? LoadTranscriptResult(StudyItemMetadata item)
    {
        try
        {
            if (item.TranscriptRelativePath is null) return null;
            var absPath = _audioStore.AbsolutePath(item.TranscriptRelativePath);
            // Try .json transcript result alongside .txt
            var jsonPath = Path.ChangeExtension(absPath, ".json");
            if (!File.Exists(jsonPath))
            {
                // Try alternate naming: transcript_result.json in note directory
                var dir = Path.GetDirectoryName(absPath);
                if (dir is not null)
                {
                    jsonPath = Path.Combine(dir, "transcript_result.json");
                }
            }
            return TranscriptResult.LoadFromJson(jsonPath);
        }
        catch { return null; }
    }

    private static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"m\:ss");
    }

    private void ShowNoteView(StudyItemMetadata item)
    {
        this.FindName("NotePanel");
        SetActivePanel(ActivePanel.Note);

        NoteTitle.Text = item.Title;
        NoteSubtitle.Text = $"{item.CreatedAt:yyyy-MM-dd HH:mm} · AI 总结";

        // Populate metadata
        NoteMetadataGrid.Children.Clear();
        NoteMetadataGrid.RowDefinitions.Clear();
        AddMetadataRow(NoteMetadataGrid, "recordingID", item.RecordingId ?? "未知");
        AddMetadataRow(NoteMetadataGrid, "笔记状态", item.NoteStatus ?? "未生成");
        AddMetadataRow(NoteMetadataGrid, "笔记文件", item.NoteRelativePath ?? "未生成");

        // Try loading note + summary
        var (note, summary) = TryLoadNoteAndSummary(item);

        string? displayContent = null;
        if (!string.IsNullOrEmpty(note))
        {
            // Extract provider/model metadata from note frontmatter
            var (provider, model, generatedAt) = MarkdownRenderer.ExtractNoteMetadata(note);
            if (provider is not null)
                AddMetadataRow(NoteMetadataGrid, "Provider", provider);
            if (model is not null)
                AddMetadataRow(NoteMetadataGrid, "模型", model);

            displayContent = MarkdownRenderer.Render(note);
        }
        else if (!string.IsNullOrEmpty(summary))
        {
            AddMetadataRow(NoteMetadataGrid, "Provider", "由 AI 模型生成");
            displayContent = "## AI 摘要\n\n" + summary;
        }
        else
        {
            AddMetadataRow(NoteMetadataGrid, "Provider", "未生成");
        }

        if (!string.IsNullOrEmpty(displayContent))
        {
            NoteContent.Text = MarkdownRenderer.Render(displayContent);
        }
        else
        {
            NoteContent.Text = "AI 总结尚未生成。\n请先点击「AI 总结」按钮开始生成。";
        }
    }

    private void CloseTranscript_Click(object sender, RoutedEventArgs e)
    {
        SetActivePanel(ActivePanel.Detail);
    }

    private void CloseNote_Click(object sender, RoutedEventArgs e)
    {
        SetActivePanel(ActivePanel.Detail);
    }

    /// <summary>
    /// Lightweight markdown-to-plain-text for display in TextBlock.
    /// Delegates to MarkdownRenderer for consistent formatting.
    /// </summary>
    private static string FormatMarkdownForDisplay(string markdown)
        => MarkdownRenderer.Render(markdown);

    /// <summary>
    /// Load transcript content from file system.
    /// Prefers .md (markdown) over .txt (plain text).
    /// </summary>
    private string? TryLoadTranscript(StudyItemMetadata item)
    {
        try
        {
            // Try loading as markdown first
            if (item.TranscriptMarkdownRelativePath is not null)
            {
                var mdPath = _audioStore.AbsolutePath(item.TranscriptMarkdownRelativePath);
                if (File.Exists(mdPath))
                    return File.ReadAllText(mdPath);
            }

            if (item.TranscriptRelativePath is not null)
            {
                var path = _audioStore.AbsolutePath(item.TranscriptRelativePath);
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }

            return null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Load note content and summary preview from file system.
    /// Tries summary.json for structured preview, falls back to raw markdown.
    /// </summary>
    private (string? note, string? summary) TryLoadNoteAndSummary(StudyItemMetadata item)
    {
        try
        {
            var note = (string?)null;
            var summary = (string?)null;

            if (item.NoteRelativePath is not null)
            {
                var notePath = _audioStore.AbsolutePath(item.NoteRelativePath);
                if (File.Exists(notePath))
                    note = File.ReadAllText(notePath);

                // Try to load summary.json from note directory
                var noteDir = Path.GetDirectoryName(notePath);
                if (noteDir is not null)
                {
                    var summaryPath = Path.Combine(noteDir, "summary.json");
                    if (File.Exists(summaryPath))
                    {
                        var json = File.ReadAllText(summaryPath);
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        var summaryParts = new List<string>();
                        if (root.TryGetProperty("short_summary", out var ss))
                            summaryParts.Add(ss.GetString() ?? "");
                        else if (root.TryGetProperty("summary", out var s))
                            summaryParts.Add(s.GetString() ?? "");

                        if (root.TryGetProperty("key_points", out var kp) &&
                            kp.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var points = new List<string>();
                            foreach (var point in kp.EnumerateArray())
                            {
                                var text = point.GetString();
                                if (!string.IsNullOrEmpty(text))
                                    points.Add($"  • {text}");
                            }
                            if (points.Count > 0)
                                summaryParts.Add("要点：\n" + string.Join("\n", points));
                        }

                        summary = string.Join("\n\n", summaryParts.Where(p => !string.IsNullOrEmpty(p)));
                    }
                }
            }

            return (note, summary);
        }
        catch { return (null, null); }
    }

    private static void AddMetadataRow(Grid grid, string label, string value)
    {
        var rowIndex = grid.RowDefinitions.Count;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var labelBlock = new TextBlock
        {
            Text = label,
            FontSize = 11,
            Opacity = 0.5,
            Margin = new Thickness(0, 0, 12, 4)
        };
        Grid.SetRow(labelBlock, rowIndex);
        Grid.SetColumn(labelBlock, 0);

        var valueBlock = new TextBlock
        {
            Text = value,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        };
        Grid.SetRow(valueBlock, rowIndex);
        Grid.SetColumn(valueBlock, 1);

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);
    }

    // ── Filing picker candidate autocomplete ─────────────────────

    private void FilingBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        RefreshCandidates(tb);
    }

    private void FilingBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        RefreshCandidates(tb);
    }

    private void Candidate_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not string value) return;
        if (sender is not ListView senderList) return;

        // Determine which TextBox to fill based on which candidate list was clicked
        if (ReferenceEquals(senderList, TypeCandidates))
            TypeBox.Text = value;
        else if (ReferenceEquals(senderList, SubjectCandidates))
            SubjectBox.Text = value;
        else if (ReferenceEquals(senderList, ChapterCandidates))
            ChapterBox.Text = value;
        else if (ReferenceEquals(senderList, TopicCandidates))
            TopicBox.Text = value;

        // Hide all candidate lists
        HideAllCandidates();
    }

    private void RefreshCandidates(TextBox tb)
    {
        var allValues = CollectFilingValues(tb);
        var filter = tb.Text?.Trim() ?? "";

        var candidates = string.IsNullOrEmpty(filter)
            ? allValues
            : allValues.Where(v => v.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        var listView = CandidateListFor(tb);
        if (listView is null) return;

        if (candidates.Count == 0 || (candidates.Count == 1 &&
            string.Equals(candidates[0], filter, StringComparison.OrdinalIgnoreCase)))
        {
            listView.Visibility = Visibility.Collapsed;
            return;
        }

        listView.ItemsSource = candidates;
        listView.Visibility = Visibility.Visible;
    }

    private void HideAllCandidates()
    {
        TypeCandidates.Visibility = Visibility.Collapsed;
        SubjectCandidates.Visibility = Visibility.Collapsed;
        ChapterCandidates.Visibility = Visibility.Collapsed;
        TopicCandidates.Visibility = Visibility.Collapsed;
    }

    private ListView? CandidateListFor(TextBox tb)
    {
        if (tb == TypeBox) return TypeCandidates;
        if (tb == SubjectBox) return SubjectCandidates;
        if (tb == ChapterBox) return ChapterCandidates;
        if (tb == TopicBox) return TopicCandidates;
        return null;
    }

    private List<string> CollectFilingValues(TextBox tb)
    {
        var levelIndex = tb == TypeBox ? 0 : tb == SubjectBox ? 1 : tb == ChapterBox ? 2 : 3;
        var levelKey = StudyLibraryBrowser.LevelKeys[levelIndex];

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in _studyStore.AllStudyItems.Where(i => !i.IsTrashed))
        {
            var val = item.FilingPath.ValueFor(levelKey);
            if (!string.IsNullOrWhiteSpace(val) && val != StudyHierarchyRule.MissingValue)
                values.Add(val);
        }

        return values.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ── Toolbar actions ──────────────────────────────────────────

    private void GoBack_Click(object sender, RoutedEventArgs e)
    {
        if (!_currentPath.IsRoot)
        {
            _currentPath = _currentPath.Parent;
            RefreshBrowser();
        }
    }

    private async void NewFolder_Click(object sender, RoutedEventArgs e)
    {
        // Show dialog to get new folder name
        var dialog = new ContentDialog
        {
            Title = "新建文件夹",
            PrimaryButtonText = "创建",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var input = new TextBox { PlaceholderText = "文件夹名称", Width = 300 };
        dialog.Content = input;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(input.Text))
        {
            var levelKey = StudyLibraryBrowser.LevelKeys[Math.Min(
                _currentPath.Depth, StudyLibraryBrowser.LevelKeys.Length - 1)];
            _studyStore.CreateFolder(input.Text.Trim(), _currentPath);
            RefreshBrowser();
        }
    }

    private async void ImportToChat_Click(object sender, RoutedEventArgs e)
    {
        // Build chat context from current browse path and all items in it
        var content = StudyLibraryBrowser.Browse(
            _studyStore.AllStudyItems, _studyStore.AllStudyFolders, _currentPath);

        if (content.Items.Count == 0)
        {
            await new ContentDialog
            {
                Title = "提示",
                Content = "当前路径下没有可导入的学习资料。",
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            }.ShowAsync();
            return;
        }

        // Navigate to chat page with context
        // The context would be built and passed to the chat page
    }

    private async void OpenTrash_Click(object sender, RoutedEventArgs e)
    {
        // Show trash items in a dialog or toggle trash view
        var recordingMgr = App.Current.Services.GetService<RecordingManager>();
        var trashed = recordingMgr?.TrashedRecordings ?? new List<RecordingMetadata>();

        if (trashed.Count == 0)
        {
            await new ContentDialog
            {
                Title = "废纸篓",
                Content = "废纸篓为空。",
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            }.ShowAsync();
            return;
        }

        var list = new ListView { MaxHeight = 400 };
        var items = trashed.Select(r => new TextBlock { Text = r.Title, Margin = new Thickness(4) }).ToList();
        list.ItemsSource = items;

        var restoreBtn = new Button { Content = "恢复选中", Margin = new Thickness(0, 8, 0, 0) };
        restoreBtn.Click += (_, _) =>
        {
            if (list.SelectedItem is TextBlock tb)
            {
                var rec = trashed.FirstOrDefault(r => r.Title == tb.Text);
                if (rec is not null)
                    recordingMgr?.RestoreRecording(rec.Id);
                RefreshBrowser();
            }
        };

        var panel = new StackPanel();
        panel.Children.Add(list);
        panel.Children.Add(restoreBtn);

        await new ContentDialog
        {
            Title = $"废纸篓 ({trashed.Count} 项)",
            Content = panel,
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot
        }.ShowAsync();

        RefreshBrowser();
    }

    // ── Breadcrumbs ──────────────────────────────────────────────

    private List<string> BuildBreadcrumbs()
    {
        var crumbs = new List<string> { "学习库" };
        crumbs.AddRange(_currentPath.Components);
        return crumbs;
    }

    private static string DurationText(TimeSpan d) =>
        d.TotalSeconds < 60
            ? $"{(int)d.TotalSeconds}''"
            : $"{d.Minutes}'{d.Seconds:D2}''";
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? s)
        => string.IsNullOrEmpty(s) ? null : s;
}
