using System;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Removes a set of blocks, places a set of blocks and wires them to blocks already in the level as one undo step, all
/// or nothing. Uses only game types, plain block JSON and UIDs, nothing Script-specific, so it can move to ZeepSDK later
/// (ADR 0007).
/// </summary>
internal static partial class BlockBatch
{
    private const string KeyPaintBase = "p";

    /// <summary>
    /// Registers everything as one undo step named <paramref name="label"/>. Wires between
    /// <paramref name="adds"/> go in their JSON; each of <paramref name="wires"/> has at least one end on a block
    /// already in the level. Blocks that stay are recorded as changed when they lose a Wire to a removed block
    /// or gain one of <paramref name="wires"/>, so undo restores them exactly. If anything throws, the added blocks are
    /// removed again, the removed ones come back, the changed ones get their old state back, no undo step is registered
    /// and the exception is rethrown.
    /// </summary>
    public static List<BlockProperties> Apply(
        LEV_LevelEditorCentral central,
        IReadOnlyList<BlockPropertyJSON> adds,
        IReadOnlyList<BlockProperties> removes,
        IReadOnlyList<BlockWire> wires,
        string label)
    {
        // BreakLock silently skips the undo step while grabbing
        if (central.gizmos.isGrabbing)
            throw new InvalidOperationException("Can't apply a block batch while blocks are grabbed");

        // Undo finds each block by UID, so a remove and an add of one UID in one step would depend on their order
        HashSet<string> removedUids = [.. removes.Select(b => b.UID)];
        if (adds.Any(json => removedUids.Contains(json.u)))
            throw new InvalidOperationException("Can't remove and add a block with the same UID in one block batch");

        HashSet<string> selectedUids = [.. removes.Where(central.selection.list.Contains).Select(b => b.UID)];
        List<string> removed = [];
        List<BlockProperties> added = [];
        List<ChangedBlock> changed = [];
        try
        {
            RemoveBlocks(central, removes, removedUids, removed, changed, label);
            foreach (var json in adds)
            {
                added.Add(CreateBlock(central, json, label));
            }

            LoadConnectors(added, label);
            Connect(central, added, changed, wires, label);
            RegisterUndo(central, removed, added, changed, label);
            return added;
        }
        catch
        {
            Remove(central, added);
            try
            {
                Recreate(central, removed, selectedUids, label);
            }
            finally
            {
                Restore(central, changed, label);
            }

            throw;
        }
    }

    /// <summary>
    /// Runs <paramref name="change"/> on each block and registers it as one undo step named <paramref name="label"/>.
    /// If anything throws, every block gets its old state back, no undo step is registered and the exception is
    /// rethrown.
    /// </summary>
    public static void Change(
        LEV_LevelEditorCentral central,
        IReadOnlyList<BlockProperties> blocks,
        Action<BlockProperties> change,
        string label)
    {
        if (central.gizmos.isGrabbing)
            throw new InvalidOperationException("Can't apply a block batch while blocks are grabbed");

        List<ChangedBlock> changed = [];
        try
        {
            foreach (var block in blocks)
            {
                changed.Add(new ChangedBlock(block, block.ConvertBlockToJSON_v15_string(true)));
                change(block);
            }

            RegisterUndo(central, [], [], changed, label);
        }
        catch
        {
            Restore(central, changed, label);
            throw;
        }
    }

