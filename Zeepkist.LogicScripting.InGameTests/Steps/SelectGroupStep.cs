using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class SelectGroupStep : ScenarioStep
{
    public SelectGroupStep(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireOpenWindow() && !context.Window.SelectGroup(Name))
            context.Fail($"no Script named \"{Name}\"", Name, string.Join(", ", context.Window.ScriptNames));

        yield return null;
    }
}
