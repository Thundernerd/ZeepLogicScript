namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal enum TokenKind
{
    Integer,
    Name,
    Ampersand,
    Pipe,
    Caret,
    Plus,
    Minus,
    Star,
    Slash,
    Percent,
    Less,
    Greater,
    LessEquals,
    GreaterEquals,
    EqualsEquals,
    BangEquals,
    Bang,
    OpenParen,
    CloseParen,
    Comma,
    Colon,
    Dot,
    Equals,
    NewLine,
    End
}

internal readonly struct Token
{
    public Token(TokenKind kind, string text, int line, int column, bool isUnclosed = false)
    {
        Kind = kind;
        Text = text;
        Line = line;
        Column = column;
        IsUnclosed = isUnclosed;
    }

    public TokenKind Kind { get; }
    public string Text { get; }
    public int Line { get; }
    public int Column { get; }

    /// <summary>
    /// An opening bracket with no closing bracket to match it.
    /// </summary>
    public bool IsUnclosed { get; }
}
