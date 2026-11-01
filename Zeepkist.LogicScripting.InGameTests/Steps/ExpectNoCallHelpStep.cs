using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the call help box doesn't show.
/// </summary>
internal sealed class ExpectNoCallHelpStep : ScenarioStep
{
    private const float WaitSeconds = 1;

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireScript())
            yield break;

        yield return context.WaitUntil(() => context.Window.CallHelp == null, WaitSeconds);

        var help = context.Window.CallHelp;
        if (help == null)
            context.Pass("no call help", "(no call help)", "(no call help)");
        else
            context.Fail("the call help shows", "(no call help)", ExpectCallHelpStep.Describe(help));
    }
}
