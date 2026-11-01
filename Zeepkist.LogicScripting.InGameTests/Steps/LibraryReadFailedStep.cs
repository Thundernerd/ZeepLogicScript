using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class LibraryReadFailedStep : ScenarioStep
{
    public LibraryReadFailedStep(bool failed)
    {
        Failed = failed;
    }

    public bool Failed { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        context.Window.ShowLibraryReadFailed(Failed);

        // A frame to draw the tab with or without the warning, and one for the footer, which sizes itself from the last
        yield return null;
        yield return null;
    }
}
