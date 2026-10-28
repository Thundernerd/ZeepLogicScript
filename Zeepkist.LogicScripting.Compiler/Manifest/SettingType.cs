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
    Option,

    /// <summary>
    /// A player as the editor shows it: <c>1</c>–<c>4</c>, or <c>all</c> where the editor offers everyone. Stored as
    /// its index in <see cref="BlockSetting.Options"/>, like an option.
    /// </summary>
    Player,

    /// <summary>
    /// The blocks a Toggler switches, given as Placed Block References.
    /// </summary>
    Targets
}
