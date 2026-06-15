namespace Flotis.Models;

public sealed class PromptCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? ShortcutIndex { get; set; }
}
