namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

public enum CompletionKind
{
    /// <summary>
    /// A Block Function, an alias of one, or <c>placed</c>: accepting it opens its call.
    /// </summary>
    Function,

    /// <summary>
    /// <c>true</c> and <c>false</c>.
    /// </summary>
    Keyword
}
