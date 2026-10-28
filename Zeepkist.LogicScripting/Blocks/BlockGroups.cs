using System.Collections.Generic;
using System.Linq;
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

    public static bool IsInAnyGroup(BlockProperties block)
    {
        return GetScriptId(block) != null;
    }

    /// <summary>
    /// Whether the live block with a UID is in the Script's Block Group.
    /// </summary>
    public static bool IsInGroup(LEV_LevelEditorCentral central, string uid, string scriptId)
    {
        var block = LevelBlocks.Find(central, uid);
        return block != null && GetScriptId(block) == scriptId;
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

    /// <summary>
    /// How many of the group's Wires to blocks outside it a Replace removes without <paramref name="placedWires"/>
    /// making them again, matched by the outside block's Connector. A hand-made Wire into the group is one of these.
    /// </summary>
    public static int CountWiresNotRemade(List<BlockProperties> group, IReadOnlyList<BlockWire> placedWires)
    {
        HashSet<string> groupUids = [.. group.Select(b => b.UID)];
        Dictionary<(string Uid, int Connector), int> remade = [];
        foreach (var wire in placedWires)
        {
            foreach (var end in new[] { (wire.FromUid, wire.FromConnector), (wire.ToUid, wire.ToConnector) })
            {
                remade[end] = remade.TryGetValue(end, out var count) ? count + 1 : 1;
            }
        }

        var lost = 0;
        foreach (var block in group)
        {
            foreach (var hub in block.allConnectorHubsOnThisBlock.Values)
            {
                foreach (var wire in hub?.GetThisHubsConnectionStructs() ?? [])
                {
                    if (groupUids.Contains(wire.targetUID))
                        continue;

                    var end = (wire.targetUID, wire.connectorID);
                    if (remade.TryGetValue(end, out var count) && count > 0)
                        remade[end] = count - 1;
                    else
                        lost++;
                }
            }
        }

        return lost;
    }
}
