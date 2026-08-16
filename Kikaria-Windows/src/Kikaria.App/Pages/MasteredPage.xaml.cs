using Kikaria.App.Controls;
using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Kikaria.App.Pages;

/// <summary>已掌握列表(对齐 Apple 版 MasteredView):搜索 + 卡片 + 移出已掌握。</summary>
public sealed partial class MasteredPage : Page
{
    public MasteredPage()
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
        var points = AppSession.Current.Points.Where(point => point.IsMastered).ToList();

        var query = SearchBox.Text.Trim();
        var filtered = query.Length == 0
            ? points
            : points.Where(point => StudyLogic.MatchesSearchQuery(point, query)).ToList();

        ClearSearchButton.Visibility = query.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyCard.Visibility = points.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        NoResultsCard.Visibility = points.Count > 0 && filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PointsPanel.Visibility = filtered.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        StartCountText.Text = points.Count.ToString();

        PointsPanel.Children.Clear();
        foreach (var point in filtered)
        {
            PointsPanel.Children.Add(MakePointCard(point));
        }
    }

    private GlassCard MakePointCard(KnowledgePoint point)
    {
        var card = new GlassCard { CardRadius = 22, ContentPadding = new Thickness(16) };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock
        {
            Text = point.Title,
            Style = (Style)Application.Current.Resources["SubHeadlineStyle"],
            FontSize = 16,
            Foreground = Theme.ThemedBrush(this, "DeepTextBrush"),
            TextWrapping = TextWrapping.Wrap
        });
        info.Children.Add(new TextBlock
        {
            Text = string.Join(", ", point.Tags),
            Style = (Style)Application.Current.Resources["TagTextStyle"],
            Foreground = Theme.ThemedBrush(this, "SoftTextBrush"),
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2
        });

        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        var removeButton = new Button
        {
            Style = (Style)Application.Current.Resources["CircleButtonStyle"],
            Width = 40,
            Height = 40,
            Background = Theme.ThemedBrush(this, "CardSubtleFillBrush"),
            Content = new FontIcon { Glyph = "\uE738", FontSize = 13, Foreground = Theme.ThemedBrush(this, "RemoveCoralBrush") }
        };
        removeButton.Click += (_, _) => RemoveFromMastered(point);
        Grid.SetColumn(removeButton, 1);
        grid.Children.Add(removeButton);

        card.Content = grid;
        return card;
    }

    private void RemoveFromMastered(KnowledgePoint point)
    {
        var live = AppSession.Current.Points.FirstOrDefault(p => p.Id == point.Id);
        if (live is null || !live.IsMastered)
        {
            return;
        }

        live.UnmarkMastered(DateTime.Now);
        AppSession.Current.RecordActivity(StudyActivityType.RemovedMastered, live);
        AppSession.Current.Save();
        Toast.Show(Localization.RemovedMasteredToast(live.Title));
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

    private void OnStartReviewClick(object sender, RoutedEventArgs e)
    {
        AppSession.Current.PendingReviewMode = ReviewMode.Mastered;
        MainWindow.Navigate("review");
    }
}
