namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// One collapsible, resizable panel of the Script window: whether it's collapsed to its header, and its size along
/// the axis it's resized on. A zero size means the panel was never resized.
/// </summary>
internal sealed class PanelState
{
    public bool Collapsed { get; set; }
    public float Size { get; set; }

    public PanelState Copy()
    {
        return (PanelState)MemberwiseClone();
    }

    public bool IsSameAs(PanelState other)
    {
        return Collapsed == other.Collapsed && Size == other.Size;
    }
}
