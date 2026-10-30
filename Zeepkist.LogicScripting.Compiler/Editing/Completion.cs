using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// The completions for what is being typed at the caret, and what accepting one does to the text.
/// </summary>
public static class Completion
{
    private static readonly IReadOnlyList<CompletionItem> _expressionItems = CreateExpressionItems();

    /// <summary>
    /// The items whose label starts with the typed prefix, then those that only contain it, each in alphabetical
    /// order, or for a Setting's values, in the order the Setting stores them; at the start of an argument, the named
    /// arguments come before the expressions. Case is ignored. A Number Setting lists nothing. Of the
    /// <paramref name="definedNames"/>, only those defined before the caret's statement are offered, unless the caret is
    /// inside <c>delay(...)</c>.
    /// </summary>
    public static IReadOnlyList<CompletionItem> GetItems(CaretContext context, IReadOnlyList<DefinedName> definedNames = null)
    {
        if (context == null)
            return [];

        definedNames ??= [];
        return context.Kind switch
        {
            CaretContextKind.Expression => Match(context, GetExpressionItems(context, definedNames)),
            CaretContextKind.Argument => Match(context, GetArgumentItems(context, definedNames))
                .Concat(Match(context, GetExpressionItems(context, definedNames)))
                .ToList(),
            CaretContextKind.ArgumentName => Match(context, GetArgumentItems(context, definedNames)),
            CaretContextKind.DefaultSetting => Match(context, [ArgumentItem(BlockSetting.Hide)]),
            CaretContextKind.SettingValue => Match(context, GetValueItems(context), sort: false),
            _ => []
        };
    }

    /// <summary>
    /// Replaces the context's whole word with the item; a function also gets its opening parenthesis, and a named
    /// argument its colon and a space, unless one already follows. The caret ends after what was written.
    /// </summary>
    public static TextSnapshot Accept(CaretContext context, CompletionItem item)
    {
        var text = context.Text;
        var word = context.Word;
        var inserted = item.Label;
        var caret = word.Start + inserted.Length;
        switch (item.Kind)
        {
            case CompletionKind.Function:
                if (word.End >= text.Length || text[word.End] != '(')
                    inserted += "(";

                caret++;
                break;

            case CompletionKind.Argument when IsColonNext(text, word.End):
                inserted = inserted.TrimEnd(':');
                caret = word.Start + inserted.Length;
                break;

            case CompletionKind.Argument:
                inserted += " ";
                caret++;
                break;
        }

        return new TextSnapshot(text.Remove(word.Start, word.Length).Insert(word.Start, inserted), caret);
    }

    private static List<CompletionItem> Match(CaretContext context, IEnumerable<CompletionItem> candidates, bool sort = true)
    {
        List<CompletionItem> items = [.. candidates];
        if (sort)
            items.Sort(CompareLabels);

        var prefix = context.Prefix;
        var starting = items.Where(i => i.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        var containing = items.Where(i =>
            !i.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && i.Label.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0);

        return starting.Concat(containing).ToList();
    }

    private static IEnumerable<CompletionItem> GetExpressionItems(CaretContext context, IReadOnlyList<DefinedName> definedNames)
    {
        return _expressionItems.Concat(GetNameItems(context, definedNames));
    }

    /// <summary>
    /// The named arguments the call doesn't have yet: a Block Function's inputs and Settings, or a Placed Block
    /// Reference's inputs, since its Settings can't be changed. Inputs its positional arguments give are left out.
    /// </summary>
    private static IEnumerable<CompletionItem> GetArgumentItems(CaretContext context, IReadOnlyList<DefinedName> definedNames)
    {
        if (BlockManifest.TryGetFunction(context.Callee, out var type))
        {
            var items = GetInputItems(context, type)
                .Concat(type.Settings.Where(s => !IsGiven(s.Name)).Select(ArgumentItem));
            if (type.HasStyles && !IsGiven(BlockType.StyleName))
                items = items.Append(new CompletionItem($"{BlockType.StyleName}:", CompletionKind.Argument, string.Join(", ", type.Styles.Select(s => s.Name))));

            return IsGiven(BlockSetting.Hide.Name) ? items : items.Append(ArgumentItem(BlockSetting.Hide));
        }

        var placed = definedNames.LastOrDefault(n => n.Name == context.Callee && IsVisible(context, n));
        return placed is { IsPlaced: true, Type: not null } ? GetInputItems(context, placed.Type) : [];

        bool IsGiven(string name)
        {
            return context.GivenArguments.Contains(name);
        }
    }

    private static IEnumerable<CompletionItem> GetValueItems(CaretContext context)
    {
        return context.SettingValues.Select(v => new CompletionItem(v, CompletionKind.Value, string.Empty));
    }

    private static IEnumerable<CompletionItem> GetInputItems(CaretContext context, BlockType type)
    {
        return type.Inputs
            .Skip(context.PositionalArguments)
            .Where(i => !context.GivenArguments.Contains(i.Name))
            .Select(i => new CompletionItem($"{i.Name}:", CompletionKind.Argument, "input"));
    }

    private static CompletionItem ArgumentItem(BlockSetting setting)
    {
        return new CompletionItem($"{setting.Name}:", CompletionKind.Argument, BlockReference.DescribeValues(setting));
    }

    private static bool IsColonNext(string text, int index)
    {
        while (index < text.Length && text[index] is ' ' or '\t')
        {
            index++;
        }

        return index < text.Length && text[index] == ':';
    }

    private static List<CompletionItem> CreateExpressionItems()
    {
        List<CompletionItem> items = [];
        foreach (var type in BlockManifest.All.Where(t => !t.IsPlacedOnly))
        {
            items.Add(new CompletionItem(type.Function, CompletionKind.Function, DescribeArguments(type)));
        }

        foreach (var alias in BlockManifest.Aliases)
        {
            items.Add(new CompletionItem(alias.Key, CompletionKind.Function, $"{alias.Value.Function}{DescribeArguments(alias.Value)}"));
        }

        items.Add(new CompletionItem("placed", CompletionKind.Function, "(\"UID\")"));
        items.Add(new CompletionItem("true", CompletionKind.Keyword, string.Empty));
        items.Add(new CompletionItem("false", CompletionKind.Keyword, string.Empty));
        return items;
    }

    /// <summary>
    /// A name defined twice is offered once.
    /// </summary>
    private static IEnumerable<CompletionItem> GetNameItems(CaretContext context, IReadOnlyList<DefinedName> definedNames)
    {
        HashSet<string> offered = [];
        foreach (var name in definedNames)
        {
            if (IsVisible(context, name) && offered.Add(name.Name))
                yield return new CompletionItem(name.Name, CompletionKind.Name, name.Type?.Function ?? string.Empty);
        }
    }

    private static bool IsVisible(CaretContext context, DefinedName name)
    {
        return context.CanReferAhead || name.Span.Start < context.StatementStart;
    }

    private static int CompareLabels(CompletionItem a, CompletionItem b)
    {
        return string.CompareOrdinal(a.Label, b.Label);
    }

    /// <summary>
    /// The inputs, then the Settings and <c>style:</c>, each with its colon; <c>hide:</c> is left out, since every
    /// Block Function takes it.
    /// </summary>
    private static string DescribeArguments(BlockType type)
    {
        var names = type.Inputs.Select(i => i.Name).Concat(type.Settings.Select(s => $"{s.Name}:"));
        if (type.HasStyles)
            names = names.Append($"{BlockType.StyleName}:");

        return $"({string.Join(", ", names)})";
    }
}
