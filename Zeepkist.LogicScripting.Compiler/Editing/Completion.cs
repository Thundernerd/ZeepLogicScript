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
    private static readonly IReadOnlyList<CompletionItem> _expressionItems = GetExpressionItems();

    /// <summary>
    /// The items whose label starts with the typed prefix, then those that only contain it, each in alphabetical
    /// order. Case is ignored. Of the <paramref name="definedNames"/>, only those defined before the caret's statement
    /// are offered, unless the caret is inside <c>delay(...)</c>.
    /// </summary>
    public static IReadOnlyList<CompletionItem> GetItems(CaretContext context, IReadOnlyList<DefinedName> definedNames = null)
    {
        if (context == null || context.Kind != CaretContextKind.Expression)
            return [];

        List<CompletionItem> items = [.. _expressionItems, .. GetNameItems(context, definedNames ?? [])];
        items.Sort(CompareLabels);

        var prefix = context.Prefix;
        var starting = items.Where(i => i.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        var containing = items.Where(i =>
            !i.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && i.Label.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0);

        return starting.Concat(containing).ToList();
    }

    /// <summary>
    /// Replaces the context's whole word with the item; a function also gets its opening parenthesis, unless one
    /// already follows. The caret ends after what was written.
    /// </summary>
    public static TextSnapshot Accept(CaretContext context, CompletionItem item)
    {
        var text = context.Text;
        var word = context.Word;
        var inserted = item.Label;
        var opensCall = item.Kind == CompletionKind.Function;
        if (opensCall && (word.End >= text.Length || text[word.End] != '('))
            inserted += "(";

        var caret = word.Start + (opensCall ? item.Label.Length + 1 : inserted.Length);
        return new TextSnapshot(text.Remove(word.Start, word.Length).Insert(word.Start, inserted), caret);
    }

    private static List<CompletionItem> GetExpressionItems()
    {
        List<CompletionItem> items = [];
        foreach (var type in BlockManifest.All.Where(t => !t.IsPlacedOnly))
        {
            items.Add(new CompletionItem(type.Function, CompletionKind.Function, DescribeInputs(type)));
        }

        foreach (var alias in BlockManifest.Aliases)
        {
            items.Add(new CompletionItem(alias.Key, CompletionKind.Function, $"{alias.Value.Function}{DescribeInputs(alias.Value)}"));
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
            if ((context.CanReferAhead || name.Span.Start < context.StatementStart) && offered.Add(name.Name))
                yield return new CompletionItem(name.Name, CompletionKind.Name, name.Type?.Function ?? string.Empty);
        }
    }

    private static int CompareLabels(CompletionItem a, CompletionItem b)
    {
        return string.CompareOrdinal(a.Label, b.Label);
    }

    private static string DescribeInputs(BlockType type)
    {
        return $"({string.Join(", ", type.Inputs.Select(i => i.Name))})";
    }
}
