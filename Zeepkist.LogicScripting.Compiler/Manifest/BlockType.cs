using System;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One Logic Block type in the Block Manifest: its Block Function name, the blocks it can place, its Connectors and
/// its Settings.
/// </summary>
public sealed class BlockType
{
    /// <summary>
    /// The argument that picks one of several <see cref="Styles"/>.
    /// </summary>
    public const string StyleName = "style";

    public BlockType(
        string function,
        int blockId,
        int length,
        IReadOnlyList<BlockConnector> inputs,
        IReadOnlyList<BlockConnector> outputs,
        params BlockSetting[] settings)
        : this(function, [new BlockStyle(null, blockId, length)], inputs, outputs, settings)
    {
    }

    public BlockType(
        string function,
        IReadOnlyList<BlockStyle> styles,
        IReadOnlyList<BlockConnector> inputs,
        IReadOnlyList<BlockConnector> outputs,
        IReadOnlyList<BlockSetting> settings = null,
        BlockModes modes = null,
        bool isPlacedOnly = false,
        BlockRange range = null)
    {
        Function = function;
        Styles = styles;
        Inputs = inputs;
        Outputs = outputs;
        Settings = settings ?? [];
        Modes = modes;
        IsPlacedOnly = isPlacedOnly;
        Range = range;
    }

    /// <summary>
    /// The Block Function name; for a placed-only type, the name a Placed Block Reference is checked against.
    /// </summary>
    public string Function { get; }

    /// <summary>
    /// The blocks this type can be, one per block ID. The first is what a call without <c>style:</c> places.
    /// </summary>
    public IReadOnlyList<BlockStyle> Styles { get; }

    public BlockStyle DefaultStyle => Styles[0];
    public bool HasStyles => Styles.Count > 1;

    /// <summary>
    /// In the order positional arguments fill them.
    /// </summary>
    public IReadOnlyList<BlockConnector> Inputs { get; }

    /// <summary>
    /// A block with more than one is used through dot access. For a block with <see cref="Modes"/>, these are the
    /// names in its first mode.
    /// </summary>
    public IReadOnlyList<BlockConnector> Outputs { get; }

    public IReadOnlyList<BlockSetting> Settings { get; }

    /// <summary>
    /// The Setting that renames the outputs per mode; null when the outputs never change.
    /// </summary>
    public BlockModes Modes { get; }

    /// <summary>
    /// A block whose position matters: Scripts only reach it through a Placed Block Reference, never place it.
    /// </summary>
    public bool IsPlacedOnly { get; }

    /// <summary>
    /// Two of <see cref="Settings"/> whose minimum must not be above the maximum, or null.
    /// </summary>
    public BlockRange Range { get; }

    public int IndexOfInput(string name)
    {
        for (var i = 0; i < Inputs.Count; i++)
        {
            if (Inputs[i].Name == name)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// The outputs of a block with the given Settings; they differ from <see cref="Outputs"/> only per mode.
    /// </summary>
    public IReadOnlyList<BlockConnector> GetOutputs(IReadOnlyList<Setting> settings)
    {
        return Modes == null ? Outputs : Modes.Outputs[GetModeIndex(settings)];
    }

    public string GetModeName(IReadOnlyList<Setting> settings)
    {
        return Modes.Setting.Options[GetModeIndex(settings)];
    }

    /// <summary>
    /// The mode a block with the given Settings is in; 0, the fresh block's mode, when the Settings leave it out.
    /// </summary>
    public int GetModeIndex(IReadOnlyList<Setting> settings)
    {
        foreach (var setting in settings)
        {
            if (setting.Key == Modes.Setting.Key)
                return setting.IntValue;
        }

        return 0;
    }

    public string Describe(BlockStyle style)
    {
        return HasStyles ? $"{Function} style {style.Name}" : Function;
    }

    public BlockStyle FindStyle(string name)
    {
        foreach (var style in Styles)
        {
            if (style.Name == name)
                return style;
        }

        return null;
    }

    /// <summary>
    /// One of <see cref="Settings"/>, or <see cref="BlockSetting.Hide"/>, which every Block Function takes.
    /// </summary>
    public BlockSetting FindSetting(string name)
    {
        foreach (var setting in Settings)
        {
            if (setting.Name == name)
                return setting;
        }

        return !IsPlacedOnly && name == BlockSetting.Hide.Name ? BlockSetting.Hide : null;
    }

    /// <summary>
    /// The input or Setting name a misspelled <paramref name="name"/> most likely meant, or null when none is close.
    /// </summary>
    public string FindClosestName(string name)
    {
        string closest = null;
        var closestDistance = int.MaxValue;
        foreach (var candidate in GetArgumentNames())
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
    /// The given Settings, each stored under one or more keys, in the order <see cref="Settings"/> declares them,
    /// <c>hide:</c> last, whatever order they were given in.
    /// </summary>
    public Setting[] OrderSettings(IReadOnlyDictionary<BlockSetting, IReadOnlyList<Setting>> settings)
    {
        List<Setting> ordered = [];
        foreach (var blockSetting in Settings.Append(BlockSetting.Hide))
        {
            if (settings.TryGetValue(blockSetting, out var stored))
                ordered.AddRange(stored);
        }

        return ordered.ToArray();
    }

    private IEnumerable<string> GetArgumentNames()
    {
        var names = Inputs.Select(i => i.Name).Concat(Settings.Select(s => s.Name));
        if (!IsPlacedOnly)
            names = names.Append(BlockSetting.Hide.Name);

        if (HasStyles)
            names = names.Append(StyleName);

        return names;
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
