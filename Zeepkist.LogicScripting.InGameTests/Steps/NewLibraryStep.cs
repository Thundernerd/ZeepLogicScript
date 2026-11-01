using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class NewLibraryStep : ScenarioStep
{
    public NewLibraryStep(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireOpenWindow() && context.Window.NewLibrary(Name) is { } error)
            context.Fail(error);

        yield return null;
    }
}
