namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

public enum CompletionKind
{
    /// <summary>
    /// A Block Function, an alias of one, or <c>placed</c>: accepting it opens its call.
    /// </summary>
    Function,
    Keyword,
    Name,

    /// <summary>
    /// A named argument, labeled with its colon: accepting it writes the colon and a space too.
    /// </summary>
    Argument
}
