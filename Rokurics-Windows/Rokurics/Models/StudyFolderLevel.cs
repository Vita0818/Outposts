namespace Rokurics.Models;

public enum StudyFolderLevel
{
    Type,
    Subject,
    Chapter,
    Topic,
    Custom
}

public static class StudyFolderLevelExtensions
{
    public static string Title(this StudyFolderLevel level) => level switch
    {
        StudyFolderLevel.Type => "门类",
        StudyFolderLevel.Subject => "课程",
        StudyFolderLevel.Chapter => "章节",
        StudyFolderLevel.Topic => "主题",
        StudyFolderLevel.Custom => "文件夹",
        _ => "文件夹"
    };

    public static StudyFolderLevel? Next(this StudyFolderLevel level) => level switch
    {
        StudyFolderLevel.Type => StudyFolderLevel.Subject,
        StudyFolderLevel.Subject => StudyFolderLevel.Chapter,
        StudyFolderLevel.Chapter => StudyFolderLevel.Topic,
        _ => null
    };
}
