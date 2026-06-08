using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Kikaria.Models;
using Kikaria.ViewModels;

namespace Kikaria.Views;

public sealed partial class ReinforcementPage : Page
{
    private MainViewModel VM => App.MainViewModel;

    public ReinforcementPage()
    {
        InitializeComponent();
        Loaded += ReinforcementPage_Loaded;
    }

    private void ReinforcementPage_Loaded(object sender, RoutedEventArgs e)
    {
        VM.PropertyChanged += VM_PropertyChanged;
        RefreshList();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        VM.PropertyChanged += VM_PropertyChanged;
        RefreshList();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        VM.PropertyChanged -= VM_PropertyChanged;
    }

    private void VM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ReinforcedCount) ||
            e.PropertyName == nameof(MainViewModel.ToastMessage))
        {
            DispatcherQueue.TryEnqueue(RefreshList);
        }
    }

    private void RefreshList()
    {
        var points = GetFilteredPoints();
        CardList.ItemsSource = points;
        CountText.Text = VM.ReinforcedCount.ToString();
        EmptyState.Visibility = points.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        StartReviewButton.IsEnabled = points.Count > 0;
        StartReviewButton.Opacity = points.Count > 0 ? 1.0 : 0.5;
        UpdateToast();
    }

    private List<KnowledgePoint> GetFilteredPoints()
    {
        var points = VM.ReinforcedPointsList;
        var query = SearchBox?.Text;
        if (!string.IsNullOrEmpty(query))
            points = points.FindAll(p => p.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                         p.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
        return points;
    }

    private void UpdateToast()
    {
        if (!string.IsNullOrEmpty(VM.ToastMessage))
        {
            ToastText.Text = VM.ToastMessage;
            ToastBorder.Visibility = Visibility.Visible;
        }
        else
        {
            ToastBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshList();
    }

    private void StartReview_Click(object sender, RoutedEventArgs e)
    {
        VM.ReviewMode = ReviewMode.Reinforcement;
        VM.BuildReviewQueue();
        if (!VM.IsReviewQueueEmpty)
            Frame.Navigate(typeof(ReviewPage));
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is KnowledgePoint point)
        {
            VM.RemoveReinforcedPointAction(point);
            RefreshList();
        }
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool isWide = e.NewSize.Width >= 800;
        ContentRoot.MaxWidth = isWide ? 1000 : 700;
    }
}
