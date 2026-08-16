//
//  LatexParser.cs
//  Kikaria-Windows
//
//  $..$ / $$..$$ 公式词法,逐行移植自 Kikaria-Apple 的 KikariaLatexParser.swift + LatexToken.swift:
//  - 代码围栏(``` 与 `)内不解析;
//  - \$ 转义为字面 $;
//  - 闭合 $ 前有奇数个反斜杠视为转义;
//  - $$ 块级找下一个未转义 $$,找不到则整段按文本;
//  - $ 行内不能跨行。
//

namespace Kikaria.Core;

public enum LatexTokenKind
{
    Text,
    InlineMath,
    BlockMath,
    Fallback
}

public sealed class LatexToken
{
    public LatexTokenKind Kind { get; }
    public string Source { get; }
    public string Body { get; }

    private LatexToken(LatexTokenKind kind, string source, string body)
    {
        Kind = kind;
        Source = source;
        Body = body;
    }

    public static LatexToken Text(string value) => new(LatexTokenKind.Text, value, value);
    public static LatexToken InlineMath(string source, string body) => new(LatexTokenKind.InlineMath, source, body);
    public static LatexToken BlockMath(string source, string body) => new(LatexTokenKind.BlockMath, source, body);
}

public static class LatexParser
{
    public static List<LatexToken> Tokenize(string text) => new LatexTextScanner(text).Scan();

    private sealed class LatexTextScanner
    {
        private readonly List<char> _characters;
        private int _index;
        private readonly StringBuilder _textBuffer = new();
        private readonly List<LatexToken> _tokens = new();

        public LatexTextScanner(string text)
        {
            _characters = new List<char>(text);
        }

        public List<LatexToken> Scan()
        {
            while (_index < _characters.Count)
            {
                if (StartsWith("```", _index))
                {
                    AppendCodeSpan("```");
                }
                else if (_characters[_index] == '`')
                {
                    AppendCodeSpan("`");
                }
                else if (IsEscapedDollar(_index))
                {
                    _textBuffer.Append('$');
                    _index += 2;
                }
                else if (_characters[_index] == '$')
                {
                    ScanMathToken();
                }
                else
                {
                    _textBuffer.Append(_characters[_index]);
                    _index++;
                }
            }

            FlushText();
            return _tokens;
        }

        private void ScanMathToken()
        {
            if (StartsWith("$$", _index))
            {
                ScanBlockMath();
            }
            else
            {
                ScanInlineMath();
            }
        }

        private void ScanBlockMath()
        {
            var start = _index;
            var closeIndex = ClosingDoubleDollarIndex(start + 2);
            if (closeIndex is null)
            {
                AppendRangeToBuffer(start, _characters.Count);
                _index = _characters.Count;
                return;
            }

            var body = GetString(start + 2, closeIndex.Value);
            var source = GetString(start, closeIndex.Value + 2);

            FlushText();
            _tokens.Add(LatexToken.BlockMath(source, body));
            _index = closeIndex.Value + 2;
        }

        private void ScanInlineMath()
        {
            var start = _index;
            var closeIndex = ClosingSingleDollarIndex(start + 1);
            if (closeIndex is null)
            {
                AppendRangeToBuffer(start, _characters.Count);
                _index = _characters.Count;
                return;
            }

            var body = GetString(start + 1, closeIndex.Value);
            var source = GetString(start, closeIndex.Value);

            FlushText();
            _tokens.Add(LatexToken.InlineMath(source, body));
            _index = closeIndex.Value + 1;
        }

        private void AppendCodeSpan(string fence)
        {
            var start = _index;
            _index += fence.Length;

            while (_index < _characters.Count)
            {
                if (StartsWith(fence, _index))
                {
                    _index += fence.Length;
                    _textBuffer.Append(GetString(start, _index));
                    return;
                }

                _index++;
            }

            _textBuffer.Append(GetString(start, _characters.Count));
        }

        private int? ClosingDoubleDollarIndex(int startIndex)
        {
            var searchIndex = startIndex;
            while (searchIndex < _characters.Count - 1)
            {
                if (StartsWith("$$", searchIndex) && !IsEscaped(searchIndex))
                {
                    return searchIndex;
                }

                searchIndex++;
            }

            return null;
        }

        private int? ClosingSingleDollarIndex(int startIndex)
        {
            var searchIndex = startIndex;
            while (searchIndex < _characters.Count)
            {
                if (_characters[searchIndex] == '\n')
                {
                    return null;
                }

                if (_characters[searchIndex] == '$' &&
                    !IsEscaped(searchIndex) &&
                    !StartsWith("$$", searchIndex))
                {
                    return searchIndex;
                }

                searchIndex++;
            }

            return null;
        }

        private void FlushText()
        {
            if (_textBuffer.Length == 0)
            {
                return;
            }

            _tokens.Add(LatexToken.Text(_textBuffer.ToString()));
            _textBuffer.Clear();
        }

        private bool IsEscapedDollar(int characterIndex)
        {
            return characterIndex + 1 < _characters.Count &&
                _characters[characterIndex] == '\\' &&
                _characters[characterIndex + 1] == '$';
        }

        private bool IsEscaped(int characterIndex)
        {
            var slashCount = 0;
            var searchIndex = characterIndex - 1;
            while (searchIndex >= 0 && _characters[searchIndex] == '\\')
            {
                slashCount++;
                searchIndex--;
            }

            return slashCount % 2 == 1;
        }

        private bool StartsWith(string marker, int startIndex)
        {
            if (startIndex < 0 || startIndex + marker.Length > _characters.Count)
            {
                return false;
            }

            for (var offset = 0; offset < marker.Length; offset++)
            {
                if (_characters[startIndex + offset] != marker[offset])
                {
                    return false;
                }
            }

            return true;
        }

        private string GetString(int start, int end)
        {
            if (end <= start)
            {
                return string.Empty;
            }

            return new string(_characters.GetRange(start, end - start).ToArray());
        }

        private void AppendRangeToBuffer(int start, int end)
        {
            _textBuffer.Append(GetString(start, end));
        }
    }
}
