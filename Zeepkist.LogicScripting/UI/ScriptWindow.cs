using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.UI;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Logic Scripting window: the Script editor, the errors from live checking, Insert selected and Compile buttons,
/// the result line and the Placed blocks panel. Editor mouse input is blocked while the cursor is over the window,
/// editor keyboard input while the field has focus.
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
    /// The most errors listed under the field; the underlines still show every one.
    /// </summary>
    private const int MaxListedErrors = 8;

    private static readonly Color32 _errorColor = new(230, 90, 90, 255);

    private readonly ManualLogSource _logger;
    private readonly StartupCheck _startupCheck;
    private readonly PlacedBlockHighlight _highlight = new();
    private readonly ScriptEditor _editor = new();

    // One Script per game session until Scripts are stored in the level; its Block Group is found by this id
    private readonly string _scriptId = Guid.NewGuid().ToString();

    private string _result = string.Empty;
    private string _insertMessage = string.Empty;
    private CompileResult _check;
    private string _checkedText;
    private float _nextCheckTime;
    private bool _open;
    private bool _mouseOver;
    private bool _fieldFocused;
    private bool _blockingMouse;
    private bool _blockingKeyboard;

    /// <summary>
    /// Whether the latest check is of the text as it is now, and found errors.
    /// </summary>
    private bool HasCurrentErrors => _check != null && _checkedText == _editor.Text && !_check.Succeeded;

    public ScriptWindow(ManualLogSource logger, StartupCheck startupCheck)
    {
        _logger = logger;
        _startupCheck = startupCheck;
    }

    public void OnZeepGUI(ImGui gui)
    {
        if (!LevelEditorApi.IsInLevelEditor || LevelEditorApi.IsTestingLevel)
        {
            ReleaseInput();
            return;
        }

        if (!_fieldFocused && Input.GetKeyDown(ToggleKey))
            _open = !_open;

        if (!_open)
        {
            ReleaseInput();
            return;
        }

        BlockProperties hovered = null;
        if (gui.BeginWindow(
                "Logic Script",
                ref _open,
                ref _mouseOver,
                mouseEntered: () => SetMouseBlocked(true),
                mouseExited: () => SetMouseBlocked(false),
                size: new ImSize(480, 560)))
        {
            DrawStartupCheck(gui);
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
            hovered = DrawPlacedBlocks(gui);
        }

        gui.EndWindow();
        _highlight.Set(ComponentCache.Get<LEV_LevelEditorCentral>(), _open ? hovered : null);
    }

    /// <summary>
    /// Releases every input block; call when the mod unloads.
    /// </summary>
    public void ReleaseInput()
    {
        _highlight.Set(ComponentCache.Get<LEV_LevelEditorCentral>(), null);
        _mouseOver = false;
        _fieldFocused = false;
        SetMouseBlocked(false);
        SetKeyboardBlocked(false);
    }

    /// <summary>
    /// A standing warning for what the startup check turned off.
    /// </summary>
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

        // Imui never sees clicks that land in the level, so the field would keep focus and keep taking keys
        if (!_mouseOver && gui.IsControlActive(_editor.Id) && IsAnyMouseButtonDown())
            gui.ResetActiveControl();

        if (_editor.Draw(gui, rect, out var compileRequested))
            OnEdited();

        _fieldFocused = gui.IsControlActive(_editor.Id);
        if (_fieldFocused && Input.GetKeyDown(KeyCode.Escape))
        {
            gui.ResetActiveControl();
            _fieldFocused = false;
        }

        SetKeyboardBlocked(_fieldFocused);
        return compileRequested;
    }

    /// <summary>
    /// The result line belongs to the text it was made for, and the check waits until typing stops.
    /// </summary>
    private void OnEdited()
    {
        _result = string.Empty;
        _insertMessage = string.Empty;
        _nextCheckTime = Time.unscaledTime + CheckDelay;
        _editor.SetErrors([]);
    }

    /// <summary>
    /// The errors of the latest check, once it's up to date with the text; clicking one jumps to it.
    /// </summary>
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

    /// <summary>
    /// Returns whether it was clicked; it's turned off, with the reason as its tooltip, while Compile can't run.
    /// </summary>
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
    /// Why Compile can't run right now, or null when it can: the checked Script has errors, or blocks are grabbed.
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

        _result = ScriptCompile.Run(_editor.Text, _scriptId, _startupCheck, _logger);

        // Placing changed the level, so the Placed blocks panel checks again now
        _nextCheckTime = 0;
    }

    /// <summary>
    /// Writes a Placed Block Reference for each selected block at the field's caret.
    /// </summary>
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
    /// One row per placed block the Script refers to; a missing one in red. Clicking a row frames its block.
    /// Returns the block under the hovered row, if any.
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

    /// <summary>
    /// Compiles the Script without placing anything, for live checking and the Placed blocks panel: shortly after
    /// typing stops, and every <see cref="CheckInterval"/> seconds while it's unchanged.
    /// </summary>
    private void RefreshCheck()
    {
        if (Time.unscaledTime < _nextCheckTime)
            return;

        var text = _editor.Text;
        _check = ScriptCompile.Check(text, _scriptId, _startupCheck);
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
