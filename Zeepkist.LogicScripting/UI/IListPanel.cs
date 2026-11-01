using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// A list tab's footer, without what it lists, for <see cref="ScriptWindow.Driver"/> to work the shown tab's.
/// </summary>
internal interface IListPanel
{
    public string Noun { get; }
    public IReadOnlyList<string> Names { get; }

    /// <summary>
    /// The result line, or empty.
    /// </summary>
    public string Message { get; }

    public ListPrompt OpenPrompt { get; }

    /// <inheritdoc cref="ListFooterLayout"/>
    public ListFooterLayout FooterLayout { get; }

    /// <summary>
    /// Whether a button <see cref="Press"/> pressed still waits for the prompt to be drawn.
    /// </summary>
    public bool IsPressPending { get; }

    public void OpenNew();

    /// <summary>
    /// Returns whether there's an entry by that name.
    /// </summary>
    public bool OpenRename(string name);

    /// <inheritdoc cref="OpenRename"/>
    public bool OpenDelete(string name);

    /// <summary>
    /// Replaces the text in the open name prompt. Returns whether one is open.
    /// </summary>
    public bool SetName(string name);

    /// <summary>
    /// The open prompt's buttons, or none with no prompt open.
    /// </summary>
    public IReadOnlyList<PromptButton> PromptButtons { get; }

    /// <summary>
    /// Presses one of <see cref="PromptButtons"/> the next time the prompt is drawn, as clicking it does.
    /// </summary>
    public void Press(PromptButton button);
}
