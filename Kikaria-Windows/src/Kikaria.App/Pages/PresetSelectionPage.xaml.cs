using Kikaria.App.Controls;
using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Kikaria.App.Pages;

/// <summary>预设管理(对齐 Apple 版 PresetSelectionView):切换确认、编辑、删除。</summary>
public sealed partial class PresetSelectionPage : Page
{
    public PresetSelectionPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefreshUi();
    }

    private void RefreshUi()
    {
        var session = AppSession.Current;
        PresetListPanel.Children.Clear();

        foreach (var preset in session.State.Presets)
        {
            PresetListPanel.Children.Add(MakePresetCard(preset, preset.Id == session.State.CurrentPresetID));
        }
    }

    private string DisplayName(KnowledgePreset preset) =>
        Localization.BuiltInPresetDisplayName(preset.Name, preset.IsBuiltIn);

    private GlassCard MakePresetCard(KnowledgePreset preset, bool isCurrent)
    {
        var card = new GlassCard { CardRadius = 24, ContentPadding = new Thickness(0) };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // 信息区(点击切换;当前预设禁用)。
        var infoButton = new Button
        {
            Style = (Style)Application.Current.Resources["ActionButtonStyle"],
            Background = new SolidColorBrush(Windows.UI.Colors.Transparent),
            Foreground = Theme.ThemedBrush(this, "DeepTextBrush"),
            CornerRadius = new CornerRadius(24, 0, 0, 24),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsEnabled = !isCurrent,
            Opacity = isCurrent ? 0.92 : 1.0,
            Content = new StackPanel
            {
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    MakeTitleRow(preset, isCurrent),
                    new TextBlock
                    {
                        Text = Localization.KnowledgePointCount(preset.KnowledgePointCount),
                        Style = (Style)Application.Current.Resources["TagTextStyle"],
                        Foreground = Theme.ThemedBrush(this, "SoftTextBrush")
                    }
                }
            }
        };
        infoButton.Click += async (_, _) => await ConfirmSwitchAsync(preset);
        Grid.SetColumn(infoButton, 0);
        grid.Children.Add(infoButton);

        var editButton = new Button
        {
            Style = (Style)Application.Current.Resources["CircleButtonStyle"],
            Width = 38,
            Height = 38,
            Margin = new Thickness(8, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Background = Theme.ThemedBrush(this, "CardSubtleFillBrush"),
            Content = new FontIcon { Glyph = "\uE70F", FontSize = 13, Foreground = Theme.ThemedBrush(this, "DeepTextBrush") }
        };
        editButton.Click += (_, _) => EditPreset(preset);
        Grid.SetColumn(editButton, 1);
        grid.Children.Add(editButton);

        var deleteButton = new Button
        {
            Style = (Style)Application.Current.Resources["CircleButtonStyle"],
            Width = 38,
            Height = 38,
            VerticalAlignment = VerticalAlignment.Center,
            Background = Theme.ThemedBrush(this, "CardSubtleFillBrush"),
            Content = new FontIcon { Glyph = "\uE74D", FontSize = 13, Foreground = Theme.ThemedBrush(this, "RemoveCoralBrush") }
        };
        deleteButton.Click += async (_, _) => await ConfirmDeleteAsync(preset);
        Grid.SetColumn(deleteButton, 2);
        grid.Children.Add(deleteButton);

        card.Content = grid;
        return card;
    }

    private StackPanel MakeTitleRow(KnowledgePreset preset, bool isCurrent)
    {
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        titleRow.Children.Add(new TextBlock
        {
            Text = DisplayName(preset),
            Style = (Style)Application.Current.Resources["HeadlineStyle"],
            FontSize = 18,
            Foreground = Theme.ThemedBrush(this, "DeepTextBrush"),
            TextWrapping = TextWrapping.Wrap
        });
        if (isCurrent)
        {
            titleRow.Children.Add(new Border
            {
                Style = (Style)Application.Current.Resources["TagPillBorderStyle"],
                Padding = new Thickness(8, 3, 8, 3),
                Child = new TextBlock
                {
                    Text = "当前",
                    Style = (Style)Application.Current.Resources["TagTextStyle"],
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = Theme.ThemedBrush(this, "SkyBrush")
                }
            });
        }

        return titleRow;
    }

    private async System.Threading.Tasks.Task ConfirmSwitchAsync(KnowledgePreset preset)
    {
        var dialog = new ContentDialog
        {
            Title = "切换预设？",
            Content = "将切换到另一套知识点。当前预设的学习进度会被保留。",
            PrimaryButtonText = "确认切换",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };
        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        if (AppSession.Current.SwitchPreset(preset.Id))
        {
            Toast.Show(Localization.PresetSwitchedToast(DisplayName(preset)));
        }
        else
        {
            Toast.Show("预设解析失败，请稍后再试");
        }

        RefreshUi();
    }

    private async System.Threading.Tasks.Task ConfirmDeleteAsync(KnowledgePreset preset)
    {
        var dialog = new ContentDialog
        {
            Title = "删除预设？",
            Content = "删除后将移除该预设的所有知识点、重点集锦、已掌握状态和学习记录。",
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

        switch (AppSession.Current.DeletePreset(preset.Id))
        {
            case PresetDeleteOutcome.Deleted:
                Toast.Show(Localization.PresetDeletedToast(DisplayName(preset)));
                break;
            case PresetDeleteOutcome.BlockedLastPreset:
                Toast.Show("至少需要保留一个预设");
                break;
            case PresetDeleteOutcome.NotFound:
                Toast.Show("预设不存在");
                break;
        }

        RefreshUi();
    }

    private void EditPreset(KnowledgePreset preset)
    {
        AppSession.Current.PendingPresetId = preset.Id;
        MainWindow.Navigate("editPreset");
    }

    private void OnUploadNewPresetClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("newPreset");
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        MainWindow.GoBack();
    }
}
