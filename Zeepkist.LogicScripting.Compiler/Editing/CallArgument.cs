namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

internal readonly struct CallArgument
{
    public CallArgument(int separatorStart, int? firstTokenStart, string name)
    {
        SeparatorStart = separatorStart;
        FirstTokenStart = firstTokenStart;
        Name = name;
    }

    /// <summary>
    /// The call's opening parenthesis or the comma before the argument.
    /// </summary>
    public int SeparatorStart { get; }

    /// <summary>
    /// Past the line breaks a call left open carries on over; a named argument's starts at its name.
    /// </summary>
    public int? FirstTokenStart { get; }

    /// <summary>
    /// The input or Setting a named argument gives; null for a positional one.
    /// </summary>
    public string Name { get; }

    public bool IsEmpty => FirstTokenStart == null;
    public bool IsNamed => Name != null;
}
