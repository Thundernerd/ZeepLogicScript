using System;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// A one-based line and column in the Script.
/// </summary>
public readonly struct Position : IComparable<Position>
{
    public Position(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public int Line { get; }
    public int Column { get; }

    /// <summary>
    /// Orders by line, then by column: the order positions appear in the Script.
    /// </summary>
    public int CompareTo(Position other)
    {
        var line = Line.CompareTo(other.Line);
        return line != 0 ? line : Column.CompareTo(other.Column);
    }

    public override string ToString()
    {
        return $"{Line}:{Column}";
    }
}
