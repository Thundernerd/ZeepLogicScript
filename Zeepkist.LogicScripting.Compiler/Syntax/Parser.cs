using System;
using System.Collections.Generic;
using System.Globalization;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// Recursive-descent parser for one statement per line. A syntax error skips the rest of its line, so every line
/// still gets checked.
/// </summary>
internal sealed partial class Parser
{
    private readonly List<Token> _tokens;
    private readonly List<Diagnostic> _diagnostics;
    private int _index;

    private Parser(List<Token> tokens, List<Diagnostic> diagnostics)
    {
        _tokens = tokens;
        _diagnostics = diagnostics;
    }

    private Token Current => _tokens[_index];

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
                _index++;
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
                if (error.ContinuesOnNextLine)
                    SkipContinuationLine();
            }
        }

        return statements;
    }

    private Statement ParseStatement()
    {
        if (IsDefinitionStart())
        {
            var name = Current;
            _index += 2;
            return new Definition(name.Position, name.Text, ParseExpression());
        }

        if (Current.Kind == TokenKind.Name && Current.Text == DefaultStatement.Keyword && IsNamedArgument(1))
        {
            var keyword = Current;
            _index++;
            return new DefaultStatement(keyword.Position, ParseNamedArgument());
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
        while (Current.BinaryOperator is { } op && op.Precedence >= minPrecedence)
        {
            var token = Current;
            _index++;
            if (Current.Kind == TokenKind.NewLine)
                throw DanglingOperator(Current);

            var right = ParseBinary(op.Precedence + 1);
            left = new BinaryExpression(token.Position, op, left, right);
        }

        return left;
    }

    private Expression ParseUnary()
    {
        var token = Current;
        if (token.UnaryOperator is not { } op)
            return ParsePostfix();

        _index++;

        if (op.SignsInteger && IsTouchingInteger(token, Current))
            return ParseInteger(Current, token, op.Symbol);

        return new UnaryExpression(token.Position, op, ParseUnary());
    }

    private static bool IsTouchingInteger(Token sign, Token next)
    {
        return next.Kind == TokenKind.Integer && next.Position.IsDirectlyAfter(sign.Position);
    }

    private Expression ParsePostfix()
    {
        var expression = ParsePrimary();
        while (Current.Kind == TokenKind.Dot)
        {
            var dot = Current;
            _index++;
            if (expression is not (NameExpression or CallExpression))
                throw Error(dot, "Only names and calls have outputs to pick with .");

            var output = Current;
            Expect(TokenKind.Name, "an output name");
            expression = new MemberExpression(output.Position, expression, output.Text);
        }

        return expression;
    }

    private Expression ParsePrimary()
    {
        var token = Current;
        switch (token.Kind)
        {
            case TokenKind.Integer:
                return ParseInteger(token, token, string.Empty);

            case TokenKind.String:
                _index++;
                return new StringLiteral(token.Position, token.Text);

            case TokenKind.OpenBracket:
                return ParseList();

            case TokenKind.Name:
                _index++;
                if (Current.Kind == TokenKind.OpenParen)
                    return ParseCall(token);

                return new NameExpression(token.Position, token.Text);

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
    /// <param name="start">Where the literal starts: the digits, or the sign before them.</param>
    /// <param name="sign">The sign's symbol, or empty.</param>
    private IntegerLiteral ParseInteger(Token digits, Token start, string sign)
    {
        _index++;
        var text = sign + digits.Text;
        if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            throw Error(start, $"{text} doesn't fit in a Signal");

        return new IntegerLiteral(start.Position, value);
    }

    private CallExpression ParseCall(Token function)
    {
        ExpectOpenParen();
        List<Expression> arguments = [];
        List<NamedArgument> namedArguments = [];
        if (Current.Kind == TokenKind.CloseParen)
        {
            _index++;
            return new CallExpression(function.Position, function.Text, arguments, namedArguments);
        }

        while (true)
        {
            if (IsNamedArgument(0))
            {
                namedArguments.Add(ParseNamedArgument());
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
                _index++;
                continue;
            }

            Expect(TokenKind.CloseParen, "',' or ')'");
            return new CallExpression(function.Position, function.Text, arguments, namedArguments);
        }
    }

    private ListExpression ParseList()
    {
        var open = Current;
        _index++;
        List<Expression> items = [];
        if (Current.Kind == TokenKind.CloseBracket)
        {
            _index++;
            return new ListExpression(open.Position, items);
        }

        while (true)
        {
            items.Add(ParseExpression());
            if (Current.Kind == TokenKind.Comma)
            {
                _index++;
                continue;
            }

            Expect(TokenKind.CloseBracket, "',' or ']'");
            return new ListExpression(open.Position, items);
        }
    }

    private bool IsDefinitionStart()
    {
        return Current.Kind == TokenKind.Name && Peek(1).Kind == TokenKind.Equals;
    }

    private bool IsNamedArgument(int offset)
    {
        return Peek(offset).Kind == TokenKind.Name && Peek(offset + 1).Kind == TokenKind.Colon;
    }

    private NamedArgument ParseNamedArgument()
    {
        var name = Current;
        _index += 2;
        var valueStart = Current;
        var value = ParseExpression();
        return new NamedArgument(name.Position, name.Text, value, valueStart.Position);
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

        _index++;
    }

    private Token Peek(int offset)
    {
        return _tokens[Math.Min(_index + offset, _tokens.Count - 1)];
    }

    private void SkipLine()
    {
        while (Current.Kind != TokenKind.NewLine && Current.Kind != TokenKind.End)
        {
            _index++;
        }
    }

    /// <summary>
    /// Skips the line after a statement that broke off mid-expression, since it most likely holds the rest of that
    /// expression and would only add a second error. A line that starts a definition is kept.
    /// </summary>
    private void SkipContinuationLine()
    {
        if (Current.Kind != TokenKind.NewLine)
            return;

        _index++;
        if (IsDefinitionStart())
            return;

        SkipLine();
    }

    private static string Describe(Token token)
    {
        return token.Kind switch
        {
            TokenKind.NewLine => "the end of the line",
            TokenKind.End => "the end of the Script",
            TokenKind.String => $"\"{token.Text}\"",
            _ => $"'{token.Text}'"
        };
    }

    private static SyntaxError Error(Token token, string message)
    {
        return new SyntaxError(new Diagnostic(token.Position, message), false);
    }

    /// <param name="lineEnd">The line break right after a binary operator.</param>
    private static SyntaxError DanglingOperator(Token lineEnd)
    {
        var message = "Expected a value, found the end of the line; to continue on the next line, put the expression in ( )";
        return new SyntaxError(new Diagnostic(lineEnd.Position, message), true);
    }
}
