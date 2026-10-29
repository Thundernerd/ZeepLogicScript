using System;
using System.Linq;
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
    // Every panel, so copying, comparing and loading can't miss one when a panel is added or renamed
    private static readonly (Func<WindowState, PanelState> Get, Action<WindowState, PanelState> Set)[] _panels =
    [
        (state => state.Scripts, (state, panel) => state.Scripts = panel),
        (state => state.Blocks, (state, panel) => state.Blocks = panel),
        (state => state.Errors, (state, panel) => state.Errors = panel),
    ];

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
        foreach (var (get, set) in _panels)
            set(copy, get(this).Copy());

        return copy;
    }

    public bool IsSameAs(WindowState other)
    {
        return Open == other.Open
            && X == other.X
            && Y == other.Y
            && Width == other.Width
            && Height == other.Height
            && _panels.All(panel => panel.Get(this).IsSameAs(panel.Get(other)));
    }

    /// <summary>
    /// Gives each panel the default layout when the file set it to null, which overrides the property's default.
    /// </summary>
    public void FillMissingPanels()
    {
        foreach (var (get, set) in _panels)
        {
            if (get(this) == null)
                set(this, new PanelState());
        }
    }
}
