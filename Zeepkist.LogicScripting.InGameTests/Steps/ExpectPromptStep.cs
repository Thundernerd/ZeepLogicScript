using System.Collections;
using TNRD.Zeepkist.LogicScripting.UI;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the shown tab's footer has this prompt open, or none.
/// </summary>
internal sealed class ExpectPromptStep : ScenarioStep
{
    public ExpectPromptStep(ListPrompt prompt)
    {
        Prompt = prompt;
    }

    public ListPrompt Prompt { get; }

    public override IEnumerator Run(StepContext context)
    {
        var open = PromptNames.Of(context.Window.ShownList.OpenPrompt);
        if (context.Window.ShownList.OpenPrompt == Prompt)
            context.Pass(open, PromptNames.Of(Prompt), open);
        else
            context.Fail("another prompt is open", PromptNames.Of(Prompt), open);

        yield return null;
    }
}
