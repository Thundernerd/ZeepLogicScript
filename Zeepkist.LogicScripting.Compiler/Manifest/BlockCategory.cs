using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// The heading a block type is listed under in the Reference tab. These follow the subfolders of the game's own logic
/// block folder, so players find a block where the block browser already shows it.
/// </summary>
public sealed class BlockCategory
{
    public static readonly BlockCategory Inputs = new("Inputs");
    public static readonly BlockCategory Operators = new("Operators");
    public static readonly BlockCategory Outputs = new("Outputs");
    public static readonly BlockCategory Movers = new("Movers");

    private BlockCategory(string name)
    {
        Name = name;
    }

    /// <summary>
    /// In the order the Reference tab shows them, which is the block browser's.
    /// </summary>
    public static IReadOnlyList<BlockCategory> All { get; } = [Inputs, Operators, Outputs, Movers];

    public string Name { get; }
}
