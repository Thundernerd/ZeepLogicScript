namespace TNRD.Zeepkist.LogicScripting.UI;

internal enum DeleteChoice
{
    None,

    /// <summary>
    /// Delete the Script or Library, and a Script's blocks with it.
    /// </summary>
    Delete,

    /// <summary>
    /// Untag the Script's blocks, leaving them in the level as plain Logic Blocks.
    /// </summary>
    KeepBlocks
}
