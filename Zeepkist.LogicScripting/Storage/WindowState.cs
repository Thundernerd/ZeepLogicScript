namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// What the Script window looked like when it was last used, as its JSON file holds it. Imui keeps none of this
/// between sessions. A zero width or height means the window was never placed.
/// </summary>
internal sealed class WindowState
{
    public bool Open { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public bool ScriptsCollapsed { get; set; }
    public bool LevelCollapsed { get; set; }
}
