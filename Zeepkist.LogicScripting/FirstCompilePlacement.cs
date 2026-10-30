using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting;

internal sealed class FirstCompilePlacement : CompilePlacement
{
    private readonly LEV_LevelEditorCentral _central;

    public FirstCompilePlacement(LEV_LevelEditorCentral central)
    {
        _central = central;
        var right = central.cam.cameraTransform.right;
        QuarterTurns = ScriptCompiler.GetQuarterTurnsFacing(right.x, right.z);
    }

    public override IReadOnlyList<BlockProperties> Group => [];
    public override int QuarterTurns { get; }

    public override bool ChangesNothing(CompileResult result)
    {
        return result.Blocks.Count == 0 && result.PlacedWires.Count == 0;
    }

    /// <summary>
    /// Same point as the game's own new-block placement, but the new-block height is only read, never changed.
    /// </summary>
    public override Vector3 GetOrigin(CompileResult result)
    {
        var camera = _central.cam.transform.position;
        var point = camera + Vector3.Scale(_central.cam.cameraTransform.forward * 80, new Vector3(1, 0, 1));
        var height = _central.gizmos.newBlockHeight;
        point.y = camera.y < height ? camera.y - 40 : height;
        return new Vector3(
            Mathf.Round(point.x / 16) * 16,
            Mathf.Max(0, Mathf.Round(point.y / 8) * 8),
            Mathf.Round(point.z / 16) * 16);
    }

    protected override string NotStoredNote => "A Script that places no blocks isn't stored in the level";

    protected override string GetPlacedLine(int blockCount)
    {
        return $"Placed {blockCount} blocks";
    }
}
