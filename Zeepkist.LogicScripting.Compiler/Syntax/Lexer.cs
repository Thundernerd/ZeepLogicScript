using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// Splits Script text into tokens. Line breaks are tokens of their own, because each statement takes one line,
/// except inside a matched pair of parentheses, where a statement continues on the next line. <c>#</c> starts a comment
/// that runs to the end of the line.
/// </summary>
internal static class Lexer
{
    /// <summary>
    /// Comments make no tokens; when <paramref name="comments"/> is given, each one's span is added to it instead.
    /// </summary>
    public static List<Token> Tokenize(string text, List<Diagnostic> diagnostics, List<TextSpan> comments = null)
    {
        List<Token> tokens = [];
        var line = 1;
        var lineStart = 0;
        var index = 0;

        Position CurrentPosition()
        {
            return new Position(line, index - lineStart + 1);
        }

        while (index < text.Length)
        {
            var c = text[index];
            var position = CurrentPosition();

            if (c == '\n')
            {
                tokens.Add(new Token(TokenKind.NewLine, "\n", position, new TextSpan(index, 1)));
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
                var start = index;
                while (index < text.Length && text[index] != '\n')
                {
                    index++;
                }

                comments?.Add(new TextSpan(start, index - start));
            }
            else if (IsDigit(c))
            {
                var start = index;
                index = SkipDigits(text, index);
                tokens.Add(new Token(TokenKind.Integer, text.Substring(start, index - start), position, new TextSpan(start, index - start)));

                // Keep the whole part as the token, so the rest of the line still parses
                if (index + 1 < text.Length && text[index] == '.' && IsDigit(text[index + 1]))
                {
                    diagnostics.Add(new Diagnostic(position, "Only whole numbers are allowed"));
                    index = SkipDigits(text, index + 1);
                }
            }
            else if (c == '"')
            {
                var start = index + 1;
                index = start;
                while (index < text.Length && text[index] != '"' && text[index] != '\n')
                {
                    index++;
                }

                var value = text.Substring(start, index - start);
                if (index < text.Length && text[index] == '"')
                    index++;
                else
                    diagnostics.Add(new Diagnostic(position, "Text in quotes isn't closed on its line"));

                tokens.Add(new Token(TokenKind.String, value, position, new TextSpan(start - 1, index - start + 1)));
            }
            else if (IsNameStart(c))
            {
                var start = index;
                while (index < text.Length && (IsNameStart(text[index]) || IsDigit(text[index])))
                {
                    index++;
                }

                tokens.Add(new Token(TokenKind.Name, text.Substring(start, index - start), position, new TextSpan(start, index - start)));
            }
            else if (MatchOperator(text, index, position) is { } op)
            {
                tokens.Add(op);
                index += op.Text.Length;
            }
            else
            {
                var kind = GetPunctuation(c);
                if (kind == null)
                    diagnostics.Add(new Diagnostic(position, $"Unexpected character '{c}'"));
                else
                    tokens.Add(new Token(kind.Value, c.ToString(), position, new TextSpan(index, 1)));

                index++;
            }
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, CurrentPosition(), new TextSpan(index, 0)));
        return JoinParenthesizedLines(tokens);
    }

    /// <summary>
    /// Drops the line breaks inside each matched pair of parentheses and flags opening ones that are never closed. An
    /// unclosed parenthesis doesn't swallow the lines after it, so they're still checked on their own.
    /// </summary>
    private static List<Token> JoinParenthesizedLines(List<Token> tokens)
    {
        Stack<int> open = new();
        var matched = new bool[tokens.Count];
        for (var i = 0; i < tokens.Count; i++)
        {
            // A definition can't sit inside parentheses, so the ones still open before it were never closed
            if (IsDefinitionStart(tokens, i))
            {
                open.Clear();
            }
            else if (tokens[i].Kind == TokenKind.OpenParen)
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
                token = new Token(token.Kind, token.Text, token.Position, token.Span, true);

            joined.Add(token);
        }

        return joined;
    }

    private static bool IsDefinitionStart(List<Token> tokens, int index)
    {
        return index > 0
            && index + 1 < tokens.Count
            && tokens[index - 1].Kind == TokenKind.NewLine
            && tokens[index].Kind == TokenKind.Name
            && tokens[index + 1].Kind == TokenKind.Equals;
    }

    private static int SkipDigits(string text, int index)
    {
        while (index < text.Length && IsDigit(text[index]))
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// The operator token whose symbol starts at <paramref name="index"/>, preferring the longest, so <c>!=</c> isn't
    /// read as <c>!</c>. A symbol in both tables, like <c>-</c>, spells both operators.
    /// </summary>
    private static Token? MatchOperator(string text, int index, Position position)
    {
        var binary = MatchLongest(BinaryOperator.All, op => op.Symbol, text, index);
        var unary = MatchLongest(UnaryOperator.All, op => op.Symbol, text, index);
        if (binary != null && unary != null && binary.Symbol.Length != unary.Symbol.Length)
        {
            if (binary.Symbol.Length > unary.Symbol.Length)
                unary = null;
            else
                binary = null;
        }

        if (binary == null && unary == null)
            return null;

        return new Token(binary, unary, position, index);
    }

    private static T MatchLongest<T>(IEnumerable<T> operators, Func<T, string> getSymbol, string text, int index)
        where T : class
    {
        T match = null;
        var matchLength = 0;
        foreach (var op in operators)
        {
            var symbol = getSymbol(op);
            if (symbol.Length > matchLength
                && index + symbol.Length <= text.Length
                && string.CompareOrdinal(text, index, symbol, 0, symbol.Length) == 0)
            {
                match = op;
                matchLength = symbol.Length;
            }
        }

        return match;
    }

    private static TokenKind? GetPunctuation(char c)
    {
        return c switch
        {
            '(' => TokenKind.OpenParen,
            ')' => TokenKind.CloseParen,
            '[' => TokenKind.OpenBracket,
            ']' => TokenKind.CloseBracket,
            ',' => TokenKind.Comma,
            ':' => TokenKind.Colon,
            '.' => TokenKind.Dot,
            '=' => TokenKind.Equals,
            _ => null
        };
    }

    internal static bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    internal static bool IsNameStart(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    }
}
