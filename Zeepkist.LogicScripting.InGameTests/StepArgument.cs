using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Reads what follows a step's name on its line, or throws <see cref="ScenarioFormatException"/> saying what the step
/// takes.
/// </summary>
internal static class StepArgument
{
    public static void None(string step, string argument)
    {
        if (argument.Length > 0)
            throw new ScenarioFormatException($"{step} takes nothing after it");
    }

    public static string Text(string step, string argument, string what)
    {
        return argument.Length > 0 ? argument : throw new ScenarioFormatException($"{step} takes {what}");
    }

    public static int Count(string step, string argument)
    {
        var what = $"{step} takes a whole number of 0 or more";
        if (argument.Length == 0)
            throw new ScenarioFormatException(what);

        return int.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
            ? count
            : throw new ScenarioFormatException($"{what}, not '{argument}'");
    }

    /// <summary>
    /// A whole number of 1 or more, such as a one-based line.
    /// </summary>
    public static bool TryPositive(string text, out int number)
    {
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number > 0;
    }

    public static float Seconds(string step, string argument)
    {
        var what = $"{step} takes a number of seconds above 0";
        if (argument.Length == 0)
            throw new ScenarioFormatException(what);

        return float.TryParse(argument, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
            ? seconds
            : throw new ScenarioFormatException($"{what}, not '{argument}'");
    }

    /// <summary>
    /// Keys separated by spaces, each a key's name with any of <c>shift+</c>, <c>ctrl+</c>, <c>alt+</c> and
    /// <c>cmd+</c> before it.
    /// </summary>
    public static IReadOnlyList<KeyPress> Keys(string step, string argument, int max)
    {
        var what = $"{step} takes 1 to {max} keys, such as enter or ctrl+space";
        var names = argument.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0 || names.Length > max)
            throw new ScenarioFormatException(argument.Length == 0 ? what : $"{what}, not {names.Length}");

        return [.. names.Select(name => KeyNames.Parse(name) ?? throw new ScenarioFormatException($"{step} doesn't know the key '{name}'"))];
    }

    /// <summary>
    /// A name that's also part of a file name.
    /// </summary>
    public static string FileName(string step, string argument)
    {
        var what = $"{step} takes a name of letters, digits, '-' and '_'";
        if (argument.Length == 0)
            throw new ScenarioFormatException(what);

        return argument.All(c => (c < 128 && char.IsLetterOrDigit(c)) || c == '-' || c == '_')
            ? argument
            : throw new ScenarioFormatException($"{what}, not '{argument}'");
    }
}
