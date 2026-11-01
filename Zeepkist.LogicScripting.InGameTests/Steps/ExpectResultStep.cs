using System;
using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the result line contains the text, matching case.
/// </summary>
internal sealed class ExpectResultStep : ScenarioStep
{
    public ExpectResultStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        var result = context.Window.Result;
        if (result.IndexOf(Text, StringComparison.Ordinal) >= 0)
            context.Pass(result, Text, result);
        else
            context.Fail("the result line doesn't contain it", Text, result.Length == 0 ? "(no result line)" : result);

        yield return null;
    }
}
