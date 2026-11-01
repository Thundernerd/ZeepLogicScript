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
        if (context.RequireOpenWindow() && !context.Window.OpenDelete(Name))
            context.Fail($"no Script named \"{Name}\"", Name, string.Join(", ", context.Window.ScriptNames));

        yield return null;
    }
}
