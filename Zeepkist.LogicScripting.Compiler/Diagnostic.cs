namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// A Compile error at a one-based line and column of the Script.
/// </summary>
public sealed class Diagnostic
{
    public Diagnostic(int line, int column, string message)
    {
        Line = line;
        Column = column;
        Message = message;
    }

    public int Line { get; }
    public int Column { get; }
    public string Message { get; }

    public override string ToString()
    {
        return $"{Line}:{Column}: {Message}";
    }
}
