//
//  MathFallback.cs
//  Kikaria-Windows
//
//  readableMathFallback 的完整移植,来自 Kikaria-Apple 的 KikariaMathFormulaView.swift:
//  - \frac{a}{b} → "(a) / (b)"(分量含空格/换行/斜杠时加括号,递归处理);
//  - \sqrt{x} → "√(x)";
//  - 剥 \operatorname / \mathrm / \mathbf / \text(\bar 追加组合上横线 U+0304);
//  - \begin{cases} 等环境 → 空、\\ → 换行、& → 两空格、\quad → 空格;
//  - \left/\right/\Bigg/\bigg/\Big/\big → 空;
//  - 希腊字母与运算符全表逐条照抄源文件。
//  替换顺序必须保持与源一致(长命令在前,如 \iiint → \iint → \int)。
//

using System.Text;

namespace Kikaria.Core;

public static class MathFallback
{
    private static readonly (string Source, string Replacement)[] Replacements =
    {
        ("\\begin{cases}", ""),
        ("\\end{cases}", ""),
        ("\\begin{aligned}", ""),
        ("\\end{aligned}", ""),
        ("\\begin{matrix}", ""),
        ("\\end{matrix}", ""),
        ("\\\\", "\n"),
        ("&", "  "),
        ("\\qquad", "  "),
        ("\\quad", " "),
        ("\\,", " "),
        ("\\;", " "),
        ("\\:", " "),
        ("\\!", ""),
        ("\\left", ""),
        ("\\right", ""),
        ("\\Bigg", ""),
        ("\\bigg", ""),
        ("\\Big", ""),
        ("\\big", ""),
        ("\\iiint", "∫∫∫"),
        ("\\iint", "∫∫"),
        ("\\int", "∫"),
        ("\\partial", "∂"),
        ("\\nabla", "∇"),
        ("\\Delta", "Δ"),
        ("\\delta", "δ"),
        ("\\theta", "θ"),
        ("\\rho", "ρ"),
        ("\\xi", "ξ"),
        ("\\alpha", "α"),
        ("\\beta", "β"),
        ("\\gamma", "γ"),
        ("\\lambda", "λ"),
        ("\\mu", "μ"),
        ("\\pi", "π"),
        ("\\neq", "≠"),
        ("\\ne", "≠"),
        ("\\leq", "≤"),
        ("\\le", "≤"),
        ("\\geq", "≥"),
        ("\\ge", "≥"),
        ("\\to", "→"),
        ("\\infty", "∞"),
        ("\\cdots", "⋯"),
        ("\\ldots", "…"),
        ("\\times", "×"),
        ("\\pm", "±"),
        ("\\mp", "∓"),
        ("\\in", "∈")
    };

    /// <summary>块级公式的可读文本(等价于 Apple 版 readableMathFallback)。</summary>
    public static string Readable(string source)
    {
        var readable = StripBlockMathDelimiters(source);
        readable = ReplaceFractions(readable);
        readable = ReplaceBracedCommand(readable, "\\sqrt", value => "√(" + Readable(value) + ")");
        readable = ReplaceBracedCommand(readable, "\\operatorname", value => value);
        readable = ReplaceBracedCommand(readable, "\\mathrm", value => value);
        readable = ReplaceBracedCommand(readable, "\\mathbf", value => value);
        readable = ReplaceBracedCommand(readable, "\\text", value => value);
        readable = ReplaceBracedCommand(readable, "\\bar", value => Readable(value) + "\u0304");

        foreach (var (source1, replacement) in Replacements)
        {
            readable = readable.Replace(source1, replacement);
        }

        readable = readable.Replace("\\", "");
        return NormalizeFallbackWhitespace(readable);
    }

    /// <summary>行内公式的可读文本:去掉 $ 定界符后同样走可读化转换。</summary>
    public static string InlineReadable(string body) => Readable(body);

