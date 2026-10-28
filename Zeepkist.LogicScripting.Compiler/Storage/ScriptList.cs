using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// The Scripts the window shows: those the level holds, plus those made in the window and not stored in the level.
/// </summary>
public sealed class ScriptList
{
    private readonly List<ScriptEntry> _entries = [];

    public IReadOnlyList<ScriptEntry> Entries => _entries;

    /// <summary>
    /// Returns why <paramref name="name"/> can't name a new Script, or the Script with <paramref name="renamedId"/>,
    /// or null when it can. Trim it first.
    /// </summary>
    public string GetNameError(string name, ScriptId renamedId = null)
    {
        return ScriptName.GetError(name, _entries.Where(e => e.Id != renamedId).Select(e => e.Name));
    }

    /// <summary>
    /// Adds an empty Script with a new id. Check the name with <see cref="GetNameError"/> first.
    /// </summary>
    public ScriptEntry Add(string name)
    {
        var entry = new ScriptEntry(ScriptId.New(), name, string.Empty);
        _entries.Add(entry);
        return entry;
    }

    public ScriptEntry Find(ScriptId id)
    {
        return _entries.FirstOrDefault(e => e.Id == id);
    }

    /// <summary>
    /// Takes in what the level holds now. A stored Script the window doesn't have yet is added with the level's name
    /// and text; a Script the window has keeps its own name and text, whatever the level holds.
    /// </summary>
    /// <param name="stored">Every Script whose text the level holds.</param>
    /// <param name="groupIds">The Scripts that have blocks in the level.</param>
    public void Update(IReadOnlyList<StoredScript> stored, IReadOnlyCollection<ScriptId> groupIds)
    {
        foreach (var script in stored)
        {
            if (Find(script.Id) == null)
                _entries.Add(new ScriptEntry(script.Id, script.Name, script.Text));
        }

        foreach (var entry in _entries)
        {
            entry.Stored = stored.FirstOrDefault(s => s.Id == entry.Id);
            entry.HasGroup = groupIds.Contains(entry.Id);
        }
    }

    /// <summary>
    /// Forgets every Script, for another level.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }
}
