using System;
using System.Collections.Generic;
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
    public sealed partial class EditPresetPage : Page
    {
        private Kikaria.Models.KikariaAppState? _appState;
        private KnowledgePreset? _preset;
        private List<KnowledgePoint> _points = new();
        private string _presetId = string.Empty;

        public EditPresetPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is EditPresetPageNavParam param)
            {
                _appState = param.AppState;
                _presetId = param.PresetId;
            }
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
            LoadPreset();
        }

        private void ApplyTheme()
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            RootGrid.Background = KikariaTheme.PageGradient(isDark);
            PageTitle.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            NameLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            CategoryLabel.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);

            NameCard.IsDarkMode = isDark;
            CategoryCard.IsDarkMode = isDark;

            var actionGradient = KikariaTheme.ActionGradient(isDark);
            SaveButton.Background = actionGradient;
            SaveButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);

            var removeGradient = KikariaTheme.RemoveGradient(isDark);
            DeletePresetButton.Background = removeGradient;
            DeletePresetButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        }

        private void LoadPreset()
        {
            if (_appState == null) return;

            _preset = _appState.Presets.FirstOrDefault(p => p.Id == _presetId);
            if (_preset == null) return;

            PresetNameBox.Text = _preset.Name;
            CategoryBox.Text = _preset.Category;

            if (_appState.PresetStates.TryGetValue(_presetId, out var state))
            {
                _points = new List<KnowledgePoint>(state.KnowledgePoints);
            }
            else
            {
                _points = KnowledgePoint.ParseMarkdown(_preset.MarkdownText);
            }

            DeletePresetButton.Visibility = _preset.IsBuiltIn ? Visibility.Collapsed : Visibility.Visible;

            RefreshPointsList();
        }

        private void RefreshPointsList(string filter = "")
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            KnowledgePointsPanel.Children.Clear();

            var filtered = string.IsNullOrWhiteSpace(filter)
                ? _points
                : _points.Where(p =>
                    p.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    p.Tags.Any(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            foreach (var point in filtered)
            {
                var card = new Controls.LiquidGlassCard
                {
                    CardCornerRadius = 14,
                    Padding = new Thickness(16, 14, 16, 14),
                    IsDarkMode = isDark,
                    AccentColor = KikariaTheme.GetColor(KikariaThemeColor.Cyan, isDark)
                };

                var panel = new StackPanel { Spacing = 8 };

                var titleText = new TextBlock
                {
                    Text = point.Title,
                    FontSize = 15,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontFamily = KikariaTypography.ChineseHeadlineFont
                };
                titleText.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
                panel.Children.Add(titleText);

                if (point.Tags.Count > 0)
                {
                    var tagsText = new TextBlock
                    {
                        Text = string.Join(", ", point.Tags),
                        FontSize = 12,
                        Opacity = 0.5,
                        FontFamily = KikariaTypography.TagFont
                    };
                    panel.Children.Add(tagsText);
                }

                var buttonRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var editBtn = new Button
                {
                    Content = "编辑",
                    FontSize = 12,
                    Height = 28,
                    MinWidth = 50,
                    CornerRadius = new CornerRadius(8),
                    Tag = point.Id,
                    FontFamily = KikariaTypography.ChineseButtonFont
                };
                editBtn.Click += EditPoint_Click;
                buttonRow.Children.Add(editBtn);

                var deleteBtn = new Button
                {
                    Content = "删除",
                    FontSize = 12,
                    Height = 28,
                    MinWidth = 50,
                    CornerRadius = new CornerRadius(8),
                    Tag = point.Id,
                    FontFamily = KikariaTypography.ChineseButtonFont
                };
                deleteBtn.Click += DeletePoint_Click;
                buttonRow.Children.Add(deleteBtn);

                panel.Children.Add(buttonRow);
                card.Content = panel;
                KnowledgePointsPanel.Children.Add(card);
            }

            if (filtered.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "没有找到匹配的知识点",
                    FontSize = 14,
                    Opacity = 0.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontFamily = KikariaTypography.ChineseBodyFont,
                    Margin = new Thickness(0, 20, 0, 20)
                };
                KnowledgePointsPanel.Children.Add(emptyText);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshPointsList(SearchBox.Text);
        }

        private void AddPoint_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(EditKnowledgePointPage), new EditKnowledgePointNavParam
            {
                AppState = _appState!,
                PresetId = _presetId,
                PointId = null
            });
        }

        private void EditPoint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Guid pointId)
                return;

            Frame.Navigate(typeof(EditKnowledgePointPage), new EditKnowledgePointNavParam
            {
                AppState = _appState!,
                PresetId = _presetId,
                PointId = pointId
            });
        }

        private async void DeletePoint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Guid pointId)
                return;

            var point = _points.FirstOrDefault(p => p.Id == pointId);
            if (point == null) return;

            DeletePointMessage.Text = $"确定要删除「{point.Title}」吗？";
            var result = await DeletePointDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _points.Remove(point);
                RefreshPointsList(SearchBox.Text);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorBar.IsOpen = false;

            string name = PresetNameBox.Text.Trim();
            string category = CategoryBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorBar.Message = "请输入预设名称";
                ErrorBar.IsOpen = true;
                return;
            }

            if (_preset == null || _appState == null) return;

            _preset.Name = name;
            _preset.Category = category;
            _preset.MarkdownText = KnowledgePoint.MarkdownTextFrom(_points);

            if (_appState.PresetStates.TryGetValue(_presetId, out var state))
            {
                state.KnowledgePoints = new List<KnowledgePoint>(_points);
                state.MarkdownText = _preset.MarkdownText;
            }

            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void ExportMarkdown_Click(object sender, RoutedEventArgs e)
        {
            if (_preset == null) return;

            try
            {
                var picker = new FileSavePicker();
                picker.FileTypeChoices.Add("Markdown", new List<string> { ".md" });
                picker.SuggestedFileName = _preset.Name;

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    string markdown = KnowledgePoint.MarkdownTextFrom(_points);
                    await Windows.Storage.FileIO.WriteTextAsync(file, markdown);
                }
            }
            catch (Exception ex)
            {
                ErrorBar.Message = $"导出失败: {ex.Message}";
                ErrorBar.IsOpen = true;
            }
        }

        private void EditRawMarkdown_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MarkdownEditorPage), new MarkdownEditorPageNavParam
            {
                AppState = _appState!,
                PresetId = _presetId
            });
        }

        private async void DeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (_preset == null || _appState == null) return;

            DeletePresetMessage.Text = $"确定要删除预设「{_preset.Name}」吗？此操作不可撤销。";
            var result = await DeletePresetDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _appState.Presets.Remove(_preset);
                _appState.PresetStates.Remove(_presetId);

                if (_appState.CurrentPresetID == _presetId)
                {
                    _appState.CurrentPresetID = _appState.Presets.FirstOrDefault()?.Id ?? string.Empty;
                }

                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
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

    public class EditKnowledgePointNavParam
    {
        public Kikaria.Models.KikariaAppState AppState { get; set; } = null!;
        public string PresetId { get; set; } = string.Empty;
        public Guid? PointId { get; set; }
    }
}
