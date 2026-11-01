using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Makes sure the Logic Scripting the game loaded is the build the run script deployed next to this plugin, and that
/// BepInEx didn't find another copy of it.
/// </summary>
internal static class BuildCheck
{
    private const string Guid = "Zeepkist.LogicScripting";
    private const string OldGuid = "net.tnrd.zeepkist.logicscripting";

    public static string Problem(string testFolder)
    {
        if (!Chainloader.PluginInfos.TryGetValue(Guid, out var mod))
            return "Logic Scripting isn't loaded";

        var expected = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFolder), Guid));
        var actual = Path.GetFullPath(Path.GetDirectoryName(mod.Location));
        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            return $"Logic Scripting loaded from {actual}, not the deployed build in {expected}";

        // Copies installed before the mod's GUID changed load next to it instead of being skipped
        foreach (var other in Chainloader.PluginInfos.Values)
        {
            if (other.Metadata.GUID != Guid && (other.Metadata.Name == mod.Metadata.Name || other.Metadata.GUID == OldGuid))
                return $"Another copy of Logic Scripting loaded too: {other.Location}";
        }

        var skipped = SkippedCopy();
        return skipped == null ? null : $"BepInEx found another copy: {skipped}";
    }

    /// <summary>
    /// BepInEx only logs the copies it skips for an older version of the same plugin, and its log file is still open.
    /// </summary>
    private static string SkippedCopy()
    {
        var path = Path.Combine(Paths.BepInExRootPath, "LogOutput.log");
        if (!File.Exists(path))
            return null;

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Contains("Skipping [Logic Scripting "))
                return line.Substring(line.IndexOf("Skipping", StringComparison.Ordinal));
        }

        return null;
    }
}
