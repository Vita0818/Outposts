using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace Kikaria.App.Pages;

/// <summary>首页:品牌标题 + 开始背诵气泡 + 今日进度卡 + 仪表卡(对齐 Apple 版首页单列布局)。</summary>
public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefreshState();
    }

    private void RefreshState()
    {
        var session = AppSession.Current;
        session.EnsureLoaded();
        var state = session.CurrentState;
        var now = DateTime.Now;

        AvatarInitialText.Text = session.AvatarInitial();
        HomeDateText.Text = Localization.HomeDateTitle(now);
        HomeDaysLeftText.Text = Localization.DaysLeftText(session.CountdownDayCount);

        var todayMarked = StudyLogic.TodayMarkedMasteredCount(state.ActivityRecords, now);
        HomeProgressText.Text = todayMarked + "/" + state.DailyGoal;

        var allTags = session.AllTags();
        ScopeCountText.Text = state.SelectedTags.Count == 0
            ? allTags.Count.ToString()
            : state.SelectedTags.Count.ToString();
        ReinforcedCountText.Text = session.ReinforcedCount.ToString();
        MasteredCountText.Text = session.MasteredCount.ToString();
        PresetNameText.Text = Localization.BuiltInPresetDisplayName(session.CurrentPresetName, session.CurrentPreset.IsBuiltIn);
    }

    private void OnAvatarClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("settings");
    }

    private void OnStartReviewClick(object sender, RoutedEventArgs e)
    {
        AppSession.Current.PendingReviewMode = ReviewMode.Normal;
        MainWindow.Navigate("review");
    }

    private void OnProgressCardClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("today");
    }

    private void OnScopeClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("scope");
    }

    private void OnReinforcementClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("reinforcement");
    }

    private void OnMasteredClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("mastered");
    }

    private void OnPresetRowClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("presetSelection");
    }
}
