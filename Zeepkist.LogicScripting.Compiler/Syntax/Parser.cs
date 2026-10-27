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
        return ParseBinary(0);
    }

    /// <summary>
    /// Precedence climbing over C's binary precedence levels; every operator is left-associative.
    /// </summary>
    private Expression ParseBinary(int minPrecedence)
    {
        var left = ParseUnary();
        while (TryGetBinaryOperator(Current.Kind, out var op, out var precedence) && precedence >= minPrecedence)
        {
            var token = Current;
            _position++;
            var right = ParseBinary(precedence + 1);
            left = new BinaryExpression(token.Line, token.Column, op, left, right);
        }

        return left;
    }

    private Expression ParseUnary()
    {
        var token = Current;
        switch (token.Kind)
        {
            case TokenKind.Bang:
                _position++;
                return new UnaryExpression(token.Line, token.Column, UnaryOperator.Not, ParseUnary());

            case TokenKind.Minus when IsTouchingInteger(token, Peek(1)):
                // A minus right before a number is part of the literal, so -5 is one Generator, not 0 - 5
                _position++;
                return ParseInteger(Current, token, "-");

            case TokenKind.Minus:
                _position++;
                return new UnaryExpression(token.Line, token.Column, UnaryOperator.Negate, ParseUnary());

            default:
                return ParsePostfix();
        }
    }

    private static bool IsTouchingInteger(Token minus, Token next)
    {
        return next.Kind == TokenKind.Integer && next.Line == minus.Line && next.Column == minus.Column + 1;
    }

    private Expression ParsePostfix()
    {
        var expression = ParsePrimary();
        while (Current.Kind == TokenKind.Dot)
        {
            var dot = Current;
            _position++;
            if (expression is not (NameExpression or CallExpression))
                throw Error(dot, "Only names and calls have outputs to pick with .");

            var output = Current;
            Expect(TokenKind.Name, "an output name");
            expression = new MemberExpression(output.Line, output.Column, expression, output.Text);
        }

        return expression;
    }

    private static bool TryGetBinaryOperator(TokenKind kind, out BinaryOperator op, out int precedence)
    {
        (op, precedence) = kind switch
        {
            TokenKind.Pipe => (BinaryOperator.Or, 0),
            TokenKind.Caret => (BinaryOperator.Xor, 1),
            TokenKind.Ampersand => (BinaryOperator.And, 2),
            TokenKind.EqualsEquals => (BinaryOperator.Equal, 3),
            TokenKind.BangEquals => (BinaryOperator.NotEqual, 3),
            TokenKind.Less => (BinaryOperator.Less, 4),
            TokenKind.Greater => (BinaryOperator.Greater, 4),
            TokenKind.LessEquals => (BinaryOperator.LessEqual, 4),
            TokenKind.GreaterEquals => (BinaryOperator.GreaterEqual, 4),
            TokenKind.Plus => (BinaryOperator.Add, 5),
            TokenKind.Minus => (BinaryOperator.Subtract, 5),
            TokenKind.Star => (BinaryOperator.Multiply, 6),
            TokenKind.Slash => (BinaryOperator.Divide, 6),
            TokenKind.Percent => (BinaryOperator.Modulo, 6),
            _ => (default, -1)
        };

        return precedence >= 0;
    }

    private Expression ParsePrimary()
    {
        var token = Current;
        switch (token.Kind)
        {
            case TokenKind.Integer:
                return ParseInteger(token, token, string.Empty);

            case TokenKind.Name:
                _position++;
                if (Current.Kind == TokenKind.OpenParen)
                    return ParseCall(token);

                return new NameExpression(token.Line, token.Column, token.Text);

            case TokenKind.OpenParen:
                ExpectOpenParen();
                var inner = ParseExpression();
                Expect(TokenKind.CloseParen, "')'");
                return inner;

            default:
                throw Error(token, $"Expected a value, found {Describe(token)}");
        }
    }

    /// <param name="digits">The integer token.</param>
    /// <param name="start">Where the literal starts: the digits, or the minus before them.</param>
    /// <param name="sign">"-" or empty.</param>
    private IntegerLiteral ParseInteger(Token digits, Token start, string sign)
    {
        _position++;
        var text = sign + digits.Text;
        if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            throw Error(start, $"{text} doesn't fit in a Signal");

        return new IntegerLiteral(start.Line, start.Column, value);
    }

    private CallExpression ParseCall(Token function)
    {
        ExpectOpenParen();
        List<Expression> arguments = [];
        List<NamedArgument> namedArguments = [];
        if (Current.Kind == TokenKind.CloseParen)
        {
            _position++;
            return new CallExpression(function.Line, function.Column, function.Text, arguments, namedArguments);
        }

        while (true)
        {
            if (Current.Kind == TokenKind.Name && Peek(1).Kind == TokenKind.Colon)
            {
                var name = Current;
                _position += 2;
                var valueStart = Current;
                var value = ParseExpression();
                namedArguments.Add(new NamedArgument(name.Line, name.Column, name.Text, value, valueStart.Line, valueStart.Column));
            }
            else
            {
                var start = Current;
                var argument = ParseExpression();
                if (namedArguments.Count > 0)
                    throw Error(start, "Positional inputs go before named arguments");

                arguments.Add(argument);
            }

            if (Current.Kind == TokenKind.Comma)
            {
                _position++;
                continue;
            }

            Expect(TokenKind.CloseParen, "',' or ')'");
            return new CallExpression(function.Line, function.Column, function.Text, arguments, namedArguments);
        }
    }

    private void ExpectOpenParen()
    {
        if (Current.IsUnclosed)
            throw Error(Current, "'(' isn't closed");

        Expect(TokenKind.OpenParen, "'('");
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
