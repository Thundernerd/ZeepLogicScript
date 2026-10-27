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

    public Token(BinaryOperator op, Position position)
        : this(TokenKind.Operator, op.Symbol, position)
    {
        Operator = op;
    }

    public TokenKind Kind { get; }
    public string Text { get; }
    public Position Position { get; }

    /// <summary>
    /// An opening bracket with no closing bracket to match it.
    /// </summary>
    public bool IsUnclosed { get; }

    /// <summary>
    /// The binary operator an <see cref="TokenKind.Operator"/> token spells; <c>-</c> can also be a unary minus.
    /// </summary>
    public BinaryOperator Operator { get; }
}
