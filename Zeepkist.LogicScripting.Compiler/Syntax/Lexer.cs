using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// Splits Script text into tokens. Line breaks are tokens of their own, because each statement takes one line.
/// </summary>
internal static class Lexer
{
    public static List<Token> Tokenize(string text, List<Diagnostic> diagnostics)
    {
        List<Token> tokens = [];
        var line = 1;
        var lineStart = 0;
        var index = 0;

        while (index < text.Length)
        {
            var c = text[index];
            var column = index - lineStart + 1;

            if (c == '\n')
            {
                tokens.Add(new Token(TokenKind.NewLine, "\n", line, column));
                index++;
                line++;
                lineStart = index;
            }
            else if (c == ' ' || c == '\t' || c == '\r')
            {
                index++;
            }
            else if (IsDigit(c))
            {
                var start = index;
                while (index < text.Length && IsDigit(text[index]))
                {
                    index++;
                }

                tokens.Add(new Token(TokenKind.Integer, text.Substring(start, index - start), line, column));
            }
            else if (IsNameStart(c))
            {
                var start = index;
                while (index < text.Length && (IsNameStart(text[index]) || IsDigit(text[index])))
                {
                    index++;
                }

                tokens.Add(new Token(TokenKind.Name, text.Substring(start, index - start), line, column));
            }
            else
            {
                var kind = GetPunctuation(c);
                if (kind == null)
                    diagnostics.Add(new Diagnostic(line, column, $"Unexpected character '{c}'"));
                else
                    tokens.Add(new Token(kind.Value, c.ToString(), line, column));

                index++;
            }
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, line, index - lineStart + 1));
        return tokens;
    }

    private static TokenKind? GetPunctuation(char c)
    {
        return c switch
        {
            '&' => TokenKind.Ampersand,
            '(' => TokenKind.OpenParen,
            ')' => TokenKind.CloseParen,
            ',' => TokenKind.Comma,
            '=' => TokenKind.Equals,
            _ => null
        };
    }

    private static bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private static bool IsNameStart(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    }
}
