using Kikaria.App.Controls;
using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Kikaria.App.Pages;

/// <summary>
/// 复习页:标题 + 标签 + 今日次数,查看提示 / 查看答案,
/// 答案后按 normal / reinforcement / mastered 三模式显示动作网格,
/// 队列 shuffle 且首位避免与上一点相同(对齐 Apple 版 ReviewView)。
/// </summary>
public sealed partial class ReviewPage : Page
{
    private ReviewMode _mode = ReviewMode.Normal;
    private List<Guid> _queue = new();
    private int _queueIndex;
    private Guid? _currentPointId;
    private Guid? _lastQueuePointId;
    private bool _showHint;
    private bool _showContent;
    private List<Guid> _matchingIds = new();

    public ReviewPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var previousPointId = _currentPointId;
        _mode = AppSession.Current.PendingReviewMode;
        RefreshMatching();
        RebuildQueue(previousPointId);
        UpdateUi();
    }

    private AppSession Session => AppSession.Current;

    private KnowledgePoint? CurrentPoint
    {
        get
        {
            if (_currentPointId is null)
            {
                return null;
            }

            return Session.Points.FirstOrDefault(point => point.Id == _currentPointId.Value);
        }
    }

    // ------------------------------------------------------------------
    // 队列
    // ------------------------------------------------------------------

    private void RefreshMatching()
    {
        _matchingIds = StudyLogic.MatchingPointIds(Session.Points, Session.CurrentState.SelectedTags, _mode);
    }

    private void RebuildQueue(Guid? avoidFirstId)
    {
        RefreshMatching();
        _queue = StudyLogic.BuildShuffledQueue(_matchingIds, avoidFirstId);
        _queueIndex = 0;
        _lastQueuePointId = avoidFirstId ?? _lastQueuePointId;

        if (_queue.Count == 0)
        {
            _currentPointId = null;
            ResetRevealState();
            return;
        }

        SetCurrentFromQueue(0);
    }

    private void SetCurrentFromQueue(int index)
    {
        if (index < 0 || index >= _queue.Count)
        {
            RebuildQueue(_lastQueuePointId);
            return;
        }

        _queueIndex = index;
        _currentPointId = _queue[index];
        _lastQueuePointId = _currentPointId;
        ResetRevealState();
    }

    private void ReconcileQueue()
    {
        var validIds = new HashSet<Guid>(_matchingIds);
        _queue = StudyLogic.ReconcileQueue(_queue, validIds);

        if (_currentPointId is not null)
        {
            var currentIndex = _queue.IndexOf(_currentPointId.Value);
            if (currentIndex >= 0)
            {
                _queueIndex = currentIndex;
                return;
            }
        }

        if (_queueIndex >= _queue.Count)
        {
            _queueIndex = Math.Max(0, _queue.Count - 1);
        }
    }

    private void MoveToNext()
    {
        ReconcileQueue();
        if (_queue.Count == 0)
        {
            RebuildQueue(_lastQueuePointId);
            return;
        }

        int nextIndex;
        if (_currentPointId is not null)
        {
            var currentIndex = _queue.IndexOf(_currentPointId.Value);
            nextIndex = currentIndex >= 0 ? currentIndex + 1 : _queueIndex;
        }
        else
        {
            nextIndex = _queueIndex;
        }

        if (nextIndex < _queue.Count)
        {
            SetCurrentFromQueue(nextIndex);
        }
        else
        {
            RebuildQueue(_currentPointId ?? _lastQueuePointId);
        }
    }

    private void MoveToPrevious()
    {
        ReconcileQueue();
        if (_queue.Count == 0)
        {
            RebuildQueue(_lastQueuePointId);
            return;
        }

        if (_currentPointId is not null)
        {
            var currentIndex = _queue.IndexOf(_currentPointId.Value);
            if (currentIndex >= 0)
            {
                _queueIndex = currentIndex;
            }
        }

        if (_queue.Count == 1)
        {
            SetCurrentFromQueue(0);
            return;
        }

        var previousIndex = _queueIndex > 0 ? _queueIndex - 1 : _queue.Count - 1;
        SetCurrentFromQueue(previousIndex);
    }

    private void ResetRevealState()
    {
        _showHint = false;
        _showContent = false;
    }

    // ------------------------------------------------------------------
    // UI
    // ------------------------------------------------------------------

    private void UpdateUi()
    {
        var point = CurrentPoint;
        var isEmptyCollection = _matchingIds.Count == 0 && point is null;

        if (isEmptyCollection)
        {
            ContentPanel.Visibility = Visibility.Collapsed;
            RevealPanel.Visibility = Visibility.Collapsed;
            ActionPanel.Visibility = Visibility.Collapsed;
            PreviousButton.IsEnabled = false;
            ScopeButton.IsEnabled = _mode == ReviewMode.Normal;

            if (_mode == ReviewMode.Reinforcement || _mode == ReviewMode.Mastered)
            {
                CompletionPanel.Visibility = Visibility.Visible;
                EmptyStateCard.Visibility = Visibility.Collapsed;
            }
            else
            {
                CompletionPanel.Visibility = Visibility.Collapsed;
                EmptyStateCard.Visibility = Visibility.Visible;
            }

            return;
        }

        CompletionPanel.Visibility = Visibility.Collapsed;
        EmptyStateCard.Visibility = Visibility.Collapsed;
        ContentPanel.Visibility = point is null ? Visibility.Collapsed : Visibility.Visible;
        PreviousButton.IsEnabled = true;
        ScopeButton.IsEnabled = _mode == ReviewMode.Normal;

        if (point is null)
        {
            return;
        }

        TitleText.Text = point.Title;

        TagsPanel.Children.Clear();
        foreach (var tag in point.Tags)
        {
            var pill = new Border
            {
                Style = (Style)Application.Current.Resources["TagPillBorderStyle"],
                Child = new TextBlock
                {
                    Text = tag,
                    Style = (Style)Application.Current.Resources["TagTextStyle"],
                    Foreground = Theme.ThemedBrush(this, "SoftTextBrush")
                }
            };
            TagsPanel.Children.Add(pill);
        }

        TodayPillText.Text = Localization.TodayReviewCount(Session.TodayReviewCountFor(point.Id));

        HintCard.Visibility = _showHint ? Visibility.Visible : Visibility.Collapsed;
        AnswerCard.Visibility = _showContent ? Visibility.Visible : Visibility.Collapsed;
        if (_showHint)
        {
            HintMath.Text = point.Hint;
        }

        if (_showContent)
        {
            AnswerMath.Text = point.Content;
        }

        RevealPanel.Visibility = _showContent ? Visibility.Collapsed : Visibility.Visible;
        RevealHintButton.IsEnabled = !_showHint;
        RevealHintButton.Opacity = _showHint ? 0.4 : 1.0;
        ActionPanel.Visibility = _showContent ? Visibility.Visible : Visibility.Collapsed;
        if (_showContent)
        {
            BuildActionButtons(point);
        }
    }

    private void BuildActionButtons(KnowledgePoint point)
    {
        ActionPanel.Children.Clear();

        Button MakeAction(string title, string glyph, string backgroundKey, Brush foreground, RoutedEventHandler onClick)
        {
            var button = new Button
            {
                Style = (Style)Application.Current.Resources["ActionButtonStyle"],
                Background = Theme.ThemedBrush(this, backgroundKey),
                Foreground = foreground,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children =
                    {
                        new FontIcon { Glyph = glyph, FontSize = 16 },
                        new TextBlock { Text = title }
                    }
                }
            };
            button.Click += onClick;
            return button;
        }

        var white = new SolidColorBrush(Windows.UI.Color.FromArgb(245, 255, 255, 255));

        if (_mode == ReviewMode.Reinforcement)
        {
            ActionPanel.Children.Add(MakeAction(
                "移出重点集锦", "\uE738", "RemoveGradientBrush", white,
                (_, _) => RemoveCurrentFromReinforcementAndAdvance()));
            ActionPanel.Children.Add(MakeMasteredButton(point));
        }
        else if (_mode == ReviewMode.Mastered)
        {
            ActionPanel.Children.Add(MakeAction(
                Localization.AddFocusButtonTitle(point.ReinforcementCount), "\uE710", "ActionGradientBrush", white,
                (_, _) => AddCurrentToReinforcementAndAdvance()));
            ActionPanel.Children.Add(MakeAction(
                "移出已掌握", "\uE738", "RemoveGradientBrush", white,
                (_, _) => RemoveCurrentFromMasteredAndAdvance()));
        }
        else
        {
            ActionPanel.Children.Add(MakeAction(
                Localization.AddFocusButtonTitle(point.ReinforcementCount), "\uE710", "ActionGradientBrush", white,
                (_, _) => AddCurrentToReinforcementAndAdvance()));
            ActionPanel.Children.Add(MakeMasteredButton(point));
        }

        ActionPanel.Children.Add(MakeAction(
            "下一个", "\uE8B1", "NextGradientBrush", white,
            (_, _) => MoveToNextUi()));
    }

    private Button MakeMasteredButton(KnowledgePoint point)
    {
        var mastered = point.IsMastered;
        var button = new Button
        {
            Style = (Style)Application.Current.Resources["ActionButtonStyle"],
            Background = mastered
                ? Theme.ThemedBrush(this, "CardSubtleFillBrush")
                : Theme.ThemedBrush(this, "MasteredActionGradientBrush"),
            Foreground = mastered ? Theme.ThemedBrush(this, "SoftTextBrush") : new SolidColorBrush(Windows.UI.Color.FromArgb(245, 255, 255, 255)),
            IsEnabled = !mastered,
            Opacity = mastered ? 0.88 : 1.0,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Children =
                {
                    new FontIcon { Glyph = mastered ? "\uE73E" : "\uE710", FontSize = 15 },
                    new TextBlock { Text = mastered ? "已设定为掌握" : "加入已掌握" }
                }
            }
        };
        button.Click += (_, _) => MarkCurrentAsMasteredAndAdvance();
        return button;
    }

    // ------------------------------------------------------------------
    // 事件
    // ------------------------------------------------------------------

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        MainWindow.GoBack();
    }

    private void OnPreviousClick(object sender, RoutedEventArgs e)
    {
        MoveToPrevious();
        UpdateUi();
    }

    private void OnScopeClick(object sender, RoutedEventArgs e)
    {
        if (_mode != ReviewMode.Normal)
        {
            return;
        }

        MainWindow.Navigate("scope");
    }

    private void OnReturnHomeClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("home", clearHistory: true);
    }

    private void OnRevealHintClick(object sender, RoutedEventArgs e)
    {
        if (_showHint)
        {
            return;
        }

        var point = CurrentPoint;
        if (point is not null)
        {
            Session.RecordActivity(StudyActivityType.ViewedHint, point);
        }

        _showHint = true;
        UpdateUi();
    }

    private void OnRevealAnswerClick(object sender, RoutedEventArgs e)
    {
        if (_showContent)
        {
            return;
        }

        var point = CurrentPoint;
        if (point is null)
        {
            return;
        }

        _showContent = true;
        Session.IncrementTodayReviewCount(point.Id);
        Session.RecordActivity(StudyActivityType.ReviewedAnswer, point);
        UpdateUi();
    }

    private void MoveToNextUi()
    {
        MoveToNext();
        UpdateUi();
    }

    // ------------------------------------------------------------------
    // 动作(加入重点 / 掌握 / 移出),全部与 Apple 版语义一致
    // ------------------------------------------------------------------

    private void AddCurrentToReinforcementAndAdvance()
    {
        var point = CurrentPoint;
        if (point is null)
        {
            return;
        }

        var newCount = point.AddReinforcement(DateTime.Now);
        Session.RecordActivity(StudyActivityType.AddedReinforcement, point);
        Toast.Show(Localization.AddedFocusToast(point.Title, newCount));
        MoveToNextUi();
    }

    private void MarkCurrentAsMasteredAndAdvance()
    {
        var point = CurrentPoint;
        if (point is null || point.IsMastered)
        {
            return;
        }

        point.MarkMastered(DateTime.Now);
        Session.RecordActivity(StudyActivityType.MarkedMastered, point);
        Toast.Show(Localization.MasteredToast(point.Title));
        MoveToNextUi();
    }

    private void RemoveCurrentFromReinforcementAndAdvance()
    {
        var point = CurrentPoint;
        if (point is null || point.ReinforcementCount <= 0)
        {
            MoveToNextUi();
            return;
        }

        point.ClearReinforcement(DateTime.Now);
        Session.RecordActivity(StudyActivityType.RemovedReinforcement, point);
        Toast.Show(Localization.RemovedFocusToast(point.Title));
        MoveToNextUi();
    }

    private void RemoveCurrentFromMasteredAndAdvance()
    {
        var point = CurrentPoint;
        if (point is null || !point.IsMastered)
        {
            MoveToNextUi();
            return;
        }

        point.UnmarkMastered(DateTime.Now);
        Session.RecordActivity(StudyActivityType.RemovedMastered, point);
        Toast.Show(Localization.RemovedMasteredToast(point.Title));
        MoveToNextUi();
    }
}
