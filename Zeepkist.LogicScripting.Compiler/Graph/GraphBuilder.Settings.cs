using System.Globalization;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// A Setting's value is written out in the call itself: a number, true / false, or an option word.
    /// </summary>
    private Setting GetSettingValue(BlockSetting setting, NamedArgument argument)
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

                return Setting.Text(setting.Key, literal.Value.ToString(CultureInfo.InvariantCulture));

            case SettingType.Bool:
                if (argument.Value is NameExpression { Name: "true" or "false" } word)
                    return Setting.Int(setting.Key, word.Name == "true" ? 1 : 0);

                ValueError(argument, $"{setting.Name} takes true or false");
                return null;

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

                return setting.Choose(option.Name);
        }
    }
}
