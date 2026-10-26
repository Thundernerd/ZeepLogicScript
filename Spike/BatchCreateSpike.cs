using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using ZeepSDK.Utilities;

namespace TNRD.ZeepLogicScript.Spike
{
    /// <summary>
    /// Throwaway spike: places a hard-coded `x = a & b` (two Generators + one AND Logic block),
    /// wired, as a single undo step. See .scratch/logic-script/research/03-batch-create-path.md.
    /// </summary>
    internal static class BatchCreateSpike
    {
        private const string Source = "ZeepLogicScript Spike";

        private const int GeneratorBlockId = 2414;
        private const int GeneratorOut = 0;
        private const string KeyGeneratorPower = "lbp";

        private const int LogicBlockId = 2420;
        private const int LogicInA = 0;
        private const int LogicInB = 1;
        private const string KeyLogicOperator = "xmo";
        private const int LogicOperatorAnd = 0;

        private const string KeyConnectionBase = "id";
        private const string KeyPaintBase = "p";
        private const float Spacing = 16;

        public static void Run(ManualLogSource logger)
        {
            LEV_LevelEditorCentral central = ComponentCache.Get<LEV_LevelEditorCentral>();
            if (central == null)
            {
                logger.LogError("No LEV_LevelEditorCentral found");
                return;
            }

            if (central.gizmos.isGrabbing)
            {
                logger.LogWarning("Can't run while grabbing blocks; BreakLock would skip the undo step");
                return;
            }

            if (!CheckPrefab(central, GeneratorBlockId, logger) || !CheckPrefab(central, LogicBlockId, logger))
                return;

            Vector3 origin = GetOrigin(central);

            BlockPropertyJSON generatorA = CreateJson(GeneratorBlockId, origin + new Vector3(-Spacing, 0, Spacing / 2));
            BlockPropertyJSON generatorB = CreateJson(GeneratorBlockId, origin + new Vector3(-Spacing, 0, -Spacing / 2));
            BlockPropertyJSON logic = CreateJson(LogicBlockId, origin);

            generatorA.d.SetStringDictionaryValue(KeyGeneratorPower, "1");
            generatorB.d.SetStringDictionaryValue(KeyGeneratorPower, "1");
            logic.d.SetIntDictionaryValue(KeyLogicOperator, LogicOperatorAnd);

            AddWire(generatorA, GeneratorOut, logic, LogicInA);
            AddWire(generatorB, GeneratorOut, logic, LogicInB);

            List<BlockPropertyJSON> jsons = [generatorA, generatorB, logic];

            // Pass 1: create every block and register it, so Wires can resolve their targets
            List<BlockProperties> blocks = [];
            foreach (BlockPropertyJSON json in jsons)
            {
                blocks.Add(CreateBlock(central, json));
            }

            // Pass 2: LoadProperties_v15 skips connector scripts; load them now that all blocks are tracked
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

            // One undo step: before = null marks each block as added
            List<string> before = [];
            List<string> after = [];
            foreach (BlockProperties block in blocks)
            {
                before.Add(null);
                after.Add(block.ConvertBlockToJSON_v15_string(true));
            }

            Change_Collection collection = central.undoRedo.ConvertBeforeAndAfterListToCollection(
                before,
                after,
                blocks,
                [],
                []);
            central.validation.BreakLock(collection, Source);

            for (int i = 0; i < blocks.Count; i++)
            {
                logger.LogInfo($"Created {blocks[i].gameObject.name} ({blocks[i].UID}): {after[i]}");
            }
        }

        private static bool CheckPrefab(LEV_LevelEditorCentral central, int blockId, ManualLogSource logger)
        {
            List<BlockProperties> prefabs = central.manager.loader.globalBlockList.blocks;
            if (blockId >= prefabs.Count || prefabs[blockId] == null)
            {
                logger.LogError($"No prefab at index {blockId}");
                return false;
            }

            BlockProperties prefab = prefabs[blockId];
            logger.LogInfo($"Prefab {blockId}: {prefab.gameObject.name} (blockID {prefab.blockID})");
            return true;
        }

        private static Vector3 GetOrigin(LEV_LevelEditorCentral central)
        {
            Transform camera = central.cam.cameraTransform;
            Vector3 forward = Vector3.Scale(camera.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 origin = central.cam.transform.position + forward * 80;
            return new Vector3(Mathf.Round(origin.x), Mathf.Max(0, Mathf.Round(origin.y)), Mathf.Round(origin.z));
        }

        private static BlockPropertyJSON CreateJson(int blockId, Vector3 position)
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
        private static void AddWire(BlockPropertyJSON from, int fromConnector, BlockPropertyJSON to, int toConnector)
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
        /// Mirrors LEV_UndoRedo.GenerateNewBlock plus the steps its redo caller adds.
        /// </summary>
        private static BlockProperties CreateBlock(LEV_LevelEditorCentral central, BlockPropertyJSON json)
        {
            BlockProperties prefab = central.manager.loader.globalBlockList.blocks[json.i];
            AddDefaultPaint(prefab, json);

            BlockProperties block = Object.Instantiate(prefab);
            block.gameObject.name = prefab.gameObject.name;

            block.CreateBlock();
            block.DrawDebugUID();
            block.properties.Clear();
            block.isEditor = true;
            block.UID = json.u;

            block.LoadProperties_v15(json, false);
            block.isLoading = false;
            block.NewBlockCreatedFromEditorForV15JSON();

            StaticConnectorTracker.AddBlockToTracker(block, Source);
            return block;
        }

        /// <summary>
        /// Paint is stored per renderer material slot as n["p{index}"]; a missing key loads as material 0.
        /// Mirrors Properties_RoadPainter.SavePaintProperties, which only runs from the CreateBlock step we skip.
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
    }
}
