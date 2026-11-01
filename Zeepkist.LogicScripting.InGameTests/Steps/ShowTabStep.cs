using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class ShowTabStep : ScenarioStep
{
    public ShowTabStep(bool libraries)
    {
        Libraries = libraries;
    }

    public bool Libraries { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireOpenWindow())
            context.Window.ShowTab(Libraries);

        yield return null;
    }
}
