namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// What a piece of Script text is, for its color in the editor.
/// </summary>
public enum HighlightKind
{
    Name,

    /// <summary>
    /// A Block Function or <c>placed</c>.
    /// </summary>
    Function,

    /// <summary>
    /// <c>default</c>, <c>true</c> and <c>false</c>.
    /// </summary>
    Keyword,
    Number,
    String,

    /// <summary>
    /// An operator, or the <c>=</c> of a definition.
    /// </summary>
    Operator,
    Punctuation,
    Comment
}
