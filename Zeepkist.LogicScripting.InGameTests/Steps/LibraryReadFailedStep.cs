using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class LibraryReadFailedStep : ScenarioStep
{
    public LibraryReadFailedStep(bool show)
    {
        Show = show;
    }

    public bool Show { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        context.Window.ShowLibraryReadFailed(Show);

        // A frame to draw the tab with or without the warning, and one for the footer, which sizes itself from the last
        yield return null;
        yield return null;
    }
}
