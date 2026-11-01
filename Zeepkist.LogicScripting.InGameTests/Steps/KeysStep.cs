using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Sends keys to the focused field through Imui, a group of them each frame, and waits until the field has handled
/// them. An Escape is also held for the Script editor, as Unity's <c>Input</c> would report it.
/// </summary>
internal abstract class KeysStep : ScenarioStep
{
    /// <summary>
    /// Imui's backend keeps 16 key events a frame, and each key is two.
    /// </summary>
    public const int MaxKeysPerFrame = 8;

    protected abstract IEnumerable<IReadOnlyList<KeyPress>> GetFrames();

    public override IEnumerator Run(StepContext context)
    {
        if (!context.RequireFocusedField())
            yield break;

        foreach (var keys in GetFrames())
        {
            if (!ImuiKeys.Queue(keys))
            {
                context.Fail("Imui has no backend to send keys to");
                yield break;
            }

            if (keys.Any(k => k.IsEscape))
                context.Window.HoldSentEscape();

            yield return null;
        }

        // The field handles them the frame after; the window reacts to what changed after that
        yield return null;
    }
}
