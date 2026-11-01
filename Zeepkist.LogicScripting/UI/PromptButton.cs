namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// A button of a list tab's prompt, for the in-game tests to press.
/// </summary>
internal enum PromptButton
{
    Ok,
    Cancel,

    /// <summary>
    /// Delete, or for a Script with blocks, Delete blocks too.
    /// </summary>
    Delete,

    KeepBlocks
}
