using Kikaria.Models;
using Kikaria.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kikaria.Views
{
    public sealed partial class ReviewPage : Page
    {
        private List<string> _selectedTags = new();
        private List<KnowledgePoint> _reviewPoints = new();
        private KnowledgePoint _currentPoint;
        private Random _random = new();
        private int _reviewedCount = 0;

        public ReviewPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is List<string> tags)
            {
                _selectedTags = tags;
                LoadReviewPoints();
                LoadNextPoint();
            }
        }

        private void LoadReviewPoints()
        {
            var currentState = App.AppState.CurrentPresetState;
            _reviewPoints = currentState.KnowledgePoints.Where(p => p.Tags.Any(t => _selectedTags.Contains(t)))
                                      .ToList();
            UpdateProgressText();
        }

        private void UpdateProgressText()
        {
            ProgressText.Text = $"{_reviewedCount} / {_reviewPoints.Count}";
        }

        private void LoadNextPoint()
        {
            if (!_reviewPoints.Any())
            {
                var dialog = new ContentDialog
                {
                    Title = "Review Complete",
                    Content = "You have reviewed all knowledge points for selected tags.",
                    CloseButtonText = "Go Back",
                    XamlRoot = this.XamlRoot
                };
                dialog.ShowAsync();
                Frame.GoBack();
                return;
            }

            var index = _random.Next(_reviewPoints.Count);
            _currentPoint = _reviewPoints[index];

            TitleTextBlock.Text = _currentPoint.Title;
            HintTextBlock.Text = _currentPoint.Hint;
            ContentTextBlock.Text = _currentPoint.Content;

            HintBorder.Visibility = Visibility.Collapsed;
            ContentBorder.Visibility = Visibility.Collapsed;
            AddToReinforcementButton.IsEnabled = false;
            MarkMasteredButton.IsEnabled = false;
            ShowHintButton.IsEnabled = true;
            ShowContentButton.IsEnabled = true;

            _reviewedCount++;
            UpdateProgressText();

            var currentState = App.AppState.CurrentPresetState;
            currentState.ActivityRecords.Add(new StudyActivityRecord(
                id: Guid.NewGuid(),
                presetId: currentState.PresetId,
                date: DateTimeOffset.Now,
                type: StudyActivityType.ReviewedAnswer,
                pointId: _currentPoint.Id,
                pointTitle: _currentPoint.Title
            ));
            App.SaveAppState();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void ShowHintButton_Click(object sender, RoutedEventArgs e)
        {
            HintBorder.Visibility = Visibility.Visible;
            ShowHintButton.IsEnabled = false;

            var currentState = App.AppState.CurrentPresetState;
            currentState.ActivityRecords.Add(new StudyActivityRecord(
                id: Guid.NewGuid(),
                presetId: currentState.PresetId,
                date: DateTimeOffset.Now,
                type: StudyActivityType.ViewedHint,
                pointId: _currentPoint.Id,
                pointTitle: _currentPoint.Title
            ));
            App.SaveAppState();
        }

        private void ShowContentButton_Click(object sender, RoutedEventArgs e)
        {
            ContentBorder.Visibility = Visibility.Visible;
            ShowContentButton.IsEnabled = false;
            AddToReinforcementButton.IsEnabled = !_currentPoint.IsReinforced;
            MarkMasteredButton.IsEnabled = !_currentPoint.IsMastered;
        }

        private async void AddToReinforcementButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPoint.AddReinforcement();
            App.SaveAppState();
            AddToReinforcementButton.IsEnabled = false;

            var currentState = App.AppState.CurrentPresetState;
            currentState.ActivityRecords.Add(new StudyActivityRecord(
                id: Guid.NewGuid(),
                presetId: currentState.PresetId,
                date: DateTimeOffset.Now,
                type: StudyActivityType.AddedReinforcement,
                pointId: _currentPoint.Id,
                pointTitle: _currentPoint.Title
            ));
            App.SaveAppState();

            var dialog = new ContentDialog
            {
                Title = "Added to Reinforcement",
                Content = "This knowledge point has been added to your reinforcement list.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void MarkMasteredButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPoint.IsMastered = true;
            _currentPoint.UpdatedAt = DateTimeOffset.Now;
            App.SaveAppState();
            MarkMasteredButton.IsEnabled = false;

            var currentState = App.AppState.CurrentPresetState;
            currentState.ActivityRecords.Add(new StudyActivityRecord(
                id: Guid.NewGuid(),
                presetId: currentState.PresetId,
                date: DateTimeOffset.Now,
                type: StudyActivityType.MarkedMastered,
                pointId: _currentPoint.Id,
                pointTitle: _currentPoint.Title
            ));
            App.SaveAppState();

            var dialog = new ContentDialog
            {
                Title = "Marked as Mastered",
                Content = "This knowledge point has been marked as mastered.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            LoadNextPoint();
        }
    }
}