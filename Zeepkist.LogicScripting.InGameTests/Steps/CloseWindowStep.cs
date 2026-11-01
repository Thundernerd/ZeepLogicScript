using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class CloseWindowStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        context.Window.SetOpen(false);
        yield return null;
    }
}
