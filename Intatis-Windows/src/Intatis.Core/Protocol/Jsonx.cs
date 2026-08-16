using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Intatis.Core.Protocol;

/// <summary>JSONC stripping and canonical JSON helpers shared by config and protocol code.</summary>
public static class Jsonx
{
    /// <summary>
    /// Strips // and /* */ comments plus trailing commas, string-aware, mirroring the
    /// Apple importer's JSONC preprocessing.
    /// </summary>
    public static string StripJsonc(string source)
    {
        var sb = new StringBuilder(source.Length);
        var i = 0;
        while (i < source.Length)
        {
            var c = source[i];
            if (c == '"')
            {
                var start = i;
                i++;
                while (i < source.Length)
                {
                    var s = source[i];
                    if (s == '\\') { i += 2; continue; }
                    if (s == '"') { i++; break; }
                    i++;
                }
                sb.Append(source, start, i - start);
                continue;
            }
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
            {
                while (i < source.Length && source[i] != '\n') i++;
                continue;
            }
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/')) i++;
                i = Math.Min(i + 2, source.Length);
                continue;
            }
            sb.Append(c);
            i++;
        }

        // Remove trailing commas before ] or } (the input has already been string-stripped
        // above only inside copies; do a second string-aware pass on the comment-free text).
        var text = sb.ToString();
        var out2 = new StringBuilder(text.Length);
        i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '"')
            {
                var start = i;
                i++;
                while (i < text.Length)
                {
                    var s = text[i];
                    if (s == '\\') { i += 2; continue; }
                    if (s == '"') { i++; break; }
                    i++;
                }
                out2.Append(text, start, i - start);
                continue;
            }
            if (c == ',')
            {
                var j = i + 1;
                while (j < text.Length && char.IsWhiteSpace(text[j])) j++;
                if (j < text.Length && (text[j] == ']' || text[j] == '}'))
                {
                    i++;
                    continue;
                }
            }
            out2.Append(c);
            i++;
        }
        return out2.ToString();
    }

    public static JsonObject ParseObject(string json)
    {
        var node = JsonNode.Parse(json, documentOptions: JsonDocumentOptionsAllowCommentsAndTrailingCommas)
            ?? throw new JsonException("empty JSON document");
        return node.AsObject();
    }

    public static JsonObject? ParseObjectOrNull(string json)
    {
        try { return ParseObject(json); }
        catch (JsonException) { return null; }
    }

    public static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>Serialize a JsonNode with alphabetically sorted keys for deterministic bytes.</summary>
    public static string SerializeSorted(JsonNode? node)
    {
        if (node is null) return "null";
        var clone = SortDeep(node);
        return clone.ToJsonString();
    }

    private static JsonNode SortDeep(JsonNode node) => node switch
    {
        JsonObject obj => new JsonObject(obj.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new KeyValuePair<string, JsonNode?>(p.Key, p.Value is null ? null : SortDeep(p.Value)))),
        JsonArray arr => new JsonArray(arr.Select(item => item is null ? null : SortDeep(item))),
        _ => node.DeepClone(),
    };

    public static readonly JsonDocumentOptions JsonDocumentOptionsAllowCommentsAndTrailingCommas = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}
