using System;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One block type as the window's Reference tab shows it.
/// </summary>
public sealed class BlockReferenceEntry
{
    public BlockReferenceEntry(
        string function,
        string aliases,
        string inputs,
        string outputs,
        IReadOnlyList<BlockReferenceRow> modeOutputs,
        IReadOnlyList<BlockReferenceRow> settings,
        bool isPlacedOnly)
    {
        Function = function;
        Aliases = aliases;
        Inputs = inputs;
        Outputs = outputs;
        ModeOutputs = modeOutputs;
        Settings = settings;
        IsPlacedOnly = isPlacedOnly;
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
    public IReadOnlyList<BlockReferenceRow> ModeOutputs { get; }

    /// <summary>
    /// Empty when the type takes none.
    /// </summary>
    public IReadOnlyList<BlockReferenceRow> Settings { get; }

    /// <summary>
    /// A type Scripts only reach through <c>placed("UID")</c>.
    /// </summary>
    public bool IsPlacedOnly { get; }

    /// <summary>
    /// Whether any part contains <paramref name="filter"/>, ignoring case; a blank filter matches every entry.
    /// </summary>
    public bool Matches(string filter)
    {
        var trimmed = filter.Trim();
        return trimmed.Length == 0
            || Contains(Function, trimmed)
            || Contains(Aliases, trimmed)
            || Contains(Inputs, trimmed)
            || (Outputs != null && Contains(Outputs, trimmed))
            || ModeOutputs.Concat(Settings).Any(r => Contains(r.Name, trimmed) || Contains(r.Value, trimmed));
    }

    private static bool Contains(string text, string filter)
    {
        return text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
