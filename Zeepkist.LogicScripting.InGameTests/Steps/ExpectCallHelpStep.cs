using System;
using System.Collections;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the call help box shows and its call, as it's written there (<c>logic(a, b, op_in, op:, hide:)</c>),
/// contains the text, matching case.
/// </summary>
internal sealed class ExpectCallHelpStep : ScenarioStep
{
    /// <summary>
    /// What a call help step reports as found when the call help doesn't show.
    /// </summary>
    public const string NoCallHelp = "(no call help)";

    private const float WaitSeconds = 1;

    public ExpectCallHelpStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireText())
            yield break;

        yield return context.WaitUntil(() => Contains(context.Window.CallHelp), WaitSeconds);

        var help = context.Window.CallHelp;
        var actual = help == null ? NoCallHelp : Describe(help);
        if (Contains(help))
            context.Pass(actual, Text, actual);
        else
            context.Fail("the call help doesn't show it", Text, actual);
    }

    /// <summary>
    /// The call as the box writes it, such as <c>logic(a, b, op_in, op:, hide:)</c>.
    /// </summary>
    public static string Describe(CallHelp help)
    {
        return $"{help.Callee}({string.Join(", ", help.Items.Select(i => i.Label))})";
    }

    private bool Contains(CallHelp help)
    {
        return help != null && Describe(help).IndexOf(Text, StringComparison.Ordinal) >= 0;
    }
}
