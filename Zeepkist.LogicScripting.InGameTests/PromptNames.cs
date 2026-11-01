using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.UI;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// How a scenario writes a list tab's prompts and their buttons.
/// </summary>
internal static class PromptNames
{
    public static readonly IReadOnlyDictionary<string, ListPrompt> Prompts = new Dictionary<string, ListPrompt>
    {
        ["none"] = ListPrompt.None,
        ["new"] = ListPrompt.New,
        ["rename"] = ListPrompt.Rename,
        ["delete"] = ListPrompt.Delete,
    };

    public static readonly IReadOnlyDictionary<string, PromptButton> Buttons = new Dictionary<string, PromptButton>
    {
        ["ok"] = PromptButton.Ok,
        ["cancel"] = PromptButton.Cancel,
        ["delete"] = PromptButton.Delete,
        ["keep-blocks"] = PromptButton.KeepBlocks,
    };

    public static string Of(ListPrompt prompt)
    {
        return Prompts.First(p => p.Value == prompt).Key;
    }

    public static string Of(PromptButton button)
    {
        return Buttons.First(b => b.Value == button).Key;
    }
}
