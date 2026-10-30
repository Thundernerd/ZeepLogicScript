namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// One argument of an <see cref="OpenCall"/>.
/// </summary>
internal readonly struct CallArgument
{
    public CallArgument(int start, int? valueStart, string name)
    {
        Start = start;
        ValueStart = valueStart;
        Name = name;
    }

    /// <summary>
    /// Where the call's opening parenthesis or the comma before the argument is in the text.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Where its first token is in the text, past the line breaks a call left open carries on over; null when it's
    /// empty.
    /// </summary>
    public int? ValueStart { get; }

    /// <summary>
    /// The input or Setting a named argument gives; null for a positional one.
    /// </summary>
    public string Name { get; }

    public bool IsEmpty => ValueStart == null;
    public bool IsNamed => Name != null;
}
