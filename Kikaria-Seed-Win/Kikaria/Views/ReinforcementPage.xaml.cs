using Kikaria.Models;
using Kikaria.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;

namespace Kikaria.Views
{
    public sealed partial class ReinforcementPage : Page
    {
        public ReinforcementPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadReinforcementPoints();
        }

        private void LoadReinforcementPoints()
        {
            var allPoints = App.AppState.CurrentPresetState.KnowledgePoints;
            var reinforcedPoints = allPoints.Where(p => p.ReinforcementCount > 0)
                                           .OrderByDescending(p => p.UpdatedAt)
                                           .ToList();
            ReinforcementListView.ItemsSource = reinforcedPoints;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private async void RemoveFromReinforcementButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid pointId)
            {
                var currentState = App.AppState.CurrentPresetState;
                var point = currentState.KnowledgePoints.FirstOrDefault(p => p.Id == pointId);
                if (point != null)
                {
                    point.ClearReinforcement();
                    App.SaveAppState();
                    LoadReinforcementPoints();

                    currentState.ActivityRecords.Add(new StudyActivityRecord(
                        id: Guid.NewGuid(),
                        presetId: currentState.PresetId,
                        date: DateTimeOffset.Now,
                        type: StudyActivityType.RemovedReinforcement,
                        pointId: point.Id,
                        pointTitle: point.Title
                    ));
                    App.SaveAppState();

                    var dialog = new ContentDialog
                    {
                        Title = "Removed from Reinforcement",
                        Content = "This knowledge point has been removed from your reinforcement list.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }
    }
}