using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using UnityEngine;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting;

/// <summary>
/// Compiles a Script and places its blocks in the open level: a first Compile in front of the camera, a recompile in
/// place of the Script's Block Group. The Script's name and text go into the level with its blocks, in the same undo
/// step (ADR 0006).
/// </summary>
internal static class ScriptCompile
{
    private const string UndoLabel = "Logic Scripting Compile";

    // Its text is stored on one of its blocks
    private const string NotStoredMessage = "a Script that places no blocks isn't stored in the level";

    /// <summary>
    /// Returns the result to show in the window: one line, or the error list. The new blocks replace the Script's Block
    /// Group.
    /// </summary>
    public static string Run(StoredScript script, StartupCheck check, ManualLogSource logger)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
            return "Open the level editor to compile";

        if (central.gizmos.isGrabbing)
            return "Let go of the grabbed blocks to compile";

        var group = BlockGroups.Find(central, script.Id);
        var right = central.cam.cameraTransform.right;
        var quarterTurns = group.Count > 0
            ? BlockGroupPlacement.GetMajorityQuarterTurns(group.Select(b => b.transform.eulerAngles.y))
            : ScriptCompiler.GetQuarterTurnsFacing(right.x, right.z);

        var result = Check(central, script.Text, script.Id, check, quarterTurns);
        if (!result.Succeeded)
            return FormatErrors(result);

        if (!check.CanPlace)
            return $"No errors, but {StartupCheck.UpdateNeededMessage}; nothing placed";

        if (result.Blocks.Count == 0 && result.PlacedWires.Count == 0 && group.Count == 0)
            return $"Nothing to place; {NotStoredMessage}";

        int lostWires;
        try
        {
            var origin = group.Count > 0 ? GetGroupOrigin(group, result, quarterTurns) : GetPlacementPoint(central);
            var jsons = CompiledBlockJson.Create(result, origin, script);
            var placedWires = CompiledBlockJson.CreatePlacedWires(result, jsons);
            lostWires = BlockGroups.CountWiresNotRemade(group, placedWires);
            BlockBatch.Apply(central, jsons, group, placedWires, UndoLabel);
        }
        catch (Exception e)
        {
            logger.LogError($"Compile failed: {e}");
            return "Compile failed; level unchanged";
        }

        var line = group.Count == 0
            ? $"Placed {result.Blocks.Count} blocks"
            : $"Replaced {group.Count} → {result.Blocks.Count} blocks";

        var wiredBlocks = result.WiredPlacedBlockCount;
        if (wiredBlocks > 0)
            line += $", wired to {wiredBlocks} placed blocks";

        if (result.Blocks.Count == 0)
            line += group.Count > 0 ? "; the Script is no longer stored in the level" : $"; {NotStoredMessage}";

        if (lostWires > 0)
            line += $"\nRemoved {lostWires} Wires between the group and other blocks that this Compile doesn't make again; to keep a hand-made one, wire the block in the Script with placed(...)";

        return line;
    }

    /// <summary>
    /// Compiles without placing anything, for the Placed blocks panel; null outside the editor.
    /// </summary>
    public static CompileResult Check(string text, string scriptId, StartupCheck check)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        return central == null ? null : Check(central, text, scriptId, check, 0);
    }

    private static CompileResult Check(LEV_LevelEditorCentral central, string text, string scriptId, StartupCheck check, int quarterTurns)
    {
        check.RunOnce();
        return ScriptCompiler.Compile(text, quarterTurns, check.DisabledBlockIds, uid => PlacedBlocks.Find(central, uid, scriptId));
    }

    /// <summary>
    /// Where the new group's centre cell goes so its start corner lands on the old group's, at the old group's
    /// lowest height.
    /// </summary>
    private static Vector3 GetGroupOrigin(List<BlockProperties> group, CompileResult result, int quarterTurns)
    {
        var positions = group.Select(b => b.transform.position).ToList();
        var (x, z) = BlockGroupPlacement.GetOrigin(
            positions.Select(p => (p.x, p.z)).ToList(),
            result.Blocks.Select(b => b.Cell).ToList(),
            quarterTurns);

        return new Vector3(x, positions.Min(p => p.y), z);
    }

    private static string FormatErrors(CompileResult result)
    {
        var heading = result.Errors.Count == 1 ? "1 error; nothing placed" : $"{result.Errors.Count} errors; nothing placed";
        return heading + "\n" + string.Join("\n", result.Errors);
    }

    /// <summary>
    /// Same point as the game's own new-block placement, but the new-block height is only read, never changed.
    /// </summary>
    private static Vector3 GetPlacementPoint(LEV_LevelEditorCentral central)
    {
        var camera = central.cam.transform.position;
        var point = camera + Vector3.Scale(central.cam.cameraTransform.forward * 80, new Vector3(1, 0, 1));
        var height = central.gizmos.newBlockHeight;
        point.y = camera.y < height ? camera.y - 40 : height;
        return new Vector3(
            Mathf.Round(point.x / 16) * 16,
            Mathf.Max(0, Mathf.Round(point.y / 8) * 8),
            Mathf.Round(point.z / 16) * 16);
    }
}
