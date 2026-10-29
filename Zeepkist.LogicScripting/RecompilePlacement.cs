using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting;

internal sealed class RecompilePlacement : CompilePlacement
{
    // How far a block may be off Compile's scale and level before it counts as reshaped by hand
    private const float ScaleTolerance = 0.001f;
    private const float TiltToleranceDegrees = 0.1f;

    private readonly List<BlockProperties> _group;

    /// <param name="group">The Script's Block Group; not empty.</param>
    public RecompilePlacement(List<BlockProperties> group)
    {
        _group = group;

        // The carrier's yaw, so hand-turning other blocks of the group never turns a recompile; the majority without one
        // or when the carrier itself was reshaped
        var blocks = group.Select(ToGroupBlock).ToList();
        var carrier = group.FindIndex(LevelScripts.IsCarrier);
        var yawBlocks = carrier >= 0 && !blocks[carrier].IsReshaped ? [blocks[carrier]] : blocks;
        QuarterTurns = BlockGroupPlacement.GetMajorityQuarterTurns(yawBlocks);
    }

    public override IReadOnlyList<BlockProperties> Group => _group;
    public override int QuarterTurns { get; }

    /// <summary>
    /// Even an unchanged Script is placed again, which undoes hand edits to its blocks.
    /// </summary>
    public override bool ChangesNothing(CompileResult result)
    {
        return false;
    }

    /// <summary>
    /// The new group's start corner lands on the old group's, at the old group's lowest height.
    /// </summary>
    public override Vector3 GetOrigin(CompileResult result)
    {
        var oldBlocks = _group.Select(ToGroupBlock).ToList();
        var (x, z) = BlockGroupPlacement.GetOrigin(oldBlocks, result.Blocks, QuarterTurns);
        return new Vector3(x, _group.Min(b => b.transform.position.y), z);
    }

    private static GroupBlock ToGroupBlock(BlockProperties block)
    {
        var position = block.transform.position;
        var style = BlockManifest.TryGetBlock(block.blockID, out _, out var found) ? found : null;
        return new GroupBlock(position.x, position.z, block.transform.eulerAngles.y, style, IsReshaped(block));
    }

    /// <summary>
    /// Whether the block is scaled or tilted away from how <see cref="CompiledBlockJson"/> places it.
    /// </summary>
    private static bool IsReshaped(BlockProperties block)
    {
        var scale = block.transform.localScale;
        var isScaled = Mathf.Abs(scale.x - 1) > ScaleTolerance
            || Mathf.Abs(scale.y - 1) > ScaleTolerance
            || Mathf.Abs(scale.z - 1) > ScaleTolerance;
        return isScaled || Vector3.Angle(block.transform.up, Vector3.up) > TiltToleranceDegrees;
    }

    protected override string NotStoredNote => "the Script is no longer stored in the level";

    protected override string GetPlacedLine(int blockCount)
    {
        return $"Replaced {_group.Count} → {blockCount} blocks";
    }
}
