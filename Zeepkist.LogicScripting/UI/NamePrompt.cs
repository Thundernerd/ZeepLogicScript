using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The name field for a new Script, or for renaming one.
/// </summary>
internal sealed class NamePrompt
{
    private string _input = string.Empty;
    private string _error = string.Empty;
    private bool _open;
    private bool _focusRequested;

    /// <summary>
    /// The Script being renamed, or null when naming a new one.
    /// </summary>
    public ScriptEntry Renamed { get; private set; }

    public bool Focused { get; set; }

    public void OpenNew()
    {
        Open(null, string.Empty);
    }

    public void OpenRename(ScriptEntry entry)
    {
        Open(entry, entry.Name);
    }

    public void Close()
    {
        _open = false;
        Focused = false;
        Renamed = null;
    }

    /// <summary>
    /// Returns the trimmed name once the player confirms one that <paramref name="scripts"/> doesn't use yet, or null.
    /// </summary>
    public string Draw(ImGui gui, ScriptList scripts)
    {
        if (!_open)
        {
            Focused = false;
            return null;
        }

        gui.Text(Renamed == null ? "Name of the new Script" : $"New name for \"{Renamed.Name}\"", wrap: true);
        gui.AddSpacingIfLayoutFrameNotEmpty();
        var rect = gui.AddSingleRowRect(default);
        var id = gui.GetNextControlId();
        ref var state = ref gui.Storage.Get<ImTextEditState>(id);
        if (_focusRequested)
        {
            gui.SetActiveControl(id);
            _focusRequested = false;
        }

        if (gui.TextEdit(id, ref _input, ref state, rect, false))
            _error = string.Empty;

        Focused = gui.IsControlActive(id);
        var confirmed = Focused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter));
        if (gui.Button("OK"))
            confirmed = true;

        if (gui.Button("Cancel") || (Focused && Input.GetKeyDown(KeyCode.Escape)))
        {
            Close();
            return null;
        }

        if (_error.Length > 0)
            gui.Text(_error, wrap: true);

        if (!confirmed)
            return null;

        var name = _input.Trim();
        _error = scripts.GetNameError(name, Renamed?.Id) ?? string.Empty;
        return _error.Length == 0 ? name : null;
    }

    private void Open(ScriptEntry renamed, string name)
    {
        Renamed = renamed;
        _input = name;
        _error = string.Empty;
        _open = true;
        _focusRequested = true;
    }
}
