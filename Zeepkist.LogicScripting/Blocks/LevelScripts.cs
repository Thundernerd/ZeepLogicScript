using System.Collections.Generic;
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
    /// <returns>Every Script whose text the level holds, and every Script with blocks in the level.</returns>
    public static (List<StoredScript> Stored, HashSet<string> GroupIds) Find(LEV_LevelEditorCentral central)
    {
        List<StoredScript> stored = [];
        HashSet<string> groupIds = [];
        HashSet<string> storedIds = [];
        foreach (var block in central.undoRedo.allBlocksDictionary.Values)
        {
            var scriptId = block == null ? null : BlockGroups.GetScriptId(block);
            if (scriptId == null)
                continue;

            groupIds.Add(scriptId);
            var script = Read(block.customBlockEditProperties, scriptId);
            if (script != null && storedIds.Add(scriptId))
                stored.Add(script);
        }

        return (stored, groupIds);
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

    private static StoredScript Read(PropertyDictionariesJSON properties, string scriptId)
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
