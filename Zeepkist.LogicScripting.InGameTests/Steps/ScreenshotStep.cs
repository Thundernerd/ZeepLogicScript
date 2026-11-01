using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class ScreenshotStep : ScenarioStep
{
    public ScreenshotStep(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override IEnumerator Run(StepContext context)
    {
        return context.Screenshot(Name);
    }
}
