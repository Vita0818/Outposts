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
    public sealed partial class EditProfilePage : Page
    {
        private Kikaria.Models.KikariaAppState? _appState;
        private byte[]? _avatarImageData;

        public EditProfilePage()
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
            LoadProfile();
        }

        private void ApplyTheme()
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            RootGrid.Background = KikariaTheme.PageGradient(isDark);
            PageTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            DisplayNameLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            UserIdLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);

            AvatarCard.IsDarkMode = isDark;
            NameCard.IsDarkMode = isDark;
            UserIdCard.IsDarkMode = isDark;

            var avatarGradient = KikariaTheme.ActionGradient(isDark);
            AvatarBackground.Fill = avatarGradient;
            AvatarOverlay.Background = avatarGradient;

            var actionGradient = KikariaTheme.ActionGradient(isDark);
            SaveButton.Background = actionGradient;
            SaveButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        }

        private void LoadProfile()
        {
            if (_appState?.UserProfile == null) return;

            var profile = _appState.UserProfile;
            DisplayNameBox.Text = profile.DisplayName;
            UserIdBox.Text = profile.UserHandle;

            if (profile.AvatarImageData != null && profile.AvatarImageData.Length > 0)
            {
                _avatarImageData = profile.AvatarImageData;
                var bitmap = new BitmapImage();
                using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                using var writer = new Windows.Storage.Streams.DataWriter(stream);
                writer.WriteBytes(_avatarImageData);
                _ = writer.StoreAsync().AsTask().ContinueWith(async _ =>
                {
                    stream.Seek(0);
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await bitmap.SetSourceAsync(stream);
                        AvatarPicture.ProfilePicture = bitmap;
                    });
                });
            }
            else if (!string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                AvatarPicture.DisplayName = profile.DisplayName;
            }
        }

        private async void ChangePhoto_Click(object sender, RoutedEventArgs e)
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

                    stream.Seek(0);
                    using var dataReader = new Windows.Storage.Streams.DataReader(stream);
                    await dataReader.LoadAsync((uint)stream.Size);
                    _avatarImageData = new byte[stream.Size];
                    dataReader.ReadBytes(_avatarImageData);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Photo picker error: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_appState == null) return;

            _appState.UserProfile.DisplayName = DisplayNameBox.Text.Trim();
            _appState.UserProfile.UserHandle = UserIdBox.Text.Trim();

            if (_avatarImageData != null)
            {
                _appState.UserProfile.AvatarImageData = _avatarImageData;
            }

            SuccessBar.IsOpen = true;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, args) =>
            {
                SuccessBar.IsOpen = false;
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
