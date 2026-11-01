using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Replaces the text in the open name prompt, as typing it does.
/// </summary>
internal sealed class TypeNameStep : ScenarioStep
{
    public TypeNameStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (context.RequireOpenWindow() && !context.Window.ShownList.SetName(Text))
            context.Fail("no name prompt is open: add open-new or open-rename first", "new or rename", PromptNames.Of(context.Window.ShownList.OpenPrompt));

        yield return null;
    }
}
