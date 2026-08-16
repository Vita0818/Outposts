//
//  Presets.cs
//  Kikaria-Windows
//
//  内置预设加载,移植自 Kikaria-Apple 的 KnowledgePreset.loadBuiltInPresets:
//  从应用目录 Presets\*.md 读取,按文件名排序,id = "builtin-<文件名去扩展>"。
//

namespace Kikaria.Core;

public static class PresetLibrary
{
    public const string PresetsResourceDirectory = "Presets";
    public const string BuiltInCategory = "内置预设";

    /// <summary>默认内置预设目录(应用输出目录下的 Presets)。</summary>
    public static string DefaultPresetsDirectory => Path.Combine(AppContext.BaseDirectory, PresetsResourceDirectory);

    /// <summary>加载全部内置预设;目录缺失或为空时返回占位预设。</summary>
    public static List<KnowledgePreset> LoadBuiltInPresets(string? directory = null)
    {
        var dir = directory ?? DefaultPresetsDirectory;
        var presets = new List<KnowledgePreset>();

        if (Directory.Exists(dir))
        {
            var files = Directory.GetFiles(dir, "*.md")
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                presets.Add(MakeBuiltInPreset(file));
            }
        }

        if (presets.Count == 0)
        {
            return new List<KnowledgePreset> { EmptyBuiltInPreset() };
        }

        return presets;
    }

    /// <summary>无内置文件时的占位预设(与 Apple 版 emptyBuiltInPreset 一致)。</summary>
    public static KnowledgePreset EmptyBuiltInPreset() => new()
    {
        Id = "builtin-empty",
        Name = "内置预设",
        Subtitle = "内置知识点",
        Description = "未找到内置 Markdown 预设。",
        Category = BuiltInCategory,
        MarkdownText = "",
        IsBuiltIn = true
    };

    private static KnowledgePreset MakeBuiltInPreset(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var displayName = Path.GetFileNameWithoutExtension(fileName);

        string markdown;
        try
        {
            markdown = File.ReadAllText(filePath).Trim();
        }
        catch (IOException)
        {
            markdown = "";
        }

        return new KnowledgePreset
        {
            Id = "builtin-" + displayName,
            Name = displayName,
            Subtitle = displayName + "知识点",
            Description = "由内置 Markdown 文件「" + PresetsResourceDirectory + "/" + fileName + "」提供的知识点预设。",
            Category = BuiltInCategory,
            MarkdownText = markdown,
            IsBuiltIn = true
        };
    }
}
