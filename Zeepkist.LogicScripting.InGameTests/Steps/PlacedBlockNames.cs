using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// The UIDs of the blocks <c>place-block ... as &lt;name&gt;</c> placed, by that name, so a step's text can write one
/// as <c>{uid:&lt;name&gt;}</c>.
/// </summary>
internal sealed class PlacedBlockNames
{
    private static readonly Regex _placeholder = new(@"\{uid:([^}]*)\}");

    private readonly Dictionary<string, string> _uids = [];

    public string Listed => _uids.Count == 0 ? "(none)" : string.Join(", ", _uids.Keys);

    /// <summary>
    /// Returns false when a block was already placed as <paramref name="name"/>.
    /// </summary>
    public bool Remember(string name, string uid)
    {
        if (_uids.ContainsKey(name))
            return false;

        _uids.Add(name, uid);
        return true;
    }

    /// <summary>
    /// Writes each <c>{uid:&lt;name&gt;}</c> in <paramref name="text"/> as that block's UID. Returns null, with the
    /// first name no block was placed as in <paramref name="unknown"/>, when there is one.
    /// </summary>
    public string Expand(string text, out string unknown)
    {
        unknown = _placeholder.Matches(text).Cast<Match>().Select(m => m.Groups[1].Value).FirstOrDefault(n => !_uids.ContainsKey(n));
        return unknown == null ? _placeholder.Replace(text, m => _uids[m.Groups[1].Value]) : null;
    }
}
