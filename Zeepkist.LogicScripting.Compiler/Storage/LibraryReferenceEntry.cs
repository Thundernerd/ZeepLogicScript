using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// One Library Function as the window's Reference tab shows it.
/// </summary>
public sealed class LibraryReferenceEntry
{
    private const string SettingMark = " (setting)";

    private readonly IReadOnlyList<string> _names;

    /// <param name="library">The Library's name.</param>
    /// <param name="functionName">The Function's own name, without the Library's.</param>
    /// <param name="inputs">The input names, in order.</param>
    /// <param name="settingInputs">The inputs its body uses as a Setting.</param>
    /// <param name="outputs">The output names, in order.</param>
    /// <param name="libraryHasErrors">Whether the Library's text has an error.</param>
    public LibraryReferenceEntry(
        string library,
        string functionName,
        IReadOnlyList<string> inputs,
        IReadOnlyCollection<string> settingInputs,
        IReadOnlyList<string> outputs,
        bool libraryHasErrors)
    {
        Function = $"{library}.{functionName}";
        Inputs = ReferenceText.JoinNames(inputs.Select(i => settingInputs.Contains(i) ? i + SettingMark : i));
        Outputs = ReferenceText.JoinNames(outputs);
        _names = inputs.Concat(outputs).ToList();
        LibraryHasErrors = libraryHasErrors;
    }

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
    /// A Script can't call the Function until its Library's errors are fixed, as any of them stops every call into it.
    /// </summary>
    public bool LibraryHasErrors { get; }

    /// <summary>
    /// Whether the qualified name, an input's or an output's name contains <paramref name="filter"/>, ignoring case;
    /// a blank filter matches every entry.
    /// </summary>
    public bool Matches(string filter)
    {
        var trimmed = filter.Trim();
        return trimmed.Length == 0 || ReferenceText.Contains(Function, trimmed) || _names.Any(n => ReferenceText.Contains(n, trimmed));
    }
}
