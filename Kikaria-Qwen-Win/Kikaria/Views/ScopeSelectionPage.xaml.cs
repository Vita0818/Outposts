using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Kikaria.ViewModels;

namespace Kikaria.Views;

public sealed partial class ScopeSelectionPage : Page
{
    private MainViewModel VM => App.MainViewModel;

    public ScopeSelectionPage()
    {
        InitializeComponent();
        Loaded += ScopeSelectionPage_Loaded;
    }

    private void ScopeSelectionPage_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshTags();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefreshTags();
    }

    private void RefreshTags()
    {
        var tags = VM.AllTags;
        SubtitleText.Text = VM.SelectedTags.Count > 0
            ? $"已选择 {VM.SelectedTags.Count} 个标签"
            : "选择标签来筛选知识点";
        TagGrid.ItemsSource = tags;
        EmptyState.Visibility = tags.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text;
        var tags = VM.AllTags;
        if (!string.IsNullOrEmpty(query))
            tags = tags.Where(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        TagGrid.ItemsSource = tags;
        EmptyState.Visibility = tags.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TagChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            VM.ToggleTagSelection(tag);
            SubtitleText.Text = VM.SelectedTags.Count > 0
                ? $"已选择 {VM.SelectedTags.Count} 个标签"
                : "选择标签来筛选知识点";

            if (btn.Content is Border border)
            {
                bool selected = VM.IsTagSelected(tag);
                border.Background = selected
                    ? new SolidColorBrush(Color.FromArgb(255, 100, 186, 245))
                    : new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
                if (border.Child is TextBlock tb)
                {
                    tb.Foreground = selected
                        ? new SolidColorBrush(Colors.White)
                        : new SolidColorBrush(Color.FromArgb(255, 56, 152, 236));
                }
            }
        }
    }

    private void DoneButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool isWide = e.NewSize.Width >= 800;
        ContentRoot.MaxWidth = isWide ? 1000 : 700;
    }
}
