using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using TNRD.Zeepkist.LogicScripting.Storage;
using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.UI;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Editor mouse input is blocked while the cursor is over the window, editor keyboard input while the field has focus.
/// </summary>
internal sealed class ScriptWindow : IZeepGUIDrawer
{
    // F12 is Steam's screenshot key; F9 is the only F key the game itself leaves free
    private const KeyCode ToggleKey = KeyCode.F9;
    private const float FieldHeight = 240;

    /// <summary>
    /// Seconds between checks of an unchanged Script, in case blocks were added or deleted.
    /// </summary>
    private const float CheckInterval = 1;

    /// <summary>
    /// Seconds after the last edit before the Script is checked, so it isn't checked on every key while typing.
    /// </summary>
    private const float CheckDelay = 0.4f;

    /// <summary>
    /// Seconds after the last change before the Drafts are saved, so they aren't written on every key while typing.
    /// </summary>
    private const float DraftDelay = 1;

    /// <summary>
    /// The underlines still show every error.
    /// </summary>
    private const int MaxListedErrors = 8;

    private static readonly Color32 _errorColor = new(230, 90, 90, 255);

    private readonly ManualLogSource _logger;
    private readonly StartupCheck _startupCheck;
    private readonly PlacedBlockHighlight _highlight = new();
    private readonly ScriptList _scripts = new();
    private readonly DraftStore _drafts;
    private readonly DraftChanges _draftChanges = new();

    // One each, so every Script keeps its own caret and undo
    private readonly Dictionary<ScriptId, ScriptEditor> _editors = [];
    private readonly NamePrompt _namePrompt = new();
    private readonly DeletePrompt _deletePrompt = new();
    private readonly OrphanedGroupsPanel _orphanedGroupsPanel;

    private ScriptEntry _current;
    private ScriptEditor _editor;
    private int? _readLevelChanges;
    private string _listMessage = string.Empty;
    private string _result = string.Empty;
    private string _insertMessage = string.Empty;
    private CompileResult _check;
    private string _checkedText;
    private float _nextCheckTime;
    private float? _draftSaveTime;
    private bool _open;
    private bool _mouseOver;
    private bool _fieldFocused;
    private bool _blockingMouse;
    private bool _blockingKeyboard;

    private bool HasCurrentErrors => _check != null && _editor != null && _checkedText == _editor.Text && !_check.Succeeded;

    public ScriptWindow(ManualLogSource logger, StartupCheck startupCheck, DraftStore drafts)
    {
        _logger = logger;
        _startupCheck = startupCheck;
        _drafts = drafts;
        _orphanedGroupsPanel = new OrphanedGroupsPanel(logger);
    }

    public void OnZeepGUI(ImGui gui)
    {
        // Outside the editor nothing changes the Scripts any more, so waiting longer could only lose the last edits
        if (_draftSaveTime != null && (Time.unscaledTime >= _draftSaveTime || !LevelEditorApi.IsInLevelEditor))
            SaveDrafts();

        if (!LevelEditorApi.IsInLevelEditor || LevelEditorApi.IsTestingLevel)
        {
            ReleaseInput();
            return;
        }

        if (!_fieldFocused && !_namePrompt.Focused && Input.GetKeyDown(ToggleKey))
            _open = !_open;

        if (!_open)
        {
            ReleaseInput();
            return;
        }

        // Imui never sees clicks that land in the level, so a field would keep focus and keep taking keys
        if (!_mouseOver && (_fieldFocused || _namePrompt.Focused) && IsAnyMouseButtonDown())
            gui.ResetActiveControl();

        BlockProperties hovered = null;
        if (gui.BeginWindow(
                "Logic Script",
                ref _open,
                ref _mouseOver,
                mouseEntered: () => SetMouseBlocked(true),
                mouseExited: () => SetMouseBlocked(false),
                size: new ImSize(480, 680)))
        {
            DrawStartupCheck(gui);
            RefreshScripts();
            DrawScriptList(gui);
            if (_current != null)
                hovered = DrawCurrentScript(gui);
        }

        gui.EndWindow();
        SetKeyboardBlocked(_fieldFocused || _namePrompt.Focused);
        _highlight.Set(ComponentCache.Get<LEV_LevelEditorCentral>(), _open ? hovered : null);
    }

