using BepInEx.Logging;
using Newtonsoft.Json.Linq;
using TNRD.Zeepkist.LogicScripting.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class WindowStateStoreTests
{
    private const string FileName = "window";

    private readonly MemoryStorage _storage = new();
    private readonly WindowStateStore _store;

    public WindowStateStoreTests()
    {
        _store = new WindowStateStore(_storage, new ManualLogSource("Tests"));
    }

    [Fact]
    public void AFileWithALevelPanel_LoadsItAsTheBlocksPanel()
    {
        _storage.WriteJson(FileName, """{ "Open": true, "Level": { "Collapsed": true, "Size": 120 } }""");

        var state = _store.Load();

        Assert.True(state.Open);
        Assert.True(state.Blocks.Collapsed);
        Assert.Equal(120, state.Blocks.Size);
    }

    [Fact]
    public void Saving_WritesTheBlocksPanelAndNoLevelPanel()
    {
        var state = new WindowState();
        state.Blocks.Size = 120;

        _store.Save(state);

        AssertSavedBlocksPanelSize(120);
    }

    [Fact]
    public void AFileSavedWithALevelPanel_SavesItBackAsTheBlocksPanel()
    {
        _storage.WriteJson(FileName, """{ "Level": { "Size": 120 } }""");

        _store.Save(_store.Load());

        AssertSavedBlocksPanelSize(120);
    }

    [Theory]
    [InlineData("Scripts")]
    [InlineData("Blocks")]
    [InlineData("Level")]
    [InlineData("Errors")]
    public void ANullPanel_LoadsAsAFreshPanel(string panel)
    {
        _storage.WriteJson(FileName, $$"""{ "Open": true, "{{panel}}": null }""");

        var state = _store.Load();

        Assert.True(state.Open);
        Assert.True(state.Scripts.IsSameAs(new PanelState()));
        Assert.True(state.Blocks.IsSameAs(new PanelState()));
        Assert.True(state.Errors.IsSameAs(new PanelState()));
    }

    [Fact]
    public void CollapsedReferenceCategories_AreSavedAndLoaded()
    {
        var state = new WindowState();
        state.CollapsedReferenceCategories.Add("Movers");

        _store.Save(state);

        Assert.Equal(["Movers"], _store.Load().CollapsedReferenceCategories);
    }

    [Fact]
    public void AFileWithoutCollapsedReferenceCategories_LoadsThemAllOpen()
    {
        _storage.WriteJson(FileName, """{ "Open": true }""");

        Assert.Empty(_store.Load().CollapsedReferenceCategories);
    }

    [Fact]
    public void NullCollapsedReferenceCategories_LoadThemAllOpen()
    {
        _storage.WriteJson(FileName, """{ "CollapsedReferenceCategories": null }""");

        Assert.Empty(_store.Load().CollapsedReferenceCategories);
    }

    [Fact]
    public void ACopy_DoesNotShareCollapsedReferenceCategories()
    {
        var state = new WindowState();
        var copy = state.Copy();

        state.CollapsedReferenceCategories.Add("Movers");

        Assert.False(state.IsSameAs(copy));
    }

    [Fact]
    public void NoFile_LoadsAClosedNeverPlacedWindow()
    {
        var state = _store.Load();

        Assert.True(state.IsSameAs(new WindowState()));
    }

    [Fact]
    public void AnUnreadableFile_LoadsAClosedNeverPlacedWindow()
    {
        _storage.WriteJson(FileName, "{ not json");

        var state = _store.Load();

        Assert.True(state.IsSameAs(new WindowState()));
    }

    private void AssertSavedBlocksPanelSize(float size)
    {
        var json = JObject.Parse(_storage.ReadJson(FileName));
        Assert.Equal(size, (float)json["Blocks"]["Size"]);
        Assert.Null(json["Level"]);
    }
}
