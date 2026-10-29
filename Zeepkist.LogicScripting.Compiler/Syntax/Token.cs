using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal readonly struct Token
{
    public Token(TokenKind kind, string text, Position position, TextSpan span, bool isUnclosed = false)
    {
        Kind = kind;
        Text = text;
        Position = position;
        Span = span;
        IsUnclosed = isUnclosed;
    }

    public Token(BinaryOperator binaryOperator, UnaryOperator unaryOperator, Position position, int start)
        : this(
            TokenKind.Operator,
            binaryOperator?.Symbol ?? unaryOperator.Symbol,
            position,
            new TextSpan(start, (binaryOperator?.Symbol ?? unaryOperator.Symbol).Length))
    {
        BinaryOperator = binaryOperator;
        UnaryOperator = unaryOperator;
    }

    public TokenKind Kind { get; }
    public string Text { get; }
    public Position Position { get; }

    /// <summary>
    /// The characters the token covers in the text; a string's include its quotes, which <see cref="Text"/> leaves
    /// out.
    /// </summary>
    public TextSpan Span { get; }

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
