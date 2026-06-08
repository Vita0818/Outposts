using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using Kikaria.Helpers;
using Kikaria.Models;

namespace Kikaria.Views
{
    public sealed partial class ProfileSetupPage : Page
    {
        private Kikaria.Models.KikariaAppState? _appState;
        private byte[]? _avatarImageData;

        public ProfileSetupPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Kikaria.Models.KikariaAppState state)
            {
                _appState = state;
            }
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            RootGrid.Background = KikariaTheme.PageGradient(isDark);
            WelcomeTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            SetupSubtitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.SoftText, isDark);
            DisplayNameLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            UsernameLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);

            ProfileCard.IsDarkMode = isDark;
            ProfileCard.AccentColor = KikariaTheme.GetColor(KikariaThemeColor.Sky, isDark);

            var avatarGradient = KikariaTheme.ActionGradient(isDark);
            AvatarBackground.Fill = avatarGradient;
            AvatarOverlay.Background = avatarGradient;

            var actionGradient = KikariaTheme.ActionGradient(isDark);
            StartButton.Background = actionGradient;
            StartButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        }

        private void DisplayNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            StartButton.IsEnabled = !string.IsNullOrWhiteSpace(DisplayNameBox.Text);
        }

        private async void AvatarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var stream = await file.OpenReadAsync();
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    AvatarPicture.ProfilePicture = bitmap;

                    using var dataReader = new Windows.Storage.Streams.DataReader(stream);
                    await dataReader.LoadAsync((uint)stream.Size);
                    _avatarImageData = new byte[stream.Size];
                    dataReader.ReadBytes(_avatarImageData);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Avatar picker error: {ex.Message}");
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _appState ??= new Kikaria.Models.KikariaAppState();

            _appState.UserProfile = new UserProfile
            {
                DisplayName = DisplayNameBox.Text.Trim(),
                UserHandle = UsernameBox.Text.Trim(),
                AvatarSystemName = "person.circle",
                AvatarImageData = _avatarImageData
            };
            _appState.HasCompletedProfileSetup = true;
            _appState.HasCompletedOnboarding = true;

            Frame.Navigate(typeof(TodayOverviewPage), _appState);
        }
    }
}
