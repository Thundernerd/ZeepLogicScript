using System.Collections;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the open completion list has a row with the label, and with the detail too when one is given.
/// </summary>
internal sealed class ExpectCompletionStep : ScenarioStep
{
    private const float WaitSeconds = 1;

    public ExpectCompletionStep(string label, string detail)
    {
        Label = label;
        Detail = detail;
    }

    public string Label { get; }

    /// <summary>
    /// Null to accept any detail.
    /// </summary>
    public string Detail { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireText())
            yield break;

        yield return context.WaitUntil(() => context.Window.CompletionItems.Any(Matches), WaitSeconds);

        var expected = Detail == null ? Label : Describe(Label, Detail);
        var items = context.Window.CompletionItems;
        var actual = items.Count == 0 ? "(the list is closed)" : string.Join(", ", items.Select(i => Describe(i.Label, i.Detail)));
        if (items.Any(Matches))
            context.Pass(actual, expected, actual);
        else
            context.Fail("the completion list has no such row", expected, actual);
    }

    private static string Describe(string label, string detail)
    {
        return detail.Length == 0 ? label : $"{label} ({detail})";
    }

    private bool Matches(CompletionItem item)
    {
        return item.Label == Label && (Detail == null || item.Detail == Detail);
    }
}
