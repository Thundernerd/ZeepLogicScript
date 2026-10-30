using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using ZeepSDK.Storage;

namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// Keeps every Library in one JSON file in the mod's storage folder, so the same Library works in every level
/// (ADR 0009).
/// </summary>
internal sealed class LibraryStore
{
    private const string FileName = "libraries";

    private readonly IModStorage _storage;
    private readonly ManualLogSource _logger;
    private int _savedVersion;

    public LibraryStore(IModStorage storage, ManualLogSource logger)
    {
        _storage = storage;
        _logger = logger;
        Libraries = new LibraryList(Load());
        _savedVersion = Libraries.Version;
    }

    public LibraryList Libraries { get; }

    /// <summary>
    /// Whether the file was there but couldn't be read. Nothing is saved then, so fixing the file by hand still brings
    /// the Libraries back.
    /// </summary>
    public bool ReadFailed { get; private set; }

    public bool HasUnsavedChanges => Libraries.Version != _savedVersion;

    /// <summary>
    /// Writes the file when a Library changed since it was last written.
    /// </summary>
    public void Save()
    {
        if (ReadFailed || !HasUnsavedChanges)
            return;

        var file = new LibrariesFile
        {
            Libraries = Libraries.Entries
                .Select(l => new LibraryFileEntry { Id = l.Id.ToString(), Name = l.Name, Text = l.Text })
                .ToList()
        };

        _storage.SaveToJson(FileName, file);
        _savedVersion = Libraries.Version;
    }

    private List<Library> Load()
    {
        if (!_storage.JsonFileExists(FileName))
            return [];

        var result = _storage.TryLoadFromJson<LibrariesFile>(FileName);
        if (!result.IsSuccess || result.Value == null)
        {
            ReadFailed = true;
            _logger.LogWarning($"Couldn't read the Libraries, so changes to them aren't saved until the file is fixed or removed: {result.Error} {result.Exception}");
            return [];
        }

        // A file edited by hand may lack a field, or hold an id twice
        List<Library> libraries = [];
        foreach (var entry in result.Value.Libraries ?? [])
        {
            if (entry == null || string.IsNullOrEmpty(entry.Id) || string.IsNullOrEmpty(entry.Name))
                continue;

            var id = new LibraryId(entry.Id);
            if (libraries.Any(l => l.Id == id))
                continue;

            libraries.Add(new Library(id, entry.Name, entry.Text ?? string.Empty));
        }

        return libraries;
    }
}
