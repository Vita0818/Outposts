using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Kikaria.ViewModels;

namespace Kikaria.Views;

public sealed partial class HomePage : Page
{
    private MainViewModel VM => App.MainViewModel;

    public HomePage()
    {
        InitializeComponent();
        Loaded += HomePage_Loaded;
    }

    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateDisplay();
        OrbitStoryboard.Begin();
        BreatheStoryboard.Begin();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        DateText.Text = VM.HomeDateTitle;
        DaysLeftRun.Text = (VM.CountdownDayCount ?? 0).ToString();

        string progress = VM.HomeProgressText;
        var parts = progress.Split('/');
        if (parts.Length >= 2)
        {
            MasteredRun.Text = parts[0].Trim();
            var goalPart = parts[1].Split(' ')[0].Trim();
            GoalRun.Text = goalPart;
        }
        else
        {
            MasteredRun.Text = VM.MasteredCount.ToString();
            GoalRun.Text = VM.KnowledgePoints.Count.ToString();
        }

        ScopeCountText.Text = VM.KnowledgePoints.Count.ToString();
        ReinforcementCountText.Text = VM.ReinforcedCount.ToString();
        MasteredCountText.Text = VM.MasteredCount.ToString();
        PresetNameText.Text = VM.CurrentPreset?.Name ?? "未选择预设";
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        VM.ReviewMode = Models.ReviewMode.Normal;
        VM.BuildReviewQueue();
        Frame.Navigate(typeof(ReviewPage));
    }

    private void AvatarButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage));
    }

    private void NavigateScope_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ScopeSelectionPage));
    }

    private void NavigateReinforcement_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ReinforcementPage));
    }

    private void NavigateMastered_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MasteredPage));
    }

    private void NavigatePresetSelection_Click(object sender, RoutedEventArgs e)
    {
        VM.NavigateTo(Models.AppRoute.PresetSelection);
    }

    private void NavigateTodayOverview_Click(object sender, RoutedEventArgs e)
    {
        VM.NavigateTo(Models.AppRoute.TodayOverview);
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool isWide = e.NewSize.Width >= 800;
        WideNavRow.Visibility = isWide ? Visibility.Visible : Visibility.Collapsed;
        RootGrid.MaxWidth = isWide ? 1100 : 900;
    }
}
