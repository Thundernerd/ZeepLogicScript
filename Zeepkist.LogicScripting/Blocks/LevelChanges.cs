namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Counts the editor's block changes, so the Scripts the level holds are only read again after one.
/// </summary>
internal static class LevelChanges
{
    public static int Count { get; private set; }

    public static void Add()
    {
        Count++;
    }
}
