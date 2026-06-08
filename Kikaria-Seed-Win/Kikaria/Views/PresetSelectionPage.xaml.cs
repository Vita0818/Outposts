using Kikaria.Models;
using Kikaria.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace Kikaria.Views
{
    public sealed partial class PresetSelectionPage : Page
    {
        public PresetSelectionPage()
        {
            this.InitializeComponent();
            LoadPresets();
        }

        private void LoadPresets()
        {
            PresetsListView.ItemsSource = App.AppState.Presets;
            PresetsListView.SelectedItem = App.AppState.Presets.FirstOrDefault(p => p.Id == App.AppState.CurrentPresetId);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private async void PresetsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PresetsListView.SelectedItem is KnowledgePreset selectedPreset)
            {
                App.AppState.CurrentPresetId = selectedPreset.Id;

                if (!App.AppState.PresetStates.ContainsKey(selectedPreset.Id))
                {
                    var newState = new PresetStudyState(
                        presetId: selectedPreset.Id,
                        knowledgePoints: KnowledgePoint.ParseMarkdown(selectedPreset.MarkdownText),
                        markdownText: selectedPreset.MarkdownText,
                        selectedTags: new HashSet<string>(),
                        dailyReviewRecords: new Dictionary<Guid, DailyReviewRecord>(),
                        activityRecords: new List<StudyActivityRecord>(),
                        dailyGoal: 20
                    );
                    App.AppState.PresetStates[selectedPreset.Id] = newState;
                }

                App.SaveAppState();

                var dialog = new ContentDialog
                {
                    Title = "Preset Selected",
                    Content = $"Switched to preset: {selectedPreset.Name}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();

                Frame.GoBack();
            }
        }
    }
}