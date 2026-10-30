using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

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
            .SelectMany(l => Describe(l.Name, libraries.FindText))
            .OrderBy(e => e.Function, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<LibraryReferenceEntry> Describe(string library, Func<string, string> findLibrary)
    {
        var functions = GraphBuilder.DescribeLibrary(library, findLibrary, out var hasErrors);
        return functions.Select(f => Describe(library, f.Function, f.SettingInputs, hasErrors));
    }

    private static LibraryReferenceEntry Describe(
        string library,
        FunctionDefinition function,
        IReadOnlyCollection<string> settingInputs,
        bool libraryHasErrors)
    {
        return new LibraryReferenceEntry(
            library,
            function.Name.Name,
            function.Inputs.Select(i => i.Name).ToList(),
            settingInputs,
            function.Outputs.Select(o => o.Name).ToList(),
            libraryHasErrors);
    }
}
