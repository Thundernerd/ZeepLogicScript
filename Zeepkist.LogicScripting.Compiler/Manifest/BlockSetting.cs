using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// What a Setting accepts in a Script and how its value is stored.
/// </summary>
public enum SettingType
{
    /// <summary>
    /// A whole number, stored as text.
    /// </summary>
    Number,

    /// <summary>
    /// <c>true</c> or <c>false</c>, stored as 1 or 0.
    /// </summary>
    Bool,

    /// <summary>
    /// A bare word from <see cref="BlockSetting.Options"/>, stored as its index.
    /// </summary>
    Option
}

/// <summary>
/// A Setting a block type takes as a named argument, and the key it's saved under.
/// </summary>
public sealed class BlockSetting
{
    private BlockSetting(string name, string key, SettingType type, IReadOnlyList<string> options, bool isRequired, long minimum)
    {
        Name = name;
        Key = key;
        Type = type;
        Options = options;
        IsRequired = isRequired;
        Minimum = minimum;
    }

    public string Name { get; }
    public string Key { get; }
    public SettingType Type { get; }

    /// <summary>
    /// The option words in stored order; empty unless <see cref="Type"/> is <see cref="SettingType.Option"/>.
    /// </summary>
    public IReadOnlyList<string> Options { get; }

    /// <summary>
    /// Whether a call must give it, because the block's own value is known to break the level.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// The lowest number a <see cref="SettingType.Number"/> Setting accepts, for values known to break the level.
    /// </summary>
    public long Minimum { get; }

    public static BlockSetting Number(string name, string key, bool isRequired = false, long minimum = long.MinValue)
    {
        return new BlockSetting(name, key, SettingType.Number, [], isRequired, minimum);
    }

    public static BlockSetting Bool(string name, string key)
    {
        return new BlockSetting(name, key, SettingType.Bool, [], false, 0);
    }

    public static BlockSetting Option(string name, string key, params string[] options)
    {
        return new BlockSetting(name, key, SettingType.Option, options, false, 0);
    }

    /// <summary>
    /// The stored Setting for one of <see cref="Options"/>.
    /// </summary>
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
