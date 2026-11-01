using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Puts the caret before the selected Script's or Library's first character or after its last, and focuses the field,
/// as clicking there does. The call help box shows for the call the caret is then in.
/// </summary>
internal sealed class PlaceCaretStep : ScenarioStep
{
    public PlaceCaretStep(bool atEnd)
    {
        AtEnd = atEnd;
    }

    public bool AtEnd { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireText())
            yield break;

        context.Window.PlaceCaret(AtEnd ? context.Window.Text.Length : 0);

        // The field takes the focus when it's next drawn
        yield return null;
        yield return null;
    }
}
