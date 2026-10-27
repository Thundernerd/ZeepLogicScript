using System.Linq;
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

            Error(call, BlockManifest.TryGetPlacedOnly(call.Function, out _)
                ? $"'{call.Function}' is placed by hand; a Script can only refer to one with placed(...)"
                : $"'{call.Function}' isn't a Block Function");
            return null;
        }

        var check = new ArgumentCheck(call, type, Error);
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

        var block = AddBlock(call, type, check.Style, type.OrderSettings(check.Settings));
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
                check.WireByPosition(i, feed);
            }
        }

        if (call.Arguments.Count > inputCount)
        {
            check.ReportAndFail(call, $"'{call.Function}' takes at most {inputCount} inputs, got {call.Arguments.Count}");
        }
    }

    /// <summary>
    /// What feeds an argument; the check fails when nothing can.
    /// </summary>
    private InputFeed? EvaluateArgument(ArgumentCheck check, Expression expression)
    {
        var feed = EvaluateFeed(expression);
        if (feed == null)
            check.MarkFailed();

        return feed;
    }

    /// <summary>
    /// A named argument wires an input or gives a Setting.
    /// </summary>
    private void VisitNamedArgument(ArgumentCheck check, NamedArgument argument)
    {
        if (!check.TryGive(argument))
            return;

        var input = check.Type.IndexOfInput(argument.Name);
        if (input >= 0)
        {
            check.WireByName(argument, input, EvaluateArgument(check, argument.Value));
            return;
        }

        if (argument.Name == BlockType.StyleName && check.Type.HasStyles)
        {
            check.ChooseStyle(GetStyle(check.Type, argument));
            return;
        }

        var blockSetting = check.Type.FindSetting(argument.Name);
        if (blockSetting == null)
        {
            check.ReportUnknownName(argument);
            return;
        }

        check.GiveSetting(blockSetting, GetSettingValue(blockSetting, argument));
    }

    /// <summary>
    /// The style a <c>style:</c> argument names, or null after reporting why it names none.
    /// </summary>
    private BlockStyle GetStyle(BlockType type, NamedArgument argument)
    {
        var styles = string.Join(", ", type.Styles.Select(s => s.Name));
        if (argument.Value is not NameExpression word)
        {
            ValueError(argument, $"{BlockType.StyleName} takes a word: {styles}");
            return null;
        }

        var style = type.FindStyle(word.Name);
        if (style == null)
            ValueError(argument, $"'{word.Name}' isn't a style of {type.Function}; use {styles}");

        return style;
    }
}
