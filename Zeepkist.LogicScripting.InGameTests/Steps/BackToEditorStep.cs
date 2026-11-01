using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Waits until the editor has loaded the level again, blocks and all.
/// </summary>
internal sealed class BackToEditorStep : ScenarioStep
{
    private const float LoadSeconds = 60;

    public override IEnumerator Run(StepContext context)
    {
        if (!TestModeControls.IsActive)
        {
            context.Fail("not in test mode: add test-mode first");
            yield break;
        }

        var loadsBefore = context.Game.LevelLoads;
        yield return TestModeControls.Leave();
        yield return context.WaitUntil(() => TestModeControls.IsLevelBack(context.Game, loadsBefore), LoadSeconds);
        if (!TestModeControls.IsLevelBack(context.Game, loadsBefore))
            context.Fail($"the editor didn't load the level again within {LoadSeconds} seconds");
    }
}
