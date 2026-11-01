using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Reads keys as a scenario writes them, such as <c>enter</c>, <c>ctrl+space</c> or <c>ctrl+/</c>, and the key that
/// types a character. Characters are those of a US keyboard.
/// </summary>
internal static class KeyNames
{
    private static readonly Dictionary<string, KeyModifiers> _modifiers = new()
    {
        ["shift"] = KeyModifiers.Shift,
        ["ctrl"] = KeyModifiers.Control,
        ["alt"] = KeyModifiers.Alt,
        ["cmd"] = KeyModifiers.Command,
    };

    private static readonly Dictionary<string, (string Key, char Character)> _named = new()
    {
        ["space"] = ("Space", ' '),
        ["enter"] = ("Return", '\n'),
        ["tab"] = ("Tab", '\t'),
        ["escape"] = ("Escape", '\0'),
        ["backspace"] = ("Backspace", '\0'),
        ["delete"] = ("Delete", '\0'),
        ["up"] = ("UpArrow", '\0'),
        ["down"] = ("DownArrow", '\0'),
        ["left"] = ("LeftArrow", '\0'),
        ["right"] = ("RightArrow", '\0'),
        ["home"] = ("Home", '\0'),
        ["end"] = ("End", '\0'),
        ["pageup"] = ("PageUp", '\0'),
        ["pagedown"] = ("PageDown", '\0'),
    };

    private static readonly Dictionary<char, string> _symbols = new()
    {
        ['/'] = "Slash",
        ['\\'] = "Backslash",
        ['.'] = "Period",
        [','] = "Comma",
        [';'] = "Semicolon",
        ['\''] = "Quote",
        ['['] = "LeftBracket",
        [']'] = "RightBracket",
        ['-'] = "Minus",
        ['='] = "Equals",
        ['`'] = "BackQuote",
    };

    /// <summary>
    /// Shift with a letter types it in upper case. Returns null for a key it doesn't know.
    /// </summary>
    public static KeyPress? Parse(string text)
    {
        var parts = text.ToLowerInvariant().Split('+');
        var modifiers = KeyModifiers.None;
        foreach (var part in parts.Take(parts.Length - 1))
        {
            if (!_modifiers.TryGetValue(part, out var modifier))
                return null;

            modifiers |= modifier;
        }

        var name = parts[parts.Length - 1];
        if (_named.TryGetValue(name, out var named))
            return new KeyPress(named.Key, named.Character, modifiers);

        if (name.Length != 1 || GetKey(name[0]) is not { } key)
            return null;

        var character = (modifiers & KeyModifiers.Shift) != 0 ? char.ToUpperInvariant(name[0]) : name[0];
        return new KeyPress(key, character, modifiers);
    }

    /// <summary>
    /// The key that types the character, with Shift for an upper-case letter. A character without a key of its own here,
    /// such as <c>(</c>, comes as its character event alone.
    /// </summary>
    public static KeyPress ForCharacter(char character)
    {
        if (character == ' ')
            return new KeyPress("Space", ' ', KeyModifiers.None);

        var lower = char.ToLowerInvariant(character);
        var shift = lower != character ? KeyModifiers.Shift : KeyModifiers.None;
        return new KeyPress(GetKey(lower) ?? "None", character, shift);
    }

    /// <summary>
    /// Null for a character without a key of its own here.
    /// </summary>
    private static string GetKey(char character)
    {
        return character switch
        {
            >= 'a' and <= 'z' => char.ToUpperInvariant(character).ToString(),
            >= '0' and <= '9' => $"Alpha{character}",
            _ => _symbols.TryGetValue(character, out var key) ? key : null,
        };
    }
}
