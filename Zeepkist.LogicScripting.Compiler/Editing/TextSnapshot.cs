using System;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// The editor's text with its caret and selection, as <see cref="EditHistory"/> stores it.
/// </summary>
public readonly struct TextSnapshot : IEquatable<TextSnapshot>
{
    public TextSnapshot(string text, int caret, int selection = 0)
    {
        Text = text ?? string.Empty;
        Caret = caret;
        Selection = selection;
    }

    public string Text { get; }
    public int Caret { get; }

    /// <summary>
    /// The selected length counted from <see cref="Caret"/>; negative when it lies before the caret.
    /// </summary>
    public int Selection { get; }

    public bool Equals(TextSnapshot other)
    {
        return Text == other.Text && Caret == other.Caret && Selection == other.Selection;
    }

    public override bool Equals(object obj)
    {
        return obj is TextSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (((Text?.GetHashCode() ?? 0) * 397) ^ Caret) * 397 ^ Selection;
        }
    }

    public override string ToString()
    {
        return $"\"{Text}\" caret {Caret} selection {Selection}";
    }
}
