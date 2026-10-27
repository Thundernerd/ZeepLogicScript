using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using static TNRD.ZeepLogicScript.Spike.SpikeBlocks;

namespace TNRD.ZeepLogicScript.Spike
{
    /// <summary>
    /// Throwaway spike scenarios for the in-game checks in .scratch/logic-script/spike-checklist.md.
    /// </summary>
    internal static class BatchCreateSpike
    {
        private const float Spacing = 16;

        // Layout of the test group relative to its origin; the carrier (generator A) is first
        private static readonly Vector3 GeneratorAOffset = new(-Spacing, 0, Spacing / 2);
        private static readonly Vector3 GeneratorBOffset = new(-Spacing, 0, -Spacing / 2);
        private static readonly Vector3 LogicOffset = Vector3.zero;
        private static readonly Vector3 DisplayOffset = new(Spacing, 0, 0);

        /// <summary>
        /// Two Generators (power 1) into a Logic block, its output into a Power Display, tagged as one Block Group.
        /// </summary>
        public static void PlaceGroup(ManualLogSource logger)
        {
            if (!TryGetCentral(logger, out LEV_LevelEditorCentral central))
                return;

            string scriptId = Guid.NewGuid().ToString("N");
            List<BlockPropertyJSON> jsons = BuildGroup(GetOrigin(central), LogicOperatorAnd);
            Tag(jsons, scriptId, SpikeCarrier.TestName, SpikeCarrier.BuildTestText());

            List<BlockProperties> blocks = CreateBlocks(central, jsons);
            RegisterUndo(central, [], [], blocks);

            logger.LogInfo($"[place] Block Group {scriptId}: {blocks.Count} blocks, AND, carrier {blocks[0].UID}");
        }

        /// <summary>
        /// Replaces the first Block Group found (by tag) with a new one that flips AND/OR, as one undo step.
        /// </summary>
        public static void RecompileGroup(ManualLogSource logger)
        {
            if (!TryGetCentral(logger, out LEV_LevelEditorCentral central))
                return;

            SpikeGroup group = null;
            foreach (SpikeGroup candidate in SpikeInspector.FindGroups(central).Values)
            {
                if (candidate.Carrier != null)
                {
                    group = candidate;
                    break;
                }
            }

            if (group == null)
            {
                logger.LogWarning("[recompile] No Block Group with a carrier found; place one first");
                return;
            }

            int oldOperator = LogicOperatorAnd;
            foreach (BlockProperties member in group.Members)
            {
                if (member.blockID == LogicBlockId)
                    oldOperator = member.customBlockEditProperties.GetIntDictionaryValue(KeyLogicOperator);
            }

            int newOperator = oldOperator == LogicOperatorAnd ? LogicOperatorOr : LogicOperatorAnd;
            Vector3 origin = group.Carrier.transform.position - GeneratorAOffset;
            SpikeCarrier.TryParse(
                group.Carrier.customBlockEditProperties.GetStringDictionaryValue(KeyScriptText),
                out _,
                out _,
                out string name,
                out string text);

            List<BlockPropertyJSON> jsons = BuildGroup(origin, newOperator);
            Tag(jsons, group.ScriptId, name, text);

            List<BlockProperties> removed = group.Members;
            List<string> removedBefore = RemoveBlocks(central, removed);
            List<BlockProperties> added = CreateBlocks(central, jsons);
            RegisterUndo(central, removed, removedBefore, added);

            logger.LogInfo(
                $"[recompile] Block Group {group.ScriptId}: replaced {removed.Count} -> {added.Count} blocks, " +
                $"{(oldOperator == LogicOperatorAnd ? "AND" : "OR")} -> {(newOperator == LogicOperatorAnd ? "AND" : "OR")}");
        }

        /// <summary>
        /// A visible Generator → Passthrough pair with a long Wire, and the same pair hidden in game, to drive into.
        /// </summary>
        public static void PlaceCollisionTest(ManualLogSource logger)
        {
            if (!TryGetCentral(logger, out LEV_LevelEditorCentral central))
                return;

            Vector3 origin = GetOrigin(central);
            List<BlockPropertyJSON> jsons = [];
            AddPair(jsons, origin, false);
            AddPair(jsons, origin + new Vector3(Spacing * 3, 0, 0), true);

            List<BlockProperties> blocks = CreateBlocks(central, jsons);
            RegisterUndo(central, [], [], blocks);

            logger.LogInfo("[collision] Placed a visible pair (left) and a hidden pair (right), each with a 64-unit Wire");
        }

        private static void AddPair(List<BlockPropertyJSON> jsons, Vector3 origin, bool hide)
        {
            BlockPropertyJSON generator = CreateJson(GeneratorBlockId, origin);
            BlockPropertyJSON passthrough = CreateJson(PassthroughBlockId, origin + new Vector3(0, 0, Spacing * 4));
            generator.d.SetStringDictionaryValue(KeyGeneratorPower, "1");
            generator.d.SetBoolDictionaryValue(KeyHide, hide);
            passthrough.d.SetBoolDictionaryValue(KeyHide, hide);
            AddWire(generator, GeneratorOut, passthrough, PassthroughIn);
            jsons.Add(generator);
            jsons.Add(passthrough);
        }

        private static List<BlockPropertyJSON> BuildGroup(Vector3 origin, int logicOperator)
        {
            BlockPropertyJSON generatorA = CreateJson(GeneratorBlockId, origin + GeneratorAOffset);
            BlockPropertyJSON generatorB = CreateJson(GeneratorBlockId, origin + GeneratorBOffset);
            BlockPropertyJSON logic = CreateJson(LogicBlockId, origin + LogicOffset);
            BlockPropertyJSON display = CreateJson(PowerDisplayBlockId, origin + DisplayOffset);

            generatorA.d.SetStringDictionaryValue(KeyGeneratorPower, "1");
            generatorB.d.SetStringDictionaryValue(KeyGeneratorPower, "1");
            logic.d.SetIntDictionaryValue(KeyLogicOperator, logicOperator);

            AddWire(generatorA, GeneratorOut, logic, LogicInA);
            AddWire(generatorB, GeneratorOut, logic, LogicInB);
            AddWire(logic, LogicOut, display, PowerDisplayIn);

            return [generatorA, generatorB, logic, display];
        }
    }
}
