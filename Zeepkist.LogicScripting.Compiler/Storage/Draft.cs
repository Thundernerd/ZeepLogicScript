using System.Security.Cryptography;
using System.Text;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// A Script's latest name and text, kept on the player's own machine, with a hash of the level's text it was typed over
/// (ADR 0006).
/// </summary>
public sealed class Draft
{
    public Draft(ScriptId id, string name, string text, string baseHash)
    {
        Id = id;
        Name = name;
        Text = text;
        BaseHash = baseHash;
    }

    public ScriptId Id { get; }
    public string Name { get; }
    public string Text { get; }

    /// <summary>
    /// The hash of the text the level held for the Script, or null when it held none.
    /// </summary>
    public string BaseHash { get; }

    public static Draft For(ScriptEntry entry)
    {
        return new Draft(entry.Id, entry.Name, entry.Text, HashOf(entry.Stored?.Text));
    }

    /// <summary>
    /// SHA-256 of the UTF-8 text in hex, so it stays the same across sessions and machines; null for null.
    /// </summary>
    public static string HashOf(string text)
    {
        if (text == null)
            return null;

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Whether the Draft was typed over the text the level holds now, so it's newer than the level and not older.
    /// </summary>
    public bool IsBasedOn(StoredScript stored)
    {
        return BaseHash != null && BaseHash == HashOf(stored.Text);
    }

    public bool HasSameContent(Draft other)
    {
        return other != null && Id == other.Id && Name == other.Name && Text == other.Text && BaseHash == other.BaseHash;
    }
}
