namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// A named set of Functions kept on the player's machine, which any Script calls by their qualified name,
/// <c>timing.edge(x)</c>.
/// </summary>
public sealed class Library
{
    public Library(LibraryId id, string name, string text)
    {
        Id = id;
        Name = name;
        Text = text;
    }

    public LibraryId Id { get; }

    /// <summary>
    /// What a Script writes before the dot; see <see cref="LibraryName"/>. Changed through <see cref="LibraryList"/>.
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// Changed through <see cref="LibraryList"/>.
    /// </summary>
    public string Text { get; internal set; }
}
