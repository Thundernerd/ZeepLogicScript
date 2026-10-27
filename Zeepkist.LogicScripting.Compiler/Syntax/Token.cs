namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal enum TokenKind
{
    Integer,
    Name,
    Ampersand,
    OpenParen,
    CloseParen,
    Comma,
    Equals,
    NewLine,
    End
}

internal readonly struct Token
{
    public Token(TokenKind kind, string text, int line, int column)
    {
        Kind = kind;
        Text = text;
        Line = line;
        Column = column;
    }

    public TokenKind Kind { get; }
    public string Text { get; }
    public int Line { get; }
    public int Column { get; }
}
