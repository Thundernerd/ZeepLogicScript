using System;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One Logic Block type in the Block Manifest: its Block Function name, block ID, Connectors and footprint.
/// </summary>
public sealed class BlockType
{
    public BlockType(
        string function,
        int blockId,
        int length,
        IReadOnlyList<BlockConnector> inputs,
        IReadOnlyList<BlockConnector> outputs,
        params BlockSetting[] settings)
    {
        Function = function;
        BlockId = blockId;
        Length = length;
        Inputs = inputs;
        Outputs = outputs;
        Settings = settings;
    }

    public string Function { get; }
    public int BlockId { get; }

    /// <summary>
    /// Footprint along the block's own +Z (the direction Signals flow), in 16-unit cells.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Input Connectors in the order positional arguments fill them.
    /// </summary>
    public IReadOnlyList<BlockConnector> Inputs { get; }

    /// <summary>
    /// Output Connectors; a block with more than one is used through dot access.
    /// </summary>
    public IReadOnlyList<BlockConnector> Outputs { get; }

    public IReadOnlyList<BlockSetting> Settings { get; }

    /// <summary>
    /// The position of the named input in <see cref="Inputs"/>, or -1.
    /// </summary>
    public int IndexOfInput(string name)
    {
        for (var i = 0; i < Inputs.Count; i++)
        {
            if (Inputs[i].Name == name)
                return i;
        }

        return -1;
    }

    public BlockConnector FindOutput(string name)
    {
        foreach (var output in Outputs)
        {
            if (output.Name == name)
                return output;
        }

        return null;
    }

    public BlockSetting FindSetting(string name)
    {
        foreach (var setting in Settings)
        {
            if (setting.Name == name)
                return setting;
        }

        return null;
    }

    /// <summary>
    /// The input or Setting name a misspelled <paramref name="name"/> most likely meant, or null when none is close.
    /// </summary>
    public string FindClosestName(string name)
    {
        string closest = null;
        var closestDistance = int.MaxValue;
        foreach (var candidate in Inputs.Select(i => i.Name).Concat(Settings.Select(s => s.Name)))
        {
            var distance = GetEditDistance(name, candidate);
            if (distance <= 2 && distance < Math.Min(name.Length, candidate.Length) && distance < closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }

        return closest;
    }

    /// <summary>
    /// The given Settings in the order <see cref="Settings"/> declares them, whatever order they were given in.
    /// </summary>
    public Setting[] OrderSettings(IReadOnlyDictionary<BlockSetting, Setting> settings)
    {
        List<Setting> ordered = [];
        foreach (var blockSetting in Settings)
        {
            if (settings.TryGetValue(blockSetting, out var setting))
                ordered.Add(setting);
        }

        return ordered.ToArray();
    }

    private static int GetEditDistance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(substitution, Math.Min(previous[j], current[j - 1]) + 1);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
