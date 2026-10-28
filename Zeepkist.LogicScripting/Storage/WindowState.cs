namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// What the Script window looked like when it was last used, as its JSON file holds it. Imui keeps none of this
/// between sessions. A zero width or height means the window was never placed, and a zero panel size that the panel
/// was never resized.
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
    public float ScriptsWidth { get; set; }
    public float LevelWidth { get; set; }
    public float ErrorsHeight { get; set; }
    public bool ErrorsHidden { get; set; }

    public WindowState Copy()
    {
        return (WindowState)MemberwiseClone();
    }

    public bool IsSameAs(WindowState other)
    {
        return Open == other.Open
            && X == other.X
            && Y == other.Y
            && Width == other.Width
            && Height == other.Height
            && ScriptsCollapsed == other.ScriptsCollapsed
            && LevelCollapsed == other.LevelCollapsed
            && ScriptsWidth == other.ScriptsWidth
            && LevelWidth == other.LevelWidth
            && ErrorsHeight == other.ErrorsHeight
            && ErrorsHidden == other.ErrorsHidden;
    }
}
