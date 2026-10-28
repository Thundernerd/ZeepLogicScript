using BepInEx.Logging;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.UI;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Logic Scripting window: a Script field, a Compile button and the result (a line, or the error list). Editor mouse input is blocked
/// while the cursor is over the window, editor keyboard input while the field has focus.
/// </summary>
internal sealed class ScriptWindow : IZeepGUIDrawer
{
    // F12 is Steam's screenshot key; F9 is the only F key the game itself leaves free
    private const KeyCode ToggleKey = KeyCode.F9;
    private const float FieldHeight = 240;

    private readonly ManualLogSource _logger;
    private readonly StartupCheck _startupCheck;

    private string _text = string.Empty;
    private string _result = string.Empty;
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

        if (gui.BeginWindow(
                "Logic Script",
                ref _open,
                ref _mouseOver,
                mouseEntered: () => SetMouseBlocked(true),
                mouseExited: () => SetMouseBlocked(false),
                size: new ImSize(480, 360)))
        {
            DrawStartupCheck(gui);
            DrawField(gui);

            if (gui.Button("Compile"))
                _result = ScriptCompile.Run(_text, _startupCheck, _logger);

            gui.Text(_result, wrap: true);
        }

        gui.EndWindow();
    }

    /// <summary>
    /// Releases every input block; call when the mod unloads.
    /// </summary>
    public void ReleaseInput()
    {
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
