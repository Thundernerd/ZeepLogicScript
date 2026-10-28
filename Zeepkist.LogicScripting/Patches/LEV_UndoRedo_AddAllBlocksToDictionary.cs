using HarmonyLib;
using TNRD.Zeepkist.LogicScripting.Blocks;

namespace TNRD.Zeepkist.LogicScripting.Patches;

/// <summary>
/// The level's blocks are all there: a level finished loading, the editor came back from test mode, or it opened on a
/// new level. ZeepSDK's LevelLoaded fires before the blocks exist.
/// </summary>
[HarmonyPatch(typeof(LEV_UndoRedo), nameof(LEV_UndoRedo.AddAllBlocksToDictionary))]
internal static class LEV_UndoRedo_AddAllBlocksToDictionary
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        LevelChanges.Add();
    }
}
