using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private BlockNode VisitCall(CallExpression call)
    {
        if (IsReferenceCall(call))
        {
            VisitReferenceCall(call);
            return null;
        }

        if (!BlockManifest.TryGetFunction(call.Function, out var type))
        {
            foreach (var argument in call.Arguments)
            {
                EvaluateFeed(argument);
            }

            foreach (var argument in call.NamedArguments)
            {
                CheckUntypedValue(argument.Value);
            }

            var message = GetUnknownFunctionMessage(call.Function);
            if (message != null)
                Error(call, message);

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
        check.CheckRange();
        if (check.Failed)
            return null;

        var block = AddBlock(call, type, check.Style, type.OrderSettings(check.Settings));
        for (var i = 0; i < check.Inputs.Length; i++)
        {
            if (check.Inputs[i]?.Feed is { } feed)
                Connect(feed, block, type.Inputs[i].Index);
        }

        return block;
    }

    /// <summary>
    /// Reports what's wrong in a named argument's value when there's no type to tell a Setting from an input. A bare
    /// word may be a Setting's option (<c>op: and</c>), so it isn't looked up as a name; the items of a list are, as
    /// only a Toggler's targets take one.
    /// </summary>
    private void CheckUntypedValue(Expression value)
    {
        switch (value)
        {
            case NameExpression:
                return;

            case ListExpression list:
                foreach (var item in list.Items)
                {
                    Evaluate(item);
                }

                return;

            default:
                if (_scope.DelayDepth == 0 || !IsForwardReference(value))
                    Evaluate(value);

                return;
        }
    }

    /// <summary>
    /// Why a call to something other than a Block Function, a Function or a Placed Block Reference is wrong, or null when its
    /// name's definition already reported why it failed.
    /// </summary>
    private string GetUnknownFunctionMessage(string function)
    {
        // A Setting input called as a placed block is reported with its Function
        if (_scope.FailedNames.Contains(function) || _scope.Names.TryGetValue(function, out var value) && value is SettingConstant)
            return null;

        if (_scope.Names.ContainsKey(function))
            return $"'{function}' isn't a placed block, so it can't be called. Only names defined with {PlacedFunction}(...) can";

        if (_scope.Definitions.TryGetValue(function, out var later))
            return $"'{function}' is defined later, on line {later.Position.Line}";

        if (GetOutOfScopeMessage(function) is { } outOfScope)
            return outOfScope;

        return BlockManifest.TryGetPlacedOnly(function, out _)
            ? $"'{function}' is placed by hand, so a Script can only refer to it with {PlacedFunction}(...)"
            : $"'{function}' isn't a Block Function";
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

    private InputFeed EvaluateArgument(ArgumentCheck check, Expression expression)
    {
        var feed = EvaluateFeed(expression);
        if (feed == null)
            check.MarkFailed();

        return feed;
    }

    private void VisitNamedArgument(ArgumentCheck check, NamedArgument argument)
    {
        if (!check.TryGive(argument))
            return;

        var type = check.Type;
        switch (GetNamedArgumentKind(type, argument.Name))
        {
            case NamedArgumentKind.Input:
                check.WireByName(argument, type.IndexOfInput(argument.Name), EvaluateArgument(check, argument.Value));
                break;
            case NamedArgumentKind.Style:
                check.ChooseStyle(WithSettingInput(argument, a => GetStyle(type, a)));
                break;
            case NamedArgumentKind.Setting:
                var blockSetting = type.FindSetting(argument.Name);
                check.GiveSettings(blockSetting, WithSettingInput(argument, a => GetSettingValues(blockSetting, a)));
                break;
            case NamedArgumentKind.Targets:
                var targets = type.FindSetting(argument.Name);
                check.GiveSettings(targets, GetTargets(targets, argument));
                break;
            default:
                check.ReportUnknownName(argument);
                break;
        }
    }

    /// <summary>
    /// An input by name comes first, then the style, then a Setting.
    /// </summary>
    private static NamedArgumentKind GetNamedArgumentKind(BlockType type, string name)
    {
        if (type.IndexOfInput(name) >= 0)
            return NamedArgumentKind.Input;

        if (name == BlockType.StyleName && type.HasStyles)
            return NamedArgumentKind.Style;

        return type.FindSetting(name)?.Type switch
        {
            null => NamedArgumentKind.Unknown,
            SettingType.Targets => NamedArgumentKind.Targets,
            _ => NamedArgumentKind.Setting
        };
    }

    /// <summary>
    /// Whether a named argument gives a constant: a Setting or the style. A Toggler's targets are placed blocks, so
    /// they're names to look up.
    /// </summary>
    private static bool GivesConstant(BlockType type, string name)
    {
        return GetNamedArgumentKind(type, name) is NamedArgumentKind.Style or NamedArgumentKind.Setting;
    }

    /// <summary>
    /// Null after reporting why the argument names no style.
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
            ValueError(argument, $"'{word.Name}' isn't a style of {type.Function}. Use {styles}");

        return style;
    }
}
