using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using TNRD.Zeepkist.LogicScripting.Storage;
using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;
using ZeepSDK.UI;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Editor mouse input is blocked while the cursor is over the window, editor keyboard input while one of its fields
/// has focus.
/// </summary>
internal sealed partial class ScriptWindow : IZeepGUIDrawer
{
    private const string LogFile = "BepInEx/LogOutput.log";

    private readonly ManualLogSource _logger;
    private readonly StartupCheck _startupCheck;
    private readonly ConfigEntry<KeyCode> _toggleKey;
    private readonly EditorAssist _editorAssist;
    private readonly PlacedBlockHighlight _highlight = new();
    private readonly ScriptList _scripts = new();
    private readonly DraftStore _drafts;
    private readonly DraftSaver _draftSaver;
    private readonly LibraryStore _libraryStore;
    private readonly LibrarySaver _librarySaver;
    private readonly WindowStateStore _stateStore;
    private readonly WindowState _state;

    // One each, so every Script keeps its own caret and undo
    private readonly Dictionary<ScriptId, ScriptEditor> _editors = [];
    private readonly Dictionary<LibraryId, ScriptEditor> _libraryEditors = [];
    private readonly ScriptListPanel _scriptListPanel;
    private readonly LibraryListPanel _libraryListPanel;
    private readonly OrphanedGroupsPanel _orphanedGroupsPanel;
    private readonly PlacedBlocksPanel _placedBlocksPanel = new();
    private readonly ReferencePanel _referencePanel;

    private EditedItem _edited;
    private int? _readLevelChanges;
    private string _result = string.Empty;
    private bool _mouseOver;
    private bool _fieldFocused;
    private bool _blockingMouse;
    private bool _blockingKeyboard;
    private bool _failed;

    private bool AnyFieldFocused => _fieldFocused || _scriptListPanel.NameFocused || _libraryListPanel.NameFocused || _referencePanel.FilterFocused;
    private LibraryList Libraries => _libraryStore.Libraries;
    private ScriptEntry CurrentScript => (_edited as EditedScript)?.Entry;
    private Library CurrentLibrary => (_edited as EditedLibrary)?.Library;

    public ScriptWindow(
        ManualLogSource logger,
        StartupCheck startupCheck,
        DraftStore drafts,
        LibraryStore libraries,
        WindowStateStore stateStore,
        ConfigEntry<KeyCode> toggleKey,
        EditorAssist editorAssist)
    {
        _logger = logger;
        _startupCheck = startupCheck;
        _drafts = drafts;
        _stateStore = stateStore;
        _state = stateStore.Load();
        _savedState = _state.Copy();
        _toggleKey = toggleKey;
        _editorAssist = editorAssist;
        _draftSaver = new DraftSaver(drafts, _scripts);
        _libraryStore = libraries;
        _librarySaver = new LibrarySaver(libraries);
        _scriptListPanel = new ScriptListPanel(logger, _scripts, _draftSaver, OnScriptDeleted);
        _libraryListPanel = new LibraryListPanel(libraries.Libraries, OnLibraryDeleted);
        _referencePanel = new ReferencePanel(libraries.Libraries);
        _orphanedGroupsPanel = new OrphanedGroupsPanel(logger);
    }

    public bool IsOpen => _state.Open;

    /// <summary>
    /// ZeepSDK draws every drawer in one loop without catching, so an exception here would stop the drawers after this
    /// one, and come back every frame with the editor's input still blocked. Taking the checkpoint is guarded too: it
    /// reads Imui's private fields, which a game update can rename.
    /// </summary>
    public void OnZeepGUI(ImGui gui)
    {
        GuiCheckpoint? checkpoint = null;
        try
        {
            checkpoint = GuiCheckpoint.Take(gui);
            DrawFrame(gui);
        }
        catch (Exception e)
        {
            OnFailed(gui, e);
            checkpoint?.Restore(gui);
        }
    }

    private void DrawFrame(ImGui gui)
    {
        // Outside the editor nothing changes the Scripts any more, so waiting longer could only lose the last edits
        if (LevelEditorApi.IsInLevelEditor)
        {
            _draftSaver.Update();
            _librarySaver.Update();
        }
        else
        {
            _draftSaver.SavePending();
            _librarySaver.Save();
        }

        SaveStateWhenSettled();

        // Hidden, not closed: the window comes back where it was after test mode
        if (!LevelEditorApi.IsInLevelEditor || LevelEditorApi.IsTestingLevel)
        {
            ReleaseInput();
            return;
        }

        var toggleKey = _toggleKey.Value;
        if (toggleKey != KeyCode.None && !AnyFieldFocused && Input.GetKeyDown(toggleKey))
            SetOpen(!_state.Open);

        if (!_state.Open)
        {
            ReleaseInput();
            return;
        }

        // Imui never sees clicks that land in the level, so a field would keep focus and keep taking keys
        if (!_mouseOver && AnyFieldFocused && IsAnyMouseButtonDown())
            gui.ResetActiveControl();

        var hovered = DrawWindow(gui);
        SetKeyboardBlocked(AnyFieldFocused);
        _highlight.Set(ComponentCache.Get<LEV_LevelEditorCentral>(), _state.Open ? hovered : null);
    }

    /// <summary>
    /// Logs only the first failure until the window is opened again, as most come back every frame. The highlight is
    /// left for the next frame, which removes it with the window closed: it's game code, and may be what failed.
    /// </summary>
    private void OnFailed(ImGui gui, Exception e)
    {
        var wasOpen = _state.Open;
        if (!_failed)
        {
            _failed = true;
            _logger.LogError($"The Script window failed: {e}");
            MessengerApi.LogError(wasOpen
                ? $"The Logic Script window ran into an error and closed. Details are in {LogFile}"
                : $"Logic Scripting ran into an error. Details are in {LogFile}");
        }

        // A field that was active when drawing stopped would otherwise take focus, and the keyboard, on reopening
        if (wasOpen)
            gui.ResetActiveControl();

        _state.Open = false;
        UnblockEditor();
    }

