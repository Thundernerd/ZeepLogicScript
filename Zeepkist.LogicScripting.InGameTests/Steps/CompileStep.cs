using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes whatever the Compile did, so a scenario can check a failing one with the expect steps.
/// </summary>
internal sealed class CompileStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireScript())
            context.Detail = context.Window.Compile() ?? "the check found errors, which the error list shows";

        yield return null;
    }
}
