using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class LibraryListTests
{
    private const string Edge = "function edge(x) -> y\n    y = x\nend";

    [Fact]
    public void ANewLibrary_IsEmpty()
    {
        var list = new LibraryList();

        var library = list.Add("timing");

        Assert.Equal("timing", library.Name);
        Assert.Equal(string.Empty, library.Text);
        Assert.Same(library, Assert.Single(list.Entries));
    }

    [Fact]
    public void NewLibraries_GetTheirOwnIds()
    {
        var list = new LibraryList();

        Assert.NotEqual(list.Add("a").Id, list.Add("b").Id);
    }

    [Fact]
    public void FindText_MatchesTheNameExactly()
    {
        var list = new LibraryList();
        list.SetText(list.Add("timing"), Edge);

        Assert.Equal(Edge, list.FindText("timing"));
        Assert.Null(list.FindText("Timing"));
        Assert.Null(list.FindText("latches"));
    }

    [Fact]
    public void ARenamedLibrary_IsFoundOnlyByItsNewName()
    {
        var list = new LibraryList();
        var library = list.Add("timing");
        list.SetText(library, Edge);

        list.Rename(library, "edges");

        Assert.Null(list.FindText("timing"));
        Assert.Equal(Edge, list.FindText("edges"));
    }

    [Fact]
    public void ARemovedLibrary_IsGone()
    {
        var list = new LibraryList();
        var library = list.Add("timing");

        list.Remove(library.Id);

        Assert.Empty(list.Entries);
        Assert.Null(list.FindText("timing"));
    }

    [Fact]
    public void GetNameError_ChecksAgainstTheOtherLibraries()
    {
        var list = new LibraryList();
        var timing = list.Add("timing");

        Assert.NotNull(list.GetNameError("Timing"));
        Assert.Null(list.GetNameError("Timing", timing.Id));
        Assert.Null(list.GetNameError("latches"));
    }

    [Fact]
    public void FunctionNames_FollowTheText()
    {
        var list = new LibraryList();
        var library = list.Add("timing");
        Assert.Empty(list.FunctionNames["timing"]);

        list.SetText(library, Edge);

        Assert.Equal(["edge"], list.FunctionNames["timing"]);
    }

    [Fact]
    public void EveryChange_RaisesTheVersion()
    {
        var list = new LibraryList();
        var versions = new List<int> { list.Version };
        var library = list.Add("timing");
        versions.Add(list.Version);
        list.SetText(library, Edge);
        versions.Add(list.Version);
        list.Rename(library, "edges");
        versions.Add(list.Version);
        list.Remove(library.Id);
        versions.Add(list.Version);

        Assert.Equal(versions.Distinct().Count(), versions.Count);
    }

    [Fact]
    public void AnUnchangedTextOrName_KeepsTheVersion()
    {
        var list = new LibraryList();
        var library = list.Add("timing");
        var version = list.Version;

        list.SetText(library, string.Empty);
        list.Rename(library, "timing");

        Assert.Equal(version, list.Version);
    }
}
