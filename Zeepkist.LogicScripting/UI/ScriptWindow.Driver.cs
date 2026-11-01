using System.Collections.Generic;
using System.Linq;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptWindow
{
    /// <summary>
    /// Does what the window's buttons and typing do, through the same code paths, for the in-game tests. Nothing in the
    /// mod itself uses it.
    /// </summary>
    internal sealed class Driver
    {
        private readonly ScriptWindow _window;

        public Driver(ScriptWindow window)
        {
            _window = window;
        }

        public bool IsOpen => _window.IsOpen;
        public bool HasScript => _window.CurrentScript != null;

        /// <summary>
        /// Whether a Script or a Library is selected, so there's text in the editor.
        /// </summary>
        public bool HasText => _window._edited != null;

        public IReadOnlyList<string> ScriptNames => [.. _window._scripts.Entries.Select(e => e.Name)];
        public IReadOnlyList<string> LibraryNames => [.. _window.Libraries.Entries.Select(l => l.Name)];

        /// <summary>
        /// Empty until a Compile, and again after an edit or selecting another Script.
        /// </summary>
        public string Result => _window._result;

        /// <summary>
        /// The selected Script's or Library's text in the editor, or null with neither selected.
        /// </summary>
        public string Text => _window._edited?.Editor.Text;

        /// <summary>
        /// Whether the window has checked the current text. It only checks while it's open, a moment after the last edit.
        /// </summary>
        public bool IsChecked => _window._edited?.IsChecked ?? false;

        /// <summary>
        /// The errors the last check found, or null when it couldn't run.
        /// </summary>
        public IReadOnlyList<Diagnostic> Errors => _window._edited?.Errors;

        /// <inheritdoc cref="ScriptEditor.CompletionItems"/>
        public IReadOnlyList<CompletionItem> CompletionItems => _window._edited?.Editor.CompletionItems ?? [];

        /// <inheritdoc cref="ScriptEditor.CallHelp"/>
        public CallHelp CallHelp => _window._edited?.Editor.CallHelp;

        public bool IsFieldFocused => _window.AnyFieldFocused;

        /// <summary>
        /// The shown tab's list and footer.
        /// </summary>
        public IListPanel ShownList => _window._state.ShowLibraries ? _window._libraryListPanel : _window._scriptListPanel;

        /// <summary>
        /// The window's rect as Imui last drew it.
        /// </summary>
        public ImRect WindowRect => new(_window._state.X, _window._state.Y, _window._state.Width, _window._state.Height);

        /// <summary>
        /// Whether a height <see cref="SetWindowHeight"/> asked for waits for the window to be drawn.
        /// </summary>
        public bool IsHeightPending => _window._requestedHeight != null;

        public void SetOpen(bool open)
        {
            _window.SetOpen(open);
        }

        /// <summary>
        /// Like New Script on the Scripts tab and naming it in the prompt, which selects it. Returns why the name isn't
        /// taken, or null.
        /// </summary>
        public string NewScript(string name)
        {
            ShowTab(libraries: false);
            var entry = _window._scriptListPanel.New(name, out var error);
            if (entry != null)
                _window.Select(entry);

            return error;
        }

        /// <summary>
        /// Like clicking the Script in the list on the Scripts tab. Returns whether there's one by that name.
        /// </summary>
        public bool SelectScript(string name)
        {
            var entry = FindScript(name);
            if (entry == null)
                return false;

            ShowTab(libraries: false);
            if (entry != _window.CurrentScript)
                _window.Select(entry);

            return true;
        }

        /// <summary>
        /// Like Select group in the Script's context menu on the Scripts tab. Returns whether there's one by that name.
        /// </summary>
        public bool SelectGroup(string name)
        {
            var entry = FindScript(name);
            if (entry == null)
                return false;

            ShowTab(libraries: false);
            _window._scriptListPanel.SelectGroup(entry);
            return true;
        }

        /// <summary>
        /// Like dragging the window's edge until it's this tall, which it is from the next frame.
        /// </summary>
        public void SetWindowHeight(float height)
        {
            _window._requestedHeight = height;
        }

        /// <summary>
        /// Like New Library on the Libraries tab and naming it in the prompt, which selects it. Returns why the name
        /// isn't taken, or null.
        /// </summary>
        public string NewLibrary(string name)
        {
            ShowTab(libraries: true);
            var library = _window._libraryListPanel.New(name, out var error);
            if (library != null)
                _window.SelectLibrary(library);

            return error;
        }

        /// <summary>
        /// Like clicking the Library in the list on the Libraries tab. Returns whether there's one by that name.
        /// </summary>
        public bool SelectLibrary(string name)
        {
            var library = _window.Libraries.Entries.FirstOrDefault(l => l.Name == name);
            if (library == null)
                return false;

            ShowTab(libraries: true);
            if (library != _window.CurrentLibrary)
                _window.SelectLibrary(library);

            return true;
        }

        /// <summary>
        /// Like clicking the Scripts or the Libraries tab, whose list the buttons for one are in.
        /// </summary>
        public void ShowTab(bool libraries)
        {
            _window._state.ShowLibraries = libraries;
        }

        /// <summary>
        /// Null when there's none by that name.
        /// </summary>
        private ScriptEntry FindScript(string name)
        {
            return _window._scripts.Entries.FirstOrDefault(e => e.Name == name);
        }

        /// <summary>
        /// Replaces the selected Script's or Library's text as one edit, as typing it would leave it.
        /// </summary>
        public void SetText(string text)
        {
            _window._edited.Editor.Replace(text, text.Length, Time.unscaledTimeAsDouble);
            _window.OnTyped();
        }

        /// <summary>
        /// Like clicking at a character index of the selected Script's or Library's text, which focuses the field.
        /// </summary>
        public void PlaceCaret(int index)
        {
            _window._edited.Editor.JumpTo(index);
        }

        /// <inheritdoc cref="ScriptEditor.RequestCompletion"/>
        public void RequestCompletion()
        {
            _window._edited.Editor.RequestCompletion();
        }

        /// <inheritdoc cref="ScriptEditor.HoldSentEscape"/>
        public void HoldSentEscape()
        {
            _window._edited?.Editor.HoldSentEscape();
        }

        /// <summary>
        /// Like Insert selected. Returns whether it changed the text.
        /// </summary>
        public bool InsertSelected(LEV_LevelEditorCentral central)
        {
            var edited = _window._placedBlocksPanel.InsertSelected(central, _window._edited.Editor, Time.unscaledTimeAsDouble);
            if (edited)
                _window.OnEdited();

            return edited;
        }

        /// <summary>
        /// Like Ctrl+Enter in the editor. Returns the result line, which says why when Compile can't run, or null when
        /// the check it runs first on unchecked text found errors, which the error list shows.
        /// </summary>
        public string Compile()
        {
            var result = _window._edited.CompileFromKeyboard();
            _window.ShowResult(result);
            return result;
        }
    }
}
