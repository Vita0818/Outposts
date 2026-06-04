using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace Rokurics.Helpers;

/// <summary>
/// Mirrors MacMixedFontText / MacMixedTextRun from Apple source.
/// Splits text into CJK, Latin, digit, punctuation, and technical runs
/// and renders each with the appropriate font family.
/// </summary>
public static partial class MacMixedFontHelper
{
    private const string SerifFont = "Georgia, Times New Roman, serif";
    private const string DefaultFont = "Microsoft YaHei UI";
    private const string MonoFont = "Cascadia Code, Consolas";

        // CJK Unicode ranges (matching macContainsCJK from MacTypography.swift)
    private static bool IsCJK(char c) => c is
        >= '\u4E00' and <= '\u9FFF' or
        >= '\u3400' and <= '\u4DBF' or
        >= '\u3040' and <= '\u30FF' or
        >= '\uAC00' and <= '\uD7A3';
// Match UUIDs, hex hashes, IPs, file paths (matching macLooksTechnicalToken)
    [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex UuidPattern();

    [GeneratedRegex(@"^[0-9a-fA-F]{32,}$")]
    private static partial Regex HexHashPattern();

    [GeneratedRegex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(:\d+)?$")]
    private static partial Regex IpPortPattern();

    private static bool LooksTechnical(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        return UuidPattern().IsMatch(token)
            || HexHashPattern().IsMatch(token)
            || IpPortPattern().IsMatch(token)
            || token.StartsWith('/')
            || token.StartsWith("SHA256:");
    }

    /// <summary>
    /// Creates a TextBlock with mixed-font inline runs for CJK + Latin text.
    /// Matches MacMixedFontText behavior.
    /// </summary>
    public static TextBlock CreateMixedTextBlock(
        string text,
        double fontSize = 14,
        string? serifWeight = null,
        string? cjkWeight = null)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        };

        if (string.IsNullOrEmpty(text))
        {
            block.Text = "";
            return block;
        }

        // Split into character-class runs: CJK, Latin, Digits, Punctuation, Technical
        var runs = SegmentText(text);
        foreach (var (segment, kind) in runs)
        {
            var run = new Run { Text = segment };
            switch (kind)
            {
                case TextSegmentKind.CJK:
                    run.FontFamily = new FontFamily(DefaultFont);
                    run.FontSize = fontSize;
                    if (cjkWeight != null)
                        run.FontWeight = ParseWeight(cjkWeight);
                    break;
                case TextSegmentKind.Latin:
                    run.FontFamily = new FontFamily(SerifFont);
                    run.FontSize = fontSize;
                    if (serifWeight != null)
                        run.FontWeight = ParseWeight(serifWeight);
                    break;
                case TextSegmentKind.Digit:
                    run.FontFamily = new FontFamily(SerifFont);
                    run.FontSize = fontSize;
                    break;
                case TextSegmentKind.Technical:
                    run.FontFamily = new FontFamily(MonoFont);
                    run.FontSize = fontSize - 1;
                    break;
                default:
                    run.FontFamily = new FontFamily(DefaultFont);
                    run.FontSize = fontSize;
                    break;
            }
            block.Inlines.Add(run);
        }

        return block;
    }

    private static List<(string Text, TextSegmentKind Kind)> SegmentText(string text)
    {
        var result = new List<(string, TextSegmentKind)>();
        if (string.IsNullOrEmpty(text)) return result;

        // First check if entire token is technical
        if (LooksTechnical(text))
        {
            result.Add((text, TextSegmentKind.Technical));
            return result;
        }

        int i = 0;
        while (i < text.Length)
        {
            var kind = ClassifyChar(text[i]);
            int start = i;
            while (i < text.Length && (ClassifyChar(text[i]) == kind || IsPunctuation(text[i])))
            {
                // Punctuation follows preceding kind
                if (IsPunctuation(text[i]) && i > start)
                    break;
                i++;
            }
            result.Add((text[start..i], kind));
        }

        return result;
    }

    private static TextSegmentKind ClassifyChar(char c)
    {
        if (IsCJK(c)) return TextSegmentKind.CJK;
        if (char.IsLetter(c)) return TextSegmentKind.Latin;
        if (char.IsDigit(c)) return TextSegmentKind.Digit;
        return TextSegmentKind.Latin; // fallback
    }

    private static bool IsPunctuation(char c) =>
        char.IsPunctuation(c) || char.IsSeparator(c) || char.IsWhiteSpace(c);

    private static FontWeight ParseWeight(string weight) => weight.ToLowerInvariant() switch
    {
        "bold" => new FontWeight { Weight = 700 },
        "semibold" => new FontWeight { Weight = 600 },
        "medium" => new FontWeight { Weight = 500 },
        "regular" => new FontWeight { Weight = 400 },
        _ => new FontWeight { Weight = 400 },
    };

    private enum TextSegmentKind { CJK, Latin, Digit, Technical }
}

