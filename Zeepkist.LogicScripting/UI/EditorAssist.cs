using BepInEx.Configuration;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// What the Script editor's help reads from outside the editor, shared by every Script's editor.
/// </summary>
internal sealed class EditorAssist
{
    private readonly ConfigEntry<bool> _completionWhileTyping;

    public EditorAssist(ConfigEntry<bool> completionWhileTyping)
    {
        _completionWhileTyping = completionWhileTyping;
    }

    /// <summary>
    /// Whether typing a name opens the completion list; Ctrl+Space opens it either way.
    /// </summary>
    public bool CompletionWhileTyping => _completionWhileTyping.Value;
}
