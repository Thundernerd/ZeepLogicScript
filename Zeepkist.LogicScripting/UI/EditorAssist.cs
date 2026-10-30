using BepInEx.Configuration;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// What the Script editor's help reads from outside the editor, shared by every Script's editor.
/// </summary>
internal sealed class EditorAssist
{
    private readonly ConfigEntry<bool> _completionWhileTyping;

    public EditorAssist(ConfigEntry<bool> completionWhileTyping, LibraryList libraries)
    {
        _completionWhileTyping = completionWhileTyping;
        Libraries = libraries;
    }

    /// <summary>
    /// The Libraries on this machine, whose Functions the editor colors.
    /// </summary>
    public LibraryList Libraries { get; }

    /// <summary>
    /// Whether typing a name opens the completion list; Ctrl+Space opens it either way.
    /// </summary>
    public bool CompletionWhileTyping => _completionWhileTyping.Value;

    /// <summary>
    /// The block in the open level with a UID; null outside the editor or when there's none.
    /// </summary>
    public PlacedBlock FindPlacedBlock(string uid)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        return central == null ? null : PlacedBlocks.FindWithoutWires(central, uid);
    }
}
