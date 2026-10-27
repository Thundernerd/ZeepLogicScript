using System;
using System.Collections.Generic;
using System.Globalization;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// Recursive-descent parser for one statement per line. A syntax error skips the rest of its line, so every line
/// still gets checked.
/// </summary>
internal sealed class Parser
{
    private readonly List<Token> _tokens;
    private readonly List<Diagnostic> _diagnostics;
    private int _position;

    private Parser(List<Token> tokens, List<Diagnostic> diagnostics)
    {
        _tokens = tokens;
        _diagnostics = diagnostics;
    }

    private Token Current => _tokens[_position];

    public static List<Statement> Parse(List<Token> tokens, List<Diagnostic> diagnostics)
    {
        return new Parser(tokens, diagnostics).ParseScript();
    }

    private List<Statement> ParseScript()
    {
        List<Statement> statements = [];

        while (Current.Kind != TokenKind.End)
        {
            if (Current.Kind == TokenKind.NewLine)
            {
                _position++;
                continue;
            }

            try
            {
                statements.Add(ParseStatement());
                if (Current.Kind != TokenKind.NewLine && Current.Kind != TokenKind.End)
                    throw Error(Current, $"Expected the end of the line, found {Describe(Current)}");
            }
            catch (SyntaxError error)
            {
                _diagnostics.Add(error.Diagnostic);
                SkipLine();
            }
        }

        return statements;
    }

    private Statement ParseStatement()
    {
        if (Current.Kind == TokenKind.Name && Peek(1).Kind == TokenKind.Equals)
        {
            var name = Current;
            _position += 2;
            return new Definition(name.Line, name.Column, name.Text, ParseExpression());
        }

        return new ExpressionStatement(ParseExpression());
    }

    private Expression ParseExpression()
    {
        var left = ParsePrimary();
        while (Current.Kind == TokenKind.Ampersand)
        {
            var op = Current;
            _position++;
            left = new BinaryExpression(op.Line, op.Column, BinaryOperator.And, left, ParsePrimary());
        }

        return left;
    }

    private Expression ParsePrimary()
    {
        var token = Current;
        switch (token.Kind)
        {
            case TokenKind.Integer:
                _position++;
                if (!long.TryParse(token.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                    throw Error(token, $"{token.Text} is too large for a Signal");

                return new IntegerLiteral(token.Line, token.Column, value);

            case TokenKind.Name:
                _position++;
                if (Current.Kind == TokenKind.OpenParen)
                    return new CallExpression(token.Line, token.Column, token.Text, ParseArguments());

                return new NameExpression(token.Line, token.Column, token.Text);

            case TokenKind.OpenParen:
                _position++;
                var inner = ParseExpression();
                Expect(TokenKind.CloseParen, "')'");
                return inner;

            default:
                throw Error(token, $"Expected a value, found {Describe(token)}");
        }
    }

    private List<Expression> ParseArguments()
    {
        Expect(TokenKind.OpenParen, "'('");
        List<Expression> arguments = [];
        if (Current.Kind == TokenKind.CloseParen)
        {
            _position++;
            return arguments;
        }

        while (true)
        {
            arguments.Add(ParseExpression());
            if (Current.Kind == TokenKind.Comma)
            {
                _position++;
                continue;
            }

            Expect(TokenKind.CloseParen, "',' or ')'");
            return arguments;
        }
    }

    private void Expect(TokenKind kind, string expected)
    {
        if (Current.Kind != kind)
            throw Error(Current, $"Expected {expected}, found {Describe(Current)}");

        _position++;
    }

    private Token Peek(int offset)
    {
        return _tokens[Math.Min(_position + offset, _tokens.Count - 1)];
    }

    private void SkipLine()
    {
        while (Current.Kind != TokenKind.NewLine && Current.Kind != TokenKind.End)
        {
            _position++;
        }
    }

    private static string Describe(Token token)
    {
        return token.Kind switch
        {
            TokenKind.NewLine => "the end of the line",
            TokenKind.End => "the end of the Script",
            _ => $"'{token.Text}'"
        };
    }

    private static SyntaxError Error(Token token, string message)
    {
        return new SyntaxError(new Diagnostic(token.Line, token.Column, message));
    }

    private sealed class SyntaxError : Exception
    {
        public SyntaxError(Diagnostic diagnostic)
            : base(diagnostic.Message)
        {
            Diagnostic = diagnostic;
        }

        public Diagnostic Diagnostic { get; }
    }
}
