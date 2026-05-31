using Microsoft.UI.Xaml;

namespace Rokurics.Views;

public record SettingsRowItem(string Label, string Value, string ActionTag, bool ShowSeparator = true)
{
    public Visibility SeparatorVisibility => ShowSeparator ? Visibility.Visible : Visibility.Collapsed;
}
