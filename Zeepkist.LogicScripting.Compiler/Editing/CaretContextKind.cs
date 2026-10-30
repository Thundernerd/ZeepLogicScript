namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// What can be typed at the caret, for completion.
/// </summary>
public enum CaretContextKind
{
    /// <summary>
    /// Nothing to complete: inside a comment, a string or a number, or where no name can go.
    /// </summary>
    None,

    /// <summary>
    /// Where an expression, or a statement, can start.
    /// </summary>
    Expression,

    /// <summary>
    /// Where an argument of a call starts: a named argument or an expression.
    /// </summary>
    Argument,

    /// <summary>
    /// The name of a named argument already followed by its colon.
    /// </summary>
    ArgumentName,

    /// <summary>
    /// The Setting after <c>default</c>.
    /// </summary>
    DefaultSetting,

    /// <summary>
    /// The value after a Setting's colon, in a call to a Block Function or after <c>default</c>. A Setting that takes
    /// placed blocks takes an expression instead.
    /// </summary>
    SettingValue
}
