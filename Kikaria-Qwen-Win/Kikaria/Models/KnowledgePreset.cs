using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kikaria.Models;

public partial class KnowledgePreset : ObservableObject
{
    public const int BuiltInSeedVersion = 4;

    [ObservableProperty]
    [JsonInclude]
    private string id;

    [ObservableProperty]
    [JsonInclude]
    private string name;

    [ObservableProperty]
    [JsonInclude]
    private string subtitle;

    [ObservableProperty]
    [JsonInclude]
    private string description;

    [ObservableProperty]
    [JsonInclude]
    private string category;

    [ObservableProperty]
    [JsonInclude]
    private string markdownText;

    [ObservableProperty]
    [JsonInclude]
    private bool isBuiltIn;

    [JsonIgnore]
    public int KnowledgePointCount
    {
        get
        {
            if (string.IsNullOrWhiteSpace(MarkdownText))
                return 0;
            return KnowledgePoint.ParseMarkdown(MarkdownText).Count;
        }
    }

    [JsonConstructor]
    public KnowledgePreset(
        string id,
        string name,
        string subtitle,
        string description,
        string category,
        string markdownText,
        bool isBuiltIn)
    {
        Id = id;
        Name = name;
        Subtitle = subtitle;
        Description = description;
        Category = category;
        MarkdownText = markdownText;
        IsBuiltIn = isBuiltIn;
    }

    public KnowledgePreset()
    {
        Id = Guid.NewGuid().ToString();
        Name = string.Empty;
        Subtitle = string.Empty;
        Description = string.Empty;
        Category = string.Empty;
        MarkdownText = string.Empty;
        IsBuiltIn = false;
    }

    private static List<KnowledgePreset>? _all;

    [JsonIgnore]
    public static List<KnowledgePreset> All
    {
        get
        {
            _all ??= LoadBuiltInPresets();
            return _all;
        }
    }

    private static readonly KnowledgePreset EmptyBuiltInPreset = new()
    {
        Id = "builtin-empty",
        Name = "内置预设",
        Subtitle = "内置预设",
        Description = "空的内置预设。",
        Category = "内置预设",
        MarkdownText = string.Empty,
        IsBuiltIn = true
    };

    public static string DefaultPresetID => All.FirstOrDefault()?.Id ?? EmptyBuiltInPreset.Id;

    [JsonIgnore]
    public static KnowledgePreset DefaultPreset
    {
        get
        {
            var preset = All.FirstOrDefault(p => p.Id == DefaultPresetID);
            return preset ?? EmptyBuiltInPreset;
        }
    }

    [JsonIgnore]
    public static List<string> CurrentBuiltInPresetIDs
    {
        get
        {
            return All.Where(p => p.IsBuiltIn).Select(p => p.Id).ToList();
        }
    }

    public static List<KnowledgePreset> LoadBuiltInPresets()
    {
        var presets = new List<KnowledgePreset>();

        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var presetsDir = Path.Combine(baseDir, "Presets");

            if (!Directory.Exists(presetsDir))
            {
                presetsDir = Path.Combine(baseDir, "..", "Presets");
            }

            if (Directory.Exists(presetsDir))
            {
                var mdFiles = Directory.GetFiles(presetsDir, "*.md");

                foreach (var filePath in mdFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        var displayName = Path.GetFileNameWithoutExtension(filePath);
                        var markdownText = File.ReadAllText(filePath);

                        var preset = new KnowledgePreset
                        {
                            Id = $"builtin-{displayName}",
                            Name = displayName,
                            Subtitle = $"{displayName}知识点",
                            Description = $"由内置 Markdown 文件「Presets/{fileName}」提供的知识点预设。",
                            Category = "内置预设",
                            MarkdownText = markdownText,
                            IsBuiltIn = true
                        };

                        presets.Add(preset);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        if (presets.Count == 0)
        {
            presets.Add(EmptyBuiltInPreset);
        }

        return presets;
    }
}
