using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Describes blocks already in the level for Placed Block References.
/// </summary>
internal static class PlacedBlocks
{
    /// <summary>
    /// What the compiler needs to know about the block with a UID, or null when the level has none.
    /// </summary>
    public static PlacedBlock Find(LEV_LevelEditorCentral central, string uid)
    {
        var block = LevelBlocks.Find(central, uid);
        if (block == null)
            return null;

        if (!BlockManifest.TryGetBlock(block.blockID, out var type, out _))
            return new PlacedBlock(block.blockID);

        List<Setting> settings = [];
        if (type.Modes != null)
        {
            var key = type.Modes.Setting.Key;
            settings.Add(Setting.Int(key, block.customBlockEditProperties.GetIntDictionaryValue(key)));
        }

        List<int> wiredInputs = [];
        foreach (var input in type.Inputs)
        {
            if (GetWires(block, input.Index).Count > 0)
                wiredInputs.Add(input.Index);
        }

        return new PlacedBlock(block.blockID, settings, wiredInputs, GetDownstream(central, block, type));
    }

    /// <summary>
    /// Follows the level's Wires out of the block, stopping at Delays, which pass a Signal on only in the next Tick.
    /// </summary>
    private static HashSet<string> GetDownstream(LEV_LevelEditorCentral central, BlockProperties start, BlockType startType)
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
                    if (!reached.Add(wire.targetUID))
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
