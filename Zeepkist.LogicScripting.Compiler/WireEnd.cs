using System;
using System.Globalization;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// One end of a <see cref="CompiledWire"/>: a block the Compile places, or one already in the level.
/// </summary>
public sealed class WireEnd
{
    private readonly int _block;
    private readonly string _uid;

    private WireEnd(int block, string uid)
    {
        _block = block;
        _uid = uid;
    }

    /// <summary>
    /// The block's index in <see cref="CompileResult.Blocks"/>; only a block the Compile places has one.
    /// </summary>
    public int Block => IsPlaced
        ? throw new InvalidOperationException($"Block {_uid} is already in the level, so it has no index")
        : _block;

    /// <summary>
    /// The placed block's UID; only a block already in the level has one.
    /// </summary>
    public string Uid => IsPlaced
        ? _uid
        : throw new InvalidOperationException($"Block {_block} is one the Compile places, so it has no UID yet");

    public bool IsPlaced => _uid != null;

    public static WireEnd Compiled(int block)
    {
        return new WireEnd(block, null);
    }

    public static WireEnd Placed(string uid)
    {
        return new WireEnd(0, uid ?? throw new ArgumentNullException(nameof(uid)));
    }

    /// <summary>
    /// The UID of a placed block, or the index of a block the Compile places.
    /// </summary>
    public override string ToString()
    {
        return IsPlaced ? _uid : _block.ToString(CultureInfo.InvariantCulture);
    }
}
