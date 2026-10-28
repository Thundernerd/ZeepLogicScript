using System;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// The Scripts the window shows: those the level holds, plus those made in the window and not stored in the level.
/// </summary>
public sealed class ScriptList
{
    private readonly List<ScriptEntry> _entries = [];
    private readonly List<OrphanedGroup> _orphans = [];

    public IReadOnlyList<ScriptEntry> Entries => _entries;

    /// <summary>
    /// The level's Block Groups whose Script isn't in <see cref="Entries"/>, as of the last <see cref="Update"/>.
    /// </summary>
    public IReadOnlyList<OrphanedGroup> Orphans => _orphans;

    /// <summary>
    /// Returns why <paramref name="name"/> can't name a new Script, or the Script with <paramref name="renamedId"/>,
    /// or null when it can. Trim it first. The level's names count too: a Script renamed in the window keeps its old
    /// name in the level until it's compiled.
    /// </summary>
    public string GetNameError(string name, ScriptId renamedId = null)
    {
        var others = _entries.Where(e => e.Id != renamedId).ToList();
        var names = others.Select(e => e.Name).Concat(others.Where(e => e.Stored != null).Select(e => e.Stored.Name));
        return ScriptName.GetError(name, names);
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
    /// and text, or its Draft's when the Draft was typed over that text; a Script the window has keeps its own name and
    /// text, whatever the level holds.
    /// </summary>
    /// <param name="stored">Every Script whose text the level holds.</param>
    /// <param name="groups">The number of blocks of every Script that has blocks in the level.</param>
    /// <param name="findDraft">The Script's Draft, or null when there is none.</param>
    public void Update(
        IReadOnlyList<StoredScript> stored,
        IReadOnlyDictionary<ScriptId, int> groups,
        Func<ScriptId, Draft> findDraft)
    {
        foreach (var script in stored)
        {
            if (Find(script.Id) != null)
                continue;

            var draft = findDraft(script.Id);
            _entries.Add(draft != null && draft.IsBasedOn(script)
                ? new ScriptEntry(script.Id, GetFreeName(draft.Name ?? script.Name), draft.Text ?? string.Empty)
                : new ScriptEntry(script.Id, GetFreeName(script.Name), script.Text));
        }

        foreach (var entry in _entries)
        {
            entry.Stored = stored.FirstOrDefault(s => s.Id == entry.Id);
            entry.HasGroup = groups.ContainsKey(entry.Id);
        }

        _orphans.Clear();
        foreach (var group in groups)
        {
            if (Find(group.Key) == null)
                _orphans.Add(new OrphanedGroup(group.Key, group.Value, findDraft(group.Key)));
        }
    }

    /// <summary>
    /// Brings an Orphaned Group's Script back from its Draft, under a free name. Only the window has it until it's
    /// compiled.
    /// </summary>
    public ScriptEntry Restore(OrphanedGroup orphan)
    {
        if (orphan.Draft == null)
            throw new ArgumentException("An Orphaned Group without a Draft can't be restored", nameof(orphan));

        var entry = new ScriptEntry(orphan.Id, GetFreeName(orphan.Draft.Name ?? string.Empty), orphan.Draft.Text ?? string.Empty)
        {
            HasGroup = true
        };

        _entries.Add(entry);
        _orphans.Remove(orphan);
        return entry;
    }

    /// <summary>
    /// Drops the Script from the window. The level's copy and blocks are the caller's to remove.
    /// </summary>
    public void Remove(ScriptId id)
    {
        _entries.RemoveAll(e => e.Id == id);
    }

    /// <summary>
    /// For a level from another mod version or edited by hand: without its name, or with a name already taken.
    /// </summary>
    private string GetFreeName(string name)
    {
        var baseName = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (baseName.Length == 0)
            baseName = "Script";

        var candidate = baseName;
        for (var number = 2; GetNameError(candidate) != null && number < 1000; number++)
        {
            var suffix = $" {number}";
            candidate = baseName.Substring(0, Math.Min(baseName.Length, ScriptName.MaxLength - suffix.Length)) + suffix;
        }

        return candidate;
    }

    public void Clear()
    {
        _entries.Clear();
        _orphans.Clear();
    }
}
