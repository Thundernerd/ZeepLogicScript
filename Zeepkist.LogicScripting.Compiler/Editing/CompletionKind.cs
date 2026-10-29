namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

public enum CompletionKind
{
    /// <summary>
    /// A Block Function, an alias of one, or <c>placed</c>: accepting it opens its call.
    /// </summary>
    Function,
    Keyword,

    /// <summary>
    /// A name the Script defines.
    /// </summary>
    Name
}
