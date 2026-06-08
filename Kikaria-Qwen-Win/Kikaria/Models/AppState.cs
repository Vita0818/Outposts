using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kikaria.Models;

public partial class KikariaAppState : ObservableObject
{
    public const string StorageKey = "kikaria.appStateJSON";
    public const int CurrentSchemaVersion = 4;

    [ObservableProperty]
    [JsonInclude]
    private int schemaVersion;

    [ObservableProperty]
    [JsonInclude]
    private List<KnowledgePreset> presets;

    [ObservableProperty]
    [JsonInclude]
    private Dictionary<string, PresetStudyState> presetStates;

    [ObservableProperty]
    [JsonInclude]
    private string currentPresetID;

    [ObservableProperty]
    [JsonInclude]
    private UserProfile userProfile;

    [ObservableProperty]
    [JsonInclude]
    private bool hasCompletedProfileSetup;

    [ObservableProperty]
    [JsonInclude]
    private bool hasCompletedOnboarding;

    [JsonConstructor]
    public KikariaAppState(
        int schemaVersion,
        List<KnowledgePreset> presets,
        Dictionary<string, PresetStudyState> presetStates,
        string currentPresetID,
        UserProfile userProfile,
        bool hasCompletedProfileSetup,
        bool hasCompletedOnboarding)
    {
        SchemaVersion = schemaVersion;
        Presets = presets;
        PresetStates = presetStates;
        CurrentPresetID = currentPresetID;
        UserProfile = userProfile;
        HasCompletedProfileSetup = hasCompletedProfileSetup;
        HasCompletedOnboarding = hasCompletedOnboarding;
    }

    public KikariaAppState()
    {
        SchemaVersion = CurrentSchemaVersion;
        Presets = new List<KnowledgePreset>();
        PresetStates = new Dictionary<string, PresetStudyState>();
        CurrentPresetID = KnowledgePreset.DefaultPresetID;
        UserProfile = new UserProfile();
        HasCompletedProfileSetup = false;
        HasCompletedOnboarding = false;
    }

    [JsonIgnore]
    public KnowledgePreset? CurrentPreset
    {
        get => Presets.FirstOrDefault(p => p.Id == CurrentPresetID);
    }

    [JsonIgnore]
    public PresetStudyState? CurrentPresetState
    {
        get
        {
            if (PresetStates.TryGetValue(CurrentPresetID, out var state))
                return state;
            return null;
        }
    }

    public PresetStudyState GetOrCreateState(string presetId)
    {
        if (PresetStates.TryGetValue(presetId, out var state))
            return state;

        var preset = Presets.FirstOrDefault(p => p.Id == presetId);
        var points = preset != null
            ? KnowledgePoint.ParseMarkdown(preset.MarkdownText)
            : new List<KnowledgePoint>();

        var newState = new PresetStudyState(presetId, points, preset?.MarkdownText ?? string.Empty);
        PresetStates[presetId] = newState;
        return newState;
    }

    public string Serialize()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(this, options);
    }

    public static KikariaAppState? Deserialize(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<KikariaAppState>(json, options);
        }
        catch
        {
            return null;
        }
    }

    public static KikariaAppState CreateDefault()
    {
        var state = new KikariaAppState();
        var builtInPresets = KnowledgePreset.LoadBuiltInPresets();
        state.Presets.AddRange(builtInPresets);

        foreach (var preset in builtInPresets)
        {
            var points = KnowledgePoint.ParseMarkdown(preset.MarkdownText);
            state.PresetStates[preset.Id] = new PresetStudyState(preset.Id, points, preset.MarkdownText);
        }

        return state;
    }
}

public partial class UserProfile : ObservableObject
{
    [ObservableProperty]
    [JsonInclude]
    private string displayName;

    [ObservableProperty]
    [JsonInclude]
    private string userHandle;

    [ObservableProperty]
    [JsonInclude]
    private string avatarSystemName;

    [ObservableProperty]
    [JsonInclude]
    private byte[]? avatarImageData;

    [JsonConstructor]
    public UserProfile(string displayName, string userHandle, string avatarSystemName, byte[]? avatarImageData)
    {
        DisplayName = displayName;
        UserHandle = userHandle;
        AvatarSystemName = avatarSystemName;
        AvatarImageData = avatarImageData;
    }

    public UserProfile()
    {
        DisplayName = "Vita";
        UserHandle = "vita_0818";
        AvatarSystemName = "person.crop.circle.fill";
        AvatarImageData = null;
    }
}

public class PresetLibrarySnapshot
{
    [JsonInclude]
    public List<KnowledgePreset> Presets { get; set; }

    [JsonInclude]
    public Dictionary<string, PresetStudyState> States { get; set; }

    [JsonInclude]
    public string CurrentPresetID { get; set; }

    [JsonConstructor]
    public PresetLibrarySnapshot(
        List<KnowledgePreset> presets,
        Dictionary<string, PresetStudyState> states,
        string currentPresetID)
    {
        Presets = presets;
        States = states;
        CurrentPresetID = currentPresetID;
    }

    public PresetLibrarySnapshot()
    {
        Presets = new List<KnowledgePreset>();
        States = new Dictionary<string, PresetStudyState>();
        CurrentPresetID = string.Empty;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PresetCreationOutcome
{
    Success,
    InvalidInput,
    DuplicateName,
    ParseError,
    SaveFailed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PresetDeleteOutcome
{
    Success,
    NotFound,
    IsDefault,
    DeleteFailed
}
