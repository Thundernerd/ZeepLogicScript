using System.Collections;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Waits for the window to check the text, as it does a moment after typing, then checks the errors in its list.
/// </summary>
internal abstract class ErrorCheckStep : ScenarioStep
{
    private const float CheckSeconds = 5;

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireText())
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

        Check(context, errors);
    }

    protected abstract void Check(StepContext context, IReadOnlyList<Diagnostic> errors);
}
