using Intatis.App.Services;
using Intatis.App.Views;
using Microsoft.UI.Xaml;

namespace Intatis.App;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;
    public static Window? MainWindow { get; private set; }

    public AppEnvironment Environment { get; }

    public App()
    {
        InitializeComponent();
        Environment = AppEnvironment.Load();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
