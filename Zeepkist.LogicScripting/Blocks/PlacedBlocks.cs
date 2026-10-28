using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Describes blocks already in the level for Placed Block References.
/// </summary>
internal static class PlacedBlocks
{
    /// <summary>
    /// What the compiler needs to know about the block with a UID, or null when the level has none. The Block Group of
    /// the Script being compiled is left out of its Wires: a recompile replaces it, Wires and all.
    /// </summary>
    public static PlacedBlock Find(LEV_LevelEditorCentral central, string uid, string scriptId)
    {
        var block = LevelBlocks.Find(central, uid);
        if (block == null)
            return null;

        var isInBlockGroup = BlockGroups.IsInAnyGroup(block);
        if (!BlockManifest.TryGetBlock(block.blockID, out var type, out _))
            return new PlacedBlock(block.blockID, isInBlockGroup: isInBlockGroup);

        List<Setting> settings = [];
        if (type.Modes != null)
        {
            var key = type.Modes.Setting.Key;
            settings.Add(Setting.Int(key, block.customBlockEditProperties.GetIntDictionaryValue(key)));
        }

        List<int> wiredInputs = [];
        foreach (var input in type.Inputs)
        {
            if (GetWires(block, input.Index).Any(w => !BlockGroups.IsInGroup(central, w.targetUID, scriptId)))
                wiredInputs.Add(input.Index);
        }

        return new PlacedBlock(block.blockID, settings, wiredInputs, GetDownstream(central, block, type, scriptId), isInBlockGroup);
    }

    /// <summary>
    /// Follows the level's Wires out of the block, stopping at Delays, which pass a Signal on only in the next Tick, and
    /// at the compiling Script's own Block Group, which the Compile replaces.
    /// </summary>
    private static HashSet<string> GetDownstream(LEV_LevelEditorCentral central, BlockProperties start, BlockType startType, string scriptId)
    {
        HashSet<string> reached = [];
        Stack<(BlockProperties Block, BlockType Type)> open = new([(start, startType)]);
        while (open.Count > 0)
        {
            var (block, type) = open.Pop();
            foreach (var output in type.Outputs)
            {
                foreach (var wire in GetWires(block, output.Index))
                {
                    if (BlockGroups.IsInGroup(central, wire.targetUID, scriptId) || !reached.Add(wire.targetUID))
                        continue;

                    var next = LevelBlocks.Find(central, wire.targetUID);
                    if (next != null
                        && BlockManifest.TryGetBlock(next.blockID, out var nextType, out _)
                        && nextType != BlockManifest.Delay)
                        open.Push((next, nextType));
                }
            }
        }

        return reached;
    }

    /// <summary>
    /// The Wires on one of the block's Connectors; each holds the block and Connector at its other end.
    /// </summary>
    private static List<ConnectionStruct> GetWires(BlockProperties block, int connector)
    {
        return block.allConnectorHubsOnThisBlock.TryGetValue(connector, out var hub) && hub != null
            ? hub.GetThisHubsConnectionStructs() ?? []
            : [];
    }
}
