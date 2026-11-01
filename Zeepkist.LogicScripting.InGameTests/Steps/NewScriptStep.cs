using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class NewScriptStep : ScenarioStep
{
    public NewScriptStep(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireOpenWindow() && context.Window.NewScript(Name) is { } error)
            context.Fail(error);

        yield return null;
    }
}
