using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class OpenNewStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireOpenWindow())
            context.Window.ShownList.OpenNew();

        yield return null;
    }
}
