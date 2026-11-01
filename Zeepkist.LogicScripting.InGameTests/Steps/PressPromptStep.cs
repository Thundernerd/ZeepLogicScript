using System.Collections;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.UI;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Clicks a button of the open prompt on the shown tab, which the prompt handles the next time it's drawn.
/// </summary>
internal sealed class PressPromptStep : ScenarioStep
{
    private const float DrawSeconds = 2;

    public PressPromptStep(PromptButton button)
    {
        Button = button;
    }

    public PromptButton Button { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        var list = context.Window.ShownList;
        var buttons = list.PromptButtons;
        if (!buttons.Contains(Button))
        {
            var prompt = list.OpenPrompt;
            context.Fail(
                prompt == ListPrompt.None ? "no prompt is open" : $"the {PromptNames.Of(prompt)} prompt has no such button",
                PromptNames.Of(Button),
                buttons.Count == 0 ? "(no buttons)" : string.Join(", ", buttons.Select(PromptNames.Of)));
            yield break;
        }

        list.Press(Button);
        yield return context.WaitUntil(() => !list.IsPressPending, DrawSeconds);
        if (list.IsPressPending)
            context.Fail("the prompt wasn't drawn to take the click");
    }
}
