using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class LogStep : ScenarioStep
{
    public LogStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        context.Logger.LogInfo($"{TestRun.Marker} {context.Scenario}: {Text}");
        yield return null;
    }
}
