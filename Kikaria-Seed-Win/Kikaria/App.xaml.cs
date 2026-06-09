using Kikaria.Models;
using Kikaria.Services;
using Microsoft.UI.Xaml;
using System;

namespace Kikaria
{
    public partial class App : Application
    {
        public static StorageService StorageService { get; } = new StorageService();
        public static KikariaAppState AppState { get; private set; } = null!;
        public static Window MainWindow { get; private set; } = null!;

        public App()
        {
            this.InitializeComponent();
            AppState = StorageService.LoadAppState();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            MainWindow = m_window;
            m_window.Activate();
        }

        public static void SaveAppState()
        {
            StorageService.SaveAppState(AppState);
        }

        private Window m_window;
    }
}