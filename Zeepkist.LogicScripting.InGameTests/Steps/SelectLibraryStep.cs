using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class SelectLibraryStep : ScenarioStep
{
    public SelectLibraryStep(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireOpenWindow() && !context.Window.SelectLibrary(Name))
            context.Fail($"no Library named \"{Name}\"", Name, string.Join(", ", context.Window.LibraryNames));

        yield return null;
    }
}
