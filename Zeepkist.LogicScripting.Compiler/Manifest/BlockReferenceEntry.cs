using System;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One block type as the window's Reference tab shows it, each part as one line of text.
/// </summary>
public sealed class BlockReferenceEntry
{
    public BlockReferenceEntry(string function, string aliases, string inputs, string outputs, string settings, bool isPlacedOnly)
    {
        Function = function;
        Aliases = aliases;
        Inputs = inputs;
        Outputs = outputs;
        Settings = settings;
        IsPlacedOnly = isPlacedOnly;
    }

    public string Function { get; }

    /// <summary>
    /// Empty when the type has none.
    /// </summary>
    public string Aliases { get; }

    public string Inputs { get; }
    public string Outputs { get; }
    public string Settings { get; }

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
            || Contains(Outputs, trimmed)
            || Contains(Settings, trimmed);
    }

    private static bool Contains(string text, string filter)
    {
        return text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
