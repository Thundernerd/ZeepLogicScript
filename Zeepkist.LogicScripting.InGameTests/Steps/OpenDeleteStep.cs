using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class OpenDeleteStep : ScenarioStep
{
    public OpenDeleteStep(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireOpenWindow() && !context.Window.ShownList.OpenDelete(Name))
            context.FailNoEntry(Name);

        yield return null;
    }
}
