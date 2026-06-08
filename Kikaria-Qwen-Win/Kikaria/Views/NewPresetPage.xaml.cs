using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using Kikaria.Helpers;
using Kikaria.Models;

namespace Kikaria.Views
{
    public sealed partial class NewPresetPage : Page
    {
        private Kikaria.Models.KikariaAppState? _appState;

        public NewPresetPage()
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
            PageTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            NameLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            CategoryLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            ImportLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            EditorLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);

            NameCard.IsDarkMode = isDark;
            CategoryCard.IsDarkMode = isDark;
            ImportCard.IsDarkMode = isDark;
            EditorCard.IsDarkMode = isDark;

            var actionGradient = KikariaTheme.ActionGradient(isDark);
            SaveButton.Background = actionGradient;
            SaveButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        }

        private async void ImportFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".md");
                picker.FileTypeFilter.Add(".txt");
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var text = await Windows.Storage.FileIO.ReadTextAsync(file);
                    MarkdownEditor.Text = text;
                    ImportedFileName.Text = file.Name;

                    if (string.IsNullOrWhiteSpace(PresetNameBox.Text))
                    {
                        PresetNameBox.Text = System.IO.Path.GetFileNameWithoutExtension(file.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"导入文件失败: {ex.Message}");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorBar.IsOpen = false;

            string name = PresetNameBox.Text.Trim();
            string category = CategoryBox.Text.Trim();
            string markdown = MarkdownEditor.Text;

            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("请输入预设名称");
                return;
            }

            if (string.IsNullOrWhiteSpace(markdown))
            {
                ShowError("请输入或导入 Markdown 内容");
                return;
            }

            if (_appState == null) return;

            if (_appState.Presets.Any(p => p.Name == name))
            {
                ShowError("已存在同名预设");
                return;
            }

            var points = KnowledgePoint.ParseMarkdown(markdown);
            if (points.Count == 0)
            {
                ShowError("未能从 Markdown 中解析出任何知识点，请检查格式");
                return;
            }

            var preset = new KnowledgePreset
            {
                Id = $"kikaria.preset.custom.{Guid.NewGuid():N}",
                Name = name,
                Subtitle = $"自定义预设: {name}",
                Description = $"用户创建的预设「{name}」",
                Category = string.IsNullOrWhiteSpace(category) ? "自定义" : category,
                MarkdownText = markdown,
                IsBuiltIn = false
            };

            _appState.Presets.Add(preset);
            _appState.PresetStates[preset.Id] = new PresetStudyState(preset.Id, points, markdown);

            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void FormatGuide_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MarkdownFormatGuidePage));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void ShowError(string message)
        {
            ErrorBar.Message = message;
            ErrorBar.IsOpen = true;
        }
    }
}
