using System.Collections.Generic;
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

        EnterArguments(type);

        var failed = false;
        var inputs = new InputFeed?[type.Inputs.Count];
        var wiredByPosition = new bool[type.Inputs.Count];
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            var output = EvaluateFeed(call.Arguments[i]);
            failed |= output == null;
            if (i < inputs.Length)
            {
                inputs[i] = output;
                wiredByPosition[i] = true;
            }
        }

        if (call.Arguments.Count > type.Inputs.Count)
        {
            Error(call, $"'{call.Function}' takes at most {type.Inputs.Count} inputs, got {call.Arguments.Count}");
            failed = true;
        }

        Dictionary<BlockSetting, Setting> settings = [];
        HashSet<string> givenNames = [];
        HashSet<string> meantNames = [];
        foreach (var argument in call.NamedArguments)
        {
            if (!givenNames.Add(argument.Name))
            {
                Error(argument, $"'{argument.Name}' is given twice");
                failed = true;
                continue;
            }

            var input = type.IndexOfInput(argument.Name);
            if (input >= 0)
            {
                var output = EvaluateFeed(argument.Value);
                failed |= output == null;
                if (wiredByPosition[input])
                {
                    Error(argument, $"Input '{argument.Name}' is already wired by position");
                    failed = true;
                }

                inputs[input] = output;
                continue;
            }

            var blockSetting = type.FindSetting(argument.Name);
            if (blockSetting == null)
            {
                var closest = type.FindClosestName(argument.Name);
                if (closest == null)
                {
                    Error(argument, $"'{call.Function}' has no input or Setting '{argument.Name}'");
                }
                else
                {
                    Error(argument, $"'{call.Function}' has no input or Setting '{argument.Name}'; did you mean {closest}?");
                    meantNames.Add(closest);
                }

                failed = true;
                continue;
            }

            var setting = GetSettingValue(blockSetting, argument);
            if (setting == null)
                failed = true;
            else
                settings.Add(blockSetting, setting);
        }

        LeaveArguments(type);

        failed |= !HasRequiredSettings(call, type, givenNames, meantNames);
        if (failed)
            return null;

        var block = _graph.AddBlock(type, type.OrderSettings(settings));
        for (var i = 0; i < inputs.Length; i++)
        {
            if (inputs[i] != null)
                Connect(inputs[i].Value, block, type.Inputs[i].Index);
        }

        return block;
    }
}
