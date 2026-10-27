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
    public static List<BlockPropertyJSON> Create(CompileResult result, Vector3 origin)
    {
        List<BlockPropertyJSON> jsons = [];
        foreach (var block in result.Blocks)
        {
            var json = new BlockPropertyJSON
            {
                i = block.Type.BlockId,
                u = PlayerManager.Instance.GenerateUniqueIDforBlocks(block.Type.BlockId.ToString()),
                p = new CV3(origin + new Vector3(block.Cell.X, 0, block.Cell.Z) * GridCell.Size),
                r = new CV3(new Vector3(0, block.Yaw, 0)),
                s = new CV3(Vector3.one),
                d = new PropertyDictionariesJSON()
            };

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
            AddWire(jsons[wire.From], wire.FromConnector, jsons[wire.To], wire.ToConnector);
        }

        return jsons;
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
