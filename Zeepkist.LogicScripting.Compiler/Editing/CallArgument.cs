namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// One argument of an <see cref="OpenCall"/>, as token indices.
/// </summary>
internal readonly struct CallArgument
{
    public CallArgument(int start, int first, string name)
    {
        Start = start;
        First = first;
        Name = name;
    }

    /// <summary>
    /// The call's opening parenthesis or the comma before the argument.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Its first token, past the line breaks a call left open carries on over; -1 when it's empty.
    /// </summary>
    public int First { get; }

    /// <summary>
    /// The input or Setting a named argument gives; null for a positional one.
    /// </summary>
    public string Name { get; }

    public bool IsEmpty => First < 0;
    public bool IsNamed => Name != null;
}
