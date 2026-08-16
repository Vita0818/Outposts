using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Kikaria.App.Controls;

/// <summary>
/// 混排文字与公式的自定义控件:
/// - 普通文本段:Microsoft YaHei UI;
/// - $..$ 行内公式:MathFallback 可读文本,Microsoft Serif UI 斜体;
/// - $$..$$ 块级公式:单独居中段落,块级字号(min(max(f*1.34, f+5), f+8))。
/// 解析词法与 fallback 文本与 Apple 版 KikariaMathText / KikariaMathFormulaView 对齐。
/// </summary>
public sealed partial class MathText : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(MathText),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty MathFontSizeProperty =
        DependencyProperty.Register(nameof(MathFontSize), typeof(double), typeof(MathText),
            new PropertyMetadata(16.0, OnTextChanged));

    private static readonly FontFamily SansFamily = new(Theme.SansFont);
    private static readonly FontFamily SerifFamily = new(Theme.SerifFont);

    public MathText()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
        Unloaded += (_, _) => RichText.Blocks.Clear();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>正文字号(公式按 Apple 版比例放大,块级居中)。</summary>
    public double MathFontSize
    {
        get => (double)GetValue(MathFontSizeProperty);
        set => SetValue(MathFontSizeProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MathText)d).Rebuild();
    }

    private void Rebuild()
    {
        RichText.Blocks.Clear();
        var text = Text ?? string.Empty;
        var tokens = LatexParser.Tokenize(text);

        // 按换行切段;行内公式与文字混排;块级公式单独成段居中。
        var paragraph = new Paragraph();
        var hasContent = false;

        void FlushParagraph()
        {
            if (hasContent)
            {
                RichText.Blocks.Add(paragraph);
                paragraph = new Paragraph();
                hasContent = false;
            }
        }

        foreach (var token in tokens)
        {
            switch (token.Kind)
            {
                case LatexTokenKind.Text:
                    var parts = token.Body.Split('\n');
                    for (var i = 0; i < parts.Length; i++)
                    {
                        if (i > 0)
                        {
                            FlushParagraph();
                        }

                        if (parts[i].Length > 0)
                        {
                            AppendPlainRun(paragraph, parts[i]);
                            hasContent = true;
                        }
                    }

                    break;

                case LatexTokenKind.InlineMath:
                    AppendFormulaRun(paragraph, MathFallback.InlineReadable(token.Body), MathFontSize * 1.02);
                    hasContent = true;
                    break;

                case LatexTokenKind.BlockMath:
                    FlushParagraph();
                    var blockParagraph = new Paragraph { TextAlignment = TextAlignment.Center };
                    AppendFormulaRun(blockParagraph, MathFallback.Readable(token.Body), BlockFontSize());
                    RichText.Blocks.Add(blockParagraph);
                    break;
            }
        }

        FlushParagraph();
        RichText.LineHeight = Math.Max(MathFontSize * 1.6, 20);
    }

    private double BlockFontSize()
    {
        // Apple:blockFontSize = min(max(fontSize*1.34, fontSize+5), fontSize+8)。
        return Math.Min(Math.Max(MathFontSize * 1.34, MathFontSize + 5), MathFontSize + 8);
    }

    private void AppendPlainRun(Paragraph paragraph, string value)
    {
        paragraph.Inlines.Add(new Run
        {
            Text = value,
            FontFamily = SansFamily,
            FontSize = MathFontSize
        });
    }

    private void AppendFormulaRun(Paragraph paragraph, string fallbackText, double fontSize)
    {
        var lines = fallbackText.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            if (lines[i].Length == 0)
            {
                continue;
            }

            paragraph.Inlines.Add(new Run
            {
                Text = lines[i],
                FontFamily = SerifFamily,
                FontSize = fontSize,
                FontStyle = Windows.UI.Text.FontStyle.Italic
            });
        }
    }
}