    /// <summary>
    /// Also call when the mod unloads, or the level editor's input stays blocked.
    /// </summary>
    public void ReleaseInput()
    {
        _highlight.Set(ComponentCache.Get<LEV_LevelEditorCentral>(), null);
        _mouseOver = false;
        _fieldFocused = false;
        _namePrompt.Focused = false;
        SetMouseBlocked(false);
        SetKeyboardBlocked(false);
    }

    /// <summary>
    /// Writes the Draft of every Script that changed since its last one. Also call when the mod unloads.
    /// </summary>
    public void SaveDrafts()
    {
        _draftSaveTime = null;
        foreach (var draft in _draftChanges.TakeChanged(_scripts.Entries, _drafts.Find))
        {
            _drafts.Save(draft);
        }
    }

    /// <summary>
    /// Drops every Script, so the next level's come from that level, after saving their Drafts. Scripts the level
    /// doesn't hold are only kept in their Drafts.
    /// </summary>
    public void ForgetScripts()
    {
        SaveDrafts();
        _scripts.Clear();
        _editors.Clear();
        _namePrompt.Close();
        _deletePrompt.Close();
        _orphanedGroupsPanel.ClearMessage();
        _listMessage = string.Empty;
        _readLevelChanges = null;
        Select(null);
    }

