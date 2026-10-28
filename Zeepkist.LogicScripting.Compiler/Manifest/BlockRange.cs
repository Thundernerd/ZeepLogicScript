namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// Two Number Settings that make a range, where the block reads a minimum above the maximum as some other range
/// than the Script suggests.
/// </summary>
public sealed class BlockRange
{
    public BlockRange(BlockSetting minimum, BlockSetting maximum, string maximumInput)
    {
        Minimum = minimum;
        Maximum = maximum;
        MaximumInput = maximumInput;
    }

    public BlockSetting Minimum { get; }
    public BlockSetting Maximum { get; }

    /// <summary>
    /// The input that replaces <see cref="Maximum"/> while it's non-zero. With it wired, <see cref="Maximum"/> is only
    /// a fallback, so the range isn't checked.
    /// </summary>
    public string MaximumInput { get; }
}
