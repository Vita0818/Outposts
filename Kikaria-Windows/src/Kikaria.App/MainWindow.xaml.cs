using Kikaria.App.Pages;
using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Kikaria.App;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    /// <summary>窗口原生句柄(FileOpenPicker / FileSavePicker 初始化用)。</summary>
    public static IntPtr WindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(Instance!);

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;

        Title = "Kikaria";

        // 手机版单列布局:起始 480x900。
        try
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(480, 900));
        }
        catch (Exception)
        {
            // 窗口尺寸尽力而为。
        }

        // 自定义标题栏融入背景渐变。
        try
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
        }
        catch (Exception)
        {
            // 标题栏扩展尽力而为。
        }

        // 窗口最小 420x760。
        try
        {
            var presenter = OverlappedPresenter;
            presenter.PreferredMinimumWidth = 420;
            presenter.PreferredMinimumHeight = 760;
        }
        catch (Exception)
        {
            // 最小尺寸限制在旧版 Windows App SDK 上可能不可用。
        }

        // 窗口关闭时保存状态。
        Closed += (_, _) => AppSession.Current.Save();

        // 启动路由:未完成引导 → 引导;未设置资料 → 资料设置;否则首页。
        var session = AppSession.Current.EnsureLoaded();
        if (!session.HasCompletedOnboarding)
        {
            Navigate("onboarding", clearHistory: true);
        }
        else if (!session.HasCompletedProfileSetup)
        {
            Navigate("profileSetup", clearHistory: true);
        }
        else
        {
            Navigate("home", clearHistory: true);
        }
    }

    /// <summary>路由字符串键 → 页面类型。</summary>
    public static Type? RouteToType(string route) => route switch
    {
        "onboarding" => typeof(OnboardingPage),
        "profileSetup" => typeof(ProfileSetupPage),
        "home" => typeof(HomePage),
        "review" => typeof(ReviewPage),
        "scope" => typeof(ScopePage),
        "today" => typeof(TodayOverviewPage),
        "history" => typeof(ReviewHistoryPage),
        "reinforcement" => typeof(ReinforcementPage),
        "mastered" => typeof(MasteredPage),
        "settings" => typeof(SettingsPage),
        "editProfile" => typeof(EditProfilePage),
        "presetSelection" => typeof(PresetSelectionPage),
        "newPreset" => typeof(NewPresetPage),
        "editPreset" => typeof(EditPresetPage),
        "editPoint" => typeof(EditKnowledgePointPage),
        "markdownGuide" => typeof(MarkdownGuidePage),
        _ => null
    };

    /// <summary>按路由键导航;clearHistory 时清空返回栈。</summary>
    public static void Navigate(string route, bool clearHistory = false)
    {
        var window = Instance;
        var pageType = RouteToType(route);
        if (window is null || pageType is null)
        {
            return;
        }

        if (clearHistory)
        {
            window.RootFrame.BackStack.Clear();
        }

        window.RootFrame.Navigate(pageType);
    }

    /// <summary>返回上一页。</summary>
    public static void GoBack()
    {
        var window = Instance;
        if (window is not null && window.RootFrame.CanGoBack)
        {
            window.RootFrame.GoBack();
        }
    }

    /// <summary>清空返回栈(用于引导完成后回首页)。</summary>
    public static void ClearBackStack()
    {
        Instance?.RootFrame.BackStack.Clear();
    }
}
