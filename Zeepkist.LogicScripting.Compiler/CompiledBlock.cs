using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

public sealed class CompiledBlock
{
    public CompiledBlock(BlockType type, IReadOnlyList<Setting> settings, GridCell cell, int yaw)
    {
        Type = type;
        Settings = settings;
        Cell = cell;
        Yaw = yaw;
    }

    public BlockType Type { get; }
    public IReadOnlyList<Setting> Settings { get; }

    /// <summary>
    /// The block's pivot cell, in world axes, relative to the group's centre.
    /// </summary>
    public GridCell Cell { get; }

    /// <summary>
    /// Rotation about Y in degrees.
    /// </summary>
    public int Yaw { get; }
}
