using System.Collections.Generic;
using BepInEx.Logging;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.UI;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Logic Scripting window: a Script field, Insert selected and Compile buttons, the result (a line, or the error
/// list) and the Placed blocks panel. Editor mouse input is blocked while the cursor is over the window, editor
/// keyboard input while the field has focus.
/// </summary>
internal sealed class ScriptWindow : IZeepGUIDrawer
{
    // F12 is Steam's screenshot key; F9 is the only F key the game itself leaves free
    private const KeyCode ToggleKey = KeyCode.F9;
    private const float FieldHeight = 240;

    /// <summary>
    /// Seconds between checks of an unchanged Script for the Placed blocks panel, in case blocks were added or deleted.
    /// </summary>
    private const float CheckInterval = 1;

    private static readonly Color32 _missingColor = new(230, 90, 90, 255);

    private readonly ManualLogSource _logger;
    private readonly StartupCheck _startupCheck;
    private readonly PlacedBlockHighlight _highlight = new();

    private string _text = string.Empty;
    private string _result = string.Empty;
    private string _insertMessage = string.Empty;
    private CompileResult _check;
    private string _checkedText;
    private float _nextCheckTime;
    private uint _fieldId;
    private bool _open;
    private bool _mouseOver;
    private bool _fieldFocused;
    private bool _blockingMouse;
    private bool _blockingKeyboard;

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
            DrawField(gui);

            if (gui.Button("Insert selected"))
                InsertSelected(gui);

            if (_insertMessage.Length > 0)
                gui.Text(_insertMessage, wrap: true);

            if (gui.Button("Compile"))
            {
                _result = ScriptCompile.Run(_text, _startupCheck, _logger);
                _checkedText = null;
            }

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

    private void DrawField(ImGui gui)
    {
        var fieldId = gui.GetNextControlId();
        _fieldId = fieldId;
        var rect = ImTextEdit.AddRect(gui, new ImSize(gui.GetLayoutWidth(), FieldHeight), true, out _);
        ref var state = ref gui.Storage.Get<ImTextEditState>(fieldId);

        // Imui never sees clicks that land in the level, so the field would keep focus and keep taking keys
        if (!_mouseOver && gui.IsControlActive(fieldId) && IsAnyMouseButtonDown())
            gui.ResetActiveControl();

        gui.TextEdit(fieldId, ref _text, ref state, rect, true);

        _fieldFocused = gui.IsControlActive(fieldId);
        if (_fieldFocused && Input.GetKeyDown(KeyCode.Escape))
        {
            gui.ResetActiveControl();
            _fieldFocused = false;
        }

        SetKeyboardBlocked(_fieldFocused);
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
            selected.Add(new SelectedBlock(block.UID, block.blockID));
        }

        // Clicking the button took focus from the field, but its state still holds the caret
        ref var state = ref gui.Storage.Get<ImTextEditState>(_fieldId);
        var insert = PlacedBlockReferenceInsert.Insert(_text, state.Caret, selected);
        _text = insert.Text;
        state.Caret = insert.Caret;
        state.Selection = 0;

        _insertMessage = insert.SkippedCount == 0
            ? $"Inserted {insert.InsertedCount} placed blocks"
            : $"Inserted {insert.InsertedCount} placed blocks; skipped {insert.SkippedCount} the Script already refers to";
    }

    /// <summary>
    /// One row per placed block the Script refers to; a missing one in red. Clicking a row frames its block.
    /// Returns the block under the hovered row, if any.
    /// </summary>
    private BlockProperties DrawPlacedBlocks(ImGui gui)
    {
        RefreshCheck();
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
                gui.Text($"{name}: no block with UID \"{reference.Uid}\"", _missingColor);
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
    /// Compiles the Script without placing anything when it changed, and every <see cref="CheckInterval"/> seconds.
    /// </summary>
    private void RefreshCheck()
    {
        if (_text == _checkedText && Time.unscaledTime < _nextCheckTime)
            return;

        _check = ScriptCompile.Check(_text, _startupCheck);
        _checkedText = _text;
        _nextCheckTime = Time.unscaledTime + CheckInterval;
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
