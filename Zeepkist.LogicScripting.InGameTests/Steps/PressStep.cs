using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Presses the keys in the same frame.
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
        yield return Keys;
    }
}
