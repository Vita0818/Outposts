using Microsoft.UI.Xaml.Controls;
using Rokurics.ViewModels;

namespace Rokurics.Views;

public sealed partial class SettingsPage : Page
{
    internal SettingsViewModel ViewModel { get; }

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }
}
