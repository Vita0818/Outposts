using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Kikaria.App.Controls;

/// <summary>
/// 玻璃拟态卡片:Acrylic 背景 + 渐变描边 + 彩色阴影。
/// 默认参数照 Apple liquidGlassCard:圆角 28、fillOpacity 0.48、stroke 0.42。
/// 通过 CardRadius / ContentPadding 属性定制。
/// </summary>
public sealed partial class GlassCard : UserControl
{
    public static readonly DependencyProperty CardRadiusProperty =
        DependencyProperty.Register(nameof(CardRadius), typeof(CornerRadius), typeof(GlassCard),
            new PropertyMetadata(new CornerRadius(28), OnCardRadiusChanged));

    public static readonly DependencyProperty ContentPaddingProperty =
        DependencyProperty.Register(nameof(ContentPadding), typeof(Thickness), typeof(GlassCard),
            new PropertyMetadata(new Thickness(18), OnContentPaddingChanged));

    public static readonly DependencyProperty ShadowOpacityMultiplierProperty =
        DependencyProperty.Register(nameof(ShadowOpacityMultiplier), typeof(double), typeof(GlassCard),
            new PropertyMetadata(0.12, OnShadowOpacityChanged));

    public GlassCard()
    {
        InitializeComponent();
    }

    public CornerRadius CardRadius
    {
        get => (CornerRadius)GetValue(CardRadiusProperty);
        set => SetValue(CardRadiusProperty, value);
    }

    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    /// <summary>彩色阴影不透明度(0-1,默认 0.12)。</summary>
    public double ShadowOpacityMultiplier
    {
        get => (double)GetValue(ShadowOpacityMultiplierProperty);
        set => SetValue(ShadowOpacityMultiplierProperty, value);
    }

    private static void OnCardRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var card = (GlassCard)d;
        var radius = (CornerRadius)e.NewValue;
        card.CardBorder.CornerRadius = radius;
        card.ShadowBorder.CornerRadius = radius;
    }

    private static void OnContentPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((GlassCard)d).CardContent.Margin = (Thickness)e.NewValue;
    }

    private static void OnShadowOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((GlassCard)d).ShadowBorder.Opacity = (double)e.NewValue;
    }
}
