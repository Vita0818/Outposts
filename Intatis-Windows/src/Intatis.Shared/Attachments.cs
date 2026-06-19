using System.Text;

namespace Intatis.Windows.Shared;

public abstract class ChatAttachment
{
    public string Name { get; }

    protected ChatAttachment(string name)
    {
        Name = name;
    }
}

public sealed class TextAttachment : ChatAttachment
{
    public string Content { get; }

    public TextAttachment(string name, string content)
        : base(name)
    {
        Content = content;
    }
}

public sealed class ImageAttachment : ChatAttachment
{
    public string Url { get; }
    public string MimeType { get; }

    public ImageAttachment(string name, string mimeType, string url)
        : base(name)
    {
        MimeType = mimeType;
        Url = url;
    }
}

public sealed class AttachmentLoadResult
{
    public ChatAttachment? Attachment { get; }
    public string? Failure { get; }

    private AttachmentLoadResult(ChatAttachment? attachment, string? failure)
    {
        Attachment = attachment;
        Failure = failure;
    }

    public bool IsSuccess => Attachment is not null;

    public static AttachmentLoadResult Success(ChatAttachment attachment) => new(attachment, null);
    public static AttachmentLoadResult Failure(string message) => new(null, message);
}

public static class AttachmentLoader
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "png",
        "jpg",
        "jpeg",
        "gif",
        "webp",
        "bmp",
    };

    public static AttachmentLoadResult Load(string path)
    {
        var normalized = CommandParser.ExpandTilde(path);
        var fullPath = Path.GetFullPath(normalized);

        if (!File.Exists(fullPath))
            return AttachmentLoadResult.Failure($"file not found: {fullPath}");

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(fullPath);
        }
        catch (Exception ex)
        {
            return AttachmentLoadResult.Failure($"cannot read {fullPath}: {ex.Message}");
        }

        var ext = Path.GetExtension(fullPath).TrimStart('.').ToLowerInvariant();

        if (ImageExtensions.Contains(ext))
        {
            var mime = ext == "jpg" ? "image/jpeg" : $"image/{ext}";
            var dataUri = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            return AttachmentLoadResult.Success(new ImageAttachment(Path.GetFileName(fullPath), mime, dataUri));
        }

        var utf8 = new UTF8Encoding(false, true);
        try
        {
            var text = utf8.GetString(bytes);
            return AttachmentLoadResult.Success(new TextAttachment(Path.GetFileName(fullPath), text));
        }
        catch (DecoderFallbackException)
        {
            return AttachmentLoadResult.Failure($"unsupported file type '.{ext}' (only images and UTF-8 text).");
        }
    }
}

