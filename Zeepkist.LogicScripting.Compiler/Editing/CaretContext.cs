using System;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// What is being typed at the caret. Read from the tokens around it rather than the syntax tree, which drops a
/// half-typed line.
/// </summary>
public sealed class CaretContext
{
    private CaretContext(string text, int caret, CaretContextKind kind, TextSpan word)
    {
        Text = text;
        Caret = caret;
        Kind = kind;
        Word = word;
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

    public static CaretContext At(string text, int caret)
    {
        text ??= string.Empty;
        caret = Math.Max(0, Math.Min(caret, text.Length));
        var word = FindWord(text, caret);
        var kind = IsInCommentOrString(text, caret) || (word.Length > 0 && IsDigit(text[word.Start]))
            ? CaretContextKind.None
            : GetKind(text, word);

        return new CaretContext(text, caret, kind, word);
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
    /// Neither can span lines, so the caret's line is read from its start, as the lexer would.
    /// </summary>
    private static bool IsInCommentOrString(string text, int caret)
    {
        var inString = false;
        for (var i = caret > 0 ? text.LastIndexOf('\n', caret - 1) + 1 : 0; i < caret; i++)
        {
            if (text[i] == '"')
                inString = !inString;
            else if (text[i] == '#' && !inString)
                return true;
        }

        return inString;
    }

    /// <summary>
    /// An expression starts at the start of a statement or after anything that takes a value next: <c>=</c>, an
    /// operator, an opening parenthesis or bracket, a comma or a named argument's colon. A name at the start of a
    /// statement followed by <c>=</c> is being defined, and one followed by <c>:</c> names an argument.
    /// </summary>
    private static CaretContextKind GetKind(string text, TextSpan word)
    {
        var tokens = Lexer.Tokenize(text, []);
        var lineStarts = ScriptText.GetLineStarts(text);
        Token? before = null;
        Token? after = null;
        foreach (var token in tokens)
        {
            var start = ScriptText.GetIndex(lineStarts, text.Length, token.Position);
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

    private static bool IsNameCharacter(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || IsDigit(c);
    }

    private static bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }
}
