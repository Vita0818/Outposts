using Kikaria.App.Controls;
using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Kikaria.App.Pages;

/// <summary>
/// 设置页(对齐 Apple 版 SettingsView):
/// 每日目标 1-100、倒数日起止、安全线 1-100、通知开关(Windows 上仅应用内占位,未接系统通知)、
/// 编辑资料入口、新手引导重放、Markdown 格式、隐私政策、版权与备案号、版本号。
/// </summary>
public sealed partial class SettingsPage : Page
{
    private bool _suppressToggleEvents;

    public SettingsPage()
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
        var state = session.CurrentState;

        AvatarInitialText.Text = session.AvatarInitial();
        DisplayNameText.Text = session.State.UserProfile.DisplayName;
        HandleText.Text = "@" + session.State.UserProfile.UserHandle;
        VersionValueText.Text = Theme.VersionText;

        DailyGoalValueText.Text = state.DailyGoal.ToString();
        DangerValueText.Text = state.DangerPercent + "%";
        CountdownValueText.Text = session.CountdownDayCount is { } days
            ? Localization.CountdownText(days)
            : "未设置";

        _suppressToggleEvents = true;
        NotificationsToggle.IsOn = state.NotificationsEnabled;
        _suppressToggleEvents = false;
        NotificationTimeSection.Visibility = state.NotificationsEnabled ? Visibility.Visible : Visibility.Collapsed;
        NotificationTimeValueText.Text = state.NotificationTime.ToString("HH:mm");
        CountdownMissingText.Visibility =
            state.NotificationsEnabled && (state.CountdownStartDate is null || state.CountdownEndDate is null)
                ? Visibility.Visible
                : Visibility.Collapsed;

