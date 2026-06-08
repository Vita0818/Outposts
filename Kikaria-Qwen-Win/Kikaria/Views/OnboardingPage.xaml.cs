using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using Kikaria.Helpers;

namespace Kikaria.Views
{
    public sealed partial class OnboardingPage : Page
    {
        private int _currentPage = 0;
        private const int TotalPages = 3;

        public OnboardingPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
            UpdatePage();
            BuildIndicators();
        }

        private void ApplyTheme()
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            RootGrid.Background = KikariaTheme.PageGradient(isDark);
            AppTitleText.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);

            OnboardingCard.IsDarkMode = isDark;
            OnboardingCard.AccentColor = KikariaTheme.GetColor(KikariaThemeColor.Sky, isDark);

            var actionGradient = KikariaTheme.ActionGradient(isDark);
            NextButton.Background = actionGradient;
            NextButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);

            Color iconGradientStart = KikariaTheme.GetColor(KikariaThemeColor.Sky, isDark);
            Color iconGradientEnd = KikariaTheme.GetColor(KikariaThemeColor.Cyan, isDark);

            var iconGradient = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };
            iconGradient.GradientStops.Add(new GradientStop { Color = iconGradientStart, Offset = 0.0 });
            iconGradient.GradientStops.Add(new GradientStop { Color = iconGradientEnd, Offset = 1.0 });

            Icon1Circle.Fill = iconGradient;
            Icon2Circle.Fill = iconGradient;
            Icon3Circle.Fill = iconGradient;

            var textColor = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            Page1Title.Foreground = textColor;
            Page2Title.Foreground = textColor;
            Page3Title.Foreground = textColor;
        }

        private void BuildIndicators()
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            PageIndicators.Children.Clear();

            for (int i = 0; i < TotalPages; i++)
            {
                var dot = new Ellipse
                {
                    Width = i == _currentPage ? 24 : 8,
                    Height = 8,
                    Fill = i == _currentPage
                        ? KikariaTheme.GetBrush(KikariaThemeColor.Sky, isDark)
                        : new SolidColorBrush(Microsoft.UI.Color.FromArgb(80, 128, 128, 128))
                };
                PageIndicators.Children.Add(dot);
            }
        }

        private void UpdatePage()
        {
            Page1Content.Visibility = _currentPage == 0 ? Visibility.Visible : Visibility.Collapsed;
            Page2Content.Visibility = _currentPage == 1 ? Visibility.Visible : Visibility.Collapsed;
            Page3Content.Visibility = _currentPage == 2 ? Visibility.Visible : Visibility.Collapsed;

            NextButton.Content = _currentPage == TotalPages - 1 ? "开始使用" : "下一步";

            BuildIndicators();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < TotalPages - 1)
            {
                _currentPage++;
                UpdatePage();
            }
            else
            {
                Frame.Navigate(typeof(ProfileSetupPage));
            }
        }
    }
}
