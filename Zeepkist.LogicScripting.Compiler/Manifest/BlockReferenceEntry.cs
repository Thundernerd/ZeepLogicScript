using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One block type as the window's Reference tab shows it.
/// </summary>
public sealed class BlockReferenceEntry
{
    private const string PlacedOnlyNote = "placed(\"UID\") only";

    public BlockReferenceEntry(
        string function,
        string aliases,
        string inputs,
        string outputs,
        IReadOnlyList<ReferenceRow> modeOutputs,
        IReadOnlyList<ReferenceRow> settings,
        bool isPlacedOnly)
    {
        Function = function;
        Aliases = aliases;
        Inputs = inputs;
        Outputs = outputs;
        ModeOutputs = modeOutputs;
        Settings = settings;
        IsPlacedOnly = isPlacedOnly;
        Layout = CreateLayout();
    }

    public string Function { get; }

    /// <summary>
    /// Empty when the type has none.
    /// </summary>
    public string Aliases { get; }

    public string Inputs { get; }

    /// <summary>
    /// Null for a block whose mode renames its outputs; <see cref="ModeOutputs"/> has them then.
    /// </summary>
    public string Outputs { get; }

    /// <summary>
    /// One row per mode, empty unless the mode renames the outputs.
    /// </summary>
    public IReadOnlyList<ReferenceRow> ModeOutputs { get; }

    /// <summary>
    /// Empty when the type takes none.
    /// </summary>
    public IReadOnlyList<ReferenceRow> Settings { get; }

    /// <summary>
    /// A type Scripts only reach through <c>placed("UID")</c>.
    /// </summary>
    public bool IsPlacedOnly { get; }

    public ReferenceEntryLayout Layout { get; }

    /// <summary>
    /// Whether any part contains <paramref name="filter"/>, ignoring case; a blank filter matches every entry.
    /// </summary>
    public bool Matches(string filter)
    {
        var trimmed = filter.Trim();
        return trimmed.Length == 0
            || ReferenceText.Contains(Function, trimmed)
            || ReferenceText.Contains(Aliases, trimmed)
            || ReferenceText.Contains(Inputs, trimmed)
            || (Outputs != null && ReferenceText.Contains(Outputs, trimmed))
            || ModeOutputs.Concat(Settings).Any(r => ReferenceText.Contains(r.Name, trimmed) || ReferenceText.Contains(r.Value, trimmed));
    }

    private ReferenceEntryLayout CreateLayout()
    {
        List<ReferenceRow> rows = [new ReferenceRow(ReferenceEntryLayout.InputsLabel, Inputs)];
        List<ReferenceEntryGroup> groups = [];
        if (Outputs != null)
            rows.Add(new ReferenceRow(ReferenceEntryLayout.OutputsLabel, Outputs));
        else
            groups.Add(new ReferenceEntryGroup(ReferenceEntryLayout.OutputsLabel, ModeOutputs));

        if (Settings.Count > 0)
            groups.Add(new ReferenceEntryGroup(ReferenceEntryLayout.SettingsLabel, Settings));

        return new ReferenceEntryLayout(Function, GetNote(), rows, groups);
    }

    private string GetNote()
    {
        if (IsPlacedOnly)
            return PlacedOnlyNote;

        return Aliases.Length > 0 ? $"alias {Aliases}" : string.Empty;
    }
}
