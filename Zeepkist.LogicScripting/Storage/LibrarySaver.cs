using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// Saves the Libraries shortly after the last change, so they aren't written on every key while typing.
/// </summary>
internal sealed class LibrarySaver
{
    private const float Delay = 1;

    private readonly LibraryStore _store;
    private int _seenVersion;
    private float? _saveTime;

    public LibrarySaver(LibraryStore store)
    {
        _store = store;
        _seenVersion = store.Libraries.Version;
    }

    /// <summary>
    /// Call every frame; each change pushes the save back.
    /// </summary>
    public void Update()
    {
        var version = _store.Libraries.Version;
        if (version != _seenVersion)
        {
            _seenVersion = version;
            _saveTime = Time.unscaledTime + Delay;
        }

        if (_saveTime != null && Time.unscaledTime >= _saveTime)
            Save();
    }

    /// <summary>
    /// Saves a change without waiting for the delay. Also call when the mod unloads.
    /// </summary>
    public void Save()
    {
        _saveTime = null;
        _seenVersion = _store.Libraries.Version;
        _store.Save();
    }
}
