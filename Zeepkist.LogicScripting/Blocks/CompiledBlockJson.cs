using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Turns the compiler's plain block data into the game's block JSON, Wires included.
/// </summary>
internal static class CompiledBlockJson
{
    private const string KeyConnectionBase = "id";

    /// <param name="result">A successful Compile.</param>
    /// <param name="origin">World position of the group's centre cell.</param>
    /// <param name="scriptId">The Script whose Block Group the blocks join.</param>
    public static List<BlockPropertyJSON> Create(CompileResult result, Vector3 origin, string scriptId)
    {
        List<BlockPropertyJSON> jsons = [];
        foreach (var block in result.Blocks)
        {
            var json = new BlockPropertyJSON
            {
                i = block.Style.BlockId,
                u = PlayerManager.Instance.GenerateUniqueIDforBlocks(block.Style.BlockId.ToString()),
                p = new CV3(origin + new Vector3(block.Cell.X, 0, block.Cell.Z) * GridCell.Size),
                r = new CV3(new Vector3(0, block.Yaw, 0)),
                s = new CV3(Vector3.one),
                d = new PropertyDictionariesJSON()
            };

            json.d.SetStringDictionaryValue(BlockGroupTag.Key, BlockGroupTag.Create(scriptId, json.u));

            foreach (var setting in block.Settings)
            {
                if (setting.Kind == SettingKind.Int)
                    json.d.SetIntDictionaryValue(setting.Key, setting.IntValue);
                else
                    json.d.SetStringDictionaryValue(setting.Key, setting.TextValue);
            }

            jsons.Add(json);
        }

        foreach (var wire in result.Wires)
        {
            AddWire(jsons[wire.From.Block], wire.FromConnector, jsons[wire.To.Block], wire.ToConnector);
        }

        return jsons;
    }

    /// <summary>
    /// The Wires into and out of blocks already in the level, with the new blocks' UIDs from <paramref name="jsons"/>.
    /// </summary>
    /// <param name="result">A successful Compile.</param>
    /// <param name="jsons">The blocks <see cref="Create"/> made from it.</param>
    public static List<BlockWire> CreatePlacedWires(CompileResult result, IReadOnlyList<BlockPropertyJSON> jsons)
    {
        List<BlockWire> wires = [];
        foreach (var wire in result.PlacedWires)
        {
            wires.Add(new BlockWire(GetUid(wire.From, jsons), wire.FromConnector, GetUid(wire.To, jsons), wire.ToConnector));
        }

        return wires;
    }

    private static string GetUid(WireEnd end, IReadOnlyList<BlockPropertyJSON> jsons)
    {
        return end.IsPlaced ? end.Uid : jsons[end.Block].u;
    }

    /// <summary>
    /// Writes the Wire into both blocks' dictionaries, like LEV_ConnectorTool does via Connect on both ends.
    /// The output end is the drawing leader.
    /// </summary>
    private static void AddWire(BlockPropertyJSON from, int fromConnector, BlockPropertyJSON to, int toConnector)
    {
        AddConnection(from, fromConnector, new ConnectionStruct(to.u, toConnector, true));
        AddConnection(to, toConnector, new ConnectionStruct(from.u, fromConnector, false));
    }

    private static void AddConnection(BlockPropertyJSON json, int connector, ConnectionStruct connection)
    {
        var countKey = KeyConnectionBase + connector;
        var count = json.d.GetIntDictionaryValue(countKey);
        json.d.SetStringDictionaryValue(countKey + "-" + count, connection.Serialize());
        json.d.SetIntDictionaryValue(countKey, count + 1);
    }
}
