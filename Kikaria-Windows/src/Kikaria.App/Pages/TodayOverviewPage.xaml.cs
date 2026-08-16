using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace Kikaria.App.Pages;

/// <summary>今日概览(对齐 Apple 版 TodayOverviewView)。</summary>
public sealed partial class TodayOverviewPage : Page
{
    public TodayOverviewPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var session = AppSession.Current;
        var state = session.CurrentState;
        var records = state.ActivityRecords;
        var now = DateTime.Now;

        PresetNameText.Text = Localization.BuiltInPresetDisplayName(session.CurrentPresetName, session.CurrentPreset.IsBuiltIn);

        var marked = StudyLogic.TodayMarkedMasteredCount(records, now);
        MarkedCountText.Text = marked.ToString();
        GoalText.Text = "/ " + state.DailyGoal;

        var reviewed = StudyLogic.TodayReviewedAnswerCount(records, now);
        var viewedHint = StudyLogic.TodayViewedHintCount(records, now);

        ProgressMessageText.Text = Localization.ProgressMessage(marked, reviewed, state.DailyGoal);
        ReviewedCountText.Text = reviewed.ToString();
        MasteredTotalText.Text = session.MasteredCount.ToString();
        HintCountText.Text = viewedHint.ToString();
        CountdownText.Text = Localization.CountdownText(session.CountdownDayCount);
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        MainWindow.GoBack();
    }

    private void OnOpenHistoryClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("history");
    }
}
