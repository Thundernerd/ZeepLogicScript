namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// A Script as the window holds it. Its name and text are the window's own: only a Compile stores them in the level.
/// </summary>
public sealed class ScriptEntry
{
    internal ScriptEntry(string id, string name, string text)
    {
        Id = id;
        Name = name;
        Text = text;
    }

    public string Id { get; }
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

    /// <summary>
    /// Whether the level has blocks in this Script's Block Group.
    /// </summary>
    public bool HasGroup { get; internal set; }

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
