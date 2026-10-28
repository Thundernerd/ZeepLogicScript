namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// The tag each compiled block carries in its string Settings, <c>"&lt;scriptId&gt;:&lt;ownUID&gt;"</c> (ADR 0005).
/// A block whose tag names another UID is a pasted copy, not a member of the group.
/// </summary>
public static class BlockGroupTag
{
    /// <summary>
    /// Must not start with "id": the game removes keys that do when it saves a block's Wires.
    /// </summary>
    public const string Key = "zls_g";

    private const char Separator = ':';

    public static string Create(ScriptId scriptId, string uid)
    {
        return scriptId.ToString() + Separator + uid;
    }

    /// <summary>
    /// The Script whose Block Group a block with this tag belongs to, or null when the block is no member: it has no
    /// tag, or the tag names another block.
    /// </summary>
    public static ScriptId GetScriptId(string tag, string uid)
    {
        if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(uid))
            return null;

        // A UID can hold any character, so the split is at the first separator: the Script id is a GUID
        var separator = tag.IndexOf(Separator);
        if (separator <= 0 || tag.Substring(separator + 1) != uid)
            return null;

        return new ScriptId(tag.Substring(0, separator));
    }
}
