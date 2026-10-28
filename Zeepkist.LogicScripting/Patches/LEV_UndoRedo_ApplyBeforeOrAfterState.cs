using HarmonyLib;
using TNRD.Zeepkist.LogicScripting.Blocks;

namespace TNRD.Zeepkist.LogicScripting.Patches;

/// <summary>
/// Undo and redo.
/// </summary>
[HarmonyPatch(typeof(LEV_UndoRedo), nameof(LEV_UndoRedo.ApplyBeforeOrAfterState))]
internal static class LEV_UndoRedo_ApplyBeforeOrAfterState
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        LevelChanges.Add();
    }
}
