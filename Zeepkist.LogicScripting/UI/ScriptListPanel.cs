using System;
using BepInEx.Logging;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using TNRD.Zeepkist.LogicScripting.Storage;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The window's Scripts with their marks, New, and per Script Rename, Select group and Delete.
/// </summary>
internal sealed class ScriptListPanel
{
    private readonly ManualLogSource _logger;
    private readonly ScriptList _scripts;
    private readonly DraftSaver _draftSaver;
    private readonly Action<ScriptEntry> _deleted;
    private readonly NamePrompt _namePrompt = new();
    private readonly DeletePrompt _deletePrompt = new();
    private string _message = string.Empty;

    /// <summary>
    /// <paramref name="deleted"/> is called with a Script after Delete dropped it from <paramref name="scripts"/>.
    /// </summary>
    public ScriptListPanel(ManualLogSource logger, ScriptList scripts, DraftSaver draftSaver, Action<ScriptEntry> deleted)
    {
        _logger = logger;
        _scripts = scripts;
        _draftSaver = draftSaver;
        _deleted = deleted;
    }

    public bool NameFocused => _namePrompt.Focused;

    public void ReleaseFocus()
    {
        _namePrompt.Focused = false;
    }

    public void Clear()
    {
        _namePrompt.Close();
        _deletePrompt.Close();
        _message = string.Empty;
    }

    /// <summary>
    /// The font has no ⚠ or ◌, so the carrier-missing and never-compiled marks use ! and ○.
    /// </summary>
    public static (string Glyph, string Tooltip) GetMarkText(ScriptMark mark)
    {
        return mark switch
        {
            ScriptMark.NotCompiled => ("●", "Not compiled: the level holds another name or text"),
            ScriptMark.CarrierMissing => ("!", "The level has this Script's blocks but not its text; Compile to store it again"),
            ScriptMark.NeverCompiled => ("○", "Never compiled: the level doesn't hold this Script"),
            _ => (null, null)
        };
    }

    /// <summary>
    /// Returns the Script the player clicked or made, or null.
    /// </summary>
    public ScriptEntry Draw(ImGui gui, ScriptEntry current)
    {
        ScriptEntry selected = null;
        foreach (var entry in _scripts.Entries)
        {
            gui.AddSpacingIfLayoutFrameNotEmpty();
            var rect = gui.AddSingleRowRect(default);
            ref readonly var style = ref entry == current ? ref gui.Style.List.ItemSelected : ref gui.Style.List.ItemNormal;
            var (glyph, tooltip) = GetMarkText(entry.Mark);
            if (gui.Button(gui.GetNextControlId(), glyph == null ? entry.Name : $"{glyph} {entry.Name}", rect, in style, out _) && entry != current)
                selected = entry;

            if (tooltip != null)
                gui.TooltipAtLastControl(tooltip);

            if (gui.BeginContextMenu(rect))
            {
                if (gui.Menu("Rename"))
                    _namePrompt.OpenRename(entry);

                if (gui.Menu("Select group"))
                    SelectGroup(entry);

                if (gui.Menu("Delete"))
                {
                    _namePrompt.Close();
                    _deletePrompt.Open(entry);
                }

                gui.EndContextMenu();
            }
        }

        if (_scripts.Entries.Count == 0)
            gui.Text("None yet in this level", wrap: true);

        if (gui.Button("New Script"))
        {
            _deletePrompt.Close();
            _namePrompt.OpenNew();
        }

        var named = _namePrompt.Draw(gui, _scripts);
        if (named != null)
        {
            _message = string.Empty;
            if (_namePrompt.Renamed == null)
                selected = _scripts.Add(named);
            else
                _namePrompt.Renamed.Name = named;

            _namePrompt.Close();
            _draftSaver.Request();
        }

        var deleteChoice = _deletePrompt.Draw(gui);
        if (deleteChoice != DeleteChoice.None)
        {
            DeleteScript(_deletePrompt.Deleted, deleteChoice);
            _deletePrompt.Close();
        }

        if (_message.Length > 0)
            gui.Text(_message, wrap: true);

        return selected;
    }

    /// <summary>
    /// Removes the Script's blocks or untags them as one undo step, then drops the Script from the window. Its Draft
    /// stays, so undo brings the Script back with the text typed since its last Compile.
    /// </summary>
    private void DeleteScript(ScriptEntry entry, DeleteChoice choice)
    {
        var central = LevelEdit.GetCentral(out var blocker);
        if (central == null)
        {
            _message = blocker;
            return;
        }

        var edited = LevelEdit.TryEditGroup(
            _logger,
            $"Deleting Script \"{entry.Name}\"",
            () => choice == DeleteChoice.KeepBlocks
                ? BlockGroupEdits.Untag(central, entry.Id)
                : BlockGroupEdits.Delete(central, entry.Id));

        if (edited is not { } count)
        {
            _message = "Delete failed; level unchanged";
            return;
        }

        _scripts.Remove(entry.Id);
        _deleted(entry);

        if (count == 0)
            _message = $"Deleted \"{entry.Name}\"";
        else if (choice == DeleteChoice.KeepBlocks)
            _message = $"Deleted \"{entry.Name}\" and kept its {count} blocks as plain Logic Blocks";
        else
            _message = $"Deleted \"{entry.Name}\" and its {count} blocks";
    }

    private void SelectGroup(ScriptEntry entry)
    {
        var central = LevelEdit.GetCentral(out var blocker);
        if (central == null)
        {
            _message = blocker;
            return;
        }

        var group = BlockGroups.Find(central, entry.Id);
        if (group.Count == 0)
        {
            _message = $"\"{entry.Name}\" has no blocks in the level";
            return;
        }

        BlockGroups.Select(central, group);
        _message = $"Selected {group.Count} blocks of \"{entry.Name}\"";
    }
}
