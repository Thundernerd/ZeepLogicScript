using System.Collections.Generic;
using System.Linq;
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
            _window._state.ShowLibraries = false;
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

            _window._state.ShowLibraries = false;
            if (entry != _window.CurrentScript)
                _window.Select(entry);

            return true;
        }

        /// <summary>
        /// Like Delete in the Script's context menu, which opens the delete prompt. Returns whether there's one by that
        /// name.
        /// </summary>
        public bool OpenDelete(string name)
        {
            var entry = FindScript(name);
            if (entry == null)
                return false;

            _window._scriptListPanel.OpenDelete(entry);
            return true;
        }

        /// <summary>
        /// Like New Library on the Libraries tab and naming it in the prompt, which selects it. Returns why the name
        /// isn't taken, or null.
        /// </summary>
        public string NewLibrary(string name)
        {
            _window._state.ShowLibraries = true;
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

            _window._state.ShowLibraries = true;
            if (library != _window.CurrentLibrary)
                _window.SelectLibrary(library);

            return true;
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
