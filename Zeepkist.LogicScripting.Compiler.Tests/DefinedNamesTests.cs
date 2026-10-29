using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class DefinedNamesTests
{
    private static readonly Dictionary<string, PlacedBlock> _level = new()
    {
        ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId),
        ["OTHER"] = new PlacedBlock(1)
    };

    [Fact]
    public void Read_EachDefinition_WithWhereItsNameIs()
    {
        var names = Read("a = generator(power: 1)\n\nb = a & 1");

        Assert.Equal(["a", "b"], names.Select(n => n.Name));
        Assert.Equal(0, names[0].Span.Start);
        Assert.Equal(25, names[1].Span.Start);
        Assert.Equal(1, names[1].Span.Length);
    }

    [Theory]
    [InlineData("x = logic(1, 2)", "logic")]
    [InlineData("x = mux(1, 2)", "multiplexer")]
    [InlineData("x = delay(x, ticks: 1)", "delay")]
    [InlineData("x = logic(\n    1,\n    2\n)", "logic")]
    [InlineData("x = logic(1, ", "logic")]
    [InlineData("x = placed(\"L\")", "lamp")]
    [InlineData("x = placed(\"OTHER\")", "block")]
    public void Read_TheBlockType_OfACall(string text, string function)
    {
        Assert.Equal(function, Read(text)[0].Type?.Function);
    }

    [Theory]
    [InlineData("x = 1")]
    [InlineData("x = a")]
    [InlineData("x = a & b")]
    [InlineData("x = logic(1) & 1")]
    [InlineData("x = p.horn")]
    [InlineData("x = player_input().horn")]
    [InlineData("x = unknown(1)")]
    [InlineData("x = placed(\"MISSING\")")]
    [InlineData("x = placed()")]
    [InlineData("x = ")]
    public void Read_NoType_WhenTheValueIsNoKnownCall(string text)
    {
        Assert.Null(Read(text)[0].Type);
    }

    [Theory]
    [InlineData("t = simple_telemetry(mode: local_velocity)", "local_velocity")]
    [InlineData("t = simple_telemetry(player: 1, mode: global_velocity)", "global_velocity")]
    [InlineData("t = simple_telemetry(\n    mode: basic\n)", "basic")]
    [InlineData("t = simple_telemetry(mode: local_velocity", "local_velocity")]
    [InlineData("t = simple_telemetry()", null)]
    [InlineData("t = simple_telemetry(mode: )", null)]
    [InlineData("t = simple_telemetry(logic(mode: x))", null)]
    [InlineData("t = logic(mode: and)", null)]
    public void Read_TheWrittenMode(string text, string mode)
    {
        Assert.Equal(mode, Read(text)[0].Mode);
    }

    [Fact]
    public void Read_DefinitionsAroundAHalfTypedLine()
    {
        var names = Read("a = 1\nb = logic(1,, ]\nc = power_display(\nd = 2");

        Assert.Equal(["a", "b", "c", "d"], names.Select(n => n.Name));
    }

    [Theory]
    [InlineData("logic = 1")]
    [InlineData("placed = 1")]
    [InlineData("true = 1")]
    [InlineData("x == 1")]
    [InlineData("power_display(x = 1)")]
    [InlineData("x.y = 1")]
    [InlineData("# x = 1")]
    public void Read_NothingThatIsntADefinitionOfAName(string text)
    {
        Assert.Empty(Read(text));
    }

    [Fact]
    public void Read_ANameDefinedTwice_Twice()
    {
        Assert.Equal(["a", "a"], Read("a = 1\na = 2").Select(n => n.Name));
    }

    private static IReadOnlyList<DefinedName> Read(string text)
    {
        return DefinedNames.Read(text, uid => _level.GetValueOrDefault(uid));
    }
}