    /// <summary>
    /// Also call when the mod unloads, or the level editor's input stays blocked.
    /// </summary>
    public void ReleaseInput()
    {
        UnblockEditor();
        _highlight.Set(ComponentCache.Get<LEV_LevelEditorCentral>(), null);
    }

    private void UnblockEditor()
    {
        _mouseOver = false;
        _fieldFocused = false;
        ReleasePanelFocus();
        SetMouseBlocked(false);
        SetKeyboardBlocked(false);
    }

    private void ReleasePanelFocus()
    {
        _scriptListPanel.ReleaseFocus();
        _libraryListPanel.ReleaseFocus();
        _referencePanel.ReleaseFocus();
    }

    /// <summary>
    /// Writes the Draft of every Script that changed since its last one. Also call when the mod unloads.
    /// </summary>
    public void SaveDrafts()
    {
        _draftSaver.Save();
    }

    /// <summary>
    /// Writes the Libraries if they changed since they were last written. Also call when the mod unloads.
    /// </summary>
    public void SaveLibraries()
    {
        _librarySaver.Save();
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
        _scriptListPanel.Clear();
        _orphanedGroupsPanel.ClearMessage();
        _readLevelChanges = null;

        if (_edited?.BelongsToLevel ?? true)
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
        _draftSaver.Request();
        if (_edited == null && _scripts.Entries.Count > 0)
            Select(_scripts.Entries[0]);
    }

    private void DrawScriptsTab(ImGui gui)
    {
        // A scope each for the lists, so a Script or Orphaned Group coming or going doesn't change the id, and with it
        // the focus and text, of the name field drawn under them
        gui.PushId("scripts list");
        var selected = _scriptListPanel.DrawEntries(gui, CurrentScript);
        gui.PopId();

        // Restored after drawing, which goes through the list that restoring changes
        gui.PushId("orphaned groups");
        var collapsed = _state.OrphanedGroupsCollapsed;
        var orphan = _orphanedGroupsPanel.Draw(gui, _scripts.OrphanedGroups, ref collapsed);
        _state.OrphanedGroupsCollapsed = collapsed;
        gui.PopId();
        if (orphan != null)
        {
            var restored = _scripts.Restore(orphan);
            _orphanedGroupsPanel.ShowRestored(restored);
            selected = restored;
            _draftSaver.Request();
        }

        selected = _scriptListPanel.DrawActions(gui) ?? selected;
        if (selected != null)
            Select(selected);
    }

    private void DrawLibrariesTab(ImGui gui)
    {
        var library = _libraryListPanel.Draw(gui, CurrentLibrary, _libraryStore.ReadFailed);
        if (library != null)
            SelectLibrary(library);
    }

    private void OnLibraryDeleted(Library library)
    {
        _libraryEditors.Remove(library.Id);
        if (library == CurrentLibrary)
            Select(_scripts.Entries.Count > 0 ? _scripts.Entries[0] : null);
    }

    private void OnScriptDeleted(ScriptEntry entry)
    {
        _editors.Remove(entry.Id);
        if (entry == CurrentScript)
            Select(_scripts.Entries.Count > 0 ? _scripts.Entries[0] : null);
    }

    private void Select(ScriptEntry entry)
    {
        Show(entry == null ? null : new EditedScript(entry, GetEditor(entry), _draftSaver, _startupCheck, Libraries, _logger));
    }

    private void SelectLibrary(Library library)
    {
        Show(new EditedLibrary(library, GetEditor(library), Libraries));
    }

    /// <summary>
    /// The result line belongs to the Script or Library it was made for, like the errors and checks.
    /// </summary>
    private void Show(EditedItem edited)
    {
        _edited = edited;
        _fieldFocused = false;
        _result = string.Empty;
        _placedBlocksPanel.ClearMessage();
    }

    private ScriptEditor GetEditor(ScriptEntry entry)
    {
        if (!_editors.TryGetValue(entry.Id, out var editor))
        {
            editor = new ScriptEditor(entry.Text, _editorAssist);
            _editors.Add(entry.Id, editor);
        }

        return editor;
    }

    private ScriptEditor GetEditor(Library library)
    {
        if (!_libraryEditors.TryGetValue(library.Id, out var editor))
        {
            editor = new ScriptEditor(library.Text, _editorAssist);
            _libraryEditors.Add(library.Id, editor);
        }

        return editor;
    }

    /// <summary>
    /// Returns whether Ctrl+Enter was pressed in the field.
    /// </summary>
    private bool DrawField(ImGui gui, ImRect rect)
    {
        var editor = _edited.Editor;
        if (editor.Draw(gui, rect, out var compileRequested))
            OnTyped();

        _fieldFocused = FieldFocus.Update(gui, editor.Id, editor.HoldsEscape);

        return compileRequested;
    }

    private void OnTyped()
    {
        _placedBlocksPanel.ClearMessage();
        OnEdited();
    }

    /// <summary>
    /// Null leaves the result line as it is: a check just found errors, which the error list shows.
    /// </summary>
    private void ShowResult(string result)
    {
        if (result != null)
            _result = result;
    }

    /// <summary>
    /// The result line belongs to the text it was made for.
    /// </summary>
    private void OnEdited()
    {
        _edited.OnEdited();
        _result = string.Empty;
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
