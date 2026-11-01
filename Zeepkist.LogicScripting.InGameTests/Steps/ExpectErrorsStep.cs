using System.Collections;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Waits for the window to check the text, as it does a moment after typing, then counts the errors in its list.
/// </summary>
internal class ExpectErrorsStep : ScenarioStep
{
    private const float CheckSeconds = 5;

    public ExpectErrorsStep(int count)
    {
        Count = count;
    }

    public int Count { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireScript())
            yield break;

        yield return context.WaitUntil(() => context.Window.IsChecked, CheckSeconds);
        if (!context.Window.IsChecked)
        {
            context.Fail($"the window didn't check the text within {CheckSeconds} seconds");
            yield break;
        }

        var errors = context.Window.Errors;
        if (errors == null)
        {
            context.Fail("the check couldn't run: see LogOutput.log");
            yield break;
        }

        var listed = string.Join("; ", errors.Select(e => e.ToString()));
        if (errors.Count == Count)
            context.Pass(listed, Count.ToString(), errors.Count.ToString());
        else
            context.Fail($"{errors.Count} errors", Count.ToString(), errors.Count == 0 ? "0" : $"{errors.Count}: {listed}");
    }
}
