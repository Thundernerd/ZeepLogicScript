using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class LibraryReferenceTests
{
    private const string PulseAfter = "function pulse_after(x, ticks) -> out\n    out = pulser(delay(x, ticks: ticks))\nend\n";

    private static LibraryList WithLibrary(string name, string text)
    {
        var list = new LibraryList();
        list.SetText(list.Add(name), text);
        return list;
    }

    [Fact]
    public void AnEntry_HasTheQualifiedName_TheInputs_AndTheOutputs()
    {
        var entry = Assert.Single(LibraryReference.Describe(WithLibrary("timing", "function edge(x, y) -> rise, fall\n    rise = x\n    fall = y\nend")));

        Assert.Equal("timing.edge", entry.Function);
        Assert.Equal("x, y", entry.Inputs);
        Assert.Equal("rise, fall", entry.Outputs);
    }

    [Fact]
    public void AnInputUsedAsASetting_IsMarked()
    {
        var entry = Assert.Single(LibraryReference.Describe(WithLibrary("timing", PulseAfter)));

        Assert.Equal("x, ticks (setting)", entry.Inputs);
    }

    [Fact]
    public void AnInputPassedOnToAnotherLibrarysSettingInput_IsMarked()
    {
        var list = WithLibrary("timing", PulseAfter);
        list.SetText(list.Add("wrap"), "function later(x, n) -> y\n    y = timing.pulse_after(x, ticks: n)\nend\n");

        var entry = LibraryReference.Describe(list).Single(e => e.Function == "wrap.later");

        Assert.Equal("x, n (setting)", entry.Inputs);
    }

    [Fact]
    public void AnInputPassedOnToTheSettingInputOfALibraryWithErrors_IsMarked()
    {
        var list = WithLibrary("timing", PulseAfter + "x = 1\n");
        list.SetText(list.Add("wrap"), "function later(x, n) -> y\n    y = timing.pulse_after(x, ticks: n)\nend\n");

        var entry = LibraryReference.Describe(list).Single(e => e.Function == "wrap.later");

        Assert.Equal("x, n (setting)", entry.Inputs);
    }

    [Fact]
    public void LibrariesThatCallEachOther_MarkTheSameSettingInputs()
    {
        var list = WithLibrary("a", "function forward(x, n) -> y\n    y = b.forward(x, n)\nend\n" + PulseAfter);
        list.SetText(list.Add("b"), "function forward(x, n) -> y\n    y = a.pulse_after(x, n)\nend\n");

        var inputs = LibraryReference.Describe(list).Select(e => $"{e.Function}({e.Inputs})");

        Assert.Equal(["a.forward(x, n (setting))", "a.pulse_after(x, ticks (setting))", "b.forward(x, n (setting))"], inputs);
    }

    [Fact]
    public void Entries_AreSortedByQualifiedName_AcrossLibraries()
    {
        var list = WithLibrary("timing", "function z() -> y\n    y = 1\nend\nfunction a() -> y\n    y = 1\nend\n");
        list.SetText(list.Add("latches"), "function set() -> y\n    y = 1\nend\n");

        var names = LibraryReference.Describe(list).Select(e => e.Function);

        Assert.Equal(["latches.set", "timing.a", "timing.z"], names);
    }

    [Fact]
    public void AFunctionWithoutInputs_ShowsNone()
    {
        var entry = Assert.Single(LibraryReference.Describe(WithLibrary("timing", "function one() -> y\n    y = 1\nend")));

        Assert.Equal("–", entry.Inputs);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("TIMING", true)]
    [InlineData("pulse_", true)]
    [InlineData("ticks", true)]
    [InlineData("out", true)]
    [InlineData(" x ", true)]
    [InlineData("setting", false)]
    [InlineData("delay", false)]
    public void Matches_LooksAtTheLibraryFunctionInputAndOutputNames(string filter, bool expected)
    {
        var entry = Assert.Single(LibraryReference.Describe(WithLibrary("timing", PulseAfter)));

        Assert.Equal(expected, entry.Matches(filter));
    }

    [Fact]
    public void ALibraryWithErrors_StillListsTheFunctionsItHas()
    {
        var list = WithLibrary("timing", "x = 1\nfunction a(x) -> y\n    y = nope(x)\nend\nfunction b(");

        var names = LibraryReference.Describe(list).Select(e => e.Function);

        Assert.Contains("timing.a", names);
    }

    [Fact]
    public void AnEntry_TellsWhetherItsLibraryHasErrors()
    {
        var list = WithLibrary("broken", "x = 1\nfunction a() -> y\n    y = 1\nend\n");
        list.SetText(list.Add("fine"), "function b() -> y\n    y = 1\nend\n");

        var entries = LibraryReference.Describe(list);

        Assert.True(entries.Single(e => e.Function == "broken.a").LibraryHasErrors);
        Assert.False(entries.Single(e => e.Function == "fine.b").LibraryHasErrors);
    }

    [Fact]
    public void Entries_AreSortedIgnoringCase()
    {
        var list = WithLibrary("Zeta", "function a() -> y\n    y = 1\nend\n");
        list.SetText(list.Add("alpha"), "function a() -> y\n    y = 1\nend\n");

        var names = LibraryReference.Describe(list).Select(e => e.Function);

        Assert.Equal(["alpha.a", "Zeta.a"], names);
    }
}
