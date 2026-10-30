using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Texts mark the clicked character with <c>|</c> before it.
/// </summary>
public class DefinitionJumpTests
{
    [Fact]
    public void DefinedName_JumpsToTheNameInItsDefinition()
    {
        var jump = At("a = 1\n  x = logic(a, 1)\npower_display(|x)");

        Assert.Equal((8, 1), (jump.Target.Start, jump.Target.Length));
        Assert.Equal((38, 1), (jump.Name.Start, jump.Name.Length));
    }

    [Fact]
    public void DefinedName_AnyCharacterOfTheName_Jumps()
    {
        Assert.Equal(0, At("abc = 1\npower_display(ab|c)").Target.Start);
    }

    [Fact]
    public void DefinedName_DefinedTwice_JumpsToTheLatestDefinitionAbove()
    {
        Assert.Equal(6, At("x = 1\nx = 2\npower_display(|x)\nx = 3").Target.Start);
    }

    [Fact]
    public void DefinedName_DefinedOnlyBelow_JumpsToTheFirstDefinitionBelow()
    {
        Assert.Equal(27, At("x = delay(|later, ticks: 1)\nlater = 1\nlater = 2").Target.Start);
    }

    [Fact]
    public void DefinedName_OnAHalfTypedLine_StillJumps()
    {
        Assert.Equal(0, At("a = generator(power: 1)\nx = logic(|a, ").Target.Start);
    }

    [Fact]
    public void DefinedName_BeforeAnOutput_Jumps()
    {
        Assert.Equal(0, At("p = player_input()\nd = |p.horn").Target.Start);
    }

    [Fact]
    public void ScriptFunction_JumpsToItsNameInTheHeader()
    {
        var jump = At("function edge(x) -> rising\n    rising = x\nend\npower_display(ed|ge(1))");

        Assert.Equal((9, 4), (jump.Target.Start, jump.Target.Length));
    }

    [Theory]
    [InlineData("x = |nothing")]
    [InlineData("x = |logic(1, 2)")]
    [InlineData("x = m|ux(1, 2)")]
    [InlineData("a = 1\nx = logic(|a: a)")]
    [InlineData("op = 1\nx = math(1, 2, op: |op)")]
    [InlineData("horn = 1\np = player_input()\nd = p.|horn")]
    [InlineData("x = 1 # |x")]
    [InlineData("x = |1")]
    [InlineData("x = 1|")]
    [InlineData("|")]
    [InlineData("|function f(x) -> y\nend")]
    public void NotADefinedName_IsNull(string marked)
    {
        Assert.Null(At(marked));
    }

    [Fact]
    public void IndexOutsideTheText_IsNull()
    {
        Assert.Null(DefinitionJump.At("x = 1", -1));
        Assert.Null(DefinitionJump.At("x = 1", 6));
        Assert.Null(DefinitionJump.At(null, 0));
    }

    private static DefinitionJump At(string marked)
    {
        var index = marked.IndexOf('|');
        return DefinitionJump.At(marked.Remove(index, 1), index);
    }
}
