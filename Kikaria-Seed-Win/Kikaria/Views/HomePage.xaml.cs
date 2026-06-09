using Kikaria.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage.Pickers;

namespace Kikaria.Views
{
    public sealed partial class HomePage : Page
    {
        private List<string> _selectedTags = new();
        private List<CheckBox> _tagCheckboxes = new();

        public HomePage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var currentState = App.AppState.CurrentPresetState;
            var points = currentState.KnowledgePoints;

            TotalPointsText.Text = points.Count.ToString();
            ReinforcedPointsText.Text = points.Count(p => p.ReinforcementCount > 0).ToString();
            MasteredPointsText.Text = points.Count(p => p.IsMastered).ToString();

            var allTags = points.SelectMany(p => p.Tags)
                               .Distinct()
                               .OrderBy(t => t)
                               .ToList();

            TagsWrapPanel.Children.Clear();
            _tagCheckboxes.Clear();

            foreach (var tag in allTags)
            {
                var checkBox = new CheckBox
                {
                    Content = tag,
                    Padding = new Thickness(12, 8, 12, 8),
                    CornerRadius = new CornerRadius(16),
                    Background = KikariaTheme.Instance.Mist,
                    Foreground = KikariaTheme.Instance.DeepText,
                    BorderBrush = KikariaTheme.Instance.GlassStrokeAccent,
                    IsChecked = currentState.SelectedTags.Contains(tag)
                };
                _tagCheckboxes.Add(checkBox);
                TagsWrapPanel.Children.Add(checkBox);
            }

            StartReviewButton.IsEnabled = points.Any();
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".md");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            try
            {
                var markdownText = File.ReadAllText(file.Path);
                var parsedPoints = KnowledgePoint.ParseMarkdown(markdownText);
                var currentState = App.AppState.CurrentPresetState;
                
                currentState.KnowledgePoints.AddRange(parsedPoints);
                currentState.MarkdownText += Environment.NewLine + "---" + Environment.NewLine + markdownText;
                App.SaveAppState();

                LoadData();

                var dialog = new ContentDialog
                {
                    Title = "Import Successful",
                    Content = $"Imported {parsedPoints.Count} knowledge points.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Import Failed",
                    Content = $"Error parsing file: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void StartReviewButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedTags = _tagCheckboxes.Where(cb => cb.IsChecked == true)
                                         .Select(cb => cb.Content.ToString() ?? "")
                                         .Where(t => !string.IsNullOrEmpty(t))
                                         .ToList();

            var currentState = App.AppState.CurrentPresetState;
            currentState.SelectedTags = new HashSet<string>(_selectedTags);
            App.SaveAppState();

            if (!_selectedTags.Any() && currentState.KnowledgePoints.Any())
            {
                _selectedTags = currentState.KnowledgePoints.SelectMany(p => p.Tags).Distinct().ToList();
            }

            if (!_selectedTags.Any())
            {
                var dialog = new ContentDialog
                {
                    Title = "No Knowledge Points",
                    Content = "Please import Markdown file first.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                dialog.ShowAsync();
                return;
            }

            Frame.Navigate(typeof(ReviewPage), _selectedTags);
        }

        private void ViewReinforcementButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ReinforcementPage));
        }

        private void ViewMasteredButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MasteredPage));
        }

        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PresetSelectionPage));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadData();
        }
    }
}