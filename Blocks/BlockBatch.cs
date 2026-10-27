using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Places a set of blocks as one undo step, all or nothing. Uses only game types and plain block JSON, nothing
/// Script-specific, so it can move to ZeepSDK later (ADR 0007).
/// </summary>
internal static class BlockBatch
{
    private const string KeyPaintBase = "p";

    /// <summary>
    /// Creates the blocks, Wires included in their JSON, and registers them as one undo step named
    /// <paramref name="label"/>. If anything throws, every block created so far is removed again, no undo step is
    /// registered and the exception is rethrown.
    /// </summary>
    public static List<BlockProperties> Apply(LEV_LevelEditorCentral central, IReadOnlyList<BlockPropertyJSON> adds, string label)
    {
        // BreakLock silently skips the undo step while grabbing
        if (central.gizmos.isGrabbing)
            throw new InvalidOperationException("Can't apply a block batch while blocks are grabbed");

        List<BlockProperties> added = [];
        try
        {
            foreach (var json in adds)
            {
                added.Add(CreateBlock(central, json, label));
            }

            LoadConnectors(added, label);
            RegisterUndo(central, added, label);
            return added;
        }
        catch
        {
            Remove(central, added);
            throw;
        }
    }

    /// <summary>
    /// Calls the game's private GenerateNewBlock (ADR 0007), plus the steps it skips for new blocks.
    /// </summary>
    private static BlockProperties CreateBlock(LEV_LevelEditorCentral central, BlockPropertyJSON json, string label)
    {
        var prefab = central.manager.loader.globalBlockList.blocks[json.i];
        AddDefaultPaint(prefab, json);

        // Private: callable because AllowUnsafeBlocks makes Mono skip its access checks for this assembly
        var block = central.undoRedo.GenerateNewBlock(json, json.u);
        block.NewBlockCreatedFromEditorForV15JSON();

        StaticConnectorTracker.AddBlockToTracker(block, label);
        return block;
    }

    /// <summary>
    /// Paint is stored per renderer material slot as n["p{index}"]; a missing key loads as material 0.
    /// Mirrors Properties_RoadPainter.SavePaintProperties, which only runs from the CreateBlock step GenerateNewBlock skips.
    /// </summary>
    private static void AddDefaultPaint(BlockProperties prefab, BlockPropertyJSON json)
    {
        var painter = prefab.GetComponent<Properties_RoadPainter>();
        if (painter == null || !MaterialManager.DoesExist())
            return;

        var slot = 0;
        foreach (var renderer in painter.renderers)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                var key = KeyPaintBase + slot;
                if (json.d.n == null || !json.d.n.ContainsKey(key))
                    json.d.SetIntDictionaryValue(key, MaterialManager.GetMaterialHolderFromMaterial(material).materialID);

                slot++;
            }
        }
    }

    /// <summary>
    /// Second pass, like the level loader: LoadProperties_v15 skips Connector scripts, and a Wire only resolves once
    /// the block at its other end is tracked.
    /// </summary>
    private static void LoadConnectors(List<BlockProperties> blocks, string label)
    {
        foreach (var block in blocks)
        {
            foreach (var blockEdit in block.GetAllBlockEditV18sFromThisBlock())
            {
                if (blockEdit.HasConnectors())
                    blockEdit.GetBlockEditV18ConnectionBase().LoadProperties();
            }
        }

        foreach (var block in blocks)
        {
            foreach (var blockEdit in block.GetAllBlockEditV18sFromThisBlock())
            {
                if (blockEdit.HasConnectors())
                    blockEdit.GetBlockEditV18ConnectionBase().ForceRedrawConnectionVisualizers(label);
            }
        }
    }

    /// <summary>
    /// Added blocks are before = null / after = JSON, as the gizmo's paste does. Taken after the Connectors loaded,
    /// so redo restores the Wires.
    /// </summary>
    private static void RegisterUndo(LEV_LevelEditorCentral central, List<BlockProperties> added, string label)
    {
        List<string> before = [];
        List<string> after = [];
        foreach (var block in added)
        {
            before.Add(null);
            after.Add(block.ConvertBlockToJSON_v15_string(true));
        }

        var collection = central.undoRedo.ConvertBeforeAndAfterListToCollection(before, after, added, [], []);
        central.validation.BreakLock(collection, label);
    }

    private static void Remove(LEV_LevelEditorCentral central, List<BlockProperties> blocks)
    {
        foreach (var block in blocks)
        {
            central.undoRedo.allBlocksDictionary.Remove(block.UID);
            central.selection.list.Remove(block);
            StaticConnectorTracker.RemoveBlockFromTracker(block.UID);
            UnityEngine.Object.Destroy(block.gameObject);
        }
    }
}
