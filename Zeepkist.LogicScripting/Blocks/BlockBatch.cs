using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Places a set of blocks and wires them to blocks already in the level as one undo step, all or nothing. Uses only
/// game types, plain block JSON and UIDs, nothing Script-specific, so it can move to ZeepSDK later (ADR 0007).
/// </summary>
internal static partial class BlockBatch
{
    private const string KeyPaintBase = "p";

    /// <summary>
    /// Creates the blocks, Wires between them included in their JSON, adds <paramref name="wires"/>, each with at
    /// least one end on a block already in the level, and registers it all as one undo step named
    /// <paramref name="label"/>. The existing blocks the Wires touch are recorded as changed, so undo restores them
    /// exactly. If anything throws, every block created so far is removed again, the existing blocks get their old
    /// state back, no undo step is registered and the exception is rethrown.
    /// </summary>
    public static List<BlockProperties> Apply(
        LEV_LevelEditorCentral central,
        IReadOnlyList<BlockPropertyJSON> adds,
        IReadOnlyList<BlockWire> wires,
        string label)
    {
        // BreakLock silently skips the undo step while grabbing
        if (central.gizmos.isGrabbing)
            throw new InvalidOperationException("Can't apply a block batch while blocks are grabbed");

        List<BlockProperties> added = [];
        List<ChangedBlock> changed = [];
        try
        {
            foreach (var json in adds)
            {
                added.Add(CreateBlock(central, json, label));
            }

            LoadConnectors(added, label);
            Connect(central, added, changed, wires, label);
            RegisterUndo(central, added, changed, label);
            return added;
        }
        catch
        {
            Remove(central, added);
            Restore(central, changed, label);
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
            RedrawWires(block, label);
        }
    }

    /// <summary>
    /// Wires both ends, as the connector tool does; the output end is the drawing leader. Each existing block's JSON
    /// is noted in <paramref name="changed"/> before its first Wire, for undo and for rolling back.
    /// </summary>
    private static void Connect(
        LEV_LevelEditorCentral central,
        List<BlockProperties> added,
        List<ChangedBlock> changed,
        IReadOnlyList<BlockWire> wires,
        string label)
    {
        HashSet<BlockProperties> touched = [.. added];
        foreach (var wire in wires)
        {
            var from = GetConnector(central, wire.FromUid, wire.FromConnector);
            var to = GetConnector(central, wire.ToUid, wire.ToConnector);
            foreach (var block in new[] { from.GetBlockProperties(), to.GetBlockProperties() })
            {
                if (touched.Add(block))
                    changed.Add(new ChangedBlock(block, block.ConvertBlockToJSON_v15_string(true)));
            }

            from.Connect(to, true);
            to.Connect(from, false);
        }

        foreach (var block in touched)
        {
            RedrawWires(block, label);
        }
    }

    private static ConnectorHub_Base GetConnector(LEV_LevelEditorCentral central, string uid, int connector)
    {
        var block = LevelBlocks.Find(central, uid);
        if (block == null || !block.allConnectorHubsOnThisBlock.TryGetValue(connector, out var hub))
            throw new InvalidOperationException($"Block {uid} has no Connector {connector} to wire");

        return hub;
    }

    /// <summary>
    /// Added blocks are recorded as before = null / after = JSON and changed blocks as JSON / JSON, the shapes the
    /// editor's own undo steps use. Taken after wiring, so redo restores the Wires.
    /// </summary>
    private static void RegisterUndo(LEV_LevelEditorCentral central, List<BlockProperties> added, List<ChangedBlock> changed, string label)
    {
        List<string> before = [];
        List<string> after = [];
        List<BlockProperties> blocks = [];
        foreach (var block in added)
        {
            before.Add(null);
            after.Add(block.ConvertBlockToJSON_v15_string(true));
            blocks.Add(block);
        }

        foreach (var change in changed)
        {
            before.Add(change.Before);
            after.Add(change.Block.ConvertBlockToJSON_v15_string(true));
            blocks.Add(change.Block);
        }

        var collection = central.undoRedo.ConvertBeforeAndAfterListToCollection(before, after, blocks, [], []);
        central.validation.BreakLock(collection, label);
    }

    /// <summary>
    /// Puts each changed block's Settings and Wires back as they were, then reloads and redraws every block wired to
    /// one of them, the same steps undo takes. Call after removing the added blocks, so nothing redraws a Wire to them.
    /// </summary>
    private static void Restore(LEV_LevelEditorCentral central, List<ChangedBlock> changed, string label)
    {
        HashSet<string> reloaded = [];
        HashSet<string> affected = [];
        foreach (var change in changed)
        {
            change.Block.customBlockEditProperties = LEV_UndoRedo.GetJSONblock(change.Before).d?.Clone() ?? new PropertyDictionariesJSON();
            reloaded.Add(change.Block.UID);
            foreach (var blockEdit in change.Block.GetAllBlockEditV18sFromThisBlock())
            {
                if (!blockEdit.HasConnectors())
                    continue;

                var connections = blockEdit.GetBlockEditV18ConnectionBase();
                connections.LoadProperties();
                affected.UnionWith(connections.GetAllConnectedUIDsOnEntireBlockEdit_ButAlsoYourOwnBlockUID());
            }
        }

        foreach (var uid in affected)
        {
            var block = LevelBlocks.Find(central, uid);
            if (block == null)
                continue;

            if (!reloaded.Contains(uid))
                block.LoadOnlyPropertyScripts();

            RedrawWires(block, label);
        }
    }

    private static void RedrawWires(BlockProperties block, string label)
    {
        foreach (var blockEdit in block.GetAllBlockEditV18sFromThisBlock())
        {
            if (blockEdit.HasConnectors())
                blockEdit.GetBlockEditV18ConnectionBase().ForceRedrawConnectionVisualizers(label);
        }
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
