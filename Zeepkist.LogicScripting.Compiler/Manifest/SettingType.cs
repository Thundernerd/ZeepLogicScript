namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// What a Setting accepts in a Script and how its value is stored.
/// </summary>
public enum SettingType
{
    /// <summary>
    /// A whole number, stored as text.
    /// </summary>
    Number,

    /// <summary>
    /// <c>true</c> or <c>false</c>, stored as 1 or 0.
    /// </summary>
    Bool,

    /// <summary>
    /// A bare word from <see cref="BlockSetting.Options"/>, stored as its index.
    /// </summary>
    Option
}
