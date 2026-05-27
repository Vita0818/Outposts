using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Rokurics.Views;

public sealed partial class SidebarView : UserControl
{
    public event Action<string>? SelectionChanged;

    public string UserDisplayName => Environment.UserName;
    public string UserInitials => (Environment.UserName?.FirstOrDefault() ?? 'U').ToString().ToUpper();
    public string UserHandle => $"@{Environment.UserName?.ToLowerInvariant() ?? "user"}";

    public SidebarView()
    {
        InitializeComponent();
    }

    private void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (SidebarItemsList.SelectedItem is ListViewItem item && item.Tag is string tag)
        {
            SelectionChanged?.Invoke(tag);
        }
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        SelectionChanged?.Invoke("settings");
        // Also deselect sidebar items since settings is separate
        SidebarItemsList.SelectedItem = null;
    }
}
