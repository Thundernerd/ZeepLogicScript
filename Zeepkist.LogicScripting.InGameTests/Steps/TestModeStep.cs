using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Waits until the round starts, when the player can drive.
/// </summary>
internal sealed class TestModeStep : ScenarioStep
{
    private const float StartSeconds = 60;

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        if (!TestModeControls.HasStart(central))
        {
            context.Fail("the level has no start block: add place-start first");
            yield break;
        }

        var roundsBefore = context.Game.RoundStarts;
        TestModeControls.Enter(central);
        yield return context.WaitUntil(() => context.Game.RoundStarts > roundsBefore, StartSeconds);
        if (context.Game.RoundStarts > roundsBefore)
            yield break;

        context.Fail(TestModeControls.IsActive
            ? $"the round didn't start within {StartSeconds} seconds"
            : $"test mode didn't start within {StartSeconds} seconds");
    }
}
