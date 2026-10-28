using System;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// The editor's text with its caret and selection, as <see cref="EditHistory"/> stores it.
/// </summary>
public readonly struct TextSnapshot : IEquatable<TextSnapshot>
{
    private readonly string _text;

    public TextSnapshot(string text, int caret, int selection = 0)
    {
        _text = text;
        Caret = caret;
        Selection = selection;
    }

    // Also covers default(TextSnapshot), which skips the constructor
    public string Text => _text ?? string.Empty;
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
            return ((Text.GetHashCode() * 397) ^ Caret) * 397 ^ Selection;
        }
    }

    public override string ToString()
    {
        return $"\"{Text}\" caret {Caret} selection {Selection}";
    }
}
