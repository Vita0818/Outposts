using Intatis.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Intatis.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public string ConfigPathText => ViewModel.ConfigPath;

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = new SettingsViewModel(App.Current.Environment);
    }

    private void OnProviderSelected(object sender, SelectionChangedEventArgs e)
    {
        // TwoWay SelectedItem binding updates the ViewModel.
    }

    private async void OnAddProviderClick(object sender, RoutedEventArgs e)
    {
        var idBox = new TextBox
        {
            Header = "Provider id (as used in <provider>/<model> routes)",
            PlaceholderText = "openai",
        };
        var dialog = new ContentDialog
        {
            Title = "Add provider",
            Content = idBox,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };
        if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            ViewModel.AddProvider(idBox.Text);
    }

    private async void OnAddModelClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProvider is not { } provider) return;
        var idBox = new TextBox
        {
            Header = "Model id",
            PlaceholderText = "gpt-4o-mini",
        };
        var dialog = new ContentDialog
        {
            Title = $"Add model to {provider.Id}",
            Content = idBox,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };
        if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            ViewModel.AddModel(provider, idBox.Text);
    }

    private void OnDeleteModelClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ModelRowVm model }) return;
        if (ViewModel.SelectedProvider is { } provider)
            ViewModel.DeleteModel(provider, model);
    }

    private void OnDeleteProviderClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProvider is { } provider)
            ViewModel.DeleteProvider(provider);
    }

    private void OnSaveClick(object sender, RoutedEventArgs e) => ViewModel.Save();

    private async void OnTestProviderClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProvider is { } provider)
            await ViewModel.TestProviderAsync(provider);
    }

    private void OnReloadClick(object sender, RoutedEventArgs e)
    {
        App.Current.Environment.ReloadConfig();
        ViewModel.Reload();
        RaiseConfigPath();
    }

    private void OnRevealConfigClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = ViewModel.ConfigPath;
            var argument = File.Exists(path)
                ? $"/select,\"{path}\""
                : $"/select,\"{Path.GetDirectoryName(path)}\"";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = argument,
                UseShellExecute = true,
            });
        }
        catch (Exception)
        {
            ViewModel.StatusText = "could not open Explorer";
        }
    }

    private void RaiseConfigPath() => Raise(nameof(ConfigPathText));

    private void Raise(string name)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
