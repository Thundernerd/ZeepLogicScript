using System;
using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The name field for a new Script or Library, or for renaming one.
/// </summary>
internal sealed class NamePrompt
{
    private readonly string _noun;

    // The current name of what's being renamed, or null when naming a new one
    private string _renamed;
    private string _input = string.Empty;
    private string _error = string.Empty;
    private bool _open;
    private bool _focusRequested;

    /// <summary>
    /// <paramref name="noun"/> is what's named, such as "Script".
    /// </summary>
    public NamePrompt(string noun)
    {
        _noun = noun;
    }

    public bool Focused { get; set; }

    public bool IsOpen => _open;

    /// <summary>
    /// Whether it's open to rename a Script or Library, rather than to name a new one.
    /// </summary>
    public bool IsRenaming => _open && _renamed != null;

    public void OpenNew()
    {
        Open(null, string.Empty);
    }

    public void OpenRename(string name)
    {
        Open(name, name);
    }

    public void Close()
    {
        _open = false;
        Focused = false;
        _renamed = null;
    }

    /// <summary>
    /// Replaces the text in the field, as typing it would.
    /// </summary>
    public void SetName(string name)
    {
        _input = name;
        _error = string.Empty;
    }

    /// <summary>
    /// Returns the trimmed name once the player confirms one that <paramref name="getError"/> finds no error in, or
    /// null. <paramref name="pressed"/> is a button pressed for the player, which the prompt handles as clicked.
    /// </summary>
    public string Draw(ImGui gui, Func<string, string> getError, PromptButton? pressed)
    {
        if (!_open)
        {
            Focused = false;
            return null;
        }

        gui.Text(_renamed == null ? $"Name of the new {_noun}" : $"New name for \"{_renamed}\"", wrap: true);
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
        if (gui.Button("OK") || pressed == PromptButton.Ok)
            confirmed = true;

        if (gui.Button("Cancel") || pressed == PromptButton.Cancel || (Focused && Input.GetKeyDown(KeyCode.Escape)))
        {
            Close();
            return null;
        }

        if (_error.Length > 0)
            gui.Text(_error, wrap: true);

        if (!confirmed)
            return null;

        var name = _input.Trim();
        _error = getError(name) ?? string.Empty;
        return _error.Length == 0 ? name : null;
    }

    private void Open(string renamed, string name)
    {
        _renamed = renamed;
        _input = name;
        _error = string.Empty;
        _open = true;
        _focusRequested = true;
    }
}
