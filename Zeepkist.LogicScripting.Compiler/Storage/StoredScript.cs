namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// What the level holds for one Script, on a block of its Block Group (ADR 0006).
/// </summary>
public sealed class StoredScript
{
    public StoredScript(string id, string name, string text, int languageVersion)
    {
        Id = id;
        Name = name;
        Text = text;
        LanguageVersion = languageVersion;
    }

    public string Id { get; }
    public string Name { get; }
    public string Text { get; }
    public int LanguageVersion { get; }
}
