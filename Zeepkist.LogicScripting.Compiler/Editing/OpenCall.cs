using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// A call a position in the text is inside, and its arguments, which completion and the call's help both read. Read
/// from the tokens, so a half-typed call has one too: a call left open while it's typed carries on to the next line,
/// unless a statement starts there.
/// </summary>
internal sealed class OpenCall
{
    private readonly List<Token> _tokens;
    private List<CallArgument> _arguments;

    private OpenCall(List<Token> tokens, int paren)
    {
        _tokens = tokens;
        Paren = paren;
    }

    /// <summary>
    /// The token index of its opening parenthesis.
    /// </summary>
    public int Paren { get; }

    /// <summary>
    /// The function or name called, as written.
    /// </summary>
    public string Callee => _tokens[Paren - 1].Text;

    /// <summary>
    /// Its arguments up to its closing parenthesis, or while it's typed, the next statement.
    /// </summary>
    public IReadOnlyList<CallArgument> Arguments => _arguments ??= ReadArguments();

    /// <summary>
    /// The calls the position is inside, innermost first.
    /// </summary>
    public static List<OpenCall> FindAll(List<Token> tokens, int position)
    {
        return FindOpen(tokens, position, out _).Where(i => IsCall(tokens, i)).Select(i => new OpenCall(tokens, i)).ToList();
    }

    /// <summary>
    /// The call the position starts an argument of, right after its opening parenthesis or after a comma between its
    /// arguments; null when it doesn't start one.
    /// </summary>
    public static OpenCall FindStartedAt(List<Token> tokens, int position)
    {
        var open = FindOpen(tokens, position, out var previous);
        if (open.Count == 0)
            return null;

        var call = open[0];
        return IsCall(tokens, call) && (previous == call || tokens[previous].Kind == TokenKind.Comma) ? new OpenCall(tokens, call) : null;
    }

    /// <summary>
    /// The parentheses and brackets opened before the position that are still open there, innermost first, and the
    /// last token before it that isn't a line break the call carries on over.
    /// </summary>
    private static List<int> FindOpen(List<Token> tokens, int position, out int previous)
    {
        Stack<int> open = new();
        previous = -1;
        for (var i = 0; i < tokens.Count && tokens[i].Span.Start < position; i++)
        {
            switch (tokens[i].Kind)
            {
                case TokenKind.NewLine when open.Count == 0 || Lexer.IsStatementStart(tokens, i + 1):
                    open.Clear();
                    break;

                case TokenKind.NewLine:
                    continue;

                case TokenKind.OpenParen or TokenKind.OpenBracket:
                    open.Push(i);
                    break;

                case TokenKind.CloseParen or TokenKind.CloseBracket when open.Count > 0:
                    open.Pop();
                    break;
            }

            previous = i;
        }

        return open.ToList();
    }

    private static bool IsCall(List<Token> tokens, int open)
    {
        return tokens[open].Kind == TokenKind.OpenParen && open > 0 && tokens[open - 1].Kind == TokenKind.Name;
    }

    private List<CallArgument> ReadArguments()
    {
        List<CallArgument> arguments = [];
        var depth = 0;
        var start = -1;
        for (var i = Paren; _tokens[i].Kind != TokenKind.End; i++)
        {
            var token = _tokens[i];
            if (token.Kind == TokenKind.NewLine)
            {
                if (Lexer.IsStatementStart(_tokens, i + 1))
                    break;

                continue;
            }

            if (start >= 0)
            {
                arguments.Add(ReadArgument(start, i));
                start = -1;
            }

            switch (token.Kind)
            {
                case TokenKind.OpenParen or TokenKind.OpenBracket:
                    depth++;
                    if (depth == 1)
                        start = i;

                    break;

                case TokenKind.CloseParen or TokenKind.CloseBracket:
                    depth--;
                    if (depth == 0)
                        return arguments;

                    break;

                case TokenKind.Comma when depth == 1:
                    start = i;
                    break;
            }
        }

        if (start >= 0)
            arguments.Add(new CallArgument(start, -1, null));

        return arguments;
    }

    private CallArgument ReadArgument(int start, int first)
    {
        if (_tokens[first].Kind is TokenKind.Comma or TokenKind.CloseParen)
            return new CallArgument(start, -1, null);

        var isNamed = _tokens[first].Kind == TokenKind.Name && _tokens[first + 1].Kind == TokenKind.Colon;
        return new CallArgument(start, first, isNamed ? _tokens[first].Text : null);
    }
}
