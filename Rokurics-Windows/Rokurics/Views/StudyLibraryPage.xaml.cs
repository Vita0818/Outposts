using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rokurics.Models;
using Rokurics.ViewModels;

namespace Rokurics.Views;

public sealed partial class StudyLibraryPage : Page
{
    internal StudyLibraryViewModel ViewModel { get; }

    public StudyLibraryPage(StudyLibraryViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private void GoBack_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GoBackCommand.Execute(null);
    }

    private void Breadcrumb_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton btn && btn.DataContext is ValueTuple<string, StudyBrowserPath> crumb)
        {
            // Navigate to breadcrumb path
            ViewModel.CurrentPath = crumb.Path;
            ViewModel.Refresh();
        }
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is StudyItemMetadata item)
        {
            ViewModel.DeleteItemCommand.Execute(item);
        }
    }
}
