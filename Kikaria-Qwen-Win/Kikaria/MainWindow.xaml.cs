using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Kikaria.Helpers;
using Kikaria.Models;
using Kikaria.ViewModels;
using Windows.Graphics;
using WinRT.Interop;

namespace Kikaria
{
    public sealed partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private AppWindow? _appWindow;

        public MainWindow()
        {
            this.InitializeComponent();
            ConfigureWindow();
            ConfigureTitlebar();
        }

        public void SetViewModel(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            NavigateToCurrentRoute();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentRoute))
            {
                NavigateToCurrentRoute();
            }
        }

        private void NavigateToCurrentRoute()
        {
            if (_viewModel == null) return;

            var route = _viewModel.CurrentRoute;
            Type? pageType = route switch
            {
                null => typeof(Views.HomePage),
                AppRoute.Scope => typeof(Views.ScopeSelectionPage),
                AppRoute.Review => typeof(Views.ReviewPage),
                AppRoute.TodayOverview => typeof(Views.TodayOverviewPage),
                AppRoute.ReviewHistory => typeof(Views.ReviewHistoryPage),
                AppRoute.Reinforcement => typeof(Views.ReinforcementPage),
                AppRoute.ReinforcementReview => typeof(Views.ReviewPage),
                AppRoute.Mastered => typeof(Views.MasteredPage),
                AppRoute.MasteredReview => typeof(Views.ReviewPage),
                AppRoute.Settings => typeof(Views.SettingsPage),
                AppRoute.EditProfile => typeof(Views.EditProfilePage),
                AppRoute.MarkdownEditor => typeof(Views.MarkdownEditorPage),
                AppRoute.PresetSelection => typeof(Views.PresetSelectionPage),
                AppRoute.NewPreset => typeof(Views.NewPresetPage),
                AppRoute.MarkdownFormatGuide => typeof(Views.MarkdownFormatGuidePage),
                _ => null
            };

            if (pageType != null)
            {
                ContentFrame.Navigate(pageType);
            }
        }

        private void ConfigureWindow()
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            if (_appWindow != null)
            {
                _appWindow.Resize(new SizeInt32(1240, 780));

                if (AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.SetBorderAndTitleBar(true, true);
                }
            }

            this.Closed += MainWindow_Closed;
        }

        private void ConfigureTitlebar()
        {
            if (_appWindow != null)
            {
                _appWindow.Title = "Kikaria";

                var titleBar = _appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = false;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = KikariaTheme.DeepTextLight;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(30, 56, 152, 236);
                titleBar.ButtonHoverForegroundColor = KikariaTheme.SkyLight;
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                NavigateToTag(tag);
            }
        }

        private void NavigateToTag(string tag)
        {
            if (_viewModel == null) return;

            switch (tag)
            {
                case "Home":
                    _viewModel.NavigateHome();
                    break;
                case "TodayOverview":
                    _viewModel.NavigateTo(AppRoute.TodayOverview);
                    break;
                case "Reinforcement":
                    _viewModel.NavigateTo(AppRoute.Reinforcement);
                    break;
                case "Mastered":
                    _viewModel.NavigateTo(AppRoute.Mastered);
                    break;
                case "PresetLibrary":
                    _viewModel.NavigateTo(AppRoute.PresetSelection);
                    break;
                case "Statistics":
                    _viewModel.NavigateTo(AppRoute.ReviewHistory);
                    break;
                case "Profile":
                    _viewModel.NavigateTo(AppRoute.EditProfile);
                    break;
                case "Settings":
                    _viewModel.NavigateTo(AppRoute.Settings);
                    break;
                default:
                    _viewModel.NavigateHome();
                    break;
            }
        }

        private void ProfileItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.NavigateTo(AppRoute.EditProfile);
            }

            foreach (var item in NavView.FooterMenuItems.OfType<NavigationViewItem>())
            {
                item.IsSelected = false;
            }
        }

        private void SettingsItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.NavigateTo(AppRoute.Settings);
            }
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (_viewModel != null)
            {
                await _viewModel.SaveAppState();
                _viewModel.UpdateWidgetSnapshot();
            }
        }

        public void HandleReviewKeyboardShortcut(KeyRoutedEventArgs e)
        {
            if (_viewModel == null) return;

            switch (e.Key)
            {
                case Windows.System.VirtualKey.Space:
                    break;
                case Windows.System.VirtualKey.Enter:
                    break;
                case Windows.System.VirtualKey.K:
                case Windows.System.VirtualKey.M:
                    break;
                case Windows.System.VirtualKey.L:
                case Windows.System.VirtualKey.Oem1:
                case Windows.System.VirtualKey.Oem7:
                    break;
            }
        }
    }
}
