using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using Kikaria.Helpers;
using Windows.Foundation;

namespace Kikaria.Controls
{
    public class LiquidGlassCard : ContentControl
    {
        public static readonly DependencyProperty CardCornerRadiusProperty =
            DependencyProperty.Register(nameof(CardCornerRadius), typeof(double), typeof(LiquidGlassCard),
                new PropertyMetadata(20.0, OnPropertyChanged));

        public static readonly DependencyProperty FillOpacityProperty =
            DependencyProperty.Register(nameof(FillOpacity), typeof(double), typeof(LiquidGlassCard),
                new PropertyMetadata(0.7, OnPropertyChanged));

        public static readonly DependencyProperty StrokeOpacityProperty =
            DependencyProperty.Register(nameof(StrokeOpacity), typeof(double), typeof(LiquidGlassCard),
                new PropertyMetadata(0.25, OnPropertyChanged));

        public static readonly DependencyProperty ShadowOpacityProperty =
            DependencyProperty.Register(nameof(ShadowOpacity), typeof(double), typeof(LiquidGlassCard),
                new PropertyMetadata(0.15, OnPropertyChanged));

        public static readonly DependencyProperty ShadowRadiusProperty =
            DependencyProperty.Register(nameof(ShadowRadius), typeof(double), typeof(LiquidGlassCard),
                new PropertyMetadata(20.0, OnPropertyChanged));

        public static readonly DependencyProperty ShadowYProperty =
            DependencyProperty.Register(nameof(ShadowY), typeof(double), typeof(LiquidGlassCard),
                new PropertyMetadata(8.0, OnPropertyChanged));

        public static readonly DependencyProperty IsDarkModeProperty =
            DependencyProperty.Register(nameof(IsDarkMode), typeof(bool), typeof(LiquidGlassCard),
                new PropertyMetadata(false, OnPropertyChanged));

        public static readonly DependencyProperty AccentColorProperty =
            DependencyProperty.Register(nameof(AccentColor), typeof(Color), typeof(LiquidGlassCard),
                new PropertyMetadata(KikariaTheme.SkyLight, OnPropertyChanged));

        public double CardCornerRadius
        {
            get => (double)GetValue(CardCornerRadiusProperty);
            set => SetValue(CardCornerRadiusProperty, value);
        }

        public double FillOpacity
        {
            get => (double)GetValue(FillOpacityProperty);
            set => SetValue(FillOpacityProperty, value);
        }

        public double StrokeOpacity
        {
            get => (double)GetValue(StrokeOpacityProperty);
            set => SetValue(StrokeOpacityProperty, value);
        }

        public double ShadowOpacity
        {
            get => (double)GetValue(ShadowOpacityProperty);
            set => SetValue(ShadowOpacityProperty, value);
        }

        public double ShadowRadius
        {
            get => (double)GetValue(ShadowRadiusProperty);
            set => SetValue(ShadowRadiusProperty, value);
        }

        public double ShadowY
        {
            get => (double)GetValue(ShadowYProperty);
            set => SetValue(ShadowYProperty, value);
        }

        public bool IsDarkMode
        {
            get => (bool)GetValue(IsDarkModeProperty);
            set => SetValue(IsDarkModeProperty, value);
        }

        public Color AccentColor
        {
            get => (Color)GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }

        private Grid _rootGrid = null!;
        private Grid _shadowGrid = null!;
        private Grid _contentGrid = null!;
        private Border _border = null!;

