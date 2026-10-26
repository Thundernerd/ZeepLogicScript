using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using ZeepSDK.Utilities;

namespace TNRD.ZeepLogicScript.Spike
{
    /// <summary>
    /// Shared helpers for the spike: block JSON, Wires, tags, creation through the game's own
    /// LEV_UndoRedo.GenerateNewBlock, removal, and one-step undo registration.
    /// </summary>
    internal static class SpikeBlocks
    {
        public const string Source = "ZeepLogicScript Spike";

        public const int GeneratorBlockId = 2414;
        public const int GeneratorOut = 0;
        public const string KeyGeneratorPower = "lbp";

        public const int LogicBlockId = 2420;
        public const int LogicInA = 0;
        public const int LogicInB = 1;
        public const int LogicOut = 3;
        public const string KeyLogicOperator = "xmo";
        public const int LogicOperatorAnd = 0;
        public const int LogicOperatorOr = 1;

        public const int PowerDisplayBlockId = 2415;
        public const int PowerDisplayIn = 0;

        public const int PassthroughBlockId = 2426;
        public const int PassthroughIn = 0;

        public const string KeyHide = "lbhd";
        public const string KeyGroupTag = "zls_g";
        public const string KeyScriptText = "zls_s";

        private const string KeyConnectionBase = "id";
        private const string KeyPaintBase = "p";

