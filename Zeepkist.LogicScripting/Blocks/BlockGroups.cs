using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Reads Block Group membership from the tag in each block (ADR 0005).
/// </summary>
internal static class BlockGroups
{
    /// <summary>
    /// The Script whose Block Group the block is in, or null when it's in none.
    /// </summary>
    public static string GetScriptId(BlockProperties block)
    {
        var tag = block.customBlockEditProperties?.GetStringDictionaryValue(BlockGroupTag.Key);
        return BlockGroupTag.GetScriptId(tag, block.UID);
    }

    /// <summary>
    /// Every block in the Script's Block Group. Goes through all the level's blocks, so call it on a Compile, not per
    /// frame.
    /// </summary>
    public static List<BlockProperties> Find(LEV_LevelEditorCentral central, string scriptId)
    {
        List<BlockProperties> blocks = [];
        foreach (var block in central.undoRedo.allBlocksDictionary.Values)
        {
            if (block != null && GetScriptId(block) == scriptId)
                blocks.Add(block);
        }

        return blocks;
    }
}
