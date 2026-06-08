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
    public sealed partial class EditKnowledgePointPage : Page
    {
        private Kikaria.Models.KikariaAppState? _appState;
        private string _presetId = string.Empty;
        private Guid? _pointId;
        private KnowledgePoint? _existingPoint;
        private bool _isEditing;

        public EditKnowledgePointPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is EditKnowledgePointNavParam param)
            {
                _appState = param.AppState;
                _presetId = param.PresetId;
                _pointId = param.PointId;
                _isEditing = _pointId.HasValue;
            }
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
            LoadData();
        }

        private void ApplyTheme()
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            RootGrid.Background = KikariaTheme.PageGradient(isDark);
            PageTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            TitleLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            TagsLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            HintLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            ContentLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);

            PresetInfoCard.IsDarkMode = isDark;
            TitleCard.IsDarkMode = isDark;
            TagsCard.IsDarkMode = isDark;
            HintCard.IsDarkMode = isDark;
            ContentCard.IsDarkMode = isDark;

            var actionGradient = KikariaTheme.ActionGradient(isDark);
            SaveButton.Background = actionGradient;
            SaveButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        }

        private void LoadData()
        {
            if (_appState == null) return;

            var preset = _appState.Presets.FirstOrDefault(p => p.Id == _presetId);
            if (preset != null)
            {
                PresetNameDisplay.Text = $"预设: {preset.Name}";
            }

            if (_isEditing && _pointId.HasValue)
            {
                PageTitle.Text = "编辑知识点";

                if (_appState.PresetStates.TryGetValue(_presetId, out var state))
                {
                    _existingPoint = state.KnowledgePoints.FirstOrDefault(p => p.Id == _pointId.Value);
                }

                if (_existingPoint != null)
                {
                    TitleBox.Text = _existingPoint.Title;
                    TagsBox.Text = string.Join(", ", _existingPoint.Tags);
                    HintBox.Text = _existingPoint.Hint;
                    ContentBox.Text = _existingPoint.Content;
                }
            }
            else
            {
                PageTitle.Text = "添加知识点";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorBar.IsOpen = false;

            string title = TitleBox.Text.Trim();
            string tagsText = TagsBox.Text.Trim();
            string hint = HintBox.Text.Trim();
            string content = ContentBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                ShowError("请输入标题");
                return;
            }

            if (string.IsNullOrWhiteSpace(hint))
            {
                ShowError("请输入提示内容");
                return;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                ShowError("请输入内容");
                return;
            }

            var tags = string.IsNullOrWhiteSpace(tagsText)
                ? new List<string>()
                : tagsText.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

            if (_appState == null) return;

            if (!_appState.PresetStates.TryGetValue(_presetId, out var state))
                return;

            if (_isEditing && _existingPoint != null)
            {
                _existingPoint.Title = title;
                _existingPoint.Tags = tags;
                _existingPoint.Hint = hint;
                _existingPoint.Content = content;
                _existingPoint.UpdatedAt = DateTime.Now;
            }
            else
            {
                var newPoint = new KnowledgePoint(title, tags, hint, content);
                state.KnowledgePoints.Add(newPoint);
            }

            var preset = _appState.Presets.FirstOrDefault(p => p.Id == _presetId);
            if (preset != null)
            {
                preset.MarkdownText = KnowledgePoint.MarkdownTextFrom(state.KnowledgePoints);
                state.MarkdownText = preset.MarkdownText;
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

        private void ShowError(string message)
        {
            ErrorBar.Message = message;
            ErrorBar.IsOpen = true;
        }
    }
}
