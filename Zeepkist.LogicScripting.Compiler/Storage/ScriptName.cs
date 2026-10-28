using System;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

public static class ScriptName
{
    public const int MaxLength = 40;

    /// <summary>
    /// Returns why <paramref name="name"/>, already trimmed, can't name a Script next to the level's other Scripts,
    /// <paramref name="otherNames"/>, or null when it can.
    /// </summary>
    public static string GetError(string name, IEnumerable<string> otherNames)
    {
        if (name.Length == 0)
            return "Give the Script a name";

        if (name.Length > MaxLength)
            return $"Use at most {MaxLength} characters";

        if (name.Any(char.IsControl))
            return "Keep the name on one line";

        // Two names that only differ in case would look like one Script in the list
        if (otherNames.Any(other => string.Equals(other, name, StringComparison.OrdinalIgnoreCase)))
            return $"Another Script is already called \"{name}\"";

        return null;
    }
}
