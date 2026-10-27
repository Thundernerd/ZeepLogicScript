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

        var check = new ArgumentCheck(call, type);
        EnterArguments(type);
        VisitPositionalArguments(check);
        foreach (var argument in call.NamedArguments)
        {
            VisitNamedArgument(check, argument);
        }

        LeaveArguments(type);

        check.Failed |= !HasRequiredSettings(check);
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
            Fail(check, call, $"'{call.Function}' takes at most {inputCount} inputs, got {call.Arguments.Count}");
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

    private void Fail(ArgumentCheck check, SyntaxNode node, string message)
    {
        Error(node, message);
        check.Failed = true;
    }

    /// <summary>
    /// A named argument wires an input or gives a Setting.
    /// </summary>
    private void VisitNamedArgument(ArgumentCheck check, NamedArgument argument)
    {
        if (!check.GivenNames.Add(argument.Name))
        {
            Fail(check, argument, $"'{argument.Name}' is given twice");
            return;
        }

        var input = check.Type.IndexOfInput(argument.Name);
        if (input >= 0)
        {
            WireNamedInput(check, argument, input);
            return;
        }

        var blockSetting = check.Type.FindSetting(argument.Name);
        if (blockSetting == null)
        {
            ReportUnknownName(check, argument);
            return;
        }

        var setting = GetSettingValue(blockSetting, argument);
        if (setting == null)
            check.Failed = true;
        else
            check.Settings.Add(blockSetting, setting);
    }

    private void WireNamedInput(ArgumentCheck check, NamedArgument argument, int input)
    {
        var feed = EvaluateArgument(check, argument.Value);
        if (check.Inputs[input]?.IsByPosition == true)
        {
            Fail(check, argument, $"Input '{argument.Name}' is already wired by position");
            return;
        }

        check.Inputs[input] = WiredInput.ByName(feed);
    }

    /// <summary>
    /// Reports a name that is neither an input nor a Setting, with the closest one it was probably meant as.
    /// </summary>
    private void ReportUnknownName(ArgumentCheck check, NamedArgument argument)
    {
        var call = check.Call;
        var message = $"'{call.Function}' has no input or Setting '{argument.Name}'";
        var closest = check.Type.FindClosestName(argument.Name);
        if (closest != null)
        {
            message += $"; did you mean {closest}?";
            check.MeantNames.Add(closest);
        }

        Fail(check, argument, message);
    }
}
