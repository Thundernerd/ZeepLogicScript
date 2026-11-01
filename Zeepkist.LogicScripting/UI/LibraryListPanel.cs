using System;
using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Libraries on this machine, New, and per Library Rename and Delete.
/// </summary>
internal sealed class LibraryListPanel : ItemListPanel<Library>
{
    public const string Help = "On this machine, for Scripts in every level. Right-click a Library to rename or delete it";

    private readonly LibraryList _libraries;
    private readonly Action<Library> _deleted;

    /// <summary>
    /// <paramref name="deleted"/> is called with a Library after Delete dropped it from <paramref name="libraries"/>.
    /// </summary>
    public LibraryListPanel(LibraryList libraries, Action<Library> deleted)
        : base("Library")
    {
        _libraries = libraries;
        _deleted = deleted;
    }

    protected override IReadOnlyList<Library> Entries => _libraries.Entries;
    protected override string EmptyText => "None yet on this machine. A Library is for Scripts in every level. New Library makes one, and right-clicking a Library renames or deletes it.";
    protected override string NewTooltip => Help;

    /// <summary>
    /// Returns the Library the player clicked or made, or null.
    /// </summary>
    public Library Draw(ImGui gui, Library current, bool readFailed)
    {
        if (readFailed)
            gui.Text("Couldn't read libraries.json, so changes to Libraries aren't saved. Fix or remove the file, then restart the game.", WindowColors.Error, wrap: true);

        var selected = DrawEntries(gui, current);
        return DrawActions(gui) ?? selected;
    }

    protected override string GetName(Library entry)
    {
        return entry.Name;
    }

    protected override string GetLabel(Library entry)
    {
        return entry.Name;
    }

    protected override string GetNameError(string name, Library renamed)
    {
        return _libraries.GetNameError(name, renamed?.Id);
    }

    protected override Library Add(string name)
    {
        return _libraries.Add(name);
    }

    protected override void Rename(Library entry, string name)
    {
        _libraries.Rename(entry, name);
    }

    protected override string GetDeleteQuestion(Library entry)
    {
        return $"Delete \"{entry.Name}\"? Scripts that call it can't be compiled on this machine any more, but their blocks in levels keep working.";
    }

    protected override void Delete(Library entry, DeleteChoice choice)
    {
        _libraries.Remove(entry.Id);
        Message = $"Deleted \"{entry.Name}\"";
        _deleted(entry);
    }
}
