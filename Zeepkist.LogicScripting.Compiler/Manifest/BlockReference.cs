using System;
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
    /// One per <see cref="BlockCategory.All"/> category, in its order, with every <see cref="BlockManifest.All"/> type
    /// in it.
    /// </summary>
    public static IReadOnlyList<BlockReferenceCategory> Categories { get; } = BlockCategory.All.Select(DescribeCategory).ToList();

    /// <summary>
    /// The entry the Reference tab shows for a type in <see cref="BlockManifest.All"/>.
    /// </summary>
    public static BlockReferenceEntry GetEntry(BlockType type)
    {
        return Categories.SelectMany(c => c.Entries).First(e => e.Function == type.Function);
    }

    private static BlockReferenceCategory DescribeCategory(BlockCategory category)
    {
        var entries = BlockManifest.All
            .Where(t => t.Category == category)
            .OrderBy(t => t.Function, StringComparer.Ordinal)
            .Select(Describe)
            .ToList();
        return new BlockReferenceCategory(category, entries);
    }

    private static BlockReferenceEntry Describe(BlockType type)
    {
        var aliases = BlockManifest.Aliases.Where(a => a.Value == type).Select(a => a.Key);
        return new BlockReferenceEntry(
            type.Function,
            string.Join(", ", aliases),
            JoinNames(type.Inputs),
            type.Modes == null ? JoinNames(type.Outputs) : null,
            DescribeModeOutputs(type),
            DescribeSettings(type),
            type.IsPlacedOnly);
    }

    private static List<BlockReferenceRow> DescribeModeOutputs(BlockType type)
    {
        if (type.Modes == null)
            return [];

        var options = type.Modes.Setting.Options;
        return options.Select((option, i) => new BlockReferenceRow(option, JoinNames(type.Modes.Outputs[i]))).ToList();
    }

    private static List<BlockReferenceRow> DescribeSettings(BlockType type)
    {
        var settings = type.Settings.Select(DescribeSetting).ToList();
        if (type.HasStyles)
            settings.Add(new BlockReferenceRow(BlockType.StyleName, string.Join(", ", type.Styles.Select(s => s.Name))));

        if (!type.IsPlacedOnly)
            settings.Add(DescribeSetting(BlockSetting.Hide));

        return settings;
    }

    private static BlockReferenceRow DescribeSetting(BlockSetting setting)
    {
        return new BlockReferenceRow(setting.Name, DescribeValues(setting));
    }

    /// <summary>
    /// The values a Setting accepts, and whether a call must give it.
    /// </summary>
    internal static string DescribeValues(BlockSetting setting)
    {
        var accepts = setting.Type switch
        {
            SettingType.Number => DescribeNumber(setting),
            SettingType.Targets => $"1–{BlockSetting.MaxTargets} placed blocks",
            _ => string.Join(", ", setting.Values)
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
