using System.Collections.Generic;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using ZeepSDK.Storage;

namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// Keeps each Script's Draft in a JSON file of its own in the mod's storage folder, named after the Script's id so it
/// survives renaming or copying the level (ADR 0006).
/// </summary>
internal sealed class DraftStore
{
    private const string FilePrefix = "draft-";

    private readonly IModStorage _storage;
    private readonly ManualLogSource _logger;

    // Holds misses too, so the orphan list doesn't read the disk after every change to the level
    private readonly Dictionary<ScriptId, Draft> _loaded = [];

    public DraftStore(IModStorage storage, ManualLogSource logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// The Script's Draft, or null when this machine has none or its file can't be read.
    /// </summary>
    public Draft Find(ScriptId id)
    {
        if (_loaded.TryGetValue(id, out var draft))
            return draft;

        draft = Load(id);
        _loaded[id] = draft;
        return draft;
    }

    public void Save(Draft draft)
    {
        _storage.SaveToJson(GetFileName(draft.Id), new DraftFile { Name = draft.Name, Text = draft.Text, BaseHash = draft.BaseHash });
        _loaded[draft.Id] = draft;
    }

    private Draft Load(ScriptId id)
    {
        var name = GetFileName(id);
        if (!_storage.JsonFileExists(name))
            return null;

        var result = _storage.TryLoadFromJson<DraftFile>(name);
        if (!result.IsSuccess || result.Value == null)
        {
            _logger.LogWarning($"Couldn't read the Draft of Script {id}: {result.Error} {result.Exception}");
            return null;
        }

        // A file edited by hand may lack a field
        return new Draft(id, result.Value.Name ?? string.Empty, result.Value.Text ?? string.Empty, result.Value.BaseHash);
    }

    private static string GetFileName(ScriptId id)
    {
        return FilePrefix + id;
    }
}
