using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// The column the names of a list of <see cref="ReferenceRow"/>s go in, with each value beside its name, as the
/// window's Reference tab and the hover box both lay them out.
/// </summary>
public sealed class NameColumn
{
    /// <summary>
    /// The most of the width the column takes, so the values beside it still have room.
    /// </summary>
    public const float MaxShare = 0.45f;

    private readonly float[] _nameWidths;
    private readonly float _gap;
    private readonly float _maxWidth;

    private NameColumn(float[] nameWidths, float gap, float maxWidth, float width)
    {
        _nameWidths = nameWidths;
        _gap = gap;
        _maxWidth = maxWidth;
        Width = width;
    }

    /// <summary>
    /// As wide as the longest name that fits, plus the gap.
    /// </summary>
    public float Width { get; }

    /// <param name="rows"></param>
    /// <param name="measure">The width a name takes when drawn.</param>
    /// <param name="gap">The room between a name and its value.</param>
    /// <param name="availableWidth">The width the rows are laid out in.</param>
    public static NameColumn Fit(IReadOnlyList<ReferenceRow> rows, Func<string, float> measure, float gap, float availableWidth)
    {
        var maxWidth = availableWidth * MaxShare;
        var nameWidths = new float[rows.Count];
        var width = 0f;
        for (var i = 0; i < rows.Count; i++)
        {
            nameWidths[i] = measure(rows[i].Name);
            if (nameWidths[i] + gap <= maxWidth)
                width = Math.Max(width, nameWidths[i] + gap);
        }

        return new NameColumn(nameWidths, gap, maxWidth, width);
    }

    /// <summary>
    /// Whether the name of row <paramref name="index"/> fits the column. One that doesn't goes on a line of its own,
    /// with its value on the next.
    /// </summary>
    public bool Fits(int index)
    {
        return _nameWidths[index] + _gap <= _maxWidth;
    }

    /// <summary>
    /// The name of row <paramref name="index"/> alone, without the gap.
    /// </summary>
    public float GetNameWidth(int index)
    {
        return _nameWidths[index];
    }
}
