namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// A Script as the window holds it. Its name and text are the window's own: only a Compile stores them in the level.
/// </summary>
public sealed class ScriptEntry
{
    internal ScriptEntry(ScriptId id, string name, string text)
    {
        Id = id;
        Name = name;
        Text = text;
    }

    public ScriptId Id { get; }
    /// <summary>
    /// Check a new name with <see cref="ScriptList.GetNameError"/> first.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The editor's text, which editor undo never changes.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// What the level holds for this Script, or null when it holds nothing.
    /// </summary>
    public StoredScript Stored { get; internal set; }

    public bool HasGroup { get; internal set; }

    /// <summary>
    /// What a Compile stores in the level: the window's name and text.
    /// </summary>
    public StoredScript ToStored()
    {
        return new StoredScript(Id, Name, Text, ScriptCarrier.LanguageVersion);
    }

    public ScriptMark Mark
    {
        get
        {
            if (Stored != null)
                return Stored.Name == Name && Stored.Text == Text ? ScriptMark.None : ScriptMark.NotCompiled;

            return HasGroup ? ScriptMark.CarrierMissing : ScriptMark.NeverCompiled;
        }
    }
}
