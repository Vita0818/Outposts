using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Rokurics.Helpers;
using Rokurics.Models;

namespace Rokurics.Converters;

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : value;
}

/// <summary>
/// Returns Collapsed when value is null, Visible otherwise.
/// </summary>
public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is null || (value is string s && string.IsNullOrEmpty(s))
            ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns HorizontalAlignment based on ChatMessageRole.
/// </summary>
public class ChatRoleToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is ChatMessageRole.User ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns background brush based on ChatMessageRole.
/// </summary>
public class ChatRoleToBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush AssistantBubbleBrush = new(Color.FromArgb(60, 128, 128, 128));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ChatMessageRole.User)
            return RokuricsColors.ActionGradientBrush;
        return AssistantBubbleBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
