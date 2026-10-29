using Newtonsoft.Json;

namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// What the Script window looked like when it was last used, as its JSON file holds it. Imui keeps none of this
/// between sessions. A zero width or height means the window was never placed.
/// </summary>
/// <remarks>
/// A file saved before the panels had a <see cref="PanelState"/> each has none of their keys, so its panels load with
/// the default layout. The Blocks panel was saved as Level before it was renamed, which <see cref="LegacyLevel"/> still reads.
/// </remarks>
internal sealed class WindowState
{
    public bool Open { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public PanelState Scripts { get; set; } = new();
    public PanelState Blocks { get; set; } = new();
    public PanelState Errors { get; set; } = new();

    [JsonProperty("Level")]
    private PanelState LegacyLevel
    {
        set => Blocks = value;
    }

    public WindowState Copy()
    {
        var copy = (WindowState)MemberwiseClone();
        copy.Scripts = Scripts.Copy();
        copy.Blocks = Blocks.Copy();
        copy.Errors = Errors.Copy();
        return copy;
    }

    public bool IsSameAs(WindowState other)
    {
        return Open == other.Open
            && X == other.X
            && Y == other.Y
            && Width == other.Width
            && Height == other.Height
            && Scripts.IsSameAs(other.Scripts)
            && Blocks.IsSameAs(other.Blocks)
            && Errors.IsSameAs(other.Errors);
    }
}
