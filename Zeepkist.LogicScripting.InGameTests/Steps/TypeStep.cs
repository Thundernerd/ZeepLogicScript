using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Types the text one key a frame.
/// </summary>
internal sealed class TypeStep : KeysStep
{
    public TypeStep(string text)
    {
        Text = text;
    }

    public string Text { get; }

    protected override IEnumerable<IReadOnlyList<KeyPress>> GetFrames()
    {
        foreach (var character in Text)
        {
            yield return [KeyNames.ForCharacter(character)];
        }
    }
}
