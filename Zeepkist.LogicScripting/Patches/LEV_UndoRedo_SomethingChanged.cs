using HarmonyLib;
using TNRD.Zeepkist.LogicScripting.Blocks;

namespace TNRD.Zeepkist.LogicScripting.Patches;

/// <summary>
/// Every edit that registers an undo step, a Compile included.
/// </summary>
[HarmonyPatch(typeof(LEV_UndoRedo), nameof(LEV_UndoRedo.SomethingChanged))]
internal static class LEV_UndoRedo_SomethingChanged
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        LevelChanges.Add();
    }
}
