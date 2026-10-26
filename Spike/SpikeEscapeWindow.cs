using BepInEx.Logging;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.UI;
using ZeepSDK.Utilities;

namespace TNRD.ZeepLogicScript.Spike
{
    /// <summary>
    /// A tiny Imui window with a multi-line field, to check keyboard blocking and whether Escape while typing
    /// opens the editor's pause menu. With the guard on, the editor's own input lock is held while the field has
    /// focus and for a couple of frames after Escape, which LEV_ToolSwitch checks before opening the pause menu.
    /// </summary>
    internal class SpikeEscapeWindow : IZeepGUIDrawer
    {
        private const int ReleaseDelayFrames = 2;
        private const int ReportDelayFrames = 5;

        private readonly ManualLogSource _logger;

        private bool _open;
        private bool _guard = true;
        private string _text = "Type here, then press Escape.\nWASD and Ctrl+Z should not reach the editor while this has focus.";

        private bool _blocking;
        private bool _lockedByUs;
        private int _escapeFrame = -1;
        private int _reportFrame = -1;
        private bool _reportGuard;

        public SpikeEscapeWindow(ManualLogSource logger)
        {
            _logger = logger;
        }

        public void Toggle()
        {
            _open = !_open;
        }

        public void OnZeepGUI(ImGui gui)
        {
            if (!LevelEditorApi.IsInLevelEditor || LevelEditorApi.IsTestingLevel || !_open)
            {
                Release(true);
                return;
            }

            if (gui.BeginWindow("Logic Script spike: Escape", ref _open, new ImSize(460, 300)))
            {
                gui.Checkbox(ref _guard, "Escape guard (hold the editor input lock while typing)");

                uint fieldId = gui.GetNextControlId();
                ImRect rect = ImTextEdit.AddRect(gui, new ImSize(440, 180), true, out _);
                ref ImTextEditState state = ref gui.Storage.Get<ImTextEditState>(fieldId);
                gui.TextEdit(fieldId, ref _text, ref state, rect, true);

                bool active = gui.IsControlActive(fieldId);
                if (active && Input.GetKeyDown(KeyCode.Escape))
                {
                    gui.ResetActiveControl();
                    active = false;
                    _escapeFrame = Time.frameCount;
                    _reportFrame = Time.frameCount + ReportDelayFrames;
                    _reportGuard = _guard;
                }

                gui.Text(_blocking ? "Editor shortcuts: blocked" : "Editor shortcuts: active");

                if (active)
                    Hold();
                else
                    Release(false);
            }

            gui.EndWindow();

            if (_reportFrame >= 0 && Time.frameCount >= _reportFrame)
                Report();
        }

        private void Hold()
        {
            if (!_blocking)
            {
                LevelEditorApi.BlockKeyboardInput(this);
                _blocking = true;
            }

            if (_guard && !_lockedByUs)
            {
                LEV_LevelEditorCentral central = ComponentCache.Get<LEV_LevelEditorCentral>();
                if (central != null && !central.input.inputLocked)
                {
                    central.input.inputLocked = true;
                    _lockedByUs = true;
                }
            }
        }

        private void Release(bool immediately)
        {
            if (_blocking)
            {
                LevelEditorApi.UnblockKeyboardInput(this);
                _blocking = false;
            }

            if (!_lockedByUs)
                return;

            // Keep the lock for a couple of frames after Escape, so the editor can't see that same key press
            if (!immediately && _escapeFrame >= 0 && Time.frameCount < _escapeFrame + ReleaseDelayFrames)
                return;

            LEV_LevelEditorCentral central = ComponentCache.Get<LEV_LevelEditorCentral>();
            if (central != null)
                central.input.inputLocked = false;

            _lockedByUs = false;
        }

        private void Report()
        {
            _reportFrame = -1;
            LEV_LevelEditorCentral central = ComponentCache.Get<LEV_LevelEditorCentral>();
            if (central == null)
                return;

            bool pauseOpen = central.pause.gameObject.activeSelf;
            _logger.LogInfo($"[escape] Escape left the field with the guard {(_reportGuard ? "ON" : "OFF")}: pause menu {(pauseOpen ? "OPENED" : "stayed closed")}");
        }
    }
}
