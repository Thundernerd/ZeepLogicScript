using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// Every Library on the player's machine, which every Script in every level can call (ADR 0009).
/// </summary>
public sealed class LibraryList
{
    private readonly List<Library> _entries = [];
    private Dictionary<string, IReadOnlyCollection<string>> _functionNames;

    public LibraryList(IEnumerable<Library> libraries = null)
    {
        _entries.AddRange(libraries ?? []);
    }

    public IReadOnlyList<Library> Entries => _entries;

    /// <summary>
    /// Goes up with every change, so what's made from the Libraries, such as the editor's colors, knows to make it
    /// again.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// The Function names of each Library by its name, for <see cref="ScriptHighlighter.Highlight"/>.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> FunctionNames =>
        _functionNames ??= _entries
            .GroupBy(l => l.Name)
            .ToDictionary(g => g.Key, g => ScriptHighlighter.GetFunctionNames(g.First().Text));

    /// <summary>
    /// Returns why <paramref name="name"/> can't name a new Library, or the Library with <paramref name="renamedId"/>,
    /// or null when it can. Trim it first.
    /// </summary>
    public string GetNameError(string name, LibraryId renamedId = null)
    {
        return LibraryName.GetError(name, _entries.Where(l => l.Id != renamedId).Select(l => l.Name));
    }

    /// <summary>
    /// Adds an empty Library with a new id. Check the name with <see cref="GetNameError"/> first.
    /// </summary>
    public Library Add(string name)
    {
        var library = new Library(LibraryId.New(), name, string.Empty);
        _entries.Add(library);
        OnChanged();
        return library;
    }

    /// <summary>
    /// Check the name with <see cref="GetNameError"/> first.
    /// </summary>
    public void Rename(Library library, string name)
    {
        if (library.Name == name)
            return;

        library.Name = name;
        OnChanged();
    }

    public void SetText(Library library, string text)
    {
        text ??= string.Empty;
        if (library.Text == text)
            return;

        library.Text = text;
        OnChanged();
    }

    public void Remove(LibraryId id)
    {
        if (_entries.RemoveAll(l => l.Id == id) > 0)
            OnChanged();
    }

    /// <summary>
    /// The text of the Library a Script calls by <paramref name="name"/>, or null when there's none. Names in a Script
    /// are case-sensitive, so this one is too.
    /// </summary>
    public string FindText(string name)
    {
        return _entries.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal))?.Text;
    }

    private void OnChanged()
    {
        Version++;
        _functionNames = null;
    }
}
