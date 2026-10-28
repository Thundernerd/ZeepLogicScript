using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class PlacedBlockReferenceInsertTests
{
    private static readonly SelectedBlock _lamp = new("L", BlockManifest.Lamp.DefaultStyle.BlockId);
    private static readonly SelectedBlock _otherLamp = new("L2", BlockManifest.Lamp.DefaultStyle.BlockId);
    private static readonly SelectedBlock _door = new("D", 1);

    [Fact]
    public void EachBlock_GetsALineNamedAfterItsType()
    {
        var result = PlacedBlockReferenceInsert.Insert(string.Empty, 0, [_lamp, _otherLamp, _door]);

        Assert.Equal("lamp_1 = placed(\"L\")\nlamp_2 = placed(\"L2\")\nblock_1 = placed(\"D\")", result.Text);
        Assert.Equal(result.Text.Length, result.Caret);
        Assert.Equal((3, 0), (result.InsertedCount, result.SkippedCount));
    }

    [Fact]
    public void Names_SkipNumbersAlreadyDefined()
    {
        var result = PlacedBlockReferenceInsert.Insert("lamp_1 = placed(\"X\")\n", 21, [_lamp]);

        Assert.Equal("lamp_1 = placed(\"X\")\nlamp_2 = placed(\"L\")", result.Text);
    }

    [Fact]
    public void BlocksTheScriptAlreadyRefersTo_AreSkipped()
    {
        var result = PlacedBlockReferenceInsert.Insert("x = placed(\"L\")", 0, [_lamp, _lamp]);

        Assert.Equal("x = placed(\"L\")", result.Text);
        Assert.Equal((0, 2), (result.InsertedCount, result.SkippedCount));
    }

    [Theory]
    [InlineData("a = 1\nb = 2", 6, "a = 1\nlamp_1 = placed(\"L\")\nb = 2", 27)]
    [InlineData("a = 1\nb = 2", 2, "a = 1\nlamp_1 = placed(\"L\")\nb = 2", 26)]
    [InlineData("a = 1\nb = 2", 11, "a = 1\nb = 2\nlamp_1 = placed(\"L\")", 32)]
    [InlineData("a = 1", 99, "a = 1\nlamp_1 = placed(\"L\")", 26)]
    public void Lines_GoInWholeAtTheCaret(string text, int caret, string expected, int expectedCaret)
    {
        var result = PlacedBlockReferenceInsert.Insert(text, caret, [_lamp]);

        Assert.Equal(expected, result.Text);
        Assert.Equal(expectedCaret, result.Caret);
    }
}
