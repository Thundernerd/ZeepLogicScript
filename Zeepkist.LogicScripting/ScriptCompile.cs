using System;
using System.Linq;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using UnityEngine;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting;

/// <summary>
/// Compiles a Script and places its blocks in the open level, in front of the camera.
/// </summary>
internal static class ScriptCompile
{
    private const string UndoLabel = "Logic Scripting Compile";

    /// <summary>
    /// Returns the result to show in the window: one line, or the error list. When the startup check found the
    /// game's block creation changed, the Script is still checked, but nothing is placed.
    /// </summary>
    public static string Run(string text, StartupCheck check, ManualLogSource logger)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
            return "Open the level editor to compile";

        if (central.gizmos.isGrabbing)
            return "Let go of the grabbed blocks to compile";

        var right = central.cam.cameraTransform.right;
        var result = Check(central, text, check, ScriptCompiler.GetQuarterTurnsFacing(right.x, right.z));
        if (!result.Succeeded)
            return FormatErrors(result);

        if (!check.CanPlace)
            return $"No errors, but {StartupCheck.UpdateNeededMessage}; nothing placed";

        if (result.Blocks.Count == 0 && result.PlacedWires.Count == 0)
            return "Nothing to place";

        try
        {
            var jsons = CompiledBlockJson.Create(result, GetPlacementPoint(central));
            BlockBatch.Apply(central, jsons, CompiledBlockJson.CreatePlacedWires(result, jsons), UndoLabel);
        }
        catch (Exception e)
        {
            logger.LogError($"Compile failed: {e}");
            return "Compile failed; level unchanged";
        }

        var wiredBlocks = result.PlacedWires
            .SelectMany(w => new[] { w.From.Uid, w.To.Uid })
            .Where(uid => uid != null)
            .Distinct()
            .Count();
        return wiredBlocks == 0
            ? $"Placed {result.Blocks.Count} blocks"
            : $"Placed {result.Blocks.Count} blocks, wired to {wiredBlocks} placed blocks";
    }

    /// <summary>
    /// Compiles without placing anything, for the Placed blocks panel; null outside the editor.
    /// </summary>
    public static CompileResult Check(string text, StartupCheck check)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        return central == null ? null : Check(central, text, check, 0);
    }

    private static CompileResult Check(LEV_LevelEditorCentral central, string text, StartupCheck check, int quarterTurns)
    {
        check.RunOnce();
        return ScriptCompiler.Compile(text, quarterTurns, check.DisabledBlockIds, uid => PlacedBlocks.Find(central, uid));
    }

    /// <summary>
    /// A heading line, then every error as "line:column: message", one per line.
    /// </summary>
    private static string FormatErrors(CompileResult result)
    {
        var heading = result.Errors.Count == 1 ? "1 error; nothing placed" : $"{result.Errors.Count} errors; nothing placed";
        return heading + "\n" + string.Join("\n", result.Errors);
    }

    /// <summary>
    /// Same rule as the game's own new-block placement: 80 units ahead on the flattened view direction,
    /// snapped to 16 (8 vertically), y >= 0.
    /// </summary>
    private static Vector3 GetPlacementPoint(LEV_LevelEditorCentral central)
    {
        var forward = Vector3.Scale(central.cam.cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
        var point = central.cam.transform.position + forward * 80;
        return new Vector3(
            Mathf.Round(point.x / 16) * 16,
            Mathf.Max(0, Mathf.Round(point.y / 8) * 8),
            Mathf.Round(point.z / 16) * 16);
    }
}
