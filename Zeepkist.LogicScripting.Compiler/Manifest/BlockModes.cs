using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// A Setting whose option changes what a block's outputs mean, and the output names for each option.
/// </summary>
public sealed class BlockModes
{
    public BlockModes(BlockSetting setting, params IReadOnlyList<BlockConnector>[] outputs)
    {
        Setting = setting;
        Outputs = outputs;
    }

    public BlockSetting Setting { get; }

    /// <summary>
    /// The outputs per mode, in the order of <see cref="BlockSetting.Options"/>.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<BlockConnector>> Outputs { get; }
}
