using System;
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
    /// Returns the result line to show in the window.
    /// </summary>
    public static string Run(string text, ManualLogSource logger)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
            return "Open the level editor to compile";

        if (central.gizmos.isGrabbing)
            return "Let go of the grabbed blocks to compile";

        var right = central.cam.cameraTransform.right;
        var result = ScriptCompiler.Compile(text, ScriptCompiler.GetQuarterTurnsFacing(right.x, right.z));
        if (!result.Succeeded)
            return FormatErrors(result);

        if (result.Blocks.Count == 0)
            return "Nothing to place";

        try
        {
            var jsons = CompiledBlockJson.Create(result, GetPlacementPoint(central));
            BlockBatch.Apply(central, jsons, UndoLabel);
        }
        catch (Exception e)
        {
            logger.LogError($"Compile failed: {e}");
            return "Compile failed; level unchanged";
        }

        return $"Placed {result.Blocks.Count} blocks";
    }

    private static string FormatErrors(CompileResult result)
    {
        var first = result.Errors[0];
        return result.Errors.Count == 1
            ? $"Error at {first}"
            : $"{result.Errors.Count} errors, first at {first}";
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
