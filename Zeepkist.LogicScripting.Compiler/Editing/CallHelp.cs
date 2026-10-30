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
    /// The innermost call around the caret whose function is a Block Function or one of the Script's own Functions with
    /// inputs, or whose name is a Placed Block Reference to a block with inputs; null when there's none, and inside a comment or string. A named argument is
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

        var functions = DefinedNames.ReadFunctions(tokens).Select(f => f.Function).ToList();
        var calls = OpenCall.FindAll(tokens, caret);
        var enclosingCalls = calls.Select(c => tokens[c.Paren].Span.Start).ToList();
        foreach (var call in calls)
        {
            var openParen = tokens[call.Paren].Span.Start;
            var items = GetItems(text, call.Callee, openParen, functions, findPlacedBlock);
            if (items.Count > 0)
                return new CallHelp(call.Callee, openParen, items, FindActive(text, tokens, call, caret, items), enclosingCalls);
        }

        return null;
    }

    /// <summary>
    /// A Block Function's inputs, Settings, <c>style:</c> and <c>hide:</c>, a Function's inputs, or a Placed Block
    /// Reference's inputs only, since its Settings can't be changed.
    /// </summary>
    private static List<CallHelpItem> GetItems(
        string text,
        string callee,
        int openParen,
        List<DefinedFunction> functions,
        Func<string, PlacedBlock> findPlacedBlock)
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

        var function = functions.FirstOrDefault(f => f.Name != null && f.Name == callee);
        if (function != null)
            return function.Inputs.Select(InputItem).ToList();

        var placed = DefinedNames.Read(text, findPlacedBlock)
            .FirstOrDefault(n => n.Name == callee && n.Span.Start < openParen && n.IsInScopeAt(openParen, functions));
        return placed is { IsPlaced: true, Type: not null } ? GetInputItems(placed.Type) : [];
    }

    private static List<CallHelpItem> GetInputItems(BlockType type)
    {
        return type.Inputs.Select(i => InputItem(i.Name)).ToList();
    }

    private static CallHelpItem InputItem(string name)
    {
        return new CallHelpItem(name, true, "input");
    }

    private static CallHelpItem SettingItem(BlockSetting setting)
    {
        return new CallHelpItem(setting.Name, false, BlockReference.DescribeValues(setting));
    }

    private static int FindActive(string text, List<Token> tokens, OpenCall call, int caret, List<CallHelpItem> items)
    {
        var before = call.Arguments.TakeWhile(a => tokens[a.Start].Span.Start < caret).ToList();
        var current = before[before.Count - 1];
        before.RemoveAt(before.Count - 1);

        // A name after the caret on a later line has no comma before it yet, so it starts an argument of its own
        if (current.IsNamed)
        {
            var start = tokens[current.First].Span.Start;
            var isLater = start > caret && text.IndexOf('\n', caret, start - caret) >= 0;
            if (!isLater)
                return items.FindIndex(i => i.Name == current.Name);
        }

        var positional = before.Count(a => !a.IsEmpty && !a.IsNamed);
        var hasNamed = before.Any(a => a.IsNamed);
        return !hasNamed && positional < items.Count && items[positional].IsInput ? positional : -1;
    }
}
