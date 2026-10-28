using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// Remembers each Script's last saved Draft, so only the ones that changed are written again.
/// </summary>
public sealed class DraftChanges
{
    private readonly Dictionary<ScriptId, Draft> _saved = [];

    /// <summary>
    /// Returns the Drafts of <paramref name="entries"/> that differ from the last ones returned, and takes them as saved.
    /// </summary>
    public List<Draft> TakeChanged(IEnumerable<ScriptEntry> entries)
    {
        List<Draft> changed = [];
        foreach (var entry in entries)
        {
            var draft = Draft.For(entry);
            if (_saved.TryGetValue(entry.Id, out var saved) && saved.HasSameContent(draft))
                continue;

            _saved[entry.Id] = draft;
            changed.Add(draft);
        }

        return changed;
    }

    /// <summary>
    /// Call when the Draft's file is deleted, so the Script's next Draft is written again.
    /// </summary>
    public void Forget(ScriptId id)
    {
        _saved.Remove(id);
    }
}
