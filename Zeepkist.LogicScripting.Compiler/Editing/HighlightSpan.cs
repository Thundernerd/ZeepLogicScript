namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// A run of Script text drawn in one colour. It never spans a line break.
/// </summary>
public readonly struct HighlightSpan
{
    public HighlightSpan(int start, int length, HighlightKind kind)
    {
        Start = start;
        Length = length;
        Kind = kind;
    }

    public int Start { get; }
    public int Length { get; }
    public HighlightKind Kind { get; }
}
