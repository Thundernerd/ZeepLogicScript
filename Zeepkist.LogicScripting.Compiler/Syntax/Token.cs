namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal readonly struct Token
{
    public Token(TokenKind kind, string text, Position position, bool isUnclosed = false)
    {
        Kind = kind;
        Text = text;
        Position = position;
        IsUnclosed = isUnclosed;
    }

    public TokenKind Kind { get; }
    public string Text { get; }
    public Position Position { get; }

    /// <summary>
    /// An opening bracket with no closing bracket to match it.
    /// </summary>
    public bool IsUnclosed { get; }
}
