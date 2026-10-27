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

        var arguments = new CallArguments(call, type);
        EnterArguments(type);
        VisitPositionalArguments(arguments);
        foreach (var argument in call.NamedArguments)
        {
            VisitNamedArgument(arguments, argument);
        }

        LeaveArguments(type);

        arguments.Failed |= !HasRequiredSettings(arguments);
        if (arguments.Failed)
            return null;

        var block = _graph.AddBlock(type, type.OrderSettings(arguments.Settings));
        for (var i = 0; i < arguments.Inputs.Length; i++)
        {
            if (arguments.Inputs[i] != null)
                Connect(arguments.Inputs[i].Value, block, type.Inputs[i].Index);
        }

        return block;
    }

    private void VisitPositionalArguments(CallArguments arguments)
    {
        var call = arguments.Call;
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            var output = EvaluateFeed(call.Arguments[i]);
            arguments.Failed |= output == null;
            if (i < arguments.Inputs.Length)
            {
                arguments.Inputs[i] = output;
                arguments.WiredByPosition[i] = true;
            }
        }

        if (call.Arguments.Count > arguments.Type.Inputs.Count)
        {
            Error(call, $"'{call.Function}' takes at most {arguments.Type.Inputs.Count} inputs, got {call.Arguments.Count}");
            arguments.Failed = true;
        }
    }

    /// <summary>
    /// A named argument wires an input or gives a Setting.
    /// </summary>
    private void VisitNamedArgument(CallArguments arguments, NamedArgument argument)
    {
        if (!arguments.GivenNames.Add(argument.Name))
        {
            Error(argument, $"'{argument.Name}' is given twice");
            arguments.Failed = true;
            return;
        }

        var input = arguments.Type.IndexOfInput(argument.Name);
        if (input >= 0)
        {
            WireNamedInput(arguments, argument, input);
            return;
        }

        var blockSetting = arguments.Type.FindSetting(argument.Name);
        if (blockSetting == null)
        {
            ReportUnknownName(arguments, argument);
            return;
        }

        var setting = GetSettingValue(blockSetting, argument);
        if (setting == null)
            arguments.Failed = true;
        else
            arguments.Settings.Add(blockSetting, setting);
    }

    private void WireNamedInput(CallArguments arguments, NamedArgument argument, int input)
    {
        var output = EvaluateFeed(argument.Value);
        arguments.Failed |= output == null;
        if (arguments.WiredByPosition[input])
        {
            Error(argument, $"Input '{argument.Name}' is already wired by position");
            arguments.Failed = true;
        }

        arguments.Inputs[input] = output;
    }

    /// <summary>
    /// Reports a name that is neither an input nor a Setting, with the closest one it was probably meant as.
    /// </summary>
    private void ReportUnknownName(CallArguments arguments, NamedArgument argument)
    {
        var function = arguments.Call.Function;
        var closest = arguments.Type.FindClosestName(argument.Name);
        if (closest == null)
        {
            Error(argument, $"'{function}' has no input or Setting '{argument.Name}'");
        }
        else
        {
            Error(argument, $"'{function}' has no input or Setting '{argument.Name}'; did you mean {closest}?");
            arguments.MeantNames.Add(closest);
        }

        arguments.Failed = true;
    }
}