    /// <summary>
    /// Reads the level's Scripts again after the level changed, but not while a level is still loading, when the old
    /// level's blocks are still there.
    /// </summary>
    private void RefreshScripts()
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null || central.saveload.isLoading || _readLevelChanges == LevelChanges.Count)
            return;

        var (stored, blockCounts) = LevelScripts.Find(central);
        _scripts.Update(stored, blockCounts, _drafts.Find);
        _readLevelChanges = LevelChanges.Count;

        // A Compile or undo changes the text a Draft is based on
        RequestDraftSave();
        if (_current == null && _scripts.Entries.Count > 0)
            Select(_scripts.Entries[0]);
    }

    private void DrawScriptList(ImGui gui)
    {
        gui.Text("Scripts");
        ScriptEntry clicked = null;
        foreach (var entry in _scripts.Entries)
        {
            gui.AddSpacingIfLayoutFrameNotEmpty();
            var rect = gui.AddSingleRowRect(default);
            ref readonly var style = ref entry == _current ? ref gui.Style.List.ItemSelected : ref gui.Style.List.ItemNormal;
            var (glyph, tooltip) = GetMarkText(entry.Mark);
            if (gui.Button(gui.GetNextControlId(), glyph == null ? entry.Name : $"{glyph} {entry.Name}", rect, in style, out _))
                clicked = entry;

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

        if (clicked != null && clicked != _current)
            Select(clicked);

        var named = _namePrompt.Draw(gui, _scripts);
        if (named != null)
        {
            _listMessage = string.Empty;
            if (_namePrompt.Renamed == null)
                Select(_scripts.Add(named));
            else
                _namePrompt.Renamed.Name = named;

            _namePrompt.Close();
            RequestDraftSave();
        }

        var deleteChoice = _deletePrompt.Draw(gui);
        if (deleteChoice != DeleteChoice.None)
        {
            DeleteScript(_deletePrompt.Deleted, deleteChoice);
            _deletePrompt.Close();
        }

        if (_listMessage.Length > 0)
            gui.Text(_listMessage, wrap: true);

        // Restored after drawing, which goes through the list that restoring changes
        var orphan = _orphanedGroupsPanel.Draw(gui, _scripts.OrphanedGroups);
        if (orphan != null)
        {
            var restored = _scripts.Restore(orphan);
            _orphanedGroupsPanel.ShowRestored(restored);
            Select(restored);
            RequestDraftSave();
        }
    }

    /// <summary>
    /// Removes the Script's blocks or untags them as one undo step, then drops the Script from the window. Its Draft
    /// stays, so undo brings the Script back with the text typed since its last Compile.
    /// </summary>
    private void DeleteScript(ScriptEntry entry, DeleteChoice choice)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
            return;

        // The undo step isn't registered while grabbing
        if (central.gizmos.isGrabbing)
        {
            _listMessage = "Let go of the grabbed blocks first";
            return;
        }

        int count;
        try
        {
            count = choice == DeleteChoice.KeepBlocks
                ? BlockGroupEdits.Untag(central, entry.Id)
                : BlockGroupEdits.Delete(central, entry.Id);
        }
        catch (Exception e)
        {
            _logger.LogError($"Deleting Script \"{entry.Name}\" failed: {e}");
            _listMessage = "Delete failed; level unchanged";
            return;
        }

        _scripts.Remove(entry.Id);
        _editors.Remove(entry.Id);
        if (entry == _current)
            Select(_scripts.Entries.Count > 0 ? _scripts.Entries[0] : null);

        if (count == 0)
            _listMessage = $"Deleted \"{entry.Name}\"";
        else if (choice == DeleteChoice.KeepBlocks)
            _listMessage = $"Deleted \"{entry.Name}\" and kept its {count} blocks as plain Logic Blocks";
        else
            _listMessage = $"Deleted \"{entry.Name}\" and its {count} blocks";
    }

    private void RequestDraftSave()
    {
        _draftSaveTime = Time.unscaledTime + DraftDelay;
    }

    /// <summary>
    /// The font has no ⚠ or ◌, so the carrier-missing and never-compiled marks use ! and ○.
    /// </summary>
    private static (string Glyph, string Tooltip) GetMarkText(ScriptMark mark)
    {
        return mark switch
        {
            ScriptMark.NotCompiled => ("●", "Not compiled: the level holds another name or text"),
            ScriptMark.CarrierMissing => ("!", "The level has this Script's blocks but not its text; Compile to store it again"),
            ScriptMark.NeverCompiled => ("○", "Never compiled: the level doesn't hold this Script"),
            _ => (null, null)
        };
    }

    private void SelectGroup(ScriptEntry entry)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
            return;

        // The selection's undo step isn't registered while grabbing
        if (central.gizmos.isGrabbing)
        {
            _listMessage = "Let go of the grabbed blocks first";
            return;
        }

        var group = BlockGroups.Find(central, entry.Id);
        if (group.Count == 0)
        {
            _listMessage = $"\"{entry.Name}\" has no blocks in the level";
            return;
        }

        BlockGroups.Select(central, group);
        _listMessage = $"Selected {group.Count} blocks of \"{entry.Name}\"";
    }

    /// <summary>
    /// The result line, errors and checks belong to the Script they were made for.
    /// </summary>
    private void Select(ScriptEntry entry)
    {
        _current = entry;
        _editor = entry == null ? null : GetEditor(entry);
        _fieldFocused = false;
        _result = string.Empty;
        _insertMessage = string.Empty;
        _check = null;
        _checkedText = null;
        _nextCheckTime = 0;
        _editor?.SetErrors([]);
    }

    private ScriptEditor GetEditor(ScriptEntry entry)
    {
        if (!_editors.TryGetValue(entry.Id, out var editor))
        {
            editor = new ScriptEditor(entry.Text);
            _editors.Add(entry.Id, editor);
        }

        return editor;
    }

    /// <summary>
    /// Returns the block under the hovered row of the Placed blocks panel, or null.
    /// </summary>
    private BlockProperties DrawCurrentScript(ImGui gui)
    {
        gui.Text(_current.Mark == ScriptMark.NotCompiled ? $"{_current.Name} ● not compiled" : _current.Name);
        DrawStorageWarning(gui);
        RefreshCheck();
        var compileRequested = DrawField(gui);
        DrawErrors(gui);

        if (gui.Button("Insert selected"))
            InsertSelected(gui);

        if (_insertMessage.Length > 0)
            gui.Text(_insertMessage, wrap: true);

        if (DrawCompileButton(gui))
            Compile();
        else if (compileRequested)
            CompileFromKeyboard();

        gui.Text(_result, wrap: true);
        return DrawPlacedBlocks(gui);
    }

    /// <summary>
    /// Stays up while the level is missing the Script's text: leaving the editor can't be cancelled to save it.
    /// </summary>
    private void DrawStorageWarning(ImGui gui)
    {
        if (_current.Mark == ScriptMark.CarrierMissing)
        {
            gui.Text("The level has this Script's blocks but not its text, likely because a block holding it was deleted. Compile to store it again.", _errorColor, wrap: true);
        }
        else if (_current.Mark == ScriptMark.NeverCompiled && _current.Text.Length > 0)
        {
            gui.Text("Not stored in the level yet: Compile to store it. Until then, loading a level or going back to the main menu loses it.", _errorColor, wrap: true);
        }
    }

    private void DrawStartupCheck(ImGui gui)
    {
        if (!_startupCheck.CanPlace)
            gui.Text($"{StartupCheck.UpdateNeededMessage}. Scripts are still checked, but Compile places nothing.", wrap: true);

        if (_startupCheck.DisabledBlocks.Count > 0)
            gui.Text($"Turned off for this game version: {string.Join(", ", _startupCheck.DisabledBlocks)}", wrap: true);
    }

    /// <summary>
    /// Returns whether Ctrl+Enter was pressed in the field.
    /// </summary>
    private bool DrawField(ImGui gui)
    {
        gui.AddSpacingIfLayoutFrameNotEmpty();
        var rect = gui.Layout.AddRect(gui.GetLayoutWidth(), FieldHeight);

        if (_editor.Draw(gui, rect, out var compileRequested))
            OnEdited();

        _fieldFocused = gui.IsControlActive(_editor.Id);
        if (_fieldFocused && Input.GetKeyDown(KeyCode.Escape))
        {
            gui.ResetActiveControl();
            _fieldFocused = false;
        }

        return compileRequested;
    }

    /// <summary>
    /// The result line belongs to the text it was made for, and the check waits until typing stops.
    /// </summary>
    private void OnEdited()
    {
        _current.Text = _editor.Text;
        RequestDraftSave();
        _result = string.Empty;
        _insertMessage = string.Empty;
        _nextCheckTime = Time.unscaledTime + CheckDelay;
        _editor.SetErrors([]);
    }

    private void DrawErrors(ImGui gui)
    {
        if (!HasCurrentErrors)
            return;

        var count = _check.Errors.Count;
        gui.Text(count == 1 ? "1 error" : $"{count} errors", _errorColor);
        foreach (var error in _check.Errors.Take(MaxListedErrors))
        {
            if (gui.Button(error.ToString()))
                _editor.JumpTo(error.Position);
        }

        if (count > MaxListedErrors)
            gui.Text($"and {count - MaxListedErrors} more", _errorColor);
    }

    private bool DrawCompileButton(ImGui gui)
    {
        var blocker = GetCompileBlocker();
        gui.BeginReadOnly(blocker != null);
        var clicked = gui.Button("Compile (Ctrl+Enter)");
        gui.EndReadOnly();

        if (blocker != null)
            gui.TooltipAtLastControl(blocker);

        return clicked && blocker == null;
    }

    /// <summary>
    /// Why Compile can't run right now, or null when it can.
    /// </summary>
    private string GetCompileBlocker()
    {
        if (HasCurrentErrors)
            return "Fix the errors first";

        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        return central != null && central.gizmos.isGrabbing ? "Let go of the grabbed blocks first" : null;
    }

    /// <summary>
    /// Ctrl+Enter has no button to show why Compile can't run, so the result line says it.
    /// </summary>
    private void CompileFromKeyboard()
    {
        var blocker = GetCompileBlocker();
        if (blocker == null)
            Compile();
        else
            _result = blocker;
    }

    /// <summary>
    /// Checks a Script edited since the last check first, so errors typed just now show in the list, not in the
    /// result line.
    /// </summary>
    private void Compile()
    {
        if (_checkedText != _editor.Text)
        {
            _nextCheckTime = 0;
            RefreshCheck();
            if (HasCurrentErrors)
                return;
        }

        _result = ScriptCompile.Run(_current.ToStored(), _startupCheck, _logger);

        // Placing changed the level, so the Placed blocks panel checks again now
        _nextCheckTime = 0;
    }

    private void InsertSelected(ImGui gui)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null || central.selection.list.Count == 0)
        {
            _insertMessage = "Select blocks in the level first";
            return;
        }

        List<SelectedBlock> selected = [];
        foreach (var block in central.selection.list)
        {
            selected.Add(new SelectedBlock(block.UID, block.blockID, BlockGroups.IsInAnyGroup(block)));
        }

        // Clicking the button took focus from the field, but the editor still holds the caret
        var insert = PlacedBlockReferenceInsert.Insert(_editor.Text, _editor.Caret, selected);
        if (insert.InsertedCount > 0)
        {
            _editor.Replace(insert.Text, insert.Caret, gui.Input.Time);
            OnEdited();
        }

        _insertMessage = $"Inserted {insert.InsertedCount} placed blocks";
        if (insert.AlreadyReferencedCount > 0)
            _insertMessage += $"; skipped {insert.AlreadyReferencedCount} the Script already refers to";

        if (insert.SkippedGroupBlockCount > 0)
            _insertMessage += $"; skipped {insert.SkippedGroupBlockCount} in a Block Group, which a recompile replaces";
    }

    /// <summary>
    /// Returns the block under the hovered row, or null.
    /// </summary>
    private BlockProperties DrawPlacedBlocks(ImGui gui)
    {
        gui.Text("Placed blocks");

        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null || _check == null || _check.PlacedBlockReferences.Count == 0)
        {
            gui.Text("None yet; select blocks in the level and press Insert selected", wrap: true);
            return null;
        }

        BlockProperties hovered = null;
        HashSet<string> shown = [];
        foreach (var reference in _check.PlacedBlockReferences)
        {
            if (!shown.Add(reference.Uid))
                continue;

            var name = reference.Name ?? "(unnamed)";
            var block = reference.Type == null ? null : LevelBlocks.Find(central, reference.Uid);
            if (block == null)
            {
                gui.Text($"{name}: no block with UID \"{reference.Uid}\"", _errorColor);
                continue;
            }

            gui.AddSpacingIfLayoutFrameNotEmpty();
            var id = gui.GetNextControlId();
            var rect = gui.AddSingleRowRect(default);
            if (gui.Button(id, $"{name}: {reference.Type.Function} \"{reference.Uid}\"", rect, out var state))
                PlacedBlockHighlight.Frame(central, block);

            if (state != ImButtonState.Normal)
                hovered = block;
        }

        return hovered;
    }

    private void RefreshCheck()
    {
        if (Time.unscaledTime < _nextCheckTime)
            return;

        var text = _editor.Text;
        _check = ScriptCompile.Check(text, _current.Id, _startupCheck);
        _checkedText = text;
        _nextCheckTime = Time.unscaledTime + CheckInterval;
        _editor.SetErrors(_check == null ? [] : ScriptText.GetErrorSpans(text, _check.Errors.Select(e => e.Position)));
    }

    private static bool IsAnyMouseButtonDown()
    {
        return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
    }

    private void SetMouseBlocked(bool blocked)
    {
        if (blocked == _blockingMouse)
            return;

        if (blocked)
            LevelEditorApi.BlockMouseInput(this);
        else
            LevelEditorApi.UnblockMouseInput(this);

        _blockingMouse = blocked;
    }

    private void SetKeyboardBlocked(bool blocked)
    {
        if (blocked == _blockingKeyboard)
            return;

        if (blocked)
            LevelEditorApi.BlockKeyboardInput(this);
        else
            LevelEditorApi.UnblockKeyboardInput(this);

        _blockingKeyboard = blocked;
    }
}
