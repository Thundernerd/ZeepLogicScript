namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// The Settings a Script's text travels in, on the first block of its Block Group. Like <see cref="BlockGroupTag.Key"/>,
/// no key may start with "id".
/// </summary>
public static class ScriptCarrier
{
    public const string TextKey = "zls_s";
    public const string NameKey = "zls_n";
    public const string LanguageVersionKey = "zls_v";

    /// <summary>
    /// Raise it when the language changes so that older text would compile differently.
    /// </summary>
    public const int LanguageVersion = 1;
}
