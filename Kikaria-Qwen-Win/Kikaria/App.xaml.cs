using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Kikaria.ViewModels;

namespace Kikaria
{
    public partial class App : Application
    {
        private static MainViewModel? _mainViewModel;
        public static MainViewModel MainViewModel => _mainViewModel ??= MainViewModel.Instance;

        private Window? _window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _mainViewModel = MainViewModel.Instance;
            await _mainViewModel.LoadAppState();

            _window = new MainWindow();
            _window.Activate();

            if (_window is MainWindow mainWindow)
            {
                mainWindow.SetViewModel(_mainViewModel);
            }

            if (!_mainViewModel.HasCompletedOnboarding)
            {
                _mainViewModel.NavigateTo(AppRoute.Scope);
            }
            else if (!_mainViewModel.HasCompletedProfileSetup)
            {
                _mainViewModel.NavigateTo(AppRoute.EditProfile);
            }
        }

        public static new App Current => (App)Application.Current;

        public Window? MainWindow => _window;

        public static Window CurrentWindow => Current.MainWindow!;
    }
}
