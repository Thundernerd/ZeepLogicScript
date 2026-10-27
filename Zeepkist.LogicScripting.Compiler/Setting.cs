namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// Which of the block's property maps a Setting is stored in.
/// </summary>
public enum SettingKind
{
    Int,
    Text
}

/// <summary>
/// A Setting key and value, ready to write into the block's property dictionary.
/// </summary>
public sealed class Setting
{
    private Setting(string key, SettingKind kind, int intValue, string textValue)
    {
        Key = key;
        Kind = kind;
        IntValue = intValue;
        TextValue = textValue;
    }

    public string Key { get; }
    public SettingKind Kind { get; }
    public int IntValue { get; }
    public string TextValue { get; }

    public static Setting Int(string key, int value)
    {
        return new Setting(key, SettingKind.Int, value, null);
    }

    public static Setting Text(string key, string value)
    {
        return new Setting(key, SettingKind.Text, 0, value);
    }
}
