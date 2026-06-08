using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Kikaria.Helpers;
using Kikaria.Models;

namespace Kikaria.Views
{
    public class MarkdownEditorPageNavParam
    {
        public Kikaria.Models.KikariaAppState AppState { get; set; } = null!;
        public string PresetId { get; set; } = string.Empty;
    }

    public sealed partial class MarkdownEditorPage : Page
    {
        private Kikaria.Models.KikariaAppState? _appState;
        private string _presetId = string.Empty;

        public MarkdownEditorPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is MarkdownEditorPageNavParam param)
            {
                _appState = param.AppState;
                _presetId = param.PresetId;
            }
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
            LoadContent();
        }

        private void ApplyTheme()
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            RootGrid.Background = KikariaTheme.PageGradient(isDark);
            PageTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);

            EditorCard.IsDarkMode = isDark;

            var actionGradient = KikariaTheme.ActionGradient(isDark);
            ApplyButton.Background = actionGradient;
            ApplyButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);

            MarkdownTextBox.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
        }

        private void LoadContent()
        {
            if (_appState == null) return;

            if (_appState.PresetStates.TryGetValue(_presetId, out var state))
            {
                MarkdownTextBox.Text = state.MarkdownText;
            }
            else
            {
                var preset = _appState.Presets.FirstOrDefault(p => p.Id == _presetId);
                if (preset != null)
                {
                    MarkdownTextBox.Text = preset.MarkdownText;
                }
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorBar.IsOpen = false;

            string markdown = MarkdownTextBox.Text;

            if (string.IsNullOrWhiteSpace(markdown))
            {
                ErrorBar.Message = "Markdown 内容不能为空";
                ErrorBar.IsOpen = true;
                return;
            }

            if (_appState == null) return;

            var points = KnowledgePoint.ParseMarkdown(markdown);

            if (points.Count == 0)
            {
                ErrorBar.Message = "未能解析出任何知识点，请检查 Markdown 格式";
                ErrorBar.IsOpen = true;
                return;
            }

            if (_appState.PresetStates.TryGetValue(_presetId, out var state))
            {
                state.KnowledgePoints = new List<KnowledgePoint>(points);
                state.MarkdownText = markdown;
            }

            var preset = _appState.Presets.FirstOrDefault(p => p.Id == _presetId);
            if (preset != null)
            {
                preset.MarkdownText = markdown;
            }

            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
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
