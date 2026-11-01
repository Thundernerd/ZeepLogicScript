using System.Collections;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the completion list is closed.
/// </summary>
internal sealed class ExpectNoCompletionStep : ScenarioStep
{
    private const float WaitSeconds = 1;

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireText())
            yield break;

        yield return context.WaitUntil(() => context.Window.CompletionItems.Count == 0, WaitSeconds);

        var items = context.Window.CompletionItems;
        if (items.Count == 0)
            context.Pass("the list is closed", "(the list is closed)", "(the list is closed)");
        else
            context.Fail("the completion list is open", "(the list is closed)", string.Join(", ", items.Select(i => i.Label)));
    }
}
