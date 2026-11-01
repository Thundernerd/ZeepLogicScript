using System.Collections;
using System.Globalization;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.UI;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the shown tab's footer, the open prompt, the result line and New, lies inside the tab, inside the
/// window, with the list above it at least two rows tall. The prompts may scroll when there's no room for all of them,
/// but get at least a row.
/// </summary>
internal sealed class ExpectFooterInViewStep : ScenarioStep
{
    // Rects are laid out in fractions of a pixel
    private const float Tolerance = 0.5f;

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireOpenWindow())
            yield break;

        // The footer sizes itself from the height its prompts took the frame before, so a prompt just opened settles
        // a frame after it's first drawn
        yield return null;
        yield return null;
        yield return null;

        var list = context.Window.ShownList;
        var layout = list.FooterLayout;
        var window = context.Window.WindowRect;
        var hasPrompts = list.OpenPrompt != ListPrompt.None || list.Message.Length > 0;
        if (!Contains(window, layout.Tab))
            context.Fail("the tab reaches outside the window", Describe(window), Describe(layout.Tab));
        else if (!Contains(layout.Tab, layout.NewRow))
            context.Fail("New reaches outside the tab", Describe(layout.Tab), Describe(layout.NewRow));
        else if (layout.List.H < layout.MinListHeight - Tolerance)
            context.Fail("the list is less than two rows tall", $"{Pixels(layout.MinListHeight)} px or more", $"{Pixels(layout.List.H)} px");
        else if (hasPrompts && layout.PromptsHeight <= 0)
            context.Fail("the prompt or result line wasn't drawn");
        else if (hasPrompts && !Contains(layout.Tab, layout.Prompts))
            context.Fail("the prompt or result line reaches outside the tab", Describe(layout.Tab), Describe(layout.Prompts));
        else if (hasPrompts && layout.Prompts.H < layout.RowHeight - Tolerance)
            context.Fail("the prompt and result line get less than a row", $"{Pixels(layout.RowHeight)} px or more", $"{Pixels(layout.Prompts.H)} px");
        else
            context.Pass(Summarize(layout, hasPrompts), null, null);
    }

    private static string Summarize(ListFooterLayout layout, bool hasPrompts)
    {
        var listed = $"the list is {Pixels(layout.List.H)} px tall";
        if (!hasPrompts)
            return $"{listed}, with no prompt or result line";

        return layout.Prompts.H >= layout.PromptsHeight - Tolerance
            ? $"{listed}, and the prompt and result line show all {Pixels(layout.PromptsHeight)} px"
            : $"{listed}, and the prompt and result line show {Pixels(layout.Prompts.H)} of {Pixels(layout.PromptsHeight)} px, the rest scrolls";
    }

    private static bool Contains(ImRect outer, ImRect inner)
    {
        return inner.H >= 0
            && inner.Left >= outer.Left - Tolerance
            && inner.Right <= outer.Right + Tolerance
            && inner.Bottom >= outer.Bottom - Tolerance
            && inner.Top <= outer.Top + Tolerance;
    }

    private static string Describe(ImRect rect)
    {
        return $"x {Pixels(rect.X)}, y {Pixels(rect.Y)}, {Pixels(rect.W)} by {Pixels(rect.H)} px";
    }

    private static string Pixels(float pixels)
    {
        return pixels.ToString("0", CultureInfo.InvariantCulture);
    }
}
