using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Kikaria.App.Pages;

/// <summary>范围选择:标签多选 + 搜索(对齐 Apple 版 ScopeSelectionView)。</summary>
public sealed partial class ScopePage : Page
{
    private List<string> _allTags = new();

    public ScopePage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _allTags = AppSession.Current.AllTags();
        RefreshUi();
    }

    private void RefreshUi()
    {
        var session = AppSession.Current;
        var selected = session.CurrentState.SelectedTags;
        SummaryText.Text = Localization.SelectedTagsSummary(selected.Count == 0, selected.Count);

        var query = SearchBox.Text.Trim();
        List<string> visibleTags;
        if (query.Length == 0)
        {
            visibleTags = _allTags;
        }
        else
        {
            // 与 Apple 版一致:标签本身匹配,或任何匹配知识点的标签都保留。
            var relevantTags = new HashSet<string>(
                session.Points
                    .Where(point => StudyLogic.MatchesSearchQuery(point, query))
                    .SelectMany(point => point.Tags));

            visibleTags = _allTags
                .Where(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase) || relevantTags.Contains(tag))
                .ToList();
        }

        ClearSearchButton.Visibility = query.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyTagsCard.Visibility = visibleTags.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        TagsGrid.Children.Clear();
        TagsGrid.RowDefinitions.Clear();
        TagsGrid.ColumnDefinitions.Clear();
        TagsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        TagsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        const int spacing = 10;
        for (var i = 0; i < visibleTags.Count; i += 2)
        {
            var row = new RowDefinition { Height = GridLength.Auto };
            TagsGrid.RowDefinitions.Add(row);
            var rowIndex = TagsGrid.RowDefinitions.Count - 1;

            TagsGrid.Children.Add(MakeTagChip(visibleTags[i], rowIndex, 0));
            if (i + 1 < visibleTags.Count)
            {
                TagsGrid.Children.Add(MakeTagChip(visibleTags[i + 1], rowIndex, 1));
            }
        }

        TagsGrid.RowSpacing = spacing;
        TagsGrid.ColumnSpacing = spacing;
    }

    private Button MakeTagChip(string tag, int row, int column)
    {
        var isSelected = AppSession.Current.CurrentState.SelectedTags.Contains(tag);

        var button = new Button
        {
            Style = (Style)Application.Current.Resources["ActionButtonStyle"],
            MinHeight = 54,
            CornerRadius = new CornerRadius(20),
            FontSize = 13,
            Content = new TextBlock
            {
                Text = tag,
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 2,
                TextAlignment = TextAlignment.Center
            },
            Background = isSelected
                ? Theme.ThemedBrush(this, "ActionGradientBrush")
                : Theme.ThemedBrush(this, "CardSubtleFillBrush"),
            Foreground = isSelected
                ? new SolidColorBrush(Windows.UI.Colors.White)
                : Theme.ThemedBrush(this, "DeepTextBrush")
        };
        Grid.SetRow(button, row);
        Grid.SetColumn(button, column);
        button.Click += (_, _) => ToggleTag(tag);
        return button;
    }

    private void ToggleTag(string tag)
    {
        var selected = AppSession.Current.CurrentState.SelectedTags;
        if (!selected.Remove(tag))
        {
            selected.Add(tag);
        }

        AppSession.Current.UpdateSelectedTags(selected);
        RefreshUi();
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

    private void OnDoneClick(object sender, RoutedEventArgs e)
    {
        if (MainWindow.Instance is not null && MainWindow.Instance.RootFrame.CanGoBack)
        {
            MainWindow.GoBack();
        }
        else
        {
            MainWindow.Navigate("home");
        }
    }
}
