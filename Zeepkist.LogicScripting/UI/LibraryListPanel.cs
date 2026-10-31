using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Libraries on this machine, New, and per Library Rename and Delete.
/// </summary>
internal sealed class LibraryListPanel
{
    public const string Help = "On this machine, for Scripts in every level. Right-click a Library to rename or delete it";

    private readonly LibraryList _libraries;
    private readonly NamePrompt _namePrompt = new("Library");
    private Library _renamed;
    private Library _deleted;
    private string _message = string.Empty;

    public LibraryListPanel(LibraryList libraries)
    {
        _libraries = libraries;
    }

    public bool NameFocused => _namePrompt.Focused;

    public void ReleaseFocus()
    {
        _namePrompt.Focused = false;
    }

    /// <summary>
    /// Returns the Library the player clicked or made, or null. <paramref name="deleted"/> is the Library Delete just
    /// dropped, or null.
    /// </summary>
    public Library Draw(ImGui gui, Library current, bool readFailed, out Library deleted)
    {
        deleted = null;
        if (readFailed)
            gui.Text("Couldn't read libraries.json, so changes to Libraries aren't saved. Fix or remove the file, then restart the game.", WindowColors.Error, wrap: true);

        Library selected = null;
        foreach (var library in _libraries.Entries)
        {
            gui.AddSpacingIfLayoutFrameNotEmpty();
            var rect = gui.AddSingleRowRect(default);
            ref readonly var style = ref library == current ? ref gui.Style.List.ItemSelected : ref gui.Style.List.ItemNormal;
            if (gui.Button(gui.GetNextControlId(), library.Name, rect, in style, out _) && library != current)
                selected = library;

            if (gui.BeginContextMenu(rect))
            {
                if (gui.Menu("Rename"))
                {
                    _deleted = null;
                    _renamed = library;
                    _namePrompt.OpenRename(library.Name);
                }

                if (gui.Menu("Delete"))
                {
                    _namePrompt.Close();
                    _deleted = library;
                }

                gui.EndContextMenu();
            }
        }

        if (_libraries.Entries.Count == 0)
            gui.Text("None yet on this machine. A Library is for Scripts in every level. New Library makes one, and right-clicking a Library renames or deletes it.", WindowColors.Muted, wrap: true);

        if (gui.Button("New Library"))
        {
            _deleted = null;
            _renamed = null;
            _namePrompt.OpenNew();
        }

        gui.TooltipAtLastControl(Help);

        var named = _namePrompt.Draw(gui, name => _libraries.GetNameError(name, _renamed?.Id));
        if (named != null)
        {
            _message = string.Empty;
            if (_renamed == null)
                selected = _libraries.Add(named);
            else
                _libraries.Rename(_renamed, named);

            _namePrompt.Close();
        }

        if (_deleted != null)
        {
            gui.Text($"Delete \"{_deleted.Name}\"? Scripts that call it can't be compiled on this machine any more, but their blocks in levels keep working.", wrap: true);
            if (gui.Button("Delete"))
            {
                _libraries.Remove(_deleted.Id);
                _message = $"Deleted \"{_deleted.Name}\"";
                deleted = _deleted;
                _deleted = null;
            }

            if (gui.Button("Cancel"))
                _deleted = null;
        }

        if (_message.Length > 0)
            gui.Text(_message, wrap: true);

        return selected;
    }
}
