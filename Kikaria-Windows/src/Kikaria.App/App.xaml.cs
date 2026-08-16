using Kikaria.Core;
using Microsoft.UI.Xaml;

namespace Kikaria.App;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
