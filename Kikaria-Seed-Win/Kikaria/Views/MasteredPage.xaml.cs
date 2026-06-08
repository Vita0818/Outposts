using Kikaria.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kikaria.Views
{
    public sealed partial class MasteredPage : Page
    {
        public MasteredPage()
        {
            this.InitializeComponent();
            LoadMasteredPoints();
        }

        private void LoadMasteredPoints()
        {
            var allPoints = App.AppState.CurrentPresetState.KnowledgePoints;
            var masteredPoints = allPoints.Where(p => p.IsMastered)
                                         .OrderByDescending(p => p.UpdatedAt)
                                         .ToList();
            MasteredListView.ItemsSource = masteredPoints;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private async void RemoveFromMasteredButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid pointId)
            {
                var currentState = App.AppState.CurrentPresetState;
                var point = currentState.KnowledgePoints.FirstOrDefault(p => p.Id == pointId);
                if (point != null)
                {
                    point.IsMastered = false;
                    point.UpdatedAt = DateTimeOffset.Now;
                    App.SaveAppState();
                    LoadMasteredPoints();

                    var dialog = new ContentDialog
                    {
                        Title = "Removed from Mastered",
                        Content = "This knowledge point has been removed from your mastered list.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }
    }
}