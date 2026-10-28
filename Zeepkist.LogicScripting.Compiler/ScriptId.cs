using System;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// A GUID made when the Script is created, so renaming it never loses its Block Group (ADR 0005).
/// </summary>
public sealed class ScriptId : IEquatable<ScriptId>
{
    private readonly string _value;

    public ScriptId(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("A Script id can't be empty", nameof(value));

        _value = value;
    }

    public static ScriptId New()
    {
        return new ScriptId(Guid.NewGuid().ToString());
    }

    public static bool operator ==(ScriptId left, ScriptId right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ScriptId left, ScriptId right)
    {
        return !Equals(left, right);
    }

    public bool Equals(ScriptId other)
    {
        return other is not null && _value == other._value;
    }

    public override bool Equals(object obj)
    {
        return obj is ScriptId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    /// <summary>
    /// The text the level stores.
    /// </summary>
    public override string ToString()
    {
        return _value;
    }
}
