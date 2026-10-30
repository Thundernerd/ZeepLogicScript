using System;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// A GUID made when the Library is created, so the player's store keeps track of it through a rename.
/// </summary>
public sealed class LibraryId : IEquatable<LibraryId>
{
    private readonly string _value;

    public LibraryId(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("A Library id can't be empty", nameof(value));

        _value = value;
    }

    public static LibraryId New()
    {
        return new LibraryId(Guid.NewGuid().ToString());
    }

    public static bool operator ==(LibraryId left, LibraryId right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(LibraryId left, LibraryId right)
    {
        return !Equals(left, right);
    }

    public bool Equals(LibraryId other)
    {
        return other is not null && _value == other._value;
    }

    public override bool Equals(object obj)
    {
        return obj is LibraryId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override string ToString()
    {
        return _value;
    }
}
