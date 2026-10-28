using System;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Layout;

/// <summary>
/// A yaw in 90° steps, as blocks in a Block Group are turned.
/// </summary>
internal static class QuarterTurns
{
    /// <summary>
    /// The same turn as 0 to 3 quarter turns.
    /// </summary>
    public static int Normalize(int quarterTurns)
    {
        return ((quarterTurns % 4) + 4) % 4;
    }

    /// <summary>
    /// The nearest number of quarter turns to a yaw in degrees, from 0 to 3.
    /// </summary>
    public static int FromDegrees(double degrees)
    {
        return Normalize((int)Math.Round(degrees / 90));
    }

    /// <summary>
    /// Rotates a point about the Y axis the way a block's yaw does: +Z turns towards +X.
    /// </summary>
    public static (float X, float Z) Rotate(float x, float z, int quarterTurns)
    {
        return Normalize(quarterTurns) switch
        {
            1 => (z, -x),
            2 => (-x, -z),
            3 => (-z, x),
            _ => (x, z)
        };
    }
}
