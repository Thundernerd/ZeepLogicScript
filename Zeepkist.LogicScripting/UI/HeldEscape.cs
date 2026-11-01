namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// An Escape Unity reported, held until Imui's event of it, so it's handled in the order the keys were pressed. Imui
/// collects key events after the frame is drawn, so the event comes a frame after Unity reports the key.
/// </summary>
internal sealed class HeldEscape
{
    private int? _frame;

    /// <summary>
    /// Holds the Escape Unity reported in <paramref name="frame"/>, unless one is held already.
    /// </summary>
    public bool Hold(int frame)
    {
        if (_frame != null)
            return false;

        _frame = frame;
        return true;
    }

    /// <summary>
    /// Whether an Escape key event in <paramref name="frame"/> belongs to the held Escape, which it releases.
    /// </summary>
    public bool Release(int frame)
    {
        // An event in the same frame comes from an earlier press
        if (_frame == null || frame <= _frame)
            return false;

        _frame = null;
        return true;
    }

    /// <summary>
    /// Whether the held Escape's event should have come before <paramref name="frame"/>, as when something else used
    /// it, so it's handled now; it's no longer held after.
    /// </summary>
    public bool Expire(int frame)
    {
        if (_frame == null || frame <= _frame + 1)
            return false;

        _frame = null;
        return true;
    }

    /// <summary>
    /// Forgets the held Escape without handling it, as when what held it lost focus before its event.
    /// </summary>
    public void Drop()
    {
        _frame = null;
    }
}
