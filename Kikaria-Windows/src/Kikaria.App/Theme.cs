//
//  Theme.cs
//  Kikaria-Windows
//
//  颜色 / 渐变 / 玻璃卡参数常量(照抄 KikariaTheme)与主题切换感知辅助。
//  具体画刷定义在 App.xaml 的 ThemeDictionaries 中,此处常量供代码侧构造 UI 使用。
//

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Kikaria.App;

public static class Theme
{
    // ---- 玻璃卡参数(照 Apple liquidGlassCard 默认) ----
    public const double GlassCornerRadius = 28;
    public const double GlassFillOpacity = 0.48;
    public const double GlassStrokeOpacity = 0.42;

    // ---- 字体名 ----
    public const string SansFont = "Microsoft YaHei UI";
    public const string SerifFont = "Microsoft Serif UI";

    // ---- 版本信息(设置页显示) ----
    public const string DisplayVersion = "1.0.0";
    public const string BuildVersion = "1";
    public const string VersionText = "1.0.0 (1)";
    public const string CopyrightText = "© 2026 Vita";
    public const string IcpText = "浙ICP备2026034004号";

    /// <summary>当前是否暗色主题(跟随系统,RequestedTheme=Default)。</summary>
    public static bool IsDark(FrameworkElement? element)
    {
        if (element is not null && element.ActualTheme != ElementTheme.Default)
        {
            return element.ActualTheme == ElementTheme.Dark;
        }

        return Application.Current.RequestedTheme == ApplicationTheme.Dark;
    }

    /// <summary>
    /// 按当前主题解析 ThemeDictionaries 中的画刷(代码生成元素用;
    /// 直接走 ThemeDictionaries 字典,确保亮暗正确)。
    /// </summary>
    public static Brush ThemedBrush(FrameworkElement element, string key)
    {
        var themeKey = IsDark(element) ? "Dark" : "Light";
        if (Application.Current.Resources.ThemeDictionaries.TryGetValue(themeKey, out var dictionary) &&
            dictionary is ResourceDictionary themed &&
            themed.TryGetValue(key, out var value) &&
            value is Brush brush)
        {
            return brush;
        }

        if (Application.Current.Resources.TryGetValue(key, out var fallback) && fallback is Brush fallbackBrush)
        {
            return fallbackBrush;
        }

        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 63, 186, 245));
    }

    /// <summary>构造 0,0 → 1,1 的两色渐变画刷(用于代码侧生成的元素)。</summary>
    public static LinearGradientBrush DiagonalGradient(string fromHex, string toHex)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1),
            MappingMode = BrushMappingMode.RelativeToBoundingBox
        };
        brush.GradientStops.Add(new GradientStop { Color = ParseColor(fromHex), Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = ParseColor(toHex), Offset = 1 });
        return brush;
    }

    public static Windows.UI.Color ParseColor(string hex)
    {
        var value = hex.TrimStart('#');
        if (value.Length == 6)
        {
            value = "FF" + value;
        }

        var alpha = Convert.ToByte(value.Substring(0, 2), 16);
        var r = Convert.ToByte(value.Substring(2, 2), 16);
        var g = Convert.ToByte(value.Substring(4, 2), 16);
        var b = Convert.ToByte(value.Substring(6, 2), 16);
        return Windows.UI.Color.FromArgb(alpha, r, g, b);
    }
}
