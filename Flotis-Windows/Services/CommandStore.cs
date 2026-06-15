using Flotis.Models;

namespace Flotis.Services;

public static class CommandStore
{
    public static IReadOnlyList<PromptCommand> DefaultCommands { get; } = new[]
    {
        new PromptCommand { Id = Guid.NewGuid(), Title = "说中文", Content = "请使用中文回答。", ShortcutIndex = 1 },
        new PromptCommand { Id = Guid.NewGuid(), Title = "不改变量名", Content = "不要修改现有变量名、函数名、文件名，除非这是修复该问题所必需的。若必须修改，请先说明原因。", ShortcutIndex = 2 },
        new PromptCommand { Id = Guid.NewGuid(), Title = "遵循指令", Content = "请严格遵循我上一条消息中的所有约束，不要自行扩大任务范围。", ShortcutIndex = 3 },
        new PromptCommand { Id = Guid.NewGuid(), Title = "还是报错", Content = "仍然报错。请不要重复上一轮方案，先定位根因，再给出最小修改。", ShortcutIndex = 4 },
        new PromptCommand { Id = Guid.NewGuid(), Title = "最小修改", Content = "只做解决当前问题所需的最小修改，不要顺手重构，不要引入新的抽象。", ShortcutIndex = 5 },
        new PromptCommand { Id = Guid.NewGuid(), Title = "不要重构", Content = "不要进行重构。保持现有结构，只修复当前明确指出的问题。", ShortcutIndex = 6 },
        new PromptCommand { Id = Guid.NewGuid(), Title = "先定位根因", Content = "先定位根因，再给出修改方案。不要直接猜测式修改。", ShortcutIndex = 7 },
        new PromptCommand { Id = Guid.NewGuid(), Title = "只输出命令", Content = "只输出需要执行的命令，不要解释。", ShortcutIndex = 8 }
    };
}
