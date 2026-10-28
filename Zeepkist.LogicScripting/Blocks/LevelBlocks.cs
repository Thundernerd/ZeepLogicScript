namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Finds the blocks in the open level by UID, for both Placed Block References and <see cref="BlockBatch"/>.
/// </summary>
internal static class LevelBlocks
{
    /// <summary>
    /// The live block with a UID, or null when the level has none. Reads the editor's own block dictionary rather
    /// than <see cref="StaticConnectorTracker"/>, which can still hold a block after undo removes it.
    /// </summary>
    public static BlockProperties Find(LEV_LevelEditorCentral central, string uid)
    {
        return uid != null && central.undoRedo.allBlocksDictionary.TryGetValue(uid, out var block) && block != null
            ? block
            : null;
    }
}
