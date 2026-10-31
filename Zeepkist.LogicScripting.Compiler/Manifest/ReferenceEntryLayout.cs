using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// What the window's Reference tab and the hover box show for an entry, top to bottom: the name, a note, a row per
/// part, then each group of named rows under its heading.
/// </summary>
public sealed class ReferenceEntryLayout
{
    public const string InputsLabel = "inputs";
    public const string OutputsLabel = "outputs";
    public const string SettingsLabel = "settings";

    public ReferenceEntryLayout(string function, string note, IReadOnlyList<ReferenceRow> rows, IReadOnlyList<ReferenceEntryGroup> groups)
    {
        Function = function;
        Note = note;
        Rows = rows;
        Groups = groups;
    }

    public string Function { get; }

    /// <summary>
    /// Empty when there's nothing to say.
    /// </summary>
    public string Note { get; }

    /// <summary>
    /// The inputs and, unless a group has them per mode, the outputs, labeled.
    /// </summary>
    public IReadOnlyList<ReferenceRow> Rows { get; }

    public IReadOnlyList<ReferenceEntryGroup> Groups { get; }
}
