using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// Splits Script text into tokens. Line breaks are tokens of their own, because each statement takes one line,
/// except inside a matched pair of brackets, where a statement continues on the next line. <c>#</c> starts a comment
/// that runs to the end of the line.
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
            else if (c == '#')
            {
                while (index < text.Length && text[index] != '\n')
                {
                    index++;
                }
            }
            else if (IsDigit(c))
            {
                var start = index;
                index = SkipDigits(text, index);
                tokens.Add(new Token(TokenKind.Integer, text.Substring(start, index - start), line, column));

                // Keep the whole part as the token, so the rest of the line still parses
                if (index + 1 < text.Length && text[index] == '.' && IsDigit(text[index + 1]))
                {
                    diagnostics.Add(new Diagnostic(line, column, "Only whole numbers are allowed"));
                    index = SkipDigits(text, index + 1);
                }
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
            else if (index + 1 < text.Length && GetTwoCharPunctuation(c, text[index + 1]) is { } twoChar)
            {
                tokens.Add(new Token(twoChar, text.Substring(index, 2), line, column));
                index += 2;
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
        return JoinBracketedLines(tokens);
    }

    /// <summary>
    /// Drops the line breaks inside each matched pair of brackets and flags opening brackets that are never closed.
    /// An unclosed bracket doesn't swallow the lines after it, so they're still checked on their own.
    /// </summary>
    private static List<Token> JoinBracketedLines(List<Token> tokens)
    {
        Stack<int> open = new();
        var matched = new bool[tokens.Count];
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Kind == TokenKind.OpenParen)
            {
                open.Push(i);
            }
            else if (tokens[i].Kind == TokenKind.CloseParen && open.Count > 0)
            {
                matched[open.Pop()] = true;
                matched[i] = true;
            }
        }

        List<Token> joined = [];
        var depth = 0;
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Kind == TokenKind.NewLine && depth > 0)
                continue;

            if (token.Kind == TokenKind.OpenParen && matched[i])
                depth++;
            else if (token.Kind == TokenKind.CloseParen && matched[i])
                depth--;

            if (token.Kind == TokenKind.OpenParen && !matched[i])
                token = new Token(token.Kind, token.Text, token.Line, token.Column, true);

            joined.Add(token);
        }

        return joined;
    }

    private static int SkipDigits(string text, int index)
    {
        while (index < text.Length && IsDigit(text[index]))
        {
            index++;
        }

        return index;
    }

    private static TokenKind? GetTwoCharPunctuation(char first, char second)
    {
        if (second != '=')
            return null;

        return first switch
        {
            '<' => TokenKind.LessEquals,
            '>' => TokenKind.GreaterEquals,
            '=' => TokenKind.EqualsEquals,
            '!' => TokenKind.BangEquals,
            _ => null
        };
    }

    private static TokenKind? GetPunctuation(char c)
    {
        return c switch
        {
            '&' => TokenKind.Ampersand,
            '|' => TokenKind.Pipe,
            '^' => TokenKind.Caret,
            '+' => TokenKind.Plus,
            '-' => TokenKind.Minus,
            '*' => TokenKind.Star,
            '/' => TokenKind.Slash,
            '%' => TokenKind.Percent,
            '<' => TokenKind.Less,
            '>' => TokenKind.Greater,
            '!' => TokenKind.Bang,
            '(' => TokenKind.OpenParen,
            ')' => TokenKind.CloseParen,
            ',' => TokenKind.Comma,
            ':' => TokenKind.Colon,
            '.' => TokenKind.Dot,
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
