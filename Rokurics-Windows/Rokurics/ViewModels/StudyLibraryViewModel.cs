using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rokurics.Models;
using Rokurics.Services;

namespace Rokurics.ViewModels;

/// <summary>
/// ViewModel for the study library browser page.
/// </summary>
public partial class StudyLibraryViewModel : ObservableObject
{
    private readonly StudyLibraryStore _store;

    [ObservableProperty] private List<StudyItemMetadata> _items = new();
    [ObservableProperty] private List<StudyBrowserFolder> _folders = new();
    [ObservableProperty] private StudyBrowserPath _currentPath = new();
    [ObservableProperty] private string _levelTitle = "门类";
    [ObservableProperty] private List<(string Title, StudyBrowserPath Path)> _breadcrumbs = new();

    public StudyLibraryViewModel(StudyLibraryStore store)
    {
        _store = store;
        Refresh();
    }

    public void Refresh()
    {
        _store.Refresh();
        var content = StudyLibraryBrowser.Browse(_store.AllStudyItems, _store.AllStudyFolders, CurrentPath);
        Items = content.Items;
        Folders = content.Folders;
        Breadcrumbs = BuildBreadcrumbs(CurrentPath);
        LevelTitle = LevelTitleFor(CurrentPath);
        OnPropertyChanged(nameof(LevelTitle));
    }

    [RelayCommand]
    private void NavigateToFolder(StudyBrowserFolder folder)
    {
        CurrentPath = folder.Path;
        Refresh();
    }

    [RelayCommand]
    private void NavigateToBreadcrumb((string Title, StudyBrowserPath Path) crumb)
    {
        CurrentPath = crumb.Path;
        Refresh();
    }

    [RelayCommand]
    private void GoBack()
    {
        if (!CurrentPath.IsRoot)
        {
            CurrentPath = CurrentPath.Parent;
            Refresh();
        }
    }

    [RelayCommand]
    private void DeleteItem(StudyItemMetadata item)
    {
        if (item.RecordingId is not null)
        {
            var rm = App.Current.Services.GetService(typeof(RecordingManager)) as RecordingManager;
            rm?.DeleteRecording(item.RecordingId);
        }
        Refresh();
    }

    private static List<(string, StudyBrowserPath)> BuildBreadcrumbs(StudyBrowserPath path)
    {
        var result = new List<(string, StudyBrowserPath)> { ("学习库", new StudyBrowserPath()) };
        for (int i = 0; i < path.Components.Count; i++)
        {
            var cp = new StudyBrowserPath(path.Components.Take(i + 1).ToList());
            result.Add((path.Components[i], cp));
        }
        return result;
    }

    private static string LevelTitleFor(StudyBrowserPath path)
    {
        if (path.Depth >= StudyLibraryBrowser.LevelKeys.Length) return "录音";
        return StudyLibraryBrowser.LevelKeys[path.Depth] switch
        {
            "type" => "门类",
            "subject" => "课程",
            "chapter" => "章节",
            "topic" => "主题",
            _ => "文件夹"
        };
    }
}
