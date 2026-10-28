using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// Reads the Scripts the level holds from their Block Groups (ADR 0006).
/// </summary>
internal static class LevelScripts
{
    /// <summary>
    /// Goes through all the level's blocks, so call it after a change, not per frame.
    /// </summary>
    /// <returns>Every Script whose text the level holds, and the number of blocks of every Script with blocks in the
    /// level.</returns>
    public static (List<StoredScript> Stored, Dictionary<ScriptId, int> BlockCounts) Find(LEV_LevelEditorCentral central)
    {
        List<StoredScript> stored = [];
        Dictionary<ScriptId, int> blockCounts = [];
        HashSet<ScriptId> storedIds = [];
        foreach (var block in central.undoRedo.allBlocksDictionary.Values)
        {
            var scriptId = block == null ? null : BlockGroups.GetScriptId(block);
            if (scriptId == null)
                continue;

            blockCounts[scriptId] = blockCounts.TryGetValue(scriptId, out var count) ? count + 1 : 1;
            var script = Read(block.customBlockEditProperties, scriptId);
            if (script != null && storedIds.Add(scriptId))
                stored.Add(script);
        }

        return (stored, blockCounts);
    }

    /// <summary>
    /// Writes the Script onto a block of its Block Group, which must already carry its tag.
    /// </summary>
    public static void Write(PropertyDictionariesJSON properties, StoredScript script)
    {
        properties.SetStringDictionaryValue(ScriptCarrier.NameKey, script.Name);
        properties.SetStringDictionaryValue(ScriptCarrier.TextKey, script.Text);
        properties.SetIntDictionaryValue(ScriptCarrier.LanguageVersionKey, script.LanguageVersion);
    }

    /// <summary>
    /// Takes the block out of its Block Group, with the Script it carries, if any.
    /// </summary>
    public static void Untag(PropertyDictionariesJSON properties)
    {
        properties.t?.Remove(BlockGroupTag.Key);
        properties.t?.Remove(ScriptCarrier.NameKey);
        properties.t?.Remove(ScriptCarrier.TextKey);
        properties.n?.Remove(ScriptCarrier.LanguageVersionKey);
    }

    /// <summary>
    /// Whether the block, a member of a Block Group, holds its Script's text.
    /// </summary>
    public static bool IsCarrier(BlockProperties block)
    {
        return block.customBlockEditProperties?.t?.ContainsKey(ScriptCarrier.TextKey) == true;
    }

    private static StoredScript Read(PropertyDictionariesJSON properties, ScriptId scriptId)
    {
        if (properties?.t == null || !properties.t.TryGetValue(ScriptCarrier.TextKey, out var text))
            return null;

        return new StoredScript(
            scriptId,
            properties.GetStringDictionaryValue(ScriptCarrier.NameKey),
            text,
            properties.GetIntDictionaryValue(ScriptCarrier.LanguageVersionKey));
    }
}
