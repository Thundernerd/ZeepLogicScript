using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Writes <c>{uid:&lt;name&gt;}</c> as the UID of the block <c>place-block ... as &lt;name&gt;</c> placed, so a Script
/// can refer to it inline.
/// </summary>
internal sealed class SetTextStep : ScenarioStep
{
    private static readonly Regex _uid = new(@"\{uid:([^}]*)\}");

    public SetTextStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireText())
            yield break;

        var unknown = _uid.Matches(Text).Cast<Match>().Select(m => m.Groups[1].Value).FirstOrDefault(n => !context.PlacedUids.ContainsKey(n));
        if (unknown != null)
        {
            context.Fail(
                $"no block placed as \"{unknown}\": add place-block <function> as {unknown} first",
                unknown,
                context.PlacedUids.Count == 0 ? "(none)" : string.Join(", ", context.PlacedUids.Keys));
            yield break;
        }

        context.Window.SetText(_uid.Replace(Text, m => context.PlacedUids[m.Groups[1].Value]));
        yield return null;
    }
}