    private static string StripBlockMathDelimiters(string source)
    {
        var trimmed = source.Trim();
        if (trimmed.StartsWith("$$", StringComparison.Ordinal) &&
            trimmed.EndsWith("$$", StringComparison.Ordinal) &&
            trimmed.Length >= 4)
        {
            return trimmed.Substring(2, trimmed.Length - 4).Trim();
        }

        return trimmed;
    }

    private static string ReplaceFractions(string text)
    {
        return ReplaceBracedPairCommand(text, new[] { "\\dfrac", "\\tfrac", "\\frac" }, (numerator, denominator) =>
        {
            var readableNumerator = Readable(numerator);
            var readableDenominator = Readable(denominator);
            return WrapComponent(readableNumerator) + " / " + WrapComponent(readableDenominator);
        });
    }

    private static string ReplaceBracedPairCommand(
        string text,
        string[] commands,
        Func<string, string, string> transform)
    {
        var result = new StringBuilder();
        var index = 0;

        while (index < text.Length)
        {
            var matched = false;
            foreach (var command in commands)
            {
                if (!HasPrefixAt(text, command, index))
                {
                    continue;
                }

                var cursor = index + command.Length;
                SkipWhitespace(text, ref cursor);

                if (BracedGroup(text, ref cursor, out var firstValue))
                {
                    SkipWhitespace(text, ref cursor);
                    if (BracedGroup(text, ref cursor, out var secondValue))
                    {
                        result.Append(transform(firstValue, secondValue));
                        index = cursor;
                        matched = true;
                        break;
                    }
                }
            }

            if (matched)
            {
                continue;
            }

            result.Append(text[index]);
            index++;
        }

        return result.ToString();
    }

    private static string ReplaceBracedCommand(string text, string command, Func<string, string> transform)
    {
        var result = new StringBuilder();
        var index = 0;

        while (index < text.Length)
        {
            if (HasPrefixAt(text, command, index))
            {
                var cursor = index + command.Length;
                SkipWhitespace(text, ref cursor);

                if (BracedGroup(text, ref cursor, out var value))
                {
                    result.Append(transform(value));
                    index = cursor;
                    continue;
                }
            }

            result.Append(text[index]);
            index++;
        }

        return result.ToString();
    }

    private static bool BracedGroup(string text, ref int cursor, out string value)
    {
        value = string.Empty;
        if (cursor >= text.Length || text[cursor] != '{')
        {
            return false;
        }

        cursor++;
        var start = cursor;
        var depth = 1;

        while (cursor < text.Length)
        {
            if (text[cursor] == '{')
            {
                depth++;
            }
            else if (text[cursor] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    value = text.Substring(start, cursor - start);
                    cursor++;
                    return true;
                }
            }

            cursor++;
        }

        return false;
    }

    private static void SkipWhitespace(string text, ref int cursor)
    {
        while (cursor < text.Length && char.IsWhiteSpace(text[cursor]))
        {
            cursor++;
        }
    }

    private static string WrapComponent(string value)
    {
        if (value.Contains(' ') || value.Contains('\n') || value.Contains('/'))
        {
            return "(" + value + ")";
        }

        return value;
    }

    private static string NormalizeFallbackWhitespace(string source)
    {
        var normalizedLines = new List<string>();
        foreach (var rawLine in source.Split('\n'))
        {
            var line = rawLine.Trim();
            while (line.Contains("  ", StringComparison.Ordinal))
            {
                line = line.Replace("  ", " ");
            }

            if (line.Length == 0)
            {
                if (normalizedLines.Count > 0 && normalizedLines[^1].Length != 0)
                {
                    normalizedLines.Add(line);
                }
            }
            else
            {
                normalizedLines.Add(line);
            }
        }

        return string.Join("\n", normalizedLines);
    }

    private static bool HasPrefixAt(string text, string prefix, int index)
    {
        if (index < 0 || index + prefix.Length > text.Length)
        {
            return false;
        }

        for (var offset = 0; offset < prefix.Length; offset++)
        {
            if (text[index + offset] != prefix[offset])
            {
                return false;
            }
        }

        return true;
    }
}
