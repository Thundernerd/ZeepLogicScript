namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// A Draft as its JSON file holds it; the Script's id is in the file name.
/// </summary>
internal sealed class DraftFile
{
    public string Name { get; set; }
    public string Text { get; set; }
    public string BaseHash { get; set; }
}
