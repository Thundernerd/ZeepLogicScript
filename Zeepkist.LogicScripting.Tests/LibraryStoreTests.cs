using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class LibraryStoreTests
{
    private const string FileName = "libraries";
    private const string Edge = "function edge(x) -> y\n    y = x\nend";

    private readonly MemoryStorage _storage = new();

    [Fact]
    public void WithoutAFile_ThereAreNoLibraries()
    {
        var store = CreateStore();

        Assert.Empty(store.Libraries.Entries);
        Assert.False(store.ReadFailed);
    }

    [Fact]
    public void SavedLibraries_LoadAgain()
    {
        var store = CreateStore();
        var timing = store.Libraries.Add("timing");
        store.Libraries.SetText(timing, Edge);
        store.Libraries.Add("latches");

        store.Save();
        var loaded = CreateStore().Libraries.Entries;

        Assert.Collection(
            loaded,
            l =>
            {
                Assert.Equal(timing.Id, l.Id);
                Assert.Equal("timing", l.Name);
                Assert.Equal(Edge, l.Text);
            },
            l => Assert.Equal("latches", l.Name));
    }

    [Fact]
    public void ADeletedLibrary_StaysDeleted()
    {
        var store = CreateStore();
        var timing = store.Libraries.Add("timing");
        store.Save();

        store.Libraries.Remove(timing.Id);
        store.Save();

        Assert.Empty(CreateStore().Libraries.Entries);
    }

    [Fact]
    public void WithoutChanges_NothingIsWritten()
    {
        CreateStore().Save();

        Assert.False(_storage.JsonFileExists(FileName));
    }

    [Fact]
    public void AnUnreadableFile_IsNeverWrittenOver()
    {
        _storage.WriteJson(FileName, "{ not json");
        var store = CreateStore();

        store.Libraries.Add("timing");
        store.Save();

        Assert.True(store.ReadFailed);
        Assert.Equal("{ not json", _storage.ReadJson(FileName));
    }

    [Fact]
    public void EntriesWithoutAnIdOrName_OrWithATakenIdOrName_AreSkipped()
    {
        _storage.WriteJson(FileName, """
            { "Libraries": [
                { "Id": "a", "Name": "timing" },
                { "Name": "no_id", "Text": "" },
                { "Id": "b", "Text": "" },
                { "Id": "a", "Name": "again", "Text": "" },
                { "Id": "c", "Name": "Timing", "Text": "" },
                null
            ] }
            """);

        var library = Assert.Single(CreateStore().Libraries.Entries);

        Assert.Equal("timing", library.Name);
        Assert.Equal(string.Empty, library.Text);
    }

    private LibraryStore CreateStore()
    {
        return new LibraryStore(_storage, new ManualLogSource("Tests"));
    }
}
