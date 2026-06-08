using System;
using System.Collections.Generic;
using System.Text;

namespace Kikaria.Helpers
{
    public abstract record LatexToken
    {
        public sealed record Text(string Value) : LatexToken;
        public sealed record InlineMath(string Source, string Body) : LatexToken;
        public sealed record BlockMath(string Source, string Body) : LatexToken;
        public sealed record Fallback(string Value) : LatexToken;
    }

    public static class LatexParser
    {
        public static List<LatexToken> Tokenize(string text)
        {
            var tokens = new List<LatexToken>();
            if (string.IsNullOrEmpty(text))
                return tokens;

            int length = text.Length;
            int pos = 0;

            while (pos < length)
            {
                if (pos + 2 < length && text[pos] == '`' && text[pos + 1] == '`' && text[pos + 2] == '`')
                {
                    int codeEnd = ScanCodeSpan(text, pos, length);
                    string codeText = text.Substring(pos, codeEnd - pos);
                    AppendText(tokens, codeText);
                    pos = codeEnd;
                }
                else if (text[pos] == '`')
                {
                    int codeEnd = ScanCodeSpan(text, pos, length);
                    string codeText = text.Substring(pos, codeEnd - pos);
                    AppendText(tokens, codeText);
                    pos = codeEnd;
                }
                else if (text[pos] == '\\' && pos + 1 < length && text[pos + 1] == '$')
                {
                    AppendText(tokens, "$");
                    pos += 2;
                }
                else if (text[pos] == '$')
                {
                    if (pos + 1 < length && text[pos + 1] == '$')
                    {
                        int blockEnd = ScanBlockMath(text, pos + 2, length);
                        if (blockEnd > pos + 2)
                        {
                            string body = text.Substring(pos + 2, blockEnd - (pos + 2));
                            string source = text.Substring(pos, blockEnd + 2 - pos);
                            tokens.Add(new LatexToken.BlockMath(source, body));
                            pos = blockEnd + 2;
                        }
                        else
                        {
                            AppendText(tokens, "$$");
                            pos += 2;
                        }
                    }
                    else
                    {
                        int inlineEnd = ScanInlineMath(text, pos + 1, length);
                        if (inlineEnd > pos + 1)
                        {
                            string body = text.Substring(pos + 1, inlineEnd - (pos + 1));
                            string source = text.Substring(pos, inlineEnd + 1 - pos);
                            tokens.Add(new LatexToken.InlineMath(source, body));
                            pos = inlineEnd + 1;
                        }
                        else
                        {
                            AppendText(tokens, "$");
                            pos += 1;
                        }
                    }
                }
                else
                {
                    int nextSpecial = FindNextSpecial(text, pos, length);
                    string plainText = text.Substring(pos, nextSpecial - pos);
                    AppendText(tokens, plainText);
                    pos = nextSpecial;
                }
            }

            return tokens;
        }

        private static int ScanCodeSpan(string text, int start, int length)
        {
            int backtickCount = 0;
            int i = start;
            while (i < length && text[i] == '`')
            {
                backtickCount++;
                i++;
            }

            int searchFrom = i;
            while (searchFrom < length)
            {
                int closingCount = 0;
                int j = searchFrom;
                while (j < length && text[j] == '`')
                {
                    closingCount++;
                    j++;
                }

                if (closingCount == backtickCount)
                    return j;

                if (closingCount > 0)
                    searchFrom = j;
                else
                    searchFrom++;
            }

            return length;
        }

        private static bool IsEscaped(string text, int index)
        {
            int count = 0;
            int i = index - 1;
            while (i >= 0 && text[i] == '\\')
            {
                count++;
                i--;
            }
            return count % 2 == 1;
        }

        private static int ScanBlockMath(string text, int bodyStart, int length)
        {
            int i = bodyStart;
            while (i < length - 1)
            {
                if (text[i] == '$' && text[i + 1] == '$' && !IsEscaped(text, i))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        private static int ScanInlineMath(string text, int bodyStart, int length)
        {
            int i = bodyStart;
            while (i < length)
            {
                if (text[i] == '$' && !IsEscaped(text, i))
                {
                    if (i + 1 < length && text[i + 1] == '$')
                    {
                        i += 2;
                        continue;
                    }
                    return i;
                }
                if (text[i] == '\n' || text[i] == '\r')
                {
                    return -1;
                }
                i++;
            }
            return -1;
        }

        private static int FindNextSpecial(string text, int start, int length)
        {
            int i = start;
            while (i < length)
            {
                char c = text[i];
                if (c == '$' || c == '`')
                    return i;
                if (c == '\\' && i + 1 < length && text[i + 1] == '$')
                    return i;
                i++;
            }
            return length;
        }

        private static void AppendText(List<LatexToken> tokens, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (tokens.Count > 0 && tokens[^1] is LatexToken.Text existingText)
            {
                tokens[^1] = new LatexToken.Text(existingText.Value + text);
            }
            else
            {
                tokens.Add(new LatexToken.Text(text));
            }
        }
    }
}