        public static bool TryGetCentral(ManualLogSource logger, out LEV_LevelEditorCentral central)
        {
            central = ComponentCache.Get<LEV_LevelEditorCentral>();
            if (central == null)
            {
                logger.LogError("No LEV_LevelEditorCentral found");
                return false;
            }

            if (central.gizmos.isGrabbing)
            {
                logger.LogWarning("Can't run while grabbing blocks; BreakLock would skip the undo step");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Same rule as the game's own placement: 80 units ahead on the flattened view direction, snapped, y >= 0.
        /// </summary>
        public static Vector3 GetOrigin(LEV_LevelEditorCentral central)
        {
            Transform camera = central.cam.cameraTransform;
            Vector3 forward = Vector3.Scale(camera.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 origin = central.cam.transform.position + forward * 80;
            return new Vector3(Mathf.Round(origin.x / 16) * 16, Mathf.Max(0, Mathf.Round(origin.y / 8) * 8), Mathf.Round(origin.z / 16) * 16);
        }

        public static BlockPropertyJSON CreateJson(int blockId, Vector3 position)
        {
            return new BlockPropertyJSON
            {
                i = blockId,
                u = PlayerManager.Instance.GenerateUniqueIDforBlocks(blockId.ToString()),
                p = new CV3(position),
                r = new CV3(Vector3.zero),
                s = new CV3(Vector3.one),
                d = new PropertyDictionariesJSON()
            };
        }

        /// <summary>
        /// Writes the Wire into both blocks' dictionaries, like LEV_ConnectorTool does via Connect on both ends.
        /// The output end is the drawing leader.
        /// </summary>
        public static void AddWire(BlockPropertyJSON from, int fromConnector, BlockPropertyJSON to, int toConnector)
        {
            AddConnection(from, fromConnector, new ConnectionStruct(to.u, toConnector, true));
            AddConnection(to, toConnector, new ConnectionStruct(from.u, fromConnector, false));
        }

        private static void AddConnection(BlockPropertyJSON json, int connector, ConnectionStruct connection)
        {
            string countKey = KeyConnectionBase + connector;
            int count = json.d.GetIntDictionaryValue(countKey);
            json.d.SetStringDictionaryValue(countKey + "-" + count, connection.Serialize());
            json.d.SetIntDictionaryValue(countKey, count + 1);
        }

        /// <summary>
        /// Tags every block as a Block Group member ("scriptId:ownUID") and puts the Script text on the first one.
        /// </summary>
        public static void Tag(List<BlockPropertyJSON> jsons, string scriptId, string scriptName, string scriptText)
        {
            foreach (BlockPropertyJSON json in jsons)
            {
                json.d.SetStringDictionaryValue(KeyGroupTag, scriptId + ":" + json.u);
            }

            jsons[0].d.SetStringDictionaryValue(KeyScriptText, SpikeCarrier.Serialize(scriptId, scriptName, scriptText));
        }

        /// <summary>
        /// Two-pass create like the level loader: every block first, then the Connectors once all are tracked.
        /// </summary>
        public static List<BlockProperties> CreateBlocks(LEV_LevelEditorCentral central, List<BlockPropertyJSON> jsons)
        {
            List<BlockProperties> blocks = [];
            foreach (BlockPropertyJSON json in jsons)
            {
                blocks.Add(CreateBlock(central, json));
            }

            // LoadProperties_v15 skips connector scripts; load them now that all blocks are tracked
            foreach (BlockProperties block in blocks)
            {
                foreach (BlockEdit_v18 blockEdit in block.GetAllBlockEditV18sFromThisBlock())
                {
                    if (blockEdit.HasConnectors())
                        blockEdit.GetBlockEditV18ConnectionBase().LoadProperties();
                }
            }

            foreach (BlockProperties block in blocks)
            {
                foreach (BlockEdit_v18 blockEdit in block.GetAllBlockEditV18sFromThisBlock())
                {
                    if (blockEdit.HasConnectors())
                        blockEdit.GetBlockEditV18ConnectionBase().ForceRedrawConnectionVisualizers(Source);
                }
            }

            return blocks;
        }

        /// <summary>
        /// Calls the game's private GenerateNewBlock (ADR 0007), plus the steps its redo caller adds.
        /// </summary>
        private static BlockProperties CreateBlock(LEV_LevelEditorCentral central, BlockPropertyJSON json)
        {
            BlockProperties prefab = central.manager.loader.globalBlockList.blocks[json.i];
            AddDefaultPaint(prefab, json);

            BlockProperties block = central.undoRedo.GenerateNewBlock(json, json.u);
            block.NewBlockCreatedFromEditorForV15JSON();

            StaticConnectorTracker.AddBlockToTracker(block, Source);
            return block;
        }

        /// <summary>
        /// Paint is stored per renderer material slot as n["p{index}"]; a missing key loads as material 0.
        /// Mirrors Properties_RoadPainter.SavePaintProperties, which only runs from the CreateBlock step GenerateNewBlock skips.
        /// </summary>
        private static void AddDefaultPaint(BlockProperties prefab, BlockPropertyJSON json)
        {
            Properties_RoadPainter painter = prefab.GetComponent<Properties_RoadPainter>();
            if (painter == null || !MaterialManager.DoesExist())
                return;

            int slot = 0;
            foreach (MeshRenderer renderer in painter.renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    string key = KeyPaintBase + slot;
                    if (json.d.n == null || !json.d.n.ContainsKey(key))
                        json.d.SetIntDictionaryValue(key, MaterialManager.GetMaterialHolderFromMaterial(material).materialID);

                    slot++;
                }
            }
        }

        /// <summary>
        /// Removes blocks the way the gizmo delete does, and returns their "before" JSON for the undo step.
        /// The spike's groups have no Wires to outside blocks, so there are no survivors to clean up.
        /// </summary>
        public static List<string> RemoveBlocks(LEV_LevelEditorCentral central, List<BlockProperties> blocks)
        {
            List<string> before = [];
            foreach (BlockProperties block in blocks)
            {
                before.Add(block.ConvertBlockToJSON_v15_string(true));
                central.selection.list.Remove(block);
            }

            foreach (BlockProperties block in blocks)
            {
                StaticConnectorTracker.RemoveBlockFromTracker(block.UID);
                Object.Destroy(block.gameObject);
            }

            return before;
        }

        /// <summary>
        /// One undo step: removed blocks are before = JSON / after = null, added blocks before = null / after = JSON.
        /// </summary>
        public static void RegisterUndo(
            LEV_LevelEditorCentral central,
            List<BlockProperties> removed,
            List<string> removedBefore,
            List<BlockProperties> added)
        {
            List<string> before = [];
            List<string> after = [];
            List<BlockProperties> blocks = [];

            for (int i = 0; i < removed.Count; i++)
            {
                before.Add(removedBefore[i]);
                after.Add(null);
                blocks.Add(removed[i]);
            }

            foreach (BlockProperties block in added)
            {
                before.Add(null);
                after.Add(block.ConvertBlockToJSON_v15_string(true));
                blocks.Add(block);
            }

            Change_Collection collection = central.undoRedo.ConvertBeforeAndAfterListToCollection(
                before,
                after,
                blocks,
                [],
                []);
            central.validation.BreakLock(collection, Source);
        }

        /// <summary>
        /// The startup check from ADR 0007: does the private method still have the signature the spike calls?
        /// </summary>
        public static bool CheckGenerateNewBlockSignature(ManualLogSource logger)
        {
            MethodInfo method = typeof(LEV_UndoRedo).GetMethod(
                "GenerateNewBlock",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                [typeof(BlockPropertyJSON), typeof(string)],
                null);

            bool ok = method != null && method.ReturnType == typeof(BlockProperties);
            if (ok)
                logger.LogInfo($"[check] LEV_UndoRedo.GenerateNewBlock found (private: {method.IsPrivate}, returns BlockProperties)");
            else
                logger.LogError("[check] LEV_UndoRedo.GenerateNewBlock(BlockPropertyJSON, string) not found; Compile would be disabled");

            return ok;
        }
    }
}
