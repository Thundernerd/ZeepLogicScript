using System;
using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the selected Script's or Library's text contains the text, matching case.
/// </summary>
internal sealed class ExpectTextStep : ScenarioStep
{
    public ExpectTextStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireText())
        {
            var text = context.Window.Text;
            if (text.IndexOf(Text, StringComparison.Ordinal) >= 0)
                context.Pass(text, Text, text);
            else
                context.Fail("the text doesn't contain it", Text, text.Length == 0 ? "(no text)" : text);
        }

        yield return null;
    }
}
