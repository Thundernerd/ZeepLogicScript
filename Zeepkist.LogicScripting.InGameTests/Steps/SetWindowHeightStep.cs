using System.Collections;
using System.Globalization;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class SetWindowHeightStep : ScenarioStep
{
    private const float DrawSeconds = 2;

    public SetWindowHeightStep(int height)
    {
        Height = height;
    }

    public int Height { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        context.Window.SetWindowHeight(Height);
        yield return context.WaitUntil(() => !context.Window.IsHeightPending, DrawSeconds);
        if (context.Window.IsHeightPending)
        {
            context.Fail("the window wasn't drawn to take the height");
            yield break;
        }

        // A frame to lay the window out at its new height, and one for the footer, which sizes itself from the last
        yield return null;
        yield return null;

        // A height below the window's minimum gives the smallest window
        var actual = context.Window.WindowRect.H.ToString("0", CultureInfo.InvariantCulture);
        context.Pass($"{actual} px tall", Height.ToString(CultureInfo.InvariantCulture), actual);
    }
}
