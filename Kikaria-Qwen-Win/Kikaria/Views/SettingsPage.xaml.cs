using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Kikaria.ViewModels;

namespace Kikaria.Views;

public sealed partial class SettingsPage : Page
{
    private MainViewModel VM => App.MainViewModel;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
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
        DisplayNameText.Text = VM.DisplayName;
        UserHandleText.Text = VM.UserHandle;
        PresetNameText.Text = VM.CurrentPresetName;
        VersionText.Text = $"Version {VM.VersionString}";

        DailyGoalPicker.Value = VM.DailyGoal;
        DangerPicker.Value = VM.DangerPercent;

        NotificationToggle.IsOn = VM.NotificationsEnabled;
        NotificationTimePicker.Time = VM.NotificationTime.TimeOfDay;

        StartDateText.Text = VM.CountdownStartDate?.ToString("yyyy-MM-dd") ?? "未设置";
        EndDateText.Text = VM.CountdownEndDate?.ToString("yyyy-MM-dd") ?? "未设置";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        ShowProfileEditDialog();
    }

    private async void ShowProfileEditDialog()
    {
        var dialog = new ContentDialog
        {
            Title = "编辑个人资料",
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            XamlRoot = XamlRoot
        };

        var nameBox = new TextBox
        {
            Header = "显示名称",
            Text = VM.DisplayName,
            PlaceholderText = "输入显示名称",
            Margin = new Thickness(0, 0, 0, 12)
        };

        var handleBox = new TextBox
        {
            Header = "用户标识",
            Text = VM.UserHandle.TrimStart('@'),
            PlaceholderText = "输入用户标识"
        };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(nameBox);
        panel.Children.Add(handleBox);
        dialog.Content = panel;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            VM.UserProfile.DisplayName = nameBox.Text;
            VM.UserProfile.UserHandle = handleBox.Text;
            VM.NotifyProfileChanged();
            UpdateDisplay();
        }
    }

    private void DailyGoalPicker_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (args.NewValue >= 1 && args.NewValue <= 100)
        {
            VM.UpdateDailyGoal((int)args.NewValue);
        }
    }

    private void DangerPicker_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (args.NewValue >= 0 && args.NewValue <= 100)
        {
            VM.UpdateDangerPercent((int)args.NewValue);
        }
    }

    private async void StartDate_Click(object sender, RoutedEventArgs e)
    {
        var picker = new DatePicker
        {
            Date = VM.CountdownStartDate ?? DateTime.Today
        };

        var dialog = new ContentDialog
        {
            Title = "选择开始日期",
            Content = picker,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            VM.UpdateCountdownRange(picker.Date.DateTime, VM.CountdownEndDate);
            StartDateText.Text = picker.Date.DateTime.ToString("yyyy-MM-dd");
        }
    }

    private async void EndDate_Click(object sender, RoutedEventArgs e)
    {
        var picker = new DatePicker
        {
            Date = VM.CountdownEndDate ?? DateTime.Today.AddDays(30)
        };

        var dialog = new ContentDialog
        {
            Title = "选择结束日期",
            Content = picker,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            VM.UpdateCountdownRange(VM.CountdownStartDate, picker.Date.DateTime);
            EndDateText.Text = picker.Date.DateTime.ToString("yyyy-MM-dd");
        }
    }

    private void NotificationToggle_Toggled(object sender, RoutedEventArgs e)
    {
        VM.UpdateNotificationsEnabled(NotificationToggle.IsOn);
    }

    private void NotificationTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        if (e.NewTime.HasValue)
        {
            VM.UpdateNotificationTime(DateTime.Today.Add(e.NewTime.Value));
        }
    }

    private void TestNotif_Click(object sender, RoutedEventArgs e)
    {
        VM.ShowToastMessage("测试通知已发送");
    }

    private void Onboarding_Click(object sender, RoutedEventArgs e)
    {
        VM.HasCompletedOnboarding = false;
        VM.ShowToastMessage("引导将在下次启动时重新播放");
    }

    private async void MarkdownGuide_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Markdown 格式指南",
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot,
            Content = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = "# 标题\n\n每个知识点以 # 标题开始\n\n" +
                           "tags: 标签1, 标签2\n\n" +
                           "hint:\n提示信息，帮助回忆\n\n" +
                           "content:\n完整答案内容\n\n" +
                           "---\n\n用 --- 分隔不同知识点\n\n" +
                           "支持 LaTeX 数学公式：$E=mc^2$",
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new FontFamily("Cascadia Code, Consolas, monospace"),
                    FontSize = 14,
                    LineHeight = 24
                }
            }
        };
        await dialog.ShowAsync();
    }

    private async void Privacy_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "隐私政策",
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot,
            Content = new TextBlock
            {
                Text = "Kikaria 当前仅在本机保存你的学习资料、预设、头像和学习进度。学习进度通知使用 Windows 本地通知，不会上传到服务器。",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 15,
                FontFamily = new FontFamily("Microsoft YaHei, PingFang SC, sans-serif")
            }
        };
        await dialog.ShowAsync();
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool isWide = e.NewSize.Width >= 900;
        ContentRoot.MaxWidth = isWide ? 1100 : 700;

        if (isWide)
        {
            ProfileCol.Width = new GridLength(1, GridUnitType.Star);
            SettingsCol.Width = new GridLength(1, GridUnitType.Star);
            ProfileColumn.Margin = new Thickness(0, 0, 8, 0);
            SettingsColumn.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(SettingsColumn, 1);
        }
        else
        {
            ProfileCol.Width = new GridLength(1, GridUnitType.Star);
            SettingsCol.Width = new GridLength(0, GridUnitType.Pixel);
            ProfileColumn.Margin = new Thickness(0);
            SettingsColumn.Margin = new Thickness(0);
            Grid.SetColumn(SettingsColumn, 0);
        }
        SettingsColumn.Visibility = Visibility.Visible;
    }
}
