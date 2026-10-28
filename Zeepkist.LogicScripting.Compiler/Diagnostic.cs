namespace TNRD.Zeepkist.LogicScripting.Compiler;

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
