using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace Kikaria.App.Pages;

/// <summary>编辑资料(对齐 Apple 版 EditProfileView;头像为文字头像)。</summary>
public sealed partial class EditProfilePage : Page
{
    public EditProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var profile = AppSession.Current.EnsureLoaded().UserProfile;
        DisplayNameBox.Text = profile.DisplayName;
        UserHandleBox.Text = profile.UserHandle;
        RefreshAvatar();
    }

    private void RefreshAvatar()
    {
        var name = DisplayNameBox.Text.Trim();
        AvatarInitialText.Text = name.Length > 0 ? name.Substring(0, 1).ToUpperInvariant() : "V";
    }

    private void OnFieldChanged(object sender, TextChangedEventArgs e)
    {
        RefreshAvatar();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // 空值回落默认,与 Apple 版 saveProfile 一致。
        AppSession.Current.SaveProfile(DisplayNameBox.Text, UserHandleBox.Text);
        MainWindow.GoBack();
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        MainWindow.GoBack();
    }
}
