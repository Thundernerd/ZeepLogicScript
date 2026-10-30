using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// The Libraries' part of the window's Reference tab, read from their text so it can't drift from what Compile accepts.
/// </summary>
public static class LibraryReference
{
    /// <summary>
    /// Every Function of every Library, sorted by qualified name.
    /// </summary>
    public static IReadOnlyList<LibraryReferenceEntry> Describe(LibraryList libraries)
    {
        return libraries.Entries
            .SelectMany(l => GraphBuilder.DescribeLibrary(l.Name, libraries.FindText).Select(f => new LibraryReferenceEntry(
                l.Name,
                f.Function.Name.Name,
                f.Function.Inputs.Select(i => i.Name).ToList(),
                f.SettingInputs,
                f.Function.Outputs.Select(o => o.Name).ToList())))
            .OrderBy(e => e.Function, StringComparer.Ordinal)
            .ToList();
    }
}
