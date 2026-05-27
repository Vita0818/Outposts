using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rokurics.ViewModels;

namespace Rokurics.Views;

public sealed partial class HomePage : Page
{
    internal MainViewModel ViewModel { get; }

    public HomePage(MainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private void ToggleRecording_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleRecordingCommand.Execute(null);
    }

    private void NavigateToLibrary_Click(object sender, RoutedEventArgs e)
    {
        var studyVm = App.Current.Services.GetRequiredService<StudyLibraryViewModel>();
        studyVm.Refresh();
        Frame.Navigate(typeof(StudyLibraryPage), studyVm);
    }

    private void NavigateToChat_Click(object sender, RoutedEventArgs e)
    {
        var chatVm = App.Current.Services.GetRequiredService<ChatViewModel>();
        Frame.Navigate(typeof(ChatPage), chatVm);
    }

    private void NavigateToSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsVm = App.Current.Services.GetRequiredService<SettingsViewModel>();
        Frame.Navigate(typeof(SettingsPage), settingsVm);
    }
}
