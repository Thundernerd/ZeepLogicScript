using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class SetTextStep : ScenarioStep
{
    public SetTextStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireScript())
            context.Window.SetText(Text);

        yield return null;
    }
}
