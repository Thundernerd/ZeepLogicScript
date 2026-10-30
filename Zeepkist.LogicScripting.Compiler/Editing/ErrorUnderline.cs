namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// What the editor underlines for one error, and the error's message, which hovering the underline shows.
/// </summary>
public sealed class ErrorUnderline
{
    public ErrorUnderline(TextSpan span, string message)
    {
        Span = span;
        Message = message;
    }

    /// <summary>
    /// Empty at the end of a line, where the editor still draws a short underline.
    /// </summary>
    public TextSpan Span { get; }

    public string Message { get; }

    /// <summary>
    /// Whether the character at <paramref name="index"/> is underlined; an empty underline covers its own index.
    /// </summary>
    public bool Covers(int index)
    {
        return Span.Length == 0 ? index == Span.Start : Span.Start <= index && index < Span.End;
    }
}
