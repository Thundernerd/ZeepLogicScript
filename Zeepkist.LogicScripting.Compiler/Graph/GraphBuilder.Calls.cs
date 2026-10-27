using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private BlockNode VisitCall(CallExpression call)
    {
        if (!BlockManifest.TryGetFunction(call.Function, out var type))
        {
            foreach (var argument in call.Arguments)
            {
                EvaluateFeed(argument);
            }

            Error(call, $"'{call.Function}' isn't a Block Function");
            return null;
        }

        var check = new ArgumentCheck(call, type, _diagnostics);
        EnterArguments(type);
        VisitPositionalArguments(check);
        foreach (var argument in call.NamedArguments)
        {
            VisitNamedArgument(check, argument);
        }

        LeaveArguments(type);

        check.CheckRequiredSettings();
        if (check.Failed)
            return null;

        var block = _graph.AddBlock(type, type.OrderSettings(check.Settings));
        for (var i = 0; i < check.Inputs.Length; i++)
        {
            var feed = check.Inputs[i]?.Feed;
            if (feed != null)
                Connect(feed.Value, block, type.Inputs[i].Index);
        }

        return block;
    }

    private void VisitPositionalArguments(ArgumentCheck check)
    {
        var call = check.Call;
        var inputCount = check.Type.Inputs.Count;
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            var feed = EvaluateArgument(check, call.Arguments[i]);
            if (i < inputCount)
            {
                check.Inputs[i] = WiredInput.ByPosition(feed);
            }
        }

        if (call.Arguments.Count > inputCount)
        {
            check.Fail(call, $"'{call.Function}' takes at most {inputCount} inputs, got {call.Arguments.Count}");
        }
    }

    /// <summary>
    /// What feeds an argument; the check fails when nothing can.
    /// </summary>
    private InputFeed? EvaluateArgument(ArgumentCheck check, Expression expression)
    {
        var feed = EvaluateFeed(expression);
        check.Failed |= feed == null;
        return feed;
    }

    /// <summary>
    /// A named argument wires an input or gives a Setting.
    /// </summary>
    private void VisitNamedArgument(ArgumentCheck check, NamedArgument argument)
    {
        if (!check.Give(argument))
            return;

        var input = check.Type.IndexOfInput(argument.Name);
        if (input >= 0)
        {
            check.WireByName(argument, input, EvaluateArgument(check, argument.Value));
            return;
        }

        var blockSetting = check.Type.FindSetting(argument.Name);
        if (blockSetting == null)
        {
            check.ReportUnknownName(argument);
            return;
        }

        var setting = GetSettingValue(blockSetting, argument);
        if (setting == null)
            check.Failed = true;
        else
            check.Settings.Add(blockSetting, setting);
    }
}
