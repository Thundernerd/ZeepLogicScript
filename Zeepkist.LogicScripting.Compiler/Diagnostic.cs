namespace TNRD.Zeepkist.LogicScripting.Compiler;

public sealed class Diagnostic
{
    public Diagnostic(Position position, string message)
        : this(position, null, message)
    {
    }

    public Diagnostic(Position position, string origin, string detail)
    {
        Position = position;
        Origin = origin;
        Detail = detail;
        Message = origin == null ? detail : $"{origin}: {detail}";
    }

    public Position Position { get; }

    /// <summary>
    /// <c>In b.g, line 2</c> for an error inside a Library Function, reported at the Script's call into it; null for
    /// any other.
    /// </summary>
    public string Origin { get; }

    /// <summary>
    /// The message without its <see cref="Origin"/>.
    /// </summary>
    public string Detail { get; }

    public string Message { get; }

    public override string ToString()
    {
        return $"{Position}: {Message}";
    }
}
