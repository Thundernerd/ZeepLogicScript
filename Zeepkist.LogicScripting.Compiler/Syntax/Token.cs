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

    public Token(BinaryOperator binaryOperator, UnaryOperator unaryOperator, Position position)
        : this(TokenKind.Operator, binaryOperator?.Symbol ?? unaryOperator.Symbol, position)
    {
        BinaryOperator = binaryOperator;
        UnaryOperator = unaryOperator;
    }

    public TokenKind Kind { get; }
    public string Text { get; }
    public Position Position { get; }

    /// <summary>
    /// An opening parenthesis with no closing one to match it.
    /// </summary>
    public bool IsUnclosed { get; }

    /// <summary>
    /// The binary operator an <see cref="TokenKind.Operator"/> token spells, if any.
    /// </summary>
    public BinaryOperator BinaryOperator { get; }

    /// <summary>
    /// The unary operator an <see cref="TokenKind.Operator"/> token spells, if any; <c>-</c> is both.
    /// </summary>
    public UnaryOperator UnaryOperator { get; }
}
