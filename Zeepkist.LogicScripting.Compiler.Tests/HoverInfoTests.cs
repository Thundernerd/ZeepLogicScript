using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Texts mark the hovered character with <c>|</c> before it.
/// </summary>
public class HoverInfoTests
{
    private static readonly Dictionary<string, PlacedBlock> _level = new()
    {
        ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId)
    };

    [Theory]
    [InlineData("x = |logic(a, b)")]
    [InlineData("x = log|ic(a, b)")]
    [InlineData("x = logi|c")]
    public void BlockFunction_IsItsReferenceEntry(string marked)
    {
        var info = At(marked);

        Assert.Same(BlockReference.GetEntry(BlockManifest.Logic), info.Function);
        Assert.Equal((4, 5), (info.Span.Start, info.Span.Length));
        Assert.Null(info.Argument);
        Assert.Null(info.Definition);
    }

    [Fact]
    public void Alias_IsTheEntryOfItsBlockFunction()
    {
        Assert.Same(BlockReference.GetEntry(BlockManifest.Multiplexer), At("x = m|ux(1, 2)").Function);
    }

    [Fact]
    public void DefinedName_IsTheLineThatDefinesIt()
    {
        var info = At("a = 1\nb = 2\n  x = logic(a, b)  # both\npower_display(|x)");

        Assert.Equal(3, info.DefinitionLine);
        Assert.Equal("x = logic(a, b)  # both", info.Definition);
        Assert.Equal((52, 1), (info.Span.Start, info.Span.Length));
        Assert.Null(info.Function);
    }

    [Fact]
    public void DefinedName_InItsOwnDefinition_IsThatLine()
    {
        var info = At("|x = 1");

        Assert.Equal((1, "x = 1"), (info.DefinitionLine, info.Definition));
    }

    [Fact]
    public void DefinedName_DefinedTwice_IsTheLatestDefinitionAbove()
    {
        Assert.Equal(2, At("x = 1\nx = 2\npower_display(|x)\nx = 3").DefinitionLine);
    }

    [Fact]
    public void DefinedName_DefinedOnlyBelow_IsTheFirstDefinitionBelow()
    {
        Assert.Equal(2, At("x = delay(|later, ticks: 1)\nlater = 1\nlater = 2").DefinitionLine);
    }

    [Fact]
    public void DefinedName_OnAHalfTypedLine_StillFound()
    {
        Assert.Equal(1, At("a = generator(power: 1)\nx = logic(|a, ").DefinitionLine);
    }

    [Fact]
    public void ScriptFunction_IsItsHeader()
    {
        var info = At("function edge(x) -> rising\n    rising = x\nend\npower_display(ed|ge(1))");

        Assert.Equal((1, "function edge(x) -> rising"), (info.DefinitionLine, info.Definition));
    }

    [Theory]
    [InlineData("x = |nothing")]
    [InlineData("x = 1 |+ 2")]
    [InlineData("x = |1")]
    [InlineData("x = placed(\"|L\")")]
    [InlineData("x = 1 # |x")]
    [InlineData("x = 1|")]
    [InlineData("|")]
    [InlineData("p = player_input()\nd = p.|horn")]
    [InlineData("|function f(x) -> y\nend")]
    [InlineData("|default hide: true")]
    [InlineData("x = math(1, 2, 3, op: |lerp)")]
    public void NothingToTell_IsNull(string marked)
    {
        Assert.Null(At(marked));
    }

    [Fact]
    public void NamedArgument_IsItsSettingsRow()
    {
        var info = At("x = logic(a, b, |op: or)");

        Assert.Equal("op", info.Argument.Name);
        Assert.Equal(BlockReference.DescribeValues(BlockManifest.Logic.Settings[0]), info.Argument.Value);
        Assert.Equal((16, 2), (info.Span.Start, info.Span.Length));
        Assert.Null(info.Definition);
    }

    [Fact]
    public void NamedArgument_ForAnInput_IsTheInput()
    {
        var info = At("a = 1\nx = logic(|a: a)");

        Assert.Equal(("a", "input"), (info.Argument.Name, info.Argument.Value));
        Assert.Equal(0, info.DefinitionLine);
    }

    [Fact]
    public void NamedArgument_Hide_IsItsRow()
    {
        Assert.Equal(BlockReference.DescribeValues(BlockSetting.Hide), At("power_display(1, h|ide: true)").Argument.Value);
    }

    [Fact]
    public void NamedArgument_OfAPlacedBlockReference_IsItsInput()
    {
        var info = At("lamp_1 = placed(\"L\")\nlamp_1(|intensity: 1)", uid => _level.GetValueOrDefault(uid));

        Assert.Equal(("intensity", "input"), (info.Argument.Name, info.Argument.Value));
    }

    [Theory]
    [InlineData("x = logic(a, |nope: 1)")]
    [InlineData("x = logic(a, nothing(|op: 1))")]
    public void NamedArgument_TheCallDoesNotTake_IsNull(string marked)
    {
        Assert.Null(At(marked));
    }

    [Fact]
    public void ErrorUnderline_IsItsMessage()
    {
        ErrorUnderline[] errors = [new(new TextSpan(4, 3), "'abc' isn't defined")];

        var info = At("x = a|bc", errors: errors);

        Assert.Equal(["'abc' isn't defined"], info.Errors);
        Assert.Null(info.Definition);
    }

    [Fact]
    public void ErrorUnderlines_AllAtTheCharacter_InOrder_WithTheNameToo()
    {
        ErrorUnderline[] errors = [new(new TextSpan(4, 5), "first"), new(new TextSpan(0, 1), "elsewhere"), new(new TextSpan(4, 1), "second")];

        var info = At("x = |logic", errors: errors);

        Assert.Equal(["first", "second"], info.Errors);
        Assert.NotNull(info.Function);
    }

    [Fact]
    public void ErrorUnderline_Empty_CoversItsOwnIndex()
    {
        ErrorUnderline[] errors = [new(new TextSpan(9, 0), "expected ')'")];

        var info = At("x = f(1, |", errors: errors);

        Assert.Equal(["expected ')'"], info.Errors);
        Assert.Equal((9, 0), (info.Span.Start, info.Span.Length));
    }

    [Fact]
    public void ErrorUnderline_EndsBeforeTheCharacter_IsNotShown()
    {
        ErrorUnderline[] errors = [new(new TextSpan(4, 1), "here")];

        Assert.Null(At("x = 1|2 + 3", errors: errors));
    }

    private static HoverInfo At(string marked, Func<string, PlacedBlock> findPlacedBlock = null, IReadOnlyList<ErrorUnderline> errors = null)
    {
        var index = marked.IndexOf('|');
        return HoverInfo.At(marked.Remove(index, 1), index, errors, findPlacedBlock);
    }
}
