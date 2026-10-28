using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// The values a named argument gives a Setting, written out in the call itself: a number, true / false, an option
    /// word, or for Targets one per block (see <see cref="GetTargets"/>). Null after reporting why the value is wrong.
    /// </summary>
    private IReadOnlyList<Setting> GetSettingValues(BlockSetting setting, NamedArgument argument)
    {
        switch (setting.Type)
        {
            case SettingType.Number:
                if (argument.Value is not IntegerLiteral literal)
                {
                    ValueError(argument, $"{setting.Name} takes a whole number, like {setting.Name}: 5");
                    return null;
                }

                if (literal.Value < setting.Minimum)
                {
                    ValueError(argument, $"{setting.Name} must be at least {setting.Minimum}");
                    return null;
                }

                if (literal.Value > setting.Maximum)
                {
                    ValueError(argument, $"{setting.Name} must be at most {setting.Maximum}");
                    return null;
                }

                return [Setting.Text(setting.Key, literal.Value.ToString(CultureInfo.InvariantCulture))];

            case SettingType.Bool:
                if (argument.Value is NameExpression { Name: "true" or "false" } word)
                    return [Setting.Int(setting.Key, word.Name == "true" ? 1 : 0)];

                ValueError(argument, $"{setting.Name} takes true or false");
                return null;

            case SettingType.Player:
                var player = argument.Value switch
                {
                    IntegerLiteral number => number.Value.ToString(CultureInfo.InvariantCulture),
                    NameExpression everyone => everyone.Name,
                    _ => null
                };

                if (player == null || setting.IndexOf(player) < 0)
                {
                    var players = string.Join(", ", setting.Options.Take(setting.Options.Count - 1));
                    ValueError(argument, $"{setting.Name} takes {players} or {setting.Options[setting.Options.Count - 1]}");
                    return null;
                }

                return [setting.Choose(player)];

            case SettingType.Targets:
                return GetTargets(setting, argument);

            default:
                var options = string.Join(", ", setting.Options);
                if (argument.Value is not NameExpression option)
                {
                    ValueError(argument, $"{setting.Name} takes a word: {options}");
                    return null;
                }

                if (setting.IndexOf(option.Name) < 0)
                {
                    ValueError(argument, $"'{option.Name}' isn't an option of {setting.Name}; use {options}");
                    return null;
                }

                return [setting.Choose(option.Name)];
        }
    }

    /// <summary>
    /// A Toggler's targets: one Placed Block Reference, or a list of 1 to 8 different ones. Null after reporting why
    /// the value is wrong.
    /// </summary>
    private IReadOnlyList<Setting> GetTargets(BlockSetting setting, NamedArgument argument)
    {
        var example = $"{setting.Name}: [door_1, door_2]";
        var items = argument.Value is ListExpression list ? list.Items : [argument.Value];
        if (items.Count == 0)
        {
            ValueError(argument, $"{setting.Name} takes 1 to {BlockSetting.MaxTargets} placed blocks, like {example}");
            return null;
        }

        var failed = false;
        if (items.Count > BlockSetting.MaxTargets)
        {
            Error(items[BlockSetting.MaxTargets], $"A Toggler switches at most {BlockSetting.MaxTargets} blocks");
            failed = true;
        }

        List<Setting> targets = [];
        HashSet<string> uids = [];
        foreach (var item in items)
        {
            var value = Evaluate(item);
            if (value == null)
            {
                failed = true;
            }
            else if (!value.Value.Block.IsPlaced)
            {
                Error(item, $"'{Describe(item)}' isn't a placed block; {setting.Name} takes names defined with {PlacedFunction}(...)");
                failed = true;
            }
            else if (!uids.Add(value.Value.Block.Uid))
            {
                Error(item, $"'{Describe(item)}' is already a target");
                failed = true;
            }
            else
            {
                targets.Add(Setting.Text(setting.Key + uids.Count.ToString(CultureInfo.InvariantCulture), value.Value.Block.Uid));
            }
        }

        return failed ? null : targets;
    }
}
