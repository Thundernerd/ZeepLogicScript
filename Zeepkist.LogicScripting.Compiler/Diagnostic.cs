namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// A Compile error at a position in the Script.
/// </summary>
public sealed class Diagnostic
{
    public Diagnostic(Position position, string message)
    {
        Position = position;
        Message = message;
    }

    public Position Position { get; }
    public string Message { get; }

    public override string ToString()
    {
        return $"{Position}: {Message}";
    }
}
