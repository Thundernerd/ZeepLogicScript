using System;
using System.IO;

namespace TNRD.Zeepkist.LogicScripting.Tests;

/// <summary>
/// Reads the repo's own files, such as the docs, from the folder that holds the solution.
/// </summary>
internal static class RepoFiles
{
    public static string Root { get; } = FindRoot();

    public static string Read(string path)
    {
        return File.ReadAllText(Path.Combine(Root, path));
    }

    private static string FindRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "Zeepkist.LogicScripting.sln")))
                return folder.FullName;
        }

        throw new InvalidOperationException("No Zeepkist.LogicScripting.sln above " + AppContext.BaseDirectory);
    }
}
