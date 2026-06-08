using System;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace Kikaria.Helpers
{
    public static class KikariaTypography
    {
        private const string ChineseFontFamily = "PingFang SC";
        private const string ChineseFallbackFont = "Microsoft YaHei";
        private const string LatinFontFamily = "SF Pro Display";
        private const string LatinFallbackFont = "Segoe UI";
        private const string MonoFontFamily = "SF Mono";
        private const string MonoFallbackFont = "Cascadia Code";
        private const string SerifFontFamily = "Georgia";
        private const string SerifFallbackFont = "Palatino Linotype";

        private static readonly char[] ChinesePunctuationChars =
        {
            '\u3002', '\uFF0C', '\uFF01', '\uFF1F', '\uFF1B', '\uFF1A',
            '\u3001', '\u2018', '\u2019', '\u201C', '\u201D',
            '\u300A', '\u300B', '\u3010', '\u3011',
            '\u2014', '\u2026', '\u00B7',
            '\uFF08', '\uFF09', '\u3008', '\u3009',
            '\u300C', '\u300D', '\u300E', '\u300F',
            '\uFFE5'
        };

        private static readonly HashSet<char> ChinesePunctuationSet = new(ChinesePunctuationChars);

        // MARK: - Font Creation Methods

        public static FontFamily AppTitleFont => ResolveFont(SerifFontFamily, SerifFallbackFont);
        public static double AppTitleSize => 39;

        public static FontFamily ChineseLargeTitleFont => ResolveFont(ChineseFontFamily, ChineseFallbackFont);
        public static double ChineseLargeTitleSize => 34;

        public static FontFamily ChineseTitleFont => ResolveFont(ChineseFontFamily, ChineseFallbackFont);
        public static double ChineseTitleSize => 32;

        public static FontFamily ChineseHeadlineFont => ResolveFont(ChineseFontFamily, ChineseFallbackFont);
        public static double ChineseHeadlineSize => 17;

        public static FontFamily ChineseBodyFont => ResolveFont(ChineseFontFamily, ChineseFallbackFont);
        public static double ChineseBodySize => 15;

        public static FontFamily ChineseButtonFont => ResolveFont(ChineseFontFamily, ChineseFallbackFont);
        public static double ChineseButtonSize => 17;

        public static FontFamily ChineseCaptionFont => ResolveFont(ChineseFontFamily, ChineseFallbackFont);
        public static double ChineseCaptionSize => 12;

        public static FontFamily TagFont => ResolveFont(LatinFontFamily, LatinFallbackFont);
        public static double TagSize => 12;

        public static FontFamily NumberFont => ResolveFont(SerifFontFamily, SerifFallbackFont);
        public static double NumberSizeFor(double size) => size;

        // MARK: - Font Resolution

        private static FontFamily ResolveFont(string primary, string fallback)
        {
            try
            {
                return new FontFamily($"{primary}, {fallback}");
            }
            catch
            {
                return new FontFamily(fallback);
            }
        }

        // MARK: - Mixed Text Support

        public static bool ContainsChinese(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (char c in text)
            {
                if (IsChineseCharacter(c))
                    return true;
            }
            return false;
        }

        public static bool IsChineseCharacter(char c)
        {
            int code = c;
            return (code >= 0x3000 && code <= 0x303F) ||
                   (code >= 0x3400 && code <= 0x4DBF) ||
                   (code >= 0x4E00 && code <= 0x9FFF) ||
                   (code >= 0xF900 && code <= 0xFAFF) ||
                   (code >= 0xFF00 && code <= 0xFFEF) ||
                   (code >= 0x20000 && code <= 0x2A6DF) ||
                   (code >= 0x2A700 && code <= 0x2B73F) ||
                   (code >= 0x2B740 && code <= 0x2B81F) ||
                   (code >= 0x2B820 && code <= 0x2CEAF) ||
                   (code >= 0x2CEB0 && code <= 0x2EBEF);
        }

        public static bool IsChinesePunctuation(char c)
        {
            return ChinesePunctuationSet.Contains(c);
        }

        public static bool IsChineseOrPunctuation(char c)
        {
            return IsChineseCharacter(c) || IsChinesePunctuation(c);
        }

        public static TextBlock CreateMixedTextBlock(
            string text,
            double fontSize,
            FontFamily? defaultFont = null,
            FontWeight fontWeight = default,
            Brush? foreground = null)
        {
            var textBlock = new TextBlock
            {
                FontSize = fontSize,
                FontWeight = fontWeight == default ? FontWeights.Normal : fontWeight,
                TextWrapping = TextWrapping.Wrap
            };

            if (foreground != null)
                textBlock.Foreground = foreground;

            if (string.IsNullOrEmpty(text))
            {
                textBlock.Text = text ?? string.Empty;
                return textBlock;
            }

            var segments = SplitByScript(text);

            if (segments.Count == 1 && !ContainsChinese(segments[0].text))
            {
                textBlock.FontFamily = defaultFont ?? AppTitleFont;
                textBlock.Text = text;
                return textBlock;
            }

            foreach (var (segment, isChinese) in segments)
            {
                var run = new Run { Text = segment };

                if (isChinese)
                {
                    run.FontFamily = ChineseBodyFont;
                }
                else
                {
                    run.FontFamily = defaultFont ?? AppTitleFont;
                }

                textBlock.Inlines.Add(run);
            }

            return textBlock;
        }

        public static List<(string text, bool isChinese)> SplitByScript(string text)
        {
            var result = new List<(string text, bool isChinese)>();
            if (string.IsNullOrEmpty(text))
                return result;

            var current = new StringBuilder();
            bool? currentIsChinese = null;

            foreach (char c in text)
            {
                bool charIsChinese = IsChineseOrPunctuation(c);

                if (currentIsChinese == null)
                {
                    currentIsChinese = charIsChinese;
                    current.Append(c);
                }
                else if (charIsChinese == currentIsChinese)
                {
                    current.Append(c);
                }
                else
                {
                    if (current.Length > 0)
                        result.Add((current.ToString(), currentIsChinese.Value));

                    current.Clear();
                    current.Append(c);
                    currentIsChinese = charIsChinese;
                }
            }

            if (current.Length > 0 && currentIsChinese != null)
                result.Add((current.ToString(), currentIsChinese.Value));

            return result;
        }

        // MARK: - Convenience Style Application

        public static void ApplyAppTitleStyle(TextBlock textBlock)
        {
            textBlock.FontFamily = AppTitleFont;
            textBlock.FontSize = AppTitleSize;
            textBlock.FontWeight = FontWeights.SemiBold;
        }

        public static void ApplyChineseLargeTitleStyle(TextBlock textBlock)
        {
            textBlock.FontFamily = ChineseLargeTitleFont;
            textBlock.FontSize = ChineseLargeTitleSize;
            textBlock.FontWeight = FontWeights.Bold;
        }

        public static void ApplyChineseTitleStyle(TextBlock textBlock)
        {
            textBlock.FontFamily = ChineseTitleFont;
            textBlock.FontSize = ChineseTitleSize;
            textBlock.FontWeight = FontWeights.Bold;
        }

        public static void ApplyChineseHeadlineStyle(TextBlock textBlock)
        {
            textBlock.FontFamily = ChineseHeadlineFont;
            textBlock.FontSize = ChineseHeadlineSize;
            textBlock.FontWeight = FontWeights.SemiBold;
        }

        public static void ApplyChineseBodyStyle(TextBlock textBlock)
        {
            textBlock.FontFamily = ChineseBodyFont;
            textBlock.FontSize = ChineseBodySize;
            textBlock.FontWeight = FontWeights.Normal;
        }

        public static void ApplyChineseButtonStyle(TextBlock textBlock)
        {
            textBlock.FontFamily = ChineseButtonFont;
            textBlock.FontSize = ChineseButtonSize;
            textBlock.FontWeight = FontWeights.SemiBold;
        }

        public static void ApplyChineseCaptionStyle(TextBlock textBlock)
        {
            textBlock.FontFamily = ChineseCaptionFont;
            textBlock.FontSize = ChineseCaptionSize;
            textBlock.FontWeight = FontWeights.Medium;
        }

        public static void ApplyTagStyle(TextBlock textBlock)
        {
            textBlock.FontFamily = TagFont;
            textBlock.FontSize = TagSize;
            textBlock.FontWeight = FontWeights.SemiBold;
        }

        public static void ApplyNumberStyle(TextBlock textBlock, double size)
        {
            textBlock.FontFamily = NumberFont;
            textBlock.FontSize = NumberSizeFor(size);
            textBlock.FontWeight = FontWeights.SemiBold;
        }
    }
}
