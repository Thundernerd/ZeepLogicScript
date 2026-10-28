using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// The window's Reference tab, made from the Block Manifest so it can't drift from what Compile accepts.
/// </summary>
public static class BlockReference
{
    private const string None = "–";

    /// <summary>
    /// One per <see cref="BlockManifest.All"/> type, in its order.
    /// </summary>
    public static IReadOnlyList<BlockReferenceEntry> Entries { get; } = BlockManifest.All.Select(Describe).ToList();

    private static BlockReferenceEntry Describe(BlockType type)
    {
        var aliases = BlockManifest.Aliases.Where(a => a.Value == type).Select(a => a.Key);
        return new BlockReferenceEntry(
            type.Function,
            string.Join(", ", aliases),
            JoinNames(type.Inputs),
            DescribeOutputs(type),
            DescribeSettings(type),
            type.IsPlacedOnly);
    }

    private static List<BlockReferenceRow> DescribeOutputs(BlockType type)
    {
        if (type.Modes == null)
            return [new BlockReferenceRow(null, JoinNames(type.Outputs))];

        var options = type.Modes.Setting.Options;
        return options.Select((option, i) => new BlockReferenceRow(option, JoinNames(type.Modes.Outputs[i]))).ToList();
    }

    private static List<BlockReferenceRow> DescribeSettings(BlockType type)
    {
        var settings = type.Settings.Select(s => new BlockReferenceRow(s.Name, DescribeSetting(s))).ToList();
        if (type.HasStyles)
            settings.Add(new BlockReferenceRow(BlockType.StyleName, string.Join(", ", type.Styles.Select(s => s.Name))));

        if (!type.IsPlacedOnly)
            settings.Add(new BlockReferenceRow(BlockSetting.Hide.Name, DescribeSetting(BlockSetting.Hide)));

        return settings;
    }

    /// <summary>
    /// What the Setting accepts.
    /// </summary>
    private static string DescribeSetting(BlockSetting setting)
    {
        var accepts = setting.Type switch
        {
            SettingType.Number => DescribeNumber(setting),
            SettingType.Bool => "true, false",
            SettingType.Targets => $"1–{BlockSetting.MaxTargets} placed blocks",
            _ => string.Join(", ", setting.Options)
        };

        return setting.IsRequired && setting.Type != SettingType.Targets ? $"{accepts} (required)" : accepts;
    }

    private static string DescribeNumber(BlockSetting setting)
    {
        var hasMinimum = setting.Minimum != long.MinValue;

        // An int's limit only keeps the stored value readable; nobody types a number that big
        var hasMaximum = setting.Maximum != long.MaxValue && setting.Maximum != int.MaxValue;
        if (hasMinimum && hasMaximum)
            return $"number {setting.Minimum}–{setting.Maximum}";

        if (hasMinimum)
            return $"number, at least {setting.Minimum}";

        return hasMaximum ? $"number, at most {setting.Maximum}" : "number";
    }

    private static string JoinNames(IReadOnlyList<BlockConnector> connectors)
    {
        return connectors.Count == 0 ? None : string.Join(", ", connectors.Select(c => c.Name));
    }
}
