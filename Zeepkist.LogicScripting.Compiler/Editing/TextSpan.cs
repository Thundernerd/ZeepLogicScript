namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// A run of characters in the Script, by index.
/// </summary>
public readonly struct TextSpan
{
    public TextSpan(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;
}
