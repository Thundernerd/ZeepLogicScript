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
        string dottedName,
        string dottedCallee,
        string dottedMode,
        string settingName,
        IReadOnlyList<string> settingValues,
        IReadOnlyCollection<string> givenArguments,
        int positionalArguments,
        bool isStatementStart,
        IReadOnlyList<DefinedFunction> functions)
    {
        Text = text;
        Caret = caret;
        Kind = kind;
        Word = word;
        StatementStart = statementStart;
        CanReferAhead = canReferAhead;
        Callee = callee;
        DottedName = dottedName;
        DottedCallee = dottedCallee;
        DottedMode = dottedMode;
        SettingName = settingName;
        SettingValues = settingValues;
        GivenArguments = givenArguments;
        PositionalArguments = positionalArguments;
        IsStatementStart = isStatementStart;
        Functions = functions;
        EnclosingFunction = functions.FirstOrDefault(f => f.Contains(caret));
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
    /// The name before the dot whose output the word is, for <see cref="CaretContextKind.Output"/>; null otherwise, and
    /// after a call.
    /// </summary>
    public string DottedName { get; }

    /// <summary>
    /// The function called by the call before the dot whose output the word is, for <see cref="CaretContextKind.Output"/>;
    /// null otherwise, and after a name.
    /// </summary>
    public string DottedCallee { get; }

    /// <summary>
    /// The option written for the mode Setting of the <see cref="DottedCallee"/>'s call (<c>mode: local_velocity</c>);
    /// null when none is written, or the call isn't to a Block Function.
    /// </summary>
    public string DottedMode { get; }

    /// <summary>
    /// The Setting whose value the word is, for <see cref="CaretContextKind.SettingValue"/>; null otherwise.
    /// </summary>
    public string SettingName { get; }

    /// <summary>
    /// The words the <see cref="SettingName"/> Setting takes, in the order it stores them; empty for a number.
    /// </summary>
    public IReadOnlyList<string> SettingValues { get; }

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

    /// <summary>
    /// Whether the word is the first on its line, where <c>function</c> and <c>end</c> go.
    /// </summary>
    public bool IsStatementStart { get; }

    /// <summary>
    /// The Functions the text defines, which can be called from anywhere in it; one without a name only gives its body
    /// a scope.
    /// </summary>
    public IReadOnlyList<DefinedFunction> Functions { get; }

    /// <summary>
    /// The Function whose body the caret is in; null at the top level.
    /// </summary>
    public DefinedFunction EnclosingFunction { get; }

    public static CaretContext At(string text, int caret)
    {
        text ??= string.Empty;
        caret = Math.Max(0, Math.Min(caret, text.Length));
        var word = FindWord(text, caret);
        List<TextSpan> comments = [];
        var tokens = Lexer.Tokenize(text, [], comments);
        var call = OpenCall.FindStartedAt(tokens, word.Start);
        var settingValues = FindSettingValues(tokens, word, out var settingCallee, out var settingName);
        var dotted = FindDotted(tokens, word, out var isDottedCall);
        CaretContextKind kind;
        if (IsInCommentOrString(tokens, comments, caret))
            kind = CaretContextKind.None;
        else if (settingValues != null)
            kind = CaretContextKind.SettingValue;
        else if (word.Length > 0 && Lexer.IsDigit(text[word.Start]))
            kind = CaretContextKind.None;
        else if (IsOnFunctionsFirstLine(tokens, word))
            kind = CaretContextKind.None;
        else if (dotted >= 0)
            kind = CaretContextKind.Output;
        else
            kind = GetKind(tokens, word, call != null);

        var statementStart = FindStatementStart(tokens, word, out var canReferAhead);
        var inCall = kind is CaretContextKind.Argument or CaretContextKind.ArgumentName;
        var callee = inCall ? call.Callee : null;
        var positionalArguments = 0;
        var isOutput = kind == CaretContextKind.Output;
        var dottedName = isOutput && !isDottedCall ? tokens[dotted].Text : null;
        var dottedCallee = isOutput && isDottedCall ? tokens[dotted].Text : null;
        var dottedMode = dottedCallee != null && BlockManifest.TryGetFunction(dottedCallee, out var type)
            ? DefinedNames.FindWrittenMode(tokens, dotted + 1, type)
            : null;

        IReadOnlyCollection<string> givenArguments = inCall ? FindGivenArguments(call, word, out positionalArguments) : [];
        if (kind == CaretContextKind.SettingValue)
        {
            callee = settingCallee;
        }
        else
        {
            settingName = null;
            settingValues = null;
        }

        return new CaretContext(
            text,
            caret,
            kind,
            word,
            statementStart,
            canReferAhead,
            callee,
            dottedName,
            dottedCallee,
            dottedMode,
            settingName,
            settingValues,
            givenArguments,
            positionalArguments,
            IsFirstOnLine(tokens, word),
            DefinedNames.ReadFunctions(tokens).Select(f => f.Function).ToList());
    }

    private static TextSpan FindWord(string text, int caret)
    {
        var start = caret;
        while (start > 0 && ScriptText.IsNamePart(text[start - 1]))
        {
            start--;
        }

        var end = caret;
        while (end < text.Length && ScriptText.IsNamePart(text[end]))
        {
            end++;
        }

        return new TextSpan(start, end - start);
    }

    private static bool IsFirstOnLine(List<Token> tokens, TextSpan word)
    {
        var before = tokens.FindLastIndex(t => t.Span.Start < word.Start);
        return before < 0 || tokens[before].Kind == TokenKind.NewLine;
    }

    /// <summary>
    /// Its name, inputs and outputs are new names, so there's nothing to complete there.
    /// </summary>
    private static bool IsOnFunctionsFirstLine(List<Token> tokens, TextSpan word)
    {
        var lineStart = tokens.FindLastIndex(t => t.Span.Start < word.Start && t.Kind == TokenKind.NewLine) + 1;
        return tokens[lineStart].Span.Start < word.Start && DefinedNames.IsFunctionStart(tokens, lineStart);
    }

    /// <summary>
    /// The token of the name the word picks an output of, or of the function whose call it does: right before a dot
    /// that's right before the word, and not itself after a dot, since only a name or a call has outputs. -1 when
    /// there's none.
    /// </summary>
    private static int FindDotted(List<Token> tokens, TextSpan word, out bool isCall)
    {
        isCall = false;
        var dot = tokens.FindLastIndex(t => t.Span.Start < word.Start);
        if (dot < 1 || tokens[dot].Kind != TokenKind.Dot || tokens[dot].Span.End != word.Start
            || tokens[dot - 1].Span.End != tokens[dot].Span.Start)
            return -1;

        isCall = tokens[dot - 1].Kind == TokenKind.CloseParen;
        var name = isCall ? FindOpenParen(tokens, dot - 1) - 1 : dot - 1;
        if (name < 0 || tokens[name].Kind != TokenKind.Name)
            return -1;

        return name >= 1 && tokens[name - 1].Kind == TokenKind.Dot ? -1 : name;
    }

    /// <summary>
    /// The opening parenthesis the one at <paramref name="closeParen"/> closes, past the brackets in between; -1 when
    /// nothing opens it, or a bracket does.
    /// </summary>
    private static int FindOpenParen(List<Token> tokens, int closeParen)
    {
        var open = BracketWalk.FindOpen(tokens, closeParen);
        return open >= 0 && tokens[open].Kind == TokenKind.OpenParen ? open : -1;
    }

    /// <summary>
    /// The caret is inside a comment or string from just after its first character, and stays inside a string up to
    /// its closing quote.
    /// </summary>
    internal static bool IsInCommentOrString(List<Token> tokens, List<TextSpan> comments, int caret)
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
    /// The words the Setting takes when the word comes right after its colon: in a Block Function's call, or
    /// <c>hide:</c> after <c>default</c> at the start of a statement. Null anywhere else, and after an input or a
    /// Setting that takes placed blocks, whose value is an expression.
    /// </summary>
    private static IReadOnlyList<string> FindSettingValues(List<Token> tokens, TextSpan word, out string callee, out string settingName)
    {
        callee = null;
        settingName = null;
        // A call left open while it's typed carries on to the next line, so its value can be there
        var colon = tokens.FindLastIndex(t => t.Span.Start < word.Start && t.Kind != TokenKind.NewLine);
        var valueOnNextLine = colon >= 0 && colon + 1 < tokens.Count && tokens[colon + 1].Kind == TokenKind.NewLine
            && tokens[colon + 1].Span.Start < word.Start;
        if (colon < 1 || tokens[colon].Kind != TokenKind.Colon || tokens[colon - 1].Kind != TokenKind.Name)
            return null;

        var name = tokens[colon - 1].Text;
        if (colon >= 2
            && tokens[colon - 2].Kind == TokenKind.Name
            && tokens[colon - 2].Text == DefaultStatement.Keyword
            && (colon == 2 || tokens[colon - 3].Kind == TokenKind.NewLine))
        {
            if (valueOnNextLine)
                return null;


            settingName = name;
            return name == BlockSetting.Hide.Name ? BlockSetting.Hide.Values : null;
        }

        var call = OpenCall.FindStartedAt(tokens, tokens[colon - 1].Span.Start);
        if (call == null || !BlockManifest.TryGetFunction(call.Callee, out var type))
            return null;

        callee = call.Callee;
        settingName = name;
        return type.GetSettingValues(name);
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
                // A call left open while it's typed carries on to the next line, unless a statement starts there
                case TokenKind.NewLine when delayCalls.Count == 0 || Lexer.IsStatementStart(tokens, i + 1):
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
    /// The names of the call's own named arguments, and how many positional arguments it has, apart from the one the
    /// word starts.
    /// </summary>
    private static HashSet<string> FindGivenArguments(OpenCall call, TextSpan word, out int positionalCount)
    {
        HashSet<string> names = [];
        positionalCount = 0;
        var passedWord = false;
        foreach (var argument in call.Arguments.Where(a => !a.IsEmpty))
        {
            // An argument after the caret on the next line has no comma before it yet, so only one that starts right at
            // the caret is the word's own
            var start = argument.FirstTokenStart.Value;
            var isWords = !passedWord && start == word.Start;
            passedWord |= start >= word.Start;
            if (isWords)
                continue;

            if (argument.IsNamed)
                names.Add(argument.Name);
            else
                positionalCount++;
        }

        return names;
    }
}
