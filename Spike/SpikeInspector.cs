using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using ZeepSDK.Level;
using static TNRD.ZeepLogicScript.Spike.SpikeBlocks;

namespace TNRD.ZeepLogicScript.Spike
{
    internal class SpikeGroup
    {
        public string ScriptId;
        public readonly List<BlockProperties> Members = [];
        public readonly List<BlockProperties> Copies = [];
        public BlockProperties Carrier;
    }

    /// <summary>
    /// Logs every Block Group tag and carrier in the level, plus a content hash, so the checks can compare
    /// before/after save, reload, paste, inspector edits and undo/redo. Only runs from a menu click.
    /// </summary>
    internal static class SpikeInspector
    {
        public static Dictionary<string, SpikeGroup> FindGroups(LEV_LevelEditorCentral central)
        {
            Dictionary<string, SpikeGroup> groups = [];
            foreach (BlockProperties block in central.undoRedo.allBlocksDictionary.Values)
            {
                if (block == null)
                    continue;

                string tag = block.customBlockEditProperties.GetStringDictionaryValue(KeyGroupTag);
                if (string.IsNullOrEmpty(tag))
                    continue;

                int separator = tag.IndexOf(':');
                string scriptId = separator < 0 ? tag : tag.Substring(0, separator);
                string taggedUid = separator < 0 ? "" : tag.Substring(separator + 1);

                if (!groups.TryGetValue(scriptId, out SpikeGroup group))
                {
                    group = new SpikeGroup { ScriptId = scriptId };
                    groups.Add(scriptId, group);
                }

                if (taggedUid != block.UID)
                {
                    group.Copies.Add(block);
                    continue;
                }

                group.Members.Add(block);
                if (!string.IsNullOrEmpty(block.customBlockEditProperties.GetStringDictionaryValue(KeyScriptText)))
                    group.Carrier = block;
            }

            return groups;
        }

        public static void Inspect(ManualLogSource logger)
        {
            if (!TryGetCentral(logger, out LEV_LevelEditorCentral central))
                return;

            Dictionary<string, SpikeGroup> groups = FindGroups(central);
            logger.LogInfo($"[inspect] {groups.Count} Block Group(s)");

            string expectedHash = SpikeCarrier.Sha256(SpikeCarrier.BuildTestText());
            foreach (SpikeGroup group in groups.Values)
            {
                logger.LogInfo(
                    $"[inspect] Group {group.ScriptId}: {group.Members.Count} members, {group.Copies.Count} copies (tag UID != own UID), " +
                    $"carrier {(group.Carrier != null ? group.Carrier.UID : "MISSING")}");

                foreach (BlockProperties member in group.Members)
                {
                    string extra = member.blockID == LogicBlockId
                        ? $" op={(member.customBlockEditProperties.GetIntDictionaryValue(KeyLogicOperator) == LogicOperatorAnd ? "AND" : "OR")}"
                        : "";
                    logger.LogInfo($"[inspect]   member {member.UID} {member.gameObject.name}{extra}");
                }

                foreach (BlockProperties copy in group.Copies)
                {
                    logger.LogInfo($"[inspect]   copy   {copy.UID} {copy.gameObject.name} (tag says {copy.customBlockEditProperties.GetStringDictionaryValue(KeyGroupTag)})");
                }

                if (group.Carrier == null)
                    continue;

                string value = group.Carrier.customBlockEditProperties.GetStringDictionaryValue(KeyScriptText);
                if (!SpikeCarrier.TryParse(value, out int version, out string scriptId, out string name, out string text))
                {
                    logger.LogError($"[inspect]   carrier text unreadable ({value?.Length ?? 0} chars)");
                    continue;
                }

                string hash = SpikeCarrier.Sha256(text);
                logger.LogInfo(
                    $"[inspect]   carrier: version {version}, id matches {scriptId == group.ScriptId}, name '{name}', " +
                    $"{text.Length} chars, {text.Count(c => c == '\n') + 1} lines, text intact: {hash == expectedHash}");
            }

            logger.LogInfo($"[inspect] Content hash of all blocks: {ContentHash(central)}");
            logger.LogInfo($"[inspect] ZeepSDK LevelHashV2 of the loaded level: {LevelApi.CurrentHashV2?.Hash ?? "(none)"}");
        }

        /// <summary>
        /// SHA256 over every block's saved JSON, sorted by UID: equal before and after an unchanged save/reload
        /// means the level content (and so LevelHashV2) stays the same.
        /// </summary>
        private static string ContentHash(LEV_LevelEditorCentral central)
        {
            StringBuilder builder = new();
            foreach (BlockProperties block in central.undoRedo.allBlocksDictionary.Values
                         .Where(block => block != null)
                         .OrderBy(block => block.UID, System.StringComparer.Ordinal))
            {
                builder.Append(block.ConvertBlockToJSON_v15_string(true)).Append('\n');
            }

            return SpikeCarrier.Sha256(builder.ToString());
        }
    }
}
