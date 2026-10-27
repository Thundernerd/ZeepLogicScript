namespace TNRD.Zeepkist.LogicScripting.Compiler.Layout;

/// <summary>
/// A position on the editor's 16-unit block grid, relative to the group's centre.
/// </summary>
public readonly struct GridCell
{
    public const float Size = 16;

    public GridCell(int x, int z)
    {
        X = x;
        Z = z;
    }

    public int X { get; }
    public int Z { get; }

    public override string ToString()
    {
        return $"({X}, {Z})";
    }
}
