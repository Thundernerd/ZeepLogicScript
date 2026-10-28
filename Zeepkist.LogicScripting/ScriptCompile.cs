using System;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting;

/// <summary>
/// Compiles a Script and places its blocks in the open level, where its <see cref="CompilePlacement"/> says. The
/// Script's name and text go into the level with its blocks, in the same undo step (ADR 0006).
/// </summary>
internal static class ScriptCompile
{
    private const string UndoLabel = "Logic Scripting Compile";

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

        var placement = CompilePlacement.For(central, script.Id);
        var result = Check(central, script.Text, script.Id, check, placement.QuarterTurns);
        if (!result.Succeeded)
            return FormatErrors(result);

        if (!check.CanPlace)
            return $"No errors, but {StartupCheck.UpdateNeededMessage}; nothing placed";

        if (placement.ChangesNothing(result))
            return placement.GetNothingToPlaceLine();

        int lostWires;
        try
        {
            var jsons = CompiledBlockJson.Create(result, placement.GetOrigin(result), script);
            var placedWires = CompiledBlockJson.CreatePlacedWires(result, jsons);
            lostWires = BlockGroups.CountWiresNotRemade(placement.Group, placedWires);
            BlockBatch.Apply(central, jsons, placement.Group, placedWires, UndoLabel);
        }
        catch (Exception e)
        {
            logger.LogError($"Compile failed: {e}");
            return "Compile failed; level unchanged";
        }

        var line = placement.GetResultLine(result);
        if (lostWires > 0)
            line += $"\nRemoved {lostWires} Wires between the group and other blocks that this Compile doesn't make again; to keep a hand-made one, wire the block in the Script with placed(...)";

        return line;
    }

    /// <summary>
    /// Compiles without placing anything, for the Placed blocks panel; null outside the editor.
    /// </summary>
    public static CompileResult Check(string text, ScriptId scriptId, StartupCheck check)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        return central == null ? null : Check(central, text, scriptId, check, 0);
    }

    private static CompileResult Check(LEV_LevelEditorCentral central, string text, ScriptId scriptId, StartupCheck check, int quarterTurns)
    {
        check.RunOnce();
        return ScriptCompiler.Compile(text, quarterTurns, check.DisabledBlockIds, uid => PlacedBlocks.Find(central, uid, scriptId));
    }

    private static string FormatErrors(CompileResult result)
    {
        var heading = result.Errors.Count == 1 ? "1 error; nothing placed" : $"{result.Errors.Count} errors; nothing placed";
        return heading + "\n" + string.Join("\n", result.Errors);
    }
}
