using TNRD.Zeepkist.LogicScripting.Compiler.Storage;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// How the Script list and the centre header show a <see cref="ScriptMark"/>.
/// </summary>
internal static class ScriptMarkText
{
    /// <summary>
    /// Nulls for <see cref="ScriptMark.None"/>. The font has no ⚠ or ◌, so the carrier-missing and never-compiled
    /// glyphs are ! and ○.
    /// </summary>
    public static (string Glyph, string Label, string Tooltip) Get(ScriptMark mark)
    {
        return mark switch
        {
            ScriptMark.NotCompiled => ("●", "not compiled", "Not compiled: the level holds another name or text"),
            ScriptMark.CarrierMissing => ("!", "text missing from the level", "The level has this Script's blocks but not its text; Compile to store it again"),
            ScriptMark.NeverCompiled => ("○", "never compiled", "Never compiled: the level doesn't hold this Script"),
            _ => (null, null, null)
        };
    }
}
