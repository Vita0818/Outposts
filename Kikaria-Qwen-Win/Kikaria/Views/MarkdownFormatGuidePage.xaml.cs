using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Kikaria.Helpers;

namespace Kikaria.Views
{
    public sealed partial class MarkdownFormatGuidePage : Page
    {
        public MarkdownFormatGuidePage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            RootGrid.Background = KikariaTheme.PageGradient(isDark);
            PageTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);

            IntroCard.IsDarkMode = isDark;
            FormatCard.IsDarkMode = isDark;
            RulesCard.IsDarkMode = isDark;
            LatexCard.IsDarkMode = isDark;
            ExampleCard.IsDarkMode = isDark;
            AIPromptCard.IsDarkMode = isDark;

            IntroTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            FormatTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            RulesTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            LatexTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            ExampleTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            AIPromptTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            AIPromptDesc.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.SoftText, isDark);

            var textColor = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            IntroBody.Foreground = textColor;
            FormatCode.Foreground = textColor;
            ExampleCode.Foreground = textColor;
            AIPromptText.Foreground = textColor;

            foreach (var child in RulesList.Children)
            {
                if (child is TextBlock tb)
                    tb.Foreground = textColor;
            }
        }

        private void CopyPrompt_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(AIPromptText.Text);
            Clipboard.SetContent(dataPackage);

            CopyPromptButton.Content = "已复制";
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, args) =>
            {
                CopyPromptButton.Content = "复制";
                timer.Stop();
            };
            timer.Start();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}
