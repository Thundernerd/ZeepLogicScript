namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// How a Script in the window stands against the level.
/// </summary>
public enum ScriptMark
{
    /// <summary>
    /// The level holds the window's name and text.
    /// </summary>
    None,

    /// <summary>
    /// The level holds another name or text than the window.
    /// </summary>
    NotCompiled,

    /// <summary>
    /// The level has the Script's Block Group but not its text, usually because the block holding it was deleted.
    /// </summary>
    CarrierMissing,

    /// <summary>
    /// The level has neither the Script's text nor its Block Group, so the Script only exists in the window.
    /// </summary>
    NeverCompiled
}
