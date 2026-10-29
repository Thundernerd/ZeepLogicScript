using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// What is being typed at the caret. Read from the tokens around it rather than the syntax tree, which drops a
/// half-typed line.
/// </summary>
public sealed class CaretContext
{
    private CaretContext(string text, int caret, CaretContextKind kind, TextSpan word, int statementStart, bool canReferAhead)
    {
        Text = text;
        Caret = caret;
        Kind = kind;
        Word = word;
        StatementStart = statementStart;
        CanReferAhead = canReferAhead;
    }

    public string Text { get; }
    public int Caret { get; }
    public CaretContextKind Kind { get; }

    /// <summary>
    /// The name the caret is in or right after, which accepting a completion replaces; empty at the caret when none is
    /// typed yet.
    /// </summary>
    public TextSpan Word { get; }

    /// <summary>
    /// The part of <see cref="Word"/> before the caret, which completions are matched against.
    /// </summary>
    public string Prefix => Text.Substring(Word.Start, Caret - Word.Start);

    /// <summary>
    /// Where the statement the word is in starts; the names defined before it can be used there.
    /// </summary>
    public int StatementStart { get; }

    /// <summary>
    /// Whether the word is inside <c>delay(...)</c>, where a name may refer to a later definition or to the one being
    /// defined.
    /// </summary>
    public bool CanReferAhead { get; }

    public static CaretContext At(string text, int caret)
    {
        text ??= string.Empty;
        caret = Math.Max(0, Math.Min(caret, text.Length));
        var word = FindWord(text, caret);
        List<TextSpan> comments = [];
        var tokens = Lexer.Tokenize(text, [], comments);
        var kind = IsInCommentOrString(tokens, comments, caret) || (word.Length > 0 && Lexer.IsDigit(text[word.Start]))
            ? CaretContextKind.None
            : GetKind(tokens, word);
        var statementStart = FindStatementStart(tokens, word, out var canReferAhead);

        return new CaretContext(text, caret, kind, word, statementStart, canReferAhead);
    }

    private static TextSpan FindWord(string text, int caret)
    {
        var start = caret;
        while (start > 0 && IsNameCharacter(text[start - 1]))
        {
            start--;
        }

        var end = caret;
        while (end < text.Length && IsNameCharacter(text[end]))
        {
            end++;
        }

        return new TextSpan(start, end - start);
    }

    /// <summary>
    /// The caret is inside a comment or string from just after its first character, and stays inside a string up to
    /// its closing quote.
    /// </summary>
    private static bool IsInCommentOrString(List<Token> tokens, List<TextSpan> comments, int caret)
    {
        return comments.Any(c => c.Start < caret && caret <= c.End)
            || tokens.Any(t => t.Kind == TokenKind.String && t.Span.Start < caret && caret <= t.Span.Start + 1 + t.Text.Length);
    }

    /// <summary>
    /// An expression starts at the start of a statement or after anything that takes a value next: <c>=</c>, an
    /// operator, an opening parenthesis or bracket, a comma or a named argument's colon. A name at the start of a
    /// statement followed by <c>=</c> is being defined, and one followed by <c>:</c> names an argument.
    /// </summary>
    private static CaretContextKind GetKind(List<Token> tokens, TextSpan word)
    {
        Token? before = null;
        Token? after = null;
        foreach (var token in tokens)
        {
            var start = token.Span.Start;
            if (start < word.Start)
            {
                before = token;
            }
            else if (start >= word.End && token.Kind != TokenKind.End)
            {
                after = token;
                break;
            }
        }

        var statementStart = before == null || before.Value.Kind == TokenKind.NewLine;
        if (after?.Kind == TokenKind.Colon || (statementStart && after?.Kind == TokenKind.Equals))
            return CaretContextKind.None;

        if (statementStart || before.Value.Kind is TokenKind.Equals or TokenKind.Operator or TokenKind.OpenParen
                or TokenKind.OpenBracket or TokenKind.Comma or TokenKind.Colon)
            return CaretContextKind.Expression;

        return CaretContextKind.None;
    }

    /// <summary>
    /// Follows the parentheses opened in the word's statement before it, to tell whether one of them is a Delay's
    /// call.
    /// </summary>
    private static int FindStatementStart(List<Token> tokens, TextSpan word, out bool isInDelay)
    {
        var statementStart = 0;
        Stack<bool> delayCalls = new();
        for (var i = 0; i < tokens.Count && tokens[i].Span.Start < word.Start; i++)
        {
            switch (tokens[i].Kind)
            {
                // A call left open while it's typed carries on to the next line, unless a definition starts there
                case TokenKind.NewLine when delayCalls.Count == 0 || IsDefinitionStart(tokens, i + 1):
                    statementStart = tokens[i].Span.End;
                    delayCalls.Clear();
                    break;

                case TokenKind.OpenParen:
                    delayCalls.Push(i > 0
                        && tokens[i - 1].Kind == TokenKind.Name
                        && BlockManifest.TryGetFunction(tokens[i - 1].Text, out var type)
                        && type == BlockManifest.Delay);
                    break;

                case TokenKind.CloseParen when delayCalls.Count > 0:
                    delayCalls.Pop();
                    break;
            }
        }

        isInDelay = delayCalls.Contains(true);
        return statementStart;
    }

    private static bool IsDefinitionStart(List<Token> tokens, int index)
    {
        return index + 1 < tokens.Count && tokens[index].Kind == TokenKind.Name && tokens[index + 1].Kind == TokenKind.Equals;
    }

    private static bool IsNameCharacter(char c)
    {
        return Lexer.IsNameStart(c) || Lexer.IsDigit(c);
    }
}
