using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// Every Library, as the one JSON file that holds them all.
/// </summary>
internal sealed class LibrariesFile
{
    public List<LibraryFileEntry> Libraries { get; set; } = [];
}
