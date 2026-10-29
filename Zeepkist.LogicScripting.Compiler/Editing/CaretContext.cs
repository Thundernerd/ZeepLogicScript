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
    private CaretContext(
        string text,
        int caret,
        CaretContextKind kind,
        TextSpan word,
        int statementStart,
        bool canReferAhead,
        string callee,
        string settingName,
        IReadOnlyCollection<string> givenArguments,
        int positionalArguments)
    {
        Text = text;
        Caret = caret;
        Kind = kind;
        Word = word;
        StatementStart = statementStart;
        CanReferAhead = canReferAhead;
        Callee = callee;
        SettingName = settingName;
        GivenArguments = givenArguments;
        PositionalArguments = positionalArguments;
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

    /// <summary>
    /// The function or name called by the call whose argument the word is, for <see cref="CaretContextKind.Argument"/>
    /// and <see cref="CaretContextKind.ArgumentName"/>, or whose Setting the word is the value of, for
    /// <see cref="CaretContextKind.SettingValue"/>; null otherwise, and after <c>default</c>.
    /// </summary>
    public string Callee { get; }

    /// <summary>
    /// The Setting whose value the word is, for <see cref="CaretContextKind.SettingValue"/>; null otherwise.
    /// </summary>
    public string SettingName { get; }

    /// <summary>
    /// The named arguments the <see cref="Callee"/>'s call already has, before or after the word, but not the word
    /// itself.
    /// </summary>
    public IReadOnlyCollection<string> GivenArguments { get; }

    /// <summary>
    /// How many positional arguments the <see cref="Callee"/>'s call has, apart from the one the word starts; they
    /// give its first inputs.
    /// </summary>
    public int PositionalArguments { get; }

    public static CaretContext At(string text, int caret)
    {
        text ??= string.Empty;
        caret = Math.Max(0, Math.Min(caret, text.Length));
        var word = FindWord(text, caret);
        List<TextSpan> comments = [];
        var tokens = Lexer.Tokenize(text, [], comments);
        var call = FindArgumentCallParen(tokens, word);
        var isSettingValue = IsSettingValue(tokens, word, out var settingCallee, out var settingName);

        // A player can be a number
        var kind = IsInCommentOrString(tokens, comments, caret)
            || (word.Length > 0 && Lexer.IsDigit(text[word.Start]) && !isSettingValue)
            ? CaretContextKind.None
            : isSettingValue ? CaretContextKind.SettingValue : GetKind(tokens, word, call >= 0);
        var statementStart = FindStatementStart(tokens, word, out var canReferAhead);
        var inCall = kind is CaretContextKind.Argument or CaretContextKind.ArgumentName;
        var callee = inCall ? tokens[call - 1].Text : null;
        var positionalArguments = 0;
        IReadOnlyCollection<string> givenArguments = inCall ? FindGivenArguments(tokens, call, word, out positionalArguments) : [];
        if (kind == CaretContextKind.SettingValue)
            callee = settingCallee;
        else
            settingName = null;

        return new CaretContext(
            text,
            caret,
            kind,
            word,
            statementStart,
            canReferAhead,
            callee,
            settingName,
            givenArguments,
            positionalArguments);
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
    /// statement followed by <c>=</c> is being defined, and one followed by <c>:</c> names an argument. The name after
    /// <c>default</c> at the start of a statement is its Setting.
    /// </summary>
    private static CaretContextKind GetKind(List<Token> tokens, TextSpan word, bool startsArgument)
    {
        Token? twoBefore = null;
        Token? before = null;
        Token? after = null;
        foreach (var token in tokens)
        {
            var start = token.Span.Start;
            if (start < word.Start)
            {
                twoBefore = before;
                before = token;
            }
            else if (start >= word.End && token.Kind != TokenKind.End)
            {
                after = token;
                break;
            }
        }

        var statementStart = before == null || before.Value.Kind == TokenKind.NewLine;
        if (before?.Kind == TokenKind.Name
            && before.Value.Text == DefaultStatement.Keyword
            && (twoBefore == null || twoBefore.Value.Kind == TokenKind.NewLine))
            return CaretContextKind.DefaultSetting;

        if (after?.Kind == TokenKind.Colon)
            return startsArgument ? CaretContextKind.ArgumentName : CaretContextKind.None;

        if (statementStart && after?.Kind == TokenKind.Equals)
            return CaretContextKind.None;

        if (startsArgument)
            return CaretContextKind.Argument;

        if (statementStart || before.Value.Kind is TokenKind.Equals or TokenKind.Operator or TokenKind.OpenParen
                or TokenKind.OpenBracket or TokenKind.Comma or TokenKind.Colon)
            return CaretContextKind.Expression;

        return CaretContextKind.None;
    }

    /// <summary>
    /// Whether the word comes right after the colon of a Setting that takes a value it can list: one of a Block
    /// Function's call, or <c>hide:</c> after <c>default</c> at the start of a statement.
    /// </summary>
    private static bool IsSettingValue(List<Token> tokens, TextSpan word, out string callee, out string settingName)
    {
        callee = null;
        settingName = null;
        var colon = tokens.FindLastIndex(t => t.Span.Start < word.Start);
        if (colon < 1 || tokens[colon].Kind != TokenKind.Colon || tokens[colon - 1].Kind != TokenKind.Name)
            return false;

        var name = tokens[colon - 1];
        if (colon >= 2
            && tokens[colon - 2].Kind == TokenKind.Name
            && tokens[colon - 2].Text == DefaultStatement.Keyword
            && (colon == 2 || tokens[colon - 3].Kind == TokenKind.NewLine))
        {
            settingName = name.Text;
            return name.Text == BlockSetting.Hide.Name;
        }

        var call = FindArgumentCallParen(tokens, name.Span);
        if (call < 0 || !BlockManifest.TryGetFunction(tokens[call - 1].Text, out var type))
            return false;

        callee = tokens[call - 1].Text;
        settingName = name.Text;
        return (name.Text == BlockType.StyleName && type.HasStyles)
            || type.FindSetting(name.Text) is { Type: not SettingType.Targets };
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

    /// <summary>
    /// The opening parenthesis of the call the word starts an argument of, right after it or after a comma between
    /// its arguments; -1 when the word doesn't start one.
    /// </summary>
    private static int FindArgumentCallParen(List<Token> tokens, TextSpan word)
    {
        Stack<int> open = new();
        var previous = -1;
        for (var i = 0; i < tokens.Count && tokens[i].Span.Start < word.Start; i++)
        {
            switch (tokens[i].Kind)
            {
                case TokenKind.NewLine when open.Count == 0 || IsDefinitionStart(tokens, i + 1):
                    open.Clear();
                    break;

                // A call left open while it's typed carries on to the next line
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

        if (open.Count == 0)
            return -1;

        var call = open.Peek();
        var isCall = tokens[call].Kind == TokenKind.OpenParen && call > 0 && tokens[call - 1].Kind == TokenKind.Name;
        return isCall && (previous == call || tokens[previous].Kind == TokenKind.Comma) ? call : -1;
    }

    /// <summary>
    /// The names of the call's own named arguments, and how many positional arguments it has, apart from the one the
    /// word starts: up to its closing parenthesis, or while it's typed, the next definition.
    /// </summary>
    private static HashSet<string> FindGivenArguments(List<Token> tokens, int call, TextSpan word, out int positionalCount)
    {
        HashSet<string> names = [];
        positionalCount = 0;
        var depth = 0;
        var startsArgument = false;
        var passedWord = false;
        for (var i = call; tokens[i].Kind != TokenKind.End; i++)
        {
            var token = tokens[i];

            // A call left open while it's typed carries on to the next line
            if (token.Kind == TokenKind.NewLine)
            {
                if (IsDefinitionStart(tokens, i + 1))
                    break;

                continue;
            }

            if (startsArgument)
            {
                startsArgument = false;

                // An argument after the caret on the next line has no comma before it yet, so only one that starts
                // right at the caret is the word's own
                var isWords = !passedWord && token.Span.Start == word.Start;
                passedWord |= token.Span.Start >= word.Start;
                if (!isWords && token.Kind == TokenKind.Name && tokens[i + 1].Kind == TokenKind.Colon)
                    names.Add(token.Text);
                else if (!isWords && token.Kind is not (TokenKind.Comma or TokenKind.CloseParen))
                    positionalCount++;
            }

            switch (token.Kind)
            {
                case TokenKind.OpenParen or TokenKind.OpenBracket:
                    depth++;
                    startsArgument = depth == 1;
                    break;

                case TokenKind.CloseParen or TokenKind.CloseBracket:
                    depth--;
                    if (depth == 0)
                        return names;

                    break;

                case TokenKind.Comma when depth == 1:
                    startsArgument = true;
                    break;
            }
        }

        return names;
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
