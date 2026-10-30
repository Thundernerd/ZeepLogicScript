namespace TNRD.Zeepkist.LogicScripting.Compiler;

public sealed class Diagnostic
{
    public Diagnostic(Position position, string message)
        : this(position, null, message)
    {
    }

    /// <param name="origin">Where in a Library Function the error is, when it was reported at the Script's call into it.</param>
    public Diagnostic(Position position, string origin, string detail)
    {
        Position = position;
        Origin = origin;
        Detail = detail;
        Message = origin == null ? detail : $"{origin}: {detail}";
    }

    public Position Position { get; }

    /// <summary>
    /// <c>In b.g, line 2</c> for an error inside a Library Function, or null.
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
