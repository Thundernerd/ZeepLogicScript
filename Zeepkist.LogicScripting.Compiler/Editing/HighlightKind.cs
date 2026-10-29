namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// What a piece of Script text is, for its color in the editor.
/// </summary>
public enum HighlightKind
{
    Name,

    /// <summary>
    /// A Block Function, one of the Script's own Functions, or <c>placed</c>.
    /// </summary>
    Function,

    /// <summary>
    /// <c>default</c>, <c>true</c>, <c>false</c>, <c>function</c> and <c>end</c>.
    /// </summary>
    Keyword,
    Number,
    String,

    /// <summary>
    /// An operator, the <c>=</c> of a definition, or the <c>-></c> before a Function's outputs.
    /// </summary>
    Operator,
    Punctuation,
    Comment
}
