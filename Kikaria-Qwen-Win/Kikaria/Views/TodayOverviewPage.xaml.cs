using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Kikaria.ViewModels;

namespace Kikaria.Views;

public sealed partial class TodayOverviewPage : Page
{
    private MainViewModel VM => App.MainViewModel;

    public TodayOverviewPage()
    {
        InitializeComponent();
        Loaded += TodayOverviewPage_Loaded;
    }

    private void TodayOverviewPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateDisplay();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        HintCountText.Text = VM.TodayViewedHintCount.ToString();
        AnswerCountText.Text = VM.TodayReviewedAnswerCount.ToString();
        MasteredCountText.Text = VM.TodayMarkedMasteredCount.ToString();
        ProgressText.Text = VM.HomeProgressText;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }
}
