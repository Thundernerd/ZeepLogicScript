using System.Collections;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Passes when the call help box shows and the input or Setting it highlights is the one with the label, as the box
/// writes it (<c>b</c>, <c>op:</c>), or, without a label, when it highlights none of them.
/// </summary>
internal sealed class ExpectCallHelpActiveStep : ScenarioStep
{
    private const float WaitSeconds = 1;
    private const string NoCallHelp = "(no call help)";
    private const string NoneActive = "(none)";

    /// <param name="label">Null to expect none highlighted.</param>
    public ExpectCallHelpActiveStep(string label)
    {
        Label = label;
    }

    public string Label { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireText())
            yield break;

        var expected = Label ?? NoneActive;
        yield return context.WaitUntil(() => DescribeActive(context.Window.CallHelp) == expected, WaitSeconds);

        var help = context.Window.CallHelp;
        var actual = DescribeActive(help);
        var call = help == null ? NoCallHelp : ExpectCallHelpStep.Describe(help);
        if (actual == expected)
            context.Pass($"{actual} in {call}", expected, actual);
        else if (help == null)
            context.Fail("the call help doesn't show", expected, actual);
        else
            context.Fail($"{call} highlights {actual}", expected, actual);
    }

    /// <summary>
    /// The highlighted item's label, or what stands in for it when there's none or no call help.
    /// </summary>
    private static string DescribeActive(CallHelp help)
    {
        if (help == null)
            return NoCallHelp;

        return help.Active >= 0 ? help.Items[help.Active].Label : NoneActive;
    }
}
