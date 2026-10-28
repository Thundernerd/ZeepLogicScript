using TNRD.Zeepkist.LogicScripting.Compiler;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Removes a Block Group, or turns it into plain blocks, as one undo step each.
/// </summary>
internal static class BlockGroupEdits
{
    private const string DeleteLabel = "Logic Scripting Delete group";
    private const string UntagLabel = "Logic Scripting Untag group";

    /// <summary>
    /// Removes the group's blocks and their Wires to other blocks. Returns how many blocks it removed.
    /// </summary>
    public static int Delete(LEV_LevelEditorCentral central, ScriptId scriptId)
    {
        var group = BlockGroups.Find(central, scriptId);
        if (group.Count > 0)
            BlockBatch.Apply(central, [], group, [], DeleteLabel);

        return group.Count;
    }

    /// <summary>
    /// Leaves the group's blocks in the level as plain Logic Blocks, without their tag or the Script's text. Returns how
    /// many blocks it changed.
    /// </summary>
    public static int Untag(LEV_LevelEditorCentral central, ScriptId scriptId)
    {
        var group = BlockGroups.Find(central, scriptId);
        if (group.Count > 0)
            BlockBatch.Change(central, group, block => LevelScripts.Untag(block.customBlockEditProperties), UntagLabel);

        return group.Count;
    }
}
