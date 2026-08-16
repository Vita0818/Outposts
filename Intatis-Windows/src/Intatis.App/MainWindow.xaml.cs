using Intatis.App.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Intatis.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Intatis";

        try
        {
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetPrimary();
            var workArea = displayArea.WorkArea;
            AppWindow.Resize(new Windows.Graphics.SizeInt32(
                Math.Min(1240, workArea.Width - 80),
                Math.Min(840, workArea.Height - 80)));
        }
        catch (Exception)
        {
            // Window sizing is best-effort; the shell still works at the default size.
        }

        ContentFrame.Navigate(typeof(ChatPage));
        Nav.SelectedItem = Nav.MenuItems[0];
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag) return;
        var page = tag switch
        {
            "code" => typeof(CodePage),
            "cowork" => typeof(CoworkPage),
            "settings" => typeof(SettingsPage),
            _ => typeof(ChatPage),
        };
        if (ContentFrame.CurrentSourcePageType != page)
            ContentFrame.Navigate(page);
    }
}
