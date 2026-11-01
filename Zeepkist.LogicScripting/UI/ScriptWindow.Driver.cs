using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;
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
        public IReadOnlyList<string> ScriptNames => [.. _window._scripts.Entries.Select(e => e.Name)];

        /// <summary>
        /// Empty until a Compile, and again after an edit or selecting another Script.
        /// </summary>
        public string Result => _window._result;

        /// <summary>
        /// The selected Script's text in the editor, or null with none selected.
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

        public void SetOpen(bool open)
        {
            _window.SetOpen(open);
        }

        /// <summary>
        /// Like New Script and naming it in the prompt, which selects it. Returns why the name isn't taken, or null.
        /// </summary>
        public string NewScript(string name)
        {
            var entry = _window._scriptListPanel.New(name, out var error);
            if (entry != null)
                _window.Select(entry);

            return error;
        }

        /// <summary>
        /// Like clicking the Script in the list. Returns whether there's one by that name.
        /// </summary>
        public bool SelectScript(string name)
        {
            var entry = _window._scripts.Entries.FirstOrDefault(e => e.Name == name);
            if (entry == null)
                return false;

            if (entry != _window.CurrentScript)
                _window.Select(entry);

            return true;
        }

        /// <summary>
        /// Replaces the selected Script's text as one edit, as typing it would leave it.
        /// </summary>
        public void SetText(string text)
        {
            _window._edited.Editor.Replace(text, text.Length, Time.unscaledTimeAsDouble);
            _window.OnTyped();
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
