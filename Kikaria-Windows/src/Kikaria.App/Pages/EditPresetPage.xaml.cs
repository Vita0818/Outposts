using Kikaria.App.Controls;
using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Kikaria.App.Pages;

/// <summary>编辑预设(对齐 Apple 版 EditPresetView):元数据编辑 + 导出 + 知识点增删改。</summary>
public sealed partial class EditPresetPage : Page
{
    private KnowledgePreset _preset = PresetLibrary.EmptyBuiltInPreset();

    public EditPresetPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var session = AppSession.Current;
        var presetId = session.PendingPresetId ?? session.State.CurrentPresetID;
        _preset = session.State.Presets.FirstOrDefault(p => p.Id == presetId)
            ?? session.CurrentPreset;

        NameBox.Text = _preset.Name;
        CategoryBox.Text = _preset.Category;
        DeletePresetButton.Visibility = _preset.IsBuiltIn ? Visibility.Collapsed : Visibility.Visible;
        RefreshUi();
    }

    private void RefreshUi()
    {
        var query = SearchBox.Text.Trim();
        ClearSearchButton.Visibility = query.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

        var points = AppSession.Current.StateForPreset(_preset.Id).KnowledgePoints;
        var filtered = query.Length == 0
            ? points
            : points.Where(point => StudyLogic.MatchesSearchQuery(point, query)).ToList();

        NoResultsCard.Visibility = filtered.Count == 0 && points.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        PointsPanel.Children.Clear();

        foreach (var point in filtered)
        {
            PointsPanel.Children.Add(MakePointRow(point));
        }
    }

    private GlassCard MakePointRow(KnowledgePoint point)
    {
        var card = new GlassCard { CardRadius = 22, ContentPadding = new Thickness(16) };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = point.Title,
                    Style = (Style)Application.Current.Resources["SubHeadlineStyle"],
                    FontSize = 16,
                    Foreground = Theme.ThemedBrush(this, "DeepTextBrush"),
                    TextWrapping = TextWrapping.Wrap
                },
                new TextBlock
                {
                    Text = string.Join(", ", point.Tags),
                    Style = (Style)Application.Current.Resources["TagTextStyle"],
                    Foreground = Theme.ThemedBrush(this, "SoftTextBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    MaxLines = 2
                }
            }
        };
        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        var editButton = new Button
        {
            Style = (Style)Application.Current.Resources["CircleButtonStyle"],
            Width = 36,
            Height = 36,
            Margin = new Thickness(8, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Background = Theme.ThemedBrush(this, "CardSubtleFillBrush"),
            Content = new FontIcon { Glyph = "\uE70F", FontSize = 12, Foreground = Theme.ThemedBrush(this, "SkyBrush") }
        };
        editButton.Click += (_, _) => EditPoint(point.Id);
        Grid.SetColumn(editButton, 1);
        grid.Children.Add(editButton);

        var deleteButton = new Button
        {
            Style = (Style)Application.Current.Resources["CircleButtonStyle"],
            Width = 36,
            Height = 36,
            VerticalAlignment = VerticalAlignment.Center,
            Background = Theme.ThemedBrush(this, "CardSubtleFillBrush"),
            Content = new FontIcon { Glyph = "\uE74D", FontSize = 12, Foreground = Theme.ThemedBrush(this, "RemoveCoralBrush") }
        };
        deleteButton.Click += async (_, _) => await ConfirmDeletePointAsync(point);
        Grid.SetColumn(deleteButton, 2);
        grid.Children.Add(deleteButton);

        card.Content = grid;
        return card;
    }

    private void EditPoint(Guid pointId)
    {
        AppSession.Current.PendingPresetId = _preset.Id;
        AppSession.Current.PendingPointId = pointId;
        MainWindow.Navigate("editPoint");
    }

    private async System.Threading.Tasks.Task ConfirmDeletePointAsync(KnowledgePoint point)
    {
        var dialog = new ContentDialog
        {
            Title = "删除知识点？",
            Content = "删除后，该知识点的重点集锦、已掌握和今日复习次数也会一并移除。",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        AppSession.Current.DeleteKnowledgePoint(_preset.Id, point.Id);
        Toast.Show(Localization.PointsUpdatedToast(
            AppSession.Current.StateForPreset(_preset.Id).KnowledgePoints.Count));
        RefreshUi();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        AppSession.Current.UpdatePresetMetadata(_preset.Id, NameBox.Text, CategoryBox.Text);
        Toast.Show("已更新 " + AppSession.Current.StateForPreset(_preset.Id).KnowledgePoints.Count + " 个知识点");
        MainWindow.GoBack();
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        var state = AppSession.Current.StateForPreset(_preset.Id);
        var markdown = MarkdownParser.MarkdownText(state.KnowledgePoints);

        var picker = new FileSavePicker { SuggestedFileName = "Kikaria-" + SanitizedFilename(_preset.Name) };
        picker.FileTypeChoices.Add("Markdown", new List<string> { ".md" });

        try
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, MainWindow.WindowHandle);
            var file = await picker.PickSaveFileAsync();
            if (file is null)
            {
                return;
            }

            await FileIO.WriteTextAsync(file, markdown);
            Toast.Show("导出文件已准备好");
        }
        catch (Exception)
        {
            Toast.Show("导出失败");
        }
    }

    private void OnAddPointClick(object sender, RoutedEventArgs e)
    {
        AppSession.Current.PendingPresetId = _preset.Id;
        AppSession.Current.PendingPointId = null;
        MainWindow.Navigate("editPoint");
    }

    private async void OnDeletePresetClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "删除预设？",
            Content = "此操作会删除该自定义预设和它的学习状态。",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        if (AppSession.Current.DeletePreset(_preset.Id) == PresetDeleteOutcome.Deleted)
        {
            Toast.Show(Localization.PresetDeletedToast(_preset.Name));
        }
        else
        {
            Toast.Show("至少需要保留一个预设");
            return;
        }

        MainWindow.GoBack();
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        RefreshUi();
    }

    private void OnClearSearchClick(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        MainWindow.GoBack();
    }

    private static string SanitizedFilename(string name)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var c in name.Trim())
        {
            builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-');
        }

        var result = builder.ToString().Trim('-');
        return result.Length == 0 ? "preset" : result;
    }
}
