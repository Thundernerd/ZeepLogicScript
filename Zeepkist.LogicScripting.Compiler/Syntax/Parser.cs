using System;
using System.Collections.Generic;
using System.Globalization;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// Recursive-descent parser for one statement per line, and for Functions, which wrap lines of their own. A syntax error
/// skips the rest of its line, so every line still gets checked.
/// </summary>
internal sealed partial class Parser
{
    /// <summary>
    /// How deep an expression may nest, counting operators, calls, lists and parentheses; far more than a Script needs,
    /// and shallow enough that parsing and compiling it can't overflow the stack.
    /// </summary>
    public const int MaxDepth = 50;

    private readonly List<Token> _tokens;
    private readonly List<Diagnostic> _diagnostics;
    private int _index;
    private int _depth;

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
            }
            else if (IsFunctionStart())
            {
                var function = ParseFunction();
                if (function != null)
                    statements.Add(function);
            }
            else if (IsEndLine())
            {
                _diagnostics.Add(new Diagnostic(Current.Position, $"'{FunctionDefinition.EndKeyword}' has no function to close"));
                _index++;
            }
            else
            {
                ParseLine(statements);
            }
        }

        return statements;
    }

    private void ParseLine(List<Statement> statements)
    {
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

    /// <summary>
    /// A Function's first line, its body and its <c>end</c>; null when the first line has no name to call it by.
    /// Indentation is only style, so the body ends at the first <c>end</c> line. A Function can't be defined inside
    /// another, so a <c>function</c> line before it ends this one, to be read as the next Function.
    /// </summary>
    private FunctionDefinition ParseFunction()
    {
        var keyword = Current;
        _index++;
        FunctionParameter name = null;
        List<FunctionParameter> inputs = [];
        List<FunctionParameter> outputs = [];
        var isComplete = true;
        try
        {
            name = ParseParameter("the function's name");
            ExpectOpenParen();
            if (Current.Kind == TokenKind.CloseParen)
                _index++;
            else
                ParseParameters(inputs, "an input name", TokenKind.CloseParen, "',' or ')'");

            if (Current.Kind == TokenKind.Arrow)
            {
                _index++;
                ParseParameters(outputs, "an output name", null, "',' or the end of the line");
            }
            else if (Current.Kind != TokenKind.NewLine && Current.Kind != TokenKind.End)
            {
                throw Error(Current, $"Expected '->' or the end of the line, found {Describe(Current)}");
            }
        }
        catch (SyntaxError error)
        {
            _diagnostics.Add(error.Diagnostic);
            SkipLine();
            isComplete = false;
        }

        var shownName = name == null ? "This function" : $"Function '{name.Name}'";
        List<Statement> body = [];
        while (true)
        {
            if (Current.Kind == TokenKind.End)
            {
                _diagnostics.Add(new Diagnostic(keyword.Position, $"{shownName} has no {FunctionDefinition.EndKeyword}"));
                isComplete = false;
                break;
            }

            if (Current.Kind == TokenKind.NewLine)
            {
                _index++;
            }
            else if (IsFunctionStart())
            {
                var message = $"{shownName} has no {FunctionDefinition.EndKeyword} before this one; functions can't be defined inside each other";
                _diagnostics.Add(new Diagnostic(Current.Position, message));
                isComplete = false;
                break;
            }
            else if (IsEndLine())
            {
                _index++;
                break;
            }
            else
            {
                ParseLine(body);
            }
        }

        return name == null ? null : new FunctionDefinition(keyword.Position, name, inputs, outputs, body, isComplete);
    }

    /// <summary>
    /// Names separated by commas, up to <paramref name="close"/>, or to the end of the line when it's null.
    /// </summary>
    private void ParseParameters(List<FunctionParameter> parameters, string expected, TokenKind? close, string expectedAfter)
    {
        while (true)
        {
            parameters.Add(ParseParameter(expected));
            if (Current.Kind == TokenKind.Comma)
            {
                _index++;
                continue;
            }

            if (close == null)
            {
                if (Current.Kind != TokenKind.NewLine && Current.Kind != TokenKind.End)
                    throw Error(Current, $"Expected {expectedAfter}, found {Describe(Current)}");

                return;
            }

            Expect(close.Value, expectedAfter);
            return;
        }
    }

    private FunctionParameter ParseParameter(string expected)
    {
        var token = Current;
        Expect(TokenKind.Name, expected);
        return new FunctionParameter(token.Position, token.Text);
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
            left = CheckDepth(new BinaryExpression(token.Position, op, left, right), token);
        }

        return left;
    }

    private Expression ParseUnary()
    {
        var token = Current;
        if (++_depth > MaxDepth)
            throw TooDeep(token);

        try
        {
            return CheckDepth(ParseUnaryUnchecked(), token);
        }
        finally
        {
            _depth--;
        }
    }

    private Expression ParseUnaryUnchecked()
    {
        var token = Current;
        if (token.UnaryOperator is not { } op)
            return ParsePostfix();

        _index++;

        if (op.SignsInteger && IsTouchingInteger(token, Current))
            return ParseInteger(Current, token, op.Symbol);

        return new UnaryExpression(token.Position, op, ParseUnary());
    }

    /// <summary>
    /// The parser's own recursion only bounds how deep the parentheses go; a long run of operators builds a deep
    /// expression in a loop.
    /// </summary>
    private static Expression CheckDepth(Expression expression, Token token)
    {
        if (expression.Depth > MaxDepth)
            throw TooDeep(token);

        return expression;
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
            expression = expression is NameExpression library && Current.Kind == TokenKind.OpenParen
                ? ParseCall(library, output)
                : new MemberExpression(output.Position, expression, output.Text);
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
                    return ParseCall(null, token);

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

    /// <summary>
    /// <paramref name="library"/> is the name before the dot of a qualified call, <c>timing.edge(x)</c>, or null.
    /// </summary>
    private CallExpression ParseCall(NameExpression library, Token function)
    {
        var position = library?.Position ?? function.Position;
        ExpectOpenParen();
        List<Expression> arguments = [];
        List<NamedArgument> namedArguments = [];
        if (Current.Kind == TokenKind.CloseParen)
        {
            _index++;
            return new CallExpression(position, library?.Name, function.Text, arguments, namedArguments);
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
            return new CallExpression(position, library?.Name, function.Text, arguments, namedArguments);
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

    /// <summary>
    /// <c>function = ...</c> is a definition, so it gets the reserved-word error instead.
    /// </summary>
    private bool IsFunctionStart()
    {
        return Current.Kind == TokenKind.Name && Current.Text == FunctionDefinition.Keyword && Peek(1).Kind != TokenKind.Equals;
    }

    private bool IsEndLine()
    {
        return Current.Kind == TokenKind.Name
            && Current.Text == FunctionDefinition.EndKeyword
            && Peek(1).Kind is TokenKind.NewLine or TokenKind.End;
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
    /// expression and would only add a second error. A line that starts a definition or a Function, or ends one, is
    /// kept.
    /// </summary>
    private void SkipContinuationLine()
    {
        if (Current.Kind != TokenKind.NewLine)
            return;

        _index++;
        if (IsDefinitionStart() || IsFunctionStart() || IsEndLine())
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

    private static SyntaxError TooDeep(Token token)
    {
        return Error(token, $"This nests more than {MaxDepth} deep; split it into definitions");
    }

    /// <param name="lineEnd">The line break right after a binary operator.</param>
    private static SyntaxError DanglingOperator(Token lineEnd)
    {
        var message = "Expected a value, found the end of the line; to continue on the next line, put the expression in ( )";
        return new SyntaxError(new Diagnostic(lineEnd.Position, message), true);
    }
}