        RefreshDangerStatus();
    }

    /// <summary>危险线状态:仅返回 bool 并在此展示,不接系统通知。</summary>
    private void RefreshDangerStatus()
    {
        var warning = StudyLogic.EvaluateStudyProgressWarning(AppSession.Current.CurrentState);
        DangerStatusText.Text = warning is null
            ? "学习进度正常，暂无安全线提醒。"
            : "学习进度低于安全线：已掌握 " + warning.MasteredCount + " / 预期 " + warning.ExpectedMasteredCount +
              "(安全线 " + warning.DangerPercent + "%，剩余 " + (warning.RemainingDays?.ToString() ?? "--") + " 天)。";
    }

    private void CloseAllPanels()
    {
        DailyGoalPanel.Visibility = Visibility.Collapsed;
        CountdownPanel.Visibility = Visibility.Collapsed;
        DangerPanel.Visibility = Visibility.Collapsed;
        NotificationTimePanel.Visibility = Visibility.Collapsed;
    }

    // ------------------------------------------------------------------
    // 每日学习目标
    // ------------------------------------------------------------------

    private void OnDailyGoalRowClick(object sender, RoutedEventArgs e)
    {
        var wasOpen = DailyGoalPanel.Visibility == Visibility.Visible;
        CloseAllPanels();
        if (wasOpen)
        {
            return;
        }

        DailyGoalPicker.SetItems(WheelPicker.Numbers(1, 100, " 个"), AppSession.Current.CurrentState.DailyGoal - 1);
        DailyGoalPanel.Visibility = Visibility.Visible;
    }

    private void OnDailyGoalDoneClick(object sender, RoutedEventArgs e)
    {
        var value = DailyGoalPicker.SelectedIndex + 1;
        AppSession.Current.UpdateDailyGoal(value);
        RefreshUi();
        CloseAllPanels();
    }

    // ------------------------------------------------------------------
    // 倒数日
    // ------------------------------------------------------------------

    private void OnCountdownRowClick(object sender, RoutedEventArgs e)
    {
        var wasOpen = CountdownPanel.Visibility == Visibility.Visible;
        CloseAllPanels();
        if (wasOpen)
        {
            return;
        }

        var state = AppSession.Current.CurrentState;
        var start = state.CountdownStartDate ?? DateTime.Today;
        var end = state.CountdownEndDate ?? state.CountdownStartDate ?? DateTime.Today;
        CountdownErrorText.Visibility = Visibility.Collapsed;

        FillDatePickers(StartYearPicker, StartMonthPicker, StartDayPicker, start);
        FillDatePickers(EndYearPicker, EndMonthPicker, EndDayPicker, end);
        CountdownPanel.Visibility = Visibility.Visible;
    }

    private static void FillDatePickers(WheelPicker year, WheelPicker month, WheelPicker day, DateTime date)
    {
        year.SetItems(WheelPicker.Numbers(2000, 2100, " 年"), date.Year - 2000);
        month.SetItems(WheelPicker.Numbers(1, 12, " 月"), date.Month - 1);
        day.SetItems(WheelPicker.Numbers(1, 31, " 日"), date.Day - 1);
    }

    private static DateTime ReadDatePickers(WheelPicker year, WheelPicker month, WheelPicker day)
    {
        var y = Math.Clamp(year.SelectedIndex + 2000, 1, 9999);
        var m = Math.Clamp(month.SelectedIndex + 1, 1, 12);
        var d = Math.Clamp(day.SelectedIndex + 1, 1, 31);
        d = Math.Min(d, DateTime.DaysInMonth(y, m));
        return new DateTime(y, m, d);
    }

    private void OnCountdownDoneClick(object sender, RoutedEventArgs e)
    {
        var start = ReadDatePickers(StartYearPicker, StartMonthPicker, StartDayPicker);
        var end = ReadDatePickers(EndYearPicker, EndMonthPicker, EndDayPicker);

        if (end.Date < start.Date)
        {
            CountdownErrorText.Visibility = Visibility.Visible;
            Toast.Show("结束日期不能早于开始日期");
            return;
        }

        AppSession.Current.UpdateCountdown(start, end);
        RefreshUi();
        CloseAllPanels();
    }

    private void OnCountdownClearClick(object sender, RoutedEventArgs e)
    {
        AppSession.Current.UpdateCountdown(null, null);
        CountdownErrorText.Visibility = Visibility.Collapsed;
        RefreshUi();
        CloseAllPanels();
    }

    // ------------------------------------------------------------------
    // 进度安全线
    // ------------------------------------------------------------------

    private void OnDangerRowClick(object sender, RoutedEventArgs e)
    {
        var wasOpen = DangerPanel.Visibility == Visibility.Visible;
        CloseAllPanels();
        if (wasOpen)
        {
            return;
        }

        DangerPicker.SetItems(WheelPicker.Numbers(1, 100, " %"), AppSession.Current.CurrentState.DangerPercent - 1);
        DangerPanel.Visibility = Visibility.Visible;
    }

    private void OnDangerDoneClick(object sender, RoutedEventArgs e)
    {
        AppSession.Current.UpdateDangerPercent(DangerPicker.SelectedIndex + 1);
        RefreshUi();
        CloseAllPanels();
    }

    // ------------------------------------------------------------------
    // 通知(Windows 版未接系统通知,仅保存设置)
    // ------------------------------------------------------------------

    private void OnNotificationsToggled(object sender, RoutedEventArgs e)
    {
        if (_suppressToggleEvents)
        {
            return;
        }

        AppSession.Current.UpdateNotificationsEnabled(NotificationsToggle.IsOn);
        RefreshUi();
    }

    private void OnNotificationTimeRowClick(object sender, RoutedEventArgs e)
    {
        var wasOpen = NotificationTimePanel.Visibility == Visibility.Visible;
        CloseAllPanels();
        if (wasOpen)
        {
            return;
        }

        var time = AppSession.Current.CurrentState.NotificationTime;
        NotifyHourPicker.SetItems(WheelPicker.Numbers(0, 23, " 时"), time.Hour);
        NotifyMinutePicker.SetItems(WheelPicker.Numbers(0, 59, " 分"), time.Minute);
        NotificationTimePanel.Visibility = Visibility.Visible;
    }

    private void OnNotificationTimeDoneClick(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        var time = new DateTime(now.Year, now.Month, now.Day,
            NotifyHourPicker.SelectedIndex, NotifyMinutePicker.SelectedIndex, 0);
        AppSession.Current.UpdateNotificationTime(time);
        RefreshUi();
        CloseAllPanels();
    }

    // ------------------------------------------------------------------
    // 帮助 / 关于
    // ------------------------------------------------------------------

    private void OnOnboardingRowClick(object sender, RoutedEventArgs e)
    {
        AppSession.Current.OnboardingReplay = true;
        MainWindow.Navigate("onboarding");
    }

    private void OnMarkdownGuideRowClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("markdownGuide");
    }

    private async void OnPrivacyRowClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "隐私政策",
            Content = "Kikaria 当前仅在本机保存你的学习资料、预设、头像和学习进度。学习进度通知使用 iOS 本地通知，不会上传到服务器。",
            PrimaryButtonText = "知道了",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void OnEditProfileClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("editProfile");
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (MainWindow.Instance is not null && MainWindow.Instance.RootFrame.CanGoBack)
        {
            MainWindow.GoBack();
        }
        else
        {
            MainWindow.Navigate("home", clearHistory: true);
        }
    }
}
