using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Presses the keys in the same frame, except that each Escape after the first waits for the next frame: Unity reports
/// one Escape a frame, and the Script editor only handles the one it reported.
/// </summary>
internal sealed class PressStep : KeysStep
{
    public PressStep(IReadOnlyList<KeyPress> keys)
    {
        Keys = keys;
    }

    public IReadOnlyList<KeyPress> Keys { get; }

    protected override IEnumerable<IReadOnlyList<KeyPress>> GetFrames()
    {
        List<KeyPress> frame = [];
        foreach (var key in Keys)
        {
            if (key.IsEscape && frame.Exists(k => k.IsEscape))
            {
                yield return frame;
                frame = [];
            }

            frame.Add(key);
        }

        yield return frame;
    }
}
