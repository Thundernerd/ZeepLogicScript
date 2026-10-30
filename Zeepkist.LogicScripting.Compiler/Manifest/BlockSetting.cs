using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// A Setting a block type takes as a named argument, and the key it's saved under.
/// </summary>
public sealed class BlockSetting
{
    /// <summary>
    /// <c>hide:</c>, which every Block Function takes: hides the block and its Wires in game.
    /// </summary>
    public static readonly BlockSetting Hide = Bool("hide", "lbhd");

    public const int MaxTargets = 8;

    private BlockSetting(
        string name,
        string key,
        SettingType type,
        IReadOnlyList<string> options,
        bool isRequired,
        long minimum = long.MinValue,
        long maximum = long.MaxValue)
    {
        Name = name;
        Key = key;
        Type = type;
        Options = options;
        IsRequired = isRequired;
        Minimum = minimum;
        Maximum = maximum;
    }

    public string Name { get; }
    public string Key { get; }
    public SettingType Type { get; }

    /// <summary>
    /// The option words in stored order; empty unless <see cref="Type"/> is <see cref="SettingType.Option"/> or
    /// <see cref="SettingType.Player"/>.
    /// </summary>
    public IReadOnlyList<string> Options { get; }

    /// <summary>
    /// Whether a call must give it, because the block's own value is known to break the level.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// The lowest number a <see cref="SettingType.Number"/> Setting accepts, for values known to break the level or
    /// that the block wouldn't keep.
    /// </summary>
    public long Minimum { get; }

    /// <summary>
    /// The highest number a <see cref="SettingType.Number"/> Setting accepts, for values the block wouldn't keep.
    /// </summary>
    public long Maximum { get; }

    /// <summary>
    /// The words a Script can give it, for completion: none for a number, and null for placed blocks, which are
    /// names the Script defines.
    /// </summary>
    public IReadOnlyList<string> Values => Type switch
    {
        SettingType.Bool => ["true", "false"],
        SettingType.Option or SettingType.Player => Options,
        SettingType.Targets => null,
        _ => []
    };

    public static BlockSetting Number(
        string name,
        string key,
        bool isRequired = false,
        long minimum = long.MinValue,
        long maximum = long.MaxValue)
    {
        return new BlockSetting(name, key, SettingType.Number, [], isRequired, minimum, maximum);
    }

    public static BlockSetting Bool(string name, string key)
    {
        return new BlockSetting(name, key, SettingType.Bool, [], false);
    }

    public static BlockSetting Option(string name, string key, params string[] options)
    {
        return new BlockSetting(name, key, SettingType.Option, options, false);
    }

    /// <summary>
    /// <c>player:</c>, with the players in the order the editor lists them; <paramref name="hasEveryone"/> when the
    /// list starts with everyone.
    /// </summary>
    public static BlockSetting Player(string key, bool hasEveryone)
    {
        string[] options = hasEveryone ? ["all", "1", "2", "3", "4"] : ["1", "2", "3", "4"];
        return new BlockSetting("player", key, SettingType.Player, options, false);
    }

    /// <summary>
    /// A required list of Placed Block References, stored as one key per target: the prefix and 1, 2, ….
    /// </summary>
    public static BlockSetting Targets(string name, string keyPrefix)
    {
        return new BlockSetting(name, keyPrefix, SettingType.Targets, [], true);
    }

    public Setting Choose(string option)
    {
        var index = IndexOf(option);
        if (index < 0)
            throw new ArgumentException($"'{option}' isn't an option of {Name}", nameof(option));

        return Setting.Int(Key, index);
    }

    public int IndexOf(string option)
    {
        for (var i = 0; i < Options.Count; i++)
        {
            if (Options[i] == option)
                return i;
        }

        return -1;
    }
}
