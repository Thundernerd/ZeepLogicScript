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

    private const string ReadFailedText = "Couldn't read libraries.json, so changes to Libraries aren't saved. Fix or remove the file, then restart the game.";

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
    /// Returns the Library the player clicked or made, or null. With <paramref name="readFailed"/>, a warning is pinned
    /// above the list.
    /// </summary>
    public Library Draw(ImGui gui, ImRect rect, Library current, bool readFailed)
    {
        if (readFailed)
            rect = DrawReadFailed(gui, rect);

        BeginList(gui, rect);
        var selected = DrawEntries(gui, current);
        return EndList(gui) ?? selected;
    }

    /// <summary>
    /// The height the tab needs for <see cref="ItemListPanel{T}.GetMinHeight"/>, and a row of the warning with
    /// <paramref name="readFailed"/>.
    /// </summary>
    public static float GetMinHeight(ImGui gui, bool readFailed)
    {
        return GetMinHeight(gui) + (readFailed ? GetMinWarningHeight(gui) : 0);
    }

    private static float GetMinWarningHeight(ImGui gui)
    {
        return gui.GetRowHeight() + (gui.Style.Layout.Spacing * 2);
    }

    /// <summary>
    /// Returns the rest of <paramref name="rect"/>, under the warning. In a short tab the warning scrolls, so the list
    /// keeps its two rows and New.
    /// </summary>
    private static ImRect DrawReadFailed(ImGui gui, ImRect rect)
    {
        var spacing = gui.Style.Layout.Spacing;
        var settings = WrappedText.GetSettings(gui);
        var wanted = WrappedText.GetHeight(gui, ReadFailedText, in settings, rect.W - (spacing * 2), minHeight: 0) + (spacing * 2);
        var height = FooterSplit.GetWarningHeight(rect.H, wanted, GetMinHeight(gui), GetMinWarningHeight(gui));
        var warning = rect.TakeTop(height, out var rest);

        // A scope of its own, so the warning's scroll area doesn't share an id with the list's
        gui.PushId("read failed");
        WindowRegion.Begin(gui, warning);
        gui.Text(ReadFailedText, in settings, WindowColors.Error);
        WindowRegion.End(gui);
        gui.PopId();
        return rest;
    }

    protected override string GetName(Library entry)
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
