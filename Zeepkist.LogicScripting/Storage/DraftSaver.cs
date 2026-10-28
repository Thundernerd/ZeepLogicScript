using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// Saves the Drafts of the window's Scripts shortly after the last change, so they aren't written on every key while
/// typing.
/// </summary>
internal sealed class DraftSaver
{
    private const float Delay = 1;

    private readonly DraftStore _store;
    private readonly ScriptList _scripts;
    private readonly DraftChanges _changes = new();
    private float? _saveTime;

    public DraftSaver(DraftStore store, ScriptList scripts)
    {
        _store = store;
        _scripts = scripts;
    }

    public void Request()
    {
        _saveTime = Time.unscaledTime + Delay;
    }

    /// <summary>
    /// Call every frame; <paramref name="now"/> saves a pending change without waiting.
    /// </summary>
    public void Update(bool now)
    {
        if (_saveTime != null && (now || Time.unscaledTime >= _saveTime))
            Save();
    }

    /// <summary>
    /// Writes the Draft of every Script that changed since its last one.
    /// </summary>
    public void Save()
    {
        _saveTime = null;
        foreach (var draft in _changes.TakeChanged(_scripts.Entries, _store.Find))
        {
            _store.Save(draft);
        }
    }
}
