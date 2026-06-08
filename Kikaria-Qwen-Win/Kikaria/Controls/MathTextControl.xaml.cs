using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Kikaria.Helpers;

namespace Kikaria.Controls
{
    public sealed partial class MathTextControl : UserControl
    {
        private static readonly Dictionary<string, string> MathFallbackMap = new()
        {
            { @"\alpha", "\u03B1" }, { @"\beta", "\u03B2" }, { @"\gamma", "\u03B3" },
            { @"\delta", "\u03B4" }, { @"\epsilon", "\u03B5" }, { @"\zeta", "\u03B6" },
            { @"\eta", "\u03B7" }, { @"\theta", "\u03B8" }, { @"\iota", "\u03B9" },
            { @"\kappa", "\u03BA" }, { @"\lambda", "\u03BB" }, { @"\mu", "\u03BC" },
            { @"\nu", "\u03BD" }, { @"\xi", "\u03BE" }, { @"\pi", "\u03C0" },
            { @"\rho", "\u03C1" }, { @"\sigma", "\u03C3" }, { @"\tau", "\u03C4" },
            { @"\upsilon", "\u03C5" }, { @"\phi", "\u03C6" }, { @"\chi", "\u03C7" },
            { @"\psi", "\u03C8" }, { @"\omega", "\u03C9" },
            { @"\Alpha", "\u0391" }, { @"\Beta", "\u0392" }, { @"\Gamma", "\u0393" },
            { @"\Delta", "\u0394" }, { @"\Theta", "\u0398" }, { @"\Lambda", "\u039B" },
            { @"\Xi", "\u039E" }, { @"\Pi", "\u03A0" }, { @"\Sigma", "\u03A3" },
            { @"\Phi", "\u03A6" }, { @"\Psi", "\u03A8" }, { @"\Omega", "\u03A9" },
            { @"\infty", "\u221E" }, { @"\partial", "\u2202" }, { @"\nabla", "\u2207" },
            { @"\sum", "\u2211" }, { @"\prod", "\u220F" }, { @"\int", "\u222B" },
            { @"\sqrt", "\u221A" }, { @"\pm", "\u00B1" }, { @"\times", "\u00D7" },
            { @"\div", "\u00F7" }, { @"\cdot", "\u00B7" }, { @"\leq", "\u2264" },
            { @"\geq", "\u2265" }, { @"\neq", "\u2260" }, { @"\approx", "\u2248" },
            { @"\equiv", "\u2261" }, { @"\in", "\u2208" }, { @"\notin", "\u2209" },
            { @"\subset", "\u2282" }, { @"\supset", "\u2283" }, { @"\cup", "\u222A" },
            { @"\cap", "\u2229" }, { @"\forall", "\u2200" }, { @"\exists", "\u2203" },
            { @"\rightarrow", "\u2192" }, { @"\leftarrow", "\u2190" },
            { @"\Rightarrow", "\u21D2" }, { @"\Leftarrow", "\u21D0" },
            { @"\lim", "lim" }, { @"\sin", "sin" }, { @"\cos", "cos" },
            { @"\tan", "tan" }, { @"\log", "log" }, { @"\ln", "ln" },
            { @"\exp", "exp" }, { @"\max", "max" }, { @"\min", "min" },
            { @"\sup", "sup" }, { @"\inf", "inf" },
            { @"\ldots", "\u2026" }, { @"\cdots", "\u22EF" },
            { @"\langle", "\u27E8" }, { @"\rangle", "\u27E9" },
            { @"\lfloor", "\u230A" }, { @"\rfloor", "\u230B" },
            { @"\lceil", "\u2308" }, { @"\rceil", "\u2309" },
            { @"\hat", "^" }, { @"\bar", "\u0304" }, { @"\vec", "\u20D7" },
            { @"\text", "" }, { @"\mathrm", "" }, { @"\mathbf", "" },
            { @"\left", "" }, { @"\right", "" },
            { @"\\", "\n" }, { @"\,", " " }, { @"\;", "  " }, { @"\quad", "    " }
        };

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(MathTextControl),
                new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        public static readonly DependencyProperty MathFontSizeProperty =
            DependencyProperty.Register(nameof(MathFontSize), typeof(double), typeof(MathTextControl),
                new PropertyMetadata(16.0, OnTextPropertyChanged));

        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register(nameof(TextColor), typeof(Color), typeof(MathTextControl),
                new PropertyMetadata(Color.FromArgb(255, 28, 32, 40), OnTextPropertyChanged));

        public static readonly DependencyProperty AccentColorProperty =
            DependencyProperty.Register(nameof(AccentColor), typeof(Color), typeof(MathTextControl),
                new PropertyMetadata(KikariaTheme.SkyLight, OnTextPropertyChanged));

