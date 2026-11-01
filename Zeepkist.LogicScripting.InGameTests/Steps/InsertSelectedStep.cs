using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Writes a Placed Block Reference for each block selected in the level at the caret, as Insert selected does. Fails
/// when it writes none.
/// </summary>
internal sealed class InsertSelectedStep : ScenarioStep
{
    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null || !context.RequireScript())
            yield break;

        if (!context.Window.InsertSelected(central))
            context.Fail("inserted nothing: add place-block first");

        yield return null;
    }
}
