namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// One Library in the <see cref="LibrariesFile"/>.
/// </summary>
internal sealed class LibraryFileEntry
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Text { get; set; }
}
