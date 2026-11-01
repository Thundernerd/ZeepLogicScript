using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class OpenWindowStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        context.Window.SetOpen(true);

        // A frame to draw it, and one more to find out whether drawing it failed, which closes it again
        yield return null;
        yield return null;
        if (!context.Window.IsOpen)
            context.Fail("the window closed again: see LogOutput.log");
    }
}
