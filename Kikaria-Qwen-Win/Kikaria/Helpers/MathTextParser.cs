using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kikaria.Helpers
{
    public abstract record MathTextBlock
    {
        public sealed record Paragraph(List<InlineMathSegment> Segments) : MathTextBlock;
        public sealed record BlockMath(string Source, string Body) : MathTextBlock;
        public sealed record BlankLine : MathTextBlock;
    }

    public abstract record InlineMathSegment
    {
        public sealed record Text(string Value) : InlineMathSegment;
        public sealed record InlineMath(string Source, string Body) : InlineMathSegment;
    }

    public abstract record InlineMathItem
    {
        public sealed record Text(string Value) : InlineMathItem;
        public sealed record InlineMath(string Source, string Body) : InlineMathItem;
        public sealed record InlineMathWithTrailingText(string Source, string Body, string TrailingText) : InlineMathItem;
    }

    public class MathTextParser
    {
        private const int CacheLimit = 180;
        private static readonly object CacheLock = new();
        private static readonly Dictionary<string, CacheEntry> Cache = new();
        private static readonly LinkedList<string> LruOrder = new();

        private class CacheEntry
        {
            public List<MathTextBlock> Blocks { get; set; } = new();
            public LinkedListNode<string> LruNode { get; set; } = null!;
        }

        public static List<MathTextBlock> ParsedContentFor(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<MathTextBlock> { new MathTextBlock.Paragraph(new List<InlineMathSegment> { new InlineMathSegment.Text("") }) };

            lock (CacheLock)
            {
                if (Cache.TryGetValue(text, out var existing))
                {
                    LruOrder.Remove(existing.LruNode);
                    LruOrder.AddFirst(existing.LruNode);
                    return existing.Blocks;
                }
            }

            var tokens = LatexParser.Tokenize(text);

            if (tokens.Count == 1 && tokens[0] is LatexToken.Text singleText && singleText.Value == text)
            {
                var plainBlocks = BuildDisplayBlocks(tokens);
                lock (CacheLock)
                {
                    var node = LruOrder.AddFirst(text);
                    Cache[text] = new CacheEntry { Blocks = plainBlocks, LruNode = node };
                    while (Cache.Count > CacheLimit)
                    {
                        var lastNode = LruOrder.Last;
                        if (lastNode != null) { Cache.Remove(lastNode.Value); LruOrder.RemoveLast(); }
                    }
                }
                return plainBlocks;
            }

            var blocks = BuildDisplayBlocks(tokens);
            var promoted = PromoteStandaloneInlineMath(blocks);
            var normalized = NormalizeBlocks(promoted);

            lock (CacheLock)
            {
                if (Cache.TryGetValue(text, out var existing))
                {
                    LruOrder.Remove(existing.LruNode);
                    LruOrder.AddFirst(existing.LruNode);
                    return existing.Blocks;
                }

                var node = LruOrder.AddFirst(text);
                Cache[text] = new CacheEntry { Blocks = normalized, LruNode = node };

                while (Cache.Count > CacheLimit)
                {
                    var lastNode = LruOrder.Last;
                    if (lastNode != null)
                    {
                        Cache.Remove(lastNode.Value);
                        LruOrder.RemoveLast();
                    }
                }
            }

            return normalized;
        }

        public static string CacheSummaryFor(string text)
        {
            var blocks = ParsedContentFor(text);
            int paragraphCount = 0;
            int blockMathCount = 0;
            int blankCount = 0;

            foreach (var block in blocks)
            {
                switch (block)
                {
                    case MathTextBlock.Paragraph:
                        paragraphCount++;
                        break;
                    case MathTextBlock.BlockMath:
                        blockMathCount++;
                        break;
                    case MathTextBlock.BlankLine:
                        blankCount++;
                        break;
                }
            }

            lock (CacheLock)
            {
                return $"Blocks: {blocks.Count} (P:{paragraphCount} B:{blockMathCount} Blank:{blankCount}) | Cache: {Cache.Count}/{CacheLimit}";
            }
        }

        private static List<MathTextBlock> BuildDisplayBlocks(List<LatexToken> tokens)
        {
            var blocks = new List<MathTextBlock>();
            var currentSegments = new List<InlineMathSegment>();

            foreach (var token in tokens)
            {
                switch (token)
                {
                    case LatexToken.Text textToken:
                        ProcessTextToken(textToken.Value, currentSegments, blocks);
                        break;

                    case LatexToken.InlineMath inlineMath:
                        currentSegments.Add(new InlineMathSegment.InlineMath(inlineMath.Source, inlineMath.Body));
                        break;

                    case LatexToken.BlockMath blockMath:
                        if (currentSegments.Count > 0)
                        {
                            blocks.Add(new MathTextBlock.Paragraph(new List<InlineMathSegment>(currentSegments)));
                            currentSegments.Clear();
                        }
                        blocks.Add(new MathTextBlock.BlockMath(blockMath.Source, blockMath.Body));
                        break;

                    case LatexToken.Fallback fallback:
                        currentSegments.Add(new InlineMathSegment.Text(fallback.Value));
                        break;
                }
            }

            if (currentSegments.Count > 0)
            {
                blocks.Add(new MathTextBlock.Paragraph(new List<InlineMathSegment>(currentSegments)));
            }

            return blocks;
        }

        private static void ProcessTextToken(string text, List<InlineMathSegment> currentSegments, List<MathTextBlock> blocks)
        {
            int i = 0;
            int length = text.Length;

            while (i < length)
            {
                if (text[i] == '\n')
                {
                    if (currentSegments.Count > 0)
                    {
                        blocks.Add(new MathTextBlock.Paragraph(new List<InlineMathSegment>(currentSegments)));
                        currentSegments.Clear();
                    }

                    blocks.Add(new MathTextBlock.BlankLine());
                    i++;

                    while (i < length && text[i] == '\n')
                    {
                        blocks.Add(new MathTextBlock.BlankLine());
                        i++;
                    }
                }
                else
                {
                    int lineEnd = text.IndexOf('\n', i);
                    if (lineEnd < 0) lineEnd = length;

                    string line = text.Substring(i, lineEnd - i);
                    if (!string.IsNullOrEmpty(line))
                    {
                        currentSegments.Add(new InlineMathSegment.Text(line));
                    }
                    i = lineEnd;
                }
            }
        }

        private static List<MathTextBlock> PromoteStandaloneInlineMath(List<MathTextBlock> blocks)
        {
            var result = new List<MathTextBlock>(blocks.Count);
            foreach (var block in blocks)
            {
                if (block is MathTextBlock.Paragraph para && para.Segments.Count > 0)
                {
                    InlineMathSegment.InlineMath? mathSeg = null;
                    bool allWhitespace = true;

                    foreach (var seg in para.Segments)
                    {
                        if (seg is InlineMathSegment.InlineMath im)
                        {
                            if (mathSeg != null)
                            {
                                mathSeg = null;
                                break;
                            }
                            mathSeg = im;
                        }
                        else if (seg is InlineMathSegment.Text t)
                        {
                            if (!string.IsNullOrWhiteSpace(t.Value))
                            {
                                allWhitespace = false;
                                break;
                            }
                        }
                    }

                    if (mathSeg != null && allWhitespace)
                    {
                        result.Add(new MathTextBlock.BlockMath(mathSeg.Source, mathSeg.Body));
                        continue;
                    }
                }
                result.Add(block);
            }
            return result;
        }

        private static List<MathTextBlock> NormalizeBlocks(List<MathTextBlock> blocks)
        {
            if (blocks.Count <= 1)
                return blocks;

            var result = new List<MathTextBlock>();
            bool lastWasBlank = false;

            foreach (var block in blocks)
            {
                if (block is MathTextBlock.BlankLine)
                {
                    if (lastWasBlank)
                        continue;
                    result.Add(block);
                    lastWasBlank = true;
                }
                else if (block is MathTextBlock.BlockMath)
                {
                    if (result.Count > 0 && result[^1] is MathTextBlock.BlankLine)
                    {
                        result.RemoveAt(result.Count - 1);
                    }
                    result.Add(block);
                    lastWasBlank = false;
                }
                else
                {
                    result.Add(block);
                    lastWasBlank = false;
                }
            }

            while (result.Count > 0 && result[^1] is MathTextBlock.BlankLine)
            {
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }

        public static List<InlineMathItem> InlineItemsFromSegments(List<InlineMathSegment> segments)
        {
            var items = new List<InlineMathItem>();

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                if (segment is InlineMathSegment.Text textSeg)
                {
                    items.Add(new InlineMathItem.Text(textSeg.Value));
                }
                else if (segment is InlineMathSegment.InlineMath mathSeg)
                {
                    string? trailingText = TryExtractTrailingPunctuation(mathSeg.Body, segments, i);

                    if (trailingText != null)
                    {
                        items.Add(new InlineMathItem.InlineMathWithTrailingText(
                            mathSeg.Source, mathSeg.Body, trailingText));
                    }
                    else
                    {
                        items.Add(new InlineMathItem.InlineMath(mathSeg.Source, mathSeg.Body));
                    }
                }
            }

            return items;
        }

        private static readonly HashSet<char> TrailingPunctuation = new()
        {
            ',', '.', ';', ':', '?', '!', ')', ']', '}',
            '\uFF0C', '\u3002', '\uFF1B', '\uFF1A', '\uFF1F', '\uFF01',
            '\u3001', '\uFF09', '\u300B', '\u3011', '\u300D', '\u300F',
            '\u3009', '\u3015', '\uFF3D', '\uFF7D'
        };

        private static string? TryExtractTrailingPunctuation(string body, List<InlineMathSegment> segments, int currentIndex)
        {
            if (string.IsNullOrEmpty(body))
                return null;

            if (currentIndex + 1 >= segments.Count)
                return null;

            var nextSegment = segments[currentIndex + 1];
            if (nextSegment is not InlineMathSegment.Text nextText)
                return null;

            string text = nextText.Value;
            if (string.IsNullOrEmpty(text))
                return null;

            int i = 0;
            while (i < text.Length && TrailingPunctuation.Contains(text[i]))
            {
                i++;
            }

            if (i > 0)
            {
                string trailing = text.Substring(0, i);
                string remaining = text.Substring(i);

                segments[currentIndex + 1] = string.IsNullOrEmpty(remaining)
                    ? new InlineMathSegment.Text("")
                    : new InlineMathSegment.Text(remaining);

                return trailing;
            }

            return null;
        }

        public static void ClearCache()
        {
            lock (CacheLock)
            {
                Cache.Clear();
                LruOrder.Clear();
            }
        }
    }
}
