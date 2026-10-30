using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class ReferenceEntryLayoutTests
{
    [Fact]
    public void BlockEntry_HasItsInputsAndOutputsAsRows_AndItsSettingsUnderAHeading()
    {
        var layout = Find("logic").Layout;

        Assert.Equal("logic", layout.Function);
        Assert.Equal(string.Empty, layout.Note);
        Assert.Equal([("inputs", "a, b, op_in"), ("outputs", "out")], Rows(layout.Rows));
        var group = Assert.Single(layout.Groups);
        Assert.Equal("settings", group.Heading);
        Assert.Equal([("op", "and, or, nand, nor, xor, xnor"), ("hide", "true, false")], Rows(group.Rows));
    }

    [Fact]
    public void BlockEntryWithModes_HasItsOutputsPerModeUnderAHeading_BeforeItsSettings()
    {
        var layout = Find("simple_telemetry").Layout;

        Assert.Equal([("inputs", "–")], Rows(layout.Rows));
        Assert.Equal(["outputs", "settings"], layout.Groups.Select(g => g.Heading));
        Assert.Equal(("basic", "speed, yaw, up, state"), Rows(layout.Groups[0].Rows)[0]);
    }

    [Fact]
    public void BlockEntryWithoutSettings_HasNoSettingsHeading()
    {
        Assert.Empty(Find("trigger").Layout.Groups);
    }

    [Fact]
    public void BlockEntryWithAliases_NotesThem()
    {
        Assert.Equal("alias mux", Find("multiplexer").Layout.Note);
    }

    [Fact]
    public void PlacedOnlyBlockEntry_NotesHowToReachIt()
    {
        Assert.Equal("placed(\"UID\") only", Find("lamp").Layout.Note);
    }

    [Fact]
    public void LibraryEntry_HasItsInputsAndOutputsAsRows()
    {
        var layout = Assert.Single(LibraryReference.Describe(WithLibrary("timing", "function edge(x) -> rise\n    rise = x\nend\n"))).Layout;

        Assert.Equal("timing.edge", layout.Function);
        Assert.Equal(string.Empty, layout.Note);
        Assert.Equal([("inputs", "x"), ("outputs", "rise")], Rows(layout.Rows));
        Assert.Empty(layout.Groups);
    }

    [Fact]
    public void LibraryEntryWhoseLibraryHasErrors_NotesItCantBeCalled()
    {
        var layout = Assert.Single(LibraryReference.Describe(WithLibrary("broken", "x = 1\nfunction a() -> y\n    y = 1\nend\n"))).Layout;

        Assert.Equal("its Library has errors, so it can't be called", layout.Note);
    }

    private static LibraryList WithLibrary(string name, string text)
    {
        var list = new LibraryList();
        list.SetText(list.Add(name), text);
        return list;
    }

    private static List<(string, string)> Rows(IReadOnlyList<ReferenceRow> rows)
    {
        return rows.Select(r => (r.Name, r.Value)).ToList();
    }

    private static BlockReferenceEntry Find(string function)
    {
        return BlockReference.Categories.SelectMany(c => c.Entries).Single(e => e.Function == function);
    }
}