    /// <summary>
    /// Removes the blocks and the other blocks' Wires to them, noting each one's JSON in <paramref name="removed"/>
    /// and each block that loses a Wire in <paramref name="changed"/>. A changed block's undo state is noted before the
    /// cleanup; its redo state is taken in <see cref="RegisterUndo"/>, after it, so redo doesn't bring back Wires to
    /// removed blocks.
    /// </summary>
    private static void RemoveBlocks(
        LEV_LevelEditorCentral central,
        IReadOnlyList<BlockProperties> blocks,
        HashSet<string> removedUids,
        List<string> removed,
        List<ChangedBlock> changed,
        string label)
    {
        HashSet<string> wiredUids = [];
        foreach (var block in blocks)
        {
            foreach (var blockEdit in block.GetAllBlockEditV18sFromThisBlock())
            {
                if (blockEdit.HasConnectors())
                    wiredUids.UnionWith(blockEdit.GetBlockEditV18ConnectionBase().GetAllConnectedUIDsOnEntireBlockEdit());
            }
        }

        List<BlockProperties> survivors = [];
        foreach (var uid in wiredUids)
        {
            var block = LevelBlocks.Find(central, uid);
            if (block != null && !removedUids.Contains(uid))
                survivors.Add(block);
        }

        foreach (var survivor in survivors)
        {
            changed.Add(new ChangedBlock(survivor, survivor.ConvertBlockToJSON_v15_string(true)));
        }

        var wasSelected = blocks.Any(central.selection.list.Contains);
        foreach (var block in blocks)
        {
            removed.Add(block.ConvertBlockToJSON_v15_string(true));
            Remove(central, [block]);
        }

        if (wasSelected)
            UpdateSelection(central, label);

        // Cleanup drops each Wire whose other end has left StaticConnectorTracker, which Remove did
        foreach (var survivor in survivors)
        {
            survivor.RunConnectionCleanup();
            RedrawWires(survivor, label);
        }
    }

    private static void UpdateSelection(LEV_LevelEditorCentral central, string label)
    {
        if (central.selection.list.Count == 0)
            central.selection.DeselectAllBlocks(false, label);
        else
            central.selection.CalculateMiddlePivot();
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
    /// Writes the default paint that Properties_RoadPainter.SavePaintProperties would, which GenerateNewBlock leaves out.
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
    /// Second pass: LoadProperties_v15 leaves the Connector scripts out, and a Wire only resolves once the block at its
    /// other end exists.
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
    /// Wires both ends; the output end is the drawing leader. Each existing block's JSON
    /// is noted in <paramref name="changed"/> before its first Wire, for undo and for rolling back.
    /// </summary>
    private static void Connect(
        LEV_LevelEditorCentral central,
        List<BlockProperties> added,
        List<ChangedBlock> changed,
        IReadOnlyList<BlockWire> wires,
        string label)
    {
        HashSet<BlockProperties> touched = [.. added, .. changed.Select(c => c.Block)];
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
    /// Removed blocks are recorded as before = JSON / after = null, added blocks as null / JSON and changed blocks as
    /// JSON / JSON, the shapes the editor's own undo steps use. Taken after wiring, so redo restores the Wires.
    /// </summary>
    private static void RegisterUndo(
        LEV_LevelEditorCentral central,
        List<string> removed,
        List<BlockProperties> added,
        List<ChangedBlock> changed,
        string label)
    {
        List<string> before = [];
        List<string> after = [];
        List<BlockProperties> blocks = [];
        foreach (var json in removed)
        {
            before.Add(json);
            after.Add(null);
            blocks.Add(null);
        }

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
    /// Call before <see cref="Restore"/>, so the changed blocks' Wires to the recreated blocks resolve.
    /// </summary>
    private static void Recreate(LEV_LevelEditorCentral central, List<string> removed, HashSet<string> selectedUids, string label)
    {
        List<BlockProperties> blocks = [];
        foreach (var json in removed)
        {
            blocks.Add(CreateBlock(central, LEV_UndoRedo.GetJSONblock(json), label));
        }

        LoadConnectors(blocks, label);
        foreach (var block in blocks)
        {
            if (selectedUids.Contains(block.UID))
                central.selection.AddThisBlock(block);
        }
    }

    /// <summary>
    /// Also reloads every block wired to a changed one, so it shows the restored Wires. Call after removing the added
    /// blocks, so nothing redraws a Wire to them.
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
