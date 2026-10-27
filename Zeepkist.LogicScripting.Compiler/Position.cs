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
    /// Whether this is the next column after <paramref name="other"/> on the same line.
    /// </summary>
    public bool IsDirectlyAfter(Position other)
    {
        return Line == other.Line && Column == other.Column + 1;
    }

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
