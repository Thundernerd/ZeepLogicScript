using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class SelectScriptStep : ScenarioStep
{
    public SelectScriptStep(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireOpenWindow() && !context.Window.SelectScript(Name))
            context.FailNoScript(Name);

        yield return null;
    }
}
