using System.Collections.Generic;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// What the Script window's editor shows: a Script or a Library, with its check. Made anew each time one is selected,
/// so the errors and checks belong to the one they were made for.
/// </summary>
internal abstract class EditedItem
{
    /// <summary>
    /// Seconds between checks of unchanged text, in case blocks were added or deleted.
    /// </summary>
    private const float CheckInterval = 1;

    /// <summary>
    /// Seconds after the last edit before the text is checked, so it isn't checked on every key while typing.
    /// </summary>
    private const float CheckDelay = 0.4f;

    private string _checkedText;
    private float _nextCheckTime;

    protected EditedItem(ScriptEditor editor)
    {
        Editor = editor;
        Editor.SetErrors([]);
    }

    public ScriptEditor Editor { get; }

    /// <summary>
    /// Those of the last check, or null when it couldn't run.
    /// </summary>
    public IReadOnlyList<Diagnostic> Errors { get; private set; }

    public bool IsChecked => _checkedText == Editor.Text;
    public bool HasCurrentErrors => Errors != null && IsChecked && Errors.Count > 0;

    /// <summary>
    /// A Script is dropped with the level it belongs to; a Library stays open.
    /// </summary>
    public abstract bool BelongsToLevel { get; }

    /// <summary>
    /// The name row and what goes under it. Returns whether Compile was clicked.
    /// </summary>
    public abstract bool DrawHeader(ImGui gui);

    /// <summary>
    /// Returns the result line, or null when a check just found errors, which the error list shows.
    /// </summary>
    public abstract string Compile();

    /// <summary>
    /// Like <see cref="Compile"/>, for Ctrl+Enter, which has no button to show why Compile can't run.
    /// </summary>
    public abstract string CompileFromKeyboard();

    /// <summary>
    /// The Placed blocks tab. Returns the block under the hovered row, or null. <paramref name="edited"/> is whether
    /// the tab changed the text.
    /// </summary>
    public abstract BlockProperties DrawPlacedBlocks(ImGui gui, PlacedBlocksPanel panel, out bool edited);

    /// <summary>
    /// The check waits until typing stops.
    /// </summary>
    public void OnEdited()
    {
        StoreText(Editor.Text);
        _nextCheckTime = Time.unscaledTime + CheckDelay;
        Editor.SetErrors([]);
    }

    public void RefreshCheck()
    {
        if (Time.unscaledTime < _nextCheckTime)
            return;

        var text = Editor.Text;
        Errors = Check(text);
        _checkedText = text;
        _nextCheckTime = Time.unscaledTime + CheckInterval;
        Editor.SetErrors(Errors == null ? [] : ScriptText.GetErrorUnderlines(text, Errors));
    }

    protected void CheckNow()
    {
        _nextCheckTime = 0;
        RefreshCheck();
    }

    /// <summary>
    /// Checks again on the next <see cref="RefreshCheck"/>.
    /// </summary>
    protected void CheckSoon()
    {
        _nextCheckTime = 0;
    }

    protected abstract void StoreText(string text);

    /// <summary>
    /// Returns the errors in <paramref name="text"/>, or null when it can't be checked.
    /// </summary>
    protected abstract IReadOnlyList<Diagnostic> Check(string text);
}
