using Microsoft.UI.Xaml;

namespace Kikaria.App.Pages;

/// <summary>
/// 首次资料设置(对齐 Apple 版 InitialProfileSetupView)。
/// Windows 版头像用首字母圆代替照片;昵称必填,用户名可留空自动生成。
/// </summary>
public sealed partial class ProfileSetupPage : Page
{
    public ProfileSetupPage()
    {
        InitializeComponent();

        var profile = AppSession.Current.EnsureLoaded().UserProfile;
        if (profile.DisplayName != "Vita")
        {
            DisplayNameBox.Text = profile.DisplayName;
        }

        if (profile.UserHandle != "vita_0818")
        {
            UserHandleBox.Text = profile.UserHandle;
        }

        RefreshState();
    }

    private void RefreshState()
    {
        var canSave = DisplayNameBox.Text.Trim().Length > 0;
        StartButton.IsEnabled = canSave;
        StartButton.Opacity = canSave ? 1.0 : 0.48;

        var name = DisplayNameBox.Text.Trim();
        AvatarInitialText.Text = name.Length > 0 ? name.Substring(0, 1).ToUpperInvariant() : "V";
    }

    private void OnFieldChanged(object sender, TextChangedEventArgs e)
    {
        RefreshState();
    }

    private void OnStartClick(object sender, RoutedEventArgs e)
    {
        var displayName = DisplayNameBox.Text.Trim();
        if (displayName.Length == 0)
        {
            return;
        }

        var handle = UserHandleBox.Text.Trim().TrimStart('@');
        if (handle.Length == 0)
        {
            handle = GeneratedHandle(displayName);
        }

        AppSession.Current.CompleteProfileSetup(displayName, handle);
        MainWindow.Navigate("home", clearHistory: true);
    }

    /// <summary>与 Apple 版 generatedHandle 一致:非字母数字转 _,去首尾 _。</summary>
    private static string GeneratedHandle(string name)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var c in name.ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        var normalized = builder.ToString().Trim('_');
        return normalized.Length == 0 ? "kikaria_user" : normalized;
    }
}
