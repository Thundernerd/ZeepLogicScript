using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// The inputs and Settings of the call the caret is inside, and which of them the argument at the caret gives. Read
/// from the tokens around the caret, like <see cref="CaretContext"/>, so a half-typed call has it too.
/// </summary>
public sealed class CallHelp
{
    private CallHelp(string callee, int openParen, IReadOnlyList<CallHelpItem> items, int active, IReadOnlyList<int> enclosingCalls)
    {
        Callee = callee;
        OpenParen = openParen;
        Items = items;
        Active = active;
        EnclosingCalls = enclosingCalls;
    }

    /// <summary>
    /// The function or name called, as written.
    /// </summary>
    public string Callee { get; }

    /// <summary>
    /// Where the call's opening parenthesis is in the text, which tells one call from another.
    /// </summary>
    public int OpenParen { get; }

    /// <summary>
    /// The inputs in the order positional arguments fill them, then the Settings, in the Reference tab's order.
    /// </summary>
    public IReadOnlyList<CallHelpItem> Items { get; }

    /// <summary>
    /// The index in <see cref="Items"/> of what the argument at the caret gives; -1 when it gives none of them, such as
    /// a positional argument past the last input.
    /// </summary>
    public int Active { get; }

    /// <summary>
    /// The opening parentheses of every call the caret is inside, innermost first, with or without help of its own.
    /// </summary>
    public IReadOnlyList<int> EnclosingCalls { get; }

    /// <summary>
    /// The innermost call around the caret whose function is a Block Function, or whose name is a Placed Block
    /// Reference to a block with inputs; null when there's none, and inside a comment or string. A named argument is
    /// active from its name on; any other argument gives the input after those the positional arguments before it give,
    /// unless a named argument comes before it, which a positional one can't follow.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="caret"></param>
    /// <param name="findPlacedBlock">
    /// The block in the level with a UID, or null when there's none; it gives a Placed Block Reference its type, and is
    /// only asked when the call isn't to a Block Function.
    /// </param>
    public static CallHelp At(string text, int caret, Func<string, PlacedBlock> findPlacedBlock = null)
    {
        text ??= string.Empty;
        caret = Math.Max(0, Math.Min(caret, text.Length));
        List<TextSpan> comments = [];
        var tokens = Lexer.Tokenize(text, [], comments);
        if (CaretContext.IsInCommentOrString(tokens, comments, caret))
            return null;

        var calls = FindCallParens(tokens, caret);
        var enclosingCalls = calls.Select(c => tokens[c].Span.Start).ToList();
        foreach (var call in calls)
        {
            var callee = tokens[call - 1].Text;
            var openParen = tokens[call].Span.Start;
            var items = GetItems(text, callee, openParen, findPlacedBlock);
            if (items.Count > 0)
                return new CallHelp(callee, openParen, items, FindActive(text, tokens, call, caret, items), enclosingCalls);
        }

        return null;
    }

    /// <summary>
    /// The opening parentheses before the caret that are still open and follow a name, innermost first.
    /// </summary>
    private static List<int> FindCallParens(List<Token> tokens, int caret)
    {
        Stack<int> open = new();
        for (var i = 0; i < tokens.Count && tokens[i].Span.Start < caret; i++)
        {
            switch (tokens[i].Kind)
            {
                // A call left open while it's typed carries on to the next line, unless a statement starts there
                case TokenKind.NewLine when open.Count == 0 || Lexer.IsStatementStart(tokens, i + 1):
                    open.Clear();
                    break;

                case TokenKind.OpenParen or TokenKind.OpenBracket:
                    open.Push(i);
                    break;

                case TokenKind.CloseParen or TokenKind.CloseBracket when open.Count > 0:
                    open.Pop();
                    break;
            }
        }

        return open.Where(i => tokens[i].Kind == TokenKind.OpenParen && i > 0 && tokens[i - 1].Kind == TokenKind.Name).ToList();
    }

    /// <summary>
    /// A Block Function's inputs, Settings, <c>style:</c> and <c>hide:</c>, or a Placed Block Reference's inputs only,
    /// since its Settings can't be changed.
    /// </summary>
    private static List<CallHelpItem> GetItems(string text, string callee, int openParen, Func<string, PlacedBlock> findPlacedBlock)
    {
        if (BlockManifest.TryGetFunction(callee, out var type))
        {
            var items = GetInputItems(type);
            items.AddRange(type.Settings.Select(SettingItem));
            if (type.HasStyles)
                items.Add(new CallHelpItem(BlockType.StyleName, false, string.Join(", ", type.Styles.Select(s => s.Name))));

            items.Add(SettingItem(BlockSetting.Hide));
            return items;
        }

        var placed = DefinedNames.Read(text, findPlacedBlock).LastOrDefault(n => n.Name == callee && n.Span.Start < openParen);
        return placed is { IsPlaced: true, Type: not null } ? GetInputItems(placed.Type) : [];
    }

    private static List<CallHelpItem> GetInputItems(BlockType type)
    {
        return type.Inputs.Select(i => new CallHelpItem(i.Name, true, "input")).ToList();
    }

    private static CallHelpItem SettingItem(BlockSetting setting)
    {
        return new CallHelpItem(setting.Name, false, BlockReference.DescribeValues(setting));
    }

    private static int FindActive(string text, List<Token> tokens, int call, int caret, List<CallHelpItem> items)
    {
        var positional = 0;
        var hasNamed = false;
        var argumentStart = call + 1;
        var depth = 0;
        for (var i = call + 1; tokens[i].Kind != TokenKind.End && tokens[i].Span.Start < caret; i++)
        {
            switch (tokens[i].Kind)
            {
                case TokenKind.OpenParen or TokenKind.OpenBracket:
                    depth++;
                    break;

                case TokenKind.CloseParen or TokenKind.CloseBracket:
                    depth--;
                    break;

                case TokenKind.Comma when depth == 0:
                    var first = SkipLineBreaks(tokens, argumentStart);
                    if (IsNamed(tokens, first))
                        hasNamed = true;
                    else if (tokens[first].Kind != TokenKind.Comma)
                        positional++;

                    argumentStart = i + 1;
                    break;
            }
        }

        // A name after the caret on a later line has no comma before it yet, so it starts an argument of its own
        var current = SkipLineBreaks(tokens, argumentStart);
        var start = tokens[current].Span.Start;
        var isLater = start > caret && text.IndexOf('\n', caret, start - caret) >= 0;
        if (!isLater && IsNamed(tokens, current))
        {
            var name = tokens[current].Text;
            return items.FindIndex(i => i.Name == name);
        }

        return !hasNamed && positional < items.Count && items[positional].IsInput ? positional : -1;
    }

    /// <summary>
    /// Only a call left open keeps its line breaks as tokens.
    /// </summary>
    private static int SkipLineBreaks(List<Token> tokens, int index)
    {
        while (tokens[index].Kind == TokenKind.NewLine)
        {
            index++;
        }

        return index;
    }

    private static bool IsNamed(List<Token> tokens, int index)
    {
        return tokens[index].Kind == TokenKind.Name && tokens[index + 1].Kind == TokenKind.Colon;
    }
}
