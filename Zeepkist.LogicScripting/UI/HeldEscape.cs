namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Escape Unity reported in a frame, held until Imui's events of it, so it's handled in the order the keys were
/// pressed. Imui collects key events after the frame is drawn, so the events come a frame after Unity reports the key.
/// Unity reports one Escape a frame however often it was pressed, so every Escape event of that next frame belongs to it.
/// </summary>
internal sealed class HeldEscape
{
    private int? _heldIn;
    private int? _releasedIn;

    /// <summary>
    /// Holds the Escape Unity reported in <paramref name="frame"/>, unless one is held already.
    /// </summary>
    public bool Hold(int frame)
    {
        if (_heldIn != null)
            return false;

        _heldIn = frame;
        return true;
    }

    /// <summary>
    /// Whether an Escape key event in <paramref name="frame"/> belongs to the held Escape, which it releases.
    /// </summary>
    public bool Release(int frame)
    {
        if (frame == _releasedIn)
            return true;

        // An event in the same frame comes from an earlier press
        if (_heldIn == null || frame <= _heldIn)
            return false;

        _heldIn = null;
        _releasedIn = frame;
        return true;
    }

    /// <summary>
    /// Whether the held Escape's event should have come before <paramref name="frame"/>, as when something else used
    /// it, so it's handled now; it's no longer held after.
    /// </summary>
    public bool Expire(int frame)
    {
        if (_heldIn == null || frame <= _heldIn + 1)
            return false;

        _heldIn = null;
        return true;
    }

    /// <summary>
    /// Forgets the held Escape without handling it, as when what held it lost focus before its event.
    /// </summary>
    public void Drop()
    {
        _heldIn = null;
    }
}