        public LiquidGlassCard()
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BuildVisualTree();
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LiquidGlassCard card && card._rootGrid != null)
            {
                card.BuildVisualTree();
            }
        }

        private void BuildVisualTree()
        {
            double radius = CardCornerRadius;
            bool dark = IsDarkMode;
            double fillOp = dark ? FillOpacity * 0.65 : FillOpacity;
            double strokeOp = dark ? StrokeOpacity * 0.8 : StrokeOpacity;
            double shadowOp = dark ? ShadowOpacity * 0.6 : ShadowOpacity;

            Color surfaceColor = KikariaTheme.GetColor(KikariaThemeColor.GlassSurface, dark);
            Color fillColor = Color.FromArgb((byte)(surfaceColor.A * fillOp), surfaceColor.R, surfaceColor.G, surfaceColor.B);

            Color accent = AccentColor;
            Color strokeStart = Color.FromArgb((byte)(255 * strokeOp), 255, 255, 255);
            Color strokeEnd = Color.FromArgb((byte)(accent.A * strokeOp), accent.R, accent.G, accent.B);

            var strokeGradient = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };
            strokeGradient.GradientStops.Add(new GradientStop { Color = strokeStart, Offset = 0.0 });
            strokeGradient.GradientStops.Add(new GradientStop { Color = strokeEnd, Offset = 1.0 });

            Color shadowColor = KikariaTheme.GetColor(KikariaThemeColor.Shadow, dark);
            Color finalShadow = Color.FromArgb((byte)(shadowColor.A * shadowOp / 0.15), shadowColor.R, shadowColor.G, shadowColor.B);

            var shadowBrush = new SolidColorBrush(finalShadow);

            _shadowGrid = new Grid
            {
                CornerRadius = new CornerRadius(radius),
                Background = shadowBrush,
                Margin = new Thickness(0, 0, 0, 0),
                Opacity = shadowOp
            };
            _shadowGrid.RenderTransform = new TranslateTransform { X = 0, Y = ShadowY };
            _shadowGrid.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);

            var acrylicBrush = new AcrylicBrush
            {
                TintColor = dark ? Color.FromArgb(255, 28, 32, 40) : Color.FromArgb(255, 248, 248, 252),
                TintOpacity = fillOp,
                FallbackColor = fillColor
            };

            _border = new Border
            {
                CornerRadius = new CornerRadius(radius),
                Background = acrylicBrush,
                BorderBrush = strokeGradient,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0)
            };

            _contentGrid = new Grid();
            var contentPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalContentAlignment,
                VerticalAlignment = VerticalContentAlignment
            };
            contentPresenter.SetBinding(ContentPresenter.ContentProperty,
                new Microsoft.UI.Xaml.Data.Binding { Source = this, Path = new PropertyPath("Content") });
            contentPresenter.SetBinding(ContentPresenter.PaddingProperty,
                new Microsoft.UI.Xaml.Data.Binding { Source = this, Path = new PropertyPath("Padding") });
            _contentGrid.Children.Add(contentPresenter);
            _border.Child = _contentGrid;

            _rootGrid = new Grid();
            _rootGrid.Children.Add(_shadowGrid);
            _rootGrid.Children.Add(_border);

            var rootContent = new Grid();
            rootContent.Children.Add(_rootGrid);
            base.Content = rootContent;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            BuildVisualTree();
        }
    }

    public class LiquidGlassCapsule : ContentControl
    {
        public static readonly DependencyProperty IsDarkModeProperty =
            DependencyProperty.Register(nameof(IsDarkMode), typeof(bool), typeof(LiquidGlassCapsule),
                new PropertyMetadata(false));

        public static readonly DependencyProperty FillOpacityProperty =
            DependencyProperty.Register(nameof(FillOpacity), typeof(double), typeof(LiquidGlassCapsule),
                new PropertyMetadata(0.6));

        public bool IsDarkMode
        {
            get => (bool)GetValue(IsDarkModeProperty);
            set => SetValue(IsDarkModeProperty, value);
        }

        public double FillOpacity
        {
            get => (double)GetValue(FillOpacityProperty);
            set => SetValue(FillOpacityProperty, value);
        }

        public LiquidGlassCapsule()
        {
            HorizontalContentAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            BuildVisual();
            return base.MeasureOverride(availableSize);
        }

        private void BuildVisual()
        {
            bool dark = IsDarkMode;
            double fillOp = dark ? FillOpacity * 0.65 : FillOpacity;

            Color surfaceColor = KikariaTheme.GetColor(KikariaThemeColor.GlassSurface, dark);
            Color fillColor = Color.FromArgb((byte)(surfaceColor.A * fillOp), surfaceColor.R, surfaceColor.G, surfaceColor.B);

            Color accent = KikariaTheme.GetColor(KikariaThemeColor.GlassStrokeAccent, dark);
            Color strokeStart = Color.FromArgb(100, 255, 255, 255);
            Color strokeEnd = Color.FromArgb(accent.A, accent.R, accent.G, accent.B);

            var strokeGradient = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };
            strokeGradient.GradientStops.Add(new GradientStop { Color = strokeStart, Offset = 0.0 });
            strokeGradient.GradientStops.Add(new GradientStop { Color = strokeEnd, Offset = 1.0 });

            var border = new Border
            {
                CornerRadius = new CornerRadius(999),
                Background = new SolidColorBrush(fillColor),
                BorderBrush = strokeGradient,
                BorderThickness = new Thickness(1),
                Padding = Padding
            };

            var cp = new ContentPresenter
            {
                HorizontalAlignment = HorizontalContentAlignment,
                VerticalAlignment = VerticalContentAlignment
            };
            cp.SetBinding(ContentPresenter.ContentProperty,
                new Microsoft.UI.Xaml.Data.Binding { Source = this, Path = new PropertyPath("Content") });
            border.Child = cp;

            base.Content = border;
        }
    }

    public class LiquidGlassCircle : ContentControl
    {
        public static readonly DependencyProperty DiameterProperty =
            DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(LiquidGlassCircle),
                new PropertyMetadata(60.0));

        public static readonly DependencyProperty IsDarkModeProperty =
            DependencyProperty.Register(nameof(IsDarkMode), typeof(bool), typeof(LiquidGlassCircle),
                new PropertyMetadata(false));

        public double Diameter
        {
            get => (double)GetValue(DiameterProperty);
            set => SetValue(DiameterProperty, value);
        }

        public bool IsDarkMode
        {
            get => (bool)GetValue(IsDarkModeProperty);
            set => SetValue(IsDarkModeProperty, value);
        }

        public LiquidGlassCircle()
        {
            HorizontalContentAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            BuildVisual();
            return base.MeasureOverride(availableSize);
        }

        private void BuildVisual()
        {
            bool dark = IsDarkMode;
            double d = Diameter;

            Color surfaceColor = KikariaTheme.GetColor(KikariaThemeColor.GlassSurface, dark);
            double fillOp = dark ? 0.45 : 0.7;
            Color fillColor = Color.FromArgb((byte)(surfaceColor.A * fillOp), surfaceColor.R, surfaceColor.G, surfaceColor.B);

            Color accent = KikariaTheme.GetColor(KikariaThemeColor.GlassStrokeAccent, dark);
            Color strokeStart = Color.FromArgb(100, 255, 255, 255);
            Color strokeEnd = Color.FromArgb(accent.A, accent.R, accent.G, accent.B);

            var strokeGradient = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };
            strokeGradient.GradientStops.Add(new GradientStop { Color = strokeStart, Offset = 0.0 });
            strokeGradient.GradientStops.Add(new GradientStop { Color = strokeEnd, Offset = 1.0 });

            var border = new Border
            {
                Width = d,
                Height = d,
                CornerRadius = new CornerRadius(d / 2),
                Background = new SolidColorBrush(fillColor),
                BorderBrush = strokeGradient,
                BorderThickness = new Thickness(1)
            };

            var cp = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            cp.SetBinding(ContentPresenter.ContentProperty,
                new Microsoft.UI.Xaml.Data.Binding { Source = this, Path = new PropertyPath("Content") });
            border.Child = cp;

            base.Content = border;
        }
    }
}
