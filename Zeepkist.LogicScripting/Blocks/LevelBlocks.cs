namespace TNRD.Zeepkist.LogicScripting.Blocks;

internal static class LevelBlocks
{
    /// <summary>
    /// Reads the editor's own block dictionary rather than <see cref="StaticConnectorTracker"/>, which can still hold a
    /// block after undo removes it.
    /// </summary>
    public static BlockProperties Find(LEV_LevelEditorCentral central, string uid)
    {
        return uid != null && central.undoRedo.allBlocksDictionary.TryGetValue(uid, out var block) && block != null
            ? block
            : null;
    }
}
