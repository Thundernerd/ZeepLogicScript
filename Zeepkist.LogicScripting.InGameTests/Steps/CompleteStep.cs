using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Opens the completion list at the caret, as Ctrl+Space does.
/// </summary>
internal sealed class CompleteStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireText())
            context.Window.RequestCompletion();

        yield return null;
    }
}
