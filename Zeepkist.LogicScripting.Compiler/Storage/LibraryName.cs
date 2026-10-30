using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

public static class LibraryName
{
    public const int MaxLength = 40;

    /// <summary>
    /// Returns why <paramref name="name"/>, already trimmed, can't name a Library next to the player's other
    /// Libraries, <paramref name="otherNames"/>, or null when it can. A Script writes it before the dot of a call, so
    /// it's a name like any other in a Script.
    /// </summary>
    public static string GetError(string name, IEnumerable<string> otherNames)
    {
        if (name.Length == 0)
            return "Give the Library a name";

        if (name.Length > MaxLength)
            return $"Use at most {MaxLength} characters";

        if (!Lexer.IsNameStart(name[0]) || !name.All(c => Lexer.IsNameStart(c) || Lexer.IsDigit(c)))
            return "Use only letters, digits and _, and start with a letter or _";

        if (BlockManifest.TryGetFunction(name, out _))
            return $"'{name}' is a Block Function";

        if (GraphBuilder.IsReserved(name))
            return $"'{name}' is a reserved word";

        // Two names that only differ in case would look like one Library in the list
        if (otherNames.Any(other => string.Equals(other, name, StringComparison.OrdinalIgnoreCase)))
            return $"Another Library is already called \"{name}\"";

        return null;
    }
}
