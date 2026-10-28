using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting;

internal sealed class RecompilePlacement : CompilePlacement
{
    private readonly List<BlockProperties> _group;

    /// <param name="group">The Script's Block Group; not empty.</param>
    public RecompilePlacement(List<BlockProperties> group)
    {
        _group = group;
        QuarterTurns = BlockGroupPlacement.GetMajorityQuarterTurns(group.Select(b => b.transform.eulerAngles.y));
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
        var positions = _group.Select(b => b.transform.position).ToList();
        var (x, z) = BlockGroupPlacement.GetOrigin(
            positions.Select(p => (p.x, p.z)).ToList(),
            result.Blocks.Select(b => b.Cell).ToList(),
            QuarterTurns);

        return new Vector3(x, positions.Min(p => p.y), z);
    }

    protected override string NotStoredNote => "the Script is no longer stored in the level";

    protected override string GetPlacedLine(int blockCount)
    {
        return $"Replaced {_group.Count} → {blockCount} blocks";
    }
}
