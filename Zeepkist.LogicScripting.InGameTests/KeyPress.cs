namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// One key as Unity reports it to Imui: a key event, then the character it types as an event of its own. A key that
/// types nothing, such as Escape, has <see cref="Character"/> 0; a character with no key of its own has
/// <see cref="Key"/> <c>None</c>.
/// </summary>
internal readonly struct KeyPress
{
    public KeyPress(string key, char character, KeyModifiers modifiers)
    {
        Key = key;
        Character = character;
        Modifiers = modifiers;
    }

    /// <summary>
    /// The name of the key's <c>KeyCode</c>, which the scenario parser can't use: the xUnit tests run it without Unity.
    /// </summary>
    public string Key { get; }

    public char Character { get; }
    public KeyModifiers Modifiers { get; }
    public bool IsEscape => Key == "Escape";
}