        public static readonly DependencyProperty LineSpacingProperty =
            DependencyProperty.Register(nameof(LineSpacing), typeof(double), typeof(MathTextControl),
                new PropertyMetadata(4.0, OnTextPropertyChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public double MathFontSize
        {
            get => (double)GetValue(MathFontSizeProperty);
            set => SetValue(MathFontSizeProperty, value);
        }

        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public Color AccentColor
        {
            get => (Color)GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }

        public double LineSpacing
        {
            get => (double)GetValue(LineSpacingProperty);
            set => SetValue(LineSpacingProperty, value);
        }

        public MathTextControl()
        {
            InitializeComponent();
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MathTextControl control)
            {
                control.RenderContent();
            }
        }

        private void RenderContent()
        {
            ContentPanel.Children.Clear();

            if (string.IsNullOrEmpty(Text))
                return;

            var blocks = MathTextParser.ParsedContentFor(Text);

            foreach (var block in blocks)
            {
                switch (block)
                {
                    case MathTextBlock.Paragraph para:
                        RenderParagraph(para);
                        break;
                    case MathTextBlock.BlockMath blockMath:
                        RenderBlockMath(blockMath);
                        break;
                    case MathTextBlock.BlankLine:
                        RenderBlankLine();
                        break;
                }
            }
        }

        private void RenderParagraph(MathTextBlock.Paragraph para)
        {
            var textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = MathFontSize,
                Foreground = new SolidColorBrush(TextColor),
                LineHeight = MathFontSize + LineSpacing,
                FontFamily = KikariaTypography.ChineseBodyFont
            };

            var items = MathTextParser.InlineItemsFromSegments(para.Segments);

            foreach (var item in items)
            {
                switch (item)
                {
                    case InlineMathItem.Text textItem:
                        var textRun = new Run { Text = textItem.Value };
                        if (KikariaTypography.ContainsChinese(textItem.Value))
                            textRun.FontFamily = KikariaTypography.ChineseBodyFont;
                        textBlock.Inlines.Add(textRun);
                        break;

                    case InlineMathItem.InlineMath mathItem:
                        string fallback = ReadableMathFallback(mathItem.Body);
                        var mathRun = new Run
                        {
                            Text = fallback,
                            FontStyle = Windows.UI.Text.FontStyle.Italic,
                            FontFamily = new FontFamily("Cambria Math, Cascadia Code, Consolas")
                        };
                        mathRun.Foreground = new SolidColorBrush(AccentColor);
                        textBlock.Inlines.Add(mathRun);
                        break;

                    case InlineMathItem.InlineMathWithTrailingText trailingItem:
                        string trailingFallback = ReadableMathFallback(trailingItem.Body);
                        var trailingMathRun = new Run
                        {
                            Text = trailingFallback,
                            FontStyle = Windows.UI.Text.FontStyle.Italic,
                            FontFamily = new FontFamily("Cambria Math, Cascadia Code, Consolas")
                        };
                        trailingMathRun.Foreground = new SolidColorBrush(AccentColor);
                        textBlock.Inlines.Add(trailingMathRun);

                        var trailingRun = new Run { Text = trailingItem.TrailingText };
                        textBlock.Inlines.Add(trailingRun);
                        break;
                }
            }

            ContentPanel.Children.Add(textBlock);
        }

        private void RenderBlockMath(MathTextBlock.BlockMath blockMath)
        {
            string fallback = ReadableMathFallback(blockMath.Body);

            var container = new Border
            {
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 8, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var textBlock = new TextBlock
            {
                Text = fallback,
                TextWrapping = TextWrapping.Wrap,
                FontSize = MathFontSize * 1.15,
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                FontFamily = new FontFamily("Cambria Math, Cascadia Code, Consolas"),
                Foreground = new SolidColorBrush(AccentColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                LineHeight = (MathFontSize * 1.15) + LineSpacing + 2
            };

            container.Child = textBlock;
            ContentPanel.Children.Add(container);
        }

        private void RenderBlankLine()
        {
            var spacer = new Border
            {
                Height = LineSpacing + 4,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            ContentPanel.Children.Add(spacer);
        }

        public static string ReadableMathFallback(string latexBody)
        {
            if (string.IsNullOrEmpty(latexBody))
                return string.Empty;

            string result = latexBody;

            result = result.Replace(@"\frac", "");

            foreach (var kvp in MathFallbackMap)
            {
                if (result.Contains(kvp.Key))
                {
                    result = result.Replace(kvp.Key, kvp.Value);
                }
            }

            result = result.Replace("{", "").Replace("}", "");
            result = result.Replace("^", "^").Replace("_", "_");
            result = result.Replace(@"\", "");

            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
            return result.Trim();
        }
    }
}
