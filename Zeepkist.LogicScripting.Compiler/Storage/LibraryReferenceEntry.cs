using System;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// One Library Function as the window's Reference tab shows it.
/// </summary>
public sealed class LibraryReferenceEntry
{
    private const string None = "–";
    private const string SettingMark = " (setting)";

    private readonly IReadOnlyList<string> _names;

    /// <param name="library"></param>
    /// <param name="name">The Function's own name.</param>
    /// <param name="inputs"></param>
    /// <param name="settingInputs">The inputs its body uses as a Setting.</param>
    /// <param name="outputs"></param>
    public LibraryReferenceEntry(
        string library,
        string name,
        IReadOnlyList<string> inputs,
        IReadOnlyCollection<string> settingInputs,
        IReadOnlyList<string> outputs)
    {
        Library = library;
        Function = $"{library}.{name}";
        Inputs = JoinNames(inputs.Select(i => settingInputs.Contains(i) ? i + SettingMark : i));
        Outputs = JoinNames(outputs);
        _names = inputs.Concat(outputs).ToList();
    }

    public string Library { get; }

    /// <summary>
    /// The qualified name a Script calls, <c>timing.edge</c>.
    /// </summary>
    public string Function { get; }

    /// <summary>
    /// Each input used as a Setting is marked, as a call passes it a constant instead of a Wire.
    /// </summary>
    public string Inputs { get; }

    public string Outputs { get; }

    /// <summary>
    /// Whether the qualified name, an input's or an output's name contains <paramref name="filter"/>, ignoring case;
    /// a blank filter matches every entry.
    /// </summary>
    public bool Matches(string filter)
    {
        var trimmed = filter.Trim();
        return trimmed.Length == 0 || Contains(Function, trimmed) || _names.Any(n => Contains(n, trimmed));
    }

    private static bool Contains(string text, string filter)
    {
        return text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string JoinNames(IEnumerable<string> names)
    {
        var joined = string.Join(", ", names);
        return joined.Length == 0 ? None : joined;
    }
}
