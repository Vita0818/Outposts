//
//  Toast.cs
//  Kikaria-Windows
//
//  顶部 Toast 通知层,2 秒后消失(对齐 Apple 版 KikariaToastLayer)。
//  用 Popup 悬浮在窗口顶部中央,跨页面复用。
//

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Kikaria.App.Controls;

public static class Toast
{
    private static Popup? _popup;
    private static DispatcherQueueTimer? _timer;

    /// <summary>显示一条 Toast,2 秒后自动消失;新 Toast 会顶替旧的。</summary>
    public static void Show(string message)
    {
        var host = MainWindow.Instance?.Content as FrameworkElement;
        var root = host?.XamlRoot;
        if (root is null)
        {
            return;
        }

        var textBlock = new TextBlock
        {
            Text = message,
            Style = (Style)Application.Current.Resources["HeadlineStyle"],
            Foreground = Theme.ThemedBrush(host, "DeepTextBrush"),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
            FontSize = 14
        };

        var card = new Border
        {
            Child = textBlock,
            CornerRadius = new CornerRadius(22),
            Background = Theme.ThemedBrush(host, "GlassCardBackgroundBrush"),
            BorderBrush = Theme.ThemedBrush(host, "GlassCardStrokeBrush"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18, 13, 18, 13),
            MaxWidth = 380
        };

        if (_popup is not null)
        {
            _popup.IsOpen = false;
            _popup = null;
        }

        _timer?.Stop();

        _popup = new Popup
        {
            XamlRoot = root,
            Child = card,
            HorizontalOffset = 0,
            VerticalOffset = 56
        };
        _popup.IsOpen = true;

        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.Tick += (_, _) =>
        {
            if (_popup is not null)
            {
                _popup.IsOpen = false;
                _popup = null;
            }

            _timer?.Stop();
        };
        _timer.Start();
    }
}
