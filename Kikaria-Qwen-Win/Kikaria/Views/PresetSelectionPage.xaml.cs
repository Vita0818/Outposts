using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Kikaria.Models;
using Kikaria.ViewModels;

namespace Kikaria.Views;

public sealed partial class PresetSelectionPage : Page
{
    private MainViewModel VM => App.MainViewModel;

    public PresetSelectionPage()
    {
        InitializeComponent();
        Loaded += PresetSelectionPage_Loaded;
    }

    private void PresetSelectionPage_Loaded(object sender, RoutedEventArgs e)
    {
        PresetList.ItemsSource = VM.Presets;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void PresetItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is KnowledgePreset preset)
        {
            VM.SwitchToPreset(preset);
            if (Frame.CanGoBack) Frame.GoBack();
        }
    }

    private void UploadPreset_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(NewPresetPage));
    }

    private void EditPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is KnowledgePreset preset)
        {
            Frame.Navigate(typeof(EditPresetPage), preset);
        }
    }
}
