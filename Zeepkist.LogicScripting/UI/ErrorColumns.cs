namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The widths of the error table's number columns, padding included, shared by its header and its rows so they line
/// up.
/// </summary>
internal readonly struct ErrorColumns
{
    public ErrorColumns(float line, float column)
    {
        Line = line;
        Column = column;
    }

    public float Line { get; }
    public float Column { get; }
}
