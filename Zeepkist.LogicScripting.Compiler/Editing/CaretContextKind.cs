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
    Expression
}
